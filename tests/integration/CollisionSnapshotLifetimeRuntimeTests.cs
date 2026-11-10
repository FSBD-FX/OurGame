using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;

namespace Knight.Tests.Integration;

// Our scalar route observer must not leave temporary native snapshots to GC.
// This does not identify historical shutdown leaks or qualify a gameplay route.
internal static class CollisionSnapshotLifetimeRuntimeTests
{
    internal static async Task Run(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        RunCleanupLeaseControls();
        var floorShape = new RectangleShape2D { Size = new Vector2(200, 24) };
        var bodyShape = new CapsuleShape2D { Radius = 6, Height = 32 };
        var fixture = new Node2D { Name = "CollisionSnapshotFixture" };
        var floor = new StaticBody2D
        {
            Name = "OwnedFloor", Position = new Vector2(0, 76),
            CollisionLayer = 1, CollisionMask = 1
        };
        floor.AddChild(new CollisionShape2D { Shape = floorShape });
        var body = new CharacterBody2D
        {
            Name = "OwnedBody", Position = new Vector2(0, 48),
            CollisionLayer = 1, CollisionMask = 1
        };
        body.AddChild(new CollisionShape2D { Shape = bodyShape });
        fixture.AddChild(floor);
        fixture.AddChild(body);
        ulong snapshotId = 0;
        WeakReference<KinematicCollision2D> cleanupLease = null;
        Exception primary = null;
        Exception cleanupFault = null;
        bool bodyReleased = false, aliasPreserved = false, collected = false, snapshotAlive = false;
        int observed = 0;
        try
        {
            tree.Root.AddChild(fixture);
            TestAssert.Equal("airborne", PrologueCanyonGeneratedMainRouteTests.DescribeFloorCollider(
                body, (_, _) => observed++), "unmoved native body has no floor snapshot");
            TestAssert.Equal(0, observed, "airborne observer does not acquire a contact");
            for (int frame = 0; frame < 6 && !body.IsOnFloor(); frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                body.Velocity = new Vector2(0, 120);
                body.MoveAndSlide();
            }
            TestAssert.True(body.IsOnFloor() && body.GetSlideCollisionCount() == 1,
                "owned primitive body must make exactly one real floor contact");
            string expected;
            using (NodePath path = floor.GetPath()) expected = $"{path}#{floor.GetInstanceId()}";
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            using (KinematicCollision2D peer = body.GetSlideCollision(0))
            {
                TestAssert.True(peer.GetCollider() == floor && peer.GetNormal().Y < -.9f,
                    "native contact belongs to the exact owned floor");
                ulong peerId = peer.GetInstanceId();
                string actual = PrologueCanyonGeneratedMainRouteTests.DescribeFloorCollider(body, (id, lease) =>
                {
                    snapshotId = id;
                    cleanupLease = lease;
                    observed++;
                });
                TestAssert.Equal(expected, actual, "real scalar floor description remains exact");
                TestAssert.True(observed == 1 && snapshotId != 0 && snapshotId != peerId,
                    "record the helper's own acquired snapshot, not its held peer");
                aliasPreserved = GodotObject.IsInstanceValid(peer) && peer.GetInstanceId() == peerId &&
                    peer.GetCollider() == floor && peer.GetNormal().Y < -.9f;
                TestAssert.True(aliasPreserved, "scalar observation preserves a live peer contact");
            }
            ulong bodyId = body.GetInstanceId();
            body.Free();
            bodyReleased = !GodotObject.IsInstanceIdValid(bodyId);
            snapshotAlive = GodotObject.IsInstanceIdValid(snapshotId);
            collected = gen0 != GC.CollectionCount(0) || gen1 != GC.CollectionCount(1) ||
                gen2 != GC.CollectionCount(2);
            TestAssert.True(bodyReleased, "owned body must be synchronously released before assertion");
            TestAssert.True(!collected, "COLLISION_SNAPSHOT_INCONCLUSIVE: managed collection changed during assay");
            TestAssert.True(!snapshotAlive,
                "COLLISION_SNAPSHOT_LIFETIME_RED: scalar floor observer retains a native temporary after body release without GC");
        }
        catch (Exception exception) { primary = exception; }
        finally
        {
            primary = CompleteOwnedCleanup(primary, new Action[]
            {
                () => { if (GodotObject.IsInstanceValid(fixture)) fixture.Free(); },
                () =>
                {
                    TestAssert.True(ReleaseObservedSnapshot(snapshotId, cleanupLease),
                        "COLLISION_SNAPSHOT_CLEANUP_INCONCLUSIVE: weak lease unavailable while native temporary remains");
                },
                bodyShape.Dispose,
                floorShape.Dispose
            }, out cleanupFault);
        }
        GD.Print($"COLLISION_SNAPSHOT_LIFETIME_ROW observed={observed} owner_released={bodyReleased} " +
            $"alias_preserved={aliasPreserved} collection_changed={collected} temporary_survived={snapshotAlive} " +
            $"cleanup_ok={cleanupFault == null} snapshot_id={snapshotId} signed_id={unchecked((long)snapshotId)}");
        if (primary != null)
        {
            if (cleanupFault != null) GD.Print("COLLISION_SNAPSHOT_CLEANUP_FAULT " + cleanupFault);
            ExceptionDispatchInfo.Capture(primary).Throw();
        }
        GD.Print("COLLISION_SNAPSHOT_CLEANUP_CONTROLS_OK weak_loss_refused=true same_wrapper_only=true " +
            "primary_preserved=true all_cleanup_attempted=true forced_gc=false simulated_lease_loss=true");
        GD.Print("COLLISION_SNAPSHOT_LIFETIME_OK native_floor=true scalar_observer=true peer_preserved=true " +
            "temporary_released=true forced_gc=false production_changed=false human=false");
    }

