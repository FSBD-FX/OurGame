using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Knight.Tests.Integration;
using Knight.Tests.Unit;
using Knight.Tests.Presentation;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Residents;

namespace Knight.Tests;

public partial class RunPrologueTests : SceneTree
{
    private const string ResidentFirstPostArgument = "--resident-first-start-post";
    private const int ResidentFirstPostCaseIndex = 18;
    private const string ResidentFirstPostCaseName = "PrologueGuidanceRuntimeTests.MountedGuidanceRendersAndClearsWithoutHidingObjective";
    [ThreadStatic] internal static Action ResidentFirstPostStartupCommitted;
    private static PackedScene _postMainSceneKeepAlive;
    private bool _phaseEntered;
#if REFCOUNTED_FATAL_IDENTITY
    internal static void AssertPreLifetimeTraceCompleteForSuccess()
    {
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Active.AssertComplete();
        Console.Error.WriteLine("PROFILE_LIFETIME_PRE_TRACE_COMPLETE checked_faults_and_scopes=true resource_fix=false behavior_accepted=false");
        Console.Error.Flush();
    }
    internal static void EndResidentAssignmentTraceForSuccess()
    {
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Active.AssertComplete();
        GD.Print("PROFILE_LIFETIME_TRACE_COMPLETE scalar_only=true resource_fix=false behavior_accepted=false");
        if (!Knight.Tests.Diagnostics.Task14NativeResourceIdentity.EndResidentAssignmentTrace())
            throw new InvalidOperationException("Resident assignment diagnostic completion failed.");
    }
    internal static void EndResidentProfileTraceForSuccess()
    {
        if (!Knight.Tests.Diagnostics.Task14NativeResourceIdentity.EndResidentProfileTrace())
            throw new InvalidOperationException("Resident profile diagnostic completion failed; never accept a clean-looking END after a sink failure.");
    }
#endif

    internal static Exception WriteEarlyFailure(Exception primary, System.IO.TextWriter sink)
    {
        try
        {
            sink.WriteLine("PROLOGUE_TEST_FAILURE_DETAIL " + System.Text.Json.JsonSerializer.Serialize(new
            {
                Type = primary.GetType().FullName, primary.Message, Detail = primary.ToString()
            }));
            sink.Flush();
            return null;
        }
        catch (Exception loggingFailure) { return loggingFailure; }
    }

    public override async void _Initialize()
    {
        try
        {
            string[] userArgs = OS.GetCmdlineUserArgs();
            bool residentFirstStartPost = ResolveResidentFirstStartPost(userArgs);
            if (await AdmitStorageOrStopProbe()) return;
            DisplayBackedFlowAutoStartHarness.Attach(this);
            if (userArgs.Contains("--task3-catch-one-only"))
            {
                _phaseEntered = true;
                await PrologueCanyonTraversalTests.RunCatchOneProbe(this);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("task3-catch-one");
                Quit(0);
                return;
            }
            string phase = ResolvePhase();
            bool omitLegacyTraversal = ResolveLegacyPostPrefixControl(phase,
                System.Environment.GetEnvironmentVariable("KNIGHT_POST_PREFIX_CONTROL"));
            if (phase == "pre")
            {
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.BeginOriginalProbeTrace();
#endif
                _phaseEntered = true;
                await RunPrePhase(this);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("pre");
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.AssertPresentationAllocationComplete();
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.AssertSectorAllocationComplete();
                AssertPreLifetimeTraceCompleteForSuccess();
#endif
                GD.Print("PROLOGUE_TEST_PHASE_OK phase=pre");
                Quit(0);
                return;
            }
            if (phase == "post")
            {
#if REFCOUNTED_FATAL_IDENTITY
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.BeginResidentAssignmentTrace();
#endif
                _phaseEntered = true;
                if (omitLegacyTraversal)
                    GD.Print("POST_PREFIX_CONTROL_BEGIN arm=omit-canyon-traversal skipped=PrologueCanyonTraversalTests.Run skipped_count=1 original_post_accepted=false behavior_accepted=false resource_fix=false whole_goal=false");
                await RunPostPhase(this, omitLegacyTraversal, residentFirstStartPost);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("post");
#if REFCOUNTED_FATAL_IDENTITY
                EndResidentAssignmentTraceForSuccess();
#endif
                if (omitLegacyTraversal)
                    GD.Print("POST_PREFIX_CONTROL_RETURN arm=omit-canyon-traversal skipped=PrologueCanyonTraversalTests.Run skipped_count=1 original_post_accepted=false behavior_accepted=false resource_fix=false whole_goal=false");
                else
                {
                    GD.Print("PROLOGUE_TEST_PHASE_OK phase=post");
                    GD.Print("PROLOGUE_TESTS_OK");
                }
                Quit(0);
                return;
            }
        }
        catch (Exception exception)
        {
            GD.Print("PROLOGUE_TEST_FAILURE_ENTER type=" + exception.GetType().FullName);
            Exception earlyLoggingFailure = WriteEarlyFailure(exception, Console.Error);
#if REFCOUNTED_FATAL_IDENTITY
            // End before cleanup can fail natively; this method contains its own
            // sink failures, so the original exception remains authoritative.
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.EndResidentProfileTrace();
            Knight.Tests.Diagnostics.Task14NativeResourceIdentity.EndResidentAssignmentTrace();
#endif
            Exception cleanupFailure = null;
            try
            {
                DisplayBackedFlowAutoStartHarness.Detach(this);
                _postMainSceneKeepAlive = null;
                await ToSignal(this, SignalName.ProcessFrame);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("failure");
            }
            catch (Exception cleanupException)
            {
                cleanupFailure = cleanupException;
            }
            GD.PushError(exception.ToString());
            if (cleanupFailure != null)
                GD.PushError($"RunPrologueTests failure-path cleanup also failed: {cleanupFailure}");
            if (earlyLoggingFailure != null)
                GD.PushError($"RunPrologueTests early failure logging also failed: {earlyLoggingFailure}");
            Quit(1);
        }
        finally
        {
            DisplayBackedFlowAutoStartHarness.Detach(this);
            _postMainSceneKeepAlive = null;
        }
    }

