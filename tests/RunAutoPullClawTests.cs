using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunAutoPullClawTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await MechanicalClawResidencyContractTests.Run(this);
            MechanicalClawArrivalGeometryTests.Run();
            MechanicalClawModelTests.Run();
            await PlayerActionModeRuntimeTests.RunAutoPullClawTask3(this);
            await PlayerTraversalInputTests.RunAutoPullClawTask3(this);
            await MechanicalClawAutoPullRuntimeTests.Run(this);
            await MechanicalClawRuntimeTests.AttachedOcclusionReleasesBeforePullingThroughTheBlocker(this);
            GD.Print("AUTO_PULL_CLAW_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
