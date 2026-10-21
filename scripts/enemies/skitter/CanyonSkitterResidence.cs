using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Residents;
using Knight.Mechanisms;
using Knight.Prologue.Residents;

namespace Knight.Enemies.Skitter;

// Opt-in only. The saved encounter actor retains its existing default policy.
public partial class CanyonSkitter : IResidentEnemyAgent,IDamageReceiver,IThreadInteractionReceiver
{
    public ResidentPatrolFrame ResidenceFrame {get;private set;}
    private ResidentPatrolModel _residentModel;
    private readonly ResidentPatrolSettings _residentSettings=new();
    private ResidentPlacementCandidate _residence;
    private CollisionShape2D _residentBody;
    private Hurtbox _residentHurtbox;
    private ThreadTarget2D _residentTarget;
    private Area2D _residentTargetArea;
    private uint _residentHurtLayer,_residentTargetLayer;
    private bool _residentHurtMonitoring,_residentHurtMonitorable,_residentThreadGroup,_residentGrappleGroup;
    private bool _residentDormant,_residentVoidPending;
    private float _residentCombatOriginX;
    private ResidentCombatProfile _residentCombatProfile;
    private float _boneThreadRemaining;
    private bool HasBoneProfile=>_residentCombatProfile?.BoneWeakness==ResidentBoneWeakness.RecoveryOrThreadExposed;
    // Moving Retreat also publishes a Recovery timeline, but is not this weak pose.
    private bool BonePoseAllowsDamage=>!HasBoneProfile||CurrentFrame.State==CanyonSkitterState.Recover||
        _boneThreadRemaining>0&&CurrentFrame.State is CanyonSkitterState.Stunned or CanyonSkitterState.Falling;

    public void ConfigureResidence(ResidentPopulationDefinition definition)
    {
        if(IsInsideTree()||_residentModel!=null)throw new InvalidOperationException("Residence must be configured once before mounting.");
        if(definition==null)throw new ArgumentNullException(nameof(definition));
        if(string.IsNullOrWhiteSpace(definition.StableId)||string.IsNullOrWhiteSpace(definition.SubtypeId)||
            !Enum.IsDefined(definition.Family)||!Enum.IsDefined(definition.RouteNode)||!Enum.IsDefined(definition.Sector)||
            definition.Archetype!=ResidentArchetype.Charger)
            throw new ArgumentException("Skitter residence supports a defined ground Charger only.",nameof(definition));
        ValidateChargerCombatProfile(definition);
        var model=new ResidentPatrolModel(definition.Placement,_residentSettings);
        var body=GetNode<CollisionShape2D>("BodyCollider");
        if(body.Disabled||body.TopLevel||body.Shape is not RectangleShape2D||
            MathF.Abs(Rotation)>.001f||Scale!=Vector2.One||MathF.Abs(Skew)>.001f)
            throw new ArgumentException("Unsupported resident body transform.",nameof(definition));
        foreach(Vector2 point in definition.Placement.PatrolPoints.Append(definition.Placement.WorldSpawn))
            if(!ResidentPatrolModel.Contains(definition.Placement.Territory,new Transform2D(0,point)*body.Transform*body.Shape.GetRect()))
                throw new ArgumentException("Complete body/path must fit its territory.",nameof(definition));
        _residentModel=model;_residence=definition.Placement;_residentBody=body;_residentCombatProfile=definition.CombatProfile;
    }

