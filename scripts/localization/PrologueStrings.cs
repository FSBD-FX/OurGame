using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Knight.Settings;

namespace Knight.Localization;

public static class PrologueStrings
{
    private static IReadOnlyDictionary<GameLanguage, IReadOnlyDictionary<string, string>> _languages =
        new ReadOnlyDictionary<GameLanguage, IReadOnlyDictionary<string, string>>(
            new Dictionary<GameLanguage, IReadOnlyDictionary<string, string>>());

    public static void Load(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw Invalid("JSON is empty");
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("version", out JsonElement version) || !version.TryGetInt32(out int number) || number != 1)
                throw Invalid("version must be 1");
            if (!root.TryGetProperty("languages", out JsonElement languages) || languages.ValueKind != JsonValueKind.Object)
                throw Invalid("languages must be an object");

            Dictionary<string, string> chinese = ReadLanguage(languages, "zh-CN");
            Dictionary<string, string> english = ReadLanguage(languages, "en");
            var keys = new HashSet<string>(chinese.Keys, StringComparer.Ordinal);
            keys.UnionWith(english.Keys);
            foreach (string key in keys)
            {
                if (!chinese.ContainsKey(key)) throw Invalid($"missing zh-CN translation for '{key}'");
                if (!english.ContainsKey(key)) throw Invalid($"missing en translation for '{key}'");
            }

            _languages = new ReadOnlyDictionary<GameLanguage, IReadOnlyDictionary<string, string>>(
                new Dictionary<GameLanguage, IReadOnlyDictionary<string, string>>
                {
                    [GameLanguage.ChineseSimplified] = new ReadOnlyDictionary<string, string>(chinese),
                    [GameLanguage.English] = new ReadOnlyDictionary<string, string>(english)
                });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Invalid prologue string table: malformed JSON", exception);
        }
    }

    public static string Get(string key, GameLanguage language)
    {
        if (!_languages.TryGetValue(language, out IReadOnlyDictionary<string, string> table) ||
            key == null || !table.TryGetValue(key, out string value))
        {
            throw new KeyNotFoundException($"Missing prologue string '{key}' for language '{language}'.");
        }
        return value;
    }

    public static int KeyCount(GameLanguage language) =>
        _languages.TryGetValue(language, out IReadOnlyDictionary<string, string> table) ? table.Count : 0;

    private static Dictionary<string, string> ReadLanguage(JsonElement languages, string code)
    {
        if (!languages.TryGetProperty(code, out JsonElement language) || language.ValueKind != JsonValueKind.Object)
            throw Invalid($"language '{code}' must be an object");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty property in language.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.Value.GetString()))
                throw Invalid($"translation '{property.Name}' in '{code}' must be non-empty");
            result.Add(property.Name, property.Value.GetString());
        }
        return result;
    }

    private static InvalidDataException Invalid(string reason) =>
        new($"Invalid prologue string table: {reason}");
}
