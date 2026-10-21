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

namespace Knight.Enemies.Ranged;

public abstract partial class RangedWeaponEnemy : CharacterBody2D, IThreadPullReceiver, IEncounterActor, IResidentEnemyAgent, IDamageReceiver
{
    public abstract RangedWeaponRole Role { get; }
    [Export] public RangedWeaponSettings Settings { get; set; }

    public event Action Defeated;
    public event Action<AttackPresentationFrame> AttackFrameChanged;
    public event Action<PresentationCue> PresentationCueRequested;
    public event Action<RangedWeaponFrame> FrameChanged;
    public event Action<ulong, Vector2> ShotEmitted;

    public RangedWeaponFrame CurrentFrame { get; private set; } =
        new(RangedWeaponState.Idle, Vector2.Zero, default, -1, 0);

    // Presentation consumers need synchronous revocation authority; the last
    // published frame may still be Active until the next native physics tick.
    internal bool HasPresentationAuthority => CanOwnAttack();

    private readonly RangedVolleyDamageLedger _ledger = new();
    private readonly Dictionary<RangedWeaponProjectile, DamageAttackIdentity> _projectiles = new();
    private readonly Dictionary<DamageAttackIdentity, int> _identityUsers = new();
    private RangedWeaponSettings _runtimeSettings;
    private RangedWeaponModel _model;
    private HealthComponent _health;
    private Node2D _muzzle;
    private AttackTelegraphVisual _telegraph;
    private CapsuleShape2D _beamCollision;
    private Godot.Collections.Array<Rid> _ownerExclude;
    private Godot.Collections.Array _ownerExcludeBacking;
    private Rect2 _worldArena;
    private bool _arenaConfigured;
    private bool _threadRequested;
    private bool _revoking;
    private bool _defeatedEmitted;
    private bool _spawnCaptured;
    private Vector2 _spawnPosition;
    private Vector2 _beamOrigin;
    private Vector2 _beamDirection;
    private DamageAttackIdentity _beamIdentity;
    private ulong _damageOrdinal;
    private ResidentTurretResidence _residence;
    private Node2D _residentTarget;

    public override void _Ready()
    {
        // Exported Resources are borrowed. All mounted decisions and projectile
        // inputs use this owned duplicate, never a mutable inspector resource.
        RangedWeaponSettings source = Settings;
        bool temporary = source == null;
        source ??= RangedWeaponSettings.ForRole(Role);
        try
        {
            if (source.Role != Role || source.Validate().Count != 0)
                throw new InvalidOperationException("Invalid ranged settings for " + Role);
            _runtimeSettings = (RangedWeaponSettings)source.Duplicate();
        }
        finally { if (temporary) source.Dispose(); }
        _model = new RangedWeaponModel(_runtimeSettings);
        _health = GetNode<HealthComponent>("Health");
        _muzzle = GetNode<Node2D>("Muzzle");
        _telegraph = GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        _beamCollision = new CapsuleShape2D();
        _ownerExclude = new Godot.Collections.Array<Rid> { GetRid() };
        _ownerExcludeBacking = (Godot.Collections.Array)_ownerExclude;
        _health.Damaged += OnDamaged;
        _health.Died += OnDied;
        _residence?.Initialize(_health);
        CallDeferred(nameof(CaptureSpawnPosition));
    }

