using System;
using System.Text.Json;
using Godot;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

internal static class KeyboardInputTraceBufferTests
{
    internal static void Run()
    {
        // Break: retain first events/drop latest or grow unbounded instead of tail.
        var tail = new KeyboardInputTraceBuffer();
        for (int index = 1; index <= 260; index++) tail.Record(Row("received", (ulong)index));
        using (JsonDocument json = JsonDocument.Parse(tail.CreateJson("failed")))
        {
            JsonElement root = json.RootElement, rows = root.GetProperty("records");
            TestAssert.Equal(256, root.GetProperty("retained_count").GetInt32(), "keyboard trace retains the last256 actual events");
            TestAssert.Equal(4UL, root.GetProperty("overwritten_count").GetUInt64(), "tail overwrite is explicit");
            TestAssert.Equal(5UL, rows[0].GetProperty("ordinal").GetUInt64(), "oldest retained event is fifth");
            TestAssert.Equal(260UL, rows[255].GetProperty("ordinal").GetUInt64(), "newest survives overflow");
            TestAssert.True(!root.GetProperty("full_history_retained").GetBoolean(), "tail is not a complete history");
        }
        // Break: cleanup replaces first failure or records after freeze.
        tail.Record(Row("owned_guard", 999)); tail.Freeze("owned_key_withdrawn");
        string frozen = tail.CreateJson("failed");
        tail.Record(Row("after_flush", 1000)); tail.Freeze("dispose");
        TestAssert.Equal(frozen, tail.CreateJson("failed"), "first failure tail remains byte-identical after cleanup");
        // Break: correlate by phase/key/coordinates or treat unmatched as hardware.
        var identity = new KeyboardInputTraceBuffer();
        identity.Record(Row("before_parse", 71)); identity.Record(Row("received", 71)); identity.Record(Row("received", 72));
        using (JsonDocument json = JsonDocument.Parse(identity.CreateJson("failed")))
        {
            JsonElement rows = json.RootElement.GetProperty("records");
            TestAssert.True(rows[1].GetProperty("known_injected_id_match").GetBoolean() &&
                !rows[2].GetProperty("known_injected_id_match").GetBoolean(), "ID match is only weak original instance correlation");
            TestAssert.True(!json.RootElement.GetProperty("os_origin_proven").GetBoolean(), "unmatched never means hardware origin");
        }
        // Break: observer exceptions are lost or replace the original guarded failure.
        var failure = new KeyboardInputTraceBuffer(); failure.Record(Row("received", 81));
        failure.MarkFailure(new InvalidOperationException("first_observation_failure"));
        failure.MarkFailure(new InvalidOperationException("later_cleanup"));
        using (JsonDocument json = JsonDocument.Parse(failure.CreateJson("failed")))
        {
            TestAssert.Equal(1, json.RootElement.GetProperty("observer_failures").GetInt32(), "first observer failure freezes once");
            TestAssert.True(json.RootElement.GetProperty("observer_error").GetString().Contains("first_observation_failure"), "first observer error retained");
            TestAssert.Equal(1, json.RootElement.GetProperty("retained_count").GetInt32(), "preceding evidence survives observer failure");
        }
        var wrapped = new KeyboardInputTraceBuffer();
        for(int index=1;index<=256;index++) wrapped.Record(Row("received",(ulong)index));
        wrapped.Record(Row("before_parse",700)); wrapped.Record(Row("received",700));
        using(JsonDocument json=JsonDocument.Parse(wrapped.CreateJson("fixture")))
            TestAssert.True(json.RootElement.GetProperty("records")[255].GetProperty("known_injected_id_match").GetBoolean(),
                "wrap retains matching injection identity");
        for(int index=1;index<=256;index++) wrapped.Record(Row("received",(ulong)index));
        wrapped.Record(Row("received",700));
        wrapped.Record(Row("before_parse",0)); wrapped.Record(Row("received",0));
        using(JsonDocument json=JsonDocument.Parse(wrapped.CreateJson("fixture")))
        {
            JsonElement rows=json.RootElement.GetProperty("records");
            TestAssert.True(!rows[253].GetProperty("known_injected_id_match").GetBoolean() &&
                !rows[255].GetProperty("known_injected_id_match").GetBoolean(), "evicted and zero identities never claim injection match");
        }
        GD.Print("KEYBOARD_INPUT_TRACE_BUFFER_OK cases=5");
        RunFocus();
    }

