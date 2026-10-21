using Godot;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Presentation;

namespace Knight.Prologue.Sample;

// Sample-only skin. The actor, attack collider and published world-space threat remain authoritative.
public partial class BasinLaborerVisual : Node2D, IBellBackLaborerVisual
{
    private static readonly Color Ink = new("101e22");
    private static readonly Color Recess = new("213537");
    private static readonly Color Copper = new("805d42");
    private static readonly Color Brass = new("b59661");
    private static readonly Color Edge = new("d0b77d");
    private static readonly Color Ivory = new("ddd6ba");
    private static readonly Color CoreLight = new("73c9bd");
    private Node2D _backBell, _torso, _head, _core, _chestLeft, _chestRight;
    private Node2D _upperArm, _forearm, _hammer, _feet;
    private Node2D[] _parts;
    private BellBackLaborer _actor;
    private AttackTimelineFrame _timeline;
    private int _facing = 1;
    private float _clock;
    private int _hurtFrames;
    private Vector2 _bodyOffset;
    private float _bodyLean;
    private float _upperLength, _foreLength;

    public BellBackLaborerState CurrentState { get; private set; } = BellBackLaborerState.Idle;
    public AttackPresentationFrame CurrentAttackFrame { get; private set; }
    public string CurrentAnimation { get; private set; } = "idle";

    public override void _Ready()
    {
        EnsureParts();
        BindActor();
        if (IsInstanceValid(_actor)) ApplyLaborerFrame(_actor.CurrentFrame, _actor.Facing);
        else ApplyPose();
    }

