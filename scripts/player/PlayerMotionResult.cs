using Godot;

namespace Knight.Player;

public readonly record struct PlayerMotionResult(
    Vector2 Velocity,
    bool StartedJump,
    bool StartedDash,
    bool DashRejected,
    bool Landed);
