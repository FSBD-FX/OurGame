using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Knight.Settings;

namespace Knight.Tests.Unit;

/// <summary>Real settings-store compatibility, not a fake settings serializer.</summary>
public static class ReducedFogSettingsTests
{
    public static void Run()
    {
        // Catches a missing option, a Normalize copy which drops it, a true
        // old-file default, and a save/load path which loses either toggle.
        PropertyInfo option = typeof(GameSettingsData).GetProperty("ReducedFog");
        TestAssert.True(option != null && option.PropertyType == typeof(bool) &&
            option.CanRead && option.CanWrite, "settings expose the independent reduced-fog option");
        TestAssert.True(!(bool)option.GetValue(new GameSettingsData()), "new settings default to normal fog");

        string root = NewCaseRoot();
        string path = Path.Combine(root, "settings.json");
        File.WriteAllText(path, "{\"version\":1,\"masterVolume\":0.42,\"reduceFlashes\":true,\"language\":\"English\"}");
        GameSettingsData old = GameSettingsStore.LoadOrDefault(path);
        TestAssert.True(!(bool)option.GetValue(old), "old valid files lacking reducedFog default false");
        TestAssert.True(old.ReduceFlashes && old.Language == GameLanguage.English && old.MasterVolume == .42f,
            "missing reducedFog does not reset valid existing settings");

        option.SetValue(old, true);
        old.ReduceFlashes = false;
        GameSettingsData normalized = old.Normalize();
        TestAssert.True((bool)option.GetValue(normalized) && !normalized.ReduceFlashes,
            "Normalize preserves reduced fog independently from reduced flashes");
        GameSettingsStore.SaveAtomic(path, normalized);
        GameSettingsData loaded = GameSettingsStore.LoadOrDefault(path);
        TestAssert.True((bool)option.GetValue(loaded) && !loaded.ReduceFlashes,
            "true reduced fog survives a real atomic settings round trip");
        TestAssert.Equal(.42f, loaded.MasterVolume, "adding reduced fog preserves previous volume");
        TestAssert.Equal(GameLanguage.English, loaded.Language, "adding reduced fog preserves previous language");

        option.SetValue(loaded, false);
        loaded.ReduceFlashes = true;
        GameSettingsStore.SaveAtomic(path, loaded);
        GameSettingsData toggled = GameSettingsStore.LoadOrDefault(path);
        TestAssert.True(!(bool)option.GetValue(toggled) && toggled.ReduceFlashes,
            "false reduced fog also survives and does not mirror reduced flashes");
        TestAssert.Equal(0, Directory.GetFiles(root, "*.tmp").Length, "atomic saves leave no temporary file");
        GD.Print("REDUCED_FOG_SETTINGS_STORE_OK");
    }

    private static string NewCaseRoot()
    {
        const string prefix = "--case-storage-root=";
        string[] options = OS.GetCmdlineUserArgs().Where(value => value.StartsWith(prefix,
            StringComparison.Ordinal)).ToArray();
        TestAssert.Equal(1, options.Length, "settings tests require exactly one isolated storage root");
        string supplied = options[0][prefix.Length..];
        TestAssert.True(Path.IsPathFullyQualified(supplied) && Directory.Exists(supplied),
            "settings tests require an existing absolute storage root");
        string root = Path.GetFullPath(supplied).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (string excluded in new[] { ProjectSettings.GlobalizePath("res://"), OS.GetUserDataDir() })
        {
            string forbidden = Path.GetFullPath(excluded).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            TestAssert.True(!root.Equals(forbidden, StringComparison.OrdinalIgnoreCase) &&
                !root.StartsWith(forbidden + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "settings fixture stays outside source and real application data");
        }
        for (DirectoryInfo current = new(root); current != null; current = current.Parent)
            TestAssert.True((current.Attributes & FileAttributes.ReparsePoint) == 0,
                "settings fixture rejects reparse ancestors");
        string caseRoot = Path.Combine(root, "reduced-fog-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(caseRoot);
        return caseRoot; // Retain the isolated round-trip file as evidence.
    }
}
