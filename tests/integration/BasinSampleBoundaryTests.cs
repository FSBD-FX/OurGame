using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class BasinSampleBoundaryTests
{
    private static readonly Vector2 TempleCheckpoint = new(5040, 1294);

    public static async Task Run(SceneTree tree, string selected)
    {
        if (selected is "all" or "exit") await ExitAfterActualBossDeath(tree);
        if (selected is "all" or "fall") await OutsideFallRecovers(tree);
        if (selected is "all" or "continue") await TempleCheckpointContinues(tree);
        if (selected is "all" or "edges") await EnvelopeEdgesRespectGameplayState(tree);
        if (selected is "all" or "persistence") await DefeatedBossSurvivesRecoveryAndRemount(tree);
    }

    private static async Task ExitAfterActualBossDeath(SceneTree tree)
    {
        var fixture = await Fixture.Create(tree);
        try
        {
            await fixture.EnterTemple();
            var level = fixture.Entry.Level;
            var arena = level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            // Controlled Health death isolates post-boss exit mechanics. This is NOT
            // a claim that normal combat or the route from Basin has been played.
            level.Player.GlobalPosition = new Vector2(5920, 1294);
            level.Player.Velocity = Vector2.Zero;
            arena.BeginEncounter();
            var health = arena.CurrentBoss.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0, 940001UL)),
                "controlled BossHealth death is accepted by the real boss");
            await Frames(tree, 8);
            TestAssert.True(level.Progress.BossDefeated &&
                level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision").Disabled,
                "actual boss death opens the authored right exit before input traversal");
            Send(InputActions.MoveRight, true);
            for (int i = 0; i < 160 && fixture.Flow.State == PrologueGameFlowState.Playing; i++)
                await Frames(tree, 1);
            Send(InputActions.MoveRight, false);
            GD.Print($"SAMPLE_EXIT_AFTER_D state={fixture.Flow.State} pos={level.Player.GlobalPosition} " +
                $"velocity={level.Player.Velocity} boss={level.Progress.BossDefeated}");
            // On RED keep the failed route moving left long enough to record the
            // reported symptom rather than aborting before the out-of-world state.
            Send(InputActions.MoveLeft, true);
            await Frames(tree, 30);
            Send(InputActions.MoveLeft, false);
            GD.Print($"SAMPLE_EXIT_AFTER_A state={fixture.Flow.State} pos={level.Player.GlobalPosition}");
            TestAssert.Equal(PrologueGameFlowState.Completed, fixture.Flow.State,
                "real D through the defeated boss exit must enter the development sample ending before falling away");
            TestAssert.True(level.Player.GlobalPosition.X < 6080 && level.Player.GlobalPosition.Y < 1330,
                "ending takes over while the player still has authored floor");
            TestAssert.True(!level.Player.IsPhysicsProcessing(), "sample ending stops gameplay input movement");
            var view = level.GetNode<Control>("Hud/FormalHud/Completion");
            string title = view.GetNode<Label>("Panel/Title").Text;
            TestAssert.True(view.IsVisibleInTree() && title.Contains("样片") && !title.Contains("序章完成"),
                "ending displays a visible development-sample title, not a full-prologue completion claim");
            view.GetNodeOrNull<PrologueCompletionView>(".")?.SetLanguage(GameLanguage.English);
            TestAssert.True(view.GetNode<Label>("Panel/Title").Text.Contains("SAMPLE") &&
                !view.GetNode<Label>("Panel/Title").Text.Contains("PROLOGUE COMPLETE"),
                "development ending follows English without falling back to the full-game claim");
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            TestAssert.True(completion.CompletionCount == 0 && completion.CurrentObjective == PrologueObjective.Wake,
                "sample ending does not manufacture full-route objective completion");
            var save = PrologueSaveStore.LoadOrNew(fixture.SavePath);
            TestAssert.True(save.BossDefeated && !save.Completed && save.Objective == nameof(PrologueObjective.Wake),
                "sample preserves actual boss defeat without claiming full-game completion");
            TestAssert.True(save.BestHumanCompletionSeconds == -1d && save.BestHumanDeaths == -1,
                "controlled sample victory never manufactures human completion metrics");
            TestAssert.Equal(1, fixture.DefeatEvents, "real Health death emits exactly one encounter defeat");
            view.GetNode<Button>("Panel/ReturnToTitle").EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 3);
            TestAssert.Equal(PrologueGameFlowState.Title, fixture.Flow.State, "ending Return to Title is functional");
            TestAssert.True(fixture.Flow.HasSave, "post-boss Temple checkpoint can still continue");
            fixture.Flow.Continue();
            await Frames(tree, 8);
            TestAssert.True(level.Progress.BossDefeated, "post-boss Continue retains actual victory");
            var restoredArena = level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            TestAssert.True(!GodotObject.IsInstanceValid(restoredArena.CurrentBoss) ||
                restoredArena.CurrentBoss.GetNode<HealthComponent>("Health").CurrentHealth == 0,
                "post-boss Continue cannot resurrect the defeated boss");
            TestAssert.True(level.Player.GlobalPosition.DistanceTo(TempleCheckpoint) < 2f,
                "continued exit traversal starts at the saved checkpoint left of the arena entry");
            Send(InputActions.MoveRight, true);
            for (int i = 0; i < 300 && fixture.Flow.State == PrologueGameFlowState.Playing; i++)
                await Frames(tree, 1);
            Send(InputActions.MoveRight, false);
            GD.Print($"SAMPLE_CONTINUED_EXIT_D state={fixture.Flow.State} pos={level.Player.GlobalPosition}");
            TestAssert.Equal(PrologueGameFlowState.Completed, fixture.Flow.State,
                "continued victory walks from the real checkpoint through both gates to the ending without teleporting");
            TestAssert.Equal(1, fixture.DefeatEvents, "Continue and repeated ending emit no duplicate defeat");
            view.GetNode<Button>("Panel/Replay").EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 8);
            TestAssert.True(fixture.Flow.State == PrologueGameFlowState.Playing &&
                level.CurrentSector == PrologueSectorId.CanyonBasin && !level.Progress.BossDefeated && !view.Visible,
                "Replay starts a clean live Basin session without a stale ending");
            GD.Print("BASIN_SAMPLE_EXIT_OK controlled_boss_health=True real_D_A=True");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task OutsideFallRecovers(SceneTree tree)
    {
        var fixture = await Fixture.Create(tree);
        try
        {
            await fixture.EnterTemple();
            var level = fixture.Entry.Level;
            int recovered = 0;
            PrologueLevelController.RecoveryCompletedEventHandler onRecovery = _ =>
            {
                recovered++;
                Send(InputActions.MoveRight, false);
            };
            level.RecoveryCompleted += onRecovery;
            try
            {
                // Controlled initial edge placement; real D then walks off the
                // last floor, bypassing the finite bottom Area exactly as reported.
                level.Player.GlobalPosition = new Vector2(6086, 1294);
                level.Player.Velocity = Vector2.Zero;
                Send(InputActions.MoveRight, true);
                for (int i = 0; i < 150 && recovered == 0; i++) await Frames(tree, 1);
                Send(InputActions.MoveRight, false);
                await Frames(tree, 8);
                GD.Print($"SAMPLE_OUTSIDE_FALL pos={level.Player.GlobalPosition} recoveries={recovered}");
                TestAssert.Equal(1, recovered, "walking beyond the rightmost floor must use safe checkpoint recovery");
                TestAssert.True(level.Player.GlobalPosition.DistanceTo(TempleCheckpoint) < 2f && level.Player.IsOnFloor(),
                    "outside recovery returns a visible supported player to the real Temple checkpoint");
                TestAssert.Equal(PrologueGameFlowState.Playing, fixture.Flow.State, "outside recovery restores Playing");
            }
            finally { level.RecoveryCompleted -= onRecovery; }
            GD.Print("BASIN_SAMPLE_OUTSIDE_FALL_OK");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task TempleCheckpointContinues(SceneTree tree)
    {
        var fixture = await Fixture.Create(tree);
        try
        {
            await fixture.EnterTemple();
            var saved = PrologueSaveStore.LoadOrNew(fixture.SavePath);
            TestAssert.True(saved.CheckpointSector == nameof(PrologueSectorId.LostTemple) &&
                saved.CheckpointWorldPosition.DistanceTo(TempleCheckpoint) < .01f,
                "actual checkpoint BodyEntered persists the authored Temple checkpoint");
            await fixture.Reload();
            TestAssert.True(fixture.Flow.HasSave &&
                !fixture.Entry.Level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").Disabled,
                "restarted sample must offer Continue for the real reachable Temple checkpoint");
            fixture.Entry.Level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 8);
            TestAssert.True(fixture.Entry.Level.CurrentSector == PrologueSectorId.LostTemple &&
                fixture.Entry.Level.Player.GlobalPosition.DistanceTo(TempleCheckpoint) < 2f,
                "actual Continue returns to the saved Temple checkpoint, not Basin");
            GD.Print("BASIN_SAMPLE_TEMPLE_CONTINUE_OK");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task EnvelopeEdgesRespectGameplayState(SceneTree tree)
    {
        var fixture = await Fixture.Create(tree);
        try
        {
            await fixture.EnterTemple();
            var level = fixture.Entry.Level;
            int recovered = 0;
            PrologueLevelController.RecoveryCompletedEventHandler handler = _ => recovered++;
            level.RecoveryCompleted += handler;
            try
            {
                foreach (Vector2 allowed in new[] { new Vector2(6174, 1294), new Vector2(-94, 100),
                    new Vector2(5000, -1000), new Vector2(2882, 2014) })
                {
                    level.Player.GlobalPosition = allowed;
                    level.Player.Velocity = Vector2.Zero;
                    await Frames(tree, 2);
                    TestAssert.Equal(0, recovered,
                        $"side tolerance, unclamped top and a real shared seam remain playable at {allowed}");
                }
                foreach (Vector2 outside in new[] { new Vector2(6178, 1294), new Vector2(-98, 100),
                    new Vector2(5040, 1538) })
                {
                    int before = recovered;
                    level.Player.GlobalPosition = outside;
                    level.Player.Velocity = Vector2.Zero;
                    await Frames(tree, 8);
                    TestAssert.Equal(before + 1, recovered, $"outside side/bottom envelope recovers once at {outside}");
                    TestAssert.True(level.Player.GlobalPosition.DistanceTo(TempleCheckpoint) < 2f,
                        "edge fallback preserves the activated Temple checkpoint across sectors");
                }
                int prior = recovered;
                fixture.Flow.ReturnToTitle();
                level.Player.GlobalPosition = new Vector2(7000, 3000);
                await Frames(tree, 5);
                TestAssert.True(recovered == prior && fixture.Flow.State == PrologueGameFlowState.Title,
                    "title never starts a recovery from a disabled player's stale out-of-world position");
                fixture.Flow.StartNew();
                await Frames(tree, 5);
                TestAssert.True(recovered == prior && level.CurrentSector == PrologueSectorId.CanyonBasin,
                    "new session clears the stale external position before resuming gameplay");
            }
            finally { level.RecoveryCompleted -= handler; }
            GD.Print("BASIN_SAMPLE_ENVELOPE_EDGES_OK inside=4 outside=3 title=True");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task DefeatedBossSurvivesRecoveryAndRemount(SceneTree tree)
    {
        var fixture = await Fixture.Create(tree);
        try
        {
            await fixture.EnterTemple();
            var level = fixture.Entry.Level;
            var arena = level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
            var freshBoss = arena.CurrentBoss;
            level.RequestRespawn("UNDEFEATED RETRY FIXTURE");
            await Frames(tree, 10);
            TestAssert.True(GodotObject.IsInstanceValid(arena.CurrentBoss) && arena.CurrentBoss != freshBoss &&
                arena.CurrentBoss.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                "ordinary unbeaten checkpoint retry still creates a fresh live boss");
            arena.BeginEncounter();
            await Frames(tree, 2);
            TestAssert.True(!level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Entry/Collision").Disabled &&
                !level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision").Disabled,
                "a live undefeated encounter still locks both doors during combat");
            var health = arena.CurrentBoss.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0, 940002UL)),
                "controlled real boss death prepares persistence coverage");
            await Frames(tree, 6);
            TestAssert.True(PrologueSaveStore.LoadOrNew(fixture.SavePath).BossDefeated,
                "saving actual sample victory must not coerce BossDefeated back to false");
            TestAssert.Equal(1, fixture.DefeatEvents, "actual death emits one defeat before recovery");
            await WalkEntryBothWays(tree, fixture, "immediate real victory");
            var playerHealth = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(playerHealth.ApplyDamage(new DamageRequest(playerHealth.CurrentHealth, Vector2.Down, 0, 940003UL)),
                "actual player Health death starts post-boss recovery");
            await Frames(tree, 12);
            AssertNoBoss(level, "death recovery");
            await WalkEntryBothWays(tree, fixture, "death recovery");
            TestAssert.Equal(1, fixture.DefeatEvents, "death recovery does not emit another defeat");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            level.Player.GlobalPosition = new Vector2(2380, 2014);
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 8);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.LostTemple, out _),
                "controlled move to Basin truly unloads Temple rather than reusing its arena");
            level.Player.GlobalPosition = TempleCheckpoint;
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 10);
            AssertNoBoss(level, "new streamed Temple instance");
            await WalkEntryBothWays(tree, fixture, "new streamed Temple instance");
            TestAssert.Equal(1, fixture.DefeatEvents, "streamed arena restoration does not replay defeat");
            await fixture.Reload();
            TestAssert.True(fixture.Flow.HasSave, "persisted defeated Temple session still offers Continue");
            fixture.Flow.Continue();
            await Frames(tree, 10);
            AssertNoBoss(fixture.Entry.Level, "fresh entry Continue");
            await WalkEntryBothWays(tree, fixture, "fresh entry Continue");
            TestAssert.Equal(1, fixture.DefeatEvents, "fresh entry Continue does not replay defeat");
            GD.Print("BASIN_SAMPLE_BOSS_PERSISTENCE_OK death=True remount=True continue=True unbeaten_retry=True");
        }
        finally { await fixture.Dispose(); }
    }

    private static void AssertNoBoss(PrologueLevelController level, string stage)
    {
        var arena = level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");
        TestAssert.True(level.Progress.BossDefeated && arena.CurrentBoss == null && !arena.IsEncounterActive,
            stage + ": real defeated state cannot resurrect a boss");
        TestAssert.True(level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision").Disabled &&
            level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Entry/Collision").Disabled,
            stage + ": persisted victory opens both gates for checkpoint return and onward exploration");
    }

    private static async Task WalkEntryBothWays(SceneTree tree, Fixture fixture, string stage)
    {
        var player = fixture.Entry.Level.Player;
        TestAssert.True(player.GlobalPosition.X < 5060 && player.IsOnFloor(), stage + ": starts left of entry at checkpoint");
        Send(InputActions.MoveRight, true);
        for (int i = 0; i < 130 && player.GlobalPosition.X < 5380; i++) await Frames(tree, 1);
        Send(InputActions.MoveRight, false);
        TestAssert.True(player.GlobalPosition.X >= 5380 && fixture.Flow.State == PrologueGameFlowState.Playing,
            stage + ": actual D crosses the old entry gate and activation trigger");
        Send(InputActions.MoveLeft, true);
        for (int i = 0; i < 130 && player.GlobalPosition.X > 5045; i++) await Frames(tree, 1);
        Send(InputActions.MoveLeft, false);
        await Frames(tree, 8);
        TestAssert.True(player.GlobalPosition.X < 5060 && player.IsOnFloor(),
            stage + ": actual A returns through the gate without re-locking the defeated encounter");
        GD.Print($"SAMPLE_DEFEATED_GATE_ROUNDTRIP stage={stage} pos={player.GlobalPosition}");
    }

    private sealed class Fixture
    {
        public BasinQualitySampleEntry Entry;
        public PrologueGameFlowController Flow => Entry.Level.GetNode<PrologueGameFlowController>("GameFlow");
        public string SavePath => Path.Combine(_directory, "save.json");
        public int DefeatEvents { get; private set; }
        private SceneTree _tree;
        private string _directory;
        private readonly List<BossArenaController> _observedArenas = new();

        public static async Task<Fixture> Create(SceneTree tree)
        {
            var fixture = new Fixture { _tree = tree, _directory = Path.Combine(Path.GetTempPath(),
                "knight-sample-boundary-" + Guid.NewGuid().ToString("N")) };
            Directory.CreateDirectory(fixture._directory);
            tree.NodeAdded += fixture.ObserveArena;
            await fixture.Reload();
            fixture.Entry.Level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 5);
            TestAssert.Equal(PrologueGameFlowState.Playing, fixture.Flow.State, "fixture starts through real sample title");
            return fixture;
        }

        public async Task EnterTemple()
        {
            // This scoped mechanism test skips traversal with the existing scene
            // restoration API; the checkpoint itself activates by real body overlap.
            var world = Entry.Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple, TempleCheckpoint),
                "controlled fixture mounts the actual Temple scene");
            Entry.Level.Player.Velocity = Vector2.Zero;
            await Frames(_tree, 8);
            TestAssert.True(Entry.Level.Progress.RestoreCheckpoint().SectorId == PrologueSectorId.LostTemple,
                "real Temple checkpoint overlap updates progress authority");
        }

        public async Task Reload()
        {
            await FreeEntry();
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            Entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            Entry.StorageDirectory = _directory;
            _tree.Root.AddChild(Entry);
            await Frames(_tree, 3);
        }

        private async Task FreeEntry()
        {
            ReleaseInputs();
            _tree.Paused = false;
            if (!GodotObject.IsInstanceValid(Entry)) return;
            await TestSceneTeardown.DrainAudioOwner(_tree, Entry.Level);
            Entry.QueueFree();
            await Frames(_tree, 3);
            Entry = null;
        }

        private void ObserveArena(Node node)
        {
            if (node is not BossArenaController arena) return;
            _observedArenas.Add(arena);
            arena.EncounterDefeated += OnDefeat;
        }

        private void OnDefeat() => DefeatEvents++;

        public async Task Dispose()
        {
            _tree.NodeAdded -= ObserveArena;
            foreach (var arena in _observedArenas)
                if (GodotObject.IsInstanceValid(arena)) arena.EncounterDefeated -= OnDefeat;
            await FreeEntry();
            string absolute = Path.GetFullPath(_directory);
            string parent = Path.GetDirectoryName(absolute);
            if (parent == Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) &&
                Path.GetFileName(absolute).StartsWith("knight-sample-boundary-", StringComparison.Ordinal))
                Directory.Delete(absolute, recursive: true);
        }
    }

    private static void Send(StringName action, bool down)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = down, Strength = down ? 1f : 0f });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseInputs()
    {
        foreach (var action in new[] { InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp,
            InputActions.MoveDown, InputActions.PrimaryAction, InputActions.Shield, InputActions.Jump,
            InputActions.Dash, InputActions.SelectAttackMode, InputActions.SelectGrappleMode }) Send(action, false);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