    private static void ValidateChargerCombatProfile(ResidentPopulationDefinition definition)
    {
        ResidentCombatProfile profile=definition.CombatProfile;
        if(definition.Family==ResidentFamily.Bone)
        {
            if(profile==null||profile.BoneWeakness!=ResidentBoneWeakness.RecoveryOrThreadExposed||
                profile.GhostHiddenSeconds!=0||profile.GhostManifestSeconds!=0)
                throw new ArgumentException("Bone charger needs an exclusive recovery/thread-exposure weakness profile.",nameof(definition));
        }
        else if(profile!=null)throw new ArgumentException("This charger cannot silently ignore another family's combat profile.",nameof(definition));
    }

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        // Captured callers may outlive discovery or queue an ancestor for deletion.
        // Source health and AI territory are deliberately not external-pull permissions.
        if(!LiveNodeChain(this)||!LiveNodeChain(interaction.Player)||
            !IsInstanceValid(_health)||_health.CurrentHealth<=0||_model==null||
            _residentModel!=null&&(_residentDormant||_residentVoidPending)||
            interaction.Player.GetTree()!=GetTree()||interaction.Player.GetWorld2D()!=GetWorld2D()||
            interaction.Settings==null||!float.IsFinite(interaction.Settings.EnemyPullSpeed)||interaction.Settings.EnemyPullSpeed<=0)return false;
        Vector2 direction=GlobalPosition.DirectionTo(interaction.Player.GlobalPosition);
        if(!Finite(direction))return false;
        ApplyThreadPull(direction*interaction.Settings.EnemyPullSpeed);
        return true;
    }
    private static bool LiveNodeChain(Node node)
    {
        if(!IsInstanceValid(node)||!node.IsInsideTree())return false;
        for(Node current=node;current!=null;current=current.GetParent())
            if(!IsInstanceValid(current)||current.IsQueuedForDeletion())return false;
        return true;
    }

    private void InitializeResidence()
    {
        if(_residentModel==null)return;
        _residentHurtbox=GetNode<Hurtbox>("Hurtbox");_residentHurtbox.SetDamageReceiverPath(new NodePath(".."));
        _residentHurtLayer=_residentHurtbox.CollisionLayer;_residentHurtMonitoring=_residentHurtbox.Monitoring;_residentHurtMonitorable=_residentHurtbox.Monitorable;
        _residentTarget=GetNode<ThreadTarget2D>("ThreadTarget");_residentTargetArea=_residentTarget.GetNode<Area2D>("TargetCollision");
        _residentTargetLayer=_residentTargetArea.CollisionLayer;_residentThreadGroup=_residentTarget.IsInGroup(ThreadTarget2D.TargetGroup);
        _residentGrappleGroup=_residentTarget.IsInGroup(GrappleSurface2D.TargetGroup);
        ApplyResidentDormancy();
    }
    public void SetResidentDormant(bool dormant)
    {
        if(_residentModel==null)throw new InvalidOperationException("Only configured residents can sleep.");
        if(_residentDormant==dormant)return;_residentDormant=dormant;
        if(IsInsideTree()&&_health!=null)
        {
            if(dormant)CancelResidentCombat();
            ApplyResidentDormancy();
        }
    }
    public bool ReceiveDamage(DamageRequest request)=>!_residentDormant&&!_residentVoidPending&&BonePoseAllowsDamage&&_health!=null&&_health.ReceiveDamage(request);
    private void ApplyResidentDormancy()
    {
        bool unavailable=_residentDormant||_residentVoidPending||_health.CurrentHealth<=0;
        _residentHurtbox.CollisionLayer=unavailable?0:_residentHurtLayer;
        _residentHurtbox.Monitoring=!unavailable&&_residentHurtMonitoring;
        // Both death and checkpoint reset can run during native AreaEntered.
        // Commit monitorability only after in/out emission, reading the current
        // life rather than queueing a stale value from the preceding life.
        Callable.From(CommitResidentMonitorable).CallDeferred();
        _residentTargetArea.CollisionLayer=unavailable?0:_residentTargetLayer;
        Group(ThreadTarget2D.TargetGroup,_residentThreadGroup);Group(GrappleSurface2D.TargetGroup,_residentGrappleGroup);
        if(unavailable)DisableDamage();
        void Group(string group,bool original)
        {
            if(unavailable){if(_residentTarget.IsInGroup(group))_residentTarget.RemoveFromGroup(group);}
            else if(original&&!_residentTarget.IsInGroup(group))_residentTarget.AddToGroup(group);
        }
    }
    private void CommitResidentMonitorable()
    {
        if(!IsInstanceValid(this)||!IsInsideTree()||!IsInstanceValid(_residentHurtbox)||!_residentHurtbox.IsInsideTree())return;
        _residentHurtbox.Monitorable=!_residentDormant&&!_residentVoidPending&&_health.CurrentHealth>0&&_residentHurtMonitorable;
    }
    private bool ObserveResidence(Node2D target,bool visible,float step)
    {
        if(_residentModel==null)return visible;
        bool allowed=visible&&ResidentTargetAllowed(target);
        ResidenceFrame=_residentModel.Step(new(GlobalPosition,ResidentBounds(),allowed,IsOnFloor(),
            CanResidentMove(-_residentSettings.Speed*step),CanResidentMove(_residentSettings.Speed*step),
            CurrentFrame.State==CanyonSkitterState.Stunned||_residentVoidPending,_residentDormant,_health.CurrentHealth<=0));
        return ResidenceFrame.CombatAllowed;
    }
    private bool ResidentTargetAllowed(Node2D target)=>!_residentDormant&&!_residentVoidPending&&_health.CurrentHealth>0&&
        GodotObject.IsInstanceValid(target)&&target.IsInsideTree()&&target.GetWorld2D()==GetWorld2D()&&
        ResidentPatrolModel.Contains(_residence.Territory,ResidentBounds())&&TryResidentTargetBounds(target,out Rect2 bounds)&&
        ResidentPatrolModel.Contains(_residence.Territory,bounds);
    private static bool TryResidentTargetBounds(Node2D target,out Rect2 bounds)
    {
        for(int i=0;i<target.GetChildCount();i++)
            if(target.GetChild(i) is CollisionShape2D {Disabled:false,Shape:not null} shape)
            {bounds=shape.GlobalTransform*shape.Shape.GetRect();return true;}
        bounds=default;return false;
    }
    private Rect2 ResidentBounds()=>_residentBody.GlobalTransform*_residentBody.Shape.GetRect();
    private void CancelResidentCombat()
    {
        _boneThreadRemaining=0;
        DisableDamage();
        CurrentFrame=_model.Step(new(GlobalPosition,GlobalPosition,false,false,HasGroundContact(),true,false),0);
        _lastState=CurrentFrame.State;_rushTravel=_retreatTravel=0;PublishFrame();
    }
    private void ApplyPendingResidentVoidDeath()
    {
        DisableDamage();
        if(CurrentFrame.State is not (CanyonSkitterState.Stunned or CanyonSkitterState.Dead))CancelResidentCombat();
        ApplyResidentDormancy();
        if(_health.CurrentHealth>0)_health.ApplyDamage(new(_health.CurrentHealth,Vector2.Zero,0,ulong.MaxValue,GetInstanceId()));
    }

    // Only autonomous intent is constrained; real external pull/gravity compose
    // afterwards. Continuous static rectangular top support, not two foot rays.
    private bool CanResidentMove(float dx)
    {
        if(!IsOnFloor()||!float.IsFinite(dx)||dx==0)return false;
        Rect2 body=ResidentBounds(),ahead=new(body.Position+new Vector2(dx+MathF.Sign(dx)*2,0),body.Size);
        if(ResidentPatrolModel.Contains(_residence.Territory,body))
        {if(!ResidentPatrolModel.Contains(_residence.Territory,ahead))return false;}
        else if(MathF.Abs(GlobalPosition.X+dx-_residence.WorldSpawn.X)>=MathF.Abs(GlobalPosition.X-_residence.WorldSpawn.X))return false;
        if(TestMove(GlobalTransform,new(dx,0),null,SafeMargin,false))return false;
        Rect2 sweep=body.Merge(ahead);
        using var strip=new RectangleShape2D{Size=new(sweep.Size.X,2.5f)};
        var exclusions=new Godot.Collections.Array<Rid>{GetRid()};using Godot.Collections.Array owner=(Godot.Collections.Array)exclusions;
        using var query=new PhysicsShapeQueryParameters2D{Shape=strip,Transform=new(0,new Vector2(sweep.GetCenter().X,body.End.Y)),
            CollisionMask=CombatCollisionLayers.World,CollideWithAreas=false,CollideWithBodies=true,Exclude=exclusions};
        using Godot.Collections.Array hits=(Godot.Collections.Array)GetWorld2D().DirectSpaceState.IntersectShape(query,64);
        if(hits.Count>=64)return false;var intervals=new List<Rect2>();
        for(int i=0;i<hits.Count;i++)
        {
            using Godot.Collections.Dictionary hit=hits[i].AsGodotDictionary();
            if(hit["collider"].AsGodotObject() is not StaticBody2D floor)continue;
            if(floor.ShapeOwnerGetOwner(floor.ShapeFindOwner(hit["shape"].AsInt32())) is not CollisionShape2D
                {Disabled:false,OneWayCollision:false,Shape:RectangleShape2D} shape)continue;
            Transform2D pose=shape.GlobalTransform;
            if(MathF.Abs(pose.X.Y)>.001f||MathF.Abs(pose.Y.X)>.001f||pose.X.X<=0||pose.Y.Y<=0)continue;
            Rect2 top=pose*shape.Shape.GetRect();if(MathF.Abs(top.Position.Y-body.End.Y)<=1.25f)intervals.Add(top);
        }
        float covered=sweep.Position.X;
        foreach(Rect2 interval in intervals.OrderBy(x=>x.Position.X))
        {
            if(interval.End.X<covered-.001f)continue;if(interval.Position.X>covered+.001f)break;
            covered=MathF.Max(covered,interval.End.X);
        }
        return covered>=sweep.End.X-.001f;
    }
}
