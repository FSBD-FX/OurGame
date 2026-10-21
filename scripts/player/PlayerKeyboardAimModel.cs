using Godot;

namespace Knight.Player;

public static class PlayerKeyboardAimModel
{
    public static Vector2 ResolveDirection(Vector2 rawAim, int lastFacing)
    {
        if (!float.IsFinite(rawAim.X) || !float.IsFinite(rawAim.Y) || rawAim.IsZeroApprox())
            return lastFacing < 0 ? Vector2.Left : Vector2.Right;
        float angle = Mathf.Round(rawAim.Angle() / (Mathf.Pi / 4f)) * (Mathf.Pi / 4f);
        return Vector2.FromAngle(angle).Snapped(new Vector2(.000001f, .000001f)).Normalized();
    }
}
