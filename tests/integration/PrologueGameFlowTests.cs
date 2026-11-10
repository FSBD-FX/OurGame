using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Presentation;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class PrologueGameFlowTests
{
    private static readonly Vector2 BasinCheckpoint =
        CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.basin-camp"];

    public static async Task Run(SceneTree tree)
    {
        string root = CreateStorageRoot("all");
        try { await Task7CaseRegistry.Execute(Task7Cases(tree, root)); }
        finally { tree.Paused = false; }
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
        string root = CreateStorageRoot("independent");
        try
        {
            Task7CaseRun run = await Task7CaseRegistry.Execute(Task7Cases(tree, root));
            TestAssert.Equal(5, run.Completed.Count,
                "all continuous game-flow delegates complete in a fresh process");
            GD.Print("TASK9_GAME_FLOW_INDIVIDUAL_OK cases=5 retired=0");
        }
        finally { tree.Paused = false; }
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
            "completed current-layout save cannot advertise Continue");
        PrologueSaveData restarted = await ObserveEntryCommit(tree, level, flow.StartNew, writesSlot: true);
        TestAssert.True(!restarted.WasRecovered && !restarted.Completed && !restarted.HasActivatedCheckpoint &&
            restarted.CheckpointId == "canyon.v2.start" && !restarted.BossDefeated &&
            !restarted.ShortcutOpen && restarted.OpenShortcuts.Count == 0 &&
            restarted.ActivatedMechanisms.Count == 0 && restarted.Discoveries.Count == 0 &&
            restarted.CompletedTutorials.Count == 0,
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
        WriteCorruptFixtureSave(savePath);
        PrologueLevelController level = Instantiate("CorruptIndependentFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(flow.Save.WasRecovered && !flow.HasSave,
            "corrupt save fails closed and cannot masquerade as Continue data");
        byte[] corruptBytes = File.ReadAllBytes(savePath);
        PrologueSaveData restarted = await ObserveEntryCommit(tree, level, flow.StartNew, writesSlot: true);
        TestAssert.True(!restarted.WasRecovered && !restarted.Completed && !restarted.HasActivatedCheckpoint &&
            restarted.CheckpointId == "canyon.v2.start" &&
            restarted.CheckpointSector == nameof(PrologueSectorId.CanyonEntrance),
            "Start New after corrupt recovery persists a clean canyon run");
        string[] backups = Directory.GetFiles(root, Path.GetFileName(savePath) + ".preserved-*.json");
        TestAssert.True(backups.Length == 1 && SameBytes(corruptBytes, File.ReadAllBytes(backups[0])),
            "the real replacement leaves an exact backup of the corrupt file");
        await Dispose(tree, level);
    }

    private static async Task CompletedSaveStartsCleanAndFirstDeathReturnsToRoundStart(
        SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "completed-first-death.json");
        WriteCompleted(savePath);
        PrologueLevelController level = Instantiate("CompletedFirstDeathFixture", savePath);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        ulong playerId = player.GetInstanceId();
        Exception primary = null;
        bool recovered = false, subscribed = false, applyingLethal = false, outsideDamage = false, committed = false;
        Vector2 completionPosition = default;
        int requested = 0, completed = 0;
        void Requested(string _) => requested++;
        void Completed(string _)
        {
            recovered = true; completed++;
            outsideDamage = !applyingLethal;
            committed = !level.ResidentPreparationPending && level.ResidentPreparationFailure.Length == 0;
            completionPosition = player.GlobalPosition;
        }
        object Snapshot() => new
        {
            State = flow.State.ToString(), Pending = level.ResidentPreparationPending,
            Failure = level.ResidentPreparationFailure, Recovering = level.RecoveryInProgress,
            Health = health.CurrentHealth, Requested = requested, Completed = completed,
            Sector = level.CurrentSector?.ToString(), Position = player.GlobalPosition.ToString(),
            Physics = Engine.GetPhysicsFrames()
        };
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 2);
            await ObserveEntryCommit(tree, level, flow.StartNew, writesSlot: true);
            PrologueCheckpointSnapshot start = level.Progress.RestoreCheckpoint();
            TestAssert.True(!start.HasActivatedCheckpoint && start.SectorId == PrologueSectorId.CanyonEntrance,
                "completed save starts clean at the canyon round start");
            level.RecoveryRequested += Requested;
            level.RecoveryCompleted += Completed;
            subscribed = true;
            object beforeLethal = Snapshot();
            applyingLethal = true;
            try
            {
                TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Down, 0f, 900001UL)),
                    "real generated player health reaches physical death before checkpoint activation");
            }
            finally { applyingLethal = false; }
            object afterLethal = Snapshot();
            int recoveryFrames = 0;
            while (!recovered && recoveryFrames < 180)
            {
                TestAssert.True(flow.State == PrologueGameFlowState.Recovering &&
                    level.ResidentPreparationFailure.Length == 0,
                    "accepted first-death recovery retains ownership until its real resident commit: " +
                    level.ResidentPreparationFailure);
                await Frames(tree, 1);
                recoveryFrames++;
            }
            if (!recovered) GD.Print("COMPLETED_SAVE_FIRST_DEATH_FAILURE " +
                System.Text.Json.JsonSerializer.Serialize(new { Before = beforeLethal, After = afterLethal, Final = Snapshot() }));
            TestAssert.True(recovered && requested == 1 && completed == 1 && outsideDamage && committed &&
                flow.State == PrologueGameFlowState.Playing,
                "first-death recovery completes once after leaving the damage callback and committing its real session");
            TestAssert.True(level.CurrentSector == PrologueSectorId.CanyonEntrance &&
                completionPosition.DistanceTo(start.WorldPosition) < 1.1f &&
                player.GlobalPosition.DistanceTo(start.WorldPosition) < 1.1f &&
                !level.Progress.RestoreCheckpoint().HasActivatedCheckpoint &&
                player.GetInstanceId() == playerId && ReferenceEquals(level.Player, player),
                "the first physical death returns the same player to the exact round start without advancing checkpoint");
            TestAssert.True(health.CurrentHealth == health.MaxHealth, "round-start recovery restores full health");
            GD.Print($"COMPLETED_SAVE_FIRST_DEATH_COMMIT_OK actual_playing=true real_lethal=true requested=1 completed=1 " +
                $"outside_damage=true same_player=true round_start=true resident_commit=true wait_physics={recoveryFrames}");
        }
        catch (Exception error) { primary = error; throw; }
        finally
        {
            if (subscribed && GodotObject.IsInstanceValid(level))
            {
                level.RecoveryRequested -= Requested;
                level.RecoveryCompleted -= Completed;
            }
            tree.Paused = false;
            await ApprovedLanternRuntimeTests.ReleaseOwnedLevel(tree, level, primary);
        }
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
        await ObserveEntryCommit(tree, level, flow.StartNew, writesSlot: true);
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
        int cancelledRequests = 0;
        level.RecoveryRequested += _ => cancelledRequests++;
        level.RequestRespawn("DEATH RESET");
        TestAssert.Equal(PrologueGameFlowState.Completed, flow.State,
            "completed flow rejects recovery transitions");
        await Frames(tree, 5);
        TestAssert.True(cancelledRequests == 0 &&
            !level.GetNode<PrologueFormalHud>("Hud/FormalHud").CurrentModel.RecoveryVisible,
            "rejected completed recovery must not start HUD, pose or recorder recovery consumers");
        var completedHealth = level.Player.GetNode<HealthComponent>("Health");
        TestAssert.True(completedHealth.ApplyDamage(new DamageRequest(
            completedHealth.CurrentHealth, Vector2.Down, 0, 900002UL)),
            "completed-session fixture reaches actual Health.Died before Replay");
        await Frames(tree, 5);
        TestAssert.True(flow.State == PrologueGameFlowState.Completed && cancelledRequests == 0,
            "actual post-completion death cannot reopen checkpoint recovery");
        await ObserveEntryCommit(tree, level, flow.StartNew, writesSlot: true);
        await Frames(tree, 8);
        var replayPose = level.Player.GetNode<PlayerFormalVisual>("VisualRoot/Formal")
            .CurrentPlayerFrame.Locomotion.State;
        TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
            replayPose is not PlayerLocomotionState.Recovery and not PlayerLocomotionState.Dead,
            "Replay after rejected recovery returns to ordinary player presentation");
        await Dispose(tree, level);
    }

    private static async Task ContinueAndCorruptRecovery(SceneTree tree, string root)
    {
        string savePath = Path.Combine(root, "continue-v2.json");
        PrologueSaveData continuing = PrologueSaveData.NewLayoutV2();
        continuing.CheckpointId = "canyon.v2.basin-camp";
        continuing.OpenShortcuts =
        [
            CanyonWorldLayout.UpperBasinShortcutId,
            "canyon.v2.temple-bell-gate"
        ];
        continuing.Discoveries = ["supply_cave"];
        continuing.CompletedTutorials = ["grip_stone"];
        continuing.Objective = nameof(PrologueObjective.LearnShield);
        WriteFixtureSave(savePath, continuing);
        PrologueLevelController level = Instantiate("ContinueContinuousFixture", savePath);
        tree.Root.AddChild(level);
        await Frames(tree, 2);
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(flow.HasSave, "valid current-layout disk save enables Continue");
        await ObserveEntryCommit(tree, level, flow.Continue, writesSlot: false);
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

        WriteCorruptFixtureSave(savePath);
        PrologueLevelController corrupt = Instantiate("ContinueCorruptFixture", savePath);
        tree.Root.AddChild(corrupt);
        await Frames(tree, 2);
        PrologueGameFlowController recovered = corrupt.GetNode<PrologueGameFlowController>("GameFlow");
        TestAssert.True(recovered.Save.WasRecovered && !recovered.HasSave,
            "corrupt Continue data fails closed");
        PrologueSaveData replaced = await ObserveEntryCommit(tree, corrupt, recovered.StartNew, writesSlot: true);
        TestAssert.True(!replaced.WasRecovered && replaced.CheckpointId == "canyon.v2.start" &&
            corrupt.CurrentSector == PrologueSectorId.CanyonEntrance &&
            !corrupt.Progress.RestoreCheckpoint().HasActivatedCheckpoint,
            "corrupt recovery starts a clean continuous run");
        await Dispose(tree, corrupt);
    }

    private static async Task<PrologueSaveData> ObserveEntryCommit(SceneTree tree,
        PrologueLevelController level, Action request, bool writesSlot)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var playerRoot = level.GetNode<Node2D>("PlayerRoot");
        byte[] ReadSlot() => File.Exists(flow.SavePath) ? File.ReadAllBytes(flow.SavePath) : null;
        byte[] before = ReadSlot(), publishedBytes = null;
        DateTime? beforeWriteTime = null, publishedWriteTime = null;
        if (!writesSlot)
        {
            // A deterministic SaveAtomic can replace the slot with identical bytes.
            // Mark this isolated fixture so a normal rewrite cannot hide behind equality.
            var sentinel = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            TestAssert.True(before != null, "ordinary Continue requires an existing isolated slot");
            File.SetLastWriteTimeUtc(flow.SavePath, sentinel);
            beforeWriteTime = File.GetLastWriteTimeUtc(flow.SavePath);
            TestAssert.True(beforeWriteTime == sentinel, "ordinary Continue fixture write marker is applied");
        }
        PrologueSaveData accepted = flow.Save, published = null, publishedAuthority = null;
        bool playableAtSignal = false;
        int signals = 0;
        Exception readFailure = null;
        void OnSessionStarted()
        {
            if (++signals != 1) return;
            playableAtSignal = flow.State == PrologueGameFlowState.Playing &&
                !level.ResidentPreparationPending && playerRoot.IsVisibleInTree();
            publishedAuthority = flow.Save;
            try
            {
                publishedBytes = ReadSlot();
                if (!writesSlot) publishedWriteTime = File.GetLastWriteTimeUtc(flow.SavePath);
                published = PrologueSaveStore.LoadOrNew(flow.SavePath, CanyonWorldLayout.CurrentLayoutVersion);
            }
            catch (Exception error) { readFailure = error; }
        }
        void AssertIntroUnpublished() => TestAssert.True(
            flow.State == PrologueGameFlowState.Intro && level.ResidentPreparationPending &&
            !playerRoot.IsVisibleInTree() && signals == 0 && ReferenceEquals(accepted, flow.Save) &&
            SameBytes(before, ReadSlot()) &&
            (writesSlot || beforeWriteTime == File.GetLastWriteTimeUtc(flow.SavePath)),
            "actual full-world Intro keeps the old slot unpublished and gameplay hidden");

        flow.SessionStarted += OnSessionStarted;
        try
        {
            request();
            AssertIntroUnpublished();
            for (int frame = 0; frame < 180 && flow.State == PrologueGameFlowState.Intro; frame++)
            {
                await Frames(tree, 1);
                if (flow.State == PrologueGameFlowState.Intro) AssertIntroUnpublished();
            }
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && !level.ResidentPreparationPending,
                $"entry must commit its real session: state={flow.State} pending={level.ResidentPreparationPending} " +
                $"reason={level.ResidentPreparationFailure}");
            TestAssert.True(signals == 1 && playableAtSignal && readFailure == null &&
                publishedAuthority != null && ReferenceEquals(publishedAuthority, flow.Save) &&
                publishedBytes != null && published != null && !published.WasRecovered &&
                SameBytes(publishedBytes, ReadSlot()),
                "exactly one real SessionStarted must publish a usable slot already on disk: " + readFailure?.Message);
            TestAssert.True(writesSlot
                    ? !ReferenceEquals(accepted, publishedAuthority) && !SameBytes(before, publishedBytes)
                    : ReferenceEquals(accepted, publishedAuthority) && SameBytes(before, publishedBytes) &&
                      beforeWriteTime == publishedWriteTime &&
                      beforeWriteTime == File.GetLastWriteTimeUtc(flow.SavePath),
                writesSlot ? "fresh entry replaces the accepted slot only when committed"
                    : "ordinary Continue retains the accepted slot bytes and write marker through entry");
            GD.Print($"GAME_FLOW_ENTRY_COMMIT_OK fixture={level.Name} wrote_slot={writesSlot} " +
                "old_bytes_in_intro=true resident_ready=true session_events=1 usable_disk_at_signal=true" +
                (writesSlot ? "" : " continue_write_marker_unchanged=true"));
            return published;
        }
        finally { flow.SessionStarted -= OnSessionStarted; }
    }

    private static bool SameBytes(byte[] left, byte[] right) => left == null ? right == null :
        right != null && left.AsSpan().SequenceEqual(right);

    private static void WriteCompleted(string path)
    {
        PrologueSaveData completed = PrologueSaveData.NewLayoutV2();
        completed.CheckpointId = "canyon.v2.temple-forecourt";
        completed.OpenShortcuts =
        [
            CanyonWorldLayout.UpperBasinShortcutId,
            "canyon.v2.temple-bell-gate"
        ];
        completed.ActivatedMechanisms =
        [
            CanyonWorldLayout.CourtyardSealMechanismId,
            "canyon.v2.encounter.split-level"
        ];
        completed.Discoveries = ["supply_cave"];
        completed.CompletedTutorials = ["grip_stone"];
        completed.Objective = nameof(PrologueObjective.Complete);
        completed.BossDefeated = true;
        completed.Completed = true;
        completed.BestHumanCompletionSeconds = 123.5d;
        completed.BestHumanDeaths = 7;
        WriteFixtureSave(path, completed);
    }

    internal static void WriteFixtureSave(string path, PrologueSaveData data)
    {
        IsolatedPrologueFixture.ValidateExistingSavePath(path);
        PrologueSaveStore.SaveAtomic(path, data);
    }

    internal static void WriteCorruptFixtureSave(string path)
    {
        IsolatedPrologueFixture.ValidateExistingSavePath(path);
        File.WriteAllText(path, "{broken");
    }

    internal static PrologueLevelController Instantiate(string name, string savePath)
    {
        IsolatedPrologueFixture.ValidateExistingSavePath(savePath);
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        level.Name = name;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        IsolatedPrologueFixture.ConfigureExisting(level, savePath);
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

    internal static string CreateStorageRoot(string suffix)
        => IsolatedPrologueFixture.CreatePaths("game-flow-" + suffix).CaseRoot;
}
