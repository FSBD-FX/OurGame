using System;
using Godot;
using Knight.Config;

namespace Knight.Player;

public readonly record struct ThrusterDynamicsInput(
    Vector2 Velocity,
    float MoveAxis,
    bool OnFloor,
    Vector2 BurnDirection,
    float BurnRemaining,
    bool ApplyGravity = true);

// Contributions compare the final capped velocity with the terminal-clamped
// unpowered baseline. Gravity may include terminal correction; thrust is the
// effective difference, not raw a*dt. These are per-step, not source ownership.
public readonly record struct ThrusterDynamicsResult(
    Vector2 Velocity,
    float BurnRemaining,
    float BurnUsedSeconds,
    Vector2 GravityVelocityDelta,
    Vector2 ThrustVelocityDelta,
    bool Braking,
    bool CancelledForReverse)
{
    // The same control branch may stop and reverse within one physics step.
    // This fact is not inferred from its final (possibly already negative) speed.
    public bool BrakeReachedZero { get; init; }
}

// Pure velocity arithmetic; the Motor remains the only native movement owner.
public static class ThrusterDynamics
{
    public static ThrusterDynamicsResult Step(ThrusterDynamicsInput input,
        MobilityModuleSettings module, PlayerMotionSettings motion, double delta)
    {
        Validate(input, module, motion, delta);
        float seconds = (float)delta;
        Vector2 direction = Normalize(input.BurnDirection);
        bool cancelled = seconds > 0f && input.BurnRemaining > 0f &&
            direction.X != 0f && input.MoveAxis * direction.X < 0f;
        float remaining = cancelled ? 0f : input.BurnRemaining;
        float used = Mathf.Min(remaining, seconds);
        Vector2 velocity = input.Velocity;
        Vector2 gravityDelta = Vector2.Zero;
        Vector2 thrustDelta = Vector2.Zero;
        bool braking = false;
        bool brakeReachedZero = false;
        IntegrateSegment(used, true);
        IntegrateSegment(seconds - used, false);
        if (!Finite(velocity) || !Finite(gravityDelta) || !Finite(thrustDelta))
            throw new ArgumentException("Velocity integration exceeds the finite arithmetic range", nameof(input));
        return new ThrusterDynamicsResult(velocity,
            Mathf.Max(0f, remaining - used), used, gravityDelta,
            thrustDelta, braking, cancelled) { BrakeReachedZero = brakeReachedZero };

        void IntegrateSegment(float duration, bool burning)
        {
            if (duration <= 0f) return;
            velocity = new Vector2(burning && direction.X != 0f
                ? Passive(velocity.X, duration) : Controlled(velocity.X, duration), velocity.Y);
            Vector2 beforeGravity = velocity;
            if (!input.OnFloor && input.ApplyGravity)
                velocity = new Vector2(velocity.X, velocity.Y + motion.Gravity * duration);
            // Accounting baseline only: the real projection calculation below
            // still sees gravity before its terminal-speed correction.
            Vector2 unpowered = BoundFall(velocity);
            gravityDelta += unpowered - beforeGravity;
            if (burning)
            {
                float addition = Mathf.Min(module.ThrustAcceleration * duration,
                    Mathf.Max(0f, module.PoweredProjectionLimit - velocity.Dot(direction)));
                velocity += direction * addition;
            }
            velocity = BoundFall(velocity);
            thrustDelta += velocity - unpowered;
        }

        Vector2 BoundFall(Vector2 value) => input.OnFloor ? value
            : new Vector2(value.X, Mathf.Min(value.Y, motion.MaxFallSpeed));

        float Passive(float speed, float duration) => input.OnFloor
            ? Mathf.MoveToward(speed, 0f, module.GroundFriction * duration)
            : speed * Mathf.Exp(-module.AirDragRate * duration);

        float Controlled(float speed, float duration)
        {
            if (input.MoveAxis == 0f) return Passive(speed, duration);
            float target = input.MoveAxis * motion.MaxRunSpeed;
            float acceleration = input.OnFloor ? motion.GroundAcceleration : motion.AirAcceleration;
            if (speed * input.MoveAxis < 0f)
            {
                float brake = input.OnFloor ? module.GroundReverseBrake : module.AirReverseBrake;
                float stoppingTime = Mathf.Abs(speed) / brake;
                braking = true;
                brakeReachedZero |= duration >= stoppingTime;
                if (duration <= stoppingTime) return Mathf.MoveToward(speed, 0f, brake * duration);
                return Mathf.MoveToward(0f, target, acceleration * (duration - stoppingTime));
            }
            if (Mathf.Abs(speed) > Mathf.Abs(target))
                return Mathf.Sign(target) * Mathf.Max(Mathf.Abs(target), Mathf.Abs(Passive(speed, duration)));
            return Mathf.MoveToward(speed, target, acceleration * duration);
        }
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static Vector2 Normalize(Vector2 value)
    {
        // Normalize finite author input without float length-squared overflow.
        double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
        return length == 0.0 ? Vector2.Zero
            : new Vector2((float)(value.X / length), (float)(value.Y / length));
    }

    private static void Validate(ThrusterDynamicsInput input, MobilityModuleSettings module,
        PlayerMotionSettings motion, double delta)
    {
        if (!double.IsFinite(delta) || delta < 0.0 || delta > float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delta), "delta must be finite, nonnegative and float-representable");
        var moduleErrors = MobilityModuleDefinition.Validate(module);
        if (moduleErrors.Count != 0)
            throw new ArgumentException(string.Join("; ", moduleErrors), nameof(module));
        ArgumentNullException.ThrowIfNull(motion);
        if (!float.IsFinite(motion.MaxRunSpeed) || motion.MaxRunSpeed <= 0f ||
            !float.IsFinite(motion.GroundAcceleration) || motion.GroundAcceleration <= 0f ||
            !float.IsFinite(motion.AirAcceleration) || motion.AirAcceleration <= 0f ||
            !float.IsFinite(motion.Gravity) || motion.Gravity < 0f ||
            !float.IsFinite(motion.MaxFallSpeed) || motion.MaxFallSpeed <= 0f)
            throw new ArgumentException("motion requires finite positive speed/accelerations and nonnegative gravity", nameof(motion));
        if (!Finite(input.Velocity))
            throw new ArgumentException("Velocity must be finite", nameof(input));
        if (!float.IsFinite(input.MoveAxis) || input.MoveAxis < -1f || input.MoveAxis > 1f)
            throw new ArgumentException("MoveAxis must be finite and in [-1,1]", nameof(input));
        if (!Finite(input.BurnDirection) || (input.BurnRemaining > 0f && input.BurnDirection == Vector2.Zero))
            throw new ArgumentException("BurnDirection must be finite and nonzero during a burn", nameof(input));
        if (!float.IsFinite(input.BurnRemaining) || input.BurnRemaining < 0f || input.BurnRemaining > module.BurnDuration)
            throw new ArgumentException("BurnRemaining must be finite and in [0,BurnDuration]", nameof(input));
    }
}
