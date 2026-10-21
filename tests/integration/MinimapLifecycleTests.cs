using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Presentation.Minimap;
using Knight.Presentation.Fog;
using System.Reflection;
using Knight.Combat;
using Knight.Save;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;
using Knight.Settings;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;

namespace Knight.Tests.Integration;

public static class MinimapLifecycleTests
{
    public static async Task Run(SceneTree tree)
    {
        await QuitDurableSuccessRequiresActualWrite(tree);
        await CurrentFrameReveals(tree);
        await PauseNeverReveals(tree);
        await RealDeathKeepsMap(tree);
        await ContinueOnlyDurableMap(tree);
        await NewGameClearsMap(tree);
        await RetiredWorldObservationRejected(tree);
        await RetiredPlayerAndWriteEligibility(tree);
        await AdjacentMountedUnionAndUnload(tree);
        await FragmentFailedCommitKeepsBothStates(tree);
        await QuitFailureRetryCancelStaysPaused(tree);
        await NormalQuitExactlyOnce(tree);
        await NoSaveExitAndStandalonePause(tree);
        GD.Print("MINIMAP_LIFECYCLE_OK cases=13");
    }

    private static async Task QuitDurableSuccessRequiresActualWrite(SceneTree tree)
    {
        // Invariant: true must mean an actual SaveAtomic temp was created for
        // this unique request's slot, not a void persistence early-return.
        // No production hook: race only real atomic file replacements, with
        // bounded attempts declared before execution and an independent watcher.
        const int maximumAttempts = 128;
        var f = new Fixture(tree, "quit-durable-write-result");
        var created = new ConcurrentDictionary<string, byte>();
        var watcherErrors = new ConcurrentQueue<string>();
        int successes = 0, refusals = 0, transitions = 0;
        try
        {
            await f.Start(tree); Present(f);
            byte[] valid = System.Text.Encoding.UTF8.GetBytes(System.IO.File.ReadAllText(f.Paths.SavePath) + new string(' ', 1048576));
            byte[] corrupt = System.Text.Encoding.UTF8.GetBytes("owned invalid transition");
            using var watcher = new FileSystemWatcher(f.Paths.CaseRoot, ".*.json.*.tmp")
                { NotifyFilter = NotifyFilters.FileName, InternalBufferSize = 65536 };
            watcher.Created += (_, e) => created.TryAdd(Path.GetFileName(e.FullPath), 0);
            watcher.Error += (_, e) => watcherErrors.Enqueue(e.GetException().ToString());
            watcher.EnableRaisingEvents = true;
            bool Written(string slot) => created.Keys.Any(name => name.StartsWith("." + Path.GetFileName(slot) + ".", StringComparison.Ordinal));

            // Real successful preflight proves watcher delivery before racing.
            f.Flow.SavePath = Path.Combine(f.Paths.CaseRoot, "positive-control.json");
            TestAssert.True(f.Flow.TrySaveBeforeQuit(out _), "watcher positive control performs actual eligible save");
            TestAssert.True(SpinWait.SpinUntil(() => Written(f.Flow.SavePath), 2000), "watcher positive control receives actual temp creation");
            for (int attempt = 0; attempt < maximumAttempts; attempt++)
            {
                string slot = Path.Combine(f.Paths.CaseRoot, $"transition-save-{attempt:D3}.json");
                string seed = Path.Combine(f.Paths.CaseRoot, $"transition-seed-{attempt:D3}.json");
                f.Flow.SavePath = slot;
                System.IO.File.WriteAllBytes(slot, valid);
                TestAssert.True((bool)typeof(PrologueGameFlowController).GetMethod("CanWriteFullWorldSlot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(f.Flow, null),
                    "each transition starts from actual admitted eligible slot");
                using var stop = new ManualResetEventSlim(false);
                using var firstReplacement = new ManualResetEventSlim(false);
                Exception flipperFailure = null;
                int localTransitions = 0;
                var flipper = new Thread(() =>
                {
                    bool nextValid = true;
                    try
                    {
                        while (!stop.IsSet)
                        {
                            System.IO.File.WriteAllBytes(seed, nextValid ? valid : corrupt);
                            try
                            {
                                System.IO.File.Move(seed, slot, true);
                                nextValid = !nextValid;
                                Interlocked.Increment(ref localTransitions);
                                firstReplacement.Set();
                            }
                            catch (IOException) { } // Reader may hold no-Delete sharing.
                            catch (UnauthorizedAccessException) { }
                            Thread.Sleep(1);
                        }
                    }
                    catch (Exception exception) { flipperFailure = exception; firstReplacement.Set(); }
                }) { IsBackground = true, Name = "owned-quit-slot-transition" };
                bool result = false; string failure = null;
                flipper.Start();
                try
                {
                    TestAssert.True(firstReplacement.Wait(2000), "actual transition worker reaches first replacement");
                    result = f.Flow.TrySaveBeforeQuit(out failure);
                }
                finally { stop.Set(); TestAssert.True(flipper.Join(2000), "owned transition worker terminates"); }
                TestAssert.True(flipperFailure == null && watcherErrors.IsEmpty, "transition/watcher infrastructure remains valid");
                transitions += localTransitions;
                if (!result) { refusals++; TestAssert.True(!string.IsNullOrEmpty(failure), "refusal has failure detail"); continue; }
                successes++;
                bool observedWrite = SpinWait.SpinUntil(() => Written(slot), 2000);
                if (!observedWrite)
                {
                    f.Flow.SavePath = Path.Combine(f.Paths.CaseRoot, $"negative-postflight-control-{attempt:D3}.json");
                    TestAssert.True(f.Flow.TrySaveBeforeQuit(out _) && SpinWait.SpinUntil(() => Written(f.Flow.SavePath), 2000),
                        "negative witness is bracketed by live watcher actual-write controls");
                }
                TestAssert.True(watcherErrors.IsEmpty, "watcher overflow invalidates race witness");
                GD.Print($"MINIMAP_QUIT_TRANSITION_SAMPLE attempt={attempt} admitted_before=True transitions={localTransitions} result={result} actual_temp={observedWrite}");
                TestAssert.True(observedWrite,
                    "QuitDurableSuccessRequiresActualWrite: quit reported success after eligibility transition without an actual SaveAtomic attempt");
            }
            string refusedSlot = Path.Combine(f.Paths.CaseRoot, "controlled-refusal.json");
            f.Flow.SavePath = refusedSlot;
            System.IO.File.WriteAllBytes(refusedSlot, valid);
            TestAssert.True((bool)typeof(PrologueGameFlowController).GetMethod("CanWriteFullWorldSlot", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(f.Flow, null),
                "controlled refusal starts as actual admissible slot");
            System.IO.File.WriteAllBytes(refusedSlot, corrupt);
            TestAssert.True(!f.Flow.TrySaveBeforeQuit(out string refusedFailure) && !string.IsNullOrEmpty(refusedFailure) && !Written(refusedSlot),
                "actual admissible-to-corrupt refusal cannot return durable success or write");
            refusals++;
            // Postflight delivery control prevents a dead watcher being treated
            // as meaningful negative evidence; unique prefixes forbid late prior
            // request events from authorizing any subsequent request.
            f.Flow.SavePath = Path.Combine(f.Paths.CaseRoot, "postflight-control.json");
            TestAssert.True(f.Flow.TrySaveBeforeQuit(out _), "postflight actual save succeeds");
            TestAssert.True(SpinWait.SpinUntil(() => Written(f.Flow.SavePath), 2000) && watcherErrors.IsEmpty,
                "postflight watcher delivery and no-overflow control");
            TestAssert.True(!Written(refusedSlot), "controlled refusal remains write-free after watcher delivery drain");
            TestAssert.True(transitions >= maximumAttempts && refusals > 0, "bounded test exercised actual admissible-to-corrupt transitions and refusals");
            GD.Print($"MINIMAP_CASE_OK QuitDurableSuccessRequiresActualWrite attempts={maximumAttempts} transitions={transitions} successes_with_temp={successes} refusals={refusals} controls=2 watcher_overflow=0");
        }
        finally { await f.Free(tree); }
    }

    private sealed class Fixture
    {
        internal PrologueLevelController Level;
        internal PrologueGameFlowController Flow;
        internal ProloguePauseMenu Pause;
        internal PrologueFogController Fog;
        internal PrologueMinimapController Map;
        internal IsolatedPrologueFixturePaths Paths;
        internal int Quits;
        internal Fixture(SceneTree tree, string name, string existing = null, MinimapTerrainCatalog catalog = null)
        {
            Level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            Paths = existing == null ? IsolatedPrologueFixture.Configure(Level, name) : IsolatedPrologueFixture.ConfigureExisting(Level, existing);
            Flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
            Flow.HeadlessAutoStart = false;
            Pause = Level.GetNode<ProloguePauseMenu>("PauseMenu");
            Pause.QuitHandler = () => Quits++;
            Fog = Level.GetNode<PrologueFogController>("FogController");
            Map = Level.GetNode<PrologueMinimapController>("MinimapController");
            Map.Catalog = catalog ?? new MinimapTerrainCatalog(new Rect2(80, 480, 320, 320), "lifecycle-1");
            tree.Root.AddChild(Level);
        }
        internal async Task Start(SceneTree tree) { await Frames(tree, 3); Flow.StartNew(); await Frames(tree, 3); }
        internal async Task Free(SceneTree tree)
        {
            KeyEvent(Key.Escape, false); KeyEvent(Key.Enter, false); KeyEvent(Key.Tab, false);
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(Level)) Level.QueueFree();
            await Frames(tree, 3);
        }
    }

    private static void Present(Fixture f) => typeof(PrologueFogController)
        .GetMethod("OnFramePreDraw", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(f.Fog, null);
    private static void Replay(Fixture f, FogOcclusionObservation observation) => typeof(PrologueMinimapController)
        .GetMethod("OnObservation", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(f.Map, new object[] { observation });
    private static string Bits(Fixture f) => f.Map.CaptureExploration().FreeCellsBase64;
    private static void Press(Fixture f, string name) => f.Pause.GetNode<Button>("Panel/" + name).EmitSignal(BaseButton.SignalName.Pressed);
    private static void KeyEvent(Key key, bool down)
    { Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = down }); Input.FlushBufferedEvents(); }
    private static async Task Tap(SceneTree tree, Key key)
    { KeyEvent(key, true); await Frames(tree, 2); KeyEvent(key, false); await Frames(tree, 2); }
    private static void Same(string before, Fixture f, string name) => TestAssert.Equal(before, Bits(f), name);

    private static async Task PauseNeverReveals(SceneTree tree)
    {
        var f = new Fixture(tree, "pause-never-reveals");
        try
        {
            await f.Start(tree); Present(f);
            var old = new FogOcclusionObservation(f.Fog.LastOcclusionFrame, f.Level.Player.GlobalPosition,
                Engine.GetPhysicsFrames(), f.Level.GetNode<Node2D>("WorldRoot").GetInstanceId());
            string before = Bits(f);
            f.Pause.Open();
            f.Level.Player.GlobalPosition += new Vector2(200, -40);
            await Frames(tree, 3); Present(f); Replay(f, old);
            Same(before, f, "PauseNeverReveals: paused fog/replayed observations grant no bits");
            f.Pause.Execute(ProloguePauseCommand.Resume);
            await Frames(tree, 2); Replay(f, old);
            Same(before, f, "PauseNeverReveals: old capture cannot reveal after resume");
            Present(f);
            TestAssert.True(Bits(f) != before, "PauseNeverReveals: new live frame at relocated player does reveal");
            GD.Print("MINIMAP_CASE_OK PauseNeverReveals");
        }
        finally { await f.Free(tree); }
    }

    private static async Task RealDeathKeepsMap(SceneTree tree)
    {
        var f = new Fixture(tree, "real-death");
        try
        {
            await f.Start(tree); Present(f); string before = Bits(f);
            var health = f.Level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(99, Vector2.Right, 0, 881)), "actual lethal hit admitted");
            TestAssert.True(health.CurrentHealth == 0 && f.Flow.State == PrologueGameFlowState.Recovering, "actual death enters recovery");
            Present(f); Same(before, f, "RealDeathKeepsMap: lethal observation does not erase or reveal");
            for (int i = 0; i < 150 && f.Flow.State == PrologueGameFlowState.Recovering; i++) await Frames(tree, 1);
            TestAssert.True(health.CurrentHealth > 0 && f.Flow.State == PrologueGameFlowState.Playing, "actual recovery completed");
            Same(before, f, "RealDeathKeepsMap: respawn preserves live exploration");
            GD.Print("MINIMAP_CASE_OK RealDeathKeepsMap");
        }
        finally { await f.Free(tree); }
    }

    private static async Task ContinueOnlyDurableMap(SceneTree tree)
    {
        var f = new Fixture(tree, "durable-continue");
        string save = f.Paths.SavePath, durable = null;
        try
        {
            await f.Start(tree); Present(f); f.Flow.PersistProgress("fixture-durable-A"); durable = Bits(f);
            TestAssert.Equal(durable, f.Flow.Save.ExplorationMap.FreeCellsBase64, "successful save commits A DTO");
            f.Level.Player.GlobalPosition += new Vector2(200, -40); await Frames(tree, 3); Present(f);
            TestAssert.True(Bits(f) != durable, "ContinueOnlyDurableMap: actual observation B is unsaved");
            f.Pause.Open();
            using (var locked = new FileStream(save, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
            {
                Press(f, "QuitToDesktop");
                TestAssert.True(f.Pause.QuitFailureVisible, "actual replacement refusal opens failure UI");
                Press(f, "QuitFailure/ExitWithoutSaving");
                TestAssert.Equal(1, f.Quits, "explicit no-save quit calls original handler");
            }
            TestAssert.Equal(durable, PrologueSaveStore.LoadOrNew(save, mapCatalog: f.Map.Catalog).ExplorationMap.FreeCellsBase64, "durable A remains on disk");
        }
        finally { await f.Free(tree); }
        var reloaded = new Fixture(tree, "durable-reloaded", save);
        try
        {
            await Frames(tree, 3);
            TestAssert.True(reloaded.Flow.HasSave, "same admitted slot offers Continue");
            reloaded.Flow.Continue();
            TestAssert.True(reloaded.Flow.State == PrologueGameFlowState.Playing, "actual Continue reaches Playing");
            Same(durable, reloaded, "ContinueOnlyDurableMap: recreated actual flow restores A only, never unsaved B");
            GD.Print("MINIMAP_CASE_OK ContinueOnlyDurableMap");
        }
        finally { await reloaded.Free(tree); }
    }

    private static async Task NewGameClearsMap(SceneTree tree)
    {
        var f = new Fixture(tree, "new-game-clears");
        try
        {
            await f.Start(tree); Present(f); string revealed = Bits(f);
            f.Flow.ReturnToTitle(); f.Flow.StartNew();
            TestAssert.True(Bits(f) != revealed && Array.TrueForAll(Convert.FromBase64String(Bits(f)), b => b == 0),
                "NewGameClearsMap: actual StartNew clears before new-game atomic save");
            TestAssert.Equal(Bits(f), PrologueSaveStore.LoadOrNew(f.Paths.SavePath, mapCatalog: f.Map.Catalog).ExplorationMap.FreeCellsBase64,
                "NewGameClearsMap: disk stores cleared map");
            GD.Print("MINIMAP_CASE_OK NewGameClearsMap");
        }
        finally { await f.Free(tree); }
    }

    private static async Task RetiredWorldObservationRejected(SceneTree tree)
    {
        var f = new Fixture(tree, "retired-world");
        Node2D retiredWorld = null;
        try
        {
            await f.Start(tree); Present(f); string before = Bits(f);
            await Frames(tree, 2);
            Replay(f, new FogOcclusionObservation(f.Fog.LastOcclusionFrame, f.Level.Player.GlobalPosition,
                Engine.GetPhysicsFrames(), f.Level.GetNode<Node2D>("WorldRoot").GetInstanceId() + 10000));
            Same(before, f, "RetiredWorldObservationRejected: foreign world capture rejected");
            Node2D world = f.Level.GetNode<Node2D>("WorldRoot");
            world.Scale = new Vector2(2, 1); Present(f);
            Same(before, f, "unknown transformed loaded bounds fail closed");
            world.Scale = Vector2.One;
            await Frames(tree, 2);
            // Retirement is terminal: do not reenter old gameplay actors whose
            // original signals intentionally bind once in Ready.
            retiredWorld = world;
            f.Level.RemoveChild(world);
            f.Level.AddChild(new Node2D { Name = "WorldRoot" });
            Replay(f, new FogOcclusionObservation(f.Fog.LastOcclusionFrame, f.Level.Player.GlobalPosition,
                Engine.GetPhysicsFrames(), world.GetInstanceId()));
            Same(before, f, "RetiredWorldObservationRejected: detached original world capture rejected with replacement present");
            f.Map.QueueFree(); Present(f);
            Same(before, f, "retired queued controller rejects observations");
            GD.Print("MINIMAP_CASE_OK RetiredWorldObservationRejected");
        }
        finally { await f.Free(tree); if (GodotObject.IsInstanceValid(retiredWorld)) retiredWorld.Free(); }
    }

    private static async Task AdjacentMountedUnionAndUnload(SceneTree tree)
    {
        var catalog = new MinimapTerrainCatalog(new Rect2(1276, 80, 8, 8), "union-1");
        var f = new Fixture(tree, "adjacent-union", catalog: catalog);
        try
        {
            await f.Start(tree);
            var sectors = f.Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(sectors.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out _) &&
                sectors.TryGetMountedSector(PrologueSectorId.CanyonDescent, out _), "actual adjacent sectors mounted");
            f.Level.Player.GlobalPosition = new Vector2(1279, 76); await Frames(tree, 2); Present(f);
            TestAssert.True(f.Map.Model.IsCellExplored(0), "AdjacentMountedUnion: entire8px cell straddles actual x1280 mount union");
            ulong generation = f.Map.WorldGeneration; string before = Bits(f);
            TestAssert.True(sectors.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(1600, 3800)), "actual stream relocation admitted");
            await Frames(tree, 3); Present(f);
            TestAssert.True(!sectors.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out _), "actual crossing unloads original entrance");
            TestAssert.True(f.Map.WorldGeneration > generation, "mounted set change increments local generation");
            Same(before, f, "actual unload retains earlier exploration");
            GD.Print("MINIMAP_CASE_OK AdjacentMountedUnionAndUnload");
        }
        finally { await f.Free(tree); }
    }

    private static async Task RetiredPlayerAndWriteEligibility(SceneTree tree)
    {
        var f = new Fixture(tree, "retired-player");
        Node2D outsider = new() { Name = "RetiredPlayerOwner" };
        tree.Root.AddChild(outsider);
        try
        {
            await f.Start(tree); Present(f); string before = Bits(f);
            f.Level.Player.GlobalPosition += new Vector2(200, -40); await Frames(tree, 2);
            var observation = new FogOcclusionObservation(f.Fog.LastOcclusionFrame, f.Level.Player.GlobalPosition,
                Engine.GetPhysicsFrames(), f.Level.GetNode<Node2D>("WorldRoot").GetInstanceId());
            f.Level.Player.Reparent(outsider, true);
            Replay(f, observation);
            Same(before, f, "retired player outside owning level cannot reveal despite live native identity");
            GD.Print("MINIMAP_CASE_OK RetiredPlayerOwnership");
        }
        finally { await f.Free(tree); outsider.QueueFree(); await Frames(tree, 2); }

        var rejected = new Fixture(tree, "write-ineligible");
        try
        {
            // Seed before deferred initialization; destination was admitted and
            // configured before AddChild. No production/default save is touched.
            System.IO.File.WriteAllText(rejected.Paths.SavePath, "not valid json");
            await Frames(tree, 3);
            TestAssert.True(!rejected.Flow.TrySaveBeforeQuit(out string failure) && !string.IsNullOrEmpty(failure),
                "write eligibility rejection cannot report an early no-op as durable success");
            TestAssert.Equal("not valid json", System.IO.File.ReadAllText(rejected.Paths.SavePath), "ineligible original slot untouched");
            GD.Print("MINIMAP_CASE_OK QuitWriteEligibilityNoFalseSuccess");
        }
        finally { await rejected.Free(tree); }
    }

    private static async Task FragmentFailedCommitKeepsBothStates(SceneTree tree)
    {
        var f = new Fixture(tree, "fragment-map-transaction");
        try
        {
            await f.Start(tree); Present(f); f.Flow.PersistProgress("durable-A"); string durable = Bits(f);
            f.Level.Player.GlobalPosition += new Vector2(200, -40); await Frames(tree, 2); Present(f); string unsaved = Bits(f);
            TestAssert.True(unsaved != durable, "fragment fixture has unsaved exploration");
            string sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(f.Paths.SavePath)));
            using (var locked = new FileStream(f.Paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
            {
                TestAssert.Equal(FragmentCollectionResult.SaveFailed, f.Flow.TryCollectFragment("canyon.v2.fragment.entrance"), "FragmentFailedCommitKeepsBothStates: actual replacement fails");
                TestAssert.True(!f.Level.Progress.CollectedFragmentIds.Contains("canyon.v2.fragment.entrance") &&
                    !f.Flow.Save.CollectedFragmentIds.Contains("canyon.v2.fragment.entrance"), "failed fragment remains absent live/durable");
                TestAssert.Equal(durable, f.Flow.Save.ExplorationMap.FreeCellsBase64, "failed fragment cannot commit map DTO");
                Same(unsaved, f, "failed fragment does not erase unsaved exploration");
                TestAssert.Equal(sha, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(f.Paths.SavePath))), "failed fragment preserves exact slot SHA");
            }
            TestAssert.Equal(FragmentCollectionResult.Collected, f.Flow.TryCollectFragment("canyon.v2.fragment.entrance"), "retry fragment commits real slot");
            TestAssert.Equal(unsaved, f.Flow.Save.ExplorationMap.FreeCellsBase64, "fragment success atomically commits current map");
            GD.Print("MINIMAP_CASE_OK FragmentFailedCommitKeepsBothStates");
        }
        finally { await f.Free(tree); }
    }

    private static async Task CurrentFrameReveals(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, "minimap-live");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        var controller = level.GetNode<PrologueMinimapController>("MinimapController");
        controller.Catalog = new MinimapTerrainCatalog(new Rect2(80, 480, 320, 320), "lifecycle-1");
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 3);
            flow.StartNew();
            await Frames(tree, 3);
            var fog = level.GetNode<PrologueFogController>("FogController");
            typeof(PrologueFogController).GetMethod("OnFramePreDraw", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fog, null);
            TestAssert.True(controller.Model.RevisionCounter > 1,
                "CurrentFrameReveals: successful live fog branch reveals actual player cells");
        }
        finally { tree.Paused = false; level.QueueFree(); await Frames(tree, 3); }
    }

    private static async Task QuitFailureRetryCancelStaysPaused(SceneTree tree)
    {
        var f = new Fixture(tree, "minimap-quit");
        try
        {
            await f.Start(tree); Present(f); string map = Bits(f);
            string blocker = Path.Combine(f.Paths.CaseRoot, "blocked-parent");
            System.IO.File.WriteAllText(blocker, "owned filesystem refusal");
            f.Flow.SavePath = Path.Combine(blocker, "save.json");
            f.Pause.Open(); Press(f, "QuitToDesktop");
            TestAssert.True(tree.Paused && f.Pause.IsOpen && f.Quits == 0,
                "QuitFailureRetryCancelStaysPaused: failed actual save keeps pause open and invokes Quit zero times");
            TestAssert.True(f.Pause.QuitFailureVisible && f.Pause.GetNode<Button>("Panel/QuitFailure/Retry").HasFocus(), "failure modal focuses keyboard Retry");
            foreach (GameLanguage language in new[] { GameLanguage.ChineseSimplified, GameLanguage.English })
            {
                f.Pause.SetLanguage(language);
                string warning = f.Pause.GetNode<Label>("Panel/QuitFailure/Message").Text;
                TestAssert.True(language == GameLanguage.English ? warning.Contains("unsaved exploration") && warning.Contains("last successful save") :
                    warning.Contains("上次成功保存") && warning.Contains("探索进度"), "both languages explicitly warn about unsaved exploration loss");
                foreach (string name in new[] { "Retry", "ExitWithoutSaving", "Cancel" })
                    TestAssert.True(f.Pause.GetNode<Button>("Panel/QuitFailure/" + name).FocusMode == Control.FocusModeEnum.All,
                        "authored modal choice is keyboard focusable");
            }
            f.Pause.Execute(ProloguePauseCommand.Resume); f.Pause.Execute(ProloguePauseCommand.ReturnToCheckpoint);
            TestAssert.True(tree.Paused && f.Flow.State == PrologueGameFlowState.Paused, "modal blocks ordinary resume/checkpoint commands");
            await Tap(tree, Key.Escape);
            TestAssert.True(tree.Paused && f.Pause.IsOpen && !f.Pause.QuitFailureVisible &&
                f.Pause.GetNode<Button>("Panel/QuitToDesktop").HasFocus(), "Esc cancels modal only, never resumes same frame");
            Same(map, f, "Esc cancellation preserves map");
            Press(f, "QuitToDesktop"); Press(f, "QuitFailure/Cancel");
            TestAssert.True(tree.Paused && !f.Pause.QuitFailureVisible && f.Quits == 0, "Cancel signal stays paused without saving/clearing");

            // Real normal slot replacement refusal reaches GUID-temp write and
            // File.Move. Test-only watcher counts actual temp-file creations.
            f.Flow.SavePath = f.Paths.SavePath;
            var writes = new ConcurrentDictionary<string, byte>();
            using var watcher = new FileSystemWatcher(f.Paths.CaseRoot, ".save.json.*.tmp") { NotifyFilter = NotifyFilters.FileName };
            watcher.Created += (_, e) => writes.TryAdd(e.FullPath, 0);
            watcher.EnableRaisingEvents = true;
            string durable = f.Flow.Save.ExplorationMap.FreeCellsBase64;
            byte[] oldBytes = System.IO.File.ReadAllBytes(f.Paths.SavePath);
            using (var locked = new FileStream(f.Paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read))
            {
                Press(f, "QuitToDesktop");
                TestAssert.True(SpinWait.SpinUntil(() => writes.Count == 1, 1000), "quit reaches one actual temp write before replacement refusal");
                TestAssert.True(f.Pause.QuitFailureVisible && tree.Paused && f.Quits == 0, "actual regular-slot refusal keeps quit uncommitted");
                TestAssert.Equal(durable, f.Flow.Save.ExplorationMap.FreeCellsBase64, "failed quit cannot advance durable map DTO");
                TestAssert.Equal(Convert.ToBase64String(oldBytes), Convert.ToBase64String(System.IO.File.ReadAllBytes(f.Paths.SavePath)), "quit failure preserves exact old bytes");
                await Tap(tree, Key.Enter); // Focus is Retry: real GUI key event.
                TestAssert.True(SpinWait.SpinUntil(() => writes.Count == 2, 1000), "keyboard Retry performs exactly one actual attempt");
                TestAssert.True(f.Pause.QuitFailureVisible && f.Quits == 0 && tree.Paused, "failed keyboard retry remains modal and paused");
            }
            Press(f, "QuitFailure/Retry");
            TestAssert.True(SpinWait.SpinUntil(() => writes.Count == 3, 1000), "successful retry performs exactly one atomic attempt");
            TestAssert.Equal(1, f.Quits, "only successful retry calls original handler once");
            Press(f, "QuitFailure/Retry"); Press(f, "QuitToDesktop"); Press(f, "QuitFailure/ExitWithoutSaving");
            await Tap(tree, Key.Escape);
            TestAssert.True(f.Quits == 1 && writes.Count == 3 && tree.Paused && f.Pause.IsOpen, "terminal quit latch survives returning handler and all duplicate controls");
            TestAssert.Equal(map, PrologueSaveStore.LoadOrNew(f.Paths.SavePath, mapCatalog: f.Map.Catalog).ExplorationMap.FreeCellsBase64,
                "successful retry persists unsaved exploration");
            TestAssert.Equal(0, Directory.GetFiles(f.Paths.CaseRoot, "*.tmp").Length, "real refusal/success leaves no temp leaks");
            GD.Print("MINIMAP_CASE_OK QuitFailureRetryCancelStaysPaused directory_refusal=True replacement_temp_attempts=3 quit_calls=1");
        }
        finally { await f.Free(tree); }
    }

    private static async Task NormalQuitExactlyOnce(SceneTree tree)
    {
        var f = new Fixture(tree, "normal-quit");
        try
        {
            await f.Start(tree); Present(f); f.Pause.Open();
            f.Pause.QuitHandler = () => { f.Quits++; Press(f, "QuitToDesktop"); };
            Press(f, "QuitToDesktop"); Press(f, "QuitToDesktop");
            TestAssert.Equal(1, f.Quits, "NormalQuitExactlyOnce: reentrant and later quit requests ignored after commit");
            TestAssert.Equal(Bits(f), PrologueSaveStore.LoadOrNew(f.Paths.SavePath, mapCatalog: f.Map.Catalog).ExplorationMap.FreeCellsBase64, "normal quit commits actual map");
            GD.Print("MINIMAP_CASE_OK NormalQuitExactlyOnce");
        }
        finally { await f.Free(tree); }
    }

    private static async Task NoSaveExitAndStandalonePause(SceneTree tree)
    {
        var f = new Fixture(tree, "no-save-exit");
        try
        {
            await f.Start(tree); Present(f); string map = Bits(f);
            byte[] durable = System.IO.File.ReadAllBytes(f.Paths.SavePath);
            f.Pause.Open();
            using var locked = new FileStream(f.Paths.SavePath, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
            Press(f, "QuitToDesktop");
            var writes = new ConcurrentDictionary<string, byte>();
            using var watcher = new FileSystemWatcher(f.Paths.CaseRoot, ".save.json.*.tmp");
            watcher.Created += (_, e) => writes.TryAdd(e.FullPath, 0); watcher.EnableRaisingEvents = true;
            f.Pause.GetNode<Button>("Panel/QuitFailure/ExitWithoutSaving").GrabFocus();
            await Tap(tree, Key.Enter);
            TestAssert.True(f.Quits == 1 && writes.Count == 0 && tree.Paused && f.Pause.IsOpen, "explicit keyboard no-save exit invokes quit without any write");
            Press(f, "QuitFailure/ExitWithoutSaving"); Press(f, "QuitFailure/Retry"); Press(f, "QuitToDesktop");
            TestAssert.True(f.Quits == 1 && writes.Count == 0, "no-save terminal latch prevents duplicate quit/write");
            TestAssert.Equal(Convert.ToBase64String(durable), Convert.ToBase64String(System.IO.File.ReadAllBytes(f.Paths.SavePath)), "no-save retains exact durable bytes");
            Same(map, f, "no-save choice does not clear live map before handler");
        }
        finally { await f.Free(tree); }

        Node original = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate();
        var standalone = original.GetNode<ProloguePauseMenu>("PauseMenu");
        original.RemoveChild(standalone); original.Free();
        int quits = 0; standalone.QuitHandler = () => quits++;
        tree.Root.AddChild(standalone);
        try
        {
            standalone.Open(); standalone.Execute(ProloguePauseCommand.Resume);
            TestAssert.True(!tree.Paused && !standalone.IsOpen, "standalone Pause without Flow resumes normally");
            standalone.Open();
            standalone.GetNode<Button>("Panel/QuitToDesktop").EmitSignal(BaseButton.SignalName.Pressed);
            standalone.Execute(ProloguePauseCommand.QuitToDesktop);
            TestAssert.True(quits == 1 && !standalone.QuitFailureVisible, "standalone absent Flow exits once without invented save failure");
            GD.Print("MINIMAP_CASE_OK NoSaveExitAndStandalonePause");
        }
        finally { KeyEvent(Key.Enter, false); tree.Paused = false; standalone.QueueFree(); await Frames(tree, 3); }
    }

    private static async Task Frames(SceneTree tree, int count)
    { for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); }
}
