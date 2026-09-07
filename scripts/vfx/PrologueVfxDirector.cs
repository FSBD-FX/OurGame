using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Settings;
using Knight.Player;

namespace Knight.Vfx;

public readonly record struct VfxCueStyle(
    Color Color,
    int RayCount,
    float Radius,
    CameraFeedback Camera,
    bool FullScreenFlash,
    float FlashOpacity,
    float LifetimeSeconds);

public partial class PrologueVfxDirector : Node2D
{
    private const string VfxGroup = "prologue_vfx";
    public static readonly string[] Task10EffectRootNames =
    [
        "ShieldFan", "ShieldBlockFlash", "ShieldHeavyBreak",
        "ShieldOverheatVent", "ClawMaterialHit", "RopeTension"
    ];

    [Export] public NodePath WorldEffectsPath { get; set; } = new("World");
    [Export] public NodePath ScreenFlashPath { get; set; } = new("Screen/Flash");
    [Export] public NodePath CameraFeedbackPath { get; set; } = new("CameraFeedback");
    [Export] public NodePath CameraPath { get; set; } = new("../PlayerRoot/Player/Camera2D");

    public int CueCount { get; private set; }
    public PresentationCue LastCue { get; private set; }
    public Vector2 LastWorldPosition { get; private set; }
    public float LastScreenFlashOpacity { get; private set; }
    public float ShakeScale { get; private set; } = 1f;
    public int ActiveBurstCount => _worldEffects?.GetChildCount() ?? 0;
    public int ActiveTask10EffectCount => _task10Effects.Count;
    public int TypedEffectCount => CountTypedEffects(effect => true);
    public int ActiveDangerCount => CountTypedEffects(effect => effect.IsDanger);
    public int ActiveThreadEffectCount => CountTypedEffects(effect => effect.IsThread);
    public Color LastTypedColor { get; private set; } = Colors.Transparent;
    public AttackShapeKind LastTypedAttackShape { get; private set; } = AttackShapeKind.None;
    public string LastNonColorChannel { get; private set; } = string.Empty;
    public string LastTypedAudit { get; private set; } = string.Empty;

    private static readonly Dictionary<PresentationCue, VfxCueStyle> Styles = new()
    {
        [PresentationCue.AttackStarted] = Style("d7b85b", 3, 18, 1.5f, .08f),
        [PresentationCue.AttackHit] = Style("f2e6c9", 8, 34, 5f, .16f),
        [PresentationCue.Hurt] = Style("c45f50", 7, 38, 7f, .2f, true, .12f),
        [PresentationCue.Guard] = Style("9aabb2", 6, 28, 4f, .12f),
        [PresentationCue.Dash] = Style("86d8d0", 4, 30, 2f, .08f),
        [PresentationCue.Downstrike] = Style("e4c45f", 8, 42, 6f, .16f),
        [PresentationCue.Died] = Style("9a5353", 10, 52, 8f, .28f, true, .16f),
        [PresentationCue.ThreadFired] = Style("d7b85b", 3, 20, 1f, .06f),
        [PresentationCue.ThreadHit] = Style("8de4d8", 6, 26, 3f, .1f),
        [PresentationCue.ThreadFailed] = Style("a9584d", 2, 14, 0f, 0f),
        [PresentationCue.WeakpointOpened] = Style("8de4d8", 9, 48, 5f, .18f),
        [PresentationCue.PhaseChanged] = Style("d7b85b", 10, 60, 8f, .24f, true, .14f),
        [PresentationCue.CheckpointActivated] = Style("d7b85b", 9, 46, 3f, .14f),
        [PresentationCue.ShortcutOpened] = Style("8de4d8", 8, 42, 4f, .16f),
        [PresentationCue.BossTransition] = Style("d7b85b", 12, 72, 12f, .35f, true, .2f),
        [PresentationCue.BossExposed] = Style("8de4d8", 12, 64, 9f, .24f, true, .14f),
        [PresentationCue.BossDefeated] = Style("f2e6c9", 14, 88, 12f, .35f, true, .22f),
        [PresentationCue.RecoveryStarted] = Style("61747c", 6, 52, 2f, .18f, true, .1f),
        [PresentationCue.RecoveryCompleted] = Style("d7b85b", 8, 42, 2f, .14f)
    };

    private Node2D _worldEffects;
    private ColorRect _screenFlash;
    private CameraFeedbackController _cameraFeedback;
    private bool _reduceFlashes;
    private float _flashRemaining;
    private float _flashDuration;
    private readonly HashSet<AttackPhaseIdentity> _presentedAttackPhases = new();
    private readonly HashSet<(ulong CastId, ThreadCastState State)> _presentedThreadStates = new();
    private readonly Dictionary<AttackIdentity, PrologueTypedVfxEffect> _attackEffects = new();
    private readonly Dictionary<(PrologueAudioCue Cue, ulong TransitionId), PrologueVfxBurst> _task10Effects = new();
    private readonly HashSet<PrologueVfxBurst> _bursts = new();
    private readonly List<AttackIdentity> _expiredAttackEffects = new();
    private PrologueTypedVfxEffect _threadEffect;
    private bool _wasPaused;
    private PrologueLevelController _level;
    private ulong _boundRoomInstanceId;
    private readonly Dictionary<Node, System.Action<AttackPresentationFrame>> _attackBindings = new();
    private readonly Dictionary<Node, AttackIdentity> _sourceAttackIds = new();
    private ThreadAbility _threadSource;
    private System.Action<ThreadCastFrame, ThreadTarget2D> _threadBinding;
    private PrologueAudioDirector _task10Audio;
    private MechanicalClawController _task10Claw;

