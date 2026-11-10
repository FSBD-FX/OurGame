using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    public static void RunAuthoredLayout()
    {
        const string digest = "AEACB6915FD16F9BDEE4ECC93FC59AD55D028EAE40E05EFCE83926831762DF18";
        const string context = "376E243A4D9DF7363A97769F2F6A062853B1DBF52EC698D017035B703B119707";
        byte[] bytes = FileAccess.GetFileAsBytes("res://assets/data/residents/canyon-resident-layout-v001.json");
        TestAssert.Equal(digest, Convert.ToHexString(SHA256.HashData(bytes)), "actual runtime authored input has the independently pinned digest");
        var sites = ResidentAuthoredLayout.Parse(bytes, digest, context);
        TestAssert.Equal(50, sites.Count, "runtime authored input must bind all fifty real site rows, not an empty allocation");
        TestAssert.Equal(new Vector2(666, 604), sites[0].Definition.Placement.WorldSpawn, "literal bridge home");
        TestAssert.Equal(new Vector2(1818, 1744), sites[13].Definition.Placement.WorldSpawn, "literal lower Archive home uses expanded-layout source");
        TestAssert.Equal(new Vector2(5153, 1211), sites[49].Definition.Placement.WorldSpawn, "literal courtyard turret home");
        using (var source = JsonDocument.Parse(bytes))
        {
            for (int i = 0; i < 50; i++)
            {
                var row = source.RootElement.GetProperty("sites")[i];
                var site = sites[i]; var slot = CanyonResidentCatalog.All[i]; var p = site.Definition.Placement;
                TestAssert.True(site.Definition == slot with { Placement = p } && site.Definition.StableId == row.GetProperty("stableId").GetString(),
                    "authored data inherits identity, family, sector and subtype instead of remapping allocation");
                TestAssert.True(p.WorldSpawn == Point(row.GetProperty("home")) && p.Territory == Rect(row.GetProperty("territory")) &&
                    site.Occupancy == Rect(row.GetProperty("occupancy")) && p.PatrolPoints.SequenceEqual(row.GetProperty("points").EnumerateArray().Select(Point)),
                    "every numeric field and every ordered patrol point round-trips from the real runtime input");
            }
        }
        AssertReadOnly((IList<ResidentAuthoredSite>)sites, sites[1]);
        AssertReadOnly((IList<Vector2>)sites[0].Definition.Placement.PatrolPoints, Vector2.Zero);
        byte[] changed = bytes.ToArray(); var retained = ResidentAuthoredLayout.Parse(changed, digest, context);
        Array.Fill(changed, (byte)0);
        TestAssert.True(retained[0].Definition.Placement.WorldSpawn == new Vector2(666, 604) &&
            retained[0].Definition.Placement.PatrolPoints[1] == new Vector2(602, 604), "parsed values own immutable copies, not caller bytes");

        int negatives = 0;
        void Reject(byte[] input, string label, string hash = null, string ctx = context)
        {
            bool refused = false;
            try { ResidentAuthoredLayout.Parse(input, hash ?? Convert.ToHexString(SHA256.HashData(input)), ctx); }
            catch (ArgumentException error) { refused = error.Message.StartsWith("resident_layout.", StringComparison.Ordinal); }
            TestAssert.True(refused, "same production layout consumer rejects " + label); negatives++;
        }
        void Mutate(string label, Action<JsonObject> mutation)
        {
            var root = JsonNode.Parse(bytes).AsObject(); mutation(root);
            Reject(Encoding.UTF8.GetBytes(root.ToJsonString()), label);
        }
        Mutate("missing identity", root => root["sites"].AsArray().RemoveAt(0));
        Mutate("duplicate identity", root => root["sites"][1]["stableId"] = root["sites"][0]["stableId"].GetValue<string>());
        Mutate("unknown identity", root => root["sites"][0]["stableId"] = "canyon.v2.resident.missing.01");
        Mutate("ordered identity swap", root => { var rows = root["sites"].AsArray(); var a = rows[0].DeepClone(); rows[0] = rows[1].DeepClone(); rows[1] = a; });
        Mutate("expanded territory", root => root["sites"][0]["territory"][2] = 1281);
        Mutate("home outside territory", root => root["sites"][0]["home"] = new JsonArray(-1, 604));
        Mutate("point shape", root => root["sites"][0]["home"] = new JsonArray(666));
        Mutate("home mismatch", root => root["sites"][0]["home"][0] = 667);
        Mutate("open route", root => root["sites"][0]["points"].AsArray().RemoveAt(3));
        Mutate("no motion", root => root["sites"][0]["points"] = new JsonArray(new JsonArray(666,604),new JsonArray(666,604),new JsonArray(666,604)));
        Mutate("turret movement", root => root["sites"][2]["points"].AsArray().Add(new JsonArray(315,455)));
        Mutate("too many points", root => { var points = root["sites"][0]["points"].AsArray(); while (points.Count <= 16) points.Add(new JsonArray(666,604)); });
        Mutate("empty occupancy", root => root["sites"][0]["occupancy"][2] = 0);
        Mutate("overflowed rectangle end", root => root["sites"][0]["occupancy"] = new JsonArray(float.MaxValue,0,float.MaxValue,1));
        Mutate("out of territory patrol", root => root["sites"][0]["points"][1][0] = -1);
        Mutate("wrong schema", root => root["schema"] = 2);
        Mutate("wrong kind", root => root["kind"] = "accepted-bucket");
        Mutate("unknown root field", root => root["formalCertificate"] = true);
        Mutate("unknown row field", root => root["sites"][0]["family"] = "Ghost");
        Mutate("missing field", root => root["sites"][0].AsObject().Remove("occupancy"));
        Mutate("null row", root => root["sites"][0] = null);
        Mutate("null points", root => root["sites"][0]["points"] = null);
        Mutate("nonnumeric point", root => root["sites"][0]["home"][0] = "666");
        Reject(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"schema\":1", "\"schema\":1,\"schema\":1")), "duplicate JSON property");
        Reject(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"home\":[666,604]", "\"home\":[1e99,604]")), "nonfinite single precision point");
        Reject(bytes, "digest", new string('0',64));
        Reject(bytes, "invalid digest format", new string('Z',64));
        Reject(bytes, "context", digest, new string('B',64));
        Reject(Encoding.UTF8.GetBytes("{"), "malformed JSON");
        Reject(Array.Empty<byte>(), "empty");
        Reject(new byte[1024*1024+1], "oversize");
        TestAssert.Equal(31, negatives, "all independently corrupted production layout controls executed");
        TestAssert.True(CanyonResidentCatalog.All.All(x => x.Placement == null), "allocation baseline remains unplaced; loaded data does not mutate it or certify scenes");
        GD.Print($"RESIDENT_AUTHORED_LAYOUT_OK identities=50 fields=all ordered=true immutable=true negatives={negatives} native_certificate=false spawned=0");
        static Vector2 Point(JsonElement p) => new(p[0].GetSingle(),p[1].GetSingle());
        static Rect2 Rect(JsonElement p) => new(p[0].GetSingle(),p[1].GetSingle(),p[2].GetSingle(),p[3].GetSingle());
    }
}
