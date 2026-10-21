namespace Knight.Player;

public sealed record PlayerMotionSettings(
    float MaxRunSpeed = 260f,
    float GroundAcceleration = 2200f,
    float GroundDeceleration = 2800f,
    float AirAcceleration = 1500f,
    float Gravity = 1850f,
    float MaxFallSpeed = 780f,
    float JumpSpeed = 590f,
    float JumpCutSpeed = 230f,
    float CoyoteSeconds = 0.10f,
    float JumpBufferSeconds = 0.12f,
    float DashSpeed = 620f,
    float DashSeconds = 0.14f);
