#if TASK14_DISPOSE_IDENTIFICATION
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Tests.Integration;

namespace Knight.Tests.Diagnostics;

// One existing adapter fixture, with bounded read-only mounted-resource observations.
public partial class RunTask14NativeIdentityDiagnostic : SceneTree
{
    private const string StorageArgument = "--case-storage-root=";
    private const string ExactLevelPath = "res://scenes/prologue/PrologueLevel.tscn";
    private int _levelsObserved;
    private int _treeExited;
    private int _distinctDefinitions;
    private string _callbackFailure = string.Empty;

    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 1 || !args[0].StartsWith(StorageArgument, StringComparison.Ordinal))
                throw new InvalidOperationException("native-identity diagnostic requires exactly one --case-storage-root");
            string storage = args[0][StorageArgument.Length..];
            if (!Path.IsPathFullyQualified(storage) || !Directory.Exists(storage))
                throw new InvalidOperationException("case storage must be an existing absolute directory");
            Task14NativeResourceIdentity.DeclareMainManagedThread();
            string cases = Path.Combine(Path.GetFullPath(storage), "full-regression");
            string[] before = AdapterCases(cases);
            GD.Print("TASK14_NATIVE_IDENTITY_BEGIN scope=one-existing-adapter-fixture game_acceptance=not_claimed");
            NodeAdded += ObserveAddedNode;
            try
            {
                await Task14TelegraphAdapterTests.Run(this);
            }
            finally
            {
                NodeAdded -= ObserveAddedNode;
            }
            string[] after = AdapterCases(cases);
            string[] fresh = after.Except(before, StringComparer.OrdinalIgnoreCase).ToArray();
            if (fresh.Length != 1 || after.Length != before.Length + 1)
                throw new InvalidOperationException(
                    $"adapter fixture created {fresh.Length} fresh paths; expected exactly one");
            if (_callbackFailure.Length != 0)
                throw new InvalidOperationException($"mounted snapshot failed: {_callbackFailure}");
            if (_levelsObserved != 1 || _treeExited != 1 || _distinctDefinitions != 5)
                throw new InvalidOperationException(
                    $"mounted observation incomplete: levels={_levelsObserved} tree_exited={_treeExited} distinct_sectors={_distinctDefinitions}");
            GD.Print($"TASK14_NATIVE_IDENTITY_ADAPTER_OK fresh_case={fresh[0]} " +
                     $"levels={_levelsObserved} tree_exited={_treeExited} sectors={_distinctDefinitions}");
            GD.Print("TASK14_NATIVE_IDENTITY_COLLECT_BEGIN");
            GC.Collect(); // Same post-fixture schedule as the earlier one-round comparison.
            GD.Print("TASK14_NATIVE_IDENTITY_COLLECT_RETURNED");
            GD.Print("TASK14_NATIVE_IDENTITY_DRAIN_BEGIN");
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("task14-native-identity");
            GD.Print("TASK14_NATIVE_IDENTITY_DIAGNOSTIC_COMPLETE game_acceptance=not_claimed");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"TASK14_NATIVE_IDENTITY_FAILURE {exception}");
            Quit(1);
        }
    }

    private void ObserveAddedNode(Node node)
    {
        if (node is not PrologueLevelController level || level.SceneFilePath != ExactLevelPath)
            return;
        _levelsObserved++;
        if (_levelsObserved > 1)
        {
            _callbackFailure = "more than one exact PrologueLevel mounted";
            return;
        }
        try
        {
            ulong levelId = level.GetInstanceId();
            var weak = new WeakReference<PrologueLevelController>(level);
            Task14NativeResourceIdentity.SnapshotResult snapshot =
                Task14NativeResourceIdentity.ObserveMountedLevel(level, "NodeAdded",
                    includeSceneDependencies: true);
            _distinctDefinitions = snapshot.DistinctSectorDefinitions;
            GD.Print($"TASK14_NATIVE_IDENTITY_LEVEL_ADDED native_id={levelId} " +
                     $"sectors={snapshot.DistinctSectorDefinitions} " +
                     $"boss_records={snapshot.BossTuningRecords} " +
                     $"profile_records={snapshot.PresentationProfileRecords}");
            level.TreeExiting += () =>
            {
                if (!weak.TryGetTarget(out PrologueLevelController live) ||
                    !GodotObject.IsInstanceValid(live))
                {
                    _callbackFailure = "TreeExiting weak mounted-level target missing or invalid";
                    return;
                }
                try
                {
                    Task14NativeResourceIdentity.ObserveMountedLevel(live, "TreeExiting",
                        includeSceneDependencies: false);
                }
                catch (Exception exception)
                {
                    _callbackFailure = $"TreeExiting snapshot: {exception}";
                }
            };
            level.TreeExited += () =>
            {
                _treeExited++;
                GD.Print($"TASK14_NATIVE_IDENTITY_LEVEL_TREE_EXITED native_id={levelId}");
            };
        }
        catch (Exception exception)
        {
            _callbackFailure = $"NodeAdded snapshot: {exception}";
        }
    }

    private static string[] AdapterCases(string cases) => Directory.Exists(cases)
        ? Directory.GetDirectories(cases, "task14-firefly-sentry-*")
        : Array.Empty<string>();
}
#endif
