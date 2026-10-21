using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinLaborerVisualTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await BasinLaborerVisualTests.Run(this);
            GD.Print("BASIN_LABORER_VISUAL_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
