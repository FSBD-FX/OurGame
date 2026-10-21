using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Current layout2 regression, separate from the retained layout1 SalvageFloor sample.
public static class CurrentBasinLaborerClearanceRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        AssertOffTreeFactory();
        ClearanceObservation normal = await ObserveTraversal(tree, blocked: false);
        AssertTraversal(normal);
        ClearanceObservation blocked = await ObserveTraversal(tree, blocked: true);
        bool rejectedForMovement = false;
        try { AssertTraversal(blocked); }
        catch (InvalidOperationException error)
        {
            rejectedForMovement = error.Message.Contains("approach at least 100px", StringComparison.Ordinal);
        }
        TestAssert.True(blocked.ApproachSeen && blocked.BlockerContact && blocked.ActorSupported &&
            blocked.LongestSupport >= 12 && blocked.MovedTowardPlayer is >= 0f and < 100f &&
            blocked.PlayerSupported && blocked.Recoveries == 0 && rejectedForMovement,
            "CURRENT_BASIN_BLOCKED_LANE_RED: a real78px soffit must physically block the84px Laborer and be rejected by the shared movement contract");
        GD.Print("CURRENT_BASIN_BLOCKED_LANE_REJECTED native_contact=true displacement_contract=true");
        await AssertOriginalWalkwayTopStillSupportsPlayer(tree);
        GD.Print("CURRENT_BASIN_LABORER_CLEARANCE_OK layout=2 natural_spawn=True blocked_negative=True observation_budget=300");
    }

    private static void AssertOffTreeFactory()
    {
        // Exercise the actual retained temp-storage consumer factory, not a
        // second mocked configuration. No AddChild/Ready/file seed is allowed.
        PrologueLevelController level = BasinLaborerClearanceRuntimeTests.CreateLevelFixture();
        try
        {
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            var paths = IsolatedPrologueFixture.ValidateExistingSavePath(flow.SavePath);
            TestAssert.True(!level.IsInsideTree() && !level.IsNodeReady() &&
                flow.SettingsPath == paths.SettingsPath &&
                level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory == paths.PlaytestsDirectory &&
                !System.IO.File.Exists(paths.SavePath) && !System.IO.File.Exists(paths.SettingsPath) &&
                !System.IO.Directory.Exists(paths.PlaytestsDirectory),
                "actual retained laborer factory is isolated off-tree and cannot seed persistence");
            TestAssert.True(world.LayoutVersion == 2 && world.InitialSector == PrologueSectorId.CanyonBasin &&
                player.Position == new Vector2(3300, 4200) &&
                player.GetNode<PlayerInputReader>("Input").ProcessMode == Node.ProcessModeEnum.Disabled &&
                !flow.HeadlessAutoStart && flow.AutomationAutoStart,
                "CURRENT_BASIN_FACTORY_RED: retained laborer storage factory must stage the current V2 lane without mounting or legacy coordinates");
            GD.Print("CURRENT_BASIN_FACTORY_OK off_tree=true seeded=false layout=2");
        }
        finally { level.Free(); }
    }

    private sealed record ClearanceObservation(bool ApproachSeen, float MovedTowardPlayer,
        int LongestSupport, bool ActorSupported, bool PlayerSupported, bool SamePlayerWorld,
        int Recoveries, ulong AttackId, int Telegraph, int Active, int Recovery, bool BlockerContact);

    private static void AssertTraversal(ClearanceObservation sample)
    {
        TestAssert.True(sample.ApproachSeen,
            "real SteamValves Laborer must see and approach the player on the same layer");
        TestAssert.True(sample.MovedTowardPlayer >= 100f,
            "current SteamValves passage must let its unchanged Laborer approach at least 100px instead of wedging under the shortcut walkway");
        TestAssert.True(sample.LongestSupport >= 12 && sample.ActorSupported,
            "current Laborer must retain native trench-floor support while approaching and attacking");
        TestAssert.True(sample.PlayerSupported && sample.SamePlayerWorld && sample.Recoveries == 0,
            "clearance must preserve grounded player, current full world, and zero recovery relocation");
        TestAssert.True(sample.AttackId > 0 && sample.Telegraph >= 0 &&
            sample.Active > sample.Telegraph && sample.Recovery > sample.Active,
            "the physically approaching current enemy must finish Telegraph→Active→Recovery on one real AttackId");
    }

    private static async Task<ClearanceObservation> ObserveTraversal(SceneTree tree, bool blocked)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        BellBackLaborer actor = null;
        RectangleShape2D blockerShape = null;
        Action<AttackPresentationFrame> observe = null;
        int recoveries = 0;
        void Recovered(string _) => recoveries++;
        try
        {
            // Inside the actual SteamValves activation box and grounded under the
            // return walkway. No fake actors, disabled health/AI, or post-tree warps.
            var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            player.Position = new Vector2(3300, 4200);
            player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.Equal(2, world.LayoutVersion, "current clearance must use the saved full layout2 Main");
            world.InitialSector = PrologueSectorId.CanyonBasin;
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = true;
            IsolatedPrologueFixture.Configure(level, "current-steam-laborer-clearance");
            level.RecoveryCompleted += Recovered;
            ulong playerId = player.GetInstanceId();
            ulong worldId = world.GetInstanceId();
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            var group = level.CurrentSectorRoot.FindChildren("*", null, true, false)
                .OfType<CanyonEncounterGroup>().Single(item => item.EncounterId ==
                    new StringName("canyon.v2.encounter.steam-valves"));
            TestAssert.True(group.IsActive && group.CurrentBatch == 0 && group.LiveActorCount == 1,
                "actual SteamValves trigger must spawn its authored first batch, not a hand-added actor");
            actor = level.CurrentSectorRoot.GetNode("Gameplay/Actors").GetChildren()
                .OfType<BellBackLaborer>().Single();
            var floor = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/SteamTrenchFloor");
            var roofShape = level.CurrentSectorRoot.GetNode<CollisionShape2D>(
                "Gameplay/Collision/ShortcutBasinWalkway/Collision");
            var floorShape = floor.GetNode<CollisionShape2D>("Collision");
            float floorTop = floorShape.ToGlobal(-((RectangleShape2D)floorShape.Shape).Size / 2f).Y;
            float roofBottom = roofShape.ToGlobal(((RectangleShape2D)roofShape.Shape).Size / 2f).Y;
            var capsule = (CapsuleShape2D)actor.GetNode<CollisionShape2D>("BodyCollider").Shape;
            TestAssert.True(capsule.Height == 84f && capsule.Radius == 28f &&
                floorTop == 4236f && roofBottom == 4144f && floorTop - roofBottom == 92f &&
                roofShape.ToGlobal(-((RectangleShape2D)roofShape.Shape).Size / 2f).Y == 4116f &&
                !roofShape.Disabled && !floorShape.Disabled &&
                (floor.CollisionLayer & CombatCollisionLayers.World) != 0 &&
                (roofShape.GetParent<StaticBody2D>().CollisionLayer & CombatCollisionLayers.World) != 0,
                "saved native floor/soffit preserve92px clearance and the unchanged84px/radius28 capsule and upper top4116");
            StaticBody2D blocker = null;
            if (blocked)
            {
                // Isolated extension below the real soffit, ahead of the native
                // spawn (not intersecting it or the player). Never saved to disk.
                blockerShape = new RectangleShape2D { Size = new Vector2(70, 14) };
                blocker = new StaticBody2D { Name = "ClearanceNegativeSoffit",
                    CollisionLayer = CombatCollisionLayers.World,
                    CollisionMask = CombatCollisionLayers.None };
                blocker.AddChild(new CollisionShape2D { Name = "Collision", Shape = blockerShape });
                level.CurrentSectorRoot.GetNode("Gameplay/Collision").AddChild(blocker);
                blocker.GlobalPosition = new Vector2(3515, 4151);
                TestAssert.True(actor.GlobalPosition.X - capsule.Radius > 3550f &&
                    player.GlobalPosition.X + 20f < 3480f && floorTop - (4151f + 7f) == 78f,
                    "negative narrow segment is ahead of the actual actor, clear of both initial bodies, with78px headroom");
            }
            float startX = actor.GlobalPosition.X;
            var phases = new List<(ulong Id, AttackPhase Phase)>();
            observe = frame =>
            {
                if (frame.Timeline.Phase != AttackPhase.None)
                    phases.Add((frame.Timeline.AttackId, frame.Timeline.Phase));
            };
            actor.AttackFrameChanged += observe;
            int stable = 0, longestSupport = 0;
            bool approachSeen = false, blockerContact = false;
            for (int tick = 0; tick < 300; tick++)
            {
                await Frames(tree, 1);
                approachSeen |= actor.CurrentState == BellBackLaborerState.Approach;
                for (int contact = 0; blocker != null && contact < actor.GetSlideCollisionCount(); contact++)
                    blockerContact |= actor.GetSlideCollision(contact).GetCollider() == blocker;
                stable = SupportedBy(actor, floor) ? stable + 1 : 0;
                longestSupport = Math.Max(longestSupport, stable);
                if (phases.Any(item => item.Phase == AttackPhase.Recovery)) break;
            }
            float moved = startX - actor.GlobalPosition.X;
            GD.Print($"CURRENT_BASIN_LABORER_SAMPLE blocked={blocked} gap={floorTop - roofBottom} capsule={capsule.Height} " +
                $"start_x={startX} actor={actor.GlobalPosition} moved_left={moved} " +
                $"support_ticks={longestSupport} player={player.GlobalPosition} recoveries={recoveries} " +
                $"blocker_contact={blockerContact} phases={string.Join(",", phases.Distinct())}");
            ulong id = phases.FirstOrDefault(item => item.Phase == AttackPhase.Telegraph).Id;
            int telegraph = phases.FindIndex(item => item.Id == id && item.Phase == AttackPhase.Telegraph);
            int active = phases.FindIndex(item => item.Id == id && item.Phase == AttackPhase.Active);
            int recovery = phases.FindIndex(item => item.Id == id && item.Phase == AttackPhase.Recovery);
            return new ClearanceObservation(approachSeen, moved, longestSupport, SupportedBy(actor, floor),
                player.IsOnFloor(), player.GetInstanceId() == playerId && world.GetInstanceId() == worldId &&
                    level.CurrentSector == PrologueSectorId.CanyonBasin && world.LayoutVersion == 2,
                recoveries, id, telegraph, active, recovery, blockerContact);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(actor) && observe != null) actor.AttackFrameChanged -= observe;
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryCompleted -= Recovered;
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
            blockerShape?.Dispose();
        }
    }

    private static async Task AssertOriginalWalkwayTopStillSupportsPlayer(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        int recoveries = 0;
        void Recovered(string _) => recoveries++;
        try
        {
            var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            // Existing upper-route walking position: foot at4116, far from triggers.
            player.Position = new Vector2(2800, 4080);
            player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.Equal(2, world.LayoutVersion, "walkway support remains a current full-Main contract");
            world.InitialSector = PrologueSectorId.CanyonBasin;
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = true;
            IsolatedPrologueFixture.Configure(level, "current-walkway-top-support");
            level.RecoveryCompleted += Recovered;
            tree.Root.AddChild(level);
            await Frames(tree, 8);
            var walkway = level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway");
            for (int tick = 0; tick < 24; tick++)
            {
                await Frames(tree, 1);
                TestAssert.True(SupportedBy(player, walkway) &&
                                player.GlobalPosition.DistanceTo(new Vector2(2800, 4080)) < .1f &&
                                recoveries == 0,
                    "thinning only the soffit must preserve 24 physical holds on the original upper walking top");
            }
            GD.Print("CURRENT_BASIN_WALKWAY_SUPPORT_OK original_top=4116 holds=24 recovery=0");
        }
        finally
        {
            level.RecoveryCompleted -= Recovered;
            if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            else level.Free();
        }
    }

    private static bool SupportedBy(CharacterBody2D body, StaticBody2D floor)
    {
        if (!body.IsOnFloor()) return false;
        for (int i = 0; i < body.GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = body.GetSlideCollision(i);
            if (collision.GetCollider() == floor && collision.GetNormal().Y < -.7f) return true;
        }
        return false;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
