using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinEnvironmentVisualTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            BasinEnvironmentVisualTests.Run();
            GD.Print("BASIN_ENVIRONMENT_VISUAL_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
