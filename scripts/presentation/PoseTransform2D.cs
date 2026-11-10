using System;
using Godot;

namespace Knight.Presentation;

public readonly record struct PoseTransform2D(
    Vector2 Position,
    float RotationRadians,
    Vector2 Scale,
    Color Modulate)
{
    public static PoseTransform2D Identity { get; } =
        new(Vector2.Zero, 0f, Vector2.One, Colors.White);

    public PoseTransform2D Validate()
    {
        if (!IsFinite(Position)) throw new ArgumentOutOfRangeException(nameof(Position));
        if (!IsFinite(RotationRadians)) throw new ArgumentOutOfRangeException(nameof(RotationRadians));
        if (!IsFinite(Scale)) throw new ArgumentOutOfRangeException(nameof(Scale));
        if (!IsFinite(Modulate)) throw new ArgumentOutOfRangeException(nameof(Modulate));
        return this;
    }

    public static PoseTransform2D Interpolate(
        PoseTransform2D from,
        PoseTransform2D to,
        float weight)
    {
        from.Validate();
        to.Validate();
        if (!IsFinite(weight)) throw new ArgumentOutOfRangeException(nameof(weight));

        float clamped = Mathf.Clamp(weight, 0f, 1f);
        double rawAngleDelta = (double)to.RotationRadians - from.RotationRadians;
        double wrappedAngleDelta = Math.IEEERemainder(rawAngleDelta, Math.Tau);
        double angleDelta = Math.Atan2(
            Math.Sin(wrappedAngleDelta),
            Math.Cos(wrappedAngleDelta));
        return new PoseTransform2D(
            SafeLerp(from.Position, to.Position, clamped),
            ToFiniteFloat((double)from.RotationRadians + angleDelta * clamped),
            SafeLerp(from.Scale, to.Scale, clamped),
            SafeLerp(from.Modulate, to.Modulate, clamped));
    }

    private static Vector2 SafeLerp(Vector2 from, Vector2 to, float weight) => new(
        SafeLerp(from.X, to.X, weight),
        SafeLerp(from.Y, to.Y, weight));

    private static Color SafeLerp(Color from, Color to, float weight) => new(
        SafeLerp(from.R, to.R, weight),
        SafeLerp(from.G, to.G, weight),
        SafeLerp(from.B, to.B, weight),
        SafeLerp(from.A, to.A, weight));

    private static float SafeLerp(float from, float to, float weight) =>
        ToFiniteFloat((double)from + ((double)to - from) * weight);

    private static float ToFiniteFloat(double value) => value switch
    {
        > float.MaxValue => float.MaxValue,
        < -float.MaxValue => -float.MaxValue,
        _ => (float)value
    };

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool IsFinite(Vector2 value) => IsFinite(value.X) && IsFinite(value.Y);
    private static bool IsFinite(Color value) =>
        IsFinite(value.R) && IsFinite(value.G) && IsFinite(value.B) && IsFinite(value.A);
}
