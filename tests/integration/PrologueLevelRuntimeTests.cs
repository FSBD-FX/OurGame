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
    public static async Task Run(SceneTree tree)
    {
        await OverlappingLevelsCoordinateAndRestoreSustainedLowLatency(tree);
        await CrossingARealExitPreservesThePlayerInstance(tree);
        await RepeatedTransitionsReleaseEarlierRetiredRooms(tree);
        await RepeatedRestoreReleasesPreviouslyRetiredRooms(tree);
        await GeneratedMainDeathRestoresExplicitProgressCheckpointInSamePhysicsFrame(tree);
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

    private static async Task GeneratedMainDeathRestoresExplicitProgressCheckpointInSamePhysicsFrame(SceneTree tree)
    {
        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position =
            new Vector2(120f, 574f); // sole pre-tree spawn assignment
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
        int requested = 0;
        int completed = 0;
        level.RecoveryRequested += _ => requested++;
        level.RecoveryCompleted += _ => completed++;
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ApplyDamage(new DamageRequest(
                health.CurrentHealth, Vector2.Down, 0f, 920001UL)),
            "public player health path reaches a real generated-main death");

        TestAssert.True(player.GlobalPosition.DistanceTo(checkpoint.WorldPosition) < 1.1f,
            "real generated-main death returns player to the explicit progress checkpoint");
        TestAssert.True(requested == 1 && completed == 1 && health.CurrentHealth == health.MaxHealth,
            "generated-main death requests, completes, and restores full health in the same physics frame");
        bool immediateTransientClosure = input.Current == default &&
            player.Velocity == Vector2.Zero && !combat.IsAttacking &&
            claw.CurrentFrame.State == MechanicalClawState.Idle && !shield.IsActive &&
            energy.EnergyRatio >= .999f && energy.HeatRatio <= .001f &&
            motor.AcceptedDashCount == 0;
        bool immediateProcessingClosure = !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing() &&
            !combat.IsPhysicsProcessing() && !claw.IsPhysicsProcessing() &&
            !actionModes.IsPhysicsProcessing() && !shield.IsPhysicsProcessing() &&
            !energy.IsPhysicsProcessing();
        bool actorDeferredDuringSynchronousPhase = resettable.Dirty && resettable.ResetCount == 0;

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        bool endOfCurrentFrameClosure = input.Current == default && player.Velocity == Vector2.Zero &&
            energy.EnergyRatio >= .999f && energy.HeatRatio <= .001f && !shield.IsActive &&
            !input.IsPhysicsProcessing() && !motor.IsPhysicsProcessing() &&
            !combat.IsPhysicsProcessing() && !claw.IsPhysicsProcessing() &&
            !actionModes.IsPhysicsProcessing() && !shield.IsPhysicsProcessing() &&
            !energy.IsPhysicsProcessing();
        bool deferredActorReset = !resettable.Dirty && resettable.ResetCount == 1 &&
            resettable.ResetSector == Prologue.Seamless.PrologueSectorId.CanyonEntrance;

        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        bool resumedAtNextBoundary = input.IsPhysicsProcessing() && motor.IsPhysicsProcessing() &&
            combat.IsPhysicsProcessing() && claw.IsPhysicsProcessing() &&
            actionModes.IsPhysicsProcessing() && shield.IsPhysicsProcessing() &&
            energy.IsPhysicsProcessing();
        bool noStalePrimaryEdge = !input.Current.PrimaryPressed;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        bool actorResetExactlyOnce = resettable.ResetCount == 1;
        TestAssert.True(immediateTransientClosure && immediateProcessingClosure &&
            actorDeferredDuringSynchronousPhase && endOfCurrentFrameClosure &&
            deferredActorReset && resumedAtNextBoundary && noStalePrimaryEdge &&
            actorResetExactlyOnce,
            $"same-frame recovery closes held input and defers mounted actor reset exactly once; " +
            $"immediate_transients={immediateTransientClosure} immediate_processing={immediateProcessingClosure} " +
            $"sync_actor_zero={actorDeferredDuringSynchronousPhase} end_frame={endOfCurrentFrameClosure} " +
            $"deferred_actor={deferredActorReset} resumed_next={resumedAtNextBoundary} " +
            $"stale_primary_blocked={noStalePrimaryEdge} actor_once={actorResetExactlyOnce} " +
            $"input={input.Current} energy={energy.EnergyRatio:0.000} heat={energy.HeatRatio:0.000}");
        GD.Print($"TASK9_POST_GENERATED_MAIN_DEATH_OK checkpoint={checkpoint.WorldPosition} " +
            $"requested={requested} completed={completed} input_closure=True actor_deferred=1");
        SendAction(InputActions.MoveRight, false);
        SendAction(InputActions.PrimaryAction, false);
        SendAction(InputActions.Shield, false);
        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
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
