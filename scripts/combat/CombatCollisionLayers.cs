namespace Knight.Combat;

public static class CombatCollisionLayers
{
    public const uint None = 0u;
    public const uint World = 1u << 0;
    public const uint Player = 1u << 1;
    public const uint Enemy = 1u << 2;
    public const uint PlayerAttack = 1u << 3;
    public const uint EnemyAttack = 1u << 4;
    public const uint ThreadTarget = 1u << 5;
}
