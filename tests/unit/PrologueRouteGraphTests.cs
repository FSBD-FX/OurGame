using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue;

namespace Knight.Tests.Unit;

public static class PrologueRouteGraphTests
{
    public static void Run()
    {
        LiteralSixRoomRouteResolvesEveryBranchAndReturn();
        UnknownExitFailsWithoutInventingALink();
        DuplicateRoomExitKeysAreRejected();
        LinksOutsideProvidedDefinitionsAreRejected();
        RequiredRoomValidationIsStageSpecific();
        RoomDefinitionValidationRejectsMalformedAuthoredData();
        LiteralCourtyardDefinitionIsValid();
    }

    private static void LiteralSixRoomRouteResolvesEveryBranchAndReturn()
    {
        PrologueRouteGraph graph = FullGraph();

        AssertLink(graph, PrologueRoomId.BrokenToothCorridor, "to_upper",
            PrologueRoomId.UpperPendulum, "from_corridor");
        AssertLink(graph, PrologueRoomId.BrokenToothCorridor, "to_lower",
            PrologueRoomId.LowerBuriedMachinery, "from_corridor");
        AssertLink(graph, PrologueRoomId.UpperPendulum, "to_mechanism",
            PrologueRoomId.BellChainMechanism, "from_upper");
        AssertLink(graph, PrologueRoomId.LowerBuriedMachinery, "to_mechanism",
            PrologueRoomId.BellChainMechanism, "from_lower");
        AssertLink(graph, PrologueRoomId.BellChainMechanism, "shortcut",
            PrologueRoomId.BrokenToothCorridor, "checkpoint");
        AssertLink(graph, PrologueRoomId.UpperPendulum, "to_corridor",
            PrologueRoomId.BrokenToothCorridor, "from_upper");
        AssertLink(graph, PrologueRoomId.LowerBuriedMachinery, "to_corridor",
            PrologueRoomId.BrokenToothCorridor, "from_lower");
        AssertLink(graph, PrologueRoomId.BellChainMechanism, "to_upper",
            PrologueRoomId.UpperPendulum, "from_mechanism");
        AssertLink(graph, PrologueRoomId.BellChainMechanism, "to_lower",
            PrologueRoomId.LowerBuriedMachinery, "from_mechanism");
    }

    private static void UnknownExitFailsWithoutInventingALink()
    {
        bool found = FullGraph().TryResolve(PrologueRoomId.AwakeningCourtyard, "missing", out PrologueRoomLink link);
        TestAssert.True(!found, "unknown room exit is rejected");
        TestAssert.Equal(default(PrologueRoomLink), link, "failed lookup returns the default immutable link");
    }

    private static void DuplicateRoomExitKeysAreRejected()
    {
        RoomDefinition courtyard = ValidRoom(PrologueRoomId.AwakeningCourtyard);
        courtyard.OutgoingLinks.Add(Link(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "west"));
        courtyard.OutgoingLinks.Add(Link(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "checkpoint"));
        RoomDefinition corridor = ValidRoom(PrologueRoomId.BrokenToothCorridor);

        AssertThrows<ArgumentException>(() => new PrologueRouteGraph([courtyard, corridor]),
            "duplicate (room, exit) keys are rejected");
    }

    private static void LinksOutsideProvidedDefinitionsAreRejected()
    {
        RoomDefinition courtyard = ValidRoom(PrologueRoomId.AwakeningCourtyard);
        courtyard.OutgoingLinks.Add(Link(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.UpperPendulum, "from_courtyard"));

        AssertThrows<ArgumentException>(() => new PrologueRouteGraph([courtyard]),
            "link destinations must exist in the provided room definitions");
    }

    private static void RequiredRoomValidationIsStageSpecific()
    {
        RoomDefinition courtyard = ValidRoom(PrologueRoomId.AwakeningCourtyard);
        RoomDefinition corridor = ValidRoom(PrologueRoomId.BrokenToothCorridor);
        courtyard.OutgoingLinks.Add(Link(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "from_courtyard"));
        corridor.OutgoingLinks.Add(Link(PrologueRoomId.BrokenToothCorridor, "west",
            PrologueRoomId.AwakeningCourtyard, "from_corridor"));
        var graph = new PrologueRouteGraph([courtyard, corridor]);

        TestAssert.Equal(0, graph.ValidateRequiredRooms(new HashSet<PrologueRoomId>
        {
            PrologueRoomId.AwakeningCourtyard,
            PrologueRoomId.BrokenToothCorridor
        }).Count, "P0 can declare exactly courtyard and corridor");
        IReadOnlyList<string> errors = graph.ValidateRequiredRooms(new HashSet<PrologueRoomId>
        {
            PrologueRoomId.AwakeningCourtyard,
            PrologueRoomId.BrokenToothCorridor,
            PrologueRoomId.FallenBellkeeperArena
        });
        TestAssert.True(Contains(errors, nameof(PrologueRoomId.FallenBellkeeperArena)),
            "a later stage fails until its Boss room definition exists");
    }

