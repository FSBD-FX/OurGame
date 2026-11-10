using System;
using Godot;
using Knight.Localization;
using Knight.Settings;
using Knight.Audio;

namespace Knight.Ui;

public partial class PrologueUiRoot : CanvasLayer
{
    [Export] public bool BasinQualitySampleMode { get; set; }

    public event Action StartRequested;
    public event Action ContinueRequested;
    public event Action QuitRequested;
    public event Action<GameSettingsData> SettingsChanged;

    public GameLanguage Language { get; private set; } = GameLanguage.ChineseSimplified;
    public InputDeviceKind LastInputDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

    private bool _hasSave;
    private PrologueMainMenu _main;
    private PrologueSettingsMenu _settings;
    private PrologueControlsView _controls;
    private GameSettingsData _currentSettings = new();

    public override void _Ready()
    {
        PrologueUiPalette.ApplyBorderlessPanels(this);
        PrologueStrings.Load(Godot.FileAccess.GetFileAsString("res://assets/localization/prologue-ui.json"));
        _main = GetNode<PrologueMainMenu>("MainMenu");
        _main.BasinQualitySampleMode = BasinQualitySampleMode;
        _settings = GetNode<PrologueSettingsMenu>("SettingsMenu");
        _controls = GetNode<PrologueControlsView>("ControlsView");
        Bind("MainMenu/Panel/Buttons/Start", () => StartRequested?.Invoke());
        Bind("MainMenu/Panel/Buttons/Continue", () => ContinueRequested?.Invoke());
        Bind("MainMenu/Panel/Buttons/Settings", () => ShowSettings(_currentSettings));
        Bind("MainMenu/Panel/Buttons/Controls", ShowControls);
        Bind("MainMenu/Panel/Buttons/Quit", () => QuitRequested?.Invoke());
        Bind("SettingsMenu/Panel/Back", HandleBack);
        Bind("ControlsView/Panel/Back", HandleBack);
        ShowMainMenu(hasSave: false);
    }

    public void ShowMainMenu(bool hasSave)
    {
        _hasSave = hasSave;
        EnsureNodes();
        _settings.Visible = false;
        _controls.Visible = false;
        _main.BasinQualitySampleMode = BasinQualitySampleMode;
        _main.Present(hasSave, Language);
        GetNode<Button>("MainMenu/Panel/Buttons/Start").GrabFocus();
    }

    public void ShowSettings(GameSettingsData settings)
    {
        EnsureNodes();
        SetSettings(settings);
        _main.Visible = false;
        _controls.Visible = false;
        _settings.Present(_currentSettings, allowGuidance: !BasinQualitySampleMode);
        _settings.Value<HSlider>("Master").GrabFocus();
    }

    public void ShowControls()
    {
        EnsureNodes();
        _main.Visible = false;
        _settings.Visible = false;
        _controls.Present(LastInputDevice, Language);
        GetNode<Button>("ControlsView/Panel/Back").GrabFocus();
    }

    internal void UpdateContinueAvailability(bool hasSave)
    {
        _hasSave = hasSave;
        EnsureNodes();
        _main.UpdateContinueAvailability(hasSave);
    }

    public void SetLastInputDevice(InputDeviceKind device)
    {
        LastInputDevice = device;
        EnsureNodes();
        _controls.SetDevice(device);
    }

    public void SetLanguage(GameLanguage language)
    {
        Language = Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        _currentSettings.Language = Language;
    }

    public void SetSettings(GameSettingsData settings)
    {
        // Keep a normalized copy without changing the current menu or its focus.
        _currentSettings = (settings ?? new GameSettingsData()).Normalize();
        Language = _currentSettings.Language;
    }

    public void HandleBack()
    {
        if (_settings?.Visible == true)
        {
            _currentSettings = ReadSettings();
            Language = _currentSettings.Language;
            SettingsChanged?.Invoke(_currentSettings);
        }
        ShowMainMenu(_hasSave);
    }

    public GameSettingsData ReadSettings()
    {
        EnsureNodes();
        int[] widths = [1280, 1920, 2560, 3840];
        int[] heights = [720, 1080, 1440, 2160];
        int resolution = Mathf.Clamp(_settings.Value<OptionButton>("Resolution").Selected, 0, 3);
        return new GameSettingsData
        {
            MasterVolume = (float)_settings.Value<HSlider>("Master").Value,
            MusicVolume = (float)_settings.Value<HSlider>("Music").Value,
            EffectsVolume = (float)_settings.Value<HSlider>("Effects").Value,
            AmbienceVolume = (float)_settings.Value<HSlider>("Ambience").Value,
            UiVolume = (float)_settings.Value<HSlider>("UiVolume").Value,
            WindowMode = (GameWindowMode)_settings.Value<OptionButton>("WindowMode").Selected,
            ResolutionWidth = widths[resolution], ResolutionHeight = heights[resolution],
            VSync = _settings.Value<CheckButton>("VSync").ButtonPressed,
            UiScale = (float)_settings.Value<HSlider>("UiScale").Value,
            ScreenShake = (float)_settings.Value<HSlider>("ScreenShake").Value,
            ReduceFlashes = _settings.Value<CheckButton>("ReduceFlashes").ButtonPressed,
            ReducedFog = _settings.Value<CheckButton>("ReducedFog").ButtonPressed,
            Language = (GameLanguage)_settings.Value<OptionButton>("Language").Selected,
            GuidanceEnabled = !BasinQualitySampleMode && _settings.Value<CheckButton>("Guidance").ButtonPressed
        }.Normalize();
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventJoypadButton or InputEventJoypadMotion)
            SetLastInputDevice(InputDeviceKind.Gamepad);
        else if (inputEvent is InputEventKey or InputEventMouseButton or InputEventMouseMotion)
            SetLastInputDevice(InputDeviceKind.KeyboardMouse);
    }

    private void EnsureNodes()
    {
        _main ??= GetNode<PrologueMainMenu>("MainMenu");
        _main.BasinQualitySampleMode = BasinQualitySampleMode;
        _settings ??= GetNode<PrologueSettingsMenu>("SettingsMenu");
        _controls ??= GetNode<PrologueControlsView>("ControlsView");
    }

    private void Bind(NodePath path, Action action) => GetNode<Button>(path).Pressed += () =>
    {
        PrologueAudioDirector.PlayUiCueFromTree(this, PrologueAudioCue.UiConfirm);
        action();
    };
}
