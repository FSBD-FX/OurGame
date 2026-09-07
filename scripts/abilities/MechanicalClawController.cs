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

    private readonly MechanicalClawSettings _settings = MechanicalClawSettings.Default;
    private MechanicalClawModel _model;
    private MechanicalClawTargetResolver _resolver;
    private CharacterBody2D _player;
    private PlayerInputReader _input;
    private PlayerMotor _motor;
    private PlayerActionModeController _actionModes;
    private PlayerShieldController _shield;
    private HealthComponent _health;
    private bool _retractRequested;
    private bool _ready;
    private readonly Queue<FramePublication> _publicationQueue = new();
    private bool _publishingFrames;

    public MechanicalClawFrame CurrentFrame { get; private set; } = MechanicalClawFrame.Idle();
    public GrappleSurface2D CurrentTarget { get; private set; }

    public event Action<MechanicalClawFrame, GrappleSurface2D> ClawFrameChanged;

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
        if (!_ready) return;
        ResolveDependencies();
        PlayerInputFrame sampled = _input.CurrentFrame;
        MechanicalClawFrame previous = CurrentFrame with
        {
            CasterWorldPosition = _player.GlobalPosition,
            CasterVelocity = _player.Velocity
        };
        bool active = IsLive(previous.State);
        bool routedPress = sampled.PrimaryPressed && (active || _actionModes.CanBeginGrapple);
        MechanicalClawTargetResolution resolution = ResolveTarget(previous, sampled.MouseWorld, routedPress);
        MechanicalClawTargetSnapshot snapshot = resolution.Snapshot;

        // A same-frame second deployment attempt owns the highest failure priority. The locked
        // target may have disappeared concurrently, but no replacement cast may be allocated.
        if (active && sampled.PrimaryPressed && !snapshot.Exists)
        {
            snapshot = new MechanicalClawTargetSnapshot(
                previous.LockedTargetRuntimeId,
                true,
                CurrentTarget != null && GodotObject.IsInstanceValid(CurrentTarget)
                    ? CurrentTarget.Material
                    : GrappleSurfaceMaterial.GripStone,
                previous.AnchorPoint,
                true,
                false);
        }

        bool shieldActive = sampled.ShieldHeld || (_shield?.IsActive ?? false);
        bool attached = IsAttached(previous.State);
        if (active && (sampled.PrimaryReleased || !sampled.PrimaryHeld))
            _retractRequested = true;
        if (attached && sampled.JumpPressed)
            _motor.SuppressJumpForCurrentPhysicsFrame();
        var modelInput = new MechanicalClawInput(
            PrimaryPressed: routedPress,
            PrimaryHeld: sampled.PrimaryHeld,
            PrimaryReleased: active && sampled.PrimaryReleased,
            DetachPressed: active && _retractRequested,
            SwingAxis: sampled.MoveAxis,
            ReelAxis: shieldActive ? 0f : sampled.Aim.Y,
            ShieldActive: shieldActive,
            BoostPressed: attached && sampled.JumpPressed,
            Grounded: _player.IsOnFloor());
        MechanicalClawFrame next = _model.Step(previous, modelInput, snapshot,
            Mathf.Max(0f, (float)delta));
        if (next.State is MechanicalClawState.Idle or MechanicalClawState.Released or
            MechanicalClawState.Failed)
            _retractRequested = false;
        UpdateTarget(previous, next, resolution.Surface);

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
            Publish(next);
            MechanicalClawFrame releasedSource = next with
            {
                CasterWorldPosition = _player.GlobalPosition,
                CasterVelocity = _player.Velocity
            };
            MechanicalClawFrame released = _model.Step(releasedSource,
                modelInput with { PrimaryPressed = false, PrimaryReleased = false, DetachPressed = true },
                snapshot,
                0f);
            CurrentTarget = null;
            Publish(released);
            ApplyMotor(released);
            return;
        }

        Publish(next);
        ApplyMotor(next);
    }

    public override void _ExitTree()
    {
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
        CurrentTarget = null;
    }

    public void ResetTransientState()
    {
        if (!_ready)
        {
            CurrentFrame = MechanicalClawFrame.Idle();
            CurrentTarget = null;
            return;
        }

        _model = new MechanicalClawModel(_settings);
        CurrentTarget = null;
        _retractRequested = false;
        _motor.ClearGrappleConstraint();
        if (CurrentFrame.State == MechanicalClawState.Idle || HasPendingIdle()) return;
        Publish(MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity));
    }

    public bool NotifyTargetWillUnload(Node subtree)
    {
        if (!IsNodeInside(CurrentTarget, subtree)) return false;
        PublishTargetLostAndReset();
        return true;
    }

    private void PublishTargetLostAndReset()
        => PublishFailureAndReset(CurrentFrame, MechanicalClawFailure.TargetLost);

    private void PublishFailureAndReset(MechanicalClawFrame source, MechanicalClawFailure failure)
    {
        MechanicalClawFrame failed = source with
        {
            State = MechanicalClawState.Failed,
            Failure = failure,
            EffectiveVisibility = false,
            VelocityCorrection = Vector2.Zero
        };
        CurrentTarget = null;
        _retractRequested = false;
        _motor.ClearGrappleConstraint();
        _model = new MechanicalClawModel(_settings);
        _publicationQueue.Enqueue(new FramePublication(failed, null));
        _publicationQueue.Enqueue(new FramePublication(
            MechanicalClawFrame.Idle(_player.GlobalPosition, _player.Velocity), null));
        DrainPublications();
    }

    private MechanicalClawTargetResolution ResolveTarget(MechanicalClawFrame frame,
        Vector2 cursorWorld, bool routedPress)
    {
        if (NeedsLockedTarget(frame.State))
        {
            MechanicalClawTargetSnapshot locked = _resolver.SnapshotLocked(_player, CurrentTarget,
                frame.AnchorPoint);
            if (!locked.Exists && frame.LockedTargetRuntimeId != 0UL)
                locked = locked with { RuntimeId = frame.LockedTargetRuntimeId, WorldAnchor = frame.AnchorPoint };
            return new MechanicalClawTargetResolution(locked.Exists ? CurrentTarget : null,
                locked, float.PositiveInfinity);
        }

        if (routedPress || frame.State is MechanicalClawState.Aim or MechanicalClawState.Windup)
            return _resolver.Resolve(_player, cursorWorld);
        return MechanicalClawTargetResolution.None;
    }

    private void UpdateTarget(MechanicalClawFrame previous, MechanicalClawFrame next,
        GrappleSurface2D resolved)
    {
        if (next.State is MechanicalClawState.Idle or MechanicalClawState.Failed ||
            next.Failure == MechanicalClawFailure.TargetLost)
        {
            CurrentTarget = null;
            return;
        }
        if (resolved != null && GodotObject.IsInstanceValid(resolved)) CurrentTarget = resolved;
        if (previous.LockedTargetRuntimeId != 0UL && next.LockedTargetRuntimeId == 0UL &&
            next.State is MechanicalClawState.Released or MechanicalClawState.Retract)
            CurrentTarget = null;
    }

    private void Publish(MechanicalClawFrame frame)
    {
        GrappleSurface2D target = CurrentTarget != null && GodotObject.IsInstanceValid(CurrentTarget)
            ? CurrentTarget
            : null;
        _publicationQueue.Enqueue(new FramePublication(frame, target));
        DrainPublications();
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
                CurrentFrame = publication.Frame;
                DispatchFrameChanged(publication.Frame, publication.Target);
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

    private void DispatchFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        Action<MechanicalClawFrame, GrappleSurface2D> observers = ClawFrameChanged;
        if (observers == null) return;
        foreach (Delegate observer in observers.GetInvocationList())
        {
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

    private void ApplyMotor(MechanicalClawFrame frame)
    {
        if (frame.State == MechanicalClawState.Released)
        {
            _motor.ApplyGrappleRelease(frame.VelocityCorrection);
            return;
        }
        float movementScale = frame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or
            MechanicalClawState.Travel ? CastingMovementScale : 1f;
        Vector2 correction = frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
            MechanicalClawState.Swing or MechanicalClawState.Boost
            ? frame.VelocityCorrection
            : Vector2.Zero;
        _motor.ApplyGrappleConstraint(correction, movementScale);
    }

    private void OnAttackModeRequested() => _retractRequested = true;
    private void OnDamaged(int remaining, Vector2 impulse) => ResetTransientState();
    private void OnDied() => ResetTransientState();

    private void ResolveDependencies()
    {
        _player ??= GetNode<CharacterBody2D>(PlayerPath);
        _input ??= GetNode<PlayerInputReader>(InputPath);
        _motor ??= GetNode<PlayerMotor>(MotorPath);
        _actionModes ??= GetNode<PlayerActionModeController>(ActionModesPath);
        _shield ??= GetNodeOrNull<PlayerShieldController>(ShieldPath);
        _health ??= GetNode<HealthComponent>(HealthPath);
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

    private static bool IsNodeInside(Node node, Node possibleAncestor)
    {
        if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(possibleAncestor)) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (ReferenceEquals(current, possibleAncestor)) return true;
        return false;
    }

    private readonly record struct FramePublication(MechanicalClawFrame Frame,
        GrappleSurface2D Target);
}
