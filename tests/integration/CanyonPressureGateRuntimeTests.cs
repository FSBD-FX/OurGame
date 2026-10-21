using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Skitter;
using Knight.Enemies;
using Knight.Mechanisms;

namespace Knight.Tests.Integration;

public static partial class CanyonPressureGateRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await GroundedBoundPlayerOpensActualDoor(tree);
        await EnemyOnPlateAndAirbornePlayerCannotOpen(tree);
        await GraceWarningReentryAndWorldClosure(tree);
        await InteriorDeadColliderAndPlayerCannotBeSealedIn(tree);
        await LateDoorEntryAfterPhysicsDecisionCancelsDeferredClosure(tree);
        await NewlyMountedOrReparentedLateBodyCancelsClosure(tree);
        await OccupantTraversalReleasesPendingNodesOnEarlyReturn(tree);
        await ResetAfterCloseDecisionKeepsLateOccupantSafe(tree);
        await PausedDeferredCloseCannotAdvanceCollision(tree);
        await RemovedOccupantAndGateCannotLeaveStaleClosure(tree);
        await GroundedOuterProximityIsNotAPlatePress(tree);
        await ClosedLeftApproachAndProtectedRightApproachUseRealBodies(tree);
        await RunFix1MountClear(tree);
        await RunFix1MountOccupied(tree);
        await RunFix1Visual(tree);
        await RunFix1PauseTiming(tree);
    }

    public static async Task RunFix1MountClear(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(tree.Root.IsNodeReady(),
            "fix1 clear-mount fixture requires a ready SceneTree root before mounting");
        // Authored collision is input data, not a safe initial-state decision.
        foreach (bool authoredDisabled in new[] { true, false })
        {
            var (root, player, gate) = NewWorld(new Vector2(1550f, 1604f));
            gate.DoorCollision.Disabled = authoredDisabled;
            tree.Root.AddChild(root);
            try
            {
                TestAssert.True(root.IsNodeReady() && gate.IsNodeReady(),
                    $"fix1 clear-mount fixture requires ready nodes before first physics; " +
                    $"root={root.IsNodeReady()} gate={gate.IsNodeReady()}");
                TestAssert.True(gate.DoorCollision.Disabled && gate.IsOpen &&
                                gate.Frame.Traversable && !gate.Frame.HazardActive,
                    "clear mount begins nonblocking before physics regardless of authored door collision flag");
                await Frames(tree, 2);
                TestAssert.True(!gate.DoorCollision.Disabled && !gate.IsOpen &&
                                !gate.Frame.Traversable && !gate.HasOpened && DoorBlocks(root),
                    "clear mount commits a physically closed door after its first safe clearance check");
            }
            finally { root.QueueFree(); await Frames(tree, 2); }
        }
    }

    public static async Task RunFix1MountOccupied(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(tree.Root.IsNodeReady(),
            "fix1 occupied-mount fixture requires a ready SceneTree root before mounting");
        foreach (bool authoredDisabled in new[] { true, false })
        {
            var (root, player, gate) = NewWorld(new Vector2(1550f, 1604f));
            gate.DoorCollision.Disabled = authoredDisabled;
            RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
                .Instantiate<RustShellSquire>();
            rust.GlobalPosition = new Vector2(1740f, 1608f);
            root.AddChild(rust);
            root.MoveChild(rust, 2); // The real collider enters the tree before the gate mounts.
            int authoredMaxHealth = rust.GetNode<HealthComponent>("Health").MaxHealth;
            tree.Root.AddChild(root);
            try
            {
                HealthComponent health = rust.GetNode<HealthComponent>("Health");
                TestAssert.True(root.IsNodeReady() && gate.IsNodeReady() &&
                                rust.IsNodeReady() && health.IsNodeReady(),
                    $"fix1 occupied-mount fixture requires ready nodes before first physics; " +
                    $"root={root.IsNodeReady()} gate={gate.IsNodeReady()} " +
                    $"rust={rust.IsNodeReady()} health={health.IsNodeReady()}");
                rust.SetPhysicsProcess(false);
                TestAssert.True(!rust.IsPhysicsProcessing() &&
                                !rust.GetNode<CollisionShape2D>("BodyCollider").Disabled,
                    "mount fixture stops Rust AI after Ready while preserving its enabled real collider");
                TestAssert.True(authoredMaxHealth > 0 &&
                                health.CurrentHealth == authoredMaxHealth,
                    "mount fixture real Rust health initializes on tree entry before no-damage checks");
                CollisionShape2D bodyShape = rust.GetNode<CollisionShape2D>("BodyCollider");
                bool penetrating = !bodyShape.Disabled &&
                    gate.DoorCollision.Shape.Collide(gate.DoorCollision.GlobalTransform,
                        bodyShape.Shape, bodyShape.GlobalTransform);
                TestAssert.True(penetrating && rust.IsInsideTree(),
                    "mount fixture places an enabled real Rust capsule through the actual door before physics");
                TestAssert.True(gate.DoorCollision.Disabled && gate.IsOpen &&
                                gate.Frame.Traversable && !gate.Frame.HazardActive,
                    "door-penetrating real body mounts with nonblocking committed collision and matching frame");
                await Frames(tree, 2);
                TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled &&
                                gate.Frame.Traversable && !DoorBlocks(root) &&
                                rust.GlobalPosition.DistanceTo(new Vector2(1740f, 1608f)) < 1f &&
                                rust.GetNode<HealthComponent>("Health").CurrentHealth == authoredMaxHealth,
                    "first safe commit keeps the occupied real door open without displacement or damage");
            }
            finally { root.QueueFree(); await Frames(tree, 2); }
        }
    }

    public static async Task RunFix1Visual(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(tree.Root.IsNodeReady(),
            "fix1 visual fixture requires a ready SceneTree root before mounting");
        var (root, player, gate) = NewWorld(new Vector2(1550f, 1604f));
        var visual = new CanyonPressureGateVisual { Name = "DoorPaint" };
        gate.AddChild(visual);
        tree.Root.AddChild(root);
        try
        {
            TestAssert.True(root.IsNodeReady() && gate.IsNodeReady() && visual.IsNodeReady(),
                $"fix1 visual fixture requires ready nodes; root={root.IsNodeReady()} " +
                $"gate={gate.IsNodeReady()} visual={visual.IsNodeReady()}");
            // The authored rectangle is center (1740,1532), size (40,216).
            TestAssert.True(visual.ClosedSlabWorldRect == new Rect2(1720f, 1424f, 40f, 216f),
                $"visible closed slab matches the actual door collision bounds; " +
                $"paint={visual.ClosedSlabWorldRect}");
            gate.GetNode<StaticBody2D>("Door").GlobalPosition = new Vector2(1840f, 1532f);
            TestAssert.True(visual.ClosedSlabWorldRect == new Rect2(1820f, 1424f, 40f, 216f),
                $"visible closed slab follows a translated actual door collision; " +
                $"paint={visual.ClosedSlabWorldRect}");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    public static async Task RunFix1PauseTiming(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(tree.Root.IsNodeReady(),
            "fix1 pause fixture requires a ready SceneTree root before mounting");
        await MidGracePausePreservesRemainingDuration(tree);
        await MidWarningPausePreservesRemainingDuration(tree);
    }

    private static async Task MidGracePausePreservesRemainingDuration(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 8);
            TestAssert.True(gate.IsOpen, "mid-grace fixture first commits an open real door");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await Frames(tree, 30);
            TestAssert.True(gate.IsOpen && !gate.IsDangerTelegraphActive,
                "mid-grace fixture pauses before the warning boundary");
            tree.Paused = true;
            for (int frame = 0; frame < 120; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(gate.IsOpen && !gate.IsDangerTelegraphActive && !DoorBlocks(root),
                "mid-grace pause freezes the open collision and warning countdown");
            tree.Paused = false;
            await Frames(tree, 50);
            TestAssert.True(gate.IsOpen && !gate.IsDangerTelegraphActive,
                "resume preserves remaining grace rather than skipping to warning");
            await Frames(tree, 10);
            TestAssert.True(gate.IsOpen && gate.IsDangerTelegraphActive,
                "warning begins only after the remaining grace physics duration elapses");
        }
        finally
        {
            tree.Paused = false;
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task MidWarningPausePreservesRemainingDuration(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 8);
            player.GlobalPosition = new Vector2(1550f, 1604f);
            for (int frame = 0; frame < 100 && !gate.IsDangerTelegraphActive; frame++)
                await Frames(tree, 1);
            TestAssert.True(gate.IsOpen && gate.IsDangerTelegraphActive,
                "mid-warning fixture reaches a harmless real closing warning");
            await Frames(tree, 8);
            float beforePause = gate.Frame.Phase01;
            TestAssert.True(beforePause > 0f && beforePause < 1f && !DoorBlocks(root),
                "mid-warning fixture pauses before the collision-close boundary");
            tree.Paused = true;
            for (int frame = 0; frame < 120; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(gate.IsOpen && gate.IsDangerTelegraphActive &&
                            Mathf.Abs(gate.Frame.Phase01 - beforePause) < .001f &&
                            !DoorBlocks(root),
                "mid-warning pause freezes exact countdown progress and committed open collision");
            tree.Paused = false;
            await Frames(tree, 10);
            TestAssert.True(gate.IsOpen && gate.IsDangerTelegraphActive &&
                            gate.Frame.Phase01 > beforePause && !DoorBlocks(root),
                "resume consumes only remaining warning time without an early closed frame");
            await Frames(tree, 35);
            TestAssert.True(!gate.IsOpen && !gate.Frame.Traversable && DoorBlocks(root),
                "full remaining warning duration eventually commits a physically closed door");
        }
        finally
        {
            tree.Paused = false;
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    // Catches the anti-crush margin becoming an unintended second outside plate.
    private static async Task GroundedOuterProximityIsNotAPlatePress(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1700f, 1604f));
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 8);
            TestAssert.True(player.IsOnFloor() && !gate.IsOpen && DoorBlocks(root),
                "standing outside at x1700 touches padded anti-crush margin but cannot open a closed gate");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Catches reliance on a BodyExited signal after the enemy or sector is freed.
    private static async Task RemovedOccupantAndGateCannotLeaveStaleClosure(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        CanyonSkitter skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
            .Instantiate<CanyonSkitter>();
        skitter.GlobalPosition = new Vector2(1810f, 1641f);
        root.AddChild(skitter);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 10);
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await Frames(tree, 5);
            skitter.QueueFree();
            await Frames(tree, 2);
            await Frames(tree, 130);
            TestAssert.True(!gate.IsOpen && DoorBlocks(root),
                "freed interior enemy cannot pin an overlap cache open forever");
            gate.QueueFree();
            await Frames(tree, 2);
            CanyonPressurePlateGate replacement = NewGate(player);
            replacement.Name = "ReplacementGate";
            root.AddChild(replacement);
            await Frames(tree, 3);
            TestAssert.True(!replacement.IsOpen && !replacement.HasOpened && DoorBlocks(root),
                "a new instance defaults closed and receives no callback from removed gate");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await Frames(tree, 5);
            TestAssert.True(replacement.IsOpen && replacement.HasOpened && !DoorBlocks(root),
                "replacement opens solely for its newly bound real player");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Catches collision that works only for the left entrant, or seals a right-side actor in.
    private static async Task ClosedLeftApproachAndProtectedRightApproachUseRealBodies(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1670f, 1540f));
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            using (var collision = new KinematicCollision2D())
            {
                bool blocked = player.TestMove(player.GlobalTransform, new Vector2(80f, 0f), collision);
                TestAssert.True(!player.IsOnFloor() && !gate.IsOpen && blocked &&
                                collision.GetCollider() == gate.GetNode<StaticBody2D>("Door"),
                    "closed door physically stops an airborne real Player capsule from the left");
            }
            RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
                .Instantiate<RustShellSquire>();
            rust.GlobalPosition = new Vector2(1800f, 1608f);
            root.AddChild(rust);
            rust.SetPhysicsProcess(false);
            await Frames(tree, 5);
            TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled && !DoorBlocks(root),
                "real right-side Rust body triggers escape protection without pressing outer plate");
            bool rightBlocked = rust.TestMove(rust.GlobalTransform, new Vector2(-60f, 0f));
            TestAssert.True(!rightBlocked,
                "right-side real enemy capsule can move across the committed open door");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Catches a pre-reset deferred close callback mutating the new checkpoint phase.
    private static async Task ResetAfterCloseDecisionKeepsLateOccupantSafe(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        rust.GlobalPosition = new Vector2(1900f, 1608f);
        root.AddChild(rust);
        var mover = new LateDoorEntryMover
        {
            Gate = gate, Body = rust, ResetAfterMove = true, ProcessPhysicsPriority = 100
        };
        root.AddChild(mover);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 10);
            rust.SetPhysicsProcess(false);
            player.GlobalPosition = new Vector2(1550f, 1604f);
            for (int frame = 0; frame < 150 && !mover.ObservedAfterCommit; frame++)
                await Frames(tree, 1);
            TestAssert.True(mover.Moved && mover.ObservedAfterCommit && mover.CommitStayedOpen &&
                            !gate.HasOpened && gate.DoorCollision.Disabled,
                "reset clears local observation and invalidates prior close while current Rust occupies door");
            await Frames(tree, 3);
            TestAssert.True(gate.IsOpen && rust.GlobalPosition.DistanceTo(new Vector2(1740f, 1608f)) < 1f,
                "post-reset occupied door does not close and eject the stationary real collider");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Catches CallDeferred closing a door while physics timers are paused.
    private static async Task PausedDeferredCloseCannotAdvanceCollision(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        var pauser = new PauseAfterCloseDecision
        {
            Gate = gate, ProcessPhysicsPriority = 100
        };
        root.AddChild(pauser);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 10);
            player.GlobalPosition = new Vector2(1550f, 1604f);
            for (int frame = 0; frame < 150 && !pauser.PausedAfterDecision; frame++)
                await Frames(tree, 1);
            TestAssert.True(pauser.PausedAfterDecision && tree.Paused,
                "fixture pauses only after warning-end physics decision");
            for (int frame = 0; frame < 5; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled && !DoorBlocks(root),
                "deferred close does not enable collision during pause");
            tree.Paused = false;
            await Frames(tree, 5);
            TestAssert.True(!gate.IsOpen && DoorBlocks(root),
                "resume completes one checked close without replaying grace");
        }
        finally
        {
            tree.Paused = false;
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private sealed partial class PauseAfterCloseDecision : Node
    {
        public CanyonPressurePlateGate Gate;
        public bool PausedAfterDecision { get; private set; }
        private bool _sawWarning;

        public override void _PhysicsProcess(double delta)
        {
            if (PausedAfterDecision || !GodotObject.IsInstanceValid(Gate)) return;
            _sawWarning |= Gate.IsDangerTelegraphActive;
            if (!_sawWarning || Gate.IsDangerTelegraphActive || !Gate.IsOpen) return;
            GetTree().Paused = true;
            PausedAfterDecision = true;
        }
    }

    // Catches assuming a dead enemy is irrelevant while its real body collider remains enabled.
    private static async Task InteriorDeadColliderAndPlayerCannotBeSealedIn(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        CanyonSkitter skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
            .Instantiate<CanyonSkitter>();
        skitter.GlobalPosition = new Vector2(1810f, 1641f);
        root.AddChild(skitter);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 10);
            TestAssert.True(gate.IsOpen && skitter.IsOnFloor(),
                "fixture establishes a grounded real Skitter inside an actually open door");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            Vector2 enemyPosition = skitter.GlobalPosition;
            int enemyHealth = skitter.GetNode<HealthComponent>("Health").CurrentHealth;
            await Frames(tree, 140);
            TestAssert.True(gate.IsOpen && !gate.IsDangerTelegraphActive &&
                            skitter.IsInsideTree() && skitter.GlobalPosition.DistanceTo(enemyPosition) < 1f &&
                            skitter.GetNode<HealthComponent>("Health").CurrentHealth == enemyHealth,
                "stationary interior enemy holds door without gate displacement, damage, or retirement");
            skitter.GetNode<HealthComponent>("Health").ApplyDamage(
                new DamageRequest(enemyHealth, Vector2.Right, 0f, 991u, skitter.GetInstanceId()));
            TestAssert.True(skitter.GetNode<HealthComponent>("Health").CurrentHealth == 0 &&
                            !skitter.GetNode<CollisionShape2D>("BodyCollider").Disabled,
                "dead accepted Skitter still has its real enabled blocking collider");
            await Frames(tree, 140);
            TestAssert.True(gate.IsOpen && skitter.IsInsideTree(),
                "enabled death collider remains protected after an entire close deadline");
            skitter.GlobalPosition = new Vector2(1590f, 1641f);
            player.GlobalPosition = new Vector2(1810f, 1604f);
            await Frames(tree, 140);
            TestAssert.True(gate.IsOpen && player.IsInsideTree() &&
                            player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                "stationary interior real Player is protected after enemy leaves");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await Frames(tree, 140);
            TestAssert.True(!gate.IsOpen && DoorBlocks(root),
                "door finally closes only after all interior bodies leave");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Catches stale overlap data between physics decision and the deferred shape-enable commit.
    private static async Task LateDoorEntryAfterPhysicsDecisionCancelsDeferredClosure(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
        RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
            .Instantiate<RustShellSquire>();
        rust.GlobalPosition = new Vector2(1900f, 1608f);
        root.AddChild(rust);
        var mover = new LateDoorEntryMover { Gate = gate, Body = rust, ProcessPhysicsPriority = 100 };
        root.AddChild(mover);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 10);
            TestAssert.True(gate.IsOpen, "late-entry fixture starts from committed open door");
            rust.SetPhysicsProcess(false);
            player.GlobalPosition = new Vector2(1550f, 1604f);
            for (int frame = 0; frame < 150 && !mover.Moved; frame++) await Frames(tree, 1);
            TestAssert.True(mover.Moved,
                "fixture enters the real Rust capsule after gate's warning-end physics decision");
            for (int frame = 0; frame < 4 && !mover.ObservedAfterCommit; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(mover.ObservedAfterCommit && mover.CommitStayedOpen,
                $"same deferred queue observes an open shape immediately after actual close commit; " +
                $"observed={mover.ObservedAfterCommit} stayed={mover.CommitStayedOpen} " +
                $"open={gate.IsOpen} disabled={gate.DoorCollision.Disabled} rust={rust.GlobalPosition}");
            await Frames(tree, 3);
            TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled &&
                            rust.IsInsideTree() &&
                            rust.GetNode<HealthComponent>("Health").CurrentHealth > 0 &&
                            rust.GlobalPosition.X >= 1720f && rust.GlobalPosition.X <= 1760f,
                $"deferred close commit rechecks current door shape and cannot crush late Rust entry; " +
                $"open={gate.IsOpen} disabled={gate.DoorCollision.Disabled} " +
                $"health={rust.GetNode<HealthComponent>("Health").CurrentHealth} " +
                $"position={rust.GlobalPosition}");
            rust.GlobalPosition = new Vector2(1900f, 1608f);
            await Frames(tree, 140);
            TestAssert.True(!gate.IsOpen && DoorBlocks(root),
                "after late body leaves, a fresh full warning permits actual closure");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    // Both bodies enter after the warning-end decision but before deferred close.
    // Repeated occupied early exits must not retain nodes after the body is freed.
    private static async Task NewlyMountedOrReparentedLateBodyCancelsClosure(SceneTree tree)
    {
        foreach (bool initiallyMounted in new[] { false, true })
        {
            var (root, player, gate) = NewWorld(new Vector2(1660f, 1604f));
            var outside = new Node2D { Name = "OtherLiveBranch" };
            var entryParent = new Node2D { Name = "NewEnemyBranch" };
            root.AddChild(entryParent);
            RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
                .Instantiate<RustShellSquire>();
            rust.GlobalPosition = new Vector2(1900f, 1608f);
            if (initiallyMounted) outside.AddChild(rust);
            int expectedHealth = rust.GetNode<HealthComponent>("Health").MaxHealth;
            TestAssert.True(expectedHealth > 0, "dynamic fixture uses authored positive real enemy health");
            var mover = new LateDoorEntryMover
            {
                Gate = gate, Body = rust, EntryParent = entryParent,
                ProcessPhysicsPriority = 100
            };
            root.AddChild(mover);
            tree.Root.AddChild(outside);
            tree.Root.AddChild(root);
            try
            {
                await Frames(tree, 10);
                if (initiallyMounted) rust.SetPhysicsProcess(false);
                TestAssert.True(gate.IsOpen, "dynamic late-entry fixture starts with committed open gate");
                player.GlobalPosition = new Vector2(1550f, 1604f);
                for (int frame = 0; frame < 150 && !mover.ObservedAfterCommit; frame++)
                    await Frames(tree, 1);
                TestAssert.True(mover.Moved && mover.ObservedAfterCommit && mover.CommitStayedOpen &&
                    rust.GetParent() == entryParent && rust.IsInsideTree() &&
                    !rust.GetNode<CollisionShape2D>("BodyCollider").Disabled && gate.DoorCollision.Disabled &&
                    rust.GetNode<HealthComponent>("Health").CurrentHealth == expectedHealth,
                    $"same-frame {(initiallyMounted ? "reparented" : "newly mounted")} real body cancels deferred close");
                await Frames(tree, 140);
                TestAssert.True(gate.IsOpen && rust.GetNode<HealthComponent>("Health").CurrentHealth == expectedHealth &&
                    rust.GlobalPosition.DistanceTo(new Vector2(1740f, 1608f)) < 1f,
                    "repeated occupied early exits preserve body without damage or displacement");
                rust.QueueFree();
                await Frames(tree, 140);
                TestAssert.True(!gate.IsOpen && !gate.DoorCollision.Disabled && DoorBlocks(root),
                    "freed occupant leaves no stale traversal reference or false hold; gate closes safely");
            }
            finally
            {
                if (GodotObject.IsInstanceValid(rust) && !rust.IsInsideTree()) rust.Free();
                root.QueueFree(); outside.QueueFree();
                await Frames(tree, 2);
            }
        }
    }

    private static async Task OccupantTraversalReleasesPendingNodesOnEarlyReturn(SceneTree tree)
    {
        var (root, player, gate) = NewWorld(new Vector2(1550f, 1604f));
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            TestAssert.True(!gate.IsOpen, "scratch cleanup fixture begins with a real closed gate");
            // Last child is visited first, leaving floor/player/gate sibling work
            // pending when the interior shape returns true. Inspect immediately:
            // entry Clear on a subsequent call cannot mask a missing finally.
            RustShellSquire rust = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn")
                .Instantiate<RustShellSquire>();
            rust.GlobalPosition = new Vector2(1810f, 1608f);
            root.AddChild(rust);
            rust.SetPhysicsProcess(false);
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic;
            var scan = typeof(CanyonPressurePlateGate).GetMethod("HasCurrentTransformOccupant", flags);
            var scratch = typeof(CanyonPressurePlateGate).GetField("_occupantTraversal", flags)
                ?.GetValue(gate) as System.Collections.Generic.Stack<Node>;
            TestAssert.True(scan != null && scratch != null, "inspect private scratch without a public gameplay API");
            for (int index = 0; index < 3; index++)
            {
                TestAssert.True((bool)scan.Invoke(gate, new object[] { false }),
                    "current-transform scan immediately detects newly mounted interior collider");
                TestAssert.Equal(0, scratch.Count, "occupied early return immediately releases all pending node references");
            }
            rust.QueueFree();
            await Frames(tree, 2);
            TestAssert.True(!(bool)scan.Invoke(gate, new object[] { false }),
                "fresh scan after occupant removal does not use a cached occupant");
            TestAssert.Equal(0, scratch.Count, "empty complete traversal also leaves no pending references");
        }
        finally { root.QueueFree(); await Frames(tree, 2); }
    }

    private static (Node2D Root, CharacterBody2D Player, CanyonPressurePlateGate Gate)
        NewWorld(Vector2 playerPosition)
    {
        var root = new Node2D { Name = "PressureGatePhysicalFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor", GlobalPosition = new Vector2(1750f, 1650f),
            CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(600f, 20f) }
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = playerPosition;
        root.AddChild(player);
        CanyonPressurePlateGate gate = NewGate(player);
        root.AddChild(gate);
        return (root, player, gate);
    }

    private sealed partial class LateDoorEntryMover : Node
    {
        public CanyonPressurePlateGate Gate;
        public RustShellSquire Body;
        public Node2D EntryParent;
        public bool ResetAfterMove;
        public bool Moved { get; private set; }
        public bool ObservedAfterCommit { get; private set; }
        public bool CommitStayedOpen { get; private set; }
        private bool _sawWarning;

        public override void _PhysicsProcess(double delta)
        {
            if (Moved || !GodotObject.IsInstanceValid(Gate) || !GodotObject.IsInstanceValid(Body)) return;
            _sawWarning |= Gate.IsDangerTelegraphActive;
            if (!_sawWarning || Gate.IsDangerTelegraphActive || !Gate.IsOpen) return;
            if (EntryParent != null)
            {
                if (Body.IsInsideTree()) Body.Reparent(EntryParent);
                else EntryParent.AddChild(Body);
                Body.SetPhysicsProcess(false);
            }
            Body.GlobalPosition = new Vector2(1740f, 1608f);
            Moved = true;
            if (ResetAfterMove) Gate.ResetForCheckpoint();
            CallDeferred(nameof(ObserveCommit));
        }

        private void ObserveCommit()
        {
            ObservedAfterCommit = true;
            CommitStayedOpen = GodotObject.IsInstanceValid(Gate) &&
                Gate.IsOpen && Gate.DoorCollision.Disabled;
        }
    }

    // Catches a visual-only close or an unannounced blocking frame after the player leaves.
    private static async Task GraceWarningReentryAndWorldClosure(SceneTree tree)
    {
        var root = new Node2D { Name = "PressureGateClosingCycle" };
        var floor = new StaticBody2D
        {
            Name = "Floor", GlobalPosition = new Vector2(1750f, 1650f),
            CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(600f, 20f) }
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = new Vector2(1660f, 1604f);
        root.AddChild(player);
        CanyonPressurePlateGate gate = NewGate(player);
        var visual = new CanyonPressureGateVisual { Name = "RemovableVisual" };
        gate.AddChild(visual);
        root.AddChild(gate);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 8);
            TestAssert.True(gate.IsOpen && !DoorBlocks(root),
                "open means a real world ray crosses the disabled door shape");
            visual.QueueFree();
            await Frames(tree, 2);
            TestAssert.True(gate.IsOpen && !DoorBlocks(root),
                "removing all optional gate paint leaves the real open collision unchanged");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await Frames(tree, 75);
            TestAssert.True(gate.IsOpen && !gate.IsDangerTelegraphActive,
                "during grace the open door remains traversable without a closing warning");
            await Frames(tree, 15);
            TestAssert.True(gate.IsOpen && gate.IsDangerTelegraphActive &&
                            !gate.Frame.HazardActive && gate.Frame.Traversable,
                "closing warning is visible, harmless, and still traversable");
            player.GlobalPosition = new Vector2(1660f, 1604f);
            await Frames(tree, 3);
            TestAssert.True(gate.IsOpen && !gate.IsDangerTelegraphActive && !DoorBlocks(root),
                "reentry cancels warning without even one blocking frame");
            player.GlobalPosition = new Vector2(1550f, 1604f);
            await Frames(tree, 130);
            TestAssert.True(!gate.IsOpen && !gate.Frame.Traversable && DoorBlocks(root),
                "after full grace and warning the committed door blocks world travel");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static bool DoorBlocks(Node2D root)
    {
        using var query = PhysicsRayQueryParameters2D.Create(new Vector2(1690f, 1550f),
            new Vector2(1790f, 1550f), CombatCollisionLayers.World);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        using var hit = root.GetWorld2D().DirectSpaceState.IntersectRay(query);
        return hit.Count > 0;
    }

    // Catches broad Area/layer activation by an enemy or an airborne player passing over the plate.
    private static async Task EnemyOnPlateAndAirbornePlayerCannotOpen(SceneTree tree)
    {
        var root = new Node2D { Name = "PressureGateEnemyAndAirborne" };
        var floor = new StaticBody2D
        {
            Name = "Floor", GlobalPosition = new Vector2(1750f, 1650f),
            CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(600f, 20f) }
        });
        root.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GlobalPosition = new Vector2(1550f, 1604f);
        root.AddChild(player);
        CanyonSkitter skitter = GD.Load<PackedScene>("res://scenes/enemies/CanyonSkitter.tscn")
            .Instantiate<CanyonSkitter>();
        skitter.GlobalPosition = new Vector2(1660f, 1641f);
        root.AddChild(skitter);
        var gate = NewGate(player);
        root.AddChild(gate);
        tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 20);
            TestAssert.True(skitter.IsOnFloor() && !gate.IsOpen,
                "grounded accepted enemy over plate cannot open a player-only gate");
            skitter.QueueFree();
            await Frames(tree, 2);
            player.GlobalPosition = new Vector2(1660f, 1510f);
            await Frames(tree, 2);
            TestAssert.True(!player.IsOnFloor() && !gate.IsOpen,
                "airborne accepted player passing through the candidate plate Area cannot press it");
            await Frames(tree, 80);
            TestAssert.True(player.IsOnFloor() && gate.IsOpen,
                "same player opens only after real floor-supported foot contact");
        }
        finally
        {
            root.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static CanyonPressurePlateGate NewGate(CharacterBody2D player)
    {
        var gate = new CanyonPressurePlateGate { Name = "Gate", PlayerBody = player };
        var door = new StaticBody2D
        {
            Name = "Door", GlobalPosition = new Vector2(1740f, 1532f),
            CollisionLayer = CombatCollisionLayers.World
        };
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(40f, 216f) }
        });
        gate.AddChild(door);
        return gate;
    }

    // Catches an intent-only IsOpen/Frame that leaves the actual blocking shape enabled.
    private static async Task GroundedBoundPlayerOpensActualDoor(SceneTree tree)
    {
        var fixture = new Node2D { Name = "PressureGateGroundedPlayer" };
        var floor = new StaticBody2D
        {
            Name = "Floor", GlobalPosition = new Vector2(1750f, 1650f),
            CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(300f, 20f) }
        });
        fixture.AddChild(floor);
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "BoundPlayer";
        player.GlobalPosition = new Vector2(1660f, 1604f);
        fixture.AddChild(player);
        var gate = new CanyonPressurePlateGate { Name = "Gate", PlayerBody = player };
        var door = new StaticBody2D
        {
            Name = "Door", GlobalPosition = new Vector2(1740f, 1532f),
            CollisionLayer = CombatCollisionLayers.World
        };
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(40f, 216f) }
        });
        gate.AddChild(door);
        fixture.AddChild(gate);
        tree.Root.AddChild(fixture);
        try
        {
            await Frames(tree, 8);
            TestAssert.True(player.IsOnFloor(), "accepted player body really lands on the floor");
            TestAssert.True(gate.IsOpen && gate.DoorCollision.Disabled,
                "real grounded bound player opens the committed collision shape");
            TestAssert.True(gate.Frame.Traversable && gate.Frame.HazardActive == false,
                "published frame agrees with committed open, never a damaging hazard");
            TestAssert.True(gate.HasOpened, "committed open latches local observation");
        }
        finally
        {
            fixture.QueueFree();
            await Frames(tree, 2);
        }
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}
