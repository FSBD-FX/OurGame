using System;
using Godot;
using Knight.Presentation.Fog;

namespace Knight.Tests.Unit;

public static class HeadlampPatchEquivalenceTests
{
    internal const string ReferenceSourceSha256 = "58C99A90BA64C48B2BEE06054646770F5B968594120E26B35C8021F92E71A667";
    private const uint CorpusSeed = 0xBB67AE85;
    private const int CorpusPairs = 5000;
    private static readonly Rect2 SurfaceBounds = new(-8f, 0f, 16f, 8f);
    private static readonly Vector2 TopOrigin = new(0f, -10f);
    private static readonly Vector2 TopFrom = new(-4f, 0f);
    private static readonly Vector2 TopTo = new(4f, 0f);
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

    public static void Run()
    {
        ContainedPatchAndEachOcclusionRegionHaveLiteralOutcomes();
        ExactSnapshotRowIsTheOnlyTerminalException();
        SurfaceAndBodyFaceKeepTheirDifferentAdmissionRules();
        BodyFacesRespectAllFourSidesAndAffineTransforms();
        SurfacePatchRetainsItsWorldDownExtrusion();
        FloatEndAndLegacyConservativeContactArePreserved();
        NonfiniteAndIncompleteInputsFailClosed();
        DeterministicCorpusMatchesFrozenCurrentWedgeReference();
    }

    // Catches testing only the top wedge, losing the final closing edge, a stale
    // corner reused between triangles, or replacing containment by endpoint rays.
    private static void ContainedPatchAndEachOcclusionRegionHaveLiteralOutcomes()
    {
        HeadlampOccluder surface = Surface(Transform2D.Identity);
        AssertBothTop(true, new([surface], true), surface, TopOrigin, TopFrom, TopTo, 2f,
            "the entire two-unit patch is contained in its exact terminal surface");
        var regions = new (string Name, Rect2 Bounds)[]
        {
            ("first radial edge", new(-2.125f, -5.125f, .25f, .25f)),
            ("first triangle interior", new(-.125f, -5f, .25f, .25f)),
            ("second triangle right strip", new(3.95f, .875f, .025f, .25f)),
            ("third triangle thin lower interior", new(-.0001f, .875f, .0002f, .25f)),
            ("fourth triangle left strip", new(-3.975f, .875f, .025f, .25f))
        };
        foreach (var item in regions)
        {
            HeadlampOccluder blocker = Blocker(item.Bounds);
            if (item.Bounds.Position.Y > 0f)
            {
                var blockerOnly = new HeadlampOcclusionFrame([blocker], true);
                TestAssert.Equal(true, blockerOnly.CanSeeSegment(TopOrigin, TopFrom, TopTo),
                    item.Name + ": the top triangle alone is genuinely clear");
            }
            AssertBothTop(false, new([surface, blocker], true), surface, TopOrigin, TopFrom, TopTo, 2f,
                item.Name + " blocks the full drawn patch");
            AssertBothTop(false, new([Blocker(new Rect2(40f, 40f, 2f, 2f)), blocker, surface], true),
                surface, TopOrigin, TopFrom, TopTo, 2f, item.Name + " remains blocking after a clear row");
        }
        AssertBothTop(false, new([surface, Blocker(new Rect2(-.5f, 2f, 1f, 1f))], true),
            surface, TopOrigin, TopFrom, TopTo, 2f, "another row touching the closed bottom edge blocks");
        AssertBothTop(true, new([surface, Blocker(new Rect2(-.5f, MathF.BitIncrement(2f), 1f, 1f))], true),
            surface, TopOrigin, TopFrom, TopTo, 2f, "one outward ULP below the bottom edge is clear");
    }

