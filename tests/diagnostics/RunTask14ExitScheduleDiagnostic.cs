using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Tests;
using Knight.Tests.Integration;

namespace Knight.Tests.Diagnostics;

// Ordinary, bounded exit-schedule comparison; no dump or cache instrumentation.
public partial class RunTask14ExitScheduleDiagnostic : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string[] roundOptions = args.Where(arg => arg.StartsWith("--rounds=", StringComparison.Ordinal))
                .ToArray();
            string[] storageOptions = args.Where(arg =>
                arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)).ToArray();
            if (args.Length != 2 || roundOptions.Length != 1 || storageOptions.Length != 1 ||
                (roundOptions[0] != "--rounds=1" && roundOptions[0] != "--rounds=4"))
                throw new InvalidOperationException(
                    "exit-schedule diagnostic requires exactly --rounds=1|4 and one --case-storage-root");

            int rounds = roundOptions[0] == "--rounds=1" ? 1 : 4;
            string storage = storageOptions[0]["--case-storage-root=".Length..];
            if (!Path.IsPathFullyQualified(storage) || !Directory.Exists(storage))
                throw new InvalidOperationException("exit-schedule storage must be an existing absolute directory");

#if TASK14_DISPOSE_IDENTIFICATION
            throw new InvalidOperationException("exit-schedule comparison requires the ordinary unflagged build");
#else
            GD.Print("TASK14_EXIT_SCHEDULE_PROBE_COMPILED=false");
#endif
            string cases = Path.Combine(Path.GetFullPath(storage), "full-regression");
            GD.Print($"TASK14_EXIT_SCHEDULE_BEGIN rounds={rounds} mode=fixture-default");
            for (int round = 1; round <= rounds; round++)
            {
                string[] before = AdapterCasePaths(cases);
                GD.Print($"TASK14_EXIT_SCHEDULE_ROUND_BEGIN rounds={rounds} round={round} existing_cases={before.Length}");
                await Task14TelegraphAdapterTests.Run(this);
                string[] after = AdapterCasePaths(cases);
                string[] fresh = after.Except(before, StringComparer.OrdinalIgnoreCase).ToArray();
                if (fresh.Length != 1 || after.Length != before.Length + 1)
                    throw new InvalidOperationException(
                        $"round {round} created {fresh.Length} fresh adapter cases; expected exactly one");
                GD.Print($"TASK14_EXIT_SCHEDULE_ROUND_DONE rounds={rounds} round={round} case_storage={fresh[0]}");
                GD.Print($"TASK14_EXIT_SCHEDULE_COLLECT_BEGIN rounds={rounds} round={round}");
                GC.Collect(); // Identical post-round collection in both arms; no finalizer wait here.
                GD.Print($"TASK14_EXIT_SCHEDULE_COLLECT_RETURNED rounds={rounds} round={round}");
            }
            GD.Print($"TASK14_EXIT_SCHEDULE_DRAIN_BEGIN rounds={rounds}");
            TestSceneTeardown.DrainManagedWrappersAtProcessExit($"task14-exit-schedule-{rounds}");
            GD.Print($"TASK14_EXIT_SCHEDULE_OK rounds={rounds}");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static string[] AdapterCasePaths(string cases) => Directory.Exists(cases)
        ? Directory.GetDirectories(cases, "task14-firefly-sentry-*")
        : Array.Empty<string>();
}
