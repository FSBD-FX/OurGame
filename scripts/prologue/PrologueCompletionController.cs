using System;
using Godot;
using Knight.Combat;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue.Seamless;
using Knight.Settings;

namespace Knight.Prologue;

public partial class PrologueCompletionController : Node
{
    [Signal] public delegate void PrologueCompletedEventHandler();
    [Signal] public delegate void DevelopmentSampleEndedEventHandler();

    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath HudPath { get; set; } = new("../Hud");
    [Export] public bool BasinQualitySampleMode { get; set; }

    public PrologueObjective CurrentObjective => _model.Current;
    public int CompletionCount { get; private set; }
    public bool SampleEndingReached { get; private set; }

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
    private MechanicalClawController _claw;
    private bool _attackStartedWhileCurrent;
    private bool _clawAttachedWhileCurrent;
    private bool _shieldRisingEdgeWhileCurrent;
    private bool _completeMappedLegacyBossResult;
    private ulong _startedAtMilliseconds;
    private int _deaths;
    private GameLanguage _language = GameLanguage.ChineseSimplified;

    public override void _Ready()
    {
        _level = GetNode<PrologueLevelController>(LevelPath);
        _hud = GetNode<PrologueHud>(HudPath);
        _startedAtMilliseconds = Time.GetTicksMsec();
        if (!BasinQualitySampleMode)
        {
            _level.RecoveryRequested += OnRecoveryRequested;
            EnsureAttackObservation();
            EnsureClawObservation();
            EnsureShieldObservation();
            _model.Observe(PrologueProgressEvent.Woke);
        }
        Display();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (BasinQualitySampleMode) { ObserveSampleExit(); return; }
        if (_model.Current == PrologueObjective.Complete) { ObserveNormalExit(); return; }
        EnsureAttackObservation();
        EnsureClawObservation();
        EnsureShieldObservation();
        PrologueSector sector = _level.CurrentSectorRoot;
        if (!GodotObject.IsInstanceValid(sector)) return;

        PrologueProgressEvent? observed = _model.Current switch
        {
            PrologueObjective.LearnAttackMode when AttackLessonObserved() =>
                PrologueProgressEvent.AttackModeLearned,
            PrologueObjective.LearnGrappleMode when _clawAttachedWhileCurrent =>
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
        _clawAttachedWhileCurrent = false;
        _shieldRisingEdgeWhileCurrent = false;
        Display();
        if (_model.Current == PrologueObjective.Complete) ObserveNormalExit();
    }

    public void ResetSession()
    {
        CompletionCount = 0;
        SampleEndingReached = false;
        _completeMappedLegacyBossResult = false;
        _deaths = 0;
        _startedAtMilliseconds = Time.GetTicksMsec();
        _model.Reset();
        _attackStartedWhileCurrent = false;
        _clawAttachedWhileCurrent = false;
        _shieldRisingEdgeWhileCurrent = false;
        if (!BasinQualitySampleMode) _model.Observe(PrologueProgressEvent.Woke);
        _hud?.ResetForNewGame();
        Display();
    }

    public void RestoreSession(PrologueObjective objective, bool completeMappedLegacyBossResult = false)
    {
        ResetSession();
        if (BasinQualitySampleMode) return;
        int target = Array.IndexOf(Enum.GetValues<PrologueObjective>(), objective);
        if (target < 1) return;
        _completeMappedLegacyBossResult = completeMappedLegacyBossResult;
        // ResetSession already consumed Woke (edge zero). Replay only the explicitly saved
        // chronological prefix; each Observe still advances one edge.
        for (int index = 1; index < target && index < OrderedEvents.Length; index++)
            _model.Observe(OrderedEvents[index]);
        Display();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_level) && !BasinQualitySampleMode)
            _level.RecoveryRequested -= OnRecoveryRequested;
        if (IsInstanceValid(_combat)) _combat.AttackStarted -= OnAttackStarted;
        if (IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClawFrameChanged;
        if (IsInstanceValid(_shield)) _shield.ShieldFrameChanged -= OnShieldFrameChanged;
    }

    public void SetLanguage(GameLanguage language)
    {
        _language = Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        Display();
    }

    public void RefreshDisplay() => Display();

    private void ObserveSampleExit()
    {
        if (SampleEndingReached || _level.CurrentSector != PrologueSectorId.LostTemple ||
            !_level.Progress.BossDefeated ||
            _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State != PrologueGameFlowState.Playing ||
            !_level.Player.IsPhysicsProcessing() || !_level.Player.IsOnFloor()) return;
        PrologueSector sector = _level.CurrentSectorRoot;
        var door = sector?.GetNodeOrNull<CollisionShape2D>("Gameplay/Doors/Exit/Collision");
        var floor = sector?.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/ArenaFloor");
        if (door?.Shape is not RectangleShape2D rectangle || !door.Disabled || floor == null ||
            door.ToLocal(_level.Player.GlobalPosition).X <= rectangle.Size.X * .5f) return;
        bool supported = false;
        for (int i = 0; i < _level.Player.GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = _level.Player.GetSlideCollision(i);
            if (collision.GetCollider() == floor && collision.GetNormal().Dot(Vector2.Up) > .7f)
                supported = true;
        }
        if (!supported) return;
        SampleEndingReached = true;
        EmitSignal(SignalName.DevelopmentSampleEnded);
    }

