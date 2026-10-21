using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;

namespace Knight.Enemies.Laborer;

// Explicit opt-in. Old wave armor, committed sweep and Heavy-claw contracts stay.
public partial class BellBackLaborer : IResidentEnemyAgent,IDamageReceiver
{
    private ResidentPlacementCandidate _heavyResidence;
    private ResidentCombatProfile _heavyCombatProfile;
    private ResidentPatrolModel _heavyPatrol;
    private readonly ResidentPatrolSettings _heavyPatrolSettings=new();
    private CollisionShape2D _heavyBody;
    private Area2D _heavyTargetArea;
    private Node2D _heavyCombatTarget;
    private uint _heavyHurtLayer,_heavyTargetLayer;
    private bool _heavyHurtMonitoring,_heavyHurtMonitorable,_heavyThreadGroup,_heavyGrappleGroup;
    private bool _heavyDormant,_heavyReturning,_heavyHadTarget,_heavyDamageActive,_heavyVoidPending,_heavyDefeated;
    private float _heavyImpulseX;

    public void ConfigureResidence(ResidentPopulationDefinition definition)
    {
        if(IsInsideTree()||_heavyResidence!=null)throw new InvalidOperationException("Heavy residence must be configured once before mounting.");
        if(definition==null)throw new ArgumentNullException(nameof(definition));
        if(string.IsNullOrWhiteSpace(definition.StableId)||string.IsNullOrWhiteSpace(definition.SubtypeId)||
            !Enum.IsDefined(definition.Family)||!Enum.IsDefined(definition.RouteNode)||!Enum.IsDefined(definition.Sector)||definition.Archetype!=ResidentArchetype.Heavy)
            throw new ArgumentException("Invalid Heavy resident identity.",nameof(definition));
        if(definition.Family==ResidentFamily.Bone
            ?definition.CombatProfile?.BoneWeakness!=ResidentBoneWeakness.RearOrExposed||
                definition.CombatProfile.GhostHiddenSeconds!=0||definition.CombatProfile.GhostManifestSeconds!=0
            :definition.CombatProfile!=null)
            throw new ArgumentException("Heavy Bone requires its supported immutable weakness profile; other families cannot borrow it.",nameof(definition));
        var model=new ResidentPatrolModel(definition.Placement,_heavyPatrolSettings);
        var body=GetNode<CollisionShape2D>("BodyCollider");
        if(body.Disabled||body.TopLevel||body.Shape is not CapsuleShape2D||!HeavyFinite(body.Position)||
            MathF.Abs(body.Rotation)>.001f||body.Scale!=Vector2.One||MathF.Abs(body.Skew)>.001f||
            MathF.Abs(Rotation)>.001f||Scale!=Vector2.One||MathF.Abs(Skew)>.001f)
            throw new ArgumentException("Unsupported Heavy body transform.",nameof(definition));
        foreach(Vector2 point in definition.Placement.PatrolPoints.Append(definition.Placement.WorldSpawn))
            if(!ResidentPatrolModel.Contains(definition.Placement.Territory,new Transform2D(0,point)*body.Transform*body.Shape.GetRect()))
                throw new ArgumentException("Complete Heavy body/path must fit its territory.",nameof(definition));
        _heavyBody=body;_heavyCombatProfile=definition.CombatProfile;_heavyResidence=definition.Placement;_heavyPatrol=model;
    }
    private void InitializeHeavyResidence()
    {
        if(_heavyResidence==null)return;
        _heavyHurtLayer=_hurtbox.CollisionLayer;_heavyHurtMonitoring=_hurtbox.Monitoring;_heavyHurtMonitorable=_hurtbox.Monitorable;
        _heavyTargetArea=_threadTarget.GetNode<Area2D>("TargetCollision");_heavyTargetLayer=_heavyTargetArea.CollisionLayer;
        _heavyThreadGroup=_threadTarget.IsInGroup(ThreadTarget2D.TargetGroup);_heavyGrappleGroup=_threadTarget.IsInGroup(GrappleSurface2D.TargetGroup);
        _hurtbox.SetDamageReceiverPath(new NodePath(".."));ApplyHeavyAuthority();
    }
    private void ExitHeavyResidence()
    {
        if(_heavyResidence==null)return;
        _heavyDamageActive=false;_heavyVoidPending=false;_heavyCombatTarget=null;
        if(IsInstanceValid(_attackHitbox))_attackHitbox.End();PublishAttack(default);
    }
    public void SetResidentDormant(bool dormant)
    {
        if(_heavyResidence==null)throw new InvalidOperationException("Only configured Heavy residents can sleep.");
        if(_heavyDormant==dormant)return;_heavyDormant=dormant;
        if(IsInsideTree()&&IsInstanceValid(_health)){if(dormant)CancelHeavyCombat(true);ApplyHeavyAuthority();}
    }
    public bool ReceiveDamage(DamageRequest request)
    {
        if(_heavyDormant||_heavyVoidPending||!IsInstanceValid(_health)||_health.CurrentHealth<=0)return false;
        // Direction is the real incoming impulse direction, not a local hitpoint.
        // The protected front must not spend the armor's fractional remainder.
        if(_heavyCombatProfile?.BoneWeakness==ResidentBoneWeakness.RearOrExposed&&
            CurrentState!=BellBackLaborerState.Exposed&&
            (!HeavyFinite(request.Direction)||request.Direction.X*Facing<=0))return false;
        // Permission precedes the accumulator; sleep must not spend a hit remainder.
        return CurrentState==BellBackLaborerState.Exposed?_health.ReceiveDamage(request):_armor.ReceiveDamage(request);
    }
    private void ApplyHeavyAuthority()
    {
        bool unavailable=_heavyDormant||_heavyVoidPending||_health.CurrentHealth<=0;
        _hurtbox.CollisionLayer=unavailable?0:_heavyHurtLayer;_hurtbox.Monitoring=!unavailable&&_heavyHurtMonitoring;
        Callable.From(CommitHeavyMonitorable).CallDeferred();_heavyTargetArea.CollisionLayer=unavailable?0:_heavyTargetLayer;
        Group(ThreadTarget2D.TargetGroup,_heavyThreadGroup);Group(GrappleSurface2D.TargetGroup,_heavyGrappleGroup);
        if(unavailable){_heavyDamageActive=false;_attackHitbox.End();}
        void Group(string name,bool original)
        {
            if(unavailable){if(_threadTarget.IsInGroup(name))_threadTarget.RemoveFromGroup(name);}
            else if(original&&!_threadTarget.IsInGroup(name))_threadTarget.AddToGroup(name);
        }
    }
    private void CommitHeavyMonitorable()
    {
        if(!HeavyLive(this)||!HeavyLive(_hurtbox)||!IsInstanceValid(_health))return;
        _hurtbox.Monitorable=!_heavyDormant&&!_heavyVoidPending&&_health.CurrentHealth>0&&_heavyHurtMonitorable;
    }
    private void CancelHeavyCombat(bool returning)
    {
        _heavyReturning|=returning;_heavyDamageActive=false;_attackHitbox.End();_heavyCombatTarget=null;
        CurrentFrame=_model.CancelAttack();CurrentState=CurrentFrame.State;
        _presentationTargetLocked=false;_presentationLockedTarget=Vector2.Zero;_attackFacingLocked=false;
        PublishAttack(default);FrameChanged?.Invoke(CurrentFrame,Facing);
    }
    private void ResetHeavyResidence()
    {
        if(_heavyResidence==null)return;
        _heavyPatrol=new ResidentPatrolModel(_heavyResidence,_heavyPatrolSettings);_heavyDormant=false;
        _heavyReturning=_heavyHadTarget=_heavyDamageActive=_heavyVoidPending=_heavyDefeated=false;
        _heavyImpulseX=0;_heavyCombatTarget=null;ApplyHeavyAuthority();
    }
    private void HurtHeavyResidence(Vector2 impulse)
    {
        if(_health.CurrentHealth<=0)return;
        CancelHeavyCombat(true);
        if(HeavyFinite(impulse)){_heavyImpulseX+=impulse.X;Velocity=new(Velocity.X,Velocity.Y+impulse.Y);}
    }
    private void DieHeavyResidence()
    {
        _heavyVoidPending=false;_heavyDamageActive=false;_heavyImpulseX=0;Velocity=Vector2.Zero;_heavyCombatTarget=null;
        CurrentFrame=_model.Step(0,false,false,true,0);CurrentState=CurrentFrame.State;
        _presentationTargetLocked=false;_attackFacingLocked=false;ApplyHeavyAuthority();PublishAttack(default);FrameChanged?.Invoke(CurrentFrame,Facing);
    }
    private void StepHeavyResidence(double delta)
    {
        if(!double.IsFinite(delta)||delta<=0||delta>.25)return;float step=(float)delta;
        if(_heavyVoidPending)ApplyPendingHeavyDeath();if(_health.CurrentHealth<=0){Velocity=Vector2.Zero;return;}
        bool inside=ResidentPatrolModel.Contains(_heavyResidence.Territory,HeavyBounds());
        if(!inside)_heavyReturning=true;
        bool homePause=_heavyReturning&&inside&&IsOnFloor()&&MathF.Abs(GlobalPosition.X-_heavyResidence.WorldSpawn.X)<=_heavyPatrolSettings.ArrivalTolerance&&MathF.Abs(_heavyImpulseX)<1;
        if(homePause)_heavyReturning=false;
        Node2D player=null;
        if(!_heavyDormant&&!_heavyVoidPending&&!_heavyReturning&&!homePause)
            player=CurrentState is BellBackLaborerState.Telegraph or BellBackLaborerState.Attack
                ?HeavyTargetAllowed(_heavyCombatTarget)?_heavyCombatTarget:null:SelectHeavyTarget();
        bool allowed=player!=null&&IsOnFloor();
        if(!allowed&&(_heavyHadTarget||CurrentState is BellBackLaborerState.Guard or BellBackLaborerState.Telegraph or BellBackLaborerState.Attack or BellBackLaborerState.Recover))CancelHeavyCombat(true);
        _heavyHadTarget=allowed;
        float gap=player==null?float.PositiveInfinity:player.GlobalPosition.X-GlobalPosition.X;
        BellBackLaborerFrame frame=_model.Step(gap,allowed,_threadRequested,false,delta);_threadRequested=false;
        if(frame.State==BellBackLaborerState.Telegraph&&frame.EnteredState)_heavyCombatTarget=player;
        float speed=_settings.MoveSpeed;
        if(frame.State is BellBackLaborerState.Idle or BellBackLaborerState.Approach && !allowed)
        {
            float direction=0;
            if(!_heavyDormant&&!_heavyVoidPending&&!homePause&&MathF.Abs(_heavyImpulseX)<1)
            {
                if(_heavyReturning)
                {direction=MathF.Sign(_heavyResidence.WorldSpawn.X-GlobalPosition.X);if(!CanHeavyMove(direction*_heavyPatrolSettings.Speed*step))direction=0;}
                else direction=_heavyPatrol.Step(new(GlobalPosition,HeavyBounds(),false,IsOnFloor(),CanHeavyMove(-_heavyPatrolSettings.Speed*step),CanHeavyMove(_heavyPatrolSettings.Speed*step))).MoveDirection;
            }
            speed=_heavyPatrolSettings.Speed;frame=frame with {MoveDirection=direction,State=direction==0?BellBackLaborerState.Idle:BellBackLaborerState.Approach};
        }
        if(_heavyDormant||_heavyVoidPending||homePause)frame=frame with {MoveDirection=0};
        if(frame.MoveDirection!=0&&!CanHeavyMove(frame.MoveDirection*speed*step))frame=frame with {MoveDirection=0};
        CurrentFrame=frame;CurrentState=frame.State;
        UpdateAttackFacing(frame.AttackTimeline,gap);_attackHitbox.Position=new(0,-10);_attackHitbox.Rotation=Facing<0?Mathf.Pi:0;
        AttackTimelineFrame timeline=EnrichTimeline(frame.AttackTimeline,player);
        _heavyDamageActive=frame.AttackActive&&allowed&&!_heavyDormant&&!_heavyVoidPending;
        if(_heavyDamageActive&&_lastAttackTimeline.Phase!=AttackPhase.Active)_attackHitbox.Begin(timeline.AttackId);
        else if(!_heavyDamageActive)_attackHitbox.End();PublishAttack(timeline);
        Transform2D before=_heavyBody.GlobalTransform;
        Velocity=new(frame.MoveDirection*speed+_heavyImpulseX,Velocity.Y+_settings.Gravity*step);_heavyImpulseX*=Mathf.Exp(-12*step);
        MoveAndSlide();
        if(!_heavyVoidPending&&ResidentVoidContact.Crossed(this,_heavyBody,before))_heavyVoidPending=true;
        if(_heavyVoidPending)ApplyPendingHeavyDeath();FrameChanged?.Invoke(CurrentFrame,Facing);
    }
    private Node2D SelectHeavyTarget()
    {
        Node2D nearest=null;float distance=float.PositiveInfinity;
        using Godot.Collections.Array players=(Godot.Collections.Array)GetTree().GetNodesInGroup("player");
        for(int i=0;i<players.Count;i++)if(players[i].AsGodotObject() is Node2D player&&HeavyTargetAllowed(player))
        {float next=GlobalPosition.DistanceSquaredTo(player.GlobalPosition);if(next<distance){nearest=player;distance=next;}}
        return nearest;
    }
    private bool HeavyTargetAllowed(Node2D player)
    {
        if(_heavyDormant||_heavyVoidPending||_health.CurrentHealth<=0||!HeavyLive(player)||player.GetWorld2D()!=GetWorld2D()||
            !player.IsInGroup("player")||player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not >0||
            GlobalPosition.DistanceTo(player.GlobalPosition)>_settings.DetectionRange||!ResidentPatrolModel.Contains(_heavyResidence.Territory,HeavyBounds()))return false;
        CollisionShape2D body=null;
        for(int i=0;i<player.GetChildCount();i++)if(player.GetChild(i) is CollisionShape2D {Disabled:false,Shape:not null} found){body=found;break;}
        return body!=null&&ResidentPatrolModel.Contains(_heavyResidence.Territory,body.GlobalTransform*body.Shape.GetRect())&&HasClearTerrainPathTo(player.GlobalPosition);
    }
    private bool HeavyCanHit(Hurtbox target)
    {
        if(!_heavyDamageActive||_heavyDormant||_heavyVoidPending||_health.CurrentHealth<=0||!HeavyLive(target))return false;
        for(Node node=target;node!=null;node=node.GetParent())if(node is Node2D player&&player.IsInGroup("player"))return ReferenceEquals(player,_heavyCombatTarget)&&HeavyTargetAllowed(player);
        return false;
    }
    private Rect2 HeavyBounds()=>_heavyBody.GlobalTransform*_heavyBody.Shape.GetRect();
    // Full continuous static top support, not two rays/center contact. Only
    // autonomous intent is bounded: accepted impulse and gravity compose later.
    private bool CanHeavyMove(float dx)
    {
        if(!IsOnFloor()||!float.IsFinite(dx)||dx==0)return false;
        Rect2 body=HeavyBounds(),ahead=new(body.Position+new Vector2(dx+MathF.Sign(dx)*2,0),body.Size);
        if(ResidentPatrolModel.Contains(_heavyResidence.Territory,body))
        {if(!ResidentPatrolModel.Contains(_heavyResidence.Territory,ahead))return false;}
        else if(MathF.Abs(GlobalPosition.X+dx-_heavyResidence.WorldSpawn.X)>=MathF.Abs(GlobalPosition.X-_heavyResidence.WorldSpawn.X))return false;
        if(TestMove(GlobalTransform,new(dx,0),null,SafeMargin,false))return false;
        Rect2 sweep=body.Merge(ahead);using var strip=new RectangleShape2D{Size=new(sweep.Size.X,2.5f)};
        var exclusions=new Godot.Collections.Array<Rid>{GetRid()};using Godot.Collections.Array owner=(Godot.Collections.Array)exclusions;
        using var query=new PhysicsShapeQueryParameters2D{Shape=strip,Transform=new(0,new Vector2(sweep.GetCenter().X,body.End.Y)),
            CollisionMask=CombatCollisionLayers.World,CollideWithAreas=false,CollideWithBodies=true,Exclude=exclusions};
        using Godot.Collections.Array hits=(Godot.Collections.Array)GetWorld2D().DirectSpaceState.IntersectShape(query,64);
        if(hits.Count>=64)return false;var intervals=new List<Rect2>();
        for(int i=0;i<hits.Count;i++)
        {
            using Godot.Collections.Dictionary hit=hits[i].AsGodotDictionary();
            if(hit["collider"].AsGodotObject() is not StaticBody2D floor)continue;
            if(floor.ShapeOwnerGetOwner(floor.ShapeFindOwner(hit["shape"].AsInt32())) is not CollisionShape2D {Disabled:false,OneWayCollision:false,Shape:RectangleShape2D} shape)continue;
            Transform2D pose=shape.GlobalTransform;
            if(MathF.Abs(pose.X.Y)>.001f||MathF.Abs(pose.Y.X)>.001f||pose.X.X<=0||pose.Y.Y<=0)continue;
            Rect2 top=pose*shape.Shape.GetRect();if(MathF.Abs(top.Position.Y-body.End.Y)<=1.25f)intervals.Add(top);
        }
        float covered=sweep.Position.X;
        foreach(Rect2 interval in intervals.OrderBy(x=>x.Position.X)){if(interval.End.X<covered-.001f)continue;if(interval.Position.X>covered+.001f)break;covered=MathF.Max(covered,interval.End.X);}
        return covered>=sweep.End.X-.001f;
    }
    private void ApplyPendingHeavyDeath()
    {
        CancelHeavyCombat(true);ApplyHeavyAuthority();if(_health.CurrentHealth>0)_health.ApplyDamage(new(_health.CurrentHealth,Vector2.Zero,0,ulong.MaxValue,GetInstanceId()));
    }
    private static bool HeavyFinite(Vector2 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y);
    private static bool HeavyLive(Node node)
    {
        if(!GodotObject.IsInstanceValid(node)||!node.IsInsideTree())return false;
        for(Node current=node;current!=null;current=current.GetParent())if(current.IsQueuedForDeletion())return false;return true;
    }
}