    private readonly record struct AttackIdentity(ulong SourceInstanceId, ulong AttackId);
    private readonly record struct AttackPhaseIdentity(AttackIdentity Attack, AttackPhase Phase);

    public override void _EnterTree() => AddToGroup(VfxGroup);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _worldEffects = GetNodeOrNull<Node2D>(WorldEffectsPath) ?? this;
        _screenFlash = GetNodeOrNull<ColorRect>(ScreenFlashPath);
        _cameraFeedback = GetNodeOrNull<CameraFeedbackController>(CameraFeedbackPath);
        Camera2D camera = GetNodeOrNull<Camera2D>(CameraPath);
        if (camera != null) _cameraFeedback?.BindCamera(camera);
        ApplyFlash(0f);
        _wasPaused = GetTree()?.Paused ?? false;
        _level = GetParent() as PrologueLevelController;
        if (GetTree() != null)
        {
            GetTree().NodeAdded += OnTreeNodeAdded;
            GetTree().NodeRemoved += OnTreeNodeRemoved;
        }
        RefreshTypedBindings();
        RefreshTask10Bindings();
    }

    public override void _ExitTree()
    {
        if (GetTree() != null)
        {
            GetTree().NodeAdded -= OnTreeNodeAdded;
            GetTree().NodeRemoved -= OnTreeNodeRemoved;
        }
        UnbindTypedSources();
        UnbindTask10Sources();
        ClearTransientEffects();
    }

    public override void _Process(double delta)
    {
        PruneCompletedAttackEffects();
        bool paused = GetTree()?.Paused ?? false;
        if (paused && !_wasPaused) ClearTransientEffects();
        _wasPaused = paused;
        RefreshTypedBindings();
        RefreshTask10Bindings();
        if (_flashRemaining <= 0f)
        {
            ApplyFlash(0f);
            return;
        }
        _flashRemaining = Mathf.Max(0f, _flashRemaining - (float)delta);
        float ratio = _flashDuration <= 0f ? 0f : _flashRemaining / _flashDuration;
        ApplyFlash(LastScreenFlashOpacity * ratio);
    }

    public void Configure(GameSettingsData settings)
    {
        GameSettingsData normalized = (settings ?? new GameSettingsData()).Normalize();
        ShakeScale = normalized.ScreenShake;
        _reduceFlashes = normalized.ReduceFlashes;
        if (_cameraFeedback != null) _cameraFeedback.IntensityScale = ShakeScale;
        if (_reduceFlashes) ApplyFlash(0f);
    }

    public static bool HasExplicitMapping(PresentationCue cue) => Styles.ContainsKey(cue);

    public static bool HasTask10Mapping(PrologueAudioCue cue) => cue is
        PrologueAudioCue.ModeAttack or PrologueAudioCue.ModeGrapple or PrologueAudioCue.EmptySlot or
        PrologueAudioCue.EnergyEmpty or PrologueAudioCue.ShieldRaise or PrologueAudioCue.ShieldBlock or
        PrologueAudioCue.ShieldHeavyBreak or PrologueAudioCue.ShieldOverheat or PrologueAudioCue.ShieldReady or
        PrologueAudioCue.ClawFire or PrologueAudioCue.ClawTravel or PrologueAudioCue.ClawLatch or
        PrologueAudioCue.ClawTension or PrologueAudioCue.ClawRelease or PrologueAudioCue.ClawIceSlip or
        PrologueAudioCue.ClawFail or PrologueAudioCue.ThreadFail or PrologueAudioCue.ThreadPreview;

    public VfxCueStyle ResolveStyle(PresentationCue cue)
    {
        VfxCueStyle style = Styles.TryGetValue(cue, out VfxCueStyle resolved)
            ? resolved
            : Style("f2e6c9", 3, 18, 0f, 0f);
        return _reduceFlashes && style.FullScreenFlash
            ? style with { FullScreenFlash = false, FlashOpacity = 0f }
            : style;
    }

    public void Play(PresentationCue cue, Vector2 worldPosition, Vector2 direction)
    {
        CueCount++;
        LastCue = cue;
        LastWorldPosition = worldPosition;
        VfxCueStyle style = ResolveStyle(cue);
        var burst = new PrologueVfxBurst
        {
            Name = $"{cue}Burst{CueCount}",
            Position = _worldEffects?.ToLocal(worldPosition) ?? worldPosition,
            CueColor = style.Color,
            RayCount = style.RayCount,
            Radius = style.Radius,
            Direction = direction.LengthSquared() < .001f ? Vector2.Right : direction.Normalized(),
            LifetimeSeconds = style.LifetimeSeconds
        };
        (_worldEffects ?? this).AddChild(burst);
        _bursts.Add(burst);
        _cameraFeedback ??= GetNodeOrNull<CameraFeedbackController>(CameraFeedbackPath);
        if (_cameraFeedback != null)
        {
            _cameraFeedback.IntensityScale = ShakeScale;
            _cameraFeedback.Request(style.Camera);
        }
        LastScreenFlashOpacity = style.FullScreenFlash ? style.FlashOpacity : 0f;
        _flashDuration = style.LifetimeSeconds;
        _flashRemaining = style.FullScreenFlash ? _flashDuration : 0f;
        ApplyFlash(LastScreenFlashOpacity);
    }

    public void PresentAttackFrame(Node source, AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase == AttackPhase.None)
        {
            ReleaseAttackSource(source);
            return;
        }
        frame = frame.Validate();
        var attack = new AttackIdentity(SourceInstanceId(source), frame.Timeline.AttackId);
        var key = new AttackPhaseIdentity(attack, frame.Timeline.Phase);
        bool duplicate = !_presentedAttackPhases.Add(key);
        if (duplicate)
        {
            if (_attackEffects.TryGetValue(key.Attack, out PrologueTypedVfxEffect existing) &&
                GodotObject.IsInstanceValid(existing) && !existing.IsQueuedForDeletion())
            {
                existing.RefreshAttack(frame);
                LastTypedAttackShape = frame.Shape.Kind;
            }
            LastTypedAudit = $"Source={key.Attack.SourceInstanceId} AttackId={key.Attack.AttackId} " +
                             $"Phase={key.Phase} Shape={frame.Shape.Kind} duplicate=True refreshed=True " +
                             $"danger={key.Phase == AttackPhase.Active}";
            return;
        }

        RemoveAttackEffect(key.Attack);
        Color color;
        string channel;
        bool danger;
        switch (key.Phase)
        {
            case AttackPhase.Telegraph:
                color = new Color("d7b85b");
                channel = "countdown-outline";
                danger = false;
                break;
            case AttackPhase.Active:
                color = new Color("b75a4b");
                channel = "danger-hatching";
                danger = true;
                break;
            default:
                color = new Color("f2e6c9");
                channel = "impact-cross";
                danger = false;
                break;
        }

        var effect = PrologueTypedVfxEffect.Attack(frame, color, channel, danger, _reduceFlashes);
        effect.Name = $"Attack{key.Attack.SourceInstanceId}_{key.Attack.AttackId}{key.Phase}";
        (_worldEffects ?? this).AddChild(effect);
        effect.RefreshAttack(frame);
        _attackEffects[key.Attack] = effect;
        _sourceAttackIds[source] = key.Attack;
        LastTypedColor = color;
        LastTypedAttackShape = frame.Shape.Kind;
        LastNonColorChannel = channel;
        LastTypedAudit = $"Source={key.Attack.SourceInstanceId} AttackId={key.Attack.AttackId} " +
                          $"Phase={key.Phase} Shape={frame.Shape.Kind} " +
                          $"duplicate=False danger={danger}";

        if (key.Phase == AttackPhase.Active && _cameraFeedback != null)
        {
            _cameraFeedback.IntensityScale = ShakeScale;
            _cameraFeedback.Request(new CameraFeedback(2.5f, .08f));
        }
    }

    public void PresentThreadFrame(ThreadCastFrame frame, Vector2 origin, Vector2 targetOrFailurePoint)
    {
        if (frame.State == ThreadCastState.Idle ||
            (frame.State == ThreadCastState.Failed && frame.Failure == ThreadFailureReason.Cancelled))
        {
            ClearThreadEffect();
            _presentedThreadStates.Clear();
            LastTypedAudit = $"CastId={frame.CastId} State={frame.State} duplicate=False cleared=True";
            return;
        }

        (ulong CastId, ThreadCastState State) key = (frame.CastId, frame.State);
        bool duplicate = !_presentedThreadStates.Add(key);
        if (duplicate)
        {
            LastTypedAudit = $"CastId={key.CastId} State={key.State} duplicate=True cleared=False";
            return;
        }

        ClearThreadEffect();
        string channel = frame.State switch
        {
            ThreadCastState.Preview => "candidate-diamond",
            ThreadCastState.Windup => "spool-rings",
            ThreadCastState.Travel => "shuttle-motion",
            ThreadCastState.Latched => "latch-diamond",
            ThreadCastState.Apply => "triple-tension",
            ThreadCastState.Recovery => "retract-dashes",
            _ => "failure-cross"
        };
        _threadEffect = PrologueTypedVfxEffect.Thread(frame, origin, targetOrFailurePoint,
            new Color("8de4d8"), channel, _reduceFlashes);
        _threadEffect.Name = $"Thread{frame.CastId}{frame.State}";
        _threadEffect.Position = _worldEffects?.ToLocal(origin) ?? origin;
        (_worldEffects ?? this).AddChild(_threadEffect);
        LastTypedColor = new Color("8de4d8");
        LastTypedAttackShape = AttackShapeKind.None;
        LastNonColorChannel = channel;
        LastTypedAudit = $"CastId={key.CastId} State={key.State} duplicate=False channel={channel}";
        if (frame.State == ThreadCastState.Apply && _cameraFeedback != null)
        {
            _cameraFeedback.IntensityScale = ShakeScale;
            _cameraFeedback.Request(new CameraFeedback(1.5f, .06f));
        }
    }

    public void ClearTransientEffects()
    {
        ClearAttackEffects();
        ClearThreadEffect();
        _presentedAttackPhases.Clear();
        _presentedThreadStates.Clear();
        foreach (PrologueVfxBurst burst in _bursts)
            if (GodotObject.IsInstanceValid(burst) && !burst.IsQueuedForDeletion()) burst.QueueFree();
        _bursts.Clear();
        _task10Effects.Clear();
        _flashRemaining = 0f;
        ApplyFlash(0f);
    }

    public void PresentTask10Cue(PrologueAudioCue cue, ulong transitionId,
        Vector2 worldPosition, Vector2 direction)
    {
        var key = (cue, transitionId);
        if (_task10Effects.ContainsKey(key)) return;
        string rootName = Task10RootForCue(cue);
        Node2D parent = string.IsNullOrEmpty(rootName)
            ? _worldEffects
            : _worldEffects?.GetNodeOrNull<Node2D>(rootName);
        parent ??= _worldEffects ?? this;
        var burst = new PrologueVfxBurst
        {
            Name = $"{cue}{transitionId}",
            Position = parent.ToLocal(worldPosition),
            CueColor = Task10Color(cue),
            RayCount = Task10RayCount(cue),
            Radius = cue is PrologueAudioCue.ClawTension ? 42f : 28f,
            Direction = direction.LengthSquared() > .001f ? direction.Normalized() : Vector2.Right,
            LifetimeSeconds = cue is PrologueAudioCue.ShieldOverheat ? .45f : .22f
        };
        parent.AddChild(burst);
        _bursts.Add(burst);
        _task10Effects[key] = burst;
    }

    public void PresentClawFrame(MechanicalClawFrame frame, Vector2 worldPosition)
    {
        PrologueAudioCue? cue = PrologueAudioDirector.CueForClawFrame(frame);
        if (cue.HasValue) PresentTask10Cue(cue.Value, frame.CastId, worldPosition,
            frame.AnchorPoint - worldPosition);
    }

    public static string Task10RootForCue(PrologueAudioCue cue) => cue switch
    {
        PrologueAudioCue.ShieldRaise => "ShieldFan",
        PrologueAudioCue.ShieldBlock => "ShieldBlockFlash",
        PrologueAudioCue.ShieldHeavyBreak => "ShieldHeavyBreak",
        PrologueAudioCue.ShieldOverheat or PrologueAudioCue.ShieldReady => "ShieldOverheatVent",
        PrologueAudioCue.ClawTension or PrologueAudioCue.ThreadPreview => "RopeTension",
        PrologueAudioCue.ClawFire or PrologueAudioCue.ClawTravel or PrologueAudioCue.ClawLatch or
            PrologueAudioCue.ClawRelease or PrologueAudioCue.ClawIceSlip or PrologueAudioCue.ClawFail or
            PrologueAudioCue.ThreadFail => "ClawMaterialHit",
        _ => string.Empty
    };

    public static Color Task10Color(PrologueAudioCue cue) => cue switch
    {
        PrologueAudioCue.ShieldHeavyBreak or PrologueAudioCue.ShieldOverheat => new Color("e56b5d"),
        PrologueAudioCue.ClawIceSlip => new Color("8dd8ff"),
        PrologueAudioCue.ClawFail => new Color("c98076"),
        PrologueAudioCue.ThreadFail => new Color("ef7b68"),
        PrologueAudioCue.ThreadPreview => new Color("b7a0c9"),
        PrologueAudioCue.ClawFire or PrologueAudioCue.ClawTravel or PrologueAudioCue.ClawLatch or
            PrologueAudioCue.ClawTension or PrologueAudioCue.ClawRelease => new Color("8de4d8"),
        _ => new Color("d7b85b")
    };

    public static int Task10RayCount(PrologueAudioCue cue) => cue switch
    {
        PrologueAudioCue.ShieldHeavyBreak => 12,
        PrologueAudioCue.ClawIceSlip => 10,
        PrologueAudioCue.ClawFail => 4,
        PrologueAudioCue.ThreadFail => 5,
        PrologueAudioCue.ThreadPreview => 3,
        _ => 6
    };

    public void ReleaseAttackSource(Node source)
    {
        if (!GodotObject.IsInstanceValid(source)) return;
        ulong sourceInstanceId = source.GetInstanceId();
        if (_sourceAttackIds.Remove(source, out AttackIdentity attack)) RemoveAttackEffect(attack);
        _presentedAttackPhases.RemoveWhere(key => key.Attack.SourceInstanceId == sourceInstanceId);
    }

    public static void PlayFromTree(Node source, PresentationCue cue, Vector2 worldPosition, Vector2 direction)
    {
        if (source == null || !source.IsInsideTree()) return;
        PrologueAudioDirector.PlayPresentationCueFromTree(source, cue, worldPosition);
        if (source.GetTree()?.GetFirstNodeInGroup(VfxGroup) is PrologueVfxDirector director)
        {
            if (cue == PresentationCue.RecoveryStarted) director.ClearTransientEffects();
            director.Play(cue, worldPosition, direction);
        }
    }

    private int CountTypedEffects(System.Func<PrologueTypedVfxEffect, bool> predicate)
    {
        if (_worldEffects == null) return 0;
        int count = 0;
        foreach (Node child in _worldEffects.GetChildren())
            if (child is PrologueTypedVfxEffect effect && !effect.IsQueuedForDeletion() && predicate(effect)) count++;
        return count;
    }

    private void RefreshTypedBindings()
    {
        if (!GodotObject.IsInstanceValid(_level)) return;
        ulong roomInstanceId = GodotObject.IsInstanceValid(_level.CurrentRoom)
            ? _level.CurrentRoom.GetInstanceId() : 0;
        if (roomInstanceId != _boundRoomInstanceId)
        {
            UnbindAttackSources();
            ClearTransientEffects();
            _boundRoomInstanceId = roomInstanceId;
            BindDescendants(_level.CurrentRoom);
        }
        if (!GodotObject.IsInstanceValid(_threadSource) && GodotObject.IsInstanceValid(_level.Player))
            BindThreadSource(_level.Player.GetNodeOrNull<ThreadAbility>("ThreadAbility"));
    }

    private void RefreshTask10Bindings()
    {
        PrologueAudioDirector audio = GetTree()?.GetFirstNodeInGroup(
            PrologueAudioDirector.AudioGroup) as PrologueAudioDirector;
        MechanicalClawController claw = GodotObject.IsInstanceValid(_level?.Player)
            ? _level.Player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw")
            : null;
        if (ReferenceEquals(audio, _task10Audio) && ReferenceEquals(claw, _task10Claw)) return;
        UnbindTask10Sources();
        _task10Audio = audio;
        _task10Claw = claw;
        if (GodotObject.IsInstanceValid(_task10Audio))
        {
            _task10Audio.Task10CueEmitted += OnTask10CueEmitted;
            _task10Audio.Task10TransitionsCleared += ClearTransientEffects;
        }
        if (GodotObject.IsInstanceValid(_task10Claw))
            _task10Claw.ClawFrameChanged += OnTask10ClawFrameChanged;
    }

    private void UnbindTask10Sources()
    {
        if (GodotObject.IsInstanceValid(_task10Audio))
        {
            _task10Audio.Task10CueEmitted -= OnTask10CueEmitted;
            _task10Audio.Task10TransitionsCleared -= ClearTransientEffects;
        }
        if (GodotObject.IsInstanceValid(_task10Claw))
            _task10Claw.ClawFrameChanged -= OnTask10ClawFrameChanged;
        _task10Audio = null;
        _task10Claw = null;
    }

    private void OnTask10CueEmitted(PrologueAudioCue cue, ulong transitionId, Vector2 position)
    {
        if (IsClawCue(cue)) return;
        PresentTask10Cue(cue, transitionId, position, Vector2.Right);
    }

    private void OnTask10ClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        PrologueGameFlowController flow = _level?.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        if (GodotObject.IsInstanceValid(flow) && flow.State != PrologueGameFlowState.Playing) return;
        PresentClawFrame(frame,
            (_task10Claw?.GetParent() as Node2D)?.GlobalPosition ?? Vector2.Zero);
    }

    private static bool IsClawCue(PrologueAudioCue cue) => cue is
        PrologueAudioCue.ClawFire or PrologueAudioCue.ClawTravel or PrologueAudioCue.ClawLatch or
        PrologueAudioCue.ClawTension or PrologueAudioCue.ClawRelease or
        PrologueAudioCue.ClawIceSlip or PrologueAudioCue.ClawFail or PrologueAudioCue.ThreadFail or
        PrologueAudioCue.ThreadPreview;

    private void BindDescendants(Node root)
    {
        if (!GodotObject.IsInstanceValid(root)) return;
        BindAttackSource(root);
        foreach (Node child in root.GetChildren()) BindDescendants(child);
    }

    private void BindAttackSource(Node source)
    {
        if (source == null || _attackBindings.ContainsKey(source)) return;
        System.Action<AttackPresentationFrame> binding = frame => OnAttackFrame(source, frame);
        switch (source)
        {
            case RustShellSquire rust:
                rust.AttackFrameChanged += binding;
                break;
            case GraveLampMoth moth:
                moth.AttackFrameChanged += binding;
                break;
            case BellBackLaborer laborer:
                laborer.AttackFrameChanged += binding;
                break;
            case FallenBellkeeper boss:
                boss.AttackFrameChanged += binding;
                break;
            default:
                return;
        }
        _attackBindings[source] = binding;
    }

    private void BindThreadSource(ThreadAbility source)
    {
        if (!GodotObject.IsInstanceValid(source)) return;
        _threadSource = source;
        _threadBinding = OnThreadFrame;
        _threadSource.CastFrameChanged += _threadBinding;
    }

    private void OnAttackFrame(Node source, AttackPresentationFrame frame)
    {
        PresentAttackFrame(source, frame);
        if (source.GetTree()?.GetFirstNodeInGroup(PrologueAudioDirector.AudioGroup) is PrologueAudioDirector audio)
            audio.PlayAttackFrame(source, frame, frame.Timeline.Phase == AttackPhase.None
                ? (source as Node2D)?.GlobalPosition ?? Vector2.Zero : frame.Shape.Origin);
    }

    private void OnThreadFrame(ThreadCastFrame frame, ThreadTarget2D target)
    {
        Vector2 origin = GodotObject.IsInstanceValid(_level?.Player)
            ? _level.Player.GlobalPosition : Vector2.Zero;
        Vector2 endpoint = GodotObject.IsInstanceValid(target)
            ? target.GlobalPosition
            : origin + (frame.Aim.LengthSquared() < .001f ? Vector2.Right : frame.Aim.Normalized()) * 140f;
        PresentThreadFrame(frame, origin, endpoint);
        if (GetTree()?.GetFirstNodeInGroup(PrologueAudioDirector.AudioGroup) is PrologueAudioDirector audio)
            audio.PlayThreadFrame(frame, origin);
    }

    private void OnTreeNodeAdded(Node node)
    {
        if (!GodotObject.IsInstanceValid(_level?.CurrentRoom) || !IsDescendantOf(node, _level.CurrentRoom)) return;
        BindAttackSource(node);
    }

    private void OnTreeNodeRemoved(Node node)
    {
        if (_attackBindings.ContainsKey(node)) UnbindAttackSource(node);
        if (node == _threadSource) UnbindThreadSource();
    }

    private void UnbindTypedSources()
    {
        UnbindAttackSources();
        UnbindThreadSource();
    }

    private void UnbindAttackSources()
    {
        Node[] sources = new Node[_attackBindings.Count];
        _attackBindings.Keys.CopyTo(sources, 0);
        foreach (Node source in sources) UnbindAttackSource(source);
    }

    private void UnbindAttackSource(Node source)
    {
        if (!_attackBindings.Remove(source, out System.Action<AttackPresentationFrame> binding)) return;
        if (GodotObject.IsInstanceValid(source))
        {
            switch (source)
            {
                case RustShellSquire rust: rust.AttackFrameChanged -= binding; break;
                case GraveLampMoth moth: moth.AttackFrameChanged -= binding; break;
                case BellBackLaborer laborer: laborer.AttackFrameChanged -= binding; break;
                case FallenBellkeeper boss: boss.AttackFrameChanged -= binding; break;
            }
        }
        ReleaseAttackSource(source);
        if (GetTree()?.GetFirstNodeInGroup(PrologueAudioDirector.AudioGroup) is PrologueAudioDirector audio)
            audio.ReleaseAttackSource(source);
    }

    private void UnbindThreadSource()
    {
        if (GodotObject.IsInstanceValid(_threadSource) && _threadBinding != null)
            _threadSource.CastFrameChanged -= _threadBinding;
        _threadSource = null;
        _threadBinding = null;
    }

    private static bool IsDescendantOf(Node node, Node ancestor)
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (current == ancestor) return true;
        return false;
    }

    private void ClearAttackEffects()
    {
        foreach (PrologueTypedVfxEffect effect in _attackEffects.Values)
            RemoveEffect(effect);
        _attackEffects.Clear();
    }

    private void RemoveAttackEffect(AttackIdentity attack)
    {
        if (!_attackEffects.Remove(attack, out PrologueTypedVfxEffect effect)) return;
        RemoveEffect(effect);
    }

    private void PruneCompletedAttackEffects()
    {
        if (_attackEffects.Count == 0) return;
        _expiredAttackEffects.Clear();
        foreach ((AttackIdentity attack, PrologueTypedVfxEffect effect) in _attackEffects)
            if (!GodotObject.IsInstanceValid(effect) || effect.IsQueuedForDeletion())
                _expiredAttackEffects.Add(attack);
        foreach (AttackIdentity attack in _expiredAttackEffects) _attackEffects.Remove(attack);
        _expiredAttackEffects.Clear();
    }

    private void ClearThreadEffect()
    {
        RemoveEffect(_threadEffect);
        _threadEffect = null;
    }

    private static void RemoveEffect(PrologueTypedVfxEffect effect)
    {
        if (!GodotObject.IsInstanceValid(effect) || effect.IsQueuedForDeletion()) return;
        effect.QueueFree();
    }

    private static ulong SourceInstanceId(Node source)
    {
        if (!GodotObject.IsInstanceValid(source))
            throw new System.ArgumentException("Typed attack VFX requires a live source instance.", nameof(source));
        return source.GetInstanceId();
    }

    private void ApplyFlash(float opacity)
    {
        if (_screenFlash != null)
        {
            Color color = _screenFlash.Color;
            color.A = Mathf.Clamp(opacity, 0f, .24f);
            _screenFlash.Color = color;
            _screenFlash.Visible = color.A > .001f;
        }
        if (opacity <= .001f) LastScreenFlashOpacity = 0f;
    }

    private static VfxCueStyle Style(string color, int rays, float radius, float shake, float duration,
        bool flash = false, float flashOpacity = 0f) =>
        new(new Color(color), rays, radius, new CameraFeedback(shake, duration), flash,
            Mathf.Min(.24f, flashOpacity), Mathf.Max(.08f, duration));
}

