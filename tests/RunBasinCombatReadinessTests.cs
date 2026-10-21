using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinCombatReadinessTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string selected = Array.Find(OS.GetCmdlineUserArgs(), arg => arg.StartsWith("--case="))?[7..];
            foreach (string scenario in selected == null ? new[] { "overheat", "depleted", "energy", "energy_held", "shield", "slot", "claw", "claw_hot" } : new[] { selected })
                await BasinCombatReadinessTests.Run(this, scenario);
            GD.Print("BASIN_COMBAT_READINESS_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
