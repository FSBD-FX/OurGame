using System;
using System.Linq;
using Godot;
using Knight.Prologue.Residents;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask9Tests : SceneTree
{
    private const string P1LowerHandleArmArgument = "--p1-lower-handle-arm";
    private const string ResidentFirstStartArmArgument = "--resident-first-start-arm";

    public override async void _Initialize()
    {
        try
        {
            string[] userArgs = OS.GetCmdlineUserArgs();
            string[] residentFlags = userArgs.Where(value => value.TrimStart()
                .StartsWith(ResidentFirstStartArmArgument, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (residentFlags.Length != 0)
            {
                TestAssert.True(residentFlags.Length == 1 && userArgs.Length == 2,
                    "Resident first-start probe requires exactly one explicit arm and one isolated storage root");
                string residentArm = residentFlags[0] switch
                {
                    ResidentFirstStartArmArgument + "=guidance-only" => "guidance-only",
                    ResidentFirstStartArmArgument + "=registry-prefix" => "registry-prefix",
                    _ => throw new InvalidOperationException("Invalid resident first-start arm")
                };
                TestAssert.Equal("headless", DisplayServer.GetName(), "Resident first-start probe is headless only");
                string residentStorage = IsolatedPrologueFixture.ValidateStorageRoot();
                Callable.From(() => RunResidentFirstStartArm(residentArm, residentStorage)).CallDeferred();
                return;
            }
            string[] armFlags = userArgs.Where(value => value.TrimStart()
                .StartsWith(P1LowerHandleArmArgument, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (armFlags.Length != 0)
            {
                TestAssert.True(armFlags.Length == 1 && userArgs.Length == 2,
                    "P1 lower Handle probe requires exactly one explicit arm and one isolated storage root");
                string arm = armFlags[0] switch
                {
                    P1LowerHandleArmArgument + "=lower-only" => "lower-only",
                    P1LowerHandleArmArgument + "=reload" => "reload",
                    P1LowerHandleArmArgument + "=recovery-reload" => "recovery-reload",
                    _ => throw new InvalidOperationException("Invalid P1 lower Handle arm")
                };
                TestAssert.Equal("headless", DisplayServer.GetName(), "P1 lower Handle probe is headless only");
                string storageRoot = IsolatedPrologueFixture.ValidateStorageRoot();
                // Begin all arms after SceneTree initialization, without adding a frame wait.
                Callable.From(() => RunP1LowerHandleArm(arm, storageRoot)).CallDeferred();
                return;
            }
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

    private async void RunResidentFirstStartArm(string arm, string storageRoot)
    {
        const int targetIndex = 18;
        const string targetName = "PrologueGuidanceRuntimeTests.MountedGuidanceRendersAndClearsWithoutHidingObjective";
        bool previousFallbacks = Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks;
        CanyonResidentManager.ResidentFirstInstantiateTrace trace = null;
        try
        {
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
            var registered = RunPrologueTests.BuildLegacyMainCases(this, storageRoot);
            RunPrologueTests.AssertLegacyMainClassifications(registered);
            TestAssert.Equal(targetName, registered[targetIndex].Name, "Resident first-start probe binds original post case #18");
            int start = arm == "guidance-only" ? targetIndex : 0;
            Task7Case[] selected = registered.Skip(start).Take(targetIndex + 1 - start).ToArray();
            TestAssert.True(selected.All(item => item.Disposition == Task7CaseDisposition.Executable && item.Execute != null),
                "Resident first-start probe requires all original selected cases to be executable");

            ulong originalRoot = Root.GetInstanceId();
            ulong[] originalChildren = CaptureRootChildren();
            int cursor = start;
            GD.Print($"RESIDENT_FIRST_ARM_BEGIN arm={arm} count={selected.Length}");
            await Task7CaseRegistry.Execute(selected, (name, entered) =>
            {
                TestAssert.Equal(registered[cursor].Name, name, "Resident first-start probe preserves original case order");
                if (cursor == targetIndex && !entered)
                {
                    trace?.Dispose();
                    TestAssert.True(trace?.Complete == true, "Resident first-start instantiate trace was absent, exceeded its bound, or failed to write");
                    trace = null;
                }
                GD.Print($"RESIDENT_FIRST_CASE arm={arm} index={cursor} stage={(entered ? "enter" : "return")} " +
                    $"name={name} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()}");
                if (cursor == targetIndex && entered) trace = new(arm);
                if (!entered) cursor++;
            });
            TestAssert.Equal(targetIndex + 1, cursor, "Resident first-start probe requires every original selected case to return");
            TestAssert.True(originalRoot == Root.GetInstanceId() && originalChildren.SequenceEqual(CaptureRootChildren()),
                "Resident first-start probe retains the original root and direct child identities after the original case tail");
            GD.Print($"RESIDENT_FIRST_ARM_END arm={arm} count={selected.Length} root_delta=0 diagnostic=true resource_fixed=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
        finally
        {
            trace?.Dispose();
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = previousFallbacks;
        }
    }

    private async void RunP1LowerHandleArm(string arm, string storageRoot)
    {
        bool previousFallbacks = Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks;
        try
        {
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
            var registered = RunPrologueTests.BuildLegacyMainCases(this, storageRoot);
            RunPrologueTests.AssertLegacyMainClassifications(registered);
            (int Index, string Name) lowerStructure = (6, "P1LoopStructureTests.LowerRouteHasMixedEncounterContracts");
            (int Index, string Name) hiddenRecovery = (11, "P1LoopRuntimeTests.HiddenDiscoveryPersistsAcrossDeathRecovery");
            (int Index, string Name) lowerReset = (12, "P1LoopRuntimeTests.PartialLowerEncounterResetsBothActors");
            (int Index, string Name)[] plan = arm switch
            {
                "lower-only" => [lowerReset],
                "reload" => [lowerStructure, lowerReset],
                "recovery-reload" => [lowerStructure, hiddenRecovery, lowerReset],
                _ => throw new InvalidOperationException("Unrecognized admitted P1 lower Handle arm")
            };
            Task7Case[] selected = plan.Select(item =>
            {
                Task7Case original = registered[item.Index];
                TestAssert.True(original.Name == item.Name && original.Disposition == Task7CaseDisposition.Executable &&
                    original.Execute != null, $"P1 lower Handle probe requires original executable post case {item.Index} {item.Name}");
                return original;
            }).ToArray();

            ulong originalRoot = Root.GetInstanceId();
            ulong[] originalChildren = CaptureRootChildren();
            int cursor = 0;
            GD.Print($"P1_LOWER_HANDLE_ARM_BEGIN arm={arm} count={plan.Length}");
            await Task7CaseRegistry.Execute(selected, (name, entered) =>
            {
                (int index, string expectedName) = plan[cursor];
                TestAssert.Equal(expectedName, name, "P1 lower Handle probe preserves the original case order");
                GD.Print($"P1_LOWER_HANDLE_CASE arm={arm} index={index} stage={(entered ? "enter" : "return")} " +
                    $"name={name} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()}");
                if (!entered) cursor++;
            });
            TestAssert.Equal(plan.Length, cursor, "P1 lower Handle probe requires every original case to return");
            TestAssert.True(originalRoot == Root.GetInstanceId() && originalChildren.SequenceEqual(CaptureRootChildren()),
                "P1 lower Handle probe retains the original root and direct child identities after the original case tail");
            GD.Print($"P1_LOWER_HANDLE_ARM_END arm={arm} count={plan.Length} root_delta=0 diagnostic=true resource_fixed=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
        finally
        {
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = previousFallbacks;
        }
    }

    private ulong[] CaptureRootChildren()
    {
        var ids = new ulong[Root.GetChildCount(true)];
        for (int index = 0; index < ids.Length; index++) ids[index] = Root.GetChild(index, true).GetInstanceId();
        return ids;
    }

    private static bool HasArgument(string expected)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
            if (argument == expected) return true;
        return false;
    }
}
