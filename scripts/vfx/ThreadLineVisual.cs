using System.Collections.Generic;
using Godot;
using Knight.Abilities;

namespace Knight.Vfx;

public enum ThreadLineState
{
    Idle,
    Fired,
    Connected,
    Failed,
    Preview,
    Travel,
    Latched,
    Recovery,
    Occluded
}

public partial class ThreadLineVisual : Line2D
{
    private const int StrandTotal = 3;

    private readonly List<Line2D> _strands = new(StrandTotal);
    private MechanicalClawController _claw;
    private Gradient _occlusionFade;

    [Export] public NodePath MechanicalClawPath { get; set; } = new("../../../MechanicalClaw");

    public Vector2 LastWorldStart { get; private set; }
    public Vector2 LastWorldEnd { get; private set; }
    public float LastTension { get; private set; }
    public ThreadLineState CurrentState { get; private set; } = ThreadLineState.Idle;
    public bool IsConnectionObscured { get; private set; }
    public int StrandCount => StrandTotal;
    public int ActiveStrandCount
    {
        get
        {
            EnsureStrands();
            int active = 0;
            foreach (Line2D strand in _strands)
                if (strand.Points.Length > 0) active++;
            return active;
        }
    }

    public override void _Ready()
    {
        EnsureStrands();
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

    public void Present(Vector2 start, Vector2 end, float tension, ThreadLineState state)
    {
        EnsureStrands();
        LastWorldStart = start;
        LastWorldEnd = end;
        LastTension = Mathf.Clamp(tension, 0f, 1f);
        CurrentState = state;
        IsConnectionObscured = state == ThreadLineState.Occluded;

        Vector2 localStart = ToLocal(start);
        Vector2 localEnd = ToLocal(end);
        Vector2 delta = localEnd - localStart;
        Vector2 normal = delta.LengthSquared() < .001f
            ? Vector2.Up
            : new Vector2(-delta.Y, delta.X).Normalized();
        float sag = (1f - LastTension) * Mathf.Min(24f, delta.Length() * .08f);
        int visibleStrands = state is ThreadLineState.Failed or ThreadLineState.Occluded ? 1 : StrandTotal;
        Color color = ColorFor(state);
        float alphaStep = state == ThreadLineState.Preview ? .09f : .14f;

        for (int index = 0; index < StrandTotal; index++)
        {
            Line2D strand = _strands[index];
            strand.Gradient = null;
            if (index >= visibleStrands)
            {
                strand.Points = [];
                continue;
            }

            float lane = index - 1f;
            float laneOffset = lane * (state == ThreadLineState.Latched ? 4.2f : 3f);
            Vector2 firstBend = localStart.Lerp(localEnd, .34f) + normal * (sag * .72f + laneOffset);
            Vector2 secondBend = localStart.Lerp(localEnd, .68f) +
                normal * (sag * .92f - laneOffset * .55f);
            // No sag or lane offsets near a blocking plane: those could enter the wall
            // even though the center endpoint was clipped correctly.
            strand.Points = state == ThreadLineState.Occluded
                ? [localStart, localEnd]
                : [localStart, firstBend, secondBend, localEnd];
            Color strandColor = color;
            strandColor.A = Mathf.Clamp(color.A - Mathf.Abs(lane) * alphaStep, 0f, 1f);
            strand.DefaultColor = strandColor;
            strand.Width = WidthFor(state, index);
            if (state == ThreadLineState.Occluded)
            {
                _occlusionFade ??= new Gradient();
                _occlusionFade.Offsets = [0f, Mathf.Max(.01f, 1f - 24f / Mathf.Max(1f, delta.Length())), 1f];
                _occlusionFade.Colors = [color, color, new Color(color.R, color.G, color.B, 0f)];
                strand.Gradient = _occlusionFade;
            }
        }
    }

    public Vector2[] GetStrandPoints(int index)
    {
        EnsureStrands();
        if (index < 0 || index >= _strands.Count) return [];
        Vector2[] points = _strands[index].Points;
        return (Vector2[])points.Clone();
    }

    public void Clear()
    {
        EnsureStrands();
        foreach (Line2D strand in _strands)
        {
            strand.Points = [];
            strand.Gradient = null;
        }
        CurrentState = ThreadLineState.Idle;
        IsConnectionObscured = false;
        LastTension = 0f;
        LastWorldStart = Vector2.Zero;
        LastWorldEnd = Vector2.Zero;
    }

    private void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (frame.State == MechanicalClawState.Failed &&
            frame.Failure == MechanicalClawFailure.NoTarget)
        {
            // NoTarget has no world endpoint. The shuttle visual owns the small local failure
            // marker beside the caster; drawing a strand here would connect to Vector2.Zero.
            Clear();
            return;
        }
        ThreadLineState state = frame.State switch
        {
            MechanicalClawState.Aim => ThreadLineState.Preview,
            MechanicalClawState.Windup => ThreadLineState.Fired,
            MechanicalClawState.Travel => ThreadLineState.Travel,
            MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or
                MechanicalClawState.Boost =>
                ThreadLineState.Latched,
            MechanicalClawState.Failed => ThreadLineState.Failed,
            _ => ThreadLineState.Idle
        };
        if (state == ThreadLineState.Latched)
        {
            Node2D caster = GodotObject.IsInstanceValid(_claw)
                ? _claw.GetNodeOrNull<Node2D>(_claw.PlayerPath) : null;
            bool blocked = MechanicalClawTargetResolver.TryGetLineOcclusion(caster, target,
                frame.CasterWorldPosition, frame.AnchorPoint, out Vector2 hit, out Vector2 normal);
            // Admission rays intentionally ignore an origin inside a one-way platform.
            // Rendering must still clip there, even when gameplay visibility is true.
            if (blocked || !frame.EffectiveVisibility)
            {
                if (blocked && !normal.IsZeroApprox() && frame.CasterWorldPosition.DistanceTo(hit) > 8f)
                {
                    // Normal clearance works for grazing rays; the single 2.1px strand
                    // has no lateral bends that could cross the blocking plane.
                    Present(frame.CasterWorldPosition, hit + normal.Normalized() * 4f, 1f,
                        ThreadLineState.Occluded);
                }
                else Clear(); // Too close/inside/out of range: retain only the local cue.
                IsConnectionObscured = true;
                return;
            }
        }
        if (state == ThreadLineState.Idle || !frame.EffectiveVisibility &&
            frame.State != MechanicalClawState.Failed)
        {
            Clear();
            return;
        }

        Vector2 end = frame.AnchorPoint;
        if (frame.State == MechanicalClawState.Travel)
            end = frame.CasterWorldPosition.MoveToward(frame.AnchorPoint, frame.ShuttleDistance);
        float distance = frame.CasterWorldPosition.DistanceTo(frame.AnchorPoint);
        float tension = frame.RopeLength <= .001f
            ? .35f
            : Mathf.Clamp(distance / frame.RopeLength - .05f, 0f, 1f);
        Present(frame.CasterWorldPosition, end, tension, state);
    }

