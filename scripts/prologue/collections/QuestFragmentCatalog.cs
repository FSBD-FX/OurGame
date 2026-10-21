using System;
using System.Collections.Generic;
using System.Linq;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Collections;

public sealed record QuestFragmentDefinition(string StableId, CanyonRouteNodeId Region, bool Optional);

public static class QuestFragmentCatalog
{
    public const int RequiredCount = 6;

    public static IReadOnlyList<QuestFragmentDefinition> Definitions { get; } =
        Array.AsReadOnly(new[]
        {
            new QuestFragmentDefinition("canyon.v2.fragment.entrance", CanyonRouteNodeId.LowCeilingBridge, false),
            new QuestFragmentDefinition("canyon.v2.fragment.rift", CanyonRouteNodeId.StaggeredRift, false),
            new QuestFragmentDefinition("canyon.v2.fragment.abyss-floor", CanyonRouteNodeId.ControlledAbyss, false),
            new QuestFragmentDefinition("canyon.v2.fragment.gear-gallery", CanyonRouteNodeId.GearPistons, false),
            new QuestFragmentDefinition("canyon.v2.fragment.ascent-mid", CanyonRouteNodeId.CounterweightLift, false),
            new QuestFragmentDefinition("canyon.v2.fragment.courtyard", CanyonRouteNodeId.SealedCourtyard, false),
            new QuestFragmentDefinition("canyon.v2.fragment.archive", CanyonRouteNodeId.ArchiveSideCave, true),
            new QuestFragmentDefinition("canyon.v2.fragment.steam-loop", CanyonRouteNodeId.SteamValves, true),
            new QuestFragmentDefinition("canyon.v2.fragment.temple-upper", CanyonRouteNodeId.SealedCourtyard, true)
        });
    public static IReadOnlyList<string> StableIds { get; } =
        Array.AsReadOnly(Definitions.Select(value => value.StableId).ToArray());

    private static readonly HashSet<string> KnownIds = new(StableIds, StringComparer.Ordinal);

    public static bool IsKnown(string stableId) => stableId != null && KnownIds.Contains(stableId);
}
