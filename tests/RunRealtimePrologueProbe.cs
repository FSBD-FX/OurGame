using System;
using System.IO;
using System.Text.Json;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

/// <summary>
/// Diagnostic only: run with a display and without --fixed-fps / --write-movie.
/// The accepted normal continuous canyon route owns all gameplay and automatic input. No performance pass/fail
/// threshold or human-playability conclusion is inferred from this automated workload.
/// </summary>
public partial class RunRealtimePrologueProbe : SceneTree, ITask24ShieldEndTimingSink, ITask24ShieldDeferredOutputSink, IKeyboardInputTraceSink
{
    private readonly RealtimeFrameSampler _sampler = new();
    private readonly RealtimeLongFrameTrace _longFrames = new();
    private readonly Task24ShieldEndTiming _shieldEndTiming = new();
    private readonly Task24ShieldDeferredOutput _shieldOutput = new();
    private Task11PointerAdmissionTrace _pointerTrace;
    private bool _pointerStopped, _pointerSidecarAttempted;
    private string _pointerSetupError;
    private NodePath _routePath;
    private readonly NodePath _worldPath = new("WorldRoot/Sectors");
    private readonly NodePath _flowPath = new("GameFlow");
    private readonly NodePath _completionPath = new("Completion");
    private readonly NodePath _playerPath = new("PlayerRoot/Player");
    private readonly NodePath _recorderPath = new("PlaytestRecorder");
    private PrologueLevelController _route;
    private SeamlessPrologueWorld _world;
    private PrologueGameFlowController _flow;
    private PrologueCompletionController _completion;
    private CharacterBody2D _player;
    private bool _routeAcquired;
    private bool _sidecarAttempted;
    private bool _storageAccepted;
    private string _storageRoot, _sidecarPath, _expectedSave, _expectedSettings, _expectedPlaytests;
    private string _observerError;
    private bool _collecting;
    private bool _summaryPrinted;
    private int _focusLossCount;
    private bool _focusHookInstalled;
    private KeyboardInputTraceBuffer _keyboardTrace;
    private bool _keyboardSidecarAttempted;
    private readonly KeyboardFocusTraceBuffer _focusTrace = new();
    private KeyboardRouteFocusObserver _focusObserver;
    private bool _focusTraceArmed, _focusTraceStopped, _focusSidecarAttempted;

