using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Traversal;
using Knight.Tests.Integration;

namespace Knight.Tests.Unit;

public static class P1LoopStructureTests
{
    private const string UpperPath = "res://scenes/prologue/rooms/UpperPendulum.tscn";
    private const string LowerPath = "res://scenes/prologue/rooms/LowerBuriedMachinery.tscn";
    private const string MechanismPath = "res://scenes/prologue/rooms/BellChainMechanism.tscn";
    private const string LevelPath = "res://scenes/prologue/PrologueLevel.tscn";

    public static void Run()
    {
        Task7CaseRegistry.Execute(Task7Cases()).GetAwaiter().GetResult();
    }

    internal static IReadOnlyList<Task7Case> Task7Cases() =>
    [
        Task7CaseRegistry.Case(nameof(P1LoopStructureTests), nameof(NewPackedRoomsExist),
            NewPackedRoomsExist),
        Task7CaseRegistry.Case(nameof(P1LoopStructureTests),
            nameof(UpperRouteHasVerticalThreadAndHiddenContracts),
            UpperRouteHasVerticalThreadAndHiddenContracts),
        Task7CaseRegistry.Case(nameof(P1LoopStructureTests),
            nameof(LowerRouteHasMixedEncounterContracts), LowerRouteHasMixedEncounterContracts),
        Task7CaseRegistry.Case(nameof(P1LoopStructureTests),
            nameof(GeneratedEnemiesKeepAttackReadabilityContracts),
            GeneratedEnemiesKeepAttackReadabilityContracts),
        Task7CaseRegistry.Case(nameof(P1LoopStructureTests),
            nameof(MechanismStubConvergesBothBranchesSafely),
            MechanismStubConvergesBothBranchesSafely),
        Task7CaseRegistry.Case(nameof(P1LoopStructureTests),
            nameof(FormalLevelLinksEitherBranchAndTheUnchosenReturn),
            PrologueCanyonLegacyReplacementTests.FiveSectorSeamsFormOneContinuousU)
    ];

    private static void NewPackedRoomsExist()
    {
        TestAssert.True(ResourceLoader.Exists(UpperPath), "upper pendulum packed room exists");
        TestAssert.True(ResourceLoader.Exists(LowerPath), "lower buried machinery packed room exists");
        TestAssert.True(ResourceLoader.Exists(MechanismPath), "bell-chain convergence packed room exists");
    }

    private static void UpperRouteHasVerticalThreadAndHiddenContracts()
    {
        PrologueRoom room = Load(UpperPath);
        TestAssert.Equal(PrologueRoomId.UpperPendulum, room.RoomId, "upper route room ID is literal");
        TestAssert.Equal(new Rect2(-640, -720, 2400, 1080), room.CameraBounds,
            "upper route camera bounds preserve authored vertical space");
        Entrance(room, "from_corridor", new Vector2(-540, 198));
        Entrance(room, "from_mechanism", new Vector2(970, 68));
        Exit(room, "ToCorridor", "to_corridor");
        Exit(room, "ToMechanism", "to_mechanism");
        Anchor(room, "Geometry/AnchorLow", new Vector2(170, -90));
        Anchor(room, "Geometry/AnchorHigh", new Vector2(360, -50));
        Anchor(room, "Geometry/ReboundTarget", new Vector2(580, -60));
        var hidden = room.GetNode<HiddenAlcoveTrigger>("HiddenAlcove");
        TestAssert.Equal(new StringName("upper_hidden_alcove"), hidden.DiscoveryId,
            "hidden alcove discovery ID is stable");
        TestAssert.Equal(new Vector2(930, -38), hidden.Position, "hidden alcove trigger location is literal");
        TestAssert.True(room.GetNodeOrNull("HiddenAlcove/Reward") == null,
            "hidden alcove has no required gameplay-stat reward");
        TestAssert.Equal(1, room.GetNode("SpawnMarkers").GetChildCount(),
            "upper route authors one aerial encounter marker");
        Node authoredActor = room.GetNode<EncounterController>("EncounterController").Definition.Spawns[0].Scene.Instantiate();
        TestAssert.True(authoredActor is GraveLampMoth, "upper encounter is a formal Grave-Lamp Moth");
        authoredActor.Free();
        TestAssert.Equal(0, room.ValidateContract("from_corridor").Count,
            "upper room validates through the common room boundary");
        room.Free();
    }