    // Normal entry and diagnostic share the same validation call. Probe mode always
    // stops before phase dispatch, even when a bad root is unexpectedly accepted.
    private async Task<bool> AdmitStorageOrStopProbe()
    {
        bool probe = OS.GetCmdlineUserArgs().Contains("--storage-validation-only");
        int mainAdded = 0;
        int flowAdded = 0;
        int mainInitial = Root.GetChildren().Count(node => node is PrologueLevelController);
        void Added(Node node)
        {
            if (node is PrologueLevelController) mainAdded++;
            if (node is PrologueGameFlowController) flowAdded++;
        }
        if (probe) NodeAdded += Added;
        bool accepted = true;
        try
        {
            if (probe)
                GD.Print("STORAGE_ADMISSION_CONTEXT " + System.Text.Json.JsonSerializer.Serialize(new
                {
                    ProjectRoot = ProjectSettings.GlobalizePath("res://"),
                    UserDataRoot = OS.GetUserDataDir()
                }));
            try
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
            }
            catch (InvalidOperationException exception) when (probe)
            {
                accepted = false;
                GD.Print("STORAGE_ADMISSION_REJECTED reason=" + exception.Message);
            }
            if (!probe) return false; // Normal admission failure propagates to the runner.
            if (accepted) GD.Print("STORAGE_ADMISSION_ACCEPTED");
            await ToSignal(this, SignalName.ProcessFrame);
            GD.Print($"STORAGE_ADMISSION_BOUNDARY phase_entered={_phaseEntered} " +
                $"main_initial={mainInitial} main_added={mainAdded} flow_added={flowAdded} " +
                $"harness_attached={DisplayBackedFlowAutoStartHarness.IsAttached(this)}");
            TestAssert.True(!_phaseEntered && mainInitial == 0 && mainAdded == 0 && flowAdded == 0 &&
                !DisplayBackedFlowAutoStartHarness.IsAttached(this),
                "admission-only probe must never attach harness or enter a fixture phase");
            Quit(accepted ? 0 : 23);
            return true; // Never dispatch a phase in probe mode, including acceptance.
        }
        finally { if (probe) NodeAdded -= Added; }
    }

    private static string ResolvePhase()
    {
        string selectedPhase = null;
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            const string prefix = "--phase=";
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                if (selectedPhase != null)
                    throw new InvalidOperationException(
                        "RunPrologueTests requires exactly one --phase=pre or --phase=post argument.");
                selectedPhase = argument.Substring(prefix.Length);
            }
        }
        if (selectedPhase == "pre" || selectedPhase == "post") return selectedPhase;
        string actual = selectedPhase ?? "<missing>";
        throw new InvalidOperationException(
            $"RunPrologueTests requires --phase=pre or --phase=post; actual={actual}.");
    }

    internal static bool ResolveLegacyPostPrefixControl(string phase, string arm)
    {
        if (phase is not ("pre" or "post"))
            throw new InvalidOperationException("Post prefix control requires an ordinary pre/post phase.");
        if (string.IsNullOrEmpty(arm)) return false;
        if (phase != "post" || arm != "omit-canyon-traversal")
            throw new InvalidOperationException("Unknown or non-post KNIGHT_POST_PREFIX_CONTROL arm.");
        return true;
    }

    private static bool ResolveResidentFirstStartPost(string[] userArgs)
    {
        bool admitted = false;
        foreach (string argument in userArgs)
        {
            if (!argument.TrimStart().StartsWith(ResidentFirstPostArgument, StringComparison.OrdinalIgnoreCase)) continue;
            if (admitted || argument != ResidentFirstPostArgument)
                throw new InvalidOperationException("Resident first-start post diagnostic requires one exact flag");
            admitted = true;
        }
        if (!admitted) return false;
        if (userArgs.Length != 3 || !userArgs.Contains("--phase=post") ||
            userArgs.Count(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)) != 1 ||
            DisplayServer.GetName() != "headless" || !string.IsNullOrEmpty(
                System.Environment.GetEnvironmentVariable("KNIGHT_POST_PREFIX_CONTROL")))
            throw new InvalidOperationException("Resident first-start post diagnostic requires headless exact post with one isolated storage root and no mixed modes");
#if REFCOUNTED_FATAL_IDENTITY
        throw new InvalidOperationException("Resident first-start post diagnostic requires the ordinary binary");
#else
        return true;
#endif
    }

    private static async Task RunPrePhase(SceneTree tree)
    {
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        var timing = new PreFixtureTimingJournal();
        CapturePreBoundary(timing, 0, "BasinAscentCameraRuntimeTests.Run", true);
        await BasinAscentCameraRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 0, "BasinAscentCameraRuntimeTests.Run", false);
        CapturePreBoundary(timing, 1, "BasinLaborerClearanceRuntimeTests.Run", true);
        await BasinLaborerClearanceRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 1, "BasinLaborerClearanceRuntimeTests.Run", false);
        CapturePreBoundary(timing, 2, "BasinClawTargetTests.Run", true);
        await BasinClawTargetTests.Run(tree);
        CapturePreBoundary(timing, 2, "BasinClawTargetTests.Run", false);
        CapturePreBoundary(timing, 3, "HealthRestoreTests.Run", true);
        HealthRestoreTests.Run();
        CapturePreBoundary(timing, 3, "HealthRestoreTests.Run", false);
        CapturePreBoundary(timing, 4, "BasinSupplyRuntimeTests.OriginalCases", true);
#if REFCOUNTED_FATAL_IDENTITY
        // Keep the original pre-prefix and four Supply cases in their original
        // position. Enable the already owned synchronous assigned-field probe.
        await BasinSupplyRuntimeTests.RunCompleteLifetime(tree, prefix: "pre");
#else
        await BasinSupplyRuntimeTests.Run(tree);
