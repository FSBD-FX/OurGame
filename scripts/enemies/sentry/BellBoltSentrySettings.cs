using System.Collections.Generic;
using Godot;

namespace Knight.Enemies.Sentry;

[GlobalClass]
public partial class BellBoltSentrySettings : Resource
{
    [Export] public float DetectionRange { get; set; } = 560f;
    [Export] public float TelegraphSeconds { get; set; } = 1.10f;
    [Export] public float DirectionLockSeconds { get; set; } = .55f;
    [Export] public float FireSeconds { get; set; } = .12f;
    [Export] public float RecoverySeconds { get; set; } = 1f;
    [Export] public float ThreadStunSeconds { get; set; } = .80f;
    [Export] public float BoltSpeed { get; set; } = 440f;
    [Export] public float MaximumTravel { get; set; } = 640f;
    [Export] public float BoltRadius { get; set; } = 7f;
    [Export] public int MaxHealth { get; set; } = 2;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float Gravity { get; set; } = 980f;

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        Positive(errors, nameof(DetectionRange), DetectionRange);
        Positive(errors, nameof(TelegraphSeconds), TelegraphSeconds);
        Positive(errors, nameof(DirectionLockSeconds), DirectionLockSeconds);
        if (DirectionLockSeconds >= TelegraphSeconds)
            errors.Add($"{nameof(DirectionLockSeconds)} must be less than {nameof(TelegraphSeconds)}");
        Positive(errors, nameof(FireSeconds), FireSeconds);
        Positive(errors, nameof(RecoverySeconds), RecoverySeconds);
        Positive(errors, nameof(ThreadStunSeconds), ThreadStunSeconds);
        Positive(errors, nameof(BoltSpeed), BoltSpeed);
        Positive(errors, nameof(MaximumTravel), MaximumTravel);
        Positive(errors, nameof(BoltRadius), BoltRadius);
        Positive(errors, nameof(Gravity), Gravity);
        if (MaxHealth <= 0) errors.Add($"{nameof(MaxHealth)} must be greater than zero");
        if (Damage <= 0) errors.Add($"{nameof(Damage)} must be greater than zero");
        return errors;
    }

    private static void Positive(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value <= 0f) errors.Add($"{name} must be finite and greater than zero");
    }
}
