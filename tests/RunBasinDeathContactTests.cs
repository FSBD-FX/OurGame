using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinDeathContactTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await BasinQualitySampleEntryTests.LethalAreaContactDefersRecoveryUntilAfterContact(this);
            await PrologueGameFlowTests.Run(this);
            await PlayerFormalVisualTests.Run(this);
            await ProloguePlaytestRecorderTests.RunTask5FallContextContracts(this);
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
            try { await PrologueLevelRuntimeTests.Run(this); }
            finally { Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = false; }
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("basin-death-contact");
            GD.Print("BASIN_DEATH_CONTACT_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
