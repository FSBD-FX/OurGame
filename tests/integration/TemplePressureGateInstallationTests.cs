using System;
using System.IO;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Save;

namespace Knight.Tests.Integration;

// Task18 preparation fixtures only: no mount, physics, input, healing or route claim.
public static class TemplePressureGateInstallationTests
{
    internal const string GateId = "canyon.v2.pressure-gate.temple-reward";
    internal const string RewardId = "canyon.v2.reward.temple-supply";
    private static readonly Vector2 TempleOrigin = new(5120f, 720f);

    // Catches lost per-installation authoring through real Godot serialization, not attribute text.
    public static void GateConfigurationRoundTrip()
    {
        AssertGateRoundTrip(false);
        GD.Print("TASK18_ARCHIVE_DEFAULT_CONFIGURATION_OK fixture=true fullRoute=false");
        AssertGateRoundTrip(true);
    }

    private static void AssertGateRoundTrip(bool temple)
    {
        var original = new CanyonPressurePlateGate { Name = "GateConfigurationFixture" };
        using var packed = new PackedScene();
        Node restored = null;
        try
        {
            if (temple)
            {
                original.MechanismId = GateId;
                original.PlateWorldRect = new Rect2(5868, 900, 60, 60);
                original.InteriorWorldRect = new Rect2(5960, 760, 100, 200);
                original.AntiCrushWorldRect = new Rect2(5910, 760, 70, 200);
            }
            TestAssert.True(packed.Pack(original) == Error.Ok,
                "actual off-tree gate configuration pack succeeds");
            restored = packed.Instantiate();
            TestAssert.True(restored is CanyonPressurePlateGate && !restored.IsInsideTree(),
                "configuration roundtrip re-instantiates the correct off-tree CSharp script class");
            var gate = (CanyonPressurePlateGate)restored;
            TestAssert.True(gate.MechanismId == (temple ? GateId : "canyon.v2.pressure-gate.archive-reward") &&
                            gate.OpenGraceSeconds == 1.40d && gate.ClosingWarningSeconds == .55d &&
                            gate.PlayerBody == null,
                "gate roundtrip preserves identity defaults timing and absent runtime binding");
            if (temple)
                TestAssert.True(gate.PlateWorldRect == new Rect2(5868, 900, 60, 60) &&
                                gate.InteriorWorldRect == new Rect2(5960, 760, 100, 200) &&
                                gate.AntiCrushWorldRect == new Rect2(5910, 760, 70, 200),
                    "Temple gate configuration survives actual PackedScene roundtrip");
            else
                TestAssert.True(gate.PlateWorldRect == new Rect2(1620, 1580, 80, 60) &&
                                gate.InteriorWorldRect == new Rect2(1760, 1424, 120, 216) &&
                                gate.AntiCrushWorldRect == new Rect2(1700, 1424, 80, 216),
                    "Archive default gate configuration remains unchanged after actual pack");
        }
        finally
        {
            restored?.Free();
            original.Free();
        }
    }

