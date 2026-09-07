using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Prologue;
using Knight.Ui;
using Knight.Vfx;

namespace Knight.Presentation;

public readonly record struct PlayerPresentationInput(
    Vector2 Velocity,
    bool Grounded,
    bool Dashing,
    bool Dead,
    bool CheckpointRecovering,
    int Facing,
    ulong AttackId = 0,
    bool IsAttacking = false,
    AttackDirection AttackDirection = AttackDirection.Horizontal,
    bool AttackConnected = false,
    bool HurtTriggered = false,
    Vector2 HurtImpulse = default,
    bool MovementCancelRequested = false,
    ThreadCastFrame Thread = default,
    MechanicalClawFrame Claw = default);

/// <summary>
/// Presentation-only timeline. It observes gameplay state and centralized tuning and never
/// changes input, physics, hitboxes, damage, cooldowns, or cast state.
/// </summary>
public sealed class PlayerPresentationTimeline
{
    private const float StartRunSeconds = .10f;
    private const float BrakeSeconds = .12f;
    private const float JumpStartSeconds = .08f;
    private const float SoftLandSeconds = .10f;
    private const float HardLandSeconds = .16f;
    private const float HurtContactSeconds = .055f;
    private const float HurtRecoverySeconds = .125f;
    private const float HitPauseSeconds = .025f;
    private const float ApexVelocity = 80f;
    private const float RunThreshold = 8f;
    private const float HardLandVelocity = 430f;

    private readonly PlayerTuning _tuning;
    private readonly SecondaryMotionModel _secondary = new();
    private PlayerLocomotionState _locomotionState = PlayerLocomotionState.Idle;
    private float _locomotionElapsed;
    private bool _hasSample;
    private bool _wasGrounded;
    private Vector2 _previousVelocity;
    private int _previousFacing = 1;
    private float _previousFallVelocity;
    private ulong _attackId;
    private ulong _lastObservedAttackId;
    private PlayerActionState _attackState;
    private Vector2 _attackAim;
    private float _attackElapsed;
    private bool _attackCancelled;
    private float _hitPauseRemaining;
    private float _contactImpulse;
    private bool _hurtActive;
    private float _hurtElapsed;

    public PlayerPresentationTimeline(PlayerTuning tuning = null)
    {
        _tuning = tuning ?? new PlayerTuning();
        System.Collections.Generic.IReadOnlyList<string> errors = _tuning.Validate();
        if (errors.Count > 0)
            throw new ArgumentException("Invalid PlayerTuning: " + string.Join("; ", errors), nameof(tuning));
    }

