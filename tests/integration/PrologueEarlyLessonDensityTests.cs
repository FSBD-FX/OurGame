using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class PrologueEarlyLessonDensityTests
{
    // Catches unconditional duplicate-ribbon presentation, not a mocked coverage flag.
    // This is the original packed main's genuine initial lesson: no progress restoration,
    // player relocation, input edge, or private production setter is used.
    public static async Task OrdinaryVisibleAttackGuidanceReplacesDuplicateRibbon(SceneTree tree, string storage)
    {
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        float oldScale = tree.Root.ContentScaleFactor;
        var oldScaleMode = tree.Root.ContentScaleMode;
        var oldScaleAspect = tree.Root.ContentScaleAspect;
        bool oldPaused = tree.Paused;
        var oldMouseMode = Input.MouseMode;
        Vector2 oldPointer = tree.Root.GetMousePosition();
        bool native = DisplayServer.GetName() != "headless";
        var oldWindowMode = native ? DisplayServer.WindowGetMode() : DisplayServer.WindowMode.Windowed;
        var oldVsync = native ? DisplayServer.WindowGetVsyncMode() : DisplayServer.VSyncMode.Disabled;
        Vector2I oldWindowSize = native ? DisplayServer.WindowGetSize() : oldSize;
        Vector2I oldWindowPosition = native ? DisplayServer.WindowGetPosition() : Vector2I.Zero;
        var oldActions = new Dictionary<StringName, float>();
        foreach (StringName action in InputMap.GetActions()) oldActions.Add(action, Input.GetActionStrength(action));
        PrologueLevelController level = null;
        try
        {
            tree.Paused = false;
            foreach (StringName action in oldActions.Keys) Input.ActionRelease(action);
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            tree.Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            tree.Root.ContentScaleFactor = 1f;

            // Runner has already validated fresh off-tree storage and released the retained PID.
            string settingsPath = Path.Combine(storage, "settings.json");
            GameSettingsStore.SaveAtomic(settingsPath, new GameSettingsData
                { Language = GameLanguage.English, GuidanceEnabled = true });
            TestAssert.Equal("res://scenes/prologue/PrologueLevel.tscn",
                ProjectSettings.GetSetting("application/run/main_scene").AsString(),
                "fixture loads the original packed main, not a replacement tutorial scene");
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.SavePath = Path.Combine(storage, "save.json");
            flow.SettingsPath = settingsPath;
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await Settle(tree);
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
                "fresh isolated packed main initializes at the real title before StartNew");
            flow.StartNew();
            await Settle(tree);

            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            Label card = visual.GetNode<Label>("Text");
            Control backdrop = visual.GetNode<Control>("Backdrop");
            Control ribbon = formal.GetNode<Control>("ObjectiveRibbon");
            Label ribbonLabel = ribbon.GetNode<Label>("Text");
            PrologueGuidanceInstruction instruction = guidance.CurrentInstruction;
            const string fullAttack = "Press Q for Attack mode or E for mechanical-claw mode; choose slots 1/2/3/4. LMB performs the current mode and spends energy, which regenerates after you stop.";
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "real StartNew enters gameplay rather than observing a title-only HUD");
            TestAssert.Equal(2, level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").LayoutVersion,
                "ordinary suppression is tested in original layout2 rather than legacy/sample mode");
            TestAssert.True(!completion.BasinQualitySampleMode && !flow.BasinQualitySampleMode,
                "initial attack lesson is not fabricated by the Basin development sample");
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective,
                "genuine original startup still owns the pending attack lesson without forced progress");
            TestAssert.True(guidance.GuidanceEnabled && instruction.Visible &&
                string.IsNullOrEmpty(instruction.ContextTitle),
                "ordinary early instruction is enabled and not the nearby combat override");
            TestAssert.Equal(fullAttack, instruction.StepText,
                "complete approved attack teaching resolves Q E slots1-4 and LMB before the ribbon assertion");
            TestAssert.Equal(fullAttack, visual.RenderedStepText,
                "actual visual renders the same current lesson rather than an obsolete or shortened step");
            TestAssert.True(card.Text.Contains(fullAttack, StringComparison.Ordinal) &&
                !card.Text.Contains('{') && !card.Text.Contains('}'),
                "actual card label preserves the complete instruction and resolved action glyphs");
            TestAssert.True(visual.IsVisibleInTree() && card.IsVisibleInTree() && backdrop.IsVisibleInTree() &&
                visual.HasVisiblePrompt && visual.RenderedTextContained,
                "fully visible real card/backdrop exists; hidden content or failed placement cannot qualify");
            TestAssert.True(visual.PromptRect.Grow(.5f).Encloses(card.GetRect()) &&
                card.GetMinimumSize().Y <= card.Size.Y + .5f &&
                new Rect2(new Vector2(48, 40), level.GetViewportRect().Size - new Vector2(96, 80))
                    .Encloses(visual.PromptRect),
                "actual shaped full card is contained within its safe-area legal backdrop");
            TestAssert.True(!visual.PromptRect.Intersects(ribbon.GetGlobalRect()),
                "preexisting ribbon bounds remain distinct from the legal teaching card");
            TestAssert.Equal("SELECT ATTACK MODE", formal.CurrentModel.Objective,
                "duplicate suppression never blanks or replaces the canonical pending HUD objective");
            TestAssert.Equal("SELECT ATTACK MODE", ribbonLabel.Text,
                "actual ribbon label remains the current complete title even when its parent is suppressed");
            TestAssert.True(!formal.CurrentModel.RecoveryVisible && !formal.CurrentModel.CompletionVisible &&
                !formal.CurrentModel.BossVisible && completion.CompletionCount == 0 &&
                level.Progress.CompletedTutorials.Count == 0 && !level.Progress.BossDefeated,
                "presentation does not consume tutorials or fabricate a modal/completed game");
            AssertActionLabels(formal);
            GD.Print("EARLY_LESSON_DENSITY_PREREQUISITES_OK");

            // Current production must reach this specific behavioral RED after all real prerequisites.
            TestAssert.True(!ribbon.IsVisibleInTree(),
                "ordinary visible attack guidance replaces duplicate objective ribbon");

            await CheckEarlyPresentationLifecycle(tree, level);
            await CheckLayoutAndProtection(tree, level);
            await CheckContextAndMarkerlessFixtures(tree, level);
            await CheckRealModeAdvancement(tree, level);
            await CheckControlledStaleEarlyCard(tree, level);
            await CheckControlledAttachedLesson(tree, level);
            GD.Print("EARLY_LESSON_DENSITY_REGRESSIONS_OK genuine_Q_E=true controlled_fixture_cases=true");
        }
        finally
        {
            try
            {
                tree.Paused = false;
                foreach (StringName action in oldActions.Keys) Input.ActionRelease(action);
                Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
                Input.FlushBufferedEvents();
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    else level.Free();
                }
            }
            finally
            {
                tree.Root.Size = oldSize;
                tree.Root.ContentScaleSize = oldContentSize;
                tree.Root.ContentScaleMode = oldScaleMode;
                tree.Root.ContentScaleAspect = oldScaleAspect;
                tree.Root.ContentScaleFactor = oldScale;
                if (native)
                {
                    DisplayServer.WindowSetMode(oldWindowMode);
                    DisplayServer.WindowSetSize(oldWindowSize);
                    DisplayServer.WindowSetPosition(oldWindowPosition);
                    DisplayServer.WindowSetVsyncMode(oldVsync);
                }
                Input.MouseMode = oldMouseMode;
                var restoredPointer = new InputEventMouseMotion { Position = oldPointer, GlobalPosition = oldPointer };
                tree.Root.PushInput(restoredPointer, true);
                Input.ParseInputEvent(restoredPointer);
                Input.FlushBufferedEvents();
                foreach (var action in oldActions)
                {
                    Input.ActionRelease(action.Key);
                    if (action.Value > 0f) Input.ActionPress(action.Key, action.Value);
                }
                tree.Paused = oldPaused;
            }
            // Not emitted if audio drain/free or state restoration fails; keep all case files.
            GD.Print("EARLY_LESSON_DENSITY_TEARDOWN_OK");
        }
    }

    private static void AssertActionLabels(PrologueFormalHud formal, GameLanguage language = GameLanguage.English)
    {
        foreach (var mode in new[] { ("Attack", "Q ATTACK"), ("Grapple", "E GRAPPLE") })
        {
            Label label = formal.GetNode<Label>("ModeSelector/" + mode.Item1 + "/Label");
            string expected = language == GameLanguage.English ? mode.Item2 :
                mode.Item1 == "Attack" ? "Q 攻击" : "E 机械爪";
            TestAssert.True(label.IsVisibleInTree() && label.Text == expected,
                "actual " + mode.Item1 + " action selection remains visible with its Q/E glyph");
        }
        for (int slot = 1; slot <= 4; slot++)
        {
            Label label = formal.GetNode<Label>($"Loadout/Slot{slot}/Label");
            TestAssert.True(label.IsVisibleInTree() && label.Text == slot.ToString(CultureInfo.InvariantCulture),
                $"actual slot {slot} action label is not hidden with the duplicate objective");
        }
    }

    private static async Task CheckEarlyPresentationLifecycle(SceneTree tree, PrologueLevelController level)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Control ribbon = formal.GetNode<Control>("ObjectiveRibbon");
        Label card = visual.GetNode<Label>("Text");
        string fullText = card.Text;
        float transition = formal.CurrentModel.ObjectiveTransitionSeconds;
        guidance.SetGuidanceEnabled(false);
        TestAssert.True(ribbon.IsVisibleInTree() && !visual.HasVisiblePrompt &&
            !guidance.CurrentInstruction.Visible, "disable immediately reveals pending title and clears actual card");
        TestAssert.True(level.GetNode<Label>("Hud/Objective").Visible,
            "legacy Hud/Objective disable contract remains intact");
        guidance.SetGuidanceEnabled(true);
        await AssertCoveredFrames(tree, level, 3, "re-enable");
        TestAssert.True(formal.CurrentModel.ObjectiveTransitionSeconds >= transition,
            "coverage toggles do not reset the canonical objective transition clock");

        // Controlled view-model fixtures only: do not mislabel these as actual boss/recovery/completion play.
        PrologueHudViewModel ordinary = formal.CurrentModel;
        foreach (var modal in new[] { ordinary.WithRecovery("TEST RECOVERY"),
            ordinary.WithBoss("TEST BOSS", 3, 5, 1, true, false),
            ordinary.WithCompletion("00:00:01", 0, false, false) })
        {
            formal.Present(modal);
            TestAssert.True(ribbon.IsVisibleInTree() && !formal.EarlyLessonRibbonSuppressed,
                "a modal HUD update reveals the latest objective before any guidance refresh");
            guidance.RefreshNow();
            TestAssert.True(ribbon.IsVisibleInTree(), "modal early objective cannot be re-suppressed by refresh");
            formal.Present(ordinary);
            guidance.RefreshNow();
            AssertCovered(level, "controlled modal cleared");
        }
        formal.Present(ordinary.WithObjective("REACH THE CANYON BASIN"));
        TestAssert.True(ribbon.IsVisibleInTree() && ribbon.GetNode<Label>("Text").Text == "REACH THE CANYON BASIN",
            "latest noncovered objective is visible immediately before guidance refresh; old title is not restored");
        guidance.RefreshNow();
        TestAssert.True(ribbon.IsVisibleInTree(), "pending enum does not suppress a different latest canonical model title");
        formal.Present(ordinary);
        guidance.RefreshNow();
        PrologueHudViewModel actionFixture = formal.CurrentModel.WithActionState(PlayerActionMode.Grapple, 3,
            .4f, .6f, true, PlayerActionRejectionReason.EmptySlot);
        formal.Present(actionFixture);
        guidance.SetGuidanceEnabled(false);
        guidance.SetGuidanceEnabled(true);
        TestAssert.True(formal.CurrentModel.ActionMode == PlayerActionMode.Grapple &&
            formal.CurrentModel.ActiveSlot == 3 && formal.CurrentModel.EnergyRatio == .4f &&
            formal.CurrentModel.HeatRatio == .6f && formal.CurrentModel.IsOverheated &&
            formal.CurrentModel.ActionRejection == PlayerActionRejectionReason.EmptySlot,
            "coverage-only HUD presentation preserves mode/slots/resources/rejection model fields");
        TestAssert.True(formal.GetNode<Control>("ModeSelector/Grapple/SelectedMarker").Visible &&
            formal.GetNode<Control>("Loadout/Slot3/SelectedMarker").Visible &&
            formal.GetNode<Control>("ResourceCluster/Heat/OverheatMarker").Visible &&
            Math.Abs(formal.GetNode<ProgressBar>("ResourceCluster/Energy").Value - .4f) < .001 &&
            Math.Abs(formal.GetNode<ProgressBar>("ResourceCluster/Heat").Value - .6f) < .001,
            "coverage-only setter leaves actual action markers and meters intact");
        formal.Present(ordinary);
        guidance.RefreshNow();

        var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        pause.Open();
        try
        {
            TestAssert.True(tree.Paused && ribbon.IsVisibleInTree(),
                "pause Opened reveals ribbon synchronously without waiting for a paused process tick");
            pause.Execute(ProloguePauseCommand.ToggleGuidance);
            pause.Execute(ProloguePauseCommand.ToggleGuidance);
            TestAssert.True(guidance.GuidanceEnabled && ribbon.IsVisibleInTree(),
                "guidance settings toggle while paused cannot rehide the objective");
        }
        finally { pause.Execute(ProloguePauseCommand.Resume); }
        await Settle(tree);
        await AssertCoveredFrames(tree, level, 3, "pause resumed");

        // Actual visibility invalidations between controller refreshes must reveal immediately.
        card.Visible = false;
        TestAssert.True(ribbon.IsVisibleInTree(), "hidden actual label invalidates stale early coverage immediately");
        guidance.RefreshNow();
        AssertCovered(level, "label shown by next valid presentation");
        visual.Visible = false;
        TestAssert.True(ribbon.IsVisibleInTree(), "hidden visual invalidates stale early coverage immediately");
        guidance.RefreshNow();
        AssertCovered(level, "visual shown by next valid presentation");
        visual.Clear();
        TestAssert.True(ribbon.IsVisibleInTree(), "actual visual Clear immediately restores the title");
        guidance.RefreshNow();
        AssertCovered(level, "visual Clear restored");

        visual.RemoveChild(card);
        try
        {
            guidance.RefreshNow();
            TestAssert.True(ribbon.IsVisibleInTree(), "missing actual label never qualifies as full visible teaching");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(ribbon.IsVisibleInTree(), "missing label cannot leave stale suppression across a real frame");
        }
        finally { visual.AddChild(card); }
        guidance.RefreshNow();
        AssertCovered(level, "same retained label remounted");
        Node visualParent = visual.GetParent();
        visualParent.RemoveChild(visual);
        try
        {
            guidance.RefreshNow();
            TestAssert.True(ribbon.IsVisibleInTree(), "missing actual visual safely clears coverage without a disposed reference");
        }
        finally { visualParent.AddChild(visual); }
        guidance.RefreshNow();
        AssertCovered(level, "same retained visual remounted");

        // Controlled protection fixture: a real typed HUD cluster occupies every safe edge.
        // Health has stable bounds across frames, unlike the objective's own transition-height animation.
        Control blocker = formal.GetNode<Control>("Health");
        Vector2 oldBlockerPosition = blocker.Position, oldBlockerSize = blocker.Size;
        try
        {
            blocker.Position = Vector2.Zero;
            blocker.Size = visual.Size;
            guidance.RefreshNow();
            TestAssert.True(!visual.HasVisiblePrompt && !card.Visible && card.Text == fullText &&
                ribbon.IsVisibleInTree(), "failed real placement restores title while preserving complete pending card label");
            for (int frame = 0; frame < 3; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(ribbon.IsVisibleInTree() && !visual.HasVisiblePrompt,
                    "blocked placement never oscillates stale hidden title across consecutive frames");
            }
        }
        finally { blocker.Position = oldBlockerPosition; blocker.Size = oldBlockerSize; }
        guidance.RefreshNow();
        await AssertCoveredFrames(tree, level, 3, "legal placement restored");

        Vector2 oldVisualSize = visual.Size;
        try
        {
            visual.Size = new Vector2(120, 90);
            TestAssert.True(!visual.HasVisiblePrompt && ribbon.IsVisibleInTree(),
                "actual resize-induced failed layout immediately invalidates covered title in the same frame");
        }
        finally { visual.Size = oldVisualSize; }
        guidance.RefreshNow();
        await AssertCoveredFrames(tree, level, 3, "resize returned to legal card");

        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        int oldLayout = world.LayoutVersion;
        try
        {
            world.LayoutVersion = 1;
            guidance.RefreshNow();
            TestAssert.True(ribbon.IsVisibleInTree(), "controlled layout1 eligibility fixture retains its objective presentation");
        }
        finally { world.LayoutVersion = oldLayout; }
        var completion = level.GetNode<PrologueCompletionController>("Completion");
        completion.BasinQualitySampleMode = true;
        try
        {
            guidance.RefreshNow();
            TestAssert.True(ribbon.IsVisibleInTree(), "controlled sample-mode eligibility fixture never receives normal lesson suppression");
        }
        finally { completion.BasinQualitySampleMode = false; }
        guidance.RefreshNow();
        AssertCovered(level, "normal layout/sample eligibility restored");

        // Real recovery lifecycle, requested as a diagnostic; not a walked/death-route witness.
        int recovered = 0;
        void OnRecovered(string _) => recovered++;
        level.RecoveryCompleted += OnRecovered;
        try
        {
            level.RequestRespawn("RETURNED TO CHECKPOINT");
            TestAssert.True(ribbon.IsVisibleInTree() && formal.CurrentModel.RecoveryVisible,
                "actual recovery request reveals title synchronously with real recovery overlay");
            for (int frame = 0; frame < 12 && recovered == 0; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await Settle(tree);
            TestAssert.True(recovered == 1 && !formal.CurrentModel.RecoveryVisible && ribbon.IsVisibleInTree(),
                "completed real recovery retains actual recovery reason rather than stale hidden lesson title");
        }
        finally { level.RecoveryCompleted -= OnRecovered; }
        level.GetNode<PrologueCompletionController>("Completion").RefreshDisplay();
        guidance.RefreshNow();
        await AssertCoveredFrames(tree, level, 3, "canonical pending title displayed after recovery");

        // Owner exit must detach subscriptions; use RequestReady when remounting the same retained owner.
        level.RemoveChild(guidance);
        try
        {
            TestAssert.True(ribbon.IsVisibleInTree(), "guidance owner ExitTree synchronously restores pending objective");
            pause.Open();
            pause.Execute(ProloguePauseCommand.Resume);
        }
        finally { guidance.RequestReady(); level.AddChild(guidance); }
        await AssertCoveredFrames(tree, level, 3, "guidance owner remounted after detached pause lifecycle");
        GD.Print("EARLY_LESSON_DENSITY_LIFECYCLE_OK");
    }

    private static async Task CheckLayoutAndProtection(SceneTree tree, PrologueLevelController level)
    {
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        GameSettingsData oldSettings = flow.Settings.Normalize();
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        float oldFactor = tree.Root.ContentScaleFactor;
        try
        {
            foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            foreach (float scale in new[] { .8f, 1.5f })
            foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
            {
                tree.Root.Size = size;
                tree.Root.ContentScaleSize = size;
                GameSettingsData settings = oldSettings.Normalize();
                settings.WindowMode = GameWindowMode.Windowed;
                settings.ResolutionWidth = size.X;
                settings.ResolutionHeight = size.Y;
                settings.UiScale = scale;
                settings.Language = language;
                // Real case-owned settings path, NOT the independent private FormalHud multiplier.
                flow.ApplySettings(settings);
                await Settle(tree);
                string stage = $"production {size} scale={scale} {language}";
                TestAssert.Near(scale, tree.Root.ContentScaleFactor, .001f,
                    "public production settings apply actual global root factor: " + stage);
                Vector2 effective = level.GetViewportRect().Size;
                TestAssert.Near(size.X / scale, effective.X, 2f,
                    "actual effective viewport width follows production global scale: " + stage);
                TestAssert.Near(size.Y / scale, effective.Y, 2f,
                    "actual effective viewport height follows production global scale: " + stage);
                TestAssert.True(tree.Root.GetVisibleRect().Size.DistanceTo(effective) <= 2f,
                    "packed-main guidance observes the actual globally scaled root viewport: " + stage);
                for (int frame = 0; frame < 4; frame++)
                {
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    AssertActualScalePresentation(level, stage + " frame=" + frame);
                }
            }
        }
        finally
        {
            flow.ApplySettings(oldSettings);
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
            tree.Root.ContentScaleFactor = oldFactor;
        }
        await Settle(tree);
        await AssertCoveredFrames(tree, level, 3, "layout restored");
        Control hiddenPrompt = formal.GetNode<Control>("Prompt");
        bool prior = hiddenPrompt.Visible;
        hiddenPrompt.Visible = false;
        try
        {
            Rect2 hiddenRect = hiddenPrompt.GetGlobalRect();
            var collected = WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formal);
            bool containsHidden = false;
            foreach (Rect2 region in collected) containsHidden |= region == hiddenRect;
            TestAssert.True(!containsHidden, "generic hidden Prompt is not protected merely because one typed ribbon is reserved");
        }
        finally { hiddenPrompt.Visible = prior; }
        GD.Print("EARLY_LESSON_DENSITY_LAYOUT_MATRIX_OK sizes=2 scales=2 languages=2 frames=4 production_global_scale=true correct_fallback_allowed=true");
    }

    private static void AssertActualScalePresentation(PrologueLevelController level, string stage)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        Label card = visual.GetNode<Label>("Text");
        string expected = guidance.Language == GameLanguage.English
            ? "Press Q for Attack mode or E for mechanical-claw mode; choose slots 1/2/3/4. LMB performs the current mode and spends energy, which regenerates after you stop."
            : "按 Q 选择攻击模式，按 E 选择机械爪模式；用 1/2/3/4 选择装备，LMB 执行当前模式并会消耗能量，停手后恢复。";
        TestAssert.True(guidance.CurrentInstruction.StepText == expected && visual.RenderedStepText == expected &&
            card.Text.Contains(expected, StringComparison.Ordinal),
            "production scale preserves complete approved pending copy even in no-placement fallback: " + stage);
        if (visual.HasVisiblePrompt)
        {
            // A visible clipped/misplaced card may not be reclassified as an acceptable fallback.
            AssertCovered(level, stage);
            return;
        }
        TestAssert.True(!card.Visible && !visual.GetNode<Control>("Backdrop").Visible &&
            !visual.RenderedTextContained && formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
            !formal.EarlyLessonRibbonSuppressed && formal.CurrentModel.Objective == "SELECT ATTACK MODE" &&
            formal.GetNode<Label>("ObjectiveRibbon/Text").Text ==
                (guidance.Language == GameLanguage.English ? "SELECT ATTACK MODE" : "选择攻击模式"),
            "actual no-legal-placement hides content and reveals latest complete title, never false coverage: " + stage);
        AssertActionLabels(formal, guidance.Language);
    }

    private static async Task CheckContextAndMarkerlessFixtures(SceneTree tree, PrologueLevelController level)
    {
        CharacterBody2D player = level.Player;
        Vector2 spawn = player.GlobalPosition;
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        Node actors = level.CurrentSectorRoot.GetNode("Gameplay/Actors");
        var encounter = level.CurrentSectorRoot.GetNode<CanyonEncounterGroup>(
            "Gameplay/Encounters/LowCeilingBridgeEncounter");
        TestAssert.True(actors.GetNode("RustShellSquire") is Marker2D &&
            !encounter.IsActive && encounter.LiveActorCount == 0 &&
            FindDescendant<RustShellSquire>(actors) == null,
            "original layout2 spawn has only the inert guidance marker before authored encounter activation");
        try
        {
            // Controlled encounter arrangement, not a walked-route witness or lesson advancement.
            // The real layout2 actor is spawned by the encounter, not the static marker path.
            player.GlobalPosition = new Vector2(500, 520);
            player.Velocity = Vector2.Zero;
            TestAssert.True(encounter.Definition.ActivationBounds.HasPoint(player.GlobalPosition) &&
                encounter.Definition.ArenaBounds.HasPoint(player.GlobalPosition),
                "controlled first-combat setup lies inside authored activation and retention bounds");
            RustShellSquire enemy = null;
            for (int frame = 0; frame < 12 && !GodotObject.IsInstanceValid(enemy); frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                enemy = FindDescendant<RustShellSquire>(actors);
            }
            TestAssert.True(GodotObject.IsInstanceValid(enemy) && encounter.IsActive &&
                encounter.LiveActorCount == 1 && !encounter.IsComplete && enemy.IsInsideTree() &&
                enemy.GetNode<HealthComponent>(enemy.HealthPath).CurrentHealth > 0,
                "authored encounter actually mounts one living real RustShell before contextual assertions");
            enemy.GlobalPosition = player.GlobalPosition + new Vector2(120, 0);
            enemy.Velocity = Vector2.Zero;
            await Settle(tree);
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonEntrance &&
                player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode == PlayerActionMode.Attack &&
                player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State == MechanicalClawState.Idle &&
                level.GetNode<PrologueCompletionController>("Completion").CurrentObjective == PrologueObjective.LearnAttackMode &&
                enemy.IsVisibleInTree() && enemy.HasLineOfSightTo(player) &&
                Math.Abs(enemy.GlobalPosition.X - player.GlobalPosition.X) <= 340f &&
                Math.Abs(enemy.GlobalPosition.Y - player.GlobalPosition.Y) <= 100f &&
                level.GetViewportRect().HasPoint(enemy.GetGlobalTransformWithCanvas().Origin) &&
                !string.IsNullOrEmpty(guidance.CurrentInstruction.ContextTitle),
                "controlled real nearby encounter activates actual ContextTitle without consuming attack objective");
            TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
                formal.GetNode<Label>("ObjectiveRibbon/Text").Text == guidance.CurrentInstruction.ContextTitle &&
                guidance.CurrentInstruction.StepText.Contains("RMB") && guidance.CurrentInstruction.StepText.Contains("LMB"),
                "live contextual combat advice and its actual current ribbon are never suppressed");
            var contextualVisual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(contextualVisual.HasVisiblePrompt && contextualVisual.RenderedTextContained &&
                contextualVisual.GetNode<Label>("Text").IsVisibleInTree() &&
                contextualVisual.GetNode<Label>("Text").Text.Contains(guidance.CurrentInstruction.StepText,
                    StringComparison.Ordinal),
                "actual contextual full-card advice remains visible alongside its preserved current ribbon");
        }
        finally
        {
            player.GlobalPosition = spawn;
            player.Velocity = Vector2.Zero;
        }
        await Settle(tree);
        TestAssert.True(!encounter.IsActive && encounter.LiveActorCount == 0 &&
            FindDescendant<RustShellSquire>(actors) == null,
            "returning to original spawn retires the generated encounter actor before later genuine Q/E input");
        await AssertCoveredFrames(tree, level, 3, "context exited to pending attack title");

        // Controlled layout2 support arrangement, not a route or genuine movement witness.
        var spawnSupport = CaptureStartFloor(level);
        var tutorials = new HashSet<StringName>(level.Progress.CompletedTutorials);
        Exception markerlessFailure = null;
        void RecoveryRequested(string reason) => MarkerlessDiagnostic("recovery_requested", new { reason });
        void RecoveryCompleted(string reason) => MarkerlessDiagnostic("recovery_completed", new { reason });
        level.RecoveryRequested += RecoveryRequested;
        level.RecoveryCompleted += RecoveryCompleted;
        try
        {
            try
            {
                MarkerlessSnapshot(tree, level, "before_teleport");
                SpatialArrangement arrangement = ReadSpatialArrangement(level);
                player.GlobalPosition = arrangement.Placement;
                player.Velocity = Vector2.Zero;
                await WaitForSettledSupport(tree, level, PrologueSectorId.CanyonDescent,
                    arrangement.Support, arrangement.Placement, "markerless_setup");
                AssertSpatialLesson(level, PrologueObjective.LearnAttackMode, PlayerActionMode.Attack,
                    tutorials, "markerless setup retains genuine pending attack");
                var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
                MarkerlessSnapshot(tree, level, "before_original_conjunction");
                TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonDescent && player.IsOnFloor() &&
                    visual.RenderedTargetCount == 0,
                    "controlled supported descent fixture has no off-sector attack marker and remains genuine pending attack");
                await AssertCoveredFrames(tree, level, 4, "markerless pending attack on supported catch shelf");
                Rect2 central = new(level.GetViewportRect().Size * new Vector2(.22f, .18f),
                    level.GetViewportRect().Size * new Vector2(.56f, .64f));
                TestAssert.True(!visual.PromptRect.Intersects(central.Grow(8f)),
                    "actual markerless early card leaves central action/gameplay band unobscured");
            }
            catch (Exception exception)
            {
                markerlessFailure = exception;
                MarkerlessSnapshot(tree, level, "markerless_body_failure");
                throw;
            }
            finally
            {
                await RestoreSpatialSpawn(tree, level, spawn, spawnSupport,
                    PrologueObjective.LearnAttackMode, PlayerActionMode.Attack, tutorials,
                    "markerless_cleanup", markerlessFailure);
            }
            await AssertCoveredFrames(tree, level, 3, "original spawn restored before genuine input");
            GD.Print("EARLY_LESSON_DENSITY_CONTEXT_MARKERLESS_OK controlled_fixture=true");
        }
        finally
        {
            level.RecoveryRequested -= RecoveryRequested;
            level.RecoveryCompleted -= RecoveryCompleted;
        }
    }

    // Test-local snapshots do not claim all callbacks completed. The separately labeled
    // support witness is sampled only after physics has crossed into a process signal.
    private static void MarkerlessDiagnostic(string phase, object values)
    {
        GD.Print("TASK22_LAYOUT2_FIX4 " + JsonSerializer.Serialize(new
        {
            phase, physics = Engine.GetPhysicsFrames(), process = Engine.GetProcessFrames(),
            timing = "observation_only_not_callback_completion", values
        }));
    }

    private static object MarkerlessNode(Node node) => !GodotObject.IsInstanceValid(node)
        ? new { available = false } : (object)new
        {
            available = true, id = node.GetInstanceId(), path = node.GetPath().ToString(),
            type = node.GetType().Name, scene = node.SceneFilePath,
            processMode = node.ProcessMode.ToString(), canProcess = node.CanProcess(),
            physicsProcessing = node.IsPhysicsProcessing(), processing = node.IsProcessing(),
            transform = (node as Node2D)?.GlobalTransform.ToString(),
            layout = node.HasMeta("canyon_layout_version")
                ? node.GetMeta("canyon_layout_version").ToString() : "unavailable"
        };

    private static void MarkerlessSnapshot(SceneTree tree, PrologueLevelController level, string phase)
    {
        try
        {
            var player = level.Player;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var input = player.GetNode<PlayerInputReader>("Input");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            var card = visual.GetNode<Label>("Text");
            var ribbon = formal.GetNode<Control>("ObjectiveRibbon");
            var viewport = level.GetViewport();
            var camera = viewport.GetCamera2D();
            var contacts = new List<object>();
            for (int index = 0; index < player.GetSlideCollisionCount(); index++)
            {
                var collision = player.GetSlideCollision(index);
                var collider = collision.GetCollider();
                contacts.Add(new { index, id = collision.GetColliderId(),
                    type = collider?.GetType().Name, node = MarkerlessNode(collider as Node),
                    position = collision.GetPosition().ToString(), normal = collision.GetNormal().ToString() });
            }
            // Evaluate each operand independently; leave the original conjunction untouched.
            bool sectorOperand = level.CurrentSector == PrologueSectorId.CanyonDescent;
            bool floorOperand = player.IsOnFloor();
            int targetCount = visual.RenderedTargetCount;
            MarkerlessDiagnostic(phase, new
            {
                operands = new { sectorOperand, floorOperand, zeroTargetOperand = targetCount == 0, targetCount },
                sector = level.CurrentSector?.ToString(), root = MarkerlessNode(level.CurrentSectorRoot),
                world = new { node = MarkerlessNode(world), world.LayoutVersion,
                    mounted = string.Join(",", world.MountedSectorIds) },
                player = new { node = MarkerlessNode(player), position = player.GlobalPosition.ToString(),
                    velocity = player.Velocity.ToString(), floor = floorOperand, contacts,
                    motor = MarkerlessNode(player.GetNodeOrNull("Motor")) },
                tree.Paused, flow = level.GetNode<PrologueGameFlowController>("GameFlow").State.ToString(),
                lesson = new { objective = completion.CurrentObjective.ToString(), completion.CompletionCount,
                    tutorials = string.Join(",", level.Progress.CompletedTutorials),
                    tutorialCount = level.Progress.CompletedTutorials.Count,
                    mode = player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode.ToString(),
                    claw = claw.CurrentFrame.ToString(), target = MarkerlessNode(claw.CurrentTarget),
                    sampledInput = input.CurrentFrame.ToString(), input.CurrentFrameSequence },
                instruction = new { guidance.GuidanceEnabled, guidance.CurrentInstruction.Visible,
                    guidance.CurrentInstruction.StepText, guidance.CurrentInstruction.ContextTitle,
                    targets = string.Join(",", guidance.CurrentInstruction.Targets) },
                visual = new { visible = visual.IsVisibleInTree(), visual.HasVisiblePrompt,
                    visual.RenderedStepText, visual.RenderedTextContained,
                    bounds = visual.RenderedTextBounds.ToString(), prompt = visual.PromptRect.ToString(),
                    cardText = card.Text, cardVisible = card.IsVisibleInTree(), cardRect = card.GetRect().ToString(),
                    backdropVisible = visual.GetNode<Control>("Backdrop").IsVisibleInTree(),
                    canvasTransform = visual.GetGlobalTransformWithCanvas().ToString() },
                ribbon = new { canonical = formal.CurrentModel.Objective, formal.EarlyLessonRibbonSuppressed,
                    visible = ribbon.IsVisibleInTree(), title = ribbon.GetNode<Label>("Text").Text,
                    rect = ribbon.GetGlobalRect().ToString() },
                view = new { viewport = viewport.GetVisibleRect().ToString(),
                    canvas = viewport.CanvasTransform.ToString(), final = viewport.GetFinalTransform().ToString(),
                    playerCanvas = player.GetGlobalTransformWithCanvas().ToString(), camera = MarkerlessNode(camera),
                    cameraCenter = camera?.GetScreenCenterPosition().ToString(), zoom = camera?.Zoom.ToString(),
                    size = tree.Root.Size.ToString(), contentSize = tree.Root.ContentScaleSize.ToString(),
                    tree.Root.ContentScaleFactor, scaleMode = tree.Root.ContentScaleMode.ToString(),
                    scaleAspect = tree.Root.ContentScaleAspect.ToString(), language = guidance.Language.ToString(),
                    centralProtectedRect = new Rect2(level.GetViewportRect().Size * new Vector2(.22f, .18f),
                        level.GetViewportRect().Size * new Vector2(.56f, .64f)).ToString(),
                    protectedRegions = "unavailable_via_public_API; resolved_collection_is_internal",
                    privateMotorState = "unavailable_via_public_API" }
            });
        }
        catch (Exception exception)
        {
            // An ancillary observation must never prevent the original assertion or cleanup.
            MarkerlessDiagnostic(phase, new { unavailable = exception.GetType().Name });
        }
    }

    private readonly record struct SpatialArrangement(StaticBody2D Support, GrappleSurface2D Target,
        Rect2 SupportBounds, Rect2 TargetBounds, Vector2 Placement, Vector2 Aim);

    private static PrologueSector RequireLayout2Sector(PrologueLevelController level, PrologueSectorId id)
    {
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TestAssert.True(world.LayoutVersion == 2 && world.TryGetMountedSector(id, out _),
            "spatial fixture requires its actual mounted layout2 sector: " + id);
        world.TryGetMountedSector(id, out var sector);
        TestAssert.True(sector.SectorId == id && sector.HasMeta("canyon_layout_version") &&
            sector.GetMeta("canyon_layout_version").AsInt32() == 2 && sector.IsInsideTree(),
            "mounted spatial root has the expected live sector identity and layout metadata: " + id);
        return sector;
    }

    private static Rect2 ReadSupportRectangle(CharacterBody2D player, StaticBody2D body)
    {
        var shape = body.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(body.IsInsideTree() && body.CanProcess() && !shape.Disabled &&
            !shape.OneWayCollision && (body.CollisionLayer & CombatCollisionLayers.World) != 0 &&
            (body.CollisionLayer & player.CollisionMask) != 0 && shape.Shape is RectangleShape2D,
            "named support/target has enabled solid world collision against the player: " + body.GetPath());
        var rectangle = (RectangleShape2D)shape.Shape;
        Transform2D transform = shape.GlobalTransform;
        TestAssert.True(rectangle.Size.X > 0 && rectangle.Size.Y > 0 &&
            transform.X.X > 0 && transform.Y.Y > 0 &&
            Mathf.Abs(transform.X.Y) < .001f && Mathf.Abs(transform.Y.X) < .001f,
            "spatial fixture requires positive upright axis-aligned live rectangle geometry");
        Vector2 start = shape.ToGlobal(-rectangle.Size / 2f);
        Vector2 end = shape.ToGlobal(rectangle.Size / 2f);
        return new Rect2(start, end - start);
    }

    private static SpatialArrangement ReadSpatialArrangement(PrologueLevelController level)
    {
        var descent = RequireLayout2Sector(level, PrologueSectorId.CanyonDescent);
        var support = descent.GetNode<StaticBody2D>("Gameplay/Collision/ArchiveEntry");
        var ceiling = descent.GetNode<StaticBody2D>("Gameplay/Collision/RiftLowerCatch");
        Rect2 supportBounds = ReadSupportRectangle(level.Player, support);
        Rect2 ceilingBounds = ReadSupportRectangle(level.Player, ceiling);
        var target = ceiling.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        var supportProfile = support.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        TestAssert.True(target.IsAvailable && target.IsVisibleInTree() && target.UseCollisionHitPoint &&
            target.Material == GrappleSurfaceMaterial.GripStone && target.Validate().Count == 0 &&
            supportProfile.IsAvailable && supportProfile.Material == GrappleSurfaceMaterial.GripStone &&
            (ceiling.CollisionLayer & CombatCollisionLayers.ThreadTarget) != 0,
            "layout2 candidate is a live rough ceiling over an available rough support, not a synthetic anchor");
        var bodyShape = level.Player.GetNode<CollisionShape2D>("BodyCollider");
        TestAssert.True(!bodyShape.Disabled && bodyShape.Shape is CapsuleShape2D,
            "derived placement uses the real enabled player capsule");
        var capsule = (CapsuleShape2D)bodyShape.Shape;
        Transform2D bodyTransform = bodyShape.GlobalTransform;
        TestAssert.True(capsule.Radius > 0 && capsule.Height >= 2f * capsule.Radius &&
            bodyTransform.X.X > 0 && bodyTransform.Y.Y > 0 &&
            Mathf.Abs(bodyTransform.X.Y) < .001f && Mathf.Abs(bodyTransform.Y.X) < .001f,
            "derived player clearance requires a positive upright capsule");
        Vector2 offset = bodyShape.GlobalPosition - level.Player.GlobalPosition;
        float radius = capsule.Radius * bodyTransform.X.X;
        float halfHeight = capsule.Height * bodyTransform.Y.Y / 2f;
        float clearance = Mathf.Max(1f, level.Player.SafeMargin * 2f);
        float left = Mathf.Max(supportBounds.Position.X, ceilingBounds.Position.X) + radius + clearance;
        float right = Mathf.Min(supportBounds.End.X, ceilingBounds.End.X) - radius - clearance;
        TestAssert.True(right > left && supportBounds.Position.Y - ceilingBounds.End.Y >
            2f * halfHeight + 2f * clearance,
            "candidate overlap has whole-body horizontal and vertical clearance");
        float centerX = (left + right) / 2f;
        Vector2 placement = new(centerX - offset.X,
            supportBounds.Position.Y - offset.Y - halfHeight - clearance);
        Vector2 aim = new(centerX, ceilingBounds.GetCenter().Y);
        TestAssert.True(placement.DistanceTo(aim) < MechanicalClawSettings.Default.MaxRange &&
            placement.Y - ceilingBounds.End.Y > MechanicalClawSettings.Default.MinimumRopeLength,
            "candidate gives a separated upward cast within real claw range");
        MarkerlessDiagnostic("derived_layout2_arrangement", new { root = MarkerlessNode(descent),
            support = MarkerlessNode(support), target = MarkerlessNode(target),
            supportBounds = supportBounds.ToString(), targetBounds = ceilingBounds.ToString(),
            radius, halfHeight, clearance, placement = placement.ToString(), aim = aim.ToString() });
        return new SpatialArrangement(support, target, supportBounds, ceilingBounds, placement, aim);
    }

    private static StaticBody2D CaptureStartFloor(PrologueLevelController level)
    {
        var entrance = RequireLayout2Sector(level, PrologueSectorId.CanyonEntrance);
        var support = entrance.GetNode<StaticBody2D>("Gameplay/Collision/StartFloor");
        ReadSupportRectangle(level.Player, support);
        TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonEntrance &&
            level.Player.IsOnFloor() && HasExactFloorContact(level.Player, support),
            "captured original spawn has actual upward StartFloor contact before spatial arrangement");
        return support;
    }

    private static bool HasExactFloorContact(CharacterBody2D player, StaticBody2D support)
    {
        if (!GodotObject.IsInstanceValid(support)) return false;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            var contact = player.GetSlideCollision(index);
            if (contact.GetColliderId() == support.GetInstanceId() &&
                contact.GetNormal().Dot(Vector2.Up) >= .99f) return true;
        }
        return false;
    }

    private static bool AtSettledSupport(PrologueLevelController level, PrologueSectorId sector,
        StaticBody2D support, Vector2 expected) => level.CurrentSector == sector &&
        level.Player.IsOnFloor() && HasExactFloorContact(level.Player, support) &&
        level.Player.Velocity.Length() <= 1f &&
        Mathf.Abs(level.Player.GlobalPosition.X - expected.X) <= .5f &&
        Mathf.Abs(level.Player.GlobalPosition.Y - expected.Y) <= 2f;

    private static async Task WaitForSettledSupport(SceneTree tree, PrologueLevelController level,
        PrologueSectorId sector, StaticBody2D support, Vector2 expected, string stage)
    {
        const int maximumObservations = 90, requiredStableObservations = 3;
        int stable = 0;
        ulong previousPhysics = Engine.GetPhysicsFrames();
        Vector2 previousPosition = level.Player.GlobalPosition;
        try
        {
            for (int observation = 0; observation < maximumObservations; observation++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                // PhysicsFrame fires BEFORE node physics callbacks. ProcessFrame is the
                // following phase: the sampled movement/contact belongs to completed physics.
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                ulong physics = Engine.GetPhysicsFrames();
                bool settled = !Engine.IsInPhysicsFrame() && physics > previousPhysics &&
                    AtSettledSupport(level, sector, support, expected) &&
                    level.Player.GlobalPosition.DistanceTo(previousPosition) <= .25f;
                stable = settled ? stable + 1 : 0;
                previousPhysics = physics;
                previousPosition = level.Player.GlobalPosition;
                if (stable < requiredStableObservations) continue;
                ulong process = Engine.GetProcessFrames();
                // The preceding normal _Process presentation completes before this next signal.
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(Engine.GetProcessFrames() > process &&
                    AtSettledSupport(level, sector, support, expected) &&
                    level.Player.GlobalPosition.DistanceTo(previousPosition) <= .25f,
                    "fresh normal presentation retains the exact settled support: " + stage);
                MarkerlessDiagnostic(stage + "_settled_support", new { observation = observation + 1,
                    stable, expected = expected.ToString(), support = MarkerlessNode(support),
                    physicsCompletedBeforeProcessSignal = true, freshNormalProcess = true });
                MarkerlessSnapshot(tree, level, stage + "_settled_snapshot");
                return;
            }
            throw new InvalidOperationException("Support readiness bound exhausted: " + stage +
                "; required exact sector/support/upward contact, three stable completed physics observations" +
                "; limit=" + maximumObservations + "; expected=" + expected);
        }
        catch
        {
            MarkerlessDiagnostic(stage + "_support_bound_failure", new { stable, maximumObservations,
                expected = expected.ToString(), requiredSector = sector.ToString(), support = MarkerlessNode(support) });
            MarkerlessSnapshot(tree, level, stage + "_support_failure_snapshot");
            throw;
        }
    }

    private static void AssertSpatialLesson(PrologueLevelController level, PrologueObjective objective,
        PlayerActionMode mode, HashSet<StringName> tutorials, string stage)
    {
        var player = level.Player;
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerInputFrame input = player.GetNode<PlayerInputReader>("Input").CurrentFrame;
        bool quiet = Mathf.IsZeroApprox(input.MoveAxis) && input.Aim.IsZeroApprox() &&
            !input.JumpPressed && !input.JumpHeld && !input.DashPressed && !input.AttackPressed &&
            !input.ThreadPressed && !input.ThreadHeld && !input.ThreadReleased &&
            !input.AttackModePressed && !input.GrappleModePressed && input.RequestedSlot == 0 &&
            !input.PrimaryPressed && !input.PrimaryHeld && !input.PrimaryReleased && !input.ShieldHeld;
        foreach (StringName action in InputMap.GetActions()) quiet &= !Input.IsActionPressed(action);
        TestAssert.True(level.GetNode<PrologueCompletionController>("Completion").CurrentObjective == objective &&
            player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode == mode &&
            claw.CurrentFrame.State == MechanicalClawState.Idle && claw.CurrentTarget == null && quiet &&
            tutorials.SetEquals(level.Progress.CompletedTutorials),
            "spatial fixture preserves expected objective/mode, idle claw, quiet input and tutorials: " + stage);
    }

    private static async Task RestoreSpatialSpawn(SceneTree tree, PrologueLevelController level,
        Vector2 spawn, StaticBody2D support, PrologueObjective objective, PlayerActionMode mode,
        HashSet<StringName> tutorials, string stage, Exception originalFailure)
    {
        try
        {
            level.Player.GlobalPosition = spawn;
            level.Player.Velocity = Vector2.Zero;
            await WaitForSettledSupport(tree, level, PrologueSectorId.CanyonEntrance, support, spawn, stage);
            AssertSpatialLesson(level, objective, mode, tutorials, stage);
        }
        catch (Exception cleanupFailure)
        {
            MarkerlessSnapshot(tree, level, stage + "_failure");
            if (originalFailure != null)
                throw new AggregateException("Spatial case and its bounded cleanup both failed",
                    originalFailure, cleanupFailure);
            throw;
        }
    }

    private static async Task CheckRealModeAdvancement(SceneTree tree, PrologueLevelController level)
    {
        var completion = level.GetNode<PrologueCompletionController>("Completion");
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective,
            "all controlled UI arrangements left the original attack lesson pending before genuine Q input");
        await Pulse(tree, InputActions.SelectAttackMode);
        TestAssert.Equal(PrologueObjective.LearnGrappleMode, completion.CurrentObjective,
            "genuine Q input advances the original attack objective once, without injected progress");
        TestAssert.True(string.IsNullOrEmpty(guidance.CurrentInstruction.ContextTitle) &&
            guidance.CurrentInstruction.StepText.Contains("E") && guidance.CurrentInstruction.StepText.Contains("LMB") &&
            !guidance.CurrentInstruction.StepText.Contains("SPACE", StringComparison.OrdinalIgnoreCase),
            "real Q yields ordinary unattached E lesson away from contextual first-combat or attached advice");
        TestAssert.Equal("SELECT GRAPPLE MODE", formal.CurrentModel.Objective,
            "genuine Q updates canonical model even while ordinary grapple lesson covers its title");
        await AssertCoveredFrames(tree, level, 4, "genuine Q ordinary E lesson");
        await Pulse(tree, InputActions.SelectGrappleMode);
        TestAssert.Equal(PrologueObjective.ReachBasin, completion.CurrentObjective,
            "genuine E advances the ordinary grapple objective once without forced completion");
        TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
            formal.GetNode<Label>("ObjectiveRibbon/Text").Text == "REACH THE CANYON BASIN" &&
            formal.CurrentModel.Objective == "REACH THE CANYON BASIN" && !formal.EarlyLessonRibbonSuppressed,
            "real E immediately restores latest ReachBasin actual title, not cached attack/grapple title");
        AssertActionLabels(formal, guidance.Language);
        GD.Print("EARLY_LESSON_DENSITY_GENUINE_Q_E_OK objective=ReachBasin");
    }

    private static async Task CheckControlledStaleEarlyCard(SceneTree tree, PrologueLevelController level)
    {
        // Only AFTER genuine Q/E proof: public session restores arrange a synchronous owner seam.
        // Catches binding live E objective to the still-rendered Q card during resize notification.
        var completion = level.GetNode<PrologueCompletionController>("Completion");
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        Vector2 originalSize = visual.Size;
        try
        {
            completion.RestoreSession(PrologueObjective.LearnAttackMode);
            guidance.RefreshNow();
            AssertCovered(level, "controlled stale-owner setup: actual Q card");
            string oldStep = guidance.CurrentInstruction.StepText;
            completion.RestoreSession(PrologueObjective.LearnGrappleMode);
            TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
                formal.GetNode<Label>("ObjectiveRibbon/Text").Text == "SELECT GRAPPLE MODE" &&
                guidance.CurrentInstruction.StepText == oldStep && visual.RenderedStepText == oldStep,
                "new canonical E title is visible while old Q card remains before any guidance refresh");
            visual.Size = originalSize + new Vector2(0, 1);
            TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
                !formal.EarlyLessonRibbonSuppressed,
                "actual layout invalidation cannot cover new E objective with stale ordinary Q card");
            visual.Size = originalSize;
            guidance.RefreshNow();
            TestAssert.True(guidance.CurrentInstruction.StepText != oldStep &&
                visual.RenderedStepText == guidance.CurrentInstruction.StepText,
                "suppression resumes only after fresh evaluated E instruction is actually presented");
            AssertCovered(level, "controlled matching E card replaces stale Q card");
        }
        finally
        {
            visual.Size = originalSize;
            completion.RestoreSession(PrologueObjective.ReachBasin);
            guidance.RefreshNow();
        }
        await Settle(tree);
        GD.Print("EARLY_LESSON_DENSITY_STALE_OBJECTIVE_OK controlled_saved_lesson=true after_genuine_Q_E=true");
    }

    private static async Task CheckControlledAttachedLesson(SceneTree tree, PrologueLevelController level)
    {
        // Explicit diagnostic setup AFTER genuine Q/E proof: public saved-session restore gives
        // LearnGrappleMode while retaining genuine E-selected mode; actual LMB owns claw attachment.
        // This is not presented as original lesson progression or a route completion witness.
        var completion = level.GetNode<PrologueCompletionController>("Completion");
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        CharacterBody2D player = level.Player;
        Vector2 spawn = player.GlobalPosition;
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var spawnSupport = CaptureStartFloor(level);
        var tutorials = new HashSet<StringName>(level.Progress.CompletedTutorials);
        AssertSpatialLesson(level, PrologueObjective.ReachBasin, PlayerActionMode.Grapple,
            tutorials, "attachment starts only after genuine Q/E and stale-owner cleanup");
        Exception attachmentFailure = null;
        try
        {
            completion.RestoreSession(PrologueObjective.LearnGrappleMode);
            SpatialArrangement arrangement = ReadSpatialArrangement(level);
            player.GlobalPosition = arrangement.Placement;
            player.Velocity = Vector2.Zero;
            await WaitForSettledSupport(tree, level, PrologueSectorId.CanyonDescent,
                arrangement.Support, arrangement.Placement, "attachment_setup");
            AssertSpatialLesson(level, PrologueObjective.LearnGrappleMode, PlayerActionMode.Grapple,
                tutorials, "restored E lesson retains actual E-selected mode before real LMB");
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonDescent && player.IsOnFloor() &&
                completion.CurrentObjective == PrologueObjective.LearnGrappleMode,
                "controlled restored E lesson stays pending on the genuine supported shelf");
            var target = arrangement.Target;
            MechanicalClawTargetResolution hit = new MechanicalClawTargetResolver().Resolve(player, arrangement.Aim);
            MarkerlessDiagnostic("attachment_actual_resolver_hit", new { expected = MarkerlessNode(target),
                actual = MarkerlessNode(hit.Surface), snapshot = hit.Snapshot.ToString(),
                aim = arrangement.Aim.ToString(), distance = player.GlobalPosition.DistanceTo(hit.Snapshot.WorldAnchor) });
            TestAssert.True(hit.Surface == target && hit.Snapshot.Exists && hit.Snapshot.HasLineOfSight,
                "controlled attachment diagnostic resolves actual available rough ceiling collision hit");
            float separation = player.GlobalPosition.DistanceTo(hit.Snapshot.WorldAnchor);
            TestAssert.True(target.IsAvailable && target.IsVisibleInTree() &&
                hit.Snapshot.RuntimeId == target.GetInstanceId() &&
                hit.Snapshot.Material == GrappleSurfaceMaterial.GripStone && !hit.Snapshot.IsLegacyInteraction &&
                separation > MechanicalClawSettings.Default.MinimumRopeLength &&
                separation <= MechanicalClawSettings.Default.MaxRange &&
                hit.Snapshot.SurfaceNormal.Dot(Vector2.Down) >= .99f &&
                Mathf.Abs(hit.Snapshot.WorldAnchor.Y - arrangement.TargetBounds.End.Y) <= .5f &&
                hit.Snapshot.WorldAnchor.X > arrangement.TargetBounds.Position.X &&
                hit.Snapshot.WorldAnchor.X < arrangement.TargetBounds.End.X &&
                level.GetViewportRect().HasPoint(player.GetGlobalTransformWithCanvas() *
                    player.ToLocal(hit.Snapshot.WorldAnchor)),
                "real resolver first collision is the selected rough underside, separated, visible and in range");
            // Keep the real viewport conversion; aim inside the selected rectangle, not
            // exactly on its boundary where a subsequent ray endpoint could stop in air.
            Vector2 pointer = player.GetViewport().GetFinalTransform() *
                (player.GetGlobalTransformWithCanvas() * player.ToLocal(arrangement.Aim));
            var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
            player.GetViewport().PushInput(motion, true);
            Input.ParseInputEvent(motion);
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true,
                Position = pointer, GlobalPosition = pointer, ButtonMask = MouseButtonMask.Left });
            Input.FlushBufferedEvents();
            for (int frame = 0; frame < 120 && !Attached(claw.CurrentFrame.State); frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            // Let the preceding ordinary process presentation complete before inspecting its card.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            MarkerlessSnapshot(tree, level, "attachment_actual_lmb_snapshot");
            TestAssert.True(Attached(claw.CurrentFrame.State) && claw.CurrentTarget == target &&
                claw.CurrentFrame.LockedTargetRuntimeId == target.GetInstanceId(),
                "actual LMB latches real collision-hit target rather than injecting a claw-state enum");
            TestAssert.True(completion.CurrentObjective == PrologueObjective.LearnGrappleMode &&
                string.IsNullOrEmpty(guidance.CurrentInstruction.ContextTitle) &&
                guidance.CurrentInstruction.StepText.Contains("SPACE", StringComparison.OrdinalIgnoreCase) &&
                guidance.CurrentInstruction.StepText.Contains("A/D") &&
                formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() && !formal.EarlyLessonRibbonSuppressed,
                "empty-context early-objective attached-claw override retains live attachment advice and objective title");
            var attachedVisual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(attachedVisual.HasVisiblePrompt && attachedVisual.RenderedTextContained &&
                attachedVisual.GetNode<Label>("Text").IsVisibleInTree() &&
                attachedVisual.RenderedStepText == guidance.CurrentInstruction.StepText &&
                attachedVisual.GetNode<Label>("Text").Text.Contains(guidance.CurrentInstruction.StepText,
                    StringComparison.Ordinal),
                "actual complete attached-claw card remains visible; only redundant ordinary mode teaching qualifies");
            // Synchronous diagnostic detachment, not proof of a natural release/landing.
            // Catches accepting stale attached advice merely because live claw has become Idle.
            string attachedStep = guidance.CurrentInstruction.StepText;
            Vector2 oldVisualSize = attachedVisual.Size;
            claw.ResetTransientState();
            TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Idle &&
                guidance.CurrentInstruction.StepText == attachedStep && attachedVisual.RenderedStepText == attachedStep,
                "public transient reset leaves actual attached card stale until a fresh guidance presentation");
            try
            {
                attachedVisual.Size = oldVisualSize + new Vector2(0, 1);
                TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
                    !formal.EarlyLessonRibbonSuppressed,
                    "actual invalidation after detachment cannot reinterpret stale attached card as ordinary E coverage");
            }
            finally { attachedVisual.Size = oldVisualSize; }
            guidance.RefreshNow();
            TestAssert.True(guidance.CurrentInstruction.StepText != attachedStep &&
                !guidance.CurrentInstruction.StepText.Contains("SPACE", StringComparison.OrdinalIgnoreCase),
                "fresh detached presentation restores ordinary E instruction before eligible coverage resumes");
            AssertCovered(level, "controlled detached matching ordinary E card");
        }
        catch (Exception exception)
        {
            attachmentFailure = exception;
            MarkerlessSnapshot(tree, level, "attachment_body_failure");
            throw;
        }
        finally
        {
            try
            {
                Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
                Input.FlushBufferedEvents();
                // Public transient reset is diagnostic cleanup, not evidence of real release/landing.
                claw.ResetTransientState();
                completion.RestoreSession(PrologueObjective.ReachBasin);
                await RestoreSpatialSpawn(tree, level, spawn, spawnSupport,
                    PrologueObjective.ReachBasin, PlayerActionMode.Grapple, tutorials,
                    "attachment_cleanup", null);
            }
            catch (Exception cleanupFailure)
            {
                MarkerlessSnapshot(tree, level, "attachment_cleanup_failure");
                if (attachmentFailure != null)
                    throw new AggregateException("Attachment case and its bounded cleanup both failed",
                        attachmentFailure, cleanupFailure);
                throw;
            }
        }
        TestAssert.True(formal.GetNode<Control>("ObjectiveRibbon").IsVisibleInTree() &&
            formal.GetNode<Label>("ObjectiveRibbon/Text").Text == "REACH THE CANYON BASIN",
            "controlled attachment cleanup restores latest genuine route title without stale early suppression");
        GD.Print("EARLY_LESSON_DENSITY_ATTACHED_OK controlled_saved_lesson=true real_LMB_attachment=true");
    }

    private static async Task AssertCoveredFrames(SceneTree tree, PrologueLevelController level, int frames, string stage)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            AssertCovered(level, stage + " frame=" + frame);
        }
    }

    private static void AssertCovered(PrologueLevelController level, string stage)
    {
        var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Label card = visual.GetNode<Label>("Text");
        Control ribbon = formal.GetNode<Control>("ObjectiveRibbon");
        TestAssert.True(guidance.CurrentInstruction.Visible && string.IsNullOrEmpty(guidance.CurrentInstruction.ContextTitle) &&
            visual.IsVisibleInTree() && visual.HasVisiblePrompt && visual.RenderedTextContained &&
            visual.GetNode<Control>("Backdrop").IsVisibleInTree() && card.IsVisibleInTree() &&
            card.Text.Contains(guidance.CurrentInstruction.StepText, StringComparison.Ordinal) &&
            visual.RenderedStepText == guidance.CurrentInstruction.StepText &&
            visual.PromptRect.Grow(.5f).Encloses(card.GetRect()) && card.GetMinimumSize().Y <= card.Size.Y + .5f,
            "full actual legal current early card remains shaped-text-contained: " + stage);
        TestAssert.True(!ribbon.IsVisibleInTree() && formal.EarlyLessonRibbonSuppressed &&
            !string.IsNullOrEmpty(formal.CurrentModel.Objective) && !string.IsNullOrEmpty(ribbon.GetNode<Label>("Text").Text),
            "actual duplicate is hidden without blanking model or current label: " + stage);
        Rect2 safe = new(new Vector2(48, 40), level.GetViewportRect().Size - new Vector2(96, 80));
        TestAssert.True(safe.Encloses(visual.PromptRect), "actual covered card stays in safe area: " + stage);
        foreach (Rect2 region in visual.ResolveProtectedRegions())
            TestAssert.True(!visual.PromptRect.Intersects(region.Grow(8f)),
                "actual card avoids live gameplay/landing/HUD protected bounds: " + stage);
        bool reservesRibbon = false;
        foreach (Rect2 region in WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formal))
            reservesRibbon |= region == ribbon.GetGlobalRect();
        TestAssert.True(reservesRibbon, "typed hidden early ribbon retains its current bounds for fallback protection: " + stage);
        AssertActionLabels(formal, guidance.Language);
    }

    private static async Task Pulse(SceneTree tree, StringName action)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true, Strength = 1f });
        Input.FlushBufferedEvents();
        await Settle(tree);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false, Strength = 0f });
        Input.FlushBufferedEvents();
        await Settle(tree);
    }

    private static bool Attached(MechanicalClawState state) => state is MechanicalClawState.Latched or
        MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;

    private static T FindDescendant<T>(Node root) where T : Node
    {
        if (root is T match) return match;
        foreach (Node child in root.GetChildren())
        {
            T found = FindDescendant<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static async Task Settle(SceneTree tree)
    {
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        for (int frame = 0; frame < 4; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
