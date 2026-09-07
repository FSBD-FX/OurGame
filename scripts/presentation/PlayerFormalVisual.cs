using System.Collections.Generic;
using Godot;

namespace Knight.Presentation;

public partial class PlayerFormalVisual : Node2D, IEntityVisual2D
{
    private static readonly string[] RequiredLayers =
    [
        "Body", "NearArm", "FarArm", "NearLeg", "FarLeg", "Weapon", "Mantle",
        "ShoulderCore", "Lantern", "Tether"
    ];

    private const PlayerPoseOwnership UpperBodyOwnership =
        PlayerPoseOwnership.Body | PlayerPoseOwnership.NearArm |
        PlayerPoseOwnership.FarArm | PlayerPoseOwnership.Weapon;

    [Export(PropertyHint.File, "*.json")]
    public string AnimationMapPath { get; set; } = "res://assets/player/formal/player-animation-map.json";

    private readonly Dictionary<string, Node2D> _layers = new();
    private readonly Dictionary<string, Vector2> _restPositions = new();
    private readonly Dictionary<string, float> _restRotations = new();
    private readonly Dictionary<string, Vector2> _restScales = new();
    private readonly Dictionary<string, Color> _restModulates = new();
    private PlayerAnimationMap _map;
    private EntityPresentationFrame _legacyFrame = new(EntityVisualState.Idle, 1, true);
    private PlayerAnimationClip _baseClip = PlayerAnimationClip.Idle;
    private float _overrideSeconds;
    private bool _hasTypedFrame;
    private PlayerLocomotionState _lastLocomotionState;
    private PlayerActionState _lastActionState;
    private PlayerActionPhase _lastActionPhase;
    private float _lastActionNormalizedTime;
    private float _threadEntryBlend;
    private PlayerPose _lastActionTarget = PlayerPose.Identity;
    private Vector2 _lastActionAim;
    private int _lastActionFacing = 1;
    private PlayerPose _locomotionPose = PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
    private PlayerPose _locomotionFrom = PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
    private float _locomotionEntryBlend = 1f;
    private float _lastLocomotionPhase;
    private PlayerPose _actionFrom = PlayerPose.Identity;
    private PlayerPose _actionReleaseFrom = PlayerPose.Identity;
    private float _actionReleaseWeight;
    private float _lastActionOverlayWeight;

    public PlayerAnimationClip CurrentClip { get; private set; } = PlayerAnimationClip.Idle;
    public PlayerPresentationFrame CurrentPlayerFrame { get; private set; }
    public PlayerPose CurrentPose { get; private set; } =
        PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
    public PlayerPose CurrentActionPose { get; private set; } = PlayerPose.Identity;

    public override void _Ready()
    {
        EnsureMap();
        foreach (string name in RequiredLayers)
        {
            Node2D layer = GetNode<Node2D>(name);
            _layers.Add(name, layer);
            _restPositions.Add(name, layer.Position);
            _restRotations.Add(name, layer.Rotation);
            _restScales.Add(name, layer.Scale);
            _restModulates.Add(name, layer.Modulate);
        }
        SetProcess(true);
        ApplyFrame(_legacyFrame);
    }

    public override void _Process(double delta)
    {
        if (_overrideSeconds <= 0f) return;
        _overrideSeconds = Mathf.Max(0f, _overrideSeconds - (float)delta);
        if (!Mathf.IsZeroApprox(_overrideSeconds)) return;
        CurrentClip = _baseClip;
        ApplyPlayerFrameCore(CurrentPlayerFrame, preserveCueOverride: false,
            resetTransitions: false);
    }

    /// <summary>Primary production path. The complete typed frame owns the final rig pose.</summary>
    public void ApplyPlayerFrame(PlayerPresentationFrame frame)
    {
        EnsureMap();
        _overrideSeconds = 0f;
        ApplyPlayerFrameCore(frame, preserveCueOverride: false, resetTransitions: false);
    }

