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
        theme.SetStylebox("panel", "Panel", Box(Glass, Brass, 2, 12));
        theme.SetStylebox("normal", "Button", Box(new Color(GlassRaised, 0.72f), new Color(Brass, 0.45f), 1, 7));
        theme.SetStylebox("hover", "Button", Box(GlassRaised, Brass, 2, 7));
        theme.SetStylebox("pressed", "Button", Box(new Color(0.04f, 0.18f, 0.18f, 1f), Thread, 2, 7));
        theme.SetStylebox("focus", "Button", Box(new Color(0f, 0f, 0f, 0f), Bone, 2, 7));
        theme.SetStylebox("disabled", "Button", Box(new Color(Glass, 0.45f), new Color(BoneMuted, 0.2f), 1, 7));
        theme.SetStylebox("normal", "OptionButton", Box(GlassRaised, new Color(Brass, 0.5f), 1, 6));
        return theme;
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
