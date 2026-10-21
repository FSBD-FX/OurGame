using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Save;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class PrologueLevelRuntimeTests
{
    public static async Task RunGeneratedMainDeathOnly(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await GeneratedMainDeathDefersRestoreAndPreservesRecoveryClosure(tree);
        GD.Print("THRUSTER_GENERATED_MAIN_DEATH_OK original_saved_scene=true original_death_input=true same_checkpoint=true original_waits=true human=false full_route=false");
    }

    public static async Task Run(SceneTree tree)
    {
        await OverlappingLevelsCoordinateAndRestoreSustainedLowLatency(tree);
        await CrossingARealExitPreservesThePlayerInstance(tree);
        await RepeatedTransitionsReleaseEarlierRetiredRooms(tree);
        await RepeatedRestoreReleasesPreviouslyRetiredRooms(tree);
        await LegacyRecoveryWinsOverSameFrameTransition(tree);
        await GeneratedMainDeathDefersRestoreAndPreservesRecoveryClosure(tree);
    }

    private static async Task OverlappingLevelsCoordinateAndRestoreSustainedLowLatency(SceneTree tree)
    {
        GCLatencyMode prior = GCSettings.LatencyMode;
        RoomDefinition roomA = Definition(PrologueRoomId.AwakeningCourtyard,
            RuntimeRoom(PrologueRoomId.AwakeningCourtyard, "unused",
                new Vector2(10000, 10000), false));
        RoomDefinition roomB = Definition(PrologueRoomId.AwakeningCourtyard,
            RuntimeRoom(PrologueRoomId.AwakeningCourtyard, "unused",
                new Vector2(10000, 10000), false));
        PrologueLevelController levelA = Level(SimplePlayer(), [roomA]);
        PrologueLevelController levelB = Level(SimplePlayer(), [roomB]);
        tree.Root.AddChild(levelA);
        tree.Root.AddChild(levelB);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(GCLatencyMode.SustainedLowLatency, GCSettings.LatencyMode,
            "active prologue levels use sustained-low-latency collection for player-visible frames");

        levelA.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(GCLatencyMode.SustainedLowLatency, GCSettings.LatencyMode,
            "one overlapping level cannot restore the process policy while another remains active");

        levelB.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(prior, GCSettings.LatencyMode,
            "last prologue level restores the process GC latency policy");
    }

    private static async Task RepeatedTransitionsReleaseEarlierRetiredRooms(SceneTree tree)
    {
        RoomDefinition roomA = Definition(PrologueRoomId.AwakeningCourtyard,
            RuntimeRoom(PrologueRoomId.AwakeningCourtyard, "east", new Vector2(10000, 10000), false));
        RoomDefinition roomB = Definition(PrologueRoomId.BrokenToothCorridor,
            RuntimeRoom(PrologueRoomId.BrokenToothCorridor, "west", new Vector2(10000, 10000), false));
        CharacterBody2D player = SimplePlayer();
        PrologueLevelController level = Level(player, [roomA, roomB]);
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

        PrologueRoom originalRoom = level.CurrentRoom;
        level.TransitionTo(new PrologueRoomLink(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "start"));
        await PhysicsFrames(tree, 4);
        TestAssert.True(GodotObject.IsInstanceValid(originalRoom) && !originalRoom.IsInsideTree(),
            "first transition keeps only the immediately retired room outside the active tree");

        level.TransitionTo(new PrologueRoomLink(PrologueRoomId.BrokenToothCorridor, "west",
            PrologueRoomId.AwakeningCourtyard, "start"));
        await PhysicsFrames(tree, 4);
        TestAssert.True(GodotObject.IsInstanceValid(originalRoom) && !originalRoom.IsInsideTree(),
            "retirement cache may retain at most one detached graph per authored room");

        level.TransitionTo(new PrologueRoomLink(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "start"));
        await PhysicsFrames(tree, 4);
        TestAssert.True(!GodotObject.IsInstanceValid(originalRoom),
            "transition history beyond the authored-room bound releases the oldest graph");

        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task RepeatedRestoreReleasesPreviouslyRetiredRooms(SceneTree tree)
    {
        RoomDefinition roomA = Definition(PrologueRoomId.AwakeningCourtyard,
            RuntimeRoom(PrologueRoomId.AwakeningCourtyard, "east", new Vector2(160, 40), false));
        RoomDefinition roomB = Definition(PrologueRoomId.BrokenToothCorridor,
            RuntimeRoom(PrologueRoomId.BrokenToothCorridor, "west", new Vector2(300, 80), false));
        CharacterBody2D player = SimplePlayer();
        PrologueLevelController level = Level(player, [roomA, roomB]);
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

        PrologueRoom originalRoom = level.CurrentRoom;
        level.RestoreLegacyFixtureCheckpoint(PrologueRoomId.BrokenToothCorridor, "start");
        TestAssert.True(GodotObject.IsInstanceValid(originalRoom) && !originalRoom.IsInsideTree(),
            "first restore temporarily retires the replaced room outside the active tree");

        level.RestoreLegacyFixtureCheckpoint(PrologueRoomId.AwakeningCourtyard, "start");
        TestAssert.True(!GodotObject.IsInstanceValid(originalRoom),
            "subsequent restore releases rooms retired by the previous restore");

        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task CrossingARealExitPreservesThePlayerInstance(SceneTree tree)
    {
        RoomDefinition roomA = Definition(PrologueRoomId.AwakeningCourtyard,
            RuntimeRoom(PrologueRoomId.AwakeningCourtyard, "east", new Vector2(160, 40), false));
        RoomDefinition roomB = Definition(PrologueRoomId.BrokenToothCorridor,
            RuntimeRoom(PrologueRoomId.BrokenToothCorridor, "west", new Vector2(300, 80), false));
        roomA.OutgoingLinks.Add(Link(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "start"));
        roomB.OutgoingLinks.Add(Link(PrologueRoomId.BrokenToothCorridor, "west",
            PrologueRoomId.AwakeningCourtyard, "start"));

        CharacterBody2D player = SimplePlayer();
        PrologueLevelController level = Level(player, [roomA, roomB]);
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        ulong playerId = player.GetInstanceId();

        player.GlobalPosition = new Vector2(160, 40);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

        TestAssert.Equal(PrologueRoomId.BrokenToothCorridor, level.CurrentRoomId,
            "crossing a real RoomExit transitions to its graph destination");
        TestAssert.Equal(playerId, level.Player.GetInstanceId(),
            "room transition preserves the exact persistent player instance");
        TestAssert.True(level.Player.GlobalPosition.DistanceTo(new Vector2(300, 80)) < 0.1f,
            "transition places persistent player at destination entrance marker");
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task LegacyRecoveryWinsOverSameFrameTransition(SceneTree tree)
    {
        RoomDefinition roomA = Definition(PrologueRoomId.AwakeningCourtyard,
            RuntimeRoom(PrologueRoomId.AwakeningCourtyard, "east",
                new Vector2(10000, 10000), false));
        RoomDefinition roomB = Definition(PrologueRoomId.BrokenToothCorridor,
            RuntimeRoom(PrologueRoomId.BrokenToothCorridor, "west",
                new Vector2(10000, 10000), false));
        CharacterBody2D player = SimplePlayer();
        PrologueLevelController level = Level(player, [roomA, roomB]);
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

        LegacyPrologueCheckpointSnapshot checkpoint = level.LegacyCheckpointForFixture;
        player.GlobalPosition = new Vector2(180, 90);
        int requested = 0;
        int completed = 0;
        level.RecoveryRequested += _ => requested++;
        level.RecoveryCompleted += _ => completed++;

        level.RequestRespawn("LEGACY RACE");
        TestAssert.True(requested == 1 && completed == 0 && !player.IsPhysicsProcessing(),
            "legacy recovery is accepted synchronously and stops gameplay before deferred restore");
        level.TransitionTo(new PrologueRoomLink(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "start"));

        int waitFrames = 0;
        while ((completed == 0 || !player.IsPhysicsProcessing()) && waitFrames < 8)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            waitFrames++;
        }
        TestAssert.True(requested == 1 && completed == 1 &&
            level.CurrentRoomId == checkpoint.Room &&
            player.GlobalPosition.DistanceTo(new Vector2(40, 40)) < .1f &&
            player.IsPhysicsProcessing(),
            $"accepted legacy recovery wins the same-frame transition and resumes once; " +
            $"wait_frames={waitFrames} requested={requested} completed={completed} " +
            $"room={level.CurrentRoomId} checkpoint={checkpoint.Room} " +
            $"position={player.GlobalPosition} processing={player.IsPhysicsProcessing()}");

        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task GeneratedMainDeathDefersRestoreAndPreservesRecoveryClosure(SceneTree tree)
    {
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        Exception primaryFailure = null;
        try
        {
        Vector2 savedSceneStart = level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position;
        level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position =
            new Vector2(120f, 574f); // sole pre-tree spawn assignment
        IsolatedPrologueFixture.Configure(level,
            nameof(GeneratedMainDeathDefersRestoreAndPreservesRecoveryClosure));
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        CharacterBody2D player = level.Player;
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerActionModeController actionModes = player.GetNode<PlayerActionModeController>("ActionModes");
        PlayerShieldController shield = player.GetNode<PlayerShieldController>("Shield");
        PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
        PrologueCheckpointSnapshot checkpoint = level.Progress.RestoreCheckpoint();
        TestAssert.True(!checkpoint.HasActivatedCheckpoint,
            "fresh progress uses the authored seamless-world start checkpoint");
        bool checkpointResolved = Prologue.Seamless.CanyonWorldLayout.TryResolveCheckpoint(
            checkpoint.CheckpointId, out var checkpointSector, out Vector2 checkpointPosition);
        TestAssert.True(checkpointResolved && checkpoint.CheckpointId == "canyon.v2.start" &&
            checkpointSector == Prologue.Seamless.PrologueSectorId.CanyonEntrance &&
            checkpoint.SectorId == checkpointSector && checkpointPosition == savedSceneStart,
            "fresh stable checkpoint resolves to the saved scene start, not a later checkpoint or fixture pose");
        var resettable = new RecordingResettable { Name = "DirtyRecoveryActor" };
        level.CurrentSectorRoot.GetNode("Gameplay").AddChild(resettable);

        SendAction(InputActions.MoveRight, true);
        SendAction(InputActions.PrimaryAction, true);
        SendAction(InputActions.Shield, true);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(input.Current.MoveAxis > .5f && input.Current.PrimaryHeld &&
            input.Current.ShieldHeld,
            "recovery fixture begins with real held movement, primary and shield input sampled");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        int requested = 0;
        int completed = 0;
        bool requestSawHeldInput = false;
        bool completionClosedTransients = false;
        bool completionKeptGameplayDisabled = false;
        bool actorStillDeferredAtCompletion = false;
        level.RecoveryRequested += _ =>
        {
            requested++;
            requestSawHeldInput = input.Current.MoveAxis > .5f && input.Current.PrimaryHeld &&
                input.Current.ShieldHeld;
        };
        level.RecoveryCompleted += _ =>
        {
            completed++;
            completionClosedTransients = health.CurrentHealth == health.MaxHealth &&
                input.Current == default && player.Velocity == Vector2.Zero &&
                !combat.IsAttacking && claw.CurrentFrame.State == MechanicalClawState.Idle &&
                !shield.IsActive && energy.EnergyRatio >= .999f && energy.HeatRatio <= .001f &&
                motor.AcceptedDashCount == 0;
            completionKeptGameplayDisabled = !player.IsPhysicsProcessing() &&
                !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing() &&
                !combat.IsPhysicsProcessing() && !claw.IsPhysicsProcessing() &&
                !actionModes.IsPhysicsProcessing() && !shield.IsPhysicsProcessing() &&
                !energy.IsPhysicsProcessing();
            actorStillDeferredAtCompletion = resettable.Dirty && resettable.ResetCount == 0;
        };
        var deathTrace = new List<string>(8);
        bool deathTraceOverflow = false;
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        void TraceDeath(string stage)
        {
            if (deathTrace.Count == 8) { deathTraceOverflow = true; return; }
            deathTrace.Add($"DEATH_RECOVERY_TRACE stage={stage} physics={Engine.GetPhysicsFrames()} " +
                $"sequence={input.CurrentFrameSequence} move={input.Current.MoveAxis} " +
                $"primary={input.Current.PrimaryHeld} shield={input.Current.ShieldHeld} " +
                $"raw_move={input.RawFrame.MoveAxis} raw_primary={input.RawFrame.PrimaryHeld} " +
                $"raw_shield={input.RawFrame.ShieldHeld} " +
                $"map_right={Input.IsActionPressed(InputActions.MoveRight)} " +
                $"map_primary={Input.IsActionPressed(InputActions.PrimaryAction)} " +
                $"map_shield={Input.IsActionPressed(InputActions.Shield)} hp={health.CurrentHealth} " +
                $"claw={claw.CurrentFrame.State} authority={claw.AuthorityRevision} " +
                $"flow={flow.State} requested={requested} completed={completed}");
        }
        // These observers are installed after all existing Ready subscriptions.
        // They describe that point in dispatch, not signal-entry state.
        HealthComponent.DamagedEventHandler damagedTrace = (_, _) => TraceDeath("after_existing_damaged_observers");
        HealthComponent.DiedEventHandler diedTrace = () => TraceDeath("after_existing_died_observers");
        PrologueLevelController.RecoveryRequestedEventHandler requestTrace =
            _ => TraceDeath("recovery_request_callback");
        health.Damaged += damagedTrace;
        health.Died += diedTrace;
        level.RecoveryRequested += requestTrace;
        ulong beforeDeathSequence = input.CurrentFrameSequence;
        try
        {
            TraceDeath("before_damage");
            TestAssert.True(health.ApplyDamage(new DamageRequest(
                    health.CurrentHealth, Vector2.Down, 0f, 920001UL)),
                "public player health path reaches a real generated-main death");
            TraceDeath("after_damage_return");
            TestAssert.True(requested == 1 && completed == 0 && health.CurrentHealth == 0,
                "death publishes one request before deferred restoration");
            // The request carries a reason, not an immutable pre-damage input snapshot.
            // Death invalidates the sampled frames; the held source actions remain held.
            TestAssert.True(input.Current == default && input.RawFrame == default &&
                input.CurrentFrameSequence == beforeDeathSequence &&
                Input.IsActionPressed(InputActions.MoveRight) &&
                Input.IsActionPressed(InputActions.PrimaryAction) &&
                Input.IsActionPressed(InputActions.Shield),
                "death clears Current and Raw without resampling or releasing the held source actions");
            TestAssert.True(!deathTraceOverflow, "death diagnostic preserves the complete synchronous trace");
        }
        finally
        {
            health.Damaged -= damagedTrace;
            health.Died -= diedTrace;
            level.RecoveryRequested -= requestTrace;
            foreach (string row in deathTrace) GD.Print(row);
            GD.Print($"DEATH_RECOVERY_ASSERT_PARTS requested={requested} completed={completed} " +
                $"hp={health.CurrentHealth} request_saw_held={requestSawHeldInput} " +
                $"sampled_closed={input.Current == default} raw_closed={input.RawFrame == default} " +
                $"sequence_same={input.CurrentFrameSequence == beforeDeathSequence} " +
                $"trace_rows={deathTrace.Count} overflow={deathTraceOverflow}");
        }
        bool immediateProcessingClosure = !player.IsPhysicsProcessing() &&
            !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing() &&
            !combat.IsPhysicsProcessing() && !claw.IsPhysicsProcessing() &&
            !actionModes.IsPhysicsProcessing() && !shield.IsPhysicsProcessing() &&
            !energy.IsPhysicsProcessing();
        bool actorDeferredDuringSynchronousPhase = resettable.Dirty && resettable.ResetCount == 0;
        TestAssert.True(immediateProcessingClosure && actorDeferredDuringSynchronousPhase,
            "accepted recovery stops gameplay immediately but does not mutate the scene during the request callback");

        int completionWaitFrames = 0;
        while (completed == 0 && completionWaitFrames < 4)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            completionWaitFrames++;
        }
        PrologueCheckpointSnapshot recoveredCheckpoint = level.Progress.RestoreCheckpoint();
        GD.Print($"DEATH_RECOVERY_CHECKPOINT_FACTS captured_id={checkpoint.CheckpointId} " +
            $"captured_position={checkpoint.WorldPosition} activated={checkpoint.HasActivatedCheckpoint} " +
            $"resolved_sector={checkpointSector} resolved_position={checkpointPosition} " +
            $"saved_scene_start={savedSceneStart} actual={player.GlobalPosition} " +
            $"recovered_id={recoveredCheckpoint.CheckpointId} recovered_activated={recoveredCheckpoint.HasActivatedCheckpoint}");
        TestAssert.True(completed == 1 && completionClosedTransients &&
            completionKeptGameplayDisabled && actorStillDeferredAtCompletion &&
            player.GlobalPosition.DistanceTo(checkpointPosition) < 1.1f &&
            recoveredCheckpoint.CheckpointId == checkpoint.CheckpointId &&
            !recoveredCheckpoint.HasActivatedCheckpoint && recoveredCheckpoint.SectorId == checkpointSector,
            $"deferred recovery completes once with the exact checkpoint and closed transients; " +
            $"wait_frames={completionWaitFrames} completed={completed} " +
            $"transients={completionClosedTransients} disabled={completionKeptGameplayDisabled} " +
            $"actor_pending={actorStillDeferredAtCompletion} position={player.GlobalPosition}");

        int actorWaitFrames = 0;
        while (resettable.ResetCount == 0 && actorWaitFrames < 4)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            actorWaitFrames++;
        }
        bool deferredActorReset = !resettable.Dirty && resettable.ResetCount == 1 &&
            resettable.ResetSector == Prologue.Seamless.PrologueSectorId.CanyonEntrance;

        int resumeWaitFrames = 0;
        while (!input.IsPhysicsProcessing() && resumeWaitFrames < 4)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            resumeWaitFrames++;
        }
        bool resumedAtNextBoundary = input.IsPhysicsProcessing() && motor.IsPhysicsProcessing() &&
            combat.IsPhysicsProcessing() && claw.IsPhysicsProcessing() &&
            actionModes.IsPhysicsProcessing() && shield.IsPhysicsProcessing() &&
            energy.IsPhysicsProcessing();
        bool noStalePrimaryEdge = !input.Current.PrimaryPressed;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        bool actorResetExactlyOnce = resettable.ResetCount == 1;
        TestAssert.True(deferredActorReset && resumedAtNextBoundary && noStalePrimaryEdge &&
            actorResetExactlyOnce,
            $"deferred recovery resets the mounted actor exactly once and resumes without stale input; " +
            $"actor_wait={actorWaitFrames} deferred_actor={deferredActorReset} " +
            $"resume_wait={resumeWaitFrames} resumed_next={resumedAtNextBoundary} " +
            $"stale_primary_blocked={noStalePrimaryEdge} actor_once={actorResetExactlyOnce} " +
            $"input={input.Current} energy={energy.EnergyRatio:0.000} heat={energy.HeatRatio:0.000}");
        GD.Print($"TASK9_POST_GENERATED_MAIN_DEATH_OK checkpoint={checkpointPosition} " +
            $"requested={requested} completed={completed} input_closure=True actor_deferred=1 " +
            $"completion_wait={completionWaitFrames} resume_wait={resumeWaitFrames}");
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                SendAction(InputActions.MoveRight, false);
                SendAction(InputActions.PrimaryAction, false);
                SendAction(InputActions.Shield, false);
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree())
                        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    else level.Free();
                }
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                GD.Print($"DEATH_RECOVERY_FIXTURE_CLEANUP_OK primary_failure={primaryFailure != null} original_tail=true");
            }
            catch (Exception cleanupFailure)
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree()) level.QueueFree();
                    else level.Free();
                }
                if (primaryFailure != null)
                    throw new AggregateException("Generated death assertion and cleanup both failed", primaryFailure, cleanupFailure);
                throw;
            }
        }
    }

    private static PrologueLevelController Level(CharacterBody2D player,
        Godot.Collections.Array<RoomDefinition> definitions)
    {
        var level = new PrologueLevelController
        {
            Name = "PrologueLevel",
            RoomDefinitions = definitions,
            RequiredRooms = [PrologueRoomId.AwakeningCourtyard]
        };
        var playerRoot = new Node2D { Name = "PlayerRoot" };
        player.Name = "Player";
        playerRoot.AddChild(player);
        level.AddChild(playerRoot);
        level.AddChild(new Node2D { Name = "RoomRoot" });
        level.AddChild(new CanvasLayer { Name = "Hud" });
        level.AddChild(new CanvasLayer { Name = "Fade" });
        return level;
    }

    private static CharacterBody2D SimplePlayer()
    {
        var player = new CharacterBody2D
        {
            Name = "Player",
            CollisionLayer = CombatCollisionLayers.Player,
            CollisionMask = CombatCollisionLayers.World
        };
        player.AddChild(new CollisionShape2D
        {
            Name = "BodyCollider",
            Shape = new RectangleShape2D { Size = new Vector2(24, 48) }
        });
        return player;
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static void SendAction(StringName action, bool pressed)
    {
        Input.ParseInputEvent(new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        });
        Input.FlushBufferedEvents();
    }

    private static RoomDefinition Definition(PrologueRoomId id, PackedScene scene) => new()
    {
        RoomId = id,
        Scene = scene,
        StartEntrance = "start",
        CameraBounds = new Rect2(-640, -360, 1280, 720)
    };

    private static PrologueRoomLinkResource Link(PrologueRoomId source, StringName exit,
        PrologueRoomId destination, StringName entrance) => new()
    {
        SourceRoom = source,
        ExitId = exit,
        DestinationRoom = destination,
        EntranceId = entrance
    };

    private static PackedScene RuntimeRoom(PrologueRoomId id, StringName exitId, Vector2 exitPosition,
        bool withKillZone)
    {
        var room = new PrologueRoom
        {
            Name = id.ToString(),
            RoomId = id,
            CameraBounds = new Rect2(-640, -360, 1280, 720)
        };
        var entrances = new Node2D { Name = "Entrances" };
        entrances.AddChild(new Marker2D { Name = "start", Position = id == PrologueRoomId.AwakeningCourtyard ? new Vector2(40, 40) : new Vector2(300, 80) });
        room.AddChild(entrances);
        var exits = new Node2D { Name = "Exits" };
        var exit = new RoomExit
        {
            Name = "Exit",
            ExitId = exitId,
            Position = withKillZone ? new Vector2(10000, 10000) : exitPosition,
            CollisionLayer = 0,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        exit.AddChild(new CollisionShape2D { Name = "Collider", Shape = new RectangleShape2D { Size = new Vector2(80, 100) } });
        exits.AddChild(exit);
        room.AddChild(exits);
        var kill = new World.KillZone
        {
            Name = "KillZone",
            Position = withKillZone ? new Vector2(360, 40) : new Vector2(10000, 10000),
            CollisionLayer = 0,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        kill.AddChild(new CollisionShape2D { Name = "Collider", Shape = new RectangleShape2D { Size = new Vector2(80, 100) } });
        room.AddChild(kill);
        var presentation = new Node2D { Name = "Presentation" };
        var visual = new Node2D { Name = "VisualRoot" };
        visual.AddChild(new Node2D { Name = "Placeholder" });
        presentation.AddChild(visual);
        room.AddChild(presentation);
        Own(room, room);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(room), "runtime synthetic room packs");
        room.Free();
        return packed;
    }

    private static void Own(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            Own(child, owner);
        }
    }
}

internal partial class RecordingResettable : Node, ICheckpointResettable
{
    public bool Dirty { get; private set; } = true;
    public int ResetCount { get; private set; }
    public Prologue.Seamless.PrologueSectorId? ResetSector { get; private set; }

    public void ResetForCheckpoint()
    {
        ResetCount++;
        Dirty = false;
        for (Node current = GetParent(); current != null; current = current.GetParent())
            if (current is Prologue.Seamless.PrologueSector sector)
            {
                ResetSector = sector.SectorId;
                break;
            }
    }
}
