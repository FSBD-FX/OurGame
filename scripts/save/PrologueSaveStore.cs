using System;
using System.IO;
using System.Text.Json;

namespace Knight.Save;

public static class PrologueSaveStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static PrologueSaveData LoadOrNew(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return new PrologueSaveData();
        try
        {
            string json = File.ReadAllText(path);
            using JsonDocument document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("version", out JsonElement versionElement) ||
                !versionElement.TryGetInt32(out int version))
                return Recovered();
            return version switch
            {
                1 => MigrateV1(document.RootElement),
                PrologueSaveData.CurrentVersion =>
                    (JsonSerializer.Deserialize<PrologueSaveData>(json, JsonOptions) ??
                     throw new JsonException("Save payload is null.")).Normalize(),
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

    public static void SaveAtomic(string path, PrologueSaveData data)
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
            File.WriteAllText(tempPath, JsonSerializer.Serialize(normalized, JsonOptions));
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