    private static bool ReleaseObservedSnapshot(ulong id, WeakReference<KinematicCollision2D> lease)
    {
        if (id == 0 || !GodotObject.IsInstanceIdValid(id)) return true;
        // A short weak reference cannot recover a queued finalizer. Promote the
        // ORIGINAL wrapper only now, after measurement; never bind by scalar ID.
        if (lease == null || !lease.TryGetTarget(out KinematicCollision2D remaining) ||
            !GodotObject.IsInstanceValid(remaining)) return !GodotObject.IsInstanceIdValid(id);
        if (remaining.GetInstanceId() != id || remaining.GetReferenceCount() != 1) return false;
        remaining.Dispose();
        return !GodotObject.IsInstanceIdValid(id);
    }

    private static Exception CompleteOwnedCleanup(Exception primary, Action[] actions, out Exception cleanupFault)
    {
        cleanupFault = null;
        foreach (Action action in actions)
        {
            try { action(); }
            catch (Exception exception) { cleanupFault ??= exception; }
        }
        return primary ?? cleanupFault;
    }

    private static void RunCleanupLeaseControls()
    {
        // Strong ownership is deliberate ONLY in this control. Clearing the
        // short weak reference simulates loss after measurement, without GC.
        using (var owned = new KinematicCollision2D())
        {
            ulong id = owned.GetInstanceId();
            var missing = new WeakReference<KinematicCollision2D>(owned);
            missing.SetTarget(null);
            bool released = ReleaseObservedSnapshot(id, missing);
            TestAssert.True(!released && GodotObject.IsInstanceValid(owned) && owned.GetInstanceId() == id,
                "COLLISION_SNAPSHOT_WEAK_LEASE_RED: cleanup must refuse lost weak ownership despite a live native ID");
        }
        using (var owned = new KinematicCollision2D())
        {
            ulong id = owned.GetInstanceId();
            var lease = new WeakReference<KinematicCollision2D>(owned);
            TestAssert.True(ReleaseObservedSnapshot(id, lease) && !GodotObject.IsInstanceIdValid(id),
                "valid exact original weak target permits owned cleanup");
        }
        using (var owned = new KinematicCollision2D())
        using (var other = new KinematicCollision2D())
        {
            var wrongLease = new WeakReference<KinematicCollision2D>(other);
            TestAssert.True(!ReleaseObservedSnapshot(owned.GetInstanceId(), wrongLease) &&
                GodotObject.IsInstanceValid(owned) && GodotObject.IsInstanceValid(other),
                "mismatched weak identity releases neither owned object");
        }
        var primary = new InvalidOperationException("owned primary control");
        var cleanup = new InvalidOperationException("owned cleanup control");
        int attempted = 0;
        Exception selected = CompleteOwnedCleanup(primary, new Action[]
        {
            () => { attempted++; throw cleanup; }, () => attempted++
        }, out Exception recorded);
        TestAssert.True(ReferenceEquals(primary, selected) && ReferenceEquals(cleanup, recorded) && attempted == 2,
            "primary plus cleanup faults preserve primary, record cleanup and still attempt next owner");
        selected = CompleteOwnedCleanup(null, new Action[] { () => throw cleanup }, out recorded);
        TestAssert.True(ReferenceEquals(cleanup, selected) && ReferenceEquals(cleanup, recorded),
            "cleanup-only failure cannot report success");
        selected = CompleteOwnedCleanup(primary, new Action[] { () => attempted++ }, out recorded);
        TestAssert.True(ReferenceEquals(primary, selected) && recorded == null, "primary-only failure preserved");
        selected = CompleteOwnedCleanup(null, new Action[] { () => attempted++ }, out recorded);
        TestAssert.True(selected == null && recorded == null && attempted == 4, "successful cleanup stays successful");
    }
}
