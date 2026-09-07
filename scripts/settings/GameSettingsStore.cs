using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Knight.Settings;

public static class GameSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static GameSettingsData LoadOrDefault(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return new GameSettingsData();
        try
        {
            GameSettingsData data = JsonSerializer.Deserialize<GameSettingsData>(File.ReadAllText(path), JsonOptions);
            return data == null || data.Version != GameSettingsData.CurrentVersion
                ? new GameSettingsData()
                : data.Normalize();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           JsonException or NotSupportedException)
        {
            return new GameSettingsData();
        }
    }

    public static void SaveAtomic(string path, GameSettingsData data)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Settings path is required.", nameof(path));
        if (data == null) throw new ArgumentNullException(nameof(data));
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(directory);
        string tempPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(tempPath, JsonSerializer.Serialize(data.Normalize(), JsonOptions));
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
