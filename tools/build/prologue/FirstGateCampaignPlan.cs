using System;
using System.Collections.Generic;
using System.Linq;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;

namespace Knight.Build.Prologue;

public enum FirstGateRegion { Abyss, BuriedForecourt, GateWorks }
public enum FirstGateFeatureRole { Damage, TimedSolid, Recovery, Traversal, Control, Material }
public sealed record FirstGateSegment(int Number, string Name, FirstGateRegion Region);
public sealed record FirstGateBranch(string Id, int Entry, int Return, string RewardId);
public sealed record FirstGateMigration(string SourceId, string Route);
public sealed record FirstGateNaturalSpawn(string StableId, string Kind, string Route);
public sealed record FirstGateReward(string StableId, string Route, string Resource, int Quantity, bool IsChest);
public sealed record FirstGateFeature(string Kind, FirstGateFeatureRole Role, string Route, int Count);
public sealed record FirstGateCampaignPlan(
    IReadOnlyList<FirstGateSegment> Segments, IReadOnlyList<FirstGateBranch> Branches,
    IReadOnlyList<FirstGateMigration> Migrations, IReadOnlyList<FirstGateNaturalSpawn> NaturalSpawns,
    IReadOnlyList<FirstGateReward> Rewards, IReadOnlyList<FirstGateFeature> Features,
    int BossFrom = 23, int BossTo = 24, int FirstLevelEnd = 24);

// Authoring input only: no world coordinates, actor instances, live map or save mutations.
public static class FirstGateCampaignCatalog
{
    private sealed record FeatureRule(string Kind, FirstGateFeatureRole Role, int Minimum);
    // Baseline: five saved sectors, node->script bindings (not visual scripts or ext_resource declarations).
    private static readonly FeatureRule[] FeatureRules =
    {
        new("fixed-gear", FirstGateFeatureRole.Damage, 2),
        new("moving-gear", FirstGateFeatureRole.Damage, 1),
        new("arc-gear", FirstGateFeatureRole.Damage, 1),
        new("steam-jet", FirstGateFeatureRole.Damage, 5),
        new("incinerator", FirstGateFeatureRole.Damage, 1),
        new("shield-lane", FirstGateFeatureRole.Damage, 1),
        new("gear-piston", FirstGateFeatureRole.TimedSolid, 1),
        new("pendulum", FirstGateFeatureRole.TimedSolid, 1),
        new("kill-zone", FirstGateFeatureRole.Recovery, 6),
        new("springboard", FirstGateFeatureRole.Traversal, 2),
        new("updraft", FirstGateFeatureRole.Traversal, 1),
        new("counterweight-lift", FirstGateFeatureRole.Traversal, 2),
        new("pressure-gate", FirstGateFeatureRole.Control, 2),
        new("steam-diverter", FirstGateFeatureRole.Control, 2),
        new("steam-valve", FirstGateFeatureRole.Control, 2),
        new("shortcut-winch", FirstGateFeatureRole.Control, 1),
        new("seal-gate", FirstGateFeatureRole.Control, 1),
        new("bell-chain", FirstGateFeatureRole.Control, 1),
        new("claw-mechanism", FirstGateFeatureRole.Control, 2),
        new("ice-no-grip", FirstGateFeatureRole.Material, 1),
        new("falling-rock", FirstGateFeatureRole.Damage, 1),
        new("climbing-vine", FirstGateFeatureRole.Traversal, 1),
        new("thorn-vine", FirstGateFeatureRole.Damage, 1),
        new("exposed-wire", FirstGateFeatureRole.Damage, 1),
        new("broken-device", FirstGateFeatureRole.TimedSolid, 1)
    };
    private static readonly (int Entry, int Return)[] BranchLinks =
        { (2,3), (4,6), (6,7), (7,9), (9,10), (11,12), (14,15), (15,17), (20,22), (23,23) };
    private static readonly string[] NaturalKinds = { "carnivorous-flower", "bat", "earth-elemental", "wood-elemental" };
    private static readonly int[] NaturalMinimum = { 6, 8, 6, 4 };
    private static readonly string[] Resources = { "salvage", "data", "echo-memory", "mineral", "fiber" };

