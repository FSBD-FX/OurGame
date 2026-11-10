using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests.Unit;

public static class Task7CaseRegistryTests
{
    public static async Task Run(SceneTree tree)
    {
        RunPreTimingScalarContracts();
        await RejectsDuplicateNames();
        await RejectsUnclassifiedEntries();
        await RunBoundaryObservations();
        RejectsIgnoredLegacySelectionParameters();
        Task8ReplacementSetIsExactAndUnexpectedRetirementFailsClosed(tree);
        FullAuthoritativeSetRejectsEveryMutation(tree);
    }

    // Breaks caught: forged return/ordering, rejected observations consuming
    // state, clock/frame rollback, premature plan completion, zero-delta loss
    // and UInt64 precision loss. This executes the real scalar journal, not a
    // mocked fixture or source-text check; native pre still runs independently.
    private static void RunPreTimingScalarContracts()
    {
        Type type = typeof(RunPrologueTests).Assembly.GetType("Knight.Tests.PreFixtureTimingJournal");
        TestAssert.True(type != null,
            "PRE_TIMING_JOURNAL_RED: original pre needs ordered scalar enter/return timing, not only one cumulative Traversal timestamp");
        object journal = Activator.CreateInstance(type);
        MethodInfo observe = type.GetMethod("Observe"), complete = type.GetMethod("Complete");
        TestAssert.True(observe != null && complete != null, "real journal exposes observation and complete-plan contracts");
        string Stamp(object owner, int index, string name, bool enter, ulong ms, ulong physics, ulong process) =>
            (string)observe.Invoke(owner, new object[] { index, name, enter, ms, physics, process });
        string Done(object owner, int count) => (string)complete.Invoke(owner, new object[] { count });
        int rejected = 0;
        void Reject(Action run, string why)
        {
            bool failed = false;
            try { run(); }
            catch (TargetInvocationException error) when (error.InnerException is ArgumentException or InvalidOperationException) { failed = true; }
            TestAssert.True(failed, "PRE_TIMING_SCALAR_REJECTION: " + why);
            rejected++;
        }
        Reject(() => Done(journal, 0), "empty plan cannot claim pre completion");
        Reject(() => Done(journal, 1), "missing original case cannot claim completion");
        Reject(() => Stamp(journal, 0, "A.Run", false, 100, 10, 20), "return without entry");
        Reject(() => Stamp(journal, 1, "B.Run", true, 100, 10, 20), "skip first index");
        Reject(() => Stamp(journal, 0, "", true, 100, 10, 20), "empty identity");
        Reject(() => Stamp(journal, 0, "A.Run\nforged", true, 100, 10, 20), "injected record identity");
        TestAssert.Equal("PROLOGUE_PRE_CASE_BOUNDARY index=0 name=A.Run stage=enter engine_ms=100 physics=10 process=20 elapsed_ms=0 physics_delta=0 process_delta=0",
            Stamp(journal, 0, "A.Run", true, 100, 10, 20), "actual first entry retains literal independent scalar values");
        Reject(() => Stamp(journal, 1, "B.Run", true, 101, 11, 21), "pending case cannot start a new case");
        Reject(() => Stamp(journal, 0, "B.Run", false, 125, 13, 28), "mismatched return name");
        Reject(() => Stamp(journal, 0, "A.Run", false, 99, 13, 28), "elapsed clock rollback");
        Reject(() => Stamp(journal, 0, "A.Run", false, 125, 9, 28), "physics rollback");
        Reject(() => Stamp(journal, 0, "A.Run", false, 125, 13, 19), "process rollback");
        Reject(() => Done(journal, 1), "pending case has no synthetic return");
        TestAssert.Equal("PROLOGUE_PRE_CASE_BOUNDARY index=0 name=A.Run stage=return engine_ms=125 physics=13 process=28 elapsed_ms=25 physics_delta=3 process_delta=8",
            Stamp(journal, 0, "A.Run", false, 125, 13, 28), "rejected observations do not consume the actual matching return; exact elapsed/frame differences");
        Reject(() => Stamp(journal, 0, "A.Run", false, 125, 13, 28), "duplicate completed return");
        Reject(() => Stamp(journal, 1, "B.Run", true, 124, 13, 28), "between-case clock rollback");
        Stamp(journal, 1, "B.Run", true, 125, 13, 28);
        TestAssert.Equal("PROLOGUE_PRE_CASE_BOUNDARY index=1 name=B.Run stage=return engine_ms=125 physics=13 process=28 elapsed_ms=0 physics_delta=0 process_delta=0",
            Stamp(journal, 1, "B.Run", false, 125, 13, 28), "actual synchronous same-tick return is valid");
        TestAssert.Equal("PROLOGUE_PRE_CASE_PLAN_OK executed=2 scalar_only=true resource_fix=false behavior_accepted=false",
            Done(journal, 2), "only actual matched completions determine plan receipt");
        Reject(() => Done(journal, 2), "duplicate plan completion");
        Reject(() => Stamp(journal, 2, "C.Run", true, 130, 14, 30), "closed plan cannot append records");

        object large = Activator.CreateInstance(type);
        const ulong beyondDouble = 9_007_199_254_740_993UL;
        Stamp(large, 0, "Large.Run", true, beyondDouble, beyondDouble, beyondDouble);
        TestAssert.Equal("PROLOGUE_PRE_CASE_BOUNDARY index=0 name=Large.Run stage=return engine_ms=9007199254741002 physics=9007199254740995 process=9007199254740996 elapsed_ms=9 physics_delta=2 process_delta=3",
            Stamp(large, 0, "Large.Run", false, beyondDouble + 9, beyondDouble + 2, beyondDouble + 3),
            "clock accounting stays exact beyond floating point integer precision");
        Done(large, 1);
        TestAssert.Equal(16, rejected, "all independent scalar rejection controls execute");
        GD.Print("PRE_TIMING_SCALAR_CONTRACT_OK positive=3 negative=16 same_tick=true large_unsigned=true no_native_ownership=true");
    }