    /// <summary>Compatibility adapter for coarse entity consumers during migration.</summary>
    public void ApplyFrame(EntityPresentationFrame frame)
    {
        EnsureMap();
        _legacyFrame = frame;
        PlayerAnimationClip compatibilityClip = _map.Resolve(frame);
        var typed = new PlayerPresentationFrame(
            frame,
            PlayerLocomotionFrame.FromLegacy(frame),
            PlayerActionFrame.None,
            SecondaryMotionFrame.Identity);
        ApplyPlayerFrameCore(typed, preserveCueOverride: _overrideSeconds > 0f,
            resetTransitions: !_hasTypedFrame);
        _baseClip = compatibilityClip;
        if (_overrideSeconds <= 0f) CurrentClip = compatibilityClip;
    }

    /// <summary>Compatibility cue adapter. The next typed frame remains authoritative.</summary>
    public void PlayCue(PresentationCue cue)
    {
        EnsureMap();
        PlayerPresentationFrame basis = CurrentPlayerFrame;
        PlayerAnimationClip basisClip = _baseClip;
        CurrentClip = _map.ResolveCue(cue, CurrentPlayerFrame.Base);
        _overrideSeconds = cue switch
        {
            PresentationCue.Died => .55f,
            PresentationCue.Hurt => .18f,
            PresentationCue.ThreadFired or PresentationCue.ThreadHit => .24f,
            PresentationCue.AttackStarted or PresentationCue.Downstrike => .2f,
            _ => .12f
        };
        PlayerPresentationFrame cueFrame = CueFrame(cue, basis);
        ApplyPlayerFrameCore(cueFrame, preserveCueOverride: true, resetTransitions: true);
        CurrentPlayerFrame = basis;
        _legacyFrame = basis.Base;
        _baseClip = basisClip;
        CurrentClip = _map.ResolveCue(cue, basis.Base);
    }

    public void ClearCueOverride()
    {
        _overrideSeconds = 0f;
        CurrentClip = _baseClip;
        ApplyPlayerFrameCore(CurrentPlayerFrame, preserveCueOverride: false,
            resetTransitions: false);
    }

