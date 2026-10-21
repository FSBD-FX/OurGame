using System;
using System.Linq;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

// Focused UI regression only: controlled fixture state is not a natural route or human-input proof.
public partial class RunUiConsistencyTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Count(arg => arg.StartsWith("--case-storage-root=")) == 1 &&
                args.Count(arg => arg.StartsWith("--phase=")) == 1 &&
                args.All(arg => arg.StartsWith("--case-storage-root=") || arg.StartsWith("--phase=") || arg == "--capture"),
                "UI consistency accepts one external storage root and one focused phase");
            IsolatedPrologueFixture.ValidateStorageRoot();
            await ToSignal(this, SignalName.ProcessFrame);
            string phase = args.Single(arg => arg.StartsWith("--phase="))[8..];
            bool capture = args.Contains("--capture");
            if (phase == "menu") await PrologueMigrationMenuTests.Run(this, capture);
            else if (phase == "guidance") await BossAdmissionGuidanceTests.Run(this, capture);
            else if (phase == "models")
            {
                BossAdmissionModelTests.Run();
                PrologueGuidanceModelTests.Run();
                PrologueGuidanceModelTests.RunTempleTerrainGuidance();
                await TemplePressureGateAccessPhysicsTests.TempleGuidanceRuntime(this);
            }
            else throw new InvalidOperationException("Unknown UI consistency phase");
            GD.Print("UI_CONSISTENCY_OK phase=" + phase + " human=false whole_goal=false");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
        finally { Paused = false; }
    }
}
