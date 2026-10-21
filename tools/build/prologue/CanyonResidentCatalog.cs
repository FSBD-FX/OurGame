using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Godot;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

// Allocation is independent of scene readiness and native placement proof.
public static class CanyonResidentCatalog
{
    public static IReadOnlyList<ResidentSubtypeDefinition> Subtypes { get; } = Array.AsReadOnly(new[]
    {
        Type("rock-wanderer", "碎岩游兽", ResidentFamily.Native, ResidentArchetype.Wanderer, CanyonEncounterCatalog.RustShellScene),
        Type("ridge-charger", "岩脊冲角兽", ResidentFamily.Native, ResidentArchetype.Charger, CanyonEncounterCatalog.SkitterScene),
        Type("rift-flyer", "裂谷浮翼", ResidentFamily.Native, ResidentArchetype.Flyer, CanyonEncounterCatalog.MothScene),
        Type("cliff-diver", "崖栖钩翼", ResidentFamily.Native, ResidentArchetype.Diver, CanyonEncounterCatalog.MothScene),
        Type("scrap-wanderer", "断臂拾荒械", ResidentFamily.Scrap, ResidentArchetype.Wanderer, CanyonEncounterCatalog.RustShellScene),
        Type("scrap-charger", "废轨撞击者", ResidentFamily.Scrap, ResidentArchetype.Charger, CanyonEncounterCatalog.SkitterScene),
        Type("scrap-flyer", "破叶巡游机", ResidentFamily.Scrap, ResidentArchetype.Flyer, CanyonEncounterCatalog.MothScene),
        Type("scrap-diver", "回收钩翼", ResidentFamily.Scrap, ResidentArchetype.Diver, CanyonEncounterCatalog.MothScene),
        Type("scrap-scatter", "铆接散射座", ResidentFamily.Scrap, ResidentArchetype.Turret, CanyonEncounterCatalog.ScatterScene),
        Type("scrap-heavy", "熔核重装拾荒者", ResidentFamily.Scrap, ResidentArchetype.Heavy, CanyonEncounterCatalog.LaborerScene),
        Type("precision-burst", "制式清除枪座", ResidentFamily.Precision, ResidentArchetype.Turret, CanyonEncounterCatalog.BurstScene),
        Type("precision-laser", "棱镜哨台", ResidentFamily.Precision, ResidentArchetype.Turret, CanyonEncounterCatalog.LaserScene),
        Type("precision-missile", "微型导弹阵列", ResidentFamily.Precision, ResidentArchetype.Turret, CanyonEncounterCatalog.MissileScene),
        Type("precision-flyer", "巡航监工", ResidentFamily.Precision, ResidentArchetype.Flyer, CanyonEncounterCatalog.MothScene),
        Type("precision-charger", "相位冲压卫", ResidentFamily.Precision, ResidentArchetype.Charger, CanyonEncounterCatalog.SkitterScene),
        Type("precision-heavy", "装甲清理员", ResidentFamily.Precision, ResidentArchetype.Heavy, CanyonEncounterCatalog.LaborerScene),
        Type("bone-wanderer", "废骨缝械", ResidentFamily.Bone, ResidentArchetype.Wanderer, CanyonEncounterCatalog.RustShellScene),
        Type("bone-charger", "骨轮突进者", ResidentFamily.Bone, ResidentArchetype.Charger, CanyonEncounterCatalog.SkitterScene),
        Type("bone-diver", "精制骨翼", ResidentFamily.Bone, ResidentArchetype.Diver, CanyonEncounterCatalog.MothScene),
        Type("bone-heavy", "骨架守卫", ResidentFamily.Bone, ResidentArchetype.Heavy, CanyonEncounterCatalog.LaborerScene),
        Type("lost-echo", "失序残响", ResidentFamily.Ghost, ResidentArchetype.Wanderer, CanyonEncounterCatalog.RustShellScene),
        Type("bound-ghost", "被缚游魂", ResidentFamily.Ghost, ResidentArchetype.Flyer, CanyonEncounterCatalog.MothScene),
        Type("ghost-diver", "断片俯灵", ResidentFamily.Ghost, ResidentArchetype.Diver, CanyonEncounterCatalog.MothScene),
        Type("ghost-tower", "灵能拘束塔", ResidentFamily.Ghost, ResidentArchetype.Turret, CanyonEncounterCatalog.SentryScene)
    });

