using System;
using System.IO;
using Godot;
using Knight.Audio;
using Knight.Prologue.Guidance;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;
using Knight.Vfx;

namespace Knight.Prologue;

public enum PrologueGameFlowState
{
    Title,
    Intro,
    Playing,
    Paused,
    Recovering,
    Completed
}

public partial class PrologueGameFlowController : Node
{
    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath UiPath { get; set; } = new("../ProductUi");
    [Export] public NodePath PausePath { get; set; } = new("../PauseMenu");
    [Export] public NodePath CompletionPath { get; set; } = new("../Completion");
    [Export] public NodePath AudioPath { get; set; } = new("../Audio");
    [Export] public NodePath VfxPath { get; set; } = new("../Vfx");
    [Export] public NodePath GuidancePath { get; set; } = new("../Guidance");
    [Export] public string SavePath { get; set; } = "user://knight/prologue-save.json";
    [Export] public string SettingsPath { get; set; } = "user://knight/settings.json";
    [Export] public bool HeadlessAutoStart { get; set; } = true;
    [Export] public bool AutomationAutoStart { get; set; }

    public PrologueGameFlowState State { get; private set; } = PrologueGameFlowState.Title;
    public PrologueSaveData Save { get; private set; } = new();
    public GameSettingsData Settings { get; private set; } = new();
    public bool HasSave { get; private set; }
    public string LastRejectedTransition { get; private set; } = string.Empty;

