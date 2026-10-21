using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Knight.Tests.Presentation;

internal interface ITask24ShieldDeferredOutputSink
{
    // Atomic ownership: false leaves the original immediate path responsible for all output.
    bool TryAcceptTask24ShieldOutput(Task24ShieldOutputBatch batch);
}

internal enum Task24ShieldOutputKind { Unknown, Immediate, Deferred, ImmediateFallback }

internal readonly record struct Task24ShieldOutputDisposition(Task24ShieldOutputKind Kind, string Reason, string Error)
{
    public bool IsKnown => Kind switch
    {
        Task24ShieldOutputKind.Immediate or Task24ShieldOutputKind.Deferred => Reason == null && Error == null,
        Task24ShieldOutputKind.ImmediateFallback => !string.IsNullOrEmpty(Reason),
        _ => false
    };
    public bool DeferralAttempted => Kind == Task24ShieldOutputKind.Deferred || Kind == Task24ShieldOutputKind.ImmediateFallback;
    public string Label => Kind switch
    {
        Task24ShieldOutputKind.Immediate => "immediate",
        Task24ShieldOutputKind.Deferred => "deferred",
        Task24ShieldOutputKind.ImmediateFallback => "immediate_fallback",
        _ => "unknown"
    };
}

/// <summary>Private copied string references only; no delegate, observer, native node or scene retained.</summary>
internal sealed class Task24ShieldOutputBatch
{
    internal const int MaximumRows = 256;
    internal const int MaximumCharacters = 1_048_576;
    public IReadOnlyList<string> Lines { get; }
    public int RowCount { get; }
    public int CharacterCount { get; }
    public int ObservationErrors { get; }
    public string FirstObservationError { get; }

    private Task24ShieldOutputBatch(string[] lines, int rows, int characters, int errors, string firstError)
    {
        Lines = Array.AsReadOnly(lines); RowCount = rows; CharacterCount = characters;
        ObservationErrors = errors; FirstObservationError = firstError;
    }

    // The complete block is admitted or rejected; never truncate or mutate the original evidence.
    public static bool TryCreate(IReadOnlyList<string> lines, int rows, int errors, string firstError,
        out Task24ShieldOutputBatch batch, out string reason)
    {
        batch = null; reason = null;
        if (rows < 0 || rows > MaximumRows) { reason = "row_limit"; return false; }
        if (lines == null || lines.Count != rows + 2 || errors < 0 || firstError == null)
        { reason = "invalid_snapshot"; return false; }
        var copy = new string[lines.Count];
        int characters = 0;
        for (int i = 0; i < copy.Length; i++)
        {
            string line = lines[i];
            if (line == null) { reason = "invalid_snapshot"; return false; }
            if (line.Length > MaximumCharacters - characters) { reason = "character_limit"; return false; }
            characters += line.Length; copy[i] = line;
        }
        batch = new(copy, rows, characters, errors, firstError); return true;
    }
}

internal static class Task24ShieldOutputDispatch
{
    // createBatch is used synchronously, never retained. Unsupported runners do no snapshot work.
    public static Task24ShieldOutputDisposition Dispatch(object candidate, Func<Task24ShieldOutputBatch> createBatch)
    {
        if (candidate is not ITask24ShieldDeferredOutputSink sink)
            return new(Task24ShieldOutputKind.Immediate, null, null);
        Task24ShieldOutputBatch batch;
        try { batch = createBatch(); }
        catch (Exception exception)
        { return new(Task24ShieldOutputKind.ImmediateFallback, "snapshot_failed", ExceptionEvidence(exception)); }
        if (batch == null) return new(Task24ShieldOutputKind.ImmediateFallback, "snapshot_rejected", null);
        try
        {
            return sink.TryAcceptTask24ShieldOutput(batch)
                ? new(Task24ShieldOutputKind.Deferred, null, null)
                : new(Task24ShieldOutputKind.ImmediateFallback, "sink_rejected", null);
        }
        catch (Exception exception)
        { return new(Task24ShieldOutputKind.ImmediateFallback, "sink_failed", ExceptionEvidence(exception)); }
    }

    internal static string ExceptionEvidence(Exception exception)
    {
        try { return exception.ToString(); }
        catch (Exception) { return "exception_details_unavailable"; }
    }
}

/// <summary>One batch, one collection lifecycle, one claimed drain/write; no queue or per-frame work.</summary>
internal sealed class Task24ShieldDeferredOutput : ITask24ShieldDeferredOutputSink
{
    private readonly object _gate = new();
    private Task24ShieldOutputBatch _batch;
    private string[] _lineStatus;
    private bool _started, _collecting, _stopped, _drainClaimed, _drainCompleted, _sidecarAttempted;
    private bool _dispositionCaptured;
    private Task24ShieldOutputDisposition _disposition;
    private int _acceptanceAttempts, _acceptanceRejected, _attempted, _succeeded, _failed;
    private string _firstEmissionError, _outcome;

