using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

public readonly record struct HeadlampOccluder(Transform2D WorldToLocal, Rect2 LocalBounds,
    ulong BodyInstanceId = 0, uint OwnerId = 0, int ShapeIndex = 0);

public sealed class HeadlampOcclusionFrame
{
    public IReadOnlyList<HeadlampOccluder> Shapes { get; }
    public bool IsComplete { get; }
    public string Failure { get; }
    public int PhysicsQueries => 0;

    internal HeadlampOcclusionFrame(HeadlampOccluder[] shapes, bool complete, string failure = "")
    {
        Shapes = Array.AsReadOnly(shapes);
        IsComplete = complete;
        Failure = failure;
    }

    public bool CanSee(Vector2 origin, Vector2 point)
    {
        if (!IsComplete || !HeadlampOcclusionSampler.Finite(origin) || !HeadlampOcclusionSampler.Finite(point))
            return false;
        foreach (HeadlampOccluder shape in Shapes)
            if (Intersects(shape, origin, point)) return false;
        return true;
    }

    /// <summary>Every point on a segment must be visible, not just sampled rays.</summary>
    public bool CanSeeSegment(Vector2 origin, Vector2 from, Vector2 to)
    {
        if (!IsComplete || !HeadlampOcclusionSampler.Finite(origin) ||
            !HeadlampOcclusionSampler.Finite(from) || !HeadlampOcclusionSampler.Finite(to)) return false;
        foreach (HeadlampOccluder shape in Shapes)
            if (IntersectsWedge(shape, origin, from, to)) return false;
        return true;
    }

    /// <summary>Visibility of all four ordered edges, without terminal-face exemptions.</summary>
    public bool CanSeeQuad(Vector2 origin, Vector2 first, Vector2 second, Vector2 third, Vector2 fourth)
    {
        if (!IsComplete || !HeadlampOcclusionSampler.Finite(origin) ||
            !HeadlampOcclusionSampler.Finite(first) || !HeadlampOcclusionSampler.Finite(second) ||
            !HeadlampOcclusionSampler.Finite(third) || !HeadlampOcclusionSampler.Finite(fourth)) return false;
        // Same four visibility wedges, with each blocker and local corner reused
        // within this query only. No cached visibility or terminal-owner exception.
        foreach (HeadlampOccluder shape in Shapes)
            if (IntersectsQuad(shape, origin, first, second, third, fourth)) return false;
        return true;
    }

    /// <summary>
    /// A contained top-face mark can touch its own destination slab, but only
    /// after proving its entire patch is inside that exact snapshot member and
    /// the origin is above it. Every other owner blocks the whole drawn patch.
    /// </summary>
    public bool CanSeeSurfacePatch(Vector2 origin, Vector2 from, Vector2 to,
        float thickness, HeadlampOccluder surface)
    {
        if (!IsComplete || surface.BodyInstanceId == 0 || !HeadlampOcclusionSampler.Finite(origin) ||
            !HeadlampOcclusionSampler.Finite(from) || !HeadlampOcclusionSampler.Finite(to) ||
            !float.IsFinite(thickness) || thickness <= 0f) return false;
        bool member = false;
        foreach (HeadlampOccluder shape in Shapes) if (shape == surface) { member = true; break; }
        if (!member) return false;
        Vector2 bottomFrom = from + Vector2.Down * thickness, bottomTo = to + Vector2.Down * thickness;
        if (!HeadlampOcclusionSampler.Finite(bottomFrom) || !HeadlampOcclusionSampler.Finite(bottomTo)) return false;
        var actor = Local(surface.WorldToLocal, origin);
        var a = Local(surface.WorldToLocal, from); var b = Local(surface.WorldToLocal, to);
        var c = Local(surface.WorldToLocal, bottomFrom); var d = Local(surface.WorldToLocal, bottomTo);
        Rect2 rect = surface.LocalBounds;
        // Outside-to-top contact only: never authorize an underside or an origin
        // inside the body. A tiny outward rounding tolerance cannot enter its slab.
        if (actor.Y >= rect.Position.Y || a.Y > rect.Position.Y || b.Y > rect.Position.Y ||
            a.Y < rect.Position.Y - .001d || b.Y < rect.Position.Y - .001d ||
            a.X < rect.Position.X || b.X < rect.Position.X || a.X > rect.End.X || b.X > rect.End.X ||
            c.X < rect.Position.X || d.X < rect.Position.X || c.X > rect.End.X || d.X > rect.End.X ||
            c.Y < rect.Position.Y || d.Y < rect.Position.Y || c.Y > rect.End.Y || d.Y > rect.End.Y) return false;
        foreach (HeadlampOccluder shape in Shapes)
        {
            if (shape == surface) continue; // Proven terminal face/contained style only, not a general LOS exclusion.
            // The four triangles cover the visibility hull of the actual drawn
            // quad, including thin blockers between its top and bottom edges.
            if (IntersectsQuad(shape, origin, from, to, bottomTo, bottomFrom)) return false;
        }
        return true;
    }

