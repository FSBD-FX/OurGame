using System;
using System.Text.Json;
using Godot;

namespace Knight.Tests.Presentation;

internal readonly record struct KeyboardInputTraceRecord(
    string Phase, Key Key, bool Pressed, bool Echo, ulong EventId,
    ulong ProcessFrame, ulong PhysicsFrame, ulong Usec, ulong SampleSequence,
    bool Focused, bool RawDown, ulong RawMask, ulong OwnedMask, bool KnownInjectedId = false,
    int PhysicalKeycode = 0, int LogicalKeycode = 0, string EventKind = null,
    int EventCode = 0, int EventDevice = 0, string EventAction = null,
    bool RawPrimaryHeld = false, bool RawPrimaryPressed = false, bool RawPrimaryReleased = false,
    bool RawMouseLeftDown = false, bool SampledKeyboardActions = false,
    bool SampledPrimaryHeld = false, bool SampledPrimaryPressed = false);

internal readonly record struct KeyboardForegroundSnapshot(
    string Status, long BeforeWindow, long AfterWindow, uint ProcessId, uint ThreadId, int Win32Error = 0);

internal readonly record struct KeyboardFocusTraceRecord(
    string Phase, ulong ProcessFrame, ulong PhysicsFrame, ulong Usec, bool? WindowFocused,
    long OwnWindow, int OwnProcessId, KeyboardForegroundSnapshot Foreground);

