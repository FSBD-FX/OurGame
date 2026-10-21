using System;
using System.Collections.Generic;
using System.Linq;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class QuestFragmentProgressTests
{
    private static readonly QuestFragmentDefinition[] ExpectedDefinitions =
    [
        new("canyon.v2.fragment.entrance", CanyonRouteNodeId.LowCeilingBridge, false),
        new("canyon.v2.fragment.rift", CanyonRouteNodeId.StaggeredRift, false),
        new("canyon.v2.fragment.abyss-floor", CanyonRouteNodeId.ControlledAbyss, false),
        new("canyon.v2.fragment.gear-gallery", CanyonRouteNodeId.GearPistons, false),
        new("canyon.v2.fragment.ascent-mid", CanyonRouteNodeId.CounterweightLift, false),
        new("canyon.v2.fragment.courtyard", CanyonRouteNodeId.SealedCourtyard, false),
        new("canyon.v2.fragment.archive", CanyonRouteNodeId.ArchiveSideCave, true),
        new("canyon.v2.fragment.steam-loop", CanyonRouteNodeId.SteamValves, true),
        new("canyon.v2.fragment.temple-upper", CanyonRouteNodeId.SealedCourtyard, true)
    ];

    public static void Run()
    {
        CatalogContainsExactlyTheApprovedFragments();
        CatalogSeparatesMainRouteFromOptionalFragments();
        CatalogRejectsDiscoverySupplyAndUnknownIds();
        CountUsesDistinctCatalogIntersection();
        CountRetainsAllNineWithoutConsumingInput();
        QuotaRequiresSixDistinctKnownFragments();
        OptionalFragmentsCanReplaceMissedMainRouteFragments();
        LegacyExemptionDoesNotFabricateCollectedFragments();
    }

    private static void CatalogContainsExactlyTheApprovedFragments()
    {
        TestAssert.Equal(9, QuestFragmentCatalog.Definitions.Count,
            "fragment catalog contains all nine approved definitions");
        TestAssert.Equal(9, QuestFragmentCatalog.StableIds.Count,
            "fragment ID registry contains exactly nine entries");
        TestAssert.Equal(9, QuestFragmentCatalog.StableIds.Distinct(StringComparer.Ordinal).Count(),
            "fragment registry has no duplicate permanent IDs");
        foreach (QuestFragmentDefinition expected in ExpectedDefinitions)
        {
            QuestFragmentDefinition actual = QuestFragmentCatalog.Definitions.SingleOrDefault(
                value => string.Equals(value.StableId, expected.StableId, StringComparison.Ordinal));
            TestAssert.True(actual != null, $"catalog registers {expected.StableId}");
            TestAssert.Equal(expected.Region, actual.Region,
                $"{expected.StableId} retains its approved route region");
            TestAssert.Equal(expected.Optional, actual.Optional,
                $"{expected.StableId} retains its approved route category");
            TestAssert.True(QuestFragmentCatalog.StableIds.Contains(expected.StableId),
                $"ID registry includes {expected.StableId}");
            TestAssert.True(QuestFragmentCatalog.IsKnown(expected.StableId),
                $"catalog recognizes {expected.StableId} for save validation");
        }
    }

    private static void CatalogSeparatesMainRouteFromOptionalFragments()
    {
        TestAssert.Equal(6, QuestFragmentCatalog.Definitions.Count(value => !value.Optional),
            "main route offers six fragments");
        TestAssert.Equal(3, QuestFragmentCatalog.Definitions.Count(value => value.Optional),
            "optional routes offer three replacement fragments");
        QuestFragmentDefinition courtyard = QuestFragmentCatalog.Definitions.Single(
            value => value.StableId == "canyon.v2.fragment.courtyard");
        QuestFragmentDefinition upperTemple = QuestFragmentCatalog.Definitions.Single(
            value => value.StableId == "canyon.v2.fragment.temple-upper");
        TestAssert.True(!courtyard.Optional && upperTemple.Optional,
            "a shared SealedCourtyard region does not erase the main/optional distinction");
    }

    private static void CatalogRejectsDiscoverySupplyAndUnknownIds()
    {
        foreach (string id in new[]
        {
            "canyon.v2.archive-side-cave", "canyon.v2.archive-supply", "supply_cave",
            "canyon.v2.fragment.unregistered", "CANYON.V2.FRAGMENT.ENTRANCE", "", " ", null
        })
            TestAssert.True(!QuestFragmentCatalog.IsKnown(id),
                $"non-fragment or non-exact ID is not in the fragment registry: {id ?? "null"}");
    }

    private static void CountUsesDistinctCatalogIntersection()
    {
        string[] ids =
        [
            "canyon.v2.fragment.entrance", "canyon.v2.fragment.entrance",
            "canyon.v2.archive-side-cave", "canyon.v2.archive-supply", "supply_cave",
            "canyon.v2.fragment.unregistered", "CANYON.V2.FRAGMENT.ENTRANCE", "", null
        ];
        TestAssert.Equal(1, QuestFragmentProgressModel.CountCollected(ids),
            "only distinct registered fragment IDs increase the count");
        TestAssert.Equal(0, QuestFragmentProgressModel.CountCollected(Array.Empty<string>()),
            "an empty collection has zero fragments");
    }

    private static void CountRetainsAllNineWithoutConsumingInput()
    {
        var ids = new List<string>(ExpectedDefinitions.Select(value => value.StableId));
        string[] before = ids.ToArray();
        TestAssert.Equal(9, QuestFragmentProgressModel.CountCollected(ids),
            "all nine fragments remain visible instead of being capped at the quota");
        TestAssert.True(QuestFragmentProgressModel.HasQuota(ids, false),
            "nine fragments satisfy the entry quota");
        TestAssert.True(ids.SequenceEqual(before),
            "count and quota queries do not consume or alter collection records");
    }

    private static void QuotaRequiresSixDistinctKnownFragments()
    {
        string[] five = ExpectedDefinitions.Take(5).Select(value => value.StableId).ToArray();
        string[] six = ExpectedDefinitions.Take(6).Select(value => value.StableId).ToArray();
        TestAssert.True(!QuestFragmentProgressModel.HasQuota(five, false),
            "five fragments do not satisfy the six-fragment quota");
        TestAssert.True(QuestFragmentProgressModel.HasQuota(six, false),
            "six distinct registered fragments satisfy the quota");
        string[] paddedFive = five.Concat(new[]
        {
            five[0], "canyon.v2.archive-side-cave", "canyon.v2.archive-supply",
            "canyon.v2.fragment.unregistered"
        }).ToArray();
        TestAssert.True(!QuestFragmentProgressModel.HasQuota(paddedFive, false),
            "duplicates, discoveries and unknown IDs cannot pad five fragments to the quota");
    }

    private static void OptionalFragmentsCanReplaceMissedMainRouteFragments()
    {
        string[] mixedRoute =
        [
            "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
            "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.archive",
            "canyon.v2.fragment.steam-loop", "canyon.v2.fragment.temple-upper"
        ];
        TestAssert.Equal(6, QuestFragmentProgressModel.CountCollected(mixedRoute),
            "optional fragments count as real replacement fragments");
        TestAssert.True(QuestFragmentProgressModel.HasQuota(mixedRoute, false),
            "the quota does not require every main-route fragment");
    }

    private static void LegacyExemptionDoesNotFabricateCollectedFragments()
    {
        string[] empty = [];
        TestAssert.True(!QuestFragmentProgressModel.HasQuota(empty, false),
            "a new game with no exemption cannot enter with zero fragments");
        TestAssert.True(QuestFragmentProgressModel.HasQuota(empty, true),
            "an explicitly established legacy exemption bypasses only the quantity check");
        TestAssert.Equal(0, QuestFragmentProgressModel.CountCollected(empty),
            "legacy exemption leaves the honest collected count at zero");
        TestAssert.Equal(0, empty.Length, "legacy exemption does not create fragment IDs");
    }
}
