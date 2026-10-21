using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Enemies.Ranged;

[GlobalClass]
public partial class RangedWeaponSettings : Resource
{
    [Export] public RangedWeaponRole Role { get; set; } = RangedWeaponRole.Scatter;
    [Export] public float DetectionRange { get; set; } = 360f;
    [Export] public float MaximumTravel { get; set; } = 400f;
    [Export] public float TelegraphSeconds { get; set; } = 1.10f;
    [Export] public float DirectionLockSeconds { get; set; } = .55f;
    [Export] public float ActiveSeconds { get; set; } = .12f;
    [Export] public float RecoverySeconds { get; set; } = 1.60f;
    [Export] public float ThreadStunSeconds { get; set; } = .80f;
    [Export] public float ProjectileSpeed { get; set; } = 360f;
    [Export] public float ProjectileRadius { get; set; } = 6f;
    [Export] public int ScatterPelletCount { get; set; } = 5;
    [Export] public float ScatterHalfAngleDegrees { get; set; } = 16f;
    [Export] public int BurstEmissionCount { get; set; } = 3;
    [Export] public float BurstIntervalSeconds { get; set; } = .18f;
    [Export] public float ExplosionRadius { get; set; } = 56f;
    [Export] public float MissileGuidanceSeconds { get; set; } = .40f;
    [Export] public float MissileTurnRateDegrees { get; set; } = 75f;
    [Export] public float MissileMaximumTurnDegrees { get; set; } = 20f;
    [Export] public float MissileMaximumAgeSeconds { get; set; } = 3f;
    [Export] public float LaserHalfWidth { get; set; } = 5f;
    [Export] public int MaxHealth { get; set; } = 2;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float Gravity { get; set; } = 980f;

    public static RangedWeaponSettings ForRole(RangedWeaponRole role)
    {
        var settings = new RangedWeaponSettings { Role = role };
        switch (role)
        {
            case RangedWeaponRole.Burst:
                settings.DetectionRange = 520f;
                settings.MaximumTravel = 560f;
                settings.ActiveSeconds = .42f;
                settings.RecoverySeconds = 1.20f;
                settings.ProjectileSpeed = 440f;
                settings.ProjectileRadius = 5f;
                break;
            case RangedWeaponRole.Missile:
                settings.DetectionRange = 520f;
                settings.MaximumTravel = 560f;
                settings.TelegraphSeconds = 1.30f;
                settings.DirectionLockSeconds = .70f;
                settings.ProjectileSpeed = 260f;
                settings.ProjectileRadius = 8f;
                break;
            case RangedWeaponRole.Laser:
                settings.DetectionRange = 520f;
                settings.MaximumTravel = 560f;
                settings.TelegraphSeconds = 1.25f;
                settings.DirectionLockSeconds = .70f;
                settings.ActiveSeconds = .28f;
                settings.RecoverySeconds = 1.40f;
                break;
        }
        return settings;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(Role)) errors.Add($"{nameof(Role)} must name a supported ranged weapon");
        Positive(errors, nameof(DetectionRange), DetectionRange);
        Positive(errors, nameof(MaximumTravel), MaximumTravel);
        Positive(errors, nameof(TelegraphSeconds), TelegraphSeconds);
        Positive(errors, nameof(DirectionLockSeconds), DirectionLockSeconds);
        if (DirectionLockSeconds >= TelegraphSeconds)
            errors.Add($"{nameof(DirectionLockSeconds)} must be less than {nameof(TelegraphSeconds)}");
        Positive(errors, nameof(ActiveSeconds), ActiveSeconds);
        Positive(errors, nameof(RecoverySeconds), RecoverySeconds);
        Positive(errors, nameof(ThreadStunSeconds), ThreadStunSeconds);
        Positive(errors, nameof(ProjectileSpeed), ProjectileSpeed);
        Positive(errors, nameof(ProjectileRadius), ProjectileRadius);
        if (ScatterPelletCount < 3 || ScatterPelletCount > 9 || (ScatterPelletCount & 1) == 0)
            errors.Add($"{nameof(ScatterPelletCount)} must be odd and between three and nine");
        Positive(errors, nameof(ScatterHalfAngleDegrees), ScatterHalfAngleDegrees);
        if (BurstEmissionCount < 1 || BurstEmissionCount > 8)
            errors.Add($"{nameof(BurstEmissionCount)} must be between one and eight");
        Positive(errors, nameof(BurstIntervalSeconds), BurstIntervalSeconds);
        if (Role == RangedWeaponRole.Burst &&
            (double)(BurstEmissionCount - 1) * BurstIntervalSeconds >= ActiveSeconds)
            errors.Add($"{nameof(ActiveSeconds)} must extend past the final burst emission");
        Positive(errors, nameof(ExplosionRadius), ExplosionRadius);
        Positive(errors, nameof(MissileGuidanceSeconds), MissileGuidanceSeconds);
        Positive(errors, nameof(MissileTurnRateDegrees), MissileTurnRateDegrees);
        Positive(errors, nameof(MissileMaximumTurnDegrees), MissileMaximumTurnDegrees);
        if (MissileMaximumTurnDegrees > 180f)
            errors.Add($"{nameof(MissileMaximumTurnDegrees)} must not exceed 180 degrees");
        Positive(errors, nameof(MissileMaximumAgeSeconds), MissileMaximumAgeSeconds);
        Positive(errors, nameof(LaserHalfWidth), LaserHalfWidth);
        if (MaxHealth <= 0) errors.Add($"{nameof(MaxHealth)} must be greater than zero");
        if (Damage <= 0) errors.Add($"{nameof(Damage)} must be greater than zero");
        Positive(errors, nameof(Gravity), Gravity);
        return errors;
    }

    private static void Positive(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value <= 0f)
            errors.Add($"{name} must be finite and greater than zero");
    }
}
