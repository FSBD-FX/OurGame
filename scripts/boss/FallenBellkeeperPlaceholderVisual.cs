using Godot;
using Knight.Presentation;

namespace Knight.Boss;

public partial class FallenBellkeeperPlaceholderVisual : Node2D, IEntityVisual2D
{
    private CanvasItem _closed;
    private CanvasItem _open;
    private CanvasItem _hammer;
    private Node2D _body;

    public override void _Ready()
    {
        _closed = GetNodeOrNull<CanvasItem>("ClosedArmor");
        _open = GetNodeOrNull<CanvasItem>("OpenArmor");
        _hammer = GetNodeOrNull<CanvasItem>("HammerArc");
        _body = GetNodeOrNull<Node2D>("Body");
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        Scale = new Vector2(frame.Facing < 0 ? -1f : 1f, 1f);
    }

    public void PlayCue(PresentationCue cue) { }

    public void ApplyBossFrame(FallenBellkeeperFrame frame, int facing)
    {
        ApplyFrame(new EntityPresentationFrame(EntityVisualState.Idle, facing, true));
        if (_closed != null) _closed.Visible = frame.ArmorClosed;
        if (_open != null) _open.Visible = frame.State == FallenBellkeeperState.Exposed;
        if (_hammer != null) _hammer.Visible = frame.State is FallenBellkeeperState.HammerTelegraph or
            FallenBellkeeperState.HammerThrown;
        if (_body != null) _body.Scale = frame.State switch
        {
            FallenBellkeeperState.Transition => new Vector2(1.35f, .72f),
            FallenBellkeeperState.Exposed => new Vector2(.82f, 1.18f),
            FallenBellkeeperState.Defeated => new Vector2(1.3f, .30f),
            _ => Vector2.One
        };
    }
}
