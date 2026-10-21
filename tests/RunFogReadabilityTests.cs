using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

/// <summary>Hardware-rendered isolated protected-cue pixels; saved fog layers are explicitly isolated.</summary>
public partial class RunFogReadabilityTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
                "fog readability pixels require a normal display, not headless");
            TestAssert.Equal("gl_compatibility",
                ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                "fog readability uses the actual Compatibility renderer");
            string adapter = RenderingServer.GetVideoAdapterName();
            TestAssert.True(!string.IsNullOrWhiteSpace(adapter), "hardware GPU adapter must be reported");
            foreach (string software in new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" })
                TestAssert.True(!adapter.Contains(software, StringComparison.OrdinalIgnoreCase),
                    $"software GPU cannot certify fog readability pixels: {adapter}");
            TestAssert.True(adapter.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
                $"this hardware acceptance uses the actual NVIDIA adapter, got {adapter}");
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            await FogFragmentReadabilityRuntimeTests.RunSaveFailure(this);
            await FogBarrierReadabilityRuntimeTests.Run(this);
            await FogMovingGeometryReadabilityRuntimeTests.Run(this);
            await FogPressureGateReadabilityRuntimeTests.Run(this);
            await FogReadabilityRuntimeTests.Run(this);
            await FogMothReadabilityRuntimeTests.Run(this);
            await FogClawReadabilityRuntimeTests.Run(this);
            await FogFragmentReadabilityRuntimeTests.Run(this);
            await FogTerrainReadabilityRuntimeTests.Run(this);
            await FogSteamReadabilityRuntimeTests.Run(this);
            await FogShieldLaneReadabilityRuntimeTests.Run(this);
            GD.Print("FOG_READABILITY_ATTACK_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
