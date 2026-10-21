using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Save;

namespace Knight.Tests.Integration;

public static class ContinuousCanyonMigrationTests
{
    // Applying old coordinates, writing before consent, or dropping independent
    // progress must fail these literal expectations, not merely change source text.
    public static void RunStore()
    {
        string root = Path.Combine(Path.GetTempPath(), "knight-task7-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string legacy = Path.Combine(root, "prologue-save.json");
            string next = Path.Combine(root, "prologue-save-layout-v2.json");
            foreach (var fixture in new[]
            {
                ("CanyonBasin", true, "canyon.v2.basin-camp", new Vector2(1680,3960)),
                ("LostTemple", true, "canyon.v2.temple-forecourt", new Vector2(5920,1080)),
                ("CanyonEntrance", false, "canyon.v2.start", new Vector2(160,600)),
                ("LostTemple", false, "canyon.v2.start", new Vector2(160,600))
            })
            {
                File.WriteAllText(legacy, "{\"version\":2,\"hasActivatedCheckpoint\":" +
                    fixture.Item2.ToString().ToLowerInvariant() + ",\"checkpointSector\":\"" + fixture.Item1 +
                    "\",\"checkpointWorldX\":5040,\"checkpointWorldY\":1294," +
                    "\"discoveries\":[\"supply_cave\"],\"completedTutorials\":[\"grip_stone\"],\"bossDefeated\":true}");
                string hash = Hash(legacy);
                PrologueSaveLoadResult result = PrologueSaveStore.LoadFullWorld(next, legacy);
                TestAssert.Equal(PrologueSaveLoadStatus.MigratableLegacy, result.Status,
                    "LegacyRequiresExplicitChoice: schema2 load cannot silently accept or discard a legacy checkpoint");
                TestAssert.Equal(fixture.Item3, result.Data.CheckpointId, "legacy sector maps to exact stable identity");
                TestAssert.Equal(fixture.Item4, result.Data.CheckpointWorldPosition,
                    "CoordinateRejection: legacy oldTemple(5040,1294) is never applied to layout2");
                TestAssert.True(result.Data.Version == PrologueSaveData.CurrentVersion && result.Data.LayoutVersion == 2,
                    "current save schema and layout2 are independent");
                TestAssert.True(result.Data.BossDefeated && result.Data.Discoveries.Contains("supply_cave") &&
                    result.Data.CompletedTutorials.Contains("grip_stone"), "supported durable evidence survives mapping");
                TestAssert.True(!File.Exists(next), "load before choice never writes the new slot");
                Preserve(legacy, hash, "load " + fixture.Item1);
            }
            foreach (string invalid in new[]
            {
                "{\"version\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"CanyonAscent\"}",
                "{\"version\":2,\"checkpointSector\":\"not-a-sector\"}",
                "{\"version\":2,\"checkpointId\":\"unknown\"}",
                "{\"version\":2,\"activatedMechanisms\":[\"unknown\"]}",
                "{\"version\":2,\"openShortcuts\":[\"unknown\"]}",
                "{\"version\":2,\"discoveries\":[\"unknown\"]}",
                "{\"version\":2,\"layoutVersion\":99}",
                "{\"version\":2,\"layoutVersion\":null}",
                "{\"version\":2,\"layoutVersion\":\"2\"}",
                "{\"version\":2,\"layoutVersion\":2.5}",
                "{\"Version\":2}", "not-json"
            })
            {
                File.WriteAllText(legacy, invalid);
                string hash = Hash(legacy);
                var result = PrologueSaveStore.LoadFullWorld(next, legacy);
                TestAssert.Equal(PrologueSaveLoadStatus.NeedsNewLayoutChoice, result.Status,
                    "unknown or malformed legacy state requires a new-layout choice, never silent repair");
                TestAssert.True(!File.Exists(next), "invalid legacy choice does not create a slot");
                Preserve(legacy, hash, "invalid legacy choice");
            }
            File.WriteAllText(legacy, "{\"version\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"CanyonBasin\"}");
            string preserved = Hash(legacy);
            var absentBoss = PrologueSaveStore.LoadFullWorld(next, legacy);
            TestAssert.True(!absentBoss.Data.BossDefeated, "migration never synthesizes absent Boss evidence");
            PrologueSaveStore.SaveAtomic(next, absentBoss.Data);
            Preserve(legacy, preserved, "new slot write");
            string currentHash = Hash(next);
            using (var locked = new FileStream(next, FileMode.Open, System.IO.FileAccess.ReadWrite, FileShare.None))
            {
                bool failed = false;
                try { PrologueSaveStore.SaveAtomic(next, PrologueSaveData.NewLayoutV2()); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failed = true; }
                TestAssert.True(failed, "real denied replacement surfaces a failed atomic write");
            }
            Preserve(next, currentHash, "failed write current slot");
            Preserve(legacy, preserved, "failed write legacy slot");
            TestAssert.Equal(0, Directory.GetFiles(root, "*.tmp").Length, "failed write cleans only its temporary file");
        }
        finally { Directory.Delete(root, true); }
    }

