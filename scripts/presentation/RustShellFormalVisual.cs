using Godot;
using Knight.Combat;
using Knight.Enemies;

namespace Knight.Presentation;

public partial class RustShellFormalVisual : Node2D, IEntityVisual2D
{
    private const float CorruptionCircleRadius = 10f;
    private const int HurtFlashPhysicsFrames = 3;
    private RustShellState _state;
    private RustShellSquire _attackSource;
    private float _phase;
    private int _hurtFlashFrames;
    private EnemyFormalVisualPart _body;
    private EnemyFormalVisualPart _shell;
    private EnemyFormalVisualPart _weapon;
    private EnemyFormalVisualPart _corruption;
    private Vector2 _bodyRest;
    private Vector2 _shellRest;
    private Vector2 _weaponRest;
    private Vector2 _corruptionRest;
    private PoseTransform2D _bodyPose = PoseTransform2D.Identity;
    private PoseTransform2D _weaponPose = PoseTransform2D.Identity;
    private PoseTransform2D _shellPose = PoseTransform2D.Identity;
    private PoseTransform2D _corruptionPose = PoseTransform2D.Identity;

    public float CurrentContactIntensity { get; private set; }
    public float CurrentHurtFlashIntensity => _hurtFlashFrames > 0 ? 1f : 0f;
    public AttackPresentationFrame CurrentAttackFrame { get; private set; }
    public PoseTransform2D CurrentBodyTransform => BodyComponent();
    public PoseTransform2D CurrentWeaponTransform => ChildComponent(_weapon, _weaponRest);
    public PoseTransform2D CurrentShellTransform => ChildComponent(_shell, _shellRest);
    public PoseTransform2D CurrentCorruptionTransform => IndependentComponent(_corruption, _corruptionRest);
    public Rect2 CurrentCorruptionBounds => CorruptionBounds();
    public string CurrentAnimation { get; private set; } = "idle";
    public string CurrentPoseMarker { get; private set; } = "rust_idle";

