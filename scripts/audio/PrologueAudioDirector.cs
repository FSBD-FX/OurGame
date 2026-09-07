using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Settings;
using Knight.Player;
using Knight.Ui;

namespace Knight.Audio;

public partial class PrologueAudioDirector : Node
{
    public const string AudioGroup = "prologue_audio";
    public const string ManifestPath = "res://assets/audio/prologue/audio-manifest.json";

    public PrologueAudioCue LastCue { get; private set; }
    public string LastPlayedPath { get; private set; } = string.Empty;
    public Vector2 LastWorldPosition { get; private set; }
    public PrologueMusicState MusicState { get; private set; } = PrologueMusicState.Silent;
    public int ActiveMusicLayerCount { get; private set; }
    public bool AudioPaused { get; private set; }
    public int FramePlaybackCount { get; private set; }
    public bool LastFrameDeduplicated { get; private set; }
    public string LastFrameAudit { get; private set; } = string.Empty;
    public int Task10TransitionCueCount { get; private set; }
    public IReadOnlyList<PrologueAudioCue> Task10CueHistory => _task10CueHistory;
    public int ActiveTask10TransitionCount =>
        (_hasTask10ClawFrame && _lastTask10Claw.State != MechanicalClawState.Idle ? 1 : 0) +
        (_hasTask10EnergyFrame && _lastTask10EnergyFrame.State.Overheated ? 1 : 0) +
        (_hasTask10ShieldFrame && _lastTask10ShieldActive ? 1 : 0) +
        _task10ShieldResults.Count;
    [Export] public bool PlaybackEnabled { get; set; } = true;
    public event Action<PrologueAudioCue, ulong, Vector2> Task10CueEmitted;
    public event Action Task10TransitionsCleared;

    private PrologueAudioManifest _manifest;
    private AudioStreamPlayer _musicA;
    private AudioStreamPlayer _musicB;
    private AudioStreamPlayer _ambience;
    private AudioStreamPlayer _ui;
    private readonly List<AudioStreamPlayer> _sfx = new();
    private readonly Dictionary<PrologueAudioCue, int> _variantIndices = new();
    private readonly List<Tween> _tweens = new();
    private readonly HashSet<AttackPhaseIdentity> _playedAttackPhases = new();
    private readonly HashSet<(ulong CastId, ThreadCastState State)> _playedThreadStates = new();
    private LegacyCompatibilityToken? _legacyCompatibilityToken;
    private int _sfxIndex;
    private bool _hasTask10ActionFrame;
    private PlayerHudActionState _lastTask10ActionFrame;
    private bool _hasTask10ClawFrame;
    private MechanicalClawFrame _lastTask10Claw;
    private PrologueAudioCue? _lastTask10ClawCue;
    private bool _hasTask10EnergyFrame;
    private PlayerEnergyFrame _lastTask10EnergyFrame;
    private bool _hasTask10ShieldFrame;
    private bool _lastTask10ShieldActive;
    private readonly HashSet<ulong> _task10ShieldResults = new();
    private readonly List<PrologueAudioCue> _task10CueHistory = new();
    private ulong _task10CueSequence;
    private PrologueLevelController _task10Level;
    private PrologueCompletionController _task10Completion;
    private PrologueGameFlowController _task10Flow;
    private PlayerActionModeController _task10ActionModes;
    private PlayerEnergyController _task10Energy;
    private PlayerShieldController _task10Shield;
    private MechanicalClawController _task10Claw;
    private Prologue.Seamless.PrologueSectorId? _task10Sector;
    private PrologueGameFlowState? _task10FlowState;

    private readonly record struct AttackPhaseIdentity(
        ulong SourceInstanceId, ulong AttackId, AttackPhase Phase);

    private readonly record struct LegacyCompatibilityToken(
        Node Source,
        ulong SourceInstanceId,
        ulong AttackId,
        AttackPhase Phase,
        PresentationCue ExpectedCue);

