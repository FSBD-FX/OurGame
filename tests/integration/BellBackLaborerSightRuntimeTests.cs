using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Laborer;

namespace Knight.Tests.Integration;

public static class BellBackLaborerSightRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        foreach (int direction in new[] { 1, -1 })
            await OwnedCollidersAllowCommittedSweep(tree, direction);
        await SolidPlatformPreventsNewAttack(tree);
        foreach (int direction in new[] { 1, -1 })
            await WorldWallBlocksUntilRemoved(tree, direction);
        foreach (int direction in new[] { 1, -1 })
        {
            await CommittedSweepDamageRespectsWorldWall(tree, direction, false);
            await CommittedSweepDamageRespectsWorldWall(tree, direction, true);
        }
        await ExplicitFilteredRetryPreservesAttackIdentity(tree);
        await UnconfiguredHitboxRetainsExplicitContactBehavior(tree);
        GD.Print("BELL_BACK_LABORER_SIGHT_RUNTIME_OK");
    }

    private static async Task OwnedCollidersAllowCommittedSweep(SceneTree tree, int direction)
    {
        await InFixture(tree, new Vector2(direction * 70, 0), null, async fixture =>
        {
            // Authored Body/Hurtbox/AttackHitbox/ThreadTarget remain intact, and
            // the fixture adds a queryable nested owned area in front of the actor.
            await Until(tree, () => fixture.Actor.CurrentState == BellBackLaborerState.Telegraph,
                90, $"direction {direction}: owned colliders must not prevent a real near-target Telegraph");
            ulong attackId = fixture.Actor.CurrentFrame.AttackTimeline.AttackId;
            TestAssert.True(attackId > 0, "automatic Telegraph allocates a real attack identity");
            TestAssert.Equal(direction, fixture.Actor.Facing, "near-target attack locks the correct facing");

            // Controlled geometry changes AFTER a legal commitment. The right
            // case loses LOS only; the left also loses horizontal detection range.
            fixture.Root.AddChild(WorldBody("LateWall", new Vector2(direction * 40, 0), new Vector2(10, 180)));
            if (direction < 0) fixture.Player.Position = new Vector2(-500, 0);
            await Until(tree, () => fixture.Samples.Any(sample => sample.Id == attackId && sample.Phase == AttackPhase.Recovery),
                120, "losing sight after Telegraph must still complete Active then Recovery");
            int active = fixture.Samples.FindIndex(sample => sample.Id == attackId && sample.Phase == AttackPhase.Active);
            int recovery = fixture.Samples.FindIndex(sample => sample.Id == attackId && sample.Phase == AttackPhase.Recovery);
            TestAssert.True(active >= 0 && recovery > active, "the same committed AttackId publishes ordered Active and Recovery");
            TestAssert.True(fixture.Samples.Where(sample => sample.Id == attackId && sample.Phase == AttackPhase.Active)
                .All(sample => sample.Monitoring), "the committed sweep really enables its physical hitbox");
            TestAssert.True(fixture.Samples.Where(sample => sample.Id == attackId && sample.Phase == AttackPhase.Recovery)
                .All(sample => !sample.Monitoring), "Recovery disables the physical hitbox");
            TestAssert.Equal(direction, fixture.Actor.Facing, "lost sight does not flip the committed sweep");
        });
    }

    private static async Task SolidPlatformPreventsNewAttack(SceneTree tree)
    {
        // Actor top=-42; platform spans y=-80..-60; player capsule bottom=-80.
        // Horizontal separation remains 70, inside GuardRange. This is a solid
        // terrain separation fixture, NOT a specification for open-air Y distance.
        await InFixture(tree, new Vector2(70, -122),
            WorldBody("PlayerPlatform", new Vector2(40, -70), new Vector2(220, 20)), async fixture =>
            {
                await AssertNoNewAttack(tree, fixture, "player above a solid platform / Laborer below");
            });
    }

    private static async Task WorldWallBlocksUntilRemoved(SceneTree tree, int direction)
    {
        StaticBody2D wall = WorldBody("InitialWall", new Vector2(direction * 40, 0), new Vector2(10, 180));
        await InFixture(tree, new Vector2(direction * 70, 0), wall, async fixture =>
        {
            await AssertNoNewAttack(tree, fixture, $"direction {direction}: real World wall");
            wall.QueueFree();
            await Until(tree, () => fixture.Actor.CurrentState == BellBackLaborerState.Telegraph,
                90, $"direction {direction}: removing the wall restores normal attack admission");
            ulong attackId = fixture.Actor.CurrentFrame.AttackTimeline.AttackId;
            await Until(tree, () => fixture.Samples.Any(sample => sample.Id == attackId && sample.Phase == AttackPhase.Active),
                90, "restored sight produces an actual active sweep, not just an idle-state change");
        });
    }

    private static async Task AssertNoNewAttack(SceneTree tree, Fixture fixture, string reason)
    {
        // Longer than Guard + Telegraph + Active: observing only the final Idle
        // state would miss an illicit attack that ran between sampling points.
        await Frames(tree, 150);
        TestAssert.True(!fixture.Samples.Any(sample => sample.Phase != AttackPhase.None),
            $"{reason}: must not publish a new Telegraph/Active/Recovery; observed=" +
            string.Join(",", fixture.Samples.Select(sample => $"{sample.Id}:{sample.Phase}").Distinct()));
        TestAssert.True(!fixture.Hitbox.Monitoring, $"{reason}: blocked target cannot arm the damage hitbox");
    }

    private static async Task CommittedSweepDamageRespectsWorldWall(SceneTree tree, int direction, bool blocked)
    {
        await InFixture(tree, new Vector2(direction * 70, 0), null, async fixture =>
        {
            await Until(tree, () => fixture.Actor.CurrentState == BellBackLaborerState.Telegraph,
                90, "damage fixture must first earn a legal unobstructed Telegraph");
            ulong attackId = fixture.Actor.CurrentFrame.AttackTimeline.AttackId;
            if (blocked)
                fixture.Root.AddChild(WorldBody("DamageBarrier", new Vector2(direction * 40, 0), new Vector2(10, 180)));
            await Until(tree, () => fixture.Samples.Any(sample => sample.Id == attackId && sample.Phase == AttackPhase.Recovery),
                120, "a late damage barrier must not cancel the committed Active/Recovery sequence");
            await Frames(tree, 2);
            TestAssert.True(fixture.Samples.Any(sample => sample.Id == attackId && sample.Phase == AttackPhase.Active && sample.Monitoring),
                "committed sweep must actually activate its damage Area, even behind the late wall");
            TestAssert.True(fixture.PhysicalOverlapSeen,
                $"direction {direction}, wall={blocked}: actual active Area must overlap the real player Hurtbox; missing overlap is not proof of filtering");
            GD.Print($"LABORER_DAMAGE_LOS direction={direction} wall={blocked} attack={attackId} " +
                $"overlap={fixture.PhysicalOverlapSeen} connected={fixture.Connections} damaged={fixture.DamageEvents} hp={fixture.Health.CurrentHealth}");
            TestAssert.Equal(blocked ? 5 : 4, fixture.Health.CurrentHealth,
                $"direction {direction}: {(blocked ? "solid World wall must block damage from an already committed sweep" : "unobstructed automatic Area contact must remove exactly one health point")}");
            TestAssert.Equal(blocked ? 0 : 1, fixture.DamageEvents,
                "actual Health.Damaged events agree with the physical damage outcome");
            TestAssert.Equal(blocked ? 0 : 1, fixture.Connections,
                "blocked contact is not accepted; the unobstructed sweep connects once per AttackId");
        });
    }

    private static async Task ExplicitFilteredRetryPreservesAttackIdentity(SceneTree tree)
    {
        StaticBody2D wall = WorldBody("RetryWall", new Vector2(40, 0), new Vector2(10, 180));
        await InFixture(tree, new Vector2(70, 0), wall, async fixture =>
        {
            Hurtbox target = fixture.Player.GetNode<Hurtbox>("Hurtbox");
            // Explicit public contact-boundary test, NOT an automatic re-contact
            // test: removing a wall does not re-emit AreaEntered. End immediately
            // keeps queued overlap callbacks out of this manual identity check.
            fixture.Hitbox.Begin(900001);
            fixture.Hitbox.End();
            TestAssert.True(!fixture.Hitbox.TryConnect(target), "World-filtered explicit contact is rejected");
            TestAssert.Equal(5, fixture.Health.CurrentHealth, "rejected contact leaves real Health unchanged");
            TestAssert.Equal(0, fixture.Connections, "rejected contact emits no Connected event");
            wall.QueueFree();
            await Frames(tree, 3);
            // No second Begin: the exact same attack must retain its opportunity.
            TestAssert.True(fixture.Hitbox.TryConnect(target), "filter rejection must not spend the AttackId's target eligibility");
            TestAssert.True(!fixture.Hitbox.TryConnect(target), "successful explicit contact still deduplicates the same target and AttackId");
            TestAssert.Equal(4, fixture.Health.CurrentHealth, "one accepted retry removes exactly one health point");
            TestAssert.Equal(1, fixture.DamageEvents, "retry produces exactly one actual Damaged event");
            TestAssert.Equal(1, fixture.Connections, "only the successful retry emits Connected");
        });
    }

    private static async Task UnconfiguredHitboxRetainsExplicitContactBehavior(SceneTree tree)
    {
        await InFixture(tree, new Vector2(70, 0),
            WorldBody("DefaultContactWall", new Vector2(40, 0), new Vector2(10, 180)), fixture =>
            {
                // Independent unconfigured Hitbox: existing explicit contact
                // semantics do not acquire Laborer's World-only policy globally.
                var hitbox = new Hitbox { Name = "UnconfiguredHitbox", Damage = 1, CollisionMask = 0 };
                fixture.Root.AddChild(hitbox);
                Hurtbox target = fixture.Player.GetNode<Hurtbox>("Hurtbox");
                hitbox.Begin(900002);
                hitbox.End();
                TestAssert.True(hitbox.TryConnect(target), "unconfigured Hitbox keeps accepting explicit contact despite intervening World geometry");
                TestAssert.True(!hitbox.TryConnect(target), "unconfigured Hitbox retains ordinary successful-contact deduplication");
                TestAssert.Equal(4, fixture.Health.CurrentHealth, "unconfigured contact still causes exactly one damage point");
                return Task.CompletedTask;
            });
    }

    private static async Task InFixture(SceneTree tree, Vector2 playerPosition, StaticBody2D barrier,
        Func<Fixture, Task> check)
    {
        var root = new Node2D { Name = "LaborerSightFixture" };
        BellBackLaborer actor = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
            .Instantiate<BellBackLaborer>();
        // Fresh tuning avoids mutating the packed scene's shared resource.
        actor.Tuning = new BellBackLaborerTuning { Gravity = 0 };
        var player = new CharacterBody2D
        {
            Name = "Player", Position = playerPosition,
            CollisionLayer = CombatCollisionLayers.Player, CollisionMask = CombatCollisionLayers.None
        };
        player.AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = 14, Height = 84 } });
        var health = new HealthComponent { Name = "Health", MaxHealth = 5, InvulnerabilitySeconds = 0 };
        var hurtbox = new Hurtbox
        {
            Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Player,
            CollisionMask = CombatCollisionLayers.None
        };
        hurtbox.AddChild(new CollisionShape2D { Shape = new CapsuleShape2D { Radius = 14, Height = 84 } });
        player.AddChild(health);
        player.AddChild(hurtbox);
        player.AddToGroup("player");
        var mount = new Node2D { Name = "OwnedSensorMount" };
        var nestedArea = new Area2D
        {
            Name = "OwnedNestedArea", Position = new Vector2(Math.Sign(playerPosition.X) * 35, 0),
            CollisionLayer = CombatCollisionLayers.EnemyAttack, CollisionMask = CombatCollisionLayers.None,
            Monitoring = false, Monitorable = true
        };
        nestedArea.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(8, 50) } });
        mount.AddChild(nestedArea);
        actor.AddChild(mount);
        root.AddChild(actor);
        root.AddChild(player);
        root.AddChild(WorldBody("Ground", new Vector2(0, 52), new Vector2(1200, 20)));
        if (barrier != null) root.AddChild(barrier);
        var fixture = new Fixture(root, actor, player, actor.GetNode<Hitbox>("AttackHitbox"), health);
        void Record(AttackPresentationFrame frame)
        {
            fixture.Samples.Add((frame.Timeline.AttackId, frame.Timeline.Phase, fixture.Hitbox.Monitoring));
            if (frame.Timeline.Phase == AttackPhase.Active && fixture.Hitbox.GetOverlappingAreas().Contains(hurtbox))
                fixture.PhysicalOverlapSeen = true;
        }
        void OnConnected(Hurtbox target) { if (target == hurtbox) fixture.Connections++; }
        void OnDamaged(int remaining, Vector2 impulse) => fixture.DamageEvents++;
        actor.AttackFrameChanged += Record;
        fixture.Hitbox.Connected += OnConnected;
        health.Damaged += OnDamaged;
        try
        {
            tree.Root.AddChild(root);
            await Frames(tree, 3);
            TestAssert.Equal(player, tree.GetFirstNodeInGroup("player"), "isolated fixture owns the real AI target");
            await check(fixture);
        }
        finally
        {
            actor.AttackFrameChanged -= Record;
            fixture.Hitbox.Connected -= OnConnected;
            health.Damaged -= OnDamaged;
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static StaticBody2D WorldBody(string name, Vector2 position, Vector2 size)
    {
        var body = new StaticBody2D
        {
            Name = name, Position = position, CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        return body;
    }

    private static async Task Until(SceneTree tree, Func<bool> condition, int ticks, string message)
    {
        for (int tick = 0; tick < ticks && !condition(); tick++) await Frames(tree, 1);
        TestAssert.True(condition(), message);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private sealed record Fixture(Node2D Root, BellBackLaborer Actor, CharacterBody2D Player, Hitbox Hitbox, HealthComponent Health)
    {
        public List<(ulong Id, AttackPhase Phase, bool Monitoring)> Samples { get; } = new();
        public bool PhysicalOverlapSeen { get; set; }
        public int Connections { get; set; }
        public int DamageEvents { get; set; }
    }
}
