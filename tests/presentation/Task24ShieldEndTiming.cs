using System.Text.Json;

namespace Knight.Tests.Presentation;

internal enum Task24ShieldEndBoundary { T0, T1, T2, T3 }

internal interface ITask24ShieldEndTimingSink
{
    bool BeginTask24ShieldEnd(int observationErrors);
    void MarkTask24ShieldEnd(Task24ShieldEndBoundary boundary, int observationErrors);
    void RecordTask24ShieldOutputDisposition(Task24ShieldOutputDisposition disposition);
}

// Nullable fields retain successful native reads even when another scalar read fails.
internal readonly record struct Task24ShieldEndStamp(ulong? TicksUsec, ulong? ProcessFrame,
    ulong? PhysicsFrame, int ObservationErrors)
{
    public bool Valid => TicksUsec.HasValue && ProcessFrame.HasValue && PhysicsFrame.HasValue;
}

/// <summary>One test-only receipt. No native calls, callbacks, scene state or on-path I/O.</summary>
internal sealed class Task24ShieldEndTiming
{
    private readonly Task24ShieldEndStamp[] _stamps = new Task24ShieldEndStamp[4];
    private readonly bool[] _attempted = new bool[4];
    private bool _begun, _frozen, _sidecarAttempted, _invalidOrder;
    private int _beginAttempts, _duplicateBegins, _boundaryAttempts, _duplicateBoundaries;
    private int _rejectedBoundaries, _retained, _lastBoundary = -1, _beginErrors;
    private int _acquisitionFailures;
    private string _firstAcquisitionError, _outcome;
    private bool _outputDispositionCaptured;
    private Task24ShieldOutputDisposition _outputDisposition;
    private int _outputDispositionFailures;
    private string _firstOutputDispositionError;

    public void RecordOutputDisposition(Task24ShieldOutputDisposition disposition)
    {
        if (_frozen || _outputDispositionCaptured) return;
        _outputDispositionCaptured = true;
        _outputDisposition = disposition;
        if (!disposition.IsKnown)
        {
            _outputDispositionFailures++;
            _firstOutputDispositionError = disposition.Error ?? "unknown_or_invalid_output_disposition";
        }
    }

    public bool Begin(int observationErrors)
    {
        if (_frozen) return false;
        _beginAttempts++;
        if (_begun) { _duplicateBegins++; return false; }
        _begun = true; _beginErrors = observationErrors;
        return true;
    }

    public bool Mark(Task24ShieldEndBoundary boundary, in Task24ShieldEndStamp stamp)
    {
        if (_frozen) return false;
        _boundaryAttempts++;
        int index = (int)boundary;
        if (!_begun || index < 0 || index >= 4)
        {
            _rejectedBoundaries++; _invalidOrder = true; return false;
        }
        if (_attempted[index])
        {
            _duplicateBoundaries++; _rejectedBoundaries++; return false;
        }
        // Keep first raw values even when acquisition order is invalid; never certify that order.
        if (index <= _lastBoundary) _invalidOrder = true;
        if (index > _lastBoundary) _lastBoundary = index;
        _attempted[index] = true; _stamps[index] = stamp; _retained++;
        return true;
    }

    public void MarkAcquisitionFailure(string error)
    {
        if (_frozen) return;
        _acquisitionFailures++;
        _firstAcquisitionError ??= error;
    }

    public void Freeze(string outcome)
    {
        if (_frozen) return;
        _frozen = true; _outcome = outcome;
    }

    public bool TryClaimSidecarAttempt()
    {
        if (!_frozen || _sidecarAttempted) return false;
        _sidecarAttempted = true; return true;
    }

