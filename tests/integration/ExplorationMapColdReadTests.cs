using System;
using System.IO;
using System.Text.Json;
using Godot;
using Knight.Save;
using Knight.Tests.Unit;
using Environment = System.Environment;

namespace Knight.Tests.Integration;

internal static class ExplorationMapColdReadTests
{
    internal static void Run()
    {
        string root = IsolatedPrologueFixture.ValidateStorageRoot(); // Before first read, no test writes.
        int writerPid = int.Parse(File.ReadAllText(Path.Combine(root, "writer-pid.txt")));
        TestAssert.True(writerPid != Environment.ProcessId, "cold read must be a truly different process");
        using var catalog = ExplorationMapSaveTests.Catalog();
        foreach (string name in new[] { "map-roundtrip.json", "old-core-only.json" })
        {
            string path = Path.Combine(root, name), before = ExplorationMapSaveTests.Hash(path);
            TestAssert.Equal(File.ReadAllText(path + ".sha256"), before, "retained exact-byte witness SHA");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            ExplorationMapSaveTests.AssertCore(JsonSerializer.Deserialize<PrologueSaveData>(
                document.RootElement.GetRawText(), ExplorationMapSaveTests.OldCoreOptions));
            bool hasMap = document.RootElement.TryGetProperty("explorationMap", out var wire);
            TestAssert.Equal(name == "map-roundtrip.json", hasMap, "cold raw optional property presence");
            if (hasMap) TestAssert.True(wire.GetProperty("formatVersion").GetInt32() == 1 &&
                wire.GetProperty("terrainRevision").GetString() == "save-small-v1" &&
                wire.GetProperty("freeCellsBase64").GetString() == "BQI=" &&
                wire.GetProperty("boundarySegmentsBase64").GetString() == "BQ==", "cold literal raw wire");
            var loaded = PrologueSaveStore.LoadOrNew(path, 2, catalog); ExplorationMapSaveTests.AssertCore(loaded);
            if (hasMap) ExplorationMapSaveTests.AssertBits(loaded.ExplorationMap);
            else TestAssert.True(loaded.ExplorationMap == null, "old core-only rewrite loses only optional map");
            TestAssert.Equal(before, ExplorationMapSaveTests.Hash(path), "cold reader never rewrites source");
            GD.Print($"MINIMAP_COLD_WITNESS name={name} source_sha256={before} map={hasMap}");
        }
        GD.Print($"MINIMAP_COLD_READ_OK writer_pid={writerPid} reader_pid={Environment.ProcessId} witnesses=2 read_only=True");
    }
}
