using System;
using Godot;
using Knight.Presentation.Fog;

namespace Knight.Presentation.Minimap;

public static class MinimapVisibility
{
    private readonly record struct Point(double X, double Y)
    {
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    }

    public static bool CellAllowed(Vector2 actor, Rect2 cell, HeadlampOcclusionFrame frame)
        => CellAllowed(actor, cell, frame, out _);

    public static bool CellAllowed(Vector2 actor, Rect2 cell, HeadlampOcclusionFrame frame,
        out int finiteOccluderTests)
    {
        finiteOccluderTests = 0;
        if (!Finite(actor) || !Valid(cell) || !Valid(frame)) return false;
        Span<Point> points = stackalloc Point[5];
        points[0] = new(actor.X, actor.Y);
        points[1] = new(cell.Position.X, cell.Position.Y);
        points[2] = new(cell.End.X, cell.Position.Y);
        points[3] = new(cell.End.X, cell.End.Y);
        points[4] = new(cell.Position.X, cell.End.Y);
        Span<Point> hull = stackalloc Point[10];
        int count = ConvexHull(points, hull);
        return count >= 3 && SweepAllowed(hull[..count], frame, default, false, ref finiteOccluderTests);
    }

    public static bool BoundaryAllowed(Vector2 actor, MinimapSolid solid,
        MinimapBoundary boundary, HeadlampOcclusionFrame frame)
        => BoundaryAllowed(actor, solid, boundary, frame, out _);

    public static bool BoundaryAllowed(Vector2 actor, MinimapSolid solid,
        MinimapBoundary boundary, HeadlampOcclusionFrame frame, out int finiteOccluderTests)
    {
        finiteOccluderTests = 0;
        if (!Finite(actor) || !Valid(solid.LocalBounds) || !Valid(solid.LocalToWorld) ||
            !Valid(frame) || boundary.ShapeId < 0 || !Finite(boundary.FromLocal) ||
            !Finite(boundary.ToLocal) || boundary.FromLocal == boundary.ToLocal ||
            !Finite(boundary.OutwardLocal)) return false;

        Point localActor = Inverse(solid.LocalToWorld, new(actor.X, actor.Y));
        Rect2 rect = solid.LocalBounds;
        Vector2 from = boundary.FromLocal, to = boundary.ToLocal;
        if (!InsideClosed(rect, from) || !InsideClosed(rect, to)) return false;
        // Validate an entire exact face segment AND the exterior half-plane.
        // This proves the wedge only touches this target at its terminal face;
        // it never licenses skipping the target's interior intersection check.
        bool front = boundary.OutwardLocal == Vector2.Left && from.X == rect.Position.X &&
                to.X == rect.Position.X && localActor.X < rect.Position.X - Tolerance(rect.Position.X) ||
            boundary.OutwardLocal == Vector2.Right && from.X == rect.End.X &&
                to.X == rect.End.X && localActor.X > rect.End.X + Tolerance(rect.End.X) ||
            boundary.OutwardLocal == Vector2.Up && from.Y == rect.Position.Y &&
                to.Y == rect.Position.Y && localActor.Y < rect.Position.Y - Tolerance(rect.Position.Y) ||
            boundary.OutwardLocal == Vector2.Down && from.Y == rect.End.Y &&
                to.Y == rect.End.Y && localActor.Y > rect.End.Y + Tolerance(rect.End.Y);
        if (!front) return false;

        Transform2D expectedInverse = solid.LocalToWorld.AffineInverse();
        bool member = false;
        for (int i = 0; i < frame.Shapes.Count; i++)
            if (SameTarget(frame.Shapes[i], expectedInverse, rect)) { member = true; break; }
        if (!member) return false; // Catalog knowledge alone is not current visibility.

        Span<Point> points = stackalloc Point[3];
        points[0] = new(actor.X, actor.Y);
        points[1] = Transform(solid.LocalToWorld, new(from.X, from.Y));
        points[2] = Transform(solid.LocalToWorld, new(to.X, to.Y));
        Point segment = points[2] - points[1];
        if (segment.X * segment.X + segment.Y * segment.Y > 64d + 1e-8d) return false;
        Span<Point> hull = stackalloc Point[6];
        int count = ConvexHull(points, hull);
        return count == 3 && SweepAllowed(hull[..count], frame, solid, true, ref finiteOccluderTests);
    }

    private static bool SweepAllowed(ReadOnlySpan<Point> hull, HeadlampOcclusionFrame frame,
        MinimapSolid target, bool terminalContact, ref int finiteOccluderTests)
    {
        Transform2D targetInverse = terminalContact ? target.LocalToWorld.AffineInverse() : default;
        Span<Point> localHull = stackalloc Point[5];
        for (int i = 0; i < frame.Shapes.Count; i++)
        {
            HeadlampOccluder shape = frame.Shapes[i];
            if (!Valid(shape.WorldToLocal) || !Valid(shape.LocalBounds)) return false;
            bool isTarget = terminalContact && SameTarget(shape, targetInverse, target.LocalBounds);
            for (int point = 0; point < hull.Length; point++)
            {
                // For the proven exact target row use its original forward
                // transform's double inverse, avoiding two float round trips at
                // a rotated terminal face. Target is still tested, not excluded.
                localHull[point] = isTarget ? Inverse(target.LocalToWorld, hull[point]) :
                    Transform(shape.WorldToLocal, hull[point]);
                if (!Finite(localHull[point])) return false;
            }
            finiteOccluderTests++;
            if (Intersects(localHull[..hull.Length], shape.LocalBounds, isTarget)) return false;
        }
        return true;
    }

