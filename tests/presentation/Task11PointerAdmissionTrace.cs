using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Knight.Abilities;
using Knight.Player;

namespace Knight.Tests.Presentation;

internal readonly record struct Task11PointerTraceRecord
{
    internal ulong Ordinal { get; init; }
    internal ulong ProcessFrame { get; init; }
    internal ulong PhysicsFrame { get; init; }
    internal ulong Usec { get; init; }
    internal string Phase { get; init; }
    internal string EventKind { get; init; }
    internal ulong EventId { get; init; }
    internal string IdentityCorrelation { get; init; }
    internal Vector2 Position { get; init; }
    internal Vector2 GlobalPosition { get; init; }
    internal Vector2 Relative { get; init; }
    internal long ButtonMask { get; init; }
    internal long Button { get; init; }
    internal bool Pressed { get; init; }
    internal Vector2 RequestedWorld { get; init; }
    internal bool HasRequestedWorld { get; init; }
    internal Vector2 PlayerLocal { get; init; }
    internal Transform2D GlobalWithCanvas { get; init; }
    internal Transform2D Canvas { get; init; }
    internal Transform2D Final { get; init; }
    internal bool ReaderAvailable { get; init; }
    internal PlayerInputFrame Frame { get; init; }
    internal ulong FrameSequence { get; init; }
    internal Vector2 LastViewport { get; init; }
    internal ulong MotionSequence { get; init; }
    internal bool HasMotion { get; init; }
    internal ulong PlayerId { get; init; }
    internal ulong ChainId { get; init; }
    internal Vector2 ChainAnchor { get; init; }
    internal bool HasNonFiniteData { get; init; }
}

/// <summary>Small pure fixed-array retention seam; no native API calls.</summary>
internal sealed class Task11PointerTraceRetention
{
    private readonly Task11PointerTraceRecord[] _delivered = new Task11PointerTraceRecord[128];
    private readonly Task11PointerTraceRecord[] _driver = new Task11PointerTraceRecord[64];
    private readonly Task11PointerTraceRecord[] _sampled = new Task11PointerTraceRecord[32];
    private int _deliveredCount, _driverCount, _sampledCount;
    private int _deliveredOmitted, _driverOmitted, _sampledOmitted, _spanOmitted, _failures;
    private int _nonFiniteRecords;
    private ulong _ordinal, _armedPhysics;
    private bool _everArmed;
    private string _lifecycle = "dormant", _freezeReason, _error;
    internal bool Armed => _lifecycle == "armed";

    internal bool Arm(ulong physics)
    {
        if (_everArmed || _lifecycle != "dormant") return false;
        _everArmed = true; _armedPhysics = physics; _lifecycle = "armed"; return true;
    }

    internal void Freeze(string reason)
    {
        if (_lifecycle == "frozen") return;
        _lifecycle = "frozen"; _freezeReason = reason;
    }

    internal void Guard(Action action)
    {
        try { action(); }
        catch (Exception exception) { MarkFailure(exception); }
    }

    internal void MarkFailure(Exception exception)
    {
        _failures++; _error ??= exception.ToString(); Freeze("observer_error");
    }

    internal void Record(string kind, Task11PointerTraceRecord record)
    {
        if (!Armed) return;
        record = record with { Ordinal = ++_ordinal, HasNonFiniteData = NonFinite(record) };
        if (record.PhysicsFrame < _armedPhysics || record.PhysicsFrame - _armedPhysics > 32)
        {
            _spanOmitted++;
            Freeze(record.PhysicsFrame < _armedPhysics ? "physics_frame_regressed" : "physics_span_expired");
            return;
        }
        switch (kind)
        {
            case "driver":
                if (_driverCount == _driver.Length) { _driverOmitted++; return; }
                _driver[_driverCount++] = record; break;
            case "delivered":
                if (_deliveredCount == _delivered.Length) { _deliveredOmitted++; return; }
                bool matched = false;
                for (int i = 0; i < _driverCount; i++)
                    if (record.EventId != 0 && _driver[i].EventId == record.EventId) { matched = true; break; }
                _delivered[_deliveredCount++] = record with
                { IdentityCorrelation = matched ? "known_injected_id_match" : "unmatched" }; break;
            case "sampled":
                if (_sampledCount == _sampled.Length) { _sampledOmitted++; return; }
                _sampled[_sampledCount++] = record; break;
            default: throw new ArgumentException("Unknown pointer trace record kind.", nameof(kind));
        }
        if (record.HasNonFiniteData) _nonFiniteRecords++;
    }

