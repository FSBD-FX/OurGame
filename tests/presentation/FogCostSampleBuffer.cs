using System;

namespace Knight.Tests.Presentation;

// Test-only numeric evidence. No clocks, Godot objects, logging or IO in this collector.
internal readonly record struct FogCostSample(ulong BeforeUsec, ulong AfterUsec,
    ulong PostUsec, ulong BeforeProcess, ulong AfterProcess, ulong CountBefore,
    ulong CountAfter, bool PresentationExpected, bool FreshSharedSnapshot,
    bool ConsumersValid, bool GameplayValid, bool Focused, double RenderCpuMs,
    double RenderGpuMs, double SetupCpuMs, long DrawCalls, float ActorX, float ActorY,
    int Health, int Occluders, ulong ConsumerCounts, ulong Physics = 0,
    ulong SupportId = 0, int Recoveries = 0, ulong MountedMask = 0,
    FogAllocationWindow Allocation = default);

internal sealed class FogCostSampleBuffer
{
    public const int WarmupRequired = 120;
    public const int TargetSamples = 600;
    private readonly FogCostSample[] _raw = new FogCostSample[8192];
    private readonly double[] _intervals = new double[TargetSamples];
    private readonly double[] _brackets = new double[TargetSamples];
    public int RawCount { get; private set; }
    public int WarmupCount { get; private set; }
    public int ValidCount { get; private set; }
    public int InvalidCount { get; private set; }
    public int OverflowCount { get; private set; }
    public bool Complete => ValidCount == TargetSamples && OverflowCount == 0;
    private ulong _lastPost;
    private bool _primed;
    public ReadOnlySpan<FogCostSample> Raw => _raw.AsSpan(0, RawCount);
    public ReadOnlySpan<double> Intervals => _intervals.AsSpan(0, ValidCount);
    public ReadOnlySpan<double> Brackets => _brackets.AsSpan(0, ValidCount);

    public static bool Admissible(FogCostSample sample) =>
        sample.BeforeProcess == sample.AfterProcess &&
        sample.AfterUsec >= sample.BeforeUsec && sample.PostUsec >= sample.AfterUsec &&
        sample.ConsumersValid && sample.GameplayValid &&
        double.IsFinite(sample.RenderCpuMs) && sample.RenderCpuMs >= 0d &&
        double.IsFinite(sample.RenderGpuMs) && sample.RenderGpuMs >= 0d &&
        double.IsFinite(sample.SetupCpuMs) && sample.SetupCpuMs >= 0d &&
        (sample.PresentationExpected
            ? sample.CountBefore < ulong.MaxValue && sample.CountAfter == sample.CountBefore + 1 &&
              sample.FreshSharedSnapshot
            : sample.CountAfter == sample.CountBefore);

    public void Observe(FogCostSample sample)
    {
        if (RawCount == _raw.Length) { OverflowCount++; return; }
        _raw[RawCount++] = sample;
        bool clockValid = !_primed || sample.PostUsec > _lastPost;
        ulong interval = _primed && clockValid ? sample.PostUsec - _lastPost : 0;
        _lastPost = sample.PostUsec;
        if (!_primed) { _primed = true; return; }
        if (!clockValid || !Admissible(sample)) { InvalidCount++; return; }
        if (WarmupCount < WarmupRequired) { WarmupCount++; return; }
        if (ValidCount == TargetSamples) { OverflowCount++; return; }
        _intervals[ValidCount] = interval / 1000d;
        _brackets[ValidCount++] = (sample.AfterUsec - sample.BeforeUsec) / 1000d;
    }
}
