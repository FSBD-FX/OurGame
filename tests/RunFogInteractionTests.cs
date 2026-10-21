using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunFogInteractionTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            TestAssert.True(DisplayServer.GetName() != "headless", "Q cue proof requires actual rendered pixels");
            TestAssert.Equal("gl_compatibility", ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                "Q cue proof uses the current Compatibility renderer");
            string adapter = RenderingServer.GetVideoAdapterName();
            TestAssert.True(adapter.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
                $"Q cue proof requires this machine's hardware NVIDIA adapter: {adapter}");
            await FogInteractionRuntimeTests.Run(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("fog-interaction");
            GD.Print("FOG_INTERACTION_OK");
            Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); Quit(1); }
    }
}
