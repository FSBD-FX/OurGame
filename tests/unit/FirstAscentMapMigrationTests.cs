using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Knight.Build.Prologue;
using Knight.Presentation;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Tests.Integration;
using Knight.Tools;

namespace Knight.Tests.Unit;

internal static class FirstAscentMapMigrationTests
{
    private const string PostRevision = "BEE5E8069591E12D83F10EB0477FFCA3CFEAC1A314DE6C6C958F7E257271E3C2";
    private const string PostSha = "566F599CC762B2EBC7D6D3BAC94628422C61565B5D6081F77DFD5B2661E1F8B4";
    private const string BaffleRevision = "D8581BF1A4012D09B85953DDF863B607273AE569EBA7B226D6989098DCA09A2D";

    private static byte[] RemoveBaffle(byte[] current)
    {
        const int header = 96, solidSize = 40, edgeSize = 28;
        int baffleSolid = header + 91 * solidSize;
        int newEdges = header + 144 * solidSize;
        byte[] prior = new byte[354696];
        Array.Copy(current, prior, baffleSolid);
        Array.Copy(current, baffleSolid + solidSize, prior, baffleSolid, newEdges - baffleSolid - solidSize);
        for (int id = 0; id < 12460; id++)
        {
            int offset = header + 143 * solidSize + id * edgeSize;
            Array.Copy(current, newEdges + (id < 9358 ? id : id + 8) * edgeSize, prior, offset, edgeSize);
            if (id >= 9358) BitConverter.GetBytes(BitConverter.ToInt32(prior, offset) - 1).CopyTo(prior, offset);
        }
        BitConverter.GetBytes(143).CopyTo(prior, 24);
        BitConverter.GetBytes(12460).CopyTo(prior, 28);
        System.Text.Encoding.ASCII.GetBytes(PostRevision).CopyTo(prior, 32);
        TestAssert.Equal(PostSha, Convert.ToHexString(SHA256.HashData(prior)),
            "removing only the new baffle reproduces the exact historical catalog");
        return prior;
    }

