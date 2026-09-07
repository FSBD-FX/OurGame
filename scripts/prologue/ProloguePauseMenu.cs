using System;
using Godot;
using Knight.Shared;
using Knight.Prologue.Guidance;
using Knight.Settings;

namespace Knight.Prologue;

public enum ProloguePauseCommand
{
    Resume,
    ShowControls,
    ReturnToCheckpoint,
    QuitToDesktop,
    ToggleGuidance
}

public partial class ProloguePauseMenu : CanvasLayer
{
    public event Action Opened;
    public event Action Closed;
    [Export] public NodePath RecoveryPath { get; set; } = new("..");
    [Export] public NodePath GuidancePath { get; set; } = new("../Guidance");

    public bool IsOpen { get; private set; }
    public string ControlsText { get; private set; } =
        "移动  A / D\n跳跃  SPACE\n攻击模式  Q + LMB\n机械爪模式  E + LMB\n护盾  RMB\n装备  1–4\n冲刺  SHIFT\n暂停  ESC\n第一关指导：暂停菜单";
    public Action QuitHandler { get; set; }

    private Control _panel;
    private Label _controls;
    private GameLanguage _language = GameLanguage.ChineseSimplified;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNodeOrNull<Control>("Panel");
        _controls = GetNodeOrNull<Label>("Panel/Controls");
        QuitHandler ??= () => GetTree().Quit();
        BindButton("Panel/Resume", ProloguePauseCommand.Resume);
        BindButton("Panel/ShowControls", ProloguePauseCommand.ShowControls);
        BindButton("Panel/ReturnToCheckpoint", ProloguePauseCommand.ReturnToCheckpoint);
        BindButton("Panel/QuitToDesktop", ProloguePauseCommand.QuitToDesktop);
        BindButton("Panel/GuidanceToggle", ProloguePauseCommand.ToggleGuidance);
        ApplyLanguage();
        ApplyVisibility();
    }

    public override void _Process(double delta)
    {
        if (!Input.IsActionJustPressed(InputActions.Pause)) return;
        PrologueGameFlowController flow = GetNodeOrNull<PrologueGameFlowController>("../GameFlow");
        if (flow != null && flow.State is not (PrologueGameFlowState.Playing or PrologueGameFlowState.Paused)) return;
        if (IsOpen) Execute(ProloguePauseCommand.Resume);
        else Open();
    }

    public void Open()
    {
        IsOpen = true;
        GetTree().Paused = true;
        ApplyVisibility();
        UpdateGuidanceButton();
        GetNodeOrNull<Button>("Panel/Resume")?.GrabFocus();
        Opened?.Invoke();
    }

    public void SetLanguage(GameLanguage language)
    {
        _language = Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        ApplyLanguage();
    }

    public void Execute(ProloguePauseCommand command)
    {
        switch (command)
        {
            case ProloguePauseCommand.Resume:
                Close();
                break;
            case ProloguePauseCommand.ShowControls:
                if (_controls != null) _controls.Text = ControlsText;
                break;
            case ProloguePauseCommand.ReturnToCheckpoint:
                Close();
                (GetNodeOrNull(RecoveryPath) as ILevelRecovery)?.RequestRespawn("RETURNED TO CHECKPOINT");
                break;
            case ProloguePauseCommand.QuitToDesktop:
                Close();
                QuitHandler?.Invoke();
                break;
            case ProloguePauseCommand.ToggleGuidance:
                PrologueGuidanceController guidance = GetNodeOrNull<PrologueGuidanceController>(GuidancePath);
                guidance?.ToggleGuidance();
                UpdateGuidanceButton();
                break;
        }
    }

    private void Close()
    {
        GetTree().Paused = false;
        IsOpen = false;
        ApplyVisibility();
        Closed?.Invoke();
    }

    private void ApplyVisibility()
    {
        if (_panel != null) _panel.Visible = IsOpen;
    }

    private void UpdateGuidanceButton()
    {
        Button button = GetNodeOrNull<Button>("Panel/GuidanceToggle");
        PrologueGuidanceController guidance = GetNodeOrNull<PrologueGuidanceController>(GuidancePath);
        if (button == null) return;
        bool enabled = guidance?.GuidanceEnabled != false;
        button.Text = _language == GameLanguage.English
            ? $"FIRST-LEVEL GUIDANCE: {(enabled ? "ON" : "OFF")}"
            : $"第一关指导：{(enabled ? "开" : "关")}";
    }

    private void ApplyLanguage()
    {
        bool english = _language == GameLanguage.English;
        ControlsText = english
            ? "MOVE  A / D\nJUMP  SPACE\nATTACK MODE  Q + LMB\nGRAPPLE MODE  E + LMB\nSHIELD  RMB\nLOADOUT  1–4\nDASH  SHIFT\nPAUSE  ESC\nFIRST-LEVEL GUIDANCE: PAUSE MENU"
            : "移动  A / D\n跳跃  SPACE\n攻击模式  Q + LMB\n机械爪模式  E + LMB\n护盾  RMB\n装备  1–4\n冲刺  SHIFT\n暂停  ESC\n第一关指导：暂停菜单";
        SetText("Panel/Title", english ? "THE NAMELESS BELFRY · PAUSED" : "无名钟楼 · 暂停");
        SetText("Panel/Subtitle", english ? "THE BELLS ARE STILL WAITING" : "钟声仍在等待");
        SetText("Panel/Resume", english ? "RESUME" : "继续");
        SetText("Panel/ShowControls", english ? "CONTROLS" : "操作说明");
        SetText("Panel/ReturnToCheckpoint", english ? "RETURN TO CHECKPOINT" : "返回墓灯");
        SetText("Panel/QuitToDesktop", english ? "QUIT" : "退出");
        if (_controls != null && !string.IsNullOrWhiteSpace(_controls.Text)) _controls.Text = ControlsText;
        UpdateGuidanceButton();
    }

    private void SetText(NodePath path, string text)
    {
        if (GetNodeOrNull(path) is Label label) label.Text = text;
        else if (GetNodeOrNull(path) is Button button) button.Text = text;
    }

    private void BindButton(NodePath path, ProloguePauseCommand command)
    {
        Button button = GetNodeOrNull<Button>(path);
        if (button != null) button.Pressed += () => Execute(command);
    }
}
