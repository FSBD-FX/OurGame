using System;
using System.Collections.Generic;
using Godot;
using Knight.Presentation.Fog;

namespace Knight.Tests.Unit;

public static class HeadlampWedgeEquivalenceTests
{
    private const uint CorpusSeed = 0x6A09E667;
    private const int CorpusCases = 10000;
    private static readonly Rect2 UnitBounds = new(-1f, -1f, 2f, 2f);
    private static readonly Transform2D[] Affines =
    [
        Transform2D.Identity,
        new(new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(8f, -4f)),
        new(new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(-3f, 7f)),
        new(new Vector2(1f, 0f), new Vector2(.5f, 1f), new Vector2(2f, -6f)),
        new(new Vector2(1f, .5f), new Vector2(.5f, -1f), new Vector2(-8f, 4f)),
        new(new Vector2(.6f, .8f), new Vector2(-.8f, .6f), new Vector2(3f, -2f)),
        new(new Vector2(2f, 0f), new Vector2(.25f, .5f), new Vector2(-4f, 9f))
    ];
    private static readonly float[] ExtremeCoordinates =
    [
        0f, float.Epsilon, -float.Epsilon, 1e-30f, -1e-30f, 1e-20f, -1e-20f,
        1e-8f, -1e-8f, 1f, -1f, 1e8f, -1e8f, 1e20f, -1e20f,
        1e30f, -1e30f, float.MaxValue, -float.MaxValue
    ];

    public static void Run()
    {
        InteriorThinBlockerIsNotAuthorizedByClearEndpointRays();
        DestinationEdgeAndEitherRadialEdgeStillBlock();
        ClosedContactAndAdjacentUlpsStayDistinct();
        DegenerateWedgesKeepTheirEdgeCoverage();
        AffineSnapshotsKeepLocalRectangleGeometry();
        DoubleArithmeticAndLegacyConservativeContactArePreserved();
        FrameGatesAndEveryShapeStillMatter();
        DeterministicCorpusMatchesFrozenLegacy();
    }

    // Catches endpoint-only sampling and removal of the rectangle-corner test.
    private static void InteriorThinBlockerIsNotAuthorizedByClearEndpointRays()
    {
        var shape = new HeadlampOccluder(Transform2D.Identity, new Rect2(5f, -.0001f, .0001f, .0002f));
        var frame = new HeadlampOcclusionFrame([shape], true);
        Vector2 origin = Vector2.Zero, from = new(10f, -5f), to = new(10f, 5f);
        // At x=5 the two endpoint rays have y=-2.5/+2.5, while the
        // positive-area thin rectangle is strictly inside their triangle.
        TestAssert.True(frame.CanSee(origin, from), "thin interior blocker leaves the first endpoint ray clear");
        TestAssert.True(frame.CanSee(origin, to), "thin interior blocker leaves the second endpoint ray clear");
        TestAssert.Equal(false, frame.CanSee(origin, new Vector2(10f, 0f)),
            "the independently chosen middle ray crosses the thin blocker");
        AssertKnown(false, [shape], origin, from, to, "thin rectangle entirely inside the visibility wedge");
    }

    // Each literal rectangle intersects only its named triangle edge.
    // Catches dropping any of the three slabs while reusing transformed points.
    private static void DestinationEdgeAndEitherRadialEdgeStillBlock()
    {
        Vector2 origin = Vector2.Zero, from = new(10f, -5f), to = new(10f, 5f);
        AssertKnown(false, [Shape(new Rect2(9.75f, -.25f, .5f, .5f))], origin, from, to,
            "destination edge crosses the rectangle although both origin rays miss");
        AssertKnown(false, [Shape(new Rect2(4f, -2.25f, .25f, .5f))], origin, from, to,
            "first radial edge at y=-x/2 crosses the rectangle");
        AssertKnown(false, [Shape(new Rect2(4f, 1.75f, .25f, .5f))], origin, from, to,
            "second radial edge at y=x/2 crosses the rectangle");
        AssertKnown(true, [Shape(new Rect2(4f, 4f, 1f, 1f))], origin, from, to,
            "rectangle above y=x/2 is outside all triangle edges and its interior");
    }

