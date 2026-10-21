using Godot;
using Knight.Presentation;

namespace Knight.Enemies.Moth;

public partial class GraveLampMothPlaceholderVisual : Node2D, IEntityVisual2D
{
    private GraveLampMothState _state = GraveLampMothState.Hover;
    private int _facing = 1;
    private float _flash;

    public override void _Process(double delta)
    {
        _flash = Mathf.Max(0f, _flash - (float)delta);
        QueueRedraw();
    }

    public void ApplyMothFrame(GraveLampMothFrame frame, int facing, bool grounded)
    {
        _state = frame.State;
        _facing = facing < 0 ? -1 : 1;
        Scale = frame.State switch
        {
            GraveLampMothState.Telegraph => new Vector2(1.25f, 0.65f),
            GraveLampMothState.Dive => new Vector2(0.65f, 1.45f),
            GraveLampMothState.ThreadStunned => new Vector2(1.35f, 0.40f),
            GraveLampMothState.Dead => new Vector2(1.20f, 0.30f),
            _ => Vector2.One
        };
        Rotation = frame.State == GraveLampMothState.ThreadStunned ? 0.45f * _facing : 0f;
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        _facing = frame.Facing < 0 ? -1 : 1;
        QueueRedraw();
    }

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _flash = 0.10f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color body = _flash > 0f ? new Color("fff0b0") : new Color("d7c36f");
        if (_state == GraveLampMothState.Dead) body = new Color("554b38");
        Color wing = _state == GraveLampMothState.Telegraph ? new Color("e58f38") : new Color("6ab7bb");
        Vector2[] diamond = [new(0, -18), new(13, 0), new(0, 18), new(-13, 0)];
        DrawColoredPolygon(diamond, body);
        DrawColoredPolygon([new(-4, -3), new(-30, -15), new(-22, 8)], wing);
        DrawColoredPolygon([new(4, -3), new(30, -15), new(22, 8)], wing);
        DrawCircle(new Vector2(_facing * 5f, -4f), 3f, new Color("f4e38b"));
    }
}
