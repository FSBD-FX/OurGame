using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Vfx;
using Knight.Settings;
using Knight.Config;

namespace Knight.Tests.Integration;

// Saved formal Player and automatic physics/native InputMap keys, never OS keys.
// Deliberately suspending only rendering proves the journal isn't a render edge.
public static class ThrusterPresentationRuntimeTests
{
    public static async Task RunWorldJetLastLeaf(SceneTree tree)
    {
        // Exact original last leaf, including synchronous fixture disposal.
        // No predecessor scenarios, extra wait, drain or borrowed playback.
        await DelayedWorldJet(tree);
        GD.Print("THRUSTER_WORLD_JET_LAST_LEAF_DIAGNOSTIC original_leaf_delegate=true original_inputs_frames=true synchronous_root_free=true no_exit_drain=true diagnostic_only=true human=false");
    }

    public static async Task RunWorldJet(SceneTree tree)
    {
        TestAssert.True(typeof(PrologueVfxDirector).GetProperty("ActiveThrusterEffectCount") != null &&
            typeof(PrologueAudioDirector).GetProperty("ActiveThrusterIdentity") != null,
            "actual world jet has no owned sustained effect/identity voice consumer");
        foreach (bool left in new[] { false, true }) await WorldJet(tree, left);
        await LongWorldJetAndCancel(tree);
        await RejectedWorldJet(tree);
        await DelayedWorldJet(tree);
        GD.Print("THRUSTER_WORLD_JET_OK saved_player=true actual_nozzle_world_axis=true single_owned_effect_voice=true delayed_render_no_replay=true rebind_no_replay=true pause_clear_no_replay=true reentry_death=true cached_audio_unchanged=true headless_only=true human=false");
    }

    private static (PrologueVfxDirector Vfx, PrologueAudioDirector Audio) AddJetDirectors(Fixture fixture)
    {
        var vfx = new PrologueVfxDirector { Name = "Vfx" };
        vfx.AddChild(new Node2D { Name = "World" });
        fixture.Root.AddChild(vfx);
        var audio = PrologueAudioDirector.CreateRuntimeTree(); fixture.Root.AddChild(audio);
        return (vfx, audio);
    }

