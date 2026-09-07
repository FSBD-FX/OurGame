using System;
using System.IO;
using Godot;
using Knight.Localization;
using Knight.Settings;
using Knight.Audio;

namespace Knight.Ui;

public partial class PrologueUiRoot : CanvasLayer
{
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
        string stringsPath = ProjectSettings.GlobalizePath("res://assets/localization/prologue-ui.json");
        PrologueStrings.Load(File.ReadAllText(stringsPath));
        _main = GetNode<PrologueMainMenu>("MainMenu");
        _settings = GetNode<PrologueSettingsMenu>("SettingsMenu");
        _controls = GetNode<PrologueControlsView>("ControlsView");
        Bind("MainMenu/Panel/Buttons/Start", () => StartRequested?.Invoke());
        Bind("MainMenu/Panel/Buttons/Continue", () => ContinueRequested?.Invoke());
        Bind("MainMenu/Panel/Buttons/Settings", () => ShowSettings(new GameSettingsData { Language = Language }));
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
        _main.Present(hasSave, Language);
        GetNode<Button>("MainMenu/Panel/Buttons/Start").GrabFocus();
    }

    public void ShowSettings(GameSettingsData settings)
    {
        EnsureNodes();
        _currentSettings = settings.Normalize();
        Language = _currentSettings.Language;
        _main.Visible = false;
        _controls.Visible = false;
        _settings.Present(_currentSettings);
        GetNode<HSlider>("SettingsMenu/Panel/Rows/Master/Value").GrabFocus();
    }

    public void ShowControls()
    {
        EnsureNodes();
        _main.Visible = false;
        _settings.Visible = false;
        _controls.Present(LastInputDevice, Language);
        GetNode<Button>("ControlsView/Panel/Back").GrabFocus();
    }

    public void SetLastInputDevice(InputDeviceKind device)
    {
        LastInputDevice = device;
        EnsureNodes();
        _controls.SetDevice(device);
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
        int resolution = Mathf.Clamp(GetNode<OptionButton>("SettingsMenu/Panel/Rows/Resolution/Value").Selected, 0, 3);
        return new GameSettingsData
        {
            MasterVolume = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/Master/Value").Value,
            MusicVolume = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/Music/Value").Value,
            EffectsVolume = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/Effects/Value").Value,
            AmbienceVolume = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/Ambience/Value").Value,
            UiVolume = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/UiVolume/Value").Value,
            WindowMode = (GameWindowMode)GetNode<OptionButton>("SettingsMenu/Panel/Rows/WindowMode/Value").Selected,
            ResolutionWidth = widths[resolution], ResolutionHeight = heights[resolution],
            VSync = GetNode<CheckButton>("SettingsMenu/Panel/Rows/VSync/Value").ButtonPressed,
            UiScale = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/UiScale/Value").Value,
            ScreenShake = (float)GetNode<HSlider>("SettingsMenu/Panel/Rows/ScreenShake/Value").Value,
            ReduceFlashes = GetNode<CheckButton>("SettingsMenu/Panel/Rows/ReduceFlashes/Value").ButtonPressed,
            Language = (GameLanguage)GetNode<OptionButton>("SettingsMenu/Panel/Rows/Language/Value").Selected,
            GuidanceEnabled = _currentSettings.GuidanceEnabled
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
        _settings ??= GetNode<PrologueSettingsMenu>("SettingsMenu");
        _controls ??= GetNode<PrologueControlsView>("ControlsView");
    }

    private void Bind(NodePath path, Action action) => GetNode<Button>(path).Pressed += () =>
    {
        PrologueAudioDirector.PlayUiCueFromTree(this, PrologueAudioCue.UiConfirm);
        action();
    };
}
