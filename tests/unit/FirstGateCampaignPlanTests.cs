using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue.Collections;

namespace Knight.Tests.Unit;

public static class FirstGateCampaignPlanTests
{
    public static void Run()
    {
        FirstGateCampaignPlan p = FirstGateCampaignCatalog.Candidate;
        TestAssert.Equal(24, p.Segments.Count, "first-gate candidate has24 main segments");
        TestAssert.True(p.Segments.Select(x => x.Number).SequenceEqual(Enumerable.Range(1, 24)), "ordered main segments");
        TestAssert.Equal("山洞入口", p.Segments[0].Name, "natural entrance identity");
        TestAssert.Equal("真正的大门前", p.Segments[23].Name, "level ends outside gate");
        TestAssert.True(p.Segments.Take(12).All(x => x.Region == FirstGateRegion.Abyss) &&
            p.Segments.Skip(12).Take(6).All(x => x.Region == FirstGateRegion.BuriedForecourt) &&
            p.Segments.Skip(18).All(x => x.Region == FirstGateRegion.GateWorks), "three continuous environmental bands");
        TestAssert.Equal(10, p.Branches.Count, "ten optional branches");
        TestAssert.Equal("ABCDEFGHIJ", string.Concat(p.Branches.Select(x => x.Id)), "branch identities A-J");
        TestAssert.Equal(23, p.BossFrom, "Boss approach23");
        TestAssert.Equal(24, p.BossTo, "Boss exit24");
        TestAssert.Equal(24, p.FirstLevelEnd, "gate is first level end");
        FirstGateBranch j = p.Branches.Single(x => x.Id == "J");
        TestAssert.True(j.Entry == 23 && j.Return == 23, "J returns before Boss, not after it");
        TestAssert.Equal(76, p.Migrations.Count, "all old slots retained, Boss separate");
        TestAssert.Equal(76, FirstGateCampaignCatalog.LegacyEnemyIds.Count, "actual old catalogs supply76 IDs");
        TestAssert.True(p.Migrations.Select(x => x.SourceId).OrderBy(x => x, StringComparer.Ordinal)
            .SequenceEqual(FirstGateCampaignCatalog.LegacyEnemyIds.OrderBy(x => x, StringComparer.Ordinal)), "exact old identity multiset");
        TestAssert.Equal(24, CanyonResidentCatalog.Subtypes.Count, "all old resident subtypes remain");
        TestAssert.Equal(24, p.NaturalSpawns.Count, "additional natural slots are separate");
        foreach ((string kind, int count) in new[] { ("carnivorous-flower",6), ("bat",8), ("earth-elemental",6), ("wood-elemental",4) })
            TestAssert.Equal(count, p.NaturalSpawns.Count(x => x.Kind == kind), "new natural allocation " + kind);
        TestAssert.Equal(54, p.Rewards.Count, "10 chests plus44 independent resource points");
        TestAssert.Equal(10, p.Rewards.Count(x => x.IsChest), "one primary chest per branch");
        foreach (FirstGateBranch branch in p.Branches)
        {
            TestAssert.Equal(3, p.Rewards.Count(x => x.Route == branch.Id), "one chest and2 resources " + branch.Id);
            TestAssert.True(p.Rewards.Any(x => x.StableId == branch.RewardId && x.Route == branch.Id && x.IsChest), "branch owns real reward definition " + branch.Id);
        }
        TestAssert.Equal(9, QuestFragmentCatalog.Definitions.Count, "chests do not create quest fragments");
        TestAssert.Equal(6, QuestFragmentCatalog.RequiredCount, "quota unchanged");
        TestAssert.Equal(0, FirstGateCampaignCatalog.Validate(p).Count, "authored plan passes real authoring validator");

        Reject(p with { Segments = p.Segments.Skip(1).ToArray() }, "segments", "missing main");
        Reject(p with { Segments = p.Segments.Reverse().ToArray() }, "segments", "ordered route required");
        Reject(p with { BossTo = 23 }, "boss_boundary", "wrong Boss boundary");
        Reject(p with { FirstLevelEnd = 25 }, "boss_boundary", "second level excluded");
        Reject(p with { Branches = p.Branches.Select(x => x.Id == "J" ? x with { Return = 24 } : x).ToArray() }, "branch_topology:J", "J bypass");
        Reject(p with { Branches = p.Branches.Skip(1).ToArray() }, "branches", "missing branch");
        Reject(p with { Branches = p.Branches.Select(x => x.Id == "A" ? x with { Id = "B" } : x).ToArray() }, "branches", "duplicate branch");
        Reject(p with { Migrations = p.Migrations.Skip(1).ToArray() }, "legacy_set", "omitted legacy");
        var duplicate = p.Migrations.ToArray(); duplicate[0] = duplicate[1];
        Reject(p with { Migrations = duplicate }, "legacy_set", "76 records may hide lost identity");
        Reject(p with { Migrations = p.Migrations.Select((x,i) => i == 0 ? x with { Route = "M25" } : x).ToArray() }, "migration_route", "unknown route");
        Reject(p with { NaturalSpawns = p.NaturalSpawns.Select((x,i) => i == 0 ? x with { Route = "M19" } : x).ToArray() }, "natural_front", "natural additions must populate front");
        Reject(p with { NaturalSpawns = p.NaturalSpawns.Select((x,i) => i == 0 ? x with { Kind = "renamed-old-enemy" } : x).ToArray() }, "natural_kind", "names are not new implemented kinds");
        Reject(p with { NaturalSpawns = p.NaturalSpawns.Select((x,i) => i == 0 ? x with { StableId = p.Migrations[0].SourceId } : x).ToArray() }, "content_id", "new cannot consume old ID");
        Reject(p with { Rewards = p.Rewards.Where(x => x.StableId != p.Branches[0].RewardId).ToArray() }, "branch_reward:A", "empty branch chest");
        Reject(p with { Rewards = p.Rewards.Select(x => x.StableId == p.Branches[0].RewardId ? x with { Route = "B" } : x).ToArray() }, "branch_reward:A", "other branch cannot own chest");
        Reject(p with { Rewards = p.Rewards.Select((x,i) => i == 0 ? x with { Quantity = 0 } : x).ToArray() }, "reward_quantity", "empty reward");
        Reject(p with { Rewards = p.Rewards.Select((x,i) => i == 0 ? x with { Resource = "quest-fragment" } : x).ToArray() }, "reward_resource", "separate quota");
        Reject(p with { Features = p.Features.Where(x => x.Kind != "incinerator").ToArray() }, "feature_floor:incinerator", "old capability cannot disappear");
        Reject(p with { Features = p.Features.Select(x => x.Kind == "fixed-gear" ? x with { Role = FirstGateFeatureRole.Traversal } : x).ToArray() }, "feature_role:fixed-gear", "damage vs traversal not mixed");
        Reject(p with { Features = p.Features.Select(x => x.Kind == "kill-zone" ? x with { Count = 1 } : x).ToArray() }, "feature_floor:kill-zone", "old instance floor, not just type");
        Reject(p with { Features = p.Features.Select((x,i) => i == 0 ? x with { Kind = "unknown-trap" } : x).ToArray() }, "feature_kind", "unknown mechanisms");
        Reject(null, "plan_incomplete", "null plan");
        Reject(p with { Rewards = null }, "plan_incomplete", "null collection");
        TestAssert.Equal(0, FirstGateCampaignCatalog.ValidateDensity(76, 1000, 100, 1200).Count, "measured richer density");
        TestAssert.Equal(0, FirstGateCampaignCatalog.ValidateDensity(76, 1000, 152, 2000).Count, "equal density retained");
        TestAssert.True(FirstGateCampaignCatalog.ValidateDensity(76, 1000, 100, 2000).Contains("density_lower"), "more enemies may still mean emptier map");
        foreach (double bad in new[] { 0d, -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            TestAssert.True(FirstGateCampaignCatalog.ValidateDensity(76, bad, 100, 1200).Contains("density_input"), "old length must be measured finite positive");
            TestAssert.True(FirstGateCampaignCatalog.ValidateDensity(76, 1000, 100, bad).Contains("density_input"), "new length must be measured finite positive");
        }
        TestAssert.True(FirstGateCampaignCatalog.ValidateDensity(76, 1000, 75, 800).Contains("enemy_count_lower"), "density cannot excuse reduced population");
        TestAssert.True(FirstGateCampaignCatalog.ValidateDensity(0, 1000, 100, 1200).Contains("density_input"), "zero population is not valid baseline");
        TestAssert.True(FirstGateCampaignCatalog.ValidateDensity(76, double.Epsilon, 100, 1200).Contains("density_input"), "nonfinite density despite finite input rejected");
        AssertReadOnly((IList<FirstGateSegment>)p.Segments, p.Segments[0]);
        AssertReadOnly((IList<FirstGateReward>)p.Rewards, p.Rewards[0]);
        GD.Print("FIRST_GATE_ALLOCATION_OK main=24 branches=10 legacy=76 natural=24 chests=10 resource_points=44 topology_only=true runtime_placed=false density_measured=false human=false");
    }

    private static void Reject(FirstGateCampaignPlan p, string code, string reason) =>
        TestAssert.True(FirstGateCampaignCatalog.Validate(p).Any(x => x == code || x.StartsWith(code + ":", StringComparison.Ordinal)), "reject " + reason + " as " + code);

    private static void AssertReadOnly<T>(IList<T> values, T value)
    {
        bool rejected = false;
        try { values[0] = value; } catch (NotSupportedException) { rejected = true; }
        TestAssert.True(rejected, "catalog must not expose mutable arrays");
    }
}
