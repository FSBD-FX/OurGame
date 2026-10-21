using System;
using System.Reflection;
using Godot;
using Knight.Presentation.Fog;

namespace Knight.Tests.Unit;

public static class HeadlampQuadEquivalenceTests
{
    internal delegate bool QuadQuery(HeadlampOcclusionFrame frame, Vector2 origin,
        Vector2 first, Vector2 second, Vector2 third, Vector2 fourth);
    private static QuadQuery _current;
    private const uint CorpusSeed = 0x3C6EF372;
    private const int CorpusCases = 10000;
    private static readonly Vector2 Origin = new(0f, -10f);
    private static readonly Vector2[] Quad = [new(-4f, 0f), new(4f, 0f), new(4f, 2f), new(-4f, 2f)];
    private static readonly Transform2D[] Affines =
    [
        Transform2D.Identity,
        new(new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(8f, -4f)),
        new(new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(-3f, 7f)),
        new(new Vector2(1f, 0f), new Vector2(.5f, 1f), new Vector2(2f, -6f)),
        new(new Vector2(1f, .5f), new Vector2(.5f, -1f), new Vector2(-8f, 4f)),
        new(new Vector2(.6f, .8f), new Vector2(-.8f, .6f), new Vector2(3f, -2f))
    ];
    private static readonly float[] Extremes =
    [0f, float.Epsilon, -float.Epsilon, 1e-30f, -1e-30f, 1e-20f, -1e-20f,
        1f, -1f, 1e20f, -1e20f, 1e30f, -1e30f, float.MaxValue, -float.MaxValue];

    public static void Run()
    {
        BindCurrent(); // API absence is an assertion RED, never a compile failure or fallback.
        EveryTriangleAndClosingEdgeKeepTheirCoverage();
        ClosedContactAndUlpsRemainDistinct();
        DegenerateAndTerminalLookingQuadsKeepLiteralOutcomes();
        AffineAndExtremeArithmeticRemainEquivalent();
        CompleteAndFiniteGatesApplyEvenWithoutShapes();
        DeterministicCorpusMatchesFourCurrentWedges();
    }

    internal static QuadQuery BindCurrent()
    {
        if (_current != null) return _current;
        MethodInfo method = typeof(HeadlampOcclusionFrame).GetMethod("CanSeeQuad", BindingFlags.Public | BindingFlags.Instance,
            null, [typeof(Vector2), typeof(Vector2), typeof(Vector2), typeof(Vector2), typeof(Vector2)], null);
        TestAssert.True(method != null && method.ReturnType == typeof(bool),
            "QuadApiMustExist: HeadlampOcclusionFrame must expose public bool CanSeeQuad(origin, first, second, third, fourth)");
        _current = method.CreateDelegate<QuadQuery>(); // Open instance delegate; reflected only once per process.
        return _current;
    }

    // Deliberately the CURRENT three-transform CanSeeSegment API four times.
    // No terminal exclusion and no earlier nine-transform test-only baseline.
    internal static bool ReferenceCanSeeQuad(HeadlampOcclusionFrame frame, Vector2 origin,
        Vector2 first, Vector2 second, Vector2 third, Vector2 fourth) =>
        frame.CanSeeSegment(origin, first, second) && frame.CanSeeSegment(origin, second, third) &&
        frame.CanSeeSegment(origin, third, fourth) && frame.CanSeeSegment(origin, fourth, first);

