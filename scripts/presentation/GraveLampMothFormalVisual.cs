using Godot;
using Knight.Combat;
using Knight.Enemies.Moth;

namespace Knight.Presentation;

public partial class GraveLampMothFormalVisual : Node2D, IEntityVisual2D
{
    private const float CorruptionCircleRadius = 11f;
    private const int HurtFlashPhysicsFrames = 3;
    private GraveLampMothState _state;
    private GraveLampMoth _attackSource;
    private float _phase;
    private int _hurtFlashFrames;
    private EnemyFormalVisualPart _body;
    private Node2D _wings;
    private EnemyFormalVisualPart _farWing;
    private EnemyFormalVisualPart _nearWing;
    private EnemyFormalVisualPart _lamp;
    private EnemyFormalVisualPart _corruption;
    private Vector2 _bodyRest;
    private Vector2 _wingsRest;
    private Vector2 _farWingRest;
    private Vector2 _nearWingRest;
    private Vector2 _lampRest;
    private Vector2 _corruptionRest;
    private PoseTransform2D _bodyPose = PoseTransform2D.Identity;
    private PoseTransform2D _wingPose = PoseTransform2D.Identity;
    private PoseTransform2D _lampPose = PoseTransform2D.Identity;
    private PoseTransform2D _corruptionPose = PoseTransform2D.Identity;

    public float CurrentContactIntensity { get; private set; }
    public float CurrentHurtFlashIntensity => _hurtFlashFrames > 0 ? 1f : 0f;
    public AttackPresentationFrame CurrentAttackFrame { get; private set; }
    public PoseTransform2D CurrentBodyTransform => IndependentComponent(_body, _bodyRest);
    public PoseTransform2D CurrentWingTransform => IndependentComponent(_wings, _wingsRest);
    public PoseTransform2D CurrentFarWingTransform => IndependentComponent(_farWing, _farWingRest);
    public PoseTransform2D CurrentNearWingTransform => IndependentComponent(_nearWing, _nearWingRest);
    public PoseTransform2D CurrentLampTransform => ChildComponent(_lamp, _lampRest);
    public PoseTransform2D CurrentWeaponTransform => CurrentLampTransform;
    public PoseTransform2D CurrentCorruptionTransform => IndependentComponent(_corruption, _corruptionRest);
    public Rect2 CurrentCorruptionBounds => CorruptionBounds();
    public string CurrentAnimation { get; private set; } = "hover";
    public string CurrentPoseMarker { get; private set; } = "moth_hover";