    public override async void _Initialize()
    {
        int exitCode = 1;
        string outcome = "failed";
        try
        {
            // As in the existing Windows test launchers, allow the first process frame before
            // rejecting arguments so Quit(1) also works for an initialization failure.
            await ToSignal(this, SignalName.ProcessFrame);
            string[] engineArguments = OS.GetCmdlineArgs();
            string[] userArguments = OS.GetCmdlineUserArgs();
            string[] managedArguments = System.Environment.GetCommandLineArgs();
            GD.Print($"PROLOGUE_REALTIME_COMMAND executable={JsonSerializer.Serialize(OS.GetExecutablePath())} " +
                $"engine_args={JsonSerializer.Serialize(engineArguments)} " +
                $"user_args={JsonSerializer.Serialize(userArguments)} " +
                $"managed_args={JsonSerializer.Serialize(managedArguments)} " +
                "managed_argv_completeness=not_independently_verified");
            PrintRuntimeConditions("start");
            RejectNonRealtimeArguments(engineArguments);
            RejectNonRealtimeArguments(userArguments);
            // Godot removes recognized engine options from OS.GetCmdlineArgs(). Consult
            // managed host argv too, without claiming that the host preserved every option.
            RejectNonRealtimeArguments(managedArguments);
            string moviePath = Engine.GetWriteMoviePath();
            GD.Print($"PROLOGUE_REALTIME_GUARD movie_path={JsonSerializer.Serialize(moviePath)} " +
                "fixed_fps_check=available_command_lines_only " +
                "host_rewritten_argv=absence_of_fixed_fps_cannot_be_verified");
            if (!string.IsNullOrEmpty(moviePath))
                throw new InvalidOperationException("Realtime probe rejects an active engine movie writer.");
            if (DisplayServer.GetName() == "headless")
                throw new InvalidOperationException("Realtime probe requires a visible display, not headless.");
            if (Math.Abs(Engine.TimeScale - 1d) > .000001d)
                throw new InvalidOperationException("Realtime probe requires the existing time scale to be 1; it will not change it.");

            string routeVariant = ValidateRouteArguments(userArguments);
            PrepareLongFrameDestination(userArguments, routeVariant);
            RealtimeRouteArgumentContractTests.Run();
            GD.Print($"PROLOGUE_REALTIME_ARGUMENT_TESTS_OK cases={RealtimeRouteArgumentContractTests.CaseCount} " +
                "phase=before_collection");
            RealtimeFrameSamplerTests.Run();
            GD.Print("PROLOGUE_REALTIME_SAMPLER_TESTS_OK cases=6 phase=before_collection");
            RealtimeLongFrameTraceTests.Run();
            GD.Print($"PROLOGUE_REALTIME_LONGFRAME_TESTS_OK cases={RealtimeLongFrameTraceTests.CaseCount} " +
                "phase=before_collection");
            Task11PointerAdmissionTraceTests.Run();
            GD.Print($"PROLOGUE_REALTIME_POINTER_TESTS_OK cases={Task11PointerAdmissionTraceTests.CaseCount} phase=before_collection");
            Task24ShieldEndTimingTests.Run();
            GD.Print($"PROLOGUE_REALTIME_SHIELD_END_TESTS_OK cases={Task24ShieldEndTimingTests.CaseCount} phase=before_collection");
            Task24ShieldDeferredOutputTests.Run();
            GD.Print($"PROLOGUE_REALTIME_SHIELD_DEFERRED_TESTS_OK cases={Task24ShieldDeferredOutputTests.CaseCount} phase=before_collection");
            PreparePointerTrace();
            PrepareFocusTrace();
            GD.Print("PROLOGUE_REALTIME_SCOPE automated_input=True telemetry_overhead=True " +
                $"workload=accepted_task10_normal_continuous_canyon_deferred_shield_output_diagnostic route_variant={routeVariant} " +
                "shield_output_fallback=actual_disposition_in_postcollection_sidecars baseline_workload_comparable=False " +
                "capture_frames=False window=shared_route_including_setup_teardown_and_instrumentation " +
                "warmup=first_120_callback_intervals_excluded " +
                "tail_after_last_callback=not_sampled " +
                "timing=process_callback_wall_clock_not_isolated_gpu_or_display_present " +
                "stable_60fps_claim=False human_feel_claim=False target_playtime_claim=False");
            GD.Print("PROLOGUE_REALTIME_FOCUS background_window=False " +
                $"focused={Root.HasFocus()} window_unfocusable={Root.Unfocusable}");
            Root.FocusExited += OnFocusExited;
            _focusHookInstalled = true;
            _shieldOutput.BeginCollection();
            _collecting = true;
            await ContinuousCanyonRouteProbe.Run(this);
            _collecting = false;
            outcome = "route_completed";
            exitCode = 0;
        }
        catch (Exception exception)
        {
            _collecting = false;
            GD.PushError("PROLOGUE_REALTIME_PROBE_FAILED: " + exception);
        }
        finally
        {
            _collecting = false;
            StopFocusTrace("runner_finally");
            WriteFocusSidecarOnce(outcome);
            WriteKeyboardSidecarOnce(outcome);
            DrainShieldOutputOnce(outcome);
            if (_focusHookInstalled) Root.FocusExited -= OnFocusExited;
            StopPointerTrace("runner_finally");
            PrintSummaryOnce(outcome);
            WriteLongFrameSidecarOnce(outcome);
            WritePointerSidecarOnce(outcome);
            WriteShieldOutputSidecarOnce();
            WriteShieldEndSidecarOnce(outcome);
            PrintRuntimeConditions("end");
            GD.Print($"PROLOGUE_REALTIME_PROBE_END outcome={outcome} exit_code={exitCode} " +
                $"focus_loss_count={_focusLossCount} " +
                "result_kind=automated_route_diagnostic_not_performance_certification");
            Quit(exitCode);
        }
    }

    public override bool _Process(double delta)
    {
        if (_collecting)
        {
            // One callback-start clock read, shared with the unchanged primary sampler.
            ulong callbackUsec = Time.GetTicksUsec();
            _sampler.Observe(callbackUsec);
            try { _longFrames.Observe(callbackUsec, CaptureLongFrameContext()); }
            catch (Exception exception)
            {
                // An ancillary observer must not interrupt or rewrite the route's result.
                _longFrames.MarkObserverFailure();
                _observerError ??= exception.ToString();
            }
        }
        return false;
    }

