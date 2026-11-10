using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Integration;

public static class CanyonIncineratorLayoutTests
{
    // Break caught: a miniature/reset-only pit, a missing return route, or a
    // generated-only edit whose saved parent still loads the old basin/map.
    public static Task Run(SceneTree tree)
    {
        AssertSteamLandingRecoveryClearance();
        AssertAnnexGraphQualification();
        TestAssert.Equal(new Rect2(1280,3600,5120,720),
            CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin],
            "full incinerator annex requires an actual 1280px eastern basin extension");
        TestAssert.Equal(new Rect2(0,0,7680,4320), CanyonWorldLayout.DesignBounds,
            "annex stays inside original world envelope");
        var graphErrors=CanyonWorldLayout.Validate();
        TestAssert.True(graphErrors.Count == 0, "extended graph remains valid: "+string.Join("; ",graphErrors));
        TestAssert.True(CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.start"] == new Vector2(160,600) &&
            CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.basin-camp"] == new Vector2(1680,3960) &&
            CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"] == new Vector2(5920,1080),
            "annex does not relocate existing legal checkpoints");
        TestAssert.True(CanyonWorldLayout.TryResolveRouteNode(new(5000,3564), out var steam) &&
            steam == CanyonRouteNodeId.CounterweightLift,
            "existing steam top standing point remains inside the original Ascent lift node, before fallback");
        TestAssert.True(CanyonWorldLayout.TryResolveRouteNode(new(5600,3720), out var annex) &&
            annex.ToString() == "IncineratorAnnex" &&
            CanyonWorldLayout.TryResolveRouteNode(new(4800,3960), out var connector) && connector == annex,
            "new room and lower access support resolve without swallowing old steam corridor");
        TestAssert.Equal(12, CanyonWorldLayout.Nodes.Count(n => n.ChallengeKind.HasValue),
            "annex is an optional environment room, not a duplicate combat challenge");
        TestAssert.Equal(26, CanyonEncounterCatalog.Normal.Sum(e => e.Enemies.Count),
            "original encounter slots are not removed to make room for the furnace");

        PrologueSector built = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
        try { CheckRoom(built); }
        finally { built.Free(); }

