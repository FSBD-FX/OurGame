using System;
using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static class MechanicalClawModelTests
{
    private const float Frame = 1f / 60f;

    public static void Run()
    {
        AutomaticPullBrakesSidewaysFallBeforeArrival();
        ClickReleaseDuringTravelDoesNotCancel();
        NoInputAfterLatchAutomaticallyShortensTheRope();
        AutoPullDeceleratesInsideOneHundredSixtyPixels();
        AutoPullSpeedAndAccelerationRemainBoundedAcrossDeltaSizes();
        ArrivedTransitionsFromReelToSwingBeforeAnyStallDecision();
        ArrivalTransitionKeepsSwingOutsideTheInstantaneousEnvelope();
        ActiveCastCommandCancelsInsteadOfFailingAlreadyDeployed();
        BoostReleaseProducesOneReleasedFrameWithBoostMetadata();
        MaximumRangeIncludesExactlySixHundredTwentyPixels();
        GrippableMaterialsEnterTravel();
        IceUsesTypedSlipFailure();
        ForbiddenUsesTypedFailure();
        RangeAboveSixHundredTwentyFailsClosed();
        ObstructedGrippableTargetUsesTypedFailure();
        MissingTargetUsesTypedFailure();
        TargetLossAfterLatchFailsThenRetracts();
        TargetIdentityReplacementDuringTravelFailsThenRetracts();
        TargetIdentityReplacementWhileAttachedFailsThenRetracts();
        TargetAnchorAndSurfaceNormalRemainLockedAfterTravelBegins();
        LifecycleTravelsLatchesReelsSwingsReleasesRetractsAndIdles();
        SwingInputProducesOpposedTangentialCorrections();
        RadialCorrectionNeverTeleportsTheCaster();
        ReleasedFramePreservesTangentialInertiaWithoutRadialConstraint();
        ReleaseAtAnchorClearsUndefinedVelocityCorrection();
        TangentialDampingUsesExponentialDeltaScaling();
        ClawContractsContainNoEnergyField();
    }

    // A diagonal rope used to remove only radial fall velocity, leaving sideways
    // gravity untouched. This catches that leak before testing native departure.
    private static void AutomaticPullBrakesSidewaysFallBeforeArrival()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(47UL, new Vector2(400f, -200f));
        MechanicalClawFrame falling = EnterLatched(model, target) with
        {
            CasterWorldPosition = Vector2.Zero,
            CasterVelocity = new Vector2(0f, 600f),
            VelocityCorrection = Vector2.Zero,
            DockPoint = new Vector2(352f, -176f)
        };
        Vector2 tangent = new Vector2(1f, 2f).Normalized();
        MechanicalClawFrame pulled = model.Step(falling,
            Input(dockPoint: falling.DockPoint), target, Frame);
        Vector2 composed = falling.CasterVelocity + pulled.VelocityCorrection;
        TestAssert.True(composed.Dot(tangent) < falling.CasterVelocity.Dot(tangent) - 10f,
            "AutomaticPullBrakesSidewaysFall: diagonal pull must reduce lateral fall, not leave gravity tangent untouched");
        TestAssert.True((composed - falling.CasterVelocity).Length() <= 40.01f,
            "the complete velocity change, including lateral braking, respects acceleration at 60Hz");
        TestAssert.Equal(falling.CasterWorldPosition, pulled.CasterWorldPosition,
            "automatic pull remains a velocity correction, never a position rewrite");
        foreach (float aimAxis in new[] { -1f, 1f })
        {
            MechanicalClawFrame aimed = model.Step(falling,
                Input(swingAxis: aimAxis, dockPoint: falling.DockPoint), target, Frame);
            TestAssert.True(aimed.VelocityCorrection.DistanceTo(pulled.VelocityCorrection) < .001f,
                "direction held for target selection must not add sideways sag before Swing");
        }
    }

    public static void RunArrivalTransitionRegression()
    {
        ArrivalTransitionKeepsSwingOutsideTheInstantaneousEnvelope();
        GD.Print("TASK18_CLAW_SWING_TRANSITION_MODEL_OK");
    }

    private static void ArrivalTransitionKeepsSwingOutsideTheInstantaneousEnvelope()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(46UL, Vector2.Zero) with
        {
            SurfaceNormal = Vector2.Down
        };
        MechanicalClawFrame latched = EnterLatched(model, target) with
        {
            RopeLength = 48f,
            CasterWorldPosition = new Vector2(0f, 48f),
            CasterVelocity = Vector2.Zero
        };
        MechanicalClawFrame reel = model.Step(latched,
            Input(dockPoint: latched.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Reel, reel.State,
            "a never-arrived sample first establishes Reel through the model");
        MechanicalClawFrame arrived = model.Step(reel with { CasterVelocity = Vector2.Zero },
            Input(arrived: true, dockPoint: reel.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Swing, arrived.State,
            "genuine arrival, rather than a fabricated Swing state, establishes the attained phase");
        TestAssert.True(arrived.Arrived, "the establishing model step publishes genuine arrival");

        // Recorded WestAccess frame4→5 sample, translated so its saved anchor is the origin:
        // anchor=(5826.972,860), dock=(5826.972,908), post-motor=(5835.087,912.40247).
        MechanicalClawFrame departure = arrived with
        {
            CasterWorldPosition = new Vector2(8.115f, 52.40247f),
            CasterVelocity = new Vector2(150.3767f, -33.606445f),
            VelocityCorrection = new Vector2(25.376686f, -813.60645f)
        };
        MechanicalClawFrame lateral = model.Step(departure,
            Input(swingAxis: 1f, arrived: false, dockPoint: arrived.DockPoint), target, Frame);
        TestAssert.True(departure.CasterWorldPosition.DistanceTo(arrived.DockPoint) > 8f,
            "the explicit D sample is genuinely outside the instantaneous 8px envelope");
        TestAssert.Equal(MechanicalClawState.Swing, lateral.State,
            "an attained Swing must not return to dockward Reel on lateral departure");
        TestAssert.True(!lateral.Arrived,
            "instantaneous arrival telemetry stays false outside the envelope");
        TestAssert.Equal(arrived.CastId, lateral.CastId, "departure preserves the admitted cast");
        TestAssert.Equal(arrived.LockedTargetRuntimeId, lateral.LockedTargetRuntimeId,
            "departure preserves the locked target identity");
        TestAssert.Equal(arrived.AnchorPoint, lateral.AnchorPoint, "departure preserves the anchor");
        TestAssert.Equal(arrived.DockPoint, lateral.DockPoint, "departure preserves the dock");
        TestAssert.Near(48f, lateral.RopeLength, .001f, "Swing retains its attained rope length");
        // Independently derived anchor-radial/tangent counterfactual for the recorded sample;
        // tolerances account for rounded telemetry, not an exact-bit or model-helper oracle.
        TestAssert.Near(17.92f, lateral.VelocityCorrection.X, .2f,
            "departure keeps the anchor-radial/tangent correction basis");
        TestAssert.Near(-813.79f, lateral.VelocityCorrection.Y, .2f,
            "departure does not discard the upward fall-base cancellation");
        TestAssert.True(lateral.VelocityCorrection.DistanceTo(departure.VelocityCorrection) < 25f,
            "this explicit outside-envelope sample has continuous correction rather than a basis jump");
        Vector2 composed = new Vector2(150f, 780f) + lateral.VelocityCorrection;
        TestAssert.Near(167.92f, composed.X, .2f, "the recorded next motor base composes continuously");
        TestAssert.Near(-33.79f, composed.Y, .2f,
            "the next retained motor base composes without exposing hundreds of px/s downward velocity");

        MechanicalClawFrame neverArrived = model.Step(reel with
            {
                CasterWorldPosition = departure.CasterWorldPosition,
                CasterVelocity = departure.CasterVelocity
            }, Input(swingAxis: 1f, arrived: false, dockPoint: reel.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Reel, neverArrived.State,
            "the paired never-arrived outside-envelope sample remains automatic Reel");
        TestAssert.True(!neverArrived.Arrived, "never-arrived telemetry remains false");
    }

    private static void ClickReleaseDuringTravelDoesNotCancel()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(40UL, new Vector2(520f, 0f));
        MechanicalClawFrame travel = EnterTravel(model, target);

        MechanicalClawFrame afterMouseUp = model.Step(travel,
            Input(dockPoint: travel.DockPoint), target, Frame);

        TestAssert.Equal(MechanicalClawState.Travel, afterMouseUp.State,
            "mouse-up does not cancel a click-owned cast");
    }

    private static void NoInputAfterLatchAutomaticallyShortensTheRope()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(41UL, new Vector2(220f, 0f));
        MechanicalClawFrame attached = EnterLatched(model, target);
        float before = attached.RopeLength;

        MechanicalClawFrame automatic = model.Step(attached,
            Input(dockPoint: attached.DockPoint), target, Frame);

        TestAssert.True(automatic.RopeLength < before,
            "an attached claw takes up rope without W or held LMB");
    }

    private static void AutoPullDeceleratesInsideOneHundredSixtyPixels()
    {
        MechanicalClawSettings settings = MechanicalClawSettings.Default;
        var model = new MechanicalClawModel(settings);
        MechanicalClawTargetSnapshot target = Target(42UL, new Vector2(400f, 0f));
        MechanicalClawFrame attached = EnterLatched(model, target) with
        {
            CasterWorldPosition = new Vector2(252f, 0f),
            CasterVelocity = Vector2.Zero,
            VelocityCorrection = Vector2.Zero,
            DockPoint = new Vector2(352f, 0f),
            RopeLength = 148f
        };

        MechanicalClawFrame decelerating = model.Step(attached,
            Input(dockPoint: attached.DockPoint), target, 1f);
        float speed = decelerating.VelocityCorrection.Dot(
            attached.CasterWorldPosition.DirectionTo(attached.DockPoint));

        TestAssert.Near(450f, speed, .01f,
            "inside 160 pixels the targetward speed scales down with remaining dock distance");
        TestAssert.True(speed < settings.MaxPullSpeed,
            "the arrival zone requests less than maximum pull speed");
    }

    private static void AutoPullSpeedAndAccelerationRemainBoundedAcrossDeltaSizes()
    {
        foreach (int frequency in new[] { 30, 60, 120 })
        {
            float delta = 1f / frequency;
            MechanicalClawSettings settings = MechanicalClawSettings.Default;
            var model = new MechanicalClawModel(settings);
            MechanicalClawTargetSnapshot target = Target((ulong)(100 + frequency), new Vector2(400f, 0f));
            MechanicalClawFrame frame = EnterLatched(model, target) with
            {
                DockPoint = new Vector2(352f, 0f),
                CasterWorldPosition = Vector2.Zero,
                CasterVelocity = Vector2.Zero,
                VelocityCorrection = Vector2.Zero
            };
            Vector2 previousVelocity = Vector2.Zero;
            float maximumObservedSpeed = 0f;
            float maximumObservedAcceleration = 0f;

            for (int tick = 0; tick < frequency * 2; tick++)
            {
                Vector2 casterBefore = frame.CasterWorldPosition;
                bool arrived = MechanicalClawArrivalGeometry.IsArrived(
                    casterBefore, frame.DockPoint, settings.ArrivalTolerance);
                MechanicalClawFrame next = model.Step(frame,
                    Input(arrived: arrived, dockPoint: frame.DockPoint), target, delta);
                TestAssert.Equal(casterBefore, next.CasterWorldPosition,
                    $"the {frequency}Hz model step never writes caster position");

                // The real motor adopts measured velocity as its new free base at
                // Reel→Swing. The test integrator must make that same ownership handoff,
                // not subtract a servo correction whose old base no longer exists.
                Vector2 priorCorrection = frame.State == MechanicalClawState.Reel &&
                    next.State == MechanicalClawState.Swing ? Vector2.Zero : frame.VelocityCorrection;
                Vector2 composedVelocity = frame.CasterVelocity - priorCorrection +
                    next.VelocityCorrection;
                maximumObservedSpeed = Mathf.Max(maximumObservedSpeed, composedVelocity.Length());
                // Measure acceleration in the fixed world frame. The direction to
                // the dock reverses after passing it in Swing, even at constant velocity.
                maximumObservedAcceleration = Mathf.Max(maximumObservedAcceleration,
                    composedVelocity.DistanceTo(previousVelocity) / delta);
                previousVelocity = composedVelocity;
                frame = next with
                {
                    CasterWorldPosition = casterBefore + composedVelocity * delta,
                    CasterVelocity = composedVelocity
                };
            }

            TestAssert.True(maximumObservedSpeed <= settings.MaxPullSpeed + .01f,
                $"{frequency}Hz targetward speed remains at or below 720px/s; actual={maximumObservedSpeed}");
            TestAssert.True(maximumObservedAcceleration <= settings.MaxPullAcceleration + .1f,
                $"{frequency}Hz acceleration remains at or below 2400px/s²; actual={maximumObservedAcceleration}");
            TestAssert.Near(settings.MinimumRopeLength, frame.RopeLength, .001f,
                $"{frequency}Hz automatic take-up reaches the same 48px rope bound");
        }
    }

    private static void ArrivedTransitionsFromReelToSwingBeforeAnyStallDecision()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(43UL, new Vector2(220f, 0f));
        MechanicalClawFrame reel = EnterLatched(model, target) with
        {
            State = MechanicalClawState.Reel,
            RopeLength = 80f,
            CasterWorldPosition = new Vector2(172f, 0f),
            DockPoint = new Vector2(172f, 0f)
        };

        MechanicalClawFrame arrived = model.Step(reel,
            Input(arrived: true, dockPoint: reel.DockPoint), target, Frame);

        TestAssert.Equal(MechanicalClawState.Swing, arrived.State,
            "arrival transitions Reel to Swing before later stall handling exists");
        TestAssert.True(arrived.Arrived, "arrival is retained in the deterministic frame");
        TestAssert.Near(reel.RopeLength, arrived.RopeLength, .0001f,
            "arrival stops automatic rope shortening");
    }

    private static void ActiveCastCommandCancelsInsteadOfFailingAlreadyDeployed()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(44UL, new Vector2(300f, 0f));
        MechanicalClawFrame travel = EnterTravel(model, target);

        MechanicalClawFrame cancelled = model.Step(travel,
            Input(MechanicalClawCommand.Cast, dockPoint: travel.DockPoint), target, Frame);

        TestAssert.Equal(MechanicalClawState.Released, cancelled.State,
            "an active Cast command is the user's cancellation edge");
        TestAssert.Equal(MechanicalClawFailure.None, cancelled.Failure,
            "active Cast cancellation never reports AlreadyDeployed");
        TestAssert.Equal(MechanicalClawReleaseKind.Cancel, cancelled.ReleaseKind,
            "active Cast cancellation carries ordinary release metadata");
    }

    private static void BoostReleaseProducesOneReleasedFrameWithBoostMetadata()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(45UL, new Vector2(220f, 0f));
        MechanicalClawFrame attached = EnterLatched(model, target);

        MechanicalClawFrame released = model.Step(attached,
            Input(MechanicalClawCommand.BoostRelease, dockPoint: attached.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Released, released.State,
            "attached BoostRelease produces a Released frame for the controller transaction");
        TestAssert.Equal(MechanicalClawReleaseKind.Boost, released.ReleaseKind,
            "the Released frame tells the controller to publish the atomic boost event");
        TestAssert.True(released.BoostConsumed,
            "the accepted attached BoostRelease is consumed exactly once");

        MechanicalClawFrame retract = model.Step(released,
            Input(dockPoint: released.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, retract.State,
            "the one Released frame advances to Retract on the next step");
        TestAssert.Equal(MechanicalClawReleaseKind.None, retract.ReleaseKind,
            "boost metadata exists only on the Released transaction frame");

        var earlyModel = new MechanicalClawModel();
        MechanicalClawFrame travel = EnterTravel(earlyModel, target);
        MechanicalClawFrame ignored = earlyModel.Step(travel,
            Input(MechanicalClawCommand.BoostRelease, dockPoint: travel.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Travel, ignored.State,
            "BoostRelease never grants boost during Travel");
    }

    private static void LifecycleTravelsLatchesReelsSwingsReleasesRetractsAndIdles()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(1UL, new Vector2(520f, 0f));
        MechanicalClawFrame frame = MechanicalClawFrame.Idle();

        frame = model.Step(frame, Input(MechanicalClawCommand.Cast), target, Frame);
        TestAssert.Equal(MechanicalClawState.Aim, frame.State, "Cast enters Aim");
        frame = model.Step(frame, Input(), target, Frame);
        TestAssert.Equal(MechanicalClawState.Windup, frame.State, "None advances Aim to Windup");
        frame = model.Step(frame, Input(), target, Frame) with { DockPoint = DockPoint(target) };
        TestAssert.Equal(MechanicalClawState.Travel, frame.State, "None advances Windup to Travel");
        ulong castId = frame.CastId;
        frame = EnterLatchedFromTravel(model, frame, target);

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Reel, frame.State, "automatic take-up enters Reel");
        frame = model.Step(frame, Input(swingAxis: 1f, arrived: true, dockPoint: frame.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Swing, frame.State, "arrival enters Swing");
        frame = model.Step(frame, Input(MechanicalClawCommand.Cancel, dockPoint: frame.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Released, frame.State, "Cancel enters Released");
        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State, "Released enters Retract");
        frame = AdvanceToIdle(model, frame, target);
        TestAssert.Equal(MechanicalClawState.Idle, frame.State, "Retract returns to Idle");
        TestAssert.Equal(castId, frame.NextCastId - 1UL, "one lifecycle allocates one cast identity");
    }

    private static void MaximumRangeIncludesExactlySixHundredTwentyPixels()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(22UL, new Vector2(620f, 0f)));
        TestAssert.Equal(MechanicalClawState.Windup, frame.State,
            "the inclusive 620-pixel boundary remains valid");
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
            MechanicalClawFrame frame = EnterTravel(new MechanicalClawModel(),
                Target(10UL + (ulong)material, new Vector2(120f, 0f), material));
            TestAssert.Equal(MechanicalClawState.Travel, frame.State,
                $"{material} remains a valid mechanical-claw surface");
            TestAssert.Equal(MechanicalClawFailure.None, frame.Failure,
                $"{material} has no failure while the shuttle travels");
        }
    }

    private static void IceUsesTypedSlipFailure()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(20UL, new Vector2(120f, 0f), GrappleSurfaceMaterial.Ice));
        TestAssert.Equal(MechanicalClawFailure.IceSlip, frame.Failure,
            "ice retains its typed slip failure");
    }

    private static void ForbiddenUsesTypedFailure()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(21UL, new Vector2(120f, 0f), GrappleSurfaceMaterial.Forbidden));
        TestAssert.Equal(MechanicalClawFailure.Forbidden, frame.Failure,
            "forbidden terrain retains its typed failure");
    }

    private static void RangeAboveSixHundredTwentyFailsClosed()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(23UL, new Vector2(620.01f, 0f)));
        TestAssert.Equal(MechanicalClawFailure.OutOfRange, frame.Failure,
            "620.01 pixels remains out of range");
    }

    private static void ObstructedGrippableTargetUsesTypedFailure()
    {
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(),
            Target(24UL, new Vector2(120f, 0f), hasLineOfSight: false));
        TestAssert.Equal(MechanicalClawFailure.Obstructed, frame.Failure,
            "an obstructed valid material retains Obstructed rather than NoTarget");
        TestAssert.True(!frame.EffectiveVisibility, "an obstructed target is not effectively visible");
    }

    private static void MissingTargetUsesTypedFailure()
    {
        MechanicalClawTargetSnapshot missing = new(0UL, false, GrappleSurfaceMaterial.GripStone,
            Vector2.Zero, false, false, Vector2.Zero);
        MechanicalClawFrame frame = StartAttempt(new MechanicalClawModel(), missing);
        TestAssert.Equal(MechanicalClawFailure.NoTarget, frame.Failure,
            "a missing target retains NoTarget rather than a material failure");
    }

    private static void TargetLossAfterLatchFailsThenRetracts()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(25UL, new Vector2(120f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);
        MechanicalClawTargetSnapshot lost = target with { Exists = false };

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), lost, Frame);
        TestAssert.Equal(MechanicalClawFailure.TargetLost, frame.Failure,
            "target loss after latch remains distinguishable from initial NoTarget");
        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), lost, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "TargetLost retracts the deployed claw");
    }

    private static void TargetIdentityReplacementDuringTravelFailsThenRetracts()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(251UL, new Vector2(240f, 0f));
        MechanicalClawFrame frame = EnterTravel(model, target);
        MechanicalClawTargetSnapshot replacement = Target(252UL, target.WorldAnchor);

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), replacement, Frame);
        TestAssert.Equal(MechanicalClawFailure.TargetLost, frame.Failure,
            "a different runtime ID fails closed during Travel");
        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), replacement, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "a replaced travel target retracts the deployed claw");
    }

    private static void TargetIdentityReplacementWhileAttachedFailsThenRetracts()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(253UL, new Vector2(160f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target);
        MechanicalClawTargetSnapshot replacement = Target(254UL, target.WorldAnchor);

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), replacement, Frame);
        TestAssert.Equal(MechanicalClawFailure.TargetLost, frame.Failure,
            "a different runtime ID fails closed while attached");
        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), replacement, Frame);
        TestAssert.Equal(MechanicalClawState.Retract, frame.State,
            "a replaced attached target retracts the deployed claw");
    }

    private static void TargetAnchorAndSurfaceNormalRemainLockedAfterTravelBegins()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(26UL, new Vector2(300f, 0f));
        MechanicalClawFrame frame = EnterTravel(model, target);
        Vector2 lockedAnchor = frame.AnchorPoint;
        Vector2 lockedNormal = frame.SurfaceNormal;
        MechanicalClawTargetSnapshot moved = target with
        {
            WorldAnchor = new Vector2(410f, 90f), SurfaceNormal = Vector2.Down
        };

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), moved, Frame);
        TestAssert.Equal(lockedAnchor, frame.AnchorPoint,
            "Travel keeps the admitted anchor instead of chasing the snapshot");
        TestAssert.Equal(lockedNormal, frame.SurfaceNormal,
            "Travel keeps the admitted surface normal instead of changing faces");
        TestAssert.Equal(target.RuntimeId, frame.LockedTargetRuntimeId,
            "Travel keeps the admitted target identity");
    }

    private static void SwingInputProducesOpposedTangentialCorrections()
    {
        MechanicalClawTargetSnapshot target = Target(27UL, new Vector2(220f, 0f));
        var rightModel = new MechanicalClawModel();
        var leftModel = new MechanicalClawModel();
        MechanicalClawFrame right = EnterSwing(rightModel, target);
        MechanicalClawFrame left = EnterSwing(leftModel, target);

        right = rightModel.Step(right,
            Input(swingAxis: 1f, dockPoint: right.DockPoint), target, .1f);
        left = leftModel.Step(left,
            Input(swingAxis: -1f, dockPoint: left.DockPoint), target, .1f);
        TestAssert.True(right.VelocityCorrection.Y > 0f,
            "D produces positive anchor-tangential correction");
        TestAssert.True(left.VelocityCorrection.Y < 0f,
            "A produces the opposing anchor-tangential correction");
    }

    private static void RadialCorrectionNeverTeleportsTheCaster()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(29UL, new Vector2(200f, 0f));
        MechanicalClawFrame frame = EnterLatched(model, target) with
        {
            RopeLength = 100f,
            CasterWorldPosition = Vector2.Zero,
            CasterVelocity = Vector2.Zero
        };
        Vector2 before = frame.CasterWorldPosition;

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), target, .1f);

        TestAssert.Equal(before, frame.CasterWorldPosition,
            "the model returns correction without writing gameplay position");
        TestAssert.True(frame.VelocityCorrection.Dot(before.DirectionTo(frame.DockPoint)) > 0f,
            "the correction points toward the saved dock");
    }

    private static void ReleasedFramePreservesTangentialInertiaWithoutRadialConstraint()
    {
        var model = new MechanicalClawModel();
        MechanicalClawTargetSnapshot target = Target(31UL, new Vector2(200f, 0f));
        MechanicalClawFrame frame = EnterSwing(model, target) with
        {
            CasterWorldPosition = new Vector2(0f, 100f),
            CasterVelocity = Vector2.Zero
        };
        frame = model.Step(frame, Input(swingAxis: 1f, dockPoint: frame.DockPoint), target, .1f);
        Vector2 radial = frame.CasterWorldPosition.DirectionTo(frame.AnchorPoint);
        Vector2 tangent = new(-radial.Y, radial.X);
        float tangentialBefore = frame.VelocityCorrection.Dot(tangent);

        MechanicalClawFrame released = model.Step(frame,
            Input(MechanicalClawCommand.Cancel, dockPoint: frame.DockPoint), target, Frame);

        TestAssert.Near(tangentialBefore, released.VelocityCorrection.Dot(tangent), .001f,
            "ordinary release preserves tangential inertia");
        TestAssert.Near(0f, released.VelocityCorrection.Dot(radial), .001f,
            "ordinary release discards radial pull");
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
        MechanicalClawFrame released = model.Step(frame,
            Input(MechanicalClawCommand.Cancel, dockPoint: frame.DockPoint), target, Frame);
        TestAssert.Equal(Vector2.Zero, released.VelocityCorrection,
            "release at the anchor clears an undefined radial/tangent basis");
    }

    private static void TangentialDampingUsesExponentialDeltaScaling()
    {
        MechanicalClawSettings settings = MechanicalClawSettings.Default;
        var model = new MechanicalClawModel(settings);
        MechanicalClawTargetSnapshot target = Target(30UL, new Vector2(180f, 0f));
        MechanicalClawFrame frame = EnterSwing(model, target);
        frame = model.Step(frame, Input(swingAxis: 1f, dockPoint: frame.DockPoint), target, .1f);
        float before = frame.VelocityCorrection.Y;
        TestAssert.True(Mathf.Abs(before) > 1f, "damping fixture has genuine Swing motion to decay");

        frame = model.Step(frame, Input(dockPoint: frame.DockPoint), target, .1f);

        TestAssert.Near(before * Mathf.Exp(-settings.RadialDamping * .1f),
            frame.VelocityCorrection.Y, .001f,
            "anchor-tangential damping remains exponential across delta sizes");
    }

    private static void ClawContractsContainNoEnergyField()
    {
        foreach (Type type in new[] { typeof(MechanicalClawInput), typeof(MechanicalClawFrame) })
        foreach (var property in type.GetProperties())
            TestAssert.True(!property.Name.Contains("Energy", StringComparison.OrdinalIgnoreCase),
                $"{type.Name} remains independent of energy");
    }

    private static MechanicalClawFrame EnterSwing(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target)
    {
        MechanicalClawFrame latched = EnterLatched(model, target);
        latched = latched with { CasterWorldPosition = latched.DockPoint };
        return model.Step(latched, Input(arrived: true, dockPoint: latched.DockPoint), target, Frame);
    }

    private static MechanicalClawFrame EnterTravel(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target)
    {
        MechanicalClawFrame frame = MechanicalClawFrame.Idle();
        frame = model.Step(frame, Input(MechanicalClawCommand.Cast), target, Frame);
        frame = model.Step(frame, Input(), target, Frame);
        frame = model.Step(frame, Input(), target, Frame) with { DockPoint = DockPoint(target) };
        TestAssert.Equal(MechanicalClawState.Travel, frame.State, "test setup enters Travel");
        return frame;
    }

    private static MechanicalClawFrame EnterLatched(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target) =>
        EnterLatchedFromTravel(model, EnterTravel(model, target), target);

    private static MechanicalClawFrame EnterLatchedFromTravel(MechanicalClawModel model,
        MechanicalClawFrame frame, MechanicalClawTargetSnapshot target)
    {
        for (int index = 0; index < 120 && frame.State == MechanicalClawState.Travel; index++)
            frame = model.Step(frame, Input(dockPoint: frame.DockPoint), target, Frame);
        TestAssert.Equal(MechanicalClawState.Latched, frame.State, "test setup reaches Latched");
        return frame;
    }

    private static MechanicalClawFrame StartAttempt(MechanicalClawModel model,
        MechanicalClawTargetSnapshot target)
    {
        MechanicalClawFrame frame = model.Step(MechanicalClawFrame.Idle(),
            Input(MechanicalClawCommand.Cast), target, Frame);
        return model.Step(frame, Input(), target, Frame);
    }

    private static MechanicalClawFrame AdvanceToIdle(MechanicalClawModel model,
        MechanicalClawFrame frame, MechanicalClawTargetSnapshot target)
    {
        for (int index = 0; index < 120 && frame.State != MechanicalClawState.Idle; index++)
            frame = model.Step(frame, Input(dockPoint: frame.DockPoint), target, Frame);
        return frame;
    }

    private static MechanicalClawInput Input(
        MechanicalClawCommand command = MechanicalClawCommand.None,
        float swingAxis = 0f,
        bool arrived = false,
        Vector2 dockPoint = default) => new(command, swingAxis, arrived, dockPoint);

    private static Vector2 DockPoint(MechanicalClawTargetSnapshot target) =>
        target.WorldAnchor + target.SurfaceNormal.Normalized() *
        MechanicalClawSettings.Default.MinimumRopeLength;

    private static MechanicalClawTargetSnapshot Target(ulong id, Vector2 anchor,
        GrappleSurfaceMaterial material = GrappleSurfaceMaterial.GripStone,
        bool hasLineOfSight = true) =>
        new(id, true, material, anchor, hasLineOfSight, false,
            anchor.DirectionTo(Vector2.Zero));
}
