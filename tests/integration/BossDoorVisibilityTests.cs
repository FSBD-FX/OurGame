using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Tests.Integration;

public static class BossDoorVisibilityTests
{
    public static async Task Run(SceneTree tree)
    {
        // Removing the ReadableDoor binding must fail: an open collision cannot leave the
        // generated solid door panel visible. Formal visual state must still be preserved.
        Node2D sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn")
            .Instantiate<Node2D>();
        Node2D entry = sector.GetNode<Node2D>("Gameplay/Doors/Entry");
        Node2D exit = sector.GetNode<Node2D>("Gameplay/Doors/Exit");
        entry.AddChild(new PrologueMechanismVisual
            { Name = "Visual", Kind = MechanismVisualKind.ShortcutDoor });
        exit.AddChild(new PrologueMechanismVisual
            { Name = "Visual", Kind = MechanismVisualKind.ShortcutDoor });
        var progress = new PrologueProgress();
        BossAdmissionFixturePrerequisites.Apply(progress); // Controlled lifecycle fixture, not admission proof.
        sector.GetNode<BossArenaController>("Gameplay/BossArenaController").BindRuntime(progress, null);
        tree.Root.AddChild(sector);
        try
        {
            BossArenaController arena = sector.GetNode<BossArenaController>(
                "Gameplay/BossArenaController");
            await Settle(tree);
            AssertDoor(entry, open: true, "initial entry");
            AssertDoor(exit, open: false, "initial exit");

            arena.BeginEncounter();
            await Settle(tree);
            TestAssert.True(arena.IsEncounterActive, "eligible controlled arena actually begins before visual lifecycle assertions");
            AssertDoor(entry, open: false, "active entry");
            AssertDoor(exit, open: false, "active exit");

            HealthComponent health = arena.CurrentBoss.GetNode<HealthComponent>("Health");
            health.ApplyDamage(new DamageRequest(health.MaxHealth, Vector2.Right, 0f, 99101UL));
            await Settle(tree);
            TestAssert.True(!arena.IsEncounterActive, "defeat ends the real arena encounter");
            AssertDoor(entry, open: true, "defeated entry");
            AssertDoor(exit, open: true, "defeated exit");
            arena.BeginEncounter(); // Local defeat must not depend on an external persistence subscriber.
            await Settle(tree);
            TestAssert.True(!arena.IsEncounterActive, "local terminal defeat cannot restart the dead Boss before explicit reset");
            AssertDoor(exit, open: true, "defeated repeat activation exit");

            arena.ResetForCheckpoint();
            await Settle(tree);
            AssertDoor(entry, open: true, "retry entry");
            AssertDoor(exit, open: false, "retry exit");
        }
        finally
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, sector);
        }
    }

    private static void AssertDoor(Node2D door, bool open, string state)
    {
        TestAssert.Equal(open, door.GetNode<CollisionShape2D>("Collision").Disabled,
            $"{state}: collision follows passage state");
        TestAssert.Equal(!open, door.GetNode<Polygon2D>("ReadableDoor").Visible,
            $"{state}: visible solid door panel follows collision state");
        TestAssert.Equal(open ? MechanismVisualState.Active : MechanismVisualState.Disabled,
            door.GetNode<PrologueMechanismVisual>("Visual").CurrentState,
            $"{state}: formal door visual retains its existing state contract");
    }

    private static async Task Settle(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