#endif
        CapturePreBoundary(timing, 4, "BasinSupplyRuntimeTests.OriginalCases", false);
        CapturePreBoundary(timing, 5, "DescentClawGuidanceRuntimeTests.Run", true);
        await DescentClawGuidanceRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 5, "DescentClawGuidanceRuntimeTests.Run", false);
        CapturePreBoundary(timing, 6, "FirstCombatGuidanceRuntimeTests.Run", true);
        await FirstCombatGuidanceRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 6, "FirstCombatGuidanceRuntimeTests.Run", false);
        CapturePreBoundary(timing, 7, "PrologueGuidanceRuntimeTests.ContinuousGuidanceProtectsLiveGameplay", true);
        await PrologueGuidanceRuntimeTests.ContinuousGuidanceProtectsLiveGameplay(tree);
        CapturePreBoundary(timing, 7, "PrologueGuidanceRuntimeTests.ContinuousGuidanceProtectsLiveGameplay", false);
        CapturePreBoundary(timing, 8, "PlayerShieldAttackHandoffTests.Run", true);
        await PlayerShieldAttackHandoffTests.Run(tree);
        CapturePreBoundary(timing, 8, "PlayerShieldAttackHandoffTests.Run", false);
        CapturePreBoundary(timing, 9, "RustShellSightRuntimeTests.Run", true);
        await RustShellSightRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 9, "RustShellSightRuntimeTests.Run", false);
        CapturePreBoundary(timing, 10, "BellBackLaborerSightRuntimeTests.Run", true);
        await BellBackLaborerSightRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 10, "BellBackLaborerSightRuntimeTests.Run", false);
        CapturePreBoundary(timing, 11, "GrappleSurfaceTests.Run", true);
            GrappleSurfaceTests.Run();
        CapturePreBoundary(timing, 11, "GrappleSurfaceTests.Run", false);
        CapturePreBoundary(timing, 12, "MechanicalClawRuntimeTests.Run", true);
             await MechanicalClawRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 12, "MechanicalClawRuntimeTests.Run", false);
        CapturePreBoundary(timing, 13, "Integration.ThreadInteractionTests.Run", true);
             await Knight.Tests.Integration.ThreadInteractionTests.Run(tree);
        CapturePreBoundary(timing, 13, "Integration.ThreadInteractionTests.Run", false);
        CapturePreBoundary(timing, 14, "CanyonClawMechanismTests.Run", true);
             await CanyonClawMechanismTests.Run(tree);
        CapturePreBoundary(timing, 14, "CanyonClawMechanismTests.Run", false);
        CapturePreBoundary(timing, 15, "PlayerTraversalInputTests.Run", true);
             await PlayerTraversalInputTests.Run(tree);
        CapturePreBoundary(timing, 15, "PlayerTraversalInputTests.Run", false);
            GD.Print("TASK2_TYPED_TERRAIN_TESTS_OK");
        CapturePreBoundary(timing, 16, "ProductionAssetManifestTests.Run", true);
            ProductionAssetManifestTests.Run();
        CapturePreBoundary(timing, 16, "ProductionAssetManifestTests.Run", false);
        CapturePreBoundary(timing, 17, "PrologueAudioManifestTests.Run", true);
            PrologueAudioManifestTests.Run();
        CapturePreBoundary(timing, 17, "PrologueAudioManifestTests.Run", false);
        CapturePreBoundary(timing, 18, "GameSettingsTests.Run", true);
            GameSettingsTests.Run();
        CapturePreBoundary(timing, 18, "GameSettingsTests.Run", false);
        CapturePreBoundary(timing, 19, "PrologueSaveTests.Run", true);
            PrologueSaveTests.Run();
        CapturePreBoundary(timing, 19, "PrologueSaveTests.Run", false);
        CapturePreBoundary(timing, 20, "InputGlyphResolverTests.Run", true);
            InputGlyphResolverTests.Run();
        CapturePreBoundary(timing, 20, "InputGlyphResolverTests.Run", false);
        CapturePreBoundary(timing, 21, "PrologueStringsTests.Run", true);
            PrologueStringsTests.Run();
        CapturePreBoundary(timing, 21, "PrologueStringsTests.Run", false);
        CapturePreBoundary(timing, 22, "TuningContractTests.Run", true);
            TuningContractTests.Run();
        CapturePreBoundary(timing, 22, "TuningContractTests.Run", false);
        CapturePreBoundary(timing, 23, "PlayerPresentationTests.Run", true);
            PlayerPresentationTests.Run();
        CapturePreBoundary(timing, 23, "PlayerPresentationTests.Run", false);
        CapturePreBoundary(timing, 24, "PlayerPoseMixerTests.Run", true);
            PlayerPoseMixerTests.Run();
        CapturePreBoundary(timing, 24, "PlayerPoseMixerTests.Run", false);
        CapturePreBoundary(timing, 25, "SecondaryMotionModelTests.Run", true);
            SecondaryMotionModelTests.Run();
        CapturePreBoundary(timing, 25, "SecondaryMotionModelTests.Run", false);
        CapturePreBoundary(timing, 26, "AttackPresentationFrameTests.Run", true);
            AttackPresentationFrameTests.Run();
        CapturePreBoundary(timing, 26, "AttackPresentationFrameTests.Run", false);
        CapturePreBoundary(timing, 27, "CombatReadabilityExperienceTests.Run", true);
            CombatReadabilityExperienceTests.Run();
        CapturePreBoundary(timing, 27, "CombatReadabilityExperienceTests.Run", false);
        CapturePreBoundary(timing, 28, "GraveLampMothModelTests.Run", true);
            GraveLampMothModelTests.Run();
        CapturePreBoundary(timing, 28, "GraveLampMothModelTests.Run", false);
        CapturePreBoundary(timing, 29, "EncounterDefinitionTests.Run", true);
            EncounterDefinitionTests.Run();
        CapturePreBoundary(timing, 29, "EncounterDefinitionTests.Run", false);
        CapturePreBoundary(timing, 30, "ThreadAbilityModelTests.Run", true);
            ThreadAbilityModelTests.Run();
        CapturePreBoundary(timing, 30, "ThreadAbilityModelTests.Run", false);
        CapturePreBoundary(timing, 31, "ThreadCastModelTests.Run", true);
            ThreadCastModelTests.Run();
        CapturePreBoundary(timing, 31, "ThreadCastModelTests.Run", false);
        CapturePreBoundary(timing, 32, "ThreadExperienceRuntimeTests.Run", true);
            ThreadExperienceRuntimeTests.Run();
        CapturePreBoundary(timing, 32, "ThreadExperienceRuntimeTests.Run", false);
        CapturePreBoundary(timing, 33, "Unit.ThreadInteractionTests.Run", true);
            Knight.Tests.Unit.ThreadInteractionTests.Run();
        CapturePreBoundary(timing, 33, "Unit.ThreadInteractionTests.Run", false);
        CapturePreBoundary(timing, 34, "ArmoredHitAccumulatorTests.Run", true);
            ArmoredHitAccumulatorTests.Run();
        CapturePreBoundary(timing, 34, "ArmoredHitAccumulatorTests.Run", false);
        CapturePreBoundary(timing, 35, "BellBackLaborerModelTests.Run", true);
            BellBackLaborerModelTests.Run();
        CapturePreBoundary(timing, 35, "BellBackLaborerModelTests.Run", false);
        CapturePreBoundary(timing, 36, "FallenBellkeeperModelTests.Run", true);
            FallenBellkeeperModelTests.Run();
        CapturePreBoundary(timing, 36, "FallenBellkeeperModelTests.Run", false);
        CapturePreBoundary(timing, 37, "DamageSourceIdentityRuntimeTests.Run", true);
            DamageSourceIdentityRuntimeTests.Run();
        CapturePreBoundary(timing, 37, "DamageSourceIdentityRuntimeTests.Run", false);
        CapturePreBoundary(timing, 38, "PlayerShieldRuntimeTests.Run", true);
            await PlayerShieldRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 38, "PlayerShieldRuntimeTests.Run", false);
        CapturePreBoundary(timing, 39, "PlayerActionModeRuntimeTests.Run", true);
            await PlayerActionModeRuntimeTests.Run(tree);
        CapturePreBoundary(timing, 39, "PlayerActionModeRuntimeTests.Run", false);
        CapturePreBoundary(timing, 40, "PrologueObjectiveTests.Run", true);
            PrologueObjectiveTests.Run();
        CapturePreBoundary(timing, 40, "PrologueObjectiveTests.Run", false);
        CapturePreBoundary(timing, 41, "PrologueGuidanceModelTests.Run", true);
            PrologueGuidanceModelTests.Run();
        CapturePreBoundary(timing, 41, "PrologueGuidanceModelTests.Run", false);
        CapturePreBoundary(timing, 42, "PrologueGuidanceTargetResolverTests.Run", true);
            await PrologueGuidanceTargetResolverTests.Run(tree);
        CapturePreBoundary(timing, 42, "PrologueGuidanceTargetResolverTests.Run", false);
        CapturePreBoundary(timing, 43, "PrologueTraversalRouteTests.Run", true);
            GD.Print($"PROLOGUE_PRE_TRAVERSAL_OBS stage=enter engine_ms={Time.GetTicksMsec()} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()}");
            await PrologueTraversalRouteTests.Run(tree);
            GD.Print($"PROLOGUE_PRE_TRAVERSAL_OBS stage=return engine_ms={Time.GetTicksMsec()} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()}");
        CapturePreBoundary(timing, 43, "PrologueTraversalRouteTests.Run", false);
        GD.Print(timing.Complete(44));
    }

    private static void CapturePreBoundary(PreFixtureTimingJournal timing, int index, string name, bool entered) =>
        GD.Print(timing.Observe(index, name, entered, Time.GetTicksMsec(), Engine.GetPhysicsFrames(), Engine.GetProcessFrames()));

    private static async Task RunPostPhase(SceneTree tree, bool omitLegacyTraversal = false, bool residentFirstStartPost = false)
    {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            _postMainSceneKeepAlive = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn") ??
                throw new InvalidOperationException("Task 7 main prologue scene is missing.");
            PrologueCanyonStructureTests.Run(_postMainSceneKeepAlive);
            CanyonMaterialSkinTests.Run(fromBuilder: false);
            if (!omitLegacyTraversal) await PrologueCanyonTraversalTests.Run(tree);
            else GD.Print("POST_PREFIX_CONTROL_SKIPPED arm=omit-canyon-traversal skipped=PrologueCanyonTraversalTests.Run skipped_count=1 original_post_accepted=false behavior_accepted=false resource_fix=false whole_goal=false");
            ContinuousCanyonRouteProbe.RunTelemetryContract();
            ProloguePlaytestSessionTests.Run();
            ProloguePlaytestEvidenceGrantTests.Run();
            await ProloguePlaytestRecorderTests.RunTask5FallContextContracts(tree);
            PrologueSectorStreamModelTests.Run();
            using (var expectedWarnings = new ExpectedWarningLogger())
            {
                await SeamlessPrologueWorldTests.Run(tree);
                expectedWarnings.AssertExact(
                    ExpectedWarningLogger.ThrowingClawObserverExpectations());
                GD.Print("PROLOGUE_POST_WARNING_SCOPE_OK scope=seamless count=1 source=scripts/abilities/MechanicalClawController.cs function=DispatchFrameChanged");
            }
            SeamlessPrologueMainSceneContractTests.Run(_postMainSceneKeepAlive);
            PrologueRouteGraphTests.Run();
            PrologueProgressTests.Run();
            using (var expectedWarnings = new ExpectedWarningLogger())
            {
                PrologueRoomContractTests.Run();
                expectedWarnings.AssertExact(new ExpectedWarningLogger.Expected(
                    "RoomPresentationProfile: VisualScene root must be Node2D for 'VisualRoot'.",
                    "scripts/config/RoomPresentationProfile.cs", "Resolve", 1));
                GD.Print("PROLOGUE_POST_WARNING_SCOPE_OK scope=room-profile count=1 source=scripts/config/RoomPresentationProfile.cs function=Resolve");
            }
            PrologueUiStructureTests.Run();
            PrologueHudViewModelTests.Run();
            PlayerAnimationMapTests.Run();
            BossSceneStructureTests.Run();
            await GraveLampMothRuntimeTests.Run(tree);
            await EncounterControllerTests.Run(tree);
            await BellChainRuntimeTests.Run(tree);
            await PrologueLevelRuntimeTests.Run(tree);
            await PrologueUiRuntimeTests.Run(tree);
            await PrologueFormalHudTests.Run(tree);
            EnemyFormalVisualTests.Run();
            FallenBellkeeperFormalVisualTests.Run();
            PrologueEnvironmentStructureTests.Run();
            await PrologueEnvironmentRuntimeTests.Run(tree);
            PrologueMechanismVisualTests.Run();
            PresentationCueCoverageTests.Run();
            await PrologueAudioDirectorTests.Run(tree);
            using (var expectedWarnings = new ExpectedWarningLogger())
            {
                await ExecuteLegacyMainCasePlan(tree, residentFirstStartPost);
                // This registry runs the five flow cases, not the migration menu/save
                // fixtures that intentionally emit six Title->Playing rejections.
                expectedWarnings.AssertExact(
                    new ExpectedWarningLogger.Expected(
                        "Rejected prologue flow transition Completed->Recovering.",
                        "scripts/prologue/PrologueGameFlowController.cs", "Reject", 2));
                GD.Print("PROLOGUE_POST_WARNING_SCOPE_OK scope=flow-registry count=2 source=scripts/prologue/PrologueGameFlowController.cs function=Reject");
            }
            PrologueProductionMode.AllowDiagnosticFallbacks = false;
            await PrologueProductionAssetRuntimeTests.Run(tree);
            await PrologueAccessibilityTests.Run(tree);
            SeamlessPrologueMainSceneContractTests.VerifyProcessLifetimeOwner(_postMainSceneKeepAlive);
    }

    internal static readonly string[] Task8ReplacedLegacyCaseNames =
    [
        "P0RoomStructureTests.LevelKeepsPlayerPersistentAndLinksBothRooms",
        "P1LoopStructureTests.FormalLevelLinksEitherBranchAndTheUnchosenReturn",
        "P1LoopRuntimeTests.UpperAndLowerRealExitRoutesBothReachConvergence",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowFailedCastClearsPendingApply",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff",
        "PrologueGuidanceRuntimeTests.ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt",
        "PrologueGuidanceRuntimeTests.ExpandedTeachingAppearsOnlyForAllowedTriggers",
        "PrologueGuidanceRuntimeTests.RecoveryCountsWithinOneRoomAndResetsOnTransition",
        "PrologueGuidanceRuntimeTests.CrossCheckpointRetriesRestoreTheFailedChallengeStreak",
        "PrologueGuidanceRuntimeTests.RealCriticalPathRetargetsThroughShortcutAndBossStates",
        "PrologueGuidanceRuntimeTests.ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels",
        "PrologueGuidanceRuntimeTests.MissingTargetKeepsInstructionAndWarnsOnce",
        "PrologueCompletionTests.RealShortcutAndPlayerFinalHitCompleteOnce",
        "ProloguePlaytestRecorderTests.ExplicitHeadlessFixtureWritesInspectableEvidence",
        "ThreadCastRuntimeTests.RealLevelRoomTransitionClearsTheLockedRoomTarget",
        "ThreadCastRuntimeTests.ActiveClawRoomTransitionsClearImmediatelyWithoutRepeatingGameplay"
    ];

    internal static readonly string[] Task9ReplacedLegacyCaseNames =
    [
        "PrologueGameFlowTests.CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart",
        "PrologueGameFlowTests.ContinueAndCorruptRecovery"
    ];

    // Independent literal authority. This list intentionally does not derive from suite wrappers,
    // counts, or replacement subsets: any ordinary rename/substitution must fail equality.
    internal static readonly string[] AuthoritativeLegacyMainCaseNames =
    [
        "P0RoomStructureTests.CorridorHasLiteralCombatThreadAndCheckpointRoute",
        "P0RoomStructureTests.CourtyardHasLiteralTeachingRoute",
        "P0RoomStructureTests.LevelKeepsPlayerPersistentAndLinksBothRooms",
        "P0RoomStructureTests.RequiredPackedScenesExist",
        "P1LoopRuntimeTests.HiddenDiscoveryPersistsAcrossDeathRecovery",
        "P1LoopRuntimeTests.PartialLowerEncounterResetsBothActors",
        "P1LoopRuntimeTests.UpperAndLowerRealExitRoutesBothReachConvergence",
        "P1LoopStructureTests.FormalLevelLinksEitherBranchAndTheUnchosenReturn",
        "P1LoopStructureTests.GeneratedEnemiesKeepAttackReadabilityContracts",
        "P1LoopStructureTests.LowerRouteHasMixedEncounterContracts",
        "P1LoopStructureTests.MechanismStubConvergesBothBranchesSafely",
        "P1LoopStructureTests.NewPackedRoomsExist",
        "P1LoopStructureTests.UpperRouteHasVerticalThreadAndHiddenContracts",
        "PlayerFormalVisualTests.ActionOverRunKeepsLegsAndSecondarySlotsContinuous",
        "PlayerFormalVisualTests.AirAndDownAttacksExposeFourDistinctRigStages",
        "PlayerFormalVisualTests.CompatibilityAdaptersRemainVisible",
        "PlayerFormalVisualTests.FacingLocalSecondaryMotionMirrorsWorldExactly",
        "PlayerFormalVisualTests.LoopWrapKeepsEveryLocomotionSlotContinuous",
        "PlayerFormalVisualTests.RealAttackSignalsKeepTypedAnticipationAuthoritative",
        "PlayerFormalVisualTests.RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder",
        "PlayerFormalVisualTests.RealCheckpointRecoveryClearsAttackPresentationLock",
        "PlayerFormalVisualTests.RealCompletedAttackIdDoesNotRelatchPresentation",
        "PlayerFormalVisualTests.RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode",
        "PlayerFormalVisualTests.RealCoordinatorKeepsFormalThreadCastThroughEveryPhase",
        "PlayerFormalVisualTests.RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase",
        "PlayerFormalVisualTests.RealCoordinatorSustainsFormalAndPlaceholderFailedPoses",
        "PlayerFormalVisualTests.RealInputAttackUpdatesFormalFacingToMatchHitbox",
        "PlayerFormalVisualTests.RealLethalDamageClearsAttackAcrossRecoveryLifecycle",
        "PlayerFormalVisualTests.RealMechanicalClawAimUpdatesFormalFacingToMatchCast",
        "PlayerFormalVisualTests.SetupAndBasicContract",
        "PlayerFormalVisualTests.SixtyFpsFramesKeepEverySlotBounded",
        "PlayerFormalVisualTests.TaskOneSecondaryOutputsReachTheirOwnedFormalSlots",
        "PlayerFormalVisualTests.ThreadAimUsesFacingLocalMirroredRig",
        "PlayerFormalVisualTests.ThreadTerminalPoseReturnsContinuouslyToIdle",
        "PlayerFormalVisualTests.ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers",
        "PlayerFormalVisualTests.TypedActionPhaseBoundaryBlendsWithoutHardCut",
        "PrologueCompletionTests.PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit",
        "PrologueCompletionTests.RealShortcutAndPlayerFinalHitCompleteOnce",
        "PrologueGameFlowTests.CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart",
        "PrologueGameFlowTests.CompletedSaveStartsCleanAndPreservesBestMetrics",
        "PrologueGameFlowTests.ContinueAndCorruptRecovery",
        "PrologueGameFlowTests.CorruptSaveRecoveryStartsClean",
        "PrologueGameFlowTests.TitleNewSettingsCompletionAndInvalidRecovery",
        "PrologueGuidanceRuntimeTests.CrossCheckpointRetriesRestoreTheFailedChallengeStreak",
        "PrologueGuidanceRuntimeTests.DisablingGuidanceClearsWithoutTouchingCompletionOrProgress",
        "PrologueGuidanceRuntimeTests.ExistingUpperAnchorLowCompletionSuppressesFirstUsePrompt",
        "PrologueGuidanceRuntimeTests.ExpandedTeachingAppearsOnlyForAllowedTriggers",
        "PrologueGuidanceRuntimeTests.MissingTargetKeepsInstructionAndWarnsOnce",
        "PrologueGuidanceRuntimeTests.MountedGuidanceDoesNotOverlapBossHud",
        "PrologueGuidanceRuntimeTests.MountedGuidancePassesPointerInputThroughEveryOverlayControl",
        "PrologueGuidanceRuntimeTests.MountedGuidanceRendersAndClearsWithoutHidingObjective",
        "PrologueGuidanceRuntimeTests.RealCriticalPathRetargetsThroughShortcutAndBossStates",
        "PrologueGuidanceRuntimeTests.RecoveryCountsWithinOneRoomAndResetsOnTransition",
        "PrologueGuidanceRuntimeTests.ShortcutRetargetsOnTheOpeningSignalAcrossFreshLevels",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowFailedCastClearsPendingApply",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowRealApplyAndLandingCompletesEvenWhenGuidanceIsOff",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowRecoveryAndRoomLeaveClearPendingApply",
        "PrologueGuidanceRuntimeTests.UpperAnchorLowWrongTargetAndAirborneCrossingDoNotComplete",
        "ProloguePlaytestRecorderTests.ExplicitHeadlessFixtureWritesInspectableEvidence",
        "ProloguePlaytestRecorderTests.HeadlessAutomationDoesNotCreateHumanEvidence",
        "PrologueVfxRuntimeTests.BasicTypedVfxRuntimeContract",
        "PrologueVfxRuntimeTests.VerifyRuntimeFrameBinding",
        "ThreadCastRuntimeTests.ActiveClawRoomTransitionsClearImmediatelyWithoutRepeatingGameplay",
        "ThreadCastRuntimeTests.CooldownHoldNeverEntersPreviewOrAutoFires",
        "ThreadCastRuntimeTests.DeadLockedTargetStillInSceneFailsBeforeApply",
        "ThreadCastRuntimeTests.EveryRuntimeCancellationSourceClearsTransientPresentation",
        "ThreadCastRuntimeTests.LockedTargetRevalidationCoversGroupQueueRangeAndInsertedLos",
        "ThreadCastRuntimeTests.NeutralTapUsesLiveFacingButNeutralHoldSelectsNothing",
        "ThreadCastRuntimeTests.NonPreviewCancellationSourcesRemainBoundedAndExactlyOnce",
        "ThreadCastRuntimeTests.PhysicalReleaseAtTapThresholdDoesNotAccumulateAGapSample",
        "ThreadCastRuntimeTests.PreviewTravelLatchApplyAndRecoveryUseRealPhysicsFrames",
        "ThreadCastRuntimeTests.RealLevelRoomTransitionClearsTheLockedRoomTarget",
        "ThreadCastRuntimeTests.RealWorldObstructionUsesItsRayHitAndNamedFailuresRemainVisible",
        "ThreadCastRuntimeTests.RecoveryMovementAndJumpCancelOnlyPresentation",
        "ThreadCastRuntimeTests.TargetFreedDuringTravelFailsWithoutPullOrStaleReference"
    ];

    internal static List<Task7Case> BuildLegacyMainCases(SceneTree tree, string flowRoot)
    {
        var cases = new List<Task7Case>();
        cases.AddRange(P0RoomStructureTests.Task7Cases());
        cases.AddRange(P1LoopStructureTests.Task7Cases());
        cases.AddRange(P1LoopRuntimeTests.Task7Cases(tree));
        cases.AddRange(PrologueGuidanceRuntimeTests.Task7Cases(tree));
        cases.AddRange(PrologueCompletionTests.Task7Cases(tree));
        cases.AddRange(ProloguePlaytestRecorderTests.Task7Cases(tree));
        cases.AddRange(ThreadCastRuntimeTests.Task7Cases(tree));
        cases.AddRange(PrologueVfxRuntimeTests.Task7Cases(tree));
        cases.AddRange(PrologueGameFlowTests.Task7Cases(tree, flowRoot));
        cases.AddRange(PlayerFormalVisualTests.Task7Cases(tree));
        return cases;
    }

    internal static void AssertLegacyMainClassifications(IReadOnlyList<Task7Case> cases)
    {
        string expectedNames = string.Join("|", AuthoritativeLegacyMainCaseNames
            .OrderBy(name => name, StringComparer.Ordinal));
        string actualNames = string.Join("|", cases.Select(item => item.Name)
            .OrderBy(name => name, StringComparer.Ordinal));
        TestAssert.Equal(expectedNames, actualNames,
            "authoritative registry equals the independent literal 75-name set");
        TestAssert.Equal(75, cases.Count,
            "authoritative legacy registry retains the exact 75 classified cases");
        TestAssert.Equal(75, cases.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count(),
            "authoritative legacy registry contains no duplicate names");
        TestAssert.Equal(0, cases.Count(item => item.Disposition == Task7CaseDisposition.Unclassified),
            "authoritative legacy registry contains no unclassified cases");
        string actualTask8 = string.Join("|", cases
            .Where(item => Task8ReplacedLegacyCaseNames.Contains(item.Name, StringComparer.Ordinal))
            .Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal));
        string expectedTask8 = string.Join("|", Task8ReplacedLegacyCaseNames
            .OrderBy(name => name, StringComparer.Ordinal));
        TestAssert.Equal(expectedTask8, actualTask8,
            "the authoritative registry contains the exact eighteen original Task8 case names");
        foreach (Task7Case item in cases.Where(item =>
                     Task8ReplacedLegacyCaseNames.Contains(item.Name, StringComparer.Ordinal)))
            TestAssert.True(item.Disposition == Task7CaseDisposition.Executable && item.Execute != null,
                $"Task8 replacement is independently executable under its original name: {item.Name}");

        string actualTask9 = string.Join("|", cases
            .Where(item => Task9ReplacedLegacyCaseNames.Contains(item.Name,
                StringComparer.Ordinal) && item.Disposition == Task7CaseDisposition.Executable)
            .Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal));
        string expectedTask9 = string.Join("|", Task9ReplacedLegacyCaseNames
            .OrderBy(name => name, StringComparer.Ordinal));
        TestAssert.Equal(expectedTask9, actualTask9,
            "the exact two Task9 save cases are executable delegates");
        TestAssert.Equal(0, cases.Count(item => item.Disposition == Task7CaseDisposition.Retired),
            "any remaining or newly introduced retirement fails closed");
        TestAssert.Equal(75, cases.Count(item => item.Disposition == Task7CaseDisposition.Executable),
            "every authoritative registry case is executable");
    }

    internal static async Task ExecuteLegacyMainCasePlan(SceneTree tree, bool residentFirstStartPost = false)
    {
        string flowRoot = CreateLegacyMainFlowRoot();
        List<Task7Case> cases = BuildLegacyMainCases(tree, flowRoot);
        AssertLegacyMainClassifications(cases);
        if (residentFirstStartPost)
            TestAssert.Equal(ResidentFirstPostCaseName, cases[ResidentFirstPostCaseIndex].Name,
                "Resident first-start post diagnostic requires the exact original case #18");

        var names = new HashSet<string>(cases.Select(item => item.Name), StringComparer.Ordinal);
        TestAssert.True(!names.Contains("ThreadCastRuntimeTests.TapRight"),
            "legacy case accounting excludes the TapRight helper");
        TestAssert.True(names.Contains("PrologueVfxRuntimeTests.BasicTypedVfxRuntimeContract") &&
            names.Contains("PrologueVfxRuntimeTests.VerifyRuntimeFrameBinding"),
            "VFX wrapper basic contract is an explicit executable case");
        TestAssert.True(names.Contains("PlayerFormalVisualTests.SetupAndBasicContract") &&
            names.Contains("PlayerFormalVisualTests.TypedActionPhaseBoundaryBlendsWithoutHardCut") &&
            names.Contains("PlayerFormalVisualTests.AirAndDownAttacksExposeFourDistinctRigStages") &&
            names.Contains("PlayerFormalVisualTests.ThreadTerminalPoseReturnsContinuouslyToIdle") &&
            names.Contains("PlayerFormalVisualTests.ThreadAimUsesFacingLocalMirroredRig") &&
            names.Contains("PlayerFormalVisualTests.ActionOverRunKeepsLegsAndSecondarySlotsContinuous") &&
            names.Contains("PlayerFormalVisualTests.LoopWrapKeepsEveryLocomotionSlotContinuous") &&
            names.Contains("PlayerFormalVisualTests.TaskOneSecondaryOutputsReachTheirOwnedFormalSlots") &&
            names.Contains("PlayerFormalVisualTests.CompatibilityAdaptersRemainVisible") &&
            names.Contains("PlayerFormalVisualTests.SixtyFpsFramesKeepEverySlotBounded"),
            "formal player setup/basic and nine fixture contracts are explicit executable cases");
        TestAssert.Equal(18, cases.Count(item =>
                Task8ReplacedLegacyCaseNames.Contains(item.Name, StringComparer.Ordinal) &&
                item.Disposition == Task7CaseDisposition.Executable),
            "Task 8 retains all eighteen executable layout-one canyon geometry compatibility contracts; formal V2 is validated separately");
        Task7CaseRun run;
        ulong boundaryStart = Time.GetTicksMsec();
        int boundaryIndex = 0;
        CanyonResidentManager.ResidentFirstInstantiateTrace residentTrace = null;
        bool residentScopeWritten = true, residentScopeComplete = false;
        int residentStartupCommits = 0;
        try
        {
            run = await Task7CaseRegistry.Execute(cases, (name, entered) =>
            {
                ulong engineMs = Time.GetTicksMsec();
                ulong physics = Engine.GetPhysicsFrames(), process = Engine.GetProcessFrames();
                GD.Print($"TASK7_CASE_BOUNDARY index={boundaryIndex} name={name} stage={(entered ? "enter" : "return")} " +
                    $"physics={physics} process={process} engine_ms={engineMs} elapsed_ms={engineMs - boundaryStart}");
                if (residentFirstStartPost && boundaryIndex == ResidentFirstPostCaseIndex)
                {
                    if (entered)
                    {
                        residentScopeWritten &= WriteResidentFirstPostScope("enter", physics, process, engineMs);
                        residentTrace = new("full-post");
                        ResidentFirstPostStartupCommitted = () =>
                        {
                            residentStartupCommits++;
                            try
                            {
                                Console.Error.WriteLine("RESIDENT_FIRST_POST_STAGE arm=full-post stage=startup-committed " +
                                    $"physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} engine_ms={Time.GetTicksMsec()}");
                            }
                            catch (Exception) { residentScopeWritten = false; }
                        };
                    }
                    else
                    {
                        ResidentFirstPostStartupCommitted = null;
                        residentScopeComplete = residentTrace?.Complete == true;
                        residentTrace?.Dispose();
                        residentTrace = null;
                        residentScopeWritten &= WriteResidentFirstPostScope("return", physics, process, engineMs);
                    }
                }
                if (!entered) boundaryIndex++;
            });
        }
        finally
        {
            if (residentTrace != null)
            {
                ResidentFirstPostStartupCommitted = null;
                residentTrace.Dispose();
                // Do not let diagnostic logging replace the original case exception.
                WriteResidentFirstPostScope("fault", Engine.GetPhysicsFrames(), Engine.GetProcessFrames(), Time.GetTicksMsec());
            }
            tree.Paused = false;
        }
        if (residentFirstStartPost && (!residentScopeComplete || !residentScopeWritten || residentStartupCommits != 1))
            GD.PushError("Resident first-start post diagnostic scope or startup boundary was absent, duplicated, exceeded its bound, or failed to write");
        TestAssert.Equal(string.Join("|", run.Executable.Order()),
            string.Join("|", run.Started.Order()), "all executable cases start exactly once");
        TestAssert.Equal(string.Join("|", run.Executable.Order()),
            string.Join("|", run.Completed.Order()), "all executable cases complete exactly once");
        string[] vfxRequired =
        [
            "PrologueVfxRuntimeTests.BasicTypedVfxRuntimeContract",
            "PrologueVfxRuntimeTests.VerifyRuntimeFrameBinding"
        ];
        string[] formalFixtureRequired =
        [
            "PlayerFormalVisualTests.SetupAndBasicContract",
            "PlayerFormalVisualTests.TypedActionPhaseBoundaryBlendsWithoutHardCut",
            "PlayerFormalVisualTests.AirAndDownAttacksExposeFourDistinctRigStages",
            "PlayerFormalVisualTests.ThreadTerminalPoseReturnsContinuouslyToIdle",
            "PlayerFormalVisualTests.ThreadAimUsesFacingLocalMirroredRig",
            "PlayerFormalVisualTests.ActionOverRunKeepsLegsAndSecondarySlotsContinuous",
            "PlayerFormalVisualTests.LoopWrapKeepsEveryLocomotionSlotContinuous",
            "PlayerFormalVisualTests.TaskOneSecondaryOutputsReachTheirOwnedFormalSlots",
            "PlayerFormalVisualTests.CompatibilityAdaptersRemainVisible",
            "PlayerFormalVisualTests.SixtyFpsFramesKeepEverySlotBounded"
        ];
        foreach (string name in vfxRequired)
            TestAssert.True(run.Started.Contains(name) && run.Completed.Contains(name),
                $"required VFX case starts and completes: {name}");
        foreach (string name in formalFixtureRequired)
            TestAssert.True(run.Started.Contains(name) && run.Completed.Contains(name),
                $"required formal-player case starts and completes: {name}");
        GD.Print("TASK7_VFX_EXPLICIT_CASES_OK started=2 completed=2");
        GD.Print("TASK7_PLAYER_FORMAL_EXPLICIT_CASES_OK setup_basic=1 fixtures=9 " +
                 "started=10 completed=10 tap_right_registered=False");
        GD.Print("TASK9_REGISTRY_REPLACEMENTS_OK exact=2 executable=True " +
                 "retirements=0 fail_closed=True");
        GD.Print("TASK8_ACTIVE_REPLACEMENT_CASES_OK original_names=18 self_contained=True");
        GD.Print($"TASK7_LEGACY_CASE_PLAN_OK suites=10 executed={run.Executable.Count} " +
                 $"retired={run.Retired.Count} classified={run.All.Count} fail_closed=True");
    }

    internal static string CreateLegacyMainFlowRoot()
        => IsolatedPrologueFixture.CreatePaths("legacy-main-flow-registry").CaseRoot;

    private static bool WriteResidentFirstPostScope(string stage, ulong physics, ulong process, ulong engineMs)
    {
        try
        {
            Console.Error.WriteLine($"RESIDENT_FIRST_POST_SCOPE arm=full-post index={ResidentFirstPostCaseIndex} " +
                $"stage={stage} name={ResidentFirstPostCaseName} physics={physics} process={process} engine_ms={engineMs}");
            return true;
        }
        catch (Exception) { return false; }
    }
}

