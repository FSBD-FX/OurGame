using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Settings;
using Knight.Ui;
using Knight.Vfx;
using Knight.Boss;
using Knight.Abilities;
using Knight.Combat;
using Knight.Build.Prologue;
using Knight.Localization;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Player;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PrologueGuidanceRuntimeTests
{
    private static PackedScene _levelScene;

    public static async Task Run(SceneTree tree) =>
        await Task7CaseRegistry.Execute(Task7Cases(tree));

    internal static IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Replacement(nameof(UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete),
            UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete),
        Replacement(nameof(UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply),
            UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply),
        Replacement(nameof(UpperAnchorLowFailedCastClearsPendingApply),
            UpperAnchorLowFailedCastClearsPendingApply),
        Replacement(nameof(UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff),
            UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff),
        Replacement(nameof(ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt),
            ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt),
        Case(nameof(MountedGuidanceRendersAndClearsWithoutHidingObjective),
            () => MountedGuidanceRendersAndClearsWithoutHidingObjective(tree)),
        Replacement(nameof(ExpandedTeachingAppearsOnlyForAllowedTriggers),
            ExpandedTeachingAppearsOnlyForAllowedTriggers),
        Case(nameof(MountedGuidancePassesPointerInputThroughEveryOverlayControl),
            () => MountedGuidancePassesPointerInputThroughEveryOverlayControl(tree)),
        Case(nameof(MountedGuidanceDoesNotOverlapBossHud),
            () => MountedGuidanceDoesNotOverlapBossHud(tree)),
        Replacement(nameof(RecoveryCountsWithinOneRoomAndResetsOnTransition),
            RecoveryCountsWithinOneRoomAndResetsOnTransition),
        Replacement(nameof(CrossCheckpointRetriesRestoreTheFailedChallengeStreak),
            CrossCheckpointRetriesRestoreTheFailedChallengeStreak),
        Case(nameof(DisablingGuidanceClearsWithoutTouchingCompletionOrProgress),
            () => DisablingGuidanceClearsWithoutTouchingCompletionOrProgress(tree)),
        Replacement(nameof(RealCriticalPathRetargetsThroughShortcutAndBossStates),
            RealCriticalPathRetargetsThroughShortcutAndBossStates),
        Replacement(nameof(ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels),
            ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels),
        Replacement(nameof(MissingTargetKeepsInstructionAndWarnsOnce),
            MissingTargetKeepsInstructionAndWarnsOnce)
    ];

    private static Task7Case Case(string name, System.Func<Task> execute) =>
        Task7CaseRegistry.Case(nameof(PrologueGuidanceRuntimeTests), name, execute);
    private static Task7Case Replacement(string name, System.Action execute) =>
        Task7CaseRegistry.Case(nameof(PrologueGuidanceRuntimeTests), name, execute);

    public static async Task RunTask7IndependentCases(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
        GD.Print("TASK7_LEGACY_SUITE_CASE_EXECUTED " +
                 "suite=PrologueGuidanceRuntimeTests registry=True");
    }

    public static async Task RunCurrentRiftLandingContractOnly(SceneTree tree)
    {
        const string name = nameof(PrologueGuidanceRuntimeTests) + "." +
            nameof(UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff);
        Task7Case[] selected = Task7Cases(tree).Where(item => item.Name == name).ToArray();
        TestAssert.Equal(1, selected.Length, "rift probe executes exactly the original registered case");
        Task7CaseRun run = await Task7CaseRegistry.Execute(selected);
        TestAssert.Equal(1, run.Completed.Count, "original rift structural case completes once");
        GD.Print("THRUSTER_GUIDANCE_RIFT_CONTRACT_OK original_registered_case=true " +
                 "saved_formal_v2=true structural_only=true native_player_passage=false human=false");
    }

    public static async Task RunCurrentMountedGuidanceOnly(SceneTree tree)
    {
        const string name = nameof(PrologueGuidanceRuntimeTests) + "." +
            nameof(MountedGuidanceRendersAndClearsWithoutHidingObjective);
        Task7Case[] selected = Task7Cases(tree).Where(item => item.Name == name).ToArray();
        TestAssert.Equal(1, selected.Length, "mounted probe executes exactly the original registered case");
        Task7CaseRun run = await Task7CaseRegistry.Execute(selected);
        TestAssert.Equal(1, run.Completed.Count, "original mounted guidance case completes once");
        GD.Print("THRUSTER_MOUNTED_GUIDANCE_OK original_registered_case=true " +
                 "live_recovery=true live_rebinding=true language_switch=true headless_ui_only=true human=false");
    }

    public static async Task RunStableBorderlessPresentation(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "StableBorderlessGuidanceFixture");
        Vector2I originalSize = tree.Root.Size;
        Vector2I originalContent = tree.Root.ContentScaleSize;
        try
        {
            fixture.Controller.SetProcess(false);
            PrologueFormalHud formal = fixture.Level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            formal.SetProcess(false);
            fixture.Level.GetNode<Node>("PlayerRoot").ProcessMode = Node.ProcessModeEnum.Disabled;
            PrologueGuidanceInstruction instruction = fixture.Controller.CurrentInstruction;
            TestAssert.True(instruction.Visible && instruction.StepText.Contains("J") &&
                            instruction.StepText.Contains("K"),
                "stable presentation starts with the real pending keyboard lesson");
            foreach (Vector2I resolution in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
            foreach (float scale in new[] { .8f, 1f, 1.5f })
            {
                tree.Root.Size = resolution;
                tree.Root.ContentScaleSize = resolution;
                formal.SetUiScale(scale);
                fixture.Controller.SetLanguage(language);
                instruction = fixture.Controller.CurrentInstruction;
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                string sample = $"{resolution}/{language}/scale={scale}";
                Vector2[] movingTargets =
                [new(160, 220), new(resolution.X * .6f, resolution.Y * .7f),
                 new(resolution.X - 60, 70), new(-120, resolution.Y * .5f)];
                Rect2 initial = default;
                for (int index = 0; index < movingTargets.Length; index++)
                {
                    fixture.Level.Player.GlobalPosition = movingTargets[index];
                    fixture.Visual.SetExpandedTeaching(false);
                    fixture.Visual.Present(instruction,
                        [new PrologueGuidanceTargetView(movingTargets[index], Vector2.Right,
                            240f, index < 3, new Vector2(180, 160))]);
                    Rect2 card = fixture.Visual.PromptRect;
                    if (index == 0) initial = card;
                    TestAssert.True(fixture.Visual.HasVisiblePrompt && fixture.Visual.RenderedTextContained,
                        "fixed lesson remains visible with full wrapped glyphs at " + sample);
                    TestAssert.Equal(initial.Position, card.Position,
                        "moving world targets and player cannot move the lesson card at " + sample);
                    TestAssert.Equal(initial.Size.X, card.Size.X,
                        "moving world targets cannot resize the lesson card at " + sample);
                    TestAssert.True(card.Position.X <= 64f &&
                                    card.Position.Y >= formal.GetNode<Control>("Health").GetGlobalRect().End.Y + 8f,
                        "lesson stays in the left rail below player status at " + sample);
                    foreach (Rect2 protectedHud in WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formal))
                        TestAssert.True(!card.Intersects(protectedHud),
                            "fixed lesson avoids visible and reserved formal HUD at " + sample);
                    Label text = fixture.Visual.GetNode<Label>("Text");
                    TestAssert.True(text.Text.Contains("J") && text.Text.Contains("K") &&
                                    text.Text.Contains("1") && text.Text.Contains("4") &&
                                    !text.ClipText && text.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming,
                        "fixed lesson keeps every real action/slot glyph without clipping at " + sample);
                }
                Control boss = formal.GetNode<Control>("Boss");
                bool bossWasVisible = boss.Visible;
                boss.Visible = true;
                fixture.Visual.Present(instruction, Array.Empty<PrologueGuidanceTargetView>());
                TestAssert.Equal(initial.Position, fixture.Visual.PromptRect.Position,
                    "boss visibility cannot move the fixed reading anchor at " + sample);
                boss.Visible = bossWasVisible;
                fixture.Visual.SetExpandedTeaching(true);
                fixture.Visual.Present(instruction, Array.Empty<PrologueGuidanceTargetView>());
                TestAssert.Equal(initial.Position, fixture.Visual.PromptRect.Position,
                    "expanded teaching changes content height, not the reading anchor at " + sample);
                TestAssert.Equal(initial.Size.X, fixture.Visual.PromptRect.Size.X,
                    "expanded teaching keeps the same readable column at " + sample);
                TestAssert.True(fixture.Visual.HasVisiblePrompt && fixture.Visual.RenderedTextContained,
                    "expanded teaching keeps its complete localized text inside the fixed card at " + sample);
                TestAssert.True(!fixture.Visual.GetNode<Control>("TopRule").Visible,
                    "guidance has no decorative gold rule at " + sample);
                ColorRect backdrop = fixture.Visual.GetNode<ColorRect>("Backdrop");
                TestAssert.True(backdrop.Visible && backdrop.Color.A > .1f && backdrop.Color.A <= .72f,
                    "guidance retains a soft readable backing without an opaque box at " + sample);
                fixture.Visual.SetMarkerlessSafeEdgeEnabled(true);
                fixture.Visual.Present(instruction, Array.Empty<PrologueGuidanceTargetView>());
                TestAssert.Equal(initial.Position, fixture.Visual.PromptRect.Position,
                    "losing the world target does not move the instruction at " + sample);
                string Expand(string text) => text.Replace("{move_left}", "A").Replace("{move_right}", "D")
                    .Replace("{jump}", "SPACE").Replace("{claw}", "K").Replace("{attack}", "J")
                    .Replace("{shield}", "L");
                var attached = instruction with
                {
                    StepText = Expand(PrologueStrings.Get("guidance.grapple_attached", language)),
                    HintText = Expand(PrologueStrings.Get("guidance.hint.ascent", language))
                };
                fixture.Visual.SetExpandedTeaching(true);
                fixture.Visual.Present(attached, Array.Empty<PrologueGuidanceTargetView>());
                TestAssert.True(fixture.Visual.HasVisiblePrompt && fixture.Visual.RenderedTextContained,
                    "attached keyword lesson keeps every action visible at " + sample);
                TestAssert.Equal(initial.Position, fixture.Visual.PromptRect.Position,
                    "long attached keyword lesson keeps the same reading anchor at " + sample);
            }
            PrologueFormalHudTests.AssertBorderlessPresentation(formal);
            GD.Print("STABLE_BORDERLESS_GUIDANCE_OK samples=12 moving_target_samples=48 full_glyphs=True");
        }
        finally
        {
            tree.Root.Size = originalSize;
            tree.Root.ContentScaleSize = originalContent;
            await Dispose(tree, fixture);
        }
    }

    public static async Task RunTask8MarkerlessCard(SceneTree tree)
    {
        AssertTask8ClawLocalization();
        Fixture fixture = await LoadFixture(tree, "Task8MarkerlessCardFixture");
        try
        {
            PrologueGameFlowController flow = fixture.Level.GetNode<PrologueGameFlowController>("GameFlow");
            Camera2D playerCamera = fixture.Level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D");
            playerCamera.Enabled = true;
            flow.ReturnToTitle();
            flow.StartNew();
            PlayerInputReader inputReader = fixture.Level.Player.GetNode<PlayerInputReader>("Input");
            bool processingImmediatelyAfterStart = inputReader.IsPhysicsProcessing();
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
                "actual-input markerless proof enters the normal playing lifecycle");
            TestAssert.True(processingImmediatelyAfterStart && inputReader.IsPhysicsProcessing(),
                $"normal Start/Playing path owns active player input processing; immediate=" +
                $"{processingImmediatelyAfterStart} settled={inputReader.IsPhysicsProcessing()} " +
                $"player={fixture.Level.Player.IsPhysicsProcessing()} " +
                $"motor={fixture.Level.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing()} " +
                $"player_root_mode={fixture.Level.GetNode<Node>("PlayerRoot").ProcessMode}");
            PrologueCompletionController completion =
                fixture.Level.GetNode<PrologueCompletionController>("Completion");
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective,
                "normal entry begins with the genuine pending attack lesson");
            int travelFrames = 0;
            int recoveryRequests = 0;
            float maximumWorldX = fixture.Level.Player.GlobalPosition.X;
            void RecordRecovery(string _) => recoveryRequests++;
            fixture.Level.RecoveryRequested += RecordRecovery;
            Input.ParseInputEvent(new InputEventKey
            {
                Keycode = Key.D,
                PhysicalKeycode = Key.D,
                Pressed = true,
            });
            try
            {
                while (travelFrames < 720 &&
                       fixture.Level.CurrentSector != PrologueSectorId.CanyonDescent)
                {
                    int jumpPhase = travelFrames % 30;
                    if (jumpPhase == 0)
                        Input.ParseInputEvent(new InputEventKey
                        {
                            Keycode = Key.Space,
                            PhysicalKeycode = Key.Space,
                            Pressed = true,
                        });
                    else if (jumpPhase == 12)
                        Input.ParseInputEvent(new InputEventKey
                        {
                            Keycode = Key.Space,
                            PhysicalKeycode = Key.Space,
                            Pressed = false,
                        });
                    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    travelFrames++;
                    maximumWorldX = Mathf.Max(maximumWorldX,
                        fixture.Level.Player.GlobalPosition.X);
                }
            }
            finally
            {
                fixture.Level.RecoveryRequested -= RecordRecovery;
                Input.ParseInputEvent(new InputEventKey
                {
                    Keycode = Key.D,
                    PhysicalKeycode = Key.D,
                    Pressed = false,
                });
                Input.ActionRelease(InputActions.MoveRight);
                Input.ParseInputEvent(new InputEventKey
                {
                    Keycode = Key.Space,
                    PhysicalKeycode = Key.Space,
                    Pressed = false,
                });
                Input.ActionRelease(InputActions.Jump);
            }
            fixture.Controller.RefreshNow();
            TestAssert.Equal(PrologueSectorId.CanyonDescent, fixture.Level.CurrentSector.Value,
                $"actual move-right input leaves Entrance without completing the pending lesson " +
                $"within the bounded proof window; frames={travelFrames} " +
                $"position={fixture.Level.Player.GlobalPosition} max_x={maximumWorldX} " +
                $"recoveries={recoveryRequests} flow={flow.State} " +
                $"reader_processing={inputReader.IsPhysicsProcessing()} " +
                $"sampled_axis={inputReader.CurrentFrame.MoveAxis} " +
                $"action_pressed={Input.IsActionPressed(InputActions.MoveRight)}");
            TestAssert.Equal(PrologueObjective.LearnAttackMode, completion.CurrentObjective,
                "off-owner projection preserves actual pending lesson authority");
            TestAssert.True(fixture.Controller.CurrentInstruction.Visible &&
                            fixture.Visual.RenderedTargetCount == 0 &&
                            fixture.Visual.MarkerlessSafeEdgeEnabled,
                "normal layout2 keeps full pending instruction with no fabricated local target");

            Vector2I oldSize = tree.Root.Size;
            Vector2I oldContent = tree.Root.ContentScaleSize;
            var formal = fixture.Level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            try
            {
                foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
                foreach (GameLanguage language in new[]
                         { GameLanguage.ChineseSimplified, GameLanguage.English })
                foreach (float scale in new[] { .8f, 1.5f })
                {
                    tree.Root.Size = size;
                    tree.Root.ContentScaleSize = size;
                    formal.SetUiScale(scale);
                    fixture.Controller.SetLanguage(language);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    Rect2 card = fixture.Visual.PromptRect;
                    Rect2 centralAction = new(size.X * .22f, size.Y * .18f,
                        size.X * .56f, size.Y * .64f);
                    string label = $"{size} {language} scale={scale}";
                    TestAssert.True(fixture.Visual.HasVisiblePrompt &&
                                    fixture.Visual.RenderedTextContained &&
                                    !card.Intersects(centralAction),
                        "markerless full teaching card uses a readable safe edge outside central action at " +
                        $"{label}; visible={fixture.Visual.HasVisiblePrompt} " +
                        $"contained={fixture.Visual.RenderedTextContained} card={card} " +
                        $"central={centralAction} visual_size={fixture.Visual.Size} " +
                        $"prompt_size={fixture.Visual.LastPromptSize} " +
                        $"protected={string.Join("|", fixture.Visual.ResolveProtectedRegions())}");
                    TestAssert.True(card.Position.X <= 64f || card.End.X >= size.X - 64f ||
                                    card.Position.Y <= 64f || card.End.Y >= size.Y - 64f,
                        "markerless teaching card remains edge-anchored at " + label);
                    Label text = fixture.Visual.GetNode<Label>("Text");
                    TestAssert.True(text.Text.Contains("Q") &&
                                    text.GetMinimumSize().Y <= text.Size.Y + .5f,
                        "complete localized pending text remains wrapped without ellipsis at " + label);
                }
            }
            finally
            {
                tree.Root.Size = oldSize;
                tree.Root.ContentScaleSize = oldContent;
                formal.SetUiScale(1f);
            }

            Label pendingLabel = fixture.Visual.GetNode<Label>("Text");
            string completePendingLabel = pendingLabel.Text;
            Control objectiveRibbon = fixture.Level.GetNode<Control>(
                "Hud/FormalHud/ObjectiveRibbon");
            Vector2 oldRibbonPosition = objectiveRibbon.Position;
            Vector2 oldRibbonSize = objectiveRibbon.Size;
            bool oldRibbonVisible = objectiveRibbon.Visible;
            try
            {
                objectiveRibbon.Visible = true;
                objectiveRibbon.Position = Vector2.Zero;
                objectiveRibbon.Size = fixture.Visual.Size;
                fixture.Controller.RefreshNow();
                TestAssert.True(fixture.Controller.CurrentInstruction.Visible &&
                                !fixture.Visual.HasVisiblePrompt && !pendingLabel.Visible &&
                                fixture.Visual.RenderedStepText.Length > 0 &&
                                pendingLabel.Text == completePendingLabel,
                    "actual pending markerless card hides content but preserves its complete label " +
                    "when every safe edge is protected");
            }
            finally
            {
                objectiveRibbon.Position = oldRibbonPosition;
                objectiveRibbon.Size = oldRibbonSize;
                objectiveRibbon.Visible = oldRibbonVisible;
            }
            fixture.Controller.RefreshNow();
            TestAssert.True(fixture.Controller.CurrentInstruction.Visible &&
                            fixture.Visual.HasVisiblePrompt && pendingLabel.Visible &&
                            fixture.Visual.RenderedTextContained &&
                            pendingLabel.Text == completePendingLabel,
                "the same actual pending card restores its complete label when a legal edge returns");
        }
        finally { await Dispose(tree, fixture); }
    }

    private static void AssertTask8ClawLocalization()
    {
        string chinese = PrologueStrings.Get("controls.grapple_keyboard_help",
            GameLanguage.ChineseSimplified);
        TestAssert.True(chinese.Contains("E") && chinese.Contains("点按左键") &&
                        chinese.Contains("自动牵引") && chinese.Contains("SPACE") &&
                        chinese.Contains("松爪") && !chinese.Contains("W/S") &&
                        !chinese.Contains("按住左键"),
            "Chinese claw copy teaches E, click, automatic pull, then Space release without stale holds");
        string english = PrologueStrings.Get("controls.grapple_keyboard_help", GameLanguage.English);
        TestAssert.True(english.Contains("E") && english.Contains("click LMB") &&
                        english.Contains("pulls automatically") && english.Contains("SPACE") &&
                        english.Contains("releases") && !english.Contains("W/S") &&
                        !english.Contains("hold LMB", StringComparison.OrdinalIgnoreCase),
            "English claw copy teaches E, click, automatic pull, then Space release without stale holds");
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            string template = PrologueStrings.Get("guidance.grapple", language);
            int select = template.IndexOf("{grapple_mode}", StringComparison.Ordinal);
            int click = template.IndexOf("{primary}", StringComparison.Ordinal);
            int automatic = language == GameLanguage.English
                ? template.IndexOf("automatically", StringComparison.OrdinalIgnoreCase)
                : template.IndexOf("自动牵引", StringComparison.Ordinal);
            int release = template.IndexOf("{jump}", StringComparison.Ordinal);
            TestAssert.True(select >= 0 && click > select && automatic > click && release > automatic &&
                            !template.Contains("W/S") &&
                            !template.Contains("hold", StringComparison.OrdinalIgnoreCase) &&
                            !template.Contains("按住", StringComparison.Ordinal),
                $"{language} grapple teaching preserves select-click-auto-pull-release order");
        }
    }

    public static async Task RunTask8MovingVisualProof(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "Task8MovingVisualProof");
        try
        {
            PrologueGameFlowController flow = fixture.Level.GetNode<PrologueGameFlowController>("GameFlow");
            fixture.Level.Player.GetNode<Camera2D>("Camera2D").Enabled = true;
            flow.ReturnToTitle();
            flow.StartNew();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            Input.ParseInputEvent(new InputEventKey
            {
                Keycode = Key.D,
                PhysicalKeycode = Key.D,
                Pressed = true,
            });
            int frame = 0;
            try
            {
                while (frame < 720 && fixture.Level.CurrentSector != PrologueSectorId.CanyonDescent)
                {
                    if (frame % 30 == 0)
                        Input.ParseInputEvent(new InputEventKey
                        {
                            Keycode = Key.Space,
                            PhysicalKeycode = Key.Space,
                            Pressed = true,
                        });
                    else if (frame % 30 == 12)
                        Input.ParseInputEvent(new InputEventKey
                        {
                            Keycode = Key.Space,
                            PhysicalKeycode = Key.Space,
                            Pressed = false,
                        });
                    await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    frame++;
                }
            }
            finally
            {
                Input.ParseInputEvent(new InputEventKey
                {
                    Keycode = Key.D,
                    PhysicalKeycode = Key.D,
                    Pressed = false,
                });
                Input.ActionRelease(InputActions.MoveRight);
                Input.ParseInputEvent(new InputEventKey
                {
                    Keycode = Key.Space,
                    PhysicalKeycode = Key.Space,
                    Pressed = false,
                });
                Input.ActionRelease(InputActions.Jump);
            }

            fixture.Controller.RefreshNow();
            TestAssert.Equal(PrologueSectorId.CanyonDescent, fixture.Level.CurrentSector.Value,
                $"visual proof uses real D+Space travel into Descent; frames={frame}");
            TestAssert.True(fixture.Controller.CurrentInstruction.Visible &&
                            fixture.Visual.RenderedTargetCount == 0,
                "visual proof reaches the genuine markerless pending lesson");
            GD.Print($"TASK8_VISUAL_CARD mode={(fixture.Visual.MarkerlessSafeEdgeEnabled ? "safe-edge" : "legacy")} " +
                     $"rect={fixture.Visual.PromptRect} player={fixture.Level.Player.GlobalPosition} " +
                     $"sector={fixture.Level.CurrentSector} visual_global={fixture.Visual.GlobalPosition} " +
                     $"visual_rect={fixture.Visual.GetGlobalRect()} " +
                     $"backdrop_global={fixture.Visual.GetNode<Control>("Backdrop").GetGlobalRect()}");
            for (int hold = 0; hold < 150; hold++)
            {
                if (hold == 30)
                    Input.ParseInputEvent(new InputEventKey
                    {
                        Keycode = Key.A,
                        PhysicalKeycode = Key.A,
                        Pressed = true,
                    });
                if (hold == 90)
                {
                    Input.ParseInputEvent(new InputEventKey
                    {
                        Keycode = Key.A,
                        PhysicalKeycode = Key.A,
                        Pressed = false,
                    });
                    Input.ActionRelease(InputActions.MoveLeft);
                }
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                if (hold is 30 or 90 or 149)
                    GD.Print($"TASK8_VISUAL_FRAME hold={hold} prompt={fixture.Visual.PromptRect} " +
                             $"backdrop_global={fixture.Visual.GetNode<Control>("Backdrop").GetGlobalRect()} " +
                             $"player_screen={fixture.Level.Player.GetGlobalTransformWithCanvas().Origin}");
            }
        }
        finally
        {
            Input.ActionRelease(InputActions.MoveLeft);
            Input.ActionRelease(InputActions.MoveRight);
            Input.ActionRelease(InputActions.Jump);
            await Dispose(tree, fixture);
        }
    }

    internal static async Task AssertMountedGuidanceFailureDisposesFixture(SceneTree tree)
    {
        // Exercise the real case's assertion-failure path, not a mock cleanup helper.
        // The fourth slot is deliberately rebound so the approved default-glyph
        // assertion must fail. Restore the complete live map even when cleanup fails.
        ulong[] before = tree.Root.GetChildren().Select(node => node.GetInstanceId()).Order().ToArray();
        InputEvent[] oldBindings = InputMap.ActionGetEvents(InputActions.Slot4).ToArray();
        bool assertionObserved = false;
        try
        {
            InputMap.ActionEraseEvents(InputActions.Slot4);
            InputMap.ActionAddEvent(InputActions.Slot4, new InputEventKey { PhysicalKeycode = Key.Y });
            try { await MountedGuidanceRendersAndClearsWithoutHidingObjective(tree); }
            catch (InvalidOperationException exception) when (exception.Message ==
                "Assertion failed: live InputMap glyphs populate the first attack-mode instruction")
            { assertionObserved = true; }
        }
        finally
        {
            InputMap.ActionEraseEvents(InputActions.Slot4);
            foreach (InputEvent binding in oldBindings) InputMap.ActionAddEvent(InputActions.Slot4, binding);
        }
        TestAssert.True(assertionObserved, "the actual mounted case must expose its deliberate glyph assertion failure");
        TestAssert.True(!tree.Paused && before.SequenceEqual(tree.Root.GetChildren()
            .Select(node => node.GetInstanceId()).Order()),
            "failed mounted guidance assertion must release its scene and leave the tree unpaused");
        GD.Print("MOUNTED_GUIDANCE_FAILURE_CLEANUP_OK actual_assertion=True map_restored=True");
    }

    private static async Task MountedGuidanceRendersAndClearsWithoutHidingObjective(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "MountedContinuousGuidanceFixture");
        try
        {
        TestAssert.True(fixture.Level.CurrentSector.HasValue &&
            GodotObject.IsInstanceValid(fixture.Level.CurrentSectorRoot),
            "guidance authority is the live current mounted sector");
        TestAssert.True(fixture.Visual is IPrologueGuidanceVisual && fixture.Visual.Visible &&
            fixture.Visual.RenderedTargetCount > 0,
            "continuous guidance presents its current-sector target through the visual contract");
        TestAssert.True(fixture.Controller.CurrentInstruction.StepText.Contains("J") &&
            fixture.Controller.CurrentInstruction.StepText.Contains("K") &&
            fixture.Controller.CurrentInstruction.StepText.Contains("1") &&
            fixture.Controller.CurrentInstruction.StepText.Contains("4"),
            "live InputMap glyphs populate the first attack-mode instruction");
        AssertLiveKeyboardGlyphRebinding(fixture);

        for (int recovery = 0;
             recovery < 4 && fixture.Controller.SameSectorFailureCount < 2;
             recovery++)
        {
            fixture.Level.RequestRespawn("DEATH RESET");
            await SettleRecovery(tree);
            fixture.Controller.RefreshNow();
        }
        TestAssert.True(fixture.Controller.SameSectorFailureCount >= 2 &&
                        fixture.Visual.IsExpandedTeaching,
            "two real same-sector death recoveries expand mounted guidance");
        fixture.Controller.SetLanguage(GameLanguage.English);
        string english = fixture.Level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        GD.Print($"MOUNTED_GUIDANCE_LANGUAGE_FACTS language={fixture.Controller.Language} " +
                 $"expanded={fixture.Visual.IsExpandedTeaching} " +
                 $"heading={fixture.Controller.CurrentInstruction.HeadingText} " +
                 $"hint={fixture.Controller.CurrentInstruction.HintPrefix} " +
                 $"text={english.Replace("\n", "\\n")}");
        TestAssert.True(english.Contains("First-Level Guidance", System.StringComparison.Ordinal) &&
            !english.Contains("第一关引导", System.StringComparison.Ordinal) &&
            !english.Contains("提示：", System.StringComparison.Ordinal),
            "mounted English expanded guidance localizes its heading and hint chrome");
        fixture.Controller.SetLanguage(GameLanguage.ChineseSimplified);
        string chinese = fixture.Level.GetNode<Label>("Hud/Guidance/VisualRoot/Text").Text;
        GD.Print($"MOUNTED_GUIDANCE_LANGUAGE_FACTS language={fixture.Controller.Language} " +
                 $"expanded={fixture.Visual.IsExpandedTeaching} " +
                 $"heading={fixture.Controller.CurrentInstruction.HeadingText} " +
                 $"hint={fixture.Controller.CurrentInstruction.HintPrefix} " +
                 $"text={chinese.Replace("\n", "\\n")}");
        TestAssert.True(chinese.Contains("第一关引导", System.StringComparison.Ordinal) &&
            !chinese.Contains("First-Level Guidance", System.StringComparison.Ordinal),
            "mounted Chinese expanded guidance retains its localized heading chrome");

        fixture.Controller.SetGuidanceEnabled(false);
        TestAssert.True(!fixture.Visual.Visible && fixture.Visual.RenderedTargetCount == 0 &&
            fixture.Visual.RenderedStepText.Length == 0,
            "disabling guidance clears the continuous target without hiding the objective HUD");
        TestAssert.True(fixture.Level.GetNode<Label>("Hud/Objective").Visible,
            "guidance state does not own objective visibility");
        }
        finally { await Dispose(tree, fixture); }
    }

    private static void AssertLiveKeyboardGlyphRebinding(Fixture fixture)
    {
        InputEvent[] attack = InputMap.ActionGetEvents(InputActions.Attack).ToArray();
        InputEvent[] claw = InputMap.ActionGetEvents(InputActions.Claw).ToArray();
        try
        {
            InputMap.ActionEraseEvents(InputActions.Attack);
            InputMap.ActionAddEvent(InputActions.Attack, new InputEventKey { PhysicalKeycode = Key.F });
            InputMap.ActionEraseEvents(InputActions.Claw);
            InputMap.ActionAddEvent(InputActions.Claw, new InputEventKey { PhysicalKeycode = Key.G });
            fixture.Controller.RefreshNow();
            string text = fixture.Controller.CurrentInstruction.StepText;
            TestAssert.True(text.Contains("F") && text.Contains("G") &&
                            !text.Contains("J") && !text.Contains("K") &&
                            fixture.Visual.RenderedStepText == text,
                "mounted teaching must render live attack/claw rebinding rather than stale J/K or mode-switch glyphs");
        }
        finally
        {
            InputMap.ActionEraseEvents(InputActions.Attack);
            foreach (InputEvent binding in attack) InputMap.ActionAddEvent(InputActions.Attack, binding);
            InputMap.ActionEraseEvents(InputActions.Claw);
            foreach (InputEvent binding in claw) InputMap.ActionAddEvent(InputActions.Claw, binding);
            fixture.Controller.RefreshNow();
        }
        TestAssert.True(fixture.Visual.RenderedStepText.Contains("J") &&
                        fixture.Visual.RenderedStepText.Contains("K"),
            "restoring the live keyboard map restores the displayed default attack/claw glyphs");
        GD.Print("MOUNTED_GUIDANCE_KEYBOARD_GLYPHS_OK defaults=JK rebind=FG restored=JK");
    }

    private static async Task MountedGuidancePassesPointerInputThroughEveryOverlayControl(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceInputFixture");
        try
        {
        Control text = fixture.Level.GetNode<Control>("Hud/Guidance/VisualRoot/Text");
        TestAssert.Equal(Control.MouseFilterEnum.Ignore, fixture.Visual.MouseFilter,
            "guidance root passes mouse input through");
        TestAssert.Equal(Control.MouseFilterEnum.Ignore, text.MouseFilter,
            "guidance text passes mouse input through");
        }
        finally { await Dispose(tree, fixture); }
    }

    private static async Task MountedGuidanceDoesNotOverlapBossHud(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceLayoutFixture");
        try
        {
        Control panel = fixture.Level.GetNode<Control>("Hud/Panel");
        Control boss = fixture.Level.GetNode<Control>("Hud/Boss");
        TestAssert.True(!panel.GetGlobalRect().Intersects(boss.GetGlobalRect()),
            "guidance/objective panel geometry does not overlap the Boss HUD");
        }
        finally { await Dispose(tree, fixture); }
    }

    public static async Task ContinuousGuidanceProtectsLiveGameplay(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceProtectionFixture");
        try
        {
            AssertLandingSurfaceTopProtection(fixture);
            AssertRaisedArmorProtection(fixture);
            fixture.Level.GetNode<Node2D>("PlayerRoot").Visible = true;
            Node2D player = fixture.Level.Player;
            var rope = new ThreadLineVisual { Name = "ProtectionTestRope" };
            player.AddChild(rope);
            Vector2 ropeEnd = player.GlobalPosition + new Vector2(220, -180);
            rope.Present(player.GlobalPosition, ropeEnd, .8f, ThreadLineState.Latched);
            var shuttle = new ThreadShuttleVisual { Name = "ProtectionTestReturningShuttle" };
            player.AddChild(shuttle);
            shuttle.Present(MechanicalClawFrame.Idle(player.GlobalPosition) with
            {
                State = MechanicalClawState.Retract,
                AnchorPoint = player.GlobalPosition + new Vector2(420, 0), RopeLength = 360
            });
            var hammer = new FallenBellkeeperFormalPart
            {
                Name = "ProtectionTestRaisedHammer", Kind = FallenBellkeeperPartKind.Hammer,
                Position = new Vector2(520, 290), Rotation = -.9f
            };
            fixture.Level.CurrentSectorRoot.AddChild(hammer);
            var terrain = new StaticBody2D
            {
                Name = "ProtectionTestStaticTerrain", Position = new Vector2(900, 560),
                CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget
            };
            terrain.AddChild(new CollisionShape2D
            {
                Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(100, 100) }
            });
            terrain.AddChild(new GrappleSurface2D
            {
                Name = "GrappleSurfaceProfile", UseCollisionHitPoint = true,
                Material = GrappleSurfaceMaterial.GripStone
            });
            fixture.Level.CurrentSectorRoot.AddChild(terrain);

            var regions = fixture.Visual.ResolveProtectedRegions();
            Vector2 playerScreen = player.GetGlobalTransformWithCanvas().Origin;
            TestAssert.True(Contains(regions, playerScreen),
                "continuous guidance protects the live player even when CurrentRoom is null");
            Vector2 ropeScreen = player.GetCanvasTransform() * player.GlobalPosition.Lerp(ropeEnd, .5f);
            TestAssert.True(Contains(regions, ropeScreen),
                "continuous guidance protects the visible mechanical-claw rope");
            Vector2 hammerScreen = hammer.GetGlobalTransformWithCanvas() * new Vector2(76, -56);
            TestAssert.True(Contains(regions, hammerScreen),
                "guidance protects the raised hammer silhouette, not only the Boss hitbox");
            TestAssert.True(Contains(regions, shuttle.GetCanvasTransform() * shuttle.ShuttleWorldPosition),
                "guidance protects the returning shuttle even after the tether line is cleared");
            TestAssert.True(!Contains(regions, terrain.GetGlobalTransformWithCanvas().Origin),
                "grippable static walls do not consume the whole prompt safe area");
            Rect2 prompt = WorldTargetMarkerVisual.PlacePrompt(new Rect2(48, 40, 1184, 640),
                playerScreen, new Vector2(248, 64), regions);
            TestAssert.True(prompt.HasArea(), "gameplay protection still leaves a readable prompt location");
            foreach (Rect2 region in regions)
                TestAssert.True(!prompt.Intersects(region.Grow(8)),
                    "guidance card cannot overlap a live protected gameplay silhouette");
            fixture.Controller.SetProcess(false);
            Label text = fixture.Visual.GetNode<Label>("Text");
            text.AddThemeConstantOverride("line_spacing", 8);
            fixture.Visual.Present(new PrologueGuidanceInstruction(true,
                "按 E 切换机械爪 → 瞄准粗糙墙面、平台边缘或顶面并点按 LMB → 命中后自动牵引 → " +
                "按 SPACE 跃出并松爪；Q/RMB 取消。冰面和封闭表面不可抓。",
                string.Empty, string.Empty, string.Empty, Array.Empty<PrologueGuidanceTargetKey>()),
                Array.Empty<PrologueGuidanceTargetView>());
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Control backdrop = fixture.Visual.GetNode<Control>("Backdrop");
            TestAssert.True(fixture.Visual.HasVisiblePrompt &&
                backdrop.GetGlobalRect().Encloses(text.GetGlobalRect()),
                "the actual wrapped label, including theme line spacing, fits inside its background");
        }
        finally { await Dispose(tree, fixture); }
    }

    private static void AssertRaisedArmorProtection(Fixture fixture)
    {
        var scope = new Node2D { Name = "RaisedArmorProtectionFixture" };
        fixture.Level.CurrentSectorRoot.AddChild(scope);
        try
        {
            var canvas = new CanvasLayer
            {
                Name = "ArmorTestCanvas", Offset = new Vector2(21, -15),
                Rotation = -.12f, Scale = new Vector2(.92f, 1.08f)
            };
            scope.AddChild(canvas);
            var parent = new Node2D
            {
                Name = "ArmorTestPoseParent", Position = new Vector2(700, 350),
                Rotation = .35f, Scale = new Vector2(1.25f, .7f)
            };
            canvas.AddChild(parent);
            var armor = new FallenBellkeeperFormalPart
            {
                Name = "ProtectionTestRaisedArmor", Kind = FallenBellkeeperPartKind.ArmorShell,
                Position = new Vector2(-60, -25), Rotation = -.95f
            };
            parent.AddChild(armor);
            // Independently transcribed from DrawArmor's painted outer Ink polygon.
            // Its rim, arc and patina strokes are inside these seven outer vertices.
            Vector2[] paintedVertices =
            [
                new(-68, -62), new(54, -66), new(73, 43), new(50, 57),
                new(2, 46), new(-42, 61), new(-78, 34)
            ];
            Transform2D raisedScreen = canvas.Transform * parent.Transform * armor.Transform;
            Rect2 raisedBounds = AssertPaintedArmorProtection(raisedScreen, paintedVertices,
                RoomProtectionRegionAdapter.CollectGameplay(scope, null), "raised");
            AssertPaintedArmorProtection(raisedScreen, paintedVertices,
                fixture.Visual.ResolveProtectedRegions(), "mounted guidance");
            AssertArmorPromptAvoidance(fixture, raisedScreen.Origin, raisedBounds);

            armor.Visible = false;
            TestAssert.Equal(0, RoomProtectionRegionAdapter.CollectGameplay(scope, null).Count,
                "hidden ArmorShell reserves no gameplay protection region");
            armor.Visible = true;
            parent.Visible = false;
            TestAssert.Equal(0, RoomProtectionRegionAdapter.CollectGameplay(scope, null).Count,
                "ArmorShell under a hidden pose parent reserves no gameplay protection region");
            parent.Visible = true;

            parent.Position += new Vector2(320, 80);
            parent.Rotation = -.45f;
            parent.Scale = new Vector2(.8f, 1.35f);
            armor.Position = new Vector2(35, -50);
            armor.Rotation = .45f;
            canvas.Offset = new Vector2(-17, 29);
            canvas.Rotation = .08f;
            canvas.Scale = new Vector2(1.05f, .95f);
            Transform2D movedScreen = canvas.Transform * parent.Transform * armor.Transform;
            IReadOnlyList<Rect2> movedRegions = RoomProtectionRegionAdapter.CollectGameplay(scope, null);
            Rect2 movedBounds = AssertPaintedArmorProtection(movedScreen, paintedVertices,
                movedRegions, "current transformed");
            TestAssert.True(!Contains(movedRegions, raisedScreen.Origin),
                "moving ArmorShell and its canvas leaves no stale raised-pose protection");
            AssertPaintedArmorProtection(movedScreen, paintedVertices,
                fixture.Visual.ResolveProtectedRegions(), "current mounted guidance");
            AssertArmorPromptAvoidance(fixture, movedScreen.Origin, movedBounds);
            GD.Print("GUIDANCE_RAISED_ARMOR_PROTECTION_OK painted_vertices=7 parent_transform=True " +
                     "canvas_transform=True hidden=False stale=False actual_prompt=True");
        }
        finally { scope.Free(); }
    }

    private static Rect2 AssertPaintedArmorProtection(Transform2D screen, Vector2[] vertices,
        IReadOnlyList<Rect2> regions, string pose)
    {
        Vector2[] paintedScreen = vertices.Select(vertex => screen * vertex).ToArray();
        // Rect2.HasPoint excludes the far edge; tolerate only float roundoff there.
        TestAssert.True(regions.Any(region => paintedScreen.All(point => region.Grow(.01f).HasPoint(point))),
            $"{pose} ArmorShell protection encloses all seven painted polygon vertices, " +
            "independently of player, rope, hammer, terrain and Boss collision regions");
        Vector2 minimum = new(paintedScreen.Min(point => point.X), paintedScreen.Min(point => point.Y));
        Vector2 maximum = new(paintedScreen.Max(point => point.X), paintedScreen.Max(point => point.Y));
        return new Rect2(minimum, maximum - minimum);
    }

    private static void AssertArmorPromptAvoidance(Fixture fixture, Vector2 target, Rect2 paintedBounds)
    {
        IReadOnlyList<Rect2> regions = fixture.Visual.ResolveProtectedRegions();
        Rect2 prompt = WorldTargetMarkerVisual.PlacePrompt(new Rect2(48, 40, 1184, 640),
            target, new Vector2(248, 64), regions);
        TestAssert.True(prompt.HasArea(), "raised armor protection leaves an actionable prompt location");
        TestAssert.True(!prompt.Intersects(paintedBounds.Grow(8)),
            "actual guidance placer cannot cover the independently derived painted ArmorShell bounds");
        foreach (Rect2 region in regions)
            TestAssert.True(!prompt.Intersects(region.Grow(8)),
                "armor-aware guidance retains all other live gameplay and HUD protection");
    }

    private static void AssertLandingSurfaceTopProtection(Fixture fixture)
    {
        var scope = new Node2D { Name = "LandingSurfaceProtectionFixture" };
        fixture.Level.CurrentSectorRoot.AddChild(scope);
        try
        {
            var platform = new StaticBody2D
            {
                Name = "HorizontalLandingPlatform", Position = new Vector2(760, 360),
                CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget
            };
            // Match CanyonContentBuilder's authored safe-catch-shelf contract;
            // a wide rectangle alone must not classify a wall or roof as a landing.
            platform.SetMeta("catch_shelf", true);
            var collision = new CollisionShape2D
            {
                Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(160, 32) }
            };
            platform.AddChild(collision);
            platform.AddChild(new GrappleSurface2D
            {
                Name = "GrappleSurfaceProfile", UseCollisionHitPoint = true,
                Material = GrappleSurfaceMaterial.GripStone
            });
            scope.AddChild(platform);
            var wall = new StaticBody2D
            {
                Name = "VerticalWall", Position = new Vector2(1000, 360),
                CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget
            };
            wall.AddChild(new CollisionShape2D
            {
                Shape = new RectangleShape2D { Size = new Vector2(32, 160) }
            });
            wall.AddChild(new GrappleSurface2D
            {
                Name = "GrappleSurfaceProfile", UseCollisionHitPoint = true,
                Material = GrappleSurfaceMaterial.GripStone
            });
            scope.AddChild(wall);

            IReadOnlyList<Rect2> regions = RoomProtectionRegionAdapter.CollectGameplay(scope, null);
            Transform2D screen = collision.GetGlobalTransformWithCanvas();
            foreach (float x in new[] { -60f, 0f, 60f })
                TestAssert.True(Contains(regions, screen * new Vector2(x, -16f)),
                    "mounted grippable landing platform protects points along its actual horizontal top, not only anchors");
            TestAssert.True(!Contains(regions, screen.Origin),
                "landing protection is a thin top band rather than the full platform interior");
            TestAssert.True(!Contains(regions, wall.GetGlobalTransformWithCanvas().Origin),
                "vertical wall interiors do not consume the prompt-placement region");
            TestAssert.True(Contains(fixture.Visual.ResolveProtectedRegions(), screen * new Vector2(0, -16f)),
                "the mounted guidance visual receives the landing top through its real protection adapter");

            platform.RemoveMeta("catch_shelf");
            TestAssert.True(!Contains(RoomProtectionRegionAdapter.CollectGameplay(scope, null),
                screen * new Vector2(0, -16f)), "identical unmarked terrain is not inferred to be a safe landing shelf");
            platform.SetMeta("catch_shelf", false);
            TestAssert.True(!Contains(RoomProtectionRegionAdapter.CollectGameplay(scope, null),
                screen * new Vector2(0, -16f)), "an explicit false catch-shelf marker does not enable landing protection");
            platform.SetMeta("catch_shelf", true);
            TestAssert.True(Contains(RoomProtectionRegionAdapter.CollectGameplay(scope, null),
                screen * new Vector2(0, -16f)), "restoring the authored catch-shelf marker restores top protection");

            collision.Disabled = true;
            TestAssert.True(!Contains(RoomProtectionRegionAdapter.CollectGameplay(scope, null),
                screen * new Vector2(0, -16f)), "disabled platform collision has no protected landing top");
            collision.Disabled = false;
            platform.Visible = false;
            TestAssert.True(!Contains(RoomProtectionRegionAdapter.CollectGameplay(scope, null),
                screen * new Vector2(0, -16f)), "hidden platform has no protected landing top");
            GD.Print("GUIDANCE_LANDING_SURFACE_PROTECTION_OK marked_top=True unmarked=False false_marker=False interior=False disabled=False hidden=False");
        }
        finally { scope.Free(); }
    }

    private static bool Contains(IReadOnlyList<Rect2> regions, Vector2 point)
    {
        foreach (Rect2 region in regions)
            if (region.HasPoint(point)) return true;
        return false;
    }

    private static async Task DisablingGuidanceClearsWithoutTouchingCompletionOrProgress(SceneTree tree)
    {
        Fixture fixture = await LoadFixture(tree, "ContinuousGuidanceDisableFixture");
        try
        {
        PrologueCompletionController completion = fixture.Level.GetNode<PrologueCompletionController>("Completion");
        PrologueObjective objective = completion.CurrentObjective;
        PrologueCheckpointSnapshot before = fixture.Level.Progress.RestoreCheckpoint();
        fixture.Controller.SetGuidanceEnabled(false);
        PrologueCheckpointSnapshot after = fixture.Level.Progress.RestoreCheckpoint();

        TestAssert.True(!fixture.Visual.Visible && fixture.Visual.RenderedTargetCount == 0,
            "disabling guidance clears every rendered marker");
        TestAssert.Equal(objective, completion.CurrentObjective,
            "disabling guidance leaves the exact objective untouched");
        TestAssert.True(before.SectorId == after.SectorId &&
            before.WorldPosition == after.WorldPosition &&
            before.HasActivatedCheckpoint == after.HasActivatedCheckpoint &&
            before.ShortcutOpen == after.ShortcutOpen &&
            before.Progress.BossDefeated == after.Progress.BossDefeated &&
            before.Progress.Discoveries.SetEquals(after.Progress.Discoveries),
            "disabling guidance leaves checkpoint, shortcut, tutorials, and discoveries untouched");
        }
        finally { await Dispose(tree, fixture); }
    }

    // These names remain exact retained-suite registry keys. Their assertions follow the
    // continuous-canyon authorities that replaced the former room-specific targets.
    private static void UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete()
    {
        WithCurrentSector(PrologueSectorId.CanyonDescent, sector =>
        {
            GrappleSurface2D valid = sector.GetNode<GrappleSurface2D>(
                "Gameplay/Collision/RiftUpperFloor/GrappleSurfaceProfile");
            GrappleSurface2D rejected = sector.GetNode<GrappleSurface2D>(
                "Gameplay/Collision/FrozenCeiling/GrappleSurfaceProfile");
            TestAssert.True(valid.Material == GrappleSurfaceMaterial.GripStone &&
                            rejected.Material == GrappleSurfaceMaterial.Ice &&
                            valid.GetInstanceId() != rejected.GetInstanceId(),
                "current Descent keeps valid rift support distinct from the rejected Ice route");
        });
    }

    private static void UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply()
    {
        WithCurrentSector(PrologueSectorId.CanyonDescent, sector =>
        {
            Marker2D seam = sector.GetNode<Marker2D>("Gameplay/RouteMarkers/NextSeam");
            foreach (Area2D area in sector.GetNode("Gameplay/FallRecovery").GetChildren()
                         .OfType<Area2D>())
            {
                var shape = area.GetNode<CollisionShape2D>("Collision").Shape as RectangleShape2D;
                Rect2 bounds = new(area.Position - shape.Size / 2f, shape.Size);
                TestAssert.True(!bounds.HasPoint(seam.Position),
                    "current Descent recovery authority cannot consume the Basin seam target");
            }
        });
    }

    private static void UpperAnchorLowFailedCastClearsPendingApply()
    {
        WithCurrentSector(PrologueSectorId.CanyonDescent, sector =>
        {
            StaticBody2D ice = sector.GetNode<StaticBody2D>("Gameplay/Collision/FrozenCeiling");
            GrappleSurface2D profile = ice.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            CollisionShape2D collision = ice.GetNode<CollisionShape2D>("Collision");
            TestAssert.True(profile.Material == GrappleSurfaceMaterial.Ice &&
                            ice.Visible && ice.CollisionLayer != 0 && !collision.Disabled,
                "current Ice rejection remains visible, typed and physically queryable");
        });
    }

    private static void UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff()
    {
        WithCurrentSector(PrologueSectorId.CanyonDescent, sector =>
        {
            TraversalSegment2D segment = Descendants<TraversalSegment2D>(sector).Single(item =>
                item.SegmentId == new StringName("canyon.v2.route.rift-to-abyss"));
            IReadOnlyList<string> errors = segment.Validate(sector);
            GD.Print($"RIFT_LANDING_CONTRACT_FACTS id={segment.SegmentId} " +
                     $"kind={segment.Kind} kind_value={(int)segment.Kind} " +
                     $"abilities={segment.RequiredAbilities} " +
                     $"valid={errors.Count == 0} landing_area={segment.LandingZone.HasArea()} " +
                     $"launch={segment.LaunchZone} landing={segment.LandingZone} " +
                     $"validation_errors={string.Join("|", errors)} structural_only=true");
            TestAssert.True(errors.Count == 0 &&
                            segment.Kind == TraversalSegmentKind.GroundTransfer &&
                            segment.RequiredAbilities == TraversalAbility.None &&
                            segment.LandingZone.HasArea(),
                "current rift-to-abyss traversal keeps a complete legal landing contract");
        });
    }

    private static void ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt()
    {
        WithCurrentSector(PrologueSectorId.CanyonAscent, sector =>
        {
            Marker2D[] markers =
            [
                sector.GetNode<Marker2D>("Gameplay/MicroScenes/CounterweightLift"),
                sector.GetNode<Marker2D>("Gameplay/MicroScenes/PendulumGallery"),
                sector.GetNode<Marker2D>("Gameplay/MicroScenes/SplitLevelEncounter")
            ];
            string[] mainRouteIds =
            [
                "canyon.v2.route.lift-to-pendulums",
                "canyon.v2.route.pendulums-to-split-level",
                "canyon.v2.route.split-level-to-courtyard"
            ];
            TraversalSegment2D[] contracts = Descendants<TraversalSegment2D>(sector)
                .Where(segment => mainRouteIds.Contains(segment.SegmentId.ToString(),
                    System.StringComparer.Ordinal)).ToArray();
            TestAssert.True(markers.Select(marker => marker.GetInstanceId()).Distinct().Count() == 3 &&
                            markers.Select(marker => marker.Position).Distinct().Count() == 3 &&
                            contracts.Length == 3 &&
                            contracts.All(contract => contract.Validate(sector).Count == 0 &&
                                contract.LandingZone.HasArea()) &&
                            contracts.Select(contract => contract.LandingZone).Distinct().Count() == 3,
                "three current Ascent challenges retain unique mounted targets and legal landing contracts");
        });
    }

    private static void ExpandedTeachingAppearsOnlyForAllowedTriggers()
    {
        WithCurrentSector(PrologueSectorId.CanyonEntrance, sector =>
        {
            CanyonEncounterDefinition encounter = CanyonEncounterCatalog.Normal.Single(item =>
                item.RouteNode == CanyonRouteNodeId.LowCeilingBridge);
            TestAssert.Equal(1, encounter.Enemies.Count(enemy =>
                    enemy.ScenePath == CanyonEncounterCatalog.RustShellScene),
                "Entrance expanded-teaching pressure remains authored as one living Rust-Shell");
            TestAssert.Equal(0, Descendants<Knight.Enemies.RustShellSquire>(sector).Count(),
                "the static EntranceCombat marker is never mistaken for the living Rust-Shell actor");
            TestAssert.Equal(1, Descendants<TraversalSegment2D>(sector).Count(),
                "Entrance retains one focused first-act traversal contract");
        });
    }

    private static void RecoveryCountsWithinOneRoomAndResetsOnTransition()
    {
        int recoveryAuthorities = 0;
        foreach (PrologueSectorId id in System.Enum.GetValues<PrologueSectorId>())
            WithCurrentSector(id, sector =>
            {
                if (sector.GetNodeOrNull<Marker2D>("Gameplay/FallRecovery/RecoveryLanding") != null)
                    recoveryAuthorities++;
            });
        TestAssert.Equal(5, recoveryAuthorities,
            "each current streamed sector owns one explicit recovery landing authority");
    }

    private static void CrossCheckpointRetriesRestoreTheFailedChallengeStreak()
    {
        WithCurrentSector(PrologueSectorId.CanyonBasin, basin =>
        {
            CheckpointTrigger checkpoint = basin.GetNode<CheckpointTrigger>("Gameplay/BasinCheckpoint");
            TestAssert.Equal(new StringName("canyon.v2.basin-camp"), checkpoint.EntranceId,
                "Basin checkpoint retains its stable continuous-canyon identity");
        });
        WithCurrentSector(PrologueSectorId.LostTemple, temple =>
        {
            CheckpointTrigger checkpoint = temple.GetNode<CheckpointTrigger>("Gameplay/TempleCheckpoint");
            TestAssert.Equal(new StringName("canyon.v2.temple-forecourt"), checkpoint.EntranceId,
                "Temple checkpoint retains its stable continuous-canyon identity");
        });
    }

    private static void RealCriticalPathRetargetsThroughShortcutAndBossStates()
    {
        WithCurrentSector(PrologueSectorId.LostTemple, sector =>
        {
            float mechanismX = sector.GetNode<BellChainMechanism>(
                "Gameplay/Mechanisms/BellChain").Position.X;
            float triggerX = sector.GetNode<Area2D>("Gameplay/ActivationTrigger").Position.X;
            TestAssert.True(mechanismX < triggerX &&
                            sector.GetNodeOrNull<BossArenaController>(
                                "Gameplay/BossArenaController") != null,
                "current critical path orders BellChain and seal authority before Boss activation");
        });
    }

    private static void ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels()
    {
        WithCurrentSector(PrologueSectorId.LostTemple, sector =>
        {
            BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
                "Gameplay/Mechanisms/BellChain");
            CollisionShape2D door = chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath);
            TestAssert.True(chain.HasSignal(BellChainMechanism.SignalName.Opened) &&
                            !chain.IsOpen && !door.Disabled,
                "fresh current BellChain exposes its opening signal and starts physically closed");
        });
    }

    private static void MissingTargetKeepsInstructionAndWarnsOnce()
    {
        var sectors = System.Enum.GetValues<PrologueSectorId>()
            .ToDictionary(id => id, LoadCurrentSector);
        try
        {
            foreach (PrologueSector sector in sectors.Values)
            foreach (TraversalSegment2D segment in Descendants<TraversalSegment2D>(sector))
            {
                TestAssert.Equal(0, segment.Validate(sector).Count,
                    $"current traversal contract {segment.SegmentId} remains fully valid");
                if (segment.Kind != TraversalSegmentKind.Assisted) continue;
                TestAssert.True(!segment.AssistTargetPath.IsEmpty &&
                                sector.GetNodeOrNull<GrappleSurface2D>(
                                    segment.AssistTargetPath) != null,
                    $"current assisted contract {segment.SegmentId} resolves its GrappleSurface2D target");
            }
            foreach (CanyonRouteNodeDefinition node in CanyonWorldLayout.Nodes)
                TestAssert.True(sectors[node.SectorId].GetNodeOrNull<Marker2D>(
                        $"Gameplay/MicroScenes/{node.Id}") != null,
                    $"current route node {node.Id} has a mounted guidance marker");
            foreach (CanyonRouteEdge edge in CanyonWorldLayout.Edges)
            {
                TestAssert.True(sectors.Values.SelectMany(Descendants<TraversalSegment2D>)
                        .Any(segment => segment.SegmentId == new StringName(edge.TraversalSegmentId)),
                    $"current route edge {edge.TraversalSegmentId} has no missing traversal contract");
                if (!string.IsNullOrEmpty(edge.ReverseTraversalSegmentId))
                    TestAssert.True(sectors.Values.SelectMany(Descendants<TraversalSegment2D>)
                            .Any(segment => segment.SegmentId ==
                                            new StringName(edge.ReverseTraversalSegmentId)),
                        $"current reverse edge {edge.ReverseTraversalSegmentId} has no missing contract");
            }
        }
        finally
        {
            foreach (PrologueSector sector in sectors.Values) sector.Free();
        }
    }

    private static IEnumerable<T> Descendants<T>(Node root) where T : Node =>
        root.FindChildren("*", null, true, false).OfType<T>();

    private static PrologueSector LoadCurrentSector(PrologueSectorId id) =>
        GD.Load<PackedScene>($"res://scenes/prologue/sectors/{id}.tscn")
            .Instantiate<PrologueSector>();

    private static void WithCurrentSector(PrologueSectorId id, System.Action<PrologueSector> assertion)
    {
        PrologueSector sector = LoadCurrentSector(id);
        try { assertion(sector); }
        finally { sector.Free(); }
    }

    private static async Task<Fixture> LoadFixture(SceneTree tree, string name)
    {
        PrologueLevelController level = CreateLevelFixture(name);
        try
        {
            string stateRoot = Path.GetDirectoryName(level.GetNode<PrologueGameFlowController>("GameFlow").SavePath)!;
            tree.Root.AddChild(level);
            for (int index = 0; index < 3; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            return new Fixture(level, level.GetNode<PrologueGuidanceController>("Guidance"),
                level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot"), stateRoot);
        }
        catch
        {
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
            throw;
        }
    }

    internal static PrologueLevelController CreateLevelFixture(string name)
    {
        _levelScene ??= GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = _levelScene.Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        IsolatedPrologueFixture.Configure(level, "guidance-" + name);
        return level;
    }

    private static async Task Dispose(SceneTree tree, Fixture fixture)
    {
        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, fixture.Level);
    }

    private static async Task SettleRecovery(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private sealed record Fixture(PrologueLevelController Level,
        PrologueGuidanceController Controller, WorldTargetMarkerVisual Visual, string StateRoot);
}