    /// <summary>
    /// A thin local-space mark on one exact native rectangle face. Only this
    /// snapshot row is terminal contact; every other owner/index still occludes
    /// the whole quad. Works for rotated/mirrored bodies without a world AABB.
    /// </summary>
    public bool CanSeeBodyFacePatch(Vector2 origin, HeadlampOccluder surface,
        Vector2 fromLocal, Vector2 toLocal, float thickness)
    {
        Rect2 rect=surface.LocalBounds;
        Transform2D inverse=surface.WorldToLocal;
        if (!IsComplete || surface.BodyInstanceId==0 || !HeadlampOcclusionSampler.Finite(origin) ||
            !HeadlampOcclusionSampler.Finite(fromLocal) || !HeadlampOcclusionSampler.Finite(toLocal) ||
            fromLocal==toLocal || !float.IsFinite(thickness) || thickness<=0f || thickness>8f ||
            !HeadlampOcclusionSampler.Finite(rect.Position) || !HeadlampOcclusionSampler.Finite(rect.Size) ||
            !HeadlampOcclusionSampler.Finite(rect.End) || rect.Size.X<=0f || rect.Size.Y<=0f ||
            !HeadlampOcclusionSampler.Finite(inverse.X) || !HeadlampOcclusionSampler.Finite(inverse.Y) ||
            !HeadlampOcclusionSampler.Finite(inverse.Origin) || !float.IsFinite(inverse.Determinant()) ||
            inverse.Determinant()==0f) return false;
        bool member=false;
        foreach (HeadlampOccluder shape in Shapes) if (shape==surface) { member=true; break; }
        if (!member) return false;
        var actor=Local(inverse,origin);
        Vector2 inward;
        if (fromLocal.X==rect.Position.X && toLocal.X==rect.Position.X && actor.X<rect.Position.X)
            inward=Vector2.Right*thickness;
        else if (fromLocal.X==rect.End.X && toLocal.X==rect.End.X && actor.X>rect.End.X)
            inward=Vector2.Left*thickness;
        else if (fromLocal.Y==rect.Position.Y && toLocal.Y==rect.Position.Y && actor.Y<rect.Position.Y)
            inward=Vector2.Down*thickness;
        else if (fromLocal.Y==rect.End.Y && toLocal.Y==rect.End.Y && actor.Y>rect.End.Y)
            inward=Vector2.Up*thickness;
        else return false;
        Vector2[] local=[fromLocal,toLocal,toLocal+inward,fromLocal+inward];
        Transform2D forward=inverse.AffineInverse();
        if (!HeadlampOcclusionSampler.Finite(forward.X) || !HeadlampOcclusionSampler.Finite(forward.Y) ||
            !HeadlampOcclusionSampler.Finite(forward.Origin)) return false;
        Vector2[] world=new Vector2[4];
        for (int index=0;index<4;index++)
        {
            Vector2 point=local[index];
            if (!HeadlampOcclusionSampler.Finite(point) || point.X<rect.Position.X || point.X>rect.End.X ||
                point.Y<rect.Position.Y || point.Y>rect.End.Y) return false;
            world[index]=forward*point;
            if (!HeadlampOcclusionSampler.Finite(world[index])) return false;
        }
        foreach (HeadlampOccluder shape in Shapes)
        {
            if (shape==surface) continue;
            if (IntersectsQuad(shape,origin,world[0],world[1],world[2],world[3])) return false;
        }
        return true;
    }

