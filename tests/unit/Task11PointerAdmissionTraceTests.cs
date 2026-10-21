using System;
using System.Text.Json;
using Godot;
using Knight.Player;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

/// <summary>Pure retention tests: no scene, input delivery, clock, filesystem or engine run.</summary>
internal static class Task11PointerAdmissionTraceTests
{
    internal const int CaseCount = 13;

    internal static void Run()
    {
        DormantArmOnceAndFreeze();
        Capacity("delivered", 128);
        Capacity("driver", 64);
        Capacity("sampled", 32);
        SpanBoundaryAndExpiry();
        CommonOrdering();
        IdentityIsNotCoordinateProvenance();
        AncillaryFailureIsRetained();
        JsonAndFailedRouteCompleteness();
        NonFiniteCoordinatesRemainRetainedAndIncomplete();
        NonFiniteTransformsRemainRetainedAndIncomplete();
        NonFiniteReaderDataRemainRetainedAndIncomplete();
        FiniteControlRemainsComplete();
    }

    // Break caught: dormant observations retained, rearming, or changing the first freeze reason.
    private static void DormantArmOnceAndFreeze()
    {
        var trace = new Task11PointerTraceRetention();
        trace.Record("driver", Record(100));
        using (JsonDocument json = Report(trace)) Equal(0, json.RootElement, "driver_count");
        TestAssert.True(trace.Arm(100), "first arm accepted");
        TestAssert.True(!trace.Arm(101), "second arm refused");
        trace.Record("driver", Record(100)); trace.Freeze("first_aim_observed");
        trace.Freeze("later_cleanup"); trace.Record("driver", Record(100));
        TestAssert.True(!trace.Arm(100), "frozen scope never rearms");
        using JsonDocument frozen = Report(trace);
        Equal(1, frozen.RootElement, "driver_count");
        TestAssert.Equal("frozen", frozen.RootElement.GetProperty("lifecycle").GetString(), "frozen lifecycle");
        TestAssert.Equal("first_aim_observed", frozen.RootElement.GetProperty("freeze_reason").GetString(), "first freeze retained");
        var neverArmed = new Task11PointerTraceRetention(); neverArmed.Freeze("runner_finally"); neverArmed.Freeze("again");
        TestAssert.True(!neverArmed.Arm(100), "dormant finalization prevents subsequent arm");
    }

