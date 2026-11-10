using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Tests.Unit;

public static class AttackPresentationFrameTests
{
    public static void Run()
    {
        RejectsNegativeDurationOrRemainingTime();
        RejectsRemainingTimeAboveTheDeclaredPhase();
        PreservesAttackIdentityAcrossAllThreePhases();
        RejectsNegativeShapeExtent();
        RejectsSelfIntersectingPolygon();
        RejectsZeroExtentGeometryAcrossExtentBasedShapes();
        PreservesLockedWorldTargetAcrossTelegraphAndActive();
    }

    private static void RejectsNegativeDurationOrRemainingTime()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(duration: -0.01f).Validate(),
            "negative phase duration is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(remaining: -0.01f).Validate(),
            "negative phase remaining time is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(kind: (EnemyAttackKind)999).Validate(),
            "invalid enemy attack kind is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(phase: (AttackPhase)999).Validate(),
            "invalid attack phase is rejected");
    }

    private static void RejectsRemainingTimeAboveTheDeclaredPhase()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(duration: 0.65f, remaining: 0.66f).Validate(),
            "remaining time beyond the declared phase duration is rejected");
    }

    private static void PreservesAttackIdentityAcrossAllThreePhases()
    {
        const ulong attackId = 41;
        const EnemyAttackKind kind = EnemyAttackKind.LaborerSweep;
        foreach (AttackPhase phase in new[] { AttackPhase.Telegraph, AttackPhase.Active, AttackPhase.Recovery })
        {
            AttackPresentationFrame frame = Frame(attackId: attackId, kind: kind, phase: phase).Validate();
            TestAssert.Equal(attackId, frame.Timeline.AttackId, $"{phase} preserves the stable attack identity");
            TestAssert.Equal(kind, frame.Timeline.Kind, $"{phase} preserves the attack kind");
        }

        AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: NoneShape()).Validate(),
            "a non-None phase requires attack geometry");
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: Polygon(new Vector2(0f, 0f), new Vector2(1f, 0f))).Validate(),
            "a malformed convex polygon is rejected");

        var sourcePoints = new List<Vector2>
        {
            new(0f, 0f), new(4f, 0f), new(0f, 3f)
        };
        AttackPresentationFrame validated = Frame(shape: new AttackShapeFrame(
            AttackShapeKind.ConvexPolygon, Vector2.Zero, Vector2.Zero, 0f, sourcePoints)).Validate();
        sourcePoints[0] = new Vector2(9f, 9f);
        TestAssert.Equal(new Vector2(0f, 0f), validated.Shape.Points[0],
            "validated polygon points do not retain a mutable caller collection");
    }

    private static void RejectsNegativeShapeExtent()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: new AttackShapeFrame(
            AttackShapeKind.Rectangle, Vector2.Zero, new Vector2(-1f, 2f), 0f, Array.Empty<Vector2>())).Validate(),
            "negative rectangle extent is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: new AttackShapeFrame(
            (AttackShapeKind)999, Vector2.Zero, new Vector2(1f, 2f), 0f, Array.Empty<Vector2>())).Validate(),
            "invalid attack shape kind is rejected");
    }

    private static void RejectsZeroExtentGeometryAcrossExtentBasedShapes()
    {
        foreach (AttackShapeKind kind in new[]
                 {
                     AttackShapeKind.Rectangle, AttackShapeKind.Circle,
                     AttackShapeKind.Capsule, AttackShapeKind.GroundBand
                 })
        {
            AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: new AttackShapeFrame(
                kind, Vector2.Zero, new Vector2(0f, 2f), 0f, Array.Empty<Vector2>())).Validate(),
                $"{kind} rejects a zero horizontal extent");
            AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: new AttackShapeFrame(
                kind, Vector2.Zero, new Vector2(2f, 0f), 0f, Array.Empty<Vector2>())).Validate(),
                $"{kind} rejects a zero vertical extent");
        }
    }

    private static void RejectsSelfIntersectingPolygon()
    {
        AttackShapeFrame pentagram = Polygon(
            new Vector2(0f, -4f), new Vector2(2.351f, 3.236f), new Vector2(-3.804f, -1.236f),
            new Vector2(3.804f, -1.236f), new Vector2(-2.351f, 3.236f));

        AssertThrows<ArgumentOutOfRangeException>(() => Frame(shape: pentagram).Validate(),
            "same-winding polygon with crossing non-adjacent edges is rejected");
    }

    private static void PreservesLockedWorldTargetAcrossTelegraphAndActive()
    {
        Vector2 lockedTarget = new(320f, -48f);
        AttackPresentationFrame telegraph = Frame(phase: AttackPhase.Telegraph, lockedTarget: lockedTarget, hasLockedTarget: true).Validate();
        AttackPresentationFrame active = Frame(phase: AttackPhase.Active, lockedTarget: lockedTarget, hasLockedTarget: true).Validate();

        TestAssert.True(telegraph.Timeline.HasLockedTarget && active.Timeline.HasLockedTarget,
            "locked world target remains marked across telegraph and active");
        TestAssert.Equal(lockedTarget, telegraph.Timeline.LockedWorldTarget,
            "telegraph keeps its locked world target");
        TestAssert.Equal(lockedTarget, active.Timeline.LockedWorldTarget,
            "active keeps the telegraph locked world target");
    }

    private static AttackPresentationFrame Frame(
        ulong attackId = 41,
        EnemyAttackKind kind = EnemyAttackKind.LaborerSweep,
        AttackPhase phase = AttackPhase.Telegraph,
        float duration = 0.75f,
        float remaining = 0.75f,
        AttackShapeFrame? shape = null,
        Vector2 lockedTarget = default,
        bool hasLockedTarget = false)
    {
        return new AttackPresentationFrame(
            new AttackTimelineFrame(attackId, kind, phase, duration, remaining, lockedTarget, hasLockedTarget),
            shape ?? RectangleShape());
    }

    private static AttackShapeFrame RectangleShape() => new(
        AttackShapeKind.Rectangle, Vector2.Zero, new Vector2(24f, 12f), 0f, Array.Empty<Vector2>());

    private static AttackShapeFrame NoneShape() => new(
        AttackShapeKind.None, Vector2.Zero, Vector2.Zero, 0f, Array.Empty<Vector2>());

    private static AttackShapeFrame Polygon(params Vector2[] points) => new(
        AttackShapeKind.ConvexPolygon, Vector2.Zero, Vector2.Zero, 0f, points);

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Assertion failed: {message}; expected {typeof(T).Name}");
    }
}
