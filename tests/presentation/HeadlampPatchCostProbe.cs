using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Godot;
using Knight.Presentation.Fog;
using Knight.Tests.Unit;

namespace Knight.Tests.Presentation;

// Same-process all-clear patch kernels only: not route FPS, GPU time, or blocked
// path timing. The independent reference already has the three-transform wedge.
internal static class HeadlampPatchCostProbe
{
    private const int Iterations = 6000;
    private const int Rounds = 9;
    private const int SnapshotRows = 32;
    private const double AcceptanceRatio = .85;
    private delegate bool Query(Vector2 origin, Vector2 from, Vector2 to);
    private sealed record Timing(double[] CurrentMs, double[] ReferenceMs, double CurrentMedianMs,
        double ReferenceMedianMs, double Ratio);

    internal static void Run(string storageRoot)
    {
        var surface = new HeadlampOccluder(Transform2D.Identity, new Rect2(-64f, 0f, 128f, 16f), 101, 7, 2);
        var shapes = new HeadlampOccluder[SnapshotRows];
        shapes[0] = surface;
        for (int index = 1; index < shapes.Length; index++)
            shapes[index] = new HeadlampOccluder(new Transform2D(.27f,
                new Vector2(-10000 - index * 170, -10000 - index * 80)), new Rect2(-50f, -4f, 100f, 8f),
                (ulong)(200 + index), 1, 0);
        var frame = new HeadlampOcclusionFrame(shapes, true);
        Query surfaceCurrent = (a, b, c) => frame.CanSeeSurfacePatch(a, b, c, 2f, surface);
        Query surfaceReference = (a, b, c) => HeadlampPatchEquivalenceTests.LegacyCanSeeSurfacePatch(frame, a, b, c, 2f, surface);
        Query bodyCurrent = (a, b, c) => frame.CanSeeBodyFacePatch(a, surface, b, c, 2f);
        Query bodyReference = (a, b, c) => HeadlampPatchEquivalenceTests.LegacyCanSeeBodyFacePatch(frame, a, surface, b, c, 2f);

        // Complete BOTH measurements before the sole cost gate, so an expected
        // pre-optimization RED still retains both full sets of timed rounds.
        Timing surfaceTiming = MeasurePair(surfaceCurrent, surfaceReference);
        Timing bodyFaceTiming = MeasurePair(bodyCurrent, bodyReference);
        var result = new
        {
            iterations = Iterations, rounds = Rounds, snapshotRows = SnapshotRows,
            terminalRows = 1, nonterminalOccluders = SnapshotRows - 1,
            referenceSourceSha256 = HeadlampPatchEquivalenceTests.ReferenceSourceSha256,
            referenceWedgeClearTransforms = 3, referencePatchClearTransformsPerOccluder = 12,
            surface = surfaceTiming, bodyFace = bodyFaceTiming, acceptanceRatio = AcceptanceRatio,
            scope = "same-process all-clear patch kernels only; not route FPS, GPU time, or blocked-path cost"
        };
        using (var stream = new FileStream(Path.Combine(storageRoot, "patch-cost.json"), FileMode.CreateNew))
            JsonSerializer.Serialize(stream, result, new JsonSerializerOptions
            { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        PrintTiming("surface", surfaceTiming);
        PrintTiming("body-face", bodyFaceTiming);
        TestAssert.True(MeetsTarget(surfaceTiming) && MeetsTarget(bodyFaceTiming),
            "PatchClearPathAvoidsRepeatedTransformCost: both median current/reference ratios must be below 0.85; surface=" +
            surfaceTiming.Ratio.ToString("F4", CultureInfo.InvariantCulture) + "; body-face=" +
            bodyFaceTiming.Ratio.ToString("F4", CultureInfo.InvariantCulture));
        GD.Print("PATCH_COST_TARGET_OK scope=kernel_only not_route_fps=true");
    }

    private static Timing MeasurePair(Query current, Query reference)
    {
        Measure(current, 4000); Measure(reference, 4000);
        var currentMs = new double[Rounds]; var referenceMs = new double[Rounds];
        for (int round = 0; round < Rounds; round++)
        {
            if ((round & 1) == 0)
            { referenceMs[round] = Measure(reference, Iterations); currentMs[round] = Measure(current, Iterations); }
            else
            { currentMs[round] = Measure(current, Iterations); referenceMs[round] = Measure(reference, Iterations); }
        }
        var orderedCurrent = (double[])currentMs.Clone(); var orderedReference = (double[])referenceMs.Clone();
        Array.Sort(orderedCurrent); Array.Sort(orderedReference);
        double currentMedian = orderedCurrent[Rounds / 2], referenceMedian = orderedReference[Rounds / 2];
        return new(currentMs, referenceMs, currentMedian, referenceMedian, currentMedian / referenceMedian);
    }

    private static double Measure(Query query, int iterations)
    {
        int visible = 0;
        long start = Stopwatch.GetTimestamp();
        for (int index = 0; index < iterations; index++)
        {
            float offset = (index & 15) * .03125f;
            if (query(new Vector2(offset, -80f), new Vector2(-24f + offset, 0f), new Vector2(24f + offset, 0f))) visible++;
        }
        double elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        TestAssert.Equal(iterations, visible, "every benchmark patch must remain visible, including all four corners and terminal containment");
        return elapsed;
    }

    private static bool MeetsTarget(Timing timing) => double.IsFinite(timing.Ratio) &&
        timing.CurrentMedianMs > 0d && timing.ReferenceMedianMs > 0d && timing.Ratio < AcceptanceRatio;

    private static void PrintTiming(string api, Timing timing) => GD.Print(FormattableString.Invariant(
        $"PATCH_COST_OBSERVED api={api} current_median_ms={timing.CurrentMedianMs:F3} reference_median_ms={timing.ReferenceMedianMs:F3} ratio={timing.Ratio:F4} rounds={Rounds} queries_per_round={Iterations} snapshot_rows={SnapshotRows}"));
}
