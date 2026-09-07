using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Mechanisms;
using Knight.Presentation;

namespace Knight.Boss;

public partial class FallenBellkeeper : CharacterBody2D, IThreadInteractionReceiver
{
    [Signal] public delegate void DefeatedEventHandler();

    [Export] public NodePath HealthPath { get; set; } = new("Health");
    [Export] public NodePath ArmorPath { get; set; } = new("Armor");
    [Export] public NodePath HurtboxPath { get; set; } = new("Hurtbox");
    [Export] public NodePath AttackHitboxPath { get; set; } = new("AttackHitbox");
    [Export] public NodePath ChainTargetPath { get; set; } = new("ChainTarget");
    [Export] public BossTuning Tuning { get; set; } = new();
    [Export] public float Gravity { get; set; } = 980f;

    public event Action<FallenBellkeeperFrame, int> FrameChanged;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;

    public FallenBellkeeperFrame CurrentFrame { get; private set; } = new(
        FallenBellkeeperState.Dormant, FallenBellkeeperAttack.None, false, false, false, false);
    public int Facing { get; private set; } = -1;

    private FallenBellkeeperModel _model;
    private BossSettings _settings;
    private HealthComponent _health;
    private ArmoredDamageReceiver _armor;
    private Hurtbox _hurtbox;
    private Hitbox _attackHitbox;
    private CollisionShape2D _attackCollider;
    private AttackTelegraphVisual _attackTelegraph;
    private ThreadTarget2D _chainTarget;
    private CollisionShape2D _chainCollision;
    private bool _arenaActive;
    private bool _threadRequested;
    private bool _attackWasActive;
    private AttackTimelineFrame _lastAttackTimeline;
    private bool _attackTargetLocked;
    private Vector2 _lockedAttackTarget;
    private int _lockedAttackFacing = -1;

    public override void _Ready()
    {
        Tuning ??= new BossTuning();
        IReadOnlyList<string> errors = Tuning.Validate();
        if (errors.Count > 0) throw new InvalidOperationException("Invalid BossTuning: " + string.Join("; ", errors));
        _settings = Tuning.ToSettings();
        _model = new FallenBellkeeperModel(_settings);
        _health = GetNode<HealthComponent>(HealthPath);
        _armor = GetNode<ArmoredDamageReceiver>(ArmorPath);
        _hurtbox = GetNode<Hurtbox>(HurtboxPath);
        _attackHitbox = GetNode<Hitbox>(AttackHitboxPath);
        _attackCollider = _attackHitbox.GetNode<CollisionShape2D>("AttackCollider");
        _attackTelegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _chainTarget = GetNode<ThreadTarget2D>(ChainTargetPath);
        _chainCollision = _chainTarget.GetNode<CollisionShape2D>("TargetCollision/Collision");
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        _armor.Guarded += OnGuarded;
        ApplyFrame(CurrentFrame, default, GlobalPosition);
    }

    public override void _ExitTree()
    {
        if (!IsInstanceValid(_health)) return;
        _health.Damaged -= OnDamaged;
        _health.Died -= OnDied;
        if (IsInstanceValid(_armor)) _armor.Guarded -= OnGuarded;
    }

