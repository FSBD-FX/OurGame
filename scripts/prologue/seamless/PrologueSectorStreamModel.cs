using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue.Seamless;

public sealed class PrologueSectorStreamModel
{
    public const float UnloadSeamMargin = 96f;
    private const float SeamEpsilon = .01f;

    private readonly Dictionary<PrologueSectorId, PrologueSectorDefinition> _definitions = new();

    public PrologueSectorStreamModel(IEnumerable<PrologueSectorDefinition> definitions)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        var errors = new List<string>();
        int index = 0;
        foreach (PrologueSectorDefinition definition in definitions)
        {
            if (definition == null)
            {
                errors.Add($"definition[{index}] must not be null");
                index++;
                continue;
            }
            foreach (string error in definition.Validate())
                errors.Add($"{definition.Id}: {error}");
            if (!_definitions.TryAdd(definition.Id, definition))
                errors.Add($"duplicate sector ID {definition.Id}");
            index++;
        }

        PrologueSectorId[] sectorIds = Enum.GetValues<PrologueSectorId>();
        if (_definitions.Count != sectorIds.Length)
            errors.Add($"sector graph must contain exactly {sectorIds.Length} unique definitions");
        foreach (PrologueSectorId id in sectorIds)
            if (!_definitions.ContainsKey(id)) errors.Add($"missing definition for {id}");

        foreach ((PrologueSectorId id, PrologueSectorDefinition definition) in _definitions)
        {
            foreach (PrologueSectorId neighborId in definition.Neighbors)
            {
                if (!_definitions.TryGetValue(neighborId, out PrologueSectorDefinition neighbor))
                {
                    errors.Add($"{id} Neighbors references missing or undefined {neighborId}");
                    continue;
                }
                if (!Contains(neighbor.Neighbors, id))
                    errors.Add($"physical streaming neighbors {id} and {neighborId} must be symmetric");
                if ((int)id >= (int)neighborId) continue;
                if (!TryResolveSeam(definition.WorldBounds, neighbor.WorldBounds, out Seam seam))
                {
                    errors.Add($"physical streaming neighbors {id} and {neighborId} bounds " +
                        "must be contiguous at one shared seam");
                    continue;
                }
                ValidateInteractionMargin(definition, seam, firstSide: true, errors);
                ValidateInteractionMargin(neighbor, seam, firstSide: false, errors);
            }
        }

        for (int firstIndex = 0; firstIndex < sectorIds.Length; firstIndex++)
        for (int secondIndex = firstIndex + 1; secondIndex < sectorIds.Length; secondIndex++)
        {
            if (!_definitions.TryGetValue(sectorIds[firstIndex], out PrologueSectorDefinition first) ||
                !_definitions.TryGetValue(sectorIds[secondIndex], out PrologueSectorDefinition second) ||
                !PrologueSectorDefinition.FinitePositive(first.WorldBounds) ||
                !PrologueSectorDefinition.FinitePositive(second.WorldBounds)) continue;
            float overlapX = MathF.Min(first.WorldBounds.End.X, second.WorldBounds.End.X) -
                MathF.Max(first.WorldBounds.Position.X, second.WorldBounds.Position.X);
            float overlapY = MathF.Min(first.WorldBounds.End.Y, second.WorldBounds.End.Y) -
                MathF.Max(first.WorldBounds.Position.Y, second.WorldBounds.Position.Y);
            if (overlapX > SeamEpsilon && overlapY > SeamEpsilon)
                errors.Add($"sector bounds {first.Id} and {second.Id} overlap");
        }

        if (errors.Count > 0)
            throw new ArgumentException("Invalid prologue sector definitions: " +
                string.Join("; ", errors), nameof(definitions));
    }

    public IReadOnlyList<PrologueSectorId> RequiredSet(PrologueSectorId current,
        IEnumerable<PrologueSectorId> pinned)
    {
        if (!_definitions.TryGetValue(current, out PrologueSectorDefinition definition))
            throw new ArgumentOutOfRangeException(nameof(current), current,
                "Current sector must exist in the validated graph.");
        ArgumentNullException.ThrowIfNull(pinned);
        var required = new HashSet<PrologueSectorId> { current };
        foreach (PrologueSectorId neighbor in definition.Neighbors) required.Add(neighbor);
        foreach (PrologueSectorId id in pinned)
            if (_definitions.ContainsKey(id)) required.Add(id);
        var ordered = new List<PrologueSectorId>(required);
        ordered.Sort();
        return Array.AsReadOnly(ordered.ToArray());
    }

    private static bool Contains(IReadOnlyList<PrologueSectorId> values, PrologueSectorId expected)
    {
        foreach (PrologueSectorId value in values)
            if (value == expected) return true;
        return false;
    }

    private static void ValidateInteractionMargin(PrologueSectorDefinition definition,
        Seam seam, bool firstSide, List<string> errors)
    {
        if (definition.KeyInteractionBounds == null) return;
        bool lowSide = firstSide ? seam.FirstIsLowSide : !seam.FirstIsLowSide;
        for (int index = 0; index < definition.KeyInteractionBounds.Count; index++)
        {
            Rect2 local = definition.KeyInteractionBounds[index];
            if (!PrologueSectorDefinition.FinitePositive(local)) continue;
            Rect2 world = new(local.Position + definition.WorldOffset, local.Size);
            float distance = seam.Axis == SeamAxis.X
                ? lowSide ? seam.Coordinate - world.End.X : world.Position.X - seam.Coordinate
                : lowSide ? seam.Coordinate - world.End.Y : world.Position.Y - seam.Coordinate;
            if (distance + SeamEpsilon < UnloadSeamMargin)
                errors.Add($"{definition.Id} KeyInteractionBounds[{index}] enters the " +
                    $"{UnloadSeamMargin:0}-pixel unload seam margin");
        }
    }

    private static bool TryResolveSeam(Rect2 first, Rect2 second, out Seam seam)
    {
        float overlapY = MathF.Min(first.End.Y, second.End.Y) -
            MathF.Max(first.Position.Y, second.Position.Y);
        if (overlapY > SeamEpsilon && MathF.Abs(first.End.X - second.Position.X) <= SeamEpsilon)
        {
            seam = new Seam(SeamAxis.X, first.End.X, true);
            return true;
        }
        if (overlapY > SeamEpsilon && MathF.Abs(first.Position.X - second.End.X) <= SeamEpsilon)
        {
            seam = new Seam(SeamAxis.X, first.Position.X, false);
            return true;
        }

        float overlapX = MathF.Min(first.End.X, second.End.X) -
            MathF.Max(first.Position.X, second.Position.X);
        if (overlapX > SeamEpsilon && MathF.Abs(first.End.Y - second.Position.Y) <= SeamEpsilon)
        {
            seam = new Seam(SeamAxis.Y, first.End.Y, true);
            return true;
        }
        if (overlapX > SeamEpsilon && MathF.Abs(first.Position.Y - second.End.Y) <= SeamEpsilon)
        {
            seam = new Seam(SeamAxis.Y, first.Position.Y, false);
            return true;
        }
        seam = default;
        return false;
    }

    private enum SeamAxis
    {
        X,
        Y
    }

    private readonly record struct Seam(SeamAxis Axis, float Coordinate, bool FirstIsLowSide);
}
