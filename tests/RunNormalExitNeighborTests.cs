using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Shared;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

// Narrow fresh-process neighbors, never a normal lesson/combat/full-route runner.
public partial class RunNormalExitNeighborTests : SceneTree
{
    private static PackedScene _mainSceneLifetimeOwner;
    private const string FlowSource = "scripts/prologue/PrologueGameFlowController.cs";

    public override async void _Initialize()
    {
        try
        {
            string mode = null;
            try
            {
                (string storage, string selected) = ValidateLaunch();
                mode = selected;
                await AwaitIdentityRelease(storage);
                _mainSceneLifetimeOwner = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                    ?? throw new InvalidOperationException("neighbor normal packed scene is missing");
                TestAssert.True(GodotObject.IsInstanceValid(_mainSceneLifetimeOwner),
                    "neighbor keeps normal packed resource graph alive for the process");
                AssertWarningMatcherGuards();
                using (var expectedWarnings = new ExpectedWarningLogger())
                {
                    switch (mode)
                    {
                        case "migration":
                            PrologueSaveTests.Run();
                            PrologueProgressTests.Run();
                            ContinuousCanyonMigrationTests.RunStore();
                            await ContinuousCanyonMigrationTests.RunMenus(this);
                            await ContinuousCanyonSaveIsolationTests.Run(this);
                            expectedWarnings.AssertExact(ExpectedWarningLogger.GameFlowNegativeExpectations());
                            break;
                        case "flow":
                            await PrologueGameFlowTests.RunTask7IndependentCases(this);
                            expectedWarnings.AssertExact(new ExpectedWarningLogger.Expected(
                                "Rejected prologue flow transition Completed->Recovering.", FlowSource, "Reject", 2));
                            break;
                        case "sample-exit":
                            await BasinSampleBoundaryTests.Run(this, "exit");
                            expectedWarnings.AssertExact();
                            break;
                        default: throw new ArgumentException("unknown exit neighbor");
                    }
                }
                GD.Print(WarningSentinel(mode));
                if (mode == "migration") GD.Print("TASK7_MIGRATION_STORE_OK");
            }
            finally
            {
                ReleaseInputs();
                Paused = false;
            }
            GD.Print($"NORMAL_EXIT_NEIGHBOR_ALL_OK mode={mode} fixture=true fullRoute=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static string WarningSentinel(string mode) => mode switch
    {
        "migration" => $"NORMAL_EXIT_NEIGHBOR_WARNINGS_OK mode=migration title=6 completed=1 source={FlowSource} function=Reject",
        "flow" => $"NORMAL_EXIT_NEIGHBOR_WARNINGS_OK mode=flow title=0 completed=2 source={FlowSource} function=Reject",
        "sample-exit" => "NORMAL_EXIT_NEIGHBOR_WARNINGS_OK mode=sample-exit title=0 completed=0 source=none function=none",
        _ => throw new ArgumentException("unknown exit neighbor warning contract")
    };

    private static void AssertWarningMatcherGuards()
    {
        ExpectedWarningLogger.AssertMatcherGuards();
        var expected = new ExpectedWarningLogger.Expected(
            "Rejected prologue flow transition Completed->Recovering.", FlowSource, "Reject", 1);
        var wrongSource = new ExpectedWarningLogger.Warning("Reject", "tests/WrongSource.cs", 1,
            string.Empty, expected.Message, (int)Logger.ErrorType.Warning);
        var wrongFunction = new ExpectedWarningLogger.Warning("WrongFunction", FlowSource, 1,
            string.Empty, expected.Message, (int)Logger.ErrorType.Warning);
        TestAssert.True(ExpectedWarningLogger.FindExactFailure(new[] { wrongSource }, new[] { expected }) != null,
            "neighbor warning matcher rejects wrong source with matching message/count");
        TestAssert.True(ExpectedWarningLogger.FindExactFailure(new[] { wrongFunction }, new[] { expected }) != null,
            "neighbor warning matcher rejects wrong function with matching message/count");
    }

    private static (string Storage, string Mode) ValidateLaunch()
    {
        string[] args = OS.GetCmdlineUserArgs();
        const string storagePrefix = "--case-storage-root=";
        const string modePrefix = "--exit-neighbor=";
        if (args.Length != 2 || args.Count(a => a.StartsWith(storagePrefix, StringComparison.Ordinal)) != 1 ||
            args.Count(a => a.StartsWith(modePrefix, StringComparison.Ordinal)) != 1 ||
            OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"))
            throw new ArgumentException("neighbors require exact isolated arguments and no hands-on grant");
        string mode = args.Single(a => a.StartsWith(modePrefix, StringComparison.Ordinal))[modePrefix.Length..];
        if (mode is not ("migration" or "flow" or "sample-exit"))
            throw new ArgumentException("unknown exit neighbor mode");
        string raw = args.Single(a => a.StartsWith(storagePrefix, StringComparison.Ordinal))[storagePrefix.Length..];
        if (!Path.IsPathFullyQualified(raw)) throw new ArgumentException("neighbor raw storage must be fully absolute");
        string storage = Trim(Path.GetFullPath(raw));
        string caseRoot = Path.GetDirectoryName(storage)!;
        string expectedTemp = Path.Combine(caseRoot, "unit-temp");
        string repo = Trim(Path.GetFullPath("E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555"));
        if (!string.Equals(Path.GetDirectoryName(caseRoot), Trim(Path.GetFullPath(
                "E:/OurGame/Knight/.evidence/september09-continuous-canyon/task10")), StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(storage) != "storage" || !Directory.Exists(storage) ||
            Directory.EnumerateFileSystemEntries(storage).Any() || !Directory.Exists(expectedTemp) ||
            !string.Equals(Trim(Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))), repo, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("neighbor storage must be empty external case-local in the exact linked checkout");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !Path.IsPathFullyQualified(value) ||
                !string.Equals(Trim(Path.GetFullPath(value)), expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("neighbor child TMP/TEMP must be case-local");
        }
        if (!string.Equals(Trim(Path.GetFullPath(Path.GetTempPath())), expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("neighbor runtime temporary path must be case-local");
        foreach (string path in new[] { storage, expectedTemp, repo,
            Path.Combine(repo, "tests/RunNormalExitNeighborTests.cs"),
            Path.Combine(repo, "scenes/prologue/PrologueLevel.tscn") }) AssertNoReparse(path);
        return (storage, mode);
    }

    private async Task AwaitIdentityRelease(string storage)
    {
        string release = Path.Combine(Path.GetDirectoryName(storage)!, "tests-identity.release");
        string pid = OS.GetProcessId().ToString(CultureInfo.InvariantCulture);
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (File.Exists(release))
            {
                AssertNoReparse(release);
                if (File.ReadAllText(release) != pid) throw new InvalidOperationException("neighbor identity release PID mismatch");
                GD.Print("NORMAL_EXIT_NEIGHBOR_IDENTITY_RELEASED pid=" + pid);
                return;
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        throw new TimeoutException("neighbor actual PID identity release missing before GD.Load/mount");
    }

    private static void AssertNoReparse(string path)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("neighbor file rejects reparse");
        for (DirectoryInfo current = new(Directory.Exists(path) ? path : Path.GetDirectoryName(path)!);
             current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("neighbor rejects reparse ancestors");
    }
    private static string Trim(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    private static void ReleaseInputs()
    {
        foreach (var action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.PrimaryAction, InputActions.Shield, InputActions.Jump,
            InputActions.Dash, InputActions.SelectAttackMode, InputActions.SelectGrappleMode })
            Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false, Strength = 0f });
        Input.FlushBufferedEvents();
    }
}
