using Godot;
using Knight.Prologue;

namespace Knight.Mechanisms;

// Owns both child clocks. Nonselected and handoff jets remain disarmed.
public partial class CanyonSteamDiverter : Node2D, IThreadInteractionReceiver,
    ICheckpointResettable, ICanyonTelegraphSource
{
    [Export] public string MechanismId { get; set; } = string.Empty;
    public bool HasActivated { get; private set; }
    [Export] public NodePath LeftJetPath { get; set; } = new("LeftJet");
    [Export] public NodePath RightJetPath { get; set; } = new("RightJet");
    public CanyonSteamDiverterSide SelectedSide => _model.SelectedSide;
    public bool IsHandingOff => _model.IsHandingOff;
    public bool IsDangerTelegraphActive => _model.IsHandingOff ||
        (GodotObject.IsInstanceValid(_left) && _left.Frame.Telegraphing) ||
        (GodotObject.IsInstanceValid(_right) && _right.Frame.Telegraphing);
    private readonly CanyonSteamDiverterModel _model = new();
    private readonly CanyonSteamDiverterClock _leftClock = new();
    private readonly CanyonSteamDiverterClock _rightClock = new();
    private CanyonSteamJet _left;
    private CanyonSteamJet _right;
    private bool _configurationValid;
    private ulong _latchPhysicsFrame;

    public override void _Ready()
    {
        _left = GetNode<CanyonSteamJet>(LeftJetPath);
        _right = GetNode<CanyonSteamJet>(RightJetPath);
        _left.SetPhysicsProcess(false);
        _right.SetPhysicsProcess(false);
        ResetForCheckpoint();
    }

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if (!CanOperate || !_model.TrySwitch()) return false;
        // Desired side has flipped; disarm the outgoing side first, including
        // its logical Frame, before touching the already harmless incoming side.
        if (SelectedSide == CanyonSteamDiverterSide.Left) { _right.DisableForRun(); _left.DisableForRun(); }
        else { _left.DisableForRun(); _right.DisableForRun(); }
        ResetClocksAndJetElapsed();
        _latchPhysicsFrame = Engine.GetPhysicsFrames();
        HasActivated = true;
        return true;
    }

    public override void _PhysicsProcess(double delta) => Advance(delta);
    public void Advance(double delta)
    {
        if (!CanOperate || GetTree().Paused || !double.IsFinite(delta) || delta <= 0d) return;
        if (_model.IsHandingOff)
        {
            if (Engine.GetPhysicsFrames() == _latchPhysicsFrame) return;
            _model.Step(delta);
            if (!_model.IsHandingOff) SelectedJet.EnableForNewRun();
            return; // Never consume the handoff callback's remainder as jet time.
        }
        CanyonSteamDiverterClock clock = SelectedClock;
        CanyonSteamJet jet = SelectedJet;
        clock.Step(delta);
        if (!clock.IsConfigured) { DisarmBoth(); _configurationValid = false; return; }
        if (clock.SampleSeconds == 0f && jet.ElapsedSeconds > 0f) jet.EnableForNewRun();
        else if (clock.SampleSeconds > jet.ElapsedSeconds)
            jet.AdvanceRuntime(clock.SampleSeconds - jet.ElapsedSeconds);
        // Fail closed if float subtraction/addition cannot publish the validated
        // phase sample through the unchanged jet API.
        bool active = clock.Phase == CanyonSteamDiverterPhase.Active;
        bool warning = clock.Phase is CanyonSteamDiverterPhase.EarlyWarning or CanyonSteamDiverterPhase.Warning;
        if (jet.Frame.HazardActive != active || jet.Frame.Telegraphing != warning || jet.Frame.Traversable == active)
        { DisarmBoth(); _configurationValid = false; }
    }

    public void ResetForCheckpoint()
    {
        HasActivated = false;
        DisarmBoth();
        _configurationValid = false;
        if (!JetsAvailable || _model.IsRetired) return;
        ResetClocksAndJetElapsed();
        _model.Reset();
        _latchPhysicsFrame = 0;
        bool leftValid = _leftClock.TryConfigure(_left.SafeSeconds, _left.TelegraphSeconds, _left.ActiveSeconds);
        bool rightValid = _rightClock.TryConfigure(_right.SafeSeconds, _right.TelegraphSeconds, _right.ActiveSeconds);
        _configurationValid = leftValid && rightValid;
        if (_configurationValid) _right.EnableForNewRun();
    }

    public override void _ExitTree() => RetireBindings();

    private void RetireBindings()
    {
        _model.Retire();
        _leftClock.Retire();
        _rightClock.Retire();
        DisarmBoth();
        _configurationValid = false;
        _left = null;
        _right = null;
    }

    private bool JetsAvailable => GodotObject.IsInstanceValid(_left) && GodotObject.IsInstanceValid(_right) &&
        !_left.IsQueuedForDeletion() && !_right.IsQueuedForDeletion() && _left.IsInsideTree() && _right.IsInsideTree();
    private bool CanOperate
    {
        get
        {
            // A missing original child leaves no independent clock to finish
            // its live sibling's hazard. Retire the pair rather than freezing
            // that armed state; never look up an automatic replacement.
            if (IsNodeReady() && !_model.IsRetired && !JetsAvailable) RetireBindings();
            return !_model.IsRetired && !IsQueuedForDeletion() && IsInsideTree() &&
                JetsAvailable && _configurationValid;
        }
    }
    private CanyonSteamJet SelectedJet => SelectedSide == CanyonSteamDiverterSide.Left ? _left : _right;
    private CanyonSteamDiverterClock SelectedClock => SelectedSide == CanyonSteamDiverterSide.Left ? _leftClock : _rightClock;

    private void DisarmBoth()
    {
        if (GodotObject.IsInstanceValid(_left)) _left.DisableForRun();
        if (GodotObject.IsInstanceValid(_right)) _right.DisableForRun();
    }

    private void ResetClocksAndJetElapsed()
    {
        _leftClock.Reset(); _rightClock.Reset();
        _left.ResetForCheckpoint(); _right.ResetForCheckpoint();
    }
}
