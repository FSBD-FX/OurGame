using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinCombatReadoutTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await BasinCombatReadoutTests.Run(this);
            GD.Print("BASIN_COMBAT_READOUT_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
