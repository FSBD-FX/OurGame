using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;

namespace Knight.Tests.Unit;

// Synthetic validation records only: never traversal, operation, Boss or exit evidence.
public static class Task10RouteProofContractTests
{
    public static void Run()
    {
        RawRelativeRejectedBeforeNormalization();
        ApplicableVariantContract();
        CompletionAndRecoveryContract();
        TerminalReceiptContract();
        GD.Print("TASK10_ROUTE_PROOF_CONTRACT_OK groups=4 fixture=true fullRoute=false");
    }

    public static void RunArtifactContract()
    {
        // This boundary must reject invalid evidence, not just return a success
        // path. Removing validation (even if no writer runs) breaks the first case.
        string temp = Task10RouteProofContract.ValidateRawStorageRoot(Path.GetTempPath());
        string root = Path.Combine(temp, "task18-route-result-fixture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            PrologueContinuousRouteResult main = Main();
            RejectArtifact(main with { TerminalProof = null }, "Main", "missing terminal");
            RejectArtifact(main with { TerminalProof = TerminalFixture() with { CompletionEvents = 0 } },
                "Main", "invalid terminal");
            RejectArtifact(main with { BossDefeated = false }, "Main", "invalid completion");
            RejectArtifact(main, "Exploration", "mismatched variant");
            RejectArtifact(main with { Variant = "unknown" }, "unknown", "unknown variant");
            RejectArtifact(Exploration() with { TerminalProof = null }, "Exploration", "missing Exploration terminal");
            foreach (string supplied in new[] { "relative-case-storage", "", "E:case-storage",
                "../case-storage", Path.Combine(root, "child", ".."), Path.GetPathRoot(root) })
            {
                Reject(() => Task10RouteResultArtifact.Persist(main, "Main", supplied, syntheticFixture: true),
                    "RouteResultArtifactContract rejects raw relative/traversal output roots");
                TestAssert.Equal(0, Directory.GetFileSystemEntries(root).Length,
                    "RouteResultArtifactContract invalid path creates no success file");
            }

            AssertArtifactRoundTrip(main with
            {
                MechanismsActivated = ["fixture.observed-mechanism"],
                RecoveryRequests = 1, RecoveryCompletions = 1
            }, "Main");
            File.Delete(Path.Combine(root, "task10-route-result.json"));
            AssertArtifactRoundTrip(Exploration() with
            {
                MechanismsActivated = ["fixture.observed-mechanism"],
                RecoveryRequests = 1, RecoveryCompletions = 1
            }, "Exploration");

            // Inspect the shared serializer's non-fixture branch in memory only.
            // No unit-generated route is ever persisted as actual-route evidence.
            using (JsonDocument actualMetadata = JsonDocument.Parse(
                Task10RouteResultArtifact.ToJson(main, "Main", syntheticFixture: false)))
                AssertArtifactMetadata(actualMetadata.RootElement, syntheticFixture: false);
            GD.Print("TASK18_ROUTE_RESULT_REJECTION_CONTRACT_OK fixture=true fullRoute=false");
            GD.Print("TASK18_ROUTE_RESULT_ARTIFACT_CONTRACT_OK variants=2 fixture=true fullRoute=false");
        }
        finally
        {
            // Unique fixture root has no nested directories. Propagate cleanup
            // failures; never touch actual route storage or default save/settings.
            foreach (string file in Directory.GetFiles(root)) File.Delete(file);
            Directory.Delete(root);
        }

        void RejectArtifact(PrologueContinuousRouteResult route, string variant, string reason)
        {
            bool rejected = false;
            try { Task10RouteResultArtifact.Persist(route, variant, root, syntheticFixture: true); }
            catch (ArgumentException) { rejected = true; }
            catch (InvalidOperationException) { rejected = true; }
            TestAssert.Equal(0, Directory.GetFileSystemEntries(root).Length,
                $"RouteResultArtifactContract leaves no output for {reason}; fixture=true/fullRoute=false");
            TestAssert.True(rejected,
                $"RouteResultArtifactContract rejects {reason} before any output write; fixture=true/fullRoute=false");
        }

        void AssertArtifactRoundTrip(PrologueContinuousRouteResult route, string variant)
        {
            string path = Task10RouteResultArtifact.Persist(route, variant, root, syntheticFixture: true);
            TestAssert.Equal(Path.Combine(root, "task10-route-result.json"), path,
                "RouteResultArtifactContract uses one fixed filename within the fixture root");
            TestAssert.Equal(1, Directory.GetFileSystemEntries(root).Length,
                "RouteResultArtifactContract writes exactly one result file");
            byte[] original = File.ReadAllBytes(path);
            using JsonDocument document = JsonDocument.Parse(original);
            AssertArtifactMetadata(document.RootElement, syntheticFixture: true);
            JsonElement observed = document.RootElement.GetProperty("route");
            TestAssert.Equal(17, observed.EnumerateObject().Count(),
                "RouteResultArtifactContract retains every measured route field");
            TestAssert.Equal(variant, observed.GetProperty("variant").GetString(), "artifact variant round-trip");
            TestAssert.True(observed.GetProperty("startedFromTitle").GetBoolean() &&
                observed.GetProperty("persistentPlayer").GetBoolean() &&
                observed.GetProperty("bossDefeated").GetBoolean() && observed.GetProperty("safeExitReached").GetBoolean(),
                "artifact retains measured start/player/Boss/exit booleans");
            TestAssert.Equal(101UL, observed.GetProperty("playerInstanceId").GetUInt64(), "artifact original player identity");
            TestAssert.Equal("Completed", observed.GetProperty("flowState").GetString(), "artifact flow state");
            TestAssert.Equal(1, observed.GetProperty("recoveryRequests").GetInt32(), "artifact recovery requests");
            TestAssert.Equal(1, observed.GetProperty("recoveryCompletions").GetInt32(), "artifact recovery completions");
            AssertJsonStrings(observed.GetProperty("mechanismsActivated"), ["fixture.observed-mechanism"]);
            AssertJsonStrings(observed.GetProperty("checkpointsActivated"), ["canyon.v2.basin-camp", "canyon.v2.temple-forecourt"]);
            bool mainVariant = variant == "Main";
            AssertJsonStrings(observed.GetProperty("routeNodes"), mainVariant
                ? ["LowCeilingBridge", "StaggeredRift", "ControlledAbyss", "BasinCamp", "GearPistons",
                    "CounterweightLift", "PendulumGallery", "SplitLevelEncounter", "SealedCourtyard", "BossHall", "SafeExit"]
                : ["LowCeilingBridge", "StaggeredRift", "ArchiveSideCave", "FrozenDetour", "BasinCamp", "GearPistons",
                    "SteamValves", "CounterweightLift", "PendulumGallery", "SplitLevelEncounter", "SealedCourtyard", "BossHall", "SafeExit"]);
            AssertJsonStrings(observed.GetProperty("challengeKinds"), mainVariant
                ? ["LowCeilingBridge", "StaggeredRift", "ControlledAbyss", "GearPistons", "CounterweightLift",
                    "PendulumGallery", "SplitLevelEncounter", "SealedCourtyard", "BossHall"]
                : ["LowCeilingBridge", "StaggeredRift", "ArchiveSideCave", "FrozenDetour", "GearPistons", "SteamValves",
                    "CounterweightLift", "PendulumGallery", "SplitLevelEncounter", "SealedCourtyard", "BossHall"]);
            // Existing hand-authored encounter literals are independent of serializer code.
            string[] expectedEncounters = mainVariant ? MainEncounters :
                ["canyon.v2.encounter.low-ceiling-bridge", "canyon.v2.encounter.staggered-rift",
                    "canyon.v2.encounter.gear-pistons", "canyon.v2.encounter.pendulum-gallery",
                    "canyon.v2.encounter.split-level", "canyon.v2.encounter.sealed-courtyard",
                    "canyon.v2.encounter.archive-side-cave", "canyon.v2.encounter.frozen-upper",
                    "canyon.v2.encounter.steam-valves", "canyon.v2.encounter.frozen-lower"];
            AssertJsonStrings(observed.GetProperty("encounterActivations"), expectedEncounters);
            AssertJsonStrings(observed.GetProperty("encounterDefeats"), expectedEncounters);
            AssertJsonStrings(observed.GetProperty("shortcutDirections"), mainVariant ? [] :
                ["BasinCamp->SplitLevelEncounter", "SplitLevelEncounter->BasinCamp"]);
            AssertArtifactTerminal(observed.GetProperty("terminalProof"));

            bool duplicateRejected = false;
            try { Task10RouteResultArtifact.Persist(route with { RecoveryRequests = 2, RecoveryCompletions = 2 },
                variant, root, syntheticFixture: true); }
            catch (IOException) { duplicateRejected = true; }
            TestAssert.True(duplicateRejected, "RouteResultArtifactContract rejects a duplicate output instead of overwriting");
            TestAssert.True(original.SequenceEqual(File.ReadAllBytes(path)),
                "RouteResultArtifactContract duplicate leaves original bytes unchanged");
            TestAssert.Equal(1, Directory.GetFileSystemEntries(root).Length,
                "RouteResultArtifactContract duplicate leaves no extra temporary artifact");
        }
    }

