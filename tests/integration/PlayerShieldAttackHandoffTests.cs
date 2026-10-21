using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PlayerShieldAttackHandoffTests
{
    public static async Task Run(SceneTree tree)
    {
        // RED: stale Shield.IsActive must not reject a fresh attack edge whose same
        // automatically sampled input already says ShieldHeld=false.
        await WithFixture(tree, "ReleaseAndFreshPress", async fixture =>
        {
            await RaiseShield(tree, fixture);
            BatchSnapshot batch = await Dispatch(tree, fixture,
                (InputActions.Shield, false), (InputActions.PrimaryAction, true));
            await Samples(tree, fixture, 2);
            TestAssert.Equal(1, fixture.Attacks.Count,
                "same input batch releasing an active shield and freshly pressing primary starts one attack");
            AttackSample accepted = fixture.Attacks[0];
            TestAssert.Equal(batch.InputSequence + 1, accepted.InputSequence,
                "shield-to-attack handoff is accepted on the first automatic input sampling tick");
            TestAssert.True(!accepted.ShieldHeld && accepted.PrimaryPressed,
                "accepted handoff observes released shield and a new primary edge from real InputMap events");
            TestAssert.Near(12f, batch.Energy - accepted.Energy, .1f,
                "the accepted handoff spends one attack's energy, not a delayed or duplicate attack");
            TestAssert.True(!fixture.Shield.IsActive, "automatic shield processing completes the release");
        });
        await WithFixture(tree, "HeldShieldRejectsNewPress", async fixture =>
        {
            await RaiseShield(tree, fixture);
            BatchSnapshot batch = await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));
            await Samples(tree, fixture, 2);
            TestAssert.True(fixture.Shield.IsActive && fixture.Input.Current.ShieldHeld,
                "continued RMB input retains the real active shield");
            TestAssert.Equal(0, fixture.Attacks.Count, "a new LMB edge while an active shield stays held does not attack");
            TestAssert.Equal(PlayerActionRejectionReason.ShieldActive, fixture.Modes.LastRejection,
                "held active shield rejects the primary edge for the actual shield reason");
            TestAssert.True(batch.Energy - fixture.EnergyAmount < 2f,
                "rejected attack spends no 12-energy charge beyond two ticks of ordinary shield drain");
        });
        await WithFixture(tree, "OldPrimaryEdgeDoesNotReplay", async fixture =>
        {
            await RaiseShield(tree, fixture);
            await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));
            await Samples(tree, fixture, 2);
            TestAssert.Equal(0, fixture.Attacks.Count, "the original primary edge is rejected while shielding");
            BatchSnapshot batch = await Dispatch(tree, fixture, (InputActions.Shield, false)); // primary stays held
            await Samples(tree, fixture, 4);
            TestAssert.True(!fixture.Shield.IsActive && fixture.Input.Current.PrimaryHeld && !fixture.Input.Current.PrimaryPressed,
                "real sampling sees shield down and only an old held primary input");
            TestAssert.Equal(0, fixture.Attacks.Count, "releasing shield never replays an earlier rejected primary edge");
            TestAssert.Near(batch.Energy, fixture.EnergyAmount, .1f,
                "release without a fresh attack edge spends no attack energy");
        });
        await WithFixture(tree, "OverheatedShieldDoesNotBlockAttack", async fixture =>
        {
            await RaiseShield(tree, fixture);
            for (int tick = 0; tick < 300 && !fixture.Energy.IsOverheated; tick++)
                await Samples(tree, fixture, 1);
            TestAssert.True(fixture.Energy.IsOverheated && !fixture.Shield.IsActive && fixture.Input.Current.ShieldHeld,
                "real sustained RMB naturally overheats and closes the shield while the button remains held");
            float beforeEnergy = fixture.EnergyAmount;
            TestAssert.True(beforeEnergy >= 12f, "natural overheat fixture still has enough actual energy for an attack");
            BatchSnapshot batch = await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));
            await Samples(tree, fixture, 2);
            TestAssert.Equal(1, fixture.Attacks.Count, "held RMB cannot block a fresh affordable attack after shield overheat");
            AttackSample accepted = fixture.Attacks[0];
            TestAssert.True(accepted.ShieldHeld && accepted.PrimaryPressed && !accepted.ShieldActive,
                "overheat acceptance uses the real inactive guard despite ongoing RMB input");
            TestAssert.Equal(batch.InputSequence + 1, accepted.InputSequence, "overheat attack uses the first fresh input tick");
            TestAssert.Near(12f, batch.Energy - accepted.Energy, .1f, "overheat attack spends exactly one attack charge");
        });
        GD.Print("PLAYER_SHIELD_ATTACK_HANDOFF_OK");
    }

    private static async Task RaiseShield(SceneTree tree, Fixture fixture)
    {
        await Dispatch(tree, fixture, (InputActions.Shield, true));
        for (int tick = 0; tick < 8 && !fixture.Shield.IsActive; tick++) await Samples(tree, fixture, 1);
        TestAssert.True(fixture.Shield.IsActive && fixture.Energy.IsShieldSustained,
            "real InputReader/Shield/Energy processing establishes a sustained shield before the handoff");
    }

    private static async Task WithFixture(SceneTree tree, string name, Func<Fixture, Task> test)
    {
        await ReleaseAll(tree);
        var root = new Node2D { Name = "ShieldHandoff" + name };
        var floor = new StaticBody2D
        {
            Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(800, 20) } });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14);
        root.AddChild(player);
        tree.Root.AddChild(root);
        var fixture = new Fixture(player);
        try
        {
            await Samples(tree, fixture, 3);
            await Dispatch(tree, fixture, (InputActions.SelectAttackMode, true));
            await Samples(tree, fixture, 1);
            await Dispatch(tree, fixture, (InputActions.SelectAttackMode, false));
            await Samples(tree, fixture, 1);
            TestAssert.Equal(PlayerActionMode.Attack, fixture.Modes.CurrentMode, "real Q input selects attack mode");
            await test(fixture);
        }
        finally
        {
            fixture.Dispose();
            await ReleaseAll(tree);
            root.QueueFree();
            for (int tick = 0; tick < 2; tick++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task Samples(SceneTree tree, Fixture fixture, int count)
    {
        ulong target = fixture.Input.CurrentFrameSequence + (ulong)count;
        for (int tick = 0; tick < count + 4 && fixture.Input.CurrentFrameSequence < target; tick++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(fixture.Input.CurrentFrameSequence >= target,
            "the real automatic InputReader must advance; no manual physics or sampled-frame injection is used");
    }

    private static async Task<BatchSnapshot> Dispatch(SceneTree tree, Fixture fixture,
        params (StringName Action, bool Pressed)[] changes)
    {
        // PhysicsFrame is emitted before physics callbacks: dispatching there can
        // change Held immediately while JustPressed lands in a later sampling tick.
        // ProcessFrame is outside that physics step, matching the normal-window probe.
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var before = new BatchSnapshot(fixture.Input.CurrentFrameSequence, fixture.EnergyAmount);
        fixture.LastBatchSequence = before.InputSequence;
        GD.Print($"SHIELD_HANDOFF_BATCH process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()} " +
            $"before_sample={before.InputSequence} shield_active={fixture.Shield.IsActive} energy={before.Energy:0.00} " +
            $"changes={string.Join(", ", changes)}");
        SendBatch(changes);
        return before;
    }

    private static void SendBatch(params (StringName Action, bool Pressed)[] changes)
    {
        foreach (var change in changes)
            Input.ParseInputEvent(new InputEventAction
            { Action = change.Action, Pressed = change.Pressed, Strength = change.Pressed ? 1f : 0f });
        Input.FlushBufferedEvents();
    }

    private static async Task ReleaseAll(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        foreach (StringName action in new[] { InputActions.Shield, InputActions.PrimaryAction,
            InputActions.SelectAttackMode, InputActions.SelectGrappleMode, InputActions.MoveLeft,
            InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown, InputActions.Jump, InputActions.Dash })
            SendBatch((action, false));
    }

    private readonly record struct BatchSnapshot(ulong InputSequence, float Energy);
    private readonly record struct AttackSample(ulong InputSequence, bool ShieldHeld, bool PrimaryPressed,
        bool ShieldActive, float Energy);

    private sealed class Fixture : IDisposable
    {
        public PlayerInputReader Input { get; }
        public PlayerActionModeController Modes { get; }
        public PlayerShieldController Shield { get; }
        public PlayerEnergyController Energy { get; }
        public PlayerCombat Combat { get; }
        public List<AttackSample> Attacks { get; } = new();
        public ulong LastBatchSequence { get; set; }
        public float EnergyAmount => Energy.EnergyRatio * Energy.Tuning.MaxEnergy;
        public Fixture(CharacterBody2D player)
        {
            Input = player.GetNode<PlayerInputReader>("Input");
            Modes = player.GetNode<PlayerActionModeController>("ActionModes");
            Shield = player.GetNode<PlayerShieldController>("Shield");
            Energy = player.GetNode<PlayerEnergyController>("Energy");
            Combat = player.GetNode<PlayerCombat>("Combat");
            Combat.AttackStarted += OnAttack;
            Modes.PrimaryRejected += OnRejected;
        }
        private void OnAttack(ulong attackId, int direction)
        {
            Attacks.Add(new AttackSample(Input.CurrentFrameSequence, Input.Current.ShieldHeld,
                Input.Current.PrimaryPressed, Shield.IsActive, EnergyAmount));
            GD.Print($"SHIELD_HANDOFF_ACCEPT id={attackId} batch_before={LastBatchSequence} sample={Input.CurrentFrameSequence} " +
                $"shield_held={Input.Current.ShieldHeld} primary_pressed={Input.Current.PrimaryPressed} " +
                $"shield_active={Shield.IsActive} energy={EnergyAmount:0.00}");
        }
        private void OnRejected(PlayerActionRejectionReason reason) => GD.Print(
            $"SHIELD_HANDOFF_REJECT reason={reason} batch_before={LastBatchSequence} sample={Input.CurrentFrameSequence} " +
            $"shield_held={Input.Current.ShieldHeld} primary_pressed={Input.Current.PrimaryPressed} shield_active={Shield.IsActive}");
        public void Dispose()
        {
            Combat.AttackStarted -= OnAttack;
            Modes.PrimaryRejected -= OnRejected;
        }
    }
}
