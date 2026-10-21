using System;
using Godot;
using Knight.Player;

namespace Knight.Tests.Presentation;

public readonly record struct HorizontalForecastResult(float NetDistance,
    float ForwardDistance, float Velocity, int Steps, bool SpeedThresholdReached, bool BrakeReachedZero);

// Route-driver arithmetic only. No node, collision, wind, claw or movement owner.
// Displacement follows Motor's one native move per tick using final step velocity.
public static class FreeMotionForecast
{
    public static HorizontalForecastResult Neutral(float velocity, bool onFloor,
        PlayerMotionSettings motion, MobilityModuleSettings module, double delta, int maxSteps, float speedThreshold) =>
        Predict(velocity, onFloor, motion, module, delta, maxSteps, speedThreshold, reverse: false);

    public static HorizontalForecastResult Reverse(float velocity, bool onFloor,
        PlayerMotionSettings motion, MobilityModuleSettings module, double delta, int maxSteps, float speedThreshold) =>
        Predict(velocity, onFloor, motion, module, delta, maxSteps, speedThreshold, reverse: true);

    private static HorizontalForecastResult Predict(float velocity, bool onFloor,
        PlayerMotionSettings motion, MobilityModuleSettings module, double delta,
        int maxSteps, float speedThreshold, bool reverse)
    {
        if (!double.IsFinite(delta) || delta <= 0d || delta > float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delta));
        if (maxSteps < 0) throw new ArgumentOutOfRangeException(nameof(maxSteps));
        if (!float.IsFinite(speedThreshold) || speedThreshold < 0f)
            throw new ArgumentOutOfRangeException(nameof(speedThreshold));
        float sign = Mathf.Sign(velocity);
        float axis = reverse ? -sign : 0f;
        // Validate even a zero-budget request without consuming a physical step.
        ThrusterDynamics.Step(new(new Vector2(velocity, 0f), axis, onFloor,
            Vector2.Zero, 0f, ApplyGravity: false), module, motion, 0d);
        float distance = 0f, peak = 0f;
        int steps = 0;
        bool reached = Mathf.Abs(velocity) <= speedThreshold, crossed = false;
        while (steps < maxSteps && !reached && !crossed)
        {
            var result = ThrusterDynamics.Step(new(new Vector2(velocity, 0f), axis,
                onFloor, Vector2.Zero, 0f, ApplyGravity: false), module, motion, delta);
            velocity = result.Velocity.X;
            distance += velocity * (float)delta;
            peak = Mathf.Max(peak, distance * sign);
            steps++;
            crossed = reverse && result.BrakeReachedZero;
            reached = Mathf.Abs(velocity) <= speedThreshold;
        }
        return new(distance, peak, velocity, steps, reached, crossed);
    }
}
