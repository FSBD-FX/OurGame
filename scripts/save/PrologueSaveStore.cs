using System;
using System.IO;
using System.Text.Json;
using Knight.Prologue.Seamless;
using Knight.Presentation.Minimap;

namespace Knight.Save;

public enum PrologueSaveLoadStatus { Current, MigratableLegacy, NeedsNewLayoutChoice, Recovered }
public sealed record PrologueSaveLoadResult(PrologueSaveLoadStatus Status,
    PrologueSaveData Data, string PreservedLegacyPath);

public static class PrologueSaveStore
{
    public static PrologueSaveLoadResult LoadFullWorld(string path, string legacyPath, MinimapTerrainCatalog mapCatalog = null)
    {
        // Never rewrite either source during inspection. A malformed/foreign new
        // slot is protected too; explicit restart retains a same-directory backup.
        if (File.Exists(path))
        {
            PrologueSaveData current = LoadOrNew(path, CanyonWorldLayout.CurrentLayoutVersion, mapCatalog);
            return new(current.WasRecovered ? PrologueSaveLoadStatus.Recovered :
                PrologueSaveLoadStatus.Current, current, legacyPath);
        }
        if (!File.Exists(legacyPath))
            return new(PrologueSaveLoadStatus.Current, PrologueSaveData.NewLayoutV2(), legacyPath);
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(legacyPath));
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !TryVersion(root, out int version) || version != 2)
                return Choice(legacyPath);
            // Schema2 may carry absent/0/1/2 layout; none makes it schema3 current data.
            if (root.TryGetProperty("layoutVersion", out JsonElement layout) &&
                (layout.ValueKind != JsonValueKind.Number || !layout.TryGetInt32(out int value) ||
                 value is not (0 or 1 or 2))) return Choice(legacyPath);
            var data = JsonSerializer.Deserialize<PrologueSaveData>(root.GetRawText(), JsonOptions)
                ?? throw new JsonException("Save payload is null.");
            data.Version = PrologueSaveData.CurrentVersion;
            data.LayoutVersion = CanyonWorldLayout.CurrentLayoutVersion;
            ApplyLegacyLayout2Checkpoint(data, root);
            MigrateCollectionMetadata(data, root);
            return new(PrologueSaveLoadStatus.MigratableLegacy, data.Normalize(), legacyPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            JsonException or NotSupportedException or InvalidDataException or OverflowException)
        { return Choice(legacyPath); }
    }

    private static PrologueSaveLoadResult Choice(string path) =>
        new(PrologueSaveLoadStatus.NeedsNewLayoutChoice, PrologueSaveData.NewLayoutV2(), path);

    private static bool TryVersion(JsonElement root, out int version)
    {
        version = 0;
        return root.TryGetProperty("version", out JsonElement token) &&
            token.ValueKind == JsonValueKind.Number && token.TryGetInt32(out version);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static PrologueSaveData LoadOrNew(string path, int? requiredLayoutVersion = null, MinimapTerrainCatalog mapCatalog = null)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return new PrologueSaveData();
        try
        {
            string json = File.ReadAllText(path);
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return Recovered();
            // Inspect the wire token before migration or normalization can discard evidence.
            if (requiredLayoutVersion.HasValue &&
                (!document.RootElement.TryGetProperty("layoutVersion", out JsonElement layout) ||
                 layout.ValueKind != JsonValueKind.Number || !layout.TryGetInt32(out int rawLayout) ||
                 rawLayout != requiredLayoutVersion.Value)) return Recovered();
            if (!document.RootElement.TryGetProperty("version", out JsonElement versionElement) ||
                versionElement.ValueKind != JsonValueKind.Number ||
                !versionElement.TryGetInt32(out int version))
                return Recovered();
            return version switch
            {
                1 when !requiredLayoutVersion.HasValue => MigrateV1(document.RootElement),
                2 when !requiredLayoutVersion.HasValue => ReadData(json, document.RootElement, legacySchema: true, mapCatalog),
                3 => ReadData(json, document.RootElement, legacySchema: true, mapCatalog),
                PrologueSaveData.CurrentVersion => ReadData(json, document.RootElement, legacySchema: false, mapCatalog),
                _ => Recovered()
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           JsonException or NotSupportedException or InvalidDataException or
                                           OverflowException)
        {
            return Recovered();
        }
    }

    private static PrologueSaveData ReadData(string json, JsonElement root, bool legacySchema, MinimapTerrainCatalog mapCatalog)
    {
        // Current wire metadata is mandatory. Property initializers must not turn
        // an incomplete schema4 payload into an implicitly accepted migration.
        if (!legacySchema &&
            (!root.TryGetProperty("collectionRuleVersion", out JsonElement rule) ||
             rule.ValueKind != JsonValueKind.Number || !rule.TryGetInt32(out int version) || version != 1 ||
             !root.TryGetProperty("legacyCollectionQuotaExempt", out JsonElement exempt) ||
             exempt.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
             !root.TryGetProperty("collectedFragmentIds", out JsonElement fragments) ||
             fragments.ValueKind != JsonValueKind.Array))
            throw new InvalidDataException("Current save requires explicit quest fragment metadata.");
        var data = JsonSerializer.Deserialize<PrologueSaveData>(json, JsonOptions)
            ?? throw new JsonException("Save payload is null.");
        if (legacySchema)
        {
            if (TryVersion(root, out int legacyVersion) && legacyVersion == 2 &&
                data.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
                ApplyLegacyLayout2Checkpoint(data, root);
            data.Version = PrologueSaveData.CurrentVersion;
            MigrateCollectionMetadata(data, root);
        }
        PrologueSaveData normalized = data.Normalize(); // Core authority is validated first, unchanged.
        if (!legacySchema && mapCatalog != null &&
            ExplorationMapSaveCodec.TryRead(root, mapCatalog, out var map, out _))
            normalized.ExplorationMap = map;
        return normalized;
    }

    private static void ApplyLegacyLayout2Checkpoint(PrologueSaveData data, JsonElement root)
    {
        // All schema2→layout2 readers use the sector as the authority. A supplied
        // stable ID must still be valid, but cannot override that legal mapping.
        string id = data.HasActivatedCheckpoint ? data.CheckpointSector switch
        {
            "CanyonBasin" => "canyon.v2.basin-camp",
            "LostTemple" => "canyon.v2.temple-forecourt",
            _ => null
        } : data.CheckpointSector switch
        {
            "CanyonEntrance" or "CanyonDescent" or "CanyonBasin" or "CanyonAscent" or "LostTemple"
                => "canyon.v2.start",
            _ => null
        };
        if (id == null || root.TryGetProperty("checkpointId", out JsonElement checkpoint) &&
            (checkpoint.ValueKind != JsonValueKind.String ||
             !CanyonWorldLayout.TryResolveCheckpoint(checkpoint.GetString(), out _, out _)))
            throw new InvalidDataException("Legacy checkpoint cannot be mapped to layout2.");
        data.CheckpointId = id;
        data.CheckpointWorldX = 0;
        data.CheckpointWorldY = 0;
        if (data.OpenShortcuts == null)
            throw new InvalidDataException("Legacy shortcuts cannot be null.");
        if (data.ShortcutOpen) data.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
    }

    private static void MigrateCollectionMetadata(PrologueSaveData data, JsonElement root)
    {
        // Read raw activation BEFORE Normalize derives it from a non-start ID.
        // Schema2 callers first apply the existing legal sector→checkpoint map.
        // Neither coordinates nor injected fields grant new collection progress.
        data.CollectionRuleVersion = 1;
        data.CollectedFragmentIds = [];
        data.LegacyCollectionQuotaExempt = data.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion &&
            (ReadBoolean(root, "bossDefeated") || ReadBoolean(root, "completed") ||
             data.CheckpointId == "canyon.v2.temple-forecourt" && ReadBoolean(root, "hasActivatedCheckpoint"));
    }

    public static void SaveAtomic(string path, PrologueSaveData data, bool preserveExisting = false)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Save path is required.", nameof(path));
        if (data == null) throw new ArgumentNullException(nameof(data));
        PrologueSaveData normalized = data.Normalize();
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);
        string tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                foreach (JsonProperty property in JsonSerializer.SerializeToElement(normalized, JsonOptions).EnumerateObject())
                    property.WriteTo(writer);
                ExplorationMapSaveCodec.Write(writer, normalized.ExplorationMap);
                writer.WriteEndObject();
            }
            if (preserveExisting && File.Exists(fullPath))
                File.Copy(fullPath, fullPath + $".preserved-{Guid.NewGuid():N}.json", overwrite: false);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static PrologueSaveData MigrateV1(JsonElement root) => PrologueSaveData.MigrateV1(
        ReadBoolean(root, "completed"), ReadDouble(root, "bestHumanCompletionSeconds", -1d),
        ReadInt32(root, "bestHumanDeaths", -1));

    private static bool ReadBoolean(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.True;

    private static double ReadDouble(JsonElement root, string name, double fallback) =>
        root.TryGetProperty(name, out JsonElement value) && value.TryGetDouble(out double number) &&
        double.IsFinite(number) ? number : fallback;

    private static int ReadInt32(JsonElement root, string name, int fallback) =>
        root.TryGetProperty(name, out JsonElement value) && value.TryGetInt32(out int number)
            ? number : fallback;

    private static PrologueSaveData Recovered() => new() { WasRecovered = true };
}