    public bool BeginCollection()
    {
        lock (_gate)
        {
            if (_started || _stopped || _drainClaimed) return false;
            _started = true; _collecting = true; return true;
        }
    }

    public void StopCollection() { lock (_gate) { _collecting = false; _stopped = true; } }

    public bool TryAcceptTask24ShieldOutput(Task24ShieldOutputBatch batch)
    {
        lock (_gate)
        {
            if (_drainClaimed) return false;
            _acceptanceAttempts++;
            if (!_collecting || _stopped || _batch != null || batch == null)
            { _acceptanceRejected++; return false; }
            // Prepare everything which can allocate before committing the owned batch.
            var statuses = new string[batch.Lines.Count]; Array.Fill(statuses, "pending");
            _lineStatus = statuses; _batch = batch; return true;
        }
    }

    public void RecordDisposition(Task24ShieldOutputDisposition disposition)
    {
        lock (_gate)
        {
            if (_dispositionCaptured || _drainClaimed) return;
            _dispositionCaptured = true; _disposition = disposition;
        }
    }

    public bool Drain(Action<string> emit, string outcome)
    {
        lock (_gate)
        {
            if (_collecting || !_stopped || _drainClaimed) return false;
            // Claim before the first print: partial failure can never replay the accepted block.
            _drainClaimed = true; _outcome = outcome;
        }
        if (_batch != null)
        {
            for (int i = 0; i < _batch.Lines.Count; i++)
            {
                _attempted++;
                try { emit(_batch.Lines[i]); _succeeded++; _lineStatus[i] = "succeeded"; }
                catch (Exception exception)
                {
                    _failed++; _lineStatus[i] = "failed";
                    _firstEmissionError ??= Task24ShieldOutputDispatch.ExceptionEvidence(exception);
                }
            }
        }
        lock (_gate) { _drainCompleted = true; }
        return true;
    }

    public bool TryClaimSidecarAttempt()
    {
        lock (_gate)
        {
            if (!_drainCompleted || _sidecarAttempted) return false;
            _sidecarAttempted = true; return true;
        }
    }

    public string CreateJson()
    {
        lock (_gate)
        {
            if (!_drainCompleted) throw new InvalidOperationException("deferred evidence requires completed postcollection drain");
            return JsonSerializer.Serialize(new
            {
                version = 1, result_kind = "deferred_shield_output_diagnostic_not_performance_certification",
                route_outcome = _outcome, route_completed = _outcome == "route_completed", complete_route_evidence = false,
                output_disposition = _dispositionCaptured && _disposition.IsKnown ? _disposition.Label : "unknown",
                output_disposition_known = _dispositionCaptured && _disposition.IsKnown,
                raw_output_disposition = _dispositionCaptured ? _disposition.Kind.ToString() : null,
                deferral_attempted = _dispositionCaptured && _disposition.IsKnown ? (bool?)_disposition.DeferralAttempted : null,
                dispatch_reason = _disposition.Reason, dispatch_error = _disposition.Error,
                batch_accepted = _batch != null, acceptance_attempts = _acceptanceAttempts, acceptance_rejected = _acceptanceRejected,
                drain_claimed = _drainClaimed, drain_completed = _drainCompleted,
                emission_status = _batch == null ? "not_accepted" : _failed > 0 ? "partial_failure" : "emitted",
                emission_attempted = _attempted, emission_succeeded = _succeeded, emission_failed = _failed,
                first_emission_error = _firstEmissionError, line_emission_status = _lineStatus,
                batch = _batch == null ? null : new
                {
                    rows = _batch.RowCount, utf16_characters = _batch.CharacterCount,
                    observation_errors = _batch.ObservationErrors, first_observation_error = _batch.FirstObservationError,
                    lines = _batch.Lines
                },
                maximum_rows = Task24ShieldOutputBatch.MaximumRows, maximum_utf16_characters = Task24ShieldOutputBatch.MaximumCharacters,
                global_stdout_order_changed = _batch != null, within_block_text_order_preserved = _batch != null,
                output_after_collection = true, hard_kill_before_cleanup_can_lose_deferred_evidence = true,
                end_performs_disk_io = false, guaranteed_crash_capture = false,
                baseline_workload_comparable = false, production_performance_claim = false,
                emission_duration_usec = (ulong?)null, waiting_duration_is_not_output_time = true
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
