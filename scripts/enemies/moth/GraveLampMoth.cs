using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Enemies.Moth;

public partial class GraveLampMoth : CharacterBody2D, IThreadPullReceiver, IEncounterActor
{
    [Export] public NodePath HealthPath { get; set; } = new("Health");
    [Export] public NodePath AttackHitboxPath { get; set; } = new("AttackHitbox");
    [Export] public EnemyArchetypeTuning Tuning { get; set; } = new();

    public event Action<GraveLampMothFrame, int, bool> FrameChanged;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;
    public event Action Defeated;

    public GraveLampMothState CurrentState { get; private set; } = GraveLampMothState.Hover;
    public GraveLampMothFrame CurrentFrame { get; private set; } =
        new(GraveLampMothState.Hover, 0f, 0f, false, false);
    public int Facing { get; private set; } = 1;

    private GraveLampMothModel _model;
    private HealthComponent _health;
    private Hitbox _attackHitbox;
    private CollisionShape2D _attackCollider;
    private AttackTelegraphVisual _attackTelegraph;
    private Vector2 _spawnPosition;
    private Vector2 _threadVelocity;
    private Vector2 _damageImpulse;
    private bool _diveContactedWorld;
    private bool _threadRequested;
    private AttackTimelineFrame _lastAttackTimeline;
    private bool _attackTargetLocked;
    private Vector2 _lockedAttackTarget;
    private Transform2D _lockedAttackStartTransform;

    public override void _Ready()
    {
        Tuning ??= new EnemyArchetypeTuning();
        IReadOnlyList<string> errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid Grave-Lamp Moth tuning: " + string.Join("; ", errors));
        _model = CreateAttackModel();
        _health = GetNode<HealthComponent>(HealthPath);
        _attackHitbox = GetNode<Hitbox>(AttackHitboxPath);
        _attackCollider = _attackHitbox.GetNode<CollisionShape2D>("AttackCollider");
        _attackTelegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        _spawnPosition = GlobalPosition;
        InitializeAirResidence();
    }

    public override void _ExitTree()
    {
        ExitAirResidence();
        if (!IsInstanceValid(_health)) return;
        _health.Damaged -= OnDamaged;
        _health.Died -= OnDied;
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_airResidence!=null){StepAirResidence(delta);return;}
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        Vector2 targetWorldPosition = _attackTargetLocked
            ? _lockedAttackTarget
            : player?.GlobalPosition ?? Vector2.Zero;
        Vector2 playerOffset = player == null ? Vector2.Zero : targetWorldPosition - GlobalPosition;
        Vector2 attackPoint = player == null
            ? Vector2.Zero
            : player.GlobalPosition + Vector2.Up * Tuning.PreferredHoverHeight;
        Vector2 alignmentError = player == null ? Vector2.Zero : attackPoint - GlobalPosition;
        bool contactedWorld = _diveContactedWorld || IsOnFloor() || IsOnWall() || IsOnCeiling();
        _diveContactedWorld = false;
        CurrentFrame = _model.Step(new GraveLampMothInput(
            player != null && playerOffset.Length() <= Tuning.DetectionRange,
            playerOffset.X,
            playerOffset.Y,
            alignmentError.X,
            alignmentError.Y,
            contactedWorld,
            _threadRequested,
            _health.CurrentHealth <= 0), delta, targetWorldPosition);
        CurrentState = CurrentFrame.State;
        _threadRequested = false;

        AttackTimelineFrame timeline = EnrichTimeline(CurrentFrame.AttackTimeline, player, targetWorldPosition);

        if (Mathf.Abs(CurrentFrame.VelocityX) > 0.01f) Facing = CurrentFrame.VelocityX < 0f ? -1 : 1;
        if (timeline.Phase == AttackPhase.Active && _lastAttackTimeline.Phase != AttackPhase.Active)
        {
            _attackHitbox.Begin(timeline.AttackId);
        }
        else if (!CurrentFrame.AttackActive) _attackHitbox.End();
        Vector2 velocity = new(CurrentFrame.VelocityX, CurrentFrame.VelocityY);
        if (CurrentState == GraveLampMothState.ThreadStunned)
        {
            _threadVelocity.Y += Tuning.Gravity * (float)delta;
            velocity = _threadVelocity;
            _threadVelocity.X *= Mathf.Exp(-2f * (float)delta);
        }
        else _threadVelocity = Vector2.Zero;

