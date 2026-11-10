using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Enemies.Ranged;

// Replacement visuals own their art, never combat. Reset is a checkpoint
// generation boundary; Clear only revokes the current attack presentation.
public interface IRangedWeaponVisual2D : IEntityVisual2D
{
    void PresentWeaponFrame(RangedWeaponRole role, RangedWeaponFrame frame);
    void ClearAttackPresentation();
    void ResetWeaponPresentation(RangedWeaponRole role);
}

public enum RangedWeaponRole
{
    Scatter,
    Burst,
    Missile,
    Laser
}

public enum RangedWeaponState
{
    Idle,
    Telegraph,
    Active,
    Recovery,
    Stunned,
    Dead
}

public readonly record struct RangedWeaponInput(
    Vector2 MuzzleWorldPosition,
    Vector2 TargetWorldPosition,
    bool TargetVisible,
    bool StunRequested,
    bool Dead);

// Consume the zero-based [FirstEmissionIndex, FirstEmissionIndex + EmissionsDue)
// interval before branching on State. A hitch may deliver the last burst events
// together with Recovery, while preserving their originating AttackId.
// FirstEmissionIndex == -1 means no event, and EmissionsDue is then zero.
public readonly record struct RangedWeaponFrame(
    RangedWeaponState State,
    Vector2 AimDirection,
    AttackTimelineFrame AttackTimeline,
    int FirstEmissionIndex,
    int EmissionsDue);
