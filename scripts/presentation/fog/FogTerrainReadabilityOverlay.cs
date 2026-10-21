using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

/// <summary>
/// Small support-height marks, not new terrain or a claim of a damage-free landing.
/// Style comes from authored ReadableEdge; geometry comes only from the fog's
/// same immutable native owner snapshot. Unknown/sloping surfaces fail closed.
/// </summary>
public partial class FogTerrainReadabilityOverlay : Node2D
{
    [Export] public float TerrainCueRadius { get; set; } = 620f;
    public int PresentedSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _bodies = new();

    public override void _Ready() { ProcessMode = ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta) => Clear();

    public void PresentTerrain(IReadOnlyList<CanyonPlatformVisual> sources, Vector2 actor,
        HeadlampOcclusionFrame frame)
    {
        Clear();
        if (!Live(this) || GetTree().Paused || sources == null || frame is not { IsComplete: true } ||
            !HeadlampOcclusionSampler.Finite(actor) || !float.IsFinite(TerrainCueRadius) || TerrainCueRadius <= 0f) return;
        LastOcclusionFrame = frame;
        float radius = Mathf.Min(TerrainCueRadius, 620f); // Bounded display work, not a grapple-range change.
        foreach (CanyonPlatformVisual skin in sources)
        {
            if (!Live(skin) || !skin.IsVisibleInTree() ||
                skin.GetParent()?.GetParent()?.GetParent() is not PrologueSector sector || !Live(sector) ||
                sector.GetNodeOrNull<CanyonPlatformVisual>("VisualRoot/GameplayReadable/" + skin.Name) != skin ||
                sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/" + skin.Name) is not { } body || !Live(body) ||
                skin.GetNodeOrNull<Polygon2D>("ReadableEdge") is not { } edge || !Live(edge) || !edge.IsVisibleInTree() ||
                skin.GetViewport() != GetViewport() || body.GetViewport() != GetViewport() ||
                skin.GetWorld2D().GetRid() != GetWorld2D().GetRid() || body.GetWorld2D().GetRid() != GetWorld2D().GetRid() ||
                skin.GetCanvas() != body.GetCanvas() || !Aligned(skin.GetCanvasTransform(), GetCanvasTransform())) continue;
            Color color = edge.Color;
            if (!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) ||
                !float.IsFinite(color.A) || color.A <= 0f || !_bodies.Add(body.GetInstanceId())) continue;
            color.A = 1f;
            foreach (HeadlampOccluder shape in frame.Shapes)
            {
                if (shape.BodyInstanceId != body.GetInstanceId()) continue;
                Transform2D forward = shape.WorldToLocal.AffineInverse();
                Rect2 rect = shape.LocalBounds;
                Vector2 a = forward * rect.Position;
                Vector2 b = forward * new Vector2(rect.End.X, rect.Position.Y);
                // No AABB approximation for rotated/mirrored vertical faces.
                // Horizontal X reflection remains the same real top surface.
                if (!HeadlampOcclusionSampler.Finite(a) || !HeadlampOcclusionSampler.Finite(b) ||
                    Mathf.Abs(a.Y - b.Y) > .001f || Mathf.Abs(forward.Y.X) > .001f || forward.Y.Y <= 0f) continue;
                float left = Mathf.Min(a.X, b.X), right = Mathf.Max(a.X, b.X), top = a.Y;
                if (right - left < 40f || actor.Y >= top - .5f) continue;
                double distanceY = top - actor.Y;
                double horizontalSquared = (double)radius * radius - distanceY * distanceY;
                if (horizontalSquared <= 0d) continue;
                float reach = (float)Math.Sqrt(horizontalSquared);
                float start = Mathf.Max(left, actor.X - reach), end = Mathf.Min(right, actor.X + reach);
                float nearest = Mathf.Clamp(actor.X, left, right);
                for (float x = start; x < end; x += 8f)
                {
                    float stop = Mathf.Min(x + 8f, end), middle = (x + stop) * .5f;
                    // Short end marks and the nearest local patch retain the real
                    // gap/edge without outlining every distant platform in full.
                    if (middle > left + 40f && middle < right - 40f && Mathf.Abs(middle - nearest) > 20f) continue;
                    Vector2 from = new(x, top), to = new(stop, top);
                    if (!frame.CanSeeSurfacePatch(actor, from, to, 2f, shape)) continue;
                    Vector2 p0 = ToLocal(new Vector2(x, top));
                    Vector2 p1 = ToLocal(new Vector2(stop, top));
                    Vector2 p2 = ToLocal(new Vector2(stop, top + 2f));
                    Vector2 p3 = ToLocal(new Vector2(x, top + 2f));
                    if (!HeadlampOcclusionSampler.Finite(p0) || !HeadlampOcclusionSampler.Finite(p1) ||
                        !HeadlampOcclusionSampler.Finite(p2) || !HeadlampOcclusionSampler.Finite(p3)) continue;
                    Rect2 viewport = GetViewport().GetVisibleRect();
                    Transform2D canvas = body.GetCanvasTransform();
                    Vector2 c0 = canvas * new Vector2(x, top), c1 = canvas * new Vector2(stop, top);
                    Vector2 c2 = canvas * new Vector2(stop, top + 2f), c3 = canvas * new Vector2(x, top + 2f);
                    Vector2 min = c0.Min(c1).Min(c2).Min(c3), max = c0.Max(c1).Max(c2).Max(c3);
                    if (!viewport.Intersects(new Rect2(min, max - min))) continue;
                    // This is an affine quad, never an arbitrary polygon. At the
                    // circular range boundary its width can be one float ULP;
                    // generic polygon triangulation then loses its signed area
                    // at large canyon coordinates. Submit the known primitive
                    // without widening the cue or changing range/occlusion.
                    RenderingServer.CanvasItemAddPrimitive(GetCanvasItem(), [p0,p1,p2,p3], [color], [], default);
                    PresentedSegmentCount++;
                }
            }
        }
    }

    public void Clear()
    {
        PresentedSegmentCount = 0; LastOcclusionFrame = null; _bodies.Clear();
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }
    public override void _ExitTree() { Clear(); RequestReady(); }

    private static bool Aligned(Transform2D a, Transform2D b) =>
        HeadlampOcclusionSampler.Finite(a.X) && HeadlampOcclusionSampler.Finite(a.Y) &&
        HeadlampOcclusionSampler.Finite(a.Origin) && HeadlampOcclusionSampler.Finite(b.X) &&
        HeadlampOcclusionSampler.Finite(b.Y) && HeadlampOcclusionSampler.Finite(b.Origin) &&
        a.X.DistanceTo(b.X) < .001f && a.Y.DistanceTo(b.Y) < .001f && a.Origin.DistanceTo(b.Origin) < .01f;
    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }
}
