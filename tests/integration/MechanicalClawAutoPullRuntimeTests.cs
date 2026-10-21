using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Integration;

public static class MechanicalClawAutoPullRuntimeTests
{
    // Separate evidence collection, never a replacement for the original six-sample assertion.
    public static async Task RunCeilingNeutralObservation(SceneTree tree)
    {
        await WithPhysicalSurfaceFixture(tree, "CeilingDockObservation", new Vector2(0f, -220f),
            new Vector2(260f, 40f), Vector2.Zero, new Vector2(0f, 14f), true, async fixture =>
        {
            var observer = new CeilingDockNeutralObserver
            {
                Name = "CeilingDockNeutralObserver",
                ProcessPhysicsPriority = 100,
                Player = fixture.Player,
                InputReader = fixture.Input,
                Claw = fixture.Claw,
                Motor = fixture.Motor,
                Target = fixture.Surface
            };
            TestAssert.True(observer.ProcessPhysicsPriority > fixture.Motor.ProcessPhysicsPriority,
                "ceiling observation executes after the unmodified saved motor");
            fixture.Root.AddChild(observer);
            observer.Subscribe();
            try
            {
                await SelectGrapple(tree, fixture);
                await ClickForOneFrame(tree, fixture);
                await WaitForClaw(tree, fixture, IsAttached, "ceiling diagnostic attached pull");
                await WaitFor(tree, fixture, () => observer.Started || observer.Failure != null,
                    "ceiling diagnostic genuine Reel-to-Swing arrival");
                observer.ThrowFailure();
                // The original bounded waiter only waits. The post-motor callback, not await
                // iterations, owns the sample count and freezes exactly 120 consecutive ticks.
                await WaitFor(tree, fixture, () => observer.Complete || observer.Failure != null,
                    "ceiling diagnostic 120 actual neutral physics samples");
                observer.ThrowFailure();
                observer.PrintSummary();
            }
            finally { observer.Unsubscribe(); }
        });
    }

    public static async Task RunContactSafetyRegression(SceneTree tree)
    {
        await AutoPullStopsAtWallDockWithoutOverlapOrJitter(tree);
        await AutoPullStopsBelowCeilingWithoutOverlapOrJitter(tree);
        await RunObstructionSafetyRegression(tree);
    }

    public static async Task RunObstructionSafetyRegression(SceneTree tree)
    {
        await ThinWorldBlockerBetweenCasterAndTargetCannotBeCrossed(tree);
        await InsertedBlockerDuringPullReleasesAsObstructed(tree);
        await CapsuleFlankBlockerWithClearLineOfSightReleasesBeforeContact(tree);
        await EarlyCollisionWithAnotherFaceOfTheTargetIsObstructed(tree);
        await NoProgressForQuarterSecondReleasesOnceAndClearsCorrection(tree);
        await ExplicitReleaseIsNotReclassifiedByPullSweep(tree);
        await ArrivedConstraintStillReleasesOnContinuingLosLoss(tree);
    }

    public static async Task Run(SceneTree tree)
    {
        await ClickForOneFrameThenMouseUpStillLatchesAndPullsWithoutW(tree);
        await SecondClickCancelsAndAThirdClickCanStartANewCast(tree);
        await PostSampleTargetUnloadConsumesPrimaryBeforeClawAndFreshClickRecovers(tree);
        await ResolverTargetLossBeatsSameFramePrimaryAndFreshClickRecovers(tree);
        await QAndPrimaryConsumesPrimaryWithoutAttackOrCast(tree);
        await ShieldAndSpaceCancelsWithoutBoostOrJump(tree);
        await UnavailableShieldAndSpaceStillCancelsWithoutJump(tree);
        await DashAndSpaceCancelsThenUsesOnlyTheExistingDash(tree);
        await AttachedSpacePublishesBoostThenReleasedAndMovesTheMotorOnce(tree);
        await BoostObserverResetPreemptsStaleReleaseMotorApplication(tree);
        await TravelSpaceRemainsAnOrdinaryJumpAndGrantsNoBoostRelease(tree);
        await AutoPullStopsAtWallDockWithoutOverlapOrJitter(tree);
        await AutoPullStopsBelowCeilingWithoutOverlapOrJitter(tree);
        await ArrivedCeilingSwingSurvivesLateralDepartureThenSpaceBoostsOnce(tree);
        await AutoPullStopsOnTheSavedFaceOfAConvexEdge(tree);
        await ThinWorldBlockerBetweenCasterAndTargetCannotBeCrossed(tree);
        await InsertedBlockerDuringPullReleasesAsObstructed(tree);
        await CapsuleFlankBlockerWithClearLineOfSightReleasesBeforeContact(tree);
        await EarlyCollisionWithAnotherFaceOfTheTargetIsObstructed(tree);
        await ReachedDockWinsBeforeTheNoProgressTimeout(tree);
        await NoProgressForQuarterSecondReleasesOnceAndClearsCorrection(tree);
        await SubpixelDockProgressAccumulatesBeforeStallTimeout(tree);
        await ExplicitReleaseIsNotReclassifiedByPullSweep(tree);
        await PauseDoesNotAdvanceTheNoProgressClock(tree);
        await ArrivedConstraintStillReleasesOnContinuingLosLoss(tree);
        await FourSecondFallCanClickAVisibleWallAndBrakeContinuously(tree);
        GD.Print("MECHANICAL_CLAW_AUTO_PULL_RUNTIME_OK");
    }

