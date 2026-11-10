using System;
using System.Security.Cryptography;
using System.Text.Json;
using Knight.Presentation.Minimap;

namespace Knight.Save;

// Optional memory is deliberately isolated from strong core deserialization.
public static class ExplorationMapSaveCodec
{
    private const int MaximumBytes = MinimapTerrainCatalog.MaximumBits / 8;
    private const int MaximumEncodedLength = ((MaximumBytes + 2) / 3) * 4;
    private const string ShelfRevision = "48020BD483F79CC058185DB230C17608740B5FC43309E2F783F0C7C02B45CA15";
    private const string PostRevision = "BEE5E8069591E12D83F10EB0477FFCA3CFEAC1A314DE6C6C958F7E257271E3C2";
    private const string BaffleRevision = "D8581BF1A4012D09B85953DDF863B607273AE569EBA7B226D6989098DCA09A2D";
    private const string AnnexRevision = "5A99D13578D8271BD99AA01678884F615A681958281D8A621CBA2D27F86581B4";

    public static bool TryRead(JsonElement root, MinimapTerrainCatalog catalog,
        out ExplorationMapSaveData data, out string diagnostic)
    {
        data = null; diagnostic = string.Empty;
        if (catalog == null || root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("explorationMap", out JsonElement map) || map.ValueKind == JsonValueKind.Null)
            return false;
        if (map.ValueKind != JsonValueKind.Object) return Reject("Optional map is not an object.", out diagnostic);
        if (!map.TryGetProperty("formatVersion", out var format) || format.ValueKind != JsonValueKind.Number ||
            !format.TryGetInt32(out int version) || version != (catalog.CellSize == 8 ? 1 : 2))
            return Reject("Optional map format is unsupported.", out diagnostic);
        if (!map.TryGetProperty("terrainRevision", out var revision) || revision.ValueKind != JsonValueKind.String)
            return Reject("Optional map terrain revision does not match.", out diagnostic);
        if (!revision.ValueEquals(catalog.Revision))
            return TryReadKnownRevision(map, revision.GetString(), catalog, out data, out diagnostic);
        if (catalog.CellCount < 0 || catalog.CellCount > MinimapTerrainCatalog.MaximumBits ||
            catalog.BoundaryCount < 0 || catalog.BoundaryCount > MinimapTerrainCatalog.MaximumBits ||
            !ReadBits(map, "freeCellsBase64", catalog.CellCount, out string cells) ||
            !ReadBits(map, "boundarySegmentsBase64", catalog.BoundaryCount, out string boundaries))
            return Reject("Optional map bitset is invalid.", out diagnostic);
        data = new() { FormatVersion = version, TerrainRevision = catalog.Revision, FreeCellsBase64 = cells, BoundarySegmentsBase64 = boundaries };
        return true;
    }

