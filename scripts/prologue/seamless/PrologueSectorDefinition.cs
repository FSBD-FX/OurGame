using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue.Seamless;

[GlobalClass]
public partial class PrologueSectorDefinition : Resource
{
    private const int NoNeighbor = -1;

    [Export] public PrologueSectorId Id { get; set; }
    [Export] public PackedScene Scene { get; set; }
    [Export] public Vector2 WorldOffset { get; set; }
    [Export] public Rect2 Bounds { get; set; } = new(0f, 0f, 1280f, 720f);
    [Export] public int PreviousValue { get; set; } = NoNeighbor;
    [Export] public int NextValue { get; set; } = NoNeighbor;
    [Export] public Godot.Collections.Array<Rect2> KeyInteractionBounds { get; set; } = new();

    public PrologueSectorId? Previous
    {
        get => DecodeNeighbor(PreviousValue);
        set => PreviousValue = EncodeNeighbor(value);
    }

    public PrologueSectorId? Next
    {
        get => DecodeNeighbor(NextValue);
        set => NextValue = EncodeNeighbor(value);
    }

    public Rect2 WorldBounds => new(Bounds.Position + WorldOffset, Bounds.Size);

    public PrologueSectorDefinition()
    {
    }

    public PrologueSectorDefinition(PrologueSectorId id, PackedScene scene, Vector2 worldOffset,
        Rect2 bounds, PrologueSectorId? previous, PrologueSectorId? next)
    {
        Id = id;
        Scene = scene;
        WorldOffset = worldOffset;
        Bounds = bounds;
        Previous = previous;
        Next = next;
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(Id)) errors.Add($"{nameof(Id)} is undefined");
        if (Scene == null) errors.Add($"{nameof(Scene)} is required");
        else ValidateSceneRootTransform(errors);
        if (!Finite(WorldOffset)) errors.Add($"{nameof(WorldOffset)} must be finite");
        if (!FinitePositive(Bounds))
            errors.Add($"{nameof(Bounds)} must have finite position and positive finite size");
        ValidateNeighborValue(PreviousValue, nameof(Previous), errors);
        ValidateNeighborValue(NextValue, nameof(Next), errors);

        if (KeyInteractionBounds == null)
        {
            errors.Add($"{nameof(KeyInteractionBounds)} must not be null");
            return errors;
        }
        for (int index = 0; index < KeyInteractionBounds.Count; index++)
        {
            Rect2 interaction = KeyInteractionBounds[index];
            if (!FinitePositive(interaction))
            {
                errors.Add($"{nameof(KeyInteractionBounds)}[{index}] must have finite position and positive finite size");
                continue;
            }
            if (!Contains(Bounds, interaction))
                errors.Add($"{nameof(KeyInteractionBounds)}[{index}] must stay inside {nameof(Bounds)}");
        }
        return errors;
    }

    private void ValidateSceneRootTransform(List<string> errors)
    {
        Node instance = null;
        try
        {
            instance = Scene.Instantiate();
            if (instance is not Node2D root) return;
            Transform2D transform = root.Transform;
            bool finite = Finite(transform.X) && Finite(transform.Y) && Finite(transform.Origin);
            bool identityBasis = transform.X.DistanceTo(Vector2.Right) <= .0001f &&
                transform.Y.DistanceTo(Vector2.Down) <= .0001f;
            if (!finite || !identityBasis)
                errors.Add($"{nameof(Scene)} root transform must be finite and translation-only");
        }
        catch (Exception exception)
        {
            errors.Add($"{nameof(Scene)} root could not be validated: {exception.Message}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(instance)) instance.Free();
        }
    }

    private static void ValidateNeighborValue(int value, string field, List<string> errors)
    {
        if (value == NoNeighbor) return;
        if (!Enum.IsDefined((PrologueSectorId)value)) errors.Add($"{field} is undefined");
    }

    private static PrologueSectorId? DecodeNeighbor(int value) => value == NoNeighbor
        ? null
        : (PrologueSectorId)value;

    private static int EncodeNeighbor(PrologueSectorId? value) => value.HasValue
        ? (int)value.Value
        : NoNeighbor;

    internal static bool Finite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    internal static bool FinitePositive(Rect2 value) => Finite(value.Position) &&
        float.IsFinite(value.Size.X) && float.IsFinite(value.Size.Y) &&
        value.Size.X > 0f && value.Size.Y > 0f;

    internal static bool Contains(Rect2 outer, Rect2 inner, float epsilon = .01f) =>
        inner.Position.X >= outer.Position.X - epsilon &&
        inner.Position.Y >= outer.Position.Y - epsilon &&
        inner.End.X <= outer.End.X + epsilon &&
        inner.End.Y <= outer.End.Y + epsilon;
}