    private PrologueLevelController _level;
    private PrologueUiRoot _ui;
    private ProloguePauseMenu _pause;
    private PrologueCompletionController _completion;
    private PrologueAudioDirector _audio;
    private PrologueVfxDirector _vfx;
    private PrologueGuidanceController _guidance;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _level = GetNode<PrologueLevelController>(LevelPath);
        _ui = GetNode<PrologueUiRoot>(UiPath);
        _pause = GetNode<ProloguePauseMenu>(PausePath);
        _completion = GetNode<PrologueCompletionController>(CompletionPath);
        _audio = GetNode<PrologueAudioDirector>(AudioPath);
        _vfx = GetNode<PrologueVfxDirector>(VfxPath);
        _guidance = GetNode<PrologueGuidanceController>(GuidancePath);
        _ui.StartRequested += StartNew;
        _ui.ContinueRequested += Continue;
        _ui.QuitRequested += OnQuit;
        _ui.SettingsChanged += ApplySettings;
        _pause.Opened += OnPaused;
        _pause.Closed += OnResumed;
        _completion.PrologueCompleted += Complete;
        _level.RecoveryRequested += OnRecoveryRequested;
        _level.RecoveryCompleted += OnRecoveryCompleted;
        _level.ProgressPersistRequested += PersistProgress;
        Button returnButton = _level.GetNodeOrNull<Button>("Hud/FormalHud/Completion/Panel/ReturnToTitle");
        Button replayButton = _level.GetNodeOrNull<Button>("Hud/FormalHud/Completion/Panel/Replay");
        if (returnButton != null) returnButton.Pressed += ReturnToTitle;
        if (replayButton != null) replayButton.Pressed += StartNew;
        Callable.From(InitializeAfterLevelReady).CallDeferred();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_ui))
        {
            _ui.StartRequested -= StartNew;
            _ui.ContinueRequested -= Continue;
            _ui.QuitRequested -= OnQuit;
            _ui.SettingsChanged -= ApplySettings;
        }
        if (IsInstanceValid(_pause)) { _pause.Opened -= OnPaused; _pause.Closed -= OnResumed; }
        if (IsInstanceValid(_completion)) _completion.PrologueCompleted -= Complete;
        if (IsInstanceValid(_level))
        {
            _level.RecoveryRequested -= OnRecoveryRequested;
            _level.RecoveryCompleted -= OnRecoveryCompleted;
            _level.ProgressPersistRequested -= PersistProgress;
        }
    }

    public void StartNew()
    {
        if (!TryState(PrologueGameFlowState.Intro, PrologueGameFlowState.Title, PrologueGameFlowState.Completed)) return;
        Save = new PrologueSaveData
        {
            BestHumanCompletionSeconds = Save.BestHumanCompletionSeconds,
            BestHumanDeaths = Save.BestHumanDeaths
        };
        _completion.ResetSession();
        _level.StartNewGame();
        PersistProgress("new-game");
        BeginPlaying();
    }

    public void Continue()
    {
        if (State != PrologueGameFlowState.Title || !HasSave)
        {
            Reject(PrologueGameFlowState.Playing);
            return;
        }
        _completion.ResetSession();
        _level.RestoreFromSave(Save);
        if (Enum.TryParse(Save.Objective, ignoreCase: false, out PrologueObjective objective) &&
            Enum.IsDefined(objective)) _completion.RestoreSession(objective);
        BeginPlaying();
    }

    public void ReturnToTitle()
    {
        if (State == PrologueGameFlowState.Recovering) return;
        GetTree().Paused = false;
        State = PrologueGameFlowState.Title;
        _audio.SetPaused(false);
        _level.SetGameplayActive(false);
        _ui.Visible = true;
        HasSave = File.Exists(Absolute(SavePath)) && !Save.WasRecovered && !Save.Completed;
        _ui.ShowMainMenu(HasSave);
    }

    public void ApplySettings(GameSettingsData settings)
    {
        Settings = (settings ?? new GameSettingsData()).Normalize();
        GameSettingsStore.SaveAtomic(Absolute(SettingsPath), Settings);
        _audio.ApplySettings(Settings);
        _vfx.Configure(Settings);
        _guidance.SetGuidanceEnabled(Settings.GuidanceEnabled);
        _guidance.SetLanguage(Settings.Language);
        _level.SetLanguage(Settings.Language);
        _pause.SetLanguage(Settings.Language);
        _level.GetNodeOrNull<PrologueFormalHud>("Hud/FormalHud")?.SetLanguage(Settings.Language);
        if (DisplayServer.GetName() != "headless")
        {
            DisplayServer.WindowSetVsyncMode(Settings.VSync
                ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
            DisplayServer.WindowSetMode(Settings.WindowMode switch
            {
                GameWindowMode.Fullscreen => DisplayServer.WindowMode.Fullscreen,
                GameWindowMode.Borderless => DisplayServer.WindowMode.ExclusiveFullscreen,
                _ => DisplayServer.WindowMode.Windowed
            });
            if (Settings.WindowMode == GameWindowMode.Windowed)
                DisplayServer.WindowSetSize(new Vector2I(Settings.ResolutionWidth, Settings.ResolutionHeight));
        }
        GetTree().Root.ContentScaleFactor = Settings.UiScale;
    }

    public void Complete()
    {
        if (!TryState(PrologueGameFlowState.Completed, PrologueGameFlowState.Playing)) return;
        Save.Completed = true;
        Save.Objective = nameof(PrologueObjective.Complete);
        PersistProgress("completion");
        _audio.ClearStagedFrameAudio();
        _audio.Play(PrologueAudioCue.Completion, Vector2.Zero);
        _audio.SetMusicState(PrologueMusicState.Complete);
    }

    public void PersistProgress(string reason)
    {
        PrologueCheckpointSnapshot snapshot = _level.Progress.RestoreCheckpoint();
        Save.HasActivatedCheckpoint = snapshot.HasActivatedCheckpoint;
        Save.CheckpointSector = snapshot.SectorId.ToString();
        Save.CheckpointWorldX = snapshot.WorldPosition.X;
        Save.CheckpointWorldY = snapshot.WorldPosition.Y;
        Save.ShortcutOpen = snapshot.ShortcutOpen;
        Save.Discoveries.Clear();
        foreach (string discovery in snapshot.Progress.Discoveries) Save.Discoveries.Add(discovery);
        Save.BossDefeated = snapshot.Progress.BossDefeated;
        Save.Objective = _completion.CurrentObjective.ToString();
        Save.CompletedTutorials.Clear();
        foreach (StringName tutorial in _level.Progress.CompletedTutorials)
            Save.CompletedTutorials.Add(tutorial.ToString());
        PrologueSaveStore.SaveAtomic(Absolute(SavePath), Save);
        HasSave = !Save.Completed;
    }

    private void InitializeAfterLevelReady()
    {
        Save = PrologueSaveStore.LoadOrNew(Absolute(SavePath));
        HasSave = File.Exists(Absolute(SavePath)) && !Save.WasRecovered && !Save.Completed;
        Settings = GameSettingsStore.LoadOrDefault(Absolute(SettingsPath));
        ApplySettings(Settings);
        ReturnToTitle();
        if ((HeadlessAutoStart && DisplayServer.GetName() == "headless") || AutomationAutoStart) StartNew();
    }

    private void BeginPlaying()
    {
        State = PrologueGameFlowState.Playing;
        _ui.Visible = false;
        _level.SetGameplayActive(true);
        _audio.SetPaused(false);
    }

    private void OnPaused()
    {
        if (!TryState(PrologueGameFlowState.Paused, PrologueGameFlowState.Playing)) return;
        _audio.SetPaused(true);
    }

    private void OnResumed()
    {
        if (State != PrologueGameFlowState.Paused) return;
        State = PrologueGameFlowState.Playing;
        _audio.SetPaused(false);
    }

    private void OnRecoveryRequested(string reason)
    {
        if (State == PrologueGameFlowState.Completed)
        {
            Reject(PrologueGameFlowState.Recovering);
            _level.CancelQueuedRecovery();
            return;
        }
        TryState(PrologueGameFlowState.Recovering, PrologueGameFlowState.Playing);
    }

    private void OnRecoveryCompleted(string reason)
    {
        if (State == PrologueGameFlowState.Recovering) State = PrologueGameFlowState.Playing;
    }

    private bool TryState(PrologueGameFlowState target, params PrologueGameFlowState[] allowed)
    {
        foreach (PrologueGameFlowState state in allowed)
            if (State == state) { State = target; LastRejectedTransition = string.Empty; return true; }
        Reject(target);
        return false;
    }

    private void Reject(PrologueGameFlowState target)
    {
        LastRejectedTransition = $"{State}->{target}";
        GD.PushWarning($"Rejected prologue flow transition {LastRejectedTransition}.");
    }

    private void OnQuit() => GetTree().Quit();

    private static string Absolute(string path) => path.StartsWith("user://", StringComparison.Ordinal)
        ? ProjectSettings.GlobalizePath(path) : Path.GetFullPath(path);
}
