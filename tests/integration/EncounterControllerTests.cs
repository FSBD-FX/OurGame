using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;

namespace Knight.Tests.Integration;

public static class EncounterControllerTests
{
    public static async Task Run(SceneTree tree)
    {
        await ActivationCompletionAndResetAreOwnedAndDeterministic(tree);
        await ResettingInactiveEncounterDoesNotSpawn(tree);
    }

    private static async Task ActivationCompletionAndResetAreOwnedAndDeterministic(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "LiveEncounter");
        int completions = 0;
        fixture.Controller.Completed += () => completions++;

        fixture.Controller.Activate();
        TestAssert.Equal(2, fixture.Actors.GetChildCount(), "activation spawns each authored actor exactly once");
        TestAssert.Equal(new Vector2(120f, 80f), ((Node2D)fixture.Actors.GetNode("ground")).GlobalPosition,
            "ground actor spawns at authored marker");
        TestAssert.Equal(new Vector2(280f, -40f), ((Node2D)fixture.Actors.GetNode("air")).GlobalPosition,
            "air actor spawns at authored marker");

        fixture.Controller.Activate();
        await Process(tree);
        TestAssert.Equal(2, fixture.Actors.GetChildCount(), "repeated activation never duplicates actors");

        IReadOnlyList<ulong> originalIds = InstanceIds(fixture.Actors);
        Kill(fixture.Actors.GetNode("ground"), 7101UL);
        TestAssert.Equal(0, completions, "one defeated actor cannot complete a two-actor encounter");
        Kill(fixture.Actors.GetNode("air"), 7102UL);
        TestAssert.Equal(1, completions, "both defeated actors complete the encounter exactly once");
        Kill(fixture.Actors.GetNode("air"), 7103UL);
        TestAssert.Equal(1, completions, "completed encounter never emits twice");

        fixture.Controller.ResetForCheckpoint();
        TestAssert.Equal(2, fixture.Actors.GetChildCount(), "active encounter reset recreates both actors");
        IReadOnlyList<ulong> resetIds = InstanceIds(fixture.Actors);
        TestAssert.True(originalIds[0] != resetIds[0] && originalIds[1] != resetIds[1],
            "active encounter reset removes old instances and creates fresh instances");
        TestAssert.Equal(new Vector2(120f, 80f), ((Node2D)fixture.Actors.GetNode("ground")).GlobalPosition,
            "reset ground actor returns to authored marker");
        TestAssert.Equal(new Vector2(280f, -40f), ((Node2D)fixture.Actors.GetNode("air")).GlobalPosition,
            "reset air actor returns to authored marker");
        TestAssert.Equal(1, completions, "reset itself does not emit another encounter completion");
        TestAssert.True(!fixture.Controller.IsCompleted, "reset reopens an active completed encounter");
        await Process(tree);
        await Dispose(tree, fixture.Root);
    }

    private static async Task ResettingInactiveEncounterDoesNotSpawn(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "InactiveEncounter");
        fixture.Controller.ResetForCheckpoint();
        await Process(tree);
        TestAssert.Equal(0, fixture.Actors.GetChildCount(), "resetting an inactive encounter does not spawn actors");
        TestAssert.True(!fixture.Controller.IsActive, "inactive encounter remains inactive after reset");
        await Dispose(tree, fixture.Root);
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree, string name)
    {
        var root = new Node2D { Name = name };
        var markers = new Node2D { Name = "Markers" };
        markers.AddChild(new Marker2D { Name = "Ground", Position = new Vector2(120f, 80f) });
        markers.AddChild(new Marker2D { Name = "Air", Position = new Vector2(280f, -40f) });
        var actors = new Node2D { Name = "Actors" };
        var controller = new EncounterController
        {
            Name = "EncounterController",
            Definition = Knight.Tests.Unit.EncounterDefinitionTests.ValidMixed(),
            ActorRootPath = new NodePath("../Actors")
        };
        root.AddChild(markers);
        root.AddChild(actors);
        root.AddChild(controller);
        tree.Root.AddChild(root);
        await Process(tree);
        return new Fixture(root, actors, controller);
    }

    private static void Kill(Node actor, ulong attackId)
    {
        HealthComponent health = actor.GetNode<HealthComponent>("Health");
        health.ApplyDamage(new DamageRequest(999, Vector2.Right, 0f, attackId));
    }

    private static IReadOnlyList<ulong> InstanceIds(Node actors) =>
        new[] { actors.GetNode("ground").GetInstanceId(), actors.GetNode("air").GetInstanceId() };

    private static async Task Process(SceneTree tree) =>
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

    private static async Task Dispose(SceneTree tree, Node root)
    {
        root.QueueFree();
        await Process(tree);
    }

    private sealed record Fixture(Node2D Root, Node2D Actors, EncounterController Controller);
}
