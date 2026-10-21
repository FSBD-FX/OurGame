using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;

namespace Knight.Presentation;

public partial class BellBackLaborerFormalVisual : Node2D, IEntityVisual2D
{
    private const float CorruptionCircleRadius = 12f;
    private const int HurtFlashPhysicsFrames = 3;
    private BellBackLaborerState _state;
    private BellBackLaborer _attackSource;
    private float _phase;
    private int _hurtFlashFrames;
    private PoseTransform2D _weaponExitFrom = PoseTransform2D.Identity;
    private PoseTransform2D _bellExitFrom = PoseTransform2D.Identity;
    private float _attackExitBlend = 1f;
    private EnemyFormalVisualPart _feet;
    private EnemyFormalVisualPart _body;
    private EnemyFormalVisualPart _bell;
    private EnemyFormalVisualPart _weapon;
    private EnemyFormalVisualPart _corruption;
    private Vector2 _feetRest;
    private Vector2 _bodyRest;
    private Vector2 _bellRest;
    private Vector2 _weaponRest;
    private Vector2 _corruptionRest;
    private PoseTransform2D _feetPose = PoseTransform2D.Identity;
    private PoseTransform2D _bodyPose = PoseTransform2D.Identity;
    private PoseTransform2D _bellPose = PoseTransform2D.Identity;
    private PoseTransform2D _weaponPose = PoseTransform2D.Identity;
    private PoseTransform2D _corruptionPose = PoseTransform2D.Identity;

    public float CurrentContactIntensity { get; private set; }
    public float CurrentHurtFlashIntensity => _hurtFlashFrames > 0 ? 1f : 0f;
    public AttackPresentationFrame CurrentAttackFrame { get; private set; }
    public PoseTransform2D CurrentBodyTransform => IndependentComponent(_body, _bodyRest);
    public PoseTransform2D CurrentWeaponTransform => ChildComponent(_weapon, _weaponRest);
    public PoseTransform2D CurrentBellTransform => ChildComponent(_bell, _bellRest);
    public PoseTransform2D CurrentFeetTransform => ChildComponent(_feet, _feetRest);
    public PoseTransform2D CurrentCorruptionTransform => IndependentComponent(_corruption, _corruptionRest);
    public Rect2 CurrentCorruptionBounds => CorruptionBounds();
    public string CurrentAnimation { get; private set; } = "idle";
    public string CurrentPoseMarker { get; private set; } = "laborer_idle";

