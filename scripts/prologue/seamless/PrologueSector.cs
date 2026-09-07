using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue.Seamless;

public partial class PrologueSector : Node2D
{
    [Export] public PrologueSectorId SectorId { get; set; }
    [Export] public Rect2 Bounds { get; set; } = new(0f, 0f, 1280f, 720f);

    public IReadOnlyList<string> ValidateContract(PrologueSectorDefinition definition)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(SectorId)) errors.Add($"{nameof(SectorId)} is undefined");
        if (!PrologueSectorDefinition.FinitePositive(Bounds))
            errors.Add($"{nameof(Bounds)} must have finite position and positive finite size");
        if (definition == null)
        {
            errors.Add("definition is required");
            return errors;
        }
        if (SectorId != definition.Id)
            errors.Add($"{nameof(SectorId)} {SectorId} does not match definition {definition.Id}");
        if (!Near(Bounds, definition.Bounds))
            errors.Add($"{nameof(Bounds)} does not match definition bounds");
        Transform2D transform = Transform;
        if (!PrologueSectorDefinition.Finite(transform.X) ||
            !PrologueSectorDefinition.Finite(transform.Y) ||
            !PrologueSectorDefinition.Finite(transform.Origin) ||
            transform.X.DistanceTo(Vector2.Right) > .0001f ||
            transform.Y.DistanceTo(Vector2.Down) > .0001f)
            errors.Add("sector root transform must be finite and translation-only");
        return errors;
    }

    private static bool Near(Rect2 left, Rect2 right) =>
        left.Position.DistanceTo(right.Position) <= .01f &&
        left.Size.DistanceTo(right.Size) <= .01f;
}
