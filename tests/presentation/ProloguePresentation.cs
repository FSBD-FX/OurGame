using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Traversal;
using Knight.Presentation;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;
using Knight.Vfx;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

public partial class ProloguePresentation : SceneTree
{
    // Keep the real completion card on screen for two seconds after the Boss has
    // actually completed.  This is a presentation-only dwell; it does not alter
    // the route, combat, or runtime actors.
    private const int FinalFrame = 2989;
    private const int CompletionReturnFrame = FinalFrame - 120;
    private const int AudioDrainLeadFrames = 90;
    // ThreadAbility presents from the real player origin during the physics tick. Twelve pixels
    // is one 60 Hz step at the production 720 px/s pull speed, so it tolerates frame ordering
    // without accepting a detached line origin.
    private const float ThreadOriginTolerance = 12f;
    private const float ThreadTargetTolerance = 1f;
    private const float EffectiveVisibilityEpsilon = .001f;
    private const float GuidancePromptTargetGap = 16f;
    private const float GuidancePromptProtectionPadding = 8f;
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private PrologueUiRoot _ui;
    private ProloguePauseMenu _pause;
    private CharacterBody2D _player;
    private Camera2D _camera;
    private int _frame;
    private bool _bossThreadPressed;
    private int _bossThreadPressedFrame = -1;
    private int _bossThreadReleasedFrame = -1;
    private int _bossHammerThrownFrame = -1;
    private bool _bossTransitionTriggered;
    private bool _bossPreconditioned;
    private bool _bossFinalStrike;
    private bool _completionLogged;
    private bool _returnedToTitle;
    private bool _mothSensoryVisibilityObserved;
    private readonly bool _forceThreadAttemptSplice =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_SPLICE_THREAD_ATTEMPT"), "1", StringComparison.Ordinal);
    private readonly bool _forceVisibilityBoundsFailure =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_FORCE_VISIBILITY_BOUNDS_FAILURE"), "1", StringComparison.Ordinal);
    private readonly bool _forceMothOffscreen =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_FORCE_MOTH_OFFSCREEN"), "1", StringComparison.Ordinal);
    private readonly string _forceHiddenTargetLifecycle =
        System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_HIDE_REQUIRED_TARGET_LIFECYCLE") ?? string.Empty;
    private readonly string _forceHiddenTargetAncestorLifecycle =
        System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_HIDE_REQUIRED_TARGET_ANCESTOR_LIFECYCLE") ?? string.Empty;
    private readonly string _dropTraversalStage =
        System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_DROP_TRAVERSAL_STAGE") ?? string.Empty;
    private readonly bool _hideTraversalThread =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_HIDE_TRAVERSAL_THREAD"), "1", StringComparison.Ordinal);
    private readonly bool _forceWrongTraversalLineEnd =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_WRONG_TRAVERSAL_LINE_END"), "1", StringComparison.Ordinal);
    private readonly bool _forceZeroTraversalMarkerScale =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_ZERO_TRAVERSAL_MARKER_SCALE"), "1", StringComparison.Ordinal);
    private readonly bool _forceBossPromptCandidatesBlocked =
        string.Equals(System.Environment.GetEnvironmentVariable(
            "KNIGHT_PRESENTATION_FORCE_BOSS_PROMPT_CANDIDATES_BLOCKED"), "1",
            StringComparison.Ordinal);
    private CanvasItem _forcedHiddenTargetAncestor;
    private readonly List<Area2D> _forcedBossPromptBlockers = [];
    private readonly List<RectangleShape2D> _forcedBossPromptBlockerShapes = [];
    private bool _forcedBossPromptBlockersLogged;
    private Task _audioDrainTask;
    private bool _failurePending;
    private FallenBellkeeperState _lastBossState = (FallenBellkeeperState)(-1);
    private int _suppressFallsUntil;
    private int _followPlayerCameraUntilFrame;
    private readonly Dictionary<string, AttackEvidence> _attackEvidence = new();
    private readonly Dictionary<string, int> _sensoryCaptureFrames = new(StringComparer.Ordinal);
    private readonly Dictionary<ThreadCastState, ThreadEvidence> _threadStateEvidence = new();
    private readonly Dictionary<ThreadFailureReason, ThreadFailureEvidence> _threadFailureEvidence = new();
    private readonly Dictionary<ThreadAttemptKind, ThreadAttempt> _threadAttempts = new();
    private ThreadAttempt _activeThreadAttempt;
    private int _nextThreadAttemptId;
    private ThreadAbility _threadAbility;
    private StaticBody2D _threadObstruction;
    private bool _recoveryCancelPending;
    private bool _recoveryFormalClearPending;
    private bool _recoveryCancelObserved;
    private int _recoveryCancelHitCount;
    private int _recoveryCancelFailureCount;
    private int _threadHitCount;
    private int _threadFailureCount;
    private readonly Dictionary<string, MotionSample> _previousMotion = new();
    private readonly Dictionary<string, MotionEvidence> _motionEvidence = new();
    private readonly Dictionary<string, int> _targetMarkerBoundsSamples = new();
    private readonly Dictionary<EnemyAttackKind, BossGuidanceEvidence> _bossGuidanceEvidence = new();
    private readonly Dictionary<EnemyAttackKind, List<string>> _bossGuidanceViolations = new();
    private readonly HashSet<EnemyAttackKind> _bossGuidanceSuppressions = new();
    private readonly List<string> _traversalProofStages = new();
    private int _dangerGeometryBoundsSamples;
    private PlayerMotor _motor;
    private bool _singleJumpLeftFloor;
    private bool _singleJumpComplete;
    private int _dashAcceptedBaseline;
    private int _dashRejectedBaseline;
    private int _dashAcceptedDelta;
    private int _dashRejectedDelta;
    private int _secondDashPressedFrame = -1;
    private int _secondDashRejectedFrame = -1;
    private int _postRejectFrames;
    private bool _secondDashBurstObserved;
    private Vector2 _previousDashProofPosition;
    private bool _failedCastObserved;
    private int _failedCastVisibleFrames;
    private int _failedCastFirstVisibleFrame = -1;
    private int _failedCastLastVisibleFrame = -1;
    private bool _failedCastCleaned;
    private bool _failedCastRetryReady;
    private PrologueRoom _upperProofRoom;
    private TraversalSegment2D[] _upperSegments = [];
    private int _upperSegmentIndex;
    private bool _upperProofActive;
    private bool _upperFinalLeftFloor;
    private bool _upperFinalLanded;
    private bool _upperExitRequested;
    private bool _upperProofSucceeded;
    private int _upperDashBaseline;
    private Task _upperProofTask;
    private Exception _upperProofFailure;
    private readonly HashSet<ulong> _upperCastIds = new();
    private readonly HashSet<ulong> _upperApplyIds = new();
    private UpperAssistedEvidence _upperAssisted = new();
    private PrologueMechanismVisual _mutatedTraversalMarker;
    private Vector2 _mutatedTraversalMarkerScale;

    private readonly record struct AttackEvidence(int Frame, ulong AttackId);
    private readonly record struct ThreadEvidence(
        int Frame, ulong CastId, int AttemptId, ThreadAttemptKind AttemptKind);
    private readonly record struct ThreadFailureEvidence(
        int Frame, int AttemptId, ThreadAttemptKind AttemptKind);
    private readonly record struct ThreadAttempt(
        int Id, ThreadAttemptKind Kind, int StartedFrame);
    private readonly record struct MotionSample(
        int Frame, string Transition, Vector2 Position, float Rotation, Vector2 Scale);
    private sealed class BossGuidanceEvidence
    {
        public int LastFrame = -2;
        public int CurrentRun;
        public int MaxRun;
    }
    private sealed class MotionEvidence
    {
        public int Samples;
        public int EntryBoundarySamples;
        public int ExitBoundarySamples;
        public int JudgedBoundsFrames;
        public float MaxSlotDisplacement;
        public float MaxSlotRotation;
        public float MaxSlotScaleDelta;
        public string WorstDisplacementSlot = string.Empty;
        public int WorstDisplacementFrame;
        public string WorstSlot = string.Empty;
        public int WorstFrame;
    }
    private readonly record struct MotionJudgementWindow(string Transition, int FirstFrame, int LastFrame);
    private readonly record struct TargetJudgementWindow(string Lifecycle, int FirstFrame, int LastFrame);
    private sealed class UpperAssistedEvidence
    {
        public string Stage = string.Empty;
        public ulong ExpectedTargetId;
        public ulong AppliedTargetId;
        public int CastCount;
        public int ApplyCount;
        public int VisibleRun;
        public int MaxVisibleRun;
        public int FirstVisibleFrame = -1;
        public int LastVisibleFrame = -1;
        public float MaxOriginDistance;
        public float MaxLineEndDistance;
        public float MaxStrandEndDistance;
        public float LastDisplayedOriginDistance = float.PositiveInfinity;
        public float LastDisplayedLineEndDistance = float.PositiveInfinity;
        public float LastDisplayedStrandEndDistance = float.PositiveInfinity;
        public float LastMarkerScaleX;
        public float LastMarkerScaleY;
        public float LastMarkerAlpha;
        public bool ApplySeen;
        public bool CleanupBeforeApply;
        public bool OverlayClear = true;
    }
    private enum ThreadAttemptKind { None, NoTarget, Anchor, Cooldown, Obstructed }

    private static readonly string[] RequiredMotionTransitions =
    [
        "run_start_stop_jump_land", "attack_hit_whiff", "thread_tap_hold", "hurt_recovery",
        "rust_shell_attack", "grave_lamp_moth_dive", "bell_back_laborer_sweep",
        "boss_sweep", "boss_leap_slam", "boss_ground_wave", "boss_hammer_throw"
    ];

    private static readonly MotionJudgementWindow[] MotionJudgementWindows =
    [
        new("run_start_stop_jump_land", 295, 394),
        new("attack_hit_whiff", 350, 380),
        new("attack_hit_whiff", 445, 480),
        new("thread_tap_hold", 615, 748),
        new("hurt_recovery", 752, 754),
        new("rust_shell_attack", 430, 520),
        new("grave_lamp_moth_dive", 830, 920),
        new("bell_back_laborer_sweep", 1739, 1894),
        new("boss_sweep", 2039, 2741),
        new("boss_leap_slam", 2039, 2741),
        new("boss_ground_wave", 2039, 2741),
        new("boss_hammer_throw", 2039, 2741)
    ];

    private static readonly TargetJudgementWindow[] RequiredTargetWindows =
    [
        new("player-intro-route", 250, 394),
        new("rust-shell-route", 430, 534),
        new("thread-lesson", 615, 805),
        new("upper-moth-route", 830, 995),
        new("lower-route", 1629, 1709),
        new("laborer-mechanism-route", 1739, 1994),
        new("boss-guidance", 2039, 2741)
    ];

