using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Prologue;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class FallenBellkeeperRuntimeTests
{
    private const int TransitionToHammerThrownBudgetFrames = 168;
    private const int PhaseTwoToHammerThrownBudgetFrames = 92;
    private static readonly Lazy<PackedScene> BossScene = new(() =>
        GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn"));
    public static async Task Run(SceneTree tree)
    {
        await FormalBossRunsArmorChainAndDefeatContract(tree);
        await GeneratedMainBossRetryUsesCurrentInputMap(tree);
    }

    private static async Task FormalBossRunsArmorChainAndDefeatContract(SceneTree tree)
    {
        FallenBellkeeper boss = BossScene.Value.Instantiate<FallenBellkeeper>();
        boss.Name = "BossRuntimeFixture";
        tree.Root.AddChild(boss);
        await Frames(tree, 2);
        boss.ActivateEncounter();
        await Frames(tree, 65);
        TestAssert.Equal(FallenBellkeeperState.PhaseOne, boss.CurrentFrame.State,
            "real Boss completes Intro into phase one");
        TestAssert.True(!boss.CurrentFrame.ArmorClosed, "phase one accepts direct health damage");
        HealthComponent health = boss.GetNode<HealthComponent>("Health");
        Hurtbox hurtbox = boss.GetNode<Hurtbox>("Hurtbox");
        health.ApplyDamage(new DamageRequest(8, Vector2.Right, 0f, 10001UL));
        await Frames(tree, 1);
        TestAssert.Equal(FallenBellkeeperState.Transition, boss.CurrentFrame.State,
            "real Boss enters transition at or below sixty percent");
        TestAssert.True(!boss.CurrentFrame.AttackActive, "real transition is non-damaging");
        await Frames(tree, 75);
        TestAssert.Equal(FallenBellkeeperState.PhaseTwoArmored, boss.CurrentFrame.State,
            "real Boss completes transition into armored phase");

        int armoredBefore = health.CurrentHealth;
        for (ulong id = 1; id <= 3; id++)
            hurtbox.Receive(new DamageRequest(1, Vector2.Right, 0f, 10100UL + id));
        TestAssert.Equal(armoredBefore - 1, health.CurrentHealth,
            "closed Boss armor applies the shared three-to-one accumulator");
        bool reachedHammer = await WaitForBossState(tree, boss,
            FallenBellkeeperState.HammerThrown, PhaseTwoToHammerThrownBudgetFrames);
        TestAssert.True(reachedHammer,
            "real Boss exposes HammerThrown within the preserved 0.65-second dwell plus 0.85-second telegraph budget");
        TestAssert.True(boss.CurrentFrame.ChainTargetActive &&
            boss.GetNode<ThreadTarget2D>("ChainTarget").IsInGroup("thread_targets"),
            "only thrown hammer publishes the chain target");
        TestAssert.True(boss.TryReceiveThread(new ThreadInteraction(null, Vector2.Zero,
            boss.GlobalPosition, new ThreadAbilitySettings())), "thrown chain accepts thread interaction");
        await Frames(tree, 1);
        TestAssert.Equal(FallenBellkeeperState.Exposed, boss.CurrentFrame.State,
            "thread interaction opens real exposed state");
        TestAssert.True(!boss.CurrentFrame.ArmorClosed &&
            !boss.GetNode<ThreadTarget2D>("ChainTarget").IsInGroup("thread_targets"),
            "exposure hides chain target and bypasses armor");
        int exposedBefore = health.CurrentHealth;
        hurtbox.Receive(new DamageRequest(1, Vector2.Right, 0f, 10200UL));
        TestAssert.Equal(exposedBefore - 1, health.CurrentHealth, "exposed hit deals full damage");
        bool defeated = false;
        boss.Defeated += () => defeated = true;
        hurtbox.Receive(new DamageRequest(99, Vector2.Right, 0f, 10300UL));
        await Frames(tree, 1);
        TestAssert.True(defeated && boss.CurrentFrame.State == FallenBellkeeperState.Defeated,
            "player-deliverable final damage emits defeated and leaves Boss inert");
        await Dispose(tree, boss);
    }

    private static async Task GeneratedMainBossRetryUsesCurrentInputMap(SceneTree tree)
    {
        // The old six-room fixture no longer exists in the current five-sector generated main.
        // This is the real production player path: it exercises the current Q/E/mouse InputMap,
        // both checkpoints, physical Boss death/retry, and a player-delivered completion strike.
        DisplayServer.WindowSetSize(new Vector2I(1280, 720));
        tree.Root.Size = new Vector2I(1280, 720);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await PrologueTask9GeneratedMainTests.Run(tree, captureFrames: false);
        GD.Print("FALLEN_BELLKEEPER_RUNTIME_GENERATED_MAIN_OK input_map=True retry=True completion=True");
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task<bool> WaitForBossState(SceneTree tree, FallenBellkeeper boss,
        FallenBellkeeperState state, int frameBudget)
    {
        if (boss.CurrentFrame.State == state) return true;
        for (int frame = 0; frame < frameBudget; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            if (boss.CurrentFrame.State == state) return true;
        }
        return false;
    }

    private static async Task Dispose(SceneTree tree, Node node)
    {
        foreach (StringName action in new[]
                 { InputActions.MoveRight, InputActions.MoveUp, InputActions.Jump,
                     InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
                     InputActions.PrimaryAction, InputActions.Shield, InputActions.Slot1 })
            Input.ActionRelease(action);
        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, node);
    }
}
