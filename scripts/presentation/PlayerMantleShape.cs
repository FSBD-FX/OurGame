using Godot;

namespace Knight.Presentation;

/// <summary>Stateless, facing-local cloth mapping driven by the existing damped channel.</summary>
public static class PlayerMantleShape
{
    public static Vector2 Map(Vector2 restPoint, PoseTransform2D motion)
    {
        if (!float.IsFinite(restPoint.X) || !float.IsFinite(restPoint.Y)) return Vector2.Zero;
        if (restPoint.Y <= -3f) return restPoint;

        float x = FiniteClamp(motion.Position.X, 12f);
        float y = FiniteClamp(motion.Position.Y, 7f);
        float angle = FiniteClamp(motion.RotationRadians, .42f);
        if (x == 0f && y == 0f && angle == 0f) return restPoint;

        // The spring's rightward motion has negative X and angle. Use the same trailing sign;
        // rigidly rotating positive-Y hem points would oppose X translation and cancel lag.
        float horizontal = Mathf.Clamp(x + angle * 16f, -12f, 12f);
        float depth = (float)System.Math.Clamp(((double)restPoint.Y + 3d) / 34d, 0d, 1d);
        float weight = depth * depth * (3f - 2f * depth);
        return restPoint + new Vector2(horizontal * weight, y * weight);
    }

    private static float FiniteClamp(float value, float limit) =>
        float.IsFinite(value) ? Mathf.Clamp(value, -limit, limit) : 0f;
}