    private static readonly IReadOnlyDictionary<string, ResidentSubtypeDefinition> Types =
        new ReadOnlyDictionary<string, ResidentSubtypeDefinition>(Subtypes.ToDictionary(x => x.Id, StringComparer.Ordinal));

    private static readonly IReadOnlyDictionary<CanyonRouteNodeId, int> RegionCounts =
        new ReadOnlyDictionary<CanyonRouteNodeId, int>(new Dictionary<CanyonRouteNodeId, int>
        {
            [CanyonRouteNodeId.LowCeilingBridge] = 3, [CanyonRouteNodeId.StaggeredRift] = 5,
            [CanyonRouteNodeId.ControlledAbyss] = 5, [CanyonRouteNodeId.ArchiveSideCave] = 5,
            [CanyonRouteNodeId.FrozenDetour] = 6, [CanyonRouteNodeId.BasinCamp] = 0,
            [CanyonRouteNodeId.GearPistons] = 7, [CanyonRouteNodeId.SteamValves] = 6,
            [CanyonRouteNodeId.CounterweightLift] = 5, [CanyonRouteNodeId.PendulumGallery] = 4,
            [CanyonRouteNodeId.SplitLevelEncounter] = 3, [CanyonRouteNodeId.SealedCourtyard] = 1,
            [CanyonRouteNodeId.BossHall] = 0, [CanyonRouteNodeId.SafeExit] = 0
        });

    public static IReadOnlyList<ResidentPopulationDefinition> All { get; } = AuthorAllocation();
    private static readonly IReadOnlyDictionary<string, ResidentPopulationDefinition> IdentityBindings =
        new ReadOnlyDictionary<string, ResidentPopulationDefinition>(All.ToDictionary(x => x.StableId, StringComparer.Ordinal));