    // Catches top-only/endpoint-only authorization, a stale reused corner, and
    // omitting the fourth triangle. The three lower blockers miss the top wedge.
    private static void EveryTriangleAndClosingEdgeKeepTheirCoverage()
    {
        AssertKnown(true, [], Origin, Quad, "empty complete snapshot leaves the quad visible");
        var cases = new (string Name, Rect2 Bounds)[]
        {
            ("first radial edge", new(-2.125f, -5.125f, .25f, .25f)),
            ("first triangle interior", new(-.0001f, -5f, .0002f, .25f)),
            ("second triangle right strip", new(3.95f, .875f, .025f, .25f)),
            ("third triangle thin lower interior", new(-.0001f, .875f, .0002f, .25f)),
            ("fourth triangle left strip", new(-3.975f, .875f, .025f, .25f)),
            ("closed fourth-to-first edge contact", new(-4.25f, .75f, .25f, .5f))
        };
        foreach (var item in cases)
        {
            HeadlampOccluder blocker = Shape(item.Bounds);
            var frame = new HeadlampOcclusionFrame([blocker], true);
            if (item.Bounds.Position.Y > 0f)
                TestAssert.Equal(true, frame.CanSeeSegment(Origin, Quad[0], Quad[1]),
                    item.Name + ": top triangle is independently clear");
            AssertKnown(false, [blocker], Origin, Quad, item.Name);
            AssertKnown(false, [Shape(new Rect2(40f, 40f, 2f, 2f)), blocker], Origin, Quad,
                item.Name + " after an earlier clear shape");
        }
        var interior = new HeadlampOcclusionFrame([Shape(new Rect2(-.0001f, -5f, .0002f, .25f))], true);
        foreach (Vector2 vertex in Quad)
            TestAssert.True(interior.CanSee(Origin, vertex), "the thin interior blocker leaves all four vertex rays clear");
    }

    // Catches replacing closed slabs by half-open contact or expanding by epsilon.
    private static void ClosedContactAndUlpsRemainDistinct()
    {
        foreach (bool vertical in new[] { false, true })
        foreach (float side in new[] { -1f, 1f })
        {
            float outside = side < 0f ? MathF.BitDecrement(side) : MathF.BitIncrement(side);
            foreach (var item in new[] { (Coordinate: side, Visible: false), (Coordinate: outside, Visible: true) })
            {
                Vector2 actor = vertical ? new(item.Coordinate, -2f) : new(-2f, item.Coordinate);
                Vector2 end = vertical ? new(item.Coordinate, 2f) : new(2f, item.Coordinate);
                AssertKnown(item.Visible, [Shape(new Rect2(-1f, -1f, 2f, 2f))], actor, [end, end, end, end],
                    $"face contact/ULP vertical={vertical} side={side} coordinate={item.Coordinate}");
            }
        }
        AssertKnown(false, [Shape(new Rect2(-.5f, 2f, 1f, 1f))], Origin, Quad, "closed bottom quad edge contact");
        AssertKnown(true, [Shape(new Rect2(-.5f, MathF.BitIncrement(2f), 1f, 1f))], Origin, Quad,
            "one outward ULP below the bottom quad edge is clear");
    }

    // Catches newly rejecting zero area or importing the patch APIs' terminal-row
    // exemption into a general quad query. Its contract is four wedge queries.
    private static void DegenerateAndTerminalLookingQuadsKeepLiteralOutcomes()
    {
        HeadlampOccluder unit = Shape(new Rect2(-1f, -1f, 2f, 2f));
        AssertKnown(false, [unit], Vector2.Zero, [Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero], "point inside blocker");
        Vector2 outside = new(2f, 2f);
        AssertKnown(true, [unit], outside, [outside, outside, outside, outside], "point outside blocker");
        AssertKnown(false, [unit], new(-3f, 0f), [new(3f, 0f), new(-2f, 0f), new(2f, 0f), new(-2f, 0f)],
            "collinear backtracking quad still crosses the slab");
        AssertKnown(true, [unit], new(-3f, 2f), [new(3f, 2f), new(-2f, 2f), new(2f, 2f), new(-2f, 2f)],
            "collinear backtracking quad above the slab remains clear");
        AssertKnown(false, [new(Transform2D.Identity, new Rect2(-8f, 0f, 16f, 8f), 101, 7, 2)], Origin, Quad,
            "even a named containing body/owner/index has no terminal exception");
        AssertKnown(true, [], Origin, [Quad[0], Quad[2], Quad[1], Quad[3]], "crossed vertex order is not an extra admission failure");
    }

