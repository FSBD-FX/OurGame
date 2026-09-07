using Godot;
using Knight.Player;
using Knight.Settings;

namespace Knight.Ui;

public readonly record struct PrologueActionModeFrame(
    PlayerActionMode Mode,
    PlayerActionRejectionReason Rejection)
{
    public static readonly PrologueActionModeFrame Default =
        new(PlayerActionMode.Attack, PlayerActionRejectionReason.None);
}

public partial class PrologueActionModeVisual : Control
{
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    public PrologueActionModeFrame CurrentFrame { get; private set; } = PrologueActionModeFrame.Default;

    public void Present(PrologueActionModeFrame frame)
    {
        if (frame == CurrentFrame) return;
        CurrentFrame = frame;
        QueueRedraw();
    }

    public void SetLanguage(GameLanguage language)
    {
        _language = language;
        if (GetNodeOrNull<Label>("Attack/Label") is { } attack)
            attack.Text = language == GameLanguage.English ? "Q ATTACK" : "Q 攻击";
        if (GetNodeOrNull<Label>("Grapple/Label") is { } grapple)
            grapple.Text = language == GameLanguage.English ? "E GRAPPLE" : "E 机械爪";
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0f,
            new Vector2(Size.X / 220f, Size.Y / 36f));
        DrawMode(new Rect2(0, 0, 94, 34), CurrentFrame.Mode == PlayerActionMode.Attack,
            new Color("d7b85b"), "Q", _language == GameLanguage.English ? "ATTACK" : "攻击");
        DrawMode(new Rect2(102, 0, 94, 34), CurrentFrame.Mode == PlayerActionMode.Grapple,
            new Color("72d9d1"), "E", _language == GameLanguage.English ? "GRAPPLE" : "机械爪");
        if (CurrentFrame.Rejection != PlayerActionRejectionReason.None)
            DrawString(ThemeDB.FallbackFont, new Vector2(202, 23), "!", HorizontalAlignment.Left,
                18, 18, new Color("ef7b68"));
    }

    private void DrawMode(Rect2 rect, bool selected, Color accent, string glyph, string label)
    {
        DrawRect(rect, new Color("101a22"), true);
        DrawRect(rect, selected ? accent : new Color("4b5c62"), false, selected ? 3f : 1f);
        if (selected) DrawCircle(new Vector2(rect.Position.X + 11, rect.GetCenter().Y), 4f, accent);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(20, 15), glyph,
            HorizontalAlignment.Left, 22, 12, accent);
        DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(20, 29), label,
            HorizontalAlignment.Left, 70, 10, new Color("f2e6c9"));
    }
}
