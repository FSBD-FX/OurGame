using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue.Residents;

namespace Knight.Enemies.Residents;

// Shared permissions only. Each saved gun still owns its original combat model,
// native gravity, projectiles and ledger; this service never writes world pose.
public sealed class ResidentTurretResidence
{
    private readonly CharacterBody2D _actor;
    private readonly CollisionShape2D _body;
    private readonly Func<bool> _combatAllowed;
    private HealthComponent _health;
    private Hurtbox _hurtbox;
    private ThreadTarget2D _thread;
    private Area2D _targetArea;
    private uint _hurtLayer,_targetLayer;
    private bool _hurtMonitoring,_hurtMonitorable,_threadGroup,_grappleGroup;
    public Rect2 Territory {get;}
    public bool Dormant {get;private set;}
    public bool VoidPending {get;private set;}
    public Transform2D BodyPose=>_body.GlobalTransform;
    public bool Available=>!Dormant&&!VoidPending&&Live(_actor)&&GodotObject.IsInstanceValid(_health)&&_health.CurrentHealth>0;
    public bool CombatAvailable=>Available&&(_combatAllowed?.Invoke()??true);

    public ResidentTurretResidence(CharacterBody2D actor,ResidentPopulationDefinition definition,Func<bool> combatAllowed=null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ResidentPlacementCandidate p=definition.Placement;
        if(actor.IsInsideTree()||string.IsNullOrWhiteSpace(definition.StableId)||string.IsNullOrWhiteSpace(definition.SubtypeId)||
            !Enum.IsDefined(definition.Family)||!Enum.IsDefined(definition.RouteNode)||!Enum.IsDefined(definition.Sector)||definition.Archetype!=ResidentArchetype.Turret||
            p==null||!Finite(p.WorldSpawn)||!Finite(p.Territory.Position)||!Finite(p.Territory.Size)||!Finite(p.Territory.End)||
            p.Territory.Size.X<=0||p.Territory.Size.Y<=0||p.PatrolPoints.Count!=1||p.PatrolPoints[0]!=p.WorldSpawn)
            throw new ArgumentException("Turret requires a valid single fixed home and territory.",nameof(definition));
        CollisionShape2D body=actor.GetNode<CollisionShape2D>("BodyCollider");
        if(body.Disabled||body.TopLevel||body.Shape is not CapsuleShape2D||!Finite(body.Position)||
            MathF.Abs(body.Rotation)>.001f||body.Scale!=Vector2.One||MathF.Abs(body.Skew)>.001f||
            MathF.Abs(actor.Rotation)>.001f||actor.Scale!=Vector2.One||MathF.Abs(actor.Skew)>.001f||
            !ResidentPatrolModel.Contains(p.Territory,new Transform2D(0,p.WorldSpawn)*body.Transform*body.Shape.GetRect()))
            throw new ArgumentException("Complete turret body must fit its fixed territory.",nameof(definition));
        _actor=actor;_body=body;Territory=p.Territory;_combatAllowed=combatAllowed;
    }
    public void Initialize(HealthComponent health)
    {
        _health=health;_hurtbox=_actor.GetNode<Hurtbox>("Hurtbox");_thread=_actor.GetNode<ThreadTarget2D>("ThreadTarget");
        _targetArea=_thread.GetNode<Area2D>("TargetCollision");
        _hurtLayer=_hurtbox.CollisionLayer;_hurtMonitoring=_hurtbox.Monitoring;_hurtMonitorable=_hurtbox.Monitorable;_targetLayer=_targetArea.CollisionLayer;
        _threadGroup=_thread.IsInGroup(ThreadTarget2D.TargetGroup);_grappleGroup=_thread.IsInGroup(GrappleSurface2D.TargetGroup);
        _hurtbox.SetDamageReceiverPath(new NodePath(".."));ApplyAuthority();
    }
    public bool SetDormant(bool dormant)
    {
        if(Dormant==dormant)return false;Dormant=dormant;return true;
    }
    public bool ObserveVoidContact(Transform2D before)
    {
        // Sleeping does not suspend gravity or make real environment contact
        // harmless. Once latched, source lifetime no longer owns this death.
        if(!VoidPending&&_health.CurrentHealth>0&&ResidentVoidContact.Crossed(_actor,_body,before))VoidPending=true;
        if(VoidPending)ApplyAuthority();return VoidPending;
    }
    public void ApplyPendingVoidDeath()
    {
        if(VoidPending&&Live(_actor)&&_health.CurrentHealth>0)
            _health.ApplyDamage(new(_health.CurrentHealth,Vector2.Zero,0,ulong.MaxValue,_actor.GetInstanceId()));
        // Health may synchronously publish death/reset. No old-life writes here.
    }
    public void ResetForCheckpoint()
    {
        VoidPending=false;Dormant=false;ApplyAuthority();
    }
    public void ApplyAuthority()
    {
        if(!GodotObject.IsInstanceValid(_health)||!GodotObject.IsInstanceValid(_hurtbox))return;
        bool enabled=Available,targetEnabled=CombatAvailable;
        _hurtbox.CollisionLayer=enabled?_hurtLayer:0;_hurtbox.Monitoring=enabled&&_hurtMonitoring;
        Callable.From(CommitMonitorable).CallDeferred();_targetArea.CollisionLayer=targetEnabled?_targetLayer:0;
        Group(ThreadTarget2D.TargetGroup,_threadGroup);Group(GrappleSurface2D.TargetGroup,_grappleGroup);
        void Group(string name,bool original)
        {
            if(!targetEnabled){if(_thread.IsInGroup(name))_thread.RemoveFromGroup(name);}
            else if(original&&!_thread.IsInGroup(name))_thread.AddToGroup(name);
        }
    }
    private void CommitMonitorable()
    {
        if(Live(_actor)&&Live(_hurtbox))_hurtbox.Monitorable=Available&&_hurtMonitorable;
    }
    public bool ReceiveDamage(DamageRequest request)=>CombatAvailable&&_health.ReceiveDamage(request);
    public Node2D SelectTarget(float range,Vector2 muzzle)
    {
        if(!CombatAvailable)return null;Node2D nearest=null;float distance=float.PositiveInfinity;
        using Godot.Collections.Array players=(Godot.Collections.Array)_actor.GetTree().GetNodesInGroup("player");
        for(int i=0;i<players.Count;i++)if(players[i].AsGodotObject() is Node2D player&&TargetAllowed(player,range,muzzle))
        {float next=_actor.GlobalPosition.DistanceSquaredTo(player.GlobalPosition);if(next<distance){nearest=player;distance=next;}}
        return nearest;
    }
    public bool TargetAllowed(Node2D player,float range,Vector2 muzzle)
    {
        if(!CombatAvailable||!Live(player)||player.GetWorld2D()!=_actor.GetWorld2D()||!player.IsInGroup("player")||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not >0||!Finite(muzzle)||
            muzzle.DistanceTo(player.GlobalPosition)>range||!ResidentPatrolModel.Contains(Territory,_body.GlobalTransform*_body.Shape.GetRect())||
            !_actor.GetViewport().GetVisibleRect().HasPoint(_actor.GetNode<Node2D>("Muzzle").GetGlobalTransformWithCanvas().Origin))return false;
        CollisionShape2D body=null;
        for(int i=0;i<player.GetChildCount();i++)if(player.GetChild(i) is CollisionShape2D {Disabled:false,Shape:not null} found){body=found;break;}
        if(body==null||!ResidentPatrolModel.Contains(Territory,body.GlobalTransform*body.Shape.GetRect()))return false;
        var excludes=new Godot.Collections.Array<Rid>{_actor.GetRid()};using Godot.Collections.Array backing=(Godot.Collections.Array)excludes;
        using var query=PhysicsRayQueryParameters2D.Create(muzzle,player.GlobalPosition,CombatCollisionLayers.World);
        query.CollideWithAreas=false;query.CollideWithBodies=true;query.HitFromInside=true;query.Exclude=excludes;
        using Godot.Collections.Dictionary hit=_actor.GetWorld2D().DirectSpaceState.IntersectRay(query);return hit.Count==0;
    }
    private static bool Finite(Vector2 v)=>float.IsFinite(v.X)&&float.IsFinite(v.Y);
    private static bool Live(Node node)
    {
        if(!GodotObject.IsInstanceValid(node)||!node.IsInsideTree())return false;
        for(Node n=node;n!=null;n=n.GetParent())if(n.IsQueuedForDeletion())return false;return true;
    }
}
