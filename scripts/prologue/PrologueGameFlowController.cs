using System;
using System.IO;
using Godot;
using Knight.Audio;
using Knight.Boss;
using Knight.Combat;
using Knight.Prologue.Guidance;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;
using Knight.Vfx;
using Knight.Presentation.Minimap;

namespace Knight.Prologue;

public enum PrologueGameFlowState
{
    Title,
    Intro,
    Playing,
    Paused,
    Recovering,
    Completed,
    CompletionSaveFailed
}

public partial class PrologueGameFlowController : Node
{
    [Signal] public delegate void SessionStartedEventHandler();
    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath UiPath { get; set; } = new("../ProductUi");
    [Export] public NodePath PausePath { get; set; } = new("../PauseMenu");
    [Export] public NodePath CompletionPath { get; set; } = new("../Completion");
    [Export] public NodePath AudioPath { get; set; } = new("../Audio");
    [Export] public NodePath VfxPath { get; set; } = new("../Vfx");
    [Export] public NodePath GuidancePath { get; set; } = new("../Guidance");
    [Export] public string SavePath { get; set; } = "user://knight/prologue-save-layout-v2.json";
    public const string LegacySavePath = "user://knight/prologue-save.json";
    [Export] public string SettingsPath { get; set; } = "user://knight/settings.json";
    [Export] public bool HeadlessAutoStart { get; set; } = true;
    [Export] public bool AutomationAutoStart { get; set; }
    [Export] public bool BasinQualitySampleMode { get; set; }

    public PrologueGameFlowState State { get; private set; } = PrologueGameFlowState.Title;
    public PrologueSaveData Save { get; private set; } = new();
    public GameSettingsData Settings { get; private set; } = new();
    public bool HasSave { get; private set; }
    public PrologueSaveLoadResult LoadResult { get; private set; }
    public string LastRejectedTransition { get; private set; } = string.Empty;

