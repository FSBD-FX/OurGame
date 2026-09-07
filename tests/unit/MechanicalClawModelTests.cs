using System;
using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static class MechanicalClawModelTests
{
    private const float Frame = 1f / 60f;

    public static void Run()
    {
        ReleasedFramePreservesTangentialInertiaWithoutRadialConstraint();
        ReleaseAtAnchorClearsUndefinedVelocityCorrection();
        TargetIdentityReplacementDuringTravelFailsThenRetracts();
        TargetIdentityReplacementWhileAttachedFailsThenRetracts();
        AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity();
        SecondSpaceOnTheSameAirborneLatchDoesNotAddVelocity();
        GroundContactOrRelatchRestoresExactlyOneBoost();
        ShieldedSpaceDoesNotBoostOrConsumeTheBoost();
        MaximumRangeIncludesExactlySixHundredTwentyPixels();
        LifecycleTravelsLatchesSwingsReelsReleasesRetractsAndIdles();
        SecondPrimaryPressBeforeFullReturnFailsAsAlreadyDeployed();
        GrippableMaterialsEnterTravel();
        IceUsesTypedSlipFailure();
        ForbiddenUsesTypedFailure();
        RangeAboveSixHundredTwentyFailsClosed();
        ObstructedGrippableTargetUsesTypedFailure();
        MissingTargetUsesTypedFailure();
        TargetLossAfterLatchFailsThenRetracts();
        TargetAnchorRemainsLockedAfterTravelBegins();
        SwingInputProducesOpposedTangentialCorrections();
        RopeReelRespectsBoundsAndShieldPause();
        RadialConstraintReturnsVelocityWithoutTeleportingTheCaster();
        TangentialDampingUsesExponentialDeltaScaling();
        ClawContractsContainNoEnergyField();
    }

    private static void LifecycleTravelsLatchesSwingsReelsReleasesRetractsAndIdles()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(1UL, new Vector2(520f, 0f));
        MechanicalClawFrame frame = MechanicalClawFrame.Idle();

        frame = model.Step(frame, Input(pressed: true, held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Aim, frame.State,
            "lifecycle enters Aim on the primary press");
        TestAssert.True(frame.EffectiveVisibility, "Aim publishes effective target visibility");

        frame = model.Step(frame, Input(held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Windup, frame.State,
            "lifecycle enters Windup after Aim");
        TestAssert.True(frame.CastId != 0UL, "Windup allocates the live claw cast identity");
        ulong castId = frame.CastId;

        frame = model.Step(frame, Input(held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Travel, frame.State,
            "lifecycle enters Travel after Windup");

        int travelFrames = 1;
        while (frame.State == MechanicalClawState.Travel)
        {
            frame = model.Step(frame, Input(held: true), target, Frame);
            if (frame.State == MechanicalClawState.Travel) travelFrames++;
        }

        TestAssert.True(travelFrames >= 2,
            "a 520-pixel cast remains in Travel for at least two distinct physics steps");
        TestAssert.Equal(MechanicalClawState.Latched, frame.State,
            "Travel reaches Latched only after shuttle progress completes");
        TestAssert.Equal(castId, frame.CastId, "the live claw retains one cast identity through latch");

        frame = model.Step(frame, Input(held: true, swingAxis: 1f), target, Frame);
        TestAssert.Equal(MechanicalClawState.Swing, frame.State,
            "tangential input enters Swing after latch");

        frame = model.Step(frame, Input(held: true, reelAxis: -1f), target, Frame);
        TestAssert.Equal(MechanicalClawState.Reel, frame.State,
            "W-axis rope input enters Reel after swing");
        TestAssert.Equal(castId, frame.CastId, "reeling does not allocate another live claw identity");

        Vector2 releaseRadialDirection = frame.CasterWorldPosition.DirectionTo(frame.AnchorPoint);
        Vector2 releaseTangentDirection = new(-releaseRadialDirection.Y, releaseRadialDirection.X);
        float releasedTangentialInertia = frame.VelocityCorrection.Dot(releaseTangentDirection);
        frame = model.Step(frame, Input(released: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Released, frame.State,
            "releasing primary enters Released before retracting");
        TestAssert.Near(releasedTangentialInertia, frame.VelocityCorrection.Dot(releaseTangentDirection), .001f,
            "release preserves the accumulated tangential velocity correction");
        TestAssert.Near(0f, frame.VelocityCorrection.Dot(releaseRadialDirection), .001f,
            "release discards the radial rope constraint correction");

        frame = model.Step(frame, Input(), target, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "Released enters the observable Retract state");
        TestAssert.Equal(castId, frame.CastId, "Retract retains the active cast until the claw is home");

        frame = AdvanceToIdle(model, frame, target);
        TestAssert.Equal(MechanicalClawState.Idle, frame.State,
            "Retract returns the claw to Idle only after full return");
        TestAssert.Equal(0UL, frame.CastId, "Idle clears the completed claw identity");
    }

    private static void SecondPrimaryPressBeforeFullReturnFailsAsAlreadyDeployed()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(2UL, new Vector2(220f, 0f));
        MechanicalClawFrame frame = EnterTravel(model, target);
        ulong liveCast = frame.CastId;

        frame = model.Step(frame, Input(pressed: true, held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Failed, frame.State,
            "SecondPrimaryPressBeforeFullReturnFailsAsAlreadyDeployed observes second deployment feedback");
        TestAssert.Equal(MechanicalClawFailure.AlreadyDeployed, frame.Failure,
            "SecondPrimaryPressBeforeFullReturnFailsAsAlreadyDeployed preserves the typed guard failure");
        TestAssert.Equal(liveCast, frame.CastId,
            "second deployment cannot allocate a second cast identity while the first claw is live");

        frame = model.Step(frame, Input(), target, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "failed second deployment returns the original live claw through retract");
    }

    private static void GrippableMaterialsEnterTravel()
    {
        foreach (GrappleSurfaceMaterial material in new[]
                 {
                     GrappleSurfaceMaterial.GripStone,
                     GrappleSurfaceMaterial.TempleMetal,
                     GrappleSurfaceMaterial.DedicatedAnchor
                 })
        {
            var model = new MechanicalClawModel();
            MechanicalClawFrame frame = EnterTravel(model, Target(10UL + (ulong)material,
                new Vector2(120f, 0f), material));
            TestAssert.Equal(MechanicalClawState.Travel, frame.State,
                $"{material} is a valid surface for a mechanical claw cast");
            TestAssert.Equal(MechanicalClawFailure.None, frame.Failure,
                $"{material} has no failure while the shuttle travels");
        }
    }

    private static void IceUsesTypedSlipFailure()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(20UL, new Vector2(120f, 0f), GrappleSurfaceMaterial.Ice));

        TestAssert.Equal(MechanicalClawState.Failed, frame.State,
            "IceUsesTypedSlipFailure observes the ice target failure state");
        TestAssert.Equal(MechanicalClawFailure.IceSlip, frame.Failure,
            "IceUsesTypedSlipFailure keeps ice separate from other invalid surfaces");
    }

    private static void ForbiddenUsesTypedFailure()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(21UL, new Vector2(120f, 0f), GrappleSurfaceMaterial.Forbidden));

        TestAssert.Equal(MechanicalClawFailure.Forbidden, frame.Failure,
            "forbidden terrain has its own explicit no-grapple failure");
    }

    private static void AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(220UL, new Vector2(220f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);
        ulong castId = frame.CastId;
        ulong targetId = frame.LockedTargetRuntimeId;
        Vector2 before = frame.VelocityCorrection;

        MechanicalClawFrame boosted = model.Step(frame,
            Input(held: true, boostPressed: true), target, Frame);

        TestAssert.Equal(MechanicalClawState.Boost, boosted.State,
            "AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity enters Boost while attached");
        TestAssert.Equal(castId, boosted.CastId,
            "AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity keeps the same cast latch");
        TestAssert.Equal(targetId, boosted.LockedTargetRuntimeId,
            "AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity keeps the same target latch");
        TestAssert.True(boosted.VelocityCorrection.Y < before.Y,
            "AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity adds upward velocity");
        TestAssert.True(boosted.BoostConsumed,
            "AttachedSpaceBoostKeepsTheSameLatchAndAddsUpwardTangentVelocity consumes the one-shot boost");
    }

    private static void SecondSpaceOnTheSameAirborneLatchDoesNotAddVelocity()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(221UL, new Vector2(220f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);

        frame = model.Step(frame, Input(held: true, boostPressed: true), target, Frame);
        Vector2 afterFirst = frame.VelocityCorrection;
        frame = model.Step(frame, Input(held: true, boostPressed: true), target, 0f);

        TestAssert.Equal(afterFirst, frame.VelocityCorrection,
            "SecondSpaceOnTheSameAirborneLatchDoesNotAddVelocity rejects a repeated Space edge");
        TestAssert.True(frame.BoostConsumed,
            "SecondSpaceOnTheSameAirborneLatchDoesNotAddVelocity keeps the consumed latch set");
    }

    private static void GroundContactOrRelatchRestoresExactlyOneBoost()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(222UL, new Vector2(220f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);

        frame = model.Step(frame, Input(held: true, boostPressed: true), target, Frame);
        Vector2 afterFirst = frame.VelocityCorrection;
        frame = model.Step(frame, Input(held: true, boostPressed: true, grounded: true), target, 0f);
        Vector2 afterGroundedReset = frame.VelocityCorrection;
        frame = model.Step(frame, Input(held: true, boostPressed: true), target, 0f);

        TestAssert.True(afterGroundedReset.Y < afterFirst.Y,
            "GroundContactOrRelatchRestoresExactlyOneBoost accepts one boost on grounded contact");
        TestAssert.Equal(afterGroundedReset, frame.VelocityCorrection,
            "GroundContactOrRelatchRestoresExactlyOneBoost rejects a second airborne boost after contact");

        frame = model.Step(frame, Input(released: true), target, Frame);
        frame = model.Step(frame, Input(), target, Frame);
        frame = AdvanceToIdle(model, frame, target);
        frame = model.Step(frame, Input(pressed: true, held: true), target, Frame);
        frame = model.Step(frame, Input(held: true), target, Frame);
        frame = model.Step(frame, Input(held: true), target, Frame);
        frame = EnterLatchedFromTravel(model, frame, target);
        MechanicalClawFrame relatchBoost = model.Step(frame,
            Input(held: true, boostPressed: true), target, Frame);

        TestAssert.Equal(MechanicalClawState.Boost, relatchBoost.State,
            "GroundContactOrRelatchRestoresExactlyOneBoost accepts one boost on a new cast");
    }

    private static void ShieldedSpaceDoesNotBoostOrConsumeTheBoost()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(223UL, new Vector2(220f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);

        frame = model.Step(frame, Input(held: true, boostPressed: true, shieldActive: true), target, 0f);
        TestAssert.Equal(MechanicalClawState.Swing, frame.State,
            "ShieldedSpaceDoesNotBoostOrConsumeTheBoost leaves the attached state unchanged");
        TestAssert.True(!frame.BoostConsumed,
            "ShieldedSpaceDoesNotBoostOrConsumeTheBoost does not consume a shielded Space edge");

        frame = model.Step(frame, Input(held: true, boostPressed: true), target, 0f);
        TestAssert.Equal(MechanicalClawState.Boost, frame.State,
            "ShieldedSpaceDoesNotBoostOrConsumeTheBoost accepts a later unshielded Space edge");
    }

    private static void MaximumRangeIncludesExactlySixHundredTwentyPixels()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(22UL, new Vector2(620f, 0f)));

        TestAssert.Equal(MechanicalClawState.Windup, frame.State,
            "MaximumRangeIncludesExactlySixHundredTwentyPixels accepts the inclusive 620-pixel boundary");
    }

    private static void RangeAboveSixHundredTwentyFailsClosed()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(23UL, new Vector2(620.01f, 0f)));

        TestAssert.Equal(MechanicalClawFailure.OutOfRange, frame.Failure,
            "620.01 pixels produces the distinct OutOfRange failure");
    }

    private static void ObstructedGrippableTargetUsesTypedFailure()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(24UL, new Vector2(120f, 0f), hasLineOfSight: false));

        TestAssert.Equal(MechanicalClawFailure.Obstructed, frame.Failure,
            "a valid material behind an obstruction reports Obstructed instead of NoTarget");
        TestAssert.True(!frame.EffectiveVisibility,
            "an obstructed target is not effectively visible to the model output");
    }

    private static void MissingTargetUsesTypedFailure()
    {
        MechanicalClawTargetSnapshot missing = new(0UL, false, GrappleSurfaceMaterial.GripStone,
            Vector2.Zero, false, false);
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(), missing);

        TestAssert.Equal(MechanicalClawFailure.NoTarget, frame.Failure,
            "a missing target produces NoTarget rather than a material failure");
    }

    private static void TargetLossAfterLatchFailsThenRetracts()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(25UL, new Vector2(120f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);

        MechanicalClawTargetSnapshot lost = target with { Exists = false };
        frame = model.Step(frame, Input(held: true), lost, Frame);
        TestAssert.Equal(MechanicalClawState.Failed, frame.State,
            "target loss after a latch publishes a failure before retracting");
        TestAssert.Equal(MechanicalClawFailure.TargetLost, frame.Failure,
            "target loss after a latch remains distinguishable from initial NoTarget");

        frame = model.Step(frame, Input(), lost, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "TargetLost state moves the deployed claw into Retract");
    }

    private static void TargetIdentityReplacementDuringTravelFailsThenRetracts()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(251UL, new Vector2(240f, 0f));
        MechanicalClawFrame frame = EnterTravel(model, target);
        MechanicalClawTargetSnapshot replacement = Target(252UL, target.WorldAnchor);

        frame = model.Step(frame, Input(held: true), replacement, Frame);
        TestAssert.Equal(MechanicalClawState.Failed, frame.State,
            "TargetIdentityReplacementDuringTravelFailsThenRetracts exposes a failed state");
        TestAssert.Equal(MechanicalClawFailure.TargetLost, frame.Failure,
            "TargetIdentityReplacementDuringTravelFailsThenRetracts fails closed for a different live snapshot ID");

        frame = model.Step(frame, Input(), replacement, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "a replaced travel target retracts the already-deployed claw");
    }

    private static void TargetIdentityReplacementWhileAttachedFailsThenRetracts()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(253UL, new Vector2(160f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);
        MechanicalClawTargetSnapshot replacement = Target(254UL, target.WorldAnchor);

        frame = model.Step(frame, Input(held: true), replacement, Frame);
        TestAssert.Equal(MechanicalClawState.Failed, frame.State,
            "TargetIdentityReplacementWhileAttachedFailsThenRetracts exposes a failed state");
        TestAssert.Equal(MechanicalClawFailure.TargetLost, frame.Failure,
            "TargetIdentityReplacementWhileAttachedFailsThenRetracts fails closed for a different attached snapshot ID");

        frame = model.Step(frame, Input(), replacement, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "a replaced attached target retracts the live rope connection");
    }

    private static void TargetAnchorRemainsLockedAfterTravelBegins()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(26UL, new Vector2(300f, 0f));
        MechanicalClawFrame frame = EnterTravel(model, target);
        Vector2 lockedAnchor = frame.AnchorPoint;

        MechanicalClawTargetSnapshot movedSnapshot = target with { WorldAnchor = new Vector2(410f, 90f) };
        frame = model.Step(frame, Input(held: true), movedSnapshot, Frame);

        TestAssert.Equal(lockedAnchor, frame.AnchorPoint,
            "Travel keeps the anchor locked instead of chasing changing snapshot coordinates");
        TestAssert.Equal(target.RuntimeId, frame.LockedTargetRuntimeId,
            "Travel retains the target identity locked at entry");
    }

    private static void SwingInputProducesOpposedTangentialCorrections()
    {
        MechanicalClawTargetSnapshot target = Target(27UL, new Vector2(220f, 0f));
        var rightModel = new MechanicalClawModel();
        var leftModel = new MechanicalClawModel();
        MechanicalClawFrame right = EnterLatched(rightModel, target);
        MechanicalClawFrame left = EnterLatched(leftModel, target);

        right = rightModel.Step(right, Input(held: true, swingAxis: 1f), target, .1f);
        left = leftModel.Step(left, Input(held: true, swingAxis: -1f), target, .1f);

        TestAssert.True(right.VelocityCorrection.Y > 0f,
            "D-axis tangential input accelerates perpendicular to the rope in one direction");
        TestAssert.True(left.VelocityCorrection.Y < 0f,
            "A-axis tangential input accelerates perpendicular to the rope in the opposing direction");
    }

    private static void RopeReelRespectsBoundsAndShieldPause()
    {
        MechanicalClawSettings settings = MechanicalClawSettings.Default;
        var model = new MechanicalClawModel(settings);
        MechanicalClawTargetSnapshot target = Target(28UL, new Vector2(420f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);

        float initialLength = frame.RopeLength;
        frame = model.Step(frame, Input(held: true, reelAxis: -1f), target, .1f);
        TestAssert.True(frame.RopeLength < initialLength,
            "W-axis reel input shortens the rope at the centralized reel speed");

        float shortenedLength = frame.RopeLength;
        frame = model.Step(frame, Input(held: true, reelAxis: 1f), target, .1f);
        TestAssert.True(frame.RopeLength > shortenedLength,
            "S-axis reel input lengthens the rope at the centralized reel speed");

        float beforeShield = frame.RopeLength;
        frame = model.Step(frame, Input(held: true, reelAxis: -1f, shieldActive: true), target, 1f);
        TestAssert.Near(beforeShield, frame.RopeLength, .0001f,
            "shield pauses reeling while preserving the current rope connection length");
        TestAssert.True(frame.State is MechanicalClawState.Swing or MechanicalClawState.Reel,
            "shield pause keeps the latched rope connection active");

        frame = model.Step(frame, Input(held: true, reelAxis: -1f), target, 100f);
        TestAssert.Near(settings.MinimumRopeLength, frame.RopeLength, .0001f,
            "reeling cannot shorten below the centralized minimum rope length");

        frame = model.Step(frame, Input(held: true, reelAxis: 1f), target, 100f);
        TestAssert.Near(settings.MaxRange, frame.RopeLength, .0001f,
            "paying out rope cannot exceed the centralized maximum range");
    }

    private static void RadialConstraintReturnsVelocityWithoutTeleportingTheCaster()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(29UL, new Vector2(200f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target) with
        {
            CasterWorldPosition = new Vector2(0f, 100f),
            CasterVelocity = Vector2.Zero
        };

        Vector2 casterBefore = frame.CasterWorldPosition;
        frame = model.Step(frame, Input(held: true), target, .1f);
        Vector2 towardAnchor = casterBefore.DirectionTo(frame.AnchorPoint);

        TestAssert.Equal(casterBefore, frame.CasterWorldPosition,
            "the pure rope model never teleports the caster position");
        TestAssert.True(frame.VelocityCorrection.Dot(towardAnchor) > 0f,
            "radial rope stretch returns a velocity correction toward the anchor");
    }

    private static void ReleasedFramePreservesTangentialInertiaWithoutRadialConstraint()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(31UL, new Vector2(200f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target) with
        {
            CasterWorldPosition = new Vector2(0f, 100f),
            CasterVelocity = Vector2.Zero
        };
        frame = model.Step(frame, Input(held: true, swingAxis: 1f), target, .1f);

        Vector2 radialDirection = frame.CasterWorldPosition.DirectionTo(frame.AnchorPoint);
        Vector2 tangentDirection = new(-radialDirection.Y, radialDirection.X);
        float radialBeforeRelease = frame.VelocityCorrection.Dot(radialDirection);
        float tangentialBeforeRelease = frame.VelocityCorrection.Dot(tangentDirection);
        TestAssert.True(radialBeforeRelease > 0f && Mathf.Abs(tangentialBeforeRelease) > 0f,
            "test setup creates both a radial constraint and tangential swing correction");

        MechanicalClawFrame released = model.Step(frame, Input(released: true), target, Frame);

        TestAssert.Equal(MechanicalClawState.Released, released.State,
            "primary release enters Released before retracting");
        TestAssert.Near(tangentialBeforeRelease, released.VelocityCorrection.Dot(tangentDirection), .001f,
            "ReleasedFramePreservesTangentialInertiaWithoutRadialConstraint preserves tangential inertia");
        TestAssert.Near(0f, released.VelocityCorrection.Dot(radialDirection), .001f,
            "ReleasedFramePreservesTangentialInertiaWithoutRadialConstraint discards radial rope pull");
    }

    private static void ReleaseAtAnchorClearsUndefinedVelocityCorrection()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(32UL, new Vector2(180f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target) with
        {
            CasterWorldPosition = target.WorldAnchor,
            VelocityCorrection = new Vector2(45f, -30f)
        };

        MechanicalClawFrame released = model.Step(frame, Input(released: true), target, Frame);

        TestAssert.Equal(Vector2.Zero, released.VelocityCorrection,
            "release at a zero-length rope safely clears the undefined radial and tangent basis");
    }

    private static void TangentialDampingUsesExponentialDeltaScaling()
    {
        MechanicalClawSettings settings = MechanicalClawSettings.Default;
        var model = new MechanicalClawModel(settings);
        MechanicalClawTargetSnapshot target = Target(30UL, new Vector2(180f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);
        frame = model.Step(frame, Input(held: true, swingAxis: 1f), target, .1f);
        Vector2 beforeDamping = frame.VelocityCorrection;

        frame = model.Step(frame, Input(held: true), target, .1f);
        float expectedScale = Mathf.Exp(-settings.RadialDamping * .1f);

        TestAssert.Near(beforeDamping.Y * expectedScale, frame.VelocityCorrection.Y, .001f,
            "tangential velocity damping is exponential and independent of a fixed frame count");
    }

    private static void ClawContractsContainNoEnergyField()
    {
        foreach (Type type in new[] { typeof(MechanicalClawInput), typeof(MechanicalClawFrame) })
        {
            foreach (var property in type.GetProperties())
            {
                TestAssert.True(!property.Name.Contains("Energy", StringComparison.OrdinalIgnoreCase),
                    $"{type.Name} contains no energy field because claw behavior never consumes energy");
            }
        }
    }

    private static MechanicalClawFrame EnterTravel(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target)
    {
        MechanicalClawFrame frame = MechanicalClawFrame.Idle();
        frame = model.Step(frame, Input(pressed: true, held: true), target, Frame);
        frame = model.Step(frame, Input(held: true), target, Frame);
        frame = model.Step(frame, Input(held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Travel, frame.State, "test setup enters Travel");
        return frame;
    }

    private static MechanicalClawFrame EnterLatched(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target)
    {
        MechanicalClawFrame frame = EnterTravel(model, target);
        for (int index = 0; index < 120 && frame.State == MechanicalClawState.Travel; index++)
            frame = model.Step(frame, Input(held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Latched, frame.State, "test setup reaches Latched");
        return frame;
    }

    private static MechanicalClawFrame EnterLatchedFromTravel(MechanicalClawModel model,
        MechanicalClawFrame frame, MechanicalClawTargetSnapshot target)
    {
        for (int index = 0; index < 120 && frame.State == MechanicalClawState.Travel; index++)
            frame = model.Step(frame, Input(held: true), target, Frame);
        TestAssert.Equal(MechanicalClawState.Latched, frame.State, "test setup reaches Latched from a new cast");
        return frame;
    }

    private static MechanicalClawFrame StartAttempt(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target)
    {
        MechanicalClawFrame frame = MechanicalClawFrame.Idle();
        frame = model.Step(frame, Input(pressed: true, held: true), target, Frame);
        return model.Step(frame, Input(held: true), target, Frame);
    }

    private static MechanicalClawFrame AdvanceToIdle(MechanicalClawModel model,
        MechanicalClawFrame frame, MechanicalClawTargetSnapshot target)
    {
        for (int index = 0; index < 120 && frame.State != MechanicalClawState.Idle; index++)
            frame = model.Step(frame, Input(), target, Frame);
        return frame;
    }

    private static MechanicalClawInput Input(bool pressed = false, bool held = false, bool released = false,
        bool detachPressed = false, float swingAxis = 0f, float reelAxis = 0f, bool shieldActive = false,
        bool boostPressed = false, bool grounded = false) =>
        new(pressed, held, released, detachPressed, swingAxis, reelAxis, shieldActive, boostPressed, grounded);

    private static MechanicalClawTargetSnapshot Target(ulong id, Vector2 anchor,
        GrappleSurfaceMaterial material = GrappleSurfaceMaterial.GripStone, bool hasLineOfSight = true) =>
        new(id, true, material, anchor, hasLineOfSight, false);
}
