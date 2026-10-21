using System;
using System.Text.Json;
using Knight.Presentation.Minimap;

namespace Knight.Save;

// Optional memory is deliberately isolated from strong core deserialization.
public static class ExplorationMapSaveCodec
{
    private const int MaximumBytes = MinimapTerrainCatalog.MaximumBits / 8;
    private const int MaximumEncodedLength = ((MaximumBytes + 2) / 3) * 4;

    public static bool TryRead(JsonElement root, MinimapTerrainCatalog catalog,
        out ExplorationMapSaveData data, out string diagnostic)
    {
        data = null; diagnostic = string.Empty;
        if (catalog == null || root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("explorationMap", out JsonElement map) || map.ValueKind == JsonValueKind.Null)
            return false;
        if (map.ValueKind != JsonValueKind.Object) return Reject("Optional map is not an object.", out diagnostic);
        if (!map.TryGetProperty("formatVersion", out var format) || format.ValueKind != JsonValueKind.Number ||
            !format.TryGetInt32(out int version) || version != 1)
            return Reject("Optional map format is unsupported.", out diagnostic);
        if (!map.TryGetProperty("terrainRevision", out var revision) || revision.ValueKind != JsonValueKind.String ||
            !revision.ValueEquals(catalog.Revision))
            return Reject("Optional map terrain revision does not match.", out diagnostic);
        if (catalog.CellCount < 0 || catalog.CellCount > MinimapTerrainCatalog.MaximumBits ||
            catalog.BoundaryCount < 0 || catalog.BoundaryCount > MinimapTerrainCatalog.MaximumBits ||
            !ReadBits(map, "freeCellsBase64", catalog.CellCount, out string cells) ||
            !ReadBits(map, "boundarySegmentsBase64", catalog.BoundaryCount, out string boundaries))
            return Reject("Optional map bitset is invalid.", out diagnostic);
        data = new() { TerrainRevision = catalog.Revision, FreeCellsBase64 = cells, BoundarySegmentsBase64 = boundaries };
        return true;
    }

    public static void Write(Utf8JsonWriter writer, ExplorationMapSaveData data)
    {
        // No catalog is available at this boundary. Intrinsically malformed DTOs
        // are omitted, never repaired into core progress; count/revision/padding
        // are independently checked against the actual catalog on read.
        if (data == null || data.FormatVersion != 1 || string.IsNullOrWhiteSpace(data.TerrainRevision) ||
            !DecodeBounded(data.FreeCellsBase64, out _) || !DecodeBounded(data.BoundarySegmentsBase64, out _)) return;
        writer.WritePropertyName("explorationMap"); writer.WriteStartObject();
        writer.WriteNumber("formatVersion", 1); writer.WriteString("terrainRevision", data.TerrainRevision);
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