    public PlayerPresentationFrame Step(PlayerPresentationInput input, float deltaSeconds)
    {
        float delta = float.IsFinite(deltaSeconds) && deltaSeconds > 0f ? deltaSeconds : 0f;
        int facing = input.Facing < 0 ? -1 : 1;
        Vector2 velocity = Finite(input.Velocity) ? input.Velocity : Vector2.Zero;
        bool landed = _hasSample && !_wasGrounded && input.Grounded;
        float landingSpeed = landed ? Mathf.Max(0f, _previousFallVelocity) : 0f;

        PlayerLocomotionState nextLocomotion = ResolveLocomotion(input with { Velocity = velocity }, landingSpeed);
        if (nextLocomotion != _locomotionState)
        {
            _locomotionState = nextLocomotion;
            _locomotionElapsed = 0f;
        }
        else
        {
            _locomotionElapsed += delta;
        }
        PlayerLocomotionFrame locomotion = new(_locomotionState,
            NormalizedLocomotionTime(_locomotionState, _locomotionElapsed));

        if (input.AttackId != 0 && input.AttackId != _lastObservedAttackId)
        {
            _lastObservedAttackId = input.AttackId;
            _attackId = input.AttackId;
            _attackState = input.AttackDirection == AttackDirection.Down
                ? PlayerActionState.DownAttack
                : input.Grounded ? PlayerActionState.GroundAttack : PlayerActionState.AirAttack;
            _attackAim = AttackAim(input.AttackDirection, facing);
            _attackElapsed = 0f;
            _attackCancelled = false;
            _hitPauseRemaining = 0f;
        }
        if (input.AttackConnected)
        {
            _hitPauseRemaining = HitPauseSeconds;
            _contactImpulse = 1f;
        }
        if (input.HurtTriggered)
        {
            _hurtActive = true;
            _hurtElapsed = 0f;
            _contactImpulse = 1f;
        }

        PlayerActionFrame action = ResolveAction(input, delta);
        float accelerationScale = delta > 0f && _hasSample ? 1f / delta : 0f;
        Vector2 acceleration = (velocity - _previousVelocity) * accelerationScale;
        if (input.HurtTriggered && delta > 0f && Finite(input.HurtImpulse))
            acceleration += input.HurtImpulse / delta;
        float turn = _hasSample && facing != _previousFacing ? facing : 0f;
        float facingSign = facing < 0 ? -1f : 1f;
        Vector2 facingLocalVelocity = new(velocity.X * facingSign, velocity.Y);
        Vector2 facingLocalAcceleration = new(acceleration.X * facingSign, acceleration.Y);
        float facingLocalTurn = turn * facingSign;
        float landingImpulse = landingSpeed <= 0f ? 0f : Mathf.Clamp(landingSpeed / 650f, 0f, 1f);
        bool clawActive = input.Claw.State != MechanicalClawState.Idle;
        float threadEnergy = clawActive
            ? input.Claw.State == MechanicalClawState.Failed ? .35f : 1f
            : input.Thread.State == ThreadCastState.Idle ? 0f :
                input.Thread.State == ThreadCastState.Failed ? .35f : 1f;
        PlayerActionPhase secondaryPhase = clawActive
            ? MechanicalClawSecondaryPhase(input.Claw.State)
            : ThreadSecondaryPhase(input.Thread.State);
        SecondaryMotionFrame secondary = _secondary.Step(new SecondaryMotionInput(
            facingLocalVelocity,
            facingLocalAcceleration,
            facingLocalTurn,
            landingImpulse,
            _contactImpulse,
            secondaryPhase,
            threadEnergy), delta);

        _contactImpulse *= Mathf.Exp(-12f * delta);
        if (!input.Grounded) _previousFallVelocity = velocity.Y;
        else if (!landed) _previousFallVelocity = 0f;
        _previousVelocity = velocity;
        _previousFacing = facing;
        _wasGrounded = input.Grounded;
        _hasSample = true;

        EntityPresentationFrame baseFrame = new(CoarseState(locomotion.State), facing, input.Grounded);
        return new PlayerPresentationFrame(baseFrame, locomotion, action, secondary);
    }

    internal void ClearAttackState()
    {
        _attackId = 0;
        _lastObservedAttackId = 0;
        _attackState = PlayerActionState.None;
        _attackAim = Vector2.Zero;
        _attackElapsed = 0f;
        _attackCancelled = false;
        _hitPauseRemaining = 0f;
        _contactImpulse = 0f;
    }

    private PlayerLocomotionState ResolveLocomotion(PlayerPresentationInput input, float landingSpeed)
    {
        if (input.CheckpointRecovering) return PlayerLocomotionState.Recovery;
        if (input.Dead) return PlayerLocomotionState.Dead;
        if (input.Dashing) return PlayerLocomotionState.Dash;

        if (!input.Grounded)
        {
            if (_hasSample && _wasGrounded) return PlayerLocomotionState.JumpStart;
            if (_locomotionState == PlayerLocomotionState.JumpStart &&
                _locomotionElapsed < JumpStartSeconds) return PlayerLocomotionState.JumpStart;
            if (input.Velocity.Y < -ApexVelocity) return PlayerLocomotionState.Rise;
            if (input.Velocity.Y <= ApexVelocity) return PlayerLocomotionState.Apex;
            return PlayerLocomotionState.Fall;
        }

        if (_hasSample && !_wasGrounded)
            return landingSpeed >= HardLandVelocity
                ? PlayerLocomotionState.HardLand
                : PlayerLocomotionState.SoftLand;
        if (_locomotionState == PlayerLocomotionState.HardLand &&
            _locomotionElapsed < HardLandSeconds) return PlayerLocomotionState.HardLand;
        if (_locomotionState == PlayerLocomotionState.SoftLand &&
            _locomotionElapsed < SoftLandSeconds) return PlayerLocomotionState.SoftLand;

        bool moving = Mathf.Abs(input.Velocity.X) > RunThreshold;
        if (moving)
        {
            if (_locomotionState == PlayerLocomotionState.StartRun &&
                _locomotionElapsed < StartRunSeconds) return PlayerLocomotionState.StartRun;
            if (_locomotionState is PlayerLocomotionState.Idle or PlayerLocomotionState.Brake or
                PlayerLocomotionState.SoftLand or PlayerLocomotionState.HardLand)
                return PlayerLocomotionState.StartRun;
            return PlayerLocomotionState.Run;
        }

        if (_locomotionState == PlayerLocomotionState.Brake &&
            _locomotionElapsed < BrakeSeconds) return PlayerLocomotionState.Brake;
        if (_locomotionState is PlayerLocomotionState.Run or PlayerLocomotionState.StartRun)
            return PlayerLocomotionState.Brake;
        return PlayerLocomotionState.Idle;
    }

