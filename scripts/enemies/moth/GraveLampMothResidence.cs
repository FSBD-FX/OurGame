using System;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Residents;
using Knight.Mechanisms;
using Knight.Prologue.Residents;

namespace Knight.Enemies.Moth;

// Explicit opt-in only; saved unconfigured wave actors retain their old policy.
public partial class GraveLampMoth : IResidentEnemyAgent,IDamageReceiver,IThreadInteractionReceiver
{
    private ResidentPlacementCandidate _airResidence;
    private readonly ResidentPatrolSettings _airSettings=new();
    private CollisionShape2D _airBody;
    private Hurtbox _airHurt;
    private ThreadTarget2D _airTarget;
    private Area2D _airTargetArea;
    private Node2D _airCombatTarget;
    private Node2D _airApproachTarget;
    private ResidentArchetype _airArchetype;
    private int _airApproachSide;
    private float _airAttackSpeed;
    private Func<Hurtbox,bool> _airOriginalFilter;
    private uint _airHurtLayer,_airTargetLayer;
    private bool _airHurtMonitoring,_airHurtMonitorable,_airThreadGroup,_airGrappleGroup;
    private bool _airDormant,_airReturning,_airDamageActive,_airVoidPending,_airDefeated;
    private int _airPoint;
    private ulong _airLifeVersion;
    private ResidentCombatProfile _airCombatProfile;
    private ResidentFamily _airCombatFamily;
    private bool _ghostAirManifest;
    private float _ghostAirRemaining;
    private Node2D _ghostAirVisual;
    private Color _ghostAirColor;
    private bool _boneAirThreadOpening;
    private bool HasAirGhostProfile=>_airCombatFamily==ResidentFamily.Ghost&&_airCombatProfile!=null;
    private bool GhostAirAllowsInteraction=>!HasAirGhostProfile||_ghostAirManifest;
    private bool HasAirBoneProfile=>_airCombatFamily==ResidentFamily.Bone&&_airCombatProfile?.BoneWeakness==ResidentBoneWeakness.RecoveryOrThreadStunned;
    private bool BoneAirPoseAllowsDamage=>!HasAirBoneProfile||CurrentState==GraveLampMothState.Recover||
        CurrentState==GraveLampMothState.ThreadStunned&&_boneAirThreadOpening;

    public void ConfigureResidence(ResidentPopulationDefinition definition)
    {
        if(IsInsideTree()||_airResidence!=null)throw new InvalidOperationException("Air residence must be configured once before mounting.");
        if(definition==null)throw new ArgumentNullException(nameof(definition));
        ResidentPlacementCandidate placement=definition.Placement;
        if(string.IsNullOrWhiteSpace(definition.StableId)||string.IsNullOrWhiteSpace(definition.SubtypeId)||
            !Enum.IsDefined(definition.Family)||!Enum.IsDefined(definition.RouteNode)||!Enum.IsDefined(definition.Sector)||
            definition.Archetype is not (ResidentArchetype.Flyer or ResidentArchetype.Diver)||placement==null||
            !AirFinite(placement.WorldSpawn)||!AirFinite(placement.Territory.Position)||!AirFinite(placement.Territory.Size)||
            placement.Territory.Size.X<=0||placement.Territory.Size.Y<=0||placement.PatrolPoints.Count<2||
            placement.PatrolPoints.Distinct().Count()<2||placement.PatrolPoints.Any(p=>!AirFinite(p)))
            throw new ArgumentException("Invalid resident air route.",nameof(definition));
        ValidateAirCombatProfile(definition);
        CollisionShape2D body=GetNode<CollisionShape2D>("BodyCollider");
        if(body.Disabled||body.TopLevel||body.Shape is not CircleShape2D||!AirFinite(body.Position)||
            MathF.Abs(body.Rotation)>.001f||body.Scale!=Vector2.One||MathF.Abs(body.Skew)>.001f||
            MathF.Abs(Rotation)>.001f||Scale!=Vector2.One||MathF.Abs(Skew)>.001f)
            throw new ArgumentException("Unsupported resident air body transform.",nameof(definition));
        foreach(Vector2 point in placement.PatrolPoints.Append(placement.WorldSpawn))
            if(!ResidentPatrolModel.Contains(placement.Territory,new Transform2D(0,point)*body.Transform*body.Shape.GetRect()))
                throw new ArgumentException("Complete air body/path must fit its territory.",nameof(definition));
        if(placement.PatrolPoints.All(p=>p.DistanceTo(placement.PatrolPoints[0])<=_airSettings.ArrivalTolerance*2))
            throw new ArgumentException("Air patrol requires distinct useful waypoints.",nameof(definition));
        _airBody=body;_airArchetype=definition.Archetype;_airResidence=placement;_airCombatProfile=definition.CombatProfile;_airCombatFamily=definition.Family;
    }

