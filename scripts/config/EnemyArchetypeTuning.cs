using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Config;

public readonly record struct EnemyArchetypeSettings(
    float DetectionRange,
    float MoveSpeed,
    float DiveSpeed,
    int MaxHealth,
    float PreferredHoverHeight,
    float HorizontalAlignmentTolerance,
    float VerticalAlignmentTolerance,
    float TelegraphSeconds,
    float AttackSeconds,
    float RecoverySeconds,
    float ThreadStunSeconds,
    float Gravity,
    int ContactDamage);

[GlobalClass]
public partial class EnemyArchetypeTuning : Resource
{
    [Export] public float DetectionRange { get; set; } = 480f;
    [Export] public float MoveSpeed { get; set; } = 140f;
    [Export] public float DiveSpeed { get; set; } = 420f;
    [Export] public int MaxHealth { get; set; } = 2;
    [Export] public float PreferredHoverHeight { get; set; } = 140f;
    [Export] public float HorizontalAlignmentTolerance { get; set; } = 36f;
    [Export] public float VerticalAlignmentTolerance { get; set; } = 28f;
    [Export] public float TelegraphSeconds { get; set; } = 0.70f;
    [Export] public float AttackSeconds { get; set; } = 0.35f;
    [Export] public float RecoverySeconds { get; set; } = 0.70f;
    [Export] public float ThreadStunSeconds { get; set; } = 0.75f;
    [Export] public float Gravity { get; set; } = 980f;
    [Export] public int ContactDamage { get; set; } = 1;

    public EnemyArchetypeSettings ToSettings() => new(
        DetectionRange,
        MoveSpeed,
        DiveSpeed,
        MaxHealth,
        PreferredHoverHeight,
        HorizontalAlignmentTolerance,
        VerticalAlignmentTolerance,
        TelegraphSeconds,
        AttackSeconds,
        RecoverySeconds,
        ThreadStunSeconds,
        Gravity,
        ContactDamage);

    public IReadOnlyList<string> Validate() => Validate(ToSettings());

    public static IReadOnlyList<string> Validate(EnemyArchetypeSettings settings)
    {
        var errors = new List<string>();
        Positive(errors, nameof(DetectionRange), settings.DetectionRange);
        Positive(errors, nameof(MoveSpeed), settings.MoveSpeed);
        Positive(errors, nameof(DiveSpeed), settings.DiveSpeed);
        if (settings.MaxHealth <= 0) errors.Add($"{nameof(MaxHealth)} must be greater than zero");
        Positive(errors, nameof(PreferredHoverHeight), settings.PreferredHoverHeight);
        NonNegative(errors, nameof(HorizontalAlignmentTolerance), settings.HorizontalAlignmentTolerance);
        NonNegative(errors, nameof(VerticalAlignmentTolerance), settings.VerticalAlignmentTolerance);
        NonNegative(errors, nameof(TelegraphSeconds), settings.TelegraphSeconds);
        Positive(errors, nameof(AttackSeconds), settings.AttackSeconds);
        NonNegative(errors, nameof(RecoverySeconds), settings.RecoverySeconds);
        Positive(errors, nameof(ThreadStunSeconds), settings.ThreadStunSeconds);
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
