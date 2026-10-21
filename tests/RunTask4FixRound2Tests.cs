using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunTask4FixRound2Tests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string testCase = System.Environment.GetEnvironmentVariable("TASK4_FIX2_CASE") ?? "all";
            await Task4FixRound2ContractTests.RunCase(this, testCase);
            GD.Print($"TASK4_FIX_ROUND2_TESTS_OK case={testCase}");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
