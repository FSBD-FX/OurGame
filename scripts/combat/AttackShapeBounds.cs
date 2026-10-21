using System;
using Godot;

namespace Knight.Combat;

public static class AttackShapeBounds
{
    public static Rect2 WorldBounds(AttackShapeFrame shape)
    {
        if (!Enum.IsDefined(shape.Kind) || !Finite(shape.Origin) || !Finite(shape.Extent) ||
            !float.IsFinite(shape.RotationRadians) || shape.Extent.X < 0f || shape.Extent.Y < 0f)
            throw new ArgumentOutOfRangeException(nameof(shape), "World geometry must be finite and non-negative.");
        if (shape.Kind == AttackShapeKind.None) return new Rect2();
        if (shape.Kind == AttackShapeKind.ConvexPolygon)
        {
            if (shape.Points == null || shape.Points.Count < 3)
                throw new ArgumentOutOfRangeException(nameof(shape), "A polygon requires finite world vertices.");
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
            foreach (Vector2 point in shape.Points)
            {
                if (!Finite(point)) throw new ArgumentOutOfRangeException(nameof(shape));
                minX = Math.Min(minX, point.X); minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X); maxY = Math.Max(maxY, point.Y);
            }
            // Polygon points are already world-space, just like the telegraph.
            return Bounds(minX, minY, maxX, maxY);
        }

        double cosine = Math.Cos(shape.RotationRadians), sine = Math.Sin(shape.RotationRadians);
        double x = shape.Extent.X, y = shape.Extent.Y;
        double halfX, halfY;
        switch (shape.Kind)
        {
            case AttackShapeKind.Rectangle:
            case AttackShapeKind.GroundBand:
                halfX = Math.Abs(cosine) * x + Math.Abs(sine) * y;
                halfY = Math.Abs(sine) * x + Math.Abs(cosine) * y;
                break;
            case AttackShapeKind.Circle:
                // Circle frames can also declare an ellipse. Its support is
                // not the support of the enclosing, rotated rectangle.
                halfX = Math.Sqrt(x * x * cosine * cosine + y * y * sine * sine);
                halfY = Math.Sqrt(x * x * sine * sine + y * y * cosine * cosine);
                break;
            case AttackShapeKind.Capsule:
                double straight = Math.Max(0d, y - x);
                halfX = x + Math.Abs(sine) * straight;
                halfY = x + Math.Abs(cosine) * straight;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
        return Bounds(shape.Origin.X - halfX, shape.Origin.Y - halfY,
            shape.Origin.X + halfX, shape.Origin.Y + halfY);
    }

    private static Rect2 Bounds(double minX, double minY, double maxX, double maxY)
    {
        double width = maxX - minX, height = maxY - minY;
        if (!Representable(minX) || !Representable(minY) || !Representable(maxX) || !Representable(maxY) ||
            !Representable(width) || !Representable(height) || width < 0d || height < 0d)
            throw new ArgumentOutOfRangeException("shape", "World bounds overflow finite geometry.");
        return new Rect2((float)minX, (float)minY, (float)width, (float)height);
    }

    private static bool Representable(double value) =>
        double.IsFinite(value) && value >= -float.MaxValue && value <= float.MaxValue;

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
