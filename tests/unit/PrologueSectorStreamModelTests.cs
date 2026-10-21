using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class PrologueSectorStreamModelTests
{
    private const float SectorSize = 256f;

    public static void Run()
    {
        SectorIdsAreTheFiveApprovedLocationsInRouteOrder();
        RequiredSetsAreUniqueOrderedAndBounded();
        PinnedSectorsJoinOnlyExplicitPhysicalNeighbors();
        ExplicitNeighborsAreAuthoritativeAndSymmetric();
        GeneratedLayoutOneSceneKeepsGeometryAndAddsExplicitNeighbors();
        GeneratedMainActivatesLayoutTwoWithoutChangingSectorIdentity();
        MalformedDefinitionsFailClosed();
        SectorSceneRootsMustBeTranslationOnly();
        NonAdjacentWorldBoundsCannotOverlap();
        InteractionBoundsCannotEnterAnUnloadSeamMargin();
    }

    private static void GeneratedLayoutOneSceneKeepsGeometryAndAddsExplicitNeighbors()
    {
        var root = new SeamlessPrologueWorld { LayoutVersion = 1,
            Definitions = BasinSampleLegacyLayout.CreateDefinitions() };
        try
        {
            SeamlessPrologueWorld world = root;
            TestAssert.True(world != null && world.Definitions.Count == 5,
                "sample retains all five generated layout-one sector definitions");
            var expected = new Dictionary<PrologueSectorId, (Vector2 Offset, Rect2 Bounds,
                int Previous, int Next, int[] Neighbors)>
            {
                [PrologueSectorId.CanyonEntrance] = (Vector2.Zero,
                    new Rect2(0f, 0f, 1280f, 720f), -1, 1, [1]),
                [PrologueSectorId.CanyonDescent] = (new Vector2(1280f, 0f),
                    new Rect2(0f, 0f, 1280f, 1440f), 0, 2, [0, 2]),
                [PrologueSectorId.CanyonBasin] = (new Vector2(1280f, 1440f),
                    new Rect2(0f, 0f, 1600f, 720f), 1, 3, [1, 3]),
                [PrologueSectorId.CanyonAscent] = (new Vector2(2880f, 720f),
                    new Rect2(0f, 0f, 1280f, 1440f), 2, 4, [2, 4]),
                [PrologueSectorId.LostTemple] = (new Vector2(4160f, 720f),
                    new Rect2(0f, 0f, 1920f, 720f), 3, -1, [3])
            };
            foreach (PrologueSectorDefinition definition in world.Definitions)
            {
                var contract = expected[definition.Id];
                TestAssert.Equal(contract.Offset, definition.WorldOffset,
                    $"{definition.Id} current generated layout-one offset is preserved");
                TestAssert.Equal(contract.Bounds, definition.Bounds,
                    $"{definition.Id} current generated layout-one bounds are preserved");
                TestAssert.Equal(contract.Previous, definition.PreviousValue,
                    $"{definition.Id} legacy PreviousValue remains readable");
                TestAssert.Equal(contract.Next, definition.NextValue,
                    $"{definition.Id} legacy NextValue remains readable");
                TestAssert.Equal(string.Join(",", contract.Neighbors),
                    string.Join(",", definition.NeighborValues),
                    $"{definition.Id} serializes its explicit physical NeighborValues");
                string path = definition.Id == PrologueSectorId.CanyonBasin
                    ? BasinQualitySampleEntry.SectorScenePath : BasinSampleLegacyLayout.FrozenScenePath(definition.Id);
                TestAssert.Equal(path, definition.Scene.ResourcePath, "legacy contract loads the actual sample leaf resource");
                var leaf = definition.Scene.Instantiate<PrologueSector>();
                try { TestAssert.Equal(contract.Bounds, leaf.Bounds, "sample leaf geometry retains the exact legacy rectangle"); }
                finally { leaf.Free(); }
            }
            _ = new PrologueSectorStreamModel(world.Definitions);
            TestAssert.True(world.Definitions[2].WorldBounds !=
                    CanyonWorldLayout.SectorBounds[PrologueSectorId.CanyonBasin],
                "legacy sample geometry remains independent of the activated layout-v2 main");
        }
        finally
        {
            root.Free();
        }
    }

    private static void GeneratedMainActivatesLayoutTwoWithoutChangingSectorIdentity()
    {
        var root = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate();
        try
        {
            var world = root.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.Equal(2, world.LayoutVersion, "generated main explicitly activates layout2 after the isolation guards");
            TestAssert.Equal(5, world.Definitions.Count, "activated main retains all five serialized sector identities");
            int[][] neighbors = [[1],[0,2],[1,3],[2,4],[3]];
            foreach (var definition in world.Definitions)
            {
                Rect2 expected = CanyonWorldLayout.SectorBounds[definition.Id];
                TestAssert.Equal(expected.Position, definition.WorldOffset, "generated main offset consumes immutable Task1 authority");
                TestAssert.Equal(new Rect2(Vector2.Zero,expected.Size), definition.Bounds, "generated main bounds consume immutable Task1 authority");
                TestAssert.Equal(string.Join(",",neighbors[(int)definition.Id]), string.Join(",",definition.NeighborValues),
                    "generated main preserves explicit physical sector neighbors");
            }
            _ = new PrologueSectorStreamModel(world.Definitions);
        }
        finally { root.Free(); }
    }

    private static void PinnedSectorsJoinOnlyExplicitPhysicalNeighbors()
    {
        var model = new PrologueSectorStreamModel(ValidDefinitions());

        AssertSequence(model.RequiredSet(PrologueSectorId.CanyonBasin,
                [PrologueSectorId.LostTemple, PrologueSectorId.CanyonBasin,
                    PrologueSectorId.LostTemple, (PrologueSectorId)999]),
            [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
            "Basin includes explicit physical neighbors and one otherwise non-neighbor pinned Temple");
    }

    private static void ExplicitNeighborsAreAuthoritativeAndSymmetric()
    {
        List<PrologueSectorDefinition> definitions = ValidDefinitions();
        definitions[2].Previous = PrologueSectorId.CanyonEntrance;
        definitions[2].Next = null;
        var model = new PrologueSectorStreamModel(definitions);
        AssertSequence(model.RequiredSet(PrologueSectorId.CanyonBasin, []),
            [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                PrologueSectorId.CanyonAscent],
            "legacy Previous/Next fields do not override explicit NeighborValues");

        AssertInvalid(items => items[1].NeighborValues[0] = 999,
            "undefined", "an undefined explicit neighbor is rejected");
        AssertInvalid(items => items[1].NeighborValues.Remove((int)PrologueSectorId.CanyonBasin),
            "symmetric", "an asymmetric physical streaming edge is rejected");
        AssertInvalid(items => items[0].NeighborValues.Clear(),
            "symmetric", "missing explicit exported neighbors cannot fall back to legacy fields");
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

        AssertSequence(model.RequiredSet(PrologueSectorId.CanyonEntrance, []),
            [PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent],
            "entrance prepares itself and the existing next sector");
        AssertSequence(model.RequiredSet(PrologueSectorId.CanyonBasin, []),
            [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                PrologueSectorId.CanyonAscent],
            "a middle sector prepares previous, current and next in route order");
        AssertSequence(model.RequiredSet(PrologueSectorId.LostTemple, []),
            [PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
            "temple prepares the existing previous sector and itself");

        foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
        {
            IReadOnlyList<PrologueSectorId> required = model.RequiredSet(id, []);
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
        AssertInvalid(definitions => definitions[2].Previous = (PrologueSectorId)999,
            "Previous", "an undefined legacy previous value remains readable and rejected");
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
            "overlap", "a non-neighbor sector cannot overlap an earlier world extent");
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