    private static readonly FieldInfo GuidanceTargetsField =
        typeof(WorldTargetMarkerVisual).GetField("_targets", BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new InvalidOperationException("WorldTargetMarkerVisual target storage contract is unavailable");
    private static readonly FieldInfo ThreadSettingsField =
        typeof(ThreadAbility).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new InvalidOperationException("ThreadAbility settings field is unavailable for presentation reset");

    public override void _Initialize()
    {
        try
        {
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            _flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
            _flow.HeadlessAutoStart = false;
            _flow.AutomationAutoStart = false;
            _player = _level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            _motor = _player.GetNode<PlayerMotor>("Motor");
            _threadAbility = _player.GetNode<ThreadAbility>("ThreadAbility");
            _threadAbility.Tuning = new ThreadTuning { CooldownSeconds = 1f };
            Root.AddChild(_level);
            _ui = _level.GetNode<PrologueUiRoot>("ProductUi");
            _pause = _level.GetNode<ProloguePauseMenu>("PauseMenu");
            _threadAbility.CastFrameChanged += ObserveThreadCast;
            _threadAbility.ThreadHit += () => _threadHitCount++;
            _threadAbility.ThreadFailed += () => _threadFailureCount++;
            _player.GetNode<Camera2D>("Camera2D").Enabled = false;
            _camera = new Camera2D { Name = "PrologueProofCamera", Enabled = true, Position = new Vector2(300, 60) };
            _level.AddChild(_camera);
            _player.GetNode<HealthComponent>("Health").MaxHealth = 99;
            _player.GetNode<HealthComponent>("Health").ResetFull();
            _suppressFallsUntil = FinalFrame;
            GD.Print("PROLOGUE_PRESENTATION_STAGE=boot_to_title");
        }
        catch (Exception exception)
        {
            BeginFailure("PROLOGUE_PRESENTATION_INIT_FAIL", exception);
        }
    }

    public override bool _Process(double delta)
    {
        if (_failurePending)
        {
            CompleteFailureWhenAudioSettles();
            return false;
        }
        if (_frame == FinalFrame - 1 && _audioDrainTask?.IsCompleted == false)
        {
            try { ThrowIfAudioDrainFailed(); }
            catch (Exception exception)
            {
                BeginFailure($"PROLOGUE_PRESENTATION_FAIL frame={_frame}", exception);
            }
            return false;
        }
        _frame++;
        try
        {
            ValidatePendingRecoveryFormalClear();
            Drive();
            ObserveTraversalProof();
            ObserveMotion();
            ObserveSensoryLandmarks();
            KeepProofActorSafe();
            if (_frame == FinalFrame - AudioDrainLeadFrames) StartAudioDrain();
            ThrowIfAudioDrainFailed();
            if (_frame != FinalFrame) return false;
            ReleaseAll();
            ValidateAttackEvidence();
            ValidateBossGuidanceClearance();
            ValidateThreadEvidence();
            ValidateMotionEvidence();
            if (!_mothSensoryVisibilityObserved)
                throw new InvalidOperationException("Moth sensory segment never kept the real Moth inside the proof frame");
            PrologueVisualAudit.AuditTraversalProofStageOrder(_traversalProofStages);
            PrologueVisualAudit.AuditDashProof(
                _dashAcceptedDelta, _dashRejectedDelta,
                _secondDashBurstObserved, _postRejectFrames);
            PrologueVisualAudit.AuditFailedCastProof(_failedCastObserved,
                _failedCastVisibleFrames, _failedCastCleaned, _failedCastRetryReady);
            if (_upperProofFailure != null)
                throw new InvalidOperationException("continuous Upper traversal proof failed", _upperProofFailure);
            if (_upperProofTask == null || !_upperProofTask.IsCompletedSuccessfully || !_upperProofSucceeded)
                throw new InvalidOperationException(
                    $"continuous Upper traversal proof did not finish; " +
                    $"task={_upperProofTask?.Status} succeeded={_upperProofSucceeded}");
            PrologueCompletionController completion = _level.GetNode<PrologueCompletionController>("Completion");
            if (completion.CurrentObjective != PrologueObjective.Complete || completion.CompletionCount != 1 ||
                !_returnedToTitle || _flow.State != PrologueGameFlowState.Title)
                throw new InvalidOperationException("presentation did not reach one terminal completion");
            if (_frame != FinalFrame)
                throw new InvalidOperationException(
                    $"presentation completion marker must use the actual final frame; actual={_frame}, expected={FinalFrame}");
            if (_audioDrainTask == null || !_audioDrainTask.IsCompletedSuccessfully)
                throw new InvalidOperationException("presentation audio drain did not complete before the final frame");
            if (!_level.IsInsideTree() || _level.GetNodeOrNull<Knight.Audio.PrologueAudioDirector>("Audio") != null)
                throw new InvalidOperationException("presentation final frame must keep the visible level and release only audio");
            GD.Print($"PROLOGUE_PRESENTATION_FINAL_VISIBLE frame={_frame} state={_flow.State} level_inside_tree=True");
            GD.Print($"PROLOGUE_PRESENTATION_COMPLETE frame={_frame}");
            TestSceneTeardown.DrainManagedWrappersBeforeQuit("prologue");
            Quit(0);
        }
        catch (Exception exception)
        {
            BeginFailure($"PROLOGUE_PRESENTATION_FAIL frame={_frame}", exception);
        }
        return false;
    }

    private void StartAudioDrain()
    {
        _audioDrainTask ??= TestSceneTeardown.DrainAudioOwner(this, _level);
    }

    private void ThrowIfAudioDrainFailed()
    {
        if (_audioDrainTask?.IsFaulted == true)
            throw new InvalidOperationException("presentation audio drain failed",
                _audioDrainTask.Exception?.GetBaseException());
        if (_audioDrainTask?.IsCanceled == true)
            throw new InvalidOperationException("presentation audio drain was canceled");
    }

    private void BeginFailure(string label, Exception exception)
    {
        if (_failurePending) return;
        ReleaseAll();
        GD.PushError($"{label}: {exception}");
        _failurePending = true;
        if (_audioDrainTask == null && IsInstanceValid(_level) && _level.IsInsideTree()) StartAudioDrain();
        CompleteFailureWhenAudioSettles();
    }

    private void CompleteFailureWhenAudioSettles()
    {
        if (_audioDrainTask?.IsFaulted == true)
            _ = _audioDrainTask.Exception;
        if (_audioDrainTask == null || _audioDrainTask.IsCompleted) Quit(1);
    }

    private void Drive()
    {
        if (_frame <= _followPlayerCameraUntilFrame)
            _camera.GlobalPosition = _player.GlobalPosition;
        if (_frame == 20) GD.Print("PROLOGUE_PRESENTATION_STAGE=title_menu");
        if (_frame == 70)
        {
            _ui.ShowSettings(new GameSettingsData());
            GD.Print("PROLOGUE_PRESENTATION_STAGE=settings_menu");
        }
        if (_frame == 140)
        {
            _ui.ShowControls();
            GD.Print("PROLOGUE_PRESENTATION_STAGE=controls_menu");
        }
        if (_frame == 205)
        {
            _ui.ShowMainMenu(false);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=title_return");
        }
        if (_frame == 250)
        {
            _flow.StartNew();
            _camera.Position = _player.GlobalPosition;
            GD.Print("PROLOGUE_PRESENTATION_STAGE=intro_awakening");
        }

        if (_frame == 295)
        {
            _dashAcceptedBaseline = _motor.AcceptedDashCount;
            _dashRejectedBaseline = _motor.RejectedDashCount;
            Press(InputActions.MoveRight);
        }
        if (_frame == 300) Press(InputActions.Jump);
        if (_frame == 305) Release(InputActions.Jump);
        if (_frame == 355) Press(InputActions.Attack);
        if (_frame == 358) Release(InputActions.Attack);
        if (_frame == 365)
        {
            if (!_singleJumpComplete || !_player.IsOnFloor())
                throw new InvalidOperationException(
                    $"ordinary single-jump proof did not settle before dash cycle; " +
                    $"single={_singleJumpComplete} floor={_player.IsOnFloor()} position={_player.Position}");
            Press(InputActions.Jump);
        }
        if (_frame == 367)
        {
            Press(InputActions.MoveUp);
            Press(InputActions.Dash);
        }
        if (_frame == 368) Release(InputActions.Dash);
        if (_frame == 375)
        {
            _secondDashPressedFrame = _frame;
            _previousDashProofPosition = _player.GlobalPosition;
            Press(InputActions.Dash);
        }
        if (_frame == 376) Release(InputActions.Dash);
        if (_frame == 383) Release(InputActions.Jump);
        if (_frame == 388) Release(InputActions.MoveUp);
        if (_frame == 390) Release(InputActions.MoveRight);
        if (_frame == 395) TouchExit("Exits/ToCorridor", "broken_tooth_corridor");

        if (_frame == 430)
        {
            _camera.Position = new Vector2(-80, 60);
            RustShellSquire rustShell = _level.CurrentRoom.GetNode<RustShellSquire>("Actors/RustShellSquire");
            Fixture(rustShell.GlobalPosition + new Vector2(-70, 0));
            Track(rustShell, "RustShell");
            GD.Print("PROLOGUE_PRESENTATION_STAGE=first_combat");
        }
        if (_frame == 450) AttackPulse(true);
        if (_frame == 454) AttackPulse(false);
        if (_frame == 535)
        {
            _camera.Position = new Vector2(850, 55);
            FixtureCheckpointTrigger();
            GD.Print("PROLOGUE_PRESENTATION_STAGE=checkpoint_lit");
        }
        if (_frame == 560)
        {
            _pause.Open();
            GD.Print("PROLOGUE_PRESENTATION_STAGE=pause_menu");
        }
        if (_frame == 610) _pause.Execute(ProloguePauseCommand.Resume);
        if (_frame == 615)
        {
            _camera.Position = new Vector2(-210, 30);
            Fixture(new Vector2(210, 68));
            BeginThreadAttempt(ThreadAttemptKind.NoTarget);
            Press(InputActions.MoveDown);
            Press(InputActions.Thread);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=thread_no_target");
        }
        if (_frame == 618) Release(InputActions.Thread);
        if (_frame == 619) Release(InputActions.MoveDown);
        if (_frame == 625)
        {
            _camera.Position = new Vector2(490, 30);
            Fixture(new Vector2(210, 68));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=thread_anchor_lesson");
        }
        if (_frame == 630)
        {
            BeginThreadAttempt(ThreadAttemptKind.Anchor);
            Press(InputActions.MoveRight);
            Press(InputActions.MoveUp);
            Press(InputActions.Thread);
        }
        if (_forceThreadAttemptSplice && _frame == 640)
        {
            Press(InputActions.Dash);
            Release(InputActions.Thread);
            ReleaseAim();
            EndThreadAttempt();
            GD.Print("PROLOGUE_THREAD_PRESENTATION_SPLICE_MUTATION attempt=A aborted_by=Dash");
        }
        if (_forceThreadAttemptSplice && _frame == 641) Release(InputActions.Dash);
        if (_forceThreadAttemptSplice && _frame == 643)
        {
            Fixture(new Vector2(210, 68));
            BeginThreadAttempt(ThreadAttemptKind.Anchor);
            Press(InputActions.MoveRight);
            Press(InputActions.MoveUp);
            Press(InputActions.Thread);
            GD.Print("PROLOGUE_THREAD_PRESENTATION_SPLICE_MUTATION attempt=B started=True");
        }
        if (_frame == (_forceThreadAttemptSplice ? 655 : 650)) Release(InputActions.Thread);
        if (_frame == (_forceThreadAttemptSplice ? 656 : 651))
        {
            Release(InputActions.MoveRight);
            Release(InputActions.MoveUp);
        }
        if (!_forceThreadAttemptSplice && _frame == 677)
        {
            if (_threadAbility.CurrentCastFrame.State != ThreadCastState.Recovery)
                throw new InvalidOperationException(
                    $"recovery-cancel proof expected Recovery at frame 677; actual={_threadAbility.CurrentCastFrame.State}");
            _recoveryCancelPending = true;
            _recoveryCancelHitCount = _threadHitCount;
            _recoveryCancelFailureCount = _threadFailureCount;
            Press(InputActions.MoveLeft);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=thread_recovery_cancel");
        }
        if (!_forceThreadAttemptSplice && _frame == 679) Release(InputActions.MoveLeft);
        if (!_forceThreadAttemptSplice && _frame == 680)
        {
            BeginThreadAttempt(ThreadAttemptKind.Cooldown);
            Press(InputActions.MoveRight);
            Press(InputActions.Thread);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=thread_cooldown_hold");
        }
        if (!_forceThreadAttemptSplice && _frame == 718) Release(InputActions.Thread);
        if (!_forceThreadAttemptSplice && _frame == 719) Release(InputActions.MoveRight);
        if (_frame == (_forceThreadAttemptSplice ? 685 : 722))
        {
            ThreadTarget2D anchor = _level.CurrentRoom.GetNode<ThreadTarget2D>("Geometry/ThreadAnchor");
            Fixture(anchor.GlobalPosition + new Vector2(-180, 0));
            AddThreadObstruction((_player.GlobalPosition + anchor.GlobalPosition) * .5f);
            BeginThreadAttempt(ThreadAttemptKind.Obstructed);
            Press(InputActions.MoveRight);
            Press(InputActions.Thread);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=thread_wall_obstructed");
        }
        if (_frame == (_forceThreadAttemptSplice ? 700 : 737)) Release(InputActions.Thread);
        if (_frame == (_forceThreadAttemptSplice ? 701 : 738)) Release(InputActions.MoveRight);
        if (_frame == (_forceThreadAttemptSplice ? 710 : 748)) RemoveThreadObstruction();
        if (_frame == (_forceThreadAttemptSplice ? 720 : 755))
        {
            _level.RequestRespawn("坠落 · 墓灯重燃");
            _level.GetNode<PrologueHud>("Hud").ShowRecovery("坠落 · 墓灯重燃");
            GD.Print("PROLOGUE_PRESENTATION_STAGE=checkpoint_recovery");
        }
        if (_frame == (_forceThreadAttemptSplice ? 718 : 752))
        {
            // Presentation-only proof: Hurt → automatic pose recovery without changing gameplay.
            _player.GetNode<PlayerFormalVisual>("VisualRoot/Formal").PlayCue(PresentationCue.Hurt);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=hurt_recovery");
        }
        if (_frame == (_forceThreadAttemptSplice ? 760 : 780))
            _level.GetNode<PrologueHud>("Hud").HideRecovery();
        if (_frame == (_forceThreadAttemptSplice ? 770 : 790)) FixtureCheckpointTrigger();
        if (_frame == (_forceThreadAttemptSplice ? 790 : 805))
            TouchExit("Exits/ToUpper", "upper_pendulum_route");

        if (_frame == 830)
        {
            GraveLampMoth moth = _level.CurrentRoom.GetNode<GraveLampMoth>("Actors/air");
            // The proof camera follows the real Moth rather than the room's broad
            // landmark framing, so Telegraph → Active → Recovery remains readable.
            _camera.Position = moth.GlobalPosition +
                (_forceMothOffscreen ? new Vector2(900f, 0f) : new Vector2(0f, -28f));
            Fixture(moth.GlobalPosition + new Vector2(-120f, 60f));
            Track(moth, "GraveLampMoth");
            GD.Print("PROLOGUE_PRESENTATION_STAGE=light_enemy_thread_pull");
        }
        if (_frame == 945) ThreadPulse(true, false);
        if (_frame == 949) ThreadPulse(false, false);
        if (_frame == 970)
        {
            _camera.Position = new Vector2(930, -120);
            Fixture(new Vector2(930, -38));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=hidden_alcove");
        }
        if (_frame == 995) SetupUpperTraversalProof();
        if (_frame == 1000)
        {
            _upperProofActive = true;
            _upperProofTask = RunUpperTraversalProof();
        }
        if (_frame == 1604) TouchExit("Exits/ToLower", "lower_buried_machinery");

        if (_frame == 1629)
        {
            _camera.Position = new Vector2(300, 70);
            Fixture(new Vector2(30, 198));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=lower_mixed_encounter");
        }
        if (_frame == 1649) AttackPulse(true);
        if (_frame == 1653) AttackPulse(false);
        if (_frame == 1669)
        {
            _camera.Position = new Vector2(790, -40);
            Fixture(new Vector2(690, 198));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=lower_air_enemy");
        }
        if (_frame == 1689) ThreadPulse(true, true);
        if (_frame == 1693) ThreadPulse(false, false);
        if (_frame == 1709) TouchExit("Exits/ToMechanism", "lower_convergence");

        if (_frame == 1739)
        {
            _camera.Position = new Vector2(430, 70);
            BellBackLaborer laborer = _level.CurrentRoom.GetNode<BellBackLaborer>("Actors/laborer");
            Fixture(laborer.GlobalPosition + new Vector2(-70, 0));
            Track(laborer, "BellBackLaborer");
            GD.Print("PROLOGUE_PRESENTATION_STAGE=heavy_enemy_exposure");
        }
        if (_frame == 1869) ThreadPulse(true, false);
        if (_frame == 1873) ThreadPulse(false, false);
        if (_frame == 1899) AttackPulse(true);
        if (_frame == 1903) AttackPulse(false);
        if (_frame == 1929)
        {
            _camera.Position = new Vector2(780, 70);
            BellChainMechanism mechanism = _level.CurrentRoom.GetNode<BellChainMechanism>("Mechanisms/BellChain");
            Fixture(mechanism.GlobalPosition + new Vector2(-190, 40));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=bell_chain_neutral_aim_assist");
        }
        if (_frame == 1949)
        {
            Release(InputActions.MoveRight);
            Release(InputActions.MoveUp);
            Press(InputActions.Thread);
        }
        if (_frame == 1953) Release(InputActions.Thread);
        if (_frame == 1994) TouchExit("Exits/P1Complete", "fallen_bellkeeper_arena");

        if (_frame == 2039)
        {
            _camera.Position = new Vector2(380, 40);
            Fixture(_level.CurrentRoom.GetNode<Area2D>("ActivationTrigger").GlobalPosition);
            Track(Boss(), "Boss");
            GD.Print("PROLOGUE_PRESENTATION_STAGE=boss_intro");
        }
        if (_frame == 2114) GD.Print("PROLOGUE_PRESENTATION_STAGE=boss_phase_one");

        FallenBellkeeper activeBoss = TryBoss();
        if (activeBoss != null && !_bossTransitionTriggered &&
            HasEvidence("Boss", EnemyAttackKind.BossGroundWave, "recovery"))
        {
            _bossTransitionTriggered = true;
            HealthComponent health = activeBoss.GetNode<HealthComponent>("Health");
            if (health.CurrentHealth > 7)
                health.ApplyDamage(new DamageRequest(health.CurrentHealth - 7, Vector2.Right, 0f, 98000UL));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=boss_phase_transition");
        }
        if (activeBoss != null && activeBoss.CurrentFrame.State != _lastBossState)
        {
            _lastBossState = activeBoss.CurrentFrame.State;
            GD.Print($"PROLOGUE_PRESENTATION_BOSS_STATE={_lastBossState} frame={_frame}");
        }
        if (activeBoss != null && activeBoss.CurrentFrame.State == FallenBellkeeperState.HammerThrown &&
            _bossHammerThrownFrame < 0)
        {
            _bossHammerThrownFrame = _frame;
            _camera.Position = new Vector2(420, 35);
            Fixture(activeBoss.GlobalPosition + new Vector2(-190, 30));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=boss_hammer_chain_guidance");
        }
        if (activeBoss != null && activeBoss.CurrentFrame.State == FallenBellkeeperState.HammerThrown &&
            !_bossThreadPressed && HasEvidence("Boss", EnemyAttackKind.BossHammerThrow, "recovery"))
        {
            _bossThreadPressed = true;
            _bossThreadPressedFrame = _frame;
            Fixture(activeBoss.GlobalPosition + new Vector2(-190, 30));
            ThreadPulse(true, false);
            GD.Print("PROLOGUE_PRESENTATION_STAGE=boss_hammer_thread_window");
        }
        if (_bossThreadPressed && Input.IsActionPressed(InputActions.Thread) &&
            _frame >= _bossThreadPressedFrame + 12)
        {
            Release(InputActions.Thread);
            _bossThreadReleasedFrame = _frame;
        }
        if (_bossThreadReleasedFrame >= 0 && _frame == _bossThreadReleasedFrame + 1)
            ReleaseAim();
        if (activeBoss != null && activeBoss.CurrentFrame.State == FallenBellkeeperState.Exposed &&
            !_bossPreconditioned)
        {
            _bossPreconditioned = true;
            HealthComponent health = activeBoss.GetNode<HealthComponent>("Health");
            activeBoss.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(
                health.CurrentHealth - 1, Vector2.Right, 0f, 98001UL));
            GD.Print("PROLOGUE_PRESENTATION_STAGE=boss_exposed");
        }
        if (_bossPreconditioned && !_bossFinalStrike && activeBoss != null)
        {
            _bossFinalStrike = true;
            Fixture(activeBoss.GlobalPosition + new Vector2(-52, 14));
            AttackPulse(true);
        }
        if (_bossFinalStrike && Input.IsActionPressed(InputActions.Attack) && activeBoss == null)
            AttackPulse(false);
        if (_bossFinalStrike && activeBoss != null)
        {
            _player.GlobalPosition = activeBoss.GlobalPosition + new Vector2(-52, 0);
            _player.Velocity = Vector2.Zero;
        }
        PrologueCompletionController completion = _level.GetNode<PrologueCompletionController>("Completion");
        if (!_completionLogged && completion.CompletionCount == 1)
        {
            _completionLogged = true;
            ReleaseAll();
            _camera.Position = new Vector2(470, 35);
            GD.Print($"PROLOGUE_PRESENTATION_STAGE=prologue_complete objective={completion.CurrentObjective} " +
                $"count={completion.CompletionCount} shortcut={_level.Progress.BellChainShortcutOpen} " +
                $"boss_state={activeBoss?.CurrentFrame.State} boss_health={activeBoss?.GetNode<HealthComponent>("Health").CurrentHealth} " +
                $"thread={_bossThreadPressed} preconditioned={_bossPreconditioned} strike={_bossFinalStrike}");
        }
        if (_frame == CompletionReturnFrame)
        {
            if (!_completionLogged)
            {
                PrologueCompletionController pending = _level.GetNode<PrologueCompletionController>("Completion");
                throw new InvalidOperationException("completion card was not reached before title return; " +
                    $"objective={pending.CurrentObjective}, count={pending.CompletionCount}, " +
                    $"shortcut={_level.Progress.BellChainShortcutOpen}, boss={_level.Progress.BossDefeated}, " +
                    $"boss_node={TryBoss() != null}");
            }
            _flow.ReturnToTitle();
            _returnedToTitle = true;
            GD.Print("PROLOGUE_PRESENTATION_STAGE=completion_return_to_title");
        }
    }

