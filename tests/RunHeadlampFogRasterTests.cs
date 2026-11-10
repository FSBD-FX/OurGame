using System;
using Godot;
using Knight.Tests.Presentation;

namespace Knight.Tests;

/// <summary>Default is display-backed GPU acceptance. The explicit storage-only probe makes no raster claim.</summary>
public partial class RunHeadlampFogRasterTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length == 1 && args[0] == "--fog-baseline-read-only")
            {
                await HeadlampFogRasterTests.RunNormalShaderComparison(this, baselineReadOnly: true);
                GD.Print("FOG_BASELINE_READ_ONLY_OK");
                Quit(0);
                return;
            }
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
