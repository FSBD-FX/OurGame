using Godot;

namespace Knight.Abilities;

public enum ThreadCastState
{
    Idle,
    Preview,
    Windup,
    Travel,
    Latched,
    Apply,
    Recovery,
    Failed
}

public enum ThreadAimMode
{
    Tap,
    Hold
}

public enum ThreadFailureReason
{
    None,
    NoTarget,
    Obstructed,
    OutOfRange,
    Cooldown,
    TargetLost,
    Cancelled
}

public readonly record struct ThreadCastInput(
    bool Pressed,
    bool Held,
    bool Released,
    Vector2 Aim,
    bool Cancelled);

public readonly record struct ThreadTargetSnapshot(
    ulong Id,
    Vector2 RelativePosition,
    bool IsValid,
    ThreadFailureReason InvalidReason);

public readonly record struct ThreadCastFrame(
    ulong CastId,
    ThreadCastState State,
    ThreadAimMode AimMode,
    Vector2 Aim,
    ulong TargetId,
    bool HasTarget,
    ThreadFailureReason Failure,
    float StateDurationSeconds,
    float StateRemainingSeconds);