    internal static void RunFocus()
    {
        // Break: null/racing/failed native queries are mislabeled as known ownership.
        (long Before,long After,uint Pid,uint Tid,int Error,string Want)[] nativeCases =
        [ (100,100,7,9,0,"observed"), (0,0,0,0,0,"no_foreground"),
          (0,200,0,0,0,"raced"), (100,200,7,9,0,"raced"),
          (100,100,0,0,1400,"query_failed"), (100,100,0,9,0,"query_failed") ];
        foreach (var item in nativeCases)
        {
            KeyboardForegroundSnapshot snapshot=KeyboardForegroundProbe.Classify(item.Before,item.After,item.Pid,item.Tid,item.Error);
            TestAssert.True(snapshot.Status==item.Want && snapshot.BeforeWindow==item.Before &&
                snapshot.AfterWindow==item.After && snapshot.ProcessId==item.Pid &&
                snapshot.ThreadId==item.Tid && snapshot.Win32Error==item.Error,
                "foreground classification preserves raw identity/error but does not invent known owner");
        }
        // Break: new evidence shares the old key freeze and loses later focus events.
        var keyboard = new KeyboardInputTraceBuffer();
        keyboard.Record(Row("owned_guard", 91)); keyboard.Freeze("owned_key_withdrawn");
        string original = keyboard.CreateJson("failed");
        var focus = new KeyboardFocusTraceBuffer();
        focus.Record(new KeyboardFocusTraceRecord("application_focus_out", 1, 2, 3, true,
            100, 7, new KeyboardForegroundSnapshot("observed", 200, 200, 8, 9)));
        using (JsonDocument json = JsonDocument.Parse(focus.CreateJson("failed")))
            TestAssert.True(json.RootElement.GetProperty("retained_count").GetInt32() == 1,
                "KEYBOARD_FOCUS_RETAINS_AFTER_KEY_FREEZE_RED independent focus survives original key failure");
        TestAssert.Equal(original, keyboard.CreateJson("failed"), "focus observation never rewrites original failure");
        // Break: confuse an observed other-process window with own, or infer raced/zero ownership.
        focus.Record(new KeyboardFocusTraceRecord("window_focus_exited", 4, 5, 6, false,
            100, 7, new KeyboardForegroundSnapshot("observed", 100, 100, 7, 9)));
        focus.Record(new KeyboardFocusTraceRecord("application_focus_in", 7, 8, 9, true,
            100, 7, new KeyboardForegroundSnapshot("raced", 100, 200, 7, 9)));
        focus.Record(new KeyboardFocusTraceRecord("cleanup", 10, 11, 12, null,
            100, 7, new KeyboardForegroundSnapshot("no_foreground", 0, 0, 0, 0)));
        using (JsonDocument json = JsonDocument.Parse(focus.CreateJson("failed")))
        {
            JsonElement root = json.RootElement, rows = root.GetProperty("records");
            TestAssert.True(rows[0].GetProperty("foreground_owner_known").GetBoolean() &&
                !rows[0].GetProperty("foreground_is_own_process").GetBoolean() &&
                rows[1].GetProperty("foreground_is_own_window").GetBoolean() &&
                rows[1].GetProperty("foreground_is_own_process").GetBoolean() &&
                !rows[2].GetProperty("foreground_owner_known").GetBoolean() &&
                !rows[2].GetProperty("foreground_is_own_window").GetBoolean() &&
                !rows[2].GetProperty("foreground_is_own_process").GetBoolean() &&
                !rows[3].GetProperty("foreground_owner_known").GetBoolean(),
                "unknown and raced snapshots never infer foreground ownership");
            TestAssert.True(root.GetProperty("unknown_observations").GetUInt64() == 2 &&
                rows[3].GetProperty("window_focused").ValueKind == JsonValueKind.Null &&
                !root.GetProperty("deactivation_cause_proven").GetBoolean(),
                "unknown state and attribution limits remain explicit");
        }
        // Break: errors freeze the new tail early, or cleanup overwrites the first stop reason.
        focus.MarkFailure(new InvalidOperationException("first_focus_failure"));
        focus.Record(new KeyboardFocusTraceRecord("later_window_event", 13, 14, 15, false,
            100, 7, new KeyboardForegroundSnapshot("unsupported", 0, 0, 0, 0)));
        focus.MarkFailure(new InvalidOperationException("second_failure"));
        focus.Freeze("runner_finally");
        string stopped = focus.CreateJson("failed");
        focus.Record(default); focus.MarkFailure(new InvalidOperationException("after_stop")); focus.Freeze("finalize");
        TestAssert.Equal(stopped, focus.CreateJson("failed"), "focus stop is idempotent and prevents later events or errors");
        using (JsonDocument json = JsonDocument.Parse(stopped))
            TestAssert.True(json.RootElement.GetProperty("retained_count").GetInt32() == 5 &&
                json.RootElement.GetProperty("observer_failures").GetInt32() == 2 &&
                json.RootElement.GetProperty("first_observer_error").GetString() == "first_focus_failure" &&
                json.RootElement.GetProperty("freeze_reason").GetString() == "runner_finally",
                "observer failure preserves later evidence and only first error text");
        // Break: unbounded retention or prefix retention loses the final focus event.
        var overflow = new KeyboardFocusTraceBuffer();
        for (ulong index = 1; index <= 67; index++)
            overflow.Record(new KeyboardFocusTraceRecord("window_focus_exited", index, index, index, false,
                100, 7, new KeyboardForegroundSnapshot("observed", 200, 200, 8, 9)));
        using (JsonDocument json = JsonDocument.Parse(overflow.CreateJson("failed")))
            TestAssert.True(json.RootElement.GetProperty("retained_count").GetInt32() == 64 &&
                json.RootElement.GetProperty("overwritten_count").GetUInt64() == 3 &&
                json.RootElement.GetProperty("records")[0].GetProperty("ordinal").GetUInt64() == 4 &&
                json.RootElement.GetProperty("records")[63].GetProperty("ordinal").GetUInt64() == 67 &&
                !json.RootElement.GetProperty("full_history_retained").GetBoolean(),
                "bounded focus tail keeps latest events with truthful overflow");
        GD.Print("KEYBOARD_FOCUS_TRACE_BUFFER_OK cases=4 native_classifications=6 independent_keyboard_freeze=true");
    }

    private static KeyboardInputTraceRecord Row(string phase, ulong eventId) =>
        new(phase, Key.W, true, false, eventId, 100, 200, 300, 400, true, true, 1, 1);
}