    private void ObserveTraversalProof()
    {
        ObserveSingleJumpAndDashProof();
        ObserveFailedCastProof();
        ObserveUpperTraversalProof();
    }

    private void ObserveSingleJumpAndDashProof()
    {
        if (_frame >= 300 && _frame < 365 && !_singleJumpComplete)
        {
            if (!_player.IsOnFloor()) _singleJumpLeftFloor = true;
            if (_singleJumpLeftFloor && _frame > 305 && _player.IsOnFloor())
            {
                if (_motor.AcceptedDashCount != _dashAcceptedBaseline ||
                    _motor.RejectedDashCount != _dashRejectedBaseline ||
                    _threadAbility.CurrentCastFrame.State != ThreadCastState.Idle)
                    throw new InvalidOperationException(
                        "ordinary single-jump proof consumed Dash or mechanical thread");
                _singleJumpComplete = true;
                PublishTraversalStage("single_jump");
            }
        }

        if (_frame >= 367 && _frame < 375 &&
            _motor.AcceptedDashCount == _dashAcceptedBaseline + 1 &&
            !_traversalProofStages.Contains("air_dash_consumed"))
        {
            if (_player.IsOnFloor() || _motor.AirDashAvailable)
                throw new InvalidOperationException(
                    $"accepted dash proof is not in one consumed airborne cycle; " +
                    $"floor={_player.IsOnFloor()} available={_motor.AirDashAvailable}");
            _dashAcceptedDelta = 1;
            PublishTraversalStage("air_dash_consumed");
        }

        if (_secondDashPressedFrame < 0) return;
        if (_traversalProofStages.Contains("second_dash_rejected")) return;
        Vector2 step = _player.GlobalPosition - _previousDashProofPosition;
        _previousDashProofPosition = _player.GlobalPosition;
        if (step.Length() > 18f) _secondDashBurstObserved = true;
        if (_motor.AcceptedDashCount > _dashAcceptedBaseline + 1)
            _secondDashBurstObserved = true;
        if (_secondDashRejectedFrame < 0 &&
            _motor.RejectedDashCount > _dashRejectedBaseline)
        {
            _secondDashRejectedFrame = _frame;
            _dashRejectedDelta = _motor.RejectedDashCount - _dashRejectedBaseline;
        }
        if (_secondDashRejectedFrame < 0) return;
        _postRejectFrames = _frame - _secondDashRejectedFrame + 1;
        if (_postRejectFrames >= 2 &&
            !_traversalProofStages.Contains("second_dash_rejected"))
        {
            _dashAcceptedDelta = _motor.AcceptedDashCount - _dashAcceptedBaseline;
            _dashRejectedDelta = _motor.RejectedDashCount - _dashRejectedBaseline;
            PrologueVisualAudit.AuditDashProof(_dashAcceptedDelta, _dashRejectedDelta,
                _secondDashBurstObserved, _postRejectFrames);
            PublishTraversalStage("second_dash_rejected");
        }
    }

    private void ObserveFailedCastProof()
    {
        if (_failedCastObserved && !_failedCastCleaned)
        {
            ThreadLineVisual line = _player.GetNode<ThreadLineVisual>("ThreadLine");
            ThreadShuttleVisual shuttle = _player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle");
            bool lineFeedback = line.CurrentState == ThreadLineState.Failed &&
                line.ActiveStrandCount > 0;
            bool casterFeedback = shuttle.CurrentFrame.State == ThreadCastState.Failed &&
                shuttle.FailureAtCasterVisible && shuttle.Visible;
            if (_threadAbility.CurrentCastFrame.State == ThreadCastState.Failed &&
                (lineFeedback || casterFeedback))
            {
                if (_failedCastFirstVisibleFrame < 0)
                    _failedCastFirstVisibleFrame = _frame;
                _failedCastLastVisibleFrame = _frame;
                _failedCastVisibleFrames++;
            }
            if (_threadAbility.CurrentCastFrame.State == ThreadCastState.Idle)
            {
                if (line.ActiveStrandCount != 0 || line.CurrentState != ThreadLineState.Idle ||
                    !shuttle.IsClear || _threadAbility.HasLockedTarget)
                    throw new InvalidOperationException(
                        "traversal Failed cast left line, shuttle, or target residue");
                _failedCastCleaned = true;
            }
        }
    }

    private void SetupUpperTraversalProof()
    {
        if (_level.CurrentRoomId != PrologueRoomId.UpperPendulum)
            throw new InvalidOperationException(
                $"Upper traversal proof setup requires UpperPendulum; actual={_level.CurrentRoomId}");
        _upperProofRoom = _level.CurrentRoom;
        _upperSegments =
        [
            _upperProofRoom.GetNode<TraversalSegment2D>("TraversalContracts/entry_to_lower_swing"),
            _upperProofRoom.GetNode<TraversalSegment2D>("TraversalContracts/lower_to_mid_swing"),
            _upperProofRoom.GetNode<TraversalSegment2D>("TraversalContracts/mid_to_high_swing"),
            _upperProofRoom.GetNode<TraversalSegment2D>("TraversalContracts/high_to_mechanism_landing")
        ];
        Marker2D entrance = _upperProofRoom.GetEntrance("from_corridor") ??
            throw new InvalidOperationException("Upper traversal proof lacks from_corridor entrance");
        var formalThreadTuning = new ThreadTuning();
        _threadAbility.Tuning = formalThreadTuning;
        ThreadSettingsField.SetValue(_threadAbility, formalThreadTuning.ToSettings());
        Fixture(entrance.GlobalPosition);
        _player = _level.Player;
        _motor = _player.GetNode<PlayerMotor>("Motor");
        _threadAbility = _player.GetNode<ThreadAbility>("ThreadAbility");
        _upperDashBaseline = _motor.AcceptedDashCount;
        _upperSegmentIndex = 0;
        BeginUpperAssistedEvidence("upper_anchor_low", _upperSegments[0]);
        RoomExit exit = _upperProofRoom.GetNode<RoomExit>("Exits/ToMechanism");
        exit.ExitRequested += OnUpperExitRequested;
        _camera.GlobalPosition = _player.GlobalPosition;
        _followPlayerCameraUntilFrame = 1584;
        GD.Print($"PROLOGUE_TRAVERSAL_SETUP frame={_frame} room=UpperPendulum " +
            $"position={_player.Position} placement=from_corridor");
    }

    private async Task RunUpperTraversalProof()
    {
        try
        {
            await PrologueTraversalRouteTests.TraverseUpperContractsContinuously(
                this, _upperProofRoom, _player);
            await PrologueTraversalRouteTests.ReachUpperMechanismExitContinuously(
                this, _upperProofRoom, _player);
            for (int frame = 0; frame < 30 &&
                _level.CurrentRoomId != PrologueRoomId.BellChainMechanism; frame++)
                await ToSignal(this, SignalName.PhysicsFrame);
            if (_level.CurrentRoomId != PrologueRoomId.BellChainMechanism)
                throw new InvalidOperationException(
                    $"real Upper exit did not transition to BellChainMechanism; actual={_level.CurrentRoomId}");
            _upperProofSucceeded = true;
        }
        catch (Exception exception)
        {
            _upperProofFailure = exception;
        }
    }

    private void ObserveUpperTraversalProof()
    {
        if (!_upperProofActive) return;
        if (_upperProofFailure != null)
            throw new InvalidOperationException("continuous Upper traversal helper failed", _upperProofFailure);
        if (_frame > 1584 && !_upperProofSucceeded)
            throw new InvalidOperationException(
                $"continuous Upper traversal exceeded approved frame 1584; task={_upperProofTask?.Status}");

        if (_level.CurrentRoomId == PrologueRoomId.UpperPendulum &&
            _upperSegmentIndex < _upperSegments.Length)
        {
            TraversalSegment2D segment = _upperSegments[_upperSegmentIndex];
            if (segment.Kind == TraversalSegmentKind.Assisted)
            {
                ThreadLineVisual line = _player.GetNode<ThreadLineVisual>("ThreadLine");
                ThreadShuttleVisual shuttle = _player.GetNode<ThreadShuttleVisual>(
                    "VisualRoot/ThreadShuttle");
                ThreadTarget2D target = _upperProofRoom.GetNode<ThreadTarget2D>(segment.AssistTargetPath);
                if (_hideTraversalThread && _upperSegmentIndex == 0)
                    line.Hide();
                if (_forceWrongTraversalLineEnd && _upperSegmentIndex == 0 &&
                    line.ActiveStrandCount > 0)
                    line.Present(line.LastWorldStart, target.GlobalPosition + new Vector2(80f, 0f),
                        line.LastTension, line.CurrentState);
                PrologueMechanismVisual anchorMarker =
                    target.GetNode<PrologueMechanismVisual>("Formal");
                if (_forceZeroTraversalMarkerScale && _upperSegmentIndex == 0)
                {
                    if (_mutatedTraversalMarker == null)
                    {
                        _mutatedTraversalMarker = anchorMarker;
                        _mutatedTraversalMarkerScale = anchorMarker.Scale;
                    }
                    anchorMarker.Scale = Vector2.Zero;
                }
                bool exactMarker = TraversalAnchorMarkerVisible(target,
                    out float markerScaleX, out float markerScaleY, out float markerAlpha);
                bool exactLineGeometry = ExactThreadLineGeometry(line, _player.GlobalPosition,
                    target.GlobalPosition, out float originDistance, out float lineEndDistance,
                    out float strandEndDistance);
                _upperAssisted.LastMarkerScaleX = markerScaleX;
                _upperAssisted.LastMarkerScaleY = markerScaleY;
                _upperAssisted.LastMarkerAlpha = markerAlpha;
                bool displayedCandidate = line.IsVisibleInTree() &&
                    line.ActiveStrandCount > 0 &&
                    shuttle.IsVisibleInTree() && !shuttle.IsClear &&
                    Finite(shuttle.LastWorldTarget) &&
                    shuttle.LastWorldTarget.DistanceTo(target.GlobalPosition) <= 1f &&
                    (shuttle.CandidateDiamondVisible || shuttle.LatchVisible || shuttle.NeedleVisible);
                if (displayedCandidate)
                {
                    _upperAssisted.LastDisplayedOriginDistance = originDistance;
                    _upperAssisted.LastDisplayedLineEndDistance = lineEndDistance;
                    _upperAssisted.LastDisplayedStrandEndDistance = strandEndDistance;
                }
                bool exactVisual = displayedCandidate && exactMarker && exactLineGeometry;
                _upperAssisted.VisibleRun = exactVisual ? _upperAssisted.VisibleRun + 1 : 0;
                _upperAssisted.MaxVisibleRun = Math.Max(
                    _upperAssisted.MaxVisibleRun, _upperAssisted.VisibleRun);
                if (exactVisual)
                {
                    _upperAssisted.MaxOriginDistance = Math.Max(
                        _upperAssisted.MaxOriginDistance, originDistance);
                    _upperAssisted.MaxLineEndDistance = Math.Max(
                        _upperAssisted.MaxLineEndDistance, lineEndDistance);
                    _upperAssisted.MaxStrandEndDistance = Math.Max(
                        _upperAssisted.MaxStrandEndDistance, strandEndDistance);
                    if (_upperAssisted.FirstVisibleFrame < 0)
                        _upperAssisted.FirstVisibleFrame = _frame;
                    _upperAssisted.LastVisibleFrame = _frame;
                    _upperAssisted.OverlayClear &= TraversalOverlayClear(segment, target);
                }
                bool landed = _upperAssisted.ApplySeen && _player.IsOnFloor() &&
                    segment.LandingZone.HasPoint(_player.Position);
                if (!_upperAssisted.ApplySeen && _upperAssisted.MaxVisibleRun > 0 &&
                    (_threadAbility.CurrentCastFrame.State == ThreadCastState.Idle ||
                     line.CurrentState == ThreadLineState.Idle || line.ActiveStrandCount == 0 ||
                     shuttle.IsClear))
                    _upperAssisted.CleanupBeforeApply = true;
                if (landed) CompleteUpperAssistedStage(segment);
            }
            else
            {
                if (!_player.IsOnFloor()) _upperFinalLeftFloor = true;
                if (_upperFinalLeftFloor && _player.IsOnFloor() &&
                    segment.LandingZone.HasPoint(_player.Position))
                    _upperFinalLanded = true;
            }
        }

        if (!_upperProofSucceeded ||
            _traversalProofStages.Contains("upper_landed")) return;
        if (!_upperFinalLanded || !_upperExitRequested ||
            _motor.AcceptedDashCount != _upperDashBaseline)
            throw new InvalidOperationException(
                $"real Upper arrival is incomplete; finalLanding={_upperFinalLanded} " +
                $"exitRequested={_upperExitRequested} dashDelta={_motor.AcceptedDashCount - _upperDashBaseline}");
        ThreadLineVisual finalLine = _player.GetNode<ThreadLineVisual>("ThreadLine");
        ThreadShuttleVisual finalShuttle = _player.GetNode<ThreadShuttleVisual>(
            "VisualRoot/ThreadShuttle");
        if (_threadAbility.CurrentCastFrame.State != ThreadCastState.Idle ||
            finalLine.ActiveStrandCount != 0 || !finalShuttle.IsClear || _threadAbility.HasLockedTarget)
            throw new InvalidOperationException(
                "real Upper arrival retained thread line, shuttle, cast, or target residue");
        PublishTraversalStage("upper_landed");
        GD.Print("PROLOGUE_PRESENTATION_TRANSITION=upper_convergence");
        _upperProofActive = false;
    }

