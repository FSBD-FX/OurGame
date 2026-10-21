using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunTask4PauseStressTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            int iterations = int.TryParse(System.Environment.GetEnvironmentVariable("TASK4_PAUSE_ITERATIONS"),
                out int parsed) ? parsed : 20;
            await PrologueCompletionTests.RunPauseEdgeStress(this, iterations);
            GD.Print($"TASK4_PAUSE_RUNNER_OK iterations={iterations}");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
