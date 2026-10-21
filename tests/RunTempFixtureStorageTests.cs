using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Tests.Integration;

namespace Knight.Tests;

// Default/RED inspects actual factories off-tree. TMP is owned but deliberately
// outside admitted storage; this never executes the unsafe legacy aggregate.
public partial class RunTempFixtureStorageTests : SceneTree
{
    public override async void _Initialize()
    {
        int mountedLevels = 0, mountedFlows = 0;
        void Added(Node node)
        {
            if (node is PrologueLevelController) mountedLevels++;
            if (node is PrologueGameFlowController) mountedFlows++;
        }
        NodeAdded += Added;
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length is 1 or 2 && args.All(arg =>
                    arg == "--runtime-only" || arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)),
                "temp fixture contract accepts only external storage and optional runtime mode");
            string root = IsolatedPrologueFixture.ValidateStorageRoot();
            string temp = Path.GetFullPath(Path.GetTempPath());
            TestAssert.True(Within(temp, Path.Combine(Path.GetDirectoryName(root)!, "tmp")) &&
                !Within(temp, root), "real process TMP must be owned but outside admitted storage");
            bool runtime = args.Contains("--runtime-only");
            var failures = new List<string>();
            var roots = new Dictionary<string, string>();
            foreach ((string name, Func<string> create) in new (string, Func<string>)[]
                     {
                         ("supply", () => BasinSupplyRuntimeTests.CreateStorageRoot("supply")),
                         ("supply-reentry", () => BasinSupplyRuntimeTests.CreateStorageRoot("supply-reentry")),
                         ("supply-title", () => BasinSupplyRuntimeTests.CreateStorageRoot("supply-title")),
                         ("flow-all", () => PrologueGameFlowTests.CreateStorageRoot("all")),
                         ("flow-independent", () => PrologueGameFlowTests.CreateStorageRoot("independent")),
                         ("flow-registry", RunPrologueTests.CreateLegacyMainFlowRoot)
                     })
            {
                string actual = create();
                roots.Add(name, actual);
                Check(() => TestAssert.True(Path.IsPathFullyQualified(actual) &&
                    Within(actual, root) && Directory.Exists(actual),
                    name + ": actual pre-seed allocation must be admitted"), name, failures);
            }
            int checkedCount = 0;
            var snapshots = new Dictionary<string, (string Save, string Settings, string Recorder)>();
            foreach ((string name, string slot, string expectedSave, Func<PrologueLevelController> create) in Factories(roots))
            {
                PrologueLevelController level = create();
                try
                {
                    Check(() =>
                    {
                        TestAssert.True(!level.IsInsideTree() && !level.IsNodeReady(),
                            name + ": actual fixture must remain off-tree");
                        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                        string recorder = level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory;
                        foreach ((string label, string path) in new[]
                                 { ("save", flow.SavePath), ("settings", flow.SettingsPath), ("recorder", recorder) })
                            TestAssert.True(Path.IsPathFullyQualified(path) && Within(path, root),
                                $"{name}: {label} is not admitted; actual={path}");
                        string parent = Path.GetDirectoryName(flow.SavePath)!;
                        TestAssert.True(Directory.Exists(parent) &&
                            Path.GetDirectoryName(flow.SettingsPath) == parent &&
                            Path.GetDirectoryName(recorder) == parent,
                            name + ": persistence channels share the same case directory");
                        TestAssert.True(!File.Exists(flow.SavePath) && !File.Exists(flow.SettingsPath) &&
                            !Directory.Exists(recorder), name + ": off-tree construction cannot seed or write");
                        if (expectedSave != null)
                            TestAssert.Equal(expectedSave, flow.SavePath, name + ": preserve caller's exact save slot");
                        var binding = (flow.SavePath, flow.SettingsPath, recorder);
                        if (slot != null && snapshots.TryGetValue(slot, out var previous))
                            TestAssert.Equal(previous, binding, name + ": reload must reuse all three exact paths");
                        else if (slot != null) snapshots.Add(slot, binding);
                    }, name, failures);
                }
                finally { level.Free(); }
                checkedCount++;
            }
            TestAssert.Equal(14, checkedCount, "all six temp families and retained-slot factories are observed");
            TestAssert.True(mountedLevels == 0 && mountedFlows == 0,
                "preflight must never mount Main or GameFlow, even on RED");
            GD.Print($"TEMP_FIXTURE_STORAGE_BOUNDARY instances={checkedCount} roots={roots.Count} main_added={mountedLevels} flow_added={mountedFlows}");
            TestAssert.True(failures.Count == 0,
                $"temp fixture storage failures={failures.Count}; {string.Join(" | ", failures)}");
            // Actual write helpers, not mocks: even a missing guard can only
            // write this owned TMP canary, and no callback below can mount.
            string unowned = Path.Combine(temp, "must-remain-unseeded.json");
            AssertRejected(() => PrologueGameFlowTests.WriteFixtureSave(unowned, PrologueSaveData.NewLayoutV2()), unowned);
            AssertRejected(() => PrologueGameFlowTests.WriteCorruptFixtureSave(unowned), unowned);
            AssertRejected(() => PrologueGameFlowTests.Instantiate("RejectedUnowned", unowned).Free(), unowned);
            AssertRejected(() => BasinSupplyRuntimeTests.CreateSampleFixture(unowned, false).Free(), unowned);
            TestAssert.True(mountedLevels == 0 && mountedFlows == 0,
                "unowned-slot refusal probes must remain off-tree");
            GD.Print("TEMP_FIXTURE_PRESEED_REFUSAL_OK cases=4 main_added=0 flow_added=0");
            if (runtime)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await RunRuntimeFamilies();
            }
            GD.Print($"TEMP_FIXTURE_STORAGE_OK instances={checkedCount} roots={roots.Count} runtime={runtime}");
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(1);
        }
        finally { NodeAdded -= Added; }
    }

    private async Task RunRuntimeFamilies()
    {
        var failures = new List<string>();
        int passed = 0;
        foreach ((string name, Func<Task> run) in new (string, Func<Task>)[]
                 {
                     // Check the retained-slot lifecycles before unrelated old
                     // terrain/glyph contracts; every family remains mandatory.
                     ("game-flow", () => PrologueGameFlowTests.RunTask7IndependentCases(this)),
                     ("completion", async () =>
                     {
                         Task7Case[] cases = PrologueCompletionTests.Task7Cases(this).Where(item =>
                             item.Name.EndsWith(".PauseMenuOwnsPauseControlsRecoveryAndInjectedQuit", StringComparison.Ordinal)).ToArray();
                         TestAssert.Equal(1, cases.Length, "retain actual pause/completion lifecycle contract");
                         await Task7CaseRegistry.Execute(cases);
                     }),
                     ("basin-supply", () => BasinSupplyRuntimeTests.Run(this)),
                     ("basin-camera", () => BasinAscentCameraRuntimeTests.Run(this)),
                     ("basin-laborer", () => BasinLaborerClearanceRuntimeTests.Run(this)),
                     ("guidance", async () =>
                     {
                         Task7Case[] cases = PrologueGuidanceRuntimeTests.Task7Cases(this).Where(item =>
                             item.Name.Contains(".MountedGuidance", StringComparison.Ordinal) ||
                             item.Name.EndsWith(".DisablingGuidanceClearsWithoutTouchingCompletionOrProgress", StringComparison.Ordinal)).ToArray();
                         TestAssert.Equal(4, cases.Length, "retain four actual mounted guidance contracts");
                         await Task7CaseRegistry.Execute(cases);
                     })
                 })
        {
            ulong[] children = Root.GetChildren().Select(node => node.GetInstanceId()).Order().ToArray();
            try
            {
                await run();
                passed++;
                GD.Print("TEMP_FIXTURE_LIFECYCLE_CASE_OK family=" + name);
            }
            catch (Exception exception)
            {
                failures.Add(name + ": " + exception.Message);
                GD.Print("TEMP_FIXTURE_LIFECYCLE_CASE_FAILED family=" + name + " exception=" + exception);
            }
            // Diagnostic continuation only after original teardown. Never hide a
            // failed family, silently clean up its leak or count a partial pass.
            TestAssert.True(!Paused && children.SequenceEqual(Root.GetChildren()
                    .Select(node => node.GetInstanceId()).Order()),
                name + ": cannot continue diagnostic batch after leaked scene or paused tree");
        }
        GD.Print($"TEMP_FIXTURE_STORAGE_LIFECYCLE_RESULT families=6 passed={passed} failed={failures.Count}");
        TestAssert.True(failures.Count == 0, string.Join(" | ", failures));
        GD.Print("TEMP_FIXTURE_STORAGE_LIFECYCLES_OK families=6 guidance_cases=4 completion_cases=1 flow_cases=5");
    }

    private static IEnumerable<(string Name, string Slot, string Save, Func<PrologueLevelController> Create)> Factories(
        IReadOnlyDictionary<string, string> roots)
    {
        yield return ("basin-camera", null, null, () => BasinAscentCameraRuntimeTests.CreateLevelFixture(
            PrologueSectorId.CanyonBasin, new Vector2(3915, 3759)));
        yield return ("ascent-camera", null, null, () => BasinAscentCameraRuntimeTests.CreateLevelFixture(
            PrologueSectorId.CanyonAscent, new Vector2(3915, 3547)));
        yield return ("ascent-high", null, null, () => BasinAscentCameraRuntimeTests.CreateLevelFixture(
            PrologueSectorId.CanyonAscent, new Vector2(4780, 1283)));
        yield return ("laborer", null, null, BasinLaborerClearanceRuntimeTests.CreateLevelFixture);
        yield return ("guidance", null, null, () => PrologueGuidanceRuntimeTests.CreateLevelFixture("StorageGuidance"));
        yield return ("completion", null, null, () => PrologueCompletionTests.CreateLevelFixture("StorageCompletion"));
        foreach (string name in new[] { "supply", "supply-reentry", "supply-title" })
        {
            string path = Path.Combine(roots[name], "save.json");
            yield return (name, name, path, () => BasinSupplyRuntimeTests.CreateSampleFixture(path, false));
            if (name == "supply")
                yield return (name + "-reload", name, path, () => BasinSupplyRuntimeTests.CreateSampleFixture(path, true));
        }
        foreach (string name in new[] { "flow-all", "flow-independent", "flow-registry" })
        {
            string path = Path.Combine(roots[name], "continue-v2.json");
            yield return (name, name, path, () => PrologueGameFlowTests.Instantiate(name, path));
            if (name == "flow-independent")
                yield return (name + "-reload", name, path, () => PrologueGameFlowTests.Instantiate(name + "-reload", path));
        }
    }

    private static void Check(Action assertion, string name, List<string> failures)
    {
        try { assertion(); GD.Print("TEMP_FIXTURE_STORAGE_CASE_OK name=" + name); }
        catch (InvalidOperationException exception) when (exception.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
        {
            failures.Add(exception.Message);
            GD.Print("TEMP_FIXTURE_STORAGE_CASE_REJECTED name=" + name + " reason=" + exception.Message);
        }
    }

    private static void AssertRejected(Action operation, string canary)
    {
        bool rejected = false;
        try { operation(); }
        catch (InvalidOperationException exception) when
            (exception.Message.Contains("must belong to this process's admitted case", StringComparison.Ordinal))
        { rejected = true; }
        TestAssert.True(rejected && !File.Exists(canary),
            "unowned slot must be refused before writing a seed or creating a mounted scene");
    }

    private static bool Within(string candidate, string root)
    {
        string c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return c.Equals(r, StringComparison.OrdinalIgnoreCase) || c.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