    public override void _Ready()
    {
        EnsureParts();
        _attackSource = GetParent()?.GetParent() as BellBackLaborer;
        if (_attackSource != null) _attackSource.AttackFrameChanged += PresentAttack;
        ApplySlots();
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        ApplySlots();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_hurtFlashFrames <= 0) return;
        _hurtFlashFrames--;
        ApplySlots();
    }

    public void ApplyLaborerFrame(BellBackLaborerFrame frame, int facing)
    {
        _state = frame.State;
        CurrentAnimation = frame.State switch
        {
            BellBackLaborerState.Idle => "idle",
            BellBackLaborerState.Approach => "approach",
            BellBackLaborerState.Guard => "guard",
            BellBackLaborerState.Telegraph => "telegraph",
            BellBackLaborerState.Attack => "attack",
            BellBackLaborerState.Exposed => "exposed",
            BellBackLaborerState.Recover => "recover",
            BellBackLaborerState.Dead => "dead",
            _ => "idle"
        };
        CurrentPoseMarker = frame.State switch
        {
            BellBackLaborerState.Telegraph => "laborer_counter_twist",
            BellBackLaborerState.Attack => "laborer_sweep",
            BellBackLaborerState.Recover => "laborer_hammer_drag",
            _ => "laborer_idle"
        };
        ApplyAttackTimeline(frame.AttackTimeline, false);
        Scale = new Vector2(facing < 0 ? -1f : 1f, 1f);
    }

    public void PresentAttack(AttackPresentationFrame frame)
    {
        bool exitedSweep = CurrentAttackFrame.Timeline.Phase != AttackPhase.None &&
            frame.Timeline.Phase == AttackPhase.None;
        if (exitedSweep)
        {
            _weaponExitFrom = CurrentWeaponTransform;
            _bellExitFrom = CurrentBellTransform;
            _attackExitBlend = 0f;
        }
        CurrentAttackFrame = frame;
        ApplyAttackTimeline(frame.Timeline, true);
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        BellBackLaborerState state = frame.State switch
        {
            EntityVisualState.Move => BellBackLaborerState.Approach,
            EntityVisualState.Attack => BellBackLaborerState.Attack,
            EntityVisualState.Hurt => BellBackLaborerState.Exposed,
            EntityVisualState.Dead => BellBackLaborerState.Dead,
            _ => BellBackLaborerState.Idle
        };
        ApplyLaborerFrame(new BellBackLaborerFrame(state, 0f, false, false, false), frame.Facing);
    }

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _hurtFlashFrames = HurtFlashPhysicsFrames;
        ApplySlots();
    }

    private void ApplyAttackTimeline(AttackTimelineFrame timeline, bool advanceExitBlend)
    {
        if (timeline.Phase == AttackPhase.None || timeline.Kind != EnemyAttackKind.LaborerSweep)
        {
            CurrentAttackFrame = default;
            _bodyPose = BaseBodyTransform();
            PoseTransform2D baseWeapon = Pose(Vector2.Zero, BaseWeaponRotation());
            if (advanceExitBlend && _attackExitBlend < 1f)
                _attackExitBlend = Mathf.Min(1f, _attackExitBlend + .125f);
            _weaponPose = _attackExitBlend < 1f
                ? PoseTransform2D.Interpolate(_weaponExitFrom, baseWeapon, Ease(_attackExitBlend))
                : baseWeapon;
            PoseTransform2D baseBell = _state == BellBackLaborerState.Exposed
                ? Pose(new Vector2(0f, -34f), -.7f) : PoseTransform2D.Identity;
            _bellPose = _attackExitBlend < 1f
                ? PoseTransform2D.Interpolate(_bellExitFrom, baseBell, Ease(_attackExitBlend))
                : baseBell;
            _feetPose = PoseTransform2D.Identity;
            _corruptionPose = PoseTransform2D.Identity;
            CurrentContactIntensity = 0f;
            ApplySlots();
            return;
        }

        _attackExitBlend = 1f;
        float progress = Progress(timeline);
        CurrentContactIntensity = Contact(timeline, progress);
        _corruptionPose = new PoseTransform2D(new Vector2(-20f, -4f), 0f, Vector2.One, Colors.White);
        PoseTransform2D telegraphFeet = Pose(new Vector2(0f, 4f), 0f);
        PoseTransform2D telegraphBody = Pose(new Vector2(-2f, 3f), -.24f);
        PoseTransform2D telegraphWeapon = Pose(new Vector2(-8f, -4f), -1.02f);
        PoseTransform2D telegraphBell = Pose(new Vector2(2f, -1f), .16f);
        PoseTransform2D activeFeet = Pose(new Vector2(0f, 2f), 0f);
        PoseTransform2D activeBody = Pose(new Vector2(4f, 2f), .28f);
        PoseTransform2D activeWeapon = Pose(new Vector2(15f, 4f), .22f);
        PoseTransform2D activeBell = Pose(new Vector2(-4f, 2f), -.12f);
        PoseTransform2D recoveryBody = PoseTransform2D.Identity;
        PoseTransform2D recoveryWeapon = Pose(new Vector2(0f, 7f), .24f);
        switch (timeline.Phase)
        {
            case AttackPhase.Telegraph:
                CurrentPoseMarker = "laborer_counter_twist";
                _feetPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphFeet, Ease(progress));
                _bodyPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphBody, Ease(progress));
                _weaponPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphWeapon, Ease(progress));
                _bellPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphBell, Delayed(progress));
                break;
            case AttackPhase.Active:
                CurrentPoseMarker = "laborer_sweep";
                _feetPose = PoseTransform2D.Interpolate(telegraphFeet, activeFeet, Ease(progress));
                _bodyPose = PoseTransform2D.Interpolate(telegraphBody, activeBody, Ease(progress));
                _weaponPose = PoseTransform2D.Interpolate(telegraphWeapon, activeWeapon, Ease(progress));
                _bellPose = PoseTransform2D.Interpolate(telegraphBell, activeBell, Delayed(progress));
                break;
            case AttackPhase.Recovery:
                CurrentPoseMarker = "laborer_hammer_drag";
                _feetPose = PoseTransform2D.Interpolate(activeFeet, PoseTransform2D.Identity, Ease(progress));
                _bodyPose = PoseTransform2D.Interpolate(activeBody, recoveryBody, Ease(progress));
                _weaponPose = PoseTransform2D.Interpolate(activeWeapon, recoveryWeapon, Ease(progress));
                _bellPose = PoseTransform2D.Interpolate(activeBell, PoseTransform2D.Identity, Delayed(progress));
                break;
        }
        ApplySlots();
    }

    private void EnsureParts()
    {
        if (_body != null) return;
        _feet = GetNode<EnemyFormalVisualPart>("Feet");
        _body = GetNode<EnemyFormalVisualPart>("Body");
        _bell = GetNode<EnemyFormalVisualPart>("Bell");
        _weapon = GetNode<EnemyFormalVisualPart>("Weapon");
        _corruption = GetNode<EnemyFormalVisualPart>("Corruption");
        _feetRest = _feet.Position;
        _bodyRest = _body.Position;
        _bellRest = _bell.Position;
        _weaponRest = _weapon.Position;
        _corruptionRest = _corruption.Position;
    }

    private void ApplySlots()
    {
        EnsureParts();
        PoseTransform2D feet = _feetPose;
        if (_state == BellBackLaborerState.Approach)
            feet = feet with { RotationRadians = feet.RotationRadians + Mathf.Sin(_phase * 8f) * .08f };
        ApplyIndependent(_body, _bodyRest, _bodyPose);
        ApplyChild(_feet, _feetRest, _bodyPose, feet);
        ApplyChild(_bell, _bellRest, _bodyPose, _bellPose);
        ApplyChild(_weapon, _weaponRest, _bodyPose, _weaponPose);
        ApplyIndependent(_corruption, _corruptionRest, _corruptionPose);
        _feet.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _body.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _bell.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _weapon.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
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
        BellBackLaborerState.Exposed => Pose(Vector2.Zero, -.34f),
        BellBackLaborerState.Recover => Pose(Vector2.Zero, .16f),
        BellBackLaborerState.Dead => Pose(new Vector2(0f, 24f), 1.22f),
        _ => PoseTransform2D.Identity
    };
    private float BaseWeaponRotation() => _state == BellBackLaborerState.Exposed ? .9f : -.18f;
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