    // Catches excluding an entire body/owner rather than the exact record, or
    // admitting a visually identical but absent surface argument.
    private static void ExactSnapshotRowIsTheOnlyTerminalException()
    {
        HeadlampOccluder surface = Surface(Transform2D.Identity);
        foreach (HeadlampOccluder other in new[]
        {
            surface with { BodyInstanceId = 102 },
            surface with { OwnerId = 8 },
            surface with { ShapeIndex = 3 }
        })
            AssertBothTop(false, new([surface, other], true), surface, TopOrigin, TopFrom, TopTo, 2f,
                "another snapshot row occupying the same slab is not terminal contact");
        AssertBothTop(true, new([surface, surface], true), surface, TopOrigin, TopFrom, TopTo, 2f,
            "exact duplicate records retain the legacy exact-equality exception");
        AssertBothTop(false, new([], true), surface, TopOrigin, TopFrom, TopTo, 2f,
            "a terminal surface must belong to the snapshot");
        AssertBothTop(false, new([surface with { OwnerId = 8 }], true), surface, TopOrigin, TopFrom, TopTo, 2f,
            "geometry and body identity alone cannot establish membership");
        HeadlampOccluder anonymous = surface with { BodyInstanceId = 0 };
        AssertBothTop(false, new([anonymous], true), anonymous, TopOrigin, TopFrom, TopTo, 2f,
            "an anonymous surface cannot authorize terminal contact");
    }

    // These API differences are existing behavior, not a proposed redesign.
    // Catches broadening/narrowing validation during the shared geometry refactor.
    private static void SurfaceAndBodyFaceKeepTheirDifferentAdmissionRules()
    {
        HeadlampOccluder surface = Surface(Transform2D.Identity);
        var frame = new HeadlampOcclusionFrame([surface], true);
        AssertSurface(true, frame, surface, TopOrigin, Vector2.Zero, Vector2.Zero, 2f,
            "surface API preserves its admitted zero-width patch");
        AssertBody(false, frame, surface, TopOrigin, Vector2.Zero, Vector2.Zero, 2f,
            "body-face API rejects coincident endpoints");
        AssertSurface(true, frame, surface, TopOrigin, new(-4f, -.0005f), new(4f, -.0005f), 2f,
            "surface top contact allows its existing small outward tolerance");
        AssertBody(false, frame, surface, TopOrigin, new(-4f, -.0005f), new(4f, -.0005f), 2f,
            "body-face contact requires the exact local face");
        AssertSurface(false, frame, surface, TopOrigin, new(-4f, -.002f), new(4f, -.002f), 2f,
            "outward surface displacement beyond the admitted tolerance is rejected");
        AssertBothTop(false, frame, surface, TopOrigin, new(-4f, float.Epsilon), new(4f, float.Epsilon), 2f,
            "even the first positive ULP puts the top edge inside the slab");
        AssertBothTop(false, frame, surface, new(0f, 0f), TopFrom, TopTo, 2f,
            "origin on the terminal face is not an outside observer");
        AssertBothTop(false, frame, surface, new(0f, 1f), TopFrom, TopTo, 2f,
            "origin inside the slab cannot authorize a face patch");
        AssertBothTop(false, frame, surface, TopOrigin, new(MathF.BitDecrement(-8f), 0f), TopTo, 2f,
            "one ULP outside the left extent is rejected");
        AssertBothTop(true, frame, surface, TopOrigin, new(-8f, 0f), new(8f, 0f), 8f,
            "closed local bounds permit the exact full-width full-depth patch");
        AssertBothTop(false, frame, surface, TopOrigin, TopFrom, TopTo, MathF.BitIncrement(8f),
            "a patch extending beyond the destination slab is rejected");
        HeadlampOccluder deep = surface with { LocalBounds = new Rect2(-8f, 0f, 16f, 16f) };
        var deepFrame = new HeadlampOcclusionFrame([deep], true);
        AssertSurface(true, deepFrame, deep, TopOrigin, TopFrom, TopTo, 9f,
            "surface API has no independent eight-unit thickness cap");
        AssertBody(false, deepFrame, deep, TopOrigin, TopFrom, TopTo, 9f,
            "body-face API retains its eight-unit thickness cap");
    }

    // Catches a world-up/world-AABB substitution, wrong inward sign, or a basis
    // row/column swap when the shared quad is transformed for an occluder.
    private static void BodyFacesRespectAllFourSidesAndAffineTransforms()
    {
        foreach (Transform2D inverse in Affines)
        for (int face = 0; face < 4; face++)
        {
            HeadlampOccluder surface = Surface(inverse);
            Face(face, out Vector2 actor, out Vector2 from, out Vector2 to);
            Vector2 origin = inverse.AffineInverse() * actor;
            AssertBody(true, new([surface], true), surface, origin, from, to, 2f,
                $"affine exact local face {face} remains visible");
            Rect2 localBlock = face switch
            {
                0 => new Rect2(-.125f, 1f, .25f, .25f),
                1 => new Rect2(-7f, 3.875f, .25f, .25f),
                2 => new Rect2(6.75f, 3.875f, .25f, .25f),
                _ => new Rect2(-.125f, 6.75f, .25f, .25f)
            };
            var blocker = new HeadlampOccluder(inverse, localBlock, 202, 5, 0);
            AssertBody(false, new([surface, blocker], true), surface, origin, from, to, 2f,
                $"affine local face {face} has a blocker strictly inside its patch");
        }
    }

