using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tests.Integration;

namespace Knight.Tests.Unit;

public static class P0RoomStructureTests
{
    private const string CourtyardPath = "res://scenes/prologue/rooms/AwakeningCourtyard.tscn";
    private const string CorridorPath = "res://scenes/prologue/rooms/BrokenToothCorridor.tscn";
    private const string LevelPath = "res://scenes/prologue/PrologueLevel.tscn";

    public static void Run()
    {
        Task7CaseRegistry.Execute(Task7Cases()).GetAwaiter().GetResult();
    }

    internal static IReadOnlyList<Task7Case> Task7Cases() =>
    [
        Task7CaseRegistry.Case(nameof(P0RoomStructureTests), nameof(RequiredPackedScenesExist),
            RequiredPackedScenesExist),
        Task7CaseRegistry.Case(nameof(P0RoomStructureTests), nameof(CourtyardHasLiteralTeachingRoute),
            CourtyardHasLiteralTeachingRoute),
        Task7CaseRegistry.Case(nameof(P0RoomStructureTests),
            nameof(CorridorHasLiteralCombatThreadAndCheckpointRoute),
            CorridorHasLiteralCombatThreadAndCheckpointRoute),
        Task7CaseRegistry.Case(nameof(P0RoomStructureTests),
            nameof(LevelKeepsPlayerPersistentAndLinksBothRooms),
            PrologueCanyonLegacyReplacementTests.PersistentPlayerAndFiveSectorContent)
    ];

    private static void RequiredPackedScenesExist()
    {
        TestAssert.True(ResourceLoader.Exists(CourtyardPath), "P0 courtyard packed scene exists");
        TestAssert.True(ResourceLoader.Exists(CorridorPath), "P0 corridor packed scene exists");
        TestAssert.True(ResourceLoader.Exists(LevelPath), "P0 persistent level packed scene exists");
    }

    private static void CourtyardHasLiteralTeachingRoute()
    {
        PrologueRoom room = LoadRoom(CourtyardPath);
        TestAssert.Equal(PrologueRoomId.AwakeningCourtyard, room.RoomId, "courtyard has strong room ID");
        TestAssert.Equal(new Rect2(-640, -360, 1920, 720), room.CameraBounds,
            "courtyard camera bounds match authored route");
        Platform(room, "Geometry/SpawnFloor", new Vector2(-360, 250), new Vector2(560, 40));
        Platform(room, "Geometry/JumpStep", new Vector2(40, 165), new Vector2(180, 30));
        Platform(room, "Geometry/AttackLedge", new Vector2(255, 120), new Vector2(260, 30));
        TestAssert.True(room.GetNodeOrNull<Marker2D>("Entrances/start") != null,
            "courtyard start entrance exists");
        TestAssert.Equal(new StringName("to_corridor"), room.GetNode<RoomExit>("Exits/ToCorridor").ExitId,
            "courtyard exit ID is stable");
        Tutorial(room, "Tutorials/Move", "move");
        Tutorial(room, "Tutorials/Jump", "jump");
        Tutorial(room, "Tutorials/Attack", "attack");
        TestAssert.True(room.GetNodeOrNull("Presentation/VisualRoot/Placeholder") != null,
            "courtyard has replaceable presentation fallback");
        TestAssert.Equal(0, room.ValidateContract("start").Count, "courtyard runtime contract validates");
        room.Free();
    }