    public override void _ExitTree()
    {
        RevokeAttacks();
        if (IsInstanceValid(_health))
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }
        _beamCollision?.Dispose();
        _beamCollision = null;
        _runtimeSettings?.Dispose();
        _runtimeSettings = null;
        _ownerExcludeBacking?.Dispose();
        _ownerExcludeBacking = null;
        _ownerExclude = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_model == null || _runtimeSettings == null) return;
        if (IsQueuedForDeletion()) { RevokeAttacks(); return; }
        if(_residence is {VoidPending:true}){CancelResidentAttack();_residence.ApplyPendingVoidDeath();}
        CaptureSpawnPosition();
        float step = NormalizeStep(delta, _runtimeSettings.Gravity);
        Vector2 origin = _muzzle.GlobalPosition;
        Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        if(_residence!=null)
        {
            bool committed=CurrentFrame.AttackTimeline.Phase!=AttackPhase.None&&_residentTarget!=null;
            player=committed?_residence.TargetAllowed(_residentTarget,_runtimeSettings.DetectionRange,origin)?_residentTarget:null:
                _residence.SelectTarget(_runtimeSettings.DetectionRange,origin);
            if(!_residence.Available||committed&&player==null){CancelResidentAttack();player=null;}
        }
        Vector2 target = IsInstanceValid(player) ? player.GlobalPosition : origin;
        bool alive = IsInstanceValid(player) && !player.IsQueuedForDeletion() &&
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth > 0;
        bool visible = _arenaConfigured && _worldArena.HasPoint(origin) && alive &&
            MuzzleIsInViewport() && RangedAttackGeometry.HasClearWorldPath(
                GetWorld2D().DirectSpaceState, origin, target, ExcludedOwner());
        CurrentFrame = _model.Step(new RangedWeaponInput(origin, target, visible,
            _threadRequested, _health.CurrentHealth <= 0), step);
        _threadRequested = false;
        if(_residence!=null&&CurrentFrame.State==RangedWeaponState.Telegraph)_residentTarget??=player;

        // Recovery can contain the two tail events after an Active-phase hitch.
        // Consume the interval before any state-based cancellation/presentation.
        for (int eventIndex = 0; eventIndex < CurrentFrame.EmissionsDue; eventIndex++)
        {
            if (!CanOwnAttack()) break;
            EmitAttack(player);
        }
        if (!CanOwnAttack())
        {
            if (_health.CurrentHealth <= 0) CurrentFrame = _model.Step(new RangedWeaponInput(
                origin, target, false, false, true), 0d);
            RevokeAttacks();
        }
        else if (Role == RangedWeaponRole.Laser)
        {
            if (CurrentFrame.State == RangedWeaponState.Active) PresentAndDamageBeam();
            else ReleaseBeam();
        }
        if (IsQueuedForDeletion() || !IsInsideTree()) return;
        PublishAttack();
        FrameChanged?.Invoke(CurrentFrame);
        if (CurrentFrame.State == RangedWeaponState.Dead)
        {
            Velocity = Vector2.Zero;
            return;
        }
        float vertical = float.IsFinite(Velocity.Y) ? Velocity.Y : 0f;
        float nextVertical = vertical + _runtimeSettings.Gravity * step;
        Velocity = new Vector2(0f, float.IsFinite(nextVertical) ? nextVertical : vertical);
        Transform2D before=_residence?.BodyPose??default;
        MoveAndSlide();
        if(_residence?.ObserveVoidContact(before)==true){CancelResidentAttack();_residence.ApplyPendingVoidDeath();}
    }

    public void ConfigureEncounterEnvelope(Rect2 worldArena)
    {
        if (IsInsideTree()) throw new InvalidOperationException("Configure ranged arena before AddChild.");
        _worldArena = worldArena;
        _arenaConfigured = Finite(worldArena.Position) && Finite(worldArena.Size) &&
            worldArena.Size.X > 0f && worldArena.Size.Y > 0f && Finite(worldArena.End);
    }

    public void ConfigureResidence(ResidentPopulationDefinition definition)
    {
        if(IsInsideTree()||_residence!=null)throw new InvalidOperationException("Configure turret residence once before mounting.");
        var residence=new ResidentTurretResidence(this,definition);
        Rect2 arena=_arenaConfigured?_worldArena.Intersection(residence.Territory):residence.Territory;
        if(arena.Size.X<=0||arena.Size.Y<=0)throw new ArgumentException("Turret arena and territory do not overlap.",nameof(definition));
        _residence=residence;_worldArena=arena;_arenaConfigured=true;
    }
    public void SetResidentDormant(bool dormant)
    {
        if(_residence==null)throw new InvalidOperationException("Only configured turret residents can sleep.");
        if(!_residence.SetDormant(dormant))return;
        if(_model!=null&&dormant)CancelResidentAttack();_residence.ApplyAuthority();
    }
    public bool ReceiveDamage(DamageRequest request)=>_residence!=null?_residence.ReceiveDamage(request):_health?.ReceiveDamage(request)??false;
    private void CancelResidentAttack()
    {
        CurrentFrame=_model.CancelAttack();_residentTarget=null;_threadRequested=false;RevokeAttacks();
    }

    public void AppendAuthorizedAttackLeaves(List<AttackTelegraphVisual> leaves)
    {
        ArgumentNullException.ThrowIfNull(leaves);
        if (!IsInsideTree() || IsQueuedForDeletion() || _health?.CurrentHealth <= 0 || _threadRequested||_residence is {Available:false}) return;
        if (IsActiveLeaf(_telegraph)) leaves.Add(_telegraph);
        foreach (RangedWeaponProjectile projectile in _projectiles.Keys)
        {
            if (!IsInstanceValid(projectile) || projectile.IsQueuedForDeletion() || projectile.IsRetired) continue;
            AttackTelegraphVisual leaf = projectile.Telegraph;
            if (IsActiveLeaf(leaf) && IsAncestorOf(leaf)) leaves.Add(leaf);
        }
    }

    public void ResetForCheckpoint()
    {
        RevokeAttacks();
        if (_runtimeSettings == null) return;
        if (_spawnCaptured) GlobalPosition = _spawnPosition;
        Velocity = Vector2.Zero;
        _threadRequested = false;
        _defeatedEmitted = false;
        _model = new RangedWeaponModel(_runtimeSettings);
        // The timeline may restart; _damageOrdinal deliberately never does.
        CurrentFrame = new(RangedWeaponState.Idle, Vector2.Zero, default, -1, 0);
        _health?.ResetFull();
        _residentTarget=null;_residence?.ResetForCheckpoint();
        GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.AddToGroup(ThreadTarget2D.TargetGroup);
        FrameChanged?.Invoke(CurrentFrame);
    }

    public void ApplyThreadPull(Vector2 velocity)
    {
        if (!Finite(velocity) || !CanOwnAttack() || _threadRequested ||
            CurrentFrame.State == RangedWeaponState.Stunned) return;
        // One interaction edge, not a level that refreshes the model every tick.
        _threadRequested = true;
        RevokeAttacks();
    }

    private void EmitAttack(Node2D player)
    {
        if (_damageOrdinal == ulong.MaxValue || !Finite(CurrentFrame.AimDirection) ||
            CurrentFrame.AimDirection.IsZeroApprox()) return;
        var identity = new DamageAttackIdentity(GetInstanceId(), ++_damageOrdinal);
        Vector2 origin = _muzzle.GlobalPosition;
        bool emitted = false;
        if (Role == RangedWeaponRole.Laser)
        {
            if (ClippedBeam(origin, CurrentFrame.AimDirection, _runtimeSettings.LaserHalfWidth).Kind == AttackShapeKind.None)
                return;
            _beamIdentity = identity;
            _beamOrigin = origin;
            _beamDirection = CurrentFrame.AimDirection;
            emitted = true;
        }
        else
        {
            Vector2[] directions = Role == RangedWeaponRole.Scatter
                ? RangedAttackGeometry.ScatterDirections(CurrentFrame.AimDirection,
                    _runtimeSettings.ScatterPelletCount, _runtimeSettings.ScatterHalfAngleDegrees)
                : [CurrentFrame.AimDirection];
            var siblings = new List<(RangedWeaponProjectile Projectile, Vector2 Direction)>();
            // Register every sibling before Launch may synchronously retire one.
            foreach (Vector2 direction in directions)
            {
                var projectile = new RangedWeaponProjectile { Name = $"{Role}Round{identity.LocalAttackId}_{siblings.Count}", ZIndex = 7 };
                AddChild(projectile);
                _projectiles.Add(projectile, identity);
                _identityUsers.TryGetValue(identity, out int users);
                _identityUsers[identity] = users + 1;
                projectile.Retired += OnProjectileRetired;
                projectile.ExplosionResolved += OnExplosionResolved;
                siblings.Add((projectile, direction));
            }
            foreach ((RangedWeaponProjectile projectile, Vector2 direction) in siblings)
            {
                if (!CanOwnAttack() || !_projectiles.ContainsKey(projectile)) break;
                projectile.Launch(this, origin, direction, identity, _runtimeSettings, _ledger, _worldArena,
                    Role == RangedWeaponRole.Missile && IsInstanceValid(player) ? player : null);
                emitted |= !projectile.IsRetired;
            }
        }
        if (emitted && CanOwnAttack()) ShotEmitted?.Invoke(identity.LocalAttackId, origin);
    }

    private void PresentAndDamageBeam()
    {
        if (_beamIdentity.SourceRuntimeId == 0 || !CanOwnAttack()) return;
        AttackShapeFrame shape = ClippedBeam(_beamOrigin, _beamDirection, _runtimeSettings.LaserHalfWidth);
        if (shape.Kind == AttackShapeKind.None) return;
        var presentation = new AttackPresentationFrame(CurrentFrame.AttackTimeline, shape).Validate();
        if (IsInstanceValid(_telegraph)) _telegraph.Present(presentation);
        _beamCollision.Radius = shape.Extent.X;
        _beamCollision.Height = shape.Extent.Y * 2f;
        using var query = new PhysicsShapeQueryParameters2D
        {
            Shape = _beamCollision, Transform = new Transform2D(shape.RotationRadians, shape.Origin),
            CollisionMask = CombatCollisionLayers.Player, CollideWithAreas = true,
            CollideWithBodies = false, Exclude = ExcludedOwner()
        };
        using Godot.Collections.Array hits = (Godot.Collections.Array)GetWorld2D().DirectSpaceState.IntersectShape(query, 256);
        if (hits.Count >= 256) return; // Never deliver from an incomplete native result set.
        for (int index = 0; index < hits.Count; index++)
        {
            if (!CanOwnAttack() || _beamIdentity.SourceRuntimeId == 0) break;
            using Godot.Collections.Dictionary hit = hits[index].AsGodotDictionary();
            if (!hit.TryGetValue("collider", out Variant collider) || collider.AsGodotObject() is not Hurtbox target) continue;
            _ledger.TryDeliver(target, new DamageRequest(_runtimeSettings.Damage, _beamDirection, 120f,
                _beamIdentity.LocalAttackId, _beamIdentity.SourceRuntimeId, DamageGuardClass.Normal));
        }
    }

    private void PublishAttack()
    {
        AttackTimelineFrame timeline = CurrentFrame.AttackTimeline;
        if (timeline.Phase == AttackPhase.None)
        {
            ClearLocalWarning();
            AttackFrameChanged?.Invoke(default);
            return;
        }
        AttackShapeFrame shape;
        if (timeline.Phase == AttackPhase.Telegraph)
        {
            shape = TelegraphShape();
            if (shape.Kind == AttackShapeKind.None) { ClearLocalWarning(); AttackFrameChanged?.Invoke(default); return; }
            if (timeline.HasLockedTarget)
                timeline = timeline with { LockedWorldTarget = ShapeEndpoint(shape) };
            var warning = new AttackPresentationFrame(timeline, shape).Validate();
            if (IsInstanceValid(_telegraph)) _telegraph.Present(warning);
            AttackFrameChanged?.Invoke(warning);
            return;
        }
        if (Role == RangedWeaponRole.Laser && timeline.Phase == AttackPhase.Active)
        {
            shape = ClippedBeam(_beamOrigin, _beamDirection, _runtimeSettings.LaserHalfWidth);
            if (shape.Kind == AttackShapeKind.None) { ClearLocalWarning(); AttackFrameChanged?.Invoke(default); return; }
        }
        else
        {
            ClearLocalWarning();
            RangedWeaponProjectile live = _projectiles.Keys.FirstOrDefault(p => IsInstanceValid(p) && !p.IsRetired);
            shape = timeline.Phase == AttackPhase.Active && IsInstanceValid(live?.Telegraph)
                ? live.Telegraph.CurrentFrame.Shape
                : new AttackShapeFrame(AttackShapeKind.Circle, _muzzle.GlobalPosition, Vector2.One, 0f, Array.Empty<Vector2>());
            if (shape.Kind == AttackShapeKind.None)
                shape = new AttackShapeFrame(AttackShapeKind.Circle, _muzzle.GlobalPosition, Vector2.One, 0f, Array.Empty<Vector2>());
        }
        AttackFrameChanged?.Invoke(new AttackPresentationFrame(timeline, shape).Validate());
    }

    private AttackShapeFrame TelegraphShape()
    {
        Vector2 origin = _muzzle.GlobalPosition;
        if (Role != RangedWeaponRole.Scatter)
            return ClippedBeam(origin, CurrentFrame.AimDirection,
                Role == RangedWeaponRole.Laser ? _runtimeSettings.LaserHalfWidth : _runtimeSettings.ProjectileRadius);
        Vector2[] directions = RangedAttackGeometry.ScatterDirections(CurrentFrame.AimDirection, 3,
            _runtimeSettings.ScatterHalfAngleDegrees);
        Vector2 left = ClippedEnd(origin, directions[0], _runtimeSettings.ProjectileRadius);
        Vector2 right = ClippedEnd(origin, directions[^1], _runtimeSettings.ProjectileRadius);
        if (MathF.Abs((left - origin).Cross(right - origin)) < .001f) return default;
        return new AttackShapeFrame(AttackShapeKind.ConvexPolygon, origin, Vector2.Zero, 0f, [origin, left, right]);
    }

    private AttackShapeFrame ClippedBeam(Vector2 origin, Vector2 direction, float radius)
    {
        Vector2 end = ClippedEnd(origin, direction, radius);
        return origin.DistanceSquaredTo(end) <= .0001f ? default : RangedAttackGeometry.Beam(origin, end, radius);
    }

    private Vector2 ClippedEnd(Vector2 origin, Vector2 direction, float radius)
    {
        if (!_arenaConfigured || !Finite(direction) || direction.IsZeroApprox()) return origin;
        Vector2 bounded = RangedAttackGeometry.ClipToArea(origin,
            origin + direction.Normalized() * _runtimeSettings.MaximumTravel, _worldArena, radius);
        return RangedAttackGeometry.ClipBeam(GetWorld2D().DirectSpaceState, origin,
            direction, origin.DistanceTo(bounded), radius, ExcludedOwner());
    }

    private static Vector2 ShapeEndpoint(AttackShapeFrame shape) => shape.Kind == AttackShapeKind.ConvexPolygon
        ? (shape.Points[1] + shape.Points[2]) * .5f
        : shape.Origin + Vector2.Down.Rotated(shape.RotationRadians) * (shape.Extent.Y - shape.Extent.X);

    private void OnProjectileRetired(RangedWeaponProjectile projectile)
    {
        projectile.Retired -= OnProjectileRetired;
        projectile.ExplosionResolved -= OnExplosionResolved;
        if (_projectiles.Remove(projectile, out DamageAttackIdentity identity) && _identityUsers.TryGetValue(identity, out int users))
        {
            if (users <= 1) { _identityUsers.Remove(identity); _ledger.Release(identity); }
            else _identityUsers[identity] = users - 1;
        }
        if (IsInstanceValid(projectile) && !projectile.IsQueuedForDeletion()) projectile.QueueFree();
    }

    private void RevokeAttacks()
    {
        if (_revoking) return;
        _revoking = true;
        try
        {
            foreach (RangedWeaponProjectile projectile in _projectiles.Keys.ToArray())
            {
                if (!IsInstanceValid(projectile)) continue;
                projectile.Retire();
                projectile.Retired -= OnProjectileRetired;
                projectile.ExplosionResolved -= OnExplosionResolved;
                if (!projectile.IsQueuedForDeletion()) projectile.QueueFree();
            }
            _projectiles.Clear();
            _identityUsers.Clear();
            ReleaseBeam();
            _ledger.Clear();
            ClearLocalWarning();
            Node2D effects = GetNodeOrNull<Node2D>("VisualRoot/Effects");
            if (IsInstanceValid(effects))
                foreach (Node child in effects.GetChildren())
                    if (child is RangedWeaponImpactRing && !child.IsQueuedForDeletion()) child.QueueFree();
            AttackFrameChanged?.Invoke(default);
        }
        finally { _revoking = false; }
    }

    private void ReleaseBeam()
    {
        if (_beamIdentity.SourceRuntimeId != 0) _ledger.Release(_beamIdentity);
        _beamIdentity = default;
        _beamOrigin = Vector2.Zero;
        _beamDirection = Vector2.Zero;
        if (Role == RangedWeaponRole.Laser) ClearLocalWarning();
    }

    private void OnExplosionResolved(Vector2 center, float radius)
    {
        if (!CanOwnAttack() || !Finite(center) || !float.IsFinite(radius) || radius <= 0f) return;
        Node2D effects = GetNodeOrNull<Node2D>("VisualRoot/Effects");
        if (!IsInstanceValid(effects) || effects.IsQueuedForDeletion()) return;
        effects.AddChild(new RangedWeaponImpactRing
        {
            Name = "MissileImpact", Position = effects.ToLocal(center), Radius = radius, ZIndex = 8
        });
    }

    private void OnDamaged(int remaining, Vector2 impulse) => PresentationCueRequested?.Invoke(PresentationCue.Hurt);
    private void OnDied()
    {
        // Commit only this life's death before retirement/clear publications
        // can synchronously reset the actor to a new model and target state.
        if (_defeatedEmitted) return;
        _defeatedEmitted = true;
        CurrentFrame = _model.Step(new RangedWeaponInput(Vector2.Zero, Vector2.Zero, false, false, true), 0d);
        _residentTarget=null;_residence?.ApplyAuthority();
        GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.RemoveFromGroup(ThreadTarget2D.TargetGroup);
        RevokeAttacks();
        FrameChanged?.Invoke(CurrentFrame);
        PresentationCueRequested?.Invoke(PresentationCue.Died);
        Defeated?.Invoke();
    }

    private bool CanOwnAttack() => !_revoking && IsInsideTree() && !IsQueuedForDeletion() &&
        (_residence==null||_residence.Available)&&
        !_threadRequested && CurrentFrame.State != RangedWeaponState.Stunned &&
        _arenaConfigured && _runtimeSettings != null && _health?.CurrentHealth > 0;
    private bool MuzzleIsInViewport() => IsInstanceValid(_muzzle) &&
        GetViewport().GetVisibleRect().HasPoint(_muzzle.GetGlobalTransformWithCanvas().Origin);
    private bool IsActiveLeaf(AttackTelegraphVisual leaf) => IsInstanceValid(leaf) &&
        leaf.IsInsideTree() && !leaf.IsQueuedForDeletion() && IsAncestorOf(leaf) &&
        leaf.CurrentFrame.Timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active;
    private void ClearLocalWarning() { if (IsInstanceValid(_telegraph)) _telegraph.Present(default); }
    private Godot.Collections.Array<Rid> ExcludedOwner() => _ownerExclude;
    private void CaptureSpawnPosition()
    {
        if (_spawnCaptured || !IsInsideTree()) return;
        _spawnPosition = GlobalPosition;
        _spawnCaptured = true;
    }
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private static float NormalizeStep(double delta, float gravity) => double.IsFinite(delta) && delta > 0d &&
        float.IsFinite(gravity) && gravity > 0f && delta <= float.MaxValue / (double)gravity ? (float)delta : 0f;
}
