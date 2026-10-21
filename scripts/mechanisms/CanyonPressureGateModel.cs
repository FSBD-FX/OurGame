namespace Knight.Mechanisms;

public enum CanyonPressureGatePhase
{
    Closed,
    Open,
    Grace,
    ClosingWarning
}

public sealed class CanyonPressureGateModel
{
    public double OpenGraceSeconds { get; set; } = 1.40d;
    public double ClosingWarningSeconds { get; set; } = 0.55d;
    public CanyonPressureGatePhase Phase { get; private set; } = CanyonPressureGatePhase.Closed;
    public double PhaseElapsed { get; private set; }
    public bool WantsOpen => Phase != CanyonPressureGatePhase.Closed;
    public bool HasOpened { get; private set; }

    public void Step(bool platePressed, bool holdOccupied, double delta)
    {
        if (platePressed || holdOccupied)
        {
            Phase = CanyonPressureGatePhase.Open;
            PhaseElapsed = 0d;
            return;
        }
        if (Phase == CanyonPressureGatePhase.Open)
        {
            Phase = CanyonPressureGatePhase.Grace;
            PhaseElapsed = 0d;
        }
        if (!double.IsFinite(delta) || delta < 0d ||
            !double.IsFinite(OpenGraceSeconds) || OpenGraceSeconds <= 0d ||
            !double.IsFinite(ClosingWarningSeconds) || ClosingWarningSeconds <= 0d)
            return;
        switch (Phase)
        {
            case CanyonPressureGatePhase.Grace:
                PhaseElapsed += delta;
                if (PhaseElapsed >= OpenGraceSeconds)
                {
                    Phase = CanyonPressureGatePhase.ClosingWarning;
                    PhaseElapsed = 0d;
                }
                break;
            case CanyonPressureGatePhase.ClosingWarning:
                PhaseElapsed += delta;
                if (PhaseElapsed >= ClosingWarningSeconds)
                {
                    Phase = CanyonPressureGatePhase.Closed;
                    PhaseElapsed = 0d;
                }
                break;
        }
    }

    public void MarkOpened()
    {
        if (WantsOpen) HasOpened = true;
    }

    public void Reset()
    {
        Phase = CanyonPressureGatePhase.Closed;
        PhaseElapsed = 0d;
        HasOpened = false;
    }
}
