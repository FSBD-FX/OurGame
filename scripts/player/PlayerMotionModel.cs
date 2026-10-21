using Godot;

namespace Knight.Player;

public sealed class PlayerMotionModel
{
    private readonly PlayerMotionSettings _settings;
    internal PlayerMotionSettings Settings => _settings;

    public PlayerMotionModel(PlayerMotionSettings settings = null)
    {
        _settings = settings ?? new PlayerMotionSettings();
    }

    public bool IsDashing(PlayerMotionState state) => state.DashRemaining > 0f;

    // Actual Player path. The pulse owns duration/budget; this model owns only
    // ordinary jump lifecycle and delegates free velocity to the shared core.
    public PlayerMotionResult StepFree(PlayerMotionState state, PlayerInputFrame input,
        bool onFloor, double delta, MobilityModuleSettings module, Vector2 burnDirection,
        float burnRemaining, bool automaticPull, bool suppressJump)
    {
        float seconds = (float)delta;
        bool landed = onFloor && !state.WasOnFloor;
        bool jumpBlocked = suppressJump || burnRemaining > 0f;
        state.JumpBufferRemaining = jumpBlocked ? 0f : input.JumpPressed
            ? _settings.JumpBufferSeconds : Mathf.Max(0f, state.JumpBufferRemaining - seconds);
        state.CoyoteRemaining = onFloor ? _settings.CoyoteSeconds
            : Mathf.Max(0f, state.CoyoteRemaining - seconds);
        bool startedJump = !jumpBlocked && state.JumpBufferRemaining > 0f && state.CoyoteRemaining > 0f;
        if (startedJump)
        {
            state.GrappleRiseProtected = false;
            state.ThrusterRiseProtected = false;
            state.PreservingGrappleReleaseMomentum = false;
            state.Velocity = new Vector2(state.Velocity.X, -_settings.JumpSpeed);
            state.JumpBufferRemaining = 0f;
            state.CoyoteRemaining = 0f;
        }
        else if (!automaticPull && !state.GrappleRiseProtected && !state.ThrusterRiseProtected &&
            !input.JumpHeld && state.Velocity.Y < -_settings.JumpCutSpeed)
            state.Velocity = new Vector2(state.Velocity.X, -_settings.JumpCutSpeed);

        state.WasOnFloor = onFloor;
        if (automaticPull)
            return new PlayerMotionResult(state.Velocity, startedJump, false, false, landed);
        ThrusterDynamicsResult motion = ThrusterDynamics.Step(new(state.Velocity, input.MoveAxis,
            onFloor, burnDirection, burnRemaining, ApplyGravity: !startedJump), module, _settings, delta);
        return new PlayerMotionResult(motion.Velocity, startedJump, false, false, landed)
        {
            GravityVelocityDelta = motion.GravityVelocityDelta,
            ThrustVelocityDelta = motion.ThrustVelocityDelta,
            BurnRemaining = motion.BurnRemaining,
            Braking = motion.Braking,
            BrakeReachedZero = motion.BrakeReachedZero,
            CancelledForReverse = motion.CancelledForReverse
        };
    }

    public static Vector2 TautRopeGravitySupport(Vector2 gravityDelta, Vector2 fromAnchor,
        float ropeLength)
    {
        // A rope supports outward gravity only while taut. Its tangential gravity
        // still drives the swing; a negative terminal-speed clamp is not gravity to undo.
        if (gravityDelta.Y <= 0f || ropeLength <= 0f || fromAnchor.IsZeroApprox() ||
            fromAnchor.LengthSquared() < ropeLength * ropeLength) return Vector2.Zero;
        Vector2 outward = fromAnchor.Normalized();
        return -outward * Mathf.Max(0f, gravityDelta.Dot(outward));
    }

