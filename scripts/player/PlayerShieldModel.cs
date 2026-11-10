using Godot;
using Knight.Combat;

namespace Knight.Player;

public readonly record struct PlayerShieldInput(
    bool ShieldActive,
    Vector2 ShieldDirection,
    Vector2 IncomingDirection,
    DamageGuardClass GuardClass,
    bool HeavyEnergyAvailable);

public readonly record struct PlayerShieldResult(
    bool Guarded,
    int DamageAmount,
    float KnockbackScale,
    bool SpendHeavyEnergy,
    bool BrokeGuard);

public sealed class PlayerShieldModel
{
    private readonly PlayerShieldSettings _settings;

    public PlayerShieldModel(PlayerShieldSettings settings)
    {
        _settings = settings;
    }

    public PlayerShieldResult Resolve(PlayerShieldInput input)
    {
        if (!input.ShieldActive || input.GuardClass == DamageGuardClass.Unblockable || !IsFront(input))
            return PassThrough(false);

        return input.GuardClass switch
        {
            DamageGuardClass.Normal => new PlayerShieldResult(true, 0, 0f, false, false),
            DamageGuardClass.Heavy when input.HeavyEnergyAvailable => new PlayerShieldResult(
                true, 1, _settings.HeavyKnockbackScale, true, false),
            DamageGuardClass.Heavy => PassThrough(true),
            _ => PassThrough(false)
        };
    }

    private bool IsFront(PlayerShieldInput input)
    {
        if (!IsFinite(input.ShieldDirection) || !IsFinite(input.IncomingDirection) ||
            input.ShieldDirection.LengthSquared() <= float.Epsilon ||
            input.IncomingDirection.LengthSquared() <= float.Epsilon) return false;
        float halfArcRadians = Mathf.DegToRad(_settings.FrontArcDegrees * .5f);
        float threshold = Mathf.Cos(halfArcRadians);
        return input.ShieldDirection.Normalized().Dot(input.IncomingDirection.Normalized()) >= threshold - .0001f;
    }

    private static PlayerShieldResult PassThrough(bool brokeGuard) =>
        new(false, 1, 1f, false, brokeGuard);

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
