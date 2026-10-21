using System;
using System.Runtime.InteropServices;
using Godot;
using Knight.Shared;

namespace Knight.Tests.Presentation;

internal interface IKeyboardInputTraceSink
{
    void RetainKeyboardInputTrace(KeyboardInputTraceBuffer trace);
}

// Own-game passive observer only: no process/physics callback, handling or replay.
internal partial class KeyboardRouteInputObserver : Node
{
    internal KeyboardRouteInputDriver Driver;
    private Window _window;
    private bool _stopped;

    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        SetProcess(false); SetPhysicsProcess(false);
        _window=GetWindow();
        _window.FocusEntered+=OnFocusEntered;
        _window.FocusExited+=OnFocusExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (_stopped) return;
        if(@event is InputEventKey keyEvent)
        {
            Key key=keyEvent.PhysicalKeycode!=Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
            if(Array.IndexOf(KeyboardRouteInputDriver.TraceKeys,key)>=0) Driver.RecordTrace("received",key,keyEvent);
        }
        else if(@event is InputEventMouseButton or InputEventJoypadButton)
            Driver.RecordTrace("received",inputEvent:@event);
        else if(@event is InputEventAction action && (action.Action==InputActions.PrimaryAction ||
            action.Action==InputActions.SelectAttackMode || action.Action==InputActions.SelectGrappleMode ||
            action.Action==InputActions.Shield))
            Driver.RecordTrace("received",inputEvent:@event);
    }

    internal void OnFocusEntered() => Driver.RecordTrace("focus_entered");
    internal void OnFocusExited() => Driver.RecordTrace("focus_exited");

    public override void _Notification(int what)
    {
        if (_stopped || Driver==null) return;
        // Application notifications and window signals have different native
        // boundaries. Record the notification, not an inferred OS state/cause.
        if (what==NotificationApplicationFocusOut) Driver.RecordTrace("application_focus_out");
        else if (what==NotificationApplicationFocusIn) Driver.RecordTrace("application_focus_in");
    }

    internal void Stop()
    {
        if (_stopped) return;
        _stopped=true;
        try
        {
            if (GodotObject.IsInstanceValid(_window))
            {
                _window.FocusEntered-=OnFocusEntered;
                _window.FocusExited-=OnFocusExited;
            }
            if (GodotObject.IsInstanceValid(this))
            {
                SetProcessInput(false);
                QueueFree();
            }
        }
        catch (Exception error) { Driver.Trace.MarkFailure(error); }
    }

    public override void _ExitTree() => Stop();
}

// Test diagnostic only. Borrowed HWNDs are scalars, never owned/disposed handles.
internal static class KeyboardForegroundProbe
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    internal static KeyboardForegroundSnapshot Classify(long before,long after,uint processId,uint threadId,int error)
    {
        string status = before != after ? "raced" : before == 0 ? "no_foreground" :
            threadId == 0 || processId == 0 ? "query_failed" : "observed";
        return new KeyboardForegroundSnapshot(status,before,after,processId,threadId,error);
    }
    internal static KeyboardForegroundSnapshot Capture()
    {
        if (!OperatingSystem.IsWindows()) return new("unsupported",0,0,0,0);
        try
        {
            IntPtr before = GetForegroundWindow();
            uint processId=0, threadId=0; int error=0;
            if (before != IntPtr.Zero)
            {
                Marshal.SetLastPInvokeError(0);
                threadId = GetWindowThreadProcessId(before,out processId);
                if (threadId == 0) { error = Marshal.GetLastPInvokeError(); processId = 0; }
            }
            IntPtr after = GetForegroundWindow();
            return Classify(before.ToInt64(),after.ToInt64(),processId,threadId,error);
        }
        catch (Exception error) { return new("api_unavailable",0,0,0,0,error.HResult); }
    }
}

// Root/runner-owned, event-only: survives player/driver cleanup and old trace freeze.
internal partial class KeyboardRouteFocusObserver : Node
{
    internal KeyboardFocusTraceBuffer Trace;
    private Window _window;
    private long _ownWindow;
    private readonly int _ownProcessId=System.Environment.ProcessId;
    private bool _stopped, _enteredHook, _exitedHook;

    public override void _Ready()
    {
        ProcessMode=ProcessModeEnum.Always;
        SetProcess(false); SetPhysicsProcess(false); SetProcessInput(false);
        try
        {
            _window=GetWindow();
            if (OperatingSystem.IsWindows() && DisplayServer.GetName() != "headless")
                _ownWindow=unchecked((long)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle,_window.GetWindowId()));
            _window.FocusEntered+=OnFocusEntered; _enteredHook=true;
            _window.FocusExited+=OnFocusExited; _exitedHook=true;
        }
        catch (Exception error) { Trace.MarkFailure(error); }
        RecordBoundary("observer_ready");
    }
    internal void RecordBoundary(string phase)
    {
        if (_stopped || Trace == null || Trace.Frozen) return;
        try
        {
            bool? focused=GodotObject.IsInstanceValid(_window) ? _window.HasFocus() : null;
            Trace.Record(new KeyboardFocusTraceRecord(phase,Engine.GetProcessFrames(),Engine.GetPhysicsFrames(),
                Time.GetTicksUsec(),focused,_ownWindow,_ownProcessId,KeyboardForegroundProbe.Capture()));
        }
        catch (Exception error) { Trace.MarkFailure(error); }
    }
    internal void OnFocusEntered() => RecordBoundary("window_focus_entered");
    internal void OnFocusExited() => RecordBoundary("window_focus_exited");
    public override void _Notification(int what)
    {
        if (what==NotificationApplicationFocusOut) RecordBoundary("application_focus_out");
        else if (what==NotificationApplicationFocusIn) RecordBoundary("application_focus_in");
    }
    internal void Stop(string reason)
    {
        if (_stopped) return;
        RecordBoundary(reason);
        _stopped=true;
        // Independent removals preserve both diagnostics even if one native call fails.
        try { if (_enteredHook && GodotObject.IsInstanceValid(_window)) _window.FocusEntered-=OnFocusEntered; }
        catch (Exception error) { Trace.MarkFailure(error); }
        try { if (_exitedHook && GodotObject.IsInstanceValid(_window)) _window.FocusExited-=OnFocusExited; }
        catch (Exception error) { Trace.MarkFailure(error); }
        try { if (GodotObject.IsInstanceValid(this) && IsInsideTree()) QueueFree(); }
        catch (Exception error) { Trace.MarkFailure(error); }
        finally { Trace.Freeze(reason); }
    }
    public override void _ExitTree() => Stop("observer_exit_tree");
}
