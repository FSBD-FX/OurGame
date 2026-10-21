#nullable enable
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;
using Knight.Tests.Diagnostics;
using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Knight.Tests;

// Explicit bounded modes only; generated-mounted synthetic phases launch separately.
public partial class RunTask17SteamDiverterIntegrationTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 2 || args[0] is not ("--packed-contracts" or "--activation-contracts" or "--recorder-contracts" or "--presentation-contracts" or "--render-refinement-contracts" or "--state-silhouette-contracts" or "--flow-contracts" or "--stroke-bounds-contracts" or "--vapor-fade-contracts" or "--installation-bindings-contracts" or "--route-membership-contracts" or "--archive-metadata-neighbor-contract" or "--mounted-route-contracts" or "--mounted-lifecycle-contracts" or "--movie-frozen" or "--movie-trench" or "--neighbor-structure" or "--neighbor-steam" or "--neighbor-valve" or "--neighbor-encounters" or "--neighbor-pressure" or "--neighbor-sample" or "--diagnose-encounters") ||
                !args[1].StartsWith("--case-storage-root=", StringComparison.Ordinal))
                throw new ArgumentException("Task17B requires an explicit bounded mode and isolated case root.");
            string storage = args[1].Substring("--case-storage-root=".Length);
            ValidateStorage(storage);
            await AwaitIdentityRelease(storage);
            if (args[0] == "--packed-contracts") CanyonSteamDiverterIntegrationTests.RunPackedContracts();
            else if (args[0] == "--activation-contracts") await CanyonSteamDiverterIntegrationTests.RunRuntimeContracts(this);
            else if (args[0] == "--recorder-contracts") await CanyonSteamDiverterIntegrationTests.RunRecorderContracts(this, storage);
            else if (args[0] == "--presentation-contracts") await CanyonSteamDiverterVisualTests.RunInstallationPresentationContracts(this);
            else if (args[0] == "--render-refinement-contracts") await CanyonSteamDiverterVisualTests.RunRefinementContracts(this);
            else if (args[0] == "--state-silhouette-contracts") await CanyonSteamDiverterVisualTests.RunStateSilhouetteContracts(this);
            else if (args[0] == "--flow-contracts") await CanyonSteamDiverterVisualTests.RunFlowContracts(this);
            else if (args[0] == "--stroke-bounds-contracts") await CanyonSteamDiverterVisualTests.RunStrokeBoundsContracts(this);
            else if (args[0] == "--vapor-fade-contracts") await CanyonSteamDiverterVisualTests.RunVaporFadeContracts(this);
            else if (args[0] == "--installation-bindings-contracts") CanyonSteamDiverterIntegrationTests.RunInstallationBindingsContracts();
            else if (args[0] == "--route-membership-contracts") CanyonSteamDiverterIntegrationTests.RunRouteMembershipContracts();
            else if (args[0] == "--mounted-route-contracts") await CanyonSteamDiverterIntegrationTests.RunMountedRouteContracts(this);
            else if (args[0] == "--mounted-lifecycle-contracts") await CanyonSteamDiverterIntegrationTests.RunMountedLifecycleContracts(this);
            else if (args[0] == "--movie-frozen") await CanyonSteamDiverterRouteProbe.Run(this, "frozen", storage);
            else if (args[0] == "--movie-trench") await CanyonSteamDiverterRouteProbe.Run(this, "trench", storage);
            else if (args[0] == "--archive-metadata-neighbor-contract") CanyonSteamDiverterIntegrationTests.RunArchiveMetadataNeighborContract();
            else if (args[0] == "--neighbor-structure")
            {
                CanyonWorldLayoutTests.Run();
                ContinuousCanyonReadabilityTests.Run(false);
                ContinuousCanyonReadabilityTests.Run(true);
                GD.Print("TASK17B_NEIGHBOR_STRUCTURE_OK layout=true readabilityPacked=true readabilityBuilder=true sceneWrites=false");
            }
            else if (args[0] == "--neighbor-steam")
            {
                CanyonSteamDiverterModelTests.RunFirstHandoff();
                CanyonSteamDiverterModelTests.RunRemainingContracts();
                CanyonSteamDiverterClockTests.RunContracts();
                await CanyonSteamDiverterRuntimeTests.RunMountedDefault(this);
                await CanyonSteamDiverterRuntimeTests.RunFunctionalContracts(this);
                await CanyonSteamDiverterPhysicalTests.RunContracts(this);
                await CanyonSteamDiverterVisualTests.RunContracts(this);
                GD.Print("TASK17B_NEIGHBOR_STEAM_OK modelFirst=true modelRemaining=true clock=true mountedDefault=true functional=true physical=true originalVisual=true componentOnly=true");
            }
            else if (args[0] == "--neighbor-valve")
            {
                await CanyonMechanismRuntimeTests.RunGeneratedSteamVisibility(this);
                await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(this);
                GD.Print("TASK17B_NEIGHBOR_VALVE_OK generatedVisibility=true requiredLifecycle=true persistenceInMemory=true capture=false aggregate=false");
            }
            else if (args[0] == "--neighbor-encounters")
            {
                await ContinuousCanyonEncounterTests.RunTask14FrozenOnly(this);
                await ContinuousCanyonEncounterTests.RunTask14GeneratedOnly(this);
                await Task14TelegraphAdapterTests.Run(this);
                GD.Print("TASK17B_NEIGHBOR_ENCOUNTERS_OK task14Frozen=true task14Generated=true telegraphAdapter=true externalPersistence=true inputProof=false");
            }
            else if (args[0] == "--diagnose-encounters")
            {
                await ContinuousCanyonEncounterTests.RunTask14FrozenOnly(this);
                await ContinuousCanyonEncounterTests.RunTask14GeneratedOnly(this);
                var observer = new Task17TelegraphFailureObserver(this);
                try { await Task14TelegraphAdapterTests.Run(this); }
                finally { observer.Dispose(); }
                // Reached only if the unchanged adapter returned normally.
                GD.Print("TASK17B_REMOTE_ORIGIN_ADAPTER_RETURNED targetAssertion=false snapshot=false acceptance=observational-only");
                Quit(0);
                return;
            }
            else if (args[0] == "--neighbor-pressure")
            {
                await ArchivePressureGateIntegrationTests.RecorderSeesOneRealGateOpen(this);
                await ArchivePressureGateIntegrationTests.RecorderRebindBaselinesThenTracksNewMount(this);
                await CanyonPressureGateRuntimeTests.RunFix1MountClear(this);
                await CanyonPressureGateRuntimeTests.RunFix1MountOccupied(this);
                await CanyonPressureGateRuntimeTests.RunFix1Visual(this);
                await CanyonPressureGateRuntimeTests.RunFix1PauseTiming(this);
                GD.Print("TASK17B_NEIGHBOR_PRESSURE_OK recorderOpen=true recorderRebind=true mountClear=true mountOccupied=true visual=true pauseTiming=true aggregate=false");
            }
            else if (args[0] == "--neighbor-sample")
            {
                await BasinSampleLayoutIsolationTests.Run(this);
                GD.Print("TASK17B_NEIGHBOR_SAMPLE_OK frozenLayoutIsolation=true lifecycle=true caseLocalTemp=true cleanupOwnDirectoryOnly=true");
            }
            else throw new ArgumentException("Task17B bounded mode has no registered entrypoint.");
            GD.Print("TASK17B_STEAM_INSTALLATION_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    // Keep the owned process alive until its real identity is recorded. This
    // mounts nothing and advances no test/game state before PID-bound release.
    private async Task AwaitIdentityRelease(string storage)
    {
        string release = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(storage))!, "tests-identity.release");
        string expectedPid = OS.GetProcessId().ToString(CultureInfo.InvariantCulture);
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (File.Exists(release))
            {
                if ((File.GetAttributes(release) & FileAttributes.ReparsePoint) != 0 ||
                    !string.Equals(File.ReadAllText(release), expectedPid, StringComparison.Ordinal))
                    throw new InvalidOperationException("Task17B identity release must be a regular marker matching this owned PID.");
                GD.Print("TASK17B_IDENTITY_RELEASED pid=" + expectedPid);
                return;
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        throw new TimeoutException("Task17B actual identity release was not received within the bounded 10 second startup wait.");
    }

    private static void ValidateStorage(string raw)
    {
        if (!Path.IsPathFullyQualified(raw))
            throw new ArgumentException("Task17B case root must be absolute.");
        string storage = Trim(Path.GetFullPath(raw));
        string evidence = Trim(Path.GetFullPath(
            "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task17b"));
        string project = Trim(Path.GetFullPath(ProjectSettings.GlobalizePath("res://")));
        string expectedProject = Trim(Path.GetFullPath(
            "E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555"));
        string? caseDirectory = Path.GetDirectoryName(storage);
        string? caseParent = caseDirectory == null ? null : Path.GetDirectoryName(caseDirectory);
        string expectedTemp = caseDirectory == null ? "" : Path.Combine(caseDirectory, "unit-temp");
        if (!Directory.Exists(storage) || !Directory.Exists(expectedTemp) ||
            !string.Equals(caseParent, evidence, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(storage), "storage", StringComparison.Ordinal) ||
            !string.Equals(project, expectedProject, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Trim(Path.GetFullPath(Path.GetTempPath())), expectedTemp,
                StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Task17B requires an existing unique external evidence case.");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string? value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !string.Equals(Trim(Path.GetFullPath(value)), expectedTemp,
                    StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Task17B {name} must resolve inside the isolated case.");
        }
        foreach (string path in new[] { storage, expectedTemp })
            for (DirectoryInfo? directory = new(path); directory != null; directory = directory.Parent)
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Task17B case paths reject reparse ancestors.");
    }

    private static string Trim(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}


