using Godot;
using Knight.Tools;
using Knight.Ui;

namespace Knight.Build.Ui;

public partial class BuildPrologueUi : SceneTree
{
    private static readonly Vector2 MainPanelSize = new(540, 610);
    private static readonly Vector2 WidePanelSize = new(820, 624);

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://scenes/ui"));
        Theme theme = PrologueUiPalette.CreateTheme();
        var root = new PrologueUiRoot { Name = "PrologueUi", Layer = 50 };
        root.AddChild(FullRect(new ColorRect
        {
            Name = "Backdrop",
            Color = new Color(PrologueUiPalette.Ink, 0.97f),
            MouseFilter = Control.MouseFilterEnum.Stop
        }));
        root.AddChild(BuildMainMenu(theme));
        root.AddChild(BuildSettings(theme));
        root.AddChild(BuildControls(theme));

        Error error = SceneBuildUtil.PackAndSave(root, "res://scenes/ui/PrologueUi.tscn");
        root.Free();
        if (error != Error.Ok)
        {
            GD.PushError($"BuildPrologueUi failed: {error}");
            Quit(1);
            return;
        }

        PrologueUiRoot copy = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn")
            .Instantiate<PrologueUiRoot>();
        bool valid = copy.GetNodeOrNull<Button>("MainMenu/Panel/Buttons/Start") != null &&
            copy.GetNodeOrNull<Button>("MainMenu/Panel/Buttons/Continue") != null &&
            copy.GetNodeOrNull<PrologueSettingsMenu>("SettingsMenu") != null &&
            copy.GetNodeOrNull<PrologueControlsView>("ControlsView") != null;
        copy.Free();
        if (!valid)
        {
            GD.PushError("BuildPrologueUi contract validation failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildPrologueUi: formal menu shell packed and validated.");
        Quit(0);
    }

