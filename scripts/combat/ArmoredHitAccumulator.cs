using System;

namespace Knight.Combat;

public sealed class ArmoredHitAccumulator
{
    private readonly int _divisor;
    private int _remainder;

    public ArmoredHitAccumulator(int divisor = 3)
    {
        if (divisor <= 0) throw new ArgumentOutOfRangeException(nameof(divisor));
        _divisor = divisor;
    }

    public int Reduce(int amount)
    {
        if (amount <= 0) return 0;
        int accumulated = _remainder + amount;
        int reduced = accumulated / _divisor;
        _remainder = accumulated % _divisor;
        return reduced;
    }

    public void Reset() => _remainder = 0;
}
