using Godot;

namespace Knight.Player;

public readonly record struct PlayerMotionResult(
    Vector2 Velocity,
    bool StartedJump,
    bool StartedDash,
    bool DashRejected,
    bool Landed)
{
    public Vector2 GravityVelocityDelta { get; init; }
    public Vector2 ThrustVelocityDelta { get; init; }
    public float BurnRemaining { get; init; }
    public bool Braking { get; init; }
    public bool BrakeReachedZero { get; init; }
    public bool CancelledForReverse { get; init; }
}
