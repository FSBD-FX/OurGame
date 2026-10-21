using System;

namespace Knight.Mechanisms;

public enum CanyonSteamDiverterPhase { Safe, EarlyWarning, Warning, Active }

// Phase-local time is independent of the reused jet's rounded float clock.
// Each callback exposes at most one boundary and discards its overshoot.
public sealed class CanyonSteamDiverterClock
{
    public bool IsConfigured { get; private set; }
    public CanyonSteamDiverterPhase Phase { get; private set; }
    public double PhaseElapsedSeconds { get; private set; }
    public float SampleSeconds { get; private set; }
    private readonly double[] _durations = new double[4];
    private readonly float[] _first = new float[4];
    private readonly float[] _last = new float[4];
    private float _safe, _warning, _active;
    private double _elapsedCompensation;
    private bool _retired;

    public bool TryConfigure(float safe, float warning, float active)
    {
        IsConfigured = false;
        Reset();
        if (_retired || !ValidDuration(safe) || !ValidDuration(warning) || !ValidDuration(active)) return false;
        _safe = MathF.Max(.01f, safe);
        _warning = MathF.Max(.01f, warning);
        _active = MathF.Max(.01f, active);
        float early = _safe * .75f;
        float activeStart = _safe + _warning;
        float cycle = activeStart + _active;
        if (!float.IsFinite(cycle) || early <= 0f || early >= _safe ||
            activeStart <= _safe || cycle <= activeStart) return false;

        // Rounded safe+warning can still evaluate as warning (default 1.4f).
        // Verify the next representable value rather than inventing an epsilon.
        if (!CanyonSteamJet.Evaluate(activeStart, _safe, _warning, _active).HazardActive)
            activeStart = MathF.BitIncrement(activeStart);
        if (activeStart >= cycle) return false;
        _first[0] = 0f; _first[1] = early; _first[2] = _safe; _first[3] = activeStart;
        _last[0] = MathF.BitDecrement(early);
        _last[1] = MathF.BitDecrement(_safe);
        _last[2] = MathF.BitDecrement(activeStart);
        _last[3] = MathF.BitDecrement(cycle);
        _durations[0] = early;
        _durations[1] = (double)_safe - early;
        _durations[2] = _warning;
        _durations[3] = _active;
        for (int index = 0; index < 4; index++)
            if (_last[index] < _first[index] ||
                !Matches((CanyonSteamDiverterPhase)index, _first[index]) ||
                !Matches((CanyonSteamDiverterPhase)index, _last[index])) return false;
        IsConfigured = true;
        return true;
    }

    public void Step(double delta, bool paused = false)
    {
        if (!IsConfigured || paused || !double.IsFinite(delta) || delta <= 0d) return;
        int index = (int)Phase;
        double duration = _durations[index];
        // Compare before addition so even double.MaxValue cannot overflow.
        if (delta >= duration - PhaseElapsedSeconds) { EnterNextPhase(); return; }
        // Compensation retains sub-ULP progress without allowing float sampling
        // precision to stall the phase's independent time authority.
        double corrected = delta - _elapsedCompensation;
        double elapsed = PhaseElapsedSeconds + corrected;
        _elapsedCompensation = (elapsed - PhaseElapsedSeconds) - corrected;
        if (elapsed >= duration) { EnterNextPhase(); return; }
        PhaseElapsedSeconds = elapsed;
        double fraction = elapsed / duration;
        SampleSeconds = Math.Clamp((float)(_first[index] +
            ((double)_last[index] - _first[index]) * fraction), _first[index], _last[index]);
        if (!Matches(Phase, SampleSeconds)) { IsConfigured = false; Reset(); }
    }

    public void Reset()
    {
        Phase = CanyonSteamDiverterPhase.Safe;
        PhaseElapsedSeconds = 0d;
        _elapsedCompensation = 0d;
        SampleSeconds = 0f;
    }

    public void Retire() { _retired = true; IsConfigured = false; Reset(); }

    private void EnterNextPhase()
    {
        Phase = (CanyonSteamDiverterPhase)(((int)Phase + 1) % 4);
        PhaseElapsedSeconds = 0d;
        _elapsedCompensation = 0d;
        SampleSeconds = _first[(int)Phase];
    }

    private static bool ValidDuration(float value) => float.IsFinite(value) && value >= 0f;
    private bool Matches(CanyonSteamDiverterPhase phase, float sample)
    {
        CanyonMechanismFrame frame = CanyonSteamJet.Evaluate(sample, _safe, _warning, _active);
        return phase switch
        {
            CanyonSteamDiverterPhase.Safe => !frame.Telegraphing && !frame.HazardActive && frame.Traversable,
            CanyonSteamDiverterPhase.EarlyWarning => sample < _safe && frame.Telegraphing && !frame.HazardActive && frame.Traversable,
            CanyonSteamDiverterPhase.Warning => sample >= _safe && frame.Telegraphing && !frame.HazardActive && frame.Traversable,
            CanyonSteamDiverterPhase.Active => !frame.Telegraphing && frame.HazardActive && !frame.Traversable,
            _ => false
        };
    }
}
