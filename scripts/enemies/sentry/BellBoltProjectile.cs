using System;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;

namespace Knight.Enemies.Sentry;

public partial class BellBoltProjectile : CharacterBody2D
{
    public event Action<BellBoltProjectile> Retired;

    public bool IsRetired { get; private set; }
    public float TravelledDistance { get; private set; }

    private Vector2 _direction;
    private float _speed;
    private float _maximumTravel;
    private float _radius;
    private Hitbox _hitbox;
    private Rect2? _residentArea;
    private Func<bool> _residentAvailable;
    private CircleShape2D _ownedBodyShape,_ownedHitShape;

    internal void CreateOwnedCollision(float radius,int damage)
    {
        if(IsInsideTree()||GetChildCount()!=0||_ownedBodyShape!=null||_ownedHitShape!=null)
            throw new InvalidOperationException("Create a bolt's owned collision once before mounting.");
        try
        {
            _ownedBodyShape=new CircleShape2D{Radius=radius};_ownedHitShape=new CircleShape2D{Radius=radius};
            AddChild(new CollisionShape2D{Name="BodyCollider",Shape=_ownedBodyShape});
            var hitbox=new Hitbox{Name="Hitbox",CollisionLayer=CombatCollisionLayers.EnemyAttack,CollisionMask=CombatCollisionLayers.Player,
                Damage=damage,Knockback=120f,GuardClass=DamageGuardClass.Normal,Monitoring=false,Monitorable=false};
            hitbox.AddChild(new CollisionShape2D{Name="HitCollider",Shape=_ownedHitShape});AddChild(hitbox);
        }
        catch{ReleaseOwnedCollision();throw;}
    }

    internal void ReleaseOwnedCollision()
    {
        // Only resources created above are ours. An externally constructed bolt
        // has null fields: never scan/Dispose its borrowed collider properties.
        CircleShape2D body=_ownedBodyShape,hit=_ownedHitShape;_ownedBodyShape=null;_ownedHitShape=null;
        if(IsInstanceValid(body))body.Dispose();if(IsInstanceValid(hit))hit.Dispose();
    }

    public override void _Ready()
    {
        _hitbox = GetNode<Hitbox>("Hitbox");
        _hitbox.Connected += OnConnected;
        SetPhysicsProcess(false);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_hitbox))
        {
            _hitbox.Connected -= OnConnected;
            _hitbox.End();
        }
        ReleaseOwnedCollision();
    }

    public void Launch(Vector2 worldPosition, Vector2 direction, ulong attackId,
        BellBoltSentrySettings settings)
    {
        if (settings == null || settings.Validate().Count > 0 || !Finite(worldPosition) ||
            !Finite(direction) || direction.LengthSquared() <= .000001f || attackId == 0)
        {
            Retire();
            return;
        }
        TopLevel = true;
        GlobalPosition = worldPosition;
        _direction = direction.Normalized();
        _speed = settings.BoltSpeed;
        _maximumTravel = settings.MaximumTravel;
        _radius = settings.BoltRadius;
        TravelledDistance = 0f;
        IsRetired = false;
        Visible = true;
        _hitbox.Damage = settings.Damage;
        _hitbox.GuardClass = DamageGuardClass.Normal;
        _hitbox.Begin(attackId);
        SetPhysicsProcess(true);
        QueueRedraw();
    }

    public void Retire()
    {
        if (IsRetired) return;
        IsRetired = true;
        SetPhysicsProcess(false);
        _hitbox?.SetDeferred(Area2D.PropertyName.Monitoring, false);
        Visible = false;
        CollisionShape2D body = GetNodeOrNull<CollisionShape2D>("BodyCollider");
        body?.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        Retired?.Invoke(this);
    }

    internal void SetResidentAuthority(Rect2 area,Func<bool> available)
    {
        ArgumentNullException.ThrowIfNull(available);
        _residentArea=area;_residentAvailable=available;
        // Logical revocation is synchronous even while physics Monitoring must
        // be changed deferred. Only opt-in resident bolts receive this filter.
        _hitbox.TargetFilter=_=>!IsRetired&&available()&&ResidentCircleFits();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsRetired || !double.IsFinite(delta) || delta <= 0d) return;
        if(_residentArea.HasValue&&(!_residentAvailable()||!ResidentCircleFits())){Retire();return;}
        float remaining = _maximumTravel - TravelledDistance;
        if (remaining <= .0001f)
        {
            Retire();
            return;
        }
        float distance = Mathf.Min(_speed * (float)delta, remaining);
        Vector2 motion = _direction * distance;
        bool territoryEnd=false;
        if(_residentArea is Rect2 area)
        {
            Vector2 wanted=GlobalPosition+motion;
            Vector2 bounded=RangedAttackGeometry.ClipToArea(GlobalPosition,wanted,area,_radius);
            territoryEnd=bounded.DistanceSquaredTo(wanted)>.000001f;motion=bounded-GlobalPosition;
        }
        KinematicCollision2D collision = MoveAndCollide(motion);
        if (collision != null)
        {
            TravelledDistance += collision.GetTravel().Length();
            Retire();
            return;
        }
        TravelledDistance += _residentArea.HasValue?motion.Length():distance;
        if (territoryEnd||TravelledDistance + .0001f >= _maximumTravel) Retire();
    }

    public override void _Draw()
    {
        if (IsRetired) return;
        DrawCircle(Vector2.Zero, _radius, new Color("f0b94a"));
        DrawArc(Vector2.Zero, _radius + 3f, 0f, Mathf.Tau, 24, new Color("d9574c"), 2f, true);
        DrawLine(-_direction * (_radius + 10f), -_direction * (_radius + 3f),
            new Color("f8dc84"), 4f, true);
    }

    private void OnConnected(Hurtbox hurtbox) => Retire();

    private bool ResidentCircleFits()=>_residentArea is not Rect2 area||
        GlobalPosition.X-_radius>=area.Position.X-.0001f&&GlobalPosition.Y-_radius>=area.Position.Y-.0001f&&
        GlobalPosition.X+_radius<=area.End.X+.0001f&&GlobalPosition.Y+_radius<=area.End.Y+.0001f;

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
