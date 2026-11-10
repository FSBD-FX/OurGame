using System.Collections.Generic;
using Godot;

namespace Knight.Config;

public readonly record struct BellBackLaborerSettings(
    float DetectionRange,
    float GuardRange,
    float MoveSpeed,
    float GuardSeconds,
    float TelegraphSeconds,
    float AttackSeconds,
    float RecoverySeconds,
    float ExposedSeconds,
    float Gravity,
    int ContactDamage);

[GlobalClass]
public partial class BellBackLaborerTuning : Resource
{
    [Export] public float DetectionRange { get; set; } = 420f;
    [Export] public float GuardRange { get; set; } = 90f;
    [Export] public float MoveSpeed { get; set; } = 70f;
    [Export] public float GuardSeconds { get; set; } = .40f;
    [Export] public float TelegraphSeconds { get; set; } = .75f;
    [Export] public float AttackSeconds { get; set; } = .20f;
    [Export] public float RecoverySeconds { get; set; } = .70f;
    [Export] public float ExposedSeconds { get; set; } = 1.20f;
    [Export] public float Gravity { get; set; } = 980f;
    [Export] public int ContactDamage { get; set; } = 1;

    public BellBackLaborerSettings ToSettings() => new(
        DetectionRange,
        GuardRange,
        MoveSpeed,
        GuardSeconds,
        TelegraphSeconds,
        AttackSeconds,
        RecoverySeconds,
        ExposedSeconds,
        Gravity,
        ContactDamage);

    public IReadOnlyList<string> Validate() => Validate(ToSettings());

    public static IReadOnlyList<string> Validate(BellBackLaborerSettings settings)
    {
        var errors = new List<string>();
        Positive(errors, nameof(DetectionRange), settings.DetectionRange);
        Positive(errors, nameof(GuardRange), settings.GuardRange);
        if (settings.GuardRange > settings.DetectionRange)
            errors.Add($"{nameof(GuardRange)} must not exceed {nameof(DetectionRange)}");
        Positive(errors, nameof(MoveSpeed), settings.MoveSpeed);
        NonNegative(errors, nameof(GuardSeconds), settings.GuardSeconds);
        NonNegative(errors, nameof(TelegraphSeconds), settings.TelegraphSeconds);
        Positive(errors, nameof(AttackSeconds), settings.AttackSeconds);
        NonNegative(errors, nameof(RecoverySeconds), settings.RecoverySeconds);
        Positive(errors, nameof(ExposedSeconds), settings.ExposedSeconds);
        NonNegative(errors, nameof(Gravity), settings.Gravity);
        if (settings.ContactDamage != 1)
            errors.Add($"{nameof(ContactDamage)} must equal one in the first level");
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