    // Catches rejection/loss of the new finite reward, graph pollution, or relaxed unknown-ID validation.
    public static void SaveRewardRoundTrip(string storageRoot)
    {
        string caseRoot = Path.Combine(storageRoot, "reward-save");
        TestAssert.True(!Directory.Exists(caseRoot) && !File.Exists(caseRoot),
            "Temple reward fixture starts in a fresh case-owned subdirectory");
        Directory.CreateDirectory(caseRoot);
        string slot = Path.Combine(caseRoot, "save.json");
        string legacy = Path.Combine(caseRoot, "prologue-save.json");
        PrologueSaveData before = PrologueSaveData.NewLayoutV2();
        before.Discoveries.Add("canyon.v2.discovery.archive-side-cave");
        before.Discoveries.Add(RewardId);
        bool accepted;
        try { before.Normalize(); accepted = true; }
        catch (InvalidDataException) { accepted = false; }
        TestAssert.True(accepted, "Temple reward is accepted by the separate reward allowlist");
        TestAssert.True(CanyonWorldLayout.PersistableRewardIds.Contains(RewardId) &&
                        CanyonWorldLayout.StableDiscoveryIds.SequenceEqual(new[]
                        { "canyon.v2.discovery.archive-side-cave", "canyon.v2.discovery.frozen-detour" }) &&
                        !CanyonWorldLayout.StableMechanismIds.Contains(GateId),
            "Temple reward is separate from graph discoveries and local door state is not persisted");
        PrologueSaveStore.SaveAtomic(slot, before);
        PrologueSaveLoadResult loaded = PrologueSaveStore.LoadFullWorld(slot, legacy);
        TestAssert.True(loaded.Status == PrologueSaveLoadStatus.Current &&
                        loaded.Data.Discoveries.Count == 2 &&
                        loaded.Data.Discoveries.Contains(RewardId) &&
                        loaded.Data.Discoveries.Contains("canyon.v2.discovery.archive-side-cave"),
            "Temple reward and existing discovery survive actual SaveAtomic and LoadFullWorld");
        PrologueSaveData old = PrologueSaveData.NewLayoutV2();
        old.Discoveries.Add("canyon.v2.discovery.archive-side-cave");
        TestAssert.True(old.Normalize().Discoveries.Count == 1,
            "existing layout2 saves without Temple reward remain valid");
        string oldSlot = Path.Combine(caseRoot, "existing-save.json");
        PrologueSaveStore.SaveAtomic(oldSlot, old);
        TestAssert.True(PrologueSaveStore.LoadFullWorld(oldSlot, legacy).Data.Discoveries
                .SequenceEqual(new[] { "canyon.v2.discovery.archive-side-cave" }),
            "existing save without Temple reward round-trips unchanged");
        PrologueSaveData foreign = PrologueSaveData.NewLayoutV2();
        foreign.Discoveries.Add("canyon.v2.reward.unknown");
        bool rejected;
        try { foreign.Normalize(); rejected = false; }
        catch (InvalidDataException) { rejected = true; }
        TestAssert.True(rejected, "unknown reward IDs remain rejected");
    }

