using Godot;

namespace Knight.Abilities;

public static class MechanicalClawArrivalGeometry
{
    public static Vector2 DockPoint(Vector2 anchor, Vector2 outwardNormal,
        CapsuleShape2D capsule, float minimumRopeLength, float margin)
    {
        Vector2 normal = outwardNormal.Normalized();
        float segmentHalf = Mathf.Max(0f, (capsule.Height - capsule.Radius * 2f) * .5f);
        float support = capsule.Radius + segmentHalf * Mathf.Abs(normal.Y) + margin;
        return anchor + normal * Mathf.Max(minimumRopeLength, support);
    }

    public static bool IsArrived(Vector2 caster, Vector2 dockPoint, float tolerance) =>
        caster.DistanceTo(dockPoint) <= tolerance;

    public static bool NormalMatches(Vector2 expected, Vector2 actual, float minimumDot) =>
        !expected.IsZeroApprox() && !actual.IsZeroApprox() &&
        expected.Normalized().Dot(actual.Normalized()) >= minimumDot;
}
