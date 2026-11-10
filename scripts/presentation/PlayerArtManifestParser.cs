using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Knight.Presentation;

public static class PlayerArtManifestParser
{
    public static PlayerArtManifest Parse(string json, string textureResourcePath)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw Invalid("<manifest>", "JSON is empty");
        if (string.IsNullOrWhiteSpace(textureResourcePath))
            throw Invalid("<manifest>", "texture resource path is empty");

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string characterId = RequiredString(root, "characterId", "<manifest>");
            RequiredString(root, "game_input", "<manifest>");

            JsonElement animationRows = RequiredObject(
                RequiredObject(root, "animation", "<manifest>"), "rows", "<manifest>");
            JsonElement frameLayout = RequiredObject(root, "frame_layout", "<manifest>");
            int sheetWidth = RequiredInt(frameLayout, "sheetWidth", "<manifest>");
            int sheetHeight = RequiredInt(frameLayout, "sheetHeight", "<manifest>");
            if (sheetWidth <= 0 || sheetHeight <= 0)
                throw Invalid("<manifest>", "sheet dimensions must be positive");

            JsonElement layoutRows = RequiredObject(frameLayout, "rows", "<manifest>");
            var clips = new Dictionary<string, PlayerAnimationClip>(StringComparer.Ordinal);

            foreach (JsonProperty layoutRow in layoutRows.EnumerateObject())
            {
                string clipName = layoutRow.Name;
                if (!animationRows.TryGetProperty(clipName, out JsonElement metadata) ||
                    metadata.ValueKind != JsonValueKind.Object)
                {
                    throw Invalid(clipName, "animation metadata is missing");
                }

                if (layoutRow.Value.ValueKind != JsonValueKind.Array)
                    throw Invalid(clipName, "frame rectangles must be an array");

                int expectedFrameCount = RequiredInt(metadata, "frames", clipName);
                double fps = RequiredDouble(metadata, "fps", clipName);
                bool loop = RequiredBool(metadata, "loop", clipName);
                if (fps <= 0d)
                    throw Invalid(clipName, "fps must be positive");

                var rectangles = new List<PlayerAtlasRect>();
                foreach (JsonElement rectangle in layoutRow.Value.EnumerateArray())
                {
                    if (rectangle.ValueKind != JsonValueKind.Object)
                        throw Invalid(clipName, "frame rectangle must be an object");

                    int x = RequiredInt(rectangle, "x", clipName);
                    int y = RequiredInt(rectangle, "y", clipName);
                    int width = RequiredInt(rectangle, "w", clipName);
                    int height = RequiredInt(rectangle, "h", clipName);
                    if (width <= 0 || height <= 0)
                        throw Invalid(clipName, "frame dimensions must be positive");
                    if (x < 0 || y < 0 || width > sheetWidth || height > sheetHeight ||
                        x > sheetWidth - width || y > sheetHeight - height)
                    {
                        throw Invalid(clipName, "frame rectangle is outside the atlas");
                    }

                    rectangles.Add(new PlayerAtlasRect(x, y, width, height));
                }

                if (expectedFrameCount != rectangles.Count)
                {
                    throw Invalid(clipName,
                        $"metadata declares {expectedFrameCount} frames but layout contains {rectangles.Count}");
                }

                clips.Add(clipName, new PlayerAnimationClip(
                    clipName,
                    fps,
                    loop,
                    new ReadOnlyCollection<PlayerAtlasRect>(rectangles)));
            }

            foreach (JsonProperty animationRow in animationRows.EnumerateObject())
            {
                if (!clips.ContainsKey(animationRow.Name))
                    throw Invalid(animationRow.Name, "frame layout is missing");
            }

            return new PlayerArtManifest(
                characterId,
                textureResourcePath,
                sheetWidth,
                sheetHeight,
                new ReadOnlyDictionary<string, PlayerAnimationClip>(clips));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Invalid player art manifest '<manifest>': malformed JSON", exception);
        }
    }

    private static JsonElement RequiredObject(JsonElement parent, string propertyName, string clipName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Object)
            throw Invalid(clipName, $"'{propertyName}' must be an object");
        return value;
    }

    private static string RequiredString(JsonElement parent, string propertyName, string clipName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw Invalid(clipName, $"'{propertyName}' must be a non-empty string");
        }

        return value.GetString()!;
    }

    private static int RequiredInt(JsonElement parent, string propertyName, string clipName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value) || !value.TryGetInt32(out int result))
            throw Invalid(clipName, $"'{propertyName}' must be an integer");
        return result;
    }

    private static double RequiredDouble(JsonElement parent, string propertyName, string clipName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value) || !value.TryGetDouble(out double result))
            throw Invalid(clipName, $"'{propertyName}' must be a number");
        return result;
    }

    private static bool RequiredBool(JsonElement parent, string propertyName, string clipName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            throw Invalid(clipName, $"'{propertyName}' must be a boolean");
        }

        return value.GetBoolean();
    }

    private static InvalidDataException Invalid(string clipName, string reason) =>
        new($"Invalid player art manifest clip '{clipName}': {reason}");
}
