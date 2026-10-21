using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Ui;

namespace Knight.Tests;

/// <summary>Real saved-scene component diagnostics at controlled positions, never route/FPS proof.</summary>
public partial class RunFogComponentCostProbe : SceneTree
{
    private const int Rounds = 5;
    private const int CallsPerRound = 10;
    private const int WarmupCalls = 2;
    private static readonly string[] MountedMethods =
    [
        "PresentMountedLanes", "PresentMountedTerrain", "PresentMountedBarriers",
        "PresentMountedSteam", "PresentMountedMovingBodies", "PresentMountedGates"
    ];

    public override async void _Initialize()
    {
        string failure = null;
        try
        {
            IsolatedPrologueFixture.ValidateStorageRoot();
            AssertStatistics();
            TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase) &&
                Engine.TimeScale == 1d && string.IsNullOrEmpty(Engine.GetWriteMoviePath()),
                "component probe needs ordinary rendered frames at TimeScale 1, without movie output");
            foreach (string argument in OS.GetCmdlineArgs())
                TestAssert.True(!argument.StartsWith("--fixed-fps", StringComparison.Ordinal),
                    "component probe is not a fixed-time capture");
            await RunFixture();
        }
        catch (Exception exception) { failure = exception.ToString(); }
        try { TestSceneTeardown.DrainManagedWrappersBeforeQuit("fog-component-cost"); }
        catch (Exception exception) { failure = failure == null ? exception.ToString() : failure + "\nCleanup: " + exception; }
        if (failure == null) GD.Print("FOG_COMPONENT_COST_PROBE_OK");
        else GD.PushError(failure);
        Quit(failure == null ? 0 : 1);
    }

    private async Task RunFixture()
    {
        PrologueLevelController level = null;
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            using PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
            level = packed.Instantiate<PrologueLevelController>();
            IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(level, "fog-component-cost");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            Root.AddChild(level);
            await Frames(5);
            flow.StartNew();
            await Frames(5);
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var sectors = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(sectors.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(2260f, 3810f)),
                "original checkpoint adapter must mount the actual saved Basin");
            await Frames(8);
            var controller = level.GetNode<PrologueFogController>("FogController");
            var marker = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            Node2D world = level.GetNode<Node2D>("WorldRoot");

            // Reflection and closures happen once, outside every timed/byte window.
            var individual = new (string Name, Action Invoke)[MountedMethods.Length + 1];
            for (int index = 0; index < MountedMethods.Length; index++)
            {
                Action<Node2D> mounted = Bind<Action<Node2D>>(controller, MountedMethods[index], [typeof(Node2D)]);
                individual[index] = (MountedMethods[index], () => mounted(world));
            }
            individual[^1] = ("WorldTargetMarkerVisual.ApplyPromptLayout", Bind<Action>(marker, "ApplyPromptLayout", Type.EmptyTypes));
            Action whole = Bind<Action>(controller, "OnFramePreDraw", Type.EmptyTypes);
            var cases = new (string Name, Vector2 Position)[]
            {
                ("basin-near-shield-lane", new(2260f, 3810f)),
                ("basin-away-from-shield-lane", new(2975f, 3700f))
            };
            foreach (var item in cases)
            {
                Position(level, item.Position);
                await Frames(3); // Last continuation is after the completed FramePostDraw.
                // No await anywhere in this call: actor, native world and frame stay fixed.
                object result = MeasureCase(item.Name, item.Position, level, controller, marker, world, individual, whole);
                string path = Path.Combine(paths.CaseRoot, item.Name + ".json");
                using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.Read))
                    JsonSerializer.Serialize(stream, result, new JsonSerializerOptions
                    { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                GD.Print($"FOG_COMPONENT_COST_EVIDENCE case={item.Name} path={path} controlled_position=true natural_route=false");
            }
        }
        finally
        {
            Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (!level.IsInsideTree()) level.Free();
                else
                {
                    try { await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level); }
                    finally
                    {
                        // Also release a partially initialized fixture if audio drain fails.
                        // Preserve the cleanup exception rather than claiming successful drain.
                        if (GodotObject.IsInstanceValid(level))
                        {
                            if (!level.IsQueuedForDeletion()) level.QueueFree();
                            await ToSignal(this, SignalName.ProcessFrame);
                            await ToSignal(this, SignalName.ProcessFrame);
                        }
                    }
                }
            }
            level = null;
        }
    }

    private object MeasureCase(string name, Vector2 requestedPosition, PrologueLevelController level,
        PrologueFogController controller, WorldTargetMarkerVisual marker, Node2D world,
        (string Name, Action Invoke)[] individual, Action whole)
    {
        HeadlampOcclusionFrame snapshot = controller.LastOcclusionFrame;
        var canvas = level.GetNode<PrologueFogCanvas>("Fog/Canvas");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var health = level.Player.GetNode<HealthComponent>("Health");
        var presentation = level.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var lane = level.CurrentSectorRoot.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
        Vector2 position = level.Player.GlobalPosition;
        ulong actorId = level.Player.GetInstanceId(), worldId = world.GetInstanceId();
        ulong sectorId = level.CurrentSectorRoot.GetInstanceId();
        ulong process = Engine.GetProcessFrames(), physics = Engine.GetPhysicsFrames();
        int initialHealth = health.CurrentHealth, thread = System.Environment.CurrentManagedThreadId;
        TestAssert.True(snapshot is { IsComplete: true } && snapshot.Shapes.Count > 0 &&
            position == requestedPosition && initialHealth > 0 && flow.State == PrologueGameFlowState.Playing &&
            canvas.FrameValid && canvas.IsVisibleInTree() && !Paused &&
            !level.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing() && lane.Monitoring && lane.IsPhysicsProcessing() &&
            presentation.FrameObserverFailureCount == 0,
            "component probe requires the living controlled actor, complete real snapshot and active original fog/cues/hazard");
        object beforeCounts = DescribePresentation(level, marker);
        var raw = new Sample[individual.Length][];
        for (int component = 0; component < individual.Length; component++) raw[component] = new Sample[Rounds];

        for (int warmup = 0; warmup < WarmupCalls; warmup++)
            foreach (var component in individual) component.Invoke();
        AssertContext(snapshot, true);
        // Rotate the first component and reverse alternate rounds. Do not put the
        // capturing whole entry in this phase: it would replace the fixed snapshot.
        for (int round = 0; round < Rounds; round++)
        for (int order = 0; order < individual.Length; order++)
        {
            int component = ((round & 1) == 0 ? round + order : round + individual.Length - 1 - order) % individual.Length;
            raw[component][round] = Measure(individual[component].Invoke, round, order);
            AssertContext(snapshot, true);
        }
        object afterIndividualCounts = DescribePresentation(level, marker);

        // This is a separate inclusive scope: it recaptures geometry and invokes
        // all synchronous ObservationPresented subscribers, including guidance.
        // It is neither a disjoint sum nor exclusive fog CPU/GPU time.
        for (int warmup = 0; warmup < WarmupCalls; warmup++) whole();
        AssertContext(snapshot, false);
        var wholeRaw = new Sample[Rounds];
        for (int round = 0; round < Rounds; round++)
        {
            HeadlampOcclusionFrame prior = controller.LastOcclusionFrame;
            wholeRaw[round] = Measure(whole, round, 0);
            TestAssert.True(!ReferenceEquals(prior, controller.LastOcclusionFrame),
                "whole entry measurement must execute its real Capture rather than an admission early return");
            AssertContext(snapshot, false);
        }
        var components = new List<object>(individual.Length);
        for (int component = 0; component < individual.Length; component++)
            components.Add(new { name = individual[component].Name, samples = raw[component], summary = Summarize(raw[component], Stopwatch.Frequency) });
        return new
        {
            name, fixture = true, controlledPosition = true, naturalRoute = false, performanceCertification = false,
            requestedPosition = new { x = requestedPosition.X, y = requestedPosition.Y },
            actorPosition = new { x = position.X, y = position.Y }, actorId, worldInstanceId = worldId,
            sectorInstanceId = sectorId, sector = level.CurrentSector.ToString(),
            processFrame = process, physicsFrame = physics, managedThread = thread,
            shapeCount = snapshot.Shapes.Count, finalShapeCount = controller.LastOcclusionFrame.Shapes.Count,
            originalHealth = initialHealth, finalHealth = health.CurrentHealth,
            rounds = Rounds, callsPerRound = CallsPerRound, warmupCalls = WarmupCalls, stopwatchFrequency = Stopwatch.Frequency,
            renderer = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
            adapter = RenderingServer.GetVideoAdapterName(), display = DisplayServer.GetName(),
            beforeCounts, afterIndividualCounts, afterWholeCounts = DescribePresentation(level, marker),
            fixedSnapshotComponents = components,
            wholeEntry = new { name = "OnFramePreDraw", samples = wholeRaw, summary = Summarize(wholeRaw, Stopwatch.Frequency) },
            scope = "Synchronous repeated calls after one completed draw at a controlled position; no await in measurement. " +
                "Components share one captured snapshot. Whole entry subsequently recaptures equal geometry and includes synchronous observers. " +
                "Do not sum overlapping components into whole cost. Stopwatch reports elapsed wall time including native calls, not exclusive CPU or GPU time. " +
                "Bytes cover only this managed thread, not native/GPU/other-thread memory. Delegate/loop overhead is included, not subtracted. " +
                "Repeated calls may warm caches and enqueue native draw/layout work; this is not natural-route frame timing. No fog/cue/hazard was disabled."
        };

        void AssertContext(HeadlampOcclusionFrame original, bool requireSameSnapshot)
        {
            HeadlampOcclusionFrame current = controller.LastOcclusionFrame;
            TestAssert.True(System.Environment.CurrentManagedThreadId == thread && Engine.GetProcessFrames() == process &&
                Engine.GetPhysicsFrames() == physics && level.Player.GetInstanceId() == actorId && world.GetInstanceId() == worldId &&
                level.CurrentSectorRoot.GetInstanceId() == sectorId && level.Player.GlobalPosition == position &&
                health.CurrentHealth == initialHealth && flow.State == PrologueGameFlowState.Playing && !Paused &&
                canvas.FrameValid && canvas.IsVisibleInTree() && lane.Monitoring && lane.IsPhysicsProcessing() &&
                presentation.FrameObserverFailureCount == 0 && current is { IsComplete: true },
                "component calls must retain their thread/frame/living actor/world and visible real fog context");
            if (requireSameSnapshot)
                TestAssert.True(ReferenceEquals(original, current), "individual component timing must reuse the exact completed-frame snapshot");
            else
            {
                TestAssert.Equal(original.Shapes.Count, current.Shapes.Count, "whole entry recaptures the same native geometry count");
                for (int index = 0; index < original.Shapes.Count; index++)
                    TestAssert.Equal(original.Shapes[index], current.Shapes[index], "whole entry recaptures unchanged native shape rows");
            }
        }
    }

    private static Sample Measure(Action operation, int round, int order)
    {
        int thread = System.Environment.CurrentManagedThreadId;
        ulong process = Engine.GetProcessFrames(), physics = Engine.GetPhysicsFrames();
        long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        for (int call = 0; call < CallsPerRound; call++) operation();
        long ticks = Stopwatch.GetTimestamp() - start;
        long afterBytes = GC.GetAllocatedBytesForCurrentThread();
        int afterThread = System.Environment.CurrentManagedThreadId;
        TestAssert.True(TryAllocatedDelta(beforeBytes, afterBytes, thread, afterThread, out long bytes) &&
            ticks >= 0 && Engine.GetProcessFrames() == process && Engine.GetPhysicsFrames() == physics,
            "a timed batch must not cross a thread or simulation frame or report decreasing allocation counters");
        return new(round, order, CallsPerRound, ticks, bytes, thread, process, physics);
    }

    private static object DescribePresentation(PrologueLevelController level, WorldTargetMarkerVisual marker)
    {
        var lanes = level.GetNode<FogShieldLaneReadabilityOverlay>("ProtectedCues/ShieldLane");
        var terrain = level.GetNode<FogTerrainReadabilityOverlay>("ProtectedCues/Terrain");
        var barriers = level.GetNode<FogBarrierReadabilityOverlay>("ProtectedCues/Barriers");
        var steam = level.GetNode<FogSteamReadabilityOverlay>("ProtectedCues/Steam");
        var moving = level.GetNode<FogMovingGeometryReadabilityOverlay>("ProtectedCues/MovingBodies");
        var gates = level.GetNode<FogPressureGateReadabilityOverlay>("ProtectedCues/PressureGates");
        return new
        {
            lanes = lanes.PresentedLaneCount, laneSegments = lanes.PresentedSegmentCount,
            terrainSegments = terrain.PresentedSegmentCount, barriers = barriers.PresentedBodyCount,
            barrierSegments = barriers.PresentedSegmentCount, steamJets = steam.PresentedJetCount,
            steamSegments = steam.PresentedSegmentCount, movingBodies = moving.PresentedBodyCount,
            movingSegments = moving.PresentedSegmentCount, gates = gates.PresentedGateCount, gateSegments = gates.PresentedSegmentCount,
            cuesVisible = lanes.IsVisibleInTree() && terrain.IsVisibleInTree() && barriers.IsVisibleInTree() &&
                steam.IsVisibleInTree() && moving.IsVisibleInTree() && gates.IsVisibleInTree(),
            promptVisible = marker.IsVisibleInTree() && marker.HasVisiblePrompt,
            promptTargets = marker.RenderedTargetCount, promptCharacters = marker.RenderedStepText.Length
        };
    }

    private static T Bind<T>(object target, string name, Type[] arguments) where T : Delegate =>
        (target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, arguments, null)
            ?? throw new MissingMethodException(target.GetType().FullName, name)).CreateDelegate<T>(target);

    private static bool TryAllocatedDelta(long before, long after, int beforeThread, int afterThread, out long bytes)
    {
        bytes = 0;
        if (before < 0 || after < before || beforeThread != afterThread) return false;
        bytes = after - before;
        return true;
    }

    private static Summary Summarize(Sample[] samples, long frequency)
    {
        var milliseconds = new double[samples.Length]; var allocated = new double[samples.Length];
        var perCallMs = new double[samples.Length]; var perCallBytes = new double[samples.Length];
        for (int index = 0; index < samples.Length; index++)
        {
            milliseconds[index] = samples[index].ElapsedTicks * 1000d / frequency;
            allocated[index] = samples[index].AllocatedBytes;
            perCallMs[index] = milliseconds[index] / samples[index].Calls;
            perCallBytes[index] = allocated[index] / samples[index].Calls;
        }
        return new(Median(milliseconds), Median(allocated), Median(perCallMs), Median(perCallBytes));
    }

    private static double Median(double[] values)
    {
        if (values.Length == 0) throw new ArgumentException("Median requires observations.", nameof(values));
        var sorted = (double[])values.Clone(); Array.Sort(sorted);
        int middle = sorted.Length / 2;
        return (sorted.Length & 1) == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2d;
    }

    // Literal self-checks catch unit, even-median, cumulative-byte and paired-sort mistakes.
    // No noisy timing or performance threshold is part of these arithmetic checks.
    private static void AssertStatistics()
    {
        TestAssert.Equal(2.5d, Median([4d, 1d, 3d, 2d]), "even median averages the two central values");
        Sample[] raw = [new(0, 0, 10, 9, 100, 1, 1, 1), new(1, 0, 10, 1, 900, 1, 1, 1), new(2, 0, 10, 5, 0, 1, 1, 1)];
        Summary summary = Summarize(raw, 1000);
        TestAssert.Equal(new Summary(5d, 100d, .5d, 10d), summary,
            "elapsed and bytes have independent medians and preserve per-call units");
        TestAssert.Equal(9L, raw[0].ElapsedTicks, "summary must preserve raw round order");
        TestAssert.True(TryAllocatedDelta(1000, 1600, 7, 7, out long bytes) && bytes == 600,
            "managed allocation reports interval delta, not the cumulative counter");
        TestAssert.True(TryAllocatedDelta(1600, 1600, 7, 7, out bytes) && bytes == 0 &&
            !TryAllocatedDelta(1600, 1000, 7, 7, out _) && !TryAllocatedDelta(1000, 1600, 7, 8, out _),
            "zero-byte intervals are valid; backwards counters and different threads are not");
    }

    private static void Position(PrologueLevelController level, Vector2 position)
    {
        level.Player.GlobalPosition = position;
        level.Player.Velocity = Vector2.Zero;
        level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
    }

    private async Task Frames(int count)
    {
        for (int index = 0; index < count; index++)
        {
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }

    private readonly record struct Sample(int Round, int Order, int Calls, long ElapsedTicks, long AllocatedBytes,
        int ManagedThread, ulong ProcessFrame, ulong PhysicsFrame);
    private readonly record struct Summary(double MedianElapsedMs, double MedianAllocatedBytes,
        double MedianElapsedMsPerCall, double MedianAllocatedBytesPerCall);
}
