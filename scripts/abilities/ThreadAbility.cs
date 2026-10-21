using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Vfx;

namespace Knight.Abilities;

public partial class ThreadAbility : Node
{
    [Export]
    public NodePath PlayerPath { get; set; } = new("..");

    [Export]
    public NodePath InputPath { get; set; } = new("../Input");

    [Export]
    public NodePath MotorPath { get; set; } = new("../Motor");

    [Export]
    public NodePath LinePath { get; set; } = new("../ThreadLine");

    [Export]
    public NodePath ShuttlePath { get; set; } = new("../VisualRoot/ThreadShuttle");

    [Export]
    public NodePath HealthPath { get; set; } = new("../Health");

    [Export]
    public ThreadTuning Tuning { get; set; } = new();

    [Signal]
    public delegate void ThreadFiredEventHandler();

    [Signal]
    public delegate void ThreadHitEventHandler();

    [Signal]
    public delegate void ThreadFailedEventHandler();

    public event Action<ThreadCastFrame, ThreadTarget2D> CastFrameChanged;

    private ThreadAbilitySettings _settings = null!;
    private ThreadAbilityModel _abilityModel = null!;
    private ThreadCastModel _castModel = null!;
    private readonly Dictionary<ulong, ThreadTarget2D> _targetsById = new();
    private readonly List<ThreadCandidate> _candidateBuffer = new(16);
    private readonly List<ThreadTarget2D> _targetCandidates = new(16);
    private SceneTree _targetCandidateTree;
    private PlayerInputReader _input = null!;
    private CharacterBody2D _player = null!;
    private PlayerMotor _motor;
    private HealthComponent _health;
    private ThreadLineVisual _line;
    private ThreadShuttleVisual _shuttle;
    private WeakReference<ThreadTarget2D> _lockedTarget;
    private Vector2 _lastFacing = Vector2.Right;
    private Vector2 _lastTargetPoint;
    private ulong _consumedCastId;
    private ulong _appliedCastId;
    private double _elapsedSeconds;
    private bool _threadIntentActive;
    private bool _swallowThreadRelease;
    private bool _physicsDisableObserved;
    private bool _resetForPhysicsDisable;
    private bool _ready;

    public ThreadCastFrame CurrentCastFrame { get; private set; } = IdleFrame();
    public float ReadyRatio => _abilityModel?.ReadyRatio(_elapsedSeconds) ?? 1f;
    public bool HasLockedTarget => TryGetLockedTarget(out _);

    public override void _Ready()
    {
        Tuning ??= new ThreadTuning();
        IReadOnlyList<string> errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid ThreadTuning: " + string.Join("; ", errors));

        _settings = Tuning.ToSettings();
        _abilityModel = new ThreadAbilityModel(_settings);
        _castModel = new ThreadCastModel(_settings);
        _player = GetNode<CharacterBody2D>(PlayerPath);
        _input = GetNode<PlayerInputReader>(InputPath);
        _motor = GetNodeOrNull<PlayerMotor>(MotorPath);
        _line = GetNodeOrNull<ThreadLineVisual>(LinePath);
        _shuttle = GetNodeOrNull<ThreadShuttleVisual>(ShuttlePath);
        _health = GetNodeOrNull<HealthComponent>(HealthPath);
        if (_health != null)
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        _targetCandidateTree = GetTree();
        _targetCandidateTree.NodeAdded += OnTargetNodeAdded;
        _targetCandidateTree.NodeRemoved += OnTargetNodeRemoved;
        CollectTargetCandidates(_targetCandidateTree.Root);

        ProcessMode = ProcessModeEnum.Always;
        CurrentCastFrame = IdleFrame();
        _lastTargetPoint = _player.GlobalPosition;
        RestoreMovement();
        ClearPresentation();
        _ready = true;
    }

