using Godot;

namespace Knight.Enemies;

/// <summary>Presentation-only gray-box silhouette and combat telegraphs for the squire.</summary>
public partial class RustShellDebugVisual : Node2D
{
    private RustShellState _state = RustShellState.Idle;
    private int _facing = 1;
    private float _flashSeconds;

    public override void _Process(double delta)
    {
        _flashSeconds = Mathf.Max(0f, _flashSeconds - (float)delta);
        QueueRedraw();
    }

    public void SetFrame(RustShellFrame frame, int facing)
    {
        _state = frame.State;
        if (facing != 0) _facing = facing;
        Position = frame.State == RustShellState.Telegraph
            ? new Vector2(-_facing * 5f, 2f)
            : Vector2.Zero;
        Rotation = frame.State == RustShellState.Telegraph ? -_facing * 0.10f : 0f;
        QueueRedraw();
    }

    public void FlashDamage()
    {
        _flashSeconds = 0.10f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color shell = _flashSeconds > 0f ? new Color(1f, 0.93f, 0.72f) : new Color(0.43f, 0.18f, 0.10f);
        Color bell = _state == RustShellState.Telegraph ? new Color(1f, 0.62f, 0.08f) : new Color(0.22f, 0.22f, 0.25f);
        Color blade = _state == RustShellState.Attack ? new Color(1f, 0.16f, 0.10f) : new Color(0.55f, 0.57f, 0.58f);
        if (_state == RustShellState.Dead)
        {
            shell = new Color(0.18f, 0.12f, 0.10f);
            bell = new Color(0.10f, 0.10f, 0.11f);
        }

        DrawCircle(new Vector2(-_facing * 10f, -25f), 14f, bell);
        DrawRect(new Rect2(-18f, -20f, 36f, 42f), shell);
        float bladeX = _facing > 0 ? 18f : -40f;
        DrawRect(new Rect2(bladeX, -8f, 22f, 7f), blade);
        DrawLine(new Vector2(-10f, 22f), new Vector2(-10f, 33f), new Color(0.15f, 0.13f, 0.12f), 5f);
        DrawLine(new Vector2(10f, 22f), new Vector2(10f, 33f), new Color(0.15f, 0.13f, 0.12f), 5f);
    }
}
