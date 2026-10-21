using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Knight.Prologue.Seamless;

public enum CanyonRouteGate
{
    None,
    ShortcutOpen,
    MechanismActivated,
    BossDefeated
}

public sealed record CanyonRouteNodeDefinition(
    CanyonRouteNodeId Id,
    PrologueSectorId SectorId,
    Rect2 WorldBounds,
    CanyonChallengeKind? ChallengeKind,
    bool Optional,
    bool RestArea,
    string StableDiscoveryId = "");

public sealed record CanyonRouteEdge(
    CanyonRouteNodeId From,
    CanyonRouteNodeId To,
    bool Bidirectional,
    CanyonRouteGate Gate = CanyonRouteGate.None,
    string StableShortcutId = "",
    string TraversalSegmentId = "",
    string RequiredMechanismId = "",
    string ReverseTraversalSegmentId = "");

public static class CanyonRouteGraph
{
    private const float GeometryEpsilon = .01f;

    public static IReadOnlyList<CanyonRouteNodeId> Reachable(
        CanyonRouteNodeId from,
        IReadOnlySet<string> openShortcuts,
        IReadOnlySet<string> activatedMechanisms,
        bool bossDefeated)
    {
        if (!Enum.IsDefined(from))
            throw new ArgumentOutOfRangeException(nameof(from), from, "Route node must be defined.");
        ArgumentNullException.ThrowIfNull(openShortcuts);
        ArgumentNullException.ThrowIfNull(activatedMechanisms);
        return Reachable(CanyonWorldLayout.Edges, from, openShortcuts,
            activatedMechanisms, bossDefeated);
    }

    public static IReadOnlyList<string> Validate(
        Rect2 designBounds,
        IReadOnlyDictionary<PrologueSectorId, Rect2> sectorBounds,
        IReadOnlyList<CanyonRouteNodeDefinition> nodes,
        IReadOnlyList<CanyonRouteEdge> edges,
        IReadOnlyDictionary<string, Vector2> checkpointWorldPositions,
        IReadOnlyList<string> stableCheckpointIds,
        IReadOnlyList<string> stableShortcutIds,
        IReadOnlyList<string> stableMechanismIds,
        IReadOnlyList<string> stableDiscoveryIds)
    {
        var errors = new List<string>();
        if (!FinitePositive(designBounds))
            errors.Add("DesignBounds must have finite position and positive finite size");
        if (sectorBounds == null || nodes == null || edges == null ||
            checkpointWorldPositions == null || stableCheckpointIds == null ||
            stableShortcutIds == null || stableMechanismIds == null ||
            stableDiscoveryIds == null)
        {
            errors.Add("layout graph collections must not be null");
            return errors;
        }

        ValidateStableIds(stableCheckpointIds, stableShortcutIds, stableMechanismIds,
            stableDiscoveryIds, errors);
        ValidateSectors(designBounds, sectorBounds, errors);

        var nodeById = new Dictionary<CanyonRouteNodeId, CanyonRouteNodeDefinition>();
        var challengeCounts = Enum.GetValues<CanyonChallengeKind>()
            .ToDictionary(kind => kind, _ => 0);
        foreach (CanyonRouteNodeDefinition node in nodes)
        {
            if (node == null)
            {
                errors.Add("route node must not be null");
                continue;
            }
            if (!Enum.IsDefined(node.Id)) errors.Add($"route node ID {node.Id} is undefined");
            else if (!nodeById.TryAdd(node.Id, node)) errors.Add($"duplicate route node {node.Id}");
            if (!Enum.IsDefined(node.SectorId) || !sectorBounds.TryGetValue(node.SectorId,
                    out Rect2 sector))
                errors.Add($"{node.Id} declares an undefined sector {node.SectorId}");
            else if (!Contains(sector, node.WorldBounds))
                errors.Add($"{node.Id} WorldBounds must stay inside declared sector {node.SectorId}");
            if (!FinitePositive(node.WorldBounds) || !Contains(designBounds, node.WorldBounds))
                errors.Add($"{node.Id} WorldBounds must stay inside DesignBounds");
            if (node.ChallengeKind.HasValue)
            {
                if (!Enum.IsDefined(node.ChallengeKind.Value))
                    errors.Add($"{node.Id} challenge kind is undefined");
                else challengeCounts[node.ChallengeKind.Value]++;
            }
            if (!string.IsNullOrEmpty(node.StableDiscoveryId) &&
                !stableDiscoveryIds.Contains(node.StableDiscoveryId, StringComparer.Ordinal))
                errors.Add($"{node.Id} references unknown stable discovery ID {node.StableDiscoveryId}");
        }

        foreach (CanyonRouteNodeId id in Enum.GetValues<CanyonRouteNodeId>())
            if (!nodeById.ContainsKey(id)) errors.Add($"missing route node {id}");
        foreach ((CanyonChallengeKind kind, int count) in challengeCounts)
            if (count != 1) errors.Add($"challenge kind {kind} must be represented exactly once; count={count}");
        foreach (string discoveryId in stableDiscoveryIds)
            if (!nodes.Any(node => node?.StableDiscoveryId == discoveryId))
                errors.Add($"unknown stable discovery ID {discoveryId}");

        ValidateCheckpoints(designBounds, checkpointWorldPositions, stableCheckpointIds, errors);
        ValidateEdges(edges, nodeById, stableShortcutIds, stableMechanismIds, errors);
        CanyonRouteEdge[] topologyEdges = edges.Where(edge => edge != null &&
            Enum.IsDefined(edge.From) && Enum.IsDefined(edge.To) &&
            nodeById.ContainsKey(edge.From) && nodeById.ContainsKey(edge.To)).ToArray();
        ValidateTopology(topologyEdges, nodeById, stableShortcutIds, stableMechanismIds, errors);
        return errors;
    }

