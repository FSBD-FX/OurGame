using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Tests.Integration;

/// <summary>Actual live-node queries, without a flow controller or persistence writer.</summary>
public static class RoomProtectionRegionRuntimeTests
{
    public static void Run(SceneTree tree)
    {
        var scope = new Node2D { Name = "ProtectionRegionFixture" };
        var decorations = new Node2D { Name = "Decorations" };
        scope.AddChild(decorations);
        for (int index = 0; index < 128; index++)
            decorations.AddChild(new Node2D { Name = "Decoration" + index });
        var actor = new Node2D { Name = "Actor", Position = new Vector2(100, 200) };
        actor.AddToGroup("player");
        var target = new ThreadTarget2D { Name = "Target", Position = new Vector2(500, 200),
            Material = GrappleSurfaceMaterial.GripStone };
        var shelf = new StaticBody2D { Name = "Shelf", Position = new Vector2(400, 300),
            CollisionLayer = CombatCollisionLayers.World };
        var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(80, 40) } };
        shelf.AddChild(shape);
        shelf.SetMeta("catch_shelf", false);
        scope.AddChild(actor); scope.AddChild(target); scope.AddChild(shelf);
        try
        {
            tree.Root.AddChild(scope);
            AssertRegions(scope, new Rect2(60, 138, 80, 124), new Rect2(468, 168, 64, 64));
            actor.RemoveFromGroup("player");
            TestAssert.True(!RoomProtectionRegionAdapter.IsCandidateNode(actor),
                "removing the live player group removes candidate eligibility");
            AssertRegions(scope, new Rect2(468, 168, 64, 64));
            actor.AddToGroup("player");
            TestAssert.True(RoomProtectionRegionAdapter.IsCandidateNode(actor),
                "restoring the live player group immediately restores candidate eligibility");
            actor.Position = new Vector2(200, 250);
            AssertRegions(scope, new Rect2(160, 188, 80, 124), new Rect2(468, 168, 64, 64));
            target.RemoveFromGroup("thread_targets");
            AssertRegions(scope, new Rect2(160, 188, 80, 124));
            target.AddToGroup("thread_targets");
            target.Hide();
            AssertRegions(scope, new Rect2(160, 188, 80, 124));
            target.Show();
            shelf.SetMeta("catch_shelf", true);
            AssertRegions(scope, new Rect2(160, 188, 80, 124), new Rect2(468, 168, 64, 64),
                new Rect2(360, 274, 80, 12));
            shape.Disabled = true;
            AssertRegions(scope, new Rect2(160, 188, 80, 124), new Rect2(468, 168, 64, 64));
            shape.Disabled = false;
            shelf.RemoveMeta("catch_shelf");
            AssertRegions(scope, new Rect2(160, 188, 80, 124), new Rect2(468, 168, 64, 64));
            scope.RemoveChild(target);
            AssertRegions(scope, new Rect2(160, 188, 80, 124));
            scope.AddChild(target);
            AssertRegions(scope, new Rect2(160, 188, 80, 124), new Rect2(468, 168, 64, 64));
            scope.Hide();
            AssertRegions(scope);
            scope.Show();
            GD.Print("ROOM_PROTECTION_LIVE_STATE_OK ordered_regions=true groups=true transforms=true " +
                "shelf_metadata=true disabled=true hidden=true remove_reentry=true");

            // Hypothesis: repeated conversion of constant group/metadata names makes
            // ordinary visible decorations allocate wrappers on every public query.
            // Warm all actual wrappers; do not include setup, logging or expected arrays.
            for (int index = 0; index < 8; index++) RoomProtectionRegionAdapter.CollectGameplay(scope, null);
            int beforeThread = System.Environment.CurrentManagedThreadId;
            ulong beforeFrame = Engine.GetProcessFrames();
            long before = GC.GetAllocatedBytesForCurrentThread();
            bool unchanged = true;
            for (int index = 0; index < 64; index++)
            {
                IReadOnlyList<Rect2> regions = RoomProtectionRegionAdapter.CollectGameplay(scope, null);
                unchanged &= regions.Count == 2 && regions[0] == new Rect2(160, 188, 80, 124) &&
                    regions[1] == new Rect2(468, 168, 64, 64);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestAssert.True(beforeThread == System.Environment.CurrentManagedThreadId &&
                beforeFrame == Engine.GetProcessFrames() && unchanged,
                "measured queries remain synchronous and preserve exact current regions");
            GD.Print($"ROOM_PROTECTION_QUERY_COST calls=64 decorations=128 threadBytes={allocated} " +
                "basis=synchronous_warmed_live_query_not_frame_time");
            // 4 KiB/query permits result lists and real node bindings but not a new
            // managed StringName wrapper for each irrelevant visible decoration.
            TestAssert.True(allocated <= 64L * 4096,
                $"protection query budget <=262144 bytes for64 calls; actual={allocated}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(target) && target.GetParent() == null) target.Free();
            scope.Free();
        }
    }

    private static void AssertRegions(Node2D scope, params Rect2[] expected)
    {
        IReadOnlyList<Rect2> regions = RoomProtectionRegionAdapter.CollectGameplay(scope, null);
        TestAssert.Equal(expected.Length, regions.Count, "live protection region count");
        for (int index = 0; index < expected.Length; index++)
            TestAssert.Equal(expected[index], regions[index], "live depth-first protection rectangle " + index);
    }
}
