using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

// Actual authored checkpoint and physics contact, with controlled actor positioning.
// Not natural traversal or human input. RunColdRetry separately reopens the
// retained actual GL retry slots in a new process through the title menu.
public static class CheckpointPersistenceRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await RunCase(tree, FileShare.None, "read-denied");
        await RunCase(tree, FileShare.Read, "replace-denied");
        GD.Print("CHECKPOINT_SAVE_FAILURE_OK read_denied=true replacement_denied=true physical_retry=true controlled_positions=true human=false");
    }

    public static async Task RunColdRetry(SceneTree tree)
    {
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        string input = Path.Combine(Directory.GetParent(storage)!.FullName, "checkpoint-input");
        foreach (var (tag, expected) in new[] {
            ("read-denied", "CF9A16A0029D9231FE1E1226C169FBD1DA4C821626C0163C7AB493B55BA8CDE3"),
            ("replace-denied", "1057246BA2CEE61A80EFEE7F0FB083F512876ECE25AB533D07669D9F15AD7DD0") })
        {
            var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            try
            {
                string source = Path.Combine(input, tag + ".json");
                TestAssert.Equal(expected, Hash(source), "cold checkpoint input is the actual terminated GL retry slot");
                var paths = IsolatedPrologueFixture.Configure(level, "checkpoint-cold-" + tag);
                File.Copy(source, paths.SavePath, overwrite: false);
                var original = PrologueSaveStore.LoadOrNew(paths.SavePath, 2);
                var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
                int sessions = 0, recoveries = 0;
                Vector2 initial = default;
                string restoredMap = null;
                level.RecoveryRequested += _ => recoveries++;
                flow.SessionStarted += () =>
                {
                    sessions++;
                    initial = level.Player.GlobalPosition;
                    restoredMap = JsonSerializer.Serialize(level.GetNode<Knight.Presentation.Minimap.PrologueMinimapController>("MinimapController").CaptureExploration());
                };
                tree.Root.AddChild(level);
                await Frames(tree, 5);
                // Map decoding needs the mounted catalog; the generic core-save
                // reader intentionally returns no map without that validator.
                original = PrologueSaveStore.LoadOrNew(paths.SavePath, 2,
                    level.GetNode<Knight.Presentation.Minimap.PrologueMinimapController>("MinimapController").Catalog);
                TestAssert.True(!original.WasRecovered && original.ExplorationMap != null,
                    "expected cold map is validated against the current terrain catalog");
                TestAssert.True(flow.State == PrologueGameFlowState.Title && flow.HasSave &&
                    flow.LoadResult.Status == PrologueSaveLoadStatus.Current && sessions == 0,
                    "new process offers Continue for the actual retry-written slot");
                var start = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start");
                var resume = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue");
                TestAssert.True(start.HasFocus() && !resume.Disabled, "cold title initially focuses Start");
                MenuKey(Key.Down, true); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); MenuKey(Key.Down, false);
                TestAssert.True(resume.HasFocus(), "actual Down chooses Continue");
                MenuKey(Key.Enter, true); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); MenuKey(Key.Enter, false);
                GD.Print($"CHECKPOINT_COLD_OBSERVED kind={tag} sessions={sessions} initial={initial} recoveries={recoveries} map_equal={restoredMap == JsonSerializer.Serialize(original.ExplorationMap)}");
                TestAssert.True(sessions == 1 && initial == new Vector2(1680, 3960) && recoveries == 0 &&
                    restoredMap == JsonSerializer.Serialize(original.ExplorationMap),
                    "cold session restores exact retried checkpoint and explored map before the next gameplay tick");
                await Frames(tree, 15);
                TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Player.IsOnFloor() &&
                    level.Player.GlobalPosition.DistanceTo(initial) < 2f && recoveries == 0 &&
                    flow.Save.CheckpointId == "canyon.v2.basin-camp" &&
                    level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.basin-camp" &&
                    level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").CheckpointId == "canyon.v2.basin-camp" &&
                    level.Progress.CollectedFragmentIds.Count == 3 &&
                    level.Progress.CollectedFragmentIds.SetEquals(original.CollectedFragmentIds) && Hash(paths.SavePath) == expected,
                    "cold Continue stands on the retried camp, preserves three IDs and leaves input bytes unchanged");
                GD.Print($"CHECKPOINT_COLD_RETRY_OK kind={tag} source_sha={expected} title_keys=true human=false source_was_controlled=true");
            }
            finally
            {
                MenuKey(Key.Down, false); MenuKey(Key.Enter, false);
                tree.Paused = false;
                if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            }
        }
    }

    private static void MenuKey(Key key, bool pressed)
    {
        using var input = new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }

    private static async Task RunCase(SceneTree tree, FileShare fileShare, string tag)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            var paths = IsolatedPrologueFixture.Configure(level, "checkpoint-" + tag);
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            flow.StartNew();
            await Frames(tree, 8);
            foreach (string id in new[] { "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift", "canyon.v2.fragment.abyss-floor" })
                TestAssert.True(flow.TryCollectFragment(id) == Knight.Prologue.Collections.FragmentCollectionResult.Collected,
                    "controlled fixture commits three real catalog IDs before checkpoint failure");
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(1860, 3960)),
                "checkpoint fixture mounts the authored Basin without collecting its checkpoint");
            await Frames(tree, 8);
            var checkpoint = level.CurrentSectorRoot.GetNode<CheckpointTrigger>("Gameplay/BasinCheckpoint");
            var visual = checkpoint.GetNode<PrologueMechanismVisual>("Visual");
            var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            TestAssert.True(!checkpoint.OverlapsBody(level.Player) &&
                !level.Progress.RestoreCheckpoint().HasActivatedCheckpoint && world.CheckpointId == "canyon.v2.start" &&
                !PrologueSaveStore.LoadOrNew(paths.SavePath, 2).HasActivatedCheckpoint,
                "controlled setup leaves live, disk and stable world checkpoint at start");
            int contacts = 0, successEvents = 0, triggerEvents = 0;
            bool durableAtNotification = true;
            checkpoint.BodyEntered += body => { if (body == level.Player) contacts++; };
            world.CheckpointActivated += (_, _) => successEvents++;
            checkpoint.CheckpointActivated += _ =>
            {
                triggerEvents++;
                var disk = PrologueSaveStore.LoadOrNew(paths.SavePath, 2);
                durableAtNotification &= !disk.WasRecovered && disk.CheckpointId == "canyon.v2.basin-camp" &&
                    disk.CollectedFragmentIds.Count == 3 && level.Progress.RestoreCheckpoint().CheckpointId == disk.CheckpointId &&
                    world.CheckpointId == disk.CheckpointId && flow.Save.CheckpointId == disk.CheckpointId &&
                    visual.CurrentState == MechanismVisualState.Active;
            };
            string oldHash = Hash(paths.SavePath);
            var oldSave = flow.Save;
            using (var locked = new FileStream(paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, fileShare))
            {
                if (fileShare == FileShare.Read)
                    TestAssert.True(!PrologueSaveStore.LoadOrNew(paths.SavePath, 2).WasRecovered,
                        "replacement failure permits preflight read and reaches the real atomic file write");
                level.Player.GlobalPosition = checkpoint.GlobalPosition;
                level.Player.Velocity = Vector2.Zero;
                await Frames(tree, 8);
                TestAssert.True(contacts == 1 && checkpoint.OverlapsBody(level.Player),
                    "failure fixture uses one real authored BodyEntered, not a synthetic signal");
                GD.Print($"CHECKPOINT_DENIED kind={tag} contacts={contacts} success_events={successEvents} " +
                    $"live={level.Progress.RestoreCheckpoint().CheckpointId} world={world.CheckpointId} " +
                    $"save={flow.Save.CheckpointId} visual={visual.CurrentState} objective={formal.CurrentModel.Objective}");
                TestAssert.True(!level.Progress.RestoreCheckpoint().HasActivatedCheckpoint &&
                    world.CheckpointId == "canyon.v2.start" && !flow.Save.HasActivatedCheckpoint &&
                    ReferenceEquals(oldSave, flow.Save) && visual.CurrentState != MechanismVisualState.Active && successEvents == 0 && triggerEvents == 0 &&
                    level.Progress.CollectedFragmentIds.Count == 3 && flow.Save.CollectedFragmentIds.Count == 3,
                    "CheckpointReadDeniedKeepsOldLiveState");
                TestAssert.True(Directory.GetFiles(paths.CaseRoot, ".save.json.*.tmp").Length == 0,
                    "failed atomic checkpoint write removes its own temporary file");
                var label = formal.GetNode<Label>("ObjectiveRibbon/Text");
                TestAssert.True(label.IsVisibleInTree() && label.Text.Contains("墓灯未保存", StringComparison.Ordinal),
                    "actual visible Chinese failure ribbon overrides ordinary contextual guidance");
                await Capture(tree, paths.CaseRoot, tag + "-cn.png");
                formal.SetLanguage(GameLanguage.English);
                await Frames(tree, 3);
                TestAssert.True(label.IsVisibleInTree() && label.Text == "SAVE FAILED · RE-ENTER LAMP",
                    "English failure is explicit and stays above live guidance ticks");
                await Capture(tree, paths.CaseRoot, tag + "-en.png");
                formal.SetLanguage(GameLanguage.ChineseSimplified);
            }
            TestAssert.Equal(oldHash, Hash(paths.SavePath), "failed checkpoint preserves exact old disk bytes");
            level.Player.GlobalPosition = new Vector2(1860, 3960);
            await Frames(tree, 6);
            TestAssert.True(!checkpoint.OverlapsBody(level.Player), "retry leaves actual Area2D overlap");
            level.Player.GlobalPosition = checkpoint.GlobalPosition;
            await Frames(tree, 8);
            var saved = PrologueSaveStore.LoadOrNew(paths.SavePath, 2);
            TestAssert.True(contacts == 2 && successEvents == 1 && triggerEvents == 1 && durableAtNotification &&
                !saved.WasRecovered && saved.CheckpointId == "canyon.v2.basin-camp" &&
                saved.HasActivatedCheckpoint && saved.CollectedFragmentIds.Count == 3 &&
                level.Progress.CollectedFragmentIds.SetEquals(saved.CollectedFragmentIds) &&
                flow.Save.CollectedFragmentIds.SequenceEqual(saved.CollectedFragmentIds) &&
                !ReferenceEquals(oldSave, flow.Save) && Hash(paths.SavePath) != oldHash &&
                !formal.GetNode<Label>("ObjectiveRibbon/Text").Text.Contains("未保存", StringComparison.Ordinal),
                "real reentry after unlocking durably commits once before success feedback and clears failure");
            await Capture(tree, paths.CaseRoot, tag + "-retry-success.png");
            string retryHash = Hash(paths.SavePath);
            level.Player.GlobalPosition = new Vector2(1860, 3960);
            await Frames(tree, 5);
            using (var duplicateWriteDenied = new FileStream(paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
            {
                level.Player.GlobalPosition = checkpoint.GlobalPosition;
                await Frames(tree, 6);
                TestAssert.True(contacts == 3 && successEvents == 2 && triggerEvents == 2 && durableAtNotification &&
                    Hash(paths.SavePath) == retryHash && !formal.GetNode<Label>("ObjectiveRibbon/Text").Text.Contains("未保存", StringComparison.Ordinal),
                    "already-current physical contact succeeds even with replacement locked, without a duplicate save");
            }
            tree.Paused = true;
            TestAssert.True(!world.TryActivateCheckpoint("canyon.v2.basin-camp") && Hash(paths.SavePath) == retryHash,
                "paused production world cannot bypass the checkpoint transaction gate");
            tree.Paused = false;
            TestAssert.True(!world.TryActivateCheckpoint("unknown") && !world.TryActivateCheckpoint("canyon.v2.temple-entry"),
                "invalid identity or a checkpoint outside the mounted current sector stays rejected");
            flow.ReturnToTitle();
            flow.Continue();
            await Frames(tree, 12);
            TestAssert.True(level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.basin-camp" &&
                flow.Save.CheckpointId == "canyon.v2.basin-camp" && level.Progress.CollectedFragmentIds.Count == 3 &&
                level.Player.GlobalPosition.DistanceTo(saved.CheckpointWorldPosition) < 2f,
                "same-session title Continue uses the successfully retried checkpoint and preserves quota");
            GD.Print($"CHECKPOINT_RETRY_OK kind={tag} path={paths.SavePath} controlled_positions=true cold_process=false");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private static async Task Frames(SceneTree tree, int count)
    { for (int frame = 0; frame < count; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static async Task Capture(SceneTree tree, string directory, string filename)
    {
        if (DisplayServer.GetName() == "headless") return;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = tree.Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, image.SavePng(Path.Combine(directory, filename)), "retain original GPU checkpoint frame");
    }
}
