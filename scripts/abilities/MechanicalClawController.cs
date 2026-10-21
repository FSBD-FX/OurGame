using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Player;

namespace Knight.Abilities;

public partial class MechanicalClawController : Node
{
    private const float CastingMovementScale = .65f;

    [Export] public NodePath PlayerPath { get; set; } = new("..");
    [Export] public NodePath InputPath { get; set; } = new("../Input");
    [Export] public NodePath MotorPath { get; set; } = new("../Motor");
    [Export] public NodePath ActionModesPath { get; set; } = new("../ActionModes");
    [Export] public NodePath ShieldPath { get; set; } = new("../Shield");
    [Export] public NodePath HealthPath { get; set; } = new("../Health");
    [Export] public NodePath BodyColliderPath { get; set; } = new("../BodyCollider");

    private readonly MechanicalClawSettings _settings = MechanicalClawSettings.Default;
    private MechanicalClawModel _model;
    private MechanicalClawTargetResolver _resolver;
    private CharacterBody2D _player;
    private PlayerInputReader _input;
    private PlayerMotor _motor;
    private PlayerActionModeController _actionModes;
    private PlayerShieldController _shield;
    private HealthComponent _health;
    private CollisionShape2D _bodyCollider;
    private bool _retractRequested;
    private bool _ready;
    private readonly Queue<FramePublication> _publicationQueue = new();
    private bool _publishingFrames;
    private float _stallSeconds;
    private float? _lastPullDistance;
    private Vector2? _lastObservedPullVelocity;
    private bool _changingResidency;
    private bool _hasPendingResidency;
    private GrappleSurface2D _pendingResidency;
    private MechanicalClawTargetResolution _keyboardCastSelection = MechanicalClawTargetResolution.None;
    private PendingBoostRelease? _pendingBoostRelease;

    public MechanicalClawFrame CurrentFrame { get; private set; } = MechanicalClawFrame.Idle();
    // Independent of a temporarily old CurrentFrame while callbacks are running.
    public ulong AuthorityRevision { get; private set; } = 1UL;
    public GrappleSurface2D CurrentTarget { get; private set; }
    public MechanicalClawTargetResolution KeyboardPreview { get; private set; } = MechanicalClawTargetResolution.None;

    // Streamers subscribe first, then immediately read this property to acquire an
    // already-active target. A false callback runs while the old value is still readable.
    public GrappleSurface2D ResidencyTarget { get; private set; }

    public event Action<MechanicalClawFrame, GrappleSurface2D> ClawFrameChanged;

    // Consumers pin the target's owning sector on true. On false they first guarantee
    // player/landing residency, then unpin the target sector; abilities never own sector ids.
    public event Action<GrappleSurface2D, bool> TargetResidencyChanged;