    private PlayerActionFrame ResolveAction(PlayerPresentationInput input, float delta)
    {
        if (input.Dead)
        {
            _hurtActive = false;
            return PlayerActionFrame.None;
        }

        PlayerActionFrame claw = MechanicalClawAction(input.Claw);
        if (claw.State != PlayerActionState.None) return claw;

        PlayerActionFrame thread = ThreadAction(input.Thread);
        if (thread.State != PlayerActionState.None) return thread;

        if (_hurtActive)
        {
            _hurtElapsed += delta;
            float total = HurtContactSeconds + HurtRecoverySeconds;
            if (_hurtElapsed >= total)
            {
                _hurtActive = false;
            }
            else if (_hurtElapsed < HurtContactSeconds)
            {
                return new PlayerActionFrame(PlayerActionState.Hurt, PlayerActionPhase.Contact,
                    Ratio(_hurtElapsed, HurtContactSeconds), false, input.HurtImpulse);
            }
            else
            {
                return new PlayerActionFrame(PlayerActionState.Hurt, PlayerActionPhase.Recovery,
                    Ratio(_hurtElapsed - HurtContactSeconds, HurtRecoverySeconds), true,
                    input.HurtImpulse);
            }
        }

        if (_attackId == 0 || _attackCancelled) return PlayerActionFrame.None;
        float totalAttack = _tuning.AttackStartupSeconds + _tuning.AttackActiveSeconds +
            _tuning.AttackRecoverySeconds;
        if (!input.IsAttacking && _attackElapsed >= totalAttack)
        {
            _attackId = 0;
            return PlayerActionFrame.None;
        }

        if (_hitPauseRemaining > 0f)
            _hitPauseRemaining = Mathf.Max(0f, _hitPauseRemaining - delta);
        else
            _attackElapsed += delta;

        float burstSeconds = _tuning.AttackActiveSeconds * .55f;
        float contactSeconds = _tuning.AttackActiveSeconds - burstSeconds;
        PlayerActionPhase phase;
        float progress;
        bool cancellable = false;
        if (_attackElapsed < _tuning.AttackStartupSeconds)
        {
            phase = PlayerActionPhase.Anticipation;
            progress = Ratio(_attackElapsed, _tuning.AttackStartupSeconds);
        }
        else if (_attackElapsed < _tuning.AttackStartupSeconds + burstSeconds)
        {
            phase = PlayerActionPhase.Burst;
            progress = Ratio(_attackElapsed - _tuning.AttackStartupSeconds, burstSeconds);
        }
        else if (_attackElapsed < _tuning.AttackStartupSeconds + _tuning.AttackActiveSeconds)
        {
            phase = PlayerActionPhase.Contact;
            progress = Ratio(_attackElapsed - _tuning.AttackStartupSeconds - burstSeconds, contactSeconds);
        }
        else
        {
            phase = PlayerActionPhase.Recovery;
            progress = Ratio(_attackElapsed - _tuning.AttackStartupSeconds -
                _tuning.AttackActiveSeconds, _tuning.AttackRecoverySeconds);
            cancellable = true;
            if (input.MovementCancelRequested)
            {
                _attackCancelled = true;
                return PlayerActionFrame.None;
            }
        }

        return new PlayerActionFrame(_attackState, phase, progress, cancellable, _attackAim);
    }

    private static PlayerActionFrame ThreadAction(ThreadCastFrame frame)
    {
        float progress = PhaseProgress(frame);
        return frame.State switch
        {
            ThreadCastState.Preview => new(PlayerActionState.ThreadPreview,
                PlayerActionPhase.Anticipation, progress, false, frame.Aim),
            ThreadCastState.Windup => new(PlayerActionState.ThreadWindup,
                PlayerActionPhase.Anticipation, progress, false, frame.Aim),
            ThreadCastState.Travel => new(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, progress, false, frame.Aim),
            ThreadCastState.Latched => new(PlayerActionState.ThreadLatch,
                PlayerActionPhase.Contact, progress, false, frame.Aim),
            ThreadCastState.Apply => new(PlayerActionState.ThreadApply,
                PlayerActionPhase.Contact, progress, false, frame.Aim),
            ThreadCastState.Recovery => new(PlayerActionState.ThreadApply,
                PlayerActionPhase.Recovery, progress, true, frame.Aim),
            ThreadCastState.Failed => new(PlayerActionState.ThreadFailed,
                PlayerActionPhase.Recovery, progress, false, frame.Aim),
            _ => PlayerActionFrame.None
        };
    }