    // Catches half-open slabs, an epsilon expansion, or a float/rounded comparison
    // that confuses a closed face with the very next representable coordinate.
    private static void ClosedContactAndAdjacentUlpsStayDistinct()
    {
        var cases = new (string Name, Vector2 Origin, Vector2 End, bool Visible)[]
        {
            ("top contact", new(-2f, -1f), new(2f, -1f), false),
            ("top outside ULP", new(-2f, MathF.BitDecrement(-1f)), new(2f, MathF.BitDecrement(-1f)), true),
            ("top inside ULP", new(-2f, MathF.BitIncrement(-1f)), new(2f, MathF.BitIncrement(-1f)), false),
            ("bottom contact", new(-2f, 1f), new(2f, 1f), false),
            ("bottom outside ULP", new(-2f, MathF.BitIncrement(1f)), new(2f, MathF.BitIncrement(1f)), true),
            ("bottom inside ULP", new(-2f, MathF.BitDecrement(1f)), new(2f, MathF.BitDecrement(1f)), false),
            ("left contact", new(-1f, -2f), new(-1f, 2f), false),
            ("left outside ULP", new(MathF.BitDecrement(-1f), -2f), new(MathF.BitDecrement(-1f), 2f), true),
            ("left inside ULP", new(MathF.BitIncrement(-1f), -2f), new(MathF.BitIncrement(-1f), 2f), false),
            ("right contact", new(1f, -2f), new(1f, 2f), false),
            ("right outside ULP", new(MathF.BitIncrement(1f), -2f), new(MathF.BitIncrement(1f), 2f), true),
            ("right inside ULP", new(MathF.BitDecrement(1f), -2f), new(MathF.BitDecrement(1f), 2f), false),
            ("corner-only contact", new(-2f, 0f), new(0f, -2f), false),
            ("terminal point touches corner", new(-2f, -2f), new(-1f, -1f), false)
        };
        foreach (var item in cases)
            AssertKnown(item.Visible, [Shape(UnitBounds)], item.Origin, item.End, item.End, item.Name);
    }

    // Catches zero-area wedges being discarded before their edges are tested.
    private static void DegenerateWedgesKeepTheirEdgeCoverage()
    {
        AssertKnown(false, [Shape(UnitBounds)], Vector2.Zero, Vector2.Zero, Vector2.Zero,
            "one point inside a rectangle is blocked");
        AssertKnown(true, [Shape(UnitBounds)], new(2f, 2f), new(2f, 2f), new(2f, 2f),
            "one point outside a rectangle is clear");
        AssertKnown(false, [Shape(UnitBounds)], new(-3f, 0f), new(3f, 0f), new(-2f, 0f),
            "collinear reversed endpoints still span the rectangle");
        AssertKnown(true, [Shape(UnitBounds)], new(-3f, 2f), new(3f, 2f), new(-2f, 2f),
            "collinear wedge above the rectangle is clear");
        AssertKnown(false, [Shape(UnitBounds)], Vector2.Zero, new(4f, -2f), new(4f, 2f),
            "origin inside the rectangle blocks a nondegenerate wedge");
    }

    // Catches row/column swaps, omitted translation, world-AABB substitution,
    // and assuming a positive determinant. Margins are large, not rounding ties.
    private static void AffineSnapshotsKeepLocalRectangleGeometry()
    {
        foreach (Transform2D inverse in Affines)
        {
            Transform2D forward = inverse.AffineInverse();
            Vector2 origin = forward * Vector2.Zero;
            Vector2 from = forward * new Vector2(10f, -5f), to = forward * new Vector2(10f, 5f);
            AssertKnown(false, [new(inverse, new Rect2(4f, -.25f, 1f, .5f))], origin, from, to,
                "affine local rectangle inside the wedge");
            AssertKnown(true, [new(inverse, new Rect2(4f, 4f, 1f, 1f))], origin, from, to,
                "affine local rectangle outside the wedge");
        }
    }

