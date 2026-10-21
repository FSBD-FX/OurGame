namespace Knight.Player;

public readonly record struct PlayerShieldSettings(
    float FrontArcDegrees,
    float MovementScale,
    float HeavyEnergyCost,
    float HeavyKnockbackScale)
{
    public static readonly PlayerShieldSettings Approved = new(150f, .65f, 24f, .45f);
}
