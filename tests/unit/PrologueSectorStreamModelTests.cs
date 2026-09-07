using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class PrologueSectorStreamModelTests
{
    private const float SectorSize = 256f;

    public static void Run()
    {
        SectorIdsAreTheFiveApprovedLocationsInRouteOrder();
        RequiredSetsAreUniqueOrderedAndBounded();
        MalformedDefinitionsFailClosed();
        SectorSceneRootsMustBeTranslationOnly();
        NonAdjacentWorldBoundsCannotOverlap();
        InteractionBoundsCannotEnterAnUnloadSeamMargin();
    }

    private static void SectorIdsAreTheFiveApprovedLocationsInRouteOrder()
    {
        string[] expected =
        [
            "CanyonEntrance",
            "CanyonDescent",
            "CanyonBasin",
            "CanyonAscent",
            "LostTemple"
        ];

        string[] actual = Enum.GetNames<PrologueSectorId>();
        TestAssert.Equal(string.Join(",", expected), string.Join(",", actual),
            "sector identity is the literal five-location canyon route");
    }

    private static void RequiredSetsAreUniqueOrderedAndBounded()
    {
        var model = new PrologueSectorStreamModel(ValidDefinitions());

        AssertSequence(model.RequiredSet(PrologueSectorId.CanyonEntrance),
            [PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent],
            "entrance prepares itself and the existing next sector");
        AssertSequence(model.RequiredSet(PrologueSectorId.CanyonBasin),
            [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                PrologueSectorId.CanyonAscent],
            "a middle sector prepares previous, current and next in route order");
        AssertSequence(model.RequiredSet(PrologueSectorId.LostTemple),
            [PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
            "temple prepares the existing previous sector and itself");

        foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
        {
            IReadOnlyList<PrologueSectorId> required = model.RequiredSet(id);
            TestAssert.True(required.Count <= 3, $"{id} never requires more than three sectors");
            TestAssert.Equal(required.Count, new HashSet<PrologueSectorId>(required).Count,
                $"{id} required set contains no duplicate mount");
            for (int index = 1; index < required.Count; index++)
                TestAssert.True((int)required[index - 1] < (int)required[index],
                    $"{id} required set stays in authored route order");
        }
    }

    private static void MalformedDefinitionsFailClosed()
    {
        AssertInvalid(definitions => definitions[0].Scene = null,
            "Scene", "a null sector scene is rejected");
        AssertInvalid(definitions => definitions[1].WorldOffset = new Vector2(float.NaN, 0f),
            "WorldOffset", "a non-finite world offset is rejected");
        AssertInvalid(definitions => definitions[2].Bounds =
                new Rect2(0f, 0f, float.PositiveInfinity, SectorSize),
            "Bounds", "non-finite bounds are rejected");
        AssertInvalid(definitions => definitions[2].Id = PrologueSectorId.CanyonDescent,
            "duplicate", "duplicate sector IDs are rejected");
        AssertInvalid(definitions => definitions[2].Previous = PrologueSectorId.CanyonEntrance,
            "Previous", "a disconnected previous neighbor is rejected");
        AssertInvalid(definitions => definitions[2].Next = null,
            "Next", "a missing contiguous next neighbor is rejected");
        AssertInvalid(definitions => definitions[2].WorldOffset += Vector2.Right * 8f,
            "contiguous", "a positive world-space gap between neighbors is rejected");
        AssertInvalid(definitions => definitions[1] = null,
            "null", "a null definition entry is rejected");
    }

    private static void InteractionBoundsCannotEnterAnUnloadSeamMargin()
    {
        List<PrologueSectorDefinition> valid = ValidDefinitions();
        valid[2].KeyInteractionBounds.Add(new Rect2(112f, 112f, 24f, 24f));
        _ = new PrologueSectorStreamModel(valid);

        AssertInvalid(definitions => definitions[1].KeyInteractionBounds.Add(
                new Rect2(170f, 104f, 28f, 28f)),
            "unload seam", "a key interaction inside the 96-pixel seam margin is rejected");
        AssertInvalid(definitions => definitions[3].KeyInteractionBounds.Add(
                new Rect2(float.NaN, 104f, 28f, 28f)),
            "KeyInteractionBounds", "non-finite key interaction bounds are rejected");
    }

    private static void SectorSceneRootsMustBeTranslationOnly()
    {
        AssertInvalid(definitions => definitions[2].Scene = PackPlaceholder(
                "RotatedBasin", rotation: .2f),
            "translation-only", "a rotated sector root is rejected before mounting");
        AssertInvalid(definitions => definitions[2].Scene = PackPlaceholder(
                "ScaledBasin", scale: new Vector2(1.1f, 1f)),
            "translation-only", "a scaled sector root is rejected before mounting");
        AssertInvalid(definitions => definitions[2].Scene = PackPlaceholder(
                "SkewedBasin", skew: .1f),
            "translation-only", "a skewed sector root is rejected before mounting");
    }

    private static void NonAdjacentWorldBoundsCannotOverlap()
    {
        AssertInvalid(definitions => definitions[4].WorldOffset = Vector2.Right * SectorSize,
            "non-adjacent", "a non-adjacent sector cannot overlap an earlier world extent");
    }

    private static List<PrologueSectorDefinition> ValidDefinitions()
    {
        PackedScene[] scenes =
        [
            PackPlaceholder("Entrance"),
            PackPlaceholder("Descent"),
            PackPlaceholder("Basin"),
            PackPlaceholder("Ascent"),
            PackPlaceholder("Temple")
        ];
        return
        [
            Definition(PrologueSectorId.CanyonEntrance, 0, null,
                PrologueSectorId.CanyonDescent, scenes[0]),
            Definition(PrologueSectorId.CanyonDescent, 1,
                PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonBasin, scenes[1]),
            Definition(PrologueSectorId.CanyonBasin, 2,
                PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonAscent, scenes[2]),
            Definition(PrologueSectorId.CanyonAscent, 3,
                PrologueSectorId.CanyonBasin, PrologueSectorId.LostTemple, scenes[3]),
            Definition(PrologueSectorId.LostTemple, 4,
                PrologueSectorId.CanyonAscent, null, scenes[4])
        ];
    }

    private static PrologueSectorDefinition Definition(PrologueSectorId id, int index,
        PrologueSectorId? previous, PrologueSectorId? next, PackedScene scene) => new(
        id,
        scene,
        Vector2.Right * SectorSize * index,
        new Rect2(0f, 0f, SectorSize, SectorSize),
        previous,
        next);

    private static PackedScene PackPlaceholder(string name, float rotation = 0f,
        Vector2? scale = null, float skew = 0f)
    {
        var root = new Node2D
        {
            Name = name,
            Rotation = rotation,
            Scale = scale ?? Vector2.One,
            Skew = skew
        };
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(root), $"{name} pure stream fixture packs");
        root.Free();
        return packed;
    }

    private static void AssertInvalid(Action<List<PrologueSectorDefinition>> mutate,
        string expectedMessage, string message)
    {
        List<PrologueSectorDefinition> definitions = ValidDefinitions();
        mutate(definitions);
        try
        {
            _ = new PrologueSectorStreamModel(definitions);
        }
        catch (ArgumentException exception)
        {
            TestAssert.True(exception.Message.Contains(expectedMessage,
                    StringComparison.OrdinalIgnoreCase),
                $"{message} names the authored contract; actual={exception.Message}");
            return;
        }
        throw new InvalidOperationException($"Assertion failed: {message}; expected ArgumentException");
    }

    private static void AssertSequence(IReadOnlyList<PrologueSectorId> actual,
        IReadOnlyList<PrologueSectorId> expected, string message)
    {
        TestAssert.Equal(expected.Count, actual.Count, message + " count");
        for (int index = 0; index < expected.Count; index++)
            TestAssert.Equal(expected[index], actual[index], message + $" item {index}");
    }
}
