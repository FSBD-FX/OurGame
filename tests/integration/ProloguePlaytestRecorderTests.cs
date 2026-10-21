using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class ProloguePlaytestRecorderTests
{
    public static async Task Run(SceneTree tree) =>
        await Task7CaseRegistry.Execute(Task7Cases(tree));

    internal static async Task RunTask5FallContextContracts(SceneTree tree)
    {
        await RecoveryRequestedSamplesCurrentInputBeforeReset(tree);
        await PlayerInputSequenceAdvancesOnlyOnPhysicsFrames(tree);
        InputFrameSequenceDoesNotDuplicateJustPressedEdges();
        InputSamplerKeepsOnlyStableEdgesAndTracksReleases();
        await GroundedSupportUsesTheColliderScenePath(tree);
    }

    internal static async Task RunTask10TelemetryContracts(SceneTree tree)
    {
        await RecorderBaselinesRestoredStateAndTracksRealLifecycleEdges(tree);
        ShortcutTelemetryRequiresAnUninterruptedOpenCorridor();
    }

    private static async Task RecorderBaselinesRestoredStateAndTracksRealLifecycleEdges(
        SceneTree tree)
    {
        var root = new Node2D { Name = "Task10TelemetryRoot" };
        var actors = new Node2D { Name = "Actors" };
        var player = new Node2D { Name = "Player", Position = new Vector2(1000f, 1000f) };
        player.AddToGroup("player");
        var recorder = new ProloguePlaytestRecorder { Name = "Recorder" };
        EventInfo nodeAdded = typeof(SceneTree).GetEvent(nameof(SceneTree.NodeAdded))
            ?? throw new InvalidOperationException("telemetry fixture cannot find SceneTree.NodeAdded");
        Delegate recorderNodeAddedHandler = null;
        SceneTree.NodeAddedEventHandler restoreBeforeRecorder = null;
        var restoredProgress = new PrologueProgress();
        const string restoredId = "canyon.v2.encounter.telemetry-restored";
        restoredProgress.ActivateMechanism(restoredId);
        CanyonEncounterGroup restored = NewTelemetryEncounter(restoredId, actors,
            gatesProgression: true);
        restored.ApplyProgress(restoredProgress);
        TestAssert.True(restored.IsComplete,
            "restored encounter fixture is genuinely complete before recorder baseline");
        var restoredWinch = new CanyonShortcutWinch
        {
            Name = "RestoredWinch", ShortcutId = "canyon.v2.telemetry-restored-winch"
        };
        restoredWinch.TryReceiveThread(default);
        root.AddChild(actors);
        root.AddChild(player);
        root.AddChild(restored);
        root.AddChild(restoredWinch);
        root.AddChild(recorder);
        tree.Root.AddChild(root);
        var session = new ProloguePlaytestSession("task10-recorder-lifecycle",
            "2026-09-21T00:00:00Z", "AutomatedFixture");
        try
        {
            await Frames(tree, 2);
            recorder.ConfigureOrderedTelemetryForTest(session, root);
            restoreBeforeRecorder = node =>
            {
                if (node is CanyonShortcutWinch winch &&
                    winch.ShortcutId.ToString() == "canyon.v2.telemetry-delayed-restored")
                {
                    var progress = new PrologueProgress();
                    progress.OpenShortcut(winch.ShortcutId.ToString());
                    winch.ApplyProgress(progress);
                }
            };
            tree.NodeAdded += restoreBeforeRecorder;
            recorderNodeAddedHandler = Delegate.CreateDelegate(nodeAdded.EventHandlerType,
                recorder, RequiredMethod(typeof(ProloguePlaytestRecorder), "OnTelemetryNodeAdded"));
            nodeAdded.AddEventHandler(tree, recorderNodeAddedHandler);
            recorder.ObserveOrderedTelemetryForTest();
            ProloguePlaytestReport baseline = session.BuildReport(PrologueSectorId.CanyonDescent,
                PrologueObjective.ReachBasin, "2026-09-21T00:00:01Z");
            TestAssert.Equal(0, baseline.Encounters.Count,
                "restored completed encounter state is baseline, not a current-run defeat");
            TestAssert.Equal(0, baseline.Mechanisms.Count,
                "restored open mechanism state is baseline, not a current-run activation");

            var delayedParent = new Node2D { Name = "DelayedSector" };
            var delayedRestoredWinch = new CanyonShortcutWinch
            {
                Name = "DelayedRestoredWinch",
                ShortcutId = "canyon.v2.telemetry-delayed-restored"
            };
            delayedParent.AddChild(delayedRestoredWinch);
            root.AddChild(delayedParent);
            await Frames(tree, 1);
            recorder.ObserveOrderedTelemetryForTest();
            TestAssert.True(delayedRestoredWinch.IsOpen,
                "production-ordered NodeAdded callback restores the delayed child before observation");
            TestAssert.Equal(0, session.BuildReport(PrologueSectorId.CanyonDescent,
                    PrologueObjective.ReachBasin, "2026-09-21T00:00:01Z").Mechanisms.Count,
                "a dynamically mounted restored child is baseline, not a fresh activation");

            const string lifecycleId = "canyon.v2.encounter.telemetry-lifecycle";
            CanyonEncounterGroup first = NewTelemetryEncounter(lifecycleId, actors,
                gatesProgression: false);
            root.AddChild(first);
            await UntilPhysics(tree, () => !first.IsActive, 4,
                "first lifecycle starts inactive outside activation bounds");
            recorder.ObserveOrderedTelemetryForTest();
            player.Position = Vector2.Zero;
            await UntilPhysics(tree, () => first.IsActive, 12,
                "first lifecycle activates through real physics bounds");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEncounterEventCount(session, lifecycleId, "Activated", 1,
                "initial physical entry records the first activation edge");
            player.Position = new Vector2(1000f, 1000f);
            await UntilPhysics(tree, () => !first.IsActive, 12,
                "leaving the arena retires the first lifecycle");
            recorder.ObserveOrderedTelemetryForTest();
            player.Position = Vector2.Zero;
            await UntilPhysics(tree, () => first.IsActive, 12,
                "same instance re-enters through real activation physics");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEncounterEventCount(session, lifecycleId, "Activated", 2,
                "same-instance retirement and re-entry records a second activation edge");
            first.EmitSignal(CanyonEncounterGroup.SignalName.GroupDefeated, first.EncounterId);

            player.Position = new Vector2(1000f, 1000f);
            first.QueueFree();
            await Frames(tree, 2);
            CanyonEncounterGroup replacement = NewTelemetryEncounter(lifecycleId, actors,
                gatesProgression: false);
            root.AddChild(replacement);
            await UntilPhysics(tree, () => !replacement.IsActive, 4,
                "replacement lifecycle starts inactive outside activation bounds");
            recorder.ObserveOrderedTelemetryForTest();
            player.Position = Vector2.Zero;
            await UntilPhysics(tree, () => replacement.IsActive, 12,
                "replacement instance enters through real activation physics");
            recorder.ObserveOrderedTelemetryForTest();
            AssertEncounterEventCount(session, lifecycleId, "Activated", 3,
                "replacement instance preserves the third real activation edge");
            replacement.EmitSignal(CanyonEncounterGroup.SignalName.GroupDefeated,
                replacement.EncounterId);

            var firstWinch = new CanyonShortcutWinch
            {
                Name = "FirstWinch", ShortcutId = "canyon.v2.telemetry-retry-winch"
            };
            root.AddChild(firstWinch);
            await Frames(tree, 1);
            recorder.ObserveOrderedTelemetryForTest();
            firstWinch.TryReceiveThread(default);
            recorder.ObserveOrderedTelemetryForTest();
            firstWinch.QueueFree();
            await Frames(tree, 2);
            var secondWinch = new CanyonShortcutWinch
            {
                Name = "SecondWinch", ShortcutId = "canyon.v2.telemetry-retry-winch"
            };
            root.AddChild(secondWinch);
            await Frames(tree, 1);
            recorder.ObserveOrderedTelemetryForTest();
            secondWinch.TryReceiveThread(default);
            recorder.ObserveOrderedTelemetryForTest();

            ProloguePlaytestReport report = session.BuildReport(PrologueSectorId.CanyonDescent,
                PrologueObjective.ReachBasin, "2026-09-21T00:00:02Z");
            TestAssert.Equal(3, report.Encounters.Count(sample =>
                    sample.EncounterId == lifecycleId && sample.Event == "Activated"),
                "initial, re-entry, and replacement activation lifecycles all remain observable");
            TestAssert.Equal(2, report.Encounters.Count(sample =>
                    sample.EncounterId == lifecycleId && sample.Event == "Defeated"),
                "delayed replacement groups bind the actual defeat signal for both lifecycles");
            TestAssert.Equal(2, report.Mechanisms.Count(sample =>
                    sample.MechanismId == "canyon.v2.telemetry-retry-winch"),
                "replacement mechanism instances retain real reactivation edges");
        }
        finally
        {
            if (recorderNodeAddedHandler != null)
                nodeAdded.RemoveEventHandler(tree, recorderNodeAddedHandler);
            if (restoreBeforeRecorder != null) tree.NodeAdded -= restoreBeforeRecorder;
            if (GodotObject.IsInstanceValid(root)) root.QueueFree();
            await Frames(tree, 3);
        }
    }

    private static CanyonEncounterGroup NewTelemetryEncounter(string id, Node actors,
        bool gatesProgression) => new()
    {
        Name = id.Replace('.', '_'),
        EncounterId = id,
        ActorRootPath = new NodePath("../Actors"),
        Definition = new CanyonEncounterDefinition(id, CanyonRouteNodeId.StaggeredRift,
            Optional: false, new Rect2(-100f, -100f, 200f, 200f),
            new Rect2(-100f, -100f, 200f, 200f),
            Array.AsReadOnly(new[]
            {
                new CanyonEnemyPlacement(CanyonEncounterCatalog.RustShellScene,
                    new Vector2(40f, 0f), 0)
            }), GatesProgression: gatesProgression)
    };

    private static void ShortcutTelemetryRequiresAnUninterruptedOpenCorridor()
    {
        var tracker = new PrologueShortcutCorridorTracker();
        Rect2 corridor = new(0f, 100f, 100f, 200f);
        TestAssert.True(tracker.Observe(true, corridor, new Vector2(50f, 320f)) == null &&
                        tracker.Observe(true, corridor, new Vector2(50f, 260f)) == null,
            "shortcut traversal begins only after entering the open physical corridor");
        tracker.Reset();
        TestAssert.True(tracker.Observe(true, corridor, new Vector2(50f, 80f)) == null,
            "recovery clears a pending corridor traversal before the restored position");

        tracker.Reset();
        tracker.Observe(true, corridor, new Vector2(50f, 320f));
        tracker.Observe(true, corridor, new Vector2(50f, 260f));
        PrologueShortcutPass? upward = tracker.Observe(true, corridor, new Vector2(50f, 80f));
        TestAssert.True(upward.HasValue && upward.Value.From == CanyonRouteNodeId.BasinCamp &&
                        upward.Value.To == CanyonRouteNodeId.SplitLevelEncounter,
            "continuous lower-entry/upper-exit motion records the real upward direction");

        tracker.Reset();
        tracker.Observe(true, corridor, new Vector2(50f, 80f));
        tracker.Observe(true, corridor, new Vector2(50f, 140f));
        PrologueShortcutPass? downward = tracker.Observe(true, corridor, new Vector2(50f, 320f));
        TestAssert.True(downward.HasValue &&
                        downward.Value.From == CanyonRouteNodeId.SplitLevelEncounter &&
                        downward.Value.To == CanyonRouteNodeId.BasinCamp,
            "continuous upper-entry/lower-exit motion records the real downward direction");

        tracker.Reset();
        PrologueShortcutPass[] exactDownward = new[] { 80f, 140f, 300f, 320f }
            .Select(y => tracker.Observe(true, corridor, new Vector2(50f, y)))
            .Where(pass => pass.HasValue).Select(pass => pass.Value).ToArray();
        TestAssert.True(exactDownward.Length == 1 &&
                        exactDownward[0].From == CanyonRouteNodeId.SplitLevelEncounter &&
                        exactDownward[0].To == CanyonRouteNodeId.BasinCamp,
            "a lower-boundary sample followed by outside motion records downward once");

        tracker.Reset();
        PrologueShortcutPass[] exactUpward = new[] { 320f, 300f, 260f, 80f }
            .Select(y => tracker.Observe(true, corridor, new Vector2(50f, y)))
            .Where(pass => pass.HasValue).Select(pass => pass.Value).ToArray();
        TestAssert.True(exactUpward.Length == 1 &&
                        exactUpward[0].From == CanyonRouteNodeId.BasinCamp &&
                        exactUpward[0].To == CanyonRouteNodeId.SplitLevelEncounter,
            "an outside lower-boundary sample followed by entry records upward once");
        tracker.Reset();
        TestAssert.True(tracker.Observe(true, corridor, new Vector2(50f, 80f)) == null,
            "a graph-sized position jump that never enters the corridor records nothing");

        tracker.Reset();
        tracker.Observe(true, corridor, new Vector2(-10f, 140f));
        tracker.Observe(true, corridor, new Vector2(10f, 140f));
        tracker.Observe(true, corridor, new Vector2(10f, 260f));
        TestAssert.True(tracker.Observe(true, corridor, new Vector2(-10f, 260f)) == null,
            "side entry and side exit cannot impersonate endpoint traversal");

        tracker.Reset();
        tracker.Observe(true, corridor, new Vector2(50f, 260f));
        TestAssert.True(tracker.Observe(true, corridor, new Vector2(50f, 80f)) == null,
            "an initial observation already inside cannot manufacture a traversal");

        tracker.Reset();
        tracker.Observe(false, corridor, new Vector2(50f, 260f));
        tracker.Observe(true, corridor, new Vector2(50f, 260f));
        TestAssert.True(tracker.Observe(true, corridor, new Vector2(50f, 80f)) == null,
            "opening the shortcut around a player already inside cannot manufacture traversal");
    }

    private static async Task UntilPhysics(SceneTree tree, Func<bool> predicate,
        int maxFrames, string message)
    {
        for (int frame = 0; frame < maxFrames && !predicate(); frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(predicate(), message);
    }

    private static void AssertEncounterEventCount(ProloguePlaytestSession session,
        string id, string eventName, int expected, string message)
    {
        ProloguePlaytestReport report = session.BuildReport(PrologueSectorId.CanyonDescent,
            PrologueObjective.ReachBasin, "2026-09-21T00:00:02Z");
        TestAssert.Equal(expected, report.Encounters.Count(sample =>
            sample.EncounterId == id && sample.Event == eventName), message);
    }

    internal static System.Collections.Generic.IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Task7CaseRegistry.Case(nameof(ProloguePlaytestRecorderTests),
            nameof(HeadlessAutomationDoesNotCreateHumanEvidence),
            () => HeadlessAutomationDoesNotCreateHumanEvidence(tree)),
        Task7CaseRegistry.Case(nameof(ProloguePlaytestRecorderTests),
            nameof(ExplicitHeadlessFixtureWritesInspectableEvidence),
            PrologueCanyonLegacyReplacementTests.InspectableRouteEvidenceSourcesArePacked)
    ];

    private static async Task HeadlessAutomationDoesNotCreateHumanEvidence(SceneTree tree)
    {
        string outputDirectory = null;
        string absoluteDirectory = null;
        string prior = OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE")
            ? OS.GetEnvironment("KNIGHT_PLAYTEST_SOURCE")
            : null;
        PrologueLevelController level = null;
        try
        {
            // Even an old fixture flag plus an impersonated source cannot cross the headless
            // source boundary. Only the real display launcher may emit HandsOnLauncher evidence.
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(level,
                nameof(HeadlessAutomationDoesNotCreateHumanEvidence));
            outputDirectory = paths.PlaytestsDirectory;
            absoluteDirectory = outputDirectory;
            OS.SetEnvironment("KNIGHT_PLAYTEST_SOURCE", "HandsOnLauncher");
            ProloguePlaytestRecorder recorder = level.GetNode<ProloguePlaytestRecorder>(
                "PlaytestRecorder");
            recorder.OutputDirectory = outputDirectory;
            recorder.RecordInHeadlessMode = true;
            level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            TestAssert.True(!Directory.Exists(absoluteDirectory),
                "headless automation cannot emit or impersonate HandsOnLauncher evidence");
        }
        finally
        {
            if (prior == null) OS.UnsetEnvironment("KNIGHT_PLAYTEST_SOURCE");
            else OS.SetEnvironment("KNIGHT_PLAYTEST_SOURCE", prior);
            if (GodotObject.IsInstanceValid(level))
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    // This exact method name remains the Task 8 retired registry key. Its executable delegate is
    // the continuous route evidence contract above; it never writes hands-on telemetry.
    private static void ExplicitHeadlessFixtureWritesInspectableEvidence() { }

    private static void InputSamplerKeepsOnlyStableEdgesAndTracksReleases()
    {
        var state = default(PrologueInputEdgeState);
        var first = ProloguePlaytestRecorder.SampleInputEdges(new PlayerInputFrame(
            -1f, new Vector2(431f, -1f), true, false, true, false, false,
            MouseWorld: new Vector2(9123f, -424f), AttackModePressed: true,
            GrappleModePressed: true, PrimaryPressed: true, PrimaryHeld: true,
            ShieldHeld: true), ref state);
        AssertEdges(first, "Q", "E", "LMB_DOWN", "RMB_DOWN", "SPACE", "SHIFT", "A_DOWN", "W_DOWN");

        var second = ProloguePlaytestRecorder.SampleInputEdges(new PlayerInputFrame(
            1f, new Vector2(-812f, 1f), false, false, false, false, false,
            MouseWorld: new Vector2(-999f, 111f), PrimaryReleased: true), ref state);
        AssertEdges(second, "LMB_UP", "RMB_UP", "A_UP", "D_DOWN", "W_UP", "S_DOWN");

        var third = ProloguePlaytestRecorder.SampleInputEdges(default, ref state);
        AssertEdges(third, "D_UP", "S_UP");
    }

    private static void InputFrameSequenceDoesNotDuplicateJustPressedEdges()
    {
        var state = default(PrologueInputEdgeState);
        ulong lastSequence = ulong.MaxValue;
        var recorded = new System.Collections.Generic.List<string>();
        var frame = new PlayerInputFrame(0f, Vector2.Zero, JumpPressed: true, JumpHeld: false,
            DashPressed: true, AttackPressed: false, ThreadPressed: false,
            AttackModePressed: true, GrappleModePressed: true, PrimaryPressed: true,
            PrimaryHeld: true);

        if (ProloguePlaytestRecorder.TryClaimInputFrameSequence(41ul, ref lastSequence))
            recorded.AddRange(ProloguePlaytestRecorder.SampleInputEdges(frame, ref state));
        if (ProloguePlaytestRecorder.TryClaimInputFrameSequence(41ul, ref lastSequence))
            recorded.AddRange(ProloguePlaytestRecorder.SampleInputEdges(frame, ref state));

        AssertEdges(recorded, "Q", "E", "LMB_DOWN", "SPACE", "SHIFT");
        TestAssert.Equal(41ul, lastSequence,
            "the recorder remembers the sampled physical-input sequence instead of clearing its frame");
    }

    private static async Task RecoveryRequestedSamplesCurrentInputBeforeReset(SceneTree tree)
    {
        string outputDirectory = null;
        string absoluteDirectory = null;
        PrologueLevelController level = null;
        EventInfo recoveryRequested = null;
        Delegate recorderHandler = null;
        try
        {
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(level,
                nameof(RecoveryRequestedSamplesCurrentInputBeforeReset));
            outputDirectory = paths.PlaytestsDirectory;
            absoluteDirectory = outputDirectory;
            tree.Root.AddChild(level);
            await Frames(tree, 4);

            TestAssert.True(level.CurrentSector.HasValue && level.CurrentSectorRoot != null,
                "the recovery-timing fixture loads a real active prologue sector");
            ProloguePlaytestRecorder recorder = level.GetNode<ProloguePlaytestRecorder>(
                "PlaytestRecorder");
            PlayerInputReader input = level.Player.GetNode<PlayerInputReader>("Input");
            PrologueCompletionController completion =
                level.GetNode<PrologueCompletionController>("Completion");
            var session = new ProloguePlaytestSession("recovery-input-timing",
                "2026-09-01T00:00:00Z", "AutomatedFixture");
            ConfigureRecorderForRecoveryTiming(recorder, level, completion, input, session,
                outputDirectory);

            var physicalFrame = new PlayerInputFrame(
                MoveAxis: -1f,
                Aim: Vector2.Zero,
                JumpPressed: true,
                JumpHeld: true,
                DashPressed: false,
                AttackPressed: false,
                ThreadPressed: false,
                GrappleModePressed: true,
                PrimaryPressed: true,
                PrimaryHeld: true,
                ShieldHeld: true);
            SetPrivateProperty(input, nameof(PlayerInputReader.CurrentFrame), physicalFrame);
            SetPrivateProperty(input, nameof(PlayerInputReader.CurrentFrameSequence), 71ul);

            MethodInfo onRecoveryRequested = RequiredMethod(typeof(ProloguePlaytestRecorder),
                "OnRecoveryRequested");
            recoveryRequested = typeof(PrologueLevelController).GetEvent("RecoveryRequested",
                BindingFlags.Instance | BindingFlags.Public)
                ?? throw new InvalidOperationException("recovery timing fixture cannot find RecoveryRequested");
            recorderHandler = Delegate.CreateDelegate(recoveryRequested.EventHandlerType,
                recorder, onRecoveryRequested);
            recoveryRequested.AddEventHandler(level, recorderHandler);

            // This deliberately invokes the real signal before a normal recorder _Process has
            // observed the current physics frame. Reset now follows at the safe deferred boundary.
            var recoveryFinished = tree.ToSignal(level, PrologueLevelController.SignalName.RecoveryCompleted);
            level.RequestRespawn("FALL RESET");

            string runJsonPath = $"{outputDirectory}/run.json";
            string runJsonAbsolutePath = ProjectSettings.GlobalizePath(runJsonPath);
            TestAssert.True(File.Exists(runJsonAbsolutePath),
                "RecoveryRequested persists its own pre-reset fall snapshot");
            ProloguePlaytestReport report = ProloguePlaytestReportFormatter.FromJson(
                File.ReadAllText(runJsonAbsolutePath));
            TestAssert.Equal(1, report.Falls.Count,
                "the real RecoveryRequested callback records one fall before reset");
            AssertEdges(report.Falls[0].RecentInputs.Select(sample => sample.Action).ToArray(),
                "E", "LMB_DOWN", "RMB_DOWN", "SPACE", "A_DOWN");

            await recoveryFinished;
            TestAssert.Equal(default(PlayerInputFrame), input.CurrentFrame,
                "the recovery completion boundary has really cleared the sampled input");
            TestAssert.Equal(71ul, input.CurrentFrameSequence,
                "ClearSampledFrame preserves the physical sequence after recovery reset");
            InvokePrivate(recorder, "ObserveInputEdges");
            InvokePrivate(recorder, "ObserveInputEdges");
            AssertEdges(session.RecentInputEdges.Select(sample => sample.Action).ToArray(),
                "E", "LMB_DOWN", "RMB_DOWN", "SPACE", "A_DOWN");
            TestAssert.True(!session.RecentInputEdges.Any(sample =>
                    sample.Action is "LMB_UP" or "RMB_UP" or "A_UP"),
                "the reset default frame cannot manufacture release edges after its sequence was claimed");
        }
        finally
        {
            if (recoveryRequested != null && recorderHandler != null &&
                GodotObject.IsInstanceValid(level))
                recoveryRequested.RemoveEventHandler(level, recorderHandler);
            if (GodotObject.IsInstanceValid(level))
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static void ConfigureRecorderForRecoveryTiming(ProloguePlaytestRecorder recorder,
        PrologueLevelController level, PrologueCompletionController completion,
        PlayerInputReader input, ProloguePlaytestSession session, string outputDirectory)
    {
        SetPrivateField(recorder, "_level", level);
        SetPrivateField(recorder, "_completion", completion);
        SetPrivateField(recorder, "_session", session);
        SetPrivateField(recorder, "_observedSector", level.CurrentSector!.Value);
        SetPrivateField(recorder, "_input", input);
        SetPrivateField(recorder, "_lastInputFrameSequence", ulong.MaxValue);
        string runJsonPath = $"{outputDirectory}/run.json";
        string runMarkdownPath = $"{outputDirectory}/run.md";
        SetPrivateField(recorder, "_runJsonPath", runJsonPath);
        SetPrivateField(recorder, "_runMarkdownPath", runMarkdownPath);
        SetPrivateProperty(recorder, nameof(ProloguePlaytestRecorder.LatestJsonPath),
            $"{outputDirectory}/latest.json");
        SetPrivateProperty(recorder, nameof(ProloguePlaytestRecorder.LatestMarkdownPath),
            $"{outputDirectory}/latest.md");
    }

    private static void SetPrivateField(object instance, string name, object value)
    {
        FieldInfo field = instance.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"missing private field {name}");
        field.SetValue(instance, value);
    }

    private static void SetPrivateProperty(object instance, string name, object value)
    {
        PropertyInfo property = instance.GetType().GetProperty(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"missing property {name}");
        MethodInfo setter = property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"missing private setter {name}");
        setter.Invoke(instance, [value]);
    }

    private static MethodInfo RequiredMethod(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"missing private method {type.Name}.{name}");

    private static void InvokePrivate(object instance, string name) =>
        RequiredMethod(instance.GetType(), name).Invoke(instance, null);

    private static async Task PlayerInputSequenceAdvancesOnlyOnPhysicsFrames(SceneTree tree)
    {
        var root = new Node2D { Name = "InputSequenceFixture" };
        var reader = new PlayerInputReader { Name = "Input" };
        root.AddChild(reader);
        tree.Root.AddChild(root);
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            ulong first = reader.CurrentFrameSequence;
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            ulong second = reader.CurrentFrameSequence;
            reader.ClearSampledFrame();

            TestAssert.True(first > 0ul && second > first,
                $"each real physics input update advances a monotonic frame sequence (first={first}, second={second})");
            TestAssert.Equal(second, reader.CurrentFrameSequence,
                "clearing sampled input stays isolated from the recorder's frame sequence");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(root)) root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task GroundedSupportUsesTheColliderScenePath(SceneTree tree)
    {
        var root = new Node2D { Name = "FallContextFixture" };
        var gameplay = new Node2D { Name = "Gameplay" };
        var platforms = new Node2D { Name = "Platforms" };
        var shelf = new StaticBody2D { Name = "GripCatchShelf", Position = new Vector2(0f, 72f) };
        shelf.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(200f, 20f) }
        });
        platforms.AddChild(shelf);
        gameplay.AddChild(platforms);
        var player = new CharacterBody2D { Name = "Player", Position = new Vector2(0f, 0f) };
        player.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(24f, 32f) }
        });
        root.AddChild(gameplay);
        root.AddChild(player);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            for (int index = 0; index < 12 && !player.IsOnFloor(); index++)
            {
                player.Velocity = new Vector2(0f, 720f);
                player.MoveAndSlide();
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            TestAssert.True(player.IsOnFloor(), "fixture player lands on a real slide collision");
            TestAssert.Equal("Gameplay/Platforms/GripCatchShelf",
                ProloguePlaytestRecorder.TryReadGroundedSupportPath(player, root),
                "floor telemetry records the collider's stable scene-relative path");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(root)) root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static void AssertEdges(System.Collections.Generic.IReadOnlyList<string> actual,
        params string[] expected)
    {
        TestAssert.Equal(string.Join("|", expected), string.Join("|", actual),
            "input sampler emits only the stable edge vocabulary in deterministic order");
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
