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
    private const float ActionReleaseStep = .1f;
    private const float ThreadEntryLimit = .25f;
    private const float TimedEntrySeconds = 2f / 15f;
    private const float TimedReleaseSeconds = 1f / 6f;
    private double? _lastPresentationTime;

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
    private bool _lastOrdinaryAirTemplate;
    private PlayerActionState _lastActionState;
    private PlayerActionPhase _lastActionPhase;
    private float _lastActionNormalizedTime;
    private float _threadEntryBlend;
    private PlayerPose _lastActionTarget = PlayerPose.Identity;
    private Vector2 _lastActionAim;
    private int _lastActionFacing = 1;
    private bool _airAimRetargeting;
    private PlayerPose _locomotionPose = PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
    private PlayerPose _locomotionFrom = PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
    private float _locomotionEntryBlend = 1f;
    private float _lastLocomotionPhase;
    private PlayerPose _actionFrom = PlayerPose.Identity;
    private PlayerPose _actionReleaseFrom = PlayerPose.Identity;
    private float _actionReleaseWeight;
    private float _lastActionOverlayWeight;
    private bool _hasSecondaryGrip;
    private bool _secondaryGripWanted;
    private float _secondaryGripWeight = 1f;
    private float _secondaryGripFrom = 1f;
    private float _secondaryGripProgress;
    private float _secondaryGripReleaseStart;
    private Vector2 _attackWristOffset;
    private bool _airWristOwnsReach;
    private Vector2 _groundAttackWeightOffset;
    private int _groundAttackWeightFacing = 1;
    private bool _ordinaryAirEntered;
    private readonly Dictionary<string,(Vector2 Ankle,Vector2 Up)> _ordinaryDeparture=new();
    // Pure displayed load, never a retained thruster lease or final mixed pose.
    private bool _lastAirLoadWasJet, _airLoadHandoff, _freezeAirFeet, _airGapRecovery, _recoverAirFeet;
    private float _airLoadWorldAngle, _airLoadShift, _airLoadFromAngle, _airLoadFromShift;
    private float _airLoadProgress=1f, _airFootEntryLimit=1f;

    public PlayerAnimationClip CurrentClip { get; private set; } = PlayerAnimationClip.Idle;
    public PlayerPresentationFrame CurrentPlayerFrame { get; private set; }
    /// <summary>
    /// Logical compatibility aggregate, including the mantle secondary channel. The actual
    /// mantle node uses the pre-secondary base/action attachment; its Draw consumer owns hem lag.
    /// Arm Draw consumers own fixed-length elbow/wrist articulation after the ten typed slots.
    /// </summary>
    public PlayerPose CurrentPose { get; private set; } =
        PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
    public PlayerPose CurrentActionPose { get; private set; } = PlayerPose.Identity;
    /// <summary>Current world pose, including facing, action and secondary motion.</summary>
    public Vector2 LanternWorldPosition => GetNode<Node2D>("Lantern").GlobalPosition;
    /// <summary>Actual posed shoulder-core rim, including rigid lean and facing.</summary>
    public Vector2 ThrusterNozzleWorldPosition =>
        GetNode<Node2D>("ShoulderCore").ToGlobal(new Vector2(-8f, 0f));

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
        float? elapsed = ConsumePresentationTime(frame.PresentationTimeSeconds);
        CurrentPlayerFrame = frame;
        _legacyFrame = frame.Base;
        Scale = new Vector2(frame.Base.Facing < 0 ? -1f : 1f, 1f);
        // The layers are siblings: a bounded rigid root rotation preserves
        // every authored attachment and cannot stretch the image or collider.
        // Higher action/death/recovery poses still own the visible posture.
        base.Rotation = frame.Action.State != PlayerActionState.None ||
            frame.Locomotion.State is PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery ||
            !float.IsFinite(frame.MobilityLeanRadians)
            ? 0f : Mathf.Clamp(frame.MobilityLeanRadians, -.2f, .2f);
        PlayerAnimationClip locomotionClip = _map.ResolveLocomotion(frame.Locomotion);
        _baseClip = locomotionClip;

        bool ordinaryAirTemplate = HasOrdinaryAir(frame) && frame.Locomotion.State is
            (PlayerLocomotionState.JumpStart or PlayerLocomotionState.Rise or PlayerLocomotionState.Apex or PlayerLocomotionState.Fall);
        PlayerPose locomotionTarget = LocomotionTarget(frame.Locomotion, HasMeasuredLandingSupport(frame), HasAirJet(frame), ordinaryAirTemplate, HasGroundBrake(frame));
        if (!_hasTypedFrame || resetTransitions)
        {
            _locomotionPose = locomotionTarget;
            _locomotionFrom = locomotionTarget;
            _lastLocomotionState = frame.Locomotion.State;
            _lastOrdinaryAirTemplate = ordinaryAirTemplate;
            _locomotionEntryBlend = 1f;
            _lastLocomotionPhase = frame.Locomotion.NormalizedTime;
        }
        else
        {
            if (frame.Locomotion.State != _lastLocomotionState || ordinaryAirTemplate != _lastOrdinaryAirTemplate)
            {
                _locomotionFrom = _locomotionPose;
                _lastLocomotionState = frame.Locomotion.State;
                _lastOrdinaryAirTemplate = ordinaryAirTemplate;
                // The measured-air carrier can replace a legacy leg/body template
                // without changing Fall/Rise. Bridge that boundary as well, using
                // only locomotion history, never re-baking action or common loads.
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
                    _locomotionEntryBlend + (elapsed.HasValue ? elapsed.Value / TimedEntrySeconds : Mathf.Max(.125f, phaseDelta)), 0f, 1f);
                _lastLocomotionPhase = frame.Locomotion.NormalizedTime;
            }
            _locomotionPose = PlayerPoseMixer.Mix(_locomotionFrom, locomotionTarget,
                Smooth(_locomotionEntryBlend));
        }

        PlayerPose locomotion = _locomotionPose;
        if (frame.Action.State == PlayerActionState.None)
        {
            bool suppressRelease = frame.Locomotion.State is PlayerLocomotionState.Dead or
                PlayerLocomotionState.Recovery;
            if (suppressRelease)
            {
                _actionReleaseFrom = PlayerPose.Identity;
                _actionReleaseWeight = 0f;
            }
            else if (_lastActionState != PlayerActionState.None)
            {
                _actionReleaseFrom = CurrentActionPose;
                _actionReleaseWeight = _lastActionOverlayWeight;
            }
            else _actionReleaseWeight = Mathf.Max(0f, _actionReleaseWeight -
                (elapsed.HasValue ? elapsed.Value / TimedReleaseSeconds : ActionReleaseStep));
            CurrentActionPose = PlayerPose.Identity;
            _actionFrom = PlayerPose.Identity;
            _airAimRetargeting = false;
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
            bool airAimRetarget = !resetTransitions && frame.Action.State == PlayerActionState.AirAttack &&
                _lastActionState == PlayerActionState.AirAttack && frame.Action.Phase == _lastActionPhase &&
                frame.Action.Aim != _lastActionAim && actionTarget != _lastActionTarget;
            if (airAimRetarget) _airAimRetargeting = true;
            if (resetTransitions || frame.Action.State != _lastActionState ||
                frame.Action.Phase != _lastActionPhase)
            {
                _airAimRetargeting = false;
                _actionFrom = resetTransitions
                    ? PlayerPose.Identity
                    : CaptureVisibleActionPose(
                        locomotion,
                        // None clears the public action pose but may still draw a fading overlay.
                        _lastActionState == PlayerActionState.None ? _actionReleaseFrom : CurrentActionPose,
                        _lastActionOverlayWeight);
                _lastActionState = frame.Action.State;
                _lastActionPhase = frame.Action.Phase;
                _lastActionNormalizedTime = frame.Action.NormalizedTime;
                _threadEntryBlend = IsThreadAction(frame.Action.State)
                    ? elapsed.HasValue ? 0f : Mathf.Min(frame.Action.NormalizedTime, ThreadEntryLimit)
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
                        _threadEntryBlend + (elapsed.HasValue ? elapsed.Value / TimedEntrySeconds : Mathf.Max(.125f, phaseDelta)), 0f, 1f);
                _lastActionNormalizedTime = frame.Action.NormalizedTime;
            }
            float actionProgress = IsThreadAction(frame.Action.State)
                ? _threadEntryBlend
                : frame.Action.NormalizedTime;
            PlayerPose candidate = MixActionTransition(
                _actionFrom, actionTarget, locomotion, Smooth(actionProgress));
            // Same-phase directional retargeting must carry the whole visible
            // pose, not instantly switch shoulders/blade and discard a live
            // reach correction. Reuse the existing presentation elapsed time.
            CurrentActionPose = _airAimRetargeting && elapsed.HasValue
                ? MixActionTransition(CurrentActionPose, candidate, locomotion,
                    1f - Mathf.Exp(-elapsed.Value / TimedEntrySeconds))
                : candidate;
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
        PlayerPose mixed = PlayerPoseMixer.Mix(locomotion, visibleActionPose, actionWeight);
        // Common parent-space load must survive action ownership coherently:
        // replacing Body/arms cannot leave only the attached core/cloth loaded.
        mixed = ApplyBodyLoad(mixed, frame);
        mixed = ApplyGroundAttackWeight(mixed, frame, elapsed, resetTransitions);
        mixed = ApplyLandingLoad(mixed, frame);
        mixed = ApplyAirLoad(mixed, frame, elapsed, resetTransitions);
        CurrentPose = ApplySecondary(mixed, frame.Secondary);
        PrepareOrdinaryAirEntry(HasOrdinaryAir(frame));
        ApplyPose(CurrentPose, mixed.Mantle);
        ApplySecondaryGrip(frame, elapsed, resetTransitions);
        ApplyGroundGait(frame.Locomotion.State is PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery
            ? default : frame.Gait, HasAirJet(frame) ? frame.AirJet : default,
            HasOrdinaryAir(frame) ? frame.OrdinaryAir : default, _airFootEntryLimit, _freezeAirFeet, _recoverAirFeet);
        if (_layers.TryGetValue("Mantle", out Node2D mantle) && mantle is PlayerFormalLayer cloth)
            cloth.SetMantleSecondaryMotion(frame.Secondary.Mantle, frame.MantleMotionInParentSpace);
        if (!preserveCueOverride)
            CurrentClip = frame.Action.State == PlayerActionState.None
                ? locomotionClip
                : _map.ResolveAction(frame.Action);
        _hasTypedFrame = true;
    }

    private float? ConsumePresentationTime(double? time)
    {
        if(!time.HasValue) { _lastPresentationTime=null; return null; }
        double current=time.Value;
        if(!double.IsFinite(current) || current<0) return 0f;
        double elapsed=_lastPresentationTime.HasValue && current>=_lastPresentationTime.Value
            ? System.Math.Min(1d,current-_lastPresentationTime.Value) : 0d;
        _lastPresentationTime=current;
        return (float)elapsed;
    }

    private PlayerPose ApplyBodyLoad(PlayerPose pose, PlayerPresentationFrame frame)
    {
        var load=frame.BodyLoad;
        bool measuredBrake=HasGroundBrake(frame);
        if(measuredBrake) load=new(true,frame.GroundBrake.PelvisDrop,
            frame.GroundBrake.TorsoLeanWorld*(frame.Base.Facing<0?-1:1));
        if(!load.Observed || !frame.Base.Grounded ||
            frame.Locomotion.State is PlayerLocomotionState.Dash or PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery ||
            !float.IsFinite(load.PelvisDrop) || !float.IsFinite(load.TorsoLeanRadians)) return pose;
        Vector2 shift=new(0,Mathf.Clamp(load.PelvisDrop,0,measuredBrake?2.2f:1.5f));
        float limit=measuredBrake ? .13f : .1f;
        float angle=Mathf.Clamp(load.TorsoLeanRadians,-limit,limit);
        Vector2 pivot=(_restPositions["NearLeg"]+_restPositions["FarLeg"])*.5f;
        for(int i=0;i<RequiredLayers.Length;i++)
        {
            var slot=(PlayerPoseSlot)i;
            var original=pose.Get(slot);
            bool leg=slot is PlayerPoseSlot.NearLeg or PlayerPoseSlot.FarLeg;
            Vector2 position=original.Position+shift;
            if(!leg)
            {
                Vector2 rest=_restPositions[RequiredLayers[i]];
                position=pivot+(rest+original.Position-pivot).Rotated(angle)+shift-rest;
            }
            pose=pose.With(slot,original with { Position=position,RotationRadians=original.RotationRadians+(leg?0:angle) });
        }
        return pose;
    }

    private PlayerPose ApplyGroundAttackWeight(PlayerPose pose, PlayerPresentationFrame frame,
        float? elapsed, bool resetTransitions)
    {
        int facing = frame.Base.Facing < 0 ? -1 : 1;
        bool supported = frame.Base.Grounded && frame.Gait.Observed &&
            float.IsFinite(frame.Gait.Weight) && frame.Gait.Weight >= .999f &&
            frame.Gait.NearSoleWorld.IsFinite() && frame.Gait.FarSoleWorld.IsFinite() &&
            frame.Locomotion.State is not (PlayerLocomotionState.Dash or
                PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery);
        if (resetTransitions || !supported)
            _groundAttackWeightOffset = Vector2.Zero;
        else
        {
            // This cache is facing-local. Re-express it before tracking so a
            // mirrored rig cannot reverse an already visible world load.
            if (facing != _groundAttackWeightFacing)
                _groundAttackWeightOffset.X = -_groundAttackWeightOffset.X;
            Vector2 target = GroundAttackWeightTarget(frame.Action, facing);
            float weight = 1f - Mathf.Exp(-30f * (elapsed ?? 1f / 60f));
            _groundAttackWeightOffset = _groundAttackWeightOffset.Lerp(target, weight);
        }
        _groundAttackWeightFacing = facing;
        // Translate after measured body lean, then let the existing rigid
        // leg/hand solvers keep soles planted and the original weapon held.
        for (int i = 0; i < RequiredLayers.Length; i++)
        {
            var slot = (PlayerPoseSlot)i;
            var transform = pose.Get(slot);
            pose = pose.With(slot, transform with { Position = transform.Position + _groundAttackWeightOffset });
        }
        return pose;
    }

    private static Vector2 GroundAttackWeightTarget(PlayerActionFrame action, int facing)
    {
        if (action.State != PlayerActionState.GroundAttack) return Vector2.Zero;
        Vector2 windup = new(-1.4f, 1.4f), strike = new(1.8f, 2f), follow = new(.6f, .8f);
        float progress = Smooth(action.NormalizedTime);
        Vector2 offset = action.Phase switch
        {
            PlayerActionPhase.Anticipation => Vector2.Zero.Lerp(windup, progress),
            PlayerActionPhase.Burst => windup.Lerp(strike, progress),
            PlayerActionPhase.Contact => strike.Lerp(follow, progress),
            PlayerActionPhase.Recovery => follow.Lerp(Vector2.Zero, progress),
            _ => Vector2.Zero
        };
        offset.X *= action.Aim.X * facing;
        return offset;
    }

    private static bool HasMeasuredLandingSupport(PlayerPresentationFrame frame) =>
        frame.LandingLoad.Active && float.IsFinite(frame.LandingLoad.Compression) &&
        frame.LandingLoad.Compression > 0f && frame.Base.Grounded && frame.Gait.Observed &&
        float.IsFinite(frame.Gait.Weight) && frame.Gait.Weight >= .999f &&
        frame.Gait.NearSoleWorld.IsFinite() && frame.Gait.FarSoleWorld.IsFinite() &&
        frame.Locomotion.State is not (PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery);

    private static PlayerPose ApplyLandingLoad(PlayerPose pose, PlayerPresentationFrame frame)
    {
        if (!HasMeasuredLandingSupport(frame)) return pose;
        Vector2 shift = new(0f, Mathf.Clamp(frame.LandingLoad.Compression, 0f, 4f));
        // Common load follows action mixing. Hips move, but subsequent rigid leg
        // solving holds the observed world soles; the root/collider never moves.
        for (int i = 0; i < RequiredLayers.Length; i++)
        {
            var slot = (PlayerPoseSlot)i;
            var transform = pose.Get(slot);
            pose = pose.With(slot, transform with { Position = transform.Position + shift });
        }
        return pose;
    }

    private static bool HasAirJet(PlayerPresentationFrame frame) =>
        frame.AirJet.Active && !frame.Base.Grounded &&
        frame.Locomotion.State is not (PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery) &&
        float.IsFinite(frame.AirJet.TorsoLeanRadians) && float.IsFinite(frame.AirJet.PelvisLag) &&
        float.IsFinite(frame.AirJet.NearFold) && float.IsFinite(frame.AirJet.FarFold) && float.IsFinite(frame.AirJet.FootTrail);

    private static bool HasOrdinaryAir(PlayerPresentationFrame frame) =>
        frame.OrdinaryAir.Active && !frame.Base.Grounded && !HasAirJet(frame) &&
        frame.Locomotion.State is not (PlayerLocomotionState.Dash or PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery) &&
        frame.Action.State is not (PlayerActionState.ThreadLatch or PlayerActionState.ThreadApply) &&
        float.IsFinite(frame.OrdinaryAir.EntryWeight) && float.IsFinite(frame.OrdinaryAir.TorsoLeanRadians) &&
        float.IsFinite(frame.OrdinaryAir.PelvisOffset) && float.IsFinite(frame.OrdinaryAir.NearLift) &&
        float.IsFinite(frame.OrdinaryAir.FarLift) && float.IsFinite(frame.OrdinaryAir.FootTrail);

    private PlayerPose ApplyAirLoad(PlayerPose pose,PlayerPresentationFrame frame,float? elapsed,bool resetTransitions)
    {
        bool jet=HasAirJet(frame), ordinary=HasOrdinaryAir(frame);
        int facing=frame.Base.Facing<0?-1:1;
        bool invalid=frame.AirJet.Active && !jet || !jet && frame.OrdinaryAir.Active && !ordinary;
        _freezeAirFeet=_recoverAirFeet=false;
        if(resetTransitions || invalid)ClearAirLoad();
        if(!jet && !ordinary)
        {
            // A positive render can still repeat the zero-time primed physical
            // sample, leaving ordinary inactive. Release only a finite visual
            // residual; never require or resurrect a thruster lease.
            bool freeAir=!frame.Base.Grounded && frame.Locomotion.State is
                (PlayerLocomotionState.JumpStart or PlayerLocomotionState.Rise or PlayerLocomotionState.Apex or PlayerLocomotionState.Fall) &&
                frame.Action.State is not (PlayerActionState.ThreadLatch or PlayerActionState.ThreadApply);
            if(resetTransitions || invalid || !_lastAirLoadWasJet || !freeAir){ClearAirLoad();return pose;}
            if(!_airGapRecovery)
            {
                _airLoadFromAngle=_airLoadWorldAngle;_airLoadFromShift=_airLoadShift;
                _airLoadProgress=0;_airLoadHandoff=false;_airGapRecovery=true;
                CaptureAirFootDeparture();
            }
            else _airLoadProgress=Mathf.Clamp(_airLoadProgress+(elapsed??1f/60f)/TimedEntrySeconds,0,1);
            if(_airLoadProgress>=1){ClearAirLoad();return pose;}
            float weight=Smooth(_airLoadProgress);
            _airLoadWorldAngle=Mathf.Lerp(_airLoadFromAngle,0,weight);
            _airLoadShift=Mathf.Lerp(_airLoadFromShift,0,weight);
            _airFootEntryLimit=weight;_recoverAirFeet=true;_freezeAirFeet=weight==0;
        }
        else
        {
            _airGapRecovery=false;
            float targetAngle=(jet?Mathf.Clamp(frame.AirJet.TorsoLeanRadians,-.16f,.16f):
                Mathf.Clamp(frame.OrdinaryAir.TorsoLeanRadians,-.05f,.05f))*facing;
            float targetShift=jet?Mathf.Clamp(frame.AirJet.PelvisLag,-.8f,.8f):Mathf.Clamp(frame.OrdinaryAir.PelvisOffset,-.3f,.6f);
            if(ordinary && _lastAirLoadWasJet)
            {
                _airLoadFromAngle=_airLoadWorldAngle;_airLoadFromShift=_airLoadShift;
                _airLoadProgress=0;_airLoadHandoff=true;
            }
            else if(ordinary && _airLoadHandoff)
                _airLoadProgress=Mathf.Clamp(_airLoadProgress+(elapsed??1f/60f)/TimedEntrySeconds,0,1);
            else {_airLoadHandoff=false;_airLoadProgress=1;}
            float weight=_airLoadHandoff?Smooth(_airLoadProgress):1;
            _airLoadWorldAngle=Mathf.Lerp(_airLoadFromAngle,targetAngle,weight);
            _airLoadShift=Mathf.Lerp(_airLoadFromShift,targetShift,weight);
            _airFootEntryLimit=weight;
            _lastAirLoadWasJet=jet;
            if(_airLoadProgress>=1)_airLoadHandoff=false;
        }
        float angle=_airLoadWorldAngle*facing;
        Vector2 shift=new(0,_airLoadShift);
        Vector2 pivot=(_restPositions["NearLeg"]+_restPositions["FarLeg"])*.5f;
        for(int i=0;i<RequiredLayers.Length;i++)
        {
            var slot=(PlayerPoseSlot)i;var original=pose.Get(slot);Vector2 rest=_restPositions[RequiredLayers[i]];
            pose=pose.With(slot,original with
            {
                Position=pivot+(rest+original.Position-pivot).Rotated(angle)+shift-rest,
                RotationRadians=original.RotationRadians+angle
            });
        }
        return pose;
    }

    private void ClearAirLoad()
    {
        _lastAirLoadWasJet=_airLoadHandoff=_freezeAirFeet=_airGapRecovery=_recoverAirFeet=false;
        _airLoadWorldAngle=_airLoadShift=_airLoadFromAngle=_airLoadFromShift=0;
        _airLoadProgress=_airFootEntryLimit=1;
    }

    private void PrepareOrdinaryAirEntry(bool active)
    {
        if(active && !_ordinaryAirEntered)CaptureAirFootDeparture();
        _ordinaryAirEntered=active;
    }

    private void CaptureAirFootDeparture()
    {
        foreach(string name in new[]{"NearLeg","FarLeg"})
            if(_layers[name] is PlayerFormalLayer leg)
                _ordinaryDeparture[name]=(leg.LegAnkleLocal,(leg.LegAnkleLocal-leg.LegSoleLocal).Normalized());
    }

    private static bool HasGroundBrake(PlayerPresentationFrame frame) => frame.Base.Grounded &&
        frame.Gait.Observed && frame.Gait.Weight>=.999f && frame.GroundBrake.Observed &&
        float.IsFinite(frame.GroundBrake.Strength) && frame.GroundBrake.Strength>.0001f &&
        float.IsFinite(frame.GroundBrake.PelvisDrop) && float.IsFinite(frame.GroundBrake.TorsoLeanWorld);

    private static PlayerPose LocomotionTarget(PlayerLocomotionFrame frame, bool measuredLanding = false, bool measuredAir = false,
        bool measuredOrdinary=false,bool measuredBrake=false)
    {
        float p = frame.NormalizedTime;
        float wave = Mathf.Sin(p * Mathf.Tau);
        PlayerPose pose = PlayerPose.Identity.WithOwnership(PlayerPoseOwnership.All);
        PlayerPose target=frame.State switch
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
                .With(PlayerPoseSlot.Body, Offset(0f, measuredLanding ? 0f : 3f * Mathf.Sin(p * Mathf.Pi)))
                .With(PlayerPoseSlot.NearLeg, Rotation(.18f * (1f - p)))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.18f * (1f - p)))
                .With(PlayerPoseSlot.Mantle, Rotation(.08f * (1f - p))),
            PlayerLocomotionState.HardLand => pose
                .With(PlayerPoseSlot.Body, OffsetRotate(0f, measuredLanding ? 0f : 5f * Mathf.Sin(p * Mathf.Pi), -.06f))
                .With(PlayerPoseSlot.NearLeg, Rotation(.30f * (1f - p)))
                .With(PlayerPoseSlot.FarLeg, Rotation(-.30f * (1f - p)))
                .With(PlayerPoseSlot.Mantle, Rotation(.15f * (1f - p))),
            PlayerLocomotionState.Dash when measuredAir => pose,
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
        // Real ordinary-air joints carry the arc; old whole-leg/body templates
        // remain only for legacy callers without the measured channel.
        if(measuredBrake && frame.State==PlayerLocomotionState.Brake)
            target=target.With(PlayerPoseSlot.Body,pose.Body).With(PlayerPoseSlot.NearLeg,pose.NearLeg).With(PlayerPoseSlot.FarLeg,pose.FarLeg);
        return measuredOrdinary && frame.State is (PlayerLocomotionState.JumpStart or PlayerLocomotionState.Rise or
            PlayerLocomotionState.Apex or PlayerLocomotionState.Fall)
            ? target.With(PlayerPoseSlot.Body,pose.Body).With(PlayerPoseSlot.NearLeg,pose.NearLeg).With(PlayerPoseSlot.FarLeg,pose.FarLeg)
            : target;
    }

    private static bool IsLooping(PlayerLocomotionState state) =>
        state is PlayerLocomotionState.Idle or PlayerLocomotionState.Run;

    private static PlayerPose ActionTarget(PlayerActionFrame frame, int facing)
    {
        PlayerPoseOwnership ownership = frame.State == PlayerActionState.Shield
            ? UpperBodyOwnership | PlayerPoseOwnership.Mantle
            : UpperBodyOwnership;
        PlayerPose pose = PlayerPose.Identity.WithOwnership(ownership);
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
            if (frame.State == PlayerActionState.Shield)
                return AimShieldPose(pose
                    .With(PlayerPoseSlot.Body, OffsetRotate(-1f, 1f, -.12f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(2f, -2f, -1.12f))
                    .With(PlayerPoseSlot.FarArm, OffsetRotate(0f, -1f, -.68f))
                    .With(PlayerPoseSlot.Weapon, OffsetRotate(-1f, -3f, -.52f))
                    .With(PlayerPoseSlot.Mantle, OffsetRotate(-4f, 1f, .16f)),
                    frame.Aim, facing);
            if (frame.State is not PlayerActionState.GroundAttack and
                not PlayerActionState.AirAttack and not PlayerActionState.DownAttack)
                return pose;
        }

        return frame.State switch
        {
            // Ground Up-J owns an ascending cut, not a horizontal hitbox with
            // the same forward wrist/blade pose. Keep the planted lower body
            // and the existing phase clock; both hands still solve the rigid
            // bitmap handle after these shoulder/weapon targets are mixed.
            PlayerActionState.GroundAttack when frame.Aim == Vector2.Up => frame.Phase switch
            {
                PlayerActionPhase.Anticipation => pose
                    .With(PlayerPoseSlot.Body, Rotate(.10f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-2f, 1f, -.18f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.16f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.12f)),
                PlayerActionPhase.Burst => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.14f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-4f, -2f, -2.05f))
                    .With(PlayerPoseSlot.FarArm, OffsetRotate(4f, -1f, .34f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-1.80f)),
                PlayerActionPhase.Contact => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.10f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-3f, -1f, -1.80f))
                    .With(PlayerPoseSlot.FarArm, OffsetRotate(3f, -1f, .26f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-1.50f)),
                _ => pose
            },
            PlayerActionState.GroundAttack => frame.Phase switch
            {
                PlayerActionPhase.Anticipation => pose
                    .With(PlayerPoseSlot.Body, Rotate(.10f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-2f, 1f, .06f))
                    .With(PlayerPoseSlot.FarArm, Rotate(-.16f))
                    .With(PlayerPoseSlot.Weapon, Rotate(.12f)),
                PlayerActionPhase.Burst => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.14f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(3f, -2f, -1.06f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.34f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.92f)),
                PlayerActionPhase.Contact => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.10f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(1f, -1f, -.76f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.26f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.62f)),
                _ => pose
            },
            PlayerActionState.AirAttack when frame.Aim == Vector2.Up => frame.Phase switch
            {
                PlayerActionPhase.Anticipation => pose
                    .With(PlayerPoseSlot.Body, Rotate(.05f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-2f, 1f, -.34f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.16f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.12f)),
                PlayerActionPhase.Burst => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.11f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-4f, -2f, -2.05f))
                    .With(PlayerPoseSlot.FarArm, OffsetRotate(4f, -1f, .34f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-1.80f)),
                PlayerActionPhase.Contact => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.08f))
                    .With(PlayerPoseSlot.NearArm, OffsetRotate(-3f, -1f, -1.80f))
                    .With(PlayerPoseSlot.FarArm, OffsetRotate(3f, -1f, .26f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-1.50f)),
                _ => pose
                    .With(PlayerPoseSlot.Body, Rotate(-.02f))
                    .With(PlayerPoseSlot.NearArm, Rotate(-.18f))
                    .With(PlayerPoseSlot.FarArm, Rotate(.08f))
                    .With(PlayerPoseSlot.Weapon, Rotate(-.08f))
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
            PlayerActionState.Shield => AimShieldPose(pose
                .With(PlayerPoseSlot.Body, OffsetRotate(-1f, 1f, -.12f))
                .With(PlayerPoseSlot.NearArm, OffsetRotate(2f, -2f, -1.12f))
                .With(PlayerPoseSlot.FarArm, OffsetRotate(0f, -1f, -.68f))
                .With(PlayerPoseSlot.Weapon, OffsetRotate(-1f, -3f, -.52f))
                .With(PlayerPoseSlot.Mantle, OffsetRotate(-4f, 1f, .16f)),
                frame.Aim, facing),
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
        (frame.State is PlayerActionState.ThreadApply or PlayerActionState.ThreadFailed or
            PlayerActionState.Shield)
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

    private static PlayerPose AimShieldPose(PlayerPose pose, Vector2 worldAim, int facing)
    {
        Vector2 aim = new(worldAim.X * (facing < 0 ? -1f : 1f), worldAim.Y);
        if (aim.LengthSquared() <= .000001f) return pose;
        float angle = Mathf.Atan2(aim.Y, aim.X);
        return pose
            .With(PlayerPoseSlot.Body, Add(pose.Body, Rotate(angle * .07f)))
            .With(PlayerPoseSlot.NearArm, Add(pose.NearArm, Rotate(angle * .34f)))
            .With(PlayerPoseSlot.FarArm, Add(pose.FarArm, Rotate(angle * .22f)))
            .With(PlayerPoseSlot.Weapon, Add(pose.Weapon, Rotate(angle * .42f)));
    }

    private static PlayerPose CaptureVisibleActionPose(
        PlayerPose locomotion, PlayerPose action, float actionWeight)
    {
        float weight = Mathf.Clamp(actionWeight, 0f, 1f);
        if (weight <= 0f || action.Ownership == PlayerPoseOwnership.None)
            return PlayerPose.Identity;
        return PlayerPose.FromSlots(
            action.Ownership,
            Capture(PlayerPoseSlot.Body),
            Capture(PlayerPoseSlot.NearArm),
            Capture(PlayerPoseSlot.FarArm),
            Capture(PlayerPoseSlot.NearLeg),
            Capture(PlayerPoseSlot.FarLeg),
            Capture(PlayerPoseSlot.Weapon),
            Capture(PlayerPoseSlot.Mantle),
            Capture(PlayerPoseSlot.ShoulderCore),
            Capture(PlayerPoseSlot.Lantern),
            Capture(PlayerPoseSlot.Tether));

        PoseTransform2D Capture(PlayerPoseSlot slot) => action.Owns(slot)
            ? PoseTransform2D.Interpolate(locomotion.Get(slot), action.Get(slot), weight)
            : PoseTransform2D.Identity;
    }

    private static PlayerPose MixActionTransition(
        PlayerPose from, PlayerPose target, PlayerPose locomotion, float transitionWeight)
    {
        float weight = Mathf.Clamp(transitionWeight, 0f, 1f);
        PlayerPoseOwnership ownership = weight <= 0f
            ? from.Ownership
            : weight >= 1f
                ? target.Ownership
                : from.Ownership | target.Ownership;
        return PlayerPose.FromSlots(
            ownership,
            Transition(PlayerPoseSlot.Body),
            Transition(PlayerPoseSlot.NearArm),
            Transition(PlayerPoseSlot.FarArm),
            Transition(PlayerPoseSlot.NearLeg),
            Transition(PlayerPoseSlot.FarLeg),
            Transition(PlayerPoseSlot.Weapon),
            Transition(PlayerPoseSlot.Mantle),
            Transition(PlayerPoseSlot.ShoulderCore),
            Transition(PlayerPoseSlot.Lantern),
            Transition(PlayerPoseSlot.Tether));

        PoseTransform2D Transition(PlayerPoseSlot slot)
        {
            if (target.Owns(slot))
            {
                PoseTransform2D start = from.Owns(slot)
                    ? from.Get(slot)
                    : PoseTransform2D.Identity;
                return PoseTransform2D.Interpolate(start, target.Get(slot), weight);
            }
            if (from.Owns(slot))
                return PoseTransform2D.Interpolate(from.Get(slot), locomotion.Get(slot), weight);
            return PoseTransform2D.Identity;
        }
    }

    private static PlayerPose ApplySecondary(PlayerPose pose, SecondaryMotionFrame secondary) => pose
        .With(PlayerPoseSlot.Mantle, Add(pose.Mantle, secondary.Mantle))
        .With(PlayerPoseSlot.Lantern, Add(pose.Lantern, secondary.Lantern))
        .With(PlayerPoseSlot.ShoulderCore, Add(pose.ShoulderCore, secondary.ShoulderSpool))
        .With(PlayerPoseSlot.Tether, Add(pose.Tether, secondary.Tether));

    private void ApplyPose(PlayerPose pose, PoseTransform2D mantleAttachment)
    {
        Apply("Body", pose.Body);
        Apply("NearArm", pose.NearArm);
        Apply("FarArm", pose.FarArm);
        Apply("NearLeg", pose.NearLeg);
        Apply("FarLeg", pose.FarLeg);
        Apply("Weapon", pose.Weapon);
        // Only authored locomotion/action cues move the shoulder attachment. Secondary is
        // consumed once by the mantle's bounded Draw mapping, never by this whole-node transform.
        Apply("Mantle", mantleAttachment);
        Apply("ShoulderCore", pose.ShoulderCore);
        Apply("Lantern", pose.Lantern);
        Apply("Tether", pose.Tether);
    }

    private void ApplySecondaryGrip(PlayerPresentationFrame frame, float? elapsed, bool resetTransitions)
    {
        if (!_layers.TryGetValue("Weapon", out Node2D weaponLayer) || weaponLayer is not PlayerFormalLayer weapon ||
            !_layers.TryGetValue("NearArm", out Node2D nearLayer) || nearLayer is not PlayerFormalLayer near ||
            !_layers.TryGetValue("FarArm", out Node2D farLayer) || farLayer is not PlayerFormalLayer far) return;
        bool wanted = frame.Locomotion.State is not (PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery) &&
            frame.Action.State is PlayerActionState.None or PlayerActionState.GroundAttack or
                PlayerActionState.AirAttack or PlayerActionState.DownAttack;
        if (!_hasSecondaryGrip)
        {
            _secondaryGripWanted = wanted;
            _secondaryGripWeight = _secondaryGripFrom = wanted ? 1f : 0f;
            _hasSecondaryGrip = true;
        }
        else
        {
            bool changed = wanted != _secondaryGripWanted;
            if (changed)
            {
                _secondaryGripWanted = wanted;
                _secondaryGripFrom = _secondaryGripWeight;
                _secondaryGripProgress = 0f;
                _secondaryGripReleaseStart = _actionReleaseWeight;
            }
            float progress;
            if (frame.Action.State == PlayerActionState.None)
                progress = changed ? 0f : _secondaryGripReleaseStart > .0001f
                    ? 1f - _actionReleaseWeight / _secondaryGripReleaseStart
                    : 1f;
            else progress = IsThreadAction(frame.Action.State) ? _threadEntryBlend : frame.Action.NormalizedTime;
            // Consume the bounded visible-entry/release cadence even when the
            // ordinary producer enters at terminal progress or zero overlay.
            // No additional Process/timer; the ten authored slots stay intact.
            float step = frame.Action.State == PlayerActionState.None ? ActionReleaseStep : ThreadEntryLimit;
            if(elapsed.HasValue) step=elapsed.Value/(frame.Action.State==PlayerActionState.None ? TimedReleaseSeconds : 1f/15f);
            progress = Mathf.Min(progress, _secondaryGripProgress + step);
            _secondaryGripProgress = Mathf.Max(_secondaryGripProgress, Mathf.Clamp(progress, 0f, 1f));
            _secondaryGripWeight = Mathf.Lerp(_secondaryGripFrom, wanted ? 1f : 0f,
                Smooth(_secondaryGripProgress));
        }
        // Author the internal bend as well as the shoulder layer. It consumes
        // the same presentation elapsed time; repeated/paused frames do not
        // advance another clock, and no force or attack timing is changed.
        bool articulatedAirAttack = IsHorizontalAirAttack(frame.Action) || IsUpAirAttack(frame.Action);
        if (resetTransitions || frame.Locomotion.State is PlayerLocomotionState.Dead or PlayerLocomotionState.Recovery)
        {
            _attackWristOffset = Vector2.Zero;
            _airWristOwnsReach = false;
        }
        else
        {
            Vector2 target = AttackWristOffset(frame.Action);
            float weight = 1f - Mathf.Exp(-30f * (elapsed ?? 1f / 60f));
            _attackWristOffset = _attackWristOffset.Lerp(target, weight);
            _airWristOwnsReach |= articulatedAirAttack;
        }
        Vector2 guardImpact = StepGuardImpactWorld(frame, elapsed, resetTransitions);
        float freeHandWeight = 1f - _secondaryGripWeight;
        Vector2 nearWrist = new Vector2(0f, 21f) + _attackWristOffset +
            GuardImpactInLocal(near, guardImpact) * freeHandWeight;
        if (_airWristOwnsReach)
        {
            Vector2 reachable = KeepAirWristInOffhandReach(near, far, weapon, nearWrist);
            // A same-clock pose handoff must not remove a live correction.
            // During release it fades with the same continuous grip weight as
            // the offhand. Retire it when the next pose no longer needs it.
            if (!articulatedAirAttack && (reachable == nearWrist || _secondaryGripWeight <= 0f))
                _airWristOwnsReach = false;
            nearWrist = nearWrist.Lerp(reachable, _secondaryGripWeight);
        }
        near.SetArmWristLocal(nearWrist);
        // Solve the near wrist first, bind the original bitmap to that actual
        // hand, then solve its offhand. This order prevents one-frame grip lag.
        weapon.SetWeaponGripLocal(weapon.ToLocal(near.ToGlobal(near.ArmWristLocal)));
        Vector2 secondary = weapon.ToGlobal(weapon.WeaponSecondaryGripLocal);
        far.SetArmWristLocal(new Vector2(0f, 19f).Lerp(far.ToLocal(secondary), _secondaryGripWeight) +
            GuardImpactInLocal(far, guardImpact) * (.55f * freeHandWeight));
        weapon.SetWeaponSecondaryGripContact(_secondaryGripWeight >= .99999f &&
            far.ToGlobal(far.ArmWristLocal).DistanceTo(secondary) < .001f);
    }

    private static Vector2 KeepAirWristInOffhandReach(PlayerFormalLayer near, PlayerFormalLayer far,
        PlayerFormalLayer weapon, Vector2 nearWrist)
    {
        // Correct the visible wrist before either grip is solved. Moving the
        // primary moves the whole rigid weapon; clamping only the far arm
        // afterwards would leave it visibly detached from the bitmap handle.
        Vector2 delta = weapon.ToGlobal(PlayerFormalLayer.WeaponSecondaryGripOffsetLocal) - weapon.GlobalPosition;
        Vector2 requested = far.ToLocal(near.ToGlobal(nearWrist) + delta);
        const float reach = PlayerFormalLayer.MaxArmReach - .25f;
        float length = requested.Length();
        if (!float.IsFinite(length) || length <= reach) return nearWrist;
        Vector2 corrected = near.ToLocal(far.ToGlobal(requested * (reach / length)) - delta);
        // Do not ask the near IK to clamp the correction and invalidate the
        // offhand solution. Impossible poses retain honest grip reporting.
        return corrected.IsFinite() && corrected.LengthSquared() < PlayerFormalLayer.MaxArmReach * PlayerFormalLayer.MaxArmReach
            ? corrected : nearWrist;
    }

    private static bool IsHorizontalAirAttack(PlayerActionFrame action) =>
        action.State == PlayerActionState.AirAttack && (action.Aim == Vector2.Left || action.Aim == Vector2.Right);

    private static bool IsUpAirAttack(PlayerActionFrame action) =>
        action.State == PlayerActionState.AirAttack && action.Aim == Vector2.Up;

    private static Vector2 AttackWristOffset(PlayerActionFrame action)
    {
        Vector2 windup, strike, followThrough;
        if (action.State == PlayerActionState.GroundAttack)
        {
            bool upward = action.Aim == Vector2.Up;
            windup = upward ? new(-2f, -3.5f) : new(-2f, -3f);
            strike = upward ? new(.5f, 4.5f) : new(2f, 4.5f);
            followThrough = upward ? new(.25f, 2f) : new(1f, 2f);
        }
        else if (IsUpAirAttack(action))
        {
            windup = new(-2f, -4.5f);
            strike = new(.25f, 5.5f);
            followThrough = new(.25f, 2.5f);
        }
        else if (IsHorizontalAirAttack(action))
        {
            // Local pose; the authored rig mirrors it once. Tuck before
            // reaching through the swing, then share the contact/recovery
            // endpoint so the short aerial cut can recoil without a snap.
            windup = new(-1.5f, -5f);
            strike = new(2f, 6.5f);
            followThrough = new(1.5f, 3f);
        }
        else return Vector2.Zero;
        float progress = Smooth(action.NormalizedTime);
        return action.Phase switch
        {
            PlayerActionPhase.Anticipation => Vector2.Zero.Lerp(windup, progress),
            PlayerActionPhase.Burst => windup.Lerp(strike, progress),
            PlayerActionPhase.Contact => strike.Lerp(followThrough, progress),
            PlayerActionPhase.Recovery => followThrough.Lerp(Vector2.Zero, progress),
            _ => Vector2.Zero
        };
    }

    private void ApplyGroundGait(PlayerGroundGaitFrame gait, PlayerAirJetFrame air=default,PlayerOrdinaryAirFrame ordinary=default,
        float ordinaryEntryLimit=1f,bool freezeAirFeet=false,bool recoverAirFeet=false)
    {
        if(freezeAirFeet)return;
        bool valid=gait.Observed && float.IsFinite(gait.Weight) && gait.Weight>0 &&
            gait.NearSoleWorld.IsFinite() && gait.FarSoleWorld.IsFinite();
        foreach(string name in new[]{"NearLeg","FarLeg"})
        {
            if(!_layers.TryGetValue(name,out var node) || node is not PlayerFormalLayer leg) continue;
            Vector2 sole=leg.FreeLegSoleLocal, up=Vector2.Up;
            if(air.Active)
                sole+=new Vector2(Mathf.Clamp(air.FootTrail,-4f,4f),
                    -Mathf.Clamp(name=="NearLeg"?air.NearFold:air.FarFold,0f,name=="NearLeg"?11f:10f));
            else if(ordinary.Active || recoverAirFeet)
            {
                float lift=recoverAirFeet?0:Mathf.Clamp(name=="NearLeg"?ordinary.NearLift:ordinary.FarLift,
                    name=="NearLeg"?-2f:-1f,name=="NearLeg"?12f:10f);
                float trail=recoverAirFeet?0:Mathf.Clamp(ordinary.FootTrail,-3f,3f);
                Vector2 targetAnkle=sole+new Vector2(trail,-lift)+Vector2.Up*4f;
                var departure=_ordinaryDeparture[name];
                float entry=Mathf.Min(recoverAirFeet?1:Mathf.Clamp(ordinary.EntryWeight,0f,1f),ordinaryEntryLimit);
                up=departure.Up.Rotated(Mathf.AngleDifference(departure.Up.Angle(),Vector2.Up.Angle())*entry).Normalized();
                sole=departure.Ankle.Lerp(targetAnkle,entry)-up*4f;
            }
            else if(valid)
            {
                Vector2 target=name=="NearLeg"?gait.NearSoleWorld:gait.FarSoleWorld;
                sole=leg.ToLocal(leg.ToGlobal(sole).Lerp(target,Mathf.Clamp(gait.Weight,0,1)));
                Vector2 worldUp=gait.GroundUp.IsFinite() && gait.GroundUp.LengthSquared()>.001f ? gait.GroundUp.Normalized() : Vector2.Up;
                up=(leg.ToLocal(leg.GlobalPosition+worldUp)-leg.ToLocal(leg.GlobalPosition)).Normalized();
            }
            leg.SetLegSoleLocal(sole,up);
        }
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
