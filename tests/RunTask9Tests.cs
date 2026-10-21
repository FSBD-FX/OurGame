using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask9Tests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (HasArgument("--registry-contract"))
            {
                await Task7CaseRegistryTests.Run(this);
                GD.Print("TASK9_REGISTRY_CONTRACT_FOCUSED_OK authoritative=75");
                Quit(0);
                return;
            }
            if (HasArgument("--progress-contract"))
            {
                PrologueProgressTests.Run();
                GD.Print("TASK9_PROGRESS_CONTRACT_FOCUSED_OK legacy_aliases=0");
                Quit(0);
                return;
            }
            if (HasArgument("--telemetry-grant-contract"))
            {
                ProloguePlaytestEvidenceGrantTests.Run();
                GD.Print("TASK9_TELEMETRY_GRANT_CONTRACT_FOCUSED_OK single_use=True");
                Quit(0);
                return;
            }
            if (HasArgument("--registry-diagnose"))
            {
                string flowRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    "knight-task9-registry-diagnose-" + Guid.NewGuid().ToString("N"));
                System.IO.Directory.CreateDirectory(flowRoot);
                Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
                try
                {
                    var cases = RunPrologueTests.BuildLegacyMainCases(this, flowRoot);
                    RunPrologueTests.AssertLegacyMainClassifications(cases);
                    foreach (Task7Case item in cases)
                    {
                        GD.Print($"TASK9_REGISTRY_CASE_START name={item.Name}");
                        await Task7CaseRegistry.Execute([item]);
                        GD.Print($"TASK9_REGISTRY_CASE_COMPLETE name={item.Name}");
                    }
                }
                finally
                {
                    Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = false;
                    Paused = false;
                    if (System.IO.Directory.Exists(flowRoot))
                        System.IO.Directory.Delete(flowRoot, recursive: true);
                }
                GD.Print("TASK9_REGISTRY_DIAGNOSE_OK executable=75");
                Quit(0);
                return;
            }
            if (HasArgument("--registry-aggregate"))
            {
                Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
                try
                {
                    await RunPrologueTests.ExecuteLegacyMainCasePlan(this);
                }
                finally
                {
                    Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = false;
                }
                GD.Print("TASK9_REGISTRY_AGGREGATE_FOCUSED_OK executable=75 retired=0");
                Quit(0);
                return;
            }
            if (HasArgument("--level-runtime"))
            {
                Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
                try
                {
                    await PrologueLevelRuntimeTests.Run(this);
                }
                finally
                {
                    Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = false;
                }
                GD.Print("TASK9_LEVEL_RUNTIME_FOCUSED_OK continuous_death=True");
                Quit(0);
                return;
            }
            if (HasArgument("--guidance-runtime"))
            {
                Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
                try
                {
                    await PrologueGuidanceRuntimeTests.Run(this);
                }
                finally
                {
                    Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = false;
                }
                GD.Print("TASK9_GUIDANCE_RUNTIME_FOCUSED_OK bilingual_chrome=True");
                Quit(0);
                return;
            }
            if (HasArgument("--generated-main-headless"))
            {
                bool completedDisplayRoute = await PrologueCanyonGeneratedMainRouteTests.Run(
                    this, captureFrames: false);
                TestAssert.True(!completedDisplayRoute && DisplayServer.GetName() == "headless",
                    "headless generated-main smoke stops truthfully at the display pointer boundary");
                GD.Print("TASK9_GENERATED_MAIN_HEADLESS_OK stream=True recovery=0 pointer_boundary=True");
                Quit(0);
                return;
            }
            if (HasArgument("--generated-main"))
            {
                DisplayServer.WindowSetSize(new Vector2I(1280, 720));
                Root.Size = new Vector2I(1280, 720);
                await ToSignal(this, SignalName.ProcessFrame);
                await PrologueTask9GeneratedMainTests.Run(this, captureFrames: false);
                GD.Print("TASK9_GENERATED_MAIN_OK");
                Quit(0);
                return;
            }
            PrologueObjectiveTests.Run();
            GD.Print("TASK9_OBJECTIVE_GREEN_OK states=10 same_frame_skip=False");
            PrologueProgressTests.Run();
            GD.Print("TASK9_PROGRESS_GREEN_OK checkpoints=start,basin,temple explicit_only=True");
            PrologueGuidanceModelTests.Run();
            await PrologueGuidanceTargetResolverTests.Run(this);
            GD.Print("TASK9_GUIDANCE_GREEN_OK bilingual=True inputmap=True current_sector=True");
            PrologueSaveTests.Run();
            GD.Print("TASK9_SAVE_GREEN_OK v1_selective=True v2_roundtrip=True corrupt_fail_closed=True");
            ProloguePlaytestSessionTests.Run();
            GD.Print("TASK9_TELEMETRY_GREEN_OK schema=2 exact_fields=6 migration=True");
            await PrologueGameFlowTests.RunTask7IndependentCases(this);
            await Task7CaseRegistryTests.Run(this);
            GD.Print("TASK9_REGISTRY_GREEN_OK replacements=2 retirements=0 fail_closed=True");
            GD.Print("TASK9_CORE_FOCUSED_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static bool HasArgument(string expected)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
            if (argument == expected) return true;
        return false;
    }
}
