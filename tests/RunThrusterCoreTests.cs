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
            else if (phase == "steam-top-seam") await PrologueCanyonTraversalTests.RunSteamTopSeamOnly(this);
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
            else if (phase == "presentation-model") ThrusterPresentationModelTests.Run();
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