    public static IReadOnlyList<string> ValidateAllocation(IReadOnlyList<ResidentPopulationDefinition> entries = null)
    {
        entries ??= All;
        var errors = new List<string>();
        if (entries.Count != 50) errors.Add($"population_count:{entries.Count}");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < entries.Count; i++)
        {
            ResidentPopulationDefinition entry = entries[i];
            if (entry == null) { errors.Add($"null_entry:{i}"); continue; }
            if (string.IsNullOrWhiteSpace(entry.StableId) || !entry.StableId.StartsWith("canyon.v2.resident.", StringComparison.Ordinal))
                errors.Add($"stable_id:{i}");
            else
            {
                if (!ids.Add(entry.StableId)) errors.Add($"duplicate_id:{entry.StableId}");
                if (!IdentityBindings.TryGetValue(entry.StableId, out ResidentPopulationDefinition binding))
                    errors.Add($"unknown_id:{entry.StableId}");
                else if (binding.SubtypeId != entry.SubtypeId || binding.RouteNode != entry.RouteNode ||
                    binding.Sector != entry.Sector || binding.Family != entry.Family || binding.Archetype != entry.Archetype)
                    errors.Add($"identity_binding:{entry.StableId}");
            }
            CanyonRouteNodeDefinition node = CanyonWorldLayout.Nodes.SingleOrDefault(x => x.Id == entry.RouteNode);
            if (node == null) errors.Add($"unknown_route:{entry.StableId}");
            else
            {
                if (node.SectorId != entry.Sector) errors.Add($"route_sector:{entry.StableId}");
                if (node.RestArea || node.Id == CanyonRouteNodeId.BossHall || node.Id == CanyonRouteNodeId.BasinCamp)
                    errors.Add($"protected_route:{entry.StableId}");
            }
            if (entry.SubtypeId == null || !Types.TryGetValue(entry.SubtypeId, out ResidentSubtypeDefinition subtype))
                errors.Add($"unknown_subtype:{entry.StableId}");
            else
            {
                if (entry.Family != subtype.Family) errors.Add($"subtype_family:{entry.StableId}");
                if (entry.Archetype != subtype.Archetype) errors.Add($"subtype_archetype:{entry.StableId}");
            }
        }
        foreach (var expected in RegionCounts)
        {
            int actual = entries.Count(x => x != null && x.RouteNode == expected.Key);
            if (actual != expected.Value) errors.Add($"route_count:{expected.Key}:expected={expected.Value}:actual={actual}");
        }
        foreach ((ResidentFamily family, int count) in new[]
        {
            (ResidentFamily.Native, 8), (ResidentFamily.Scrap, 12), (ResidentFamily.Precision, 10),
            (ResidentFamily.Bone, 10), (ResidentFamily.Ghost, 10)
        })
            if (entries.Count(x => x != null && x.Family == family) != count) errors.Add($"family_count:{family}");
        foreach ((ResidentArchetype role, int count) in new[]
        {
            (ResidentArchetype.Wanderer, 16), (ResidentArchetype.Charger, 7), (ResidentArchetype.Flyer, 8),
            (ResidentArchetype.Diver, 6), (ResidentArchetype.Turret, 9), (ResidentArchetype.Heavy, 4)
        })
            if (entries.Count(x => x != null && x.Archetype == role) != count) errors.Add($"archetype_count:{role}");
        return errors.AsReadOnly();
    }

    // This is structural candidate validation ONLY. Even a valid rectangle and
    // patrol route remain unready until native complete-body/hazard/landing proof.
    public static IReadOnlyList<string> ValidatePlacementCandidates(IReadOnlyList<ResidentPopulationDefinition> entries = null)
    {
        entries ??= All;
        var errors = new List<string>(ValidateAllocation(entries));
        foreach (ResidentPopulationDefinition entry in entries.Where(x => x != null))
        {
            if (entry.Placement == null) { errors.Add($"placement.not_yet_placed:{entry.StableId}"); continue; }
            ResidentPlacementCandidate p = entry.Placement;
            int before = errors.Count;
            bool finite = Finite(p.WorldSpawn) && Finite(p.Territory.Position) && Finite(p.Territory.Size) && p.PatrolPoints.All(Finite);
            if (!finite) errors.Add($"placement.non_finite:{entry.StableId}");
            if (p.Territory.Size.X <= 0 || p.Territory.Size.Y <= 0) errors.Add($"placement.invalid_territory:{entry.StableId}");
            if (finite && !p.Territory.HasPoint(p.WorldSpawn)) errors.Add($"placement.spawn_outside_territory:{entry.StableId}");
            if (finite && p.PatrolPoints.Any(x => !p.Territory.HasPoint(x))) errors.Add($"placement.patrol_outside_territory:{entry.StableId}");
            int minimum = entry.Archetype == ResidentArchetype.Turret ? 1 : 2;
            if (p.PatrolPoints.Count < minimum) errors.Add($"placement.patrol_count:{entry.StableId}");
            CanyonRouteNodeDefinition node = CanyonWorldLayout.Nodes.SingleOrDefault(x => x.Id == entry.RouteNode);
            // Conservative primary route bounds only. Upper-gallery extensions
            // require their exact saved support corridor in the later native gate.
            if (finite && node != null && !node.WorldBounds.Encloses(p.Territory))
                errors.Add($"placement.territory_outside_route:{entry.StableId}");
            if (errors.Count == before) errors.Add($"placement.native_proof_required:{entry.StableId}");
        }
        return errors.AsReadOnly();
    }

    public static IReadOnlyList<string> ValidateSceneReadiness(IReadOnlyList<ResidentPopulationDefinition> entries = null)
    {
        entries ??= All;
        var errors = new List<string>(ValidateAllocation(entries));
        foreach (ResidentPopulationDefinition entry in entries.Where(x => x != null))
        {
            if (entry.SubtypeId == null || !Types.TryGetValue(entry.SubtypeId, out ResidentSubtypeDefinition subtype)) continue;
            string code = subtype.SceneState switch
            {
                ResidentSceneState.Unimplemented => "unimplemented",
                ResidentSceneState.ExplicitPlaceholder => "placeholder",
                _ => "native_behavior_proof_required"
            };
            errors.Add($"scene.{code}:{entry.StableId}");
        }
        return errors.AsReadOnly();
    }

    private static bool Finite(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y);

    private static ResidentSubtypeDefinition Type(string id, string name, ResidentFamily family,
        ResidentArchetype role, string probe) => new(id, name, family, role, probe, Carrier: probe switch
        {
            CanyonEncounterCatalog.RustShellScene => ResidentCarrierKind.Wanderer,
            CanyonEncounterCatalog.SkitterScene => ResidentCarrierKind.Charger,
            CanyonEncounterCatalog.MothScene => ResidentCarrierKind.Air,
            CanyonEncounterCatalog.LaborerScene => ResidentCarrierKind.Heavy,
            CanyonEncounterCatalog.SentryScene => ResidentCarrierKind.Sentry,
            CanyonEncounterCatalog.ScatterScene => ResidentCarrierKind.Scatter,
            CanyonEncounterCatalog.BurstScene => ResidentCarrierKind.Burst,
            CanyonEncounterCatalog.MissileScene => ResidentCarrierKind.Missile,
            CanyonEncounterCatalog.LaserScene => ResidentCarrierKind.Laser,
            _ => throw new ArgumentException("Resident carrier must be explicitly supported.", nameof(probe))
        });

    private static IReadOnlyList<ResidentPopulationDefinition> AuthorAllocation()
    {
        var entries = new List<ResidentPopulationDefinition>();
        Add(CanyonRouteNodeId.LowCeilingBridge, "low-ceiling-bridge", "rock-wanderer", "ridge-charger", "scrap-scatter");
        Add(CanyonRouteNodeId.StaggeredRift, "staggered-rift", "rock-wanderer", "rock-wanderer", "ridge-charger", "rift-flyer", "precision-burst");
        Add(CanyonRouteNodeId.ControlledAbyss, "controlled-abyss", "lost-echo", "rift-flyer", "cliff-diver", "scrap-diver", "precision-missile");
        Add(CanyonRouteNodeId.ArchiveSideCave, "archive-side-cave", "bone-wanderer", "bone-wanderer", "bone-charger", "bone-diver", "ghost-tower");
        Add(CanyonRouteNodeId.FrozenDetour, "frozen-detour", "lost-echo", "lost-echo", "bone-charger", "bound-ghost", "bound-ghost", "ghost-diver");
        Add(CanyonRouteNodeId.GearPistons, "gear-pistons", "scrap-wanderer", "scrap-wanderer", "scrap-charger", "scrap-flyer", "scrap-scatter", "scrap-heavy", "precision-heavy");
        Add(CanyonRouteNodeId.SteamValves, "steam-valves", "scrap-wanderer", "scrap-wanderer", "scrap-charger", "scrap-flyer", "precision-missile", "bone-heavy");
        Add(CanyonRouteNodeId.CounterweightLift, "counterweight-lift", "bone-wanderer", "precision-flyer", "ghost-diver", "precision-burst", "precision-heavy");
        Add(CanyonRouteNodeId.PendulumGallery, "pendulum-gallery", "bone-wanderer", "precision-charger", "bone-diver", "precision-laser");
        Add(CanyonRouteNodeId.SplitLevelEncounter, "split-level", "bone-wanderer", "lost-echo", "bound-ghost");
        Add(CanyonRouteNodeId.SealedCourtyard, "sealed-courtyard", "precision-laser");
        return entries.AsReadOnly();

        void Add(CanyonRouteNodeId route, string region, params string[] subtypes)
        {
            PrologueSectorId sector = CanyonWorldLayout.Nodes.Single(x => x.Id == route).SectorId;
            for (int i = 0; i < subtypes.Length; i++)
            {
                ResidentSubtypeDefinition type = Types[subtypes[i]];
                entries.Add(new ResidentPopulationDefinition($"canyon.v2.resident.{region}.{i + 1:00}",
                    type.Id, route, sector, type.Family, type.Archetype));
            }
        }
    }
}