public partial class PrologueVfxBurst : Node2D
{
    public Color CueColor { get; set; } = Colors.White;
    public int RayCount { get; set; } = 6;
    public float Radius { get; set; } = 30f;
    public Vector2 Direction { get; set; } = Vector2.Right;
    public float LifetimeSeconds { get; set; } = .15f;

    private float _elapsed;

    public override void _Ready() => QueueRedraw();

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= LifetimeSeconds) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        float ratio = LifetimeSeconds <= 0f ? 1f : Mathf.Clamp(_elapsed / LifetimeSeconds, 0f, 1f);
        Color color = CueColor;
        color.A = 1f - ratio;
        float radius = Mathf.Lerp(Radius * .35f, Radius, ratio);
        DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 28, color, Mathf.Lerp(4f, 1f, ratio), true);
        float baseAngle = Direction.Angle();
        int rays = Mathf.Max(1, RayCount);
        for (int index = 0; index < rays; index++)
        {
            float angle = baseAngle + Mathf.Tau * index / rays;
            Vector2 axis = Vector2.FromAngle(angle);
            DrawLine(axis * radius * .45f, axis * radius * 1.25f, color, 2f, true);
        }
    }
}

public partial class PrologueTypedVfxEffect : Node2D
{
    public Color SemanticColor { get; private set; }
    public string NonColorChannel { get; private set; } = string.Empty;
    public bool IsDanger { get; private set; }
    public bool IsThread { get; private set; }
    public AttackPresentationFrame AttackFrame { get; private set; }
    public AttackShapeFrame AttackShape { get; private set; }
    public ThreadCastFrame ThreadFrame { get; private set; }

