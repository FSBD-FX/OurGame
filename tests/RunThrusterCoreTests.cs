using System;
using Godot;
using Knight.Tests.Unit;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunThrusterCoreTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string phase = null;
            foreach (string argument in OS.GetCmdlineUserArgs())
            {
                if (!argument.StartsWith("--phase=", StringComparison.Ordinal)) continue;
                if (phase != null) throw new ArgumentException("Duplicate thruster phase");
                phase = argument.Substring("--phase=".Length);
            }
            if (phase == "module") MobilityModuleTests.Run();
            else if (phase == "arc-gear")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                ArcGearMotionModelTests.Run();
                await CanyonMechanismRuntimeTests.RunArcGear(this);
            }
            else if (phase == "arc-gear-layout") await CanyonArcGearPlacementTests.RunSaved(this);
            else if (phase == "arc-gear-candidate") await CanyonArcGearPlacementTests.RunCandidate(this);
            else if (phase == "moving-gear-layout") await CanyonMovingGearPlacementTests.RunSaved(this);
            else if (phase == "moving-gear-candidate") await CanyonMovingGearPlacementTests.RunCandidate(this);
            else if (phase == "moving-gear")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                MovingGearMotionModelTests.Run();
                await CanyonMechanismRuntimeTests.RunMovingGear(this);
                await CanyonMechanismRuntimeTests.RunMovingGearFog(this);
                await CanyonMechanismRuntimeTests.RunFixedGearContact(this);
            }
            else if (phase == "ground-gait")
            {
                await OrdinaryAirMotionRuntimeTests.RunNonzeroAirHandoff(this);
                await OrdinaryAirMotionRuntimeTests.RunCarrierHandoff(this);
                await ThrusterPresentationRuntimeTests.RunNaturalAirCarrierHandoff(this);
                await ThrusterPresentationRuntimeTests.RunNaturalAirActionHandoff(this);
                await ThrusterPresentationRuntimeTests.RunNaturalActionReleaseReentry(this);
                await PlayerFormalVisualTests.RunGroundBrakeNative(this);
                PlayerFormalVisualTests.RunGroundBrakeModel();
                await PlayerFormalVisualTests.RunGroundBrakeDrawOnly(this);
                PlayerFormalVisualTests.RunSecondaryFacingModel();
                await PlayerFormalVisualTests.RunSecondaryFacingDrawOnly(this);
                await OrdinaryAirMotionRuntimeTests.RunCause(this);
                OrdinaryAirMotionTests.Run();
                await OrdinaryAirMotionRuntimeTests.RunDraw(this);
                await OrdinaryAirMotionRuntimeTests.RunJump(this);
                PlayerPresentationTests.RunLandingLoad();
                PlayerPresentationTests.RunLandingSupport();
                await PlayerFormalVisualTests.RunLandingLoadOnly(this);
                PlayerPresentationTests.RunBodyLoad();
                await PlayerFormalVisualTests.RunBodyLoadOnly(this);
                await PlayerFormalVisualTests.RunGroundGaitReviewContracts(this);
                PlayerPresentationTests.RunGroundGait();
                await PlayerFormalVisualTests.RunGroundGaitOnly(this);
            }
            else if (phase == "profile-trace-normal")
            {
                TestAssert.True(typeof(RunThrusterCoreTests).Assembly.GetType("Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace", false) == null &&
                    typeof(RunThrusterCoreTests).Assembly.GetType("Knight.Tests.Diagnostics.Task14NativeResourceIdentity", false) == null,
                    "ordinary loaded assembly must exclude diagnostic lifetime and native identity types");
                GD.Print("PROFILE_TRACE_NORMAL_OK loaded_assembly=true lifetime_type_absent=true native_identity_absent=true gameplay_accepted=false human=false");
            }
            else if (phase == "approved-lantern") await ApprovedLanternRuntimeTests.RunBinding(this);
            else if (phase == "approved-lantern-visible") await ApprovedLanternRuntimeTests.RunVisible(this);
            else if (phase == "approved-lantern-layout")
            {
                await PrologueGuidanceRuntimeTests.RunStableBorderlessPresentation(this);
                PrologueGuidanceModelTests.RunThrusterTeaching();
                await KeyboardTeachingRuntimeTests.RunThrusterTeaching(this);
                GD.Print("APPROVED_LANTERN_LAYOUT_REGRESSION_OK samples=12 old_mounted_teaching=true human=false");
            }
            else if (phase == "main-scene-owner") await PrologueUiRuntimeTests.MainSceneOwnerSurvivesOriginalPauseCase(this);
            else if (phase == "completion-pause") await PrologueCompletionTests.RunCurrentPauseQuitOnly(this);
            else if (phase == "recovery-feedback") await PrologueAudioDirectorTests.RecoveryFeedbackSurvivesOwnBoundary(this);
            else if (phase == "formal-recovery")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                await PlayerFormalVisualTests.RunTask7RecoveryContracts(this);
            }
            else if (phase == "incinerator") await CanyonIncineratorRuntimeTests.Run(this);
            else if (phase == "incinerator-layout") await CanyonIncineratorLayoutTests.Run(this);
            else if (phase == "incinerator-traversal") await CanyonIncineratorTraversalTests.Run(this);
            else if (phase == "incinerator-bypass") await CanyonIncineratorTraversalTests.RunBypass(this);
            else if (phase == "incinerator-death") await CanyonIncineratorTraversalTests.RunDeath(this);
            else if (phase == "incinerator-stage-leaf")
                Knight.Build.Prologue.IncineratorAnnexAuthoring.StageLeaf(IsolatedPrologueFixture.ValidateStorageRoot());
            else if (phase == "incinerator-stage-parent")
            {
                string stagedLeaf=null;
                foreach(string argument in OS.GetCmdlineUserArgs())
                    if(argument.StartsWith("--incinerator-leaf-input=",StringComparison.Ordinal))
                    {
                        if(stagedLeaf!=null) throw new ArgumentException("Duplicate staged incinerator leaf input");
                        stagedLeaf=argument.Substring("--incinerator-leaf-input=".Length);
                    }
                Knight.Build.Prologue.IncineratorAnnexAuthoring.StageParent(IsolatedPrologueFixture.ValidateStorageRoot(),stagedLeaf);
            }
            else if (phase == "patrol-world-window")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                await CanyonResidentPlacementTests.RunPatrolWorldWindow(this);
            }
            else if (phase == "rock-leaf-stages")
            {
                await CanyonResidentPlacementTests.RunRockLeafStages(this, IsolatedPrologueFixture.ValidateStorageRoot());
            }
            else if (phase == "rock-leaf-faults")
            {
                await CanyonResidentPlacementTests.RunRockLeafCleanupFaults(this, IsolatedPrologueFixture.ValidateStorageRoot());
            }
            else if (phase == "sector-feedback-faults")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                await PrologueAudioDirectorTests.SectorCommitObserverFaultsDoNotAbortRecovery(this);
            }
            else if (phase == "recovery-compatibility")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                RecoveryResourceBoundaryTrace.RunControls();
                using var recoveryResourceTrace = RecoveryResourceBoundaryTrace.Begin();
                RecoveryResourceBoundaryTrace.SetScope("saved1");
                await PrologueAudioDirectorTests.SectorCommitClearsOldFeedbackSynchronously(this);
                await SeamlessPrologueWorldTests.RunFeedbackCommitBoundaryControls(this);
                RecoveryResourceBoundaryTrace.SetScope("saved2");
                await PrologueAudioDirectorTests.Task10ActionCueBaselineAndReentryAreExact(this);
                RecoveryResourceBoundaryTrace.SetScope("publishers");
                await PrologueAudioDirectorTests.Task10ProductionPublishersReachDirectors(this);
                RecoveryResourceBoundaryTrace.AssertComplete();
                GD.Print("THRUSTER_RECOVERY_COMPATIBILITY_OK original_delegates=2 typed_cleanup=true " +
                    "legacy_component_fixture=true native_player_movement=false headless_only=true human=false");
            }
            else if (phase == "minimap-lifetime") await MinimapLifecycleTests.RunOwnedViewResourceLifetime(this);
            else if (phase == "minimap-lifecycle")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                await MinimapLifecycleTests.Run(this);
            }
            else if (phase == "guidance-contracts") await PrologueGuidanceRuntimeTests.RunCurrentTraversalContractsOnly(this);
            else if (phase == "formal-claw-phases") await PlayerFormalVisualTests.RunCurrentClawPhasesOnly(this);
            else if (phase == "steam-top-seam") await PrologueCanyonTraversalTests.RunSteamTopSeamOnly(this);
            else if (phase == "ascent-nine-entry") await PrologueCanyonTraversalTests.RunAscentNineEntryOnly(this);
            else if (phase == "temple-trigger-admission") await PrologueCanyonTraversalTests.RunTempleTriggerAdmissionOnly(this);
            else if (phase == "boss-admission-regression")
            {
                IsolatedPrologueFixture.ValidateStorageRoot();
                await ToSignal(this, SignalName.ProcessFrame);
                await BossAdmissionRuntimeTests.Run(this);
                GD.Print("THRUSTER_BOSS_ADMISSION_REGRESSION_OK original_full_runtime_delegate=true isolated_storage=true controlled_metadata=true natural_route=false human=false");
            }
            else if (phase == "descent-handoff") await PrologueCanyonGeneratedMainRouteTests.RunTask10DescentHandoff(this);
            else if (phase == "main-abyss") await PrologueCanyonGeneratedMainRouteTests.RunTask10MainAbyssEntry(this);
            else if (phase == "route-main-diagnostic") await PrologueCanyonGeneratedMainRouteTests.RunTask10HeadlessRoute(this, PrologueCanyonGeneratedMainRouteTests.Task10RouteVariant.Main);
            else if (phase == "rift-wall-sweep") await PrologueCanyonGeneratedMainRouteTests.RunTask10HeadlessRoute(this, PrologueCanyonGeneratedMainRouteTests.Task10RouteVariant.Main, riftWallOnly: true);
            else if (phase == "route-exploration-diagnostic") await PrologueCanyonGeneratedMainRouteTests.RunTask10HeadlessRoute(this, PrologueCanyonGeneratedMainRouteTests.Task10RouteVariant.Exploration);
            else if (phase == "seamless-reset-observer") await SeamlessPrologueWorldTests.RunThrusterResetObserverOnly(this);
            else if (phase == "generated-main-death") await PrologueLevelRuntimeTests.RunGeneratedMainDeathOnly(this);
            else if (phase == "legacy-geometry")
            {
                PrologueCanyonLegacyReplacementTests.RunLegacyGeometryOnly();
                await PrologueGuidanceRuntimeTests.RunCurrentRiftLandingContractOnly(this);
            }
            else if (phase == "p1-hidden-recovery") await P1LoopRuntimeTests.RunHiddenRecoveryOnly(this);
            else if (phase == "p1-recovery-exit") await P1LoopRuntimeTests.RunRecoveryExitBoundaryOnly(this);
            else if (phase == "motion-snapshot") await ThrusterMotionForecastRuntimeTests.RunSnapshot(this);
            else if (phase == "free-forecast") FreeMotionForecastTests.Run();
            else if (phase == "upper-mid") await PrologueTraversalRouteTests.RunUpperMidOnly(this);
            else if (phase == "upper-high") await PrologueTraversalRouteTests.RunUpperHighOnly(this);
            else if (phase == "legacy-grip-stone") await PrologueCanyonTraversalTests.RunLegacyGripStoneOnly(this);
            else if (phase == "legacy-terrain") await PrologueCanyonTraversalTests.RunLegacyTerrainOnly(this);
            else if (phase == "legacy-sightlines") await PrologueCanyonTraversalTests.RunLegacyAscentSightlinesOnly(this);
            else if (phase == "legacy-descent-entry") await PrologueCanyonTraversalTests.RunLegacyDescentEntryOnly(this);
            else if (phase == "legacy-descent-entry-trace") await PrologueCanyonTraversalTests.RunLegacyDescentEntryTraceOnly(this);
            else if (phase == "legacy-ascent-high") await PrologueCanyonTraversalTests.RunLegacyAscentHighEntryOnly(this);
            else if (phase == "profile-diagnostics") PlayerPresentationTests.RunProfileDiagnosticContract();
            else if (phase == "detached-motor") Knight.Tests.Unit.ThreadInteractionTests.RunDetachedMotorContract();
            else if (phase == "collision-snapshot-lifetime") await CollisionSnapshotLifetimeRuntimeTests.Run(this);
            else if (phase == "stationary-guard")
            {
                await BasinCounterweightRuntimeTests.RunStationaryGuardContract(this);
                await GhostLandingGuardRuntimeTests.Run(this);
                await GhostRecoveryCounterRuntimeTests.Run(this);
            }
            else if (phase == "bell-gate") await PrologueCanyonTraversalTests.RunBellGateOnly(this);
            else if (phase == "dynamics") ThrusterDynamicsTests.Run();
            else if (phase == "pulse") ThrusterPulseModelTests.Run();
            else if (phase == "impact-formula") LandingImpactDamageTests.Run();
            else if (phase == "impact-config") LandingImpactDamageTests.RunConfiguration();
            else if (phase == "impact-player") await LandingImpactRuntimeTests.Run(this);
            else if (phase == "impact-negative") await LandingImpactRuntimeTests.RunNegatives(this);
            else if (phase == "impact-support") await LandingImpactRuntimeTests.RunMovingSupports(this);
            else if (phase == "impact-reentry") await LandingImpactRuntimeTests.RunReentry(this);
            else if (phase == "impact-dangers") await LandingImpactRuntimeTests.RunIndependentDangers(this);
            else if (phase == "equip") await ThrusterEquipRuntimeTests.Run(this);
            else if (phase == "equip-support") await ThrusterEquipRuntimeTests.RunEligibility(this);
            else if (phase == "equip-guards") await ThrusterEquipRuntimeTests.RunGuards(this);
            else if (phase == "initial-module") await ThrusterEquipRuntimeTests.RunInitialModule(this);
            else if (phase == "authoring-contract") ThrusterEquipRuntimeTests.RunAuthoringContract();
            else if (phase == "saved-module-prepare") ThrusterEquipRuntimeTests.PrepareSavedModules();
            else if (phase == "saved-module") await ThrusterEquipRuntimeTests.RunSavedModules(this);
            else if (phase == "equip-claw") await ThrusterEquipRuntimeTests.RunClawGuards(this);
            else if (phase == "formal-module") await ThrusterEquipRuntimeTests.RunFormalModule(this);
            else if (phase == "presentation-model")
            {
                PlayerFormalVisualTests.RunGroundBrakeModel();
                OrdinaryAirMotionTests.Run();
                ThrusterPresentationModelTests.Run();
            }
            else if (phase == "brake-crossing") ThrusterPresentationModelTests.RunBrakeCrossing();
            else if (phase == "presentation-player") await ThrusterPresentationRuntimeTests.Run(this);
            else if (phase == "presentation-rig") await ThrusterPresentationRuntimeTests.RunRig(this);
            else if (phase == "world-jet") await ThrusterPresentationRuntimeTests.RunWorldJet(this);
            else if (phase == "world-jet-last-leaf") await ThrusterPresentationRuntimeTests.RunWorldJetLastLeaf(this);
            else if (phase == "rig-compatibility") await ThrusterPresentationRuntimeTests.RunRigCompatibility(this);
            else if (phase == "mantle")
            {
                await PlayerMantleMotionTests.Run(this);
                GD.Print("THRUSTER_MANTLE_OK original_cases=2 real_draw_mapper=true fixed_upper_band=true deforming_hem=true rigid_root_compatible=true headless_geometry_only=true human=false");
            }
            else if (phase == "audio-owner") await ThrusterPresentationRuntimeTests.RunAudioOwner(this);
            else if (phase == "teaching")
            {
                PrologueGuidanceModelTests.RunThrusterTeaching();
                await KeyboardTeachingRuntimeTests.RunUi(this);
                KeyboardTeachingRuntimeTests.RunGuidance();
                await KeyboardTeachingRuntimeTests.RunThrusterTeaching(this);
            }
            else if (phase == "teaching-entry") await KeyboardTeachingRuntimeTests.RunThrusterTeaching(this);
            else if (phase == "teaching-ui")
            {
                PrologueGuidanceModelTests.RunThrusterTeaching();
                await KeyboardTeachingRuntimeTests.RunUi(this);
                await KeyboardTeachingRuntimeTests.RunHighScaleTitleReference(this);
                await KeyboardTeachingRuntimeTests.RunHighScaleTitleSettings(this);
                await KeyboardTeachingRuntimeTests.RunHighScalePause(this);
                KeyboardTeachingRuntimeTests.RunGuidance();
                await PrologueGuidanceRuntimeTests.RunCurrentMountedGuidanceOnly(this);
            }
            else if (phase == "impact-legacy")
            {
                await PlayerFallDamageRuntimeTests.Run(this);
                GD.Print("THRUSTER_IMPACT_LEGACY_OK existing_full_drop_fixtures=true scripted_adapter_provenance=true extracted_saved_bottom=true whole_route=false human=false");
            }
            else if (phase == "input") await ThrusterKeyboardFactsRuntimeTests.Run(this);
            else if (phase == "traversal")
            {
                await PlayerTraversalInputTests.Run(this);
                GD.Print("THRUSTER_TRAVERSAL_OK keyboard_budget_matrix=true human=false whole_route=false");
            }
            else if (phase == "player")
            {
                await ThrusterPlayerRuntimeTests.Run(this);
                await MechanicalClawRuntimeTests.RealClawTakeoverEndsBurnWithoutRefund(this);
                await ThrusterPlayerRuntimeTests.RunLegacyMomentum(this);
            }
            else throw new ArgumentException("Unsupported thruster phase: " + phase);
            GD.Print($"THRUSTER_CORE_OK phase={phase} human=false whole_goal=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
