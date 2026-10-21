using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;

namespace Knight.Enemies;

/// <summary>Scene adapter that binds the squire model to Godot physics and existing combat contracts.</summary>
public partial class RustShellSquire : CharacterBody2D, IThreadPullReceiver, IEncounterActor, IResidentEnemyAgent, IDamageReceiver
{
    public ResidentPatrolFrame ResidenceFrame { get; private set; }
    private readonly ResidentPatrolSettings _residentSettings=new();
    private ResidentPatrolModel _residentModel;
    private ResidentPlacementCandidate _residence;
    private CollisionShape2D _residentBody;
    private Area2D _residentTargetArea;
    private bool _residentDormant,_residentPermitted,_originalHurtMonitoring,_originalHurtMonitorable;
    private bool _originalThreadGroup,_originalGrappleGroup;
    private uint _originalHurtLayer,_originalTargetLayer;
    private Func<Hurtbox,bool> _priorResidentTargetFilter;
    private bool _residentVoidPending;

    public void ConfigureResidence(ResidentPopulationDefinition definition)
    {
        if(IsInsideTree()||_residentModel!=null) throw new InvalidOperationException("Residence must be configured once, before mounting.");
        if(definition==null) throw new ArgumentNullException(nameof(definition));
        if(string.IsNullOrWhiteSpace(definition.StableId)||string.IsNullOrWhiteSpace(definition.SubtypeId)||
           !Enum.IsDefined(definition.Family)||!Enum.IsDefined(definition.RouteNode)||!Enum.IsDefined(definition.Sector)||
           definition.Archetype!=ResidentArchetype.Wanderer)
            throw new ArgumentException("Rust residence supports a defined ground wanderer only.",nameof(definition));
        ValidateWandererCombatProfile(definition);
        var model=new ResidentPatrolModel(definition.Placement,_residentSettings);
        CollisionShape2D body=GetNode<CollisionShape2D>("BodyCollider");
        if(body.Disabled||body.Shape is not CapsuleShape2D || body.TopLevel ||
           MathF.Abs(Rotation)>.001f||Scale!=Vector2.One||MathF.Abs(Skew)>.001f)
            throw new ArgumentException("Unsupported resident body transform.",nameof(definition));
        foreach(Vector2 point in definition.Placement.PatrolPoints.Append(definition.Placement.WorldSpawn))
        {
            Rect2 bounds=new Transform2D(0,point)*body.Transform*body.Shape.GetRect();
            if(!ResidentPatrolModel.Contains(definition.Placement.Territory,bounds))
                throw new ArgumentException("Complete resident body/path must fit its territory.",nameof(definition));
        }
        _residentBody=body;_residentCombatFamily=definition.Family;_residentCombatProfile=definition.CombatProfile;
        _residence=definition.Placement;_residentModel=model;
    }

    public void SetResidentDormant(bool dormant)
    {
        if(_residentModel==null) throw new InvalidOperationException("Only configured residents can sleep.");
        if(_residentDormant==dormant) return;
        _residentDormant=dormant;
        if(dormant) { _stunRequested=true;_boneThreadOpening=false; }
        if(IsInsideTree()&&_health!=null) ApplyResidentDormancy();
    }

