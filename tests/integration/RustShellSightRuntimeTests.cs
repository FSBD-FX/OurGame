using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;

namespace Knight.Tests.Integration;

public static class RustShellSightRuntimeTests
{
    // Broken query: excluding only the actor's body RID lets its own forward
    // AttackHitbox hide the player and terminates Chase after its first moving tick.
    public static async Task Run(SceneTree tree)
    {
        foreach (int direction in new[] { 1, -1 }) await ChaseExcludesOnlyOwnedColliders(tree, direction);
        GD.Print("RUST_SHELL_SIGHT_RUNTIME_OK");
    }

    private static async Task ChaseExcludesOnlyOwnedColliders(SceneTree tree, int direction)
    {
        var root = new Node2D { Name = direction > 0 ? "RustSightRight" : "RustSightLeft" };
        RustShellSquire actor = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        actor.Gravity = 0f;
        var player = new CharacterBody2D
        {
            Name = "Player", Position = new Vector2(direction * 260f, 0f),
            CollisionLayer = CombatCollisionLayers.Player, CollisionMask = CombatCollisionLayers.None
        };
        player.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 14f } });
        player.AddToGroup("player");
        root.AddChild(actor);
        root.AddChild(player);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 3);
            TestAssert.Equal(player, tree.GetFirstNodeInGroup("player"), "isolated sight fixture owns the actual AI target");
            await AssertSustainedChase("authored forward AttackHitbox");

            var nested = new Node2D { Name = "OwnedSensorMount" };
            Area2D ownedArea = Area("OwnedNestedArea", new Vector2(direction * 75f, 0));
            nested.AddChild(ownedArea);
            actor.AddChild(nested);
            await Frames(tree, 2);
            await AssertSustainedChase("nested actor-owned Area2D");

            Vector2 barrierPosition = actor.GlobalPosition + new Vector2(direction * 110f, 0);
            var wall = new StaticBody2D
            {
                Name = "RealWorldWall", Position = barrierPosition,
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None
            };
            wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 120) } });
            root.AddChild(wall);
            await Frames(tree, 3);
            await AssertExternalBlocker("real World wall");
            wall.QueueFree();
            await Frames(tree, 3);
            TestAssert.True(actor.HasLineOfSightTo(player), "removing the World wall restores sight through only owned areas");

            // An unrelated queryable area must still block: disabling all Area2D ray
            // hits would fix self-occlusion but silently change the sight contract.
            root.AddChild(Area("ForeignArea", barrierPosition));
            await Frames(tree, 3);
            await AssertExternalBlocker("unrelated Area2D");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }

        async Task AssertSustainedChase(string ownCollider)
        {
            float startX = actor.GlobalPosition.X;
            for (int tick = 0; tick < 8; tick++)
            {
                await Frames(tree, 1);
                TestAssert.True(actor.HasLineOfSightTo(player), $"direction {direction}: {ownCollider} never hides the player");
                TestAssert.Equal(RustShellState.Chase, actor.CurrentState,
                    $"direction {direction}: automatic physics sustains Chase through {ownCollider}");
            }
            TestAssert.True(direction * (actor.GlobalPosition.X - startX) > 8f,
                $"direction {direction}: real MoveAndSlide advances for multiple chase ticks through {ownCollider}");
        }

        async Task AssertExternalBlocker(string blocker)
        {
            TestAssert.True(!actor.HasLineOfSightTo(player), $"direction {direction}: {blocker} still blocks the production ray");
            float stoppedX = actor.GlobalPosition.X;
            await Frames(tree, 4);
            TestAssert.Equal(RustShellState.Idle, actor.CurrentState,
                $"direction {direction}: real AI stops chasing behind {blocker}");
            TestAssert.Near(stoppedX, actor.GlobalPosition.X, .01f,
                $"direction {direction}: automatic movement remains stopped behind {blocker}");
        }
    }

    private static Area2D Area(string name, Vector2 position)
    {
        var area = new Area2D
        {
            Name = name, Position = position, CollisionLayer = CombatCollisionLayers.EnemyAttack,
            CollisionMask = CombatCollisionLayers.None, Monitoring = false, Monitorable = true
        };
        area.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(12, 80) } });
        return area;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}
