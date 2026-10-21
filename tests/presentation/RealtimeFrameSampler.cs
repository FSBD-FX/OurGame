using System;
using System.Globalization;

namespace Knight.Tests.Presentation;

/// <summary>
/// Test-only wall-clock interval collector. Observe is allocation-free; sorting and formatting
/// happen only after collection stops. The first callback primes the clock, not a zero sample.
/// </summary>
internal sealed class RealtimeFrameSampler
{
    public const int WarmupIntervalCount = 120;
    private const int Capacity = 262144; // 2 MiB, allocated before the measured route starts.
    private readonly double[] _milliseconds = new double[Capacity];
    private bool _primed;
    private ulong _firstCallbackUsec;
    private ulong _lastCallbackUsec;
    private ulong _sampledUsec;
    private int _warmupDiscarded;
    private int _sampleCount;
    private int _overflowCount;
    private int _clockRegressionCount;
    private int _over33Milliseconds;
    private int _over50Milliseconds;

    public void Observe(ulong callbackUsec)
    {
        if (!_primed)
        {
            _primed = true;
            _firstCallbackUsec = _lastCallbackUsec = callbackUsec;
            return;
        }

        if (callbackUsec < _lastCallbackUsec)
        {
            _clockRegressionCount++;
            _lastCallbackUsec = callbackUsec;
            return;
        }

        ulong elapsedUsec = callbackUsec - _lastCallbackUsec;
        _lastCallbackUsec = callbackUsec;
        if (_warmupDiscarded < WarmupIntervalCount)
        {
            _warmupDiscarded++;
            return;
        }
        if (_sampleCount == Capacity)
        {
            _overflowCount++;
            return;
        }

        _milliseconds[_sampleCount++] = elapsedUsec / 1000d;
        _sampledUsec += elapsedUsec;
        if (elapsedUsec > 33300UL) _over33Milliseconds++;
        if (elapsedUsec > 50000UL) _over50Milliseconds++;
    }

    public string CreateSummary(string outcome)
    {
        // No callback can append while this runs: the driver stops collection first.
        Array.Sort(_milliseconds, 0, _sampleCount);
        double totalWallMilliseconds = _primed && _lastCallbackUsec >= _firstCallbackUsec
            ? (_lastCallbackUsec - _firstCallbackUsec) / 1000d
            : 0d;
        bool completeSampleSet = _sampleCount > 0 && _overflowCount == 0 &&
            _clockRegressionCount == 0;
        return $"PROLOGUE_REALTIME_FRAME_STATS outcome={outcome} " +
            $"basis=process_callback_wall_clock percentile_method=nearest_rank " +
            $"warmup_required={WarmupIntervalCount} warmup_discarded={_warmupDiscarded} " +
            $"samples={_sampleCount} " +
            FormattableString.Invariant($"callback_window_wall_ms={totalWallMilliseconds:0.000} sampled_wall_ms={_sampledUsec / 1000d:0.000} ") +
            $"p50_ms={Percentile(.50d)} p95_ms={Percentile(.95d)} " +
            $"p99_ms={Percentile(.99d)} max_ms={Percentile(1d)} " +
            $"over_33_3_ms={_over33Milliseconds} over_50_ms={_over50Milliseconds} " +
            $"overflow_discarded={_overflowCount} clock_regressions={_clockRegressionCount} " +
            $"complete_sample_set={completeSampleSet}";
    }

    private string Percentile(double fraction)
    {
        if (_sampleCount == 0) return "unavailable";
        int index = Math.Clamp((int)Math.Ceiling(fraction * _sampleCount) - 1,
            0, _sampleCount - 1);
        return _milliseconds[index].ToString("0.000", CultureInfo.InvariantCulture);
    }
}