    private static PrologueMainMenu BuildMainMenu(Theme theme)
    {
        var main = FullRect(new PrologueMainMenu { Name = "MainMenu" });
        Panel panel = CenterPanel("Panel", MainPanelSize, theme);
        panel.AddChild(Label("Title", new Vector2(50, 38), new Vector2(440, 54), "THE NAMELESS BELFRY", 31,
            PrologueUiPalette.Brass, HorizontalAlignment.Center));
        panel.AddChild(Label("Subtitle", new Vector2(50, 92), new Vector2(440, 34), string.Empty, 17,
            PrologueUiPalette.BoneMuted, HorizontalAlignment.Center));
        panel.AddChild(Rule("TopRule", new Vector2(70, 142), new Vector2(400, 2)));
        var buttons = new VBoxContainer
        {
            Name = "Buttons",
            Position = new Vector2(60, 174),
            Size = new Vector2(420, 330),
            Theme = theme,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttons.AddThemeConstantOverride("separation", 12);
        buttons.AddChild(MenuButton("Start"));
        buttons.AddChild(MenuButton("Continue"));
        buttons.AddChild(MenuButton("Settings"));
        buttons.AddChild(MenuButton("Controls"));
        buttons.AddChild(MenuButton("Quit"));
        panel.AddChild(buttons);
        panel.AddChild(Label("Footer", new Vector2(50, 552), new Vector2(440, 28), string.Empty, 14,
            PrologueUiPalette.BoneMuted, HorizontalAlignment.Center));
        main.AddChild(panel);
        return main;
    }

    private static PrologueSettingsMenu BuildSettings(Theme theme)
    {
        var settings = FullRect(new PrologueSettingsMenu { Name = "SettingsMenu", Visible = false });
        Panel panel = CenterPanel("Panel", WidePanelSize, theme);
        panel.AddChild(Label("Title", new Vector2(50, 30), new Vector2(720, 44), "SETTINGS", 28,
            PrologueUiPalette.Brass, HorizontalAlignment.Center));
        panel.AddChild(Rule("TopRule", new Vector2(70, 86), new Vector2(680, 2)));
        var rows = new VBoxContainer
        {
            Name = "Rows", Position = new Vector2(70, 110), Size = new Vector2(680, 450), Theme = theme
        };
        rows.AddThemeConstantOverride("separation", 4);
        rows.AddChild(SliderRow("Master", 0, 1, .01));
        rows.AddChild(SliderRow("Music", 0, 1, .01));
        rows.AddChild(SliderRow("Effects", 0, 1, .01));
        rows.AddChild(SliderRow("Ambience", 0, 1, .01));
        rows.AddChild(SliderRow("UiVolume", 0, 1, .01));
        rows.AddChild(OptionRow("WindowMode", ["Windowed", "Borderless", "Fullscreen"]));
        rows.AddChild(OptionRow("Resolution", ["1280×720", "1920×1080", "2560×1440", "3840×2160"]));
        rows.AddChild(CheckRow("VSync"));
        rows.AddChild(SliderRow("UiScale", .8, 1.5, .05));
        rows.AddChild(SliderRow("ScreenShake", 0, 1, .05));
        rows.AddChild(CheckRow("ReduceFlashes"));
        rows.AddChild(OptionRow("Language", ["简体中文", "English"]));
        panel.AddChild(rows);
        panel.AddChild(new Button
        {
            Name = "Back", Position = new Vector2(50, 30), Size = new Vector2(150, 44), Theme = theme
        });
        settings.AddChild(panel);
        return settings;
    }

    private static PrologueControlsView BuildControls(Theme theme)
    {
        var controls = FullRect(new PrologueControlsView { Name = "ControlsView", Visible = false });
        Panel panel = CenterPanel("Panel", new Vector2(700, 570), theme);
        panel.AddChild(Label("Title", new Vector2(50, 34), new Vector2(600, 44), "CONTROLS", 28,
            PrologueUiPalette.Brass, HorizontalAlignment.Center));
        panel.AddChild(Rule("TopRule", new Vector2(70, 92), new Vector2(560, 2)));
        var rows = new VBoxContainer
        {
            Name = "Rows", Position = new Vector2(80, 124), Size = new Vector2(540, 320), Theme = theme
        };
        rows.AddThemeConstantOverride("separation", 10);
        foreach (string name in new[]
                 { "Move", "Jump", "Dash", "AttackMode", "GrappleMode", "Shield", "Loadout", "Pause" })
            rows.AddChild(Label(name, Vector2.Zero, new Vector2(540, 40), string.Empty, 20,
                PrologueUiPalette.Bone, HorizontalAlignment.Left));
        panel.AddChild(rows);
        panel.AddChild(new Button
        {
            Name = "Back", Position = new Vector2(450, 494), Size = new Vector2(170, 48), Theme = theme
        });
        controls.AddChild(panel);
        return controls;
    }

    private static HBoxContainer SliderRow(string name, double min, double max, double step)
    {
        var row = Row(name);
        row.AddChild(RowLabel());
        row.AddChild(new HSlider
        {
            Name = "Value", MinValue = min, MaxValue = max, Step = step,
            CustomMinimumSize = new Vector2(330, 34), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        });
        return row;
    }

    private static HBoxContainer CheckRow(string name)
    {
        var row = Row(name);
        row.AddChild(RowLabel());
        row.AddChild(new CheckButton { Name = "Value", CustomMinimumSize = new Vector2(120, 34) });
        return row;
    }

    private static HBoxContainer OptionRow(string name, string[] values)
    {
        var row = Row(name);
        row.AddChild(RowLabel());
        var option = new OptionButton
        {
            Name = "Value", CustomMinimumSize = new Vector2(330, 34), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        foreach (string value in values) option.AddItem(value);
        row.AddChild(option);
        return row;
    }

    private static HBoxContainer Row(string name) => new()
    {
        Name = name, CustomMinimumSize = new Vector2(680, 34), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };

    private static Label RowLabel() => new()
    {
        Name = "Label", CustomMinimumSize = new Vector2(280, 34), VerticalAlignment = VerticalAlignment.Center
    };

    private static Button MenuButton(string name) => new()
    {
        Name = name,
        CustomMinimumSize = new Vector2(420, 54),
        FocusMode = Control.FocusModeEnum.All,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
    };

    private static Panel CenterPanel(string name, Vector2 size, Theme theme)
    {
        var panel = new Panel { Name = name, Theme = theme, MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AnchorLeft = panel.AnchorRight = panel.AnchorTop = panel.AnchorBottom = 0.5f;
        panel.OffsetLeft = -size.X / 2f;
        panel.OffsetRight = size.X / 2f;
        panel.OffsetTop = -size.Y / 2f;
        panel.OffsetBottom = size.Y / 2f;
        return panel;
    }

    private static T FullRect<T>(T control) where T : Control
    {
        control.AnchorRight = 1f;
        control.AnchorBottom = 1f;
        control.OffsetLeft = control.OffsetTop = control.OffsetRight = control.OffsetBottom = 0f;
        return control;
    }

    private static ColorRect Rule(string name, Vector2 position, Vector2 size) => new()
    {
        Name = name, Position = position, Size = size, Color = new Color(PrologueUiPalette.Brass, 0.7f),
        MouseFilter = Control.MouseFilterEnum.Ignore
    };

    private static Label Label(string name, Vector2 position, Vector2 size, string text, int fontSize,
        Color color, HorizontalAlignment alignment)
    {
        var label = new Label
        {
            Name = name,
            Position = position,
            Size = size,
            Text = text,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }
}