    private static void CorridorHasLiteralCombatThreadAndCheckpointRoute()
    {
        PrologueRoom room = LoadRoom(CorridorPath);
        TestAssert.Equal(PrologueRoomId.BrokenToothCorridor, room.RoomId, "corridor has strong room ID");
        TestAssert.Equal(new Rect2(-640, -360, 2360, 720), room.CameraBounds,
            "corridor camera bounds match authored route");
        Platform(room, "Geometry/EntryFloor", new Vector2(-350, 250), new Vector2(580, 40));
        Platform(room, "Geometry/TeachingLedge", new Vector2(130, 160), new Vector2(260, 30));
        Platform(room, "Geometry/AnchorLanding", new Vector2(650, 250), new Vector2(400, 40));
        TraversalSegment2D entryJump = room.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_floor_to_teaching_ledge");
        TestAssert.Equal(TraversalSegmentKind.SingleJump, entryJump.Kind,
            "corridor entry-to-teaching segment is a literal SingleJump contract");
        TestAssert.Equal(TraversalAbility.Jump, entryJump.RequiredAbilities,
            "corridor entry-to-teaching segment requires Jump and no hidden Dash");
        TestAssert.Equal(new Rect2(-126, 182, 48, 48), entryJump.LaunchZone,
            "corridor entry-to-teaching launch band hugs the supported EntryFloor edge");
        TestAssert.Equal(new Rect2(40, 97, 96, 48), entryJump.LandingZone,
            "corridor entry-to-teaching landing band tracks the lowered TeachingLedge");
        TraversalSegment2D anchorRoute = room.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_to_anchor_landing");
        TestAssert.Equal(new Rect2(140, 97, 96, 48), anchorRoute.LaunchZone,
            "corridor assisted launch band tracks the lowered TeachingLedge");
        TestAssert.True(room.GetNodeOrNull<RustShellSquire>("Actors/RustShellSquire") != null,
            "corridor contains exactly one formal Rust-Shell fixture");
        RustShellSquire rustShell = room.GetNode<RustShellSquire>("Actors/RustShellSquire");
        AssertAttackTelegraph(rustShell, "Rust-Shell");
        RustShellSettings tuning = new();
        TestAssert.Equal(.65f, tuning.TelegraphSeconds,
            "generated Rust-Shell uses the approved 0.65 second preparation");
        TestAssert.Equal(.14f, tuning.AttackSeconds,
            "generated Rust-Shell uses the approved 0.14 second active window");
        TestAssert.Equal(.65f, tuning.RecoverySeconds,
            "generated Rust-Shell uses the approved 0.65 second recovery");
        TestAssert.Equal(1, tuning.ContactDamage,
            "generated Rust-Shell attack damage remains one first-level health cell");
        TestAssert.Equal(3, rustShell.GetNode<HealthComponent>("Health").MaxHealth,
            "generated Rust-Shell health remains the approved first-level value");
        TestAssert.Equal(1, rustShell.GetNode<Hitbox>("AttackHitbox").Damage,
            "generated Rust-Shell hitbox deals one first-level health cell");
        TestAssert.Equal(1, room.GetNode("Actors").GetChildCount(), "corridor has one P0 enemy");
        ThreadTarget2D anchor = room.GetNode<ThreadTarget2D>("Geometry/ThreadAnchor");
        TestAssert.Equal(ThreadTargetKind.Anchor, anchor.Kind, "corridor thread target is an anchor");
        TestAssert.Equal(0f, anchor.FallbackAimAssistRange,
            "ordinary corridor anchor keeps fallback aim assist disabled");
        TestAssert.Equal(new Vector2(480, -100), anchor.Position, "anchor forces the authored gap interaction");
        CheckpointTrigger checkpoint = room.GetNode<CheckpointTrigger>("Checkpoint");
        TestAssert.Equal(new StringName("checkpoint"), checkpoint.EntranceId,
            "corridor checkpoint respawns at stable checkpoint entrance");
        TestAssert.Equal(new Vector2(1120, 198), checkpoint.Position,
            "corridor checkpoint trigger stands on the supported CompletionFloor");
        TestAssert.True(room.GetNodeOrNull<Marker2D>("Entrances/from_courtyard") != null,
            "corridor courtyard entrance exists");
        Marker2D checkpointEntrance = room.GetNodeOrNull<Marker2D>("Entrances/checkpoint");
        TestAssert.True(checkpointEntrance != null, "corridor checkpoint entrance exists");
        TestAssert.Equal(new Vector2(1120, 198), checkpointEntrance.Position,
            "corridor checkpoint entrance shares the trigger's supported CompletionFloor point");
        TestAssert.Equal(new StringName("to_courtyard"), room.GetNode<RoomExit>("Exits/ToCourtyard").ExitId,
            "corridor reverse exit allows backtracking");
        TestAssert.Equal(new StringName("p0_complete"), room.GetNode<RoomExit>("Exits/P0Complete").ExitId,
            "corridor P0 completion exit is stable");
        Tutorial(room, "Tutorials/ThreadAnchor", "thread_anchor");
        TestAssert.True(room.GetNodeOrNull("Presentation/VisualRoot/Placeholder") != null,
            "corridor has replaceable presentation fallback");
        TestAssert.Equal(0, room.ValidateContract("from_courtyard").Count, "corridor runtime contract validates");
        room.Free();
    }

