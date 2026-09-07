using Godot;

namespace Knight.Abilities;

public readonly record struct MechanicalClawFrame(
    ulong CastId,
    MechanicalClawState State,
    MechanicalClawFailure Failure,
    Vector2 AnchorPoint,
    float RopeLength,
    Vector2 VelocityCorrection,
    bool EffectiveVisibility,
    ulong LockedTargetRuntimeId = 0UL,
    Vector2 CasterWorldPosition = default,
    Vector2 CasterVelocity = default,
    float ShuttleDistance = 0f,
    ulong NextCastId = 1UL,
    bool BoostConsumed = false)
{
    public static MechanicalClawFrame Idle(Vector2 casterWorldPosition = default,
        Vector2 casterVelocity = default) => new(
        CastId: 0UL,
        State: MechanicalClawState.Idle,
        Failure: MechanicalClawFailure.None,
        AnchorPoint: Vector2.Zero,
        RopeLength: 0f,
        VelocityCorrection: Vector2.Zero,
        EffectiveVisibility: false,
        LockedTargetRuntimeId: 0UL,
        CasterWorldPosition: casterWorldPosition,
        CasterVelocity: casterVelocity,
        ShuttleDistance: 0f,
        NextCastId: 1UL,
        BoostConsumed: false);
}
