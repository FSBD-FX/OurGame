using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunTask8Fix2GripStoneRed : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await PrologueCanyonTraversalTests.AssertGripStoneLegacyMarginPhysicalRed(this);
            GD.Print("TASK8_FIX3_GRIPSTONE_MARGIN_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
