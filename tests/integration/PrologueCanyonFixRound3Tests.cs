using System;
using Godot;
using Knight.Build.Prologue;

namespace Knight.Tests.Integration;

public static class PrologueCanyonFixRound3Tests
{
    // Break caught: a classifier that accepts an out-of-bounds tangential coordinate or a
    // corner tie carves recovery despite neither marker being a unique bounded bottom seam.
    internal static void AssertRecoveryClassifierRejectsUnboundedAndAmbiguousEdges()
    {
        Rect2 bounds = PrologueCanyonBuildParts.DescentBounds;
        Vector2 tangentialOutside = new(bounds.Position.X - 25f, bounds.End.Y - 1f);
        Vector2 ambiguousCorner = new(bounds.Position.X + 2f, bounds.End.Y - 2f);
        bool outsideAccepted = PrologueCanyonBuildParts.IsUnambiguousBottomRecoverySeam(
            tangentialOutside, bounds);
        bool cornerAccepted = PrologueCanyonBuildParts.IsUnambiguousBottomRecoverySeam(
            ambiguousCorner, bounds);
        GD.Print($"TASK8_FIX4_SEAM_CLASSIFIER_PROBE outside={outsideAccepted} " +
            $"corner={cornerAccepted} outside={tangentialOutside} corner_pos={ambiguousCorner}");
        TestAssert.True(!outsideAccepted,
            "a bottom-near marker outside the bounded tangential X range must never carve recovery");
        TestAssert.True(!cornerAccepted,
            "an equally-near bottom/left corner marker must fail closed rather than carve recovery");
    }
}