    private Vector2 _threadDelta;
    private float _lifetime;
    private float _elapsed;
    private bool _reducedFlash;

    public static PrologueTypedVfxEffect Attack(AttackPresentationFrame frame, Color color,
        string channel, bool danger, bool reducedFlash) => new()
    {
        SemanticColor = color,
        NonColorChannel = channel,
        IsDanger = danger,
        AttackFrame = frame,
        AttackShape = frame.Shape,
        _reducedFlash = reducedFlash,
        _lifetime = frame.Timeline.Phase == AttackPhase.Recovery ? .18f :
            Mathf.Max(.12f, frame.Timeline.PhaseRemainingSeconds)
    };

    public static PrologueTypedVfxEffect Thread(ThreadCastFrame frame, Vector2 origin, Vector2 target,
        Color color, string channel, bool reducedFlash) => new()
    {
        SemanticColor = color,
        NonColorChannel = channel,
        IsThread = true,
        ThreadFrame = frame,
        _threadDelta = target - origin,
        _reducedFlash = reducedFlash,
        _lifetime = Mathf.Max(.12f, frame.StateRemainingSeconds)
    };

    public void RefreshAttack(AttackPresentationFrame frame)
    {
        AttackFrame = frame.Validate();
        AttackShape = AttackFrame.Shape;
        GlobalTransform = new Transform2D(AttackShape.RotationRadians, AttackShape.Origin);
        _lifetime = Mathf.Max(_lifetime, _elapsed + AttackFrame.Timeline.PhaseRemainingSeconds);
        QueueRedraw();
    }

