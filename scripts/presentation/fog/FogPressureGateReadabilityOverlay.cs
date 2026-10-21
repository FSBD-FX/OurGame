using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

/// <summary>Protects original harmless closing paint; never controls the gate or its collision.</summary>
public partial class FogPressureGateReadabilityOverlay : Node2D
{
    [Export] public float GateCueRadius { get; set; } = 620f;
    public int PresentedGateCount { get; private set; }
    public int PresentedSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _seen = new();

    public override void _Ready() { ProcessMode = ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta) => Clear();

    public void PresentGates(IReadOnlyList<CanyonPressureGateVisual> sources, Vector2 actor,
        HeadlampOcclusionFrame frame)
    {
        Clear();
        if (!Live(this) || GetTree().Paused || sources == null || frame is not { IsComplete: true } ||
            !HeadlampOcclusionSampler.Finite(actor) || !float.IsFinite(GateCueRadius) || GateCueRadius <= 0f) return;
        LastOcclusionFrame = frame;
        float radius = Mathf.Min(GateCueRadius, 620f);
        foreach (CanyonPressureGateVisual visual in sources)
        {
            if (!Live(visual) || !visual.IsVisibleInTree() || !_seen.Add(visual.GetInstanceId()) ||
                visual.GetParent() is not CanyonPressurePlateGate gate || !Live(gate) ||
                !gate.IsPhysicsProcessing() || !gate.CanProcess() || !gate.IsDangerTelegraphActive ||
                !gate.IsOpen || !gate.Frame.Telegraphing || gate.Frame.HazardActive || !gate.Frame.Traversable ||
                !CanonicalSource(gate, visual) || visual.GetViewport() != GetViewport() ||
                visual.GetWorld2D().GetRid() != GetWorld2D().GetRid() ||
                !Aligned(visual.GetCanvasTransform(), GetCanvasTransform())) continue;
            StaticBody2D door = gate.GetNodeOrNull<StaticBody2D>(gate.DoorPath);
            CollisionShape2D collision = door?.GetNodeOrNull<CollisionShape2D>("Collision");
            if (!Live(door) || !Live(collision) || !collision.Disabled ||
                (door.CollisionLayer & CombatCollisionLayers.World) == 0) continue;
            int[] owners = door.GetShapeOwners();
            if (owners.Length != 1) continue;
            uint owner = (uint)owners[0];
            // The warning door is disabled: deliberately not a snapshot member.
            // Prove native/node agreement without inventing an occluder for it.
            if (door.ShapeOwnerGetOwner(owner) != collision || !door.IsShapeOwnerDisabled(owner) ||
                door.ShapeOwnerGetShapeCount(owner) != 1 ||
                door.ShapeOwnerGetShape(owner, 0) is not RectangleShape2D shape || shape != collision.Shape ||
                !Aligned(door.GlobalTransform * door.ShapeOwnerGetTransform(owner), collision.GlobalTransform)) continue;
            CanyonPressureGatePaintFrame paint = visual.ClosingPaint;
            if (paint.Strokes.Count != 6 || !FiniteTransform(paint.SourceToWorld)) continue;
            int before = PresentedSegmentCount;
            foreach (CanyonPressureGatePaintStroke stroke in paint.Strokes)
            {
                Vector2 vector = stroke.To - stroke.From;
                float length = vector.Length();
                if (!HeadlampOcclusionSampler.Finite(stroke.From) || !HeadlampOcclusionSampler.Finite(stroke.To) ||
                    !float.IsFinite(length) || length <= 0f || length > 512f ||
                    !float.IsFinite(stroke.Width) || stroke.Width <= 0f || stroke.Width > 8f) continue;
                Vector2 direction = vector / length;
                Vector2 halfWidth = new Vector2(-direction.Y, direction.X) * (stroke.Width * .5f);
                for (float distance = 0f; distance < length; distance += 8f)
                {
                    Vector2 start = stroke.From + direction * distance;
                    Vector2 end = stroke.From + direction * Mathf.Min(length, distance + 8f);
                    Paint([start - halfWidth, end - halfWidth, end + halfWidth, start + halfWidth],
                        paint.SourceToWorld, visual, stroke.Color, actor, frame, radius);
                }
            }
            if (PresentedSegmentCount > before) PresentedGateCount++;
        }
    }

    private void Paint(Vector2[] corners, Transform2D transform, Node2D source, Color color, Vector2 actor,
        HeadlampOcclusionFrame frame, float radius)
    {
        Vector2[] world = [transform * corners[0], transform * corners[1], transform * corners[2], transform * corners[3]];
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity), max = new(float.NegativeInfinity, float.NegativeInfinity);
        Transform2D canvas = source.GetCanvasTransform();
        for (int index = 0; index < 4; index++)
        {
            Vector2 point = world[index];
            if (!HeadlampOcclusionSampler.Finite(point) || actor.DistanceSquaredTo(point) > radius * radius ||
                !frame.CanSeeSegment(actor, point, world[(index + 1) % 4])) return;
            Vector2 pixel = canvas * point;
            if (!HeadlampOcclusionSampler.Finite(pixel)) return;
            min = min.Min(pixel); max = max.Max(pixel);
        }
        if (!GetViewport().GetVisibleRect().Intersects(new Rect2(min, max - min))) return;
        Vector2[] local = [ToLocal(world[0]), ToLocal(world[1]), ToLocal(world[2]), ToLocal(world[3])];
        foreach (Vector2 point in local) if (!HeadlampOcclusionSampler.Finite(point)) return;
        RenderingServer.CanvasItemAddPolygon(GetCanvasItem(), local, [color]);
        PresentedSegmentCount++;
    }

    private static bool CanonicalSource(CanyonPressurePlateGate gate, CanyonPressureGateVisual visual) =>
        gate.GetParent()?.GetParent()?.GetParent() is PrologueSector sector && Live(sector) &&
        sector.GetNodeOrNull<Node>("Gameplay/Mechanisms") == gate.GetParent() &&
        gate.GetNodeOrNull<CanyonPressureGateVisual>("ReadableGate") == visual;
    private static bool FiniteTransform(Transform2D transform) =>
        HeadlampOcclusionSampler.Finite(transform.X) && HeadlampOcclusionSampler.Finite(transform.Y) &&
        HeadlampOcclusionSampler.Finite(transform.Origin) && float.IsFinite(transform.Determinant()) &&
        transform.Determinant() != 0f;
    private static bool Aligned(Transform2D a, Transform2D b) => FiniteTransform(a) && FiniteTransform(b) &&
        a.X.DistanceTo(b.X) < .001f && a.Y.DistanceTo(b.Y) < .001f && a.Origin.DistanceTo(b.Origin) < .01f;
    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion() || current.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
        return true;
    }
    public void Clear()
    {
        PresentedGateCount = 0; PresentedSegmentCount = 0; LastOcclusionFrame = null; _seen.Clear();
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }
    public override void _ExitTree() { Clear(); RequestReady(); }
}
