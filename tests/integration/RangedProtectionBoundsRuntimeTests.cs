using System;
using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class RangedProtectionBoundsRuntimeTests
{
    // The production consumer must protect the same world-space danger that
    // the telegraph draws, not its unrotated Extent or the leaf's local origin.
    public static void Run(SceneTree tree)
    {
        var scope = new Node2D { Name = "RangedProtectionBoundsFixture", Position = new Vector2(300, 150) };
        var leaf = new AttackTelegraphVisual { Name = "ActualWarning", Position = new Vector2(70, 80) };
        scope.AddChild(leaf);
        tree.Root.AddChild(scope);
        try
        {
            TestAssert.True(scope.IsInsideTree() && leaf.IsInsideTree() && leaf.IsVisibleInTree(),
                "protection fixture is mounted and visible before testing consumer geometry");
            leaf.Present(Frame(new AttackShapeFrame(AttackShapeKind.Capsule,
                new Vector2(100, 200), new Vector2(5, 55), -Mathf.Pi / 2,
                Array.Empty<Vector2>())));
            AssertSingle(scope, new Rect2(29, 179, 142, 42),
                "RANGED_PROTECTION_BOUNDS_RED horizontal width-five beam protects its actual endpoints");

            leaf.Present(Frame(new AttackShapeFrame(AttackShapeKind.Rectangle,
                new Vector2(100, 200), new Vector2(20, 10), Mathf.Pi / 2,
                Array.Empty<Vector2>())));
            AssertSingle(scope, new Rect2(74, 164, 52, 72),
                "rotated rectangle warning swaps the protected world dimensions");

            leaf.Present(Frame(new AttackShapeFrame(AttackShapeKind.ConvexPolygon,
                new Vector2(999, -400), Vector2.Zero, Mathf.Pi / 2,
                new Vector2[] { new(30, 60), new(45, 55), new(50, 80), new(35, 90) })));
            AssertSingle(scope, new Rect2(14, 39, 52, 67),
                "world polygon protection does not transform already-world points twice");

            leaf.Present(default);
            TestAssert.Equal(0, RoomProtectionRegionAdapter.CollectGameplay(scope, null).Count,
                "retirement immediately withdraws obsolete protection geometry");
            GD.Print("RANGED_PROTECTION_BOUNDS_OK actual_consumer=true rotated=true polygon=true");
        }
        finally { scope.Free(); }
    }

    private static AttackPresentationFrame Frame(AttackShapeFrame shape) => new(
        new AttackTimelineFrame(1, EnemyAttackKind.PrismLaserBeam, AttackPhase.Active,
            .28f, .28f, new Vector2(200, 200), true), shape);

    private static void AssertSingle(Node2D scope, Rect2 expected, string message)
    {
        var regions = RoomProtectionRegionAdapter.CollectGameplay(scope, null);
        TestAssert.Equal(1, regions.Count, message + " count");
        Rect2 actual = regions[0];
        TestAssert.True(actual.Position.DistanceTo(expected.Position) <= .001f &&
            actual.Size.DistanceTo(expected.Size) <= .001f, message +
            $"; expected={expected}, actual={actual}");
    }
}
