using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Enemies;

/// <summary>Scene adapter that binds the squire model to Godot physics and existing combat contracts.</summary>
public partial class RustShellSquire : CharacterBody2D, IThreadPullReceiver, IEncounterActor
{
    [Export] public NodePath HealthPath { get; set; } = new NodePath("Health");
    [Export] public NodePath HurtboxPath { get; set; } = new NodePath("Hurtbox");
    [Export] public NodePath AttackHitboxPath { get; set; } = new NodePath("AttackHitbox");
    [Export] public NodePath ThreadTargetPath { get; set; } = new NodePath("ThreadTarget");
    [Export] public float Gravity { get; set; } = 980f;

    public event Action<RustShellFrame, int> FrameChanged;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;
    public event Action Defeated;

    private readonly RustShellSettings _settings = new();
    private RustShellModel _model = new();
    private HealthComponent _health = null!;
    private Hurtbox _hurtbox = null!;
    private Hitbox _attackHitbox = null!;
    private CollisionShape2D _attackCollider = null!;
    private AttackTelegraphVisual _attackTelegraph;
    private ThreadTarget2D _threadTarget = null!;
    private bool _stunRequested;
    private Vector2 _threadPullVelocity;
    private Vector2 _damageImpulse;
    private int _facing = 1;
    private AttackTimelineFrame _lastAttackTimeline;
    private bool _presentationTargetLocked;
    private Vector2 _presentationLockedTarget;
    private Vector2 _checkpointPosition;

    public RustShellState CurrentState { get; private set; } = RustShellState.Idle;
    public RustShellFrame CurrentFrame { get; private set; } = new(RustShellState.Idle, 0f, false, false);
    public int Facing => _facing;

    public override void _Ready()
    {
        _health = GetNode<HealthComponent>(HealthPath);
        _hurtbox = GetNode<Hurtbox>(HurtboxPath);
        _attackHitbox = GetNode<Hitbox>(AttackHitboxPath);
        _attackCollider = _attackHitbox.GetNode<CollisionShape2D>("AttackCollider");
        _attackTelegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _threadTarget = GetNode<ThreadTarget2D>(ThreadTargetPath);
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        _checkpointPosition = GlobalPosition;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        bool seesPlayer = player is not null && HasLineOfSightTo(player);
        float signedDistance = player is null ? float.PositiveInfinity : player.GlobalPosition.X - GlobalPosition.X;
        RustShellFrame frame = _model.Step(signedDistance, seesPlayer, _stunRequested, _health.CurrentHealth <= 0, delta);
        CurrentFrame = frame;
        CurrentState = frame.State;
        _stunRequested = false;

        if (frame.MoveDirection != 0f) _facing = Mathf.RoundToInt(frame.MoveDirection);
        _attackHitbox.Position = new Vector2(_facing * 34f, -7f);
        AttackTimelineFrame timeline = EnrichTimeline(frame.AttackTimeline, player);
        if (timeline.Phase == AttackPhase.Active && _lastAttackTimeline.Phase != AttackPhase.Active)
            _attackHitbox.Begin(timeline.AttackId);
        else if (!frame.AttackActive)
            _attackHitbox.End();
        PublishAttack(timeline);
        float pullX = frame.State == RustShellState.Stunned ? _threadPullVelocity.X : 0f;
        float horizontalVelocity = frame.MoveDirection * _settings.MoveSpeed + pullX + _damageImpulse.X;
        _damageImpulse = _damageImpulse.Lerp(Vector2.Zero, 1f - Mathf.Exp(-12f * (float)delta));
        _threadPullVelocity = _threadPullVelocity.Lerp(Vector2.Zero, 1f - Mathf.Exp(-10f * (float)delta));
        Velocity = new Vector2(horizontalVelocity, Velocity.Y + Gravity * (float)delta + _damageImpulse.Y);
        if (frame.State == RustShellState.Dead) Velocity = Vector2.Zero;
        MoveAndSlide();
        FrameChanged?.Invoke(frame, _facing);
    }

    public void ApplyThreadPull(Vector2 velocity)
    {
        _threadPullVelocity = velocity;
        Velocity = velocity;
        _stunRequested = true;
    }

    public void ResetForCheckpoint()
    {
        GlobalPosition = _checkpointPosition;
        Velocity = Vector2.Zero;
        _threadPullVelocity = Vector2.Zero;
        _damageImpulse = Vector2.Zero;
        _stunRequested = false;
        _model = new RustShellModel();
        CurrentState = RustShellState.Idle;
        CurrentFrame = new RustShellFrame(RustShellState.Idle, 0f, false, false);
        _attackHitbox?.End();
        _presentationTargetLocked = false;
        PublishAttack(default);
        _health?.ResetFull();
    }

    public bool HasLineOfSightTo(Node2D player)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(GlobalPosition, player.GlobalPosition);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return true;
        Node collider = hit["collider"].AsGodotObject() as Node;
        return collider != null && NodesBelongToSameTarget(collider, player);
    }

    public static bool NodesBelongToSameTarget(Node collider, Node target)
    {
        return IsSelfOrDescendant(collider, target) || IsSelfOrDescendant(target, collider);
    }

    private static bool IsSelfOrDescendant(Node node, Node possibleAncestor)
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (current == possibleAncestor) return true;
        return false;
    }

    private void OnDamaged(int remaining, Vector2 impulse)
    {
        _damageImpulse = impulse;
        PresentationCueRequested?.Invoke(PresentationCue.Hurt);
    }

    private void OnDied()
    {
        Velocity = Vector2.Zero;
        _attackHitbox?.End();
        PublishAttack(default);
        PresentationCueRequested?.Invoke(PresentationCue.Died);
        Defeated?.Invoke();
    }

    private AttackTimelineFrame EnrichTimeline(AttackTimelineFrame timeline, Node2D player)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            _presentationTargetLocked = false;
            return default;
        }
        if (timeline.AttackId != _lastAttackTimeline.AttackId)
            _presentationTargetLocked = false;
        bool lockNow = timeline.Phase == AttackPhase.Active ||
            timeline.Phase == AttackPhase.Telegraph &&
            timeline.PhaseRemainingSeconds <= timeline.PhaseDurationSeconds * .5f;
        if (!_presentationTargetLocked && lockNow)
        {
            _presentationLockedTarget = player?.GlobalPosition ??
                _attackCollider.GlobalPosition + Vector2.Right * _facing * 36f;
            _presentationTargetLocked = true;
        }
        return _presentationTargetLocked
            ? timeline with { LockedWorldTarget = _presentationLockedTarget, HasLockedTarget = true }
            : timeline;
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
        AttackPresentationFrame presentation = new AttackPresentationFrame(timeline,
            AttackShapeFactory.FromCollision(_attackCollider, _attackCollider.GlobalTransform,
                AttackShapeKind.Rectangle)).Validate();
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
