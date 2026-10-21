using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class PrologueMigrationMenuTests
{
    public static async Task Run(SceneTree tree, bool capture = false)
    {
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        var viewport = new SubViewport
        {
            Size = new Vector2I(1280, 720), RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        var ui = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn").Instantiate<PrologueUiRoot>();
        try
        {
            tree.Root.AddChild(viewport);
            viewport.AddChild(ui);
            await Frame(tree);
            var menu = ui.GetNode<PrologueMainMenu>("MainMenu");
            var start = menu.GetNode<Button>("Panel/Buttons/Start");
            var resume = menu.GetNode<Button>("Panel/Buttons/Continue");
            var footer = menu.GetNode<Label>("Panel/Footer");
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            foreach (PrologueSaveLoadStatus status in new[] { PrologueSaveLoadStatus.MigratableLegacy,
                         PrologueSaveLoadStatus.NeedsNewLayoutChoice, PrologueSaveLoadStatus.Recovered })
            {
                menu.MigrationStatus = status;
                menu.Present(true, language);
                await Frame(tree);
                bool chinese = language == GameLanguage.ChineseSimplified;
                TestAssert.True(start.Text.Contains(chinese ? "旧存档保留" : "old save kept"),
                    $"migration menu preserves chosen language: {language}/{status}/Start");
                TestAssert.True(resume.Text.Contains(chinese ? "映射存档点" : "mapped checkpoint"),
                    $"migration menu preserves chosen language: {language}/{status}/Continue");
                TestAssert.True(footer.Text.Contains(status == PrologueSaveLoadStatus.Recovered
                        ? chinese ? "备份" : "backup" : chinese ? "旧存档" : "old save"),
                    "localized footer explains preservation or unreadable-slot backup");
                TestAssert.Equal(status != PrologueSaveLoadStatus.MigratableLegacy, resume.Disabled,
                    "only a valid mapped legacy save can Continue");
                AssertFits(menu);
                if (capture && status is PrologueSaveLoadStatus.MigratableLegacy or PrologueSaveLoadStatus.Recovered)
                {
                    await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using Image image = viewport.GetTexture().GetImage();
                    string file = Path.Combine(storage, $"menu-{language}-{status}.png");
                    TestAssert.Equal(Error.Ok, image.SavePng(file), "save rendered menu original");
                }
                menu.Present(false, language);
                TestAssert.True(resume.Disabled, "a missing slot cannot Continue even with migration status");
                menu.BasinQualitySampleMode = true;
                menu.Present(true, language);
                TestAssert.True(!start.Text.Contains("old save kept") && !start.Text.Contains("旧存档保留"),
                    "sample mode does not inherit migration choices");
                menu.BasinQualitySampleMode = false;
                menu.MigrationStatus = null;
                menu.Present(true, language);
                TestAssert.True(!resume.Disabled && !start.Text.Contains("old save kept") &&
                    !start.Text.Contains("旧存档保留"), "ordinary menu restores default copy and Continue state");
            }
            GD.Print("UI_MIGRATION_MENU_OK languages=2 statuses=3 source=saved-ui-scene writes=external-images-only");
        }
        finally
        {
            viewport.Free();
        }
    }

    private static void AssertFits(PrologueMainMenu menu)
    {
        Rect2 panel = menu.GetNode<Control>("Panel").GetGlobalRect();
        foreach (Node child in menu.GetNode("Panel/Buttons").GetChildren())
        {
            if (child is not Button button) continue;
            float text = button.GetThemeFont("font").GetStringSize(button.Text,
                HorizontalAlignment.Left, -1, button.GetThemeFontSize("font_size")).X;
            StyleBox style = button.GetThemeStylebox("normal");
            TestAssert.True(text + style.GetMinimumSize().X <= button.Size.X + .5f,
                $"migration menu button text fits: {button.Name} text={text} size={button.Size.X}");
            TestAssert.True(panel.Encloses(button.GetGlobalRect()), "menu panel contains every migration button");
        }
        var footer = menu.GetNode<Label>("Panel/Footer");
        float footerText = footer.GetThemeFont("font").GetStringSize(footer.Text,
            HorizontalAlignment.Left, -1, footer.GetThemeFontSize("font_size")).X;
        TestAssert.True(footerText <= 440.5f && panel.Encloses(footer.GetGlobalRect()),
            "migration footer fits the authored 440px area inside panel");
        TestAssert.True(!footer.GetGlobalRect().Intersects(menu.GetNode<Control>("Panel/Buttons").GetGlobalRect()),
            "migration footer does not overlap the button stack");
    }

    private static async Task Frame(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
