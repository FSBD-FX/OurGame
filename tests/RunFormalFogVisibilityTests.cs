using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunFormalFogVisibilityTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
                "formal fog pixels require a normal display");
            string adapter = RenderingServer.GetVideoAdapterName();
            TestAssert.True(!string.IsNullOrWhiteSpace(adapter), "actual renderer must report its adapter");
            foreach (string software in new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" })
                TestAssert.True(!adapter.Contains(software, StringComparison.OrdinalIgnoreCase), "software raster is not GPU acceptance");
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            await FormalFogVisibilityRuntimeTests.Run(this);
            GD.Print("FORMAL_FOG_VISIBILITY_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
