using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

// Exactly fourteen explicit fixtures; neither model nor mounted local physics is full-route proof.
public partial class RunTask18TempleGateTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            (string storage, string mode) = ValidateLaunch();
            await AwaitIdentityRelease(storage);
            if (mode == "packed") TemplePressureGateInstallationTests.GeneratedTempleContainsFiniteRewardAndGate();
            else if (mode == "configuration") TemplePressureGateInstallationTests.GateConfigurationRoundTrip();
            else if (mode == "access") await TemplePressureGateAccessPhysicsTests.Access(this);
            else if (mode == "jump-access") await TemplePressureGateAccessPhysicsTests.JumpAccess(this);
            else if (mode == "west-access") await TemplePressureGateAccessPhysicsTests.WestAccess(this);
            else if (mode == "closed-chain") await TemplePressureGateAccessPhysicsTests.ClosedChainAccess(this);
            else if (mode == "regional-reward") await TemplePressureGateAccessPhysicsTests.RegionalReward(this);
            else if (mode == "space-exit") await TemplePressureGateAccessPhysicsTests.SpaceExit(this);
            else if (mode == "laborer") await TemplePressureGateAccessPhysicsTests.LaborerClearance(this);
            else if (mode == "reward") TemplePressureGateInstallationTests.SaveRewardRoundTrip(storage);
            else if (mode == "swing-transition")
            {
                MechanicalClawModelTests.RunArrivalTransitionRegression();
                await MechanicalClawAutoPullRuntimeTests.RunArrivalTransitionRegression(this);
            }
            else if (mode == "claw-regression")
            {
                MechanicalClawModelTests.Run();
                GD.Print("TASK18_CLAW_FULL_MODEL_OK");
                MechanicalClawArrivalGeometryTests.Run();
                GD.Print("TASK18_CLAW_FULL_GEOMETRY_OK");
                await MechanicalClawAutoPullRuntimeTests.Run(this);
                GD.Print("TASK18_CLAW_FULL_RUNTIME_OK");
            }
            else if (mode == "temple-guidance")
            {
                PrologueGuidanceModelTests.RunTempleTerrainGuidance();
                GD.Print("TASK18_TEMPLE_GUIDANCE_MODEL_OK");
            }
            else if (mode == "temple-guidance-runtime")
                await TemplePressureGateAccessPhysicsTests.TempleGuidanceRuntime(this);
            else throw new ArgumentException("Unknown explicit Task18 fixture mode.");
            GD.Print($"TASK18_TEMPLE_GATE_ALL_OK mode={mode} fixture=true fullRoute=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static (string Storage, string Mode) ValidateLaunch()
    {
        string[] args = OS.GetCmdlineUserArgs();
        const string prefix = "--case-storage-root=";
        string[] selections = { "--packed-placement-only", "--reward-save-only", "--gate-configuration-only",
            "--temple-access-physics-only", "--temple-laborer-clearance-only", "--temple-jump-access-only",
            "--temple-west-access-only", "--claw-swing-transition-only", "--temple-closed-chain-only",
            "--claw-full-regression-only", "--temple-regional-reward-only", "--temple-space-exit-only",
            "--temple-guidance-only", "--temple-guidance-runtime-only" };
        if (args.Length != 2 || args.Count(a => selections.Contains(a, StringComparer.Ordinal)) != 1 ||
            args.Count(a => a.StartsWith(prefix, StringComparison.Ordinal)) != 1 ||
            OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"))
            throw new ArgumentException("Task18 requires exactly one explicit fixture selection and case storage, no grant.");
        string mode = args.Contains("--packed-placement-only", StringComparer.Ordinal) ? "packed" :
            args.Contains("--gate-configuration-only", StringComparer.Ordinal) ? "configuration" :
            args.Contains("--temple-access-physics-only", StringComparer.Ordinal) ? "access" :
            args.Contains("--temple-jump-access-only", StringComparer.Ordinal) ? "jump-access" :
            args.Contains("--temple-west-access-only", StringComparer.Ordinal) ? "west-access" :
            args.Contains("--temple-laborer-clearance-only", StringComparer.Ordinal) ? "laborer" :
            args.Contains("--reward-save-only", StringComparer.Ordinal) ? "reward" :
            args.Contains("--claw-swing-transition-only", StringComparer.Ordinal) ? "swing-transition" :
            args.Contains("--temple-closed-chain-only", StringComparer.Ordinal) ? "closed-chain" :
            args.Contains("--temple-regional-reward-only", StringComparer.Ordinal) ? "regional-reward" :
            args.Contains("--temple-space-exit-only", StringComparer.Ordinal) ? "space-exit" :
            args.Contains("--claw-full-regression-only", StringComparer.Ordinal) ? "claw-regression" :
            args.Contains("--temple-guidance-only", StringComparer.Ordinal) ? "temple-guidance" :
            args.Contains("--temple-guidance-runtime-only", StringComparer.Ordinal) ? "temple-guidance-runtime" :
            throw new ArgumentException("Unknown explicit Task18 fixture selection.");
        string raw = args.Single(a => a.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];
        if (!Path.IsPathFullyQualified(raw) || raw.Split('/', '\\').Any(p => p is "." or "..") ||
            raw.StartsWith("\\\\", StringComparison.Ordinal) || raw.IndexOf(':', 2) >= 0)
            throw new ArgumentException("Task18 raw storage must be an absolute non-traversing drive path.");
        string storage = Trim(Path.GetFullPath(raw));
        string caseRoot = Path.GetDirectoryName(storage)!;
        string expectedTemp = Path.Combine(caseRoot, "unit-temp");
        string evidence = Trim(Path.GetFullPath("E:/OurGame/Knight/.evidence/september09-continuous-canyon/task18"));
        string repo = Trim(Path.GetFullPath("E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555"));
        string userData = Trim(Path.GetFullPath("C:/Users/22499/AppData/Roaming/Godot/app_userdata/Knight"));
        if (!string.Equals(Path.GetDirectoryName(caseRoot), evidence, StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(Path.GetFileName(caseRoot), "^[a-z0-9-]{1,40}-[0-9]{8}T[0-9]{9}-[0-9a-f]{32}$") ||
            Path.GetFileName(storage) != "storage" || !Directory.Exists(storage) ||
            Directory.EnumerateFileSystemEntries(storage).Any() || !Directory.Exists(expectedTemp) ||
            !string.Equals(Trim(Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))), repo,
                StringComparison.OrdinalIgnoreCase) || IsWithin(storage, repo) || IsWithin(storage, userData) ||
            IsWithin(expectedTemp, repo) || IsWithin(expectedTemp, userData))
            throw new ArgumentException("Task18 storage must be fresh external case-owned in exact checkout.");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !Path.IsPathFullyQualified(value) || value.Split('/', '\\').Any(p => p is "." or "..") ||
                !string.Equals(Trim(Path.GetFullPath(value)), expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Task18 child TMP/TEMP must be case-local.");
        }
        if (!string.Equals(Trim(Path.GetFullPath(Path.GetTempPath())), expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Task18 runtime temporary path must be case-local.");
        foreach (string path in new[] { storage, expectedTemp, repo,
            Path.Combine(repo, "tests/RunTask18TempleGateTests.cs"),
            Path.Combine(repo, "scenes/prologue/sectors/LostTemple.tscn"),
            Path.Combine(repo, "scenes/prologue/PrologueLevel.tscn") }) AssertNoReparse(path);
        return (storage, mode);
    }

    // Same retained-PID release handshake as the reviewed fresh-process neighbor runner.
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
                if (File.ReadAllText(release) != pid) throw new InvalidOperationException("Task18 identity release PID mismatch");
                GD.Print("TASK18_TEMPLE_IDENTITY_RELEASED pid=" + pid);
                return;
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        throw new TimeoutException("Task18 actual PID identity release missing before GD.Load/Save");
    }

    private static void AssertNoReparse(string path)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("Task18 file rejects reparse");
        for (DirectoryInfo current = new(Directory.Exists(path) ? path : Path.GetDirectoryName(path)!);
             current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Task18 rejects reparse ancestors");
    }
    private static bool IsWithin(string path, string parent) =>
        string.Equals(path, parent, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static string Trim(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
