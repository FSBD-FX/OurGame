using Godot;

namespace Knight.Abilities;

public enum ThreadTargetPriority
{
    Mechanism,
    PullableEnemy,
    MovementAnchor
}

public readonly record struct ThreadCandidate(
    ulong Id,
    Vector2 Position,
    ThreadTargetKind Kind,
    bool HasLineOfSight,
    float FallbackAimAssistRange = 0f);