    public static async Task RunBoundaryObservations()
    {
        var events = new List<string>();
        var gate = new TaskCompletionSource<bool>();
        Task<Task7CaseRun> pending = Task7CaseRegistry.Execute(
        [
            Task7CaseRegistry.Case("Boundary", "Sync", () => events.Add("body:sync")),
            Task7CaseRegistry.Case("Boundary", "Async", () => gate.Task),
            Task7CaseRegistry.Case("Boundary", "Last", () => events.Add("body:last"))
        ], (name, entered) => events.Add((entered ? "enter:" : "return:") + name));
        TestAssert.Equal("enter:Boundary.Sync|body:sync|return:Boundary.Sync|enter:Boundary.Async",
            string.Join("|", events),
            "boundaries surround real synchronous execution and never complete a pending task early");
        TestAssert.True(!pending.IsCompleted, "a pending delegate keeps the actual registry pending");
        gate.SetResult(true);
        Task7CaseRun result = await pending;
        TestAssert.Equal("enter:Boundary.Sync|body:sync|return:Boundary.Sync|enter:Boundary.Async|return:Boundary.Async|enter:Boundary.Last|body:last|return:Boundary.Last",
            string.Join("|", events), "completed async execution returns before the next case starts");
        TestAssert.Equal(3, result.Completed.Count, "observations do not replace actual case completion");

        foreach (bool synchronous in new[] { true, false })
        {
            events.Clear();
            var primary = new InvalidOperationException("boundary-primary-" + synchronous);
            var failure = new TaskCompletionSource<bool>();
            Func<Task> execute = synchronous
                ? () => throw primary
                : () => failure.Task;
            Exception observed = null;
            Task<Task7CaseRun> run = Task7CaseRegistry.Execute(
            [
                Task7CaseRegistry.Case("Boundary", "Failure", execute),
                Task7CaseRegistry.Case("Boundary", "MustNotRun", () => events.Add("unexpected"))
            ], (name, entered) => events.Add((entered ? "enter:" : "return:") + name));
            if (!synchronous) failure.SetException(primary);
            try { await run; }
            catch (Exception error) { observed = error; }
            TestAssert.True(ReferenceEquals(primary, observed),
                "boundary observation preserves the original synchronous or asynchronous exception");
            TestAssert.Equal("enter:Boundary.Failure", string.Join("|", events),
                "a failed delegate has no return receipt and never starts the following case");
            using var sink = new StringWriter();
            Exception loggingFailure = RunPrologueTests.WriteEarlyFailure(observed, sink);
            const string prefix = "PROLOGUE_TEST_FAILURE_DETAIL ";
            string record = sink.ToString().TrimEnd();
            TestAssert.True(loggingFailure == null && record.StartsWith(prefix, StringComparison.Ordinal),
                "the real primary failure detail must be retained before resource cleanup can start");
            using var document = System.Text.Json.JsonDocument.Parse(record[prefix.Length..]);
            TestAssert.True(document.RootElement.GetProperty("Type").GetString() == primary.GetType().FullName &&
                document.RootElement.GetProperty("Message").GetString() == primary.Message &&
                document.RootElement.GetProperty("Detail").GetString().Contains(nameof(RunBoundaryObservations)) &&
                ReferenceEquals(primary, observed),
                "early failure JSON preserves actual synchronous/asynchronous primary type/message/stack and identity");
        }
        var sinkFailure = new IOException("intentional-owned-log-sink-failure");
        var retainedPrimary = new InvalidOperationException("not replaced by sink failure");
        using var brokenSink = new FailingFailureWriter(sinkFailure);
        TestAssert.True(ReferenceEquals(sinkFailure, RunPrologueTests.WriteEarlyFailure(retainedPrimary, brokenSink)) &&
            retainedPrimary.Message == "not replaced by sink failure",
            "logging failure is separately returned instead of replacing the primary or preventing cleanup");
        GD.Print("PROLOGUE_EARLY_FAILURE_CONTRACT_OK sync_async=true primary_identity=true sink_failure_contained=true cases=3");
        var prefixControlFailures = new List<string>();
        foreach (var sample in new (string Phase, string Arm, bool Accepted, bool Reject)[]
        {
            ("pre", null, false, false), ("post", "", false, false),
            ("post", "omit-canyon-traversal", true, false),
            ("pre", "omit-canyon-traversal", false, true),
            ("post", "omit-canyon-traversal ", false, true),
            ("post", "unknown", false, true), ("other", "", false, true)
        })
        {
            bool rejected = false, accepted = false;
            try { accepted = RunPrologueTests.ResolveLegacyPostPrefixControl(sample.Phase, sample.Arm); }
            catch (InvalidOperationException) { rejected = true; }
            if (rejected != sample.Reject || !rejected && accepted != sample.Accepted)
                prefixControlFailures.Add($"phase={sample.Phase}/arm={sample.Arm ?? "<null>"}");
        }
        TestAssert.True(prefixControlFailures.Count == 0,
            "POST_PREFIX_CONTROL_ADMISSION_RED: " + string.Join("; ", prefixControlFailures));
        GD.Print("POST_PREFIX_CONTROL_ADMISSION_OK cases=7 default_ordinary=true unknown_rejected=true post_only=true");
        GD.Print("TASK7_BOUNDARY_CONTRACT_OK sync_and_async=true pending_not_completed=true exceptions_preserved=true failed_no_return=true cases=3");
    }

