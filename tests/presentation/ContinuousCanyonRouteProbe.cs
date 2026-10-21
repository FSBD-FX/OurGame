using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests.Presentation;

/// <summary>
/// Reusable actual-input route proof. The standalone runner and the later visible diagnostic
/// share this entry point so headless acceptance cannot drift away from the rendered route.
/// </summary>
public static class ContinuousCanyonRouteProbe
{
    public static async Task Run(SceneTree tree)
    {
        AssertOrderedTelemetryContract();
        if (Array.Exists(OS.GetCmdlineUserArgs(), value =>
                value == "--gear-entry-window-only"))
        {
            CanyonWorldLayoutTests.Run();
            PrologueCanyonStructureTests.RunTask4();
            await PrologueCanyonGeneratedMainRouteTests.RunTask10GearEntryWindow(tree);
            GD.Print("CONTINUOUS_CANYON_GEAR_ENTRY_WINDOW_OK");
            return;
        }
        string variant = Array.Find(OS.GetCmdlineUserArgs(), value =>
            value.StartsWith("--route-variant=", StringComparison.Ordinal));
        TestAssert.True(variant != null,
            "Task10 route probe requires --route-variant=main or exploration");
        PrologueCanyonGeneratedMainRouteTests.Task10RouteVariant selected = variant switch
        {
            "--route-variant=main" =>
                PrologueCanyonGeneratedMainRouteTests.Task10RouteVariant.Main,
            "--route-variant=exploration" =>
                PrologueCanyonGeneratedMainRouteTests.Task10RouteVariant.Exploration,
            _ => throw new InvalidOperationException($"unsupported Task10 route variant {variant}")
        };
        Stopwatch postludeTimer = Stopwatch.StartNew();
        void TracePostlude(string boundary)
        {
            // Diagnostic output must not decide route acceptance.
            try
            {
                GD.Print($"TASK10_ROUTE_POSTLUDE boundary={boundary} " +
                    $"elapsed_ticks={postludeTimer.ElapsedTicks} ticks_per_second={Stopwatch.Frequency}");
            }
            catch (Exception) { }
        }
        TracePostlude("RUN_TASK10_BEGIN");
        PrologueContinuousRouteResult route =
            await PrologueCanyonGeneratedMainRouteTests.RunTask10(tree, selected);
        TracePostlude("RUN_TASK10_RETURN");
        GD.Print($"TASK18_ENCOUNTER_EVIDENCE routeVariant={selected} " +
            $"EncounterActivations=[{string.Join(",", route.EncounterActivations.OrderBy(id => id, StringComparer.Ordinal))}] " +
            $"EncounterDefeats=[{string.Join(",", route.EncounterDefeats.OrderBy(id => id, StringComparer.Ordinal))}] " +
            $"EncounterDefeatsExceptEncounterActivations=[{string.Join(",", route.EncounterDefeats
                .Except(route.EncounterActivations, StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal))}]");
        TracePostlude("ASSERT_COMPLETE_BEGIN");
        route.AssertComplete(selected.ToString());
        if (tree is Knight.Tests.RunRealtimePrologueProbe)
        {
            TestAssert.True(route.NormalRangedExerciseRequired,
                "RANGED_NORMAL_ROLE_EXERCISE_RED NORMAL runner must retain actual per-instance receipts");
            TestAssert.True(route.RecoveryRequests == 0 && route.RecoveryCompletions == 0,
                "RANGED_NORMAL_ZERO_RECOVERY_RED NORMAL success artifact requires zero whole-route recovery");
            PrologueCanyonGeneratedMainRouteTests.AssertNormalRangedExercise(
                selected.ToString(), route.NormalRangedInstances);
            GD.Print("RANGED_NORMAL_INSTANCE_RECEIPTS " +
                System.Text.Json.JsonSerializer.Serialize(route.NormalRangedInstances));
        }
        TracePostlude("ASSERT_COMPLETE_END");
        const string storagePrefix = "--case-storage-root=";
        string storage = OS.GetCmdlineUserArgs().Single(value =>
            value.StartsWith(storagePrefix, StringComparison.Ordinal))[storagePrefix.Length..];
        string resultPath = Task10RouteResultArtifact.Persist(route, selected.ToString(), storage,
            syntheticFixture: false, diagnostic: TracePostlude);
        TracePostlude("PERSIST_RETURN");
        GD.Print($"TASK18_ROUTE_RESULT_WRITTEN path={resultPath} version=1 proofKind=automated " +
            "syntheticFixture=false humanValidated=false operationCoverageComplete=false fullGoalAccepted=false");
    }

    public static void RunTelemetryContract()
    {
        AssertOrderedTelemetryContract();
        Task10RouteProofContractTests.Run();
        Task10RouteProofContractTests.RunArtifactContract();
    }

    public static Task RunStorageGuard(SceneTree tree) =>
        PrologueCanyonGeneratedMainRouteTests.RunTask10StorageGuard(tree);

    private static void AssertOrderedTelemetryContract()
    {
        var session = new ProloguePlaytestSession("task10-telemetry-red",
            "2026-09-21T00:00:00Z", "AutomatedFixture");
        session.RecordRouteNode(CanyonRouteNodeId.LowCeilingBridge, entered: true);
        session.Advance(.25d);
        session.RecordEncounter("canyon.v2.encounter.low-ceiling-bridge", activated: true);
        session.Advance(.25d);
        session.RecordMechanism("canyon.v2.steam-valve-lower");
        session.Advance(.25d);
        session.RecordShortcutDirection(CanyonWorldLayout.UpperBasinShortcutId,
            CanyonRouteNodeId.BasinCamp, CanyonRouteNodeId.SplitLevelEncounter);
        session.Advance(.25d);
        session.RecordFireflyTarget(nameof(CanyonRouteNodeId.StaggeredRift));

        ProloguePlaytestReport report = session.BuildReport(
            PrologueSectorId.CanyonEntrance, PrologueObjective.ReachBasin,
            "2026-09-21T00:00:01Z");
        TestAssert.Equal(1, report.RouteNodes.Count,
            "route telemetry records ordered node entry/exit observations");
        TestAssert.Equal(1, report.Encounters.Count,
            "route telemetry records encounter activation/defeat observations");
        TestAssert.Equal(1, report.Mechanisms.Count,
            "route telemetry records mechanism activations");
        TestAssert.Equal(1, report.ShortcutTraversals.Count,
            "route telemetry records shortcut direction");
        TestAssert.Equal(1, report.FireflyTargets.Count,
            "route telemetry records firefly target changes");
        ProloguePlaytestReport restored = ProloguePlaytestReportFormatter.FromJson(
            ProloguePlaytestReportFormatter.ToJson(report));
        TestAssert.Equal(ProloguePlaytestReport.CurrentVersion, restored.Version,
            "ordered route telemetry serializes with the current schema");
        TestAssert.True(restored.RouteNodes.Count == 1 && restored.Encounters.Count == 1 &&
            restored.Mechanisms.Count == 1 && restored.ShortcutTraversals.Count == 1 &&
            restored.FireflyTargets.Count == 1,
            "ordered route telemetry round-trips without dropping event categories");
        double[] timestamps = report.RouteNodes.Select(sample => sample.Seconds)
            .Concat(report.Encounters.Select(sample => sample.Seconds))
            .Concat(report.Mechanisms.Select(sample => sample.Seconds))
            .Concat(report.ShortcutTraversals.Select(sample => sample.Seconds))
            .Concat(report.FireflyTargets.Select(sample => sample.Seconds)).ToArray();
        TestAssert.True(timestamps.SequenceEqual(timestamps.OrderBy(value => value)),
            "all route telemetry timestamps are monotonic");
    }
}
