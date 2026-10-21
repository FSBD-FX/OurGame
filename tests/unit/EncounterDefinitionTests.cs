using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue;

namespace Knight.Tests.Unit;

public static class EncounterDefinitionTests
{
    public static void Run()
    {
        EmptySpawnIdIsRejected();
        DuplicateSpawnIdsAreRejected();
        NullPackedSceneIsRejected();
        MissingMarkerPathIsRejected();
        ZeroSpawnsAreRejected();
        MixedEncounterIsAccepted();
        RoomRequiresTypedUniqueEncounters();
    }

    private static void EmptySpawnIdIsRejected()
    {
        EncounterDefinition definition = ValidMixed();
        definition.Spawns[0].SpawnId = default;
        TestAssert.True(Contains(definition.Validate(), "SpawnId"), "empty encounter spawn ID is rejected");
    }

    private static void DuplicateSpawnIdsAreRejected()
    {
        EncounterDefinition definition = ValidMixed();
        definition.Spawns[1].SpawnId = "ground";
        TestAssert.True(Contains(definition.Validate(), "ground"), "duplicate encounter spawn IDs are rejected");
    }

    private static void NullPackedSceneIsRejected()
    {
        EncounterDefinition definition = ValidMixed();
        definition.Spawns[0].Scene = null;
        TestAssert.True(Contains(definition.Validate(), "Scene"), "null encounter packed scene is rejected");
    }

    private static void MissingMarkerPathIsRejected()
    {
        EncounterDefinition definition = ValidMixed();
        definition.Spawns[1].MarkerPath = default;
        TestAssert.True(Contains(definition.Validate(), "MarkerPath"), "missing encounter marker path is rejected");
    }

    private static void ZeroSpawnsAreRejected()
    {
        var definition = new EncounterDefinition { EncounterId = "empty" };
        TestAssert.True(Contains(definition.Validate(), "at least one"), "encounter with zero spawns is rejected");
    }

    private static void MixedEncounterIsAccepted()
    {
        EncounterDefinition definition = ValidMixed();
        TestAssert.Equal(0, definition.Validate().Count,
            "one Rust-Shell and one Grave-Lamp Moth with distinct markers is valid");
        TestAssert.Equal(new StringName("ground"), definition.Spawns[0].SpawnId,
            "ground spawn ID remains literal");
        TestAssert.Equal(new NodePath("../Markers/Ground"), definition.Spawns[0].MarkerPath,
            "ground marker path remains literal");
        TestAssert.Equal(new StringName("air"), definition.Spawns[1].SpawnId,
            "air spawn ID remains literal");
        TestAssert.Equal(new NodePath("../Markers/Air"), definition.Spawns[1].MarkerPath,
            "air marker path remains literal");
    }

    private static void RoomRequiresTypedUniqueEncounters()
    {
        var room = new RoomDefinition
        {
            RoomId = PrologueRoomId.AwakeningCourtyard,
            Scene = Pack(new Node2D { Name = "Room" }),
            StartEntrance = "start"
        };
        room.EncounterResources.Add(new Resource());
        TestAssert.True(Contains(room.Validate(), "EncounterDefinition"),
            "room rejects encounter resources with the wrong type");

        room.EncounterResources.Clear();
        EncounterDefinition first = ValidMixed();
        EncounterDefinition second = ValidMixed();
        first.EncounterId = "ambush";
        second.EncounterId = "ambush";
        room.EncounterResources.Add(first);
        room.EncounterResources.Add(second);
        TestAssert.True(Contains(room.Validate(), "ambush"),
            "room rejects duplicate encounter IDs");
    }

    internal static EncounterDefinition ValidMixed() => new()
    {
        EncounterId = "mixed_intro",
        Spawns = new Godot.Collections.Array<EncounterSpawn>
        {
            new()
            {
                SpawnId = "ground",
                Scene = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn"),
                MarkerPath = new NodePath("../Markers/Ground")
            },
            new()
            {
                SpawnId = "air",
                Scene = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn"),
                MarkerPath = new NodePath("../Markers/Air")
            }
        }
    };

    private static bool Contains(IReadOnlyList<string> errors, string value)
    {
        foreach (string error in errors)
            if (error.Contains(value, StringComparison.Ordinal)) return true;
        return false;
    }

    private static PackedScene Pack(Node root)
    {
        var scene = new PackedScene();
        TestAssert.Equal(Error.Ok, scene.Pack(root), "synthetic room scene packs");
        root.Free();
        return scene;
    }
}
