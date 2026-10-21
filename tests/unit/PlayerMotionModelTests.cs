using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerMotionModelTests
{
    public static void RunGravityDeltaRegression()
    {
        GravityDeltaReportsActualAirAccelerationAndTerminalClamp();
        GravityDeltaExcludesReleasedJumpCut();
        GravityDeltaExcludesGroundedAndCoyoteJumpLaunches();
        GravityDeltaIsZeroOnAuthoritativeLanding();
        GravityDeltaIsZeroForNewActiveAndRejectedDashes();
        GravityDeltaReportsExistingZeroDeltaClampSemantics();
        GD.Print("PLAYER_MOTION_GRAVITY_DELTA_OK");
    }

    public static void RunRopeGravitySupportRegression()
    {
        // Literal vector examples distinguish radial support from replacing the
        // complete velocity or cancelling tangential gravity. No model helper
        // derives the expected result.
        foreach (var sample in new[]
        {
            (Name: "VerticalTaut", Gravity: new Vector2(0f, 30f), Offset: new Vector2(0f, 60f),
                Rope: 48f, Expected: new Vector2(0f, -30f)),
            (Name: "HorizontalTaut", Gravity: new Vector2(0f, 30f), Offset: new Vector2(60f, 0f),
                Rope: 48f, Expected: Vector2.Zero),
            (Name: "DiagonalTaut", Gravity: new Vector2(0f, 30f), Offset: new Vector2(30f, 40f),
                Rope: 48f, Expected: new Vector2(-14.4f, -19.2f)),
            (Name: "MirroredDiagonalTaut", Gravity: new Vector2(0f, 30f), Offset: new Vector2(-30f, 40f),
                Rope: 48f, Expected: new Vector2(14.4f, -19.2f)),
            (Name: "AnchorBelow", Gravity: new Vector2(0f, 30f), Offset: new Vector2(0f, -60f),
                Rope: 48f, Expected: Vector2.Zero),
            (Name: "Slack", Gravity: new Vector2(0f, 30f), Offset: new Vector2(0f, 40f),
                Rope: 48f, Expected: Vector2.Zero),
            (Name: "ExactlyTaut", Gravity: new Vector2(0f, 30f), Offset: new Vector2(0f, 48f),
                Rope: 48f, Expected: new Vector2(0f, -30f)),
            (Name: "ZeroOffset", Gravity: new Vector2(0f, 30f), Offset: Vector2.Zero,
                Rope: 48f, Expected: Vector2.Zero),
            (Name: "InvalidZeroRope", Gravity: new Vector2(0f, 30f), Offset: new Vector2(0f, 60f),
                Rope: 0f, Expected: Vector2.Zero),
            (Name: "TerminalClampAboveAnchor", Gravity: new Vector2(0f, -200f), Offset: new Vector2(0f, 60f),
                Rope: 48f, Expected: Vector2.Zero),
            (Name: "TerminalClampBelowAnchor", Gravity: new Vector2(0f, -200f), Offset: new Vector2(0f, -60f),
                Rope: 48f, Expected: Vector2.Zero),
            (Name: "ZeroGravityDelta", Gravity: Vector2.Zero, Offset: new Vector2(0f, 60f),
                Rope: 48f, Expected: Vector2.Zero)
        })
        {
            Vector2 support = PlayerMotionModel.TautRopeGravitySupport(sample.Gravity, sample.Offset, sample.Rope);

            TestAssert.True(float.IsFinite(support.X) && float.IsFinite(support.Y),
                $"TautRopeGravitySupport{sample.Name}: support must be a finite velocity delta");
            TestAssert.Near(sample.Expected.X, support.X, .001f,
                $"TautRopeGravitySupport{sample.Name}: horizontal support preserves the tangential component");
            TestAssert.Near(sample.Expected.Y, support.Y, .001f,
                $"TautRopeGravitySupport{sample.Name}: support opposes only positive outward gravity on a taut rope");
        }
        GD.Print("PLAYER_MOTION_ROPE_GRAVITY_SUPPORT_OK");
    }

    private static void GravityDeltaReportsActualAirAccelerationAndTerminalClamp()
    {
        // At .02 seconds, ordinary gravity adds 37. Horizontal input separately
        // adds 30, which must never be reported as part of gravity's vector.
        foreach (var sample in new[]
        {
            (Name: "OrdinaryAir", StartY: 120f, FinalY: 157f, GravityY: 37f),
            (Name: "NearTerminalCap", StartY: 770f, FinalY: 780f, GravityY: 10f),
            (Name: "AtTerminalCap", StartY: 780f, FinalY: 780f, GravityY: 0f),
            (Name: "AboveTerminalCap", StartY: 980f, FinalY: 780f, GravityY: -200f)
        })
        {
            var model = new PlayerMotionModel();
            var state = new PlayerMotionState
            {
                Velocity = new Vector2(100f, sample.StartY),
                WasOnFloor = false,
                CoyoteRemaining = 0f,
                JumpBufferRemaining = 0f,
                DashRemaining = 0f
            };
            var input = new PlayerInputFrame(1f, Vector2.Right, false, false, false, false, false);

            PlayerMotionResult result = model.Step(state, input, false, .02d);

            AssertGravityResult(result, new Vector2(130f, sample.FinalY),
                new Vector2(0f, sample.GravityY), sample.Name);
            TestAssert.True(!result.StartedJump && !result.StartedDash && !result.DashRejected && !result.Landed,
                $"GravityVelocityDelta{sample.Name}: the sample executes ordinary airborne movement");
        }
    }

    private static void GravityDeltaExcludesReleasedJumpCut()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState
        {
            Velocity = new Vector2(100f, -590f),
            PreservingGrappleReleaseMomentum = false,
            WasOnFloor = false,
            CoyoteRemaining = 0f
        };
        var released = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult result = model.Step(state, released, false, .02d);

        // Cut adds 360 and gravity adds 37: only the latter belongs in the field.
        // Neutral horizontal deceleration separately changes 100 to 44.
        AssertGravityResult(result, new Vector2(44f, -193f), new Vector2(0f, 37f), "ReleasedJumpCut");
        TestAssert.True(!result.StartedJump, "GravityVelocityDeltaReleasedJumpCut: this is not a new jump");
    }

    private static void GravityDeltaExcludesGroundedAndCoyoteJumpLaunches()
    {
        var model = new PlayerMotionModel();
        var jump = new PlayerInputFrame(0f, Vector2.Right, true, true, false, false, false);
        var grounded = new PlayerMotionState { WasOnFloor = true, Velocity = Vector2.Zero };

        PlayerMotionResult groundJump = model.Step(grounded, jump, true, .02d);

        AssertGravityResult(groundJump, new Vector2(0f, -590f), Vector2.Zero, "GroundJump");
        TestAssert.True(groundJump.StartedJump && !groundJump.StartedDash,
            "GravityVelocityDeltaGroundJump: actual floor permission starts the ordinary jump");

        // Earn coyote time from a real preceding grounded model step.
        var coyote = new PlayerMotionState { WasOnFloor = true, Velocity = Vector2.Zero };
        var idle = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);
        model.Step(coyote, idle, true, .02d);
        PlayerMotionResult coyoteJump = model.Step(coyote, jump, false, .04d);

        AssertGravityResult(coyoteJump, new Vector2(0f, -590f), Vector2.Zero, "CoyoteJump");
        TestAssert.True(coyoteJump.StartedJump && !coyoteJump.Landed,
            "GravityVelocityDeltaCoyoteJump: earned coyote permission, not floor contact, starts this jump");
    }

    private static void GravityDeltaIsZeroOnAuthoritativeLanding()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState
        {
            Velocity = new Vector2(100f, 600f),
            WasOnFloor = false,
            AirDashAvailable = false,
            CoyoteRemaining = 0f,
            JumpBufferRemaining = 0f
        };
        var idle = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult result = model.Step(state, idle, true, .02d);

        // The motion model does not manufacture a collision velocity reset;
        // native collision is Motor's job. Its floor branch adds no gravity.
        AssertGravityResult(result, new Vector2(44f, 600f), Vector2.Zero, "Landing");
        TestAssert.True(result.Landed && state.AirDashAvailable && !result.StartedJump,
            "GravityVelocityDeltaLanding: real airborne-to-floor transition restores the dash without launching");
    }

    private static void GravityDeltaIsZeroForNewActiveAndRejectedDashes()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState
        {
            Velocity = new Vector2(100f, 120f),
            WasOnFloor = false,
            AirDashAvailable = true,
            DashRemaining = 0f
        };
        var upDash = new PlayerInputFrame(0f, Vector2.Up, false, false, true, false, false);
        var rightDash = new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false);
        var idle = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult started = model.Step(state, upDash, false, .02d);
        AssertGravityResult(started, new Vector2(0f, -620f), Vector2.Zero, "NewDash");
        TestAssert.True(started.StartedDash && !state.AirDashAvailable,
            "GravityVelocityDeltaNewDash: the accepted upward dash consumes its airborne permission");

        PlayerMotionResult active = model.Step(state, idle, false, .02d);
        AssertGravityResult(active, new Vector2(0f, -620f), Vector2.Zero, "ActiveDash");
        TestAssert.True(model.IsDashing(state) && !active.StartedDash && !active.DashRejected,
            "GravityVelocityDeltaActiveDash: a continuing dash, not a fresh launch, owns velocity");

        PlayerMotionResult activeRejected = model.Step(state, rightDash, false, .02d);
        AssertGravityResult(activeRejected, new Vector2(0f, -620f), Vector2.Zero, "ActiveDashRejection");
        TestAssert.True(activeRejected.DashRejected && !activeRejected.StartedDash,
            "GravityVelocityDeltaActiveDashRejection: a repeated press cannot replace the active direction");

        PlayerMotionResult lastActive = model.Step(state, idle, false, .2d);
        AssertGravityResult(lastActive, new Vector2(0f, -620f), Vector2.Zero, "DashExpiryTick");
        TestAssert.True(!model.IsDashing(state),
            "GravityVelocityDeltaDashExpiryTick: the last active step expires the original dash");

        PlayerMotionResult rejected = model.Step(state, rightDash, false, .02d);
        AssertGravityResult(rejected, new Vector2(0f, -620f), Vector2.Zero, "SpentAirDashRejection");
        TestAssert.True(rejected.DashRejected && !rejected.StartedDash && !state.AirDashAvailable,
            "GravityVelocityDeltaSpentAirDashRejection: rejection preserves velocity and skips ordinary gravity");
    }

    private static void GravityDeltaReportsExistingZeroDeltaClampSemantics()
    {
        foreach (var sample in new[]
        {
            (Name: "ZeroDeltaOrdinary", StartY: 120f, FinalY: 120f, GravityY: 0f),
            (Name: "ZeroDeltaJumpCut", StartY: -590f, FinalY: -230f, GravityY: 0f),
            (Name: "ZeroDeltaAboveCap", StartY: 980f, FinalY: 780f, GravityY: -200f)
        })
        {
            var model = new PlayerMotionModel();
            var state = new PlayerMotionState
            {
                Velocity = new Vector2(100f, sample.StartY),
                WasOnFloor = false,
                CoyoteRemaining = 0f,
                PreservingGrappleReleaseMomentum = false
            };
            var idle = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

            PlayerMotionResult result = model.Step(state, idle, false, 0d);

            // Existing zero-time steps still apply JumpCut and the terminal clamp.
            // Report the clamp's actual delta, without relabelling JumpCut as gravity.
            AssertGravityResult(result, new Vector2(100f, sample.FinalY),
                new Vector2(0f, sample.GravityY), sample.Name);
        }
    }

    private static void AssertGravityResult(PlayerMotionResult result, Vector2 expectedVelocity,
        Vector2 expectedGravity, string sample)
    {
        TestAssert.True(float.IsFinite(result.Velocity.X) && float.IsFinite(result.Velocity.Y) &&
            float.IsFinite(result.GravityVelocityDelta.X) && float.IsFinite(result.GravityVelocityDelta.Y),
            $"GravityVelocityDelta{sample}: motion and reported gravity must remain finite");
        TestAssert.Near(expectedVelocity.X, result.Velocity.X, .001f,
            $"GravityVelocityDelta{sample}: ordinary horizontal motion is preserved");
        TestAssert.Near(expectedVelocity.Y, result.Velocity.Y, .001f,
            $"GravityVelocityDelta{sample}: ordinary vertical motion is preserved");
        TestAssert.Near(expectedGravity.X, result.GravityVelocityDelta.X, .001f,
            $"GravityVelocityDelta{sample}: horizontal motion is not a gravity contribution");
        TestAssert.Near(expectedGravity.Y, result.GravityVelocityDelta.Y, .001f,
            $"GravityVelocityDelta{sample}: report only the actual gravity-and-terminal-clamp contribution");
    }

    public static void Run()
    {
        GroundAccelerationReachesConfiguredSpeedWithoutOvershoot();
        BufferedJumpFiresOnTheFirstGroundedFrame();
        CoyoteJumpFiresInsideTheConfiguredWindow();
        ReleasedJumpCutsUpwardVelocity();
        NewDashReplacesPreviousGrappleLaunch();
        ActiveDashReplacesMomentumReleasedDuringTheDash();
        AirDashCanOnlyBeConsumedOnceBeforeLanding();
        LandingRestoresTheAirDash();
        SpaceUpDashThreadThenDashRejectsSecondDashBeforeLanding();
        SimultaneousSpaceUpDashThreadThenDashRejectsSecondDashBeforeLanding();
        ThreadImpulseDashThenDashRejectsSecondDashBeforeLanding();
        WallCeilingPauseAttackAndBounceNeverRefundAirDash();
        AirborneRoomTransitionWithoutFloorContactNeverRefundsAirDash();
        AuthoritativeLandingRefundsExactlyOneAirDash();
        DashPressedDuringActiveDashIsRejectedWithoutChangingVelocity();
        GroundDashPreservesAirDashForTakeoff();
    }

    private static void GroundAccelerationReachesConfiguredSpeedWithoutOvershoot()
    {
        var settings = new PlayerMotionSettings(MaxRunSpeed: 260f, GroundAcceleration: 2200f);
        var model = new PlayerMotionModel(settings);
        var state = new PlayerMotionState();
        var input = new PlayerInputFrame(1f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult result = model.Step(state, input, true, 0.25d);

        TestAssert.Near(260f, result.Velocity.X, 0.001f,
            "ground acceleration reaches configured maximum without overshooting it");
    }

    private static void BufferedJumpFiresOnTheFirstGroundedFrame()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState { JumpBufferRemaining = 0f };
        var pressJump = new PlayerInputFrame(0f, Vector2.Right, true, true, false, false, false);

        model.Step(state, pressJump, false, 0.016d);
        PlayerMotionResult result = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, true, false, false, false), true, 0.016d);

        TestAssert.True(result.StartedJump, "a jump pressed shortly before landing fires on the first grounded frame");
        TestAssert.True(result.Velocity.Y < 0f, "buffered jump applies upward velocity");
    }

    private static void CoyoteJumpFiresInsideTheConfiguredWindow()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        model.Step(state, new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false), true, 0.016d);

        PlayerMotionResult result = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, true, true, false, false, false), false, 0.05d);

        TestAssert.True(result.StartedJump, "jump starts during the configured coyote-time window");
        TestAssert.True(result.Velocity.Y < 0f, "coyote jump applies upward velocity");
    }

    private static void ReleasedJumpCutsUpwardVelocity()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState { Velocity = new Vector2(0f, -590f) };

        PlayerMotionResult result = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false), false, 0.016d);

        TestAssert.True(result.Velocity.Y >= -230f,
            "releasing jump cuts an upward velocity above the configured cut speed");
    }

    private static void NewDashReplacesPreviousGrappleLaunch()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState
        {
            Velocity = new Vector2(0f, -360f),
            PreservingGrappleReleaseMomentum = true
        };
        var idle = new PlayerInputFrame(0f, Vector2.Up, false, false, false, false, false);
        PlayerMotionResult dash = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, false, false, true, false, false), false, 1d / 60d);
        TestAssert.True(dash.StartedDash, "a new upward dash starts after releasing the claw");
        model.Step(state, idle, false, 0.2d);
        PlayerMotionResult result = model.Step(state, idle, false, 1d / 60d);
        TestAssert.Near(-230f + 1850f / 60f, result.Velocity.Y, 0.01f,
            "a new dash replaces grapple momentum and ends with the ordinary dash release behavior");
    }

    private static void ActiveDashReplacesMomentumReleasedDuringTheDash()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        var idle = new PlayerInputFrame(0f, Vector2.Up, false, false, false, false, false);
        PlayerMotionResult dash = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, false, false, true, false, false), false, 1d / 60d);
        TestAssert.True(dash.StartedDash, "the upward dash begins before the claw is released");

        // ApplyGrappleRelease can deliver these observed launch values while a dash is active.
        // The next active dash tick replaces that launch; it must not inherit its short-hop exemption.
        state.Velocity = new Vector2(0f, -360f);
        state.PreservingGrappleReleaseMomentum = true;
        for (int frame = 0; frame < 30 && model.IsDashing(state); frame++)
            model.Step(state, idle, false, 1d / 60d);
        TestAssert.True(!model.IsDashing(state), "the original dash ends within its normal duration");
        PlayerMotionResult result = model.Step(state, idle, false, 1d / 60d);
        TestAssert.Near(-230f + 1850f / 60f, result.Velocity.Y, 0.01f,
            "releasing a claw during an active dash cannot extend the upward dash tail");
        TestAssert.True(!state.AirDashAvailable, "claw release does not refund the consumed air dash");
    }

    private static void AirDashCanOnlyBeConsumedOnceBeforeLanding()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        var dash = new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false);
        var noDash = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult first = model.Step(state, dash, false, 0.016d);
        model.Step(state, noDash, false, 0.14d);
        model.Step(state, noDash, false, 0.001d);
        PlayerMotionResult second = model.Step(state, dash, false, 0.016d);

        TestAssert.True(first.StartedDash, "first airborne dash is accepted");
        TestAssert.True(!second.StartedDash, "second airborne dash is rejected before landing");
    }

    private static void LandingRestoresTheAirDash()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        var dash = new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false);
        var noDash = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult firstDash = model.Step(state, dash, false, 0.016d);
        model.Step(state, noDash, false, 0.14d);
        model.Step(state, noDash, false, 0.001d);
        model.Step(state, noDash, true, 0.016d);
        PlayerMotionResult result = model.Step(state,
            dash, false, 0.016d);

        TestAssert.True(firstDash.StartedDash, "initial airborne dash is consumed before landing");
        TestAssert.True(result.StartedDash, "landing restores the consumed air dash");
    }

    private static void SpaceUpDashThreadThenDashRejectsSecondDashBeforeLanding()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();

        model.Step(state, new PlayerInputFrame(0f, Vector2.Up, true, true, false, false, false), true, 1d / 60d);
        PlayerMotionResult first = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, false, true, true, false, false), false, 1d / 60d);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Up, false, true, false, false, true), false, 0.14d);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult second = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, false, true, true, false, false), false, 1d / 60d);

        AssertOneDashThenRejection(first, second, state, velocityBeforeRejectedPress,
            "Space then up-dash then thread");
    }

    private static void SimultaneousSpaceUpDashThreadThenDashRejectsSecondDashBeforeLanding()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();

        PlayerMotionResult first = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, true, true, true, false, true), true, 1d / 60d);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Up, false, false, false, false, false), false, 0.14d);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult second = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, false, false, true, false, false), false, 1d / 60d);

        TestAssert.True(first.Velocity.Y < 0f,
            "landed simultaneous Space plus up-dash launches upward in its first movement result");
        AssertOneDashThenRejection(first, second, state, velocityBeforeRejectedPress,
            "landed simultaneous Space plus up-dash plus thread");
    }

    private static void ThreadImpulseDashThenDashRejectsSecondDashBeforeLanding()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState { Velocity = new Vector2(140f, -410f) };

        model.Step(state, new PlayerInputFrame(0f, Vector2.Up, false, false, false, false, true), false, 1d / 60d);
        PlayerMotionResult first = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false), false, 1d / 60d);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false), false, 0.14d);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult second = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false), false, 1d / 60d);

        AssertOneDashThenRejection(first, second, state, velocityBeforeRejectedPress,
            "thread impulse then dash");
    }

    private static void WallCeilingPauseAttackAndBounceNeverRefundAirDash()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        PlayerMotionResult first = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false), false, 1d / 60d);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false), false, 0.14d);

        state.Velocity = new Vector2(0f, -360f);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Up, false, false, false, true, false), false, 0d);
        state.Velocity = new Vector2(-220f, 0f);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Left, false, false, false, false, false), false, 1d / 60d);
        state.Velocity = new Vector2(120f, -360f);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult second = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Down, false, false, true, true, false), false, 1d / 60d);

        AssertOneDashThenRejection(first, second, state, velocityBeforeRejectedPress,
            "wall, ceiling, pause, attack, and bounce");
    }

    private static void AirborneRoomTransitionWithoutFloorContactNeverRefundsAirDash()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        PlayerMotionResult first = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false), false, 1d / 60d);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false), false, 0.14d);
        model.Step(state, new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false), false, 1d / 60d);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult second = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false), false, 1d / 60d);

        AssertOneDashThenRejection(first, second, state, velocityBeforeRejectedPress,
            "airborne room transition without floor contact");
    }

    private static void AuthoritativeLandingRefundsExactlyOneAirDash()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        var dash = new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false);
        var idle = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);

        PlayerMotionResult first = model.Step(state, dash, false, 1d / 60d);
        model.Step(state, idle, false, 0.14d);
        PlayerMotionResult landed = model.Step(state, idle, true, 1d / 60d);
        model.Step(state, idle, false, 1d / 60d);
        PlayerMotionResult afterLanding = model.Step(state, dash, false, 1d / 60d);
        model.Step(state, idle, false, 0.14d);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult secondAfterLanding = model.Step(state, dash, false, 1d / 60d);

        TestAssert.True(first.StartedDash, "the initial airborne dash is accepted");
        TestAssert.True(landed.Landed, "an airborne-to-floor edge is the authoritative landing");
        AssertOneDashThenRejection(afterLanding, secondAfterLanding, state, velocityBeforeRejectedPress,
            "the airborne cycle after authoritative landing");
    }

    private static void DashPressedDuringActiveDashIsRejectedWithoutChangingVelocity()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        PlayerMotionResult first = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false), false, 1d / 60d);
        Vector2 activeDashVelocity = first.Velocity;

        PlayerMotionResult repeatedPress = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Up, false, false, true, false, false), false, 1d / 60d);

        TestAssert.True(first.StartedDash, "the first airborne press starts the active dash");
        TestAssert.True(repeatedPress.DashRejected,
            "a new press during the active dash publishes rejected feedback");
        TestAssert.True(!repeatedPress.StartedDash,
            "a new press during the active dash does not publish another successful transition");
        TestAssert.True(repeatedPress.Velocity.IsEqualApprox(activeDashVelocity),
            "a rejected press during active dash preserves the authoritative dash vector");
        TestAssert.True(!state.AirDashAvailable,
            "a rejected press during active dash leaves the airborne budget consumed");
    }

    private static void GroundDashPreservesAirDashForTakeoff()
    {
        var model = new PlayerMotionModel();
        var state = new PlayerMotionState();
        var idle = new PlayerInputFrame(0f, Vector2.Right, false, false, false, false, false);
        var dash = new PlayerInputFrame(0f, Vector2.Right, false, false, true, false, false);

        PlayerMotionResult groundDash = model.Step(state, dash, true, 1d / 60d);
        model.Step(state, idle, true, 0.14d);
        PlayerMotionResult takeoff = model.Step(state,
            new PlayerInputFrame(0f, Vector2.Right, true, true, false, false, false), true, 1d / 60d);
        model.Step(state, idle, false, 1d / 60d);
        PlayerMotionResult airDash = model.Step(state, dash, false, 1d / 60d);
        model.Step(state, idle, false, 0.14d);
        Vector2 velocityBeforeRejectedPress = state.Velocity;
        PlayerMotionResult secondAirDash = model.Step(state, dash, false, 1d / 60d);

        TestAssert.True(groundDash.StartedDash, "ground dash starts without consuming airborne budget");
        TestAssert.True(takeoff.StartedJump, "real grounded jump begins the airborne cycle after ground dash");
        AssertOneDashThenRejection(airDash, secondAirDash, state, velocityBeforeRejectedPress,
            "ground dash then takeoff airborne cycle");
    }

    private static void AssertOneDashThenRejection(PlayerMotionResult first, PlayerMotionResult second,
        PlayerMotionState state, Vector2 velocityBeforeRejectedPress, string sequence)
    {
        TestAssert.True(first.StartedDash, $"{sequence} starts exactly one airborne dash");
        TestAssert.True(!first.DashRejected, $"{sequence} accepts its first airborne dash");
        TestAssert.True(!second.StartedDash, $"{sequence} does not start a second airborne dash");
        TestAssert.True(second.DashRejected, $"{sequence} explicitly rejects the second dash press");
        TestAssert.True(!state.AirDashAvailable, $"{sequence} leaves the airborne-cycle dash consumed");
        TestAssert.True(second.Velocity.IsEqualApprox(velocityBeforeRejectedPress),
            $"{sequence} preserves pre-press velocity when the second dash is rejected");
    }
}