    public override void _Ready()
    {
        EnsureParts();
        _attackSource = GetParent()?.GetParent() as GraveLampMoth;
        if (_attackSource != null) _attackSource.AttackFrameChanged += PresentAttack;
        ApplySlots();
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        if (CurrentAttackFrame.Timeline.Phase == AttackPhase.None)
            _wingPose = Pose(Vector2.Zero, BaseWingRotation());
        ApplySlots();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_hurtFlashFrames <= 0) return;
        _hurtFlashFrames--;
        ApplySlots();
    }

    public void ApplyMothFrame(GraveLampMothFrame frame, int facing, bool grounded)
    {
        _state = frame.State;
        CurrentAnimation = frame.State switch
        {
            GraveLampMothState.Hover => "hover",
            GraveLampMothState.Align => "fly",
            GraveLampMothState.Telegraph => "telegraph",
            GraveLampMothState.Dive => "dive",
            GraveLampMothState.Recover => "recover",
            GraveLampMothState.ThreadStunned => "thread_pull",
            GraveLampMothState.Dead => "dead",
            _ => "hover"
        };
        CurrentPoseMarker = frame.State switch
        {
            GraveLampMothState.Telegraph => "moth_folded_wings",
            GraveLampMothState.Dive => "moth_dive",
            GraveLampMothState.Recover => "moth_landing_recover",
            _ => "moth_hover"
        };
        ApplyAttackTimeline(frame.AttackTimeline);
        Scale = new Vector2(facing < 0 ? -1f : 1f, 1f);
    }

    public void PresentAttack(AttackPresentationFrame frame)
    {
        CurrentAttackFrame = frame;
        ApplyAttackTimeline(frame.Timeline);
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        GraveLampMothState state = frame.State switch
        {
            EntityVisualState.Move => GraveLampMothState.Align,
            EntityVisualState.Attack => GraveLampMothState.Dive,
            EntityVisualState.Hurt => GraveLampMothState.ThreadStunned,
            EntityVisualState.Dead => GraveLampMothState.Dead,
            _ => GraveLampMothState.Hover
        };
        ApplyMothFrame(new GraveLampMothFrame(state, 0f, 0f, false, false), frame.Facing, frame.Grounded);
    }

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _hurtFlashFrames = HurtFlashPhysicsFrames;
        ApplySlots();
    }

    private void ApplyAttackTimeline(AttackTimelineFrame timeline)
    {
        if (timeline.Phase == AttackPhase.None || timeline.Kind != EnemyAttackKind.MothDive)
        {
            CurrentAttackFrame = default;
            _bodyPose = BaseBodyTransform();
            _wingPose = Pose(Vector2.Zero, BaseWingRotation());
            _lampPose = PoseTransform2D.Identity;
            _corruptionPose = PoseTransform2D.Identity;
            CurrentContactIntensity = 0f;
            ApplySlots();
            return;
        }

        float progress = Progress(timeline);
        CurrentContactIntensity = Contact(timeline, progress);
        _corruptionPose = new PoseTransform2D(new Vector2(-18f, -3f), 0f, Vector2.One, Colors.White);
        PoseTransform2D telegraphBody = Pose(new Vector2(0f, 3f), -.10f);
        PoseTransform2D telegraphWing = Pose(new Vector2(0f, 1f), .72f);
        PoseTransform2D telegraphLamp = new(Vector2.Zero, 0f, Vector2.One * 1.14f, Colors.White);
        PoseTransform2D activeBody = Pose(new Vector2(17f, 8f), .82f);
        PoseTransform2D activeWing = Pose(new Vector2(-2f, 2f), -.28f);
        PoseTransform2D activeLamp = new(new Vector2(4f, 1f), .04f, Vector2.One * 1.08f, Colors.White);
        PoseTransform2D recoveryBody = Pose(Vector2.Zero, -.18f);
        switch (timeline.Phase)
        {
            case AttackPhase.Telegraph:
                CurrentPoseMarker = "moth_folded_wings";
                _bodyPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphBody, Ease(progress));
                _wingPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphWing, Delayed(progress));
                _lampPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphLamp, Delayed(progress));
                break;
            case AttackPhase.Active:
                CurrentPoseMarker = "moth_dive";
                _bodyPose = PoseTransform2D.Interpolate(telegraphBody, activeBody, Ease(progress));
                _wingPose = PoseTransform2D.Interpolate(telegraphWing, activeWing, Delayed(progress));
                _lampPose = PoseTransform2D.Interpolate(telegraphLamp, activeLamp, Delayed(progress));
                break;
            case AttackPhase.Recovery:
                CurrentPoseMarker = "moth_landing_recover";
                _bodyPose = PoseTransform2D.Interpolate(activeBody, recoveryBody, Ease(progress));
                _wingPose = PoseTransform2D.Interpolate(activeWing, PoseTransform2D.Identity, Delayed(progress));
                _lampPose = PoseTransform2D.Interpolate(activeLamp, PoseTransform2D.Identity, Delayed(progress));
                break;
        }
        ApplySlots();
    }

    private void EnsureParts()
    {
        if (_body != null) return;
        _body = GetNode<EnemyFormalVisualPart>("Body");
        _wings = GetNode<Node2D>("Wings");
        _farWing = GetNode<EnemyFormalVisualPart>("Wings/FarWing");
        _nearWing = GetNode<EnemyFormalVisualPart>("Wings/NearWing");
        _lamp = GetNode<EnemyFormalVisualPart>("Lamp");
        _corruption = GetNode<EnemyFormalVisualPart>("Corruption");
        _bodyRest = _body.Position;
        _wingsRest = _wings.Position;
        _farWingRest = _farWing.Position;
        _nearWingRest = _nearWing.Position;
        _lampRest = _lamp.Position;
        _corruptionRest = _corruption.Position;
    }

    private void ApplySlots()
    {
        EnsureParts();
        ApplyIndependent(_body, _bodyRest, _bodyPose);
        ApplyIndependent(_wings, _wingsRest,
            new PoseTransform2D(_wingPose.Position, 0f, Vector2.One, Colors.White));
        ApplyIndependent(_farWing, _farWingRest,
            new PoseTransform2D(Vector2.Zero, _wingPose.RotationRadians,
                _wingPose.Scale, _wingPose.Modulate));
        ApplyIndependent(_nearWing, _nearWingRest,
            new PoseTransform2D(Vector2.Zero, -_wingPose.RotationRadians,
                _wingPose.Scale, _wingPose.Modulate));
        ApplyChild(_lamp, _lampRest, _bodyPose, _lampPose);
        ApplyIndependent(_corruption, _corruptionRest, _corruptionPose);
        _body.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _farWing.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _nearWing.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _lamp.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _corruption.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
    }

    private PoseTransform2D ChildComponent(Node2D node, Vector2 rest)
    {
        PoseTransform2D body = CurrentBodyTransform;
        return new PoseTransform2D(node.Position - rest - body.Position,
            Mathf.AngleDifference(body.RotationRadians, node.Rotation), node.Scale, node.Modulate);
    }

    private static PoseTransform2D IndependentComponent(Node2D node, Vector2 rest) => node == null
        ? PoseTransform2D.Identity
        : new PoseTransform2D(node.Position - rest, node.Rotation, node.Scale, node.Modulate);
    private static void ApplyIndependent(Node2D node, Vector2 rest, PoseTransform2D pose)
    {
        node.Position = rest + pose.Position;
        node.Rotation = pose.RotationRadians;
        node.Scale = pose.Scale;
        node.Modulate = pose.Modulate;
    }
    private static void ApplyChild(Node2D node, Vector2 rest, PoseTransform2D body, PoseTransform2D part)
    {
        node.Position = rest + body.Position + part.Position;
        node.Rotation = body.RotationRadians + part.RotationRadians;
        node.Scale = part.Scale;
        node.Modulate = part.Modulate;
    }
    private Rect2 CorruptionBounds()
    {
        EnsureParts();
        Vector2 radius = Vector2.One * CorruptionCircleRadius * _corruption.Scale.Abs();
        return new Rect2(_corruption.Position - radius, radius * 2f);
    }
    private PoseTransform2D BaseBodyTransform() => _state switch
    {
        GraveLampMothState.ThreadStunned => Pose(Vector2.Zero, -.5f),
        GraveLampMothState.Dead => Pose(Vector2.Zero, 1.4f),
        GraveLampMothState.Recover => Pose(Vector2.Zero, -.18f),
        _ => PoseTransform2D.Identity
    };
    private float BaseWingRotation() => _state switch
    {
        GraveLampMothState.ThreadStunned => -.5f,
        GraveLampMothState.Dead => 1.4f,
        GraveLampMothState.Recover => -.18f,
        _ => Mathf.Sin(_phase * 8f) * .22f
    };
    private static float Progress(AttackTimelineFrame timeline) => timeline.PhaseDurationSeconds <= 0f ? 1f :
        Mathf.Clamp(1f - timeline.PhaseRemainingSeconds / timeline.PhaseDurationSeconds, 0f, 1f);
    private static float Ease(float value) => value * value * (3f - 2f * value);
    private static float Delayed(float value) => Ease(Mathf.Clamp((value - .18f) / .82f, 0f, 1f));
    private static float Contact(AttackTimelineFrame timeline, float progress) => timeline.Phase != AttackPhase.Active ? 0f :
        Mathf.Clamp(1f - progress / Mathf.Min(1f, .05f / Mathf.Max(.0001f, timeline.PhaseDurationSeconds)), 0f, 1f);
    private static PoseTransform2D Pose(Vector2 position, float rotation) =>
        new(position, rotation, Vector2.One, Colors.White);

    public override void _ExitTree()
    {
        if (IsInstanceValid(_attackSource)) _attackSource.AttackFrameChanged -= PresentAttack;
    }
}
