using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class CanyonResidentCatalogTests
{
    public static void Run()
    {
        IReadOnlyList<ResidentPopulationDefinition> all = CanyonResidentCatalog.All;
        TestAssert.Equal(50, all.Count, "resident allocation contains fifty additional stable identities");
        TestAssert.Equal(50, all.Select(x => x.StableId).Distinct(StringComparer.Ordinal).Count(),
            "fifty identities are unique, not waves or alternate sites");
        TestAssert.Equal(0, CanyonResidentCatalog.ValidateAllocation().Count,
            "complete authored allocation passes the production validator");
        TestAssert.Equal(24, CanyonResidentCatalog.Subtypes.Count, "all twenty-four configured subtypes retained");
        foreach ((ResidentFamily family, int count) in new[]
        {
            (ResidentFamily.Native, 8), (ResidentFamily.Scrap, 12), (ResidentFamily.Precision, 10),
            (ResidentFamily.Bone, 10), (ResidentFamily.Ghost, 10)
        }) TestAssert.Equal(count, all.Count(x => x.Family == family), $"literal family allocation {family}");
        foreach ((ResidentArchetype role, int count) in new[]
        {
            (ResidentArchetype.Wanderer, 16), (ResidentArchetype.Charger, 7), (ResidentArchetype.Flyer, 8),
            (ResidentArchetype.Diver, 6), (ResidentArchetype.Turret, 9), (ResidentArchetype.Heavy, 4)
        }) TestAssert.Equal(count, all.Count(x => x.Archetype == role), $"literal role allocation {role}");

        // Literal independent region AND subtype multisets, not counts derived
        // from the implementation's authoring loops or expected-count helper.
        foreach ((CanyonRouteNodeId route, string[] subtypeIds) in new[]
        {
            (CanyonRouteNodeId.LowCeilingBridge, new[] { "rock-wanderer", "ridge-charger", "scrap-scatter" }),
            (CanyonRouteNodeId.StaggeredRift, new[] { "rock-wanderer", "rock-wanderer", "ridge-charger", "rift-flyer", "precision-burst" }),
            (CanyonRouteNodeId.ControlledAbyss, new[] { "lost-echo", "rift-flyer", "cliff-diver", "scrap-diver", "precision-missile" }),
            (CanyonRouteNodeId.ArchiveSideCave, new[] { "bone-wanderer", "bone-wanderer", "bone-charger", "bone-diver", "ghost-tower" }),
            (CanyonRouteNodeId.FrozenDetour, new[] { "lost-echo", "lost-echo", "bone-charger", "bound-ghost", "bound-ghost", "ghost-diver" }),
            (CanyonRouteNodeId.BasinCamp, Array.Empty<string>()),
            (CanyonRouteNodeId.GearPistons, new[] { "scrap-wanderer", "scrap-wanderer", "scrap-charger", "scrap-flyer", "scrap-scatter", "scrap-heavy", "precision-heavy" }),
            (CanyonRouteNodeId.SteamValves, new[] { "scrap-wanderer", "scrap-wanderer", "scrap-charger", "scrap-flyer", "precision-missile", "bone-heavy" }),
            (CanyonRouteNodeId.CounterweightLift, new[] { "bone-wanderer", "precision-flyer", "ghost-diver", "precision-burst", "precision-heavy" }),
            (CanyonRouteNodeId.PendulumGallery, new[] { "bone-wanderer", "precision-charger", "bone-diver", "precision-laser" }),
            (CanyonRouteNodeId.SplitLevelEncounter, new[] { "bone-wanderer", "lost-echo", "bound-ghost" }),
            (CanyonRouteNodeId.SealedCourtyard, new[] { "precision-laser" }),
            (CanyonRouteNodeId.BossHall, Array.Empty<string>()),
            (CanyonRouteNodeId.SafeExit, Array.Empty<string>())
        }) TestAssert.True(all.Where(x => x.RouteNode == route).Select(x => x.SubtypeId)
                .OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(subtypeIds.OrderBy(x => x, StringComparer.Ordinal)),
                $"literal complete subtype multiset for {route}");
        TestAssert.Equal(33, all.Count(x => !CanyonWorldLayout.Nodes.Single(n => n.Id == x.RouteNode).Optional),
            "main receives exactly thirty-three residents");
        TestAssert.Equal(17, all.Count(x => CanyonWorldLayout.Nodes.Single(n => n.Id == x.RouteNode).Optional),
            "optional routes receive exactly seventeen residents");
        TestAssert.Equal(12, CanyonEncounterCatalog.Normal.Count, "old twelve wave groups remain separate");
        TestAssert.Equal(26, CanyonEncounterCatalog.Normal.Sum(x => x.Enemies.Count), "old twenty-six slots retained");
        TestAssert.Equal(76, all.Count + CanyonEncounterCatalog.Normal.Sum(x => x.Enemies.Count),
            "whole target is seventy-six ordinary identities, boss is separate");

        TestAssert.True(CanyonResidentCatalog.Subtypes.All(x => x.SceneState == ResidentSceneState.Unimplemented &&
            x.ScenePath == null && x.ProbeScenePath.StartsWith("res://scenes/enemies/", StringComparison.Ordinal)),
            "probe scenes are explicit references, never finished family assets");
        TestAssert.True(CanyonResidentCatalog.Subtypes.Where(x => x.Family == ResidentFamily.Bone)
            .Select(x => x.DisplayName).OrderBy(x => x).SequenceEqual(
                new[] { "废骨缝械", "骨轮突进者", "精制骨翼", "骨架守卫" }.OrderBy(x => x)),
            "bone names and all four configured roles retained");
        TestAssert.True(CanyonResidentCatalog.Subtypes.Where(x => x.Family == ResidentFamily.Ghost)
            .Select(x => x.DisplayName).OrderBy(x => x).SequenceEqual(
                new[] { "失序残响", "被缚游魂", "断片俯灵", "灵能拘束塔" }.OrderBy(x => x)),
            "ghost names and all four configured roles retained");
        TestAssert.True(all.All(x => x.Placement == null), "unplaced identity does not invent origin coordinates");
        IReadOnlyList<string> pending = CanyonResidentCatalog.ValidatePlacementCandidates();
        TestAssert.Equal(50, pending.Count, "every unplaced identity is reported, none silently skipped");
        TestAssert.True(all.All(x => pending.Contains($"placement.not_yet_placed:{x.StableId}")),
            "not-yet-placed failures bind every stable identity");
        TestAssert.Equal(50, CanyonResidentCatalog.ValidateSceneReadiness().Count,
            "allocation green is not scene readiness green");

        int negatives = 0;
        var moved = all.ToArray();
        int rift = Array.FindIndex(moved, x => x.RouteNode == CanyonRouteNodeId.StaggeredRift);
        moved[0] = all[0] with { RouteNode = all[rift].RouteNode, Sector = all[rift].Sector };
        moved[rift] = all[rift] with { RouteNode = all[0].RouteNode, Sector = all[0].Sector };
        TestAssert.True(CanyonResidentCatalog.ValidateAllocation(moved).Any(x => x.StartsWith("identity_binding:", StringComparison.Ordinal)),
            "same checker rejects remapping stable identities while preserving every population total");
        negatives++;
        Reject(all[0] with { StableId = "canyon.v2.resident.unregistered.01" }, "unknown_id");
        Reject(all[0] with { StableId = all[1].StableId }, "duplicate_id");
        Reject(all[0] with { Family = ResidentFamily.Ghost }, "subtype_family");
        Reject(all[0] with { Archetype = ResidentArchetype.Turret }, "subtype_archetype");
        Reject(all[0] with { Sector = PrologueSectorId.LostTemple }, "route_sector");
        Reject(all[0] with { RouteNode = (CanyonRouteNodeId)999 }, "unknown_route");
        Reject(all[0] with { RouteNode = CanyonRouteNodeId.BasinCamp, Sector = PrologueSectorId.CanyonBasin }, "protected_route");
        Reject(all[0] with { SubtypeId = "missing-subtype" }, "unknown_subtype");
        Reject(all[0] with { StableId = "" }, "stable_id");
        TestAssert.True(CanyonResidentCatalog.ValidateAllocation(all.Skip(1).ToArray())
            .Contains("population_count:49"), "same checker rejects a silently reduced target");
        negatives++;

        Vector2[] points = { new(900, 500), new(1000, 500) };
        var candidate = new ResidentPlacementCandidate(new Vector2(950, 500), new Rect2(850, 450, 200, 100), points);
        points[0] = new Vector2(-1, -1);
        TestAssert.Equal(new Vector2(900, 500), candidate.PatrolPoints[0], "candidate defensively copies caller patrol points");
        AssertReadOnly((IList<Vector2>)candidate.PatrolPoints, new Vector2(1, 1));
        AssertReadOnly((IList<ResidentPopulationDefinition>)all, all[1]);
        var changed = all.ToArray();
        changed[0] = all[0] with { Placement = candidate };
        TestAssert.True(CanyonResidentCatalog.ValidatePlacementCandidates(changed).Contains(
            $"placement.native_proof_required:{all[0].StableId}"), "structurally valid candidate is still not native-safe");
        PlacementReject(new Vector2(1100, 500), candidate.Territory, candidate.PatrolPoints, "spawn_outside_territory");
        PlacementReject(new Vector2(float.NaN, 500), candidate.Territory, candidate.PatrolPoints, "non_finite");
        PlacementReject(candidate.WorldSpawn, candidate.Territory, new[] { new Vector2(1, 1) }, "patrol_outside_territory");
        PlacementReject(candidate.WorldSpawn, new Rect2(850, 450, 0, 100), candidate.PatrolPoints, "invalid_territory");
        PlacementReject(candidate.WorldSpawn, new Rect2(850, 450, 1000, 100), candidate.PatrolPoints, "territory_outside_route");
        PlacementReject(candidate.WorldSpawn, candidate.Territory, Array.Empty<Vector2>(), "patrol_count");
        GD.Print($"RESIDENT_ALLOCATION_OK identities=50 subtypes=24 old_slots=26 bone=10 ghost=10 main=33 optional=17");
        GD.Print($"RESIDENT_ALLOCATION_NEGATIVES_OK cases={negatives} same_checker=true");
        GD.Print("RESIDENT_READINESS_UNRESOLVED sites=50 scenes=50 spawned=0 native_safe=false whole_goal=false");

        void Reject(ResidentPopulationDefinition replacement, string code)
        {
            var mutated = all.ToArray(); mutated[0] = replacement;
            TestAssert.True(CanyonResidentCatalog.ValidateAllocation(mutated).Any(x => x.StartsWith(code + ":", StringComparison.Ordinal)),
                $"same allocation validator rejects {code}");
            negatives++;
        }
        void PlacementReject(Vector2 spawn, Rect2 territory, IEnumerable<Vector2> patrol, string code)
        {
            var mutated = all.ToArray();
            mutated[0] = all[0] with { Placement = new ResidentPlacementCandidate(spawn, territory, patrol) };
            TestAssert.True(CanyonResidentCatalog.ValidatePlacementCandidates(mutated).Contains($"placement.{code}:{all[0].StableId}"),
                $"same candidate validator rejects {code}");
            negatives++;
        }
    }

    private static void AssertReadOnly<T>(IList<T> values, T replacement)
    {
        bool refused = false;
        try { values[0] = replacement; }
        catch (NotSupportedException) { refused = true; }
        TestAssert.True(refused, "immutable contract refuses caller overwrite");
    }
}
