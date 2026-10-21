using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Godot;
using Knight.Presentation.Fog;
using Knight.Tests.Unit;

namespace Knight.Tests.Presentation;

// A same-process kernel comparison, not a frame-rate or GPU benchmark.
// Break caught: reintroducing repeated transforms on the all-clear wedge path.
internal static class HeadlampWedgeCostProbe
{
    private const int Iterations = 6000;
    private const int Rounds = 9;
    private delegate bool Query(Vector2 origin, Vector2 from, Vector2 to);

    internal static void Run(string storageRoot)
    {
        var shapes = new HeadlampOccluder[32];
        for (int i = 0; i < shapes.Length; i++)
            shapes[i] = new HeadlampOccluder(new Transform2D(.27f,
                new Vector2(-10000 - i * 170, -10000 - i * 80)), new Rect2(-50, -4, 100, 8));
        var frame = new HeadlampOcclusionFrame(shapes, true);
        Query current = frame.CanSeeSegment;
        Query legacy = (a, b, c) => HeadlampWedgeEquivalenceTests.LegacyCanSeeSegment(frame.Shapes, a, b, c);
        // Both closures are exercised before timed rounds; each query varies
        // coordinates and its real result is accumulated and checked.
        Measure(current, 4000); Measure(legacy, 4000);
        var currentMs = new double[Rounds]; var legacyMs = new double[Rounds];
        for (int round = 0; round < Rounds; round++)
        {
            if (round % 2 == 0)
            { legacyMs[round] = Measure(legacy, Iterations); currentMs[round] = Measure(current, Iterations); }
            else
            { currentMs[round] = Measure(current, Iterations); legacyMs[round] = Measure(legacy, Iterations); }
        }
        var orderedCurrent = (double[])currentMs.Clone(); var orderedLegacy = (double[])legacyMs.Clone();
        Array.Sort(orderedCurrent); Array.Sort(orderedLegacy);
        double currentMedian = orderedCurrent[Rounds / 2], legacyMedian = orderedLegacy[Rounds / 2];
        double ratio = currentMedian / legacyMedian;
        var result = new { iterations = Iterations, shapes = shapes.Length, rounds = Rounds,
            currentMs, legacyMs, currentMedianMs = currentMedian, legacyMedianMs = legacyMedian, ratio,
            acceptanceRatio = .85, scope = "same-process all-clear CanSeeSegment kernel, not route FPS or GPU time" };
        using (var stream = new FileStream(Path.Combine(storageRoot, "wedge-cost.json"), FileMode.CreateNew))
            JsonSerializer.Serialize(stream, result, new JsonSerializerOptions { WriteIndented = true });
        GD.Print(FormattableString.Invariant($"WEDGE_COST_OBSERVED current_median_ms={currentMedian:F3} legacy_median_ms={legacyMedian:F3} ratio={ratio:F4} rounds={Rounds} queries_per_round={Iterations} shapes={shapes.Length}"));
        TestAssert.True(double.IsFinite(ratio) && currentMedian > 0 && legacyMedian > 0 && ratio < .85,
            "WedgeClearPathAvoidsRepeatedTransformCost: median current/legacy must be below 0.85; observed=" + ratio.ToString("F4", CultureInfo.InvariantCulture));
        GD.Print("WEDGE_COST_TARGET_OK scope=kernel_only not_route_fps=true");
    }

    private static double Measure(Query query, int iterations)
    {
        int visible = 0;
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < iterations; i++)
        {
            float offset = (i & 15) * .03125f;
            if (query(new Vector2(offset, 0), new Vector2(900 + offset, -20), new Vector2(920 + offset, 20))) visible++;
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        TestAssert.Equal(iterations, visible, "all far-away occluders leave every benchmark wedge visible");
        return elapsed;
    }
}
