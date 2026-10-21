using Godot;
using Knight.Presentation;

namespace Knight.Enemies.Laborer;

public partial class BellBackLaborerPlaceholderVisual : Node2D, IEntityVisual2D
{
    private Polygon2D _body;
    private Polygon2D _bell;

    public override void _Ready()
    {
        _body = new Polygon2D
        {
            Name = "Body", Color = new Color("9b7654"),
            Polygon = [new Vector2(-24,-38), new Vector2(24,-38), new Vector2(30,34), new Vector2(-30,34)]
        };
        _bell = new Polygon2D
        {
            Name = "BellArmor", Position = new Vector2(0,-16), Color = new Color("d0a94e"),
            Polygon = [new Vector2(-35,-30), new Vector2(35,-30), new Vector2(44,20), new Vector2(-44,20)]
        };
        AddChild(_body); AddChild(_bell);
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        Scale = new Vector2(frame.Facing < 0 ? -1f : 1f, 1f);
        Rotation = 0f;
    }

    public void PlayCue(PresentationCue cue) { }

    public void ApplyLaborerFrame(BellBackLaborerFrame frame, int facing)
    {
        ApplyFrame(new EntityPresentationFrame(EntityVisualState.Idle, facing, true));
        if (_bell == null) return;
        _bell.Visible = frame.State != BellBackLaborerState.Exposed && frame.State != BellBackLaborerState.Dead;
        _body.Scale = frame.State switch
        {
            BellBackLaborerState.Telegraph => new Vector2(1.18f, .82f),
            BellBackLaborerState.Attack => new Vector2(1.35f, .72f),
            BellBackLaborerState.Exposed => new Vector2(.72f, 1.22f),
            BellBackLaborerState.Dead => new Vector2(1.25f, .35f),
            _ => Vector2.One
        };
    }
}