    public override void _Process(double delta)
    {
        BindActor();
        _clock += Mathf.Max(0f, (float)delta);
        ApplyPose();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_hurtFrames > 0) _hurtFrames--;
    }

    public void ApplyLaborerFrame(BellBackLaborerFrame frame, int facing)
    {
        CurrentState = frame.State;
        _facing = facing < 0 ? -1 : 1;
        CurrentAnimation = frame.State switch
        {
            BellBackLaborerState.Approach => "approach",
            BellBackLaborerState.Guard => "guard",
            BellBackLaborerState.Telegraph => "telegraph",
            BellBackLaborerState.Attack => "attack",
            BellBackLaborerState.Recover => "recover",
            BellBackLaborerState.Exposed => "exposed",
            BellBackLaborerState.Dead => "dead",
            _ => "idle"
        };
        _timeline = frame.AttackTimeline;
        if (_timeline.Phase == AttackPhase.None) CurrentAttackFrame = default;
        ApplyPose();
    }

    public void PresentAttack(AttackPresentationFrame frame)
    {
        // Do not mirror or rebuild Shape: its polygon points are already in world space.
        CurrentAttackFrame = frame.Timeline.Phase == AttackPhase.None ? default : frame.Validate();
        _timeline = frame.Timeline;
        ApplyPose();
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        // The normal coordinator supplies the typed frame. Generic fallback is for an unbound preview.
        if (IsInstanceValid(_actor)) return;
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
        if (cue == PresentationCue.Hurt) _hurtFrames = 3;
        ApplyPose();
    }

    public void ClearCueOverride() => _hurtFrames = 0;

    private void BindActor()
    {
        BellBackLaborer next = GetParent()?.GetParent() as BellBackLaborer;
        if (ReferenceEquals(next, _actor) && IsInstanceValid(_actor)) return;
        if (IsInstanceValid(_actor)) _actor.AttackFrameChanged -= PresentAttack;
        _actor = next;
        if (IsInstanceValid(_actor)) _actor.AttackFrameChanged += PresentAttack;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_actor)) _actor.AttackFrameChanged -= PresentAttack;
        _actor = null;
        _hurtFrames = 0;
        _timeline = default;
        CurrentAttackFrame = default;
    }

    private void EnsureParts()
    {
        if (IsInstanceValid(_torso)) return;
        _backBell = Part("BackBell", -2);
        _feet = Part("Feet", -1);
        _torso = Part("Torso", 0);
        _core = Part("Core", 1);
        _chestLeft = Part("ChestLeft", 2);
        _chestRight = Part("ChestRight", 2);
        _head = Part("Head", 3);
        _upperArm = Part("UpperArm", 4);
        _forearm = Part("Forearm", 4);
        _hammer = Part("Hammer", 5);
        _parts = [_backBell, _feet, _torso, _core, _chestLeft, _chestRight,
            _head, _upperArm, _forearm, _hammer];
    }

    private Node2D Part(string name, int depth)
    {
        var part = new Node2D { Name = name, ZIndex = depth };
        part.Draw += () => DrawPart(part, name);
        AddChild(part);
        return part;
    }

    private readonly record struct Stance(Vector2 Body, float Lean, Vector2 Elbow,
        Vector2 Hand, float HammerAngle);

    private static Stance Blend(Stance a, Stance b, float amount) => new(
        a.Body.Lerp(b.Body, amount), Mathf.Lerp(a.Lean, b.Lean, amount),
        a.Elbow.Lerp(b.Elbow, amount), a.Hand.Lerp(b.Hand, amount),
        Mathf.Lerp(a.HammerAngle, b.HammerAngle, amount));

    private void ApplyPose()
    {
        EnsureParts();
        Scale = new Vector2(_facing, 1f);
        Stance idle = new(Vector2.Zero, 0f, new(20, -5), new(28, 6), .55f);
        Stance guard = new(new(0, 1), -.06f, new(24, -7), new(7, -16), -1.40f);
        Stance drawnBack = new(new(-5, 4), -.23f, new(-2, -17), new(-7, -30), -2.00f);
        Stance sweep = new(new(5, 2), .20f, new(24, -8), new(33, -3), -.10f);
        Stance recovered = new(new(1, 3), .10f, new(24, 2), new(26, 12), .50f);
        Stance pose = CurrentState switch
        {
            BellBackLaborerState.Guard => guard,
            BellBackLaborerState.Telegraph => drawnBack,
            BellBackLaborerState.Attack => sweep,
            BellBackLaborerState.Recover => recovered,
            BellBackLaborerState.Exposed => new(new(-3, 4), -.20f, new(27, -5), new(34, 8), .45f),
            BellBackLaborerState.Dead => new(new(1, 24), 1.12f, new(24, 29), new(37, 31), .05f),
            _ => idle
        };
        if (_timeline.Kind == EnemyAttackKind.LaborerSweep && _timeline.Phase != AttackPhase.None)
        {
            float progress = _timeline.PhaseDurationSeconds <= 0f ? 1f :
                Mathf.Clamp(1f - _timeline.PhaseRemainingSeconds / _timeline.PhaseDurationSeconds, 0f, 1f);
            float eased = progress * progress * (3f - 2f * progress);
            pose = _timeline.Phase switch
            {
                AttackPhase.Telegraph => Blend(guard, drawnBack, eased),
                AttackPhase.Active => Blend(drawnBack, sweep, eased),
                AttackPhase.Recovery => Blend(sweep, recovered, eased),
                _ => pose
            };
        }
        _bodyOffset = pose.Body;
        _bodyLean = pose.Lean;
        Place(_torso, pose.Body, pose.Lean);
        Place(_backBell, BodyPoint(new(-13, -15)), pose.Lean * .65f);
        Place(_head, BodyPoint(new(9, -39)), pose.Lean * .65f);
        Place(_core, BodyPoint(new(1, -12)), pose.Lean);
        bool exposed = CurrentState == BellBackLaborerState.Exposed;
        float opening = exposed ? 17f : 0f;
        Place(_chestLeft, BodyPoint(new(-7 - opening, -14)), pose.Lean - (exposed ? .48f : 0f));
        Place(_chestRight, BodyPoint(new(8 + opening, -14)), pose.Lean + (exposed ? .48f : 0f));
        _core.Visible = exposed;
        Vector2 shoulder = BodyPoint(new(12, -24));
        _upperLength = shoulder.DistanceTo(pose.Elbow);
        _foreLength = pose.Elbow.DistanceTo(pose.Hand);
        Place(_upperArm, shoulder, (pose.Elbow - shoulder).Angle());
        Place(_forearm, pose.Elbow, (pose.Hand - pose.Elbow).Angle());
        Place(_hammer, pose.Hand, pose.HammerAngle);
        foreach (Node2D part in _parts) part.QueueRedraw();
    }

    private Vector2 BodyPoint(Vector2 point) => _bodyOffset + point.Rotated(_bodyLean);
    private static void Place(Node2D part, Vector2 position, float rotation)
    { part.Position = position; part.Rotation = rotation; }

    private void DrawPart(Node2D part, string kind)
    {
        Color rim = _hurtFrames > 0 ? Ivory : Edge;
        switch (kind)
        {
            case "BackBell":
                Plate(part, [new(-30, 15), new(-28, -16), new(-23, -33), new(-13, -43),
                    new(-1, -45), new(11, -37), new(20, -20), new(23, 15)], Copper, Ink, 4f);
                part.DrawArc(new(-2, -17), 25f, Mathf.Pi, Mathf.Tau - .18f, 22, Brass, 3f, true);
                part.DrawLine(new(-29, 15), new(22, 15), Brass, 5f, true);
                part.DrawLine(new(-27, 19), new(22, 19), Ink, 3f, true);
                break;
            case "Torso":
                Plate(part, [new(-18, -31), new(-7, -37), new(12, -34), new(23, -18),
                    new(20, 13), new(10, 23), new(-15, 20), new(-22, 6)], Recess, Ink, 4f);
                part.DrawLine(new(-16, 13), new(17, 13), Copper, 6f, true);
                break;
            case "Head":
                Plate(part, [new(-9, -12), new(5, -15), new(13, -7), new(11, 12),
                    new(1, 17), new(-8, 8)], Ivory, Ink, 3f);
                part.DrawLine(new(-3, -3), new(11, -5), Ink, 5f, true);
                part.DrawLine(new(4, -4), new(10, -5), CoreLight, 2f, true);
                part.DrawLine(new(3, 4), new(2, 12), Copper, 2f, true);
                break;
            case "ChestLeft":
                Plate(part, [new(-11, -14), new(5, -17), new(8, -8), new(7, 19),
                    new(-6, 22), new(-12, 10)], Copper, Ink, 3f);
                part.DrawLine(new(5, -10), new(5, 16), rim, 2f, true);
                part.DrawArc(new(-2, 1), 9f, 1.8f, 4.1f, 12, Brass, 2f, true);
                break;
            case "ChestRight":
                Plate(part, [new(-7, -17), new(7, -13), new(13, -2), new(10, 15),
                    new(0, 21), new(-7, 18)], Brass, Ink, 3f);
                part.DrawLine(new(-5, -10), new(-5, 16), rim, 2f, true);
                part.DrawLine(new(7, -4), new(5, 11), Copper, 3f, true);
                break;
            case "Core":
                part.DrawCircle(Vector2.Zero, 12f, Ink);
                part.DrawCircle(Vector2.Zero, 9f, CoreLight);
                part.DrawCircle(new(-2, -2), 4f, Ivory);
                part.DrawArc(Vector2.Zero, 13f, -.8f, 2.2f, 16, Brass, 2f, true);
                break;
            case "UpperArm":
                Limb(part, _upperLength, Copper, 10f);
                break;
            case "Forearm":
                Limb(part, _foreLength, Brass, 13f);
                break;
            case "Hammer":
                part.DrawLine(new(-8, 0), new(35, 0), Ink, 8f, true);
                part.DrawLine(new(-7, 0), new(35, 0), Ivory, 3f, true);
                part.DrawCircle(Vector2.Zero, 5f, Copper);
                Plate(part, [new(29, -9), new(38, -13), new(44, -8), new(44, 8),
                    new(38, 13), new(29, 9)], Copper, Ink, 3f);
                part.DrawLine(new(41, -7), new(41, 7), rim, 4f, true);
                part.DrawLine(new(32, -7), new(32, 7), Brass, 3f, true);
                break;
            case "Feet":
                DrawLegs(part);
                break;
        }
    }

    private void DrawLegs(Node2D part)
    {
        float stride = CurrentState == BellBackLaborerState.Approach ? Mathf.Sin(_clock * 8f) * 5f : 0f;
        Vector2 leftHip = BodyPoint(new(-10, 17));
        Vector2 rightHip = BodyPoint(new(10, 17));
        Vector2 leftKnee = new(-16 - stride, 29);
        Vector2 rightKnee = new(17 + stride, 27);
        if (CurrentState == BellBackLaborerState.Dead)
        { leftKnee = new(-19, 35); rightKnee = new(12, 35); }
        Leg(part, leftHip, leftKnee, new(-18, 36));
        Leg(part, rightHip, rightKnee, new(19, 36));
        // Broad soles end at local y=42, matching the authored standing support plane.
        Plate(part, [new(-27, 36), new(-12, 36), new(-9, 41), new(-29, 41)], Copper, Ink, 2f);
        Plate(part, [new(12, 36), new(25, 36), new(30, 41), new(10, 41)], Brass, Ink, 2f);
    }

    private static void Leg(Node2D part, Vector2 hip, Vector2 knee, Vector2 ankle)
    {
        part.DrawLine(hip, knee, Ink, 11f, true);
        part.DrawLine(hip, knee, Copper, 6f, true);
        part.DrawLine(knee, ankle, Ink, 10f, true);
        part.DrawLine(knee, ankle, Brass, 5f, true);
        part.DrawCircle(knee, 5f, Ink);
        part.DrawCircle(knee, 2f, Edge);
    }

    private static void Limb(Node2D part, float length, Color color, float width)
    {
        part.DrawLine(Vector2.Zero, new(length, 0), Ink, width + 4f, true);
        part.DrawLine(Vector2.Zero, new(length, 0), color, width, true);
        part.DrawCircle(Vector2.Zero, width * .55f, Ink);
        part.DrawCircle(Vector2.Zero, 2.5f, Edge);
        part.DrawCircle(new(length, 0), width * .45f, Ink);
    }

    private static void Plate(Node2D part, Vector2[] polygon, Color fill, Color outline, float width)
    {
        part.DrawColoredPolygon(polygon, fill);
        for (int index = 0; index < polygon.Length; index++)
            part.DrawLine(polygon[index], polygon[(index + 1) % polygon.Length], outline, width, true);
    }
}
