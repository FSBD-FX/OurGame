using Godot;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class PrologueUiStructureTests
{
    public static void Run()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn");
        TestAssert.True(scene != null, "formal prologue UI scene exists");
        PrologueUiRoot root = scene.Instantiate<PrologueUiRoot>();
        try
        {
            Require<Button>(root, "MainMenu/Panel/Buttons/Start");
            Require<Button>(root, "MainMenu/Panel/Buttons/Continue");
            Require<Button>(root, "MainMenu/Panel/Buttons/Settings");
            Require<Button>(root, "MainMenu/Panel/Buttons/Controls");
            Require<Button>(root, "MainMenu/Panel/Buttons/Quit");
            Require<PrologueSettingsMenu>(root, "SettingsMenu");
            Require<PrologueControlsView>(root, "ControlsView");
            foreach (string row in new[]
                     { "AttackMode", "GrappleMode", "Shield", "Loadout" })
                Require<Label>(root, $"ControlsView/Panel/Rows/{row}");
            Panel mainPanel = Require<Panel>(root, "MainMenu/Panel");
            TestAssert.True(mainPanel.Theme != null, "formal main menu has the production theme");
            TestAssert.True(mainPanel.AnchorLeft == 0.5f && mainPanel.AnchorRight == 0.5f &&
                            mainPanel.AnchorTop == 0.5f && mainPanel.AnchorBottom == 0.5f,
                "main panel remains centered by anchors");
        }
        finally { root.Free(); }
    }

    private static T Require<T>(Node root, NodePath path) where T : Node
    {
        T node = root.GetNodeOrNull<T>(path);
        TestAssert.True(node != null, $"formal UI exposes {path} as {typeof(T).Name}");
        return node;
    }
}