    private static bool IntersectsWedge(HeadlampOccluder shape, Vector2 origin, Vector2 from, Vector2 to)
    {
        // Reuse the same double local points for all three edges and containment.
        // Keep the first-edge early exit: blocked wedges still transform only two
        // points, while a clear wedge transforms three instead of nine.
        var a = Local(shape.WorldToLocal, origin); var b = Local(shape.WorldToLocal, from);
        Rect2 rect = shape.LocalBounds;
        Vector2 minimum = rect.Position, maximum = rect.End;
        if (IntersectsLocal(minimum, maximum, a, b)) return true;
        var c = Local(shape.WorldToLocal, to);
        return IntersectsWedgeTail(minimum, maximum, a, b, c);
    }

    private static bool IntersectsQuad(HeadlampOccluder shape, Vector2 origin,
        Vector2 first, Vector2 second, Vector2 third, Vector2 fourth)
    {
        // Same ordered four triangles as before, but each local point is computed
        // once per blocker. Calculate later corners only after earlier tests miss.
        // The caller's float world-space quad and Rect.End rounding stay intact.
        Transform2D inverse = shape.WorldToLocal;
        Rect2 rect = shape.LocalBounds;
        Vector2 minimum = rect.Position, maximum = rect.End;
        var a = Local(inverse, origin); var b = Local(inverse, first);
        if (IntersectsLocal(minimum, maximum, a, b)) return true;
        var c = Local(inverse, second);
        if (IntersectsWedgeTail(minimum, maximum, a, b, c)) return true;
        // A missed tail has already tested the next triangle's first radial
        // edge with these exact double arguments. Reuse that false result.
        var d = Local(inverse, third);
        if (IntersectsWedgeTail(minimum, maximum, a, c, d)) return true;
        var e = Local(inverse, fourth);
        if (IntersectsWedgeTail(minimum, maximum, a, d, e)) return true;
        // Both radial edges of the closing triangle were proven clear above.
        return IntersectsLocal(minimum, maximum, e, b) ||
            IntersectsWedgeInterior(minimum, maximum, a, e, b);
    }

