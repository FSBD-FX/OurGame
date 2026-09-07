#if TOOLS
using System;
using Godot;

[Tool]
public partial class VerificationGcDrain : EditorPlugin
{
    public override void _ExitTree()
    {
        if (!string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal)) return;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GD.Print("VERIFICATION_EDITOR_GC_DRAIN_OK");
    }
}
#endif