    private static void ValidateAirCombatProfile(ResidentPopulationDefinition definition)
    {
        ResidentCombatProfile profile=definition.CombatProfile;
        if(definition.Family==ResidentFamily.Bone)
        {
            if(definition.Archetype!=ResidentArchetype.Diver||profile==null||
                profile.BoneWeakness!=ResidentBoneWeakness.RecoveryOrThreadStunned||
                profile.GhostHiddenSeconds!=0||profile.GhostManifestSeconds!=0)
                throw new ArgumentException("Bone air resident needs an exclusive Diver recovery/thread-stun profile.",nameof(definition));
        }
        else if(definition.Family==ResidentFamily.Ghost)
        {
            if(profile==null||profile.BoneWeakness!=ResidentBoneWeakness.None||!Duration(profile.GhostHiddenSeconds)||!Duration(profile.GhostManifestSeconds))
                throw new ArgumentException("Ghost air resident needs an exclusive finite manifestation profile.",nameof(definition));
        }
        else if(profile!=null)throw new ArgumentException("This air resident cannot silently ignore another family's combat profile.",nameof(definition));
        static bool Duration(float value)=>float.IsFinite(value)&&value>=1f/60&&value<=60;
    }
    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if(!AirLive(this)||!AirLive(interaction.Player)||_model==null||!IsInstanceValid(_health)||_health.CurrentHealth<=0||
            !GhostAirAllowsInteraction||_airResidence!=null&&(_airDormant||_airVoidPending)||
            interaction.Player.GetTree()!=GetTree()||interaction.Player.GetWorld2D()!=GetWorld2D()||
            interaction.Settings==null||!float.IsFinite(interaction.Settings.EnemyPullSpeed)||interaction.Settings.EnemyPullSpeed<=0)return false;
        Vector2 direction=GlobalPosition.DirectionTo(interaction.Player.GlobalPosition);
        if(!AirFinite(direction))return false;
        // Keep the unconfigured and both native-role paths; bool acceptance must
        // not replace their original pull with a Bone-only implementation.
        if(_airResidence!=null)return PullAirResident(direction*interaction.Settings.EnemyPullSpeed);
        ApplyThreadPull(direction*interaction.Settings.EnemyPullSpeed);return true;
    }

    private GraveLampMothModel CreateAttackModel()
    {
        EnemyArchetypeSettings settings=Tuning.ToSettings();
        if(_airResidence!=null&&_airArchetype==ResidentArchetype.Flyer)
        {
            // A short side pass, rather than relabelling the legacy 147px dive.
            // The model, warning cast and native motion use this same speed.
            settings=settings with {DiveSpeed=MathF.Min(settings.DiveSpeed,96/settings.AttackSeconds),
                HorizontalAlignmentTolerance=MathF.Min(settings.HorizontalAlignmentTolerance,8),
                VerticalAlignmentTolerance=MathF.Min(settings.VerticalAlignmentTolerance,8)};
        }
        _airAttackSpeed=settings.DiveSpeed;
        return new GraveLampMothModel(settings);
    }
    private float AttackMotionSpeed=>_airResidence==null?Tuning.DiveSpeed:_airAttackSpeed;

    private Vector2 AirAlignmentPoint(Node2D player)
    {
        if(_airArchetype!=ResidentArchetype.Flyer)return player.GlobalPosition+Vector2.Up*Tuning.PreferredHoverHeight;
        if(!ReferenceEquals(_airApproachTarget,player))
        {
            _airApproachTarget=player;
            _airApproachSide=GlobalPosition.X<=player.GlobalPosition.X?-1:1;
        }
        // Keep the chosen side through this engagement; crossing the player
        // must not flip the approach every tick. Fit the complete saved body.
        Vector2 desired=player.GlobalPosition+new Vector2(_airApproachSide*80,-24);
        return GlobalPosition+ClipAirForecast(GlobalTransform,desired-GlobalPosition);
    }

    private void InitializeAirResidence()
    {
        if(_airResidence==null)return;
        _airHurt=GetNode<Hurtbox>("Hurtbox");_airHurt.SetDamageReceiverPath(new NodePath(".."));
        _airHurtLayer=_airHurt.CollisionLayer;_airHurtMonitoring=_airHurt.Monitoring;_airHurtMonitorable=_airHurt.Monitorable;
        _airTarget=GetNode<ThreadTarget2D>("ThreadTarget");_airTargetArea=_airTarget.GetNode<Area2D>("TargetCollision");
        _airTargetLayer=_airTargetArea.CollisionLayer;_airThreadGroup=_airTarget.IsInGroup(ThreadTarget2D.TargetGroup);
        _airGrappleGroup=_airTarget.IsInGroup(GrappleSurface2D.TargetGroup);
        _airOriginalFilter=_attackHitbox.TargetFilter;_attackHitbox.TargetFilter=AirCanHit;
        if(HasAirGhostProfile)
        {
            _ghostAirVisual=GetNodeOrNull<Node2D>("VisualRoot");if(IsInstanceValid(_ghostAirVisual))_ghostAirColor=_ghostAirVisual.Modulate;
            ResetGhostAirPhase();
        }
        ApplyAirAuthority();
    }
    private void ExitAirResidence()
    {
        _boneAirThreadOpening=false;
        if(_airResidence==null)return;
        _airDamageActive=false;_airVoidPending=false;_airCombatTarget=null;
        if(IsInstanceValid(_attackHitbox)){_attackHitbox.End();_attackHitbox.TargetFilter=_airOriginalFilter;}
        PublishAttack(default);
    }
    public bool ReceiveDamage(DamageRequest request)=>!_airDormant&&!_airVoidPending&&GhostAirAllowsInteraction&&BoneAirPoseAllowsDamage&&IsInstanceValid(_health)&&_health.ReceiveDamage(request);
    public void SetResidentDormant(bool dormant)
    {
        if(_airResidence==null)throw new InvalidOperationException("Only configured air residents can sleep.");
        if(_airDormant==dormant)return;_airDormant=dormant;
        if(dormant)_boneAirThreadOpening=false;
        if(IsInsideTree()&&IsInstanceValid(_health))
        {
            if(dormant)CancelAirAttack(true);
            ApplyAirAuthority();
        }
    }
    private void ApplyAirAuthority()
    {
        bool unavailable=_airDormant||_airVoidPending||_health.CurrentHealth<=0;
        bool targetUnavailable=unavailable||!GhostAirAllowsInteraction;
        _airHurt.CollisionLayer=unavailable?0:_airHurtLayer;_airHurt.Monitoring=!unavailable&&_airHurtMonitoring;
        // Native lethal contact and a same-signal reset both require this boundary.
        // Re-read current life; never enqueue a stale false/true from an old life.
        Callable.From(CommitAirMonitorable).CallDeferred();
        _airTargetArea.CollisionLayer=targetUnavailable?0:_airTargetLayer;
        Group(ThreadTarget2D.TargetGroup,_airThreadGroup);Group(GrappleSurface2D.TargetGroup,_airGrappleGroup);
        if(targetUnavailable){_airDamageActive=false;_attackHitbox.End();}
        void Group(string name,bool original)
        {
            if(targetUnavailable){if(_airTarget.IsInGroup(name))_airTarget.RemoveFromGroup(name);}
            else if(original&&!_airTarget.IsInGroup(name))_airTarget.AddToGroup(name);
        }
    }
    private void CommitAirMonitorable()
    {
        if(!AirLive(this)||!AirLive(_airHurt)||!IsInstanceValid(_health))return;
        _airHurt.Monitorable=!_airDormant&&!_airVoidPending&&_health.CurrentHealth>0&&_airHurtMonitorable;
    }
    private void CancelAirAttack(bool returning)
    {
        _airReturning|=returning;_airDamageActive=false;_attackHitbox.End();
        CurrentFrame=_model.CancelAttack();CurrentState=CurrentFrame.State;
        _airCombatTarget=null;_airApproachTarget=null;_airApproachSide=0;
        _attackTargetLocked=false;_lockedAttackTarget=Vector2.Zero;_lockedAttackStartTransform=default;
        _diveContactedWorld=false;PublishAttack(default);FrameChanged?.Invoke(CurrentFrame,Facing,IsOnFloor());
    }
    private void ResetAirResidence()
    {
        _boneAirThreadOpening=false;
        if(_airResidence==null)return;
        _airPoint=0;_airReturning=false;_airVoidPending=false;_airDefeated=false;_airDamageActive=false;_airCombatTarget=null;
        _airApproachTarget=null;_airApproachSide=0;ResetGhostAirPhase();ApplyAirAuthority();
    }

    private void ResetGhostAirPhase()
    {
        if(!HasAirGhostProfile)return;
        _ghostAirManifest=false;_ghostAirRemaining=_airCombatProfile.GhostHiddenSeconds;PresentGhostAirPhase();
    }
    private void StepGhostAirPhase(float step)
    {
        if(!HasAirGhostProfile||_airDormant||_airVoidPending||_health.CurrentHealth<=0)return;
        ulong life=_airLifeVersion;
        float cycle=_airCombatProfile.GhostHiddenSeconds+_airCombatProfile.GhostManifestSeconds;
        _ghostAirRemaining-=step%cycle;bool changed=false;
        while(_ghostAirRemaining<=0)
        {
            _ghostAirManifest=!_ghostAirManifest;
            _ghostAirRemaining+=_ghostAirManifest?_airCombatProfile.GhostManifestSeconds:_airCombatProfile.GhostHiddenSeconds;
            changed=true;
            if(!_ghostAirManifest)
            {
                CancelAirAttack(true);
                if(life!=_airLifeVersion||!AirLive(this)||_health.CurrentHealth<=0||_airDormant)return;
            }
        }
        if(changed){ApplyAirAuthority();PresentGhostAirPhase();}
    }
    private void PresentGhostAirPhase()
    {
        if(IsInstanceValid(_ghostAirVisual))_ghostAirVisual.Modulate=new(_ghostAirColor.R,_ghostAirColor.G,_ghostAirColor.B,_ghostAirColor.A*(_ghostAirManifest?1:.3f));
    }

    private void StepAirResidence(double delta)
    {
        if(!double.IsFinite(delta)||delta<=0||delta>.25)return;
        float step=(float)delta;
        ulong life=_airLifeVersion;
        if(_airVoidPending)ApplyPendingAirDeath();
        if(life!=_airLifeVersion||!AirLive(this))return;
        if(_health.CurrentHealth<=0){Velocity=Vector2.Zero;return;}
        StepGhostAirPhase(step);
        if(life!=_airLifeVersion||!AirLive(this)||_health.CurrentHealth<=0)return;
        if(!ResidentPatrolModel.Contains(_airResidence.Territory,AirBounds(GlobalTransform)))_airReturning=true;
        Node2D player=null;
        if(!_airDormant&&!_airVoidPending&&!_airReturning&&GhostAirAllowsInteraction)
            player=CurrentState is GraveLampMothState.Telegraph or GraveLampMothState.Dive
                ?AirTargetAllowed(_airCombatTarget)?_airCombatTarget:null:SelectAirTarget();
        bool permitted=player!=null;
        if(!permitted&&CurrentState is GraveLampMothState.Align or GraveLampMothState.Telegraph or GraveLampMothState.Dive)
            CancelAirAttack(true);
        if(life!=_airLifeVersion||!AirLive(this)||_health.CurrentHealth<=0)return;
        Vector2 targetPosition=_attackTargetLocked?_lockedAttackTarget:player?.GlobalPosition??Vector2.Zero;
        Vector2 offset=player==null?Vector2.Zero:targetPosition-GlobalPosition;
        Vector2 alignment=player==null?Vector2.Zero:AirAlignmentPoint(player)-GlobalPosition;
        bool contact=_diveContactedWorld;_diveContactedWorld=false;
        CurrentFrame=_model.Step(new(permitted,offset.X,offset.Y,alignment.X,alignment.Y,contact,_threadRequested,false),step,targetPosition);
        CurrentState=CurrentFrame.State;_threadRequested=false;
        if(CurrentState!=GraveLampMothState.ThreadStunned)_boneAirThreadOpening=false;
        if(CurrentState==GraveLampMothState.Telegraph&&CurrentFrame.EnteredState)_airCombatTarget=player;
        if(CurrentState==GraveLampMothState.Recover)_airReturning=true;
        AttackTimelineFrame timeline=EnrichTimeline(CurrentFrame.AttackTimeline,player,targetPosition);
        _airDamageActive=CurrentState==GraveLampMothState.Dive&&permitted&&!_airDormant&&!_airVoidPending&&GhostAirAllowsInteraction;
        if(_airDamageActive&&_lastAttackTimeline.Phase!=AttackPhase.Active)_attackHitbox.Begin(timeline.AttackId);
        else if(!_airDamageActive)_attackHitbox.End();

        Vector2 velocity=new(CurrentFrame.VelocityX,CurrentFrame.VelocityY);
        if(CurrentState==GraveLampMothState.Hover)velocity=AirPatrolVelocity(step);
        else if(CurrentState==GraveLampMothState.Align)velocity=SafeAirVelocity(velocity,step);
        if(_airDormant||_airVoidPending)velocity=Vector2.Zero;
        if(CurrentState==GraveLampMothState.ThreadStunned||_airVoidPending)
        {
            _threadVelocity.Y+=Tuning.Gravity*step;velocity=_threadVelocity;_threadVelocity.X*=Mathf.Exp(-2*step);
        }
        else _threadVelocity=Vector2.Zero;
        _damageImpulse=_damageImpulse.Lerp(Vector2.Zero,1-Mathf.Exp(-12*step));velocity+=_damageImpulse;
        if(MathF.Abs(velocity.X)>.01f)Facing=velocity.X<0?-1:1;
        Transform2D before=_airBody.GlobalTransform;Velocity=velocity;
        if(CurrentState==GraveLampMothState.Dive)
        {
            Vector2 motion=velocity*step;float remaining=GlobalPosition.DistanceTo(_lockedAttackTarget);
            if(_attackTargetLocked&&motion.Length()>remaining)motion=motion.LimitLength(remaining);
            using KinematicCollision2D collision=MoveAndCollide(motion,false,SafeMargin,false);
            if(collision!=null||motion.LengthSquared()<velocity.LengthSquared()*step*step-.0001f)
            {_diveContactedWorld=true;Velocity=Vector2.Zero;}
        }
        else MoveAndSlide();
        if(!_airVoidPending&&ResidentVoidContact.Crossed(this,_airBody,before))_airVoidPending=true;
        if(_airVoidPending){ApplyPendingAirDeath();timeline=default;}
        if(_health.CurrentHealth>0)PublishAttack(timeline);
        FrameChanged?.Invoke(CurrentFrame,Facing,IsOnFloor());
    }
    private Vector2 AirPatrolVelocity(float step)
    {
        if(_airDormant||_airVoidPending)return Vector2.Zero;
        Vector2 goal=_airReturning?_airResidence.WorldSpawn:_airResidence.PatrolPoints[_airPoint];
        if(GlobalPosition.DistanceTo(goal)<=_airSettings.ArrivalTolerance)
        {
            if(_airReturning){_airReturning=false;_airApproachTarget=null;_airApproachSide=0;return Vector2.Zero;}
            _airPoint=(_airPoint+1)%_airResidence.PatrolPoints.Count;goal=_airResidence.PatrolPoints[_airPoint];
        }
        Vector2 velocity=Toward(goal);
        Vector2 safe=SafeAirVelocity(velocity,step);
        if(safe.IsZeroApprox()&&!_airReturning)
        {_airPoint=(_airPoint+1)%_airResidence.PatrolPoints.Count;safe=SafeAirVelocity(Toward(_airResidence.PatrolPoints[_airPoint]),step);}
        return safe;
        Vector2 Toward(Vector2 point)
        {
            Vector2 gap=point-GlobalPosition;if(MathF.Abs(gap.X)<1)gap.X=0;if(MathF.Abs(gap.Y)<1)gap.Y=0;
            return gap.Normalized()*(_airReturning?Tuning.MoveSpeed:_airSettings.Speed);
        }
    }
    private Vector2 SafeAirVelocity(Vector2 velocity,float step)
    {
        if(CanMove(velocity))return velocity;
        Vector2 x=new(velocity.X,0),y=new(0,velocity.Y);
        if(!x.IsZeroApprox()&&CanMove(x))return x;if(!y.IsZeroApprox()&&CanMove(y))return y;return Vector2.Zero;
        bool CanMove(Vector2 v)
        {
            Vector2 motion=v*step;Rect2 current=AirBounds(GlobalTransform);Transform2D next=GlobalTransform;next.Origin+=motion;
            if(ResidentPatrolModel.Contains(_airResidence.Territory,current))
            {if(!ResidentPatrolModel.Contains(_airResidence.Territory,AirBounds(next)))return false;}
            else if((GlobalPosition+motion).DistanceSquaredTo(_airResidence.WorldSpawn)>=GlobalPosition.DistanceSquaredTo(_airResidence.WorldSpawn))return false;
            return !TestMove(GlobalTransform,motion,null,SafeMargin,false);
        }
    }
    private Rect2 AirBounds(Transform2D root)=>root*_airBody.Transform*_airBody.Shape.GetRect();
    private Vector2 ClipAirForecast(Transform2D root,Vector2 motion)
    {
        Rect2 body=AirBounds(root),area=_airResidence.Territory;float fraction=1;
        if(motion.X>0)fraction=MathF.Min(fraction,(area.End.X-body.End.X)/motion.X);
        else if(motion.X<0)fraction=MathF.Min(fraction,(area.Position.X-body.Position.X)/motion.X);
        if(motion.Y>0)fraction=MathF.Min(fraction,(area.End.Y-body.End.Y)/motion.Y);
        else if(motion.Y<0)fraction=MathF.Min(fraction,(area.Position.Y-body.Position.Y)/motion.Y);
        return motion*Mathf.Clamp(fraction,0,1);
    }
    private Node2D SelectAirTarget()
    {
        Node2D nearest=null;float distance=float.PositiveInfinity;
        using Godot.Collections.Array players=(Godot.Collections.Array)GetTree().GetNodesInGroup("player");
        for(int i=0;i<players.Count;i++)if(players[i].AsGodotObject() is Node2D player&&AirTargetAllowed(player))
        {float next=GlobalPosition.DistanceSquaredTo(player.GlobalPosition);if(next<distance){nearest=player;distance=next;}}
        return nearest;
    }
    private bool AirTargetAllowed(Node2D player)
    {
        if(_airDormant||_airVoidPending||!GhostAirAllowsInteraction||_health.CurrentHealth<=0||!AirLive(player)||player.GetWorld2D()!=GetWorld2D()||
            !player.IsInGroup("player")||player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not >0||
            GlobalPosition.DistanceTo(player.GlobalPosition)>Tuning.DetectionRange||
            !ResidentPatrolModel.Contains(_airResidence.Territory,AirBounds(GlobalTransform)))return false;
        CollisionShape2D collider=null;
        for(int i=0;i<player.GetChildCount();i++)if(player.GetChild(i) is CollisionShape2D {Disabled:false,Shape:not null} found){collider=found;break;}
        if(collider==null||!ResidentPatrolModel.Contains(_airResidence.Territory,collider.GlobalTransform*collider.Shape.GetRect()))return false;
        using var ray=PhysicsRayQueryParameters2D.Create(GlobalPosition,player.GlobalPosition,CombatCollisionLayers.World);
        ray.CollideWithAreas=false;ray.CollideWithBodies=true;
        using Godot.Collections.Dictionary hit=GetWorld2D().DirectSpaceState.IntersectRay(ray);return hit.Count==0;
    }
    private bool AirCanHit(Hurtbox hurt)
    {
        if(!_airDamageActive||_airDormant||_airVoidPending||!GhostAirAllowsInteraction||_health.CurrentHealth<=0||!AirLive(hurt)||_airOriginalFilter?.Invoke(hurt)==false)return false;
        for(Node node=hurt;node!=null;node=node.GetParent())if(node is Node2D player&&player.IsInGroup("player"))return ReferenceEquals(player,_airCombatTarget)&&AirTargetAllowed(player);
        return false;
    }
    private bool PullAirResident(Vector2 velocity)
    {
        if(_airDormant||_airVoidPending||!GhostAirAllowsInteraction||_health.CurrentHealth<=0||!AirFinite(velocity))return false;
        ulong life=_airLifeVersion;CancelAirAttack(true);
        if(life!=_airLifeVersion||!AirLive(this)||_airDormant||_airVoidPending||!GhostAirAllowsInteraction||_health.CurrentHealth<=0)return false;
        _threadVelocity=velocity.LimitLength(600);Velocity=_threadVelocity;_threadRequested=true;
        if(HasAirBoneProfile)_boneAirThreadOpening=true;
        return true;
    }
    private void HurtAirResident(Vector2 impulse)
    {
        if(_health.CurrentHealth<=0)return;
        CancelAirAttack(true);
        if(!impulse.IsZeroApprox()){_threadVelocity=Vector2.Zero;_threadRequested=true;}
    }
    private void DieAirResident()
    {
        _boneAirThreadOpening=false;
        _airVoidPending=false;_airDamageActive=false;_threadVelocity=_damageImpulse=Velocity=Vector2.Zero;
        _airCombatTarget=null;
        CurrentFrame=_model.Step(new(false,0,0,0,0,false,false,true),0);CurrentState=CurrentFrame.State;
        _attackTargetLocked=false;_lockedAttackTarget=Vector2.Zero;_lockedAttackStartTransform=default;
        ApplyAirAuthority();PublishAttack(default);FrameChanged?.Invoke(CurrentFrame,Facing,IsOnFloor());
    }
    private void ApplyPendingAirDeath()
    {
        _boneAirThreadOpening=false;
        ulong life=_airLifeVersion;CancelAirAttack(true);
        if(life!=_airLifeVersion||!AirLive(this)||!_airVoidPending)return;
        ApplyAirAuthority();
        if(_health.CurrentHealth>0)_health.ApplyDamage(new(_health.CurrentHealth,Vector2.Zero,0,ulong.MaxValue,GetInstanceId()));
    }
    private static bool AirFinite(Vector2 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y);
    private static bool AirLive(Node node)
    {
        if(!GodotObject.IsInstanceValid(node)||!node.IsInsideTree())return false;
        for(Node current=node;current!=null;current=current.GetParent())if(current.IsQueuedForDeletion())return false;return true;
    }
}
