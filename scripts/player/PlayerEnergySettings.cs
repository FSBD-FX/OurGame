namespace Knight.Player;

public readonly record struct PlayerEnergySettings(
    float MaxEnergy,
    float AttackCost,
    float RegenDelaySeconds,
    float RegenPerSecond,
    float ShieldDrainPerSecond,
    float ShieldHeatPerSecond,
    float HeatCoolDelaySeconds,
    float HeatCoolPerSecond,
    float OverheatUnlockHeat)
{
    public static readonly PlayerEnergySettings Approved = new(
        100f, 12f, .55f, 28f, 24f, 42f, .35f, 35f, 35f);
}
