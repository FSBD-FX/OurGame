using Godot;
using Knight.Abilities;

namespace Knight.Vfx;

public partial class ThreadShuttleVisual : Node2D
{
    private static readonly Color Cyan = new("8de4d8");
    private static readonly Color BrightCyan = new("c5fff6");
    private static readonly Color Gold = new("d7b85b");
    private static readonly Color FailedRed = new("b75a4b");

    private MechanicalClawController _claw;

    [Export] public NodePath MechanicalClawPath { get; set; } = new("../../MechanicalClaw");

    public ThreadCastFrame CurrentFrame { get; private set; }
    public MechanicalClawFrame CurrentClawFrame { get; private set; } = MechanicalClawFrame.Idle();
    public Vector2 LastWorldOrigin { get; private set; }
    public Vector2 LastWorldTarget { get; private set; }
    public Vector2 ShuttleWorldPosition { get; private set; }
    public float TravelProgress { get; private set; }
    public float RecoveryProgress { get; private set; }
    public float LatchScale { get; private set; }
    public float SpoolRotation { get; private set; }
    public bool CandidateDiamondVisible { get; private set; }
    public bool LatchVisible { get; private set; }
    public bool NeedleVisible { get; private set; }
    public bool FailureAtCasterVisible { get; private set; }
    public bool IsClear => CurrentFrame.State == ThreadCastState.Idle && !Visible;
    public bool IsClawClear => CurrentClawFrame.State == MechanicalClawState.Idle && !Visible;

