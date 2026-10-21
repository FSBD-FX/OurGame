using System;
using Godot;
using Knight.Shared;
using Knight.Prologue.Guidance;
using Knight.Settings;
using Knight.Ui;

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
    private const string ChineseControls = "移动 A/D · WASD 八向瞄准 · SPACE 跳跃\nSHIFT 短喷 · A/D 反向键制动\nJ 攻击 · K 发爪/再次取消 · 命中自动牵引\n抵达后 A/D 摆荡；SPACE 跃出；J/L 取消抓取\n按住 L 举盾（耗能/过热）· 松开散热\nQ 近距站稳交互 · 装备 1–4 · ESC 暂停\n冰面、封闭面、超出射程不可抓";
    public string ControlsText { get; private set; } = ChineseControls;
    public Action QuitHandler { get; set; }

    private Control _panel;
    private Label _controls;
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private bool _quitBusy;
    private bool _quitCommitted;
    private ulong _cancelFrame;
    public bool QuitFailureVisible { get; private set; }

    public override void _Ready()
    {
        PrologueUiPalette.ApplyBorderlessPanels(this);
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNodeOrNull<Control>("Panel");
        _controls = GetNodeOrNull<Label>("Panel/Controls");
        QuitHandler ??= () => GetTree().Quit();
        BindButton("Panel/Resume", ProloguePauseCommand.Resume);
        BindButton("Panel/ShowControls", ProloguePauseCommand.ShowControls);
        BindButton("Panel/ReturnToCheckpoint", ProloguePauseCommand.ReturnToCheckpoint);
        BindButton("Panel/QuitToDesktop", ProloguePauseCommand.QuitToDesktop);
        BindButton("Panel/GuidanceToggle", ProloguePauseCommand.ToggleGuidance);
        GetNodeOrNull<Button>("Panel/QuitFailure/Retry")?.Connect(BaseButton.SignalName.Pressed, Callable.From(RequestQuit));
        GetNodeOrNull<Button>("Panel/QuitFailure/ExitWithoutSaving")?.Connect(BaseButton.SignalName.Pressed, Callable.From(ExitWithoutSaving));
        GetNodeOrNull<Button>("Panel/QuitFailure/Cancel")?.Connect(BaseButton.SignalName.Pressed, Callable.From(CancelQuit));
        ApplyLanguage();
        ApplyVisibility();
    }

    public override void _Process(double delta)
    {
        if (!Input.IsActionJustPressed(InputActions.Pause)) return;
        if (_quitCommitted || _quitBusy) return;
        if (QuitFailureVisible) { CancelQuit(); return; }
        if (_cancelFrame == Engine.GetProcessFrames()) return;
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
        if (_quitCommitted || _quitBusy || QuitFailureVisible) return;
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
                RequestQuit();
                break;
            case ProloguePauseCommand.ToggleGuidance:
                PrologueGuidanceController guidance = GetNodeOrNull<PrologueGuidanceController>(GuidancePath);
                guidance?.ToggleGuidance();
                UpdateGuidanceButton();
                break;
        }
    }

    private void RequestQuit()
    {
        if (_quitBusy || _quitCommitted) return;
        _quitBusy = true;
        PrologueGameFlowController flow = GetNodeOrNull<PrologueGameFlowController>("../GameFlow");
        if (flow == null || flow.TrySaveBeforeQuit(out _))
        {
            // Set before invoking: an injected handler may return or reenter.
            _quitCommitted = true;
            QuitHandler?.Invoke();
            return;
        }
        _quitBusy = false;
        QuitFailureVisible = true;
        GetTree().Paused = true;
        IsOpen = true;
        ApplyVisibility();
        GetNodeOrNull<Button>("Panel/QuitFailure/Retry")?.GrabFocus();
    }

    private void ExitWithoutSaving()
    {
        if (!QuitFailureVisible || _quitBusy || _quitCommitted) return;
        _quitCommitted = true;
        QuitHandler?.Invoke();
    }

    private void CancelQuit()
    {
        if (!QuitFailureVisible || _quitBusy || _quitCommitted) return;
        QuitFailureVisible = false;
        _cancelFrame = Engine.GetProcessFrames();
        ApplyVisibility();
        GetNodeOrNull<Button>("Panel/QuitToDesktop")?.GrabFocus();
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
        if (GetNodeOrNull<Control>("Panel/QuitFailure") is { } dialog) dialog.Visible = QuitFailureVisible;
        foreach (string name in new[] { "Resume", "ShowControls", "ReturnToCheckpoint", "QuitToDesktop", "GuidanceToggle" })
            if (GetNodeOrNull<Button>("Panel/" + name) is { } button) button.Disabled = QuitFailureVisible;
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
            ? "MOVE A/D · WASD AIM · SPACE JUMP\nSHIFT SHORT JET · OPPOSITE A/D TO BRAKE\nJ ATTACK · K CAST / CANCEL · AUTO-PULL\nA/D SWING AFTER ARRIVAL; SPACE LEAP; J/L CANCEL CLAW\nHOLD L SHIELD (ENERGY / HEAT); RELEASE TO COOL\nQ NEARBY GROUNDED INTERACT · 1–4 SLOTS · ESC PAUSE\nCLAW: ICE / SEALED / OUT OF RANGE FAIL"
            : ChineseControls;
        SetText("Panel/Title", english ? "THE NAMELESS BELFRY · PAUSED" : "无名钟楼 · 暂停");
        SetText("Panel/Subtitle", english ? "THE BELLS ARE STILL WAITING" : "钟声仍在等待");
        SetText("Panel/Resume", english ? "RESUME" : "继续");
        SetText("Panel/ShowControls", english ? "CONTROLS" : "操作说明");
        SetText("Panel/ReturnToCheckpoint", english ? "RETURN TO CHECKPOINT" : "返回墓灯");
        SetText("Panel/QuitToDesktop", english ? "QUIT" : "退出");
        SetText("Panel/QuitFailure/Message", english
            ? "SAVE FAILED\nExiting without saving loses unsaved exploration\nsince the last successful save."
            : "保存失败\n不保存退出将丢失自上次成功保存以来\n尚未保存的探索进度。");
        SetText("Panel/QuitFailure/Retry", english ? "RETRY" : "重试保存");
        SetText("Panel/QuitFailure/ExitWithoutSaving", english ? "EXIT WITHOUT SAVING" : "不保存退出");
        SetText("Panel/QuitFailure/Cancel", english ? "CANCEL" : "取消");
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
