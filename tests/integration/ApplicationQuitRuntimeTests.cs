using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class ApplicationQuitRuntimeTests
{
    public static async Task RunReviewGuard(SceneTree tree, string mode)
    {
        if (mode == "guard-fault") await FaultedRetiredOwner(tree);
        else if (mode == "guard-recovery") await RecoveryAfterCommit(tree);
        else if (mode == "guard-persistence") await DirectPersistenceAfterCommit(tree);
        else throw new ArgumentException("Unknown quit review guard", nameof(mode));
    }

    private static async Task<(PrologueLevelController Level, PrologueGameFlowController Flow,
        IsolatedPrologueFixturePaths Paths)> PlayingLevel(SceneTree tree, string name)
    {
        tree.Paused = false;
        ThrusterPlayerRuntimeTests.Release(); Input.ActionRelease("ui_accept");
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level, name);
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = flow.AutomationAutoStart = false;
        tree.Root.AddChild(level); tree.CurrentScene = level;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        flow.StartNew();
        for (int frame = 0; frame < 180 && flow.State != PrologueGameFlowState.Playing; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "review guard uses a real prepared full session");
        return (level, flow, paths);
    }

    private static bool GameplayDisabled(PrologueLevelController level) => !level.Player.IsPhysicsProcessing() &&
        new[] { "Input", "EnvironmentInteraction", "Motor", "Combat", "MechanicalClaw", "ActionModes", "Shield", "Energy" }
            .All(path => !level.Player.GetNode<Node>(path).IsPhysicsProcessing());

    private static async Task FaultedRetiredOwner(SceneTree tree)
    {
        var (level, flow, _) = await PlayingLevel(tree, "quit-fault-owner");
        var audio = level.GetNode<PrologueAudioDirector>("Audio");
        Node2D replacement = null;
        void ReplaceAndThrow()
        {
            replacement = new Node2D { Name = "ReplacementAfterQuitFault" };
            tree.Root.AddChild(replacement); tree.CurrentScene = replacement;
            throw new InvalidOperationException("controlled retired quit callback");
        }
        audio.Task10TransitionsCleared += ReplaceAndThrow;
        try
        {
            ulong before = Engine.GetProcessFrames();
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Quit").EmitSignal(BaseButton.SignalName.Pressed);
            audio.Task10TransitionsCleared -= ReplaceAndThrow;
            TestAssert.True(tree.CurrentScene == replacement && GodotObject.IsInstanceValid(level),
                "fault control replaces current application while retaining old owner");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(Engine.GetProcessFrames() > before + 1 && GodotObject.IsInstanceValid(replacement) && replacement.IsInsideTree(),
                "APPLICATION_QUIT_FAULT_OWNER_RED: retired exception must not globally quit its replacement");
            GD.Print("APPLICATION_QUIT_FAULT_OWNER_OK replaced_alive=true retired_failure_no_global_quit=true");
        }
        finally
        {
            audio.Task10TransitionsCleared -= ReplaceAndThrow;
            if (GodotObject.IsInstanceValid(level)) level.Free();
            if (GodotObject.IsInstanceValid(replacement)) replacement.Free();
            tree.Paused = false;
        }
    }

    private static async Task RecoveryAfterCommit(SceneTree tree)
    {
        var failures = new List<string>();
        for (int scenario = 0; scenario < 4; scenario++)
        {
            var (level, flow, _) = await PlayingLevel(tree, "quit-recovery-" + scenario);
            var audio = level.GetNode<PrologueAudioDirector>("Audio");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            int requests = 0, completions = 0, lateResets = 0;
            Task<AudioQuitDrainResult> pending = null;
            Node2D replacement = null;
            void Check(bool valid, string boundary) { if (!valid) failures.Add($"scenario={scenario} {boundary}"); }
            level.RecoveryRequested += _ =>
            {
                requests++;
                if (scenario == 2) pending = flow.PrepareApplicationQuitAsync();
                // Select the existing ordinary resume branch, without changing
                // geometry/population, so its later physics continuation is exercised.
                if (scenario == 3) level.CancelResidentPreparation();
            };
            level.RecoveryCompleted += _ =>
            {
                completions++;
                if (scenario == 3)
                {
                    TestAssert.True(level.RecoveryInProgress && !level.ResidentPreparationPending,
                        "ordinary recovery completion really precedes the next-physics resume branch");
                    pending = flow.PrepareApplicationQuitAsync();
                }
            };
            world.ResidentSectorResetFinished += (_, _) => { if (flow.IsApplicationQuitting) lateResets++; };
            audio.Task10TransitionsCleared += () =>
            {
                if (flow.IsApplicationQuitting) Check(!level.RecoveryInProgress && !level.ResidentPreparationPending,
                    "recovery must be invalidated before synchronous audio callbacks");
            };
            try
            {
                if (scenario == 0)
                {
                    var pause = level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open();
                    tree.Root.EmitSignal(Window.SignalName.CloseRequested);
                    pending = flow.PrepareApplicationQuitAsync();
                    pause.Execute(ProloguePauseCommand.ReturnToCheckpoint);
                    pause.Execute(ProloguePauseCommand.Resume);
                    Check(tree.Paused && requests == 0 && !level.RecoveryInProgress,
                        "window-close must reject checkpoint/resume commands without unpausing");
                    replacement = new Node2D { Name = "ReplacementDuringWindowDrain" };
                    tree.Root.AddChild(replacement); tree.CurrentScene = replacement;
                }
                else
                {
                    level.RequestRespawn("QUIT GUARD RECOVERY");
                    Check(requests == 1, "real recovery request accepted before commitment");
                    if (scenario == 1) pending = flow.PrepareApplicationQuitAsync();
                    if (scenario is 1 or 2) Check(!level.RecoveryInProgress && !level.ResidentPreparationPending,
                        "queued recovery revoked immediately on quit");
                    for (int frame = 0; frame < 180 && pending == null; frame++)
                        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                    TestAssert.True(pending != null, "active recovery reached its genuine completion callback");
                }
                await pending!;
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                Check(GameplayDisabled(level) && !level.GetNode<Node2D>("PlayerRoot").Visible,
                    "no next-physics continuation may reenable terminal gameplay");
                Check(lateResets == 0 && completions == (scenario == 3 ? 1 : 0),
                    "no deferred reset or extra recovery completion after commitment");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(level)) level.Free();
                if (GodotObject.IsInstanceValid(replacement)) replacement.Free();
                tree.Paused = false;
            }
        }
        TestAssert.True(failures.Count == 0, "APPLICATION_QUIT_RECOVERY_RED: " + string.Join("; ", failures));
        GD.Print("APPLICATION_QUIT_RECOVERY_OK commands_rejected=true queued_cancelled=true active_resume_cancelled=true gameplay_disabled=true");
    }

    private static async Task DirectPersistenceAfterCommit(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (string transaction in new[] { "completion", "fragment", "checkpoint" })
        {
            var (level, flow, paths) = await PlayingLevel(tree, "quit-save-" + transaction);
            try
            {
                if (transaction == "completion")
                {
                    string blocked = Path.Combine(paths.CaseRoot, "blocked-save-directory");
                    Directory.CreateDirectory(blocked); flow.SavePath = blocked;
                    TestAssert.True(!flow.TryComplete() && flow.State == PrologueGameFlowState.CompletionSaveFailed,
                        "completion retry control begins with a real failed durable transaction");
                    flow.SavePath = paths.SavePath;
                }
                byte[] before = File.ReadAllBytes(paths.SavePath);
                Task<AudioQuitDrainResult> pending = flow.PrepareApplicationQuitAsync();
                if (transaction == "completion")
                {
                    flow.GetNode<ConfirmationDialog>("CompletionSaveFailure").EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                    if (flow.State != PrologueGameFlowState.CompletionSaveFailed) failures.Add("completion retry committed after quit");
                }
                else if (transaction == "fragment")
                {
                    if (flow.TryCollectFragment("canyon.v2.fragment.entrance") != FragmentCollectionResult.Unavailable)
                        failures.Add("late fragment transaction accepted");
                }
                else if (flow.TryCommitCheckpoint("canyon.v2.basin-camp")) failures.Add("late checkpoint transaction accepted");
                if (!before.SequenceEqual(File.ReadAllBytes(paths.SavePath))) failures.Add(transaction + " changed isolated save bytes");
                await pending;
            }
            finally { if (GodotObject.IsInstanceValid(level)) level.Free(); tree.Paused = false; }
        }
        TestAssert.True(failures.Count == 0, "APPLICATION_QUIT_PERSISTENCE_RED: " + string.Join("; ", failures));
        GD.Print("APPLICATION_QUIT_PERSISTENCE_OK completion_retry_denied=true fragment_denied=true checkpoint_denied=true unchanged_bytes=true");
    }
    public static async Task WindowLeaseControls(SceneTree tree)
    {
        bool originalAutoQuit = tree.AutoAcceptQuit;
        Node originalScene = tree.CurrentScene;
        PrologueLevelController first = null, foreign = null, replacement = null;
        PrologueLevelController Mount(string name, bool current)
        {
            var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            IsolatedPrologueFixture.Configure(level, name);
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            if (current) tree.CurrentScene = level;
            return level;
        }
        async Task Settle()
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            first = Mount("quit-first-application", true); await Settle();
            TestAssert.True(!tree.AutoAcceptQuit, "current application owns window close");
            int bindings = tree.Root.GetSignalConnectionList(Window.SignalName.CloseRequested).Count;
            foreign = Mount("quit-foreign-fixture", false); await Settle();
            TestAssert.Equal(bindings, tree.Root.GetSignalConnectionList(Window.SignalName.CloseRequested).Count,
                "non-current fixture cannot claim application window close");
            foreign.Free(); foreign = null;
            replacement = Mount("quit-replacement-application", true); await Settle();
            var retiredFlow = first.GetNode<PrologueGameFlowController>("GameFlow");
            first.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Quit").EmitSignal(BaseButton.SignalName.Pressed);
            TestAssert.True(!retiredFlow.IsApplicationQuitting,
                "APPLICATION_QUIT_RETIRED_REQUEST_RED: non-current application cannot initiate a global quit");
            first.Free(); first = null;
            TestAssert.True(!tree.AutoAcceptQuit,
                "APPLICATION_QUIT_WINDOW_LEASE_RED: retiring old flow cannot restore auto-quit over its replacement");
            replacement.Free(); replacement = null;
            TestAssert.Equal(originalAutoQuit, tree.AutoAcceptQuit,
                "last application owner restores the original window policy, not its predecessor's temporary false");
            GD.Print("APPLICATION_QUIT_WINDOW_LEASE_OK foreign_ignored=true replacement_preserved=true original_restored=true");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(first)) first.Free();
            if (GodotObject.IsInstanceValid(foreign)) foreign.Free();
            if (GodotObject.IsInstanceValid(replacement)) replacement.Free();
            tree.CurrentScene = originalScene;
            tree.AutoAcceptQuit = originalAutoQuit;
        }
    }
    private static MethodInfo Method(string name) => typeof(PrologueAudioDirector).GetMethod(name) ??
        throw new InvalidOperationException("APPLICATION_QUIT_AUDIO_RED: missing " + name);
    private static void Begin(PrologueAudioDirector audio) => Method("BeginApplicationQuit").Invoke(audio, null);
    private static Task Prepare(PrologueAudioDirector audio) => (Task)Method("PrepareApplicationQuitAsync").Invoke(audio, null)!;
    private static string State(Task task)
    {
        object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
        return result.GetType().GetProperty("State")!.GetValue(result)!.ToString()!;
    }
    private static AudioStreamPlayer[] Voices(PrologueAudioDirector audio) =>
        audio.GetChildren().OfType<AudioStreamPlayer>().ToArray();
    private static bool Silent(PrologueAudioDirector audio) =>
        Voices(audio).All(voice => voice.Stream == null && !voice.Playing);

    public static async Task Run(SceneTree tree)
    {
        var root = new Node2D { Name = "ApplicationQuitAudioFixture" };
        using var shape = new RectangleShape2D { Size = new Vector2(4000, 20) };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = shape }); root.AddChild(floor);
        var holder = new Node2D { Name = "PlayerRoot" }; root.AddChild(holder);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "Player"; player.Position = new Vector2(0, 14);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        holder.AddChild(player);
        var audio = PrologueAudioDirector.CreateRuntimeTree(); root.AddChild(audio);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "quit audio fixture has actual support");
            audio.Shutdown();
            audio.Play(PrologueAudioCue.UiConfirm, Vector2.Zero);
            TestAssert.True(Voices(audio).Any(voice => voice.Playing && voice.Stream != null),
                "ordinary Shutdown remains reusable with real enabled playback");
            ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
            ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 2);
            ThrusterPlayerRuntimeTests.Release();
            TestAssert.True(motor.AcceptedDashCount == 1 && audio.ThrusterStartCount == 1 && audio.ThrusterVoicePlaying,
                "terminal quit begins during an actual fresh native thruster voice");
            ulong loopId = ((GodotObject)typeof(PrologueAudioDirector)
                .GetField("_thrusterLoop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(audio)!).GetInstanceId();
            int events = 0;
            audio.Task10CueEmitted += (_, _, _) => events++;
            audio.Task10TransitionsCleared += () =>
            {
                audio.Play(PrologueAudioCue.UiConfirm, Vector2.Zero);
                audio.SetMusicState(PrologueMusicState.Exploration);
                audio.PresentShieldActive(true, Vector2.Zero);
            };
            Begin(audio);
            ulong generation = audio.ThrusterClearGeneration;
            Begin(audio);
            TestAssert.True(Silent(audio) && generation == audio.ThrusterClearGeneration && events == 0,
                "APPLICATION_QUIT_AUDIO_RED: terminal flag precedes synchronous callbacks and repeated shutdown");
            int starts = audio.ThrusterStartCount, cues = audio.Task10TransitionCueCount;
            audio.SetMusicState(PrologueMusicState.BossPhaseOne);
            audio.SetRoomAmbience(default);
            audio.Play(PrologueAudioCue.UiConfirm, Vector2.Zero);
            TestAssert.True(!audio.TryPlayGuidanceCrackle("terminal-test"), "terminal guidance cannot allocate/play");
            audio.PresentActionFrame(default, Vector2.Zero);
            audio.PresentClawFrame(default, Vector2.Zero);
            audio.PresentEnergyFrame(default, Vector2.Zero);
            audio.PresentShieldActive(true, Vector2.Zero);
            audio.SetPaused(false);
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 60);
            ThrusterPlayerRuntimeTests.KeyEvent(Key.A, true);
            ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 2);
            ThrusterPlayerRuntimeTests.Release();
            TestAssert.True(motor.AcceptedDashCount == 2 && audio.ThrusterStartCount == starts &&
                audio.Task10TransitionCueCount == cues && events == 0 && Silent(audio),
                "fresh real native identity cannot rearm terminal audio or Task10 publisher state");
            Task pending = Prepare(audio);
            TestAssert.True(ReferenceEquals(pending, Prepare(audio)), "audio preparation task is idempotent");
            await pending;
            TestAssert.Equal("Ready", State(pending), "actual native mix and owned release complete");
            TestAssert.True(!GodotObject.IsInstanceIdValid(loopId) && Silent(audio),
                "same owned native WAV released without borrowed playback disposal");
            GD.Print("APPLICATION_QUIT_AUDIO_OK enabled=true generic_reuse=true sync_reentry=true fresh_burn_denied=true owned_released=true duplicate=true");
        }
        finally
        {
            ThrusterPlayerRuntimeTests.Release(); tree.Paused = false;
            if (GodotObject.IsInstanceValid(root)) root.Free();
        }
        var cancelled = PrologueAudioDirector.CreateRuntimeTree(); tree.Root.AddChild(cancelled);
        Task cancelledTask = Prepare(cancelled);
        cancelled.Free();
        await cancelledTask;
        TestAssert.Equal("Cancelled", State(cancelledTask), "removed audio owner cancels without global quit");
        var beforeReady = PrologueAudioDirector.CreateRuntimeTree();
        Begin(beforeReady); tree.Root.AddChild(beforeReady);
        TestAssert.True(Silent(beforeReady), "terminal pre-Ready audio cannot autoplay");
        beforeReady.Free();
        GD.Print("APPLICATION_QUIT_OWNER_OK removed_cancelled=true pre_ready=true gameplay_force_changed=false");
    }
}