    // Surface patches extrude in world Down, unlike body-face patches. Preserve
    // even the 90-degree case whose local depth becomes zero, rather than silently
    // converting this public API to a different local-space extrusion rule.
    private static void SurfacePatchRetainsItsWorldDownExtrusion()
    {
        var cases = new (Transform2D Inverse, bool Visible)[]
        {
            (Affines[0], true), (Affines[1], true), (Affines[2], true), (Affines[3], true),
            (new Transform2D(new Vector2(1f, 0f), new Vector2(0f, -1f), Vector2.Zero), false)
        };
        foreach (var item in cases)
        {
            HeadlampOccluder surface = Surface(item.Inverse);
            Transform2D forward = item.Inverse.AffineInverse();
            AssertSurface(item.Visible, new([surface], true), surface, forward * TopOrigin,
                forward * TopFrom, forward * TopTo, 2f, "surface world-down extrusion retains affine containment behavior");
        }
    }

    // Catches recomputing Rect.End in double and introducing a same-side reject
    // that disagrees with the old closed double slab under cancellation.
    private static void FloatEndAndLegacyConservativeContactArePreserved()
    {
        HeadlampOccluder surface = Surface(Transform2D.Identity) with { LocalBounds = new Rect2(0f, 0f, 8f, 8f) };
        // float(16777216 + 1) is 16777216. The frozen rectangle is therefore
        // the line local x=16777216 (world x=0), not a double-width interval
        // ending at world x=2. The whole patch stays at world x >= 2.
        var roundedEndBlocker = new HeadlampOccluder(
            new Transform2D(new Vector2(.5f, 0f), new Vector2(0f, 1f), new Vector2(16777216f, 0f)),
            new Rect2(16777216f, .5f, 1f, 1f), 203, 1, 0);
        AssertBothTop(true, new([surface, roundedEndBlocker], true), surface,
            new(4f, -10f), new(2f, 0f), new(6f, 0f), 2f,
            "all four triangles use the existing float-rounded Rect.End");

        surface = Surface(Transform2D.Identity);
        var cancellationBlocker = Blocker(new Rect2(-1f, -2f, 1f, 4f));
        // All X values are positive, but dx rounds to -x0, so both X slab
        // crossings round to t=1. Y=0 is inside [-2,2]: literal false is the
        // frozen conservative result, not an exact-geometric intersection claim.
        AssertBothTop(false, new([surface, cancellationBlocker], true), surface,
            new(1e30f, -10f), new(1e-20f, 0f), new(2e-20f, 0f), 2f,
            "legacy conservative cancellation contact is not removed by quad reuse");
    }