    private static void LowerRouteHasMixedEncounterContracts()
    {
        PrologueRoom room = Load(LowerPath);
        TestAssert.Equal(PrologueRoomId.LowerBuriedMachinery, room.RoomId, "lower route room ID is literal");
        TestAssert.Equal(new Rect2(-640, -360, 2400, 720), room.CameraBounds,
            "lower route camera bounds are literal");
        Entrance(room, "from_corridor", new Vector2(-540, 198));
        Entrance(room, "from_mechanism", new Vector2(1480, 198));
        Exit(room, "ToCorridor", "to_corridor");
        Exit(room, "ToMechanism", "to_mechanism");
        TestAssert.Equal(new Vector2(520, 198), room.GetNode<Marker2D>("SpawnMarkers/Ground").Position,
            "lower ground enemy marker is safely beyond the entrance");
        TestAssert.Equal(new Vector2(920, -80), room.GetNode<Marker2D>("SpawnMarkers/Air").Position,
            "lower moth marker leaves dodge space below");
        EncounterDefinition definition = room.GetNode<EncounterController>("EncounterController").Definition;
        TestAssert.Equal(2, definition.Spawns.Count, "lower encounter authors exactly two enemies");
        TestAssert.True(definition.Spawns[0].Scene.ResourcePath.EndsWith("RustShellSquire.tscn", StringComparison.Ordinal),
            "lower encounter includes one Rust-Shell");
        TestAssert.True(definition.Spawns[1].Scene.ResourcePath.EndsWith("GraveLampMoth.tscn", StringComparison.Ordinal),
            "lower encounter includes one Grave-Lamp Moth");
        TestAssert.True(room.GetNode<Marker2D>("Entrances/from_corridor").Position
                .DistanceTo(room.GetNode<Marker2D>("SpawnMarkers/Ground").Position) > 800f,
            "lower encounter cannot deal unavoidable contact damage at entry");
        TestAssert.Equal(0, room.ValidateContract("from_corridor").Count,
            "lower room validates through the common room boundary");
        room.Free();
    }

    private static void MechanismStubConvergesBothBranchesSafely()
    {
        PrologueRoom room = Load(MechanismPath);
        TestAssert.Equal(PrologueRoomId.BellChainMechanism, room.RoomId, "mechanism stub room ID is literal");
        Entrance(room, "from_upper", new Vector2(-500, 108));
        Entrance(room, "from_lower", new Vector2(-500, 238));
        Exit(room, "ToUpper", "to_upper");
        Exit(room, "ToLower", "to_lower");
        Exit(room, "P1Complete", "p1_complete");
        Exit(room, "Shortcut", "shortcut");
        StaticBody2D completionDais = room.GetNode<StaticBody2D>("Geometry/CompletionDais");
        TestAssert.Equal(new Vector2(900, 200), completionDais.Position,
            "mechanism CompletionDais is one supported SingleJump above LowerFloor");
        TestAssert.Equal(new Vector2(320, 28),
            ((RectangleShape2D)completionDais.GetNode<CollisionShape2D>("Collision").Shape).Size,
            "mechanism CompletionDais preserves its authored horizontal span");
        TraversalSegment2D bossApproach = room.GetNode<TraversalSegment2D>(
            "TraversalContracts/chain_floor_to_completion_dais");
        TestAssert.Equal(TraversalSegmentKind.SingleJump, bossApproach.Kind,
            "mechanism boss approach is a literal SingleJump contract");
        TestAssert.Equal(TraversalAbility.Jump, bossApproach.RequiredAbilities,
            "mechanism boss approach requires Jump without hidden Dash or Thread");
        TestAssert.Equal(new Rect2(636, 222, 48, 48), bossApproach.LaunchZone,
            "mechanism boss approach exposes a supported edge launch band");
        TestAssert.Equal(new Rect2(740, 138, 96, 48), bossApproach.LandingZone,
            "mechanism boss approach lands on the lowered CompletionDais");
        TestAssert.True(room.GetNodeOrNull("Actors") != null,
            "convergence room exposes stable encounter actor root");
        TestAssert.True(room.GetNodeOrNull("Mechanisms/BellChain") != null,
            "convergence room now contains the formal bell-chain mechanism");
        ThreadTarget2D mechanismTarget = room.GetNode<ThreadTarget2D>(
            "Mechanisms/BellChain/ThreadTarget");
        TestAssert.Equal(260f, mechanismTarget.FallbackAimAssistRange,
            "first-level Bell Chain alone opts into 260 pixel fallback assist");
        TestAssert.True(room.GetNode<RoomExit>("Exits/Shortcut").Position.X >
            room.GetNode<Node2D>("Mechanisms/BellChain").Position.X + 150f,
            "shortcut trigger is physically behind the closed mechanism door");
        TestAssert.True(room.GetNodeOrNull<RoomExit>("Exits/ToBoss") == null,
            "Task 7 convergence stub has no Boss exit before the arena exists");
        room.Free();
    }