    private static async Task WorldJet(SceneTree tree, bool left)
    {
        using var fixture = new Fixture(tree, true);
        var (vfx, audio) = AddJetDirectors(fixture);
        await fixture.Start();
        var presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var visual = (PlayerFormalVisual)presentation.ActiveVisual;
        var cached = GD.Load<AudioStreamWav>("res://assets/audio/prologue/sfx/player/dash.wav"); // Borrowed.
        var originalLoop = cached.LoopMode;
        int starts = ReadRigProperty<int>(audio, "ThrusterStartCount"), samples = 0;
        void Observe(PlayerPresentationFrame frame)
        {
            if (!Frame(fixture.Motor).Burning) return;
            samples++;
            TestAssert.Equal(1, ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount"), "one sustained world jet");
            TestAssert.Near(0f, ReadRigProperty<Vector2>(vfx, "ThrusterWorldPosition")
                .DistanceTo(visual.ThrusterNozzleWorldPosition), .001f, "same published pose owns actual world nozzle");
            TestAssert.Equal(-Frame(fixture.Motor).Direction, ReadRigProperty<Vector2>(vfx, "ThrusterWorldDirection"),
                "world exhaust is opposite locked direction, not facing multiplied twice");
        }
        presentation.FramePublished += Observe;
        try
        {
            SendKey(left ? Key.A : Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2);
            var identity = fixture.Motor.ThrusterState.ActiveIdentity;
            TestAssert.True(identity != default && samples > 0 && presentation.FrameObserverFailureCount == 0 &&
                ReadRigProperty<ThrusterIdentity>(audio, "ActiveThrusterIdentity") == identity &&
                ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") &&
                ReadRigProperty<int>(audio, "ThrusterStartCount") == starts + 1,
                "actual burn owns a single native playing voice and same-frame jet");
            TestAssert.True(vfx.LastCue != PresentationCue.Dash && audio.LastCue != PrologueAudioCue.PlayerDash,
                "actual typed jet does not also replay the legacy Dash VFX/audio cue");
            presentation.BindVisualRoot(fixture.Player.GetNode("VisualRoot")); await fixture.Frames(1);
            TestAssert.Equal(starts + 1, ReadRigProperty<int>(audio, "ThrusterStartCount"), "visual rebind cannot replay start");
            vfx.Configure(new GameSettingsData { ReduceFlashes = true });
            TestAssert.True(ReadRigProperty<bool>(vfx, "ThrusterReducedFlash"), "live effect adopts reduce-flash immediately");
            tree.Paused = true;
            for (int frame = 0; frame < 2; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") && Frame(fixture.Motor).Burning,
                "pause clears owned transient presentation, not physics burn budget");
            tree.Paused = false; await fixture.Frames(1);
            TestAssert.True(ReadRigProperty<int>(audio, "ThrusterStartCount") == starts + 1 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") &&
                ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 1,
                "resume quietly follows live jet without replaying voice");
            audio.ClearStagedFrameAudio(); vfx.ClearTransientEffects(); await fixture.Frames(1);
            TestAssert.Equal(starts + 1, ReadRigProperty<int>(audio, "ThrusterStartCount"), "room-style clear retains start watermark");
            Release(); await fixture.Frames(15);
            TestAssert.True(ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying"), "ended burn leaves no sustained object or voice");
            SendKey(left ? Key.A : Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
            TestAssert.Equal(starts + 2, ReadRigProperty<int>(audio, "ThrusterStartCount"), "new real burn gets exactly one new voice");
            fixture.Root.RemoveChild(fixture.Player); fixture.Root.AddChild(fixture.Player); await fixture.Frames(1);
            TestAssert.True(ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying"), "same owner exit stops only its jet");
            var health = fixture.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero, 0f, 711UL,
                fixture.Player.GetInstanceId())), "reentered presentation dies through actual Health");
            await fixture.Frames(1);
            TestAssert.Equal(EntityVisualState.Dead, presentation.LastPlayerFrame.Base.State,
                "presentation death listeners restore on reentry without RequestReady");
            TestAssert.Equal(originalLoop, cached.LoopMode, "cached imported WAV was never modified");
            TestAssert.Equal(0, presentation.FrameObserverFailureCount, "all same-publish effect geometry assertions passed");
        }
        finally { presentation.FramePublished -= Observe; Release(); tree.Paused = false; }
    }

    private static async Task DelayedWorldJet(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        var (vfx, audio) = AddJetDirectors(fixture); await fixture.Start();
        var presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        presentation.SetProcess(false);
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(16); Release();
        TestAssert.True(Facts(fixture.Motor).Length == 2 && !Frame(fixture.Motor).Burning,
            "real start/end occur while render consumer is suspended");
        presentation.SetProcess(true); await fixture.Frames(2);
        TestAssert.True(ReadRigProperty<int>(audio, "ThrusterStartCount") == 0 &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0,
            "same-drain start/end must not replay a dead burst or sound");
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        TestAssert.Equal(1, ReadRigProperty<int>(audio, "ThrusterStartCount"), "cursor still consumes next fresh start");
    }

    private static async Task LongWorldJetAndCancel(SceneTree tree)
    {
        using var module = new MobilityModuleDefinition { ModuleId = "test.presentation.long-jet", BurnDuration = .65f };
        using var fixture = new Fixture(tree, true); fixture.Motor.InitialMobilityModule = module;
        var (vfx, audio) = AddJetDirectors(fixture); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(27);
        var identity = fixture.Motor.ThrusterState.ActiveIdentity;
        TestAssert.True(identity != default && Frame(fixture.Motor).Burning &&
            ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 1 &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 1,
            "configured long burn sustains one native loop beyond original .2s WAV, never frame-replays Play");
        SendKey(Key.D, false); SendKey(Key.Shift, false); SendKey(Key.A, true); await fixture.Frames(1); Release();
        // ProcessFrame is emitted before node _Process. Wait for the actual
        // published non-burning frame, not a new physics fact paired with old audio.
        var presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        for (int frame = 0; frame < 6 && presentation.LastPlayerFrame.Mobility.Burning; frame++)
            await fixture.Frames(1);
        GD.Print($"THRUSTER_CANCEL_DIAGNOSTIC motor={Frame(fixture.Motor)} end={Facts(fixture.Motor).Last()} " +
            $"published={fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation").LastPlayerFrame.Mobility} " +
            $"audio_identity={ReadRigProperty<ThrusterIdentity>(audio, "ActiveThrusterIdentity")} " +
            $"ends={ReadRigProperty<int>(audio, "ThrusterEndCount")} tail={ReadRigProperty<bool>(audio, "ThrusterTailPlaying")} " +
            $"vfx={ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount")}");
        TestAssert.True(!Frame(fixture.Motor).Burning &&
            Facts(fixture.Motor).Last().Identity == identity &&
            Facts(fixture.Motor).Last().Reason == ThrusterEndReason.ReverseCancelled &&
            ReadRigProperty<int>(audio, "ThrusterEndCount") == 1 &&
            ReadRigProperty<bool>(audio, "ThrusterTailPlaying") &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0,
            "actual reverse cancel ends same sound identity once and starts only its bounded cancellation tail");
        await fixture.Frames(20);
        TestAssert.True(!ReadRigProperty<bool>(audio, "ThrusterTailPlaying") &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 1 &&
            ReadRigProperty<int>(audio, "ThrusterEndCount") == 1, "tail naturally finishes without replay or cleanup drain");
        GD.Print("THRUSTER_WORLD_JET_LONG_CANCEL_OK actual_long_module=true original_wav_duration_exceeded=true one_play=true same_identity_tail=true naturally_finished=true");
    }

    private static async Task RejectedWorldJet(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        var (vfx, audio) = AddJetDirectors(fixture); await fixture.Start();
        SendKey(Key.L, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        TestAssert.True(Facts(fixture.Motor).Length == 0 &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 0 &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0,
            "rejected guarded Shift creates neither successful jet nor successful voice");
        SendKey(Key.D, true); await fixture.Frames(12); SendKey(Key.D, false); SendKey(Key.A, true);
        await fixture.Frames(2); Release();
        TestAssert.True(Frame(fixture.Motor).Braking && Facts(fixture.Motor).Length == 0 &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 0,
            "ordinary reverse brake has no successful jet sound identity");
        GD.Print("THRUSTER_WORLD_JET_REJECTION_OK guarded_shift=true ordinary_brake=true no_success_presentation=true");
    }

    public static async Task RunRigCompatibility(SceneTree tree)
    {
        Knight.Tests.Unit.PlayerPresentationTests.Run();
        Knight.Tests.Unit.PlayerPoseMixerTests.Run();
        Knight.Tests.Unit.PlayerAnimationMapTests.Run();
        PlayerCombatPoseTests.Run();
        // Original named delegates, not copied assertions. This explicit leaf
        // subset does not claim the full level/recovery/legacy case registry.
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "SetupAndBasicContract", "TypedActionPhaseBoundaryBlendsWithoutHardCut",
            "AirAndDownAttacksExposeFourDistinctRigStages", "ThreadTerminalPoseReturnsContinuouslyToIdle",
            "ThreadAimUsesFacingLocalMirroredRig", "ActionOverRunKeepsLegsAndSecondarySlotsContinuous",
            "LoopWrapKeepsEveryLocomotionSlotContinuous", "TaskOneSecondaryOutputsReachTheirOwnedFormalSlots",
            "CompatibilityAdaptersRemainVisible", "SixtyFpsFramesKeepEverySlotBounded",
            "FacingLocalSecondaryMotionMirrorsWorldExactly"
        };
        Task7Case[] cases = PlayerFormalVisualTests.Task7Cases(tree).Where(item =>
            names.Contains(item.Name.Substring("PlayerFormalVisualTests.".Length))).ToArray();
        TestAssert.Equal(11, cases.Length, "all eleven explicitly selected original formal leaf cases exist");
        Task7CaseRun run = await Task7CaseRegistry.Execute(cases);
        TestAssert.Equal(11, run.Completed.Count, "every original formal leaf delegate completed");
        await PlayerMantleMotionTests.Run(tree);
        GD.Print("THRUSTER_RIG_COMPATIBILITY_OK existing_timeline_pose_map=true formal_leaf_cases=11 mantle_cases=2 original_case_delegates=true headless_only=true full_legacy_suite=false human=false");
    }

    public static async Task RunRig(SceneTree tree)
    {
        TestAssert.True(typeof(PlayerPresentationFrame).GetProperty("Mobility") != null &&
            typeof(PlayerPresentationFrame).GetProperty("MobilityLeanRadians") != null &&
            typeof(PlayerFormalVisual).GetProperty("ThrusterNozzleWorldPosition") != null,
            "single typed rig has no mobility observation/lean/nozzle consumer");
        await RigJetAndActions(tree, false);
        await RigJetAndActions(tree, true);
        await RigOrdinaryBrake(tree);
        await RigRejectedGuard(tree);
        await RigDeath(tree);
        GD.Print("THRUSTER_PRESENTATION_RIG_OK saved_player=true single_typed_consumer=true burn_coast_brake=true ordinary_brake_empty_identity=true action_death_priority=true pause_frozen=true rigid_no_stretch=true actual_nozzle=true headless_transforms_only=true human=false");
    }

    private readonly record struct RigSample(PlayerPresentationFrame Frame,
        ThrusterPresentationFrame Facts, Vector2 Velocity, float Rotation, Vector2 Scale, Vector2 Nozzle,
        Vector2 CorePosition);
    private static T ReadRigProperty<T>(object value, string name) =>
        (T)value.GetType().GetProperty(name).GetValue(value);
    private static ThrusterPresentationFrame Mobility(RigSample sample) =>
        ReadRigProperty<ThrusterPresentationFrame>(sample.Frame, "Mobility");

    private sealed class RigObserver : IDisposable
    {
        private readonly Fixture _fixture;
        private readonly PlayerPresentationCoordinator _presentation;
        internal readonly PlayerFormalVisual Visual;
        internal readonly List<RigSample> Samples = new();
        internal RigObserver(Fixture fixture)
        {
            _fixture = fixture;
            _presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            Visual = (PlayerFormalVisual)_presentation.ActiveVisual;
            _presentation.FramePublished += Published;
        }
        private void Published(PlayerPresentationFrame frame) => Samples.Add(new RigSample(frame,
            Frame(_fixture.Motor), _fixture.Player.Velocity, Visual.Rotation, Visual.Scale,
            ReadRigProperty<Vector2>(Visual, "ThrusterNozzleWorldPosition"),
            Visual.GetNode<Node2D>("ShoulderCore").GlobalPosition));
        internal async Task<RigSample> Wait(Func<RigSample, bool> predicate, string label, int frames = 6)
        {
            int first = Samples.Count;
            for (int frame = 0; frame < frames; frame++)
            {
                await _fixture.Frames(1);
                for (int index = first; index < Samples.Count; index++)
                {
                    RigSample sample = Samples[index];
                    TestAssert.Equal(sample.Facts, Mobility(sample), "real published frame consumes identical Motor facts");
                    TestAssert.Near(ReadRigProperty<float>(sample.Frame, "MobilityLeanRadians"),
                        sample.Rotation, .0001f, "single typed renderer owns rigid lean");
                    TestAssert.True(Mathf.Abs(sample.Scale.X) == 1f && sample.Scale.Y == 1f,
                        "rig facing only, no nonuniform image stretch");
                    if (predicate(sample)) return sample;
                }
            }
            throw new InvalidOperationException("Actual typed rig never observed " + label);
        }
        public void Dispose() => _presentation.FramePublished -= Published;
    }

    private static async Task RigJetAndActions(SceneTree tree, bool left)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        using var observer = new RigObserver(fixture);
        int sign = left ? -1 : 1;
        SendKey(left ? Key.A : Key.D, true); SendKey(Key.Shift, true);
        RigSample burn = await observer.Wait(s => s.Facts.Burning && s.Rotation * sign > .005f, "forward burn lean");
        GD.Print($"THRUSTER_RIG_NOZZLE_DIAGNOSTIC rotation={burn.Rotation} identity={burn.Facts.Identity} " +
            $"nozzle={burn.Nozzle} core_at_publish={burn.CorePosition} " +
            $"core_at_assert={observer.Visual.GetNode<Node2D>("ShoulderCore").GlobalPosition} " +
            $"same_sample_distance={burn.Nozzle.DistanceTo(burn.CorePosition)} " +
            $"mixed_sample_distance={burn.Nozzle.DistanceTo(observer.Visual.GetNode<Node2D>("ShoulderCore").GlobalPosition)}");
        TestAssert.True(Mathf.Abs(burn.Rotation) <= .2f && burn.Facts.Identity != default &&
            burn.Nozzle.DistanceTo(burn.CorePosition) <= 8.1f,
            "finite jet lean and actual shoulder-core nozzle attachment");
        float rotation = observer.Visual.Rotation;
        ThrusterPresentationFrame frozen = Frame(fixture.Motor);
        int count = observer.Samples.Count;
        tree.Paused = true;
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(frozen, Frame(fixture.Motor), "pause freezes actual mobility");
        TestAssert.Equal(rotation, observer.Visual.Rotation, "pause freezes visible rig lean");
        TestAssert.Equal(count, observer.Samples.Count, "paused coordinator does not advance a presentation clock");
        tree.Paused = false; Release();
        RigSample coast = await observer.Wait(s => s.Facts.Coasting && !s.Facts.Burning,
            "real coast settle", 18);
        TestAssert.True(coast.Rotation * sign > 0f && coast.Facts.Identity == burn.Facts.Identity,
            "coast pose retains same accepted identity, not another dash");
        SendKey(left ? Key.D : Key.A, true);
        RigSample brake = await observer.Wait(s => s.Facts.Braking && s.Velocity.X * sign > 0f && s.Rotation * sign < -.005f,
            "reverse-brake lean while still moving forward");
        TestAssert.True(brake.Facts.Coasting && Mathf.Abs(brake.Rotation) <= .2f,
            "braking is orthogonal to short coast and has a distinct opposing pose");
        Release(); SendKey(Key.J, true);
        RigSample attack = await observer.Wait(s => s.Frame.Action.State is PlayerActionState.GroundAttack or
            PlayerActionState.AirAttack or PlayerActionState.DownAttack, "real J attack priority");
        TestAssert.Near(0f, attack.Rotation, .0001f, "attack owns visible posture over mobility");
        Release();
    }

    private static async Task RigOrdinaryBrake(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        using var observer = new RigObserver(fixture);
        SendKey(Key.D, true); await fixture.Frames(20); Release(); SendKey(Key.A, true);
        RigSample brake = await observer.Wait(s => s.Facts.Braking && s.Velocity.X > 0f && s.Rotation < 0f,
            "ordinary run reverse brake");
        TestAssert.True(brake.Facts.Identity == default && !brake.Facts.Burning && !brake.Facts.Coasting &&
            Facts(fixture.Motor).Length == 0, "ordinary brake pose never invents a burn identity");
        Release();
    }

    private static async Task RigRejectedGuard(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        using var observer = new RigObserver(fixture);
        SendKey(Key.L, true); SendKey(Key.Shift, true);
        RigSample guard = await observer.Wait(s => s.Frame.Action.State == PlayerActionState.Shield,
            "actual shield priority over rejected Shift");
        TestAssert.True(!guard.Facts.Burning && guard.Facts.Identity == default &&
            Facts(fixture.Motor).Length == 0, "guard-rejected input has no successful jet observation");
        TestAssert.Near(0f, guard.Rotation, .0001f, "shield posture is not tilted by rejected input");
        Release();
    }

    private static async Task RigDeath(SceneTree tree)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        using var observer = new RigObserver(fixture);
        SendKey(Key.D, true); SendKey(Key.Shift, true);
        await observer.Wait(s => s.Facts.Burning, "actual airborne jet before death"); Release();
        var health = fixture.Player.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero,
            0f, 803UL, fixture.Player.GetInstanceId())), "real Health lethal request");
        RigSample dead = await observer.Wait(s => s.Frame.Locomotion.State == PlayerLocomotionState.Dead,
            "real death priority");
        TestAssert.True(Mobility(dead) == default && dead.Facts == default,
            "death removes mobility observation rather than visually continuing old jet");
        TestAssert.Near(0f, dead.Rotation, .0001f, "death pose is not rotated by old mobility lean");
    }

    public static async Task RunAudioOwner(SceneTree tree)
    {
        foreach (bool complete in new[] { false, true })
        {
            var audio = PrologueAudioDirector.CreateRuntimeTree();
            tree.Root.AddChild(audio);
            try
            {
                // SceneTree _Initialize runs before the root's first Ready.
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                var field = typeof(PrologueAudioDirector).GetField("_tweens",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var list = (List<Tween>)field.GetValue(audio);
                TestAssert.True(list.Count > 0 && list.Any(x => x.IsValid()),
                    "actual enabled audio creates its own native music fade, no fake Tween fixture");
                if (complete)
                {
                    await Finish();
                    TestAssert.Near(0f, audio.GetNode<AudioStreamPlayer>("MusicA").VolumeDb, .01f,
                        "native fade still completes to intended volume");
                    audio.SetMusicState(PrologueMusicState.Silent);
                    await Finish();
                    TestAssert.True(audio.GetNode<AudioStreamPlayer>("MusicA").Stream == null,
                        "native fade-out callback still releases its stream");
                }
                Tween[] created = list.ToArray(); // Read-only aliases, never disposed by test.
                audio.Shutdown();
                TestAssert.True(list.Count == 0 && created.All(x => !GodotObject.IsInstanceValid(x)),
                    "audio Shutdown releases its own active and completed Tween wrappers deterministically");
                audio.Shutdown();

                async Task Finish()
                {
                    for (int frame = 0; frame < 180 && list.Any(x => x.IsValid()); frame++)
                    {
                        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    }
                    TestAssert.True(list.All(x => !x.IsValid()), "actual native fade/callback completes within bounded gameplay frames");
                }
            }
            finally
            {
                audio.Free();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        GD.Print("THRUSTER_AUDIO_OWNER_OK native_fade_completed=true callback_completed=true active_shutdown=true completed_shutdown=true idempotent=true only_owned_wrappers=true no_gc_or_drain=true human=false");
    }

    private static ThrusterPresentationFrame Frame(PlayerMotor motor) =>
        (ThrusterPresentationFrame)typeof(PlayerMotor).GetProperty("ThrusterPresentation").GetValue(motor);
    private static ThrusterPresentationTransition[] Facts(PlayerMotor motor, ulong after = 0UL) =>
        ((IReadOnlyList<ThrusterPresentationTransition>)typeof(PlayerMotor)
            .GetMethod("ReadThrusterTransitionsAfter").Invoke(motor, new object[] { after })).ToArray();

    public static async Task Run(SceneTree tree)
    {
        TestAssert.True(typeof(PlayerMotor).GetProperty("ThrusterPresentation") != null &&
            typeof(PlayerMotor).GetMethod("ReadThrusterTransitionsAfter") != null,
            "actual saved Player typed thruster facts are unavailable");
        await DelayedRenderAndReverseBrake(tree);
        await RejectedGuardAndOrdinaryBrake(tree);
        await ResetAndDeath(tree, false);
        await ResetAndDeath(tree, true);
        await ClawClearsCoast(tree);
        await CollisionClearsCoast(tree, false);
        await CollisionClearsCoast(tree, true);
        await ReentryAndDeath(tree);
        await ReverseAndReplacement(tree, false);
        await ReverseAndReplacement(tree, true);
        await NewJumpClearsCoast(tree);
        GD.Print("THRUSTER_PRESENTATION_BOUNDARIES_OK reverse_cancel=true replacement=true jump_exit=true");
        GD.Print("THRUSTER_PRESENTATION_PLAYER_OK saved_player=true automatic_keyboard=true delayed_render_start_end=true finite_coast=true pause_frozen=true actual_brake_crossing=true rejected_guard=true reset_death=true claw_takeover=true axis_collision=true reentry=true human=false audiovisual=false");
    }

    private static async Task DelayedRenderAndReverseBrake(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        await fixture.Start();
        fixture.Player.GetNode("Presentation").SetProcess(false);
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(1);
        ThrusterIdentity identity = fixture.Motor.ThrusterState.ActiveIdentity;
        TestAssert.True(Frame(fixture.Motor).Burning && Frame(fixture.Motor).Identity == identity,
            "accepted real commit produces the identical typed owner/life/epoch/burn");
        Release(); await fixture.Frames(14);
        var facts = Facts(fixture.Motor);
        TestAssert.True(facts.Length == 2 && facts[0].Kind == ThrusterTransitionKind.Started &&
            facts[1].Kind == ThrusterTransitionKind.Ended && facts.All(x => x.Identity == identity) &&
            facts[1].Reason == ThrusterEndReason.Exhausted,
            "start/end both survive without any intermediate presentation processing");
        TestAssert.True(Frame(fixture.Motor).Coasting && !Frame(fixture.Motor).Burning &&
            Frame(fixture.Motor).CoastRemaining <= .45f && fixture.Player.Velocity.X > 300f,
            "actual finite force creates short observation without deleting real residual speed");
        TestAssert.Equal(0, Facts(fixture.Motor, facts[1].Sequence).Length, "reader cursor doesn't replay transitions");
        var frozen = Frame(fixture.Motor); var pulse = fixture.Motor.ThrusterState;
        tree.Paused = true;
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(frozen, Frame(fixture.Motor), "pause freezes the exact typed coast frame");
        TestAssert.Equal(pulse, fixture.Motor.ThrusterState, "pause preserves physical pulse/budget");
        tree.Paused = false;
        SendKey(Key.A, true); await fixture.Frames(1);
        TestAssert.True(Frame(fixture.Motor).Braking && Frame(fixture.Motor).Coasting,
            "real reverse braking and short coasting are orthogonal");
        bool crossed = false;
        for (int tick = 0; tick < 20; tick++)
        {
            await fixture.Frames(1);
            if (fixture.Player.Velocity.X <= 0f)
            {
                crossed = true;
                TestAssert.True(!Frame(fixture.Motor).Coasting && Frame(fixture.Motor).Identity == default,
                    "actual brake-to-zero exits coast even when the remaining step already reverses");
                break;
            }
        }
        TestAssert.True(crossed && fixture.Motor.AirDashAvailable && Facts(fixture.Motor).Length == 2,
            "observation braking neither spends budget nor invents another start/end");
        Release();
    }

    private static async Task ReverseAndReplacement(SceneTree tree, bool activeReplacement)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        ThrusterIdentity identity = fixture.Motor.ThrusterState.ActiveIdentity;
        if (!activeReplacement)
        {
            SendKey(Key.A, true); await fixture.Frames(1); Release();
            TestAssert.True(Frame(fixture.Motor).Coasting && Frame(fixture.Motor).Braking &&
                !Frame(fixture.Motor).Burning && fixture.Player.Velocity.X > 0f,
                "real mid-burn opposite control cancels only finite thrust and brakes inherited residual velocity");
        }
        fixture.Motor.ExternalImpulse(new Vector2(-40f, 20f));
        var facts = Facts(fixture.Motor);
        TestAssert.True(facts.Length == 2 && facts[1].Identity == identity &&
            facts[1].Reason == (activeReplacement ? ThrusterEndReason.ExternalReplacement : ThrusterEndReason.ReverseCancelled) &&
            Frame(fixture.Motor) == default && !fixture.Motor.AirDashAvailable,
            "full replacement clears typed observation without duplicate end or budget refund");
        TestAssert.Equal(new Vector2(-40f, 20f), fixture.Player.Velocity,
            "presentation never modifies the real complete replacement");
    }

    private static async Task NewJumpClearsCoast(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(1); Release();
        await fixture.Frames(14);
        TestAssert.True(Frame(fixture.Motor).Coasting && fixture.Player.IsOnFloor(), "jump exit starts from real supported coast");
        SendKey(Key.Space, true); await fixture.Frames(1); Release();
        TestAssert.True(fixture.Motor.AcceptedJumpCount == 1 && fixture.Player.Velocity.Y < 0f &&
            !Frame(fixture.Motor).Coasting && Frame(fixture.Motor).Identity == default && Facts(fixture.Motor).Length == 2,
            "new ordinary jump exits jet observation without inventing another burn or blocking legitimate ascent");
    }

    private static async Task RejectedGuardAndOrdinaryBrake(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        SendKey(Key.L, true); SendKey(Key.Shift, true); await fixture.Frames(1);
        TestAssert.True(fixture.Player.GetNode<PlayerInputReader>("Input").RawFrame.DashPressed &&
            fixture.Player.GetNode<PlayerInputReader>("Input").RawFrame.ShieldHeld &&
            fixture.Motor.AcceptedDashCount == 0 && Facts(fixture.Motor).Length == 0 &&
            Frame(fixture.Motor).Identity == default && !Frame(fixture.Motor).Burning,
            "raw guard priority suppresses Shift before Motor; no accepted identity or transition");
        Release(); await fixture.Frames(1);
        SendKey(Key.D, true); await fixture.Frames(12); SendKey(Key.D, false); SendKey(Key.A, true);
        await fixture.Frames(1);
        TestAssert.True(Frame(fixture.Motor).Braking && !Frame(fixture.Motor).Coasting &&
            Frame(fixture.Motor).Identity == default,
            "ordinary real reverse braking has no invented old burn identity");
        Release();
    }

    private static async Task ResetAndDeath(SceneTree tree, bool death)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        var identity = fixture.Motor.ThrusterState.ActiveIdentity;
        if (death)
        {
            var health = fixture.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero,
                0f, 701UL, fixture.Player.GetInstanceId())), "actual damage kills; no HP rewrite");
        }
        else fixture.Motor.ResetTransientState();
        var facts = Facts(fixture.Motor);
        TestAssert.True(facts.Length == 2 && facts[1].Identity == identity &&
            facts[1].Reason == (death ? ThrusterEndReason.Death : ThrusterEndReason.Reset),
            "real death and explicit reset retain their distinct old-identity end reasons");
        TestAssert.Equal(default(ThrusterPresentationFrame), Frame(fixture.Motor),
            "retired lifecycle leaves no burning, coast, brake or stale direction");
        await fixture.Frames(3);
        TestAssert.Equal(2, Facts(fixture.Motor).Length, "reset/death cannot replay the held or retired pulse");
    }

    private static async Task ClawClearsCoast(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        fixture.Root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        await fixture.Start(); SendKey(Key.W, true); SendKey(Key.Space, true);
        SendKey(Key.Shift, true); await fixture.Frames(1);
        SendKey(Key.Shift, false); SendKey(Key.Space, false); await fixture.Frames(13);
        TestAssert.True(Frame(fixture.Motor).Coasting, "claw test starts with actual upward jet coast");
        SendKey(Key.K, true); await fixture.Frames(1); SendKey(Key.K, false);
        var claw = fixture.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        for (int tick = 0; tick < 150 && claw.CurrentFrame.State is not (MechanicalClawState.Reel or MechanicalClawState.Swing); tick++)
            await fixture.Frames(1);
        TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing,
            "real claw reaches attached phase, not merely Aim or Travel");
        TestAssert.True(!Frame(fixture.Motor).Coasting && !Frame(fixture.Motor).Burning &&
            !fixture.Motor.AirDashAvailable, "actual claw attachment clears observation without air-use refund");
        Release();
    }

    private static async Task CollisionClearsCoast(SceneTree tree, bool ceiling)
    {
        using var fixture = new Fixture(tree, ceiling);
        fixture.AddSolid(ceiling ? new Vector2(0, -80) : new Vector2(120, -300),
            ceiling ? new Vector2(800, 10) : new Vector2(10, 1600));
        bool observed = false, retained = false;
        fixture.Root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 110, Observe = () =>
        {
            if (!(ceiling ? fixture.Player.IsOnCeiling() : fixture.Player.IsOnWall())) return;
            observed = true;
            if (Frame(fixture.Motor).Coasting && !retained)
            {
                retained = true;
                GD.Print($"THRUSTER_AXIS_CONTACT_DIAGNOSTIC ceiling={ceiling} count={fixture.Player.GetSlideCollisionCount()} velocity={fixture.Player.Velocity} platform={fixture.Player.GetPlatformVelocity()} frame={Frame(fixture.Motor)}");
                for (int index = 0; index < fixture.Player.GetSlideCollisionCount(); index++)
                {
                    var contact = fixture.Player.GetSlideCollision(index); // Borrowed.
                    GD.Print($"THRUSTER_AXIS_CONTACT_DETAIL index={index} normal={contact.GetNormal()} depth={contact.GetDepth()} travel={contact.GetTravel()} remainder={contact.GetRemainder()} colliderVelocity={contact.GetColliderVelocity()} type={contact.GetCollider()?.GetClass()}");
                }
            }
        } });
        await fixture.Start(); SendKey(ceiling ? Key.W : Key.D, true); SendKey(Key.Shift, true);
        await fixture.Frames(20); Release();
        TestAssert.True(observed && !retained && !fixture.Motor.IsDashing && !Frame(fixture.Motor).Coasting,
            "saved capsule reaches thin wall/low ceiling and finite burn ends with no blocked coast");
    }

    private static async Task ReentryAndDeath(SceneTree tree)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        var identity = fixture.Motor.ThrusterState.ActiveIdentity;
        fixture.Root.RemoveChild(fixture.Player); fixture.Root.AddChild(fixture.Player);
        var ended = Facts(fixture.Motor);
        TestAssert.True(ended.Length == 2 && ended[1].Identity == identity &&
            ended[1].Reason.ToString() == "OwnerRetired" && !Frame(fixture.Motor).Burning,
            "same Player reparent retires exact burn without misreporting HP death");
        await fixture.Frames(1);
        var health = fixture.Player.GetNode<HealthComponent>("Health");
        ulong before = fixture.Motor.ThrusterState.Lifecycle;
        TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero,
            0f, 702UL, fixture.Player.GetInstanceId())), "reentered Player dies through Health");
        TestAssert.True(fixture.Motor.ThrusterState.Lifecycle > before,
            "death subscription remains alive after tree reentry without RequestReady");
        TestAssert.Equal(2, Facts(fixture.Motor).Length, "retired burn is not ended again on later death");
    }

    private static void SendKey(Key key, bool pressed) => ThrusterPlayerRuntimeTests.KeyEvent(key, pressed);
    private static void Release() => ThrusterPlayerRuntimeTests.Release();
    private sealed class Fixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly bool _supported;
        private readonly List<Shape2D> _shapes = new();
        internal Node2D Root { get; } = new() { Name = "ThrusterPresentationPhysics" };
        internal CharacterBody2D Player { get; }
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal Fixture(SceneTree tree, bool supported)
        {
            _tree = tree; _supported = supported;
            if (supported) AddSolid(new Vector2(0, 60), new Vector2(4000, 20));
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new Vector2(0, supported ? 14 : -500);
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Root.AddChild(Player);
        }
        internal void AddSolid(Vector2 position, Vector2 size)
        {
            var shape = new RectangleShape2D { Size = size }; _shapes.Add(shape);
            var body = new StaticBody2D { Position = position, CollisionLayer = CombatCollisionLayers.World };
            body.AddChild(new CollisionShape2D { Shape = shape }); Root.AddChild(body);
        }
        internal async Task Start()
        {
            Release(); _tree.Root.AddChild(Root); await Frames(_supported ? 8 : 1);
            TestAssert.Equal(_supported, Player.IsOnFloor(), "fixture starts with intended real support");
        }
        internal Task Frames(int count) => ThrusterPlayerRuntimeTests.Frames(_tree,
            Player.GetNode<PlayerInputReader>("Input"), count);
        public void Dispose()
        {
            // Scalar diagnosis of the actually owned copy, no playback lookup,
            // borrowed disposal, extra frame or retained wrapper across teardown.
            ulong loopId = 0UL, voiceId = 0UL, audioId = 0UL;
            ulong rootId = Root.GetInstanceId();
            bool trace = Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace-thruster-audio-owners") >= 0;
            if (trace && Root.GetNodeOrNull<PrologueAudioDirector>("Audio") is PrologueAudioDirector audio)
            {
                var loop = typeof(PrologueAudioDirector).GetField("_thrusterLoop",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(audio) as GodotObject;
                loopId = loop?.GetInstanceId() ?? 0UL;
                audioId = audio.GetInstanceId();
                voiceId = audio.GetNodeOrNull<AudioStreamPlayer>("ThrusterVoice")?.GetInstanceId() ?? 0UL;
                GD.Print($"THRUSTER_OWNED_COPY_ID stage=before-root-free id={loopId} present={loopId != 0UL} " +
                    $"root={rootId} audio={audioId} voice={voiceId} diagnostic_only=true");
            }
            Release(); _tree.Paused = false; if (GodotObject.IsInstanceValid(Root)) Root.Free();
            foreach (var shape in _shapes) shape.Dispose();
            if (trace) GD.Print($"THRUSTER_OWNED_COPY_ID stage=after-root-free id={loopId} " +
                $"valid={loopId != 0UL && GodotObject.IsInstanceIdValid(loopId)} root={rootId} root_valid={GodotObject.IsInstanceIdValid(rootId)} " +
                $"audio={audioId} audio_valid={audioId != 0UL && GodotObject.IsInstanceIdValid(audioId)} " +
                $"voice={voiceId} voice_valid={voiceId != 0UL && GodotObject.IsInstanceIdValid(voiceId)} diagnostic_only=true");
        }
    }
}
