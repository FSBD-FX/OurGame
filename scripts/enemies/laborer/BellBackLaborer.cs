using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Enemies.Laborer;

public partial class BellBackLaborer : CharacterBody2D, IEncounterActor, IThreadInteractionReceiver
{
    [Export] public NodePath HealthPath { get; set; } = new("Health");
    [Export] public NodePath ArmorPath { get; set; } = new("Armor");
    [Export] public NodePath HurtboxPath { get; set; } = new("Hurtbox");
    [Export] public NodePath AttackHitboxPath { get; set; } = new("AttackHitbox");
    [Export] public BellBackLaborerTuning Tuning { get; set; } = new();

    public event Action<BellBackLaborerFrame, int> FrameChanged;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;
    public event Action Defeated;

    public BellBackLaborerState CurrentState { get; private set; } = BellBackLaborerState.Idle;
    public BellBackLaborerFrame CurrentFrame { get; private set; } =
        new(BellBackLaborerState.Idle, 0f, false, false, false);
    public int Facing { get; private set; } = 1;

    private BellBackLaborerModel _model = new();
    private BellBackLaborerSettings _settings;
    private HealthComponent _health;
    private ArmoredDamageReceiver _armor;
    private Hurtbox _hurtbox;
    private Hitbox _attackHitbox;
    private CollisionShape2D _attackCollider;
    private ThreadTarget2D _threadTarget;
    private AttackTelegraphVisual _attackTelegraph;
    private Vector2 _spawnPosition;
    private bool _threadRequested;
    private AttackTimelineFrame _lastAttackTimeline;
    private bool _presentationTargetLocked;
    private Vector2 _presentationLockedTarget;
    private bool _attackFacingLocked;
    private int _lockedAttackFacing = 1;

    public override void _Ready()
    {
        Tuning ??= new BellBackLaborerTuning();
        var errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid BellBackLaborerTuning: " + string.Join("; ", errors));
        _settings = Tuning.ToSettings();
        _model = new BellBackLaborerModel(_settings);
        _health = GetNode<HealthComponent>(HealthPath);
        _armor = GetNode<ArmoredDamageReceiver>(ArmorPath);
        _hurtbox = GetNode<Hurtbox>(HurtboxPath);
        _attackHitbox = GetNode<Hitbox>(AttackHitboxPath);
        _attackCollider = _attackHitbox.GetNode<CollisionShape2D>("AttackCollider");
        _threadTarget = GetNode<ThreadTarget2D>("ThreadTarget");
        _attackTelegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _attackHitbox.Damage = _settings.ContactDamage;
        _attackHitbox.TargetFilter = CanHitTarget;
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        _spawnPosition = GlobalPosition;
        UseArmor();
        InitializeHeavyResidence();
    }

    public override void _ExitTree()
    {
        ExitHeavyResidence();
        if (IsInstanceValid(_attackHitbox)) _attackHitbox.TargetFilter = null;
        if (!IsInstanceValid(_health)) return;
        _health.Damaged -= OnDamaged;
        _health.Died -= OnDied;
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_heavyResidence!=null){StepHeavyResidence(delta);return;}
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        float signedDistance = player == null ? float.PositiveInfinity : player.GlobalPosition.X - GlobalPosition.X;
        bool seesPlayer = player != null && Mathf.Abs(signedDistance) <= _settings.DetectionRange &&
            HasClearTerrainPathTo(player.GlobalPosition);
        CurrentFrame = _model.Step(signedDistance, seesPlayer, _threadRequested, _health.CurrentHealth <= 0, delta);
        CurrentState = CurrentFrame.State;
        _threadRequested = false;
        if (CurrentFrame.State == BellBackLaborerState.Exposed) UseHealth(); else UseArmor();

        UpdateAttackFacing(CurrentFrame.AttackTimeline, signedDistance);
        _attackHitbox.Position = new Vector2(0f, -10f);
        _attackHitbox.Rotation = Facing < 0 ? Mathf.Pi : 0f;
        AttackTimelineFrame timeline = EnrichTimeline(CurrentFrame.AttackTimeline, player);
        if (timeline.Phase == AttackPhase.Active && _lastAttackTimeline.Phase != AttackPhase.Active)
        {
            _attackHitbox.Begin(timeline.AttackId);
        }
        else if (!CurrentFrame.AttackActive) _attackHitbox.End();
        PublishAttack(timeline);