    internal string CreateJson(string outcome)
    {
        bool complete = _everArmed && _lifecycle == "frozen" && _freezeReason == "first_aim_observed" &&
            _failures == 0 && _spanOmitted == 0 && _deliveredOmitted == 0 && _driverOmitted == 0 && _sampledOmitted == 0 &&
            _nonFiniteRecords == 0;
        return JsonSerializer.Serialize(new
        {
            version = 1, outcome, lifecycle = _lifecycle, ever_armed = _everArmed, armed_physics_frame = _armedPhysics,
            freeze_reason = _freezeReason, observation_complete = complete,
            complete_route_evidence = complete && outcome == "route_completed",
            delivered_capacity = 128, driver_capacity = 64, sampled_capacity = 32, max_physics_span = 32,
            delivered_count = _deliveredCount, driver_count = _driverCount, sampled_count = _sampledCount,
            delivered_omitted = _deliveredOmitted, driver_omitted = _driverOmitted, sampled_omitted = _sampledOmitted,
            span_omitted = _spanOmitted, observer_failures = _failures, observer_error = _error,
            non_finite_records = _nonFiniteRecords,
            driver = Export(_driver, _driverCount), delivered = Export(_delivered, _deliveredCount), sampled = Export(_sampled, _sampledCount),
            limits = new
            {
                os_origin_and_timestamp = "unavailable", buffering_and_copy_history = "unavailable",
                exact_reader_event_identity_and_execution_order = "unavailable", atomic_sampling_transform = "unavailable",
                transforms = "current_observation_time_not_atomic_sampling_time",
                identity = "matching_original_instance_ID_is_opportunistic_only_Godot_may_copy_events",
                coordinates_and_phase_adjacency = "not_provenance", observer_order = "not_reader_execution_order",
                non_finite_data = "preserved_as_named_JSON_strings_record_status_non_finite_evidence_incomplete",
                handled_before_observer = "delivery_not_guaranteed", bounded_observer_overhead = "may_affect_timing_unmeasured"
            }
        }, new JsonSerializerOptions { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals });
    }

    private static object[] Export(Task11PointerTraceRecord[] records, int count)
    {
        var result = new object[count];
        for (int i = 0; i < count; i++)
        {
            Task11PointerTraceRecord r = records[i];
            result[i] = new
            {
                ordinal = r.Ordinal, process_frame = r.ProcessFrame, physics_frame = r.PhysicsFrame, observation_usec = r.Usec,
                data_status = r.HasNonFiniteData ? "non_finite" : "finite",
                phase = r.Phase, event_kind = r.EventKind, original_or_delivered_instance_id = r.EventId,
                identity_correlation = r.IdentityCorrelation, position = V(r.Position), global_position = V(r.GlobalPosition),
                relative = V(r.Relative), button_mask = r.ButtonMask, button = r.Button, pressed = r.Pressed,
                requested_world = r.HasRequestedWorld ? V(r.RequestedWorld) : null,
                player_local = r.HasRequestedWorld ? V(r.PlayerLocal) : null,
                global_transform_with_canvas = T(r.GlobalWithCanvas), canvas_transform = T(r.Canvas), final_transform = T(r.Final),
                reader_available = r.ReaderAvailable,
                current_frame = r.ReaderAvailable ? (object)new
                {
                    mouse_world = V(r.Frame.MouseWorld), move_axis = r.Frame.MoveAxis, aim = V(r.Frame.Aim),
                    primary_pressed = r.Frame.PrimaryPressed, primary_held = r.Frame.PrimaryHeld, primary_released = r.Frame.PrimaryReleased,
                    grapple_mode_pressed = r.Frame.GrappleModePressed, attack_mode_pressed = r.Frame.AttackModePressed,
                    attack_pressed = r.Frame.AttackPressed, shield_held = r.Frame.ShieldHeld,
                    jump_pressed = r.Frame.JumpPressed, jump_held = r.Frame.JumpHeld, dash_pressed = r.Frame.DashPressed,
                    thread_pressed = r.Frame.ThreadPressed, thread_held = r.Frame.ThreadHeld, thread_released = r.Frame.ThreadReleased,
                    requested_slot = r.Frame.RequestedSlot
                } : null,
                current_frame_sequence = r.FrameSequence, last_observed_mouse_viewport_position = V(r.LastViewport),
                mouse_motion_sequence = r.MotionSequence, has_observed_mouse_motion = r.HasMotion,
                player_id = r.PlayerId, chain_id = r.ChainId, chain_anchor = V(r.ChainAnchor)
            };
        }
        return result;
    }
    private static float[] V(Vector2 v) => new[] { v.X, v.Y };
    private static float[] T(Transform2D t) => new[] { t.X.X, t.X.Y, t.Y.X, t.Y.Y, t.Origin.X, t.Origin.Y };
    private static bool Finite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
    private static bool Finite(Transform2D t) => Finite(t.X) && Finite(t.Y) && Finite(t.Origin);
    private static bool NonFinite(Task11PointerTraceRecord r) =>
        !Finite(r.Position) || !Finite(r.GlobalPosition) || !Finite(r.Relative) ||
        !Finite(r.RequestedWorld) || !Finite(r.PlayerLocal) || !Finite(r.ChainAnchor) ||
        !Finite(r.GlobalWithCanvas) || !Finite(r.Canvas) || !Finite(r.Final) ||
        !Finite(r.Frame.MouseWorld) || !Finite(r.Frame.Aim) || !float.IsFinite(r.Frame.MoveAxis) || !Finite(r.LastViewport);
}