    public override void _PhysicsProcess(double delta)
    {
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if (player != null && Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X) > 1f)
            Facing = player.GlobalPosition.X < GlobalPosition.X ? -1 : 1;
        float healthFraction = _health.MaxHealth <= 0 ? 0f : (float)_health.CurrentHealth / _health.MaxHealth;
        Vector2 stepTarget = _attackTargetLocked
            ? _lockedAttackTarget
            : LiveTarget(player, CurrentFrame.AttackTimeline.Kind);
        CurrentFrame = _model.Step(new FallenBellkeeperInput(_arenaActive, healthFraction, _threadRequested),
            delta, stepTarget);
        _threadRequested = false;
        PrepareAttackIdentity(CurrentFrame.AttackTimeline);
        Vector2 frameTarget = _attackTargetLocked
            ? _lockedAttackTarget
            : LiveTarget(player, CurrentFrame.AttackTimeline.Kind);
        AttackTimelineFrame timeline = EnrichTimeline(CurrentFrame.AttackTimeline, frameTarget);
        ApplyFrame(CurrentFrame, timeline, frameTarget);
        Velocity = CurrentFrame.State == FallenBellkeeperState.Defeated
            ? Vector2.Zero
            : new Vector2(0f, Velocity.Y + Gravity * (float)delta);
        MoveAndSlide();
        FrameChanged?.Invoke(CurrentFrame, Facing);
    }

    public void ActivateEncounter() => _arenaActive = true;

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if (!CurrentFrame.ChainTargetActive || _health.CurrentHealth <= 0) return false;
        _threadRequested = true;
        return true;
    }

    private void ApplyFrame(FallenBellkeeperFrame frame, AttackTimelineFrame timeline, Vector2 previewTarget)
    {
        bool useArmor = frame.ArmorClosed;
        NodePath receiver = useArmor ? new NodePath("../Armor") : new NodePath("../Health");
        if (_hurtbox.DamageReceiverPath != receiver) _hurtbox.SetDamageReceiverPath(receiver);

        if (frame.ChainTargetActive) _chainTarget.AddToGroup("thread_targets");
        else _chainTarget.RemoveFromGroup("thread_targets");
        _chainCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, !frame.ChainTargetActive);

        if (timeline.Phase != AttackPhase.None)
            ConfigureAttackGeometry(timeline, previewTarget);

        if (frame.AttackActive && !_attackWasActive)
        {
            _attackHitbox.Begin(timeline.AttackId);
        }
        else if (!frame.AttackActive) _attackHitbox.End();
        _attackWasActive = frame.AttackActive;
        PublishAttack(timeline);

        if (frame.EnteredState && frame.State == FallenBellkeeperState.Transition)
            PresentationCueRequested?.Invoke(PresentationCue.PhaseChanged);
        if (frame.EnteredState && frame.State == FallenBellkeeperState.Exposed)
            PresentationCueRequested?.Invoke(PresentationCue.WeakpointOpened);
    }

    private void OnDamaged(int remaining, Vector2 impulse) => PresentationCueRequested?.Invoke(PresentationCue.Hurt);

    private void OnGuarded() => PresentationCueRequested?.Invoke(PresentationCue.Guard);

    private void OnDied()
    {
        _attackHitbox.End();
        PublishAttack(default);
        _chainTarget.RemoveFromGroup("thread_targets");
        _chainCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        PresentationCueRequested?.Invoke(PresentationCue.Died);
        EmitSignal(SignalName.Defeated);
    }

    private AttackTimelineFrame EnrichTimeline(AttackTimelineFrame timeline, Vector2 targetWorldPosition)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            _attackTargetLocked = false;
            return default;
        }
        bool lockNow = timeline.Phase == AttackPhase.Active ||
            timeline.Phase == AttackPhase.Telegraph &&
            timeline.PhaseRemainingSeconds <= timeline.PhaseDurationSeconds * .5f;
        if (!_attackTargetLocked && lockNow)
        {
            _lockedAttackTarget = targetWorldPosition;
            _attackTargetLocked = true;
        }
        return _attackTargetLocked
            ? timeline with { LockedWorldTarget = _lockedAttackTarget, HasLockedTarget = true }
            : timeline;
    }

    private void PrepareAttackIdentity(AttackTimelineFrame timeline)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            _attackTargetLocked = false;
            return;
        }
        if (timeline.AttackId == _lastAttackTimeline.AttackId) return;
        _attackTargetLocked = false;
        _lockedAttackFacing = Facing;
    }

    private Vector2 LiveTarget(Node2D player, EnemyAttackKind kind)
    {
        Vector2 target = player?.GlobalPosition ?? GlobalPosition + Vector2.Right * Facing * 120f;
        return kind == EnemyAttackKind.BossHammerThrow ? target + Vector2.Up * 64f : target;
    }

    private void ConfigureAttackGeometry(AttackTimelineFrame timeline, Vector2 previewTarget)
    {
        Vector2 target = timeline.HasLockedTarget ? timeline.LockedWorldTarget : previewTarget;
        int attackFacing = _lockedAttackFacing < 0 ? -1 : 1;
        _attackHitbox.GuardClass = timeline.Kind switch
        {
            EnemyAttackKind.BossSweep or EnemyAttackKind.BossLeapSlam or EnemyAttackKind.BossHammerThrow =>
                DamageGuardClass.Heavy,
            EnemyAttackKind.BossGroundWave => DamageGuardClass.Unblockable,
            _ => DamageGuardClass.Normal
        };
        switch (timeline.Kind)
        {
            case EnemyAttackKind.BossSweep:
                _attackCollider.Shape = new ConvexPolygonShape2D
                {
                    Points =
                    [
                        new Vector2(0f, -12f), new Vector2(attackFacing * 105f, -54f),
                        new Vector2(attackFacing * 148f, 0f), new Vector2(attackFacing * 105f, 54f),
                        new Vector2(0f, 12f)
                    ]
                };
                _attackHitbox.Position = new Vector2(attackFacing * 20f, -18f);
                _attackHitbox.Rotation = 0f;
                break;
            case EnemyAttackKind.BossLeapSlam:
                _attackCollider.Shape = new CircleShape2D { Radius = 62f };
                _attackHitbox.GlobalPosition = target;
                _attackHitbox.GlobalRotation = 0f;
                break;
            case EnemyAttackKind.BossGroundWave:
                _attackCollider.Shape = new RectangleShape2D { Size = new Vector2(250f, 34f) };
                _attackHitbox.GlobalPosition = GlobalPosition + new Vector2(attackFacing * 125f, 38f);
                _attackHitbox.GlobalRotation = 0f;
                break;
            case EnemyAttackKind.BossHammerThrow:
                Vector2 start = GlobalPosition + new Vector2(attackFacing * 36f, -42f);
                Vector2 delta = target - start;
                if (delta.LengthSquared() < 1f) delta = Vector2.Right * attackFacing * 80f;
                _attackCollider.Shape = new CapsuleShape2D
                {
                    Radius = 18f,
                    Height = Mathf.Max(36f, delta.Length() + 36f)
                };
                _attackHitbox.GlobalPosition = start + delta * .5f;
                _attackHitbox.GlobalRotation = delta.Angle() - Mathf.Pi * .5f;
                break;
        }
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
        AttackShapeKind requested = timeline.Kind switch
        {
            EnemyAttackKind.BossSweep => AttackShapeKind.ConvexPolygon,
            EnemyAttackKind.BossLeapSlam => AttackShapeKind.Circle,
            EnemyAttackKind.BossGroundWave => AttackShapeKind.GroundBand,
            EnemyAttackKind.BossHammerThrow => AttackShapeKind.Capsule,
            _ => AttackShapeKind.None
        };
        AttackPresentationFrame presentation = new AttackPresentationFrame(timeline,
            AttackShapeFactory.FromCollision(_attackCollider, _attackCollider.GlobalTransform, requested)).Validate();
        _attackTelegraph?.Present(presentation);
        AttackFrameChanged?.Invoke(presentation);
        if (timeline.Phase == AttackPhase.Telegraph &&
            (_lastAttackTimeline.AttackId != timeline.AttackId || _lastAttackTimeline.Phase != AttackPhase.Telegraph))
            PresentationCueRequested?.Invoke(timeline.Kind == EnemyAttackKind.BossGroundWave
                ? PresentationCue.UnblockableAttackTelegraph
                : PresentationCue.AttackTelegraph);
        if (timeline.Phase == AttackPhase.Active && _lastAttackTimeline.Phase != AttackPhase.Active)
            PresentationCueRequested?.Invoke(PresentationCue.AttackStarted);
        _lastAttackTimeline = timeline;
    }
}
