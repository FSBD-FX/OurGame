using System;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using System.Linq;

namespace Knight.Tests.Integration;

public static class Task14TelegraphAdapterTests
{
    public static async Task Run(SceneTree tree)
    {
        await RealPublisherControlsGuidanceAdapter<BellBoltSentry>(tree,
            CanyonEncounterCatalog.SentryScene, new Vector2(300, 171), new Vector2(400, 171),
            actor => ((BellBoltSentry)actor).CurrentFrame.AttackTimeline.Phase);
        await RealPublisherControlsGuidanceAdapter<CanyonSkitter>(tree,
            CanyonEncounterCatalog.SkitterScene, new Vector2(300, 201), new Vector2(400, 201),
            actor => ((CanyonSkitter)actor).CurrentFrame.AttackTimeline.Phase);
        await GeneratedActiveSentryDefersFireflyButRemoteActorDoesNot(tree);
        GD.Print("TASK14_TELEGRAPH_ADAPTER_OK");
    }

    private static async Task GeneratedActiveSentryDefersFireflyButRemoteActorDoesNot(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "task14-firefly-sentry");
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 6);
            CharacterBody2D player = level.Player;
            player.GlobalPosition = new Vector2(1400, 2144);
            player.Velocity = Vector2.Zero;
            await Frames(tree, 8);
            PrologueSector sector = level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent");
            CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
                "Gameplay/Encounters/FrozenUpperEncounter");
            Node actors = sector.GetNode("Gameplay/Actors");
            CanyonFireflyGuidanceController controller = level.GetNode<CanyonFireflyGuidanceController>(
                "WorldRoot/FireflyGuidance");
            TestAssert.True(group.IsActive && group.LiveActorCount == 1 &&
                            level.CurrentSector == PrologueSectorId.CanyonDescent,
                "generated frozen group is active in the live full-level firefly sector");
            HealthComponent first = actors.GetChild(0).GetNode<HealthComponent>("Health");
            TestAssert.True(first.ApplyDamage(new DamageRequest(first.CurrentHealth,
                    Vector2.Right, 0f, 714900UL)),
                "real frozen first actor emits Defeated before the new sentry arrival");
            await Frames(tree, 39);
            BellBoltSentry sentry = actors.GetChildren().OfType<BellBoltSentry>().Single();
            bool telegraph = false;
            for (int frame = 0; frame < 30; frame++)
            {
                await Frames(tree, 1);
                if (sentry.CurrentFrame.AttackTimeline.Phase != AttackPhase.Telegraph) continue;
                sentry.SetPhysicsProcess(false);
                telegraph = true;
                break;
            }
            TestAssert.True(telegraph && group.IsActive &&
                            CanyonCombatTelegraphAdapter.IsTelegraphActive(sentry),
                "generated new sentry emits its actual full Telegraph while encounter is active");
            controller.RefreshNow(0f);
            TestAssert.True(controller.CombatTelegraphActive && !controller.CurrentFrame.Visible,
                "active generated new enemy warning suppresses firefly guidance");
            Vector2 authored = sentry.GlobalPosition;
            sentry.GlobalPosition = new Vector2(1900, authored.Y);
            controller.RefreshNow(0f);
            TestAssert.True(!controller.CombatTelegraphActive,
                "same telegraph publisher outside its active arena cannot suppress firefly");
            sentry.GlobalPosition = authored;
            group.ResetForCheckpoint();
            controller.RefreshNow(0f);
            TestAssert.True(!controller.CombatTelegraphActive,
                "inactive encounter cannot keep the new publisher's firefly suppression latched");
        }
        finally
        {
            level.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task RealPublisherControlsGuidanceAdapter<T>(SceneTree tree,
        string scenePath, Vector2 actorPosition, Vector2 playerPosition,
        Func<Node2D, AttackPhase> phase) where T : Node2D
    {
        var root = new Node2D { Name = "Task14RealPublisher" + typeof(T).Name };
        var floor = new StaticBody2D
        {
            Name = "Floor", Position = new Vector2(350, 220),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        floor.AddChild(new CollisionShape2D
        { Shape = new RectangleShape2D { Size = new Vector2(600, 40) } });
        root.AddChild(floor);
        var player = new Node2D { Name = "LivingPlayer", Position = playerPosition };
        player.AddToGroup("player");
        player.AddChild(new HealthComponent { Name = "Health", MaxHealth = 5 });
        root.AddChild(player);
        T actor = GD.Load<PackedScene>(scenePath).Instantiate<T>();
        actor.Position = actorPosition;
        root.AddChild(actor);
        tree.Root.AddChild(root);
        try
        {
            bool sawTelegraph = false;
            for (int frame = 0; frame < 100; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                if (phase(actor) != AttackPhase.Telegraph) continue;
                actor.SetPhysicsProcess(false);
                sawTelegraph = true;
                break;
            }
            TestAssert.True(sawTelegraph,
                $"actual {typeof(T).Name} publisher enters its ordinary Telegraph phase");
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(actor),
                $"formal guidance adapter recognizes actual {typeof(T).Name} telegraph");
            actor.SetPhysicsProcess(true);
            bool sawNonTelegraph = false;
            for (int frame = 0; frame < 100; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                if (phase(actor) == AttackPhase.Telegraph) continue;
                actor.SetPhysicsProcess(false);
                sawNonTelegraph = true;
                break;
            }
            TestAssert.True(sawNonTelegraph && !CanyonCombatTelegraphAdapter.IsTelegraphActive(actor),
                $"actual {typeof(T).Name} non-Telegraph phase releases guidance suppression");
            root.RemoveChild(actor);
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(actor),
                $"unmounted {typeof(T).Name} cannot keep guidance suppressed");
            actor.Free();
        }
        finally
        {
            player.RemoveFromGroup("player");
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }
}
