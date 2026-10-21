using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Expression = System.Linq.Expressions.Expression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunMinimapFrameDiagnostic : SceneTree
{
    private readonly MinimapFrameDiagnosticBuffer[] _arms = [new(), new(), new()];
    private readonly TaskCompletionSource<bool>[] _completion = [new(), new(), new()];
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private PrologueMinimapController _map;
    private PrologueMinimapView _view;
    private PrologueFogController _fog;
    private ExplorationMapModel _model;
    private CharacterBody2D _actor;
    private HealthComponent _health;
    private Node2D _world;
    private Action<FogOcclusionObservation> _original, _wrapper;
    private Func<Action<FogOcclusionObservation>> _binding;
    private IsolatedPrologueFixturePaths _paths;
    private Node.ProcessModeEnum _originalMode;
    private MinimapFrameStamp _pre, _after, _handlerFirst, _handlerLast;
    private Rid _viewport;
    private bool _collect, _hasPre, _hasAfter, _handlerAvailable, _changedBinding;
    private bool _preHook, _afterHook, _postHook, _watchHook, _focusHook, _recoveryHook, _timingEnabled;
    private bool _complete, _restored, _dumped;
    private int _arm = -1, _handlerCalls, _focusEvents, _recoveries;
    private long _handlerTicks, _handlerBytes, _started;
    private ulong _actorId, _worldId, _support, _generation;
    private Vector2 _position;
    private int _hp, _maxFps, _modelIdentity;
    private Vector2I _windowSize;
    private Rect2 _visibleRect;
    private DisplayServer.VSyncMode _vsync;
    private (float,float,float,float,float,int,int,int,bool,float,float,bool,bool,int,bool) _settings;
    private string _failure;
    private object _context;
    private readonly ulong[] _revisionStarts = new ulong[3], _revisionEnds = new ulong[3];
    // Bounded diagnostic observations, not an FPS benchmark: no I/O or allocations
    // on the callback/heartbeat path. Keep pre-restoration state even if drawing stops.
    private readonly WindowTrace[] _events = new WindowTrace[16], _heartbeats = new WindowTrace[128];
    private int _eventCount, _eventOverflow, _heartbeatCount, _heartbeatOverflow, _windowId = -1;
    private bool _focusLossControl, _controlRequested, _windowHooks;
    private long _nextHeartbeat, _preEntries, _afterEntries, _postEntries;
    private long _preEntry, _preExit, _afterEntry, _afterExit, _postEntry, _postExit;
    private readonly record struct WindowTrace(string Kind, long Ticks, ulong ProcessFrame, ulong PhysicsFrame,
        int DrawnFrames, int Arm, bool Collecting, int Endpoints, bool WindowAvailable,
        bool Visible, bool Focused, int Mode, int NativeMode, bool CanDraw, bool NativeCanDraw,
        int Width, int Height, bool RenderLoop, bool LowProcessor,
        long PreEntries, long PreEntry, long PreExit, long AfterEntries, long AfterEntry, long AfterExit,
        long PostEntries, long PostEntry, long PostExit);

    public override async void _Initialize()
    {
        _started = Stopwatch.GetTimestamp();
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            bool controls = args.Contains("--collector-controls");
            bool focusContract = args.Contains("--focus-interruption-contract");
            _focusLossControl = args.Contains("--focus-loss-control");
            TestAssert.True(args.Length == (controls || focusContract || _focusLossControl ? 2 : 1) &&
                args.Count(a => a is "--collector-controls" or "--focus-interruption-contract" or "--focus-loss-control") <= 1 &&
                args.Count(a => a.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1,
                "diagnostic accepts exactly one storage root and at most one exact diagnostic control");
            IsolatedPrologueFixture.ValidateStorageRoot();
            if (focusContract)
            {
                TestAssert.True(DisplayServer.GetName() == "headless", "focus contract is headless only");
                // Break caught: a known focus event must terminate an active arm
                // even when there will be no subsequent FramePostDraw callback.
                _arm = 0; _collect = true;
                OnFocus();
                TestAssert.True(_completion[0].Task.IsFaulted && !_collect,
                    "FocusChangeMustInterruptCollectionWithoutAnotherDraw: active arm faults immediately");
                TestAssert.True(_completion[0].Task.Exception?.InnerException is InvalidOperationException,
                    "focus interruption retains its explicit diagnostic failure");
                GD.Print("FOCUS_INTERRUPTION_CONTRACT_OK"); Quit(0); return;
            }
            if (controls)
            {
                TestAssert.True(DisplayServer.GetName() == "headless", "collector controls are headless only");
                MinimapFrameDiagnosticBufferTests.Run(); Quit(0); return;
            }
            foreach (string arg in OS.GetCmdlineArgs().Concat(System.Environment.GetCommandLineArgs()))
                TestAssert.True(!arg.StartsWith("--fixed-fps", StringComparison.Ordinal) &&
                    !arg.StartsWith("--write-movie", StringComparison.Ordinal) &&
                    !arg.StartsWith("--headless", StringComparison.Ordinal), "normal diagnostic rejects all fixed/movie/headless arguments");
            TestAssert.True(DisplayServer.GetName().Equals("Windows", StringComparison.OrdinalIgnoreCase) &&
                RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility" && Engine.TimeScale == 1 &&
                string.IsNullOrEmpty(Engine.GetWriteMoviePath()), "normal production GL visible Windows TimeScale1 no movie");

            await ToSignal(this, SignalName.ProcessFrame); // Match existing diagnostic: root has actually entered/Readied.
            RenderingServer.FramePreDraw += BeforeDraw; _preHook = true; // Before saved fog Ready.
            ProcessFrame += WatchDeadline; _watchHook = true;
            _level = ResourceLoader.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn",
                cacheMode: ResourceLoader.CacheMode.Ignore).Instantiate<PrologueLevelController>();
            _paths = IsolatedPrologueFixture.Configure(_level, "frame-attribution-first-normal");
            _flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
            _flow.HeadlessAutoStart = _flow.AutomationAutoStart = false;
            Root.AddChild(_level);
            if (!_level.IsNodeReady()) await ToSignal(_level, Node.SignalName.Ready);
            TestAssert.True(_level.IsNodeReady() && _level.GetNode<PrologueFogController>("FogController").IsNodeReady(),
                "actual saved level and fog Ready completed before installing after-pre hook");
            RenderingServer.FramePreDraw += AfterDrawPreparation; _afterHook = true;
            RenderingServer.FramePostDraw += AfterDraw; _postHook = true;
            _level.RecoveryRequested += OnRecovery; _recoveryHook = true;
            await Frames(5); _flow.StartNew(); await Frames(60);
            Root.Show(); Root.GrabFocus(); await Frames(5);
            _actor = _level.Player; _health = _actor.GetNode<HealthComponent>("Health");
            _world = _level.GetNode<Node2D>("WorldRoot");
            _map = _level.GetNode<PrologueMinimapController>("MinimapController");
            _view = _level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap");
            _fog = _level.GetNode<PrologueFogController>("FogController"); _model = _map.Model;
            _originalMode = _map.ProcessMode;
            _actorId = _actor.GetInstanceId(); _worldId = _world.GetInstanceId();
            _support = Support(); _position = _actor.GlobalPosition; _hp = _health.CurrentHealth;
            _generation = _map.WorldGeneration; _modelIdentity = RuntimeHelpers.GetHashCode(_model);
            _windowSize = Root.Size; _visibleRect = Root.GetVisibleRect(); _vsync = DisplayServer.WindowGetVsyncMode();
            _maxFps = Engine.MaxFps; _settings = Settings(_flow);
            TestAssert.True(Root.Visible && Root.HasFocus() && _actor.IsOnFloor() && _support != 0 && _hp > 0 &&
                _flow.State == PrologueGameFlowState.Playing && _recoveries == 0, "natural living supported focused saved scene");
            PrepareBinding(); // Reflection, delegate construction and invocation-list allocation only in setup.
            _windowId = Root.GetWindowId();
            Root.FocusEntered += OnFocusEntered; Root.FocusExited += OnFocusExited; _focusHook = true;
            Root.VisibilityChanged += OnVisibility; Root.SizeChanged += OnSize; _windowHooks = true;
            TraceEvent("sampling_ready");
            _viewport = Root.GetViewportRid(); RenderingServer.ViewportSetMeasureRenderTime(_viewport, true); _timingEnabled = true;
            _context = new { renderer = RenderingServer.GetCurrentRenderingMethod(), device = RenderingServer.GetVideoAdapterName(),
                display = DisplayServer.GetName(), windowWidth = _windowSize.X, windowHeight = _windowSize.Y,
                visibleRect = new { x = _visibleRect.Position.X, y = _visibleRect.Position.Y, width = _visibleRect.Size.X, height = _visibleRect.Size.Y },
                vsync = _vsync.ToString(), maxFps = _maxFps, timeScale = Engine.TimeScale, actor = _actorId,
                world = _worldId, support = _support, x = _position.X, y = _position.Y, hp = _hp,
                modelIdentity = _modelIdentity, generation = _generation, settings = _flow.Settings,
                installedSubscriberCount = 1, installedTarget = _original.Target.GetType().FullName,
                installedMethod = _original.Method.Name, originalProcessMode = _originalMode.ToString(),
                savedLevelReadyBeforeAfterHook = true,
                clock = "Stopwatch.GetTimestamp", frequency = Stopwatch.Frequency,
                args = OS.GetCmdlineArgs(), userArgs = args };
            GD.Print($"MINIMAP_FRAME_DIAGNOSTIC_START root={_paths.CaseRoot} normal=true visible={Root.Visible} focus={Root.HasFocus()} performanceCertificate=false");
            for (int arm = 0; arm < 3; arm++)
            {
                if (_focusEvents != 0) throw new InvalidOperationException("NormalFrameCollectionInterrupted: focus_changed");
                SetArm(arm); _arm = arm; _hasPre = _hasAfter = false;
                _revisionStarts[arm] = _model.RevisionCounter; _collect = true;
                await _completion[arm].Task; // One completion await per arm; no per-sample allocating ToSignal.
                _collect = false; _revisionEnds[arm] = _model.RevisionCounter;
                TestAssert.True(arm == 1 || _revisionEnds[arm] == _revisionStarts[arm], "off retains model and prevents observation advances");
                await Frames(1); // Outside collection, never substitutes an endpoint.
            }
            _complete = _arms.All(a => a.Count == 721 && a.Invalid == 0 && a.Overflow == 0 &&
                Enumerable.Range(1,720).All(i => a.TrySegments(i,out _))) && _focusEvents == 0;
            TestAssert.True(_complete, "all 2163 endpoints and adjacent boundaries required; invalid retained, never replaced");
        }
        catch (Exception exception) { _failure = exception.ToString(); _complete = false; }
        finally
        {
            _collect = false;
            if (_windowId >= 0) TraceEvent("before_restore");
            try { Restore(); } catch (Exception exception) { _failure ??= exception.ToString(); _complete = false; }
            try { Dump("normal_body_returned"); } catch (Exception exception) { _failure ??= exception.ToString(); _complete = false; }
            if (GodotObject.IsInstanceValid(_level) && _level.IsInsideTree())
                try
                {
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);
                    GD.Print("MINIMAP_FRAME_DIAGNOSTIC_TEARDOWN_OK");
                }
                catch (Exception exception) { _failure ??= exception.ToString(); _complete = false; }
        }
        if (_failure != null) GD.PushError(_failure);
        bool success = _complete && _restored && _dumped && _failure == null;
        GD.Print(success ? "MINIMAP_FRAME_DIAGNOSTIC_COMPLETE evidence_only=true" : "MINIMAP_FRAME_DIAGNOSTIC_PARTIAL_OR_INVALID");
        Quit(success ? 0 : 1);
    }

    private void PrepareBinding()
    {
        var method = typeof(PrologueMinimapController).GetMethod("OnObservation", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Actual map OnObservation unavailable");
        var field = typeof(PrologueFogController).GetField("ObservationPresented", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Actual observation backing field unavailable");
        var expression = Expression.Field(Expression.Constant(_fog), field);
        _binding = Expression.Lambda<Func<Action<FogOcclusionObservation>>>(expression).Compile();
        var installed = _binding(); var list = installed?.GetInvocationList();
        TestAssert.True(list is { Length: 1 } && ReferenceEquals(list[0].Target, _map) && list[0].Method == method,
            "sole installed subscriber must be actual map target and OnObservation; never reorder unknown subscribers");
        _original = (Action<FogOcclusionObservation>)list[0]; _wrapper = ObserveActualHandler;
        _fog.ObservationPresented -= _original; _fog.ObservationPresented += _wrapper; _changedBinding = true;
        TestAssert.True(ReferenceEquals(_binding(), _wrapper), "exact test wrapper replacement once");
    }

    private void SetArm(int arm)
    {
        TestAssert.True(!_collect, "arm transitions are off collection path");
        var expected = _arm == 1 || _arm == -1 ? _wrapper : null;
        TestAssert.True(ReferenceEquals(_binding(), expected), "subscription state before arm transition");
        if (_binding() != null) _fog.ObservationPresented -= _wrapper;
        if (arm == 1) _fog.ObservationPresented += _wrapper;
        _map.ProcessMode = arm == 1 ? Node.ProcessModeEnum.Always : Node.ProcessModeEnum.Disabled;
        if (arm != 1) _view.ClearPresentation();
    }

    private static MinimapFrameStamp Stamp() => new(Stopwatch.GetTimestamp(), Engine.GetProcessFrames(),
        Engine.GetPhysicsFrames(), System.Environment.ProcessId, System.Environment.CurrentManagedThreadId,
        GC.GetAllocatedBytesForCurrentThread(), GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

    private void ObserveActualHandler(FogOcclusionObservation observation)
    {
        if (!_collect) { _original(observation); return; }
        var before = Stamp();
        try { _original(observation); } // Exactly once; original exception is never swallowed or replaced.
        finally
        {
            var after = Stamp();
            if (_handlerCalls++ == 0) _handlerFirst = before;
            _handlerLast = after;
            bool same = before.Ticks > 0 && after.Ticks >= before.Ticks && before.Thread == after.Thread &&
                before.Frame == after.Frame && before.Process == after.Process && after.Bytes >= before.Bytes;
            _handlerAvailable &= same;
            if (same) { _handlerTicks += after.Ticks - before.Ticks; _handlerBytes += after.Bytes - before.Bytes; }
        }
    }

    private void BeforeDraw()
    {
        _preEntries++; _preEntry = Stopwatch.GetTimestamp();
        if (!_collect) { _preExit = Stopwatch.GetTimestamp(); return; }
        _pre = Stamp(); _hasPre = true; _hasAfter = false;
        _handlerCalls = 0; _handlerTicks = _handlerBytes = 0; _handlerAvailable = true;
        _handlerFirst = _handlerLast = default;
        _preExit = Stopwatch.GetTimestamp();
    }

    private void AfterDrawPreparation()
    {
        _afterEntries++; _afterEntry = Stopwatch.GetTimestamp();
        if (!_collect) { _afterExit = Stopwatch.GetTimestamp(); return; }
        _after = Stamp(); _hasAfter = true;
        _afterExit = Stopwatch.GetTimestamp();
    }

    private void AfterDraw()
    {
        _postEntries++; _postEntry = Stopwatch.GetTimestamp();
        if (!_collect) { _postExit = Stopwatch.GetTimestamp(); return; }
        var post = Stamp();
        bool hooks = _hasPre && _hasAfter && ReferenceEquals(_binding(), _arm == 1 ? _wrapper : null) &&
            _map.ProcessMode == (_arm == 1 ? Node.ProcessModeEnum.Always : Node.ProcessModeEnum.Disabled) &&
            (_arm == 1 ? _handlerCalls == 1 : _handlerCalls == 0);
        ulong support = Support(); Vector2 position = _actor.GlobalPosition;
        bool valid = _flow.State == PrologueGameFlowState.Playing && !Paused && Engine.TimeScale == 1 &&
            _actor.IsOnFloor() && _actor.GetInstanceId() == _actorId && _health.CurrentHealth == _hp && _hp > 0 &&
            position == _position && support == _support && support != 0 && _world.GetInstanceId() == _worldId &&
            ReferenceEquals(_model, _map.Model) && _map.WorldGeneration == _generation && _recoveries == 0 &&
            Root.Visible && Root.Size == _windowSize && Root.GetVisibleRect() == _visibleRect &&
            DisplayServer.WindowGetVsyncMode() == _vsync && Engine.MaxFps == _maxFps && Settings(_flow) == _settings;
        var buffer = _arms[_arm];
        var endpoint = new MinimapFrameEndpoint(_arm, buffer.Count, _hasPre ? _pre : default, _hasAfter ? _after : default, post,
            Root.HasFocus(), valid, hooks, _model.RevisionCounter, _handlerCalls, _handlerTicks, _handlerBytes,
            _handlerCalls > 0 && _handlerAvailable, Performance.GetMonitor(Performance.Monitor.TimeProcess),
            Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess), RenderingServer.ViewportGetMeasuredRenderTimeCpu(_viewport),
            RenderingServer.ViewportGetMeasuredRenderTimeGpu(_viewport), RenderingServer.GetFrameSetupTimeCpu(),
            RenderingServer.ViewportGetRenderInfo(_viewport, RenderingServer.ViewportRenderInfoType.Canvas,
                RenderingServer.ViewportRenderInfo.DrawCallsInFrame), _handlerFirst, _handlerLast,
            _world.GetInstanceId(), _actor.GetInstanceId(), support, _health.CurrentHealth, position.X, position.Y,
            _map.WorldGeneration, RuntimeHelpers.GetHashCode(_map.Model));
        endpoint = endpoint with { HooksValid = hooks && MinimapFrameDiagnosticBuffer.HandlerContained(endpoint) };
        buffer.Add(endpoint);
        _hasPre = _hasAfter = false;
        if (buffer.Count == 721) { _collect = false; _completion[_arm].TrySetResult(true); }
        _postExit = Stopwatch.GetTimestamp();
    }

    private void WatchDeadline()
    {
        long now = Stopwatch.GetTimestamp();
        if (_windowId >= 0 && now >= _nextHeartbeat)
        {
            if (_heartbeatCount < _heartbeats.Length) _heartbeats[_heartbeatCount++] = Snapshot("process_heartbeat", now);
            else _heartbeatOverflow++;
            _nextHeartbeat = now + Stopwatch.Frequency;
        }
        if (_focusLossControl && !_controlRequested && _collect && _arm == 0 && _arms[0].Count >= 10)
        {
            _controlRequested = true; TraceEvent("owned_minimize_request");
            Root.Mode = Window.ModeEnum.Minimized; // Negative control changes only this owned diagnostic window.
        }
        if (_arm >= 0 && !_completion[_arm].Task.IsCompleted &&
            (now - _started) / (double)Stopwatch.Frequency >= 110)
        {
            TraceEvent("internal_deadline");
            _collect = false;
            _completion[_arm].TrySetException(new TimeoutException("110s internal bound; all partial endpoints retained"));
        }
    }

    private void Restore()
    {
        if (_preHook) { RenderingServer.FramePreDraw -= BeforeDraw; _preHook = false; }
        if (_afterHook) { RenderingServer.FramePreDraw -= AfterDrawPreparation; _afterHook = false; }
        if (_postHook) { RenderingServer.FramePostDraw -= AfterDraw; _postHook = false; }
        if (_watchHook) { ProcessFrame -= WatchDeadline; _watchHook = false; }
        if (_focusHook) { Root.FocusEntered -= OnFocusEntered; Root.FocusExited -= OnFocusExited; _focusHook = false; }
        if (_windowHooks) { Root.VisibilityChanged -= OnVisibility; Root.SizeChanged -= OnSize; _windowHooks = false; }
        if (_recoveryHook) { _level.RecoveryRequested -= OnRecovery; _recoveryHook = false; }
        if (_timingEnabled) { RenderingServer.ViewportSetMeasureRenderTime(_viewport, false); _timingEnabled = false; }
        if (_changedBinding)
        {
            var current = _binding();
            TestAssert.True(current == null || ReferenceEquals(current, _wrapper), "restore refuses to discard unexpected subscribers");
            if (current != null) _fog.ObservationPresented -= _wrapper;
            _fog.ObservationPresented += _original; _map.ProcessMode = _originalMode;
            _restored = ReferenceEquals(_binding(), _original) && _map.ProcessMode == _originalMode && ReferenceEquals(_model, _map.Model);
            TestAssert.True(_restored, "exact original subscriber, mode and model restored before audio drain");
            _changedBinding = false;
        }
    }

    public override void _Finalize()
    {
        _collect = false;
        if (_paths != null && !_dumped)
        {
            _complete = false; _failure ??= "engine_exit_before_body_return";
            // Native tree/viewport already released; no RID or node operations here.
            try { Dump("engine_exit_partial"); } catch { }
        }
    }

    private void Dump(string outcome)
    {
        if (_dumped || _paths == null) return;
        TestAssert.True(!_collect, "serialization is strictly off collection path");
        var options = new JsonSerializerOptions { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
        var summaries = new List<object>();
        for (int arm = 0; arm < 3; arm++)
        {
            var buffer = _arms[arm]; var raw = buffer.Endpoints.Take(buffer.Count).ToArray();
            File.WriteAllText(Path.Combine(_paths.CaseRoot, $"frame-arm-{arm}-raw.json"), JsonSerializer.Serialize(raw, options));
            summaries.Add(Summarize(buffer, arm));
        }
        File.WriteAllText(Path.Combine(_paths.CaseRoot, "frame-diagnostic.json"), JsonSerializer.Serialize(new {
            outcome, complete = _complete, restored = _restored, failure = _failure, context = _context,
            frequency = Stopwatch.Frequency, elapsedSeconds = (Stopwatch.GetTimestamp() - _started) / (double)Stopwatch.Frequency,
            focusEvents = _focusEvents, recoveries = _recoveries, primeEndpoints = 1, warmupIntervals = 120, descriptiveIntervals = 600,
            interruptionTrace = new { diagnosticOnly = true, focusLossControl = _focusLossControl, controlRequested = _controlRequested,
                events = _events.Take(_eventCount).ToArray(), eventOverflow = _eventOverflow,
                heartbeats = _heartbeats.Take(_heartbeatCount).ToArray(), heartbeatOverflow = _heartbeatOverflow },
            performanceCertificate = false, oldV003Result = "unchanged FAIL / INVALID; no comparison predicate evaluated here",
            overhead = "nonzero: stamps, GC counters, getters, callback entry/exit counters and ~1Hz window snapshots; diagnostic only, not directly comparable to prior version; no per-frame IO/reflection/delegate creation/ToSignal",
            bracketBasis = "pre-to-after ALL intervening FramePreDraw subscribers including fog and map; wall clock not exclusive CPU",
            handlerBasis = "actual bound OnObservation exactly once; wall time includes preemption/GC/native wait; map _Process/Present is outside bracket",
            remainderBasis = "previous-post-to-pre is unassigned wall time, NOT proven OS scheduling/VSync/GPU wait",
            monitorBasis = "installed GodotSharp XML: TimeProcess/TimePhysicsProcess seconds; viewport CPU/GPU last-frame milliseconds; setup milliseconds; zero unavailable",
            gcBasis = "process collection-count increments are event occurrence, NOT GC pause duration",
            byteBasis = "same-managed-thread adjacent stamps including observer; FogAllocationWindow.TryPartition reused; discontinuity unavailable not zero",
            limitations = "static natural entrance only, no native ETW/GPU scheduling trace, no broad-game/art/human certification", arms = summaries
        }, options));
        _dumped = true;
    }

    private static object Summarize(MinimapFrameDiagnosticBuffer buffer, int arm)
    {
        var interval = new List<double>(); var pre = new List<double>(); var before = new List<double>(); var after = new List<double>();
        var bytesBefore = new List<double>(); var bytesPre = new List<double>(); var bytesAfter = new List<double>();
        var handler = new List<double>(); var handlerBytes = new List<double>(); var process = new List<double>(); var physics = new List<double>();
        var cpu = new List<double>(); var gpu = new List<double>(); var setup = new List<double>(); var draw = new List<double>();
        var gcIntervals = new List<double>(); var nonGcIntervals = new List<double>();
        int unavailable = 0, gc0 = 0, gc1 = 0, gc2 = 0, handlerCalls = 0;
        for (int i = 121; i < buffer.Count; i++)
        {
            var e = buffer.Endpoints[i]; var p = buffer.Endpoints[i - 1]; handlerCalls += e.HandlerCalls;
            gc0 += e.Post.Gc0 - p.Post.Gc0; gc1 += e.Post.Gc1 - p.Post.Gc1; gc2 += e.Post.Gc2 - p.Post.Gc2;
            if (!buffer.TrySegments(i, out var s)) { unavailable++; continue; }
            double ms = 1000d / Stopwatch.Frequency;
            interval.Add(s.PostIntervalTicks * ms); before.Add(s.BetweenPostsToPreTicks * ms);
            pre.Add(s.AllPreDrawSubscribersTicks * ms); after.Add(s.AfterPreToPostTicks * ms);
            bytesBefore.Add(s.Bytes.BetweenCallbacks); bytesPre.Add(s.Bytes.Submission); bytesAfter.Add(s.Bytes.AfterSubmission);
            if (e.HandlerAvailable) { handler.Add(e.HandlerTicks * ms); handlerBytes.Add(e.HandlerBytes); }
            process.Add(e.TimeProcessSeconds > 0 ? e.TimeProcessSeconds * 1000 : double.NaN);
            physics.Add(e.TimePhysicsSeconds > 0 ? e.TimePhysicsSeconds * 1000 : double.NaN);
            cpu.Add(e.RenderCpuMs > 0 ? e.RenderCpuMs : double.NaN); gpu.Add(e.RenderGpuMs > 0 ? e.RenderGpuMs : double.NaN);
            setup.Add(e.SetupCpuMs > 0 ? e.SetupCpuMs : double.NaN); draw.Add(e.DrawCalls > 0 ? e.DrawCalls : double.NaN);
            ((e.Post.Gc0 > p.Post.Gc0 || e.Post.Gc1 > p.Post.Gc1 || e.Post.Gc2 > p.Post.Gc2) ? gcIntervals : nonGcIntervals).Add(s.PostIntervalTicks * ms);
        }
        return new { arm, name = arm == 1 ? "on" : arm == 0 ? "off" : "off_recovered", buffer.Count, buffer.Invalid, buffer.Overflow,
            measuredUnavailable = unavailable, firstBoundaryUnavailable = true,
            revisions = new { start = buffer.Count == 0 ? (ulong?)null : buffer.Endpoints[0].ModelRevision,
                end = buffer.Count == 0 ? (ulong?)null : buffer.Endpoints[buffer.Count - 1].ModelRevision },
            intervalMs = Describe(interval), previousPostToPreMs = Describe(before), allPreDrawSubscribersMs = Describe(pre), afterPreToPostMs = Describe(after),
            handlerMs = Describe(handler), handlerThreadBytes = Describe(handlerBytes), handlerCalls,
            bytesPreviousPostToPre = Describe(bytesBefore), bytesAllPreDraw = Describe(bytesPre), bytesAfterPreToPost = Describe(bytesAfter),
            timeProcessMs = Describe(process), timePhysicsMs = Describe(physics), previousViewportCpuMs = Describe(cpu), previousViewportGpuMs = Describe(gpu), setupCpuMs = Describe(setup), drawCalls = Describe(draw),
            gc = new { gc0, gc1, gc2, intervalsWithCollectionEventMs = Describe(gcIntervals), intervalsWithoutCollectionEventMs = Describe(nonGcIntervals) },
            descriptivePearsonOnly = new { intervalVsAllPreDraw = Correlation(interval, pre), intervalVsPreviousPostToPre = Correlation(interval, before),
                intervalVsAfterPreToPost = Correlation(interval, after), intervalVsPreviousViewportCpu = Correlation(interval, cpu),
                intervalVsPreviousViewportGpu = Correlation(interval, gpu), intervalVsTimeProcessMonitor = Correlation(interval, process) } };
    }

    private static object Describe(List<double> values)
    {
        var sorted = values.Where(double.IsFinite).Order().ToArray();
        double? P(double fraction) => sorted.Length == 0 ? null : sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * fraction) - 1,0,sorted.Length-1)];
        return new { available = sorted.Length, unavailable = values.Count - sorted.Length,
            p50 = P(.5), p95 = P(.95), p99 = P(.99), max = P(1) };
    }

    private static double? Correlation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count) return null;
        var pairs = x.Zip(y).Where(p => double.IsFinite(p.First) && double.IsFinite(p.Second)).ToArray();
        if (pairs.Length < 2) return null;
        double mx = pairs.Average(p => p.First), my = pairs.Average(p => p.Second);
        double covariance = 0, vx = 0, vy = 0;
        foreach (var p in pairs) { double dx = p.First - mx, dy = p.Second - my; covariance += dx * dy; vx += dx * dx; vy += dy * dy; }
        return vx <= 0 || vy <= 0 ? null : covariance / Math.Sqrt(vx * vy);
    }

    private void OnFocusEntered() => OnFocus("focus_entered");
    private void OnFocusExited() => OnFocus("focus_exited");
    private void OnFocus(string kind = "focus_contract")
    {
        _focusEvents++; TraceEvent(kind);
        if (_collect && _arm >= 0 && !_completion[_arm].Task.IsCompleted)
        {
            _collect = false;
            _completion[_arm].TrySetException(new InvalidOperationException("NormalFrameCollectionInterrupted: focus_changed"));
        }
    }
    private void OnVisibility() => TraceEvent("visibility_changed");
    private void OnSize() => TraceEvent("size_changed");
    private void TraceEvent(string kind)
    {
        if (_eventCount < _events.Length) _events[_eventCount++] = Snapshot(kind, Stopwatch.GetTimestamp());
        else _eventOverflow++;
    }
    private WindowTrace Snapshot(string kind, long now)
    {
        bool available = _windowId >= 0;
        return new(kind, now, Engine.GetProcessFrames(), Engine.GetPhysicsFrames(), Engine.GetFramesDrawn(),
            _arm, _collect, _arm >= 0 ? _arms[_arm].Count : 0, available,
            available && Root.Visible, available && Root.HasFocus(), available ? (int)Root.Mode : -1,
            available ? (int)DisplayServer.WindowGetMode(_windowId) : -1,
            available && Root.CanDraw(), available && DisplayServer.WindowCanDraw(_windowId),
            available ? Root.Size.X : 0, available ? Root.Size.Y : 0,
            RenderingServer.RenderLoopEnabled, OS.LowProcessorUsageMode,
            _preEntries, _preEntry, _preExit, _afterEntries, _afterEntry, _afterExit,
            _postEntries, _postEntry, _postExit);
    }
    private void OnRecovery(string reason) => _recoveries++;
    private ulong Support() => _actor.GetSlideCollisionCount() > 0 && _actor.GetSlideCollision(0).GetCollider() is GodotObject collider ? collider.GetInstanceId() : 0;
    private async Task Frames(int count) { for (int i=0;i<count;i++) await ToSignal(this, SignalName.ProcessFrame); }
    private static (float,float,float,float,float,int,int,int,bool,float,float,bool,bool,int,bool) Settings(PrologueGameFlowController flow)
    {
        var s=flow.Settings;
        return(s.MasterVolume,s.MusicVolume,s.EffectsVolume,s.AmbienceVolume,s.UiVolume,(int)s.WindowMode,s.ResolutionWidth,s.ResolutionHeight,s.VSync,s.UiScale,s.ScreenShake,s.ReduceFlashes,s.ReducedFog,(int)s.Language,s.GuidanceEnabled);
    }
}