    public static IReadOnlyList<string> LegacyEnemyIds { get; } = Array.AsReadOnly(
        CanyonEncounterCatalog.Normal.SelectMany(e => e.Enemies.Select((_, i) => WaveSlotId(e.StableId, i)))
            .Concat(CanyonResidentCatalog.All.Select(r => r.StableId)).ToArray());
    public static FirstGateCampaignPlan Candidate { get; } = Author();

    public static IReadOnlyList<string> Validate(FirstGateCampaignPlan plan)
    {
        var errors = new List<string>();
        if (plan == null || plan.Segments == null || plan.Branches == null || plan.Migrations == null ||
            plan.NaturalSpawns == null || plan.Rewards == null || plan.Features == null)
            return Array.AsReadOnly(new[] { "plan_incomplete" });
        if (plan.Segments.Any(x => x == null) || plan.Branches.Any(x => x == null) || plan.Migrations.Any(x => x == null) ||
            plan.NaturalSpawns.Any(x => x == null) || plan.Rewards.Any(x => x == null) || plan.Features.Any(x => x == null))
            return Array.AsReadOnly(new[] { "plan_incomplete:null_row" });

        if (!plan.Segments.Select(x => x.Number).SequenceEqual(Enumerable.Range(1, 24)) ||
            plan.Segments.Any(x => string.IsNullOrWhiteSpace(x.Name) || x.Region != Region(x.Number)))
            errors.Add("segments");
        if (!plan.Branches.Select(x => x.Id).SequenceEqual(Enumerable.Range(0, 10).Select(i => ((char)('A' + i)).ToString())))
            errors.Add("branches");
        foreach (FirstGateBranch branch in plan.Branches)
        {
            int index = BranchIndex(branch.Id);
            if (index < 0 || (branch.Entry, branch.Return) != BranchLinks[index]) errors.Add("branch_topology:" + branch.Id);
            if (!plan.Rewards.Any(x => x.StableId == branch.RewardId && x.Route == branch.Id && x.IsChest && x.Quantity > 0))
                errors.Add("branch_reward:" + branch.Id);
            if (plan.Rewards.Count(x => x.Route == branch.Id) != 3 || plan.Rewards.Count(x => x.Route == branch.Id && x.IsChest) != 1)
                errors.Add("branch_resources:" + branch.Id);
        }
        if (plan.BossFrom != 23 || plan.BossTo != 24 || plan.FirstLevelEnd != 24) errors.Add("boss_boundary");
        if (!plan.Migrations.Select(x => x.SourceId).OrderBy(x => x, StringComparer.Ordinal)
            .SequenceEqual(LegacyEnemyIds.OrderBy(x => x, StringComparer.Ordinal))) errors.Add("legacy_set");
        foreach (FirstGateMigration row in plan.Migrations)
            if (!KnownRoute(row.Route) || row.Route is "M01" or "M24") errors.Add("migration_route:" + row.SourceId);
        var ids = new HashSet<string>(LegacyEnemyIds, StringComparer.Ordinal);
        foreach (FirstGateNaturalSpawn row in plan.NaturalSpawns)
        {
            CheckId(row.StableId);
            if (!NaturalKinds.Contains(row.Kind, StringComparer.Ordinal)) errors.Add("natural_kind:" + row.StableId);
            if (!NaturalFront(row.Route)) errors.Add("natural_front:" + row.StableId);
        }
        for (int i = 0; i < NaturalKinds.Length; i++)
            if (plan.NaturalSpawns.Count(x => x.Kind == NaturalKinds[i]) < NaturalMinimum[i]) errors.Add("natural_floor:" + NaturalKinds[i]);
        if (plan.Rewards.Count != 54) errors.Add("reward_count");
        foreach (FirstGateReward row in plan.Rewards)
        {
            CheckId(row.StableId);
            if (!KnownRoute(row.Route)) errors.Add("reward_route:" + row.StableId);
            if (!Resources.Contains(row.Resource, StringComparer.Ordinal)) errors.Add("reward_resource:" + row.StableId);
            if (row.Quantity <= 0) errors.Add("reward_quantity:" + row.StableId);
            if (row.IsChest && BranchIndex(row.Route) < 0) errors.Add("reward_chest_route:" + row.StableId);
        }
        foreach (int n in Enumerable.Range(1, 24))
            if (plan.Rewards.Count(x => x.Route == Main(n) && !x.IsChest) != 1) errors.Add("main_resource:" + Main(n));
        foreach (FirstGateFeature row in plan.Features)
        {
            FeatureRule rule = FeatureRules.SingleOrDefault(x => x.Kind == row.Kind);
            if (rule == null) errors.Add("feature_kind:" + row.Kind);
            else if (row.Role != rule.Role) errors.Add("feature_role:" + row.Kind);
            if (row.Count <= 0) errors.Add("feature_count:" + row.Kind);
            if (!KnownRoute(row.Route)) errors.Add("feature_route:" + row.Kind);
        }
        foreach (FeatureRule rule in FeatureRules)
            if (plan.Features.Where(x => x.Kind == rule.Kind).Sum(x => (long)x.Count) < rule.Minimum)
                errors.Add("feature_floor:" + rule.Kind);
        return errors.AsReadOnly();

        void CheckId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) errors.Add("content_id:" + id);
        }
    }

    // Caller must supply measured traversable combat-route lengths, not image width or open bounding-box area.
    // This arithmetic guard cannot prove where a length came from or certify safety, difficulty or performance.
    public static IReadOnlyList<string> ValidateDensity(int oldCount, double oldLength, int newCount, double newLength)
    {
        var errors = new List<string>();
        if (oldCount <= 0 || newCount <= 0 || !double.IsFinite(oldLength) || !double.IsFinite(newLength) ||
            oldLength <= 0 || newLength <= 0 || !double.IsFinite(oldCount / oldLength) || !double.IsFinite(newCount / newLength))
            return Array.AsReadOnly(new[] { "density_input" });
        if (newCount < oldCount) errors.Add("enemy_count_lower");
        if (newCount / newLength < oldCount / oldLength) errors.Add("density_lower");
        return errors.AsReadOnly();
    }

    private static FirstGateCampaignPlan Author()
    {
        string[] names = { "山洞入口", "裂口断壁", "上层侧洞", "跨隙岩梁", "双层回廊", "悬峡腰线", "内壁长降", "回折夹层",
            "地下长廊", "双井裂谷", "深渊石林", "谷底右行口", "深渊侧口", "山腹横洞", "埋藏前庭", "石柱回廊", "前庭侧廊", "走廊入口",
            "接待前厅", "高位环廊", "机械竖井", "机座底廊", "门前甬道与战斗前场", "真正的大门前" };
        var segments = names.Select((name,i) => new FirstGateSegment(i+1, name, Region(i+1))).ToArray();
        var branches = BranchLinks.Select((link,i) => new FirstGateBranch(((char)('A'+i)).ToString(), link.Entry, link.Return,
            $"knight.first-gate.branch.{(char)('a'+i)}.chest")).ToArray();
        var migrations = new List<FirstGateMigration>();
        foreach (CanyonEncounterDefinition encounter in CanyonEncounterCatalog.Normal)
        {
            string route = encounter.StableId.EndsWith("frozen-upper", StringComparison.Ordinal) ? "D" :
                encounter.StableId.EndsWith("frozen-lower", StringComparison.Ordinal) ? "E" : LegacyTargets(encounter.RouteNode)[0];
            for (int i = 0; i < encounter.Enemies.Count; i++) migrations.Add(new(WaveSlotId(encounter.StableId,i), route));
        }
        foreach (var group in CanyonResidentCatalog.All.GroupBy(x => x.RouteNode))
        {
            string[] targets = LegacyTargets(group.Key);
            int i = 0;
            foreach (var resident in group) migrations.Add(new(resident.StableId, targets[i++ % targets.Length]));
        }
        var natural = new List<FirstGateNaturalSpawn>();
        AddNatural("carnivorous-flower", "M02", "M04", "M05", "A", "C", "D");
        AddNatural("bat", "M03", "M05", "M07", "M08", "M10", "B", "C", "E");
        AddNatural("earth-elemental", "M06", "M08", "M09", "M11", "D", "F");
        AddNatural("wood-elemental", "M03", "M06", "A", "F");
        var rewards = ResourceRewardCatalog.All.Select(x=>new FirstGateReward(x.StableId,x.Route,x.Resource,x.Quantity,x.IsChest)).ToList();
        var features = new List<FirstGateFeature>();
        AddFeature("fixed-gear", "M19",4); AddFeature("moving-gear","M16",3); AddFeature("arc-gear","M20",3);
        AddFeature("steam-jet","H",8); AddFeature("incinerator","M22",3); AddFeature("shield-lane","M19",3);
        AddFeature("gear-piston","M16",3); AddFeature("pendulum","M21",3); AddFeature("kill-zone","M07",6);
        AddFeature("springboard","M10",4); AddFeature("updraft","M07",2); AddFeature("counterweight-lift","M20",3);
        AddFeature("pressure-gate","F",3); AddFeature("steam-diverter","H",3); AddFeature("steam-valve","M17",3);
        AddFeature("shortcut-winch","M18",2); AddFeature("seal-gate","M23",1); AddFeature("bell-chain","M23",1);
        AddFeature("claw-mechanism","M21",2); AddFeature("ice-no-grip","D",2);
        AddFeature("falling-rock","M02",5); AddFeature("climbing-vine","M03",4); AddFeature("thorn-vine","M05",4);
        AddFeature("exposed-wire","M15",4); AddFeature("broken-device","M14",3);
        return new(Array.AsReadOnly(segments),Array.AsReadOnly(branches),migrations.AsReadOnly(),natural.AsReadOnly(),rewards.AsReadOnly(),features.AsReadOnly());

        void AddNatural(string kind, params string[] routes)
        {
            for (int i = 0; i < routes.Length; i++) natural.Add(new($"knight.first-gate.natural.{kind}.{i+1:00}",kind,routes[i]));
        }
        void AddFeature(string kind,string route,int count) => features.Add(new(kind,FeatureRules.Single(x=>x.Kind==kind).Role,route,count));
    }

    private static string[] LegacyTargets(CanyonRouteNodeId route) => route switch
    {
        CanyonRouteNodeId.LowCeilingBridge => new[] { "M03", "M02", "A" },
        CanyonRouteNodeId.StaggeredRift => new[] { "M04", "M05", "B", "C" },
        CanyonRouteNodeId.ControlledAbyss => new[] { "M12", "M07", "M09", "M11" },
        CanyonRouteNodeId.ArchiveSideCave => new[] { "F" },
        CanyonRouteNodeId.FrozenDetour => new[] { "D", "E" },
        CanyonRouteNodeId.GearPistons => new[] { "M16", "M15", "M17" },
        CanyonRouteNodeId.SteamValves => new[] { "H", "G", "M14" },
        CanyonRouteNodeId.CounterweightLift => new[] { "M20", "M18", "I" },
        CanyonRouteNodeId.PendulumGallery => new[] { "M21", "M22" },
        CanyonRouteNodeId.SplitLevelEncounter => new[] { "M19", "J" },
        CanyonRouteNodeId.SealedCourtyard => new[] { "M23" },
        _ => throw new InvalidOperationException("Unmapped legacy route: " + route)
    };
    private static string WaveSlotId(string encounter,int index) => $"{encounter}.slot.{index+1:00}";
    private static string Main(int n) => $"M{n:00}";
    private static FirstGateRegion Region(int n) => n<=12?FirstGateRegion.Abyss:n<=18?FirstGateRegion.BuriedForecourt:FirstGateRegion.GateWorks;
    private static int BranchIndex(string id) => id is { Length:1 } && id[0]>='A' && id[0]<='J' ? id[0]-'A' : -1;
    private static bool KnownRoute(string route) => BranchIndex(route)>=0 || Enumerable.Range(1,24).Any(n=>route==Main(n));
    private static bool NaturalFront(string route) => BranchIndex(route) is >=0 and <6 || Enumerable.Range(2,11).Any(n=>route==Main(n));
}
