namespace Knight.Mechanisms;

public enum CanyonSteamDiverterSide
{
    Left,
    Right
}

public sealed class CanyonSteamDiverterModel
{
    public CanyonSteamDiverterSide SelectedSide { get; private set; } = CanyonSteamDiverterSide.Right;
    public bool IsHandingOff { get; private set; }
    public bool LeftCanCycle => !IsRetired && !IsHandingOff && SelectedSide == CanyonSteamDiverterSide.Left;
    public bool RightCanCycle => !IsRetired && !IsHandingOff && SelectedSide == CanyonSteamDiverterSide.Right;
    public bool IsRetired { get; private set; }
    private double _handoffElapsed;

    public bool TrySwitch()
    {
        if (IsRetired || IsHandingOff) return false;
        SelectedSide = SelectedSide == CanyonSteamDiverterSide.Right
            ? CanyonSteamDiverterSide.Left : CanyonSteamDiverterSide.Right;
        IsHandingOff = true;
        _handoffElapsed = 0d;
        return true;
    }

    public void Step(double delta, bool paused = false)
    {
        if (IsRetired || paused || !IsHandingOff || !double.IsFinite(delta) || delta <= 0d) return;
        _handoffElapsed += delta;
        if (_handoffElapsed >= .60d) IsHandingOff = false;
    }

    public void Reset()
    {
        if (IsRetired) return;
        SelectedSide = CanyonSteamDiverterSide.Right;
        IsHandingOff = false;
        _handoffElapsed = 0d;
    }

    public void Retire()
    {
        IsRetired = true;
        IsHandingOff = false;
        _handoffElapsed = 0d;
    }
}
