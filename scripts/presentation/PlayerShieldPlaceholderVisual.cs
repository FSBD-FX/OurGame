using Godot;

namespace Knight.Presentation;

public partial class PlayerShieldPlaceholderVisual : Node2D
{
    private static readonly Color FanFill = new(0.30f, 0.78f, 0.92f, .20f);
    private static readonly Color FanEdge = new(0.55f, 0.92f, 1f, .78f);
    private Vector2 _direction = Vector2.Right;

    public bool IsPresented { get; private set; }
    public Vector2 Direction => _direction;

    public void Present(bool active, Vector2 direction)
    {
        IsPresented = active;
        if (direction.LengthSquared() > float.Epsilon && float.IsFinite(direction.X) && float.IsFinite(direction.Y))
            _direction = direction.Normalized();
        Visible = active;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!IsPresented) return;
        const float radius = 58f;
        const float halfArc = Mathf.Pi * 5f / 12f;
        const int segments = 12;
        float center = _direction.Angle();
        var points = new Vector2[segments + 2];
        points[0] = Vector2.Zero;
        for (int index = 0; index <= segments; index++)
        {
            float angle = center - halfArc + halfArc * 2f * index / segments;
            points[index + 1] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        DrawPolygon(points, [FanFill]);
        DrawArc(Vector2.Zero, radius, center - halfArc, center + halfArc, segments, FanEdge, 2f, true);
        DrawLine(Vector2.Zero, points[1], FanEdge, 2f, true);
        DrawLine(Vector2.Zero, points[^1], FanEdge, 2f, true);
    }
}
