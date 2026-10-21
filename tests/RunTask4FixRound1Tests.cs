using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask4FixRound1Tests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string testCase = System.Environment.GetEnvironmentVariable("TASK4_FIX_CASE") ?? "all";
            if (testCase == "mix") PrologueAudioManifestTests.ValidateWarningMix();
            else if (testCase == "onset") PrologueAudioManifestTests.ValidateGroundWaveOnset();
            else await Task4FixRound1ContractTests.RunCase(this, testCase);
            GD.Print($"TASK4_FIX_ROUND1_TESTS_OK case={testCase}");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