    private static void GeneratedEnemiesKeepAttackReadabilityContracts()
    {
        GraveLampMoth moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn")
            .Instantiate<GraveLampMoth>();
        AssertAttackTelegraph(moth, "Grave-Lamp Moth");
        TestAssert.Equal(2, moth.GetNode<HealthComponent>("Health").MaxHealth,
            "generated Grave-Lamp Moth health remains the approved first-level value");
        TestAssert.Equal(1, moth.GetNode<Hitbox>("AttackHitbox").Damage,
            "generated Grave-Lamp Moth hitbox deals one first-level health cell");
        TestAssert.Equal(.70f, moth.Tuning.TelegraphSeconds,
            "generated Grave-Lamp Moth uses the approved 0.70 second preparation");
        TestAssert.Equal(.35f, moth.Tuning.AttackSeconds,
            "generated Grave-Lamp Moth uses the approved 0.35 second active window");
        TestAssert.Equal(.70f, moth.Tuning.RecoverySeconds,
            "generated Grave-Lamp Moth uses the approved 0.70 second recovery");
        TestAssert.Equal(1, moth.Tuning.ContactDamage,
            "generated Grave-Lamp Moth contact damage remains one first-level health cell");
        moth.Free();

        BellBackLaborer laborer = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
            .Instantiate<BellBackLaborer>();
        AssertAttackTelegraph(laborer, "Bell-Back Laborer");
        TestAssert.Equal(6, laborer.GetNode<HealthComponent>("Health").MaxHealth,
            "generated Bell-Back Laborer health remains the approved first-level value");
        TestAssert.Equal(1, laborer.GetNode<Hitbox>("AttackHitbox").Damage,
            "generated Bell-Back Laborer hitbox deals one first-level health cell");
        TestAssert.True(laborer.GetNode<Hitbox>("AttackHitbox").GetNode<CollisionShape2D>("AttackCollider").Shape
            is ConvexPolygonShape2D fan && fan.Points.Length >= 5,
            "generated Bell-Back Laborer serializes the authoritative convex fan collider");
        TestAssert.Equal(.75f, laborer.Tuning.TelegraphSeconds,
            "generated Bell-Back Laborer uses the approved 0.75 second preparation");
        TestAssert.Equal(.20f, laborer.Tuning.AttackSeconds,
            "generated Bell-Back Laborer uses the approved 0.20 second active window");
        TestAssert.Equal(.70f, laborer.Tuning.RecoverySeconds,
            "generated Bell-Back Laborer uses the approved 0.70 second recovery");
        TestAssert.Equal(1, laborer.Tuning.ContactDamage,
            "generated Bell-Back Laborer contact damage remains one first-level health cell");
        laborer.Free();
    }

    private static void FormalLevelLinksEitherBranchAndTheUnchosenReturn()
    {
        PrologueLevelController level = GD.Load<PackedScene>(LevelPath).Instantiate<PrologueLevelController>();
        TestAssert.Equal(6, level.RoomDefinitions.Count,
            "P1 loop remains intact inside the complete six-room prologue route");
        RoomDefinition corridor = Find(level, PrologueRoomId.BrokenToothCorridor);
        RoomDefinition upper = Find(level, PrologueRoomId.UpperPendulum);
        RoomDefinition lower = Find(level, PrologueRoomId.LowerBuriedMachinery);
        RoomDefinition mechanism = Find(level, PrologueRoomId.BellChainMechanism);
        Link(corridor, "to_upper", PrologueRoomId.UpperPendulum, "from_corridor");
        Link(corridor, "to_lower", PrologueRoomId.LowerBuriedMachinery, "from_corridor");
        Link(upper, "to_mechanism", PrologueRoomId.BellChainMechanism, "from_upper");
        Link(lower, "to_mechanism", PrologueRoomId.BellChainMechanism, "from_lower");
        Link(mechanism, "to_upper", PrologueRoomId.UpperPendulum, "from_mechanism");
        Link(mechanism, "to_lower", PrologueRoomId.LowerBuriedMachinery, "from_mechanism");
        Link(mechanism, "shortcut", PrologueRoomId.BrokenToothCorridor, "checkpoint");
        Link(upper, "to_corridor", PrologueRoomId.BrokenToothCorridor, "from_upper");
        Link(lower, "to_corridor", PrologueRoomId.BrokenToothCorridor, "from_lower");
        level.Free();
    }

    private static PrologueRoom Load(string path) => GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();

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

    private static void Entrance(Node room, string id, Vector2 position) =>
        TestAssert.Equal(position, room.GetNode<Marker2D>($"Entrances/{id}").Position,
            $"entrance {id} is literal");

    private static void Exit(Node room, string nodeName, StringName exitId) =>
        TestAssert.Equal(exitId, room.GetNode<RoomExit>($"Exits/{nodeName}").ExitId,
            $"exit {nodeName} is stable");

    private static void Anchor(Node room, string path, Vector2 position)
    {
        ThreadTarget2D anchor = room.GetNode<ThreadTarget2D>(path);
        TestAssert.Equal(ThreadTargetKind.Anchor, anchor.Kind, $"{path} remains an anchor");
        TestAssert.Equal(position, anchor.Position, $"{path} position is literal");
    }

    private static RoomDefinition Find(PrologueLevelController level, PrologueRoomId id)
    {
        foreach (RoomDefinition definition in level.RoomDefinitions)
            if (definition.RoomId == id) return definition;
        throw new InvalidOperationException($"Missing room definition {id}");
    }

    private static void Link(RoomDefinition source, StringName exit, PrologueRoomId destination, StringName entrance)
    {
        foreach (PrologueRoomLinkResource link in source.OutgoingLinks)
            if (link.ExitId == exit && link.DestinationRoom == destination && link.EntranceId == entrance) return;
        throw new InvalidOperationException($"Assertion failed: missing {source.RoomId}/{exit} -> {destination}/{entrance}");
    }
}
