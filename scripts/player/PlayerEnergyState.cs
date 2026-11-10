namespace Knight.Player;

public readonly record struct PlayerEnergyState(
    float Energy,
    float Heat,
    float RegenDelayRemaining,
    float HeatCoolDelayRemaining,
    bool Overheated)
{
    public static PlayerEnergyState Full(PlayerEnergySettings settings) =>
        new(settings.MaxEnergy, 0f, 0f, 0f, false);
}
