using System;
using Godot;

namespace Knight.Presentation;

public readonly record struct SecondaryMotionInput(
    Vector2 Velocity,
    Vector2 Acceleration,
    float Turn,
    float LandingImpulse,
    float ContactImpulse,
    PlayerActionPhase ThreadPhase,
    float ThreadEnergy);

public readonly record struct SecondaryMotionFrame(
    PoseTransform2D Mantle,
    PoseTransform2D Lantern,
    PoseTransform2D ShoulderSpool,
    PoseTransform2D Tether)
{
    public static SecondaryMotionFrame Identity { get; } = new(
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity);
}

public static class SecondaryMotionLimits
{
    public static readonly Vector2 MantlePosition = new(12f, 7f);
    public const float MantleRotation = 0.42f;
    public static readonly Vector2 LanternPosition = new(9f, 11f);
    public const float LanternRotation = 0.34f;
    public static readonly Vector2 ShoulderSpoolPosition = new(3f, 4f);
    public const float ShoulderSpoolRotation = 0.52f;
    public static readonly Vector2 TetherPosition = new(8f, 9f);
    public const float TetherRotation = 0.38f;
    public const float PartitionTolerance = 0.001f;
}

public sealed class SecondaryMotionModel
{
    private ChannelSpring _mantle = new(8f);
    private ChannelSpring _lantern = new(11f);
    private ChannelSpring _spool = new(16f);
    private ChannelSpring _tether = new(13f);
    private SecondaryMotionFrame _lastFrame = SecondaryMotionFrame.Identity;

    public SecondaryMotionFrame Step(SecondaryMotionInput input, float deltaSeconds)
    {
        if (!IsFinite(deltaSeconds) || deltaSeconds <= 0f) return _lastFrame;

        SecondaryMotionInput safe = Sanitize(input);
        Vector2 mantleTarget = ClampPosition(
            new Vector2(-safe.Acceleration.X * 0.010f - safe.Velocity.X * 0.018f,
                safe.Turn * 2.5f),
            SecondaryMotionLimits.MantlePosition);
        float mantleRotation = Clamp(
            -safe.Acceleration.X * 0.00045f - safe.Velocity.X * 0.00065f - safe.Turn * 0.10f,
            SecondaryMotionLimits.MantleRotation);

        Vector2 lanternTarget = ClampPosition(
            new Vector2(-safe.Acceleration.X * 0.006f,
                safe.Acceleration.Y * 0.005f + safe.LandingImpulse * 9f + safe.ContactImpulse * 4f),
            SecondaryMotionLimits.LanternPosition);
        float lanternRotation = Clamp(
            -safe.Acceleration.X * 0.00038f + safe.Turn * 0.06f + safe.ContactImpulse * 0.08f,
            SecondaryMotionLimits.LanternRotation);

        float phaseDrive = safe.ThreadPhase switch
        {
            PlayerActionPhase.Anticipation => 0.45f,
            PlayerActionPhase.Burst => 1f,
            PlayerActionPhase.Contact => -0.65f,
            PlayerActionPhase.Recovery => 0.25f,
            _ => 0f
        };
        Vector2 spoolTarget = ClampPosition(
            new Vector2(safe.ThreadEnergy * phaseDrive * 2f, -safe.ThreadEnergy * 3f),
            SecondaryMotionLimits.ShoulderSpoolPosition);
        float spoolRotation = Clamp(
            safe.ThreadEnergy * phaseDrive * SecondaryMotionLimits.ShoulderSpoolRotation,
            SecondaryMotionLimits.ShoulderSpoolRotation);

        Vector2 tetherTarget = ClampPosition(
            (lanternTarget - spoolTarget) * 0.55f,
            SecondaryMotionLimits.TetherPosition);
        float tetherRotation = Clamp(
            (lanternRotation - spoolRotation) * 0.75f,
            SecondaryMotionLimits.TetherRotation);

        _lastFrame = new SecondaryMotionFrame(
            ToTransform(ref _mantle, mantleTarget, mantleRotation, deltaSeconds,
                SecondaryMotionLimits.MantlePosition, SecondaryMotionLimits.MantleRotation),
            ToTransform(ref _lantern, lanternTarget, lanternRotation, deltaSeconds,
                SecondaryMotionLimits.LanternPosition, SecondaryMotionLimits.LanternRotation),
            ToTransform(ref _spool, spoolTarget, spoolRotation, deltaSeconds,
                SecondaryMotionLimits.ShoulderSpoolPosition, SecondaryMotionLimits.ShoulderSpoolRotation),
            ToTransform(ref _tether, tetherTarget, tetherRotation, deltaSeconds,
                SecondaryMotionLimits.TetherPosition, SecondaryMotionLimits.TetherRotation));
        return _lastFrame;
    }