    private void EnsureStrands()
    {
        if (_strands.Count > 0) return;
        ConfigureStrand(this);
        _strands.Add(this);
        for (int index = 1; index < StrandTotal; index++)
        {
            var strand = new Line2D { Name = $"Strand{index + 1}" };
            ConfigureStrand(strand);
            strand.ZIndex = -index;
            AddChild(strand);
            _strands.Add(strand);
        }
    }

    private static void ConfigureStrand(Line2D strand)
    {
        strand.Width = 1.8f;
        strand.Antialiased = true;
        strand.JointMode = LineJointMode.Round;
        strand.BeginCapMode = LineCapMode.Round;
        strand.EndCapMode = LineCapMode.Round;
    }

    private static Color ColorFor(ThreadLineState state) => state switch
    {
        ThreadLineState.Preview => new Color(0.38f, 0.92f, 0.9f, .48f),
        ThreadLineState.Fired => new Color("d7b85b"),
        ThreadLineState.Travel => new Color("82e6dc"),
        ThreadLineState.Latched => new Color("b6f4eb"),
        ThreadLineState.Occluded => new Color("8de4d8"),
        ThreadLineState.Connected => new Color("9ee8df"),
        ThreadLineState.Recovery => new Color(0.5f, 0.86f, 0.83f, .72f),
        ThreadLineState.Failed => new Color("b75a4b"),
        _ => Colors.Transparent
    };

    private static float WidthFor(ThreadLineState state, int index)
    {
        float center = state switch
        {
            ThreadLineState.Preview => 1.2f,
            ThreadLineState.Latched => 2.1f,
            ThreadLineState.Occluded => 2.1f,
            ThreadLineState.Connected => 2.2f,
            ThreadLineState.Recovery => 1.55f,
            ThreadLineState.Failed => 2f,
            _ => 1.7f
        };
        return index == 0 ? center : center * .82f;
    }
}
