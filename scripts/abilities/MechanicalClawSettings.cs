using System;

namespace Knight.Abilities;

public readonly record struct MechanicalClawSettings(
    float MaxRange,
    float MainAnchorAssistRadius,
    float MinimumRopeLength,
    float ReelSpeed,
    float SwingAcceleration,
    float RadialStiffness,
    float RadialDamping,
    float ShuttleSpeed,
    float RetractSpeed,
    float BoostUpwardSpeed,
    float BoostTangentialSpeed)
{
    public static MechanicalClawSettings Default => new(
        MaxRange: 620f,
        MainAnchorAssistRadius: 36f,
        MinimumRopeLength: 48f,
        ReelSpeed: 180f,
        SwingAcceleration: 680f,
        RadialStiffness: 32f,
        RadialDamping: 8f,
        ShuttleSpeed: 720f,
        RetractSpeed: 900f,
        BoostUpwardSpeed: 360f,
        BoostTangentialSpeed: 220f);

    public void Validate()
    {
        if (MaxRange != 620f)
            throw new ArgumentOutOfRangeException(nameof(MaxRange), "Mechanical claw range is fixed at 620 pixels.");
        if (MainAnchorAssistRadius != 36f)
            throw new ArgumentOutOfRangeException(nameof(MainAnchorAssistRadius),
                "Mechanical claw main-anchor assist is fixed at 36 pixels.");
        if (!float.IsFinite(MinimumRopeLength) || MinimumRopeLength <= 0f || MinimumRopeLength > MaxRange)
            throw new ArgumentOutOfRangeException(nameof(MinimumRopeLength));
        if (!float.IsFinite(ReelSpeed) || ReelSpeed <= 0f)
            throw new ArgumentOutOfRangeException(nameof(ReelSpeed));
        if (!float.IsFinite(SwingAcceleration) || SwingAcceleration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(SwingAcceleration));
        if (!float.IsFinite(RadialStiffness) || RadialStiffness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(RadialStiffness));
        if (!float.IsFinite(RadialDamping) || RadialDamping < 0f)
            throw new ArgumentOutOfRangeException(nameof(RadialDamping));
        if (!float.IsFinite(ShuttleSpeed) || ShuttleSpeed <= 0f)
            throw new ArgumentOutOfRangeException(nameof(ShuttleSpeed));
        if (!float.IsFinite(RetractSpeed) || RetractSpeed <= 0f)
            throw new ArgumentOutOfRangeException(nameof(RetractSpeed));
        if (!float.IsFinite(BoostUpwardSpeed) || BoostUpwardSpeed <= 0f)
            throw new ArgumentOutOfRangeException(nameof(BoostUpwardSpeed));
        if (!float.IsFinite(BoostTangentialSpeed) || BoostTangentialSpeed <= 0f)
            throw new ArgumentOutOfRangeException(nameof(BoostTangentialSpeed));
    }
}
