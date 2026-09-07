using Godot;
using Knight.Abilities;

namespace Knight.Mechanisms;

public readonly record struct ThreadInteraction(
    Node2D Player,
    Vector2 Origin,
    Vector2 TargetPosition,
    ThreadAbilitySettings Settings);