    private sealed class FailingFailureWriter(Exception failure) : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void WriteLine(string value) => throw failure;
    }

    private static void FullAuthoritativeSetRejectsEveryMutation(SceneTree tree)
    {
        string flowRoot = Path.Combine(Path.GetTempPath(),
            "knight-task9-registry-mutations-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(flowRoot);
        try
        {
            List<Task7Case> authoritative = RunPrologueTests.BuildLegacyMainCases(tree, flowRoot);
            const string ordinaryName =
                "PlayerFormalVisualTests.FacingLocalSecondaryMotionMirrorsWorldExactly";
            Task7Case ordinary = authoritative.Single(item => item.Name == ordinaryName);

            var missing = authoritative.Where(item => item.Name != ordinaryName).ToList();
            var renamed = authoritative.Select(item => item.Name == ordinaryName
                ? item with { Name = "PlayerFormalVisualTests.RenamedOrdinaryExecutable" }
                : item).ToList();
            var newExecutable = authoritative.Where(item => item.Name != ordinaryName).ToList();
            newExecutable.Add(Task7CaseRegistry.Case("UnexpectedSuite", "NewExecutable",
                () => { }));
            var duplicate = new List<Task7Case>(authoritative) { ordinary };
            var unclassified = authoritative.Select(item => item.Name == ordinaryName
                ? item with { Disposition = Task7CaseDisposition.Unclassified }
                : item).ToList();
            var retired = authoritative.Select(item => item.Name == ordinaryName
                ? Task7CaseRegistry.Retired("PlayerFormalVisualTests",
                    "FacingLocalSecondaryMotionMirrorsWorldExactly")
                : item).ToList();

            bool missingRejected = ClassificationRejected(missing);
            bool renamedRejected = ClassificationRejected(renamed);
            bool newExecutableRejected = ClassificationRejected(newExecutable);
            bool duplicateRejected = ClassificationRejected(duplicate);
            bool unclassifiedRejected = ClassificationRejected(unclassified);
            bool retirementRejected = ClassificationRejected(retired);
            TestAssert.True(missingRejected && renamedRejected && newExecutableRejected &&
                duplicateRejected && unclassifiedRejected && retirementRejected,
                $"hardcoded authoritative 75-name equality rejects every registry mutation; " +
                $"missing={missingRejected} renamed={renamedRejected} new={newExecutableRejected} " +
                $"duplicate={duplicateRejected} unclassified={unclassifiedRejected} " +
                $"retired={retirementRejected}");
        }
        finally
        {
            if (Directory.Exists(flowRoot)) Directory.Delete(flowRoot, recursive: true);
        }
    }

    private static bool ClassificationRejected(IReadOnlyList<Task7Case> cases)
    {
        try { RunPrologueTests.AssertLegacyMainClassifications(cases); }
        catch (InvalidOperationException) { return true; }
        return false;
    }

    private static void Task8ReplacementSetIsExactAndUnexpectedRetirementFailsClosed(SceneTree tree)
    {
        string flowRoot = Path.Combine(Path.GetTempPath(),
            "knight-task8-registry-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(flowRoot);
        try
        {
            List<Task7Case> cases = RunPrologueTests.BuildLegacyMainCases(tree, flowRoot);
            RunPrologueTests.AssertLegacyMainClassifications(cases);
            Task7Case[] replacements = cases.Where(item =>
                RunPrologueTests.Task8ReplacedLegacyCaseNames.Contains(
                    item.Name, StringComparer.Ordinal)).ToArray();
            TestAssert.Equal(18, replacements.Length,
                "all exact original Task8 names are present once in the authoritative registry");
            TestAssert.True(replacements.All(item =>
                    item.Disposition == Task7CaseDisposition.Executable && item.Execute != null),
                "every original Task8 name has a self-contained executable delegate");

            var injected = new List<Task7Case>(cases)
            {
                Task7CaseRegistry.Retired(nameof(Task7CaseRegistryTests),
                    "UnexpectedTask8Retirement")
            };
            bool rejected = false;
            try { RunPrologueTests.AssertLegacyMainClassifications(injected); }
            catch (InvalidOperationException) { rejected = true; }
            TestAssert.True(rejected,
                "a new unrelated retired entry remains visible and fails closed");
            GD.Print("TASK8_REPLACEMENT_REGISTRY_OK original_names=18 executable=75 " +
                     "retired=0 classified=75 unexpected_retirement=rejected");
        }
        finally
        {
            if (Directory.Exists(flowRoot)) Directory.Delete(flowRoot, recursive: true);
        }
    }

    private static async Task RejectsDuplicateNames()
    {
        Task7Case first = Task7CaseRegistry.Case(nameof(Task7CaseRegistryTests), "duplicate", () => { });
        Task7Case second = Task7CaseRegistry.Case(nameof(Task7CaseRegistryTests), "duplicate", () => { });
        bool rejected = false;
        try { await Task7CaseRegistry.Execute([first, second]); }
        catch (InvalidOperationException) { rejected = true; }
        TestAssert.True(rejected, "Task7 registry fails closed on duplicate explicit names");
    }

    private static async Task RejectsUnclassifiedEntries()
    {
        var unclassified = new Task7Case(
            "Task7CaseRegistryTests.unclassified",
            Task7CaseDisposition.Unclassified,
            () => Task.CompletedTask);
        bool rejected = false;
        try { await Task7CaseRegistry.Execute([unclassified]); }
        catch (InvalidOperationException) { rejected = true; }
        TestAssert.True(rejected, "Task7 registry fails closed on unclassified entries");
    }

    private static void RejectsIgnoredLegacySelectionParameters()
    {
        Type[] wrappers =
        [
            typeof(P0RoomStructureTests),
            typeof(P1LoopStructureTests),
            typeof(P1LoopRuntimeTests),
            typeof(PrologueGuidanceRuntimeTests),
            typeof(PrologueCompletionTests),
            typeof(ProloguePlaytestRecorderTests),
            typeof(ThreadCastRuntimeTests),
            typeof(PrologueVfxRuntimeTests),
            typeof(PrologueGameFlowTests),
            typeof(PlayerFormalVisualTests)
        ];
        TestAssert.Equal(10, wrappers.Length,
            "the compile/API guard covers every explicit Task7 suite wrapper");
        foreach (Type wrapper in wrappers)
        {
            MethodInfo run = wrapper.GetMethod("Run", BindingFlags.Public | BindingFlags.Static) ??
                throw new InvalidOperationException($"{wrapper.Name} requires a public static Run wrapper.");
            foreach (ParameterInfo parameter in run.GetParameters())
                TestAssert.True(parameter.ParameterType != typeof(bool),
                    $"{wrapper.Name}.Run exposes no ignored legacy-selection parameter");
            TestAssert.True(wrapper.GetMethod("Task7Cases",
                    BindingFlags.NonPublic | BindingFlags.Static) != null,
                $"{wrapper.Name} shares its authoritative explicit Task7 registry");
        }
    }
}
