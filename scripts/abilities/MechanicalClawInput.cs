using Godot;

namespace Knight.Abilities;

public readonly record struct MechanicalClawInput(
    MechanicalClawCommand Command,
    float SwingAxis,
    bool Arrived,
    Vector2 DockPoint);