    public override void _Ready()
    {
        Clear();
        _claw = GetNodeOrNull<MechanicalClawController>(MechanicalClawPath);
        if (_claw == null) return;
        _claw.ClawFrameChanged += OnClawFrameChanged;
        OnClawFrameChanged(_claw.CurrentFrame, _claw.CurrentTarget);
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_claw))
            _claw.ClawFrameChanged -= OnClawFrameChanged;
        Clear();
    }

    public void Present(ThreadCastFrame frame, Vector2 origin, Vector2 targetOrFailurePoint)
    {
        CurrentClawFrame = MechanicalClawFrame.Idle(origin);
        CurrentFrame = frame;
        LastWorldOrigin = origin;
        LastWorldTarget = targetOrFailurePoint;
        TravelProgress = 0f;
        RecoveryProgress = 0f;
        LatchScale = 0f;
        CandidateDiamondVisible = frame.State == ThreadCastState.Preview && frame.HasTarget;
        LatchVisible = frame.State is ThreadCastState.Latched or ThreadCastState.Apply;
        FailureAtCasterVisible = frame.State == ThreadCastState.Failed && frame.Failure is
            ThreadFailureReason.NoTarget or ThreadFailureReason.Cooldown;
        NeedleVisible = frame.State is ThreadCastState.Travel or ThreadCastState.Recovery ||
            FailureAtCasterVisible;
        Visible = frame.State != ThreadCastState.Idle;

        switch (frame.State)
        {
            case ThreadCastState.Preview:
                ShuttleWorldPosition = targetOrFailurePoint;
                SpoolRotation += .12f;
                break;
            case ThreadCastState.Windup:
                ShuttleWorldPosition = origin;
                SpoolRotation += .42f + .58f * PhaseProgress(frame);
                break;
            case ThreadCastState.Travel:
                TravelProgress = PhaseProgress(frame);
                ShuttleWorldPosition = origin.Lerp(targetOrFailurePoint, TravelProgress);
                break;
            case ThreadCastState.Latched:
                ShuttleWorldPosition = targetOrFailurePoint;
                LatchScale = Mathf.Lerp(.25f, 1f, PhaseProgress(frame));
                break;
            case ThreadCastState.Apply:
                ShuttleWorldPosition = targetOrFailurePoint;
                LatchScale = 1f;
                break;
            case ThreadCastState.Recovery:
                RecoveryProgress = PhaseProgress(frame);
                ShuttleWorldPosition = targetOrFailurePoint.Lerp(origin, RecoveryProgress);
                break;
            case ThreadCastState.Failed:
                ShuttleWorldPosition = FailureAtCasterVisible
                    ? origin + new Vector2(14f, -16f)
                    : targetOrFailurePoint;
                break;
            default:
                ShuttleWorldPosition = origin;
                break;
        }

        QueueRedraw();
    }

    public void Present(MechanicalClawFrame frame)
    {
        CurrentClawFrame = frame;
        Vector2 origin = frame.CasterWorldPosition;
        Vector2 target = frame.AnchorPoint;
        float distance = origin.DistanceTo(target);
        float progress = frame.State switch
        {
            MechanicalClawState.Travel => distance <= .001f
                ? 1f
                : Mathf.Clamp(frame.ShuttleDistance / distance, 0f, 1f),
            MechanicalClawState.Released or MechanicalClawState.Retract => distance <= .001f
                ? 1f
                : Mathf.Clamp(1f - frame.RopeLength / distance, 0f, 1f),
            _ => frame.State == MechanicalClawState.Idle ? 0f : 1f
        };
        ThreadCastState state = frame.State switch
        {
            MechanicalClawState.Aim => ThreadCastState.Preview,
            MechanicalClawState.Windup => ThreadCastState.Windup,
            MechanicalClawState.Travel => ThreadCastState.Travel,
            MechanicalClawState.Latched => ThreadCastState.Latched,
            MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost =>
                ThreadCastState.Apply,
            MechanicalClawState.Released or MechanicalClawState.Retract => ThreadCastState.Recovery,
            MechanicalClawState.Failed => ThreadCastState.Failed,
            _ => ThreadCastState.Idle
        };
        Vector2 aim = origin.DirectionTo(target);
        ThreadFailureReason failure = frame.Failure switch
        {
            MechanicalClawFailure.NoTarget => ThreadFailureReason.NoTarget,
            MechanicalClawFailure.Obstructed => ThreadFailureReason.Obstructed,
            MechanicalClawFailure.OutOfRange => ThreadFailureReason.OutOfRange,
            _ => frame.Failure == MechanicalClawFailure.None
                ? ThreadFailureReason.None
                : ThreadFailureReason.Cancelled
        };
        CurrentFrame = new ThreadCastFrame(frame.CastId, state, ThreadAimMode.Tap, aim,
            frame.LockedTargetRuntimeId, frame.EffectiveVisibility, failure,
            1f - progress, 1f);
        LastWorldOrigin = origin;
        LastWorldTarget = target;
        TravelProgress = state == ThreadCastState.Travel ? progress : 0f;
        RecoveryProgress = state == ThreadCastState.Recovery ? progress : 0f;
        LatchScale = state is ThreadCastState.Latched or ThreadCastState.Apply ? 1f : 0f;
        CandidateDiamondVisible = state == ThreadCastState.Preview && frame.EffectiveVisibility;
        LatchVisible = state is ThreadCastState.Latched or ThreadCastState.Apply;
        FailureAtCasterVisible = state == ThreadCastState.Failed && frame.Failure is
            MechanicalClawFailure.NoTarget or MechanicalClawFailure.AlreadyDeployed;
        NeedleVisible = state is ThreadCastState.Travel or ThreadCastState.Recovery ||
            FailureAtCasterVisible;
        Visible = state != ThreadCastState.Idle;
        ShuttleWorldPosition = state switch
        {
            ThreadCastState.Preview => target,
            ThreadCastState.Windup => origin,
            ThreadCastState.Travel => origin.Lerp(target, progress),
            ThreadCastState.Latched or ThreadCastState.Apply => target,
            ThreadCastState.Recovery => target.Lerp(origin, progress),
            ThreadCastState.Failed => FailureAtCasterVisible
                ? origin + new Vector2(14f, -16f)
                : target,
            _ => origin
        };
        QueueRedraw();
    }

    public void Clear()
    {
        CurrentClawFrame = MechanicalClawFrame.Idle();
        CurrentFrame = new ThreadCastFrame(0, ThreadCastState.Idle, ThreadAimMode.Tap, Vector2.Right,
            0, false, ThreadFailureReason.None, 0f, 0f);
        TravelProgress = 0f;
        RecoveryProgress = 0f;
        LatchScale = 0f;
        CandidateDiamondVisible = false;
        LatchVisible = false;
        NeedleVisible = false;
        FailureAtCasterVisible = false;
        Visible = false;
        QueueRedraw();
    }

    private void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target) => Present(frame);

    public override void _Draw()
    {
        if (!Visible) return;
        Vector2 origin = ToLocal(LastWorldOrigin);
        Vector2 target = ToLocal(LastWorldTarget);
        Vector2 shuttle = ToLocal(ShuttleWorldPosition);
        Vector2 direction = LastWorldOrigin.DirectionTo(LastWorldTarget);
        if (direction.IsZeroApprox()) direction = Vector2.Right;
        Vector2 normal = new(-direction.Y, direction.X);

        if (CurrentFrame.State is ThreadCastState.Preview or ThreadCastState.Windup)
            DrawSpool(origin, CurrentFrame.State == ThreadCastState.Windup ? 1f : .55f);
        if (FailureAtCasterVisible) DrawSpool(origin, .7f);
        if (CandidateDiamondVisible) DrawDiamond(target, 7f, Gold, 2f);
        if (NeedleVisible) DrawNeedle(shuttle, direction, normal);
        if (LatchVisible) DrawDiamond(target, Mathf.Max(3f, 10f * LatchScale), BrightCyan, 2.4f);
        if (CurrentFrame.State == ThreadCastState.Failed) DrawFailure(shuttle);
    }

    private void DrawSpool(Vector2 origin, float brightness)
    {
        Color outer = Gold;
        outer.A = brightness;
        DrawArc(origin + new Vector2(0f, -16f), 8f, SpoolRotation, SpoolRotation + Mathf.Tau * .82f,
            18, outer, 2.4f, true);
        Color inner = Cyan;
        inner.A = brightness;
        DrawCircle(origin + new Vector2(0f, -16f), 3f, inner);
    }

    private void DrawNeedle(Vector2 position, Vector2 direction, Vector2 normal)
    {
        Color color = CurrentFrame.State == ThreadCastState.Recovery ? Cyan.Darkened(.22f) : BrightCyan;
        Vector2 tail = position - direction * 9f;
        Vector2 tip = position + direction * 12f;
        DrawLine(tail, tip, color, 3f, true);
        DrawLine(tip, position + direction * 5f + normal * 4f, color, 2f, true);
        DrawLine(tip, position + direction * 5f - normal * 4f, color, 2f, true);
        DrawCircle(tail, 2.4f, Gold);
    }

    private void DrawFailure(Vector2 point)
    {
        DrawLine(point + new Vector2(-5f, -5f), point + new Vector2(5f, 5f), FailedRed, 2f, true);
        DrawLine(point + new Vector2(-5f, 5f), point + new Vector2(5f, -5f), FailedRed, 2f, true);
        DrawCircle(point, 2f, Gold);
    }

    private void DrawDiamond(Vector2 center, float radius, Color color, float width)
    {
        Vector2 top = center + Vector2.Up * radius;
        Vector2 right = center + Vector2.Right * radius;
        Vector2 bottom = center + Vector2.Down * radius;
        Vector2 left = center + Vector2.Left * radius;
        DrawLine(top, right, color, width, true);
        DrawLine(right, bottom, color, width, true);
        DrawLine(bottom, left, color, width, true);
        DrawLine(left, top, color, width, true);
    }

    private static float PhaseProgress(ThreadCastFrame frame)
    {
        if (frame.StateDurationSeconds <= .000001f) return 1f;
        return Mathf.Clamp(1f - frame.StateRemainingSeconds / frame.StateDurationSeconds, 0f, 1f);
    }
}
