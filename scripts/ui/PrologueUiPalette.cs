using Godot;

namespace Knight.Ui;

public static class PrologueUiPalette
{
    public static readonly Color Ink = new("0b1118");
    public static readonly Color Glass = new(0.055f, 0.086f, 0.11f, 0.96f);
    public static readonly Color GlassRaised = new(0.085f, 0.13f, 0.16f, 0.98f);
    public static readonly Color Bone = new("e8dfc6");
    public static readonly Color BoneMuted = new("aaa58f");
    public static readonly Color Brass = new("d6a948");
    public static readonly Color Thread = new("72d9d1");
    public static readonly Color Rust = new("a54d37");

    public static Theme CreateTheme()
    {
        var theme = new Theme();
        theme.SetColor("font_color", "Label", Bone);
        theme.SetColor("font_color", "Button", Bone);
        theme.SetColor("font_hover_color", "Button", Brass);
        theme.SetColor("font_pressed_color", "Button", Thread);
        theme.SetColor("font_focus_color", "Button", Bone);
        theme.SetColor("font_disabled_color", "Button", new Color(BoneMuted, 0.45f));
        theme.SetColor("font_color", "CheckButton", Bone);
        theme.SetColor("font_color", "OptionButton", Bone);
        theme.SetFontSize("font_size", "Label", 20);
        theme.SetFontSize("font_size", "Button", 20);
        theme.SetFontSize("font_size", "CheckButton", 18);
        theme.SetFontSize("font_size", "OptionButton", 18);
        theme.SetStylebox("panel", "Panel", Box(Glass, Brass, 0, 12));
        theme.SetStylebox("normal", "Button", Box(new Color(GlassRaised, 0.72f), new Color(Brass, 0.45f), 0, 7));
        theme.SetStylebox("hover", "Button", Box(GlassRaised, Brass, 0, 7));
        theme.SetStylebox("pressed", "Button", Box(new Color(0.04f, 0.18f, 0.18f, 1f), Thread, 0, 7));
        StyleBoxFlat focus = Box(new Color(0f, 0f, 0f, 0f), Bone, 0, 7);
        focus.BorderWidthBottom = 2;
        theme.SetStylebox("focus", "Button", focus);
        theme.SetStylebox("disabled", "Button", Box(new Color(Glass, 0.45f), new Color(BoneMuted, 0.2f), 0, 7));
        theme.SetStylebox("normal", "OptionButton", Box(GlassRaised, new Color(Brass, 0.5f), 0, 6));
        return theme;
    }

    // Existing scenes serialize their theme. Copy only the presentation style
    // resources instead of requiring a scene rebuild or mutating shared themes.
    public static void ApplyBorderlessPanels(Node root)
    {
        if (root is Control control)
        {
            if (control.Name == "TopRule") control.Visible = false;
            if (control is Panel or PanelContainer)
                ApplyBorderlessStyle(control, "panel");
            if (control is Button)
            {
                foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
                    ApplyBorderlessStyle(control, state);
                ApplyBorderlessStyle(control, "focus", focus: true);
            }
        }
        foreach (Node child in root.GetChildren()) ApplyBorderlessPanels(child);
    }

    private static void ApplyBorderlessStyle(Control control, string name, bool focus = false)
    {
        if (control.GetThemeStylebox(name) is not StyleBoxFlat authored) return;
        var style = (StyleBoxFlat)authored.Duplicate();
        style.BorderWidthLeft = style.BorderWidthTop = style.BorderWidthRight = 0;
        style.BorderWidthBottom = focus ? 2 : 0;
        if (focus)
        {
            style.BgColor = new Color(0f, 0f, 0f, 0f);
            style.BorderColor = Bone;
        }
        control.AddThemeStyleboxOverride(name, style);
    }

    private static StyleBoxFlat Box(Color fill, Color border, int width, int radius) => new()
    {
        BgColor = fill,
        BorderColor = border,
        BorderWidthLeft = width,
        BorderWidthTop = width,
        BorderWidthRight = width,
        BorderWidthBottom = width,
        CornerRadiusTopLeft = radius,
        CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius,
        CornerRadiusBottomRight = radius,
        ContentMarginLeft = 18f,
        ContentMarginTop = 10f,
        ContentMarginRight = 18f,
        ContentMarginBottom = 10f
    };
}