    // Catches moving completeness/finite/identity checks after an all-clear return.
    private static void NonfiniteAndIncompleteInputsFailClosed()
    {
        HeadlampOccluder surface = Surface(Transform2D.Identity);
        AssertBothTop(false, new([surface], false), surface, TopOrigin, TopFrom, TopTo, 2f,
            "incomplete snapshots fail closed even with a matching terminal row");
        var frame = new HeadlampOcclusionFrame([surface], true);
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            AssertBothTop(false, frame, surface, new(invalid, -10f), TopFrom, TopTo, 2f, "nonfinite origin");
            AssertBothTop(false, frame, surface, TopOrigin, new(invalid, 0f), TopTo, 2f, "nonfinite first endpoint");
            AssertBothTop(false, frame, surface, TopOrigin, TopFrom, new(4f, invalid), 2f, "nonfinite second endpoint");
            AssertBothTop(false, frame, surface, TopOrigin, TopFrom, TopTo, invalid, "nonfinite thickness");
        }
        foreach (float thickness in new[] { 0f, -1f })
            AssertBothTop(false, frame, surface, TopOrigin, TopFrom, TopTo, thickness, "nonpositive thickness");
        HeadlampOccluder singular = surface with
        { WorldToLocal = new Transform2D(new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.Zero) };
        AssertBody(false, new([singular], true), singular, TopOrigin, TopFrom, TopTo, 2f,
            "body-face validation rejects a singular inverse");
        HeadlampOccluder emptyBounds = surface with { LocalBounds = new Rect2(-8f, 0f, 0f, 8f) };
        AssertBody(false, new([emptyBounds], true), emptyBounds, TopOrigin, TopFrom, TopTo, 2f,
            "body-face validation rejects a zero-size rectangle");
    }

    // 5,000 paired scenarios = 10,000 real API comparisons. This is deliberately
    // a frozen-behavior oracle; the independent literals above are not derived
    // from these helpers. No capture internals or production geometry are called.
    private static void DeterministicCorpusMatchesFrozenCurrentWedgeReference()
    {
        uint state = CorpusSeed;
        int surfaceVisible = 0, bodyVisible = 0;
        for (int index = 0; index < CorpusPairs; index++)
        {
            int family = index % 8, iteration = index / 8;
            Transform2D inverse = family == 5 ? Affines[Next(ref state, Affines.Length)] : Transform2D.Identity;
            HeadlampOccluder surface = Surface(inverse);
            int face = family is 2 or 6 or 7 ? 0 : Next(ref state, 4);
            Face(face, out Vector2 bodyActor, out Vector2 bodyFrom, out Vector2 bodyTo);
            Transform2D forward = inverse.AffineInverse();
            Vector2 surfaceOrigin = forward * TopOrigin, surfaceFrom = forward * TopFrom, surfaceTo = forward * TopTo;
            Vector2 bodyOrigin = forward * bodyActor;
            float thickness = (Next(ref state, 16) + 1) * .25f;
            bool complete = true;
            HeadlampOccluder[] shapes;
            switch (family)
            {
                case 0:
                    shapes = [surface, Blocker(new Rect2(40f + Next(ref state, 80), 40f, 2f, 2f)),
                        new(Affines[Next(ref state, Affines.Length)], new Rect2(200f, 200f, 4f, 8f), 301, 1, 0)];
                    break;
                case 1:
                    shapes = [surface, new(Affines[Next(ref state, Affines.Length)],
                        new Rect2(GridPoint(ref state), new Vector2((Next(ref state, 32) + 1) * .125f,
                            (Next(ref state, 32) + 1) * .125f)), 302, 2, 0)];
                    break;
                case 2:
                    float width = MathF.ScaleB(1f, -Next(ref state, 15) - 1);
                    shapes = [surface, Blocker(new Rect2(-width * .5f, thickness * .5f, width, thickness * .125f))];
                    break;
                case 3:
                    shapes = [surface];
                    switch (iteration % 8)
                    {
                        case 0: complete = false; break;
                        case 1: shapes = []; break;
                        case 2: surface = surface with { BodyInstanceId = 0 }; shapes = [surface]; break;
                        case 3: surfaceTo = surfaceFrom; bodyTo = bodyFrom; break;
                        case 4: thickness = 0f; break;
                        case 5: thickness = 9f; break;
                        case 6: surfaceFrom.Y = float.Epsilon; bodyFrom += new Vector2(float.Epsilon, float.Epsilon); break;
                        default: thickness = float.NaN; break;
                    }
                    break;
                case 4:
                    HeadlampOccluder other = (iteration % 4) switch
                    {
                        0 => surface, 1 => surface with { OwnerId = 9 },
                        2 => surface with { ShapeIndex = 9 }, _ => surface with { BodyInstanceId = 999 }
                    };
                    shapes = [Blocker(new Rect2(40f, 40f, 2f, 2f)), surface, other];
                    break;
                case 5:
                    shapes = [surface, new(inverse, new Rect2(40f, 40f, 2f, 2f), 303, 0, 0)];
                    break;
                case 6:
                    float y = (iteration & 1) == 0 ? thickness : MathF.BitIncrement(thickness);
                    shapes = [surface, Blocker(new Rect2(-.5f, y, 1f, .25f))];
                    break;
                default:
                    Transform2D extreme = (iteration % 3) switch
                    {
                        0 => new(new Vector2(1e20f, 0f), new Vector2(0f, 1e-20f), Vector2.Zero),
                        1 => new(new Vector2(0f, -1e-20f), new Vector2(1e20f, 0f), Vector2.Zero),
                        _ => new(new Vector2(1f, .5f), new Vector2(.25f, 1f), new Vector2(1e30f, -1e30f))
                    };
                    if ((iteration & 1) == 0) { surfaceOrigin.X = 1e30f; bodyOrigin.X = 1e30f; }
                    shapes = [surface, new(extreme, new Rect2(-1f, -1f, 2f, 2f), 304, 0, 0)];
                    break;
            }
            var frame = new HeadlampOcclusionFrame(shapes, complete);
            bool expectedSurface = LegacyCanSeeSurfacePatch(frame, surfaceOrigin, surfaceFrom, surfaceTo, thickness, surface);
            bool actualSurface = frame.CanSeeSurfacePatch(surfaceOrigin, surfaceFrom, surfaceTo, thickness, surface);
            TestAssert.Equal(expectedSurface, actualSurface, $"surface frozen-reference seed=0x{CorpusSeed:X8} case={index} family={family}");
            bool expectedBody = LegacyCanSeeBodyFacePatch(frame, bodyOrigin, surface, bodyFrom, bodyTo, thickness);
            bool actualBody = frame.CanSeeBodyFacePatch(bodyOrigin, surface, bodyFrom, bodyTo, thickness);
            TestAssert.Equal(expectedBody, actualBody, $"body-face frozen-reference seed=0x{CorpusSeed:X8} case={index} family={family} face={face}");
            if (actualSurface) surfaceVisible++;
            if (actualBody) bodyVisible++;
        }
        TestAssert.True(surfaceVisible >= 500 && surfaceVisible <= CorpusPairs - 500,
            $"surface corpus must exercise both outcomes; visible={surfaceVisible}, total={CorpusPairs}");
        TestAssert.True(bodyVisible >= 500 && bodyVisible <= CorpusPairs - 500,
            $"body-face corpus must exercise both outcomes; visible={bodyVisible}, total={CorpusPairs}");
    }

    private static HeadlampOccluder Surface(Transform2D inverse) => new(inverse, SurfaceBounds, 101, 7, 2);
    private static HeadlampOccluder Blocker(Rect2 bounds) => new(Transform2D.Identity, bounds, 201, 3, 0);
    private static Vector2 GridPoint(ref uint state) => new((Next(ref state, 257) - 128) * .125f, (Next(ref state, 257) - 128) * .125f);
    private static int Next(ref uint state, int maximum)
    {
        state ^= state << 13; state ^= state >> 17; state ^= state << 5;
        return (int)(state % (uint)maximum);
    }

    private static void Face(int face, out Vector2 actor, out Vector2 from, out Vector2 to)
    {
        switch (face)
        {
            case 0: actor = TopOrigin; from = TopFrom; to = TopTo; break;
            case 1: actor = new(-12f, 4f); from = new(-8f, 2f); to = new(-8f, 6f); break;
            case 2: actor = new(12f, 4f); from = new(8f, 2f); to = new(8f, 6f); break;
            default: actor = new(0f, 12f); from = new(-4f, 8f); to = new(4f, 8f); break;
        }
    }

    private static void AssertBothTop(bool expected, HeadlampOcclusionFrame frame, HeadlampOccluder surface,
        Vector2 origin, Vector2 from, Vector2 to, float thickness, string name)
    {
        AssertSurface(expected, frame, surface, origin, from, to, thickness, name);
        AssertBody(expected, frame, surface, origin, from, to, thickness, name);
    }

    private static void AssertSurface(bool expected, HeadlampOcclusionFrame frame, HeadlampOccluder surface,
        Vector2 origin, Vector2 from, Vector2 to, float thickness, string name)
    {
        TestAssert.Equal(expected, frame.CanSeeSurfacePatch(origin, from, to, thickness, surface), name + " (surface)");
        TestAssert.Equal(expected, frame.CanSeeSurfacePatch(origin, to, from, thickness, surface), name + " (surface reversed)");
        TestAssert.Equal(expected, LegacyCanSeeSurfacePatch(frame, origin, from, to, thickness, surface), name + " (surface reference)");
        TestAssert.Equal(expected, LegacyCanSeeSurfacePatch(frame, origin, to, from, thickness, surface), name + " (surface reversed reference)");
    }

    private static void AssertBody(bool expected, HeadlampOcclusionFrame frame, HeadlampOccluder surface,
        Vector2 origin, Vector2 from, Vector2 to, float thickness, string name)
    {
        TestAssert.Equal(expected, frame.CanSeeBodyFacePatch(origin, surface, from, to, thickness), name + " (body-face)");
        TestAssert.Equal(expected, frame.CanSeeBodyFacePatch(origin, surface, to, from, thickness), name + " (body-face reversed)");
        TestAssert.Equal(expected, LegacyCanSeeBodyFacePatch(frame, origin, surface, from, to, thickness), name + " (body-face reference)");
        TestAssert.Equal(expected, LegacyCanSeeBodyFacePatch(frame, origin, surface, to, from, thickness), name + " (body-face reversed reference)");
    }

    // Frozen from the SHA above: both public admission paths plus the CURRENT
    // three-transform wedge, not the earlier nine-transform benchmark reference.
    // Keep four wedge calls and the original arithmetic/branch ordering here.
    internal static bool LegacyCanSeeSurfacePatch(HeadlampOcclusionFrame frame, Vector2 origin, Vector2 from, Vector2 to,
        float thickness, HeadlampOccluder surface)
    {
        if (!frame.IsComplete || surface.BodyInstanceId == 0 || !Finite(origin) ||
            !Finite(from) || !Finite(to) || !float.IsFinite(thickness) || thickness <= 0f) return false;
        bool member = false;
        foreach (HeadlampOccluder shape in frame.Shapes) if (shape == surface) { member = true; break; }
        if (!member) return false;
        Vector2 bottomFrom = from + Vector2.Down * thickness, bottomTo = to + Vector2.Down * thickness;
        if (!Finite(bottomFrom) || !Finite(bottomTo)) return false;
        var actor = Local(surface.WorldToLocal, origin);
        var a = Local(surface.WorldToLocal, from); var b = Local(surface.WorldToLocal, to);
        var c = Local(surface.WorldToLocal, bottomFrom); var d = Local(surface.WorldToLocal, bottomTo);
        Rect2 rect = surface.LocalBounds;
        if (actor.Y >= rect.Position.Y || a.Y > rect.Position.Y || b.Y > rect.Position.Y ||
            a.Y < rect.Position.Y - .001d || b.Y < rect.Position.Y - .001d ||
            a.X < rect.Position.X || b.X < rect.Position.X || a.X > rect.End.X || b.X > rect.End.X ||
            c.X < rect.Position.X || d.X < rect.Position.X || c.X > rect.End.X || d.X > rect.End.X ||
            c.Y < rect.Position.Y || d.Y < rect.Position.Y || c.Y > rect.End.Y || d.Y > rect.End.Y) return false;
        foreach (HeadlampOccluder shape in frame.Shapes)
        {
            if (shape == surface) continue;
            if (IntersectsWedge(shape, origin, from, to) ||
                IntersectsWedge(shape, origin, to, bottomTo) ||
                IntersectsWedge(shape, origin, bottomTo, bottomFrom) ||
                IntersectsWedge(shape, origin, bottomFrom, from)) return false;
        }
        return true;
    }

    internal static bool LegacyCanSeeBodyFacePatch(HeadlampOcclusionFrame frame, Vector2 origin, HeadlampOccluder surface,
        Vector2 fromLocal, Vector2 toLocal, float thickness)
    {
        Rect2 rect = surface.LocalBounds;
        Transform2D inverse = surface.WorldToLocal;
        if (!frame.IsComplete || surface.BodyInstanceId == 0 || !Finite(origin) ||
            !Finite(fromLocal) || !Finite(toLocal) || fromLocal == toLocal ||
            !float.IsFinite(thickness) || thickness <= 0f || thickness > 8f ||
            !Finite(rect.Position) || !Finite(rect.Size) || !Finite(rect.End) || rect.Size.X <= 0f || rect.Size.Y <= 0f ||
            !Finite(inverse.X) || !Finite(inverse.Y) || !Finite(inverse.Origin) ||
            !float.IsFinite(inverse.Determinant()) || inverse.Determinant() == 0f) return false;
        bool member = false;
        foreach (HeadlampOccluder shape in frame.Shapes) if (shape == surface) { member = true; break; }
        if (!member) return false;
        var actor = Local(inverse, origin);
        Vector2 inward;
        if (fromLocal.X == rect.Position.X && toLocal.X == rect.Position.X && actor.X < rect.Position.X)
            inward = Vector2.Right * thickness;
        else if (fromLocal.X == rect.End.X && toLocal.X == rect.End.X && actor.X > rect.End.X)
            inward = Vector2.Left * thickness;
        else if (fromLocal.Y == rect.Position.Y && toLocal.Y == rect.Position.Y && actor.Y < rect.Position.Y)
            inward = Vector2.Down * thickness;
        else if (fromLocal.Y == rect.End.Y && toLocal.Y == rect.End.Y && actor.Y > rect.End.Y)
            inward = Vector2.Up * thickness;
        else return false;
        Vector2[] local = [fromLocal, toLocal, toLocal + inward, fromLocal + inward];
        Transform2D forward = inverse.AffineInverse();
        if (!Finite(forward.X) || !Finite(forward.Y) || !Finite(forward.Origin)) return false;
        Vector2[] world = new Vector2[4];
        for (int index = 0; index < 4; index++)
        {
            Vector2 point = local[index];
            if (!Finite(point) || point.X < rect.Position.X || point.X > rect.End.X ||
                point.Y < rect.Position.Y || point.Y > rect.End.Y) return false;
            world[index] = forward * point;
            if (!Finite(world[index])) return false;
        }
        foreach (HeadlampOccluder shape in frame.Shapes)
        {
            if (shape == surface) continue;
            for (int index = 0; index < 4; index++)
                if (IntersectsWedge(shape, origin, world[index], world[(index + 1) % 4])) return false;
        }
        return true;
    }

    private static bool IntersectsWedge(HeadlampOccluder shape, Vector2 origin, Vector2 from, Vector2 to)
    {
        var a = Local(shape.WorldToLocal, origin); var b = Local(shape.WorldToLocal, from);
        Rect2 rect = shape.LocalBounds;
        Vector2 minimum = rect.Position, maximum = rect.End;
        if (IntersectsLocal(minimum, maximum, a, b)) return true;
        var c = Local(shape.WorldToLocal, to);
        if (IntersectsLocal(minimum, maximum, a, c) || IntersectsLocal(minimum, maximum, b, c)) return true;
        double area = Cross(a, b, c);
        if (area == 0d) return false;
        return Inside((minimum.X, minimum.Y), a, b, c, area) ||
            Inside((maximum.X, minimum.Y), a, b, c, area) ||
            Inside((maximum.X, maximum.Y), a, b, c, area) ||
            Inside((minimum.X, maximum.Y), a, b, c, area);
    }

    private static bool IntersectsLocal(Vector2 minimum, Vector2 maximum, (double X, double Y) from, (double X, double Y) to)
    {
        double enter = 0d, exit = 1d;
        return ClipAxis(from.X, to.X - from.X, minimum.X, maximum.X, ref enter, ref exit) &&
            ClipAxis(from.Y, to.Y - from.Y, minimum.Y, maximum.Y, ref enter, ref exit);
    }

    private static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);
    private static (double X, double Y) Local(Transform2D inverse, Vector2 point) =>
        ((double)inverse.X.X * point.X + (double)inverse.Y.X * point.Y + inverse.Origin.X,
         (double)inverse.X.Y * point.X + (double)inverse.Y.Y * point.Y + inverse.Origin.Y);
    private static double Cross((double X, double Y) a, (double X, double Y) b, (double X, double Y) c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    private static bool Inside((double X, double Y) point, (double X, double Y) a,
        (double X, double Y) b, (double X, double Y) c, double area) => area > 0d
        ? Cross(a, b, point) >= 0d && Cross(b, c, point) >= 0d && Cross(c, a, point) >= 0d
        : Cross(a, b, point) <= 0d && Cross(b, c, point) <= 0d && Cross(c, a, point) <= 0d;
    private static bool ClipAxis(double origin, double direction, double minimum, double maximum, ref double enter, ref double exit)
    {
        if (direction == 0d) return origin >= minimum && origin <= maximum;
        double first = (minimum - origin) / direction, last = (maximum - origin) / direction;
        if (first > last) (first, last) = (last, first);
        enter = Math.Max(enter, first);
        exit = Math.Min(exit, last);
        return enter <= exit;
    }
}
