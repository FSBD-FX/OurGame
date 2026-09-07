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
    public static async Task Task10ActionCueBaselineAndReentryAreExact(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        PrologueAudioDirector audio = level.GetNode<PrologueAudioDirector>("Audio");
        audio.PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 3);
            CharacterBody2D player = level.Player;
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            PlayerActionModeController modes =
                player.GetNode<PlayerActionModeController>("ActionModes");
            PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
            PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
            PrologueVfxDirector vfx = level.GetNode<PrologueVfxDirector>("Vfx");
            var failures = new List<string>();

            TestAssert.Equal(PlayerActionMode.Attack, modes.CurrentMode,
                "generated main begins in the real Attack action state");
            TestAssert.Equal(1, modes.ActiveSlot,
                "generated main begins on the real occupied attack loadout");

            audio.ClearStagedFrameAudio();
            vfx.ClearTransientEffects();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            int firstLoadoutStart = audio.Task10CueHistory.Count;
            await Tap(tree, InputActions.Slot2);
            PrologueAudioCue[] firstLoadout = audio.Task10CueHistory.Skip(firstLoadoutStart).ToArray();
            if (firstLoadout.Length != 0 || vfx.ActiveTask10EffectCount != 0)
                failures.Add("TASK10_FIX2_FALSE_FIRST_LOADOUT_RED expected=no-cue/no-vfx actual=" +
                    $"{string.Join(',', firstLoadout)}/vfx={vfx.ActiveTask10EffectCount}");

            await Tap(tree, InputActions.Slot1);
            energy.ResetFull();
            audio.ClearStagedFrameAudio();
            vfx.ClearTransientEffects();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(energy.TrySpendHeavyGuard(100f),
                "real energy owner establishes the first-publication rejection precondition");
            int firstRejectionStart = audio.Task10CueHistory.Count;
            await Tap(tree, InputActions.PrimaryAction);
            PrologueAudioCue[] firstRejection =
                audio.Task10CueHistory.Skip(firstRejectionStart).ToArray();
            if (!firstRejection.SequenceEqual(new[] { PrologueAudioCue.EnergyEmpty }) ||
                vfx.ActiveTask10EffectCount != 1)
                failures.Add("TASK10_FIX2_FALSE_FIRST_REJECTION_RED expected=EnergyEmpty/vfx=1 actual=" +
                    $"{string.Join(',', firstRejection)}/vfx={vfx.ActiveTask10EffectCount}");

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
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    public static async Task Task10ProductionPublishersReachDirectors(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        PrologueAudioDirector audio = level.GetNode<PrologueAudioDirector>("Audio");
        audio.PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        tree.Root.AddChild(level);
        try
        {
            await Frames(tree, 2);
            CharacterBody2D player = level.Player;
            player.GetNode<Camera2D>("Camera2D").Enabled = false;
            player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
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
            flow.ApplySettings(new GameSettingsData { GuidanceEnabled = false });

            await Frames(tree, 1);
            Vector2 cursor = input.Current.MouseWorld;
            player.GlobalPosition = cursor + Vector2.Left * 180f;
            player.Velocity = Vector2.Zero;
            GrappleSurface2D surface = Surface("Task10ProductionSurface", cursor,
                GrappleSurfaceMaterial.DedicatedAnchor);
            level.AddChild(surface);
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
            Input.ActionPress(InputActions.PrimaryAction);
            await WaitFor(tree, () => claw.CurrentFrame.State == MechanicalClawState.Latched,
                180, "real InputMap claw reaches Latched");
            await WaitFor(tree, () => audio.Task10CueHistory.Contains(PrologueAudioCue.ClawTension),
                60, "real held claw enters its tension family");
            Input.ActionRelease(InputActions.PrimaryAction);
            await WaitFor(tree, () => claw.CurrentFrame.State == MechanicalClawState.Idle,
                180, "real InputMap claw release retracts to Idle");

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
            TestAssert.Equal(1, audio.Task10CueHistory.Count(cue => cue == PrologueAudioCue.ClawTension),
                "Reel/Swing polling cannot spam the one claw-tension entry cue");
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
            TestAssert.True(health.ApplyDamage(new DamageRequest(
                    health.CurrentHealth, Vector2.Down, 0f, 42001UL)),
                "real player death starts the production recovery edge");
            await Frames(tree, 3);
            TestAssert.Equal(0, audio.ActiveTask10TransitionCount,
                "death/recovery completion clears production typed audio state");
            TestAssert.Equal(0, vfx.ActiveTask10EffectCount,
                "death/recovery completion clears production typed VFX state");

            await EnterOppositeMode(tree, modes);
            PrologueSectorId beforeSector = level.CurrentSector!.Value;
            player.GlobalPosition = new Vector2(1500f, 1600f);
            await WaitFor(tree, () => level.CurrentSector != beforeSector, 60,
                "real continuous world enters a different sector");
            TestAssert.Equal(0, audio.ActiveTask10TransitionCount,
                "sector transition/unload clears production typed audio state");
            TestAssert.Equal(0, vfx.ActiveTask10EffectCount,
                "sector transition/unload clears production typed VFX state");

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
            foreach (StringName action in new[]
            {
                InputActions.SelectAttackMode, InputActions.SelectGrappleMode, InputActions.Slot1,
                InputActions.Slot2, InputActions.PrimaryAction, InputActions.Shield
            }) Input.ActionRelease(action);
            tree.Paused = false;
            level.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
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
