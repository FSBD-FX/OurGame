using System;
using System.IO;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask16ArchiveGateTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 2 || args[0] is not ("--save-only" or "--packed-only" or
                    "--parent-metadata-only" or "--perimeter-only" or
                    "--recorder-only" or "--recorder-rebind-only" or
                    "--mounted-only" or "--lifecycle-only" or "--death-reset-only" or "--all" or
                    "--neighbors-only" or "--occupied-mount-only" or
                    "--remount-spent-only" or
                    "--remount-full-only" or
                    "--recovery-same-only" or "--recovery-retained-only" or
                    "--recovery-absent-only" or
                    "--continue-spent-producer" or "--continue-spent-consumer" or
                    "--continue-full-producer" or "--continue-full-consumer") ||
                !args[1].StartsWith("--case-storage-root=", StringComparison.Ordinal))
                throw new ArgumentException("Task16B requires one explicit mode and case storage root.");
            string storage = ValidateStorage(args[1]["--case-storage-root=".Length..]);
            if (args[0] is "--save-only" or "--all")
                ArchivePressureGateIntegrationTests.SaveRewardRoundTrip(storage);
            if (args[0] is "--packed-only" or "--all")
                ArchivePressureGateIntegrationTests.GeneratedArchiveContainsFiniteRewardAndGate();
            if (args[0] is "--parent-metadata-only" or "--all")
                ArchivePressureGateIntegrationTests.ParentStreamMetadataMatchesArchiveInstallation();
            if (args[0] is "--perimeter-only" or "--all")
                await ArchivePressureGateIntegrationTests.GeneratedClosedPerimeterBlocksPhysics(this);
            if (args[0] is "--recorder-only" or "--all")
                await ArchivePressureGateIntegrationTests.RecorderSeesOneRealGateOpen(this);
            if (args[0] is "--recorder-rebind-only" or "--all")
                await ArchivePressureGateIntegrationTests.RecorderRebindBaselinesThenTracksNewMount(this);
            if (args[0] is "--mounted-only" or "--all")
                await ArchivePressureGateIntegrationTests.MountedGateAndFiniteReward(this);
            if (args[0] is "--lifecycle-only" or "--death-reset-only" or "--all")
                await ArchivePressureGateIntegrationTests.MountedCheckpointDeathLifecycle(this,
                    args[0] == "--death-reset-only");
            if (args[0] is "--remount-spent-only" or "--remount-full-only")
                await ArchivePressureGateIntegrationTests.MountedUnloadRemount(this,
                    args[0] == "--remount-spent-only");
            if (args[0] == "--occupied-mount-only")
                await ArchivePressureGateIntegrationTests.GeneratedOccupiedMountStaysSafe(this);
            if (args[0] is "--recovery-same-only" or "--recovery-retained-only" or
                    "--recovery-absent-only")
                await ArchivePressureGateIntegrationTests.RecoveryResetTargetsOnceAfterCompletion(
                    this, args[0] switch
                    {
                        "--recovery-same-only" => "same",
                        "--recovery-retained-only" => "retained",
                        _ => "absent"
                    });
            if (args[0] is "--continue-spent-producer" or "--continue-full-producer")
                await ArchivePressureGateIntegrationTests.ContinueProducer(this, storage,
                    args[0] == "--continue-spent-producer");
            if (args[0] is "--continue-spent-consumer" or "--continue-full-consumer")
                await ArchivePressureGateIntegrationTests.ContinueConsumer(this, storage,
                    args[0] == "--continue-spent-consumer");
            if (args[0] == "--neighbors-only") await RunSafeNeighborRegressions();
            GD.Print("TASK16B_ARCHIVE_GATE_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private async System.Threading.Tasks.Task RunSafeNeighborRegressions()
    {
        CanyonWorldLayoutTests.Run();
        GD.Print("TASK16B_NEIGHBOR_LAYOUT_OK");
        PrologueSaveTests.Run();
        GD.Print("TASK16B_NEIGHBOR_SAVE_OK");
        ProloguePlaytestSessionTests.Run();
        GD.Print("TASK16B_NEIGHBOR_SESSION_OK");
        await ProloguePlaytestRecorderTests.RunTask10TelemetryContracts(this);
        GD.Print("TASK16B_NEIGHBOR_RECORDER_OK");
        ContinuousCanyonReadabilityTests.Run(false);
        GD.Print("TASK16B_NEIGHBOR_READABILITY_OK");
        CanyonMaterialSkinTests.Run(false);
        GD.Print("TASK16B_NEIGHBOR_SKIN_OK");
        await CanyonMechanismRuntimeTests.RunGeneratedSealGate(this);
        GD.Print("TASK16B_NEIGHBOR_SEAL_OK");
    }

    private static string ValidateStorage(string raw)
    {
        if (!Path.IsPathFullyQualified(raw)) throw new ArgumentException("Storage must be absolute.");
        string storage = Path.GetFullPath(raw).TrimEnd(Path.DirectorySeparatorChar);
        string evidence = Path.GetFullPath(
            "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task16b")
            .TrimEnd(Path.DirectorySeparatorChar);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))
            .TrimEnd(Path.DirectorySeparatorChar);
        string expectedProject = Path.GetFullPath(
            "E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555")
            .TrimEnd(Path.DirectorySeparatorChar);
        string caseRoot = Path.GetDirectoryName(storage) ?? "";
        string expectedTemp = Path.Combine(caseRoot, "unit-temp");
        if (!Directory.Exists(storage) || !Directory.Exists(expectedTemp) ||
            !string.Equals(Path.GetDirectoryName(caseRoot), evidence,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(storage), "storage", StringComparison.Ordinal) ||
            !string.Equals(project, expectedProject, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar),
                expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Storage/TMP must be inside a dedicated Task16B case.");
        foreach (string name in new[] { "TMP", "TEMP" })
            if (!string.Equals(Path.GetFullPath(System.Environment.GetEnvironmentVariable(name) ?? "")
                    .TrimEnd(Path.DirectorySeparatorChar), expectedTemp,
                    StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"{name} must resolve inside the dedicated case.");
        for (DirectoryInfo directory = new(storage); directory != null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Storage rejects reparse ancestors.");
        for (DirectoryInfo directory = new(expectedTemp); directory != null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Temporary path rejects reparse ancestors.");
        return storage;
    }
}