    // Catches float transform products/differences, and an exact same-side
    // reject that accidentally removes the legacy slab's conservative contact.
    private static void DoubleArithmeticAndLegacyConservativeContactArePreserved()
    {
        Transform2D extremeScale = new(new Vector2(1e20f, 0f), new Vector2(0f, 1e-20f), Vector2.Zero);
        AssertKnown(false, [new(extremeScale, UnitBounds)], new(-1e20f, 0f), new(1e20f, -1e20f), new(1e20f, 1e20f),
            "finite float inputs need double local products of order 1e40 to cross the slab");
        AssertKnown(false, [Shape(UnitBounds)], new(-float.MaxValue, 0f), new(float.MaxValue, -1f), new(float.MaxValue, 1f),
            "opposite maximum finite endpoints cross the rectangle without float subtraction overflow");
        AssertKnown(true, [Shape(UnitBounds)], new(float.MaxValue, float.MaxValue),
            new(float.MaxValue / 2f, float.MaxValue), new(float.MaxValue, float.MaxValue / 2f),
            "maximum finite triangle stays entirely in the distant positive quadrant");
        float tiny = float.Epsilon;
        AssertKnown(false, [Shape(new Rect2(-tiny, -tiny, 2f * tiny, 2f * tiny))],
            new(-4f * tiny, 0f), new(4f * tiny, -2f * tiny), new(4f * tiny, 2f * tiny),
            "subnormal finite triangle crosses a positive-size subnormal rectangle");
        AssertKnown(true, [Shape(new Rect2(0f, 0f, tiny, tiny))],
            new(4f * tiny, 4f * tiny), new(5f * tiny, 4f * tiny), new(4f * tiny, 5f * tiny),
            "subnormal finite triangle remains clear when separated");

        // Literal false is intentional legacy characterization, not a claim of
        // exact mathematical intersection. With float inputs promoted to double,
        // dx=(1e-20f)-(1e30f) rounds to -x0; (-1-x0)/dx and (0-x0)/dx
        // both round to 1. The closed X slab therefore accepts t=1, whose
        // Y is -1 (inside [-2,2]). All three input Xs are nevertheless >0.
        // A raw "all vertices right of rect.End" reject changes this result.
        AssertKnown(false, [Shape(new Rect2(-1f, -2f, 1f, 4f))],
            new(1e30f, 0f), new(1e-20f, -1f), new(1e-20f, 1f),
            "legacy cancellation contact must survive transform-only optimization");
    }

    // Catches an early clear return, stopping after the first shape, or removing
    // the complete/finite gates merely because no shapes were captured.
    private static void FrameGatesAndEveryShapeStillMatter()
    {
        Vector2 origin = Vector2.Zero, from = new(10f, -5f), to = new(10f, 5f);
        AssertKnown(true, [], origin, from, to, "complete empty snapshot is clear");
        var empty = new HeadlampOcclusionFrame([], false, "fixture-incomplete");
        TestAssert.Equal(false, empty.CanSeeSegment(origin, from, to), "incomplete empty snapshot fails closed");
        TestAssert.Equal(false, LegacyCanSeeSegment([], origin, from, to, false), "frozen legacy incomplete gate");
        HeadlampOccluder clear = Shape(new Rect2(-20f, 4f, 1f, 1f));
        HeadlampOccluder blocking = Shape(new Rect2(4f, -.25f, 1f, .5f));
        AssertKnown(false, [clear, blocking], origin, from, to, "later shape still blocks the wedge");
        AssertKnown(false, [blocking, clear], origin, from, to, "earlier blocker is not overwritten by a later clear shape");
        AssertKnown(true, [clear, Shape(new Rect2(4f, 4f, 1f, 1f))], origin, from, to, "all snapshot shapes are clear");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        foreach (Vector2 bad in new[] { new Vector2(invalid, 0f), new Vector2(0f, invalid) })
        {
            AssertKnown(false, [], bad, from, to, "nonfinite origin fails closed before empty shape iteration");
            AssertKnown(false, [], origin, bad, to, "nonfinite from fails closed before empty shape iteration");
            AssertKnown(false, [], origin, from, bad, "nonfinite to fails closed before empty shape iteration");
        }
    }