    private static bool TryReadKnownRevision(JsonElement map, string oldRevision, MinimapTerrainCatalog catalog,
        out ExplorationMapSaveData data, out string diagnostic)
    {
        data = null; diagnostic = string.Empty;
        // Exact authoring deltas, not a generic copied-label bypass.
        if (Attested(catalog, AnnexRevision, 13908,
                "D6EC6A12A6D3CF6BDB0C1A9C981565A6EC43D901BB8175C987DFE0A19ADC61CE", 158))
        {
            if (!TryReadAnnexPrior(map, oldRevision, out var prior, out diagnostic)) return false;
            data = AnnexDelta(prior.FreeCellsBase64, prior.BoundarySegmentsBase64);
            return true;
        }
        bool shelfTarget = Attested(catalog, ShelfRevision, 12436,
            "1D2D41D9191238A87CC679F04F522D09ED99D18C3374C710778364B986E18C4F");
        bool postTarget = Attested(catalog, PostRevision, 12460,
            "566F599CC762B2EBC7D6D3BAC94628422C61565B5D6081F77DFD5B2661E1F8B4");
        bool baffleTarget = Attested(catalog, BaffleRevision, 12468,
            "CD66FCE70550F1D5414634B3380D2E73A98ADA2963F94EC104976AE6D0DE3566", 144);
        if (!shelfTarget && !postTarget && !baffleTarget)
            return Reject("Optional map terrain revision does not match.", out diagnostic);
        if (oldRevision == "B264EB610D985D63288925D90C9F239950A1D406AF7063E0BF5C470F8E62089E")
        {
            if (!ReadShelfDelta(map, out data, out diagnostic)) return false;
            if (postTarget || baffleTarget) data = PostDelta(data);
            if (baffleTarget) data = BaffleDelta(data);
            return true;
        }
        if (baffleTarget && oldRevision == PostRevision)
        {
            if (!ReadBits(map, "freeCellsBase64", 518400, out string postCells) ||
                !ReadBits(map, "boundarySegmentsBase64", 12460, out string postBoundaries))
                return Reject("Optional prior baffle map bitset is invalid.", out diagnostic);
            data = BaffleDelta(new ExplorationMapSaveData { TerrainRevision = PostRevision,
                FreeCellsBase64 = postCells, BoundarySegmentsBase64 = postBoundaries });
            return true;
        }
        if ((!postTarget && !baffleTarget) || oldRevision != ShelfRevision)
            return Reject("Optional map terrain revision does not match.", out diagnostic);
        if (!ReadBits(map, "freeCellsBase64", 518400, out string cells) ||
            !ReadBits(map, "boundarySegmentsBase64", 12436, out string boundaries))
            return Reject("Optional prior post map bitset is invalid.", out diagnostic);
        data = PostDelta(new ExplorationMapSaveData { TerrainRevision = ShelfRevision,
            FreeCellsBase64 = cells, BoundarySegmentsBase64 = boundaries });
        if (baffleTarget) data = BaffleDelta(data);
        return true;
    }

    private static bool Attested(MinimapTerrainCatalog catalog, string revision, int boundaries, string sha,
        int solids = 143) =>
        catalog.Revision == revision && catalog.CellCount == 518400 && catalog.Columns == 960 &&
        catalog.Rows == 540 && catalog.SolidCount == solids && catalog.BoundaryCount == boundaries &&
        Convert.ToHexString(SHA256.HashData(catalog.SerializedValues)) == sha;

