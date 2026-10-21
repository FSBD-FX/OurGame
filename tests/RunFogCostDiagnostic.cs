using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;
using Knight.Ui;

namespace Knight.Tests;

/// <summary>Disposable normal-time presentation experiment, never a playable-route proof.</summary>
public partial class RunFogCostDiagnostic : SceneTree
{
    private readonly FogCostSampleBuffer[] _arms =
        [new(), new(), new(), new()];
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private CharacterBody2D _actor;
    private HealthComponent _health;
    private PrologueFogController _controller;
    private PrologueFogCanvas _canvas;
    private SeamlessPrologueWorld _world;
    private CanvasItem[] _drawNodes;
    private ulong[] _ids;
    private Func<HeadlampOcclusionFrame>[] _snapshots;
    private Func<int>[] _counts;
    private readonly string[] _names = ["A_original", "B_native_hidden", "C_controller_detached", "A_restored"];
    private readonly long[] _allocatedStarts = new long[4], _allocatedEnds = new long[4];
    private readonly List<PhaseBoundary> _boundaries = new();
    private ulong _originalMaterialId, _restoredMaterialId;
    private string _restoreDetails;
    private int _arm = -1, _recoveries;
    private bool _collect, _hasAfter, _beforeHook, _afterHook, _postHook;
    private ulong _beforeUsec, _afterUsec, _beforeProcess, _afterProcess, _beforeCount, _afterCount;
    private HeadlampOcclusionFrame _beforeSnapshot;
    private bool _fresh, _consumers;
    private Vector2 _baselinePosition;
    private ulong _baselinePlayer, _baselineSupport;
    private int _baselineHealth;
    private Rid _viewport;
    private IsolatedPrologueFixturePaths _paths;
    private ulong _started;
    private bool _evidenceAttempted, _evidenceWritten, _complete, _restored;
    private string _failure;
    private FogAllocationStamp _previousPostAllocation, _beforeAllocation, _afterAllocation;
    private CanvasItem[] _redrawNodes;
    private Action[] _redrawHandlers;
    private string[] _redrawPaths, _redrawKinds;
    private ulong[] _redrawIds;
    private long[][] _redrawCounts;
    private readonly List<LocalMethodCost> _localMethodCosts = new();
    protected virtual bool TrackManagedAllocationTypes => false;
    private ManagedAllocationTypeProbe _allocationTypeProbe;
    private ManagedAllocationTypeEvidence _allocationTypeEvidence;

