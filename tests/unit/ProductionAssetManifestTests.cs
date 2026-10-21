using System;
using System.IO;
using Godot;
using Knight.Presentation;
using System.Text.Json.Nodes;

namespace Knight.Tests.Unit;

public static class ProductionAssetManifestTests
{
    private static readonly string[] Categories =
    [
        "ui", "player", "enemies", "boss", "environment", "vfx", "audio", "localization"
    ];

    public static void Run()
    {
        CurrentManifestIsValid();
        FormalEnemyAtlasesUseBroadLowNoiseValueGroups();
        RejectsMissingCategories();
        RejectsDuplicateIds();
        RejectsExternalRuntimePaths();
        RequiresExistingFilesAndDeclaredLicenses();
        Task10ReplacementSlotsAreExplicit();
    }

    public static void Task10ReplacementSlotsAreExplicit()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "production",
            "prologue-assets.json");
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(File.ReadAllText(path));
        string[] requiredIds =
        [
            "slot.hud.energy", "slot.hud.heat", "slot.hud.mode.attack", "slot.hud.mode.grapple",
            "slot.hud.loadout.1", "slot.hud.loadout.2", "slot.hud.loadout.3",
            "slot.hud.loadout.4", "slot.hud.aim-reticle", "slot.player.shield",
            "slot.player.mechanical-claw", "slot.sector.background", "slot.sector.midground",
            "slot.sector.gameplay-readable", "slot.sector.foreground", "slot.sector.effects"
        ];
        var ids = new System.Collections.Generic.HashSet<string>(
            System.Linq.Enumerable.Select(manifest.ReplacementSlots, slot => slot.Id), StringComparer.Ordinal);
        foreach (string requiredId in requiredIds)
            TestAssert.True(ids.Contains(requiredId),
                $"Task10 production manifest explicitly declares replacement {requiredId}");
        TestAssert.Equal(requiredIds.Length, manifest.ReplacementSlots.Count,
            "Task10 manifest exposes the exact replacement-slot set without broad fallback entries");
        Task10ManifestMutationsFailClosed();
    }

    private static void Task10ManifestMutationsFailClosed()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "production",
            "prologue-assets.json");
        string json = File.ReadAllText(path);
        ExpectInvalid(RemoveReplacementSlot(json, "slot.hud.energy"),
            "exactly 16", "removing one named replacement slot fails closed");
        ExpectInvalid(json.Replace("Hud/FormalHud/ResourceCluster/Energy",
                "Hud/FormalHud/ResourceCluster/MovedEnergy", StringComparison.Ordinal),
            "moved", "moving one named replacement slot fails closed");
        ExpectInvalid(json.Replace("slot.hud.heat", "slot.hud.energy", StringComparison.Ordinal),
            "duplicate replacement slot id", "duplicate replacement slot ids fail closed");
        ExpectInvalid(json.Replace("Hud/FormalHud/ResourceCluster/Heat",
                "Hud/FormalHud/ResourceCluster/Energy", StringComparison.Ordinal),
            "duplicate replacement stable path", "duplicate replacement stable paths fail closed");
    }

    private static string RemoveReplacementSlot(string json, string id)
    {
        JsonObject root = JsonNode.Parse(json)?.AsObject() ??
            throw new InvalidDataException("manifest mutation fixture must parse");
        JsonArray slots = root["replacementSlots"]?.AsArray() ??
            throw new InvalidDataException("manifest mutation fixture requires replacementSlots");
        int removed = 0;
        for (int index = slots.Count - 1; index >= 0; index--)
        {
            if (!string.Equals(slots[index]?["id"]?.GetValue<string>(), id,
                    StringComparison.Ordinal)) continue;
            slots.RemoveAt(index);
            removed++;
        }
        TestAssert.Equal(1, removed,
            "replacement-slot removal mutation changes exactly one parsed JSON entry");
        return root.ToJsonString();
    }

    private static void FormalEnemyAtlasesUseBroadLowNoiseValueGroups()
    {
        AssertAtlas("assets/enemies/formal/rust-shell.png", 256, 160, 4, 32);
        AssertAtlas("assets/enemies/formal/grave-lamp-moth.png", 256, 160, 4, 32);
        AssertAtlas("assets/enemies/formal/bell-back-laborer.png", 256, 160, 4, 32);
        AssertAtlas("assets/enemies/formal/enemy-atlas.png", 768, 160, 6, 32, 24);
        AssertAtlas("assets/boss/fallen-bellkeeper/fallen-bellkeeper.png", 512, 256, 8, 45);
    }

    private static void AssertAtlas(string relativePath, int width, int height,
        int minimumOpaquePercent, int maximumOpaquePercent, int maximumColors = 16)
    {
        string absolute = Path.Combine(Directory.GetCurrentDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Image image = Image.LoadFromFile(absolute);
        TestAssert.True(image != null && image.GetWidth() == width && image.GetHeight() == height,
            $"{relativePath} has its deterministic production atlas dimensions");
        int opaque = 0;
        var colors = new System.Collections.Generic.HashSet<uint>();
        for (int y = 0; y < image.GetHeight(); y += 2)
        for (int x = 0; x < image.GetWidth(); x += 2)
        {
            Color color = image.GetPixel(x, y);
            if (color.A <= .05f) continue;
            opaque++;
            colors.Add(color.ToRgba32());
        }
        int samples = (image.GetWidth() / 2) * (image.GetHeight() / 2);
        int percent = opaque * 100 / samples;
        TestAssert.True(percent >= minimumOpaquePercent && percent <= maximumOpaquePercent,
            $"{relativePath} uses a bounded broad silhouette instead of a filled sheet");
        TestAssert.True(colors.Count >= 4 && colors.Count <= maximumColors,
            $"{relativePath} uses broad value groups and sparse corrosion without micro-noise");
    }

    private static void CurrentManifestIsValid()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "production", "prologue-assets.json");
        TestAssert.True(File.Exists(path), "production asset manifest exists in the project");
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(File.ReadAllText(path));
        TestAssert.Equal(2, manifest.Version, "current production manifest uses fail-closed schema v2");
        TestAssert.Equal(8, manifest.Assets.Count, "current manifest declares all eight production categories");
        TestAssert.Equal(0, manifest.RequireRuntimeFiles(Directory.GetCurrentDirectory()).Count,
            "current nonfinal production manifest does not claim missing files are shipped");
    }

    private static void RejectsMissingCategories()
    {
        string json = ManifestJson(skipCategory: "audio");
        ExpectInvalid(json, "audio", "manifest names a missing required category");
    }

    private static void RejectsDuplicateIds()
    {
        string json = ManifestJson(duplicateFirstId: true);
        ExpectInvalid(json, "duplicate", "manifest rejects duplicate asset ids");
    }

    private static void RejectsExternalRuntimePaths()
    {
        string json = ManifestJson(firstRuntimePath: @"E:\OurGame\ArtProduction\player.png");
        ExpectInvalid(json, "res://assets/", "runtime paths stay inside the Godot asset tree");
    }

    private static void RequiresExistingFilesAndDeclaredLicenses()
    {
        string missingLicense = ManifestJson(firstLicense: string.Empty);
        ExpectInvalid(missingLicense, "license", "every production asset declares a license");

        string root = Path.Combine(Path.GetTempPath(), "knight-production-assets-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "assets", "production"));
            File.WriteAllText(Path.Combine(root, "assets", "production", "ui.txt"), "owned");
            ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(
                ManifestJson(firstRuntimePath: "res://assets/production/ui.txt", firstFinal: true));
            TestAssert.Equal(0, manifest.RequireRuntimeFiles(root).Count,
                "existing final runtime asset satisfies the file contract");

            ProductionAssetManifest missing = ProductionAssetManifestParser.Parse(
                ManifestJson(firstRuntimePath: "res://assets/production/missing-ui.txt", firstFinal: true));
            TestAssert.Equal(1, missing.RequireRuntimeFiles(root).Count,
                "missing final runtime asset is reported exactly once");

            ProductionAssetManifest planned = ProductionAssetManifestParser.Parse(
                ManifestJson(firstRuntimePath: "res://assets/production/planned-ui.txt", firstFinal: false));
            TestAssert.Equal(0, planned.RequireRuntimeFiles(root).Count,
                "planned nonfinal asset does not masquerade as a missing shipped file");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static string ManifestJson(string skipCategory = null, bool duplicateFirstId = false,
        string firstRuntimePath = "res://assets/production/planned-ui.txt",
        string firstLicense = "Project-owned",
        bool firstFinal = false)
    {
        var entries = new System.Text.StringBuilder();
        int written = 0;
        for (int index = 0; index < Categories.Length; index++)
        {
            string category = Categories[index];
            if (category == skipCategory) continue;
            if (written++ > 0) entries.Append(',');
            string id = duplicateFirstId && index == 1 ? "prologue.ui" : $"prologue.{category}";
            string path = index == 0 ? firstRuntimePath : $"res://assets/production/planned-{category}.txt";
            string license = index == 0 ? firstLicense : "Project-owned";
            bool final = index == 0 && firstFinal;
            entries.Append("{\"id\":\"").Append(id)
                .Append("\",\"category\":\"").Append(category)
                .Append("\",\"runtimePath\":\"").Append(Escape(path))
                .Append("\",\"source\":\"Knight local production\",\"license\":\"")
                .Append(Escape(license)).Append("\",\"final\":")
                .Append(final ? "true" : "false").Append('}');
        }

        return $"{{\"version\":2,\"assets\":[{entries}],\"replacementSlots\":{ReplacementSlotsJson()}}}";
    }

    private static string ReplacementSlotsJson()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "production",
            "prologue-assets.json");
        using System.Text.Json.JsonDocument document =
            System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("replacementSlots").GetRawText();
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static void ExpectInvalid(string json, string expectedFragment, string message)
    {
        try
        {
            ProductionAssetManifestParser.Parse(json);
        }
        catch (InvalidDataException exception)
        {
            TestAssert.True(exception.Message.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase), message);
            return;
        }

        throw new InvalidOperationException("Assertion failed: " + message);
    }
}
