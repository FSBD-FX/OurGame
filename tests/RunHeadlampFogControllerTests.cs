using System;
using System.Threading.Tasks;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

/// <summary>Display-backed saved controller lifecycle with formal Playing visibility.</summary>
public partial class RunHeadlampFogControllerTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
                "fog controller FramePreDraw integration requires a normal display, not headless");
            TestAssert.Equal("gl_compatibility",
                ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                "controller integration uses the actual Compatibility renderer");
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            await Observe("barriers", () => FogBarrierSavedControllerRuntimeTests.Run(this));
            await Observe("moving", () => FogMovingGeometrySavedControllerRuntimeTests.Run(this));
            await Observe("pressure-gates", () => FogPressureGateSavedControllerRuntimeTests.Run(this));
            await Observe("shield-lane", () => FogShieldLaneSavedControllerRuntimeTests.Run(this));
            await Observe("steam", () => FogSteamSavedControllerRuntimeTests.Run(this));
            await Observe("terrain", () => FogTerrainSavedControllerRuntimeTests.Run(this));
            await Observe("lifecycle", () => HeadlampFogControllerRuntimeTests.Run(this));
            await Observe("arena", () => HeadlampFogControllerRuntimeTests.RunSavedArenaSource(this));
            GD.Print("HEADLAMP_FOG_CONTROLLER_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private async Task Observe(string phase, Func<Task> run)
    {
        ulong started = Time.GetTicksUsec();
        GD.Print($"HEADLAMP_CONTROLLER_PHASE phase={phase} boundary=start usec={started} " +
            $"process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()} visible={Root.Visible} focused={Root.HasFocus()}");
        try { await run(); }
        finally
        {
            GD.Print($"HEADLAMP_CONTROLLER_PHASE phase={phase} boundary=end elapsed_ms={(Time.GetTicksUsec()-started)/1000d:F3} " +
                $"process={Engine.GetProcessFrames()} physics={Engine.GetPhysicsFrames()} visible={Root.Visible} focused={Root.HasFocus()}");
        }
    }
}