    // The first radial edge has already been checked; preserve the remaining
    // edge order, closed contact, degeneracy and rectangle corner order.
    private static bool IntersectsWedgeTail(Vector2 minimum, Vector2 maximum,
        (double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
    {
        if (IntersectsLocal(minimum, maximum, a, c) || IntersectsLocal(minimum, maximum, b, c)) return true;
        return IntersectsWedgeInterior(minimum, maximum, a, b, c);
    }

    private static bool IntersectsWedgeInterior(Vector2 minimum, Vector2 maximum,
        (double X, double Y) a, (double X, double Y) b, (double X, double Y) c)
    {
        double area = Cross(a, b, c);
        if (area == 0d) return false; // Edges already cover a degenerate wedge.
        return Inside((minimum.X, minimum.Y), a, b, c, area) ||
            Inside((maximum.X, minimum.Y), a, b, c, area) ||
            Inside((maximum.X, maximum.Y), a, b, c, area) ||
            Inside((minimum.X, maximum.Y), a, b, c, area);
    }

    private static bool IntersectsLocal(Vector2 minimum, Vector2 maximum,
        (double X, double Y) from, (double X, double Y) to)
    {
        double enter = 0d, exit = 1d;
        return ClipAxis(from.X, to.X - from.X, minimum.X, maximum.X, ref enter, ref exit) &&
            ClipAxis(from.Y, to.Y - from.Y, minimum.Y, maximum.Y, ref enter, ref exit);
    }

    private static (double X, double Y) Local(Transform2D inverse, Vector2 point) =>
        ((double)inverse.X.X * point.X + (double)inverse.Y.X * point.Y + inverse.Origin.X,
         (double)inverse.X.Y * point.X + (double)inverse.Y.Y * point.Y + inverse.Origin.Y);

    private static double Cross((double X, double Y) a, (double X, double Y) b, (double X, double Y) c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool Inside((double X, double Y) point, (double X, double Y) a,
        (double X, double Y) b, (double X, double Y) c, double area) => area > 0d
        ? Cross(a, b, point) >= 0d && Cross(b, c, point) >= 0d && Cross(c, a, point) >= 0d
        : Cross(a, b, point) <= 0d && Cross(b, c, point) <= 0d && Cross(c, a, point) <= 0d;

    private static bool Intersects(HeadlampOccluder shape, Vector2 origin, Vector2 point)
    {
        Transform2D inverse = shape.WorldToLocal;
        // Use double products so finite float coordinates and transforms cannot
        // overflow before the slab comparison. Transform2D stores basis columns.
        double x0 = (double)inverse.X.X * origin.X + (double)inverse.Y.X * origin.Y + inverse.Origin.X;
        double y0 = (double)inverse.X.Y * origin.X + (double)inverse.Y.Y * origin.Y + inverse.Origin.Y;
        double x1 = (double)inverse.X.X * point.X + (double)inverse.Y.X * point.Y + inverse.Origin.X;
        double y1 = (double)inverse.X.Y * point.X + (double)inverse.Y.Y * point.Y + inverse.Origin.Y;
        double enter = 0d, exit = 1d;
        Rect2 rect = shape.LocalBounds;
        return ClipAxis(x0, x1 - x0, rect.Position.X, rect.End.X, ref enter, ref exit) &&
            ClipAxis(y0, y1 - y0, rect.Position.Y, rect.End.Y, ref enter, ref exit);
    }

    private static bool ClipAxis(double origin, double direction, double minimum, double maximum,
        ref double enter, ref double exit)
    {
        if (direction == 0d) return origin >= minimum && origin <= maximum;
        double first = (minimum - origin) / direction, last = (maximum - origin) / direction;
        if (first > last) (first, last) = (last, first);
        enter = Math.Max(enter, first);
        exit = Math.Min(exit, last);
        return enter <= exit;
    }
}

/// <summary>
/// Exact live World rectangle snapshots. Sparse angular contours are diagnostic
/// only: they cannot authorize light through a thin wall between sampled rays.
/// </summary>
public sealed class HeadlampOcclusionSampler : IDisposable
{
    public const int MaximumShapes = 256;
    private readonly Node2D _scope;
    private readonly SceneTree _tree;
    private readonly int _capacity;
    private readonly HashSet<PhysicsBody2D> _candidates = new();
    private bool _disposed;

    public HeadlampOcclusionSampler(Node2D scope, int capacity = MaximumShapes)
    {
        if (!Live(scope)) throw new ArgumentException("Occlusion scope must be in the live tree.", nameof(scope));
        if (capacity is < 1 or > MaximumShapes) throw new ArgumentOutOfRangeException(nameof(capacity));
        _scope = scope;
        _tree = scope.GetTree();
        _capacity = capacity;
        IndexSubtree(scope);
        _tree.NodeAdded += OnNodeAdded;
        _tree.NodeRemoved += OnNodeRemoved;
    }

    public HeadlampOcclusionFrame Capture()
    {
        if (_disposed || !Live(_scope) || _scope.GetTree() != _tree)
            return new(Array.Empty<HeadlampOccluder>(), false, "scope-unavailable");
        var shapes = new List<HeadlampOccluder>(_capacity);
        string failure = string.Empty;
        foreach (PhysicsBody2D body in _candidates)
        {
            if (!Live(body) || body.GetTree() != _tree ||
                (body != _scope && !_scope.IsAncestorOf(body)) ||
                (body.CollisionLayer & CombatCollisionLayers.World) == 0 ||
                body.GetWorld2D().GetRid() != _scope.GetWorld2D().GetRid() ||
                body.GetCanvas() != _scope.GetCanvas() || !BodyIsActive(body)) continue;
            // Shape owners are native CollisionObject state, not necessarily
            // CollisionShape2D children. Re-read owners and their transforms so
            // runtime additions, deferred doors and moving geometry stay current.
            foreach (uint owner in body.GetShapeOwners())
            {
                if (body.IsShapeOwnerDisabled(owner)) continue;
                Transform2D transform = body.GlobalTransform * body.ShapeOwnerGetTransform(owner);
                for (int index = 0; index < body.ShapeOwnerGetShapeCount(owner); index++)
                {
                    if (body.ShapeOwnerGetShape(owner, index) is not RectangleShape2D rectangle)
                    {
                        failure = "unsupported-active-world-shape";
                        continue;
                    }
                    Rect2 bounds = rectangle.GetRect();
                    if (!Finite(bounds.Position) || !Finite(bounds.Size) || !Finite(bounds.End) ||
                        bounds.Size.X <= 0f || bounds.Size.Y <= 0f ||
                        !Finite(transform.X) || !Finite(transform.Y) || !Finite(transform.Origin) ||
                        !float.IsFinite(transform.Determinant()) || transform.Determinant() == 0f)
                    {
                        failure = "invalid-world-rectangle-transform";
                        continue;
                    }
                    Transform2D inverse = transform.AffineInverse();
                    if (!Finite(inverse.X) || !Finite(inverse.Y) || !Finite(inverse.Origin))
                    {
                        failure = "invalid-world-rectangle-inverse";
                        continue;
                    }
                    if (shapes.Count == _capacity)
                    {
                        failure = "world-geometry-capacity-exceeded";
                        continue;
                    }
                    shapes.Add(new HeadlampOccluder(inverse, bounds, body.GetInstanceId(), owner, index));
                }
            }
        }
        return new(shapes.ToArray(), failure.Length == 0, failure);
    }

    private static bool BodyIsActive(PhysicsBody2D body)
    {
        Node.ProcessModeEnum mode = Node.ProcessModeEnum.Inherit;
        for (Node node = body; node != null; node = node.GetParent())
        {
            if (node.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
            if (mode == Node.ProcessModeEnum.Inherit && node.ProcessMode != Node.ProcessModeEnum.Inherit)
                mode = node.ProcessMode;
        }
        return mode != Node.ProcessModeEnum.Disabled || body.DisableMode != CollisionObject2D.DisableModeEnum.Remove;
    }

    private void IndexSubtree(Node node)
    {
        if (node is PhysicsBody2D body) _candidates.Add(body);
        for (int index = 0; index < node.GetChildCount(includeInternal: true); index++)
            IndexSubtree(node.GetChild(index, includeInternal: true));
    }

    private void OnNodeAdded(Node node)
    {
        if (!_disposed && Live(_scope) && (node == _scope || _scope.IsAncestorOf(node)) &&
            node is PhysicsBody2D body) _candidates.Add(body);
    }

    private void OnNodeRemoved(Node node)
    {
        if (node is PhysicsBody2D body) _candidates.Remove(body);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (GodotObject.IsInstanceValid(_tree))
        {
            _tree.NodeAdded -= OnNodeAdded;
            _tree.NodeRemoved -= OnNodeRemoved;
        }
        _candidates.Clear();
    }

    internal static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);
    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
}
