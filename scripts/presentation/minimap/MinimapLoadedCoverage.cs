using System;
using Godot;

namespace Knight.Presentation.Minimap;

// Exact finite axis-aligned union proof, not endpoint samples. The convex hull
// is split at every polygon/coverage x edge. Within each slab the rectangle
// union is constant in y and each hull edge is linear; its endpoint extrema
// therefore prove coverage of the whole slab. Adjacent closed bounds may join.
internal static class MinimapLoadedCoverage
{
    private readonly record struct Point(double X, double Y);
    internal static bool Cell(Vector2 actor, Rect2 cell, ReadOnlySpan<Rect2> loaded)
    {
        Span<Point> points = stackalloc Point[5];
        points[0] = new(actor.X, actor.Y); points[1] = new(cell.Position.X, cell.Position.Y);
        points[2] = new(cell.End.X, cell.Position.Y); points[3] = new(cell.End.X, cell.End.Y);
        points[4] = new(cell.Position.X, cell.End.Y);
        return Covered(points, loaded);
    }
    internal static bool Boundary(Vector2 actor, Vector2 a, Vector2 b, ReadOnlySpan<Rect2> loaded)
    {
        Span<Point> points = stackalloc Point[3];
        points[0] = new(actor.X, actor.Y); points[1] = new(a.X, a.Y); points[2] = new(b.X, b.Y);
        return Covered(points, loaded);
    }
    private static bool Covered(Span<Point> points, ReadOnlySpan<Rect2> loaded)
    {
        if (loaded.Length is < 1 or > ExplorationMapModel.MaximumLoadedBounds) return false;
        for (int i = 1; i < points.Length; i++)
        { Point p = points[i]; int j = i; while (j > 0 && Compare(points[j - 1], p) > 0) { points[j] = points[j - 1]; j--; } points[j] = p; }
        Span<Point> hull = stackalloc Point[10]; int count = 0;
        for (int i = 0; i < points.Length; i++)
        { while (count >= 2 && Cross(hull[count - 2], hull[count - 1], points[i]) <= 0) count--; hull[count++] = points[i]; }
        int lower = count;
        for (int i = points.Length - 2; i >= 0; i--)
        { while (count > lower && Cross(hull[count - 2], hull[count - 1], points[i]) <= 0) count--; hull[count++] = points[i]; }
        count--; if (count < 3) return false;
        Span<double> edges = stackalloc double[138]; int edgeCount = 0;
        for (int i = 0; i < count; i++) edges[edgeCount++] = hull[i].X;
        double minX = points[0].X, maxX = points[^1].X;
        foreach (Rect2 rect in loaded)
        {
            if (!MinimapTerrainCatalog.Valid(rect)) return false;
            if (rect.Position.X > minX && rect.Position.X < maxX) edges[edgeCount++] = rect.Position.X;
            if (rect.End.X > minX && rect.End.X < maxX) edges[edgeCount++] = rect.End.X;
        }
        edges[..edgeCount].Sort();
        for (int slab = 1; slab < edgeCount; slab++)
        {
            double left = edges[slab - 1], right = edges[slab]; if (right <= left) continue;
            double low = double.PositiveInfinity, high = double.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                Point a = hull[i], b = hull[(i + 1) % count];
                if (a.X >= left && a.X <= right) { low = Math.Min(low, a.Y); high = Math.Max(high, a.Y); }
                IncludeIntersection(a, b, left, ref low, ref high); IncludeIntersection(a, b, right, ref low, ref high);
            }
            if (!double.IsFinite(low) || !double.IsFinite(high)) return false;
            double cursor = low;
            for (int step = 0; step <= loaded.Length; step++)
            {
                double end = cursor;
                foreach (Rect2 rect in loaded)
                    if (rect.Position.X <= left && rect.End.X >= right && rect.Position.Y <= cursor && rect.End.Y >= cursor)
                        end = Math.Max(end, rect.End.Y);
                if (end >= high) break;
                if (end <= cursor || step == loaded.Length) return false;
                cursor = end;
            }
        }
        return true;
    }
    private static void IncludeIntersection(Point a, Point b, double x, ref double low, ref double high)
    {
        if (a.X == b.X || x < Math.Min(a.X, b.X) || x > Math.Max(a.X, b.X)) return;
        double y = a.Y + (b.Y - a.Y) * ((x - a.X) / (b.X - a.X));
        low = Math.Min(low, y); high = Math.Max(high, y);
    }
    private static int Compare(Point a, Point b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
    private static double Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}
