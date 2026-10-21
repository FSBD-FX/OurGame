using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Save;

namespace Knight.Tests.Integration;

/// <summary>Task16B observable contracts against isolated storage and generated content.</summary>
public static class ArchivePressureGateIntegrationTests
{
    internal const string RewardId = "canyon.v2.reward.archive-supply";
    internal const string GateId = "canyon.v2.pressure-gate.archive-reward";

    // Would fail if a valid reward is rejected or an unknown discovery is accepted.
    public static void SaveRewardRoundTrip(string storageRoot)
    {
        string caseRoot = Path.Combine(storageRoot, "reward-save");
        Directory.CreateDirectory(caseRoot);
        string slot = Path.Combine(caseRoot, "save.json");
        string legacy = Path.Combine(caseRoot, "prologue-save.json");
        PrologueSaveData before = PrologueSaveData.NewLayoutV2();
        before.Discoveries.Add("canyon.v2.discovery.archive-side-cave");
        before.Discoveries.Add(RewardId);
        bool accepted;
        try { before.Normalize(); accepted = true; }
        catch (InvalidDataException) { accepted = false; }
        TestAssert.True(accepted, "archive reward is accepted by the separate discovery allowlist");

        PrologueSaveStore.SaveAtomic(slot, before);
        PrologueSaveLoadResult loaded = PrologueSaveStore.LoadFullWorld(slot, legacy);
        TestAssert.True(loaded.Status == PrologueSaveLoadStatus.Current &&
                        loaded.Data.Discoveries.Contains(RewardId) &&
                        loaded.Data.Discoveries.Contains("canyon.v2.discovery.archive-side-cave"),
            "reward and existing discovery survive a real save/load round trip");

        PrologueSaveData old = PrologueSaveData.NewLayoutV2();
        old.Discoveries.Add("canyon.v2.discovery.archive-side-cave");
        TestAssert.True(old.Normalize().Discoveries.Count == 1,
            "existing layout2 saves without the reward remain valid");
        PrologueSaveData foreign = PrologueSaveData.NewLayoutV2();
        foreign.Discoveries.Add("canyon.v2.reward.unknown");
        bool rejected;
        try { foreign.Normalize(); rejected = false; }
        catch (InvalidDataException) { rejected = true; }
        TestAssert.True(rejected, "unknown reward IDs remain rejected");
    }

    // Would fail if the generated region omitted the gate/reward or bound the wrong identity.
    public static void GeneratedArchiveContainsFiniteRewardAndGate()
    {
        var packed = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonDescent.tscn");
        TestAssert.True(packed != null, "generated Descent leaf loads");
        PrologueSector sector = packed.Instantiate<PrologueSector>();
        try
        {
            CanyonPressurePlateGate gate = sector.GetNodeOrNull<CanyonPressurePlateGate>(
                "Gameplay/Mechanisms/ArchivePressureGate");
            TestAssert.True(gate != null && gate.MechanismId == GateId,
                "generated Archive has the stable pressure gate");
            PrologueSupplyCache reward = sector.GetNodeOrNull<PrologueSupplyCache>(
                "Gameplay/Rewards/ArchiveSupply");
            TestAssert.True(reward != null && reward.DiscoveryId == RewardId &&
                            reward.HealthAmount == 2,
                "generated Archive has a finite two-health supply with the reward ID");
            TestAssert.True(gate.DoorCollision.Disabled &&
                            gate.DoorCollision.Shape is RectangleShape2D doorShape &&
                            doorShape.Size == new Vector2(40f, 216f) &&
                            gate.GetNode<StaticBody2D>("Door").Position ==
                            new Vector2(460f, 1532f) &&
                            gate.GetNode<GrappleSurface2D>("Door/GrappleSurfaceProfile")
                                .Material == GrappleSurfaceMaterial.Forbidden,
                "generated door has the safe provisional shape and forbidden grapple surface");
            foreach ((string name, Vector2 center, Vector2 size) in new[]
            {
                ("ArchiveSupplyRoof", new Vector2(520f, 1412f), new Vector2(160f, 24f)),
                ("ArchiveSupplyRightWall", new Vector2(620f, 1540f), new Vector2(40f, 280f))
            })
            {
                StaticBody2D body = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
                CanyonPlatformVisual skin = sector.GetNode<CanyonPlatformVisual>(
                    "VisualRoot/GameplayReadable/" + name);
                TestAssert.True(body.Position == center && skin.Position == center &&
                                skin.SkinSize == size &&
                                body.GetNode<CollisionShape2D>("Collision").Shape is
                                    RectangleShape2D shape && shape.Size == size &&
                                body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile")
                                    .Material == GrappleSurfaceMaterial.GripStone,
                    "new alcove roof/right wall each has matching visible stone skin and collision");
            }
            TestAssert.True(sector.GetNode<StaticBody2D>("Gameplay/Collision/ArchiveFloor")
                                .GetNode<CollisionShape2D>("Collision").Shape is
                                    RectangleShape2D floorShape &&
                            floorShape.Size == new Vector2(600f, 40f) &&
                            sector.GetNode<Node2D>("Gameplay/Mechanisms/HiddenPanel")
                                .Position.X == 160f,
                "existing ArchiveFloor support stays intact and HiddenPanel shifts clear of the door");
            TraversalSegment2D route = sector.GetNode<TraversalSegment2D>(
                "Gameplay/TraversalContracts/canyonv2routearchive-to-frozen-detour");
            TestAssert.True(route.Kind == TraversalSegmentKind.SingleJump &&
                            route.RequiredAbilities == TraversalAbility.Jump &&
                            route.LaunchZone == new Rect2(160f, 1568f, 120f, 72f) &&
                            route.LandingZone == new Rect2(40f, 2108f, 140f, 72f),
                "old Archive-to-Frozen single-jump launch and landing remain unchanged");
        }
        finally { sector.Free(); }
    }

