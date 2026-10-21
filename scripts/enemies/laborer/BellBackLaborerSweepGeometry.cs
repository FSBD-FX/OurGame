using Godot;

namespace Knight.Enemies.Laborer;

public static class BellBackLaborerSweepGeometry
{
    public const float Radius = 80f;
    public const float HalfAngleRadians = Mathf.Pi / 3f;
    public const int ArcSegments = 4;

    public static ConvexPolygonShape2D CreateShape() => new() { Points = CreatePoints() };

    public static Vector2[] CreatePoints()
    {
        var points = new Vector2[ArcSegments + 2];
        points[0] = Vector2.Zero;
        for (int i = 0; i <= ArcSegments; i++)
        {
            float progress = i / (float)ArcSegments;
            float angle = Mathf.Lerp(-HalfAngleRadians, HalfAngleRadians, progress);
            points[i + 1] = Vector2.FromAngle(angle) * Radius;
        }
        return points;
    }
}
