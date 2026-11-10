using System;
using System.Text.Json;
using Godot;

namespace Knight.Tests;

// Test-only, immediate scalar snapshots. No borrowed native wrapper survives a call.
internal static class RecoveryResourceBoundaryTrace
{
    private const int RowCap = 512;
    private static bool _enabled, _complete;
    private static int _sequence, _rows, _faults, _omitted;
    private static string _scope = "";

    internal static void RunControls()
    {
        TestAssert.Equal(42UL, ResolveKnownId(0UL, 42UL, true),
            "RECOVERY_TRACE_CONTROL_RED valid observation supplies its identity");
        TestAssert.Equal(42UL, ResolveKnownId(42UL, 0UL, false),
            "RECOVERY_TRACE_CONTROL_RED invalid observation preserves the known identity");
        TestAssert.Equal(0UL, ResolveKnownId(0UL, 0UL, false),
            "RECOVERY_TRACE_CONTROL_RED unknown invalid observation remains unknown");
        AssertSummaryFailureRejected(RejectSummary, ControlSummary, "write");
        AssertSummaryFailureRejected(IgnoreSummary, RejectSummaryFactory, "serialize");
        AssertPrimaryPreserved(RejectSummary, ControlSummary, "write");
        AssertPrimaryPreserved(IgnoreSummary, RejectSummaryFactory, "serialize");
        GD.Print("RECOVERY_TRACE_CONTROLS_OK native=false");
    }

    internal static IDisposable Begin()
    {
        _sequence = _rows = _faults = _omitted = 0;
        _scope = "";
        _complete = false;
        _enabled = true;
        return new Capture();
    }

    internal static void SetScope(string scope)
    {
        if (_enabled) _scope = scope ?? "";
    }

    internal static void Point(string stage, string role)
    {
        if (!_enabled || !AdmitRow()) return;
        EmitRow(stage, role, 0UL, 0UL, false, "", "", "");
    }

    internal static ulong Observe(string stage, string role, GodotObject borrowed, ulong knownId = 0)
    {
        if (!_enabled) return knownId;
        if (!AdmitRow()) return knownId;
        bool valid = false;
        ulong observedId = 0;
        string managedType = "", nativeClass = "", path = "";
        try
        {
            valid = GodotObject.IsInstanceValid(borrowed);
            if (valid)
            {
                observedId = borrowed.GetInstanceId();
                managedType = borrowed.GetType().FullName ?? "";
                nativeClass = borrowed.GetClass().ToString();
                if (borrowed is Resource resource) path = resource.ResourcePath;
                else if (borrowed is Node node) path = node.SceneFilePath;
            }
        }
        catch { Fault(); }
        knownId = ResolveKnownId(knownId, observedId, valid);
        EmitRow(stage, role, knownId, observedId, valid, managedType, nativeClass, path);
        return knownId;
    }

    internal static PackedScene Loaded(PackedScene borrowed, string role = "scene")
    {
        Observe("load-return", role, borrowed);
        Point("instantiate-enter", role);
        return borrowed;
    }

    internal static void AssertComplete()
    {
        TestAssert.True(_enabled, "RECOVERY_RESOURCE_TRACE inactive");
        TestAssert.True(_faults == 0 && _omitted == 0 && _rows > 0,
            $"RECOVERY_RESOURCE_TRACE incomplete rows={_rows} faults={_faults} omitted={_omitted}");
        _complete = true;
        FinishCapture(true, PrintSummary, BuildSummary);
    }

    private static ulong ResolveKnownId(ulong known, ulong observed, bool valid)
    {
        return valid ? observed : known;
    }

    private static bool AdmitRow()
    {
        _complete = false;
        if (_sequence == RowCap)
        {
            _omitted++;
            return false;
        }
        _sequence++;
        return true;
    }

    private static void Fault()
    {
        _faults++;
        _complete = false;
    }

    private static void EmitRow(string stage, string role, ulong knownId, ulong observedId,
        bool valid, string managedType, string nativeClass, string path)
    {
        ulong physics = 0, process = 0;
        try { physics = Engine.GetPhysicsFrames(); }
        catch { Fault(); }
        try { process = Engine.GetProcessFrames(); }
        catch { Fault(); }
        try
        {
            GD.Print("RECOVERY_RESOURCE_TRACE " + RowJson(_sequence, stage, role, knownId,
                observedId, valid, managedType, nativeClass, path, physics, process) + "}");
            _rows++;
        }
        catch { Fault(); }
    }

    private static string Quote(string value) => JsonSerializer.Serialize(value ?? "");

    private static string RowJson(int sequence, string stage, string role, ulong knownId,
        ulong observedId, bool valid, string managedType, string nativeClass, string path,
        ulong physics, ulong process) =>
        $"{{\"sequence\":{sequence},\"scope\":{Quote(_scope)},\"stage\":{Quote(stage)}," +
        $"\"role\":{Quote(role)},\"knownId\":{knownId},\"observedId\":{observedId}," +
        $"\"valid\":{(valid ? "true" : "false")},\"managedType\":{Quote(managedType)}," +
        $"\"nativeClass\":{Quote(nativeClass)},\"path\":{Quote(path)}," +
        $"\"physics\":{physics},\"process\":{process},\"faults\":{_faults}," +
        "\"timing_perturbed\":true,\"cause_proved\":false";

    private static void AssertSummaryFailureRejected(Action<string> writer, Func<string> factory,
        string faultStage)
    {
        bool rejected = false;
        using (Begin())
        {
            try { FinishCapture(true, writer, factory); }
            catch (InvalidOperationException) { rejected = true; }
        }
        TestAssert.True(rejected,
            $"RECOVERY_TRACE_SUMMARY_OUTPUT_RED normal {faultStage} failure must reject success");
    }

    private static void AssertPrimaryPreserved(Action<string> writer, Func<string> factory,
        string faultStage)
    {
        var primary = new InvalidOperationException("original recovery failure");
        Exception observed = null;
        using (Begin())
        {
            try
            {
                try { throw primary; }
                finally { FinishCapture(false, writer, factory); }
            }
            catch (Exception exception) { observed = exception; }
        }
        TestAssert.True(ReferenceEquals(primary, observed),
            $"RECOVERY_TRACE_SUMMARY_OUTPUT_RED unwind {faultStage} failure must preserve primary");
    }

    private static void RejectSummary(string _) => throw new InvalidOperationException("controlled summary write failure");
    private static string RejectSummaryFactory() => throw new InvalidOperationException("controlled summary serialization failure");
    private static void IgnoreSummary(string _) { }
    private static string ControlSummary() => "controlled scalar summary";
    private static void PrintSummary(string line) => GD.Print(line);

    private static string BuildSummary() =>
        "RECOVERY_RESOURCE_TRACE " + RowJson(_sequence + 1, "summary", "trace",
            0UL, 0UL, false, "", "", "", 0UL, 0UL) +
        $",\"rows\":{_rows},\"omitted\":{_omitted}," +
        $"\"complete\":{(_complete && _faults == 0 && _omitted == 0 && _rows > 0 ? "true" : "false")}}}";

    private static void FinishCapture(bool normalCompletion, Action<string> writer, Func<string> factory)
    {
        if (!_enabled) return;
        _enabled = false;
        bool emitted = false;
        try { writer(factory()); emitted = true; }
        catch { Fault(); }
        _complete &= emitted;
        if (normalCompletion)
            TestAssert.True(emitted && _faults == 0,
                "RECOVERY_RESOURCE_TRACE summary output failed; success is not admissible");
    }

    private sealed class Capture : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            FinishCapture(false, PrintSummary, BuildSummary);
        }
    }
}
