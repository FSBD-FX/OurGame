using System;
using Godot;

namespace Knight.Mechanisms;

public readonly record struct ArcGearMotionFrame(Vector2 Offset, Vector2 Velocity,
    double MinimumFraction, double MaximumFraction, double Phase01);

public sealed class ArcGearMotionModel
{
    public Vector2 PivotOffset { get; }
    public float SwingRadians { get; }
    public float PeriodSeconds { get; }
    public ArcGearMotionFrame Current { get; private set; }
    private double _seconds;

    public ArcGearMotionModel(Vector2 pivotOffset, float swingRadians, float periodSeconds)
    {
        double length = Math.Sqrt((double)pivotOffset.X * pivotOffset.X + (double)pivotOffset.Y * pivotOffset.Y);
        if (!pivotOffset.IsFinite() || !double.IsFinite(length) || length <= 0 || length > 512)
            throw new ArgumentOutOfRangeException(nameof(pivotOffset));
        if (!float.IsFinite(swingRadians) || swingRadians == 0 || Math.Abs(swingRadians) > Mathf.Pi)
            throw new ArgumentOutOfRangeException(nameof(swingRadians));
        if (!float.IsFinite(periodSeconds) || periodSeconds < 1 || periodSeconds > 20 ||
            length * Math.Abs(swingRadians) * Math.PI / periodSeconds > 600)
            throw new ArgumentOutOfRangeException(nameof(periodSeconds));
        PivotOffset = pivotOffset; SwingRadians = swingRadians; PeriodSeconds = periodSeconds;
        Reset();
    }

    public ArcGearMotionFrame Step(double delta)
    {
        double before = Fraction(_seconds);
        if (!double.IsFinite(delta) || delta <= 0) return Current = Frame(before, before);
        double old = _seconds;
        _seconds = (old + delta % PeriodSeconds) % PeriodSeconds;
        double after = Fraction(_seconds), min = Math.Min(before, after), max = Math.Max(before, after);
        if (delta >= PeriodSeconds) { min = 0; max = 1; }
        else
        {
            double half = PeriodSeconds * .5d, end = old + delta;
            for (int turn = (int)Math.Floor(old / half) + 1; turn <= (int)Math.Floor(end / half); turn++)
                if ((turn & 1) == 0) min = 0; else max = 1;
        }
        return Current = Frame(min, max);
    }

    public void Reset() { _seconds = 0; Current = new(Vector2.Zero, Vector2.Zero, 0, 0, 0); }
    private double Fraction(double seconds) => .5d * (1 - Math.Cos(Math.Tau * seconds / PeriodSeconds));
    private ArcGearMotionFrame Frame(double min, double max)
    {
        double phase = _seconds / PeriodSeconds, angle = SwingRadians * Fraction(_seconds);
        double c = Math.Cos(angle), s = Math.Sin(angle), x = -PivotOffset.X, y = -PivotOffset.Y;
        double rx = x * c - y * s, ry = x * s + y * c;
        double angular = SwingRadians * Math.PI / PeriodSeconds * Math.Sin(Math.Tau * phase);
        return new(new((float)(PivotOffset.X + rx), (float)(PivotOffset.Y + ry)),
            new((float)(-ry * angular), (float)(rx * angular)), min, max, phase);
    }
}
