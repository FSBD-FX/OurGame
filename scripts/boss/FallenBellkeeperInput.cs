namespace Knight.Boss;

public readonly record struct FallenBellkeeperInput(
    bool ArenaActive,
    float HealthFraction,
    bool ChainThreadRequested);
