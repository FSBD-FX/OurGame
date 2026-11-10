using Godot;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class SecondaryMotionModelTests
{
    public static void Run()
    {
        CapeLanternSpoolAndTetherLagDifferentInputs();
        SecondaryOffsetsRemainInsideDeclaredBounds();
        ResultsAreStableAcrossEquivalentDeltaPartitions();
        InvalidInputsAndDeltaFailClosedWithoutNonFiniteOutputs();
        FirstSpringStepMovesPartwayWithoutSnapping();
        SpringCarriesInertiaThenDecaysAfterDriveRemoval();
        SpringMatchesClosedFormBeforeSteadyState();
        PartitionEquivalenceSurvivesTargetChangeWithVelocity();
        HugeFiniteDeltaSettlesToTargetWithoutResettingOrigin();
        GroundAttackTargetsAreBoundedIndependentAndInertial();
    }

    private static void CapeLanternSpoolAndTetherLagDifferentInputs()
    {
        SecondaryMotionFrame cape = new SecondaryMotionModel().Step(
            Input(acceleration: new Vector2(900f, 0f), turn: 1f), 1f / 30f);
        SecondaryMotionFrame lantern = new SecondaryMotionModel().Step(
            Input(landingImpulse: 1f, contactImpulse: 0.5f), 1f / 30f);
        SecondaryMotionFrame spool = new SecondaryMotionModel().Step(
            Input(threadPhase: PlayerActionPhase.Burst, threadEnergy: 1f), 1f / 30f);
        SecondaryMotionFrame tether = new SecondaryMotionModel().Step(
            Input(acceleration: new Vector2(700f, -300f), landingImpulse: 0.75f,
                threadPhase: PlayerActionPhase.Contact, threadEnergy: 0.8f), 1f / 30f);

        TestAssert.True(Mathf.Abs(cape.Mantle.RotationRadians) > 0.0001f,
            "horizontal acceleration and turning excite the mantle channel");
        TestAssert.True(Mathf.Abs(lantern.Lantern.Position.Y) > 0.0001f,
            "landing and contact impulses excite the lantern channel");
        TestAssert.True(Mathf.Abs(spool.ShoulderSpool.RotationRadians) > 0.0001f,
            "thread burst energy excites the shoulder-spool channel");
        TestAssert.True(tether.Tether.Position.Length() > 0.0001f,
            "lantern/spool target separation excites the tether channel");
        TestAssert.True(Mathf.Abs(cape.Mantle.RotationRadians - lantern.Lantern.RotationRadians) > 0.0001f &&
            Mathf.Abs(spool.ShoulderSpool.RotationRadians - tether.Tether.RotationRadians) > 0.0001f,
            "the four secondary channels do not collapse to one shared response");
    }

    private static void SecondaryOffsetsRemainInsideDeclaredBounds()
    {
        var model = new SecondaryMotionModel();
        var extreme = Input(
            velocity: new Vector2(100000f, -100000f),
            acceleration: new Vector2(-100000f, 100000f),
            turn: 1000f,
            landingImpulse: 1000f,
            contactImpulse: -1000f,
            threadPhase: PlayerActionPhase.Burst,
            threadEnergy: 1000f);
        SecondaryMotionFrame frame = default;
        for (int index = 0; index < 240; index++) frame = model.Step(extreme, 1f / 60f);

        AssertBounded(frame.Mantle, SecondaryMotionLimits.MantlePosition,
            SecondaryMotionLimits.MantleRotation, "mantle");
        AssertBounded(frame.Lantern, SecondaryMotionLimits.LanternPosition,
            SecondaryMotionLimits.LanternRotation, "lantern");
        AssertBounded(frame.ShoulderSpool, SecondaryMotionLimits.ShoulderSpoolPosition,
            SecondaryMotionLimits.ShoulderSpoolRotation, "shoulder spool");
        AssertBounded(frame.Tether, SecondaryMotionLimits.TetherPosition,
            SecondaryMotionLimits.TetherRotation, "tether");
    }

    private static void ResultsAreStableAcrossEquivalentDeltaPartitions()
    {
        var coarseModel = new SecondaryMotionModel();
        var fineModel = new SecondaryMotionModel();
        var input = Input(
            velocity: new Vector2(180f, -40f),
            acceleration: new Vector2(-620f, 260f),
            turn: -0.8f,
            landingImpulse: 0.6f,
            contactImpulse: 0.25f,
            threadPhase: PlayerActionPhase.Contact,
            threadEnergy: 0.85f);
        SecondaryMotionFrame coarse = default;
        SecondaryMotionFrame fine = default;

        for (int index = 0; index < 60; index++) coarse = coarseModel.Step(input, 1f / 60f);
        for (int index = 0; index < 120; index++) fine = fineModel.Step(input, 1f / 120f);

        AssertNear(coarse.Mantle, fine.Mantle, SecondaryMotionLimits.PartitionTolerance, "mantle");
        AssertNear(coarse.Lantern, fine.Lantern, SecondaryMotionLimits.PartitionTolerance, "lantern");
        AssertNear(coarse.ShoulderSpool, fine.ShoulderSpool,
            SecondaryMotionLimits.PartitionTolerance, "shoulder spool");
        AssertNear(coarse.Tether, fine.Tether, SecondaryMotionLimits.PartitionTolerance, "tether");
    }

    private static void InvalidInputsAndDeltaFailClosedWithoutNonFiniteOutputs()
    {
        var model = new SecondaryMotionModel();
        SecondaryMotionFrame stable = model.Step(Input(acceleration: new Vector2(240f, -80f)), 1f / 60f);

        TestAssert.Equal(stable, model.Step(Input(turn: 1f), 0f),
            "zero delta preserves the last finite secondary frame");
        TestAssert.Equal(stable, model.Step(Input(turn: 1f), -0.1f),
            "negative delta preserves the last finite secondary frame");
        TestAssert.Equal(stable, model.Step(Input(turn: 1f), float.PositiveInfinity),
            "non-finite delta preserves the last finite secondary frame");

        SecondaryMotionFrame sanitized = model.Step(new SecondaryMotionInput(
            new Vector2(float.NaN, 2f),
            new Vector2(3f, float.PositiveInfinity),
            float.NegativeInfinity,
            float.NaN,
            float.PositiveInfinity,
            (PlayerActionPhase)999,
            float.NaN), 1f / 60f);

        AssertFinite(sanitized.Mantle, "mantle");
        AssertFinite(sanitized.Lantern, "lantern");
        AssertFinite(sanitized.ShoulderSpool, "shoulder spool");
        AssertFinite(sanitized.Tether, "tether");
    }

    private static void FirstSpringStepMovesPartwayWithoutSnapping()
    {
        SecondaryMotionFrame first = new SecondaryMotionModel().Step(
            Input(acceleration: new Vector2(-100000f, 0f)), 1f / 60f);

        TestAssert.True(first.Mantle.Position.X > 0f &&
            first.Mantle.Position.X < SecondaryMotionLimits.MantlePosition.X,
            "the first spring step moves strictly between origin and its clamped target");
    }

    private static void SpringCarriesInertiaThenDecaysAfterDriveRemoval()
    {
        var model = new SecondaryMotionModel();
        SecondaryMotionFrame driven = model.Step(
            Input(acceleration: new Vector2(-100000f, 0f)), 1f / 60f);
        SecondaryMotionFrame released = model.Step(Input(), 1f / 60f);
        SecondaryMotionFrame settled = released;
        for (int index = 0; index < 180; index++) settled = model.Step(Input(), 1f / 60f);

        TestAssert.True(released.Mantle.Position.X > driven.Mantle.Position.X,
            "stored spring velocity carries the mantle forward immediately after drive removal");
        TestAssert.True(Mathf.Abs(settled.Mantle.Position.X) < 0.001f,
            "the unforced mantle spring decays back to rest instead of retaining offset");
    }

    private static void SpringMatchesClosedFormBeforeSteadyState()
    {
        SecondaryMotionFrame frame = new SecondaryMotionModel().Step(
            Input(acceleration: new Vector2(-100000f, 0f)), 0.125f);

        TestAssert.Near(3.1708934f, frame.Mantle.Position.X, 0.0001f,
            "critical spring matches the hand-derived x=12-24/e value at one-eighth second");
    }

    private static void PartitionEquivalenceSurvivesTargetChangeWithVelocity()
    {
        var coarseModel = new SecondaryMotionModel();
        var fineModel = new SecondaryMotionModel();
        SecondaryMotionInput right = Input(acceleration: new Vector2(-100000f, 0f));
        SecondaryMotionInput left = Input(acceleration: new Vector2(100000f, 0f));
        SecondaryMotionFrame coarse = default;
        SecondaryMotionFrame fine = default;

        for (int index = 0; index < 30; index++) coarse = coarseModel.Step(right, 1f / 60f);
        for (int index = 0; index < 30; index++) coarse = coarseModel.Step(left, 1f / 60f);
        for (int index = 0; index < 60; index++) fine = fineModel.Step(right, 1f / 120f);
        for (int index = 0; index < 60; index++) fine = fineModel.Step(left, 1f / 120f);

        AssertNear(coarse.Mantle, fine.Mantle, SecondaryMotionLimits.PartitionTolerance,
            "mantle after target reversal");
    }

    private static void HugeFiniteDeltaSettlesToTargetWithoutResettingOrigin()
    {
        var model = new SecondaryMotionModel();
        SecondaryMotionFrame settled = model.Step(
            Input(acceleration: new Vector2(-100000f, 0f)), float.MaxValue);
        SecondaryMotionFrame released = model.Step(Input(), 1f / 60f);

        TestAssert.Near(SecondaryMotionLimits.MantlePosition.X, settled.Mantle.Position.X,
            0.0001f, "a huge finite delta settles to the finite clamped target");
        TestAssert.Near(11.902357f, released.Mantle.Position.X, 0.0001f,
            "the huge-delta settle stores the target with zero velocity before release decay");
    }

    private static SecondaryMotionInput Input(
        Vector2 velocity = default,
        Vector2 acceleration = default,
        float turn = 0f,
        float landingImpulse = 0f,
        float contactImpulse = 0f,
        PlayerActionPhase threadPhase = PlayerActionPhase.None,
        float threadEnergy = 0f) =>
        new(velocity, acceleration, turn, landingImpulse, contactImpulse, threadPhase, threadEnergy);

    private static void GroundAttackTargetsAreBoundedIndependentAndInertial()
    {
        SecondaryMotionInput Drive(Vector2 value) => Input() with { GroundAttackDrive = value };
        var steady = new SecondaryMotionModel().Step(Drive(Vector2.Right), 5f);
        TestAssert.Near(-3.6f, steady.Lantern.Position.X, .0001f,
            "ground sweep has its own bounded lantern target, not a fake hit");
        TestAssert.Near(-1.98f, steady.Tether.Position.X, .0001f,
            "energy tether inherits the independent lantern target separation");
        TestAssert.Near(-.12f, steady.Lantern.RotationRadians, .0001f,
            "horizontal attack supplies a restrained opposite lantern rotation");
        TestAssert.Equal(PoseTransform2D.Identity, steady.ShoulderSpool,
            "ground swing cannot wind a thread-only spool");
        var up = new SecondaryMotionModel().Step(Drive(Vector2.Up), 5f);
        TestAssert.Near(2.4f, up.Lantern.Position.Y, .0001f,
            "upward ground swing has vertical reaction rather than an arbitrary horizontal shove");
        TestAssert.Near(0f, up.Lantern.Position.X, .0001f,
            "upward ground swing adds no horizontal attack source");

        var invalid = new SecondaryMotionModel().Step(Drive(new(float.NaN, float.PositiveInfinity)), .1f);
        TestAssert.Equal(SecondaryMotionFrame.Identity, invalid,
            "invalid attack source cannot contaminate the original quiet channels");
        var extreme = new SecondaryMotionModel().Step(Drive(new(float.MaxValue, -float.MaxValue)), .1f);
        var clamped = new SecondaryMotionModel().Step(Drive(new(1f, -1f)), .1f);
        TestAssert.Equal(clamped, extreme, "finite attack extremes are clamped before combination");

        var model = new SecondaryMotionModel();
        var first = model.Step(Drive(Vector2.Right), 1f / 60f);
        var coast = model.Step(Input(), 1f / 60f);
        TestAssert.True(coast.Lantern.Position.X < first.Lantern.Position.X,
            "removed ground load retains actual spring momentum instead of snapping to neutral");
        for (int tick = 0; tick < 180; tick++) coast = model.Step(Input(), 1f / 60f);
        TestAssert.True(coast.Lantern.Position.Length() < .001f && coast.Tether.Position.Length() < .001f,
            "ground swing attachments return to rest without a retained attack clock");

        var coarse = new SecondaryMotionModel(); var fine = new SecondaryMotionModel();
        SecondaryMotionFrame a = default, b = default;
        var driven = Input(acceleration: new(-620, 260), contactImpulse: .25f,
            threadPhase: PlayerActionPhase.Contact, threadEnergy: .85f) with { GroundAttackDrive = new(.7f, -.4f) };
        for (int index = 0; index < 60; index++) a = coarse.Step(driven, 1f / 60f);
        for (int index = 0; index < 120; index++) b = fine.Step(driven, 1f / 120f);
        AssertNear(a.Mantle, b.Mantle, SecondaryMotionLimits.PartitionTolerance, "combined ground mantle");
        AssertNear(a.Lantern, b.Lantern, SecondaryMotionLimits.PartitionTolerance, "combined ground lantern");
        AssertNear(a.Tether, b.Tether, SecondaryMotionLimits.PartitionTolerance, "combined ground tether");
        TestAssert.Equal(a, coarse.Step(Drive(Vector2.Left), 0),
            "zero time neither applies a new ground source nor advances existing motion");
        coarse.ReflectHorizontalBasis();
        var reflected = coarse.Step(Input(), 0);
        TestAssert.Near(-a.Lantern.Position.X, reflected.Lantern.Position.X, .0001f,
            "basis reflection also re-expresses attack-driven lantern inertia");
        TestAssert.Near(a.Lantern.Position.Y, reflected.Lantern.Position.Y, .0001f,
            "reflection leaves vertical attack inertia unchanged");
        GD.Print("GROUND_ATTACK_SECONDARY_MODEL_OK bounded=true invalid_closed=true no_spool=true partitions=true zero_delta=true inertia=true human=false");
    }

    private static void AssertBounded(PoseTransform2D value, Vector2 positionLimit,
        float rotationLimit, string channel)
    {
        TestAssert.True(Mathf.Abs(value.Position.X) <= positionLimit.X + 0.0001f &&
            Mathf.Abs(value.Position.Y) <= positionLimit.Y + 0.0001f,
            $"{channel} position stays inside its explicit bounds");
        TestAssert.True(Mathf.Abs(value.RotationRadians) <= rotationLimit + 0.0001f,
            $"{channel} rotation stays inside its explicit bounds");
    }

    private static void AssertNear(PoseTransform2D expected, PoseTransform2D actual,
        float tolerance, string channel)
    {
        TestAssert.Near(expected.Position.X, actual.Position.X, tolerance,
            $"{channel} X is stable across equivalent delta partitions");
        TestAssert.Near(expected.Position.Y, actual.Position.Y, tolerance,
            $"{channel} Y is stable across equivalent delta partitions");
        TestAssert.Near(expected.RotationRadians, actual.RotationRadians, tolerance,
            $"{channel} rotation is stable across equivalent delta partitions");
    }

    private static void AssertFinite(PoseTransform2D value, string channel)
    {
        TestAssert.True(IsFinite(value.Position.X) && IsFinite(value.Position.Y) &&
            IsFinite(value.RotationRadians) && IsFinite(value.Scale.X) && IsFinite(value.Scale.Y) &&
            IsFinite(value.Modulate.R) && IsFinite(value.Modulate.G) &&
            IsFinite(value.Modulate.B) && IsFinite(value.Modulate.A),
            $"{channel} never propagates NaN or infinity");
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