    public override void _Process(double delta)
    {
        if (!_ready) return;
        if (IsPhysicsProcessing())
        {
            if (_physicsDisableObserved && NeedsLockedTarget(CurrentCastFrame.State))
            {
                TargetResolution resumedResolution = ResolveLockedTarget(CurrentCastFrame);
                if (!resumedResolution.Snapshot.IsValid)
                {
                    ThreadCastFrame resumedPrevious = CurrentCastFrame;
                    ThreadCastFrame resumedFailed = _castModel.Step(
                        new ThreadCastInput(false, false, false, resumedPrevious.Aim, false),
                        0f,
                        resumedResolution.Snapshot,
                        _abilityModel.CanConsume(_elapsedSeconds));
                    HandleFrame(resumedPrevious, resumedFailed, resumedResolution);
                }
            }
            _physicsDisableObserved = false;
            _resetForPhysicsDisable = false;
            return;
        }

        if (CurrentCastFrame.State is ThreadCastState.Apply or ThreadCastState.Recovery)
        {
            if (_resetForPhysicsDisable) return;
            _resetForPhysicsDisable = true;
            _physicsDisableObserved = false;
            ClearForPhysicsDisablePreservingCooldown();
            return;
        }

        if (NeedsLockedTarget(CurrentCastFrame.State))
        {
            TargetResolution resolution = ResolveLockedTarget(CurrentCastFrame);
            if (!resolution.Snapshot.IsValid)
            {
                ThreadCastFrame previous = CurrentCastFrame;
                ThreadCastFrame failed = _castModel.Step(
                    new ThreadCastInput(false, false, false, previous.Aim, false),
                    0f,
                    resolution.Snapshot,
                    _abilityModel.CanConsume(_elapsedSeconds));
                HandleFrame(previous, failed, resolution);
                _physicsDisableObserved = true;
                return;
            }
            // A real room transition disables gameplay before the prior room is queued. Keep the
            // locked cast intact across that handoff so the queued/freed room target is reported
            // as TargetLost instead of silently resetting to Idle.
            _physicsDisableObserved = true;
            return;
        }

        if (_resetForPhysicsDisable) return;
        _resetForPhysicsDisable = true;
        ClearForPhysicsDisablePreservingCooldown();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_ready) return;
        _elapsedSeconds += delta;
        PlayerInputFrame input = _input.CurrentFrame;
        PlayerInputFrame normalizedInput = NormalizeThreadInput(input);
        UpdateFacing(input);