    private static void AssertArtifactMetadata(JsonElement artifact, bool syntheticFixture)
    {
        TestAssert.Equal(7, artifact.EnumerateObject().Count(), "artifact has only explicit metadata and measured route");
        TestAssert.Equal(1, artifact.GetProperty("version").GetInt32(), "artifact version1 boundary");
        TestAssert.Equal("automated", artifact.GetProperty("proofKind").GetString(), "artifact automated proof boundary");
        TestAssert.Equal(syntheticFixture, artifact.GetProperty("syntheticFixture").GetBoolean(),
            "artifact distinguishes synthetic fixtures from actual route selection");
        TestAssert.True(!artifact.GetProperty("humanValidated").GetBoolean() &&
            !artifact.GetProperty("operationCoverageComplete").GetBoolean() &&
            !artifact.GetProperty("fullGoalAccepted").GetBoolean(),
            "artifact never grants human validation, complete operation coverage or full-goal acceptance");
    }

    private static void AssertJsonStrings(JsonElement array, string[] expected) =>
        TestAssert.True(array.EnumerateArray().Select(element => element.GetString()).SequenceEqual(expected),
            "artifact retains literal ordered value arrays without dropped/changed observations");

    private static void AssertArtifactTerminal(JsonElement terminal)
    {
        // Literal receipt expectations, not derived by the serializer under test.
        using JsonDocument expected = JsonDocument.Parse("""
        {
          "playerInstanceId":101,"bossInstanceId":202,"fatalAttackId":303,
          "exposurePhysics":10,"attackPhysics":20,"damagePhysics":21,"defeatedPhysics":21,
          "completionPhysics":100,"heldEndPhysics":160,
          "bossDiedEvents":1,"arenaDefeatedEvents":1,"completionEvents":1,"completionCount":1,
          "rightInputIssueSequence":30,"rightInputSampleSequence":31,
          "completionBody":{"x":620,"y":80,"width":20,"height":20},
          "completionFloor":{"x":0,"y":100,"width":1000,"height":20},
          "completionDoor":{"x":600,"y":0,"width":10,"height":100},
          "supportedAtCompletion":true,"completionFlowState":"Completed",
          "playerPhysicsEnabledAtCompletion":false,"completionPanelVisible":true,
          "savedBossDefeatedBeforeExit":true,"savedBossDefeatedAfterExit":true,
          "savedCompletedBeforeExit":false,"savedCompletedAfterExit":true,
          "checkpointBeforeExit":"True/canyon.v2.temple-forecourt/LostTemple/400/90/True",
          "checkpointAfterExit":"True/canyon.v2.temple-forecourt/LostTemple/400/90/True",
          "heldInputFrameCount":60,
          "completionPosition":{"x":630,"y":90},"heldEndPosition":{"x":630,"y":90},
          "completionVelocity":{"x":0,"y":0},"heldEndVelocity":{"x":0,"y":0},
          "heldEndBody":{"x":620,"y":80,"width":20,"height":20},
          "completionSaveSha256":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
          "heldEndSaveSha256":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        }
        """);
        AssertJsonValue(expected.RootElement, terminal);

        static void AssertJsonValue(JsonElement expectedValue, JsonElement actualValue)
        {
            TestAssert.Equal(expectedValue.ValueKind, actualValue.ValueKind, "artifact primitive value kind round-trip");
            switch (expectedValue.ValueKind)
            {
                case JsonValueKind.Object:
                    TestAssert.Equal(expectedValue.EnumerateObject().Count(), actualValue.EnumerateObject().Count(),
                        "artifact retains all and only terminal/flattened geometry fields");
                    foreach (JsonProperty property in expectedValue.EnumerateObject())
                        AssertJsonValue(property.Value, actualValue.GetProperty(property.Name));
                    break;
                case JsonValueKind.Number:
                    TestAssert.Equal(expectedValue.GetDecimal(), actualValue.GetDecimal(), "artifact literal numeric receipt round-trip");
                    break;
                case JsonValueKind.String:
                    TestAssert.Equal(expectedValue.GetString(), actualValue.GetString(), "artifact literal string receipt round-trip");
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    TestAssert.Equal(expectedValue.GetBoolean(), actualValue.GetBoolean(), "artifact literal boolean receipt round-trip");
                    break;
                default: throw new InvalidOperationException("unexpected literal terminal JSON category");
            }
        }
    }

