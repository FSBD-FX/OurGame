using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;

namespace Knight.Mechanisms;

// Persistent terrain contact, not a fall/recovery trigger. All authored shapes are borrowed.
public partial class CanyonIncinerator : Area2D, ICheckpointResettable, ICanyonTelegraphSource
{
    [Export] public Vector2 Size { get; set; } = new(720f, 276f);
    [Export] public float PulseSeconds { get; set; } = .1f;
    public ulong AttemptSequence { get; private set; }
    public Rect2 DangerBounds => new(-_size * .5f, _size);
    public bool IsDangerTelegraphActive => OwnsDanger();

    private readonly Dictionary<ulong, double> _remaining = new();
    private readonly HashSet<ulong> _present = new();
    private readonly List<ulong> _expired = new();
    private CollisionShape2D _collider;
    private Vector2 _size;
    private double _interval;
    private ulong _epoch;
    private bool _ready, _delivering;

    public override void _Ready()
    {
        if (!Size.IsFinite() || Size.X < 32f || Size.Y < 16f || Size.X > 4096f || Size.Y > 2048f ||
            !float.IsFinite(PulseSeconds) || PulseSeconds < .1f || PulseSeconds > 5f)
            throw new InvalidOperationException("Incinerator requires finite bounded rectangle and contact cadence.");
        _size = Size; _interval = PulseSeconds;
        _collider = GetNodeOrNull<CollisionShape2D>("Collision");
        if (!GeometryMatches(HasStagedOwner()))
            throw new InvalidOperationException("Incinerator requires one matching native rectangle at its origin.");
        CollisionLayer = CombatCollisionLayers.None;
        CollisionMask = CombatCollisionLayers.Player;
        Monitoring = true; Monitorable = false;
        ProcessPhysicsPriority = 100;
        _ready = true;
        QueueRedraw();
    }

    public override void _ExitTree()
    {
        _ready = false;
        ResetForCheckpoint();
        _collider = null;
    }

