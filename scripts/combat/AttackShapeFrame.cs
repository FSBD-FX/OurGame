using System.Collections.Generic;
using Godot;

namespace Knight.Combat;

public enum AttackShapeKind
{
    None,
    Rectangle,
    Circle,
    Capsule,
    ConvexPolygon,
    GroundBand
}

public readonly record struct AttackShapeFrame(
    AttackShapeKind Kind,
    Vector2 Origin,
    Vector2 Extent,
    float RotationRadians,
    IReadOnlyList<Vector2> Points);