    // Not an independent geometry oracle: this corpus characterizes equivalence
    // to the frozen pre-optimization implementation below. Literal tests above
    // separately prevent a shared reference mistake from defining all expected outcomes.
    private static void DeterministicCorpusMatchesFrozenLegacy()
    {
        uint state = CorpusSeed;
        int visible = 0, blocked = 0;
        for (int index = 0; index < CorpusCases; index++)
        {
            int familyIteration = index / 6;
            Transform2D inverse = Affines[Next(ref state, Affines.Length)];
            Vector2 origin, from, to;
            Rect2 bounds;
            switch (index % 6)
            {
                case 0: // Ordinary triangles, several rectangles, non-axis affine maps.
                    origin = GridPoint(ref state); from = GridPoint(ref state); to = GridPoint(ref state);
                    bounds = new Rect2(GridPoint(ref state), new Vector2((Next(ref state, 64) + 1) * .125f,
                        (Next(ref state, 64) + 1) * .125f));
                    break;
                case 1: // Subpixel interior blockers cannot be found by endpoint rays alone.
                    Transform2D forward = inverse.AffineInverse();
                    float reach = Next(ref state, 100) + 12f, span = Next(ref state, 40) + 4f;
                    origin = forward * Vector2.Zero;
                    from = forward * new Vector2(reach, -span); to = forward * new Vector2(reach, span);
                    float width = MathF.ScaleB(1f, -Next(ref state, 16) - 1);
                    bounds = new Rect2(reach * .5f, -width, width, width * 2f);
                    break;
                case 2: // Coincident and collinear inputs, including point-only queries.
                    origin = GridPoint(ref state); from = GridPoint(ref state);
                    to = familyIteration % 3 == 0 ? origin : from;
                    if ((familyIteration & 1) == 0) from = origin;
                    bounds = UnitBounds;
                    break;
                case 3: // Closed faces and their two adjacent ULPs at varying scales.
                    inverse = Transform2D.Identity;
                    float extent = MathF.ScaleB(1f, Next(ref state, 20) - 10);
                    float edge = (familyIteration & 1) == 0 ? extent : -extent;
                    edge = Next(ref state, 3) switch { 0 => MathF.BitDecrement(edge), 1 => edge, _ => MathF.BitIncrement(edge) };
                    origin = new Vector2(-2f * extent, edge); from = new Vector2(2f * extent, edge); to = from;
                    if ((familyIteration & 2) != 0) { origin = new(origin.Y, origin.X); from = new(from.Y, from.X); to = from; }
                    bounds = new Rect2(-extent, -extent, 2f * extent, 2f * extent);
                    break;
                case 4: // Extreme yet finite input/transform products, no float world conversion.
                    inverse = (familyIteration & 1) == 0
                        ? new Transform2D(new Vector2(1e20f, 0f), new Vector2(0f, 1e-20f), Vector2.Zero)
                        : new Transform2D(new Vector2(0f, -1e-20f), new Vector2(1e20f, 0f), Vector2.Zero);
                    origin = ExtremePoint(ref state); from = ExtremePoint(ref state); to = ExtremePoint(ref state);
                    bounds = UnitBounds;
                    break;
                default: // Clear-path cases exercise all three edge misses and corner misses.
                    inverse = Transform2D.Identity;
                    origin = GridPoint(ref state); from = GridPoint(ref state); to = GridPoint(ref state);
                    bounds = new Rect2(100f + Next(ref state, 100), 100f + Next(ref state, 100),
                        Next(ref state, 40) + 1f, Next(ref state, 40) + 1f);
                    break;
            }
            var shapes = new HeadlampOccluder[index % 6 == 0 ? Next(ref state, 4) + 1 : 1];
            shapes[^1] = new HeadlampOccluder(inverse, bounds);
            for (int shape = 0; shape < shapes.Length - 1; shape++)
                shapes[shape] = new HeadlampOccluder(Affines[Next(ref state, Affines.Length)],
                    new Rect2(GridPoint(ref state), new Vector2(Next(ref state, 8) + 1f, Next(ref state, 8) + 1f)));
            bool expected = LegacyCanSeeSegment(shapes, origin, from, to);
            bool actual = new HeadlampOcclusionFrame(shapes, true).CanSeeSegment(origin, from, to);
            TestAssert.Equal(expected, actual,
                $"frozen legacy equivalence seed=0x{CorpusSeed:X8} case={index} family={index % 6} origin={origin} from={from} to={to}");
            if (actual) visible++; else blocked++;
        }
        TestAssert.True(visible >= 1000 && blocked >= 1000,
            $"stratified equivalence corpus must exercise both outcomes; visible={visible}, blocked={blocked}");
    }

    private static HeadlampOccluder Shape(Rect2 bounds) => new(Transform2D.Identity, bounds);

    private static void AssertKnown(bool expected, HeadlampOccluder[] shapes, Vector2 origin, Vector2 from, Vector2 to, string name)
    {
        var frame = new HeadlampOcclusionFrame(shapes, true);
        TestAssert.Equal(expected, frame.CanSeeSegment(origin, from, to), name);
        TestAssert.Equal(expected, frame.CanSeeSegment(origin, to, from), name + " (reversed winding)");
        TestAssert.Equal(expected, LegacyCanSeeSegment(shapes, origin, from, to), name + " (frozen reference)");
        TestAssert.Equal(expected, LegacyCanSeeSegment(shapes, origin, to, from), name + " (reversed frozen reference)");
    }

    private static int Next(ref uint state, int exclusiveMaximum)
    {
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        return (int)(state % (uint)exclusiveMaximum);
    }

    private static Vector2 GridPoint(ref uint state) => new((Next(ref state, 513) - 256) * .125f,
        (Next(ref state, 513) - 256) * .125f);

