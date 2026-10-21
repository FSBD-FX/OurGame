using System;
using Godot;
using Knight.Player;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class CanyonCameraFocusModelTests
{
    public static void Run()
    {
        AbyssLookAheadIsMonotonicClampedAndReturnsSmoothlyAfterLanding();
        OverlapSelectsTheControlledAbyssRegionBeforeTheGeneralDescent();
        AuthoredBrakeWallCoverageHasNoVerticalBlindGap();
        WorldFocusIsConvertedToPlayerLocalCoordinates();
        InvalidMaximumFallSpeedsAreRejected();
    }

    private static void OverlapSelectsTheControlledAbyssRegionBeforeTheGeneralDescent()
    {
        CanyonCameraFocusModel model = Model();
        Vector2 player = new(2000f, 3500f);
        CanyonCameraFrame target = model.ResolveTarget(player, Vector2.Zero, Vector2.Zero,
            world => world - player);

        TestAssert.Equal("canyon.v2.camera.controlled-abyss", target.RegionId,
            "the authored first-priority abyss region wins its overlap with general descent");
        TestAssert.Equal(new Vector2(0f, 80f), target.FocusOffset,
            "the reverse seam oracle uses the selected abyss region's stationary target");
    }

    private static void AuthoredBrakeWallCoverageHasNoVerticalBlindGap()
    {
        float coveredThrough = 400f;
        foreach (Rect2 segment in CanyonWorldLayout.AbyssBrakeWallSegments)
        {
            TestAssert.True(segment.Position.Y <= coveredThrough,
                "authored reachable brake-wall segments do not leave a vertical blind gap");
            coveredThrough = Mathf.Max(coveredThrough, segment.End.Y);
        }
        TestAssert.True(coveredThrough >= CanyonWorldLayout.AbyssSafeBottom.Position.Y,
            "brake-wall coverage continues through the safe-bottom decision window");
    }

    private static void AbyssLookAheadIsMonotonicClampedAndReturnsSmoothlyAfterLanding()
    {
        CanyonCameraFocusModel model = Model();
        float maxFallSpeed = new PlayerMotionSettings().MaxFallSpeed;

        CanyonCameraFrame stationary = Target(model, 0f);
        CanyonCameraFrame halfFall = Target(model, maxFallSpeed * .5f);
        CanyonCameraFrame fullFall = Target(model, maxFallSpeed);
        CanyonCameraFrame excessiveFall = Target(model, maxFallSpeed * 2f);

        TestAssert.Near(80f, stationary.FocusOffset.Y, .001f,
            "the abyss keeps readable downward framing before the fall accelerates");
        TestAssert.Near(170f, halfFall.FocusOffset.Y, .001f,
            "half maximum fall speed produces the hand-derived midpoint look-ahead");
        TestAssert.Near(260f, fullFall.FocusOffset.Y, .001f,
            "maximum fall speed reaches the authored cap");
        TestAssert.Near(260f, excessiveFall.FocusOffset.Y, .001f,
            "fall speed beyond the motor cap cannot push the view farther down");
        TestAssert.True(stationary.FocusOffset.Y < halfFall.FocusOffset.Y &&
                halfFall.FocusOffset.Y < fullFall.FocusOffset.Y,
            "downward look-ahead increases monotonically with real player velocity");

        CanyonCameraFrame landedTarget = Target(model, 0f);
        CanyonCameraFrame firstLandingFrame = model.Blend(fullFall, landedTarget, 1f / 60f);
        TestAssert.True(firstLandingFrame.FocusOffset.Y < 260f &&
                firstLandingFrame.FocusOffset.Y > 80f,
            "landing eases back from maximum look-ahead instead of snapping");
    }

    private static void WorldFocusIsConvertedToPlayerLocalCoordinates()
    {
        CanyonCameraFocusModel model = Model();
        Vector2 playerWorld = new(2240f, 1200f);
        Vector2 authoredLocal = new(17f, -23f);
        CanyonCameraFrame frame = model.ResolveTarget(playerWorld, Vector2.Zero,
            authoredLocal, world => world - playerWorld);

        TestAssert.Equal(new Vector2(17f, 57f), frame.LocalPosition,
            "a translated Player parent receives a local camera position, not the world focus point");
        TestAssert.True(frame.LocalPosition != playerWorld + frame.FocusOffset,
            "feeding the world-space focus directly to Camera2D.Position would double-count Player translation");
    }

    private static void InvalidMaximumFallSpeedsAreRejected()
    {
        AssertThrows<ArgumentOutOfRangeException>(() =>
            new CanyonCameraFocusModel(CanyonWorldLayout.CameraFocusRegions, 0f),
            "zero maximum fall speed is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() =>
            new CanyonCameraFocusModel(CanyonWorldLayout.CameraFocusRegions, float.NaN),
            "nonfinite maximum fall speed is rejected");
    }

    private static CanyonCameraFocusModel Model() => new(
        CanyonWorldLayout.CameraFocusRegions, new PlayerMotionSettings().MaxFallSpeed);

    private static CanyonCameraFrame Target(CanyonCameraFocusModel model, float velocityY)
    {
        Vector2 player = new(2240f, 1200f);
        return model.ResolveTarget(player, new Vector2(0f, velocityY), Vector2.Zero,
            world => world - player);
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException($"Assertion failed: {message}");
    }
}
