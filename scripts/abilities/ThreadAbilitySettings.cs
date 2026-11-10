namespace Knight.Abilities;

public sealed record ThreadAbilitySettings(
    float TapThresholdSeconds = .14f,
    float TapAssistRange = 260f,
    float TapConeHalfAngleDegrees = 55f,
    float HoldRange = 400f,
    float HoldConeHalfAngleDegrees = 30f,
    float CooldownSeconds = .35f,
    float PlayerPullSpeed = 720f,
    float EnemyPullSpeed = 540f,
    float VisualSeconds = .12f,
    float WindupSeconds = 4f / 60f,
    float MinimumTravelSeconds = .08f,
    float MaximumTravelSeconds = .28f,
    float TravelDistanceForMaximumDuration = 400f,
    float LatchSeconds = 5f / 60f,
    float ApplySeconds = 3f / 60f,
    float RecoverySeconds = 7f / 60f)
{
    // Temporary source compatibility for the pre-mode ThreadAbility route and tuning contract.
    public float Range => HoldRange;
    public float ConeHalfAngleDegrees => HoldConeHalfAngleDegrees;
}