    // Serialization/derivation is only used after the runner stops realtime collection.
    public string CreateJson()
    {
        bool partial = _begun && _retained != 4, invalidCounters = _begun && _beginErrors < 0;
        bool invalidStamp = false, errorsIncreased = false;
        int clockRegressions = 0, processRegressions = 0, physicsRegressions = 0, errorRegressions = 0;
        int maximumErrors = _beginErrors, previousErrors = _beginErrors;
        ulong? previousTicks = null, previousProcess = null, previousPhysics = null;
        var raw = new object[4];
        for (int index = 0; index < 4; index++)
        {
            Task24ShieldEndStamp stamp = _stamps[index]; bool attempted = _attempted[index];
            if (attempted)
            {
                invalidStamp |= !stamp.Valid; partial |= !stamp.Valid;
                if (stamp.TicksUsec.HasValue)
                {
                    if (previousTicks.HasValue && stamp.TicksUsec.Value < previousTicks.Value) clockRegressions++;
                    previousTicks = stamp.TicksUsec;
                }
                if (stamp.ProcessFrame.HasValue)
                {
                    if (previousProcess.HasValue && stamp.ProcessFrame.Value < previousProcess.Value) processRegressions++;
                    previousProcess = stamp.ProcessFrame;
                }
                if (stamp.PhysicsFrame.HasValue)
                {
                    if (previousPhysics.HasValue && stamp.PhysicsFrame.Value < previousPhysics.Value) physicsRegressions++;
                    previousPhysics = stamp.PhysicsFrame;
                }
                if (stamp.ObservationErrors < previousErrors) errorRegressions++;
                if (stamp.ObservationErrors < 0) invalidCounters = true;
                if (stamp.ObservationErrors > maximumErrors) maximumErrors = stamp.ObservationErrors;
                previousErrors = stamp.ObservationErrors;
            }
            raw[index] = new
            {
                boundary = ((Task24ShieldEndBoundary)index).ToString(), attempted,
                valid = attempted && stamp.Valid,
                ticks_usec = attempted ? stamp.TicksUsec : null,
                process_frame = attempted ? stamp.ProcessFrame : null,
                physics_frame = attempted ? stamp.PhysicsFrame : null,
                observation_errors = attempted ? (int?)stamp.ObservationErrors : null
            };
        }
        errorsIncreased = _begun && maximumErrors > _beginErrors;
        invalidCounters |= processRegressions > 0 || physicsRegressions > 0 || errorRegressions > 0;
        bool failure = _acquisitionFailures > 0 || invalidStamp || errorsIncreased;
        bool regressed = clockRegressions > 0;
        bool dispositionKnown = _outputDispositionCaptured && _outputDisposition.IsKnown;
        bool complete = _begun && !partial && !failure && !regressed && !_invalidOrder &&
            !invalidCounters && _rejectedBoundaries == 0 && dispositionKnown;
        string status = !_begun ? "unmeasured" : failure ? "failure" : regressed ? "regressed_clock" :
            _invalidOrder || invalidCounters || _rejectedBoundaries > 0 ? "invalid" : partial ? "partial" : "measured";
        return JsonSerializer.Serialize(new
        {
            version = 2, outcome = _outcome, status,
            result_kind = "one_shield_observation_end_span_diagnostic_not_performance_certification",
            frozen = _frozen, unmeasured = !_begun, partial, failure,
            clock_regressed = regressed, invalid_order = _invalidOrder, invalid_counters = invalidCounters,
            observation_errors_increased = errorsIncreased, measurement_complete = complete,
            route_completed = _outcome == "route_completed", complete_route_evidence = false,
            begin_attempts = _beginAttempts, duplicate_begins = _duplicateBegins,
            boundary_attempts = _boundaryAttempts, duplicate_boundaries = _duplicateBoundaries,
            rejected_boundaries = _rejectedBoundaries, retained_stamps = _retained, stamp_capacity = 4,
            begin_observation_errors = _begun ? (int?)_beginErrors : null,
            maximum_observation_errors = _begun ? (int?)maximumErrors : null,
            acquisition_failures = _acquisitionFailures, first_acquisition_error = _firstAcquisitionError,
            output_disposition = dispositionKnown ? _outputDisposition.Label : "unknown", output_disposition_known = dispositionKnown,
            raw_output_disposition = _outputDispositionCaptured ? _outputDisposition.Kind.ToString() : null,
            deferral_attempted = dispositionKnown ? (bool?)_outputDisposition.DeferralAttempted : null,
            output_dispatch_reason = _outputDisposition.Reason, output_dispatch_error = _outputDisposition.Error,
            output_disposition_failures = _outputDispositionFailures, first_output_disposition_error = _firstOutputDispositionError,
            clock_regressions = clockRegressions, process_frame_regressions = processRegressions,
            physics_frame_regressions = physicsRegressions, observation_error_regressions = errorRegressions,
            durations = new
            {
                final_record_usec = Segment(0, 1, _invalidOrder || invalidCounters),
                detach_usec = Segment(1, 2, _invalidOrder || invalidCounters),
                output_or_dispatch_usec = Segment(2, 3, _invalidOrder || invalidCounters),
                existing_output_usec = dispositionKnown && _outputDisposition.Kind == Task24ShieldOutputKind.Immediate
                    ? Segment(2, 3, _invalidOrder || invalidCounters) : null,
                total_usec = partial || regressed ? null : Segment(0, 3, _invalidOrder || invalidCounters)
            },
            stamps = raw,
            scalar_reads = "observation_time_separate_reads_not_atomic_engine_state",
            output_span = !dispositionKnown ? "unknown_output_or_dispatch_workload" :
                _outputDisposition.Kind == Task24ShieldOutputKind.Deferred
                    ? "snapshot_and_sink_acceptance_not_deferred_emission_or_waiting_time" :
                _outputDisposition.Kind == Task24ShieldOutputKind.ImmediateFallback
                    ? "attempted_snapshot_dispatch_and_immediate_output_not_baseline_comparable" :
                "existing_formatting_wrappers_native_output_sink_blocking_and_scheduling_not_isolated_logging_cpu_or_gpu",
            deferred_emission_duration_usec = (ulong?)null,
            baseline_workload_comparable = false,
            baseline_workload_comparability = "not_established_new_harness_instrumentation",
            instrumentation_adds_overhead = true, measurement_overhead_subtracted = false,
            causality = "not_established"
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private ulong? Segment(int first, int last, bool invalid)
    {
        if (invalid || !_attempted[first] || !_attempted[last] || !_stamps[first].Valid || !_stamps[last].Valid)
            return null;
        ulong start = _stamps[first].TicksUsec.Value, end = _stamps[last].TicksUsec.Value;
        return end < start ? null : end - start;
    }
}