    private static PlayerActionFrame MechanicalClawAction(MechanicalClawFrame frame)
    {
        Vector2 aim = frame.CasterWorldPosition.DirectionTo(frame.AnchorPoint);
        float progress = frame.State switch
        {
            MechanicalClawState.Travel => frame.CasterWorldPosition.DistanceTo(frame.AnchorPoint) <= .001f
                ? 1f
                : Mathf.Clamp(frame.ShuttleDistance /
                    frame.CasterWorldPosition.DistanceTo(frame.AnchorPoint), 0f, 1f),
            MechanicalClawState.Aim => frame.EffectiveVisibility ? 1f : .35f,
            _ => frame.State == MechanicalClawState.Idle ? 0f : 1f
        };
        return frame.State switch
        {
            MechanicalClawState.Aim => new(PlayerActionState.ThreadPreview,
                PlayerActionPhase.Anticipation, progress, false, aim),
            MechanicalClawState.Windup => new(PlayerActionState.ThreadWindup,
                PlayerActionPhase.Anticipation, progress, false, aim),
            MechanicalClawState.Travel => new(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, progress, false, aim),
            MechanicalClawState.Latched => new(PlayerActionState.ThreadLatch,
                PlayerActionPhase.Contact, progress, false, aim),
            MechanicalClawState.Reel or MechanicalClawState.Swing => new(PlayerActionState.ThreadApply,
                PlayerActionPhase.Contact, progress, false, aim),
            MechanicalClawState.Released or MechanicalClawState.Retract => new(PlayerActionState.ThreadApply,
                PlayerActionPhase.Recovery, progress, true, aim),
            MechanicalClawState.Failed => new(PlayerActionState.ThreadFailed,
                PlayerActionPhase.Recovery, progress, false, aim),
            _ => PlayerActionFrame.None
        };
    }

    private static PlayerActionPhase ThreadSecondaryPhase(ThreadCastState state) => state switch
    {
        ThreadCastState.Preview or ThreadCastState.Windup => PlayerActionPhase.Anticipation,
        ThreadCastState.Travel => PlayerActionPhase.Burst,
        ThreadCastState.Latched or ThreadCastState.Apply => PlayerActionPhase.Contact,
        ThreadCastState.Recovery or ThreadCastState.Failed => PlayerActionPhase.Recovery,
        _ => PlayerActionPhase.None
    };

    private static PlayerActionPhase MechanicalClawSecondaryPhase(MechanicalClawState state) => state switch
    {
        MechanicalClawState.Aim or MechanicalClawState.Windup => PlayerActionPhase.Anticipation,
        MechanicalClawState.Travel => PlayerActionPhase.Burst,
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing =>
            PlayerActionPhase.Contact,
        MechanicalClawState.Released or MechanicalClawState.Retract or MechanicalClawState.Failed =>
            PlayerActionPhase.Recovery,
        _ => PlayerActionPhase.None
    };

    private static float PhaseProgress(ThreadCastFrame frame) =>
        frame.StateDurationSeconds <= .000001f
            ? (frame.State == ThreadCastState.Idle ? 0f : 1f)
            : Mathf.Clamp(1f - frame.StateRemainingSeconds / frame.StateDurationSeconds, 0f, 1f);

    private static Vector2 AttackAim(AttackDirection direction, int facing) => direction switch
    {
        AttackDirection.Up => Vector2.Up,
        AttackDirection.Down => Vector2.Down,
        _ => new Vector2(facing < 0 ? -1f : 1f, 0f)
    };

    private static float NormalizedLocomotionTime(PlayerLocomotionState state, float elapsed)
    {
        float duration = state switch
        {
            PlayerLocomotionState.Idle => 1.2f,
            PlayerLocomotionState.StartRun => StartRunSeconds,
            PlayerLocomotionState.Run => .40f,
            PlayerLocomotionState.Brake => BrakeSeconds,
            PlayerLocomotionState.JumpStart => JumpStartSeconds,
            PlayerLocomotionState.Rise => .24f,
            PlayerLocomotionState.Apex => .14f,
            PlayerLocomotionState.Fall => .28f,
            PlayerLocomotionState.SoftLand => SoftLandSeconds,
            PlayerLocomotionState.HardLand => HardLandSeconds,
            PlayerLocomotionState.Dash => .14f,
            PlayerLocomotionState.Recovery => .35f,
            PlayerLocomotionState.Dead => .55f,
            _ => 1f
        };
        if (state is PlayerLocomotionState.Idle or PlayerLocomotionState.Run)
            return duration <= 0f ? 0f : (elapsed % duration) / duration;
        return Ratio(elapsed, duration);
    }