    internal static void Grip8Baffle(string root)
    {
        using var target = HistoricalMinimapCatalogFixture.Baffle();
        TestAssert.True(target.Revision == BaffleRevision && target.SolidCount == 144 && target.BoundaryCount == 12468,
            "Grip8 migration target is the independently frozen historical144 catalog");
        RemoveBaffle(target.SerializedValues);
        byte[] cells = new byte[64800], edges = new byte[1558];
        foreach (int i in new[] { 0, 202 * 960 + 581, 202 * 960 + 582, 205 * 960 + 583, 518399 }) Set(cells, i);
        foreach (int i in new[] { 0, 9284, 9296, 9297, 9357, 9358, 12459 }) Set(edges, i);
        var core = JsonNode.Parse(ExplorationMapSaveTests.CoreJson).AsObject();
        core["explorationMap"] = JsonSerializer.SerializeToNode(new ExplorationMapSaveData
        {
            TerrainRevision = PostRevision, FreeCellsBase64 = Convert.ToBase64String(cells),
            BoundarySegmentsBase64 = Convert.ToBase64String(edges)
        }, ExplorationMapSaveTests.OldCoreOptions);
        string path = Path.Combine(root, "map-grip8-baffle-post-source.json");
        File.WriteAllText(path, core.ToJsonString());
        string hash = ExplorationMapSaveTests.Hash(path);
        var read = PrologueSaveStore.LoadOrNew(path, 2, target);
        ExplorationMapSaveTests.AssertCore(read);
        TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "Grip8 migration does not write old source");
        TestAssert.True(read.ExplorationMap != null, "GRIP8_MAP_MIGRATION_RED: unchanged exploration survives the baffle");
        AssertBaffleBits(read.ExplorationMap, target, chained: false);
        string resaved = Path.Combine(root, "map-grip8-baffle-resaved.json");
        TestAssert.True(!File.Exists(resaved), "Grip8 resave uses a fresh isolated slot");
        PrologueSaveStore.SaveAtomic(resaved, read);
        File.WriteAllText(resaved + ".sha256", ExplorationMapSaveTests.Hash(resaved));
        File.WriteAllText(path + ".sha256", hash);
        TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "resave does not write the migration source");
        GD.Print("GRIP8_MAP_MIGRATION_OK core_preserved=true source_read_only=true no_new_reveal=true");
    }

    internal static void Grip8BaffleColdRead(string root)
    {
        using var target = HistoricalMinimapCatalogFixture.Baffle();
        foreach (var witness in new[] { (Name: "map-grip8-baffle-resaved.json", Revision: BaffleRevision, Chained: false),
            (Name: "map-grip8-baffle-post-source.json", Revision: PostRevision, Chained: false),
            (Name: "map-v133-migration.json", Revision: ExplorationMapSaveTests.V133Revision, Chained: true) })
        {
            string path = Path.Combine(root, witness.Name);
            string hash = ExplorationMapSaveTests.Hash(path);
            TestAssert.Equal(File.ReadAllText(path + ".sha256"), hash, "cold retains exact Grip8 witness bytes");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var raw = document.RootElement.GetProperty("explorationMap");
            TestAssert.Equal(witness.Revision, raw.GetProperty("terrainRevision").GetString(),
                "cold distinguishes a persisted current revision from an old source");
            var read = PrologueSaveStore.LoadOrNew(path, 2, target);
            ExplorationMapSaveTests.AssertCore(read);
            AssertBaffleBits(read.ExplorationMap, target, witness.Chained);
            if (witness.Revision == BaffleRevision)
            {
                TestAssert.Equal(read.ExplorationMap.FreeCellsBase64, raw.GetProperty("freeCellsBase64").GetString(),
                    "resaved free-cell wire matches the independently restored map");
                TestAssert.Equal(read.ExplorationMap.BoundarySegmentsBase64, raw.GetProperty("boundarySegmentsBase64").GetString(),
                    "resaved boundary wire matches the independently restored map");
            }
            TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "cold Grip8 reader never rewrites a slot");
        }
        GD.Print("GRIP8_MAP_COLD_OK resaved_current=true post_source=true v133_chain=true exact_bits=true core_preserved=true read_only=true");
    }

    private static void AssertBaffleBits(ExplorationMapSaveData data, MinimapTerrainCatalog target, bool chained)
    {
        TestAssert.True(data != null && data.TerrainRevision == BaffleRevision, "Grip8 bits use the actual current revision");
        var model = new ExplorationMapModel(target); model.Restore(data);
        for (int i = 0; i < 518400; i++)
            TestAssert.Equal(chained ? i is 0 or 447715 or 447737 or 518399 : i is 0 or (202 * 960 + 581) or 518399,
                model.IsCellExplored(i), "Grip8 cell " + i);
        for (int i = 0; i < 12468; i++)
            TestAssert.Equal(chained ? i is 0 or 6525 or 6570 or 6574 or 12467 : i is 0 or 9297 or 9357 or 9366 or 12467,
                model.IsBoundaryExplored(i), "Grip8 edge " + i);
    }

    internal static MinimapTerrainCatalog HistoricalCatalog()
    {
        using var current = HistoricalMinimapCatalogFixture.Baffle();
        byte[] bytes = current.SerializedValues;
        if (current.Revision == BaffleRevision) bytes = RemoveBaffle(bytes);
        if (current.Revision == PostRevision || current.Revision == BaffleRevision)
        {
            TestAssert.Equal(PostSha, Convert.ToHexString(SHA256.HashData(bytes)), "inverse fixture requires exact new catalog");
            // Literal 26 old owner58 segments extracted before authoring from
            // the v178 frozen physical parent (full old catalog SHA pinned below).
            const string oldPost = "OgAAAAAAkMEAAPDBAACQwQAAtMEAAIC/AAAAADoAAAAAAJDBAAC0wQAAkMEAAHDBAACAvwAAAAA6AAAAAACQwQAAcMEAAJDBAADwwAAAgL8AAAAAOgAAAAAAkMEAAPDAAACQwQAAAAAAAIC/AAAAADoAAAAAAJDBAAAAAAAAkMEAAPBAAACAvwAAAAA6AAAAAACQwQAA8EAAAJDBAABwQQAAgL8AAAAAOgAAAAAAkMEAAHBBAACQwQAAtEEAAIC/AAAAADoAAAAAAJDBAAC0QQAAkMEAAPBBAACAvwAAAAA6AAAAAACQQQAA8MEAAJBBAAC0wQAAgD8AAAAAOgAAAAAAkEEAALTBAACQQQAAcMEAAIA/AAAAADoAAAAAAJBBAABwwQAAkEEAAPDAAACAPwAAAAA6AAAAAACQQQAA8MAAAJBBAAAAAAAAgD8AAAAAOgAAAAAAkEEAAAAAAACQQQAA8EAAAIA/AAAAADoAAAAAAJBBAADwQAAAkEEAAHBBAACAPwAAAAA6AAAAAACQQQAAcEEAAJBBAAC0QQAAgD8AAAAAOgAAAAAAkEEAALRBAACQQQAA8EEAAIA/AAAAADoAAAAAAJDBAADwwczMLMEAAPDBAAAAAAAAgL86AAAAzMwswQAA8MFkZmbAAADwwQAAAAAAAIC/OgAAAGRmZsAAAPDBaGZmQAAA8MEAAAAAAACAvzoAAABoZmZAAADwwc7MLEEAAPDBAAAAAAAAgL86AAAAzswsQQAA8MEAAJBBAADwwQAAAAAAAIC/OgAAAAAAkMEAAPBBzMwswQAA8EEAAAAAAACAPzoAAADMzCzBAADwQWRmZsAAAPBBAAAAAAAAgD86AAAAZGZmwAAA8EFoZmZAAADwQQAAAAAAAIA/OgAAAGhmZkAAAPBBzswsQQAA8EEAAAAAAACAPzoAAADOzCxBAADwQQAAkEEAAPBBAAAAAAAAgD8=";
            int split = 32 + 64 + 143 * 40 + 7348 * 28;
            byte[] old = new byte[354024];
            Array.Copy(bytes, old, split);
            Convert.FromBase64String(oldPost).CopyTo(old, split);
            Array.Copy(bytes, split + 50 * 28, old, split + 26 * 28, bytes.Length - split - 50 * 28);
            BitConverter.GetBytes(12436).CopyTo(old, 28);
            System.Text.Encoding.UTF8.GetBytes(ExplorationMapSaveTests.CurrentRevision).CopyTo(old, 32);
            Buffer.BlockCopy(new float[] { 1, 0, 0, 1, 4232, 3846, -18, -30, 36, 60 }, 0,
                old, 32 + 64 + 58 * 40, 40);
            bytes = old;
        }
        TestAssert.Equal("1D2D41D9191238A87CC679F04F522D09ED99D18C3374C710778364B986E18C4F",
            Convert.ToHexString(SHA256.HashData(bytes)), "historical4802 is exact bytes, not renamed current geometry");
        return new MinimapTerrainCatalog { SerializedValues = bytes };
    }

    internal static MinimapTerrainCatalog CandidateCatalog()
    {
        // The original Post143 target is now historical. Never rebuild it from
        // live158 leaves and then give the resulting geometry an old label.
        using var baffle = HistoricalMinimapCatalogFixture.Baffle();
        return new MinimapTerrainCatalog { SerializedValues = RemoveBaffle(baffle.SerializedValues) };
    }

    internal static void Run(string root)
    {
        using var old = HistoricalCatalog();
        using var target = CandidateCatalog();
        GD.Print($"POST_MAP_CANDIDATE revision={target.Revision} sha256={Convert.ToHexString(SHA256.HashData(target.SerializedValues))} " +
            $"solids={target.SolidCount} boundaries={target.BoundaryCount} bytes={target.SerializedValues.Length}");
        TestAssert.True(old.Revision == ExplorationMapSaveTests.CurrentRevision && old.SolidCount == 143 &&
            old.BoundaryCount == 12436 && target.BoundaryCount == 12460 && target.CellCount == 518400,
            "literal historical and candidate map capacities");
        for (int i = 0; i < 143; i++)
            if (i != 58) TestAssert.Equal(old.Solid(i), target.Solid(i), "unchanged owning solid " + i);
        for (int i = 0; i < 12436; i++)
            if (i is not (>= 7348 and < 7374))
                TestAssert.Equal(old.Boundary(i), target.Boundary(i < 7348 ? i : i + 24), "unchanged exact boundary " + i);
        foreach (string kind in new[] { "sparse", "full", "zero" })
        {
            byte[] cells = new byte[64800], edges = new byte[1555];
            if (kind == "full") { Array.Fill(cells, (byte)255); Array.Fill(edges, (byte)255); edges[^1] = 15; }
            if (kind == "sparse")
            {
                foreach (int i in new[] { 0, 518399, 446925, 446926, 446931, 446932, 445968, 466128 }) Set(cells, i);
                foreach (int i in new[] { 0, 7347, 7348, 7373, 7374, 12435 }) Set(edges, i);
            }
            var dto = new ExplorationMapSaveData { TerrainRevision = old.Revision,
                FreeCellsBase64 = Convert.ToBase64String(cells), BoundarySegmentsBase64 = Convert.ToBase64String(edges) };
            string path = Path.Combine(root, "map-postrise-" + kind + ".json");
            var core = JsonNode.Parse(ExplorationMapSaveTests.CoreJson).AsObject();
            core["explorationMap"] = JsonSerializer.SerializeToNode(dto, ExplorationMapSaveTests.OldCoreOptions);
            File.WriteAllText(path, core.ToJsonString());
            string hash = ExplorationMapSaveTests.Hash(path); File.WriteAllText(path + ".sha256", hash);
            var read = PrologueSaveStore.LoadOrNew(path, 2, target); ExplorationMapSaveTests.AssertCore(read);
            TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "post migration never writes old source");
            TestAssert.True(read.ExplorationMap != null, "POST_MAP_MIGRATION_RED: unaffected explored regions must survive the local post change");
            AssertBits(read.ExplorationMap, target, kind, chained: false);
        }
        foreach (string kind in new[] { "sparse", "full", "zero" })
        {
            string name = kind == "sparse" ? "map-v133-migration.json" : "map-v133-" + (kind == "full") + ".json";
            string path = Path.Combine(root, name), hash = ExplorationMapSaveTests.Hash(path);
            var read = PrologueSaveStore.LoadOrNew(path, 2, target); ExplorationMapSaveTests.AssertCore(read);
            TestAssert.True(read.ExplorationMap != null, "older B264 memory must chain through both attested deltas");
            AssertBits(read.ExplorationMap, target, kind, chained: true);
            TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "chained migration never writes source");
        }
        Controls(root, target);
        StageArtifacts(root);
        GD.Print("POST_MAP_MIGRATION_OK sparse=true full=true zero=true chain=true exact_geometry=true core_preserved=true source_read_only=true");
    }

    internal static void ColdRead(string root)
    {
        using var target = CandidateCatalog();
        foreach (string kind in new[] { "sparse", "full", "zero" })
        {
            string path = Path.Combine(root, "map-postrise-" + kind + ".json"), hash = ExplorationMapSaveTests.Hash(path);
            TestAssert.Equal(File.ReadAllText(path + ".sha256"), hash, "cold exact old4802 source witness");
            var read = PrologueSaveStore.LoadOrNew(path, 2, target); ExplorationMapSaveTests.AssertCore(read);
            TestAssert.True(read.ExplorationMap != null, "cold reader migrates original4802 wire");
            AssertBits(read.ExplorationMap, target, kind, chained: false);
            TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "cold post migration source read-only");
        }
        GD.Print("POST_MAP_COLD_OK independent_reader=true sparse=true full=true zero=true source_read_only=true");
    }

    private static void AssertBits(ExplorationMapSaveData data, MinimapTerrainCatalog target, string kind, bool chained)
    {
        TestAssert.Equal(target.Revision, data.TerrainRevision, "converted bits carry the attested target revision");
        var model = new ExplorationMapModel(target); model.Restore(data);
        int cells = 0, edges = 0;
        for (int i = 0; i < 518400; i++)
        {
            bool forgotten = i / 960 is >= 465 and <= 484 && i % 960 is >= 526 and <= 531 ||
                chained && i / 960 is >= 464 and <= 468 && i % 960 is >= 356 and <= 376;
            bool expected = kind == "full" ? !forgotten : kind == "sparse" && (chained
                ? i is 0 or 518399 or 447715 or 447737
                : i is 0 or 518399 or 446925 or 446932 or 445968 or 466128);
            TestAssert.Equal(expected, model.IsCellExplored(i), "post cell " + i);
            if (expected) cells++;
        }
        for (int i = 0; i < 12460; i++)
        {
            bool forgotten = i is >= 7348 and < 7398 || chained && i is >= 6526 and < 6570;
            bool expected = kind == "full" ? !forgotten : kind == "sparse" && (chained
                ? i is 0 or 6525 or 6570 or 6574 or 12459 : i is 0 or 7347 or 7398 or 12459);
            TestAssert.Equal(expected, model.IsBoundaryExplored(i), "post boundary " + i);
            if (expected) edges++;
        }
        if (kind == "full")
        {
            TestAssert.Equal(chained ? 518175 : 518280, cells, "only exact changed footprints forgotten");
            TestAssert.Equal(chained ? 12366 : 12410, edges, "only changed owners forgotten");
        }
        TestAssert.True((Convert.FromBase64String(data.BoundarySegmentsBase64)[^1] & 0xf0) == 0, "canonical new padding");
    }

    private static void Controls(string root, MinimapTerrainCatalog target)
    {
        string wire = File.ReadAllText(Path.Combine(root, "map-postrise-sparse.json"));
        var solids = Enumerable.Range(0, target.SolidCount).Select(target.Solid).ToArray();
        var shifted = solids[58].LocalToWorld; shifted.Origin += new Vector2(.25f, 0);
        solids[58] = solids[58] with { LocalToWorld = shifted };
        using var forged = new MinimapTerrainCatalog(target.WorldBounds, target.Revision, solids,
            Enumerable.Range(0, target.BoundaryCount).Select(target.Boundary).ToArray());
        foreach (string kind in new[] { "unknown-source", "missing-edge", "bad-padding", "forged-target" })
        {
            var node = JsonNode.Parse(wire).AsObject(); var map = node["explorationMap"].AsObject();
            if (kind == "unknown-source") map["terrainRevision"] = "unattested-source";
            if (kind == "missing-edge") map.Remove("boundarySegmentsBase64");
            if (kind == "bad-padding")
            {
                byte[] edges = Convert.FromBase64String(map["boundarySegmentsBase64"].GetValue<string>());
                edges[^1] |= 0x80; map["boundarySegmentsBase64"] = Convert.ToBase64String(edges);
            }
            string path = Path.Combine(root, "map-postrise-negative-" + kind + ".json");
            File.WriteAllText(path, node.ToJsonString()); string hash = ExplorationMapSaveTests.Hash(path);
            var read = PrologueSaveStore.LoadOrNew(path, 2, kind == "forged-target" ? forged : target);
            ExplorationMapSaveTests.AssertCore(read); TestAssert.True(read.ExplorationMap == null, "post migration rejects " + kind);
            TestAssert.Equal(hash, ExplorationMapSaveTests.Hash(path), "negative remains read-only " + kind);
        }
        GD.Print("POST_MAP_CONTROLS_OK unknown_source=true malformed=true forged_target=true core_preserved=true");
    }

    private static void StageArtifacts(string storage)
    {
        const string bodyPath = "Gameplay/Collision/ShortcutStep3956Grip";
        const string skinPath = "VisualRoot/GameplayReadable/ShortcutStep3956Grip";
        var source = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn");
        var original = source.Instantiate<PrologueSector>();
        var target = source.Instantiate<PrologueSector>();
        var donor = PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonBasin);
        Shape2D[] donorOwned = Array.Empty<Shape2D>();
        RectangleShape2D owned = null;
        try
        {
            SteamMetadataSynchronizationTests.OwnDonorShapes(donor, out donorOwned, original, target);
            var before = SteamMetadataSynchronizationTests.Snapshot(original);
            var body = target.GetNode<StaticBody2D>(bodyPath);
            var skin = target.GetNode<CanyonPlatformVisual>(skinPath);
            var donorBody = donor.GetNode<StaticBody2D>(bodyPath);
            var donorSkin = donor.GetNode<CanyonPlatformVisual>(skinPath);
            TestAssert.True(donorBody.Position == new Vector2(2952, 200) && donorSkin.Position == donorBody.Position &&
                donorSkin.SkinSize == new Vector2(36, 152) &&
                ((RectangleShape2D)donorBody.GetNode<CollisionShape2D>("Collision").Shape).Size == donorSkin.SkinSize &&
                donorSkin.GetNode<Polygon2D>("ReadableEdge").Position == new Vector2(0, -73),
                "actual generator must raise only the first post with matched visual/collision edge");
            var allowed = new HashSet<string> { bodyPath + "|position", bodyPath + "/Collision|shape",
                skinPath + "|position", skinPath + "|SkinSize", skinPath + "/ReadableEdge|position" };
            target.SceneFilePath = "";
            owned = (RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape.Duplicate();
            owned.ResourceSceneUniqueId = ""; owned.Size = new(36, 152);
            body.GetNode<CollisionShape2D>("Collision").Shape = owned;
            body.Position = donorBody.Position; skin.Position = donorSkin.Position; skin.SkinSize = donorSkin.SkinSize;
            skin.GetNode<Polygon2D>("ReadableEdge").Position = donorSkin.GetNode<Polygon2D>("ReadableEdge").Position;
            AssertOnly(before, SteamMetadataSynchronizationTests.Snapshot(target), allowed);
            string basinPath = Path.Combine(storage, "PostRise-CanyonBasin.tscn");
            TestAssert.Equal(Error.Ok, PrologueCanyonBuildParts.SaveAndValidate(target, basinPath,
                out int n, out int saved, out string failure), "stage post-only Basin: " + failure);
            TestAssert.Equal(n, saved, "all existing nodes survive stage pack");
            using var packed = ResourceLoader.Load<PackedScene>(basinPath, cacheMode: ResourceLoader.CacheMode.Ignore);
            var reload = packed.Instantiate<PrologueSector>();
            try { AssertOnly(before, SteamMetadataSynchronizationTests.Snapshot(reload), allowed); }
            finally { reload.Free(); }
            TestAssert.Equal(SteamMetadataSynchronizationTests.PersistentLinks(source),
                SteamMetadataSynchronizationTests.PersistentLinks(packed), "Basin persistent/nested links unchanged");
            AssertOnly(before, SteamMetadataSynchronizationTests.Snapshot(original), new HashSet<string>());

            var parentSource = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
            var parent = parentSource.Instantiate<PrologueLevelController>();
            var definitions = new List<PrologueSectorDefinition>();
            MinimapTerrainCatalog catalog = null;
            try
            {
                var parentBefore = SteamMetadataSynchronizationTests.Snapshot(parent);
                parent.SceneFilePath = "";
                // This separate authoring check uses the actual current map;
                // retain its baffle and annex instead of pretending it is143.
                foreach (var prior in parent.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions)
                {
                    var d = new PrologueSectorDefinition(prior.Id, prior.Id == PrologueSectorId.CanyonBasin ? packed : prior.Scene,
                        prior.WorldOffset, prior.Bounds, prior.Previous, prior.Next);
                    d.NeighborValues.Clear(); foreach (int x in prior.NeighborValues) d.NeighborValues.Add(x);
                    foreach (Rect2 r in prior.KeyInteractionBounds) d.KeyInteractionBounds.Add(r);
                    definitions.Add(d);
                }
                // The old resident authoring operation intentionally pins143.
                // Build this current158 fixture without changing that legacy
                // operation, and attest the complete current geometry first.
                catalog = MinimapTerrainCatalogBuilder.Build(definitions);
                TestAssert.Equal("D6EC6A12A6D3CF6BDB0C1A9C981565A6EC43D901BB8175C987DFE0A19ADC61CE",
                    Convert.ToHexString(SHA256.HashData(catalog.SerializedValues)), "staged actual current158 geometry retains annex and baffle");
                parent.GetNode<PrologueMinimapController>("MinimapController").Catalog = catalog;
                AssertOnly(parentBefore, SteamMetadataSynchronizationTests.Snapshot(parent), new HashSet<string> { "MinimapController|Catalog" });
                string parentPath = Path.Combine(storage, "PostRise-PrologueLevel.tscn");
                TestAssert.Equal(Error.Ok, SceneBuildUtil.PackAndSave(parent, parentPath), "stage parent catalog only");
                using var parentPacked = ResourceLoader.Load<PackedScene>(parentPath, cacheMode: ResourceLoader.CacheMode.Ignore);
                var loaded = parentPacked.Instantiate<PrologueLevelController>();
                try
                {
                    AssertOnly(parentBefore, SteamMetadataSynchronizationTests.Snapshot(loaded), new HashSet<string> { "MinimapController|Catalog" });
                    TestAssert.True(loaded.GetNode<PrologueMinimapController>("MinimapController").Catalog.SerializedValues.AsSpan()
                        .SequenceEqual(catalog.SerializedValues), "saved parent holds exact new catalog bytes");
                    TestAssert.True(loaded.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.All(d =>
                        d.Scene.ResourcePath == PrologueCanyonBuildParts.ScenePath(d.Id)), "parent keeps all canonical leaf references");
                }
                finally { loaded.Free(); }
                TestAssert.Equal(SteamMetadataSynchronizationTests.PersistentLinks(parentSource),
                    SteamMetadataSynchronizationTests.PersistentLinks(parentPacked), "parent persistent/nested links unchanged");
                TestAssert.True(!File.ReadAllText(parentPath).Contains(storage.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase) &&
                    !File.ReadAllText(basinPath).Contains(storage.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase), "staged output has no absolute case dependency");
                GD.Print("POST_MAP_STAGE_OK files=2 basin_fields=5 parent_catalog_only=true reload=true links=true canonical_written=false");
            }
            finally { parent.Free(); catalog?.Dispose(); foreach (var d in definitions) d.Dispose(); }
        }
        finally { target.Free(); original.Free(); donor.Free(); owned?.Dispose(); foreach (var s in donorOwned) s.Dispose(); }
    }

    private static void AssertOnly(Dictionary<string, string> before, Dictionary<string, string> after, HashSet<string> allowed)
    {
        TestAssert.True(before.Count == after.Count && before.Keys.All(after.ContainsKey), "stage adds/removes no semantic keys");
        foreach (string key in before.Keys)
            if (!allowed.Contains(key)) TestAssert.Equal(before[key], after[key], "unrelated staged semantic value " + key);
    }
    private static void Set(byte[] bits, int i) => bits[i >> 3] |= (byte)(1 << (i & 7));
}