    // Break caught: eviction/unbounded storage, or omission at exact capacity instead of one-over.
    private static void Capacity(string kind, int capacity)
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        for (int i = 0; i < capacity; i++) trace.Record(kind, Record(100, (ulong)i + 1));
        using (JsonDocument exact = Report(trace))
        {
            Equal(capacity, exact.RootElement, kind + "_count");
            Equal(0, exact.RootElement, kind + "_omitted");
        }
        trace.Record(kind, Record(100, 999)); trace.Freeze("first_aim_observed");
        using JsonDocument over = Report(trace);
        Equal(capacity, over.RootElement, kind + "_count"); Equal(1, over.RootElement, kind + "_omitted");
        TestAssert.Equal(1UL, over.RootElement.GetProperty(kind)[0].GetProperty("ordinal").GetUInt64(), "first is not evicted");
        TestAssert.True(!over.RootElement.GetProperty("observation_complete").GetBoolean(), "overflow incomplete");
    }

    // Break caught: off-by-one at 32 physics frames, unsigned regression, or accepting expired data.
    private static void SpanBoundaryAndExpiry()
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        trace.Record("sampled", Record(132)); trace.Record("delivered", Record(133));
        trace.Record("driver", Record(134));
        using JsonDocument json = Report(trace);
        Equal(1, json.RootElement, "sampled_count"); Equal(0, json.RootElement, "delivered_count");
        Equal(1, json.RootElement, "span_omitted");
        TestAssert.Equal("physics_span_expired", json.RootElement.GetProperty("freeze_reason").GetString(), "expires at33");
        var regressed = new Task11PointerTraceRetention(); regressed.Arm(100); regressed.Record("driver", Record(99));
        using JsonDocument regression = Report(regressed);
        Equal(1, regression.RootElement, "span_omitted");
        TestAssert.Equal("physics_frame_regressed", regression.RootElement.GetProperty("freeze_reason").GetString(), "regression explicit");
    }

    // Break caught: separate per-list ordinals incorrectly suggesting simultaneous/independent order.
    private static void CommonOrdering()
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        trace.Record("driver", Record(100)); trace.Record("delivered", Record(100)); trace.Record("sampled", Record(101));
        using JsonDocument json = Report(trace);
        TestAssert.Equal(1UL, json.RootElement.GetProperty("driver")[0].GetProperty("ordinal").GetUInt64(), "driver ordinal");
        TestAssert.Equal(2UL, json.RootElement.GetProperty("delivered")[0].GetProperty("ordinal").GetUInt64(), "delivery ordinal");
        TestAssert.Equal(3UL, json.RootElement.GetProperty("sampled")[0].GetProperty("ordinal").GetUInt64(), "sample ordinal");
    }

    // Break caught: treating equal coordinates, copied IDs or unmatched IDs as native/hardware origin.
    private static void IdentityIsNotCoordinateProvenance()
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        trace.Record("driver", Record(100, 42));
        trace.Record("delivered", Record(100, 42)); trace.Record("delivered", Record(100, 43));
        using JsonDocument json = Report(trace); JsonElement delivered = json.RootElement.GetProperty("delivered");
        TestAssert.Equal("known_injected_id_match", delivered[0].GetProperty("identity_correlation").GetString(), "ID match only");
        TestAssert.Equal("unmatched", delivered[1].GetProperty("identity_correlation").GetString(), "equal positions do not establish provenance");
        TestAssert.Equal(JsonValueKind.Null, delivered[1].GetProperty("requested_world").ValueKind,
            "unmatched delivery never invents a requested world position");
    }

    // Break caught: diagnostic exception escaping or producing falsely complete evidence.
    private static void AncillaryFailureIsRetained()
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        trace.Guard(() => throw new InvalidOperationException("fixture_observer_failure"));
        trace.Record("driver", Record(100));
        using JsonDocument json = Report(trace); Equal(1, json.RootElement, "observer_failures");
        Equal(0, json.RootElement, "driver_count");
        TestAssert.True(json.RootElement.GetProperty("observer_error").GetString().Contains("fixture_observer_failure"), "fault retained");
        TestAssert.Equal("observer_error", json.RootElement.GetProperty("freeze_reason").GetString(), "fault freezes scope");
        TestAssert.True(!json.RootElement.GetProperty("observation_complete").GetBoolean(), "fault incomplete");
    }

    // Break caught: dropping fields/limits or equating a bounded observation with complete route evidence.
    private static void JsonAndFailedRouteCompleteness()
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        trace.Record("sampled", Record(101, 42) with
        {
            Phase = "after_E_precheck", PlayerId = 11, ChainId = 22, ReaderAvailable = true,
            Frame = new PlayerInputFrame(0, default, false, false, false, false, false,
                MouseWorld: new Vector2(10, 20), GrappleModePressed: true, PrimaryPressed: true),
            FrameSequence = 50, LastViewport = new Vector2(30, 40), MotionSequence = 60, HasMotion = true,
            RequestedWorld = new Vector2(70, 80), HasRequestedWorld = true, PlayerLocal = new Vector2(90, 100), ChainAnchor = new Vector2(110, 120),
            GlobalWithCanvas = new Transform2D(Vector2.Right, Vector2.Down, new Vector2(1, 2)),
            Canvas = new Transform2D(Vector2.Right, Vector2.Down, new Vector2(3, 4)),
            Final = new Transform2D(Vector2.Right, Vector2.Down, new Vector2(5, 6))
        });
        trace.Freeze("first_aim_observed");
        using JsonDocument failed = JsonDocument.Parse(trace.CreateJson("failed")); JsonElement root = failed.RootElement;
        Equal(1, root, "version"); Equal(128, root, "delivered_capacity"); Equal(64, root, "driver_capacity");
        Equal(32, root, "sampled_capacity"); Equal(32, root, "max_physics_span");
        TestAssert.True(root.GetProperty("observation_complete").GetBoolean(), "bounded observation can finish even if route fails later");
        TestAssert.True(!root.GetProperty("complete_route_evidence").GetBoolean(), "failed route never complete evidence");
        JsonElement sample = root.GetProperty("sampled")[0];
        TestAssert.Equal("after_E_precheck", sample.GetProperty("phase").GetString(), "phase retained");
        TestAssert.Equal(11UL, sample.GetProperty("player_id").GetUInt64(), "player identity retained");
        TestAssert.Equal(22UL, sample.GetProperty("chain_id").GetUInt64(), "chain identity retained");
        TestAssert.Equal(6f, sample.GetProperty("position")[0].GetSingle(), "coordinates retained");
        TestAssert.Equal(101UL, sample.GetProperty("physics_frame").GetUInt64(), "observation physics retained");
        TestAssert.Equal(1234UL, sample.GetProperty("observation_usec").GetUInt64(), "observation timestamp retained");
        TestAssert.Equal(10f, sample.GetProperty("current_frame").GetProperty("mouse_world")[0].GetSingle(), "sampled world coordinate retained");
        TestAssert.True(sample.GetProperty("current_frame").GetProperty("primary_pressed").GetBoolean(), "sampled button retained");
        TestAssert.Equal(50UL, sample.GetProperty("current_frame_sequence").GetUInt64(), "sampled sequence retained");
        TestAssert.Equal(60UL, sample.GetProperty("mouse_motion_sequence").GetUInt64(), "reader motion sequence retained");
        TestAssert.Equal(30f, sample.GetProperty("last_observed_mouse_viewport_position")[0].GetSingle(), "reader coordinate retained");
        TestAssert.True(sample.GetProperty("has_observed_mouse_motion").GetBoolean(), "reader motion availability retained");
        TestAssert.Equal(70f, sample.GetProperty("requested_world")[0].GetSingle(), "requested world retained");
        TestAssert.Equal(90f, sample.GetProperty("player_local")[0].GetSingle(), "local coordinate retained");
        TestAssert.Equal(110f, sample.GetProperty("chain_anchor")[0].GetSingle(), "anchor retained");
        TestAssert.Equal(1f, sample.GetProperty("global_transform_with_canvas")[4].GetSingle(), "global-with-canvas retained");
        TestAssert.Equal(3f, sample.GetProperty("canvas_transform")[4].GetSingle(), "canvas retained");
        TestAssert.Equal(5f, sample.GetProperty("final_transform")[4].GetSingle(), "final retained");
        TestAssert.Equal("unavailable", root.GetProperty("limits").GetProperty("atomic_sampling_transform").GetString(), "transform honesty");
        using JsonDocument completed = Report(trace);
        TestAssert.True(completed.RootElement.GetProperty("complete_route_evidence").GetBoolean(), "complete fixture requires successful route too");
    }

    // Break caught: validating only one coordinate field, dropping/clamping anomalies, or claiming complete evidence.
    private static void NonFiniteCoordinatesRemainRetainedAndIncomplete()
    {
        foreach (var (value, token) in new[] { (float.NaN, "NaN"), (float.PositiveInfinity, "Infinity"), (float.NegativeInfinity, "-Infinity") })
        {
            Vector2 bad = new(value, 1);
            var fixtures = new[]
            {
                (Record(100) with { Position = bad }, "position"),
                (Record(100) with { GlobalPosition = bad }, "global_position"),
                (Record(100) with { Relative = bad }, "relative"),
                (Record(100) with { RequestedWorld = bad, HasRequestedWorld = true }, "requested_world"),
                (Record(100) with { PlayerLocal = bad, HasRequestedWorld = true }, "player_local"),
                (Record(100) with { ChainAnchor = bad }, "chain_anchor")
            };
            var trace = new Task11PointerTraceRetention(); trace.Arm(100);
            foreach (var (record, _) in fixtures) trace.Record("driver", record);
            trace.Record("driver", Record(100)); trace.Freeze("first_aim_observed");
            using JsonDocument json = Report(trace); AssertInvalid(json.RootElement, "driver", 6, 7);
            for (int i = 0; i < fixtures.Length; i++)
                TestAssert.Equal(token, json.RootElement.GetProperty("driver")[i].GetProperty(fixtures[i].Item2)[0].GetString(), "coordinate anomaly preserved");
            TestAssert.Equal("finite", json.RootElement.GetProperty("driver")[6].GetProperty("data_status").GetString(), "finite record after anomaly still retained");
        }
    }

    // Break caught: omitting a transform or its basis/origin component from finite-data classification.
    private static void NonFiniteTransformsRemainRetainedAndIncomplete()
    {
        foreach (var (value, token) in new[] { (float.NaN, "NaN"), (float.PositiveInfinity, "Infinity") })
        {
            var trace = new Task11PointerTraceRetention(); trace.Arm(100);
            for (int component = 0; component < 6; component++)
            {
                float[] parts = { 1, 0, 0, 1, 2, 3 }; parts[component] = value;
                Transform2D bad = new(new Vector2(parts[0], parts[1]), new Vector2(parts[2], parts[3]), new Vector2(parts[4], parts[5]));
                trace.Record("sampled", Record(100) with { GlobalWithCanvas = bad });
                trace.Record("sampled", Record(100) with { Canvas = bad });
                trace.Record("sampled", Record(100) with { Final = bad });
            }
            trace.Freeze("first_aim_observed");
            using JsonDocument json = Report(trace); AssertInvalid(json.RootElement, "sampled", 18, 18);
            string[] fields = { "global_transform_with_canvas", "canvas_transform", "final_transform" };
            for (int component = 0; component < 6; component++)
                for (int field = 0; field < 3; field++)
                    TestAssert.Equal(token, json.RootElement.GetProperty("sampled")[component * 3 + field].GetProperty(fields[field])[component].GetString(), "transform anomaly preserved");
        }
    }

    // Break caught: assuming public reader data is finite or checking MouseWorld but not axis/aim/latest viewport.
    private static void NonFiniteReaderDataRemainRetainedAndIncomplete()
    {
        foreach (var (value, token) in new[] { (float.NaN, "NaN"), (float.NegativeInfinity, "-Infinity") })
        {
            var frame = new PlayerInputFrame(0, default, false, false, false, false, false);
            var fixtures = new[]
            {
                Record(100) with { ReaderAvailable = true, Frame = frame with { MoveAxis = value } },
                Record(100) with { ReaderAvailable = true, Frame = frame with { Aim = new Vector2(1, value) } },
                Record(100) with { ReaderAvailable = true, Frame = frame with { MouseWorld = new Vector2(1, value) } },
                Record(100) with { ReaderAvailable = true, LastViewport = new Vector2(1, value) }
            };
            var trace = new Task11PointerTraceRetention(); trace.Arm(100);
            foreach (var record in fixtures) trace.Record("delivered", record);
            trace.Freeze("first_aim_observed");
            using JsonDocument json = Report(trace); AssertInvalid(json.RootElement, "delivered", 4, 4);
            JsonElement delivered = json.RootElement.GetProperty("delivered");
            TestAssert.Equal(token, delivered[0].GetProperty("current_frame").GetProperty("move_axis").GetString(), "axis anomaly preserved");
            TestAssert.Equal(token, delivered[1].GetProperty("current_frame").GetProperty("aim")[1].GetString(), "aim anomaly preserved");
            TestAssert.Equal(token, delivered[2].GetProperty("current_frame").GetProperty("mouse_world")[1].GetString(), "sampled world anomaly preserved");
            TestAssert.Equal(token, delivered[3].GetProperty("last_observed_mouse_viewport_position")[1].GetString(), "latest viewport anomaly preserved");
        }
    }

    // Break caught: classifying every record invalid or breaking otherwise finite successful-window completeness.
    private static void FiniteControlRemainsComplete()
    {
        var trace = new Task11PointerTraceRetention(); trace.Arm(100);
        foreach (string kind in new[] { "driver", "delivered", "sampled" }) trace.Record(kind, Record(100));
        trace.Freeze("first_aim_observed"); using JsonDocument json = Report(trace);
        Equal(0, json.RootElement, "non_finite_records");
        TestAssert.True(json.RootElement.GetProperty("observation_complete").GetBoolean() &&
            json.RootElement.GetProperty("complete_route_evidence").GetBoolean(), "finite completed control remains complete");
        foreach (string kind in new[] { "driver", "delivered", "sampled" })
            TestAssert.Equal("finite", json.RootElement.GetProperty(kind)[0].GetProperty("data_status").GetString(), "finite record explicit");
    }

    private static void AssertInvalid(JsonElement root, string kind, int invalid, int retained)
    {
        Equal(invalid, root, "non_finite_records"); Equal(retained, root, kind + "_count");
        Equal(0, root, "observer_failures"); Equal(0, root, kind + "_omitted");
        TestAssert.True(!root.GetProperty("observation_complete").GetBoolean() &&
            !root.GetProperty("complete_route_evidence").GetBoolean(), "invalid data makes both completeness claims false");
        TestAssert.Equal("first_aim_observed", root.GetProperty("freeze_reason").GetString(), "anomaly does not force a lifecycle interruption");
        for (int i = 0; i < invalid; i++)
            TestAssert.Equal("non_finite", root.GetProperty(kind)[i].GetProperty("data_status").GetString(), "retained anomaly explicit");
    }

    private static Task11PointerTraceRecord Record(ulong physics, ulong eventId = 0) => new()
    {
        PhysicsFrame = physics, ProcessFrame = 9, Usec = 1234, EventId = eventId,
        Phase = "fixture", EventKind = "motion", Position = new Vector2(6, 7), GlobalPosition = new Vector2(6, 7)
    };
    private static JsonDocument Report(Task11PointerTraceRetention trace) => JsonDocument.Parse(trace.CreateJson("route_completed"));
    private static void Equal(int expected, JsonElement root, string field) =>
        TestAssert.Equal(expected, root.GetProperty(field).GetInt32(), "pointer trace " + field);
}
