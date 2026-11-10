using System.Collections.Generic;
using Godot;
using Knight.Audio;

namespace Knight.Ui;

public enum PrologueAimReticleState
{
    Attack,
    Grippable,
    Ice,
    Forbidden,
    Obstructed,
    OutOfRange,
    [System.Obsolete("Retraction uses the out-of-range reticle; use OutOfRange.")]
    Retracting = OutOfRange
}

public readonly record struct PrologueAimReticleStyle(
    Color Color,
    string GeometrySignature,
    PrologueAudioCue AudioCue);

public readonly record struct PrologueAimReticleFrame(
    PrologueAimReticleState State,
    Vector2 ScreenPosition,
    bool Visible);

public partial class PrologueAimReticle : Control
{
    public const float Diameter = 64f;
    private const float ProtectionPadding = 8f;
    public PrologueAimReticleFrame CurrentFrame { get; private set; } =
        new(PrologueAimReticleState.Attack, Vector2.Zero, true);
    public PrologueAimReticleStyle CurrentStyle => StyleFor(CurrentFrame.State);

    public static PrologueAimReticleStyle StyleFor(PrologueAimReticleState state) => state switch
    {
        PrologueAimReticleState.Attack => new(new Color("f2e6c9"), "open-circle",
            PrologueAudioCue.ModeAttack),
        PrologueAimReticleState.Grippable => new(new Color("72d9d1"), "diamond",
            PrologueAudioCue.ClawLatch),
        PrologueAimReticleState.Ice => new(new Color("8ebcf2"), "crossed-ice-bars",
            PrologueAudioCue.ClawIceSlip),
        PrologueAimReticleState.Forbidden => new(new Color("c98076"), "blocked-hex-crossbar",
            PrologueAudioCue.ClawFail),
        PrologueAimReticleState.Obstructed => new(new Color("ef7b68"), "dashed-obstruction-arc",
            PrologueAudioCue.ThreadFail),
        PrologueAimReticleState.OutOfRange => new(new Color("b7a0c9"), "dual-range-ring",
            PrologueAudioCue.ThreadPreview),
        _ => throw new System.ArgumentOutOfRangeException(nameof(state), state, null)
    };

    public void Present(PrologueAimReticleFrame frame, Rect2 safeBounds,
        IReadOnlyList<Rect2> protectedRegions = null)
    {
        Vector2 half = Vector2.One * (Diameter * .5f);
        Vector2 clamped = AvoidProtectedRegions(frame.ScreenPosition, safeBounds, protectedRegions);
        CurrentFrame = frame with { ScreenPosition = clamped };
        Position = clamped - half;
        Size = Vector2.One * Diameter;
        Visible = frame.Visible;
        QueueRedraw();
    }

    public static Vector2 AvoidProtectedRegions(Vector2 requestedCenter, Rect2 safeBounds,
        IReadOnlyList<Rect2> protectedRegions)
    {
        Vector2 half = Vector2.One * (Diameter * .5f);
        Vector2 center = ClampCenter(requestedCenter, safeBounds, half);
        if (!IntersectsProtected(new Rect2(center - half, Vector2.One * Diameter), protectedRegions))
            return center;

        var candidates = new List<Vector2> { center };
        if (protectedRegions != null)
        {
            foreach (Rect2 protectedRegion in protectedRegions)
            {
                Rect2 region = protectedRegion.Grow(ProtectionPadding);
                candidates.Add(new Vector2(center.X, region.Position.Y - half.Y));
                candidates.Add(new Vector2(center.X, region.End.Y + half.Y));
                candidates.Add(new Vector2(region.Position.X - half.X, center.Y));
                candidates.Add(new Vector2(region.End.X + half.X, center.Y));
                candidates.Add(region.Position - half);
                candidates.Add(new Vector2(region.End.X + half.X, region.Position.Y - half.Y));
                candidates.Add(new Vector2(region.Position.X - half.X, region.End.Y + half.Y));
                candidates.Add(region.End + half);
            }
        }

        Vector2 best = center;
        float bestDistance = float.PositiveInfinity;
        foreach (Vector2 candidate in candidates)
        {
            Vector2 clamped = ClampCenter(candidate, safeBounds, half);
            Rect2 bounds = new(clamped - half, Vector2.One * Diameter);
            if (IntersectsProtected(bounds, protectedRegions)) continue;
            float distance = center.DistanceSquaredTo(clamped);
            if (distance < bestDistance)
            {
                best = clamped;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static Vector2 ClampCenter(Vector2 center, Rect2 safeBounds, Vector2 half)
    {
        Vector2 minimum = safeBounds.Position + half;
        Vector2 maximum = safeBounds.End - half;
        return new Vector2(Mathf.Clamp(center.X, minimum.X, maximum.X),
            Mathf.Clamp(center.Y, minimum.Y, maximum.Y));
    }

    private static bool IntersectsProtected(Rect2 bounds, IReadOnlyList<Rect2> protectedRegions)
    {
        if (protectedRegions == null) return false;
        foreach (Rect2 region in protectedRegions)
            if (bounds.Intersects(region.Grow(ProtectionPadding))) return true;
        return false;
    }

    public override void _Draw()
    {
        Color color = CurrentStyle.Color;
        Vector2 center = Size * .5f;
        switch (CurrentFrame.State)
        {
            case PrologueAimReticleState.Attack:
                DrawArc(center, 11f, .25f, Mathf.Tau - .25f, 28, color, 2.5f, true);
                DrawLine(center + Vector2.Right * 16, center + Vector2.Right * 23, color, 2f);
                break;
            case PrologueAimReticleState.Grippable:
                DrawPolyline([center + Vector2.Up * 14, center + Vector2.Right * 14,
                    center + Vector2.Down * 14, center + Vector2.Left * 14, center + Vector2.Up * 14],
                    color, 3f, true);
                break;
            case PrologueAimReticleState.Ice:
                DrawLine(center + new Vector2(-14, -14), center + new Vector2(14, 14), color, 4f);
                DrawLine(center + new Vector2(-14, 14), center + new Vector2(14, -14), color, 4f);
                DrawLine(center + new Vector2(-18, 0), center + new Vector2(18, 0), color, 2f);
                break;
            case PrologueAimReticleState.Forbidden:
                DrawPolyline([center + new Vector2(-12, -16), center + new Vector2(12, -16),
                    center + new Vector2(20, 0), center + new Vector2(12, 16),
                    center + new Vector2(-12, 16), center + new Vector2(-20, 0),
                    center + new Vector2(-12, -16)], color, 3f, true);
                DrawLine(center + Vector2.Left * 22, center + Vector2.Right * 22, color, 5f);
                break;
            case PrologueAimReticleState.Obstructed:
                for (int index = 0; index < 5; index++)
                    DrawArc(center, 16f, -.9f + index * .36f, -.72f + index * .36f,
                        4, color, 3f, true);
                DrawLine(center + new Vector2(-10, 10), center + new Vector2(10, -10), color, 2f);
                break;
            case PrologueAimReticleState.OutOfRange:
                DrawArc(center, 20f, 0, Mathf.Tau, 36, color, 2f, true);
                DrawArc(center, 8f, 0, Mathf.Tau, 24, color, 1.5f, true);
                break;
        }
    }
}
