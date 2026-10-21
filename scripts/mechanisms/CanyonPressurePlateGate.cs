using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonPressurePlateGate : Node2D, ICheckpointResettable, ICanyonTelegraphSource
{
    [Export] public string MechanismId { get; set; } = "canyon.v2.pressure-gate.archive-reward";
    [Export] public NodePath DoorPath { get; set; } = new("Door");
    public CharacterBody2D PlayerBody { get; set; }
    [Export] public Rect2 PlateWorldRect { get; set; } = new(1620f, 1580f, 80f, 60f);
    [Export] public Rect2 InteriorWorldRect { get; set; } = new(1760f, 1424f, 120f, 216f);
    [Export] public Rect2 AntiCrushWorldRect { get; set; } = new(1700f, 1424f, 80f, 216f);
    public double OpenGraceSeconds { get; set; } = 1.40d;
    public double ClosingWarningSeconds { get; set; } = 0.55d;
    public bool IsOpen { get; private set; }
    public bool HasOpened { get; private set; }
    public bool IsDangerTelegraphActive => IsOpen &&
        _model.Phase == CanyonPressureGatePhase.ClosingWarning;
    public CanyonMechanismFrame Frame => new(IsDangerTelegraphActive, false, IsOpen,
        IsDangerTelegraphActive && ClosingWarningSeconds > 0d
            ? (float)Mathf.Clamp(_model.PhaseElapsed / ClosingWarningSeconds, 0d, 1d) : 0f);
    public CollisionShape2D DoorCollision => GetNode<StaticBody2D>(DoorPath).GetNode<CollisionShape2D>("Collision");
    private readonly CanyonPressureGateModel _model = new();
    private long _generation;
    private bool _openPending;
    private bool _closePending;
    private bool _mountProvisional;
    private RectangleShape2D _interiorShape;
    private RectangleShape2D _antiCrushShape;
    private readonly System.Collections.Generic.Stack<Node> _occupantTraversal = new();

    public void ResetForCheckpoint()
    {
        _generation++;
        _openPending = _closePending = false;
        _model.Reset();
        HasOpened = false;
        if (IsInsideTree() && IsOpen && !PlayerPressesPlate() &&
            !(_mountProvisional ? HasCurrentTransformOccupant(false) : HasProtectedOccupant()))
            ScheduleClose();
    }

    public override void _Ready()
    {
        if (GetNode<StaticBody2D>(DoorPath).GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile") is { } profile)
            profile.Material = GrappleSurfaceMaterial.Forbidden;
        // Authoring state is not a safe mount decision: clear occupants before closing.
        DoorCollision.Disabled = true;
        IsOpen = DoorCollision.Disabled;
        _mountProvisional = true;
        _interiorShape = new RectangleShape2D { Size = InteriorWorldRect.Size };
        _antiCrushShape = new RectangleShape2D { Size = AntiCrushWorldRect.Size };
    }

    public override void _PhysicsProcess(double delta)
    {
        _model.OpenGraceSeconds = OpenGraceSeconds;
        _model.ClosingWarningSeconds = ClosingWarningSeconds;
        bool plate = PlayerPressesPlate();
        // The padded outside anti-crush margin only keeps an already open door safe.
        // A closed door opens without a plate press only for a body actually inside
        // the alcove or penetrating the physical door footprint.
        bool hold = _mountProvisional ? HasCurrentTransformOccupant(false) :
            IsOpen || _openPending || _closePending
                ? HasProtectedOccupant() : HasCurrentTransformOccupant(false);
        _model.Step(plate, hold, delta);
        if (_mountProvisional && _model.WantsOpen && IsOpen)
        {
            _mountProvisional = false;
            HasOpened = true;
            _model.MarkOpened();
        }
        if (_model.WantsOpen)
        {
            if (_closePending) { _generation++; _closePending = false; }
            if (!IsOpen && !_openPending) ScheduleOpen();
        }
        else if (IsOpen && !_closePending) ScheduleClose();
    }

    public override void _ExitTree()
    {
        _generation++;
        _openPending = _closePending = false;
        _occupantTraversal.Clear();
        PlayerBody = null;
    }

    private void ScheduleOpen()
    {
        _openPending = true;
        CallDeferred(nameof(CommitOpen), _generation);
    }

    private void ScheduleClose()
    {
        _closePending = true;
        CallDeferred(nameof(CommitClose), _generation);
    }

    private void CommitOpen(long generation)
    {
        _openPending = false;
        if (!IsInsideTree() || GetTree().Paused || generation != _generation || !_model.WantsOpen) return;
        DoorCollision.Disabled = true;
        IsOpen = DoorCollision.Disabled;
        if (IsOpen)
        {
            HasOpened = true;
            _model.MarkOpened();
        }
    }

    private void CommitClose(long generation)
    {
        _closePending = false;
        if (!IsInsideTree() || GetTree().Paused || generation != _generation || _model.WantsOpen)
            return;
        bool occupied = _mountProvisional ? HasCurrentTransformOccupant(false) :
            HasProtectedOccupant() || HasCurrentTransformOccupant();
        if (PlayerPressesPlate() || occupied) return;
        DoorCollision.Disabled = false;
        IsOpen = DoorCollision.Disabled;
        if (IsOpen) return;
        _mountProvisional = false;
        HasOpened = _model.HasOpened;
    }

    private bool HasProtectedOccupant()
    {
        if (!IsInsideTree()) return false;
        _interiorShape.Size = InteriorWorldRect.Size;
        _antiCrushShape.Size = AntiCrushWorldRect.Size;
        return HasProtectedBody(_interiorShape,
                new Transform2D(0f, InteriorWorldRect.GetCenter())) ||
            HasProtectedBody(_antiCrushShape,
                new Transform2D(0f, AntiCrushWorldRect.GetCenter())) ||
            HasProtectedBody(DoorCollision.Shape, DoorCollision.GlobalTransform);
    }

    private bool HasProtectedBody(Shape2D shape, Transform2D transform)
    {
        using var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape, Transform = transform,
            CollisionMask = CombatCollisionLayers.Player | CombatCollisionLayers.Enemy,
            CollideWithAreas = false, CollideWithBodies = true
        };
        var hits = GetWorld2D().DirectSpaceState.IntersectShape(query, 256);
        using var hitsOwner = (Godot.Collections.Array)hits;
        foreach (Godot.Collections.Dictionary hit in hits)
        {
            using (hit)
            {
                if (!hit.ContainsKey("collider") ||
                    hit["collider"].AsGodotObject() is not CharacterBody2D body ||
                    !GodotObject.IsInstanceValid(body) || !body.IsInsideTree()) continue;
                if (body == PlayerBody ||
                    (body.CollisionLayer & CombatCollisionLayers.Enemy) != 0 &&
                    (body.CollisionMask & CombatCollisionLayers.World) != 0) return true;
            }
        }
        return false;
    }

    // Physics broadphase may still describe the previous frame when another body is moved
    // after our physics callback. The deferred collision commit also checks current node
    // transforms against the actual enabled body shapes before enabling the door.
    private bool HasCurrentTransformOccupant(bool includeAntiCrush = true)
    {
        // Scratch storage only, never an occupant cache: rescan the current whole
        // tree each time, including bodies mounted or reparented in this frame.
        _occupantTraversal.Clear();
        _occupantTraversal.Push(GetTree().Root);
        try
        {
            while (_occupantTraversal.Count > 0)
            {
                Node node = _occupantTraversal.Pop();
                if (node is CharacterBody2D body && IsProtectedBody(body))
                {
                    for (int index = 0, count = body.GetChildCount(); index < count; index++)
                    {
                        if (body.GetChild(index) is not CollisionShape2D
                            { Disabled: false, Shape: not null } collider) continue;
                        if (TouchesProtectedZone(collider, includeAntiCrush)) return true;
                    }
                }
                // Same ascending push / last-child-first scan as GetChildren,
                // without allocating native collection wrappers for every node.
                for (int index = 0, count = node.GetChildCount(); index < count; index++)
                    _occupantTraversal.Push(node.GetChild(index));
            }
            return false;
        }
        finally { _occupantTraversal.Clear(); }
    }

    private bool TouchesProtectedZone(CollisionShape2D collider, bool includeAntiCrush)
    {
        Shape2D actor = collider.Shape;
        Transform2D actorTransform = collider.GlobalTransform;
        Transform2D interiorTransform = new(0f, InteriorWorldRect.GetCenter());
        if (includeAntiCrush)
            return _interiorShape.Collide(interiorTransform, actor, actorTransform) ||
                _antiCrushShape.Collide(new Transform2D(0f, AntiCrushWorldRect.GetCenter()),
                    actor, actorTransform) ||
                DoorCollision.Shape.Collide(DoorCollision.GlobalTransform, actor, actorTransform);
        // Strict positive overlap prevents edge contact at x1700 from becoming an
        // implicit outside pressure plate; shape collision filters rounded corners.
        return PositiveShapeOverlap(_interiorShape, interiorTransform, actor, actorTransform) ||
            PositiveShapeOverlap(DoorCollision.Shape, DoorCollision.GlobalTransform,
                actor, actorTransform);
    }

    private static bool PositiveShapeOverlap(Shape2D zone, Transform2D zoneTransform,
        Shape2D actor, Transform2D actorTransform) =>
        WorldBounds(zone.GetRect(), zoneTransform).Intersects(
            WorldBounds(actor.GetRect(), actorTransform)) &&
        zone.Collide(zoneTransform, actor, actorTransform);

    private static Rect2 WorldBounds(Rect2 local, Transform2D transform)
    {
        Vector2 a = transform * local.Position;
        Vector2 b = transform * new Vector2(local.End.X, local.Position.Y);
        Vector2 c = transform * new Vector2(local.Position.X, local.End.Y);
        Vector2 d = transform * local.End;
        Vector2 min = a.Min(b).Min(c).Min(d);
        Vector2 max = a.Max(b).Max(c).Max(d);
        return new Rect2(min, max - min);
    }

    private bool IsProtectedBody(CharacterBody2D body) =>
        GodotObject.IsInstanceValid(body) && body.IsInsideTree() &&
        (body == PlayerBody ||
         (body.CollisionLayer & CombatCollisionLayers.Enemy) != 0 &&
         (body.CollisionMask & CombatCollisionLayers.World) != 0);

    private bool PlayerPressesPlate()
    {
        CharacterBody2D player = PlayerBody;
        if (!GodotObject.IsInstanceValid(player) || !player.IsInsideTree() || !player.IsOnFloor() ||
            player.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 } ||
            player.GetNodeOrNull<CollisionShape2D>("BodyCollider") is not { Disabled: false,
                Shape: CapsuleShape2D capsule } collider)
            return false;
        Vector2 foot = collider.GlobalTransform * new Vector2(0f, capsule.Height * .5f);
        float probeOffset = capsule.Radius * .25f;
        if (!float.IsFinite(foot.X) || !float.IsFinite(foot.Y) ||
            foot.X - probeOffset < PlateWorldRect.Position.X ||
            foot.X + probeOffset > PlateWorldRect.End.X ||
            Mathf.Abs(foot.Y - PlateWorldRect.End.Y) > 4f) return false;
        foreach (float x in new[] { foot.X - probeOffset, foot.X + probeOffset })
        {
            var exclude = new Godot.Collections.Array<Rid> { player.GetRid() };
            using var excludeOwner = (Godot.Collections.Array)exclude;
            using var query = PhysicsRayQueryParameters2D.Create(new Vector2(x, foot.Y - 4f),
                new Vector2(x, foot.Y + 6f), CombatCollisionLayers.World);
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;
            query.Exclude = exclude;
            using var hit = GetWorld2D().DirectSpaceState.IntersectRay(query);
            if (hit.Count == 0) return false;
        }
        return true;
    }
}
