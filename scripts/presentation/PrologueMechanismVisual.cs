using Godot;

namespace Knight.Presentation;

public enum MechanismVisualKind
{
    GraveLamp,
    ThreadAnchor,
    BellChain,
    ShortcutDoor
}

public enum MechanismVisualState
{
    Idle,
    Highlighted,
    Activating,
    Active,
    Disabled,
    Restored
}

public partial class PrologueMechanismVisual : Node2D
{
    [Export] public MechanismVisualKind Kind { get; set; }
    [Export] public MechanismVisualState InitialState { get; set; } = MechanismVisualState.Idle;

    public MechanismVisualState CurrentState { get; private set; } = MechanismVisualState.Idle;
    public float AnimationPhase { get; private set; }

    private static readonly Color Ink = new("11171a");
    private static readonly Color Gold = new("d4aa58");
    private static readonly Color GoldLight = new("f0d481");
    private static readonly Color Cyan = new("69d7cc");
    private static readonly Color Disabled = new("566166");

    public override void _Ready() => SetState(InitialState);

    public override void _Process(double delta)
    {
        AnimationPhase += (float)delta * (CurrentState == MechanismVisualState.Activating ? 5f : 1.4f);
        QueueRedraw();
    }

    public void SetState(MechanismVisualState state)
    {
        CurrentState = state;
        QueueRedraw();
    }

    public Color ResolveAccent() => CurrentState switch
    {
        MechanismVisualState.Active or MechanismVisualState.Restored => Cyan,
        MechanismVisualState.Disabled => Disabled,
        MechanismVisualState.Highlighted or MechanismVisualState.Activating => GoldLight,
        _ => Gold
    };

    public override void _Draw()
    {
        Color accent = ResolveAccent();
        float pulse = CurrentState is MechanismVisualState.Highlighted or MechanismVisualState.Activating
            ? 1f + Mathf.Sin(AnimationPhase * 4f) * 0.08f : 1f;
        switch (Kind)
        {
            case MechanismVisualKind.GraveLamp:
                DrawLine(new Vector2(0, 12), new Vector2(0, 46), Ink, 11f);
                DrawLine(new Vector2(0, 12), new Vector2(0, 44), accent, 4f);
                DrawCircle(new Vector2(0, -5), 20f * pulse, Ink);
                DrawCircle(new Vector2(0, -5), 14f * pulse, accent);
                DrawCircle(new Vector2(0, -5), 6f, Cyan);
                break;
            case MechanismVisualKind.ThreadAnchor:
                DrawLine(new Vector2(-28, 0), new Vector2(0, -31), Ink, 12f);
                DrawLine(new Vector2(0, -31), new Vector2(28, 0), Ink, 12f);
                DrawLine(new Vector2(28, 0), new Vector2(0, 31), Ink, 12f);
                DrawLine(new Vector2(0, 31), new Vector2(-28, 0), Ink, 12f);
                DrawLine(new Vector2(-25, 0), new Vector2(0, -27), accent, 5f);
                DrawLine(new Vector2(0, -27), new Vector2(25, 0), accent, 5f);
                DrawLine(new Vector2(25, 0), new Vector2(0, 27), accent, 5f);
                DrawLine(new Vector2(0, 27), new Vector2(-25, 0), accent, 5f);
                DrawCircle(Vector2.Zero, 6f * pulse, Cyan);
                break;
            case MechanismVisualKind.BellChain:
                DrawLine(new Vector2(0, -178), new Vector2(0, -80), Ink, 11f);
                DrawLine(new Vector2(0, -178), new Vector2(0, -80), accent, 5f);
                for (int index = 0; index < 6; index++)
                    DrawCircle(new Vector2(0, -168 + index * 17), 4.5f, index % 2 == 0 ? GoldLight : accent);
                DrawCircle(new Vector2(0, -80), 22f * pulse, Ink);
                DrawCircle(new Vector2(0, -80), 15f * pulse, accent);
                DrawLine(new Vector2(-18, -73), new Vector2(18, -73), Ink, 5f);
                break;
            case MechanismVisualKind.ShortcutDoor:
                DrawRect(new Rect2(-28, -92, 56, 184), Ink);
                DrawRect(new Rect2(-21, -85, 42, 170), accent);
                for (int index = -2; index <= 2; index++)
                    DrawLine(new Vector2(index * 8, -78), new Vector2(index * 8, 78), Ink, 3f);
                if (CurrentState is MechanismVisualState.Active or MechanismVisualState.Restored)
                    DrawLine(new Vector2(-34, 84), new Vector2(34, 84), Cyan, 8f);
                break;
        }
    }
}
