using System;

namespace Knight.Tests;

public static class TestAssert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Assertion failed: {message}");
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!Equals(expected, actual))
            throw new InvalidOperationException($"Assertion failed: {message}; expected={expected}, actual={actual}");
    }

    public static void Near(float expected, float actual, float tolerance, string message)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Assertion failed: {message}; expected={expected}, actual={actual}, tolerance={tolerance}");
    }
}