    public override async void _Initialize()
    {
        IsolatedPrologueFixturePaths paths = null;
        string failure = null;
        bool restored = false, complete = false;
        ulong started = Time.GetTicksUsec();
        _started = started;
        Boundary("initialize");
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            Boundary("first-process-frame");
            string[] user = OS.GetCmdlineUserArgs();
            TestAssert.True(user.Length == 1 && user[0].StartsWith("--case-storage-root=", StringComparison.Ordinal),
                "diagnostic accepts only exactly one isolated storage argument");
            foreach (string arg in OS.GetCmdlineArgs().Concat(System.Environment.GetCommandLineArgs()))
                TestAssert.True(!arg.StartsWith("--fixed-fps", StringComparison.Ordinal) &&
                    !arg.StartsWith("--write-movie", StringComparison.Ordinal) && arg != "--headless",
                    "diagnostic rejects fixed-time/movie/headless host arguments");
            GD.Print($"FOG_COST_START display={DisplayServer.GetName()} visible={Root.Visible} " +
                $"timeScale={Engine.TimeScale} movie={Engine.GetWriteMoviePath()}");
            TestAssert.True(DisplayServer.GetName().Equals("windows", StringComparison.OrdinalIgnoreCase) && Root.Visible &&
                Engine.TimeScale == 1d && string.IsNullOrEmpty(Engine.GetWriteMoviePath()),
                "normal visible Windows engine, TimeScale1, no movie writer required");
            TestAssert.Equal("gl_compatibility", ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                "measure the actual project renderer");
            RenderingServer.FramePreDraw += BeforeSubmission; // Before saved controller _Ready subscribes.
            _beforeHook = true;
            Boundary("before-world-load");
            _level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            Boundary("after-world-instantiate");
            paths = IsolatedPrologueFixture.Configure(_level, "fog-cost-static-entrance");
            _paths = paths;
            _flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
            _flow.AutomationAutoStart = _flow.HeadlessAutoStart = false;
            Root.AddChild(_level);
            Boundary("after-world-mount");
            _level.RecoveryRequested += _ => _recoveries++;
            _controller = _level.GetNode<PrologueFogController>("FogController");
            _canvas = _level.GetNode<PrologueFogCanvas>("Fog/Canvas");
            _world = _level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            BindConsumers();
            BindRedrawCounters();
            RenderingServer.FramePreDraw += AfterSubmission;
            _afterHook = true;
            RenderingServer.FramePostDraw += AfterDraw;
            _postHook = true;
            await Frames(5);
            TestAssert.Equal(PrologueGameFlowState.Title, _flow.State, "saved fixture begins at actual Title");
            Boundary("before-start-new");
            _flow.StartNew();
            Boundary("after-start-new");
            await Frames(60); // Let real physics settle; no teleports, input, tuning or forced GC.
            Boundary("after-natural-settle");
            _actor = _level.Player;
            _health = _actor.GetNode<HealthComponent>("Health");
            _baselinePosition = _actor.GlobalPosition;
            _baselinePlayer = _actor.GetInstanceId();
            _baselineHealth = _health.CurrentHealth;
            _baselineSupport = SupportId();
            TestAssert.True(_actor.IsOnFloor() && _baselineSupport != 0 && _baselineHealth > 0 && _recoveries == 0,
                "natural spawn is living, floor supported and has never recovered");
            _viewport = Root.GetViewportRid();
            _originalMaterialId = _canvas.Material.GetInstanceId();
            RenderingServer.ViewportSetMeasureRenderTime(_viewport, true);
            GD.Print($"FOG_COST_CONTEXT player={_baselinePlayer} support={_baselineSupport} " +
                $"position={_baselinePosition} health={_baselineHealth} renderer={RenderingServer.GetVideoAdapterName()} " +
                $"size={Root.Size} vsync={DisplayServer.WindowGetVsyncMode()} maxfps={Engine.MaxFps} " +
                "timeScale=1 normalTime=true capture=false humanValidated=false performanceCertification=false");

            await MeasureArm(0, started);
            if (Time.GetTicksUsec() - started < 105_000_000UL) await MeasureArm(1, started);
            _collect = false;
            Boundary("before-controller-detach");
            // Only original presentation owners leave the disposable tree. Gameplay stays mounted.
            _level.RemoveChild(_controller);
            Boundary("after-controller-detach");
            _canvas.GetParent().RemoveChild(_canvas); // Its original ExitTree disposes owned textures/material.
            Boundary("after-canvas-detach");
            if (Time.GetTicksUsec() - started < 105_000_000UL)
            {
                if (TrackManagedAllocationTypes)
                {
                    _allocationTypeProbe = new();
                    _allocationTypeProbe.Start();
                }
                await MeasureArm(2, started);
                StopAllocationTypes();
                if (TrackManagedAllocationTypes)
                    TestAssert.True(_allocationTypeEvidence is { ProviderFound: true, Ticks: > 0, Malformed: 0, Dropped: 0 },
                        "runtime allocation type diagnostic requires actual correctly decoded bounded ticks");
            }
            RenderingServer.FramePreDraw -= AfterSubmission;
            _afterHook = false;
            Boundary("before-restore");
            _level.GetNode("Fog").AddChild(_canvas);
            _level.AddChild(_controller); // Original RequestReady rebinds the same saved consumers.
            RenderingServer.FramePreDraw += AfterSubmission; // Re-establish and validate order after re-entry.
            _afterHook = true;
            SetNativeDrawing(true);
            // Restoration is a draw-boundary assertion, not a between-process-frame visibility assertion.
            for (int index = 0; index < 3; index++)
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Boundary("after-restore-draws");
            _restoredMaterialId = _canvas.Material?.GetInstanceId() ?? 0;
            _restoreDetails = $"frameValid={_canvas.FrameValid};visible={_canvas.Visible};treeVisible={_canvas.IsVisibleInTree()};" +
                $"material={_restoredMaterialId};originalMaterial={_originalMaterialId};" +
                $"snapshotComplete={_controller.LastOcclusionFrame?.IsComplete};consumers={ConsumersReady()}";
            restored = _canvas.FrameValid && _canvas.IsVisibleInTree() && _canvas.Material != null &&
                _restoredMaterialId != _originalMaterialId &&
                _controller.LastOcclusionFrame is { IsComplete: true } && ConsumersReady();
            TestAssert.True(restored, "same saved presentation restores material, snapshot and visible consumers");
            if (Time.GetTicksUsec() - started < 105_000_000UL) await MeasureArm(3, started);
            complete = restored && _arms.All(buffer => buffer.Complete);
            if (complete) MeasureLocalMethods(); // Outside natural-time arms, never included in their intervals.
        }
        catch (Exception exception) { failure = exception.ToString(); }
        finally
        {
            StopCollection();
            Unhook();
            _complete = complete; _restored = restored; _failure = failure;
            Boundary("diagnostic-body-return");
            WriteEvidenceOnce("diagnostic_body_returned");
            if (!_evidenceWritten) failure ??= "Diagnostic evidence could not be retained.";
            if (GodotObject.IsInstanceValid(_level) && _level.IsInsideTree())
            {
                // Repair detached original owners for ordinary disposal, never restore user data.
                if (GodotObject.IsInstanceValid(_canvas) && !_canvas.IsInsideTree()) _level.GetNode("Fog").AddChild(_canvas);
                if (GodotObject.IsInstanceValid(_controller) && !_controller.IsInsideTree()) _level.AddChild(_controller);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);
            }
        }
        if (failure != null) GD.PushError(failure);
        GD.Print(complete && failure == null ? "FOG_COST_DIAGNOSTIC_OK" : "FOG_COST_DIAGNOSTIC_PARTIAL_OR_FAILED");
        Quit(complete && failure == null ? 0 : 2);
    }

    public override void _Finalize()
    {
        // A normal engine quit can abandon an async ProcessFrame continuation.
        // Preserve partial numeric evidence synchronously; hard kill remains unobservable.
        StopCollection();
        if (_evidenceAttempted) return;
        // SceneTree has already released its viewport before this final callback.
        // A cached RID can remain numerically valid but must not be sent to RenderingServer.
        Unhook(disableViewportTiming: false);
        _complete = false;
        _failure ??= "engine_exit_before_diagnostic_return";
        Boundary("engine-finalize-before-return");
        WriteEvidenceOnce("engine_exit_before_diagnostic_return");
    }

    private void StopCollection()
    {
        _collect = false;
        StopAllocationTypes();
        if (_arm >= 0 && _allocatedEnds[_arm] == 0)
            _allocatedEnds[_arm] = GC.GetTotalAllocatedBytes(false);
    }

    private void Unhook(bool disableViewportTiming = true)
    {
        if (_beforeHook) { RenderingServer.FramePreDraw -= BeforeSubmission; _beforeHook = false; }
        if (_afterHook) { RenderingServer.FramePreDraw -= AfterSubmission; _afterHook = false; }
        if (_postHook) { RenderingServer.FramePostDraw -= AfterDraw; _postHook = false; }
        // Ordinary cleanup still owns live nodes. _Finalize runs after native tree disposal;
        // freed nodes already release their signal connections, so do not address them there.
        if (disableViewportTiming && _redrawNodes != null)
            for (int index = 0; index < _redrawNodes.Length; index++)
                if (GodotObject.IsInstanceValid(_redrawNodes[index]))
                    _redrawNodes[index].Draw -= _redrawHandlers[index];
        if (disableViewportTiming && _viewport.IsValid)
            RenderingServer.ViewportSetMeasureRenderTime(_viewport, false);
        _viewport = default;
    }

    private void WriteEvidenceOnce(string outcome)
    {
        if (_evidenceAttempted || _paths == null) return;
        _evidenceAttempted = true;
        try
        {
            // Collection has stopped; sorting and IO never occur inside the sampling callbacks.
            if (_collect) throw new InvalidOperationException("Diagnostic evidence cannot serialize during collection.");
            var arms = _arms.Select((buffer, index) => new
            {
                name = _names[index], buffer.Complete, buffer.RawCount, buffer.WarmupCount,
                buffer.ValidCount, buffer.InvalidCount, buffer.OverflowCount,
                allocationDeltaBytes = _allocatedEnds[index] - _allocatedStarts[index],
                allocationPartition = DescribeAllocations(buffer),
                redraws = _redrawNodes == null ? null : _redrawNodes.Select((_, nodeIndex) => new
                {
                    path = _redrawPaths[nodeIndex], kind = _redrawKinds[nodeIndex],
                    id = _redrawIds[nodeIndex], count = _redrawCounts[index][nodeIndex]
                }).ToArray(),
                intervalP50Ms = Percentile(buffer.Intervals, .50), intervalP95Ms = Percentile(buffer.Intervals, .95),
                intervalP99Ms = Percentile(buffer.Intervals, .99), bracketP50Ms = Percentile(buffer.Brackets, .50),
                bracketP95Ms = Percentile(buffer.Brackets, .95),
                cpuTimingNonzero = buffer.Raw.ToArray().Any(sample => sample.RenderCpuMs > 0d),
                gpuTimingNonzero = buffer.Raw.ToArray().Any(sample => sample.RenderGpuMs > 0d),
                raw = buffer.Raw.ToArray()
            }).ToArray();
            string path = Path.Combine(_paths.CaseRoot, "fog-cost.json");
            using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.Write(JsonSerializer.Serialize(new
                {
                    outcome, complete = _complete, restored = _restored, failure = _failure,
                    elapsedMs = (Time.GetTicksUsec() - _started) / 1000d,
                    boundaries = _boundaries, originalMaterialId = _originalMaterialId,
                    restoredMaterialId = _restoredMaterialId, restoreDetails = _restoreDetails,
                    localMethodCosts = _localMethodCosts,
                    localMethodBasis = "synchronous_repeat_calls_after_natural_time_arms_not_per_frame_or_route_cost",
                    allocationTypeEvidence = _allocationTypeEvidence,
                    allocationTypeBasis = "own_process_C_arm_buffered_threshold_crossing_type_not_exact_type_bytes_or_callstacks_observer_overhead_present",
                    humanValidated = false, performanceCertification = false, fullRoute = false,
                    basis = "frame_post_draw_callback_wall_clock_not_desktop_present",
                    timingBasis = "previous_viewport_frame_cpu_gpu_ms_zero_means_unknown",
                    context = "natural_new_game_static_start_floor_not_route_hotspot",
                    bracketBasis = "ordered_frame_pre_draw_wall_time_not_exclusive_cpu",
                    allocationBasis = "current_managed_thread_adjacent_callback_partitions_including_warmup_and_observer_not_exclusive_methods",
                    redrawBasis = "original_node_Draw_signal_occurrence_before_Draw_method_not_allocation_or_native_retained_draw_count",
                    consumerCountEncoding = "7_bits_each_fragments_attacks_interaction_terrain_steam_lane_pressure_moving_barriers",
                    overhead = "native metric getters, consumer/context checks, numeric buffer and allocation endpoints; no per-frame IO",
                    args = OS.GetCmdlineArgs(), userArgs = OS.GetCmdlineUserArgs(), arms
                }));
            _evidenceWritten = true;
            GD.Print($"FOG_COST_ARTIFACT path={path} outcome={outcome} complete={_complete} restored={_restored}");
        }
        catch (Exception exception) { GD.PushError("FOG_COST_ARTIFACT_FAILED: " + exception); }
    }

    private async Task MeasureArm(int index, ulong started)
    {
        _arm = index; _hasAfter = false; _previousPostAllocation = default;
        _allocatedStarts[index] = GC.GetTotalAllocatedBytes(false);
        Boundary(_names[index] + "-start");
        _collect = true;
        while (!_arms[index].Complete && Time.GetTicksUsec() - started < 105_000_000UL)
            await ToSignal(this, SignalName.ProcessFrame);
        _collect = false;
        _allocatedEnds[index] = GC.GetTotalAllocatedBytes(false);
        Boundary(_names[index] + "-stop");
    }

    private void BeforeSubmission()
    {
        if (!_collect) return;
        _hasAfter = false;
        _beforeAllocation = CaptureAllocation();
        _beforeUsec = _beforeAllocation.Usec; _beforeProcess = _beforeAllocation.Process;
        _beforeCount = _controller.PresentedFrameCount;
        _beforeSnapshot = _controller.LastOcclusionFrame;
    }

    private void AfterSubmission()
    {
        if (!_collect) return;
        _afterAllocation = CaptureAllocation();
        _afterUsec = _afterAllocation.Usec; _afterProcess = _afterAllocation.Process;
        _afterCount = _controller.PresentedFrameCount;
        _fresh = _controller.LastOcclusionFrame is { IsComplete: true } current &&
            !ReferenceEquals(current, _beforeSnapshot) && SharedSnapshots(current);
        _consumers = ConsumersReady();
        if (_arm == 1) SetNativeDrawing(false); // Never high-level Visible=false.
        _hasAfter = true;
    }

    private void AfterDraw()
    {
        if (!_collect || !_hasAfter) return;
        FogAllocationStamp postAllocation = CaptureAllocation();
        ulong post = postAllocation.Usec;
        Vector2 position = _actor.GlobalPosition;
        ulong support = SupportId(), mask = 0;
        foreach (PrologueSectorId sector in _world.MountedSectorIds) mask |= 1UL << (int)sector;
        bool gameplay = _flow.State == PrologueGameFlowState.Playing && !Paused && Engine.TimeScale == 1d &&
            _actor.GetInstanceId() == _baselinePlayer && _health.CurrentHealth == _baselineHealth &&
            _recoveries == 0 && _actor.IsOnFloor() && support == _baselineSupport &&
            position.DistanceTo(_baselinePosition) < .1f &&
            _actor.GetNode<PlayerPresentationCoordinator>("Presentation").FrameObserverFailureCount == 0;
        ulong counts = 0;
        for (int index = 0; index < _counts.Length; index++)
        {
            int count = _counts[index]();
            if (count < 0 || count > 127) _consumers = false;
            else counts |= (ulong)count << (index * 7);
        }
        _arms[_arm].Observe(new FogCostSample(_beforeUsec, _afterUsec, post,
            _beforeProcess, _afterProcess, _beforeCount, _afterCount, _arm != 2,
            _fresh, _consumers, gameplay, Root.HasFocus(),
            RenderingServer.ViewportGetMeasuredRenderTimeCpu(_viewport),
            RenderingServer.ViewportGetMeasuredRenderTimeGpu(_viewport), RenderingServer.GetFrameSetupTimeCpu(),
            RenderingServer.ViewportGetRenderInfo(_viewport, RenderingServer.ViewportRenderInfoType.Canvas,
                RenderingServer.ViewportRenderInfo.DrawCallsInFrame), position.X, position.Y,
            _health.CurrentHealth, _controller.LastOcclusionFrame?.Shapes.Count ?? 0, counts,
            Engine.GetPhysicsFrames(), support, _recoveries, mask,
            new FogAllocationWindow(_previousPostAllocation, _beforeAllocation, _afterAllocation, postAllocation)));
        _previousPostAllocation = postAllocation;
        _hasAfter = false;
    }

    private void BindConsumers()
    {
        var f = _level.GetNode<FogFragmentReadabilityOverlay>("ProtectedCues/Fragments");
        var a = _level.GetNode<FogReadabilityOverlay>("ProtectedCues/Attacks");
        var q = _level.GetNode<FogInteractionReadabilityOverlay>("ProtectedCues/Interaction");
        var t = _level.GetNode<FogTerrainReadabilityOverlay>("ProtectedCues/Terrain");
        var s = _level.GetNode<FogSteamReadabilityOverlay>("ProtectedCues/Steam");
        var l = _level.GetNode<FogShieldLaneReadabilityOverlay>("ProtectedCues/ShieldLane");
        var p = _level.GetNode<FogPressureGateReadabilityOverlay>("ProtectedCues/PressureGates");
        var m = _level.GetNode<FogMovingGeometryReadabilityOverlay>("ProtectedCues/MovingBodies");
        var b = _level.GetNode<FogBarrierReadabilityOverlay>("ProtectedCues/Barriers");
        _drawNodes = [_canvas, f, a, q, t, s, l, p, m, b];
        _ids = _drawNodes.Select(node => node.GetInstanceId()).ToArray();
        _snapshots = [() => f.LastOcclusionFrame, () => a.LastOcclusionFrame,
            () => q.LastOcclusionFrame, () => t.LastOcclusionFrame, () => s.LastOcclusionFrame,
            () => l.LastOcclusionFrame, () => p.LastOcclusionFrame, () => m.LastOcclusionFrame, () => b.LastOcclusionFrame];
        _counts = [() => f.PresentedCueCount, () => a.PresentedCueCount, () => q.PresentedCueCount,
            () => t.PresentedSegmentCount, () => s.PresentedSegmentCount, () => l.PresentedSegmentCount,
            () => p.PresentedSegmentCount, () => m.PresentedSegmentCount, () => b.PresentedSegmentCount];
    }

    private void BindRedrawCounters()
    {
        var nodes = new List<CanvasItem>();
        void Visit(Node node)
        {
            if (node is PlayerFormalLayer or CanyonBackdropVisual or CanyonPlatformVisual)
                nodes.Add((CanvasItem)node);
            foreach (Node child in node.GetChildren()) Visit(child);
        }
        Visit(_level); // Setup only; never traverse the tree in a measurement callback.
        _redrawNodes = nodes.ToArray();
        _redrawHandlers = new Action[nodes.Count];
        _redrawPaths = new string[nodes.Count]; _redrawKinds = new string[nodes.Count];
        _redrawIds = new ulong[nodes.Count];
        _redrawCounts = new long[4][];
        for (int arm = 0; arm < 4; arm++) _redrawCounts[arm] = new long[nodes.Count];
        for (int index = 0; index < nodes.Count; index++)
        {
            int captured = index;
            _redrawIds[index] = nodes[index].GetInstanceId();
            _redrawPaths[index] = nodes[index].GetPath().ToString();
            _redrawKinds[index] = nodes[index].GetType().Name;
            _redrawHandlers[index] = () => { if (_collect) _redrawCounts[_arm][captured]++; };
            nodes[index].Draw += _redrawHandlers[index];
        }
    }

    private static FogAllocationStamp CaptureAllocation() => new(Time.GetTicksUsec(),
        Engine.GetProcessFrames(), System.Environment.CurrentManagedThreadId, GC.GetAllocatedBytesForCurrentThread());

    private void StopAllocationTypes()
    {
        if (_allocationTypeProbe != null)
            _allocationTypeEvidence = _allocationTypeProbe.StopAndRetain();
    }

    private void MeasureLocalMethods()
    {
        var guidance = _level.GetNode<PrologueGuidanceController>("Guidance");
        var fireflies = _level.GetNode<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance");
        var marker = _level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        Node2D sectorScope = _level.CurrentSectorRoot.GetParent() as Node2D ?? _level.CurrentSectorRoot;
        Node playerRoot = _level.GetNode(_level.PlayerRootPath);
        IReadOnlyList<Rect2> lastRegions = null;
        Measure("Guidance.RefreshNow", guidance.RefreshNow);
        Measure("CanyonFireflyGuidance.RefreshNow", () => fireflies.RefreshNow());
        Measure("WorldTargetMarker.ResolveProtectedRegions", () => lastRegions = marker.ResolveProtectedRegions());
        Measure("RoomProtectionRegionAdapter.CollectGameplay", () => lastRegions =
            RoomProtectionRegionAdapter.CollectGameplay(sectorScope, playerRoot));

        void Measure(string name, Action operation)
        {
            for (int index = 0; index < 4; index++) operation();
            FogAllocationStamp before = CaptureAllocation();
            for (int index = 0; index < 64; index++) operation();
            FogAllocationStamp after = CaptureAllocation();
            _localMethodCosts.Add(new LocalMethodCost(name, 64, after.Bytes - before.Bytes,
                (after.Usec - before.Usec) / 1000d, before.Thread, after.Thread,
                before.Process, after.Process, guidance.CurrentInstruction.Visible,
                marker.HasVisiblePrompt, marker.RenderedTargetCount, lastRegions?.Count ?? -1));
            TestAssert.True(!_collect && before.Thread == after.Thread && before.Process == after.Process &&
                after.Bytes >= before.Bytes && after.Usec >= before.Usec,
                "synchronous local measurement remains on one thread/frame outside natural collection");
            TestAssert.True(_flow.State == PrologueGameFlowState.Playing && !Paused && Engine.TimeScale == 1d &&
                _actor.GetInstanceId() == _baselinePlayer && _health.CurrentHealth == _baselineHealth &&
                _recoveries == 0 && _actor.IsOnFloor() && SupportId() == _baselineSupport &&
                _actor.GlobalPosition.DistanceTo(_baselinePosition) < .1f,
                "local presentation queries must not change living actor or native support");
        }
    }

    private readonly record struct LocalMethodCost(string Name, int Calls, long ThreadBytes,
        double ElapsedMs, int BeforeThread, int AfterThread, ulong BeforeProcess, ulong AfterProcess,
        bool InstructionVisible, bool HasVisiblePrompt, int Targets, int LastRegions);

    private static object DescribeAllocations(FogCostSampleBuffer buffer)
    {
        int accepted = 0, invalid = 0, contextRejected = 0;
        long between = 0, submission = 0, after = 0;
        foreach (FogCostSample sample in buffer.Raw)
        {
            if (!FogCostSampleBuffer.Admissible(sample)) { contextRejected++; continue; }
            if (!sample.Allocation.TryPartition(out FogAllocationSegments segments)) { invalid++; continue; }
            accepted++; between += segments.BetweenCallbacks;
            submission += segments.Submission; after += segments.AfterSubmission;
        }
        return new { accepted, invalid, contextRejected, betweenCallbackBytes = between,
            submissionBytes = submission, afterSubmissionBytes = after, totalBytes = between + submission + after };
    }

    private bool ConsumersReady()
    {
        if (_arm != 2 && (!_canvas.Visible || !_canvas.IsVisibleInTree() || !_canvas.FrameValid)) return false;
        for (int index = _arm == 2 ? 1 : 0; index < _drawNodes.Length; index++)
            if (!_drawNodes[index].IsInsideTree() || !_drawNodes[index].Visible ||
                !_drawNodes[index].IsVisibleInTree() || _drawNodes[index].GetInstanceId() != _ids[index]) return false;
        return true;
    }

    private bool SharedSnapshots(HeadlampOcclusionFrame current)
    {
        for (int index = 0; index < _snapshots.Length; index++)
        {
            HeadlampOcclusionFrame snapshot = _snapshots[index]();
            // Q has no snapshot when there is no candidate. No nonzero cue may lack the fresh frame.
            if (snapshot == null && index == 2 && _counts[index]() == 0) continue;
            if (!ReferenceEquals(snapshot, current)) return false;
        }
        return true;
    }

    private void SetNativeDrawing(bool visible)
    {
        foreach (CanvasItem item in _drawNodes)
            if (item.IsInsideTree()) RenderingServer.CanvasItemSetVisible(item.GetCanvasItem(), visible);
    }

    private ulong SupportId() => _actor.GetSlideCollisionCount() > 0
        ? _actor.GetLastSlideCollision().GetColliderId() : 0;

    private async Task Frames(int count)
    {
        for (int index = 0; index < count; index++) await ToSignal(this, SignalName.ProcessFrame);
    }

    private static double? Percentile(ReadOnlySpan<double> values, double fraction)
    {
        if (values.Length == 0) return null;
        double[] copy = values.ToArray(); Array.Sort(copy);
        return copy[Math.Clamp((int)Math.Ceiling(fraction * copy.Length) - 1, 0, copy.Length - 1)];
    }

    private void Boundary(string name) => _boundaries.Add(new PhaseBoundary(name, Time.GetTicksUsec(),
        Engine.GetProcessFrames(), Engine.GetPhysicsFrames(), GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)));

    private readonly record struct PhaseBoundary(string Name, ulong Usec, ulong Process, ulong Physics,
        int Gc0, int Gc1, int Gc2);
}