    private void BeginUpperAssistedEvidence(string stage, TraversalSegment2D segment)
    {
        ThreadTarget2D target = _upperProofRoom.GetNode<ThreadTarget2D>(segment.AssistTargetPath);
        _upperCastIds.Clear();
        _upperApplyIds.Clear();
        _upperAssisted = new UpperAssistedEvidence
        {
            Stage = stage,
            ExpectedTargetId = target.GetInstanceId()
        };
    }

    private void CompleteUpperAssistedStage(TraversalSegment2D segment)
    {
        if (_upperAssisted.MaxVisibleRun < 2)
            throw new InvalidOperationException(
                $"{_upperAssisted.Stage} exact-anchor visual assertion failed; " +
                $"visible={_upperAssisted.MaxVisibleRun} " +
                $"origin={_upperAssisted.LastDisplayedOriginDistance:F3}/{ThreadOriginTolerance:F3} " +
                $"line_end={_upperAssisted.LastDisplayedLineEndDistance:F3}/{ThreadTargetTolerance:F3} " +
                $"strand_end={_upperAssisted.LastDisplayedStrandEndDistance:F3}/{ThreadTargetTolerance:F3} " +
                $"marker_scale=({_upperAssisted.LastMarkerScaleX:F3}," +
                $"{_upperAssisted.LastMarkerScaleY:F3}) " +
                $"marker_alpha={_upperAssisted.LastMarkerAlpha:F3}");
        var evidence = new PrologueVisualAudit.AssistedTraversalProofEvidence(
            _upperAssisted.Stage,
            _upperAssisted.ExpectedTargetId,
            _upperAssisted.AppliedTargetId,
            _upperAssisted.CastCount,
            _upperAssisted.ApplyCount,
            _upperAssisted.MaxVisibleRun,
            _upperAssisted.CleanupBeforeApply,
            true,
            _upperAssisted.OverlayClear);
        PrologueVisualAudit.AuditAssistedTraversalStage(evidence);
        GD.Print($"PROLOGUE_TRAVERSAL_ASSISTED_EVIDENCE stage={_upperAssisted.Stage} " +
            $"frame={_frame} target={_upperAssisted.AppliedTargetId} " +
            $"casts={_upperAssisted.CastCount} applies={_upperAssisted.ApplyCount} " +
            $"visible_consecutive={_upperAssisted.MaxVisibleRun} " +
            $"visible_first={_upperAssisted.FirstVisibleFrame} " +
            $"visible_last={_upperAssisted.LastVisibleFrame} " +
            $"max_origin_delta={_upperAssisted.MaxOriginDistance:F3} " +
            $"max_line_end_delta={_upperAssisted.MaxLineEndDistance:F3} " +
            $"max_strand_end_delta={_upperAssisted.MaxStrandEndDistance:F3} " +
            $"marker_scale=({_upperAssisted.LastMarkerScaleX:F3}," +
            $"{_upperAssisted.LastMarkerScaleY:F3}) " +
            $"marker_alpha={_upperAssisted.LastMarkerAlpha:F3} " +
            $"overlay_clear={_upperAssisted.OverlayClear}");
        PublishTraversalStage(_upperAssisted.Stage);
        if (_mutatedTraversalMarker != null)
        {
            _mutatedTraversalMarker.Scale = _mutatedTraversalMarkerScale;
            _mutatedTraversalMarker = null;
        }
        _upperSegmentIndex++;
        if (_upperSegmentIndex == 1)
            BeginUpperAssistedEvidence("upper_anchor_high", _upperSegments[1]);
        else if (_upperSegmentIndex == 2)
            BeginUpperAssistedEvidence("upper_rebound", _upperSegments[2]);
    }

    private bool TraversalOverlayClear(TraversalSegment2D segment, ThreadTarget2D target)
    {
        Transform2D canvas = Root.GetCanvasTransform();
        Rect2 launch = ScreenRect(_upperProofRoom, segment.LaunchZone, canvas);
        Rect2 landing = ScreenRect(_upperProofRoom, segment.LandingZone, canvas);
        Vector2 targetScreen = canvas * target.GlobalPosition;
        Rect2 targetMarker = new(targetScreen - new Vector2(14, 14), new Vector2(28, 28));
        if (targetMarker.Intersects(launch) || targetMarker.Intersects(landing)) return false;
        Control guidance = _level.GetNode<Control>("Hud/Guidance/VisualRoot");
        Control backdrop = guidance.GetNode<Control>("Backdrop");
        if (guidance.IsVisibleInTree())
        {
            Rect2 guidanceRect = backdrop.GetGlobalRect();
            if (guidanceRect.Intersects(launch) || guidanceRect.Intersects(landing)) return false;
        }
        Rect2 safe = new(new Vector2(24, 24), new Vector2(1232, 672));
        return safe.HasPoint(targetScreen);
    }

    private static bool TraversalAnchorMarkerVisible(ThreadTarget2D target,
        out float effectiveScaleX, out float effectiveScaleY, out float effectiveAlpha)
    {
        PrologueMechanismVisual marker = target.GetNodeOrNull<PrologueMechanismVisual>("Formal");
        effectiveScaleX = 0f;
        effectiveScaleY = 0f;
        effectiveAlpha = 0f;
        if (marker == null) return false;
        Transform2D transform = marker.GlobalTransform;
        effectiveScaleX = transform.X.Length();
        effectiveScaleY = transform.Y.Length();
        float determinant = transform.X.X * transform.Y.Y - transform.X.Y * transform.Y.X;
        effectiveAlpha = marker.SelfModulate.A;
        for (Node current = marker; current != null; current = current.GetParent())
            if (current is CanvasItem canvas)
                effectiveAlpha *= canvas.Modulate.A;
        bool effectiveTransform = float.IsFinite(effectiveScaleX) &&
            float.IsFinite(effectiveScaleY) && float.IsFinite(determinant) &&
            effectiveScaleX > EffectiveVisibilityEpsilon &&
            effectiveScaleY > EffectiveVisibilityEpsilon &&
            Mathf.Abs(determinant) > EffectiveVisibilityEpsilon * EffectiveVisibilityEpsilon;
        return marker.Visible && marker.IsVisibleInTree() && effectiveTransform &&
            float.IsFinite(effectiveAlpha) && effectiveAlpha > EffectiveVisibilityEpsilon &&
            marker.Kind == MechanismVisualKind.ThreadAnchor &&
            marker.GlobalPosition.DistanceTo(target.GlobalPosition) <= 1f;
    }

    private static bool ExactThreadLineGeometry(ThreadLineVisual line, Vector2 expectedOrigin,
        Vector2 expectedTarget, out float originDistance, out float lineEndDistance,
        out float nearestStrandEndDistance)
    {
        originDistance = Finite(line.LastWorldStart) && Finite(expectedOrigin)
            ? line.LastWorldStart.DistanceTo(expectedOrigin) : float.PositiveInfinity;
        lineEndDistance = Finite(line.LastWorldEnd) && Finite(expectedTarget)
            ? line.LastWorldEnd.DistanceTo(expectedTarget) : float.PositiveInfinity;
        nearestStrandEndDistance = float.PositiveInfinity;
        for (int index = 0; index < line.StrandCount; index++)
        {
            Vector2[] points = line.GetStrandPoints(index);
            if (points.Length == 0) continue;
            Line2D strand = index == 0
                ? line : line.GetNode<Line2D>($"Strand{index + 1}");
            Vector2 terminal = strand.ToGlobal(points[^1]);
            if (!Finite(terminal)) continue;
            nearestStrandEndDistance = Math.Min(nearestStrandEndDistance,
                terminal.DistanceTo(expectedTarget));
        }
        return float.IsFinite(originDistance) && originDistance <= ThreadOriginTolerance &&
            float.IsFinite(lineEndDistance) && lineEndDistance <= ThreadTargetTolerance &&
            float.IsFinite(nearestStrandEndDistance) &&
            nearestStrandEndDistance <= ThreadTargetTolerance;
    }

