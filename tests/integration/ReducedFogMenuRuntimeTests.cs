using System;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Presentation.Fog;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

// Real saved menu and Flow persistence, not ShowSettings(testData).
// This does not enable the formal fog or claim a rendered-opacity proof.
public static class ReducedFogMenuRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        Vector2I windowSize = DisplayServer.WindowGetSize();
        DisplayServer.WindowMode windowMode = DisplayServer.WindowGetMode();
        DisplayServer.VSyncMode vsync = DisplayServer.WindowGetVsyncMode();
        float scale = tree.Root.ContentScaleFactor;
        PackedScene scene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = null;
        string settingsPath = null;
        try
        {
            level = scene.Instantiate<PrologueLevelController>();
            var paths = IsolatedPrologueFixture.Configure(level, "reduced-fog-real-title-menu");
            settingsPath = paths.SettingsPath;
            GameSettingsStore.SaveAtomic(settingsPath, new GameSettingsData
            {
                MasterVolume = .42f, ReduceFlashes = true, ReducedFog = true,
                GuidanceEnabled = false, Language = GameLanguage.English,
                WindowMode = GameWindowMode.Windowed, ResolutionWidth = 1280,
                ResolutionHeight = 720, VSync = false, UiScale = 1f
            });
            PrologueGameFlowController flow = Prepare(level);
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State, "menu fixture really begins at Title");
            TestAssert.True(flow.Settings.ReducedFog && flow.Settings.ReduceFlashes &&
                Math.Abs(flow.Settings.MasterVolume - .42f) < .001f,
                "Flow actually loaded the isolated nondefault settings before menu input");

            Press(level, "MainMenu/Panel/Buttons/Settings");
            await Frames(tree, 2);
            AssertValues(level, true, GameLanguage.English);
            AssertLayout(level, "English loaded menu");
            Reduced(level).ButtonPressed = false;
            Press(level, "SettingsMenu/Panel/Back");
            await Frames(tree, 2);
            AssertStored(flow, settingsPath, false, GameLanguage.English);
            Press(level, "MainMenu/Panel/Buttons/Settings");
            await Frames(tree, 2);
            AssertValues(level, false, GameLanguage.English);
            level.GetNode<OptionButton>("ProductUi/SettingsMenu/Panel/Rows/Language/Value").Selected =
                (int)GameLanguage.ChineseSimplified;
            Reduced(level).ButtonPressed = true;
            Press(level, "SettingsMenu/Panel/Back");
            await Frames(tree, 2);
            AssertStored(flow, settingsPath, true, GameLanguage.ChineseSimplified);
            Press(level, "MainMenu/Panel/Buttons/Settings");
            await Frames(tree, 2);
            AssertValues(level, true, GameLanguage.ChineseSimplified);
            AssertLayout(level, "Chinese reopened menu");
            Press(level, "SettingsMenu/Panel/Back");

            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            level = null;
            level = scene.Instantiate<PrologueLevelController>();
            IsolatedPrologueFixture.Configure(level, "reduced-fog-menu-reloaded");
            flow = Prepare(level);
            // Reuse only the first already-validated external settings path;
            // Configure's fresh save/telemetry paths remain isolated.
            flow.SettingsPath = settingsPath;
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            Press(level, "MainMenu/Panel/Buttons/Settings");
            await Frames(tree, 2);
            AssertValues(level, true, GameLanguage.ChineseSimplified);
            AssertStored(flow, settingsPath, true, GameLanguage.ChineseSimplified);
            var canvas = level.GetNode<PrologueFogCanvas>("Fog/Canvas");
            TestAssert.True(!canvas.Visible && !canvas.IsVisibleInTree(),
                "settings-only acceptance must not open the formal fog visibility gate");
            GD.Print("REDUCED_FOG_REAL_TITLE_MENU_OK saved_reloaded=true bilingual=true formal_fog_enabled=false");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            tree.Root.ContentScaleFactor = scale;
            DisplayServer.WindowSetMode(windowMode);
            if (windowMode == DisplayServer.WindowMode.Windowed) DisplayServer.WindowSetSize(windowSize);
            DisplayServer.WindowSetVsyncMode(vsync);
        }
    }

    private static PrologueGameFlowController Prepare(PrologueLevelController level)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        return flow;
    }

    private static void Press(PrologueLevelController level, string path) =>
        level.GetNode<Button>("ProductUi/" + path).EmitSignal(Button.SignalName.Pressed);

    private static CheckButton Reduced(PrologueLevelController level) =>
        level.GetNode<CheckButton>("ProductUi/SettingsMenu/Panel/Rows/ReducedFog/Value");

    private static void AssertValues(PrologueLevelController level, bool reduced, GameLanguage language)
    {
        var ui = level.GetNode<PrologueUiRoot>("ProductUi");
        TestAssert.True(ui.GetNode<Control>("SettingsMenu").Visible, "real title button opened Settings");
        TestAssert.True(Math.Abs(ui.GetNode<HSlider>("SettingsMenu/Panel/Rows/Master/Value").Value - .42) < .001,
            "real Title Settings button must preserve loaded nondefault master volume");
        TestAssert.True(ui.GetNodeOrNull<CheckButton>("SettingsMenu/Panel/Rows/ReducedFog/Value") != null,
            "saved menu has the actual ReducedFog checkbox before reading it");
        TestAssert.Equal(reduced, Reduced(level).ButtonPressed, "loaded/reopened ReducedFog matches saved value");
        TestAssert.True(ui.GetNode<CheckButton>("SettingsMenu/Panel/Rows/ReduceFlashes/Value").ButtonPressed,
            "ReducedFog cannot reset the independent ReduceFlashes choice");
        TestAssert.Equal(language == GameLanguage.English ? "Reduce fog" : "降低迷雾",
            ui.GetNode<Label>("SettingsMenu/Panel/Rows/ReducedFog/Label").Text,
            "real settings label is localized, not a missing key");
        TestAssert.Equal(language, ui.Language, "current menu language follows the complete cached settings");
    }

    private static void AssertStored(PrologueGameFlowController flow, string path, bool reduced, GameLanguage language)
    {
        foreach (GameSettingsData value in new[] { flow.Settings, GameSettingsStore.LoadOrDefault(path) })
            TestAssert.True(value.ReducedFog == reduced && value.ReduceFlashes &&
                !value.GuidanceEnabled && Math.Abs(value.MasterVolume - .42f) < .001f && value.Language == language,
                "actual Back preserves independent settings in Flow and the real atomic JSON");
    }

    private static void AssertLayout(PrologueLevelController level, string phase)
    {
        Control panel = level.GetNode<Control>("ProductUi/SettingsMenu/Panel");
        Control rows = panel.GetNode<Control>("Rows");
        Rect2 safePanel = panel.GetGlobalRect().Grow(-12f);
        TestAssert.Equal(13, rows.GetChildCount(), "the saved settings scene contains exactly thirteen rows");
        float previousBottom = float.NegativeInfinity;
        foreach (Node child in rows.GetChildren())
        {
            var row = (Control)child;
            Rect2 rect = row.GetGlobalRect();
            Label label = row.GetNode<Label>("Label");
            TestAssert.True(safePanel.Encloses(rect) && rect.Position.Y >= previousBottom,
                $"{phase}: settings rows fit the actual panel without overlap: {row.Name} {rect}");
            TestAssert.True(label.GetMinimumSize().X <= label.Size.X + 1f &&
                label.GetMinimumSize().Y <= label.Size.Y + 1f,
                $"{phase}: localized settings label is not clipped: {row.Name}");
            previousBottom = rect.End.Y;
        }
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }
}
