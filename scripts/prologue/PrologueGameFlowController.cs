using System;
using System.IO;
using System.Threading.Tasks;
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
    private sealed record StartupAttempt(long Version, PrologueSaveData Draft, bool WriteSlot, bool PreserveReplacedSlot);
    private StartupAttempt _startup;
    private long _entryVersion;

    internal sealed class SupplyClaim(PrologueLevelController level, PrologueSaveData save,
        long entryVersion, string savePath, string discoveryId)
    {
        internal readonly PrologueLevelController Level = level;
        internal readonly PrologueSaveData Save = save;
        internal readonly long EntryVersion = entryVersion;
        internal readonly string SavePath = savePath;
        internal readonly string DiscoveryId = discoveryId;
        internal bool Finished;
    }
    private PrologueMinimapController _minimap;
    private ConfirmationDialog _completionSaveFailure;
    private bool _ownsCompletionPause;
    private bool _completionRetryArmed;
    public bool IsApplicationQuitting { get; private set; }
    private Task<AudioQuitDrainResult> _applicationQuitPreparation;
    private bool _applicationExitRequested;
    private ulong _applicationQuitGeneration;
    private ulong _applicationQuitRequestGeneration;
    private SceneTree _applicationWindowTree;
    private Window _applicationWindow;
    private bool _previousAutoAcceptQuit;
    private const string ApplicationQuitWindowOwnerKey = "_knight_application_quit_window_owner";
    private const string ApplicationQuitWindowBaselineKey = "_knight_application_quit_auto_baseline";

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
        Callable.From(BindApplicationWindowQuit).CallDeferred();
    }

    public override void _ExitTree()
    {
        _startup = null;
        _applicationQuitGeneration++;
        ReleaseApplicationWindowQuit();
        if (IsInstanceValid(_level)) _level.CancelResidentPreparation();
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
        if (IsApplicationQuitting) return;
        if (!BasinQualitySampleMode && !_awaitingLayoutChoice && !CanWriteFullWorldSlot()) return;
        if (!TryState(PrologueGameFlowState.Intro, PrologueGameFlowState.Title, PrologueGameFlowState.Completed)) return;
        _entryVersion++;
        PrologueSaveData draft = NewSelectedSave();
        if (!BasinQualitySampleMode && SelectedProfile==null)
        {
            draft.BestHumanCompletionSeconds = Save.BestHumanCompletionSeconds;
            draft.BestHumanDeaths = Save.BestHumanDeaths;
        }
        var attempt = new StartupAttempt(_level.BeginResidentPreparation(), draft, WriteSlot: true,
            PreserveReplacedSlot: _awaitingLayoutChoice && LoadResult?.Status == PrologueSaveLoadStatus.Recovered);
        _startup = attempt;
        _level.SetGameplayActive(false);
        _completion.ResetSession();
        _minimap?.StartNewExploration();
        SetCheckpointSaveFailure(false);
        _level.StartNewGame(activateGameplay: false);
        BeginPlayingAfterResidents(attempt);
    }

    public void Continue()
    {
        if (IsApplicationQuitting) return;
        if (State != PrologueGameFlowState.Title || !HasSave)
        {
            Reject(PrologueGameFlowState.Playing);
            return;
        }
        State = PrologueGameFlowState.Intro;
        _entryVersion++;
        bool mappedChoice = _awaitingLayoutChoice && LoadResult?.Status == PrologueSaveLoadStatus.MigratableLegacy;
        var attempt = new StartupAttempt(_level.BeginResidentPreparation(), mappedChoice ? SelectedNormalize(Save) : Save,
            WriteSlot: mappedChoice, PreserveReplacedSlot: false);
        _startup = attempt;
        _level.SetGameplayActive(false);
        _completion.ResetSession();
        _minimap?.RestoreExploration(attempt.Draft.ExplorationMap);
        SetCheckpointSaveFailure(false);
        _level.RestoreFromSave(attempt.Draft, activateGameplay: false);
        if (!OwnsStartup(attempt)) return;
        if (BasinQualitySampleMode && _level.Progress.BossDefeated)
            _level.CurrentSectorRoot?.GetNodeOrNull<BossArenaController>("Gameplay/BossArenaController")
                ?.RestoreDefeatedEncounter();
        if (!BasinQualitySampleMode && SelectedProfile==null &&
            Enum.TryParse(attempt.Draft.Objective, ignoreCase: false, out PrologueObjective objective) &&
            Enum.IsDefined(objective)) _completion.RestoreSession(objective,
                completeMappedLegacyBossResult: mappedChoice && attempt.Draft.BossDefeated);
        BeginPlayingAfterResidents(attempt);
    }

    public void ReturnToTitle()
    {
        if (IsApplicationQuitting) return;
        if (State == PrologueGameFlowState.Recovering) return;
        _startup = null;
        _level.CancelResidentPreparation();
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
        if (IsApplicationQuitting) return;
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
        if (IsApplicationQuitting) return false;
        if (BasinQualitySampleMode || SelectedProfile!=null) return false;
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
            candidate = SelectedNormalize(candidate);
            SaveSelected(candidate);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { ShowCompletionSaveFailure(); return false; }

        // Only a durable commit may become a success notification/terminal state.
        Save = candidate;
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
        if (IsApplicationQuitting) return;
        if (State == PrologueGameFlowState.CompletionSaveFailed && !Input.IsActionPressed("ui_accept"))
            _completionRetryArmed = true;
    }

    private void RetryCompletionSave()
    {
        if (IsApplicationQuitting) return;
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
        if (IsApplicationQuitting) return FragmentCollectionResult.Unavailable;
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
            candidate = SelectedNormalize(candidate);
            // No live mutation, signal or await until the durable write commits.
            SaveSelected(candidate);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { return FragmentCollectionResult.SaveFailed; }

        if (!_level.Progress.TryCollectFragment(stableId))
            throw new InvalidOperationException("Committed fragment unexpectedly already exists in live progress.");
        Save = candidate;
        HasSave = IsContinueSaveAllowed();
        RefreshFragmentHud();
        return FragmentCollectionResult.Collected;
    }

    internal bool TryCommitCheckpoint(string stableId)
    {
        if (IsApplicationQuitting) return false;
        if (!IsInsideTree() || IsQueuedForDeletion() || !IsInstanceValid(_level) ||
            !_level.IsInsideTree() || _level.IsQueuedForDeletion() || !IsInstanceValid(_level.Player) ||
            !_level.Player.IsInsideTree() || _level.Player.IsQueuedForDeletion() || GetTree().Paused ||
            State != PrologueGameFlowState.Playing || _awaitingLayoutChoice || BasinQualitySampleMode ||
            _level.Player.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 }) return false;
        PrologueSectorId sector;Vector2 position;
        if(SelectedProfile!=null)
        {
            if(Save.LayoutVersion!=3 || !SelectedProfile.TryResolveCheckpoint(stableId,out sector,out position) || stableId==SelectedProfile.Start.StableId)return false;
        }
        else if(Save.LayoutVersion!=CanyonWorldLayout.CurrentLayoutVersion ||
            !CanyonWorldLayout.TryResolveCheckpoint(stableId,out sector,out position) || sector==PrologueSectorId.CanyonEntrance)return false;

        var old = _level.Progress.RestoreCheckpoint();
        if (old.HasActivatedCheckpoint && old.CheckpointId == stableId)
        { SetCheckpointSaveFailure(false); return true; }
        if(SelectedProfile!=null ? !SelectedProfile.CanAdvanceCheckpoint(old.CheckpointId,stableId) :
            old.HasActivatedCheckpoint && old.SectorId==PrologueSectorId.LostTemple && sector==PrologueSectorId.CanyonBasin)return false;

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
            candidate = SelectedNormalize(candidate);
            SaveSelected(candidate);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { SetCheckpointSaveFailure(true); return false; }

        // No await or public notification between the durable and live commits.
        if (!_level.Progress.ActivateCheckpoint(stableId))
            throw new InvalidOperationException("Committed checkpoint unexpectedly rejected by live progress.");
        Save = candidate;
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

    internal SupplyClaim BeginSupplyClaim(PrologueLevelController level, string discoveryId)
    {
        if (State != PrologueGameFlowState.Playing || IsApplicationQuitting || _startup != null ||
            !ReferenceEquals(_level, level) || string.IsNullOrWhiteSpace(discoveryId)) return null;
        return new SupplyClaim(level, Save, _entryVersion, SavePath, discoveryId);
    }

    internal bool OwnsSupplyClaim(SupplyClaim claim) => claim != null && !claim.Finished &&
        IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion() && !IsApplicationQuitting &&
        IsInstanceValid(_level) && _level.IsInsideTree() && !_level.IsQueuedForDeletion() &&
        ReferenceEquals(claim.Level, _level) && ReferenceEquals(claim.Save, Save) &&
        claim.EntryVersion == _entryVersion && string.Equals(claim.SavePath, SavePath, StringComparison.Ordinal) &&
        _startup == null && State != PrologueGameFlowState.Intro &&
        _level.Progress.Discoveries.Contains(claim.DiscoveryId);

    internal void FinishSupplyClaim(SupplyClaim claim)
    {
        if (!OwnsSupplyClaim(claim)) return;
        claim.Finished = true;
        if (State == PrologueGameFlowState.Title)
        {
            // A real Playing claim may finish across a title-only callback.
            // Any accepted Start/Continue, even one that later aborts, revokes it.
            bool hadSave = HasSave;
            if (!TryWriteTitleSupplyClaim()) return;
            if (HasSave != hadSave) _ui.UpdateContinueAvailability(HasSave);
        }
        // The existing gameplay listener saves normal claims. In Title it
        // still refuses generic writes, so this already committed claim is not saved twice.
        _level.EmitSignal(PrologueLevelController.SignalName.ProgressPersistRequested, "discovery");
    }

    private bool PersistProgressIfEligible(string reason)
    {
        if (IsApplicationQuitting) return false;
        if (State is PrologueGameFlowState.Title or PrologueGameFlowState.Intro or
            PrologueGameFlowState.CompletionSaveFailed) return false;
        return WriteAcceptedProgress();
    }

    private bool WriteAcceptedProgress()
    {
        if (!CanWriteAcceptedProgress()) return false;
        PrologueSaveData candidate = WriteProgressSnapshot(Save, preserveReplacedSlot: false);
        if (_minimap != null) Save.ExplorationMap = candidate.ExplorationMap;
        HasSave = IsContinueSaveAllowed();
        return true;
    }

    private bool CanWriteAcceptedProgress() => !_awaitingLayoutChoice &&
        (BasinQualitySampleMode || CanWriteFullWorldSlot());

    private bool TryWriteTitleSupplyClaim()
    {
        PrologueSaveData candidate;
        try
        {
            if (!CanWriteAcceptedProgress()) return RejectTitleSupplySave();
            candidate = WriteProgressSnapshot(SelectedNormalize(Save), preserveReplacedSlot: false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
        { return RejectTitleSupplySave(); }

        // No user callback between serialization and publication. Keep the
        // accepted object's identity, but publish none of these fields on failure.
        PopulateProgressSnapshot(Save);
        if (_minimap != null) Save.ExplorationMap = candidate.ExplorationMap;
        HasSave = IsContinueSaveAllowed();
        return true;
    }

    private bool RejectTitleSupplySave()
    {
        LastRejectedTransition = "supply-save: previous slot retained";
        if (_ui.GetNodeOrNull<Label>("MainMenu/Panel/Footer") is { } footer)
            footer.Text = Settings.Language == GameLanguage.ChineseSimplified
                ? (HasSave ? "补给领取未能保存；继续将使用上一次存档。" : "补给领取未能保存；没有可继续的存档。")
                : (HasSave ? "SUPPLY COULD NOT BE SAVED. CONTINUE WILL USE THE PREVIOUS SAVE."
                    : "SUPPLY COULD NOT BE SAVED. NO SAVE IS AVAILABLE TO CONTINUE.");
        return false;
    }

    private void PopulateProgressSnapshot(PrologueSaveData draft)
    {
        PrologueCheckpointSnapshot snapshot = _level.Progress.RestoreCheckpoint();
        draft.HasActivatedCheckpoint = snapshot.HasActivatedCheckpoint;
        draft.CheckpointSector = snapshot.SectorId.ToString();
        draft.CheckpointWorldX = snapshot.WorldPosition.X;
        draft.CheckpointWorldY = snapshot.WorldPosition.Y;
        draft.ShortcutOpen = snapshot.ShortcutOpen;
        if (!BasinQualitySampleMode)
        {
            draft.CheckpointId = snapshot.CheckpointId;
            draft.OpenShortcuts.Clear();
            foreach (string id in _level.Progress.OpenShortcuts) draft.OpenShortcuts.Add(id);
            draft.ActivatedMechanisms.Clear();
            foreach (string id in _level.Progress.ActivatedMechanisms) draft.ActivatedMechanisms.Add(id);
        }
        draft.Discoveries.Clear();
        foreach (string discovery in snapshot.Progress.Discoveries) draft.Discoveries.Add(discovery);
        draft.BossDefeated = snapshot.Progress.BossDefeated;
        draft.CollectionRuleVersion = snapshot.Progress.CollectionRuleVersion;
        draft.LegacyCollectionQuotaExempt = snapshot.Progress.LegacyCollectionQuotaExempt;
        draft.CollectedFragmentIds.Clear();
        foreach (string id in snapshot.Progress.CollectedFragmentIds) draft.CollectedFragmentIds.Add(id);
        draft.ClaimedRewardIds.Clear();
        foreach (string id in _level.Progress.ClaimedRewardIds) draft.ClaimedRewardIds.Add(id);
        draft.Objective = BasinQualitySampleMode || SelectedProfile!=null
            ? nameof(PrologueObjective.Wake) : _completion.CurrentObjective.ToString();
        if (BasinQualitySampleMode || SelectedProfile!=null)
        {
            draft.Completed = false;
            draft.BestHumanCompletionSeconds = -1d;
            draft.BestHumanDeaths = -1;
        }
        draft.CompletedTutorials.Clear();
        foreach (StringName tutorial in _level.Progress.CompletedTutorials)
            draft.CompletedTutorials.Add(tutorial.ToString());
    }

    private PrologueSaveData WriteProgressSnapshot(PrologueSaveData draft, bool preserveReplacedSlot)
    {
        PopulateProgressSnapshot(draft);
        // Preserve original core mutations/failure policy, but never commit an
        // unsaved map DTO into Save until its detached candidate actually lands.
        PrologueSaveData candidate = _minimap == null && SelectedProfile==null ? draft : SelectedNormalize(draft);
        if (_minimap != null) candidate.ExplorationMap = _minimap.CaptureExploration();
        SaveSelected(candidate, preserveReplacedSlot);
        return candidate;
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
        if (IsApplicationQuitting) return;
        _minimap = _level.GetNodeOrNull<PrologueMinimapController>("MinimapController");
        if(SelectedProfile!=null)
        {
            LoadResult=PrologueSaveStore.LoadCandidate(Absolute(SavePath),SelectedProfile,_minimap?.Catalog);
            Save=LoadResult.Data;_awaitingLayoutChoice=LoadResult.Status!=PrologueSaveLoadStatus.Current;
        }
        else if (BasinQualitySampleMode) Save = PrologueSaveStore.LoadOrNew(Absolute(SavePath), mapCatalog: _minimap?.Catalog);
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
        if (IsApplicationQuitting) return;
        State = PrologueGameFlowState.Playing;
        _ui.Visible = false;
        _level.SetGameplayActive(true);
        _audio.SetPaused(false);
        // RestoreSession resets HUD too, so refresh only after that operation.
        RefreshFragmentHud();
        EmitSignal(SignalName.SessionStarted);
    }

    private bool OwnsStartup(StartupAttempt attempt) => ReferenceEquals(_startup, attempt) &&
        IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion() && !IsApplicationQuitting &&
        State == PrologueGameFlowState.Intro && IsInstanceValid(_level) &&
        _level.HasResidentPreparationVersion(attempt.Version);

    private async void BeginPlayingAfterResidents(StartupAttempt attempt)
    {
        if (!OwnsStartup(attempt)) return;
        if (_level.ResidentPreparationPending)
        {
            bool ready;
            try { ready = await _level.PrepareResidents(attempt.Version); }
            catch (Exception error)
            {
                if (OwnsStartup(attempt)) AbortResidentPreparation(error.Message);
                return;
            }
            if (!OwnsStartup(attempt)) return;
            if (!ready || !_level.CommitResidentPreparation(attempt.Version))
            { AbortResidentPreparation(_level.ResidentPreparationFailure); return; }
        }
        if (!OwnsStartup(attempt)) return;
        if (attempt.WriteSlot)
        {
            try
            {
                if (!BasinQualitySampleMode && !CanWriteSelectedSlot(attempt.PreserveReplacedSlot))
                { AbortStartupSave("slot is not eligible for writing"); return; }
                Save = WriteProgressSnapshot(attempt.Draft, attempt.PreserveReplacedSlot);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or
                InvalidDataException or NotSupportedException or ArgumentException or System.Security.SecurityException)
            { AbortStartupSave(error.Message); return; }
        }
        _startup = null;
        AcceptLayoutChoice();
        HasSave = IsContinueSaveAllowed();
        BeginPlaying();
    }

    private void AbortStartupSave(string reason)
    {
        LastRejectedTransition = "startup-save: " + reason;
        ReturnToTitle();
        _ui.GetNode<Label>("MainMenu/Panel/Footer").Text = Settings.Language == GameLanguage.ChineseSimplified
            ? "无法保存本次游戏，请重试。" : "COULD NOT SAVE THIS SESSION. PLEASE RETRY.";
    }

    internal void AbortResidentPreparation(string reason)
    {
        LastRejectedTransition = "resident-preparation: " + reason;
        if (State == PrologueGameFlowState.Recovering) State = PrologueGameFlowState.Title;
        ReturnToTitle();
        _ui.GetNode<Label>("MainMenu/Panel/Footer").Text = Settings.Language == GameLanguage.ChineseSimplified
            ? "区域准备失败，请重试。" : "AREA PREPARATION FAILED. PLEASE RETRY.";
    }

    private void OnPaused()
    {
        if (!TryState(PrologueGameFlowState.Paused, PrologueGameFlowState.Playing)) return;
        _audio.SetPaused(true);
    }

    private void OnResumed()
    {
        if (IsApplicationQuitting) return;
        if (State != PrologueGameFlowState.Paused) return;
        State = PrologueGameFlowState.Playing;
        _audio.SetPaused(false);
    }

    internal bool AllowsRecoveryRequest()
    {
        if (IsApplicationQuitting) return false;
        if (_level.ResidentPreparationPending) return false;
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
        if (IsApplicationQuitting) return;
        if (State == PrologueGameFlowState.Recovering) State = PrologueGameFlowState.Playing;
        if (BasinQualitySampleMode || SelectedProfile!=null) _completion.RefreshDisplay();
    }

    private bool IsContinueSaveAllowed()
    {
        if (_awaitingLayoutChoice) return LoadResult?.Status == PrologueSaveLoadStatus.MigratableLegacy &&
            !Save.Completed;
        if (!File.Exists(Absolute(SavePath)) || Save.WasRecovered || Save.Completed) return false;
        if(SelectedProfile!=null)return Save.LayoutVersion==3 && Save.WorldProfileId==SelectedProfile.Id && Save.WorldProfileSaveRuleVersion==SelectedProfile.SaveRuleVersion;
        if (!BasinQualitySampleMode) return Save.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion;
        return Save.HasActivatedCheckpoint &&
            Save.BestHumanCompletionSeconds < 0d && Save.BestHumanDeaths < 0 &&
            Save.Objective == nameof(PrologueObjective.Wake) &&
            MatchesAuthoredSampleCheckpoint();
    }

    private bool CanWriteFullWorldSlot() => !_awaitingLayoutChoice && CanWriteSelectedSlot(preserveReplacedSlot: false);
    private bool CanWriteSelectedSlot(bool preserveReplacedSlot) => preserveReplacedSlot || !File.Exists(Absolute(SavePath)) ||
        !(SelectedProfile!=null ? PrologueSaveStore.LoadCandidate(Absolute(SavePath),SelectedProfile).Data :
            PrologueSaveStore.LoadOrNew(Absolute(SavePath), CanyonWorldLayout.CurrentLayoutVersion)).WasRecovered;

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
        if (IsApplicationQuitting) return false;
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

    // Scene owners must revoke deferred startup before releasing dependencies.
    // This is shared by the real quit path and owned-scene retirement, and does
    // not request a global SceneTree quit or perform an asynchronous drain.
    internal void BeginSceneRetirement()
    {
        IsApplicationQuitting = true;
        _startup = null;
        if (IsInstanceValid(_level))
        {
            _level.BeginApplicationQuit();
            _level.SetGameplayActive(false);
        }
        if (IsInstanceValid(_audio)) _audio.BeginApplicationQuit();
    }

    public Task<AudioQuitDrainResult> PrepareApplicationQuitAsync()
    {
        if (_applicationQuitPreparation != null) return _applicationQuitPreparation;
        var completion = new TaskCompletionSource<AudioQuitDrainResult>();
        _applicationQuitPreparation = completion.Task;
        IsApplicationQuitting = true;
        ulong generation = _applicationQuitRequestGeneration = ++_applicationQuitGeneration;
        try
        {
            if (!IsInstanceValid(this) || !IsInsideTree() || !IsInstanceValid(_audio) || !IsInstanceValid(_level))
                completion.TrySetResult(new(AudioQuitDrainState.Cancelled, 0, 0, false));
            else
            {
                BeginSceneRetirement();
                _ = FinishApplicationQuitPreparation(completion, generation);
            }
        }
        catch (Exception error) { completion.TrySetException(error); }
        return _applicationQuitPreparation;
    }

    private async Task FinishApplicationQuitPreparation(TaskCompletionSource<AudioQuitDrainResult> completion, ulong generation)
    {
        try
        {
            AudioQuitDrainResult result = await _audio.PrepareApplicationQuitAsync();
            if (!IsInstanceValid(this) || !IsInsideTree() || generation != _applicationQuitGeneration ||
                _applicationExitRequested && (GetTree().CurrentScene != _level || _level.GetParent() != GetTree().Root))
                result = result with { State = AudioQuitDrainState.Cancelled };
            completion.TrySetResult(result);
        }
        catch (Exception error) { completion.TrySetException(error); }
    }

    public void RequestApplicationQuit() => OnQuit();

    private async void OnQuit()
    {
        if (!IsInstanceValid(this) || !IsInsideTree() || GetTree().CurrentScene != _level ||
            _level.GetParent() != GetTree().Root) return;
        if (_applicationExitRequested) return;
        _applicationExitRequested = true;
        SceneTree applicationTree = GetTree();
        Task<AudioQuitDrainResult> preparation = PrepareApplicationQuitAsync();
        ulong generation = _applicationQuitRequestGeneration;
        try
        {
            AudioQuitDrainResult result = await preparation;
            if (!OwnsApplicationQuit(applicationTree, generation) || result.State == AudioQuitDrainState.Cancelled) return;
            if (result.State != AudioQuitDrainState.Ready)
            {
                GD.PushError($"Application quit audio drain failed: {result}");
                applicationTree.Quit(1);
                return;
            }
            GD.Print($"APPLICATION_QUIT_PREPARED elapsed_ms={result.ElapsedMilliseconds} process_observations={result.ObservedProcessFrames} mix_observed={result.MixObserved}");
            applicationTree.Quit(0);
        }
        catch (Exception error)
        {
            if (!OwnsApplicationQuit(applicationTree, generation))
            {
                GD.Print($"APPLICATION_QUIT_RETIRED_FAILURE type={error.GetType().Name} global_quit=false");
                return;
            }
            GD.PushError("Application quit preparation failed: " + error);
            applicationTree.Quit(1);
        }
    }

    private bool OwnsApplicationQuit(SceneTree tree, ulong generation) => IsInstanceValid(this) &&
        IsInsideTree() && IsInstanceValid(tree) && GetTree() == tree && generation == _applicationQuitGeneration &&
        tree.CurrentScene == _level && IsInstanceValid(_level) && _level.GetParent() == tree.Root;

    private void BindApplicationWindowQuit()
    {
        if (!IsInstanceValid(this) || !IsInsideTree() || IsApplicationQuitting || _applicationWindow != null) return;
        SceneTree tree = GetTree();
        if (tree.CurrentScene != _level || _level.GetParent() != tree.Root) return;
        _applicationWindowTree = tree;
        _applicationWindow = tree.Root;
        if (!_applicationWindow.HasMeta(ApplicationQuitWindowOwnerKey))
            _applicationWindow.SetMeta(ApplicationQuitWindowBaselineKey, tree.AutoAcceptQuit);
        _previousAutoAcceptQuit = (bool)_applicationWindow.GetMeta(ApplicationQuitWindowBaselineKey);
        _applicationWindow.SetMeta(ApplicationQuitWindowOwnerKey, (long)GetInstanceId());
        tree.AutoAcceptQuit = false;
        _applicationWindow.CloseRequested += OnQuit;
    }

    private void ReleaseApplicationWindowQuit()
    {
        if (IsInstanceValid(_applicationWindow)) _applicationWindow.CloseRequested -= OnQuit;
        if (IsInstanceValid(_applicationWindow) && IsInstanceValid(_applicationWindowTree) &&
            _applicationWindow.HasMeta(ApplicationQuitWindowOwnerKey) &&
            (long)_applicationWindow.GetMeta(ApplicationQuitWindowOwnerKey) == (long)GetInstanceId())
        {
            _applicationWindowTree.AutoAcceptQuit = _previousAutoAcceptQuit;
            _applicationWindow.RemoveMeta(ApplicationQuitWindowOwnerKey);
            _applicationWindow.RemoveMeta(ApplicationQuitWindowBaselineKey);
        }
        _applicationWindow = null;
        _applicationWindowTree = null;
    }

    private static string Absolute(string path) => path.StartsWith("user://", StringComparison.Ordinal)
        ? ProjectSettings.GlobalizePath(path) : Path.GetFullPath(path);
}
