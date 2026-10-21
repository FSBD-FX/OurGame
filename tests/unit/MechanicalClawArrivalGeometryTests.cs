using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static class MechanicalClawArrivalGeometryTests
{
    public static void Run()
    {
        WallDockUsesCapsuleRadiusAndMinimumRope();
        CeilingDockUsesCapsuleHalfHeightAndMinimumRope();
        ArrivalRequiresTheSavedSideOfTheSurface();
        CollisionNormalMustMatchTheSavedFace();
    }

    private static void WallDockUsesCapsuleRadiusAndMinimumRope()
    {
        using var capsule = new CapsuleShape2D { Radius = 20f, Height = 72f };
        Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(
            new Vector2(100f, 80f), Vector2.Left, capsule, 48f, 4f);
        TestAssert.Equal(new Vector2(52f, 80f), dock,
            "wall docking remains one 48px safe rope length outside the hit pixel");
    }

    private static void CeilingDockUsesCapsuleHalfHeightAndMinimumRope()
    {
        using var capsule = new CapsuleShape2D { Radius = 20f, Height = 72f };
        Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(
            new Vector2(100f, 80f), Vector2.Down, capsule, 48f, 4f);
        TestAssert.Equal(new Vector2(100f, 128f), dock,
            "ceiling docking keeps the player centre below the physical hit face");
    }

    private static void ArrivalRequiresTheSavedSideOfTheSurface()
    {
        using var capsule = new CapsuleShape2D { Radius = 20f, Height = 72f };
        Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(
            new Vector2(100f, 80f), Vector2.Left, capsule, 48f, 4f);

        TestAssert.True(MechanicalClawArrivalGeometry.IsArrived(
                new Vector2(56f, 80f), dock, 8f),
            "arrival accepts the capsule centre within tolerance on the saved face side");
        TestAssert.True(!MechanicalClawArrivalGeometry.IsArrived(
                new Vector2(144f, 80f), dock, 8f),
            "arrival rejects the mirrored point on the opposite side of the surface");
    }

    private static void CollisionNormalMustMatchTheSavedFace()
    {
        TestAssert.True(MechanicalClawArrivalGeometry.NormalMatches(
                Vector2.Left, new Vector2(-.99f, .05f), .9f),
            "a collision normal on the locked face satisfies the saved-normal threshold");
        TestAssert.True(!MechanicalClawArrivalGeometry.NormalMatches(
                Vector2.Left, Vector2.Right, .9f),
            "the opposite collision face cannot satisfy the saved-normal threshold");
        TestAssert.True(!MechanicalClawArrivalGeometry.NormalMatches(
                Vector2.Left, Vector2.Zero, .9f),
            "an indeterminate collision normal cannot match a saved face");
    }
}
