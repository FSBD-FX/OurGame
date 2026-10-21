using System;

namespace Knight.Settings;

public enum GameLanguage
{
    ChineseSimplified,
    English
}

public enum GameWindowMode
{
    Windowed,
    Borderless,
    Fullscreen
}

public sealed class GameSettingsData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public float MasterVolume { get; set; } = 0.8f;
    public float MusicVolume { get; set; } = 0.75f;
    public float EffectsVolume { get; set; } = 0.85f;
    public float AmbienceVolume { get; set; } = 0.75f;
    public float UiVolume { get; set; } = 0.85f;
    public GameWindowMode WindowMode { get; set; } = GameWindowMode.Windowed;
    public int ResolutionWidth { get; set; } = 1280;
    public int ResolutionHeight { get; set; } = 720;
    public bool VSync { get; set; } = true;
    public float UiScale { get; set; } = 1f;
    public float ScreenShake { get; set; } = 1f;
    public bool ReduceFlashes { get; set; }
    public bool ReducedFog { get; set; }
    public GameLanguage Language { get; set; } = GameLanguage.ChineseSimplified;
    public bool GuidanceEnabled { get; set; } = true;

    public GameSettingsData Normalize()
    {
        bool supportedResolution = ResolutionWidth >= 1280 && ResolutionHeight >= 720;
        return new GameSettingsData
        {
            Version = CurrentVersion,
            MasterVolume = Math.Clamp(MasterVolume, 0f, 1f),
            MusicVolume = Math.Clamp(MusicVolume, 0f, 1f),
            EffectsVolume = Math.Clamp(EffectsVolume, 0f, 1f),
            AmbienceVolume = Math.Clamp(AmbienceVolume, 0f, 1f),
            UiVolume = Math.Clamp(UiVolume, 0f, 1f),
            WindowMode = Enum.IsDefined(WindowMode) ? WindowMode : GameWindowMode.Windowed,
            ResolutionWidth = supportedResolution ? ResolutionWidth : 1280,
            ResolutionHeight = supportedResolution ? ResolutionHeight : 720,
            VSync = VSync,
            UiScale = Math.Clamp(UiScale, 0.8f, 1.5f),
            ScreenShake = Math.Clamp(ScreenShake, 0f, 1f),
            ReduceFlashes = ReduceFlashes,
            ReducedFog = ReducedFog,
            Language = Enum.IsDefined(Language) ? Language : GameLanguage.ChineseSimplified,
            GuidanceEnabled = GuidanceEnabled
        };
    }
}
