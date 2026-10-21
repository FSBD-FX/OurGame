using Godot;
using System;

namespace Knight.Combat;

public partial class ArmoredDamageReceiver : Node, IDamageReceiver
{
    public event Action Guarded;
    [Export] public NodePath HealthPath { get; set; } = new("../Health");
    [Export] public int Divisor { get; set; } = 3;

    private ArmoredHitAccumulator _accumulator;
    private HealthComponent _health;

    public override void _Ready()
    {
        _accumulator = new ArmoredHitAccumulator(Divisor);
        _health = GetNode<HealthComponent>(HealthPath);
    }

    public bool ReceiveDamage(DamageRequest request)
    {
        _accumulator ??= new ArmoredHitAccumulator(Divisor);
        _health ??= GetNode<HealthComponent>(HealthPath);
        int reduced = _accumulator.Reduce(request.Amount);
        if (reduced <= 0)
        {
            Guarded?.Invoke();
            return true;
        }
        return _health.ReceiveDamage(request with { Amount = reduced });
    }

    public void ResetAccumulator() => _accumulator?.Reset();
}
