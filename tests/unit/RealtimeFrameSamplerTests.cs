using System;
using System.Globalization;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

/// <summary>Deterministic timestamp fixtures; never read a clock or run the game.</summary>
public static class RealtimeFrameSamplerTests
{
    public static void Run()
    {
        EmptyInputDoesNotInventStatistics();
        FirstCallbackPrimesAndExactly120IntervalsAreExcluded();
        UnsortedIntervalsUseNearestRankAndStrictThresholds();
        NonEnglishCultureKeepsMachineReadableDecimals();
        ClockRegressionCannotBecomeAnUnsignedFrameSpike();
        FullBufferReportsDroppedIntervalsWithoutCorruptingStoredStatistics();
    }

    private static void EmptyInputDoesNotInventStatistics()
    {
        string report = new RealtimeFrameSampler().CreateSummary("empty_fixture");
        AssertField(report, "outcome", "empty_fixture");
        AssertField(report, "samples", "0");
        AssertField(report, "warmup_discarded", "0");
        AssertField(report, "callback_window_wall_ms", "0.000");
        AssertField(report, "sampled_wall_ms", "0.000");
        foreach (string field in new[] { "p50_ms", "p95_ms", "p99_ms", "max_ms" })
            AssertField(report, field, "unavailable");
        AssertField(report, "complete_sample_set", "False");
    }

    private static void FirstCallbackPrimesAndExactly120IntervalsAreExcluded()
    {
        var sampler = new RealtimeFrameSampler();
        ulong ticks = 1000000UL;
        sampler.Observe(ticks);
        string primed = sampler.CreateSummary("primed");
        AssertField(primed, "samples", "0");
        AssertField(primed, "warmup_discarded", "0");
        AssertField(primed, "p50_ms", "unavailable");
        for (int index = 0; index < 119; index++) ObserveAfter(sampler, ref ticks, 1000000UL);
        string beforeBoundary = sampler.CreateSummary("before_boundary");
        AssertField(beforeBoundary, "warmup_discarded", "119");
        AssertField(beforeBoundary, "samples", "0");
        ObserveAfter(sampler, ref ticks, 1000000UL);
        string boundary = sampler.CreateSummary("boundary");
        AssertField(boundary, "warmup_discarded", "120");
        AssertField(boundary, "samples", "0");
        ObserveAfter(sampler, ref ticks, 16667UL);
        string measured = sampler.CreateSummary("measured");
        AssertField(measured, "warmup_discarded", "120");
        AssertField(measured, "samples", "1");
        AssertField(measured, "sampled_wall_ms", "16.667");
        AssertField(measured, "callback_window_wall_ms", "120016.667");
        AssertField(measured, "p50_ms", "16.667");
        AssertField(measured, "max_ms", "16.667");
        AssertField(measured, "over_33_3_ms", "0");
        AssertField(measured, "over_50_ms", "0");
        AssertField(measured, "complete_sample_set", "True");
    }

    private static void UnsortedIntervalsUseNearestRankAndStrictThresholds()
    {
        AssertKnownDistribution(CreateKnownDistribution());
    }

    private static void NonEnglishCultureKeepsMachineReadableDecimals()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            AssertKnownDistribution(CreateKnownDistribution());
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static string CreateKnownDistribution()
    {
        (RealtimeFrameSampler sampler, ulong ticks) = WarmedSampler();
        // Deliberately unsorted. Sorted ranks 10/19/20 are 10000/50001/75000 us.
        // The hand-summed 20 intervals total 419269 us. 33300 and 50000 are exact
        // threshold boundaries: neither may count as strictly greater than itself.
        ulong[] intervals =
        [
            75000, 1000, 33300, 2000, 50000, 3000, 33301, 4000, 50001, 5000,
            40000, 6000, 30000, 7000, 20000, 8000, 16667, 9000, 16000, 10000
        ];
        foreach (ulong interval in intervals) ObserveAfter(sampler, ref ticks, interval);
        return sampler.CreateSummary("known_distribution");
    }

    private static void AssertKnownDistribution(string report)
    {
        AssertField(report, "samples", "20");
        AssertField(report, "warmup_discarded", "120");
        AssertField(report, "sampled_wall_ms", "419.269");
        AssertField(report, "callback_window_wall_ms", "120419.269");
        AssertField(report, "percentile_method", "nearest_rank");
        AssertField(report, "p50_ms", "10.000");
        AssertField(report, "p95_ms", "50.001");
        AssertField(report, "p99_ms", "75.000");
        AssertField(report, "max_ms", "75.000");
        AssertField(report, "over_33_3_ms", "5");
        AssertField(report, "over_50_ms", "2");
        AssertField(report, "complete_sample_set", "True");
    }

    private static void ClockRegressionCannotBecomeAnUnsignedFrameSpike()
    {
        (RealtimeFrameSampler sampler, ulong ticks) = WarmedSampler();
        ObserveAfter(sampler, ref ticks, 20000UL);
        ticks -= 15000UL;
        sampler.Observe(ticks);
        ObserveAfter(sampler, ref ticks, 10000UL);
        string report = sampler.CreateSummary("regressed_clock");
        AssertField(report, "clock_regressions", "1");
        AssertField(report, "samples", "2");
        AssertField(report, "sampled_wall_ms", "30.000");
        AssertField(report, "p50_ms", "10.000");
        AssertField(report, "max_ms", "20.000");
        AssertField(report, "over_50_ms", "0");
        AssertField(report, "complete_sample_set", "False");
    }

    private static void FullBufferReportsDroppedIntervalsWithoutCorruptingStoredStatistics()
    {
        (RealtimeFrameSampler sampler, ulong ticks) = WarmedSampler();
        for (int index = 0; index < 262144; index++) ObserveAfter(sampler, ref ticks, 1000UL);
        ObserveAfter(sampler, ref ticks, 900000UL);
        ObserveAfter(sampler, ref ticks, 1000000UL);
        string report = sampler.CreateSummary("overflow");
        AssertField(report, "samples", "262144");
        AssertField(report, "overflow_discarded", "2");
        AssertField(report, "sampled_wall_ms", "262144.000");
        AssertField(report, "p50_ms", "1.000");
        AssertField(report, "p99_ms", "1.000");
        AssertField(report, "max_ms", "1.000");
        AssertField(report, "over_33_3_ms", "0");
        AssertField(report, "over_50_ms", "0");
        AssertField(report, "complete_sample_set", "False");
    }

    private static (RealtimeFrameSampler Sampler, ulong Ticks) WarmedSampler()
    {
        var sampler = new RealtimeFrameSampler();
        ulong ticks = 1000000UL;
        sampler.Observe(ticks);
        for (int index = 0; index < 120; index++) ObserveAfter(sampler, ref ticks, 1000000UL);
        return (sampler, ticks);
    }

    private static void ObserveAfter(RealtimeFrameSampler sampler, ref ulong ticks, ulong interval)
    {
        ticks += interval;
        sampler.Observe(ticks);
    }

    private static void AssertField(string report, string field, string expected)
    {
        string prefix = field + "=";
        string actual = null;
        foreach (string item in report.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!item.StartsWith(prefix, StringComparison.Ordinal)) continue;
            TestAssert.True(actual == null, $"sampler field {field} occurs only once");
            actual = item[prefix.Length..];
        }
        TestAssert.Equal(expected, actual, $"sampler reports {field} from the supplied wall-clock intervals");
    }
}
