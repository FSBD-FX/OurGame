using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Prologue.Mechanisms;

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
        mechanism.AddChild(target);
        fixture.AddChild(mechanism);

        var player = new CharacterBody2D { Name = "Player", Position = new Vector2(-80f, 0f) };
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

            bool first = adapter.TryApply(player, player.GlobalPosition);
            Vector2 afterFirst = linked.Position;
            TestAssert.True(first && mechanism.IsActivated,
                $"{expected.Kind} accepts its first real adapter interaction");
            TestAssert.Equal(before + expected.Displacement, afterFirst,
                $"{expected.Kind} applies its literal deterministic linked-node displacement");
            TestAssert.Equal(1, activations,
                $"{expected.Kind} emits exactly one Activated edge on first acceptance");
            TestAssert.Equal(1, adapter.ApplicationCount,
                $"{expected.Kind} records exactly one accepted legacy-adapter application");

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

    private sealed record MechanismCase(CanyonClawMechanismKind Kind, Vector2 Displacement);
}
