using Godot;

namespace Knight.Player;

public partial class PlayerDebugVisual : Node2D
{
    private static readonly Color BoneWhite = new("e8dfc6");
    private static readonly Color Copper = new("b87333");

    public override void _Draw()
    {
        const float radius = 20f;
        DrawRect(new Rect2(-radius, -28f, radius * 2f, 56f), BoneWhite);
        DrawCircle(new Vector2(0f, -28f), radius, BoneWhite);
        DrawCircle(new Vector2(0f, 28f), radius, BoneWhite);
        DrawCircle(new Vector2(8f, 8f), 6f, Copper);
        DrawRect(new Rect2(18f, 2f, 30f, 12f), Copper);
    }
}