    // Would fail if the parent stream definition still protects the old panel location
    // but omits the plate, physical door, or finite reward as key interactions.
    public static void ParentStreamMetadataMatchesArchiveInstallation()
    {
        PrologueLevelController level = GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            PrologueSectorDefinition descent = world.Definitions.Single(definition =>
                definition.Id == PrologueSectorId.CanyonDescent);
            Rect2[] expected =
            [
                new Rect2(136f, 1416f, 48f, 48f), // shifted HiddenPanel
                new Rect2(340f, 1580f, 80f, 60f), // visible floor plate
                new Rect2(440f, 1424f, 40f, 216f), // blocking door
                new Rect2(512f, 1576f, 56f, 56f), // supply overlap shape
                new Rect2(1210f, 520f, 36f, 160f), // unchanged other interaction
                new Rect2(438f, 2478f, 44f, 44f), // Frozen diverter control
                new Rect2(352f, 2465f, 56f, 150f), // Frozen left hazard
                new Rect2(512f, 2465f, 56f, 150f) // Frozen right hazard
            ];
            TestAssert.True(descent.KeyInteractionBounds.SequenceEqual(expected),
                "parent stream metadata tracks shifted panel plus actual plate door and reward bounds");
        }
        finally { level.Free(); }
    }

    // Real physics rays, not rectangle arithmetic, prove the closed generated
    // collision encloses the pocket on its door/roof/wall/floor boundaries.
    public static async Task GeneratedClosedPerimeterBlocksPhysics(SceneTree tree)
    {
        PrologueSector sector = GD.Load<PackedScene>(
            "res://scenes/prologue/sectors/CanyonDescent.tscn").Instantiate<PrologueSector>();
        sector.Position = new Vector2(1280f, 0f);
        tree.Root.AddChild(sector);
        try
        {
            await PhysicsFrames(tree, 5);
            CanyonPressurePlateGate gate = sector.GetNode<CanyonPressurePlateGate>(
                "Gameplay/Mechanisms/ArchivePressureGate");
            TestAssert.True(!gate.IsOpen && !gate.DoorCollision.Disabled &&
                            !gate.Frame.Traversable,
                "clear generated mount commits a truly blocking closed door");
            StaticBody2D door = gate.GetNode<StaticBody2D>("Door");
            StaticBody2D roof = sector.GetNode<StaticBody2D>(
                "Gameplay/Collision/ArchiveSupplyRoof");
            StaticBody2D wall = sector.GetNode<StaticBody2D>(
                "Gameplay/Collision/ArchiveSupplyRightWall");
            StaticBody2D floor = sector.GetNode<StaticBody2D>(
                "Gameplay/Collision/ArchiveFloor");
            TestAssert.True(ReferenceEquals(RayWorld(sector, new Vector2(1690f, 1532f),
                                new Vector2(1790f, 1532f)), door) &&
                            ReferenceEquals(RayWorld(sector, new Vector2(1820f, 1360f),
                                new Vector2(1820f, 1460f)), roof) &&
                            ReferenceEquals(RayWorld(sector, new Vector2(1850f, 1532f),
                                new Vector2(1950f, 1532f)), wall) &&
                            ReferenceEquals(RayWorld(sector, new Vector2(1820f, 1590f),
                                new Vector2(1820f, 1700f)), floor),
                "actual generated door roof wall and floor shapes block four physical entry rays");
        }
        finally
        {
            sector.QueueFree();
            await PhysicsFrames(tree, 2);
        }
    }

    private static GodotObject RayWorld(Node2D source, Vector2 from, Vector2 to)
    {
        using var query = PhysicsRayQueryParameters2D.Create(from, to,
            CombatCollisionLayers.World);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        using var hit = source.GetWorld2D().DirectSpaceState.IntersectRay(query);
        return hit.Count == 0 ? null : hit["collider"].AsGodotObject();
    }

    // Would fail if the recorder ignores the real gate's open latch or records it twice.
    public static async Task RecorderSeesOneRealGateOpen(SceneTree tree)
    {
        var root = new Node2D { Name = "ArchiveGateRecorderFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor", GlobalPosition = new Vector2(1750f, 1650f),
            CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(600f, 20f) }
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = new Vector2(1550f, 1604f);
        root.AddChild(player);
        CanyonPressurePlateGate gate = NewRecorderGate(player);
        root.AddChild(gate);
        var recorder = new ProloguePlaytestRecorder { Name = "Recorder" };
        root.AddChild(recorder);
        tree.Root.AddChild(root);
        var session = new ProloguePlaytestSession("archive-recorder", "2026-09-23T00:00:00Z",
            "AutomatedFixture");
        try
        {
            await PhysicsFrames(tree, 3);
            TestAssert.True(!gate.HasOpened, "recorder baseline begins before plate activation");
            recorder.ConfigureOrderedTelemetryForTest(session, root);
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.Equal(0, Report(session).Mechanisms.Count,
                "closed gate is not a recorded open event");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 8);
            TestAssert.True(gate.HasOpened && gate.DoorCollision.Disabled,
                "recorder fixture has a genuine committed opening by the real player");
            recorder.ObserveOrderedTelemetryForTest();
            recorder.ObserveOrderedTelemetryForTest();
            ProloguePlaytestReport report = Report(session);
            TestAssert.True(report.Mechanisms.Count == 1 &&
                            report.Mechanisms[0].MechanismId == GateId,
                "recorder emits exactly one stable gate-open rising edge");
        }
        finally
        {
            root.QueueFree();
            await PhysicsFrames(tree, 2);
        }
    }

    // Would fail if a late-mounted already-open gate is replayed as a fresh event,
    // or a genuinely new mount's first real opening is missed.
    public static async Task RecorderRebindBaselinesThenTracksNewMount(SceneTree tree)
    {
        var root = new Node2D { Name = "ArchiveRecorderRebindFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor", GlobalPosition = new Vector2(1750f, 1650f),
            CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(600f, 20f) }
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = new Vector2(1660f, 1604f);
        root.AddChild(player);
        CanyonPressurePlateGate first = NewRecorderGate(player);
        root.AddChild(first);
        var recorder = new ProloguePlaytestRecorder { Name = "Recorder" };
        root.AddChild(recorder);
        tree.Root.AddChild(root);
        var session = new ProloguePlaytestSession("archive-recorder-rebind",
            "2026-09-23T00:00:00Z", "AutomatedFixture");
        try
        {
            await PhysicsFrames(tree, 8);
            TestAssert.True(first.HasOpened && first.DoorCollision.Disabled,
                "recorder rebind fixture first has a genuinely opened old mount");
            recorder.ConfigureOrderedTelemetryForTest(session, root);
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.Equal(0, Report(session).Mechanisms.Count,
                "already-open gate is baseline when recorder begins observation");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            root.RemoveChild(first);
            first.Free();
            await PhysicsFrames(tree, 2);
            recorder.ObserveOrderedTelemetryForTest();
            CanyonPressurePlateGate second = NewRecorderGate(player);
            root.AddChild(second);
            await PhysicsFrames(tree, 3);
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.Equal(0, Report(session).Mechanisms.Count,
                "a fresh closed remount does not replay the old open event");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 8);
            recorder.ObserveOrderedTelemetryForTest();
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.True(Report(session).Mechanisms.Count == 1 &&
                            Report(session).Mechanisms[0].MechanismId == GateId,
                "recorder rebind emits exactly one event for new mount's real opening");
        }
        finally
        {
            root.QueueFree();
            await PhysicsFrames(tree, 2);
        }
    }

    private static CanyonPressurePlateGate NewRecorderGate(CharacterBody2D player)
    {
        var gate = new CanyonPressurePlateGate
        {
            Name = "ArchivePressureGate", PlayerBody = player
        };
        var door = new StaticBody2D
        {
            Name = "Door", GlobalPosition = new Vector2(1740f, 1532f),
            CollisionLayer = CombatCollisionLayers.World
        };
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(40f, 216f) }
        });
        gate.AddChild(door);
        return gate;
    }

    // Controlled regional positions/damage are fixture setup, not presentation evidence.
    // Would fail if streaming leaves PlayerBody unbound, the door never commits, or the
    // existing supply consumes full HP/duplicates a claim/fails to persist the reward.
    public static async Task MountedGateAndFiniteReward(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(level,
            "task16b-mounted-reward");
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !flow.HasSave,
                "isolated new full level stays at Title until the explicit StartNew action");
            flow.StartNew();
            TestAssert.True(flow.State == PrologueGameFlowState.Playing,
                "mounted reward fixture starts a real flow in isolated storage");
            CharacterBody2D player = level.Player;
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 12);
            PrologueSector sector = level.GetNodeOrNull<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent");
            TestAssert.True(sector != null && level.CurrentSector == PrologueSectorId.CanyonDescent,
                "real generated Archive sector mounts around the regional fixture position");
            FreezeArchiveActorsForFunctionalFixture(sector);
            CanyonPressurePlateGate gate = sector.GetNode<CanyonPressurePlateGate>(
                "Gameplay/Mechanisms/ArchivePressureGate");
            PrologueSupplyCache reward = sector.GetNode<PrologueSupplyCache>(
                "Gameplay/Rewards/ArchiveSupply");
            TestAssert.True(ReferenceEquals(gate.PlayerBody, player),
                "streamed generated gate binds the actual persistent player body");
            TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled &&
                            gate.Frame.Traversable && !gate.Frame.HazardActive,
                "grounded regional player opens the generated committed door safely");
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            int full = health.MaxHealth;
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            TestAssert.True(health.CurrentHealth == full && !reward.IsSpent,
                "full-health visit leaves the generated finite supply available");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 4);
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Zero, 0f, 16001UL)),
                "controlled runtime fixture injury is accepted before claiming the supply");
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            TestAssert.True(health.CurrentHealth == full && reward.IsSpent &&
                            level.Progress.Discoveries.Contains(RewardId),
                "injured actual player takes at most two HP and consumes one supply");
            TestAssert.True(File.Exists(paths.SavePath) &&
                            PrologueSaveStore.LoadFullWorld(paths.SavePath,
                                Path.Combine(paths.CaseRoot, "prologue-save.json"))
                                .Data.Discoveries.Contains(RewardId),
                "reward claim persists through the actual isolated flow save slot");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 4);
            TestAssert.True(float.IsFinite(health.InvulnerabilitySeconds) &&
                            health.InvulnerabilitySeconds >= 0f &&
                            health.InvulnerabilitySeconds <= 2f,
                "fixture can bound the actual configured health invulnerability interval");
            await PhysicsFrames(tree,
                Math.Clamp((int)Math.Ceiling(health.InvulnerabilitySeconds * 120d) + 6,
                    6, 246));
            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Zero, 0f, 16002UL)),
                "controlled second injury is accepted for duplicate-claim check");
            int injured = health.CurrentHealth;
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            TestAssert.True(health.CurrentHealth == injured && reward.IsSpent,
                "re-entering a spent supply never heals twice");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    // Would fail if the generated gate closes through an occupied alcove on checkpoint
    // reset, retains the local latch after death, or resurrects the claimed reward.
    public static async Task MountedCheckpointDeathLifecycle(SceneTree tree,
        bool deathResetOnly = false)
    {
        PrologueLevelController level = NewIsolatedLevel("task16b-checkpoint-death",
            out var flow, out _);
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            flow.StartNew();
            CharacterBody2D player = level.Player;
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 12);
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            CanyonPressurePlateGate gate = level.GetNode<CanyonPressurePlateGate>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            TestAssert.True(gate.IsOpen && gate.HasOpened,
                "generated gate is genuinely opened before checkpoint reset");
            if (!deathResetOnly)
            {
                player.GlobalPosition = new Vector2(1820f, 1604f);
                await PhysicsFrames(tree, 3);
                world.ResetCurrentSectorForCheckpoint();
                TestAssert.True(!gate.HasOpened && gate.IsOpen && gate.DoorCollision.Disabled,
                    "checkpoint reset clears local latch but cannot close through interior player");
                await PhysicsFrames(tree, 3);
                TestAssert.True(gate.IsOpen && gate.Frame.Traversable &&
                                !gate.Frame.HazardActive,
                    "occupied reset keeps actual generated collision traversable and harmless");
                player.GlobalPosition = new Vector2(1550f, 1604f);
                await PhysicsFrames(tree, 130);
                TestAssert.True(!gate.IsOpen && !gate.DoorCollision.Disabled &&
                                !gate.Frame.Traversable,
                    "after occupied player leaves, generated door finishes its harmless close");

                player.GlobalPosition = new Vector2(1660f, 1604f);
                await PhysicsFrames(tree, 12);
                TestAssert.True(gate.IsOpen && gate.HasOpened,
                    "same mounted gate can be opened again after reset");
            }
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Zero, 0f,
                    16031UL)),
                "controlled lifecycle injury is accepted before real reward overlap");
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            TestAssert.True(level.Progress.Discoveries.Contains(RewardId),
                "lifecycle fixture actually claims the finite reward before death");
            TestAssert.True(float.IsFinite(health.InvulnerabilitySeconds) &&
                            health.InvulnerabilitySeconds >= 0f &&
                            health.InvulnerabilitySeconds <= 2f,
                "death fixture can bound the configured injury cooldown");
            await PhysicsFrames(tree,
                Math.Clamp((int)Math.Ceiling(health.InvulnerabilitySeconds * 120d) + 6,
                    6, 246));
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,
                    Vector2.Zero, 0f, 16032UL)),
                "controlled lethal fixture enters the actual death recovery flow");
            for (int frame = 0; frame < 120 &&
                 (flow.State != PrologueGameFlowState.Playing ||
                  health.CurrentHealth != health.MaxHealth); frame++)
                await PhysicsFrames(tree, 1);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                            health.CurrentHealth == health.MaxHealth &&
                            level.Progress.Discoveries.Contains(RewardId),
                "death recovery restores full health while keeping spent reward progress");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await PhysicsFrames(tree, 12);
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            CanyonPressurePlateGate restoredGate = level.GetNode<CanyonPressurePlateGate>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
            PrologueSupplyCache reward = level.GetNode<PrologueSupplyCache>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Rewards/ArchiveSupply");
            TestAssert.True(!restoredGate.HasOpened && !restoredGate.IsOpen &&
                            !restoredGate.Frame.Traversable && reward.IsSpent,
                "death/reset leaves gate locally closed while collected reward stays spent; " +
                $"opened={restoredGate.HasOpened} open={restoredGate.IsOpen} " +
                $"traversable={restoredGate.Frame.Traversable} spent={reward.IsSpent} " +
                $"current={world.CurrentSector}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    // Regional fixture transfers between distant sectors deliberately exercise the
    // actual streamer's unload/remount, never claim normal route traversal.
    public static async Task MountedUnloadRemount(SceneTree tree, bool collectReward)
    {
        PrologueLevelController level = NewIsolatedLevel(collectReward
                ? "task16b-remount-spent" : "task16b-remount-full",
            out var flow, out _);
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            flow.StartNew();
            CharacterBody2D player = level.Player;
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 12);
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            CanyonPressurePlateGate firstGate = level.GetNode<CanyonPressurePlateGate>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            if (collectReward)
                TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Zero, 0f,
                        16041UL)),
                    "controlled remount fixture injury is accepted before first visit");
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            TestAssert.True(level.Progress.Discoveries.Contains(RewardId) == collectReward,
                "first real overlap records only an injured reward claim");
            ulong firstId = firstGate.GetInstanceId();
            player.GlobalPosition = new Vector2(5500f, 1100f);
            await PhysicsFrames(tree, 12);
            TestAssert.True(world.CurrentSector == PrologueSectorId.LostTemple &&
                            !world.MountedSectorIds.Contains(PrologueSectorId.CanyonDescent),
                "distant regional fixture transfer actually unloads Descent");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await PhysicsFrames(tree, 12);
            CanyonPressurePlateGate remounted = level.GetNode<CanyonPressurePlateGate>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            PrologueSupplyCache reward = level.GetNode<PrologueSupplyCache>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Rewards/ArchiveSupply");
            TestAssert.True(remounted.GetInstanceId() != firstId &&
                            ReferenceEquals(remounted.PlayerBody, player) &&
                            !remounted.HasOpened && !remounted.IsOpen &&
                            !remounted.Frame.Traversable && reward.IsSpent == collectReward,
                "fresh generated remount resets gate phase but preserves reward state");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 12);
            TestAssert.True(remounted.IsOpen && remounted.DoorCollision.Disabled,
                "fresh remount binds the same real player and reopens on its plate");
            if (collectReward)
            {
                TestAssert.True(float.IsFinite(health.InvulnerabilitySeconds) &&
                                health.InvulnerabilitySeconds >= 0f &&
                                health.InvulnerabilitySeconds <= 2f,
                    "spent-remount fixture bounds the actual injury cooldown");
                await PhysicsFrames(tree,
                    Math.Clamp((int)Math.Ceiling(health.InvulnerabilitySeconds * 120d) + 6,
                        6, 246));
            }
            TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Zero, 0f,
                    collectReward ? 16042UL : 16043UL)),
                "controlled remount injury is accepted before revisiting supply");
            int injured = health.CurrentHealth;
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            TestAssert.True(collectReward
                    ? health.CurrentHealth == injured && reward.IsSpent
                    : health.CurrentHealth == injured + 1 && reward.IsSpent &&
                      level.Progress.Discoveries.Contains(RewardId),
                "spent remount stays inert while full-health reserve remains claimable");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    // A real persistent player is already penetrating the authored door footprint
    // when streaming mounts Descent. Staged collision restoration may not crush it.
    public static async Task GeneratedOccupiedMountStaysSafe(SceneTree tree)
    {
        PrologueLevelController level = NewIsolatedLevel("task16b-occupied-mount",
            out var flow, out _);
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            flow.StartNew();
            CharacterBody2D player = level.Player;
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            int before = health.CurrentHealth;
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            CanyonPressurePlateGate priorGate = world.GetNode<CanyonPressurePlateGate>(
                "CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
            ulong priorGateInstance = priorGate.GetInstanceId();
            GD.Print("TASK16B_OCCUPIED_PREUNLOAD " +
                OccupiedMountSnapshot(world, player, health, 0));
            // Entrance preloads Descent. A distant fixture transfer must first
            // retire that closed gate before this can test an occupied NEW mount.
            player.GlobalPosition = new Vector2(5500f, 1100f);
            // Fixture transfer, not a playable descent: reset both private motor
            // velocity and fall origin instead of counting the teleport as a fall.
            player.GetNode<PlayerMotor>("Motor").ResetTransientState();
            await PhysicsFrames(tree, 12);
            TestAssert.True(world.CurrentSector == PrologueSectorId.LostTemple &&
                            !world.MountedSectorIds.Contains(PrologueSectorId.CanyonDescent),
                "occupied-mount fixture actually unloads the preloaded Descent gate");
            GD.Print("TASK16B_OCCUPIED_DIAGNOSTIC " +
                OccupiedMountSnapshot(world, player, health, 0));
            player.GlobalPosition = new Vector2(1740f, 1604f);
            player.GetNode<PlayerMotor>("Motor").ResetTransientState();
            for (int frame = 1; frame <= 12; frame++)
            {
                await PhysicsFrames(tree, 1);
                GD.Print("TASK16B_OCCUPIED_DIAGNOSTIC " +
                    OccupiedMountSnapshot(world, player, health, frame));
            }
            CanyonPressurePlateGate gate = level.GetNode<CanyonPressurePlateGate>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            TestAssert.True(gate.GetInstanceId() != priorGateInstance,
                "occupied player triggers a fresh generated Descent gate instance");
            TestAssert.True(ReferenceEquals(gate.PlayerBody, player) &&
                            gate.IsOpen && gate.DoorCollision.Disabled &&
                            gate.Frame.Traversable && !gate.Frame.HazardActive &&
                            Mathf.Abs(player.GlobalPosition.X - 1740f) < 5f &&
                            health.CurrentHealth == before,
                "occupied generated mount keeps real player uncrushed and Frame aligned with collision");
            TestAssert.True(player.GetNode<PlayerMotor>("Motor").LastFallDamage == 0 &&
                            !player.GetNode<PlayerMotor>("Motor").LastFallDamageApplied,
                "labeled fixture transfers are not interpreted as a thousand-unit playable fall; no damage was healed or disabled");
            world.ResetCurrentSectorForCheckpoint();
            await PhysicsFrames(tree, 3);
            TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled &&
                            gate.Frame.Traversable && health.CurrentHealth == before,
                "occupied generated checkpoint reset cannot enable door through real player");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static string OccupiedMountSnapshot(SeamlessPrologueWorld world,
        CharacterBody2D player, HealthComponent health, int frame)
    {
        CanyonPressurePlateGate gate = world.GetNodeOrNull<CanyonPressurePlateGate>(
            "CanyonDescent/Gameplay/Mechanisms/ArchivePressureGate");
        string gateState = gate == null ? "absent" :
            $"instance={gate.GetInstanceId()} opened={gate.HasOpened} " +
            $"open={gate.IsOpen} disabled={gate.DoorCollision.Disabled} " +
            $"traversable={gate.Frame.Traversable} hazard={gate.Frame.HazardActive} " +
            $"bound={ReferenceEquals(gate.PlayerBody, player)}";
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        return $"frame={frame} current={world.CurrentSector} " +
            $"mounted=[{string.Join(",", world.MountedSectorIds)}] gate={gateState} " +
            $"position={player.GlobalPosition} velocity={player.Velocity} " +
            $"health={health.CurrentHealth}/{health.MaxHealth} floor={player.IsOnFloor()} " +
            $"lastFallHeight={motor.LastFallHeight} lastFallDamage={motor.LastFallDamage} fallApplied={motor.LastFallDamageApplied}";
    }

    // Test-only resettable probes measure the existing deferred recovery contract:
    // none has fired at RecoveryCompleted; then the current destination and only a
    // still-mounted identical departing instance receive exactly one reset each.
    public static async Task RecoveryResetTargetsOnceAfterCompletion(SceneTree tree,
        string scenario)
    {
        TestAssert.True(scenario is "same" or "retained" or "absent",
            "recovery reset probe accepts one of three explicit scenarios");
        PrologueLevelController level = NewIsolatedLevel("task16b-recovery-" + scenario,
            out var flow, out _);
        SeamlessPrologueWorld world = null;
        Action<PrologueSector> onMount = null;
        PrologueLevelController.RecoveryCompletedEventHandler onCompleted = null;
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            flow.StartNew();
            TestAssert.True(flow.State == PrologueGameFlowState.Playing,
                "recovery count fixture explicitly starts a real isolated run");
            world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            CharacterBody2D player = level.Player;
            var destinationCount = new ArchiveResetCount();
            var sourceCount = scenario == "same" ? destinationCount : new ArchiveResetCount();
            PrologueSectorId sourceId = scenario switch
            {
                "same" => PrologueSectorId.CanyonEntrance,
                "retained" => PrologueSectorId.CanyonDescent,
                _ => PrologueSectorId.LostTemple
            };
            if (scenario != "same")
            {
                // Regional positioning is fixture setup, not traversal proof.
                player.GlobalPosition = scenario == "retained"
                    ? new Vector2(1660f, 1604f) : new Vector2(5500f, 1100f);
                await PhysicsFrames(tree, 12);
            }
            bool sourceMounted = world.TryGetMountedSector(sourceId,
                out PrologueSector source);
            TestAssert.True(world.CurrentSector == sourceId && sourceMounted,
                "recovery count fixture genuinely occupies its departing sector");
            if (scenario != "same") FreezeSectorActorsForFunctionalFixture(source);
            ulong sourceInstance = source.GetInstanceId();
            source.AddChild(new ArchiveCheckpointResetCountProbe
            {
                Name = "Task16BRecoveryResetProbe", Counter = sourceCount
            });
            onMount = sector =>
            {
                if (sector.SectorId != PrologueSectorId.CanyonEntrance ||
                    sector.GetNodeOrNull<ArchiveCheckpointResetCountProbe>(
                        "Task16BRecoveryResetProbe") != null) return;
                sector.AddChild(new ArchiveCheckpointResetCountProbe
                {
                    Name = "Task16BRecoveryResetProbe", Counter = destinationCount
                });
            };
            world.SectorMounted += onMount;
            if (world.TryGetMountedSector(PrologueSectorId.CanyonEntrance,
                    out PrologueSector existingDestination))
                onMount(existingDestination);
            int completions = 0;
            int sourceAtCompletion = -1;
            int destinationAtCompletion = -1;
            onCompleted = _ =>
            {
                completions++;
                sourceAtCompletion = sourceCount.Count;
                destinationAtCompletion = destinationCount.Count;
            };
            level.RecoveryCompleted += onCompleted;
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.CurrentHealth == health.MaxHealth &&
                            health.ApplyDamage(new DamageRequest(health.CurrentHealth,
                                Vector2.Zero, 0f, scenario switch
                                {
                                    "same" => 16051UL,
                                    "retained" => 16052UL,
                                    _ => 16053UL
                                })),
                "controlled lethal hit begins the actual deferred recovery lifecycle");
            for (int frame = 0; frame < 90 && completions == 0; frame++)
                await PhysicsFrames(tree, 1);
            TestAssert.True(completions == 1 && sourceAtCompletion == 0 &&
                            destinationAtCompletion == 0,
                $"reset remains pending at RecoveryCompleted: scenario={scenario} " +
                $"events={completions} source={sourceAtCompletion} " +
                $"destination={destinationAtCompletion}");
            await PhysicsFrames(tree, 3);
            int expectedSource = scenario == "absent" ? 0 : 1;
            TestAssert.True(completions == 1 && sourceCount.Count == expectedSource &&
                            destinationCount.Count == 1 &&
                            world.CurrentSector == PrologueSectorId.CanyonEntrance,
                $"deferred reset exactly once per surviving sector: scenario={scenario} " +
                $"source={sourceCount.Count} destination={destinationCount.Count} " +
                $"events={completions} current={world.CurrentSector}");
            if (scenario == "retained")
                TestAssert.True(world.TryGetMountedSector(sourceId,
                                    out PrologueSector retained) &&
                                retained.GetInstanceId() == sourceInstance,
                    "departing source remains the exact mounted instance for second reset");
            if (scenario == "absent")
                TestAssert.True(!world.MountedSectorIds.Contains(sourceId),
                    "unloaded departing source is not remounted by deferred reset");
            GD.Print($"TASK16B_RECOVERY_RESET_OK scenario={scenario} " +
                $"source={sourceCount.Count} destination={destinationCount.Count}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(world) && onMount != null)
                world.SectorMounted -= onMount;
            if (GodotObject.IsInstanceValid(level) && onCompleted != null)
                level.RecoveryCompleted -= onCompleted;
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    // Producer and consumer run in distinct engine processes with the SAME external
    // fixture slot. No Progress mutation or synthetic collection stands in for overlap.
    public static async Task ContinueProducer(SceneTree tree, string storageRoot,
        bool collectReward)
    {
        PrologueLevelController level = NewIsolatedLevel(collectReward
                ? "task16b-continue-spent" : "task16b-continue-full", out var flow,
            out var paths);
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !flow.HasSave,
                "cross-process producer starts from an isolated empty Title slot");
            flow.StartNew();
            TestAssert.True(flow.State == PrologueGameFlowState.Playing,
                "cross-process producer explicitly starts a real new run");
            CharacterBody2D player = level.Player;
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 12);
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            if (collectReward)
            {
                HealthComponent health = player.GetNode<HealthComponent>("Health");
                TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Zero, 0f,
                        16011UL)),
                    "controlled producer injury is accepted before real supply overlap");
            }
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            PrologueSupplyCache reward = level.GetNode<PrologueSupplyCache>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Rewards/ArchiveSupply");
            TestAssert.True(reward.IsSpent == collectReward &&
                            level.Progress.Discoveries.Contains(RewardId) == collectReward,
                "producer actual overlap stores only the injured supply claim");
            TestAssert.True(File.Exists(paths.SavePath),
                "producer real game flow writes an isolated on-disk save");
            PrologueSaveLoadResult stored = PrologueSaveStore.LoadFullWorld(paths.SavePath,
                Path.Combine(paths.CaseRoot, "prologue-save.json"));
            TestAssert.True(stored.Status == PrologueSaveLoadStatus.Current &&
                            stored.Data.Discoveries.Contains(RewardId) == collectReward,
                "producer on-disk slot matches observed reward state");
            string marker = SlotMarker(storageRoot);
            File.WriteAllLines(marker, new[]
            {
                paths.CaseRoot,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(paths.SavePath)))
            });
            GD.Print($"TASK16B_CONTINUE_PRODUCER_OK collected={collectReward} " +
                $"slot={paths.SavePath}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    public static async Task ContinueConsumer(SceneTree tree, string storageRoot,
        bool expectCollected)
    {
        (IsolatedPrologueFixturePaths shared, string producerSha) =
            ReadProducerSlot(storageRoot);
        PrologueLevelController level = NewIsolatedLevel("task16b-continue-consumer",
            out var flow, out _);
        // Configure already set all three paths before AddChild; point each at the
        // producer's exact fixture only after validating that external slot.
        flow.SavePath = shared.SavePath;
        flow.SettingsPath = shared.SettingsPath;
        level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory =
            shared.PlaytestsDirectory;
        TestAssert.True(HashFile(shared.SavePath) == producerSha,
            "consumer reads the unchanged producer save before full-level AddChild");
        tree.Root.AddChild(level);
        try
        {
            await PhysicsFrames(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && flow.HasSave &&
                            flow.Save.Discoveries.Contains(RewardId) == expectCollected,
                "later process sees the producer slot at Title without auto-StartNew overwrite");
            flow.Continue();
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                            level.Progress.Discoveries.Contains(RewardId) == expectCollected,
                "actual Continue restores the on-disk reward state");
            TestAssert.True(HashFile(shared.SavePath) == producerSha,
                "Continue never overwrites or fabricates the producer save");
            CharacterBody2D player = level.Player;
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 12);
            FreezeArchiveActorsForFunctionalFixture(level.GetNode<PrologueSector>(
                "WorldRoot/Sectors/CanyonDescent"));
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            PrologueSupplyCache reward = level.GetNode<PrologueSupplyCache>(
                "WorldRoot/Sectors/CanyonDescent/Gameplay/Rewards/ArchiveSupply");
            TestAssert.True(reward.IsSpent == expectCollected &&
                            level.Progress.Discoveries.Contains(RewardId) == expectCollected,
                "remounted generated reward respects spent or full-health-available Continue state");
            TestAssert.True(PrologueSaveStore.LoadFullWorld(shared.SavePath,
                                Path.Combine(shared.CaseRoot, "prologue-save.json"))
                                .Data.Discoveries.Contains(RewardId) == expectCollected,
                "full-health remount or spent revisit does not save a false reward claim");
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.CurrentHealth == health.MaxHealth,
                "Continue restores default full player health without inventing HP persistence");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await PhysicsFrames(tree, 4);
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Zero, 0f,
                    expectCollected ? 16021UL : 16022UL)),
                "controlled consumer injury is accepted after Continue");
            int injured = health.CurrentHealth;
            player.GlobalPosition = new Vector2(1820f, 1604f);
            await PhysicsFrames(tree, 5);
            if (expectCollected)
                TestAssert.True(health.CurrentHealth == injured && reward.IsSpent &&
                                PrologueSaveStore.LoadFullWorld(shared.SavePath,
                                    Path.Combine(shared.CaseRoot, "prologue-save.json"))
                                    .Data.Discoveries.Contains(RewardId),
                    "spent reward cannot heal or be claimed again after cross-process Continue");
            else
                TestAssert.True(health.CurrentHealth == health.MaxHealth && reward.IsSpent &&
                                PrologueSaveStore.LoadFullWorld(shared.SavePath,
                                    Path.Combine(shared.CaseRoot, "prologue-save.json"))
                                    .Data.Discoveries.Contains(RewardId),
                    "full-health unspent reward remains claimable after cross-process Continue");
            GD.Print($"TASK16B_CONTINUE_CONSUMER_OK collected={expectCollected} " +
                $"slot={shared.SavePath}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static PrologueLevelController NewIsolatedLevel(string caseName,
        out PrologueGameFlowController flow, out IsolatedPrologueFixturePaths paths)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        paths = IsolatedPrologueFixture.Configure(level, caseName);
        return level;
    }

    private static void FreezeArchiveActorsForFunctionalFixture(PrologueSector sector)
    {
        // Functional fixture isolation only: suppresses behavior callbacks without
        // changing ProcessMode or CollisionObject2D's disable policy, so live actor
        // bodies and every world-blocking shape remain in the physics world.
        // The separate normal-window input proof keeps this encounter fully active.
        FreezeSectorActorsForFunctionalFixture(sector);
    }

    private static void FreezeSectorActorsForFunctionalFixture(PrologueSector sector)
    {
        Node groups = sector.GetNode("Gameplay/Encounters");
        for (int child = 0; child < groups.GetChildCount(); child++)
            if (groups.GetChild(child) is CanyonEncounterGroup group)
            {
                group.SetPhysicsProcess(false);
                group.SetProcess(false);
            }
        FreezeActorCallbacks(sector.GetNode("Gameplay/Actors"));
    }

    private static void FreezeActorCallbacks(Node root)
    {
        root.SetPhysicsProcess(false);
        root.SetProcess(false);
        // GetChild is borrowed; no GetChildren Array wrapper or borrowed-node disposal.
        for (int child = 0; child < root.GetChildCount(); child++)
            FreezeActorCallbacks(root.GetChild(child));
    }

    private static string SlotMarker(string storageRoot) =>
        Path.Combine(storageRoot, "task16b-continue-slot.txt");

    private static (IsolatedPrologueFixturePaths Paths, string Sha) ReadProducerSlot(
        string storageRoot)
    {
        string[] lines = File.ReadAllLines(SlotMarker(storageRoot));
        if (lines.Length != 2 || lines[1].Length != 64 ||
            !lines[1].All(Uri.IsHexDigit) || !Path.IsPathFullyQualified(lines[0]))
            throw new InvalidDataException("Invalid producer slot marker.");
        string caseRoot = Path.GetFullPath(lines[0]);
        string expectedParent = Path.Combine(Path.GetFullPath(storageRoot), "full-regression");
        if (!string.Equals(Path.GetDirectoryName(caseRoot), expectedParent,
                StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(caseRoot).StartsWith("task16b-continue-",
                StringComparison.Ordinal) || !Directory.Exists(caseRoot))
            throw new InvalidDataException("Producer slot escaped the isolated case root.");
        for (DirectoryInfo directory = new(caseRoot); directory != null;
             directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Producer slot rejects reparse ancestors.");
        var paths = new IsolatedPrologueFixturePaths(caseRoot,
            Path.Combine(caseRoot, "save.json"), Path.Combine(caseRoot, "settings.json"),
            Path.Combine(caseRoot, "playtests"));
        if (!File.Exists(paths.SavePath) || !File.Exists(paths.SettingsPath))
            throw new InvalidDataException("Producer save/settings files are missing.");
        return (paths, lines[1]);
    }

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static ProloguePlaytestReport Report(ProloguePlaytestSession session) =>
        session.BuildReport(PrologueSectorId.CanyonDescent, PrologueObjective.ReachBasin,
            "2026-09-23T00:00:01Z");

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}

internal sealed class ArchiveResetCount
{
    public int Count;
}

internal partial class ArchiveCheckpointResetCountProbe : Node, ICheckpointResettable
{
    public ArchiveResetCount Counter { get; set; }

    public void ResetForCheckpoint() => Counter.Count++;
}
