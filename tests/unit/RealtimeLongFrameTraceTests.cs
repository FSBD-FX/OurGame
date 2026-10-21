using System;
using System.Text.Json;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

/// <summary>Pure timestamp/context fixtures. Authored before implementation; execution is root-owned.</summary>
public static class RealtimeLongFrameTraceTests
{
    public static int CaseCount { get; private set; }

    public static void Run()
    {
        CaseCount = 0;
        Check(PrimingDoesNotInventAnInterval);
        Check(Exactly120ValidIntervalsAreWarmup);
        Check(ThresholdsAreStrict);
        Check(EndpointsRetainChronologyAndIdentity);
        Check(RegressionResetsEndpointWithoutConsumingWarmup);
        Check(First256StayChronologicalWhileLaterWorstAreRetained);
        Check(EqualWorstTiesKeepEarlierIntervals);
        Check(CapacityStopsBothCountsAndRetention);
        Check(PartialOutcomeCannotClaimCompleteEvidence);
        Check(SerializationRetainsFieldsAndLimits);
        Check(ObserverFailureCannotClaimCompleteEvidence);
        Check(SetupAndTeardownDoNotInventContext);
    }

    private static void Check(Action test) { test(); CaseCount++; }

    // Break caught: treating the first callback as an accepted zero-duration sample.
    private static void PrimingDoesNotInventAnInterval()
    {
        var trace = new RealtimeLongFrameTrace();
        trace.Observe(1000, Context());
        using JsonDocument json = Report(trace);
        Equal(0, json.RootElement, "samples"); Equal(0, json.RootElement, "warmup_discarded");
        Equal(0, json.RootElement, "long_intervals_total");
    }

    // Break caught: a warmup off-by-one, or discarding wall-clock spikes after warmup.
    private static void Exactly120ValidIntervalsAreWarmup()
    {
        var trace = new RealtimeLongFrameTrace(); ulong ticks = 1000;
        trace.Observe(ticks, Context());
        for (int i = 0; i < 119; i++) Next(trace, ref ticks, 60000);
        using (JsonDocument json = Report(trace)) { Equal(119, json.RootElement, "warmup_discarded"); Equal(0, json.RootElement, "samples"); }
        Next(trace, ref ticks, 60000);
        using (JsonDocument json = Report(trace)) { Equal(120, json.RootElement, "warmup_discarded"); Equal(0, json.RootElement, "samples"); }
        Next(trace, ref ticks, 60000);
        using JsonDocument measured = Report(trace);
        Equal(1, measured.RootElement, "samples"); Equal(1, measured.RootElement, "long_intervals_total");
    }

    // Break caught: >= classification at either exact threshold.
    private static void ThresholdsAreStrict()
    {
        var (trace, ticks) = Warmed();
        foreach (ulong interval in new ulong[] { 33300, 33301, 50000, 50001 }) Next(trace, ref ticks, interval);
        using JsonDocument json = Report(trace);
        Equal(4, json.RootElement, "samples"); Equal(3, json.RootElement, "long_intervals_total");
        Equal(1, json.RootElement, "over_50_ms");
    }

    // Break caught: labeling the whole interval by only its end sector/player or timestamp.
    private static void EndpointsRetainChronologyAndIdentity()
    {
        var (trace, ticks) = Warmed();
        ticks += 10;
        trace.Observe(ticks, Context(7, 12, PrologueSectorId.CanyonBasin, 111, 8, 9, false));
        ulong start = ticks; ticks += 40000;
        trace.Observe(ticks, Context(8, 14, PrologueSectorId.CanyonAscent, 222, 18, 19, true));
        using JsonDocument json = Report(trace);
        JsonElement record = json.RootElement.GetProperty("first_long_intervals")[0];
        TestAssert.Equal(2, record.GetProperty("sample_index").GetInt32(), "sample identity includes preceding short interval");
        TestAssert.Equal(start, record.GetProperty("start").GetProperty("callback_usec").GetUInt64(), "start timestamp retained");
        TestAssert.Equal(ticks, record.GetProperty("end").GetProperty("callback_usec").GetUInt64(), "end timestamp retained");
        JsonElement a = record.GetProperty("start").GetProperty("context"), b = record.GetProperty("end").GetProperty("context");
        TestAssert.Equal("CanyonBasin", a.GetProperty("sector").GetString(), "start sector is actual start observation");
        TestAssert.Equal("CanyonAscent", b.GetProperty("sector").GetString(), "end sector retained separately");
        TestAssert.Equal(111UL, a.GetProperty("player_instance_id").GetUInt64(), "start player identity");
        TestAssert.Equal(222UL, b.GetProperty("player_instance_id").GetUInt64(), "end player identity");
        TestAssert.Equal(7UL, a.GetProperty("process_frame").GetUInt64(), "start process frame");
        TestAssert.Equal(14UL, b.GetProperty("physics_frame").GetUInt64(), "end physics frame");
        TestAssert.Equal(8f, a.GetProperty("player_x").GetSingle(), "start position");
        TestAssert.Equal(19f, b.GetProperty("player_y").GetSingle(), "end position");
        TestAssert.True(!a.GetProperty("focused").GetBoolean() && b.GetProperty("focused").GetBoolean(), "both observed focus states");
    }

