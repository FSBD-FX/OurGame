using System;
using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Enemies.Ranged;

public partial class RangedWeaponProjectile : Node2D
{
    public bool IsRetired { get; private set; }
    public float TravelledDistance { get; private set; }
    public AttackTelegraphVisual Telegraph { get; private set; }
    public event Action<RangedWeaponProjectile> Retired;
    public event Action<Vector2, float> ExplosionResolved;

    private Node2D _owner;
    private Node2D _target;
    private HealthComponent _ownerHealth;
    private RangedVolleyDamageLedger _ledger;
    private Godot.Collections.Array<Rid> _exclude;
    private Godot.Collections.Array _excludeOwner;
    private DamageAttackIdentity _identity;
    private ProjectileSettings _settings;
    private Rect2 _allowedWorldArea;
    private Vector2 _direction;
    private double _age;
    private float _absoluteTurn;
    private float _presentationDuration;
    private bool _launched;
    private bool _guidanceLost;
    private bool _exploded;

    public override void _Ready()
    {
        if (_launched) return;
        Visible = false;
        SetPhysicsProcess(false);
    }

    public override void _ExitTree() => Retire();

    public void Launch(Node2D owner, Vector2 origin, Vector2 direction,
        DamageAttackIdentity identity, RangedWeaponSettings settings,
        RangedVolleyDamageLedger ledger, Rect2 allowedWorldArea, Node2D target = null)
    {
        // A projectile is a single-use owned leaf. In particular, relaunching a
        // retired pellet must not silently renew a consumed volley identity.
        if (_launched || IsRetired || !GodotObject.IsInstanceValid(owner) || !owner.IsInsideTree() ||
            owner.IsQueuedForDeletion() || GetParent() != owner || !IsInsideTree() ||
            !GodotObject.IsInstanceValid(settings) || settings.Validate().Count != 0 || ledger == null ||
            identity.SourceRuntimeId == 0 || identity.LocalAttackId == 0 || !Finite(origin) ||
            !TryNormalize(direction, out Vector2 normalized) || !ValidArea(allowedWorldArea) ||
            settings.Role == RangedWeaponRole.Laser)
        {
            Retire();
            return;
        }

        // Numeric snapshot only: neither a mutable Resource nor a duplicate of
        // one is retained. These are configurable limits; .4/75/20/3 are defaults.
        _settings = new ProjectileSettings(settings.Role, settings.ProjectileSpeed, settings.MaximumTravel,
            settings.ProjectileRadius, settings.ExplosionRadius, settings.MissileGuidanceSeconds,
            Mathf.DegToRad(settings.MissileTurnRateDegrees), Mathf.DegToRad(settings.MissileMaximumTurnDegrees),
            settings.MissileMaximumAgeSeconds, settings.Damage);
        _allowedWorldArea = allowedWorldArea;
        float requiredInset = _settings.Role == RangedWeaponRole.Missile
            ? Mathf.Max(_settings.Radius, _settings.ExplosionRadius) : _settings.Radius;
        if (!ContainsCircle(origin, requiredInset))
        {
            Retire();
            return;
        }

        _owner = owner;
        _ownerHealth = owner.GetNodeOrNull<HealthComponent>("Health");
        if (!OwnerAlive())
        {
            Retire();
            return;
        }
        _owner.TreeExiting += OnOwnerExiting;
        if (GodotObject.IsInstanceValid(_ownerHealth)) _ownerHealth.Died += OnOwnerDied;
        _target = target;
        _ledger = ledger;
        _identity = identity;
        _direction = normalized;
        _exclude = new Godot.Collections.Array<Rid>();
        _excludeOwner = (Godot.Collections.Array)_exclude;
        AddOwnedCollisionExclusions(owner);
        _launched = true;
        TopLevel = true;
        GlobalPosition = origin;
        Visible = true;
        double flightSeconds = (double)_settings.MaximumTravel / _settings.Speed;
        if (_settings.Role == RangedWeaponRole.Missile)
            flightSeconds = Math.Min(flightSeconds, _settings.MaximumAge);
        _presentationDuration = (float)Math.Clamp(flightSeconds, (double)float.Epsilon, float.MaxValue);
        Telegraph = new AttackTelegraphVisual { Name = "AttackTelegraph" };
        AddChild(Telegraph);
        PresentActive(_settings.Radius);

        // CastMotion alone does not report an already-overlapping start. The
        // geometry contract explicitly checks it, including zero-motion probes.
        RangedSweepResult start = RangedAttackGeometry.Sweep(GetWorld2D().DirectSpaceState,
            origin, Vector2.Zero, _settings.Radius, _exclude);
        if (start.StartedInWorld)
        {
            Retire();
            return;
        }
        SetPhysicsProcess(true);
        QueueRedraw();
    }

