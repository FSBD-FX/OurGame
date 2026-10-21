using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Mechanisms;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Unit;

public static class PrologueCanyonStructureTests
{
    private static readonly IReadOnlyDictionary<CanyonRouteNodeId, string[]> Task6WitnessPaths =
        new Dictionary<CanyonRouteNodeId, string[]>
        {
            [CanyonRouteNodeId.LowCeilingBridge] = ["Gameplay/Collision/LowBridgeCeiling"],
            [CanyonRouteNodeId.StaggeredRift] = ["Gameplay/Collision/RiftUpperFloor"],
            [CanyonRouteNodeId.ControlledAbyss] = ["Gameplay/Collision/AbyssSafeBottom"],
            [CanyonRouteNodeId.ArchiveSideCave] = ["Gameplay/Collision/ArchiveFloor"],
            [CanyonRouteNodeId.FrozenDetour] = ["Gameplay/Collision/FrozenLeftCatch", "Gameplay/Collision/FrozenExit"],
            [CanyonRouteNodeId.GearPistons] = ["Gameplay/Mechanisms/GearPiston"],
            [CanyonRouteNodeId.SteamValves] = ["Gameplay/Mechanisms/SteamValve", "Gameplay/Mechanisms/SteamJet"],
            [CanyonRouteNodeId.CounterweightLift] = ["Gameplay/Mechanisms/CounterweightLift"],
            [CanyonRouteNodeId.PendulumGallery] = ["Gameplay/Mechanisms/PendulumHazard"],
            [CanyonRouteNodeId.SplitLevelEncounter] = ["Gameplay/Collision/SplitUpperGallery"],
            [CanyonRouteNodeId.SealedCourtyard] = ["Gameplay/Mechanisms/CourtyardSeal"],
            [CanyonRouteNodeId.BossHall] = ["Gameplay/BossArenaController"]
        };

