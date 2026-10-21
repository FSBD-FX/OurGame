using System;
using System.Linq;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinSampleBoundaryTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            string selected = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--case="))?[7..] ?? "all";
            if (selected == "compat")
            {
                await PrologueGameFlowTests.Run(this);
                await BasinQualitySampleEntryTests.RunProductionLifecycle(this);
                await BasinQualitySampleEntryTests.RunSaveIsolationAndLocalization(this);
                await BasinQualitySampleEntryTests.DefaultMainStillStartsAtEntrance(this);
            }
            else
            {
                if (selected is not ("all" or "exit" or "fall" or "continue" or "edges" or "persistence"))
                    throw new ArgumentException("Unknown boundary test case: " + selected);
                await BasinSampleBoundaryTests.Run(this, selected);
            }
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("basin-sample-boundary");
            GD.Print("BASIN_SAMPLE_BOUNDARY_TESTS_OK case=" + selected);
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
