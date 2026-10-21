using System.Text.Json;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

/// <summary>Pure literal fixtures; no native acquisition, scene, input or filesystem.</summary>
internal static class Task24ShieldEndTimingTests
{
    internal const int CaseCount = 16;

    internal static void Run()
    {
        UnmeasuredDoesNotInventZero();
        OrderedStampsProduceLiteralDurations();
        MissingBoundaryKeepsOnlySupportedSegments();
        AcquisitionFailureKeepsRawFieldsAndPartialFlag();
        RegressionNeverSubtractsUnsignedValues();
        DuplicateAttemptsNeverOverwriteFirstCapture();
        RejectedAndOutOfOrderBoundariesCannotCertifyMeasurement();
        ObservationErrorGrowthIsIndependentFailure();
        InvalidFrameAndObservationCountersCannotCertifyMeasurement();
        AdvancingFramesAreValidButNegativeErrorsAreNot();
        FinalizationAndWriteClaimAreIdempotent();
        MeasuredFailedRouteIsNotRouteCertification();
        DeferredSpanIsDispatchNotExistingOutput();
        AttemptedFallbackSpanIsNotExistingOutput();
        UnknownDispositionCannotCertifyImmediateOutput();
        DispositionIsFirstCaptureAndFailureIsSeparate();
    }

    // Break caught: default stamps becoming a measured zero-duration span.
    private static void UnmeasuredDoesNotInventZero()
    {
        var timing = new Task24ShieldEndTiming();
        TestAssert.True(!timing.TryClaimSidecarAttempt(), "cannot write an unfrozen receipt");
        using JsonDocument json = Report(timing, supplyImmediate: false);
        JsonElement root = json.RootElement;
        Text(root, "status", "unmeasured"); Flag(root, "unmeasured", true);
        Flag(root, "partial", false); Flag(root, "measurement_complete", false);
        Equal(root, "begin_attempts", 0); Equal(root, "retained_stamps", 0);
        NullDuration(root, "total_usec");
    }

    // Break caught: swapped boundaries, wrong subtraction, or loss of raw frame identity.
    private static void OrderedStampsProduceLiteralDurations()
    {
        using JsonDocument json = Report(Complete());
        JsonElement root = json.RootElement;
        Text(root, "status", "measured"); Flag(root, "measurement_complete", true);
        Flag(root, "failure", false); Flag(root, "partial", false);
        Duration(root, "final_record_usec", 30); Duration(root, "detach_usec", 20);
        Duration(root, "existing_output_usec", 80); Duration(root, "total_usec", 130);
        Duration(root, "output_or_dispatch_usec", 80); Text(root, "output_disposition", "immediate");
        Equal(root, "version", 2);
        Equal(root, "retained_stamps", 4);
        TestAssert.Equal(7UL, root.GetProperty("stamps")[0].GetProperty("process_frame").GetUInt64(), "raw process frame");
        TestAssert.Equal(9UL, root.GetProperty("stamps")[3].GetProperty("physics_frame").GetUInt64(), "raw physics frame");
    }

