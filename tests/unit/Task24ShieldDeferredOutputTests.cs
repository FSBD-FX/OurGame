using System;
using System.Collections.Generic;
using System.Text.Json;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

/// <summary>Pure model fixtures authored before implementation; no native output or filesystem.</summary>
internal static class Task24ShieldDeferredOutputTests
{
    internal const int CaseCount = 11;

    internal static void Run()
    {
        SnapshotPreservesExactLinesAndOriginalErrorMetadata();
        EmptyAndMaximumRowsAreCompleteBatches();
        OversizeAndInvalidSnapshotsRejectWithoutTruncation();
        ExactCharacterLimitIncludesHeaderAndFooter();
        UnsupportedSinkDoesNotAttemptSnapshot();
        RejectedAndThrowingDispatchFallBack();
        SnapshotFailureFallsBackWithoutTouchingOriginalErrors();
        OnlyCollectionAcceptsOneCompleteBatch();
        DrainContinuesAfterFailuresAndNeverReplays();
        NoBatchAndSidecarClaimsAreOneShot();
        ExceptionDetailFailureCannotAbortRemainingLines();
    }

    // Break caught: retaining the mutable source list, reformatting lines or resetting original errors.
    private static void SnapshotPreservesExactLinesAndOriginalErrorMetadata()
    {
        var source = new List<string>
        {
            "TASK24_SHIELD_OBSERVATION_BEGIN records=2 maximum=256 discarded=7 first_loss_sequence=12 observation_errors=3 first_error=OriginalError input_sample_physics=unavailable raw_energy_state_getter=unavailable private_guard_broken=unavailable",
            "TASK24_SHIELD_OBSERVATION sequence=1 boundary=before_raise",
            "TASK24_SHIELD_OBSERVATION sequence=2 boundary=window_end_finally",
            "TASK24_SHIELD_OBSERVATION_END"
        };
        TestAssert.True(Task24ShieldOutputBatch.TryCreate(source, 2, 3, "OriginalError", out var batch, out _), "valid snapshot");
        source[0] = "changed"; source.Clear();
        TestAssert.Equal(4, batch.Lines.Count, "complete line count");
        TestAssert.Equal("TASK24_SHIELD_OBSERVATION_BEGIN records=2 maximum=256 discarded=7 first_loss_sequence=12 observation_errors=3 first_error=OriginalError input_sample_physics=unavailable raw_energy_state_getter=unavailable private_guard_broken=unavailable", batch.Lines[0], "exact header");
        TestAssert.Equal("TASK24_SHIELD_OBSERVATION sequence=1 boundary=before_raise", batch.Lines[1], "first row");
        TestAssert.Equal("TASK24_SHIELD_OBSERVATION sequence=2 boundary=window_end_finally", batch.Lines[2], "second row");
        TestAssert.Equal("TASK24_SHIELD_OBSERVATION_END", batch.Lines[3], "exact footer");
        TestAssert.Equal(3, batch.ObservationErrors, "original errors");
        TestAssert.Equal("OriginalError", batch.FirstObservationError, "original first error");
    }

    // Break caught: off-by-two line counting or dropping a zero-row block / last boundary row.
    private static void EmptyAndMaximumRowsAreCompleteBatches()
    {
        TestAssert.True(Task24ShieldOutputBatch.TryCreate(new[] { "BEGIN", "END" }, 0, 0, "none", out var empty, out _), "empty block");
        TestAssert.Equal(2, empty.Lines.Count, "empty retains header/footer");
        var lines = new List<string> { "BEGIN" };
        for (int i = 0; i < 256; i++) lines.Add("ROW");
        lines.Add("END");
        TestAssert.True(Task24ShieldOutputBatch.TryCreate(lines, 256, 0, "none", out var maximum, out _), "256 rows admitted");
        TestAssert.Equal(258, maximum.Lines.Count, "all 256 rows plus boundaries");
        TestAssert.Equal("ROW", maximum.Lines[256], "last row retained");
        TestAssert.Equal("END", maximum.Lines[257], "footer retained");
    }

