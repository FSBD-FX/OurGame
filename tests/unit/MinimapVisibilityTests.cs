using System;
using Godot;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;

namespace Knight.Tests.Unit;

internal static class MinimapVisibilityTests
{
    internal static void Run()
    {
        OpenCellAccepted();
        ThinWallInsideCellRejected();
        BlockerBetweenCornerRaysRejected();
        AngledNonGridFrontAcceptedBackRejected();
        TargetInteriorNeverExcluded();
        NaNOrSingularRejected();
        Shape257Rejected();
        TransformedFreeCellOcclusion();
        EdgeCasesFailClosed();
        ActualFiniteSweepInstrumentation();
        GD.Print("MINIMAP_VISIBILITY_OK cases=10 physics_query_interface=0");
    }

    private static void ActualFiniteSweepInstrumentation()
    {
        var cell = new Rect2(16, -4, 8, 8);
        TestAssert.True(MinimapVisibility.CellAllowed(Vector2.Zero, cell,
            Frame(Shape(new Rect2(1000, 1000, 8, 8)), Shape(new Rect2(2000, 2000, 8, 8))), out int accepted) && accepted == 2,
            "finite sweep instrumentation counts all actually tested far occluders");
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, cell,
            Frame(Shape(new Rect2(8, -4, 1, 8)), Shape(new Rect2(2000, 2000, 8, 8))), out int rejected) && rejected == 1,
            "finite sweep instrumentation stops on the first blocking shape, not Shapes.Count arithmetic");
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(16, -4, 8, 8));
        var face = new MinimapBoundary(0, new(16, -3), new(16, 3), Vector2.Left);
        TestAssert.True(MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, face,
            Frame(Shape(solid), Shape(new Rect2(2000, 2000, 8, 8))), out int boundary) && boundary == 2,
            "target membership validation is not miscounted as a finite occluder sweep test");
    }

    private static HeadlampOcclusionFrame Frame(params HeadlampOccluder[] shapes) => new(shapes, true);
    private static HeadlampOccluder Shape(Rect2 rect) => new(Transform2D.Identity, rect);
    private static HeadlampOccluder Shape(MinimapSolid solid) =>
        new(solid.LocalToWorld.AffineInverse(), solid.LocalBounds);

    private static void OpenCellAccepted()
    {
        var frame = Frame();
        TestAssert.True(MinimapVisibility.CellAllowed(Vector2.Zero, new Rect2(16, -4, 8, 8), frame),
            "OpenCellAccepted: complete empty frame authorizes the whole free cell");
        TestAssert.Equal(0, frame.PhysicsQueries, "visibility must not create physics rays");
    }

    private static void ThinWallInsideCellRejected()
    {
        var cell = new Rect2(16, -4, 8, 8);
        var frame = Frame(Shape(new Rect2(19, 1, .125f, 1)));
        TestAssert.True(frame.CanSee(Vector2.Zero, cell.GetCenter()),
            "negative control: the center ray is clear while part of the cell contains a thin wall");
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, cell, frame),
            "ThinWallInsideCellRejected: cell center alone cannot authorize the other side");
    }

    private static void BlockerBetweenCornerRaysRejected()
    {
        // All four corner rays miss (10,0), but the visibility hull contains it.
        var cell = new Rect2(20, -10, 8, 20);
        var frame = Frame(Shape(new Rect2(9.9f, -.1f, .2f, .2f)));
        foreach (Vector2 corner in new[] { cell.Position, cell.End,
                     new Vector2(cell.End.X, cell.Position.Y), new Vector2(cell.Position.X, cell.End.Y) })
            TestAssert.True(frame.CanSee(Vector2.Zero, corner), "negative control really misses corner rays");
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, cell, frame),
            "BlockerBetweenCornerRaysRejected: sweep is not four sampled rays");
    }

    private static void AngledNonGridFrontAcceptedBackRejected()
    {
        foreach (float mirror in new[] { 1f, -1f })
        {
            Transform2D transform = new(.37f, new Vector2(27.25f, 41.125f));
            transform.X *= mirror;
            var solid = new MinimapSolid(transform, new Rect2(-10, -3, 20, 6));
            var face = new MinimapBoundary(0, new Vector2(-4, -3), new Vector2(4, -3), Vector2.Up);
            var frame = Frame(Shape(solid));
            TestAssert.True(MinimapVisibility.BoundaryAllowed(transform * new Vector2(0, -20), solid, face, frame),
                "AngledNonGridFrontAcceptedBackRejected: actual rotated/mirrored front segment is visible");
            TestAssert.True(!MinimapVisibility.BoundaryAllowed(transform * new Vector2(0, 20), solid, face, frame),
                "AngledNonGridFrontAcceptedBackRejected: far face cannot reveal through the same target");
        }
    }

    private static void TargetInteriorNeverExcluded()
    {
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(16, -4, 8, 8));
        var far = new MinimapBoundary(0, new Vector2(24, -3), new Vector2(24, 3), Vector2.Right);
        TestAssert.True(!MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, far, Frame(Shape(solid))),
            "TargetInteriorNeverExcluded: far-side sweep enters the target body");
        var near = new MinimapBoundary(0, new Vector2(16, -3), new Vector2(16, 3), Vector2.Left);
        TestAssert.True(MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, near, Frame(Shape(solid))),
            "target front terminal contact must remain possible");
        TestAssert.True(!MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, near,
            Frame(Shape(solid), Shape(new Rect2(8, -1, 1, 2)))), "another blocker still occludes a target front");
        TestAssert.True(!MinimapVisibility.BoundaryAllowed(new Vector2(20, 0), solid, near, Frame(Shape(solid))),
            "actor within the target body cannot reveal a surface");
    }

    private static void NaNOrSingularRejected()
    {
        var cell = new Rect2(16, -4, 8, 8);
        TestAssert.True(!MinimapVisibility.CellAllowed(new Vector2(float.NaN, 0), cell, Frame()), "NaN actor rejected");
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, new Rect2(16, 0, float.NaN, 8), Frame()),
            "NaN cell rejected");
        var singular = new Transform2D(Vector2.Zero, Vector2.Down, Vector2.Zero);
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, cell,
            Frame(new HeadlampOccluder(singular, new Rect2(10, -5, 2, 10)))), "singular snapshot rejected");
        var solid = new MinimapSolid(singular, cell);
        TestAssert.True(!MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid,
            new MinimapBoundary(0, new Vector2(16, -3), new Vector2(16, 3), Vector2.Left), Frame()),
            "singular target rejected before inversion");
    }

    private static void Shape257Rejected()
    {
        var cell = new Rect2(16, -4, 8, 8);
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(32, -4, 8, 8));
        var face = new MinimapBoundary(0, new Vector2(32, -3), new Vector2(32, 3), Vector2.Left);
        var validShapes = new HeadlampOccluder[256];
        Array.Fill(validShapes, Shape(new Rect2(1000, 1000, 8, 8)));
        TestAssert.True(MinimapVisibility.CellAllowed(Vector2.Zero, cell, Frame(validShapes)),
            "exactly 256 far legal shapes must still authorize an open cell");
        validShapes[0] = Shape(solid);
        TestAssert.True(MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, face, Frame(validShapes)),
            "exactly 256 including the target must still authorize its clear front");
        var shapes = new HeadlampOccluder[257];
        Array.Fill(shapes, Shape(new Rect2(1000, 1000, 8, 8)));
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, cell, Frame(shapes)),
            "Shape257Rejected: capacity never truncates blockers");
        shapes[0] = Shape(solid);
        TestAssert.True(!MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, face, Frame(shapes)),
            "Shape257Rejected: boundary capacity also rejects without truncating");
    }

    private static void TransformedFreeCellOcclusion()
    {
        var cell = new Rect2(20, -4, 8, 8);
        foreach (float mirror in new[] { 1f, -1f })
        {
            // The solid contains its transform origin (10,0), which is strictly
            // inside the actor/cell hull. Its local rectangle is near zero, not
            // at world x10: ignoring the transform cannot pass the far positive.
            Transform2D transform = new(.61f, new Vector2(10, 0));
            transform.X *= mirror;
            var solid = new MinimapSolid(transform, new Rect2(-.1f, -1, .2f, 2));
            var frame = Frame(Shape(solid));
            TestAssert.True(!frame.CanSee(Vector2.Zero, cell.GetCenter()),
                "transformed negative control really contains a point on the center ray");
            TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, cell, frame),
                "rotated/mirrored blocker intersects the whole-cell sweep in world space");
            transform.Origin = new Vector2(1000, 1000);
            var far = new MinimapSolid(transform, solid.LocalBounds);
            TestAssert.True(MinimapVisibility.CellAllowed(Vector2.Zero, cell, Frame(Shape(far))),
                "legitimate rotated/mirrored far geometry cannot block an unrelated free cell");
        }
    }

    private static void EdgeCasesFailClosed()
    {
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, new Rect2(16, -4, 8, 8),
            new HeadlampOcclusionFrame(Array.Empty<HeadlampOccluder>(), false)), "incomplete frame rejected");
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, new Rect2(16, -4, 8, 8),
            Frame(Shape(new Rect2(-1, -1, 2, 2)))), "actor inside any blocker rejected");
        TestAssert.True(!MinimapVisibility.CellAllowed(Vector2.Zero, new Rect2(16, -4, 8, 8),
            Frame(Shape(new Rect2(24, -4, 8, 8)))), "free-cell touching unknown wall stays closed");
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(16, -4, 8, 8));
        var zero = new MinimapBoundary(0, new Vector2(16, 0), new Vector2(16, 0), Vector2.Left);
        TestAssert.True(!MinimapVisibility.BoundaryAllowed(Vector2.Zero, solid, zero, Frame(Shape(solid))),
            "zero length boundary rejected");
    }
}
