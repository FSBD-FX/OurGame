using Godot;
using Knight.Localization;
using Knight.Settings;

namespace Knight.Ui;

public partial class PrologueSettingsMenu : Control
{
    public void Present(GameSettingsData settings)
    {
        GameLanguage language = settings.Language;
        Visible = true;
        GetNode<Label>("Panel/Title").Text = PrologueStrings.Get("settings.title", language);
        SetRow("Master", "settings.master", settings.MasterVolume, language);
        SetRow("Music", "settings.music", settings.MusicVolume, language);
        SetRow("Effects", "settings.effects", settings.EffectsVolume, language);
        SetRow("Ambience", "settings.ambience", settings.AmbienceVolume, language);
        SetRow("UiVolume", "settings.ui_volume", settings.UiVolume, language);
        SetLabel("WindowMode", "settings.window_mode", language);
        SetLabel("Resolution", "settings.resolution", language);
        SetLabel("VSync", "settings.vsync", language);
        SetLabel("UiScale", "settings.ui_scale", language);
        SetLabel("ScreenShake", "settings.screen_shake", language);
        SetLabel("ReduceFlashes", "settings.reduce_flashes", language);
        SetLabel("Language", "settings.language", language);
        GetNode<Button>("Panel/Back").Text = PrologueStrings.Get("settings.back", language);
        GetNode<HSlider>("Panel/Rows/UiScale/Value").Value = settings.UiScale;
        GetNode<HSlider>("Panel/Rows/ScreenShake/Value").Value = settings.ScreenShake;
        GetNode<CheckButton>("Panel/Rows/VSync/Value").ButtonPressed = settings.VSync;
        GetNode<CheckButton>("Panel/Rows/ReduceFlashes/Value").ButtonPressed = settings.ReduceFlashes;
        GetNode<OptionButton>("Panel/Rows/WindowMode/Value").Selected = (int)settings.WindowMode;
        GetNode<OptionButton>("Panel/Rows/Resolution/Value").Selected = settings.ResolutionWidth switch
        {
            >= 3840 => 3, >= 2560 => 2, >= 1920 => 1, _ => 0
        };
        GetNode<OptionButton>("Panel/Rows/Language/Value").Selected = (int)settings.Language;
    }

    private void SetRow(string row, string key, float value, GameLanguage language)
    {
        SetLabel(row, key, language);
        GetNode<HSlider>($"Panel/Rows/{row}/Value").Value = value;
    }

    private void SetLabel(string row, string key, GameLanguage language) =>
        GetNode<Label>($"Panel/Rows/{row}/Label").Text = PrologueStrings.Get(key, language);
}
