using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Pure record validation only: never proof of actual traversal or operations.
public static class Task10RouteProofContract
{
    public static void AssertTerminal(Task10TerminalProof proof, ulong expectedPlayerInstanceId)
    {
        if (proof == null) throw new ArgumentNullException(nameof(proof));
        TestAssert.True(expectedPlayerInstanceId != 0 && proof.PlayerInstanceId != 0 &&
            proof.PlayerInstanceId == expectedPlayerInstanceId && proof.BossInstanceId != 0 && proof.FatalAttackId != 0,
            "terminal receipt retains the original nonzero player, Boss and fatal attack identities");
        TestAssert.True(proof.ExposurePhysics != 0 && proof.AttackPhysics != 0 && proof.DamagePhysics != 0 &&
            proof.DefeatedPhysics != 0 && proof.CompletionPhysics != 0 && proof.HeldEndPhysics != 0 &&
            proof.ExposurePhysics <= proof.AttackPhysics && proof.AttackPhysics <= proof.DamagePhysics &&
            proof.DamagePhysics <= proof.DefeatedPhysics && proof.DefeatedPhysics < proof.CompletionPhysics &&
            proof.CompletionPhysics <= proof.HeldEndPhysics && proof.HeldEndPhysics - proof.CompletionPhysics >= 60UL,
            "terminal receipt orders actual exposure/attack/damage/Defeated/completion/held-end frames");
        TestAssert.True(proof.BossDiedEvents == 1 && proof.ArenaDefeatedEvents == 1 &&
            proof.CompletionEvents == 1 && proof.CompletionCount == 1,
            "terminal receipt has one actual Boss death, arena defeat, completion event and completion count");
        TestAssert.True(proof.RightInputIssueSequence != 0 &&
            proof.RightInputSampleSequence > proof.RightInputIssueSequence,
            "terminal receipt has a fresh actual right-input sample after issue");
        TestAssert.True(FinitePositive(proof.CompletionBody) && FinitePositive(proof.CompletionFloor) &&
            FinitePositive(proof.CompletionDoor) && FinitePositive(proof.HeldEndBody),
            "terminal receipt has finite positive actual body/floor/door rectangle geometry");
        TestAssert.True(proof.SupportedAtCompletion &&
            proof.CompletionBody.Position.X > proof.CompletionDoor.End.X &&
            proof.CompletionBody.Position.X >= proof.CompletionFloor.Position.X &&
            proof.CompletionBody.End.X <= proof.CompletionFloor.End.X,
            "terminal completion body is supported, strictly beyond the door face and wholly within the floor");
        TestAssert.True(proof.CompletionFlowState == PrologueGameFlowState.Completed &&
            !proof.PlayerPhysicsEnabledAtCompletion && proof.CompletionPanelVisible,
            "terminal completion retains Completed flow, disabled player physics and visible ordinary panel");
        TestAssert.True(proof.SavedBossDefeatedBeforeExit && proof.SavedBossDefeatedAfterExit &&
            !proof.SavedCompletedBeforeExit && proof.SavedCompletedAfterExit,
            "terminal receipt retains the actual Boss-saved and Completed false-to-true disk transition");
        TestAssert.True(!string.IsNullOrEmpty(proof.CheckpointBeforeExit) &&
            string.Equals(proof.CheckpointBeforeExit, proof.CheckpointAfterExit, StringComparison.Ordinal),
            "terminal receipt retains its nonempty unchanged checkpoint signature");
        TestAssert.Equal(60, proof.HeldInputFrameCount, "terminal receipt retains exactly60 actual held-input waits");
        TestAssert.True(proof.CompletionPosition.IsFinite() && proof.HeldEndPosition.IsFinite() &&
            proof.CompletionVelocity.IsFinite() && proof.HeldEndVelocity.IsFinite() &&
            proof.CompletionPosition == proof.HeldEndPosition && proof.CompletionVelocity == proof.HeldEndVelocity &&
            proof.CompletionBody == proof.HeldEndBody,
            "terminal held60 retains the actual unchanged finite completion pose, velocity and body");
        TestAssert.True(IsSha256(proof.CompletionSaveSha256) && IsSha256(proof.HeldEndSaveSha256) &&
            string.Equals(proof.CompletionSaveSha256, proof.HeldEndSaveSha256, StringComparison.Ordinal),
            "terminal held60 retains matching actual completed-save byte fingerprints");
    }