/// <summary>Runner-owned passive input observer. No process/physics callbacks, consumption or mutation.</summary>
internal partial class Task11PointerAdmissionTrace : Node
{
    internal const string RootChildName = "Task11PointerAdmissionTrace";
    internal readonly Task11PointerTraceRetention Retention = new();
    private CharacterBody2D _player;
    private PlayerInputReader _reader;
    private GrappleSurface2D _chain;

    public override void _Ready() => SetProcessInput(false);

    internal void Arm(CharacterBody2D player, PlayerInputReader reader, GrappleSurface2D chain)
    {
        Retention.Guard(() =>
        {
            if (!Retention.Arm(Engine.GetPhysicsFrames())) return;
            _player = player; _reader = reader; _chain = chain;
            SetProcessInput(true);
        });
    }

    internal void Freeze(string reason)
    {
        Retention.Freeze(reason);
        Retention.Guard(() => { if (GodotObject.IsInstanceValid(this)) SetProcessInput(false); });
    }

    internal void Driver(string phase, CharacterBody2D player, Vector2 world, InputEventMouse mouse)
    {
        if (!Retention.Armed) return;
        Retention.Guard(() => Retention.Record("driver", Capture(phase, mouse, player, world) with { HasRequestedWorld = true }));
        DisableIfFrozen();
    }

    internal void Snapshot(string phase)
    {
        if (!Retention.Armed) return;
        Retention.Guard(() => Retention.Record("sampled", Capture(phase, null, _player, _chain.WorldAnchor)));
        DisableIfFrozen();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Retention.Armed || @event is not (InputEventMouseMotion or InputEventMouseButton)) return;
        Retention.Guard(() => Retention.Record("delivered", Capture("passive_Input", (InputEventMouse)@event, _player, _chain.WorldAnchor)));
        DisableIfFrozen();
    }

    private void DisableIfFrozen()
    {
        if (!Retention.Armed) Retention.Guard(() => SetProcessInput(false));
    }

    private Task11PointerTraceRecord Capture(string phase, InputEventMouse mouse, CharacterBody2D player, Vector2 world)
    {
        // These are observations made now, never reconstructed engine sampling-time transforms.
        ulong process = Engine.GetProcessFrames(), physics = Engine.GetPhysicsFrames(), usec = Time.GetTicksUsec();
        Viewport viewport = player.GetViewport();
        bool available = GodotObject.IsInstanceValid(_reader) && _reader.IsInsideTree();
        return new Task11PointerTraceRecord
        {
            ProcessFrame = process, PhysicsFrame = physics, Usec = usec, Phase = phase,
            EventKind = mouse is InputEventMouseMotion ? "motion" : mouse is InputEventMouseButton ? "button" : "sampled",
            EventId = mouse?.GetInstanceId() ?? 0, Position = mouse?.Position ?? default, GlobalPosition = mouse?.GlobalPosition ?? default,
            Relative = mouse is InputEventMouseMotion motion ? motion.Relative : default,
            ButtonMask = mouse == null ? 0 : (long)mouse.ButtonMask,
            Button = mouse is InputEventMouseButton button ? (long)button.ButtonIndex : 0,
            Pressed = mouse is InputEventMouseButton pressed && pressed.Pressed,
            RequestedWorld = world, PlayerLocal = player.ToLocal(world), GlobalWithCanvas = player.GetGlobalTransformWithCanvas(),
            Canvas = viewport.GetCanvasTransform(), Final = viewport.GetFinalTransform(),
            ReaderAvailable = available, Frame = available ? _reader.CurrentFrame : default,
            FrameSequence = available ? _reader.CurrentFrameSequence : 0, LastViewport = available ? _reader.LastObservedMouseViewportPosition : default,
            MotionSequence = available ? _reader.MouseMotionSequence : 0, HasMotion = available && _reader.HasObservedMouseMotion,
            PlayerId = player.GetInstanceId(), ChainId = _chain.GetInstanceId(), ChainAnchor = _chain.WorldAnchor
        };
    }
}
