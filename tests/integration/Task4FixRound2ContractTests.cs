using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Moth;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class Task4FixRound2ContractTests
{
    public static async Task RunCase(SceneTree tree, string testCase)
    {
        switch (testCase)
        {
            case "moth-refresh":
                await RealMothSameTelegraphRefreshesLockedSweep(tree);
                break;
            case "pause-contract":
                PauseFixtureDeclaresSettledBaselineAndFinallyCleanup();
                break;
            case "proof-route":
                MothProofCapturesLockedSweptPath();
                break;
            case "pause-delivery":
                PauseFixtureQueuesAndFlushesARealInputMapEvent();
                break;
            case "display-flow-adapter":
                await DisplayHarnessPreservesAutoStartIntent(tree);
                break;
            case "all":
                await RealMothSameTelegraphRefreshesLockedSweep(tree);
                PauseFixtureDeclaresSettledBaselineAndFinallyCleanup();
                MothProofCapturesLockedSweptPath();
                PauseFixtureQueuesAndFlushesARealInputMapEvent();
                await DisplayHarnessPreservesAutoStartIntent(tree);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(testCase), testCase, "Unknown Task 4 Fix 2 case.");
        }
    }

    private static async Task RealMothSameTelegraphRefreshesLockedSweep(SceneTree tree)
    {
        var root = new Node2D { Name = "Task4Fix2RealMothRefresh" };
        var player = new Node2D { Name = "Player", Position = new Vector2(0f, 140f) };
        player.AddToGroup("player");
        GraveLampMoth moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        PrologueVfxDirector director = GD.Load<PackedScene>("res://scenes/vfx/PrologueVfx.tscn")
            .Instantiate<PrologueVfxDirector>();
        root.AddChild(player);
        root.AddChild(moth);
        root.AddChild(director);
        tree.Root.AddChild(root);

        AttackPresentationFrame initial = default;
        AttackPresentationFrame locked = default;
        PrologueTypedVfxEffect initialEffect = null;
        moth.AttackFrameChanged += frame =>
        {
            if (frame.Timeline.Phase == AttackPhase.Telegraph)
            {
                director.PresentAttackFrame(moth, frame);
                if (!frame.Timeline.HasLockedTarget && initial.Timeline.AttackId == 0)
                {
                    initial = frame;
                    initialEffect = CurrentEffect(director);
                }
                if (frame.Timeline.HasLockedTarget) locked = frame;
            }
        };

        try
        {
            for (int frame = 0; frame < 120 && locked.Timeline.AttackId == 0; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

            TestAssert.True(initial.Timeline.AttackId != 0 && locked.Timeline.AttackId != 0,
                "real Moth publishes both initial and target-locked Telegraph frames");
            TestAssert.Equal(initial.Timeline.AttackId, locked.Timeline.AttackId,
                "real Moth same Telegraph transition retains its source-local AttackId");
            TestAssert.Equal(AttackShapeKind.Circle, initial.Shape.Kind,
                "real Moth Telegraph begins from its live circular collider");
            TestAssert.Equal(AttackShapeKind.Capsule, locked.Shape.Kind,
                "real Moth Telegraph advances to its locked swept-circle path");
            TestAssert.Equal(1, director.TypedEffectCount,
                "same source/id/phase geometry refresh keeps exactly one typed effect");

            PrologueTypedVfxEffect refreshed = CurrentEffect(director);
            TestAssert.True(ReferenceEquals(initialEffect, refreshed),
                "same source/id/phase refresh updates the existing effect instance");
            TestAssert.Equal(locked.Shape, refreshed.AttackShape,
                "existing effect stores the latest authoritative locked Moth shape");
            AssertPointsNear(ExpectedCapsuleWorldOutline(locked.Shape), RenderedWorldOutline(refreshed),
                "rendered Moth outline equals the latest authoritative swept path");
        }
        finally
        {
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void PauseFixtureDeclaresSettledBaselineAndFinallyCleanup()
    {
        string source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),
            "tests", "integration", "PrologueCompletionTests.cs"));
        int method = source.IndexOf(
            "private static async Task PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit",
            StringComparison.Ordinal);
        int press = source.IndexOf("DeliverPauseInput(true)", method, StringComparison.Ordinal);
        int settle = source.IndexOf("await SettlePauseBaseline", method, StringComparison.Ordinal);
        int helper = source.IndexOf("private static async Task SettlePauseBaseline", StringComparison.Ordinal);
        int release = source.IndexOf("DeliverPauseInput(false)", helper, StringComparison.Ordinal);
        TestAssert.True(method >= 0 && settle > method && settle < press && release > helper,
            "pause fixture pre-releases Pause before the real press edge");
        int baseline = source.IndexOf("AssertPauseBaseline", method, StringComparison.Ordinal);
        TestAssert.True(baseline > method && baseline < press &&
                        source.Contains("pause baseline action is released", StringComparison.Ordinal) &&
                        source.Contains("pause baseline tree is unpaused", StringComparison.Ordinal) &&
                        source.Contains("pause baseline menu is closed", StringComparison.Ordinal),
            "pause fixture asserts released, unpaused, closed baseline before the real press edge");
        TestAssert.True(source.IndexOf("try", method, StringComparison.Ordinal) < press &&
                        source.IndexOf("finally", press, StringComparison.Ordinal) > press,
            "pause fixture wraps the real edge and assertions in try/finally cleanup");
    }

    private static void MothProofCapturesLockedSweptPath()
    {
        string source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),
            "tests", "presentation", "PrologueVisualAudit.cs"));
        int handler = source.IndexOf("private void OnMothProofAttackFrameChanged", StringComparison.Ordinal);
        int nextMethod = source.IndexOf("\n    private ", handler + 1, StringComparison.Ordinal);
        string body = source.Substring(handler, nextMethod - handler);
        TestAssert.True(body.Contains("frame.Timeline.HasLockedTarget", StringComparison.Ordinal) &&
                        body.Contains("AttackShapeKind.Capsule", StringComparison.Ordinal),
            "Moth Telegraph proof waits for the target-locked swept-path publisher frame");
        TestAssert.True(source.Contains("Moth locked swept proof rendered outline equals authoritative frame",
                StringComparison.Ordinal),
            "Moth proof audits rendered global outline equality against its real publisher frame");
    }

    private static void PauseFixtureQueuesAndFlushesARealInputMapEvent()
    {
        string source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),
            "tests", "integration", "PrologueCompletionTests.cs"));
        TestAssert.True(source.Contains("new InputEventAction", StringComparison.Ordinal) &&
                        source.Contains("Input.ParseInputEvent", StringComparison.Ordinal) &&
                        source.Contains("Input.FlushBufferedEvents", StringComparison.Ordinal),
            "pause fixture queues and flushes an engine InputEventAction instead of only changing action state");
        TestAssert.True(source.Contains("pause InputMap edge is registered before production _Process",
                StringComparison.Ordinal),
            "pause fixture asserts the real InputMap edge before production _Process consumes it");
    }

    private static async Task DisplayHarnessPreservesAutoStartIntent(SceneTree tree)
    {
        string root = Path.Combine(Path.GetTempPath(), "knight-task4-display-flow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        PrologueLevelController automatic = null;
        PrologueLevelController explicitTitle = null;
        DisplayBackedFlowAutoStartHarness.Attach(tree);
        try
        {
            automatic = InstantiateFlowFixture("DisplayAutoStart", root, "automatic", headlessAutoStart: true);
            explicitTitle = InstantiateFlowFixture("DisplayExplicitTitle", root, "explicit", headlessAutoStart: false);
            tree.Root.AddChild(automatic);
            tree.Root.AddChild(explicitTitle);
            await ProcessFrames(tree, 3);

            PrologueGameFlowState automaticState =
                automatic.GetNode<PrologueGameFlowController>("GameFlow").State;
            PrologueGameFlowState explicitState =
                explicitTitle.GetNode<PrologueGameFlowController>("GameFlow").State;
            GD.Print($"TASK4_DISPLAY_FLOW_FIXTURE automatic={automaticState} explicit={explicitState}");
            TestAssert.Equal(PrologueGameFlowState.Title, explicitState,
                "display adapter preserves an explicit HeadlessAutoStart false title fixture");
            TestAssert.Equal(PrologueGameFlowState.Playing, automaticState,
                "display adapter preserves the existing auto-start intent for dependent fixtures");
        }
        finally
        {
            DisplayBackedFlowAutoStartHarness.Detach(tree);
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(automatic)) automatic.QueueFree();
            if (GodotObject.IsInstanceValid(explicitTitle)) explicitTitle.QueueFree();
            await ProcessFrames(tree, 2);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static PrologueLevelController InstantiateFlowFixture(string name, string root,
        string stem, bool headlessAutoStart)
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.SavePath = Path.Combine(root, stem + "-save.json");
        flow.SettingsPath = Path.Combine(root, stem + "-settings.json");
        flow.HeadlessAutoStart = headlessAutoStart;
        return level;
    }

    private static async Task ProcessFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static PrologueTypedVfxEffect CurrentEffect(PrologueVfxDirector director)
    {
        foreach (Node child in director.GetNode<Node2D>("World").GetChildren())
            if (child is PrologueTypedVfxEffect effect && !effect.IsQueuedForDeletion()) return effect;
        throw new InvalidOperationException("No live typed Moth effect was rendered.");
    }

    private static Vector2[] RenderedWorldOutline(PrologueTypedVfxEffect effect)
    {
        MethodInfo method = typeof(PrologueTypedVfxEffect).GetMethod("AttackOutline",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Typed effect exposes no rendered outline.");
        Vector2[] local = (Vector2[])method.Invoke(effect, null);
        var world = new Vector2[local.Length];
        for (int index = 0; index < local.Length; index++) world[index] = effect.GlobalTransform * local[index];
        return world;
    }

    private static Vector2[] ExpectedCapsuleWorldOutline(AttackShapeFrame shape)
    {
        const int segments = 40;
        float radius = shape.Extent.X;
        float straight = Mathf.Max(0f, shape.Extent.Y - radius);
        var expected = new Vector2[segments];
        Transform2D world = new(shape.RotationRadians, shape.Origin);
        for (int index = 0; index < segments / 2; index++)
        {
            float angle = Mathf.Pi + Mathf.Pi * index / (segments / 2 - 1f);
            expected[index] = world * new Vector2(Mathf.Cos(angle) * radius,
                -straight + Mathf.Sin(angle) * radius);
        }
        for (int index = 0; index < segments / 2; index++)
        {
            float angle = Mathf.Pi * index / (segments / 2 - 1f);
            expected[segments / 2 + index] = world * new Vector2(Mathf.Cos(angle) * radius,
                straight + Mathf.Sin(angle) * radius);
        }
        return expected;
    }

    private static void AssertPointsNear(Vector2[] expected, Vector2[] actual, string message)
    {
        TestAssert.Equal(expected.Length, actual.Length, message + " point count");
        for (int index = 0; index < expected.Length; index++)
            TestAssert.True(expected[index].DistanceTo(actual[index]) <= .05f,
                $"{message}; index={index} expected={expected[index]} actual={actual[index]}");
    }
}