    public PlayerMotionResult Step(PlayerMotionState state, PlayerInputFrame input, bool onFloor, double delta)
    {
        float seconds = Mathf.Max(0f, (float)delta);
        bool landed = onFloor && !state.WasOnFloor;
        if (onFloor) state.PreservingGrappleReleaseMomentum = false;
        state.JumpBufferRemaining = input.JumpPressed
            ? _settings.JumpBufferSeconds
            : Mathf.Max(0f, state.JumpBufferRemaining - seconds);
        state.CoyoteRemaining = onFloor
            ? _settings.CoyoteSeconds
            : Mathf.Max(0f, state.CoyoteRemaining - seconds);
        if (landed) state.AirDashAvailable = true;

        bool hadActiveDash = state.DashRemaining > 0f;
        if (hadActiveDash)
        {
            // An active dash replaces even a claw launch released during the dash.
            // Its tail must retain ordinary dash behavior rather than the launch exemption.
            state.PreservingGrappleReleaseMomentum = false;
            state.DashRemaining = Mathf.Max(0f, state.DashRemaining - seconds);
            state.Velocity = state.DashDirection * _settings.DashSpeed;
            state.WasOnFloor = onFloor;
            return new PlayerMotionResult(state.Velocity, false, false, input.DashPressed, landed);
        }

        bool dashRequested = input.DashPressed;
        bool canStartDash = dashRequested && (onFloor || state.AirDashAvailable);
        bool dashRejected = dashRequested && !canStartDash;
        if (canStartDash)
        {
            state.PreservingGrappleReleaseMomentum = false;
            state.DashDirection = ResolveDashDirection(input, state);
            state.DashRemaining = _settings.DashSeconds;
            if (!onFloor || input.JumpPressed) state.AirDashAvailable = false;
            state.Velocity = state.DashDirection * _settings.DashSpeed;
            state.WasOnFloor = onFloor;
            return new PlayerMotionResult(state.Velocity, false, true, false, landed);
        }

        if (dashRejected)
        {
            state.WasOnFloor = onFloor;
            return new PlayerMotionResult(state.Velocity, false, false, true, landed);
        }

        bool startedJump = state.JumpBufferRemaining > 0f && state.CoyoteRemaining > 0f;
        if (startedJump)
        {
            state.PreservingGrappleReleaseMomentum = false;
            state.Velocity = new Vector2(state.Velocity.X, -_settings.JumpSpeed);
            state.JumpBufferRemaining = 0f;
            state.CoyoteRemaining = 0f;
        }
        else if (!state.PreservingGrappleReleaseMomentum &&
                 !input.JumpHeld && state.Velocity.Y < -_settings.JumpCutSpeed)
        {
            state.Velocity = new Vector2(state.Velocity.X, -_settings.JumpCutSpeed);
        }

        float targetX = input.MoveAxis * _settings.MaxRunSpeed;
        float acceleration = Mathf.IsZeroApprox(input.MoveAxis)
            ? _settings.GroundDeceleration
            : onFloor ? _settings.GroundAcceleration : _settings.AirAcceleration;
        state.Velocity = new Vector2(
            Mathf.MoveToward(state.Velocity.X, targetX, acceleration * seconds),
            state.Velocity.Y);
        Vector2 gravityDelta = Vector2.Zero;
        if (!onFloor && !startedJump)
        {
            float beforeGravityY = state.Velocity.Y;
            state.Velocity = new Vector2(state.Velocity.X,
                Mathf.Min(state.Velocity.Y + _settings.Gravity * seconds, _settings.MaxFallSpeed));
            gravityDelta = new Vector2(0f, state.Velocity.Y - beforeGravityY);
        }

        state.WasOnFloor = onFloor;
        return new PlayerMotionResult(state.Velocity, startedJump, false, false, landed)
        {
            GravityVelocityDelta = gravityDelta
        };
    }

    private static Vector2 ResolveDashDirection(PlayerInputFrame input, PlayerMotionState state)
    {
        if (!input.Aim.IsZeroApprox()) return input.Aim.Normalized();
        if (!Mathf.IsZeroApprox(input.MoveAxis)) return new Vector2(Mathf.Sign(input.MoveAxis), 0f);
        return state.DashDirection.IsZeroApprox() ? Vector2.Right : state.DashDirection.Normalized();
    }
}