    private static EntityVisualState CoarseState(PlayerLocomotionState state) => state switch
    {
        PlayerLocomotionState.StartRun or PlayerLocomotionState.Run or PlayerLocomotionState.Brake =>
            EntityVisualState.Move,
        PlayerLocomotionState.JumpStart or PlayerLocomotionState.Rise => EntityVisualState.Rise,
        PlayerLocomotionState.Apex or PlayerLocomotionState.Fall => EntityVisualState.Fall,
        PlayerLocomotionState.Dash => EntityVisualState.Dash,
        PlayerLocomotionState.Dead => EntityVisualState.Dead,
        PlayerLocomotionState.Recovery => EntityVisualState.Hurt,
        _ => EntityVisualState.Idle
    };

    private static float Ratio(float value, float duration) => duration <= .000001f
        ? 1f
        : Mathf.Clamp(value / duration, 0f, 1f);
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}

public partial class PlayerPresentationCoordinator : Node
{
    private const int MinimumRecoveryStageHoldFrames = 1;
    private enum RecoveryPresentationStage { None, Dead, Recovery }

    [Export] public NodePath BodyPath { get; set; } = new("..");
    [Export] public NodePath InputPath { get; set; } = new("../Input");
    [Export] public NodePath MotorPath { get; set; } = new("../Motor");
    [Export] public NodePath CombatPath { get; set; } = new("../Combat");
    [Export] public NodePath ActionModesPath { get; set; } = new("../ActionModes");
    [Export] public NodePath EnergyPath { get; set; } = new("../Energy");
    [Export] public NodePath HealthPath { get; set; } = new("../Health");
    [Export] public NodePath MechanicalClawPath { get; set; } = new("../MechanicalClaw");
    [Export] public NodePath VisualRootPath { get; set; } = new("../VisualRoot");
    [Export] public NodePath EffectsPath { get; set; } = new("../VisualRoot/Effects");
    [Export] public PresentationProfile Profile { get; set; } = new();

    private CharacterBody2D _body;
    private PlayerInputReader _input;
    private PlayerMotor _motor;
    private PlayerCombat _combat;
    private PlayerActionModeController _actionModes;
    private PlayerEnergyController _energy;
    private HealthComponent _health;
    private MechanicalClawController _mechanicalClaw;
    private PrologueLevelController _recoverySource;
    private Node _effects;
    private IEntityVisual2D _visual;
    private PlayerPresentationTimeline _timeline;
    private int _facing = 1;
    private int _attackFacing = 1;
    private bool _wasDashing;
    private bool _checkpointRecovering;
    private bool _recoveryPresentationRequested;
    private bool _recoveryPresentationCompleted;
    private RecoveryPresentationStage _recoveryPresentationStage;
    private int _recoveryPresentationHoldFrames;
    private bool _attackConnectedPending;
    private bool _hurtPending;
    private Vector2 _hurtImpulse;
    private ulong _attackId;
    private AttackDirection _attackDirection;
    private MechanicalClawState _lastClawState = MechanicalClawState.Idle;

    public PlayerPresentationFrame LastPlayerFrame { get; private set; }
    public PlayerHudActionState ActionHudState { get; private set; } = PlayerHudActionState.Default;
    public int FrameObserverFailureCount { get; private set; }
    public event Action<PlayerPresentationFrame> FramePublished;

