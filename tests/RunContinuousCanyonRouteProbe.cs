using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Tests.Presentation;
using Knight.Shared;

public partial class RunContinuousCanyonRouteProbe : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--physical-exit-only"))
            {
                await Knight.Tests.Integration.NormalWorldPhysicalExitTests.Run(this);
                GD.Print("NORMAL_WORLD_PHYSICAL_EXIT_ALL_OK cases=7 fixture=true fullRoute=false");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--telemetry-only"))
            {
                await AwaitTelemetryIdentityRelease();
                ContinuousCanyonRouteProbe.RunTelemetryContract();
                GD.Print("CONTINUOUS_CANYON_TELEMETRY_CONTRACT_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--storage-guard-only"))
            {
                await ContinuousCanyonRouteProbe.RunStorageGuard(this);
                GD.Print("CONTINUOUS_CANYON_STORAGE_GUARD_OK");
                Quit(0);
                return;
            }
            await ContinuousCanyonRouteProbe.Run(this);
            GD.Print("CONTINUOUS_CANYON_ROUTE_PROBE_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight,
                         InputActions.MoveUp, InputActions.MoveDown, InputActions.Jump,
                         InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
                         InputActions.PrimaryAction, InputActions.Shield })
                Input.ActionRelease(action);
            await ToSignal(this, SignalName.ProcessFrame);
            for (int frame = 0; frame < 4; frame++)
                await ToSignal(this, SignalName.PhysicsFrame);
            Quit(1);
        }
    }

    private async Task AwaitTelemetryIdentityRelease()
    {
        string[] args = OS.GetCmdlineUserArgs();
        const string storagePrefix = "--case-storage-root=";
        const string variantPrefix = "--route-variant=";
        if (args.Length != 3 || args.Count(a => a.StartsWith(storagePrefix, StringComparison.Ordinal)) != 1 ||
            args.Count(a => a.StartsWith(variantPrefix, StringComparison.Ordinal)) != 1 ||
            args.Count(a => a == "--telemetry-only") != 1 || OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"))
            throw new ArgumentException("telemetry requires exact isolated arguments and no hands-on grant");
        string variant = args.Single(a => a.StartsWith(variantPrefix, StringComparison.Ordinal))[variantPrefix.Length..];
        if (variant is not ("main" or "exploration"))
            throw new ArgumentException("telemetry route variant must be main or exploration");
        string raw = args.Single(a => a.StartsWith(storagePrefix, StringComparison.Ordinal))[storagePrefix.Length..];
        string storage = QualifiedPath(raw);
        string caseRoot = Path.GetDirectoryName(storage)!;
        string evidenceRoot = Path.GetFullPath("E:/OurGame/Knight/.evidence/september09-continuous-canyon/task10");
        string expectedTemp = Path.Combine(caseRoot, "unit-temp");
        if (!string.Equals(Path.GetDirectoryName(caseRoot), evidenceRoot, StringComparison.OrdinalIgnoreCase) ||
            !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(caseRoot),
                "^[a-z0-9-]{1,40}-[0-9]{8}T[0-9]{9}-[0-9a-f]{32}$") ||
            Path.GetFileName(storage) != "storage")
            throw new ArgumentException("telemetry storage must be an exact unique external Task10 case child");
        NoReparse(storage);
        NoReparse(expectedTemp);
        if (!Directory.Exists(storage) || Directory.EnumerateFileSystemEntries(storage).Any() ||
            !Directory.Exists(expectedTemp))
            throw new ArgumentException("telemetry requires empty case-local storage and existing unit-temp");
        foreach (string name in new[] { "TMP", "TEMP" })
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            if (value == null || !string.Equals(QualifiedPath(value), expectedTemp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("telemetry child TMP/TEMP must be case-local");
        }
        if (!string.Equals(QualifiedPath(Path.GetTempPath()), expectedTemp, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("telemetry runtime temporary path must be case-local");
        string release = Path.Combine(caseRoot, "tests-identity.release");
        string pid = OS.GetProcessId().ToString(CultureInfo.InvariantCulture);
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(10))
        {
            NoReparse(release);
            if (Directory.Exists(release)) throw new ArgumentException("telemetry identity marker must be a file");
            if (File.Exists(release))
            {
                if (File.ReadAllText(release) != pid)
                    throw new InvalidOperationException("telemetry identity release PID mismatch");
                GD.Print("TASK10_TELEMETRY_IDENTITY_RELEASED pid=" + pid);
                return;
            }
            await ToSignal(this, SignalName.ProcessFrame);
        }
        throw new TimeoutException("telemetry actual PID identity release missing before pure contract tests");

        static string QualifiedPath(string value)
        {
            if (!Path.IsPathFullyQualified(value) ||
                value.Split(new[] { '\\', '/' }).Any(segment => segment is "." or ".."))
                throw new ArgumentException("telemetry infrastructure path must be untouched fully absolute without traversal");
            return Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        static void NoReparse(string path)
        {
            for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("telemetry rejects reparse marker or ancestors");
        }
    }
}
