using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Player;

public partial class PlayerGuardedDamageReceiver : Node, IDamageReceiver
{
    [Export] public NodePath HealthPath { get; set; } = new("../Health");
    [Export] public NodePath ShieldPath { get; set; } = new("../Shield");

    private readonly Queue<DamageAttackIdentity> _processedAttackOrder = new();
    private readonly HashSet<DamageAttackIdentity> _processedAttackIds = new();
    private HealthComponent _health = null!;
    private PlayerShieldController _shield = null!;

    public override void _Ready()
    {
        ResolveDependencies();
    }

    public bool ReceiveDamage(DamageRequest request)
    {
        ResolveDependencies();
        if (_processedAttackIds.Contains(request.AttackIdentity)) return false;

        bool guarded = _shield.TryResolve(request, out DamageRequest resolved);
        if (guarded && resolved.Amount <= 0)
        {
            RememberAttack(request.AttackIdentity);
            return true;
        }

        RememberAttack(request.AttackIdentity);
        return _health.ReceiveDamage(resolved);
    }

    public void ResetFull()
    {
        _processedAttackOrder.Clear();
        _processedAttackIds.Clear();
    }

    private void ResolveDependencies()
    {
        _health ??= GetNodeOrNull<HealthComponent>(HealthPath) ??
            throw new System.InvalidOperationException(
                $"PlayerGuardedDamageReceiver HealthPath '{HealthPath}' must resolve to {nameof(HealthComponent)}.");
        _shield ??= GetNodeOrNull<PlayerShieldController>(ShieldPath) ??
            throw new System.InvalidOperationException(
                $"PlayerGuardedDamageReceiver ShieldPath '{ShieldPath}' must resolve to {nameof(PlayerShieldController)}.");
    }

    private void RememberAttack(DamageAttackIdentity attackIdentity)
    {
        _processedAttackIds.Add(attackIdentity);
        _processedAttackOrder.Enqueue(attackIdentity);
        if (_processedAttackOrder.Count <= 64) return;
        _processedAttackIds.Remove(_processedAttackOrder.Dequeue());
    }
}