    public bool ReceiveDamage(DamageRequest request) => !_residentDormant && !_residentVoidPending &&
        GhostPhaseAllowsInteraction && BonePoseAllowsDamage && _health!=null && _health.ReceiveDamage(request);
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
        if(_residentModel!=null)
        {
            _originalHurtLayer=_hurtbox.CollisionLayer;_originalHurtMonitoring=_hurtbox.Monitoring;_originalHurtMonitorable=_hurtbox.Monitorable;
            _residentTargetArea=_threadTarget.GetNode<Area2D>("TargetCollision");_originalTargetLayer=_residentTargetArea.CollisionLayer;
            _originalThreadGroup=_threadTarget.IsInGroup(ThreadTarget2D.TargetGroup);
            _originalGrappleGroup=_threadTarget.IsInGroup(GrappleSurface2D.TargetGroup);
            _priorResidentTargetFilter=_attackHitbox.TargetFilter;
            _attackHitbox.TargetFilter=ResidentMayHit;
            _hurtbox.SetDamageReceiverPath(new NodePath(".."));
            InitializeGhostResidence();
            ApplyResidentDormancy();
        }
    }

    public override void _ExitTree()
    {
        _residentVoidPending=false;
        if (IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(_residentModel!=null&&_residentVoidPending) ApplyPendingResidentVoidDeath();
        StepGhostPhase(delta);
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        bool seesPlayer = player is not null && HasLineOfSightTo(player);
        if(_residentModel!=null)
        {
            seesPlayer=seesPlayer&&!_residentDormant&&!_residentVoidPending&&GhostPhaseAllowsInteraction&&IsOnFloor()&&player.GetWorld2D()==GetWorld2D()&&
                ResidentPatrolModel.Contains(_residence.Territory,ResidentBounds())&&
                TryTargetBounds(player,out Rect2 targetBounds)&&ResidentPatrolModel.Contains(_residence.Territory,targetBounds);
            if(_residentDormant||_residentVoidPending||(_residentPermitted&&!seesPlayer)) _stunRequested=true;
            _residentPermitted=seesPlayer;
        }
        float signedDistance = player is null ? float.PositiveInfinity : player.GlobalPosition.X - GlobalPosition.X;
        RustShellFrame frame = _model.Step(signedDistance, seesPlayer, _stunRequested, _health.CurrentHealth <= 0, delta);
        float authoredSpeed=_settings.MoveSpeed;
        if(_residentModel!=null)
        {
            ResidenceFrame=_residentModel.Step(new(GlobalPosition,ResidentBounds(),seesPlayer,IsOnFloor(),
                CanResidentMove(-_residentSettings.Speed*(float)delta),CanResidentMove(_residentSettings.Speed*(float)delta),
                frame.State==RustShellState.Stunned,_residentDormant,_health.CurrentHealth<=0));
            if(ResidenceFrame.State is ResidentPatrolState.Patrolling or ResidentPatrolState.Returning && frame.State is RustShellState.Idle or RustShellState.Chase)
            {
                authoredSpeed=_residentSettings.Speed;
                frame=frame with { MoveDirection=ResidenceFrame.MoveDirection,
                    State=ResidenceFrame.MoveDirection!=0?RustShellState.Chase:RustShellState.Idle };
            }
            if(frame.MoveDirection!=0&&!CanResidentMove(frame.MoveDirection*authoredSpeed*(float)delta))
                frame=frame with { MoveDirection=0 };
        }
        CurrentFrame = frame;
        CurrentState = frame.State;
        if(frame.State!=RustShellState.Stunned)_boneThreadOpening=false;
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
        float horizontalVelocity = frame.MoveDirection * authoredSpeed + pullX + _damageImpulse.X;
        _damageImpulse = _damageImpulse.Lerp(Vector2.Zero, 1f - Mathf.Exp(-12f * (float)delta));
        _threadPullVelocity = _threadPullVelocity.Lerp(Vector2.Zero, 1f - Mathf.Exp(-10f * (float)delta));
        Velocity = new Vector2(horizontalVelocity, Velocity.Y + Gravity * (float)delta + _damageImpulse.Y);
        if (frame.State == RustShellState.Dead) Velocity = Vector2.Zero;
        Transform2D beforeResidentMove=_residentModel!=null?_residentBody.GlobalTransform:default;
        MoveAndSlide();
        if(_residentModel!=null&&_health.CurrentHealth>0)
        {
            if(!_residentVoidPending&&ResidentVoidContact.Crossed(this,_residentBody,beforeResidentMove)) _residentVoidPending=true;
            if(_residentVoidPending) ApplyPendingResidentVoidDeath();
        }
        FrameChanged?.Invoke(frame, _facing);
    }

    public void ApplyThreadPull(Vector2 velocity)
    {
        if(!GhostPhaseAllowsInteraction||_residentModel!=null&&(_residentDormant||_residentVoidPending||_health!=null&&_health.CurrentHealth<=0)) return;
        _threadPullVelocity = velocity;
        Velocity = velocity;
        if(HasBoneProfile)_boneThreadOpening=true;
        _stunRequested = true;
    }

    private void ApplyResidentDormancy()
    {
        bool unavailable=_residentDormant||HasFamilyCombatProfile&&(_residentVoidPending||_health.CurrentHealth<=0);
        bool targetUnavailable=unavailable||!GhostPhaseAllowsInteraction;
        _hurtbox.CollisionLayer=unavailable?0:_originalHurtLayer;
        _hurtbox.Monitoring=!unavailable&&_originalHurtMonitoring;
        if(HasFamilyCombatProfile)Callable.From(CommitResidentHurtMonitorable).CallDeferred();
        else _hurtbox.Monitorable=!_residentDormant&&_originalHurtMonitorable;
        _residentTargetArea.CollisionLayer=targetUnavailable?0:_originalTargetLayer;
        SetTargetGroup(ThreadTarget2D.TargetGroup,_originalThreadGroup);
        SetTargetGroup(GrappleSurface2D.TargetGroup,_originalGrappleGroup);
        if(targetUnavailable) { _attackHitbox.End();PublishAttack(default); }
        void SetTargetGroup(string group,bool original)
        {
            if(targetUnavailable) { if(_threadTarget.IsInGroup(group))_threadTarget.RemoveFromGroup(group); }
            else if(original&&!_threadTarget.IsInGroup(group))_threadTarget.AddToGroup(group);
        }
    }

    private bool ResidentMayHit(Hurtbox target) => !_residentDormant&&!_residentVoidPending&&GhostPhaseAllowsInteraction&&_health.CurrentHealth>0&&CurrentFrame.AttackActive&&
        _priorResidentTargetFilter?.Invoke(target)!=false&&target.GetWorld2D()==GetWorld2D()&&
        ResidentPatrolModel.Contains(_residence.Territory,ResidentBounds())&&TryTargetBounds(target,out Rect2 bounds)&&
        ResidentPatrolModel.Contains(_residence.Territory,bounds)&&HasLineOfSightTo(target);

    private Rect2 ResidentBounds()=>_residentBody.GlobalTransform*_residentBody.Shape.GetRect();
    private void ApplyPendingResidentVoidDeath()
    {
        _boneThreadOpening=false;
        if(HasFamilyCombatProfile)ApplyResidentDormancy();
        _stunRequested=true;_residentPermitted=false;
        _attackHitbox.End();PublishAttack(default);
        // A rejected invulnerable request does not consume its identity. Keep
        // pending through sleep/source removal; Health's own physics keeps time.
        if(_health.CurrentHealth>0)
            _health.ApplyDamage(new DamageRequest(_health.CurrentHealth,Vector2.Zero,0,ulong.MaxValue,GetInstanceId()));
    }
    private static bool TryTargetBounds(Node2D target,out Rect2 bounds)
    {
        using Godot.Collections.Array children=(Godot.Collections.Array)target.GetChildren();
        for(int i=0;i<children.Count;i++)
            if(children[i].AsGodotObject() is CollisionShape2D shape&&!shape.Disabled&&shape.Shape!=null)
            { bounds=shape.GlobalTransform*shape.Shape.GetRect();return true; }
        bounds=default;return false;
    }

    // This guard affects autonomous velocity only; external forces/gravity are
    // composed afterwards. Axis-aligned static top edges are the supported policy.
    private bool CanResidentMove(float dx)
    {
        if(!IsOnFloor()||!float.IsFinite(dx)||dx==0) return false;
        Rect2 body=ResidentBounds();
        Rect2 ahead=new(body.Position+new Vector2(dx+MathF.Sign(dx)*2,0),body.Size);
        bool currentlyInside=ResidentPatrolModel.Contains(_residence.Territory,body);
        if(currentlyInside&&!ResidentPatrolModel.Contains(_residence.Territory,ahead)) return false;
        // A force may carry the body outside. Permit a collision-safe step
        // toward home from there; requiring the next body already inside
        // strands it forever and is not a return path.
        if(!currentlyInside&&MathF.Abs(GlobalPosition.X+dx-_residence.WorldSpawn.X)>=MathF.Abs(GlobalPosition.X-_residence.WorldSpawn.X)) return false;
        if(TestMove(GlobalTransform,new Vector2(dx,0),null,SafeMargin,false)) return false;
        Rect2 sweep=body.Merge(ahead);
        using var strip=new RectangleShape2D { Size=new Vector2(sweep.Size.X,2.5f) };
        var exclusions=new Godot.Collections.Array<Rid> {GetRid()};
        using Godot.Collections.Array ownedExclusions=(Godot.Collections.Array)exclusions;
        using var query=new PhysicsShapeQueryParameters2D {Shape=strip,Transform=new Transform2D(0,new Vector2(sweep.GetCenter().X,body.End.Y)),
            CollisionMask=CombatCollisionLayers.World,CollideWithAreas=false,CollideWithBodies=true,Exclude=exclusions};
        using Godot.Collections.Array hits=(Godot.Collections.Array)GetWorld2D().DirectSpaceState.IntersectShape(query,64);
        if(hits.Count>=64) return false;
        var intervals=new List<Rect2>();
        for(int i=0;i<hits.Count;i++)
        {
            using Godot.Collections.Dictionary hit=hits[i].AsGodotDictionary();
            if(hit["collider"].AsGodotObject() is not StaticBody2D floor) continue;
            if(floor.ShapeOwnerGetOwner(floor.ShapeFindOwner(hit["shape"].AsInt32())) is not CollisionShape2D shape||
                shape.Disabled||shape.OneWayCollision||shape.Shape is not RectangleShape2D) continue;
            Transform2D transform=shape.GlobalTransform;
            if(MathF.Abs(transform.X.Y)>.001f||MathF.Abs(transform.Y.X)>.001f||transform.X.X<=0||transform.Y.Y<=0) continue;
            Rect2 top=transform*shape.Shape.GetRect();
            if(MathF.Abs(top.Position.Y-body.End.Y)<=1.25f) intervals.Add(top);
        }
        float covered=sweep.Position.X;
        foreach(Rect2 interval in intervals.OrderBy(x=>x.Position.X))
        {
            if(interval.End.X<covered-.001f) continue;
            if(interval.Position.X>covered+.001f) break;
            covered=MathF.Max(covered,interval.End.X);
        }
        return covered>=sweep.End.X-.001f;
    }

    public void ResetForCheckpoint()
    {
        _residentVoidPending=false;
        GlobalPosition = _checkpointPosition;
        Velocity = Vector2.Zero;
        _threadPullVelocity = Vector2.Zero;
        _damageImpulse = Vector2.Zero;
        _boneThreadOpening = false;
        _stunRequested = false;
        _model = new RustShellModel();
        CurrentState = RustShellState.Idle;
        CurrentFrame = new RustShellFrame(RustShellState.Idle, 0f, false, false);
        _attackHitbox?.End();
        _presentationTargetLocked = false;
        PublishAttack(default);
        _health?.ResetFull();
        ResetGhostPhase();
        if(HasBoneProfile)ApplyResidentDormancy();
    }

    public bool HasLineOfSightTo(Node2D player)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(GlobalPosition, player.GlobalPosition);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        var selfColliders = new Godot.Collections.Array<Rid>();
        AppendSelfColliders(this, selfColliders);
        query.Exclude = selfColliders;
        Godot.Collections.Dictionary hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return true;
        Node collider = hit["collider"].AsGodotObject() as Node;
        return collider != null && NodesBelongToSameTarget(collider, player);
    }

    private static void AppendSelfColliders(Node node, Godot.Collections.Array<Rid> exclusions)
    {
        // Inactive attack areas remain ray-queryable. Exclude the whole actor,
        // including nested targets/sensors, without ignoring external occluders.
        if (node is CollisionObject2D collider) exclusions.Add(collider.GetRid());
        foreach (Node child in node.GetChildren()) AppendSelfColliders(child, exclusions);
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
        _residentVoidPending=false;
        _boneThreadOpening=false;
        if(HasFamilyCombatProfile)ApplyResidentDormancy();
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
