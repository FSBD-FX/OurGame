using System.Threading.Tasks;
using Godot;
using Knight.Settings;
using Knight.Ui;
using Knight.Prologue;

namespace Knight.Tests.Integration;

public static class PrologueUiRuntimeTests
{
    private static readonly Vector2I[] Resolutions =
    [
        new(1280, 720), new(1920, 1080), new(2560, 1440), new(3840, 2160)
    ];

    public static async Task Run(SceneTree tree)
    {
        foreach (Vector2I resolution in Resolutions)
            await MainMenuFitsSafeAreaAndFocusesStart(tree, resolution);
        await NavigationAndInputGlyphsWork(tree);
    }

    public static async Task Task10StableHudPathsExist(SceneTree tree)
    {
        foreach (Vector2I resolution in Resolutions)
        {
            var viewport = new SubViewport { Name = $"Task10Hud{resolution.X}x{resolution.Y}",
                Size = resolution };
            PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            level.GetNode<Knight.Audio.PrologueAudioDirector>("Audio").PlaybackEnabled = false;
            tree.Root.AddChild(viewport);
            viewport.AddChild(level);
            for (int frame = 0; frame < 3; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            string[] paths =
            [
                "Hud/FormalHud/ResourceCluster/Energy",
                "Hud/FormalHud/ResourceCluster/Heat",
                "Hud/FormalHud/ModeSelector/Attack",
                "Hud/FormalHud/ModeSelector/Grapple",
                "Hud/FormalHud/Loadout/Slot1",
                "Hud/FormalHud/Loadout/Slot2",
                "Hud/FormalHud/Loadout/Slot3",
                "Hud/FormalHud/Loadout/Slot4",
                "Hud/FormalHud/AimReticle"
            ];
            foreach (string path in paths)
                TestAssert.True(level.GetNodeOrNull(path) != null,
                    $"Task10 generated runtime exposes stable HUD path {path} at {resolution}");

            PrologueFormalHud formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            PrologueHudLayout expected = PrologueHudLayout.Calculate(resolution);
            TestAssert.Equal(expected.ModeSelectorRect,
                new Rect2(formal.GetNode<Control>("ModeSelector").Position,
                    formal.GetNode<Control>("ModeSelector").Size),
                $"Task10 mounted immutable mode frame uses the exact layout at {resolution}");
            TestAssert.Equal(expected.ResourceClusterRect,
                new Rect2(formal.GetNode<Control>("ResourceCluster").Position,
                    formal.GetNode<Control>("ResourceCluster").Size),
                $"Task10 mounted immutable energy/heat frame uses the exact layout at {resolution}");
            Control[] protectedHud =
            [
                formal.GetNode<Control>("Health"), formal.GetNode<Control>("ObjectiveRibbon"),
                formal.GetNode<Control>("Prompt"), formal.GetNode<Control>("ModeSelector"),
                formal.GetNode<Control>("Loadout"), formal.GetNode<Control>("ResourceCluster"),
                formal.GetNode<Control>("Boss")
            ];
            for (int first = 0; first < protectedHud.Length; first++)
            for (int second = first + 1; second < protectedHud.Length; second++)
                TestAssert.True(!protectedHud[first].GetGlobalRect().Intersects(
                        protectedHud[second].GetGlobalRect()),
                    $"Task10 protected HUD regions {protectedHud[first].Name}/{protectedHud[second].Name} " +
                    $"do not overlap at {resolution}");

            PrologueAimReticle reticle = formal.GetNode<PrologueAimReticle>("AimReticle");
            Rect2 safe = new(new Vector2(48, 40), (Vector2)resolution - new Vector2(96, 80));
            reticle.Present(new PrologueAimReticleFrame(PrologueAimReticleState.OutOfRange,
                new Vector2(-1000, 9000), true), safe);
            TestAssert.True(safe.Encloses(reticle.GetGlobalRect()),
                $"Task10 real reticle frame clamps inside the safe area at {resolution}");

            level.GetNode<Knight.Audio.PrologueAudioDirector>("Audio").Shutdown();
            viewport.QueueFree();
            for (int frame = 0; frame < 5; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static async Task MainMenuFitsSafeAreaAndFocusesStart(SceneTree tree, Vector2I resolution)
    {
        var viewport = new SubViewport { Size = resolution };
        PrologueUiRoot ui = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn")
            .Instantiate<PrologueUiRoot>();
        tree.Root.AddChild(viewport);
        viewport.AddChild(ui);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        ui.ShowMainMenu(hasSave: false);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        Button start = ui.GetNode<Button>("MainMenu/Panel/Buttons/Start");
        Button continueButton = ui.GetNode<Button>("MainMenu/Panel/Buttons/Continue");
        TestAssert.True(continueButton.Disabled, $"Continue is disabled without save at {resolution}");
        TestAssert.True(viewport.GuiGetFocusOwner() == start, $"Start owns focus at {resolution}");
        foreach (Node child in ui.GetNode("MainMenu/Panel/Buttons").GetChildren())
        {
            if (child is not Button button) continue;
            Rect2 rect = button.GetGlobalRect();
            TestAssert.True(rect.Position.X >= 48f && rect.Position.Y >= 48f &&
                            rect.End.X <= resolution.X - 48f && rect.End.Y <= resolution.Y - 48f,
                $"{button.Name} stays inside the 48px safe area at {resolution}");
        }

        ui.QueueFree();
        viewport.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task NavigationAndInputGlyphsWork(SceneTree tree)
    {
        var viewport = new SubViewport { Size = new Vector2I(1280, 720) };
        PrologueUiRoot ui = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn")
            .Instantiate<PrologueUiRoot>();
        tree.Root.AddChild(viewport);
        viewport.AddChild(ui);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        ui.ShowMainMenu(hasSave: true);
        TestAssert.True(!ui.GetNode<Button>("MainMenu/Panel/Buttons/Continue").Disabled,
            "Continue is enabled with valid save");
        ui.ShowSettings(new GameSettingsData { Language = GameLanguage.English });
        TestAssert.True(ui.GetNode<Control>("SettingsMenu").Visible && !ui.GetNode<Control>("MainMenu").Visible,
            "settings replaces the main menu");
        ui.HandleBack();
        TestAssert.True(ui.GetNode<Control>("MainMenu").Visible, "back returns from settings to main menu");

        ui.ShowControls();
        ui.SetLastInputDevice(InputDeviceKind.Gamepad);
        Label jump = ui.GetNode<Label>("ControlsView/Panel/Rows/Jump");
        TestAssert.True(jump.Text.Contains("[A]"), "controls view switches to gamepad glyphs");
        ui.SetLastInputDevice(InputDeviceKind.KeyboardMouse);
        TestAssert.True(jump.Text.Contains("[SPACE]"), "controls view switches back to keyboard glyphs");

        ui.QueueFree();
        viewport.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
