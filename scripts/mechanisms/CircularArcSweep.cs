using System;
using Godot;

namespace Knight.Mechanisms;

// Continuous circular-arc distance, not chord/AABB authorization or sampled poses.
public static class CircularArcSweep
{
    private readonly record struct D(double X, double Y)
    {
        public static D operator +(D a, D b) => new(a.X + b.X, a.Y + b.Y);
        public static D operator -(D a, D b) => new(a.X - b.X, a.Y - b.Y);
        public static D operator *(D a, double b) => new(a.X * b, a.Y * b);
        public double Dot(D b) => X * b.X + Y * b.Y;
        public double LengthSquared => Dot(this);
    }
    private readonly record struct Arc(D Center, double Radius, double Lo, double Hi)
    {
        public D Relative(double angle) => new(Radius * Math.Cos(angle), Radius * Math.Sin(angle));
        public D Point(double angle) => Center + Relative(angle);
    }
    private static D Point(Vector2 v)
    {
        if (!v.IsFinite()) throw new ArgumentOutOfRangeException(nameof(v));
        return new(v.X, v.Y);
    }
    private static Arc Prepare(Vector2 pivot, Vector2 initialRay, double from, double to)
    {
        D center = Point(pivot), ray = Point(initialRay);
        double radius = Math.Sqrt(ray.LengthSquared), span = to - from;
        if (radius <= 0 || !double.IsFinite(from) || !double.IsFinite(to) ||
            !double.IsFinite(span) || span < 0 || span > Mathf.Pi)
            throw new ArgumentOutOfRangeException(nameof(from));
        double lo = Math.Atan2(ray.Y, ray.X) + Math.IEEERemainder(from, Math.Tau);
        return new(center, radius, lo, lo + span);
    }
    private static double Radius(double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
    private static bool Lift(Arc arc, double angle, out double lifted)
    {
        double mid = arc.Lo + (arc.Hi - arc.Lo) * .5;
        lifted = angle + Math.Tau * Math.Round((mid - angle) / Math.Tau);
        // This tolerance can only select the real endpoint, never a point outside the visited arc.
        if (lifted < arc.Lo - 1e-12 || lifted > arc.Hi + 1e-12) return false;
        lifted = Math.Clamp(lifted, arc.Lo, arc.Hi);
        return true;
    }
    public static double SegmentDistanceSquared(Vector2 pivot, Vector2 initialRay, double from, double to,
        Vector2 a, Vector2 b) => Distance(Prepare(pivot, initialRay, from, to), Point(a), Point(b));

    private static double Distance(Arc arc, D worldA, D worldB)
    {
        D a = worldA - arc.Center, b = worldB - arc.Center, delta = b - a;
        double length = Math.Sqrt(delta.LengthSquared), best = double.PositiveInfinity;
        D e = length > 0 ? delta * (1 / length) : default;
        void Evaluate(double angle)
        {
            D v = arc.Relative(angle) - a;
            if (length > 0) v -= e * Math.Clamp(v.Dot(e), 0, length);
            best = Math.Min(best, v.LengthSquared);
        }
        void Add(double angle) { if (Lift(arc, angle, out double mapped)) Evaluate(mapped); }
        void Radial(D v)
        {
            if (v.LengthSquared == 0) return;
            double angle = Math.Atan2(v.Y, v.X); Add(angle); Add(angle + Math.PI);
        }
        void Roots(D q, double ratio)
        {
            if (ratio < -1 || ratio > 1) return;
            double phi = Math.Atan2(q.Y, q.X);
            double alpha = Math.Atan2(Math.Sqrt(Math.Max(0, (1 - ratio) * (1 + ratio))), ratio);
            Add(phi - alpha); Add(phi + alpha);
        }
        Evaluate(arc.Lo); Evaluate(arc.Hi); Radial(a); Radial(b);
        if (length > 0)
        {
            D n = new(-e.Y, e.X);
            double normal = Math.Atan2(n.Y, n.X); Add(normal); Add(normal + Math.PI);
            Roots(n, n.Dot(a) / arc.Radius);
            Roots(e, e.Dot(a) / arc.Radius);
            Roots(e, e.Dot(b) / arc.Radius);
        }
        return best;
    }

    public static bool IntersectsCircle(Vector2 pivot, Vector2 ray, double from, double to, double gearRadius,
        Vector2 center, double radius) => IntersectsCapsule(pivot, ray, from, to, gearRadius, center, center, radius);
    public static bool IntersectsCapsule(Vector2 pivot, Vector2 ray, double from, double to, double gearRadius,
        Vector2 a, Vector2 b, double radius)
    {
        double sum = Radius(gearRadius) + Radius(radius);
        return SegmentDistanceSquared(pivot, ray, from, to, a, b) <= sum * sum;
    }
    public static bool IntersectsRectangle(Vector2 pivot, Vector2 ray, double from, double to, double gearRadius,
        Transform2D transform, Vector2 size)
    {
        Arc arc = Prepare(pivot, ray, from, to);
        double radius = Radius(gearRadius);
        D origin = Point(transform.Origin), x = Point(transform.X), y = Point(transform.Y);
        if (!size.IsFinite() || size.X <= 0 || size.Y <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        double determinant = x.X * y.Y - x.Y * y.X;
        if (determinant == 0) throw new ArgumentOutOfRangeException(nameof(transform));
        D relative = arc.Point(arc.Lo) - origin;
        double localX = (relative.X * y.Y - relative.Y * y.X) / determinant;
        double localY = (x.X * relative.Y - x.Y * relative.X) / determinant;
        double hx = size.X * .5d, hy = size.Y * .5d;
        if (Math.Abs(localX) <= hx && Math.Abs(localY) <= hy) return true;
        D a = origin - x * hx - y * hy, b = origin + x * hx - y * hy;
        D c = origin + x * hx + y * hy, d = origin - x * hx + y * hy;
        double limit = radius * radius;
        return Distance(arc, a, b) <= limit || Distance(arc, b, c) <= limit ||
            Distance(arc, c, d) <= limit || Distance(arc, d, a) <= limit;
    }
    public static Rect2 Bounds(Vector2 pivot, Vector2 ray, double from, double to, double gearRadius)
    {
        Arc arc = Prepare(pivot, ray, from, to);
        double r = Radius(gearRadius), minX = double.PositiveInfinity, minY = minX;
        double maxX = double.NegativeInfinity, maxY = maxX;
        void Add(double angle)
        {
            D p = arc.Point(angle);
            minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
            maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
        }
        Add(arc.Lo); Add(arc.Hi);
        for (int i = 0; i < 4; i++) if (Lift(arc, i * Math.PI * .5, out double angle)) Add(angle);
        // Outward float rounding is a broadphase/reservation bound only; distance tests never inflate damage.
        float left = MathF.BitDecrement((float)(minX - r)), top = MathF.BitDecrement((float)(minY - r));
        float right = MathF.BitIncrement((float)(maxX + r)), bottom = MathF.BitIncrement((float)(maxY + r));
        var result = new Rect2(new(left, top), new(right - left, bottom - top));
        if (!result.Position.IsFinite() || !result.Size.IsFinite()) throw new ArgumentOutOfRangeException(nameof(pivot));
        return result;
    }
}
