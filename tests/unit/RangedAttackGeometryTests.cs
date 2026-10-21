using System;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;

namespace Knight.Tests.Unit;

public static class RangedAttackGeometryTests
{
    public static void Run()
    {
        ScatterUsesEvenSymmetricAnglesAndNormalizesAim();
        BeamUsesTheSameCapsuleAsItsVisibleDanger();
        AreaClippingContainsTheWholeRoundProjectile();
        AreaClippingRejectsIllegalOriginsAndNonFiniteInputs();
        RotatedBoundsFollowActualShapeNotUnrotatedExtents();
        PolygonBoundsUseAlreadyWorldSpacePoints();
        BoundsRejectNonFiniteOrNegativeGeometry();
        GD.Print("RANGED_ATTACK_GEOMETRY_MATH_OK literal_expectations=true");
    }

    // Catches one central ray, non-normalized aim, or angle interpolation that
    // uses count rather than count-1. Values are hand-checked cos/sin 16 and 8 degrees.
    private static void ScatterUsesEvenSymmetricAnglesAndNormalizesAim()
    {
        Vector2[] directions = RangedAttackGeometry.ScatterDirections(new Vector2(3f, 0f), 5, 16f);
        TestAssert.Equal(5, directions.Length,
            "RANGED_SCATTER_GEOMETRY_RED five real pellet directions are emitted");
        Vector2[] expected =
        [
            new(.9612617f, -.27563736f), new(.99026807f, -.1391731f),
            new(1f, 0f), new(.99026807f, .1391731f), new(.9612617f, .27563736f)
        ];
        for (int i = 0; i < expected.Length; i++)
            NearVector(expected[i], directions[i], .00001f, "literal symmetric pellet " + i);
        Vector2[] single = RangedAttackGeometry.ScatterDirections(new Vector2(0f, 7f), 1, 16f);
        TestAssert.Equal(1, single.Length, "a single pellet does not divide by zero");
        NearVector(new Vector2(0f, 1f), single[0], .00001f, "one pellet stays on the normalized aim");
        Vector2[] pair = RangedAttackGeometry.ScatterDirections(new Vector2(0f, 2f), 2, 30f);
        TestAssert.Equal(2, pair.Length, "two pellets include both fan edges");
        NearVector(new Vector2(.5f, .8660254f), pair[0], .00001f, "two-pellet clockwise edge");
        NearVector(new Vector2(-.5f, .8660254f), pair[1], .00001f, "two-pellet counterclockwise edge");
    }

    // Catches Capsule's local-Y axis being mistaken for local-X, or dropping
    // the two round caps from the declared half-height.
    private static void BeamUsesTheSameCapsuleAsItsVisibleDanger()
    {
        AttackShapeFrame beam = RangedAttackGeometry.Beam(new Vector2(10f, 20f), new Vector2(70f, 100f), 5f);
        TestAssert.Equal(AttackShapeKind.Capsule, beam.Kind, "RANGED_BEAM_CAPSULE_RED beam is a capsule");
        NearVector(new Vector2(40f, 60f), beam.Origin, .00001f, "beam capsule is centred between endpoints");
        NearVector(new Vector2(5f, 55f), beam.Extent, .00001f, "length100 plus radius5 has half-height55");
        NearFinite(-.6435011f, beam.RotationRadians, .00001f, "beam rotation maps capsule local Y to 3:4 aim");
        NearRect(new Rect2(5f, 15f, 70f, 90f), AttackShapeBounds.WorldBounds(beam), .0001f,
            "diagonal round-ended beam bounds include the literal endpoint discs");
    }

