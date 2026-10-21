using System;
using System.IO;
using System.Text.Json;
using Godot;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

internal static class KeyboardInputTraceSidecarTests
{
    internal static void Run()
    {
        // Wrapper injects this process's TEMP/TMP beneath its new evidence case.
        // Keep all test files as evidence; do not delete or fall back to user://.
        string root=Path.Combine(Path.GetTempPath(),"keyboard-trace-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var trace=new KeyboardInputTraceBuffer(); trace.Freeze("owned_key_withdrawn");
        string path=Path.Combine(root,KeyboardInputTraceSidecar.FileName);
        Exception first=null;
        try { KeyboardInputTraceSidecar.Write(true,root,trace,"failed"); }
        catch(Exception error) { first=error; }
        TestAssert.True(first==null && File.Exists(path), "frozen keyboard trace writes to existing accepted isolated storage");
        string original=File.ReadAllText(path);
        TestAssert.Equal(trace.CreateJson("failed"),original,"disk preserves complete serialized frozen trace");
        using(JsonDocument json=JsonDocument.Parse(original))
            TestAssert.True(!json.RootElement.GetProperty("complete_route_evidence").GetBoolean(),"sidecar cannot certify route completion");
        TestAssert.True(Throws<IOException>(()=>KeyboardInputTraceSidecar.Write(true,root,trace,"changed")) &&
            original==File.ReadAllText(path),"CreateNew refuses overwrite and preserves prior bytes");
        string missing=Path.Combine(root,"must-not-create");
        TestAssert.True(Throws<InvalidOperationException>(()=>KeyboardInputTraceSidecar.Write(false,missing,trace,"failed")) &&
            !Directory.Exists(missing),"unaccepted storage causes no write or creation");
        TestAssert.True(Throws<IOException>(()=>KeyboardInputTraceSidecar.Write(true,missing,trace,"failed")) &&
            !Directory.Exists(missing),"accepted missing storage is not silently created");
        string unfrozenRoot=Path.Combine(root,"unfrozen"); Directory.CreateDirectory(unfrozenRoot);
        TestAssert.True(Throws<InvalidOperationException>(()=>KeyboardInputTraceSidecar.Write(true,unfrozenRoot,new KeyboardInputTraceBuffer(),"failed")) &&
            !File.Exists(Path.Combine(unfrozenRoot,KeyboardInputTraceSidecar.FileName)),"unfrozen evidence is rejected before file open");
        TestAssert.True(Throws<IOException>(()=>KeyboardInputTraceSidecar.Write(true,"relative-not-accepted",trace,"failed")),
            "relative storage never resolves against arbitrary cwd");
        GD.Print("KEYBOARD_INPUT_TRACE_SIDECAR_OK cases=6");
        RunFocus(root);
    }
    private static void RunFocus(string root)
    {
        var focus=new KeyboardFocusTraceBuffer(); focus.Freeze("runner_finally");
        string path=KeyboardInputTraceSidecar.WriteFocus(true,root,focus,"failed");
        string original=File.ReadAllText(path);
        TestAssert.Equal(focus.CreateJson("failed"),original,"focus sidecar retains its own frozen scalar evidence");
        TestAssert.True(Throws<IOException>(()=>KeyboardInputTraceSidecar.WriteFocus(true,root,focus,"changed")) &&
            File.ReadAllText(path)==original,"focus CreateNew refuses overwrite independently of keyboard sidecar");
        string missing=Path.Combine(root,"focus-must-not-create");
        TestAssert.True(Throws<InvalidOperationException>(()=>KeyboardInputTraceSidecar.WriteFocus(false,missing,focus,"failed")) &&
            !Directory.Exists(missing),"focus unaccepted root is rejected without directory creation");
        TestAssert.True(Throws<IOException>(()=>KeyboardInputTraceSidecar.WriteFocus(true,missing,focus,"failed")) &&
            !Directory.Exists(missing),"focus missing accepted root is not created");
        string unfrozen=Path.Combine(root,"focus-unfrozen"); Directory.CreateDirectory(unfrozen);
        TestAssert.True(Throws<InvalidOperationException>(()=>KeyboardInputTraceSidecar.WriteFocus(true,unfrozen,new KeyboardFocusTraceBuffer(),"failed")) &&
            !File.Exists(Path.Combine(unfrozen,KeyboardInputTraceSidecar.FocusFileName)),"focus writes require separate stop before opening any file");
        TestAssert.True(Throws<IOException>(()=>KeyboardInputTraceSidecar.WriteFocus(true,"relative-focus-root",focus,"failed")),
            "focus evidence cannot resolve a relative storage root");
        GD.Print("KEYBOARD_FOCUS_TRACE_SIDECAR_OK cases=6");
    }
    private static bool Throws<T>(Action action) where T:Exception
    {
        try { action(); return false; } catch(T) { return true; }
    }
}
