using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;

namespace Knight.Tests;

// A focused safety contract, not permission to execute the full legacy aggregate.
// The default (and RED) path never mounts a level, even if every binding is wrong.
public partial class RunFixtureStorageTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length is 1 or 2 &&
                args.Count(v => v.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                args.All(v => v == "--runtime-only" || v.StartsWith("--case-storage-root=", StringComparison.Ordinal)),
                "storage contract accepts only one external root and optional --runtime-only");
            string root = args.Single(v => v.StartsWith("--case-storage-root=", StringComparison.Ordinal))
                ["--case-storage-root=".Length..];
            TestAssert.True(Path.IsPathFullyQualified(root) && Directory.Exists(root),
                "storage contract needs an existing absolute external root");
            TestAssert.True(!Within(root, ProjectSettings.GlobalizePath("res://")) &&
                !Within(root, OS.GetUserDataDir()), "storage contract root must be external");
            bool mounted = args.Contains("--runtime-only");
            var failures = new List<string>();
            var caseRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int checkedCount = 0;
            foreach ((string name, Func<PrologueLevelController> create) in Factories())
            {
                PrologueLevelController level = create();
                try
                {
                    TestAssert.True(!level.IsInsideTree() && !level.IsNodeReady(),
                        name + ": real fixture must still be off-tree and never ready");
                    string caseRoot = AssertPaths(level, root, name);
                    TestAssert.True(caseRoots.Add(caseRoot), name + ": each fixture has its own case directory");
                    if (mounted) await AssertMountedWrites(level, name);
                    GD.Print($"FIXTURE_STORAGE_CASE_OK name={name} mounted={mounted} root={caseRoot}");
                }
                catch (InvalidOperationException exception) when
                    (!mounted && exception.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
                {
                    failures.Add(exception.Message);
                    GD.Print($"FIXTURE_STORAGE_CASE_REJECTED name={name} mounted=false reason={exception.Message}");
                }
                finally
                {
                    if (GodotObject.IsInstanceValid(level))
                    {
                        if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(this, level);
                        else level.Free();
                    }
                }
                checkedCount++;
            }
            TestAssert.Equal(8, checkedCount, "six mount sites include all three camera seam arrangements");
            TestAssert.True(failures.Count == 0,
                $"fixture storage failures={failures.Count}; {string.Join(" | ", failures)}");
            GD.Print($"FIXTURE_STORAGE_OK instances={checkedCount} mount_sites=6 mounted={mounted}");
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(1);
        }
    }

    private static IEnumerable<(string Name, Func<PrologueLevelController> Create)> Factories()
    {
        yield return ("p1-hidden", () => P1LoopRuntimeTests.CreateLevelFixture("StorageP1"));
        yield return ("descent-guidance", DescentClawGuidanceRuntimeTests.CreateLevelFixture);
        yield return ("first-combat", FirstCombatGuidanceRuntimeTests.CreateLevelFixture);
        yield return ("hud-resource-labels", PrologueFormalHudTests.CreateResourceLabelFixture);
        yield return ("camera-basin", () => PrologueCanyonTraversalTests.CreateCameraSeamFixture(
            PrologueSectorId.CanyonBasin, new Vector2(2867.57f, 2013.98f)));
        yield return ("camera-ascent-seam", () => PrologueCanyonTraversalTests.CreateCameraSeamFixture(
            PrologueSectorId.CanyonAscent, new Vector2(2892.43f, 2013.98f)));
        yield return ("camera-ascent-high", () => PrologueCanyonTraversalTests.CreateCameraSeamFixture(
            PrologueSectorId.CanyonAscent, new Vector2(3400f, 1000f)));
        yield return ("camera-transition", PrologueCanyonTraversalTests.CreateCameraTransitionFixture);
    }

    private static string AssertPaths(PrologueLevelController level, string root, string name)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        string output = level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory;
        foreach ((string label, string path) in new[]
                 { ("save", flow.SavePath), ("settings", flow.SettingsPath), ("playtests", output) })
            TestAssert.True(Path.IsPathFullyQualified(path) && Within(path, root),
                $"{name}: {label} must be externally bound before mount; actual={path}");
        string caseRoot = Path.GetDirectoryName(Path.GetFullPath(flow.SavePath))!;
        TestAssert.True(Directory.Exists(caseRoot) &&
            Path.GetDirectoryName(Path.GetFullPath(flow.SettingsPath)) == caseRoot &&
            Path.GetDirectoryName(Path.GetFullPath(output)) == caseRoot,
            name + ": all persistence channels must share the same unique case directory");
        string legacy = Path.Combine(caseRoot, Path.GetFileName(PrologueGameFlowController.LegacySavePath));
        TestAssert.True(Within(legacy, caseRoot), name + ": legacy migration sibling stays isolated");
        TestAssert.True(!File.Exists(flow.SavePath) && !File.Exists(flow.SettingsPath) && !Directory.Exists(output),
            name + ": off-tree factory must not initialize persistence or recording");
        return caseRoot;
    }

    private async Task AssertMountedWrites(PrologueLevelController level, string name)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        Root.AddChild(level); // Only reachable after all three actual paths were checked.
        for (int frame = 0; frame < 60 && flow.State != PrologueGameFlowState.Playing; frame++)
            await ToSignal(this, SignalName.ProcessFrame);
        TestAssert.Equal(PrologueGameFlowState.Playing, flow.State,
            name + ": existing auto-start behavior must finish deferred initialization");
        TestAssert.True(File.Exists(flow.SavePath) && File.Exists(flow.SettingsPath),
            name + ": actual deferred settings and new-game save writes use the admitted paths");
        TestAssert.True(new FileInfo(flow.SavePath).Length > 0 && new FileInfo(flow.SettingsPath).Length > 0,
            name + ": retained persistence files must not be empty");
    }

    private static bool Within(string candidate, string root)
    {
        string c = Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string r = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return c.Equals(r, StringComparison.OrdinalIgnoreCase) ||
            c.StartsWith(r + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}