    // Catches omitted/misbound installation, mismatched physical/visible terrain, or changed old contracts.
    // Literal world expectations come from the revised candidate, not the builder under test.
    public static void GeneratedTempleContainsFiniteRewardAndGate()
    {
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn");
        TestAssert.True(packed != null, "generated LostTemple leaf loads");
        PrologueSector sector = packed.Instantiate<PrologueSector>();
        try
        {
            TestAssert.True(!sector.IsInsideTree() && sector.Position == Vector2.Zero &&
                            sector.SectorId == PrologueSectorId.LostTemple &&
                            sector.Bounds == new Rect2(0, 0, 2560, 720) &&
                            CanyonWorldLayout.SectorBounds[PrologueSectorId.LostTemple] ==
                                new Rect2(5120, 720, 2560, 720),
                "off-tree Temple local origin resolves to explicit world origin 5120,720");
            AssertOldContracts(sector);
            AssertParentMetadata(false);
            GD.Print("TASK18_TEMPLE_OLD_CONTRACTS_OK fixture=true fullRoute=false");
            CanyonPressurePlateGate gate = sector.GetNodeOrNull<CanyonPressurePlateGate>(
                "Gameplay/Mechanisms/TemplePressureGate");
            TestAssert.True(gate != null && gate.MechanismId == GateId,
                "generated Temple has the stable pressure gate");
            TestAssert.True(gate.PlateWorldRect == new Rect2(5868, 900, 60, 60) &&
                            gate.InteriorWorldRect == new Rect2(5960, 760, 100, 200) &&
                            gate.AntiCrushWorldRect == new Rect2(5910, 760, 70, 200) &&
                            gate.OpenGraceSeconds == 1.40d && gate.ClosingWarningSeconds == .55d &&
                            gate.GetNodeOrNull<CanyonPressureGateVisual>("ReadableGate") != null,
                "packed Temple retains exact world plate interior anti-crush and reused timing/presentation");
            StaticBody2D door = gate.GetNode<StaticBody2D>("Door");
            TestAssert.True(WorldShape(sector, door) == new Rect2(5930, 760, 30, 200) &&
                            gate.DoorCollision.Disabled &&
                            door.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material ==
                                GrappleSurfaceMaterial.Forbidden,
                "Temple door has exact proposed closed shape and safe provisional disabled collision");
            foreach ((string name, Rect2 world, GrappleSurfaceMaterial material) in new[]
            {
                ("TempleSupplyShelf", new Rect2(5865, 960, 215, 40), GrappleSurfaceMaterial.TempleMetal),
                ("TempleSupplyGripBeam", new Rect2(5805, 820, 50, 40), GrappleSurfaceMaterial.TempleMetal),
                ("TempleSupplyBeamSupport", new Rect2(5835, 760, 20, 70), GrappleSurfaceMaterial.Forbidden),
                ("TempleSupplyRightWall", new Rect2(6060, 760, 20, 200), GrappleSurfaceMaterial.Forbidden),
                ("TempleSupplyCeiling", new Rect2(5536, 720, 784, 40), GrappleSurfaceMaterial.Forbidden),
                ("TempleSupplyLeftPier", new Rect2(5536, 760, 20, 240), GrappleSurfaceMaterial.Forbidden),
                ("TempleSupplyEasternSoffit", new Rect2(6215, 760, 105, 120), GrappleSurfaceMaterial.Forbidden)
            }) AssertSurface(sector, name, world, material);
            PrologueSupplyCache reward = sector.GetNodeOrNull<PrologueSupplyCache>(
                "Gameplay/Rewards/TempleSupply");
            TestAssert.True(reward != null && reward.DiscoveryId == RewardId &&
                            reward.HealthAmount == 2 &&
                            LocalPosition(sector, reward) + TempleOrigin == new Vector2(6010, 924) &&
                            reward.GetNode<CollisionShape2D>("Collision").Shape is CircleShape2D circle &&
                            circle.Radius == 28f && reward.GetNodeOrNull<Node2D>("AvailableVisual") != null &&
                            reward.GetNodeOrNull<Node2D>("SpentVisual") != null,
                "Temple has reused finite two-health supply at exact candidate center with visible states");
            AssertParentMetadata(true);
        }
        finally { sector.Free(); }
    }