    public override void _Ready() => QueueRedraw();

    public override void _Process(double delta)
    {
        _elapsed += (float)delta;
        if (_elapsed >= _lifetime)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color color = SemanticColor;
        float fade = 1f - Mathf.Clamp(_elapsed / Mathf.Max(.001f, _lifetime), 0f, 1f);
        color.A = fade * (_reducedFlash ? .42f : .72f);
        if (IsThread) DrawThread(color);
        else DrawAttack(color);
    }

    private void DrawAttack(Color color)
    {
        Vector2[] outline = AttackOutline();
        if (outline.Length < 3) return;
        Vector2[] closed = new Vector2[outline.Length + 1];
        for (int index = 0; index < outline.Length; index++) closed[index] = outline[index];
        closed[^1] = outline[0];
        float width = IsDanger ? 4f : 3f;
        DrawPolyline(closed, color, width, true);

        if (NonColorChannel == "countdown-outline")
        {
            int tickStep = Mathf.Max(1, outline.Length / 8);
            for (int index = 0; index < outline.Length; index += tickStep)
            {
                Vector2 point = outline[index];
                Vector2 normal = point.LengthSquared() < .001f ? Vector2.Up : point.Normalized();
                DrawLine(point - normal * 5f, point + normal * 5f, color, 2f, true);
            }
        }
        else if (NonColorChannel == "danger-hatching")
        {
            Rect2 bounds = Bounds(outline);
            for (float x = bounds.Position.X - bounds.Size.Y; x < bounds.End.X; x += 18f)
                DrawLine(new Vector2(x, bounds.End.Y), new Vector2(x + bounds.Size.Y, bounds.Position.Y),
                    color, 1.5f, true);
        }
        else
        {
            float radius = Mathf.Max(12f, AttackShape.Extent.Length() * .35f);
            DrawLine(Vector2.Left * radius, Vector2.Right * radius, color, 4f, true);
            DrawLine(Vector2.Up * radius, Vector2.Down * radius, color, 4f, true);
        }
    }