    // Break caught: overflow being silently truncated, malformed evidence admitted, or source changed on rejection.
    private static void OversizeAndInvalidSnapshotsRejectWithoutTruncation()
    {
        var lines = new List<string> { "BEGIN" };
        for (int i = 0; i < 257; i++) lines.Add("ROW");
        lines.Add("END");
        TestAssert.True(!Task24ShieldOutputBatch.TryCreate(lines, 257, 4, "original", out var batch, out string reason), "row excess rejected");
        TestAssert.True(batch == null, "no partial batch"); TestAssert.Equal("row_limit", reason, "rejection reason");
        TestAssert.Equal(259, lines.Count, "original evidence untouched"); TestAssert.Equal("END", lines[258], "original footer");
        TestAssert.True(!Task24ShieldOutputBatch.TryCreate(new[] { "BEGIN", "END" }, 1, 0, "none", out _, out _), "missing row rejected");
        TestAssert.True(!Task24ShieldOutputBatch.TryCreate(new[] { "BEGIN", (string)null, "END" }, 1, 0, "none", out _, out _), "null row rejected");
        TestAssert.True(!Task24ShieldOutputBatch.TryCreate(new[] { "BEGIN", "END" }, 0, -1, "none", out _, out _), "invalid original error count rejected");
    }

    // Break caught: counting only rows, UTF-8 bytes instead of UTF-16 chars, or accepting limit+1.
    private static void ExactCharacterLimitIncludesHeaderAndFooter()
    {
        var lines = new[] { "B", new string('\u00e9', 1_048_574), "E" };
        TestAssert.True(Task24ShieldOutputBatch.TryCreate(lines, 1, 0, "none", out var batch, out _), "exact character boundary");
        TestAssert.Equal(1_048_576, batch.CharacterCount, "complete emitted strings count");
        lines[1] += "x";
        TestAssert.True(!Task24ShieldOutputBatch.TryCreate(lines, 1, 0, "none", out var rejected, out string reason), "limit+1 rejected");
        TestAssert.True(rejected == null, "no truncated block"); TestAssert.Equal("character_limit", reason, "character rejection");
        TestAssert.Equal(1_048_575, lines[1].Length, "oversize evidence not truncated");
    }

    // Break caught: unsupported runners paying snapshot cost or losing their immediate path.
    private static void UnsupportedSinkDoesNotAttemptSnapshot()
    {
        int calls = 0;
        var result = Task24ShieldOutputDispatch.Dispatch(new object(), () => { calls++; return Batch(); });
        TestAssert.Equal(Task24ShieldOutputKind.Immediate, result.Kind, "unsupported immediate path");
        TestAssert.Equal(0, calls, "unsupported does not snapshot");
        TestAssert.True(!result.DeferralAttempted, "no attempt");
    }

    // Break caught: false/throwing acceptance suppressing immediate evidence or claiming deferred ownership.
    private static void RejectedAndThrowingDispatchFallBack()
    {
        var rejected = Task24ShieldOutputDispatch.Dispatch(new RejectingSink(), Batch);
        TestAssert.Equal(Task24ShieldOutputKind.ImmediateFallback, rejected.Kind, "rejected falls back");
        TestAssert.Equal("sink_rejected", rejected.Reason, "explicit rejection");
        var failed = Task24ShieldOutputDispatch.Dispatch(new ThrowingSink(), Batch);
        TestAssert.Equal(Task24ShieldOutputKind.ImmediateFallback, failed.Kind, "throw falls back");
        TestAssert.Equal("sink_failed", failed.Reason, "explicit dispatch failure");
        TestAssert.True(failed.Error.Contains("sink fixture"), "dispatch failure evidence");
    }