    private static void AssertOldContracts(PrologueSector sector)
    {
        TestAssert.True(CanyonWorldLayout.Nodes.Where(n => n.SectorId == PrologueSectorId.LostTemple)
                .Select(n => (n.Id, n.WorldBounds)).SequenceEqual(new[]
                {
                    (CanyonRouteNodeId.SealedCourtyard, new Rect2(5120, 720, 960, 720)),
                    (CanyonRouteNodeId.BossHall, new Rect2(6080, 720, 1200, 720)),
                    (CanyonRouteNodeId.SafeExit, new Rect2(7280, 720, 400, 720))
                }), "Temple installation adds no graph node or altered old world region");
        foreach ((string name, Rect2 world) in new[]
        {
            ("CourtyardFloor", new Rect2(5120, 1240, 520, 40)),
            ("CheckpointFloor", new Rect2(5680, 1116, 400, 40)),
            ("CourtyardStep", new Rect2(5540, 1196, 200, 40)),
            ("ArenaFloor", new Rect2(6080, 1116, 1600, 40)),
            ("BossCeilingGrip", new Rect2(6300, 840, 600, 40))
        }) AssertSurface(sector, name, world, GrappleSurfaceMaterial.TempleMetal);
        BellChainMechanism chain = sector.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
        TestAssert.True(chain.Position == new Vector2(560, 250) &&
                        chain.ShortcutId.ToString() == "canyon.v2.temple-bell-gate" &&
                        chain.DoorHeight == 240f &&
                        WorldShape(sector, chain.GetNode<StaticBody2D>("Door")) ==
                            new Rect2(5805, 880, 50, 180),
            "old BellChain packed shape and configured live height remain unchanged");
        // Off-tree packed shape is 180 high; existing _Ready applies DoorHeight=240.
        CanyonSealGate seal = sector.GetNode<CanyonSealGate>("Gameplay/Mechanisms/CourtyardSeal");
        TestAssert.True(WorldShape(sector, seal) == new Rect2(5500, 720, 36, 500) &&
                        seal.RequiredMechanismId == "canyon.v2.courtyard-crank" &&
                        seal.RequiredEncounterId == "canyon.v2.encounter.split-level" &&
                        seal.ActivationId == "canyon.v2.courtyard-seal-open" && !seal.Collision.Disabled,
            "old seal geometry and actual progression prerequisites remain unchanged");
        CanyonEncounterGroup group = sector.GetNode<CanyonEncounterGroup>(
            "Gameplay/Encounters/SealedCourtyardEncounter");
        CanyonEncounterDefinition encounter = CanyonEncounterCatalog.Normal.Single(e =>
            e.StableId == group.EncounterId.ToString());
        TestAssert.True(encounter.StableId == "canyon.v2.encounter.sealed-courtyard" &&
                        encounter.RouteNode == CanyonRouteNodeId.SealedCourtyard && !encounter.Optional &&
                        !encounter.GatesProgression &&
                        encounter.ActivationBounds == new Rect2(5550, 1030, 210, 190) &&
                        encounter.ArenaBounds == new Rect2(5540, 920, 340, 330) &&
                        encounter.Enemies.SequenceEqual(new[]
                        {
                            new CanyonEnemyPlacement("res://scenes/enemies/RustShellSquire.tscn", new Vector2(5630, 1160), 0),
                            new CanyonEnemyPlacement("res://scenes/enemies/BellBackLaborer.tscn", new Vector2(5790, 1074), 1)
                        }), "old courtyard actor tuples activation and retirement arena remain unchanged");
        CheckpointTrigger checkpoint = sector.GetNode<CheckpointTrigger>("Gameplay/TempleCheckpoint");
        TestAssert.True(checkpoint.EntranceId.ToString() == "canyon.v2.temple-forecourt" &&
                        WorldShape(sector, checkpoint) == new Rect2(5894, 1028, 52, 104) &&
                        CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"] == new Vector2(5920, 1080),
            "old checkpoint position identity and trigger remain unchanged");
        TestAssert.True(WorldShape(sector, sector.GetNode<StaticBody2D>("Gameplay/Doors/Entry")) ==
                            new Rect2(6142, 936, 36, 180) &&
                        WorldShape(sector, sector.GetNode<StaticBody2D>("Gameplay/Doors/Exit")) ==
                            new Rect2(7282, 936, 36, 180) &&
                        WorldShape(sector, sector.GetNode<Area2D>("Gameplay/ActivationTrigger")) ==
                            new Rect2(6215, 936, 90, 180),
            "old Boss doors and real activation trigger remain unchanged");
        BossArenaController boss = sector.GetNode<BossArenaController>("Gameplay/BossArenaController");
        TestAssert.True(boss.BossSpawnPosition == new Vector2(1600, 360) &&
                        boss.BossScene.ResourcePath == "res://scenes/boss/FallenBellkeeper.tscn" &&
                        boss.BossRootPath.ToString() == "../BossRoot" &&
                        boss.EntryDoorCollisionPath.ToString() == "../Doors/Entry/Collision" &&
                        boss.ExitDoorCollisionPath.ToString() == "../Doors/Exit/Collision" &&
                        boss.ActivationTriggerPath.ToString() == "../ActivationTrigger" &&
                        sector.GetNode<Marker2D>("Gameplay/BossApproachStart").Position == new Vector2(940, 360),
            "old Boss binding spawn and approach remain unchanged");
        AssertRoute(sector, "courtyard-to-boss-hall", new Rect2(5880, 1044, 180, 72),
            new Rect2(6320, 1044, 180, 72));
        AssertRoute(sector, "boss-hall-to-safe-exit", new Rect2(7140, 1044, 120, 72),
            new Rect2(7360, 1044, 240, 72));
        TestAssert.True(CanyonWorldLayout.Edges.Contains(new CanyonRouteEdge(
                            CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall, false,
                            CanyonRouteGate.MechanismActivated, TraversalSegmentId: "canyon.v2.route.courtyard-to-boss-hall",
                            RequiredMechanismId: "canyon.v2.courtyard-seal-open")) &&
                        CanyonWorldLayout.Edges.Contains(new CanyonRouteEdge(
                            CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit, false,
                            CanyonRouteGate.BossDefeated, TraversalSegmentId: "canyon.v2.route.boss-hall-to-safe-exit")),
            "old courtyard and Boss-to-SafeExit graph edges retain genuine progression gates");
    }