    private void ApplyPlayerFrameCore(
        PlayerPresentationFrame frame,
        bool preserveCueOverride,
        bool resetTransitions)
    {
        CurrentPlayerFrame = frame;
        _legacyFrame = frame.Base;
        Scale = new Vector2(frame.Base.Facing < 0 ? -1f : 1f, 1f);
        PlayerAnimationClip locomotionClip = _map.ResolveLocomotion(frame.Locomotion);
        _baseClip = locomotionClip;

        PlayerPose locomotionTarget = LocomotionTarget(frame.Locomotion);
        if (!_hasTypedFrame || resetTransitions)
        {
            _locomotionPose = locomotionTarget;
            _locomotionFrom = locomotionTarget;
            _lastLocomotionState = frame.Locomotion.State;
            _locomotionEntryBlend = 1f;
            _lastLocomotionPhase = frame.Locomotion.NormalizedTime;
        }
        else
        {
            if (frame.Locomotion.State != _lastLocomotionState)
            {
                _locomotionFrom = _locomotionPose;
                _lastLocomotionState = frame.Locomotion.State;
                // A freshly entered locomotion state can be published at its terminal phase
                // (for example an immediate landing). Starting from that phase would skip the
                // visual blend and pop the NearArm; presentation must always bridge from the
                // pose displayed on the previous fixed frame.
                _locomotionEntryBlend = 0f;
                _lastLocomotionPhase = frame.Locomotion.NormalizedTime;
            }
            else
            {
                float phaseDelta = frame.Locomotion.NormalizedTime - _lastLocomotionPhase;
                if (IsLooping(frame.Locomotion.State) && phaseDelta < 0f) phaseDelta += 1f;
                _locomotionEntryBlend = Mathf.Clamp(
                    _locomotionEntryBlend + Mathf.Max(.125f, phaseDelta), 0f, 1f);
                _lastLocomotionPhase = frame.Locomotion.NormalizedTime;
            }
            _locomotionPose = PlayerPoseMixer.Mix(_locomotionFrom, locomotionTarget,
                Smooth(_locomotionEntryBlend));
        }

        if (frame.Action.State == PlayerActionState.None)
        {
            if (_lastActionState != PlayerActionState.None)
            {
                _actionReleaseFrom = CurrentActionPose;
                _actionReleaseWeight = _lastActionOverlayWeight;
            }
            else _actionReleaseWeight = Mathf.Max(0f, _actionReleaseWeight - .1f);
            CurrentActionPose = PlayerPose.Identity;
            _actionFrom = PlayerPose.Identity;
            _lastActionState = PlayerActionState.None;
            _lastActionPhase = PlayerActionPhase.None;
            _lastActionNormalizedTime = 0f;
            _threadEntryBlend = 0f;
            _lastActionTarget = PlayerPose.Identity;
            _lastActionAim = Vector2.Zero;
            _lastActionFacing = frame.Base.Facing;
        }
        else
        {
            _actionReleaseWeight = 0f;
            PlayerPose actionTarget = ActionTarget(frame.Action, frame.Base.Facing);
            if (resetTransitions || frame.Action.State != _lastActionState ||
                frame.Action.Phase != _lastActionPhase)
            {
                _actionFrom = resetTransitions ? PlayerPose.Identity : CurrentActionPose;
                _lastActionState = frame.Action.State;
                _lastActionPhase = frame.Action.Phase;
                _lastActionNormalizedTime = frame.Action.NormalizedTime;
                _threadEntryBlend = IsThreadAction(frame.Action.State)
                    ? Mathf.Min(frame.Action.NormalizedTime, .25f)
                    : frame.Action.NormalizedTime;
            }
            else if (IsThreadAction(frame.Action.State))
            {
                float phaseDelta = Mathf.Max(0f,
                    frame.Action.NormalizedTime - _lastActionNormalizedTime);
                bool equivalentFacingRetarget =
                    (frame.Base.Facing != _lastActionFacing || frame.Action.Aim != _lastActionAim) &&
                    actionTarget == _lastActionTarget;
                if (!equivalentFacingRetarget)
                    _threadEntryBlend = Mathf.Clamp(
                        _threadEntryBlend + Mathf.Max(.125f, phaseDelta), 0f, 1f);
                _lastActionNormalizedTime = frame.Action.NormalizedTime;
            }
            float actionProgress = IsThreadAction(frame.Action.State)
                ? _threadEntryBlend
                : frame.Action.NormalizedTime;
            CurrentActionPose = PlayerPoseMixer.Mix(
                _actionFrom, actionTarget, Smooth(actionProgress));
            _lastActionTarget = actionTarget;
            _lastActionAim = frame.Action.Aim;
            _lastActionFacing = frame.Base.Facing;
        }

        float actionWeight = frame.Action.State == PlayerActionState.None
            ? _actionReleaseWeight
            : ActionOverlayWeight(frame.Action);
        _lastActionOverlayWeight = actionWeight;
        PlayerPose visibleActionPose = frame.Action.State == PlayerActionState.None
            ? _actionReleaseFrom
            : CurrentActionPose;
        PlayerPose mixed = PlayerPoseMixer.Mix(_locomotionPose, visibleActionPose, actionWeight);
        CurrentPose = ApplySecondary(mixed, frame.Secondary);
        ApplyPose(CurrentPose);
        if (!preserveCueOverride)
            CurrentClip = frame.Action.State == PlayerActionState.None
                ? locomotionClip
                : _map.ResolveAction(frame.Action);
        _hasTypedFrame = true;
    }

