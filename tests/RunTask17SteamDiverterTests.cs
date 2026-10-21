#nullable enable
using Godot;
using Knight.Tests.Unit;
using Knight.Tests.Integration;
using Knight.Tests.Presentation;
using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Knight.Tests;

// Component-only: no top-level game/save/settings/recorder is instantiated.
public partial class RunTask17SteamDiverterTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 2 || args[0] is not ("--model-first-handoff" or "--model-contracts" or "--mounted-default" or "--clock-contracts" or "--functional-runtime" or "--component-regression" or "--physical-contracts" or "--visual-contracts" or "--component-proof") ||
                !args[1].StartsWith("--case-storage-root=", StringComparison.Ordinal))
                throw new ArgumentException("Task17A requires the explicit model mode and isolated case root.");
            string storage = args[1].Substring("--case-storage-root=".Length);
            ValidateStorage(storage);
            await AwaitIdentityRelease(storage);
            if (args[0] == "--model-first-handoff") CanyonSteamDiverterModelTests.RunFirstHandoff();
            if (args[0] == "--model-contracts") CanyonSteamDiverterModelTests.RunRemainingContracts();
            if (args[0] == "--mounted-default") await CanyonSteamDiverterRuntimeTests.RunMountedDefault(this);
            if (args[0] == "--clock-contracts") CanyonSteamDiverterClockTests.RunContracts();
            if (args[0] == "--functional-runtime") await CanyonSteamDiverterRuntimeTests.RunFunctionalContracts(this);
            if (args[0] == "--physical-contracts") await CanyonSteamDiverterPhysicalTests.RunContracts(this);
            if (args[0] == "--visual-contracts") await CanyonSteamDiverterVisualTests.RunContracts(this);
            if (args[0] == "--component-proof") await CanyonSteamDiverterComponentProbe.Run(this);
            if (args[0] == "--component-regression")
            {
                GD.Print("TASK17A_GROUP_BEGIN first-handoff");
                CanyonSteamDiverterModelTests.RunFirstHandoff();
                GD.Print("TASK17A_GROUP_OK first-handoff");
                GD.Print("TASK17A_GROUP_BEGIN remaining-model");
                CanyonSteamDiverterModelTests.RunRemainingContracts();
                GD.Print("TASK17A_GROUP_OK remaining-model");
                GD.Print("TASK17A_GROUP_BEGIN clock-contracts");
                CanyonSteamDiverterClockTests.RunContracts();
                GD.Print("TASK17A_GROUP_OK clock-contracts");
                GD.Print("TASK17A_GROUP_BEGIN mounted-default");
                await CanyonSteamDiverterRuntimeTests.RunMountedDefault(this);
                GD.Print("TASK17A_GROUP_OK mounted-default");
                GD.Print("TASK17A_GROUP_BEGIN functional-runtime");
                await CanyonSteamDiverterRuntimeTests.RunFunctionalContracts(this);
                GD.Print("TASK17A_GROUP_OK functional-runtime");
                GD.Print("TASK17A_GROUP_BEGIN legacy-mechanism-model");
                CanyonMechanismModelTests.Run();
                GD.Print("TASK17A_GROUP_OK legacy-mechanism-model");
                GD.Print("TASK17A_GROUP_BEGIN legacy-required-lifecycle");
                await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(this);
                GD.Print("TASK17A_GROUP_OK legacy-required-lifecycle");
                GD.Print("TASK17A_GROUP_BEGIN visual-contracts");
                await CanyonSteamDiverterVisualTests.RunContracts(this);
                GD.Print("TASK17A_GROUP_OK visual-contracts");
                GD.Print("TASK17A_GROUP_BEGIN physical-contracts");
                await CanyonSteamDiverterPhysicalTests.RunContracts(this);
                GD.Print("TASK17A_GROUP_OK physical-contracts");
            }
            GD.Print("TASK17A_STEAM_DIVERTER_TESTS_OK");
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
                    throw new InvalidOperationException("Task17A identity release must be a regular marker matching this owned PID.");
                GD.Print("TASK17A_IDENTITY_RELEASED pid=" + expectedPid);
                return;
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        throw new TimeoutException("Task17A actual identity release was not received within the bounded 10 second startup wait.");
    }

    private static void ValidateStorage(string raw)
    {
        if (!Path.IsPathFullyQualified(raw))
            throw new ArgumentException("Task17A case root must be absolute.");
        string storage = Trim(Path.GetFullPath(raw));
        string evidence = Trim(Path.GetFullPath(
            "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task17a"));
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
            throw new ArgumentException("Task17A requires an existing unique external evidence case.");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string? value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !string.Equals(Trim(Path.GetFullPath(value)), expectedTemp,
                    StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Task17A {name} must resolve inside the isolated case.");
        }
        foreach (string path in new[] { storage, expectedTemp })
            for (DirectoryInfo? directory = new(path); directory != null; directory = directory.Parent)
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Task17A case paths reject reparse ancestors.");
    }

    private static string Trim(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