    // Catches wrong transform basis/origin, float products, and same-side rejection
    // that would remove an established conservative double-slab contact.
    private static void AffineAndExtremeArithmeticRemainEquivalent()
    {
        foreach (Transform2D inverse in Affines)
        {
            Transform2D forward = inverse.AffineInverse();
            Vector2[] world = [forward * Quad[0], forward * Quad[1], forward * Quad[2], forward * Quad[3]];
            AssertKnown(false, [new(inverse, new Rect2(-.125f, 1f, .25f, .25f))], forward * Origin, world,
                "rotated mirrored or sheared quad contains an interior blocker");
            AssertKnown(true, [new(inverse, new Rect2(20f, 20f, 2f, 2f))], forward * Origin, world,
                "rotated mirrored or sheared quad stays separate from a distant blocker");
        }
        Transform2D extremeScale = new(new Vector2(1e20f, 0f), new Vector2(0f, 1e-20f), Vector2.Zero);
        AssertKnown(false, [new(extremeScale, new Rect2(-1f, -1f, 2f, 2f))], new(-1e20f, 0f),
            [new(1e20f, -1e20f), new(1e20f, 1e20f), new(2e20f, 1e20f), new(2e20f, -1e20f)],
            "finite transform products of order 1e40 must stay double");
        // dx rounds to -origin.X; both X slab crossings become t=1, where
        // y=0 is inside [-2,2]. All world Xs remain strictly positive: this
        // literal is the preserved conservative result, not an exact-geometry claim.
        AssertKnown(false, [Shape(new Rect2(-1f, -2f, 1f, 4f))], new(1e30f, -10f),
            [new(1e-20f, 0f), new(2e-20f, 0f), new(2e-20f, 2f), new(1e-20f, 2f)], "legacy cancellation contact");
        float tiny = float.Epsilon;
        AssertKnown(false, [Shape(new Rect2(-tiny, -tiny, 2f * tiny, 2f * tiny))], new(-4f * tiny, 0f),
            [new(4f * tiny, -2f * tiny), new(4f * tiny, 2f * tiny), new(5f * tiny, 2f * tiny), new(5f * tiny, -2f * tiny)],
            "subnormal finite quad crosses a positive-size rectangle");
    }

    private static void CompleteAndFiniteGatesApplyEvenWithoutShapes()
    {
        AssertKnown(false, [], Origin, Quad, "incomplete empty snapshot fails closed", complete: false);
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        foreach (bool yAxis in new[] { false, true })
        for (int point = 0; point < 5; point++)
        {
            Vector2 bad = yAxis ? new(0f, invalid) : new(invalid, 0f);
            var vertices = (Vector2[])Quad.Clone();
            Vector2 origin = point == 0 ? bad : Origin;
            if (point > 0) vertices[point - 1] = bad;
            AssertKnown(false, [], origin, vertices, $"nonfinite input point={point} yAxis={yAxis} cannot pass an empty snapshot");
        }
    }

