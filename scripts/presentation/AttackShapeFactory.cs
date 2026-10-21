using System;
using Godot;
using Knight.Combat;

namespace Knight.Presentation;

public static class AttackShapeFactory
{
    public static AttackShapeFrame FromSweptCircle(CollisionShape2D source,
        Transform2D lockedStartWorldTransform, Vector2 lockedEndWorldPosition)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Shape is not CircleShape2D circle)
            throw new ArgumentException("A swept-circle attack requires a CircleShape2D authority.", nameof(source));

        float scaleX = lockedStartWorldTransform.X.Length();
        float scaleY = lockedStartWorldTransform.Y.Length();
        if (Mathf.Abs(scaleX - scaleY) > .0001f)
            throw new ArgumentException("A swept-circle attack requires uniform world scale.", nameof(lockedStartWorldTransform));
        float radius = circle.Radius * scaleX;
        Vector2 delta = lockedEndWorldPosition - lockedStartWorldTransform.Origin;
        float distance = delta.Length();
        if (distance <= .0001f)
            return new AttackShapeFrame(AttackShapeKind.Circle, lockedStartWorldTransform.Origin,
                Vector2.One * radius, lockedStartWorldTransform.Rotation, Array.Empty<Vector2>());

        return new AttackShapeFrame(AttackShapeKind.Capsule,
            (lockedStartWorldTransform.Origin + lockedEndWorldPosition) * .5f,
            new Vector2(radius, distance * .5f + radius),
            delta.Angle() - Mathf.Pi * .5f,
            Array.Empty<Vector2>());
    }

    public static AttackShapeFrame FromCollision(CollisionShape2D source, Transform2D worldTransform,
        AttackShapeKind requestedKind)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Shape == null) throw new ArgumentException("Attack collision must own a shape.", nameof(source));

        float scaleX = worldTransform.X.Length();
        float scaleY = worldTransform.Y.Length();
        float rotation = worldTransform.Rotation;
        Vector2 origin = worldTransform.Origin;
        return source.Shape switch
        {
            RectangleShape2D rectangle when requestedKind is AttackShapeKind.Rectangle or AttackShapeKind.GroundBand =>
                new AttackShapeFrame(requestedKind, origin,
                    new Vector2(rectangle.Size.X * scaleX, rectangle.Size.Y * scaleY) * .5f,
                    rotation, Array.Empty<Vector2>()),
            CircleShape2D circle when requestedKind == AttackShapeKind.Circle =>
                new AttackShapeFrame(requestedKind, origin,
                    new Vector2(circle.Radius * scaleX, circle.Radius * scaleY),
                    rotation, Array.Empty<Vector2>()),
            CapsuleShape2D capsule when requestedKind == AttackShapeKind.Capsule =>
                new AttackShapeFrame(requestedKind, origin,
                    new Vector2(capsule.Radius * scaleX, capsule.Height * .5f * scaleY),
                    rotation, Array.Empty<Vector2>()),
            ConvexPolygonShape2D polygon when requestedKind == AttackShapeKind.ConvexPolygon =>
                ConvexFrame(polygon, worldTransform),
            _ => throw new ArgumentException(
                $"Collision {source.Shape.GetType().Name} cannot publish {requestedKind} attack geometry.", nameof(requestedKind))
        };
    }

    private static AttackShapeFrame ConvexFrame(ConvexPolygonShape2D polygon, Transform2D transform)
    {
        Vector2[] points = new Vector2[polygon.Points.Length];
        Vector2 extent = Vector2.Zero;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = transform * polygon.Points[i];
            Vector2 offset = points[i] - transform.Origin;
            extent = new Vector2(Mathf.Max(extent.X, Mathf.Abs(offset.X)),
                Mathf.Max(extent.Y, Mathf.Abs(offset.Y)));
        }
        return new AttackShapeFrame(AttackShapeKind.ConvexPolygon, transform.Origin, extent,
            transform.Rotation, Array.AsReadOnly(points));
    }
}