    public override void _Ready()
    {
        _settings.Validate();
        _model = new MechanicalClawModel(_settings);
        _resolver = new MechanicalClawTargetResolver(_settings);
        ResolveDependencies();
        _actionModes.AttackModeRequested += OnAttackModeRequested;
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        CurrentFrame = MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity);
        _motor.ApplyGrappleConstraint(Vector2.Zero, 1f);
        _ready = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_ready || GetTree().Paused) return;
        ResolveDependencies();
        if (_pendingBoostRelease.HasValue)
        {
            ResumeBoostRelease();
            return;
        }
        PlayerInputFrame sampled = _input.CurrentFrame;
        MechanicalClawFrame previous = CurrentFrame with
        {
            CasterWorldPosition = _player.GlobalPosition,
            CasterVelocity = _player.Velocity
        };
        bool active = IsLive(previous.State);
        bool attached = IsAttached(previous.State);
        bool realAttached = attached && CurrentTarget != null &&
            GodotObject.IsInstanceValid(CurrentTarget) && CurrentTarget.IsAvailable;
        bool clawPressed = sampled.KeyboardActions ? sampled.ClawPressed : sampled.PrimaryPressed;
        MechanicalClawCommand command =
            (sampled.ShieldHeld || sampled.AttackModePressed || sampled.KeyboardActions && sampled.AttackPressed || _retractRequested) && active
                ? MechanicalClawCommand.Cancel
                : sampled.JumpPressed && attached
                        ? MechanicalClawCommand.BoostRelease
                        : clawPressed && active
                            ? MechanicalClawCommand.Cancel
                            : clawPressed && _actionModes.CanBeginGrapple
                                ? MechanicalClawCommand.Cast
                                : MechanicalClawCommand.None;
        bool winningCommandConsumesPrimary = sampled.PrimaryPressed && command != MechanicalClawCommand.Cast &&
            ((sampled.ShieldHeld || sampled.AttackModePressed || sampled.DashPressed) && active ||
                sampled.JumpPressed && attached);
        if (winningCommandConsumesPrimary)
            _input.ConsumePrimaryPressedForCurrentPhysicsFrame();
        if (attached && sampled.JumpPressed)
            _motor.SuppressJumpForCurrentPhysicsFrame();

        bool castRequested = command == MechanicalClawCommand.Cast;
        if (castRequested) InvalidateAuthority();
        ulong authority = AuthorityRevision;
        KeyboardPreview = sampled.KeyboardActions && !active && _health.CurrentHealth > 0
            ? _resolver.ResolveKeyboard(_player, _input.KeyboardAimDirection)
            : MechanicalClawTargetResolution.None;
        if (sampled.KeyboardActions && castRequested) _keyboardCastSelection = KeyboardPreview;
        MechanicalClawTargetResolution resolution = ResolveTarget(previous, sampled.MouseWorld, castRequested);
        MechanicalClawTargetSnapshot snapshot = resolution.Snapshot;
        if (RequiresLiveTarget(previous.State) && !snapshot.Exists)
            _input.ClearSampledFrame();

        float seconds = Mathf.Max(0f, (float)delta);
        bool arrived = false;
        bool targetContactObstructed = false;
        bool stalled = false;
        if (attached)
        {
            if (_bodyCollider.Shape is not CapsuleShape2D)
                throw new InvalidOperationException("Mechanical claw docking requires a CapsuleShape2D body collider.");
            EvaluateSavedTargetContact(previous, out arrived, out targetContactObstructed);
            if (arrived || previous.State == MechanicalClawState.Swing)
            {
                ResetStallTracking();
            }
            else if (command == MechanicalClawCommand.None && !GetTree().Paused)
            {
                float distance = _player.GlobalPosition.DistanceTo(previous.DockPoint);
                _lastPullDistance ??= distance;
                Vector2 dockward = _player.GlobalPosition.DirectionTo(previous.DockPoint);
                float currentDockwardSpeed = _player.Velocity.Dot(dockward);
                float priorDockwardSpeed = _lastObservedPullVelocity.HasValue
                    ? _lastObservedPullVelocity.Value.Dot(dockward)
                    : currentDockwardSpeed;
                bool observedBrakingProgress = snapshot.HasLineOfSight &&
                    float.IsFinite(currentDockwardSpeed) && float.IsFinite(priorDockwardSpeed) &&
                    currentDockwardSpeed < -.01f &&
                    currentDockwardSpeed - priorDockwardSpeed > .01f;
                if (observedBrakingProgress)
                {
                    _lastPullDistance = distance;
                    _stallSeconds = 0f;
                }
                else if (_lastPullDistance.Value - distance >= _settings.StallProgressEpsilon)
                {
                    _lastPullDistance = distance;
                    _stallSeconds = 0f;
                }
                else
                {
                    _stallSeconds += seconds;
                    stalled = _stallSeconds >= _settings.StallTimeoutSeconds;
                }
                _lastObservedPullVelocity = _player.Velocity;
            }
        }

        var modelInput = new MechanicalClawInput(
            Command: command,
            SwingAxis: sampled.MoveAxis,
            Arrived: arrived,
            DockPoint: previous.DockPoint);
        MechanicalClawFrame next = _model.Step(previous, modelInput, snapshot,
            seconds);
        if (next.Failure == MechanicalClawFailure.TargetLost && previous.Failure != next.Failure)
            authority = InvalidateAuthority();
        if (sampled.KeyboardActions && next.State is MechanicalClawState.Aim or MechanicalClawState.Windup)
            KeyboardPreview = resolution;
        else if (next.State != MechanicalClawState.Idle)
            KeyboardPreview = MechanicalClawTargetResolution.None;
        if (previous.LockedTargetRuntimeId == 0UL && next.LockedTargetRuntimeId != 0UL)
        {
            if (_bodyCollider.Shape is not CapsuleShape2D capsule)
                throw new InvalidOperationException("Mechanical claw docking requires a CapsuleShape2D body collider.");
            next = next with
            {
                DockPoint = MechanicalClawArrivalGeometry.DockPoint(next.AnchorPoint,
                    next.SurfaceNormal, capsule, _settings.MinimumRopeLength, _settings.ArrivalMargin)
            };
        }
        if (!attached && IsAttached(next.State))
        {
            _stallSeconds = 0f;
            _lastPullDistance = _player.GlobalPosition.DistanceTo(next.DockPoint);
            _lastObservedPullVelocity = _player.Velocity;
        }

        bool realAttachedPull = command == MechanicalClawCommand.None && IsAttached(next.State) &&
            snapshot.Exists && !snapshot.IsLegacyInteraction;
        if (realAttachedPull)
        {
            if (!snapshot.HasLineOfSight || (!arrived && (targetContactObstructed || stalled)) ||
                PullMotionIsObstructed(previous, next, seconds))
                next = ObstructedRelease(next);
        }
        if (next.State is MechanicalClawState.Idle or MechanicalClawState.Released or
            MechanicalClawState.Failed)
        {
            _keyboardCastSelection = MechanicalClawTargetResolution.None;
            _retractRequested = false;
            ResetStallTracking();
        }
        bool boostRelease = next.State == MechanicalClawState.Released &&
            next.ReleaseKind == MechanicalClawReleaseKind.Boost;
        MechanicalClawFrame lifecycleBeforeTargetUpdate = CurrentFrame;
        UpdateTarget(previous, next, resolution.Surface, snapshot, boostRelease);
        if (!IsCurrentAuthority(authority) || CurrentFrame != lifecycleBeforeTargetUpdate) return;

        if (next.State == MechanicalClawState.Latched && snapshot.IsLegacyInteraction &&
            CurrentTarget != null && GodotObject.IsInstanceValid(CurrentTarget))
        {
            LegacyThreadInteractionAdapter adapter = CurrentTarget.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "LegacyThreadInteractionAdapter");
            if (adapter?.TryApply(_player, _player.GlobalPosition) != true)
            {
                PublishFailureAndReset(next, MechanicalClawFailure.TargetLost);
                return;
            }
            if (!IsCurrentAuthority(authority)) return;
            Publish(next);
            if (!IsCurrentAuthority(authority) || CurrentFrame != next) return;
            MechanicalClawFrame releasedSource = next with
            {
                CasterWorldPosition = _player.GlobalPosition,
                CasterVelocity = _player.Velocity
            };
            MechanicalClawFrame released = _model.Step(releasedSource,
                modelInput with { Command = MechanicalClawCommand.Cancel },
                snapshot,
                0f);
            CurrentTarget = null;
            Publish(released);
            if (IsCurrentAuthority(authority) && CurrentFrame == released) ApplyMotor(released, sampled.MoveAxis);
            return;
        }

        if (boostRelease)
        {
            ulong motionRevision = _motor.MotionAuthorityRevision;
            MechanicalClawFrame boost = next with { State = MechanicalClawState.Boost };
            Publish(boost);
            if (!IsCurrentAuthority(authority) || CurrentFrame != boost) return;
            _pendingBoostRelease = new(next, sampled.MoveAxis, realAttached, authority, motionRevision, BoostReleaseStage.Unpin);
            ResumeBoostRelease();
            return;
        }
        else
        {
            Publish(next);
        }
        if (IsCurrentAuthority(authority) && CurrentFrame == next)
            ApplyMotor(next, sampled.MoveAxis,
                attached && !previous.VelocityCorrection.IsZeroApprox(), boostRelease && realAttached);
    }

    // A synchronous callback may pause between Boost, unpin, Released and the
    // velocity application. Resume only the unfinished stage under the same
    // authority; never republish an already-delivered transition or replay Space.
    private void ResumeBoostRelease()
    {
        while (_pendingBoostRelease is { } pending)
        {
            if (!IsCurrentAuthority(pending.Authority))
            {
                _pendingBoostRelease = null;
                return;
            }
            if (_motor.MotionAuthorityRevision != pending.MotionRevision)
            {
                // Space already ended this cast, but its unapplied boost no
                // longer owns motion. Retire only this old release without
                // projection/cap, fall reset, input clear or budget changes.
                TryExitWithoutRelease(out _);
                return;
            }
            if (GetTree().Paused) return;
            if (pending.Stage == BoostReleaseStage.Unpin)
            {
                _pendingBoostRelease = pending with { Stage = BoostReleaseStage.Publish };
                SetResidencyTarget(null);
                if (!IsCurrentAuthority(pending.Authority)) return;
                CurrentTarget = null;
                continue;
            }
            if (pending.Stage == BoostReleaseStage.Publish)
            {
                _pendingBoostRelease = pending with { Stage = BoostReleaseStage.Apply };
                Publish(pending.Frame);
                continue;
            }
            _pendingBoostRelease = null;
            if (CurrentFrame == pending.Frame)
                ApplyMotor(pending.Frame, pending.SwingAxis, realAttachedBoost: pending.RealAttached);
            return;
        }
    }

    public override void _ExitTree()
    {
        InvalidateAuthority();
        KeyboardPreview = MechanicalClawTargetResolution.None;
        _keyboardCastSelection = MechanicalClawTargetResolution.None;
        _ready = false;
        if (GodotObject.IsInstanceValid(_actionModes))
            _actionModes.AttackModeRequested -= OnAttackModeRequested;
        if (GodotObject.IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
        if (GodotObject.IsInstanceValid(_motor))
            _motor.ClearGrappleConstraint();
        SetResidencyTarget(null);
        CurrentTarget = null;
        ResetStallTracking();
    }

    public void ResetTransientState()
    {
        ulong authority = InvalidateAuthority();
        ulong nextCastId = CurrentFrame.NextCastId;
        KeyboardPreview = MechanicalClawTargetResolution.None;
        _keyboardCastSelection = MechanicalClawTargetResolution.None;
        if (!_ready)
        {
            CurrentFrame = MechanicalClawFrame.Idle() with { NextCastId = nextCastId };
            SetResidencyTarget(null);
            CurrentTarget = null;
            ResetStallTracking();
            return;
        }

        _model = new MechanicalClawModel(_settings);
        SetResidencyTarget(null);
        if (!IsCurrentAuthority(authority)) return;
        CurrentTarget = null;
        _retractRequested = false;
        ResetStallTracking();
        _input.ClearSampledFrame();
        KeyboardPreview = MechanicalClawTargetResolution.None;
        _keyboardCastSelection = MechanicalClawTargetResolution.None;
        _motor.ClearGrappleConstraint();
        if (CurrentFrame.State == MechanicalClawState.Idle || HasPendingIdle()) return;
        _publicationQueue.Clear();
        Publish(MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity) with { NextCastId = nextCastId });
    }

    public bool NotifyTargetWillUnload(Node subtree)
    {
        if (!IsNodeInside(CurrentTarget, subtree)) return false;
        PublishTargetLostAndReset();
        return true;
    }

    // Called only after the Motor's read-only eligibility decision. This is not
    // an ordinary release: no boost, inward projection, speed cap or fall reset.
    internal bool TryDetachForThruster(out ulong completedAuthority)
        => TryExitWithoutRelease(out completedAuthority);

    // Shared neutral exit for an accepted jet or a superseded pending release.
    // It clears constraints/pins, never writes velocity or resets input/budget.
    private bool TryExitWithoutRelease(out ulong completedAuthority)
    {
        completedAuthority = AuthorityRevision;
        if (!_ready || _publishingFrames || _changingResidency) return false;
        ulong authority = InvalidateAuthority();
        ulong nextCastId = CurrentFrame.NextCastId;
        KeyboardPreview = MechanicalClawTargetResolution.None;
        _keyboardCastSelection = MechanicalClawTargetResolution.None;
        _publicationQueue.Clear();
        _model = new MechanicalClawModel(_settings);
        _retractRequested = false;
        ResetStallTracking();
        SetResidencyTarget(null);
        if (!IsCurrentAuthority(authority)) return false;
        CurrentTarget = null;
        _motor.DetachGrappleWithoutRelease();
        Publish(MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity) with { NextCastId = nextCastId });
        completedAuthority = authority;
        return IsCurrentAuthority(authority) && CurrentFrame.State == MechanicalClawState.Idle &&
            CurrentFrame.NextCastId == nextCastId && CurrentTarget == null && ResidencyTarget == null;
    }

    private void PublishTargetLostAndReset()
        => PublishFailureAndReset(CurrentFrame, MechanicalClawFailure.TargetLost);

    private void PublishFailureAndReset(MechanicalClawFrame source, MechanicalClawFailure failure)
    {
        ulong authority = InvalidateAuthority();
        KeyboardPreview = MechanicalClawTargetResolution.None;
        _keyboardCastSelection = MechanicalClawTargetResolution.None;
        MechanicalClawFrame failed = source with
        {
            State = MechanicalClawState.Failed,
            Failure = failure,
            EffectiveVisibility = false,
            VelocityCorrection = Vector2.Zero
        };
        MechanicalClawFrame lifecycleBeforeResidency = CurrentFrame;
        SetResidencyTarget(null);
        if (!IsCurrentAuthority(authority) || CurrentFrame != lifecycleBeforeResidency) return;
        CurrentTarget = null;
        _retractRequested = false;
        ResetStallTracking();
        _input.ClearSampledFrame();
        _motor.ClearGrappleConstraint();
        _model = new MechanicalClawModel(_settings);
        _publicationQueue.Clear();
        _publicationQueue.Enqueue(new FramePublication(failed, null, authority));
        _publicationQueue.Enqueue(new FramePublication(
            MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity) with { NextCastId = source.NextCastId }, null, authority));
        DrainPublications();
    }

    private MechanicalClawTargetResolution ResolveTarget(MechanicalClawFrame frame,
        Vector2 cursorWorld, bool routedPress)
    {
        if (NeedsLockedTarget(frame.State))
        {
            MechanicalClawTargetSnapshot locked = _resolver.SnapshotLocked(_player, CurrentTarget,
                frame.AnchorPoint, frame.SurfaceNormal);
            if (!locked.Exists && frame.LockedTargetRuntimeId != 0UL)
                locked = locked with { RuntimeId = frame.LockedTargetRuntimeId, WorldAnchor = frame.AnchorPoint };
            return new MechanicalClawTargetResolution(locked.Exists ? CurrentTarget : null,
                locked, float.PositiveInfinity);
        }

        if (routedPress || frame.State is MechanicalClawState.Aim or MechanicalClawState.Windup)
        {
            if (!_input.CurrentFrame.KeyboardActions) return _resolver.Resolve(_player, cursorWorld);
            if (_keyboardCastSelection.Surface == null) return MechanicalClawTargetResolution.None;
            MechanicalClawTargetSnapshot selected = _keyboardCastSelection.Snapshot;
            return _keyboardCastSelection with { Snapshot = _resolver.SnapshotLocked(_player,
                _keyboardCastSelection.Surface, selected.WorldAnchor, selected.SurfaceNormal) };
        }
        return MechanicalClawTargetResolution.None;
    }

    private void UpdateTarget(MechanicalClawFrame previous, MechanicalClawFrame next,
        GrappleSurface2D resolved, MechanicalClawTargetSnapshot snapshot,
        bool deferBoostReleaseClear)
    {
        if (!deferBoostReleaseClear && (next.State is MechanicalClawState.Idle or
                MechanicalClawState.Failed or MechanicalClawState.Released or
                MechanicalClawState.Retract ||
            next.Failure == MechanicalClawFailure.TargetLost))
        {
            SetResidencyTarget(null);
            CurrentTarget = null;
            return;
        }
        if (resolved != null && GodotObject.IsInstanceValid(resolved)) CurrentTarget = resolved;
        if (previous.State != MechanicalClawState.Travel && next.State == MechanicalClawState.Travel &&
            !snapshot.IsLegacyInteraction && resolved != null && GodotObject.IsInstanceValid(resolved) &&
            next.LockedTargetRuntimeId != 0UL && resolved.GetInstanceId() == next.LockedTargetRuntimeId)
            SetResidencyTarget(resolved);
    }

    private void SetResidencyTarget(GrappleSurface2D next)
    {
        _pendingResidency = next;
        _hasPendingResidency = true;
        if (_changingResidency) return;
        _changingResidency = true;
        try
        {
            while (_hasPendingResidency)
            {
                next = _pendingResidency;
                _pendingResidency = null;
                _hasPendingResidency = false;
                if (ReferenceEquals(ResidencyTarget, next)) continue;
                GrappleSurface2D previous = ResidencyTarget;
                if (previous != null && GodotObject.IsInstanceValid(previous))
                    DispatchResidencyChanged(previous, false);
                ResidencyTarget = null;

                // A reset requested inside the false callback supersedes the stale request.
                if (_hasPendingResidency)
                {
                    next = _pendingResidency;
                    _pendingResidency = null;
                    _hasPendingResidency = false;
                }
                if (next != null && GodotObject.IsInstanceValid(next))
                {
                    ResidencyTarget = next;
                    DispatchResidencyChanged(next, true);
                }
            }
        }
        finally { _changingResidency = false; }
    }

    private void Publish(MechanicalClawFrame frame)
    {
        EnqueuePublication(frame);
        DrainPublications();
    }

    private void EnqueuePublication(MechanicalClawFrame frame)
    {
        GrappleSurface2D target = CurrentTarget != null && GodotObject.IsInstanceValid(CurrentTarget)
            ? CurrentTarget
            : null;
        _publicationQueue.Enqueue(new FramePublication(frame, target, AuthorityRevision));
    }

    private void DrainPublications()
    {
        if (_publishingFrames) return;
        _publishingFrames = true;
        try
        {
            while (_publicationQueue.Count > 0)
            {
                FramePublication publication = _publicationQueue.Dequeue();
                if (!IsCurrentAuthority(publication.Authority)) continue;
                CurrentFrame = publication.Frame;
                DispatchFrameChanged(publication.Frame, publication.Target, publication.Authority);
            }
        }
        finally
        {
            _publishingFrames = false;
        }
    }

    private bool HasPendingIdle()
    {
        foreach (FramePublication publication in _publicationQueue)
            if (publication.Frame.State == MechanicalClawState.Idle) return true;
        return false;
    }

    private void DispatchFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target, ulong authority)
    {
        Action<MechanicalClawFrame, GrappleSurface2D> observers = ClawFrameChanged;
        if (observers == null) return;
        foreach (Delegate observer in observers.GetInvocationList())
        {
            if (!IsCurrentAuthority(authority)) break;
            try
            {
                ((Action<MechanicalClawFrame, GrappleSurface2D>)observer)(frame, target);
            }
            catch (Exception exception)
            {
                GD.PushWarning($"{nameof(MechanicalClawController)} isolated a throwing " +
                    $"{nameof(ClawFrameChanged)} observer: {exception.Message}");
            }
        }
    }

    private void DispatchResidencyChanged(GrappleSurface2D target, bool resident)
    {
        ulong authority = AuthorityRevision;
        Action<GrappleSurface2D, bool> observers = TargetResidencyChanged;
        if (observers == null) return;
        foreach (Delegate observer in observers.GetInvocationList())
        {
            // Every old resident still receives false to release its pin. A
            // superseded true grant must not reach additional observers.
            if (resident && !IsCurrentAuthority(authority)) break;
            try
            {
                ((Action<GrappleSurface2D, bool>)observer)(target, resident);
            }
            catch (Exception exception)
            {
                GD.PushWarning($"{nameof(MechanicalClawController)} isolated a throwing " +
                    $"{nameof(TargetResidencyChanged)} observer: {exception.Message}");
            }
        }
    }

    private void ApplyMotor(MechanicalClawFrame frame, float swingAxis,
        bool preserveLaunchMomentum = false, bool realAttachedBoost = false)
    {
        if (frame.State == MechanicalClawState.Released)
        {
            Vector2 outwardNormal = frame.SurfaceNormal.IsZeroApprox()
                ? frame.AnchorPoint.DirectionTo(frame.CasterWorldPosition)
                : frame.SurfaceNormal;
            bool boosted = frame.ReleaseKind == MechanicalClawReleaseKind.Boost;
            Vector2 boostDelta = boosted ? BoostDelta(frame, swingAxis) : Vector2.Zero;
            _motor.ApplyGrappleRelease(outwardNormal, boostDelta, _settings.ReleaseSpeedLimit,
                boosted || preserveLaunchMomentum);
            if (boosted && realAttachedBoost && !_motor.IsDashing && _player.Velocity.Y < 0f)
                _motor.ResetFallTracking();
            return;
        }
        float movementScale = frame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or
            MechanicalClawState.Travel ? CastingMovementScale : 1f;
        Vector2 correction = frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
            MechanicalClawState.Swing or MechanicalClawState.Boost
            ? frame.VelocityCorrection
            : Vector2.Zero;
        bool realAttached = IsAttached(frame.State) && CurrentTarget != null &&
            GodotObject.IsInstanceValid(CurrentTarget) && CurrentTarget.IsAvailable;
        _motor.ApplyGrappleConstraint(correction, movementScale, realAttached,
            automaticPull: realAttached && frame.State == MechanicalClawState.Reel,
            swingAnchor: frame.AnchorPoint,
            swingRopeLength: realAttached && frame.State == MechanicalClawState.Swing ? frame.RopeLength : 0f);
    }

    private void EvaluateSavedTargetContact(MechanicalClawFrame frame,
        out bool arrived, out bool obstructed)
    {
        bool insideEnvelope = MechanicalClawArrivalGeometry.IsArrived(_player.GlobalPosition,
            frame.DockPoint, _settings.ArrivalTolerance);
        bool hasTargetContact = false;
        bool hasIncompatibleTargetContact = false;
        Vector2 savedNormal = frame.SurfaceNormal.Normalized();
        if (CurrentTarget != null && GodotObject.IsInstanceValid(CurrentTarget))
        {
            for (int index = 0; index < _player.GetSlideCollisionCount(); index++)
            {
                KinematicCollision2D collision = _player.GetSlideCollision(index);
                if (collision?.GetCollider() is not Node collider ||
                    !MechanicalClawTargetResolver.ColliderBelongsToSurface(collider, CurrentTarget)) continue;
                hasTargetContact = true;
                if (collision.GetNormal().Normalized().Dot(savedNormal) < .8f)
                    hasIncompatibleTargetContact = true;
            }
        }
        arrived = insideEnvelope && !hasIncompatibleTargetContact;
        // Once genuinely arrived, lateral swing may slide against the saved face
        // away from its original dock. Native collision still prevents penetration.
        obstructed = hasTargetContact && (hasIncompatibleTargetContact ||
            (!insideEnvelope && frame.State != MechanicalClawState.Swing));
    }

    private bool PullMotionIsObstructed(MechanicalClawFrame previous,
        MechanicalClawFrame next, float seconds)
    {
        if (seconds <= 0f) return false;
        Vector2 previousCorrection = previous.State == MechanicalClawState.Reel &&
            next.State == MechanicalClawState.Swing ? Vector2.Zero : previous.VelocityCorrection;
        Vector2 intendedVelocity = _player.Velocity +
            (next.VelocityCorrection - previousCorrection);
        Vector2 intendedMotion = intendedVelocity * seconds;
        if (intendedMotion.IsZeroApprox()) return false;
        using var collision = new KinematicCollision2D();
        bool wouldCollide = _player.TestMove(_player.GlobalTransform, intendedMotion, collision,
            _player.SafeMargin, false);
        if (!wouldCollide) return false;
        Node collider = collision.GetCollider() as Node;
        bool targetCollider = collider != null && CurrentTarget != null &&
            GodotObject.IsInstanceValid(CurrentTarget) &&
            MechanicalClawTargetResolver.ColliderBelongsToSurface(collider, CurrentTarget);
        // Downward inertia may reach the departure floor before the bounded
        // upward servo reverses it. Let MoveAndSlide resolve that real landing;
        // the same Reel can then lift off. Walls, ceilings and target faces
        // retain the normal obstruction path.
        Vector2 up = _player.UpDirection.Normalized();
        bool brakingOntoFloor = next.State == MechanicalClawState.Reel && !targetCollider &&
            intendedMotion.Dot(up) < 0f &&
            (next.VelocityCorrection - previousCorrection).Dot(up) > 0f &&
            (next.DockPoint - _player.GlobalPosition).Dot(up) > _settings.ArrivalTolerance &&
            collision.GetNormal().Dot(up) >= Mathf.Cos(_player.FloorMaxAngle);
        if (brakingOntoFloor) return false;
        Vector2 predictedContact = _player.GlobalPosition + collision.GetTravel();
        bool expectedTargetContact = targetCollider &&
            collision.GetNormal().Normalized().Dot(next.SurfaceNormal.Normalized()) >= .8f &&
            (next.State == MechanicalClawState.Swing ||
                MechanicalClawArrivalGeometry.IsArrived(predictedContact, next.DockPoint,
                    _settings.ArrivalTolerance));
        return !expectedTargetContact;
    }

    private static MechanicalClawFrame ObstructedRelease(MechanicalClawFrame frame) => frame with
    {
        State = MechanicalClawState.Released,
        Failure = MechanicalClawFailure.Obstructed,
        VelocityCorrection = Vector2.Zero,
        EffectiveVisibility = false,
        Arrived = false,
        ReleaseKind = MechanicalClawReleaseKind.Obstructed
    };

    private void ResetStallTracking()
    {
        _stallSeconds = 0f;
        _lastPullDistance = null;
        _lastObservedPullVelocity = null;
    }

    private Vector2 BoostDelta(MechanicalClawFrame frame, float swingAxis)
    {
        Vector2 toAnchor = frame.CasterWorldPosition.DirectionTo(frame.AnchorPoint);
        Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
        float projected = frame.CasterVelocity.Dot(tangent);
        float requested = Mathf.Abs(swingAxis) > .0001f ? swingAxis : Mathf.Sign(projected);
        float direction = Mathf.IsZeroApprox(requested) ? 1f : Mathf.Sign(requested);
        return Vector2.Up * _settings.BoostUpwardSpeed +
            tangent * (direction * _settings.BoostTangentialSpeed);
    }

    private void OnAttackModeRequested() => _retractRequested = true;
    private void OnDamaged(int remaining, Vector2 impulse)
    {
        if (remaining <= 0)
        {
            ResetTransientState();
            return;
        }

        ResetAfterNonfatalDamage();
    }

    private void ResetAfterNonfatalDamage()
    {
        ulong authority = InvalidateAuthority();
        ulong nextCastId = CurrentFrame.NextCastId;
        KeyboardPreview = MechanicalClawTargetResolution.None;
        _keyboardCastSelection = MechanicalClawTargetResolution.None;
        if (!_ready)
        {
            CurrentFrame = MechanicalClawFrame.Idle() with { NextCastId = nextCastId };
            SetResidencyTarget(null);
            CurrentTarget = null;
            ResetStallTracking();
            return;
        }

        _model = new MechanicalClawModel(_settings);
        SetResidencyTarget(null);
        if (!IsCurrentAuthority(authority)) return;
        CurrentTarget = null;
        _retractRequested = false;
        ResetStallTracking();
        _input.ConsumeOneShotEdgesForCurrentPhysicsFrame();
        _motor.ClearGrappleConstraint();
        if (CurrentFrame.State == MechanicalClawState.Idle || HasPendingIdle()) return;
        _publicationQueue.Clear();
        Publish(MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity) with { NextCastId = nextCastId });
    }

    private void OnDied() => ResetTransientState();

    private void ResolveDependencies()
    {
        _player ??= GetNode<CharacterBody2D>(PlayerPath);
        _input ??= GetNode<PlayerInputReader>(InputPath);
        _motor ??= GetNode<PlayerMotor>(MotorPath);
        _actionModes ??= GetNode<PlayerActionModeController>(ActionModesPath);
        _shield ??= GetNodeOrNull<PlayerShieldController>(ShieldPath);
        _health ??= GetNode<HealthComponent>(HealthPath);
        _bodyCollider ??= GetNode<CollisionShape2D>(BodyColliderPath);
    }

    private static bool IsLive(MechanicalClawState state) => state is not
        MechanicalClawState.Idle and not MechanicalClawState.Failed;

    private static bool NeedsLockedTarget(MechanicalClawState state) => state is
        MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or
        MechanicalClawState.Swing or MechanicalClawState.Boost or MechanicalClawState.Released or
        MechanicalClawState.Retract;

    private static bool IsAttached(MechanicalClawState state) => state is
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or
        MechanicalClawState.Boost;

    private static bool RequiresLiveTarget(MechanicalClawState state) =>
        state == MechanicalClawState.Travel || IsAttached(state);

    private static bool IsNodeInside(Node node, Node possibleAncestor)
    {
        if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(possibleAncestor)) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (ReferenceEquals(current, possibleAncestor)) return true;
        return false;
    }

    private readonly record struct FramePublication(MechanicalClawFrame Frame,
        GrappleSurface2D Target, ulong Authority);

    private enum BoostReleaseStage { Unpin, Publish, Apply }
    private readonly record struct PendingBoostRelease(MechanicalClawFrame Frame,
        float SwingAxis, bool RealAttached, ulong Authority, ulong MotionRevision, BoostReleaseStage Stage);

    private ulong InvalidateAuthority()
    {
        AuthorityRevision = checked(AuthorityRevision + 1UL);
        _pendingBoostRelease = null;
        _publicationQueue.Clear();
        return AuthorityRevision;
    }

    private bool IsCurrentAuthority(ulong authority) => _ready && AuthorityRevision == authority;
}
