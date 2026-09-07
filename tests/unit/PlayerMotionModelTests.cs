using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerMotionModelTests
{
    public static void Run()
    {
        GroundAccelerationReachesConfiguredSpeedWithoutOvershoot();
        BufferedJumpFiresOnTheFirstGroundedFrame();
        CoyoteJumpFiresInsideTheConfiguredWindow();
        ReleasedJumpCutsUpwardVelocity();
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
