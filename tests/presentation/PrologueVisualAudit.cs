using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Boss;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Settings;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Presentation;

public partial class PrologueVisualAudit : SceneTree
{
    public static readonly string[] RequiredTraversalProofStages =
    [
        "single_jump",
        "air_dash_consumed",
        "second_dash_rejected",
        "upper_anchor_low",
        "upper_anchor_high",
        "upper_rebound",
        "upper_landed"
    ];

    private const int FinalFrame = 720;
    private const int PerformanceSamplingStartFrame = 30;
    private static readonly int ExpectedPerformanceSamples =
        FinalFrame - PerformanceSamplingStartFrame + 1;
    private const int CertifiedPerformanceSamples = 691;
    private const int CertifiedPerformanceRooms = 6;
    private const int CertifiedPerformanceRealProofs = 23;
    private const int CertifiedPerformanceTransitions = 7;
    private const int PerformanceRoomTransitionIntervalFrames = 40;
    private const double CertifiedPerformanceFrameCeilingMilliseconds = 50d;
    private const string Task4CaptureRoot = "res://screenshots/task4-audio-vfx";
    private const string PerformanceOnlyArgument = "--performance-only";
    private static readonly string[] RequiredTask4ProofNames =
    [
        "ordinary-telegraph", "ordinary-active", "ordinary-recovery",
        "ordinary-rust-telegraph", "ordinary-rust-active", "ordinary-rust-recovery",
        "ordinary-moth-telegraph", "ordinary-moth-active", "ordinary-moth-recovery",
        "ordinary-laborer-telegraph", "ordinary-laborer-active", "ordinary-laborer-recovery",
        "boss-sweep-telegraph", "boss-leap-telegraph", "boss-ground-wave-telegraph",
        "boss-hammer-telegraph", "thread-preview", "thread-windup", "thread-travel",
        "thread-latch", "thread-tension", "reduced-flash-active", "cleanup-after-cancel"
    ];
    private static readonly RigContract[] FormalRigContracts =
    [
        new("res://scenes/presentation/PlayerFormalVisual.tscn",
            [new("FarLeg", new(-8, 17), 0), new("FarArm", new(-12, -2), 1),
                new("Tether", new(-11, -28), 2), new("Lantern", new(-14, -42), 3),
                new("Body", Vector2.Zero, 4), new("NearLeg", new(8, 17), 5),
                new("Mantle", new(-3, -1), 6), new("ShoulderCore", new(12, -7), 7),
                new("NearArm", new(13, -2), 8), new("Weapon", new(14, 7), 9),
                new("ThreadShuttle", new(20, -16), 10)]),
        new("res://scenes/presentation/RustShellFormalVisual.tscn",
            [new("Corruption", new(-9, -3), 0, EnemyFormalPartKind.RustCorruption),
                new("Body", Vector2.Zero, 1, EnemyFormalPartKind.RustBody),
                new("Shell", new(-2, -2), 2, EnemyFormalPartKind.RustShell),
                new("Weapon", new(20, -5), 3, EnemyFormalPartKind.RustWeapon)]),
        new("res://scenes/presentation/GraveLampMothFormalVisual.tscn",
            [new("Corruption", new(-20, -3), 0, EnemyFormalPartKind.MothCorruption),
                new("Wings/FarWing", Vector2.Zero, 1, EnemyFormalPartKind.MothFarWing),
                new("Body", Vector2.Zero, 2, EnemyFormalPartKind.MothBody),
                new("Wings/NearWing", Vector2.Zero, 3, EnemyFormalPartKind.MothNearWing),
                new("Lamp", new(0, -5), 4, EnemyFormalPartKind.MothLamp)]),
        new("res://scenes/presentation/BellBackLaborerFormalVisual.tscn",
            [new("Corruption", new(-12, -5), 0, EnemyFormalPartKind.LaborerCorruption),
                new("Bell", new(-12, -12), 1, EnemyFormalPartKind.LaborerBell),
                new("Body", Vector2.Zero, 2, EnemyFormalPartKind.LaborerBody),
                new("Feet", new(0, 18), 3, EnemyFormalPartKind.LaborerFeet),
                new("Weapon", new(18, -2), 4, EnemyFormalPartKind.LaborerWeapon)]),
        new("res://scenes/presentation/FallenBellkeeperFormalVisual.tscn",
            [new("Body", Vector2.Zero, 1), new("Core", new(0, -24), 2),
                new("ArmorShell", new(-8, -15), 3), new("Chain", new(30, -44), 4),
                new("Hammer", new(34, -2), 5)])
    ];
    private static readonly PrologueRoomId[] Route =
    [
        PrologueRoomId.AwakeningCourtyard,
        PrologueRoomId.BrokenToothCorridor,
        PrologueRoomId.UpperPendulum,
        PrologueRoomId.LowerBuriedMachinery,
        PrologueRoomId.BellChainMechanism,
        PrologueRoomId.FallenBellkeeperArena
    ];

    private PrologueLevelController _level;
    private int _frame;
    private ulong _lastProcessCallbackTicksUsec;
    private double _maxFrameMilliseconds;
    private int _maxFrameAuditFrame;
    private PrologueRoomId _maxFrameRoom;
    private int _maxFrameProofCount;
    private bool _maxFrameProofComplete;
    private double _maxDrawCalls;
    private double _maxTextureMemory;
    private double _maxNodeCount;
    private int _maxParticles;
    private RoomFormalVisual _performanceRoomArt;
    private PrologueRoomId _performanceArtRoom;
    private int _lastProtectionCandidateRefreshCount;
    private int _lastGen0Collections;
    private int _lastGen1Collections;
    private int _lastGen2Collections;
    private bool _finishing;
    private RustShellSquire _proofRust;
    private GraveLampMoth _proofMoth;
    private BellBackLaborer _proofLaborer;
    private FallenBellkeeper _proofBoss;
    private RustShellSquire _proofReducedRust;
    private ThreadAbility _proofThreadAbility;
    private Node2D _proofThreadReceiver;
    private readonly Queue<(string Name, int EarliestFrame)> _runtimeProofCaptures = new();
    private readonly HashSet<string> _capturedRuntimeProofs = new(StringComparer.Ordinal);
    private readonly HashSet<string> _savedProofFrames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (Node2D Source, AttackPresentationFrame Frame)> _attackProofSources = new();
    private bool _threadInputReleased;
    private ThreadTarget2D _proofThreadTarget;
    private bool _bossTransitionRequested;
    private bool _reducedProofStarted;
    private bool _reducedProofCaptured;
    private bool _cancelProofStarted;
    private bool _cancelPauseRequested;
    private bool _cancelFailureObserved;
    private int _cancelAtFrame = -1;
    private bool _task4ProofComplete;
    private int _performanceRoomTraversalStartFrame = int.MaxValue;
    private int _performanceSampleCount;
    private int _performanceRoomTransitions;
    private readonly HashSet<PrologueRoomId> _performanceRooms = new();
    private readonly List<GpuParticles2D> _performanceParticles = new();
    private bool _performanceNodeTracking;
    private bool _performanceOnly;
    private bool _rustPlayerSeparated;

    private readonly record struct RigSlotContract(
        string Name,
        Vector2 Pivot,
        int ZIndex,
        EnemyFormalPartKind? PartKind = null);
    private readonly record struct RigContract(string ScenePath, RigSlotContract[] Slots);