    // Catches independent x/y clamping (which bends a shot), missing inset,
    // and clipping only the projectile centre rather than its complete circle.
    private static void AreaClippingContainsTheWholeRoundProjectile()
    {
        Rect2 arena = new(0f, 0f, 100f, 80f);
        NearVector(new Vector2(90f, 56f), RangedAttackGeometry.ClipToArea(
                new Vector2(50f, 40f), new Vector2(200f, 100f), arena, 10f), .0001f,
            "RANGED_AREA_CLIP_RED full radius10 stops on x90 without changing the shot line");
        NearVector(new Vector2(10f, 24f), RangedAttackGeometry.ClipToArea(
                new Vector2(50f, 40f), new Vector2(-100f, -20f), arena, 10f), .0001f,
            "leftward diagonal clips on the same original segment");
        NearVector(new Vector2(50f, 10f), RangedAttackGeometry.ClipToArea(
                new Vector2(50f, 40f), new Vector2(50f, -100f), arena, 10f), .0001f,
            "vertical shot respects radius at arena top");
        NearVector(new Vector2(70f, 60f), RangedAttackGeometry.ClipToArea(
                new Vector2(50f, 40f), new Vector2(70f, 60f), arena, 10f), .0001f,
            "an entirely legal segment is not shortened");
        NearVector(new Vector2(90f, 40f), RangedAttackGeometry.ClipToArea(
                new Vector2(90f, 40f), new Vector2(200f, 40f), arena, 10f), .0001f,
            "an outward shot from the full-radius boundary cannot escape");
    }

    // Catches teleporting an illegal origin back into the arena or allowing
    // invalid sizes/insets/endpoints to authorize a non-finite or outside shot.
    private static void AreaClippingRejectsIllegalOriginsAndNonFiniteInputs()
    {
        Vector2 from = new(50f, 40f);
        Rect2 arena = new(0f, 0f, 100f, 80f);
        NearVector(new Vector2(5f, 40f), RangedAttackGeometry.ClipToArea(
            new Vector2(5f, 40f), new Vector2(60f, 40f), arena, 10f), 0f,
            "illegal full-circle origin returns unchanged, never teleports inward");
        foreach (float inset in new[] { -1f, 41f, float.NaN, float.PositiveInfinity })
            NearVector(from, RangedAttackGeometry.ClipToArea(from, new Vector2(70f, 40f), arena, inset), 0f,
                "invalid radius fails closed at origin: " + inset);
        foreach (Rect2 invalid in new[]
                 {
                     new Rect2(100f, 0f, -100f, 80f), new Rect2(0f, 0f, 0f, 80f),
                     new Rect2(float.NaN, 0f, 100f, 80f), new Rect2(0f, 0f, float.PositiveInfinity, 80f)
                 })
            NearVector(from, RangedAttackGeometry.ClipToArea(from, new Vector2(70f, 40f), invalid, 5f), 0f,
                "invalid allowed area fails closed at the finite origin");
        NearVector(from, RangedAttackGeometry.ClipToArea(from, new Vector2(float.NaN, 40f), arena, 5f), 0f,
            "non-finite endpoint fails closed");
    }