        ThreadCastFrame previous = CurrentCastFrame;
        ThreadAimMode selectionMode = SelectionMode(previous, normalizedInput);
        Vector2 modelAim = selectionMode == ThreadAimMode.Tap && input.Aim.IsZeroApprox()
            ? _lastFacing
            : input.Aim;
        TargetResolution resolution = NeedsLockedTarget(previous.State)
            ? ResolveLockedTarget(previous)
            : ShouldResolveCandidates(previous.State, normalizedInput)
                ? ResolveCandidate(selectionMode, modelAim)
                : TargetResolution.Invalid(ThreadFailureReason.NoTarget, _player.GlobalPosition);
        bool recoveryMovementIntent = previous.State == ThreadCastState.Recovery &&
            (!Mathf.IsZeroApprox(input.MoveAxis) || input.JumpPressed);
        bool cancelled = GetTree().Paused || (_motor?.IsDashing ?? false) || recoveryMovementIntent;
        var castInput = new ThreadCastInput(
            normalizedInput.ThreadPressed,
            normalizedInput.ThreadHeld,
            normalizedInput.ThreadReleased,
            modelAim,
            cancelled);
        ThreadCastFrame next = _castModel.Step(castInput, (float)delta, resolution.Snapshot,
            _abilityModel.CanConsume(_elapsedSeconds));
        if (previous.State != ThreadCastState.Apply && next.State == ThreadCastState.Apply)
        {
            TargetResolution applyResolution = ResolveLockedTarget(next);
            if (!applyResolution.Snapshot.IsValid)
            {
                resolution = applyResolution;
                next = _castModel.Step(
                    new ThreadCastInput(false, false, false, next.Aim, false),
                    0f,
                    applyResolution.Snapshot,
                    _abilityModel.CanConsume(_elapsedSeconds));
            }
        }
        HandleFrame(previous, next, resolution);
    }

    public override void _ExitTree()
    {
        _ready = false;
        if (_targetCandidateTree != null)
        {
            _targetCandidateTree.NodeAdded -= OnTargetNodeAdded;
            _targetCandidateTree.NodeRemoved -= OnTargetNodeRemoved;
            _targetCandidateTree = null;
        }
        _targetCandidates.Clear();
        _candidateBuffer.Clear();
        if (GodotObject.IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
        _lockedTarget = null;
        _threadIntentActive = false;
        _swallowThreadRelease = false;
        _targetsById.Clear();
        RestoreMovement();
        ClearPresentation();
    }

    public void ResetTransientState()
    {
        if (_settings == null) return;
        ThreadCastFrame previous = CurrentCastFrame;
        _abilityModel = new ThreadAbilityModel(_settings);
        _castModel = new ThreadCastModel(_settings);
        _elapsedSeconds = 0d;
        _consumedCastId = 0;
        _appliedCastId = 0;
        _threadIntentActive = false;
        _swallowThreadRelease = false;
        _lockedTarget = null;
        _targetsById.Clear();
        CurrentCastFrame = IdleFrame();
        RestoreMovement();
        ClearPresentation();
        if (previous != CurrentCastFrame) CastFrameChanged?.Invoke(CurrentCastFrame, null);
    }

    private void ClearForPhysicsDisablePreservingCooldown()
    {
        ThreadCastFrame previous = CurrentCastFrame;
        if (previous.State != ThreadCastState.Idle)
        {
            _castModel.Step(
                new ThreadCastInput(false, false, false, previous.Aim, true),
                0f,
                default,
                _abilityModel.CanConsume(_elapsedSeconds));
            CurrentCastFrame = _castModel.Step(
                new ThreadCastInput(false, false, false, previous.Aim, false),
                _settings.VisualSeconds,
                default,
                _abilityModel.CanConsume(_elapsedSeconds));
        }

        _consumedCastId = 0;
        _appliedCastId = 0;
        _threadIntentActive = false;
        _swallowThreadRelease = false;
        _lockedTarget = null;
        _targetsById.Clear();
        RestoreMovement();
        ClearPresentation();
        if (previous != CurrentCastFrame) CastFrameChanged?.Invoke(CurrentCastFrame, null);
    }

    private void HandleFrame(ThreadCastFrame previous, ThreadCastFrame next, TargetResolution resolution)
    {
        bool enteringWindup = previous.State != ThreadCastState.Windup && next.State == ThreadCastState.Windup;
        bool enteringTravel = previous.State != ThreadCastState.Travel && next.State == ThreadCastState.Travel;
        bool enteringApply = previous.State != ThreadCastState.Apply && next.State == ThreadCastState.Apply;
        bool enteringFailed = previous.State != ThreadCastState.Failed && next.State == ThreadCastState.Failed;
        bool enteringIdle = previous.State != ThreadCastState.Idle && next.State == ThreadCastState.Idle;

        if (enteringWindup)
        {
            LockTarget(resolution.Target);
            EmitSignal(SignalName.ThreadFired);
        }

        if (enteringTravel && next.CastId != 0 && next.CastId != _consumedCastId)
        {
            if (_abilityModel.Consume(next.CastId, _elapsedSeconds)) _consumedCastId = next.CastId;
            else GD.PushError($"Thread cast {next.CastId} entered Travel without available cooldown.");
        }

        ThreadTarget2D publishedTarget = resolution.Target;
        if (enteringApply && next.CastId != 0 && next.CastId != _appliedCastId)
        {
            ThreadTarget2D target = resolution.Target;
            if (target != null && GodotObject.IsInstanceValid(target) &&
                target.TryGetEligibleReceiver(_player, out _))
            {
                _appliedCastId = next.CastId;
                target.ApplyPull(_player, _player.GlobalPosition, _settings);
                publishedTarget = target;
                EmitSignal(SignalName.ThreadHit);
            }
        }

        if (enteringFailed)
        {
            _lockedTarget = null;
            _targetsById.Clear();
            publishedTarget = null;
            EmitSignal(SignalName.ThreadFailed);
        }
        else if (enteringIdle)
        {
            _lockedTarget = null;
            _targetsById.Clear();
            publishedTarget = null;
        }

        CurrentCastFrame = next;
        if (next.State == ThreadCastState.Preview) SetMovementScale(.65f);
        else RestoreMovement();
        Present(next, resolution);
        if (next != previous) CastFrameChanged?.Invoke(next, ValidPublishedTarget(publishedTarget));
    }

    private void CancelImmediately()
    {
        if (!_ready)
        {
            ClearPresentation();
            RestoreMovement();
            return;
        }

        ThreadCastFrame previous = CurrentCastFrame;
        if (previous.State == ThreadCastState.Idle)
        {
            _castModel = new ThreadCastModel(_settings);
            _threadIntentActive = false;
            _swallowThreadRelease = false;
            ClearPresentation();
            RestoreMovement();
            return;
        }

        TargetResolution resolution = NeedsLockedTarget(previous.State)
            ? ResolveLockedTarget(previous)
            : TargetResolution.Invalid(ThreadFailureReason.Cancelled, _lastTargetPoint);
        ThreadCastFrame cancelled = _castModel.Step(
            new ThreadCastInput(false, false, false, previous.Aim, true),
            0f,
            resolution.Snapshot,
            _abilityModel.CanConsume(_elapsedSeconds));
        HandleFrame(previous, cancelled, resolution);
        _lockedTarget = null;
        _threadIntentActive = false;
        _swallowThreadRelease = false;
        RestoreMovement();
        ClearPresentation();
    }

    private PlayerInputFrame NormalizeThreadInput(PlayerInputFrame input)
    {
        bool held = input.ThreadHeld;
        bool released = input.ThreadReleased;
        if (input.ThreadPressed)
        {
            _threadIntentActive = true;
            _swallowThreadRelease = false;
            // Input backends may expose the press edge one sample before IsActionPressed. Bridge
            // only that edge so a real press is not mistaken for an immediate Tap release.
            if (!released) held = true;
        }

        if (_threadIntentActive && !held)
        {
            // A physical Held drop is the authoritative end of this input interval. Some input
            // backends deliver the matching Released edge one physics sample later; do not let
            // that gap add hold duration or create a second release.
            released = true;
            _threadIntentActive = false;
            _swallowThreadRelease = !input.ThreadReleased;
        }
        else if (input.ThreadReleased)
        {
            if (_swallowThreadRelease)
            {
                released = false;
                _swallowThreadRelease = false;
            }
            _threadIntentActive = false;
        }

        return input with { ThreadHeld = held, ThreadReleased = released };
    }

    private TargetResolution ResolveCandidate(ThreadAimMode mode, Vector2 aim)
    {
        _targetsById.Clear();
        _candidateBuffer.Clear();
        Vector2 origin = _player.GlobalPosition;
        float range = mode == ThreadAimMode.Tap ? _settings.TapAssistRange : _settings.HoldRange;
        float minimumDot = Mathf.Cos(Mathf.DegToRad(mode == ThreadAimMode.Tap
            ? _settings.TapConeHalfAngleDegrees
            : _settings.HoldConeHalfAngleDegrees));
        bool hasObstruction = false;
        bool hasOutOfRange = false;
        Vector2 obstructionPoint = origin;
        Vector2 outOfRangePoint = origin;
        float obstructionDistance = float.PositiveInfinity;
        float outOfRangeDistance = float.PositiveInfinity;

        for (int index = 0; index < _targetCandidates.Count; index++)
        {
            ThreadTarget2D target = _targetCandidates[index];
            if (!GodotObject.IsInstanceValid(target) ||
                !target.TryGetEligibleReceiver(_player, out _)) continue;

            ulong id = target.GetInstanceId();
            LineOfSightResult sight = TraceLineOfSight(origin, target);
            _targetsById[id] = target;
            _candidateBuffer.Add(new ThreadCandidate(id, target.GlobalPosition, target.Kind,
                sight.Visible, target.FallbackAimAssistRange));

            if (aim.IsZeroApprox()) continue;
            Vector2 offset = target.GlobalPosition - origin;
            float distance = offset.Length();
            if (distance <= .001f || aim.Normalized().Dot(offset / distance) < minimumDot) continue;
            if (distance > range)
            {
                if (distance < outOfRangeDistance)
                {
                    outOfRangeDistance = distance;
                    outOfRangePoint = origin + offset / distance * range;
                    hasOutOfRange = true;
                }
                continue;
            }
            if (!sight.Visible && distance < obstructionDistance)
            {
                obstructionDistance = distance;
                obstructionPoint = sight.HitPoint;
                hasObstruction = true;
            }
        }

        ThreadCandidate? selected = _abilityModel.SelectTarget(origin, aim, mode, _candidateBuffer);
        if (selected.HasValue && _targetsById.TryGetValue(selected.Value.Id, out ThreadTarget2D resolved) &&
            GodotObject.IsInstanceValid(resolved) && resolved.TryGetEligibleReceiver(_player, out _))
        {
            _lastTargetPoint = resolved.GlobalPosition;
            return TargetResolution.Valid(resolved,
                resolved.GlobalPosition - _player.GlobalPosition);
        }

        _targetsById.Clear();
        if (hasObstruction)
            return TargetResolution.Invalid(ThreadFailureReason.Obstructed, obstructionPoint, true);
        if (hasOutOfRange)
            return TargetResolution.Invalid(ThreadFailureReason.OutOfRange, outOfRangePoint, true);
        return TargetResolution.Invalid(ThreadFailureReason.NoTarget, origin);
    }

    private void OnTargetNodeAdded(Node node)
    {
        if (node is ThreadTarget2D target && !_targetCandidates.Contains(target))
            _targetCandidates.Add(target);
    }

    private void OnTargetNodeRemoved(Node node)
    {
        if (node is ThreadTarget2D target) _targetCandidates.Remove(target);
    }

    private void CollectTargetCandidates(Node node)
    {
        OnTargetNodeAdded(node);
        int childCount = node.GetChildCount();
        for (int index = 0; index < childCount; index++)
            CollectTargetCandidates(node.GetChild(index));
    }

    private TargetResolution ResolveLockedTarget(ThreadCastFrame frame)
    {
        if ((frame.State is ThreadCastState.Apply or ThreadCastState.Recovery) &&
            frame.CastId != 0 && frame.CastId == _appliedCastId)
        {
            return new TargetResolution(null,
                new ThreadTargetSnapshot(frame.TargetId, _lastTargetPoint - _player.GlobalPosition,
                    true, ThreadFailureReason.None),
                _lastTargetPoint,
                true);
        }

        if (TryGetLockedTarget(out ThreadTarget2D target))
        {
            _lastTargetPoint = target.GlobalPosition;
            float range = frame.AimMode == ThreadAimMode.Tap ? _settings.TapAssistRange : _settings.HoldRange;
            Vector2 offset = target.GlobalPosition - _player.GlobalPosition;
            float distance = offset.Length();
            if (distance > range)
            {
                Vector2 point = distance <= .001f
                    ? _player.GlobalPosition
                    : _player.GlobalPosition + offset / distance * range;
                return TargetResolution.Invalid(ThreadFailureReason.OutOfRange, point, true);
            }

            LineOfSightResult sight = TraceLineOfSight(_player.GlobalPosition, target);
            if (!sight.Visible)
                return TargetResolution.Invalid(ThreadFailureReason.Obstructed, sight.HitPoint, true);
            return TargetResolution.Valid(target, target.GlobalPosition - _player.GlobalPosition);
        }

        return new TargetResolution(null,
            new ThreadTargetSnapshot(frame.TargetId, _lastTargetPoint - _player.GlobalPosition,
                false, ThreadFailureReason.TargetLost),
            _lastTargetPoint,
            true);
    }

    private LineOfSightResult TraceLineOfSight(Vector2 origin, ThreadTarget2D target)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(origin, target.GlobalPosition);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
            CombatCollisionLayers.ThreadTarget;
        query.Exclude = CollisionRidsInSubtree(_player);
        Godot.Collections.Dictionary result = _player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0) return new LineOfSightResult(true, target.GlobalPosition);

        Node collider = result["collider"].AsGodotObject() as Node;
        bool belongs = collider != null && BelongsToTarget(collider, target);
        Vector2 point = result.ContainsKey("position") ? result["position"].AsVector2() : target.GlobalPosition;
        return new LineOfSightResult(belongs, point);
    }

    private void Present(ThreadCastFrame frame, TargetResolution resolution)
    {
        Vector2 origin = _player.GlobalPosition;
        Vector2 targetPoint = resolution.HasPresentationPoint ? resolution.PresentationPoint : _lastTargetPoint;
        if (frame.HasTarget && resolution.Target != null && GodotObject.IsInstanceValid(resolution.Target))
        {
            targetPoint = resolution.Target.GlobalPosition;
            _lastTargetPoint = targetPoint;
        }

        bool hasFailurePoint = frame.Failure is ThreadFailureReason.Obstructed or
            ThreadFailureReason.OutOfRange or ThreadFailureReason.TargetLost;
        if (frame.State == ThreadCastState.Failed && hasFailurePoint && resolution.HasPresentationPoint)
        {
            _lastTargetPoint = resolution.PresentationPoint;
            targetPoint = _lastTargetPoint;
        }
        if (GodotObject.IsInstanceValid(_shuttle))
        {
            if (frame.State == ThreadCastState.Failed && frame.Failure == ThreadFailureReason.Cancelled)
                _shuttle.Clear();
            else
                _shuttle.Present(frame, origin,
                    frame.State == ThreadCastState.Failed && !hasFailurePoint ? origin : targetPoint);
        }
        if (!GodotObject.IsInstanceValid(_line)) return;

        switch (frame.State)
        {
            case ThreadCastState.Preview:
                if (frame.HasTarget) _line.Present(origin, targetPoint, .24f, ThreadLineState.Preview);
                else _line.Clear();
                break;
            case ThreadCastState.Windup:
                _line.Present(origin, targetPoint, .36f, ThreadLineState.Fired);
                break;
            case ThreadCastState.Travel:
                _line.Present(origin, ShuttlePoint(frame, origin, targetPoint),
                    Mathf.Lerp(.42f, .72f, PhaseProgress(frame)), ThreadLineState.Travel);
                break;
            case ThreadCastState.Latched:
                _line.Present(origin, targetPoint, .78f, ThreadLineState.Latched);
                break;
            case ThreadCastState.Apply:
                _line.Present(origin, targetPoint, 1f, ThreadLineState.Connected);
                break;
            case ThreadCastState.Recovery:
                _line.Present(origin, ShuttlePoint(frame, origin, targetPoint),
                    Mathf.Lerp(1f, .45f, PhaseProgress(frame)), ThreadLineState.Recovery);
                break;
            case ThreadCastState.Failed:
                if (hasFailurePoint)
                    _line.Present(origin, targetPoint, .12f, ThreadLineState.Failed);
                else
                    _line.Clear();
                break;
            default:
                _line.Clear();
                if (GodotObject.IsInstanceValid(_shuttle)) _shuttle.Clear();
                break;
        }
    }

    private Vector2 ShuttlePoint(ThreadCastFrame frame, Vector2 origin, Vector2 target)
    {
        if (GodotObject.IsInstanceValid(_shuttle)) return _shuttle.ShuttleWorldPosition;
        float progress = PhaseProgress(frame);
        return frame.State == ThreadCastState.Recovery
            ? target.Lerp(origin, progress)
            : origin.Lerp(target, progress);
    }

    private void UpdateFacing(PlayerInputFrame input)
    {
        if (!input.Aim.IsZeroApprox())
        {
            _lastFacing = input.Aim.Normalized();
            return;
        }
        if (!Mathf.IsZeroApprox(input.MoveAxis)) _lastFacing = new Vector2(Mathf.Sign(input.MoveAxis), 0f);
    }

    private void LockTarget(ThreadTarget2D target)
    {
        _targetsById.Clear();
        if (target == null || !GodotObject.IsInstanceValid(target) || !target.IsAvailableForThread)
        {
            _lockedTarget = null;
            return;
        }
        _lastTargetPoint = target.GlobalPosition;
        _lockedTarget = new WeakReference<ThreadTarget2D>(target);
    }

    private bool TryGetLockedTarget(out ThreadTarget2D target)
    {
        target = null;
        if (_lockedTarget == null || !_lockedTarget.TryGetTarget(out ThreadTarget2D candidate) ||
            candidate == null || !GodotObject.IsInstanceValid(candidate) ||
            !candidate.TryGetEligibleReceiver(_player, out _))
        {
            _lockedTarget = null;
            return false;
        }
        target = candidate;
        return true;
    }

    private void SetMovementScale(float scale)
    {
        if (GodotObject.IsInstanceValid(_motor)) _motor.AbilityMovementScale = scale;
    }

    private void RestoreMovement() => SetMovementScale(1f);

    private void ClearPresentation()
    {
        if (GodotObject.IsInstanceValid(_line)) _line.Clear();
        if (GodotObject.IsInstanceValid(_shuttle)) _shuttle.Clear();
    }

    private void OnDamaged(int remaining, Vector2 impulse) => CancelImmediately();
    private void OnDied() => CancelImmediately();

    private static bool NeedsLockedTarget(ThreadCastState state) => state is
        ThreadCastState.Windup or ThreadCastState.Travel or ThreadCastState.Latched or
        ThreadCastState.Apply or ThreadCastState.Recovery;

    private static bool ShouldResolveCandidates(ThreadCastState state, PlayerInputFrame input) =>
        state == ThreadCastState.Preview || input.ThreadPressed || input.ThreadHeld || input.ThreadReleased;

    private static ThreadAimMode SelectionMode(ThreadCastFrame frame, PlayerInputFrame input)
    {
        if (frame.State == ThreadCastState.Preview) return ThreadAimMode.Hold;
        if (input.ThreadReleased || (input.ThreadPressed && !input.ThreadHeld)) return ThreadAimMode.Tap;
        return input.ThreadHeld ? ThreadAimMode.Hold : frame.AimMode;
    }

    private static ThreadTarget2D ValidPublishedTarget(ThreadTarget2D target) =>
        target != null && GodotObject.IsInstanceValid(target) && target.IsAvailableForThread ? target : null;

    private static float PhaseProgress(ThreadCastFrame frame)
    {
        if (frame.StateDurationSeconds <= .000001f) return 1f;
        return Mathf.Clamp(1f - frame.StateRemainingSeconds / frame.StateDurationSeconds, 0f, 1f);
    }

    private static ThreadCastFrame IdleFrame() => new(0, ThreadCastState.Idle, ThreadAimMode.Tap,
        Vector2.Right, 0, false, ThreadFailureReason.None, 0f, 0f);

    private static bool BelongsToTarget(Node collider, ThreadTarget2D target)
    {
        if (IsSelfOrDescendant(collider, target) || IsSelfOrDescendant(target, collider)) return true;
        Node receiver = target.GetNodeOrNull(target.PullReceiverPath);
        return receiver != null &&
            (IsSelfOrDescendant(collider, receiver) || IsSelfOrDescendant(receiver, collider));
    }

    private static bool IsSelfOrDescendant(Node node, Node possibleAncestor)
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (current == possibleAncestor) return true;
        return false;
    }

    private static Godot.Collections.Array<Rid> CollisionRidsInSubtree(Node root)
    {
        var rids = new Godot.Collections.Array<Rid>();
        AddCollisionRids(root, rids);
        return rids;
    }

    private static void AddCollisionRids(Node node, Godot.Collections.Array<Rid> rids)
    {
        if (node is CollisionObject2D collisionObject) rids.Add(collisionObject.GetRid());
        foreach (Node child in node.GetChildren()) AddCollisionRids(child, rids);
    }

    private readonly record struct LineOfSightResult(bool Visible, Vector2 HitPoint);

    private readonly record struct TargetResolution(
        ThreadTarget2D Target,
        ThreadTargetSnapshot Snapshot,
        Vector2 PresentationPoint,
        bool HasPresentationPoint)
    {
        public static TargetResolution Valid(ThreadTarget2D target, Vector2 relativePosition) => new(
            target,
            new ThreadTargetSnapshot(target.GetInstanceId(), relativePosition, true, ThreadFailureReason.None),
            target.GlobalPosition,
            true);

        public static TargetResolution Invalid(ThreadFailureReason reason, Vector2 point,
            bool hasPresentationPoint = false) => new(
            null,
            new ThreadTargetSnapshot(0, Vector2.Zero, false, reason),
            point,
            hasPresentationPoint);
    }
}