    public readonly record struct AssistedTraversalProofEvidence(
        string Stage,
        ulong ExpectedTargetId,
        ulong AppliedTargetId,
        int CastCount,
        int ApplyCount,
        int ConsecutiveVisibleFrames,
        bool CleanupBeforeApply,
        bool Landed,
        bool OverlayClear);

    public static void AuditTraversalProofStageOrder(IReadOnlyList<string> actual)
    {
        if (actual.Count != RequiredTraversalProofStages.Length)
            throw new InvalidOperationException(
                $"traversal proof requires exactly {RequiredTraversalProofStages.Length} ordered stages; " +
                $"actual={actual.Count} [{string.Join(",", actual)}]");
        for (int index = 0; index < RequiredTraversalProofStages.Length; index++)
        {
            if (!string.Equals(actual[index], RequiredTraversalProofStages[index], StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"traversal proof stage order mismatch at {index}: " +
                    $"expected={RequiredTraversalProofStages[index]} actual={actual[index]}");
        }
    }

    public static void AuditTraversalProofStagePrefix(IReadOnlyList<string> actual)
    {
        if (actual.Count > RequiredTraversalProofStages.Length)
            throw new InvalidOperationException(
                $"traversal proof published too many stages; actual={actual.Count}");
        for (int index = 0; index < actual.Count; index++)
        {
            if (!string.Equals(actual[index], RequiredTraversalProofStages[index], StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"traversal proof stage order mismatch at {index}: " +
                    $"expected={RequiredTraversalProofStages[index]} actual={actual[index]}");
        }
    }

    public static void AuditDashProof(int acceptedDelta, int rejectedDelta,
        bool secondBurstObserved, int consecutivePostRejectFrames)
    {
        if (acceptedDelta != 1 || rejectedDelta < 1)
            throw new InvalidOperationException(
                $"traversal dash proof requires accepted=1 and rejected>=1; " +
                $"accepted={acceptedDelta} rejected={rejectedDelta}");
        if (secondBurstObserved || consecutivePostRejectFrames < 2)
            throw new InvalidOperationException(
                $"traversal dash proof observed a second burst or too little rendered evidence; " +
                $"secondBurst={secondBurstObserved} frames={consecutivePostRejectFrames}");
    }

    public static void AuditFailedCastProof(bool failedObserved, int visibleFrames,
        bool cleaned, bool retryReady)
    {
        if (!failedObserved || visibleFrames < 2 || !cleaned || !retryReady)
            throw new InvalidOperationException(
                $"traversal failed-cast proof is incomplete; failed={failedObserved} " +
                $"visibleFrames={visibleFrames} cleaned={cleaned} retryReady={retryReady}");
    }

    public static void AuditAssistedTraversalStage(AssistedTraversalProofEvidence evidence)
    {
        if (evidence.ExpectedTargetId == 0 || evidence.AppliedTargetId != evidence.ExpectedTargetId)
            throw new InvalidOperationException(
                $"{evidence.Stage} did not apply to its exact authored anchor; " +
                $"expected={evidence.ExpectedTargetId} actual={evidence.AppliedTargetId}");
        if (evidence.CastCount != 1 || evidence.ApplyCount != 1)
            throw new InvalidOperationException(
                $"{evidence.Stage} requires exactly one real cast/apply; " +
                $"casts={evidence.CastCount} applies={evidence.ApplyCount}");
        if (evidence.ConsecutiveVisibleFrames < 2)
            throw new InvalidOperationException(
                $"{evidence.Stage} lacks two consecutive rendered frames with exact target, line and shuttle; " +
                $"frames={evidence.ConsecutiveVisibleFrames}");
        if (evidence.CleanupBeforeApply || !evidence.Landed)
            throw new InvalidOperationException(
                $"{evidence.Stage} cleaned before Apply or lacked a valid supported landing; " +
                $"cleanupBeforeApply={evidence.CleanupBeforeApply} landed={evidence.Landed}");
        if (!evidence.OverlayClear)
            throw new InvalidOperationException(
                $"{evidence.Stage} guidance or target marker covers its launch/landing zone");
    }

    public override void _Initialize()
    {
        try
        {
            _performanceOnly = Array.IndexOf(OS.GetCmdlineUserArgs(), PerformanceOnlyArgument) >= 0;
            GD.Print($"PROLOGUE_VISUAL_AUDIT_MODE={(_performanceOnly ? "performance" : "evidence")}");
            if (_performanceOnly) StartPerformanceNodeTracking();
            PrologueProductionMode.AllowDiagnosticFallbacks = false;
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _level.GetNode<PrologueAudioDirector>("Audio").PlaybackEnabled = false;
            _level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
            Root.AddChild(_level);
            PrepareRuntimeProofActors();
            if (_performanceOnly) RebuildPerformanceParticleIndex();
            AuditTerminalLocomotionProgress();
            AuditBossSweepEntryOnly();
            AuditBossSweepAdvancesOncePerRenderedFrame();
        }
        catch (Exception exception)
        {
            StopPerformanceNodeTracking();
            AttachDetachedProofActorsForCleanup();
            if (GodotObject.IsInstanceValid(_level)) _level.QueueFree();
            GD.PushError("PROLOGUE_VISUAL_AUDIT_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        if (_finishing) return false;
        _frame++;
        try
        {
            SamplePerformance();
            ApplyTask4ProofState(_frame);
            CapturePendingRuntimeProof();
            CaptureTask4ProofFrame(_frame);
            if (_frame % 60 == 20) AuditVisibleTree();
            if (_performanceOnly && _task4ProofComplete &&
                _performanceRoomTransitions < CertifiedPerformanceTransitions &&
                _frame >= _performanceRoomTraversalStartFrame &&
                (_frame - _performanceRoomTraversalStartFrame) %
                    PerformanceRoomTransitionIntervalFrames == 30)
                AdvanceRoom();
            if (_frame == 45)
            {
                PrologueVfxDirector vfx = _level.GetNode<PrologueVfxDirector>("Vfx");
                vfx.Play(PresentationCue.AttackHit, _level.Player.GlobalPosition, Vector2.Right);
                vfx.Play(PresentationCue.ThreadHit, _level.Player.GlobalPosition, Vector2.Up);
            }
            if (_frame < FinalFrame) return false;

            _finishing = true;
            FinishSuccess();
        }
        catch (Exception exception)
        {
            StopPerformanceNodeTracking();
            GD.PushError($"PROLOGUE_VISUAL_AUDIT_FAIL frame={_frame}: {exception}");
            AttachDetachedProofActorsForCleanup();
            if (GodotObject.IsInstanceValid(_level)) _level.QueueFree();
            Quit(1);
        }
        return false;
    }

    private async void FinishSuccess()
    {
        try
        {
            AuditVisibleTree();
            AuditFormalRigContracts();
            foreach (string required in RequiredTask4ProofNames)
                if (!_capturedRuntimeProofs.Contains(required))
                    throw new InvalidOperationException($"Task 4 real runtime proof omitted {required}");
            if (RequiredTask4ProofNames.Length != CertifiedPerformanceRealProofs ||
                _capturedRuntimeProofs.Count != CertifiedPerformanceRealProofs)
                throw new InvalidOperationException(
                    $"Task 4 proof contract changed: required={RequiredTask4ProofNames.Length} " +
                    $"captured={_capturedRuntimeProofs.Count} certified={CertifiedPerformanceRealProofs}");
            if (_performanceOnly) AuditPerformanceWindow();
            else if (_performanceSampleCount != 0)
                throw new InvalidOperationException(
                    $"evidence-only process unexpectedly sampled {_performanceSampleCount} performance frames");
            StopPerformanceNodeTracking();
            AttachDetachedProofActorsForCleanup();
            _level.QueueFree();
            for (int index = 0; index < 4; index++) await ToSignal(this, SignalName.ProcessFrame);
            _level = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            for (int index = 0; index < 4; index++) await ToSignal(this, SignalName.ProcessFrame);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (_performanceOnly)
                GD.Print("PROLOGUE_VISUAL_PERFORMANCE_OK");
            else GD.Print("PROLOGUE_VISUAL_EVIDENCE_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            StopPerformanceNodeTracking();
            AttachDetachedProofActorsForCleanup();
            if (GodotObject.IsInstanceValid(_level)) _level.QueueFree();
            GD.PushError($"PROLOGUE_VISUAL_AUDIT_FAIL frame={_frame}: {exception}");
            Quit(1);
        }
    }

    private void ApplyTask4ProofState(int frame)
    {
        if (_cancelAtFrame == frame && !_cancelPauseRequested)
        {
            _cancelPauseRequested = true;
            Paused = true;
        }
        switch (frame)
        {
            case 20:
                PrewarmInitialProofActor();
                break;
            case 23:
                FinishInitialProofActorPrewarm();
                break;
            case 45:
                StartRustRuntimeProof();
                break;
        }
    }

    private void CaptureTask4ProofFrame(int frame)
    {
        string name = frame switch
        {
            _ => string.Empty
        };
        if (name.Length == 0) return;

        CaptureProofFrame(name);
    }

    private void PrepareRuntimeProofActors()
    {
        _proofRust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        _proofRust.Gravity = 0f;
        _proofRust.AttackFrameChanged += OnRustProofAttackFrameChanged;
        AddParkedProofActor(_proofRust);

        _proofMoth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        _proofMoth.Tuning = new EnemyArchetypeTuning
        {
            DetectionRange = 480f, MoveSpeed = 120f, DiveSpeed = 160f, MaxHealth = 2,
            PreferredHoverHeight = 72f, HorizontalAlignmentTolerance = 500f,
            VerticalAlignmentTolerance = 500f, TelegraphSeconds = .20f,
            AttackSeconds = .16f, RecoverySeconds = .20f, ThreadStunSeconds = .2f,
            Gravity = 0f, ContactDamage = 1
        };
        _proofMoth.AttackFrameChanged += OnMothProofAttackFrameChanged;
        AddParkedProofActor(_proofMoth);

        _proofLaborer = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
            .Instantiate<BellBackLaborer>();
        _proofLaborer.Tuning = new BellBackLaborerTuning
        {
            DetectionRange = 480f, GuardRange = 100f, MoveSpeed = 70f,
            GuardSeconds = .08f, TelegraphSeconds = .20f, AttackSeconds = .14f,
            RecoverySeconds = .24f, ExposedSeconds = .2f, Gravity = 0f, ContactDamage = 1
        };
        _proofLaborer.AttackFrameChanged += OnLaborerProofAttackFrameChanged;
        AddParkedProofActor(_proofLaborer);

        _proofBoss = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn")
            .Instantiate<FallenBellkeeper>();
        _proofBoss.Tuning = new BossTuning
        {
            MaxHealth = 12, TransitionHealthRatio = .6f, IntroSeconds = .06f,
            SweepTelegraphSeconds = .22f, SweepActiveSeconds = .10f, SweepRecoverySeconds = .10f,
            LeapSlamTelegraphSeconds = .22f, LeapSlamActiveSeconds = .10f, LeapSlamRecoverySeconds = .10f,
            GroundWaveTelegraphSeconds = .22f, GroundWaveActiveSeconds = .10f, GroundWaveRecoverySeconds = .10f,
            HammerThrowTelegraphSeconds = .22f, HammerThrowActiveSeconds = .16f, HammerThrowRecoverySeconds = .10f,
            TransitionSeconds = .08f, PhaseTwoArmoredSeconds = .08f, ExposedSeconds = .2f,
            RetryReadyDelaySeconds = .5f, MoveSpeed = 90f, ContactDamage = 1
        };
        _proofBoss.Gravity = 0f;
        _proofBoss.AttackFrameChanged += OnBossProofAttackFrameChanged;
        AddParkedProofActor(_proofBoss);

        _proofReducedRust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        _proofReducedRust.Gravity = 0f;
        _proofReducedRust.AttackFrameChanged += OnReducedRustAttackFrameChanged;
        AddParkedProofActor(_proofReducedRust);
    }

    private void AddParkedProofActor(Node2D actor)
    {
        actor.Visible = false;
        actor.ProcessMode = Node.ProcessModeEnum.Disabled;
        _level.AddChild(actor);
        _level.RemoveChild(actor);
    }

    private void PrewarmInitialProofActor()
    {
        if (!GodotObject.IsInstanceValid(_level.CurrentRoom))
            throw new InvalidOperationException("proof actors require the mounted production room");
        if (_proofRust.GetParent() == null) _level.CurrentRoom.AddChild(_proofRust);
        else if (_proofRust.GetParent() != _level.CurrentRoom) _proofRust.Reparent(_level.CurrentRoom, true);
        _proofRust.GlobalPosition = _level.Player.GlobalPosition + new Vector2(400f, -250f);
        _proofRust.Modulate = new Color(1f, 1f, 1f, 0f);
        _proofRust.Visible = true;
        _proofRust.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private void FinishInitialProofActorPrewarm()
    {
        _proofRust.ProcessMode = Node.ProcessModeEnum.Disabled;
        _proofRust.Visible = false;
        _proofRust.Modulate = Colors.White;
    }

    private void ActivateProofActor(Node2D actor, Vector2 offset)
    {
        if (actor.GetParent() == null) _level.CurrentRoom.AddChild(actor);
        else if (actor.GetParent() != _level.CurrentRoom) actor.Reparent(_level.CurrentRoom, true);
        actor.GlobalPosition = _level.Player.GlobalPosition + offset;
        actor.Visible = true;
        actor.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private void ParkProofActor(Node2D actor)
    {
        if (!GodotObject.IsInstanceValid(actor)) return;
        actor.ProcessMode = Node.ProcessModeEnum.Disabled;
        actor.Visible = false;
        actor.GetParent()?.RemoveChild(actor);
    }

    private void AttachDetachedProofActorsForCleanup()
    {
        if (!GodotObject.IsInstanceValid(_level)) return;
        AttachDetachedProofActor(_proofRust);
        AttachDetachedProofActor(_proofMoth);
        AttachDetachedProofActor(_proofLaborer);
        AttachDetachedProofActor(_proofBoss);
        AttachDetachedProofActor(_proofReducedRust);
    }

    private void AttachDetachedProofActor(Node2D actor)
    {
        if (GodotObject.IsInstanceValid(actor) && actor.GetParent() == null)
            _level.AddChild(actor);
    }

    private void StartRustRuntimeProof()
    {
        Camera2D camera = _level.Player.GetNode<Camera2D>("Camera2D");
        camera.LimitLeft = -100000;
        camera.LimitRight = 100000;
        camera.LimitTop = -100000;
        camera.LimitBottom = 100000;
        camera.Zoom = new Vector2(1.45f, 1.45f);
        _level.GetNode<Control>("Hud/Guidance").Visible = false;
        _level.GetNode<Control>("Hud/FormalHud/Prompt").Visible = false;
        _level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        ActivateProofActor(_proofRust, new Vector2(58f, 0f));
        PrintEvidence($"TASK4_REAL_PUBLISHER=RustShellSquire event=AttackFrameChanged source={_proofRust.GetInstanceId()}");
    }

    private void OnRustProofAttackFrameChanged(AttackPresentationFrame frame)
    {
        if (_frame < 45) return;
        if (frame.Timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active or AttackPhase.Recovery)
        {
            if (frame.Timeline.Phase == AttackPhase.Telegraph && !_rustPlayerSeparated)
            {
                _rustPlayerSeparated = true;
                _level.Player.GlobalPosition += Vector2.Left * 84f;
            }
            string phase = frame.Timeline.Phase.ToString().ToLowerInvariant();
            QueueAttackRuntimeProof($"ordinary-rust-{phase}", _proofRust, frame);
            if (frame.Timeline.Phase == AttackPhase.Telegraph)
                QueueAttackRuntimeProof("ordinary-telegraph", _proofRust, frame);
        }
    }

    private void StartMothRuntimeProof()
    {
        ActivateProofActor(_proofMoth, new Vector2(110f, -72f));
        PrintEvidence($"TASK4_REAL_PUBLISHER=GraveLampMoth event=AttackFrameChanged source={_proofMoth.GetInstanceId()}");
    }

    private void OnMothProofAttackFrameChanged(AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase is not (AttackPhase.Telegraph or AttackPhase.Active or AttackPhase.Recovery))
            return;
        if (frame.Timeline.Phase == AttackPhase.Telegraph &&
            (!frame.Timeline.HasLockedTarget || frame.Shape.Kind != AttackShapeKind.Capsule))
            return;
        string phase = frame.Timeline.Phase.ToString().ToLowerInvariant();
        QueueAttackRuntimeProof($"ordinary-moth-{phase}", _proofMoth, frame);
        if (frame.Timeline.Phase == AttackPhase.Active)
            QueueAttackRuntimeProof("ordinary-active", _proofMoth, frame);
    }

    private void StartLaborerRuntimeProof()
    {
        ActivateProofActor(_proofLaborer, new Vector2(72f, 0f));
        PrintEvidence($"TASK4_REAL_PUBLISHER=BellBackLaborer event=AttackFrameChanged source={_proofLaborer.GetInstanceId()}");
    }

    private void OnLaborerProofAttackFrameChanged(AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase is not (AttackPhase.Telegraph or AttackPhase.Active or AttackPhase.Recovery))
            return;
        string phase = frame.Timeline.Phase.ToString().ToLowerInvariant();
        if (frame.Timeline.Phase == AttackPhase.Recovery)
            QueueAttackRuntimeProof("ordinary-recovery", _proofLaborer, frame);
        QueueAttackRuntimeProof($"ordinary-laborer-{phase}", _proofLaborer, frame);
    }

    private void StartBossRuntimeProof()
    {
        ActivateProofActor(_proofBoss, new Vector2(150f, 0f));
        _proofBoss.ActivateEncounter();
        PrintEvidence($"TASK4_REAL_PUBLISHER=FallenBellkeeper event=AttackFrameChanged source={_proofBoss.GetInstanceId()}");
    }

    private void OnBossProofAttackFrameChanged(AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase != AttackPhase.Telegraph) return;
        string name = frame.Timeline.Kind switch
        {
            EnemyAttackKind.BossSweep => "boss-sweep-telegraph",
            EnemyAttackKind.BossLeapSlam => "boss-leap-telegraph",
            EnemyAttackKind.BossGroundWave => "boss-ground-wave-telegraph",
            EnemyAttackKind.BossHammerThrow => "boss-hammer-telegraph",
            _ => string.Empty
        };
        QueueAttackRuntimeProof(name, _proofBoss, frame);
    }

    private void StartThreadRuntimeProof()
    {
        _proofThreadAbility = _level.Player.GetNode<ThreadAbility>("ThreadAbility");
        _proofThreadAbility.CastFrameChanged += OnThreadProofCastFrameChanged;
        _proofThreadReceiver = new Node2D { Name = "Task4RuntimeThreadReceiver" };
        _proofThreadTarget = new ThreadTarget2D
        {
            Name = "ThreadTarget2D",
            Kind = ThreadTargetKind.Anchor,
            PullReceiverPath = new NodePath("..")
        };
        _proofThreadReceiver.AddChild(_proofThreadTarget);
        _level.CurrentRoom.AddChild(_proofThreadReceiver);
        _proofThreadReceiver.GlobalPosition = _level.Player.GlobalPosition + new Vector2(110f, -72f);
        _level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        Input.ActionPress(InputActions.MoveRight);
        Input.ActionPress(InputActions.MoveUp);
        Input.ActionPress(InputActions.Thread);
        PrintEvidence($"TASK4_REAL_PUBLISHER=ThreadAbility event=CastFrameChanged target=ThreadTarget2D:{_proofThreadTarget.GetInstanceId()}");
    }

    private void OnThreadProofCastFrameChanged(ThreadCastFrame frame, ThreadTarget2D target)
    {
        PrintEvidence($"TASK4_REAL_THREAD_FRAME state={frame.State} cast={frame.CastId} target={frame.TargetId}");
        string name = frame.State switch
        {
            ThreadCastState.Preview => "thread-preview",
            ThreadCastState.Windup => "thread-windup",
            ThreadCastState.Travel => "thread-travel",
            ThreadCastState.Latched => "thread-latch",
            ThreadCastState.Apply => "thread-tension",
            _ => string.Empty
        };
        QueueRuntimeProof(name);
        if (frame.State == ThreadCastState.Windup)
        {
            Input.ActionRelease(InputActions.MoveRight);
            Input.ActionRelease(InputActions.MoveUp);
        }
        if (_cancelProofStarted && frame.State == ThreadCastState.Preview && !_cancelPauseRequested)
            _cancelAtFrame = _frame + 2;
        if (_cancelProofStarted && frame.State == ThreadCastState.Failed &&
            frame.Failure == ThreadFailureReason.Cancelled)
        {
            _cancelFailureObserved = true;
            Paused = false;
            Input.ActionRelease(InputActions.Thread);
            Input.ActionRelease(InputActions.MoveRight);
            Input.ActionRelease(InputActions.MoveUp);
        }
        if (_cancelFailureObserved && frame.State == ThreadCastState.Idle)
            QueueRuntimeProof("cleanup-after-cancel");
    }

    private void QueueRuntimeProof(string name)
    {
        if (name.Length == 0 || !_capturedRuntimeProofs.Add(name)) return;
        _runtimeProofCaptures.Enqueue((name, _frame + 2));
    }

    private void QueueAttackRuntimeProof(string name, Node2D source, AttackPresentationFrame frame)
    {
        if (name.Length == 0) return;
        if (frame.Timeline.AttackId == 0 || frame.Shape.Kind == AttackShapeKind.None)
            throw new InvalidOperationException(
                $"real {source?.GetType().Name} publisher supplied an untyped proof frame {name}");
        if (!_attackProofSources.TryAdd(name, (source, frame))) return;
        PrintEvidence($"TASK4_REAL_ATTACK_FRAME name={name} source={source.GetType().Name}:{source.GetInstanceId()} " +
            $"kind={frame.Timeline.Kind} attack_id={frame.Timeline.AttackId} " +
            $"phase={frame.Timeline.Phase} shape={frame.Shape.Kind} rotation={frame.Shape.RotationRadians:F3}");
        QueueRuntimeProof(name);
    }

    private void CapturePendingRuntimeProof()
    {
        if (_runtimeProofCaptures.Count == 0) return;
        (string name, int earliestFrame) = _runtimeProofCaptures.Peek();
        if (_frame < earliestFrame) return;
        _runtimeProofCaptures.Dequeue();
        PrologueVfxDirector vfx = _level.GetNode<PrologueVfxDirector>("Vfx");
        if (_attackProofSources.TryGetValue(name, out var attackProof))
        {
            if (!GodotObject.IsInstanceValid(attackProof.Source) || !attackProof.Source.IsVisibleInTree())
                throw new InvalidOperationException($"real source actor for {name} is not visibly in the scene tree");
            Transform2D canvas = Root.GetViewport().GetCanvasTransform();
            Rect2 viewportBounds = new(Vector2.Zero, Root.GetViewport().GetVisibleRect().Size);
            Vector2 actorPoint = canvas * attackProof.Source.GlobalPosition;
            Vector2 shapePoint = canvas * attackProof.Frame.Shape.Origin;
            if (!viewportBounds.HasPoint(actorPoint) || !viewportBounds.Grow(96f).HasPoint(shapePoint))
                throw new InvalidOperationException(
                    $"real actor/shape for {name} is outside the proof camera: actor={actorPoint} shape={shapePoint}");
        }
        if (name == "ordinary-telegraph" && vfx.TypedEffectCount < 1)
            throw new InvalidOperationException("real Rust AttackFrameChanged was not drawn by typed VFX");
        if (name == "ordinary-moth-telegraph" && _attackProofSources.TryGetValue(name, out attackProof))
            AuditLockedMothRenderedOutline(vfx, attackProof.Frame);
        if (name == "thread-preview")
        {
            ThreadLineVisual line = _level.Player.GetNode<ThreadLineVisual>("ThreadLine");
            ThreadShuttleVisual shuttle = _level.Player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle");
            if (vfx.ActiveThreadEffectCount < 1 || line.ActiveStrandCount != 3 ||
                !shuttle.CandidateDiamondVisible || _proofThreadTarget == null ||
                !shuttle.LastWorldTarget.IsEqualApprox(_proofThreadTarget.GlobalPosition))
                throw new InvalidOperationException(
                    "real ThreadTarget2D publisher did not draw line/shuttle/typed target endpoint");
        }
        CaptureProofFrame(name);
        AdvanceRuntimeProofAfterCapture(name);
        if (name == "thread-preview" && !_threadInputReleased)
        {
            _threadInputReleased = true;
            Input.ActionRelease(InputActions.Thread);
        }
    }

    private void AuditLockedMothRenderedOutline(PrologueVfxDirector vfx,
        AttackPresentationFrame authoritative)
    {
        PrologueTypedVfxEffect effect = null;
        foreach (Node child in vfx.GetNode<Node2D>("World").GetChildren())
        {
            if (child is PrologueTypedVfxEffect candidate && !candidate.IsQueuedForDeletion() &&
                candidate.AttackFrame.Timeline.Kind == EnemyAttackKind.MothDive &&
                candidate.AttackFrame.Timeline.AttackId == authoritative.Timeline.AttackId)
            {
                effect = candidate;
                break;
            }
        }
        if (effect == null)
            throw new InvalidOperationException("locked Moth proof has no live typed swept-path effect");
        MethodInfo method = typeof(PrologueTypedVfxEffect).GetMethod("AttackOutline",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("locked Moth effect exposes no rendered outline");
        Vector2[] local = (Vector2[])method.Invoke(effect, null);
        Vector2[] expected = ExpectedCapsuleWorldOutline(authoritative.Shape);
        if (local.Length != expected.Length)
            throw new InvalidOperationException(
                $"Moth locked swept proof outline count differs: expected={expected.Length} actual={local.Length}");
        for (int index = 0; index < local.Length; index++)
        {
            Vector2 actual = effect.GlobalTransform * local[index];
            if (actual.DistanceTo(expected[index]) > .05f)
                throw new InvalidOperationException(
                    $"Moth locked swept proof rendered outline differs at {index}: " +
                    $"expected={expected[index]} actual={actual}");
        }
        PrintEvidence("Moth locked swept proof rendered outline equals authoritative frame");
    }

    private static Vector2[] ExpectedCapsuleWorldOutline(AttackShapeFrame shape)
    {
        const int segments = 40;
        float radius = shape.Extent.X;
        float straight = Mathf.Max(0f, shape.Extent.Y - radius);
        var expected = new Vector2[segments];
        Transform2D world = new(shape.RotationRadians, shape.Origin);
        for (int index = 0; index < segments / 2; index++)
        {
            float angle = Mathf.Pi + Mathf.Pi * index / (segments / 2 - 1f);
            expected[index] = world * new Vector2(Mathf.Cos(angle) * radius,
                -straight + Mathf.Sin(angle) * radius);
        }
        for (int index = 0; index < segments / 2; index++)
        {
            float angle = Mathf.Pi * index / (segments / 2 - 1f);
            expected[segments / 2 + index] = world * new Vector2(Mathf.Cos(angle) * radius,
                straight + Mathf.Sin(angle) * radius);
        }
        return expected;
    }

    private void AdvanceRuntimeProofAfterCapture(string name)
    {
        switch (name)
        {
            case "ordinary-rust-recovery":
                ParkProofActor(_proofRust);
                StartMothRuntimeProof();
                break;
            case "ordinary-moth-recovery":
                ParkProofActor(_proofMoth);
                StartLaborerRuntimeProof();
                break;
            case "ordinary-laborer-recovery":
                ParkProofActor(_proofLaborer);
                StartBossRuntimeProof();
                break;
            case "boss-ground-wave-telegraph" when !_bossTransitionRequested:
                _bossTransitionRequested = true;
                HealthComponent health = _proofBoss.GetNode<HealthComponent>("Health");
                health.InvulnerabilitySeconds = 0f;
                if (!health.ApplyDamage(new DamageRequest(5, Vector2.Left, 0f, 9101, 9101)))
                    throw new InvalidOperationException("real Boss proof could not trigger phase-two transition");
                break;
            case "boss-hammer-telegraph":
                ParkProofActor(_proofBoss);
                StartThreadRuntimeProof();
                break;
            case "thread-tension":
                StartReducedFlashRuntimeProof();
                break;
            case "reduced-flash-active":
                _reducedProofCaptured = true;
                ParkProofActor(_proofReducedRust);
                if (_proofThreadAbility.CurrentCastFrame.State == ThreadCastState.Idle)
                    StartThreadCancelRuntimeProof();
                break;
            case "cleanup-after-cancel":
                PrologueVfxDirector vfx = _level.GetNode<PrologueVfxDirector>("Vfx");
                ThreadLineVisual line = _level.Player.GetNode<ThreadLineVisual>("ThreadLine");
                ThreadShuttleVisual shuttle = _level.Player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle");
                if (vfx.TypedEffectCount != 0 || vfx.ActiveDangerCount != 0 ||
                    vfx.ActiveThreadEffectCount != 0 || line.ActiveStrandCount != 0 || !shuttle.IsClear)
                    throw new InvalidOperationException("real cancellation cleanup retained typed VFX/line/shuttle residue");
                _task4ProofComplete = true;
                _performanceRoomTraversalStartFrame = _frame;
                break;
        }
    }

    private void StartReducedFlashRuntimeProof()
    {
        if (_reducedProofStarted) return;
        _reducedProofStarted = true;
        _level.GetNode<PrologueVfxDirector>("Vfx").Configure(
            new GameSettingsData { ScreenShake = 1f, ReduceFlashes = true });
        ActivateProofActor(_proofReducedRust, new Vector2(58f, 0f));
        PrintEvidence($"TASK4_REAL_PUBLISHER=RustShellSquire reduced_flash event=AttackFrameChanged source={_proofReducedRust.GetInstanceId()}");
    }

    private void OnReducedRustAttackFrameChanged(AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase == AttackPhase.Active)
            QueueAttackRuntimeProof("reduced-flash-active", _proofReducedRust, frame);
    }

    private void StartThreadCancelRuntimeProof()
    {
        if (_cancelProofStarted) return;
        _cancelProofStarted = true;
        _threadInputReleased = false;
        Input.ActionPress(InputActions.MoveRight);
        Input.ActionPress(InputActions.MoveUp);
        Input.ActionPress(InputActions.Thread);
        PrintEvidence("TASK4_REAL_THREAD_CANCEL input=Input.ActionPress target=ThreadTarget2D");
    }

    private void PrintEvidence(string message)
    {
        if (!_performanceOnly) GD.Print(message);
    }

    private void CaptureProofFrame(string name)
    {
        if (!_savedProofFrames.Add(name))
            throw new InvalidOperationException($"duplicate visual-audit proof capture {name}");
        if (_performanceOnly)
        {
            GD.Print($"PROLOGUE_PERFORMANCE_EXERCISE={name} frame={_frame}");
            return;
        }
        Image image = Root.GetViewport().GetTexture().GetImage();
        if (image == null || image.IsEmpty() || image.GetWidth() != 1280 || image.GetHeight() != 720)
            throw new InvalidOperationException(
                $"Task 4 proof frame {name} must be a real 1280x720 viewport capture");
        string directory = ProjectSettings.GlobalizePath(Task4CaptureRoot);
        Error directoryError = DirAccess.MakeDirRecursiveAbsolute(directory);
        if (directoryError != Error.Ok && directoryError != Error.AlreadyExists)
            throw new InvalidOperationException($"Task 4 proof directory creation failed: {directoryError}");
        string path = $"{Task4CaptureRoot}/{name}.png";
        Error saveError = image.SavePng(ProjectSettings.GlobalizePath(path));
        if (saveError != Error.Ok)
            throw new InvalidOperationException($"Task 4 proof frame save failed: {path} ({saveError})");
        GD.Print($"TASK4_VFX_CAPTURE={path}");
        GD.Print($"PROLOGUE_VISUAL_AUDIT_CAPTURE={name}");
    }

    private void AuditVisibleTree()
    {
        IReadOnlyList<string> fallbacks = PrologueProductionAudit.FindVisibleFallbacks(_level);
        if (fallbacks.Count > 0)
            throw new InvalidOperationException("visible fallback: " + string.Join(", ", fallbacks));
        IReadOnlyList<string> invalid = PrologueProductionAudit.FindInvalidVisibleResources(_level);
        if (invalid.Count > 0)
            throw new InvalidOperationException("invalid visible resource: " + string.Join(", ", invalid));
    }

    /// <summary>
    /// The production leaves are the future-art replacement boundary.  Audit the saved scene,
    /// rather than a procedural draw call, so a missing slot, collapsed scale, or z collision
    /// cannot hide until a later art replacement.
    /// </summary>
    private static void AuditFormalRigContracts()
    {
        foreach (RigContract contract in FormalRigContracts)
        {
            Node2D root = GD.Load<PackedScene>(contract.ScenePath).Instantiate<Node2D>();
            try
            {
                AssertPositiveUniformScale(root, contract.ScenePath);
                int previousZ = int.MinValue;
                foreach (RigSlotContract slot in contract.Slots)
                {
                    Node2D node = root.GetNodeOrNull<Node2D>(slot.Name) ??
                        throw new InvalidOperationException(
                            $"formal rig {contract.ScenePath} missing stable slot {slot.Name}");
                    AssertPositiveUniformScale(node, $"{contract.ScenePath}/{slot.Name}");
                    if (node.Position != slot.Pivot)
                        throw new InvalidOperationException(
                            $"formal rig {contract.ScenePath}/{slot.Name} pivot {node.Position} " +
                            $"does not match {slot.Pivot}");
                    if (node.ZIndex != slot.ZIndex || node.ZIndex <= previousZ)
                        throw new InvalidOperationException(
                            $"formal rig {contract.ScenePath} has wrong z-order at {slot.Name}: " +
                            $"actual={node.ZIndex} expected={slot.ZIndex}");
                    if (slot.PartKind is { } expectedKind &&
                        (node is not EnemyFormalVisualPart part || part.Kind != expectedKind))
                        throw new InvalidOperationException(
                            $"formal rig {contract.ScenePath}/{slot.Name} missing real typed renderer " +
                            $"expected={expectedKind} actual={node.GetType().Name}");
                    previousZ = node.ZIndex;
                }
                if (root is GraveLampMothFormalVisual)
                {
                    Node2D wings = root.GetNodeOrNull<Node2D>("Wings") ??
                        throw new InvalidOperationException(
                            $"formal rig {contract.ScenePath} missing stable Wings anchor");
                    if (wings is EnemyFormalVisualPart || wings.Position != new Vector2(0, 1))
                        throw new InvalidOperationException(
                            $"formal rig {contract.ScenePath}/Wings must be the common translation anchor");
                    AssertPositiveUniformScale(wings, $"{contract.ScenePath}/Wings");
                }
            }
            finally { root.Free(); }
        }
    }

    private static void AssertPositiveUniformScale(Node2D node, string path)
    {
        if (node.Scale.X <= 0f || node.Scale.Y <= 0f)
            throw new InvalidOperationException($"formal rig {path} has non-positive scale {node.Scale}");
        if (!Mathf.IsEqualApprox(node.Scale.X, node.Scale.Y))
            throw new InvalidOperationException($"formal rig {path} has whole-character stretch {node.Scale}");
    }

    private static void AuditTerminalLocomotionProgress()
    {
        PlayerFormalVisual visual = GD.Load<PackedScene>(
                "res://scenes/presentation/PlayerFormalVisual.tscn")
            .Instantiate<PlayerFormalVisual>();
        try
        {
            PlayerPresentationFrame idle = MotionFrame(PlayerLocomotionState.Idle, 0f);
            PlayerPresentationFrame terminalRun = MotionFrame(PlayerLocomotionState.Run, 1f);
            visual.ApplyPlayerFrame(idle);
            visual.ApplyPlayerFrame(terminalRun);
            float first = visual.CurrentPose.Mantle.RotationRadians;
            visual.ApplyPlayerFrame(terminalRun);
            float second = visual.CurrentPose.Mantle.RotationRadians;
            float progress = Mathf.Abs(Mathf.AngleDifference(first, second));
            if (progress <= .0001f)
                throw new InvalidOperationException(
                    "terminal locomotion normalizedTime=1 froze the visible entry blend");
            if (progress > .05f)
                throw new InvalidOperationException(
                    $"terminal locomotion normalizedTime=1 advanced too far in one 60 FPS frame: {progress:F3}");
        }
        finally { visual.Free(); }
    }

    private static PlayerPresentationFrame MotionFrame(PlayerLocomotionState state, float progress) => new(
        new EntityPresentationFrame(EntityVisualState.Move, 1, true),
        new PlayerLocomotionFrame(state, progress),
        PlayerActionFrame.None,
        SecondaryMotionFrame.Identity);

    private static void AuditBossSweepEntryOnly()
    {
        PackedScene scene = GD.Load<PackedScene>(
            "res://scenes/presentation/FallenBellkeeperFormalVisual.tscn");
        FallenBellkeeperFormalVisual expected = scene.Instantiate<FallenBellkeeperFormalVisual>();
        FallenBellkeeperFormalVisual actual = scene.Instantiate<FallenBellkeeperFormalVisual>();
        try
        {
            FallenBellkeeperFrame open = new(FallenBellkeeperState.PhaseOne,
                FallenBellkeeperAttack.None, false, false, false, true);
            expected.ApplyBossFrame(open, 1);
            actual.ApplyBossFrame(open, 1);
            actual.PresentAttack(new AttackPresentationFrame(
                new AttackTimelineFrame(701, EnemyAttackKind.BossSweep, AttackPhase.Recovery,
                    1f, 0f, Vector2.Zero, false), default));
            actual.PresentAttack(default);
            AssertSameTransform(expected.GetNode<Node2D>("Hammer"), actual.GetNode<Node2D>("Hammer"),
                "Boss Sweep exit must not start the entry-only Hammer bridge");
            AssertSameTransform(expected.GetNode<Node2D>("ArmorShell"),
                actual.GetNode<Node2D>("ArmorShell"),
                "Boss Sweep exit must not start the entry-only ArmorShell bridge");
        }
        finally
        {
            expected.Free();
            actual.Free();
        }
    }

    private static void AssertSameTransform(Node2D expected, Node2D actual, string message)
    {
        float displacement = expected.Position.DistanceTo(actual.Position);
        float rotation = Mathf.Abs(Mathf.AngleDifference(expected.Rotation, actual.Rotation));
        if (displacement > .001f || rotation > .001f)
            throw new InvalidOperationException(
                $"{message}: displacement={displacement:F3} rotation={rotation:F3}");
    }

    private static void AuditBossSweepAdvancesOncePerRenderedFrame()
    {
        PackedScene scene = GD.Load<PackedScene>(
            "res://scenes/presentation/FallenBellkeeperFormalVisual.tscn");
        FallenBellkeeperFormalVisual once = scene.Instantiate<FallenBellkeeperFormalVisual>();
        FallenBellkeeperFormalVisual repeated = scene.Instantiate<FallenBellkeeperFormalVisual>();
        try
        {
            FallenBellkeeperFrame intro = new(FallenBellkeeperState.Intro,
                FallenBellkeeperAttack.None, false, false, false, true);
            once.ApplyBossFrame(intro, 1);
            repeated.ApplyBossFrame(intro, 1);
            var sweep = new AttackPresentationFrame(
                new AttackTimelineFrame(702, EnemyAttackKind.BossSweep, AttackPhase.Telegraph,
                    1f, .5f, Vector2.Zero, false), default);
            once.PresentAttack(sweep);
            repeated.PresentAttack(sweep);
            repeated.PresentAttack(sweep);
            repeated.PresentAttack(sweep);
            AssertSameTransform(once.GetNode<Node2D>("Hammer"), repeated.GetNode<Node2D>("Hammer"),
                "Boss Sweep Hammer bridge may advance only once per rendered frame");
            AssertSameTransform(once.GetNode<Node2D>("ArmorShell"),
                repeated.GetNode<Node2D>("ArmorShell"),
                "Boss Sweep ArmorShell bridge may advance only once per rendered frame");
        }
        finally
        {
            once.Free();
            repeated.Free();
        }
    }

    private void AdvanceRoom()
    {
        int current = Array.IndexOf(Route, _level.CurrentRoomId);
        if (current < 0) throw new InvalidOperationException(
            $"performance route contains no room {_level.CurrentRoomId}");
        int next = (current + 1) % Route.Length;
        RoomDefinition destination = null;
        foreach (RoomDefinition definition in _level.RoomDefinitions)
            if (definition.RoomId == Route[next]) { destination = definition; break; }
        if (destination == null) throw new InvalidOperationException($"missing room definition {Route[next]}");
        GD.Print($"PROLOGUE_PERFORMANCE_ROOM_TRANSITION frame={_frame} " +
            $"from={_level.CurrentRoomId} to={destination.RoomId}");
        _performanceRoomTransitions++;
        _level.TransitionTo(new PrologueRoomLink(_level.CurrentRoomId,
            $"audit_{current}_{next}", destination.RoomId, destination.StartEntrance));
    }

    private void SamplePerformance()
    {
        ulong callbackTicksUsec = Time.GetTicksUsec();
        ulong elapsedUsec = _lastProcessCallbackTicksUsec == 0 ||
            callbackTicksUsec < _lastProcessCallbackTicksUsec
                ? 0
                : callbackTicksUsec - _lastProcessCallbackTicksUsec;
        _lastProcessCallbackTicksUsec = callbackTicksUsec;
        if (!_performanceOnly || _frame < PerformanceSamplingStartFrame) return;
        if (!GodotObject.IsInstanceValid(_performanceRoomArt) ||
            _performanceArtRoom != _level.CurrentRoomId)
        {
            _performanceRoomArt = _level.CurrentRoom.GetNodeOrNull<RoomFormalVisual>("Art");
            _performanceArtRoom = _level.CurrentRoomId;
            _lastProtectionCandidateRefreshCount =
                _performanceRoomArt?.ProtectionCandidateRefreshCount ?? 0;
        }
        _performanceSampleCount++;
        _performanceRooms.Add(_level.CurrentRoomId);
        // Measure consecutive process callback starts directly. Performance.TimeProcess is
        // an aggregate monitor whose refresh can lag, so it cannot certify an individual
        // presentation frame. This wall clock includes the preceding callback, render/pacing,
        // and scheduler delay, making the 50 ms gate conservative and user-visible.
        double frameMilliseconds = elapsedUsec / 1000d;
        int candidateRefreshCount = _performanceRoomArt?.ProtectionCandidateRefreshCount ?? 0;
        int candidateRefreshDelta = candidateRefreshCount - _lastProtectionCandidateRefreshCount;
        int gen0Collections = GC.CollectionCount(0);
        int gen1Collections = GC.CollectionCount(1);
        int gen2Collections = GC.CollectionCount(2);
        if (frameMilliseconds > 30d)
            GD.Print($"PROLOGUE_PERFORMANCE_SPIKE frame_ms={frameMilliseconds:F2} " +
                $"audit_frame={_frame} room={_level.CurrentRoomId} " +
                $"candidate_refresh={candidateRefreshCount} candidate_refresh_delta={candidateRefreshDelta} " +
                $"gen0_delta={gen0Collections - _lastGen0Collections} " +
                $"gen1_delta={gen1Collections - _lastGen1Collections} " +
                $"gen2_delta={gen2Collections - _lastGen2Collections} " +
                $"managed_bytes={GC.GetTotalMemory(false)} ticks_ms={Time.GetTicksMsec()}");
        _lastProtectionCandidateRefreshCount = candidateRefreshCount;
        _lastGen0Collections = gen0Collections;
        _lastGen1Collections = gen1Collections;
        _lastGen2Collections = gen2Collections;
        if (frameMilliseconds > _maxFrameMilliseconds)
        {
            _maxFrameMilliseconds = frameMilliseconds;
            _maxFrameAuditFrame = _frame;
            _maxFrameRoom = _level.CurrentRoomId;
            _maxFrameProofCount = _capturedRuntimeProofs.Count;
            _maxFrameProofComplete = _task4ProofComplete;
        }
        _maxDrawCalls = Math.Max(_maxDrawCalls,
            Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame));
        _maxTextureMemory = Math.Max(_maxTextureMemory,
            Performance.GetMonitor(Performance.Monitor.RenderTextureMemUsed));
        _maxNodeCount = Math.Max(_maxNodeCount,
            Performance.GetMonitor(Performance.Monitor.ObjectNodeCount));
        _maxParticles = Math.Max(_maxParticles, CountActiveParticles());
    }

    private void AuditPerformanceWindow()
    {
        if (ExpectedPerformanceSamples != CertifiedPerformanceSamples)
            throw new InvalidOperationException(
                $"performance sample contract changed from {CertifiedPerformanceSamples} " +
                $"to {ExpectedPerformanceSamples}");
        if (Route.Length != CertifiedPerformanceRooms)
            throw new InvalidOperationException(
                $"performance room contract changed from {CertifiedPerformanceRooms} to {Route.Length}");
        if (RequiredTask4ProofNames.Length != CertifiedPerformanceRealProofs)
            throw new InvalidOperationException(
                $"performance proof contract changed from {CertifiedPerformanceRealProofs} " +
                $"to {RequiredTask4ProofNames.Length}");
        if (_performanceSampleCount != ExpectedPerformanceSamples)
            throw new InvalidOperationException(
                $"performance process sampled {_performanceSampleCount} frames; " +
                $"expected exactly {ExpectedPerformanceSamples}");
        foreach (PrologueRoomId room in Route)
            if (!_performanceRooms.Contains(room))
                throw new InvalidOperationException(
                    $"performance process omitted production room {room}");
        if (_performanceRooms.Count != CertifiedPerformanceRooms)
            throw new InvalidOperationException(
                $"performance visited {_performanceRooms.Count} rooms; " +
                $"expected exactly {CertifiedPerformanceRooms}");
        if (_capturedRuntimeProofs.Count != CertifiedPerformanceRealProofs)
            throw new InvalidOperationException(
                $"performance captured {_capturedRuntimeProofs.Count} real proofs; " +
                $"expected exactly {CertifiedPerformanceRealProofs}");
        if (_performanceRoomTransitions != CertifiedPerformanceTransitions)
            throw new InvalidOperationException(
                $"performance traversed {_performanceRoomTransitions} room boundaries; " +
                $"expected exactly {CertifiedPerformanceTransitions}");
        if (_maxFrameMilliseconds > CertifiedPerformanceFrameCeilingMilliseconds)
            throw new InvalidOperationException(
                $"runtime frame spike {_maxFrameMilliseconds:F2}ms at audit_frame={_maxFrameAuditFrame} " +
                $"room={_maxFrameRoom} proofs={_maxFrameProofCount} " +
                $"proof_complete={_maxFrameProofComplete} exceeds " +
                $"{CertifiedPerformanceFrameCeilingMilliseconds:F0}ms");
        if (_maxNodeCount > 2400d)
            throw new InvalidOperationException($"runtime node count {_maxNodeCount:F0} exceeds budget");
        if (_maxParticles > 64)
            throw new InvalidOperationException($"active particle count {_maxParticles} exceeds budget");
        if (_maxTextureMemory > 1024d * 1024d * 1024d)
            throw new InvalidOperationException($"texture memory {_maxTextureMemory:F0} exceeds 1GiB");

        GD.Print($"PROLOGUE_PERFORMANCE max_frame_ms={_maxFrameMilliseconds:F2} " +
            $"max_frame_audit_frame={_maxFrameAuditFrame} " +
            $"max_frame_room={_maxFrameRoom} " +
            $"max_frame_proofs={_maxFrameProofCount} " +
            $"max_frame_proof_complete={_maxFrameProofComplete} " +
            $"performance_samples={_performanceSampleCount} " +
            $"performance_rooms={_performanceRooms.Count} " +
            $"performance_real_proofs={_capturedRuntimeProofs.Count} " +
            $"performance_transitions={_performanceRoomTransitions} " +
            "presentation_budget_basis=process_callback_wall_clock " +
            $"max_draw_calls={_maxDrawCalls:F0} max_nodes={_maxNodeCount:F0} " +
            $"max_particles={_maxParticles} texture_memory={_maxTextureMemory:F0}");
    }

    private void StartPerformanceNodeTracking()
    {
        if (_performanceNodeTracking) return;
        NodeAdded += OnPerformanceNodeAdded;
        NodeRemoved += OnPerformanceNodeRemoved;
        _performanceNodeTracking = true;
    }

    private void StopPerformanceNodeTracking()
    {
        if (!_performanceNodeTracking) return;
        NodeAdded -= OnPerformanceNodeAdded;
        NodeRemoved -= OnPerformanceNodeRemoved;
        _performanceNodeTracking = false;
        _performanceParticles.Clear();
    }

    private void RebuildPerformanceParticleIndex()
    {
        _performanceParticles.Clear();
        CollectPerformanceParticles(_level);
    }

    private void CollectPerformanceParticles(Node node)
    {
        if (node is GpuParticles2D particles) _performanceParticles.Add(particles);
        int childCount = node.GetChildCount();
        for (int index = 0; index < childCount; index++)
            CollectPerformanceParticles(node.GetChild(index));
    }

    private void OnPerformanceNodeAdded(Node node)
    {
        if (node is GpuParticles2D particles && !_performanceParticles.Contains(particles))
            _performanceParticles.Add(particles);
    }

    private void OnPerformanceNodeRemoved(Node node)
    {
        if (node is GpuParticles2D particles) _performanceParticles.Remove(particles);
    }

    private int CountActiveParticles()
    {
        int count = 0;
        for (int index = _performanceParticles.Count - 1; index >= 0; index--)
        {
            GpuParticles2D particles = _performanceParticles[index];
            if (!GodotObject.IsInstanceValid(particles) || !particles.IsInsideTree())
            {
                _performanceParticles.RemoveAt(index);
                continue;
            }
            if (particles.Emitting) count++;
        }
        return count;
    }
}