    private PrologueLevelController _level;
    private PrologueUiRoot _ui;
    private ProloguePauseMenu _pause;
    private PrologueCompletionController _completion;
    private PrologueAudioDirector _audio;
    private PrologueVfxDirector _vfx;
    private PrologueGuidanceController _guidance;
    private bool _awaitingLayoutChoice;
    private bool _preserveReplacedSlot;
    private PrologueMinimapController _minimap;
    private ConfirmationDialog _completionSaveFailure;
    private bool _ownsCompletionPause;
    private bool _completionRetryArmed;

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
        _completion.DevelopmentSampleEnded += EndDevelopmentSample;
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
        ReleaseCompletionPause();
        if (IsInstanceValid(_ui))
        {
            _ui.StartRequested -= StartNew;
            _ui.ContinueRequested -= Continue;
            _ui.QuitRequested -= OnQuit;
            _ui.SettingsChanged -= ApplySettings;
        }
        if (IsInstanceValid(_pause)) { _pause.Opened -= OnPaused; _pause.Closed -= OnResumed; }
        if (IsInstanceValid(_completion))
        {
            _completion.DevelopmentSampleEnded -= EndDevelopmentSample;
        }
        if (IsInstanceValid(_level))
        {
            _level.RecoveryRequested -= OnRecoveryRequested;
            _level.RecoveryCompleted -= OnRecoveryCompleted;
            _level.ProgressPersistRequested -= PersistProgress;
        }
    }

    public void StartNew()
    {
        if (!BasinQualitySampleMode && !_awaitingLayoutChoice && !CanWriteFullWorldSlot()) return;
        if (!TryState(PrologueGameFlowState.Intro, PrologueGameFlowState.Title, PrologueGameFlowState.Completed)) return;
        _preserveReplacedSlot = _awaitingLayoutChoice && LoadResult?.Status == PrologueSaveLoadStatus.Recovered;
        AcceptLayoutChoice();
        PrologueSaveData previous = Save;
        Save = BasinQualitySampleMode ? new PrologueSaveData() :
            PrologueSaveData.NewForLayout(CanyonWorldLayout.CurrentLayoutVersion);
        if (!BasinQualitySampleMode)
        {
            Save.BestHumanCompletionSeconds = previous.BestHumanCompletionSeconds;
            Save.BestHumanDeaths = previous.BestHumanDeaths;
        }
        _completion.ResetSession();
        _minimap?.StartNewExploration();
        SetCheckpointSaveFailure(false);
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
        bool mappedChoice = _awaitingLayoutChoice && LoadResult?.Status == PrologueSaveLoadStatus.MigratableLegacy;
        AcceptLayoutChoice();
        _completion.ResetSession();
        _minimap?.RestoreExploration(Save.ExplorationMap);
        SetCheckpointSaveFailure(false);
        _level.RestoreFromSave(Save);
        if (BasinQualitySampleMode && _level.Progress.BossDefeated)
            _level.CurrentSectorRoot?.GetNodeOrNull<BossArenaController>("Gameplay/BossArenaController")
                ?.RestoreDefeatedEncounter();
        if (!BasinQualitySampleMode &&
            Enum.TryParse(Save.Objective, ignoreCase: false, out PrologueObjective objective) &&
            Enum.IsDefined(objective)) _completion.RestoreSession(objective,
                completeMappedLegacyBossResult: mappedChoice && Save.BossDefeated);
        if (mappedChoice) PersistProgress("mapped-continue");
        BeginPlaying();
    }

    public void ReturnToTitle()
    {
        if (State == PrologueGameFlowState.Recovering) return;
        _completionSaveFailure?.Hide();
        ReleaseCompletionPause();
        GetTree().Paused = false;
        State = PrologueGameFlowState.Title;
        _audio.SetPaused(false);
        _level.SetGameplayActive(false);
        _ui.Visible = true;
        HasSave = IsContinueSaveAllowed();
        _ui.GetNode<PrologueMainMenu>("MainMenu").MigrationStatus = _awaitingLayoutChoice
            ? LoadResult?.Status : null;
        _ui.ShowMainMenu(HasSave);
    }

    public void ApplySettings(GameSettingsData settings)
    {
        Settings = (settings ?? new GameSettingsData()).Normalize();
        if (BasinQualitySampleMode) Settings.GuidanceEnabled = false;
        GameSettingsStore.SaveAtomic(Absolute(SettingsPath), Settings);
        _audio.ApplySettings(Settings);
        _vfx.Configure(Settings);
        _guidance.SetGuidanceEnabled(Settings.GuidanceEnabled);
        if (BasinQualitySampleMode) _guidance.ProcessMode = ProcessModeEnum.Disabled;
        _guidance.SetLanguage(Settings.Language);
        _ui.SetSettings(Settings);
        _level.SetLanguage(Settings.Language);
        _pause.SetLanguage(Settings.Language);
        _completion.SetLanguage(Settings.Language);
        _level.GetNodeOrNull<PrologueFormalHud>("Hud/FormalHud")?.SetLanguage(Settings.Language);
        UpdateSamplePauseChrome();
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

    public void Complete() => TryComplete();

    internal bool TryComplete()
    {
        if (BasinQualitySampleMode) return false;
        if (State is not (PrologueGameFlowState.Playing or PrologueGameFlowState.CompletionSaveFailed))
        { Reject(PrologueGameFlowState.Completed); return false; }
        PrologueSaveData candidate;
        try
        {
            if (!CanWriteFullWorldSlot()) { ShowCompletionSaveFailure(); return false; }
            candidate = _level.Progress.ToSave(Save);
            candidate.Completed = true;
            candidate.Objective = nameof(PrologueObjective.Complete);
            if (_minimap != null) candidate.ExplorationMap = _minimap.CaptureExploration();
            candidate = candidate.Normalize();
            PrologueSaveStore.SaveAtomic(Absolute(SavePath), candidate, _preserveReplacedSlot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { ShowCompletionSaveFailure(); return false; }

        // Only a durable commit may become a success notification/terminal state.
        Save = candidate;
        _preserveReplacedSlot = false;
        HasSave = false;
        State = PrologueGameFlowState.Completed;
        LastRejectedTransition = string.Empty;
        _completionSaveFailure?.Hide();
        ReleaseCompletionPause();
        _audio.SetPaused(false);
        _audio.ClearStagedFrameAudio();
        _audio.Play(PrologueAudioCue.Completion, Vector2.Zero);
        _audio.SetMusicState(PrologueMusicState.Complete);
        _level.SetGameplayActive(false);
        _level.GetNode<PrologueHud>("Hud").Visible = true;
        return true;
    }

    private void ShowCompletionSaveFailure()
    {
        State = PrologueGameFlowState.CompletionSaveFailed;
        _level.SetGameplayActive(false);
        if (!GetTree().Paused)
        {
            _ownsCompletionPause = true;
            GetTree().Paused = true;
        }
        _audio.ClearStagedFrameAudio();
        _audio.SetPaused(true);
        if (_completionSaveFailure == null)
        {
            _completionSaveFailure = new ConfirmationDialog
            {
                Name = "CompletionSaveFailure", DialogHideOnOk = false,
                Transient = true, Exclusive = true, Unresizable = true
            };
            AddChild(_completionSaveFailure);
            _completionSaveFailure.Confirmed += RetryCompletionSave;
            _completionSaveFailure.Canceled += CancelCompletionSave;
        }
        bool english = Settings.Language == GameLanguage.English;
        _completionSaveFailure.Title = english ? "COMPLETION NOT SAVED" : "通关结果尚未保存";
        _completionSaveFailure.DialogText = english
            ? "The save could not be written. Gameplay is safely stopped.\nRetry after the storage problem is resolved.\nReturning to the title keeps only the last successful save."
            : "存档写入失败，角色已安全停止。\n请在存储问题解除后重试保存。\n返回标题仅保留上次成功保存的进度，本次通关尚未记入。";
        _completionSaveFailure.OkButtonText = english ? "RETRY SAVE" : "重试保存";
        _completionSaveFailure.CancelButtonText = english ? "RETURN TO TITLE" : "返回标题";
        if (!_completionSaveFailure.Visible)
        {
            _completionRetryArmed = !Input.IsActionPressed("ui_accept");
            _completionSaveFailure.PopupCentered();
        }
        _completionSaveFailure.GetOkButton().GrabFocus();
    }

    public override void _Process(double delta)
    {
        if (State == PrologueGameFlowState.CompletionSaveFailed && !Input.IsActionPressed("ui_accept"))
            _completionRetryArmed = true;
    }

    private void RetryCompletionSave()
    {
        if (!_completionRetryArmed || State != PrologueGameFlowState.CompletionSaveFailed) return;
        _completionRetryArmed = false;
        _completion.RetryPendingCompletion();
    }

    private void ReleaseCompletionPause()
    {
        if (!_ownsCompletionPause) return;
        _ownsCompletionPause = false;
        if (IsInsideTree()) GetTree().Paused = false;
    }

    private void CancelCompletionSave()
    {
        if (State == PrologueGameFlowState.CompletionSaveFailed) ReturnToTitle();
    }

    private void EndDevelopmentSample()
    {
        if (!BasinQualitySampleMode || !_completion.SampleEndingReached ||
            !TryState(PrologueGameFlowState.Completed, PrologueGameFlowState.Playing)) return;
        PersistProgress("development-sample-exit");
        _audio.ClearStagedFrameAudio();
        _vfx.ClearTransientEffects();
        _level.SetGameplayActive(false);
        // Keep the existing completion layout/buttons, but not its full-game claim.
        var hud = _level.GetNode<PrologueHud>("Hud");
        hud.ShowCompletion(string.Empty, 0, false, false);
        hud.Visible = true;
        hud.GetNode<PrologueCompletionView>("FormalHud/Completion").PresentDevelopmentSample();
    }

    public FragmentCollectionResult TryCollectFragment(string stableId)
    {
        if (!QuestFragmentCatalog.IsKnown(stableId)) return FragmentCollectionResult.InvalidId;
        if (!IsInsideTree() || IsQueuedForDeletion() || !IsInstanceValid(_level) ||
            !_level.IsInsideTree() || _level.IsQueuedForDeletion() || !IsInstanceValid(_level.Player) ||
            !_level.Player.IsInsideTree() || _level.Player.IsQueuedForDeletion() || GetTree().Paused ||
            State != PrologueGameFlowState.Playing || _awaitingLayoutChoice ||
            _level.Player.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 })
            return FragmentCollectionResult.Unavailable;
        if (_level.Progress.CollectedFragmentIds.Contains(stableId))
            return FragmentCollectionResult.AlreadyCollected;

        PrologueSaveData candidate;
        try
        {
            if (!BasinQualitySampleMode && !CanWriteFullWorldSlot())
                return FragmentCollectionResult.SaveFailed;
            candidate = _level.Progress.ToSave(Save);
            if (_minimap != null) candidate.ExplorationMap = _minimap.CaptureExploration();
            candidate.CollectedFragmentIds.Add(stableId);
            candidate.Objective = BasinQualitySampleMode
                ? nameof(PrologueObjective.Wake) : _completion.CurrentObjective.ToString();
            if (BasinQualitySampleMode)
            {
                candidate.Completed = false;
                candidate.BestHumanCompletionSeconds = -1d;
                candidate.BestHumanDeaths = -1;
            }
            candidate = candidate.Normalize();
            // No live mutation, signal or await until the durable write commits.
            PrologueSaveStore.SaveAtomic(Absolute(SavePath), candidate, _preserveReplacedSlot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { return FragmentCollectionResult.SaveFailed; }

        if (!_level.Progress.TryCollectFragment(stableId))
            throw new InvalidOperationException("Committed fragment unexpectedly already exists in live progress.");
        Save = candidate;
        _preserveReplacedSlot = false;
        HasSave = IsContinueSaveAllowed();
        RefreshFragmentHud();
        return FragmentCollectionResult.Collected;
    }

    internal bool TryCommitCheckpoint(string stableId)
    {
        if (!IsInsideTree() || IsQueuedForDeletion() || !IsInstanceValid(_level) ||
            !_level.IsInsideTree() || _level.IsQueuedForDeletion() || !IsInstanceValid(_level.Player) ||
            !_level.Player.IsInsideTree() || _level.Player.IsQueuedForDeletion() || GetTree().Paused ||
            State != PrologueGameFlowState.Playing || _awaitingLayoutChoice || BasinQualitySampleMode ||
            Save.LayoutVersion != CanyonWorldLayout.CurrentLayoutVersion ||
            _level.Player.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 } ||
            !CanyonWorldLayout.TryResolveCheckpoint(stableId, out var sector, out var position) ||
            sector == PrologueSectorId.CanyonEntrance) return false;

        var old = _level.Progress.RestoreCheckpoint();
        if (old.HasActivatedCheckpoint && old.CheckpointId == stableId)
        { SetCheckpointSaveFailure(false); return true; }
        if (old.HasActivatedCheckpoint && old.SectorId == PrologueSectorId.LostTemple &&
            sector == PrologueSectorId.CanyonBasin) return false;

        PrologueSaveData candidate;
        try
        {
            if (!CanWriteFullWorldSlot()) { SetCheckpointSaveFailure(true); return false; }
            candidate = _level.Progress.ToSave(Save);
            candidate.CheckpointId = stableId;
            candidate.HasActivatedCheckpoint = true;
            candidate.CheckpointSector = sector.ToString();
            candidate.CheckpointWorldX = position.X;
            candidate.CheckpointWorldY = position.Y;
            candidate.Objective = _completion.CurrentObjective.ToString();
            if (_minimap != null) candidate.ExplorationMap = _minimap.CaptureExploration();
            candidate = candidate.Normalize();
            PrologueSaveStore.SaveAtomic(Absolute(SavePath), candidate, _preserveReplacedSlot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { SetCheckpointSaveFailure(true); return false; }

        // No await or public notification between the durable and live commits.
        if (!_level.Progress.ActivateCheckpoint(stableId))
            throw new InvalidOperationException("Committed checkpoint unexpectedly rejected by live progress.");
        Save = candidate;
        _preserveReplacedSlot = false;
        HasSave = IsContinueSaveAllowed();
        SetCheckpointSaveFailure(false);
        return true;
    }

    private void SetCheckpointSaveFailure(bool failed) =>
        _level?.GetNodeOrNull<PrologueFormalHud>("Hud/FormalHud")?.SetCheckpointSaveFailure(failed);

    private void RefreshFragmentHud() => _level.GetNodeOrNull<PrologueHud>("Hud")?.SetFragments(
        _level.Progress.CollectedFragmentIds, _level.Progress.LegacyCollectionQuotaExempt);

    public void PersistProgress(string reason)
    {
        PersistProgressIfEligible(reason);
    }

    private bool PersistProgressIfEligible(string reason)
    {
        if (State == PrologueGameFlowState.CompletionSaveFailed) return false;
        if (_awaitingLayoutChoice) return false;
        if (!BasinQualitySampleMode && !CanWriteFullWorldSlot()) return false;
        PrologueCheckpointSnapshot snapshot = _level.Progress.RestoreCheckpoint();
        Save.HasActivatedCheckpoint = snapshot.HasActivatedCheckpoint;
        Save.CheckpointSector = snapshot.SectorId.ToString();
        Save.CheckpointWorldX = snapshot.WorldPosition.X;
        Save.CheckpointWorldY = snapshot.WorldPosition.Y;
        Save.ShortcutOpen = snapshot.ShortcutOpen;
        if (!BasinQualitySampleMode)
        {
            Save.CheckpointId = snapshot.CheckpointId;
            Save.OpenShortcuts.Clear();
            foreach (string id in _level.Progress.OpenShortcuts) Save.OpenShortcuts.Add(id);
            Save.ActivatedMechanisms.Clear();
            foreach (string id in _level.Progress.ActivatedMechanisms) Save.ActivatedMechanisms.Add(id);
        }
        Save.Discoveries.Clear();
        foreach (string discovery in snapshot.Progress.Discoveries) Save.Discoveries.Add(discovery);
        Save.BossDefeated = snapshot.Progress.BossDefeated;
        Save.CollectionRuleVersion = snapshot.Progress.CollectionRuleVersion;
        Save.LegacyCollectionQuotaExempt = snapshot.Progress.LegacyCollectionQuotaExempt;
        Save.CollectedFragmentIds.Clear();
        foreach (string id in snapshot.Progress.CollectedFragmentIds) Save.CollectedFragmentIds.Add(id);
        Save.Objective = BasinQualitySampleMode
            ? nameof(PrologueObjective.Wake) : _completion.CurrentObjective.ToString();
        if (BasinQualitySampleMode)
        {
            Save.Completed = false;
            Save.BestHumanCompletionSeconds = -1d;
            Save.BestHumanDeaths = -1;
        }
        Save.CompletedTutorials.Clear();
        foreach (StringName tutorial in _level.Progress.CompletedTutorials)
            Save.CompletedTutorials.Add(tutorial.ToString());
        // Preserve original core mutations/failure policy, but never commit an
        // unsaved map DTO into Save until its detached candidate actually lands.
        PrologueSaveData candidate = _minimap == null ? Save : Save.Normalize();
        if (_minimap != null) candidate.ExplorationMap = _minimap.CaptureExploration();
        PrologueSaveStore.SaveAtomic(Absolute(SavePath), candidate, _preserveReplacedSlot);
        if (_minimap != null) Save.ExplorationMap = candidate.ExplorationMap;
        _preserveReplacedSlot = false;
        HasSave = IsContinueSaveAllowed();
        return true;
    }

    public bool TrySaveBeforeQuit(out string failure)
    {
        failure = string.Empty;
        try
        {
            if (!PersistProgressIfEligible("quit"))
            { failure = "Current slot is not eligible for writing."; return false; }
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { failure = "Progress could not be saved."; return false; }
    }

    private void InitializeAfterLevelReady()
    {
        _minimap = _level.GetNodeOrNull<PrologueMinimapController>("MinimapController");
        if (BasinQualitySampleMode) Save = PrologueSaveStore.LoadOrNew(Absolute(SavePath), mapCatalog: _minimap?.Catalog);
        else
        {
            // Resolve the sibling legacy slot from the selected path. Injected
            // fixtures and standalone samples must never fall back to real user data.
            string legacy = Path.Combine(Path.GetDirectoryName(Absolute(SavePath))!,
                Path.GetFileName(LegacySavePath));
            LoadResult = PrologueSaveStore.LoadFullWorld(Absolute(SavePath), legacy, _minimap?.Catalog);
            Save = LoadResult.Data;
            _awaitingLayoutChoice = LoadResult.Status != PrologueSaveLoadStatus.Current;
        }
        _minimap?.RestoreExploration(Save.ExplorationMap);
        _ui.BasinQualitySampleMode = BasinQualitySampleMode;
        HasSave = IsContinueSaveAllowed();
        Settings = GameSettingsStore.LoadOrDefault(Absolute(SettingsPath));
        ApplySettings(Settings);
        ReturnToTitle();
        if (!_awaitingLayoutChoice &&
            ((HeadlessAutoStart && DisplayServer.GetName() == "headless") || AutomationAutoStart)) StartNew();
    }

    private void AcceptLayoutChoice()
    {
        _awaitingLayoutChoice = false;
        _ui.GetNode<PrologueMainMenu>("MainMenu").MigrationStatus = null;
    }

    private void BeginPlaying()
    {
        State = PrologueGameFlowState.Playing;
        _ui.Visible = false;
        _level.SetGameplayActive(true);
        _audio.SetPaused(false);
        // RestoreSession resets HUD too, so refresh only after that operation.
        RefreshFragmentHud();
        EmitSignal(SignalName.SessionStarted);
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

    internal bool AllowsRecoveryRequest()
    {
        if (State is not (PrologueGameFlowState.Completed or PrologueGameFlowState.CompletionSaveFailed)) return true;
        Reject(PrologueGameFlowState.Recovering);
        return false;
    }

    private void OnRecoveryRequested(string reason)
    {
        TryState(PrologueGameFlowState.Recovering, PrologueGameFlowState.Playing);
    }

    private void OnRecoveryCompleted(string reason)
    {
        if (State == PrologueGameFlowState.Recovering) State = PrologueGameFlowState.Playing;
        if (BasinQualitySampleMode) _completion.RefreshDisplay();
    }

    private bool IsContinueSaveAllowed()
    {
        if (_awaitingLayoutChoice) return LoadResult?.Status == PrologueSaveLoadStatus.MigratableLegacy &&
            !Save.Completed;
        if (!File.Exists(Absolute(SavePath)) || Save.WasRecovered || Save.Completed) return false;
        if (!BasinQualitySampleMode) return Save.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion;
        return Save.HasActivatedCheckpoint &&
            Save.BestHumanCompletionSeconds < 0d && Save.BestHumanDeaths < 0 &&
            Save.Objective == nameof(PrologueObjective.Wake) &&
            MatchesAuthoredSampleCheckpoint();
    }

    private bool CanWriteFullWorldSlot() => !_awaitingLayoutChoice && (_preserveReplacedSlot || !File.Exists(Absolute(SavePath)) ||
        !PrologueSaveStore.LoadOrNew(Absolute(SavePath), CanyonWorldLayout.CurrentLayoutVersion).WasRecovered);

    private bool MatchesAuthoredSampleCheckpoint()
    {
        if (!Enum.TryParse(Save.CheckpointSector, out PrologueSectorId sector) ||
            sector is not (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple) ||
            !_level.IsInsideAuthoredSectorBounds(sector, Save.CheckpointWorldPosition)) return false;
        var world = _level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors");
        if (world == null) return false;
        if (sector == world.InitialSector &&
            world.InitialPlayerPosition.DistanceTo(Save.CheckpointWorldPosition) <= 1f) return true;
        foreach (PrologueSectorDefinition definition in world.Definitions)
        {
            if (definition.Id != sector) continue;
            // Never add this inspection instance to the tree: no actor _Ready,
            // checkpoint activation, audio or mechanism mutation may run on load.
            Node authored = definition.Scene.Instantiate();
            try
            {
                return ContainsAuthoredCheckpoint(authored,
                    new Transform2D(0f, definition.WorldOffset), Save.CheckpointWorldPosition);
            }
            finally { authored.Free(); }
        }
        return false;
    }

    private static bool ContainsAuthoredCheckpoint(Node node, Transform2D parentTransform, Vector2 expected)
    {
        Transform2D transform = node is Node2D spatial ? parentTransform * spatial.Transform : parentTransform;
        if (node is CheckpointTrigger && transform.Origin.DistanceTo(expected) <= 1f) return true;
        foreach (Node child in node.GetChildren())
            if (ContainsAuthoredCheckpoint(child, transform, expected)) return true;
        return false;
    }

    private void UpdateSamplePauseChrome()
    {
        if (!BasinQualitySampleMode) return;
        Button guidance = _level.GetNodeOrNull<Button>("PauseMenu/Panel/GuidanceToggle");
        if (guidance != null)
        {
            guidance.Visible = false;
            guidance.Disabled = true;
            guidance.FocusMode = Control.FocusModeEnum.None;
        }
        Button returnToTitle = _level.GetNodeOrNull<Button>("PauseMenu/Panel/ReturnToTitle");
        if (returnToTitle != null)
            returnToTitle.Text = Settings.Language == GameLanguage.English
                ? "RETURN TO TITLE" : "返回标题";
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
