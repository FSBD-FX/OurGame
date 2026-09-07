using System.Collections.Generic;
using Godot;
using Knight.Player;

namespace Knight.Config;

[GlobalClass]
public partial class PlayerEnergyTuning : Resource
{
    [Export] public float MaxEnergy { get; set; } = 100f;
    [Export] public float AttackCost { get; set; } = 12f;
    [Export] public float RegenDelaySeconds { get; set; } = .55f;
    [Export] public float RegenPerSecond { get; set; } = 28f;
    [Export] public float ShieldDrainPerSecond { get; set; } = 24f;
    [Export] public float ShieldHeatPerSecond { get; set; } = 42f;
    [Export] public float HeatCoolDelaySeconds { get; set; } = .35f;
    [Export] public float HeatCoolPerSecond { get; set; } = 35f;
    [Export] public float OverheatUnlockHeat { get; set; } = 35f;

    public PlayerEnergySettings ToSettings() => new(
        MaxEnergy,
        AttackCost,
        RegenDelaySeconds,
        RegenPerSecond,
        ShieldDrainPerSecond,
        ShieldHeatPerSecond,
        HeatCoolDelaySeconds,
        HeatCoolPerSecond,
        OverheatUnlockHeat);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Positive(errors, nameof(MaxEnergy), MaxEnergy);
        NonNegative(errors, nameof(AttackCost), AttackCost);
        NonNegative(errors, nameof(RegenDelaySeconds), RegenDelaySeconds);
        NonNegative(errors, nameof(RegenPerSecond), RegenPerSecond);
        NonNegative(errors, nameof(ShieldDrainPerSecond), ShieldDrainPerSecond);
        NonNegative(errors, nameof(ShieldHeatPerSecond), ShieldHeatPerSecond);
        NonNegative(errors, nameof(HeatCoolDelaySeconds), HeatCoolDelaySeconds);
        NonNegative(errors, nameof(HeatCoolPerSecond), HeatCoolPerSecond);
        if (!float.IsFinite(OverheatUnlockHeat) || OverheatUnlockHeat < 0f || OverheatUnlockHeat >= 100f)
            errors.Add($"{nameof(OverheatUnlockHeat)} must be in [0, 100)");
        if (float.IsFinite(MaxEnergy) && float.IsFinite(AttackCost) && AttackCost > MaxEnergy)
            errors.Add($"{nameof(AttackCost)} must not exceed {nameof(MaxEnergy)}");
        return errors;
    }

    private static void Positive(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value <= 0f) errors.Add($"{name} must be greater than zero");
    }

    private static void NonNegative(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value < 0f) errors.Add($"{name} must not be negative");
    }
}
