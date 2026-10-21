using System.Collections.Generic;
using Godot;

namespace Knight.Enemies.Skitter;

[GlobalClass]
public partial class CanyonSkitterSettings : Resource
{
    [Export] public int MaxHealth { get; set; } = 2;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float EngageRange { get; set; } = 120f;
    [Export] public float VerticalTolerance { get; set; } = 40f;
    [Export] public float TelegraphSeconds { get; set; } = .65f;
    [Export] public float RushSpeed { get; set; } = 320f;
    [Export] public float RushSeconds { get; set; } = .20f;
    [Export] public float RushDistance { get; set; } = 64f;
    [Export] public float RetreatSpeed { get; set; } = 160f;
    [Export] public float RetreatSeconds { get; set; } = .30f;
    [Export] public float RetreatDistance { get; set; } = 48f;
    [Export] public float RecoverySeconds { get; set; } = .60f;
    [Export] public float ThreadStunSeconds { get; set; } = .60f;
    [Export] public float Gravity { get; set; } = 980f;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Positive(errors, nameof(EngageRange), EngageRange);
        Positive(errors, nameof(VerticalTolerance), VerticalTolerance);
        Positive(errors, nameof(TelegraphSeconds), TelegraphSeconds);
        Positive(errors, nameof(RushSpeed), RushSpeed);
        Positive(errors, nameof(RushSeconds), RushSeconds);
        Positive(errors, nameof(RushDistance), RushDistance);
        Positive(errors, nameof(RetreatSpeed), RetreatSpeed);
        Positive(errors, nameof(RetreatSeconds), RetreatSeconds);
        Positive(errors, nameof(RetreatDistance), RetreatDistance);
        Positive(errors, nameof(RecoverySeconds), RecoverySeconds);
        Positive(errors, nameof(ThreadStunSeconds), ThreadStunSeconds);
        Positive(errors, nameof(Gravity), Gravity);
        if (MaxHealth <= 0) errors.Add("MaxHealth must be positive");
        if (Damage <= 0) errors.Add("Damage must be positive");
        return errors;
    }

    private static void Positive(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value <= 0f)
            errors.Add($"{name} must be finite and positive");
    }
}