    private void ObserveNormalExit()
    {
        if (CompletionCount > 0 || _model.Current != PrologueObjective.Complete ||
            _level.CurrentSector != PrologueSectorId.LostTemple || !_level.Progress.BossDefeated ||
            _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State != PrologueGameFlowState.Playing)
            return;
        // Preserve only the explicitly selected legacy Boss-result migration contract.
        // Fresh and current-layout restores leave this session-local exception false.
        if (_completeMappedLegacyBossResult) { Complete(); return; }
        CharacterBody2D player = _level.Player;
        if (!GodotObject.IsInstanceValid(player) || !player.IsPhysicsProcessing() || !player.IsOnFloor() ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0) return;
        PrologueSector sector = _level.CurrentSectorRoot;
        var body = player.GetNodeOrNull<CollisionShape2D>("BodyCollider");
        var door = sector?.GetNodeOrNull<CollisionShape2D>("Gameplay/Doors/Exit/Collision");
        var floor = sector?.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/ArenaFloor");
        var floorShape = floor?.GetNodeOrNull<CollisionShape2D>("Collision");
        if (body?.Shape is not CapsuleShape2D capsule || body.Disabled ||
            door?.Shape is not RectangleShape2D doorRectangle || !door.Disabled ||
            floorShape?.Shape is not RectangleShape2D floorRectangle || floorShape.Disabled) return;

        // Project the actual transformed capsule (circle plus axial segment), rather
        // than accepting its center or assuming a test radius/world coordinate.
        float HalfWidth(Transform2D transform) => capsule.Radius *
            new Vector2(transform.X.X, transform.Y.X).Length() +
            Mathf.Max(0f, capsule.Height * .5f - capsule.Radius) * Mathf.Abs(transform.Y.X);
        Transform2D inDoor = door.GlobalTransform.AffineInverse() * body.GlobalTransform;
        if (inDoor.Origin.X - HalfWidth(inDoor) <= doorRectangle.Size.X * .5f) return;
        Transform2D onFloor = floorShape.GlobalTransform.AffineInverse() * body.GlobalTransform;
        float halfWidth = HalfWidth(onFloor), floorHalfWidth = floorRectangle.Size.X * .5f;
        if (onFloor.Origin.X - halfWidth < -floorHalfWidth ||
            onFloor.Origin.X + halfWidth > floorHalfWidth) return;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(index);
            if (collision.GetCollider() == floor && collision.GetNormal().Dot(Vector2.Up) > .7f)
            {
                Complete();
                return;
            }
        }
    }

    private bool AttackLessonObserved() => _attackStartedWhileCurrent;

    private void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        // Aim/Windup and a locked travel cast can still fail. Only an accepted
        // attachment to the actual live target teaches a successful grab.
        if (_model.Current == PrologueObjective.LearnGrappleMode &&
            frame.State is (MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing) &&
            GodotObject.IsInstanceValid(target) && target.IsAvailable &&
            frame.LockedTargetRuntimeId == target.GetInstanceId())
            _clawAttachedWhileCurrent = true;
    }

    private void EnsureClawObservation()
    {
        MechanicalClawController next = _level?.Player?.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        if (ReferenceEquals(next, _claw) && GodotObject.IsInstanceValid(_claw)) return;
        if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClawFrameChanged;
        _clawAttachedWhileCurrent = false;
        _claw = next;
        if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged += OnClawFrameChanged;
    }

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
        _attackStartedWhileCurrent = false;
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
        if (_level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.TryComplete() != true) return;
        CompletionCount++;
        _hud.HideBoss();
        Display();
        TimeSpan elapsed = TimeSpan.FromMilliseconds(Time.GetTicksMsec() - _startedAtMilliseconds);
        _hud.ShowCompletion($"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}",
            _deaths, _level.Progress.HiddenAlcoveVisited, _level.Progress.BellChainShortcutOpen);
        EmitSignal(SignalName.PrologueCompleted);
    }

    internal void RetryPendingCompletion()
    {
        // The failed transaction already qualified the exit (or mapped legacy
        // result). Its actor is now frozen; never require it to walk out again.
        if (_level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State ==
            PrologueGameFlowState.CompletionSaveFailed) Complete();
    }

    private void OnRecoveryRequested(string reason)
    {
        _attackStartedWhileCurrent = false;
        _clawAttachedWhileCurrent = false;
        _shieldRisingEdgeWhileCurrent = false;
        if (!string.IsNullOrWhiteSpace(reason) &&
            (reason.Contains("DEATH", StringComparison.OrdinalIgnoreCase) ||
             reason.Contains("FALL", StringComparison.OrdinalIgnoreCase))) _deaths++;
    }

    private void Display() => _hud.SetObjective(BasinQualitySampleMode
        ? _language == GameLanguage.English
            ? "DEV SAMPLE: CAMP > BATTLE > GRAPPLE SUPPLY > RETURN (IN DEVELOPMENT)"
            : "开发样片：营地 > 战斗 > 机械爪补给 > 返回（开发中）"
        : _model.Current == PrologueObjective.Complete && CompletionCount == 0
            ? _language == GameLanguage.English
                ? "BELLKEEPER DEFEATED — WALK THROUGH THE OPEN EXIT"
                : "钟守已倒下 · 沿地面穿过已开启的出口"
            : PrologueObjectiveModel.TextFor(_model.Current));

}