    private static bool FinitePositive(Godot.Rect2 rectangle) => rectangle.Position.IsFinite() &&
        rectangle.Size.IsFinite() && rectangle.End.IsFinite() && rectangle.Size.X > 0f && rectangle.Size.Y > 0f;

    private static bool IsSha256(string value) => value != null && value.Length == 64 &&
        value.All(character => character is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f');

    public static string ValidateRawStorageRoot(string supplied)
    {
        if (string.IsNullOrEmpty(supplied) || !Path.IsPathFullyQualified(supplied))
            throw new ArgumentException("raw storage root must already be fully absolute", nameof(supplied));
        if (supplied.Split(new[] { '\\', '/' }).Any(segment => segment is "." or ".."))
            throw new ArgumentException("raw storage root must not contain traversal segments", nameof(supplied));
        return Path.GetFullPath(supplied)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static void AssertApplicable(PrologueContinuousRouteResult route, string expectedVariant)
    {
        TestAssert.True((expectedVariant is "Main" or "Exploration") && (route.Variant is "Main" or "Exploration"),
            "route contract requires a known explicit expected and recorded variant");
        TestAssert.Equal(expectedVariant, route.Variant,
            "route result retains its explicit forward-fork variant identity");
        TestAssert.True(route.StartedFromTitle && route.PersistentPlayer,
            "full route begins at normal Title -> Start and keeps one PlayerRoot instance");
        bool main = expectedVariant == "Main";
        TestAssert.True(route.RouteNodes.All(node => Enum.IsDefined(typeof(CanyonRouteNodeId), node)) &&
            route.ChallengeKinds.All(kind => Enum.IsDefined(typeof(CanyonChallengeKind), kind)),
            "route evidence contains only defined node and challenge identities");
        TestAssert.True(HasOrderedSubsequence(route.RouteNodes, main ? MainNodes : ExplorationNodes),
            "route visits contain the applicable ordered forward-fork subsequence");
        TestAssert.True((main ? MainKinds : ExplorationKinds).All(route.ChallengeKinds.Contains),
            "route evidence contains every applicable challenge kind; kinds are membership, not a temporal trace");
        AssertKnownEncounters(route.EncounterActivations);
        AssertKnownEncounters(route.EncounterDefeats);
        TestAssert.True(route.EncounterDefeats.All(route.EncounterActivations.Contains),
            "every claimed encounter defeat has a matching activation");
        string[] requiredEncounters = main ? MainEncounters : ExplorationEncounters;
        TestAssert.True(requiredEncounters.All(route.EncounterActivations.Contains) &&
            requiredEncounters.All(route.EncounterDefeats.Contains),
            "route activates and defeats every exact applicable encounter group");
        if (!main)
            TestAssert.True(route.ShortcutDirections.Contains("BasinCamp->SplitLevelEncounter") &&
                route.ShortcutDirections.Contains("SplitLevelEncounter->BasinCamp"),
                "Exploration traverses the upper-basin shortcut in both directions");
        TestAssert.True(route.CheckpointsActivated.Contains("canyon.v2.basin-camp") &&
            route.CheckpointsActivated.Contains("canyon.v2.temple-forecourt"),
            "both authored checkpoints activate and persist before recovery/Continue proofs");
        TestAssert.True(route.BossDefeated && route.SafeExitReached &&
            route.FlowState == PrologueGameFlowState.Completed,
            "real Boss defeat is followed by physical SafeExit before completion");
        TestAssert.True(route.RecoveryRequests >= 0 && route.RecoveryCompletions >= 0,
            "recovery transaction counts are nonnegative");
        TestAssert.Equal(route.RecoveryRequests, route.RecoveryCompletions,
            "every measured recovery transaction completes exactly once");
        AssertTerminal(route.TerminalProof, route.PlayerInstanceId);
    }

    private static bool HasOrderedSubsequence(IReadOnlyList<CanyonRouteNodeId> visits,
        IReadOnlyList<CanyonRouteNodeId> required)
    {
        int next = 0;
        foreach (CanyonRouteNodeId visit in visits)
            if (next < required.Count && visit == required[next]) next++;
        return next == required.Count;
    }

    private static void AssertKnownEncounters(IReadOnlyList<string> ids)
    {
        TestAssert.True(ids.All(id => MainEncounters.Contains(id) || IsBranchEncounter(id)) &&
            ids.Distinct(StringComparer.Ordinal).Count() == ids.Count,
            "encounter evidence uses known, nonduplicated authored IDs");
    }

    private static readonly CanyonRouteNodeId[] MainNodes =
    [
        CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.ControlledAbyss,
        CanyonRouteNodeId.BasinCamp, CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.CounterweightLift,
        CanyonRouteNodeId.PendulumGallery, CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.SealedCourtyard,
        CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit
    ];
    private static readonly CanyonRouteNodeId[] ExplorationNodes =
    [
        CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift, CanyonRouteNodeId.ArchiveSideCave,
        CanyonRouteNodeId.FrozenDetour, CanyonRouteNodeId.BasinCamp, CanyonRouteNodeId.GearPistons,
        CanyonRouteNodeId.SteamValves, CanyonRouteNodeId.CounterweightLift, CanyonRouteNodeId.PendulumGallery,
        CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.SealedCourtyard, CanyonRouteNodeId.BossHall,
        CanyonRouteNodeId.SafeExit
    ];
    private static readonly CanyonChallengeKind[] MainKinds =
    [
        CanyonChallengeKind.LowCeilingBridge, CanyonChallengeKind.StaggeredRift, CanyonChallengeKind.ControlledAbyss,
        CanyonChallengeKind.GearPistons, CanyonChallengeKind.CounterweightLift, CanyonChallengeKind.PendulumGallery,
        CanyonChallengeKind.SplitLevelEncounter, CanyonChallengeKind.SealedCourtyard, CanyonChallengeKind.BossHall
    ];
    private static readonly CanyonChallengeKind[] ExplorationKinds =
    [
        CanyonChallengeKind.LowCeilingBridge, CanyonChallengeKind.StaggeredRift, CanyonChallengeKind.ArchiveSideCave,
        CanyonChallengeKind.FrozenDetour, CanyonChallengeKind.GearPistons, CanyonChallengeKind.SteamValves,
        CanyonChallengeKind.CounterweightLift, CanyonChallengeKind.PendulumGallery,
        CanyonChallengeKind.SplitLevelEncounter, CanyonChallengeKind.SealedCourtyard, CanyonChallengeKind.BossHall
    ];
    private static readonly string[] MainEncounters =
    [
        "canyon.v2.encounter.low-ceiling-bridge", "canyon.v2.encounter.staggered-rift",
        "canyon.v2.encounter.controlled-abyss", "canyon.v2.encounter.gear-pistons",
        "canyon.v2.encounter.counterweight-lift", "canyon.v2.encounter.pendulum-gallery",
        "canyon.v2.encounter.split-level", "canyon.v2.encounter.sealed-courtyard"
    ];
    private static readonly string[] ExplorationEncounters =
    [
        "canyon.v2.encounter.archive-side-cave", "canyon.v2.encounter.frozen-upper",
        "canyon.v2.encounter.steam-valves", "canyon.v2.encounter.frozen-lower",
        "canyon.v2.encounter.split-level", "canyon.v2.encounter.sealed-courtyard"
    ];

    private static bool IsBranchEncounter(string id) => id is
        "canyon.v2.encounter.archive-side-cave" or
        "canyon.v2.encounter.frozen-upper" or
        "canyon.v2.encounter.steam-valves" or
        "canyon.v2.encounter.frozen-lower";
}
