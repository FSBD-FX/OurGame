using System.Collections.Generic;
using Godot;
using Knight.Abilities;

namespace Knight.Config;

[GlobalClass]
public partial class ThreadTuning : Resource
{
    [Export] public float TapThresholdSeconds { get; set; } = .14f;
    [Export] public float TapAssistRange { get; set; } = 260f;
    [Export] public float TapConeHalfAngleDegrees { get; set; } = 55f;
    [Export] public float HoldRange { get; set; } = 400f;
    [Export] public float HoldConeHalfAngleDegrees { get; set; } = 30f;
    [Export] public float CooldownSeconds { get; set; } = .35f;
    [Export] public float PlayerPullSpeed { get; set; } = 720f;
    [Export] public float EnemyPullSpeed { get; set; } = 540f;
    [Export] public float VisualSeconds { get; set; } = .12f;
    [Export] public float WindupSeconds { get; set; } = 4f / 60f;
    [Export] public float MinimumTravelSeconds { get; set; } = .08f;
    [Export] public float MaximumTravelSeconds { get; set; } = .28f;
    [Export] public float TravelDistanceForMaximumDuration { get; set; } = 400f;
    [Export] public float LatchSeconds { get; set; } = 5f / 60f;
    [Export] public float ApplySeconds { get; set; } = 3f / 60f;
    [Export] public float RecoverySeconds { get; set; } = 7f / 60f;

    // Compatibility aliases for existing serialized resources and the pre-mode ability route.
    public float Range { get => HoldRange; set => HoldRange = value; }
    public float ConeHalfAngleDegrees { get => HoldConeHalfAngleDegrees; set => HoldConeHalfAngleDegrees = value; }

    public ThreadAbilitySettings ToSettings() => new(
        TapThresholdSeconds,
        TapAssistRange,
        TapConeHalfAngleDegrees,
        HoldRange,
        HoldConeHalfAngleDegrees,
        CooldownSeconds,
        PlayerPullSpeed,
        EnemyPullSpeed,
        VisualSeconds,
        WindupSeconds,
        MinimumTravelSeconds,
        MaximumTravelSeconds,
        TravelDistanceForMaximumDuration,
        LatchSeconds,
        ApplySeconds,
        RecoverySeconds);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        NonNegative(errors, nameof(TapThresholdSeconds), TapThresholdSeconds);
        Positive(errors, nameof(TapAssistRange), TapAssistRange);
        Cone(errors, nameof(TapConeHalfAngleDegrees), TapConeHalfAngleDegrees);
        Positive(errors, nameof(HoldRange), HoldRange);
        Cone(errors, nameof(HoldConeHalfAngleDegrees), HoldConeHalfAngleDegrees);
        if (!float.IsFinite(HoldRange) || HoldRange <= 0f)
            errors.Add($"{nameof(Range)} must be greater than zero");
        if (!float.IsFinite(HoldConeHalfAngleDegrees) || HoldConeHalfAngleDegrees <= 0f ||
            HoldConeHalfAngleDegrees > 180f)
            errors.Add($"{nameof(ConeHalfAngleDegrees)} must be greater than zero and at most 180");
        NonNegative(errors, nameof(CooldownSeconds), CooldownSeconds);
        Positive(errors, nameof(PlayerPullSpeed), PlayerPullSpeed);
        Positive(errors, nameof(EnemyPullSpeed), EnemyPullSpeed);
        NonNegative(errors, nameof(VisualSeconds), VisualSeconds);
        NonNegative(errors, nameof(WindupSeconds), WindupSeconds);
        Positive(errors, nameof(MinimumTravelSeconds), MinimumTravelSeconds);
        Positive(errors, nameof(MaximumTravelSeconds), MaximumTravelSeconds);
        Positive(errors, nameof(TravelDistanceForMaximumDuration), TravelDistanceForMaximumDuration);
        NonNegative(errors, nameof(LatchSeconds), LatchSeconds);
        NonNegative(errors, nameof(ApplySeconds), ApplySeconds);
        NonNegative(errors, nameof(RecoverySeconds), RecoverySeconds);
        if (MaximumTravelSeconds < MinimumTravelSeconds)
            errors.Add($"{nameof(MaximumTravelSeconds)} must be at least {nameof(MinimumTravelSeconds)}");
        return errors;
    }

    private static void Cone(ICollection<string> errors, string name, float value)
    {
        if (!float.IsFinite(value) || value <= 0f || value > 180f)
            errors.Add($"{name} must be greater than zero and at most 180");
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