    private static bool Finite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static Rect2 ScreenRect(Node2D room, Rect2 local, Transform2D canvas)
    {
        Vector2 a = canvas * room.ToGlobal(local.Position);
        Vector2 b = canvas * room.ToGlobal(local.End);
        return new Rect2(new Vector2(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y)),
            new Vector2(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Y - b.Y)));
    }

    private void OnUpperExitRequested(StringName exitId)
    {
        if (exitId == "to_mechanism") _upperExitRequested = true;
    }

    private void PublishTraversalStage(string stage)
    {
        if (string.Equals(_dropTraversalStage, stage, StringComparison.Ordinal)) return;
        if (_traversalProofStages.Contains(stage))
            throw new InvalidOperationException($"duplicate traversal proof stage {stage}");
        _traversalProofStages.Add(stage);
        PrologueVisualAudit.AuditTraversalProofStagePrefix(_traversalProofStages);
        GD.Print($"PROLOGUE_TRAVERSAL_PROOF_FRAME stage={stage} frame={_frame}");
        GD.Print($"TRAVERSAL_PROOF_STAGE={stage}");
    }

    private void TouchExit(string path, string stage)
    {
        RoomExit exit = _level.CurrentRoom.GetNode<RoomExit>(path);
        Fixture(exit.GlobalPosition);
        _followPlayerCameraUntilFrame = _frame + 4;
        GD.Print($"PROLOGUE_PRESENTATION_TRANSITION={stage}");
    }

    private void FixtureCheckpointTrigger()
    {
        Area2D checkpoint = _level.CurrentRoom.GetNode<Area2D>("Checkpoint");
        Fixture(checkpoint.GlobalPosition);
    }

    private FallenBellkeeper Boss() =>
        _level.CurrentRoom.GetNode<BossArenaController>("BossArenaController").CurrentBoss;

    private FallenBellkeeper TryBoss() => _level.CurrentRoomId == PrologueRoomId.FallenBellkeeperArena
        ? _level.CurrentRoom.GetNodeOrNull<BossArenaController>("BossArenaController")?.CurrentBoss
        : null;

    private void Fixture(Vector2 position)
    {
        _player = _level.Player;
        _player.GlobalPosition = position;
        _player.Velocity = Vector2.Zero;
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        _player.GetNodeOrNull<ThreadAbility>("ThreadAbility")?.ResetTransientState();
    }

    private void AddThreadObstruction(Vector2 position)
    {
        RemoveThreadObstruction();
        _threadObstruction = new StaticBody2D
        {
            Name = "ThreadPresentationObstruction",
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = 0
        };
        _threadObstruction.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(24, 180) }
        });
        _level.CurrentRoom.AddChild(_threadObstruction);
        _threadObstruction.GlobalPosition = position;
    }

    private void RemoveThreadObstruction()
    {
        if (!IsInstanceValid(_threadObstruction)) return;
        _threadObstruction.QueueFree();
        _threadObstruction = null;
    }

    private void KeepProofActorSafe()
    {
        if (_upperProofActive) return;
        if (_frame > _suppressFallsUntil || _level.CurrentRoom == null) return;
        if (_player.GlobalPosition.Y > _level.CurrentRoom.CameraBounds.End.Y + 50f)
        {
            _player.GlobalPosition = _camera.Position;
            _player.Velocity = Vector2.Zero;
        }
    }

    private void Track(RustShellSquire actor, string label)
    {
        AttackTelegraphVisual visual = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        actor.AttackFrameChanged += frame => ObserveAttack(label, visual, frame);
    }

    private void Track(GraveLampMoth actor, string label)
    {
        AttackTelegraphVisual visual = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        actor.AttackFrameChanged += frame => ObserveAttack(label, visual, frame);
    }

    private void Track(BellBackLaborer actor, string label)
    {
        AttackTelegraphVisual visual = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        actor.AttackFrameChanged += frame => ObserveAttack(label, visual, frame);
    }

    private void Track(FallenBellkeeper actor, string label)
    {
        AttackTelegraphVisual visual = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        actor.AttackFrameChanged += frame => ObserveAttack(label, visual, frame);
    }

    private void ObserveAttack(string actor, AttackTelegraphVisual visual, AttackPresentationFrame frame)
    {
        string stage = frame.Timeline.Phase switch
        {
            AttackPhase.Telegraph when !frame.Timeline.HasLockedTarget && visual.GoldVisible &&
                !visual.DangerVisible && !visual.ImpactVisible => "gold",
            AttackPhase.Telegraph when frame.Timeline.HasLockedTarget && visual.GoldVisible &&
                visual.DangerVisible && !visual.ImpactVisible => "danger",
            AttackPhase.Active when !visual.GoldVisible && !visual.DangerVisible && visual.ImpactVisible => "impact",
            AttackPhase.Recovery when !visual.GoldVisible && !visual.DangerVisible && !visual.ImpactVisible => "recovery",
            _ => string.Empty
        };
        if (stage.Length == 0) return;
        if (stage is "danger" or "impact")
            AssertDangerGeometryVisible(actor, frame.Timeline.Kind, stage, frame.Shape);
        if (frame.Timeline.AttackId == 0)
            throw new InvalidOperationException($"attack proof stage {actor}:{frame.Timeline.Kind}:{stage} has zero AttackId");
        string key = EvidenceKey(actor, frame.Timeline.Kind, stage);
        ulong recordedAttackId = frame.Timeline.AttackId;
        if (!_attackEvidence.TryAdd(key, new AttackEvidence(_frame, recordedAttackId))) return;
        PublishAttackSensoryMarker(actor, frame.Timeline.Kind, frame.Timeline.Phase);
        GD.Print($"PROLOGUE_ATTACK_EVIDENCE actor={actor} kind={frame.Timeline.Kind} stage={stage} " +
            $"frame={_frame} attack_id={recordedAttackId}");
    }

    private void PublishAttackSensoryMarker(string actor, EnemyAttackKind kind, AttackPhase phase)
    {
        string phaseName = phase switch
        {
            AttackPhase.Telegraph => "telegraph",
            AttackPhase.Active => "active",
            AttackPhase.Recovery => "recovery",
            _ => string.Empty
        };
        if (phaseName.Length == 0) return;
        string ordinary = actor switch
        {
            "RustShell" => "rust",
            "GraveLampMoth" => "moth",
            "BellBackLaborer" => "laborer",
            _ => string.Empty
        };
        if (ordinary.Length > 0)
            PublishSensoryMarker($"attack.{ordinary}.{phaseName}");
        if (phase != AttackPhase.Telegraph || actor != "Boss") return;
        string boss = kind switch
        {
            EnemyAttackKind.BossSweep => "sweep",
            EnemyAttackKind.BossLeapSlam => "leap_slam",
            EnemyAttackKind.BossGroundWave => "ground_wave",
            EnemyAttackKind.BossHammerThrow => "hammer",
            _ => string.Empty
        };
        if (boss.Length > 0) PublishSensoryMarker($"boss.{boss}");
    }

    private void AssertDangerGeometryVisible(
        string actor, EnemyAttackKind kind, string stage, AttackShapeFrame shape)
    {
        string transition = TransitionFor(kind);
        if (!ShouldJudgeMotionGeometry(transition)) return;
        var worldPoints = new List<Vector2>();
        if (shape.Kind == AttackShapeKind.ConvexPolygon)
        {
            foreach (Vector2 point in shape.Points) worldPoints.Add(point);
        }
        else
        {
            Vector2[] local = shape.Kind is AttackShapeKind.Rectangle or AttackShapeKind.GroundBand
                ? [new(-shape.Extent.X, -shape.Extent.Y), new(shape.Extent.X, -shape.Extent.Y),
                    new(shape.Extent.X, shape.Extent.Y), new(-shape.Extent.X, shape.Extent.Y)]
                : [new(-shape.Extent.X, -shape.Extent.Y), new(shape.Extent.X, -shape.Extent.Y),
                    new(shape.Extent.X, shape.Extent.Y), new(-shape.Extent.X, shape.Extent.Y),
                    new(0f, -shape.Extent.Y), new(shape.Extent.X, 0f),
                    new(0f, shape.Extent.Y), new(-shape.Extent.X, 0f)];
            foreach (Vector2 point in local)
                worldPoints.Add(shape.Origin + point.Rotated(shape.RotationRadians));
        }

        if (worldPoints.Count < 3)
            throw new InvalidOperationException(
                $"motion visibility transition={transition} frame={_frame} " +
                $"path={actor}/danger/{stage}/{shape.Kind} has no drawable points");
        AssertWorldGeometryWithinBounds(
            transition, $"{actor}/danger/{stage}/{shape.Kind}", worldPoints.ToArray());
        _dangerGeometryBoundsSamples++;
    }

    private void ObserveThreadCast(ThreadCastFrame frame, ThreadTarget2D target)
    {
        ThreadAttempt attempt = _activeThreadAttempt;
        if (attempt.Kind == ThreadAttemptKind.NoTarget && frame.State == ThreadCastState.Failed &&
            frame.Failure == ThreadFailureReason.NoTarget)
            _failedCastObserved = true;
        if (_failedCastCleaned && attempt.Kind == ThreadAttemptKind.Anchor &&
            frame.State == ThreadCastState.Preview && target != null)
        {
            _failedCastRetryReady = true;
            PrologueVisualAudit.AuditFailedCastProof(_failedCastObserved,
                _failedCastVisibleFrames, _failedCastCleaned, _failedCastRetryReady);
            GD.Print($"PROLOGUE_TRAVERSAL_FAILED_CAST_EVIDENCE state=Failed " +
                $"visible_frames={_failedCastVisibleFrames} " +
                $"visible_first={_failedCastFirstVisibleFrame} " +
                $"visible_last={_failedCastLastVisibleFrame} cleanup=True retry=True");
            GD.Print("PROLOGUE_TRAVERSAL_FAILED_CAST_OK state=Failed cleanup=True retry=True");
        }
        if (_upperProofActive)
        {
            if (frame.State == ThreadCastState.Failed)
                throw new InvalidOperationException(
                    $"Upper traversal thread failed at {_upperAssisted.Stage}: {frame.Failure}");
            if (frame.State == ThreadCastState.Windup && frame.CastId != 0 &&
                _upperCastIds.Add(frame.CastId))
                _upperAssisted.CastCount++;
            if (frame.State == ThreadCastState.Apply && frame.CastId != 0 &&
                _upperApplyIds.Add(frame.CastId))
            {
                _upperAssisted.ApplyCount++;
                _upperAssisted.ApplySeen = true;
                _upperAssisted.AppliedTargetId = frame.TargetId;
                if (target == null || target.GetInstanceId() != _upperAssisted.ExpectedTargetId ||
                    frame.TargetId != _upperAssisted.ExpectedTargetId)
                    throw new InvalidOperationException(
                        $"{_upperAssisted.Stage} applied to the wrong target; " +
                        $"expected={_upperAssisted.ExpectedTargetId} frame={frame.TargetId} " +
                        $"published={target?.GetInstanceId() ?? 0UL}");
            }
        }
        if (_recoveryCancelPending && frame.State == ThreadCastState.Idle)
        {
            ThreadLineVisual line = _player.GetNode<ThreadLineVisual>("ThreadLine");
            ThreadShuttleVisual shuttle = _player.GetNode<ThreadShuttleVisual>("VisualRoot/ThreadShuttle");
            if (_threadHitCount != _recoveryCancelHitCount || _threadFailureCount != _recoveryCancelFailureCount)
                throw new InvalidOperationException(
                    "recovery movement cancellation duplicated Hit or emitted ThreadFailed");
            if (line.ActiveStrandCount != 0 || !shuttle.IsClear || _threadAbility.HasLockedTarget)
                throw new InvalidOperationException(
                    "recovery movement cancellation left thread line, shuttle, or target residue");
            if (_threadAbility.ReadyRatio >= .999f)
                throw new InvalidOperationException(
                    "recovery movement cancellation refunded the consumed cooldown");
            _recoveryCancelPending = false;
            _recoveryFormalClearPending = true;
        }
        if (attempt.Id == 0) return;

        if (frame.State is ThreadCastState.Preview or ThreadCastState.Windup or ThreadCastState.Travel or
            ThreadCastState.Latched or ThreadCastState.Apply or ThreadCastState.Recovery)
        {
            if (_threadStateEvidence.TryAdd(frame.State,
                    new ThreadEvidence(_frame, frame.CastId, attempt.Id, attempt.Kind)))
            {
                GD.Print($"PROLOGUE_THREAD_PRESENTATION_FRAME state={frame.State} frame={_frame} " +
                    $"cast_id={frame.CastId} target_id={frame.TargetId} attempt_id={attempt.Id} " +
                    $"attempt_kind={attempt.Kind}");
                GD.Print($"PROLOGUE_THREAD_PRESENTATION_OK state={frame.State}");
            }
            PublishSensoryMarker($"thread.{frame.State.ToString().ToLowerInvariant()}");
            if (frame.State == ThreadCastState.Recovery) EndThreadAttempt();
        }

        if (frame.State == ThreadCastState.Failed &&
            frame.Failure is ThreadFailureReason.NoTarget or ThreadFailureReason.Cooldown or
                ThreadFailureReason.Obstructed &&
            _threadFailureEvidence.TryAdd(frame.Failure,
                new ThreadFailureEvidence(_frame, attempt.Id, attempt.Kind)))
        {
            PublishSensoryMarker("thread.failed");
            GD.Print($"PROLOGUE_THREAD_PRESENTATION_FRAME failure={frame.Failure} frame={_frame} " +
                $"attempt_id={attempt.Id} attempt_kind={attempt.Kind}");
            GD.Print($"PROLOGUE_THREAD_PRESENTATION_OK failure={frame.Failure}");
        }

        if (frame.State == ThreadCastState.Idle && attempt.Kind is
            ThreadAttemptKind.NoTarget or ThreadAttemptKind.Cooldown or ThreadAttemptKind.Obstructed)
        {
            ThreadFailureReason expectedFailure = attempt.Kind switch
            {
                ThreadAttemptKind.NoTarget => ThreadFailureReason.NoTarget,
                ThreadAttemptKind.Cooldown => ThreadFailureReason.Cooldown,
                _ => ThreadFailureReason.Obstructed
            };
            if (!_threadFailureEvidence.TryGetValue(expectedFailure, out ThreadFailureEvidence failure) ||
                failure.AttemptId != attempt.Id) return;
            int dwellFrames = _frame - failure.Frame;
            int minimumDwellFrames = Mathf.Max(2,
                Mathf.FloorToInt(_threadAbility.Tuning.VisualSeconds * 60f));
            if (dwellFrames < minimumDwellFrames)
                throw new InvalidOperationException(
                    $"mechanical-thread {expectedFailure} feedback was visible for only {dwellFrames} frames; " +
                    $"expected at least {minimumDwellFrames}");
            GD.Print($"PROLOGUE_THREAD_PRESENTATION_DWELL failure={expectedFailure} frames={dwellFrames} " +
                $"attempt_id={attempt.Id}");
            GD.Print($"PROLOGUE_THREAD_PRESENTATION_DWELL_OK failure={expectedFailure}");
            EndThreadAttempt();
        }

    }

    private void ValidatePendingRecoveryFormalClear()
    {
        if (!_recoveryFormalClearPending) return;
        PlayerFormalVisual formal = _player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        if (formal.CurrentClip == PlayerAnimationClip.ThreadCast)
            throw new InvalidOperationException(
                "recovery movement cancellation left the formal player in ThreadCast");
        _recoveryFormalClearPending = false;
        _recoveryCancelObserved = true;
        GD.Print($"PROLOGUE_THREAD_PRESENTATION_RECOVERY_CANCEL frame={_frame} " +
            $"hits={_threadHitCount} failures={_threadFailureCount} formal_clip={formal.CurrentClip.Name}");
        GD.Print("PROLOGUE_THREAD_PRESENTATION_RECOVERY_CANCEL_OK");
    }

    private void BeginThreadAttempt(ThreadAttemptKind kind)
    {
        if (kind == ThreadAttemptKind.None)
            throw new InvalidOperationException("mechanical-thread presentation attempt requires a named kind");
        if (_activeThreadAttempt.Id != 0)
            throw new InvalidOperationException(
                $"mechanical-thread presentation attempt {_activeThreadAttempt.Id} is still active");
        _activeThreadAttempt = new ThreadAttempt(++_nextThreadAttemptId, kind, _frame);
        _threadAttempts[kind] = _activeThreadAttempt;
        GD.Print($"PROLOGUE_THREAD_PRESENTATION_ATTEMPT kind={kind} " +
            $"attempt_id={_activeThreadAttempt.Id} frame={_frame}");
    }

    private void EndThreadAttempt() => _activeThreadAttempt = default;

    private bool HasEvidence(string actor, EnemyAttackKind kind, string stage) =>
        _attackEvidence.ContainsKey(EvidenceKey(actor, kind, stage));

    private static string EvidenceKey(string actor, EnemyAttackKind kind, string stage) =>
        $"{actor}:{kind}:{stage}";

    private void ValidateAttackEvidence()
    {
        ValidateAttackSequence("RustShell", EnemyAttackKind.RustShellThrust);
        ValidateAttackSequence("GraveLampMoth", EnemyAttackKind.MothDive);
        ValidateAttackSequence("BellBackLaborer", EnemyAttackKind.LaborerSweep);
        ValidateAttackSequence("Boss", EnemyAttackKind.BossSweep);
        ValidateAttackSequence("Boss", EnemyAttackKind.BossLeapSlam);
        ValidateAttackSequence("Boss", EnemyAttackKind.BossGroundWave);
        ValidateAttackSequence("Boss", EnemyAttackKind.BossHammerThrow);
    }

    private void ObserveBossGuidanceClearance()
    {
        FallenBellkeeper boss = TryBoss();
        if (boss == null) return;
        AttackTimelineFrame timeline = boss.CurrentFrame.AttackTimeline;
        if (timeline.Kind is not (EnemyAttackKind.BossSweep or EnemyAttackKind.BossLeapSlam or
            EnemyAttackKind.BossGroundWave or EnemyAttackKind.BossHammerThrow) ||
            timeline.Phase is not (AttackPhase.Telegraph or AttackPhase.Active)) return;

        WorldTargetMarkerVisual marker = _level.GetNode<WorldTargetMarkerVisual>(
            "Hud/Guidance/VisualRoot");
        if (_forceBossPromptCandidatesBlocked)
            ForceBossPromptCandidateBlockers(marker);
        if (!marker.IsVisibleInTree() || !marker.HasVisiblePrompt || !marker.PromptRect.HasArea())
        {
            if (IsIntentionalBossGuidanceSuppression(marker, out string suppressionDetail))
            {
                if (_bossGuidanceSuppressions.Add(timeline.Kind))
                    GD.Print($"PROLOGUE_BOSS_GUIDANCE_SUPPRESSED kind={timeline.Kind} " +
                        $"frame={_frame} {suppressionDetail}");
                RecordBossGuidanceClearFrame(timeline.Kind);
                return;
            }
            RecordBossGuidanceViolation(timeline.Kind,
                $"missing visible prompt frame={_frame} prompt={marker.PromptRect} {suppressionDetail}");
            return;
        }
        Rect2 safe = new(new Vector2(48, 40),
            _level.GetViewport().GetVisibleRect().Size - new Vector2(96, 80));
        if (!safe.Encloses(marker.PromptRect))
            RecordBossGuidanceViolation(timeline.Kind,
                $"leaves safe layout frame={_frame} prompt={marker.PromptRect} safe={safe}");

        RoomFormalVisual art = _level.CurrentRoom?.GetNodeOrNull<RoomFormalVisual>("Art");
        foreach (Rect2 protectedRegion in RoomProtectionRegionAdapter.Collect(art))
        {
            if (marker.PromptRect.Intersects(protectedRegion.Grow(8f)))
                RecordBossGuidanceViolation(timeline.Kind,
                    $"occludes protected actor/danger geometry frame={_frame} " +
                    $"prompt={marker.PromptRect} protected={protectedRegion}");
        }

        Control bossHud = _level.GetNode<Control>("Hud/FormalHud/Boss");
        if (marker.PromptRect.Intersects(bossHud.GetGlobalRect()))
            RecordBossGuidanceViolation(timeline.Kind,
                $"overlaps bottom Boss HUD frame={_frame}");

        if (_bossGuidanceViolations.ContainsKey(timeline.Kind)) return;

        RecordBossGuidanceClearFrame(timeline.Kind);
    }

    private void ForceBossPromptCandidateBlockers(WorldTargetMarkerVisual marker)
    {
        if (GuidanceTargetsField.GetValue(marker) is not IReadOnlyList<PrologueGuidanceTargetView> targets ||
            targets.Count != 1) return;

        RoomFormalVisual art = _level.CurrentRoom?.GetNodeOrNull<RoomFormalVisual>("Art");
        if (art == null) return;
        Rect2 safe = new(new Vector2(48, 40),
            _level.GetViewport().GetVisibleRect().Size - new Vector2(96, 80));
        Vector2 promptSize = marker.IsExpandedTeaching ? new Vector2(480, 88) : new Vector2(248, 48);
        IReadOnlyList<Rect2> candidates = EnumerateIndependentPromptCandidates(safe,
            targets[0].ViewportPosition, promptSize);
        if (candidates.Count != 10)
            throw new InvalidOperationException(
                $"Boss prompt blocker fixture expected ten candidates, got {candidates.Count}");

        while (_forcedBossPromptBlockers.Count < candidates.Count)
        {
            int index = _forcedBossPromptBlockers.Count;
            var blocker = new Area2D
            {
                Name = $"Task3BossPromptCandidateBlocker{index}",
                CollisionLayer = CombatCollisionLayers.EnemyAttack,
                CollisionMask = CombatCollisionLayers.None,
                Monitoring = true
            };
            var shape = new RectangleShape2D();
            blocker.AddChild(new CollisionShape2D
            {
                Name = "Collision",
                Shape = shape
            });
            art.AddChild(blocker);
            _forcedBossPromptBlockers.Add(blocker);
            _forcedBossPromptBlockerShapes.Add(shape);
        }

        Transform2D screenToWorld = art.GetGlobalTransformWithCanvas().AffineInverse();
        for (int index = 0; index < candidates.Count; index++)
        {
            _forcedBossPromptBlockers[index].GlobalPosition = screenToWorld * candidates[index].GetCenter();
            _forcedBossPromptBlockerShapes[index].Size = candidates[index].Size;
        }

        _level.GetNode<PrologueGuidanceController>("Guidance").RefreshNow();
        if (!_forcedBossPromptBlockersLogged)
        {
            _forcedBossPromptBlockersLogged = true;
            GD.Print("PROLOGUE_BOSS_GUIDANCE_INDEPENDENT_BLOCKERS_OK candidates=10 " +
                $"padding={GuidancePromptProtectionPadding}");
        }
    }

    private bool IsIntentionalBossGuidanceSuppression(WorldTargetMarkerVisual marker,
        out string detail)
    {
        detail = string.Empty;
        PrologueGuidanceController guidance = _level.GetNode<PrologueGuidanceController>("Guidance");
        if (!marker.IsVisibleInTree() || !guidance.CurrentInstruction.Visible ||
            guidance.CurrentInstruction.Targets.Count != 1 ||
            marker.RenderedTargetCount != 1 || string.IsNullOrWhiteSpace(marker.RenderedStepText) ||
            GuidanceTargetsField.GetValue(marker) is not IReadOnlyList<PrologueGuidanceTargetView> targets ||
            targets.Count != 1)
        {
            detail = "guidance model/target was not a live single-target Boss-arena instruction";
            return false;
        }

        PrologueGuidanceTargetKey targetKey = guidance.CurrentInstruction.Targets[0];
        if (targetKey is not (PrologueGuidanceTargetKey.BossBody or PrologueGuidanceTargetKey.BossChain))
        {
            detail = $"guidance target is not a live Boss attack target key={targetKey}";
            return false;
        }

        BossArenaController arena = _level.CurrentSectorRoot?.GetNodeOrNull<BossArenaController>(
            "Gameplay/BossArenaController");
        if (arena?.CurrentBoss == null || !arena.IsEncounterActive)
        {
            detail = "Boss arena is inactive while prompt is suppressed";
            return false;
        }
        if (targetKey == PrologueGuidanceTargetKey.BossChain &&
            arena.CurrentBoss.CurrentFrame.State != FallenBellkeeperState.HammerThrown)
        {
            detail = "BossChain prompt is stale outside its exposed HammerThrown state";
            return false;
        }
        if (!_level.CurrentSector.HasValue ||
            !PrologueGuidanceTargetResolver.TryResolve(_level.CurrentSectorRoot,
                _level.CurrentSector.Value, targetKey, out Node2D resolvedTarget))
        {
            detail = $"Boss prompt target could not resolve key={targetKey}";
            return false;
        }
        Camera2D guidanceCamera = _level.Player?.GetNodeOrNull<Camera2D>("Camera2D");
        if (guidanceCamera == null ||
            targets[0].ViewportPosition.DistanceTo(
                guidanceCamera.GetCanvasTransform() * resolvedTarget.GlobalPosition) > .1f)
        {
            detail = $"Boss prompt target view is stale key={targetKey}";
            return false;
        }

        Rect2 safe = new(new Vector2(48, 40),
            _level.GetViewport().GetVisibleRect().Size - new Vector2(96, 80));
        Vector2 promptSize = marker.IsExpandedTeaching ? new Vector2(480, 88) : new Vector2(248, 48);
        IReadOnlyList<Rect2> protectedRegions = CollectBossGuidanceProtectedRegions();
        IReadOnlyList<Rect2> candidates = EnumerateIndependentPromptCandidates(safe,
            targets[0].ViewportPosition, promptSize);
        for (int index = 0; index < candidates.Count; index++)
        {
            foreach (Rect2 protectedRegion in protectedRegions)
            {
                if (!candidates[index].Intersects(
                        protectedRegion.Grow(GuidancePromptProtectionPadding))) continue;
                goto CandidateBlocked;
            }
            detail = $"independent clear candidate index={index} rect={candidates[index]}";
            return false;

            CandidateBlocked: ;
        }

        Control backdrop = marker.GetNodeOrNull<Control>("Backdrop");
        Control topRule = marker.GetNodeOrNull<Control>("TopRule");
        Label text = marker.GetNodeOrNull<Label>("Text");
        if (marker.HasVisiblePrompt || marker.PromptRect.HasArea() || backdrop == null || topRule == null ||
            text == null || backdrop.Visible || topRule.Visible || text.Visible ||
            !string.IsNullOrEmpty(text.Text))
        {
            detail = "zero-area placement retained visible or stale card content";
            return false;
        }

        detail = $"independent_candidates={candidates.Count} blocked={candidates.Count} " +
            "zero_area=true card_hidden=true stale_text=false";
        return true;
    }

    private IReadOnlyList<Rect2> CollectBossGuidanceProtectedRegions()
    {
        RoomFormalVisual art = _level.CurrentRoom?.GetNodeOrNull<RoomFormalVisual>("Art");
        var protectedRegions = new List<Rect2>(RoomProtectionRegionAdapter.Collect(art));
        Control bossHud = _level.GetNode<Control>("Hud/FormalHud/Boss");
        if (bossHud.Visible) protectedRegions.Add(bossHud.GetGlobalRect());
        return protectedRegions;
    }

    private static IReadOnlyList<Rect2> EnumerateIndependentPromptCandidates(Rect2 safeArea,
        Vector2 target, Vector2 requestedSize)
    {
        Vector2 size = new(
            Mathf.Clamp(requestedSize.X, 1f, Mathf.Max(1f, safeArea.Size.X)),
            Mathf.Clamp(requestedSize.Y, 1f, Mathf.Max(1f, safeArea.Size.Y)));
        Vector2 half = size * .5f;
        Vector2[] centers =
        [
            target + Vector2.Up * (half.Y + GuidancePromptTargetGap),
            target + Vector2.Right * (half.X + GuidancePromptTargetGap),
            target + Vector2.Down * (half.Y + GuidancePromptTargetGap),
            target + Vector2.Left * (half.X + GuidancePromptTargetGap),
            target + new Vector2(half.X + GuidancePromptTargetGap,
                half.Y + GuidancePromptTargetGap * 4f),
            target + new Vector2(-half.X - GuidancePromptTargetGap,
                half.Y + GuidancePromptTargetGap * 4f),
            target + new Vector2(half.X + GuidancePromptTargetGap,
                -half.Y - GuidancePromptTargetGap * 4f),
            target + new Vector2(-half.X - GuidancePromptTargetGap,
                -half.Y - GuidancePromptTargetGap * 4f),
            new(safeArea.End.X - half.X, safeArea.Position.Y + half.Y),
            new(safeArea.Position.X + half.X, safeArea.Position.Y + half.Y)
        ];
        var candidates = new List<Rect2>(centers.Length);
        foreach (Vector2 center in centers)
            candidates.Add(ClampIndependentPromptCandidate(new Rect2(center - half, size), safeArea));
        return candidates;
    }

    private static Rect2 ClampIndependentPromptCandidate(Rect2 rect, Rect2 safeArea)
    {
        Vector2 position = new(
            Mathf.Clamp(rect.Position.X, safeArea.Position.X, safeArea.End.X - rect.Size.X),
            Mathf.Clamp(rect.Position.Y, safeArea.Position.Y, safeArea.End.Y - rect.Size.Y));
        return new Rect2(position, rect.Size);
    }

    private void RecordBossGuidanceClearFrame(EnemyAttackKind kind)
    {
        BossGuidanceEvidence evidence = _bossGuidanceEvidence.GetValueOrDefault(kind) ?? new();
        evidence.CurrentRun = evidence.LastFrame == _frame - 1 ? evidence.CurrentRun + 1 : 1;
        evidence.LastFrame = _frame;
        evidence.MaxRun = Mathf.Max(evidence.MaxRun, evidence.CurrentRun);
        _bossGuidanceEvidence[kind] = evidence;
    }

    private void ValidateBossGuidanceClearance()
    {
        var failures = new List<string>();
        foreach (EnemyAttackKind kind in new[]
                 {
                     EnemyAttackKind.BossSweep, EnemyAttackKind.BossLeapSlam,
                     EnemyAttackKind.BossGroundWave, EnemyAttackKind.BossHammerThrow
                 })
        {
            BossGuidanceEvidence evidence = _bossGuidanceEvidence.GetValueOrDefault(kind) ?? new();
            if (evidence.MaxRun < 3)
                failures.Add($"{kind}: fewer than three protected-clear consecutive frames max={evidence.MaxRun}");
            if (_bossGuidanceViolations.TryGetValue(kind, out List<string> violations))
                failures.Add($"{kind}: {violations.Count} real-route violation(s); first={violations[0]}");
        }
        if (failures.Count > 0)
            throw new InvalidOperationException("REAL_BOSS_GUIDANCE_ROUTE_ASSERTION_FAIL " +
                string.Join(" | ", failures));
        foreach (EnemyAttackKind kind in new[]
                 {
                     EnemyAttackKind.BossSweep, EnemyAttackKind.BossLeapSlam,
                     EnemyAttackKind.BossGroundWave, EnemyAttackKind.BossHammerThrow
                 })
        {
            BossGuidanceEvidence evidence = _bossGuidanceEvidence.GetValueOrDefault(kind) ?? new();
            GD.Print($"PROLOGUE_BOSS_GUIDANCE_CLEAR_OK kind={kind} " +
                $"consecutive_frames={evidence.MaxRun}");
        }
    }

    private void RecordBossGuidanceViolation(EnemyAttackKind kind, string detail)
    {
        if (_bossGuidanceViolations.ContainsKey(kind)) return;
        var violations = new List<string>();
        _bossGuidanceViolations[kind] = violations;
        violations.Add(detail);
        GD.Print($"PROLOGUE_BOSS_GUIDANCE_CLEAR_FAIL kind={kind} {detail}");
    }

    /// <summary>Samples the rendered rig inputs once per fixed 60 FPS presentation frame.</summary>
    private void ObserveMotion()
    {
        ObserveMotionTree(_level);
        ObserveGuidanceBounds();
        ObserveBossGuidanceClearance();
    }

    private void ObserveMotionTree(Node node)
    {
        switch (node)
        {
            case PlayerFormalVisual player:
                ObservePlayerMotion(player);
                break;
            case RustShellFormalVisual rust:
                ObserveRustMotion(rust);
                break;
            case GraveLampMothFormalVisual moth:
                ObserveMothMotion(moth);
                break;
            case BellBackLaborerFormalVisual laborer:
                ObserveLaborerMotion(laborer);
                break;
            case FallenBellkeeperFormalVisual boss:
                ObserveBossMotion(boss);
                break;
        }
        foreach (Node child in node.GetChildren()) ObserveMotionTree(child);
    }

    private void ObservePlayerMotion(PlayerFormalVisual visual)
    {
        PublishPlayerSensoryMarkers(visual);
        string transition = visual.CurrentClip == PlayerAnimationClip.Hurt ||
            visual.CurrentPlayerFrame.Action.State == PlayerActionState.Hurt ||
            visual.CurrentPlayerFrame.Locomotion.State == PlayerLocomotionState.Recovery
            ? "hurt_recovery"
            : visual.CurrentPlayerFrame.Action.State is PlayerActionState.GroundAttack or
                PlayerActionState.AirAttack or PlayerActionState.DownAttack
                ? "attack_hit_whiff"
                : visual.CurrentPlayerFrame.Action.State is PlayerActionState.ThreadPreview or
                    PlayerActionState.ThreadWindup or PlayerActionState.ThreadThrow or
                    PlayerActionState.ThreadLatch or PlayerActionState.ThreadApply or
                    PlayerActionState.ThreadFailed
                    ? "thread_tap_hold"
                    : visual.CurrentPlayerFrame.Locomotion.State is PlayerLocomotionState.StartRun or
                        PlayerLocomotionState.Run or PlayerLocomotionState.Brake or
                        PlayerLocomotionState.JumpStart or PlayerLocomotionState.Rise or
                        PlayerLocomotionState.Apex or PlayerLocomotionState.Fall or
                        PlayerLocomotionState.SoftLand or PlayerLocomotionState.HardLand
                        ? "run_start_stop_jump_land"
                        : string.Empty;
        foreach (string slot in new[] { "Body", "NearArm", "FarArm", "NearLeg", "FarLeg", "Weapon",
                     "Mantle", "ShoulderCore", "Lantern", "Tether", "ThreadShuttle" })
        {
            Node2D part = visual.GetNodeOrNull<Node2D>(slot) ??
                throw new InvalidOperationException($"player motion proof lost stable slot {slot}");
            ObserveNodeMotion(transition, $"player/{slot}", part,
                transition.Length > 0 && slot == "Weapon");
        }
        if (transition.Length > 0)
        {
            Vector2 heading = visual.GetNode<Node2D>("Body").ToGlobal(new Vector2(10f, -19f));
            if (_forceVisibilityBoundsFailure && _frame == 300)
                heading += new Vector2(5000f, 0f);
            AssertWorldGeometryWithinBounds(transition, "player/feet-heading-weapon",
                visual.GetNode<Node2D>("NearLeg").ToGlobal(new Vector2(0f, 22f)),
                visual.GetNode<Node2D>("FarLeg").ToGlobal(new Vector2(0f, 20f)),
                heading,
                visual.GetNode<Node2D>("Weapon").ToGlobal(new Vector2(-2f, 0f)),
                visual.GetNode<Node2D>("Weapon").ToGlobal(new Vector2(45f, -6f)));
        }
    }

    private void PublishPlayerSensoryMarkers(PlayerFormalVisual visual)
    {
        PlayerLocomotionState locomotion = visual.CurrentPlayerFrame.Locomotion.State;
        PlayerActionState action = visual.CurrentPlayerFrame.Action.State;
        if (locomotion is PlayerLocomotionState.StartRun or PlayerLocomotionState.Run)
            PublishSensoryMarker("player.run");
        if (locomotion == PlayerLocomotionState.Brake)
            PublishSensoryMarker("player.brake");
        if (locomotion is PlayerLocomotionState.JumpStart or PlayerLocomotionState.Rise or
            PlayerLocomotionState.Apex or PlayerLocomotionState.Fall)
            PublishSensoryMarker("player.jump");
        if (locomotion is PlayerLocomotionState.SoftLand or PlayerLocomotionState.HardLand)
            PublishSensoryMarker("player.land");
        if (action is PlayerActionState.GroundAttack or PlayerActionState.AirAttack or
            PlayerActionState.DownAttack)
            PublishSensoryMarker("player.attack");
        if (action is PlayerActionState.ThreadPreview or PlayerActionState.ThreadWindup or
            PlayerActionState.ThreadThrow or PlayerActionState.ThreadLatch or
            PlayerActionState.ThreadApply or PlayerActionState.ThreadFailed)
            PublishSensoryMarker("player.thread");
        if (visual.CurrentClip == PlayerAnimationClip.Hurt || action == PlayerActionState.Hurt ||
            locomotion == PlayerLocomotionState.Recovery)
            PublishSensoryMarker("player.hurt");
    }

    private void ObserveSensoryLandmarks()
    {
        string room = _level.CurrentRoomId switch
        {
            PrologueRoomId.AwakeningCourtyard => "awakening_courtyard",
            PrologueRoomId.BrokenToothCorridor => "broken_tooth_corridor",
            PrologueRoomId.UpperPendulum => "upper_pendulum",
            PrologueRoomId.LowerBuriedMachinery => "lower_buried_machinery",
            PrologueRoomId.BellChainMechanism => "bell_chain_mechanism",
            PrologueRoomId.FallenBellkeeperArena => "fallen_bellkeeper_arena",
            _ => string.Empty
        };
        if (room.Length > 0) PublishSensoryMarker($"room.{room}");

        WorldTargetMarkerVisual guidance = _level.GetNode<WorldTargetMarkerVisual>(
            "Hud/Guidance/VisualRoot");
        if (guidance.IsExpandedTeaching)
            PublishSensoryMarker("hud.guidance_expanded");
        else if (_flow.State == PrologueGameFlowState.Playing)
            PublishSensoryMarker("hud.guidance_compact");

        PrologueFormalHud formal = _level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        if (formal.GetNode<Control>("Boss").Visible)
            PublishSensoryMarker("hud.boss");
        if (_level.GetNode<PrologueCompletionController>("Completion").CompletionCount == 1 &&
            formal.GetNode<Control>("Completion/Panel").Visible)
            PublishSensoryMarker("hud.completion_clear");
    }

    private void PublishSensoryMarker(string name)
    {
        if (!_sensoryCaptureFrames.TryAdd(name, _frame)) return;
        GD.Print($"PROLOGUE_SENSORY_CAPTURE={name}");
        GD.Print($"PROLOGUE_SENSORY_CAPTURE_FRAME name={name} frame={_frame}");
    }

    private void ObserveRustMotion(RustShellFormalVisual visual)
    {
        string transition = visual.CurrentAttackFrame.Timeline.Phase == AttackPhase.None
            ? string.Empty : "rust_shell_attack";
        foreach (string slot in new[] { "Body", "Shell", "Weapon", "Corruption" })
            ObserveNodeMotion(transition, $"rust/{slot}", visual.GetNode<Node2D>(slot),
                transition.Length > 0 && slot == "Weapon");
        if (transition.Length > 0)
        {
            Node2D weapon = visual.GetNode<Node2D>("Weapon");
            AssertWorldGeometryWithinBounds(transition, "rust/weapon",
                weapon.ToGlobal(Vector2.Zero), weapon.ToGlobal(new Vector2(38f, -2f)));
        }
    }

    private void ObserveMothMotion(GraveLampMothFormalVisual visual)
    {
        string transition = visual.CurrentAttackFrame.Timeline.Phase == AttackPhase.None
            ? string.Empty : "grave_lamp_moth_dive";
        foreach (string slot in new[] { "Body", "Wings", "Wings/FarWing", "Wings/NearWing", "Lamp", "Corruption" })
            ObserveNodeMotion(transition, $"moth/{slot}", visual.GetNode<Node2D>(slot),
                transition.Length > 0 && slot == "Lamp");
        if (transition.Length > 0)
        {
            AssertMothSensoryVisibility(visual);
            Node2D lamp = visual.GetNode<Node2D>("Lamp");
            AssertWorldGeometryWithinBounds(transition, "moth/lamp",
                lamp.ToGlobal(new Vector2(-7f, -6f)), lamp.ToGlobal(new Vector2(7f, 8f)));
        }
    }

    private void AssertMothSensoryVisibility(GraveLampMothFormalVisual visual)
    {
        if (!ShouldJudgeMotionGeometry("grave_lamp_moth_dive")) return;
        if (_forceMothOffscreen)
            throw new InvalidOperationException(
                "Moth sensory segment is intentionally staged off-screen for the RED visibility check");
        Node2D body = visual.GetNode<Node2D>("Body");
        if (!body.IsVisibleInTree())
            throw new InvalidOperationException("Moth sensory segment hides the real Moth body");
        Vector2 screen = _level.GetViewport().GetCanvasTransform() * body.GlobalPosition;
        Vector2 viewport = _level.GetViewport().GetVisibleRect().Size;
        Rect2 proofFrame = new(viewport * .2f, viewport * .6f);
        if (!proofFrame.HasPoint(screen))
            throw new InvalidOperationException(
                $"Moth sensory segment leaves the real Moth outside the central proof frame: {screen}");
        _mothSensoryVisibilityObserved = true;
    }

    private void ObserveLaborerMotion(BellBackLaborerFormalVisual visual)
    {
        string transition = visual.CurrentAttackFrame.Timeline.Phase == AttackPhase.None
            ? string.Empty : "bell_back_laborer_sweep";
        foreach (string slot in new[] { "Feet", "Body", "Bell", "Weapon", "Corruption" })
            ObserveNodeMotion(transition, $"laborer/{slot}", visual.GetNode<Node2D>(slot),
                transition.Length > 0 && slot == "Weapon");
        if (transition.Length > 0)
        {
            Node2D weapon = visual.GetNode<Node2D>("Weapon");
            AssertWorldGeometryWithinBounds(transition, "laborer/weapon",
                weapon.ToGlobal(new Vector2(0f, -6f)),
                weapon.ToGlobal(new Vector2(68f, -12f)),
                weapon.ToGlobal(new Vector2(68f, 12f)));
        }
    }

    private void ObserveBossMotion(FallenBellkeeperFormalVisual visual)
    {
        string transition = visual.CurrentAttackFrame.Timeline.Kind switch
        {
            EnemyAttackKind.BossSweep => "boss_sweep",
            EnemyAttackKind.BossLeapSlam => "boss_leap_slam",
            EnemyAttackKind.BossGroundWave => "boss_ground_wave",
            EnemyAttackKind.BossHammerThrow => "boss_hammer_throw",
            _ => string.Empty
        };
        if (visual.CurrentAttackFrame.Timeline.Phase == AttackPhase.None) transition = string.Empty;
        foreach (string slot in new[] { "Body", "Core", "ArmorShell", "Chain", "Hammer" })
        {
            Node2D part = visual.GetNode<Node2D>(slot);
            ObserveNodeMotion(transition, $"boss/{slot}", part,
                transition.Length > 0 && slot == "Hammer");
        }
        if (transition.Length > 0)
        {
            Node2D hammer = visual.GetNode<Node2D>("Hammer");
            AssertWorldGeometryWithinBounds(transition, "boss/hammer",
                hammer.ToGlobal(new Vector2(-8f, 35f)),
                hammer.ToGlobal(new Vector2(21f, -62f)),
                hammer.ToGlobal(new Vector2(85f, -18f)));
        }
    }

    private void AssertWorldGeometryWithinBounds(
        string transition, string path, params Vector2[] worldPoints)
    {
        if (!ShouldJudgeMotionGeometry(transition)) return;
        Rect2 safe = ScreenBounds(4f);
        Transform2D canvas = _level.GetViewport().GetCanvasTransform();
        for (int index = 0; index < worldPoints.Length; index++)
        {
            Vector2 worldPoint = worldPoints[index];
            Vector2 screenPoint = canvas * worldPoint;
            if (!safe.HasPoint(screenPoint))
                throw new InvalidOperationException(
                    $"motion visibility transition={transition} frame={_frame} path={path} " +
                    $"point={index} world={worldPoint} screen={screenPoint} safe={safe}");
        }
        EvidenceFor(transition).JudgedBoundsFrames++;
    }

    private bool ShouldJudgeMotionGeometry(string transition)
    {
        if (transition.Length == 0) return false;
        foreach (MotionJudgementWindow window in MotionJudgementWindows)
            if (window.Transition == transition &&
                _frame >= window.FirstFrame && _frame <= window.LastFrame)
                return true;
        return false;
    }

    private static string TransitionFor(EnemyAttackKind kind) => kind switch
    {
        EnemyAttackKind.RustShellThrust => "rust_shell_attack",
        EnemyAttackKind.MothDive => "grave_lamp_moth_dive",
        EnemyAttackKind.LaborerSweep => "bell_back_laborer_sweep",
        EnemyAttackKind.BossSweep => "boss_sweep",
        EnemyAttackKind.BossLeapSlam => "boss_leap_slam",
        EnemyAttackKind.BossGroundWave => "boss_ground_wave",
        EnemyAttackKind.BossHammerThrow => "boss_hammer_throw",
        _ => string.Empty
    };

    private Rect2 ScreenBounds(float margin)
    {
        Vector2 size = _level.GetViewport().GetVisibleRect().Size;
        return new Rect2(new Vector2(margin, margin), size - Vector2.One * margin * 2f);
    }

    private void ObserveGuidanceBounds()
    {
        string lifecycle = RequiredTargetLifecycleForFrame();
        WorldTargetMarkerVisual marker = _level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        if (_forcedHiddenTargetAncestor != null &&
            !string.Equals(_forceHiddenTargetAncestorLifecycle, lifecycle, StringComparison.Ordinal))
        {
            _forcedHiddenTargetAncestor.Visible = true;
            _forcedHiddenTargetAncestor = null;
        }
        if (lifecycle.Length == 0) return;
        if (string.Equals(_forceHiddenTargetLifecycle, lifecycle, StringComparison.Ordinal))
            marker.Visible = false;
        if (string.Equals(_forceHiddenTargetAncestorLifecycle, lifecycle, StringComparison.Ordinal))
        {
            marker.Visible = true;
            _forcedHiddenTargetAncestor ??= FindVisibleCanvasAncestor(marker);
            _forcedHiddenTargetAncestor.Visible = false;
        }
        bool effectiveVisibility = marker.IsVisibleInTree();
        if (!marker.Visible || !effectiveVisibility)
            throw new InvalidOperationException(
                $"target visibility lifecycle={lifecycle} frame={_frame} path={marker.GetPath()} " +
                $"local_visibility={marker.Visible.ToString().ToLowerInvariant()} " +
                $"effective_visibility={effectiveVisibility.ToString().ToLowerInvariant()}");
        if (marker.RenderedTargetCount == 0)
            throw new InvalidOperationException(
                $"target visibility lifecycle={lifecycle} frame={_frame} path=guidance targets=0");
        Rect2 viewport = new(Vector2.Zero, _level.GetViewport().GetVisibleRect().Size);
        if (!marker.GetGlobalRect().Encloses(viewport) || marker.Size.X <= 96f || marker.Size.Y <= 96f)
            throw new InvalidOperationException(
                $"motion visibility transition=capture/{lifecycle} frame={_frame} " +
                $"path=guidance/drawing-surface rect={marker.GetGlobalRect()} viewport={viewport}");
        if (GuidanceTargetsField.GetValue(marker) is not
            IReadOnlyList<PrologueGuidanceTargetView> targets ||
            targets.Count != marker.RenderedTargetCount)
            throw new InvalidOperationException(
                $"motion visibility transition=capture/{lifecycle} frame={_frame} " +
                $"path=guidance/targets cannot inspect {marker.RenderedTargetCount} rendered targets");

        Rect2 screenSafe = ScreenBounds(4f);
        Transform2D toScreen = marker.GetGlobalTransformWithCanvas();
        Rect2 arrowSafe = new(new Vector2(48f, 48f), marker.Size - new Vector2(96f, 96f));
        for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
        {
            PrologueGuidanceTargetView target = targets[targetIndex];
            Vector2 point = target.IsOnScreen ? target.ViewportPosition : new Vector2(
                Mathf.Clamp(target.ViewportPosition.X, arrowSafe.Position.X, arrowSafe.End.X),
                Mathf.Clamp(target.ViewportPosition.Y, arrowSafe.Position.Y, arrowSafe.End.Y));
            Vector2[] localGeometry;
            string geometryPath;
            if (target.IsOnScreen)
            {
                const float diamondRadiusWithStroke = 13f;
                localGeometry =
                [
                    point + Vector2.Up * diamondRadiusWithStroke,
                    point + Vector2.Right * diamondRadiusWithStroke,
                    point + Vector2.Down * diamondRadiusWithStroke,
                    point + Vector2.Left * diamondRadiusWithStroke
                ];
                geometryPath = "on-screen-diamond";
            }
            else
            {
                Vector2 forward = target.Direction == Vector2.Zero
                    ? Vector2.Right : target.Direction.Normalized();
                Vector2 side = forward.Orthogonal();
                localGeometry =
                [
                    point + forward * 18f,
                    point - forward * 12f + side * 10f,
                    point - forward * 12f - side * 10f
                ];
                geometryPath = "off-screen-arrow";
            }
            for (int pointIndex = 0; pointIndex < localGeometry.Length; pointIndex++)
            {
                Vector2 screen = toScreen * localGeometry[pointIndex];
                if (!screenSafe.HasPoint(screen))
                    throw new InvalidOperationException(
                        $"motion visibility transition=capture/{lifecycle} frame={_frame} " +
                        $"path=guidance/target[{targetIndex}]/{geometryPath} point={pointIndex} " +
                        $"screen={screen} safe={screenSafe}");
            }
        }
        _targetMarkerBoundsSamples[lifecycle] =
            _targetMarkerBoundsSamples.GetValueOrDefault(lifecycle) + 1;
    }

    private static CanvasItem FindVisibleCanvasAncestor(CanvasItem marker)
    {
        for (Node ancestor = marker.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor is CanvasItem canvas && canvas.Visible)
                return canvas;
        throw new InvalidOperationException(
            $"required target marker {marker.GetPath()} has no visible CanvasItem ancestor for mutation");
    }

    private string RequiredTargetLifecycleForFrame()
    {
        foreach (TargetJudgementWindow window in RequiredTargetWindows)
            if (_frame >= window.FirstFrame && _frame <= window.LastFrame)
                return window.Lifecycle;
        return string.Empty;
    }

    private void ObserveNodeMotion(string transition, string slot, Node2D node, bool mustRemainVisible)
    {
        if (mustRemainVisible && !node.Visible)
            throw new InvalidOperationException($"{transition} hides its required weapon slot {slot}");
        ObserveMotionSample(transition, $"{node.GetInstanceId()}/{slot}",
            new MotionSample(_frame, transition, node.Position, node.Rotation, node.Scale));
    }

    private void ObserveMotionSample(string transition, string key, MotionSample sample)
    {
        if (sample.Scale.X <= 0f || sample.Scale.Y <= 0f ||
            !Mathf.IsEqualApprox(sample.Scale.X, sample.Scale.Y))
            throw new InvalidOperationException($"{transition} applies non-positive or stretched scale {sample.Scale}");
        if (!_previousMotion.TryGetValue(key, out MotionSample previous) || previous.Frame != _frame - 1)
        {
            _previousMotion[key] = sample;
            return;
        }
        if (!string.Equals(previous.Transition, sample.Transition, StringComparison.Ordinal))
        {
            if (previous.Transition.Length > 0)
                EvidenceFor(previous.Transition).ExitBoundarySamples++;
            if (sample.Transition.Length > 0)
                EvidenceFor(sample.Transition).EntryBoundarySamples++;
        }
        _previousMotion[key] = sample;
        if (previous.Transition.Length > 0)
            RecordMotionDelta(previous.Transition, key, previous, sample);
        if (sample.Transition.Length > 0 &&
            !string.Equals(previous.Transition, sample.Transition, StringComparison.Ordinal))
            RecordMotionDelta(sample.Transition, key, previous, sample);
    }

    private void RecordMotionDelta(
        string transition, string key, MotionSample previous, MotionSample sample)
    {
        MotionEvidence evidence = EvidenceFor(transition);
        evidence.Samples++;
        float displacement = previous.Position.DistanceTo(sample.Position);
        if (displacement > evidence.MaxSlotDisplacement)
        {
            evidence.MaxSlotDisplacement = displacement;
            evidence.WorstDisplacementSlot = key;
            evidence.WorstDisplacementFrame = _frame;
        }
        float rotationDelta = Mathf.Abs(Mathf.AngleDifference(previous.Rotation, sample.Rotation));
        if (rotationDelta > evidence.MaxSlotRotation)
        {
            evidence.MaxSlotRotation = rotationDelta;
            evidence.WorstSlot = key;
            evidence.WorstFrame = _frame;
        }
        evidence.MaxSlotScaleDelta = Mathf.Max(evidence.MaxSlotScaleDelta,
            Mathf.Max(Mathf.Abs(previous.Scale.X - sample.Scale.X), Mathf.Abs(previous.Scale.Y - sample.Scale.Y)));
    }

    private void ValidateMotionEvidence()
    {
        foreach (string transition in RequiredMotionTransitions)
        {
            if (!_motionEvidence.TryGetValue(transition, out MotionEvidence evidence) || evidence.Samples == 0)
                throw new InvalidOperationException($"normal-speed motion proof never sampled {transition}");
            if (evidence.EntryBoundarySamples == 0 || evidence.ExitBoundarySamples == 0)
                throw new InvalidOperationException(
                    $"normal-speed motion boundary proof skipped entry/exit for {transition}: " +
                    $"entries={evidence.EntryBoundarySamples} exits={evidence.ExitBoundarySamples}");
            if (evidence.JudgedBoundsFrames == 0)
                throw new InvalidOperationException(
                    $"normal-speed visibility proof never judged required geometry for {transition}");
            if (evidence.MaxSlotDisplacement > 32f || evidence.MaxSlotRotation > .95f ||
                evidence.MaxSlotScaleDelta > .25f)
                throw new InvalidOperationException($"{transition} exceeded 60 FPS transition budget: " +
                    $"position={evidence.MaxSlotDisplacement:F3} rotation={evidence.MaxSlotRotation:F3} " +
                    $"scale={evidence.MaxSlotScaleDelta:F3} " +
                    $"position_slot={evidence.WorstDisplacementSlot} position_frame={evidence.WorstDisplacementFrame} " +
                    $"rotation_slot={evidence.WorstSlot} rotation_frame={evidence.WorstFrame}");
            GD.Print($"PROLOGUE_MOTION transition={transition} samples={evidence.Samples} " +
                $"max_slot_displacement={evidence.MaxSlotDisplacement:F3} " +
                $"max_slot_rotation={evidence.MaxSlotRotation:F3} " +
                $"max_slot_scale_delta={evidence.MaxSlotScaleDelta:F3} " +
                $"entry_boundaries={evidence.EntryBoundarySamples} " +
                $"exit_boundaries={evidence.ExitBoundarySamples} " +
                $"judged_bounds_frames={evidence.JudgedBoundsFrames}");
            GD.Print($"PROLOGUE_MOTION_OK transition={transition}");
        }
        int targetTotal = 0;
        foreach (TargetJudgementWindow window in RequiredTargetWindows)
        {
            int samples = _targetMarkerBoundsSamples.GetValueOrDefault(window.Lifecycle);
            if (samples == 0)
                throw new InvalidOperationException(
                    $"target visibility lifecycle={window.Lifecycle} has zero judged frames");
            targetTotal += samples;
            GD.Print($"PROLOGUE_TARGET_VISIBILITY lifecycle={window.Lifecycle} " +
                $"judged_frames={samples}");
        }
        if (_dangerGeometryBoundsSamples == 0)
            throw new InvalidOperationException(
                "normal-speed visibility proof missed danger geometry");
        GD.Print($"PROLOGUE_VISIBILITY target_judged_frames={targetTotal} " +
            $"danger_judged_frames={_dangerGeometryBoundsSamples}");
        GD.Print("PROLOGUE_VISIBILITY_OK");
    }

    private MotionEvidence EvidenceFor(string transition) =>
        _motionEvidence.TryGetValue(transition, out MotionEvidence current)
            ? current : _motionEvidence[transition] = new MotionEvidence();

    private void ValidateThreadEvidence()
    {
        if (_activeThreadAttempt.Id != 0)
            throw new InvalidOperationException(
                $"mechanical-thread presentation attempt {_activeThreadAttempt.Id} never reached a terminal frame");
        if (!_forceThreadAttemptSplice && (!_recoveryCancelObserved || _recoveryCancelPending))
            throw new InvalidOperationException(
                "mechanical-thread presentation never observed the real Recovery movement cancellation");
        ThreadAttempt successfulAttempt = RequiredThreadAttempt(ThreadAttemptKind.Anchor);
        ThreadCastState[] sequence =
        {
            ThreadCastState.Preview,
            ThreadCastState.Windup,
            ThreadCastState.Travel,
            ThreadCastState.Latched,
            ThreadCastState.Apply,
            ThreadCastState.Recovery
        };
        ThreadEvidence previous = default;
        ulong castId = 0;
        foreach (ThreadCastState state in sequence)
        {
            if (!_threadStateEvidence.TryGetValue(state, out ThreadEvidence evidence))
                throw new InvalidOperationException($"missing mechanical-thread presentation stage {state}");
            if (evidence.AttemptId != successfulAttempt.Id ||
                evidence.AttemptKind != ThreadAttemptKind.Anchor)
                throw new InvalidOperationException(
                    $"mechanical-thread presentation attempt splice at {state}: " +
                    $"evidence={evidence.AttemptKind}/{evidence.AttemptId}, " +
                    $"expected=Anchor/{successfulAttempt.Id}");
            if (previous.Frame != 0 && evidence.Frame <= previous.Frame)
                throw new InvalidOperationException(
                    $"mechanical-thread stage order drift: {state}={evidence.Frame}, previous={previous.Frame}");
            if (state == ThreadCastState.Preview)
            {
                if (evidence.CastId != 0)
                    throw new InvalidOperationException("mechanical-thread Preview must precede cast identity allocation");
            }
            else
            {
                if (castId == 0) castId = evidence.CastId;
                if (evidence.CastId == 0 || evidence.CastId != castId)
                    throw new InvalidOperationException(
                        $"mechanical-thread presentation identity drift at {state}: {evidence.CastId} vs {castId}");
            }
            previous = evidence;
        }
        foreach ((ThreadFailureReason failure, ThreadAttemptKind kind) in new[]
                 {
                     (ThreadFailureReason.NoTarget, ThreadAttemptKind.NoTarget),
                     (ThreadFailureReason.Cooldown, ThreadAttemptKind.Cooldown),
                     (ThreadFailureReason.Obstructed, ThreadAttemptKind.Obstructed)
                 })
        {
            if (!_threadFailureEvidence.TryGetValue(failure, out ThreadFailureEvidence evidence))
                throw new InvalidOperationException($"missing mechanical-thread failure presentation {failure}");
            ThreadAttempt expected = RequiredThreadAttempt(kind);
            if (evidence.AttemptId != expected.Id || evidence.AttemptKind != kind)
                throw new InvalidOperationException(
                    $"mechanical-thread failure attempt drift at {failure}: " +
                    $"evidence={evidence.AttemptKind}/{evidence.AttemptId}, expected={kind}/{expected.Id}");
        }
        ThreadAttempt noTargetAttempt = RequiredThreadAttempt(ThreadAttemptKind.NoTarget);
        ThreadAttempt cooldownAttempt = RequiredThreadAttempt(ThreadAttemptKind.Cooldown);
        ThreadAttempt obstructedAttempt = RequiredThreadAttempt(ThreadAttemptKind.Obstructed);
        if (successfulAttempt.Id == noTargetAttempt.Id || successfulAttempt.Id == obstructedAttempt.Id ||
            successfulAttempt.Id == cooldownAttempt.Id || noTargetAttempt.Id == cooldownAttempt.Id ||
            cooldownAttempt.Id == obstructedAttempt.Id || noTargetAttempt.Id == obstructedAttempt.Id)
            throw new InvalidOperationException("mechanical-thread presentation attempts must be distinct");
        GD.Print("PROLOGUE_THREAD_PRESENTATION_SEQUENCE_OK");
    }

    private ThreadAttempt RequiredThreadAttempt(ThreadAttemptKind kind)
    {
        if (_threadAttempts.TryGetValue(kind, out ThreadAttempt attempt) && attempt.Id != 0)
            return attempt;
        throw new InvalidOperationException($"missing mechanical-thread presentation attempt {kind}");
    }

    private void ValidateAttackSequence(string actor, EnemyAttackKind kind)
    {
        AttackEvidence gold = Evidence(actor, kind, "gold");
        AttackEvidence danger = Evidence(actor, kind, "danger");
        AttackEvidence impact = Evidence(actor, kind, "impact");
        AttackEvidence recovery = Evidence(actor, kind, "recovery");
        if (!(gold.Frame < danger.Frame && danger.Frame < impact.Frame && impact.Frame < recovery.Frame))
            throw new InvalidOperationException($"invalid attack proof order for {actor}/{kind}: " +
                $"gold={gold.Frame}, danger={danger.Frame}, impact={impact.Frame}, recovery={recovery.Frame}");
        if (gold.AttackId == 0 || danger.AttackId != gold.AttackId || impact.AttackId != gold.AttackId ||
            recovery.AttackId != gold.AttackId)
            throw new InvalidOperationException($"attack proof identity drift for {actor}/{kind}: " +
                $"gold={gold.AttackId}, danger={danger.AttackId}, impact={impact.AttackId}, " +
                $"recovery={recovery.AttackId}");
        GD.Print($"PROLOGUE_ATTACK_SEQUENCE_OK {actor}/{kind}");
    }

    private AttackEvidence Evidence(string actor, EnemyAttackKind kind, string stage)
    {
        string key = EvidenceKey(actor, kind, stage);
        if (_attackEvidence.TryGetValue(key, out AttackEvidence evidence)) return evidence;
        throw new InvalidOperationException($"missing attack proof stage {key}");
    }

    private static void AttackPulse(bool pressed)
    {
        if (pressed)
        {
            Press(InputActions.MoveRight);
            Press(InputActions.Attack);
        }
        else
        {
            Release(InputActions.Attack);
            Release(InputActions.MoveRight);
        }
    }

    private static void ThreadPulse(bool right, bool up)
    {
        if (right) Press(InputActions.MoveRight); else Release(InputActions.MoveRight);
        if (up) Press(InputActions.MoveUp); else Release(InputActions.MoveUp);
        if (right || up) Press(InputActions.Thread); else Release(InputActions.Thread);
    }

    private static void ReleaseAim()
    {
        Release(InputActions.MoveLeft);
        Release(InputActions.MoveRight);
        Release(InputActions.MoveUp);
        Release(InputActions.MoveDown);
    }

    private static void Press(StringName action) => Input.ActionPress(action);
    private static void Release(StringName action) => Input.ActionRelease(action);

    private static void ReleaseAll()
    {
        Release(InputActions.MoveLeft);
        Release(InputActions.MoveRight);
        Release(InputActions.MoveUp);
        Release(InputActions.MoveDown);
        Release(InputActions.Jump);
        Release(InputActions.Dash);
        Release(InputActions.Attack);
        Release(InputActions.Thread);
    }
}