    // Break caught: filling missing T1 or rejecting a later available boundary after a gap.
    private static void MissingBoundaryKeepsOnlySupportedSegments()
    {
        var timing = new Task24ShieldEndTiming(); timing.Begin(0);
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(150));
        timing.Mark(Task24ShieldEndBoundary.T3, Stamp(230));
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Text(root, "status", "partial"); Flag(root, "partial", true);
        NullDuration(root, "final_record_usec"); NullDuration(root, "detach_usec");
        Duration(root, "existing_output_usec", 80); NullDuration(root, "total_usec");
        Flag(root.GetProperty("stamps")[1], "attempted", false);
    }

    // Break caught: one failed native field hiding other raw values or partial/failure overlap.
    private static void AcquisitionFailureKeepsRawFieldsAndPartialFlag()
    {
        var timing = new Task24ShieldEndTiming(); timing.Begin(0);
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100));
        timing.MarkAcquisitionFailure("ticks_fixture_failure");
        timing.Mark(Task24ShieldEndBoundary.T1, new Task24ShieldEndStamp(null, 7, 9, 0));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(150));
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Text(root, "status", "failure"); Flag(root, "failure", true); Flag(root, "partial", true);
        Equal(root, "acquisition_failures", 1); Equal(root, "retained_stamps", 3);
        Text(root, "first_acquisition_error", "ticks_fixture_failure");
        JsonElement t1 = root.GetProperty("stamps")[1]; Flag(t1, "valid", false);
        TestAssert.Equal(7UL, t1.GetProperty("process_frame").GetUInt64(), "available process field survives clock failure");
        NullDuration(root, "final_record_usec"); NullDuration(root, "total_usec");
    }

    // Break caught: unsigned underflow, or a regressed inner stamp masked by ordered endpoints.
    private static void RegressionNeverSubtractsUnsignedValues()
    {
        var timing = new Task24ShieldEndTiming(); timing.Begin(0);
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100));
        timing.Mark(Task24ShieldEndBoundary.T1, Stamp(130));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(120));
        timing.Mark(Task24ShieldEndBoundary.T3, Stamp(230));
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Text(root, "status", "regressed_clock"); Flag(root, "clock_regressed", true);
        Flag(root, "measurement_complete", false); Equal(root, "clock_regressions", 1);
        Duration(root, "final_record_usec", 30); NullDuration(root, "detach_usec");
        Duration(root, "existing_output_usec", 110); NullDuration(root, "total_usec");
    }

    // Break caught: rearming or rewriting a first stamp on duplicate submission.
    private static void DuplicateAttemptsNeverOverwriteFirstCapture()
    {
        var timing = Complete();
        TestAssert.True(!timing.Begin(99), "duplicate begin refused");
        TestAssert.True(!timing.Mark(Task24ShieldEndBoundary.T0, Stamp(999, 99)), "duplicate boundary refused");
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Equal(root, "begin_attempts", 2); Equal(root, "duplicate_begins", 1);
        Equal(root, "boundary_attempts", 5); Equal(root, "duplicate_boundaries", 1);
        Equal(root, "rejected_boundaries", 1); Equal(root, "begin_observation_errors", 0);
        TestAssert.Equal(100UL, root.GetProperty("stamps")[0].GetProperty("ticks_usec").GetUInt64(), "first T0 retained");
        Duration(root, "total_usec", 130); Flag(root, "measurement_complete", false);
    }

    // Break caught: accepting pre-Begin, unknown enum or backwards acquisition as complete.
    private static void RejectedAndOutOfOrderBoundariesCannotCertifyMeasurement()
    {
        var timing = new Task24ShieldEndTiming();
        TestAssert.True(!timing.Mark(Task24ShieldEndBoundary.T0, Stamp(1)), "pre-Begin rejected");
        timing.Begin(0);
        TestAssert.True(!timing.Mark((Task24ShieldEndBoundary)4, Stamp(2)), "unknown boundary rejected");
        timing.Mark(Task24ShieldEndBoundary.T1, Stamp(130));
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(150));
        timing.Mark(Task24ShieldEndBoundary.T3, Stamp(230));
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Flag(root, "invalid_order", true); Flag(root, "measurement_complete", false);
        Equal(root, "boundary_attempts", 6); Equal(root, "rejected_boundaries", 2);
        Equal(root, "retained_stamps", 4); NullDuration(root, "total_usec");
        NullDuration(root, "final_record_usec");
    }

    // Break caught: existing observation errors being ignored, reset, or folded into timing acquisition errors.
    private static void ObservationErrorGrowthIsIndependentFailure()
    {
        var timing = new Task24ShieldEndTiming(); timing.Begin(2);
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100, 2));
        timing.Mark(Task24ShieldEndBoundary.T1, Stamp(130, 3));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(150, 3));
        timing.Mark(Task24ShieldEndBoundary.T3, Stamp(230, 4));
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Flag(root, "observation_errors_increased", true); Flag(root, "failure", true);
        Equal(root, "begin_observation_errors", 2); Equal(root, "maximum_observation_errors", 4);
        Equal(root, "acquisition_failures", 0); Flag(root, "measurement_complete", false);
        Duration(root, "total_usec", 130);
    }

    // Break caught: frame/error counter regression still labeled a valid full span.
    private static void InvalidFrameAndObservationCountersCannotCertifyMeasurement()
    {
        var timing = new Task24ShieldEndTiming(); timing.Begin(2);
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100, 2));
        timing.Mark(Task24ShieldEndBoundary.T1, new Task24ShieldEndStamp(130, 6, 8, 1));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(150, 2));
        timing.Mark(Task24ShieldEndBoundary.T3, Stamp(230, 2));
        using JsonDocument json = Report(timing); JsonElement root = json.RootElement;
        Flag(root, "invalid_counters", true); Flag(root, "measurement_complete", false);
        Equal(root, "process_frame_regressions", 1); Equal(root, "physics_frame_regressions", 1);
        Equal(root, "observation_error_regressions", 1);
        NullDuration(root, "final_record_usec"); NullDuration(root, "total_usec");
    }

    // Break caught: late teardown rewrites first outcome/stamps or retries exclusive write.
    private static void FinalizationAndWriteClaimAreIdempotent()
    {
        var timing = Complete(); timing.Freeze("failed");
        string first = timing.CreateJson(); timing.Freeze("route_completed");
        TestAssert.True(!timing.Begin(9), "frozen receipt cannot rearm");
        TestAssert.True(!timing.Mark(Task24ShieldEndBoundary.T3, Stamp(999)), "frozen stamp cannot change");
        timing.MarkAcquisitionFailure("late");
        TestAssert.Equal(first, timing.CreateJson(), "first freeze preserves whole receipt");
        TestAssert.True(timing.TryClaimSidecarAttempt(), "first sidecar attempt accepted");
        TestAssert.True(!timing.TryClaimSidecarAttempt(), "second sidecar attempt refused");
    }

    // Break caught: treating advancing scalar frames as regression, or accepting negative errors.
    private static void AdvancingFramesAreValidButNegativeErrorsAreNot()
    {
        var advancing = new Task24ShieldEndTiming(); advancing.Begin(0);
        advancing.Mark(Task24ShieldEndBoundary.T0, Stamp(100));
        advancing.Mark(Task24ShieldEndBoundary.T1, new Task24ShieldEndStamp(130, 8, 10, 0));
        advancing.Mark(Task24ShieldEndBoundary.T2, new Task24ShieldEndStamp(150, 9, 11, 0));
        advancing.Mark(Task24ShieldEndBoundary.T3, new Task24ShieldEndStamp(230, 10, 12, 0));
        using (JsonDocument json = Report(advancing))
        {
            Flag(json.RootElement, "measurement_complete", true);
            Equal(json.RootElement, "process_frame_regressions", 0);
            Equal(json.RootElement, "physics_frame_regressions", 0);
            Duration(json.RootElement, "total_usec", 130);
        }
        var negative = new Task24ShieldEndTiming(); negative.Begin(-1);
        negative.Mark(Task24ShieldEndBoundary.T0, Stamp(100, -1));
        using JsonDocument invalid = Report(negative);
        Flag(invalid.RootElement, "invalid_counters", true); Flag(invalid.RootElement, "partial", true);
        Flag(invalid.RootElement, "measurement_complete", false);
        NullDuration(invalid.RootElement, "total_usec");
    }

    // Break caught: good timing receipt certifies a failed route, or serialization invents attribution.
    private static void MeasuredFailedRouteIsNotRouteCertification()
    {
        using JsonDocument json = Report(Complete(), "failed"); JsonElement root = json.RootElement;
        Text(root, "status", "measured"); Text(root, "outcome", "failed");
        Flag(root, "measurement_complete", true); Flag(root, "route_completed", false);
        Flag(root, "complete_route_evidence", false); Flag(root, "measurement_overhead_subtracted", false);
        Text(root, "causality", "not_established");
    }

    private static Task24ShieldEndStamp Stamp(ulong ticks, int errors = 0) => new(ticks, 7, 9, errors);
    // Break caught: deferred snapshot/acceptance being mislabeled as original native output.
    private static void DeferredSpanIsDispatchNotExistingOutput()
    {
        var timing = Complete(false);
        timing.RecordOutputDisposition(new(Task24ShieldOutputKind.Deferred, null, null));
        using JsonDocument json = Report(timing, supplyImmediate: false); var root = json.RootElement;
        Text(root, "output_disposition", "deferred"); Duration(root, "output_or_dispatch_usec", 80);
        NullDuration(root, "existing_output_usec"); Duration(root, "total_usec", 130);
        Text(root, "output_span", "snapshot_and_sink_acceptance_not_deferred_emission_or_waiting_time");
    }

    // Break caught: attempted fallback's snapshot/dispatch costs being passed off as comparable output.
    private static void AttemptedFallbackSpanIsNotExistingOutput()
    {
        var timing = Complete(false);
        timing.RecordOutputDisposition(new(Task24ShieldOutputKind.ImmediateFallback, "sink_failed", "fixture error"));
        using JsonDocument json = Report(timing, supplyImmediate: false); var root = json.RootElement;
        Text(root, "output_disposition", "immediate_fallback"); Duration(root, "output_or_dispatch_usec", 80);
        NullDuration(root, "existing_output_usec"); Text(root, "output_dispatch_error", "fixture error");
        Equal(root, "maximum_observation_errors", 0); Flag(root, "failure", false);
    }

    // Break caught: absence/invalid disposition silently defaulting to known immediate output.
    private static void UnknownDispositionCannotCertifyImmediateOutput()
    {
        var timing = Complete(false);
        using (JsonDocument json = Report(timing, supplyImmediate: false))
        {
            Text(json.RootElement, "output_disposition", "unknown"); NullDuration(json.RootElement, "existing_output_usec");
            Flag(json.RootElement, "output_disposition_known", false); Flag(json.RootElement, "measurement_complete", false);
            Duration(json.RootElement, "output_or_dispatch_usec", 80);
        }
        var invalid = Complete(false); invalid.RecordOutputDisposition(new((Task24ShieldOutputKind)99, null, null));
        using JsonDocument failed = Report(invalid, supplyImmediate: false);
        Equal(failed.RootElement, "output_disposition_failures", 1); NullDuration(failed.RootElement, "existing_output_usec");
        var contradictory = Complete(false);
        contradictory.RecordOutputDisposition(new(Task24ShieldOutputKind.Immediate, "sink_failed", "fixture"));
        using JsonDocument contradiction = Report(contradictory, supplyImmediate: false);
        Flag(contradiction.RootElement, "output_disposition_known", false);
        NullDuration(contradiction.RootElement, "existing_output_usec");
    }

    // Break caught: a late disposition overwriting first capture or diagnostic errors polluting original errors.
    private static void DispositionIsFirstCaptureAndFailureIsSeparate()
    {
        var timing = Complete(false);
        timing.RecordOutputDisposition(new(Task24ShieldOutputKind.ImmediateFallback, "snapshot_failed", "first failure"));
        timing.RecordOutputDisposition(new(Task24ShieldOutputKind.Immediate, null, null));
        using JsonDocument json = Report(timing, supplyImmediate: false); var root = json.RootElement;
        Text(root, "output_disposition", "immediate_fallback"); Text(root, "output_dispatch_error", "first failure");
        Equal(root, "maximum_observation_errors", 0); NullDuration(root, "existing_output_usec");
        string frozen = timing.CreateJson(); timing.RecordOutputDisposition(new(Task24ShieldOutputKind.Deferred, null, null));
        TestAssert.Equal(frozen, timing.CreateJson(), "frozen disposition cannot change");
    }

    private static Task24ShieldEndTiming Complete(bool supplyImmediate = true)
    {
        var timing = new Task24ShieldEndTiming(); timing.Begin(0);
        timing.Mark(Task24ShieldEndBoundary.T0, Stamp(100));
        timing.Mark(Task24ShieldEndBoundary.T1, Stamp(130));
        timing.Mark(Task24ShieldEndBoundary.T2, Stamp(150));
        timing.Mark(Task24ShieldEndBoundary.T3, Stamp(230));
        if (supplyImmediate) timing.RecordOutputDisposition(new(Task24ShieldOutputKind.Immediate, null, null));
        return timing;
    }
    private static JsonDocument Report(Task24ShieldEndTiming timing, string outcome = "route_completed", bool supplyImmediate = true)
    {
        if (supplyImmediate) timing.RecordOutputDisposition(new(Task24ShieldOutputKind.Immediate, null, null));
        timing.Freeze(outcome); return JsonDocument.Parse(timing.CreateJson());
    }
    private static void Flag(JsonElement root, string field, bool expected) =>
        TestAssert.Equal(expected, root.GetProperty(field).GetBoolean(), field);
    private static void Equal(JsonElement root, string field, int expected) =>
        TestAssert.Equal(expected, root.GetProperty(field).GetInt32(), field);
    private static void Text(JsonElement root, string field, string expected) =>
        TestAssert.Equal(expected, root.GetProperty(field).GetString(), field);
    private static void Duration(JsonElement root, string field, ulong expected) =>
        TestAssert.Equal(expected, root.GetProperty("durations").GetProperty(field).GetUInt64(), field);
    private static void NullDuration(JsonElement root, string field) =>
        TestAssert.Equal(JsonValueKind.Null, root.GetProperty("durations").GetProperty(field).ValueKind, field);
}
