using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Godot;
using Knight.Presentation.Fog;
using Knight.Tests.Unit;

namespace Knight.Tests.Presentation;

// Same-process quad kernels, not real lane/route FPS. Only all-clear has a cost
// target; first/late-shape blocking explicitly expose iteration-order tradeoffs.
internal static class HeadlampQuadCostProbe
{
    private const int Iterations = 6000;
    private const int Rounds = 9;
    private const int ShapeCount = 32;
    private sealed record Timing(string Scenario, bool ExpectedVisible, double[] CurrentMs, double[] ReferenceMs,
        double CurrentMedianMs, double ReferenceMedianMs, double Ratio);

    internal static void Run(string storageRoot)
    {
        HeadlampQuadEquivalenceTests.QuadQuery current = HeadlampQuadEquivalenceTests.BindCurrent();
        HeadlampQuadEquivalenceTests.QuadQuery reference = HeadlampQuadEquivalenceTests.ReferenceCanSeeQuad;
        var timings = new Timing[3];
        for (int scenario = 0; scenario < timings.Length; scenario++)
        {
            var shapes = new HeadlampOccluder[ShapeCount];
            for (int index = 0; index < shapes.Length; index++)
                shapes[index] = new HeadlampOccluder(new Transform2D(.27f,
                    new Vector2(-10000f - index * 170f, -10000f - index * 80f)), new Rect2(-50f, -4f, 100f, 8f));
            // actor -> first passes through (-2+offset,-5), offset in [0,.234375].
            // The same literal blocker therefore rejects every timed query.
            var blocker = new HeadlampOccluder(Transform2D.Identity, new Rect2(-2.25f, -5.25f, .5f, .5f), 101, 7, 2);
            if (scenario == 1) shapes[0] = blocker;
            if (scenario == 2) shapes[^1] = blocker;
            string name = scenario switch { 0 => "all-clear", 1 => "first-shape-first-edge-block", _ => "late-shape-first-edge-block" };
            timings[scenario] = MeasurePair(name, new HeadlampOcclusionFrame(shapes, true), scenario == 0, current, reference);
        }
        // All three scenarios are retained BEFORE the single performance gate,
        // so the forwarding-API RED also records both blocked-path diagnostics.
        var result = new
        {
            iterations = Iterations, rounds = Rounds, shapes = ShapeCount, scenarios = timings,
            acceptanceRatio = .85, acceptanceScenario = "all-clear",
            reference = "four sequential CURRENT CanSeeSegment calls; no nine-transform legacy baseline",
            scope = "same-process kernel timing only; blocked scenarios are diagnostic, not pass targets; no universal speedup or route FPS claim"
        };
        using (var stream = new FileStream(Path.Combine(storageRoot, "quad-cost.json"), FileMode.CreateNew))
            JsonSerializer.Serialize(stream, result, new JsonSerializerOptions
            { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        foreach (Timing timing in timings)
            GD.Print(FormattableString.Invariant($"QUAD_COST_OBSERVED scenario={timing.Scenario} current_median_ms={timing.CurrentMedianMs:F3} reference_median_ms={timing.ReferenceMedianMs:F3} ratio={timing.Ratio:F4} rounds={Rounds} queries_per_round={Iterations} shapes={ShapeCount}"));
        Timing clear = timings[0];
        TestAssert.True(double.IsFinite(clear.Ratio) && clear.CurrentMedianMs > 0d && clear.ReferenceMedianMs > 0d && clear.Ratio < .85,
            "QuadClearPathAvoidsRepeatedScanCost: all-clear median current/reference must be below 0.85; observed=" +
            clear.Ratio.ToString("F4", CultureInfo.InvariantCulture));
        GD.Print("QUAD_COST_TARGET_OK scope=all_clear_kernel_only blocked_paths_diagnostic=true not_route_fps=true");
    }

    private static Timing MeasurePair(string scenario, HeadlampOcclusionFrame frame, bool expectedVisible,
        HeadlampQuadEquivalenceTests.QuadQuery current, HeadlampQuadEquivalenceTests.QuadQuery reference)
    {
        Measure(current, frame, expectedVisible, 4000); Measure(reference, frame, expectedVisible, 4000);
        var currentMs = new double[Rounds]; var referenceMs = new double[Rounds];
        for (int round = 0; round < Rounds; round++)
        {
            if ((round & 1) == 0)
            { referenceMs[round] = Measure(reference, frame, expectedVisible, Iterations); currentMs[round] = Measure(current, frame, expectedVisible, Iterations); }
            else
            { currentMs[round] = Measure(current, frame, expectedVisible, Iterations); referenceMs[round] = Measure(reference, frame, expectedVisible, Iterations); }
        }
        var orderedCurrent = (double[])currentMs.Clone(); var orderedReference = (double[])referenceMs.Clone();
        Array.Sort(orderedCurrent); Array.Sort(orderedReference);
        double currentMedian = orderedCurrent[Rounds / 2], referenceMedian = orderedReference[Rounds / 2];
        return new(scenario, expectedVisible, currentMs, referenceMs, currentMedian, referenceMedian, currentMedian / referenceMedian);
    }

    private static double Measure(HeadlampQuadEquivalenceTests.QuadQuery query, HeadlampOcclusionFrame frame,
        bool expectedVisible, int iterations)
    {
        int visible = 0;
        long start = Stopwatch.GetTimestamp();
        for (int index = 0; index < iterations; index++)
        {
            float offset = (index & 15) * .015625f;
            if (query(frame, new Vector2(offset, -10f), new Vector2(-4f + offset, 0f), new Vector2(4f + offset, 0f),
                new Vector2(4f + offset, 2f), new Vector2(-4f + offset, 2f))) visible++;
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        TestAssert.Equal(expectedVisible ? iterations : 0, visible, "timed quad queries must retain the scenario's literal visibility result");
        return elapsed;
    }
}
