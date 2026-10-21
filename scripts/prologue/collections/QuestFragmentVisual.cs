using Godot;

namespace Knight.Prologue.Collections;

// Replaceable presentation only. The Area/ID/save transaction owns collection.
public partial class QuestFragmentVisual : Node2D
{
    [Export] public Color ShellColor { get; set; } = new("c9ab68");
    [Export] public Color CoreColor { get; set; } = new("84ebd1");
    private float _phase;
    public override void _Process(double delta)
    {
        _phase = (_phase + (float)delta) % Mathf.Tau;
        QueueRedraw();
    }
    public override void _Draw()
    {
        Vector2 bob = new(0, -Mathf.Sin(_phase * 2f) * 2f);
        Vector2[] corners = [bob + new Vector2(0,-13), bob + new Vector2(9,-2),
            bob + new Vector2(3,12), bob + new Vector2(-8,5)];
        DrawColoredPolygon(corners, new Color("263c3b"));
        DrawPolyline([corners[0], corners[1], corners[2], corners[3], corners[0]], ShellColor, 2f, true);
        DrawLine(bob + new Vector2(-2,-6), bob + new Vector2(2,6), CoreColor, 3f, true);
        DrawCircle(bob, 3f, CoreColor);
    }
}
