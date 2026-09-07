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
        await RejectsDuplicateNames();
        await RejectsUnclassifiedEntries();
        RejectsIgnoredLegacySelectionParameters();
        Task8ReplacementSetIsExactAndUnexpectedRetirementFailsClosed(tree);
        FullAuthoritativeSetRejectsEveryMutation(tree);
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