    public override void _Ready()
    {
        _body = GetNodeOrNull<CharacterBody2D>(BodyPath);
        _input = GetNodeOrNull<PlayerInputReader>(InputPath);
        _motor = GetNodeOrNull<PlayerMotor>(MotorPath);
        _combat = GetNodeOrNull<PlayerCombat>(CombatPath);
        _actionModes = GetNodeOrNull<PlayerActionModeController>(ActionModesPath);
        _energy = GetNodeOrNull<PlayerEnergyController>(EnergyPath);
        _health = GetNodeOrNull<HealthComponent>(HealthPath);
        _mechanicalClaw = GetNodeOrNull<MechanicalClawController>(MechanicalClawPath);
        _recoverySource = FindAncestor<PrologueLevelController>(this);
        _effects = GetNodeOrNull(EffectsPath);
        _timeline = new PlayerPresentationTimeline(_motor?.Tuning ?? _combat?.Tuning ?? new PlayerTuning());
        BindVisualRoot(GetNode(VisualRootPath));

        if (_combat != null)
        {
            _combat.AttackStarted += OnAttackStarted;
            _combat.AttackConnected += OnAttackConnected;
        }
        if (_health != null)
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }
        if (_mechanicalClaw != null)
            _mechanicalClaw.ClawFrameChanged += OnClawFrameChanged;
        if (_recoverySource != null)
        {
            _recoverySource.RecoveryRequested += OnRecoveryRequested;
            _recoverySource.RecoveryCompleted += OnRecoveryCompleted;
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_combat))
        {
            _combat.AttackStarted -= OnAttackStarted;
            _combat.AttackConnected -= OnAttackConnected;
        }
        if (IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
        if (IsInstanceValid(_mechanicalClaw))
            _mechanicalClaw.ClawFrameChanged -= OnClawFrameChanged;
        if (IsInstanceValid(_recoverySource))
        {
            _recoverySource.RecoveryRequested -= OnRecoveryRequested;
            _recoverySource.RecoveryCompleted -= OnRecoveryCompleted;
        }
    }

    public override void _Process(double delta)
    {
        RefreshActionHudState();
        if (_body == null || _visual == null || _timeline == null) return;
        PresentCurrent((float)delta);
        AdvanceRecoveryPresentation();
    }

    public void BindVisualRoot(Node visualRoot)
    {
        Profile ??= new PresentationProfile();
        _visual = Profile.Resolve(visualRoot);
    }

    public void Present(EntityPresentationFrame frame) => _visual?.ApplyFrame(frame);

    public void Present(PlayerPresentationFrame frame)
    {
        LastPlayerFrame = frame;
        if (_visual is PlayerFormalVisual formal) formal.ApplyPlayerFrame(frame);
        else
        {
            _visual?.ApplyFrame(frame.Base);
            if (TryResolveThreadActionCue(frame.Action.State, out PresentationCue cue))
                _visual?.PlayCue(cue);
        }
    }

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.RecoveryStarted) _checkpointRecovering = true;
        else if (cue == PresentationCue.RecoveryCompleted) _checkpointRecovering = false;
        PlayLocalCue(cue);
        Vector2 position = _body?.GlobalPosition ?? Vector2.Zero;
        PrologueVfxDirector.PlayFromTree(this, cue, position, new Vector2(_facing, 0));
    }

    private void PresentCurrent(float delta)
    {
        bool dashing = _motor?.IsDashing ?? false;
        if (dashing && !_wasDashing) PlayCue(PresentationCue.Dash);
        _wasDashing = dashing;
        PlayerInputFrame sampledInput = _input?.CurrentFrame ?? default;
        MechanicalClawFrame claw = _mechanicalClaw?.CurrentFrame ?? default;
        Vector2 clawAim = claw.CasterWorldPosition.DirectionTo(claw.AnchorPoint);
        if (IsClawPresentationState(claw.State) && !Mathf.IsZeroApprox(clawAim.X))
            _facing = clawAim.X < 0f ? -1 : 1;
        else if (_combat?.IsAttacking ?? false)
            _facing = _attackFacing;
        else if (!Mathf.IsZeroApprox(_body.Velocity.X))
            _facing = _body.Velocity.X < 0f ? -1 : 1;
        bool presentationDead = _recoveryPresentationStage == RecoveryPresentationStage.Dead ||
            (_health != null && _health.CurrentHealth <= 0 && !_recoveryPresentationRequested);
        bool presentationRecovering = _recoveryPresentationStage == RecoveryPresentationStage.Recovery ||
            (_recoveryPresentationStage == RecoveryPresentationStage.None && _checkpointRecovering);
        LastPlayerFrame = _timeline.Step(new PlayerPresentationInput(
            _body.Velocity,
            _body.IsOnFloor(),
            dashing,
            presentationDead,
            presentationRecovering,
            _facing,
            _attackId,
            _combat?.IsAttacking ?? false,
            _attackDirection,
            _attackConnectedPending,
            _hurtPending,
            _hurtImpulse,
            !Mathf.IsZeroApprox(sampledInput.MoveAxis) || sampledInput.JumpPressed,
            default,
            claw), delta);
        _attackConnectedPending = false;
        _hurtPending = false;
        _hurtImpulse = Vector2.Zero;
        Present(LastPlayerFrame);
        PublishFrame(LastPlayerFrame);
    }

    private void RefreshActionHudState()
    {
        _actionModes ??= GetNodeOrNull<PlayerActionModeController>(ActionModesPath);
        _energy ??= GetNodeOrNull<PlayerEnergyController>(EnergyPath);
        if (_actionModes == null || _energy == null) return;
        ActionHudState = new PlayerHudActionState(
            _actionModes.CurrentMode,
            _actionModes.ActiveSlot,
            _energy.EnergyRatio,
            _energy.HeatRatio,
            _energy.IsOverheated,
            _actionModes.LastRejection);
    }

    private void PublishFrame(PlayerPresentationFrame frame)
    {
        if (FramePublished == null) return;
        foreach (Action<PlayerPresentationFrame> observer in FramePublished.GetInvocationList())
        {
            try
            {
                observer(frame);
            }
            catch (Exception)
            {
                // FramePublished is a diagnostic/downstream observation surface. A broken
                // observer must not interrupt recovery sequencing or starve later observers.
                FrameObserverFailureCount++;
            }
        }
    }

    private void PlayLocalCue(PresentationCue cue)
    {
        // The typed formal frame is the sole pose authority. Compatibility cues may
        // still drive legacy visuals, while VFX/audio remain available to every rig.
        if (_visual is not PlayerFormalVisual) _visual?.PlayCue(cue);
        if (_effects != null && Profile != null && Profile.TryInstantiateCue(cue, out Node2D effect))
        {
            _effects.AddChild(effect);
            if (effect is GpuParticles2D particles) particles.Finished += effect.QueueFree;
        }
    }

    public static EntityPresentationFrame ResolveFrame(
        Vector2 velocity,
        bool grounded,
        bool dashing,
        bool attacking,
        bool dead,
        int facing)
    {
        EntityVisualState state;
        if (dead) state = EntityVisualState.Dead;
        else if (attacking) state = EntityVisualState.Attack;
        else if (dashing) state = EntityVisualState.Dash;
        else if (!grounded) state = velocity.Y < 0f ? EntityVisualState.Rise : EntityVisualState.Fall;
        else state = Mathf.Abs(velocity.X) > 1f ? EntityVisualState.Move : EntityVisualState.Idle;
        return new EntityPresentationFrame(state, facing < 0 ? -1 : 1, grounded);
    }

    private void OnAttackStarted(ulong attackId, int direction)
    {
        _attackId = attackId;
        _attackDirection = (AttackDirection)direction;
        Vector2 inputAim = _input?.CurrentFrame.Aim ?? Vector2.Zero;
        float aimDeadZone = _combat?.Tuning?.ToCombatSettings().AimDeadZone ?? .5f;
        _attackFacing = PlayerCombatModel.ResolveHorizontalFacing(inputAim, _facing, aimDeadZone);
        _facing = _attackFacing;
        PlayCue(_attackDirection == AttackDirection.Down
            ? PresentationCue.Downstrike
            : PresentationCue.AttackStarted);
    }

    private void OnAttackConnected(Hurtbox hurtbox)
    {
        _attackConnectedPending = true;
        PlayCue(PresentationCue.AttackHit);
    }

    private void OnDamaged(int remaining, Vector2 impulse)
    {
        _hurtPending = true;
        _hurtImpulse = impulse;
        PlayCue(PresentationCue.Hurt);
    }

    private void OnDied()
    {
        ClearPresentationAttack();
        _recoveryPresentationStage = RecoveryPresentationStage.Dead;
        _recoveryPresentationHoldFrames = MinimumRecoveryStageHoldFrames;
        _recoveryPresentationRequested = false;
        _recoveryPresentationCompleted = false;
        PlayCue(PresentationCue.Died);
        if (_body != null && _visual != null && _timeline != null) PresentCurrent(0f);
    }

    private void ClearPresentationAttack()
    {
        _attackId = 0;
        _attackDirection = AttackDirection.Horizontal;
        _attackFacing = _facing;
        _attackConnectedPending = false;
        _timeline?.ClearAttackState();
    }

    private void OnRecoveryRequested(string reason)
    {
        ClearPresentationAttack();
        _checkpointRecovering = true;
        _recoveryPresentationRequested = true;
        if (_recoveryPresentationStage == RecoveryPresentationStage.None)
        {
            _recoveryPresentationStage = RecoveryPresentationStage.Recovery;
            _recoveryPresentationHoldFrames = MinimumRecoveryStageHoldFrames;
        }
    }

    private void OnRecoveryCompleted(string reason)
    {
        _checkpointRecovering = false;
        _recoveryPresentationCompleted = true;
        if (_recoveryPresentationStage == RecoveryPresentationStage.None)
        {
            _recoveryPresentationRequested = false;
            _recoveryPresentationCompleted = false;
            if (_body != null && _visual != null && _timeline != null) PresentCurrent(0f);
        }
    }

    private void AdvanceRecoveryPresentation()
    {
        if (_recoveryPresentationHoldFrames > 0)
        {
            _recoveryPresentationHoldFrames--;
            return;
        }
        if (_recoveryPresentationStage == RecoveryPresentationStage.Dead &&
            _recoveryPresentationRequested)
        {
            _recoveryPresentationStage = RecoveryPresentationStage.Recovery;
            _recoveryPresentationHoldFrames = MinimumRecoveryStageHoldFrames;
            return;
        }
        if (_recoveryPresentationStage != RecoveryPresentationStage.Recovery ||
            !_recoveryPresentationCompleted) return;
        _recoveryPresentationStage = RecoveryPresentationStage.None;
        _recoveryPresentationHoldFrames = 0;
        _recoveryPresentationRequested = false;
        _recoveryPresentationCompleted = false;
    }

    private void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (frame.State == _lastClawState) return;
        MechanicalClawState previousState = _lastClawState;
        _lastClawState = frame.State;
        if (frame.State == MechanicalClawState.Idle && IsClawPresentationState(previousState))
        {
            _visual?.ClearCueOverride();
            if (_body != null && _timeline != null) PresentCurrent(0f);
        }
        if (!TryResolveClawPhaseCue(frame.State, out PresentationCue cue)) return;
        if (frame.State == MechanicalClawState.Travel)
            PlayCue(PresentationCue.ThreadFired);
        else if (frame.State == MechanicalClawState.Latched)
            PlayCue(PresentationCue.ThreadHit);
        else if (frame.State == MechanicalClawState.Failed)
            PlayCue(PresentationCue.ThreadFailed);
        else
            PlayLocalCue(cue);
    }

    private static bool IsClawPresentationState(MechanicalClawState state) =>
        state is not MechanicalClawState.Idle;

    private static bool TryResolveClawPhaseCue(MechanicalClawState state, out PresentationCue cue)
    {
        cue = state switch
        {
            MechanicalClawState.Aim => PresentationCue.ThreadPreview,
            MechanicalClawState.Windup => PresentationCue.ThreadWindup,
            MechanicalClawState.Travel => PresentationCue.ThreadTravel,
            MechanicalClawState.Latched => PresentationCue.ThreadLatched,
            MechanicalClawState.Reel or MechanicalClawState.Swing => PresentationCue.ThreadApplied,
            MechanicalClawState.Released or MechanicalClawState.Retract => PresentationCue.ThreadRecovery,
            MechanicalClawState.Failed => PresentationCue.ThreadFailed,
            _ => default
        };
        return IsClawPresentationState(state);
    }

    private static bool IsThreadPresentationState(ThreadCastState state) =>
        state is ThreadCastState.Preview or ThreadCastState.Windup or ThreadCastState.Travel or
            ThreadCastState.Latched or ThreadCastState.Apply or ThreadCastState.Recovery or
            ThreadCastState.Failed;

    private static bool TryResolveThreadPhaseCue(ThreadCastState state, out PresentationCue cue)
    {
        cue = state switch
        {
            ThreadCastState.Preview => PresentationCue.ThreadPreview,
            ThreadCastState.Windup => PresentationCue.ThreadWindup,
            ThreadCastState.Travel => PresentationCue.ThreadTravel,
            ThreadCastState.Latched => PresentationCue.ThreadLatched,
            ThreadCastState.Apply => PresentationCue.ThreadApplied,
            ThreadCastState.Recovery => PresentationCue.ThreadRecovery,
            ThreadCastState.Failed => PresentationCue.ThreadFailed,
            _ => default
        };
        return IsThreadPresentationState(state);
    }

    private static bool TryResolveThreadActionCue(PlayerActionState state, out PresentationCue cue)
    {
        cue = state switch
        {
            PlayerActionState.ThreadPreview => PresentationCue.ThreadPreview,
            PlayerActionState.ThreadWindup => PresentationCue.ThreadWindup,
            PlayerActionState.ThreadThrow => PresentationCue.ThreadTravel,
            PlayerActionState.ThreadLatch => PresentationCue.ThreadLatched,
            PlayerActionState.ThreadApply => PresentationCue.ThreadApplied,
            PlayerActionState.ThreadFailed => PresentationCue.ThreadFailed,
            _ => default
        };
        return state is PlayerActionState.ThreadPreview or PlayerActionState.ThreadWindup or
            PlayerActionState.ThreadThrow or PlayerActionState.ThreadLatch or
            PlayerActionState.ThreadApply or PlayerActionState.ThreadFailed;
    }

    private static T FindAncestor<T>(Node node) where T : Node
    {
        for (Node current = node?.GetParent(); current != null; current = current.GetParent())
            if (current is T match) return match;
        return null;
    }
}
