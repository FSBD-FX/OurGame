using System;
using Godot;
using Knight.Tests.Presentation;

namespace Knight.Tests;

/// <summary>Display-backed GPU-only acceptance; never the headless CPU/runtime runner.</summary>
public partial class RunHeadlampFogRasterTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
                "fog raster acceptance requires a normal display, not headless");
            TestAssert.Equal("gl_compatibility",
                ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                "fog raster acceptance uses the project's actual Compatibility renderer");
            string adapter = RenderingServer.GetVideoAdapterName();
            TestAssert.True(!string.IsNullOrWhiteSpace(adapter), "GPU adapter must be reported");
            foreach (string software in new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" })
                TestAssert.True(!adapter.Contains(software, StringComparison.OrdinalIgnoreCase),
                    $"software GPU cannot certify fog pixels: {adapter}");
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            await HeadlampFogRasterTests.Run(this);
            GD.Print("HEADLAMP_FOG_RASTER_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
