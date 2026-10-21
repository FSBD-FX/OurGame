using Godot;

namespace Knight.Presentation;

// Pure presentation: encounter timing and birth never depend on this node surviving.
public partial class CanyonSpawnWarningVisual : Node2D
{
    public override void _Ready() => ZIndex = 8;

    public override void _Draw()
    {
        Color amber = new("e7b875");
        DrawArc(new Vector2(0, -3), 19f, 0f, Mathf.Tau, 24, amber, 2f, true);
        DrawLine(new Vector2(-13, -1), new Vector2(13, -1), amber, 2f, true);
        DrawCircle(new Vector2(0, -3), 2.5f, new Color("fff2c9"));
    }
}
