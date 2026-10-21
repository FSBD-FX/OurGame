using System;
using System.Linq;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public enum BasinCounterweightPhase { Contract, Ride, Transfer, Exits, Lifecycle, Neighbors, NeighborLifecycle, Steam, Fog, AttachedExits, Regressions, Presentation, Stills, BoardingTrace, BoardingCadence, RouteContract, RouteTransfer, RouteEncounter, RoutePhysicsCadence }

public partial class RunBasinCounterweightTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length == 2 &&
                args.Count(a => a.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                args.Count(a => a.StartsWith("--basin-phase=", StringComparison.Ordinal)) == 1,
                "Basin runner needs exactly one phase and one external storage root");
            string token = args.Single(a => a.StartsWith("--basin-phase=", StringComparison.Ordinal))[14..];
            TestAssert.True(Enum.TryParse(token, false, out BasinCounterweightPhase phase) &&
                Enum.IsDefined(phase) && phase.ToString() == token, "Basin runner rejects unknown/numeric phases");
            IsolatedPrologueFixture.ValidateStorageRoot();
            if(phase is BasinCounterweightPhase.Presentation or BasinCounterweightPhase.Stills or BasinCounterweightPhase.BoardingTrace or BasinCounterweightPhase.BoardingCadence)
            {
                TestAssert.Equal("Windows",DisplayServer.GetName(),"proof needs actual hardware window");
                string method=RenderingServer.GetCurrentRenderingMethod();
                string driver=RenderingServer.GetCurrentRenderingDriverName();
                string adapter=RenderingServer.GetVideoAdapterName();
                TestAssert.True(phase==BasinCounterweightPhase.Presentation ? method=="mobile" && driver=="vulkan" : method=="gl_compatibility",
                    $"proof renderer mismatch: {method}/{driver}");
                TestAssert.True(!string.IsNullOrWhiteSpace(adapter),"proof needs named hardware adapter");
                foreach(string software in new[]{"llvmpipe","lavapipe","swiftshader","Microsoft Basic Render","softpipe"})
                    TestAssert.True(!adapter.Contains(software,StringComparison.OrdinalIgnoreCase),"software rendering cannot certify proof");
                DisplayServer.WindowSetSize(new Vector2I(1280,720));Root.Size=new Vector2I(1280,720);
                GD.Print($"BASIN_PROOF_CONTEXT method={method} driver={driver} adapter={adapter} fixed60={phase==BasinCounterweightPhase.Presentation} normal_performance=false human=false controlled_initial_spawn=true");
            }
            if (phase == BasinCounterweightPhase.Fog)
            {
                TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
                    "BasinFogRequiresRenderedDisplay: GPU pixel tests cannot run headless");
                string method = RenderingServer.GetCurrentRenderingMethod();
                string driver = RenderingServer.GetCurrentRenderingDriverName();
                TestAssert.True(method == "gl_compatibility",
                    $"BasinFogRequiresCompatibilityRenderer: actual={method} driver={driver}");
                TestAssert.Equal("gl_compatibility",
                    ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
                    "Basin fog regression uses the actual Compatibility renderer");
                string adapter = RenderingServer.GetVideoAdapterName();
                TestAssert.True(!string.IsNullOrWhiteSpace(adapter), "Basin fog requires a reported hardware adapter");
                foreach (string software in new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" })
                    TestAssert.True(!adapter.Contains(software, StringComparison.OrdinalIgnoreCase),
                        $"software GPU cannot certify Basin fog pixels: {adapter}");
                GD.Print($"BASIN_FOG_RENDERER display={DisplayServer.GetName()} method={method} driver={driver} adapter={adapter}");
            }
            await BasinCounterweightRuntimeTests.Run(this, phase);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("basin-counterweight-" + phase);
            GD.Print($"BASIN_COUNTERWEIGHT_PHASE_OK phase={phase}");
            Quit(0);
        }
        catch (Exception error)
        {
            Exception cleanupFailure = null;
            try
            {
                Paused = false;
                await ToSignal(this, SignalName.ProcessFrame);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("basin-counterweight-failure");
            }
            catch (Exception cleanupError) { cleanupFailure = cleanupError; }
            GD.PushError(error.ToString());
            if (cleanupFailure != null) GD.PushError($"Failure-path cleanup also failed: {cleanupFailure}");
            Quit(1);
        }
        finally { Paused = false; }
    }
}
