using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunContinuousCanyonTests : SceneTree
{
    // As in the established aggregate runner, retain the packed resource graph
    // across repeated fixture unload/reload instead of leaving temporary wrappers.
    private static PackedScene _mainSceneLifetimeOwner;

    public override async void _Initialize()
    {
        try
        {
            if (Array.Exists(OS.GetCmdlineUserArgs(),
                    arg => arg == "--task10-warning-logger-guard-only"))
            {
                ExpectedWarningLogger.AssertMatcherGuards();
                using var warningLogger = new ExpectedWarningLogger();
                GD.PushWarning("TASK10_EXPECTED_WARNING_PROBE");
                await ToSignal(this, SignalName.ProcessFrame);
                var warnings = warningLogger.Snapshot();
                TestAssert.Equal(1, warnings.Count,
                    "focused warning probe records exactly one warning");
                TestAssert.Equal((int)Logger.ErrorType.Warning, warnings[0].ErrorType,
                    "focused warning probe records warning type");
                TestAssert.True(warnings[0].Code == "TASK10_EXPECTED_WARNING_PROBE" ||
                    warnings[0].Rationale == "TASK10_EXPECTED_WARNING_PROBE",
                    "focused warning probe captures the message in code or rationale");
                TestAssert.True(!string.IsNullOrWhiteSpace(warnings[0].File),
                    "focused warning probe captures its source file");
                TestAssert.True(!string.IsNullOrWhiteSpace(warnings[0].Function),
                    "focused warning probe captures its source function");
                GD.Print($"TASK10_WARNING_PAYLOAD type={warnings[0].ErrorType} " +
                    $"file={warnings[0].File}:{warnings[0].Line} function={warnings[0].Function} " +
                    $"code='{warnings[0].Code}' rationale='{warnings[0].Rationale}'");
                GD.Print("TASK10_EXPECTED_WARNING_GUARD_OK missing=True extra=True cleanup=using");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(),
                    arg => arg == "--task10-telemetry-regressions-only"))
            {
                ProloguePlaytestSessionTests.Run();
                await ProloguePlaytestRecorderTests.RunTask10TelemetryContracts(this);
                GD.Print("TASK10_TELEMETRY_REGRESSIONS_OK");
                Quit(0);
                return;
            }
            _mainSceneLifetimeOwner = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                ?? throw new InvalidOperationException("generated main packed scene is missing");
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task8-model-only"))
            {
                CanyonFireflyGuidanceModelTests.Run();
                GD.Print("TASK8_FIREFLY_MODEL_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task8-runtime-only"))
            {
                CanyonFireflyGuidanceModelTests.Run();
                await CanyonFireflyGuidanceRuntimeTests.Run(this);
                await PrologueGuidanceRuntimeTests.RunTask8MarkerlessCard(this);
                GD.Print("TASK8_FIREFLY_RUNTIME_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task8-regressions-only"))
            {
                PrologueGuidanceTargetResolverTests.RunTask4();
                await PrologueGuidanceTargetResolverTests.Run(this);
                await PrologueGuidanceRuntimeTests.Run(this);
                GD.Print("TASK8_EXISTING_GUIDANCE_REGRESSIONS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task8-visual-only"))
            {
                await PrologueGuidanceRuntimeTests.RunTask8MovingVisualProof(this);
                GD.Print("TASK8_MOVING_VISUAL_PROOF_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task9-readability-only"))
            {
                ContinuousCanyonReadabilityTests.Run(fromBuilder: true);
                CanyonMaterialSkinTests.Run(fromBuilder: true);
                GD.Print("TASK9_READABILITY_TESTS_OK source=builder");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task9-packed-only"))
            {
                ContinuousCanyonReadabilityTests.Run(fromBuilder: false);
                CanyonMaterialSkinTests.Run(fromBuilder: false);
                GD.Print("TASK9_READABILITY_TESTS_OK source=packed");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task9-mounted-only"))
            {
                await ContinuousCanyonReadabilityTests.RunMountedRuntime(this);
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task9-mounted-1920"))
            {
                DisplayServer.WindowSetSize(new Vector2I(1920, 1080));
                await ToSignal(this, SignalName.ProcessFrame);
                await ContinuousCanyonReadabilityTests.RunMountedRuntime(this);
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task7-migration-only"))
            {
                using (var expectedWarnings = new ExpectedWarningLogger())
                {
                    PrologueSaveTests.Run();
                    PrologueProgressTests.Run();
                    ContinuousCanyonMigrationTests.RunStore();
                    await ContinuousCanyonMigrationTests.RunMenus(this);
                    await ContinuousCanyonSaveIsolationTests.Run(this);
                    expectedWarnings.AssertExact(
                        ExpectedWarningLogger.GameFlowNegativeExpectations());
                }
                GD.Print("TASK7_MIGRATION_STORE_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task7-persistence-only"))
            {
                await ContinuousCanyonMigrationTests.RunPersistence(this);
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task7-p0-residency-only"))
            {
                await ContinuousCanyonMigrationTests.RunRecovery(this);
                await SeamlessPrologueWorldTests.RunFixRound5(this);
                await SeamlessPrologueWorldTests.RunTask2(this);
                GD.Print("TASK7_P0_AND_CLAW_RESIDENCY_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task6-catalog-only"))
            {
                CanyonEncounterCatalogTests.Run();
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task6-encounters-only"))
            {
                CanyonEncounterCatalogTests.Run();
                await ContinuousCanyonEncounterTests.Run(this);
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task6-structure-only"))
            {
                CanyonEncounterCatalogTests.Run();
                PrologueCanyonStructureTests.RunTask6();
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task6-contact-only"))
            {
                await ContinuousCanyonEncounterTests.RunTask6PhysicsContactBatch(this);
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task6-activation-only"))
            {
                await ContinuousCanyonEncounterTests.RunTask6MainRouteActivation(this);
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task6-witness-negative-only"))
            {
                PrologueCanyonStructureTests.RunTask6InertWitnessNegative();
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--bell-chain-only"))
            {
                await BellChainRuntimeTests.RunTask5(this);
                GD.Print("TASK5_BELL_CHAIN_RUNTIME_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--bell-chain-full-only"))
            {
                await BellChainRuntimeTests.Run(this);
                GD.Print("TASK5_BELL_CHAIN_FULL_RUNTIME_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--mechanisms-only"))
            {
                CanyonMechanismModelTests.Run();
                await CanyonMechanismRuntimeTests.Run(this);
                GD.Print("CANYON_MECHANISM_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--generated-lift-only"))
            {
                await CanyonMechanismRuntimeTests.RunGeneratedCounterweight(this);
                GD.Print("TASK5_GENERATED_LIFT_RUNTIME_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--generated-shortcut-only"))
            {
                await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this);
                await PrologueCanyonTraversalTests.RunTask4Ascent(this);
                GD.Print("TASK5_GENERATED_SHORTCUT_AND_PUBLIC_MAIN_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task5-seal-fix-only"))
            {
                await CanyonMechanismRuntimeTests.RunGeneratedSealGate(this);
                GD.Print("TASK5_GENERATED_SEAL_GATE_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task5-shortcut-lock-fix-only"))
            {
                await CanyonMechanismRuntimeTests.RunGeneratedShortcutLock(this);
                GD.Print("TASK5_GENERATED_SHORTCUT_LOCK_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task5-steam-visibility-fix-only"))
            {
                string outputArgument = Array.Find(OS.GetCmdlineUserArgs(),
                    arg => arg.StartsWith("--output="));
                await CanyonMechanismRuntimeTests.RunGeneratedSteamVisibility(this,
                    outputArgument?["--output=".Length..]);
                GD.Print("TASK5_GENERATED_STEAM_VISIBILITY_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--task5-lifecycle-fix-only"))
            {
                await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(this);
                GD.Print("TASK5_REQUIRED_LIFECYCLE_OK");
                Quit(0);
                return;
            }
            if(Array.Exists(OS.GetCmdlineUserArgs(),arg=>arg=="--guidance-missing-negative-only"))
            { await PrologueGuidanceTargetResolverTests.RunTask4MissingOwnerWarning(this); Quit(0); return; }
            // Explicit inner-loop probe selection only. The default/final suite below
            // always retains save, frozen sample and accepted dependency regressions.
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--probe-only"))
            {
                string[] args=OS.GetCmdlineUserArgs();
                if(Array.Exists(args,arg=>arg=="--ascent-only")) await PrologueCanyonTraversalTests.RunTask4Ascent(this);
                else if(Array.Exists(args,arg=>arg=="--connectors-only")) await PrologueCanyonTraversalTests.RunTask4GearConnectors(this);
                else if(Array.Exists(args,arg=>arg=="--shortcut-only")) await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this);
                else if(Array.Exists(args,arg=>arg=="--tolerance-only")) await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this, true);
                else if(Array.Exists(args,arg=>arg=="--main-only")) await CanyonMechanismRuntimeTests.RunGeneratedMainWithSealPrerequisites(this);
                else if(Array.Exists(args,arg=>arg=="--lessons-only")) await PrologueCanyonTraversalTests.RunTask4FirstLessons(this);
                else throw new ArgumentException("probe-only requires a named geometry probe");
                GD.Print("TASK4_FOCUSED_PHYSICAL_PROBE_OK not_full_suite=True"); Quit(0); return;
            }
            using (var expectedWarnings = new ExpectedWarningLogger())
            {
                PrologueSaveTests.Run();
                PrologueProgressTests.Run();
                ContinuousCanyonMigrationTests.RunStore();
                await ContinuousCanyonMigrationTests.RunMenus(this);
                await ContinuousCanyonMigrationTests.RunPersistence(this);
                await ContinuousCanyonMigrationTests.RunRecovery(this);
                await ContinuousCanyonSaveIsolationTests.Run(this);
                expectedWarnings.AssertExact(
                    ExpectedWarningLogger.GameFlowNegativeExpectations());
            }
            GD.Print("CONTINUOUS_CANYON_SAVE_ISOLATION_OK");
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--guidance-only"))
            { PrologueGuidanceTargetResolverTests.RunTask4(); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--save-only"))
            {
                Quit(0);
                return;
            }
            CanyonFireflyGuidanceModelTests.Run();
            await CanyonFireflyGuidanceRuntimeTests.Run(this);
            await PrologueGuidanceRuntimeTests.RunTask8MarkerlessCard(this);
            await PrologueGuidanceRuntimeTests.Run(this);
            GD.Print("TASK8_DEFAULT_GUIDANCE_COVERAGE_OK");
            ContinuousCanyonReadabilityTests.Run(fromBuilder: false);
            CanyonMaterialSkinTests.Run(fromBuilder: false);
            await ContinuousCanyonReadabilityTests.RunMountedRuntime(this);
            GD.Print("TASK9_DEFAULT_READABILITY_COVERAGE_OK");
            await BasinSampleLayoutIsolationTests.Run(this);
            await BasinSampleBoundaryTests.Run(this, "all");
            GD.Print("BASIN_SAMPLE_LAYOUT_ISOLATION_OK");
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--sample-only"))
            {
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--contracts-only"))
            {
                PrologueCanyonStructureTests.RunTask4();
                PrologueGuidanceTargetResolverTests.RunTask4();
                await PrologueGuidanceTargetResolverTests.Run(this);
                PrologueEnvironmentStructureTests.Run();
                GD.Print("TASK4_FOCUSED_GENERATED_CONTRACTS_OK not_full_suite=True"); Quit(0); return;
            }
            CanyonEncounterCatalogTests.Run();
            PrologueCanyonStructureTests.RunTask6();
            await ContinuousCanyonEncounterTests.Run(this);
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--lessons-only"))
            { await PrologueCanyonTraversalTests.RunTask4FirstLessons(this); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--shortcut-only"))
            { await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--transfer-only"))
            { await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this, firstTransferOnly: true); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--tolerance-only"))
            { await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this, true); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--connectors-only"))
            { await PrologueCanyonTraversalTests.RunTask4GearConnectors(this); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--ascent-only"))
            { await PrologueCanyonTraversalTests.RunTask4Ascent(this); Quit(0); return; }
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--main-only"))
            { await CanyonMechanismRuntimeTests.RunGeneratedMainWithSealPrerequisites(this); Quit(0); return; }
            if (!Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--abyss-only"))
                PrologueCanyonStructureTests.RunTask4();
            if (!Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--abyss-only"))
            {
                PrologueGuidanceTargetResolverTests.RunTask4();
                await PrologueGuidanceTargetResolverTests.Run(this);
                PrologueEnvironmentStructureTests.Run();
            }
            await PrologueCanyonTraversalTests.RunTask4(this);
            if (Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--abyss-only")) { Quit(0); return; }
            await PrologueCanyonTraversalTests.RunTask4FirstLessons(this);
            await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this);
            await CanyonMechanismRuntimeTests.RunGeneratedShortcutRoute(this, true);
            await PrologueCanyonTraversalTests.RunTask4GearConnectors(this);
            await PrologueCanyonTraversalTests.RunTask4Ascent(this);
            await CanyonMechanismRuntimeTests.RunGeneratedMainWithSealPrerequisites(this);
            CanyonMechanismModelTests.Run();
            await CanyonMechanismRuntimeTests.Run(this);
            CanyonCameraFocusModelTests.Run();
            await SeamlessPrologueWorldTests.RunTask3(this);
            CanyonWorldLayoutTests.Run();
            PrologueSectorStreamModelTests.Run();
            using (var expectedWarnings = new ExpectedWarningLogger())
            {
                await SeamlessPrologueWorldTests.Run(this);
                expectedWarnings.AssertExact(
                    ExpectedWarningLogger.ThrowingClawObserverExpectations());
            }
            await SeamlessPrologueWorldTests.RunFixRound2(this);
            await SeamlessPrologueWorldTests.RunFixRound3(this);
            await SeamlessPrologueWorldTests.RunFixRound4(this);
            await SeamlessPrologueWorldTests.RunFixRound5(this);
            await SeamlessPrologueWorldTests.RunTask2(this);
            SeamlessPrologueMainSceneContractTests.VerifyProcessLifetimeOwner(_mainSceneLifetimeOwner,
                ownerLabel: nameof(RunContinuousCanyonTests));
            GD.Print("CONTINUOUS_CANYON_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