    // Break caught: snapshot failure escaping End or becoming an original observation error.
    private static void SnapshotFailureFallsBackWithoutTouchingOriginalErrors()
    {
        var sink = new Task24ShieldDeferredOutput(); sink.BeginCollection();
        var result = Task24ShieldOutputDispatch.Dispatch(sink, () => throw new InvalidOperationException("snapshot fixture"));
        TestAssert.Equal(Task24ShieldOutputKind.ImmediateFallback, result.Kind, "snapshot exception fallback");
        TestAssert.Equal("snapshot_failed", result.Reason, "snapshot failure evidence");
        TestAssert.True(result.Error.Contains("snapshot fixture"), "first snapshot error");
        var invalid = Task24ShieldOutputDispatch.Dispatch(sink, () => null);
        TestAssert.Equal("snapshot_rejected", invalid.Reason, "invalid snapshot fallback");
        sink.RecordDisposition(result); sink.StopCollection(); sink.Drain(_ => { }, "failed");
        using JsonDocument json = JsonDocument.Parse(sink.CreateJson());
        TestAssert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("batch").ValueKind, "no fabricated original metadata");
        TestAssert.Equal("snapshot_failed", json.RootElement.GetProperty("dispatch_reason").GetString(), "separate dispatch evidence");
    }

    // Break caught: precollection/duplicate/late batch overwriting first accepted complete evidence.
    private static void OnlyCollectionAcceptsOneCompleteBatch()
    {
        var sink = new Task24ShieldDeferredOutput(); var first = Batch();
        TestAssert.True(Task24ShieldOutputBatch.TryCreate(new[] { "SECOND", "different row", "FIN" }, 1, 9,
            "DifferentError", out var different, out _), "distinct duplicate fixture");
        TestAssert.True(!sink.TryAcceptTask24ShieldOutput(first), "precollection refused");
        TestAssert.True(sink.BeginCollection(), "collection begins once");
        TestAssert.True(sink.TryAcceptTask24ShieldOutput(first), "first accepted atomically");
        TestAssert.True(!sink.TryAcceptTask24ShieldOutput(different), "duplicate refused");
        TestAssert.True(!sink.Drain(_ => throw new Exception("must not emit during collection"), "failed"), "collecting drain refused");
        sink.StopCollection();
        TestAssert.True(!sink.TryAcceptTask24ShieldOutput(different), "postcollection refused");
        var emitted = new List<string>(); TestAssert.True(sink.Drain(emitted.Add, "route_completed"), "postcollection drain");
        TestAssert.Equal("BEGIN errors=2", emitted[0], "first batch retained"); TestAssert.Equal(3, emitted.Count, "complete block");
        TestAssert.True(!sink.BeginCollection(), "cannot restart collection");
        TestAssert.True(!sink.TryAcceptTask24ShieldOutput(different), "after-drain refused");
    }

    // Break caught: first exception aborting remaining lines, replaying partial stdout, or losing full batch JSON.
    private static void DrainContinuesAfterFailuresAndNeverReplays()
    {
        var sink = new Task24ShieldDeferredOutput(); sink.BeginCollection();
        var disposition = Task24ShieldOutputDispatch.Dispatch(sink, Batch); sink.RecordDisposition(disposition);
        TestAssert.Equal(Task24ShieldOutputKind.Deferred, disposition.Kind, "real sink owns batch");
        sink.StopCollection(); var attempts = new List<string>();
        TestAssert.True(sink.Drain(line => { attempts.Add(line); if (line != "END") throw new InvalidOperationException(line); }, "failed"), "first drain claimed");
        TestAssert.True(!sink.Drain(_ => throw new Exception("replay"), "route_completed"), "never replay");
        TestAssert.Equal(3, attempts.Count, "all lines attempted"); TestAssert.Equal("END", attempts[2], "footer attempted after failures");
        using JsonDocument json = JsonDocument.Parse(sink.CreateJson()); JsonElement root = json.RootElement;
        TestAssert.Equal(3, root.GetProperty("emission_attempted").GetInt32(), "attempted");
        TestAssert.Equal(1, root.GetProperty("emission_succeeded").GetInt32(), "succeeded");
        TestAssert.Equal(2, root.GetProperty("emission_failed").GetInt32(), "failed");
        TestAssert.True(root.GetProperty("first_emission_error").GetString().Contains("BEGIN errors=2"), "first emission failure retained");
        TestAssert.Equal("failed", root.GetProperty("route_outcome").GetString(), "route outcome independent");
        TestAssert.True(!root.GetProperty("route_completed").GetBoolean(), "diagnostic cannot certify failed route");
        TestAssert.Equal("deferred", root.GetProperty("output_disposition").GetString(), "actual disposition");
        JsonElement batch = root.GetProperty("batch");
        TestAssert.Equal("BEGIN errors=2", batch.GetProperty("lines")[0].GetString(), "full header in evidence");
        TestAssert.Equal("ROW fixture", batch.GetProperty("lines")[1].GetString(), "failed stdout row retained");
        TestAssert.Equal("END", batch.GetProperty("lines")[2].GetString(), "full footer in evidence");
        TestAssert.Equal(2, batch.GetProperty("observation_errors").GetInt32(), "original errors independent");
        TestAssert.Equal("OriginalError", batch.GetProperty("first_observation_error").GetString(), "original first error independent");
        TestAssert.Equal("failed", root.GetProperty("line_emission_status")[0].GetString(), "header status");
        TestAssert.Equal("succeeded", root.GetProperty("line_emission_status")[2].GetString(), "footer status");
    }

    // Break caught: no-batch cleanup inventing lines, late collection, retrying writes or rewriting outcome.
    private static void NoBatchAndSidecarClaimsAreOneShot()
    {
        var sink = new Task24ShieldDeferredOutput();
        TestAssert.True(!sink.TryClaimSidecarAttempt(), "no pre-drain write");
        sink.StopCollection(); TestAssert.True(sink.Drain(_ => throw new Exception("no batch"), "early_exit"), "no-batch closes drain");
        TestAssert.True(!sink.BeginCollection(), "drained model cannot arm");
        TestAssert.True(sink.TryClaimSidecarAttempt(), "first write attempt");
        TestAssert.True(!sink.TryClaimSidecarAttempt(), "no write retry");
        TestAssert.True(!sink.Drain(_ => { }, "route_completed"), "no outcome rewrite");
        using JsonDocument json = JsonDocument.Parse(sink.CreateJson());
        TestAssert.Equal("not_accepted", json.RootElement.GetProperty("emission_status").GetString(), "explicit unavailable block");
        TestAssert.Equal("early_exit", json.RootElement.GetProperty("route_outcome").GetString(), "first outcome");
        TestAssert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("batch").ValueKind, "no evidence invented");
    }

    private static Task24ShieldOutputBatch Batch()
    {
        TestAssert.True(Task24ShieldOutputBatch.TryCreate(new[] { "BEGIN errors=2", "ROW fixture", "END" }, 1, 2,
            "OriginalError", out var batch, out _), "fixture admitted");
        return batch;
    }

    private sealed class RejectingSink : ITask24ShieldDeferredOutputSink
    { public bool TryAcceptTask24ShieldOutput(Task24ShieldOutputBatch batch) => false; }
    private sealed class ThrowingSink : ITask24ShieldDeferredOutputSink
    { public bool TryAcceptTask24ShieldOutput(Task24ShieldOutputBatch batch) => throw new InvalidOperationException("sink fixture"); }

    // Break caught: exception formatting itself escaping the guarded emission/dispatch boundary.
    private static void ExceptionDetailFailureCannotAbortRemainingLines()
    {
        var sink = new Task24ShieldDeferredOutput(); sink.BeginCollection();
        var result = Task24ShieldOutputDispatch.Dispatch(sink, () => throw new UnprintableException());
        TestAssert.Equal(Task24ShieldOutputKind.ImmediateFallback, result.Kind, "unprintable snapshot exception falls back");
        TestAssert.Equal("exception_details_unavailable", result.Error, "explicit missing exception detail");
        TestAssert.True(sink.TryAcceptTask24ShieldOutput(Batch()), "batch admitted after failed snapshot");
        sink.StopCollection(); int attempts = 0;
        sink.Drain(_ => { attempts++; if (attempts == 1) throw new UnprintableException(); }, "failed");
        TestAssert.Equal(3, attempts, "remaining lines continue after exception formatting failure");
        using JsonDocument json = JsonDocument.Parse(sink.CreateJson());
        TestAssert.Equal("exception_details_unavailable", json.RootElement.GetProperty("first_emission_error").GetString(), "detail failure recorded");
        TestAssert.Equal(2, json.RootElement.GetProperty("emission_succeeded").GetInt32(), "remaining native attempts succeed");
    }

    private sealed class UnprintableException : Exception
    { public override string ToString() => throw new InvalidOperationException("details unavailable fixture"); }
}
