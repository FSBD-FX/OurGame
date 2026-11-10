using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

// Synthetic terrain with the saved actor and real engine/input route. Not OS/human route evidence.
public partial class RunStableClawPullTests : SceneTree, IKeyboardInputTraceSink
{
    private string _acceptedStorage;
    private bool _retainInputTrace,_traceAttempted;
    private KeyboardInputTraceBuffer _keyboardTrace;
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Count(arg => arg.StartsWith("--case-storage-root=")) == 1 &&
                args.Count(arg => arg.StartsWith("--phase=")) == 1 &&
                args.All(arg => arg.StartsWith("--case-storage-root=") || arg.StartsWith("--phase=")),
                "stable claw accepts one external storage root and one focused phase");
            _acceptedStorage=IsolatedPrologueFixture.ValidateStorageRoot();
            string phase = args.Single(arg => arg.StartsWith("--phase="))[8..];
            _retainInputTrace=phase is "natural-death" or "ui-natural-death";
            TestAssert.True(phase == "forecourt-mid-route" || phase == "forecourt-column" || phase == "forecourt-ascent" || phase == "forecourt-terrain" || phase == "minimap-grid" || phase == "abyss-explore" || phase == "abyss-branch-b" || phase == "abyss-impact" || phase == "abyss-main" || phase == "abyss-profile" || phase == "abyss-terrain" || phase == "world-profile-authoring" || phase == "world-profile" || phase == "entry-terrain" || phase == "resource-rewards" || phase == "resident-map-preview" || phase is "descent-j-observation-control" or "failure-log-boundary" or "entrance-rock-roles" or "moth-lamp-cues" or "fog-edge-commands" or "recycling-traps" or "formal-hud-layout" or "resident-auto-teardown" or "resident-auto-stream" or "resident-auto-session" or "resident-auto-start" or "resident-development-stage" or "resident-development-published" or "resident-archive-metadata" or "resident-archive-alcoves" or "resident-pendulum-alcove" or "resident-steam-metadata" or "resident-save-compatibility" or "resident-level-lifecycle" or "resident-lifecycle" or "resident-state" or "models" or "departure" or "exits" or "blockers" or
                "keyboard" or "actions" or "boost-presentation" or "traversal-broken-tooth" or "traversal-upper-entry" or "boundary" or "terrain" or "fall" or "ui" or
                "ui-menu" or "ui-pause" or "ui-load" or "ui-sample" or "basin-sample-lifecycle" or "post-boundary-contract" or "vfx-frame-binding-leaf" or "basin-swing-release-trajectory" or "fog-raster" or "fog-perf" or
                "contact-safety" or "contact-blockers" or "fall-claw" or "ceiling-observation" or "basin-first-reel-trajectory" or "basin-first-reel-contract" or
                "level-cache-reuse" or "level-cache-ignore" or "level-cache-ignore-deep" or
                "gravity-delta" or "rope-support" or "swing-loss" or "arrival-transition" or "swing-contact-safety" or "ascent-guidance-model" or "ascent-guidance-runtime" or "ui-ascent-guidance" or "natural-death" or "ui-natural-death" or "cold-continue" or "ui-cold-continue" or "early-death" or "ui-early-death" or
"resident-turret-configuration" or "resident-turret-locked-wall" or "resident-turret-wall-warning" or "resident-turret-void-pending" or "resident-turret-void" or "resident-turret-perception" or "resident-turret-target-death" or "resident-turret-identity" or "resident-ranged-death" or "resident-turret-reentry" or "resident-sentry-death" or "resident-turret-boundaries" or "resident-turret" or "resident-turret-sleep" or "resident-sentry-default" or "resident-family-ghost" or "resident-family-bone-charger" or "resident-family-ghost-sentry" or "resident-family-ghost-air" or "resident-family-ghost-air-boundaries" or "resident-family-bone-air" or "resident-family-bone-wanderer" or "resident-family-bone" or "resident-heavy-boundaries" or "resident-heavy" or "resident-heavy-default" or "resident-air-roles" or "resident-air-boundaries" or "resident-air" or "resident-air-default" or "resident-charger-fixture" or "resident-charger-default" or "resident-charger" or "resident-void-stream" or "resident-void" or "ui-lane-readability" or "ui-lane-saved" or "ui-laser-guidance" or "ui-guide-style" or "checkpoint-save" or "ui-checkpoint-save" or
                "checkpoint-bindings" or "checkpoint-lifecycle" or "checkpoint-sample-continue" or "checkpoint-cold" or
                "fragment-six" or "ui-fragment-six" or "fragment-six-cold" or "completion-save" or "ui-completion-save" or "physical-exit" or
                "completion-flow" or "completion-migration" or "completion-sample" or "basin-clearance" or "basin-claw-target" or
                "descent-guidance" or "ui-descent-guidance" or "first-combat" or "entrance-enclosure" or "ui-entrance-enclosure" or "ordinary-wall-sweep" or "frozen-wall-sweep" or
"basin-supply-mount-only" or "basin-supply-lifetime" or "basin-resource-control" or "basin-resource-claw-prefix" or "basin-resource-pre-prefix" or "updraft" or "ui-updraft" or "canyon-structure-v2" or "shield-lane-keyboard" or "resident-scene-binding" or "resident-subtype-profile" or "resident-allocation" or "resident-steam-alcove" or "resident-gear-left-access" or "resident-gear-alcoves" or "resident-map-census" or "resident-abyss-alcove" or "resident-frozen-alcoves" or "resident-frozen-player" or "resident-parent" or "resident-geometry" or "resident-patrol" or "resident-patrol-range" or "resident-melee-reach" or "resident-patrol-fixture",
                "unknown stable claw phase");
            await ToSignal(this, SignalName.ProcessFrame);
            if (phase == "forecourt-ascent") await FirstGateForecourtRouteTests.RunAscent(this,_acceptedStorage);
            else if (phase == "forecourt-mid-route") await FirstGateForecourtRouteTests.RunAscent(this,_acceptedStorage,true,true);
            else if (phase == "forecourt-column") await FirstGateForecourtRouteTests.RunAscent(this,_acceptedStorage,true);
            else if (phase == "forecourt-terrain") await FirstGateForecourtTerrainTests.Run(this,_acceptedStorage);
            else if (phase == "minimap-grid") await MinimapVersionedGridTests.Run(this,_acceptedStorage);
            else if (phase == "abyss-explore") await FirstGateAbyssExplorationTests.Run(this,_acceptedStorage);
            else if (phase == "abyss-branch-b") await FirstGateAbyssExplorationTests.RunBTransfer(this,FirstGateAbyssTerrainTests.Run(_acceptedStorage));
            else if (phase == "abyss-impact") await AbyssWindLandingTests.Run(this,FirstGateAbyssTerrainTests.Run(_acceptedStorage));
            else if (phase == "abyss-main") await FirstGateAbyssRuntimeTests.Run(this,_acceptedStorage);
            else if (phase == "abyss-profile") FirstGateAbyssProfileTests.Run(_acceptedStorage);
            else if (phase == "abyss-terrain") FirstGateAbyssTerrainTests.Run(_acceptedStorage);
            else if (phase == "world-profile-authoring") {PrologueWorldProfileTests.Run(_acceptedStorage);FirstGateWorldAuthoringTests.Run(_acceptedStorage);}
            else if (phase == "world-profile") {PrologueWorldProfileTests.Run(_acceptedStorage);await FirstGateCampaignRuntimeTests.Run(this,FirstGateWorldAuthoringTests.Run(_acceptedStorage),_acceptedStorage);}
            else if (phase == "entry-terrain") await FirstGateEntryTerrainRuntimeTests.Run(this,FirstGateEntryTerrainTests.Run(_acceptedStorage));
            else if (phase == "resource-rewards") await ResourceRewardRuntimeTests.Run(this,_acceptedStorage);
            else if (phase == "descent-j-observation-control")
            {
                DescentClawGuidanceRuntimeTests.RunObservationReadControl();
                await DescentClawGuidanceRuntimeTests.RunWindowReviewControls(this);
                await DescentClawGuidanceRuntimeTests.Run(this, lateContinuation: true);
            }
            else if (phase == "failure-log-boundary")
                TestAssert.True(false, "STABLE_FAILURE_LOG_SENTINEL");
            else if (phase == "entrance-rock-roles")
            {
                BuildEntranceRockCandidate();
                CanyonMaterialSkinTests.RunEntranceRockRoles();
                await EntranceRecessedSceneryRuntimeTests.Run(this);
            }
            else if (phase == "moth-lamp-cues") AttackTelegraphRuntimeTests.RunMothLampCues();
            else if (phase == "fog-edge-commands") await AttackTelegraphRuntimeTests.RunFogEdgeCommands(this);
            else if (phase == "recycling-traps")
            {
                PlayerMotionModelTests.RunEnvironmentalLaunchRegression();
                PrologueGuidanceModelTests.RunRecyclingTeaching();
                await CanyonMechanismRuntimeTests.RunDirectionalSpringboard(this);
                await CanyonMechanismRuntimeTests.RunSpringboardControls(this);
                await CanyonMechanismRuntimeTests.RunFixedGearContact(this);
                await CanyonMechanismRuntimeTests.RunRecyclingCombo(this);
                await CanyonMechanismRuntimeTests.RunGeneratedRecyclingPassage(this);
                await CanyonMechanismRuntimeTests.RunRecyclingFogCommands(this);
                CanyonMechanismRuntimeTests.RunSavedRecyclingContract();
                await PrologueGuidanceRuntimeTests.RunCurrentMountedGuidanceOnly(this);
                await PrologueGuidanceRuntimeTests.AssertMountedGuidanceFailureDisposesFixture(this);
            }
            else if (phase is "completion-save" or "ui-completion-save") await NormalWorldPhysicalExitTests.RunCompletionSaveFailure(this);
            else if (phase == "completion-flow")
            {
                using var warnings = new ExpectedWarningLogger();
                await PrologueGameFlowTests.RunTask7IndependentCases(this);
                warnings.AssertExact(new ExpectedWarningLogger.Expected(
                    "Rejected prologue flow transition Completed->Recovering.",
                    "scripts/prologue/PrologueGameFlowController.cs", "Reject", 2));
            }
            else if (phase == "completion-migration")
            {
                using var warnings = new ExpectedWarningLogger();
                PrologueSaveTests.Run();
                ContinuousCanyonMigrationTests.RunStore();
                await ContinuousCanyonMigrationTests.RunMenus(this);
                await ContinuousCanyonSaveIsolationTests.Run(this);
                warnings.AssertExact(ExpectedWarningLogger.GameFlowNegativeExpectations());
            }
            else if (phase == "completion-sample") await BasinSampleBoundaryTests.Run(this, "exit");
            else if (phase == "physical-exit") await NormalWorldPhysicalExitTests.RunPhysicalExitRegression(this);
            else if (phase is "fragment-six" or "ui-fragment-six") await FragmentSixAdmissionRuntimeTests.Run(this);
            else if (phase == "fragment-six-cold") await FragmentSixAdmissionRuntimeTests.RunCold(this);
            else if (phase == "checkpoint-cold") await CheckpointPersistenceRuntimeTests.RunColdRetry(this);
            else if (phase == "checkpoint-bindings") await SeamlessPrologueWorldTests.RunFixRound5(this);
            else if (phase == "checkpoint-lifecycle") await SeamlessPrologueWorldTests.RunCheckpointCompatibility(this);
            else if (phase == "checkpoint-sample-continue") await BasinSampleBoundaryTests.Run(this, "continue");
            else if (phase is "checkpoint-save" or "ui-checkpoint-save") await CheckpointPersistenceRuntimeTests.Run(this);
            else if (phase == "formal-hud-layout")
            {
                await PrologueFormalHudTests.Run(this);
                GD.Print("FORMAL_HUD_FOCUSED_OK original_formal_hud_class=true headless_geometry=true human=false whole_post=false");
            }
            else if (phase == "ui-lane-readability") await FogShieldLaneReadabilityRuntimeTests.Run(this);
            else if (phase == "ui-laser-guidance") await VisibleLaserGuidanceTests.Run(this);
            else if (phase == "ui-guide-style")
            {
                await PrologueGuidanceRuntimeTests.RunStableBorderlessPresentation(this);
                await PrologueAudioDirectorTests.DamagedGuidanceCrackleIsShortIsolatedAndRateLimited(this);
            }
            else if (phase == "ui-lane-saved")
            {
                PrologueGuidanceModelTests.RunShieldLaneGuidance();
                await FogShieldLaneSavedControllerRuntimeTests.Run(this);
            }
            else if (phase is "early-death" or "ui-early-death") await PrologueCanyonGeneratedMainRouteTests.RunBeforeCheckpointDeath(this);
            else if (phase is "cold-continue" or "ui-cold-continue") await PrologueCanyonGeneratedMainRouteTests.RunColdContinue(this);
            else if (phase is "natural-death" or "ui-natural-death") await PrologueCanyonGeneratedMainRouteTests.RunNaturalHazardRecovery(this);
            else if (phase == "updraft") await CanyonUpdraftRuntimeTests.Run(this);
            else if (phase == "ui-updraft") await CanyonUpdraftRuntimeTests.RunPresentation(this);
            else if (phase == "models")
            {
                FirstGateCampaignPlanTests.Run();
                ResourceRewardTests.Run();
                PrologueCanyonGeneratedMainRouteTests.RunGroundSeamPhysicsControls();
                PrologueCanyonGeneratedMainRouteTests.RunGroundSeamHandoffControls();
                MechanicalClawModelTests.Run();
                MechanicalClawArrivalGeometryTests.Run();
            }
            else if (phase == "ascent-guidance-model") PrologueGuidanceModelTests.RunAscentTransferGuidance();
            else if (phase is "ascent-guidance-runtime" or "ui-ascent-guidance")
                await KeyboardTeachingRuntimeTests.RunAscentTransferGuidance(this);
            else if (phase == "ui")
            {
                PrologueGuidanceModelTests.Run();
                PrologueGuidanceModelTests.RunTempleTerrainGuidance();
                await PrologueUiRuntimeTests.Run(this);
            }
            else if (phase == "ui-menu") await PrologueUiRuntimeTests.ControlsReferenceExplainsTheLiveClawAndFits(this);
            else if (phase == "ui-pause") await PrologueUiRuntimeTests.PauseReferenceFitsItsAuthoredPanel(this);
            else if (phase == "ui-load") LoadLevelForResourceDiagnostic();
            else if (phase == "canyon-structure-v2") RunSavedCanyonStructure();
            else if (phase == "resident-scene-binding") await ResidentSceneBindingTests.Run(this, _acceptedStorage);
            else if (phase == "resident-subtype-profile") ResidentSubtypeProfileTests.Run(_acceptedStorage);
            else if (phase == "resident-state")
            {
                ResidentPopulationStateTests.Run();
                HealthComponentTests.Run();
                GD.Print("RESIDENT_HEALTH_LEGACY_OK cases=3 damage_identity_invulnerability_death=true");
            }
            else if (phase == "resident-lifecycle") await ResidentLifecycleRuntimeTests.Run(this);
            else if (phase == "resident-level-lifecycle")
            {
                ResidentSavedBodyGeometryTests.Run(_acceptedStorage);
                await ResidentLevelLifecycleRuntimeTests.Run(this);
                await ResidentSpawnRuntimeTests.Run(this, _acceptedStorage);
            }
            else if (phase == "resident-save-compatibility")
            {
                QuestFragmentProgressTests.Run();
                BossAdmissionModelTests.Run();
                await QuestFragmentPersistenceRuntimeTests.Run(this);
                ExplorationMapSaveTests.Run();
                GD.Print("RESIDENT_MAP_REVISION_COMPATIBILITY_OK optional_map_matrix=true core_preserved=true actual_old127_to143_continue=false");
                GD.Print("RESIDENT_SAVE_COMPATIBILITY_OK original_progress_model=true original_boss_model=true original_persistence=true contact_pickup=false normal_route=false");
            }
            else if (phase == "resident-allocation") { CanyonResidentCatalogTests.Run(); CanyonResidentCatalogTests.RunAuthoredLayout(); CanyonResidentPlacementTests.RunJointCodecContracts(); }
            else if (phase == "resident-geometry") await CanyonResidentPlacementTests.Run(this, _acceptedStorage);
            else if (phase == "resident-archive-alcoves") await CanyonResidentPlacementTests.RunArchiveCandidate(this, _acceptedStorage);
            else if (phase == "resident-pendulum-alcove") await CanyonResidentPlacementTests.RunPendulumCandidate(this, _acceptedStorage);
            else if (phase == "resident-steam-metadata") SteamMetadataSynchronizationTests.Run(_acceptedStorage);
            else if (phase == "resident-archive-metadata") SteamMetadataSynchronizationTests.RunArchive(_acceptedStorage);
            else if (phase == "resident-steam-alcove") await CanyonResidentPlacementTests.RunSteamCandidate(this, _acceptedStorage);
            else if (phase == "resident-gear-left-access") await CanyonResidentPlacementTests.RunGearLeftAccess(this, _acceptedStorage);
            else if (phase == "resident-gear-alcoves") await CanyonResidentPlacementTests.RunGearCandidate(this, _acceptedStorage);
            else if (phase == "resident-map-census") await CanyonResidentPlacementTests.RunGearMapCensus(this, _acceptedStorage);
            else if (phase == "resident-abyss-alcove") await CanyonResidentPlacementTests.RunAbyssCandidate(this, _acceptedStorage);
            else if (phase == "resident-frozen-alcoves") await CanyonResidentPlacementTests.RunFrozenCandidate(this, _acceptedStorage);
            else if (phase == "resident-frozen-player") await CanyonResidentPlacementTests.RunFrozenPlayerRoute(this, _acceptedStorage);
            else if (phase == "resident-auto-start") await ResidentAutomaticSpawnTests.RunStart(this);
            else if (phase == "resident-auto-session") await ResidentAutomaticSpawnTests.RunSession(this);
            else if (phase == "resident-auto-stream") await ResidentAutomaticSpawnTests.RunStreaming(this);
            else if (phase == "resident-auto-teardown") await ResidentAutomaticSpawnTests.RunTeardown(this);
            else if (phase == "resident-development-stage") ResidentDevelopmentPackageTests.RunStage(_acceptedStorage);
            else if (phase == "resident-development-published") await ResidentDevelopmentPackageTests.RunPublished(this);
            else if (phase == "resident-map-preview") CurrentPrologueMapExportTests.Run(_acceptedStorage);
            else if (phase == "resident-parent")
            {
                ResidentTerrainSynchronizationTests.Run(_acceptedStorage);
                await CanyonResidentPlacementTests.RunCandidateParent(this, _acceptedStorage);
                await MinimapCatalogTests.Run(this);
            }
            else if (phase == "resident-patrol-range") await ResidentPatrolRangeRuntimeTests.Run(this);
            else if (phase == "resident-melee-reach") await ResidentPatrolRangeRuntimeTests.RunMeleeReach(this);
            else if (phase == "resident-patrol-fixture") await ResidentPatrolRuntimeTests.RunFixtureOwnership(this);
            else if (phase == "resident-void") await ResidentVoidRuntimeTests.Run(this);
            else if (phase == "resident-void-stream") await ResidentVoidStreamRuntimeTests.Run(this);
            else if (phase == "resident-charger") await ResidentChargerRuntimeTests.Run(this);
            else if (phase == "resident-air") await ResidentAirRuntimeTests.Run(this);
            else if (phase == "resident-air-roles") await ResidentAirRuntimeTests.RunRoles(this);
            else if (phase == "resident-turret") await ResidentTurretRuntimeTests.Run(this);
            else if (phase == "resident-turret-identity") await ResidentTurretRuntimeTests.RunIdentity(this);
            else if (phase == "resident-turret-perception") await ResidentTurretRuntimeTests.RunPerception(this);
            else if (phase == "resident-turret-configuration") await ResidentTurretRuntimeTests.RunConfiguration(this);
            else if (phase == "resident-turret-locked-wall") await ResidentTurretRuntimeTests.RunLockedWall(this);
            else if (phase == "resident-turret-wall-warning") await ResidentTurretRuntimeTests.RunWallWarning(this);
            else if (phase == "resident-turret-target-death") await ResidentTurretRuntimeTests.RunTargetDeath(this);
            else if (phase == "resident-turret-void") await ResidentTurretRuntimeTests.RunVoid(this);
            else if (phase == "resident-turret-void-pending") await ResidentTurretRuntimeTests.RunVoidPending(this);
            else if (phase == "resident-turret-boundaries") await ResidentTurretRuntimeTests.RunBoundaries(this);
            else if (phase == "resident-turret-reentry") await ResidentTurretRuntimeTests.RunReentry(this);
            else if (phase == "resident-sentry-death") await ResidentTurretRuntimeTests.RunSentryDeath(this);
            else if (phase == "resident-ranged-death") await ResidentTurretRuntimeTests.RunRangedDeath(this);
            else if (phase == "resident-turret-sleep") await ResidentTurretRuntimeTests.RunSleep(this);
            else if (phase == "resident-sentry-default")
            {
                BellBoltSentryModelTests.Run();
                await BellBoltSentryRuntimeTests.Run(this);
                GD.Print("RESIDENT_SENTRY_DEFAULT_RESULT saved_actor=true full_existing_aggregate=true configured=false formal_spawned=0");
            }
            else if (phase == "resident-air-boundaries") await ResidentAirRuntimeTests.RunBoundaries(this);
            else if (phase == "resident-heavy") await ResidentHeavyRuntimeTests.Run(this);
            else if (phase == "resident-family-bone") await ResidentHeavyRuntimeTests.RunBone(this);
            else if (phase == "resident-family-bone-wanderer") await ResidentPatrolRuntimeTests.RunBoneWanderer(this);
            else if (phase == "resident-family-bone-charger") await ResidentChargerRuntimeTests.RunBone(this);
            else if (phase == "resident-family-ghost-air") await ResidentAirRuntimeTests.RunGhost(this);
            else if (phase == "resident-family-ghost-sentry") await ResidentTurretRuntimeTests.RunGhostSentry(this);
            else if (phase == "resident-family-ghost-air-boundaries") await ResidentAirRuntimeTests.RunGhost(this,true);
            else if (phase == "resident-family-bone-air") await ResidentAirRuntimeTests.RunBone(this);
            else if (phase == "resident-family-ghost") await ResidentPatrolRuntimeTests.RunGhost(this);
            else if (phase == "resident-heavy-boundaries") await ResidentHeavyRuntimeTests.RunBoundaries(this);
            else if (phase == "resident-heavy-default")
            {
                BellBackLaborerModelTests.Run();await BellBackLaborerSightRuntimeTests.Run(this);await BellChainRuntimeTests.RunLaborerCompatibility(this);
                GD.Print("RESIDENT_HEAVY_DEFAULT_RESULT saved_actor=true full_existing_aggregate=true configured=false formal_spawned=0");
            }
            else if (phase == "resident-air-default")
            {
                GraveLampMothModelTests.Run();await GraveLampMothRuntimeTests.Run(this);
                GD.Print("RESIDENT_AIR_DEFAULT_RESULT saved_actor=true full_existing_aggregate=true configured=false formal_spawned=0");
            }
            else if (phase == "resident-charger-fixture") await ResidentChargerRuntimeTests.RunDefaultFixture(this);
            else if (phase == "resident-charger-default")
            {
                CanyonSkitterModelTests.Run();
                await CanyonSkitterRuntimeTests.Run(this);
                GD.Print("RESIDENT_CHARGER_DEFAULT_RESULT saved_actor=true full_existing_aggregate=true configured=false formal_spawned=0");
            }
            else if (phase == "resident-patrol")
            {
                var failures = ResidentPatrolModelTests.Run().ToList();
                failures.AddRange(await ResidentPatrolRuntimeTests.Run(this));
                TestAssert.True(failures.Count == 0, string.Join(" | ",failures));
                RustShellModelTests.Run();
                await RustShellSightRuntimeTests.Run(this);
                GD.Print("RESIDENT_PATROL_OK model_cases=7 native_cases=15 default_wave_preserved=true formal_spawned=0");
            }
            else if (phase == "shield-lane-keyboard") await PrologueCanyonTraversalTests.RunShieldLaneOnly(this);
            else if (phase == "post-boundary-contract") await Task7CaseRegistryTests.Run(this);
            else if (phase == "vfx-frame-binding-leaf") await RunVfxFrameBindingLeaf();
            else if (phase == "basin-sample-lifecycle")
            {
                await BasinQualitySampleEntryTests.RunProductionLifecycle(this);
                GD.Print("BASIN_SAMPLE_LIFECYCLE_OK actual_entry=true legacy_layout=1 start_death_continue_newgame=true full_main_v2=false human=false");
            }
            else if (phase == "ui-sample") await KeyboardTeachingRuntimeTests.RunSampleGuide(this,
                IsolatedPrologueFixture.CreatePaths("claw-stage-guide").CaseRoot);
            else if (phase is "fog-raster" or "fog-perf")
            {
                TestAssert.True(DisplayServer.GetName() != "headless" &&
                    RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility",
                    "fog raster requires actual display-backed Compatibility rendering");
                string adapter = RenderingServer.GetVideoAdapterName();
                TestAssert.True(!string.IsNullOrWhiteSpace(adapter) &&
                    !new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" }
                        .Any(name => adapter.Contains(name, StringComparison.OrdinalIgnoreCase)),
                    "fog raster requires a hardware GPU");
                if (phase == "fog-raster") await Presentation.HeadlampFogRasterTests.Run(this);
                else await Presentation.HeadlampFogRasterTests.RunNormalShaderComparison(this);
            }
            else if (phase == "gravity-delta")
            {
                PlayerMotionModelTests.Run();
                PlayerMotionModelTests.RunGravityDeltaRegression();
            }
            else if (phase == "swing-loss") await StableClawPullRuntimeTests.RunSwingTargetLossRegression(this);
            else if (phase == "swing-contact-safety") await StableClawPullRuntimeTests.RunSwingContactSafetyRegression(this);
            else if (phase == "rope-support") PlayerMotionModelTests.RunRopeGravitySupportRegression();
            else if (phase == "arrival-transition") await MechanicalClawAutoPullRuntimeTests.RunArrivalTransitionRegression(this);
            else if (phase == "keyboard")
            {
                await KeyboardClawTargetTests.Run(this);
                await KeyboardClawRuntimeTests.Run(this);
                await RunIsolatedTitleHold.RunContract(this);
            }
            else if (phase == "actions") await KeyboardActionRuntimeTests.Run(this);
            else if (phase == "boost-presentation")
                await MechanicalClawRuntimeTests.ScheduledBoostKeepsTheRenderedAttachedAction(this);
            else if (phase == "traversal-broken-tooth")
                await PrologueTraversalRouteTests.RunBrokenToothOnly(this);
            else if (phase == "traversal-upper-entry")
                await PrologueTraversalRouteTests.RunUpperEntryOnly(this);
            else if (phase == "basin-clearance") await BasinLaborerClearanceRuntimeTests.Run(this);
            else if (phase == "basin-claw-target") await BasinClawTargetTests.Run(this);
            else if (phase == "first-combat") await FirstCombatGuidanceRuntimeTests.Run(this);
            else if (phase == "basin-supply-lifetime") await BasinSupplyRuntimeTests.RunCompleteLifetime(this);
            else if (phase == "basin-supply-mount-only") await BasinSupplyRuntimeTests.RunMountOnly(this);
            else if (phase is "basin-resource-control" or "basin-resource-claw-prefix" or "basin-resource-pre-prefix")
                await BasinSupplyRuntimeTests.RunLifetimeBoundary(this,
                    phase == "basin-resource-control" ? "none" : phase == "basin-resource-claw-prefix" ? "claw" : "pre");
            else if (phase is "entrance-enclosure" or "ui-entrance-enclosure")
                await PrologueCanyonTraversalTests.RunEntranceEnclosure(this, presentation: phase == "ui-entrance-enclosure");
            else if (phase == "ordinary-wall-sweep")
            {
                TestAssert.True(DisplayServer.GetName() == "headless", "local ordinary wall evidence is headless only");
                await PrologueCanyonTraversalTests.RunEntranceEnclosure(this, ordinaryWallOnly: true);
            }
            else if (phase == "frozen-wall-sweep")
            {
                TestAssert.True(DisplayServer.GetName() == "headless", "local frozen wall evidence is headless only");
                await PrologueCanyonTraversalTests.RunEntranceEnclosure(this, frozenWallOnly: true);
            }
            else if (phase is "descent-guidance" or "ui-descent-guidance")
                await DescentClawGuidanceRuntimeTests.Run(this, presentation: phase == "ui-descent-guidance");
            else if (phase == "boundary")
            {
                KeyboardInputTraceBufferTests.RunFocus();
                KeyboardInputTraceSidecarTests.Run();
                await KeyboardRouteDriverRuntimeTests.RunIndependentFocus(this);
                await FormalInputBoundaryTests.Run(this);
            }
            else if (phase == "contact-safety") await MechanicalClawAutoPullRuntimeTests.RunContactSafetyRegression(this);
            else if (phase == "contact-blockers") await MechanicalClawAutoPullRuntimeTests.RunObstructionSafetyRegression(this);
            else if (phase == "ceiling-observation") await MechanicalClawAutoPullRuntimeTests.RunCeilingNeutralObservation(this);
            else if (phase == "basin-first-reel-trajectory") await BasinCounterweightRuntimeTests.RunFirstReelTrajectory(this);
            else if (phase == "basin-swing-release-trajectory") await BasinCounterweightRuntimeTests.RunSwingReleaseTrajectory(this);
            else if (phase == "basin-first-reel-contract") await BasinCounterweightRuntimeTests.RunFirstReelOwnerContract(this);
            else if (phase.StartsWith("level-cache-", StringComparison.Ordinal))
                await BasinAscentCameraRuntimeTests.RunCacheDiagnostic(this, CacheMode(phase));
            else if (phase == "fall-claw") await PlayerFallDamageRuntimeTests.RunGrappleLandingRegression(this);
            else await StableClawPullRuntimeTests.Run(this, phase);
            WriteKeyboardTraceOnce("passed");
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("stable-claw-" + phase);
#if REFCOUNTED_FATAL_IDENTITY
            if (phase == "basin-supply-lifetime")
                Knight.Tests.Diagnostics.Task14NativeResourceIdentity.AssertSectorAllocationComplete();
