using Godot;

namespace Knight.Combat;

public readonly record struct DamageAttackIdentity(
    ulong SourceRuntimeId,
    ulong LocalAttackId);

public readonly record struct DamageRequest(
    int Amount,
    Vector2 Direction,
    float Knockback,
    ulong SourceAttackId,
    ulong SourceRuntimeId = 0UL,
    DamageGuardClass GuardClass = DamageGuardClass.Normal)
{
    public DamageAttackIdentity AttackIdentity => new(SourceRuntimeId, SourceAttackId);
}