    private void DrawThread(Color color)
    {
        Vector2 end = _threadDelta;
        switch (ThreadFrame.State)
        {
            case ThreadCastState.Preview:
                DrawDiamond(end, 13f, color, 2f);
                DrawDashedLine(Vector2.Zero, end, color, 7f, 7f, 2f);
                break;
            case ThreadCastState.Windup:
                DrawArc(Vector2.Zero, 14f, 0f, Mathf.Tau * .8f, 20, color, 3f, true);
                DrawArc(Vector2.Zero, 21f, Mathf.Pi, Mathf.Tau * 1.8f, 24, color, 2f, true);
                break;
            case ThreadCastState.Travel:
                DrawDashedLine(Vector2.Zero, end, color, 12f, 5f, 2f);
                DrawDiamond(end * .82f, 8f, color, 3f);
                break;
            case ThreadCastState.Latched:
                DrawLine(Vector2.Zero, end, color, 3f, true);
                DrawDiamond(end, 15f, color, 4f);
                break;
            case ThreadCastState.Apply:
                Vector2 normal = end.LengthSquared() < .001f ? Vector2.Up : end.Normalized().Orthogonal() * 4f;
                DrawLine(normal, end + normal, color, 2f, true);
                DrawLine(Vector2.Zero, end, color, 3f, true);
                DrawLine(-normal, end - normal, color, 2f, true);
                DrawDiamond(end, 13f, color, 3f);
                break;
            case ThreadCastState.Recovery:
                DrawDashedLine(Vector2.Zero, end * .55f, color, 5f, 6f, 2f);
                break;
            default:
                DrawLine(new Vector2(-9, -9), new Vector2(9, 9), color, 3f, true);
                DrawLine(new Vector2(-9, 9), new Vector2(9, -9), color, 3f, true);
                break;
        }
    }

