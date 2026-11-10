using System.Collections.Generic;
using Godot;
using Knight.Localization;
using Knight.Settings;

namespace Knight.Ui;

public partial class PrologueSettingsMenu : Control
{
    private Panel _panel;
    private ScrollContainer _scroll;
    private VBoxContainer _rows;
    private Button _back;

    public override void _Ready()
    {
        _panel = GetNode<Panel>("Panel");
        _scroll = _panel.GetNode<ScrollContainer>("Scroll");
        _rows = _scroll.GetNode<VBoxContainer>("Rows");
        _back = _panel.GetNode<Button>("Back");
        Resized += FitPanel;
        FitPanel();
    }

    private void UpdateFocusOrder()
    {
        var focusOrder = new List<Control> { _back };
        foreach (Node child in _rows.GetChildren())
            if (child is Control { Visible: true } row) focusOrder.Add(row.GetNode<Control>("Value"));
        for (int index = 0; index < focusOrder.Count; index++)
        {
            Control current = focusOrder[index];
            NodePath previous = current.GetPathTo(focusOrder[(index + focusOrder.Count - 1) % focusOrder.Count]);
            NodePath next = current.GetPathTo(focusOrder[(index + 1) % focusOrder.Count]);
            current.FocusPrevious = current.FocusNeighborTop = previous;
            current.FocusNext = current.FocusNeighborBottom = next;
        }
    }

    internal T Value<T>(string row) where T : Control => _rows.GetNode<T>($"{row}/Value");

    private void FitPanel()
    {
        Vector2 available = GetViewportRect().Size;
        float halfWidth = Mathf.Clamp((available.X - 32f) * .5f, 1f, 410f);
        float halfHeight = Mathf.Clamp((available.Y - 32f) * .5f, 1f, 332f);
        _panel.OffsetLeft = -halfWidth; _panel.OffsetRight = halfWidth;
        _panel.OffsetTop = -halfHeight; _panel.OffsetBottom = halfHeight;
    }

    public void Present(GameSettingsData settings, bool allowGuidance)
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
        SetLabel("ReducedFog", "settings.reduced_fog", language);
        SetLabel("Guidance", "settings.guidance", language);
        SetLabel("Language", "settings.language", language);
        _back.Text = PrologueStrings.Get("settings.back", language);
        Value<HSlider>("UiScale").Value = settings.UiScale;
        Value<HSlider>("ScreenShake").Value = settings.ScreenShake;
        Value<CheckButton>("VSync").ButtonPressed = settings.VSync;
        Value<CheckButton>("ReduceFlashes").ButtonPressed = settings.ReduceFlashes;
        Value<CheckButton>("ReducedFog").ButtonPressed = settings.ReducedFog;
        CheckButton guidance = Value<CheckButton>("Guidance");
        guidance.ButtonPressed = allowGuidance && settings.GuidanceEnabled;
        guidance.Disabled = !allowGuidance;
        guidance.FocusMode = allowGuidance ? FocusModeEnum.All : FocusModeEnum.None;
        _rows.GetNode<Control>("Guidance").Visible = allowGuidance;
        UpdateFocusOrder();
        Value<OptionButton>("WindowMode").Selected = (int)settings.WindowMode;
        Value<OptionButton>("Resolution").Selected = settings.ResolutionWidth switch
        {
            >= 3840 => 3, >= 2560 => 2, >= 1920 => 1, _ => 0
        };
        Value<OptionButton>("Language").Selected = (int)settings.Language;
        _scroll.ScrollVertical = 0;
    }

    private void SetRow(string row, string key, float value, GameLanguage language)
    {
        SetLabel(row, key, language);
        Value<HSlider>(row).Value = value;
    }

    private void SetLabel(string row, string key, GameLanguage language) =>
        _rows.GetNode<Label>($"{row}/Label").Text = PrologueStrings.Get(key, language);
}