    public static void RunTask6()
    {
        int encounterCount = 0;
        foreach (PrologueSectorId sectorId in Enum.GetValues<PrologueSectorId>())
        {
            PrologueSector sector = GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(sectorId))
                .Instantiate<PrologueSector>();
            try
            {
                CanyonEncounterGroup[] groups = Descendants(sector).OfType<CanyonEncounterGroup>().ToArray();
                CanyonEncounterDefinition[] expected = CanyonContentCatalog.EncountersForSector(sectorId).ToArray();
                encounterCount += groups.Length;
                TestAssert.Equal(expected.Length, groups.Length,
                    $"{sectorId} generated scene owns its exact Task6 encounter-group count");
                TestAssert.Equal(0, Descendants(sector.GetNode("Gameplay/Actors"))
                        .OfType<IEncounterActor>().Count(),
                    $"{sectorId} replaces uncatalogued scene-local enemies with dormant authored groups");
                foreach (CanyonEncounterDefinition definition in expected)
                {
                    CanyonEncounterGroup group = groups.Single(candidate =>
                        candidate.EncounterId.ToString() == definition.StableId);
                    TestAssert.Equal(new NodePath("../../Actors"), group.ActorRootPath,
                        $"{definition.StableId} owns an explicit actor-root lifecycle seam");
                }
                foreach (CanyonRouteNodeDefinition route in CanyonWorldLayout.Nodes.Where(node =>
                             node.SectorId == sectorId && node.ChallengeKind.HasValue))
                foreach (string path in Task6WitnessPaths[route.Id])
                    TestAssert.True(Task6WitnessIsGameplay(sector, route.Id, path),
                        $"{route.Id} contains a typed gameplay witness at {path}, not only a marker/backdrop rename");
            }
            finally { sector.Free(); }
        }
        TestAssert.Equal(12, encounterCount,
            "generated leaf scenes mount exactly eight main plus four optional normal groups");
        RunTask6InertWitnessNegative();
        GD.Print("TASK6_GENERATED_STRUCTURE_OK groups=12 challenge_witnesses=12 legacy_spawns=0");
    }

    public static void RunTask6InertWitnessNegative()
    {
        PrologueSector sector = GD.Load<PackedScene>(
                PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonEntrance))
            .Instantiate<PrologueSector>();
        try
        {
            Node parent = sector.GetNode("Gameplay/Collision");
            Node real = parent.GetNode("LowBridgeCeiling");
            parent.RemoveChild(real);
            real.Free();
            parent.AddChild(new Marker2D { Name = "LowBridgeCeiling" });
            TestAssert.True(!Task6WitnessIsGameplay(sector, CanyonRouteNodeId.LowCeilingBridge,
                    "Gameplay/Collision/LowBridgeCeiling"),
                "an inert same-name marker must fail the Task6 challenge witness contract");
        }
        finally { sector.Free(); }
        GD.Print("TASK6_INERT_WITNESS_NEGATIVE_OK");
    }

    private static bool Task6WitnessIsGameplay(PrologueSector sector, CanyonRouteNodeId route,
        string path)
    {
        Node witness = sector.GetNodeOrNull(path);
        return route switch
        {
            CanyonRouteNodeId.LowCeilingBridge or CanyonRouteNodeId.StaggeredRift or
                CanyonRouteNodeId.ControlledAbyss or CanyonRouteNodeId.ArchiveSideCave or
                CanyonRouteNodeId.FrozenDetour or CanyonRouteNodeId.SplitLevelEncounter =>
                witness is StaticBody2D && HasEnabledCollision(witness),
            CanyonRouteNodeId.GearPistons =>
                witness is CanyonGearPiston && HasEnabledCollision(witness),
            CanyonRouteNodeId.SteamValves when path.EndsWith("SteamValve", StringComparison.Ordinal) =>
                witness is CanyonSteamValve valve && valve is IThreadInteractionReceiver &&
                valve.GetNodeOrNull(valve.PairedJetPath) is CanyonSteamJet,
            CanyonRouteNodeId.SteamValves =>
                witness is CanyonSteamJet && HasEnabledCollision(witness),
            CanyonRouteNodeId.CounterweightLift =>
                witness is CanyonCounterweightLift and IThreadInteractionReceiver &&
                HasEnabledCollision(witness),
            CanyonRouteNodeId.PendulumGallery =>
                witness is CanyonPendulumHazard && HasEnabledCollision(witness),
            CanyonRouteNodeId.SealedCourtyard =>
                witness is CanyonSealGate && HasEnabledCollision(witness),
            CanyonRouteNodeId.BossHall => witness is BossArenaController boss &&
                boss.BossScene != null && boss.GetNodeOrNull(boss.BossRootPath) != null &&
                boss.GetNodeOrNull<CollisionShape2D>(boss.EntryDoorCollisionPath)?.Shape != null &&
                boss.GetNodeOrNull<CollisionShape2D>(boss.ExitDoorCollisionPath)?.Shape != null &&
                boss.GetNodeOrNull<Area2D>(boss.ActivationTriggerPath) is { } trigger &&
                HasEnabledCollision(trigger),
            _ => false
        };
    }

    private static bool HasEnabledCollision(Node witness) =>
        Descendants(witness).OfType<CollisionShape2D>()
            .Any(shape => !shape.Disabled && shape.Shape != null);

    public static void RunTask4()
    {
        var failures = new List<Exception>();
        void Check(Action assertion) { try { assertion(); } catch (Exception e) { failures.Add(e); GD.Print("TASK4_GEOMETRY_RED " + e.Message); } }
        Check(() =>
        {
            TestAssert.Equal(14, CanyonContentCatalog.All.Count, "layout v2 catalog has fourteen route/support nodes, not backdrop names");
            TestAssert.Equal(12, CanyonContentCatalog.All.Count(d => d.ChallengeKind.HasValue), "twelve actual challenge contracts");
            TestAssert.Equal(12, CanyonContentCatalog.All.Where(d => d.ChallengeKind.HasValue).Select(d => d.ChallengeKind).Distinct().Count(), "each challenge kind is distinct and explicit");
            foreach (CanyonRouteNodeDefinition node in CanyonWorldLayout.Nodes)
            {
                var entry = CanyonContentCatalog.All.Single(d => d.RouteNodeId == node.Id);
                TestAssert.Equal(node.ChallengeKind, entry.ChallengeKind, "catalog uses authoritative challenge kind, not name inference");
                var offset = CanyonWorldLayout.SectorBounds[node.SectorId].Position;
                TestAssert.Equal(node.WorldBounds, new Rect2(entry.LocalBounds.Position + offset, entry.LocalBounds.Size), "catalog covers exact accepted world-space node");
            }
        });
        foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>()) Check(() =>
        {
            var sector = GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(id)).Instantiate<PrologueSector>();
            try
            {
                Rect2 world = CanyonWorldLayout.SectorBounds[id];
                TestAssert.Equal(new Rect2(Vector2.Zero, world.Size), sector.Bounds, id + " generated collision sector uses accepted v2 bounds");
                foreach (GrappleSurface2D surface in Descendants(sector).OfType<GrappleSurface2D>())
                {
                    if (surface is ThreadTarget2D) continue; // interactive receivers are not locomotion supports
                    TestAssert.True(surface.UseCollisionHitPoint && surface.GetParent() is StaticBody2D,
                        id + " main route replaces floating anchor chains with real terrain profiles");
                }
            }
            finally { sector.Free(); }
        });
        foreach (CanyonRouteEdge edge in CanyonWorldLayout.Edges)
        {
            Check(() => AssertGeneratedEdge(edge.From, edge.To, edge.TraversalSegmentId));
            if (edge.Bidirectional) Check(() => AssertGeneratedEdge(edge.To, edge.From, edge.ReverseTraversalSegmentId));
        }
        Check(ThickTerrainAssistedSegmentValidates);
        Check(SegmentIdentitySurvivesCanonicalNodeNames);
        Check(ExactPhysicalAbyssLandmarks);
        if (failures.Count > 0) throw new AggregateException("Task4 geometry contracts failed", failures);
        int directedSegments = CanyonWorldLayout.Edges.Sum(edge => edge.Bidirectional ? 2 : 1);
        TestAssert.Equal(18, directedSegments, "includes the explicit real-terrain abyss collection return");
        GD.Print($"TASK4_CANYON_STRUCTURE_OK sectors=5 route_nodes=14 challenges=12 directed_segments={directedSegments} thick_profiles=True");
    }

    private static void ExactPhysicalAbyssLandmarks()
    {
        var sector=GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonDescent)).Instantiate<PrologueSector>();
        try
        {
            Rect2 Physical(string name)
            {
                var body=sector.GetNode<StaticBody2D>("Gameplay/Collision/"+name);
                var collider=body.GetNode<CollisionShape2D>("Collision");
                TestAssert.True(!collider.Disabled && collider.Shape is RectangleShape2D,"abyss landmarks are actual enabled thick collision bodies");
                var size=((RectangleShape2D)collider.Shape).Size;
                return new Rect2(PositionInSector(sector,body)+new Vector2(1280,0)-size/2,size);
            }
            TestAssert.Equal(new Rect2(2080,400,120,40),Physical("AbyssCommitment"),"real commitment ledge is exactly at Y400");
            for(int i=0;i<4;i++)
            {
                TestAssert.Equal(CanyonWorldLayout.AbyssBrakeWallSegments[i],Physical($"AbyssBrakeRight{i+1}"),"physical right walls match accepted camera landmarks verbatim");
                TestAssert.Equal(new Rect2(1920,400+i*800,36,i==3?640:800),Physical($"AbyssBrakeLeft{i+1}"),"continuous left brake faces end at the capsule-clear safe-bottom egress");
            }
            for(int i=0;i<3;i++) TestAssert.Equal(CanyonWorldLayout.AbyssSideExits[i],Physical($"AbyssExit{i+1}"),"physical exit matches accepted camera landmark verbatim");
            TestAssert.Equal(CanyonWorldLayout.AbyssSafeBottom,Physical("AbyssSafeBottom"),"bottom is a real thick collision at Y3560");
            TestAssert.Equal(3160f,Physical("AbyssSafeBottom").Position.Y-Physical("AbyssCommitment").Position.Y,"uninterrupted capsule-center vertical travel is 3160px");
            var centerCorridor=new Rect2(2160,440,160,3120);
            foreach(var body in sector.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>())
                TestAssert.True(!Physical(body.Name).Intersects(centerCorridor),"no collider pads or interrupts the passive center corridor: "+body.Name);
        }
        finally { sector.Free(); }
    }

    private static void AssertGeneratedEdge(CanyonRouteNodeId from, CanyonRouteNodeId to, string segmentId)
    {
        var fromNode = CanyonWorldLayout.Nodes.Single(n => n.Id == from);
        var toNode = CanyonWorldLayout.Nodes.Single(n => n.Id == to);
        var world = new Node2D();
        var sector = GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(fromNode.SectorId)).Instantiate<PrologueSector>();
        try
        {
            sector.Position = CanyonWorldLayout.SectorBounds[fromNode.SectorId].Position;
            world.AddChild(sector);
            var matches = sector.GetNode("Gameplay/TraversalContracts").GetChildren()
                .OfType<TraversalSegment2D>().Where(candidate => candidate.SegmentId == segmentId).ToArray();
            TestAssert.Equal(1, matches.Length, segmentId + " physically exists exactly once in its generated source sector");
            var segment = matches[0];
            if (segment.Kind == TraversalSegmentKind.ClawDrop && toNode.SectorId != fromNode.SectorId)
            {
                var destination = GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(toNode.SectorId))
                    .Instantiate<PrologueSector>();
                destination.Position = CanyonWorldLayout.SectorBounds[toNode.SectorId].Position;
                world.AddChild(destination);
            }
            Vector2 offset = CanyonWorldLayout.SectorBounds[fromNode.SectorId].Position + PositionInSector(sector, segment);
            TestAssert.True(new Rect2(segment.LaunchZone.Position + offset, segment.LaunchZone.Size).Intersects(fromNode.WorldBounds), segmentId + " transformed launch intersects From node");
            TestAssert.True(new Rect2(segment.LandingZone.Position + offset, segment.LandingZone.Size).Intersects(toNode.WorldBounds), segmentId + " transformed landing intersects To node");
            TestAssert.Equal(0, segment.Validate(sector).Count, segmentId + " validates the actual generated profile and traversal contract: " + string.Join("; ",segment.Validate(sector)));
            TestAssert.True((segment.RequiredAbilities & TraversalAbility.Dash) == 0, "no route edge requires unearned dash");
        }
        finally { world.Free(); }
    }

    private static void SegmentIdentitySurvivesCanonicalNodeNames()
    {
        var root = new Node2D();
        try
        {
            var segment = new TraversalSegment2D { Name = "canyonv2routetest", SegmentId = "canyon.v2.route.test",
                Kind = TraversalSegmentKind.SingleJump, RequiredAbilities = TraversalAbility.Jump,
                LaunchZone = new Rect2(0,0,100,72), LandingZone = new Rect2(160,0,100,72) };
            root.AddChild(segment);
            TestAssert.Equal(0, segment.Validate(root).Count, "exact dotted identity accepts its canonical Godot node name");
            segment.Name = "arbitrary_wrong_name";
            TestAssert.True(segment.Validate(root).Any(error => error.Contains("node name")), "arbitrary wrong node names remain invalid");
            segment.Name = "legacy_safe-name"; segment.SegmentId = "legacy_safe-name";
            TestAssert.Equal(0, segment.Validate(root).Count, "legacy legal names remain unchanged and valid");
            var duplicate = new TraversalSegment2D { Name = "OtherNode", SegmentId = segment.SegmentId };
            root.AddChild(duplicate);
            TestAssert.True(segment.Validate(root).Any(error => error.Contains("duplicate")), "exact duplicate identities fail even under different node names");
            root.RemoveChild(duplicate); duplicate.Free();
            segment.SegmentId = "";
            TestAssert.True(segment.Validate(root).Any(error => error.Contains("required")), "empty identity remains invalid");
        }
        finally { root.Free(); }
    }

    private static void ThickTerrainAssistedSegmentValidates()
    {
        var root = new Node2D();
        try
        {
            var terrain = new StaticBody2D { Name = "Terrain", Position = new Vector2(380, -100), CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget };
            terrain.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(120, 40) } });
            terrain.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.GripStone, UseCollisionHitPoint = true });
            root.AddChild(terrain);
            var segment = new TraversalSegment2D { Name = "terrain-test", SegmentId = "terrain-test", Kind = TraversalSegmentKind.Assisted,
                RequiredAbilities = TraversalAbility.Jump | TraversalAbility.Thread, LaunchZone = new Rect2(0,0,100,72), LandingZone = new Rect2(360,-60,160,72), AssistTargetPath = new NodePath("Terrain/GrappleSurfaceProfile") };
            root.AddChild(segment);
            TestAssert.Equal(0, segment.Validate(root).Count,
                "ThickTerrainAssistedSegmentValidates: supported collision-hit-point terrain cannot be rejected as non-DedicatedAnchor");
            var profile = terrain.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            foreach (var material in new[] { GrappleSurfaceMaterial.Ice, GrappleSurfaceMaterial.Forbidden, GrappleSurfaceMaterial.DedicatedAnchor })
            {
                profile.Material = material;
                TestAssert.True(segment.Validate(root).Count > 0, "invalid terrain material cannot satisfy Assisted: " + material);
            }
            profile.Material = GrappleSurfaceMaterial.TempleMetal;
            TestAssert.Equal(0, segment.Validate(root).Count, "TempleMetal enabled terrain is supported");
            var shape = terrain.GetNode<CollisionShape2D>("Collision");
            shape.Disabled = true;
            TestAssert.True(segment.Validate(root).Count > 0, "disabled terrain shape fails closed");
            shape.Disabled = false;
            terrain.CollisionLayer = CombatCollisionLayers.ThreadTarget;
            TestAssert.True(segment.Validate(root).Count > 0, "target-only body cannot counterfeit physical support");
            terrain.CollisionLayer = CombatCollisionLayers.World;
            TestAssert.True(segment.Validate(root).Count > 0, "world-only body cannot serve as a claw target");
            terrain.CollisionLayer |= CombatCollisionLayers.ThreadTarget;
            terrain.RemoveChild(shape);
            TestAssert.True(segment.Validate(root).Count > 0, "missing terrain collision fails closed");
            terrain.AddChild(shape);
            terrain.Position = new Vector2(800, -100);
            TestAssert.True(segment.Validate(root).Count > 0, "terrain retains launch range guard");
            terrain.Position = new Vector2(-100, -100);
            TestAssert.True(segment.Validate(root).Count > 0, "terrain retains landing-facing direction guard");
        }
        finally { root.Free(); }
    }

    private const float SeamMargin = PrologueSectorStreamModel.UnloadSeamMargin;
    private static readonly string[] VisualLayers =
    [
        "Background", "Midground", "GameplayReadable", "Foreground", "Effects"
    ];

    private static readonly SectorCase[] Sectors =
    [
        new(PrologueSectorId.CanyonEntrance, "res://scenes/prologue/sectors/CanyonEntrance.tscn",
            new Rect2(0, 0, 1280, 720), new Vector2(0, 0),
            ["entrance_move_attack"]),
        new(PrologueSectorId.CanyonDescent, "res://scenes/prologue/sectors/CanyonDescent.tscn",
            new Rect2(0, 0, 1280, 1440), new Vector2(1280, 0),
            ["descent_grip_lesson", "descent_ice_rejection"]),
        new(PrologueSectorId.CanyonBasin, "res://scenes/prologue/sectors/CanyonBasin.tscn",
            new Rect2(0, 0, 1600, 720), new Vector2(1280, 1440),
            ["basin_shield_lane", "basin_checkpoint"]),
        new(PrologueSectorId.CanyonAscent, "res://scenes/prologue/sectors/CanyonAscent.tscn",
            new Rect2(0, 0, 1280, 1440), new Vector2(2880, 720),
            ["ascent_grapple_one", "ascent_grapple_two", "ascent_grapple_three"]),
        new(PrologueSectorId.LostTemple, "res://scenes/prologue/sectors/LostTemple.tscn",
            new Rect2(0, 0, 1920, 720), new Vector2(4160, 720),
            ["temple_mechanism", "temple_checkpoint", "temple_boss_entry"])
    ];

    public static void Run(PackedScene mainScene)
    {
        WithV2Sectors(AssertV2Sectors);
        AssertV2GraphAndCatalog();
        WithV2Main(mainScene, AssertV2Main);
        GD.Print("CANYON_V2_STRUCTURE_OK sectors=5 nodes=14 challenges=12 directed_edges=18 " +
            "logical_edges=16 encounter_groups=12 visual_layers=5 structural_only=true");
    }

    // Independent accepted literals: never build expected data from the layout/catalog/builder.
    private const PrologueSectorId E = PrologueSectorId.CanyonEntrance;
    private const PrologueSectorId D = PrologueSectorId.CanyonDescent;
    private const PrologueSectorId B = PrologueSectorId.CanyonBasin;
    private const PrologueSectorId A = PrologueSectorId.CanyonAscent;
    private const PrologueSectorId T = PrologueSectorId.LostTemple;
    private const string RoutePrefix = "canyon.v2.route.";

    private static readonly V2Sector[] V2Sectors =
    [
        new(E, "res://scenes/prologue/sectors/CanyonEntrance.tscn", new(0,0,1280,720), new(0,0), 1, null, new(1278,600)),
        new(D, "res://scenes/prologue/sectors/CanyonDescent.tscn", new(0,0,1280,3600), new(1280,0), 5, new(2,600), new(620,3598)),
        new(B, "res://scenes/prologue/sectors/CanyonBasin.tscn", new(0,0,3840,720), new(1280,3600), 2, new(620,2), new(2460,2)),
        new(A, "res://scenes/prologue/sectors/CanyonAscent.tscn", new(0,0,1280,2880), new(3840,720), 3, new(70,2878), new(1278,484)),
        new(T, "res://scenes/prologue/sectors/LostTemple.tscn", new(0,0,2560,720), new(5120,720), 1, new(2,484), null)
    ];

    private static readonly V2Node[] V2Nodes =
    [
        new(CanyonRouteNodeId.LowCeilingBridge,E,new(0,0,1280,720),false,0),
        new(CanyonRouteNodeId.StaggeredRift,D,new(0,0,1280,1176),false,1),
        new(CanyonRouteNodeId.ControlledAbyss,D,new(640,1176,640,2424),false,2),
        new(CanyonRouteNodeId.ArchiveSideCave,D,new(0,1176,640,624),true,3,Discovery:"canyon.v2.discovery.archive-side-cave"),
        new(CanyonRouteNodeId.FrozenDetour,D,new(0,1800,640,1560),true,4,Discovery:"canyon.v2.discovery.frozen-detour"),
        new(CanyonRouteNodeId.BasinCamp,B,new(0,0,800,720),false,-1,Rest:true),
        new(CanyonRouteNodeId.GearPistons,B,new(800,0,960,720),false,5),
        new(CanyonRouteNodeId.SteamValves,B,new(1760,360,1160,360),true,6),
        new(CanyonRouteNodeId.CounterweightLift,A,new(0,2080,1280,800),false,7),
        new(CanyonRouteNodeId.PendulumGallery,A,new(0,1280,1280,800),false,8),
        new(CanyonRouteNodeId.SplitLevelEncounter,A,new(0,480,1280,800),false,9),
        new(CanyonRouteNodeId.SealedCourtyard,T,new(0,0,960,720),false,10),
        new(CanyonRouteNodeId.BossHall,T,new(960,0,1200,720),false,11),
        new(CanyonRouteNodeId.SafeExit,T,new(2160,0,400,720),false,-1,Rest:true)
    ];

    private static readonly V2Segment[] V2Segments =
    [
        new(E,"bridge-to-rift",0,1,TraversalSegmentKind.GroundTransfer,TraversalAbility.None,new(1190,564,70,72),new(1300,564,120,72)),
        new(D,"rift-to-abyss",1,2,TraversalSegmentKind.GroundTransfer,TraversalAbility.None,new(140,1004,60,72),new(830,1224,70,72)),
        new(D,"abyss-to-basin-camp",2,5,TraversalSegmentKind.GroundTransfer,TraversalAbility.None,new(680,3488,120,72),new(540,3924,180,72),Reverse:"basin-camp-to-abyss"),
        new(D,"rift-to-archive",1,3,TraversalSegmentKind.GroundTransfer,TraversalAbility.None,new(140,1004,60,72),new(68,1188,120,72)),
        new(D,"archive-to-frozen-detour",3,4,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(160,1568,120,72),new(40,2108,140,72)),
        new(D,"frozen-detour-to-basin-camp",4,5,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(360,3208,160,72),new(280,3924,240,72)),
        new(B,"basin-camp-to-gears",5,6,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(640,324,120,72),new(840,324,180,72)),
        new(B,"gears-to-lift-high",6,8,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(1560,164,160,72),new(2600,-88,220,72)),
        new(B,"gears-to-steam-valves",6,7,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(1560,164,160,72),new(1820,564,240,72)),
        new(B,"steam-valves-to-lift",7,8,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(2280,564,160,72),new(3660,-72,140,72)),
        new(B,"basin-to-upper-shortcut",5,10,TraversalSegmentKind.Assisted,TraversalAbility.Jump|TraversalAbility.Thread,new(640,324,120,72),new(2820,-2192,120,72),"Gameplay/Collision/GearEntry/GrappleSurfaceProfile",IsReverse:true),
        new(B,"basin-camp-to-abyss",5,2,TraversalSegmentKind.ClawDrop,TraversalAbility.Thread,new(380,324,120,72),new(880,-112,160,72),"../CanyonDescent/Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile","../CanyonDescent/Gameplay/Collision/AbyssSafeBottom",new(-18,290.7278f),new(-1,0),IsReverse:true),
        new(A,"lift-to-pendulums",8,9,TraversalSegmentKind.Assisted,TraversalAbility.Jump|TraversalAbility.Thread,new(820,2288,160,72),new(560,1568,80,72),"Gameplay/Collision/AscentGrip3/GrappleSurfaceProfile"),
        new(A,"pendulums-to-split-level",9,10,TraversalSegmentKind.Assisted,TraversalAbility.Jump|TraversalAbility.Thread,new(820,1328,160,72),new(560,608,80,72),"Gameplay/Collision/AscentGrip7/GrappleSurfaceProfile"),
        new(A,"split-level-to-courtyard",10,11,TraversalSegmentKind.Assisted,TraversalAbility.Jump|TraversalAbility.Thread,new(560,608,80,72),new(1340,448,220,72),"Gameplay/Collision/AscentFinalCatchGrip/GrappleSurfaceProfile"),
        new(A,"upper-to-basin-shortcut",10,5,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(260,688,120,72),new(-2000,3204,200,72),Gate:CanyonRouteGate.ShortcutOpen,GateId:"canyon.v2.upper-basin-shortcut",Reverse:"basin-to-upper-shortcut"),
        new(T,"courtyard-to-boss-hall",11,12,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(760,324,180,72),new(1200,324,180,72),Gate:CanyonRouteGate.MechanismActivated,GateId:"canyon.v2.courtyard-seal-open"),
        new(T,"boss-hall-to-safe-exit",12,13,TraversalSegmentKind.SingleJump,TraversalAbility.Jump,new(2020,324,120,72),new(2240,324,240,72),Gate:CanyonRouteGate.BossDefeated)
    ];

    private static readonly (PrologueSectorId Sector, string Path)[] V2WorldOnly =
    [
        (D,"Gameplay/Mechanisms/ArchivePressureGate/Door"),
        (B,"Gameplay/Mechanisms/GearPiston"),(B,"Gameplay/Mechanisms/BasinCounterweightLift"),
        (A,"Gameplay/Mechanisms/CounterweightLift"),(A,"Gameplay/Mechanisms/PendulumHazard"),
        (A,"Gameplay/Mechanisms/UpperBasinShortcutWinch/Barrier"),
        (T,"Gameplay/Mechanisms/CourtyardSeal"),(T,"Gameplay/Mechanisms/TemplePressureGate/Door")
    ];

    private static readonly V2Point[] V2Points =
    [
        new(D,"Gameplay/GrappleSurfaces/SteamDiverterFrozenControl",new(460,2500),"mechanism-control","../../Mechanisms/SteamDiverterFrozen"),
        new(D,"Gameplay/Mechanisms/HiddenPanel/ThreadTarget",new(160,1376),"mechanism",".."),
        new(B,"Gameplay/GrappleSurfaces/SteamValveControl",new(1880,440),"mechanism-control","../../Mechanisms/SteamValve"),
        new(B,"Gameplay/GrappleSurfaces/SteamDiverterTrenchControl",new(2710,470),"mechanism-control","../../Mechanisms/SteamDiverterTrench"),
        new(B,"Gameplay/GrappleSurfaces/BasinCounterweightControl",new(1212,216),"mechanism-control","../../Mechanisms/BasinCounterweightLift"),
        new(B,"Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor",new(1832,24),"basin-gallery-transfer",""),
        new(A,"Gameplay/GrappleSurfaces/CounterweightLiftControl",new(1260,2700),"mechanism-control","../../Mechanisms/CounterweightLift"),
        new(A,"Gameplay/GrappleSurfaces/UpperBasinShortcutControl",new(500,700),"mechanism-control","../../Mechanisms/UpperBasinShortcutWinch"),
        new(A,"Gameplay/GrappleSurfaces/CourtyardSealLinkageControl",new(1080,540),"mechanism-control","../../Mechanisms/CourtyardSealLinkage"),
        new(T,"Gameplay/Mechanisms/BellBody/ThreadTarget",new(940,176),"mechanism","..")
    ];

    private static readonly (PrologueSectorId Sector,string Name,string Id)[] V2Encounters =
    [
        (E,"LowCeilingBridgeEncounter","canyon.v2.encounter.low-ceiling-bridge"),
        (D,"StaggeredRiftEncounter","canyon.v2.encounter.staggered-rift"),
        (D,"ControlledAbyssEncounter","canyon.v2.encounter.controlled-abyss"),
        (D,"ArchiveSideCaveEncounter","canyon.v2.encounter.archive-side-cave"),
        (D,"FrozenUpperEncounter","canyon.v2.encounter.frozen-upper"),
        (D,"FrozenLowerEncounter","canyon.v2.encounter.frozen-lower"),
        (B,"GearPistonsEncounter","canyon.v2.encounter.gear-pistons"),
        (B,"SteamValvesEncounter","canyon.v2.encounter.steam-valves"),
        (A,"CounterweightLiftEncounter","canyon.v2.encounter.counterweight-lift"),
        (A,"PendulumGalleryEncounter","canyon.v2.encounter.pendulum-gallery"),
        (A,"SplitLevelEncounter","canyon.v2.encounter.split-level"),
        (T,"SealedCourtyardEncounter","canyon.v2.encounter.sealed-courtyard")
    ];

    private static void WithV2Sectors(Action<IReadOnlyDictionary<PrologueSectorId, PrologueSector>> inspect,
        Action<PrologueSector> beforeAssembly = null)
    {
        var root = new Node2D { Name = "DetachedV2Structure" };
        var sectors = new Dictionary<PrologueSectorId, PrologueSector>();
        try
        {
            foreach (V2Sector expected in V2Sectors)
            {
                TestAssert.True(ResourceLoader.Exists(expected.Path), $"V2 sector resource exists {expected.Id}");
                PackedScene packed = GD.Load<PackedScene>(expected.Path);
                TestAssert.True(packed != null, $"V2 sector resource loads {expected.Id}");
                PrologueSector sector = packed.Instantiate<PrologueSector>();
                root.AddChild(sector);
                beforeAssembly?.Invoke(sector);
                AssertV2SavedRoot(sector);
                sector.Position = expected.Offset;
                sectors.Add(expected.Id, sector);
            }
            inspect(sectors);
        }
        finally { root.Free(); }
    }

    private static void AssertV2SavedRoot(PrologueSector sector)
    {
        TestAssert.Equal(Vector2.Zero,sector.Position,$"V2 {sector.SectorId} saved root local origin");
        TestAssert.True(sector.Transform.X.IsEqualApprox(Vector2.Right) && sector.Transform.Y.IsEqualApprox(Vector2.Down),
            $"V2 {sector.SectorId} saved root identity axes");
    }

    private static void WithV2Main(PackedScene packed, Action<PrologueLevelController> inspect)
    {
        TestAssert.True(packed != null, "V2 main resource loads");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try { inspect(level); }
        finally { level.Free(); }
    }

    private static void AssertV2Sectors(IReadOnlyDictionary<PrologueSectorId, PrologueSector> sectors)
    {
        var encounters = new HashSet<string>(StringComparer.Ordinal);
        int markerCount = 0, segmentCount = 0;
        foreach (V2Sector expected in V2Sectors)
        {
            PrologueSector sector = sectors[expected.Id];
            string label = $"V2 {expected.Id}";
            TestAssert.Equal(expected.Id.ToString(), sector.Name.ToString(), label + " root name");
            TestAssert.Equal(expected.Id, sector.SectorId, label + " typed root identity");
            TestAssert.Equal(expected.Bounds, sector.Bounds, label + " exact local bounds");
            TestAssert.Equal(expected.Offset, sector.Position, label + " exact assembled offset");
            TestAssert.True(sector.Transform.X.IsEqualApprox(Vector2.Right) && sector.Transform.Y.IsEqualApprox(Vector2.Down), label + " translation-only axes");
            Node2D visual = sector.GetNodeOrNull<Node2D>("VisualRoot");
            Node2D gameplay = sector.GetNodeOrNull<Node2D>("Gameplay");
            TestAssert.True(visual != null && gameplay != null && sector.GetNodeOrNull<Node2D>("Gameplay/Collision") != null, label + " visual/gameplay roots");
            foreach(string path in new[]{"Gameplay","Gameplay/Collision","Gameplay/TraversalContracts","Gameplay/MicroScenes","Gameplay/RouteMarkers"})
            {
                var container=sector.GetNodeOrNull<Node2D>(path);
                TestAssert.True(container!=null && container.Transform.IsEqualApprox(Transform2D.Identity),label+" fixed container identity "+path);
            }
            TestAssert.Equal(string.Join("|",VisualLayers),string.Join("|",visual.GetChildren().Select(node=>node.Name.ToString())),label + " exact five visual layers");
            TestAssert.True(!Descendants(visual).Any(node=>node is GrappleSurface2D or CollisionObject2D),label + " art owns no terrain or claw authority");
            var recovery = sector.GetNodeOrNull<Area2D>("Gameplay/FallRecovery/RecoveryVolume");
            TestAssert.True(recovery?.GetNodeOrNull<CollisionShape2D>("Collision") is { Disabled:false, Shape:RectangleShape2D } &&
                sector.GetNodeOrNull<Marker2D>("Gameplay/FallRecovery/RecoveryLanding") != null,label + " recovery has real volume and landing");
            AssertV2Seam(sector,"PreviousSeam",expected.Previous);
            AssertV2Seam(sector,"NextSeam",expected.Next);
            AssertV2Markers(expected,sector);
            markerCount += V2Nodes.Count(node=>node.Sector==expected.Id);
            AssertV2Terrain(expected,sector,gameplay);
            AssertV2Points(expected,sector,gameplay);
            foreach (V2Node node in V2Nodes.Where(node=>node.Sector==expected.Id && node.Challenge>=0))
            foreach (string path in Task6WitnessPaths[node.Id])
                TestAssert.True(Task6WitnessIsGameplay(sector,node.Id,path),$"V2 witness {node.Id}/{path} enabled typed gameplay");
            Node actorRoot = sector.GetNodeOrNull("Gameplay/Actors");
            TestAssert.True(actorRoot != null,label + " actor lifecycle root");
            TestAssert.Equal(0,Descendants(actorRoot).OfType<IEncounterActor>().Count(),label + " actors are dormant, not fixed legacy spawns");
            CanyonEncounterGroup[] groups = Descendants(gameplay).OfType<CanyonEncounterGroup>().ToArray();
            TestAssert.Equal(expected.Encounters,groups.Length,label + " exact encounter group count");
            foreach(var literal in V2Encounters.Where(encounter=>encounter.Sector==expected.Id))
            {
                var group=sector.GetNodeOrNull<CanyonEncounterGroup>("Gameplay/Encounters/"+literal.Name);
                TestAssert.True(group!=null && groups.Contains(group) && group.EncounterId.ToString()==literal.Id,
                    label+" exact encounter path/ID "+literal.Name);
            }
            foreach (CanyonEncounterGroup group in groups)
            {
                TestAssert.True(!group.EncounterId.IsEmpty && encounters.Add(group.EncounterId.ToString()),label + " unique encounter ID");
                TestAssert.True(!group.IsActive && group.CurrentBatch==-1 && group.LiveActorCount==0,label + " dormant encounter lifecycle");
                TestAssert.Equal(new NodePath("../../Actors"),group.ActorRootPath,label + " encounter actor-root path");
                TestAssert.True(group.GetNodeOrNull(group.ActorRootPath)==actorRoot,label + " exact encounter actor-root identity");
            }
            AssertV2Segments(expected,sector);
            segmentCount += V2Segments.Count(segment=>segment.Owner==expected.Id);
        }
        TestAssert.Equal(14,markerCount,"V2 fourteen exact route markers");
        TestAssert.Equal(18,segmentCount,"V2 eighteen exact directed saved segments");
        TestAssert.Equal(12,encounters.Count,"V2 twelve dormant encounter groups");
        AssertV2AuthoredGates(sectors);
    }

    private static void AssertV2Seam(PrologueSector sector,string name,Vector2? expected)
    {
        var marker=sector.GetNodeOrNull<Marker2D>("Gameplay/RouteMarkers/"+name);
        TestAssert.True(expected.HasValue ? marker!=null : marker==null,$"V2 {sector.SectorId} exact seam role {name}");
        if(expected.HasValue) TestAssert.Equal(expected.Value,marker.Position,$"V2 {sector.SectorId} exact seam position {name}");
    }

    private static void AssertV2Markers(V2Sector expected,PrologueSector sector)
    {
        Node markers=sector.GetNodeOrNull("Gameplay/MicroScenes");
        V2Node[] nodes=V2Nodes.Where(node=>node.Sector==expected.Id).ToArray();
        TestAssert.True(markers!=null,$"V2 {expected.Id} marker root");
        TestAssert.Equal(string.Join("|",nodes.Select(node=>node.Id)),string.Join("|",markers.GetChildren().Select(node=>node.Name.ToString())), $"V2 {expected.Id} marker IDs/order");
        foreach(V2Node node in nodes)
        {
            var marker=markers.GetNodeOrNull<Marker2D>(node.Id.ToString());
            TestAssert.True(marker!=null,$"V2 {node.Id} marker identity");
            foreach(string key in new[]{"local_bounds","optional_subarea","route_node_id","challenge_kind"})
                TestAssert.True(marker.HasMeta(key),$"V2 {node.Id} metadata required {key}");
            TestAssert.Equal(Variant.Type.Rect2,marker.GetMeta("local_bounds").VariantType,$"V2 {node.Id} bounds metadata type");
            TestAssert.Equal(node.Bounds,marker.GetMeta("local_bounds").AsRect2(),$"V2 {node.Id} exact marker bounds");
            TestAssert.True(expected.Bounds.Encloses(node.Bounds),$"V2 {node.Id} marker sector enclosure");
            TestAssert.Equal(node.Bounds.GetCenter(),marker.Position,$"V2 {node.Id} exact marker center");
            TestAssert.Equal(Variant.Type.Bool,marker.GetMeta("optional_subarea").VariantType,$"V2 {node.Id} optional metadata type");
            TestAssert.Equal(node.Optional,marker.GetMeta("optional_subarea").AsBool(),$"V2 {node.Id} exact optional flag");
            TestAssert.Equal(Variant.Type.Int,marker.GetMeta("route_node_id").VariantType,$"V2 {node.Id} route metadata type");
            TestAssert.Equal((long)node.Id,marker.GetMeta("route_node_id").AsInt64(),$"V2 {node.Id} exact route_node_id");
            TestAssert.Equal(Variant.Type.Int,marker.GetMeta("challenge_kind").VariantType,$"V2 {node.Id} challenge metadata type");
            TestAssert.Equal((long)node.Challenge,marker.GetMeta("challenge_kind").AsInt64(),$"V2 {node.Id} exact challenge kind");
        }
    }

    private static void AssertV2Terrain(V2Sector expected,PrologueSector sector,Node gameplay)
    {
        StaticBody2D[] bodies=Descendants(gameplay).OfType<StaticBody2D>().Where(body=>!BelongsToInstancedSubscene(body,gameplay)).ToArray();
        TestAssert.True(bodies.Length>0,$"V2 {expected.Id} actual terrain bodies");
        foreach(StaticBody2D body in bodies)
        {
            string path=sector.GetPathTo(body).ToString();
            var profile=body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
            TestAssert.True(profile!=null && profile.UseCollisionHitPoint,$"V2 {expected.Id}/{path} typed terrain profile");
            TestAssert.True(profile.Material is GrappleSurfaceMaterial.GripStone or GrappleSurfaceMaterial.TempleMetal or GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden,$"V2 {expected.Id}/{path} explicit terrain material");
            bool worldOnly=V2WorldOnly.Contains((expected.Id,path));
            TestAssert.Equal(worldOnly ? CombatCollisionLayers.World : CombatCollisionLayers.World|CombatCollisionLayers.ThreadTarget,body.CollisionLayer,$"V2 {expected.Id}/{path} role-specific terrain layers");
            if(worldOnly) TestAssert.Equal(GrappleSurfaceMaterial.Forbidden,profile.Material,$"V2 {expected.Id}/{path} non-claw mechanism material");
            TestAssert.True(body.GetChildren().OfType<CollisionShape2D>().Any(shape=>shape.Shape!=null),$"V2 {expected.Id}/{path} actual terrain shape");
        }
        foreach(var item in V2WorldOnly.Where(item=>item.Sector==expected.Id))
            TestAssert.True(sector.GetNodeOrNull<StaticBody2D>(item.Path)!=null,$"V2 {expected.Id} exact World-only role {item.Path}");
        foreach(GrappleSurface2D surface in Descendants(gameplay).OfType<GrappleSurface2D>())
        {
            TestAssert.Equal(0,surface.Validate().Count,$"V2 {expected.Id}/{surface.Name} valid surface contract");
            if(surface.UseCollisionHitPoint) TestAssert.True(surface.GetParent() is StaticBody2D,$"V2 {expected.Id}/{surface.Name} terrain support identity");
        }
    }

    private static void AssertV2Points(V2Sector expected,PrologueSector sector,Node gameplay)
    {
        GrappleSurface2D[] points=Descendants(gameplay).OfType<GrappleSurface2D>().Where(surface=>!surface.UseCollisionHitPoint && !BelongsToInstancedSubscene(surface,gameplay)).ToArray();
        V2Point[] literals=V2Points.Where(point=>point.Sector==expected.Id).ToArray();
        TestAssert.Equal(literals.Length,points.Length,$"V2 {expected.Id} exact own point-target roles, not a seam waiver");
        foreach(V2Point literal in literals)
        {
            var point=sector.GetNodeOrNull<GrappleSurface2D>(literal.Path);
            TestAssert.True(point!=null && !point.UseCollisionHitPoint,$"V2 point {literal.Path} identity");
            TestAssert.Equal(literal.Position,PositionInSector(sector,point),$"V2 point {literal.Path} exact seam-relative position");
            TestAssert.True(expected.Bounds.HasPoint(literal.Position),$"V2 point {literal.Path} sector enclosure");
            TestAssert.True(point.HasMeta("target_role") && point.GetMeta("target_role").AsString()==literal.Role,$"V2 point {literal.Path} exact target role");
            var area=point.GetNodeOrNull<Area2D>("TargetCollision");
            TestAssert.True(area!=null && area.CollisionLayer==CombatCollisionLayers.ThreadTarget && HasEnabledCollision(area),$"V2 point {literal.Path} real enabled target collision");
            if(literal.Receiver.Length==0)
                TestAssert.True(point is not ThreadTarget2D && point.Material==GrappleSurfaceMaterial.DedicatedAnchor,$"V2 point {literal.Path} dedicated gallery-transfer anchor");
            else
            {
                TestAssert.Equal(GrappleSurfaceMaterial.DedicatedAnchor,point.Material,$"V2 point {literal.Path} eligible mechanism material");
                TestAssert.True(point is ThreadTarget2D target && target.Kind==ThreadTargetKind.Mechanism && target.PullReceiverPath.ToString()==literal.Receiver &&
                    target.GetNodeOrNull(target.PullReceiverPath) is IThreadInteractionReceiver,$"V2 point {literal.Path} exact typed mechanism receiver");
                var adapter=point.GetNodeOrNull<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
                TestAssert.True(adapter!=null && adapter.GetParent()==point && adapter.TargetPath.ToString()==".." &&
                    adapter.GetNodeOrNull<ThreadTarget2D>(adapter.TargetPath)==point,$"V2 point {literal.Path} exact legacy adapter target identity");
            }
        }
    }

    private static void AssertV2Segments(V2Sector expected,PrologueSector sector)
    {
        Node root=sector.GetNodeOrNull("Gameplay/TraversalContracts");
        TestAssert.True(root!=null,$"V2 {expected.Id} traversal root");
        TraversalSegment2D[] segments=root.GetChildren().OfType<TraversalSegment2D>().ToArray();
        V2Segment[] literals=V2Segments.Where(segment=>segment.Owner==expected.Id).ToArray();
        TestAssert.Equal(string.Join("|",literals.Select(segment=>RoutePrefix+segment.Id)),string.Join("|",segments.Select(segment=>segment.SegmentId.ToString())),$"V2 {expected.Id} exact segment IDs/order");
        for(int index=0;index<literals.Length;index++)
        {
            V2Segment literal=literals[index];
            TraversalSegment2D segment=segments[index];
            string label=$"V2 segment {literal.Id}";
            TestAssert.Equal((RoutePrefix+literal.Id).ValidateNodeName(),segment.Name.ToString(),label+" canonical name");
            TestAssert.Equal(Vector2.Zero,segment.Position,label+" local origin");
            TestAssert.True(segment.Transform.X.IsEqualApprox(Vector2.Right) && segment.Transform.Y.IsEqualApprox(Vector2.Down),label+" identity axes");
            TestAssert.Equal(literal.Kind,segment.Kind,label+" exact kind");
            TestAssert.Equal(literal.Abilities,segment.RequiredAbilities,label+" exact abilities");
            TestAssert.Equal(literal.Launch,segment.LaunchZone,label+" exact launch zone");
            TestAssert.Equal(literal.Landing,segment.LandingZone,label+" exact landing zone");
            TestAssert.Equal(literal.Target,segment.AssistTargetPath.ToString(),label+" exact target path");
            TestAssert.Equal(literal.Support,segment.LandingSupportPath.ToString(),label+" exact support path");
            TestAssert.Equal(literal.Hit,segment.AssistHitPoint,label+" exact collision-local hit");
            TestAssert.Equal(literal.Normal,segment.AssistHitNormal,label+" exact collision-local normal");
            Rect2 from=V2NodeWorld((CanyonRouteNodeId)literal.From),to=V2NodeWorld((CanyonRouteNodeId)literal.To);
            TestAssert.True(from.Intersects(V2WorldZone(segment,segment.LaunchZone)),label+" exact From-node overlap");
            TestAssert.True(to.Intersects(V2WorldZone(segment,segment.LandingZone)),label+" exact To-node overlap");
            foreach(string child in new[]{"LaunchZoneVisual","LandingZoneVisual"})
                TestAssert.True(segment.GetNodeOrNull<CanvasItem>(child) is { Visible:false },label+" inspectable hidden "+child);
            if(literal.Target.Length>0)
            {
                var target=sector.GetNodeOrNull<GrappleSurface2D>(literal.Target);
                TestAssert.True(target!=null && target.UseCollisionHitPoint && target.GetParent() is StaticBody2D,label+" exact real terrain target identity");
            }
            if(literal.Support.Length>0)
                TestAssert.True(sector.GetNodeOrNull<StaticBody2D>(literal.Support) is { } support && HasEnabledCollision(support),label+" exact enabled landing support identity");
            IReadOnlyList<string> errors=segment.Validate(sector);
            TestAssert.Equal(0,errors.Count,label+" validates actual assembled saved support: "+string.Join("; ",errors));
        }
    }

    private static Rect2 V2WorldZone(Node2D segment,Rect2 zone)
    {
        // Detached saved-scene graph: include every actual ancestor, without GlobalTransform/Ready.
        Transform2D pose=Transform2D.Identity;
        for(Node current=segment;current!=null;current=current.GetParent())
            if(current is Node2D canvas) pose=canvas.Transform*pose;
        Vector2 first=pose*zone.Position;
        Vector2 second=pose*new Vector2(zone.End.X,zone.Position.Y);
        Vector2 third=pose*zone.End;
        Vector2 fourth=pose*new Vector2(zone.Position.X,zone.End.Y);
        Vector2 minimum=new(Mathf.Min(Mathf.Min(first.X,second.X),Mathf.Min(third.X,fourth.X)),
            Mathf.Min(Mathf.Min(first.Y,second.Y),Mathf.Min(third.Y,fourth.Y)));
        Vector2 maximum=new(Mathf.Max(Mathf.Max(first.X,second.X),Mathf.Max(third.X,fourth.X)),
            Mathf.Max(Mathf.Max(first.Y,second.Y),Mathf.Max(third.Y,fourth.Y)));
        return new Rect2(minimum,maximum-minimum);
    }

    private static Rect2 V2NodeWorld(CanyonRouteNodeId id)
    {
        V2Node literal=V2Nodes.Single(node=>node.Id==id);
        Vector2 offset=V2Sectors.Single(sector=>sector.Id==literal.Sector).Offset;
        return new Rect2(literal.Bounds.Position+offset,literal.Bounds.Size);
    }

    private static void AssertV2GraphAndCatalog()
    {
        TestAssert.Equal(new Rect2(0,0,7680,4320),CanyonWorldLayout.DesignBounds,"V2 literal design bounds");
        TestAssert.Equal(14,CanyonWorldLayout.Nodes.Count,"V2 layout exact node count");
        TestAssert.Equal(14,CanyonContentCatalog.All.Count,"V2 catalog exact node count");
        TestAssert.Equal(12,CanyonContentCatalog.All.Count(node=>node.ChallengeKind.HasValue),"V2 twelve catalog challenges");
        TestAssert.Equal(12,CanyonContentCatalog.All.Where(node=>node.ChallengeKind.HasValue).Select(node=>node.ChallengeKind).Distinct().Count(),"V2 twelve distinct challenge kinds");
        foreach(V2Sector sector in V2Sectors)
            TestAssert.Equal(new Rect2(sector.Offset,sector.Bounds.Size),CanyonWorldLayout.SectorBounds[sector.Id],$"V2 {sector.Id} literal layout sector bounds");
        for(int index=0;index<V2Nodes.Length;index++)
        {
            V2Node node=V2Nodes[index];
            CanyonChallengeKind? challenge=node.Challenge<0 ? null : (CanyonChallengeKind)node.Challenge;
            var expected=new CanyonRouteNodeDefinition(node.Id,node.Sector,V2NodeWorld(node.Id),challenge,node.Optional,node.Rest,node.Discovery);
            TestAssert.Equal(expected,CanyonWorldLayout.Nodes[index],$"V2 layout exact node {node.Id}");
            var entry=CanyonContentCatalog.All[index];
            TestAssert.Equal(new CanyonMicroSceneDefinition(node.Id.ToString(),node.Sector,node.Bounds,node.Optional,challenge,node.Id),entry,$"V2 catalog exact node {node.Id}");
        }
        AssertV2Edges(CanyonWorldLayout.Edges);
        TestAssert.Equal(0,CanyonWorldLayout.Validate().Count,"V2 graph gates validate current literal world");
        var none=new HashSet<string>(StringComparer.Ordinal);
        var open=new HashSet<string>(StringComparer.Ordinal){"canyon.v2.upper-basin-shortcut"};
        var seal=new HashSet<string>(StringComparer.Ordinal){"canyon.v2.courtyard-seal-open"};
        TestAssert.True(!CanyonRouteGraph.Reachable(CanyonRouteNodeId.LowCeilingBridge,open,none,true).Contains(CanyonRouteNodeId.BossHall),"V2 graph seal gate cannot be bypassed");
        TestAssert.True(!CanyonRouteGraph.Reachable(CanyonRouteNodeId.LowCeilingBridge,open,seal,false).Contains(CanyonRouteNodeId.SafeExit),"V2 graph boss gate cannot be bypassed");
        TestAssert.True(CanyonRouteGraph.Reachable(CanyonRouteNodeId.LowCeilingBridge,open,seal,true).Contains(CanyonRouteNodeId.SafeExit),"V2 graph legitimate gates reach safe exit, not a physical route claim");
    }

    private static void AssertV2Edges(IReadOnlyList<CanyonRouteEdge> actual)
    {
        TestAssert.Equal(16,actual.Count,"V2 sixteen logical edges");
        foreach(V2Segment segment in V2Segments.Where(segment=>!segment.IsReverse))
        {
            var matches=actual.Where(edge=>edge.TraversalSegmentId==RoutePrefix+segment.Id).ToArray();
            TestAssert.Equal(1,matches.Length,$"V2 graph exact edge ID {segment.Id}");
            var expected=new CanyonRouteEdge((CanyonRouteNodeId)segment.From,(CanyonRouteNodeId)segment.To,segment.Reverse.Length>0,
                segment.Gate,segment.Gate==CanyonRouteGate.ShortcutOpen ? segment.GateId : "",RoutePrefix+segment.Id,
                segment.Gate==CanyonRouteGate.MechanismActivated ? segment.GateId : "",segment.Reverse.Length==0 ? "" : RoutePrefix+segment.Reverse);
            TestAssert.Equal(expected,matches[0],$"V2 graph exact From/To/gate/reverse {segment.Id}");
        }
        TestAssert.Equal(2,actual.Count(edge=>edge.Bidirectional),"V2 only two declared reverse edges");
    }

    private static void AssertV2AuthoredGates(IReadOnlyDictionary<PrologueSectorId,PrologueSector> sectors)
    {
        var mechanisms=sectors.Values.SelectMany(Descendants).OfType<CanyonClawMechanism2D>().ToArray();
        TestAssert.Equal(2,mechanisms.Length,"V2 exactly two retained one-shot claw mechanisms");
        foreach(var literal in new[]{(D,"Gameplay/Mechanisms/HiddenPanel",CanyonClawMechanismKind.HiddenPanel),(T,"Gameplay/Mechanisms/BellBody",CanyonClawMechanismKind.BellBody)})
        {
            var mechanism=sectors[literal.Item1].GetNodeOrNull<CanyonClawMechanism2D>(literal.Item2);
            TestAssert.True(mechanism!=null && mechanism.Kind==literal.Item3 && mechanism.GetNodeOrNull(mechanism.LinkedNodePath)!=null &&
                mechanism.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")!=null,"V2 exact one-shot mechanism "+literal.Item2);
        }
        foreach(var checkpoint in new[]{(B,"Gameplay/BasinCheckpoint",new Vector2(400,360),"canyon.v2.basin-camp"),(T,"Gameplay/TempleCheckpoint",new Vector2(800,360),"canyon.v2.temple-forecourt")})
        {
            var trigger=sectors[checkpoint.Item1].GetNodeOrNull<CheckpointTrigger>(checkpoint.Item2);
            TestAssert.True(trigger!=null && HasEnabledCollision(trigger),"V2 exact real checkpoint "+checkpoint.Item4);
            TestAssert.Equal(checkpoint.Item3,trigger.Position,"V2 checkpoint exact local position "+checkpoint.Item4);
            TestAssert.Equal(checkpoint.Item4,trigger.EntranceId.ToString(),"V2 checkpoint exact stable ID "+checkpoint.Item4);
        }
        var seal=sectors[T].GetNodeOrNull<CanyonSealGate>("Gameplay/Mechanisms/CourtyardSeal");
        TestAssert.True(seal!=null && seal.ActivationId=="canyon.v2.courtyard-seal-open" && seal.RequiredMechanismId=="canyon.v2.courtyard-crank" && seal.RequiredEncounterId=="canyon.v2.encounter.split-level","V2 exact courtyard seal prerequisites");
        var winch=sectors[A].GetNodeOrNull<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
        TestAssert.True(winch!=null && winch.ShortcutId=="canyon.v2.upper-basin-shortcut" && winch.GetNodeOrNull<CollisionShape2D>(winch.BarrierCollisionPath)?.Shape!=null,"V2 exact shortcut gate/barrier identity");
        var arena=sectors[T].GetNodeOrNull<BossArenaController>("Gameplay/BossArenaController");
        TestAssert.True(arena!=null && arena.RequiresFragmentAdmission && arena.BossScene?.ResourcePath=="res://scenes/boss/FallenBellkeeper.tscn" && arena.BossSpawnPosition==new Vector2(1600,360),"V2 exact boss gate scene/admission/spawn");
        TestAssert.True(arena.GetNodeOrNull<Node2D>(arena.BossRootPath)==sectors[T].GetNodeOrNull<Node2D>("Gameplay/BossRoot"),"V2 boss root exact identity");
        foreach(string path in new[]{"Gameplay/Doors/Entry/Collision","Gameplay/Doors/Exit/Collision","Gameplay/Doors/AdmissionBarrier/Collision"})
            TestAssert.True(sectors[T].GetNodeOrNull<CollisionShape2D>(path) is { Disabled:false, Shape:RectangleShape2D },"V2 boss enabled physical door "+path);
        TestAssert.True(arena.GetNodeOrNull<CollisionShape2D>(arena.EntryDoorCollisionPath)==sectors[T].GetNodeOrNull<CollisionShape2D>("Gameplay/Doors/Entry/Collision") &&
            arena.GetNodeOrNull<CollisionShape2D>(arena.ExitDoorCollisionPath)==sectors[T].GetNodeOrNull<CollisionShape2D>("Gameplay/Doors/Exit/Collision") &&
            arena.GetNodeOrNull<Area2D>(arena.ActivationTriggerPath)==sectors[T].GetNodeOrNull<Area2D>("Gameplay/ActivationTrigger"),"V2 boss exact door/trigger path identities");
    }

    private static void AssertV2Main(PrologueLevelController level)
    {
        TestAssert.Equal(new NodePath("WorldRoot/Sectors"),level.SeamlessWorldPath,"V2 main seamless path identity");
        TestAssert.Equal("p0_complete",level.MilestoneCompletionExitId.ToString(),"V2 main completion exit identity");
        var world=level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TestAssert.True(world!=null,"V2 main seamless world exists");
        TestAssert.Equal(2,world.LayoutVersion,"V2 main exact LayoutVersion");
        TestAssert.Equal(5,world.Definitions.Count,"V2 main five definitions");
        TestAssert.Equal(new Vector2(160,600),level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position,"V2 main exact start checkpoint position");
        for(int index=0;index<V2Sectors.Length;index++)
        {
            V2Sector expected=V2Sectors[index];
            PrologueSectorDefinition definition=world.Definitions[index];
            TestAssert.Equal(expected.Id,definition.Id,$"V2 main definition {index} identity");
            TestAssert.Equal(expected.Path,definition.Scene?.ResourcePath,$"V2 main definition {expected.Id} scene");
            TestAssert.Equal(expected.Bounds,definition.Bounds,$"V2 main definition {expected.Id} bounds");
            TestAssert.Equal(expected.Offset,definition.WorldOffset,$"V2 main definition {expected.Id} offset");
            TestAssert.Equal(index==0 ? -1 : index-1,definition.PreviousValue,$"V2 main definition {expected.Id} previous");
            TestAssert.Equal(index==4 ? -1 : index+1,definition.NextValue,$"V2 main definition {expected.Id} next");
            int[] neighbors=index switch{0=>[1],1=>[0,2],2=>[1,3],3=>[2,4],_=>[3]};
            TestAssert.Equal(string.Join("|",neighbors),string.Join("|",definition.NeighborValues),$"V2 main definition {expected.Id} neighbors");
            Rect2[] interactions=index switch
            {
                0=>[new(416,636,48,40)],
                1=>[new(136,1416,48,48),new(340,1580,80,60),new(440,1424,40,216),new(512,1576,56,56),new(1210,1400,36,160),new(438,2478,44,44),new(352,2465,56,150),new(512,2465,56,150)],
                2=>[new(1296,256,48,48),new(2688,448,44,44),new(2578,435,64,150),new(2798,435,64,150)],
                3=>[new(1112,2472,56,56)],
                _=>[new(532,222,56,56),new(916,216,48,48),new(748,180,60,60),new(810,40,30,200),new(862,176,56,56)]
            };
            TestAssert.True(interactions.SequenceEqual(definition.KeyInteractionBounds) && definition.KeyInteractionBounds.All(bounds=>expected.Bounds.Encloses(bounds)),$"V2 main definition {expected.Id} exact enclosed interactions");
        }
        _=new PrologueSectorStreamModel(world.Definitions.ToArray());
    }

    public static void RunV2NegativeContracts(PackedScene mainScene)
    {
        int cases=0;
        void Reject(string reason,Action assertion)
        {
            AssertV2Rejects(reason,assertion);
            cases++;
        }
        Reject("saved root local origin",()=>WithV2Sectors(AssertV2Sectors,sector=>
        {
            if(sector.SectorId==E) sector.Position+=Vector2.Right;
        }));
        Reject("saved root identity axes",()=>WithV2Sectors(AssertV2Sectors,sector=>
        {
            if(sector.SectorId==E) sector.Scale=new Vector2(2,1);
        }));
        void RejectWorld(string reason,Action<IReadOnlyDictionary<PrologueSectorId,PrologueSector>> mutate) => WithV2Sectors(sectors=>
        {
            mutate(sectors);
            Reject(reason,()=>AssertV2Sectors(sectors));
        });
        RejectWorld("fixed container identity Gameplay",sectors=>sectors[E].GetNode<Node2D>("Gameplay").Position=new Vector2(10000,0));
        RejectWorld("fixed container identity Gameplay/TraversalContracts",sectors=>sectors[E].GetNode<Node2D>("Gameplay/TraversalContracts").Position=Vector2.Right);
        RejectWorld("segment bridge-to-rift identity axes",sectors=>sectors[E].GetNode("Gameplay/TraversalContracts").GetChild<TraversalSegment2D>(0).Scale=new Vector2(2,1));
        RejectWorld("exact encounter path/ID",sectors=>sectors[E].GetNode<CanyonEncounterGroup>("Gameplay/Encounters/LowCeilingBridgeEncounter").EncounterId="unknown.encounter");
        RejectWorld("exact encounter path/ID",sectors=>sectors[E].GetNode<CanyonEncounterGroup>("Gameplay/Encounters/LowCeilingBridgeEncounter").EncounterId="canyon.v2.encounter.staggered-rift");
        RejectWorld("eligible mechanism material",sectors=>sectors[B].GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/SteamValveControl").Material=GrappleSurfaceMaterial.Forbidden);
        RejectWorld("eligible mechanism material",sectors=>sectors[B].GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/SteamValveControl").Material=GrappleSurfaceMaterial.Ice);
        RejectWorld("exact legacy adapter target identity",sectors=>sectors[B].GetNode("Gameplay/GrappleSurfaces/SteamValveControl/LegacyThreadInteractionAdapter").Free());
        RejectWorld("exact legacy adapter target identity",sectors=>sectors[B].GetNode<LegacyThreadInteractionAdapter>("Gameplay/GrappleSurfaces/SteamValveControl/LegacyThreadInteractionAdapter").TargetPath=new NodePath("../TargetCollision"));
        RejectWorld("marker IDs/order",sectors=>sectors[E].GetNode("Gameplay/MicroScenes/LowCeilingBridge").Free());
        RejectWorld("exact challenge kind",sectors=>sectors[E].GetNode("Gameplay/MicroScenes/LowCeilingBridge").SetMeta("challenge_kind",1));
        RejectWorld("metadata required route_node_id",sectors=>sectors[E].GetNode("Gameplay/MicroScenes/LowCeilingBridge").RemoveMeta("route_node_id"));
        RejectWorld("exact route_node_id",sectors=>sectors[E].GetNode("Gameplay/MicroScenes/LowCeilingBridge").SetMeta("route_node_id",13));
        RejectWorld("exact optional flag",sectors=>sectors[D].GetNode("Gameplay/MicroScenes/ArchiveSideCave").SetMeta("optional_subarea",false));
        RejectWorld("exact marker bounds",sectors=>sectors[E].GetNode("Gameplay/MicroScenes/LowCeilingBridge").SetMeta("local_bounds",new Rect2(1,1,1000,600)));
        RejectWorld("exact marker center",sectors=>sectors[E].GetNode<Marker2D>("Gameplay/MicroScenes/LowCeilingBridge").Position+=Vector2.Right);
        RejectWorld("witness LowCeilingBridge",sectors=>sectors[E].GetNode<CollisionShape2D>("Gameplay/Collision/LowBridgeCeiling/Collision").Disabled=true);
        RejectWorld("witness LowCeilingBridge",sectors=>ReplaceV2Witness(sectors[E],false));
        RejectWorld("witness LowCeilingBridge",sectors=>ReplaceV2Witness(sectors[E],true));
        RejectWorld("exact segment IDs/order",sectors=>sectors[E].GetNode("Gameplay/TraversalContracts").GetChild<TraversalSegment2D>(0).SegmentId="corrupt.segment");
        RejectWorld("exact kind",sectors=>sectors[E].GetNode("Gameplay/TraversalContracts").GetChild<TraversalSegment2D>(0).Kind=TraversalSegmentKind.SingleJump);
        RejectWorld("exact target path",sectors=>sectors[A].GetNode("Gameplay/TraversalContracts").GetChild<TraversalSegment2D>(0).AssistTargetPath=new NodePath("Gameplay/Collision/AscentGrip7/GrappleSurfaceProfile"));
        RejectWorld("exact launch zone",sectors=>sectors[E].GetNode("Gameplay/TraversalContracts").GetChild<TraversalSegment2D>(0).LaunchZone=new Rect2(1191,564,70,72));
        WithV2Main(mainScene,level=>
        {
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            world.LayoutVersion=1;
            Reject("main exact LayoutVersion",()=>AssertV2Main(level));
        });
        WithV2Main(mainScene,level=>
        {
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            PrologueSectorDefinition definition=world.Definitions[1];
            Vector2 original=definition.WorldOffset;
            try
            {
                definition.WorldOffset=original+Vector2.Right;
                Reject("main definition CanyonDescent offset",()=>AssertV2Main(level));
            }
            finally { definition.WorldOffset=original; }
        });
        void RejectEdges(string reason,Action<List<CanyonRouteEdge>> mutate)
        {
            // Records are immutable. Replace entries only in this private list, never in the static layout.
            var actual=new List<CanyonRouteEdge>(CanyonWorldLayout.Edges);
            mutate(actual);
            Reject(reason,()=>AssertV2Edges(actual));
        }
        RejectEdges("graph exact From/To/gate/reverse bridge-to-rift",edges=>edges[0]=edges[0] with { From=CanyonRouteNodeId.SafeExit });
        RejectEdges("graph exact From/To/gate/reverse bridge-to-rift",edges=>edges[0]=edges[0] with { To=CanyonRouteNodeId.SafeExit });
        RejectEdges("graph exact From/To/gate/reverse courtyard-to-boss-hall",edges=>
        {
            int index=edges.FindIndex(edge=>edge.TraversalSegmentId==RoutePrefix+"courtyard-to-boss-hall");
            edges[index]=edges[index] with { Gate=CanyonRouteGate.None };
        });
        RejectEdges("graph exact From/To/gate/reverse abyss-to-basin-camp",edges=>
        {
            int index=edges.FindIndex(edge=>edge.TraversalSegmentId==RoutePrefix+"abyss-to-basin-camp");
            edges[index]=edges[index] with { ReverseTraversalSegmentId=RoutePrefix+"bridge-to-rift" };
        });
        RejectEdges("sixteen logical edges",edges=>edges.RemoveAt(0));
        RejectEdges("graph exact edge ID bridge-to-rift",edges=>edges[1]=edges[0]);
        GD.Print($"CANYON_V2_STRUCTURE_NEGATIVES_OK cases={cases} same_assertions=true copied_edge_records=true");
    }

    private static void ReplaceV2Witness(PrologueSector sector,bool inert)
    {
        Node real=sector.GetNode("Gameplay/Collision/LowBridgeCeiling");
        Node parent=real.GetParent();
        parent.RemoveChild(real);
        real.Free();
        if(inert) parent.AddChild(new Marker2D{Name="LowBridgeCeiling"});
    }

    private static void AssertV2Rejects(string reason,Action assertion)
    {
        bool rejected=false;
        try { assertion(); }
        catch(InvalidOperationException exception) when(exception.Message.StartsWith("Assertion failed: V2",StringComparison.Ordinal) && exception.Message.Contains(reason,StringComparison.Ordinal))
        { rejected=true; }
        TestAssert.True(rejected,"V2 deliberate mutation must reject for its exact assertion reason: "+reason);
    }

    private sealed record V2Sector(PrologueSectorId Id,string Path,Rect2 Bounds,Vector2 Offset,int Encounters,Vector2? Previous,Vector2? Next);
    private sealed record V2Node(CanyonRouteNodeId Id,PrologueSectorId Sector,Rect2 Bounds,bool Optional,int Challenge,bool Rest=false,string Discovery="");
    private sealed record V2Point(PrologueSectorId Sector,string Path,Vector2 Position,string Role,string Receiver);
    private sealed record V2Segment(PrologueSectorId Owner,string Id,int From,int To,TraversalSegmentKind Kind,TraversalAbility Abilities,Rect2 Launch,Rect2 Landing,
        string Target="",string Support="",Vector2 Hit=default,Vector2 Normal=default,CanyonRouteGate Gate=CanyonRouteGate.None,string GateId="",string Reverse="",bool IsReverse=false);

    private static void AssertSectorContract(SectorCase expected, PrologueSector sector,
        ICollection<string> observedSegments, ICollection<string> observedMicroScenes,
        ICollection<string> observedClawMechanisms)
    {
        TestAssert.Equal(expected.Id.ToString(), sector.Name.ToString(),
            $"{expected.Id} root name is stable");
        TestAssert.Equal(expected.Id, sector.SectorId, $"{expected.Id} root identity is typed");
        TestAssert.Equal(expected.Bounds, sector.Bounds, $"{expected.Id} local bounds are exact");
        TestAssert.Equal(Vector2.Zero, sector.Position, $"{expected.Id} root remains local at origin");

        Node2D visual = sector.GetNodeOrNull<Node2D>("VisualRoot");
        TestAssert.True(visual != null, $"{expected.Id} exposes stable VisualRoot");
        string[] actualLayers = visual.GetChildren().Select(child => child.Name.ToString()).ToArray();
        TestAssert.Equal(string.Join("|", VisualLayers), string.Join("|", actualLayers),
            $"{expected.Id} exposes the exact replaceable visual layers");

        Node2D gameplay = sector.GetNodeOrNull<Node2D>("Gameplay");
        Node2D collision = sector.GetNodeOrNull<Node2D>("Gameplay/Collision");
        Node2D traversal = sector.GetNodeOrNull<Node2D>("Gameplay/TraversalContracts");
        TestAssert.True(gameplay != null && collision != null && traversal != null,
            $"{expected.Id} separates gameplay collision and traversal from VisualRoot");
        TestAssert.True(collision.GetChildren().OfType<StaticBody2D>().Any(body =>
                body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D),
            $"{expected.Id} contains authoritative packed world collision");
        TestAssert.True(sector.GetNodeOrNull<Area2D>("Gameplay/FallRecovery/RecoveryVolume")
                ?.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D,
            $"{expected.Id} exposes an authoritative fall-recovery volume");
        TestAssert.True(sector.GetNodeOrNull<Marker2D>("Gameplay/FallRecovery/RecoveryLanding") != null,
            $"{expected.Id} exposes a visible recovery landing target");

        AssertMicroSceneMarkers(expected, sector, observedMicroScenes);
        AssertTypedGameplayBodies(expected, gameplay, visual);
        ObserveCanyonClawMechanisms(expected, sector, observedClawMechanisms);

        Marker2D previous = sector.GetNodeOrNull<Marker2D>("Gameplay/RouteMarkers/PreviousSeam");
        Marker2D next = sector.GetNodeOrNull<Marker2D>("Gameplay/RouteMarkers/NextSeam");
        if (expected.Id == PrologueSectorId.CanyonEntrance)
            TestAssert.True(previous == null && next != null,
                "entrance exposes only the forward continuous seam");
        else if (expected.Id == PrologueSectorId.LostTemple)
            TestAssert.True(previous != null && next == null,
                "temple exposes only the backward continuous seam");
        else
            TestAssert.True(previous != null && next != null,
                $"{expected.Id} exposes both continuous route seams");

        string[] actualSegments = traversal.GetChildren().OfType<TraversalSegment2D>()
            .Select(segment => segment.SegmentId.ToString()).ToArray();
        TestAssert.Equal(string.Join("|", expected.Segments), string.Join("|", actualSegments),
            $"{expected.Id} mandatory segments are exact and ordered");
        foreach (TraversalSegment2D segment in traversal.GetChildren().OfType<TraversalSegment2D>())
        {
            IReadOnlyList<string> errors = segment.Validate(sector);
            TestAssert.Equal(0, errors.Count,
                $"{expected.Id}/{segment.SegmentId} validates: {string.Join("; ", errors)}");
            TestAssert.True((segment.RequiredAbilities & TraversalAbility.Dash) == 0,
                $"{segment.SegmentId} mandatory ability mask excludes Dash");
            TestAssert.True(segment.LandingZone.Size.X >= 180f,
                $"{segment.SegmentId} required landing is at least 180 px wide");
            TestAssert.True(expected.Bounds.Encloses(segment.LaunchZone) &&
                expected.Bounds.Encloses(segment.LandingZone),
                $"{segment.SegmentId} launch and landing zones stay inside sector bounds");
            TestAssert.True(segment.GetNodeOrNull<CanvasItem>("LaunchZoneVisual") != null &&
                segment.GetNodeOrNull<CanvasItem>("LandingZoneVisual") != null,
                $"{segment.SegmentId} launch and landing bounds remain inspectable");
            TestAssert.True(!segment.GetNode<CanvasItem>("LaunchZoneVisual").Visible &&
                !segment.GetNode<CanvasItem>("LandingZoneVisual").Visible,
                $"{segment.SegmentId} debug volumes do not masquerade as floating platforms");
            if (segment.Kind == TraversalSegmentKind.Assisted)
            {
                TestAssert.True(!segment.AssistTargetPath.IsEmpty &&
                    sector.GetNodeOrNull<GrappleSurface2D>(segment.AssistTargetPath) != null,
                    $"{segment.SegmentId} exposes an explicit grapple assist surface");
                GrappleSurface2D assist = sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
                float distance = segment.LaunchZone.GetCenter().DistanceTo(
                    PositionInSector(sector, assist));
                TestAssert.True(distance >= 360f && distance <= 520f,
                    $"{segment.SegmentId} necessary grapple distance stays inside [360,520]; " +
                    $"actual={distance:0.00}");
                AssertCatchLayer(expected.Id, sector, segment);
            }
            observedSegments.Add(segment.SegmentId.ToString());
        }

        foreach (GrappleSurface2D surface in Descendants(sector).OfType<GrappleSurface2D>())
        {
            TestAssert.Equal(0, surface.Validate().Count,
                $"{expected.Id}/{surface.Name} surface contract validates");
            if (surface.UseCollisionHitPoint)
                TestAssert.True(surface.GetParent() is StaticBody2D,
                    $"{expected.Id}/{surface.Name} terrain profile belongs to authoritative collision");
            else
                TestAssert.True(surface.GetNodeOrNull<Area2D>("TargetCollision")
                        ?.GetNodeOrNull<CollisionShape2D>("Collision") != null,
                    $"{expected.Id}/{surface.Name} point target has real target collision");
            if (surface.UseCollisionHitPoint ||
                surface.Material is GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden)
                continue;
            AssertCriticalSurfaceClearOfSeams(expected, PositionInSector(sector, surface));
        }

        AssertAuthoredContent(expected.Id, sector);
    }

    private static void ObserveCanyonClawMechanisms(SectorCase expected, PrologueSector sector,
        ICollection<string> observed)
    {
        foreach (CanyonClawMechanism2D mechanism in Descendants(sector)
                     .OfType<CanyonClawMechanism2D>())
        {
            NodePath path = sector.GetPathTo(mechanism);
            observed.Add($"{expected.Id}/{path}:{mechanism.Kind}");
        }
    }

    private static readonly ExpectedMicroScene[] ExpectedMicroScenes =
    [
        new("BrokenBridgeLookout", PrologueSectorId.CanyonEntrance, new Rect2(0, 0, 360, 720), false),
        new("SuspendedYard", PrologueSectorId.CanyonEntrance, new Rect2(360, 0, 430, 720), false),
        new("CollapsedGate", PrologueSectorId.CanyonEntrance, new Rect2(790, 0, 490, 720), false),
        new("CraneShaft", PrologueSectorId.CanyonDescent, new Rect2(0, 0, 640, 500), false),
        new("FrostVent", PrologueSectorId.CanyonDescent, new Rect2(520, 360, 760, 440), false),
        new("BrokenLift", PrologueSectorId.CanyonDescent, new Rect2(300, 760, 980, 680), false),
        new("ArchiveAlcove", PrologueSectorId.CanyonDescent, new Rect2(0, 980, 360, 300), true),
        new("GearGraveyard", PrologueSectorId.CanyonBasin, new Rect2(0, 0, 520, 720), false),
        new("SteamPumpTrench", PrologueSectorId.CanyonBasin, new Rect2(520, 0, 580, 720), false),
        new("CheckpointCampSalvage", PrologueSectorId.CanyonBasin, new Rect2(1100, 0, 500, 720), false),
        new("CounterweightWell", PrologueSectorId.CanyonAscent, new Rect2(0, 900, 480, 540), false),
        new("ScaffoldPendulumGallery", PrologueSectorId.CanyonAscent, new Rect2(320, 360, 700, 900), false),
        new("CeilingRailHighRuin", PrologueSectorId.CanyonAscent, new Rect2(760, 0, 520, 560), false),
        new("OuterSealGuardWorkshop", PrologueSectorId.LostTemple, new Rect2(0, 0, 760, 720), false),
        new("BellAntechamberBossHall", PrologueSectorId.LostTemple, new Rect2(760, 0, 1160, 720), false)
    ];

    private static void AssertExactMicroSceneCatalog()
    {
        IReadOnlyList<CanyonMicroSceneDefinition> actual = CanyonContentCatalog.All;
        TestAssert.Equal(15, actual.Count, "dense canyon catalog contains exactly fifteen definitions");
        TestAssert.Equal(15, actual.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count(),
            "dense canyon catalog contains no duplicate ID");
        for (int index = 0; index < ExpectedMicroScenes.Length; index++)
        {
            ExpectedMicroScene expected = ExpectedMicroScenes[index];
            CanyonMicroSceneDefinition definition = actual[index];
            TestAssert.Equal(expected.Id, definition.Id, $"catalog item {index} has exact ID");
            TestAssert.Equal(expected.SectorId, definition.SectorId,
                $"catalog item {expected.Id} has exact sector");
            TestAssert.Equal(expected.LocalBounds, definition.LocalBounds,
                $"catalog item {expected.Id} has exact local bounds");
            TestAssert.Equal(expected.OptionalSubarea, definition.OptionalSubarea,
                $"catalog item {expected.Id} has exact optional flag");
        }
        TestAssert.Equal("ArchiveAlcove", actual.Single(item => item.OptionalSubarea).Id,
            "ArchiveAlcove is the catalog's only optional subarea");
    }

    private static void AssertMicroSceneMarkers(SectorCase expected, PrologueSector sector,
        ICollection<string> observed)
    {
        Node markers = sector.GetNodeOrNull("Gameplay/MicroScenes");
        TestAssert.True(markers != null, $"{expected.Id} exposes stable Gameplay/MicroScenes root");
        ExpectedMicroScene[] definitions = ExpectedMicroScenes
            .Where(item => item.SectorId == expected.Id).ToArray();
        TestAssert.Equal(definitions.Length, markers.GetChildCount(),
            $"{expected.Id} contains only its exact catalog markers");
        foreach (ExpectedMicroScene definition in definitions)
        {
            Marker2D marker = markers.GetNodeOrNull<Marker2D>(definition.Id);
            TestAssert.True(marker != null && definition.LocalBounds.HasPoint(marker.Position),
                $"{definition.Id} marker exists inside its literal local bounds");
            TestAssert.True(expected.Bounds.Encloses(definition.LocalBounds),
                $"{definition.Id} local bounds remain inside {expected.Id}");
            TestAssert.Equal(definition.OptionalSubarea,
                marker.GetMeta("optional_subarea", false).AsBool(),
                $"{definition.Id} marker carries its explicit optional identity");
            observed.Add(definition.Id);
        }
    }

    private static void AssertTypedGameplayBodies(SectorCase expected, Node gameplay, Node visual)
    {
        StaticBody2D[] bodies = Descendants(gameplay).OfType<StaticBody2D>()
            .Where(body => !BelongsToInstancedSubscene(body, gameplay)).ToArray();
        TestAssert.True(bodies.Length > 0, $"{expected.Id} contains generated gameplay bodies");
        foreach (StaticBody2D body in bodies)
        {
            GrappleSurface2D profile = body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
            TestAssert.True(profile != null && profile.UseCollisionHitPoint,
                $"{expected.Id}/{body.Name} owns a typed collision-hit profile");
            TestAssert.True(profile.Material is GrappleSurfaceMaterial.GripStone or
                    GrappleSurfaceMaterial.TempleMetal or GrappleSurfaceMaterial.Ice or
                    GrappleSurfaceMaterial.Forbidden,
                $"{expected.Id}/{body.Name} uses an explicit terrain material");
            TestAssert.Equal(CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
                body.CollisionLayer,
                $"{expected.Id}/{body.Name} participates in world and thread-target queries");
        }
        TestAssert.Equal(0, Descendants(visual).OfType<GrappleSurface2D>().Count(),
            $"{expected.Id} formal/replaceable art owns no collision or claw rules");
    }

    private static bool BelongsToInstancedSubscene(Node node, Node gameplay)
    {
        for (Node current = node.GetParent(); current != null && current != gameplay;
             current = current.GetParent())
            if (!string.IsNullOrEmpty(current.SceneFilePath)) return true;
        return false;
    }

    private static void AssertCatchLayer(PrologueSectorId id, PrologueSector sector,
        TraversalSegment2D segment)
    {
        string path = segment.SegmentId.ToString() switch
        {
            "descent_grip_lesson" => "Gameplay/Collision/GripStoneCatchZone",
            "ascent_grapple_one" => "Gameplay/Collision/CatchOne",
            "ascent_grapple_two" => "Gameplay/Collision/CatchTwo",
            "ascent_grapple_three" => "Gameplay/Collision/CatchThree",
            _ => throw new InvalidOperationException($"missing catch-layer authority for {segment.SegmentId}")
        };
        StaticBody2D catchLayer = sector.GetNodeOrNull<StaticBody2D>(path);
        TestAssert.True(catchLayer != null && catchLayer.HasMeta("catch_shelf"),
            $"{id}/{segment.SegmentId} exposes its named catch platform");
        float verticalDrop = catchLayer.Position.Y - segment.LaunchZone.GetCenter().Y;
        TestAssert.True(verticalDrop >= 0f && verticalDrop <= 320f,
            $"{id}/{segment.SegmentId} catch layer is within 320 vertical px below launch; " +
            $"actual={verticalDrop:0.00}");
        if (id == PrologueSectorId.CanyonAscent)
        {
            StaticBody2D returnSurface = sector.GetNode("Gameplay/Collision").GetChildren()
                .OfType<StaticBody2D>().SingleOrDefault(body =>
                    body.HasMeta("catch_return_for") &&
                    body.GetMeta("catch_return_for").AsString() == catchLayer.Name.ToString());
            GrappleSurface2D profile = returnSurface?.GetNodeOrNull<GrappleSurface2D>(
                "GrappleSurfaceProfile");
            TestAssert.True(returnSurface != null && profile != null && profile.UseCollisionHitPoint,
                $"{id}/{segment.SegmentId} exposes one explicit typed collision-hit return surface");
            RectangleShape2D catchShape = catchLayer.GetNode<CollisionShape2D>("Collision").Shape as
                RectangleShape2D ?? throw new InvalidOperationException("catch layer must be rectangular");
            RectangleShape2D returnShape = returnSurface.GetNode<CollisionShape2D>("Collision").Shape as
                RectangleShape2D ?? throw new InvalidOperationException("catch return must be rectangular");
            Vector2 retryStart = catchLayer.Position -
                new Vector2(0f, catchShape.Size.Y / 2f + 36f);
            Vector2 returnHit = FirstRayHitOnRectangle(retryStart, returnSurface.Position,
                returnSurface.Position, returnShape.Size);
            float returnDistance = retryStart.DistanceTo(returnHit);
            TestAssert.True(returnDistance >= 360f && returnDistance <= 520f,
                $"{id}/{segment.SegmentId} typed return collision hit on its authored center ray stays " +
                $"inside [360,520]; actual={returnDistance:0.00} hit={returnHit}");
        }
    }

    private static Vector2 FirstRayHitOnRectangle(Vector2 origin, Vector2 target,
        Vector2 center, Vector2 size)
    {
        Vector2 direction = target - origin;
        float entry = 0f;
        float exit = 1f;
        ClipRayAxis(origin.X, direction.X, center.X - size.X / 2f, center.X + size.X / 2f,
            ref entry, ref exit);
        ClipRayAxis(origin.Y, direction.Y, center.Y - size.Y / 2f, center.Y + size.Y / 2f,
            ref entry, ref exit);
        TestAssert.True(entry <= exit + .0001f && entry <= 1f && exit >= 0f,
            "authored return center ray intersects its authoritative collision rectangle");
        return origin + direction * Mathf.Clamp(entry, 0f, 1f);
    }

    private static void ClipRayAxis(float origin, float delta, float minimum, float maximum,
        ref float entry, ref float exit)
    {
        if (Mathf.IsZeroApprox(delta))
        {
            TestAssert.True(origin >= minimum && origin <= maximum,
                "parallel authored return ray remains inside collision bounds");
            return;
        }
        float first = (minimum - origin) / delta;
        float second = (maximum - origin) / delta;
        entry = Mathf.Max(entry, Mathf.Min(first, second));
        exit = Mathf.Min(exit, Mathf.Max(first, second));
    }

    private static Vector2 PositionInSector(PrologueSector sector, Node2D node)
    {
        Vector2 point = node is GrappleSurface2D surface ? surface.AnchorOffset : Vector2.Zero;
        for (Node current = node; current != null && current != sector; current = current.GetParent())
            if (current is Node2D canvas)
                point = canvas.Transform * point;
        return point;
    }

    private static void AssertCriticalSurfaceClearOfSeams(SectorCase sector, Vector2 point)
    {
        float left = point.X - sector.Bounds.Position.X;
        float right = sector.Bounds.End.X - point.X;
        float top = point.Y - sector.Bounds.Position.Y;
        float bottom = sector.Bounds.End.Y - point.Y;
        switch (sector.Id)
        {
            case PrologueSectorId.CanyonEntrance:
                TestAssert.True(right >= SeamMargin, "entrance grapple surfaces clear the forward seam");
                break;
            case PrologueSectorId.CanyonDescent:
                TestAssert.True(left >= SeamMargin && bottom >= SeamMargin,
                    "descent grapple surfaces clear both unload seams");
                break;
            case PrologueSectorId.CanyonBasin:
                TestAssert.True(top >= SeamMargin && right >= SeamMargin,
                    "basin grapple surfaces clear both unload seams");
                break;
            case PrologueSectorId.CanyonAscent:
                TestAssert.True(left >= SeamMargin && right >= SeamMargin,
                    "ascent grapple surfaces clear both unload seams");
                break;
            case PrologueSectorId.LostTemple:
                TestAssert.True(left >= SeamMargin, "temple grapple surfaces clear the entry seam");
                break;
        }
    }

    private static void AssertAuthoredContent(PrologueSectorId id, PrologueSector sector)
    {
        switch (id)
        {
            case PrologueSectorId.CanyonEntrance:
                RustShellSquire rust = sector.GetNodeOrNull<RustShellSquire>(
                    "Gameplay/Actors/RustShellSquire");
                TestAssert.True(rust != null && rust.HasMeta("encounter_pressure") &&
                    rust.GetMeta("encounter_pressure").AsString() == "low",
                    "entrance contains exactly one explicitly low-pressure Rust-Shell lesson");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<RustShellSquire>().Count(),
                    "entrance contains only one Rust-Shell enemy");
                break;
            case PrologueSectorId.CanyonDescent:
                AssertSurface(sector, "Gameplay/GrappleSurfaces/GripStoneLesson",
                    GrappleSurfaceMaterial.GripStone);
                AssertSurface(sector, "Gameplay/GrappleSurfaces/DedicatedLessonAnchor",
                    GrappleSurfaceMaterial.DedicatedAnchor);
                AssertSurface(sector, "Gameplay/GrappleSurfaces/VisibleIceRejection",
                    GrappleSurfaceMaterial.Ice);
                TestAssert.True(sector.GetNode("Gameplay/Collision").GetChildren()
                    .Count(node => node.HasMeta("catch_shelf")) >= 2,
                    "descent places catch platforms below every required attempt");
                AssertTerrainProfile(sector, "Gameplay/Collision/CraneShaftWall",
                    GrappleSurfaceMaterial.GripStone);
                AssertTerrainProfile(sector, "Gameplay/Collision/FrostVentCeiling",
                    GrappleSurfaceMaterial.GripStone);
                AssertTerrainProfile(sector, "Gameplay/Collision/VisibleIceWall",
                    GrappleSurfaceMaterial.Ice);
                AssertMechanism(sector, "Gameplay/Mechanisms/Counterweight",
                    CanyonClawMechanismKind.Counterweight);
                AssertMechanism(sector, "Gameplay/Mechanisms/HiddenPanel",
                    CanyonClawMechanismKind.HiddenPanel);
                AssertOptionalPath(sector, "ArchiveAlcove");
                break;
            case PrologueSectorId.CanyonBasin:
                TestAssert.True(sector.GetNodeOrNull<Area2D>("Gameplay/ShieldLane") != null,
                    "basin contains the right-mouse shield lane");
                TestAssert.True(sector.GetNodeOrNull<CheckpointTrigger>("Gameplay/Checkpoint") != null,
                    "basin contains the first formal checkpoint");
                TestAssert.True(sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/PassStart") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/PassEnd") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/SkipStart") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/SupplyCave/SkipEnd") != null,
                    "supply cave exposes independent pass and skip routes that rejoin mainline");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<RustShellSquire>().Count(),
                    "basin reuses exactly one Rust-Shell for mixed low-pressure combat");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<BellBackLaborer>().Count(),
                    "basin reuses exactly one Laborer for mixed low-pressure combat");
                AssertDecorativeCargoRemnant(sector);
                AssertOptionalPath(sector, "SalvageCache");
                break;
            case PrologueSectorId.CanyonAscent:
                TestAssert.True(sector.GetNode("Gameplay/Collision").GetChildren()
                    .Count(node => node.HasMeta("catch_shelf")) >= 3,
                    "ascent has a lower catch shelf for each assisted grapple");
                TestAssert.True(sector.GetNodeOrNull("Gameplay/OptionalPaths/HighRuin/PassStart") != null &&
                    sector.GetNodeOrNull("Gameplay/OptionalPaths/HighRuin/SkipEnd") != null,
                    "ascent high ruin is an explicit non-blocking optional detour");
                TestAssert.Equal(1, sector.FindChildren("*", null, true, false)
                    .OfType<GraveLampMoth>().Count(),
                    "ascent reuses one Moth sightline without adding a mandatory combat wave");
                break;
            case PrologueSectorId.LostTemple:
                TestAssert.True(sector.GetNodeOrNull<BellChainMechanism>(
                    "Gameplay/Mechanisms/BellChain") != null,
                    "temple reuses the claw-compatible Bell Chain mechanism");
                TestAssert.True(sector.GetNodeOrNull<CheckpointTrigger>(
                    "Gameplay/TempleCheckpoint") != null,
                    "temple contains the second formal checkpoint");
                BossArenaController arena = sector.GetNodeOrNull<BossArenaController>(
                    "Gameplay/BossArenaController");
                Area2D trigger = sector.GetNodeOrNull<Area2D>("Gameplay/ActivationTrigger");
                TestAssert.True(arena?.BossScene != null && trigger != null &&
                    sector.GetNodeOrNull("Gameplay/Doors/Entry/Collision") != null,
                    "temple reuses the Fallen Bellkeeper arena behind an authored activation trigger");
                AssertMechanism(sector, "Gameplay/Mechanisms/BellBody",
                    CanyonClawMechanismKind.BellBody);
                break;
        }
    }

    private static void AssertDecorativeCargoRemnant(Node sector)
    {
        // Deliberate authored-design revision: Cargo no longer advertises a blocked
        // interaction beside BasinGripStone. Generic Cargo adapter coverage stays elsewhere.
        Node2D remnant = sector.GetNodeOrNull<Node2D>("VisualRoot/Midground/CargoRemnant");
        TestAssert.True(remnant != null && remnant.GetType() == typeof(Node2D),
            "Basin retains cargo context as a plain replaceable midground Node2D");
        TestAssert.True(Descendants(remnant).OfType<Polygon2D>().Any(polygon => polygon.Polygon.Length >= 3),
            "cargo remnant contains actual replaceable polygon art rather than an empty placeholder");
        TestAssert.True(!Descendants(remnant).Any(node => node is GrappleSurface2D or
                CollisionObject2D or LegacyThreadInteractionAdapter or CanyonClawMechanism2D),
            "decorative cargo cannot own grab targets, physics collision or interaction adapters");
        TestAssert.True(sector.GetNodeOrNull("Gameplay/Mechanisms/CargoPlatform") == null,
            "retired CargoPlatform has no leftover gameplay entry beside the stone anchor");
    }

    private static void AssertTerrainProfile(Node root, string bodyPath,
        GrappleSurfaceMaterial material)
    {
        StaticBody2D body = root.GetNode<StaticBody2D>(bodyPath);
        GrappleSurface2D profile = body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        TestAssert.True(profile.UseCollisionHitPoint && profile.Material == material,
            $"{bodyPath} owns literal typed terrain material {material}");
    }

    private static void AssertMechanism(Node root, string path, CanyonClawMechanismKind kind)
    {
        CanyonClawMechanism2D mechanism = root.GetNodeOrNull<CanyonClawMechanism2D>(path);
        TestAssert.True(mechanism != null && mechanism.Kind == kind &&
            mechanism.GetNodeOrNull<ThreadTarget2D>("ThreadTarget") != null &&
            mechanism.GetNodeOrNull("LinkedNode") != null,
            $"{path} exposes the literal {kind} one-shot claw mechanism contract");
    }

    private static void AssertOptionalPath(Node root, string name)
    {
        Node path = root.GetNodeOrNull($"Gameplay/OptionalPaths/{name}");
        TestAssert.True(path?.GetNodeOrNull<Marker2D>("PassStart") != null &&
            path.GetNodeOrNull<Marker2D>("PassEnd") != null &&
            path.GetNodeOrNull<Marker2D>("SkipStart") != null &&
            path.GetNodeOrNull<Marker2D>("SkipEnd") != null,
            $"{name} exposes pass/skip markers and therefore cannot gate mainline completion");
    }

    private static void AssertSurface(Node root, string path, GrappleSurfaceMaterial material) =>
        TestAssert.Equal(material, root.GetNode<GrappleSurface2D>(path).Material,
            $"{path} exposes literal {material} gameplay identity");

    private static void AssertMainComposition(PackedScene packed)
    {
        TestAssert.True(packed != null, "generated canyon main scene loads");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try
        {
            SeamlessPrologueWorld world = level.GetNodeOrNull<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            TestAssert.True(world != null && world.Definitions.Count == Sectors.Length,
                "main scene composes exactly five authored canyon definitions");
            var definitions = new List<PrologueSectorDefinition>();
            for (int index = 0; index < Sectors.Length; index++)
            {
                SectorCase expected = Sectors[index];
                PrologueSectorDefinition definition = world.Definitions[index];
                TestAssert.Equal(expected.Id, definition.Id,
                    $"main definition {index} preserves route identity");
                TestAssert.Equal(expected.Bounds, definition.Bounds,
                    $"main definition {expected.Id} preserves local bounds");
                TestAssert.Equal(expected.WorldOffset, definition.WorldOffset,
                    $"main definition {expected.Id} preserves U-shaped world offset");
                TestAssert.Equal(expected.Path, definition.Scene.ResourcePath,
                    $"main definition {expected.Id} references the authored sector resource");
                TestAssert.True(definition.KeyInteractionBounds.Count > 0,
                    $"main definition {expected.Id} declares critical interaction bounds");
                definitions.Add(definition);
            }
            _ = new PrologueSectorStreamModel(definitions);
        }
        finally
        {
            level.Free();
        }
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private sealed record SectorCase(PrologueSectorId Id, string Path, Rect2 Bounds,
        Vector2 WorldOffset, string[] Segments);

    private sealed record ExpectedMicroScene(string Id, PrologueSectorId SectorId,
        Rect2 LocalBounds, bool OptionalSubarea);
}
