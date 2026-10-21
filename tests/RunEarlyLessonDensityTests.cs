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

// No broad-suite fallback, user:// fixture, or arbitrary mode passthrough.
public partial class RunEarlyLessonDensityTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string storage = ValidateLaunch();
            await AwaitIdentityRelease(storage);
            await PrologueEarlyLessonDensityTests.OrdinaryVisibleAttackGuidanceReplacesDuplicateRibbon(this, storage);
            // These exact related suites are read-only/model or unmounted controls, not unsafe broad fixtures.
            PrologueGuidanceModelTests.Run();
            ProtectedPromptPlacerTests.Run();
            GD.Print("EARLY_LESSON_DENSITY_RELATED_MODEL_PLACER_OK");
            GD.Print("EARLY_LESSON_DENSITY_PASS");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static string ValidateLaunch()
    {
        string[] args = OS.GetCmdlineUserArgs();
        const string prefix = "--case-storage-root=";
        if (args.Length != 2 || args.Count(a => a == "--early-lesson-density") != 1 ||
            args.Count(a => a.StartsWith(prefix, StringComparison.Ordinal)) != 1 ||
            OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"))
            throw new ArgumentException("Task22 requires exactly --early-lesson-density and case storage, no grant.");
        string raw = args.Single(a => a.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];
        if (!Path.IsPathFullyQualified(raw) || raw.Split('/', '\\').Any(p => p is "." or "..") ||
            raw.StartsWith("\\\\", StringComparison.Ordinal) || raw.IndexOf(':', 2) >= 0)
            throw new ArgumentException("Task22 raw storage must be an absolute non-traversing drive path.");
        string storage = Trim(Path.GetFullPath(raw));
        string caseRoot = Path.GetDirectoryName(storage)!;
        string expectedTemp = Path.Combine(caseRoot, "unit-temp");
        string evidence = Trim(Path.GetFullPath("E:/OurGame/Knight/.evidence/september09-continuous-canyon/task22"));
        string repo = Trim(Path.GetFullPath("E:/OurGame/Knight/.worktrees/task6-certified-final-20260902T0555"));
        string userData = Trim(Path.GetFullPath("C:/Users/22499/AppData/Roaming/Godot/app_userdata/Knight"));
        if (!string.Equals(Path.GetDirectoryName(caseRoot), evidence, StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(Path.GetFileName(caseRoot), "^early-lesson-density-[0-9]{8}T[0-9]{9}-[0-9a-f]{32}$") ||
            Path.GetFileName(storage) != "storage" || !Directory.Exists(storage) ||
            Directory.EnumerateFileSystemEntries(storage).Any() || !Directory.Exists(expectedTemp) ||
            Directory.EnumerateFileSystemEntries(expectedTemp).Any() ||
            !string.Equals(Trim(Path.GetFullPath(ProjectSettings.GlobalizePath("res://"))), repo,
                StringComparison.OrdinalIgnoreCase) || IsWithin(storage, repo) || IsWithin(storage, userData) ||
            IsWithin(expectedTemp, repo) || IsWithin(expectedTemp, userData))
            throw new ArgumentException("Task22 storage and unit-temp must be fresh external case-owned in exact checkout.");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !Path.IsPathFullyQualified(value) || value.Split('/', '\\').Any(p => p is "." or "..") ||
                !string.Equals(Trim(Path.GetFullPath(value)), expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Task22 child TMP/TEMP must be case-local.");
        }
        if (!string.Equals(Trim(Path.GetFullPath(Path.GetTempPath())), expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Task22 runtime temporary path must be case-local.");
        foreach (string path in new[] { storage, expectedTemp, repo,
            Path.Combine(repo, "tests/RunEarlyLessonDensityTests.cs"),
            Path.Combine(repo, "tests/integration/PrologueEarlyLessonDensityTests.cs"),
            Path.Combine(repo, "scenes/prologue/PrologueLevel.tscn") }) AssertNoReparse(path);
        return storage;
    }

    // Reviewed neighbor pattern: do not GD.Load/Save until adapter binds retained native PID.
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
                if (File.ReadAllText(release) != pid)
                    throw new InvalidOperationException("Task22 identity release PID mismatch");
                GD.Print("EARLY_LESSON_DENSITY_IDENTITY_RELEASED pid=" + pid);
                return;
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        throw new TimeoutException("Task22 actual PID identity release missing before GD.Load/Save");
    }

    private static void AssertNoReparse(string path)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("Task22 file rejects reparse");
        for (DirectoryInfo current = new(Directory.Exists(path) ? path : Path.GetDirectoryName(path)!);
             current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Task22 rejects reparse ancestors");
    }
    private static bool IsWithin(string path, string parent) =>
        string.Equals(path, parent, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    private static string Trim(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
