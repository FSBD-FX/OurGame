using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class BasinSampleLayoutIsolationTests
{
    public static async Task Run(SceneTree tree)
    {
        AssertFrozenHashes();
        string directory = Path.Combine(Path.GetTempPath(), "knight-frozen-sample-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        BasinQualitySampleEntry entry = null;
        try
        {
            entry = await Create(tree, directory);
            PrologueLevelController level = entry.Level;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.Equal(1, world.LayoutVersion, "sample selects legacy camera/layout before tree entry");
            Vector2[] offsets = [new(0,0), new(1280,0), new(1280,1440), new(2880,720), new(4160,720)];
            Vector2[] sizes = [new(1280,720), new(1280,1440), new(1600,720), new(1280,1440), new(1920,720)];
            var main = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            try
            {
                var mainDefinitions = main.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions;
                foreach (PrologueSectorDefinition definition in world.Definitions)
                {
                    int id = (int)definition.Id;
                    string expectedPath = id == 2 ? BasinQualitySampleEntry.SectorScenePath :
                        $"res://scenes/prologue/sample/legacy/{definition.Id}.tscn";
                    TestAssert.Equal(expectedPath, definition.Scene.ResourcePath,
                        "FrozenSampleSceneIndependence: every sample sector is independent of mutable main geometry");
                    TestAssert.True(!ReferenceEquals(definition, mainDefinitions.Single(d => d.Id == definition.Id)),
                        "all five sample definition resources are owned, not aliases of main definitions");
                    TestAssert.Equal(offsets[id], definition.WorldOffset, "legacy sample offset survives main rebuild");
                    TestAssert.Equal(new Rect2(Vector2.Zero, sizes[id]), definition.Bounds, "legacy sample bounds survive main rebuild");
                    int[][] neighbors = [[1], [0,2], [1,3], [2,4], [3]];
                    TestAssert.True(definition.NeighborValues.SequenceEqual(neighbors[id]),
                        "sample preserves explicit pre-v2 neighbors independently of main");
                    var other = BasinSampleLegacyLayout.CreateDefinitions()[id];
                    TestAssert.True(!ReferenceEquals(other, definition) &&
                        !ReferenceEquals(other.KeyInteractionBounds, definition.KeyInteractionBounds) &&
                        !ReferenceEquals(other.NeighborValues, definition.NeighborValues),
                        "sample factory returns independent definition and collection resources each time");
                    Node frozen = GD.Load<PackedScene>($"res://scenes/prologue/sample/legacy/{definition.Id}.tscn").Instantiate();
                    frozen.Free();
                }
            }
            finally { main.Free(); }
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.StartNew();
            await Frames(tree, 4);
            AssertSupported(level, "Start");
            var health = level.Player.GetNode<HealthComponent>("Health");
            health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Down, 0, 984001UL));
            await Frames(tree, 8);
            AssertSupported(level, "death");
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "sample death recovers to Playing");
            flow.ReturnToTitle();
            await Dispose(tree, entry);
            entry = null;
            entry = await Create(tree, directory);
            level = entry.Level;
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave, "fresh sample instance offers valid legacy Continue");
            flow.Continue();
            await Frames(tree, 4);
            AssertSupported(level, "Continue");
            GD.Print("FROZEN_SAMPLE_LIFECYCLE_OK start=(2380,2014) capsule_floor=True death=True reload_continue=True layout=1");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(entry)) await Dispose(tree, entry);
            Directory.Delete(directory, true);
        }
    }

    private static void AssertFrozenHashes()
    {
        string[] expected =
        [
            "BA7049D79791F39E6724718CF135279BE22B53C3B5AB1A072E0EE0FCC91644E0",
            "0E500C5D8D9E7E6F4780A2E1F98BA8A915CE988999E3FA03F4346D3F8557A8B7",
            "D8A4D81226C98C2977CA16F34A82CE0A68E5A5326A75431B7DCB6E87546977C3",
            "E14E5B64A434F7B18D31E2821C25EA54302EB304D0388A8012BBF0AA4F37865B",
            "741F78674DD57B1BD1E2001777FE70192D94AC09AD8AD6CD0481163F279CB370"
        ];
        foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
        {
            string path = ProjectSettings.GlobalizePath($"res://scenes/prologue/sample/legacy/{id}.tscn");
            TestAssert.Equal(expected[(int)id], Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
                "frozen geometry bytes remain identical after main/sample builders: " + id);
        }
        GD.Print("FROZEN_FIVE_HASHES_UNCHANGED");
    }

    private static void AssertSupported(PrologueLevelController level, string stage)
    {
        TestAssert.True(level.Player.GlobalPosition.DistanceTo(new Vector2(2380, 2014)) < 1.1f &&
            level.Player.IsOnFloor(), stage + " real capsule rests at old AuthoredStart on real support");
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        TestAssert.True(world.LayoutVersion == 1 && world.CurrentSector == PrologueSectorId.CanyonBasin,
            stage + " remains on the old camera/layout branch");
        TestAssert.Equal(1, level.CurrentSectorRoot.GetNode("Gameplay/Actors").GetChildCount(),
            stage + " preserves sample-specific single-enemy modifications");
    }
    private static async Task<BasinQualitySampleEntry> Create(SceneTree tree, string directory)
    {
        await Frames(tree, 1);
        var entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn").Instantiate<BasinQualitySampleEntry>();
        entry.StorageDirectory = directory;
        tree.Root.AddChild(entry);
        entry.Level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        await Frames(tree, 3);
        return entry;
    }
    private static async Task Dispose(SceneTree tree, BasinQualitySampleEntry entry)
    {
        await TestSceneTeardown.DrainAudioOwner(tree, entry.Level);
        entry.QueueFree();
        await Frames(tree, 3);
    }
    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