#endif
            if (phase == "vfx-frame-binding-leaf")
                GD.Print("VFX_FRAME_BINDING_LEAF_EXIT whole_post=false timeout_attributed=false human=false whole_goal=false");
            else if (phase.StartsWith("level-cache-", StringComparison.Ordinal))
                GD.Print($"LEVEL_CACHE_DIAGNOSTIC_COMPLETE mode={CacheMode(phase)} cycles=3 playing=3 released=3 production_load_changed=false behavior_accepted=false whole_goal=false");
            else if (phase == "basin-swing-release-trajectory")
                GD.Print("BASIN_SWING_REPLAY_RUNNER_EXIT behavior_accepted=false full_main=false human=false whole_goal=false");
            else if (phase is "basin-first-reel-trajectory" or "basin-first-reel-contract")
                GD.Print("BASIN_FIRST_REEL_RUNNER_EXIT behavior_accepted=false full_main=false human=false whole_goal=false");
            else if (phase == "ceiling-observation")
                GD.Print("STABLE_CLAW_DIAGNOSTIC_COMPLETE phase=" + phase +
                    " behavior_accepted=false contact_safety_gate=separate_suite isolated_fixture=true human=false whole_goal=false");
#if REFCOUNTED_FATAL_IDENTITY
            else if(phase=="resident-map-census")GD.Print("RESIDENT_CENSUS_IDENTITY_DIAGNOSTIC_COMPLETE behavior_accepted=false census_qualified=false human=false whole_goal=false");
