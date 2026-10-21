namespace Knight.Player;

// A value snapshot: editing an authoring Resource cannot change a live module.
public readonly record struct MobilityModuleSettings(
    string ModuleId,
    float BurnDuration,
    float ThrustAcceleration,
    float PoweredProjectionLimit,
    float GroundFriction,
    float AirDragRate,
    float GroundReverseBrake,
    float AirReverseBrake)
{
    public static MobilityModuleSettings Standard => new(
        "mobility.short-jet.standard.v1", .18f, 5600f, 850f,
        1800f, 1.6f, 4000f, 3000f);
}