    public void Retire()
    {
        if (IsRetired) return;
        IsRetired = true;
        SetPhysicsProcess(false);
        Visible = false;
        if (GodotObject.IsInstanceValid(Telegraph)) Telegraph.Present(default);
        if (GodotObject.IsInstanceValid(_owner)) _owner.TreeExiting -= OnOwnerExiting;
        if (GodotObject.IsInstanceValid(_ownerHealth)) _ownerHealth.Died -= OnOwnerDied;
        // This array is ours; its RID entries and the direct space state are
        // borrowed. No physics body, query result target or shared shape is freed.
        _excludeOwner?.Dispose();
        _excludeOwner = null;
        _exclude = null;
        _target = null;
        _ownerHealth = null;
        _owner = null;
        _ledger = null;
        Retired?.Invoke(this); // The actor, not this leaf, owns QueueFree and ledger release.
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_launched || IsRetired) return;
        if (!OwnerAlive())
        {
            Retire();
            return;
        }
        if (GetTree().Paused || !double.IsFinite(delta) || delta <= 0d || delta > float.MaxValue) return;

        double seconds = delta;
        if (_settings.Role == RangedWeaponRole.Missile)
            seconds = Math.Min(seconds, Math.Max(0d, _settings.MaximumAge - _age));
        float remaining = Mathf.Max(0f, _settings.MaximumTravel - TravelledDistance);
        if (remaining <= .00001f || seconds <= 0d)
        {
            FinishFlight();
            return;
        }

        PhysicsDirectSpaceState2D space = GetWorld2D().DirectSpaceState;
        RangedSweepResult atStart = RangedAttackGeometry.Sweep(space, GlobalPosition, Vector2.Zero,
            _settings.Radius, _exclude);
        if (atStart.StartedInWorld)
        {
            Retire();
            return;
        }
        Guide(space, seconds);
        float distance = (float)Math.Min((double)_settings.Speed * seconds, remaining);
        Vector2 from = GlobalPosition;
        Vector2 desiredEnd = from + _direction * distance;
        if (!Finite(desiredEnd))
        {
            Retire();
            return;
        }
        float inset = _settings.Role == RangedWeaponRole.Missile
            ? Mathf.Max(_settings.Radius, _settings.ExplosionRadius) : _settings.Radius;
        Vector2 end = RangedAttackGeometry.ClipToArea(from, desiredEnd, _allowedWorldArea, inset);
        if (!Finite(end) || !ContainsCircle(end, inset))
        {
            Retire();
            return;
        }
        bool areaBoundary = end.DistanceSquaredTo(desiredEnd) > .000001f;
        Vector2 motion = end - from;
        RangedSweepResult hit = RangedAttackGeometry.Sweep(space, from, motion, _settings.Radius, _exclude);
        if (hit.StartedInWorld || !float.IsFinite(hit.SafeFraction))
        {
            Retire();
            return;
        }