    public override void _EnterTree() => AddToGroup(AudioGroup);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ResolvePlayers();
        _manifest = LoadManifest();
        if (PlaybackEnabled) SetMusicState(PrologueMusicState.Exploration);
        RefreshTask10PublisherBindings();
    }

    public override void _Process(double delta)
    {
        RefreshTask10PublisherBindings();
        if (!GodotObject.IsInstanceValid(_task10Level)) return;
        Prologue.Seamless.PrologueSectorId? current = _task10Level.CurrentSector;
        if (_task10Sector.HasValue && current.HasValue && _task10Sector != current)
            ClearTask10Transitions();
        _task10Sector = current;
        PrologueGameFlowState? flowState = GodotObject.IsInstanceValid(_task10Flow)
            ? _task10Flow.State : null;
        if (_task10FlowState.HasValue && flowState == PrologueGameFlowState.Completed &&
            _task10FlowState != flowState) ClearTask10Transitions();
        _task10FlowState = flowState;
    }

    public override void _ExitTree() => Shutdown();

    public void Shutdown()
    {
        UnbindTask10PublisherBindings();
        foreach (Tween tween in _tweens)
            if (tween.IsValid()) tween.Kill();
        _tweens.Clear();
        ResolvePlayers();
        foreach (AudioStreamPlayer player in AllPlayers())
        {
            player.Stop();
            player.Stream = null;
        }
        _playedAttackPhases.Clear();
        _playedThreadStates.Clear();
        _legacyCompatibilityToken = null;
        ClearTask10Transitions();
    }

    public static PrologueAudioDirector CreateRuntimeTree()
    {
        var director = new PrologueAudioDirector { Name = "Audio", ProcessMode = ProcessModeEnum.Always };
        director.AddChild(Player("MusicA", "Music"));
        director.AddChild(Player("MusicB", "Music"));
        director.AddChild(Player("Ambience", "Ambience"));
        for (int index = 0; index < 4; index++) director.AddChild(Player($"Sfx{index}", "SFX"));
        director.AddChild(Player("UI", "UI"));
        return director;
    }

    public static string BusForCue(PrologueAudioCue cue) => IsUiCue(cue) ? "UI" : "SFX";

    public void Play(PrologueAudioCue cue, Vector2 worldPosition)
    {
        if (!PlaybackEnabled) return;
        EnsureReady();
        if (!_manifest.Cues.TryGetValue(cue.ToString(), out List<string> variants) || variants.Count == 0)
        {
            GD.PushWarning($"No audio variants declared for {cue}.");
            return;
        }
        int next = _variantIndices.TryGetValue(cue, out int prior) ? (prior + 1) % variants.Count : 0;
        _variantIndices[cue] = next;
        string path = variants[next];
        AudioStreamPlayer player = IsUiCue(cue) ? _ui : _sfx[_sfxIndex++ % _sfx.Count];
        player.Stream = GD.Load<AudioStream>(path);
        player.Play();
        LastCue = cue;
        LastPlayedPath = path;
        LastWorldPosition = worldPosition;
    }

    public void PlayAttackFrame(Node source, AttackPresentationFrame frame, Vector2 position)
    {
        if (frame.Timeline.Phase == AttackPhase.None)
        {
            ReleaseAttackSource(source);
            return;
        }
        frame = frame.Validate();
        ulong sourceInstanceId = SourceInstanceId(source);
        var key = new AttackPhaseIdentity(sourceInstanceId,
            frame.Timeline.AttackId, frame.Timeline.Phase);
        PrologueAudioCue? cue = MapAttackFrame(source, frame);
        bool duplicate = !_playedAttackPhases.Add(key);
        LastFrameDeduplicated = duplicate;
        if (!cue.HasValue)
        {
            LastFrameAudit = $"Source={key.SourceInstanceId} AttackId={key.AttackId} " +
                             $"Phase={key.Phase} cue=None dedupe={duplicate} path=None";
            return;
        }
        if (!duplicate)
        {
            Play(cue.Value, position);
            FramePlaybackCount++;
            _legacyCompatibilityToken = CompatibilityToken(source, key);
        }
        LastFrameAudit = $"Source={key.SourceInstanceId} AttackId={key.AttackId} Phase={key.Phase} " +
                         $"cue={cue.Value} dedupe={duplicate} path=" +
                         (duplicate ? "Duplicate" : LastPlayedPath);
    }

    public void ReleaseAttackSource(Node source)
    {
        if (!GodotObject.IsInstanceValid(source)) return;
        ulong sourceInstanceId = source.GetInstanceId();
        _playedAttackPhases.RemoveWhere(key => key.SourceInstanceId == sourceInstanceId);
        if (_legacyCompatibilityToken is { } token && token.SourceInstanceId == sourceInstanceId)
            _legacyCompatibilityToken = null;
    }

    public void PlayThreadFrame(ThreadCastFrame frame, Vector2 position)
    {
        if (frame.State == ThreadCastState.Idle)
        {
            _playedThreadStates.Clear();
            return;
        }
        if (frame.State == ThreadCastState.Failed && frame.Failure == ThreadFailureReason.Cancelled)
        {
            StopTransientFrames();
            _playedThreadStates.Clear();
            LastFrameDeduplicated = false;
            LastFrameAudit = $"CastId={frame.CastId} State={frame.State} cue=None dedupe=False path=None";
            return;
        }

        (ulong CastId, ThreadCastState State) key = (frame.CastId, frame.State);
        PrologueAudioCue? cue = MapThreadFrame(frame);
        bool duplicate = !_playedThreadStates.Add(key);
        LastFrameDeduplicated = duplicate;
        if (!cue.HasValue)
        {
            LastFrameAudit = $"CastId={key.CastId} State={key.State} cue=None dedupe={duplicate} path=None";
            return;
        }
        if (!duplicate)
        {
            Play(cue.Value, position);
            FramePlaybackCount++;
        }
        LastFrameAudit = $"CastId={key.CastId} State={key.State} cue={cue.Value} " +
                         $"dedupe={duplicate} path={LastPlayedPath}";
    }

    public void SetMusicState(PrologueMusicState state)
    {
        if (!PlaybackEnabled)
        {
            MusicState = state;
            ActiveMusicLayerCount = 0;
            return;
        }
        EnsureReady();
        MusicState = state;
        List<string> layers = _manifest.Music.TryGetValue(state.ToString(), out List<string> value) ? value : new();
        ActiveMusicLayerCount = Math.Min(2, layers.Count);
        switch (state)
        {
            case PrologueMusicState.Exploration:
                TransitionLoop(_musicA, layers.Count > 0 ? layers[0] : null);
                FadeOut(_musicB);
                break;
            case PrologueMusicState.BossPhaseOne:
                FadeOut(_musicA);
                TransitionLoop(_musicB, layers.Count > 0 ? layers[0] : null);
                break;
            case PrologueMusicState.BossPhaseTwo:
                TransitionLoop(_musicB, layers.Count > 0 ? layers[0] : null);
                TransitionLoop(_musicA, layers.Count > 1 ? layers[1] : null);
                break;
            default:
                FadeOut(_musicA);
                FadeOut(_musicB);
                break;
        }
    }

    public void SetRoomAmbience(PrologueRoomId room)
    {
        if (!PlaybackEnabled) return;
        EnsureReady();
        if (!_manifest.Ambience.TryGetValue(room.ToString(), out string path)) return;
        SetLoop(_ambience, path, true);
    }

    public void ApplySettings(GameSettingsData settings)
    {
        GameSettingsData normalized = (settings ?? new GameSettingsData()).Normalize();
        SetBus("Master", normalized.MasterVolume);
        SetBus("Music", normalized.MusicVolume);
        SetBus("SFX", normalized.EffectsVolume);
        SetBus("Ambience", normalized.AmbienceVolume);
        SetBus("UI", normalized.UiVolume);
    }

    public void SetPaused(bool paused)
    {
        ResolvePlayers();
        AudioPaused = paused;
        if (paused) ClearStagedFrameAudio();
        foreach (AudioStreamPlayer player in AllPlayers()) player.StreamPaused = paused;
    }

    public static void PlayPresentationCueFromTree(Node source, PresentationCue cue, Vector2 position)
    {
        if (source == null || !source.IsInsideTree()) return;
        if (source.GetTree()?.GetFirstNodeInGroup(AudioGroup) is not PrologueAudioDirector director) return;
        if (director.IsTypedCompatibilityDuplicate(source, cue)) return;
        if (cue == PresentationCue.RecoveryStarted) director.ClearStagedFrameAudio();
        PrologueAudioCue mapped = MapCue(source, cue);
        director.Play(mapped, position);
        if (cue is PresentationCue.BossTransition or PresentationCue.PhaseChanged)
            director.SetMusicState(PrologueMusicState.BossPhaseTwo);
        else if (cue is PresentationCue.BossDefeated)
            director.SetMusicState(PrologueMusicState.Complete);
    }

    public static void ConfigureRoomFromTree(Node source, PrologueRoomId room)
    {
        if (source?.GetTree()?.GetFirstNodeInGroup(AudioGroup) is not PrologueAudioDirector director) return;
        director.ClearStagedFrameAudio();
        director.SetRoomAmbience(room);
        director.SetMusicState(room == PrologueRoomId.FallenBellkeeperArena
            ? PrologueMusicState.BossPhaseOne : PrologueMusicState.Exploration);
        if (room == PrologueRoomId.FallenBellkeeperArena) director.Play(PrologueAudioCue.BossIntro, Vector2.Zero);
    }

    public static void PlayUiCueFromTree(Node source, PrologueAudioCue cue)
    {
        if (source?.GetTree()?.GetFirstNodeInGroup(AudioGroup) is PrologueAudioDirector director)
            director.Play(cue, Vector2.Zero);
    }

    private static PrologueAudioCue MapCue(Node source, PresentationCue cue)
    {
        bool boss = HasAncestor<FallenBellkeeper>(source);
        bool laborer = HasAncestor<BellBackLaborer>(source);
        bool moth = HasAncestor<GraveLampMoth>(source);
        bool rust = HasAncestor<RustShellSquire>(source);
        return cue switch
        {
            PresentationCue.AttackStarted when boss => PrologueAudioCue.BossAttack,
            PresentationCue.AttackStarted when laborer => PrologueAudioCue.LaborerAttack,
            PresentationCue.AttackStarted when moth => PrologueAudioCue.MothDive,
            PresentationCue.AttackStarted when rust => PrologueAudioCue.RustShellAttack,
            PresentationCue.AttackStarted => PrologueAudioCue.PlayerAttack,
            PresentationCue.AttackHit => PrologueAudioCue.PlayerHit,
            PresentationCue.Hurt when boss => PrologueAudioCue.BossHammer,
            PresentationCue.Hurt when laborer => PrologueAudioCue.LaborerHurt,
            PresentationCue.Hurt when moth => PrologueAudioCue.MothHurt,
            PresentationCue.Hurt when rust => PrologueAudioCue.RustShellHurt,
            PresentationCue.Hurt => PrologueAudioCue.PlayerHurt,
            PresentationCue.Guard => laborer ? PrologueAudioCue.LaborerGuard : PrologueAudioCue.BossHammer,
            PresentationCue.Dash => PrologueAudioCue.PlayerDash,
            PresentationCue.Downstrike => PrologueAudioCue.PlayerDownstrike,
            PresentationCue.ThreadFired => PrologueAudioCue.ThreadFire,
            PresentationCue.ThreadHit => PrologueAudioCue.ThreadHit,
            PresentationCue.ThreadFailed => PrologueAudioCue.ThreadFail,
            PresentationCue.Died when boss => PrologueAudioCue.BossDefeat,
            PresentationCue.Died when laborer => PrologueAudioCue.LaborerDeath,
            PresentationCue.Died when moth => PrologueAudioCue.MothDeath,
            PresentationCue.Died when rust => PrologueAudioCue.RustShellDeath,
            PresentationCue.Died => PrologueAudioCue.PlayerHurt,
            PresentationCue.CheckpointActivated => PrologueAudioCue.Checkpoint,
            PresentationCue.ShortcutOpened => PrologueAudioCue.Shortcut,
            PresentationCue.BossTransition or PresentationCue.PhaseChanged => PrologueAudioCue.BossTransition,
            PresentationCue.BossExposed or PresentationCue.WeakpointOpened => PrologueAudioCue.BossExposed,
            PresentationCue.BossDefeated => PrologueAudioCue.BossDefeat,
            PresentationCue.RecoveryStarted => PrologueAudioCue.RecoveryStart,
            PresentationCue.RecoveryCompleted => PrologueAudioCue.RecoveryEnd,
            _ => PrologueAudioCue.PlayerHit
        };
    }

    private static PrologueAudioCue? MapAttackFrame(Node source, AttackPresentationFrame frame)
    {
        EnemyAttackKind kind = frame.Timeline.Kind;
        return frame.Timeline.Phase switch
        {
            AttackPhase.Telegraph => kind switch
            {
                EnemyAttackKind.RustShellThrust => PrologueAudioCue.RustShellTelegraph,
                EnemyAttackKind.MothDive => PrologueAudioCue.MothTelegraph,
                EnemyAttackKind.LaborerSweep => PrologueAudioCue.LaborerTelegraph,
                EnemyAttackKind.BossSweep => PrologueAudioCue.BossSweepTelegraph,
                EnemyAttackKind.BossLeapSlam => PrologueAudioCue.BossLeapTelegraph,
                EnemyAttackKind.BossGroundWave => PrologueAudioCue.BossGroundWaveTelegraph,
                EnemyAttackKind.BossHammerThrow => PrologueAudioCue.BossHammerTelegraph,
                _ => null
            },
            AttackPhase.Active => kind switch
            {
                EnemyAttackKind.RustShellThrust => PrologueAudioCue.RustShellAttack,
                EnemyAttackKind.MothDive => PrologueAudioCue.MothDive,
                EnemyAttackKind.LaborerSweep => PrologueAudioCue.LaborerAttack,
                EnemyAttackKind.BossHammerThrow => PrologueAudioCue.BossHammer,
                EnemyAttackKind.BossSweep or EnemyAttackKind.BossLeapSlam or EnemyAttackKind.BossGroundWave =>
                    PrologueAudioCue.BossAttack,
                _ => null
            },
            AttackPhase.Recovery => kind switch
            {
                EnemyAttackKind.RustShellThrust => PrologueAudioCue.RustShellRecovery,
                EnemyAttackKind.MothDive => PrologueAudioCue.MothRecovery,
                EnemyAttackKind.LaborerSweep => PrologueAudioCue.LaborerRecovery,
                EnemyAttackKind.BossSweep or EnemyAttackKind.BossLeapSlam or EnemyAttackKind.BossGroundWave or
                    EnemyAttackKind.BossHammerThrow => PrologueAudioCue.BossRecovery,
                _ => null
            },
            _ => null
        };
    }

    private static PrologueAudioCue? MapThreadFrame(ThreadCastFrame frame) => frame.State switch
    {
        ThreadCastState.Preview => PrologueAudioCue.ThreadPreview,
        ThreadCastState.Windup => PrologueAudioCue.ThreadWindup,
        ThreadCastState.Travel => PrologueAudioCue.ThreadTravel,
        ThreadCastState.Latched => PrologueAudioCue.ThreadLatch,
        ThreadCastState.Apply => PrologueAudioCue.ThreadTension,
        ThreadCastState.Recovery => PrologueAudioCue.ThreadHit,
        ThreadCastState.Failed => PrologueAudioCue.ThreadFail,
        _ => null
    };

    private bool IsTypedCompatibilityDuplicate(Node legacySource, PresentationCue cue)
    {
        LegacyCompatibilityToken? pending = _legacyCompatibilityToken;
        _legacyCompatibilityToken = null;
        if (pending is not { } token || token.ExpectedCue != cue ||
            !GodotObject.IsInstanceValid(token.Source) || !token.Source.IsInsideTree()) return false;
        Node resolvedSource = ResolveAttackSource(legacySource);
        return GodotObject.IsInstanceValid(resolvedSource) &&
               resolvedSource.GetInstanceId() == token.SourceInstanceId;
    }

    private static LegacyCompatibilityToken? CompatibilityToken(Node source, AttackPhaseIdentity key)
    {
        PresentationCue expected = key.Phase switch
        {
            AttackPhase.Telegraph => PresentationCue.AttackTelegraph,
            AttackPhase.Active => PresentationCue.AttackStarted,
            _ => default
        };
        return key.Phase is AttackPhase.Telegraph or AttackPhase.Active
            ? new LegacyCompatibilityToken(source, key.SourceInstanceId, key.AttackId, key.Phase, expected)
            : null;
    }

    public void ClearStagedFrameAudio()
    {
        StopTransientFrames();
        _playedAttackPhases.Clear();
        _playedThreadStates.Clear();
        _legacyCompatibilityToken = null;
        ClearTask10Transitions();
    }

    public void PresentActionFrame(PlayerHudActionState frame, Vector2 position)
    {
        PrologueAudioCue? cue = null;
        if (!_hasTask10ActionFrame || frame.Mode != _lastTask10ActionFrame.Mode)
            cue = frame.Mode == PlayerActionMode.Attack
                ? PrologueAudioCue.ModeAttack : PrologueAudioCue.ModeGrapple;
        else if (frame.Rejection == PlayerActionRejectionReason.EmptySlot &&
                 _lastTask10ActionFrame.Rejection != PlayerActionRejectionReason.EmptySlot)
            cue = PrologueAudioCue.EmptySlot;
        else if (frame.Rejection == PlayerActionRejectionReason.InsufficientEnergy &&
                 _lastTask10ActionFrame.Rejection != PlayerActionRejectionReason.InsufficientEnergy)
            cue = PrologueAudioCue.EnergyEmpty;
        _lastTask10ActionFrame = frame;
        _hasTask10ActionFrame = true;
        if (cue.HasValue) EmitTask10(cue.Value, position);
    }

    public static PrologueAudioCue? CueForClawFrame(MechanicalClawFrame frame) => frame.State switch
    {
        MechanicalClawState.Windup => PrologueAudioCue.ClawFire,
        MechanicalClawState.Travel => PrologueAudioCue.ClawTravel,
        MechanicalClawState.Latched => PrologueAudioCue.ClawLatch,
        MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost =>
            PrologueAudioCue.ClawTension,
        MechanicalClawState.Released or MechanicalClawState.Retract => PrologueAudioCue.ClawRelease,
        MechanicalClawState.Failed when frame.Failure == MechanicalClawFailure.IceSlip =>
            PrologueAudioCue.ClawIceSlip,
        MechanicalClawState.Failed when frame.Failure == MechanicalClawFailure.Forbidden =>
            PrologueAudioCue.ClawFail,
        MechanicalClawState.Failed when frame.Failure == MechanicalClawFailure.Obstructed =>
            PrologueAudioCue.ThreadFail,
        MechanicalClawState.Failed when frame.Failure is MechanicalClawFailure.OutOfRange or
            MechanicalClawFailure.NoTarget => PrologueAudioCue.ThreadPreview,
        MechanicalClawState.Failed => PrologueAudioCue.ClawFail,
        _ => null
    };

    public void PresentClawFrame(MechanicalClawFrame frame, Vector2 position)
    {
        if (_hasTask10ClawFrame && frame.CastId == _lastTask10Claw.CastId &&
            frame.State == _lastTask10Claw.State && frame.Failure == _lastTask10Claw.Failure) return;
        PrologueAudioCue? cue = CueForClawFrame(frame);
        bool sameCueFamily = _hasTask10ClawFrame && frame.CastId == _lastTask10Claw.CastId &&
            cue.HasValue && cue == _lastTask10ClawCue;
        _lastTask10Claw = frame;
        _lastTask10ClawCue = cue;
        _hasTask10ClawFrame = true;
        if (sameCueFamily) return;
        if (cue.HasValue) EmitTask10(cue.Value, position);
    }

    public void PresentEnergyFrame(PlayerEnergyFrame frame, Vector2 position)
    {
        PrologueAudioCue? cue = null;
        if (_hasTask10EnergyFrame && frame.State.Overheated != _lastTask10EnergyFrame.State.Overheated)
            cue = frame.State.Overheated ? PrologueAudioCue.ShieldOverheat : PrologueAudioCue.ShieldReady;
        _lastTask10EnergyFrame = frame;
        _hasTask10EnergyFrame = true;
        if (cue.HasValue) EmitTask10(cue.Value, position);
    }

    public void PresentShieldActive(bool active, Vector2 position)
    {
        if (_hasTask10ShieldFrame && active == _lastTask10ShieldActive) return;
        bool entered = active && (!_hasTask10ShieldFrame || !_lastTask10ShieldActive);
        _lastTask10ShieldActive = active;
        _hasTask10ShieldFrame = true;
        if (entered) EmitTask10(PrologueAudioCue.ShieldRaise, position);
    }

    public void PresentShieldResult(PlayerShieldResult result, ulong transitionId, Vector2 position)
    {
        if (!result.Guarded && !result.BrokeGuard || !_task10ShieldResults.Add(transitionId)) return;
        EmitTask10(result.BrokeGuard ? PrologueAudioCue.ShieldHeavyBreak : PrologueAudioCue.ShieldBlock,
            position);
    }

    public void ClearTask10Transitions()
    {
        _hasTask10ActionFrame = false;
        _lastTask10ActionFrame = default;
        _hasTask10ClawFrame = false;
        _lastTask10Claw = default;
        _lastTask10ClawCue = null;
        _hasTask10EnergyFrame = false;
        _lastTask10EnergyFrame = default;
        _hasTask10ShieldFrame = false;
        _lastTask10ShieldActive = false;
        _task10ShieldResults.Clear();
        Task10TransitionsCleared?.Invoke();
    }

    private void EmitTask10(PrologueAudioCue cue, Vector2 position)
    {
        Task10TransitionCueCount++;
        _task10CueHistory.Add(cue);
        LastCue = cue;
        Play(cue, position);
        Task10CueEmitted?.Invoke(cue, ++_task10CueSequence, position);
    }

    private void RefreshTask10PublisherBindings()
    {
        PrologueLevelController level = GetParentOrNull<PrologueLevelController>();
        CharacterBody2D player = GodotObject.IsInstanceValid(level?.Player)
            ? level.Player
            : GetNodeOrNull<CharacterBody2D>("../PlayerRoot/Player");
        PrologueCompletionController completion = level?.GetNodeOrNull<PrologueCompletionController>("Completion");
        PrologueGameFlowController flow = level?.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        PlayerActionModeController actionModes = player?.GetNodeOrNull<PlayerActionModeController>("ActionModes");
        PlayerEnergyController energy = player?.GetNodeOrNull<PlayerEnergyController>("Energy");
        PlayerShieldController shield = player?.GetNodeOrNull<PlayerShieldController>("Shield");
        MechanicalClawController claw = player?.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        if (ReferenceEquals(level, _task10Level) && ReferenceEquals(completion, _task10Completion) &&
            ReferenceEquals(flow, _task10Flow) &&
            ReferenceEquals(actionModes, _task10ActionModes) && ReferenceEquals(energy, _task10Energy) &&
            ReferenceEquals(shield, _task10Shield) && ReferenceEquals(claw, _task10Claw))
        {
            SeedTask10ActionBaseline();
            return;
        }

        UnbindTask10PublisherBindings();
        _task10Level = level;
        _task10Completion = completion;
        _task10Flow = flow;
        _task10ActionModes = actionModes;
        _task10Energy = energy;
        _task10Shield = shield;
        _task10Claw = claw;
        _task10Sector = level?.CurrentSector;
        _task10FlowState = flow?.State;
        if (GodotObject.IsInstanceValid(_task10Level))
        {
            _task10Level.RecoveryRequested += OnTask10RecoveryRequested;
            _task10Level.RecoveryCompleted += OnTask10RecoveryCompleted;
        }
        if (GodotObject.IsInstanceValid(_task10Completion))
            _task10Completion.PrologueCompleted += OnTask10TerminalState;
        if (GodotObject.IsInstanceValid(_task10ActionModes))
        {
            _task10ActionModes.ModeChanged += OnTask10ModeChanged;
            _task10ActionModes.LoadoutChanged += OnTask10LoadoutChanged;
            _task10ActionModes.RejectionChanged += OnTask10RejectionChanged;
        }
        if (GodotObject.IsInstanceValid(_task10Energy))
            _task10Energy.EnergyFrameChanged += OnTask10EnergyFrameChanged;
        if (GodotObject.IsInstanceValid(_task10Shield))
        {
            _task10Shield.ShieldFrameChanged += OnTask10ShieldFrameChanged;
            _task10Shield.ShieldResolved += OnTask10ShieldResolved;
        }
        if (GodotObject.IsInstanceValid(_task10Claw))
            _task10Claw.ClawFrameChanged += OnTask10ClawFrameChanged;
        SeedTask10ActionBaseline();
    }

    private void UnbindTask10PublisherBindings()
    {
        if (GodotObject.IsInstanceValid(_task10Level))
        {
            _task10Level.RecoveryRequested -= OnTask10RecoveryRequested;
            _task10Level.RecoveryCompleted -= OnTask10RecoveryCompleted;
        }
        if (GodotObject.IsInstanceValid(_task10Completion))
            _task10Completion.PrologueCompleted -= OnTask10TerminalState;
        if (GodotObject.IsInstanceValid(_task10ActionModes))
        {
            _task10ActionModes.ModeChanged -= OnTask10ModeChanged;
            _task10ActionModes.LoadoutChanged -= OnTask10LoadoutChanged;
            _task10ActionModes.RejectionChanged -= OnTask10RejectionChanged;
        }
        if (GodotObject.IsInstanceValid(_task10Energy))
            _task10Energy.EnergyFrameChanged -= OnTask10EnergyFrameChanged;
        if (GodotObject.IsInstanceValid(_task10Shield))
        {
            _task10Shield.ShieldFrameChanged -= OnTask10ShieldFrameChanged;
            _task10Shield.ShieldResolved -= OnTask10ShieldResolved;
        }
        if (GodotObject.IsInstanceValid(_task10Claw))
            _task10Claw.ClawFrameChanged -= OnTask10ClawFrameChanged;
        _task10Level = null;
        _task10Completion = null;
        _task10Flow = null;
        _task10ActionModes = null;
        _task10Energy = null;
        _task10Shield = null;
        _task10Claw = null;
        _task10Sector = null;
        _task10FlowState = null;
    }

    private void OnTask10ModeChanged(PlayerActionMode mode)
    {
        if (CanPresentTask10PublisherFrame()) PresentCurrentActionFrame();
    }

    private void OnTask10LoadoutChanged(int slot)
    {
        if (CanPresentTask10PublisherFrame()) PresentCurrentActionFrame();
    }

    private void OnTask10RejectionChanged(PlayerActionRejectionReason reason)
    {
        if (!CanPresentTask10PublisherFrame()) return;
        if (reason == PlayerActionRejectionReason.None) SeedTask10ActionBaseline(force: true);
        else PresentCurrentActionFrame();
    }

    private void PresentCurrentActionFrame()
    {
        if (!GodotObject.IsInstanceValid(_task10ActionModes) ||
            !GodotObject.IsInstanceValid(_task10Energy)) return;
        PresentActionFrame(CurrentTask10ActionFrame(),
            (_task10ActionModes.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero);
    }

    private void SeedTask10ActionBaseline(bool force = false)
    {
        if (!CanPresentTask10PublisherFrame() ||
            !GodotObject.IsInstanceValid(_task10ActionModes) ||
            !GodotObject.IsInstanceValid(_task10Energy) || _hasTask10ActionFrame && !force) return;
        _lastTask10ActionFrame = CurrentTask10ActionFrame();
        _hasTask10ActionFrame = true;
    }

    private PlayerHudActionState CurrentTask10ActionFrame() => new(
        _task10ActionModes.CurrentMode, _task10ActionModes.ActiveSlot,
        _task10Energy.EnergyRatio, _task10Energy.HeatRatio, _task10Energy.IsOverheated,
        _task10ActionModes.LastRejection);

    private void OnTask10EnergyFrameChanged(PlayerEnergyFrame frame)
    {
        if (CanPresentTask10PublisherFrame()) PresentEnergyFrame(frame,
            (_task10Energy?.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero);
    }

    private void OnTask10ShieldFrameChanged(bool active)
    {
        if (CanPresentTask10PublisherFrame()) PresentShieldActive(active,
            (_task10Shield?.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero);
    }

    private void OnTask10ShieldResolved(PlayerShieldResult result, ulong transitionId)
    {
        if (CanPresentTask10PublisherFrame()) PresentShieldResult(result, transitionId,
            (_task10Shield?.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero);
    }

    private void OnTask10ClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (CanPresentTask10PublisherFrame()) PresentClawFrame(frame,
            (_task10Claw?.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero);
    }

    private bool CanPresentTask10PublisherFrame() =>
        !GodotObject.IsInstanceValid(_task10Flow) || _task10Flow.State == PrologueGameFlowState.Playing;

    private void OnTask10RecoveryRequested(string reason) => ClearStagedFrameAudio();
    private void OnTask10RecoveryCompleted(string reason) => ClearStagedFrameAudio();
    private void OnTask10TerminalState() => ClearStagedFrameAudio();

    private void StopTransientFrames()
    {
        ResolvePlayers();
        foreach (AudioStreamPlayer player in _sfx)
        {
            player.Stop();
            player.Stream = null;
        }
    }

    private static bool HasAncestor<T>(Node source) where T : Node
    {
        for (Node current = source; current != null; current = current.GetParent())
            if (current is T) return true;
        return false;
    }

    private static Node ResolveAttackSource(Node source)
    {
        for (Node current = source; current != null; current = current.GetParent())
            if (current is RustShellSquire or GraveLampMoth or BellBackLaborer or FallenBellkeeper)
                return current;
        return source;
    }

    private static ulong SourceInstanceId(Node source)
    {
        if (!GodotObject.IsInstanceValid(source))
            throw new ArgumentException("Typed attack audio requires a live source instance.", nameof(source));
        return source.GetInstanceId();
    }

    private void EnsureReady()
    {
        ResolvePlayers();
        _manifest ??= LoadManifest();
    }

    private static PrologueAudioManifest LoadManifest()
    {
        string path = ProjectSettings.GlobalizePath(ManifestPath);
        return PrologueAudioManifest.Load(path);
    }

    private void ResolvePlayers()
    {
        _musicA ??= GetNodeOrNull<AudioStreamPlayer>("MusicA");
        _musicB ??= GetNodeOrNull<AudioStreamPlayer>("MusicB");
        _ambience ??= GetNodeOrNull<AudioStreamPlayer>("Ambience");
        _ui ??= GetNodeOrNull<AudioStreamPlayer>("UI");
        if (_sfx.Count == 0)
            for (int index = 0; index < 4; index++)
                if (GetNodeOrNull<AudioStreamPlayer>($"Sfx{index}") is AudioStreamPlayer player) _sfx.Add(player);
        if (_musicA == null || _musicB == null || _ambience == null || _ui == null || _sfx.Count != 4)
            throw new InvalidOperationException("PrologueAudioDirector requires MusicA/MusicB/Ambience/Sfx0-3/UI players.");
    }

    private IEnumerable<AudioStreamPlayer> AllPlayers()
    {
        yield return _musicA; yield return _musicB; yield return _ambience;
        foreach (AudioStreamPlayer player in _sfx) yield return player;
        yield return _ui;
    }

    private static AudioStreamPlayer Player(string name, string bus) => new()
    {
        Name = name,
        Bus = bus,
        ProcessMode = ProcessModeEnum.Always
    };

    private static void SetLoop(AudioStreamPlayer player, string path, bool play)
    {
        player.Stop();
        player.Stream = string.IsNullOrWhiteSpace(path) ? null : GD.Load<AudioStream>(path);
        if (player.Stream is AudioStreamWav wav) wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        if (play && player.Stream != null) player.Play();
    }

    private void TransitionLoop(AudioStreamPlayer player, string path)
    {
        if (string.IsNullOrWhiteSpace(path)) { FadeOut(player); return; }
        if (player.Stream?.ResourcePath == path && player.Playing)
        {
            FadeTo(player, 0f);
            return;
        }
        player.Stop();
        player.Stream = GD.Load<AudioStream>(path);
        if (player.Stream is AudioStreamWav wav) wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        player.VolumeDb = -24f;
        player.Play();
        FadeTo(player, 0f);
    }

    private void FadeOut(AudioStreamPlayer player)
    {
        if (player.Stream == null) return;
        if (!IsInsideTree()) { player.Stop(); player.Stream = null; return; }
        Tween tween = CreateTween();
        _tweens.Add(tween);
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(player, "volume_db", -36f, .35f);
        tween.TweenCallback(Callable.From(() => { player.Stop(); player.Stream = null; player.VolumeDb = 0f; }));
    }

    private void FadeTo(AudioStreamPlayer player, float volumeDb)
    {
        if (!IsInsideTree()) { player.VolumeDb = volumeDb; return; }
        Tween tween = CreateTween();
        _tweens.Add(tween);
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(player, "volume_db", volumeDb, .35f);
    }

    private static void SetBus(string bus, float linear)
    {
        int index = AudioServer.GetBusIndex(bus);
        if (index < 0) throw new InvalidOperationException($"Required audio bus is missing: {bus}");
        AudioServer.SetBusVolumeDb(index, linear <= .0001f ? -80f : Mathf.LinearToDb(linear));
    }

    private static bool IsUiCue(PrologueAudioCue cue) => cue is PrologueAudioCue.UiMove or
        PrologueAudioCue.UiConfirm or PrologueAudioCue.UiBack;
}