    private static IReadOnlyList<CanyonRouteNodeId> Reachable(
        IReadOnlyList<CanyonRouteEdge> edges,
        CanyonRouteNodeId from,
        IReadOnlySet<string> openShortcuts,
        IReadOnlySet<string> activatedMechanisms,
        bool bossDefeated)
    {
        var reached = new HashSet<CanyonRouteNodeId> { from };
        var ordered = new List<CanyonRouteNodeId>();
        var queue = new Queue<CanyonRouteNodeId>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            CanyonRouteNodeId current = queue.Dequeue();
            ordered.Add(current);
            var destinations = new SortedSet<CanyonRouteNodeId>();
            foreach (CanyonRouteEdge edge in edges)
            {
                if (!GatePasses(edge, openShortcuts, activatedMechanisms, bossDefeated)) continue;
                if (edge.From == current) destinations.Add(edge.To);
                if (edge.Bidirectional && edge.To == current) destinations.Add(edge.From);
            }
            foreach (CanyonRouteNodeId destination in destinations)
                if (reached.Add(destination)) queue.Enqueue(destination);
        }
        return Array.AsReadOnly(ordered.ToArray());
    }

    private static bool GatePasses(CanyonRouteEdge edge,
        IReadOnlySet<string> openShortcuts,
        IReadOnlySet<string> activatedMechanisms,
        bool bossDefeated) => edge.Gate switch
    {
        CanyonRouteGate.None => true,
        CanyonRouteGate.ShortcutOpen => !string.IsNullOrWhiteSpace(edge.StableShortcutId) &&
            openShortcuts.Contains(edge.StableShortcutId),
        CanyonRouteGate.MechanismActivated => !string.IsNullOrWhiteSpace(edge.RequiredMechanismId) &&
            activatedMechanisms.Contains(edge.RequiredMechanismId),
        CanyonRouteGate.BossDefeated => bossDefeated,
        _ => false
    };

    private static void ValidateStableIds(IReadOnlyList<string> checkpointIds,
        IReadOnlyList<string> shortcutIds, IReadOnlyList<string> mechanismIds,
        IReadOnlyList<string> discoveryIds, List<string> errors)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in checkpointIds.Concat(shortcutIds).Concat(mechanismIds).Concat(discoveryIds))
        {
            if (string.IsNullOrWhiteSpace(id)) errors.Add("stable ID must be nonblank");
            else if (!seen.Add(id)) errors.Add($"duplicate stable ID {id}");
        }
        if (!mechanismIds.Contains(CanyonWorldLayout.CourtyardSealMechanismId,
                StringComparer.Ordinal))
            errors.Add($"stable mechanism IDs must declare {CanyonWorldLayout.CourtyardSealMechanismId}");
    }

    private static void ValidateSectors(Rect2 designBounds,
        IReadOnlyDictionary<PrologueSectorId, Rect2> sectorBounds, List<string> errors)
    {
        foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
            if (!sectorBounds.ContainsKey(id)) errors.Add($"missing sector bounds for {id}");
        foreach ((PrologueSectorId id, Rect2 bounds) in sectorBounds)
        {
            if (!Enum.IsDefined(id)) errors.Add($"sector bounds key {id} is undefined");
            if (!FinitePositive(bounds) || !Contains(designBounds, bounds))
                errors.Add($"{id} sector bounds must stay inside DesignBounds");
        }
        var sectors = sectorBounds.ToArray();
        for (int first = 0; first < sectors.Length; first++)
        for (int second = first + 1; second < sectors.Length; second++)
            if (InteriorOverlaps(sectors[first].Value, sectors[second].Value))
                errors.Add($"sector bounds {sectors[first].Key} and {sectors[second].Key} overlap");
    }

    private static void ValidateCheckpoints(Rect2 designBounds,
        IReadOnlyDictionary<string, Vector2> positions, IReadOnlyList<string> checkpointIds,
        List<string> errors)
    {
        foreach (string id in checkpointIds)
            if (string.IsNullOrWhiteSpace(id))
                continue;
            else if (!positions.TryGetValue(id, out Vector2 position))
                errors.Add($"checkpoint {id} has no world position");
            else if (!Finite(position) || !designBounds.HasPoint(position))
                errors.Add($"checkpoint {id} world position must stay inside DesignBounds");
        foreach (string id in positions.Keys)
            if (!checkpointIds.Contains(id, StringComparer.Ordinal))
                errors.Add($"checkpoint world position uses unknown stable ID {id}");
    }

    private static void ValidateEdges(IReadOnlyList<CanyonRouteEdge> edges,
        IReadOnlyDictionary<CanyonRouteNodeId, CanyonRouteNodeDefinition> nodes,
        IReadOnlyList<string> shortcutIds, IReadOnlyList<string> mechanismIds,
        List<string> errors)
    {
        var traversalIds = new HashSet<string>(StringComparer.Ordinal);
        var edgeKeys = new HashSet<(CanyonRouteNodeId From, CanyonRouteNodeId To)>();
        foreach (CanyonRouteEdge edge in edges)
        {
            if (edge == null)
            {
                errors.Add("route edge must not be null");
                continue;
            }
            if (!Enum.IsDefined(edge.From) || !Enum.IsDefined(edge.To))
            {
                errors.Add($"route edge endpoint {edge.From}->{edge.To} is undefined");
                continue;
            }
            if (!nodes.ContainsKey(edge.From) || !nodes.ContainsKey(edge.To))
                errors.Add($"route edge {edge.From}->{edge.To} references an unknown endpoint");
            if (edge.From == edge.To) errors.Add($"route edge {edge.From}->{edge.To} cannot self-loop");
            if (!edgeKeys.Add((edge.From, edge.To)))
                errors.Add($"duplicate route edge {edge.From}->{edge.To}");
            ValidateTraversalId(edge.TraversalSegmentId, edge, "forward", traversalIds, errors);
            if (edge.Bidirectional)
            {
                ValidateTraversalId(edge.ReverseTraversalSegmentId, edge, "reverse", traversalIds, errors);
                if (edge.ReverseTraversalSegmentId == edge.TraversalSegmentId)
                    errors.Add($"{edge.From}<->{edge.To} reverse traversal segment must be distinct");
            }
            else if (!string.IsNullOrEmpty(edge.ReverseTraversalSegmentId))
                errors.Add($"one-way edge {edge.From}->{edge.To} must leave reverse traversal segment empty");

            switch (edge.Gate)
            {
                case CanyonRouteGate.None:
                    if (!string.IsNullOrEmpty(edge.StableShortcutId) ||
                        !string.IsNullOrEmpty(edge.RequiredMechanismId))
                        errors.Add($"ungated edge {edge.From}->{edge.To} has mismatched gate ID fields");
                    break;
                case CanyonRouteGate.ShortcutOpen:
                    if (string.IsNullOrWhiteSpace(edge.StableShortcutId) ||
                        !shortcutIds.Contains(edge.StableShortcutId, StringComparer.Ordinal) ||
                        !string.IsNullOrEmpty(edge.RequiredMechanismId))
                        errors.Add($"shortcut gate {edge.From}->{edge.To} has mismatched or unknown gate ID fields");
                    break;
                case CanyonRouteGate.MechanismActivated:
                    if (string.IsNullOrWhiteSpace(edge.RequiredMechanismId) ||
                        !mechanismIds.Contains(edge.RequiredMechanismId, StringComparer.Ordinal) ||
                        !string.IsNullOrEmpty(edge.StableShortcutId))
                        errors.Add($"mechanism gate {edge.From}->{edge.To} has mismatched or unknown gate ID fields");
                    break;
                case CanyonRouteGate.BossDefeated:
                    if (!string.IsNullOrEmpty(edge.StableShortcutId) ||
                        !string.IsNullOrEmpty(edge.RequiredMechanismId))
                        errors.Add($"BossDefeated gate {edge.From}->{edge.To} has mismatched gate ID fields");
                    break;
                default:
                    errors.Add($"edge {edge.From}->{edge.To} gate is undefined");
                    break;
            }
        }

        foreach (string shortcutId in shortcutIds)
            if (!edges.Any(edge => edge?.StableShortcutId == shortcutId))
                errors.Add($"unknown stable shortcut ID {shortcutId}");
        foreach (string mechanismId in mechanismIds)
            if (!edges.Any(edge => edge?.RequiredMechanismId == mechanismId))
                errors.Add($"unknown stable mechanism ID {mechanismId}");
    }

    private static void ValidateTraversalId(string id, CanyonRouteEdge edge, string direction,
        HashSet<string> seen, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
            errors.Add($"{edge.From}->{edge.To} {direction} TraversalSegmentId must be nonblank");
        else if (!seen.Add(id)) errors.Add($"duplicate traversal segment ID {id}");
    }

    private static void ValidateTopology(IReadOnlyList<CanyonRouteEdge> edges,
        IReadOnlyDictionary<CanyonRouteNodeId, CanyonRouteNodeDefinition> nodes,
        IReadOnlyList<string> shortcutIds,
        IReadOnlyList<string> mechanismIds,
        List<string> errors)
    {
        bool HasEdge(CanyonRouteNodeId from, CanyonRouteNodeId to) => edges.Any(edge =>
            edge != null && edge.From == from && edge.To == to && edge.Gate == CanyonRouteGate.None);
        if (!(HasEdge(CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.ArchiveSideCave) &&
              HasEdge(CanyonRouteNodeId.ArchiveSideCave, CanyonRouteNodeId.FrozenDetour) &&
              HasEdge(CanyonRouteNodeId.FrozenDetour, CanyonRouteNodeId.BasinCamp)))
            errors.Add("descent loop must pass ArchiveSideCave and FrozenDetour to BasinCamp");
        if (!(HasEdge(CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.ControlledAbyss) &&
              HasEdge(CanyonRouteNodeId.ControlledAbyss, CanyonRouteNodeId.BasinCamp)))
            errors.Add("ControlledAbyss main alternative must rejoin at BasinCamp");
        if (!(HasEdge(CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.SteamValves) &&
              HasEdge(CanyonRouteNodeId.SteamValves, CanyonRouteNodeId.CounterweightLift)))
            errors.Add("SteamValves alternative must rejoin at CounterweightLift");
        if (!HasEdge(CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.CounterweightLift))
            errors.Add("direct high route must reach CounterweightLift without SteamValves");

        CanyonRouteEdge[] bidirectionalEdges = edges.Where(edge => edge?.Bidirectional == true).ToArray();
        CanyonRouteEdge[] persistentShortcuts = bidirectionalEdges.Where(edge =>
            edge.Gate == CanyonRouteGate.ShortcutOpen).ToArray();
        if (persistentShortcuts.Length != 1 || !IsUpperBasinShortcut(persistentShortcuts[0]))
            errors.Add("upper/basin route must retain its exact persistent bidirectional shortcut");
        foreach (CanyonRouteEdge edge in bidirectionalEdges)
            if (!IsUpperBasinShortcut(edge) && !IsAbyssCollectionReturn(edge))
                errors.Add($"unapproved bidirectional return route {edge.From}<->{edge.To}");

        var bossEntries = LogicalEntriesTo(edges, CanyonRouteNodeId.BossHall);
        if (bossEntries.Length != 1 ||
            bossEntries[0].From != CanyonRouteNodeId.SealedCourtyard ||
            bossEntries[0].Edge.Gate != CanyonRouteGate.MechanismActivated ||
            bossEntries[0].Edge.RequiredMechanismId != CanyonWorldLayout.CourtyardSealMechanismId)
            errors.Add($"BossHall must have exactly one {CanyonWorldLayout.CourtyardSealMechanismId} mechanism-gated entry");
        var safeEntries = LogicalEntriesTo(edges, CanyonRouteNodeId.SafeExit);
        if (safeEntries.Length != 1 ||
            safeEntries[0].From != CanyonRouteNodeId.BossHall ||
            safeEntries[0].Edge.Gate != CanyonRouteGate.BossDefeated)
            errors.Add("SafeExit must have exactly one explicit BossDefeated-gated entry");

        if (!nodes.ContainsKey(CanyonRouteNodeId.LowCeilingBridge)) return;
        var none = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<CanyonRouteNodeId> ungated = Reachable(edges,
            CanyonRouteNodeId.LowCeilingBridge, none, none, false);
        foreach (CanyonRouteNodeId id in nodes.Keys.Where(id =>
                     id is not CanyonRouteNodeId.BossHall and not CanyonRouteNodeId.SafeExit))
            if (!ungated.Contains(id)) errors.Add($"route node {id} is disconnected before SealedCourtyard");
        var openShortcuts = new HashSet<string>(shortcutIds, StringComparer.Ordinal);
        IReadOnlyList<CanyonRouteNodeId> withoutSeal = Reachable(edges,
            CanyonRouteNodeId.LowCeilingBridge, openShortcuts, none, true);
        if (withoutSeal.Contains(CanyonRouteNodeId.BossHall))
            errors.Add("BossHall must remain unreachable without its exact completed-seal mechanism");

        var mechanisms = new HashSet<string>(mechanismIds, StringComparer.Ordinal);
        IReadOnlyList<CanyonRouteNodeId> withoutBoss = Reachable(edges,
            CanyonRouteNodeId.LowCeilingBridge, openShortcuts, mechanisms, false);
        if (withoutBoss.Contains(CanyonRouteNodeId.SafeExit))
            errors.Add("SafeExit must remain unreachable until the BossDefeated gate passes");

        IReadOnlyList<CanyonRouteNodeId> complete = Reachable(edges,
            CanyonRouteNodeId.LowCeilingBridge, openShortcuts, mechanisms, true);
        if (!complete.Contains(CanyonRouteNodeId.SafeExit))
            errors.Add("full main route must reach SafeExit after all legitimate gate inputs");
    }

    // An explicit allow-list, not permission to reverse every forward edge.
    // Collection return metadata is publishable only after the generated
    // multi-leg/cross-sector contracts have their own physical witnesses.
    private static bool IsUpperBasinShortcut(CanyonRouteEdge edge) =>
        edge.From == CanyonRouteNodeId.SplitLevelEncounter &&
        edge.To == CanyonRouteNodeId.BasinCamp && edge.Gate == CanyonRouteGate.ShortcutOpen &&
        edge.StableShortcutId == CanyonWorldLayout.UpperBasinShortcutId &&
        edge.TraversalSegmentId == "canyon.v2.route.upper-to-basin-shortcut" &&
        edge.ReverseTraversalSegmentId == "canyon.v2.route.basin-to-upper-shortcut";

    private static bool IsAbyssCollectionReturn(CanyonRouteEdge edge) =>
        edge.From == CanyonRouteNodeId.ControlledAbyss &&
        edge.To == CanyonRouteNodeId.BasinCamp && edge.Gate == CanyonRouteGate.None &&
        edge.TraversalSegmentId == "canyon.v2.route.abyss-to-basin-camp" &&
        edge.ReverseTraversalSegmentId == "canyon.v2.route.basin-camp-to-abyss";

    private static (CanyonRouteNodeId From, CanyonRouteEdge Edge)[] LogicalEntriesTo(
        IReadOnlyList<CanyonRouteEdge> edges, CanyonRouteNodeId target)
    {
        var entries = new List<(CanyonRouteNodeId From, CanyonRouteEdge Edge)>();
        foreach (CanyonRouteEdge edge in edges)
        {
            if (edge == null) continue;
            if (edge.To == target) entries.Add((edge.From, edge));
            if (edge.Bidirectional && edge.From == target) entries.Add((edge.To, edge));
        }
        return entries.ToArray();
    }

    private static bool Finite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static bool FinitePositive(Rect2 value) => Finite(value.Position) &&
        float.IsFinite(value.Size.X) && float.IsFinite(value.Size.Y) &&
        value.Size.X > 0f && value.Size.Y > 0f;

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X - GeometryEpsilon &&
        inner.Position.Y >= outer.Position.Y - GeometryEpsilon &&
        inner.End.X <= outer.End.X + GeometryEpsilon &&
        inner.End.Y <= outer.End.Y + GeometryEpsilon;

    private static bool InteriorOverlaps(Rect2 first, Rect2 second) =>
        MathF.Min(first.End.X, second.End.X) - MathF.Max(first.Position.X, second.Position.X) > GeometryEpsilon &&
        MathF.Min(first.End.Y, second.End.Y) - MathF.Max(first.Position.Y, second.Position.Y) > GeometryEpsilon;
}
