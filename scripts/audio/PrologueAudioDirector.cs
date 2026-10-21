using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Ranged;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
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
    public ThrusterIdentity ActiveThrusterIdentity { get; private set; }
    public ulong ThrusterClearGeneration { get; private set; }
    public int ThrusterStartCount { get; private set; }
    public int ThrusterEndCount { get; private set; }
    public bool ThrusterVoicePlaying => ActiveThrusterIdentity != default &&
        GodotObject.IsInstanceValid(_thrusterVoice) && _thrusterVoice.Playing;
    public bool ThrusterTailPlaying => ActiveThrusterIdentity == default && _thrusterVoiceOwner != 0UL &&
        GodotObject.IsInstanceValid(_thrusterVoice) && _thrusterVoice.Playing;
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
    private AudioStreamPlayer _guidanceCrackle;
    private AudioStreamWav _guidanceCrackleWav;
    private AudioStreamPlayer _thrusterVoice;
    private AudioStreamWav _thrusterLoop;
    private PlayerMotor _thrusterSource;
    private ThrusterIdentity _lastThrusterStarted;
    private ulong _thrusterVoiceOwner;
    private bool _thrusterWasPaused;
    private string _lastGuidanceInstruction = string.Empty;
    private ulong _lastGuidanceCrackleTicks;
    private bool _hasGuidanceCrackle;
    private readonly List<AudioStreamPlayer> _sfx = new();
    private readonly Dictionary<PrologueAudioCue, int> _variantIndices = new();
    private readonly List<Tween> _tweens = new();
    private readonly HashSet<AttackPhaseIdentity> _playedAttackPhases = new();
    private readonly HashSet<(ulong SourceInstanceId, ulong ShotOrdinal)> _playedRangedShots = new();
    private readonly HashSet<(ulong CastId, ThreadCastState State)> _playedThreadStates = new();
    private LegacyCompatibilityToken? _legacyCompatibilityToken;
    private int _sfxIndex;
    private bool _hasTask10ActionFrame;
    private PlayerHudActionState _lastTask10ActionFrame;
    private bool _hasTask10ClawFrame;
    private MechanicalClawFrame _lastTask10Claw;
    private PrologueAudioCue? _lastTask10ClawCue;
    private readonly HashSet<(ulong CastId, MechanicalClawState State,
        MechanicalClawFailure Failure)> _task10ClawTransitions = new();
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
        bool paused = GetTree()?.Paused ?? false;
        if (paused && !_thrusterWasPaused) { ThrusterClearGeneration++; StopThruster(0UL); }
        _thrusterWasPaused = paused;
        if (_thrusterSource != null && (!GodotObject.IsInstanceValid(_thrusterSource) ||
            !_thrusterSource.IsInsideTree() || (ActiveThrusterIdentity != default &&
            (!_thrusterSource.ThrusterPresentation.Burning ||
             _thrusterSource.ThrusterPresentation.Identity != ActiveThrusterIdentity)))) StopThruster(0UL);
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
        ThrusterClearGeneration++; StopThruster(0UL);
        UnbindTask10PublisherBindings();
        foreach (Tween tween in _tweens)
        {
            // This director created and owns these wrappers; no SceneTree query
            // or borrowed cached Resource is disposed here.
            if (!GodotObject.IsInstanceValid(tween)) continue;
            if (tween.IsValid()) tween.Kill();
            tween.Dispose();
        }
        _tweens.Clear();
        ResolvePlayers();
        foreach (AudioStreamPlayer player in AllPlayers())
        {
            player.Stop();
            player.Stream = null;
        }
        _guidanceCrackleWav?.Dispose();
        _guidanceCrackleWav = null;
        if (_thrusterLoop != null && Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace-thruster-audio-owners") >= 0)
            GD.Print($"THRUSTER_OWNED_COPY_ID stage=all-streams-null-before-owned-dispose id={_thrusterLoop.GetInstanceId()} " +
                $"refs={_thrusterLoop.GetReferenceCount()} diagnostic_only=true");
        _thrusterLoop?.Dispose(); _thrusterLoop = null;
        _lastGuidanceInstruction = string.Empty;
        _lastGuidanceCrackleTicks = 0UL;
        _hasGuidanceCrackle = false;
        _playedAttackPhases.Clear();
        _playedRangedShots.Clear();
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

    public void ResetGuidanceSession() => _lastGuidanceInstruction = string.Empty;

    public bool TryPlayGuidanceCrackle(string instructionIdentity)
    {
        if (!PlaybackEnabled || AudioPaused || !IsInsideTree() || GetTree().Paused ||
            string.IsNullOrWhiteSpace(instructionIdentity)) return false;
        string identity = instructionIdentity.Trim();
        ulong now = Time.GetTicksMsec();
        if (string.Equals(identity, _lastGuidanceInstruction, StringComparison.Ordinal) ||
            (_hasGuidanceCrackle &&
                now - _lastGuidanceCrackleTicks < 8000UL)) return false;
        ResolvePlayers();
        _guidanceCrackleWav ??= CreateGuidanceCrackle();
        _guidanceCrackle.Stream = _guidanceCrackleWav;
        _guidanceCrackle.Play();
        _lastGuidanceInstruction = identity;
        _lastGuidanceCrackleTicks = now;
        _hasGuidanceCrackle = true;
        return true;
    }

    private static AudioStreamWav CreateGuidanceCrackle()
    {
        const int mixRate = 22050;
        const double duration = .19;
        var data = new byte[(int)(mixRate * duration) * 2];
        var random = new Random(0x4b17);
        double previousNoise = 0d;
        for (int index = 0; index < data.Length / 2; index++)
        {
            double time = (double)index / mixRate;
            double phase = time is >= .008 and < .068 ? (time - .008) / .06 :
                time is >= .118 and < .178 ? (time - .118) / .06 : -1d;
            if (phase < 0d) continue;
            double noise = random.NextDouble() * 2d - 1d;
            double envelope = Math.Sin(Math.PI * phase);
            // A quiet, gently enveloped high-pass noise pair: no speech or long loop.
            short sample = (short)((noise - previousNoise) * .055d * envelope * 32767d);
            previousNoise = noise;
            data[index * 2] = unchecked((byte)sample);
            data[index * 2 + 1] = unchecked((byte)(sample >> 8));
        }
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = mixRate,
            Stereo = false,
            LoopMode = AudioStreamWav.LoopModeEnum.Disabled,
            Data = data
        };
    }

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

    public bool StartThruster(PlayerMotor owner, ThrusterIdentity identity, Vector2 position)
    {
        if (!CanPlayRangedSource(owner, position) || identity == default ||
            owner.ThrusterPresentation.Identity != identity || !owner.ThrusterPresentation.Burning ||
            identity.OwnerId != owner.GetInstanceId() ||
            (_lastThrusterStarted.OwnerId == identity.OwnerId && identity.BurnId <= _lastThrusterStarted.BurnId)) return false;
        EnsureReady();
        if (_thrusterVoice == null)
        {
            _thrusterVoice = Player("ThrusterVoice", "SFX");
            _thrusterVoice.ProcessMode = ProcessModeEnum.Pausable;
            AddChild(_thrusterVoice);
        }
        if (_thrusterLoop == null)
        {
            // Duplicate once, own and dispose this copy only. Never set loop mode
            // or Dispose on the cached imported resource shared by legacy cues.
            var cached = GD.Load<AudioStreamWav>("res://assets/audio/prologue/sfx/player/dash.wav");
            _thrusterLoop = (AudioStreamWav)cached.Duplicate();
            _thrusterLoop.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
            _thrusterLoop.LoopBegin = 0;
            _thrusterLoop.LoopEnd = (int)Math.Round(cached.GetLength() * cached.MixRate);
        }
        if (!CanPlayRangedSource(owner, position) || owner.ThrusterPresentation.Identity != identity ||
            !owner.ThrusterPresentation.Burning) return false;
        StopThruster(0UL);
        _lastThrusterStarted = ActiveThrusterIdentity = identity;
        _thrusterVoiceOwner = identity.OwnerId; _thrusterSource = owner;
        _thrusterVoice.Stream = _thrusterLoop; _thrusterVoice.Play();
        ThrusterStartCount++;
        return true;
    }

    public void EndThruster(ThrusterIdentity identity, ThrusterEndReason reason)
    {
        if (identity == default || ActiveThrusterIdentity != identity) return;
        StopThruster(identity.OwnerId);
        if (!PlaybackEnabled || AudioPaused || !IsInsideTree() || GetTree().Paused ||
            reason is ThrusterEndReason.Death or ThrusterEndReason.Reset or ThrusterEndReason.OwnerRetired) return;
        string path = reason == ThrusterEndReason.Exhausted
            ? "res://assets/audio/prologue/sfx/claw/release.wav"
            : "res://assets/audio/prologue/sfx/thread/preview.wav";
        _thrusterVoiceOwner = identity.OwnerId;
        _thrusterVoice.Stream = GD.Load<AudioStream>(path); _thrusterVoice.Play();
        ThrusterEndCount++;
    }

    public void StopThruster(ulong ownerId)
    {
        if (ownerId != 0UL && _thrusterVoiceOwner != ownerId) return;
        if (GodotObject.IsInstanceValid(_thrusterVoice)) { _thrusterVoice.Stop(); _thrusterVoice.Stream = null; }
        ActiveThrusterIdentity = default; _thrusterVoiceOwner = 0UL; _thrusterSource = null;
    }

    public void PlayAttackFrame(Node source, AttackPresentationFrame frame, Vector2 position)
    {
        if (frame.Timeline.Phase == AttackPhase.None)
        {
            ReleaseAttackSource(source);
            return;
        }
        // Preserve the established legacy frame audit/count contract. New
        // ranged consumers count only enabled, live, attached playback.
        if (IsRangedAttackKind(frame.Timeline.Kind) &&
            (source is not RangedWeaponEnemy ranged || !CanPlayRangedActor(ranged, position))) return;
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

    public bool TryPlayRangedShot(RangedWeaponEnemy source, ulong shotOrdinal, Vector2 position)
    {
        if (shotOrdinal == 0UL || !CanPlayRangedActor(source, position) ||
            source.CurrentFrame.AttackTimeline.Phase is not (AttackPhase.Active or AttackPhase.Recovery)) return false;
        var key = (SourceInstanceId: source.GetInstanceId(), ShotOrdinal: shotOrdinal);
        bool duplicate = !_playedRangedShots.Add(key);
        LastFrameDeduplicated = duplicate;
        if (!duplicate)
        {
            // Existing sentry-style onset is a placeholder bank reuse, not a
            // claim that four bespoke weapon sounds have been authored.
            Play(PrologueAudioCue.RustShellAttack, position);
            FramePlaybackCount++;
        }
        LastFrameAudit = $"Source={key.SourceInstanceId} ShotOrdinal={key.ShotOrdinal} " +
            $"cue={PrologueAudioCue.RustShellAttack} dedupe={duplicate} path=" +
            (duplicate ? "Duplicate" : LastPlayedPath);
        return !duplicate;
    }

    public void ReleaseAttackSource(Node source)
    {
        if (!GodotObject.IsInstanceValid(source)) return;
        ulong sourceInstanceId = source.GetInstanceId();
        _playedAttackPhases.RemoveWhere(key => key.SourceInstanceId == sourceInstanceId);
        _playedRangedShots.RemoveWhere(key => key.SourceInstanceId == sourceInstanceId);
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
        if (HasAncestor<RangedWeaponEnemy>(source) && !director.CanPlayRangedSource(source, position)) return;
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
        bool sentry = HasAncestor<BellBoltSentry>(source);
        bool skitter = HasAncestor<CanyonSkitter>(source);
        bool ranged = HasAncestor<RangedWeaponEnemy>(source);
        return cue switch
        {
            PresentationCue.AttackTelegraph when skitter => PrologueAudioCue.RustShellTelegraph,
            PresentationCue.AttackTelegraph when ranged => PrologueAudioCue.LaborerTelegraph,
            PresentationCue.AttackStarted when boss => PrologueAudioCue.BossAttack,
            PresentationCue.AttackStarted when laborer => PrologueAudioCue.LaborerAttack,
            PresentationCue.AttackStarted when moth => PrologueAudioCue.MothDive,
            PresentationCue.AttackStarted when rust => PrologueAudioCue.RustShellAttack,
            PresentationCue.AttackStarted when skitter => PrologueAudioCue.RustShellAttack,
            PresentationCue.AttackStarted when ranged => PrologueAudioCue.RustShellAttack,
            PresentationCue.AttackStarted => PrologueAudioCue.PlayerAttack,
            PresentationCue.AttackHit => PrologueAudioCue.PlayerHit,
            PresentationCue.Hurt when boss => PrologueAudioCue.BossHammer,
            PresentationCue.Hurt when laborer => PrologueAudioCue.LaborerHurt,
            PresentationCue.Hurt when moth => PrologueAudioCue.MothHurt,
            PresentationCue.Hurt when rust => PrologueAudioCue.RustShellHurt,
            PresentationCue.Hurt when sentry => PrologueAudioCue.LaborerHurt,
            PresentationCue.Hurt when skitter => PrologueAudioCue.RustShellHurt,
            PresentationCue.Hurt when ranged => PrologueAudioCue.LaborerHurt,
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
            PresentationCue.Died when sentry => PrologueAudioCue.LaborerDeath,
            PresentationCue.Died when skitter => PrologueAudioCue.RustShellDeath,
            PresentationCue.Died when ranged => PrologueAudioCue.LaborerDeath,
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
                EnemyAttackKind.BellBoltShot => PrologueAudioCue.LaborerTelegraph,
                EnemyAttackKind.SkitterRush => PrologueAudioCue.RustShellTelegraph,
                EnemyAttackKind.RivetScatterShot or EnemyAttackKind.BeltBurstShot or
                    EnemyAttackKind.FurnaceMissileShot or EnemyAttackKind.PrismLaserBeam => PrologueAudioCue.LaborerTelegraph,
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
                EnemyAttackKind.BellBoltShot => PrologueAudioCue.RustShellAttack,
                EnemyAttackKind.SkitterRush => PrologueAudioCue.RustShellAttack,
                // Real rounds/beam starts are consumed through ShotEmitted;
                // Active phase refresh must never add a second onset.
                EnemyAttackKind.RivetScatterShot or EnemyAttackKind.BeltBurstShot or
                    EnemyAttackKind.FurnaceMissileShot or EnemyAttackKind.PrismLaserBeam => null,
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
                EnemyAttackKind.BellBoltShot => PrologueAudioCue.LaborerRecovery,
                EnemyAttackKind.SkitterRush => PrologueAudioCue.RustShellRecovery,
                EnemyAttackKind.RivetScatterShot or EnemyAttackKind.BeltBurstShot or
                    EnemyAttackKind.FurnaceMissileShot or EnemyAttackKind.PrismLaserBeam => PrologueAudioCue.LaborerRecovery,
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
        ThrusterClearGeneration++; StopThruster(0UL);
        StopTransientFrames();
        _playedAttackPhases.Clear();
        _playedRangedShots.Clear();
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
        bool duplicate = frame.CastId == 0UL
            ? _hasTask10ClawFrame && _lastTask10Claw.CastId == 0UL &&
              frame.State == _lastTask10Claw.State && frame.Failure == _lastTask10Claw.Failure
            : !_task10ClawTransitions.Add((frame.CastId, frame.State, frame.Failure));
        if (duplicate) return;
        PrologueAudioCue? cue = CueForClawFrame(frame);
        // Boost is an accepted input impulse, not continuous Reel/Swing tension.
        bool sameCueFamily = _hasTask10ClawFrame && frame.CastId == _lastTask10Claw.CastId &&
            cue.HasValue && cue == _lastTask10ClawCue && frame.State != MechanicalClawState.Boost;
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
        _task10ClawTransitions.Clear();
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

    private static bool IsRangedAttackKind(EnemyAttackKind kind) => kind is
        EnemyAttackKind.RivetScatterShot or EnemyAttackKind.BeltBurstShot or
        EnemyAttackKind.FurnaceMissileShot or EnemyAttackKind.PrismLaserBeam;

    private bool CanPlayRangedActor(RangedWeaponEnemy source, Vector2 position) =>
        CanPlayRangedSource(source, position) &&
        source.HasPresentationAuthority &&
        source.CurrentFrame.State is not (RangedWeaponState.Stunned or RangedWeaponState.Dead) &&
        source.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth > 0;

    private bool CanPlayRangedSource(Node source, Vector2 position)
    {
        if (!PlaybackEnabled || AudioPaused || !IsInsideTree() || IsQueuedForDeletion() ||
            !GodotObject.IsInstanceValid(source) || !source.IsInsideTree() ||
            source.GetTree() != GetTree() || GetTree().Paused ||
            !float.IsFinite(position.X) || !float.IsFinite(position.Y)) return false;
        for (Node current = source; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }

    private static Node ResolveAttackSource(Node source)
    {
        for (Node current = source; current != null; current = current.GetParent())
            if (current is RustShellSquire or GraveLampMoth or BellBackLaborer or FallenBellkeeper or CanyonSkitter or RangedWeaponEnemy)
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
        return PrologueAudioManifest.Load(ManifestPath);
    }

    private void ResolvePlayers()
    {
        _musicA ??= GetNodeOrNull<AudioStreamPlayer>("MusicA");
        _musicB ??= GetNodeOrNull<AudioStreamPlayer>("MusicB");
        _ambience ??= GetNodeOrNull<AudioStreamPlayer>("Ambience");
        _ui ??= GetNodeOrNull<AudioStreamPlayer>("UI");
        _guidanceCrackle ??= GetNodeOrNull<AudioStreamPlayer>("GuidanceCrackle");
        if (_guidanceCrackle == null)
        {
            // Existing packed scenes keep their manifest players; this owned
            // procedural voice is mounted independently without regenerating them.
            _guidanceCrackle = Player("GuidanceCrackle", "UI");
            _guidanceCrackle.ProcessMode = ProcessModeEnum.Pausable;
            AddChild(_guidanceCrackle);
        }
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
        yield return _guidanceCrackle;
        if (_thrusterVoice != null) yield return _thrusterVoice;
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
        using PropertyTweener property = tween.TweenProperty(player, "volume_db", -36f, .35f);
        using CallbackTweener callback = tween.TweenCallback(Callable.From(() => { player.Stop(); player.Stream = null; player.VolumeDb = 0f; }));
    }

    private void FadeTo(AudioStreamPlayer player, float volumeDb)
    {
        if (!IsInsideTree()) { player.VolumeDb = volumeDb; return; }
        Tween tween = CreateTween();
        _tweens.Add(tween);
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        using PropertyTweener property = tween.TweenProperty(player, "volume_db", volumeDb, .35f);
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
