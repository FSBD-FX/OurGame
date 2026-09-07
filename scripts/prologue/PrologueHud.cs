using Godot;
using Knight.Ui;

namespace Knight.Prologue;

public partial class PrologueHud : CanvasLayer
{
    private Label _health;
    private Label _thread;
    private Label _objective;
    private Label _prompt;
    private Control _boss;
    private Label _bossName;
    private ProgressBar _bossHealth;
    private Label _bossState;
    private PrologueFormalHud _formal;
    private PrologueHudViewModel _model =
        PrologueHudViewModel.Gameplay(5, 5, 1f, string.Empty, string.Empty, string.Empty);

    public override void _Ready() => BindControls();

    public void BindControls()
    {
        _health = GetNodeOrNull<Label>("Health");
        _thread = GetNodeOrNull<Label>("Thread");
        _objective = GetNodeOrNull<Label>("Objective");
        _prompt = GetNodeOrNull<Label>("Prompt");
        _boss = GetNodeOrNull<Control>("Boss");
        _bossName = GetNodeOrNull<Label>("Boss/Name");
        _bossHealth = GetNodeOrNull<ProgressBar>("Boss/Health");
        _bossState = GetNodeOrNull<Label>("Boss/State");
        _formal = GetNodeOrNull<PrologueFormalHud>("FormalHud");
        _formal?.Present(_model);
    }

    public void SetHealth(int current, int max)
    {
        if (_health != null) _health.Text = $"HEALTH {current}/{max}";
        _model = _model.WithHealth(current, max);
        _formal?.Present(_model);
    }

    public void SetThreadReady(bool ready)
    {
        if (_thread != null) _thread.Text = ready ? "THREAD READY" : "THREAD COOLING";
        SetThreadCooldown(ready ? 1f : 0f);
    }

    public void SetThreadCooldown(float readyRatio)
    {
        _model = _model.WithThread(readyRatio);
        _formal?.Present(_model);
    }

    public void SetObjective(string text)
    {
        if (_objective != null) _objective.Text = text ?? string.Empty;
        _model = _model with { Objective = text ?? string.Empty };
        _formal?.Present(_model);
    }

    public void ShowPrompt(string text)
    {
        ShowPrompt(string.Empty, text);
    }

    public void ShowPrompt(string glyph, string text)
    {
        if (_prompt != null)
        {
            _prompt.Text = text ?? string.Empty;
            _prompt.Visible = !string.IsNullOrWhiteSpace(text);
        }
        _model = _model with { PromptGlyph = glyph ?? string.Empty, PromptText = text ?? string.Empty };
        _formal?.Present(_model);
    }

    public void ShowBoss(string name, int current, int max, int phase, bool exposed)
    {
        if (_boss != null) _boss.Visible = true;
        if (_bossName != null) _bossName.Text = name ?? string.Empty;
        if (_bossHealth != null)
        {
            _bossHealth.MaxValue = Mathf.Max(1, max);
            _bossHealth.Value = Mathf.Clamp(current, 0, max);
        }
        string posture = exposed ? "EXPOSED" : phase >= 2 ? "ARMORED" : "OPEN";
        if (_bossState != null) _bossState.Text = $"PHASE {phase} / {posture}";
        _model = _model.WithBoss(name, current, max, phase, phase >= 2 && !exposed, exposed);
        _formal?.Present(_model);
    }

    public void HideBoss()
    {
        if (_boss != null) _boss.Visible = false;
        _model = _model.WithoutBoss();
        _formal?.Present(_model);
    }

    public void ShowRecovery(string reason)
    {
        _model = _model.WithRecovery(reason);
        _formal?.Present(_model);
    }

    public void HideRecovery()
    {
        _model = _model.WithoutRecovery();
        _formal?.Present(_model);
    }

    public void ShowCompletion(string time, int deaths, bool hiddenFound, bool shortcutOpen)
    {
        _model = _model.WithCompletion(time, deaths, hiddenFound, shortcutOpen);
        _formal?.Present(_model);
    }

    public void ResetForNewGame()
    {
        _model = PrologueHudViewModel.Gameplay(5, 5, 1f, string.Empty, string.Empty, string.Empty);
        GetNodeOrNull<PrologueCompletionView>("FormalHud/Completion")?.Clear();
        GetNodeOrNull<PrologueRecoveryView>("FormalHud/Recovery")?.Clear();
        HideBoss();
        SetHealth(5, 5);
        SetObjective(PrologueObjectiveModel.TextFor(PrologueObjective.LearnAttackMode));
        ShowPrompt(string.Empty, string.Empty);
    }
}
