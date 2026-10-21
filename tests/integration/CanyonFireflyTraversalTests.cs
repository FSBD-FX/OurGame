using System;
using Godot;
using Knight.Abilities;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Real mounted support queries; no mocked traversal or production save owner.</summary>
public static class CanyonFireflyTraversalTests
{
    public static void Run(SceneTree tree)
    {
        var sector = new PrologueSector { Name = "FireflyTraversalFixture",
            SectorId = PrologueSectorId.CanyonEntrance };
        var firstBranch = new Node2D { Name = "FirstBranch" };
        var secondBranch = new Node2D { Name = "SecondBranch" };
        var marker = new Marker2D { Name = "Marker", Position = new Vector2(500f, 300f) };
        sector.AddChild(firstBranch); sector.AddChild(secondBranch); sector.AddChild(marker);
        for (int index = 0; index < 64; index++)
        {
            firstBranch.AddChild(new Node { Name = "FirstLeaf" + index });
            secondBranch.AddChild(new Node { Name = "SecondLeaf" + index });
        }
        var first = new GrappleSurface2D { Name = "FirstSupport", Position = new Vector2(510f, 300f) };
        var second = new GrappleSurface2D { Name = "SecondSupport", Position = new Vector2(500f, 310f) };
        firstBranch.AddChild(first); secondBranch.AddChild(second);
        var outside = new GrappleSurface2D { Name = "OutsideRoute", Position = new Vector2(-1f, 300f) };
        sector.AddChild(outside);
        try
        {
            TestAssert.True(!Query(marker, out _), "unmounted marker has no live support");
            tree.Root.AddChild(sector);
            TestAssert.True(Query(marker, out GrappleSurface2D support) && support == first,
                "depth-first child order preserves the first equal-distance support");
            firstBranch.Hide();
            TestAssert.True(Query(marker, out support) && support == second,
                "hidden ancestor excludes its support immediately");
            firstBranch.Show();
            first.Position = new Vector2(530f, 300f);
            TestAssert.True(Query(marker, out support) && support == second,
                "live support movement is not hidden by cached descendants");
            first.Position = new Vector2(510f, 300f);
            firstBranch.RemoveChild(first);
            TestAssert.True(Query(marker, out support) && support == second,
                "removed support is not returned by a stale traversal");
            firstBranch.AddChild(first);
            TestAssert.True(Query(marker, out support) && support == first,
                "re-entered nested support becomes eligible on the next synchronous query");
            sector.MoveChild(secondBranch, 0);
            TestAssert.True(Query(marker, out support) && support == second,
                "live sibling reordering changes the equal-distance winner");
            second.QueueFree();
            TestAssert.True(Query(marker, out support) && support == first,
                "queued support is excluded before the deletion flush");
            first.Position = new Vector2(-2f, 300f);
            TestAssert.True(!Query(marker, out _),
                "a live support outside the actual route bounds is rejected");
            first.Position = new Vector2(510f, 300f);

            // Warm real Godot wrappers and LINQ once, then measure only synchronous
            // public support queries on the same mounted 135-node fixture.
            for (int index = 0; index < 8; index++) Query(marker, out _);
            long before = GC.GetAllocatedBytesForCurrentThread();
            bool allMatched = true;
            for (int index = 0; index < 64; index++)
                allMatched &= Query(marker, out support) && support == first;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            GD.Print($"FIREFLY_SUPPORT_QUERY_COST calls=64 fixtureNodes=135 threadBytes={allocated} " +
                "basis=synchronous_warmed_public_support_queries_not_frame_time");
            TestAssert.True(allMatched, "all measured public queries retain the exact support identity");
            // Generous 32 KiB/call bound permits iterator and native-wrapper costs;
            // prevents reintroducing a native Array/enumerator for every leaf.
            TestAssert.True(allocated <= 64L * 32 * 1024,
                $"firefly support traversal budget <=2097152 bytes for64 calls; actual={allocated}");
            GD.Print("FIREFLY_TRAVERSAL_TESTS_OK assertions=11");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(first) && first.GetParent() == null) first.Free();
            sector.Free();
        }
        SupportSearchDoesNotAllocatePerIrrelevantDescendant(tree);
    }

    // Break caught: an iterator per irrelevant descendant violates a warmed,
    // fixed-result public-query budget even when indexed child traversal is used.
    private static void SupportSearchDoesNotAllocatePerIrrelevantDescendant(SceneTree tree)
    {
        var sector = new PrologueSector { Name = "NestedWideSupportFixture",
            SectorId = PrologueSectorId.CanyonEntrance };
        var earlyBranch = new Node2D { Name = "EarlyBranch" };
        var deepBranch = new Node2D { Name = "DeepBranch" };
        var first = new GrappleSurface2D { Name = "PreorderParent", Position = new Vector2(510f, 300f) };
        var nested = new GrappleSurface2D { Name = "PreorderChild", Position = new Vector2(-10f, 10f) };
        var later = new GrappleSurface2D { Name = "LaterShallow", Position = new Vector2(490f, 300f) };
        var marker = new Marker2D { Name = "LiveMarker", Position = new Vector2(500f, 300f) };
        sector.AddChild(earlyBranch); earlyBranch.AddChild(deepBranch);
        deepBranch.AddChild(first); first.AddChild(nested);
        sector.AddChild(later); sector.AddChild(marker);
        // 16 wide branches +256 irrelevant leaves, all real mounted nodes.
        for (int branchIndex = 0; branchIndex < 16; branchIndex++)
        {
            var branch = new Node { Name = "IrrelevantBranch" + branchIndex };
            earlyBranch.AddChild(branch);
            for (int leafIndex = 0; leafIndex < 16; leafIndex++)
                branch.AddChild(new Node { Name = "Leaf" + leafIndex });
        }
        try
        {
            TestAssert.True(!Query(null, out GrappleSurface2D support) && support == null,
                "null marker immediately returns no support");
            TestAssert.True(!Query(marker, out support) && support == null,
                "nested fixture unmounted marker immediately returns null support");
            tree.Root.AddChild(sector);
            // Hand-checked literal tie: parent(510,300), child(500,310), later(490,300)
            // are all10 units from marker(500,300). Parent-first depth-first wins.
            TestAssert.True(Query(marker, out support) && support == first,
                "nested literal tie visits a candidate before its child and before later shallow siblings");
            for (int index = 0; index < 8; index++) Query(marker, out _);
            int beforeThread = System.Environment.CurrentManagedThreadId;
            ulong beforeFrame = Engine.GetProcessFrames();
            long before = GC.GetAllocatedBytesForCurrentThread();
            bool allMatched = true;
            for (int index = 0; index < 64; index++)
                allMatched &= Query(marker, out support) && support == first;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            int afterThread = System.Environment.CurrentManagedThreadId;
            ulong afterFrame = Engine.GetProcessFrames();
            GD.Print($"FIREFLY_SUPPORT_FIXED_ALLOCATION calls=64 warmups=8 irrelevantDescendants=272 " +
                $"threadBytes={allocated} beforeThread={beforeThread} afterThread={afterThread} " +
                $"beforeFrame={beforeFrame} afterFrame={afterFrame} expectedSupport=PreorderParent");
            TestAssert.True(allMatched && beforeThread == afterThread && beforeFrame == afterFrame,
                "fixed-result allocation measurement retains literal support on one managed thread/frame");
            TestAssert.True(allocated <= 65536L,
                $"SupportSearchDoesNotAllocatePerIrrelevantDescendant: <=65536 bytes for64 warmed public queries; actual={allocated}");

            marker.Position = new Vector2(490f, 300f);
            TestAssert.True(Query(marker, out support) && support == later,
                "support distances use current live marker position");
            marker.Position = new Vector2(500f, 300f);
            first.Hide();
            TestAssert.True(Query(marker, out support) && support == later,
                "hidden parent and its hidden nested support are excluded immediately");
            first.Show();
            first.Position = new Vector2(550f, 300f);
            TestAssert.True(Query(marker, out support) && support == later,
                "nested support candidates use live global transforms");
            first.Position = new Vector2(510f, 300f);
            deepBranch.RemoveChild(first);
            TestAssert.True(Query(marker, out support) && support == later,
                "removing a support subtree immediately removes all its candidates");
            deepBranch.AddChild(first);
            TestAssert.True(Query(marker, out support) && support == first,
                "reentry immediately restores nested preorder support");
            sector.MoveChild(later, 0);
            TestAssert.True(Query(marker, out support) && support == later,
                "sibling reorder changes a literal tie winner on the next query");
            sector.MoveChild(earlyBranch, 0);
            first.QueueFree();
            TestAssert.True(Query(marker, out support) && support == nested,
                "queued parent candidate is excluded without pruning its still-live descendant");
            nested.QueueFree();
            TestAssert.True(Query(marker, out support) && support == later,
                "queued nested candidate is excluded before deletion flush");
            later.Position = new Vector2(-2f, 300f);
            TestAssert.True(!Query(marker, out support) && support == null,
                "out-of-route remaining candidate yields no support and null output");
            sector.RemoveChild(marker);
            TestAssert.True(!Query(marker, out support) && support == null,
                "unmounted formerly-live marker immediately yields null support");
            sector.AddChild(marker);
            marker.Free();
            TestAssert.True(!Query(marker, out support) && support == null,
                "invalid freed marker immediately yields null support");
            GD.Print("FIREFLY_SUPPORT_FIXED_ALLOCATION_OK assertions=16");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(first) && first.GetParent() == null) first.Free();
            if (GodotObject.IsInstanceValid(marker) && marker.GetParent() == null) marker.Free();
            sector.Free();
        }
    }

    private static bool Query(Marker2D marker, out GrappleSurface2D support) =>
        CanyonFireflyGuidanceController.TryResolveSupport(marker,
            CanyonRouteNodeId.LowCeilingBridge, out support);
}
