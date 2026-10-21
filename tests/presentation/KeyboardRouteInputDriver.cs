using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Shared;
using Knight.Player;

namespace Knight.Tests.Presentation;

// Test-only route planner adapter. Production input/aim/model state is never written.
public sealed class KeyboardRouteInputDriver : IDisposable
{
    public static KeyboardRouteInputDriver Current { get; private set; }
    internal KeyboardInputTraceBuffer Trace { get; } = new();
    public static KeyboardRouteInputDriver Begin(CharacterBody2D player)
    {
        TestAssert.True(Current == null, "one keyboard route owns input at a time");
        return Current = new KeyboardRouteInputDriver(player);
    }
    private readonly CharacterBody2D _player;
    private readonly HashSet<Key> _directions = new();
    private readonly HashSet<Key> _held = new();
    private KeyboardRouteInputObserver _observer;
    private bool _observerAttempted;
    internal static readonly Key[] TraceKeys =
        [Key.A,Key.D,Key.W,Key.S,Key.Space,Key.Shift,Key.J,Key.K,Key.L,Key.Q,
         Key.Key1,Key.Key2,Key.Key3,Key.Key4,Key.Escape,Key.Enter];
    private bool _clawIntent, _shieldHeld;
    private Key? _primaryKey;
    private Vector2? _aimPoint;
    public int KeyDownCount { get; private set; }
    public int AttackEdges { get; private set; }
    public int ClawEdges { get; private set; }
    public int ShieldEdges { get; private set; }
    public int ObservedFrames { get; private set; }
    public ulong ObservedMouseMotionSamples { get; private set; }
    private KeyboardRouteInputDriver(CharacterBody2D player) { _player=player; }
    private bool Attached => _player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State is
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;

    public void Command(StringName action, bool pressed)
    {
        // Old strategy names are only planner intent, never emitted input events.
        if(action==InputActions.SelectAttackMode || action==InputActions.SelectGrappleMode)
        {
            if(pressed) _clawIntent=action==InputActions.SelectGrappleMode;
            if(action==InputActions.SelectAttackMode && (Attached || _held.Contains(Key.K)))
                Emit(Key.K,pressed);
            return;
        }
        if(action==InputActions.PrimaryAction)
        {
            if(pressed)
            {
                _primaryKey=_clawIntent?Key.K:Key.J;
                RefreshDirections();
                Emit(_primaryKey.Value,true);
            }
            else
            {
                if(_primaryKey.HasValue) Emit(_primaryKey.Value,false);
                _primaryKey=null;
                RefreshDirections();
            }
            return;
        }
        Key key = KeyFor(action);
        if(key is Key.A or Key.D or Key.W or Key.S)
        {
            if(pressed) _directions.Add(key); else _directions.Remove(key);
            RefreshDirections();
        }
        else Emit(key,pressed);
    }

    public void AimAt(CharacterBody2D player, Vector2 worldPoint)
    {
        TestAssert.True(player==_player, "keyboard route aim belongs to the persistent player");
        _aimPoint=worldPoint;
        // Looking does not move a keyboard character. Direction is pressed briefly
        // with K/L and the real production resolver/guard still decides the result.
    }

    public void PointerButton(MouseButton button, bool pressed, CharacterBody2D player, Vector2 point)
    {
        if(button==MouseButton.Left) { AimAt(player,point); Command(InputActions.PrimaryAction,pressed); return; }
        TestAssert.True(button==MouseButton.Right, "keyboard route only translates known strategy intents");
        if(pressed) AimAt(player,point);
        _shieldHeld=pressed;
        RefreshDirections();
        Emit(Key.L,pressed);
    }

    private void RefreshDirections()
    {
        var wanted=new HashSet<Key>(_directions);
        if(_aimPoint.HasValue && (_shieldHeld || (_primaryKey==Key.K && !Attached)))
        {
            Vector2 delta=_aimPoint.Value-_player.GlobalPosition;
            int octant=(int)Mathf.Round(delta.Angle()/(Mathf.Pi/4f));
            Vector2 direction=Vector2.Right.Rotated(octant*Mathf.Pi/4f);
            wanted.Clear();
            bool neutralHorizontal = Mathf.Abs(direction.Y)<.5f &&
                !_directions.Contains(Key.A) && !_directions.Contains(Key.D) &&
                _player.GetNode<PlayerInputReader>("Input").KeyboardAimDirection.Dot(direction)>.99f;
            if(!neutralHorizontal && direction.X>.5f) wanted.Add(Key.D);
            if(!neutralHorizontal && direction.X<-.5f) wanted.Add(Key.A);
            if(direction.Y>.5f) wanted.Add(Key.S);
            if(direction.Y<-.5f) wanted.Add(Key.W);
        }
        foreach(Key key in new[]{Key.A,Key.D,Key.W,Key.S}) Emit(key,wanted.Contains(key));
    }

