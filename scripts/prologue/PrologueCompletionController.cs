using System;
using Godot;
using Knight.Player;
using Knight.Prologue.Seamless;

namespace Knight.Prologue;

public partial class PrologueCompletionController : Node
{
    [Signal] public delegate void PrologueCompletedEventHandler();

    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath HudPath { get; set; } = new("../Hud");

    public PrologueObjective CurrentObjective => _model.Current;
    public int CompletionCount { get; private set; }

    private static readonly PrologueProgressEvent[] OrderedEvents =
    [
        PrologueProgressEvent.Woke,
        PrologueProgressEvent.AttackModeLearned,
        PrologueProgressEvent.GrappleModeLearned,
        PrologueProgressEvent.BasinReached,
        PrologueProgressEvent.ShieldLearned,
        PrologueProgressEvent.CanyonClimbed,
        PrologueProgressEvent.TempleEntered,
        PrologueProgressEvent.TempleMechanismPulled,
        PrologueProgressEvent.BossDefeated
    ];

    private readonly PrologueObjectiveModel _model = new();
    private PrologueLevelController _level;
    private PrologueHud _hud;
    private PlayerShieldController _shield;
    private PlayerCombat _combat;
    private bool _attackStartedWhileCurrent;
    private bool _shieldRisingEdgeWhileCurrent;
    private ulong _startedAtMilliseconds;
    private int _deaths;

    public override void _Ready()
    {
        _level = GetNode<PrologueLevelController>(LevelPath);
        _hud = GetNode<PrologueHud>(HudPath);
        _startedAtMilliseconds = Time.GetTicksMsec();
        _level.RecoveryRequested += OnRecoveryRequested;
        EnsureAttackObservation();
        EnsureShieldObservation();
        _model.Observe(PrologueProgressEvent.Woke);
        Display();
    }

    public override void _PhysicsProcess(double delta)
    {
        EnsureAttackObservation();
        EnsureShieldObservation();
        PrologueSector sector = _level.CurrentSectorRoot;
        if (!GodotObject.IsInstanceValid(sector)) return;

        PrologueProgressEvent? observed = _model.Current switch
        {
            PrologueObjective.LearnAttackMode when AttackLessonObserved() =>
                PrologueProgressEvent.AttackModeLearned,
            PrologueObjective.LearnGrappleMode when GrappleModePressed() =>
                PrologueProgressEvent.GrappleModeLearned,
            PrologueObjective.ReachBasin when _level.CurrentSector == PrologueSectorId.CanyonBasin =>
                PrologueProgressEvent.BasinReached,
            PrologueObjective.LearnShield when _shieldRisingEdgeWhileCurrent =>
                PrologueProgressEvent.ShieldLearned,
            PrologueObjective.ClimbCanyon when _level.CurrentSector == PrologueSectorId.CanyonAscent =>
                PrologueProgressEvent.CanyonClimbed,
            PrologueObjective.EnterTemple when _level.CurrentSector == PrologueSectorId.LostTemple =>
                PrologueProgressEvent.TempleEntered,
            PrologueObjective.PullTempleMechanism when _level.Progress.BellChainShortcutOpen =>
                PrologueProgressEvent.TempleMechanismPulled,
            PrologueObjective.DefeatBellkeeper when _level.Progress.BossDefeated =>
                PrologueProgressEvent.BossDefeated,
            _ => null
        };
        if (!observed.HasValue || !_model.Observe(observed.Value)) return;
        _attackStartedWhileCurrent = false;
        _shieldRisingEdgeWhileCurrent = false;
        if (_model.Current == PrologueObjective.Complete) Complete();
        else Display();
    }

    public void ResetSession()
    {
        CompletionCount = 0;
        _deaths = 0;
        _startedAtMilliseconds = Time.GetTicksMsec();
        _model.Reset();
        _attackStartedWhileCurrent = false;
        _shieldRisingEdgeWhileCurrent = false;
        _model.Observe(PrologueProgressEvent.Woke);
        _hud?.ResetForNewGame();
        Display();
    }

    public void RestoreSession(PrologueObjective objective)
    {
        ResetSession();
        int target = Array.IndexOf(Enum.GetValues<PrologueObjective>(), objective);
        if (target < 1) return;
        // ResetSession already consumed Woke (edge zero). Replay only the explicitly saved
        // chronological prefix; each Observe still advances one edge.
        for (int index = 1; index < target && index < OrderedEvents.Length; index++)
            _model.Observe(OrderedEvents[index]);
        Display();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_level)) _level.RecoveryRequested -= OnRecoveryRequested;
        if (IsInstanceValid(_combat)) _combat.AttackStarted -= OnAttackStarted;
        if (IsInstanceValid(_shield)) _shield.ShieldFrameChanged -= OnShieldFrameChanged;
    }

    private bool GrappleModePressed() =>
        _level.Player?.GetNodeOrNull<PlayerInputReader>("Input")?.Current.GrappleModePressed == true;

    private bool AttackLessonObserved() => _attackStartedWhileCurrent ||
        _level.Player?.GetNodeOrNull<PlayerInputReader>("Input")?.Current.AttackModePressed == true;

    private void OnAttackStarted(ulong _, int __)
    {
        if (_model.Current == PrologueObjective.LearnAttackMode)
            _attackStartedWhileCurrent = true;
    }

    private void EnsureAttackObservation()
    {
        PlayerCombat next = _level?.Player?.GetNodeOrNull<PlayerCombat>("Combat");
        if (ReferenceEquals(next, _combat) && GodotObject.IsInstanceValid(_combat)) return;
        if (GodotObject.IsInstanceValid(_combat)) _combat.AttackStarted -= OnAttackStarted;
        _combat = next;
        if (GodotObject.IsInstanceValid(_combat)) _combat.AttackStarted += OnAttackStarted;
    }

    private void OnShieldFrameChanged(bool active)
    {
        if (active && _model.Current == PrologueObjective.LearnShield)
            _shieldRisingEdgeWhileCurrent = true;
    }

    private void EnsureShieldObservation()
    {
        PlayerShieldController next = _level?.Player?
            .GetNodeOrNull<PlayerShieldController>("Shield");
        if (ReferenceEquals(next, _shield) && GodotObject.IsInstanceValid(_shield)) return;
        if (GodotObject.IsInstanceValid(_shield))
            _shield.ShieldFrameChanged -= OnShieldFrameChanged;
        _shield = next;
        if (GodotObject.IsInstanceValid(_shield))
            _shield.ShieldFrameChanged += OnShieldFrameChanged;
    }

    private void Complete()
    {
        if (CompletionCount > 0) return;
        CompletionCount++;
        _hud.HideBoss();
        Display();
        TimeSpan elapsed = TimeSpan.FromMilliseconds(Time.GetTicksMsec() - _startedAtMilliseconds);
        _hud.ShowCompletion($"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}",
            _deaths, _level.Progress.HiddenAlcoveVisited, _level.Progress.BellChainShortcutOpen);
        EmitSignal(SignalName.PrologueCompleted);
    }

    private void OnRecoveryRequested(string reason)
    {
        _shieldRisingEdgeWhileCurrent = false;
        if (!string.IsNullOrWhiteSpace(reason) &&
            (reason.Contains("DEATH", StringComparison.OrdinalIgnoreCase) ||
             reason.Contains("FALL", StringComparison.OrdinalIgnoreCase))) _deaths++;
    }

    private void Display() => _hud.SetObjective(PrologueObjectiveModel.TextFor(_model.Current));

}
