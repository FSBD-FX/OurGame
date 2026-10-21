using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class CanyonEncounterCatalogTests
{
    private const string Rust = "res://scenes/enemies/RustShellSquire.tscn";
    private const string Laborer = "res://scenes/enemies/BellBackLaborer.tscn";
    private const string Moth = "res://scenes/enemies/GraveLampMoth.tscn";
    private const string Sentry = "res://scenes/enemies/BellBoltSentry.tscn";
    private const string Skitter = "res://scenes/enemies/CanyonSkitter.tscn";
    private const string Scatter = "res://scenes/enemies/RivetScatterGunner.tscn";
    private const string Burst = "res://scenes/enemies/BeltBurstGunner.tscn";
    private const string Missile = "res://scenes/enemies/FurnaceMissileGunner.tscn";
    private const string Laser = "res://scenes/enemies/PrismLaserGunner.tscn";

    public static void Run()
    {
        IReadOnlyList<CanyonEncounterDefinition> encounters = CanyonEncounterCatalog.Normal;
        TestAssert.Equal(8, encounters.Count(encounter => !encounter.Optional),
            "Task6 keeps exactly eight main-route normal groups");
        TestAssert.Equal(4, encounters.Count(encounter => encounter.Optional),
            "TASK6_BRANCH_REQUIRED_MUTATION optional layout keeps exactly four branch groups");
        TestAssert.Equal(12, encounters.Select(encounter => encounter.StableId).Distinct().Count(),
            "all twelve normal groups have unique stable IDs");
        // Break caught: the four real weapon roles never become authored map slots.
        TestAssert.Equal(26, encounters.Sum(encounter => encounter.Enemies.Count),
            "RANGED_PLACEMENT_CATALOG26_RED: retain old22 and add four actual weapon slots");
        TestAssert.Equal(17, encounters.Where(encounter => !encounter.Optional).Sum(encounter => encounter.Enemies.Count),
            "main route includes three new weapon roles without changing its eight stable groups");
        TestAssert.Equal(9, encounters.Where(encounter => encounter.Optional).Sum(encounter => encounter.Enemies.Count),
            "optional route retains old eight slots and adds one missile slot");
        CanyonEncounterDefinition split = encounters.Single(encounter =>
            encounter.StableId == "canyon.v2.encounter.split-level");
        CanyonEnemyPlacement splitMoth = split.Enemies.Single(enemy => enemy.ScenePath == Moth);
        CanyonEnemyPlacement splitLaborer = split.Enemies.Single(enemy => enemy.ScenePath == Laborer);
        // Conservative center envelope includes clearance for the authored radius-16 Moth body
        // above the gallery and below the arena ceiling; it is not a physics/navigation proof.
        TestAssert.True(splitMoth.WorldPosition.X >= 4100f && splitMoth.WorldPosition.X <= 4340f &&
                        splitMoth.WorldPosition.Y >= 1216f && splitMoth.WorldPosition.Y <= 1380f,
            "Split Moth starts within the conservative above-gallery clear flight-space envelope");
        TestAssert.True(splitMoth.ActivationBatch == 1 && splitLaborer.ActivationBatch == 0 &&
                        split.Enemies.Count(enemy => enemy.ActivationBatch == 1) == 1,
            "Split Moth keeps its own batch1 after the same batch0 Laborer");
        TestAssert.True(splitLaborer.WorldPosition.Y - splitMoth.WorldPosition.Y >= 140f,
            "Split Moth remains at least 140 pixels above the Laborer for the two-level read");
        string[] original17 =
        {
            "low-ceiling-bridge|RustShellSquire|560|520|0",
            "staggered-rift|RustShellSquire|1740|64|0",
            "controlled-abyss|GraveLampMoth|2280|3420|0",
            "gear-pistons|RustShellSquire|2300|3960|0",
            "gear-pistons|BellBackLaborer|2860|3794|1",
            "counterweight-lift|GraveLampMoth|5000|3000|0",
            "counterweight-lift|RustShellSquire|4700|3424|1",
            "pendulum-gallery|BellBackLaborer|4350|2318|0",
            "split-level|BellBackLaborer|4180|1438|0",
            "split-level|GraveLampMoth|4300|1280|1",
            "sealed-courtyard|RustShellSquire|5630|1160|0",
            "sealed-courtyard|BellBackLaborer|5790|1074|1",
            "archive-side-cave|RustShellSquire|1550|1604|0",
            "frozen-upper|GraveLampMoth|1410|2030|0",
            "steam-valves|BellBackLaborer|3600|4194|0",
            "frozen-lower|RustShellSquire|1460|3244|0",
            "frozen-lower|GraveLampMoth|1710|3020|1"
        };
        string[] actualOriginal = encounters.SelectMany(encounter => encounter.Enemies
            .Where(enemy => enemy.ScenePath is Rust or Laborer or Moth)
            .Select(enemy => $"{encounter.StableId["canyon.v2.encounter.".Length..]}|" +
                $"{System.IO.Path.GetFileNameWithoutExtension(enemy.ScenePath)}|" +
                $"{enemy.WorldPosition.X:0}|{enemy.WorldPosition.Y:0}|{enemy.ActivationBatch}"))
            .ToArray();
        TestAssert.True(actualOriginal.SequenceEqual(original17),
            "all seventeen scene/position/batch tuples stay authored except the explicit Split Moth flight-space repair");
        foreach (CanyonEncounterDefinition encounter in encounters)
        foreach (CanyonEnemyPlacement enemy in encounter.Enemies.Where(enemy => enemy.ScenePath is Rust or Laborer or Moth))
        {
            bool approvedRustAlternate = encounter.StableId == "canyon.v2.encounter.counterweight-lift" &&
                enemy.ScenePath == Rust && enemy.WorldPosition == new Vector2(4700, 3424) && enemy.ActivationBatch == 1;
            TestAssert.True(approvedRustAlternate
                    ? enemy.AlternateWorldPosition == new Vector2(4630, 3424)
                    : !enemy.AlternateWorldPosition.HasValue,
                "old seventeen primary/batch tuples preserve absent alternates except the explicitly approved Counterweight Rust4630 alternative");
        }
        foreach ((string suffix, string scene, Vector2 primary, Vector2 alternate) in new[]
        {
            ("controlled-abyss", Sentry, new Vector2(2450, 3531), new Vector2(2070, 3531)),
            ("frozen-upper", Sentry, new Vector2(1450, 2151), new Vector2(1315, 2151)),
            ("steam-valves", Sentry, new Vector2(3750, 4207), new Vector2(3250, 4207)),
            ("archive-side-cave", Skitter, new Vector2(1380, 1641), new Vector2(1600, 1641)),
            ("pendulum-gallery", Skitter, new Vector2(4300, 2361), new Vector2(4460, 2361))
        })
        {
            CanyonEncounterDefinition encounter = encounters.Single(item =>
                item.StableId.EndsWith(suffix, StringComparison.Ordinal));
            int authoredCount = suffix is "controlled-abyss" or "steam-valves" or "pendulum-gallery" ? 3 : 2;
            TestAssert.Equal(authoredCount, encounter.Enemies.Count,
                $"{suffix} retains its original pair and only its explicit new role slot");
            CanyonEnemyPlacement arrival = encounter.Enemies.Single(enemy => enemy.ActivationBatch == 1);
            TestAssert.True(arrival.ScenePath == scene && arrival.WorldPosition == primary &&
                            arrival.AlternateWorldPosition == alternate,
                $"{suffix} maps the approved type and two distinct candidate sites to one actor");
        }
        foreach ((string suffix, string scene, int batch, Vector2 primary, Vector2 alternate) in new[]
        {
            ("controlled-abyss", Scatter, 2, new Vector2(2450, 3531), new Vector2(2070, 3531)),
            ("pendulum-gallery", Laser, 2, new Vector2(4300, 2331), new Vector2(4460, 2331)),
            ("steam-valves", Missile, 2, new Vector2(3750, 4207), new Vector2(3250, 4207)),
            ("counterweight-lift", Burst, 1, new Vector2(4770, 3431), new Vector2(4630, 3431))
        })
        {
            CanyonEncounterDefinition encounter = encounters.Single(item => item.StableId.EndsWith(suffix, StringComparison.Ordinal));
            CanyonEnemyPlacement placement = encounter.Enemies.Single(enemy => enemy.ScenePath == scene);
            TestAssert.True(placement.WorldPosition == primary && placement.AlternateWorldPosition == alternate &&
                            placement.ActivationBatch == batch,
                $"{suffix} authors its exact new weapon primary/alternate/batch, rather than replacing an old actor");
        }
        TestAssert.Equal(4, encounters.SelectMany(encounter => encounter.Enemies).Count(enemy => IsNewRanged(enemy.ScenePath)),
            "four distinct weapon paths each occupy one actual saved-group catalog slot");
        Vector2 nearRestSite = new(1270, 3700);
        Rect2? nearRestBody = CanyonEncounterCatalog.ArrivalBodyBounds(Sentry, nearRestSite);
        TestAssert.True(nearRestBody is Rect2 body &&
                        !CanyonWorldLayout.EncounterRestBounds.Any(rest => rest.HasPoint(nearRestSite)) &&
                        CanyonEncounterCatalog.ArrivalBodyOverlapsRest(body),
            "catalog rest validation uses the packed Sentry body, not only its center point");
        TestAssert.Equal(CanyonRouteNodeId.BossHall, CanyonEncounterCatalog.BossRouteNode,
            "Fallen Bellkeeper remains one separate Boss contract outside the twelve normal groups");
        TestAssert.True(encounters.All(encounter => encounter.RouteNode != CanyonRouteNodeId.BossHall &&
                                                   encounter.RouteNode != CanyonRouteNodeId.BasinCamp),
            "Boss and BasinCamp are never counted as normal encounter groups");

        var expectedRoutes = new Dictionary<CanyonRouteNodeId, int>
        {
            [CanyonRouteNodeId.LowCeilingBridge] = 1,
            [CanyonRouteNodeId.StaggeredRift] = 1,
            [CanyonRouteNodeId.ControlledAbyss] = 1,
            [CanyonRouteNodeId.GearPistons] = 1,
            [CanyonRouteNodeId.CounterweightLift] = 1,
            [CanyonRouteNodeId.PendulumGallery] = 1,
            [CanyonRouteNodeId.SplitLevelEncounter] = 1,
            [CanyonRouteNodeId.SealedCourtyard] = 1,
            [CanyonRouteNodeId.ArchiveSideCave] = 1,
            [CanyonRouteNodeId.FrozenDetour] = 2,
            [CanyonRouteNodeId.SteamValves] = 1
        };
        foreach ((CanyonRouteNodeId route, int count) in expectedRoutes)
            TestAssert.Equal(count, encounters.Count(encounter => encounter.RouteNode == route),
                $"{route} owns its exact Task6 encounter count");

        foreach (CanyonEncounterDefinition encounter in encounters)
        {
            CanyonRouteNodeDefinition route = CanyonWorldLayout.Nodes.Single(node =>
                node.Id == encounter.RouteNode);
            TestAssert.True(Contains(route.WorldBounds, encounter.ActivationBounds) &&
                            Contains(route.WorldBounds, encounter.ArenaBounds),
                $"{encounter.StableId} activation and arena stay inside exact {encounter.RouteNode} bounds");
            TestAssert.True(encounter.Enemies.Count is >= 1 and <= 3,
                $"{encounter.StableId} contains one to three authored slots, not three concurrent enemies");
            TestAssert.True(encounter.Enemies.All(enemy => enemy.ScenePath is Rust or Laborer or Moth or Sentry or Skitter || IsNewRanged(enemy.ScenePath)),
                $"{encounter.StableId} uses an accepted old or real generated weapon enemy scene");
            TestAssert.True(encounter.Enemies.All(enemy => encounter.ArenaBounds.HasPoint(enemy.WorldPosition)),
                $"{encounter.StableId} enemy placements stay inside their authored arena");
            TestAssert.True(encounter.Enemies.GroupBy(enemy => enemy.ActivationBatch)
                    .All(batch => batch.Count() <= 2),
                $"{encounter.StableId} never mounts more than two enemies in one batch");
            TestAssert.True(encounter.Enemies.GroupBy(enemy => enemy.ActivationBatch)
                    .All(batch => batch.Count(enemy => IsNewRanged(enemy.ScenePath)) <= 1),
                $"{encounter.StableId} never combines two new ranged weapons in one live batch");
            int[] batches = encounter.Enemies.Select(enemy => enemy.ActivationBatch).Distinct().OrderBy(batch => batch).ToArray();
            TestAssert.True(batches.SequenceEqual(Enumerable.Range(0, batches.Length)),
                $"{encounter.StableId} keeps continuous batches from zero");
            CanyonEnemyPlacement[] oldActors = encounter.Enemies.Where(enemy => !IsNewRanged(enemy.ScenePath)).ToArray();
            if (oldActors.Length == 2)
                TestAssert.Equal(2, oldActors.Select(enemy => enemy.ActivationBatch).Distinct().Count(),
                    $"TASK6_BATCH_COLLAPSE_MUTATION {encounter.StableId} separates its two old actors into authored batches");
        }
        CanyonEncounterDefinition mixed = encounters.Single(encounter => encounter.RouteNode == CanyonRouteNodeId.CounterweightLift);
        TestAssert.True(mixed.Enemies.Count(enemy => enemy.ActivationBatch == 1) == 2 &&
                        mixed.Enemies.Any(enemy => enemy.ActivationBatch == 1 && enemy.ScenePath == Rust) &&
                        mixed.Enemies.Any(enemy => enemy.ActivationBatch == 1 && enemy.ScenePath == Burst),
            "Counterweight explicitly combines unchanged Rust and new Burst in one genuine local batch");

        CanyonEncounterDefinition first = encounters.Single(encounter =>
            encounter.RouteNode == CanyonRouteNodeId.LowCeilingBridge);
        TestAssert.True(first.Enemies.Count == 1 && first.Enemies[0].ScenePath == Rust,
            "Main1 is the first single Rust-Shell read");
        CanyonEncounterDefinition gears = encounters.Single(encounter =>
            encounter.RouteNode == CanyonRouteNodeId.GearPistons);
        TestAssert.True(gears.Enemies.Any(enemy => enemy.ScenePath == Laborer),
            "GearPistons exercises the Laborer armor/guard-break contract");
        TestAssert.True(encounters.Any(encounter => encounter.Enemies.Any(enemy => enemy.ScenePath == Moth)),
            "normal encounters include an aerial Grave-Lamp Moth read");
        TestAssert.True(encounters.Any(encounter => encounter.Enemies.Count == 2 &&
            MathF.Abs(encounter.Enemies[0].WorldPosition.Y - encounter.Enemies[1].WorldPosition.Y) >= 140f),
            "at least one normal group has a real two-level placement split");

        foreach (Rect2 rest in CanyonWorldLayout.EncounterRestBounds)
        foreach (CanyonEncounterDefinition encounter in encounters)
            TestAssert.True(!InteriorOverlaps(rest, encounter.ActivationBounds),
                $"rest/camp rectangle {rest} does not intersect {encounter.StableId} activation");

        foreach (CanyonEncounterDefinition branch in encounters.Where(encounter => encounter.Optional))
        {
            CanyonRouteNodeDefinition route = CanyonWorldLayout.Nodes.Single(node => node.Id == branch.RouteNode);
            TestAssert.True(route.Optional && !branch.GatesProgression,
                $"TASK6_BRANCH_REQUIRED_MUTATION {branch.StableId} belongs only to an optional route and cannot gate main progression");
        }

        CanyonEncounterDefinition[] frozen = encounters.Where(encounter =>
            encounter.RouteNode == CanyonRouteNodeId.FrozenDetour).ToArray();
        TestAssert.True(!InteriorOverlaps(frozen[0].ArenaBounds, frozen[1].ArenaBounds) &&
                        !InteriorOverlaps(frozen[0].ActivationBounds, frozen[1].ActivationBounds),
            "upper and lower FrozenDetour encounters have distinct nonconcurrent arenas and activation bounds");

        CanyonEncounterDefinition[] gates = encounters.Where(encounter => encounter.GatesProgression).ToArray();
        TestAssert.True(gates.Length == 1 &&
                        gates[0].StableId == "canyon.v2.encounter.split-level" &&
                        gates[0].RouteNode == CanyonRouteNodeId.SplitLevelEncounter,
            "only stable Main7 drives the existing exact encounter-plus-crank courtyard seal");
        TestAssert.True(!encounters.Single(encounter =>
                encounter.RouteNode == CanyonRouteNodeId.SealedCourtyard).GatesProgression,
            "Main8 is inside the unlocked courtyard and never becomes a third Boss prerequisite");

        for (int firstIndex = 0; firstIndex < encounters.Count; firstIndex++)
        for (int secondIndex = firstIndex + 1; secondIndex < encounters.Count; secondIndex++)
        {
            CanyonEncounterDefinition firstGroup = encounters[firstIndex];
            CanyonEncounterDefinition secondGroup = encounters[secondIndex];
            if (!InteriorOverlaps(firstGroup.ArenaBounds, secondGroup.ArenaBounds)) continue;
            int firstBatch = firstGroup.Enemies.Min(enemy => enemy.ActivationBatch);
            int secondBatch = secondGroup.Enemies.Min(enemy => enemy.ActivationBatch);
            int mounted = firstGroup.Enemies.Count(enemy => enemy.ActivationBatch == firstBatch) +
                          secondGroup.Enemies.Count(enemy => enemy.ActivationBatch == secondBatch);
            TestAssert.True(mounted <= 2,
                $"adjacent active arenas {firstGroup.StableId}/{secondGroup.StableId} expose at most two initial live actors");
        }

        IReadOnlyList<string> errors = CanyonEncounterCatalog.Validate();
        TestAssert.Equal(0, errors.Count,
            "Task6 encounter catalog validates: " + string.Join("; ", errors));
        GD.Print("TASK6_ENCOUNTER_CATALOG_OK normal=12 main=8 branch=4 boss=1");
    }

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X && inner.Position.Y >= outer.Position.Y &&
        inner.End.X <= outer.End.X && inner.End.Y <= outer.End.Y;

    private static bool IsNewRanged(string path) => path is Scatter or Burst or Missile or Laser;

    private static bool InteriorOverlaps(Rect2 first, Rect2 second) =>
        MathF.Min(first.End.X, second.End.X) > MathF.Max(first.Position.X, second.Position.X) &&
        MathF.Min(first.End.Y, second.End.Y) > MathF.Max(first.Position.Y, second.Position.Y);
}
