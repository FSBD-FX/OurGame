using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

// Explicit targeted suite; does not suppress or replace the separate camera pre-phase RED.
public partial class RunMechanicalClawContracts : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            MechanicalClawModelTests.Run();
            await KeyboardClawTargetTests.Run(this);
            await PlayerActionModeRuntimeTests.Run(this);
            await MechanicalClawRuntimeTests.Run(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("mechanical-claw-contracts");
            GD.Print("MECHANICAL_CLAW_CONTRACTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
