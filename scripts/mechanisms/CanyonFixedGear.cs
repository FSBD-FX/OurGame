using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;

namespace Knight.Mechanisms;

// Fixed danger circle, rotating teeth only. Never moves terrain or owns borrowed shapes.
public partial class CanyonFixedGear : Area2D, ICheckpointResettable, ICanyonTelegraphSource
{
    [Export] public float Radius { get; set; } = 30f;
    [Export] public int Damage { get; set; } = 5;
    [Export] public float ContactIntervalSeconds { get; set; } = .5f;
    [Export] public float AngularSpeedRadians { get; set; } = 2.4f;

    public ulong ContactSequence { get; private set; }
    public float DangerRadius => _radius;
    public virtual bool IsDangerTelegraphActive => _ready && OwnerLive(this) && IsVisibleInTree() && Monitoring &&
        (CollisionMask & CombatCollisionLayers.Player) != 0 && GeometryMatches();
    public event Action<Vector2, ulong> Contacted;

    private readonly Dictionary<ulong, float> _remaining = new();
    private readonly HashSet<ulong> _present = new();
    private readonly List<ulong> _expired = new();
    private CollisionShape2D _collider;
    private Vector2[] _teeth;
    private float _radius, _interval, _speed, _angle;
    private int _damage;
    private ulong _epoch;
    private bool _ready;
    private bool _processingContacts;
    protected bool IsProcessingGearContacts => _processingContacts;

    public override void _Ready()
    {
        if (!float.IsFinite(Radius) || Radius < 12f || Radius > 96f || Damage < 1 || Damage > 100 ||
            !float.IsFinite(ContactIntervalSeconds) || ContactIntervalSeconds < .1f || ContactIntervalSeconds > 5f ||
            !float.IsFinite(AngularSpeedRadians) || Mathf.Abs(AngularSpeedRadians) > 12f)
            throw new InvalidOperationException("Fixed gear requires finite bounded radius, damage, cadence and visual speed.");
        _collider = GetNodeOrNull<CollisionShape2D>("Collision");
        _radius = Radius; _damage = Damage; _interval = ContactIntervalSeconds; _speed = AngularSpeedRadians;
        if (!GeometryMatches(HasStagedOwner())) throw new InvalidOperationException("Fixed gear requires one matching native circle at its origin.");
        _teeth = new Vector2[48];
        for (int index = 0; index < _teeth.Length; index++)
            _teeth[index] = Vector2.Right.Rotated(index * Mathf.Tau / _teeth.Length) *
                (_radius * (index % 4 is 1 or 2 ? 1f : .8f));
        CollisionLayer = CombatCollisionLayers.None;
        CollisionMask = CombatCollisionLayers.Player;
        Monitoring = true; Monitorable = false; ProcessPhysicsPriority = 100;
        _ready = true;
    }

    public override void _ExitTree()
    {
        _epoch++; _ready = false; _remaining.Clear(); _present.Clear(); _expired.Clear(); _collider = null;
    }