    public void ResetForCheckpoint()
    {
        _epoch++;
        _remaining.Clear(); _present.Clear(); _expired.Clear();
        // A saved Player survives recovery. Never recycle a spent source identity.
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_delivering || !double.IsFinite(delta) || delta <= 0d) return;
        if (!OwnsDanger()) { ResetForCheckpoint(); return; }
        _delivering = true;
        try
        {
            ulong epoch = _epoch;
            Godot.Collections.Array<Node2D> bodies = GetOverlappingBodies();
            using Godot.Collections.Array backing = (Godot.Collections.Array)bodies;
            _present.Clear();
            foreach (Node2D body in bodies)
            {
                if (_epoch != epoch || !OwnsDanger()) return;
                if (body is not CharacterBody2D player || !LivePlayer(player) || !CurrentBodyContact(player)) continue;
                PlayerGuardedDamageReceiver receiver = player.GetNodeOrNull<PlayerGuardedDamageReceiver>("GuardedDamage");
                HealthComponent health = player.GetNodeOrNull<HealthComponent>("Health");
                if (!OwnerLive(receiver) || !receiver.CanProcess() || !OwnerLive(health) || !health.CanProcess() ||
                    health.MaxHealth <= 0 || health.CurrentHealth <= 0) continue;
                ulong receiverId = receiver.GetInstanceId();
                // A body may have several colliders. Identity/cadence belongs to its receiver, not a shape.
                if (!_present.Add(receiverId)) continue;
                if (_remaining.TryGetValue(receiverId, out double remaining))
                {
                    remaining = Math.Max(0d, remaining - delta);
                    _remaining[receiverId] = remaining;
                    if (remaining > .0000001d) continue;
                }
                _remaining[receiverId] = _interval; // Spend before any synchronous shield/health callbacks.
                AttemptSequence = checked(AttemptSequence + 1UL);
                Vector2 direction = GlobalPosition.DirectionTo(player.GlobalPosition);
                receiver.ReceiveDamage(new DamageRequest(health.MaxHealth, direction, 0f,
                    AttemptSequence, GetInstanceId(), DamageGuardClass.Unblockable));
                // Died may synchronously reset, relocate, stage, retire or replace the session.
                // No old overlap/cooldown work can continue into that new authority.
                if (_epoch != epoch || !OwnsDanger() || !LivePlayer(player) ||
                    !OwnerLive(receiver) || player.GetNodeOrNull<PlayerGuardedDamageReceiver>("GuardedDamage") != receiver ||
                    player.GetNodeOrNull<HealthComponent>("Health") != health || !CurrentBodyContact(player)) return;
            }
            _expired.Clear();
            foreach (ulong id in _remaining.Keys) if (!_present.Contains(id)) _expired.Add(id);
            foreach (ulong id in _expired) _remaining.Remove(id);
        }
        finally { _delivering = false; }
    }

    private bool OwnsDanger() => _ready && OwnerLive(this) && IsVisibleInTree() && CanProcess() &&
        IsPhysicsProcessing() && !GetTree().Paused && Monitoring &&
        (CollisionMask & CombatCollisionLayers.Player) != 0 && GeometryMatches() && SessionAllows(this);

    private bool LivePlayer(CharacterBody2D player) => OwnerLive(player) && player.CanProcess() &&
        player.IsPhysicsProcessing() && player.IsVisibleInTree() && player.IsInGroup("player") &&
        (player.CollisionLayer & CombatCollisionLayers.Player) != 0 && player.GetWorld2D() == GetWorld2D() &&
        player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is > 0 && SessionAllows(player);

    private static bool SessionAllows(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
        {
            if (current.GetNodeOrNull<HealthComponent>("Health") is { } ownerHealth && ownerHealth.CurrentHealth <= 0)
                return false;
            if (current is PrologueLevelController level)
                return !level.ResidentPreparationPending &&
                    (level.GetNodeOrNull<PrologueGameFlowController>("GameFlow") is not { } flow ||
                     flow.State == PrologueGameFlowState.Playing);
        }
        return true; // Standalone mechanism scenes have no product-session controller.
    }

    private bool GeometryMatches(bool preparing = false)
    {
        if (!IsInstanceValid(_collider) || !_collider.IsInsideTree() || _collider.IsQueuedForDeletion() ||
            _collider.GetParent() != this || _collider.Disabled && !preparing ||
            _collider.Shape is not RectangleShape2D rectangle || !rectangle.Size.IsEqualApprox(_size) ||
            PhysicsServer2D.AreaGetShapeCount(GetRid()) != 1 || !UsableTransform(GlobalTransform)) return false;
        Rid shape = PhysicsServer2D.AreaGetShape(GetRid(), 0);
        Transform2D actual = PhysicsServer2D.AreaGetShapeTransform(GetRid(), 0);
        using Variant dimensions = PhysicsServer2D.ShapeGetData(shape);
        return shape == rectangle.GetRid() && PhysicsServer2D.ShapeGetType(shape) == PhysicsServer2D.ShapeType.Rectangle &&
            dimensions.VariantType == Variant.Type.Vector2 && dimensions.AsVector2().IsEqualApprox(_size * .5f) &&
            actual.IsEqualApprox(Transform2D.Identity) && _collider.Transform.IsEqualApprox(actual);
    }

    private bool CurrentBodyContact(CharacterBody2D player)
    {
        // The Area overlap list updates at physics boundaries. Recheck the actual enabled native
        // body shapes with CURRENT node transforms, including teleport/reset during a contact callback.
        // Shape.Collide tests the real curved capsule, not its AABB; no query/shape resources are created.
        if (!UsableTransform(player.GlobalTransform) || _collider.Shape is not RectangleShape2D rectangle) return false;
        int count = PhysicsServer2D.BodyGetShapeCount(player.GetRid());
        for (int i = 0; i < count; i++)
        {
            uint ownerId = player.ShapeFindOwner(i);
            if (player.IsShapeOwnerDisabled(ownerId) ||
                player.ShapeOwnerGetOwner(ownerId) is not CollisionShape2D collision ||
                !OwnerLive(collision) || collision.Disabled || collision.GetParent() != player ||
                !IsInstanceValid(collision.Shape)) continue;
            Rid nativeShape = PhysicsServer2D.BodyGetShape(player.GetRid(), i);
            Transform2D local = PhysicsServer2D.BodyGetShapeTransform(player.GetRid(), i);
            if (nativeShape != collision.Shape.GetRid() || !local.IsEqualApprox(collision.Transform)) continue;
            Transform2D transform = player.GlobalTransform * local;
            if (UsableTransform(transform) && rectangle.Collide(GlobalTransform, collision.Shape, transform)) return true;
        }
        return false;
    }

    private static bool UsableTransform(Transform2D transform) => transform.Origin.IsFinite() &&
        transform.X.IsFinite() && transform.Y.IsFinite() && Mathf.Abs(transform.Determinant()) > .000001f;

    private bool HasStagedOwner()
    {
        for (Node current = this; current != null; current = current.GetParent())
            if (current.HasMeta("_seamless_staged") && current.GetMeta("_seamless_staged").AsBool()) return true;
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

    public override void _Draw()
    {
        if (!_ready) return;
        Rect2 bounds = DangerBounds;
        Color ink = new("201b1c"), steel = new("726d60"), ember = new("db7740"), hot = new("f2c36b");
        DrawRect(bounds, ink);
        DrawRect(new Rect2(bounds.Position + new Vector2(8, 12), bounds.Size - new Vector2(16, 20)), ember.Darkened(.53f));
        for (float x = bounds.Position.X + 18f; x < bounds.End.X - 10f; x += 36f)
        {
            DrawLine(new(x, bounds.Position.Y + 24), new(x, bounds.End.Y - 8), steel.Darkened(.5f), 5f, true);
            DrawLine(new(x + 9, bounds.Position.Y + 38), new(x + 9, bounds.End.Y - 20), ember, 2f, true);
        }
        DrawRect(bounds, steel, false, 5f);
        DrawLine(bounds.Position, new(bounds.End.X, bounds.Position.Y), hot, 5f, true);
        for (float x = bounds.Position.X + 10f; x < bounds.End.X - 12f; x += 32f)
            DrawLine(new(x, bounds.Position.Y + 4), new(x + 12, bounds.Position.Y + 14), hot, 4f, true);
        Vector2 warning = new(0, bounds.Position.Y + 40f);
        DrawColoredPolygon([warning + new Vector2(0, -16), warning + new Vector2(-17, 14), warning + new Vector2(17, 14)], hot);
        DrawLine(warning + new Vector2(0, -7), warning + new Vector2(0, 4), ink, 3f, true);
        DrawCircle(warning + new Vector2(0, 9), 1.8f, ink);
    }
}
