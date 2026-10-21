using Godot;

namespace Knight.Presentation;

public partial class RustShellPlaceholderVisual : Node2D, IEntityVisual2D
{
    private EntityPresentationFrame _frame = new(EntityVisualState.Idle, 1, true);
    private float _flashSeconds;

    public override void _Process(double delta)
    {
        _flashSeconds = Mathf.Max(0f, _flashSeconds - (float)delta);
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        _frame = frame;
        QueueRedraw();
    }

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _flashSeconds = 0.10f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        int facing = _frame.Facing < 0 ? -1 : 1;
        Color shell = _flashSeconds > 0f ? new Color(1f, 0.93f, 0.72f) : new Color(0.43f, 0.18f, 0.10f);
        Color bell = _frame.State == EntityVisualState.Attack ? new Color(1f, 0.62f, 0.08f) : new Color(0.22f, 0.22f, 0.25f);
        Color blade = _frame.State == EntityVisualState.Attack ? new Color(1f, 0.16f, 0.10f) : new Color(0.55f, 0.57f, 0.58f);
        if (_frame.State == EntityVisualState.Dead)
        {
            shell = new Color(0.18f, 0.12f, 0.10f);
            bell = new Color(0.10f, 0.10f, 0.11f);
        }

        Vector2 scale = _frame.State switch
        {
            EntityVisualState.Move => new Vector2(1.12f, 0.94f),
            EntityVisualState.Attack => new Vector2(1.18f, 0.88f),
            EntityVisualState.Hurt => new Vector2(0.90f, 1.10f),
            EntityVisualState.Dead => new Vector2(1.25f, 0.45f),
            _ => Vector2.One
        };
        DrawSetTransform(Vector2.Zero, 0f, scale);
        DrawCircle(new Vector2(-facing * 10f, -25f), 14f, bell);
        DrawRect(new Rect2(-18f, -20f, 36f, 42f), shell);
        float bladeLength = _frame.State == EntityVisualState.Attack ? 38f : 22f;
        float bladeX = facing > 0 ? 18f : -18f - bladeLength;
        DrawRect(new Rect2(bladeX, -8f, bladeLength, 7f), blade);
        DrawLine(new Vector2(-10f, 22f), new Vector2(-10f, 33f), new Color(0.15f, 0.13f, 0.12f), 5f);
        DrawLine(new Vector2(10f, 22f), new Vector2(10f, 33f), new Color(0.15f, 0.13f, 0.12f), 5f);
    }
}