// Separate lifecycle: the first keyboard failure must not freeze focus cleanup evidence.
internal sealed class KeyboardFocusTraceBuffer
{
    private const int Capacity = 64;
    private readonly KeyboardFocusTraceRecord[] _rows = new KeyboardFocusTraceRecord[Capacity];
    private int _count, _next, _errors;
    private ulong _total, _overwritten, _unknown;
    private string _reason, _firstError;
    internal bool Frozen { get; private set; }
    internal void Record(in KeyboardFocusTraceRecord record)
    {
        if (Frozen) return;
        _rows[_next] = record; _total++;
        _next = (_next + 1) % Capacity;
        if (_count < Capacity) _count++; else _overwritten++;
        if (record.Foreground.Status != "observed") _unknown++;
    }
    internal void MarkFailure(Exception error)
    {
        if (Frozen) return;
        _errors++;
        if (_firstError != null) return;
        string message = error.Message;
        _firstError = message.Length <= 1024 ? message : message[..1024];
    }
    internal void Freeze(string reason)
    {
        if (Frozen) return;
        Frozen = true; _reason = reason;
    }
    internal string CreateJson(string outcome)
    {
        var rows = new object[_count];
        int start = _count == Capacity ? _next : 0;
        for (int index = 0; index < _count; index++)
        {
            KeyboardFocusTraceRecord row = _rows[(start + index) % Capacity];
            KeyboardForegroundSnapshot foreground = row.Foreground;
            bool known = foreground.Status == "observed";
            rows[index] = new
            {
                ordinal = _total - (ulong)_count + (ulong)index + 1, phase = row.Phase,
                process_frame = row.ProcessFrame, physics_frame = row.PhysicsFrame,
                observation_usec = row.Usec, window_focused = row.WindowFocused,
                own_hwnd = row.OwnWindow, own_process_id = row.OwnProcessId,
                foreground_status = foreground.Status, foreground_hwnd_before = foreground.BeforeWindow,
                foreground_hwnd_after = foreground.AfterWindow, foreground_process_id = foreground.ProcessId,
                foreground_thread_id = foreground.ThreadId, win32_error = foreground.Win32Error,
                foreground_owner_known = known,
                foreground_is_own_window = known && row.OwnWindow != 0 && foreground.BeforeWindow == row.OwnWindow,
                foreground_is_own_process = known && foreground.ProcessId == row.OwnProcessId
            };
        }
        return JsonSerializer.Serialize(new
        {
            version = 1, outcome, diagnostic_only = true, capacity = Capacity,
            frozen = Frozen, freeze_reason = _reason, retained_count = _count,
            total_recorded = _total, overwritten_count = _overwritten, unknown_observations = _unknown,
            observer_failures = _errors, first_observer_error = _firstError,
            full_history_retained = _overwritten == 0, complete_route_evidence = false,
            deactivation_cause_proven = false, records = rows,
            limits = new
            {
                scope = "event_only_ready_application_window_stop_no_polling_no_focus_requests_no_input_writes",
                clock = "Godot_observation_usec_not_OS_event_timestamp",
                native = "two_foreground_reads_and_PID_query_are_sequential_not_atomic_ABA_and_destroy_reuse_possible",
                origin = "foreground_owner_at_observation_is_not_operator_or_deactivation_cause",
                gaps = "no_continuous_foreground_history_delayed_events_and_hard_exit_may_miss_transitions",
                cost = "native_queries_and_observer_lifecycle_have_unmeasured_diagnostic_overhead"
            }
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}

// Test-only numeric retention. Native observations and IO belong to adapters.
internal sealed class KeyboardInputTraceBuffer
{
    private const int Capacity = 256;
    private readonly KeyboardInputTraceRecord[] _rows = new KeyboardInputTraceRecord[Capacity];
    private readonly ulong[] _ordinals = new ulong[Capacity];
    private int _count, _next, _failures;
    private ulong _total, _overwritten;
    private string _reason, _error;
    internal bool Frozen { get; private set; }
    internal void Record(in KeyboardInputTraceRecord record)
    {
        if (Frozen) return;
        bool match = false;
        if (record.Phase == "received" && record.EventId != 0)
            for (int index = 0; index < _count; index++)
                if (_rows[index].Phase == "before_parse" && _rows[index].EventId == record.EventId)
                { match = true; break; }
        _rows[_next] = record with { KnownInjectedId = match };
        _ordinals[_next] = ++_total;
        _next = (_next + 1) % Capacity;
        if (_count < Capacity) _count++; else _overwritten++;
    }
    internal void Freeze(string reason)
    {
        if (Frozen) return;
        Frozen = true; _reason = reason;
    }
    internal void MarkFailure(Exception error)
    {
        if (Frozen) return;
        _failures++;
        string text = error.ToString();
        _error = text.Length <= 2048 ? text : text[..2048];
        Freeze("observer_error");
    }
    internal string CreateJson(string outcome)
    {
        var rows = new object[_count];
        int start = _count == Capacity ? _next : 0;
        for (int index = 0; index < _count; index++)
        {
            int slot = (start + index) % Capacity;
            KeyboardInputTraceRecord r = _rows[slot];
            rows[index] = new
            {
                ordinal = _ordinals[slot], phase = r.Phase, key = r.Key.ToString(), pressed = r.Pressed,
                echo = r.Echo, event_id = r.EventId, process_frame = r.ProcessFrame, physics_frame = r.PhysicsFrame,
                observation_usec = r.Usec, reader_sample_sequence = r.SampleSequence, focused = r.Focused,
                raw_physical_down = r.RawDown, raw_mask = r.RawMask, owned_mask = r.OwnedMask,
                known_injected_id_match = r.KnownInjectedId,
                physical_keycode = r.PhysicalKeycode, logical_keycode = r.LogicalKeycode,
                event_kind = r.EventKind, event_code = r.EventCode, event_device = r.EventDevice,
                event_action = r.EventAction, raw_primary_held = r.RawPrimaryHeld,
                raw_primary_pressed = r.RawPrimaryPressed, raw_primary_released = r.RawPrimaryReleased,
                raw_mouse_left_down = r.RawMouseLeftDown, sampled_keyboard_actions = r.SampledKeyboardActions,
                sampled_primary_held = r.SampledPrimaryHeld, sampled_primary_pressed = r.SampledPrimaryPressed
            };
        }
        return JsonSerializer.Serialize(new
        {
            version = 1, event_context_version = 2, outcome, diagnostic_only = true, capacity = Capacity,
            frozen = Frozen, freeze_reason = _reason, retained_count = _count, total_recorded = _total,
            overwritten_count = _overwritten, full_history_retained = _overwritten == 0,
            observer_failures = _failures, observer_error = _error, os_origin_proven = false,
            complete_route_evidence = false, records = rows,
            limits = new
            {
                key_masks = "A,D,W,S,Space,Shift,J,K,L,Q,1,2,3,4,Escape,Enter in bit order",
                identity = "known_injected_id_match_only_Godot_may_copy_events_unmatched_is_not_hardware",
                observer_order = "passive_Input_may_miss_handled_events_not_reader_execution_order",
                clocks = "observation_time_not_OS_event_timestamp",
                sample_sequence = "last_production_sample_not_current_raw_key_proof",
                masks = "sequential_reads_not_atomic_owned_updated_before_before_parse",
                key = "key_label_falls_back_to_logical_when_physical_code_zero_raw_query_is_still_physical",
                sources = "passive_key_mouse_button_joypad_button_selected_legacy_action_events_only_ActionPress_has_no_Input_event",
                primary = "raw_action_and_mouse_reads_are_sequential_last_sample_fields_are_not_event_consumption_proof",
                focus = "application_notifications_and_window_signals_are_distinct_observations_synthetic_notifications_possible_focused_is_window_query_not_application_state_no_native_reset_call_proven",
                overhead = "test_only_bounded_event_observation_may_affect_timing_unmeasured"
            }
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}