    // Break caught: unsigned subtraction spike or consuming a warmup slot on regression.
    private static void RegressionResetsEndpointWithoutConsumingWarmup()
    {
        var trace = new RealtimeLongFrameTrace(); ulong ticks = 100000;
        trace.Observe(ticks, Context()); trace.Observe(90000, Context()); ticks = 90000;
        for (int i = 0; i < 120; i++) Next(trace, ref ticks, 1);
        trace.Observe(ticks - 1, Context(9)); ticks--;
        Next(trace, ref ticks, 40000);
        using JsonDocument json = Report(trace);
        Equal(2, json.RootElement, "clock_regressions"); Equal(120, json.RootElement, "warmup_discarded");
        Equal(1, json.RootElement, "samples"); Equal(1, json.RootElement, "long_intervals_total");
        TestAssert.Equal(9UL, json.RootElement.GetProperty("first_long_intervals")[0].GetProperty("start").GetProperty("context").GetProperty("process_frame").GetUInt64(), "regressed callback becomes next start");
        TestAssert.True(!json.RootElement.GetProperty("complete_diagnostic_evidence").GetBoolean(), "regression is incomplete evidence");
    }

    // Break caught: first-list eviction, unbounded retention, or ignoring later spikes.
    private static void First256StayChronologicalWhileLaterWorstAreRetained()
    {
        var (trace, ticks) = Warmed();
        for (int i = 0; i < 256; i++) Next(trace, ref ticks, 40000);
        Next(trace, ref ticks, 90000); Next(trace, ref ticks, 80000);
        using JsonDocument json = Report(trace); JsonElement root = json.RootElement;
        Equal(258, root, "long_intervals_total"); Equal(256, root, "first_retained"); Equal(2, root, "first_omitted");
        Equal(16, root, "worst_retained"); Equal(242, root, "worst_omitted"); Equal(14, root, "duplicate_records_between_lists");
        Equal(258, root, "unique_retained"); Equal(0, root, "unique_omitted");
        JsonElement first = root.GetProperty("first_long_intervals"), worst = root.GetProperty("worst_long_intervals");
        TestAssert.Equal(1, first[0].GetProperty("sample_index").GetInt32(), "first chronology begins at one");
        TestAssert.Equal(256, first[255].GetProperty("sample_index").GetInt32(), "first chronology never evicts");
        TestAssert.Equal(257, worst[0].GetProperty("sample_index").GetInt32(), "later largest spike remains");
        TestAssert.Equal(258, worst[1].GetProperty("sample_index").GetInt32(), "later second largest spike remains");
    }

    // Break caught: replacing old equal-duration records with later ones nondeterministically.
    private static void EqualWorstTiesKeepEarlierIntervals()
    {
        var (trace, ticks) = Warmed(); for (int i = 0; i < 20; i++) Next(trace, ref ticks, 40000);
        using JsonDocument json = Report(trace); JsonElement worst = json.RootElement.GetProperty("worst_long_intervals");
        for (int i = 0; i < 16; i++) TestAssert.Equal(i + 1, worst[i].GetProperty("sample_index").GetInt32(), "stable earliest equal ties");
    }

    // Break caught: ancillary sample/threshold counters extending past main sampler capacity.
    private static void CapacityStopsBothCountsAndRetention()
    {
        var (trace, ticks) = Warmed(); for (int i = 0; i < 262144; i++) Next(trace, ref ticks, 1);
        Next(trace, ref ticks, 900000); Next(trace, ref ticks, 1000000);
        using JsonDocument json = Report(trace); Equal(262144, json.RootElement, "samples"); Equal(2, json.RootElement, "overflow_discarded");
        Equal(0, json.RootElement, "long_intervals_total"); Equal(0, json.RootElement, "first_retained");
        TestAssert.True(!json.RootElement.GetProperty("complete_diagnostic_evidence").GetBoolean(), "overflow is incomplete evidence");
    }