    // Catches using the unrotated Extent rectangle for fog/HUD protection,
    // rotating ellipse as a rectangle, and rotating capsule's cap radius.
    private static void RotatedBoundsFollowActualShapeNotUnrotatedExtents()
    {
        NearRect(new Rect2(90f, 180f, 20f, 40f), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.Rectangle, new Vector2(100f, 200f), new Vector2(20f, 10f), Mathf.Pi / 2f)), .0001f,
            "RANGED_ROTATED_BOUNDS_RED a quarter-turn rectangle swaps world width and height");
        NearRect(new Rect2(7.8786797f, 17.8786797f, 4.2426407f, 4.2426407f), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.GroundBand, new Vector2(10f, 20f), new Vector2(2f, 1f), Mathf.Pi / 4f)), .0001f,
            "ground band is a truly rotated rectangle");
        NearRect(new Rect2(6.8377223f, 16.8377223f, 6.3245553f, 6.3245553f), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.Circle, new Vector2(10f, 20f), new Vector2(4f, 2f), Mathf.Pi / 4f)), .0001f,
            "rotated ellipse support is sqrt10, not the rectangle's 3sqrt2");
        NearRect(new Rect2(7f, 17f, 6f, 6f), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.Circle, new Vector2(10f, 20f), new Vector2(3f, 3f), .71f)), .0001f,
            "a true circle is rotation invariant");
        NearRect(new Rect2(4.4644661f, 14.4644661f, 11.071068f, 11.071068f), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.Capsule, new Vector2(10f, 20f), new Vector2(2f, 7f), Mathf.Pi / 4f)), .0001f,
            "capsule support is radius2 plus projected straight half-length5");
        NearRect(new Rect2(3f, 18f, 14f, 4f), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.Capsule, new Vector2(10f, 20f), new Vector2(2f, 7f), -Mathf.Pi / 2f)), .0001f,
            "horizontal capsule includes full circular endcaps");
        NearRect(new Rect2(), AttackShapeBounds.WorldBounds(Shape(
            AttackShapeKind.None, new Vector2(100f, 200f), Vector2.Zero, 0f)), 0f,
            "None exposes no stale protected geometry");
    }

    // Catches applying Origin/Rotation a second time to polygon vertices that
    // AttackTelegraphVisual already interprets directly in world space.
    private static void PolygonBoundsUseAlreadyWorldSpacePoints()
    {
        AttackShapeFrame polygon = new(AttackShapeKind.ConvexPolygon,
            new Vector2(999f, -400f), new Vector2(12f, 9f), Mathf.Pi / 2f,
            new Vector2[] { new(30f, 60f), new(45f, 55f), new(50f, 80f), new(35f, 90f) });
        NearRect(new Rect2(30f, 55f, 20f, 35f), AttackShapeBounds.WorldBounds(polygon), 0f,
            "polygon bounds come from literal world vertices, not a second transform");
    }

    // Catches NaN comparisons silently accepting invalid protection geometry.
    private static void BoundsRejectNonFiniteOrNegativeGeometry()
    {
        AttackShapeFrame rectangle = Shape(AttackShapeKind.Rectangle, Vector2.Zero, Vector2.One, 0f);
        foreach (AttackShapeFrame invalid in new[]
                 {
                     rectangle with { Origin = new Vector2(float.NaN, 0f) },
                     rectangle with { Extent = new Vector2(1f, float.PositiveInfinity) },
                     rectangle with { RotationRadians = float.NegativeInfinity },
                     rectangle with { Extent = new Vector2(-1f, 1f) },
                     rectangle with { Kind = (AttackShapeKind)999 },
                     rectangle with { Kind = AttackShapeKind.ConvexPolygon,
                         Points = new Vector2[] { Vector2.Zero, Vector2.One, new(float.NaN, 1f) } }
                 })
            AssertThrows<ArgumentOutOfRangeException>(() => AttackShapeBounds.WorldBounds(invalid),
                "malformed world bounds are rejected before use");
    }

    private static AttackShapeFrame Shape(AttackShapeKind kind, Vector2 origin, Vector2 extent, float rotation) =>
        new(kind, origin, extent, rotation, Array.Empty<Vector2>());

    private static void NearRect(Rect2 expected, Rect2 actual, float tolerance, string message)
    {
        NearVector(expected.Position, actual.Position, tolerance, message + " position");
        NearVector(expected.Size, actual.Size, tolerance, message + " size");
    }

    private static void NearVector(Vector2 expected, Vector2 actual, float tolerance, string message)
    {
        NearFinite(expected.X, actual.X, tolerance, message + " X");
        NearFinite(expected.Y, actual.Y, tolerance, message + " Y");
    }

    private static void NearFinite(float expected, float actual, float tolerance, string message)
    {
        TestAssert.True(float.IsFinite(actual), message + " remains finite");
        TestAssert.Near(expected, actual, tolerance, message);
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Assertion failed: {message}; expected {typeof(T).Name}");
    }
}
