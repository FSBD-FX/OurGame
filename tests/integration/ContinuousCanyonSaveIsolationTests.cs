using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Save;

namespace Knight.Tests.Integration;

public static class ContinuousCanyonSaveIsolationTests
{
    public static async Task Run(SceneTree tree)
    {
        // Keeping the old default is the production mutation caught here. Redirect
        // its basename into an isolated directory BEFORE tree entry; never touch user saves.
        string directory = Path.Combine(Path.GetTempPath(), "knight-canyon-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        PrologueLevelController level = null;
        BasinQualitySampleEntry sample = null;
        string legacy = Path.Combine(directory, "prologue-save.json");
        string next = Path.Combine(directory, "prologue-save-layout-v2.json");
        File.WriteAllText(legacy, "{\"version\":2,\"objective\":\"Wake\"}");
        string legacyHash = Hash(legacy);
        try
        {
            level = await CreateMain(tree, directory);
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave && flow.LoadResult.Status == PrologueSaveLoadStatus.MigratableLegacy,
                "legacy entrance offers an explicit mapped choice, not a silent restore");
            flow.PersistProgress("before-explicit-choice");
            TestAssert.True(!File.Exists(next), "pending choice blocks unsolicited persistence");
            AssertHash(legacy, legacyHash, "load before choice");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").EmitSignal(Button.SignalName.Pressed);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && File.Exists(next),
                "FullWorldStartWritesOnlyNewSlot: accepted Start writes the isolated layout-v2 slot");
            flow.PersistProgress("isolation-new-slot");
            TestAssert.Equal(2, PrologueSaveStore.LoadOrNew(next).LayoutVersion,
                "new full-world Start and progress retain explicit layout version 2");
            AssertHash(legacy, legacyHash, "Start and new-slot progress");
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            level = null;
            foreach (string token in new[] { "", ",\"layoutVersion\":0", ",\"layoutVersion\":99", ",\"layoutVersion\":\"2\"" })
            {
                File.WriteAllText(next, "{\"version\":2,\"objective\":\"Wake\"" + token + "}");
                string before = Hash(next);
                level = await CreateMain(tree, directory);
                flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                TestAssert.True(!flow.HasSave, "unknown/missing new-slot layout cannot offer Continue");
                flow.Continue();
                flow.PersistProgress("before-recovered-choice");
                AssertHash(next, before, "unknown-layout rejected Continue and pre-choice persistence");
                level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").EmitSignal(Button.SignalName.Pressed);
                TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                    PrologueSaveStore.LoadOrNew(next, 2).LayoutVersion == 2,
                    "explicit Start replaces the invalid NEW slot only after retaining a recoverable backup");
                string[] backups = Directory.GetFiles(directory, "prologue-save-layout-v2.json.preserved-*.json");
                TestAssert.True(Array.Exists(backups, backup => Hash(backup) == before),
                    "unknown-layout new-slot bytes are preserved in an exact same-directory backup");
                AssertHash(legacy, legacyHash, "unknown-layout rejection");
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                level = null;
            }
            PrologueSaveStore.SaveAtomic(next, PrologueSaveData.NewForLayout(2));
            level = await CreateMain(tree, directory);
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave, "explicit layout 2 offers Continue");
            string validHash = Hash(next);
            flow.Continue();
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "layout 2 Continue starts the world");
            AssertHash(next, validHash, "accepted Continue is read-only");
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            level = null;

            string sampleDirectory = Path.Combine(directory, "sample");
            sample = new BasinQualitySampleEntry { StorageDirectory = sampleDirectory };
            tree.Root.AddChild(sample);
            await Frames(tree, 3);
            flow = sample.Level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(!flow.HasSave, "sample without checkpoint rejects Continue");
            flow.Continue();
            TestAssert.True(!File.Exists(Path.Combine(sampleDirectory, "save.json")),
                "rejected sample Continue writes nothing");
            flow.StartNew();
            await Frames(tree, 3);
            TestAssert.Equal(0, flow.Save.LayoutVersion, "sample Start keeps valid layout-0 data");
            flow.ReturnToTitle();
            TestAssert.True(flow.HasSave, "sample Start creates an authored checkpoint for Continue");
            flow.Continue();
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "sample valid Continue remains accepted");
            AssertHash(legacy, legacyHash, "sample lifecycle legacy sentinel");
            AssertHash(next, validHash, "sample lifecycle new-slot sentinel");
            GD.Print($"SAVE_SENTINELS legacy_sha256={legacyHash} new_sha256={validHash} matched=True");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            if (GodotObject.IsInstanceValid(sample))
            {
                await TestSceneTeardown.DrainAudioOwner(tree, sample.Level);
                sample.QueueFree();
                await Frames(tree, 3);
            }
            Directory.Delete(directory, true);
        }
    }

    private static async Task<PrologueLevelController> CreateMain(SceneTree tree, string directory)
    {
        await Frames(tree, 1);
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.SavePath = Path.Combine(directory, Path.GetFileName(flow.SavePath));
        flow.SettingsPath = Path.Combine(directory, "settings.json");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        return level;
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void AssertHash(string path, string expected, string stage) =>
        TestAssert.Equal(expected, Hash(path), stage + " leaves sentinel byte-identical");
    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
