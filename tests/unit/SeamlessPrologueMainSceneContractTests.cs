using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class SeamlessPrologueMainSceneContractTests
{
    private const string LevelPath = "res://scenes/prologue/PrologueLevel.tscn";

    public static void Run(PackedScene packed)
    {
        TestAssert.True(packed != null, "Task 7 main prologue scene is loadable");
        TestAssert.Equal(LevelPath, packed.ResourcePath,
            "Task 7 contract audits the generated main prologue resource");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try
        {
            TestAssert.Equal(new NodePath("WorldRoot/Sectors"), level.SeamlessWorldPath,
                "main prologue delegates lifecycle to the stable seamless-world path");
            SeamlessPrologueWorld world = level.GetNodeOrNull<SeamlessPrologueWorld>(
                "WorldRoot/Sectors");
            TestAssert.True(world != null,
                "main prologue mounts SeamlessPrologueWorld at WorldRoot/Sectors");
            TestAssert.True(level.GetNodeOrNull("RoomRoot") == null,
                "continuous main path does not expose the retired RoomRoot switcher");
            TestAssert.Equal(0, level.RoomDefinitions.Count,
                "continuous main path does not serialize legacy room definitions");
            TestAssert.Equal(0, level.RequiredRooms.Count,
                "continuous main path does not serialize a legacy required-room set");

            CharacterBody2D player = level.GetNodeOrNull<CharacterBody2D>("PlayerRoot/Player");
            TestAssert.True(player != null && player.GetParent() == level.GetNode("PlayerRoot"),
                "persistent player remains a direct PlayerRoot child outside streamed sectors");
            TestAssert.Equal(5, world.Definitions.Count,
                "main prologue serializes exactly five Task 7 sector definitions");

            PrologueSectorId[] expected = Enum.GetValues<PrologueSectorId>();
            var definitions = new List<PrologueSectorDefinition>();
            for (int index = 0; index < world.Definitions.Count; index++)
            {
                PrologueSectorDefinition definition = world.Definitions[index];
                TestAssert.True(definition != null && definition.Scene != null,
                    $"sector definition {index} has a verifiable placeholder scene");
                TestAssert.Equal(expected[index], definition.Id,
                    $"sector definition {index} preserves exact route order");
                Node placeholder = definition.Scene.Instantiate();
                try
                {
                    TestAssert.Equal(expected[index].ToString(), placeholder.Name.ToString(),
                        $"sector definition {index} placeholder root has the exact sector ID");
                }
                finally
                {
                    placeholder.Free();
                }
                definitions.Add(definition);
            }

            _ = new PrologueSectorStreamModel(definitions);
            GD.Print("TASK7_SEAMLESS_MAIN_CONTRACT_OK path=WorldRoot/Sectors definitions=5 " +
                     "legacy_room_root=False");
        }
        finally
        {
            level.Free();
        }
    }

    public static void VerifyProcessLifetimeOwner(PackedScene packed, int repetitions = 8)
    {
        TestAssert.True(packed != null && GodotObject.IsInstanceValid(packed),
            "runner-owned main PackedScene remains valid for the post-phase process lifetime");
        double nodesBefore = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        for (int index = 0; index < repetitions; index++)
        {
            PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
            TestAssert.True(level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors") != null,
                $"repeated main instance {index} retains seamless wiring");
            level.Free();
        }
        double nodesAfter = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        TestAssert.True(nodesAfter <= nodesBefore + 2d,
            $"repeated main instances return to a stable node baseline; " +
            $"before={nodesBefore} after={nodesAfter}");
        TestAssert.True(GodotObject.IsInstanceValid(packed),
            "process-lifetime owner remains valid after repeated instantiate/free");
        GD.Print($"TASK7_MAIN_PACKEDSCENE_LIFETIME_OK repetitions={repetitions} " +
                 $"owner=RunPrologueTests nodes_before={nodesBefore} nodes_after={nodesAfter}");
    }
}
