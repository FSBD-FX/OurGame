using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;

namespace Knight.Tests.Integration;

// Real saved level and physical keys. ReturnToTitle is a disclosed Flow API
// fixture boundary: there is no playing -> title button in the current menu.
// Lethal damage is controlled, not a natural combat/whole-route proof.
public static class FormalInputBoundaryTests
{
    private static readonly Key[] EdgeKeys = [Key.J, Key.K, Key.Q, Key.Shift, Key.Key2];

    public static async Task Run(SceneTree tree)
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        var level = scene.Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level, "formal-input-boundaries");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        tree.Root.AddChild(level);
        var input = level.GetNode<PlayerInputReader>("PlayerRoot/Player/Input");
        var combat = level.GetNode<PlayerCombat>("PlayerRoot/Player/Combat");
        var claw = level.GetNode<MechanicalClawController>("PlayerRoot/Player/MechanicalClaw");
        var motor = level.GetNode<PlayerMotor>("PlayerRoot/Player/Motor");
        var modes = level.GetNode<PlayerActionModeController>("PlayerRoot/Player/ActionModes");
        var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
        var firstSample = new FormalInputSampleObserver
        {
            ProcessMode = Node.ProcessModeEnum.Always, ProcessPhysicsPriority = -299
        };
        level.AddChild(firstSample);
        int attacks = 0;
        int recovered = 0;
        combat.AttackStarted += (_, _) => attacks++;
        level.RecoveryCompleted += _ => recovered++;
        try
        {
            await ProcessFrames(tree, 5);
            TestAssert.Equal(-300, input.ProcessPhysicsPriority,
                "test observers bracket the actual saved Input priority without changing it");
            TestAssert.True(flow.State == PrologueGameFlowState.Title &&
                level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").HasFocus() &&
                !input.IsPhysicsProcessing(), "formal boundary fixture begins at the actual focused Title");
            await MenuPulse(tree, Key.Enter);
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Player.IsOnFloor(),
                "physical Enter starts the real supported player without calling StartNew");

            // Walk to the original entrance fragment, not a metadata grant.
            SendKey(Key.D, true);
            for (int frame = 0; frame < 50 && level.Progress.CollectedFragmentIds.Count == 0; frame++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            SendKey(Key.D, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(level.Player.IsOnFloor() && level.Progress.CollectedFragmentIds.SetEquals(
                    new[] { "canyon.v2.fragment.entrance" }),
                "normal physical D collects exactly the actual entrance fragment on original support");
            AssertSavedFragment(paths.SavePath);

            foreach (Key heldKey in EdgeKeys)
            {
                await MenuPulse(tree, Key.Escape);
                TestAssert.True(flow.State == PrologueGameFlowState.Paused && pause.IsOpen && tree.Paused &&
                    level.GetNode<Button>("PauseMenu/Panel/Resume").HasFocus(),
                    "physical Esc opens the real pause menu/Flow and focuses Resume");
                ulong stopped = input.CurrentFrameSequence;
                await ProcessFrames(tree, 2);
                TestAssert.Equal(stopped, input.CurrentFrameSequence, "formal pause stops actual input sampling");
                int priorAttacks = attacks;
                int priorSlot = modes.ActiveSlot;
                int priorDashes = motor.AcceptedDashCount;
                firstSample.Arm(input);
                SendKey(heldKey, true);
                SendKey(Key.Escape, true);
                await ProcessFrames(tree, 1);
                SendKey(Key.Escape, false);
                await ObserveFirstSample(tree, firstSample);
                TestAssert.True(flow.State == PrologueGameFlowState.Playing && !pause.IsOpen && !tree.Paused,
                    "second physical Esc resumes through the formal pause owner");
                AssertNoEdges(firstSample.FirstFrame, "first formal Esc resume sample " + heldKey);
                TestAssert.True(attacks == priorAttacks && modes.ActiveSlot == priorSlot &&
                    motor.AcceptedDashCount == priorDashes && claw.CurrentFrame.State == MechanicalClawState.Idle,
                    "held pause command has no deferred attack, claw, dash or loadout effect");
                GD.Print($"FORMAL_INPUT_PAUSE_FIRST key={heldKey} frame={firstSample.FirstSequence} edges=0");
                SendKey(heldKey, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 4);
            }
            await FreshAttack(tree, input, () => attacks);
            await KeyboardActionRuntimeTests.Frames(tree, input, 35);

            foreach (Key heldKey in EdgeKeys)
            {
                flow.ReturnToTitle(); // Explicit API boundary, not invented menu input.
                await ProcessFrames(tree, 3);
                TestAssert.True(flow.State == PrologueGameFlowState.Title && flow.HasSave,
                    "controlled Title boundary retains the real isolated Continue slot");
                await MenuPulse(tree, Key.Down);
                TestAssert.True(level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").HasFocus(),
                    "physical Down selects the actual Continue button");
                string savedHash = Hash(paths.SavePath);
                int priorAttacks = attacks;
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                ulong beforeContinueSample = input.CurrentFrameSequence;
                SendKey(heldKey, true);
                TestAssert.True(Input.IsActionJustPressed(ActionFor(heldKey)),
                    "the held Title command is a fresh real edge immediately before Continue");
                SendKey(Key.Enter, true);
                SendKey(Key.Enter, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                TestAssert.Equal(beforeContinueSample + 1, input.CurrentFrameSequence,
                    "Continue assertion observes exactly the first resumed input sample");
                GD.Print($"FORMAL_INPUT_CONTINUE_FIRST key={heldKey} frame={input.CurrentFrameSequence} " +
                    $"attack={input.Current.AttackPressed} claw={input.Current.ClawPressed} " +
                    $"q={input.Current.InteractPressed} dash={input.Current.DashPressed} " +
                    $"slot={input.Current.RequestedSlot} attacks={attacks - priorAttacks}");
                TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "physical Enter executes real Continue");
                AssertNoEdges(input.Current, "first Continue sample " + heldKey);
                TestAssert.True(attacks == priorAttacks && modes.ActiveSlot == 1 &&
                    claw.CurrentFrame.State == MechanicalClawState.Idle && motor.AcceptedDashCount == 0,
                    "Title commands do not replay through Continue into combat, claw, dash or equipment");
                SendKey(heldKey, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 6);
                TestAssert.Equal(savedHash, Hash(paths.SavePath), "real Continue does not rewrite its source save");
                AssertSavedFragment(paths.SavePath);
                TestAssert.True(level.Progress.CollectedFragmentIds.SetEquals(
                    new[] { "canyon.v2.fragment.entrance" }) && !level.Progress.LegacyCollectionQuotaExempt,
                    "real keyboard Continue restores the honest contact-collected fragment without quota exemption");
            }
            await FreshAttack(tree, input, () => attacks);
            await KeyboardActionRuntimeTests.Frames(tree, input, 35);

            var health = level.Player.GetNode<HealthComponent>("Health");
            foreach (Key heldKey in EdgeKeys)
            {
                int priorRecoveries = recovered;
                int attacksAtDeath = attacks;
                firstSample.Arm(input);
                SignalAwaiter recoveryCompleted = tree.ToSignal(level,
                    PrologueLevelController.SignalName.RecoveryCompleted);
                TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f,
                    660001UL + (ulong)priorRecoveries, level.Player.GetInstanceId(), DamageGuardClass.Unblockable)),
                    "controlled lethal hit enters the actual health/death/recovery pipeline");
                TestAssert.Equal(PrologueGameFlowState.Recovering, flow.State,
                    "real death stops gameplay in Recovering before deferred relocation");
                TestAssert.True(!input.IsPhysicsProcessing(), "actual death disables input before key staging");
                await recoveryCompleted;
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(!input.IsPhysicsProcessing(),
                    "recovery edge is staged during the last disabled process boundary, not inside physics");
                ulong stagedSequence = input.CurrentFrameSequence;
                SendKey(heldKey, true);
                bool freshEdgeAtRecovery = Input.IsActionJustPressed(ActionFor(heldKey));
                await ObserveFirstSample(tree, firstSample);
                GD.Print($"FORMAL_INPUT_RECOVERY_STAGE key={heldKey} fresh={freshEdgeAtRecovery} " +
                    $"staged_seq={stagedSequence} " +
                    $"before={firstSample.BeforeSequence} first={firstSample.FirstSequence} " +
                    $"current={input.CurrentFrameSequence} attack={firstSample.FirstFrame.AttackPressed}");
                TestAssert.True(freshEdgeAtRecovery && stagedSequence == firstSample.BeforeSequence,
                    "controlled physical recovery key is fresh immediately before the first restored sample");
                AssertNoEdges(firstSample.FirstFrame, "first real death recovery sample " + heldKey);
                TestAssert.True(attacks == attacksAtDeath && modes.ActiveSlot == 1 &&
                    motor.AcceptedDashCount == 0 && claw.CurrentFrame.State == MechanicalClawState.Idle,
                    "first recovery command has no deferred attack, claw, dash or loadout effect");
                GD.Print($"FORMAL_INPUT_RECOVERY_FIRST key={heldKey} frame={firstSample.FirstSequence} " +
                    "staging=controlled_physical_edge edges=0");
                SendKey(heldKey, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 8);
                TestAssert.True(recovered == priorRecoveries + 1 && attacks == attacksAtDeath &&
                    flow.State == PrologueGameFlowState.Playing && health.CurrentHealth > 0 &&
                    level.Player.IsOnFloor() && level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.start" &&
                    level.Player.GlobalPosition.DistanceTo(new Vector2(160f, 600f)) < 40f &&
                    level.Progress.CollectedFragmentIds.SetEquals(new[] { "canyon.v2.fragment.entrance" }),
                    "first-checkpoint death restores original support, keeps the fragment and never replays commands");
            }
            await FreshAttack(tree, input, () => attacks);
            AssertSavedFragment(paths.SavePath);
            GD.Print("FORMAL_INPUT_BOUNDARIES_OK physical_title_start=true physical_esc=true physical_continue=true " +
                "title_return=controlled_flow_api death=controlled_lethal contact_fragment=true normal_route=false");
        }
        finally
        {
            foreach (Key key in new[] { Key.D, Key.J, Key.K, Key.Q, Key.Shift, Key.Key2, Key.Escape, Key.Enter, Key.Down })
                SendKey(key, false);
            tree.Paused = false;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            GC.KeepAlive(scene);
        }
    }

    private static void AssertNoEdges(PlayerInputFrame frame, string boundary) => TestAssert.True(
        !frame.AttackPressed && !frame.ClawPressed && !frame.InteractPressed && !frame.DashPressed &&
        !frame.JumpPressed && frame.RequestedSlot == 0, boundary + " consumes all old one-shot commands");

    private static async Task ObserveFirstSample(SceneTree tree, FormalInputSampleObserver observer)
    {
        for (int frame = 0; frame < 120 && !observer.Captured; frame++) await ProcessFrames(tree, 1);
        TestAssert.True(observer.Captured, "first restored physical input sample was observed");
        TestAssert.Equal(observer.BeforeSequence + 1, observer.FirstSequence,
            "test snapshot records exactly the first restored input sample before action consumers");
    }

    private static async Task FreshAttack(SceneTree tree, PlayerInputReader input, Func<int> count)
    {
        int before = count();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        SendKey(Key.J, true);
        await KeyboardActionRuntimeTests.Frames(tree, input, 3);
        SendKey(Key.J, false);
        TestAssert.Equal(before + 1, count(), "fresh J after boundary is accepted exactly once");
    }

    private static StringName ActionFor(Key key) => key switch
    {
        Key.J => InputActions.Attack, Key.K => InputActions.Claw, Key.Q => InputActions.Interact,
        Key.Shift => InputActions.Dash, Key.Key2 => InputActions.Slot2,
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };
    private static void SendKey(Key key, bool pressed) => KeyboardActionRuntimeTests.KeyEvent(key, pressed);
    private static async Task MenuPulse(SceneTree tree, Key key)
    {
        SendKey(key, true);
        await ProcessFrames(tree, 1);
        SendKey(key, false);
        await ProcessFrames(tree, 1);
    }
    private static async Task ProcessFrames(SceneTree tree, int count)
    { for (int frame = 0; frame < count; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void AssertSavedFragment(string path)
    {
        var saved = Knight.Save.PrologueSaveStore.LoadOrNew(path, 2);
        TestAssert.True(!saved.WasRecovered && saved.CollectedFragmentIds.Count == 1 &&
            saved.CollectedFragmentIds.Contains("canyon.v2.fragment.entrance") && !saved.LegacyCollectionQuotaExempt &&
            saved.CheckpointId == "canyon.v2.start", "real source save keeps the contact fragment and original checkpoint");
    }
}
