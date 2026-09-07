using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Knight.Tests.Integration;
using Knight.Tests.Unit;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Tests;

public partial class RunPrologueTests : SceneTree
{
    private static PackedScene _postMainSceneKeepAlive;

    public override async void _Initialize()
    {
        DisplayBackedFlowAutoStartHarness.Attach(this);
        try
        {
            if (OS.GetCmdlineUserArgs().Contains("--task3-catch-one-only"))
            {
                await PrologueCanyonTraversalTests.RunCatchOneProbe(this);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("task3-catch-one");
                Quit(0);
                return;
            }
            string phase = ResolvePhase();
            if (phase == "pre")
            {
                await RunPrePhase(this);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("pre");
                GD.Print("PROLOGUE_TEST_PHASE_OK phase=pre");
                Quit(0);
                return;
            }
            if (phase == "post")
            {
                await RunPostPhase(this);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("post");
                GD.Print("PROLOGUE_TEST_PHASE_OK phase=post");
                GD.Print("PROLOGUE_TESTS_OK");
                Quit(0);
                return;
            }
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
        finally
        {
            DisplayBackedFlowAutoStartHarness.Detach(this);
        }
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

    private static async Task RunPrePhase(SceneTree tree)
    {
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
            GrappleSurfaceTests.Run();
             await MechanicalClawRuntimeTests.Run(tree);
             await Knight.Tests.Integration.ThreadInteractionTests.Run(tree);
             await CanyonClawMechanismTests.Run(tree);
             await PlayerTraversalInputTests.Run(tree);
            GD.Print("TASK2_TYPED_TERRAIN_TESTS_OK");
            ProductionAssetManifestTests.Run();
            PrologueAudioManifestTests.Run();
            GameSettingsTests.Run();
            PrologueSaveTests.Run();
            InputGlyphResolverTests.Run();
            PrologueStringsTests.Run();
            TuningContractTests.Run();
            PlayerPresentationTests.Run();
            PlayerPoseMixerTests.Run();
            SecondaryMotionModelTests.Run();
            AttackPresentationFrameTests.Run();
            CombatReadabilityExperienceTests.Run();
            GraveLampMothModelTests.Run();
            EncounterDefinitionTests.Run();
            ThreadAbilityModelTests.Run();
            ThreadCastModelTests.Run();
            ThreadExperienceRuntimeTests.Run();
            Knight.Tests.Unit.ThreadInteractionTests.Run();
            ArmoredHitAccumulatorTests.Run();
            BellBackLaborerModelTests.Run();
            FallenBellkeeperModelTests.Run();
            DamageSourceIdentityRuntimeTests.Run();
            await PlayerShieldRuntimeTests.Run(tree);
            await PlayerActionModeRuntimeTests.Run(tree);
            PrologueObjectiveTests.Run();
            PrologueGuidanceModelTests.Run();
            await PrologueGuidanceTargetResolverTests.Run(tree);
            await PrologueTraversalRouteTests.Run(tree);
    }

    private static async Task RunPostPhase(SceneTree tree)
    {
            PrologueProductionMode.AllowDiagnosticFallbacks = true;
            _postMainSceneKeepAlive = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn") ??
                throw new InvalidOperationException("Task 7 main prologue scene is missing.");
            PrologueCanyonStructureTests.Run(_postMainSceneKeepAlive);
            CanyonMaterialSkinTests.Run(fromBuilder: false);
            await PrologueCanyonTraversalTests.Run(tree);
            ProloguePlaytestSessionTests.Run();
            ProloguePlaytestEvidenceGrantTests.Run();
            await ProloguePlaytestRecorderTests.RunTask5FallContextContracts(tree);
            PrologueSectorStreamModelTests.Run();
            await SeamlessPrologueWorldTests.Run(tree);
            SeamlessPrologueMainSceneContractTests.Run(_postMainSceneKeepAlive);
            PrologueRouteGraphTests.Run();
            PrologueProgressTests.Run();
            PrologueRoomContractTests.Run();
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
            await ExecuteLegacyMainCasePlan(tree);
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

    internal static async Task ExecuteLegacyMainCasePlan(SceneTree tree)
    {
        string flowRoot = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "knight-task7-case-plan-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(flowRoot);
        List<Task7Case> cases = BuildLegacyMainCases(tree, flowRoot);
        AssertLegacyMainClassifications(cases);

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
            "Task 8 replaces all eighteen original names with active canyon contracts");
        Task7CaseRun run;
        try
        {
            run = await Task7CaseRegistry.Execute(cases);
        }
        finally
        {
            tree.Paused = false;
            if (System.IO.Directory.Exists(flowRoot))
                System.IO.Directory.Delete(flowRoot, recursive: true);
        }
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
}

public static class DisplayBackedFlowAutoStartHarness
{
    private static SceneTree _tree;

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
