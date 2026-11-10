using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Knight.Presentation;

public static class ProductionAssetManifestParser
{
    private static readonly HashSet<string> RequiredCategories = new(StringComparer.Ordinal)
    {
        "ui", "player", "enemies", "boss", "environment", "vfx", "audio", "localization"
    };

    public static ProductionAssetManifest Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw Invalid("JSON is empty");

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid("root must be an object");

            int version = RequiredInt(root, "version", "manifest");
            if (version != 2) throw Invalid($"unsupported version {version}");
            JsonElement assetsElement = RequiredArray(root, "assets", "manifest");

            var entries = new List<ProductionAssetEntry>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var runtimePaths = new HashSet<string>(StringComparer.Ordinal);
            var foundCategories = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement element in assetsElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) throw Invalid("asset entry must be an object");
                string id = RequiredString(element, "id", "asset");
                if (!ids.Add(id)) throw Invalid($"duplicate asset id '{id}'");

                string category = RequiredString(element, "category", id);
                if (!RequiredCategories.Contains(category))
                    throw Invalid($"asset '{id}' has unknown category '{category}'");
                foundCategories.Add(category);

                string runtimePath = RequiredString(element, "runtimePath", id);
                ValidateRuntimePath(runtimePath, id);
                if (!runtimePaths.Add(runtimePath))
                    throw Invalid($"duplicate runtime path '{runtimePath}'");

                string source = RequiredString(element, "source", id);
                string license = RequiredString(element, "license", id);
                bool final = RequiredBool(element, "final", id);
                entries.Add(new ProductionAssetEntry(id, category, runtimePath, source, license, final));
            }

            if (entries.Count == 0) throw Invalid("assets must not be empty");
            foreach (string category in RequiredCategories)
            {
                if (!foundCategories.Contains(category))
                    throw Invalid($"missing required category '{category}'");
            }

            JsonElement slotsElement = RequiredArray(root, "replacementSlots", "manifest");
            var slots = new List<ProductionReplacementSlot>();
            var slotIds = new HashSet<string>(StringComparer.Ordinal);
            var slotPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonElement element in slotsElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    throw Invalid("replacement slot entry must be an object");
                string id = RequiredString(element, "id", "replacement slot");
                if (!slotIds.Add(id)) throw Invalid($"duplicate replacement slot id '{id}'");
                string scope = RequiredString(element, "scope", id);
                if (scope is not ("hud" or "player" or "sector"))
                    throw Invalid($"replacement slot '{id}' has unknown scope '{scope}'");
                string stablePath = RequiredString(element, "stablePath", id);
                ValidateStablePath(scope, stablePath, id);
                if (!slotPaths.Add(stablePath))
                    throw Invalid($"duplicate replacement stable path '{stablePath}'");
                slots.Add(new ProductionReplacementSlot(id, scope, stablePath));
            }
            ValidateExactReplacementSlots(slots);

            return new ProductionAssetManifest(version,
                new ReadOnlyCollection<ProductionAssetEntry>(entries),
                new ReadOnlyCollection<ProductionReplacementSlot>(slots));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Invalid production asset manifest: malformed JSON", exception);
        }
    }

    private static readonly IReadOnlyDictionary<string, string> RequiredReplacementSlots =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["slot.hud.energy"] = "Hud/FormalHud/ResourceCluster/Energy",
            ["slot.hud.heat"] = "Hud/FormalHud/ResourceCluster/Heat",
            ["slot.hud.mode.attack"] = "Hud/FormalHud/ModeSelector/Attack",
            ["slot.hud.mode.grapple"] = "Hud/FormalHud/ModeSelector/Grapple",
            ["slot.hud.loadout.1"] = "Hud/FormalHud/Loadout/Slot1",
            ["slot.hud.loadout.2"] = "Hud/FormalHud/Loadout/Slot2",
            ["slot.hud.loadout.3"] = "Hud/FormalHud/Loadout/Slot3",
            ["slot.hud.loadout.4"] = "Hud/FormalHud/Loadout/Slot4",
            ["slot.hud.aim-reticle"] = "Hud/FormalHud/AimReticle",
            ["slot.player.shield"] = "Player/VisualRoot/Shield",
            ["slot.player.mechanical-claw"] = "Player/VisualRoot/MechanicalClaw",
            ["slot.sector.background"] = "VisualRoot/Background",
            ["slot.sector.midground"] = "VisualRoot/Midground",
            ["slot.sector.gameplay-readable"] = "VisualRoot/GameplayReadable",
            ["slot.sector.foreground"] = "VisualRoot/Foreground",
            ["slot.sector.effects"] = "VisualRoot/Effects"
        };

    private static void ValidateExactReplacementSlots(IReadOnlyList<ProductionReplacementSlot> slots)
    {
        if (slots.Count != RequiredReplacementSlots.Count)
            throw Invalid($"replacementSlots must contain exactly {RequiredReplacementSlots.Count} entries");
        foreach ((string id, string stablePath) in RequiredReplacementSlots)
        {
            ProductionReplacementSlot slot = null;
            foreach (ProductionReplacementSlot candidate in slots)
                if (candidate.Id == id) { slot = candidate; break; }
            if (slot == null) throw Invalid($"missing required replacement slot '{id}'");
            if (slot.StablePath != stablePath)
                throw Invalid($"replacement slot '{id}' moved from required path '{stablePath}'");
        }
    }

    private static void ValidateStablePath(string scope, string stablePath, string id)
    {
        string prefix = scope switch
        {
            "hud" => "Hud/FormalHud/",
            "player" => "Player/VisualRoot/",
            _ => "VisualRoot/"
        };
        if (!stablePath.StartsWith(prefix, StringComparison.Ordinal) || stablePath.Contains('\\') ||
            stablePath.Contains("//", StringComparison.Ordinal) || stablePath.EndsWith('/'))
            throw Invalid($"replacement slot '{id}' stablePath must remain below {prefix}");
    }

    private static void ValidateRuntimePath(string runtimePath, string id)
    {
        if (!runtimePath.StartsWith("res://assets/", StringComparison.Ordinal) ||
            runtimePath.Contains('\\') || runtimePath.EndsWith("/", StringComparison.Ordinal))
        {
            throw Invalid($"asset '{id}' runtimePath must begin with res://assets/ and use forward slashes");
        }

        string[] segments = runtimePath["res://assets/".Length..].Split('/');
        foreach (string segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment) || segment is "." or "..")
                throw Invalid($"asset '{id}' runtimePath contains an invalid segment");
        }
    }

    private static JsonElement RequiredArray(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.Array)
            throw Invalid($"{context} '{name}' must be an array");
        return value;
    }

    private static string RequiredString(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw Invalid($"{context} '{name}' must be a non-empty string");
        }

        return value.GetString();
    }

    private static int RequiredInt(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) || !value.TryGetInt32(out int result))
            throw Invalid($"{context} '{name}' must be an integer");
        return result;
    }

    private static bool RequiredBool(JsonElement parent, string name, string context)
    {
        if (!parent.TryGetProperty(name, out JsonElement value) ||
            (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False))
        {
            throw Invalid($"{context} '{name}' must be a boolean");
        }

        return value.GetBoolean();
    }

    private static InvalidDataException Invalid(string reason) =>
        new($"Invalid production asset manifest: {reason}");
}