    private static Vector2 ExtremePoint(ref uint state) => new(ExtremeCoordinates[Next(ref state, ExtremeCoordinates.Length)],
        ExtremeCoordinates[Next(ref state, ExtremeCoordinates.Length)]);

    // Frozen from HeadlampOcclusionSampler.cs before the transform-reuse change.
    // Keep all arithmetic and branch ordering independent of production helpers.
    // The four-argument entry point is also the same-process benchmark baseline.
    internal static bool LegacyCanSeeSegment(IReadOnlyList<HeadlampOccluder> shapes, Vector2 origin, Vector2 from, Vector2 to) =>
        LegacyCanSeeSegment(shapes, origin, from, to, true);

    private static bool LegacyCanSeeSegment(IReadOnlyList<HeadlampOccluder> shapes, Vector2 origin, Vector2 from, Vector2 to, bool complete)
    {
        if (!complete || !LegacyFinite(origin) || !LegacyFinite(from) || !LegacyFinite(to)) return false;
        foreach (HeadlampOccluder shape in shapes)
            if (LegacyIntersectsWedge(shape, origin, from, to)) return false;
        return true;
    }

    private static bool LegacyFinite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);

    private static bool LegacyIntersectsWedge(HeadlampOccluder shape, Vector2 origin, Vector2 from, Vector2 to)
    {
        if (LegacyIntersects(shape, origin, from) || LegacyIntersects(shape, origin, to) || LegacyIntersects(shape, from, to)) return true;
        var a = LegacyLocal(shape.WorldToLocal, origin); var b = LegacyLocal(shape.WorldToLocal, from); var c = LegacyLocal(shape.WorldToLocal, to);
        double area = LegacyCross(a, b, c);
        if (area == 0d) return false;
        Rect2 rect = shape.LocalBounds;
        return LegacyInside((rect.Position.X, rect.Position.Y), a, b, c, area) ||
            LegacyInside((rect.End.X, rect.Position.Y), a, b, c, area) ||
            LegacyInside((rect.End.X, rect.End.Y), a, b, c, area) ||
            LegacyInside((rect.Position.X, rect.End.Y), a, b, c, area);
    }

    private static (double X, double Y) LegacyLocal(Transform2D inverse, Vector2 point) =>
        ((double)inverse.X.X * point.X + (double)inverse.Y.X * point.Y + inverse.Origin.X,
         (double)inverse.X.Y * point.X + (double)inverse.Y.Y * point.Y + inverse.Origin.Y);

    private static double LegacyCross((double X, double Y) a, (double X, double Y) b, (double X, double Y) c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool LegacyInside((double X, double Y) point, (double X, double Y) a,
        (double X, double Y) b, (double X, double Y) c, double area) => area > 0d
        ? LegacyCross(a, b, point) >= 0d && LegacyCross(b, c, point) >= 0d && LegacyCross(c, a, point) >= 0d
        : LegacyCross(a, b, point) <= 0d && LegacyCross(b, c, point) <= 0d && LegacyCross(c, a, point) <= 0d;

    private static bool LegacyIntersects(HeadlampOccluder shape, Vector2 origin, Vector2 point)
    {
        Transform2D inverse = shape.WorldToLocal;
        double x0 = (double)inverse.X.X * origin.X + (double)inverse.Y.X * origin.Y + inverse.Origin.X;
        double y0 = (double)inverse.X.Y * origin.X + (double)inverse.Y.Y * origin.Y + inverse.Origin.Y;
        double x1 = (double)inverse.X.X * point.X + (double)inverse.Y.X * point.Y + inverse.Origin.X;
        double y1 = (double)inverse.X.Y * point.X + (double)inverse.Y.Y * point.Y + inverse.Origin.Y;
        double enter = 0d, exit = 1d;
        Rect2 rect = shape.LocalBounds;
        return LegacyClipAxis(x0, x1 - x0, rect.Position.X, rect.End.X, ref enter, ref exit) &&
            LegacyClipAxis(y0, y1 - y0, rect.Position.Y, rect.End.Y, ref enter, ref exit);
    }

    private static bool LegacyClipAxis(double origin, double direction, double minimum, double maximum,
        ref double enter, ref double exit)
    {
        if (direction == 0d) return origin >= minimum && origin <= maximum;
        double first = (minimum - origin) / direction, last = (maximum - origin) / direction;
        if (first > last) (first, last) = (last, first);
        enter = Math.Max(enter, first);
        exit = Math.Min(exit, last);
        return enter <= exit;
    }
}