// Scalar-only timing; deliberately no SceneTree/Node/Resource or execution
// delegate references. A Return is recorded only after its real statement.
internal sealed class PreFixtureTimingJournal
{
    private int _next;
    private string _active;
    private ulong _enterMs, _enterPhysics, _enterProcess, _lastMs, _lastPhysics, _lastProcess;
    private bool _closed;
    public PreFixtureTimingJournal() { }

    public string Observe(int index, string name, bool entered, ulong ms, ulong physics, ulong process)
    {
        if (_closed || index != _next || index < 0) throw new InvalidOperationException("Pre timing boundary order/closed plan mismatch.");
        if (string.IsNullOrEmpty(name) || name.Length > 256 || name.Any(character =>
            !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or '-')))
            throw new ArgumentException("Pre timing identity must be a single fixed ASCII token.", nameof(name));
        if (ms < _lastMs || physics < _lastPhysics || process < _lastProcess)
            throw new InvalidOperationException("Pre timing clocks cannot roll backwards.");
        if (entered ? _active != null : _active != name)
            throw new InvalidOperationException("Pre timing needs a single matching entry/return.");
        ulong elapsed = entered ? 0 : ms - _enterMs;
        ulong physicsDelta = entered ? 0 : physics - _enterPhysics;
        ulong processDelta = entered ? 0 : process - _enterProcess;
        if (entered) { _active = name; _enterMs = ms; _enterPhysics = physics; _enterProcess = process; }
        else { _active = null; _next++; }
        _lastMs = ms; _lastPhysics = physics; _lastProcess = process;
        return FormattableString.Invariant($"PROLOGUE_PRE_CASE_BOUNDARY index={index} name={name} stage={(entered ? "enter" : "return")} engine_ms={ms} physics={physics} process={process} elapsed_ms={elapsed} physics_delta={physicsDelta} process_delta={processDelta}");
    }

    public string Complete(int expectedCount)
    {
        if (_closed || expectedCount <= 0 || _active != null || _next != expectedCount)
            throw new InvalidOperationException("Pre timing cannot complete a missing/pending/closed plan.");
        _closed = true;
        return FormattableString.Invariant($"PROLOGUE_PRE_CASE_PLAN_OK executed={_next} scalar_only=true resource_fix=false behavior_accepted=false");
    }
}

public static class DisplayBackedFlowAutoStartHarness
{
    private static SceneTree _tree;

    internal static bool IsAttached(SceneTree tree) => ReferenceEquals(_tree, tree);

    public static void Attach(SceneTree tree)
    {
        if (ReferenceEquals(_tree, tree)) return;
        if (_tree != null) throw new System.InvalidOperationException(
            "Display-backed flow auto-start harness is already attached to another SceneTree.");
        _tree = tree;
        _tree.NodeAdded += OnNodeAdded;
    }

    public static void Detach(SceneTree tree)
    {
        if (!ReferenceEquals(_tree, tree)) return;
        _tree.NodeAdded -= OnNodeAdded;
        _tree = null;
    }

    private static void OnNodeAdded(Node node)
    {
        if (DisplayServer.GetName() == "headless") return;
        if (node is PrologueGameFlowController flow && flow.HeadlessAutoStart)
            flow.AutomationAutoStart = true;
    }
}
