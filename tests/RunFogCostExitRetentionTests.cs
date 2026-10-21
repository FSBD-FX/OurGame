using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Knight.Tests;

/// <summary>Real engine early-quit characterization, not a complete performance experiment.</summary>
public partial class RunFogCostExitRetentionTests : RunFogCostDiagnostic
{
    public override bool _Process(double delta)
    {
        // Natural Title + 5 frames + StartNew + 60 settling frames precede A sampling.
        // No physical state, clock, frame rate, or sampling target is altered.
        if (Engine.GetProcessFrames() >= 120) Quit(0);
        return false;
    }

    public override void _Finalize()
    {
        base._Finalize();
        try
        {
            string arg = OS.GetCmdlineUserArgs()[0];
            string storage = arg["--case-storage-root=".Length..];
            string[] files = Directory.GetFiles(storage, "fog-cost.json", SearchOption.AllDirectories);
            TestAssert.Equal(1, files.Length, "engine early quit retains exactly one diagnostic artifact");
            using JsonDocument evidence = JsonDocument.Parse(File.ReadAllText(files[0]));
            JsonElement root = evidence.RootElement;
            TestAssert.Equal("engine_exit_before_diagnostic_return", root.GetProperty("outcome").GetString(),
                "partial result identifies interrupted diagnostic, not a normal completed return");
            TestAssert.True(!root.GetProperty("complete").GetBoolean() && !root.GetProperty("restored").GetBoolean(),
                "early quit never invents completed arms or restored presentation");
            TestAssert.True(root.GetProperty("failure").GetString().Contains("engine_exit_before_diagnostic_return"),
                "partial evidence records early exit as failure");
            JsonElement first = root.GetProperty("arms")[0];
            TestAssert.True(first.GetProperty("RawCount").GetInt32() > 0 &&
                first.GetProperty("WarmupCount").GetInt32() > 0, "actual rendered samples survive early engine quit");
            TestAssert.True(!root.GetProperty("humanValidated").GetBoolean() &&
                !root.GetProperty("performanceCertification").GetBoolean() && !root.GetProperty("fullRoute").GetBoolean(),
                "retention test cannot certify gameplay, human feel or performance");
            GD.Print("FOG_COST_EXIT_RETENTION_TESTS_OK assertions=6 intentionalEarlyQuit=true fullRoute=false performanceCertification=false");
        }
        catch (Exception exception) { GD.PushError("FOG_COST_EXIT_RETENTION_TEST_FAILED: " + exception); }
    }
}
