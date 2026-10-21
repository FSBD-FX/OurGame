using Godot;
using Knight.Ui;

namespace Knight.Tests.Unit;

public static class ProtectedPromptPlacerTests
{
    public static void Run()
    {
        PromptStaysNearTargetWithoutCoveringProtectedRegions();
        BossCombatFallsBackToAnUnobstructedSafeEdge();
        ContinuousFormalHudRegionsAreProtectedAndHiddenRegionsDoNotBlock();
        AllBlockedSafeAreaReturnsNoPlacement();
    }

    private static void ContinuousFormalHudRegionsAreProtectedAndHiddenRegionsDoNotBlock()
    {
        var formalHud = new Control { Name = "FormalHud" };
        foreach ((string name, Rect2 rect, bool visible) in new[]
                 {
                     ("Health", new Rect2(48, 40, 352, 80), true),
                     ("ThreadSpool", new Rect2(408, 40, 112, 80), true),
                     ("ObjectiveRibbon", new Rect2(48, 128, 352, 48), true),
                     ("Prompt", new Rect2(840, 40, 392, 48), false),
                     ("ModeSelector", new Rect2(428, 40, 196, 50), true),
                     ("Loadout", new Rect2(428, 92, 196, 42), true),
                     ("ResourceCluster", new Rect2(636, 40, 196, 94), true),
                     ("Boss", new Rect2(320, 560, 640, 72), false)
                 })
        {
            var control = new Control
            {
                Name = name,
                Position = rect.Position,
                Size = rect.Size,
                Visible = visible
            };
            formalHud.AddChild(control);
        }

        var regions = WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formalHud);
        TestAssert.Equal(6, regions.Count,
            "continuous guidance protects every visible gameplay HUD cluster and ignores hidden Prompt/Boss");
        Rect2 safe = new(new Vector2(48, 40), new Vector2(1184, 640));
        Rect2 prompt = WorldTargetMarkerVisual.PlacePrompt(safe, new Vector2(160, 80),
            new Vector2(480, 88), regions);
        TestAssert.True(prompt.HasArea() && safe.Encloses(prompt),
            "continuous first-level guidance still finds a safe visible placement");
        foreach (Rect2 region in regions)
            TestAssert.True(!prompt.Intersects(region.Grow(8f)),
                "continuous first-level guidance never covers a visible FormalHud region");

        formalHud.Free();
    }

    private static void PromptStaysNearTargetWithoutCoveringProtectedRegions()
    {
        Rect2 safe = new(new Vector2(48, 40), new Vector2(1184, 640));
        Vector2 target = new(640, 360);
        Rect2[] protectedRegions =
        [
            new(new Vector2(610, 330), new Vector2(60, 60)),
            new(new Vector2(674, 292), new Vector2(250, 96)),
            new(new Vector2(376, 332), new Vector2(240, 86))
        ];

        Rect2 prompt = ProtectedPromptPlacer.Place(safe, target, new Vector2(248, 48),
            protectedRegions);

        TestAssert.True(safe.Encloses(prompt),
            "protected target prompt remains inside the HUD safe area");
        foreach (Rect2 region in protectedRegions)
            TestAssert.True(!prompt.Intersects(region.Grow(8f)),
                "protected target prompt never covers player enemy or danger geometry");
        TestAssert.True(prompt.GetCenter().DistanceTo(target) <= 260f,
            "protected target prompt remains contextually near its world target");
    }

    private static void BossCombatFallsBackToAnUnobstructedSafeEdge()
    {
        Rect2 safe = new(new Vector2(48, 40), new Vector2(1184, 640));
        Vector2 bossTarget = new(640, 360);
        Rect2[] bossCombatGeometry =
        [
            // Real Fallen Bellkeeper screen-space body/feet and the four attack-shape
            // bounds around the canonical arena target. Together these intentionally
            // cover the local cardinal/diagonal prompt ring.
            new(new Vector2(577, 283), new Vector2(126, 154)),
            new(new Vector2(590, 420), new Vector2(100, 46)),
            new(new Vector2(512, 282), new Vector2(308, 168)), // Sweep
            new(new Vector2(562, 282), new Vector2(156, 156)), // Leap Slam
            new(new Vector2(499, 357), new Vector2(282, 66)),  // Ground Wave
            new(new Vector2(532, 232), new Vector2(236, 222)), // Hammer
            new(new Vector2(320, 560), new Vector2(640, 62))   // Bottom Boss HUD
        ];

        Rect2 prompt = ProtectedPromptPlacer.Place(safe, bossTarget, new Vector2(248, 48),
            bossCombatGeometry);

        TestAssert.True(safe.Encloses(prompt),
            "Boss combat guidance fallback remains inside the HUD safe area");
        foreach (Rect2 geometry in bossCombatGeometry)
            TestAssert.True(!prompt.Intersects(geometry.Grow(8f)),
                "Boss combat guidance fallback never covers the Boss, feet, or four attack reads");
        TestAssert.True(prompt.Position.Y <= safe.Position.Y + 1f,
            "Boss combat guidance uses a stable top safe-edge fallback instead of the centered target ring");
    }

    private static void AllBlockedSafeAreaReturnsNoPlacement()
    {
        Rect2 safe = new(new Vector2(48, 40), new Vector2(1184, 640));
        Rect2[] fullyProtected =
        [
            new Rect2(safe.Position - new Vector2(16f, 16f),
                safe.Size + new Vector2(32f, 32f))
        ];

        Rect2 prompt = ProtectedPromptPlacer.Place(safe, new Vector2(640, 360),
            new Vector2(248, 48), fullyProtected);

        TestAssert.True(!prompt.HasArea(),
            "all-blocked protected prompt placement returns explicit no-placement instead of an unsafe card");
    }
}
