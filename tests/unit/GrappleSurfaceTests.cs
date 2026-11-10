using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static class GrappleSurfaceTests
{
    public static void Run()
    {
        EveryAuthoredMaterialKeepsAnExplicitFiniteAnchor();
        CollisionProfilesDeclareRealHitPointAnchorsExplicitly();
        AssistRadiusRejectsValuesOutsideTheApprovedCursorWindow();
        LegacyKindsAreTypedInsteadOfInferredFromNodeNames();
    }

    private static void EveryAuthoredMaterialKeepsAnExplicitFiniteAnchor()
    {
        foreach (GrappleSurfaceMaterial material in System.Enum.GetValues<GrappleSurfaceMaterial>())
        {
            var surface = new GrappleSurface2D
            {
                Name = $"Surface_{material}",
                Position = new Vector2(90f, -30f),
                AnchorOffset = new Vector2(6f, -4f),
                Material = material,
                AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
            };

            TestAssert.Equal(0, surface.Validate().Count,
                $"{material} is valid authored gameplay data without visual or name inference");
            TestAssert.Equal(new Vector2(96f, -34f), surface.WorldAnchor,
                $"{material} exposes its explicit world anchor");
            TestAssert.True(!surface.IsAvailable,
                $"{material} is unavailable before it belongs to a live scene tree");
            surface.Free();
        }
    }

    private static void CollisionProfilesDeclareRealHitPointAnchorsExplicitly()
    {
        var profile = new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.GripStone,
            UseCollisionHitPoint = true,
            AnchorOffset = new Vector2(900f, 700f)
        };

        TestAssert.Equal(0, profile.Validate().Count,
            "typed terrain can opt into the collision ray hit without deriving gameplay from its name");
        TestAssert.True(profile.UseCollisionHitPoint,
            "the authored collision-hit contract remains explicit gameplay data");
        profile.Free();
    }

    private static void AssistRadiusRejectsValuesOutsideTheApprovedCursorWindow()
    {
        var surface = new GrappleSurface2D
        {
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = 36.01f
        };
        TestAssert.True(surface.Validate().Count > 0,
            "a surface cannot silently widen the approved 36-pixel cursor assist");
        surface.AssistRadius = float.NaN;
        TestAssert.True(surface.Validate().Count > 0,
            "non-finite cursor assist cannot enter a physics query");
        surface.Free();
    }

    private static void LegacyKindsAreTypedInsteadOfInferredFromNodeNames()
    {
        TestAssert.True(!ThreadTargetKind.Anchor.IsLegacyInteraction(),
            "world anchors establish the persistent rope contract");
        TestAssert.True(ThreadTargetKind.LightEnemy.IsLegacyInteraction(),
            "light enemies use the legacy pull adapter");
        TestAssert.True(ThreadTargetKind.HeavyEnemy.IsLegacyInteraction(),
            "heavy enemies use the legacy weak-point adapter");
        TestAssert.True(ThreadTargetKind.Mechanism.IsLegacyInteraction(),
            "mechanisms use the legacy activation adapter");
    }
}
