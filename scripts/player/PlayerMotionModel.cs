using Godot;

namespace Knight.Player;

public sealed class PlayerMotionModel
{
    private readonly PlayerMotionSettings _settings;

    public PlayerMotionModel(PlayerMotionSettings settings = null)
    {
        _settings = settings ?? new PlayerMotionSettings();
    }

    public bool IsDashing(PlayerMotionState state) => state.DashRemaining > 0f;

    public PlayerMotionResult Step(PlayerMotionState state, PlayerInputFrame input, bool onFloor, double delta)
    {
        float seconds = Mathf.Max(0f, (float)delta);
        bool landed = onFloor && !state.WasOnFloor;
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
            state.Velocity = new Vector2(state.Velocity.X, -_settings.JumpSpeed);
            state.JumpBufferRemaining = 0f;
            state.CoyoteRemaining = 0f;
        }
        else if (!input.JumpHeld && state.Velocity.Y < -_settings.JumpCutSpeed)
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
        if (!onFloor && !startedJump)
            state.Velocity = new Vector2(state.Velocity.X,
                Mathf.Min(state.Velocity.Y + _settings.Gravity * seconds, _settings.MaxFallSpeed));

        state.WasOnFloor = onFloor;
        return new PlayerMotionResult(state.Velocity, startedJump, false, false, landed);
    }

    private static Vector2 ResolveDashDirection(PlayerInputFrame input, PlayerMotionState state)
    {
        if (!input.Aim.IsZeroApprox()) return input.Aim.Normalized();
        if (!Mathf.IsZeroApprox(input.MoveAxis)) return new Vector2(Mathf.Sign(input.MoveAxis), 0f);
        return state.DashDirection.IsZeroApprox() ? Vector2.Right : state.DashDirection.Normalized();
    }
}