    private void OnFocusExited()
    {
        if (!_collecting) return;
        _focusLossCount++;
        GD.Print($"PROLOGUE_REALTIME_FOCUS_LOST physics_frame={Engine.GetPhysicsFrames()} " +
            "input_sequence_may_be_interrupted=True");
    }

    public override void _Finalize()
    {
        // Window close / an engine-requested early quit may interrupt the awaited route.
        // Emit its partial sample set too. Hard process termination cannot be intercepted.
        _collecting = false;
        StopFocusTrace("engine_exit_before_route_return");
        WriteFocusSidecarOnce("engine_exit_before_route_return");
        WriteKeyboardSidecarOnce("engine_exit_before_route_return");
        DrainShieldOutputOnce("engine_exit_before_route_return");
        StopPointerTrace("engine_exit_before_route_return");
        PrintSummaryOnce("engine_exit_before_route_return");
        WriteLongFrameSidecarOnce("engine_exit_before_route_return");
        WritePointerSidecarOnce("engine_exit_before_route_return");
        WriteShieldOutputSidecarOnce();
        WriteShieldEndSidecarOnce("engine_exit_before_route_return");
    }

    bool ITask24ShieldDeferredOutputSink.TryAcceptTask24ShieldOutput(Task24ShieldOutputBatch batch) =>
        _collecting && _shieldOutput.TryAcceptTask24ShieldOutput(batch);

    void IKeyboardInputTraceSink.RetainKeyboardInputTrace(KeyboardInputTraceBuffer trace) => _keyboardTrace ??= trace;

    private void PrepareFocusTrace()
    {
        try
        {
            _focusObserver=new KeyboardRouteFocusObserver { Name="RealtimeIndependentFocusObserver", Trace=_focusTrace };
            Root.AddChild(_focusObserver);
            _focusTraceArmed=true;
        }
        catch (Exception error) { _focusTrace.MarkFailure(error); }
    }
    private void StopFocusTrace(string reason)
    {
        if (_focusTraceStopped) return;
        _focusTraceStopped=true;
        try { _focusObserver?.Stop(reason); }
        catch (Exception error) { _focusTrace.MarkFailure(error); }
        finally { _focusTrace.Freeze(reason); }
    }
    private void WriteFocusSidecarOnce(string outcome)
    {
        if (_focusSidecarAttempted) return;
        _focusSidecarAttempted=true;
        try
        {
            if (_collecting) throw new InvalidOperationException("focus sidecar cannot serialize during collection");
            if (!_storageAccepted || !_focusTraceArmed)
            {
                PrintFocusSidecarStatus($"status=skipped storage_accepted={_storageAccepted} observer_armed={_focusTraceArmed}");
                return;
            }
            string path=KeyboardInputTraceSidecar.WriteFocus(true,_storageRoot,_focusTrace,outcome);
            PrintFocusSidecarStatus($"status=written path={JsonSerializer.Serialize(path)} diagnostic_only=True cause_proven=False");
        }
        catch (Exception error) { PrintFocusSidecarStatus("status=write_failed detail="+JsonSerializer.Serialize(error.ToString())); }
    }
    private static void PrintFocusSidecarStatus(string details)
    {
        try { GD.Print("PROLOGUE_REALTIME_OS_FOCUS "+details); }
        catch (Exception) { /* Ancillary output cannot replace the original route result. */ }
    }

    private void WriteKeyboardSidecarOnce(string outcome)
    {
        if(_keyboardSidecarAttempted) return;
        _keyboardSidecarAttempted=true;
        try
        {
            if(_collecting) throw new InvalidOperationException("keyboard sidecar cannot serialize during collection");
            if(!_storageAccepted || _keyboardTrace==null)
            {
                PrintKeyboardSidecarStatus($"status=skipped reason={(_storageAccepted ? "observer_not_retained" : "shared_route_isolated_storage_acceptance_not_observed")}");
                return;
            }
            // Only managed retained evidence here: route/player/window may already be freed.
            _keyboardTrace.Freeze("runner_finalization");
            string path=KeyboardInputTraceSidecar.Write(_storageAccepted,_storageRoot,_keyboardTrace,outcome);
            PrintKeyboardSidecarStatus($"status=written path={JsonSerializer.Serialize(path)} version=1 diagnostic_only=True complete_route_evidence=False");
        }
        catch(Exception exception)
        {
            PrintKeyboardSidecarStatus("status=write_failed detail="+JsonSerializer.Serialize(exception.ToString()));
        }
    }

