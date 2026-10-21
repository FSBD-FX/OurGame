namespace Knight.Player;

public readonly record struct PlayerEnergyInput(bool ShieldRequested, bool ForceShieldClosed);

public readonly record struct PlayerEnergyFrame(PlayerEnergyState State, bool ShieldSustained);
