using Godot;

namespace Knight.Prologue.Guidance;

public readonly record struct PrologueGuidanceTargetView(
    Vector2 ViewportPosition,
    Vector2 Direction,
    float Distance,
    bool IsOnScreen,
    Vector2 ProtectionSize = default);
