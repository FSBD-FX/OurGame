using Godot;

namespace Knight.Player;

public readonly record struct PlayerInputFrame(
    float MoveAxis,
    Vector2 Aim,
    bool JumpPressed,
    bool JumpHeld,
    bool DashPressed,
    bool AttackPressed,
    bool ThreadPressed,
    bool ThreadHeld = false,
    bool ThreadReleased = false,
    Vector2 MouseWorld = default,
    bool AttackModePressed = false,
    bool GrappleModePressed = false,
    int RequestedSlot = 0,
    bool PrimaryPressed = false,
    bool PrimaryHeld = false,
    bool PrimaryReleased = false,
    bool ShieldHeld = false,
    bool ClawPressed = false,
    bool InteractPressed = false,
    bool KeyboardActions = false,
    bool KeyboardShieldHeld = false,
    bool LegacyShieldHeld = false);
