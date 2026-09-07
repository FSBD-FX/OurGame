using System.Collections.Generic;
using Godot;
using Knight.Player;

namespace Knight.Config;

[GlobalClass]
public partial class PlayerTuning : Resource
{
    [Export] public float MaxRunSpeed { get; set; } = 260f;
    [Export] public float GroundAcceleration { get; set; } = 2200f;
    [Export] public float GroundDeceleration { get; set; } = 2800f;
    [Export] public float AirAcceleration { get; set; } = 1500f;
    [Export] public float Gravity { get; set; } = 1850f;
    [Export] public float MaxFallSpeed { get; set; } = 780f;
    [Export] public float JumpSpeed { get; set; } = 590f;
    [Export] public float JumpCutSpeed { get; set; } = 230f;
    [Export] public float CoyoteSeconds { get; set; } = 0.10f;
    [Export] public float JumpBufferSeconds { get; set; } = 0.12f;
    [Export] public float DashSpeed { get; set; } = 620f;
    [Export] public float DashSeconds { get; set; } = 0.14f;
    [Export] public float AttackStartupSeconds { get; set; } = 0.06f;
    [Export] public float AttackActiveSeconds { get; set; } = 0.10f;
    [Export] public float AttackRecoverySeconds { get; set; } = 0.14f;
    [Export] public int AttackDamage { get; set; } = 1;
    [Export] public float AttackKnockback { get; set; } = 260f;
    [Export] public float DownBounceSpeed { get; set; } = 360f;
    [Export] public float AimDeadZone { get; set; } = 0.5f;

    public PlayerMotionSettings ToMotionSettings() => new(
        MaxRunSpeed,
        GroundAcceleration,
        GroundDeceleration,
        AirAcceleration,
        Gravity,
        MaxFallSpeed,
        JumpSpeed,
        JumpCutSpeed,
        CoyoteSeconds,
        JumpBufferSeconds,
        DashSpeed,
        DashSeconds);

    public PlayerCombatSettings ToCombatSettings() => new(
        AttackStartupSeconds,
        AttackActiveSeconds,
        AttackRecoverySeconds,
        AttackDamage,
        AttackKnockback,
        DownBounceSpeed,
        AimDeadZone);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Positive(errors, nameof(MaxRunSpeed), MaxRunSpeed);
        Positive(errors, nameof(GroundAcceleration), GroundAcceleration);
        Positive(errors, nameof(GroundDeceleration), GroundDeceleration);
        Positive(errors, nameof(AirAcceleration), AirAcceleration);
        Positive(errors, nameof(Gravity), Gravity);
        Positive(errors, nameof(MaxFallSpeed), MaxFallSpeed);
        Positive(errors, nameof(JumpSpeed), JumpSpeed);
        Positive(errors, nameof(JumpCutSpeed), JumpCutSpeed);
        NonNegative(errors, nameof(CoyoteSeconds), CoyoteSeconds);
        NonNegative(errors, nameof(JumpBufferSeconds), JumpBufferSeconds);
        Positive(errors, nameof(DashSpeed), DashSpeed);
        Positive(errors, nameof(DashSeconds), DashSeconds);
        NonNegative(errors, nameof(AttackStartupSeconds), AttackStartupSeconds);
        Positive(errors, nameof(AttackActiveSeconds), AttackActiveSeconds);
        NonNegative(errors, nameof(AttackRecoverySeconds), AttackRecoverySeconds);
        if (AttackDamage <= 0) errors.Add($"{nameof(AttackDamage)} must be greater than zero");
        NonNegative(errors, nameof(AttackKnockback), AttackKnockback);
        Positive(errors, nameof(DownBounceSpeed), DownBounceSpeed);
        if (!float.IsFinite(AimDeadZone) || AimDeadZone < 0f || AimDeadZone > 1f)
            errors.Add($"{nameof(AimDeadZone)} must be between zero and one");
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
