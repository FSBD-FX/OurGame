using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Combat;
using Knight.Presentation;
using Knight.Settings;
using Knight.Player;
using Knight.Ui;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Vfx;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class PrologueAudioDirectorTests
{
    public static async Task SectorCommitObserverFaultsDoNotAbortRecovery(SceneTree tree)
    {
        const string worldFault = "intentional KNIGHT sector commit observer fault";
        const string audioFault = "intentional KNIGHT typed cleanup observer fault";
        ExpectedWarningLogger.AssertMatcherGuards();
        using var warningLogger = new ExpectedWarningLogger();
        var failures = new List<string>();
        void Check(bool condition, string contract)
        {
            if (!condition) failures.Add(contract);
        }
        PrologueLevelController level = GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        var audio = level.GetNode<PrologueAudioDirector>("Audio");
        var vfx = level.GetNode<PrologueVfxDirector>("Vfx");
        var world = level.GetNode<SeamlessPrologueWorld>(level.SeamlessWorldPath);
        audio.PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "audio-sector-observer-faults");
        bool throwWorld = false, throwAudio = false;
        int worldFaults = 0, audioFaults = 0, laterWorld = 0, laterClear = 0, completions = 0;
        void ThrowWorld(SeamlessPrologueWorld owner, PrologueSector sector)
        {
            if (!throwWorld) return;
            worldFaults++;
            throw new InvalidOperationException(worldFault);
        }
        void ThrowAudio()
        {
            if (!throwAudio) return;
            audioFaults++;
            throw new InvalidOperationException(audioFault);
        }
        void OnLaterWorld(SeamlessPrologueWorld owner, PrologueSector sector) => laterWorld++;
        void OnLaterClear() => laterClear++;
        void OnCompleted(string reason) => completions++;
        // Install before _Ready: faults precede Audio's World subscription and
        // VFX's Audio subscription, rather than only testing a last observer.
        world.ActiveSectorCommitted += ThrowWorld;
        audio.Task10TransitionsCleared += ThrowAudio;
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 2);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            CharacterBody2D player = level.Player;
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            var input = player.GetNode<PlayerInputReader>("Input");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            world.ActiveSectorCommitted += OnLaterWorld;
            audio.Task10TransitionsCleared += OnLaterClear;
            level.RecoveryCompleted += OnCompleted;
            await Tap(tree, InputActions.SelectGrappleMode);
            await Tap(tree, InputActions.SelectAttackMode);
            await Tap(tree, InputActions.Slot2);
            Check(modes.CurrentMode == PlayerActionMode.Attack && modes.ActiveSlot == 2 &&
                modes.LastRejection == PlayerActionRejectionReason.None &&
                audio.LastCue == PrologueAudioCue.ModeAttack && vfx.ActiveTask10EffectCount > 0 &&
                world.CurrentSector == PrologueSectorId.CanyonEntrance,
                "fault fixture establishes real Attack/slot2/None, old ModeAttack feedback and Entrance");
            GD.Print($"SECTOR_OBSERVER_FAULT_FACTS stage=pre-audio mode={modes.CurrentMode} " +
                $"slot={modes.ActiveSlot} rejection={modes.LastRejection} cue={audio.LastCue} " +
                $"keys={vfx.ActiveTask10EffectCount} sector={world.CurrentSector}");

            int clearBefore = laterClear;
            ulong generationBefore = vfx.ThrusterClearGeneration;
            bool audioEscaped = false;
            throwAudio = true;
            try { audio.ClearTask10Transitions(); }
            catch (Exception error)
            {
                audioEscaped = true;
                GD.Print($"SECTOR_OBSERVER_FAULT_ESCAPED stage=audio type={error.GetType().Name} message={error.Message}");
            }
            throwAudio = false;
            GD.Print($"SECTOR_OBSERVER_FAULT_FACTS stage=after-audio escaped={audioEscaped} " +
                $"faults={audioFaults} later={laterClear}/{clearBefore} keys={vfx.ActiveTask10EffectCount} " +
                $"generation={vfx.ThrusterClearGeneration}/{generationBefore}");
            Check(!audioEscaped && audioFaults == 1 && laterClear == clearBefore + 1 &&
                vfx.ActiveTask10EffectCount == 0 && vfx.ThrusterClearGeneration == generationBefore + 1,
                "AUDIO_TYPED_CLEANUP_OBSERVER_RED throwing first listener cannot skip VFX or later listeners");
            audio.ClearTask10Transitions();
            await Tap(tree, InputActions.SelectGrappleMode);
            await Tap(tree, InputActions.SelectAttackMode);
            Check(vfx.ActiveTask10EffectCount > 0 && audio.LastCue == PrologueAudioCue.ModeAttack,
                "world fault stage starts with real old ModeAttack feedback");

            clearBefore = laterClear;
            int worldBefore = laterWorld;
            bool worldEscaped = false, restored = false;
            throwWorld = true;
            try { restored = world.RestoreCheckpoint("canyon.v2.basin-camp"); }
            catch (Exception error)
            {
                worldEscaped = true;
                GD.Print($"SECTOR_OBSERVER_FAULT_ESCAPED stage=world type={error.GetType().Name} message={error.Message}");
            }
            GD.Print($"SECTOR_OBSERVER_FAULT_FACTS stage=after-world escaped={worldEscaped} " +
                $"restored={restored} sector={world.CurrentSector} checkpoint={world.CheckpointId} " +
                $"world_faults={worldFaults} later_world={laterWorld}/{worldBefore} " +
                $"later_clear={laterClear}/{clearBefore} keys={vfx.ActiveTask10EffectCount}");
            Check(!worldEscaped && restored && world.CurrentSector == PrologueSectorId.CanyonBasin &&
                world.CheckpointId == "canyon.v2.basin-camp" && worldFaults == 1 &&
                laterWorld == worldBefore + 1 && laterClear == clearBefore + 1 && vfx.ActiveTask10EffectCount == 0,
                "WORLD_POSTCOMMIT_OBSERVER_RED committed identity/metadata and later Audio/observers survive a first listener fault");
            // Before any await, avoid real camp BodyEntered activating Level's save.
            Check(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(1680f, 3760f)),
                "same-root fault fixture relocation succeeds before camp overlap");
            Check(level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.start" &&
                !level.Progress.RestoreCheckpoint().HasActivatedCheckpoint,
                "observer fault recovery retains the actual unactivated Entrance checkpoint");

            level.RequestRespawn("SECTOR OBSERVER FAULT RECOVERY");
            ulong recoveryGeneration = vfx.ThrusterClearGeneration;
            int recoveryFrames = 0;
            while (completions == 0 && recoveryFrames < 4)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                recoveryFrames++;
            }
            Check(completions == 1 && world.CurrentSector == PrologueSectorId.CanyonEntrance &&
                world.CheckpointId == "canyon.v2.start" && worldFaults == 2 &&
                laterWorld == worldBefore + 2 &&
                vfx.ThrusterClearGeneration == recoveryGeneration,
                "WORLD_RECOVERY_OBSERVER_RED first observer fault cannot abort the real cross-sector recovery");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            GD.Print($"SECTOR_OBSERVER_FAULT_FACTS stage=after-recovery completions={completions} " +
                $"frames={recoveryFrames}/4 sector={world.CurrentSector} checkpoint={world.CheckpointId} " +
                $"world_faults={worldFaults} audio_faults={audioFaults} later_world={laterWorld} " +
                $"later_clear={laterClear} busy={level.RecoveryInProgress} input={input.IsPhysicsProcessing()} " +
                $"generation={vfx.ThrusterClearGeneration}/{recoveryGeneration} failures={failures.Count}");
            Check(completions == 1 && !level.RecoveryInProgress && input.IsPhysicsProcessing() &&
                level.GetNode<PrologueGameFlowController>("GameFlow").State == PrologueGameFlowState.Playing &&
                vfx.ThrusterClearGeneration == recoveryGeneration,
                "observer fault recovery resumes gameplay without a stuck recovery or duplicate idle clear");
        }
        finally
        {
            throwWorld = false;
            throwAudio = false;
            world.ActiveSectorCommitted -= ThrowWorld;
            world.ActiveSectorCommitted -= OnLaterWorld;
            audio.Task10TransitionsCleared -= ThrowAudio;
            audio.Task10TransitionsCleared -= OnLaterClear;
            level.RecoveryCompleted -= OnCompleted;
            foreach (StringName action in new[]
                { InputActions.SelectGrappleMode, InputActions.SelectAttackMode, InputActions.Slot2 })
                Input.ActionRelease(action);
            tree.Paused = false;
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        TestAssert.True(failures.Count == 0,
            "SECTOR_FEEDBACK_OBSERVER_FAULT_RED " + string.Join("; ", failures));
        warningLogger.AssertExact(
            new ExpectedWarningLogger.Expected(
                "SeamlessPrologueWorld isolated a throwing ActiveSectorCommitted observer: " + worldFault,
                "scripts/prologue/seamless/SeamlessPrologueWorld.cs", "DispatchActiveSectorCommitted", 2),
            new ExpectedWarningLogger.Expected(
                "PrologueAudioDirector isolated a throwing Task10TransitionsCleared observer: " + audioFault,
                "scripts/audio/PrologueAudioDirector.cs", "ClearTask10Transitions", 1));
        GD.Print("SECTOR_COMMIT_WARNING_SCOPE_OK count=2 source=scripts/prologue/seamless/SeamlessPrologueWorld.cs function=DispatchActiveSectorCommitted");
        GD.Print("SECTOR_COMMIT_WARNING_SCOPE_OK count=1 source=scripts/audio/PrologueAudioDirector.cs function=ClearTask10Transitions");
        GD.Print("SECTOR_FEEDBACK_FAULTS_OK actual_publishers=true cross_sector_recovery=true " +
            "later_observers=true transaction_only=true human=false");
    }

    // Transaction-only lifecycle contract, not a player-movement/route proof.
    // Catches deferred sector clears and a duplicate idle clear stealing a new cue.
    public static async Task SectorCommitClearsOldFeedbackSynchronously(SceneTree tree)
    {
        RecoveryResourceBoundaryTrace.Point("load-enter", "saved-scene");
        PrologueLevelController level = RecoveryResourceBoundaryTrace.Loaded(GD.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn"), "saved-scene").Instantiate<PrologueLevelController>();
        ulong traceLevelId = RecoveryResourceBoundaryTrace.Observe("instantiate-return", "level", level);
        var audio = level.GetNode<PrologueAudioDirector>("Audio");
        var vfx = level.GetNode<PrologueVfxDirector>("Vfx");
        audio.PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "audio-sector-commit-transaction");
        int clears = 0, emitted = 0, recoveryCompletions = 0;
        PrologueAudioCue observedCue = default;
        ulong observedTransitionId = 0;
        void OnClear() => clears++;
        void OnRecoveryCompleted(string reason) => recoveryCompletions++;
        void OnCue(PrologueAudioCue cue, ulong transitionId, Vector2 position)
        {
            emitted++;
            observedCue = cue;
            observedTransitionId = transitionId;
        }
        bool HasRegisteredCue(PrologueAudioCue cue, ulong transitionId)
        {
            var field = typeof(PrologueVfxDirector).GetField("_task10Effects",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var effects = field?.GetValue(vfx) as
                Dictionary<(PrologueAudioCue Cue, ulong TransitionId, int EventKind), PrologueVfxBurst>;
            TestAssert.True(effects != null, "sector transaction observes the existing Task10 registry");
            return effects.ContainsKey((cue, transitionId, 0));
        }
        audio.Task10TransitionsCleared += OnClear;
        audio.Task10CueEmitted += OnCue;
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 2);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            CharacterBody2D player = level.Player;
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            var input = player.GetNode<PlayerInputReader>("Input");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            var world = level.GetNode<SeamlessPrologueWorld>(level.SeamlessWorldPath);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            await Tap(tree, InputActions.SelectGrappleMode);
            await Tap(tree, InputActions.SelectAttackMode);
            await Tap(tree, InputActions.Slot2);
            TestAssert.True(modes.CurrentMode == PlayerActionMode.Attack && modes.ActiveSlot == 2 &&
                modes.LastRejection == PlayerActionRejectionReason.None && emitted > 0 &&
                observedCue == PrologueAudioCue.ModeAttack && observedTransitionId != 0 &&
                HasRegisteredCue(observedCue, observedTransitionId) && vfx.ActiveTask10EffectCount > 0,
                "real mode/loadout inputs establish Attack/slot2/None and an old ModeAttack key");
            TestAssert.True(world.CurrentSector != PrologueSectorId.CanyonBasin,
                "sector transaction starts outside the destination Basin");
            clears = 0;
            int oldCount = vfx.ActiveTask10EffectCount;
            bool restored = world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(1680f, 3960f));
            // No await between the synchronous commit and this old-key contract.
            TestAssert.True(restored && world.CurrentSector == PrologueSectorId.CanyonBasin &&
                vfx.ActiveTask10EffectCount == 0,
                $"SECTOR_COMMIT_SYNC_CLEAR_RED commit returns with old Task10 keys cleared; " +
                $"restored={restored} sector={world.CurrentSector} old={oldCount} current={vfx.ActiveTask10EffectCount}");
            TestAssert.Equal(1, clears, "one actual sector commit clears typed feedback once");
            ulong committedGeneration = vfx.ThrusterClearGeneration;
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector basin),
                "committed Basin has its actual mounted root");
            ulong basinId = basin.GetInstanceId();
            // Move above the real camp trigger before any physics overlap is
            // flushed; this transaction fixture must retain its Entrance save.
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(1680f, 3760f)) &&
                world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out PrologueSector sameBasin) &&
                sameBasin.GetInstanceId() == basinId && clears == 1 &&
                vfx.ThrusterClearGeneration == committedGeneration,
                "same-sector same-root restore does not duplicate the feedback clear");
            emitted = 0;
            modes.RejectAttackRequest();
            TestAssert.True(modes.LastRejection != PlayerActionRejectionReason.None && emitted == 1 &&
                observedTransitionId != 0 && HasRegisteredCue(observedCue, observedTransitionId),
                "the real post-commit rejection publisher registers its actual emitted cue/id");
            PrologueAudioCue newCue = observedCue;
            ulong newTransitionId = observedTransitionId;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(clears == 1 && vfx.ThrusterClearGeneration == committedGeneration &&
                HasRegisteredCue(newCue, newTransitionId),
                "one complete idle keeps the new cue key and does not repeat the committed clear");

            // The Level checkpoint remains Entrance: only the world transaction
            // moved to Basin. Recovery must not clear its own feedback on that commit.
            TestAssert.True(level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.start" &&
                !level.Progress.RestoreCheckpoint().HasActivatedCheckpoint,
                "cross-sector recovery control retains the real Entrance checkpoint without touching the camp trigger");
            level.RecoveryCompleted += OnRecoveryCompleted;
            level.RequestRespawn("SECTOR COMMIT RECOVERY CONTROL");
            ulong recoveryGeneration = vfx.ThrusterClearGeneration;
            int recoveryClears = clears;
            int recoveryFrames = 0;
            while (recoveryCompletions == 0 && recoveryFrames < 4)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                recoveryFrames++;
            }
            PrologueGameFlowState recoveryFlow = level.GetNode<PrologueGameFlowController>("GameFlow").State;
            GD.Print($"SECTOR_COMMIT_RECOVERY_FACTS completions={recoveryCompletions} frames={recoveryFrames}/4 " +
                $"sector={world.CurrentSector} checkpoint={level.Progress.RestoreCheckpoint().CheckpointId} " +
                $"clear={clears}/{recoveryClears} generation={vfx.ThrusterClearGeneration}/{recoveryGeneration} " +
                $"cue={vfx.LastCue} flow={recoveryFlow}");
            TestAssert.True(recoveryCompletions == 1 && world.CurrentSector == PrologueSectorId.CanyonEntrance &&
                vfx.ThrusterClearGeneration == recoveryGeneration && clears == recoveryClears &&
                vfx.LastCue == PresentationCue.RecoveryCompleted,
                "cross-sector real recovery completes once without clearing its own start/end feedback");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(recoveryCompletions == 1 && world.CurrentSector == PrologueSectorId.CanyonEntrance &&
                vfx.ThrusterClearGeneration == recoveryGeneration && clears == recoveryClears,
                "the recovered sector baseline prevents another clear on the following complete idle");

            audio.Shutdown();
            int shutdownClears = clears;
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(1680f, 3960f)) &&
                world.CurrentSector == PrologueSectorId.CanyonBasin && clears == shutdownClears,
                "a shut-down audio owner unsubscribes from later world commits");
            GD.Print($"SECTOR_COMMIT_FEEDBACK_OK sync_old_keys=0 same_root_clear_once=true " +
                $"new_cue={newCue} new_transition={newTransitionId} complete_idle=true " +
                $"cross_sector_recovery_once=true recovery_frames={recoveryFrames}/4 " +
                "shutdown_unbound=true transaction_only=true native_player_movement=false human=false");
        }
        finally
        {
            audio.Task10TransitionsCleared -= OnClear;
            audio.Task10CueEmitted -= OnCue;
            level.RecoveryCompleted -= OnRecoveryCompleted;
            foreach (StringName action in new[]
                { InputActions.SelectGrappleMode, InputActions.SelectAttackMode, InputActions.Slot2 })
                Input.ActionRelease(action);
            tree.Paused = false;
            RecoveryResourceBoundaryTrace.Observe("cleanup-enter", "level", level, traceLevelId);
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            RecoveryResourceBoundaryTrace.Observe("cleanup-return", "level", level, traceLevelId);
        }
    }

    // Catches lifecycle listeners cancelling the very recovery cue just emitted
    // by the public recovery path. No playback wrapper or private recovery call.
    public static async Task RecoveryFeedbackSurvivesOwnBoundary(SceneTree tree)
    {
        PrologueLevelController level = P1LoopRuntimeTests.CreateLevelFixture("RecoveryFeedbackFixture");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        var audio = level.GetNode<PrologueAudioDirector>("Audio");
        audio.PlaybackEnabled = true;
        var vfx = level.GetNode<PrologueVfxDirector>("Vfx");
        const string startPath = "res://assets/audio/prologue/sfx/world/recovery-start.wav";
        const string endPath = "res://assets/audio/prologue/sfx/world/recovery-end.wav";
        int Playing(string path) => Enumerable.Range(0, 4).Count(index =>
        {
            var voice = audio.GetNode<AudioStreamPlayer>($"Sfx{index}");
            return voice.Playing && voice.Stream?.ResourcePath == path;
        });
        int LiveBursts() => vfx.GetNode<Node2D>("World").GetChildren()
            .OfType<PrologueVfxBurst>().Count(burst => !burst.IsQueuedForDeletion());
        int completions = 0;
        bool endAudio = false, endVisual = false;
        PrologueLevelController.RecoveryCompletedEventHandler completed = _ =>
        {
            completions++;
            endAudio = Playing(endPath) == 1;
            endVisual = vfx.LastCue == PresentationCue.RecoveryCompleted && LiveBursts() > 0;
        };
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 2);
            flow.StartNew();
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "feedback fixture starts through public flow");
            level.RecoveryCompleted += completed;
            audio.Play(PrologueAudioCue.PlayerAttack, level.Player.GlobalPosition);
            string priorPath = audio.LastPlayedPath;
            vfx.Play(PresentationCue.AttackHit, level.Player.GlobalPosition, Vector2.Up);
            ulong[] oldBursts = vfx.GetNode<Node2D>("World").GetChildren()
                .OfType<PrologueVfxBurst>().Select(burst => burst.GetInstanceId()).ToArray();
            TestAssert.True(Playing(priorPath) == 1 && oldBursts.Length > 0, "real prior transient audio and effects exist");

            int cuesBefore = vfx.CueCount;
            level.RequestRespawn("RECOVERY FEEDBACK");
            bool startAudio = Playing(startPath) == 1;
            bool startVisual = vfx.LastCue == PresentationCue.RecoveryStarted && LiveBursts() > 0;
            bool priorStopped = Playing(priorPath) == 0 && vfx.GetNode<Node2D>("World").GetChildren()
                .OfType<PrologueVfxBurst>().Where(burst => oldBursts.Contains(burst.GetInstanceId()))
                .All(burst => burst.IsQueuedForDeletion());
            level.RequestRespawn("DUPLICATE FEEDBACK REQUEST");
            bool startOnce = vfx.CueCount == cuesBefore + 1;
            int observed = 0;
            while (completions == 0 && observed < 4)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                observed++;
            }
            GD.Print($"RECOVERY_FEEDBACK_FACTS start_audio={startAudio} start_visual={startVisual} " +
                $"prior_stopped={priorStopped} start_once={startOnce} end_audio={endAudio} end_visual={endVisual} " +
                $"completions={completions} observation_frames={observed} observation_cap=4");
            TestAssert.True(startAudio && startVisual && priorStopped && startOnce &&
                endAudio && endVisual && completions == 1,
                "public recovery clears prior transients but preserves its own single start/end audio and visible effects");
            GD.Print("THRUSTER_RECOVERY_FEEDBACK_OK public_request=true old_transients_cleared=true " +
                "start_end_preserved=true duplicate_request_rejected=true native_playback_state_only=true human=false");
        }
        finally
        {
            tree.Paused = false;
            try
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    level.RecoveryCompleted -= completed;
                    if (level.IsInsideTree() && level.GetNodeOrNull<PrologueAudioDirector>("Audio") != null)
                        await TestSceneTeardown.DrainAudioOwner(tree, level);
                }
            }
            finally
            {
                if (GodotObject.IsInstanceValid(level) && !level.IsQueuedForDeletion()) level.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
    }

    public static async Task DamagedGuidanceCrackleIsShortIsolatedAndRateLimited(SceneTree tree)
    {
        // Catches missing output, shared UI/SFX voice stealing, pause bypass,
        // repeated instruction chatter and an owned procedural WAV leaked at shutdown.
        var method = typeof(PrologueAudioDirector).GetMethod("TryPlayGuidanceCrackle",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
            null, new[] { typeof(string) }, null);
        TestAssert.True(method != null && method.ReturnType == typeof(bool),
            "DAMAGED_AI_GUIDANCE_AUDIO_RED requires the public guarded crackle playback boundary");
        var resetSession=typeof(PrologueAudioDirector).GetMethod("ResetGuidanceSession",Type.EmptyTypes);
        TestAssert.True(resetSession!=null && resetSession.ReturnType==typeof(void),
            "DAMAGED_AI_GUIDANCE_SESSION_RED requires per-session identity reset without bypassing cooldown");
        PrologueAudioDirector director = PrologueAudioDirector.CreateRuntimeTree();
        director.PlaybackEnabled = false;
        tree.Root.AddChild(director);
        bool TryPlay(string identity) => (bool)method.Invoke(director, new object[] { identity });
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!TryPlay("first-instruction"),
                "disabled audio rejects guidance before consuming its instruction identity");
            director.PlaybackEnabled = true;
            TestAssert.True(!TryPlay(string.Empty) && !TryPlay(" \t"),
                "empty guidance has no electrical onset");
            director.SetPaused(true);
            TestAssert.True(!TryPlay("first-instruction"),
                "explicit audio pause rejects a newly visible instruction");
            director.SetPaused(false);
            tree.Paused = true;
            TestAssert.True(!TryPlay("first-instruction"),
                "tree pause rejects a newly visible instruction even without an audio pause call");
            tree.Paused = false;

            TestAssert.True(TryPlay("first-instruction"),
                "the first nonempty enabled instruction starts an actual electrical onset");
            AudioStreamPlayer voice = director.GetNodeOrNull<AudioStreamPlayer>("GuidanceCrackle");
            TestAssert.True(voice != null && voice.Bus == "UI" && voice.Playing &&
                            voice.Stream is AudioStreamWav,
                "guidance owns an actual separate UI-bus WAV player");
            AudioStreamWav wav = (AudioStreamWav)voice.Stream;
            TestAssert.True(wav.GetLength() > .04 && wav.GetLength() <= .22 &&
                            wav.Format == AudioStreamWav.FormatEnum.Format16Bits && !wav.Stereo &&
                            wav.LoopMode == AudioStreamWav.LoopModeEnum.Disabled,
                "the electrical accent is a short non-looping mono PCM clip, not speech or an ambient loop");
            byte[] pcm = wav.Data;
            float peak = 0f, firstBurst = 0f, secondBurst = 0f, middleGap = 0f;
            int samples = pcm.Length / 2;
            for (int index = 0; index < samples; index++)
            {
                float value = Mathf.Abs((short)(pcm[index * 2] | pcm[index * 2 + 1] << 8) / 32768f);
                peak = Mathf.Max(peak, value);
                if (index < samples / 3) firstBurst = Mathf.Max(firstBurst, value);
                if (index >= samples * 2 / 3) secondBurst = Mathf.Max(secondBurst, value);
                if (index >= samples * 9 / 20 && index < samples * 11 / 20)
                    middleGap = Mathf.Max(middleGap, value);
            }
            TestAssert.True(peak > .01f && peak <= .2f && firstBurst > .01f &&
                            secondBurst > .01f && middleGap == 0f,
                "real PCM contains two quiet bursts separated by silence without a full-scale spike");

            director.Play(PrologueAudioCue.UiConfirm, Vector2.Zero);
            director.Play(PrologueAudioCue.PlayerAttack, Vector2.Zero);
            TestAssert.True(voice.Playing && ReferenceEquals(wav, voice.Stream) &&
                            director.GetNode<AudioStreamPlayer>("UI").Stream != wav &&
                            director.GetNode<AudioStreamPlayer>("Sfx0").Stream != wav,
                "menu and combat cues cannot steal the dedicated damaged-AI voice");
            TestAssert.True(!TryPlay("first-instruction") && !TryPlay("second-instruction"),
                "unchanged text and changed text both cannot retrigger inside the global cooldown");
            director.SetPaused(true);
            TestAssert.True(voice.StreamPaused && !TryPlay("second-instruction"),
                "explicit pause also pauses the active crackle player");
            director.SetPaused(false);
            tree.Paused = true;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(voice.StreamPaused && !TryPlay("second-instruction"),
                "the dedicated voice itself follows a raw tree pause");
            tree.Paused = false;

            ulong waitStart = Time.GetTicksMsec();
            while (Time.GetTicksMsec() - waitStart < 8050UL)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!TryPlay("first-instruction"),
                "an unchanged visible instruction stays silent even after cooldown expiry");
            resetSession.Invoke(director,null);
            TestAssert.True(TryPlay("first-instruction") && voice.Playing,
                "the same initial instruction may speak once in a new session after cooldown");
            resetSession.Invoke(director,null);
            TestAssert.True(!TryPlay("first-instruction") && !TryPlay("second-instruction"),
                "session identity reset never bypasses the global eight-second cooldown");
            // Identity-only exit diagnosis: do not acquire a playback wrapper,
            // hold another resource, or alter the restart/shutdown boundary.
            GD.Print($"ECHO_OWNED_WAV_ID={wav.GetInstanceId()} path={wav.ResourcePath}");
            director.Shutdown();
            TestAssert.True(!voice.Playing && voice.Stream == null && !GodotObject.IsInstanceValid(wav),
                "shutdown stops and detaches the voice before disposing its owned procedural WAV");
            TestAssert.True(tree.Root.GetNodeOrNull<PrologueAudioDirector>("Audio") == director,
                "audio drain targets this test's owned ECHO director");
            // Keep the immediate shutdown contract above; only process-exit
            // cleanup waits for an observed native audio mix after owner release.
            await TestSceneTeardown.DrainAudioOwner(tree, tree.Root);
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(director) && !director.IsQueuedForDeletion())
                director.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    public static async Task Task10ActionCueBaselineAndReentryAreExact(SceneTree tree)
    {
        RecoveryResourceBoundaryTrace.Point("load-enter", "saved-scene");
        PrologueLevelController level = RecoveryResourceBoundaryTrace.Loaded(GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn"), "saved-scene")
            .Instantiate<PrologueLevelController>();
        ulong traceLevelId = RecoveryResourceBoundaryTrace.Observe("instantiate-return", "level", level);
        PrologueAudioDirector audio = level.GetNode<PrologueAudioDirector>("Audio");
        audio.PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "audio-action-baseline-reentry");
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 3);
            CharacterBody2D player = level.Player;
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            PlayerActionModeController modes =
                player.GetNode<PlayerActionModeController>("ActionModes");
            PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
            PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
            PrologueVfxDirector vfx = level.GetNode<PrologueVfxDirector>("Vfx");
            var failures = new List<string>();
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            var input = player.GetNode<PlayerInputReader>("Input");
            void TraceFirstCue(string stage) => GD.Print(
                $"TASK10_FIRST_CUE_BOUNDARY stage={stage} flow={flow.State} " +
                $"pending={level.ResidentPreparationPending} physics={Engine.GetPhysicsFrames()} " +
                $"input_sequence={input.CurrentFrameSequence} input_enabled={input.IsPhysicsProcessing()} " +
                $"combat_enabled={combat.IsPhysicsProcessing()} motor_enabled={player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing()} " +
                $"primary={input.Current.PrimaryPressed} keyboard={input.Current.KeyboardActions} " +
                $"slot={modes.ActiveSlot} rejection={modes.LastRejection} " +
                $"energy={energy.EnergyRatio:R} attacking={combat.IsAttacking}");
            RecoveryResourceBoundaryTrace.Observe("fixture-enter", "level", level, traceLevelId);
            TraceFirstCue("fixture-enter");
            // Startup owns input/combat activation until resident preparation
            // commits. A fixed three frames is not a playable session.
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            RecoveryResourceBoundaryTrace.Observe("fixture-ready", "level", level, traceLevelId);
            TraceFirstCue("fixture-ready");
            TestAssert.True(input.IsPhysicsProcessing() && combat.IsPhysicsProcessing() &&
                !input.Current.PrimaryPressed && !combat.IsAttacking,
                "first cue fixture starts after real activation and a fresh neutral input sample");

            TestAssert.Equal(PlayerActionMode.Attack, modes.CurrentMode,
                "generated main begins in the real Attack action state");
            TestAssert.Equal(1, modes.ActiveSlot,
                "generated main begins on the real occupied attack loadout");

            audio.ClearStagedFrameAudio();
            vfx.ClearTransientEffects();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            int firstLoadoutStart = audio.Task10CueHistory.Count;
            await Tap(tree, InputActions.Slot2);
            TestAssert.Equal(2, modes.ActiveSlot,
                "the real first loadout input was consumed, not silenced by startup");
            PrologueAudioCue[] firstLoadout = audio.Task10CueHistory.Skip(firstLoadoutStart).ToArray();
            if (firstLoadout.Length != 0 || vfx.ActiveTask10EffectCount != 0)
                failures.Add("TASK10_FIX2_FALSE_FIRST_LOADOUT_RED expected=no-cue/no-vfx actual=" +
                    $"{string.Join(',', firstLoadout)}/vfx={vfx.ActiveTask10EffectCount}");

            await Tap(tree, InputActions.Slot1);
            TestAssert.Equal(1, modes.ActiveSlot,
                "the real attack loadout is restored before first empty-energy input");
            energy.ResetFull();
            audio.ClearStagedFrameAudio();
            vfx.ClearTransientEffects();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(energy.TrySpendHeavyGuard(100f),
                "real energy owner establishes the first-publication rejection precondition");
            int firstRejectionStart = audio.Task10CueHistory.Count;
            int rejected = 0, changed = 0, started = 0, emitted = 0;
            void OnRejected(PlayerActionRejectionReason reason)
            { rejected++; TraceFirstCue("primary-rejected-" + reason); }
            void OnChanged(PlayerActionRejectionReason reason)
            { changed++; TraceFirstCue("rejection-changed-" + reason); }
            void OnStarted(ulong attackId, int direction)
            { started++; TraceFirstCue("attack-started"); }
            void OnCue(PrologueAudioCue cue, ulong sequence, Vector2 position)
            { emitted++; TraceFirstCue("cue-" + cue); }
            modes.PrimaryRejected += OnRejected;
            modes.RejectionChanged += OnChanged;
            combat.AttackStarted += OnStarted;
            audio.Task10CueEmitted += OnCue;
            try
            {
                TraceFirstCue("primary-enter");
                await Tap(tree, InputActions.PrimaryAction);
                TraceFirstCue("primary-return");
                GD.Print($"TASK10_FIRST_CUE_EVENTS rejected={rejected} changed={changed} " +
                    $"started={started} emitted={emitted}");
            }
            finally
            {
                modes.PrimaryRejected -= OnRejected;
                modes.RejectionChanged -= OnChanged;
                combat.AttackStarted -= OnStarted;
                audio.Task10CueEmitted -= OnCue;
            }
            PrologueAudioCue[] firstRejection =
                audio.Task10CueHistory.Skip(firstRejectionStart).ToArray();
            if (!firstRejection.SequenceEqual(new[] { PrologueAudioCue.EnergyEmpty }) ||
                vfx.ActiveTask10EffectCount != 1 || rejected != 1 || changed != 1 ||
                started != 0 || emitted != 1)
                failures.Add("TASK10_FIX2_FALSE_FIRST_REJECTION_RED expected=EnergyEmpty/vfx=1 actual=" +
                    $"{string.Join(',', firstRejection)}/vfx={vfx.ActiveTask10EffectCount}/" +
                    $"rejected={rejected}/changed={changed}/started={started}/emitted={emitted}");

            energy.ResetFull();
            audio.Shutdown();
            int rebindStart = audio.Task10CueHistory.Count;
            await Frames(tree, 2);
            if (audio.Task10CueHistory.Count != rebindStart)
                failures.Add("TASK10_FIX2_REBIND_SEED_RED refresh emitted a cue before input");
            vfx.ClearTransientEffects();
            await Tap(tree, InputActions.Slot2);
            PrologueAudioCue[] firstAfterRebind =
                audio.Task10CueHistory.Skip(rebindStart).ToArray();
            if (firstAfterRebind.Length != 0 || vfx.ActiveTask10EffectCount != 0)
                failures.Add("TASK10_FIX2_REBIND_FIRST_EVENT_RED expected=no-cue/no-vfx actual=" +
                    $"{string.Join(',', firstAfterRebind)}/vfx={vfx.ActiveTask10EffectCount}");

            await Tap(tree, InputActions.Slot1);
            energy.ResetFull();
            await Tap(tree, InputActions.PrimaryAction);
            await WaitFor(tree, () => !combat.IsAttacking, 60,
                "real reset attack completes before independent re-entry coverage");
            await Tap(tree, InputActions.SelectGrappleMode);
            await Tap(tree, InputActions.SelectAttackMode);
            energy.ResetFull();
            TestAssert.True(energy.TrySpendHeavyGuard(100f),
                "real energy owner establishes the first re-entry rejection");
            int reentryStart = audio.Task10CueHistory.Count;
            int reentryCountStart = audio.Task10TransitionCueCount;
            vfx.ClearTransientEffects();
            await Tap(tree, InputActions.PrimaryAction);
            energy.ResetFull();
            await Tap(tree, InputActions.PrimaryAction);
            await WaitFor(tree, () => combat.IsAttacking, 5,
                "real recovered primary action starts production combat");
            await WaitFor(tree, () => !combat.IsAttacking, 60,
                "real successful attack completes before the second rejection");
            float drainToOne = energy.EnergyRatio * PlayerEnergySettings.Approved.MaxEnergy - 1f;
            TestAssert.True(energy.TrySpendHeavyGuard(drainToOne),
                "real post-attack energy is reduced below attack cost for exact re-entry");
            await Tap(tree, InputActions.PrimaryAction);
            PrologueAudioCue[] reentry = audio.Task10CueHistory.Skip(reentryStart).ToArray();
            int reentryCueCount = audio.Task10TransitionCueCount - reentryCountStart;
            if (!reentry.SequenceEqual(new[]
                {
                    PrologueAudioCue.EnergyEmpty,
                    PrologueAudioCue.EnergyEmpty
                }) || reentryCueCount != 2 || vfx.ActiveTask10EffectCount != 2)
                failures.Add("TASK10_FIX2_ENERGY_REENTRY_RED expected=" +
                    "EnergyEmpty,EnergyEmpty/count=2/vfx=2 actual=" +
                    $"{string.Join(',', reentry)}/count={reentryCueCount}/" +
                    $"vfx={vfx.ActiveTask10EffectCount}");

            if (failures.Count > 0)
                throw new InvalidOperationException(string.Join("; ", failures));
        }
        finally
        {
            foreach (StringName action in new[]
            {
                InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
                InputActions.Slot1, InputActions.Slot2, InputActions.PrimaryAction
            }) Input.ActionRelease(action);
            tree.Paused = false;
            RecoveryResourceBoundaryTrace.Observe("cleanup-enter", "level", level, traceLevelId);
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            RecoveryResourceBoundaryTrace.Observe("cleanup-return", "level", level, traceLevelId);
        }
    }

    public static async Task ShieldBurstsRespectReducedFlashes(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, nameof(ShieldBurstsRespectReducedFlashes));
        var audio = level.GetNode<PrologueAudioDirector>("Audio");
        var vfx = level.GetNode<PrologueVfxDirector>("Vfx");
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        audio.PlaybackEnabled = false;
        flow.AutomationAutoStart = true;
        var samples = new List<ShieldBurstSample>();
        PrologueVfxBurst FindBurst(PrologueAudioCue cue, ulong id) =>
            vfx.GetNodeOrNull<PrologueVfxBurst>($"World/{ShieldEffectRoot(cue)}/{cue}{id}");
        void ObserveCue(PrologueAudioCue cue, ulong id, Vector2 position)
        {
            if (ShieldEffectRoot(cue) == null) return;
            PrologueVfxBurst burst = FindBurst(cue, id);
            samples.Add(burst == null
                ? new(cue, id, false, 0f, 0f, 0, 0f, default)
                : new(cue, id, burst.IsVisibleInTree(), BurstCanvasAlpha(burst), burst.LifetimeSeconds,
                    burst.RayCount, burst.Radius, burst.CueColor));
        }

        try
        {
            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            var player = level.Player;
            var input = player.GetNode<PlayerInputReader>("Input");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            var health = player.GetNode<HealthComponent>("Health");
            var hurtbox = player.GetNode<Hurtbox>("Hurtbox");
            var shieldVisual = player.GetNode<PlayerShieldPlaceholderVisual>("VisualRoot/Shield");
            var hud = level.GetNode<PrologueEnergyHeatVisual>("Hud/FormalHud/ResourceCluster");
            await WaitFor(tree, player.IsOnFloor, 90, "shield test starts on real canyon support");
            // Keep the real shield/input/energy/HUD active but hold the supported test position through the heavy hit.
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            audio.Task10CueEmitted += ObserveCue;

            async Task<ShieldBurstSample[]> PlayShieldCycle(bool reduce, ulong attackId)
            {
                flow.ApplySettings(new GameSettingsData { ReduceFlashes = reduce, GuidanceEnabled = false });
                TestAssert.True(GameSettingsStore.LoadOrDefault(flow.SettingsPath).ReduceFlashes == reduce,
                    "real GameFlow applies and persists the requested setting to the isolated slot");
                samples.Clear();
                int hp = health.CurrentHealth;
                KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
                await WaitFor(tree, () => shield.IsActive, 20, "real L raises the shield");
                TestAssert.True(input.Current.KeyboardActions && input.Current.ShieldHeld &&
                    Input.IsPhysicalKeyPressed(Key.L) && shieldVisual.IsPresented && shieldVisual.IsVisibleInTree(),
                    "the real translucent guard remains visible with normal keyboard input");
                Vector2 front = shield.GuardDirection;
                Hitbox normal = await AddHitbox(tree, level, $"ShieldFlashNormal{attackId}", player,
                    front, DamageGuardClass.Normal, attackId, 1);
                TestAssert.True(normal.TryConnect(hurtbox) && health.CurrentHealth == hp && shield.IsActive,
                    "a normal front hit is blocked without damage in both settings");
                TestAssert.True(energy.TrySpendHeavyGuard(80f), "set real energy below the heavy guard cost");
                Hitbox heavy = await AddHitbox(tree, level, $"ShieldFlashHeavy{attackId}", player,
                    front, DamageGuardClass.Heavy, attackId + 1, 1);
                TestAssert.True(heavy.TryConnect(hurtbox) && health.CurrentHealth == hp - 1 && !shield.IsActive &&
                    !shieldVisual.IsPresented, "a heavy front hit still breaks the low-energy guard in both settings");
                KeyboardActionRuntimeTests.KeyEvent(Key.L, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 2);
                health.ResetFull();
                energy.ResetFull();
                shield.ResetFull();

                KeyboardActionRuntimeTests.KeyEvent(Key.L, true);
                await WaitFor(tree, () => energy.IsOverheated && hud.CurrentFrame.IsOverheated,
                    240, "real hold reaches overheat and the persistent HUD reads venting");
                TestAssert.True(!shield.IsActive && !shieldVisual.IsPresented && hud.IsVisibleInTree(),
                    "overheat closes the guard but retains the visible HUD warning");
                KeyboardActionRuntimeTests.KeyEvent(Key.L, false);
                await WaitFor(tree, () => !energy.IsOverheated && !hud.CurrentFrame.IsOverheated,
                    360, "real cooling clears the persistent HUD warning");
                TestAssert.True(!shield.IsActive && !shieldVisual.IsPresented,
                    "cooling to ready does not re-raise the released guard");
                energy.ResetFull();
                return samples.ToArray();
            }

            ShieldBurstSample[] standard = await PlayShieldCycle(false, 53001);
            TestAssert.True(standard.Length > 0 && standard[^1].Cue == PrologueAudioCue.ShieldReady,
                "live-setting test starts at a newly emitted ready burst");
            var liveReady = FindBurst(PrologueAudioCue.ShieldReady, standard[^1].Id);
            TestAssert.True(liveReady != null && !liveReady.IsQueuedForDeletion(),
                "ready burst is still live before changing the setting");
            const ulong controlId = 900005301;
            vfx.PresentTask10Cue(PrologueAudioCue.ClawTension, controlId, player.GlobalPosition, Vector2.Right);
            var clawControl = vfx.GetNode<PrologueVfxBurst>($"World/RopeTension/ClawTension{controlId}");
            float liveNormal = BurstCanvasAlpha(liveReady);
            flow.ApplySettings(new GameSettingsData { ReduceFlashes = true, GuidanceEnabled = false });
            float liveSoft = BurstCanvasAlpha(liveReady), controlSoft = BurstCanvasAlpha(clawControl);
            flow.ApplySettings(new GameSettingsData { ReduceFlashes = false, GuidanceEnabled = false });
            float liveRestored = BurstCanvasAlpha(liveReady), controlRestored = BurstCanvasAlpha(clawControl);
            TestAssert.True(FindBurst(PrologueAudioCue.ShieldReady, standard[^1].Id) == liveReady &&
                !liveReady.IsQueuedForDeletion() && !clawControl.IsQueuedForDeletion(),
                "setting toggles must not respawn or retire active effects");

            ShieldBurstSample[] reduced = await PlayShieldCycle(true, 53011);
            PrologueAudioCue[] sequence = [PrologueAudioCue.ShieldRaise, PrologueAudioCue.ShieldBlock,
                PrologueAudioCue.ShieldHeavyBreak, PrologueAudioCue.ShieldRaise,
                PrologueAudioCue.ShieldOverheat, PrologueAudioCue.ShieldReady];
            TestAssert.True(standard.Select(sample => sample.Cue).SequenceEqual(sequence) &&
                reduced.Select(sample => sample.Cue).SequenceEqual(sequence) &&
                standard.Select(sample => sample.Id).Distinct().Count() == sequence.Length &&
                reduced.Select(sample => sample.Id).Distinct().Count() == sequence.Length,
                "the real audio publisher emits the same exact ordered cue sequence once per transition");
            for (int index = 0; index < sequence.Length; index++)
            {
                ShieldBurstSample normal = standard[index], soft = reduced[index];
                TestAssert.True(normal.Visible && soft.Visible && normal.Seconds == soft.Seconds &&
                    normal.Rays == soft.Rays && normal.Radius == soft.Radius && normal.Color == soft.Color,
                    $"{normal.Cue} retains visible onset, color, geometry and lifetime in both settings");
            }
            GD.Print("SHIELD_FLASH_CANVAS_ALPHA standard=" + string.Join(',', standard.Select(s => s.Alpha)) +
                " reduced=" + string.Join(',', reduced.Select(s => s.Alpha)) +
                $" existing={liveNormal:R}->{liveSoft:R}->{liveRestored:R} claw={controlSoft:R},{controlRestored:R}");
            bool onsetReduced = standard.All(sample => Mathf.IsEqualApprox(sample.Alpha, 1f)) &&
                reduced.All(sample => sample.Alpha is >= .30f and <= .60f);
            bool toggleReduced = Mathf.IsEqualApprox(liveNormal, 1f) && liveSoft is >= .30f and <= .60f &&
                Mathf.IsEqualApprox(liveRestored, 1f) && Mathf.IsEqualApprox(controlSoft, 1f) &&
                Mathf.IsEqualApprox(controlRestored, 1f);
            TestAssert.True(onsetReduced && toggleReduced,
                $"SHIELD_FLASH_REDUCTION_MISSING: onset={onsetReduced} live_toggle={toggleReduced}");

            var finalReady = FindBurst(PrologueAudioCue.ShieldReady, reduced[^1].Id);
            TestAssert.True(finalReady != null, "reduced ready effect exists before natural expiry");
            await WaitFor(tree, () => !GodotObject.IsInstanceValid(finalReady), 90,
                "reduced ready effect naturally expires rather than sticking on the player");
            vfx.PresentTask10Cue(PrologueAudioCue.ShieldReady, reduced[^1].Id, player.GlobalPosition, Vector2.Right);
            TestAssert.True(FindBurst(PrologueAudioCue.ShieldReady, reduced[^1].Id) == null,
                "expired shield transition still deduplicates the same cue identity");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.L, false);
            audio.Task10CueEmitted -= ObserveCue;
            tree.Paused = false;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    private readonly record struct ShieldBurstSample(PrologueAudioCue Cue, ulong Id, bool Visible,
        float Alpha, float Seconds, int Rays, float Radius, Color Color);

    private static float BurstCanvasAlpha(PrologueVfxBurst burst) => burst.Modulate.A * burst.SelfModulate.A;

    private static string ShieldEffectRoot(PrologueAudioCue cue) => cue switch
    {
        PrologueAudioCue.ShieldRaise => "ShieldFan",
        PrologueAudioCue.ShieldBlock => "ShieldBlockFlash",
        PrologueAudioCue.ShieldHeavyBreak => "ShieldHeavyBreak",
        PrologueAudioCue.ShieldOverheat or PrologueAudioCue.ShieldReady => "ShieldOverheatVent",
        _ => null
    };

    public static async Task Task10ProductionPublishersReachDirectors(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        PrologueAudioDirector audio = level.GetNode<PrologueAudioDirector>("Audio");
        audio.PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "audio-production-publishers");
        tree.Root.AddChild(level);
        int recoveryRequests = 0;
        // Tail records contain strings only; no borrowed Godot objects or dictionary entries.
        var sectorTrace = new Queue<string>(32);
        long sectorTraceSequence = 0;
        int sectorTraceOverflow = 0;
        int sectorTraceFaults = 0;
        Action detachSectorTrace = null;
        void AppendSectorTrace(string record)
        {
            if (sectorTrace.Count == 32)
            {
                sectorTrace.Dequeue();
                sectorTraceOverflow++;
            }
            sectorTrace.Enqueue(record);
        }
        void SectorTraceFault(string source, Exception exception)
        {
            sectorTraceFaults++;
            AppendSectorTrace($"seq={++sectorTraceSequence} source={source} " +
                $"diagnostic-read-error={exception.GetType().Name}:{exception.Message}");
        }
        void OnRecoveryRequested(string reason)
        {
            recoveryRequests++;
            GD.Print($"TASK10_PRODUCTION_RECOVERY request={recoveryRequests} reason={reason}");
        }
        level.RecoveryRequested += OnRecoveryRequested;
        try
        {
            await Frames(tree, 2);
            CharacterBody2D player = level.Player;
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
            PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
            PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
            PlayerShieldController shield = player.GetNode<PlayerShieldController>("Shield");
            MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            Hurtbox hurtbox = player.GetNode<Hurtbox>("Hurtbox");
            PrologueVfxDirector vfx = level.GetNode<PrologueVfxDirector>("Vfx");
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            ProloguePauseMenu pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            flow.ApplySettings(new GameSettingsData { GuidanceEnabled = false });

            await Frames(tree, 1);
            // Keep the real supported spawn inside the authored world. A stale
            // viewport cursor may be outside its recovery margin; moving the
            // player to that cursor silently invalidates every later cue.
            Vector2 fixturePosition = player.GlobalPosition;
            Vector2 cursor = fixturePosition + Vector2.Up * 180f;
            Vector2 pointer = player.GetViewport().GetCanvasTransform() * cursor;
            ulong mouseSequence = input.MouseMotionSequence;
            using var motion = new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer };
            // Position is already viewport-local. Sending the same event through
            // global ParseInputEvent again applies the headless window stretch
            // a second time and overwrites the correctly observed local pointer.
            player.GetViewport().PushInput(motion, true);
            Input.FlushBufferedEvents();
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            GD.Print($"TASK10_POINTER_BOUNDARY observed={input.HasObservedMouseMotion} " +
                $"before={mouseSequence} after={input.MouseMotionSequence} requested={cursor} actual={input.Current.MouseWorld}");
            TestAssert.True(input.HasObservedMouseMotion && input.MouseMotionSequence == mouseSequence + 1 &&
                input.Current.MouseWorld.DistanceTo(cursor) < .01f,
                "real pointer event samples the dedicated fixture anchor above the supported spawn");
            player.Velocity = Vector2.Zero;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.TryGetMountedSector(world.CurrentSector, out PrologueSector owner),
                "production cue target has an actual mounted sector owner for residency");
            GrappleSurface2D surface = Surface("Task10ProductionSurface", owner.ToLocal(cursor),
                GrappleSurfaceMaterial.DedicatedAnchor);
            owner.AddChild(surface);
            TestAssert.True(surface.WorldAnchor.DistanceTo(cursor) < .01f,
                "sector-owned cue anchor retains the sampled world position");
            await Frames(tree, 2);
            audio.ClearStagedFrameAudio();

            await Tap(tree, InputActions.SelectGrappleMode);
            await Tap(tree, InputActions.SelectAttackMode);
            await Tap(tree, InputActions.Slot2);
            await Tap(tree, InputActions.PrimaryAction);
            await Tap(tree, InputActions.Slot1);
            TestAssert.True(energy.TrySpendHeavyGuard(100f),
                "real energy owner can establish the empty-energy rejection precondition");
            await Tap(tree, InputActions.PrimaryAction);
            energy.ResetFull();

            Input.ActionPress(InputActions.Shield);
            await Frames(tree, 2);
            Vector2 shieldDirection = (input.Current.MouseWorld - player.GlobalPosition).Normalized();
            TestAssert.True(shieldDirection.LengthSquared() > .9f,
                "real sampled mouse aim establishes a directional shield front");
            Hitbox normal = await AddHitbox(tree, level, "Task10NormalBlock", player,
                shieldDirection, DamageGuardClass.Normal, 41001UL, 1);
            GD.Print($"TASK10_PRODUCTION_CUE_BOUNDARY stage=normal-block flow={flow.State} " +
                $"pending={level.ResidentPreparationPending} physics={Engine.GetPhysicsFrames()} " +
                $"input_sequence={input.CurrentFrameSequence} input_enabled={input.IsPhysicsProcessing()} " +
                $"shield_enabled={shield.IsPhysicsProcessing()} active={shield.IsActive} " +
                $"held={input.Current.ShieldHeld} keyboard={input.Current.KeyboardActions} hp={health.CurrentHealth}");
            TestAssert.True(normal.TryConnect(hurtbox),
                "real normal Hitbox collision reaches the guarded production player");
            TestAssert.True(energy.TrySpendHeavyGuard(80f),
                "real energy owner establishes a below-heavy-cost active shield");
            Hitbox heavy = await AddHitbox(tree, level, "Task10HeavyBreak", player,
                shieldDirection, DamageGuardClass.Heavy, 41002UL, 3);
            TestAssert.True(heavy.TryConnect(hurtbox),
                "real heavy Hitbox collision reaches and breaks the production shield");
            Input.ActionRelease(InputActions.Shield);
            await Frames(tree, 2);

            health.ResetFull();
            energy.ResetFull();
            shield.ResetFull();
            Input.ActionPress(InputActions.Shield);
            await WaitFor(tree, () => audio.Task10CueHistory.Contains(PrologueAudioCue.ShieldOverheat),
                240, "real held shield reaches overheat");
            Input.ActionRelease(InputActions.Shield);
            await WaitFor(tree, () => audio.Task10CueHistory.Contains(PrologueAudioCue.ShieldReady),
                360, "real energy cooling exits overheat and reaches ready");

            energy.ResetFull();
            await Tap(tree, InputActions.SelectGrappleMode);
            TestAssert.True(recoveryRequests == 0 && player.GlobalPosition.DistanceTo(fixturePosition) < .01f,
                "production cue setup stays at its supported spawn without an unintended recovery");
            GD.Print($"TASK10_CLAW_INPUT_BOUNDARY flow={flow.State} pending={level.ResidentPreparationPending} " +
                $"input_sequence={input.CurrentFrameSequence} keyboard={input.Current.KeyboardActions} " +
                $"mode={modes.CurrentMode} allowed={modes.CanBeginGrapple} held={input.Current.ShieldHeld} " +
                $"position={player.GlobalPosition} mouse={input.Current.MouseWorld} anchor={surface.WorldAnchor} " +
                $"available={surface.IsAvailable} distance={player.GlobalPosition.DistanceTo(surface.WorldAnchor):R}");
            int castObservations = 0;
            (MechanicalClawState State, MechanicalClawFailure Failure, ulong Cast)? lastCast = null;
            void ObserveCast(MechanicalClawFrame frame, GrappleSurface2D target)
            {
                var key = (frame.State, frame.Failure, frame.CastId);
                if (lastCast == key) return;
                lastCast = key;
                if (++castObservations <= 24)
                    GD.Print($"TASK10_CLAW_CAST_BOUNDARY state={frame.State} failure={frame.Failure} " +
                        $"cast={frame.CastId} target={target?.Name} primary={input.Current.PrimaryPressed} " +
                        $"input_sequence={input.CurrentFrameSequence} anchor={frame.AnchorPoint} dock={frame.DockPoint}");
            }
            claw.ClawFrameChanged += ObserveCast;
            try
            {
                Input.ActionPress(InputActions.PrimaryAction);
                await WaitFor(tree, () => claw.CurrentFrame.State == MechanicalClawState.Latched,
                    180, "real InputMap claw reaches Latched");
            }
            finally
            {
                claw.ClawFrameChanged -= ObserveCast;
                GD.Print($"TASK10_CLAW_CAST_RETURN state={claw.CurrentFrame.State} " +
                    $"failure={claw.CurrentFrame.Failure} publications={castObservations} " +
                    $"position={player.GlobalPosition} mouse={input.Current.MouseWorld} anchor={surface.WorldAnchor}");
            }
            await WaitFor(tree, () => audio.Task10CueHistory.Contains(PrologueAudioCue.ClawTension),
                60, "real held claw enters its tension family");
            Input.ActionPress(InputActions.MoveUp);
            await WaitFor(tree, () => claw.CurrentFrame.State == MechanicalClawState.Reel,
                20, "the one-click cast automatically reels while W is held before the Space boost");
            int tensionBeforeBoost = audio.Task10CueHistory.Count(
                cue => cue == PrologueAudioCue.ClawTension);
            ulong boostCast = claw.CurrentFrame.CastId;
            var boostPublications = new List<MechanicalClawFrame>();
            bool boostOverflow = false;
            void ObserveBoost(MechanicalClawFrame frame, GrappleSurface2D target)
            {
                if (frame.State is not (MechanicalClawState.Boost or MechanicalClawState.Released)) return;
                if (boostPublications.Count < 16) boostPublications.Add(frame);
                else boostOverflow = true;
            }
            claw.ClawFrameChanged += ObserveBoost;
            try
            {
                Input.ActionPress(InputActions.Jump);
                await WaitFor(tree, () => boostPublications.Count >= 2,
                    20, "real attached Space publishes its accepted boost and immediate release");
            }
            finally
            {
                claw.ClawFrameChanged -= ObserveBoost;
                GD.Print("TASK10_BOOST_PUBLICATIONS states=" + string.Join(',',
                    boostPublications.Select(frame => $"{frame.State}:{frame.CastId}:{frame.BoostConsumed}:{frame.ReleaseKind}")) +
                    $" overflow={boostOverflow} final={claw.CurrentFrame.State}");
            }
            TestAssert.True(!boostOverflow && boostCast != 0UL && boostPublications.Count == 2 &&
                boostPublications[0].State == MechanicalClawState.Boost &&
                boostPublications[1].State == MechanicalClawState.Released &&
                boostPublications.All(frame => frame.CastId == boostCast && frame.BoostConsumed &&
                    frame.ReleaseKind == MechanicalClawReleaseKind.Boost && frame.Failure == MechanicalClawFailure.None),
                "the audible impulse belongs to one accepted same-cast atomic Boost-to-Released transaction");
            await Frames(tree, 1);
            TestAssert.Equal(tensionBeforeBoost + 1, audio.Task10CueHistory.Count(
                    cue => cue == PrologueAudioCue.ClawTension),
                "real Space boost reaches the audio publisher after reeling");
            Input.ActionRelease(InputActions.Jump);
            await Frames(tree, 2);
            await Tap(tree, InputActions.Jump);
            TestAssert.Equal(tensionBeforeBoost + 1, audio.Task10CueHistory.Count(
                    cue => cue == PrologueAudioCue.ClawTension),
                "a rejected second airborne boost does not replay the onset");
            Input.ActionRelease(InputActions.MoveUp);
            Input.ActionRelease(InputActions.PrimaryAction);
            await WaitFor(tree, () => claw.CurrentFrame.State == MechanicalClawState.Idle,
                180, "the accepted boost release retracts to Idle without another cast or cancel");

            surface.Material = GrappleSurfaceMaterial.Ice;
            Input.ActionPress(InputActions.PrimaryAction);
            await WaitFor(tree, () => audio.Task10CueHistory.Contains(PrologueAudioCue.ClawIceSlip),
                60, "real ice material publishes the typed claw slip");
            Input.ActionRelease(InputActions.PrimaryAction);
            await Frames(tree, 3);
            surface.Material = GrappleSurfaceMaterial.Forbidden;
            Input.ActionPress(InputActions.PrimaryAction);
            await WaitFor(tree, () => audio.Task10CueHistory.Contains(PrologueAudioCue.ClawFail),
                60, "real forbidden material publishes the generic typed claw failure");
            Input.ActionRelease(InputActions.PrimaryAction);
            await Frames(tree, 3);

            PrologueAudioCue[] exactTask10Cues =
            [
                PrologueAudioCue.ModeAttack, PrologueAudioCue.ModeGrapple,
                PrologueAudioCue.EmptySlot, PrologueAudioCue.EnergyEmpty,
                PrologueAudioCue.ShieldRaise, PrologueAudioCue.ShieldBlock,
                PrologueAudioCue.ShieldHeavyBreak, PrologueAudioCue.ShieldOverheat,
                PrologueAudioCue.ShieldReady, PrologueAudioCue.ClawFire,
                PrologueAudioCue.ClawTravel, PrologueAudioCue.ClawLatch,
                PrologueAudioCue.ClawTension, PrologueAudioCue.ClawRelease,
                PrologueAudioCue.ClawIceSlip, PrologueAudioCue.ClawFail
            ];
            foreach (PrologueAudioCue cue in exactTask10Cues)
                TestAssert.True(audio.Task10CueHistory.Contains(cue),
                    $"real production publishers reach exact Task10 cue {cue}");
            TestAssert.Equal(16, audio.Task10CueHistory.Distinct().Count(),
                "production runtime publishes the exact sixteen typed cue identities");
            TestAssert.Equal(2, audio.Task10CueHistory.Count(cue => cue == PrologueAudioCue.ClawTension),
                "reeling and the accepted boost each sound once without polling duplicates");
            TestAssert.Equal(1, audio.Task10CueHistory.Count(cue => cue == PrologueAudioCue.ClawRelease),
                "Released/Retract polling cannot spam the one claw-release entry cue");
            TestAssert.True(vfx.ActiveTask10EffectCount > 0,
                "the real typed runtime transitions reach replaceable production VFX roots");

            await EnterOppositeMode(tree, modes);
            pause.Open();
            TestAssert.Equal(0, audio.ActiveTask10TransitionCount,
                "pause clears production typed audio transition state");
            TestAssert.Equal(0, vfx.ActiveTask10EffectCount,
                "pause clears production typed VFX state");
            pause.Execute(ProloguePauseCommand.Resume);

            health.ResetFull();
            await EnterOppositeMode(tree, modes);
            TestAssert.Equal(0, recoveryRequests,
                "the production cue sequence has no recovery before its explicit death contract");
            TestAssert.True(health.ApplyDamage(new DamageRequest(
                    health.CurrentHealth, Vector2.Down, 0f, 42001UL)),
                "real player death starts the production recovery edge");
            await Frames(tree, 3);
            TestAssert.Equal(0, audio.ActiveTask10TransitionCount,
                "death/recovery completion clears production typed audio state");
            TestAssert.Equal(0, vfx.ActiveTask10EffectCount,
                "death/recovery completion clears production typed VFX state");

            // Arm here, not during the initial sixteen-cue fixture. Observer ordering
            // is reported as observed, not as entry into a production publisher.
            string sectorStage = "before-enter-opposite-mode";
            int audioClearSequence = 0;
            bool sectorTraceArmed = true;
            void RecordSectorTrace(string source, string detail = "")
            {
                if (!sectorTraceArmed) return;
                long sequence = ++sectorTraceSequence;
                try
                {
                    AppendSectorTrace($"seq={sequence} stage={sectorStage} source={source} " +
                        $"physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} " +
                        $"sector={level.CurrentSector} mode={modes.CurrentMode} " +
                        $"vfxClearGeneration={vfx.ThrusterClearGeneration} audioClearSeq={audioClearSequence} " +
                        $"task10Count={vfx.ActiveTask10EffectCount} {detail} " +
                        Task10SectorEffectScalars(vfx));
                }
                catch (Exception exception)
                {
                    sectorTraceFaults++;
                    AppendSectorTrace($"seq={sequence} stage={sectorStage} source={source} " +
                        $"diagnostic-read-error={exception.GetType().Name}:{exception.Message}");
                }
            }
            void OnSectorAudioClear()
            {
                audioClearSequence++;
                RecordSectorTrace("audio.Task10TransitionsCleared-observed");
            }
            void OnSectorCue(PrologueAudioCue cue, ulong transitionId, Vector2 position) =>
                RecordSectorTrace("audio.Task10CueEmitted-observed",
                    $"cue={cue} transitionId={transitionId} position=({position.X:R},{position.Y:R})");
            void OnSectorClaw(MechanicalClawFrame frame, GrappleSurface2D target) =>
                RecordSectorTrace("claw.ClawFrameChanged-observed",
                    $"castId={frame.CastId} state={frame.State} failure={frame.Failure} " +
                    $"boostConsumed={frame.BoostConsumed} releaseKind={frame.ReleaseKind}");
            void OnSectorNodeAdded(Node node)
            {
                if (!sectorTraceArmed) return;
                try
                {
                    if (node is PrologueVfxBurst burst && GodotObject.IsInstanceValid(burst) &&
                        GodotObject.IsInstanceValid(vfx) && vfx.IsAncestorOf(burst))
                        // PresentTask10Cue adds the node before assigning its key;
                        // other burst producers may never assign a Task10 key at all.
                        RecordSectorTrace("tree.NodeAdded-vfx-burst-observed",
                            "phase=node-added-not-task10-registration-proof " + Task10SectorBurstScalars(burst));
                }
                catch (Exception exception) { SectorTraceFault("tree.NodeAdded", exception); }
            }
            // Each unsubscribe is isolated so a diagnostic failure cannot skip
            // other detachments or replace the original assertion/cleanup path.
            void DetachSectorObserver(Action detach, string source)
            {
                try { detach(); }
                catch (Exception exception) { SectorTraceFault(source, exception); }
            }
            detachSectorTrace = () =>
            {
                sectorTraceArmed = false;
                DetachSectorObserver(() => audio.Task10CueEmitted -= OnSectorCue, "detach-audio-cue");
                DetachSectorObserver(() => audio.Task10TransitionsCleared -= OnSectorAudioClear, "detach-audio-clear");
                DetachSectorObserver(() => claw.ClawFrameChanged -= OnSectorClaw, "detach-claw");
                DetachSectorObserver(() => tree.NodeAdded -= OnSectorNodeAdded, "detach-node-added");
            };
            audio.Task10CueEmitted += OnSectorCue;
            audio.Task10TransitionsCleared += OnSectorAudioClear;
            claw.ClawFrameChanged += OnSectorClaw;
            tree.NodeAdded += OnSectorNodeAdded;
            RecordSectorTrace("test-stage");
            await EnterOppositeMode(tree, modes);
            sectorStage = "after-enter-opposite-mode";
            RecordSectorTrace("test-stage");
            PrologueSectorId beforeSector = level.CurrentSector!.Value;
            player.GlobalPosition = new Vector2(1500f, 1600f);
            sectorStage = "after-teleport";
            RecordSectorTrace("test-stage");
            await WaitFor(tree, () => level.CurrentSector != beforeSector, 60,
                "real continuous world enters a different sector");
            sectorStage = "after-wait-for-current-sector";
            RecordSectorTrace("test-stage");
            TestAssert.Equal(0, audio.ActiveTask10TransitionCount,
                "sector transition/unload clears production typed audio state");
            sectorStage = "before-vfx-assert";
            RecordSectorTrace("test-stage");
            TestAssert.Equal(0, vfx.ActiveTask10EffectCount,
                "sector transition/unload clears production typed VFX state");
            sectorTraceArmed = false;

            await EnterOppositeMode(tree, modes);
            flow.Complete();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(0, audio.ActiveTask10TransitionCount,
                "completion clears production typed audio state");
            TestAssert.Equal(0, vfx.ActiveTask10EffectCount,
                "completion clears production typed VFX state");
        }
        finally
        {
            detachSectorTrace?.Invoke();
            GD.Print($"TASK10_SECTOR_ORDER_SUMMARY retained={sectorTrace.Count} " +
                $"observations={sectorTraceSequence} overflow={sectorTraceOverflow} faults={sectorTraceFaults} " +
                "capacity=32 keysPerSnapshot=8 countsAreDictionaryEntries=true");
            foreach (string record in sectorTrace)
                GD.Print($"TASK10_SECTOR_ORDER {record}");
            level.RecoveryRequested -= OnRecoveryRequested;
            foreach (StringName action in new[]
            {
                InputActions.SelectAttackMode, InputActions.SelectGrappleMode, InputActions.Slot1,
                InputActions.Slot2, InputActions.PrimaryAction, InputActions.Shield,
                InputActions.MoveUp, InputActions.Jump
            }) Input.ActionRelease(action);
            tree.Paused = false;
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static string Task10SectorEffectScalars(PrologueVfxDirector vfx)
    {
        var field = typeof(PrologueVfxDirector).GetField("_task10Effects",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("diagnostic field _task10Effects unavailable");
        var effects = field.GetValue(vfx) as
            Dictionary<(PrologueAudioCue Cue, ulong TransitionId, int EventKind), PrologueVfxBurst>
            ?? throw new InvalidOperationException("diagnostic _task10Effects exact dictionary type mismatch");
        var scalars = new List<string>(8);
        foreach (var entry in effects)
        {
            scalars.Add($"key=({entry.Key.Cue},{entry.Key.TransitionId},{entry.Key.EventKind}) " +
                Task10SectorBurstScalars(entry.Value));
            if (scalars.Count == 8) break;
        }
        return $"dictionaryCount={effects.Count} keysClipped={Math.Max(0, effects.Count - scalars.Count)} " +
            $"keys=[{string.Join("; ", scalars)}]";
    }

    private static string Task10SectorBurstScalars(PrologueVfxBurst burst)
    {
        if (!GodotObject.IsInstanceValid(burst)) return "burstValid=false";
        return $"burstValid=true burstId={burst.GetInstanceId()} burstName={burst.Name} " +
            $"insideTree={burst.IsInsideTree()} queued={burst.IsQueuedForDeletion()}";
    }

    private static async Task Tap(SceneTree tree, StringName action)
    {
        Input.ActionPress(action);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        Input.ActionRelease(action);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> condition, int frames, string message)
    {
        for (int index = 0; index < frames && !condition(); index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(condition(), message);
    }

    private static async Task EnterOppositeMode(SceneTree tree, PlayerActionModeController modes) =>
        await Tap(tree, modes.CurrentMode == PlayerActionMode.Attack
            ? InputActions.SelectGrappleMode : InputActions.SelectAttackMode);

    private static GrappleSurface2D Surface(string name, Vector2 position,
        GrappleSurfaceMaterial material)
    {
        var surface = new GrappleSurface2D
        {
            Name = name, Position = position, Material = material,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None, Monitoring = true, Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 10f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static async Task<Hitbox> AddHitbox(SceneTree tree, Node level, string name,
        Node2D player, Vector2 shieldDirection, DamageGuardClass guardClass, ulong attackId,
        int damage)
    {
        var source = new Node2D
        {
            Name = name + "Source", GlobalPosition = player.GlobalPosition + shieldDirection * 40f
        };
        var hitbox = new Hitbox
        {
            Name = name, Damage = damage, GuardClass = guardClass, Knockback = 100f
        };
        source.AddChild(hitbox);
        level.AddChild(source);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        hitbox.Begin(attackId);
        return hitbox;
    }

    public static async Task Run(SceneTree tree)
    {
        var fixture = new Node2D { Name = "AudioRuntimeFixture" };
        var body = new CharacterBody2D { Name = "GameplayBody", Position = new Vector2(180, 240) };
        fixture.AddChild(body);
        PrologueAudioDirector director = PrologueAudioDirector.CreateRuntimeTree();
        fixture.AddChild(director);
        tree.Root.AddChild(fixture);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        Task10TransitionsAreEntryOnly(director);
        UnallocatedClawFailureDedupEndsAtAttemptBoundary(director);
        AcceptedClawBoostHasAnOnsetAfterReeling(director);

        TestAssert.True(director.GetNodeOrNull<AudioStreamPlayer>("MusicA") != null,
            "audio director mounts the primary music player");
        TestAssert.True(director.GetNodeOrNull<AudioStreamPlayer>("MusicB") != null,
            "audio director mounts the layered music player");
        TestAssert.True(director.GetNodeOrNull<AudioStreamPlayer>("Ambience") != null,
            "audio director mounts room ambience");
        TestAssert.True(director.GetNodeOrNull<AudioStreamPlayer>("Sfx0") != null &&
                        director.GetNodeOrNull<AudioStreamPlayer>("Sfx3") != null,
            "audio director mounts a four-voice SFX pool");
        TestAssert.True(director.GetNodeOrNull<AudioStreamPlayer>("UI") != null,
            "audio director mounts a dedicated UI player");
        int masterBus = AudioServer.GetBusIndex("Master");
        TestAssert.True(AudioServer.GetBusEffectCount(masterBus) > 0 &&
                        AudioServer.GetBusEffect(masterBus, 0) is AudioEffectHardLimiter,
            "Master bus installs the production safety limiter");
        var limiter = (AudioEffectHardLimiter)AudioServer.GetBusEffect(masterBus, 0);
        TestAssert.True(limiter.CeilingDb <= -1f,
            "Master limiter retains at least one decibel of output headroom");

        Vector2 before = body.Position;
        director.Play(PrologueAudioCue.PlayerAttack, body.GlobalPosition);
        string first = director.LastPlayedPath;
        director.Play(PrologueAudioCue.PlayerAttack, body.GlobalPosition);
        TestAssert.True(first != director.LastPlayedPath,
            "multi-variant cues avoid immediate repetition");
        TestAssert.Equal(before, body.Position, "audio playback cannot mutate gameplay bodies");

        director.SetMusicState(PrologueMusicState.BossPhaseTwo);
        TestAssert.Equal(PrologueMusicState.BossPhaseTwo, director.MusicState,
            "audio director enters the layered phase-two Boss state");
        TestAssert.True(director.ActiveMusicLayerCount == 2,
            "Boss phase two enables pulse and bell music layers");

        director.ApplySettings(new GameSettingsData
        {
            MasterVolume = .5f, MusicVolume = .4f, EffectsVolume = .3f, AmbienceVolume = .2f, UiVolume = .1f
        });
        TestAssert.Near(Mathf.LinearToDb(.5f), AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master")), .01f,
            "master setting controls the Master audio bus");
        TestAssert.Near(Mathf.LinearToDb(.3f), AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("SFX")), .01f,
            "effects setting controls the SFX audio bus");

        director.SetPaused(true);
        TestAssert.True(director.AudioPaused, "audio director exposes paused state");
        director.SetPaused(false);
        TestAssert.True(!director.AudioPaused, "audio director restores playback state");

        PrologueAudioDirector.PlayPresentationCueFromTree(director, PresentationCue.ThreadHit, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ThreadHit, director.LastCue,
            "presentation cues reach the audio director through the shared tree hook");

        int beforeAttackFrames = director.FramePlaybackCount;
        AttackPresentationFrame rustTelegraph = Attack(41, EnemyAttackKind.RustShellThrust,
            AttackPhase.Telegraph);
        director.PlayAttackFrame(body, rustTelegraph, body.GlobalPosition);
        director.PlayAttackFrame(body, rustTelegraph, body.GlobalPosition);
        TestAssert.Equal(beforeAttackFrames + 1, director.FramePlaybackCount,
            "repeated AttackId and Phase play exactly one staged cue");
        TestAssert.Equal(PrologueAudioCue.RustShellTelegraph, director.LastCue,
            "rust-shell Telegraph maps to its actor-specific metal warning");
        TestAssert.True(director.LastFrameDeduplicated,
            "audio director reports the repeated attack phase as deduplicated");

        director.PlayAttackFrame(body, Attack(41, EnemyAttackKind.RustShellThrust, AttackPhase.Active),
            body.GlobalPosition);
        TestAssert.Equal(beforeAttackFrames + 2, director.FramePlaybackCount,
            "a new phase for the same AttackId plays its one onset family");
        TestAssert.Equal(PrologueAudioCue.RustShellAttack, director.LastCue,
            "rust-shell Active maps to the needle strike onset");

        var sourceA = new Node2D { Name = "LocalAttackSourceA" };
        var sourceB = new Node2D { Name = "LocalAttackSourceB" };
        fixture.AddChild(sourceA);
        fixture.AddChild(sourceB);
        int beforeCrossSource = director.FramePlaybackCount;
        director.PlayAttackFrame(sourceA,
            Attack(88, EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph), sourceA.GlobalPosition);
        director.PlayAttackFrame(sourceB,
            Attack(88, EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph), sourceB.GlobalPosition);
        TestAssert.Equal(beforeCrossSource + 2, director.FramePlaybackCount,
            "distinct live sources with the same local AttackId and phase each play exactly once");

        director.PlayAttackFrame(sourceA,
            Attack(89, EnemyAttackKind.RustShellThrust, AttackPhase.Active), sourceA.GlobalPosition);
        var playerCoordinator = new Node2D { Name = "UnrelatedPlayerCoordinator" };
        fixture.AddChild(playerCoordinator);
        PrologueAudioDirector.PlayPresentationCueFromTree(playerCoordinator,
            PresentationCue.AttackStarted, playerCoordinator.GlobalPosition);
        TestAssert.Equal(PrologueAudioCue.PlayerAttack, director.LastCue,
            "typed enemy Active never suppresses an unrelated player's AttackStarted cue");

        PrologueAudioCue[] bossTelegraphs =
        [
            PrologueAudioCue.BossSweepTelegraph,
            PrologueAudioCue.BossLeapTelegraph,
            PrologueAudioCue.BossGroundWaveTelegraph,
            PrologueAudioCue.BossHammerTelegraph
        ];
        EnemyAttackKind[] bossKinds =
        [
            EnemyAttackKind.BossSweep, EnemyAttackKind.BossLeapSlam,
            EnemyAttackKind.BossGroundWave, EnemyAttackKind.BossHammerThrow
        ];
        for (int index = 0; index < bossKinds.Length; index++)
        {
            director.PlayAttackFrame(body, Attack((ulong)(50 + index), bossKinds[index], AttackPhase.Telegraph),
                body.GlobalPosition);
            TestAssert.Equal(bossTelegraphs[index], director.LastCue,
                $"{bossKinds[index]} has a recognizable typed telegraph onset");
        }

        int beforeThreadFrames = director.FramePlaybackCount;
        ThreadCastFrame preview = Thread(0, ThreadCastState.Preview);
        director.PlayThreadFrame(preview, body.GlobalPosition);
        director.PlayThreadFrame(preview, body.GlobalPosition);
        TestAssert.Equal(beforeThreadFrames + 1, director.FramePlaybackCount,
            "repeated CastId and State play exactly one thread layer");
        TestAssert.Equal(PrologueAudioCue.ThreadPreview, director.LastCue,
            "thread Preview maps to the spool-selection layer");
        ThreadCastState[] stagedStates =
        [
            ThreadCastState.Windup, ThreadCastState.Travel, ThreadCastState.Latched,
            ThreadCastState.Apply, ThreadCastState.Recovery, ThreadCastState.Failed
        ];
        PrologueAudioCue[] stagedCues =
        [
            PrologueAudioCue.ThreadWindup, PrologueAudioCue.ThreadTravel, PrologueAudioCue.ThreadLatch,
            PrologueAudioCue.ThreadTension, PrologueAudioCue.ThreadHit, PrologueAudioCue.ThreadFail
        ];
        for (int index = 0; index < stagedStates.Length; index++)
        {
            director.PlayThreadFrame(Thread(77, stagedStates[index]), body.GlobalPosition);
            TestAssert.Equal(stagedCues[index], director.LastCue,
                $"thread {stagedStates[index]} maps to its audible cast layer");
        }
        TestAssert.True(director.LastFrameAudit.Contains("CastId=77") &&
                        director.LastFrameAudit.Contains("dedupe=False") &&
                        director.LastFrameAudit.Contains("res://assets/audio/prologue/"),
            "thread proof audit names ID, dedupe result, and exact file without normal-log flooding");

        PackedScene levelScene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        Node level = levelScene.Instantiate();
        TestAssert.True(level.GetNodeOrNull<PrologueAudioDirector>("Audio") != null,
            "generated prologue mounts the formal audio director");
        level.Free();

        fixture.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void AcceptedClawBoostHasAnOnsetAfterReeling(PrologueAudioDirector director)
    {
        director.ClearTask10Transitions();
        int before = director.Task10TransitionCueCount;
        int historyBefore = director.Task10CueHistory.Count;
        MechanicalClawFrame attached = MechanicalClawFrame.Idle() with
        {
            CastId = 9101, State = MechanicalClawState.Reel, EffectiveVisibility = true
        };
        director.PresentClawFrame(attached, Vector2.Zero);
        director.PresentClawFrame(attached with { State = MechanicalClawState.Swing }, Vector2.Zero);
        director.PresentClawFrame(attached, Vector2.Zero);
        TestAssert.Equal(before + 1, director.Task10TransitionCueCount,
            "continuous Reel/Swing changes retain one tension onset");

        MechanicalClawFrame boosted = attached with
        {
            State = MechanicalClawState.Boost, BoostConsumed = true
        };
        director.PresentClawFrame(boosted, Vector2.Zero);
        TestAssert.Equal(before + 2, director.Task10TransitionCueCount,
            "accepted Space boost must remain audible after the reel tension onset");
        director.PresentClawFrame(boosted with { State = MechanicalClawState.Released }, Vector2.Zero);
        TestAssert.Equal(before + 3, director.Task10TransitionCueCount,
            "the atomic Space transaction emits one release onset after its accepted boost onset");
        director.PresentClawFrame(boosted, Vector2.Zero);
        director.PresentClawFrame(boosted with { State = MechanicalClawState.Retract }, Vector2.Zero);
        TestAssert.Equal(before + 3, director.Task10TransitionCueCount,
            "repeated synthetic Boost and following Retract cannot replay either atomic onset");
        PrologueAudioCue[] atomic = director.Task10CueHistory
            .Skip(historyBefore).ToArray();
        TestAssert.True(atomic.SequenceEqual(new[]
            {
                PrologueAudioCue.ClawTension,
                PrologueAudioCue.ClawTension,
                PrologueAudioCue.ClawRelease
            }),
            "one cast publishes tension, distinct accepted Boost, then release exactly once and in order");
        director.ClearTask10Transitions();
    }

    private static void UnallocatedClawFailureDedupEndsAtAttemptBoundary(
        PrologueAudioDirector director)
    {
        director.ClearTask10Transitions();
        int before = director.Task10TransitionCueCount;
        int historyBefore = director.Task10CueHistory.Count;
        MechanicalClawFrame failed = MechanicalClawFrame.Idle() with
        {
            State = MechanicalClawState.Failed,
            Failure = MechanicalClawFailure.IceSlip
        };

        director.PresentClawFrame(failed, Vector2.Zero);
        director.PresentClawFrame(failed, Vector2.Zero);
        TestAssert.Equal(before + 1, director.Task10TransitionCueCount,
            "duplicate samples from one unallocated failed attempt emit one rejection onset");

        director.PresentClawFrame(MechanicalClawFrame.Idle(), Vector2.Zero);
        director.PresentClawFrame(MechanicalClawFrame.Idle() with
        {
            State = MechanicalClawState.Aim
        }, Vector2.Zero);
        director.PresentClawFrame(failed, Vector2.Zero);
        director.PresentClawFrame(failed, Vector2.Zero);
        TestAssert.Equal(before + 2, director.Task10TransitionCueCount,
            "the next identical CastId-zero rejection after Idle/new Aim emits one fresh onset");
        PrologueAudioCue[] attempts = director.Task10CueHistory.Skip(historyBefore).ToArray();
        TestAssert.True(attempts.SequenceEqual(new[]
            {
                PrologueAudioCue.ClawIceSlip,
                PrologueAudioCue.ClawIceSlip
            }),
            "two separate unallocated ice-slip attempts each publish their mapped feedback once");
        director.ClearTask10Transitions();
    }

    public static void Task10TransitionsAreEntryOnly(PrologueAudioDirector director)
    {
        int before = director.Task10TransitionCueCount;
        int historyBefore = director.Task10CueHistory.Count;
        var attack = new PlayerHudActionState(PlayerActionMode.Attack, 1, 1f, 0f, false,
            PlayerActionRejectionReason.None);
        director.PresentActionFrame(attack, Vector2.Zero);
        director.PresentActionFrame(attack, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ModeAttack, director.LastCue,
            "Task10 attack mode emits its exact cue once on initial entry");
        var grapple = new PlayerHudActionState(PlayerActionMode.Grapple, 1, 1f, 0f, false,
            PlayerActionRejectionReason.None);
        director.PresentActionFrame(grapple, Vector2.Zero);
        director.PresentActionFrame(grapple, Vector2.Zero);
        TestAssert.Equal(before + 2, director.Task10TransitionCueCount,
            "Task10 mode cue plays once on grapple entry and never from repeated frames");
        TestAssert.Equal(PrologueAudioCue.ModeGrapple, director.LastCue,
            "Task10 grapple entry maps to the exact typed cue");

        director.PresentActionFrame(grapple with { ActiveSlot = 2,
            Rejection = PlayerActionRejectionReason.EmptySlot }, Vector2.Zero);
        director.PresentActionFrame(grapple with { ActiveSlot = 2,
            Rejection = PlayerActionRejectionReason.EmptySlot }, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.EmptySlot, director.LastCue,
            "Task10 selected empty slot emits its exact rejection cue once");
        director.PresentActionFrame(grapple with
        {
            ActiveSlot = 1, Rejection = PlayerActionRejectionReason.InsufficientEnergy
        }, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.EnergyEmpty, director.LastCue,
            "Task10 insufficient energy emits its exact rejection cue once");

        PlayerEnergyFrame ready = new(new PlayerEnergyState(100f, 0f, 0f, 0f, false), false);
        PlayerEnergyFrame hot = new(new PlayerEnergyState(0f, 100f, 0f, 0f, true), false);
        director.PresentEnergyFrame(ready, Vector2.Zero);
        director.PresentEnergyFrame(hot, Vector2.Zero);
        director.PresentEnergyFrame(hot, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ShieldOverheat, director.LastCue,
            "Task10 overheat emits once on real energy-frame state entry");
        director.PresentEnergyFrame(ready, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ShieldReady, director.LastCue,
            "Task10 ready emits once when a real energy frame exits overheat");

        director.PresentShieldActive(true, Vector2.Zero);
        director.PresentShieldActive(true, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ShieldRaise, director.LastCue,
            "Task10 shield raise emits once on active-state entry");
        PlayerShieldResult block = new(true, 0, 0f, false, false);
        PlayerShieldResult heavyBreak = new(false, 1, 1f, false, true);
        director.PresentShieldResult(block, 7001, Vector2.Zero);
        director.PresentShieldResult(block, 7001, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ShieldBlock, director.LastCue,
            "Task10 normal block emits once for its real immutable shield result");
        director.PresentShieldResult(heavyBreak, 7002, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ShieldHeavyBreak, director.LastCue,
            "Task10 heavy break emits once for its real immutable shield result");

        MechanicalClawFrame claw = MechanicalClawFrame.Idle() with
        {
            CastId = 91, State = MechanicalClawState.Windup, EffectiveVisibility = true
        };
        MechanicalClawState[] states =
        [
            MechanicalClawState.Windup, MechanicalClawState.Travel, MechanicalClawState.Latched,
            MechanicalClawState.Reel, MechanicalClawState.Released
        ];
        PrologueAudioCue[] clawCues =
        [
            PrologueAudioCue.ClawFire, PrologueAudioCue.ClawTravel, PrologueAudioCue.ClawLatch,
            PrologueAudioCue.ClawTension, PrologueAudioCue.ClawRelease
        ];
        for (int index = 0; index < states.Length; index++)
        {
            MechanicalClawFrame entered = claw with { State = states[index] };
            director.PresentClawFrame(entered, Vector2.Zero);
            director.PresentClawFrame(entered, Vector2.Zero);
            TestAssert.Equal(clawCues[index], director.LastCue,
                $"Task10 real claw {states[index]} entry emits exact cue once");
        }
        MechanicalClawFrame travel = claw with
        {
            CastId = 91, State = MechanicalClawState.Travel, EffectiveVisibility = true
        };
        director.PresentClawFrame(travel with
        {
            State = MechanicalClawState.Failed, Failure = MechanicalClawFailure.IceSlip
        }, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ClawIceSlip, director.LastCue,
            "Task10 ice failure uses its independent audible cue");
        director.PresentClawFrame(travel with
        {
            CastId = 92, State = MechanicalClawState.Failed, Failure = MechanicalClawFailure.Obstructed
        }, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ThreadFail, director.LastCue,
            "Task4 blocked claw failure uses its distinct blocked-spark audible cue");
        director.PresentClawFrame(travel with
        {
            CastId = 93, State = MechanicalClawState.Failed, Failure = MechanicalClawFailure.Forbidden
        }, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ClawFail, director.LastCue,
            "Task4 forbidden claw failure keeps its dry-impact audible cue");
        director.PresentClawFrame(travel with
        {
            CastId = 94, State = MechanicalClawState.Failed, Failure = MechanicalClawFailure.OutOfRange
        }, Vector2.Zero);
        TestAssert.Equal(PrologueAudioCue.ThreadPreview, director.LastCue,
            "Task4 range failure uses its no-impact pulse audible cue");

        var observed = new System.Collections.Generic.HashSet<PrologueAudioCue>();
        for (int index = historyBefore; index < director.Task10CueHistory.Count; index++)
            observed.Add(director.Task10CueHistory[index]);
        PrologueAudioCue[] exact =
        [
            PrologueAudioCue.ModeAttack, PrologueAudioCue.ModeGrapple, PrologueAudioCue.EmptySlot,
            PrologueAudioCue.EnergyEmpty, PrologueAudioCue.ShieldRaise, PrologueAudioCue.ShieldBlock,
            PrologueAudioCue.ShieldHeavyBreak, PrologueAudioCue.ShieldOverheat, PrologueAudioCue.ShieldReady,
            PrologueAudioCue.ClawFire, PrologueAudioCue.ClawTravel, PrologueAudioCue.ClawLatch,
            PrologueAudioCue.ClawTension, PrologueAudioCue.ClawRelease, PrologueAudioCue.ClawIceSlip,
            PrologueAudioCue.ClawFail, PrologueAudioCue.ThreadFail, PrologueAudioCue.ThreadPreview
        ];
        foreach (PrologueAudioCue cue in exact)
            TestAssert.True(observed.Contains(cue), $"Task10 runtime transition coverage reaches exact cue {cue}");
        TestAssert.Equal(before + exact.Length, director.Task10TransitionCueCount,
            "Task4 exact transition matrix emits eighteen entry cues with no polling duplicates");
        director.ClearTask10Transitions();
        TestAssert.Equal(0, director.ActiveTask10TransitionCount,
            "Task10 death/pause/unload/completion cleanup clears transition identities");
    }

    private static AttackPresentationFrame Attack(ulong attackId, EnemyAttackKind kind, AttackPhase phase) =>
        new AttackPresentationFrame(
        new AttackTimelineFrame(attackId, kind, phase, .5f, .4f, new Vector2(160, 220), true),
        new AttackShapeFrame(AttackShapeKind.Circle, new Vector2(160, 220), new Vector2(24, 24), 0f,
            System.Array.Empty<Vector2>())).Validate();

    private static ThreadCastFrame Thread(ulong castId, ThreadCastState state) => new(
        castId, state, ThreadAimMode.Hold, Vector2.Right, 91, true,
        state == ThreadCastState.Failed ? ThreadFailureReason.Obstructed : ThreadFailureReason.None,
        .2f, .1f);
}
