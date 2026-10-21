namespace Knight.Player;

public sealed record PlayerCombatSettings(
    float StartupSeconds = 0.06f,
    float ActiveSeconds = 0.10f,
    float RecoverySeconds = 0.14f,
    int Damage = 1,
    float Knockback = 260f,
    float DownBounceSpeed = 360f,
    float AimDeadZone = 0.5f);
