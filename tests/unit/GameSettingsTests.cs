using System;
using System.IO;
using Knight.Settings;

namespace Knight.Tests.Unit;

public static class GameSettingsTests
{
    public static void Run()
    {
        MissingAndCorruptFilesUseSafeDefaults();
        ValuesAreClampedToSupportedRanges();
        SettingsRoundTripAtomically();
    }

    private static void MissingAndCorruptFilesUseSafeDefaults()
    {
        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "settings.json");
            GameSettingsData missing = GameSettingsStore.LoadOrDefault(path);
            TestAssert.Equal(0.8f, missing.MasterVolume, "missing settings use safe master volume");
            File.WriteAllText(path, "{broken");
            GameSettingsData corrupt = GameSettingsStore.LoadOrDefault(path);
            TestAssert.Equal(GameLanguage.ChineseSimplified, corrupt.Language,
                "corrupt settings restore the default language");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void ValuesAreClampedToSupportedRanges()
    {
        var raw = new GameSettingsData
        {
            MasterVolume = 2f,
            MusicVolume = -1f,
            EffectsVolume = 1.5f,
            AmbienceVolume = -0.5f,
            UiVolume = 9f,
            UiScale = 4f,
            ScreenShake = -2f,
            ResolutionWidth = 320,
            ResolutionHeight = 200
        };
        GameSettingsData normalized = raw.Normalize();
        TestAssert.Equal(1f, normalized.MasterVolume, "master volume clamps high");
        TestAssert.Equal(0f, normalized.MusicVolume, "music volume clamps low");
        TestAssert.Equal(1f, normalized.EffectsVolume, "effects volume clamps high");
        TestAssert.Equal(0f, normalized.AmbienceVolume, "ambience volume clamps low");
        TestAssert.Equal(1f, normalized.UiVolume, "UI volume clamps high");
        TestAssert.Equal(1.5f, normalized.UiScale, "UI scale clamps high");
        TestAssert.Equal(0f, normalized.ScreenShake, "screen shake clamps low");
        TestAssert.Equal(1280, normalized.ResolutionWidth, "unsupported narrow resolution returns baseline");
        TestAssert.Equal(720, normalized.ResolutionHeight, "unsupported short resolution returns baseline");
    }

    private static void SettingsRoundTripAtomically()
    {
        string root = TempRoot();
        try
        {
            string path = Path.Combine(root, "nested", "settings.json");
            var expected = new GameSettingsData
            {
                MasterVolume = 0.42f,
                MusicVolume = 0.31f,
                UiScale = 1.25f,
                ReduceFlashes = true,
                VSync = false,
                Language = GameLanguage.English,
                WindowMode = GameWindowMode.Borderless
            };
            GameSettingsStore.SaveAtomic(path, expected);
            GameSettingsData actual = GameSettingsStore.LoadOrDefault(path);
            TestAssert.Equal(0.42f, actual.MasterVolume, "master volume round trips");
            TestAssert.Equal(0.31f, actual.MusicVolume, "music volume round trips");
            TestAssert.Equal(1.25f, actual.UiScale, "UI scale round trips");
            TestAssert.True(actual.ReduceFlashes, "reduced flash setting round trips");
            TestAssert.True(!actual.VSync, "VSync setting round trips");
            TestAssert.Equal(GameLanguage.English, actual.Language, "language round trips");
            TestAssert.Equal(GameWindowMode.Borderless, actual.WindowMode, "window mode round trips");
            TestAssert.Equal(0, Directory.GetFiles(Path.GetDirectoryName(path), "*.tmp").Length,
                "atomic settings save leaves no temporary files");
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static string TempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "knight-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
