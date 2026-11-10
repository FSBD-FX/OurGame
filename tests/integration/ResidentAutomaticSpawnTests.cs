using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class ResidentAutomaticSpawnTests
{
    public static async Task RunStart(SceneTree tree)
    {
        await InactiveAttackGeometry(tree);
        await PlayerOverlapPreparation(tree, foreignBody: false);
        await PlayerOverlapPreparation(tree, foreignBody: true);
        await PlayerOverlapPreparation(tree, foreignBody: true, corridorOnly: true);
        await PlayerOverlapPreparation(tree, foreignBody: true, corridorOnly: true, foreignArea: true);
        var paths = IsolatedPrologueFixture.CreatePaths("resident-automatic-start");
        PrologueLevelController level = PrologueGameFlowTests.Instantiate("ResidentAutomaticStart", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var births = new HashSet<ulong>();
        var families = Enum.GetValues<ResidentFamily>().ToDictionary(x => x, _ => 0);
        bool hidden = true;
        void Added(Node node)
        {
            if (node is not CharacterBody2D body || body.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding") is not { } binding) return;
            TestAssert.True(births.Add(body.GetInstanceId()), "automatic birth records a unique real native root");
            families[binding.Profile.Family]++;
            hidden &= flow.State == PrologueGameFlowState.Intro && !world.IsVisibleInTree();
        }
        try
        {
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 4);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && level.Residents.TrackedActorCount == 0,
                "title does not silently count manual or automatic enemies");
            tree.NodeAdded += Added;
            flow.StartNew();
            TestAssert.True(flow.State == PrologueGameFlowState.Intro && level.ResidentPreparationPending &&
                !level.GetNode<Node2D>("PlayerRoot").Visible && !world.IsVisibleInTree(),
                "accepted new game must remain genuinely hidden Intro until resident preparation commits");
            var health = level.Player.GetNode<HealthComponent>("Health");
            var hurt = level.Player.GetNode<Hurtbox>("Hurtbox");
            var energy = level.Player.GetNode<PlayerEnergyController>("Energy");
            var shield = level.Player.GetNode<PlayerShieldController>("Shield");
            var hit = new DamageRequest(1, Vector2.Left, 0, 62001UL, 62002UL, DamageGuardClass.Heavy);
            int hp = health.CurrentHealth, shieldEvents = 0;
            float power = energy.EnergyRatio;
            shield.ShieldResolved += (_, _) => shieldEvents++;
            TestAssert.True(!hurt.Receive(hit) && health.CurrentHealth == hp && energy.EnergyRatio == power && shieldEvents == 0,
                "preparation rejects actual Hurtbox before HP, shield energy/events and attack identity consumption");
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            pause.Open(); pause.Execute(ProloguePauseCommand.Resume); pause.Execute(ProloguePauseCommand.ReturnToCheckpoint);
            level.RequestRespawn("PREPARATION MUST NOT REENTER RECOVERY");
            TestAssert.True(!tree.Paused && !pause.IsOpen && flow.State == PrologueGameFlowState.Intro && level.ResidentPreparationPending,
                "public pause/execute/recovery cannot bypass or deadlock Intro preparation");
            // Rejected repeated start must not cancel the accepted generation.
            using (var warnings = new ExpectedWarningLogger())
            {
                flow.StartNew();
                warnings.AssertExact(new ExpectedWarningLogger.Expected(
                    "Rejected prologue flow transition Intro->Intro.",
                    "scripts/prologue/PrologueGameFlowController.cs", "Reject", 1));
            }
            GD.Print("RESIDENT_AUTO_START_WARNING_SCOPE_OK count=1 source=scripts/prologue/PrologueGameFlowController.cs function=Reject");
            for (int i = 0; i < 180 && flow.State == PrologueGameFlowState.Intro; i++) await Frames(tree, 1);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && !level.ResidentPreparationPending,
                "accepted preparation reaches normal Playing without manual TrySpawn or fake pose/HP writes: " + level.ResidentPreparationFailure);
            TestAssert.True(hidden && births.Count == 24 && level.Residents.TrackedActorCount == 24 &&
                families[ResidentFamily.Native] == 8 && families[ResidentFamily.Scrap] == 2 &&
                families[ResidentFamily.Precision] == 2 && families[ResidentFamily.Bone] == 5 && families[ResidentFamily.Ghost] == 7,
                "literal initial population is 24 actual roots with 8/2/2/5/7 families, all born while hidden");
            foreach (var site in level.ResidentLayout)
            {
                bool initial = site.Definition.Sector is PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent;
                TestAssert.True(level.Residents.Snapshot(site.Definition.StableId).Attached == initial,
                    "initial mount owns exactly its authored stable identities " + site.Definition.StableId);
            }
            TestAssert.True(hurt.Receive(hit) && health.CurrentHealth == hp - 1,
                "same attack identity rejected in preparation can first hit after Playing");
            GD.Print("RESIDENT_AUTO_START_OK actual_roots=24 families=8,2,2,5,7 hidden_births=true real_flow=true damage_identity_preserved=true pause_guarded=true rejected_restart_preserved=true manual_spawns=0 normal_route=false final_art=false");
        }
        finally
        {
            tree.NodeAdded -= Added; tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    public static async Task RunSession(SceneTree tree)
    {
        await ReentrantCancellation(tree);
        var startupFailures = new List<Exception>();
        try { await Cancellation(tree); } catch (Exception error) { startupFailures.Add(error); }
        try { await ColdContinue(tree, cancelAtMount: true); } catch (Exception error) { startupFailures.Add(error); }
        if (startupFailures.Count != 0) throw new AggregateException("RESIDENT_STARTUP_ATOMICITY_FAILED", startupFailures);
        await ColdContinue(tree);
        await RecoveredReplacement(tree);
        await MappedContinueCommit(tree);
        await Recovery(tree);
        GD.Print("RESIDENT_AUTO_SESSION_OK cancel=true cold_continue=true lethal_recovery=true manual_spawns=0 normal_route=false final_art=false");
    }

    // Break caught: a synchronous native NodeAdded observer cancels the accepted
    // session, but its in-flight actor still activates/commits after ReturnToTitle.
    private static async Task ReentrantCancellation(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-reentrant-cancel");
        var level = PrologueGameFlowTests.Instantiate("ResidentAutoReentrantCancel", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        int roots = 0, sessions = 0; bool cancelled = false;
        flow.SessionStarted += () => sessions++;
        void Added(Node node)
        {
            if (node is not CharacterBody2D body || body.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding") == null) return;
            roots++;
            if (!cancelled) { cancelled = true; flow.ReturnToTitle(); }
        }
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4); tree.NodeAdded += Added;
            flow.StartNew();
            for (int frame = 0; frame < 180 && !cancelled; frame++) await Frames(tree, 1);
            TestAssert.True(cancelled, "actual first automatic native root triggers the synchronous title observer");
            await Frames(tree, 8);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && sessions == 0 && roots == 1 &&
                level.Residents.TrackedActorCount == 0 && !level.ResidentPreparationPending &&
                !level.GetNode<Node2D>("PlayerRoot").IsVisibleInTree() && !level.Player.IsPhysicsProcessing(),
                "reentrant title cancellation must revoke the in-flight native birth and stop the same reconciliation; " +
                $"roots={roots} committed={level.Residents.TrackedActorCount} sessions={sessions} state={flow.State}");
            flow.StartNew(); await Playing(tree, level, flow);
            TestAssert.True(sessions == 1 && level.Residents.TrackedActorCount == 24,
                "fresh generation still prepares all initial24 after reentrant cancellation");
            GD.Print("RESIDENT_AUTO_REENTRANT_CANCEL_OK actual_node_added=true cancelled_commits=0 cancelled_roots=1 late_sessions=0 fresh_roots=24 fresh_sessions=1 manual_spawns=0");
        }
        finally { tree.NodeAdded -= Added; await Close(tree, level); }
    }

    public static async Task RunTeardown(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-teardown");
        var level = PrologueGameFlowTests.Instantiate("ResidentAutoTeardown", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        int roots = 0, sessions = 0; Task drain = null;
        flow.SessionStarted += () => sessions++;
        void Added(Node node)
        {
            if (node is CharacterBody2D body && body.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding") != null && ++roots == 24)
                drain = TestSceneTeardown.DrainAudioOwner(tree, level);
        }
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4); tree.NodeAdded += Added;
            flow.StartNew();
            for (int i = 0; i < 180 && drain == null; i++) await Frames(tree, 1);
            TestAssert.True(drain != null, "real last automatic birth begins actual audio teardown while Intro is pending");
            await drain; await Frames(tree, 16);
            TestAssert.True(sessions == 0 && !level.ResidentPreparationPending &&
                !level.GetNode<Node2D>("PlayerRoot").Visible,
                "test audio teardown must cancel accepted preparation before releasing its audio dependency");
            GD.Print("RESIDENT_AUTO_TEARDOWN_OK actual_last_birth=true audio_released=true sessions_after_teardown=0 pending_cancelled=true raw_diagnostics_strict=true");
        }
        finally
        {
            tree.NodeAdded -= Added;
            if (GodotObject.IsInstanceValid(level)) { level.QueueFree(); await Frames(tree, 2); }
        }
    }

    private static async Task Cancellation(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-cancel");
        var saved = PrologueSaveData.NewLayoutV2();
        saved.CheckpointId = "canyon.v2.temple-forecourt";
        PrologueGameFlowTests.WriteFixtureSave(paths.SavePath, saved);
        byte[] previous = File.ReadAllBytes(paths.SavePath);
        var level = PrologueGameFlowTests.Instantiate("ResidentAutoCancel", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        int sessions = 0; flow.SessionStarted += () => sessions++;
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4);
            TestAssert.True(flow.HasSave && flow.Save.CheckpointId == saved.CheckpointId,
                "cancel fixture starts with an actual prior Continue slot");
            flow.StartNew(); flow.ReturnToTitle();
            await Frames(tree, 16);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && sessions == 0 &&
                !level.ResidentPreparationPending && level.Residents.TrackedActorCount == 0 &&
                !level.GetNode<Node2D>("PlayerRoot").IsVisibleInTree(),
                "cancelled preparation cannot late-spawn, publish SessionStarted or reveal gameplay");
            TestAssert.True(flow.HasSave && flow.Save.CheckpointId == saved.CheckpointId &&
                previous.SequenceEqual(File.ReadAllBytes(paths.SavePath)),
                "STARTUP_SAVE_CANCEL_RED: cancelled New Game must preserve the existing bytes and advertised Continue");
            flow.PersistProgress("cancelled-startup-control");
            TestAssert.True(previous.SequenceEqual(File.ReadAllBytes(paths.SavePath)),
                "an unpublished startup may not write progress while back at Title");
            flow.StartNew(); await Playing(tree, level, flow);
            TestAssert.True(sessions == 1 && level.Residents.Epoch == 3 && level.Residents.TrackedActorCount == 24,
                "fresh accepted generation starts exactly once after cancellation, with literal initial24");
            TestAssert.True(flow.Save.CheckpointId == "canyon.v2.start" &&
                !previous.SequenceEqual(File.ReadAllBytes(paths.SavePath)),
                "a successful retry commits the new save rather than blocking all writes");
            GD.Print("RESIDENT_AUTO_CANCEL_OK old_sessions=0 new_sessions=1 epoch=3 actual_roots=24");
        }
        finally { await Close(tree, level); }
    }

    private static async Task ColdContinue(SceneTree tree, bool cancelAtMount = false)
    {
        var paths = IsolatedPrologueFixture.CreatePaths(cancelAtMount ? "resident-auto-mount-cancel" : "resident-auto-cold-continue");
        var saved = PrologueSaveData.NewLayoutV2();
        saved.CheckpointId = "canyon.v2.temple-forecourt";
        PrologueGameFlowTests.WriteFixtureSave(paths.SavePath, saved);
        var level = PrologueGameFlowTests.Instantiate("ResidentAutoColdContinue", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        bool cancelled = false;
        int sessions = 0;
        flow.SessionStarted += () => sessions++;
        void CancelOnTempleMount(SeamlessPrologueWorld source, PrologueSector sector)
        {
            if (sector.SectorId != PrologueSectorId.LostTemple) return;
            cancelled = true;
            flow.ReturnToTitle();
        }
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4);
            TestAssert.True(flow.HasSave && flow.State == PrologueGameFlowState.Title, "isolated valid cold save advertises Continue");
            byte[] previous = File.ReadAllBytes(paths.SavePath);
            if (cancelAtMount) world.ActiveSectorCommitted += CancelOnTempleMount;
            flow.Continue();
            if (cancelAtMount)
            {
                TestAssert.True(cancelled, "the real destination mount must synchronously call ReturnToTitle");
                TestAssert.True(flow.State == PrologueGameFlowState.Title && sessions == 0 && !world.IsVisibleInTree() &&
                    !level.Player.IsPhysicsProcessing(), "STARTUP_MOUNT_CANCEL_RED: cancelled Continue cannot expose gameplay or publish Playing");
                await Frames(tree, 8);
                TestAssert.True(flow.State == PrologueGameFlowState.Title && sessions == 0 && !level.ResidentPreparationPending &&
                    flow.HasSave && previous.SequenceEqual(File.ReadAllBytes(paths.SavePath)),
                    "cancelled mount cannot late-start or change the current Continue slot");
                GD.Print("RESIDENT_STARTUP_MOUNT_CANCEL_OK synchronous=true sessions=0 save_unchanged=true");
                return;
            }
            TestAssert.True(flow.State == PrologueGameFlowState.Intro && level.ResidentPreparationPending && !world.IsVisibleInTree(),
                "actual valid Continue remains hidden until real destination residents prepare");
            await Playing(tree, level, flow);
            TestAssert.True(world.CurrentSector == PrologueSectorId.LostTemple && level.Residents.TrackedActorCount == 13 &&
                level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.temple-forecourt" &&
                sessions == 1 && previous.SequenceEqual(File.ReadAllBytes(paths.SavePath)),
                "actual cold Continue prepares 12 Ascent plus1 Temple, not old initial24 or all50 simultaneously");
            foreach (var site in level.ResidentLayout)
                TestAssert.True(level.Residents.Snapshot(site.Definition.StableId).Attached ==
                    (site.Definition.Sector is PrologueSectorId.CanyonAscent or PrologueSectorId.LostTemple),
                    "Continue owns only destination identities: " + site.Definition.StableId);
            GD.Print("RESIDENT_AUTO_CONTINUE_OK actual_roots=13 checkpoint=temple-forecourt hidden=true manual_spawns=0");
        }
        finally { world.ActiveSectorCommitted -= CancelOnTempleMount; await Close(tree, level); }
    }

    private static async Task MappedContinueCommit(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-mapped-commit");
        string legacy = Path.Combine(paths.CaseRoot, Path.GetFileName(PrologueGameFlowController.LegacySavePath));
        File.WriteAllText(legacy, """
            {"version":2,"hasActivatedCheckpoint":true,"checkpointSector":"LostTemple",
             "checkpointWorldX":5040,"checkpointWorldY":1294,"objective":"EnterTemple"}
            """);
        byte[] legacyBytes = File.ReadAllBytes(legacy);
        var level = PrologueGameFlowTests.Instantiate("ResidentMappedCommit", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var completion = level.GetNode<PrologueCompletionController>("Completion");
        flow.AutomationAutoStart = false;
        int sessions = 0;
        string diskAtPublication = null, saveAtPublication = null;
        PrologueObjective objectiveAtPublication = default;
        flow.SessionStarted += () =>
        {
            sessions++;
            objectiveAtPublication = completion.CurrentObjective;
            saveAtPublication = flow.Save.Objective;
            diskAtPublication = PrologueSaveStore.LoadOrNew(paths.SavePath, 2).Objective;
        };
        bool AdvertisesLegacy() => flow.State == PrologueGameFlowState.Title && flow.HasSave &&
            flow.LoadResult.Status == PrologueSaveLoadStatus.MigratableLegacy &&
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").Text == "从映射存档点继续";
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4);
            TestAssert.True(AdvertisesLegacy() && !File.Exists(paths.SavePath),
                "valid isolated legacy save starts with a real mapped Continue choice");
            flow.Continue();
            TestAssert.True(flow.State == PrologueGameFlowState.Intro && !File.Exists(paths.SavePath),
                "mapped Continue must not create the current slot while still preparing");
            flow.ReturnToTitle(); await Frames(tree, 8);
            TestAssert.True(AdvertisesLegacy() && sessions == 0 && !File.Exists(paths.SavePath) &&
                legacyBytes.SequenceEqual(File.ReadAllBytes(legacy)),
                "cancelled mapped Continue retains its menu choice and exact legacy bytes");

            // A directory at the exact isolated destination forces SaveAtomic to
            // fail after preparing a valid world and serializing its temporary file.
            Directory.CreateDirectory(paths.SavePath);
            flow.Continue();
            for (int frame = 0; frame < 180 && flow.State == PrologueGameFlowState.Intro; frame++)
                await Frames(tree, 1);
            TestAssert.True(AdvertisesLegacy() && sessions == 0 && !world.IsVisibleInTree() &&
                flow.LastRejectedTransition.StartsWith("startup-save:", StringComparison.Ordinal) &&
                !Directory.EnumerateFiles(paths.CaseRoot, "*.tmp").Any() &&
                legacyBytes.SequenceEqual(File.ReadAllBytes(legacy)),
                "actual startup write failure stays hidden, retains legacy, and does not publish a session");
            Directory.Move(paths.SavePath, Path.Combine(paths.CaseRoot, "retained-write-blocker"));
            flow.Continue(); await Playing(tree, level, flow);
            TestAssert.True(sessions == 1 && flow.HasSave &&
                PrologueSaveStore.LoadOrNew(paths.SavePath, 2).CheckpointId == "canyon.v2.temple-forecourt" &&
                legacyBytes.SequenceEqual(File.ReadAllBytes(legacy)),
                "successful mapped retry publishes exactly once after writing only the current slot");
            TestAssert.True(objectiveAtPublication == PrologueObjective.EnterTemple &&
                saveAtPublication == nameof(PrologueObjective.EnterTemple) && diskAtPublication == saveAtPublication,
                $"STARTUP_OBJECTIVE_EARLY: hidden preparation must not advance the objective; " +
                $"published={objectiveAtPublication}, live={saveAtPublication}, disk={diskAtPublication}");
            await Frames(tree, 4);
            TestAssert.True(completion.CurrentObjective == PrologueObjective.PullTempleMechanism,
                "normal Playing physics still advances the saved EnterTemple objective");
            GD.Print("RESIDENT_STARTUP_MAPPED_COMMIT_OK cancelled=true io_failed=true retry=true legacy_unchanged=true sessions=1 objective_after_publication=true");
        }
        finally { await Close(tree, level); }
    }

    private static async Task RecoveredReplacement(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-recovered-replacement");
        File.WriteAllText(paths.SavePath, "{\"version\":2,\"layoutVersion\":99,\"objective\":\"Wake\"}");
        byte[] original = File.ReadAllBytes(paths.SavePath);
        var level = PrologueGameFlowTests.Instantiate("ResidentRecoveredReplacement", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        int sessions = 0;
        flow.SessionStarted += () => sessions++;
        string[] Backups() => Directory.GetFiles(paths.CaseRoot, "save.json.preserved-*.json");
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4);
            TestAssert.True(flow.LoadResult.Status == PrologueSaveLoadStatus.Recovered && !flow.HasSave,
                "actual invalid current slot requires explicit replacement");
            flow.StartNew(); flow.ReturnToTitle(); await Frames(tree, 8);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !flow.HasSave && sessions == 0 &&
                original.SequenceEqual(File.ReadAllBytes(paths.SavePath)) && Backups().Length == 0,
                "cancelled recovered replacement leaves the original slot untouched and creates no backup");
            flow.StartNew(); await Playing(tree, level, flow);
            string[] backups = Backups();
            TestAssert.True(sessions == 1 && flow.HasSave && backups.Length == 1 &&
                original.SequenceEqual(File.ReadAllBytes(backups[0])) &&
                PrologueSaveStore.LoadOrNew(paths.SavePath, 2).CheckpointId == "canyon.v2.start",
                "explicit successful replacement retains exactly one byte-identical backup");
            GD.Print("RESIDENT_STARTUP_RECOVERED_REPLACEMENT_OK cancelled_unchanged=true committed=true backup_exact=true");
        }
        finally { await Close(tree, level); }
    }

    private static async Task Recovery(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-lethal-recovery");
        var level = PrologueGameFlowTests.Instantiate("ResidentAutoLethalRecovery", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        bool hiddenAtRequest = false, committedAtCompletion = false, recoveryDamageChecked = false; int completed = 0;
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4); flow.StartNew(); await Playing(tree, level, flow);
            long epoch = level.Residents.Epoch;
            world.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out var entrance);
            var victim = entrance.GetNode("Gameplay/Actors").GetChildren().OfType<CharacterBody2D>()
                .First(x => x.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding")?.Profile.Archetype == ResidentArchetype.Wanderer);
            var victimHealth = victim.GetNode<HealthComponent>("Health");
            TestAssert.True(victim.GetNode<Hurtbox>("Hurtbox").Receive(new(victimHealth.CurrentHealth, Vector2.Zero, 0, 62301UL)) &&
                !level.Residents.Snapshot("canyon.v2.resident.low-ceiling-bridge.01").Alive,
                "real automatically born resident dies before checkpoint reset");
            await Frames(tree, 3);
            level.RecoveryRequested += _ => hiddenAtRequest = level.ResidentPreparationPending && !world.IsVisibleInTree();
            level.RecoveryCompleted += _ =>
            {
                completed++;
                committedAtCompletion = !level.ResidentPreparationPending && level.Residents.TrackedActorCount == 24 &&
                    level.ResidentLayout.All(x => level.Residents.Snapshot(x.Definition.StableId).ResetRevision ==
                        (x.Definition.Sector == PrologueSectorId.CanyonEntrance ? 1 : 0)) &&
                    level.Residents.Snapshot("canyon.v2.resident.low-ceiling-bridge.01") is { Alive: true, Attached: true, Health: 3 };
                flow.ReturnToTitle();
            };
            var hp = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(level.Player.GetNode<Hurtbox>("Hurtbox").Receive(new(hp.CurrentHealth, Vector2.Zero, 0, 62302UL)),
                "real player lethal hit requests recovery");
            TestAssert.True(hiddenAtRequest && flow.State == PrologueGameFlowState.Recovering,
                "accepted recovery must already hide gameplay and reject further damage before publishing request");
            for (int i = 0; i < 180 && completed == 0; i++)
            {
                if (level.ResidentPreparationPending && hp.CurrentHealth > 0)
                {
                    var energy = level.Player.GetNode<PlayerEnergyController>("Energy");
                    float beforeEnergy = energy.EnergyRatio; int beforeHp = hp.CurrentHealth;
                    TestAssert.True(!level.Player.GetNode<Hurtbox>("Hurtbox").Receive(new(1, Vector2.Left, 0, 62303UL, 62304UL, DamageGuardClass.Heavy)) &&
                        hp.CurrentHealth == beforeHp && energy.EnergyRatio == beforeEnergy,
                        "real reset health remains immune to delivery during hidden recovery preparation");
                    var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
                    pause.Open(); pause.Execute(ProloguePauseCommand.ReturnToCheckpoint);
                    TestAssert.True(!tree.Paused && !pause.IsOpen, "recovery preparation cannot be paused or nested by public commands");
                    recoveryDamageChecked = true;
                }
                await Frames(tree, 1);
            }
            TestAssert.True(completed == 1 && committedAtCompletion && recoveryDamageChecked && level.Residents.Epoch == epoch,
                "RecoveryCompleted publishes only after one actual reset, dead resident fresh birth and preparation commit");
            await Frames(tree, 8);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !level.GetNode<Node2D>("PlayerRoot").Visible &&
                !level.Player.IsPhysicsProcessing() && !level.ResidentPreparationPending &&
                new[] { "Input", "Motor", "Combat", "MechanicalClaw", "ActionModes", "Shield", "Energy", "EnvironmentInteraction" }
                    .All(path => !level.Player.GetNode<Node>(path).IsPhysicsProcessing()),
                "completion observer may return to Title; old recovery await cannot reactivate player afterwards");
            GD.Print("RESIDENT_AUTO_RECOVERY_OK actual_lethal=true dead_resident_reborn=true reset_before_completion=true epoch_unchanged=true stale_resume_rejected=true");
        }
        finally { await Close(tree, level); }
    }

    private static async Task Playing(SceneTree tree, PrologueLevelController level, PrologueGameFlowController flow)
    {
        for (int i = 0; i < 180 && flow.State == PrologueGameFlowState.Intro; i++) await Frames(tree, 1);
        TestAssert.True(flow.State == PrologueGameFlowState.Playing && !level.ResidentPreparationPending,
            "actual automatic preparation reaches Playing: " + level.ResidentPreparationFailure);
    }

    public static async Task RunStreaming(SceneTree tree)
    {
        var paths = IsolatedPrologueFixture.CreatePaths("resident-auto-streaming");
        var level = PrologueGameFlowTests.Instantiate("ResidentAutoStreaming", paths.SavePath);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.AutomationAutoStart = false;
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var births = new Dictionary<string, List<ulong>>(StringComparer.Ordinal);
        Transform2D previousView = tree.Root.CanvasTransform;
        void Added(Node node)
        {
            if (node is not CharacterBody2D body || body.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding") is not { } binding) return;
            var slot = level.ResidentLayout.Single(x => x.Definition.Placement.WorldSpawn.DistanceSquaredTo(body.GlobalPosition) < .01f).Definition;
            TestAssert.True(slot.SubtypeId == binding.Profile.SubtypeId, "real automatically mounted body retains authored subtype");
            if (!births.TryGetValue(slot.StableId, out var owners)) births.Add(slot.StableId, owners = new());
            TestAssert.True(!owners.Contains(body.GetInstanceId()), "native root appears exactly once per birth");
            owners.Add(body.GetInstanceId());
            TestAssert.True(flow.State is PrologueGameFlowState.Intro or PrologueGameFlowState.Playing,
                "no automatic birth from title, paused, recovery or obsolete sessions");
        }
        CharacterBody2D Actual(string id)
        {
            var owners = births[id];
            return GodotObject.InstanceFromId(owners[^1]) as CharacterBody2D;
        }
        async Task Attached(int expected)
        {
            for (int i = 0; i < 180 && level.Residents.TrackedActorCount != expected && level.ResidentPreparationFailure.Length == 0; i++)
                await Frames(tree, 1);
            TestAssert.True(level.Residents.TrackedActorCount == expected && level.ResidentPreparationFailure.Length == 0,
                "actual streamed population expected=" + expected + " actual=" + level.Residents.TrackedActorCount + " failure=" + level.ResidentPreparationFailure);
        }
        async Task Retreat()
        {
            using var keyboard = KeyboardRouteInputDriver.Begin(level.Player);
            keyboard.Command(InputActions.MoveLeft, true);
            for (int i = 0; i < 60 && level.Player.GlobalPosition.X > 80; i++) await Frames(tree, 1);
            keyboard.Command(InputActions.MoveLeft, false);
            TestAssert.True(level.Player.GlobalPosition.X <= 80 && level.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                "real keyboard retreat clears conservative player birth envelope without pose/HP injection: " + level.Player.GlobalPosition);
        }
        const string wounded = "canyon.v2.resident.low-ceiling-bridge.01";
        try
        {
            tree.Root.AddChild(level); await Frames(tree, 4); tree.NodeAdded += Added;
            flow.StartNew(); await Playing(tree, level, flow);
            var first = Actual(wounded); ulong firstId = first.GetInstanceId();
            TestAssert.True(first.GetNode<Hurtbox>("Hurtbox").Receive(new(1, Vector2.Zero, 0, 62401UL)) &&
                level.Residents.Snapshot(wounded).Health == 2, "actual resident injury enters run custody before unloading");
            // Public World checkpoint transitions are lifecycle fixture setup,
            // not movement, collectible, combat or playable-route evidence.
            TestAssert.True(world.RestoreCheckpoint("canyon.v2.basin-camp"), "real committed Basin stream");
            await Attached(46);
            TestAssert.True(births.Count == 49 && !level.Residents.Snapshot(wounded).Attached &&
                level.Residents.Snapshot(wounded).Health == 2, "Basin visit adds25 identities and preserves unloaded HP");
            TestAssert.True(world.RestoreCheckpoint("canyon.v2.temple-forecourt"), "real committed Temple stream");
            await Attached(13);
            TestAssert.True(births.Count == 50 && level.ResidentLayout.GroupBy(x => x.Definition.Family)
                .All(group => group.All(x => births.ContainsKey(x.Definition.StableId))), "all50 authored stable identities have actual automatic native birth history");
            world.ResetForNewRun(); // World relocation only: NOT manager.BeginRun or a new epoch.
            await Frames(tree, 30);
            TestAssert.True(level.Residents.TrackedActorCount == 21 && births[wounded].Count == 1 &&
                level.Residents.Snapshot(wounded) is { Attached: false, Health: 2 },
                "visible Entrance return correctly defers all3 late births; never bypass viewport protection");
            // This isolated fixture deliberately disables the gameplay camera.
            // Test BOTH actual viewport branches, not a normal traversal claim.
            tree.Root.CanvasTransform = new Transform2D(0, new Vector2(-5000, -1000));
            await Retreat();
            await Attached(24);
            var restored = Actual(wounded);
            TestAssert.True(restored.GetInstanceId() != firstId && restored.GetNode<HealthComponent>("Health").CurrentHealth == 2 &&
                level.Residents.Epoch == 2 && births[wounded].Count == 2,
                "actual return uses a fresh native owner with retained2HP, no new-run heal or duplicate root");
            await Frames(tree, 24);
            TestAssert.True(restored.GetNode<Hurtbox>("Hurtbox").Receive(new(2, Vector2.Zero, 0, 62402UL)) &&
                !level.Residents.Snapshot(wounded).Alive, "real second hit kills returned resident");
            tree.Root.CanvasTransform = previousView;
            world.RestoreCheckpoint("canyon.v2.temple-forecourt"); await Attached(13);
            world.ResetForNewRun();
            tree.Root.CanvasTransform = new Transform2D(0, new Vector2(-5000, -1000));
            await Retreat(); await Attached(23);
            TestAssert.True(!level.Residents.Snapshot(wounded).Alive && !level.Residents.Snapshot(wounded).Attached && births[wounded].Count == 2,
                "dead stable identity stays dead after second unload and return without resurrection");
            GD.Print("RESIDENT_AUTO_STREAM_OK unique_identities=50 initial=24 basin=46 temple=13 return=24 dead_return=23 hp=3->2->2->0 epoch=2 manual_spawns=0 fixture_checkpoint_transfers=true fixture_viewport_control=true real_keyboard_retreat=true visible_birth_rejected=true normal_route=false final_art=false");
        }
        finally { tree.NodeAdded -= Added; tree.Root.CanvasTransform = previousView; await Close(tree, level); }
    }

    // Break caught: hidden preparation treats the captured player's native
    // body/Hurtbox as foreign blockers, or silently defers a mixed real blocker.
    private static async Task PlayerOverlapPreparation(SceneTree tree, bool foreignBody,
        bool corridorOnly = false, bool foreignArea = false)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        RectangleShape2D foreignShape = null;
        Exception primary = null;
        try
        {
            var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            player.Position = new Vector2(2800, 4080); // Original upper-walkway fixture, pre-tree only.
            player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            world.InitialSector = PrologueSectorId.CanyonBasin;
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = true;
            var paths = IsolatedPrologueFixture.Configure(level, $"resident-player-overlap-{foreignBody}-{corridorOnly}-{foreignArea}");
            byte[] previous = null;
            int sessions = 0;
            flow.SessionStarted += () => sessions++;
            if (foreignBody)
            {
                var saved = PrologueSaveData.NewLayoutV2();
                saved.CheckpointId = "canyon.v2.temple-forecourt";
                PrologueGameFlowTests.WriteFixtureSave(paths.SavePath, saved);
                previous = File.ReadAllBytes(paths.SavePath);
                // Deliberately same layer as Player: layer/name alone must
                // never grant another physical body's player-only exemption.
                foreignShape = new RectangleShape2D { Size = corridorOnly ? new Vector2(8, 8) : new Vector2(12, 12) };
                CollisionObject2D obstacle = foreignArea
                    ? new Area2D { Monitoring = false } : new StaticBody2D();
                obstacle.Name = "ForeignPlayerLayer";
                // [2890,4101]..[2898,4109] is inside the2854→2918
                // corridor but outside ALL authored endpoint bodies and the
                // stationary player's body and Grow64 protection envelope.
                obstacle.Position = corridorOnly ? new Vector2(2894, 4105) : new Vector2(2822, 4105);
                obstacle.CollisionLayer = CombatCollisionLayers.Player; obstacle.CollisionMask = 0;
                obstacle.AddChild(new CollisionShape2D { Name = "BodyCollider", Shape = foreignShape });
                level.AddChild(obstacle);
            }
            tree.Root.AddChild(level);
            for (int frame = 0; frame < 8; frame++)
            {
                await Frames(tree, 1);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            for (int frame = 0; frame < 180 && flow.State == PrologueGameFlowState.Intro; frame++)
            {
                await Frames(tree, 1);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            const string slot = "canyon.v2.resident.gear-pistons.03";
            if (!foreignBody)
            {
                TestAssert.True(flow.State == PrologueGameFlowState.Playing && !level.ResidentPreparationPending &&
                    level.Residents.Snapshot(slot) is { Alive: true, Attached: false } &&
                    player.GlobalPosition.DistanceTo(new Vector2(2800, 4080)) < .1f,
                    "PLAYER_ONLY_NATIVE_OVERLAP: actual captured body/Hurtbox defers its alive slot without moving player or blocking Playing: " +
                    $"state={flow.State} pending={level.ResidentPreparationPending} player={player.GlobalPosition} " + level.ResidentPreparationFailure);
                GD.Print("RESIDENT_PLAYER_NATIVE_DEFERRED_OK same_player=true alive=true attached=false pre_tree_fixture=true");
            }
            else
            {
                string reason = level.ResidentPreparationFailure;
                string expectedHit = (corridorOnly ? "corridor_" : "") + (foreignArea ? "native_area" : "native_actor");
                TestAssert.True(flow.State == PrologueGameFlowState.Title && !level.ResidentPreparationPending &&
                    reason.Contains(slot, StringComparison.Ordinal) && reason.Contains(expectedHit, StringComparison.Ordinal) &&
                    !reason.Contains("180 native", StringComparison.Ordinal) &&
                    level.Residents.Snapshot(slot) is { Alive: true, Attached: false },
                    "PLAYER_MIXED_NATIVE_OVERLAP: foreign same-layer body remains an explicit failure, never player-only exemption or timeout: " + reason);
                TestAssert.True(flow.HasSave && flow.Save.CheckpointId == "canyon.v2.temple-forecourt" && sessions == 0 &&
                    previous.SequenceEqual(File.ReadAllBytes(paths.SavePath)),
                    "actual resident admission rejection cannot consume the prior Continue slot");
                GD.Print($"RESIDENT_PLAYER_MIXED_BLOCKER_REJECTED_OK location={(corridorOnly ? "corridor" : "home")} " +
                    $"kind={(foreignArea ? "area" : "body")} same_layer_foreign=true explicit_failure=true timeout=false alive=true attached=false");
            }
        }
        catch (Exception error) { primary = error; throw; }
        finally
        {
            try { await ApprovedLanternRuntimeTests.ReleaseOwnedLevel(tree, level, primary); }
            finally { foreignShape?.Dispose(); }
        }
    }

    private static async Task Close(SceneTree tree, PrologueLevelController level)
    {
        tree.Paused = false;
        if (GodotObject.IsInstanceValid(level))
        {
            if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            else level.Free();
        }
    }

    private static async Task InactiveAttackGeometry(SceneTree tree)
    {
        using var capsule = new CapsuleShape2D { Radius = 18, Height = 64 };
        using var attackShape = new RectangleShape2D { Size = new Vector2(26, 18) };
        var scope = new Node2D { Name = "ResidentAttackGeometryControls" };
        var actor = new CharacterBody2D { Name = "PlacementProbe" };
        actor.AddChild(new CollisionShape2D { Name = "BodyCollider", Shape = capsule });
        Vector2 site = new(20000, -20000);
        var hitbox = new Hitbox { Name = "CurrentAutomaticAttack", Position = site,
            CollisionLayer = CombatCollisionLayers.EnemyAttack, CollisionMask = CombatCollisionLayers.Player,
            Monitorable = false };
        hitbox.AddChild(new CollisionShape2D { Name = "AttackCollider", Shape = attackShape });
        scope.AddChild(hitbox);
        tree.Root.AddChild(scope);
        Rect2 permitted = new(19800, -20200, 400, 400);
        ResidentBodyGeometryResult Body() => ResidentPlacementGeometry.CheckBody(actor, new(0, site),
            scope.GetWorld2D().DirectSpaceState, scope, permitted, false, Array.Empty<ResidentKeepOut>());
        ResidentPathGeometryResult Corridor() => ResidentPlacementGeometry.CheckCorridor(actor,
            new(0, site + new Vector2(-80, 0)), new(0, site + new Vector2(80, 0)),
            scope.GetWorld2D().DirectSpaceState, scope, permitted, false, Array.Empty<ResidentKeepOut>());
        try
        {
            await Frames(tree, 2);
            hitbox.Begin(78001);
            await Frames(tree, 2);
            TestAssert.True(!Body().BodyGeometryClear && Body().Issues.Contains("native_hazard") &&
                !Corridor().CorridorClear && Corridor().Issues.Contains("corridor_native_hazard"),
                "real active Hitbox blocks both complete body and intermediate patrol corridor");
            hitbox.End();
            await Frames(tree, 2);
            TestAssert.True(Body().BodyGeometryClear && Corridor().CorridorClear,
                "inactive automatic Hitbox geometry is not a current damaging native hazard");
            hitbox.Begin(78002);
            TestAssert.True(!Body().BodyGeometryClear && !Corridor().CorridorClear,
                "same-frame reactivation is never exempt merely because its shape/layer stayed unchanged");
            hitbox.End();
            var unknown = new Area2D { Name = "UnknownDangerArea", Position = site, Monitoring = false,
                CollisionLayer = CombatCollisionLayers.EnemyAttack, CollisionMask = 0 };
            unknown.AddChild(new CollisionShape2D { Name = "Collision", Shape = attackShape });
            scope.AddChild(unknown);
            await Frames(tree, 2);
            TestAssert.True(!Body().BodyGeometryClear && !Corridor().CorridorClear,
                "unknown EnemyAttack Area remains conservative even when Monitoring=false");
            GD.Print("RESIDENT_INACTIVE_ATTACK_GEOMETRY_OK active_rejected=true inactive_clear=true reactivated_rejected=true unknown_area_rejected=true body_and_corridor=true");
        }
        finally { actor.Free(); scope.Free(); }
    }

    private static async Task Frames(SceneTree tree, int count)
    { for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
}
