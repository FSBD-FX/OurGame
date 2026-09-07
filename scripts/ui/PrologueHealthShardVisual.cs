using Godot;

namespace Knight.Ui;

public partial class PrologueHealthShardVisual : Control
{
    public HealthShardState State { get; private set; } = HealthShardState.Filled;

    public void SetState(HealthShardState state)
    {
        State = state;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color bone = new("e8dfc6");
        Color rust = new("a94e3d");
        Color dim = new("263039");
        Vector2[] porcelain =
        [
            new(20, 2), new(34, 9), new(38, 25), new(30, 42), new(20, 48),
            new(10, 42), new(2, 25), new(6, 9)
        ];
        Color fill = State switch
        {
            HealthShardState.Filled => bone,
            HealthShardState.Broken => new Color(rust, .82f),
            _ => new Color(dim, .72f)
        };
        DrawColoredPolygon(porcelain, fill);
        DrawPolyline([.. porcelain, porcelain[0]], State == HealthShardState.Broken ? rust :
            new Color("b99a4a"), 2f, true);
        if (State == HealthShardState.Broken)
        {
            DrawPolyline([new Vector2(14, 6), new Vector2(22, 18), new Vector2(15, 28),
                new Vector2(27, 43)], new Color("1b1110"), 2f, true);
        }
        else if (State == HealthShardState.Filled)
        {
            DrawCircle(new Vector2(20, 24), 5f, new Color("9edbd2"));
        }
    }
}
