using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Sample;

/// <summary>Geometry-only compatibility boundary. Runtime player/UI/controllers stay shared.</summary>
public static class BasinSampleLegacyLayout
{
    public static string FrozenScenePath(PrologueSectorId id) =>
        $"res://scenes/prologue/sample/legacy/{id}.tscn";

    public static Godot.Collections.Array<PrologueSectorDefinition> CreateDefinitions(bool useFrozenBasinSeed = false) =>
    [
        Define(PrologueSectorId.CanyonEntrance, new(0,0), new(1280,720), null,
            PrologueSectorId.CanyonDescent, [new(896,366,48,48)], useFrozenBasinSeed),
        Define(PrologueSectorId.CanyonDescent, new(1280,0), new(1280,1440), PrologueSectorId.CanyonEntrance,
            PrologueSectorId.CanyonBasin, [new(236,236,48,48), new(412,332,56,56)], useFrozenBasinSeed),
        Define(PrologueSectorId.CanyonBasin, new(1280,1440), new(1600,720), PrologueSectorId.CanyonDescent,
            PrologueSectorId.CanyonAscent, [new(756,336,48,48)], useFrozenBasinSeed),
        Define(PrologueSectorId.CanyonAscent, new(2880,720), new(1280,1440), PrologueSectorId.CanyonBasin,
            PrologueSectorId.LostTemple, [new(372,1002,56,56), new(722,772,56,56), new(872,472,56,56)], useFrozenBasinSeed),
        Define(PrologueSectorId.LostTemple, new(4160,720), new(1920,720), PrologueSectorId.CanyonAscent,
            null, [new(572,292,56,56), new(876,336,48,48)], useFrozenBasinSeed)
    ];

    private static PrologueSectorDefinition Define(PrologueSectorId id, Vector2 offset, Vector2 size,
        PrologueSectorId? previous, PrologueSectorId? next, Rect2[] interactions, bool frozenBasin)
    {
        var definition = new PrologueSectorDefinition(id, GD.Load<PackedScene>(
                id == PrologueSectorId.CanyonBasin && !frozenBasin ? BasinQualitySampleEntry.SectorScenePath : FrozenScenePath(id)),
            offset, new Rect2(Vector2.Zero, size), previous, next);
        foreach (Rect2 bounds in interactions) definition.KeyInteractionBounds.Add(bounds);
        return definition;
    }
}