    private Vector2[] AttackOutline()
    {
        AttackShapeFrame shape = AttackShape;
        if (shape.Kind == AttackShapeKind.ConvexPolygon)
        {
            var points = new Vector2[shape.Points.Count];
            Transform2D inverse = new Transform2D(shape.RotationRadians, shape.Origin).AffineInverse();
            for (int index = 0; index < points.Length; index++) points[index] = inverse * shape.Points[index];
            return points;
        }
        if (shape.Kind is AttackShapeKind.Rectangle or AttackShapeKind.GroundBand)
            return [new(-shape.Extent.X, -shape.Extent.Y), new(shape.Extent.X, -shape.Extent.Y),
                new(shape.Extent.X, shape.Extent.Y), new(-shape.Extent.X, shape.Extent.Y)];

        const int segments = 40;
        var rounded = new Vector2[segments];
        if (shape.Kind == AttackShapeKind.Circle)
        {
            for (int index = 0; index < segments; index++)
            {
                float angle = Mathf.Tau * index / segments;
                rounded[index] = new Vector2(Mathf.Cos(angle) * shape.Extent.X,
                    Mathf.Sin(angle) * shape.Extent.Y);
            }
            return rounded;
        }
        float radius = shape.Extent.X;
        float straight = Mathf.Max(0f, shape.Extent.Y - radius);
        for (int index = 0; index < segments / 2; index++)
        {
            float angle = Mathf.Pi + Mathf.Pi * index / (segments / 2 - 1f);
            rounded[index] = new Vector2(Mathf.Cos(angle) * radius,
                -straight + Mathf.Sin(angle) * radius);
        }
        for (int index = 0; index < segments / 2; index++)
        {
            float angle = Mathf.Pi * index / (segments / 2 - 1f);
            rounded[segments / 2 + index] = new Vector2(Mathf.Cos(angle) * radius,
                straight + Mathf.Sin(angle) * radius);
        }
        return rounded;
    }

    private static Rect2 Bounds(Vector2[] points)
    {
        Vector2 minimum = points[0];
        Vector2 maximum = points[0];
        foreach (Vector2 point in points)
        {
            minimum = new Vector2(Mathf.Min(minimum.X, point.X), Mathf.Min(minimum.Y, point.Y));
            maximum = new Vector2(Mathf.Max(maximum.X, point.X), Mathf.Max(maximum.Y, point.Y));
        }
        return new Rect2(minimum, maximum - minimum);
    }

    private void DrawDiamond(Vector2 center, float radius, Color color, float width)
    {
        Vector2[] points = [center + Vector2.Up * radius, center + Vector2.Right * radius,
            center + Vector2.Down * radius, center + Vector2.Left * radius, center + Vector2.Up * radius];
        DrawPolyline(points, color, width, true);
    }

    private void DrawDashedLine(Vector2 from, Vector2 to, Color color, float dash, float gap, float width)
    {
        Vector2 delta = to - from;
        float length = delta.Length();
        if (length <= .001f) return;
        Vector2 axis = delta / length;
        for (float offset = 0f; offset < length; offset += dash + gap)
            DrawLine(from + axis * offset, from + axis * Mathf.Min(length, offset + dash), color, width, true);
    }
}
