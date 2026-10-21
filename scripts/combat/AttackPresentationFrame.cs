using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Combat;

public enum AttackPhase
{
    None,
    Telegraph,
    Active,
    Recovery
}

public enum EnemyAttackKind
{
    None,
    RustShellThrust,
    MothDive,
    LaborerSweep,
    BossSweep,
    BossLeapSlam,
    BossGroundWave,
    BossHammerThrow,
    BellBoltShot,
    SkitterRush,
    RivetScatterShot,
    BeltBurstShot,
    FurnaceMissileShot,
    PrismLaserBeam
}

public readonly record struct AttackTimelineFrame(
    ulong AttackId,
    EnemyAttackKind Kind,
    AttackPhase Phase,
    float PhaseDurationSeconds,
    float PhaseRemainingSeconds,
    Vector2 LockedWorldTarget,
    bool HasLockedTarget);

public readonly record struct AttackPresentationFrame(
    AttackTimelineFrame Timeline,
    AttackShapeFrame Shape)
{
    public AttackPresentationFrame Validate()
    {
        ValidateTimeline();
        ValidateShape();
        return this with { Shape = Shape with { Points = FreezePoints(Shape.Points) } };
    }

    private void ValidateTimeline()
    {
        if (!Enum.IsDefined(Timeline.Kind))
            throw new ArgumentOutOfRangeException(nameof(Timeline.Kind));
        if (!Enum.IsDefined(Timeline.Phase))
            throw new ArgumentOutOfRangeException(nameof(Timeline.Phase));
        if (!IsFinite(Timeline.PhaseDurationSeconds) || Timeline.PhaseDurationSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(Timeline.PhaseDurationSeconds));
        if (!IsFinite(Timeline.PhaseRemainingSeconds) || Timeline.PhaseRemainingSeconds < 0f ||
            Timeline.PhaseRemainingSeconds > Timeline.PhaseDurationSeconds)
            throw new ArgumentOutOfRangeException(nameof(Timeline.PhaseRemainingSeconds));
        if (Timeline.Phase != AttackPhase.None && Timeline.Kind == EnemyAttackKind.None)
            throw new ArgumentOutOfRangeException(nameof(Timeline.Kind));
        if (Timeline.Phase != AttackPhase.None && Timeline.AttackId == 0)
            throw new ArgumentOutOfRangeException(nameof(Timeline.AttackId));
        if (Timeline.HasLockedTarget && !IsFinite(Timeline.LockedWorldTarget))
            throw new ArgumentOutOfRangeException(nameof(Timeline.LockedWorldTarget));
    }

    private void ValidateShape()
    {
        if (!Enum.IsDefined(Shape.Kind))
            throw new ArgumentOutOfRangeException(nameof(Shape.Kind));
        if (!IsFinite(Shape.Origin))
            throw new ArgumentOutOfRangeException(nameof(Shape.Origin));
        if (!IsFinite(Shape.Extent) || Shape.Extent.X < 0f || Shape.Extent.Y < 0f)
            throw new ArgumentOutOfRangeException(nameof(Shape.Extent));
        if (!IsFinite(Shape.RotationRadians))
            throw new ArgumentOutOfRangeException(nameof(Shape.RotationRadians));
        if (Shape.Points == null)
            throw new ArgumentOutOfRangeException(nameof(Shape.Points));
        if (Timeline.Phase != AttackPhase.None && Shape.Kind == AttackShapeKind.None)
            throw new ArgumentOutOfRangeException(nameof(Shape), "Only None phases may omit geometry.");
        if (Timeline.Phase != AttackPhase.None && RequiresPositiveExtent(Shape.Kind) &&
            (Shape.Extent.X <= 0f || Shape.Extent.Y <= 0f))
            throw new ArgumentOutOfRangeException(nameof(Shape.Extent), "Attack geometry requires positive extents.");
        if (Shape.Kind == AttackShapeKind.ConvexPolygon)
            ValidateConvexPolygon(Shape.Points);
    }

    private static void ValidateConvexPolygon(IReadOnlyList<Vector2> points)
    {
        if (points == null || points.Count < 3)
            throw new ArgumentOutOfRangeException(nameof(points), "A convex polygon requires at least three points.");

        float winding = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 previous = points[(i + points.Count - 1) % points.Count];
            Vector2 current = points[i];
            Vector2 next = points[(i + 1) % points.Count];
            if (!IsFinite(current))
                throw new ArgumentOutOfRangeException(nameof(points), "Polygon points must be finite.");

            float cross = (current - previous).Cross(next - current);
            if (MathF.Abs(cross) <= float.Epsilon)
                throw new ArgumentOutOfRangeException(nameof(points), "Polygon points must form non-collinear corners.");
            if (winding == 0f) winding = MathF.Sign(cross);
            else if (MathF.Sign(cross) != MathF.Sign(winding))
                throw new ArgumentOutOfRangeException(nameof(points), "Polygon points must be convex and consistently wound.");
        }

        for (int first = 0; first < points.Count; first++)
        {
            Vector2 firstStart = points[first];
            Vector2 firstEnd = points[(first + 1) % points.Count];
            for (int second = first + 1; second < points.Count; second++)
            {
                if (EdgesAreAdjacent(first, second, points.Count)) continue;
                Vector2 secondStart = points[second];
                Vector2 secondEnd = points[(second + 1) % points.Count];
                if (SegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd))
                    throw new ArgumentOutOfRangeException(nameof(points),
                        "Polygon edges must not intersect outside their shared corners.");
            }
        }
    }

    private static IReadOnlyList<Vector2> FreezePoints(IReadOnlyList<Vector2> points)
    {
        var copy = new Vector2[points.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = points[i];
        return Array.AsReadOnly(copy);
    }

    private static bool RequiresPositiveExtent(AttackShapeKind kind) => kind is
        AttackShapeKind.Rectangle or AttackShapeKind.Circle or AttackShapeKind.Capsule or AttackShapeKind.GroundBand;

    private static bool EdgesAreAdjacent(int first, int second, int count) =>
        first == second || (first + 1) % count == second || (second + 1) % count == first;

    private static bool SegmentsIntersect(Vector2 firstStart, Vector2 firstEnd, Vector2 secondStart, Vector2 secondEnd)
    {
        float firstStartSide = (firstEnd - firstStart).Cross(secondStart - firstStart);
        float firstEndSide = (firstEnd - firstStart).Cross(secondEnd - firstStart);
        float secondStartSide = (secondEnd - secondStart).Cross(firstStart - secondStart);
        float secondEndSide = (secondEnd - secondStart).Cross(firstEnd - secondStart);

        if (OppositeSides(firstStartSide, firstEndSide) && OppositeSides(secondStartSide, secondEndSide)) return true;
        return IsOnSegment(firstStart, firstEnd, secondStart) || IsOnSegment(firstStart, firstEnd, secondEnd) ||
            IsOnSegment(secondStart, secondEnd, firstStart) || IsOnSegment(secondStart, secondEnd, firstEnd);
    }

    private static bool OppositeSides(float first, float second) =>
        (first < 0f && second > 0f) || (first > 0f && second < 0f);

    private static bool IsOnSegment(Vector2 start, Vector2 end, Vector2 point) =>
        MathF.Abs((end - start).Cross(point - start)) <= 0.0001f &&
        (point - start).Dot(point - end) <= 0.0001f;

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool IsFinite(Vector2 value) => IsFinite(value.X) && IsFinite(value.Y);
}
