using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask7Fix1Tests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            PrologueSectorStreamModelTests.Run();
            await SeamlessPrologueWorldTests.RunFixRound2(this);
            await SeamlessPrologueWorldTests.RunFixRound3(this);
            await SeamlessPrologueWorldTests.RunFixRound4(this);
            GD.Print("TASK7_FIX4_ACTIVATION_OK");
            await SeamlessPrologueWorldTests.RunFixRound5(this);
            GD.Print("TASK7_FIX5_OWNERSHIP_OK");
            await Task7CaseRegistryTests.Run(this);
            GD.Print("TASK7_FIX4_WRAPPER_API_OK wrappers=10 ignored_legacy_parameters=0");
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
            try { await RunPrologueTests.ExecuteLegacyMainCasePlan(this); }
            finally { Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = false; }
            GD.Print("TASK7_FIX4_FOCUSED_OK");
            GD.Print("TASK7_FIX5_FOCUSED_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