    public static async Task RunArrivalTransitionRegression(SceneTree tree)
    {
        await ArrivedCeilingSwingSurvivesLateralDepartureThenSpaceBoostsOnce(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_LATERAL_OK");
        await AutoPullStopsBelowCeilingWithoutOverlapOrJitter(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_SAFETY_OK case=AutoPullStopsBelowCeilingWithoutOverlapOrJitter");
        await FourSecondFallCanClickAVisibleWallAndBrakeContinuously(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_SAFETY_OK case=FourSecondFallCanClickAVisibleWallAndBrakeContinuously");
        await ArrivedConstraintStillReleasesOnContinuingLosLoss(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_SAFETY_OK case=ArrivedConstraintStillReleasesOnContinuingLosLoss");
        await EarlyCollisionWithAnotherFaceOfTheTargetIsObstructed(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_SAFETY_OK case=EarlyCollisionWithAnotherFaceOfTheTargetIsObstructed");
        await AttachedSpacePublishesBoostThenReleasedAndMovesTheMotorOnce(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_SAFETY_OK case=AttachedSpacePublishesBoostThenReleasedAndMovesTheMotorOnce");
        await BoostObserverResetPreemptsStaleReleaseMotorApplication(tree);
        GD.Print("TASK18_CLAW_SWING_TRANSITION_SAFETY_OK case=BoostObserverResetPreemptsStaleReleaseMotorApplication");
    }

    private static async Task ArrivedCeilingSwingSurvivesLateralDepartureThenSpaceBoostsOnce(SceneTree tree)
    {
        // One fixed narrow ceiling avoids a broad beam's legitimate flank/edge contact.
        // The default capsule, 48px rope, floor, motor and claw settings are unchanged.
        await WithPhysicalSurfaceFixture(tree, "ArrivalLateral", new Vector2(0f, -220f),
            new Vector2(12f, 40f), Vector2.Zero, new Vector2(0f, 14f), true, async fixture =>
        {
            int jumps = fixture.Motor.AcceptedJumpCount;
            int dashes = fixture.Motor.AcceptedDashCount;
            var publications = new List<MechanicalClawFrame>();
            var publicationVelocities = new List<Vector2>();
            Exception callbackFailure = null;
            fixture.Claw.ClawFrameChanged += Observe;
            try
            {
                await SelectGrapple(tree, fixture);
                await ClickForOneFrame(tree, fixture);
                await WaitFor(tree, fixture, () => fixture.Claw.CurrentFrame.Arrived,
                    "physical first arrival before lateral departure");
                ThrowCallbackFailure();
                MechanicalClawFrame arrival = fixture.Claw.CurrentFrame;
                TestAssert.Equal(MechanicalClawState.Swing, arrival.State,
                    "one real click reaches Swing through genuine geometric arrival");
                TestAssert.True(arrival.CasterWorldPosition.DistanceTo(arrival.DockPoint) <= 8.01f,
                    "the real sampled body, not a fabricated frame, establishes arrival");
                Vector2 start = fixture.Player.GlobalPosition;
                GD.Print($"TASK18_CLAW_SWING_TRANSITION_SAMPLE event=arrival cast={arrival.CastId} " +
                    $"position={start} observed={arrival.CasterWorldPosition} velocity={fixture.Player.Velocity} " +
                    $"anchor={arrival.AnchorPoint} dock={arrival.DockPoint} rope={arrival.RopeLength:F3}");
                await Dispatch(tree, fixture, (InputActions.MoveRight, true));
                int consecutiveOutside = 0;
                int maximumOutside = 0;
                // Thirty observed samples including the dispatch sample: 0.5s at 60Hz,
                // not a layout/force search. Require >0.25s continuously outside the old dock.
                for (int sample = 0; sample < 30; sample++)
                {
                    if (sample > 0) await Samples(tree, fixture, 1);
                    ThrowCallbackFailure();
                    MechanicalClawFrame frame = fixture.Claw.CurrentFrame;
                    TestAssert.Equal(MechanicalClawState.Swing, frame.State,
                        $"post-arrival D stays Swing without safety release at sample {sample}; " +
                        $"failure={frame.Failure}, position={fixture.Player.GlobalPosition}, dock={frame.DockPoint}");
                    TestAssert.True(fixture.Input.Current.MoveAxis > .9f &&
                        !fixture.Input.Current.PrimaryHeld && !fixture.Input.Current.JumpPressed,
                        "real D is the sole continuing action after mouse-up");
                    TestAssert.Equal(arrival.CastId, frame.CastId, "lateral samples never recast");
                    TestAssert.Equal(arrival.LockedTargetRuntimeId, frame.LockedTargetRuntimeId,
                        "lateral samples keep the same live target");
                    TestAssert.Equal(arrival.AnchorPoint, frame.AnchorPoint, "lateral samples keep the anchor");
                    TestAssert.Equal(arrival.DockPoint, frame.DockPoint, "lateral samples keep the saved dock");
                    TestAssert.Near(arrival.RopeLength, frame.RopeLength, .001f,
                        "attained Swing does not resume automatic rope shortening");
                    float observedDistance = frame.CasterWorldPosition.DistanceTo(frame.DockPoint);
                    bool physicallyOutside = observedDistance > 8.01f &&
                        fixture.Player.GlobalPosition.DistanceTo(frame.DockPoint) > 8.01f;
                    if (physicallyOutside && !frame.Arrived) consecutiveOutside++;
                    else consecutiveOutside = 0;
                    maximumOutside = Math.Max(maximumOutside, consecutiveOutside);
                    GD.Print($"TASK18_CLAW_SWING_TRANSITION_SAMPLE event=lateral sample={sample} " +
                        $"state={frame.State} arrived={frame.Arrived} observed_distance={observedDistance:F3} " +
                        $"position={fixture.Player.GlobalPosition} velocity={fixture.Player.Velocity} " +
                        $"correction={frame.VelocityCorrection} outside_run={consecutiveOutside}");
                }
                TestAssert.True(maximumOutside >= 16,
                    $"at least sixteen consecutive real outside-envelope Swing samples exceed the 0.25s stall clock; " +
                    $"maximum={maximumOutside}");
                TestAssert.True(fixture.Player.GlobalPosition.X - start.X > 8f,
                    "D produces physical lateral displacement, not only a sampled input flag");
                TestAssert.Equal(jumps, fixture.Motor.AcceptedJumpCount, "the lateral approach adds no jump");
                TestAssert.Equal(dashes, fixture.Motor.AcceptedDashCount, "the lateral approach adds no dash");

                int beforeSpace = publications.Count;
                await Dispatch(tree, fixture, (InputActions.MoveRight, false), (InputActions.Jump, true));
                ThrowCallbackFailure();
                List<MechanicalClawFrame> terminal = publications.GetRange(beforeSpace,
                    publications.Count - beforeSpace).FindAll(frame => frame.State is
                        MechanicalClawState.Boost or MechanicalClawState.Released);
                TestAssert.True(terminal.Count == 2 && terminal[0].State == MechanicalClawState.Boost &&
                    terminal[1].State == MechanicalClawState.Released &&
                    terminal[1].ReleaseKind == MechanicalClawReleaseKind.Boost,
                    "real Space after lateral Swing publishes exactly one Boost then Released");
                TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                    "the atomic Space transaction finishes Released");
                int boostIndex = publications.FindIndex(beforeSpace,
                    frame => frame.State == MechanicalClawState.Boost);
                TestAssert.True(publicationVelocities[boostIndex].DistanceTo(
                        publicationVelocities[boostIndex + 1]) < .001f,
                    "Boost and Released publications do not apply an intermediate motor rewrite");
                TestAssert.True(fixture.Player.Velocity.Length() <= 900.01f,
                    "the post-Swing release retains the composed speed cap");
                await Dispatch(tree, fixture, (InputActions.Jump, false));
                await Samples(tree, fixture, 3);
                ThrowCallbackFailure();
                TestAssert.Equal(jumps, fixture.Motor.AcceptedJumpCount,
                    "attached Space cannot replay as an ordinary double jump after release");
                TestAssert.Equal(dashes, fixture.Motor.AcceptedDashCount, "Space adds no dash");
                TestAssert.True(publications.FindAll(frame => frame.State == MechanicalClawState.Boost).Count == 1 &&
                    publications.FindAll(frame => frame.State == MechanicalClawState.Released).Count == 1,
                    "the complete actual publication stream contains one release transaction only");
                GD.Print($"TASK18_CLAW_SWING_TRANSITION_SAMPLE event=release sequence=Boost,Released " +
                    $"velocity={fixture.Player.Velocity} outside_max={maximumOutside} " +
                    $"jumps_delta={fixture.Motor.AcceptedJumpCount - jumps} dashes_delta={fixture.Motor.AcceptedDashCount - dashes}");
            }
            finally { fixture.Claw.ClawFrameChanged -= Observe; }

            void Observe(MechanicalClawFrame frame, GrappleSurface2D _)
            {
                try
                {
                    publications.Add(frame);
                    publicationVelocities.Add(fixture.Player.Velocity);
                    TestAssert.True(float.IsFinite(fixture.Player.Velocity.X) &&
                        float.IsFinite(fixture.Player.Velocity.Y), "actual published body velocities stay finite");
                }
                catch (Exception exception) { callbackFailure ??= exception; }
            }
            void ThrowCallbackFailure()
            {
                if (callbackFailure != null)
                    throw new InvalidOperationException("Lateral Swing publication callback failed", callbackFailure);
            }
        });
    }

    private static async Task ClickForOneFrameThenMouseUpStillLatchesAndPullsWithoutW(SceneTree tree)
    {
        await WithFixture(tree, "ClickOwned", new Vector2(0f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            TestAssert.True(fixture.Input.Current.PrimaryReleased && !fixture.Input.Current.PrimaryHeld,
                "the real reader observes mouse-up after exactly one held sampling frame");
            await WaitForClaw(tree, fixture, IsAttached, "one-frame click attachment");
            float distanceBefore = fixture.Player.GlobalPosition.DistanceTo(fixture.Claw.CurrentFrame.DockPoint);
            await Samples(tree, fixture, 4);
            float distanceAfter = fixture.Player.GlobalPosition.DistanceTo(fixture.Claw.CurrentFrame.DockPoint);
            TestAssert.True(IsAttached(fixture.Claw.CurrentFrame.State),
                "mouse-up leaves the click-owned claw attached");
            TestAssert.True(distanceAfter < distanceBefore - .5f,
                "the attached player auto-pulls without W or held primary");
        });
    }

    private static async Task SecondClickCancelsAndAThirdClickCanStartANewCast(SceneTree tree)
    {
        await WithFixture(tree, "SecondClickCancel", new Vector2(110f, -150f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "first cast attachment");
            ulong firstCast = fixture.Claw.CurrentFrame.CastId;

            await ClickForOneFrame(tree, fixture);
            TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Released or
                MechanicalClawState.Retract,
                "a second fresh primary click cancels the live click-owned cast");
            await WaitForClaw(tree, fixture, state => state == MechanicalClawState.Idle,
                "second-click retract completion");

            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture,
                state => state is MechanicalClawState.Windup or MechanicalClawState.Travel or
                    MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                "third-click new cast");
            TestAssert.True(fixture.Claw.CurrentFrame.CastId > firstCast,
                "the click after cancellation starts one new cast instead of replaying the consumed edge");
        });
    }

    private static async Task QAndPrimaryConsumesPrimaryWithoutAttackOrCast(SceneTree tree)
    {
        await WithFixture(tree, "QPrimary", new Vector2(90f, -150f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "Q conflict attachment");
            int attacks = 0;
            fixture.Combat.AttackStarted += (_, _) => attacks++;

            await Dispatch(tree, fixture,
                (InputActions.SelectAttackMode, true), (InputActions.PrimaryAction, true));
            TestAssert.Equal(PlayerActionMode.Attack, fixture.Modes.CurrentMode,
                "Q wins and commits Attack mode before later physics consumers");
            TestAssert.Equal(0, attacks,
                "Q plus primary consumes the losing primary edge before combat");
            TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Released or
                MechanicalClawState.Retract,
                "Q plus primary cancels the live claw and does not allocate another cast");
            await Dispatch(tree, fixture,
                (InputActions.SelectAttackMode, false), (InputActions.PrimaryAction, false));
            await WaitForClaw(tree, fixture, state => state == MechanicalClawState.Idle,
                "Q conflict retract completion");

            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture,
                state => state is MechanicalClawState.Windup or MechanicalClawState.Travel or
                    MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                "fresh click after Q conflict");
            TestAssert.Equal(0, attacks,
                "same-frame primary consumption expires without replaying an attack later");
        });
    }

    private static async Task ResolverTargetLossBeatsSameFramePrimaryAndFreshClickRecovers(SceneTree tree)
    {
        await WithFixture(tree, "ResolverLossPrimary", new Vector2(90f, -150f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "resolver-loss attachment");
            ulong lostCast = fixture.Claw.CurrentFrame.CastId;
            int attacks = 0;
            var frames = new List<MechanicalClawFrame>();
            fixture.Combat.AttackStarted += (_, _) => attacks++;
            fixture.Claw.ClawFrameChanged += (frame, _) => frames.Add(frame);
            var invalidation = new PostSampleTargetLossNode
            {
                Name = "PostSampleTargetInvalidation",
                ProcessPhysicsPriority = -150,
                Input = fixture.Input,
                Claw = fixture.Claw,
                Target = fixture.Surface,
                NotifyController = false,
                Armed = true
            };
            fixture.Root.AddChild(invalidation);

            ulong sequenceBeforeLoss = fixture.Input.CurrentFrameSequence;
            await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));

            TestAssert.True(invalidation.Triggered,
                "the fixture makes the target unavailable after Reader sampling and before Claw processing");
            TestAssert.True(invalidation.TriggeredInputSequence > sequenceBeforeLoss &&
                invalidation.TriggeredInputSequence == fixture.Input.CurrentFrameSequence,
                "resolver invalidation is tied to the fresh primary sample sequence consumed by Claw");
            TestAssert.True(frames.Exists(frame => frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost),
                "resolver-detected unavailable target wins over the simultaneous primary command");
            TestAssert.True(!fixture.Input.Current.PrimaryPressed,
                "resolver-detected target loss consumes the losing sampled primary edge");
            TestAssert.Equal(0, attacks,
                "the target-loss primary edge starts neither combat nor a replacement cast");
            await WaitForClaw(tree, fixture, state => state == MechanicalClawState.Idle,
                "resolver-loss cleanup while primary remains held");
            TestAssert.Equal((ulong)0, fixture.Claw.CurrentFrame.CastId,
                "held losing primary never replays as a replacement cast after target-loss cleanup");

            await Dispatch(tree, fixture, (InputActions.PrimaryAction, false));
            fixture.Surface.AddToGroup(GrappleSurface2D.TargetGroup);
            await Samples(tree, fixture, 1);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture,
                state => state is MechanicalClawState.Windup or MechanicalClawState.Travel or
                    MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                "fresh cast after resolver target loss");
            TestAssert.True(fixture.Claw.CurrentFrame.CastId > lostCast,
                "a later fresh primary click can cast after the unavailable target becomes valid again");
            TestAssert.Equal(0, attacks, "fresh recovery in Grapple mode never replays an attack");
        });
    }

    private static async Task PostSampleTargetUnloadConsumesPrimaryBeforeClawAndFreshClickRecovers(
        SceneTree tree)
    {
        await WithFixture(tree, "PostSampleUnloadPrimary", new Vector2(90f, -150f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "post-sample unload attachment");
            int attacks = 0;
            var frames = new List<MechanicalClawFrame>();
            fixture.Combat.AttackStarted += (_, _) => attacks++;
            fixture.Claw.ClawFrameChanged += (frame, _) => frames.Add(frame);
            var unload = new PostSampleTargetLossNode
            {
                Name = "PostSampleTargetUnload",
                ProcessPhysicsPriority = -150,
                Input = fixture.Input,
                Claw = fixture.Claw,
                Target = fixture.Surface,
                NotifyController = true,
                Armed = true
            };
            fixture.Root.AddChild(unload);

            ulong sequenceBeforeUnload = fixture.Input.CurrentFrameSequence;
            await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));

            TestAssert.True(unload.Notified,
                "the fixture invokes NotifyTargetWillUnload after Reader sampling and before Claw processing");
            TestAssert.True(unload.TriggeredInputSequence > sequenceBeforeUnload &&
                unload.TriggeredInputSequence == fixture.Input.CurrentFrameSequence,
                "NotifyTargetWillUnload is tied to the fresh primary sample sequence consumed before Claw");
            TestAssert.True(frames.Exists(frame => frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost),
                "post-sampling forced unload preserves the typed target-loss publication");
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "post-sampling target unload remains the final state instead of permitting same-edge recast");
            TestAssert.True(!fixture.Input.Current.PrimaryPressed,
                "post-sampling target unload consumes the same sampled primary before Claw and Combat");
            TestAssert.Equal(0, attacks,
                "post-sampling unload primary starts neither combat nor a replacement cast");

            await Samples(tree, fixture, 3);
            TestAssert.Equal((ulong)0, fixture.Claw.CurrentFrame.CastId,
                "held losing primary does not replay after the unload reset reaches Idle");
            await Dispatch(tree, fixture, (InputActions.PrimaryAction, false));
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture,
                state => state is MechanicalClawState.Windup or MechanicalClawState.Travel or
                    MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                "fresh cast after post-sample target unload");
            TestAssert.True(fixture.Claw.CurrentFrame.CastId != 0UL,
                "a later fresh primary click casts after lifecycle target-loss consumption");
            TestAssert.Equal(0, attacks, "post-unload recovery in Grapple mode replays no attack");
        });
    }

    private static async Task ShieldAndSpaceCancelsWithoutBoostOrJump(SceneTree tree)
    {
        await WithFixture(tree, "ShieldSpace", new Vector2(0f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "shield conflict attachment");
            int jumpsBefore = fixture.Motor.AcceptedJumpCount;
            int boosts = 0;
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Boost) boosts++;
            };

            await Dispatch(tree, fixture, (InputActions.Shield, true), (InputActions.Jump, true));
            TestAssert.True(fixture.Shield.IsActive,
                "RMB plus Space raises the available shield while cancellation owns the claw command");
            TestAssert.Equal(0, boosts, "RMB plus Space never publishes a boost");
            TestAssert.Equal(jumpsBefore, fixture.Motor.AcceptedJumpCount,
                "RMB plus attached Space consumes the losing ordinary-jump edge");
            TestAssert.Equal(MechanicalClawReleaseKind.Cancel, fixture.Claw.CurrentFrame.ReleaseKind,
                "RMB plus Space performs only normal claw cancellation");
        });
    }

    private static async Task UnavailableShieldAndSpaceStillCancelsWithoutJump(SceneTree tree)
    {
        await WithFixture(tree, "UnavailableShieldSpace", new Vector2(0f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "unavailable-shield conflict attachment");
            TestAssert.True(fixture.Energy.TrySpendHeavyGuard(100f) && fixture.Energy.EnergyRatio < .001f,
                "fixture makes the requested guard unavailable before RMB is sampled");
            int jumpsBefore = fixture.Motor.AcceptedJumpCount;

            await Dispatch(tree, fixture, (InputActions.Shield, true), (InputActions.Jump, true));

            TestAssert.True(!fixture.Shield.IsActive,
                "zero-energy RMB stays physically unavailable while its raw input still owns arbitration");
            TestAssert.Equal(jumpsBefore, fixture.Motor.AcceptedJumpCount,
                "unavailable raw RMB still consumes attached Space before jump buffering");
            TestAssert.Equal(MechanicalClawReleaseKind.Cancel, fixture.Claw.CurrentFrame.ReleaseKind,
                "unavailable raw RMB still wins the claw command over boost release");
        });
    }

    private static async Task DashAndSpaceCancelsThenUsesOnlyTheExistingDash(SceneTree tree)
    {
        await WithFixture(tree, "DashSpace", new Vector2(0f, -220f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "dash conflict attachment");
            await WaitFor(tree, fixture, () => !fixture.Player.IsOnFloor(), "airborne attached player");
            int jumpsBefore = fixture.Motor.AcceptedJumpCount;
            int dashesBefore = fixture.Motor.AcceptedDashCount;

            await Dispatch(tree, fixture, (InputActions.Dash, true), (InputActions.Jump, true));
            TestAssert.Equal(dashesBefore + 1, fixture.Motor.AcceptedDashCount,
                "Dash plus attached Space uses the existing eligible air dash exactly once");
            TestAssert.Equal(jumpsBefore, fixture.Motor.AcceptedJumpCount,
                "Dash priority consumes attached Space before the motor can buffer a jump");
            TestAssert.Equal(MechanicalClawReleaseKind.Cancel, fixture.Claw.CurrentFrame.ReleaseKind,
                "Dash plus Space performs normal cancellation rather than boost release");

            await Dispatch(tree, fixture, (InputActions.Jump, false), (InputActions.Dash, false));
            await Samples(tree, fixture, 1);
            TestAssert.Equal(jumpsBefore, fixture.Motor.AcceptedJumpCount,
                "the suppressed Space edge cannot replay on the frame after the air-dash budget is spent");
            TestAssert.True(!fixture.Motor.AirDashAvailable,
                "claw cancellation never restores the already spent air-dash budget");
        });
    }

    private static async Task AttachedSpacePublishesBoostThenReleasedAndMovesTheMotorOnce(SceneTree tree)
    {
        await WithFixture(tree, "AtomicBoostRelease", new Vector2(0f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "atomic release attachment");
            int jumpsBefore = fixture.Motor.AcceptedJumpCount;
            var terminalStates = new List<MechanicalClawState>();
            var publicationVelocities = new List<Vector2>();
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State is not (MechanicalClawState.Boost or MechanicalClawState.Released)) return;
                terminalStates.Add(frame.State);
                publicationVelocities.Add(fixture.Player.Velocity);
            };
            fixture.Motor.SetPhysicsProcess(false);
            fixture.Motor.ExternalImpulse(new Vector2(100f, 0f));

            await Dispatch(tree, fixture, (InputActions.Jump, true));

            TestAssert.True(terminalStates.Count == 2 &&
                terminalStates[0] == MechanicalClawState.Boost &&
                terminalStates[1] == MechanicalClawState.Released,
                "one attached Space publishes one ordered transaction");
            TestAssert.True(publicationVelocities.Count == 2 &&
                publicationVelocities[0].DistanceTo(publicationVelocities[1]) < .001f,
                "neither publication applies an intermediate boost velocity to the body");
            TestAssert.True(fixture.Player.Velocity.DistanceTo(new Vector2(320f, 0f)) < .01f,
                "the controller applies the hand-derived ceiling release exactly once after both publications");
            TestAssert.Equal(jumpsBefore, fixture.Motor.AcceptedJumpCount,
                "attached Space is never buffered into an ordinary jump");
            TestAssert.True(fixture.Player.Velocity.Length() <= 900.01f,
                "the one applied final release velocity is capped after composition");
            TestAssert.Equal(MechanicalClawState.Released, fixture.Claw.CurrentFrame.State,
                "the atomic controller callback leaves Released as its final current frame");
        });
    }

    private static async Task TravelSpaceRemainsAnOrdinaryJumpAndGrantsNoBoostRelease(SceneTree tree)
    {
        await WithFixture(tree, "TravelJump", new Vector2(500f, 0f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, state => state == MechanicalClawState.Travel,
                "travel jump setup");
            int jumpsBefore = fixture.Motor.AcceptedJumpCount;
            int boosts = 0;
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Boost) boosts++;
            };

            await Dispatch(tree, fixture, (InputActions.Jump, true));
            TestAssert.Equal(jumpsBefore + 1, fixture.Motor.AcceptedJumpCount,
                "Space during Travel remains the ordinary player jump");
            TestAssert.Equal(0, boosts, "Travel Space grants no attached boost publication");
            TestAssert.True(fixture.Claw.CurrentFrame.ReleaseKind != MechanicalClawReleaseKind.Boost,
                "Travel Space grants no boost-release metadata");
        });
    }

    private static async Task BoostObserverResetPreemptsStaleReleaseMotorApplication(SceneTree tree)
    {
        await WithFixture(tree, "BoostObserverReset", new Vector2(140f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "reentrant reset attachment");
            fixture.Motor.SetPhysicsProcess(false);
            fixture.Motor.ExternalImpulse(new Vector2(120f, -50f));
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Boost) fixture.Claw.ResetTransientState();
            };

            await Dispatch(tree, fixture, (InputActions.Jump, true));

            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "a synchronous lifecycle reset during Boost owns the final controller state");
            TestAssert.True(!fixture.Motor.HasGrappleReleaseMomentum,
                "the outer atomic callback cannot apply stale boost-release after a lifecycle reset");
            TestAssert.True(Mathf.Abs(fixture.Player.Velocity.X - 120f) < 1f,
                "the stale release boost cannot rewrite collision-observed horizontal velocity after reset");
        });
    }

    private static async Task AutoPullStopsAtWallDockWithoutOverlapOrJitter(SceneTree tree) =>
        await AssertStopsAtSavedFace(tree, "WallDock", new Vector2(220f, 14f),
            new Vector2(40f, 260f), Vector2.Zero, new Vector2(0f, 14f),
            Vector2.Left, position => position.X <= 580.15f, true,
            minimumPhysicalDisplacement: 96f);

    private static async Task AutoPullStopsBelowCeilingWithoutOverlapOrJitter(SceneTree tree) =>
        await AssertStopsAtSavedFace(tree, "CeilingDock", new Vector2(0f, -220f),
            new Vector2(260f, 40f), Vector2.Zero, new Vector2(0f, 14f),
            Vector2.Down, position => position.Y >= 135.85f, true,
            minimumPhysicalDisplacement: 96f);

    private static async Task AutoPullStopsOnTheSavedFaceOfAConvexEdge(SceneTree tree) =>
        await AssertStopsAtSavedFace(tree, "ConvexEdge", new Vector2(220f, 14f),
            new Vector2(40f, 80f), new Vector2(0f, 30f), new Vector2(0f, 44f),
            Vector2.Left, position => position.X <= 580.15f, true, 90f, 96f);

    private static async Task FourSecondFallCanClickAVisibleWallAndBrakeContinuously(SceneTree tree)
    {
        var wallSize = new Vector2(40f, 700f);
        await WithPhysicalSurfaceFixture(tree, "FourSecondFall", new Vector2(420f, 2900f),
            wallSize, new Vector2(0f, -50f), Vector2.Zero, false, async fixture =>
        {
            for (int frame = 0; frame < 240; frame++) await Samples(tree, fixture, 1);
            TestAssert.True(Mathf.Abs(fixture.Player.Velocity.Y - 780f) < .1f,
                $"the real motor reaches the existing 780px/s fall cap; velocity={fixture.Player.Velocity}");
            TestAssert.True(fixture.Player.GlobalPosition.DistanceTo(fixture.Surface.GlobalPosition) <= 620f,
                "after four physical seconds the side-wall target is inside 620px");
            int jumps = fixture.Motor.AcceptedJumpCount;
            int dashes = fixture.Motor.AcceptedDashCount;
            await SelectGrapple(tree, fixture);
            Vector2 clickPosition = Vector2.Zero;
            Vector2 clickVelocity = Vector2.Zero;
            fixture.Claw.ClawFrameChanged += CaptureClick;
            await ClickForOneFrame(tree, fixture);
            fixture.Claw.ClawFrameChanged -= CaptureClick;
            Vector2 lockedAnchor = fixture.Claw.CurrentFrame.AnchorPoint;
            Vector2 lockedNormal = fixture.Claw.CurrentFrame.SurfaceNormal;
            Vector2 lockedDock = fixture.Claw.CurrentFrame.DockPoint;
            StaticBody2D targetBody = fixture.Surface.GetParent<StaticBody2D>();
            float expectedFirstFace = targetBody.GlobalPosition.X - wallSize.X * .5f;
            TestAssert.True(Mathf.Abs(lockedAnchor.X - expectedFirstFace) < .1f &&
                lockedAnchor.Y >= targetBody.GlobalPosition.Y - wallSize.Y * .5f - .1f &&
                lockedAnchor.Y <= targetBody.GlobalPosition.Y + wallSize.Y * .5f + .1f,
                $"the falling cast locks the wall's real first-hit face; " +
                $"anchor={lockedAnchor}, expected_x={expectedFirstFace:F3}");
            TestAssert.True(lockedNormal.Normalized().Dot(Vector2.Left) > .99f,
                $"the falling cast saves the real outward wall normal; normal={lockedNormal}");
            TestAssert.True(lockedDock.DistanceTo(lockedAnchor + Vector2.Left *
                    MechanicalClawSettings.Default.MinimumRopeLength) < .01f,
                $"the falling cast derives its safe dock from the locked hit and normal; " +
                $"anchor={lockedAnchor}, normal={lockedNormal}, dock={lockedDock}");
            GD.Print($"TASK4_TERRAIN_TRAJECTORY event=click position={clickPosition} " +
                $"velocity={clickVelocity} aim={fixture.Surface.GlobalPosition} " +
                $"locked_anchor={lockedAnchor} saved_normal={lockedNormal} dock={lockedDock}");
            var downward = new List<float>();
            var changes = new List<float>();
            Vector2 before = fixture.Player.Velocity;
            bool latchPrinted = false;
            bool traceTrajectory = Array.Exists(OS.GetCmdlineUserArgs(),
                value => value == "--trace-task4-terrain-trajectory");
            const int maximumArrivalSamples = 220;
            for (int frame = 0; frame < maximumArrivalSamples &&
                !fixture.Claw.CurrentFrame.Arrived; frame++)
            {
                Vector2 toward = fixture.Player.GlobalPosition.DirectionTo(lockedDock);
                await Samples(tree, fixture, 1);
                Vector2 now = fixture.Player.Velocity;
                changes.Add((now - before).Dot(toward));
                downward.Add(now.Y);
                before = now;
                TestAssert.True(fixture.Claw.CurrentFrame.AnchorPoint.DistanceTo(lockedAnchor) < .001f &&
                    fixture.Claw.CurrentFrame.SurfaceNormal.DistanceTo(lockedNormal) < .001f &&
                    fixture.Claw.CurrentFrame.DockPoint.DistanceTo(lockedDock) < .001f,
                    $"the falling pull keeps its true hit, normal, and dock immutable at frame {frame}");
                if (IsAttached(fixture.Claw.CurrentFrame.State))
                {
                    string evt = latchPrinted ? "sample" : "latch";
                    if (!latchPrinted || traceTrajectory)
                        GD.Print($"TASK4_TERRAIN_TRAJECTORY event={evt} frame={frame} " +
                            $"state={fixture.Claw.CurrentFrame.State} position={fixture.Player.GlobalPosition} " +
                            $"velocity={fixture.Player.Velocity} anchor={fixture.Claw.CurrentFrame.AnchorPoint} " +
                            $"dock={fixture.Claw.CurrentFrame.DockPoint} " +
                            $"distance={fixture.Player.GlobalPosition.DistanceTo(fixture.Claw.CurrentFrame.DockPoint):F3} " +
                            $"targetward={fixture.Player.Velocity.Dot(toward):F3}");
                    latchPrinted = true;
                }
            }
            TestAssert.True(changes.TrueForAll(change => Mathf.Abs(change) <=
                    MechanicalClawSettings.Default.MaxPullAcceleration / 60f + 1850f / 60f + .5f),
                "the absolute signed targetward change stays inside pull acceleration plus one gravity step; " +
                $"changes={string.Join(",", changes.ConvertAll(value => value.ToString("F2")))}");
            bool multiFrameBrake = false;
            for (int index = 2; index < downward.Count; index++)
                if (downward[index - 2] > downward[index - 1] &&
                    downward[index - 1] > downward[index] && downward[index] > 0f)
                    multiFrameBrake = true;
            TestAssert.True(multiFrameBrake,
                "fall braking takes multiple frames instead of zeroing downward speed at once; " +
                $"state={fixture.Claw.CurrentFrame.State}, failure={fixture.Claw.CurrentFrame.Failure}, " +
                $"player={fixture.Player.GlobalPosition}, target={fixture.Surface.GlobalPosition}, " +
                $"samples={string.Join(",", downward.ConvertAll(value => value.ToString("F1")))}");
            TestAssert.Equal(jumps, fixture.Motor.AcceptedJumpCount, "claw pull adds no ordinary jump");
            TestAssert.Equal(dashes, fixture.Motor.AcceptedDashCount, "claw pull adds no dash");
            TestAssert.True(fixture.Claw.CurrentFrame.Arrived,
                $"the complete observed trajectory reaches the safe wall dock within " +
                $"{maximumArrivalSamples} physical samples; state={fixture.Claw.CurrentFrame.State}, " +
                $"failure={fixture.Claw.CurrentFrame.Failure}, position={fixture.Player.GlobalPosition}, " +
                $"dock={lockedDock}");
            TestAssert.True(fixture.Claw.CurrentFrame.CasterWorldPosition.DistanceTo(lockedDock) <=
                    MechanicalClawSettings.Default.ArrivalTolerance + .01f,
                "the sampled real body position, not a fabricated active frame, establishes arrival");
            GD.Print($"TASK4_TERRAIN_TRAJECTORY event=arrival " +
                $"observed_position={fixture.Claw.CurrentFrame.CasterWorldPosition} " +
                $"post_motor_position={fixture.Player.GlobalPosition} velocity={fixture.Player.Velocity} " +
                $"anchor={fixture.Claw.CurrentFrame.AnchorPoint} " +
                $"dock={fixture.Claw.CurrentFrame.DockPoint} " +
                $"observed_distance={fixture.Claw.CurrentFrame.CasterWorldPosition.DistanceTo(fixture.Claw.CurrentFrame.DockPoint):F3}");
            var terminalStates = new List<MechanicalClawState>();
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State is MechanicalClawState.Boost or MechanicalClawState.Released)
                    terminalStates.Add(frame.State);
            };
            await Dispatch(tree, fixture, (InputActions.Jump, true));
            TestAssert.True(terminalStates.Count == 2 &&
                terminalStates[0] == MechanicalClawState.Boost &&
                terminalStates[1] == MechanicalClawState.Released,
                "Space after the long fall publishes exactly one ordered Boost then Released transaction");
            GD.Print($"TASK4_TERRAIN_RELEASE sequence={string.Join(",", terminalStates)} " +
                $"release_kind={fixture.Claw.CurrentFrame.ReleaseKind}");
            TestAssert.Equal(MechanicalClawReleaseKind.Boost, fixture.Claw.CurrentFrame.ReleaseKind,
                "Space after the long fall publishes Boost then Released");
            TestAssert.True(fixture.Player.Velocity.Length() <= 900.01f,
                "post-fall boost release remains capped at 900px/s");
            TestAssert.Equal(jumps, fixture.Motor.AcceptedJumpCount,
                "attached Space after the fall is not an ordinary jump");

            void CaptureClick(MechanicalClawFrame published, GrappleSurface2D _)
            {
                if (clickPosition.IsZeroApprox() && published.State == MechanicalClawState.Aim)
                {
                    clickPosition = published.CasterWorldPosition;
                    clickVelocity = published.CasterVelocity;
                }
            }
        });
    }

    private static async Task ThinWorldBlockerBetweenCasterAndTargetCannotBeCrossed(SceneTree tree) =>
        await AssertInsertedBlocker(tree, "ThinBlocker", new Vector2(2f, 220f), 579.15f);

    private static async Task InsertedBlockerDuringPullReleasesAsObstructed(SceneTree tree) =>
        await AssertInsertedBlocker(tree, "InsertedBlocker", new Vector2(40f, 220f), 560.15f);

    private static async Task EarlyCollisionWithAnotherFaceOfTheTargetIsObstructed(SceneTree tree)
    {
        await WithFixture(tree, "WrongTargetFace", new Vector2(420f, -80f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "wrong-face collision setup");
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Motor.SetPhysicsProcess(false);
            var releases = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Released) releases.Add(frame);
            };
            var targetBody = WorldBlocker(new Vector2(150f, -30f), new Vector2(300f, 20f));
            fixture.Root.AddChild(targetBody);
            fixture.Surface.Reparent(targetBody);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            MechanicalClawTargetSnapshot locked = new MechanicalClawTargetResolver().SnapshotLocked(
                fixture.Player, fixture.Surface, fixture.Claw.CurrentFrame.AnchorPoint,
                fixture.Claw.CurrentFrame.SurfaceNormal);
            TestAssert.True(locked.Exists && locked.HasLineOfSight,
                "the wrong-face collider belongs to the locked target and is accepted by continuing LOS");
            fixture.Claw._PhysicsProcess(1f / 60f);
            TestAssert.True(releases.Count == 1 &&
                releases[0].Failure == MechanicalClawFailure.Obstructed &&
                releases[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed,
                "an early collision that cannot match the saved face releases as Obstructed");
        });
    }

    private static async Task CapsuleFlankBlockerWithClearLineOfSightReleasesBeforeContact(
        SceneTree tree)
    {
        await WithFixture(tree, "ClearLineFlankSweep", new Vector2(420f, -80f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "clear-line flank sweep setup");
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Motor.SetPhysicsProcess(false);

            MechanicalClawFrame frame = fixture.Claw.CurrentFrame with
            {
                CasterWorldPosition = fixture.Player.GlobalPosition,
                CasterVelocity = fixture.Player.Velocity
            };
            MechanicalClawTargetSnapshot snapshot = new MechanicalClawTargetResolver().SnapshotLocked(
                fixture.Player, fixture.Surface, frame.AnchorPoint, frame.SurfaceNormal);
            MechanicalClawFrame preview = new MechanicalClawModel().Step(frame,
                new MechanicalClawInput(MechanicalClawCommand.None, 0f, false, frame.DockPoint),
                snapshot, 1f / 60f);
            Vector2 intendedMotion = (fixture.Player.Velocity +
                preview.VelocityCorrection - frame.VelocityCorrection) / 60f;
            TestAssert.True(intendedMotion.X > fixture.Player.SafeMargin + .02f,
                $"fixture derives a positive one-tick sweep longer than SafeMargin; motion={intendedMotion}");

            float gap = Mathf.Max(fixture.Player.SafeMargin + .01f, intendedMotion.X * .5f);
            TestAssert.True(gap < intendedMotion.X,
                "the derived flank gap is outside recovery margin but inside the actual next displacement");
            Vector2 player = fixture.Player.GlobalPosition;
            float blockerLeft = player.X + 20f + gap;
            var blocker = WorldBlocker(new Vector2(
                    blockerLeft + 1f - fixture.Root.GlobalPosition.X,
                    player.Y - 11f - fixture.Root.GlobalPosition.Y),
                new Vector2(2f, 6f));
            fixture.Root.AddChild(blocker);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

            MechanicalClawTargetSnapshot clearLine = new MechanicalClawTargetResolver().SnapshotLocked(
                fixture.Player, fixture.Surface, frame.AnchorPoint, frame.SurfaceNormal);
            TestAssert.True(clearLine.Exists && clearLine.HasLineOfSight,
                "the narrow flank blocker misses the real caster-to-anchor center ray");
            for (int index = 0; index < fixture.Player.GetSlideCollisionCount(); index++)
                TestAssert.True(fixture.Player.GetSlideCollision(index).GetCollider() != blocker,
                    "the body has no prior contact with the newly inserted flank blocker");
            using (var predicted = new KinematicCollision2D())
            {
                TestAssert.True(fixture.Player.TestMove(fixture.Player.GlobalTransform,
                        intendedMotion, predicted, fixture.Player.SafeMargin, false) &&
                    predicted.GetCollider() == blocker,
                    "the real capsule TestMove detects the off-ray blocker before MoveAndSlide");
            }

            var releases = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += (published, _) =>
            {
                if (published.State == MechanicalClawState.Released) releases.Add(published);
            };
            Vector2 before = fixture.Player.GlobalPosition;
            fixture.Claw._PhysicsProcess(1f / 60f);
            TestAssert.True(releases.Count == 1 &&
                releases[0].Failure == MechanicalClawFailure.Obstructed &&
                releases[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed,
                "the clear-LOS capsule flank hit releases once before motor movement or stall timeout");
            TestAssert.True(fixture.Player.GlobalPosition.DistanceTo(before) < .001f,
                "the controller probe never moves GlobalPosition");
            fixture.Motor._PhysicsProcess(1f / 60f);
            TestAssert.True(fixture.Player.GlobalPosition.X <= blockerLeft - 20f + .1f,
                "the released body remains on the origin side of the thin flank blocker");
        });
    }

    private static async Task ReachedDockWinsBeforeTheNoProgressTimeout(SceneTree tree) =>
        await AssertStopsAtSavedFace(tree, "ArrivalBeforeStall", new Vector2(220f, 14f),
            new Vector2(40f, 260f), Vector2.Zero, new Vector2(160f, 5f),
            Vector2.Left, position => position.X <= 580.15f, true);

    private static async Task NoProgressForQuarterSecondReleasesOnceAndClearsCorrection(SceneTree tree)
    {
        await WithFixture(tree, "NoProgress", new Vector2(0f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "stalled pull setup");
            fixture.Motor.SetPhysicsProcess(false);
            fixture.Player.Velocity = Vector2.Zero;
            var released = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Released) released.Add(frame);
            };
            await Samples(tree, fixture, 18);
            TestAssert.Equal(1, released.Count,
                "0.25 seconds without meaningful dock progress publishes one release");
            TestAssert.True(released[0].Failure == MechanicalClawFailure.Obstructed &&
                released[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed &&
                released[0].VelocityCorrection.IsZeroApprox(),
                "the stall release is typed Obstructed and clears correction");
        });
    }

    private static async Task SubpixelDockProgressAccumulatesBeforeStallTimeout(SceneTree tree)
    {
        await WithFixture(tree, "SubpixelProgress", new Vector2(300f, 14f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "subpixel progress setup");
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Motor.SetPhysicsProcess(false);
            fixture.Player.Velocity = new Vector2(30f, 0f);
            for (int frame = 0; frame < 18; frame++)
            {
                fixture.Claw._PhysicsProcess(1f / 60f);
                fixture.Motor._PhysicsProcess(1f / 60f);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            TestAssert.True(IsAttached(fixture.Claw.CurrentFrame.State),
                "repeated subpixel physical progress accumulates before the stall timeout");
        });
    }

    private static async Task ExplicitReleaseIsNotReclassifiedByPullSweep(SceneTree tree)
    {
        await WithFixture(tree, "ExplicitRelease", new Vector2(420f, 14f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "explicit release setup");
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Root.AddChild(WorldBlocker(new Vector2(200f, 14f), new Vector2(40f, 220f)));
            await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));
            fixture.Claw._PhysicsProcess(1f / 60f);
            TestAssert.Equal(MechanicalClawReleaseKind.Cancel, fixture.Claw.CurrentFrame.ReleaseKind,
                "an explicit second click is not relabeled by LOS or body sweep");
            TestAssert.Equal(MechanicalClawFailure.None, fixture.Claw.CurrentFrame.Failure,
                "explicit cancellation retains normal failure metadata");
        });
    }

    private static async Task PauseDoesNotAdvanceTheNoProgressClock(SceneTree tree)
    {
        await WithFixture(tree, "PausedStall", new Vector2(0f, -180f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "paused stall setup");
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Motor.SetPhysicsProcess(false);
            tree.Paused = true;
            for (int frame = 0; frame < 30; frame++) fixture.Claw._PhysicsProcess(1f / 60f);
            tree.Paused = false;
            fixture.Claw._PhysicsProcess(1f / 60f);
            TestAssert.True(IsAttached(fixture.Claw.CurrentFrame.State),
                "paused ticks do not consume the no-progress budget");
        });
    }

    private static async Task ArrivedConstraintStillReleasesOnContinuingLosLoss(SceneTree tree)
    {
        await WithPhysicalSurfaceFixture(tree, "ArrivedLosLoss", new Vector2(220f, 14f),
            new Vector2(40f, 260f), Vector2.Zero, new Vector2(140f, 14f), true, async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitFor(tree, fixture, () => fixture.Claw.CurrentFrame.Arrived,
                "arrived constraint before LOS loss");
            var released = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Released) released.Add(frame);
            };
            fixture.Root.AddChild(WorldBlocker(new Vector2(176f, 14f), new Vector2(4f, 80f)));
            await Samples(tree, fixture, 1);
            TestAssert.True(released.Count == 1 &&
                released[0].Failure == MechanicalClawFailure.Obstructed &&
                released[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed,
                "a surviving arrived Swing releases immediately when continuing LOS is lost");
        });
    }

    private static async Task AssertStopsAtSavedFace(SceneTree tree, string name,
        Vector2 bodyPosition, Vector2 bodySize, Vector2 surfaceLocalPosition,
        Vector2 playerPosition, Vector2 savedNormal, Func<Vector2, bool> outsideFace,
        bool addFloor, float floorY = 60f, float minimumPhysicalDisplacement = 0f)
    {
        await WithPhysicalSurfaceFixture(tree, name, bodyPosition, bodySize, surfaceLocalPosition,
            playerPosition, addFloor, async fixture =>
        {
            Vector2 approachStart = fixture.Player.GlobalPosition;
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, name + " attached pull");
            Vector2 lockedAnchor = fixture.Claw.CurrentFrame.AnchorPoint;
            Vector2 lockedDock = fixture.Claw.CurrentFrame.DockPoint;
            StaticBody2D targetBody = fixture.Surface.GetParent<StaticBody2D>();
            float expectedFace = Mathf.Abs(savedNormal.X) > .9f
                ? targetBody.GlobalPosition.X - bodySize.X * .5f
                : targetBody.GlobalPosition.Y + bodySize.Y * .5f;
            float actualFace = Mathf.Abs(savedNormal.X) > .9f ? lockedAnchor.X : lockedAnchor.Y;
            TestAssert.True(Mathf.Abs(actualFace - expectedFace) < .1f &&
                fixture.Claw.CurrentFrame.SurfaceNormal.Normalized().Dot(savedNormal) > .99f,
                $"{name} locks the real first-hit face and its physics normal");
            var positions = new List<Vector2>();
            var projected = new List<float>();
            for (int frame = 0; frame < 120 && !fixture.Claw.CurrentFrame.Arrived; frame++)
            {
                await Samples(tree, fixture, 1);
                positions.Add(fixture.Player.GlobalPosition);
                projected.Add(fixture.Player.Velocity.Dot(savedNormal));
                TestAssert.True(fixture.Claw.CurrentFrame.AnchorPoint.DistanceTo(lockedAnchor) < .001f &&
                    fixture.Claw.CurrentFrame.DockPoint.DistanceTo(lockedDock) < .001f,
                    $"{name} keeps the saved anchor and dock immutable during pull");
            }
            TestAssert.True(fixture.Claw.CurrentFrame.Arrived,
                $"{name} reaches the saved non-overlapping dock through real physics; " +
                $"state={fixture.Claw.CurrentFrame.State}, failure={fixture.Claw.CurrentFrame.Failure}, " +
                $"position={fixture.Player.GlobalPosition}, dock={fixture.Claw.CurrentFrame.DockPoint}");
            Vector2 arrivalPosition = fixture.Player.GlobalPosition;
            for (int frame = 0; frame < 6; frame++)
            {
                await Samples(tree, fixture, 1);
                positions.Add(fixture.Player.GlobalPosition);
                projected.Add(fixture.Player.Velocity.Dot(savedNormal));
            }
            TestAssert.True(positions.TrueForAll(position => outsideFace(position)),
                $"{name} never crosses the collider face minus capsule support");
            if (minimumPhysicalDisplacement > 0f)
            {
                float physicalDisplacement = approachStart.DistanceTo(arrivalPosition);
                TestAssert.True(physicalDisplacement >= minimumPhysicalDisplacement,
                    $"{name} physically traverses at least {minimumPhysicalDisplacement:F0}px " +
                    $"from a meaningfully outside start; start={approachStart}, " +
                    $"arrival={arrivalPosition}, dock={lockedDock}");
                GD.Print($"TASK4_PHYSICAL_APPROACH fixture={name} start={approachStart} " +
                    $"arrival={arrivalPosition} displacement={physicalDisplacement:F3} " +
                    $"dock={lockedDock}");
            }
            TestAssert.True(positions.GetRange(positions.Count - 6, 6).TrueForAll(position =>
                    position.DistanceTo(fixture.Claw.CurrentFrame.DockPoint) <= 8.01f),
                $"{name} keeps six final samples inside the 8px arrival envelope; state=" +
                $"{fixture.Claw.CurrentFrame.State}, failure={fixture.Claw.CurrentFrame.Failure}, " +
                $"dock={fixture.Claw.CurrentFrame.DockPoint}, tail=" +
                string.Join(",", positions.GetRange(positions.Count - 6, 6).ConvertAll(position =>
                    position.DistanceTo(fixture.Claw.CurrentFrame.DockPoint).ToString("F2"))));
            List<float> tail = projected.GetRange(projected.Count - 6, 6);
            for (int index = 1; index < tail.Count; index++)
                TestAssert.True(tail[index - 1] * tail[index] >= -.01f,
                    $"{name} final speed does not alternate sign across the saved normal");
        }, floorY);
    }

    private static async Task AssertInsertedBlocker(SceneTree tree, string name,
        Vector2 blockerSize, float maximumX)
    {
        await WithFixture(tree, name, new Vector2(500f, 14f), async fixture =>
        {
            await SelectGrapple(tree, fixture);
            await ClickForOneFrame(tree, fixture);
            await WaitForClaw(tree, fixture, IsAttached, "inserted blocker setup");
            var releases = new List<MechanicalClawFrame>();
            fixture.Claw.ClawFrameChanged += (frame, _) =>
            {
                if (frame.State == MechanicalClawState.Released) releases.Add(frame);
            };
            fixture.Root.AddChild(WorldBlocker(new Vector2(200f, 14f), blockerSize));
            var positions = new List<Vector2>();
            for (int frame = 0; frame < 12; frame++)
            {
                await Samples(tree, fixture, 1);
                positions.Add(fixture.Player.GlobalPosition);
            }
            TestAssert.True(positions.TrueForAll(position => position.X <= maximumX),
                $"{name} stays on the origin side of the physical blocker");
            TestAssert.True(releases.Count == 1 &&
                releases[0].Failure == MechanicalClawFailure.Obstructed &&
                releases[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed &&
                releases[0].VelocityCorrection.IsZeroApprox(),
                $"{name} publishes exactly one typed Obstructed release with zero correction; " +
                $"release_count={releases.Count}, final_state={fixture.Claw.CurrentFrame.State}, " +
                $"final_failure={fixture.Claw.CurrentFrame.Failure}, " +
                $"final_release={fixture.Claw.CurrentFrame.ReleaseKind}, " +
                $"release_correction={(releases.Count == 1 ? releases[0].VelocityCorrection : Vector2.Zero)}");
            TestAssert.True(fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() &&
                Mathf.Abs(fixture.Motor.GrappleMovementScale - 1f) < .0001f &&
                Mathf.Abs(fixture.Motor.EffectiveMovementScale - 1f) < .0001f,
                $"{name} terminal cleanup restores normal motor movement without erasing body momentum; " +
                $"frame_correction={fixture.Claw.CurrentFrame.VelocityCorrection}, " +
                $"grapple_scale={fixture.Motor.GrappleMovementScale:F4}, " +
                $"effective_scale={fixture.Motor.EffectiveMovementScale:F4}, " +
                $"body_velocity={fixture.Player.Velocity}");
            GD.Print($"TASK4_OBSTRUCTION_RELEASE fixture={name} count={releases.Count} " +
                $"failure={releases[0].Failure} release_kind={releases[0].ReleaseKind} " +
                $"correction={releases[0].VelocityCorrection} " +
                $"grapple_scale={fixture.Motor.GrappleMovementScale:F4} " +
                $"effective_scale={fixture.Motor.EffectiveMovementScale:F4} " +
                $"body_velocity={fixture.Player.Velocity}");
            TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Released or
                    MechanicalClawState.Retract or MechanicalClawState.Idle,
                $"{name} terminates the pull instead of crossing");
        });
    }

    private static async Task WithFixture(SceneTree tree, string name, Vector2 surfacePosition,
        Func<Fixture, Task> test)
    {
        await ReleaseAll(tree);
        var root = new Node2D
        {
            Name = "AutoPullTask3" + name,
            Position = new Vector2(400f, 300f)
        };
        GrappleSurface2D surface = Surface(surfacePosition);
        root.AddChild(surface);
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 60f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(1600f, 20f) }
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = new Vector2(0f, 14f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        var fixture = new Fixture(player, surface);
        try
        {
            await Samples(tree, fixture, 3);
            await AimAtSurface(tree, fixture);
            await test(fixture);
        }
        finally
        {
            await ReleaseAll(tree);
            root.QueueFree();
            for (int index = 0; index < 2; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task WithPhysicalSurfaceFixture(SceneTree tree, string name,
        Vector2 bodyPosition, Vector2 bodySize, Vector2 surfaceLocalPosition,
        Vector2 playerPosition, bool addFloor, Func<Fixture, Task> test, float floorY = 60f)
    {
        await ReleaseAll(tree);
        var root = new Node2D { Name = "AutoPullTask4" + name, Position = new Vector2(400f, 300f) };
        StaticBody2D targetBody = WorldBlocker(bodyPosition, bodySize);
        targetBody.Name = "TargetBody";
        var surface = new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Position = surfaceLocalPosition,
            Material = GrappleSurfaceMaterial.GripStone,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius,
            UseCollisionHitPoint = true
        };
        targetBody.AddChild(surface);
        root.AddChild(targetBody);
        if (addFloor) root.AddChild(WorldBlocker(new Vector2(0f, floorY), new Vector2(1600f, 20f)));
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = playerPosition;
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        tree.Root.AddChild(root);
        var fixture = new Fixture(player, surface);
        try
        {
            await Samples(tree, fixture, 3);
            await AimAtSurface(tree, fixture);
            await test(fixture);
        }
        finally
        {
            tree.Paused = false;
            await ReleaseAll(tree);
            root.QueueFree();
            for (int index = 0; index < 2; index++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task SelectGrapple(SceneTree tree, Fixture fixture)
    {
        await Dispatch(tree, fixture, (InputActions.SelectGrappleMode, true));
        await Dispatch(tree, fixture, (InputActions.SelectGrappleMode, false));
        TestAssert.Equal(PlayerActionMode.Grapple, fixture.Modes.CurrentMode,
            "real E input selects Grapple mode");
    }

    private static async Task ClickForOneFrame(SceneTree tree, Fixture fixture)
    {
        await AimAtSurface(tree, fixture);
        await Dispatch(tree, fixture, (InputActions.PrimaryAction, true));
        TestAssert.True(fixture.Claw.CurrentFrame.State != MechanicalClawState.Idle,
            $"a real fresh primary edge begins the cast; sampled_pressed={fixture.Input.Current.PrimaryPressed}, " +
            $"sampled_mouse={fixture.Input.Current.MouseWorld}, anchor={fixture.Surface.GlobalPosition}, " +
            $"mode={fixture.Modes.CurrentMode}, state={fixture.Claw.CurrentFrame.State}, " +
            $"failure={fixture.Claw.CurrentFrame.Failure}");
        await Dispatch(tree, fixture, (InputActions.PrimaryAction, false));
    }

    private static async Task AimAtSurface(SceneTree tree, Fixture fixture)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Vector2 viewportPoint = fixture.Player.GetViewport().GetFinalTransform() *
            (fixture.Player.GetGlobalTransformWithCanvas() *
                fixture.Player.ToLocal(fixture.Surface.GlobalPosition));
        var motion = new InputEventMouseMotion
        {
            Position = viewportPoint,
            GlobalPosition = viewportPoint
        };
        fixture.Player.GetViewport().PushInput(motion, true);
        Input.ParseInputEvent(motion);
        Input.FlushBufferedEvents();
        await Samples(tree, fixture, 1);
        TestAssert.True(fixture.Input.HasObservedMouseMotion &&
            fixture.Input.Current.MouseWorld.DistanceTo(fixture.Surface.GlobalPosition) < .1f,
            $"real mouse motion aims at the authored point target; sampled={fixture.Input.Current.MouseWorld}, " +
            $"target={fixture.Surface.GlobalPosition}");
    }

    private static async Task Dispatch(SceneTree tree, Fixture fixture,
        params (StringName Action, bool Pressed)[] changes)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        foreach ((StringName action, bool pressed) in changes)
            Input.ParseInputEvent(new InputEventAction
            {
                Action = action,
                Pressed = pressed,
                Strength = pressed ? 1f : 0f
            });
        Input.FlushBufferedEvents();
        await Samples(tree, fixture, 1);
    }

    private static async Task Samples(SceneTree tree, Fixture fixture, int count)
    {
        ulong target = fixture.Input.CurrentFrameSequence + (ulong)count;
        for (int tick = 0; tick < count + 6 && fixture.Input.CurrentFrameSequence < target; tick++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(fixture.Input.CurrentFrameSequence >= target,
            "the real automatic InputReader advances under declared Player.tscn priorities");
    }

    private static async Task WaitForClaw(SceneTree tree, Fixture fixture,
        Func<MechanicalClawState, bool> predicate, string reason) =>
        await WaitFor(tree, fixture, () => predicate(fixture.Claw.CurrentFrame.State), reason);

    private static async Task WaitFor(SceneTree tree, Fixture fixture, Func<bool> predicate, string reason)
    {
        for (int tick = 0; tick < 120 && !predicate(); tick++) await Samples(tree, fixture, 1);
        TestAssert.True(predicate(), $"bounded automatic physics reaches {reason}; " +
            $"state={fixture.Claw.CurrentFrame.State}, position={fixture.Player.GlobalPosition}, " +
            $"velocity={fixture.Player.Velocity}");
    }

    private static bool IsAttached(MechanicalClawState state) => state is
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;

    private static GrappleSurface2D Surface(Vector2 position)
    {
        var surface = new GrappleSurface2D
        {
            Name = "ClawSurface",
            Position = position,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 12f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static StaticBody2D WorldBlocker(Vector2 position, Vector2 size)
    {
        var body = new StaticBody2D
        {
            Name = "WorldBlocker",
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = size }
        });
        return body;
    }

    private static async Task ReleaseAll(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        foreach (StringName action in new[]
        {
            InputActions.Shield, InputActions.PrimaryAction, InputActions.SelectAttackMode,
            InputActions.SelectGrappleMode, InputActions.MoveLeft, InputActions.MoveRight,
            InputActions.MoveUp, InputActions.MoveDown, InputActions.Jump, InputActions.Dash
        })
            Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false, Strength = 0f });
        Input.FlushBufferedEvents();
    }

    private sealed record Fixture(CharacterBody2D Player, GrappleSurface2D Surface)
    {
        public PlayerInputReader Input { get; } = Player.GetNode<PlayerInputReader>("Input");
        public PlayerActionModeController Modes { get; } = Player.GetNode<PlayerActionModeController>("ActionModes");
        public MechanicalClawController Claw { get; } = Player.GetNode<MechanicalClawController>("MechanicalClaw");
        public PlayerMotor Motor { get; } = Player.GetNode<PlayerMotor>("Motor");
        public PlayerCombat Combat { get; } = Player.GetNode<PlayerCombat>("Combat");
        public PlayerShieldController Shield { get; } = Player.GetNode<PlayerShieldController>("Shield");
        public PlayerEnergyController Energy { get; } = Player.GetNode<PlayerEnergyController>("Energy");
        public Node2D Root { get; } = Player.GetParent<Node2D>();
    }

}

internal sealed partial class PostSampleTargetLossNode : Node
{
    public PlayerInputReader Input { get; init; }
    public MechanicalClawController Claw { get; init; }
    public GrappleSurface2D Target { get; init; }
    public bool NotifyController { get; init; }
    public bool Armed { get; set; }
    public bool Triggered { get; private set; }
    public ulong TriggeredInputSequence { get; private set; }
    public bool Notified { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        if (!Armed || !Input.Current.PrimaryPressed) return;
        Armed = false;
        Triggered = true;
        TriggeredInputSequence = Input.CurrentFrameSequence;
        if (NotifyController)
            Notified = Claw.NotifyTargetWillUnload(Target);
        else
            Target.RemoveFromGroup(GrappleSurface2D.TargetGroup);
    }
}

// Diagnostic-only observer. It never supplies input, moves a body, or changes a constraint.
internal sealed partial class CeilingDockNeutralObserver : Node
{
    internal CharacterBody2D Player;
    internal PlayerInputReader InputReader;
    internal MechanicalClawController Claw;
    internal PlayerMotor Motor;
    internal GrappleSurface2D Target;
    internal Exception Failure { get; private set; }
    internal bool Started { get; private set; }
    internal bool Complete => _samples.Count == 120;

    private readonly List<Sample> _samples = new(120);
    private MechanicalClawFrame _previous, _published, _arrival;
    private ulong _publishedTick, _publishedSequence, _arrivalTick, _arrivalSequence;
    private ulong _playerId, _targetId;
    private int _initialHealth, _initialJumps, _initialDashes;
    private bool _subscribed;
    private CollisionShape2D _bodyCollider;
    private StaticBody2D _targetBody;

    private sealed record Sample(ulong Tick, ulong Sequence, MechanicalClawFrame Frame,
        Vector2 Position, Vector2 Velocity, Vector2 DerivedFreeBase, bool FreeBaseUnobstructed,
        float DockDistance, float Stretch, int Health, int Jumps, int Dashes,
        bool SameContract, bool TargetInsetOverlap);

    internal void Subscribe()
    {
        _playerId = Player.GetInstanceId();
        _targetId = Target.GetInstanceId();
        _initialHealth = Player.GetNode<HealthComponent>("Health").CurrentHealth;
        _initialJumps = Motor.AcceptedJumpCount;
        _initialDashes = Motor.AcceptedDashCount;
        _bodyCollider = Player.GetNode<CollisionShape2D>("BodyCollider");
        TestAssert.True(_bodyCollider.Shape is CapsuleShape2D,
            "ceiling observation queries the actual saved body capsule");
        _targetBody = Target.GetParent<StaticBody2D>();
        _previous = Claw.CurrentFrame;
        Claw.ClawFrameChanged += Published;
        _subscribed = true;
    }

    internal void Unsubscribe()
    {
        if (_subscribed) Claw.ClawFrameChanged -= Published;
        _subscribed = false;
        SetPhysicsProcess(false);
    }

    internal void ThrowFailure()
    {
        if (Failure != null)
            throw new InvalidOperationException("Ceiling neutral observation callback failed", Failure);
    }

    private void Published(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (Complete || Failure != null) return;
        try
        {
            _published = frame;
            _publishedTick = Engine.GetPhysicsFrames();
            _publishedSequence = InputReader.CurrentFrameSequence;
            if (!Started && _previous.State == MechanicalClawState.Reel &&
                frame.State == MechanicalClawState.Swing && frame.Arrived)
            {
                TestAssert.True(target == Target && frame.LockedTargetRuntimeId == _targetId &&
                    frame.CastId == _previous.CastId,
                    "ceiling observation starts on the original cast's actual target arrival");
                _arrival = frame;
                _arrivalTick = _publishedTick;
                _arrivalSequence = _publishedSequence;
                Started = true;
                GD.Print($"CEILING_NEUTRAL_ARRIVAL tick={_arrivalTick} sequence={_arrivalSequence} " +
                    $"cast={frame.CastId} target={frame.LockedTargetRuntimeId} player={_playerId} " +
                    $"pre_position={frame.CasterWorldPosition} pre_velocity={frame.CasterVelocity} " +
                    $"anchor={frame.AnchorPoint} dock={frame.DockPoint} rope={frame.RopeLength:F6} " +
                    $"gravity={Motor.Tuning.Gravity:F3} fall_cap={Motor.Tuning.MaxFallSpeed:F3}");
            }
            _previous = frame;
        }
        catch (Exception exception) { Failure ??= exception; }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Started || Complete || Failure != null) return;
        try
        {
            ulong tick = Engine.GetPhysicsFrames();
            if (tick == _arrivalTick) return; // First six are the six post-arrival ticks.
            ulong expectedOffset = (ulong)_samples.Count + 1UL;
            TestAssert.True(tick == _arrivalTick + expectedOffset &&
                InputReader.CurrentFrameSequence == _arrivalSequence + expectedOffset &&
                _publishedTick == tick && _publishedSequence == InputReader.CurrentFrameSequence,
                "ceiling observation requires one fresh publication and consecutive physics/input ticks");
            TestAssert.True(GodotObject.IsInstanceValid(Player) && Player.GetInstanceId() == _playerId &&
                GodotObject.IsInstanceValid(Target) && Target.GetInstanceId() == _targetId,
                "ceiling observation retains the same live player and original surface");
            PlayerInputFrame input = InputReader.Current;
            bool neutral = input.MoveAxis == 0f && input.Aim.IsZeroApprox() &&
                !input.JumpPressed && !input.JumpHeld && !input.DashPressed && !input.AttackPressed &&
                !input.ThreadPressed && !input.ThreadHeld && !input.ThreadReleased &&
                !input.AttackModePressed && !input.GrappleModePressed && input.RequestedSlot == 0 &&
                !input.PrimaryPressed && !input.PrimaryHeld && !input.PrimaryReleased &&
                !input.ShieldHeld && !input.ClawPressed && !input.InteractPressed &&
                !input.KeyboardShieldHeld && !input.LegacyShieldHeld;
            TestAssert.True(neutral, "ceiling observation has no sampled movement/action input after arrival");
            MechanicalClawFrame frame = _published;
            Vector2 postPosition = Player.GlobalPosition;
            Vector2 postVelocity = Player.Velocity;
            TestAssert.True(Finite(postPosition) && Finite(postVelocity) &&
                Finite(frame.CasterWorldPosition) && Finite(frame.CasterVelocity) &&
                Finite(frame.AnchorPoint) && Finite(frame.DockPoint) &&
                Finite(frame.VelocityCorrection) && float.IsFinite(frame.RopeLength),
                "ceiling observation records finite body and published constraint state");
            var contacts = new List<string>();
            for (int index = 0; index < Player.GetSlideCollisionCount(); index++)
            {
                using KinematicCollision2D collision = Player.GetSlideCollision(index);
                GodotObject collider = collision.GetCollider();
                contacts.Add($"id={collider?.GetInstanceId() ?? 0UL}," +
                    $"path={(collider as Node)?.GetPath().ToString() ?? "none"}," +
                    $"target={collider == _targetBody},normal={collision.GetNormal()}");
            }
            bool overlap = TargetInsetOverlap();
            bool sameContract = frame.CastId == _arrival.CastId &&
                frame.LockedTargetRuntimeId == _arrival.LockedTargetRuntimeId &&
                Claw.CurrentTarget == Target && frame.AnchorPoint == _arrival.AnchorPoint &&
                frame.DockPoint == _arrival.DockPoint &&
                Mathf.Abs(frame.RopeLength - _arrival.RopeLength) < .001f;
            Vector2 derivedFreeBase = postVelocity - frame.VelocityCorrection;
            bool freeBaseUnobstructed = contacts.Count == 0 && frame.State == MechanicalClawState.Swing;
            var sample = new Sample(tick, InputReader.CurrentFrameSequence, frame,
                postPosition, postVelocity, derivedFreeBase, freeBaseUnobstructed,
                postPosition.DistanceTo(_arrival.DockPoint),
                postPosition.DistanceTo(_arrival.AnchorPoint) - _arrival.RopeLength,
                Player.GetNode<HealthComponent>("Health").CurrentHealth,
                Motor.AcceptedJumpCount, Motor.AcceptedDashCount, sameContract, overlap);
            TestAssert.True(Finite(sample.DerivedFreeBase) && float.IsFinite(sample.DockDistance) &&
                float.IsFinite(sample.Stretch), "ceiling observation records finite derived motion");
            _samples.Add(sample);
            GD.Print($"CEILING_NEUTRAL_SAMPLE index={_samples.Count} tick={tick} sequence={sample.Sequence} " +
                $"cast={frame.CastId} target={frame.LockedTargetRuntimeId} same_contract={sameContract} " +
                $"state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} arrived={frame.Arrived} " +
                $"anchor={frame.AnchorPoint} dock={frame.DockPoint} rope={frame.RopeLength:F6} " +
                $"pre_position={frame.CasterWorldPosition} pre_velocity={frame.CasterVelocity} " +
                $"post_position={postPosition} post_velocity={postVelocity} correction={frame.VelocityCorrection} " +
                $"derived_free_base={derivedFreeBase} derived_free_base_unobstructed={freeBaseUnobstructed} " +
                $"dock_distance={sample.DockDistance:F6} stretch={sample.Stretch:F6} " +
                $"neutral={neutral} sampled_input={input} health={sample.Health} " +
                $"jumps={sample.Jumps} dashes={sample.Dashes} " +
                $"contacts=[{string.Join(';', contacts)}] original_target_inset_overlap={overlap}");
            if (Complete) SetPhysicsProcess(false);
        }
        catch (Exception exception) { Failure ??= exception; SetPhysicsProcess(false); }
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private bool TargetInsetOverlap()
    {
        var capsule = (CapsuleShape2D)_bodyCollider.Shape;
        using var inset = new CapsuleShape2D { Radius = capsule.Radius - .5f, Height = capsule.Height - 1f };
        var excluded = new Godot.Collections.Array<Rid>();
        using var excludedOwner = (Godot.Collections.Array)excluded;
        AddSelfRids(Player);
        using var query = new PhysicsShapeQueryParameters2D
        {
            Shape = inset, Transform = _bodyCollider.GlobalTransform,
            CollisionMask = CombatCollisionLayers.World,
            CollideWithBodies = true, CollideWithAreas = false, Exclude = excluded
        };
        var hits = Player.GetWorld2D().DirectSpaceState.IntersectShape(query, 32);
        using var owner = (Godot.Collections.Array)hits;
        bool overlap = false;
        foreach (Godot.Collections.Dictionary hit in hits)
        {
            using (hit)
                overlap |= hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == _targetBody;
        }
        return overlap;

        void AddSelfRids(Node node)
        {
            if (node is CollisionObject2D collision) excluded.Add(collision.GetRid());
            foreach (Node child in node.GetChildren()) AddSelfRids(child);
        }
    }

    internal void PrintSummary()
    {
        ThrowFailure();
        TestAssert.True(Complete, "ceiling diagnostic contains exactly 120 actual post-arrival samples");
        int firstOutside = _samples.FindIndex(sample => sample.DockDistance > 8.01f);
        int firstFallCap = _samples.FindIndex(sample => sample.FreeBaseUnobstructed &&
            sample.DerivedFreeBase.Y >= Motor.Tuning.MaxFallSpeed - .01f);
        float maxStretch = float.NegativeInfinity;
        foreach (Sample sample in _samples) maxStretch = Mathf.Max(maxStretch, sample.Stretch);
        List<Sample> firstSix = _samples.GetRange(0, 6);
        List<Sample> tail = _samples.GetRange(_samples.Count - 6, 6);
        GD.Print("CEILING_NEUTRAL_DIAGNOSTIC_COMPLETE samples=120 behavior_accepted=false " +
            "contact_safety_gate=separate_suite original_assertion_unchanged=true " +
            $"first_tick={_samples[0].Tick} last_tick={_samples[^1].Tick} " +
            $"first6_old_predicate={firstSix.TrueForAll(sample => sample.DockDistance <= 8.01f)} " +
            $"first6_unique_post_motor_distances=[{string.Join(',', firstSix.ConvertAll(sample => sample.DockDistance.ToString("F6")))}] " +
            $"firstOutside={(firstOutside < 0 ? -1 : firstOutside + 1)} maxStretch={maxStretch:F6} " +
            $"firstDerivedFreeFallCap={(firstFallCap < 0 ? -1 : firstFallCap + 1)} " +
            $"same_contract_all={_samples.TrueForAll(sample => sample.SameContract)} " +
            $"health_unchanged={_samples.TrueForAll(sample => sample.Health == _initialHealth)} " +
            $"jumps_unchanged={_samples.TrueForAll(sample => sample.Jumps == _initialJumps)} " +
            $"dashes_unchanged={_samples.TrueForAll(sample => sample.Dashes == _initialDashes)} " +
            $"target_overlap_any={_samples.Exists(sample => sample.TargetInsetOverlap)} " +
            $"late6_dock_distances=[{string.Join(',', tail.ConvertAll(sample => sample.DockDistance.ToString("F6")))}] " +
            $"late6_free_base_y=[{string.Join(',', tail.ConvertAll(sample => sample.DerivedFreeBase.Y.ToString("F6")))}] " +
            $"last20_vertical_drift={_samples[^1].Position.Y - _samples[^20].Position.Y:F6} " +
            "finite_window_only=true human=false whole_goal=false");
    }
}