    private static void RawRelativeRejectedBeforeNormalization()
    {
        foreach (string supplied in new[] { "relative-case-storage", "", "E:case-storage",
            "\\case-storage", "../case-storage", "E:/task10-contract/a/../case-storage" })
            Reject(() => Task10RouteProofContract.ValidateRawStorageRoot(supplied),
                $"RawRelativeRejectedBeforeNormalization rejects untouched raw storage '{supplied}'");
        string absolute = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "task10-contract-absolute"));
        TestAssert.Equal(absolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Task10RouteProofContract.ValidateRawStorageRoot(absolute),
            "RawRelativeRejectedBeforeNormalization accepts an already absolute path without writing it");
    }

    private static void ApplicableVariantContract()
    {
        PrologueContinuousRouteResult main = Main();
        PrologueContinuousRouteResult exploration = Exploration();
        main.AssertComplete("Main");
        exploration.AssertComplete("Exploration");
        // These six common IDs are an example, not six compulsory defeats. The
        // binding fork ruling permits bypassing noncritical ordinary combat.
        var bypassed = exploration with
        {
            EncounterActivations = Without(exploration.EncounterActivations, "canyon.v2.encounter.low-ceiling-bridge"),
            EncounterDefeats = Without(exploration.EncounterDefeats, "canyon.v2.encounter.low-ceiling-bridge")
        };
        bypassed.AssertComplete("Exploration");
        (main with { RouteNodes = main.RouteNodes.Concat(new[] { CanyonRouteNodeId.BasinCamp }).ToArray() })
            .AssertComplete("Main");
        Reject(() => main.AssertComplete("Exploration"), "ApplicableVariantContract rejects mismatched variant");
        Reject(() => (main with { Variant = "" }).AssertComplete(""), "ApplicableVariantContract rejects missing variant");
        Reject(() => (main with { Variant = "unknown" }).AssertComplete("unknown"), "ApplicableVariantContract rejects unknown variant");
        Reject(() => (main with { RouteNodes = main.RouteNodes.Skip(1).ToArray() }).AssertComplete("Main"),
            "ApplicableVariantContract rejects missing required node");
        var wrongOrder = main.RouteNodes.ToArray();
        (wrongOrder[0], wrongOrder[1]) = (wrongOrder[1], wrongOrder[0]);
        Reject(() => (main with { RouteNodes = wrongOrder }).AssertComplete("Main"),
            "ApplicableVariantContract rejects incorrect required node order");
        Reject(() => (main with { ChallengeKinds = main.ChallengeKinds.Skip(1).ToArray() }).AssertComplete("Main"),
            "ApplicableVariantContract rejects missing required challenge kind");
        (main with { ChallengeKinds = main.ChallengeKinds.Reverse().ToArray() }).AssertComplete("Main");
        Reject(() => (main with { ChallengeKinds = main.ChallengeKinds.Append((CanyonChallengeKind)999).ToArray() }).AssertComplete("Main"),
            "ApplicableVariantContract rejects undefined challenge kind; membership is not a temporal trace");
        foreach (string id in MainEncounters)
        {
            Reject(() => (main with { EncounterActivations = Without(main.EncounterActivations, id) }).AssertComplete("Main"),
                $"ApplicableVariantContract Main requires exact activation {id}");
            Reject(() => (main with { EncounterDefeats = Without(main.EncounterDefeats, id) }).AssertComplete("Main"),
                $"ApplicableVariantContract Main requires exact real defeat {id}");
        }
        foreach (string id in OptionalEncounters.Concat(new[] { "canyon.v2.encounter.split-level", "canyon.v2.encounter.sealed-courtyard" }))
        {
            Reject(() => (exploration with { EncounterActivations = Without(exploration.EncounterActivations, id) }).AssertComplete("Exploration"),
                $"ApplicableVariantContract Exploration requires optional/gated activation {id}");
            Reject(() => (exploration with { EncounterDefeats = Without(exploration.EncounterDefeats, id) }).AssertComplete("Exploration"),
                $"ApplicableVariantContract Exploration requires optional/gated defeat {id}");
        }
        Reject(() => (main with { EncounterActivations = main.EncounterActivations.Skip(1).Append(MainEncounters[1]).ToArray() }).AssertComplete("Main"),
            "ApplicableVariantContract rejects duplicate encounter padding for missing exact ID");
        Reject(() => (main with { EncounterDefeats = main.EncounterDefeats.Skip(1).Append("canyon.v2.encounter.unknown").ToArray() }).AssertComplete("Main"),
            "ApplicableVariantContract rejects unknown encounter padding");
        Reject(() => (bypassed with { EncounterDefeats = bypassed.EncounterDefeats.Append("canyon.v2.encounter.low-ceiling-bridge").ToArray() }).AssertComplete("Exploration"),
            "ApplicableVariantContract rejects a claimed defeat without matching activation");
        foreach (string direction in exploration.ShortcutDirections)
            Reject(() => (exploration with { ShortcutDirections = Without(exploration.ShortcutDirections, direction) }).AssertComplete("Exploration"),
                $"ApplicableVariantContract Exploration requires actual shortcut direction {direction}");
    }

    private static void CompletionAndRecoveryContract()
    {
        PrologueContinuousRouteResult main = Main();
        main.AssertComplete("Main");
        (main with { RecoveryRequests = 1, RecoveryCompletions = 1 }).AssertComplete("Main");
        Reject(() => (main with { StartedFromTitle = false }).AssertComplete("Main"), "CompletionAndRecoveryContract requires Title Start");
        Reject(() => (main with { PersistentPlayer = false }).AssertComplete("Main"), "CompletionAndRecoveryContract requires one persistent player");
        foreach (string checkpoint in main.CheckpointsActivated)
            Reject(() => (main with { CheckpointsActivated = Without(main.CheckpointsActivated, checkpoint) }).AssertComplete("Main"),
                $"CompletionAndRecoveryContract requires persisted checkpoint {checkpoint}");
        Reject(() => (main with { BossDefeated = false }).AssertComplete("Main"), "CompletionAndRecoveryContract rejects missing Boss defeat");
        Reject(() => (main with { SafeExitReached = false }).AssertComplete("Main"), "CompletionAndRecoveryContract rejects missing supported exit");
        Reject(() => (main with { FlowState = PrologueGameFlowState.Playing }).AssertComplete("Main"), "CompletionAndRecoveryContract requires Completed");
        Reject(() => (main with { RecoveryRequests = 1, RecoveryCompletions = 0 }).AssertComplete("Main"), "CompletionAndRecoveryContract rejects unfinished recovery");
        Reject(() => (main with { RecoveryRequests = 0, RecoveryCompletions = 1 }).AssertComplete("Main"), "CompletionAndRecoveryContract rejects unpaired recovery completion");
        Reject(() => (main with { RecoveryRequests = -1, RecoveryCompletions = -1 }).AssertComplete("Main"), "CompletionAndRecoveryContract rejects negative recovery counts");
        Reject(() => (main with { RouteNodes = main.RouteNodes.Where(node => node != CanyonRouteNodeId.ControlledAbyss).ToArray() }).AssertComplete("Main"),
            "CompletionAndRecoveryContract incidental Abyss kind/bottom is not Main ordered ControlledAbyss; full descent needs separate physical proof");
        Reject(() => (main with { RouteNodes = main.RouteNodes.Skip(1).Append(CanyonRouteNodeId.BasinCamp).ToArray() }).AssertComplete("Main"),
            "CompletionAndRecoveryContract repeated visits do not replace missing required coverage");
    }

    private static void TerminalReceiptContract()
    {
        // fixture=true/fullRoute=false: these literals test rejection behavior,
        // not genuine traversal, input, death, completion or physical-exit evidence.
        // Removing the applicable terminal predicate must break this first case.
        PrologueContinuousRouteResult main = Main();
        Reject(() => (main with { TerminalProof = null }).AssertComplete("Main"),
            "TerminalReceiptContract rejects missing proof through applicable route contract");
        Reject(() => (main with { PlayerInstanceId = 0UL }).AssertComplete("Main"),
            "TerminalReceiptContract rejects zero original player through applicable route contract");
        Reject(() => (main with { PlayerInstanceId = 102UL }).AssertComplete("Main"),
            "TerminalReceiptContract rejects mismatched original player through applicable route contract");
        Task10TerminalProof valid = TerminalFixture();
        Task10RouteProofContract.AssertTerminal(valid, 101UL);
        (main with { TerminalProof = valid }).AssertComplete("Main");
        Exploration().AssertComplete("Exploration");
        // Actual callback and held boundaries permit equal damage/Defeated ticks
        // and a held end 61 ticks after completion, without a crossing tick field.
        Task10RouteProofContract.AssertTerminal(valid with { HeldEndPhysics = 161UL }, 101UL);
        Task10RouteProofContract.AssertTerminal(valid with
        { AttackPhysics = 20UL, DamagePhysics = 20UL, DefeatedPhysics = 20UL }, 101UL);
        Reject(() => Task10RouteProofContract.AssertTerminal(null, 101UL),
            "TerminalReceiptContract rejects null standalone proof");
        Reject(() => Task10RouteProofContract.AssertTerminal(valid, 0UL),
            "TerminalReceiptContract rejects zero expected player identity");
        Reject(() => Task10RouteProofContract.AssertTerminal(valid, 102UL),
            "TerminalReceiptContract rejects mismatched expected player identity");
        foreach ((string name, Task10TerminalProof proof) in new (string, Task10TerminalProof)[]
        {
            ("zero player", valid with { PlayerInstanceId = 0UL }),
            ("mismatched player", valid with { PlayerInstanceId = 102UL }),
            ("zero Boss", valid with { BossInstanceId = 0UL }),
            ("zero attack", valid with { FatalAttackId = 0UL }),
            ("zero exposure frame", valid with { ExposurePhysics = 0UL }),
            ("zero attack frame", valid with { AttackPhysics = 0UL }),
            ("zero damage frame", valid with { DamagePhysics = 0UL }),
            ("zero Defeated frame", valid with { DefeatedPhysics = 0UL }),
            ("zero completion frame", valid with { CompletionPhysics = 0UL }),
            ("zero heldend frame", valid with { HeldEndPhysics = 0UL }),
            ("exposure after attack", valid with { ExposurePhysics = 21UL }),
            ("attack after damage", valid with { AttackPhysics = 22UL }),
            ("damage after Defeated", valid with { DamagePhysics = 22UL }),
            ("Defeated equals completion", valid with { DefeatedPhysics = 100UL }),
            ("Defeated after completion", valid with { DefeatedPhysics = 101UL }),
            ("heldend before completion", valid with { HeldEndPhysics = 99UL }),
            ("heldend too early", valid with { HeldEndPhysics = 159UL }),
            ("zero right issue", valid with { RightInputIssueSequence = 0UL }),
            ("zero right sample", valid with { RightInputSampleSequence = 0UL }),
            ("same right sample", valid with { RightInputSampleSequence = 30UL }),
            ("older right sample", valid with { RightInputSampleSequence = 29UL }),
            ("unsupported completion", valid with { SupportedAtCompletion = false }),
            ("door straddling", valid with { CompletionBody = new Rect2(609f, 80f, 20f, 20f) }),
            ("door face equality", valid with { CompletionBody = new Rect2(610f, 80f, 20f, 20f) }),
            ("outside floor left", valid with { CompletionFloor = new Rect2(621f, 100f, 379f, 20f) }),
            ("outside floor right", valid with { CompletionFloor = new Rect2(0f, 100f, 639f, 20f) }),
            ("wrong flow", valid with { CompletionFlowState = PrologueGameFlowState.Playing }),
            ("enabled physics", valid with { PlayerPhysicsEnabledAtCompletion = true }),
            ("hidden panel", valid with { CompletionPanelVisible = false }),
            ("Boss not saved before", valid with { SavedBossDefeatedBeforeExit = false }),
            ("Boss not saved after", valid with { SavedBossDefeatedAfterExit = false }),
            ("already Completed before", valid with { SavedCompletedBeforeExit = true }),
            ("not Completed after", valid with { SavedCompletedAfterExit = false }),
            ("checkpoint drift", valid with { CheckpointAfterExit = "another-checkpoint" }),
            ("missing checkpoint", valid with { CheckpointBeforeExit = "", CheckpointAfterExit = "" }),
            ("59 held waits", valid with { HeldInputFrameCount = 59 }),
            ("61 held waits", valid with { HeldInputFrameCount = 61 }),
            ("held position drift", valid with { HeldEndPosition = new Vector2(631f, 90f) }),
            ("held velocity drift", valid with { HeldEndVelocity = new Vector2(1f, 0f) }),
            ("held body drift", valid with { HeldEndBody = new Rect2(621f, 80f, 20f, 20f) }),
            ("nonfinite position", valid with { CompletionPosition = new Vector2(float.NaN, 90f) }),
            ("nonfinite velocity", valid with { CompletionVelocity = new Vector2(float.PositiveInfinity, 0f) }),
            ("save drift", valid with { HeldEndSaveSha256 = new string('B', 64) }),
            ("missing save hashes", valid with { CompletionSaveSha256 = "", HeldEndSaveSha256 = "" }),
            ("malformed equal save hashes", valid with { CompletionSaveSha256 = "not-a-hash", HeldEndSaveSha256 = "not-a-hash" })
        })
            Reject(() => Task10RouteProofContract.AssertTerminal(proof, 101UL),
                $"TerminalReceiptContract rejects {name}; fixture=true/fullRoute=false");
        foreach (int count in new[] { -1, 0, 2 })
        {
            RejectTerminal(valid with { BossDiedEvents = count }, $"BossDied count {count}");
            RejectTerminal(valid with { ArenaDefeatedEvents = count }, $"arena count {count}");
            RejectTerminal(valid with { CompletionEvents = count }, $"completion events {count}");
            RejectTerminal(valid with { CompletionCount = count }, $"completion count {count}");
        }
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, 0f, -1f })
        {
            foreach (string rectangle in new[] { "body", "floor", "door", "heldbody" })
            {
                RejectRectangle(rectangle, new Rect2(620f, 80f, invalid, 20f), $"width {invalid}");
                RejectRectangle(rectangle, new Rect2(620f, 80f, 20f, invalid), $"height {invalid}");
                if (float.IsFinite(invalid)) continue;
                RejectRectangle(rectangle, new Rect2(invalid, 80f, 20f, 20f), $"positionX {invalid}");
                RejectRectangle(rectangle, new Rect2(620f, invalid, 20f, 20f), $"positionY {invalid}");
            }
        }
        void RejectRectangle(string rectangle, Rect2 value, string reason)
        {
            Task10TerminalProof proof = rectangle switch
            {
                "body" => valid with { CompletionBody = value },
                "floor" => valid with { CompletionFloor = value },
                "door" => valid with { CompletionDoor = value },
                _ => valid with { HeldEndBody = value }
            };
            RejectTerminal(proof, $"{rectangle} {reason}");
        }
        void RejectTerminal(Task10TerminalProof proof, string reason) =>
            Reject(() => Task10RouteProofContract.AssertTerminal(proof, 101UL),
                $"TerminalReceiptContract rejects {reason}; fixture=true/fullRoute=false");
    }

    private static Task10TerminalProof TerminalFixture() => new()
    {
        PlayerInstanceId = 101UL, BossInstanceId = 202UL, FatalAttackId = 303UL,
        ExposurePhysics = 10UL, AttackPhysics = 20UL, DamagePhysics = 21UL, DefeatedPhysics = 21UL,
        CompletionPhysics = 100UL, HeldEndPhysics = 160UL,
        BossDiedEvents = 1, ArenaDefeatedEvents = 1, CompletionEvents = 1, CompletionCount = 1,
        RightInputIssueSequence = 30UL, RightInputSampleSequence = 31UL,
        CompletionBody = new Rect2(620f, 80f, 20f, 20f), CompletionFloor = new Rect2(0f, 100f, 1000f, 20f),
        CompletionDoor = new Rect2(600f, 0f, 10f, 100f), SupportedAtCompletion = true,
        CompletionFlowState = PrologueGameFlowState.Completed, PlayerPhysicsEnabledAtCompletion = false,
        CompletionPanelVisible = true, SavedBossDefeatedBeforeExit = true, SavedBossDefeatedAfterExit = true,
        SavedCompletedBeforeExit = false, SavedCompletedAfterExit = true,
        CheckpointBeforeExit = "True/canyon.v2.temple-forecourt/LostTemple/400/90/True",
        CheckpointAfterExit = "True/canyon.v2.temple-forecourt/LostTemple/400/90/True",
        HeldInputFrameCount = 60, CompletionPosition = new Vector2(630f, 90f),
        HeldEndPosition = new Vector2(630f, 90f), CompletionVelocity = Vector2.Zero, HeldEndVelocity = Vector2.Zero,
        HeldEndBody = new Rect2(620f, 80f, 20f, 20f),
        CompletionSaveSha256 = new string('A', 64), HeldEndSaveSha256 = new string('A', 64)
    };

    private static readonly string[] MainEncounters =
    [
        "canyon.v2.encounter.low-ceiling-bridge", "canyon.v2.encounter.staggered-rift",
        "canyon.v2.encounter.controlled-abyss", "canyon.v2.encounter.gear-pistons",
        "canyon.v2.encounter.counterweight-lift", "canyon.v2.encounter.pendulum-gallery",
        "canyon.v2.encounter.split-level", "canyon.v2.encounter.sealed-courtyard"
    ];
    private static readonly string[] OptionalEncounters =
    [
        "canyon.v2.encounter.archive-side-cave", "canyon.v2.encounter.frozen-upper",
        "canyon.v2.encounter.steam-valves", "canyon.v2.encounter.frozen-lower"
    ];
    private static PrologueContinuousRouteResult Main() => Finished("Main") with
    {
        RouteNodes = [CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift,
            CanyonRouteNodeId.ControlledAbyss, CanyonRouteNodeId.BasinCamp, CanyonRouteNodeId.GearPistons,
            CanyonRouteNodeId.CounterweightLift, CanyonRouteNodeId.PendulumGallery,
            CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.SealedCourtyard,
            CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit],
        ChallengeKinds = [CanyonChallengeKind.LowCeilingBridge, CanyonChallengeKind.StaggeredRift,
            CanyonChallengeKind.ControlledAbyss, CanyonChallengeKind.GearPistons,
            CanyonChallengeKind.CounterweightLift, CanyonChallengeKind.PendulumGallery,
            CanyonChallengeKind.SplitLevelEncounter, CanyonChallengeKind.SealedCourtyard, CanyonChallengeKind.BossHall],
        EncounterActivations = MainEncounters, EncounterDefeats = MainEncounters,
        ShortcutDirections = Array.Empty<string>()
    };
    private static PrologueContinuousRouteResult Exploration()
    {
        string[] encounters = new[] { "canyon.v2.encounter.low-ceiling-bridge", "canyon.v2.encounter.staggered-rift",
            "canyon.v2.encounter.gear-pistons", "canyon.v2.encounter.pendulum-gallery",
            "canyon.v2.encounter.split-level", "canyon.v2.encounter.sealed-courtyard" }.Concat(OptionalEncounters).ToArray();
        return Finished("Exploration") with
        {
            RouteNodes = [CanyonRouteNodeId.LowCeilingBridge, CanyonRouteNodeId.StaggeredRift,
                CanyonRouteNodeId.ArchiveSideCave, CanyonRouteNodeId.FrozenDetour, CanyonRouteNodeId.BasinCamp,
                CanyonRouteNodeId.GearPistons, CanyonRouteNodeId.SteamValves, CanyonRouteNodeId.CounterweightLift,
                CanyonRouteNodeId.PendulumGallery, CanyonRouteNodeId.SplitLevelEncounter, CanyonRouteNodeId.SealedCourtyard,
                CanyonRouteNodeId.BossHall, CanyonRouteNodeId.SafeExit],
            ChallengeKinds = [CanyonChallengeKind.LowCeilingBridge, CanyonChallengeKind.StaggeredRift,
                CanyonChallengeKind.ArchiveSideCave, CanyonChallengeKind.FrozenDetour, CanyonChallengeKind.GearPistons,
                CanyonChallengeKind.SteamValves, CanyonChallengeKind.CounterweightLift, CanyonChallengeKind.PendulumGallery,
                CanyonChallengeKind.SplitLevelEncounter, CanyonChallengeKind.SealedCourtyard, CanyonChallengeKind.BossHall],
            EncounterActivations = encounters, EncounterDefeats = encounters,
            ShortcutDirections = ["BasinCamp->SplitLevelEncounter", "SplitLevelEncounter->BasinCamp"]
        };
    }
    private static PrologueContinuousRouteResult Finished(string variant) => new()
    {
        Variant = variant, StartedFromTitle = true, PersistentPlayer = true,
        PlayerInstanceId = 101UL, TerminalProof = TerminalFixture(),
        CheckpointsActivated = ["canyon.v2.basin-camp", "canyon.v2.temple-forecourt"],
        BossDefeated = true, SafeExitReached = true, FlowState = PrologueGameFlowState.Completed
    };
    private static string[] Without(System.Collections.Generic.IReadOnlyList<string> values, string removed) =>
        values.Where(value => value != removed).ToArray();
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); }
        catch (ArgumentException) { rejected = true; }
        catch (InvalidOperationException) { rejected = true; }
        TestAssert.True(rejected, message);
    }
}