        Velocity = CurrentState == BellBackLaborerState.Dead
            ? Vector2.Zero
            : new Vector2(CurrentFrame.MoveDirection * _settings.MoveSpeed,
                Velocity.Y + _settings.Gravity * (float)delta);
        MoveAndSlide();
        FrameChanged?.Invoke(CurrentFrame, Facing);
    }

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if (_health == null || _health.CurrentHealth <= 0) return false;
        if(_heavyResidence!=null)
        {
            if(_heavyDormant||_heavyVoidPending)return false;
            CancelHeavyCombat(true);
        }
        _threadRequested = true;
        return true;
    }

    private bool CanHitTarget(Hurtbox target) => _heavyResidence==null?HasClearTerrainPathTo(target.GlobalPosition):HeavyCanHit(target);

    private bool HasClearTerrainPathTo(Vector2 target)
    {
        // Terrain separates encounter layers. Sensors, inactive attack areas and
        // other actors are not solid terrain and must not blind this enemy.
        var query = PhysicsRayQueryParameters2D.Create(GlobalPosition, target, CombatCollisionLayers.World);
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;
        query.HitFromInside = true;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
        return GetWorld2D().DirectSpaceState.IntersectRay(query).Count == 0;
    }

    public void ResetForCheckpoint()
    {
        GlobalPosition = _spawnPosition;
        Velocity = Vector2.Zero;
        _threadRequested = false;
        _model = new BellBackLaborerModel(_settings);
        CurrentState = BellBackLaborerState.Idle;
        CurrentFrame = new BellBackLaborerFrame(BellBackLaborerState.Idle, 0f, false, false, false);
        _attackHitbox?.End();
        _presentationTargetLocked = false;
        _attackFacingLocked = false;
        _lockedAttackFacing = 1;
        PublishAttack(default);
        _armor?.ResetAccumulator();
        _health?.ResetFull();
        _threadTarget?.AddToGroup("thread_targets");
        UseArmor();
        ResetHeavyResidence();
    }

    private void UseArmor()
    {
        if(_heavyResidence!=null){_hurtbox?.SetDamageReceiverPath(new NodePath(".."));return;}
        if (_hurtbox != null && _hurtbox.DamageReceiverPath != new NodePath("../Armor"))
            _hurtbox.SetDamageReceiverPath(new NodePath("../Armor"));
    }

    private void UseHealth()
    {
        if(_heavyResidence!=null){_hurtbox?.SetDamageReceiverPath(new NodePath(".."));return;}
        if (_hurtbox != null && _hurtbox.DamageReceiverPath != new NodePath("../Health"))
            _hurtbox.SetDamageReceiverPath(new NodePath("../Health"));
    }

    private void OnDamaged(int remaining, Vector2 impulse)
    {
        if(_heavyResidence!=null)HurtHeavyResidence(impulse);
        PresentationCueRequested?.Invoke(PresentationCue.Hurt);
    }

    private void OnDied()
    {
        if(_heavyResidence!=null)
        {
            if(_heavyDefeated)return;_heavyDefeated=true;
            DieHeavyResidence();PresentationCueRequested?.Invoke(PresentationCue.Died);Defeated?.Invoke();return;
        }
        Velocity = Vector2.Zero;
        _threadTarget?.RemoveFromGroup("thread_targets");
        _attackHitbox?.End();
        _attackFacingLocked = false;
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
        if (timeline.AttackId != _lastAttackTimeline.AttackId) _presentationTargetLocked = false;
        bool lockNow = timeline.Phase == AttackPhase.Active ||
            timeline.Phase == AttackPhase.Telegraph &&
            timeline.PhaseRemainingSeconds <= timeline.PhaseDurationSeconds * .5f;
        if (!_presentationTargetLocked && lockNow)
        {
            _presentationLockedTarget = player?.GlobalPosition ?? _attackCollider.GlobalPosition;
            _presentationTargetLocked = true;
        }
        return _presentationTargetLocked
            ? timeline with { LockedWorldTarget = _presentationLockedTarget, HasLockedTarget = true }
            : timeline;
    }

    private void UpdateAttackFacing(AttackTimelineFrame timeline, float signedDistanceToPlayer)
    {
        if (timeline.Phase == AttackPhase.None)
        {
            _attackFacingLocked = false;
            if (CurrentFrame.MoveDirection != 0f) Facing = CurrentFrame.MoveDirection < 0f ? -1 : 1;
            return;
        }

        if (!_attackFacingLocked || timeline.AttackId != _lastAttackTimeline.AttackId)
        {
            _lockedAttackFacing = Mathf.Abs(signedDistanceToPlayer) > .001f
                ? (signedDistanceToPlayer < 0f ? -1 : 1)
                : Facing;
            _attackFacingLocked = true;
        }
        Facing = _lockedAttackFacing;
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
                AttackShapeKind.ConvexPolygon)).Validate();
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