        GlobalPosition = from + motion * Mathf.Clamp(hit.SafeFraction, 0f, 1f);
        TravelledDistance = Mathf.Min(_settings.MaximumTravel,
            TravelledDistance + GlobalPosition.DistanceTo(from));
        _age += seconds;
        PresentActive(_settings.Radius);
        QueueRedraw();
        if (hit.WorldHit || GodotObject.IsInstanceValid(hit.Target))
        {
            if (_settings.Role == RangedWeaponRole.Missile) FinishFlight();
            else
            {
                if (!hit.WorldHit && GodotObject.IsInstanceValid(hit.Target))
                    _ledger.TryDeliver(hit.Target, Request(_direction));
                Retire();
            }
            return;
        }
        if (areaBoundary || TravelledDistance + .00001f >= _settings.MaximumTravel ||
            (_settings.Role == RangedWeaponRole.Missile && _age + .0000001d >= _settings.MaximumAge))
            FinishFlight();
    }

    private void Guide(PhysicsDirectSpaceState2D space, double seconds)
    {
        if (_settings.Role != RangedWeaponRole.Missile || _guidanceLost ||
            _age >= _settings.GuidanceSeconds || _absoluteTurn >= _settings.MaximumTurn) return;
        if (!GodotObject.IsInstanceValid(_target) || !_target.IsInsideTree() || _target.IsQueuedForDeletion() ||
            !Finite(_target.GlobalPosition))
        {
            _guidanceLost = true;
            return;
        }
        HealthComponent targetHealth = _target.GetNodeOrNull<HealthComponent>("Health");
        if ((GodotObject.IsInstanceValid(targetHealth) && targetHealth.CurrentHealth <= 0) ||
            !RangedAttackGeometry.HasClearWorldPath(space, GlobalPosition, _target.GlobalPosition, _exclude))
        {
            _guidanceLost = true;
            return;
        }
        if (!TryNormalize(_target.GlobalPosition - GlobalPosition, out Vector2 desired)) return;
        double guidedSeconds = Math.Min(seconds, _settings.GuidanceSeconds - _age);
        float available = (float)Math.Min((double)_settings.TurnRate * guidedSeconds,
            Math.Max(0f, _settings.MaximumTurn - _absoluteTurn));
        float turn = Mathf.Clamp(_direction.AngleTo(desired), -available, available);
        _direction = _direction.Rotated(turn).Normalized();
        _absoluteTurn = Mathf.Min(_settings.MaximumTurn, _absoluteTurn + Mathf.Abs(turn));
    }

    private void FinishFlight()
    {
        if (IsRetired) return;
        if (_settings.Role == RangedWeaponRole.Missile && !_exploded && OwnerAlive() &&
            ContainsCircle(GlobalPosition, _settings.ExplosionRadius))
        {
            _exploded = true;
            // During real damage callbacks the authorized leaf describes the
            // actual blast, not the smaller flight circle or the owner's pose.
            PresentActive(_settings.ExplosionRadius);
            Vector2 center = GlobalPosition;
            ExplosionResolved?.Invoke(center, _settings.ExplosionRadius);
            // Presentation subscribers may synchronously revoke the owner or
            // this leaf. A cosmetic notification never renews damage authority.
            if (IsRetired || IsQueuedForDeletion() || !OwnerAlive())
            {
                Retire();
                return;
            }
            Hurtbox[] targets = RangedAttackGeometry.ExplosionTargets(GetWorld2D().DirectSpaceState,
                center, _settings.ExplosionRadius, _exclude);
            foreach (Hurtbox target in targets)
            {
                if (IsRetired || IsQueuedForDeletion() || !OwnerAlive()) break;
                if (!GodotObject.IsInstanceValid(target) || target.IsQueuedForDeletion() || !target.IsInsideTree())
                    continue;
                Vector2 direction = TryNormalize(target.GlobalPosition - center, out Vector2 radial)
                    ? radial : _direction;
                _ledger.TryDeliver(target, Request(direction));
            }
        }
        Retire();
    }

    private DamageRequest Request(Vector2 direction) => new(_settings.Damage, direction, 120f,
        _identity.LocalAttackId, _identity.SourceRuntimeId, DamageGuardClass.Normal);

    private void PresentActive(float radius)
    {
        if (!GodotObject.IsInstanceValid(Telegraph) || IsRetired) return;
        EnemyAttackKind kind = _settings.Role switch
        {
            RangedWeaponRole.Scatter => EnemyAttackKind.RivetScatterShot,
            RangedWeaponRole.Burst => EnemyAttackKind.BeltBurstShot,
            RangedWeaponRole.Missile => EnemyAttackKind.FurnaceMissileShot,
            _ => EnemyAttackKind.None
        };
        Telegraph.Present(new AttackPresentationFrame(
            new AttackTimelineFrame(_identity.LocalAttackId, kind, AttackPhase.Active,
                _presentationDuration, (float)Math.Max(0d, _presentationDuration - _age),
                GlobalPosition, true),
            new AttackShapeFrame(AttackShapeKind.Circle, GlobalPosition, Vector2.One * radius,
                0f, Array.Empty<Vector2>())));
    }

    public override void _Draw()
    {
        if (!_launched || IsRetired) return;
        if (_settings.Role == RangedWeaponRole.Missile)
        {
            Vector2 side = _direction.Orthogonal() * _settings.Radius;
            DrawColoredPolygon([_direction * (_settings.Radius + 7f),
                -_direction * _settings.Radius + side, -_direction * _settings.Radius - side], new Color("d17749"));
            DrawLine(-_direction * (_settings.Radius + 11f), -_direction * _settings.Radius,
                new Color("f7bd6c"), 3f, true);
        }
        else
        {
            DrawCircle(Vector2.Zero, _settings.Radius, new Color("f0b94a"));
            DrawLine(-_direction * (_settings.Radius + 8f), -_direction * _settings.Radius,
                new Color("f8dc84"), 3f, true);
        }
    }

    private bool OwnerAlive() => GodotObject.IsInstanceValid(_owner) && _owner.IsInsideTree() &&
        !_owner.IsQueuedForDeletion() &&
        (!GodotObject.IsInstanceValid(_ownerHealth) || _ownerHealth.CurrentHealth > 0);

    private void OnOwnerDied() => Retire();
    private void OnOwnerExiting() => Retire();

    private void AddOwnedCollisionExclusions(Node node)
    {
        if (node is CollisionObject2D collision) _exclude.Add(collision.GetRid());
        foreach (Node child in node.GetChildren()) AddOwnedCollisionExclusions(child);
    }

    private bool ContainsCircle(Vector2 center, float radius) =>
        (double)center.X - radius >= _allowedWorldArea.Position.X &&
        (double)center.Y - radius >= _allowedWorldArea.Position.Y &&
        (double)center.X + radius <= (double)_allowedWorldArea.Position.X + _allowedWorldArea.Size.X &&
        (double)center.Y + radius <= (double)_allowedWorldArea.Position.Y + _allowedWorldArea.Size.Y;

    private static bool ValidArea(Rect2 area) => Finite(area.Position) && Finite(area.Size) &&
        area.Size.X > 0f && area.Size.Y > 0f &&
        (double)area.Position.X + area.Size.X <= float.MaxValue &&
        (double)area.Position.Y + area.Size.Y <= float.MaxValue;

    private static bool TryNormalize(Vector2 value, out Vector2 normalized)
    {
        normalized = Vector2.Zero;
        if (!Finite(value)) return false;
        double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
        if (!double.IsFinite(length) || length <= .000001d) return false;
        normalized = new Vector2((float)(value.X / length), (float)(value.Y / length));
        return Finite(normalized);
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private readonly record struct ProjectileSettings(RangedWeaponRole Role, float Speed, float MaximumTravel,
        float Radius, float ExplosionRadius, float GuidanceSeconds, float TurnRate, float MaximumTurn,
        float MaximumAge, int Damage);
}