    private static void LevelKeepsPlayerPersistentAndLinksBothRooms()
    {
        PrologueLevelController level = GD.Load<PackedScene>(LevelPath).Instantiate<PrologueLevelController>();
        CharacterBody2D player = level.GetNodeOrNull<CharacterBody2D>("PlayerRoot/Player");
        TestAssert.True(player != null,
            "Player lives under persistent PlayerRoot");
        TestAssert.True(player.GetNodeOrNull("Presentation") != null &&
            player.GetNodeOrNull<PlayerFormalVisual>("VisualRoot/Formal") != null &&
            player.GetNodeOrNull("VisualRoot/Effects") != null,
            "persistent Player preserves the replaceable presentation boundary");
        TestAssert.True(level.GetNodeOrNull<Node2D>("RoomRoot") != null, "level has stable RoomRoot");
        TestAssert.True(level.GetNodeOrNull<PrologueHud>("Hud") != null, "level has persistent HUD");
        TestAssert.Equal(6, level.RoomDefinitions.Count,
            "expanded level preserves the two P0 rooms inside all six prologue room definitions");
        TestAssert.Equal(6, level.RequiredRooms.Count,
            "expanded required room set includes P0, P1 and the Boss arena");
        TestAssert.Equal(new StringName("p0_complete"), level.MilestoneCompletionExitId,
            "P0 completion feedback is configured instead of hard-coded in room logic");
        TestAssert.Equal("P0 CORE SLICE COMPLETE", level.MilestoneCompletionObjective,
            "P0 completion feedback text is replaceable at the level boundary");
        RoomDefinition courtyard = Find(level, PrologueRoomId.AwakeningCourtyard);
        RoomDefinition corridor = Find(level, PrologueRoomId.BrokenToothCorridor);
        TestAssert.Equal("res://scenes/prologue/rooms/AwakeningCourtyard.tscn", courtyard.Scene.ResourcePath,
            "courtyard definition points to generated room");
        TestAssert.Equal("res://scenes/prologue/rooms/BrokenToothCorridor.tscn", corridor.Scene.ResourcePath,
            "corridor definition points to generated room");
        TestAssert.True(HasLink(courtyard, "to_corridor", PrologueRoomId.BrokenToothCorridor, "from_courtyard"),
            "P0 forward route is literal");
        TestAssert.True(HasLink(corridor, "to_courtyard", PrologueRoomId.AwakeningCourtyard, "from_corridor"),
            "P0 reverse route is literal");
        level.Free();
    }

    private static PrologueRoom LoadRoom(string path) => GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();

    private static void AssertAttackTelegraph(Node actor, string label)
    {
        TestAssert.Equal(1, Count<AttackTelegraphVisual>(actor),
            $"{label} owns exactly one recursive AttackTelegraphVisual");
        Node telegraphNode = actor.GetNodeOrNull("VisualRoot/AttackTelegraph");
        TestAssert.True(telegraphNode is AttackTelegraphVisual,
            $"{label} exposes the scripted VisualRoot/AttackTelegraph path");
        TestAssert.True(telegraphNode is not CollisionObject2D && telegraphNode is not Control,
            $"{label} telegraph cannot own collision layers, masks, or mouse input");
        Node2D formal = actor.GetNode<Node2D>("VisualRoot/Formal");
        Node2D telegraph = (Node2D)telegraphNode;
        Node2D effects = actor.GetNode<Node2D>("VisualRoot/Effects");
        TestAssert.Equal(6, telegraph.ZIndex,
            $"{label} telegraph renders above all gameplay skin layers");
        TestAssert.Equal(10, effects.ZIndex,
            $"{label} hit flashes remain above the attack telegraph");
        TestAssert.True(formal.ZIndex < telegraph.ZIndex && telegraph.ZIndex < effects.ZIndex,
            $"{label} visual stack is Formal then AttackTelegraph then Effects");
    }

    private static int Count<T>(Node root) where T : Node
    {
        int count = root is T ? 1 : 0;
        foreach (Node child in root.GetChildren()) count += Count<T>(child);
        return count;
    }

    private static void Platform(Node root, string path, Vector2 position, Vector2 size)
    {
        StaticBody2D body = root.GetNode<StaticBody2D>(path);
        RectangleShape2D shape = body.GetNode<CollisionShape2D>("Collision").Shape as RectangleShape2D;
        TestAssert.Equal(position, body.Position, $"{path} position is authored literally");
        TestAssert.Equal(size, shape?.Size ?? Vector2.Zero, $"{path} size is authored literally");
    }

    private static void Tutorial(Node root, string path, StringName id) =>
        TestAssert.Equal(id, root.GetNode<TutorialPromptTrigger>(path).TutorialId, $"{path} tutorial ID is stable");

    private static RoomDefinition Find(PrologueLevelController level, PrologueRoomId id)
    {
        foreach (RoomDefinition definition in level.RoomDefinitions)
            if (definition.RoomId == id) return definition;
        throw new InvalidOperationException($"Missing room definition {id}");
    }

    private static bool HasLink(RoomDefinition definition, StringName exitId,
        PrologueRoomId destination, StringName entrance)
    {
        foreach (PrologueRoomLinkResource authored in definition.OutgoingLinks)
            if (authored.ExitId == exitId && authored.DestinationRoom == destination && authored.EntranceId == entrance)
                return true;
        return false;
    }
}
