using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Mechanisms;

public readonly record struct CanyonPressureGatePaintStroke(Vector2 From, Vector2 To, float Width, Color Color);

/// <summary>A single immutable original warning paint submission, not a second gate clock.</summary>
public sealed class CanyonPressureGatePaintFrame
{
    public static CanyonPressureGatePaintFrame Empty { get; } = new(Transform2D.Identity, []);
    public Transform2D SourceToWorld { get; }
    public IReadOnlyList<CanyonPressureGatePaintStroke> Strokes { get; }
    public CanyonPressureGatePaintFrame(Transform2D sourceToWorld, CanyonPressureGatePaintStroke[] strokes)
    {
        SourceToWorld = sourceToWorld;
        Strokes = Array.AsReadOnly((CanyonPressureGatePaintStroke[])strokes.Clone());
    }
}
