using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class CanyonWorldLayoutTests
{
    private static readonly IReadOnlySet<string> NoIds = new HashSet<string>();

    public static void Run()
    {
        ApprovedEnvelopeAndSectorRectanglesAreExact();
        EveryChallengeContractIsRepresentedExactlyOnce();
        ForwardRoutesSplitAndRejoinAtBothAuthoredLoops();
        GateInputsCannotSubstituteForOneAnother();
        StableIdsAndTraversalContractsAreUniqueAndDeclared();
        LayoutValidationRejectsBrokenContracts();
        ValidationAcceptsOnlyTheNamedAbyssCollectionReturn();
        PublishedAbyssCollectionReturnNeedsNoBossOrShortcut();
        ValidationRejectsReverseGateBypasses();
        ValidationReportsNullEdgesWithoutThrowing();
        ValidationReportsNullCheckpointIdsWithoutThrowing();
        PublishedLayoutCollectionsAreImmutable();
    }

    private static void ApprovedEnvelopeAndSectorRectanglesAreExact()
    {
        TestAssert.Equal(2, CanyonWorldLayout.CurrentLayoutVersion,
            "layout version identifies the continuous canyon save boundary");
        TestAssert.Equal(new Rect2(0f, 0f, 7680f, 4320f), CanyonWorldLayout.DesignBounds,
            "design envelope is the approved six-by-six-screen extent");

        var expected = new Dictionary<PrologueSectorId, Rect2>
        {
            [PrologueSectorId.CanyonEntrance] = new(0f, 0f, 1280f, 720f),
            [PrologueSectorId.CanyonDescent] = new(1280f, 0f, 1280f, 3600f),
            [PrologueSectorId.CanyonBasin] = new(1280f, 3600f, 3840f, 720f),
            [PrologueSectorId.CanyonAscent] = new(3840f, 720f, 1280f, 2880f),
            [PrologueSectorId.LostTemple] = new(5120f, 720f, 2560f, 720f)
        };

        TestAssert.Equal(expected.Count, CanyonWorldLayout.SectorBounds.Count,
            "layout publishes exactly the five save-visible sectors");
        foreach ((PrologueSectorId id, Rect2 bounds) in expected)
            TestAssert.Equal(bounds, CanyonWorldLayout.SectorBounds[id],
                $"{id} uses its authoritative world rectangle");

        PrologueSectorId[] ids = expected.Keys.ToArray();
        for (int first = 0; first < ids.Length; first++)
        for (int second = first + 1; second < ids.Length; second++)
            TestAssert.True(!InteriorOverlaps(expected[ids[first]], expected[ids[second]]),
                $"{ids[first]} and {ids[second]} do not overlap in their interiors");
    }

    private static void EveryChallengeContractIsRepresentedExactlyOnce()
    {
        var expectedBounds = new Dictionary<CanyonRouteNodeId, (PrologueSectorId Sector, Rect2 Bounds)>
        {
            [CanyonRouteNodeId.LowCeilingBridge] = (PrologueSectorId.CanyonEntrance, new(0f, 0f, 1280f, 720f)),
            [CanyonRouteNodeId.StaggeredRift] = (PrologueSectorId.CanyonDescent, new(1280f, 0f, 1280f, 400f)),
            [CanyonRouteNodeId.ControlledAbyss] = (PrologueSectorId.CanyonDescent, new(1920f, 400f, 640f, 3200f)),
            [CanyonRouteNodeId.ArchiveSideCave] = (PrologueSectorId.CanyonDescent, new(1280f, 400f, 640f, 1400f)),
            [CanyonRouteNodeId.FrozenDetour] = (PrologueSectorId.CanyonDescent, new(1280f, 1800f, 640f, 1560f)),
            [CanyonRouteNodeId.BasinCamp] = (PrologueSectorId.CanyonBasin, new(1280f, 3600f, 800f, 720f)),
            [CanyonRouteNodeId.GearPistons] = (PrologueSectorId.CanyonBasin, new(2080f, 3600f, 960f, 720f)),
            [CanyonRouteNodeId.SteamValves] = (PrologueSectorId.CanyonBasin, new(3040f, 3960f, 1160f, 360f)),
            [CanyonRouteNodeId.CounterweightLift] = (PrologueSectorId.CanyonAscent, new(3840f, 2800f, 1280f, 800f)),
            [CanyonRouteNodeId.PendulumGallery] = (PrologueSectorId.CanyonAscent, new(3840f, 2000f, 1280f, 800f)),
            [CanyonRouteNodeId.SplitLevelEncounter] = (PrologueSectorId.CanyonAscent, new(3840f, 1200f, 1280f, 800f)),
            [CanyonRouteNodeId.SealedCourtyard] = (PrologueSectorId.LostTemple, new(5120f, 720f, 960f, 720f)),
            [CanyonRouteNodeId.BossHall] = (PrologueSectorId.LostTemple, new(6080f, 720f, 1200f, 720f)),
            [CanyonRouteNodeId.SafeExit] = (PrologueSectorId.LostTemple, new(7280f, 720f, 400f, 720f))
        };

        TestAssert.Equal(14, CanyonWorldLayout.Nodes.Count,
            "layout publishes all twelve challenges plus two support nodes");
        foreach (CanyonRouteNodeDefinition node in CanyonWorldLayout.Nodes)
        {
            TestAssert.True(expectedBounds.TryGetValue(node.Id, out var expected),
                $"{node.Id} is an approved route node");
            TestAssert.Equal(expected.Sector, node.SectorId, $"{node.Id} declares its containing sector");
            TestAssert.Equal(expected.Bounds, node.WorldBounds, $"{node.Id} keeps its authoritative world bounds");
            TestAssert.True(Contains(CanyonWorldLayout.DesignBounds, node.WorldBounds),
                $"{node.Id} stays inside the design envelope");
            TestAssert.True(Contains(CanyonWorldLayout.SectorBounds[node.SectorId], node.WorldBounds),
                $"{node.Id} stays inside its declared sector");
            if (node.ChallengeKind.HasValue)
                TestAssert.Equal(node.Id.ToString(), node.ChallengeKind.Value.ToString(),
                    $"{node.Id} carries its matching interaction contract kind");
        }

        CanyonChallengeKind[] expectedKinds = Enum.GetValues<CanyonChallengeKind>();
        CanyonChallengeKind[] actualKinds = CanyonWorldLayout.Nodes
            .Where(node => node.ChallengeKind.HasValue)
            .Select(node => node.ChallengeKind!.Value)
            .OrderBy(kind => kind)
            .ToArray();
        TestAssert.Equal(12, actualKinds.Length, "there are exactly twelve challenge contracts");
        TestAssert.Equal(string.Join(",", expectedKinds), string.Join(",", actualKinds),
            "every challenge kind is represented exactly once");
        TestAssert.True(Node(CanyonRouteNodeId.BasinCamp).ChallengeKind == null,
            "BasinCamp is support/rest data rather than a thirteenth challenge kind");
        TestAssert.True(Node(CanyonRouteNodeId.SafeExit).ChallengeKind == null,
            "SafeExit is support/end data rather than a thirteenth challenge kind");
    }

    private static void ForwardRoutesSplitAndRejoinAtBothAuthoredLoops()
    {
        TestAssert.True(HasPath(CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.BasinCamp,
                CanyonRouteNodeId.ArchiveSideCave, CanyonRouteNodeId.FrozenDetour),
            "descent exploration alternative passes ArchiveSideCave and FrozenDetour before rejoining BasinCamp");
        TestAssert.True(HasPathAvoiding(CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.BasinCamp,
                CanyonRouteNodeId.ArchiveSideCave, CanyonRouteNodeId.FrozenDetour),
            "controlled-abyss main route independently reaches BasinCamp");
        TestAssert.True(HasPath(CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.CounterweightLift,
                CanyonRouteNodeId.SteamValves),
            "steam-valves alternative rejoins at CounterweightLift");
        TestAssert.True(CanyonWorldLayout.Edges.Any(edge =>
                edge.From == CanyonRouteNodeId.GearPistons &&
                edge.To == CanyonRouteNodeId.CounterweightLift && !edge.Bidirectional),
            "direct high route independently reaches CounterweightLift");

        CanyonRouteEdge[] shortcuts = CanyonWorldLayout.Edges
            .Where(edge => edge.Gate == CanyonRouteGate.ShortcutOpen).ToArray();
        TestAssert.Equal(1, shortcuts.Length, "there is exactly one gated persistent shortcut");
        TestAssert.True(shortcuts[0].Bidirectional &&
                shortcuts[0].From == CanyonRouteNodeId.SplitLevelEncounter &&
                shortcuts[0].To == CanyonRouteNodeId.BasinCamp &&
                shortcuts[0].StableShortcutId == "canyon.v2.upper-basin-shortcut",
            "upper/basin shortcut retains its exact persistent bidirectional route contract");

        IReadOnlyList<CanyonRouteNodeId> ungated = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.LowCeilingBridge, NoIds, NoIds, bossDefeated: false);
        AssertSequence(ungated,
            [CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift,
                CanyonRouteNodeId.ControlledAbyss, CanyonRouteNodeId.ArchiveSideCave,
                CanyonRouteNodeId.BasinCamp, CanyonRouteNodeId.FrozenDetour,
                CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.SteamValves,
                CanyonRouteNodeId.CounterweightLift, CanyonRouteNodeId.PendulumGallery,
                CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.SealedCourtyard],
            "breadth-first traversal is deterministic and follows only declared edges");
        TestAssert.True(ungated.Contains(CanyonRouteNodeId.SealedCourtyard),
            "ungated main route reaches through SealedCourtyard");
        TestAssert.True(!ungated.Contains(CanyonRouteNodeId.BossHall) &&
                !ungated.Contains(CanyonRouteNodeId.SafeExit),
            "ungated traversal stops before the sealed Boss entry");
    }

    private static void GateInputsCannotSubstituteForOneAnother()
    {
        var shortcut = new HashSet<string> { "canyon.v2.upper-basin-shortcut" };
        var seal = new HashSet<string> { "canyon.v2.courtyard-seal-open" };

        IReadOnlyList<CanyonRouteNodeId> noShortcut = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.SplitLevelEncounter, NoIds, seal, bossDefeated: true);
        TestAssert.True(!noShortcut.Contains(CanyonRouteNodeId.BasinCamp),
            "completed seal and Boss flag cannot substitute for the exact shortcut activation");
        IReadOnlyList<CanyonRouteNodeId> openShortcut = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.SplitLevelEncounter, shortcut, NoIds, bossDefeated: false);
        TestAssert.True(openShortcut.Contains(CanyonRouteNodeId.BasinCamp),
            "exact shortcut activation exposes the reverse upper/basin route");

        IReadOnlyList<CanyonRouteNodeId> bossOnly = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.LowCeilingBridge, NoIds, NoIds, bossDefeated: true);
        TestAssert.True(!bossOnly.Contains(CanyonRouteNodeId.BossHall),
            "Boss flag cannot substitute for completed courtyard seal activation");
        IReadOnlyList<CanyonRouteNodeId> shortcutOnly = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.LowCeilingBridge, shortcut, NoIds, bossDefeated: false);
        TestAssert.True(!shortcutOnly.Contains(CanyonRouteNodeId.BossHall),
            "shortcut activation cannot substitute for completed courtyard seal activation");

        IReadOnlyList<CanyonRouteNodeId> sealOnly = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.LowCeilingBridge, NoIds, seal, bossDefeated: false);
        TestAssert.True(sealOnly.Contains(CanyonRouteNodeId.BossHall) &&
                !sealOnly.Contains(CanyonRouteNodeId.SafeExit),
            "completed courtyard seal exposes BossHall but not the Boss-gated SafeExit");
        IReadOnlyList<CanyonRouteNodeId> allGates = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.LowCeilingBridge, shortcut, seal, bossDefeated: true);
        TestAssert.True(allGates.Contains(CanyonRouteNodeId.SafeExit),
            "all legitimate gate inputs expose the full main route");
    }

    private static void StableIdsAndTraversalContractsAreUniqueAndDeclared()
    {
        var allStableIds = new List<string>();
        allStableIds.AddRange(CanyonWorldLayout.StableCheckpointIds);
        allStableIds.AddRange(CanyonWorldLayout.StableShortcutIds);
        allStableIds.AddRange(CanyonWorldLayout.StableMechanismIds);
        allStableIds.AddRange(CanyonWorldLayout.StableDiscoveryIds);
        TestAssert.True(allStableIds.All(id => !string.IsNullOrWhiteSpace(id)),
            "all published stable IDs are nonblank");
        TestAssert.Equal(allStableIds.Count, allStableIds.Distinct(StringComparer.Ordinal).Count(),
            "all published stable IDs are globally unique");
        TestAssert.Equal(CanyonWorldLayout.StableCheckpointIds.Count,
            CanyonWorldLayout.CheckpointWorldPositions.Count,
            "every stable checkpoint ID has one world position");
        foreach (string checkpointId in CanyonWorldLayout.StableCheckpointIds)
            TestAssert.True(CanyonWorldLayout.CheckpointWorldPositions.TryGetValue(checkpointId,
                    out Vector2 position) && CanyonWorldLayout.DesignBounds.HasPoint(position),
                $"checkpoint {checkpointId} has a position inside the design envelope");

        var traversalIds = new List<string>();
        foreach (CanyonRouteEdge edge in CanyonWorldLayout.Edges)
        {
            TestAssert.True(!string.IsNullOrWhiteSpace(edge.TraversalSegmentId),
                $"{edge.From}->{edge.To} declares a forward traversal segment");
            traversalIds.Add(edge.TraversalSegmentId);
            if (edge.Bidirectional)
            {
                TestAssert.True(!string.IsNullOrWhiteSpace(edge.ReverseTraversalSegmentId) &&
                        edge.ReverseTraversalSegmentId != edge.TraversalSegmentId,
                    $"{edge.From}<->{edge.To} declares a distinct reverse traversal segment");
                traversalIds.Add(edge.ReverseTraversalSegmentId);
            }
            else
                TestAssert.True(string.IsNullOrEmpty(edge.ReverseTraversalSegmentId),
                    $"{edge.From}->{edge.To} does not imply reverse passability");
        }
        TestAssert.Equal(traversalIds.Count, traversalIds.Distinct(StringComparer.Ordinal).Count(),
            "all forward and reverse traversal segment IDs are globally unique");
    }

    private static void LayoutValidationRejectsBrokenContracts()
    {
        TestAssert.Equal(0, CanyonWorldLayout.Validate().Count,
            "approved canyon layout satisfies its complete graph contract");

        AssertInvalid(edges: CanyonWorldLayout.Edges.Where(edge =>
                !(edge.From == CanyonRouteNodeId.SteamValves &&
                  edge.To == CanyonRouteNodeId.CounterweightLift)).ToArray(),
            expectedMessage: "SteamValves", message: "a branch that cannot rejoin is rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Where(edge =>
                !(edge.From == CanyonRouteNodeId.ControlledAbyss &&
                  edge.To == CanyonRouteNodeId.BasinCamp)).ToArray(),
            expectedMessage: "ControlledAbyss",
            message: "the main abyss alternative must also rejoin BasinCamp");
        AssertInvalid(stableCheckpointIds:
                CanyonWorldLayout.StableCheckpointIds.Concat(
                    [CanyonWorldLayout.StableCheckpointIds[0]]).ToArray(),
            expectedMessage: "duplicate", message: "duplicate stable IDs are rejected");
        AssertInvalid(nodes: CanyonWorldLayout.Nodes.Select(node =>
                node.Id == CanyonRouteNodeId.BossHall
                    ? node with { ChallengeKind = null }
                    : node).ToArray(),
            expectedMessage: "challenge kind BossHall",
            message: "a missing challenge kind is rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Append(CanyonWorldLayout.Edges[0] with
            { From = (CanyonRouteNodeId)999, TraversalSegmentId = "canyon.v2.route.unknown" }).ToArray(),
            expectedMessage: "undefined", message: "undefined edge endpoints are rejected");
        AssertInvalid(nodes: CanyonWorldLayout.Nodes.Select(node =>
                node.Id == CanyonRouteNodeId.LowCeilingBridge
                    ? node with { WorldBounds = new Rect2(-1f, 0f, 1280f, 720f) }
                    : node).ToArray(),
            expectedMessage: "DesignBounds", message: "node coordinates outside the design envelope are rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Select(edge =>
                edge.To == CanyonRouteNodeId.BossHall
                    ? edge with { Gate = CanyonRouteGate.ShortcutOpen,
                        StableShortcutId = "canyon.v2.upper-basin-shortcut", RequiredMechanismId = "" }
                    : edge).ToArray(),
            expectedMessage: "BossHall", message: "BossHall entry without completed-seal mechanism gate is rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Select(edge =>
                edge.To == CanyonRouteNodeId.SafeExit
                    ? edge with { Gate = CanyonRouteGate.None }
                    : edge).ToArray(),
            expectedMessage: "SafeExit", message: "SafeExit entry without explicit BossDefeated gate is rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Select(edge =>
                edge.Gate == CanyonRouteGate.ShortcutOpen
                    ? edge with { StableShortcutId = "canyon.v2.unknown-shortcut" }
                    : edge).ToArray(),
            expectedMessage: "unknown", message: "unknown stable shortcut IDs are rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Where(edge =>
                !(edge.From == CanyonRouteNodeId.PendulumGallery &&
                  edge.To == CanyonRouteNodeId.SplitLevelEncounter)).ToArray(),
            expectedMessage: "disconnected", message: "a disconnection before SealedCourtyard is rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Select((edge, index) => index == 1
                ? edge with { TraversalSegmentId = CanyonWorldLayout.Edges[0].TraversalSegmentId }
                : edge).ToArray(),
            expectedMessage: "duplicate traversal",
            message: "duplicate traversal segment IDs are rejected");
        AssertInvalid(edges: CanyonWorldLayout.Edges.Select(edge =>
                edge.Gate == CanyonRouteGate.None
                    ? edge with { RequiredMechanismId = "canyon.v2.courtyard-seal-open" }
                    : edge).ToArray(),
            expectedMessage: "gate", message: "gate-specific ID fields cannot be attached to an ungated edge");
    }

    // A valid same-player Camp->Abyss pickup->Camp route must not be rejected
    // merely because the persistent shortcut is no longer the only two-way edge.
    // This tests validation capability, not publication or physical reachability.
    private static void PublishedAbyssCollectionReturnNeedsNoBossOrShortcut()
    {
        CanyonRouteEdge edge = CanyonWorldLayout.Edges.Single(candidate =>
            candidate.From == CanyonRouteNodeId.ControlledAbyss && candidate.To == CanyonRouteNodeId.BasinCamp);
        TestAssert.True(edge.Bidirectional && edge.Gate == CanyonRouteGate.None &&
            edge.ReverseTraversalSegmentId == "canyon.v2.route.basin-camp-to-abyss",
            "the verified original-wall claw/drop route has an explicit published reverse ID");
        IReadOnlyList<CanyonRouteNodeId> returning = CanyonRouteGraph.Reachable(
            CanyonRouteNodeId.BasinCamp, NoIds, NoIds, bossDefeated: false);
        TestAssert.True(returning.Contains(CanyonRouteNodeId.ControlledAbyss),
            "camp can return to its abyss pickup before any shortcut, seal or Boss gate");
        TestAssert.True(!returning.Contains(CanyonRouteNodeId.BossHall) && !returning.Contains(CanyonRouteNodeId.SafeExit),
            "a new collection return does not bypass either the Boss admission or defeated-only exit gate");
    }

    private static void ValidationAcceptsOnlyTheNamedAbyssCollectionReturn()
    {
        CanyonRouteEdge[] returning = CanyonWorldLayout.Edges.Select(edge =>
            edge.From == CanyonRouteNodeId.ControlledAbyss && edge.To == CanyonRouteNodeId.BasinCamp
                ? edge with { Bidirectional = true,
                    ReverseTraversalSegmentId = "canyon.v2.route.basin-camp-to-abyss" }
                : edge).ToArray();
        IReadOnlyList<string> errors = CanyonRouteGraph.Validate(
            CanyonWorldLayout.DesignBounds, CanyonWorldLayout.SectorBounds,
            CanyonWorldLayout.Nodes, returning, CanyonWorldLayout.CheckpointWorldPositions,
            CanyonWorldLayout.StableCheckpointIds, CanyonWorldLayout.StableShortcutIds,
            CanyonWorldLayout.StableMechanismIds, CanyonWorldLayout.StableDiscoveryIds);
        TestAssert.Equal(0, errors.Count,
            "the exact ungated abyss collection return can coexist with the gated persistent shortcut; " +
            string.Join("; ", errors));

        AssertInvalid(edges: returning.Select(edge => edge.From == CanyonRouteNodeId.ControlledAbyss
                && edge.To == CanyonRouteNodeId.BasinCamp
                ? edge with { ReverseTraversalSegmentId = "canyon.v2.route.unknown-return" } : edge).ToArray(),
            expectedMessage: "unapproved bidirectional",
            message: "an undeclared reverse segment cannot impersonate the proven abyss return");
        AssertInvalid(edges: returning.Select(edge => edge.From == CanyonRouteNodeId.ControlledAbyss
                && edge.To == CanyonRouteNodeId.BasinCamp
                ? edge with { Gate = CanyonRouteGate.BossDefeated } : edge).ToArray(),
            expectedMessage: "unapproved bidirectional",
            message: "a collection return cannot be made dependent on first defeating the Boss");
        AssertInvalid(edges: returning.Select(edge => edge.Gate == CanyonRouteGate.ShortcutOpen
                ? edge with { Bidirectional = false, ReverseTraversalSegmentId = "" } : edge).ToArray(),
            expectedMessage: "persistent bidirectional shortcut",
            message: "adding collection returns cannot silently remove the original two-way shortcut");
    }

    private static void ValidationRejectsReverseGateBypasses()
    {
        AssertInvalid(edges: CanyonWorldLayout.Edges.Append(new CanyonRouteEdge(
                CanyonRouteNodeId.ArchiveSideCave,
                CanyonRouteNodeId.ControlledAbyss,
                Bidirectional: true,
                Gate: CanyonRouteGate.None,
                TraversalSegmentId: "canyon.v2.route.unapproved-bidirectional",
                ReverseTraversalSegmentId: "canyon.v2.route.unapproved-bidirectional-return"))
            .ToArray(),
            expectedMessage: "unapproved bidirectional",
            message: "arbitrary bidirectional additions are not collection return contracts");

        AssertInvalid(edges: CanyonWorldLayout.Edges.Append(new CanyonRouteEdge(
                CanyonRouteNodeId.BossHall,
                CanyonRouteNodeId.SealedCourtyard,
                Bidirectional: true,
                Gate: CanyonRouteGate.None,
                TraversalSegmentId: "canyon.v2.route.reverse-boss-bypass",
                ReverseTraversalSegmentId: "canyon.v2.route.reverse-boss-bypass-return"))
            .ToArray(),
            expectedMessage: "BossHall",
            message: "a reverse direction cannot bypass the completed courtyard seal");

        AssertInvalid(edges: CanyonWorldLayout.Edges.Append(new CanyonRouteEdge(
                CanyonRouteNodeId.SafeExit,
                CanyonRouteNodeId.SealedCourtyard,
                Bidirectional: true,
                Gate: CanyonRouteGate.None,
                TraversalSegmentId: "canyon.v2.route.reverse-safe-exit-bypass",
                ReverseTraversalSegmentId: "canyon.v2.route.reverse-safe-exit-bypass-return"))
            .ToArray(),
            expectedMessage: "SafeExit",
            message: "a reverse direction cannot bypass the defeated-Boss requirement");
    }

    private static void ValidationReportsNullEdgesWithoutThrowing()
    {
        AssertInvalid(edges: CanyonWorldLayout.Edges.Append((CanyonRouteEdge)null).ToArray(),
            expectedMessage: "route edge must not be null",
            message: "a malformed null edge is reported without throwing");
    }

    private static void ValidationReportsNullCheckpointIdsWithoutThrowing()
    {
        AssertInvalid(stableCheckpointIds: CanyonWorldLayout.StableCheckpointIds
                .Append((string)null).ToArray(),
            expectedMessage: "stable ID must be nonblank",
            message: "a malformed null checkpoint ID is reported without throwing");
    }

    private static void PublishedLayoutCollectionsAreImmutable()
    {
        AssertReadOnly((IList<CanyonRouteNodeDefinition>)CanyonWorldLayout.Nodes,
            Node(CanyonRouteNodeId.LowCeilingBridge), "published nodes cannot be mutated");
        AssertReadOnly((IList<CanyonRouteEdge>)CanyonWorldLayout.Edges,
            CanyonWorldLayout.Edges[0], "published edges cannot be mutated");
        TestAssert.True(CanyonWorldLayout.SectorBounds is not Dictionary<PrologueSectorId, Rect2>,
            "published sector bounds do not expose their mutable backing dictionary");
        TestAssert.True(CanyonWorldLayout.CheckpointWorldPositions is not Dictionary<string, Vector2>,
            "published checkpoint positions do not expose their mutable backing dictionary");
    }

    private static CanyonRouteNodeDefinition Node(CanyonRouteNodeId id) =>
        CanyonWorldLayout.Nodes.Single(node => node.Id == id);

    private static bool HasPath(CanyonRouteNodeId from, CanyonRouteNodeId to,
        params CanyonRouteNodeId[] required)
    {
        CanyonRouteNodeId current = from;
        foreach (CanyonRouteNodeId next in required.Append(to))
        {
            if (!CanyonWorldLayout.Edges.Any(edge => edge.From == current && edge.To == next &&
                    edge.Gate == CanyonRouteGate.None)) return false;
            current = next;
        }
        return true;
    }

    private static bool HasPathAvoiding(CanyonRouteNodeId from, CanyonRouteNodeId to,
        params CanyonRouteNodeId[] excluded)
    {
        var blocked = new HashSet<CanyonRouteNodeId>(excluded);
        var reached = new HashSet<CanyonRouteNodeId> { from };
        var queue = new Queue<CanyonRouteNodeId>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            CanyonRouteNodeId current = queue.Dequeue();
            foreach (CanyonRouteEdge edge in CanyonWorldLayout.Edges.Where(edge =>
                         edge.From == current && edge.Gate == CanyonRouteGate.None))
            {
                if (blocked.Contains(edge.To) || !reached.Add(edge.To)) continue;
                if (edge.To == to) return true;
                queue.Enqueue(edge.To);
            }
        }
        return false;
    }

    private static void AssertInvalid(
        IReadOnlyList<CanyonRouteNodeDefinition> nodes = null,
        IReadOnlyList<CanyonRouteEdge> edges = null,
        IReadOnlyList<string> stableCheckpointIds = null,
        string expectedMessage = "",
        string message = "")
    {
        IReadOnlyList<string> errors = CanyonRouteGraph.Validate(
            CanyonWorldLayout.DesignBounds,
            CanyonWorldLayout.SectorBounds,
            nodes ?? CanyonWorldLayout.Nodes,
            edges ?? CanyonWorldLayout.Edges,
            CanyonWorldLayout.CheckpointWorldPositions,
            stableCheckpointIds ?? CanyonWorldLayout.StableCheckpointIds,
            CanyonWorldLayout.StableShortcutIds,
            CanyonWorldLayout.StableMechanismIds,
            CanyonWorldLayout.StableDiscoveryIds);
        TestAssert.True(errors.Any(error => error.Contains(expectedMessage,
                StringComparison.OrdinalIgnoreCase)),
            $"{message}; errors={string.Join("; ", errors)}");
    }

    private static void AssertReadOnly<T>(IList<T> values, T replacement, string message)
    {
        try
        {
            values[0] = replacement;
        }
        catch (NotSupportedException)
        {
            return;
        }
        throw new InvalidOperationException($"Assertion failed: {message}");
    }

    private static void AssertSequence<T>(IReadOnlyList<T> actual,
        IReadOnlyList<T> expected, string message)
    {
        TestAssert.Equal(expected.Count, actual.Count, message + " count");
        for (int index = 0; index < expected.Count; index++)
            TestAssert.Equal(expected[index], actual[index], message + $" item {index}");
    }

    private static bool InteriorOverlaps(Rect2 first, Rect2 second) =>
        MathF.Min(first.End.X, second.End.X) - MathF.Max(first.Position.X, second.Position.X) > .01f &&
        MathF.Min(first.End.Y, second.End.Y) - MathF.Max(first.Position.Y, second.Position.Y) > .01f;

    private static bool Contains(Rect2 outer, Rect2 inner) =>
        inner.Position.X >= outer.Position.X && inner.Position.Y >= outer.Position.Y &&
        inner.End.X <= outer.End.X && inner.End.Y <= outer.End.Y;
}