    // Differential characterization of the unchanged four-wedge behavior; the
    // literal tests above independently constrain intended visible/blocked cases.
    private static void DeterministicCorpusMatchesFourCurrentWedges()
    {
        uint state = CorpusSeed;
        int visible = 0, blocked = 0;
        for (int index = 0; index < CorpusCases; index++)
        {
            int family = index % 8, iteration = index / 8;
            Vector2 origin = GridPoint(ref state);
            Vector2[] vertices = [GridPoint(ref state), GridPoint(ref state), GridPoint(ref state), GridPoint(ref state)];
            Transform2D inverse = Affines[Next(ref state, Affines.Length)];
            Rect2 bounds = new(GridPoint(ref state), new Vector2((Next(ref state, 32) + 1) * .125f, (Next(ref state, 32) + 1) * .125f));
            switch (family)
            {
                case 1:
                    origin = Origin; vertices = (Vector2[])Quad.Clone(); inverse = Transform2D.Identity;
                    float width = MathF.ScaleB(1f, -Next(ref state, 15) - 1);
                    bounds = new Rect2(-width * .5f, .875f, width, .25f);
                    break;
                case 2:
                    vertices[1] = vertices[2] = vertices[3] = vertices[0];
                    if ((iteration & 1) == 0) origin = vertices[0];
                    bounds = new Rect2(-1f, -1f, 2f, 2f);
                    break;
                case 3:
                    inverse = Transform2D.Identity;
                    float side = (iteration & 1) == 0 ? 1f : -1f;
                    side = Next(ref state, 3) switch { 0 => MathF.BitDecrement(side), 1 => side, _ => MathF.BitIncrement(side) };
                    origin = new(-2f, side);
                    Vector2 end = new(2f, side);
                    if ((iteration & 2) != 0) { origin = new(origin.Y, origin.X); end = new(end.Y, end.X); }
                    vertices = [end, end, end, end]; bounds = new Rect2(-1f, -1f, 2f, 2f);
                    break;
                case 4:
                    Transform2D forward = inverse.AffineInverse();
                    origin = forward * Origin;
                    for (int vertex = 0; vertex < 4; vertex++) vertices[vertex] = forward * Quad[vertex];
                    bounds = (iteration & 1) == 0 ? new Rect2(-.125f, 1f, .25f, .25f) : new Rect2(20f, 20f, 2f, 2f);
                    break;
                case 5:
                    inverse = (iteration & 1) == 0 ? Transform2D.Identity
                        : new Transform2D(new Vector2(1e20f, 0f), new Vector2(0f, 1e-20f), Vector2.Zero);
                    origin = ExtremePoint(ref state);
                    for (int vertex = 0; vertex < 4; vertex++) vertices[vertex] = ExtremePoint(ref state);
                    bounds = new Rect2(-1f, -1f, 2f, 2f);
                    break;
                case 7:
                    inverse = Transform2D.Identity; bounds = new Rect2(100f + Next(ref state, 100), 100f, 4f, 4f);
                    break;
            }
            HeadlampOccluder[] shapes = family == 6
                ? [Shape(new Rect2(-40f, -40f, 2f, 2f)), new(inverse, bounds, 101, 7, 2), Shape(new Rect2(40f, 40f, 2f, 2f))]
                : [new(inverse, bounds)];
            var frame = new HeadlampOcclusionFrame(shapes, true);
            bool expected = ReferenceCanSeeQuad(frame, origin, vertices[0], vertices[1], vertices[2], vertices[3]);
            bool actual = _current(frame, origin, vertices[0], vertices[1], vertices[2], vertices[3]);
            TestAssert.Equal(expected, actual, $"quad four-current-wedge equivalence seed=0x{CorpusSeed:X8} case={index} family={family}");
            if (actual) visible++; else blocked++;
        }
        TestAssert.True(visible >= 1000 && blocked >= 1000,
            $"quad corpus exercises both outcomes; visible={visible}, blocked={blocked}");
    }

    private static void AssertKnown(bool expected, HeadlampOccluder[] shapes, Vector2 origin, Vector2[] vertices,
        string name, bool complete = true)
    {
        var frame = new HeadlampOcclusionFrame(shapes, complete);
        TestAssert.Equal(expected, _current(frame, origin, vertices[0], vertices[1], vertices[2], vertices[3]), name);
        TestAssert.Equal(expected, ReferenceCanSeeQuad(frame, origin, vertices[0], vertices[1], vertices[2], vertices[3]), name + " (four current wedges)");
        TestAssert.Equal(expected, _current(frame, origin, vertices[0], vertices[3], vertices[2], vertices[1]), name + " (reverse winding)");
    }

    private static HeadlampOccluder Shape(Rect2 bounds) => new(Transform2D.Identity, bounds);
    private static Vector2 GridPoint(ref uint state) => new((Next(ref state, 513) - 256) * .125f, (Next(ref state, 513) - 256) * .125f);
    private static Vector2 ExtremePoint(ref uint state) => new(Extremes[Next(ref state, Extremes.Length)], Extremes[Next(ref state, Extremes.Length)]);
    private static int Next(ref uint state, int maximum)
    {
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        return (int)(state % (uint)maximum);
    }
}