    public static async Task RunMenus(SceneTree tree)
    {
        foreach (int mode in new[] { 0, 1, 2 })
        {
            bool mapped = mode == 0;
            bool invalidLegacy = mode == 2;
            string root = Path.Combine(Path.GetTempPath(), "knight-task7-menu-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string legacy = Path.Combine(root, "prologue-save.json");
            string next = Path.Combine(root, "prologue-save-layout-v2.json");
            File.WriteAllText(legacy, """
                {"version":2,"hasActivatedCheckpoint":true,"checkpointSector":"LostTemple",
                 "checkpointWorldX":5040,"checkpointWorldY":1294,"bossDefeated":true,
                 "objective":"DefeatBellkeeper","discoveries":["supply_cave"]}
                """);
            if (invalidLegacy) File.WriteAllText(legacy,
                "{\"version\":2,\"hasActivatedCheckpoint\":true,\"checkpointSector\":\"invalid\"}");
            string hash = Hash(legacy);
            PrologueLevelController level = null;
            try
            {
                level = await Create(tree, root, autoStart: true);
                var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
                    "migration choice cannot be consumed by automation/headless default");
                if (invalidLegacy)
                {
                    TestAssert.True(!flow.HasSave && flow.LoadResult.Status == PrologueSaveLoadStatus.NeedsNewLayoutChoice,
                        "unknown checkpoint offers Start only, not an invented mapped Continue");
                    flow.Continue();
                }
                // A real SaveAtomic attempt would throw against this directory.
                Directory.CreateDirectory(next);
                flow.PersistProgress("unsolicited-before-choice");
                Directory.Delete(next);
                TestAssert.True(!File.Exists(next), "no SaveAtomic/new slot before an explicit choice");
                Button button = level.GetNode<Button>(mapped ? "ProductUi/MainMenu/Panel/Buttons/Continue" :
                    "ProductUi/MainMenu/Panel/Buttons/Start");
                TestAssert.True(!button.Disabled && button.Text == (mapped ?
                    "从映射存档点继续" : "开始峡谷（旧存档保留）"),
                    "actual menu names the explicit nondestructive choice");
                Preserve(legacy, hash, "menu before choice");
                button.EmitSignal(Button.SignalName.Pressed);
                TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "actual selected menu action begins play");
                TestAssert.True(File.Exists(next), "only the explicit menu choice writes the new slot");
                var saved = PrologueSaveStore.LoadOrNew(next, 2);
                TestAssert.Equal(mapped ? "canyon.v2.temple-forecourt" : "canyon.v2.start",
                    saved.CheckpointId, "menu choice commits its selected authored identity");
                TestAssert.Equal(mapped ? new Vector2(5920,1080) : new Vector2(160,600),
                    level.Player.GlobalPosition, "CoordinateRejection: real Continue never applies oldTemple coordinates");
                Preserve(legacy, hash, mapped ? "mapped Continue" : "Start new canyon");
                // Let restored Boss completion settle before releasing its audio owner.
                await Frames(tree, 3);
                if (mapped)
                {
                    var completion = level.GetNode<PrologueCompletionController>("Completion");
                    TestAssert.True(flow.State == PrologueGameFlowState.Completed && completion.CompletionCount == 1,
                        "P0 migrated explicit Boss result completes once after ordered objective restore");
                    level.RequestRespawn("terminal-migration-recovery");
                    await Frames(tree, 2);
                    TestAssert.True(flow.State == PrologueGameFlowState.Completed && completion.CompletionCount == 1,
                        "P0 terminal recovery cannot revive or duplicate completion");
                    var completed = PrologueSaveStore.LoadOrNew(next, 2);
                    TestAssert.True(completed.Completed && completed.BossDefeated,
                        "P0 completion persistence retains the explicit Boss result");
                    Preserve(legacy, hash, "completion after mapped Continue");
                }
            }
            finally
            {
                if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                Directory.Delete(root, true);
            }
        }
    }

    public static async Task RunPersistence(SceneTree tree)
    {
        foreach (bool completeSeal in new[] { false, true })
        {
            string root = Path.Combine(Path.GetTempPath(), "knight-task7-live-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            PrologueLevelController level = null;
            int defeatedEvents = 0, groupEvents = 0;
            void Observe(Node node)
            {
                if (node is IEncounterActor actor) actor.Defeated += () => defeatedEvents++;
                if (node is CanyonEncounterGroup group) group.GroupDefeated += _ => groupEvents++;
            }
            tree.NodeAdded += Observe;
            try
            {
                level = await Create(tree, root);
                var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.StartNew();
                level.Player.GetNode("Motor").SetPhysicsProcess(false);
                var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                level.Player.GlobalPosition = new Vector2(1680,3960);
                await Frames(tree, 5);
                TestAssert.Equal("canyon.v2.basin-camp", level.Progress.RestoreCheckpoint().CheckpointId,
                    "real serialized Basin trigger publishes the stable ID on actual contact");
                var basin = Sector(world, PrologueSectorId.CanyonBasin);
                TestAssert.True(basin.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve")
                    .TryReceiveThread(default), "operate actual generated steam valve");
                level.Player.GlobalPosition = new Vector2(2300,3960);
                await Frames(tree, 5);
                var ordinary = basin.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/GearPistonsEncounter");
                for (int batch = 0; batch < 2; batch++)
                {
                    Node actor = basin.GetNode("Gameplay/Actors").GetChildren()
                        .Single(child => child is IEncounterActor);
                    var health = actor.GetNode<HealthComponent>("Health");
                    TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,
                        Vector2.Right, 0f, (ulong)(710010 + batch))), "defeat a real ordinary group before saving");
                    await Frames(tree, 3);
                }
                TestAssert.True(ordinary.IsComplete && defeatedEvents == 2 && groupEvents == 1 &&
                    !level.Progress.ActivatedMechanisms.Contains("canyon.v2.encounter.gear-pistons"),
                    "genuine ordinary kills stay session-only, not a durable mechanism marker");

                level.Player.GlobalPosition = new Vector2(4500,1800);
                await Frames(tree, 5);
                var ascent = Sector(world, PrologueSectorId.CanyonAscent);
                var winch = ascent.GetNode<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
                TestAssert.True(winch.TryReceiveThread(default), "operate actual generated shortcut, not a save list");
                var crank = ascent.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/CourtyardSealLinkage");
                TestAssert.True(crank.TryReceiveThread(default), "operate actual generated first seal prerequisite");
                var main7 = ascent.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/SplitLevelEncounter");
                TestAssert.True(main7.IsActive && main7.LiveActorCount == 1, "real Main7 begins its first actor batch");
                if (completeSeal)
                {
                    for (int batch = 0; batch < 2; batch++)
                    {
                        Node actor = ascent.GetNode("Gameplay/Actors").GetChildren()
                            .Single(child => child is IEncounterActor);
                        HealthComponent health = actor.GetNode<HealthComponent>("Health");
                        TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth,
                            Vector2.Right, 0f, (ulong)(710000 + batch))), "defeat the actual prerequisite actor through Health");
                        await Frames(tree, 3);
                    }
                    TestAssert.True(main7.IsComplete && defeatedEvents == 4 && groupEvents == 2,
                        "both real actor defeats, not Complete() or injected flags, supply Main7 prerequisite");
                }
                level.Player.GlobalPosition = new Vector2(5920,1080);
                await Frames(tree, 5);
                var temple = Sector(world, PrologueSectorId.LostTemple);
                TestAssert.Equal("canyon.v2.temple-forecourt", level.Progress.RestoreCheckpoint().CheckpointId,
                    "real serialized Temple trigger publishes stable checkpoint identity");
                TestAssert.True(temple.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain")
                    .TryReceiveThread(default), "operate the independent actual bell gate");
                await Frames(tree, 2);
                AssertSeal(temple, level.Progress, completeSeal);
                flow.PersistProgress("task7-real-mechanisms");
                var disk = PrologueSaveStore.LoadOrNew(Path.Combine(root, "prologue-save-layout-v2.json"), 2);
                TestAssert.True(disk.OpenShortcuts.Contains("canyon.v2.upper-basin-shortcut") &&
                    disk.ActivatedMechanisms.Contains("canyon.v2.steam-valve-lower") &&
                    disk.ActivatedMechanisms.Contains("canyon.v2.courtyard-crank"), "real mechanism state reaches disk");
                TestAssert.Equal(completeSeal, disk.ActivatedMechanisms.Contains("canyon.v2.courtyard-seal-open"),
                    "one prerequisite never fabricates completed seal in the save");
                TestAssert.True(!disk.ActivatedMechanisms.Contains("canyon.v2.encounter.gear-pistons"),
                    "the actually defeated ordinary group has no disk-persistent kill marker");
                var oldProgress = level.Progress;
                flow.ReturnToTitle();
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                level = null;
                int priorDefeats = defeatedEvents, priorGroups = groupEvents;
                level = await Create(tree, root);
                flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                TestAssert.True(!ReferenceEquals(oldProgress, level.Progress), "Continue uses a fresh progress/world instance");
                flow.Continue();
                level.Player.GetNode("Motor").SetPhysicsProcess(false);
                await Frames(tree, 5);
                world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
                temple = Sector(world, PrologueSectorId.LostTemple);
                ascent = Sector(world, PrologueSectorId.CanyonAscent);
                AssertSeal(temple, level.Progress, completeSeal);
                winch = ascent.GetNode<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
                TestAssert.True(winch.IsOpen && winch.BarrierCollision.Disabled &&
                    winch.GetNode<CanvasItem>(winch.OpenVisualPath).Visible &&
                    !winch.GetNode<CanvasItem>(winch.ClosedVisualPath).Visible,
                    $"fresh-world shortcut restores collision and visual state; progress={string.Join(',',level.Progress.OpenShortcuts)} " +
                    $"disk={string.Join(',',flow.Save.OpenShortcuts)} open={winch.IsOpen} collision={winch.BarrierCollision.Disabled} " +
                    $"openVisual={winch.GetNode<CanvasItem>(winch.OpenVisualPath).Visible} closedVisual={winch.GetNode<CanvasItem>(winch.ClosedVisualPath).Visible}");
                TestAssert.True(CanyonRouteGraph.Reachable(CanyonRouteNodeId.SplitLevelEncounter,
                    level.Progress.OpenShortcuts, level.Progress.ActivatedMechanisms, false).Contains(CanyonRouteNodeId.BasinCamp) &&
                    CanyonRouteGraph.Reachable(CanyonRouteNodeId.BasinCamp,
                    level.Progress.OpenShortcuts, level.Progress.ActivatedMechanisms, false).Contains(CanyonRouteNodeId.SplitLevelEncounter),
                    "both shortcut directions remain graph-open after fresh Continue");
                TestAssert.True(temple.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain").IsOpen,
                    "independent bell gate survives without conflating shortcut IDs");
                TestAssert.Equal(completeSeal, ascent.GetNode<CanyonEncounterGroup>(
                    "Gameplay/Encounters/SplitLevelEncounter").IsComplete, "only real completed prerequisite is restored");
                level.Player.GlobalPosition = new Vector2(1680,3960);
                await Frames(tree, 5);
                basin = Sector(world, PrologueSectorId.CanyonBasin);
                TestAssert.True(basin.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve").IsActivated &&
                    basin.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet").DisabledForRun,
                    "actual steam valve and damage jet restore together on fresh mount");
                level.Player.GlobalPosition = new Vector2(2300,3960);
                await Frames(tree, 5);
                TestAssert.True(basin.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/GearPistonsEncounter").IsActive,
                    "the previously defeated ordinary encounter is fresh again after new-world Continue");
                TestAssert.True(defeatedEvents == priorDefeats && groupEvents == priorGroups && !level.Progress.BossDefeated,
                    "fresh Continue emits no additional actor-defeat/group reward signal or fabricated Boss result");
                flow.ReturnToTitle();
                GD.Print($"TASK7_FRESH_MECHANISM_CONTINUE_OK completed_seal={completeSeal} defeats={defeatedEvents} groups={groupEvents}");
            }
            finally
            {
                tree.NodeAdded -= Observe;
                if (GodotObject.IsInstanceValid(level))
                {
                    level.GetNode<PrologueGameFlowController>("GameFlow").ReturnToTitle();
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                }
                Directory.Delete(root, true);
            }
        }
    }

    private static PrologueSector Sector(SeamlessPrologueWorld world, PrologueSectorId id)
    {
        TestAssert.True(world.TryGetMountedSector(id, out var sector), $"actual world mounted {id}");
        return sector;
    }

    public static async Task RunRecovery(SceneTree tree)
    {
        string root = Path.Combine(Path.GetTempPath(), "knight-task7-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        PrologueLevelController level = null;
        try
        {
            var completed = PrologueSaveData.NewLayoutV2();
            completed.CheckpointId = "canyon.v2.temple-forecourt";
            completed.Completed = true;
            completed.BossDefeated = true;
            completed.Objective = nameof(PrologueObjective.Complete);
            completed.BestHumanCompletionSeconds = 640;
            completed.BestHumanDeaths = 2;
            PrologueSaveStore.SaveAtomic(Path.Combine(root, "prologue-save-layout-v2.json"), completed);
            level = await Create(tree, root);
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(!flow.HasSave, "P0 completed current save cannot Continue");
            flow.StartNew();
            TestAssert.True(!flow.Save.Completed && !level.Progress.BossDefeated &&
                flow.Save.BestHumanCompletionSeconds == 640 && flow.Save.BestHumanDeaths == 2,
                "P0 new run resets completed progress while retaining paired human metrics");
            Vector2 recoveredAt = new(-1,-1);
            int recovered = 0;
            level.RecoveryCompleted += _ => { recovered++; recoveredAt = level.Player.GlobalPosition; };
            var health = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0, 710100)),
                "P0 genuine lethal damage requests first-death recovery");
            await Frames(tree, 5);
            TestAssert.True(recovered == 1 && recoveredAt == new Vector2(160,600) &&
                flow.State == PrologueGameFlowState.Playing && health.CurrentHealth == health.MaxHealth,
                "P0 first death restores the round start and resumes only after recovery");
            level.Player.GlobalPosition = new Vector2(1680,3960);
            await Frames(tree, 5);
            TestAssert.Equal("canyon.v2.basin-camp", level.Progress.RestoreCheckpoint().CheckpointId,
                "P0 Basin must activate physically before recovery authority advances");
            level.RequestRespawn("stable-basin-recovery");
            await Frames(tree, 5);
            TestAssert.True(recovered == 2 && recoveredAt == new Vector2(1680,3960) &&
                flow.State == PrologueGameFlowState.Playing,
                "P0 post-checkpoint recovery restores the stable ID authored coordinate atomically");
            flow.ReturnToTitle();
            GD.Print("TASK7_GENERATED_P0_RECOVERY_OK");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level))
            {
                level.GetNode<PrologueGameFlowController>("GameFlow").ReturnToTitle();
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            }
            Directory.Delete(root, true);
        }
    }

    private static void AssertSeal(PrologueSector temple, PrologueProgress progress, bool open)
    {
        var seal = temple.GetNode<CanyonSealGate>("Gameplay/Mechanisms/CourtyardSeal");
        TestAssert.True(seal.IsOpen == open && seal.Collision.Disabled == open &&
            seal.GetNode<CanvasItem>(seal.OpenVisualPath).Visible == open &&
            seal.GetNode<CanvasItem>(seal.ClosedVisualPath).Visible != open &&
            CanyonRouteGraph.Reachable(CanyonRouteNodeId.SealedCourtyard,
                progress.OpenShortcuts, progress.ActivatedMechanisms, false).Contains(CanyonRouteNodeId.BossHall) == open,
            "seal completion agrees in collision, visuals and route graph");
    }

    private static async Task<PrologueLevelController> Create(SceneTree tree, string root, bool autoStart = false)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.SavePath = Path.Combine(root, "prologue-save-layout-v2.json");
        flow.SettingsPath = Path.Combine(root, "settings.json");
        flow.HeadlessAutoStart = autoStart;
        flow.AutomationAutoStart = false;
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        return level;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Preserve(string path, string hash, string stage)
    {
        string after = Hash(path);
        TestAssert.Equal(hash, after, stage + " leaves old bytes unchanged");
        GD.Print($"TASK7_BYTES stage={stage} before={hash} after={after}");
    }
}
