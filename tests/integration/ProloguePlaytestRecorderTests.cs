using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Prologue;

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
        string outputDirectory = $"user://task9-recorder-boundary/{Guid.NewGuid():N}";
        string absoluteDirectory = ProjectSettings.GlobalizePath(outputDirectory);
        string prior = OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE")
            ? OS.GetEnvironment("KNIGHT_PLAYTEST_SOURCE")
            : null;
        PrologueLevelController level = null;
        try
        {
            // Even an old fixture flag plus an impersonated source cannot cross the headless
            // source boundary. Only the real display launcher may emit HandsOnLauncher evidence.
            OS.SetEnvironment("KNIGHT_PLAYTEST_SOURCE", "HandsOnLauncher");
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
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
            if (Directory.Exists(absoluteDirectory)) Directory.Delete(absoluteDirectory, true);
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
        string outputDirectory = $"user://task5-recovery-input-timing/{Guid.NewGuid():N}";
        string absoluteDirectory = ProjectSettings.GlobalizePath(outputDirectory);
        PrologueLevelController level = null;
        EventInfo recoveryRequested = null;
        Delegate recorderHandler = null;
        try
        {
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
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
            // observed the current physics frame. The level then clears CurrentFrame synchronously.
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
            if (Directory.Exists(absoluteDirectory)) Directory.Delete(absoluteDirectory, true);
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
