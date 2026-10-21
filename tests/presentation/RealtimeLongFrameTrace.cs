using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Presentation;

internal enum RealtimeContextAvailability { Setup, Available, Teardown, ObserverFailure }

internal readonly record struct RealtimeLongFrameContext(ulong ProcessFrame, ulong PhysicsFrame,
    RealtimeContextAvailability Availability, PrologueSectorId? Sector, PrologueGameFlowState? Flow,
    PrologueObjective? Objective, ulong? PlayerInstanceId, float? PlayerX, float? PlayerY, bool Focused);
internal readonly record struct RealtimeLongFrameEndpoint(ulong CallbackUsec, RealtimeLongFrameContext Context);
internal readonly record struct RealtimeLongFrameRecord(int SampleIndex, ulong ElapsedUsec,
    RealtimeLongFrameEndpoint Start, RealtimeLongFrameEndpoint End);

/// <summary>
/// Ancillary bounded test-only endpoint trace. Observe neither allocates nor sorts nor performs I/O.
/// Mirrors the unchanged sampler's valid-interval warmup, regression reset and accepted capacity.
/// </summary>
internal sealed class RealtimeLongFrameTrace
{
    private const int SampleCapacity = 262144;
    private const int FirstCapacity = 256;
    private const int WorstCapacity = 16;
    private readonly RealtimeLongFrameRecord[] _first = new RealtimeLongFrameRecord[FirstCapacity];
    private readonly RealtimeLongFrameRecord[] _worst = new RealtimeLongFrameRecord[WorstCapacity];
    private RealtimeLongFrameEndpoint _previous;
    private bool _primed;
    private int _warmup, _samples, _overflow, _regressions, _totalLong, _over50;
    private int _firstCount, _worstCount, _observerFailures;
    private int _setupCallbacks, _availableCallbacks, _teardownCallbacks, _failedCallbacks;

    public void MarkObserverFailure() => _observerFailures++;

    public void Observe(ulong callbackUsec, in RealtimeLongFrameContext context)
    {
        switch (context.Availability)
        {
            case RealtimeContextAvailability.Setup: _setupCallbacks++; break;
            case RealtimeContextAvailability.Available: _availableCallbacks++; break;
            case RealtimeContextAvailability.Teardown: _teardownCallbacks++; break;
            case RealtimeContextAvailability.ObserverFailure: _failedCallbacks++; break;
        }
        var endpoint = new RealtimeLongFrameEndpoint(callbackUsec, context);
        if (!_primed) { _primed = true; _previous = endpoint; return; }
        RealtimeLongFrameEndpoint start = _previous;
        _previous = endpoint;
        if (callbackUsec < start.CallbackUsec) { _regressions++; return; }
        ulong elapsed = callbackUsec - start.CallbackUsec;
        if (_warmup < RealtimeFrameSampler.WarmupIntervalCount) { _warmup++; return; }
        if (_samples == SampleCapacity) { _overflow++; return; }
        _samples++;
        if (elapsed <= 33300UL) return;
        _totalLong++;
        if (elapsed > 50000UL) _over50++;
        var record = new RealtimeLongFrameRecord(_samples, elapsed, start, endpoint);
        if (_firstCount < FirstCapacity) _first[_firstCount++] = record;
        if (_worstCount < WorstCapacity) { _worst[_worstCount++] = record; return; }
        // Keep an unordered fixed-size top set. Equal durations favor earlier sample identity.
        int least = 0;
        for (int i = 1; i < _worstCount; i++)
            if (_worst[i].ElapsedUsec < _worst[least].ElapsedUsec ||
                (_worst[i].ElapsedUsec == _worst[least].ElapsedUsec &&
                 _worst[i].SampleIndex > _worst[least].SampleIndex)) least = i;
        if (elapsed > _worst[least].ElapsedUsec) _worst[least] = record;
    }

    // Called only after the driver stops collection. All serialization allocations are off-path.
    public string CreateJson(string outcome, string observerError)
    {
        var first = new RealtimeLongFrameRecord[_firstCount];
        var worst = new RealtimeLongFrameRecord[_worstCount];
        Array.Copy(_first, first, _firstCount); Array.Copy(_worst, worst, _worstCount);
        Array.Sort(worst, static (a, b) =>
        {
            int duration = b.ElapsedUsec.CompareTo(a.ElapsedUsec);
            return duration != 0 ? duration : a.SampleIndex.CompareTo(b.SampleIndex);
        });
        int duplicates = 0;
        foreach (RealtimeLongFrameRecord record in worst)
            for (int i = 0; i < _firstCount; i++)
                if (_first[i].SampleIndex == record.SampleIndex) { duplicates++; break; }
        int unique = _firstCount + _worstCount - duplicates;
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Serialize(new
        {
            version = 1, outcome,
            result_kind = "bounded_automated_route_location_diagnostic_not_performance_certification",
            complete_diagnostic_evidence = outcome == "route_completed" && _samples > 0 &&
                _overflow == 0 && _regressions == 0 && _observerFailures == 0 &&
                _failedCallbacks == 0 && observerError == null && _availableCallbacks > 0,
            observer_failures = _observerFailures, observer_error = observerError,
            basis = "process_callback_start_wall_clock_not_isolated_gpu_or_display_present",
            observer_overhead_affects_following_intervals = true,
            cpu_gpu_gc_causality = "not_established", gc_counters_collected = false,
            tail_after_last_callback = "not_sampled",
            scope = "unchanged_shared_route_including_setup_teardown_and_instrumentation",
            warmup_required = RealtimeFrameSampler.WarmupIntervalCount, warmup_discarded = _warmup,
            sample_capacity = SampleCapacity, samples = _samples,
            overflow_discarded = _overflow, clock_regressions = _regressions,
            strict_long_threshold_usec = 33300, strict_second_threshold_usec = 50000,
            long_intervals_total = _totalLong, over_50_ms = _over50,
            first_capacity = FirstCapacity, first_retained = _firstCount, first_omitted = _totalLong - _firstCount,
            worst_capacity = WorstCapacity, worst_retained = _worstCount, worst_omitted = _totalLong - _worstCount,
            duplicate_records_between_lists = duplicates, unique_retained = unique, unique_omitted = _totalLong - unique,
            first_order = "chronological", worst_order = "elapsed_descending_then_earlier_sample_index",
            duplicates = "same_sample_index_in_first_and_worst_is_one_interval_not_two",
            setup_callbacks = _setupCallbacks, available_callbacks = _availableCallbacks,
            teardown_callbacks = _teardownCallbacks, observer_failure_callbacks = _failedCallbacks,
            context_limits = "endpoint_observations_only_not_continuous_state_between_callbacks; null_fields_are_unavailable; setup_before_acquisition_teardown_after_loss",
            first_long_intervals = first, worst_long_intervals = worst
        }, options);
    }
}