#endif
#if METADATA_RESOURCE_IDENTITY
            else if(phase is "resident-archive-metadata" or "resident-steam-metadata")GD.Print("METADATA_RESOURCE_DIAGNOSTIC_EXIT behavior_accepted=false canonical_published=false human=false whole_goal=false");
#endif
            else GD.Print("STABLE_CLAW_OK phase=" + phase + " isolated_fixture=true human=false whole_goal=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            // Preserve the primary failure before any asynchronous/finalizer cleanup can fail.
            GD.PushError(exception.ToString());
            WriteKeyboardTraceOnce("failed");
            Exception cleanupFailure = null;
            try
            {
                GD.Print("STABLE_FAILURE_DRAIN_ENTER original_gc=true additional_gc=0");
                Paused = false;
                await ToSignal(this, SignalName.ProcessFrame);
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("stable-claw-failure");
            }
            catch (Exception cleanupException) { cleanupFailure = cleanupException; }
            if (cleanupFailure != null) GD.PushError($"Failure-path cleanup also failed: {cleanupFailure}");
            Quit(1);
        }
        finally { Paused = false; }
    }

    void IKeyboardInputTraceSink.RetainKeyboardInputTrace(KeyboardInputTraceBuffer trace)
    {
        if(_retainInputTrace) _keyboardTrace??=trace;
    }

    private static ResourceLoader.CacheMode CacheMode(string phase) => phase switch
    {
        "level-cache-reuse" => ResourceLoader.CacheMode.Reuse,
        "level-cache-ignore" => ResourceLoader.CacheMode.Ignore,
        "level-cache-ignore-deep" => ResourceLoader.CacheMode.IgnoreDeep,
        _ => throw new ArgumentException("Unknown cache diagnostic mode", nameof(phase))
    };

    private async System.Threading.Tasks.Task RunVfxFrameBindingLeaf()
    {
        const string name = "PrologueVfxRuntimeTests.VerifyRuntimeFrameBinding";
        Task7Case[] selected = PrologueVfxRuntimeTests.Task7Cases(this).Where(item => item.Name == name).ToArray();
        TestAssert.True(selected.Length == 1 && selected[0].Disposition == Task7CaseDisposition.Executable && selected[0].Execute != null,
            "VFX diagnostic runs exactly the original registered executable delegate");
        bool prior = Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks;
        try
        {
            Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = true;
            Task7CaseRun result = await Task7CaseRegistry.Execute(selected, (caseName, entering) =>
                GD.Print($"VFX_FRAME_BINDING_LEAF_BOUNDARY stage={(entering ? "ENTER" : "RETURN")} name={caseName} engine_ms={Time.GetTicksMsec()}"));
            TestAssert.True(result.Started.SetEquals(new[] { name }) && result.Completed.SetEquals(new[] { name }),
                "the original VFX leaf starts and returns exactly once");
        }
        finally { Knight.Presentation.PrologueProductionMode.AllowDiagnosticFallbacks = prior; }
        GD.Print("VFX_FRAME_BINDING_LEAF_RETURNED original_registered_case=true started=1 completed=1 whole_post=false timeout_attributed=false human=false");
    }

    private void BuildEntranceRockCandidate()
    {
        string candidate = Path.Combine(_acceptedStorage, "entrance-rock-candidate.tscn");
        var root = Knight.Build.Prologue.PrologueCanyonBuildParts.Build(
            Knight.Prologue.Seamless.PrologueSectorId.CanyonEntrance);
        try
        {
            Error error = Knight.Build.Prologue.PrologueCanyonBuildParts.SaveAndValidate(root,
                candidate, out int before, out int after, out string failure);
            TestAssert.True(error == Error.Ok && before == after,
                $"entrance rock candidate roundtrip: {failure}");
        }
        finally { root.Free(); }
        string original = ProjectSettings.GlobalizePath("res://scenes/prologue/sectors/CanyonEntrance.tscn");
        TestAssert.True(NormalizeEntranceScene(File.ReadAllText(original)) ==
            NormalizeEntranceScene(File.ReadAllText(candidate)),
            "ENTRANCE_ROCK_SEMANTICS candidate may change only five explicit visual roles, not any gameplay or other scene property");
        GD.Print("ENTRANCE_ROCK_CANDIDATE_OK serialized_semantics=true roles_only=true pixels=false human=false");
    }

    private static string NormalizeEntranceScene(string text)
    {
        // Resource IDs and editor node IDs are regenerated by the established packer.
        // Keep EVERY other serialized byte, including resource order, paths and values.
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(text, @"\[(?:ext_resource|sub_resource) [^\r\n]* id=""([^""]+)""\]"))
            ids.Add(match.Groups[1].Value, "canonical" + ids.Count);
        text = Regex.Replace(text, @"(?<=(?:id=|ExtResource\(|SubResource\())""([^""]+)""",
            match => "\"" + ids[match.Groups[1].Value] + "\"");
        text = Regex.Replace(text, @" unique_id=\d+(?=\])", string.Empty);
        var roles = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["EntranceOuterWall"] = 3, ["StartFloor"] = 1, ["BridgeFarBank"] = 1,
            ["EntranceSkyOverhang"] = 2, ["RiftEntryCeiling"] = 2
        };
        string consumer = null;
        var normalized = new List<string>();
        foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith("[", StringComparison.Ordinal))
            {
                Match node = Regex.Match(line, @"^\[node name=""([^""]+)"" type=""Node2D"" parent=""VisualRoot/GameplayReadable""\]$");
                consumer = node.Success ? node.Groups[1].Value : null;
            }
            if (line.StartsWith("RockSkinRole = ", StringComparison.Ordinal))
            {
                TestAssert.True(consumer != null && roles.TryGetValue(consumer, out int expected) &&
                    line == "RockSkinRole = " + expected,
                    "only the five intended visual consumers may serialize a new rock role");
                continue;
            }
            normalized.Add(line);
        }
        return string.Join("\n", normalized);
    }

    private void WriteKeyboardTraceOnce(string outcome)
    {
        if(_traceAttempted || _keyboardTrace==null) return;
        _traceAttempted=true;
        try
        {
            // Route has returned or thrown: retain managed observations only,
            // without rereading or replaying keys after actor cleanup.
            _keyboardTrace.Freeze("stable_runner_finalization");
            string path=KeyboardInputTraceSidecar.Write(_acceptedStorage!=null,_acceptedStorage,_keyboardTrace,outcome);
            GD.Print($"STABLE_KEYBOARD_TRACE_WRITTEN path={path} diagnostic_only=true os_origin=false");
        }
        catch(Exception error) { GD.PushError($"Keyboard diagnostic retention failed: {error}"); }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void RunSavedCanyonStructure()
    {
        PackedScene levelScene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueCanyonStructureTests.Run(levelScene);
        PrologueCanyonStructureTests.RunV2NegativeContracts(levelScene);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void LoadLevelForResourceDiagnostic()
    {
        using PackedScene levelScene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TestAssert.True(levelScene.CanInstantiate(), "load-only diagnostic reads a valid level without instantiating it");
        GD.Print("UI_LEVEL_LOAD_ONLY no_instance=true no_tree_mount=true");
    }
}
