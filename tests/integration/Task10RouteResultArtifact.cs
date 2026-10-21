using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;

namespace Knight.Tests.Integration;

// Test-only value boundary. Synthetic fixtures are never actual route evidence;
// automated actual results never grant human/full-operation/full-goal acceptance.
public static class Task10RouteResultArtifact
{
    public static string Persist(PrologueContinuousRouteResult route, string expectedVariant,
        string rawStorageRoot, bool syntheticFixture, Action<string> diagnostic = null)
    {
        diagnostic?.Invoke("PERSIST_ENTRY");
        // Validate and serialize completely before opening any success output.
        diagnostic?.Invoke("SERIALIZATION_BEGIN");
        byte[] bytes = Encoding.UTF8.GetBytes(ToJson(route, expectedVariant, syntheticFixture));
        diagnostic?.Invoke("SERIALIZATION_END");
        string root = Task10RouteProofContract.ValidateRawStorageRoot(rawStorageRoot);
        // Existing normalization trims separators: a drive root must not become
        // a drive-relative destination such as E:task10-route-result.json.
        if (!Path.IsPathFullyQualified(root))
            throw new ArgumentException("route result storage must remain fully absolute", nameof(rawStorageRoot));
        AssertNoReparseAncestors(root);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException("route result requires existing case storage");
        string path = Path.Combine(root, "task10-route-result.json");
        AssertNoReparseAncestors(path);
        FileStream stream = null;
        bool created = false;
        try
        {
            // Never truncate an earlier result, including after a rejected duplicate.
            stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);
            created = true;
            stream.Write(bytes);
            diagnostic?.Invoke("FLUSH_BEGIN");
            stream.Flush(flushToDisk: true);
            diagnostic?.Invoke("FLUSH_END");
            stream.Dispose();
            stream = null;
            return path;
        }
        catch (Exception writeError)
        {
            try
            {
                stream?.Dispose();
                if (created) File.Delete(path); // Only this invocation's incomplete file.
            }
            catch (Exception cleanupError) { throw new AggregateException(writeError, cleanupError); }
            throw;
        }
    }

    public static string ToJson(PrologueContinuousRouteResult route, string expectedVariant, bool syntheticFixture)
    {
        if (route == null) throw new ArgumentNullException(nameof(route));
        Task10RouteProofContract.AssertApplicable(route, expectedVariant);
        // Applicable already calls AssertTerminal; retain that immutable gate.
        Task10TerminalProof terminal = route.TerminalProof;
        var value = new
        {
            version = 1,
            proofKind = "automated",
            syntheticFixture,
            humanValidated = false,
            operationCoverageComplete = false,
            fullGoalAccepted = false,
            route = new
            {
                variant = route.Variant,
                startedFromTitle = route.StartedFromTitle,
                persistentPlayer = route.PersistentPlayer,
                playerInstanceId = route.PlayerInstanceId,
                routeNodes = route.RouteNodes.Select(node => node.ToString()).ToArray(),
                challengeKinds = route.ChallengeKinds.Select(kind => kind.ToString()).ToArray(),
                encounterActivations = route.EncounterActivations.ToArray(),
                encounterDefeats = route.EncounterDefeats.ToArray(),
                mechanismsActivated = route.MechanismsActivated.ToArray(),
                shortcutDirections = route.ShortcutDirections.ToArray(),
                checkpointsActivated = route.CheckpointsActivated.ToArray(),
                bossDefeated = route.BossDefeated,
                safeExitReached = route.SafeExitReached,
                flowState = route.FlowState.ToString(),
                recoveryRequests = route.RecoveryRequests,
                recoveryCompletions = route.RecoveryCompletions,
                terminalProof = new
                {
                    playerInstanceId = terminal.PlayerInstanceId,
                    bossInstanceId = terminal.BossInstanceId,
                    fatalAttackId = terminal.FatalAttackId,
                    exposurePhysics = terminal.ExposurePhysics,
                    attackPhysics = terminal.AttackPhysics,
                    damagePhysics = terminal.DamagePhysics,
                    defeatedPhysics = terminal.DefeatedPhysics,
                    completionPhysics = terminal.CompletionPhysics,
                    heldEndPhysics = terminal.HeldEndPhysics,
                    bossDiedEvents = terminal.BossDiedEvents,
                    arenaDefeatedEvents = terminal.ArenaDefeatedEvents,
                    completionEvents = terminal.CompletionEvents,
                    completionCount = terminal.CompletionCount,
                    rightInputIssueSequence = terminal.RightInputIssueSequence,
                    rightInputSampleSequence = terminal.RightInputSampleSequence,
                    completionBody = Rectangle(terminal.CompletionBody),
                    completionFloor = Rectangle(terminal.CompletionFloor),
                    completionDoor = Rectangle(terminal.CompletionDoor),
                    supportedAtCompletion = terminal.SupportedAtCompletion,
                    completionFlowState = terminal.CompletionFlowState.ToString(),
                    playerPhysicsEnabledAtCompletion = terminal.PlayerPhysicsEnabledAtCompletion,
                    completionPanelVisible = terminal.CompletionPanelVisible,
                    savedBossDefeatedBeforeExit = terminal.SavedBossDefeatedBeforeExit,
                    savedBossDefeatedAfterExit = terminal.SavedBossDefeatedAfterExit,
                    savedCompletedBeforeExit = terminal.SavedCompletedBeforeExit,
                    savedCompletedAfterExit = terminal.SavedCompletedAfterExit,
                    checkpointBeforeExit = terminal.CheckpointBeforeExit,
                    checkpointAfterExit = terminal.CheckpointAfterExit,
                    heldInputFrameCount = terminal.HeldInputFrameCount,
                    completionPosition = Position(terminal.CompletionPosition),
                    heldEndPosition = Position(terminal.HeldEndPosition),
                    completionVelocity = Position(terminal.CompletionVelocity),
                    heldEndVelocity = Position(terminal.HeldEndVelocity),
                    heldEndBody = Rectangle(terminal.HeldEndBody),
                    completionSaveSha256 = terminal.CompletionSaveSha256,
                    heldEndSaveSha256 = terminal.HeldEndSaveSha256
                }
            }
        };
        return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object Position(Vector2 vector) => new { x = vector.X, y = vector.Y };
    private static object Rectangle(Rect2 rectangle) => new
    {
        x = rectangle.Position.X, y = rectangle.Position.Y,
        width = rectangle.Size.X, height = rectangle.Size.Y
    };

    private static void AssertNoReparseAncestors(string path)
    {
        for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
        {
            FileAttributes attributes;
            try { attributes = File.GetAttributes(current); }
            catch (FileNotFoundException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("route result rejects reparse targets or ancestors", nameof(path));
        }
    }
}