        using PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try
        {
            var definitions = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
            TestAssert.Equal(5, definitions.Length, "annex retains original five save-visible sectors");
            var definition = definitions.Single(d => d.Id == PrologueSectorId.CanyonBasin);
            TestAssert.True(definition.WorldOffset == new Vector2(1280,3600) &&
                definition.Bounds == new Rect2(0,0,5120,720),
                "saved parent definition contains expanded leaf bounds with unchanged world offset");
            PrologueSector saved = definition.Scene.Instantiate<PrologueSector>();
            try
            {
                TestAssert.True(saved.ValidateContract(definition).Count == 0,
                    "saved sector contract matches the parent actually loaded by the game");
                CheckRoom(saved);
            }
            finally { saved.Free(); }
            using MinimapTerrainCatalog expected = MinimapTerrainCatalogBuilder.Build(definitions);
            MinimapTerrainCatalog catalog = level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            TestAssert.True(expected.Revision == catalog.Revision && expected.SolidCount == catalog.SolidCount &&
                expected.SerializedValues.AsSpan().SequenceEqual(catalog.SerializedValues),
                "saved minimap agrees byte-for-byte with actual structural solids, not a stale preview");
        }
        finally { level.Free(); }
        GD.Print("INCINERATOR_LAYOUT_OK room_width=1280 pool_width=720 original_encounters=26 " +
            "saved_leaf=true saved_parent=true minimap=true right_return_steps=3 keyboard_route=false human=false whole_goal=false");
        return Task.CompletedTask;
    }

    // Break caught: publishing the upper return as SteamValves despite its
    // actual Ascent lift landing, or using its exception to waive other gates.
    private static void AssertAnnexGraphQualification()
    {
        CanyonRouteEdge lower=CanyonWorldLayout.Edges.Single(e=>
            e.From==CanyonRouteNodeId.SteamValves && e.To==CanyonRouteNodeId.IncineratorAnnex);
        TestAssert.True(!lower.Bidirectional && lower.ReverseTraversalSegmentId=="",
            "lower steam access is one-way; the physical return reaches the lift, not the lower steam route");
        CanyonRouteEdge upper=CanyonWorldLayout.Edges.SingleOrDefault(e=>
            e.From==CanyonRouteNodeId.CounterweightLift && e.To==CanyonRouteNodeId.IncineratorAnnex);
        TestAssert.True(upper is {Bidirectional:true,Gate:CanyonRouteGate.None} &&
            upper.TraversalSegmentId=="canyon.v2.route.lift-to-incinerator" &&
            upper.ReverseTraversalSegmentId=="canyon.v2.route.incinerator-to-lift",
            "the physically witnessed upper entry and service return have their exact lift endpoints");
        TestAssert.Equal(0,Validate(upper).Count,"exact lift/annex return must be allowed alongside existing returns");
        foreach(CanyonRouteEdge invalid in new[]
        {
            upper with {From=CanyonRouteNodeId.SteamValves},
            upper with {To=CanyonRouteNodeId.BossHall},
            upper with {Gate=CanyonRouteGate.BossDefeated},
            upper with {TraversalSegmentId="canyon.v2.route.unwitnessed-entry"},
            upper with {ReverseTraversalSegmentId="canyon.v2.route.unwitnessed-return"}
        })
            TestAssert.True(Validate(invalid).Any(e=>e.Contains("unapproved bidirectional",StringComparison.Ordinal)),
                "named lift return cannot waive changed endpoints, gates, entry or return IDs: "+invalid);

        System.Collections.Generic.IReadOnlyList<string> Validate(CanyonRouteEdge replacement) =>
            CanyonRouteGraph.Validate(CanyonWorldLayout.DesignBounds,CanyonWorldLayout.SectorBounds,
                CanyonWorldLayout.Nodes,CanyonWorldLayout.Edges.Select(e=>e==upper?replacement:e).ToArray(),
                CanyonWorldLayout.CheckpointWorldPositions,CanyonWorldLayout.StableCheckpointIds,
                CanyonWorldLayout.StableShortcutIds,CanyonWorldLayout.StableMechanismIds,CanyonWorldLayout.StableDiscoveryIds);
    }

    internal static void AssertSteamLandingRecoveryClearance()
    {
        // GD.Load borrows the cached scene also referenced by World definitions;
        // only the instances below belong to this helper, not that shared resource.
        PackedScene packed=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn");
        var ascent=packed.Instantiate<PrologueSector>();
        PrologueSector built=null;
        try
        {
            Check(ascent);
            built=PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonAscent);
            Check(built);
        }
        finally {built?.Free();ascent.Free();}
        static void Check(PrologueSector ascent)
        {
            // Actual40x72 body swept across the complete existing180px Top,
            // not a point that missed the eastern32px KillZone remnant.
            Rect2 supported=new(4940,3528,180,72);
            foreach(Area2D area in ascent.GetNode("Gameplay/FallRecovery").GetChildren().OfType<Area2D>())
            {
                var shape=area.GetNode<CollisionShape2D>("Collision");
                Rect2 danger=new Transform2D(0,new Vector2(3840,720))*area.Transform*shape.Transform*shape.Shape.GetRect();
                TestAssert.True(!danger.Intersects(supported),
                    "standing steam Top must not overlap an Ascent recovery volume: "+danger);
            }
        }
    }

    private static void CheckRoom(PrologueSector sector)
    {
        var contracts=sector.GetNode("Gameplay/TraversalContracts").GetChildren()
            .OfType<TraversalSegment2D>().ToArray();
        TraversalSegment2D entry=contracts.SingleOrDefault(s=>s.SegmentId=="canyon.v2.route.lift-to-incinerator");
        TraversalSegment2D returning=contracts.SingleOrDefault(s=>s.SegmentId=="canyon.v2.route.incinerator-to-lift");
        TestAssert.True(entry!=null && entry.Kind==TraversalSegmentKind.GroundTransfer &&
            entry.LaunchZone==new Rect2(3720,-72,100,72) && entry.LandingZone==new Rect2(3880,4,80,72) &&
            returning!=null && returning.Kind==TraversalSegmentKind.SingleJump &&
            returning.LaunchZone==new Rect2(3840,4,100,72) && returning.LandingZone==new Rect2(3720,-72,100,72) &&
            !contracts.Any(s=>s.SegmentId=="canyon.v2.route.incinerator-to-steam"),
            "generated and actually saved route contracts must identify the real lift landing and upper entry");
        CheckSolid("IncineratorUpperEntryA", new(5100,3676,160,24), true);
        CheckSolid("IncineratorUpperEntryB", new(5240,3756,140,24));
        CheckSolid("IncineratorSafeGallery", new(5360,3836,800,24));
        CheckSolid("IncineratorWestBank", new(4680,3996,760,24));
        CheckSolid("IncineratorContainmentFloor", new(5440,4296,720,8));
        CheckSolid("IncineratorEastBank", new(6160,3996,216,24));
        CheckSolid("IncineratorEastWall", new(6376,3600,24,720));
        CheckSolid("IncineratorRoof", new(5360,3600,1040,40));
        CheckSolid("IncineratorReturn1", new(6240,3916,136,16), true);
        CheckSolid("IncineratorReturn2", new(6240,3836,136,16), true);
        CheckSolid("IncineratorReturn3", new(6240,3756,136,16), true);
        CheckSolid("IncineratorWestReturn1", new(5140,3916,100,16), true);
        CheckSolid("IncineratorWestReturn2", new(5140,3836,100,16), true);
        CheckSolid("IncineratorWestReturn3", new(5140,3756,100,16), true);
        TestAssert.Equal(new Vector2(4720,200),
            sector.GetNode<Marker2D>("Gameplay/MicroScenes/IncineratorAnnex").Position,
            "route guidance points to the actual safe gallery standing point, not above the pool");
        Area2D pit = sector.GetNodeOrNull<Area2D>("Gameplay/Mechanisms/IncineratorPool");
        TestAssert.True(pit != null && pit.GetType().FullName == "Knight.Mechanisms.CanyonIncinerator",
            "saved large pool uses real lethal terrain, not KillZone FALL RESET");
        CollisionShape2D collision = pit.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(collision.Shape is RectangleShape2D { Size: var size } && size == new Vector2(720,276) &&
            !collision.Disabled && WorldRect(pit, collision) == new Rect2(5440,4020,720,276),
            "actual lethal rectangle covers complete pool below the visible lip and above the containment floor");

        void CheckSolid(string name, Rect2 expected, bool oneWay = false)
        {
            StaticBody2D body = sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/" + name);
            TestAssert.True(body != null, "actual annex support missing: " + name);
            CollisionShape2D shape = body.GetNode<CollisionShape2D>("Collision");
            TestAssert.True(!shape.Disabled && shape.OneWayCollision == oneWay && WorldRect(body, shape) == expected,
                "actual annex support or one-way return geometry differs: " + name);
        }
        Rect2 WorldRect(Node2D body, CollisionShape2D shape) =>
            new Transform2D(0,new Vector2(1280,3600)) * body.Transform * shape.Transform * shape.Shape.GetRect();
    }
}