    private void Emit(Key key,bool pressed)
    {
        if(_held.Contains(key)==pressed) return;
        if(pressed)
        {
            _held.Add(key); KeyDownCount++;
            if(key==Key.J) AttackEdges++;
            if(key==Key.K) ClawEdges++;
            if(key==Key.L) ShieldEdges++;
        }
        else _held.Remove(key);
        EnsureObserver();
        var keyEvent = new InputEventKey { PhysicalKeycode=key, Keycode=key, Pressed=pressed };
        RecordTrace("before_parse",key,keyEvent);
        Input.ParseInputEvent(keyEvent);
        RecordTrace("after_parse",key,keyEvent);
        Input.FlushBufferedEvents();
        RecordTrace("after_flush",key,keyEvent);
    }

    private void EnsureObserver()
    {
        // Begin is also used before the authored level enters the tree.
        if (_observerAttempted || Trace.Frozen) return;
        try
        {
            if (!GodotObject.IsInstanceValid(_player)) throw new ObjectDisposedException("keyboard trace player");
            if (!_player.IsInsideTree()) return;
            _observerAttempted = true;
            SceneTree tree = _player.GetTree();
            if (tree is IKeyboardInputTraceSink sink) sink.RetainKeyboardInputTrace(Trace);
            _observer = new KeyboardRouteInputObserver { Name="KeyboardRouteInputObserver", Driver=this };
            tree.Root.AddChild(_observer);
        }
        catch (Exception error) { Trace.MarkFailure(error); }
    }

    internal void RecordTrace(string phase,Key key=Key.None,InputEvent inputEvent=null)
    {
        if (Trace.Frozen) return;
        try
        {
            if (!GodotObject.IsInstanceValid(_player) || !_player.IsInsideTree())
                throw new ObjectDisposedException("keyboard trace player is no longer in the tree");
            ulong rawMask=0,ownedMask=0;
            for(int index=0;index<TraceKeys.Length;index++)
            {
                if(Input.IsPhysicalKeyPressed(TraceKeys[index])) rawMask|=1UL<<index;
                if(_held.Contains(TraceKeys[index])) ownedMask|=1UL<<index;
            }
            var input=_player.GetNodeOrNull<PlayerInputReader>("Input");
            var keyEvent=inputEvent as InputEventKey;
            string kind=inputEvent switch
            {
                InputEventKey => "key", InputEventMouseButton => "mouse_button",
                InputEventJoypadButton => "joypad_button", InputEventAction => "action", _ => null
            };
            int code=inputEvent switch
            {
                InputEventMouseButton button => (int)button.ButtonIndex,
                InputEventJoypadButton button => (int)button.ButtonIndex, _ => 0
            };
            Trace.Record(new KeyboardInputTraceRecord(phase,key,inputEvent?.IsPressed() ?? false,
                inputEvent?.IsEcho() ?? false,inputEvent?.GetInstanceId() ?? 0,
                Engine.GetProcessFrames(),Engine.GetPhysicsFrames(),Time.GetTicksUsec(),
                input?.CurrentFrameSequence ?? 0,_player.GetWindow().HasFocus(),
                key!=Key.None && Input.IsPhysicalKeyPressed(key),rawMask,ownedMask,
                PhysicalKeycode:(int)(keyEvent?.PhysicalKeycode ?? Key.None), LogicalKeycode:(int)(keyEvent?.Keycode ?? Key.None),
                EventKind:kind,EventCode:code,EventDevice:inputEvent?.Device ?? 0,
                EventAction:(inputEvent as InputEventAction)?.Action.ToString(),
                RawPrimaryHeld:Input.IsActionPressed(InputActions.PrimaryAction),
                RawPrimaryPressed:Input.IsActionJustPressed(InputActions.PrimaryAction),
                RawPrimaryReleased:Input.IsActionJustReleased(InputActions.PrimaryAction),
                RawMouseLeftDown:Input.IsMouseButtonPressed(MouseButton.Left),
                SampledKeyboardActions:input?.Current.KeyboardActions ?? false,
                SampledPrimaryHeld:input?.Current.PrimaryHeld ?? false,
                SampledPrimaryPressed:input?.Current.PrimaryPressed ?? false));
        }
        catch (Exception error) { Trace.MarkFailure(error); }
    }

