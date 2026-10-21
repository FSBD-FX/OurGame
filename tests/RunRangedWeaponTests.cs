using System;
using Godot;
using Knight.Tests.Unit;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunRangedWeaponTests : SceneTree
{
    public override async void _Initialize()
    {
        string phase = "unknown";
        try
        {
            IsolatedPrologueFixture.ValidateStorageRoot();
            string[] args = OS.GetCmdlineUserArgs();
            string supplied = Array.Find(args, arg => arg.StartsWith("--ranged-phase=", StringComparison.Ordinal));
            TestAssert.True(supplied != null && Array.FindAll(args,
                    arg => arg.StartsWith("--ranged-phase=", StringComparison.Ordinal)).Length == 1,
                "ranged runner requires exactly one explicit phase");
            phase = supplied["--ranged-phase=".Length..];
            bool spawnIdentityPhase = phase is "spawn-identity" or "spawn-identity-contract";
#if RANGED_SPAWN_IDENTITY
            TestAssert.True(spawnIdentityPhase, "RANGED_SPAWN_SYMBOL_BOUNDARY: diagnostic binary cannot run an ordinary phase");
#else
            TestAssert.True(!spawnIdentityPhase &&
                typeof(RunRangedWeaponTests).Assembly.GetType("Knight.Tests.Integration.RangedSpawnIdentityTrace", false) == null,
                "RANGED_SPAWN_SYMBOL_BOUNDARY: ordinary loaded assembly must exclude spawn diagnostics");
            GD.Print("RANGED_ASSEMBLY_ORDINARY_BOUNDARY_OK spawn_identity=false");
#endif
            if (phase == "ledger") await RangedVolleyDamageLedgerTests.Run(this);
            else if (phase == "models") RangedWeaponModelTests.Run();
            else if (phase == "geometry") RangedAttackGeometryTests.Run();
            else if (phase == "native-geometry") await RangedAttackGeometryRuntimeTests.Run(this);
            else if (phase == "projectile") await RangedWeaponProjectileRuntimeTests.Run(this);
            else if (phase == "actor") await RangedWeaponRuntimeTests.Run(this);
            else if (phase == "actor-saved") await RangedWeaponRuntimeTests.RunSavedScenes(this);
            else if (phase == "threat-policy") await RangedWeaponRuntimeTests.RunThreatPolicy(this);
            else if (phase == "steam-missile-policy") await RangedWeaponRuntimeTests.RunSteamMissileMovingPolicy(this);
            else if (phase == "gallery-laser-policy") await RangedWeaponRuntimeTests.RunGalleryLaserCarriedPolicy(this);
            else if (phase == "steam-sentry-recovery")
            {
                await BellBoltSentryRuntimeTests.RunSteamRecoveryPreArmBaseline(this);
                await BellBoltSentryRuntimeTests.RunSteamRecoveryKeyboard(this, retireOnDefeat: true);
                await BellBoltSentryRuntimeTests.RunSteamRecoveryKeyboard(this, retireOnDefeat: true,
                    exerciseFallbackApproach: true);
            }
            else if (phase == "mixed-policy") await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this);
            else if (phase == "mixed-resource")
            {
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true);
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true, preparationAttacks: 7);
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true,
                    counterProof: RangedWeaponPlacementRuntimeTests.MixedCounterProof.RejectShort);
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true,
                    counterProof: RangedWeaponPlacementRuntimeTests.MixedCounterProof.Connect);
            }
            else if (phase == "mixed-counter")
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true,
                    counterProof: RangedWeaponPlacementRuntimeTests.MixedCounterProof.Connect);
            else if (phase == "mixed-early-counter")
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true,
                    counterProof: RangedWeaponPlacementRuntimeTests.MixedCounterProof.EarlyWarnings);
            else if (phase == "mixed-early-boundary")
                await RangedWeaponPlacementRuntimeTests.RunMixedThreatPolicy(this, resourcePolicy: true,
                    counterProof: RangedWeaponPlacementRuntimeTests.MixedCounterProof.TightWarning);
#if RANGED_SPAWN_IDENTITY
            else if (phase == "spawn-identity-contract") await RangedSpawnIdentityTrace.Run(this, contract: true);
            else if (phase == "spawn-identity") await RangedSpawnIdentityTrace.Run(this, contract: false);
#endif
            else if (phase == "catalog") CanyonEncounterCatalogTests.Run();
            else if (phase == "spawn-contract")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await RangedWeaponPlacementRuntimeTests.RunSpawnContract(this);
            }
            else if (phase == "placement")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await RangedWeaponPlacementRuntimeTests.Run(this);
            }
            else if (phase == "fairness")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await RangedWeaponPlacementRuntimeTests.RunFairness(this);
            }
            else if (phase == "suppression-recovery")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await RangedWeaponPlacementRuntimeTests.RunSuppressionRecovery(this);
                await ContinuousCanyonEncounterTests.RunSuppressionRecoveryGuards(this);
            }
            else if (phase == "camera-ground")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await BasinAscentCameraRuntimeTests.Run(this);
            }
            else if (phase == "presentation")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await RangedWeaponPresentationRuntimeTests.Run(this);
            }
            else if (phase == "audio-legacy")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await PrologueAudioDirectorTests.Run(this);
                await PrologueAudioDirectorTests.DamagedGuidanceCrackleIsShortIsolatedAndRateLimited(this);
            }
            else if (phase == "fog-saved")
            {
                RequireHardwareFogDisplay();
                await FogRangedSavedControllerRuntimeTests.Run(this);
            }
            else if (phase == "fog-pixels")
            {
                RequireHardwareFogDisplay();
                await ToSignal(this, SignalName.ProcessFrame);
                await FogRangedReadabilityRuntimeTests.Run(this);
            }
            else if (phase == "protection")
            {
                await ToSignal(this, SignalName.ProcessFrame);
                RangedProtectionBoundsRuntimeTests.Run(this);
                RoomProtectionRegionRuntimeTests.Run(this);
            }
            else throw new InvalidOperationException("Unknown ranged phase " + phase);
            if (phase is not ("threat-policy" or "mixed-policy" or "mixed-resource" or "mixed-counter" or "mixed-early-counter" or "mixed-early-boundary" or "spawn-identity" or "spawn-identity-contract"))
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("ranged-" + phase);
            GD.Print($"RANGED_WEAPON_OK phase={phase} isolated=true human=false whole_goal=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            if (phase is not ("threat-policy" or "mixed-policy" or "mixed-resource" or "mixed-counter" or "mixed-early-counter" or "mixed-early-boundary" or "spawn-identity" or "spawn-identity-contract"))
                TestSceneTeardown.DrainManagedWrappersAtProcessExit("ranged-failed-" + phase);
            Quit(1);
        }
    }

    private static void RequireHardwareFogDisplay()
    {
        TestAssert.True(!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase),
            "ranged fog pixels require a normal display, not headless");
        TestAssert.Equal("gl_compatibility",
            ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
            "ranged fog uses the actual Compatibility renderer");
        string adapter = RenderingServer.GetVideoAdapterName();
        TestAssert.True(!string.IsNullOrWhiteSpace(adapter), "ranged fog hardware GPU must be reported");
        foreach (string software in new[] { "llvmpipe", "lavapipe", "swiftshader", "Microsoft Basic Render", "softpipe" })
            TestAssert.True(!adapter.Contains(software, StringComparison.OrdinalIgnoreCase),
                $"software GPU cannot certify ranged fog: {adapter}");
        TestAssert.True(adapter.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
            $"ranged fog acceptance uses the actual NVIDIA adapter, got {adapter}");
    }
}
