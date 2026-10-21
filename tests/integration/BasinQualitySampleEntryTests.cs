using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class BasinQualitySampleEntryTests
{
    private static readonly Vector2 AuthoredBasinStart = new(2380f, 2014f);

    public static async Task LethalAreaContactDefersRecoveryUntilAfterContact(
        SceneTree tree, bool presentation = false)
    {
        string directory = TempRoot("lethal-contact");
        BasinQualitySampleEntry entry = null;
        try
        {
            entry = await InstantiateSample(tree, directory);
            var level = entry.Level;
            if (presentation) level.Player.GetNode<Camera2D>("Camera2D").Enabled = true;
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 3);
            var health = level.Player.GetNode<HealthComponent>("Health");
            var hurtbox = level.Player.GetNode<Hurtbox>("Hurtbox");
            int requested = 0;
            int completed = 0;
            level.RecoveryRequested += _ => requested++;
            level.RecoveryCompleted += _ => completed++;

            // A real Area2D contact, not direct ApplyDamage from a safe process frame.
            // Synchronous scene restoration inside Hitbox.AreaEntered is the regression.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                // Cross the right seam so recovery must mount the missing left neighbour.
                // Same-sector direct-damage tests do not exercise collision activation.
                level.Player.GlobalPosition = new Vector2(2894, 2014);
                level.Player.Velocity = Vector2.Zero;
                await Frames(tree, 4);
                TestAssert.Equal(PrologueSectorId.CanyonAscent, level.CurrentSector.Value,
                    "contact occurs across the streamed seam, away from the camp checkpoint");
                if (presentation) await Frames(tree, 75);
                int before = completed;
                bool contacted = false;
                bool deferredAtContact = false;
                var hit = new Hitbox
                {
                    Name = "LethalContactFixture" + attempt,
                    Damage = health.MaxHealth, Knockback = 0,
                    CollisionLayer = 0, CollisionMask = hurtbox.CollisionLayer,
                    Position = level.Player.GlobalPosition
                };
                hit.AddChild(new CollisionShape2D
                {
                    Shape = new RectangleShape2D { Size = new Vector2(64, 64) }
                });
                entry.AddChild(hit);
                hit.Connected += target =>
                {
                    if (target != hurtbox) return;
                    contacted = true;
                    deferredAtContact = completed == before && health.CurrentHealth == 0;
                    level.RequestRespawn("DUPLICATE CONTACT");
                };
                hit.Begin((ulong)(892000 + attempt));
                for (int frame = 0; frame < 20 && !contacted; frame++) await Frames(tree, 1);
                TestAssert.True(contacted, "the fixture must kill through actual Hitbox.AreaEntered");
                await Frames(tree, 6);
                hit.QueueFree();
                await Frames(tree, 2);
                TestAssert.True(deferredAtContact,
                    "lethal area contact must finish before health and scene collision are restored");
                TestAssert.Equal(attempt + 1, requested, "duplicate contact queues only one recovery");
                TestAssert.Equal(attempt + 1, completed, "one contact completes exactly one recovery");
                TestAssert.True(health.CurrentHealth == health.MaxHealth &&
                    level.Player.GlobalPosition.DistanceTo(AuthoredBasinStart) < 1.1f &&
                    level.Player.IsOnFloor(), "recovery restores health and a solid camp floor");
                TestAssert.True(level.GetNode<PrologueGameFlowController>("GameFlow").State ==
                    PrologueGameFlowState.Playing, "recovery returns the session to Playing");
                AssertIsolatedEncounter(level, "lethal area contact " + attempt);
                GD.Print($"BASIN_DEATH_CONTACT_RECOVERED attempt={attempt + 1} " +
                    $"position={level.Player.GlobalPosition} health={health.CurrentHealth} " +
                    $"floor={level.Player.IsOnFloor()} requested={requested} completed={completed}");
                if (presentation) await Frames(tree, 75);
            }
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry)) await DisposeSample(tree, entry);
            Delete(directory);
        }
    }

    public static async Task ContextualGuideFollowsSessionLifecycle(SceneTree tree)
    {
        string directory = TempRoot("guide");
        BasinQualitySampleEntry entry = null;
        try
        {
            entry = await InstantiateSample(tree, directory);
            var level = entry.Level;
            var view = level.GetNodeOrNull<CanvasLayer>("SampleGuide/View");
            TestAssert.True(view != null,
                "the actual sample entry owns contextual guidance for its playable loop");
            TestAssert.True(!view.Visible, "title must not show gameplay instructions");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 3);
            TestAssert.True(view.Visible && view.GetNode<Label>("Panel/Keys").Text.Contains("Q"),
                "actual Start explains current attack controls before entering the encounter");
            var guide = level.GetNode<BasinSampleGuideController>("SampleGuide");
            // Controlled state fixtures validate teaching order; the separate loop probe
            // verifies the entire route using normal inputs, without these placements.
            level.Player.GlobalPosition = new Vector2(2640, 1856);
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 5);
            TestAssert.Equal(0, guide.Step, "visiting supply before the fight cannot complete the loop");
            level.Player.GlobalPosition = AuthoredBasinStart;
            level.Player.Velocity = Vector2.Zero;
            var enemyHealth = level.CurrentSectorRoot.GetNode<HealthComponent>(
                "Gameplay/Actors/BellBackLaborer/Health");
            enemyHealth.ApplyDamage(new DamageRequest(enemyHealth.CurrentHealth,
                Vector2.Right, 0, 891001UL));
            await Frames(tree, 5);
            TestAssert.Equal(1, guide.Step, "real enemy death reveals terrain-climb guidance");
            level.Player.GlobalPosition = new Vector2(2480, 1856);
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 5);
            TestAssert.Equal(2, guide.Step, "grounded shelf arrival points toward the supply");
            level.Player.GlobalPosition = new Vector2(2640, 1856);
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 5);
            TestAssert.Equal(3, guide.Step, "actual grounded supply overlap advances to return");
            TestAssert.True(!level.Progress.Discoveries.Contains("salvage_cache"),
                "guide cannot consume a full-health supply to mark exploration");
            level.Player.GlobalPosition = new Vector2(2380, 1930);
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 1);
            TestAssert.Equal(3, guide.Step, "flying above camp is not a grounded return");
            level.Player.GlobalPosition = AuthoredBasinStart;
            level.Player.Velocity = Vector2.Zero;
            await Frames(tree, 5);
            TestAssert.Equal(4, guide.Step, "actual grounded camp overlap completes this teaching loop");
            TestAssert.True(level.GetNode<PrologueCompletionController>("Completion").CompletionCount == 0 &&
                !PrologueSaveStore.LoadOrNew(Path.Combine(directory, "save.json")).Completed,
                "sample guide completion never completes or persists the full prologue");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.ApplySettings(new GameSettingsData { Language = GameLanguage.English });
            await Frames(tree, 2);
            TestAssert.True(view.GetNode<Label>("Panel/Title").Text.Contains("Exploration loop") &&
                view.GetNode<Label>("Panel/Body").Text.Contains("not the full prologue"),
                "completed sample guidance follows current language without a false full-game claim");
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!view.Visible, "pause hides sample guidance");
            pause.Execute(ProloguePauseCommand.Resume);
            await Frames(tree, 2);
            TestAssert.True(view.Visible, "resume restores the current guidance");
            TestAssert.Equal(4, guide.Step, "ordinary pause does not erase the observed loop");
            var health = level.Player.GetNode<HealthComponent>("Health");
            health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Down, 0, 891002UL));
            await Frames(tree, 7);
            TestAssert.True(view.Visible && guide.Step == 0,
                "death clears the completed guide and waits for deferred encounter reset");
            ReturnToTitleThroughPause(level);
            await Frames(tree, 2);
            TestAssert.True(!view.Visible, "return to title hides guidance despite transient Resume");
            level.GetNode<PrologueGameFlowController>("GameFlow").Continue();
            // ProcessFrame signals fire before node _Process: wait for the displayed
            // result after the deliberate reset boundary, not exactly at that boundary.
            ulong guideDeadline = Engine.GetPhysicsFrames() + 12;
            while (!view.Visible && Engine.GetPhysicsFrames() < guideDeadline)
                await Frames(tree, 1);
            TestAssert.True(view.Visible && view.GetNode<Label>("Panel/Keys").Text.Contains("Q"),
                "direct Continue begins a fresh sample encounter guide");
            flow.ReturnToTitle();
            flow.StartNew();
            await Frames(tree, 5);
            TestAssert.Equal(0, guide.Step, "direct New Game resets transient teaching independently of save");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry)) await DisposeSample(tree, entry);
            Delete(directory);
        }
    }

    public static async Task AuthoredBasinStartPersistsCheckpointBeforePhysics(SceneTree tree)
    {
        string directory = Path.Combine(Path.GetTempPath(),
            "knight-basin-quality-sample-red-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        PrologueLevelController level = null;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            level.Name = "AuthoredBasinStartFixture";
            level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position = AuthoredBasinStart;
            level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
            level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector =
                PrologueSectorId.CanyonBasin;
            PrologueGameFlowController flow =
                level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            flow.SavePath = Path.Combine(directory, "save.json");
            flow.SettingsPath = Path.Combine(directory, "settings.json");
            GameSettingsStore.SaveAtomic(flow.SettingsPath,
                new GameSettingsData { GuidanceEnabled = false });
            level.GetNode<PrologueGuidanceController>("Guidance").SetGuidanceEnabled(false);

            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);

            PrologueCheckpointSnapshot checkpoint = level.Progress.RestoreCheckpoint();
            TestAssert.True(checkpoint.HasActivatedCheckpoint &&
                checkpoint.SectorId == PrologueSectorId.CanyonBasin &&
                checkpoint.WorldPosition.DistanceTo(AuthoredBasinStart) < .01f,
                "actual Start must synchronously persist the authored Basin checkpoint before physics");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree())
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else
                    level.Free();
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task RunProductionLifecycle(SceneTree tree)
    {
        string directory = TempRoot("lifecycle");
        BasinQualitySampleEntry entry = null;
        try
        {
            entry = await InstantiateSample(tree, directory);
            PrologueLevelController level = entry.Level;
            PrologueGameFlowController flow =
                level.GetNode<PrologueGameFlowController>("GameFlow");
            PrologueUiRoot ui = level.GetNode<PrologueUiRoot>("ProductUi");
            TestAssert.True(flow.State == PrologueGameFlowState.Title && ui.Visible,
                "production sample opens on its title instead of auto-starting");
            TestAssert.Equal("盆地品质样片 · 开发中",
                level.GetNode<Label>("ProductUi/MainMenu/Panel/Subtitle").Text,
                "sample title truthfully labels the unfinished Chinese build");
            TestAssert.True(level.GetNode<Node2D>("WorldRoot").ProcessMode ==
                Node.ProcessModeEnum.Disabled,
                "sample title freezes world threats before gameplay begins");
            TestAssert.True(!level.GetNode<Button>("PauseMenu/Panel/GuidanceToggle").Visible &&
                !level.GetNode<PrologueGuidanceController>("Guidance").GuidanceEnabled,
                "sample disables full-route guidance and its re-enable control");

            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            PrologueCheckpointSnapshot started = level.Progress.RestoreCheckpoint();
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                started.HasActivatedCheckpoint && started.SectorId == PrologueSectorId.CanyonBasin &&
                started.WorldPosition.DistanceTo(AuthoredBasinStart) < .01f,
                "production Start immediately owns the authored Basin checkpoint");
            TestAssert.True(level.GetNode<Node2D>("WorldRoot").ProcessMode ==
                Node.ProcessModeEnum.Inherit,
                "production Start resumes the sample world");
            await Frames(tree, 2);

            AssertIsolatedEncounter(level, "Start");

            TestAssert.True(level.Progress.Discover("sample_supply"),
                "real sample progress records a discovery");
            flow.PersistProgress("sample-test-discovery");
            HealthComponent health = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(
                    health.CurrentHealth, Vector2.Down, 0f, 810001UL)),
                "real player health initiates sample death recovery");
            await Frames(tree, 3);
            AssertIsolatedEncounter(level, "death recovery");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                level.CurrentSector == PrologueSectorId.CanyonBasin &&
                level.Player.GlobalPosition.DistanceTo(AuthoredBasinStart) < 1.1f &&
                health.CurrentHealth == health.MaxHealth,
                "death recovery returns the real player to the authored sample checkpoint");
            TestAssert.True(level.GetNode<Label>("Hud/FormalHud/ObjectiveRibbon/Text").Text
                    .Contains("开发", StringComparison.Ordinal),
                "death recovery restores the truthful static sample objective");

            ReturnToTitleThroughPause(level);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !tree.Paused &&
                level.GetNode<Node2D>("WorldRoot").ProcessMode == Node.ProcessModeEnum.Disabled,
                "actual pause-menu Return to Title restores title state and freezes threats");
            await DisposeSample(tree, entry);
            entry = null;

            entry = await InstantiateSample(tree, directory);
            level = entry.Level;
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave &&
                !level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").Disabled,
                "restart offers Continue for the valid persisted Basin checkpoint");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 2);
            AssertIsolatedEncounter(level, "reload and Continue");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                level.CurrentSector == PrologueSectorId.CanyonBasin &&
                level.Player.GlobalPosition.DistanceTo(AuthoredBasinStart) < 1.1f &&
                level.Progress.Discoveries.Contains("sample_supply"),
                "actual Continue restores checkpoint position and persisted discoveries");
            TestAssert.True(level.GetNode<PrologueCompletionController>("Completion").
                    CompletionCount == 0,
                "sample Continue does not fabricate full-prologue completion");

            flow.Complete();
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                !PrologueSaveStore.LoadOrNew(Path.Combine(directory, "save.json")).Completed,
                "sample mode rejects full-prologue completion persistence");
            ReturnToTitleThroughPause(level);
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            TestAssert.True(level.Progress.Discoveries.Count == 0 &&
                !PrologueSaveStore.LoadOrNew(Path.Combine(directory, "save.json"))
                    .Discoveries.Contains("sample_supply"),
                "actual New Game clears persisted sample discoveries");
            AssertIsolatedEncounter(level, "New Game");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry)) await DisposeSample(tree, entry);
            Delete(directory);
        }
    }

    public static async Task CampWaitsForPlayerApproach(SceneTree tree)
    {
        string directory = TempRoot("camp");
        BasinQualitySampleEntry entry = null;
        try
        {
            entry = await InstantiateSample(tree, directory);
            PrologueLevelController level = entry.Level;
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            var enemy = level.CurrentSectorRoot.GetNode<BellBackLaborer>(
                "Gameplay/Actors/BellBackLaborer");
            Vector2 rest = enemy.GlobalPosition;
            await Frames(tree, 300);
            TestAssert.True(enemy.CurrentState == BellBackLaborerState.Idle &&
                Mathf.Abs(enemy.GlobalPosition.X - rest.X) < .1f,
                "waiting at the sample camp must not aggro the encounter while reading controls");
            var health = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.Equal(health.MaxHealth, health.CurrentHealth,
                "the authored sample start supports an unhurried first input");
            Input.ParseInputEvent(new InputEventAction
                { Action = InputActions.MoveRight, Pressed = true, Strength = 1 });
            Input.FlushBufferedEvents();
            for (int frame = 0; frame < 90 && enemy.CurrentState == BellBackLaborerState.Idle; frame++)
                await Frames(tree, 1);
            TestAssert.True(enemy.CurrentState != BellBackLaborerState.Idle,
                "walking toward the Laborer activates its real encounter AI");
        }
        finally
        {
            Input.ParseInputEvent(new InputEventAction
                { Action = InputActions.MoveRight, Pressed = false });
            Input.FlushBufferedEvents();
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry)) await DisposeSample(tree, entry);
            Delete(directory);
        }
    }

    public static async Task RunSaveIsolationAndLocalization(SceneTree tree)
    {
        string directory = TempRoot("isolation");
        string savePath = Path.Combine(directory, "save.json");
        string normalSentinel = Path.Combine(directory, "normal-prologue-sentinel.json");
        BasinQualitySampleEntry entry = null;
        try
        {
            File.WriteAllText(normalSentinel, "NORMAL-SAVE-UNCHANGED");
            PrologueSaveStore.SaveAtomic(savePath, SampleSave(
                PrologueSectorId.LostTemple, new Vector2(4800f, 900f)));
            string templeSave = File.ReadAllText(savePath);
            entry = await InstantiateSample(tree, directory);
            PrologueGameFlowController flow =
                entry.Level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(!flow.HasSave &&
                entry.Level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").Disabled,
                "sample rejects a Temple position that is not an authored checkpoint");
            TestAssert.Equal(templeSave, File.ReadAllText(savePath),
                "opening the title does not overwrite a rejected Temple save");
            await DisposeSample(tree, entry);
            entry = null;

            PrologueSaveStore.SaveAtomic(savePath, SampleSave(
                PrologueSectorId.CanyonBasin, new Vector2(4000f, 2014f)));
            string outOfBoundsSave = File.ReadAllText(savePath);
            entry = await InstantiateSample(tree, directory);
            flow = entry.Level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(!flow.HasSave,
                "sample rejects a Basin checkpoint outside the authored scene-definition bounds");
            TestAssert.Equal(outOfBoundsSave, File.ReadAllText(savePath),
                "opening the title does not overwrite an out-of-bounds sample save");
            TestAssert.True(flow.SavePath == $"{directory}/save.json" &&
                flow.SettingsPath == $"{directory}/settings.json" &&
                File.ReadAllText(normalSentinel) == "NORMAL-SAVE-UNCHANGED",
                "sample storage stays distinct and leaves the normal save sentinel untouched");
            await DisposeSample(tree, entry);
            entry = null;

            File.Delete(savePath);
            entry = await InstantiateSample(tree, directory);
            PrologueLevelController level = entry.Level;
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Controls")
                .EmitSignal(Button.SignalName.Pressed);
            TestAssert.True(level.GetNode<Control>("ProductUi/ControlsView").Visible,
                "sample title opens the actual controls view");
            level.GetNode<Button>("ProductUi/ControlsView/Panel/Back")
                .EmitSignal(Button.SignalName.Pressed);
            TestAssert.Equal("盆地品质样片 · 开发中",
                level.GetNode<Label>("ProductUi/MainMenu/Panel/Subtitle").Text,
                "controls Back retains the Chinese sample label");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Settings")
                .EmitSignal(Button.SignalName.Pressed);
            level.GetNode<OptionButton>("ProductUi/SettingsMenu/Panel/Rows/Language/Value").Selected =
                (int)GameLanguage.English;
            level.GetNode<Button>("ProductUi/SettingsMenu/Panel/Back")
                .EmitSignal(Button.SignalName.Pressed);
            TestAssert.Equal("BASIN QUALITY SAMPLE · IN DEVELOPMENT",
                level.GetNode<Label>("ProductUi/MainMenu/Panel/Subtitle").Text,
                "settings Back retains the English sample label");
            TestAssert.Equal("RETURN TO TITLE",
                level.GetNode<Button>("PauseMenu/Panel/ReturnToTitle").Text,
                "sample pause action follows the saved language");
            TestAssert.True(!level.GetNode<PrologueGuidanceController>("Guidance").GuidanceEnabled,
                "settings cannot re-enable full-route sample guidance");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry)) await DisposeSample(tree, entry);
            Delete(directory);
        }
    }

    public static async Task DefaultMainStillStartsAtEntrance(SceneTree tree)
    {
        string directory = TempRoot("default-main");
        PrologueLevelController level = null;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            flow.SavePath = Path.Combine(directory, "save.json");
            flow.SettingsPath = Path.Combine(directory, "settings.json");
            tree.Root.AddChild(level);
            await Frames(tree, 2);
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            PrologueCheckpointSnapshot checkpoint = level.Progress.RestoreCheckpoint();
            TestAssert.True(!flow.BasinQualitySampleMode &&
                level.CurrentSector == PrologueSectorId.CanyonEntrance &&
                checkpoint.SectorId == PrologueSectorId.CanyonEntrance &&
                !checkpoint.HasActivatedCheckpoint,
                "default production main remains an unactivated Entrance start");
            TestAssert.True(level.GetNodeOrNull("SampleGuide") == null,
                "sample teaching cannot replace the normal prologue guidance");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
            Delete(directory);
        }
    }

    private static void AssertIsolatedEncounter(PrologueLevelController level, string stage)
    {
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var basin),
            stage + ": sample Basin is really mounted");
        Node actors = basin.GetNode("Gameplay/Actors");
        TestAssert.Equal(0, actors.GetChildren().OfType<RustShellSquire>().Count(),
            stage + ": no second enemy can attack the sample camp from the left");
        TestAssert.Equal(1, actors.GetChildren().OfType<BellBackLaborer>().Count(),
            stage + ": the authored Laborer encounter survives resets without duplication");

        // Load the real normal entry again after the sample has changed its definitions.
        // Mutating a shared Resource or Array would silently remove the full game's enemy too.
        var normal = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            var baseline = normal.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors")
                .Definitions.Single(d => d.Id == PrologueSectorId.CanyonBasin);
            var selected = world.Definitions.Single(d => d.Id == PrologueSectorId.CanyonBasin);
            TestAssert.True(selected.WorldOffset == baseline.WorldOffset &&
                selected.Bounds == baseline.Bounds && selected.Previous == baseline.Previous &&
                selected.Next == baseline.Next &&
                selected.KeyInteractionBounds.SequenceEqual(baseline.KeyInteractionBounds),
                stage + ": sample scene substitution preserves world geometry and stream contracts");
            var original = baseline.Scene.Instantiate<PrologueSector>();
            try
            {
                Node originalActors = original.GetNode("Gameplay/Actors");
                TestAssert.Equal(1, originalActors.GetChildren().OfType<RustShellSquire>().Count(),
                    stage + ": original full-game Basin retains its Rust enemy");
                TestAssert.Equal(1, originalActors.GetChildren().OfType<BellBackLaborer>().Count(),
                    stage + ": original full-game Basin retains its Laborer");
                var originalEnemy = originalActors.GetChildren().OfType<BellBackLaborer>().Single();
                var sampleEnemy = actors.GetChildren().OfType<BellBackLaborer>().Single();
                TestAssert.Equal(originalEnemy.Tuning.ToSettings(), sampleEnemy.Tuning.ToSettings()
                    with { DetectionRange = originalEnemy.Tuning.DetectionRange },
                    stage + ": camp activation changes no attack, damage or counter timing");
                TestAssert.True(originalEnemy.Tuning.DetectionRange > 270f,
                    stage + ": sample camp activation cannot shrink the full game's detection range");
            }
            finally { original.Free(); }
        }
        finally { normal.Free(); }
    }

    private static PrologueSaveData SampleSave(PrologueSectorId sector, Vector2 position) => new()
    {
        HasActivatedCheckpoint = true,
        CheckpointSector = sector.ToString(),
        CheckpointWorldX = position.X,
        CheckpointWorldY = position.Y,
        Objective = nameof(PrologueObjective.Wake)
    };

    private static async Task<BasinQualitySampleEntry> InstantiateSample(
        SceneTree tree, string storageDirectory)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        BasinQualitySampleEntry entry = GD.Load<PackedScene>(
                "res://scenes/prologue/sample/BasinQualitySample.tscn")
            .Instantiate<BasinQualitySampleEntry>();
        entry.StorageDirectory = storageDirectory;
        tree.Root.AddChild(entry);
        entry.Level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        await Frames(tree, 2);
        return entry;
    }

    private static void ReturnToTitleThroughPause(PrologueLevelController level)
    {
        level.GetNode<ProloguePauseMenu>("PauseMenu").Open();
        level.GetNode<Button>("PauseMenu/Panel/ReturnToTitle")
            .EmitSignal(Button.SignalName.Pressed);
    }

    private static async Task DisposeSample(SceneTree tree, BasinQualitySampleEntry entry)
    {
        if (GodotObject.IsInstanceValid(entry.Level) && entry.Level.IsInsideTree())
            await TestSceneTeardown.DrainAudioOwner(tree, entry.Level);
        entry.QueueFree();
        await Frames(tree, 2);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static string TempRoot(string suffix)
    {
        string path = Path.Combine(Path.GetTempPath(),
            "knight-basin-quality-sample-" + suffix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}