    private static void AssertParentMetadata(bool installed)
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            PrologueSectorDefinition temple = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors")
                .Definitions.Single(d => d.Id == PrologueSectorId.LostTemple);
            Rect2[] old = { new(532, 222, 56, 56), new(916, 216, 48, 48) };
            TestAssert.True(temple.WorldOffset == TempleOrigin && temple.Bounds == new Rect2(0, 0, 2560, 720) &&
                            old.All(r => temple.KeyInteractionBounds.Count(x => x == r) == 1),
                "parent Temple origin bounds and all old interaction bounds remain unchanged");
            if (!installed) return;
            Rect2[] added = { new(748, 180, 60, 60), new(810, 40, 30, 200), new(862, 176, 56, 56) };
            TestAssert.True(temple.KeyInteractionBounds.Count == old.Length + added.Length &&
                            added.All(r => temple.KeyInteractionBounds.Count(x => x == r) == 1),
                "parent Temple metadata includes exact local plate door and finite supply bounds");
        }
        finally { level.Free(); }
    }

    private static void AssertRoute(PrologueSector sector, string suffix, Rect2 launch, Rect2 landing)
    {
        TraversalSegment2D route = sector.GetNode<TraversalSegment2D>(
            "Gameplay/TraversalContracts/canyonv2route" + suffix);
        TestAssert.True(route.SegmentId.ToString() == "canyon.v2.route." + suffix &&
                        route.Kind == TraversalSegmentKind.SingleJump && route.RequiredAbilities == TraversalAbility.Jump &&
                        new Rect2(route.LaunchZone.Position + TempleOrigin, route.LaunchZone.Size) == launch &&
                        new Rect2(route.LandingZone.Position + TempleOrigin, route.LandingZone.Size) == landing,
            "old " + suffix + " identity ability and launch/landing contract remain unchanged");
    }

    private static void AssertSurface(PrologueSector sector, string name, Rect2 world, GrappleSurfaceMaterial material)
    {
        StaticBody2D body = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
        CanyonPlatformVisual skin = sector.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/" + name);
        TestAssert.True(WorldShape(sector, body) == world && !body.GetNode<CollisionShape2D>("Collision").Disabled &&
                        LocalPosition(sector, skin) + TempleOrigin == world.GetCenter() &&
                        skin.Visible && skin.SkinSize == world.Size && skin.SurfaceMaterial == material &&
                        body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material == material,
            name + " retains matched visible skin and collision with explicit grapple material");
    }

    private static Rect2 WorldShape(PrologueSector sector, Node2D body)
    {
        CollisionShape2D collision = body.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(collision.Shape is RectangleShape2D && collision.Rotation == 0 &&
                        collision.Scale == Vector2.One && body.Rotation == 0 && body.Scale == Vector2.One,
            "fixture rectangle uses real unscaled axis-aligned shape");
        Vector2 size = ((RectangleShape2D)collision.Shape).Size;
        return new Rect2(LocalPosition(sector, collision) + TempleOrigin - size / 2f, size);
    }

    private static Vector2 LocalPosition(PrologueSector sector, Node2D node)
    {
        Vector2 result = Vector2.Zero;
        for (Node current = node; !ReferenceEquals(current, sector); current = current.GetParent())
        {
            TestAssert.True(current != null, "fixture node belongs to off-tree Temple");
            if (current is Node2D transformed)
            {
                TestAssert.True(transformed.Rotation == 0 && transformed.Scale == Vector2.One,
                    "off-tree candidate ancestors do not distort authored rectangles");
                result += transformed.Position;
            }
        }
        return result;
    }
}
