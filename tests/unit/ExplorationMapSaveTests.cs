using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Presentation.Minimap;
using Knight.Save;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace Knight.Tests.Unit;

internal static class ExplorationMapSaveTests
{
    // Ten cells => two bytes, three boundary segments => one byte. Hand bits:
    // cells 0,2,9 = 05 02; boundaries 0,2 = 05. No writer-derived expectations.
    internal static MinimapTerrainCatalog Catalog() => new(new Rect2(0, 0, 40, 16), "save-small-v1",
        new[] { new MinimapSolid(Transform2D.Identity, new Rect2(24, 0, 8, 16)) },
        new[] { new MinimapBoundary(0, new(24, 0), new(24, 8), Vector2.Left),
            new MinimapBoundary(0, new(24, 8), new(24, 16), Vector2.Left),
            new MinimapBoundary(0, new(24, 0), new(32, 0), Vector2.Up) });
    internal static ExplorationMapSaveData Bits() => new() { TerrainRevision = "save-small-v1",
        FreeCellsBase64 = "BQI=", BoundarySegmentsBase64 = "BQ==" };
    internal const string CoreJson = """
        {"version":4,"layoutVersion":2,"checkpointId":"canyon.v2.temple-forecourt",
        "openShortcuts":["canyon.v2.temple-bell-gate"],"activatedMechanisms":["canyon.v2.courtyard-crank"],
        "collectionRuleVersion":1,"legacyCollectionQuotaExempt":false,
        "collectedFragmentIds":["canyon.v2.fragment.entrance","canyon.v2.fragment.rift"],
        "hasActivatedCheckpoint":true,"checkpointSector":"LostTemple","checkpointWorldX":5920,"checkpointWorldY":1080,
        "shortcutOpen":true,"discoveries":["hidden_alcove"],"bossDefeated":true,"completed":true,
        "objective":"Complete","completedTutorials":["jump","move"],"bestHumanCompletionSeconds":123.5,"bestHumanDeaths":2}
        """;
    internal const string MapJson = """
        {"formatVersion":1,"terrainRevision":"save-small-v1","freeCellsBase64":"BQI=","boundarySegmentsBase64":"BQ=="}
        """;
    internal static readonly JsonSerializerOptions OldCoreOptions = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    internal static void AssertCore(PrologueSaveData data, bool exempt = false, bool legacy = false)
    {
        TestAssert.True(!data.WasRecovered && data.Version == 4 && data.LayoutVersion == 2 &&
            data.CheckpointId == "canyon.v2.temple-forecourt" && data.HasActivatedCheckpoint &&
            data.CheckpointSector == "LostTemple" && data.CheckpointWorldX == 5920 && data.CheckpointWorldY == 1080 &&
            data.OpenShortcuts.SequenceEqual(new[] { "canyon.v2.temple-bell-gate" }) &&
            data.ActivatedMechanisms.SequenceEqual(new[] { "canyon.v2.courtyard-crank" }) && data.ShortcutOpen &&
            data.Discoveries.SequenceEqual(new[] { "hidden_alcove" }) && data.BossDefeated && data.Completed &&
            data.Objective == "Complete" && data.CompletedTutorials.SequenceEqual(new[] { "jump", "move" }) &&
            data.BestHumanCompletionSeconds == 123.5 && data.BestHumanDeaths == 2 && data.CollectionRuleVersion == 1 &&
            data.LegacyCollectionQuotaExempt == exempt && data.CollectedFragmentIds.SequenceEqual(legacy ?
                Array.Empty<string>() : new[] { "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift" }),
            "explicit complete core fields survive without recovery or manufactured progress");
        TestAssert.Equal(exempt, QuestFragmentProgressModel.HasQuota(data.CollectedFragmentIds, data.LegacyCollectionQuotaExempt),
            "bad optional map cannot change actual fragment quota admission");
    }
    internal static void AssertBits(ExplorationMapSaveData data)
    {
        TestAssert.True(data != null && data.FormatVersion == 1 && data.TerrainRevision == "save-small-v1" &&
            data.FreeCellsBase64 == "BQI=" && data.BoundarySegmentsBase64 == "BQ==", "literal map wire fields retained");
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog); model.Restore(data);
        for (int i = 0; i < 10; i++) TestAssert.Equal(i is 0 or 2 or 9, model.IsCellExplored(i), "literal cell bit " + i);
        for (int i = 0; i < 3; i++) TestAssert.Equal(i is 0 or 2, model.IsBoundaryExplored(i), "literal boundary bit " + i);
    }
    internal static void Run()
    {
        string root = IsolatedPrologueFixture.ValidateStorageRoot();
        using var catalog = Catalog();
        var model = new ExplorationMapModel(catalog); model.Restore(Bits());
        var save = JsonSerializer.Deserialize<PrologueSaveData>(CoreJson, OldCoreOptions); save.ExplorationMap = model.CaptureSave();
        string path = Path.Combine(root, "map-roundtrip.json");
        PrologueSaveStore.SaveAtomic(path, save);
        var read = PrologueSaveStore.LoadOrNew(path, 2, catalog);
        TestAssert.True(read.ExplorationMap != null && read.ExplorationMap.FreeCellsBase64 == "BQI=" &&
            read.ExplorationMap.BoundarySegmentsBase64 == "BQ==",
            "ValidMapRoundTrips: legal core and model DTO retain literal cell and boundary bits through real store");
        AssertCore(read); AssertBits(read.ExplorationMap);
        File.WriteAllText(path + ".sha256", Hash(path));
        File.WriteAllText(Path.Combine(root, "writer-pid.txt"), Environment.ProcessId.ToString());
        // A genuinely old core-only reader/writer intentionally drops only JsonIgnore map.
        var oldCore = JsonSerializer.Deserialize<PrologueSaveData>(File.ReadAllText(path), OldCoreOptions);
        AssertCore(oldCore); TestAssert.True(oldCore.ExplorationMap == null, "old strong deserialize ignores extension");
        string oldPath = Path.Combine(root, "old-core-only.json");
        File.WriteAllText(oldPath, JsonSerializer.Serialize(oldCore, OldCoreOptions));
        TestAssert.True(!File.ReadAllText(oldPath).Contains("explorationMap"), "old writer omits optional map");
        File.WriteAllText(oldPath + ".sha256", Hash(oldPath));
        OptionalMatrix(root, catalog); LegacyMatrix(root, catalog); InvalidCoreMatrix(root, catalog);
        CopyAndNoCatalog(root, catalog, save); FailureMatrix(root, catalog, model);
        InvalidDtoWrite(root, catalog); CapacityCodec(root);
        GD.Print($"MINIMAP_SAVE_OK writer_pid={Environment.ProcessId} witnesses=2");
    }
    private static JsonObject Core(bool exempt = false)
    { var core = JsonNode.Parse(CoreJson).AsObject(); core["legacyCollectionQuotaExempt"] = exempt; return core; }
    private static void OptionalMatrix(string root, MinimapTerrainCatalog catalog)
    {
        var variants = new (string Name, string Json)[] {
            ("absent", null), ("null", "null"), ("string", "\"oops\""), ("array", "[]"),
            ("boolean", "true"), ("number", "12"), ("empty-object", "{}"),
            ("version", MapJson.Replace("\"formatVersion\":1", "\"formatVersion\":2")),
            ("version-type", MapJson.Replace("\"formatVersion\":1", "\"formatVersion\":\"1\"")),
            ("revision", MapJson.Replace("save-small-v1", "foreign-revision")),
            ("revision-type", MapJson.Replace("\"save-small-v1\"", "42")),
            ("cell-type", MapJson.Replace("\"BQI=\"", "[]")),
            ("boundary-type", MapJson.Replace("\"BQ==\"", "false")),
            ("cell-null", MapJson.Replace("\"BQI=\"", "null")),
            ("boundary-null", MapJson.Replace("\"BQ==\"", "null")),
            ("cell-base64", MapJson.Replace("BQI=", "!!!!")),
            ("boundary-base64", MapJson.Replace("BQ==", "!!!!")),
            ("cell-short", MapJson.Replace("BQI=", "BQ==")),
            ("cell-long", MapJson.Replace("BQI=", "BQIABA==")),
            ("boundary-short", MapJson.Replace("BQ==", "")),
            ("boundary-long", MapJson.Replace("BQ==", "BQI=")),
            ("cell-padding", MapJson.Replace("BQI=", "BYY=")),
            ("boundary-padding", MapJson.Replace("BQ==", "hQ==")),
            ("missing-cell", "{\"formatVersion\":1,\"terrainRevision\":\"save-small-v1\",\"boundarySegmentsBase64\":\"BQ==\"}"),
            ("oversize-cell", MapJson.Replace("BQI=", new string('A', 174768))),
            ("oversize-boundary", MapJson.Replace("BQ==", new string('A', 174768))) };
        foreach (bool exempt in new[] { false, true }) foreach (var row in variants)
        {
            var node = Core(exempt); if (row.Json != null) node["explorationMap"] = JsonNode.Parse(row.Json);
            string path = Path.Combine(root, $"optional-{row.Name}-{exempt}.json"); File.WriteAllText(path, node.ToJsonString());
            string before = Hash(path); var loaded = PrologueSaveStore.LoadOrNew(path, 2, catalog);
            AssertCore(loaded, exempt); TestAssert.True(loaded.ExplorationMap == null, "bad/absent optional map stays empty: " + row.Name);
            TestAssert.Equal(before, Hash(path), "optional row source hash unchanged");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            bool valid = ExplorationMapSaveCodec.TryRead(document.RootElement, catalog, out var map, out string diagnostic);
            TestAssert.True(!valid && map == null && diagnostic.Length <= 160, "independent bounded optional diagnostic");
            GD.Print($"MINIMAP_OPTIONAL_ROW name={row.Name} exempt={exempt} source_sha256={before} diagnostic={diagnostic}");
        }
    }
    private static void LegacyMatrix(string root, MinimapTerrainCatalog catalog)
    {
        // Schema1 never inherits checkpoint or quota; schema2/3 only legal old entitlement.
        for (int version = 1; version <= 3; version++) foreach (bool entitled in new[] { false, true })
        {
            var node = Core(); node["version"] = version; node["explorationMap"] = JsonNode.Parse(MapJson);
            if (!entitled) { node["checkpointId"] = "canyon.v2.start"; node["checkpointSector"] = "CanyonEntrance";
                node["hasActivatedCheckpoint"] = false; node["bossDefeated"] = false; node["completed"] = false; node["objective"] = "Wake"; }
            string path = Path.Combine(root, $"legacy-{version}-{entitled}.json"); File.WriteAllText(path, node.ToJsonString());
            string before = Hash(path); var loaded = PrologueSaveStore.LoadOrNew(path, mapCatalog: catalog);
            TestAssert.True(!loaded.WasRecovered && loaded.Version == 4 && loaded.ExplorationMap == null &&
                loaded.CollectedFragmentIds.Count == 0 && loaded.CollectionRuleVersion == 1 &&
                loaded.LegacyCollectionQuotaExempt == (version != 1 && entitled), "legacy injected map grants neither map nor fragment authority");
            if (version == 1)
                TestAssert.True(loaded.LayoutVersion == 0 && loaded.CheckpointId == "canyon.v2.start" &&
                    !loaded.HasActivatedCheckpoint && loaded.CheckpointSector == "CanyonEntrance" &&
                    loaded.CheckpointWorldX == 0 && loaded.CheckpointWorldY == 0 && !loaded.ShortcutOpen &&
                    loaded.OpenShortcuts.Count == 0 && loaded.ActivatedMechanisms.Count == 0 && loaded.Discoveries.Count == 0 &&
                    loaded.CompletedTutorials.Count == 0 && !loaded.BossDefeated && loaded.Completed == entitled &&
                    loaded.Objective == (entitled ? "Complete" : "Wake") && loaded.BestHumanCompletionSeconds == 123.5 &&
                    loaded.BestHumanDeaths == 2, "schema1 explicit supported migration fields only");
            else if (entitled) AssertCore(loaded, exempt: true, legacy: true);
            else TestAssert.True(loaded.LayoutVersion == 2 && loaded.CheckpointId == "canyon.v2.start" &&
                !loaded.HasActivatedCheckpoint && loaded.CheckpointSector == "CanyonEntrance" && loaded.CheckpointWorldX == 160 &&
                loaded.CheckpointWorldY == 600 && !loaded.BossDefeated && !loaded.Completed && loaded.Objective == "Wake" &&
                loaded.ShortcutOpen && loaded.OpenShortcuts.SequenceEqual(new[] { "canyon.v2.temple-bell-gate" }) &&
                loaded.ActivatedMechanisms.SequenceEqual(new[] { "canyon.v2.courtyard-crank" }) &&
                loaded.Discoveries.SequenceEqual(new[] { "hidden_alcove" }) &&
                loaded.CompletedTutorials.SequenceEqual(new[] { "jump", "move" }) && loaded.BestHumanCompletionSeconds == 123.5 &&
                loaded.BestHumanDeaths == 2, "schema2/3 nonentitled explicit original core");
            TestAssert.Equal(before, Hash(path), "legacy source unchanged");
            if (version == 2) {
                var full = PrologueSaveStore.LoadFullWorld(Path.Combine(root, $"absent-{entitled}.json"), path, catalog);
                TestAssert.True(full.Status == PrologueSaveLoadStatus.MigratableLegacy && full.Data.ExplorationMap == null &&
                    full.Data.LegacyCollectionQuotaExempt == entitled && full.Data.CollectedFragmentIds.Count == 0,
                    "full-world legal schema2 migration ignores injected map");
                TestAssert.Equal(before, Hash(path), "full-world legacy source unchanged");
            }
            GD.Print($"MINIMAP_LEGACY_ROW version={version} entitled={entitled} source_sha256={before}");
        }
        string oldNoMap = Path.Combine(root, "legacy-no-map.json"); File.WriteAllText(oldNoMap, CoreJson.Replace("\"version\":4", "\"version\":3"));
        string oldNoMapHash = Hash(oldNoMap);
        var oldLoaded = PrologueSaveStore.LoadOrNew(oldNoMap, 2, catalog);
        TestAssert.True(oldLoaded.ExplorationMap == null, "LegacyNoMapRemainsEmpty"); AssertCore(oldLoaded, true, true);
        TestAssert.Equal(oldNoMapHash, Hash(oldNoMap), "legacy no-map source unchanged");
    }
    private static void InvalidCoreMatrix(string root, MinimapTerrainCatalog catalog)
    {
        foreach (string name in new[] { "missing-fragments", "unknown-fragment", "syntax" })
        {
            var node = Core(); node["explorationMap"] = JsonNode.Parse(MapJson);
            if (name == "missing-fragments") node.Remove("collectedFragmentIds");
            if (name == "unknown-fragment") node["collectedFragmentIds"] = JsonNode.Parse("[\"foreign-fragment\"]");
            string path = Path.Combine(root, name + ".json"); File.WriteAllText(path, node.ToJsonString() + (name == "syntax" ? "{" : ""));
            string before = Hash(path); var loaded = PrologueSaveStore.LoadOrNew(path, 2, catalog);
            TestAssert.True(loaded.WasRecovered && loaded.ExplorationMap == null && !loaded.BossDefeated && !loaded.Completed &&
                !loaded.LegacyCollectionQuotaExempt && loaded.CollectedFragmentIds.Count == 0, "map never rescues invalid core or syntax");
            TestAssert.Equal(before, Hash(path), "invalid core/syntax source unchanged");
            GD.Print($"MINIMAP_INVALID_CORE_ROW name={name} source_sha256={before}");
        }
    }
    private static void CopyAndNoCatalog(string root, MinimapTerrainCatalog catalog, PrologueSaveData basis)
    {
        var copy = basis.Normalize(); AssertBits(copy.ExplorationMap);
        TestAssert.True(!ReferenceEquals(copy.ExplorationMap, basis.ExplorationMap), "Normalize independent optional DTO");
        copy.ExplorationMap.FreeCellsBase64 = "AAA="; AssertBits(basis.ExplorationMap);
        var progress = new PrologueProgress(); progress.Restore(basis); var candidate = progress.ToSave(basis);
        AssertCore(candidate); AssertBits(candidate.ExplorationMap);
        candidate.ExplorationMap.BoundarySegmentsBase64 = "AA=="; AssertBits(basis.ExplorationMap);
        string path = Path.Combine(root, "map-roundtrip.json"), before = Hash(path);
        var noCatalog = PrologueSaveStore.LoadOrNew(path, 2); AssertCore(noCatalog);
        TestAssert.True(noCatalog.ExplorationMap == null, "missing catalog ignores map without recovery");
        var full = PrologueSaveStore.LoadFullWorld(path, Path.Combine(root, "unused-legacy.json"), catalog);
        TestAssert.True(full.Status == PrologueSaveLoadStatus.Current, "full-world catalog forwarded"); AssertBits(full.Data.ExplorationMap); AssertCore(full.Data);
        var fullNoCatalog = PrologueSaveStore.LoadFullWorld(path, Path.Combine(root, "unused-legacy.json"));
        AssertCore(fullNoCatalog.Data); TestAssert.True(fullNoCatalog.Data.ExplorationMap == null, "full-world no catalog preserves core");
        TestAssert.Equal(before, Hash(path), "all missing-catalog/full-world read hashes unchanged");
    }
    private static void FailureMatrix(string root, MinimapTerrainCatalog catalog, ExplorationMapModel model)
    {
        string blocked = Path.Combine(root, "directory-blocker"); File.WriteAllText(blocked, "owned blocker");
        string before = JsonSerializer.Serialize(model.CaptureSave()); bool directoryFailed = false;
        var save = JsonSerializer.Deserialize<PrologueSaveData>(CoreJson, OldCoreOptions); save.ExplorationMap = model.CaptureSave();
        try { PrologueSaveStore.SaveAtomic(Path.Combine(blocked, "slot.json"), save); }
        catch (IOException exception) { directoryFailed = true; GD.Print("MINIMAP_IO_DIRECTORY_REFUSAL " + exception.GetType().Name); }
        TestAssert.True(directoryFailed && !Directory.EnumerateFiles(root, ".*.tmp", SearchOption.AllDirectories).Any(), "actual directory IO failure no temp leak");
        TestAssert.Equal(before, JsonSerializer.Serialize(model.CaptureSave()), "directory failure leaves model snapshot untouched");
        string slot = Path.Combine(root, "replacement-slot.json"); PrologueSaveStore.SaveAtomic(slot, save); string hash = Hash(slot);
        bool refused = false;
        using (var ownedReadHandle = new FileStream(slot, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try { var replacement = save.Normalize(); replacement.BestHumanDeaths = 9; PrologueSaveStore.SaveAtomic(slot, replacement); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
                // On Windows this stack identifies actual post-temp File.Move replacement failure, not Normalize/preflight.
                TestAssert.True(exception.StackTrace?.Contains("System.IO.File.Move") == true ||
                    exception.StackTrace?.Contains("System.IO.FileSystem.MoveFile") == true,
                    "replacement denial must occur in actual File.Move/FileSystem.MoveFile");
                refused = true; GD.Print("MINIMAP_IO_REPLACEMENT_REFUSAL stage=File.Move exception=" + exception.GetType().Name);
            }
            TestAssert.True(refused, "Windows no-Delete-share handle must actually deny existing regular-slot replacement; otherwise escalate");
            TestAssert.Equal(hash, Hash(slot), "refused replacement preserves exact old regular-slot bytes");
        }
        TestAssert.Equal(before, JsonSerializer.Serialize(model.CaptureSave()), "refused replacement leaves model snapshot untouched");
        TestAssert.True(!Directory.EnumerateFiles(root, ".*.tmp", SearchOption.AllDirectories).Any(), "actual post-write replacement refusal cleans temp");
        var invalid = save.Normalize(); invalid.CollectedFragmentIds.Add("foreign-fragment"); bool preflight = false;
        try { PrologueSaveStore.SaveAtomic(slot, invalid); } catch (InvalidDataException) { preflight = true; }
        TestAssert.True(preflight && Hash(slot) == hash, "separate invalid-core preflight control preserves slot");
        string backupSlot = Path.Combine(root, "backup-slot.json"); PrologueSaveStore.SaveAtomic(backupSlot, save); string old = Hash(backupSlot);
        var next = save.Normalize(); next.BestHumanDeaths = 7; PrologueSaveStore.SaveAtomic(backupSlot, next, preserveExisting: true);
        string backup = Directory.GetFiles(root, "backup-slot.json.preserved-*.json").Single();
        TestAssert.Equal(old, Hash(backup), "preserveExisting keeps exact prior bytes");
        TestAssert.Equal(7, PrologueSaveStore.LoadOrNew(backupSlot, 2, catalog).BestHumanDeaths, "replacement commits new core");
        GD.Print($"MINIMAP_FAILURE_MATRIX directory_io=1 replacement_move=1 preflight=1 temp_leaks=0 old_slot_sha256={hash}");
    }
    private static void InvalidDtoWrite(string root, MinimapTerrainCatalog catalog)
    {
        foreach (string name in new[] { "format", "null-text", "bad-base64", "oversize", "oversize-same-encoded-length" }) {
            var core = JsonSerializer.Deserialize<PrologueSaveData>(CoreJson, OldCoreOptions); var bits = Bits();
            if (name == "format") bits.FormatVersion = 9;
            if (name == "null-text") bits.FreeCellsBase64 = null;
            if (name == "bad-base64") bits.BoundarySegmentsBase64 = "!!!!";
            if (name == "oversize") bits.FreeCellsBase64 = new string('A', 174768);
            if (name == "oversize-same-encoded-length") bits.FreeCellsBase64 = new string('A', 174764); // 131073 decoded bytes.
            core.ExplorationMap = bits; string path = Path.Combine(root, "invalid-dto-" + name + ".json"); PrologueSaveStore.SaveAtomic(path, core);
            AssertCore(PrologueSaveStore.LoadOrNew(path, 2, catalog));
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            TestAssert.True(!doc.RootElement.TryGetProperty("explorationMap", out _), "malformed optional DTO omitted, core not manufactured");
        }
        // Catalog-dependent count/padding validation belongs to read, not writer without a catalog.
        var wrongCount = JsonSerializer.Deserialize<PrologueSaveData>(CoreJson, OldCoreOptions); wrongCount.ExplorationMap = Bits();
        wrongCount.ExplorationMap.FreeCellsBase64 = "BQ=="; string countPath = Path.Combine(root, "write-wrong-count.json");
        PrologueSaveStore.SaveAtomic(countPath, wrongCount); var loaded = PrologueSaveStore.LoadOrNew(countPath, 2, catalog);
        AssertCore(loaded); TestAssert.True(loaded.ExplorationMap == null, "catalog rejects intrinsically valid but count-wrong optional DTO only");
    }
    private static void CapacityCodec(string root)
    {
        var boundaries = new MinimapBoundary[1048576];
        Array.Fill(boundaries, new MinimapBoundary(0, new(0, 0), new(8, 0), Vector2.Up));
        using var catalog = new MinimapTerrainCatalog(new Rect2(0, 0, 8192, 8192), "codec-max",
            new[] { new MinimapSolid(Transform2D.Identity, new Rect2(0, 0, 8, 8)) }, boundaries);
        // 131070 zero bytes plus literal final bytes 00 80 => AIA=;
        // last of the 1048576 bits set independently in each exact-limit bitset.
        string maximum = new string('A', 174760) + "AIA=";
        var save = JsonSerializer.Deserialize<PrologueSaveData>(CoreJson, OldCoreOptions);
        save.ExplorationMap = new() { TerrainRevision = "codec-max", FreeCellsBase64 = maximum, BoundarySegmentsBase64 = maximum };
        string path = Path.Combine(root, "maximum-bitsets.json"); PrologueSaveStore.SaveAtomic(path, save); string before = Hash(path);
        var read = PrologueSaveStore.LoadOrNew(path, 2, catalog); AssertCore(read);
        TestAssert.True(read.ExplorationMap?.FreeCellsBase64 == maximum && read.ExplorationMap.BoundarySegmentsBase64 == maximum,
            "both 1048576-bit / 131072-byte bitsets are accepted, not truncated");
        byte[] decoded = Convert.FromBase64String(read.ExplorationMap.FreeCellsBase64);
        TestAssert.True(decoded.Length == 131072 && decoded[0] == 0 && decoded[^1] == 128, "literal maximum final bit preserved");
        TestAssert.Equal(before, Hash(path), "maximum optional map read hash unchanged");
        using var zeroBoundary = new MinimapTerrainCatalog(new Rect2(0, 0, 8, 8), "codec-zero-boundary");
        save.ExplorationMap = new() { TerrainRevision = "codec-zero-boundary", FreeCellsBase64 = "AQ==", BoundarySegmentsBase64 = "" };
        string zeroPath = Path.Combine(root, "zero-boundaries.json"); PrologueSaveStore.SaveAtomic(zeroPath, save); string zeroHash = Hash(zeroPath);
        var zeroRead = PrologueSaveStore.LoadOrNew(zeroPath, 2, zeroBoundary); AssertCore(zeroRead);
        TestAssert.True(zeroRead.ExplorationMap?.FreeCellsBase64 == "AQ==" && zeroRead.ExplorationMap.BoundarySegmentsBase64 == "",
            "zero-boundary catalog accepts exact empty boundary encoding");
        TestAssert.Equal(zeroHash, Hash(zeroPath), "zero-boundary source unchanged");
        GD.Print("MINIMAP_CODEC_CAPACITY_OK cell_bits=1048576 boundary_bits=1048576 bytes_each=131072 zero_boundary=True");
    }
}