    public override void _Ready()
    {
        EnsureParts();
        _attackSource = GetParent()?.GetParent() as RustShellSquire;
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

    public void ApplyRustShellFrame(RustShellFrame frame, int facing)
    {
        _state = frame.State;
        CurrentAnimation = frame.State switch
        {
            RustShellState.Idle => "idle",
            RustShellState.Chase => "chase",
            RustShellState.Telegraph => "telegraph",
            RustShellState.Attack => "attack",
            RustShellState.Recover => "recover",
            RustShellState.Stunned => "stunned",
            RustShellState.Dead => "dead",
            _ => "idle"
        };
        CurrentPoseMarker = frame.State switch
        {
            RustShellState.Telegraph => "rust_crouch",
            RustShellState.Attack => "rust_thrust",
            RustShellState.Recover => "rust_weapon_drop",
            _ => "rust_idle"
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
        RustShellState state = frame.State switch
        {
            EntityVisualState.Move => RustShellState.Chase,
            EntityVisualState.Attack => RustShellState.Attack,
            EntityVisualState.Hurt => RustShellState.Stunned,
            EntityVisualState.Dead => RustShellState.Dead,
            _ => RustShellState.Idle
        };
        ApplyRustShellFrame(new RustShellFrame(state, 0f, false, false), frame.Facing);
    }

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _hurtFlashFrames = HurtFlashPhysicsFrames;
        ApplySlots();
    }

    private void ApplyAttackTimeline(AttackTimelineFrame timeline)
    {
        if (timeline.Phase == AttackPhase.None || timeline.Kind != EnemyAttackKind.RustShellThrust)
        {
            CurrentAttackFrame = default;
            _bodyPose = BaseBodyTransform();
            _weaponPose = PoseTransform2D.Identity;
            _shellPose = PoseTransform2D.Identity;
            _corruptionPose = PoseTransform2D.Identity;
            CurrentContactIntensity = 0f;
            ApplySlots();
            return;
        }

        float progress = Progress(timeline);
        CurrentContactIntensity = Contact(timeline, progress);
        _corruptionPose = new PoseTransform2D(new Vector2(-16f, -3f), 0f,
            Vector2.One, Colors.White);
        PoseTransform2D telegraphBody = Pose(new Vector2(0f, 7f), -.16f);
        PoseTransform2D telegraphWeapon = Pose(new Vector2(-18f, -6f), -.18f);
        PoseTransform2D telegraphShell = Pose(new Vector2(-2f, 1f), .07f);
        PoseTransform2D activeBody = Pose(new Vector2(0f, 3f), .16f);
        PoseTransform2D activeWeapon = Pose(new Vector2(42f, -1f), .04f);
        PoseTransform2D activeShell = Pose(new Vector2(-2f, 1f), -.06f);
        PoseTransform2D recoveryBody = PoseTransform2D.Identity;
        PoseTransform2D recoveryWeapon = Pose(new Vector2(0f, 5f), .22f);
        switch (timeline.Phase)
        {
            case AttackPhase.Telegraph:
                CurrentPoseMarker = "rust_crouch";
                _bodyPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphBody, Ease(progress));
                _weaponPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphWeapon, Ease(progress));
                _shellPose = PoseTransform2D.Interpolate(PoseTransform2D.Identity, telegraphShell, Delayed(progress));
                break;
            case AttackPhase.Active:
                CurrentPoseMarker = "rust_thrust";
                _bodyPose = PoseTransform2D.Interpolate(telegraphBody, activeBody, Ease(progress));
                _weaponPose = PoseTransform2D.Interpolate(telegraphWeapon, activeWeapon, Ease(progress));
                _shellPose = PoseTransform2D.Interpolate(telegraphShell, activeShell, Delayed(progress));
                break;
            case AttackPhase.Recovery:
                CurrentPoseMarker = "rust_weapon_drop";
                _bodyPose = PoseTransform2D.Interpolate(activeBody, recoveryBody, Ease(progress));
                _weaponPose = PoseTransform2D.Interpolate(activeWeapon, recoveryWeapon, Ease(progress));
                _shellPose = PoseTransform2D.Interpolate(activeShell, PoseTransform2D.Identity, Delayed(progress));
                break;
        }
        ApplySlots();
    }

    private void EnsureParts()
    {
        if (_body != null) return;
        _body = GetNode<EnemyFormalVisualPart>("Body");
        _shell = GetNode<EnemyFormalVisualPart>("Shell");
        _weapon = GetNode<EnemyFormalVisualPart>("Weapon");
        _corruption = GetNode<EnemyFormalVisualPart>("Corruption");
        _bodyRest = _body.Position;
        _shellRest = _shell.Position;
        _weaponRest = _weapon.Position;
        _corruptionRest = _corruption.Position;
    }

    private void ApplySlots()
    {
        EnsureParts();
        float step = _state == RustShellState.Chase ? Mathf.Sin(_phase * 12f) * 2f : 0f;
        PoseTransform2D body = _bodyPose with { Position = _bodyPose.Position + Vector2.Down * step };
        ApplyIndependent(_body, _bodyRest, body);
        ApplyChild(_shell, _shellRest, body, _shellPose);
        ApplyChild(_weapon, _weaponRest, body, _weaponPose);
        ApplyIndependent(_corruption, _corruptionRest, _corruptionPose);
        _body.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _shell.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _weapon.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
        _corruption.SetFeedback(CurrentContactIntensity, CurrentHurtFlashIntensity);
    }

    private PoseTransform2D BodyComponent() => IndependentComponent(_body, _bodyRest);

    private PoseTransform2D ChildComponent(Node2D node, Vector2 rest)
    {
        PoseTransform2D body = BodyComponent();
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
        RustShellState.Stunned => Pose(Vector2.Zero, -.42f),
        RustShellState.Dead => Pose(new Vector2(0f, 20f), 1.28f),
        _ => PoseTransform2D.Identity
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
