using System.Collections.Generic;

namespace Knight.Enemies;

public sealed record RustShellSettings(
    float DetectionRange = 360f,
    float AttackRange = 70f,
    float MoveSpeed = 95f,
    float TelegraphSeconds = 0.65f,
    float AttackSeconds = 0.14f,
    float RecoverySeconds = 0.65f,
    float ThreadStunSeconds = 0.25f,
    int ContactDamage = 1)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Positive(errors, nameof(DetectionRange), DetectionRange);
        Positive(errors, nameof(AttackRange), AttackRange);
        Positive(errors, nameof(MoveSpeed), MoveSpeed);
        NonNegative(errors, nameof(TelegraphSeconds), TelegraphSeconds);
        Positive(errors, nameof(AttackSeconds), AttackSeconds);
        NonNegative(errors, nameof(RecoverySeconds), RecoverySeconds);
        Positive(errors, nameof(ThreadStunSeconds), ThreadStunSeconds);
        if (ContactDamage != 1)
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