    private static bool SameTarget(HeadlampOccluder shape, Transform2D inverse, Rect2 rect) =>
        shape.WorldToLocal == inverse && shape.LocalBounds == rect;

    // SAT proves disjointness against the entire convex sweep. Rect axes and
    // every hull-edge normal are necessary; corner ray samples are insufficient.
    private static bool Intersects(ReadOnlySpan<Point> hull, Rect2 rect, bool knownTerminal)
    {
        if (Separated(hull, rect, new(1, 0), knownTerminal) ||
            Separated(hull, rect, new(0, 1), knownTerminal)) return false;
        for (int i = 0; i < hull.Length; i++)
        {
            Point edge = hull[(i + 1) % hull.Length] - hull[i];
            if (Separated(hull, rect, new(-edge.Y, edge.X), knownTerminal)) return false;
        }
        return true;
    }

    private static bool Separated(ReadOnlySpan<Point> hull, Rect2 rect, Point axis, bool knownTerminal)
    {
        if (axis.X == 0d && axis.Y == 0d) return false;
        double polygonMin = double.PositiveInfinity, polygonMax = double.NegativeInfinity;
        foreach (Point point in hull)
        {
            double value = Dot(point, axis);
            polygonMin = Math.Min(polygonMin, value); polygonMax = Math.Max(polygonMax, value);
        }
        double a = rect.Position.X * axis.X, b = rect.End.X * axis.X;
        double c = rect.Position.Y * axis.Y, d = rect.End.Y * axis.Y;
        double rectMin = Math.Min(a, b) + Math.Min(c, d);
        double rectMax = Math.Max(a, b) + Math.Max(c, d);
        double gap = Math.Max(rectMin - polygonMax, polygonMin - rectMax);
        double tolerance = Tolerance(Math.Max(Math.Abs(rectMin), Math.Max(Math.Abs(rectMax),
            Math.Max(Math.Abs(polygonMin), Math.Abs(polygonMax)))));
        // Only the separately proven exterior target half-plane may touch.
        // For every other body a touching/uncertain projection remains blocked.
        return knownTerminal ? gap >= -tolerance : gap > tolerance;
    }

    private static int ConvexHull(Span<Point> points, Span<Point> hull)
    {
        for (int i = 1; i < points.Length; i++)
        {
            Point value = points[i]; int at = i;
            while (at > 0 && Compare(points[at - 1], value) > 0)
            { points[at] = points[at - 1]; at--; }
            points[at] = value;
        }
        int unique = 0;
        for (int i = 0; i < points.Length; i++)
            if (unique == 0 || points[i] != points[unique - 1]) points[unique++] = points[i];
        if (unique < 3) return 0;
        int count = 0;
        for (int i = 0; i < unique; i++)
        {
            while (count >= 2 && Cross(hull[count - 1] - hull[count - 2], points[i] - hull[count - 1]) <= 0d) count--;
            hull[count++] = points[i];
        }
        int lower = count;
        for (int i = unique - 2; i >= 0; i--)
        {
            while (count > lower && Cross(hull[count - 1] - hull[count - 2], points[i] - hull[count - 1]) <= 0d) count--;
            hull[count++] = points[i];
        }
        return count - 1;
    }

    private static int Compare(Point a, Point b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
    private static double Cross(Point a, Point b) => a.X * b.Y - a.Y * b.X;
    private static double Dot(Point a, Point b) => a.X * b.X + a.Y * b.Y;
    private static double Tolerance(double scale) => 1e-9d * (1d + Math.Abs(scale));
    private static Point Transform(Transform2D transform, Point point) =>
        new(transform.X.X * point.X + transform.Y.X * point.Y + transform.Origin.X,
            transform.X.Y * point.X + transform.Y.Y * point.Y + transform.Origin.Y);
    private static Point Inverse(Transform2D transform, Point point)
    {
        double x = point.X - transform.Origin.X, y = point.Y - transform.Origin.Y;
        double determinant = (double)transform.X.X * transform.Y.Y - (double)transform.Y.X * transform.X.Y;
        return new((transform.Y.Y * x - transform.Y.X * y) / determinant,
            (-transform.X.Y * x + transform.X.X * y) / determinant);
    }
    private static bool Finite(Point value) => double.IsFinite(value.X) && double.IsFinite(value.Y);
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private static bool Valid(HeadlampOcclusionFrame frame) => frame != null && frame.IsComplete &&
        frame.Shapes.Count <= HeadlampOcclusionSampler.MaximumShapes;
    private static bool Valid(Rect2 rect) => Finite(rect.Position) && Finite(rect.Size) && Finite(rect.End) &&
        rect.Size.X > 0f && rect.Size.Y > 0f && rect.End.X > rect.Position.X && rect.End.Y > rect.Position.Y;
    private static bool Valid(Transform2D transform)
    {
        if (!Finite(transform.X) || !Finite(transform.Y) || !Finite(transform.Origin)) return false;
        double a = (double)transform.X.X * transform.Y.Y, b = (double)transform.Y.X * transform.X.Y;
        return Math.Abs(a - b) > 1e-12d * Math.Max(1d, Math.Abs(a) + Math.Abs(b));
    }
    private static bool InsideClosed(Rect2 rect, Vector2 point) => point.X >= rect.Position.X &&
        point.X <= rect.End.X && point.Y >= rect.Position.Y && point.Y <= rect.End.Y;
}