    public virtual void ResetForCheckpoint()
    {
        _epoch++; _remaining.Clear(); _present.Clear(); _expired.Clear();
        // Attack identities must not repeat against a persistent saved Player.
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_processingContacts || !double.IsFinite(delta) || delta <= 0d || !IsDangerTelegraphActive || GetTree().Paused || !CanProcess()) return;
        Godot.Collections.Array<Node2D> bodies = GetOverlappingBodies();
        using Godot.Collections.Array backing = (Godot.Collections.Array)bodies;
        ProcessGearContacts(bodies,delta);
    }

    // Static overlap and moving swept geometry share exactly one cadence and
    // damage path. Reentrant callbacks cannot consume a second batch or time.
    protected void ProcessGearContacts(IEnumerable<Node2D> bodies,double delta)
    {
        if (_processingContacts || bodies==null || !double.IsFinite(delta) || delta<=0 ||
            !IsDangerTelegraphActive || GetTree().Paused || !CanProcess()) return;
        _processingContacts=true;
        try
        {
        ulong epoch = _epoch;
        _present.Clear();
        foreach (Node2D node in bodies)
        {
            if (_epoch != epoch || !IsDangerTelegraphActive || GetTree().Paused || !CanProcess()) return;
            if (node is not CharacterBody2D player || !OwnerLive(player) || !player.CanProcess() ||
                !player.IsInGroup("player") || player.GetWorld2D() != GetWorld2D() ||
                player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0) continue;
            ulong id = player.GetInstanceId();
            if (!_present.Add(id)) continue;
            if (_remaining.TryGetValue(id, out float remaining) && remaining > 0f)
            {
                _remaining[id] = (float)Math.Max(0d, remaining - delta);
                continue;
            }
            PlayerGuardedDamageReceiver receiver = player.GetNodeOrNull<PlayerGuardedDamageReceiver>("GuardedDamage");
            if (receiver == null) continue;
            _remaining[id] = _interval;
            ContactSequence = checked(ContactSequence + 1UL);
            Vector2 contact = player.GlobalPosition;
            bool accepted = receiver.ReceiveDamage(new DamageRequest(_damage,
                GlobalPosition.DirectionTo(contact), 0f, ContactSequence, GetInstanceId(), DamageGuardClass.Unblockable));
            if (_epoch != epoch || !OwnerLive(this) ||
                !IsDangerTelegraphActive || GetTree().Paused || !CanProcess()) return;
            if (accepted) Contacted?.Invoke(contact, ContactSequence);
        }
        _expired.Clear();
        foreach (ulong id in _remaining.Keys) if (!_present.Contains(id)) _expired.Add(id);
        foreach (ulong id in _expired) _remaining.Remove(id);
        }
        finally { _processingContacts=false; }
    }

    protected bool GeometryMatches(bool preparing = false)
    {
        if (!IsInstanceValid(_collider) || _collider.Disabled && !preparing || !_collider.IsInsideTree() ||
            PhysicsServer2D.AreaGetShapeCount(GetRid()) != 1) return false;
        Rid shape = PhysicsServer2D.AreaGetShape(GetRid(), 0);
        Transform2D transform = PhysicsServer2D.AreaGetShapeTransform(GetRid(), 0);
        return PhysicsServer2D.ShapeGetType(shape) == PhysicsServer2D.ShapeType.Circle &&
            Mathf.IsEqualApprox(PhysicsServer2D.ShapeGetData(shape).AsSingle(), _radius) &&
            transform.Origin.IsZeroApprox() && Mathf.IsEqualApprox(transform.X.Length(), 1f) &&
            Mathf.IsEqualApprox(transform.Y.Length(), 1f) && Mathf.Abs(transform.X.Dot(transform.Y)) < .0001f;
    }

    private bool HasStagedOwner()
    {
        // Read-only admission for the existing disabled-before-Ready streaming
        // transaction. Runtime GeometryMatches still requires enabled collision.
        for(Node current=this;current!=null;current=current.GetParent())
            if(current.HasMeta("_seamless_staged") && current.GetMeta("_seamless_staged").AsBool()) return true;
        return false;
    }

    private static bool OwnerLive(Node owner)
    {
        if (!IsInstanceValid(owner) || !owner.IsInsideTree()) return false;
        for (Node current = owner; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion() ||
                current.HasMeta("_seamless_staged") && current.GetMeta("_seamless_staged").AsBool()) return false;
        return true;
    }

    public override void _Process(double delta)
    {
        if (!_ready || !double.IsFinite(delta) || delta <= 0d || GetTree().Paused || !IsVisibleInTree()) return;
        _angle = Mathf.PosMod(_angle + _speed * (float)delta, Mathf.Tau);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_ready || _teeth == null) return;
        Color steel = new("83908d"), ink = new("162329"), amber = new("d4aa58");
        DrawLine(new(-52, 0), Vector2.Zero, ink, 12f, true);
        DrawLine(new(-52, 0), Vector2.Zero, steel.Darkened(.3f), 5f, true);
        DrawArc(Vector2.Zero, _radius, 0, Mathf.Tau, 48, amber.Darkened(.2f), 1.2f, true);
        DrawSetTransform(Vector2.Zero, _angle, Vector2.One);
        DrawColoredPolygon(_teeth, steel);
        DrawCircle(Vector2.Zero, _radius * .67f, ink);
        for (int index = 0; index < 6; index++)
        {
            Vector2 p = Vector2.Right.Rotated(index * Mathf.Tau / 6f);
            DrawLine(p * (_radius * .28f), p * (_radius * .58f), steel.Darkened(.25f), 3f, true);
        }
        DrawCircle(Vector2.Zero, _radius * .29f, amber);
        DrawCircle(Vector2.Zero, _radius * .14f, ink);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