    private static ExplorationMapSaveData AnnexDelta(string priorCells, string priorEdges)
    {
        byte[] cells = Convert.FromBase64String(priorCells);
        // Exact delta's new room and the two new supports protruding west of
        // it. Include cells touching every edge; only clear, never reveal.
        Clear(639, 800, 449, 539); // room [5120,3600]..[6400,4320]
        Clear(637, 657, 459, 462); // upper lip [5100,3676]..[5260,3700]
        Clear(584, 680, 499, 502); // west bank [4680,3996]..[5440,4020]
        byte[] old = Convert.FromBase64String(priorEdges), edges = new byte[1739];
        for (int id = 0; id < 12468; id++)
        {
            if ((old[id >> 3] & (1 << (id & 7))) == 0) continue;
            // Complete owner/endpoint/normal comparison of the two attested
            // catalogs proves these runs; new[6664,8104) remains all zero.
            int next = id < 6664 ? id : id + 1440;
            edges[next >> 3] |= (byte)(1 << (next & 7));
        }
        return new ExplorationMapSaveData { TerrainRevision = AnnexRevision,
            FreeCellsBase64 = Convert.ToBase64String(cells), BoundarySegmentsBase64 = Convert.ToBase64String(edges) };

        void Clear(int firstColumn, int lastColumn, int firstRow, int lastRow)
        {
            for (int row = firstRow; row <= lastRow; row++)
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                int id = row * 960 + column;
                cells[id >> 3] &= (byte)~(1 << (id & 7));
            }
        }
    }

    private static bool TryReadAnnexPrior(JsonElement map, string revision,
        out ExplorationMapSaveData prior, out string diagnostic)
    {
        prior = null; diagnostic = string.Empty;
        if (revision == "B264EB610D985D63288925D90C9F239950A1D406AF7063E0BF5C470F8E62089E")
        {
            if (!ReadShelfDelta(map, out prior, out diagnostic)) return false;
        }
        else
        {
            int count = revision switch { ShelfRevision => 12436, PostRevision => 12460, BaffleRevision => 12468, _ => -1 };
            if (count < 0) return Reject("Optional annex map source revision does not match.", out diagnostic);
            if (!ReadBits(map, "freeCellsBase64", 518400, out string cells) ||
                !ReadBits(map, "boundarySegmentsBase64", count, out string edges))
                return Reject("Optional prior annex map bitset is invalid.", out diagnostic);
            prior = new ExplorationMapSaveData { TerrainRevision = revision,
                FreeCellsBase64 = cells, BoundarySegmentsBase64 = edges };
        }
        // Continue only the exact previously supported authoring deltas. Never
        // feed differently sized historical bits directly to the annex mapper.
        if (prior.TerrainRevision == ShelfRevision) prior = PostDelta(prior);
        if (prior.TerrainRevision == PostRevision) prior = BaffleDelta(prior);
        return true;
    }

    private static ExplorationMapSaveData BaffleDelta(ExplorationMapSaveData prior)
    {
        byte[] cells = Convert.FromBase64String(prior.FreeCellsBase64);
        // New baffle world bounds [4660,1620]..[4664,1641]. Include touching cells.
        for (int row = 202; row <= 205; row++)
            for (int column = 582; column <= 583; column++)
            {
                int id = row * 960 + column;
                cells[id >> 3] &= (byte)~(1 << (id & 7));
            }
        byte[] old = Convert.FromBase64String(prior.BoundarySegmentsBase64), edges = new byte[1559];
        for (int id = 0; id < 12460; id++)
        {
            // Two original Grip8 boundary segments touch the baffle. The eight
            // inserted segments remain dark; unchanged geometry keeps its bit.
            if (id is 9284 or 9296 || (old[id >> 3] & (1 << (id & 7))) == 0) continue;
            int next = id < 9358 ? id : id + 8;
            edges[next >> 3] |= (byte)(1 << (next & 7));
        }
        return new ExplorationMapSaveData { TerrainRevision = BaffleRevision,
            FreeCellsBase64 = Convert.ToBase64String(cells), BoundarySegmentsBase64 = Convert.ToBase64String(edges) };
    }

    private static ExplorationMapSaveData PostDelta(ExplorationMapSaveData prior)
    {
        byte[] cells = Convert.FromBase64String(prior.FreeCellsBase64);
        // Forget cells touching the new owner58 footprint, including its border.
        for (int row = 465; row <= 484; row++)
            for (int column = 526; column <= 531; column++)
            {
                int id = row * 960 + column;
                cells[id >> 3] &= (byte)~(1 << (id & 7));
            }
        byte[] old = Convert.FromBase64String(prior.BoundarySegmentsBase64), edges = new byte[1558];
        for (int id = 0; id < 12436; id++)
        {
            if (id is >= 7348 and < 7374 || (old[id >> 3] & (1 << (id & 7))) == 0) continue;
            int next = id < 7348 ? id : id + 24;
            edges[next >> 3] |= (byte)(1 << (next & 7));
        }
        return new ExplorationMapSaveData { TerrainRevision = PostRevision,
            FreeCellsBase64 = Convert.ToBase64String(cells), BoundarySegmentsBase64 = Convert.ToBase64String(edges) };
    }

    private static bool ReadShelfDelta(JsonElement map, out ExplorationMapSaveData data, out string diagnostic)
    {
        data = null; diagnostic = string.Empty;
        if (!ReadBits(map, "freeCellsBase64", 518400, out string cells) ||
            !ReadBits(map, "boundarySegmentsBase64", 12440, out string boundaries))
            return Reject("Optional legacy map bitset is invalid.", out diagnostic);
        byte[] migratedCells = Convert.FromBase64String(cells);
        // Old changed solid48 world footprint [2856,3720]..[3008,3744].
        // Include cells touching its border; this only forgets old bits.
        for (int row = 464; row <= 468; row++)
            for (int column = 356; column <= 376; column++)
            {
                int id = row * 960 + column;
                migratedCells[id >> 3] &= (byte)~(1 << (id & 7));
            }
        byte[] oldBoundaries = Convert.FromBase64String(boundaries), migratedBoundaries = new byte[1555];
        for (int old = 0; old < 12440; old++)
        {
            // Drop the entire changed owner's old48/current44 boundaries.
            if (old is >= 6526 and < 6574 || (oldBoundaries[old >> 3] & (1 << (old & 7))) == 0) continue;
            int target = old < 6526 ? old : old - 4;
            migratedBoundaries[target >> 3] |= (byte)(1 << (target & 7));
        }
        data = new() { TerrainRevision = ShelfRevision, FreeCellsBase64 = Convert.ToBase64String(migratedCells),
            BoundarySegmentsBase64 = Convert.ToBase64String(migratedBoundaries) };
        return true;
    }

    public static void Write(Utf8JsonWriter writer, ExplorationMapSaveData data)
    {
        // No catalog is available at this boundary. Intrinsically malformed DTOs
        // are omitted, never repaired into core progress; count/revision/padding
        // are independently checked against the actual catalog on read.
        if (data == null || data.FormatVersion is not (1 or 2) || string.IsNullOrWhiteSpace(data.TerrainRevision) ||
            !DecodeBounded(data.FreeCellsBase64, out _) || !DecodeBounded(data.BoundarySegmentsBase64, out _)) return;
        writer.WritePropertyName("explorationMap"); writer.WriteStartObject();
        writer.WriteNumber("formatVersion", data.FormatVersion); writer.WriteString("terrainRevision", data.TerrainRevision);
        writer.WriteString("freeCellsBase64", data.FreeCellsBase64);
        writer.WriteString("boundarySegmentsBase64", data.BoundarySegmentsBase64); writer.WriteEndObject();
    }

    private static bool ReadBits(JsonElement map, string name, int count, out string text)
    {
        text = null;
        if (!map.TryGetProperty(name, out var token) || token.ValueKind != JsonValueKind.String) return false;
        // JsonDocument already owns the wire. Bound encoded token length before
        // base64 decoding or a decoded-byte allocation, including escaped input.
        // GetString can allocate the JSON string, but never a large decoded buffer.
        string value = token.GetString();
        int bytes = (count + 7) / 8;
        if (value == null || value.Length != ((bytes + 2) / 3) * 4 || value.Length > MaximumEncodedLength ||
            !DecodeBounded(value, out byte[] decoded) || decoded.Length != bytes ||
            count % 8 != 0 && (decoded[^1] & ~((1 << (count % 8)) - 1)) != 0) return false;
        text = value; return true;
    }

    private static bool DecodeBounded(string text, out byte[] decoded)
    {
        decoded = null;
        if (text == null || text.Length > MaximumEncodedLength || text.Length % 4 != 0) return false;
        var buffer = new byte[Math.Min(MaximumBytes, text.Length / 4 * 3)];
        if (!Convert.TryFromBase64String(text, buffer, out int written) ||
            text.Length != ((written + 2) / 3) * 4 || Convert.ToBase64String(buffer, 0, written) != text) return false;
        if (written != buffer.Length) Array.Resize(ref buffer, written);
        decoded = buffer; return true;
    }
    private static bool Reject(string reason, out string diagnostic) { diagnostic = reason; return false; }
}
