using Godot;

namespace Knight.Abilities;

public readonly record struct MechanicalClawTargetSnapshot(
    ulong RuntimeId,
    bool Exists,
    GrappleSurfaceMaterial Material,
    Vector2 WorldAnchor,
    bool HasLineOfSight,
    bool IsLegacyInteraction,
    Vector2 SurfaceNormal);
