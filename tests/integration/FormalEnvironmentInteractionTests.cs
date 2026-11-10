using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Interactions;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class FormalEnvironmentInteractionTests
{
    public static async Task Run(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level, "formal-nearby-valve");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 4);
            flow.StartNew();
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            var player = level.Player;
            var input = player.GetNode<PlayerInputReader>("Input");
            var interaction = player.GetNode<EnvironmentInteractionController>("EnvironmentInteraction");
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            level.SetGameplayActive(false);
            TestAssert.True(!interaction.IsPhysicsProcessing() && interaction.CurrentCandidate == null,
                "the saved level explicitly stops Q alongside Input during inactive gameplay");
            level.SetGameplayActive(true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);

            // Controlled saved-sector fixture, NOT a title-to-Basin route proof.
            // After this initial restore all approach movement is normal physical D.
            var world = level.GetNode<SeamlessPrologueWorld>(level.SeamlessWorldPath);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(3060, 4080)),
                "formal Q fixture enters a legitimate supported Basin checkpoint position");
            await KeyboardActionRuntimeTests.Frames(tree, input, 12);
            TestAssert.True(player.IsOnFloor() && level.CurrentSector == PrologueSectorId.CanyonBasin,
                "the real saved player stands on the original Basin walkway");
            var sector = level.CurrentSectorRoot;
            var target = sector.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamValveControl");
            var valve = sector.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
            var jet = sector.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            var legacy = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            TestAssert.True(player.GlobalPosition.DistanceTo(target.GlobalPosition) > 80f &&
                !valve.IsActivated && !jet.DisabledForRun && interaction.CurrentCandidate == null,
                "Q never advertises or activates the real valve beyond its nearby range");
            await Pulse(tree, input, Key.Q, 3);
            TestAssert.True(!valve.IsActivated, "distant fresh Q leaves the original steam challenge active");
            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            for (int tick = 0; tick < 60 && player.GlobalPosition.X < 3150f; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            // The input sampler precedes Motor. Observe completed physical steps,
            // not a fixed sample count that still leaves authored ground inertia.
            int stableSteps = 0;
            int brakeSteps = 0;
            for (; brakeSteps < 24 && stableSteps < 2; brakeSteps++)
            {
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                bool stopped = player.IsOnFloor() && Mathf.Abs(player.Velocity.X) <= .1f &&
                    Mathf.Abs(player.Velocity.Y) <= .1f && Mathf.Abs(input.Current.MoveAxis) <= .001f;
                stableSteps = stopped ? stableSteps + 1 : 0;
            }
            TestAssert.True(stableSteps == 2,
                "released D naturally brakes to rest across two completed steps without pose or velocity writes");
            GD.Print($"FORMAL_VALVE_BRAKE_OK completed_steps={brakeSteps} stable_steps={stableSteps} " +
                $"velocity={player.Velocity} move_axis={input.Current.MoveAxis} natural=true");
            TestAssert.True(player.IsOnFloor() && player.GlobalPosition.DistanceTo(target.GlobalPosition) <= 80f,
                "normal D walking reaches the supported manual control without teleport or motor freeze");
            var candidate = interaction.CurrentCandidate;
            var manual = target.GetNode<ManualSteamValveControl>("ManualSteamValveControl");
            GD.Print($"FORMAL_VALVE_NEARBY_STATE actor={player.GlobalPosition} velocity={player.Velocity} " +
                $"grounded={player.IsOnFloor()} move_axis={input.Current.MoveAxis} " +
                $"distance={player.GlobalPosition.DistanceTo(target.GlobalPosition)} " +
                $"input_active={input.IsPhysicsProcessing()} interaction_active={interaction.IsPhysicsProcessing()} " +
                $"target_visible={target.IsVisibleInTree()} target_available={target.IsAvailable} " +
                $"material={target.Material} validation_errors={target.Validate().Count} " +
                $"manual_eligible={manual.CanInteract(player)} candidate={candidate?.StableId ?? "none"}");
            TestAssert.True(candidate != null && ReferenceEquals(candidate.InteractionNode, target),
                "the real saved Basin publishes exactly its explicitly opted-in manual valve candidate");
            level.SetLanguage(GameLanguage.English);
            TestAssert.True(interaction.CurrentPromptText == "Q Close steam valve" &&
                ReferenceEquals(candidate, interaction.CurrentCandidate),
                "English live Q prompt explains the one authoritative candidate's actual action");
            level.SetLanguage(GameLanguage.ChineseSimplified);
            TestAssert.Equal("Q 关闭蒸汽阀", interaction.CurrentPromptText,
                "Chinese live Q prompt explains the same actual valve action");
            AssertCurrentInteractionGlyph(level, interaction, candidate);
            Node jetParent = jet.GetParent();
            jetParent.RemoveChild(jet);
            bool missingJetWithdrawsCandidate;
            try { missingJetWithdrawsCandidate = interaction.CurrentCandidate == null; }
            finally { jetParent.AddChild(jet); }
            TestAssert.True(missingJetWithdrawsCandidate,
                "a missing real paired steam jet withdraws manual activation instead of recording a broken mechanism");
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            foreach (var material in new[] { GrappleSurfaceMaterial.Ice, GrappleSurfaceMaterial.Forbidden })
            {
                target.Material = material;
                TestAssert.True(interaction.CurrentCandidate == null,
                    "manual Q never authorizes a disabled or forbidden original target material");
            }
            target.Material = GrappleSurfaceMaterial.DedicatedAnchor;
            target.Hide();
            TestAssert.True(interaction.CurrentCandidate == null, "hidden real control has no manual Q preview");
            target.Show();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Paused = true;
            TestAssert.True(interaction.CurrentCandidate == null, "real paused level withdraws its Q prompt");
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, true);
            await Frames(tree, 2);
            tree.Paused = false;
            await KeyboardActionRuntimeTests.Frames(tree, input, 4);
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(!valve.IsActivated && !jet.DisabledForRun,
                "Q held during real level pause does not activate the valve on resume");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            float energyBefore = energy.EnergyRatio;
            int slotBefore = modes.ActiveSlot;
            await Pulse(tree, input, Key.Q, 20);
            TestAssert.True(valve.IsActivated && jet.DisabledForRun &&
                level.Progress.ActivatedMechanisms.Contains(valve.ActivationId),
                "physical nearby Q activates the real persisted valve and disables its paired steam jet");
            TestAssert.True(interaction.CurrentCandidate == null && legacy.ApplicationCount == 0 &&
                energy.EnergyRatio >= energyBefore && modes.ActiveSlot == slotBefore,
                "Q retires its prompt without forging a K interaction, spending energy or switching equipment");
            valve.ResetForCheckpoint();
            TestAssert.True(valve.IsActivated && jet.DisabledForRun,
                "checkpoint reset preserves the original valve activation ID and paired jet state");
            // Use the existing persistence authority, not a new Q-owned writer.
            flow.PersistProgress("formal-valve-regression");
            TestAssert.True(Knight.Save.PrologueSaveStore.LoadOrNew(paths.SavePath)
                .ActivatedMechanisms.Contains(valve.ActivationId),
                "the original save pipeline retains the same manual valve activation ID on disk");

            flow.ReturnToTitle();
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
                "the first committed session ends before the independent remote-claw case starts");
            flow.StartNew();
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            TestAssert.True(ReferenceEquals(player, level.Player),
                "the second committed session keeps the original input actor");
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(3060, 4080)),
                "independent new session restores the remote-claw diagnostic stance");
            await KeyboardActionRuntimeTests.Frames(tree, input, 12);
            sector = level.CurrentSectorRoot;
            target = sector.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamValveControl");
            valve = sector.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
            jet = sector.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            legacy = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            // On the floor the original angle-first resolver prefers terrain.
            // A normal jump aligns this control with the existing rightward aim;
            // do not change priorities, hide competing terrain or force a target.
            int jumpsBefore = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
            for (int tick = 0; tick < 36 && !ReferenceEquals(claw.KeyboardPreview.Surface, target); tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            GD.Print($"FORMAL_Q_REMOTE_STANCE actor={player.GlobalPosition} target={target.GlobalPosition} " +
                $"grounded={player.IsOnFloor()} velocity={player.Velocity} active={valve.IsActivated} " +
                $"jet_disabled={jet.DisabledForRun} q_candidate={interaction.CurrentCandidate?.StableId ?? "none"} " +
                $"aim={input.KeyboardAimDirection} preview={claw.KeyboardPreview.Surface?.GetPath()} " +
                $"distance={player.GlobalPosition.DistanceTo(target.GlobalPosition)}");
            TestAssert.True(!valve.IsActivated && !jet.DisabledForRun && interaction.CurrentCandidate == null &&
                !player.IsOnFloor() && player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumpsBefore + 1 &&
                player.GlobalPosition.DistanceTo(target.GlobalPosition) > 80f &&
                ReferenceEquals(claw.KeyboardPreview.Surface, target),
                "new game resets the real valve while distant K still previews the original remote control");
            await Pulse(tree, input, Key.K, 3);
            for (int tick = 0; tick < 60 && !valve.IsActivated; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(valve.IsActivated && jet.DisabledForRun && legacy.ApplicationCount == 1 &&
                level.Progress.ActivatedMechanisms.Contains(valve.ActivationId),
                "real K beyond Q range still activates the valve through exactly one original legacy receipt");
            GD.Print("FORMAL_ENVIRONMENT_INTERACTION_OK");
        }
        finally
        {
            foreach (Key key in new[] { Key.W, Key.D, Key.Q, Key.K, Key.Space })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            tree.Paused = false;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static void AssertCurrentInteractionGlyph(PrologueLevelController level,
        EnvironmentInteractionController interaction, IEnvironmentInteractable candidate)
    {
        var original = InputMap.ActionGetEvents(InputActions.Interact);
        using var alternate = new InputEventKey { PhysicalKeycode = Key.F };
        try
        {
            InputMap.ActionEraseEvents(InputActions.Interact);
            InputMap.ActionAddEvent(InputActions.Interact, alternate);
            level.SetLanguage(GameLanguage.English);
            TestAssert.Equal("F Close steam valve", interaction.CurrentPromptText,
                "the same visible valve immediately shows the new English binding");
            level.SetLanguage(GameLanguage.ChineseSimplified);
            TestAssert.Equal("F 关闭蒸汽阀", interaction.CurrentPromptText,
                "the same visible valve immediately shows the new Chinese binding");
            InputMap.ActionEraseEvents(InputActions.Interact);
            TestAssert.Equal("未绑定 关闭蒸汽阀", interaction.CurrentPromptText,
                "an unbound action never advertises the former default Q");
            level.SetLanguage(GameLanguage.English);
            TestAssert.Equal("UNBOUND Close steam valve", interaction.CurrentPromptText,
                "an unbound action uses the existing English binding fallback");
            TestAssert.True(ReferenceEquals(candidate, interaction.CurrentCandidate),
                "displaying another or missing key does not select a different target");
        }
        finally
        {
            InputMap.ActionEraseEvents(InputActions.Interact);
            foreach (InputEvent entry in original) InputMap.ActionAddEvent(InputActions.Interact, entry);
            level.SetLanguage(GameLanguage.ChineseSimplified);
        }
        TestAssert.Equal("Q 关闭蒸汽阀", interaction.CurrentPromptText,
            "the original native Q test sees its restored default prompt");
        GD.Print("FORMAL_INTERACTION_GLYPH_OK languages=2 remapped=true unbound=true restored=true same_candidate=true");
    }

    private static async Task Pulse(SceneTree tree, PlayerInputReader input, Key key, int count)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        KeyboardActionRuntimeTests.KeyEvent(key, true);
        await KeyboardActionRuntimeTests.Frames(tree, input, count);
        KeyboardActionRuntimeTests.KeyEvent(key, false);
        await KeyboardActionRuntimeTests.Frames(tree, input, 2);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int tick = 0; tick < count; tick++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