    private static Key KeyFor(StringName action)
    {
        if(action==InputActions.MoveLeft) return Key.A;
        if(action==InputActions.MoveRight) return Key.D;
        if(action==InputActions.MoveUp) return Key.W;
        if(action==InputActions.MoveDown) return Key.S;
        if(action==InputActions.Jump) return Key.Space;
        if(action==InputActions.Dash) return Key.Shift;
        if(action==InputActions.Attack) return Key.J;
        if(action==InputActions.Claw) return Key.K;
        if(action==InputActions.Interact) return Key.Q;
        if(action==InputActions.KeyboardShield || action==InputActions.Shield) return Key.L;
        if(action==InputActions.Slot1) return Key.Key1;
        if(action==InputActions.Slot2) return Key.Key2;
        if(action==InputActions.Slot3) return Key.Key3;
        if(action==InputActions.Slot4) return Key.Key4;
        if(action==InputActions.Pause) return Key.Escape;
        if(action=="ui_accept") return Key.Enter;
        throw new InvalidOperationException($"Unsupported keyboard route intent {action}");
    }

    public void ReleaseAll()
    {
        foreach(Key key in new List<Key>(_held)) Emit(key,false);
        _directions.Clear(); _primaryKey=null; _shieldHeld=false; _aimPoint=null;
    }
    public void ObserveFrame()
    {
        EnsureObserver();
        var input=_player.GetNode<PlayerInputReader>("Input");
        // A native focus/input reset can withdraw keys without changing this
        // adapter's hold intent. Report that boundary; never silently replay it.
        foreach(Key key in _held)
            if(!Input.IsPhysicalKeyPressed(key))
            {
                RecordTrace("owned_guard",key);
                Trace.Freeze("owned_key_withdrawn");
                throw new InvalidOperationException($"keyboard route owned key was withdrawn key={key} " +
                    $"physics={Engine.GetPhysicsFrames()} sample={input.CurrentFrameSequence} " +
                    $"focused={_player.GetWindow().HasFocus()} owned={string.Join(",",_held)} " +
                    $"sampled_move={input.Current.MoveAxis} sampled_aim={input.Current.Aim}");
            }
        ObservedMouseMotionSamples = Math.Max(ObservedMouseMotionSamples, input.MouseMotionSequence);
        // A native window may deliver pointer motion on creation/focus. Merely
        // observing that coordinate is not using it: keyboard aim remains its
        // authority. Actual legacy action consumption is still rejected.
        // StartNew/recovery deliberately clears the entire sampled frame before
        // the next physics sample. This empty frame consumes no input at all.
        bool emptySample = input.Current.Equals(default(PlayerInputFrame));
        bool keyboardOnly=(emptySample || input.Current.KeyboardActions) && !input.Current.PrimaryHeld &&
            !input.Current.PrimaryPressed && !input.Current.AttackModePressed &&
            !input.Current.GrappleModePressed && !input.Current.LegacyShieldHeld;
        if(!keyboardOnly)
        {
            RecordTrace("legacy_guard");
            Trace.Freeze("legacy_input_consumed");
        }
        TestAssert.True((emptySample || input.Current.KeyboardActions) && !input.Current.PrimaryHeld &&
            !input.Current.PrimaryPressed && !input.Current.AttackModePressed &&
            !input.Current.GrappleModePressed && !input.Current.LegacyShieldHeld,
            $"keyboard normal route cannot consume mouse or legacy mode/primary input; sequence={input.CurrentFrameSequence} motion={input.MouseMotionSequence} empty={input.Current.Equals(default(PlayerInputFrame))} frame={input.Current}");
        ObservedFrames++;
    }
    public void AssertComplete()
    {
        ObserveFrame();
        TestAssert.True(AttackEdges>0 && ClawEdges>0 && ShieldEdges>0 && ObservedFrames>100,
            "keyboard normal route proves real J K L edges and continued source observation");
        GD.Print($"KEYBOARD_NORMAL_ROUTE_AMBIENT_MOTION observed_samples={ObservedMouseMotionSamples} generated_mouse_events=0 keyboard_action_authority=true");
        // Existing version1 mouse_events is the driver's emitted event count,
        // not a claim that the operating system delivered no pointer motion.
        GD.Print($"KEYBOARD_NORMAL_ROUTE_INPUT_OK version=1 physical_keys=true mouse_events=0 " +
            $"legacy_action_events=0 key_downs={KeyDownCount} J={AttackEdges} K={ClawEdges} L={ShieldEdges} samples={ObservedFrames}");
    }
    public void Dispose()
    {
        try { ReleaseAll(); }
        finally
        {
            Trace.Freeze("driver_dispose");
            _observer?.Stop();
            Current = null;
        }
    }
}