    private static void PartialOutcomeCannotClaimCompleteEvidence()
    {
        var (trace, ticks) = Warmed(); Next(trace, ref ticks, 40000);
        using JsonDocument json = JsonDocument.Parse(trace.CreateJson("failed", null));
        TestAssert.Equal("failed", json.RootElement.GetProperty("outcome").GetString(), "partial outcome retained");
        Equal(1, json.RootElement, "first_retained");
        TestAssert.True(!json.RootElement.GetProperty("complete_diagnostic_evidence").GetBoolean(), "partial route is not complete diagnostic evidence");
    }

    private static void SerializationRetainsFieldsAndLimits()
    {
        var (trace, ticks) = Warmed(); Next(trace, ref ticks, 40000);
        using JsonDocument json = Report(trace); JsonElement root = json.RootElement;
        Equal(1, root, "version"); Equal(33300, root, "strict_long_threshold_usec"); Equal(262144, root, "sample_capacity");
        TestAssert.True(root.GetProperty("complete_diagnostic_evidence").GetBoolean(), "valid finished fixture produces complete bounded evidence");
        TestAssert.True(root.GetProperty("observer_overhead_affects_following_intervals").GetBoolean(), "observer timing limitation is serialized");
        TestAssert.Equal("not_established", root.GetProperty("cpu_gpu_gc_causality").GetString(), "no causal attribution");
        JsonElement context = root.GetProperty("first_long_intervals")[0].GetProperty("end").GetProperty("context");
        TestAssert.Equal("Playing", context.GetProperty("flow").GetString(), "enum flow serialized");
        TestAssert.Equal("ReachBasin", context.GetProperty("objective").GetString(), "enum objective serialized");
    }

    private static void ObserverFailureCannotClaimCompleteEvidence()
    {
        var (trace, ticks) = Warmed(); trace.MarkObserverFailure(); Next(trace, ref ticks, 40000);
        using JsonDocument json = JsonDocument.Parse(trace.CreateJson("route_completed", "fixture_observer_failure"));
        Equal(1, json.RootElement, "observer_failures");
        TestAssert.Equal("fixture_observer_failure", json.RootElement.GetProperty("observer_error").GetString(), "failure retained");
        TestAssert.True(!json.RootElement.GetProperty("complete_diagnostic_evidence").GetBoolean(), "observer failure is incomplete");
    }

    private static void SetupAndTeardownDoNotInventContext()
    {
        var (trace, ticks) = Warmed();
        trace.Observe(++ticks, new RealtimeLongFrameContext(1, 2, RealtimeContextAvailability.Setup, null, null, null, null, null, null, true));
        ticks += 40000;
        trace.Observe(ticks, new RealtimeLongFrameContext(3, 4, RealtimeContextAvailability.Teardown, null, null, null, null, null, null, false));
        using JsonDocument json = Report(trace);
        JsonElement record = json.RootElement.GetProperty("first_long_intervals")[0];
        TestAssert.Equal("Setup", record.GetProperty("start").GetProperty("context").GetProperty("availability").GetString(), "setup endpoint");
        TestAssert.Equal("Teardown", record.GetProperty("end").GetProperty("context").GetProperty("availability").GetString(), "teardown endpoint");
        TestAssert.Equal(JsonValueKind.Null, record.GetProperty("end").GetProperty("context").GetProperty("sector").ValueKind, "unavailable sector is null");
    }

    private static RealtimeLongFrameContext Context(ulong process = 1, ulong physics = 2,
        PrologueSectorId sector = PrologueSectorId.CanyonEntrance, ulong player = 111,
        float x = 1, float y = 2, bool focus = true) => new(process, physics,
        RealtimeContextAvailability.Available, sector, PrologueGameFlowState.Playing,
        PrologueObjective.ReachBasin, player, x, y, focus);
    private static (RealtimeLongFrameTrace Trace, ulong Ticks) Warmed()
    {
        var trace = new RealtimeLongFrameTrace(); ulong ticks = 1000000;
        trace.Observe(ticks, Context()); for (int i = 0; i < 120; i++) Next(trace, ref ticks, 1);
        return (trace, ticks);
    }
    private static void Next(RealtimeLongFrameTrace trace, ref ulong ticks, ulong interval)
    { ticks += interval; trace.Observe(ticks, Context()); }
    private static JsonDocument Report(RealtimeLongFrameTrace trace) => JsonDocument.Parse(trace.CreateJson("route_completed", null));
    private static void Equal(int expected, JsonElement root, string field) => TestAssert.Equal(expected, root.GetProperty(field).GetInt32(), "trace reports " + field);
}
