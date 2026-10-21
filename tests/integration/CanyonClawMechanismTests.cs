using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue.Mechanisms;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class CanyonClawMechanismTests
{
    private static readonly MechanismCase[] Cases =
    [
        new(CanyonClawMechanismKind.Counterweight, new Vector2(0f, 120f)),
        new(CanyonClawMechanismKind.CargoPlatform, new Vector2(0f, -120f)),
        new(CanyonClawMechanismKind.HiddenPanel, new Vector2(144f, 0f)),
        new(CanyonClawMechanismKind.BellBody, new Vector2(0f, 96f))
    ];

    public static async Task Run(SceneTree tree)
    {
        foreach (MechanismCase expected in Cases)
            await FirstValidAdapterInteractionMovesExactlyOnce(tree, expected);
        GD.Print("TASK3_CLAW_MECHANISMS_OK kinds=4 activations=4 repeats_rejected=4");
    }

    // Break caught: a mechanism that accepts a repeated cast, moves twice, or bypasses the
    // production legacy adapter violates the one-shot world-state contract.
    private static async Task FirstValidAdapterInteractionMovesExactlyOnce(SceneTree tree,
        MechanismCase expected)
    {
        var fixture = new Node2D { Name = $"Task3{expected.Kind}MechanismFixture" };
        var mechanism = new CanyonClawMechanism2D
        {
            Name = expected.Kind.ToString(),
            Kind = expected.Kind,
            LinkedNodePath = new NodePath("LinkedNode")
        };
        var linked = new Node2D { Name = "LinkedNode", Position = new Vector2(24f, 32f) };
        mechanism.AddChild(linked);

        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Kind = ThreadTargetKind.Mechanism,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            PullReceiverPath = new NodePath("..")
        };
        var adapter = new LegacyThreadInteractionAdapter
        {
            Name = "LegacyThreadInteractionAdapter",
            TargetPath = new NodePath("..")
        };
        target.AddChild(adapter);
        var collision = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        collision.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 12f }
        });
        target.AddChild(collision);
        mechanism.AddChild(target);
        fixture.AddChild(mechanism);

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = new Vector2(-80f, 0f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.AddToGroup("player");
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        await Frames(tree, 2);

        int activations = 0;
        mechanism.Activated += OnActivated;
        Vector2 before = linked.Position;
        try
        {
            TestAssert.True(!mechanism.TryReceiveThread(default),
                $"{expected.Kind} rejects a malformed interaction without consuming its one shot");
            TestAssert.Equal(before, linked.Position,
                $"{expected.Kind} invalid interaction does not move the linked node");

            Vector2 playerBefore = player.GlobalPosition;
            MechanicalClawFrame terminal = await CastClaw(tree, player, target.WorldAnchor);
            Vector2 afterFirst = linked.Position;
            TestAssert.True(terminal.State == MechanicalClawState.Released && mechanism.IsActivated,
                $"{expected.Kind} accepts one real E plus one-frame LMB adapter interaction");
            TestAssert.Equal(before + expected.Displacement, afterFirst,
                $"{expected.Kind} applies its literal deterministic linked-node displacement");
            TestAssert.Equal(1, activations,
                $"{expected.Kind} emits exactly one Activated edge on first acceptance");
            TestAssert.Equal(1, adapter.ApplicationCount,
                $"{expected.Kind} records exactly one accepted legacy-adapter application");
            TestAssert.True((player.GlobalPosition - playerBefore).Dot(
                    playerBefore.DirectionTo(target.WorldAnchor)) <= .01f,
                $"{expected.Kind} activation does not pull the player toward the receiver");

            bool repeated = adapter.TryApply(player, player.GlobalPosition);
            TestAssert.True(!repeated,
                $"{expected.Kind} rejects a repeated adapter interaction after activation");
            TestAssert.Equal(afterFirst, linked.Position,
                $"{expected.Kind} repeated interaction cannot move the linked node again");
            TestAssert.Equal(1, activations,
                $"{expected.Kind} repeated interaction emits no second Activated edge");
            TestAssert.Equal(1, adapter.ApplicationCount,
                $"{expected.Kind} adapter count remains one after the rejected repeat");
        }
        finally
        {
            mechanism.Activated -= OnActivated;
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await Frames(tree, 2);
        }

        void OnActivated() => activations++;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task<MechanicalClawFrame> CastClaw(SceneTree tree,
        CharacterBody2D player, Vector2 mouseWorld)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        MechanicalClawFrame terminal = default;
        bool completed = false;
        void OnFrame(MechanicalClawFrame frame, GrappleSurface2D _)
        {
            if (frame.State is not (MechanicalClawState.Released or MechanicalClawState.Failed)) return;
            terminal = frame;
            completed = true;
        }

        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        claw.ClawFrameChanged += OnFrame;
        try
        {
            Input.ActionPress(InputActions.SelectGrappleMode);
            Input.ActionPress(InputActions.PrimaryAction);
            for (int frame = 0; frame < 180 && !completed; frame++)
            {
                input._PhysicsProcess(0d);
                PlayerInputFrame sampled = input.CurrentFrame with { MouseWorld = mouseWorld };
                typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
                    BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, sampled);
                modes._PhysicsProcess(0d);
                claw._PhysicsProcess(1d / 60d);
                await Frames(tree, 1);
                if (frame != 0) continue;
                Input.ActionRelease(InputActions.SelectGrappleMode);
                Input.ActionRelease(InputActions.PrimaryAction);
            }
            if (!completed) throw new InvalidOperationException(
                $"Timed out waiting for {player.Name} legacy claw terminal state; current={claw.CurrentFrame.State}");
            return terminal;
        }
        finally
        {
            Input.ActionRelease(InputActions.SelectGrappleMode);
            Input.ActionRelease(InputActions.PrimaryAction);
            claw.ClawFrameChanged -= OnFrame;
            input.SetPhysicsProcess(true);
            modes.SetPhysicsProcess(true);
            claw.SetPhysicsProcess(true);
        }
    }

    private sealed record MechanismCase(CanyonClawMechanismKind Kind, Vector2 Displacement);
}