    private static void RoomDefinitionValidationRejectsMalformedAuthoredData()
    {
        var cases = new (string Label, Func<RoomDefinition> Create, string NamedError)[]
        {
            ("missing scene", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.Scene = null; return x; }, "Scene"),
            ("empty start entrance", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.StartEntrance = default; return x; }, "StartEntrance"),
            ("zero camera width", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.CameraBounds = new Rect2(0, 0, 0, 720); return x; }, "CameraBounds"),
            ("negative camera height", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.CameraBounds = new Rect2(0, 0, 1280, -1); return x; }, "CameraBounds"),
            ("source mismatch", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.OutgoingLinks.Add(Link(PrologueRoomId.BrokenToothCorridor, "east", PrologueRoomId.BrokenToothCorridor, "west")); return x; }, "SourceRoom"),
            ("null encounter", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.EncounterResources.Add(null); return x; }, "EncounterResources"),
            ("null encounter array", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.EncounterResources = null; return x; }, "EncounterResources"),
            ("null outgoing array", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.OutgoingLinks = null; return x; }, "OutgoingLinks"),
            ("empty authored checkpoint", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.HasCheckpoint = true; x.CheckpointId = default; return x; }, "CheckpointId"),
            ("duplicate exit", () => { RoomDefinition x = ValidRoom(PrologueRoomId.AwakeningCourtyard); x.OutgoingLinks.Add(Link(x.RoomId, "east", PrologueRoomId.BrokenToothCorridor, "one")); x.OutgoingLinks.Add(Link(x.RoomId, "east", PrologueRoomId.BrokenToothCorridor, "two")); return x; }, "east")
        };

        foreach ((string label, Func<RoomDefinition> create, string namedError) in cases)
        {
            IReadOnlyList<string> errors = create().Validate();
            TestAssert.True(Contains(errors, namedError), $"{label} reports its authored field");
        }
    }

    private static void LiteralCourtyardDefinitionIsValid()
    {
        RoomDefinition courtyard = ValidRoom(PrologueRoomId.AwakeningCourtyard);
        courtyard.OutgoingLinks.Add(Link(PrologueRoomId.AwakeningCourtyard, "east",
            PrologueRoomId.BrokenToothCorridor, "from_courtyard"));
        TestAssert.Equal(0, courtyard.Validate().Count,
            "courtyard accepts one start entrance, one outgoing link and no encounter");
    }

    private static PrologueRouteGraph FullGraph()
    {
        RoomDefinition courtyard = ValidRoom(PrologueRoomId.AwakeningCourtyard);
        RoomDefinition corridor = ValidRoom(PrologueRoomId.BrokenToothCorridor);
        RoomDefinition upper = ValidRoom(PrologueRoomId.UpperPendulum);
        RoomDefinition lower = ValidRoom(PrologueRoomId.LowerBuriedMachinery);
        RoomDefinition mechanism = ValidRoom(PrologueRoomId.BellChainMechanism);
        RoomDefinition boss = ValidRoom(PrologueRoomId.FallenBellkeeperArena);

        courtyard.OutgoingLinks.Add(Link(courtyard.RoomId, "to_corridor", corridor.RoomId, "from_courtyard"));
        corridor.OutgoingLinks.Add(Link(corridor.RoomId, "to_courtyard", courtyard.RoomId, "from_corridor"));
        corridor.OutgoingLinks.Add(Link(corridor.RoomId, "to_upper", upper.RoomId, "from_corridor"));
        corridor.OutgoingLinks.Add(Link(corridor.RoomId, "to_lower", lower.RoomId, "from_corridor"));
        upper.OutgoingLinks.Add(Link(upper.RoomId, "to_corridor", corridor.RoomId, "from_upper"));
        upper.OutgoingLinks.Add(Link(upper.RoomId, "to_mechanism", mechanism.RoomId, "from_upper"));
        lower.OutgoingLinks.Add(Link(lower.RoomId, "to_corridor", corridor.RoomId, "from_lower"));
        lower.OutgoingLinks.Add(Link(lower.RoomId, "to_mechanism", mechanism.RoomId, "from_lower"));
        mechanism.OutgoingLinks.Add(Link(mechanism.RoomId, "to_upper", upper.RoomId, "from_mechanism"));
        mechanism.OutgoingLinks.Add(Link(mechanism.RoomId, "to_lower", lower.RoomId, "from_mechanism"));
        mechanism.OutgoingLinks.Add(Link(mechanism.RoomId, "shortcut", corridor.RoomId, "checkpoint"));
        mechanism.OutgoingLinks.Add(Link(mechanism.RoomId, "to_boss", boss.RoomId, "from_mechanism"));
        boss.OutgoingLinks.Add(Link(boss.RoomId, "to_mechanism", mechanism.RoomId, "from_boss"));

        return new PrologueRouteGraph([courtyard, corridor, upper, lower, mechanism, boss]);
    }

    private static RoomDefinition ValidRoom(PrologueRoomId id) => new()
    {
        RoomId = id,
        Scene = PackNode2D(),
        StartEntrance = "start",
        CameraBounds = new Rect2(0, 0, 1280, 720)
    };

    private static PrologueRoomLinkResource Link(
        PrologueRoomId source, StringName exit, PrologueRoomId destination, StringName entrance) => new()
    {
        SourceRoom = source,
        ExitId = exit,
        DestinationRoom = destination,
        EntranceId = entrance
    };

    private static PackedScene PackNode2D()
    {
        var root = new Node2D { Name = "Room" };
        var scene = new PackedScene();
        TestAssert.Equal(Error.Ok, scene.Pack(root), "room test scene packs");
        root.Free();
        return scene;
    }

    private static void AssertLink(PrologueRouteGraph graph, PrologueRoomId source, StringName exit,
        PrologueRoomId destination, StringName entrance)
    {
        TestAssert.True(graph.TryResolve(source, exit, out PrologueRoomLink link), $"{source}/{exit} resolves");
        TestAssert.Equal(destination, link.DestinationRoom, $"{source}/{exit} destination is literal");
        TestAssert.Equal(entrance, link.EntranceId, $"{source}/{exit} entrance is literal");
    }

    private static bool Contains(IReadOnlyList<string> errors, string value)
    {
        foreach (string error in errors)
            if (error.Contains(value, StringComparison.Ordinal)) return true;
        return false;
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Assertion failed: {message}; expected {typeof(T).Name}");
    }
}
