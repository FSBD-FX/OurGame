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
    public string ControlsText => BuildControlsText();
    public Action QuitHandler { get; set; }

    private Control _panel;
    private Label _controls;
    private Viewport _layoutViewport;
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private bool _quitBusy;
    private bool _quitCommitted;
    private ulong _cancelFrame;
    public bool QuitFailureVisible { get; private set; }

    public override void _EnterTree()
    {
        _layoutViewport = GetViewport();
        _layoutViewport.SizeChanged += FitLayout;
        FitLayout();
    }

    public override void _Ready()
    {
        PrologueUiPalette.ApplyBorderlessPanels(this);
        ProcessMode = ProcessModeEnum.Always;
        _panel = GetNodeOrNull<Control>("Panel");
        _controls = GetNodeOrNull<Label>("Panel/Controls");
        QuitHandler ??= RequestApplicationQuit;
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

    public override void _ExitTree()
    {
        if (IsInstanceValid(_layoutViewport)) _layoutViewport.SizeChanged -= FitLayout;
        _layoutViewport = null;
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
        if (!AllowsGameplayCommand()) return;
        IsOpen = true;
        GetTree().Paused = true;
        ApplyVisibility();
        RefreshShownControls();
        UpdateGuidanceButton();
        FitLayout();
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
        if (!AllowsGameplayCommand()) return;
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

    private bool AllowsGameplayCommand()
    {
        var flow = GetNodeOrNull<PrologueGameFlowController>("../GameFlow");
        return flow == null || !flow.IsApplicationQuitting &&
            flow.State is (PrologueGameFlowState.Playing or PrologueGameFlowState.Paused);
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

    private void RequestApplicationQuit()
    {
        var flow = GetNodeOrNull<PrologueGameFlowController>("../GameFlow");
        if (flow != null) flow.RequestApplicationQuit();
        else GetTree().Quit();
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
        RefreshShownControls();
        UpdateGuidanceButton();
        FitLayout();
    }

    private void FitLayout()
    {
        if (_panel == null || _controls == null || !IsInstanceValid(_layoutViewport) ||
            _panel.GetNodeOrNull<Label>("Title") is not { } title ||
            _panel.GetNodeOrNull<Label>("Subtitle") is not { } subtitle ||
            _panel.GetNodeOrNull<Control>("ControlsWell") is not { } well ||
            _panel.GetNodeOrNull<Button>("Resume") is not { } resume ||
            _panel.GetNodeOrNull<Button>("ShowControls") is not { } show ||
            _panel.GetNodeOrNull<Button>("ReturnToCheckpoint") is not { } checkpoint ||
            _panel.GetNodeOrNull<Button>("QuitToDesktop") is not { } quit ||
            _panel.GetNodeOrNull<Button>("GuidanceToggle") is not { } guidance) return;

        Vector2 available = _layoutViewport.GetVisibleRect().Size;
        float width = Mathf.Min(720f, available.X);
        float height = Mathf.Clamp(available.Y - 80f, Mathf.Min(400f, available.Y), 480f);
        bool compact = height < 480f;
        _panel.OffsetLeft = -width * .5f;
        _panel.OffsetRight = width * .5f;
        _panel.OffsetTop = -height * .5f;
        _panel.OffsetBottom = height * .5f;

        const float inset = 36f, gap = 10f;
        float bodyWidth = Mathf.Max(1f, width - inset * 2f);
        float titleY = compact ? 8f : 24f;
        float titleHeight = compact ? title.GetCombinedMinimumSize().Y : 44f;
        float subtitleY = compact ? titleY + titleHeight + 2f : 70f;
        float subtitleHeight = compact ? subtitle.GetCombinedMinimumSize().Y : 28f;
        Place(title, inset, titleY, bodyWidth, titleHeight);
        Place(subtitle, inset, subtitleY, bodyWidth, subtitleHeight);
        float firstY = compact ? subtitleY + subtitleHeight + 4f : 112f;
        float rowEnd = LayoutButtonRow([resume, show, checkpoint], inset, firstY, bodyWidth);
        float rowGap = compact ? 6f : gap;
        float secondY = rowEnd + rowGap;
        Button auxiliary = _panel.GetNodeOrNull<Button>("ReturnToTitle") ?? guidance;
        Vector2 auxMin = auxiliary.GetCombinedMinimumSize(), quitMin = quit.GetCombinedMinimumSize();
        float auxWidth = Mathf.Max(190f, auxMin.X), quitWidth = Mathf.Max(148f, quitMin.X);
        float secondHeight = Mathf.Max(48f, Mathf.Max(auxMin.Y, quitMin.Y));
        Place(auxiliary, inset, secondY, auxWidth, secondHeight);
        Place(quit, width - inset - quitWidth, secondY, quitWidth, secondHeight);
        LinkFocus([resume, show, checkpoint, auxiliary, quit]);

        float wellY = secondY + secondHeight + rowGap;
        float wellHeight = Mathf.Max(1f, Mathf.Min(430f, height - 12f) - wellY);
        float textInset = compact ? 8f : 10f;
        Place(well, inset, wellY, bodyWidth, wellHeight);
        Place(_controls, inset + 20f, wellY + textInset, Mathf.Max(1f, bodyWidth - 40f),
            Mathf.Max(1f, wellHeight - textInset * 2f));

        if (_panel.GetNodeOrNull<Control>("QuitFailure") is not { } dialog) return;
        float dialogY = compact ? firstY - 4f : 96f;
        float dialogWidth = Mathf.Max(1f, width - 48f);
        Place(dialog, 24f, dialogY, dialogWidth, Mathf.Max(1f, Mathf.Min(446f, height - 18f) - dialogY));
        if (dialog.GetNodeOrNull<Label>("Message") is { } message)
            Place(message, 24f, 32f, Mathf.Max(1f, dialogWidth - 48f), 150f);
        if (dialog.GetNodeOrNull<Button>("Retry") is { } retry &&
            dialog.GetNodeOrNull<Button>("ExitWithoutSaving") is { } exit &&
            dialog.GetNodeOrNull<Button>("Cancel") is { } cancel)
            LayoutButtonRow([retry, exit, cancel], 24f, 220f, Mathf.Max(1f, dialogWidth - 48f));
    }

    private static float LayoutButtonRow(Button[] buttons, float x, float y, float width)
    {
        const float gap = 10f;
        float available = width - gap * (buttons.Length - 1);
        float height = 48f;
        foreach (Button button in buttons)
        {
            Vector2 minimum = button.GetCombinedMinimumSize();
            available -= minimum.X;
            height = Mathf.Max(height, minimum.Y);
        }
        float extra = Mathf.Max(0f, available) / buttons.Length;
        foreach (Button button in buttons)
        {
            float buttonWidth = button.GetCombinedMinimumSize().X + extra;
            Place(button, x, y, buttonWidth, height);
            x += buttonWidth + gap;
        }
        return y + height;
    }

    private static void Place(Control control, float x, float y, float width, float height)
    {
        control.Position = new Vector2(x, y);
        control.Size = new Vector2(width, height);
    }

    private static void LinkFocus(Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            button.FocusPrevious = button.GetPathTo(buttons[(i + buttons.Length - 1) % buttons.Length]);
            button.FocusNext = button.GetPathTo(buttons[(i + 1) % buttons.Length]);
        }
    }

    private void RefreshShownControls()
    {
        if (_controls != null && !string.IsNullOrWhiteSpace(_controls.Text)) _controls.Text = ControlsText;
    }

    private string BuildControlsText()
    {
        string KeyText(StringName action) => InputGlyphResolver.LiveKeyboardText(action, _language);
        string left = KeyText(InputActions.MoveLeft), right = KeyText(InputActions.MoveRight);
        string horizontal = $"{left}/{right}";
        string aim = InputGlyphResolver.KeyboardAimText(KeyText(InputActions.MoveUp), left,
            KeyText(InputActions.MoveDown), right);
        string jump = KeyText(InputActions.Jump), jet = KeyText(InputActions.Dash);
        string attack = KeyText(InputActions.Attack), claw = KeyText(InputActions.Claw);
        string shield = KeyText(InputActions.KeyboardShield), interact = KeyText(InputActions.Interact);
        string pause = KeyText(InputActions.Pause);
        string slots = $"{KeyText(InputActions.Slot1)}/{KeyText(InputActions.Slot2)}/{KeyText(InputActions.Slot3)}/{KeyText(InputActions.Slot4)}";
        if (slots == "1/2/3/4") slots = "1–4";
        return _language == GameLanguage.English
            ? $"MOVE {horizontal} · {aim} AIM · {jump} JUMP\n{jet} SHORT JET · OPPOSITE {horizontal} TO BRAKE\n" +
              $"{attack} ATTACK · {claw} CAST / CANCEL · AUTO-PULL\n{horizontal} SWING AFTER ARRIVAL; {jump} LEAP; {attack}/{shield} CANCEL CLAW\n" +
              $"HOLD {shield} SHIELD (ENERGY / HEAT); RELEASE TO COOL\n{interact} NEARBY GROUNDED INTERACT · {slots} SLOTS · {pause} PAUSE\n" +
              "CLAW: ICE / SEALED / OUT OF RANGE FAIL"
            : $"移动 {horizontal} · {aim} 八向瞄准 · {jump} 跳跃\n{jet} 短喷 · {horizontal} 反向键制动\n" +
              $"{attack} 攻击 · {claw} 发爪/再次取消 · 命中自动牵引\n抵达后 {horizontal} 摆荡；{jump} 跃出；{attack}/{shield} 取消抓取\n" +
              $"按住 {shield} 举盾（耗能/过热）· 松开散热\n{interact} 近距站稳交互 · 装备 {slots} · {pause} 暂停\n" +
              "冰面、封闭面、超出射程不可抓";
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
