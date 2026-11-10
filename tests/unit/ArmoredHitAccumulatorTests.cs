using System;
using Godot;
using Knight.Combat;

namespace Knight.Tests.Unit;

public static class ArmoredHitAccumulatorTests
{
    public static void Run()
    {
        GuardedDamageUsesLiteralThreeToOneSequence();
        DivisorOnePassesThrough();
        InvalidDivisorIsRejected();
        ResetClearsRemainder();
    }

    private static void GuardedDamageUsesLiteralThreeToOneSequence()
    {
        var accumulator = new ArmoredHitAccumulator(3);
        int[] expected = { 0, 0, 1, 0, 0, 1 };
        for (int i = 0; i < expected.Length; i++)
            TestAssert.Equal(expected[i], accumulator.Reduce(1), $"guarded hit {i + 1} has literal 3:1 output");
    }

    private static void DivisorOnePassesThrough()
    {
        var accumulator = new ArmoredHitAccumulator(1);
        TestAssert.Equal(4, accumulator.Reduce(4), "divisor one passes damage through unchanged");
    }

    private static void InvalidDivisorIsRejected()
    {
        AssertThrows<ArgumentOutOfRangeException>(() => new ArmoredHitAccumulator(0),
            "zero armored divisor is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => new ArmoredHitAccumulator(-1),
            "negative armored divisor is rejected");
    }

    private static void ResetClearsRemainder()
    {
        var accumulator = new ArmoredHitAccumulator(3);
        accumulator.Reduce(2);
        accumulator.Reset();
        TestAssert.Equal(0, accumulator.Reduce(1), "reset clears guarded damage remainder");
    }

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Assertion failed: {message}; expected {typeof(T).Name}");
    }
}
