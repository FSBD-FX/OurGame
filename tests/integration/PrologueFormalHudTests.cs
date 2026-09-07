using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Shared;
using Knight.Ui;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class PrologueFormalHudTests
{
    public static async Task Run(SceneTree tree)
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TestAssert.True(scene != null, "formal HUD test loads prologue level");
        PrologueLevelController level = scene.Instantiate<PrologueLevelController>();
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        PrologueHud hud = level.GetNode<PrologueHud>("Hud");
        PrologueFormalHud formal = hud.GetNode<PrologueFormalHud>("FormalHud");
        ThreadShuttleVisual shuttle = level.GetNodeOrNull<ThreadShuttleVisual>(
            "PlayerRoot/Player/VisualRoot/MechanicalClaw");
        TestAssert.True(shuttle != null,
            "generated Player exposes the stable real mechanical-claw visual path");
        TestAssert.Equal(5, formal.GetNode("Health/Shards").GetChildCount(),
            "formal HUD exposes five independent health shards");
        TestAssert.True(formal.GetNodeOrNull<ProgressBar>("ThreadSpool/Cooldown") == null &&
                        !formal.GetNode<Control>("ThreadSpool").Visible,
            "Task10 removes player-facing thread cooldown presentation while preserving the hidden adapter root");
        TestAssert.True(formal.GetNodeOrNull<Control>("ModeSelector/Attack") != null &&
                        formal.GetNodeOrNull<Control>("ModeSelector/Grapple") != null,
            "formal HUD exposes stable Attack and Grapple mode paths");
        for (int slot = 1; slot <= 4; slot++)
            TestAssert.True(formal.GetNodeOrNull<Control>($"Loadout/Slot{slot}") != null,
                $"formal HUD exposes stable loadout slot {slot}");
        TestAssert.True(formal.GetNodeOrNull<ProgressBar>("ResourceCluster/Energy") != null &&
                        formal.GetNodeOrNull<ProgressBar>("ResourceCluster/Heat") != null,
            "formal HUD exposes stable energy and heat meter paths");
        AssertDoesNotOverlap(formal.GetNode<Control>("ModeSelector"), formal.GetNode<Control>("Health"),
            "mode selector avoids health at the supported 1280-by-720 baseline");
        AssertDoesNotOverlap(formal.GetNode<Control>("ModeSelector"),
            formal.GetNode<Control>("ObjectiveRibbon"),
            "mode selector avoids objective ribbon at the supported 1280-by-720 baseline");
        AssertDoesNotOverlap(formal.GetNode<Control>("Loadout"), formal.GetNode<Control>("ObjectiveRibbon"),
            "loadout avoids objective ribbon at the supported 1280-by-720 baseline");
        AssertDoesNotOverlap(formal.GetNode<Control>("ResourceCluster"), formal.GetNode<Control>("Prompt"),
            "resource cluster avoids contextual prompt at the supported 1280-by-720 baseline");
        TestAssert.True(hud.GetNodeOrNull<WorldTargetMarkerVisual>("Guidance/VisualRoot") != null,
            "formal world target marker replaces placeholder guidance visual");
        TestAssert.True(formal.GetNodeOrNull<Control>("Boss") != null,
            "formal Boss bar exists");
        TestAssert.True(formal.GetNodeOrNull<PrologueRecoveryView>("Recovery") != null,
            "formal recovery overlay exists");
        TestAssert.True(formal.GetNodeOrNull<PrologueCompletionView>("Completion") != null,
            "formal completion card exists");

        PrologueHudViewModel routerModel = formal.CurrentModel.WithActionState(
            PlayerActionMode.Grapple, 4, .42f, .86f, true, PlayerActionRejectionReason.EmptySlot);
        formal.Present(routerModel);
        TestAssert.True(!formal.GetNode<Control>("ModeSelector/Attack/SelectedMarker").Visible &&
                        formal.GetNode<Control>("ModeSelector/Grapple/SelectedMarker").Visible,
            "formal HUD renders selected and unselected modes with an explicit shape marker");
        TestAssert.True(formal.GetNode<Control>("Loadout/Slot4/SelectedMarker").Visible &&
                        formal.GetNode<Control>("Loadout/Slot4/EmptyMarker").Visible,
            "formal HUD renders a selected empty slot with separate selection and empty markers");
        TestAssert.Near(.42f, (float)formal.GetNode<ProgressBar>("ResourceCluster/Energy").Value, .0001f,
            "formal HUD renders the exact energy ratio from its immutable model");
        TestAssert.Near(.86f, (float)formal.GetNode<ProgressBar>("ResourceCluster/Heat").Value, .0001f,
            "formal HUD renders the exact heat ratio from its immutable model");
        TestAssert.True(formal.GetNode<Control>("ResourceCluster/Heat/OverheatMarker").Visible,
            "formal HUD renders overheat with a non-color shape marker");
        TestAssert.True(formal.GetNode<Control>("Loadout/RejectionMarker").Visible,
            "formal HUD renders a primary rejection with a non-color shape marker");
        TestAssert.Equal(string.Empty, formal.GetNode<Label>("ThreadSpool/State").Text,
            "hidden legacy adapter root contains no player-facing state text");

        hud.SetHealth(3, 5);
        hud.SetObjective(PrologueObjectiveModel.TextFor(PrologueObjective.LearnAttackMode));
        TestAssert.Equal("选择攻击模式", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "default Chinese locale translates the current action-mode objective");
        hud.ShowPrompt("[E + LMB]", "拉动机关");
        AssertDoesNotOverlap(formal.GetNode<Control>("Prompt/Glyph"),
            formal.GetNode<Control>("Prompt/Text"),
            "compound current-action glyph stays separate from prompt copy");
        hud.ShowBoss("坠落钟守", 9, 18, 2, false);
        TestAssert.True(formal.GetNode<Control>("Boss").Visible, "formal Boss bar becomes visible");
        formal.SetLanguage(Knight.Settings.GameLanguage.ChineseSimplified);
        TestAssert.Equal("选择攻击模式", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "Chinese locale translates the current action-mode objective");
        TestAssert.Equal("阶段 II · 护甲闭合", formal.GetNode<Label>("Boss/State").Text,
            "Chinese locale presents a coherent Boss state");
        formal.SetLanguage(Knight.Settings.GameLanguage.English);
        TestAssert.Equal("SELECT ATTACK MODE", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "English locale preserves the exact continuous objective text");
        TestAssert.Equal("PHASE II · ARMOR CLOSED", formal.GetNode<Label>("Boss/State").Text,
            "English locale presents the same coherent Boss state without mixed-language residue");
        await AssertActionRouterLayoutAtSupportedScales(tree, formal);
        hud.ShowPrompt("[E + LMB]", "PULL THE MECHANISM");
        formal.SetInputDevice(InputDeviceKind.Gamepad);
        TestAssert.Equal("[Y + RB]", formal.GetNode<Label>("Prompt/Glyph").Text,
            "context prompt switches from keyboard to current controller grapple and primary glyphs");
        formal.SetInputDevice(InputDeviceKind.KeyboardMouse);
        TestAssert.Equal("[E + LMB]", formal.GetNode<Label>("Prompt/Glyph").Text,
            "context prompt switches back to current keyboard and mouse grapple glyphs");
        formal.Present(formal.CurrentModel.WithObjective(formal.CurrentModel.Objective,
            PrologueHudViewModel.ObjectiveExpandDurationSeconds + .1f));
        formal.SetUiScale(1.5f);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(PrologueHudLayout.Calculate(formal.Size, 1.5f).HealthRect.Size,
            formal.GetNode<Control>("Health").Size,
            "mounted formal HUD applies the supported large UI-scale endpoint");
        Control shards = formal.GetNode<Control>("Health/Shards");
        TestAssert.Near(285f, shards.Size.X, 2.1f,
            "UI scale 1.5 expands the mounted health-shard row width within integer spacing");
        TestAssert.Near(63f, shards.Size.Y, .1f,
            "UI scale 1.5 expands the mounted health-shard row height");
        TestAssert.Equal(new Vector2(51f, 63f),
            formal.GetNode<Control>("Health/Shards/Shard1").CustomMinimumSize,
            "UI scale 1.5 expands each real health shard, not only its parent");
        TestAssert.True(shards.GetThemeConstant("separation") is >= 7 and <= 8,
            "UI scale 1.5 expands the health-shard spacing proportionally");
        TestAssert.Equal(22, formal.GetNode<Label>("ObjectiveRibbon/Text")
                .GetThemeFontSize("font_size"),
            "UI scale 1.5 expands the objective type");
        TestAssert.Equal(28, formal.GetNode<Label>("Prompt/Glyph")
                .GetThemeFontSize("font_size"),
            "UI scale 1.5 expands the action glyph type");
        TestAssert.Equal(24, formal.GetNode<Label>("Prompt/Text")
                .GetThemeFontSize("font_size"),
            "UI scale 1.5 expands the prompt type");
        TestAssert.Equal(26, formal.GetNode<Label>("Boss/Name")
                .GetThemeFontSize("font_size"),
            "UI scale 1.5 expands the Boss name type");
        TestAssert.Equal(21, formal.GetNode<Label>("Boss/State")
                .GetThemeFontSize("font_size"),
            "UI scale 1.5 expands the Boss state type");
        AssertContained(formal.GetNode<Control>("Health"), shards,
            "scaled health shards remain inside the health parent");
        AssertPanelContentsContained(formal, "ObjectiveRibbon",
            "scaled objective content remains inside its parent");
        AssertPanelContentsContained(formal, "Prompt",
            "scaled prompt content remains inside its parent");
        AssertPanelContentsContained(formal, "Boss",
            "scaled Boss content remains inside its parent");
        formal.SetUiScale(1f);
        formal.Present(formal.CurrentModel.WithThreadCast(ThreadCastState.Failed,
            ThreadFailureReason.Cooldown));
        TestAssert.Equal(string.Empty, formal.GetNode<Label>("ThreadSpool/State").Text,
            "legacy adapter cooldown mutation cannot restore stale player-facing text");

        PrologueHudViewModel expandedObjective = formal.CurrentModel.WithObjective("打开钟链", 0f);
        formal.Present(expandedObjective);
        TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").Size.Y > 34f,
            "fresh objective transition briefly expands the objective ribbon");
        formal.Present(formal.CurrentModel.AdvanceObjectiveTransition(.75f) with { Objective = "击败钟守" });
        TestAssert.Equal(0f, formal.CurrentModel.ObjectiveTransitionSeconds,
            "a rapid objective replacement restarts its own readable expansion window");
        PrologueHudViewModel activeObjective = formal.CurrentModel;
        formal.Present(activeObjective.AdvanceObjectiveTransition(2.5f));
        TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").Size.Y <= 34f &&
                        formal.GetNode<Label>("ObjectiveRibbon/Text").AutowrapMode ==
                        TextServer.AutowrapMode.Off,
            "settled objective collapses to one clipped line in ordinary play");
        formal.Present(activeObjective.WithThread(.5f));
        TestAssert.True(!formal.CurrentModel.ObjectiveExpanded,
            "routine HUD refresh cannot reopen an already-settled objective");

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        claw.ResetTransientState();
        formal.Present(PrologueHudViewModel.Gameplay(5, 5, 1f, "", "", ""));
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        try
        {
            Input.ActionPress(InputActions.SelectGrappleMode);
            Input.ActionPress(InputActions.PrimaryAction);
            input._PhysicsProcess(0d);
            PlayerInputFrame sampled = input.CurrentFrame with
            {
                MouseWorld = player.GlobalPosition + Vector2.Right * 100f
            };
            typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
                BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, sampled);
            modes._PhysicsProcess(0d);
            claw._PhysicsProcess(1d / 60d);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(MechanicalClawState.Aim, claw.CurrentFrame.State,
                "real E plus primary input reaches mechanical-claw Aim");
            TestAssert.Equal(PlayerActionMode.Grapple, formal.CurrentModel.ActionMode,
                "formal HUD follows the live grapple action mode");
            TestAssert.True(formal.GetNode<Control>("ModeSelector/Grapple/SelectedMarker").Visible,
                "live grapple selection is visible without a test-only Present call");
        }
        finally
        {
            Input.ActionRelease(InputActions.SelectGrappleMode);
            Input.ActionRelease(InputActions.PrimaryAction);
            claw.ResetTransientState();
            input.SetPhysicsProcess(true);
            modes.SetPhysicsProcess(true);
            claw.SetPhysicsProcess(true);
        }
        PrologueAimReticle reticle = formal.GetNode<PrologueAimReticle>("AimReticle");
        TestAssert.True(reticle.Visible, "ordinary gameplay keeps the aim reticle visible");
        hud.ShowRecovery("坠落 · 墓灯重燃");
        TestAssert.True(formal.GetNode<Control>("Recovery").Visible, "recovery overlay becomes visible");
        TestAssert.True(!reticle.Visible,
            "recovery immediately hides the aim reticle instead of drawing over recovery text");
        formal._Process(0d);
        TestAssert.True(!reticle.Visible && !reticle.CurrentFrame.Visible,
            "normal aim updates cannot restore the reticle while recovery blocks gameplay");
        hud.HideRecovery();
        TestAssert.True(reticle.Visible,
            "dismissed recovery restores the ordinary gameplay reticle");
        hud.ShowCompletion("00:12:34", 4, true, true);
        TestAssert.True(formal.GetNode<Control>("Completion").Visible, "completion card becomes visible");
        TestAssert.True(formal.GetNode<Label>("Completion/Panel/Stats").Text.Contains("00:12:34"),
            "completion card renders exact time");
        TestAssert.True(!reticle.Visible,
            "completion immediately hides the aim reticle instead of obscuring completion statistics");
        formal._Process(0d);
        TestAssert.True(!reticle.Visible && !reticle.CurrentFrame.Visible,
            "normal aim updates keep the reticle hidden while completion blocks gameplay");
        formal.Present(PrologueHudViewModel.Gameplay(5, 5, 1f, "", "", ""));
        TestAssert.True(reticle.Visible && reticle.CurrentFrame.Visible,
            "returning from completion to ordinary gameplay restores reticle presentation");

        level.GetParent().RemoveChild(level);
        level.Free();
    }

    private static void AssertPanelContentsContained(PrologueFormalHud formal, string path,
        string message)
    {
        Control parent = formal.GetNode<Control>(path);
        foreach (Node child in parent.GetChildren())
            if (child is Control control && control.Visible) AssertContained(parent, control, message);
    }

    private static void AssertContained(Control parent, Control child, string message)
    {
        TestAssert.True(parent.GetGlobalRect().Encloses(child.GetGlobalRect()),
            $"{message}: parent={parent.GetGlobalRect()} child={child.GetGlobalRect()}");
    }

    private static void AssertDoesNotOverlap(Control first, Control second, string message)
    {
        TestAssert.True(!first.GetGlobalRect().Intersects(second.GetGlobalRect()),
            $"{message}: first={first.GetGlobalRect()} second={second.GetGlobalRect()}");
    }

    private static async Task AssertActionRouterLayoutAtSupportedScales(SceneTree tree,
        PrologueFormalHud formal)
    {
        foreach (float scale in new[] { .8f, 1f, 1.5f })
        {
            formal.SetUiScale(scale);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Control mode = formal.GetNode<Control>("ModeSelector");
            Control loadout = formal.GetNode<Control>("Loadout");
            Control resources = formal.GetNode<Control>("ResourceCluster");
            AssertContained(formal, mode,
                $"mode selector stays contained at supported UI scale {scale}");
            AssertContained(formal, loadout,
                $"loadout stays contained at supported UI scale {scale}");
            AssertContained(formal, resources,
                $"resource cluster stays contained at supported UI scale {scale}");
            AssertPanelContentsContained(formal, "ModeSelector",
                $"mode selector content stays contained at supported UI scale {scale}");
            AssertPanelContentsContained(formal, "Loadout",
                $"loadout content stays contained at supported UI scale {scale}");
            AssertPanelContentsContained(formal, "ResourceCluster",
                $"resource cluster content stays contained at supported UI scale {scale}");
            AssertDoesNotOverlap(formal.GetNode<Control>("Prompt/Glyph"),
                formal.GetNode<Control>("Prompt/Text"),
                $"compound prompt glyph stays separate from copy at supported UI scale {scale}");
            AssertDoesNotOverlap(mode, loadout,
                $"mode selector and loadout do not overlap at supported UI scale {scale}");
            AssertDoesNotOverlap(mode, resources,
                $"mode selector and resource cluster do not overlap at supported UI scale {scale}");
            AssertDoesNotOverlap(loadout, resources,
                $"loadout and resource cluster do not overlap at supported UI scale {scale}");
            foreach (Control action in new[] { mode, loadout, resources })
            {
                AssertDoesNotOverlap(action, formal.GetNode<Control>("Health"),
                    $"action HUD avoids health at supported UI scale {scale}");
                AssertDoesNotOverlap(action, formal.GetNode<Control>("ThreadSpool"),
                    $"action HUD avoids thread spool at supported UI scale {scale}");
                AssertDoesNotOverlap(action, formal.GetNode<Control>("ObjectiveRibbon"),
                    $"action HUD avoids objective ribbon at supported UI scale {scale}");
                AssertDoesNotOverlap(action, formal.GetNode<Control>("Prompt"),
                    $"action HUD avoids prompt at supported UI scale {scale}");
            }
        }
        formal.SetUiScale(1f);
    }
}
