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
    public static void AssertBorderlessPresentation(PrologueFormalHud formal)
    {
        foreach (Control control in BorderlessControls(formal))
        {
            if (control.Name == "TopRule")
                TestAssert.True(!control.Visible,
                    "formal HUD decorative panel rule stays hidden: " + formal.GetPathTo(control));
            if (control is Panel panel && panel.GetThemeStylebox("panel") is StyleBoxFlat style)
                AssertNoBorder(style, "mounted panel " + formal.GetPathTo(control));
        }
        foreach (bool overheated in new[] { false, true, false })
        {
            formal.Present(formal.CurrentModel.WithActionState(PlayerActionMode.Grapple, 4,
                .42f, .86f, overheated, PlayerActionRejectionReason.EmptySlot));
            foreach (string name in new[] { "Energy", "Heat" })
            {
                ProgressBar meter = formal.GetNode<ProgressBar>("ResourceCluster/" + name);
                foreach (string part in new[] { "background", "fill" })
                {
                    TestAssert.True(meter.GetThemeStylebox(part) is StyleBoxFlat,
                        "resource meter retains its authored readable style: " + name + "/" + part);
                    var style = (StyleBoxFlat)meter.GetThemeStylebox(part);
                    AssertNoBorder(style, "live meter " + name + "/" + part);
                    TestAssert.True(style.BgColor.A > .1f,
                        "borderless resource meter keeps its track/fill visible: " + name + "/" + part);
                }
            }
            TestAssert.Near(.42f, (float)formal.GetNode<ProgressBar>("ResourceCluster/Energy").Value,
                .0001f, "borderless energy retains the exact resource ratio");
            TestAssert.Near(.86f, (float)formal.GetNode<ProgressBar>("ResourceCluster/Heat").Value,
                .0001f, "borderless heat retains the exact resource ratio");
            TestAssert.Equal(overheated,
                formal.GetNode<Control>("ResourceCluster/Heat/OverheatMarker").Visible,
                "borderless heat retains the non-color overheat cue");
            TestAssert.True(formal.GetNode<Control>("ModeSelector/Grapple/SelectedMarker").Visible &&
                            formal.GetNode<Control>("Loadout/Slot4/SelectedMarker").Visible,
                "borderless choices retain explicit selected-state shapes");
        }
        Theme theme = PrologueUiPalette.CreateTheme();
        try
        {
            AssertNoBorder((StyleBoxFlat)theme.GetStylebox("panel", "Panel"), "panel theme");
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
                AssertNoBorder((StyleBoxFlat)theme.GetStylebox(state, "Button"), "button " + state);
            AssertNoBorder((StyleBoxFlat)theme.GetStylebox("normal", "OptionButton"), "option theme");
            var focus = (StyleBoxFlat)theme.GetStylebox("focus", "Button");
            TestAssert.True(focus.BorderWidthBottom > 0 || focus.BgColor.A > .1f,
                "keyboard focus remains visibly indicated in the borderless theme");
        }
        finally { theme.Dispose(); }
        GD.Print("BORDERLESS_FORMAL_HUD_OK resource_values=True overheat=True selected_shapes=True keyboard_focus=True");
    }

    private static System.Collections.Generic.IEnumerable<Control> BorderlessControls(Node parent)
    {
        foreach (Node node in parent.GetChildren())
        {
            if (node is Control control) yield return control;
            foreach (Control descendant in BorderlessControls(node)) yield return descendant;
        }
    }

    private static void AssertNoBorder(StyleBoxFlat style, string message) =>
        TestAssert.True(style.BorderWidthLeft == 0 && style.BorderWidthTop == 0 &&
                        style.BorderWidthRight == 0 && style.BorderWidthBottom == 0,
            "decorative rectangular border is absent from " + message);

    public static async Task Run(SceneTree tree)
    {
        await AssertResourceLabelsFitBesideMeters(tree);
        PackedScene scene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TestAssert.True(scene != null, "formal HUD test loads prologue level");
        PrologueLevelController level = scene.Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, nameof(PrologueFormalHudTests));
        var viewport = new SubViewport { Name = "FormalHudLayoutViewport", Size = new Vector2I(1280, 720) };
        tree.Root.AddChild(viewport);
        viewport.AddChild(level);
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
        TestAssert.Equal("挥动武器攻击", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "default Chinese locale translates the current action-mode objective");
        hud.ShowPrompt("[E + LMB]", "拉动机关");
        AssertDoesNotOverlap(formal.GetNode<Control>("Prompt/Glyph"),
            formal.GetNode<Control>("Prompt/Text"),
            "compound current-action glyph stays separate from prompt copy");
        hud.ShowBoss("坠落钟守", 9, 18, 2, false);
        TestAssert.True(formal.GetNode<Control>("Boss").Visible, "formal Boss bar becomes visible");
        formal.SetLanguage(Knight.Settings.GameLanguage.ChineseSimplified);
        TestAssert.Equal("挥动武器攻击", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
            "Chinese locale translates the current action-mode objective");
        TestAssert.Equal("阶段 II · 护甲闭合", formal.GetNode<Label>("Boss/State").Text,
            "Chinese locale presents a coherent Boss state");
        formal.SetLanguage(Knight.Settings.GameLanguage.English);
        TestAssert.Equal("STRIKE WITH YOUR WEAPON", formal.GetNode<Label>("ObjectiveRibbon/Text").Text,
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

        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        viewport.GetParent().RemoveChild(viewport);
        viewport.Free();
    }

    internal static PrologueLevelController CreateResourceLabelFixture()
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "resource-label-layout");
        return level;
    }

    private static async Task AssertResourceLabelsFitBesideMeters(SceneTree tree)
    {
        // Regression: a fixed authored label lane must not let the full localized
        // ENERGY text grow underneath a later-drawn meter at small/default UI scales.
        var viewport = new SubViewport { Name = "ResourceLabelLayoutViewport", Size = new Vector2I(1280, 720) };
        var failures = new System.Collections.Generic.List<string>();
        try
        {
            tree.Root.AddChild(viewport);
            PrologueLevelController level = CreateResourceLabelFixture();
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            viewport.AddChild(level);
            // GameFlow applies its initial locale in a deferred callback after mounting.
            // Wait for that real initialization before selecting each measured language;
            // otherwise its default Chinese locale can overwrite the first English sample.
            for (int frame = 0; frame < 6 && flow.State != PrologueGameFlowState.Playing; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "resource-label fixture finishes deferred settings initialization before language measurements");
            PrologueFormalHud formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            foreach (Vector2I resolution in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            foreach (Knight.Settings.GameLanguage language in new[]
                { Knight.Settings.GameLanguage.English, Knight.Settings.GameLanguage.ChineseSimplified })
            foreach (float scale in new[] { 1f, .8f, 1.5f })
            {
                viewport.Size = resolution;
                formal.SetUiScale(scale);
                formal.SetLanguage(language);
                for (int frame = 0; frame < 2; frame++)
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.Equal((Vector2)resolution, formal.Size,
                    "resource-label fixture follows its real viewport dimensions");
                foreach (string resource in new[] { "Energy", "Heat" })
                {
                    Label label = formal.GetNode<Label>($"ResourceCluster/{resource}Label");
                    ProgressBar meter = formal.GetNode<ProgressBar>($"ResourceCluster/{resource}");
                    string expected = language == Knight.Settings.GameLanguage.English
                        ? (resource == "Energy" ? "ENERGY" : "HEAT")
                        : (resource == "Energy" ? "能量" : "热量");
                    TestAssert.Equal(expected, label.Text, "resource caption retains its full localized name");
                    Vector2 textSize = label.GetThemeFont("font").GetStringSize(label.Text,
                        HorizontalAlignment.Left, -1, label.GetThemeFontSize("font_size"));
                    float textHeight = Mathf.Max(textSize.Y, label.GetMinimumSize().Y);
                    Rect2 labelRect = label.GetGlobalRect(), meterRect = meter.GetGlobalRect();
                    string sample = $"{resolution}/{language}/scale={scale}/{resource}";
                    bool fits = label.IsVisibleInTree() && textSize.X <= label.Size.X + .5f &&
                        textHeight <= label.Size.Y + .5f;
                    bool separated = !labelRect.Intersects(meterRect);
                    AssertContained(formal.GetNode<Control>("ResourceCluster"), label,
                        "resource caption stays within the existing cluster at " + sample);
                    AssertContained(formal.GetNode<Control>("ResourceCluster"), meter,
                        "resource meter stays within the existing cluster at " + sample);
                    GD.Print($"HUD_RESOURCE_LABEL sample={sample} text_size={textSize} shaped_height={textHeight:0.00} " +
                        $"label={labelRect} meter={meterRect} fits={fits} separated={separated}");
                    if (!fits || !separated) failures.Add(sample + $" fits={fits} separated={separated}");
                }
            }
            TestAssert.True(failures.Count == 0,
                "full mounted resource captions fit their labels and never overlap their meters: " +
                string.Join("; ", failures));
            GD.Print("HUD_RESOURCE_LABEL_LAYOUT_OK combinations=12 labels=24");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(viewport)) viewport.QueueFree();
            for (int frame = 0; frame < 2; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
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

    private static void AssertActionLabelFits(Control hudRoot, Control choice, Label label,
        string message)
    {
        Rect2 labelRect = label.GetGlobalRect();
        Rect2 choiceRect = choice.GetGlobalRect();
        // Fixed-font labels may exceed a scaled decorative ColorRect vertically. The
        // visible HUD root is the boundary unless that intermediate control actually clips.
        AssertContained(hudRoot, label, message + " inside the visible HUD region");
        TestAssert.True(labelRect.Position.X >= choiceRect.Position.X && labelRect.End.X <= choiceRect.End.X,
            $"{message} stays in its own horizontal lane; choice={choiceRect} label={labelRect}");
        TestAssert.True(!choice.ClipContents || choiceRect.Encloses(labelRect),
            $"{message} is not clipped by its decorative parent");
    }

    private static async Task AssertActionRouterLayoutAtSupportedScales(SceneTree tree,
        PrologueFormalHud formal)
    {
        var viewport = (SubViewport)formal.GetViewport();
        Vector2I originalResolution = viewport.Size;
        foreach (Vector2I resolution in new[]
            { new Vector2I(1280, 720), new Vector2I(1920, 1080),
              new Vector2I(2560, 1440), new Vector2I(3840, 2160) })
        foreach (float scale in new[] { .8f, 1f, 1.5f })
        {
            viewport.Size = resolution;
            formal.SetUiScale(scale);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal((Vector2)resolution, formal.Size,
                "mounted formal HUD follows the actual test viewport resolution");
            Control mode = formal.GetNode<Control>("ModeSelector");
            Control loadout = formal.GetNode<Control>("Loadout");
            Control resources = formal.GetNode<Control>("ResourceCluster");
            Rect2 safe = new(new Vector2(48, 40), (Vector2)resolution - new Vector2(96, 80));
            TestAssert.True(safe.Encloses(mode.GetGlobalRect()) && safe.Encloses(loadout.GetGlobalRect()),
                $"mounted mode/loadout stay in the HUD safe area at {resolution}, scale {scale}");
            foreach (string modeName in new[] { "Attack", "Grapple" })
            {
                Control choice = mode.GetNode<Control>(modeName);
                Label label = choice.GetNode<Label>("Label");
                TestAssert.True(label.Visible && !string.IsNullOrWhiteSpace(label.Text),
                    $"{modeName} retains its visible mode label at {resolution}, scale {scale}");
                AssertActionLabelFits(mode, choice, label,
                    $"{modeName} label at {resolution}, scale {scale}");
            }
            for (int slot = 1; slot <= 4; slot++)
            {
                Control choice = loadout.GetNode<Control>($"Slot{slot}");
                Label label = choice.GetNode<Label>("Label");
                TestAssert.True(label.Visible && label.Text == slot.ToString(),
                    $"slot {slot} retains its visible numeric label at {resolution}, scale {scale}");
                AssertActionLabelFits(loadout, choice, label,
                    $"slot {slot} label at {resolution}, scale {scale}");
            }
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
        viewport.Size = originalResolution;
        formal.SetUiScale(1f);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
