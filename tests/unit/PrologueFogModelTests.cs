using Godot;
using Knight.Presentation.Fog;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class PrologueFogModelTests
{
    private static readonly PrologueFogProfile Fixture = new(180f, 620f, .72f, 620f, 22.5f);

    public static void Run()
    {
        NearClearFarHeavier();
        BeamReducesFog();
        BeamUsesActualLampPositionAndNormalizedDirection();
        BeamFallsOffAtRangeAndAngle();
        BaseClearRadiusFollowsActorNotLamp();
        OnlyBossHallReduces();
        RestAreaReadable();
        WorldPointSelectsRegionNotActor();
        RegionBoundariesStayContinuous();
        UnknownAndCorridorFallbackAreReadable();
        NonfiniteWorldOrActorNeverClearsFog();
        InvalidLampPoseDisablesOnlyBeam();
        MalformedProfilesUseSafeDefaults();
        ExtremeFinitePositionsRemainBounded();
        BeamAxisAtSmallLegalHalfAngle();
        ExtremeFiniteDirectionAndZeroRange();
    }

    // Catches a flat overlay, reversed distance weight, hard radius edge, or missing opacity cap.
    private static void NearClearFarHeavier()
    {
        var samples = new (float Distance, float Opacity)[]
        {
            (0f, 0f), (90f, 0f), (180f, 0f), (290f, .1125f),
            (400f, .36f), (510f, .6075f), (620f, .72f), (1000f, .72f)
        };
        float previous = -1f;
        foreach (var sample in samples)
        {
            float actual = PrologueFogModel.EvaluateOpacity(new Vector2(sample.Distance, 0f),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, Fixture);
            AssertOpacity(sample.Opacity, actual,
                $"NearClearFarHeavier distance={sample.Distance}: clear near, smooth distance fog farther away");
            TestAssert.True(actual >= previous && actual <= .72f,
                "distance-only opacity must increase monotonically and stay below its configured cap");
            previous = actual;
        }
        AssertContinuousOpacity(179.99f, 180.01f, "clear-radius edge");
        AssertContinuousOpacity(619.99f, 620.01f, "outer-radius edge");
    }

    // Catches an ignored beam, reversed cone, or an omnidirectional flashlight.
    private static void BeamReducesFog()
    {
        float inside = Opacity(new Vector2(400f, 0f), Vector2.Right);
        float side = Opacity(new Vector2(0f, 400f), Vector2.Right);
        float behind = Opacity(new Vector2(-400f, 0f), Vector2.Right);
        AssertBounded(inside, .36f, "beam center");
        TestAssert.True(inside < side, "the forward light cone reduces fog beyond the base clear radius");
        AssertOpacity(.36f, side, "same-distance side point keeps its distance fog");
        AssertOpacity(.36f, behind, "same-distance rear point is not lit by the forward cone");
    }

    // Catches using actor/screen center as the light origin or using direction magnitude as intensity.
    private static void BeamUsesActualLampPositionAndNormalizedDirection()
    {
        Vector2 point = new(400f, 0f);
        float aligned = Opacity(point, Vector2.Right);
        float displaced = PrologueFogModel.EvaluateOpacity(point, Vector2.Zero,
            new Vector2(0f, -400f), Vector2.Right, Fixture);
        TestAssert.True(aligned < displaced, "moving the actual lamp moves its cone away from the sample");
        AssertOpacity(.36f, displaced, "a point outside the displaced lamp cone retains base fog");
        float nonUnit = Opacity(point, new Vector2(10f, 0f));
        AssertOpacity(aligned, nonUnit, "equivalent normalized directions produce the same beam");
        float vertical = Opacity(new Vector2(0f, -400f), Vector2.Up);
        AssertOpacity(aligned, vertical, "turning upward rotates the light without changing its strength");
    }

    // Catches light extending beyond range and a hard angular cut instead of a continuous taper.
    private static void BeamFallsOffAtRangeAndAngle()
    {
        AssertOpacity(.72f, Opacity(new Vector2(620f, 0f), Vector2.Right),
            "beam contribution reaches zero at its finite range");
        AssertOpacity(.72f, Opacity(new Vector2(700f, 0f), Vector2.Right),
            "the lamp does not extend clear space beyond its range");
        float innerAngle = Opacity(AtAngle(10f, 400f), Vector2.Right);
        float outerAngle = Opacity(AtAngle(20f, 400f), Vector2.Right);
        TestAssert.True(innerAngle < outerAngle && outerAngle < .36f,
            "beam reduction tapers continuously toward the cone edge");
        float before = Opacity(AtAngle(22.49f, 400f), Vector2.Right);
        float after = Opacity(AtAngle(22.51f, 400f), Vector2.Right);
        AssertBounded(before, .36f, "just inside cone edge");
        AssertOpacity(.36f, after, "outside cone edge");
        TestAssert.True(Mathf.Abs(before - after) < .002f, "cone edge cannot jump in opacity");
        float lampCoincidence = PrologueFogModel.EvaluateOpacity(new Vector2(400f, 0f),
            Vector2.Zero, new Vector2(400f, 0f), Vector2.Right, Fixture);
        AssertBounded(lampCoincidence, .36f, "sample at the lamp has no invalid zero-length normalization");
    }

    // Catches moving the player's basic visibility circle to the animated lamp.
    private static void BaseClearRadiusFollowsActorNotLamp()
    {
        float actual = PrologueFogModel.EvaluateOpacity(new Vector2(80f, 0f), Vector2.Zero,
            new Vector2(1000f, 1000f), Vector2.Zero, Fixture);
        AssertOpacity(0f, actual, "actor's near field stays clear even when the lamp pose moves");
    }

    // Catches LostTemple-wide clearing and wrong route-ID profile mapping.
    private static void OnlyBossHallReduces()
    {
        AssertProfileOpacity(.55f, PrologueFogProfile.ForRegion(CanyonRouteNodeId.SealedCourtyard),
            "courtyard remains exploration, not BossHall");
        AssertProfileOpacity(.18f, PrologueFogProfile.ForRegion(CanyonRouteNodeId.BossHall),
            "BossHall has its own lighter candidate profile");
        AssertProfileOpacity(.10f, PrologueFogProfile.ForRegion(CanyonRouteNodeId.SafeExit),
            "safe exit remains clear");
        AssertProfileOpacity(.28f, PrologueFogProfile.ForRegion(CanyonRouteNodeId.LowCeilingBridge),
            "entrance teaching area is lighter than exploration");
        AssertProfileOpacity(.72f, PrologueFogProfile.ForRegion(CanyonRouteNodeId.ControlledAbyss),
            "deep abyss retains its denser candidate profile");
        AssertProfileOpacity(.55f, PrologueFogModel.ResolveProfile(new Vector2(5480f, 1000f)),
            "actual courtyard sample does not inherit the temple sector's Boss profile");
        AssertProfileOpacity(.55f, PrologueFogModel.ResolveProfile(new Vector2(6079f, 800f)),
            "Boss transition stays inside BossHall and cannot lighten the adjacent courtyard pixel");
        AssertProfileOpacity(.18f, PrologueFogModel.ResolveProfile(new Vector2(6680f, 1080f)),
            "actual BossHall interior sample uses the Boss profile");
        AssertProfileOpacity(.10f, PrologueFogModel.ResolveProfile(new Vector2(7480f, 1080f)),
            "actual SafeExit interior sample uses the exit profile");
    }

    // Catches missing camp/checkpoint comfort or turning the whole courtyard into a rest area.
    private static void RestAreaReadable()
    {
        AssertProfileOpacity(.10f, PrologueFogModel.ResolveProfile(new Vector2(1680f, 3960f)),
            "basin camp/checkpoint interior is comfortable");
        float forecourt = PrologueFogModel.ResolveProfile(new Vector2(5920f, 1080f)).MaxOpacity;
        AssertBounded(forecourt, .28f, "authored temple-forecourt checkpoint comfort");
        TestAssert.True(forecourt < .55f, "temple checkpoint has a local reduction within exploration");
        AssertProfileOpacity(.55f, PrologueFogModel.ResolveProfile(new Vector2(5800f, 1080f)),
            "courtyard outside the authored rest bounds must not become a camp");
        float start = PrologueFogModel.ResolveProfile(new Vector2(160f, 600f)).MaxOpacity;
        AssertBounded(start, .28f, "start checkpoint teaching visibility");
    }

    // Catches actor-region selection clearing other regions visible in the same viewport.
    private static void WorldPointSelectsRegionNotActor()
    {
        Vector2 courtyardPoint = new(5520f, 1080f);
        PrologueFogProfile profile = PrologueFogModel.ResolveProfile(courtyardPoint);
        foreach (Vector2 actor in new[]
        {
            new Vector2(4800f, 1600f), new Vector2(7040f, 1200f), new Vector2(7500f, 1080f)
        })
        {
            float actual = PrologueFogModel.EvaluateOpacity(courtyardPoint, actor,
                actor + new Vector2(0f, -42f), Vector2.Zero, profile);
            AssertOpacity(.55f, actual,
                "moving the actor into BossHall/SafeExit cannot clear a distant courtyard pixel");
        }
    }

    // Catches abrupt density switches at route/rest boundaries and half-open rectangle edges.
    private static void RegionBoundariesStayContinuous()
    {
        var boundaries = new (Vector2 Point, Vector2 Normal, string Name)[]
        {
            (new Vector2(6080f, 800f), Vector2.Right, "courtyard/BossHall"),
            (new Vector2(7280f, 1080f), Vector2.Right, "BossHall/SafeExit"),
            (new Vector2(2080f, 3960f), Vector2.Right, "camp/gears"),
            (new Vector2(5880f, 1040f), Vector2.Right, "local checkpoint/exploration"),
            (new Vector2(1920f, 2400f), Vector2.Right, "detour/abyss"),
            (new Vector2(2240f, 400f), Vector2.Down, "rift/abyss")
        };
        foreach (var boundary in boundaries)
        {
            float before = PrologueFogModel.ResolveProfile(boundary.Point - boundary.Normal * .01f).MaxOpacity;
            float on = PrologueFogModel.ResolveProfile(boundary.Point).MaxOpacity;
            float after = PrologueFogModel.ResolveProfile(boundary.Point + boundary.Normal * .01f).MaxOpacity;
            AssertBounded(before, .72f, boundary.Name + " before");
            AssertBounded(on, .72f, boundary.Name + " on");
            AssertBounded(after, .72f, boundary.Name + " after");
            TestAssert.True(Mathf.Abs(before - on) < .002f && Mathf.Abs(on - after) < .002f,
                boundary.Name + " density must blend continuously in world space");
        }
    }

    // Catches an unknown enum, uncovered corridor, or invalid position producing black/clear regions.
    private static void UnknownAndCorridorFallbackAreReadable()
    {
        AssertProfileOpacity(.55f, PrologueFogProfile.ForRegion((CanyonRouteNodeId)(-1)),
            "unknown route ID uses exploration fallback");
        foreach (Vector2 point in new[]
        {
            new Vector2(-2000f, -2000f), new Vector2(3380f, 3800f),
            new Vector2(float.NaN, 0f), new Vector2(0f, float.PositiveInfinity),
            new Vector2(float.NegativeInfinity, 0f)
        })
            AssertProfileOpacity(.55f, PrologueFogModel.ResolveProfile(point),
                "unknown/corridor/nonfinite world point uses bounded exploration fallback");
    }

    // Catches NaN propagating through distance math or invalid positions accidentally clearing fog.
    private static void NonfiniteWorldOrActorNeverClearsFog()
    {
        foreach (Vector2 invalid in InvalidVectors())
        {
            AssertOpacity(.72f, PrologueFogModel.EvaluateOpacity(invalid, Vector2.Zero,
                Vector2.Zero, Vector2.Right, Fixture), "invalid world point keeps the valid profile's opacity cap");
            AssertOpacity(.72f, PrologueFogModel.EvaluateOpacity(new Vector2(400f, 0f), invalid,
                Vector2.Zero, Vector2.Right, Fixture), "invalid actor position cannot grant clear space");
        }
    }

    // Catches corrupt lamp data clearing the scene or breaking valid actor-relative distance fog.
    private static void InvalidLampPoseDisablesOnlyBeam()
    {
        foreach (Vector2 invalid in InvalidVectors())
        {
            AssertOpacity(.36f, PrologueFogModel.EvaluateOpacity(new Vector2(400f, 0f), Vector2.Zero,
                invalid, Vector2.Right, Fixture), "invalid lamp position disables only light reduction");
            AssertOpacity(.36f, PrologueFogModel.EvaluateOpacity(new Vector2(400f, 0f), Vector2.Zero,
                Vector2.Zero, invalid, Fixture), "invalid lamp direction disables only light reduction");
        }
        AssertOpacity(.36f, Opacity(new Vector2(400f, 0f), Vector2.Zero),
            "zero direction means no directional light, not an arbitrary default cone");
    }

    // Catches malformed external candidate data escaping the [0,1] range or causing division by zero.
    private static void MalformedProfilesUseSafeDefaults()
    {
        var invalid = new[]
        {
            new PrologueFogProfile(float.NaN, 620f, .72f, 620f, 22.5f),
            new PrologueFogProfile(-1f, 620f, .72f, 620f, 22.5f),
            new PrologueFogProfile(180f, 180f, .72f, 620f, 22.5f),
            new PrologueFogProfile(180f, 100f, .72f, 620f, 22.5f),
            new PrologueFogProfile(180f, float.PositiveInfinity, .72f, 620f, 22.5f),
            new PrologueFogProfile(180f, 620f, float.NaN, 620f, 22.5f),
            new PrologueFogProfile(180f, 620f, -1f, 620f, 22.5f),
            new PrologueFogProfile(180f, 620f, 1.01f, 620f, 22.5f),
            new PrologueFogProfile(180f, 620f, .72f, -1f, 22.5f),
            new PrologueFogProfile(180f, 620f, .72f, float.PositiveInfinity, 22.5f),
            new PrologueFogProfile(180f, 620f, .72f, 620f, 0f),
            new PrologueFogProfile(180f, 620f, .72f, 620f, 91f),
            new PrologueFogProfile(180f, 620f, .72f, 620f, float.NaN)
        };
        foreach (PrologueFogProfile profile in invalid)
            AssertOpacity(.55f, PrologueFogModel.EvaluateOpacity(new Vector2(1000f, 0f),
                Vector2.Zero, Vector2.Zero, Vector2.Zero, profile),
                "malformed profiles use bounded exploration defaults rather than black/clear/NaN fog");
    }

    // Catches overflow from large but finite world coordinates entering the renderer's opacity.
    private static void ExtremeFinitePositionsRemainBounded()
    {
        float actual = PrologueFogModel.EvaluateOpacity(new Vector2(float.MaxValue, float.MaxValue),
            new Vector2(-float.MaxValue, -float.MaxValue), Vector2.Zero, Vector2.Right, Fixture);
        AssertOpacity(.72f, actual, "extreme finite distance saturates instead of becoming NaN");
    }

    // A legal narrow cone must still recognize its exact diagonal axis. acos(dot)
    // loses that axis to normalization rounding even though all inputs are finite.
    private static void BeamAxisAtSmallLegalHalfAngle()
    {
        var narrow = Fixture with { BeamHalfAngleDegrees = 1e-8f };
        float actual = PrologueFogModel.EvaluateOpacity(new Vector2(100f, 100f),
            new Vector2(-1000f, -1000f), Vector2.Zero, new Vector2(1f, 1f), narrow);
        AssertOpacity(.0952933f, actual, "exact diagonal beam axis survives a small legal half-angle");
    }

    // Exercise the live beam branch, not the previous extreme-distance early return.
    private static void ExtremeFiniteDirectionAndZeroRange()
    {
        float expected = Opacity(new Vector2(400f, 0f), Vector2.Right);
        float largeDirection = Opacity(new Vector2(400f, 0f), new Vector2(float.MaxValue, 0f));
        AssertOpacity(expected, largeDirection, "huge finite direction retains an active normalized beam");
        var noBeam = Fixture with { BeamRange = 0f };
        float distanceOnly = PrologueFogModel.EvaluateOpacity(new Vector2(400f, 0f),
            Vector2.Zero, Vector2.Zero, Vector2.Right, noBeam);
        AssertOpacity(.36f, distanceOnly, "zero beam range disables only the directional reduction");
    }

    private static Vector2[] InvalidVectors() =>
    [
        new(float.NaN, 0f), new(0f, float.NaN),
        new(float.PositiveInfinity, 0f), new(0f, float.NegativeInfinity)
    ];

    private static Vector2 AtAngle(float degrees, float distance)
    {
        float radians = Mathf.DegToRad(degrees);
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * distance;
    }

    private static float Opacity(Vector2 point, Vector2 direction) =>
        PrologueFogModel.EvaluateOpacity(point, Vector2.Zero, Vector2.Zero, direction, Fixture);

    private static void AssertContinuousOpacity(float beforeDistance, float afterDistance, string context)
    {
        float before = Opacity(new Vector2(beforeDistance, 0f), Vector2.Zero);
        float after = Opacity(new Vector2(afterDistance, 0f), Vector2.Zero);
        AssertBounded(before, .72f, context + " before");
        AssertBounded(after, .72f, context + " after");
        TestAssert.True(Mathf.Abs(before - after) < .002f, context + " must remain continuous");
    }

    private static void AssertProfileOpacity(float expected, PrologueFogProfile profile, string context) =>
        AssertOpacity(expected, profile.MaxOpacity, context);

    private static void AssertOpacity(float expected, float actual, string context)
    {
        AssertBounded(actual, 1f, context);
        TestAssert.Near(expected, actual, .0001f, context);
    }

    private static void AssertBounded(float actual, float maximum, string context) =>
        TestAssert.True(float.IsFinite(actual) && actual >= 0f && actual <= maximum + .0001f,
            context + ": opacity must be finite and bounded");
}
