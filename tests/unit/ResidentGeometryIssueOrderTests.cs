using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Knight.Prologue.Residents;

namespace Knight.Tests.Unit;

public static class ResidentGeometryIssueOrderTests
{
    public static void Run()
    {
        var bounds = new Rect2(320, 480, 18, 36);
        var pose = new Transform2D(.17f, new Vector2(329, 498));
        string[] bodyWant = { "native_current_player_body", "native_current_player_hurtbox", "reserved.current_player" };
        string[] pathWant = { "corridor_native_world", "end.native_current_player_body", "start.native_current_player_hurtbox" };
        int[][] permutations = { new[] { 2, 1, 0 }, new[] { 2, 0, 1 }, new[] { 1, 2, 0 },
            new[] { 1, 0, 2 }, new[] { 0, 2, 1 }, new[] { 0, 1, 2 } };
        var support = new List<ResidentSupportWitness> { new(12, 8, new Rect2(310, 516, 60, 12)),
            new(1, 9, new Rect2(370, 516, 40, 12)) };
        foreach (int[] order in permutations)
        {
            var body = new ResidentBodyGeometryResult(bounds, pose, order.Select(i => bodyWant[i]), support);
            var path = new ResidentPathGeometryResult(bounds, order.Select(i => pathWant[i]));
            TestAssert.True(body.Issues.SequenceEqual(bodyWant),
                "RESIDENT_ISSUE_ORDER_RED body diagnostic content must have stable ordinal order independent of native hit enumeration");
            TestAssert.True(path.Issues.SequenceEqual(pathWant),
                "RESIDENT_ISSUE_ORDER_RED path diagnostic content must have stable ordinal order independent of hit enumeration");
            TestAssert.True(!body.BodyGeometryClear && !path.CorridorClear &&
                !body.IsCompletePlacementProof && !path.IsCompletePlacementProof,
                "stable reasons retain rejection and do not grant placement or motion proof");
            TestAssert.True(body.WorldBodyBounds == bounds && body.ColliderWorldTransform == pose &&
                path.WorldCorridorBounds == bounds && body.Support.SequenceEqual(support),
                "normalization does not reorder support or alter geometry values");
        }

        var reasons = new List<string> { "reserved.current_player", "native_current_player_body", "native_current_player_body" };
        var bodySnapshot = new ResidentBodyGeometryResult(bounds, pose, reasons, support);
        var pathSnapshot = new ResidentPathGeometryResult(bounds, reasons);
        reasons.Clear(); support.Reverse();
        TestAssert.True(bodySnapshot.Issues.SequenceEqual(new[] {
            "native_current_player_body", "native_current_player_body", "reserved.current_player" }),
            "body preserves caller multiplicity in its independent ordered snapshot");
        TestAssert.True(pathSnapshot.Issues.SequenceEqual(new[] { "native_current_player_body", "reserved.current_player" }),
            "path preserves its existing ordinal distinct policy, without dropping different reasons");
        TestAssert.True(bodySnapshot.Support[0].ColliderId == 12 && bodySnapshot.Support[1].ColliderId == 1,
            "support stays a separate unsorted snapshot after caller mutation");
        AssertReadOnly(bodySnapshot.Issues); AssertReadOnly(pathSnapshot.Issues);

        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (CultureInfo culture in new[] { CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("tr-TR") })
            {
                CultureInfo.CurrentCulture = culture;
                string[] source = { "\u0131_native", "i_native", "\u0130_native", "I_native", "I_native" };
                TestAssert.True(new ResidentBodyGeometryResult(bounds, pose, source, Array.Empty<ResidentSupportWitness>())
                    .Issues.SequenceEqual(new[] { "I_native", "I_native", "i_native", "\u0130_native", "\u0131_native" }),
                    "body order is ordinal, case sensitive and culture independent");
                TestAssert.True(new ResidentPathGeometryResult(bounds, source).Issues.SequenceEqual(new[] {
                    "I_native", "i_native", "\u0130_native", "\u0131_native" }),
                    "path order and uniqueness are ordinal rather than culture or case folded");
            }
        }
        finally { CultureInfo.CurrentCulture = previous; }

        var clearBody = new ResidentBodyGeometryResult(bounds, pose, Array.Empty<string>(), Array.Empty<ResidentSupportWitness>());
        var clearPath = new ResidentPathGeometryResult(bounds, Array.Empty<string>());
        TestAssert.True(clearBody.BodyGeometryClear && clearPath.CorridorClear && clearBody.Issues.Count == 0 &&
            clearPath.Issues.Count == 0 && !clearBody.IsCompletePlacementProof && !clearPath.IsCompletePlacementProof,
            "empty reasons remain geometrically clear, never a complete placement certificate");
        TestAssert.True(new ResidentBodyGeometryResult(bounds, pose, new[] { "native_world" },
            Array.Empty<ResidentSupportWitness>()).Issues.SequenceEqual(new[] { "native_world" }) &&
            new ResidentPathGeometryResult(bounds, new[] { "corridor_native_world" }).Issues.SequenceEqual(new[] { "corridor_native_world" }),
            "singleton real world rejection is not filtered by normalization");
        var orderedPaths = new[] { new ResidentPathGeometryResult(new Rect2(500, 600, 20, 10), new[] { "z" }), clearPath };
        var patrol = new ResidentPatrolGeometryResult(clearBody, orderedPaths, 3);
        TestAssert.True(ReferenceEquals(patrol.Corridors[0], orderedPaths[0]) && ReferenceEquals(patrol.Corridors[1], clearPath) &&
            patrol.BodyCheckCount == 3, "reason normalization does not reorder patrol segments or change admission counts");
        GD.Print("RESIDENT_ISSUE_ORDER_OK permutations=6 body_and_path=true ordinal_cultures=2 body_multiplicity=true " +
            "path_distinct=true snapshots_readonly=true support_and_segments_unchanged=true native_certificate=false human=false");
    }

    private static void AssertReadOnly(IReadOnlyList<string> reasons)
    {
        bool rejected = false;
        try { ((IList<string>)reasons)[0] = "caller_mutation"; }
        catch (NotSupportedException) { rejected = true; }
        TestAssert.True(rejected && reasons[0] == "native_current_player_body", "caller cannot alter returned issue snapshot");
    }
}