        _damageImpulse = _damageImpulse.Lerp(Vector2.Zero, 1f - Mathf.Exp(-12f * (float)delta));
        if (CurrentState != GraveLampMothState.Dead) velocity += _damageImpulse;
        else velocity = Vector2.Zero;
        Velocity = velocity;
        if (CurrentState == GraveLampMothState.Dive)
        {
            Vector2 lockedDiveVelocity = new(CurrentFrame.VelocityX, CurrentFrame.VelocityY);
            Velocity = lockedDiveVelocity;
            using KinematicCollision2D collision = MoveAndCollide(
                lockedDiveVelocity * (float)delta, false, SafeMargin, false);
            if (collision != null)
            {
                _diveContactedWorld = true;
                Velocity = Vector2.Zero;
            }
        }
        else MoveAndSlide();
        PublishAttack(timeline);
        FrameChanged?.Invoke(CurrentFrame, Facing, IsOnFloor());
    }

    public void ApplyThreadPull(Vector2 velocity)
    {
        if(_airResidence!=null){PullAirResident(velocity);return;}
        _threadVelocity = velocity;
        Velocity = velocity;
        _threadRequested = true;
    }

    public void ResetForCheckpoint()
    {
        if(_airResidence!=null)_airLifeVersion++;
        GlobalPosition = _spawnPosition;
        Velocity = Vector2.Zero;
        _threadVelocity = Vector2.Zero;
        _damageImpulse = Vector2.Zero;
        _diveContactedWorld = false;
        _threadRequested = false;
        _model = CreateAttackModel();
        CurrentState = GraveLampMothState.Hover;
        CurrentFrame = new GraveLampMothFrame(GraveLampMothState.Hover, 0f, 0f, false, false);
        _attackHitbox?.End();
        _attackTargetLocked = false;
        _lockedAttackStartTransform = default;
        PublishAttack(default);
        _health?.ResetFull();
        ResetAirResidence();
    }

    private void OnDamaged(int remaining, Vector2 impulse)
    {
        _damageImpulse = impulse;
        if(_airResidence!=null)HurtAirResident(impulse);
        PresentationCueRequested?.Invoke(PresentationCue.Hurt);
    }

    private void OnDied()
    {
        if(_airResidence!=null)
        {
            if(_airDefeated)return;_airDefeated=true;DieAirResident();
        }
        _attackHitbox?.End();
        PublishAttack(default);
        PresentationCueRequested?.Invoke(PresentationCue.Died);
        Defeated?.Invoke();
    }

    private AttackTimelineFrame EnrichTimeline(AttackTimelineFrame timeline, Node2D player, Vector2 targetWorldPosition)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            _attackTargetLocked = false;
            _lockedAttackStartTransform = default;
            return default;
        }
        if (timeline.AttackId != _lastAttackTimeline.AttackId)
        {
            _attackTargetLocked = false;
            _lockedAttackStartTransform = default;
        }
        bool lockNow = timeline.Phase == AttackPhase.Active ||
            timeline.Phase == AttackPhase.Telegraph &&
            timeline.PhaseRemainingSeconds <= timeline.PhaseDurationSeconds * .5f;
        if (!_attackTargetLocked && lockNow)
        {
            _lockedAttackStartTransform = _attackCollider.GlobalTransform;
            Vector2 requestedTarget = player?.GlobalPosition ?? targetWorldPosition;
            Vector2 diveDirection = _lockedAttackStartTransform.Origin.DirectionTo(requestedTarget);
            if (diveDirection.IsZeroApprox()) diveDirection = Vector2.Down;
            Vector2 nominalMotion = diveDirection * AttackMotionSpeed * Tuning.AttackSeconds;
            _lockedAttackTarget = ReachableAttackEndpoint(nominalMotion);
            _attackTargetLocked = true;
        }
        return _attackTargetLocked
            ? timeline with { LockedWorldTarget = _lockedAttackTarget, HasLockedTarget = true }
            : timeline;
    }

    private Vector2 ReachableAttackEndpoint(Vector2 nominalMotion)
    {
        float remainingDistance = nominalMotion.Length();
        if (remainingDistance <= .0001f) return _lockedAttackStartTransform.Origin;

        Vector2 direction = nominalMotion / remainingDistance;
        float physicsStepDistance = AttackMotionSpeed / Engine.PhysicsTicksPerSecond;
        Vector2 reachableMotion = Vector2.Zero;
        Transform2D castTransform = GlobalTransform;
        using var collision = new KinematicCollision2D();
        // Match the fixed-direction Dive's real tick-sized motions; one long cast has a coarser safe-fraction interval.
        while (remainingDistance > .0001f)
        {
            Vector2 stepMotion = direction * Mathf.Min(physicsStepDistance, remainingDistance);
            if(_airResidence!=null){stepMotion=ClipAirForecast(castTransform,stepMotion);if(stepMotion.IsZeroApprox())break;}
            if (TestMove(castTransform, stepMotion, collision, SafeMargin, false))
            {
                reachableMotion += collision.GetTravel();
                break;
            }
            reachableMotion += stepMotion;
            castTransform.Origin += stepMotion;
            remainingDistance -= stepMotion.Length();
        }
        return _lockedAttackStartTransform.Origin + reachableMotion;
    }

    private void PublishAttack(AttackTimelineFrame timeline)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            _attackTelegraph?.Present(default);
            AttackFrameChanged?.Invoke(default);
            _lastAttackTimeline = default;
            return;
        }
        AttackShapeFrame shape = timeline.Phase == AttackPhase.Telegraph && timeline.HasLockedTarget
            ? AttackShapeFactory.FromSweptCircle(_attackCollider, _lockedAttackStartTransform,
                timeline.LockedWorldTarget)
            : AttackShapeFactory.FromCollision(_attackCollider, _attackCollider.GlobalTransform,
                AttackShapeKind.Circle);
        AttackPresentationFrame presentation = new AttackPresentationFrame(timeline, shape).Validate();
        _attackTelegraph?.Present(presentation);
        AttackFrameChanged?.Invoke(presentation);
        if (timeline.Phase == AttackPhase.Telegraph &&
            (_lastAttackTimeline.AttackId != timeline.AttackId || _lastAttackTimeline.Phase != AttackPhase.Telegraph))
            PresentationCueRequested?.Invoke(PresentationCue.AttackTelegraph);
        if (timeline.Phase == AttackPhase.Active && _lastAttackTimeline.Phase != AttackPhase.Active)
            PresentationCueRequested?.Invoke(PresentationCue.AttackStarted);
        _lastAttackTimeline = timeline;
    }
}
