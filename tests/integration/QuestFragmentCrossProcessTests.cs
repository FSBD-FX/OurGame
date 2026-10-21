using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Knight.Save;

namespace Knight.Tests.Integration;

public static class QuestFragmentCrossProcessTests
{
    private static string Root()
    {
        const string prefix = "--case-storage-root=";
        string[] matches = OS.GetCmdlineUserArgs().Where(arg => arg.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || !Path.IsPathFullyQualified(matches[0][prefix.Length..]))
            throw new InvalidOperationException("Cross-process witness requires one absolute external case root.");
        string root = Path.GetFullPath(matches[0][prefix.Length..]).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (string protectedRoot in new[] { ProjectSettings.GlobalizePath("res://"), OS.GetUserDataDir() })
        {
            string denied = Path.GetFullPath(protectedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (root.Equals(denied, StringComparison.OrdinalIgnoreCase) ||
                root.StartsWith(denied + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cross-process witness must stay outside project and user data.");
        }
        for (DirectoryInfo directory = new(root); directory != null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Cross-process case cannot traverse reparse points.");
        if (!Directory.Exists(root)) throw new InvalidOperationException("Cross-process case must already exist.");
        return root;
    }

    public static void PreserveFlowWrittenSlot(string source, string witness)
    {
        if (witness is not ("collected" or "legacy" or "contact")) throw new ArgumentOutOfRangeException(nameof(witness));
        string root = Root();
        string sourcePath = Path.GetFullPath(source);
        if (!sourcePath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            (File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Witness source must be a regular file within this isolated case.");
        string path = Path.Combine(root, $"fragment-cross-process-{witness}.json");
        File.Copy(source, path, overwrite: false);
        File.WriteAllText(path + ".sha256", Hash(source));
        TestAssert.Equal(Hash(source), Hash(path), "retained cross-process witness is a byte-exact actual flow-written slot");
    }

    public static void AssertCurrentWire(string path, bool legacy, string[] expectedIds = null)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        string[] expected = expectedIds ?? (legacy ? [] : ["canyon.v2.fragment.entrance", "canyon.v2.fragment.rift"]);
        TestAssert.True(root.TryGetProperty("version", out var version) &&
            version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out int schema) &&
            schema == PrologueSaveData.CurrentVersion &&
            root.TryGetProperty("layoutVersion", out var layout) &&
            layout.ValueKind == JsonValueKind.Number && layout.TryGetInt32(out int layoutVersion) && layoutVersion == 2 &&
            root.TryGetProperty("collectionRuleVersion", out var rule) &&
            rule.ValueKind == JsonValueKind.Number && rule.TryGetInt32(out int ruleVersion) && ruleVersion == 1 &&
            root.TryGetProperty("legacyCollectionQuotaExempt", out var exempt) &&
            exempt.ValueKind == (legacy ? JsonValueKind.True : JsonValueKind.False) &&
            root.TryGetProperty("collectedFragmentIds", out var ids) && ids.ValueKind == JsonValueKind.Array &&
            ids.GetArrayLength() == expected.Length &&
            ids.EnumerateArray().All(id => id.ValueKind == JsonValueKind.String) &&
            ids.EnumerateArray().Select(id => id.GetString()).Order(StringComparer.Ordinal)
                .SequenceEqual(expected.Order(StringComparer.Ordinal)),
            "actual persisted raw JSON is current schema with explicit, honest collection wire fields before migration");
    }

    public static void ReadOnly()
    {
        string root = Root();
        TestAssert.True(OS.GetCmdlineUserArgs().Length == 2 && OS.GetCmdlineUserArgs().Contains("--fragment-read-only"),
            "second process accepts only explicit read-only and external-root arguments");
        foreach (string witness in new[] { "collected", "legacy", "contact" })
        {
            string path = Path.Combine(root, $"fragment-cross-process-{witness}.json");
            TestAssert.True(File.Exists(path) && File.Exists(path + ".sha256") &&
                (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0,
                "second process reads an existing regular retained witness");
            string before = Hash(path);
            TestAssert.Equal(File.ReadAllText(path + ".sha256"), before, "new process sees exact first-process saved bytes");
            bool legacy = witness == "legacy";
            string[] expected = witness == "contact" ? ["canyon.v2.fragment.gear-gallery"] :
                legacy ? [] : ["canyon.v2.fragment.entrance", "canyon.v2.fragment.rift"];
            AssertCurrentWire(path, legacy, expected);
            var loaded = PrologueSaveStore.LoadOrNew(path, 2);
            TestAssert.True(!loaded.WasRecovered && loaded.Version == PrologueSaveData.CurrentVersion && loaded.LayoutVersion == 2 &&
                loaded.CollectionRuleVersion == 1 && loaded.LegacyCollectionQuotaExempt == legacy &&
                loaded.CollectedFragmentIds.Count == expected.Length &&
                loaded.CollectedFragmentIds.Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal)),
                "separate Godot process recovers honest fragment IDs and legacy entitlement from actual persisted bytes");
            TestAssert.Equal(before, Hash(path), "cross-process read does not rewrite witness");
        }
        GD.Print("QUEST_FRAGMENT_CROSS_PROCESS_OK processes=2 witness_unchanged=True controlled_metadata_fixture=true controlled_contact_fixture=true normal_route=false");
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