    private static PoseTransform2D ToTransform(
        ref ChannelSpring channel,
        Vector2 targetPosition,
        float targetRotation,
        float deltaSeconds,
        Vector2 positionLimit,
        float rotationLimit)
    {
        Vector2 position = channel.StepPosition(targetPosition, deltaSeconds, positionLimit);
        float rotation = channel.StepRotation(targetRotation, deltaSeconds, rotationLimit);
        return new PoseTransform2D(position, rotation, Vector2.One, Colors.White);
    }

    private static SecondaryMotionInput Sanitize(SecondaryMotionInput input) => new(
        Sanitize(input.Velocity),
        Sanitize(input.Acceleration),
        ClampFinite(input.Turn, -1f, 1f),
        ClampFinite(input.LandingImpulse, -1f, 1f),
        ClampFinite(input.ContactImpulse, -1f, 1f),
        Enum.IsDefined(input.ThreadPhase) ? input.ThreadPhase : PlayerActionPhase.None,
        ClampFinite(input.ThreadEnergy, 0f, 1f));

    private static Vector2 Sanitize(Vector2 value) => new(
        IsFinite(value.X) ? value.X : 0f,
        IsFinite(value.Y) ? value.Y : 0f);

    private static float ClampFinite(float value, float minimum, float maximum) =>
        IsFinite(value) ? Mathf.Clamp(value, minimum, maximum) : 0f;

    private static Vector2 ClampPosition(Vector2 value, Vector2 limit) => new(
        Mathf.Clamp(value.X, -limit.X, limit.X),
        Mathf.Clamp(value.Y, -limit.Y, limit.Y));

    private static float Clamp(float value, float limit) => Mathf.Clamp(value, -limit, limit);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private struct ChannelSpring
    {
        private readonly float _frequency;
        private ScalarSpring _x;
        private ScalarSpring _y;
        private ScalarSpring _rotation;

        public ChannelSpring(float frequency)
        {
            _frequency = frequency;
            _x = default;
            _y = default;
            _rotation = default;
        }

        public Vector2 StepPosition(Vector2 target, float deltaSeconds, Vector2 limit) => new(
            _x.Step(target.X, deltaSeconds, _frequency, limit.X),
            _y.Step(target.Y, deltaSeconds, _frequency, limit.Y));

        public float StepRotation(float target, float deltaSeconds, float limit) =>
            _rotation.Step(target, deltaSeconds, _frequency, limit);
    }

    private struct ScalarSpring
    {
        private double _value;
        private double _velocity;

        public float Step(float target, float deltaSeconds, float frequency, float limit)
        {
            double targetValue = target;
            double step = deltaSeconds;
            double omega = frequency;
            double phase = omega * step;
            if (phase >= 50d)
            {
                _value = targetValue;
                _velocity = 0d;
                return target;
            }

            double displacement = _value - targetValue;
            double coefficient = _velocity + omega * displacement;
            double decay = Math.Exp(-phase);
            double nextDisplacement = (displacement + coefficient * step) * decay;
            double nextVelocity = (_velocity - omega * coefficient * step) * decay;
            double nextValue = targetValue + nextDisplacement;

            if (!double.IsFinite(nextValue) || !double.IsFinite(nextVelocity))
            {
                _value = targetValue;
                _velocity = 0d;
                return target;
            }

            double clamped = Math.Clamp(nextValue, -limit, limit);
            if (clamped != nextValue && Math.Sign(nextVelocity) == Math.Sign(nextValue))
                nextVelocity = 0d;
            _value = clamped;
            _velocity = nextVelocity;
            return (float)_value;
        }
    }
}