    private static PlayerPose LocomotionTarget(PlayerLocomotionFrame frame)
    {
        float p = frame.NormalizedTime;
        float wave = Mathf.Sin(p * Mathf.Tau);
        PlayerPose pose = PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
        return frame.State switch
        {
            PlayerLocomotionState.Idle => pose
                .With(PlayerPoseSlot.Body, Offset(0f, wave * .65f))
                .With(PlayerPoseSlot.NearArm, Rotation(.08f + wave * .035f))
                .With(PlayerPoseSlot.FarArm, Rotation(-wave * .02f)),
            PlayerLocomotionState.StartRun => pose
                .With(PlayerPoseSlot.Body, OffsetRotate(0f, -p * .8f, -.055f * p))
                .With(PlayerPoseSlot.NearLeg, Rotation(.18f * p))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.18f * p))
                .With(PlayerPoseSlot.Mantle, Rotation(-.08f * p)),
            PlayerLocomotionState.Run => pose
                .With(PlayerPoseSlot.Body, Offset(0f, -Mathf.Abs(wave) * 1.4f))
                .With(PlayerPoseSlot.NearLeg, Rotation(wave * .34f))
                .With(PlayerPoseSlot.FarLeg, Rotation(-wave * .34f))
                .With(PlayerPoseSlot.NearArm, Rotation(-wave * .24f))
                .With(PlayerPoseSlot.FarArm, Rotation(wave * .24f))
                .With(PlayerPoseSlot.Mantle, Rotation(-.09f - wave * .05f)),
            PlayerLocomotionState.Brake => pose
                .With(PlayerPoseSlot.Body, OffsetRotate(0f, p * 1.2f, .10f * (1f - p)))
                .With(PlayerPoseSlot.NearLeg, Rotation(.22f * (1f - p)))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.16f * (1f - p)))
                .With(PlayerPoseSlot.Mantle, Rotation(-.12f * (1f - p))),
            PlayerLocomotionState.JumpStart => pose
                .With(PlayerPoseSlot.Body, Offset(0f, 3f * (1f - p)))
                .With(PlayerPoseSlot.NearLeg, Rotation(-.28f * p))
                .With(PlayerPoseSlot.FarLeg, Rotation(.20f * p))
                .With(PlayerPoseSlot.NearArm, Rotation(-.18f * p)),
            PlayerLocomotionState.Rise => pose
                .With(PlayerPoseSlot.NearLeg, Rotation(-.32f))
                .With(PlayerPoseSlot.FarLeg, Rotation(.22f))
                .With(PlayerPoseSlot.NearArm, Rotation(-.22f))
                .With(PlayerPoseSlot.Mantle, Rotation(.13f)),
            PlayerLocomotionState.Apex => pose
                .With(PlayerPoseSlot.Body, Offset(0f, -1f))
                .With(PlayerPoseSlot.NearLeg, Rotation(-.08f))
                .With(PlayerPoseSlot.FarLeg, Rotation(.08f))
                .With(PlayerPoseSlot.Mantle, Rotation(-.03f)),
            PlayerLocomotionState.Fall => pose
                .With(PlayerPoseSlot.NearLeg, Rotation(.16f))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.18f))
                .With(PlayerPoseSlot.NearArm, Rotation(.28f))
                .With(PlayerPoseSlot.FarArm, Rotation(-.24f))
                .With(PlayerPoseSlot.Mantle, Rotation(-.2f)),
            PlayerLocomotionState.SoftLand => pose
                .With(PlayerPoseSlot.Body, Offset(0f, 3f * Mathf.Sin(p * Mathf.Pi)))
                .With(PlayerPoseSlot.NearLeg, Rotation(.18f * (1f - p)))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.18f * (1f - p)))
                .With(PlayerPoseSlot.Mantle, Rotation(.08f * (1f - p))),
            PlayerLocomotionState.HardLand => pose
                .With(PlayerPoseSlot.Body, OffsetRotate(0f, 5f * Mathf.Sin(p * Mathf.Pi), -.06f))
                .With(PlayerPoseSlot.NearLeg, Rotation(.30f * (1f - p)))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.30f * (1f - p)))
                .With(PlayerPoseSlot.Mantle, Rotation(.15f * (1f - p))),
            PlayerLocomotionState.Dash => pose
                .With(PlayerPoseSlot.Body, Rotation(.12f))
                .With(PlayerPoseSlot.Mantle, Rotation(-.28f))
                .With(PlayerPoseSlot.Weapon, Rotation(-.08f)),
            PlayerLocomotionState.Dead => pose
                .With(PlayerPoseSlot.Body, OffsetRotate(7f, 19f, 1.35f))
                .With(PlayerPoseSlot.Mantle, OffsetRotate(3f, 13f, 1.1f))
                .With(PlayerPoseSlot.Lantern, Offset(-7f, 15f))
                .With(PlayerPoseSlot.NearLeg, Rotation(.7f))
                .With(PlayerPoseSlot.FarLeg, Rotation(1.05f)),
            PlayerLocomotionState.Recovery => pose
                .With(PlayerPoseSlot.Body, Tint(new Color(.72f, 1f, .96f, .86f)))
                .With(PlayerPoseSlot.Lantern, Offset(0f, -5f)),
            _ => pose
        };
    }

    private static bool IsLooping(PlayerLocomotionState state) =>
        state is PlayerLocomotionState.Idle or PlayerLocomotionState.Run;

    private static PlayerPose ActionTarget(PlayerActionFrame frame, int facing)
    {
        PlayerPose pose = PlayerPose.Identity.WithOwnership(UpperBodyOwnership);
        if (frame.Phase == PlayerActionPhase.Recovery)
        {
            if (frame.State == PlayerActionState.ThreadFailed)
                return pose
                    .With(PlayerPoseSlot.Body, Rotate(-.13f))
                    .With(PlayerPoseSlot.NearArm, Rotate(.48f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.28f))
                    .With(PlayerPoseSlot.Weapon, Rotate(.22f));
            if (frame.State == PlayerActionState.ThreadApply)
                return AimThreadPose(pose
                    .With(PlayerPoseSlot.NearArm, Rotate(-.28f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.12f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.04f)), frame.Aim, facing);
            if (frame.State is not PlayerActionState.GroundAttack and
                not PlayerActionState.AirAttack and not PlayerActionState.DownAttack)
                return pose;
        }

        return frame.State switch
        {
            PlayerActionState.GroundAttack => frame.Phase switch
            {
                PlayerActionPhase.Anticipation => pose
                    .With(PlayerPoseSlot.Body, Rotate(.07f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-1f, 0f, -.46f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.18f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.32f)),
                PlayerActionPhase.Burst => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.11f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(2f, -1f, -1.04f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.30f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.86f)),
                PlayerActionPhase.Contact => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.08f))
                    .With(PlayerPoseSlot.NearArm, Rotate(-.72f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.24f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.55f)),
                _ => pose
            },
            PlayerActionState.AirAttack => frame.Phase switch
            {
                PlayerActionPhase.Anticipation => pose
                    .With(PlayerPoseSlot.Body, Rotate(.08f))
                    .With(PlayerPoseSlot.NearArm, Rotate(-.42f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.20f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.26f)),
                PlayerActionPhase.Burst => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.08f))
                    .With(PlayerPoseSlot.NearArm, Rotate(-1.18f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.40f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-1.02f)),
                PlayerActionPhase.Contact => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.13f))
                    .With(PlayerPoseSlot.NearArm, Rotate(-.86f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.30f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.74f)),
                _ => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.02f))
                    .With(PlayerPoseSlot.NearArm, Rotate(-.18f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.08f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.08f))
            },
            PlayerActionState.DownAttack => frame.Phase switch
            {
                PlayerActionPhase.Anticipation => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.08f))
                    .With(PlayerPoseSlot.NearArm, Rotate(.45f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.12f))
                    .With(PlayerPoseSlot.Weapon, Rotate(.36f)),
                PlayerActionPhase.Burst => pose
                    .With(PlayerPoseSlot.Body, Rotate(.08f))
                    .With(PlayerPoseSlot.NearArm, Rotate(1.28f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.26f))
                    .With(PlayerPoseSlot.Weapon, Rotate(1.38f)),
                PlayerActionPhase.Contact => pose
                    .With(PlayerPoseSlot.Body, Rotate(.12f))
                    .With(PlayerPoseSlot.NearArm, Rotate(.92f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.18f))
                    .With(PlayerPoseSlot.Weapon, Rotate(1.05f)),
                _ => pose
                    .With(PlayerPoseSlot.Body, Rotate(.02f))
                    .With(PlayerPoseSlot.NearArm, Rotate(.16f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.05f))
                    .With(PlayerPoseSlot.Weapon, Rotate(.12f))
            },
            PlayerActionState.ThreadPreview => AimThreadPose(pose
                .With(PlayerPoseSlot.NearArm, Rotate(-.52f))
                .With(PlayerPoseSlot.FarArm, Rotate(.16f))
                .With(PlayerPoseSlot.Weapon, Rotate(-.12f)), frame.Aim, facing),
            PlayerActionState.ThreadWindup => AimThreadPose(pose
                .With(PlayerPoseSlot.Body, Rotate(.06f))
                .With(PlayerPoseSlot.NearArm, Rotate(-.92f))
                .With(PlayerPoseSlot.FarArm, Rotate(.26f))
                .With(PlayerPoseSlot.Weapon, Rotate(-.24f)), frame.Aim, facing),
            PlayerActionState.ThreadThrow => AimThreadPose(pose
                .With(PlayerPoseSlot.Body, Rotate(-.10f))
                .With(PlayerPoseSlot.NearArm, Rotate(-1.28f))
                .With(PlayerPoseSlot.FarArm, Rotate(.34f))
                .With(PlayerPoseSlot.Weapon, Rotate(-.18f)), frame.Aim, facing),
            PlayerActionState.ThreadLatch => AimThreadPose(pose
                .With(PlayerPoseSlot.Body, Rotate(-.06f))
                .With(PlayerPoseSlot.NearArm, Rotate(-1.10f))
                .With(PlayerPoseSlot.FarArm, Rotate(.26f))
                .With(PlayerPoseSlot.Weapon, Rotate(-.14f)), frame.Aim, facing),
            PlayerActionState.ThreadApply => AimThreadPose(pose
                .With(PlayerPoseSlot.Body, OffsetRotate(-1f, 1f, .10f))
                .With(PlayerPoseSlot.NearArm, Rotate(-.78f))
                .With(PlayerPoseSlot.FarArm, Rotate(.48f))
                .With(PlayerPoseSlot.Weapon, Rotate(-.06f)), frame.Aim, facing),
            PlayerActionState.Hurt => pose
                .With(PlayerPoseSlot.Body, Rotate(-.16f))
                .With(PlayerPoseSlot.NearArm, Rotate(.44f))
                .With(PlayerPoseSlot.FarArm, Rotate(-.36f))
                .With(PlayerPoseSlot.Weapon, Tint(new Color(1f, .72f, .66f))),
            _ => pose
        };
    }

    private static float ActionOverlayWeight(PlayerActionFrame frame) =>
        frame.State == PlayerActionState.None ? 0f :
        frame.Phase == PlayerActionPhase.Recovery &&
        (frame.State is PlayerActionState.ThreadApply or PlayerActionState.ThreadFailed)
            ? 1f - Smooth(frame.NormalizedTime)
            : 1f;

    private static bool IsThreadAction(PlayerActionState state) => state is
        PlayerActionState.ThreadPreview or PlayerActionState.ThreadWindup or
        PlayerActionState.ThreadThrow or PlayerActionState.ThreadLatch or
        PlayerActionState.ThreadApply or PlayerActionState.ThreadFailed;

    private static PlayerPose AimThreadPose(PlayerPose pose, Vector2 worldAim, int facing)
    {
        Vector2 aim = new(worldAim.X * (facing < 0 ? -1f : 1f), worldAim.Y);
        if (aim.LengthSquared() <= .000001f) return pose;
        float angle = Mathf.Atan2(aim.Y, aim.X);
        return pose
            .With(PlayerPoseSlot.Body, Add(pose.Body, Rotate(angle * .12f)))
            .With(PlayerPoseSlot.NearArm, Add(pose.NearArm, Rotate(angle * .65f)))
            .With(PlayerPoseSlot.FarArm, Add(pose.FarArm, Rotate(angle * .18f)))
            .With(PlayerPoseSlot.Weapon, Add(pose.Weapon, Rotate(angle * .75f)));
    }

    private static PlayerPose ApplySecondary(PlayerPose pose, SecondaryMotionFrame secondary) => pose
        .With(PlayerPoseSlot.Mantle, Add(pose.Mantle, secondary.Mantle))
        .With(PlayerPoseSlot.Lantern, Add(pose.Lantern, secondary.Lantern))
        .With(PlayerPoseSlot.ShoulderCore, Add(pose.ShoulderCore, secondary.ShoulderSpool))
        .With(PlayerPoseSlot.Tether, Add(pose.Tether, secondary.Tether));

    private void ApplyPose(PlayerPose pose)
    {
        Apply("Body", pose.Body);
        Apply("NearArm", pose.NearArm);
        Apply("FarArm", pose.FarArm);
        Apply("NearLeg", pose.NearLeg);
        Apply("FarLeg", pose.FarLeg);
        Apply("Weapon", pose.Weapon);
        Apply("Mantle", pose.Mantle);
        Apply("ShoulderCore", pose.ShoulderCore);
        Apply("Lantern", pose.Lantern);
        Apply("Tether", pose.Tether);
    }

    private void Apply(string name, PoseTransform2D transform)
    {
        if (!_layers.TryGetValue(name, out Node2D layer)) return;
        layer.Position = _restPositions[name] + transform.Position;
        layer.Rotation = _restRotations[name] + transform.RotationRadians;
        layer.Scale = _restScales[name] * transform.Scale;
        layer.Modulate = _restModulates[name] * transform.Modulate;
    }

    private static PlayerPresentationFrame CueFrame(PresentationCue cue, PlayerPresentationFrame basis)
    {
        if (cue == PresentationCue.Died)
            return basis with
            {
                Locomotion = new PlayerLocomotionFrame(PlayerLocomotionState.Dead, .65f),
                Action = PlayerActionFrame.None
            };
        PlayerActionFrame action = cue switch
        {
            PresentationCue.AttackStarted => new(PlayerActionState.GroundAttack,
                PlayerActionPhase.Burst, .65f, false),
            PresentationCue.Downstrike => new(PlayerActionState.DownAttack,
                PlayerActionPhase.Burst, .65f, false),
            PresentationCue.Hurt => new(PlayerActionState.Hurt,
                PlayerActionPhase.Contact, .65f, false),
            PresentationCue.ThreadFailed => new(PlayerActionState.ThreadFailed,
                PlayerActionPhase.Recovery, .65f, true),
            PresentationCue.ThreadPreview => new(PlayerActionState.ThreadPreview,
                PlayerActionPhase.Anticipation, .65f, false),
            PresentationCue.ThreadWindup => new(PlayerActionState.ThreadWindup,
                PlayerActionPhase.Anticipation, .65f, false),
            PresentationCue.ThreadTravel or PresentationCue.ThreadFired => new(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, .65f, false),
            PresentationCue.ThreadLatched or PresentationCue.ThreadHit => new(PlayerActionState.ThreadLatch,
                PlayerActionPhase.Contact, .65f, false),
            PresentationCue.ThreadApplied => new(PlayerActionState.ThreadApply,
                PlayerActionPhase.Contact, .65f, false),
            PresentationCue.ThreadRecovery => new(PlayerActionState.ThreadApply,
                PlayerActionPhase.Recovery, .65f, true),
            _ => basis.Action
        };
        return basis with { Action = action };
    }

    private static PoseTransform2D Add(PoseTransform2D primary, PoseTransform2D additive) => new(
        primary.Position + additive.Position,
        primary.RotationRadians + additive.RotationRadians,
        primary.Scale * additive.Scale,
        primary.Modulate * additive.Modulate);

    private static PoseTransform2D Offset(float x, float y) =>
        PoseTransform2D.Identity with { Position = new Vector2(x, y) };
    private new static PoseTransform2D Rotate(float rotation) =>
        PoseTransform2D.Identity with { RotationRadians = rotation };
    private new static PoseTransform2D Rotation(float rotation) =>
        PoseTransform2D.Identity with { RotationRadians = rotation };
    private static PoseTransform2D OffsetRotate(float x, float y, float rotation) =>
        PoseTransform2D.Identity with { Position = new Vector2(x, y), RotationRadians = rotation };
    private static PoseTransform2D Tint(Color color) =>
        PoseTransform2D.Identity with { Modulate = color };
    private static float Smooth(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, 1f);
        return clamped * clamped * (3f - 2f * clamped);
    }

    private void EnsureMap() => _map ??= PlayerAnimationMap.Load(AnimationMapPath);
}
