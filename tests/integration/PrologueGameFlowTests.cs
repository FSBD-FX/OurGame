using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class PrologueGameFlowTests
{
    private static readonly Vector2 BasinCheckpoint = new(2380f, 2014f);

    public static async Task Run(SceneTree tree)
    {
        string root = TempRoot("all");
        try { await Task7CaseRegistry.Execute(Task7Cases(tree, root)); }
        finally { tree.Paused = false; Delete(root); }
    }

    internal static System.Collections.Generic.IReadOnlyList<Task7Case> Task7Cases(
        SceneTree tree, string root) =>
    [
        Case(nameof(CompletedSaveStartsCleanAndPreservesBestMetrics),
            () => CompletedSaveStartsCleanAndPreservesBestMetrics(tree, root)),
        Case(nameof(CorruptSaveRecoveryStartsClean),
            () => CorruptSaveRecoveryStartsClean(tree, root)),
        Case(nameof(CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart),
            () => CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart(tree, root)),
        Case(nameof(TitleNewSettingsCompletionAndInvalidRecovery),
            () => TitleNewSettingsCompletionAndInvalidRecovery(tree, root)),
        Case(nameof(ContinueAndCorruptRecovery),
            () => ContinueAndCorruptRecovery(tree, root))
    ];

    private static Task7Case Case(string name, Func<Task> execute) =>
        Task7CaseRegistry.Case(nameof(PrologueGameFlowTests), name, execute);

    public static async Task RunTask7IndependentCases(SceneTree tree)
    {
        string root = TempRoot("independent");
        try
        {
            Task7CaseRun run = await Task7CaseRegistry.Execute(Task7Cases(tree, root));
            TestAssert.Equal(5, run.Completed.Count,
                "all continuous game-flow delegates complete in a fresh process");
            GD.Print("TASK9_GAME_FLOW_INDIVIDUAL_OK cases=5 retired=0");
        }
        finally { tree.Paused = false; Delete(root); }
    }

    private static async Task CompletedSaveStartsCleanAndPreservesBestMetrics(
        SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "completed-independent.json");
        WriteCompleted(savePath);
        PrologueLevelController level = Instantiate("CompletedIndependentFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(!flow.HasSave,
            "completed schema-v2 save cannot advertise Continue");
        flow.StartNew();
        await Frames(tree, 1);
        PrologueSaveData restarted = PrologueSaveStore.LoadOrNew(savePath);
        TestAssert.True(!restarted.Completed && !restarted.HasActivatedCheckpoint &&
            !restarted.ShortcutOpen && restarted.Discoveries.Count == 0,
            "Start New clears continuous progress authority on disk");
        TestAssert.Equal(123.5d, restarted.BestHumanCompletionSeconds,
            "Start New preserves the best human completion time");
        TestAssert.Equal(7, restarted.BestHumanDeaths,
            "Start New preserves the paired best human death count");
        await Dispose(tree, level);
    }

    private static async Task CorruptSaveRecoveryStartsClean(SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "corrupt-independent.json");
        File.WriteAllText(savePath, "{broken");
        PrologueLevelController level = Instantiate("CorruptIndependentFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(flow.Save.WasRecovered && !flow.HasSave,
            "corrupt save fails closed and cannot masquerade as Continue data");
        flow.StartNew();
        await Frames(tree, 1);
        PrologueSaveData restarted = PrologueSaveStore.LoadOrNew(savePath);
        TestAssert.True(!restarted.Completed && !restarted.HasActivatedCheckpoint &&
            restarted.CheckpointSector == nameof(PrologueSectorId.CanyonEntrance),
            "Start New after corrupt recovery persists a clean canyon run");
        await Dispose(tree, level);
    }

    private static async Task CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart(
        SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "completed-first-death.json");
        WriteCompleted(savePath);
        PrologueLevelController level = Instantiate("CompletedFirstDeathFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.StartNew();
        await Frames(tree, 1);

        PrologueCheckpointSnapshot start = level.Progress.RestoreCheckpoint();
        TestAssert.True(!start.HasActivatedCheckpoint &&
            start.SectorId == PrologueSectorId.CanyonEntrance,
            "completed save starts clean at the canyon round start");
        HealthComponent health = level.Player.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ApplyDamage(new DamageRequest(
                health.CurrentHealth, Vector2.Down, 0f, 900001UL)),
            "real generated player health reaches physical death before checkpoint activation");
        TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonEntrance &&
            level.Player.GlobalPosition.DistanceTo(start.WorldPosition) < 1.1f,
            "the first physical death returns to the exact round start in the same physics frame");
        TestAssert.True(health.CurrentHealth == health.MaxHealth,
            "same-frame round-start recovery restores full health");
        await Dispose(tree, level);
    }

    private static async Task TitleNewSettingsCompletionAndInvalidRecovery(
        SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "title-settings.json");
        PrologueLevelController level = Instantiate("ContinuousFlowFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        PrologueUiRoot ui = level.GetNode<PrologueUiRoot>("ProductUi");
        TestAssert.True(flow.State == PrologueGameFlowState.Title && ui.Visible,
            "finished build begins at title with gameplay disabled");
        flow.StartNew();
        await Frames(tree, 1);
        TestAssert.True(flow.State == PrologueGameFlowState.Playing && !ui.Visible,
            "Start New enters continuous gameplay");
        flow.ApplySettings(new GameSettingsData
        {
            GuidanceEnabled = false,
            Language = GameLanguage.English,
            ReduceFlashes = true
        });
        TestAssert.True(File.Exists(Path.Combine(root, "settings.json")),
            "settings are atomically persisted independently of canyon progress");
        flow.Complete();
        TestAssert.True(flow.State == PrologueGameFlowState.Completed &&
            PrologueSaveStore.LoadOrNew(savePath).Completed,
            "completion persists exact product state");
        level.RequestRespawn("DEATH RESET");
        TestAssert.Equal(PrologueGameFlowState.Completed, flow.State,
            "completed flow rejects recovery transitions");
        await Dispose(tree, level);
    }

    private static async Task ContinueAndCorruptRecovery(SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "continue-v2.json");
        PrologueSaveStore.SaveAtomic(savePath, new PrologueSaveData
        {
            HasActivatedCheckpoint = true,
            CheckpointSector = nameof(PrologueSectorId.CanyonBasin),
            CheckpointWorldX = BasinCheckpoint.X,
            CheckpointWorldY = BasinCheckpoint.Y,
            ShortcutOpen = true,
            Discoveries = ["supply_cave"],
            CompletedTutorials = ["grip_stone"],
            Objective = nameof(PrologueObjective.LearnShield)
        });
        PrologueLevelController level = Instantiate("ContinueContinuousFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(flow.HasSave, "valid schema-v2 disk save enables Continue");
        flow.Continue();
        await Frames(tree, 2);
        TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonBasin &&
            level.Player.GlobalPosition.DistanceTo(BasinCheckpoint) < 1.1f,
            $"Continue restores the authored Basin checkpoint sector and world position; " +
            $"expected={BasinCheckpoint} actual={level.Player.GlobalPosition} " +
            $"sector={level.CurrentSector} activated=" +
            $"{level.Progress.RestoreCheckpoint().HasActivatedCheckpoint}");
        TestAssert.True(level.Progress.BellChainShortcutOpen &&
            level.Progress.Discoveries.Contains("supply_cave") &&
            level.Progress.CompletedTutorials.Contains("grip_stone"),
            "Continue restores sector-independent durable progress");
        TestAssert.Equal(PrologueObjective.LearnShield,
            level.GetNode<PrologueCompletionController>("Completion").CurrentObjective,
            "Continue restores the exact continuous objective prefix");
        await Dispose(tree, level);

        File.WriteAllText(savePath, "{broken");
        PrologueLevelController corrupt = Instantiate("ContinueCorruptFixture", savePath);
        tree.Root.AddChild(corrupt);
        await Frames(tree, 2);
        PrologueGameFlowController recovered = corrupt.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(recovered.Save.WasRecovered && !recovered.HasSave,
            "corrupt Continue data fails closed");
        recovered.StartNew();
        await Frames(tree, 1);
        TestAssert.True(corrupt.CurrentSector == PrologueSectorId.CanyonEntrance &&
            !corrupt.Progress.RestoreCheckpoint().HasActivatedCheckpoint,
            "corrupt recovery starts a clean continuous run");
        await Dispose(tree, corrupt);
    }

    private static void WriteCompleted(string path) =>
        PrologueSaveStore.SaveAtomic(path, new PrologueSaveData
        {
            HasActivatedCheckpoint = true,
            CheckpointSector = nameof(PrologueSectorId.LostTemple),
            CheckpointWorldX = 4800f,
            CheckpointWorldY = 900f,
            ShortcutOpen = true,
            Discoveries = ["supply_cave"],
            CompletedTutorials = ["grip_stone"],
            Objective = nameof(PrologueObjective.Complete),
            Completed = true,
            BestHumanCompletionSeconds = 123.5d,
            BestHumanDeaths = 7
        });

    private static PrologueLevelController Instantiate(string name, string savePath)
    {
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.SavePath = savePath;
        flow.SettingsPath = Path.Combine(Path.GetDirectoryName(savePath)!, "settings.json");
        flow.HeadlessAutoStart = false;
        return level;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task Dispose(SceneTree tree, Node node)
    {
        tree.Paused = false;
        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, node);
    }

    private static string TempRoot(string suffix)
    {
        string path = Path.Combine(Path.GetTempPath(),
            "knight-task9-flow-" + suffix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}