    private static void PrintKeyboardSidecarStatus(string details)
    {
        try { GD.Print("PROLOGUE_REALTIME_KEYBOARD_TAIL "+details); }
        catch(Exception) { /* Ancillary status output cannot replace the original route result. */ }
    }

    void ITask24ShieldEndTimingSink.RecordTask24ShieldOutputDisposition(Task24ShieldOutputDisposition disposition)
    {
        _shieldEndTiming.RecordOutputDisposition(disposition);
        _shieldOutput.RecordDisposition(disposition);
    }

    private void DrainShieldOutputOnce(string outcome)
    {
        if (_collecting) return;
        _shieldOutput.StopCollection();
        // Native print failures are isolated per line by the model; the full accepted batch survives.
        _shieldOutput.Drain(line => GD.Print(line), outcome);
    }

    private void WriteShieldOutputSidecarOnce()
    {
        if (!_shieldOutput.TryClaimSidecarAttempt()) return;
        try
        {
            if (_collecting) throw new InvalidOperationException("deferred shield sidecar cannot serialize during collection");
            if (!_storageAccepted)
            {
                PrintShieldOutputStatus("status=skipped reason=shared_route_isolated_storage_acceptance_not_observed");
                return;
            }
            if (!Directory.Exists(_storageRoot)) throw new IOException("accepted case storage no longer exists");
            for (DirectoryInfo current = new(_storageRoot); current != null; current = current.Parent)
                if (!current.Exists || (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("accepted case storage has missing/reparse ancestor");
            string path = Path.Combine(_storageRoot, "task11-shield-deferred-output-v1.json");
            using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.Write(_shieldOutput.CreateJson());
            PrintShieldOutputStatus($"status=written path={JsonSerializer.Serialize(path)} " +
                "version=1 diagnostic_only=True output_after_collection=True global_stdout_order_may_change=True");
        }
        catch (Exception exception)
        {
            PrintShieldOutputStatus("status=write_failed " +
                $"error={JsonSerializer.Serialize(Task24ShieldOutputDispatch.ExceptionEvidence(exception))}");
        }
    }

    private static void PrintShieldOutputStatus(string details)
    {
        try { GD.Print("PROLOGUE_REALTIME_SHIELD_DEFERRED " + details); }
        catch (Exception) { /* Status output failure cannot rewrite storage status, route outcome or other sidecars. */ }
    }

    bool ITask24ShieldEndTimingSink.BeginTask24ShieldEnd(int observationErrors)
    {
        if (!_shieldEndTiming.Begin(observationErrors)) return false;
        CaptureShieldEndBoundary(Task24ShieldEndBoundary.T0, observationErrors);
        return true;
    }

    void ITask24ShieldEndTimingSink.MarkTask24ShieldEnd(Task24ShieldEndBoundary boundary, int observationErrors) =>
        CaptureShieldEndBoundary(boundary, observationErrors);

    private void CaptureShieldEndBoundary(Task24ShieldEndBoundary boundary, int observationErrors)
    {
        // Guard each native scalar independently; a failed read cannot suppress later reads/stamps.
        ulong? ticks = null, process = null, physics = null;
        try { ticks = Time.GetTicksUsec(); }
        catch (Exception exception) { _shieldEndTiming.MarkAcquisitionFailure("ticks_usec:" + exception.GetType().Name); }
        try { process = Engine.GetProcessFrames(); }
        catch (Exception exception) { _shieldEndTiming.MarkAcquisitionFailure("process_frame:" + exception.GetType().Name); }
        try { physics = Engine.GetPhysicsFrames(); }
        catch (Exception exception) { _shieldEndTiming.MarkAcquisitionFailure("physics_frame:" + exception.GetType().Name); }
        _shieldEndTiming.Mark(boundary, new Task24ShieldEndStamp(ticks, process, physics, observationErrors));
    }

    private void WriteShieldEndSidecarOnce(string outcome)
    {
        _shieldEndTiming.Freeze(outcome);
        if (!_shieldEndTiming.TryClaimSidecarAttempt()) return;
        // Even an ancillary status print must not replace the route outcome or other writes.
        try
        {
            if (_collecting) throw new InvalidOperationException("shield End sidecar cannot serialize during collection");
            if (!_storageAccepted)
            {
                GD.Print($"PROLOGUE_REALTIME_SHIELD_END outcome={outcome} status=skipped " +
                    "reason=shared_route_isolated_storage_acceptance_not_observed");
                return;
            }
            if (!Directory.Exists(_storageRoot)) throw new IOException("accepted case storage no longer exists");
            for (DirectoryInfo current = new(_storageRoot); current != null; current = current.Parent)
                if (!current.Exists || (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("accepted case storage has missing/reparse ancestor");
            string path = Path.Combine(_storageRoot, "task11-shield-end-timing-v2.json");
            using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.Write(_shieldEndTiming.CreateJson());
            GD.Print($"PROLOGUE_REALTIME_SHIELD_END outcome={outcome} status=written path={JsonSerializer.Serialize(path)} " +
                "version=2 diagnostic_only=True completion_status=see_sidecar");
        }
        catch (Exception exception)
        {
            try { GD.Print($"PROLOGUE_REALTIME_SHIELD_END outcome={outcome} status=write_failed " +
                $"error={JsonSerializer.Serialize(exception.ToString())}"); }
            catch (Exception) { /* Ancillary output failure must not replace the route outcome. */ }
        }
    }

    private void PreparePointerTrace()
    {
        try
        {
            // Arrays are preallocated before collection. No singleton or observer on other runners.
            _pointerTrace = new Task11PointerAdmissionTrace { Name = Task11PointerAdmissionTrace.RootChildName };
            Root.AddChild(_pointerTrace);
            _pointerTrace.SetProcessInput(false);
        }
        catch (Exception exception)
        {
            _pointerSetupError = exception.ToString();
            _pointerTrace?.Retention.MarkFailure(exception);
        }
    }

    private void StopPointerTrace(string reason)
    {
        if (_pointerStopped) return;
        _pointerStopped = true;
        if (_pointerTrace == null) return;
        _pointerTrace.Freeze(reason);
        _pointerTrace.Retention.Guard(() =>
        {
            if (!GodotObject.IsInstanceValid(_pointerTrace)) return;
            Node parent = _pointerTrace.GetParent();
            if (GodotObject.IsInstanceValid(parent)) parent.RemoveChild(_pointerTrace);
            _pointerTrace.Free(); // Retained managed fixed-array evidence is still available for serialization.
        });
    }

    private void WritePointerSidecarOnce(string outcome)
    {
        if (_pointerSidecarAttempted) return;
        _pointerSidecarAttempted = true;
        if (!_storageAccepted || _pointerTrace == null)
        {
            GD.Print($"PROLOGUE_REALTIME_POINTER_END outcome={outcome} status=skipped complete_route_evidence=False " +
                $"reason={(_storageAccepted ? "observer_not_created" : "shared_route_isolated_storage_acceptance_not_observed")} " +
                $"observer_error={JsonSerializer.Serialize(_pointerSetupError)}");
            return;
        }
        try
        {
            if (_collecting) throw new InvalidOperationException("pointer sidecar cannot serialize during collection");
            if (!Directory.Exists(_storageRoot)) throw new IOException("accepted case storage no longer exists");
            for (DirectoryInfo current = new(_storageRoot); current != null; current = current.Parent)
                if (!current.Exists || (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("accepted case storage has missing/reparse ancestor");
            string path = Path.Combine(_storageRoot, "task11-pointer-admission-v1.json");
            using (var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.Write(_pointerTrace.Retention.CreateJson(outcome));
            GD.Print($"PROLOGUE_REALTIME_POINTER_END outcome={outcome} status=written path={JsonSerializer.Serialize(path)} " +
                "version=1 diagnostic_only=True completion_status=see_sidecar");
        }
        catch (Exception exception)
        {
            GD.Print($"PROLOGUE_REALTIME_POINTER_END outcome={outcome} status=write_failed complete_route_evidence=False " +
                $"error={JsonSerializer.Serialize(exception.ToString())}");
        }
    }

    private void PrepareLongFrameDestination(string[] arguments, string routeVariant)
    {
        const string prefix = "--case-storage-root=";
        string argument = Array.Find(arguments, value => value.StartsWith(prefix, StringComparison.Ordinal));
        // This is only a candidate. The unchanged shared route owns freshness/isolation admission.
        _storageRoot = Path.GetFullPath(argument[prefix.Length..]);
        _sidecarPath = Path.Combine(_storageRoot, "task11-longframe-context-v1.json");
        _expectedSave = Path.Combine(_storageRoot, "save.json");
        _expectedSettings = Path.Combine(_storageRoot, "settings.json");
        _expectedPlaytests = Path.Combine(_storageRoot, "playtests");
        _routePath = new NodePath(routeVariant == "main" ? "Task10MainRouteProof" : "Task10ExplorationRouteProof");
    }

    private RealtimeLongFrameContext CaptureLongFrameContext()
    {
        ulong process = Engine.GetProcessFrames(), physics = Engine.GetPhysicsFrames();
        bool focus = Root.HasFocus();
        if (_observerError != null)
            return UnavailableContext(process, physics, focus, RealtimeContextAvailability.ObserverFailure);
        if (!_routeAcquired)
        {
            // Exactly one direct known-root lookup per callback until acquisition. CurrentScene is not used.
            _route = Root.GetNodeOrNull<PrologueLevelController>(_routePath);
            if (!Alive(_route)) return UnavailableContext(process, physics, focus, RealtimeContextAvailability.Setup);
            _routeAcquired = true;
            _world = _route.GetNodeOrNull<SeamlessPrologueWorld>(_worldPath);
            _flow = _route.GetNodeOrNull<PrologueGameFlowController>(_flowPath);
            _completion = _route.GetNodeOrNull<PrologueCompletionController>(_completionPath);
            _player = _route.GetNodeOrNull<CharacterBody2D>(_playerPath);
            ProloguePlaytestRecorder recorder = _route.GetNodeOrNull<ProloguePlaytestRecorder>(_recorderPath);
            // RunTask10 mounts this exact root only after its private persistence guard accepts
            // the fresh external directory and injects all three paths while off-tree.
            _storageAccepted = Alive(_flow) && Alive(recorder) &&
                string.Equals(_flow.SavePath, _expectedSave, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_flow.SettingsPath, _expectedSettings, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(recorder.OutputDirectory, _expectedPlaytests, StringComparison.OrdinalIgnoreCase);
            if (!Alive(_world) || !Alive(_flow) || !Alive(_completion) || !Alive(_player))
            {
                _longFrames.MarkObserverFailure();
                _observerError = "known_route_context_missing_at_acquisition";
                return UnavailableContext(process, physics, focus, RealtimeContextAvailability.ObserverFailure);
            }
        }
        if (!Alive(_route) || !Alive(_world) || !Alive(_flow) || !Alive(_completion) || !Alive(_player))
            return UnavailableContext(process, physics, focus, RealtimeContextAvailability.Teardown);
        Vector2 position = _player.GlobalPosition;
        if (!position.IsFinite()) throw new InvalidOperationException("long-frame observer found nonfinite player position");
        return new RealtimeLongFrameContext(process, physics, RealtimeContextAvailability.Available,
            _world.CurrentSector, _flow.State, _completion.CurrentObjective,
            _player.GetInstanceId(), position.X, position.Y, focus);
    }

    private static bool Alive(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree();

    private static RealtimeLongFrameContext UnavailableContext(ulong process, ulong physics,
        bool focus, RealtimeContextAvailability availability) =>
        new(process, physics, availability, null, null, null, null, null, null, focus);

    private void WriteLongFrameSidecarOnce(string outcome)
    {
        if (_sidecarAttempted) return;
        _sidecarAttempted = true;
        if (!_storageAccepted)
        {
            GD.Print($"PROLOGUE_REALTIME_LONGFRAME_END outcome={outcome} status=skipped " +
                "complete_diagnostic_evidence=False reason=shared_route_isolated_storage_acceptance_not_observed");
            return;
        }
        try
        {
            // Never create/rescue directories. Recheck existence and ancestor links after the route.
            // CreateNew refuses overwriting a prior sidecar or following an existing file link.
            if (!Directory.Exists(_storageRoot)) throw new IOException("accepted case storage no longer exists");
            for (DirectoryInfo current = new(_storageRoot); current != null; current = current.Parent)
                if (!current.Exists || (current.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("accepted case storage has missing/reparse ancestor");
            using (var stream = new FileStream(_sidecarPath, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
                writer.Write(_longFrames.CreateJson(outcome, _observerError));
            GD.Print($"PROLOGUE_REALTIME_LONGFRAME_END outcome={outcome} status=written " +
                $"path={JsonSerializer.Serialize(_sidecarPath)} version=1 " +
                "diagnostic_only=True completion_status=see_sidecar");
        }
        catch (Exception exception)
        {
            // Preserve the original route outcome/exit code even when the ancillary write fails.
            GD.Print($"PROLOGUE_REALTIME_LONGFRAME_END outcome={outcome} status=write_failed " +
                $"complete_diagnostic_evidence=False error={JsonSerializer.Serialize(exception.ToString())}");
        }
    }

    private static void RejectNonRealtimeArguments(string[] arguments)
    {
        foreach (string argument in arguments)
        {
            if (argument == "--fixed-fps" || argument.StartsWith("--fixed-fps=", StringComparison.Ordinal) ||
                argument == "--write-movie" || argument.StartsWith("--write-movie=", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Realtime probe rejects {argument}: fixed-step/movie output is not real-time sampling.");
        }
    }

    internal static string ValidateRouteArguments(string[] arguments)
    {
        const string variantPrefix = "--route-variant=";
        const string storagePrefix = "--case-storage-root=";
        string variant = null;
        bool storageSeen = false;
        bool keyboardSeen = false;
        foreach (string argument in arguments)
        {
            if (argument.StartsWith(variantPrefix, StringComparison.Ordinal))
            {
                if (variant != null)
                    throw new ArgumentException("Realtime probe requires exactly one route variant.");
                variant = argument[variantPrefix.Length..];
                if (variant is not ("main" or "exploration"))
                    throw new ArgumentException("Realtime probe route variant must be main or exploration.");
            }
            else if (argument.StartsWith(storagePrefix, StringComparison.Ordinal))
            {
                if (storageSeen)
                    throw new ArgumentException("Realtime probe requires exactly one case storage root.");
                storageSeen = true;
                string storage = argument[storagePrefix.Length..];
                // Fully qualified also rejects Windows drive-relative/root-relative paths.
                // The shared route retains its stricter fresh-path/reparse/default-data guards.
                if (string.IsNullOrWhiteSpace(storage) || !Path.IsPathFullyQualified(storage))
                    throw new ArgumentException("Realtime probe requires a nonempty absolute case storage root.");
            }
            else if (argument == "--keyboard-route")
            {
                if (keyboardSeen)
                    throw new ArgumentException("Realtime probe accepts at most one keyboard route selection.");
                keyboardSeen = true;
            }
            else
                throw new ArgumentException($"Realtime probe rejects unknown or shortcut user argument: {argument}");
        }
        if (variant == null || !storageSeen)
            throw new ArgumentException("Realtime probe requires exactly one --route-variant=main or exploration " +
                "and exactly one --case-storage-root=absolute-path.");
        return variant;
    }

    private void PrintSummaryOnce(string outcome)
    {
        if (_summaryPrinted) return;
        _summaryPrinted = true;
        GD.Print(_sampler.CreateSummary(outcome));
        GD.Print($"PROLOGUE_REALTIME_SAMPLE_FOCUS outcome={outcome} focus_loss_count={_focusLossCount} " +
            "focus_loss_failure_causality=not_established");
    }

    private void PrintRuntimeConditions(string phase)
    {
        string display = DisplayServer.GetName();
        GD.Print($"PROLOGUE_REALTIME_RUNTIME phase={phase} display={display} " +
            $"renderer={RenderingServer.GetCurrentRenderingMethod()} " +
            $"gpu={JsonSerializer.Serialize(RenderingServer.GetVideoAdapterName())} " +
            FormattableString.Invariant($"time_scale={Engine.TimeScale:0.000} physics_hz={Engine.PhysicsTicksPerSecond} max_fps={Engine.MaxFps}"));
        if (display == "headless") return;
        Vector2I size = DisplayServer.WindowGetSize();
        int screen = DisplayServer.WindowGetCurrentScreen();
        GD.Print($"PROLOGUE_REALTIME_WINDOW phase={phase} size={size.X}x{size.Y} " +
            $"viewport={Root.Size.X}x{Root.Size.Y} mode={DisplayServer.WindowGetMode()} " +
            $"root_visible={Root.Visible} focused={Root.HasFocus()} unfocusable={Root.Unfocusable} " +
            $"vsync={DisplayServer.WindowGetVsyncMode()} screen={screen} " +
            FormattableString.Invariant($"refresh_hz={DisplayServer.ScreenGetRefreshRate(screen):0.000}"));
    }
}
