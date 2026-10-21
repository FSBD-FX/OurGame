using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;

namespace Knight.Tests.Integration;

public static class StableClawPullRuntimeTests
{
    public static async Task RunSwingContactSafetyRegression(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await SwingSideColumnObstructs(tree, sameTargetBody: false);
        await SwingSideColumnObstructs(tree, sameTargetBody: true);
        await SwingLateSideColumnAfterCeilingSlide(tree);
    }

    // The recorded positive first touches the ceiling at x464.05133 (capsule
    // right484.05133), then reaches right488.75494 by sample23. One fixed left486
    // column tests the later side encounter without searching for a passing layout.
    private static async Task SwingLateSideColumnAfterCeilingSlide(SceneTree tree)
    {
        var fixture = new Fixture(tree, new Vector2(400f, 400f),
            new Vector2(400f, 180f), new Vector2(260f, 20f), 436f);
        StableClawPostMotorObserver observer = null;
        Exception publicationFailure = null;
        bool subscribed = false, previousCeilingContact = false;
        MechanicalClawFrame arrival = default;
        StaticBody2D column = null;
        var resolver = new MechanicalClawTargetResolver();
        var releases = new List<MechanicalClawFrame>();
        ulong firstCeilingTick = 0, firstSideTick = 0, releaseTick = 0;
        ulong previousTick = 0, previousSequence = 0;
        int sample = -1, postSamples = 0, mixedTicks = 0, continuousMixedTicks = 0;
        try
        {
            await fixture.Start();
            await fixture.CastAt(Key.W);
            await fixture.Until(() => fixture.Claw.CurrentFrame.Arrived &&
                fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing,
                180, "late side column reaches genuine ceiling arrival before lateral input");
            arrival = fixture.Claw.CurrentFrame;
            TestAssert.True(fixture.Events.Any(frame => frame.State == MechanicalClawState.Reel) &&
                arrival.SurfaceNormal.Dot(Vector2.Down) > .99f && Math.Abs(arrival.AnchorPoint.Y - 190f) < .1f,
                "late side column starts with the same actual underside and preceding Reel");
            var bodyCollider = fixture.Player.GetNode<CollisionShape2D>("BodyCollider");
            using var capsule = (CapsuleShape2D)bodyCollider.Shape;
            using var inset = new CapsuleShape2D { Radius = capsule.Radius - .5f, Height = capsule.Height - 1f };
            using var columnShape = new RectangleShape2D { Size = new Vector2(12f, 90f) };
            column = new StaticBody2D
            {
                Name = "LateIndependentSideColumn", Position = new Vector2(492f, 260f),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None
            };
            column.AddChild(new CollisionShape2D { Shape = columnShape });
            fixture.Root.AddChild(column);
            TestAssert.True(bodyCollider.GlobalPosition.X + capsule.Radius < 485f,
                "fixed late column starts separate from the full saved capsule");
            var reader = fixture.Player.GetNode<PlayerInputReader>("Input");
            previousTick = Engine.GetPhysicsFrames();
            previousSequence = reader.CurrentFrameSequence;
            fixture.Claw.ClawFrameChanged += ObserveRelease;
            subscribed = true;
            observer = new StableClawPostMotorObserver
            {
                Name = "LateSideColumnPostMotor", ProcessPhysicsPriority = 100,
                Observe = () =>
                {
                    ulong tick = Engine.GetPhysicsFrames();
                    TestAssert.True(tick == previousTick + 1 && reader.CurrentFrameSequence == previousSequence + 1,
                        "late-column witness observes consecutive actual physics and input samples");
                    previousTick = tick; previousSequence = reader.CurrentFrameSequence; postSamples++;
                    bool ceilingContact = false, sideContact = false;
                    var contacts = new List<string>();
                    for (int index = 0; index < fixture.Player.GetSlideCollisionCount(); index++)
                    {
                        using KinematicCollision2D contact = fixture.Player.GetSlideCollision(index);
                        Node collider = contact.GetCollider() as Node;
                        Vector2 normal = contact.GetNormal();
                        bool original = collider != null &&
                            MechanicalClawTargetResolver.ColliderBelongsToSurface(collider, fixture.Anchor);
                        ceilingContact |= original && normal.Dot(Vector2.Down) > .99f;
                        sideContact |= collider == column && normal.Dot(Vector2.Left) > .99f;
                        contacts.Add($"id={collider?.GetInstanceId() ?? 0UL},original_target={original}," +
                            $"column={collider == column},normal={normal}");
                    }
                    if (ceilingContact && firstCeilingTick == 0) firstCeilingTick = tick;
                    if (sideContact && firstSideTick == 0) firstSideTick = tick;
                    bool mixed = ceilingContact && sideContact;
                    if (mixed) mixedTicks++;
                    if (mixed && previousCeilingContact) continuousMixedTicks++;
                    previousCeilingContact = ceilingContact;
                    MechanicalClawFrame frame = fixture.Claw.CurrentFrame;
                    MechanicalClawTargetSnapshot visibility = resolver.SnapshotLocked(fixture.Player,
                        fixture.Anchor, arrival.AnchorPoint, arrival.SurfaceNormal);
                    using var ray = PhysicsRayQueryParameters2D.Create(fixture.Player.GlobalPosition,
                        new Vector2(498f, fixture.Player.GlobalPosition.Y), CombatCollisionLayers.World);
                    ray.CollideWithAreas = false;
                    using var sideHit = fixture.Player.GetWorld2D().DirectSpaceState.IntersectRay(ray);
                    bool knownSide = sideHit.ContainsKey("collider") && sideHit["collider"].AsGodotObject() == column &&
                        sideHit["normal"].AsVector2().Dot(Vector2.Left) > .99f &&
                        Mathf.Abs(sideHit["position"].AsVector2().X - 486f) < .1f;
                    GD.Print($"STABLE_CLAW_LATE_COLUMN_TICK sample={sample} physics={tick} " +
                        $"sequence={previousSequence} position={fixture.Player.GlobalPosition} velocity={fixture.Player.Velocity} " +
                        $"state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} cast={frame.CastId} " +
                        $"correction={frame.VelocityCorrection} anchor={frame.AnchorPoint} dock={frame.DockPoint} " +
                        $"side_query={knownSide} side_gap={486f - bodyCollider.GlobalPosition.X - capsule.Radius:F6} " +
                        $"ceiling={ceilingContact} side={sideContact} mixed={mixed} " +
                        $"first_ceiling_tick={firstCeilingTick} first_side_tick={firstSideTick} release_tick={releaseTick} " +
                        $"los={visibility.HasLineOfSight} input={reader.Current} actual_slides=[{string.Join(';', contacts)}]");
                    TestAssert.True(knownSide && visibility.Exists && visibility.HasLineOfSight,
                        "fixed late column has its native left face and never obstructs the original rope LOS");
                    if (sideContact)
                        TestAssert.True(firstCeilingTick != 0 && firstCeilingTick < tick,
                            "native late side contact follows an earlier real original-target ceiling slide");
                    if (releases.Count == 0)
                        TestAssert.True(frame.State == MechanicalClawState.Swing && frame.CastId == arrival.CastId &&
                            fixture.Claw.CurrentTarget == fixture.Anchor,
                            "late-column approach keeps the original arrived cast until its actual obstruction release");
                    TestAssert.True(bodyCollider.GlobalPosition.X + capsule.Radius <= 486.1f,
                        "late-column control never crosses the left face with the original full capsule");
                    AssertOutsideSavedFace(fixture, ceiling: true);
                    var excluded = new Godot.Collections.Array<Rid> { fixture.Player.GetRid() };
                    using var excludedOwner = (Godot.Collections.Array)excluded;
                    using var overlapQuery = new PhysicsShapeQueryParameters2D
                    {
                        Shape = inset, Transform = bodyCollider.GlobalTransform, Exclude = excluded,
                        CollisionMask = CombatCollisionLayers.World, CollideWithBodies = true, CollideWithAreas = false
                    };
                    var hits = fixture.Player.GetWorld2D().DirectSpaceState.IntersectShape(overlapQuery, 32);
                    using var hitsOwner = (Godot.Collections.Array)hits;
                    bool overlaps = false;
                    foreach (Godot.Collections.Dictionary hit in hits)
                        using (hit) overlaps |= hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == column;
                    TestAssert.True(!overlaps, "late-column actual capsule inset never penetrates its independent obstacle");
                    PlayerInputFrame input = reader.Current;
                    TestAssert.True(fixture.HeldCount == 1 && input.MoveAxis == 1f &&
                        !input.JumpPressed && !input.JumpHeld && !input.DashPressed && !input.AttackPressed &&
                        !input.ClawPressed && !input.ThreadPressed && !input.ThreadHeld && !input.ThreadReleased &&
                        !input.ShieldHeld && !input.InteractPressed && !input.PrimaryPressed && !input.PrimaryHeld &&
                        !input.PrimaryReleased && !input.AttackModePressed && !input.GrappleModePressed && input.RequestedSlot == 0 &&
                        fixture.Health.CurrentHealth == 5 && fixture.Motor.AcceptedJumpCount == 0 &&
                        fixture.Motor.AcceptedDashCount == 0 && fixture.Attacks == 0,
                        "late-column witness uses only continuous D and preserves HP5 without extra actions");
                }
            };
            fixture.Root.AddChild(observer);
            // No extra neutral registration tick: retain the positive's arrival-to-D sequence.
            fixture.Key(Key.D, true);
            for (sample = 0; sample < 24; sample++)
            {
                await fixture.Frames(1);
                ThrowCallbackFailure();
            }
            TestAssert.True(firstCeilingTick != 0 && releaseTick > firstCeilingTick && releases.Count == 1 &&
                releases[0].Failure == MechanicalClawFailure.Obstructed &&
                releases[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed &&
                releases[0].CastId == arrival.CastId && !releases[0].Arrived,
                "StableClawLateSideColumn: an independent side obstacle releases once as Obstructed after a real ceiling slide");
            TestAssert.True(fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null &&
                fixture.Events.All(frame => frame.State != MechanicalClawState.Boost &&
                    frame.ReleaseKind != MechanicalClawReleaseKind.Boost && !frame.BoostConsumed),
                "late-column obstruction relinquishes the real target without granting a boost");
            GD.Print($"STABLE_CLAW_LATE_COLUMN_OK driver_frames=24 actual_post_ticks={postSamples} " +
                $"first_ceiling_tick={firstCeilingTick} first_side_tick={firstSideTick} release_tick={releaseTick} " +
                $"mixed_ticks={mixedTicks} continuous_mixed_ticks={continuousMixedTicks} " +
                $"coverage={(continuousMixedTicks > 0 ? "ACTUAL_CONTINUOUS_MIXED_CONTACT" : "SEQUENTIAL_ONLY_REVIEW_RISK_NOT_COVERED")}");
        }
        finally
        {
            GD.Print($"STABLE_CLAW_LATE_COLUMN_COVERAGE first_ceiling_tick={firstCeilingTick} " +
                $"first_side_tick={firstSideTick} release_tick={releaseTick} mixed_ticks={mixedTicks} " +
                $"continuous_mixed_ticks={continuousMixedTicks} " +
                $"same_frame_masking_risk_covered={continuousMixedTicks > 0}");
            if (subscribed) fixture.Claw.ClawFrameChanged -= ObserveRelease;
            if (observer != null && GodotObject.IsInstanceValid(observer)) observer.SetPhysicsProcess(false);
            await fixture.Dispose();
        }

        void ThrowCallbackFailure()
        {
            Exception failure = publicationFailure ?? observer?.Failure;
            if (failure != null) throw new InvalidOperationException("Late side-column observation failed", failure);
        }
        void ObserveRelease(MechanicalClawFrame frame, GrappleSurface2D _)
        {
            if (frame.State != MechanicalClawState.Released || publicationFailure != null) return;
            try
            {
                releaseTick = Engine.GetPhysicsFrames();
                releases.Add(frame);
                MechanicalClawTargetSnapshot visibility = resolver.SnapshotLocked(fixture.Player,
                    fixture.Anchor, arrival.AnchorPoint, arrival.SurfaceNormal);
                GD.Print($"STABLE_CLAW_LATE_COLUMN_RELEASE physics={releaseTick} first_ceiling_tick={firstCeilingTick} " +
                    $"position={fixture.Player.GlobalPosition} velocity={fixture.Player.Velocity} " +
                    $"state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} los={visibility.HasLineOfSight}");
                TestAssert.True(firstCeilingTick != 0 && firstCeilingTick < releaseTick &&
                    visibility.Exists && visibility.HasLineOfSight,
                    "late side release follows real ceiling contact with the original anchor still visible");
            }
            catch (Exception exception) { publicationFailure ??= exception; }
        }
    }

    private static async Task SwingSideColumnObstructs(SceneTree tree, bool sameTargetBody)
    {
        var fixture = new Fixture(tree, new Vector2(400f, 400f),
            new Vector2(400f, 180f), new Vector2(260f, 20f), 436f);
        StableClawPostMotorObserver observer = null;
        Exception publicationFailure = null;
        bool subscribed = false;
        MechanicalClawFrame arrival = default;
        var releases = new List<MechanicalClawFrame>();
        var resolver = new MechanicalClawTargetResolver();
        StaticBody2D columnBody = null;
        int postSamples = 0;
        ulong previousTick = 0;
        try
        {
            await fixture.Start();
            await fixture.CastAt(Key.W);
            await fixture.Until(() => fixture.Claw.CurrentFrame.Arrived &&
                fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing,
                180, "Swing side-column control reaches genuine ceiling arrival before adding its obstacle");
            arrival = fixture.Claw.CurrentFrame;
            TestAssert.True(fixture.Events.Any(frame => frame.State == MechanicalClawState.Reel) &&
                arrival.SurfaceNormal.Dot(Vector2.Down) > .99f && Math.Abs(arrival.AnchorPoint.Y - 190f) < .1f,
                "Swing side-column control starts from the native underside and actual preceding Reel");
            var bodyCollider = fixture.Player.GetNode<CollisionShape2D>("BodyCollider");
            using var capsule = (CapsuleShape2D)bodyCollider.Shape;
            using var inset = new CapsuleShape2D { Radius = capsule.Radius - .5f, Height = capsule.Height - 1f };
            using var sideShape = new RectangleShape2D { Size = new Vector2(12f, 90f) };
            if (sameTargetBody)
            {
                columnBody = fixture.Anchor.GetParent<StaticBody2D>();
                columnBody.AddChild(new CollisionShape2D
                {
                    Name = "FixedSideColumn", Position = new Vector2(46f, 80f), Shape = sideShape
                });
            }
            else
            {
                columnBody = new StaticBody2D
                {
                    Name = "IndependentSideColumn", Position = new Vector2(446f, 260f),
                    CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None
                };
                columnBody.AddChild(new CollisionShape2D { Shape = sideShape });
                fixture.Root.AddChild(columnBody);
            }
            // Register the one fixed native obstacle while the original cast stays neutral.
            await fixture.Frames(1);
            AssertBodySafe();
            TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing &&
                fixture.Claw.CurrentFrame.CastId == arrival.CastId && fixture.Claw.CurrentTarget == fixture.Anchor &&
                fixture.Player.GlobalPosition.X + capsule.Radius < 440f - 1f,
                "fixed side column begins separate from the capsule without replacing the arrived cast");
            MechanicalClawTargetSnapshot initialSnapshot = resolver.SnapshotLocked(fixture.Player,
                fixture.Anchor, arrival.AnchorPoint, arrival.SurfaceNormal);
            TestAssert.True(initialSnapshot.Exists && initialSnapshot.HasLineOfSight,
                "side-column control preserves the original anchor visibility before lateral input");
            using (var query = PhysicsRayQueryParameters2D.Create(fixture.Player.GlobalPosition,
                new Vector2(452f, fixture.Player.GlobalPosition.Y), CombatCollisionLayers.World))
            {
                query.CollideWithAreas = false;
                using var hit = fixture.Player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                TestAssert.True(hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == columnBody &&
                    hit["normal"].AsVector2().Dot(Vector2.Left) > .99f &&
                    Mathf.Abs(hit["position"].AsVector2().X - 440f) < .1f,
                    "native ray verifies the fixed column's actual left face and exact body identity");
                GD.Print($"STABLE_CLAW_SWING_COLUMN_SETUP same_target={sameTargetBody} " +
                    $"body={columnBody.GetInstanceId()} hit={hit["position"].AsVector2()} " +
                    $"normal={hit["normal"].AsVector2()} player={fixture.Player.GlobalPosition} " +
                    $"anchor={arrival.AnchorPoint} dock={arrival.DockPoint} los={initialSnapshot.HasLineOfSight}");
            }
            fixture.Claw.ClawFrameChanged += ObserveRelease;
            subscribed = true;
            previousTick = Engine.GetPhysicsFrames();
            observer = new StableClawPostMotorObserver
            {
                Name = "SwingSideColumnPostMotor", ProcessPhysicsPriority = 100,
                Observe = () =>
                {
                    ulong tick = Engine.GetPhysicsFrames();
                    TestAssert.True(tick == previousTick + 1,
                        "side-column safety witness covers consecutive actual physics ticks");
                    previousTick = tick;
                    postSamples++;
                    AssertBodySafe();
                }
            };
            fixture.Root.AddChild(observer);
            fixture.Key(Key.D, true);
            for (int tick = 0; tick < 24; tick++)
            {
                await fixture.Frames(1);
                ThrowObserverFailure();
                AssertBodySafe();
            }
            TestAssert.True(releases.Count == 1 && releases[0].Failure == MechanicalClawFailure.Obstructed &&
                releases[0].ReleaseKind == MechanicalClawReleaseKind.Obstructed &&
                releases[0].CastId == arrival.CastId && !releases[0].Arrived,
                "StableClawSwingSideColumn: lateral contact with a non-target or wrong target face releases once as Obstructed");
            TestAssert.True(fixture.Events.All(frame => frame.State != MechanicalClawState.Boost &&
                frame.ReleaseKind != MechanicalClawReleaseKind.Boost && !frame.BoostConsumed),
                "side-column obstruction grants no boost or substitute release");
            TestAssert.True(fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null,
                "side-column obstruction relinquishes its target and residency");
            GD.Print($"STABLE_CLAW_SWING_COLUMN_OK same_target={sameTargetBody} driver_frames=24 " +
                $"actual_post_ticks={postSamples} releases={releases.Count} position={fixture.Player.GlobalPosition} " +
                $"velocity={fixture.Player.Velocity} health={fixture.Health.CurrentHealth}");

            void AssertBodySafe()
            {
                TestAssert.True(bodyCollider.GlobalPosition.X + capsule.Radius <= 440.1f,
                    "Swing side-column control never crosses the fixed left face with its original capsule");
                TestAssert.True(fixture.Health.CurrentHealth == 5 && fixture.Motor.AcceptedJumpCount == 0 &&
                    fixture.Motor.AcceptedDashCount == 0 && fixture.Attacks == 0,
                    "Swing side-column control preserves HP5 and adds no jump, dash or attack");
                var excluded = new Godot.Collections.Array<Rid> { fixture.Player.GetRid() };
                using var excludedOwner = (Godot.Collections.Array)excluded;
                using var query = new PhysicsShapeQueryParameters2D
                {
                    Shape = inset, Transform = bodyCollider.GlobalTransform, Exclude = excluded,
                    CollisionMask = CombatCollisionLayers.World, CollideWithBodies = true, CollideWithAreas = false
                };
                var hits = fixture.Player.GetWorld2D().DirectSpaceState.IntersectShape(query, 32);
                using var owner = (Godot.Collections.Array)hits;
                bool overlap = false;
                foreach (Godot.Collections.Dictionary hit in hits)
                {
                    using (hit) overlap |= hit.ContainsKey("collider") &&
                        hit["collider"].AsGodotObject() == columnBody;
                }
                TestAssert.True(!overlap, "actual capsule inset never penetrates the column body");
            }
        }
        finally
        {
            if (subscribed) fixture.Claw.ClawFrameChanged -= ObserveRelease;
            if (observer != null && GodotObject.IsInstanceValid(observer)) observer.SetPhysicsProcess(false);
            await fixture.Dispose();
        }

        void ThrowObserverFailure()
        {
            Exception failure = publicationFailure ?? observer?.Failure;
            if (failure != null) throw new InvalidOperationException("Swing side-column observation failed", failure);
        }
        void ObserveRelease(MechanicalClawFrame frame, GrappleSurface2D _)
        {
            if (frame.State != MechanicalClawState.Released || publicationFailure != null) return;
            try
            {
                releases.Add(frame);
                MechanicalClawTargetSnapshot snapshot = resolver.SnapshotLocked(fixture.Player,
                    fixture.Anchor, arrival.AnchorPoint, arrival.SurfaceNormal);
                TestAssert.True(snapshot.Exists && snapshot.HasLineOfSight,
                    "Swing side-column release occurs with clear original LOS, not a rope-occluding pillar");
                GD.Print($"STABLE_CLAW_SWING_COLUMN_RELEASE same_target={sameTargetBody} " +
                    $"physics={Engine.GetPhysicsFrames()} position={fixture.Player.GlobalPosition} " +
                    $"velocity={fixture.Player.Velocity} state={frame.State} failure={frame.Failure} " +
                    $"release={frame.ReleaseKind} los={snapshot.HasLineOfSight} cast={frame.CastId}");
            }
            catch (Exception exception) { publicationFailure ??= exception; }
        }
    }

    public static async Task RunSwingTargetLossRegression(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        await SwingTargetLossMomentum(tree, notifyBeforeUnload: true);
        await SwingTargetLossMomentum(tree, notifyBeforeUnload: false);
    }

    // The saved actor and existing synthetic ceiling fixture own every movement.
    // Unlike the Reel-loss tests below, this waits through 120 real neutral Swing
    // ticks so stale free-motion velocity cannot hide behind the arrival handoff.
    private static async Task SwingTargetLossMomentum(SceneTree tree, bool notifyBeforeUnload)
    {
        var fixture = new Fixture(tree, new Vector2(400f, 400f),
            new Vector2(400f, 180f), new Vector2(260f, 20f), 436f);
        StableClawPostMotorObserver observer = null;
        Exception publicationFailure = null;
        MechanicalClawFrame previousPublication = MechanicalClawFrame.Idle();
        MechanicalClawState lastExitState = MechanicalClawState.Idle;
        MechanicalClawFrame? arrival = null;
        ulong arrivalTick = 0, lastTick = 0, lastSequence = 0, requestTick = 0, lossTick = 0;
        ulong playerId = 0, targetId = 0;
        int samples = 0, afterExitSamples = 0, eventStart = 0, lossCount = 0;
        int initialJumps = 0, initialDashes = 0;
        bool requested = false, observedExit = false, complete = false, subscribed = false;
        Vector2 previousPostVelocity = Vector2.Zero, beforeRequest = Vector2.Zero;
        Vector2 lossPublishedVelocity = Vector2.Zero;
        PlayerInputReader reader = null;
        try
        {
            await fixture.Start();
            playerId = fixture.Player.GetInstanceId();
            targetId = fixture.Anchor.GetInstanceId();
            initialJumps = fixture.Motor.AcceptedJumpCount;
            initialDashes = fixture.Motor.AcceptedDashCount;
            reader = fixture.Player.GetNode<PlayerInputReader>("Input");
            fixture.Claw.ClawFrameChanged += ObservePublication;
            subscribed = true;
            observer = new StableClawPostMotorObserver
            {
                Name = "SwingLossPostMotorObserver", ProcessPhysicsPriority = 100, Observe = ObservePostMotor
            };
            TestAssert.True(observer.ProcessPhysicsPriority > fixture.Motor.ProcessPhysicsPriority,
                "Swing target-loss observer executes after the unmodified saved motor");
            fixture.Root.AddChild(observer);
            await fixture.CastAt(Key.W);
            await fixture.Until(() => arrival.HasValue || HasCallbackFailure(), 180,
                "Swing target-loss fixture reaches genuine Reel-to-Arrived-Swing");
            ThrowCallbackFailure();
            await fixture.Until(() => complete || HasCallbackFailure(), 160,
                "Swing target-loss fixture observes 120 actual neutral ticks and the real exit");
            ThrowCallbackFailure();
            TestAssert.True(complete && samples == 120 && afterExitSamples == 12,
                "Swing target-loss regression completes its declared actual-physics observation window");
            MechanicalClawFrame[] emitted = fixture.Events.Skip(eventStart).ToArray();
            TestAssert.True(lossCount == 1 && emitted.Count(frame => frame.State == MechanicalClawState.Failed) == 1 &&
                emitted.Count(frame =>
                    frame.State == MechanicalClawState.Failed && frame.Failure == MechanicalClawFailure.TargetLost) == 1,
                "Swing target loss publishes Failed/TargetLost exactly once");
            TestAssert.True(fixture.Events.All(frame => frame.State != MechanicalClawState.Boost &&
                    frame.ReleaseKind != MechanicalClawReleaseKind.Boost && !frame.BoostConsumed),
                "Swing target loss cannot supply a boost transaction");
            TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle &&
                fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null &&
                fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() &&
                Mathf.Abs(fixture.Motor.GrappleMovementScale - 1f) < .0001f,
                "Swing target loss relinquishes its target, residency and correction without remaining attached");
            GD.Print($"STABLE_CLAW_SWING_LOSS_OK notification={notifyBeforeUnload} " +
                $"neutral_ticks={samples} exit_ticks={afterExitSamples} target_lost_count={lossCount} " +
                $"before_request={beforeRequest} after={fixture.Player.Velocity} " +
                $"health={fixture.Health.CurrentHealth} jumps={fixture.Motor.AcceptedJumpCount} " +
                $"dashes={fixture.Motor.AcceptedDashCount}");
        }
        finally
        {
            if (subscribed) fixture.Claw.ClawFrameChanged -= ObservePublication;
            if (observer != null && GodotObject.IsInstanceValid(observer)) observer.SetPhysicsProcess(false);
            await fixture.Dispose();
        }

        bool HasCallbackFailure() => publicationFailure != null || observer?.Failure != null;
        void ThrowCallbackFailure()
        {
            Exception failure = publicationFailure ?? observer?.Failure;
            if (failure != null)
                throw new InvalidOperationException("Swing target-loss physics observation failed", failure);
        }
        void ObservePublication(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (complete || HasCallbackFailure()) return;
            try
            {
                if (!arrival.HasValue && previousPublication.State == MechanicalClawState.Reel &&
                    frame.State == MechanicalClawState.Swing && frame.Arrived)
                {
                    TestAssert.True(target == fixture.Anchor && frame.LockedTargetRuntimeId == targetId &&
                        frame.CastId == previousPublication.CastId,
                        "Swing loss starts from an actual same-cast, same-target geometric arrival");
                    arrival = frame;
                    arrivalTick = Engine.GetPhysicsFrames();
                    lastTick = arrivalTick;
                    lastSequence = reader.CurrentFrameSequence;
                }
                if (requested && frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost)
                {
                    lossCount++;
                    lossTick = Engine.GetPhysicsFrames();
                    lossPublishedVelocity = fixture.Player.Velocity;
                }
                previousPublication = frame;
            }
            catch (Exception exception) { publicationFailure ??= exception; }
        }
        void ObservePostMotor()
        {
            if (!arrival.HasValue || complete || HasCallbackFailure()) return;
            ulong tick = Engine.GetPhysicsFrames();
            if (tick == arrivalTick)
            {
                previousPostVelocity = fixture.Player.Velocity;
                return;
            }
            TestAssert.True(tick == lastTick + 1 && reader.CurrentFrameSequence == lastSequence + 1,
                "Swing loss observation requires consecutive actual physics ticks and input samples");
            lastTick = tick;
            lastSequence = reader.CurrentFrameSequence;
            TestAssert.True(GodotObject.IsInstanceValid(fixture.Player) &&
                fixture.Player.GetInstanceId() == playerId,
                "Swing loss keeps the same living body throughout unloading");
            PlayerInputFrame input = reader.Current;
            TestAssert.True(fixture.HeldCount == 0 && input.MoveAxis == 0f && input.Aim.IsZeroApprox() &&
                !input.JumpPressed && !input.JumpHeld && !input.DashPressed && !input.AttackPressed &&
                !input.ThreadPressed && !input.ThreadHeld && !input.ThreadReleased &&
                !input.ClawPressed && !input.InteractPressed && !input.ShieldHeld &&
                !input.PrimaryPressed && !input.PrimaryHeld && !input.PrimaryReleased &&
                !input.AttackModePressed && !input.GrappleModePressed && input.RequestedSlot == 0,
                "Swing target loss is entirely neutral after the original W/K cast");
            TestAssert.True(fixture.Health.CurrentHealth == fixture.InitialHealth &&
                fixture.Motor.AcceptedJumpCount == initialJumps && fixture.Motor.AcceptedDashCount == initialDashes,
                "Swing target loss never grants an extra jump, dash, damage or healing");
            TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Player.GetSlideCollisionCount() == 0,
                "Swing loss velocity witness stays airborne without a collision masking its momentum");
            Vector2 actualVelocity = fixture.Player.Velocity;
            TestAssert.True(float.IsFinite(actualVelocity.X) && float.IsFinite(actualVelocity.Y),
                "Swing target-loss actual body velocity remains finite");
            if (!requested)
            {
                MechanicalClawFrame frame = fixture.Claw.CurrentFrame;
                TestAssert.True(frame.State == MechanicalClawState.Swing &&
                    frame.CastId == arrival.Value.CastId && frame.LockedTargetRuntimeId == targetId &&
                    fixture.Claw.CurrentTarget == fixture.Anchor && frame.AnchorPoint == arrival.Value.AnchorPoint &&
                    frame.DockPoint == arrival.Value.DockPoint &&
                    Mathf.Abs(frame.RopeLength - arrival.Value.RopeLength) < .001f,
                    "120 neutral Swing ticks preserve the genuine cast, target and saved geometry");
                AssertOutsideSavedFace(fixture, ceiling: true);
                samples++;
                previousPostVelocity = actualVelocity;
                if (samples < 120) return;
                // No arrival-distance assertion here: this isolates unload momentum
                // from the separately retained neutral-ceiling sag assertion.
                var collider = fixture.Player.GetNode<CollisionShape2D>("BodyCollider");
                var capsule = (CapsuleShape2D)collider.Shape;
                TestAssert.True(collider.GlobalPosition.Y + capsule.Height * .5f < 436f - 20f,
                    "Swing loss begins above the unchanged floor with native capsule clearance");
                beforeRequest = actualVelocity;
                eventStart = fixture.Events.Count;
                requested = true;
                requestTick = tick;
                GD.Print($"STABLE_CLAW_SWING_LOSS_REQUEST notification={notifyBeforeUnload} tick={tick} " +
                    $"sequence={lastSequence} neutral_ticks={samples} position={fixture.Player.GlobalPosition} " +
                    $"before={beforeRequest} correction={frame.VelocityCorrection} " +
                    $"derived_free_base={beforeRequest - frame.VelocityCorrection} cast={frame.CastId}");
                StaticBody2D targetBody = fixture.Anchor.GetParent<StaticBody2D>();
                if (notifyBeforeUnload)
                    TestAssert.True(fixture.Claw.NotifyTargetWillUnload(targetBody),
                        "production unload notification recognizes the actual Swing target subtree");
                else targetBody.QueueFree();
                return;
            }
            TestAssert.True(tick - requestTick <= 3 || observedExit,
                "Swing target disappearance reaches its real TargetLost boundary within three physical ticks");
            if (!observedExit && lossCount > 0)
            {
                TestAssert.True(notifyBeforeUnload ? tick == lossTick + 1 : tick == lossTick,
                    "Swing loss compares the very first motor tick after actual target-loss publication");
                float velocityDelta = actualVelocity.DistanceTo(previousPostVelocity);
                GD.Print($"STABLE_CLAW_SWING_LOSS_MOMENTUM notification={notifyBeforeUnload} tick={tick} " +
                    $"request_tick={requestTick} loss_tick={lossTick} before_request={beforeRequest} " +
                    $"prior_post={previousPostVelocity} loss_publication={lossPublishedVelocity} " +
                    $"after={actualVelocity} delta={velocityDelta:F6} " +
                    $"gravity_step={fixture.Motor.Tuning.Gravity / Engine.PhysicsTicksPerSecond:F6} " +
                    $"state={fixture.Claw.CurrentFrame.State}");
                // Existing loss/arrival bound: one normal gravity step (~31) and
                // air steering (~25) fit well below 90; a hidden 780 base does not.
                TestAssert.True(velocityDelta <= 90f,
                    "StableClawSwingLossMomentum: first target-loss motor tick must preserve actual Swing velocity, not revive hidden free motion");
                MechanicalClawFrame lossFrame = fixture.Claw.CurrentFrame;
                // Notification synchronously publishes Failed then Idle. A deleted
                // target instead fails in StepAttached; its nonzero cast retracts on
                // subsequent model ticks before returning to Idle.
                TestAssert.True(notifyBeforeUnload
                        ? lossFrame.State == MechanicalClawState.Idle
                        : lossFrame.State == MechanicalClawState.Failed &&
                            lossFrame.Failure == MechanicalClawFailure.TargetLost &&
                            lossFrame.CastId == arrival.Value.CastId,
                    "Swing loss first free-motion motor tick follows the notification Idle or resolver TargetLost contract");
                lastExitState = lossFrame.State;
                observedExit = true;
            }
            else if (observedExit)
            {
                MechanicalClawState current = fixture.Claw.CurrentFrame.State;
                bool expectedTransition = lastExitState switch
                {
                    MechanicalClawState.Failed => current == MechanicalClawState.Retract,
                    MechanicalClawState.Retract => current is MechanicalClawState.Retract or MechanicalClawState.Idle,
                    MechanicalClawState.Idle => current == MechanicalClawState.Idle,
                    _ => false
                };
                TestAssert.True(expectedTransition,
                    $"Swing target loss follows its normal Failed-to-Retract-to-Idle sequence; previous={lastExitState}, current={current}");
                if (current != lastExitState)
                    GD.Print($"STABLE_CLAW_SWING_LOSS_LIFECYCLE notification={notifyBeforeUnload} tick={tick} " +
                        $"exit_sample={afterExitSamples + 1} previous={lastExitState} current={current}");
                lastExitState = current;
            }
            previousPostVelocity = actualVelocity;
            if (observedExit && ++afterExitSamples == 12)
            {
                complete = true;
                observer.SetPhysicsProcess(false);
            }
        }
    }

    public static async Task Run(SceneTree tree, string phase)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        if (phase == "departure")
        {
            foreach (float station in new[] { 0f, -40f, 40f }) await Departure(tree, station);
            await Departure(tree, 0f, holdRight: true);
        }
        else if (phase == "exits")
            foreach (Key key in new[] { Key.Space, Key.K, Key.L, Key.Shift }) await Exit(tree, key);
        else if (phase == "blockers")
        {
            await Blocker(tree);
            await TargetLoss(tree);
            await TargetLossMomentum(tree, highFall: false);
            await TargetLossMomentum(tree, highFall: true);
        }
        else if (phase == "terrain")
        {
            await TerrainArrival(tree, ceiling: false);
            await TerrainArrival(tree, ceiling: true);
        }
        else if (phase == "fall")
        {
            await GroundJumpCastCanLandDuringBrakingThenLift(tree);
            await UnassistedDrop(tree, 400f, 5);
            await UnassistedDrop(tree, 800f, 4);
            await HighFallWallBrake(tree);
        }
        else throw new InvalidOperationException("Unknown stable-claw runtime phase");
    }

    // Final arrival alone also passes a visibly delayed sideways slide. Require lift
    // early in the actual approach, plus the original final arrival and support checks.
    private static async Task Departure(SceneTree tree, float station, bool holdRight = false)
    {
        var fixture = new Fixture(tree, station);
        try
        {
            await fixture.Start();
            await fixture.Cast(holdRight);
            Vector2 lockedStart = fixture.Player.Position;
            MechanicalClawFrame? pullStart = null;
            bool departed = false;
            bool earlyLiftChecked = false;
            for (int tick = 0; tick < 180 && !fixture.Claw.CurrentFrame.Arrived; tick++)
            {
                Vector2 priorVelocity = fixture.Player.Velocity;
                await fixture.Frames(1);
                if (!pullStart.HasValue && fixture.Events.Any(frame => frame.State == MechanicalClawState.Reel))
                {
                    pullStart = fixture.Events.First(frame => frame.State == MechanicalClawState.Reel);
                    GD.Print($"STABLE_CLAW_PULL_START station={station} heldD={holdRight} position={pullStart.Value.CasterWorldPosition} velocity={pullStart.Value.CasterVelocity} travelDisplacement={pullStart.Value.CasterWorldPosition - lockedStart}");
                }
                departed |= !fixture.Player.IsOnFloor() && fixture.Player.Position.Y < 356f;
                if (!earlyLiftChecked && pullStart.HasValue && fixture.Claw.CurrentFrame.State == MechanicalClawState.Reel &&
                    fixture.Player.Position.X - pullStart.Value.CasterWorldPosition.X >= 80f)
                {
                    earlyLiftChecked = true;
                    float rise = pullStart.Value.CasterWorldPosition.Y - fixture.Player.Position.Y;
                    GD.Print($"STABLE_CLAW_EARLY_LIFT station={station} heldD={holdRight} start={pullStart.Value.CasterWorldPosition} position={fixture.Player.Position} rise={rise}");
                    // A straight352right/184up route rises42px over its first80px.
                    // 24px permits a curved acceleration path but rejects a near-flat slide.
                    TestAssert.True(rise >= 24f,
                        "StableClawEarlyLift: first80px horizontal Reel approach must gain24px height instead of sliding off the platform");
                }
                if (fixture.Claw.CurrentFrame.Arrived)
                {
                    float handoffDelta = fixture.Player.Velocity.DistanceTo(priorVelocity);
                    GD.Print($"STABLE_CLAW_HANDOFF heldD={holdRight} before={priorVelocity} after={fixture.Player.Velocity} delta={handoffDelta}");
                    // At60Hz normal gravity contributes31px/s and air steering at most30.
                    // A90px/s budget allows rope response but rejects stale run-speed injection.
                    TestAssert.True(handoffDelta <= 90f,
                        "StableClawArrivalMomentum: arrival must not re-inject pre-pull running velocity");
                }
                if (tick % 15 == 0) fixture.Trace("departure", tick);
                if (fixture.Events.Any(frame => frame.State is MechanicalClawState.Failed or MechanicalClawState.Released)) break;
            }
            fixture.Trace("departure-terminal", 0);
            TestAssert.True(earlyLiftChecked, "automatic approach actually crosses the measured early-lift interval");
            TestAssert.True(departed && fixture.Claw.CurrentFrame.Arrived &&
                fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing &&
                fixture.Player.Position.X > 650f && fixture.Player.Position.Y < 260f,
                "StableClawAutomaticDeparture: K must leave the supported platform and reach higher anchor Swing without held movement");
            TestAssert.True(fixture.Events.All(frame => frame.State != MechanicalClawState.Failed &&
                frame.ReleaseKind != MechanicalClawReleaseKind.Obstructed),
                "unblocked pull cannot collide with its departure platform or spuriously fail");
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "automatic departure never grants an ordinary jump");
            TestAssert.Equal(0, fixture.Motor.AcceptedDashCount, "automatic departure never consumes a dash");
            TestAssert.Equal(fixture.InitialHealth, fixture.Health.CurrentHealth, "automatic ascent preserves health");
            TestAssert.Equal(holdRight ? 1 : 0, fixture.HeldCount,
                "arrival preserves the declared no-input or held-D variant");
            fixture.ReleaseAll();
            await fixture.Frames(12);
            TestAssert.Equal(MechanicalClawState.Swing, fixture.Claw.CurrentFrame.State,
                "arrived attachment remains in Swing until explicit release");
            GD.Print($"STABLE_CLAW_DEPARTURE_OK station={station} heldD={holdRight} actor={fixture.Player.GetInstanceId()} position={fixture.Player.Position} receipts={fixture.Receipts}");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task GroundJumpCastCanLandDuringBrakingThenLift(SceneTree tree)
    {
        // Grip1/Grip2 relative height and distance, without the upper ledge or old
        // pillar: isolate contact with the original floor during bounded braking.
        var fixture = new Fixture(tree, new Vector2(400f, 400f),
            new Vector2(716f, -14f), new Vector2(36f, 60f), 436f);
        try
        {
            await fixture.Start();
            int jumps = fixture.Motor.AcceptedJumpCount, dashes = fixture.Motor.AcceptedDashCount;
            ulong nextCast = fixture.Claw.CurrentFrame.NextCastId;
            TestAssert.True(fixture.Player.IsOnFloor() && fixture.Player.Velocity.Length() <= 1f,
                "floor-brake fixture starts supported and still before its sole Space");
            fixture.Key(Key.Space, true);
            await fixture.Until(() => fixture.Motor.AcceptedJumpCount == jumps + 1 &&
                !fixture.Player.IsOnFloor() && fixture.Player.Velocity.Y >= -100f && fixture.Player.Velocity.Y < 0f,
                30, "the real held ground jump reaches its natural apex window");
            TestAssert.True(fixture.Player.Velocity.Y >= -fixture.Motor.Tuning.JumpCutSpeed,
                "the actual apex handoff cannot trigger jump cut");
            fixture.Key(Key.Space, false);
            await fixture.Cast();
            bool landedWhilePulling = false;
            for (int sample = 0; sample < 150 && !fixture.Claw.CurrentFrame.Arrived; sample++)
            {
                await fixture.Frames(1);
                MechanicalClawFrame frame = fixture.Claw.CurrentFrame;
                if (fixture.Player.IsOnFloor() && frame.State == MechanicalClawState.Reel)
                {
                    for (int index = 0; index < fixture.Player.GetSlideCollisionCount(); index++)
                    {
                        using var contact = fixture.Player.GetSlideCollision(index);
                        landedWhilePulling |= (contact.GetCollider() as Node)?.Name == "TerrainFloor" &&
                            contact.GetNormal().Dot(Vector2.Up) > .99f;
                    }
                    fixture.Trace("floor-brake-native-landing", sample);
                }
                if (sample % 8 == 0) fixture.Trace("floor-brake", sample);
                if (fixture.Events.Any(value => value.State is MechanicalClawState.Released or MechanicalClawState.Failed))
                    break;
            }
            TestAssert.True(landedWhilePulling && fixture.Claw.CurrentFrame.Arrived &&
                fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing &&
                fixture.Events.All(frame => frame.State is not (MechanicalClawState.Released or MechanicalClawState.Failed)),
                "StableClawFloorBrake: real original-floor landing during downward braking retains the same pull and then reaches Swing");
            TestAssert.Equal(jumps + 1, fixture.Motor.AcceptedJumpCount,
                "floor braking never adds a second ordinary jump");
            TestAssert.Equal(dashes, fixture.Motor.AcceptedDashCount, "floor braking adds no dash");
            TestAssert.Equal(nextCast + 1UL, fixture.Claw.CurrentFrame.NextCastId,
                "floor braking reaches arrival through the one original cast");
            TestAssert.True(fixture.Events.All(frame => !frame.BoostConsumed &&
                frame.ReleaseKind != MechanicalClawReleaseKind.Boost && frame.State != MechanicalClawState.Boost),
                "no attached boost substitutes for the ordinary ground jump");
            TestAssert.Equal(fixture.InitialHealth, fixture.Health.CurrentHealth,
                "the small original jump and floor landing preserve health");
            GD.Print($"STABLE_CLAW_FLOOR_BRAKE_OK actual_floor_contact={landedWhilePulling} " +
                $"jumps={jumps}->{fixture.Motor.AcceptedJumpCount} casts={nextCast}->{fixture.Claw.CurrentFrame.NextCastId} " +
                $"position={fixture.Player.Position} dock={fixture.Claw.CurrentFrame.DockPoint}");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task Exit(SceneTree tree, Key exit)
    {
        var fixture = new Fixture(tree, 0f);
        try
        {
            await fixture.Start(); await fixture.Cast();
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Reel,
                70, "attachment reaches real Reel before input exit");
            int eventStart = fixture.Events.Count;
            fixture.Key(exit, true);
            if (exit == Key.Space) { fixture.Key(Key.K, true); fixture.Key(Key.J, true); }
            if (exit == Key.L) fixture.Key(Key.Space, true);
            await fixture.Frames(4);
            fixture.ReleaseAll();
            MechanicalClawFrame[] emitted = fixture.Events.Skip(eventStart).ToArray();
            TestAssert.Equal(exit == Key.Space ? 1 : 0, emitted.Count(frame => frame.State == MechanicalClawState.Boost),
                "attached Space has one boost; guard, fresh K and dash never boost");
            TestAssert.Equal(1, emitted.Count(frame => frame.State == MechanicalClawState.Released),
                "exit releases exactly once");
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount,
                "attached Space including guard combination cannot become double jump");
            TestAssert.Equal(exit == Key.Shift ? 1 : 0, fixture.Motor.AcceptedDashCount,
                "only requested Shift uses the original dash");
            TestAssert.Equal(0, fixture.Attacks, "Space wins over simultaneous J and K during attachment");
            TestAssert.True(exit == Key.Space || emitted.Any(frame => frame.State == MechanicalClawState.Released &&
                frame.ReleaseKind == MechanicalClawReleaseKind.Cancel), "ordinary exit preserves Cancel reason");
            if (exit == Key.L) TestAssert.True(fixture.ShieldObserved, "L actually engages the shield");
            await fixture.Frames(18);
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "release edges never add a delayed jump");
            TestAssert.Equal(1, fixture.Events.Skip(eventStart).Count(frame => frame.State == MechanicalClawState.Released),
                "release does not duplicate after keys are lifted");
            GD.Print($"STABLE_CLAW_EXIT_OK key={exit} position={fixture.Player.Position} health={fixture.Health.CurrentHealth}");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task Blocker(SceneTree tree)
    {
        var fixture = new Fixture(tree, 0f);
        try
        {
            await fixture.Start(); await fixture.Cast();
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Reel,
                70, "live attachment before a real corridor closes");
            float wallX = fixture.Player.Position.X + 70f;
            fixture.Root.AddChild(Body("ClosingWall", new Vector2(wallX, 220f), new Vector2(20f, 560f)));
            await fixture.Until(() => fixture.Events.Any(frame => frame.State == MechanicalClawState.Released ||
                frame.State == MechanicalClawState.Failed), 80, "real wall interrupts active pull");
            TestAssert.True(fixture.Player.Position.X < wallX - 9f,
                "automatic pull never teleports the actor through a real collider");
            TestAssert.True(!fixture.Claw.CurrentFrame.Arrived && fixture.Events.All(frame => !frame.Arrived),
                "closing-wall interruption cannot masquerade as successful arrival");
            TestAssert.True(fixture.Events.Any(frame => frame.State == MechanicalClawState.Released &&
                frame.ReleaseKind == MechanicalClawReleaseKind.Obstructed &&
                frame.Failure == MechanicalClawFailure.Obstructed),
                "real closing wall preserves the explicit Obstructed release reason");
            fixture.Trace("blocker", 0);
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task TargetLoss(SceneTree tree)
    {
        var fixture = new Fixture(tree, 0f);
        try
        {
            await fixture.Start(); await fixture.Cast();
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Reel,
                70, "live attachment before actual target deletion");
            fixture.Anchor.QueueFree();
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle,
                80, "deleted target cleanly releases and retracts");
            TestAssert.True(fixture.Events.Any(frame => frame.State == MechanicalClawState.Failed &&
                frame.Failure == MechanicalClawFailure.TargetLost),
                "actual deleted target produces the explicit TargetLost failure");
            TestAssert.True(fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null,
                "deleted target cannot leave a retained live attachment");
            TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "target loss grants no extra jump");
            GD.Print("STABLE_CLAW_TARGET_LOSS_OK");
        }
        finally { await fixture.Dispose(); }
    }

    // Dropping an automatic-pull correction must retain the observed body's
    // velocity, not resurrect pre-pull running or terminal-fall base velocity.
    private static async Task TargetLossMomentum(SceneTree tree, bool highFall)
    {
        var fixture = highFall
            ? new Fixture(tree, new Vector2(400f, 0f), new Vector2(820f, 2900f),
                new Vector2(40f, 700f), null)
            : new Fixture(tree, 0f);
        try
        {
            await fixture.Start(supported: !highFall);
            if (highFall)
            {
                await fixture.Frames(239);
                TestAssert.True(fixture.Player.Position.Y > 2600f && Math.Abs(fixture.Player.Velocity.Y - 780f) < .1f,
                    "unload regression begins with a genuine four-second terminal-speed fall");
                await fixture.CastAt(Key.D);
            }
            else await fixture.Cast(holdRight: true);
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Reel &&
                !fixture.Claw.CurrentFrame.Arrived && fixture.Player.Velocity.Length() < 90f &&
                fixture.Player.Position.DistanceTo(fixture.Claw.CurrentFrame.DockPoint) < 20f,
                220, "real pull brakes near its dock before target loss and before Swing");
            TestAssert.Equal(highFall ? 0 : 1, fixture.HeldCount,
                "target loss samples declared no-input fall or continuous-D approach");
            Vector2 before = fixture.Player.Velocity;
            int firstLossEvent = fixture.Events.Count;
            if (highFall)
                TestAssert.True(fixture.Claw.NotifyTargetWillUnload(fixture.Anchor.GetParent()),
                    "real production sector-unload notification recognizes the currently attached wall subtree");
            else fixture.Anchor.QueueFree();
            bool observedExit = false;
            for (int tick = 0; tick < 30 && !observedExit; tick++)
            {
                Vector2 previousVelocity = fixture.Player.Velocity;
                await fixture.Frames(1);
                observedExit = fixture.Claw.CurrentFrame.State != MechanicalClawState.Reel;
                if (!observedExit) continue;
                float delta = fixture.Player.Velocity.DistanceTo(previousVelocity);
                GD.Print($"STABLE_CLAW_LOSS_MOMENTUM highFall={highFall} beforeRequest={before} priorPhysics={previousVelocity} actual={fixture.Player.Velocity} delta={delta} state={fixture.Claw.CurrentFrame.State}");
                TestAssert.True(delta <= 90f,
                    "StableClawLossMomentum: actual target-loss exit must preserve observed velocity without reviving pre-pull motion");
            }
            TestAssert.True(observedExit, "real target disappearance reaches a terminal state promptly");
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle,
                50, "target loss completes its ordinary clean Idle transition");
            MechanicalClawFrame[] lossEvents = fixture.Events.Skip(firstLossEvent).ToArray();
            TestAssert.True(lossEvents.Any(frame => frame.State == MechanicalClawState.Failed &&
                frame.Failure == MechanicalClawFailure.TargetLost), "actual lost target publishes TargetLost");
            TestAssert.True(lossEvents.All(frame => !frame.Arrived && frame.State != MechanicalClawState.Boost &&
                frame.ReleaseKind != MechanicalClawReleaseKind.Boost),
                "target loss cannot report a successful arrival or grant a boost");
            TestAssert.True(fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null,
                "both target-loss routes relinquish their attachment and residency");
            TestAssert.True(fixture.Motor.AcceptedJumpCount == 0 && fixture.Motor.AcceptedDashCount == 0 &&
                fixture.Health.CurrentHealth == 5, "target loss adds no jump, dash, damage or healing");
            GD.Print($"STABLE_CLAW_LOSS_MOMENTUM_OK highFall={highFall}");
        }
        finally { await fixture.Dispose(); }
    }

    // Actual wall/ceiling shapes exercise native first-hit normals and solid boundaries;
    // no model/frame injection, hidden motor freeze, or post-mount position repair.
    private static async Task TerrainArrival(SceneTree tree, bool ceiling)
    {
        var fixture = new Fixture(tree, new Vector2(400f, 400f),
            ceiling ? new Vector2(400f, 180f) : new Vector2(752f, 280f),
            ceiling ? new Vector2(260f, 20f) : new Vector2(40f, 560f), 436f);
        StableClawPostMotorObserver diagnosticObserver = null;
        Exception diagnosticFailure = null;
        bool diagnosticActive = false, diagnosticSubscribed = false, hasPreviousPost = false;
        int driverSample = -1, diagnosticRecords = 0;
        ulong previousPostTick = 0;
        Vector2 previousPostPosition = Vector2.Zero, previousPostVelocity = Vector2.Zero;
        MechanicalClawFrame previousPublication = MechanicalClawFrame.Idle();
        var diagnosticResolver = new MechanicalClawTargetResolver();
        try
        {
            await fixture.Start();
            fixture.Claw.ClawFrameChanged += ObserveTerrainPublication;
            diagnosticSubscribed = true;
            diagnosticObserver = new StableClawPostMotorObserver
            {
                Name = "TerrainSwingPostMotorDiagnostic", ProcessPhysicsPriority = 100,
                Observe = () =>
                {
                    if (!diagnosticActive) return;
                    TraceTerrain("actual-post-motor", driverSample);
                    previousPostTick = Engine.GetPhysicsFrames();
                    previousPostPosition = fixture.Player.GlobalPosition;
                    previousPostVelocity = fixture.Player.Velocity;
                    hasPreviousPost = true;
                }
            };
            fixture.Root.AddChild(diagnosticObserver);
            await fixture.CastAt(ceiling ? Key.W : Key.D);
            Vector2 hit = fixture.Claw.CurrentFrame.AnchorPoint;
            Vector2 normal = fixture.Claw.CurrentFrame.SurfaceNormal;
            TestAssert.True(ceiling ? Math.Abs(hit.Y - 190f) < .1f && normal.Y > .99f :
                Math.Abs(hit.X - 732f) < .1f && normal.X < -.99f,
                "current keyboard locks the independently known solid first-hit face and outward normal");
            await fixture.Until(() => fixture.Claw.CurrentFrame.State == MechanicalClawState.Reel,
                70, "real solid target begins Reel");
            if (ceiling)
            {
                Vector2 beforePause = fixture.Player.Position;
                MechanicalClawFrame pausedClaw = fixture.Claw.CurrentFrame;
                ulong inputSequence = fixture.Player.GetNode<PlayerInputReader>("Input").CurrentFrameSequence;
                tree.Paused = true;
                for (int tick = 0; tick < 20; tick++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(fixture.Player.Position == beforePause && fixture.Claw.CurrentFrame == pausedClaw &&
                    fixture.Player.GetNode<PlayerInputReader>("Input").CurrentFrameSequence == inputSequence,
                    "real paused tree freezes an active pull, actor and native input sequence");
                tree.Paused = false;
            }
            for (int tick = 0; tick < 180 && !fixture.Claw.CurrentFrame.Arrived; tick++)
            {
                Vector2 previousVelocity = fixture.Player.Velocity;
                await fixture.Frames(1);
                AssertOutsideSavedFace(fixture, ceiling);
                TestAssert.True(fixture.Claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing,
                    "clear solid-face approach stays attached through arrival");
                if (fixture.Claw.CurrentFrame.Arrived)
                    TestAssert.True(fixture.Player.Velocity.DistanceTo(previousVelocity) <= 90f,
                        "solid-face first Swing retains continuous arrival momentum");
                ThrowTerrainDiagnosticFailure();
            }
            TestAssert.True(fixture.Claw.CurrentFrame.Arrived && fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing &&
                fixture.Player.Position.DistanceTo(fixture.Claw.CurrentFrame.DockPoint) <= 16f,
                "native wall/ceiling approach reaches its safe dock with the actual body");
            ulong castId = fixture.Claw.CurrentFrame.CastId;
            TraceTerrain("driver-arrival", -1);
            if (ceiling) fixture.Key(Key.D, true);
            for (int tick = 0; tick < 24; tick++)
            {
                driverSample = tick;
                TraceTerrain("driver-before", tick);
                await fixture.Frames(1);
                TraceTerrain("driver-after", tick);
                AssertOutsideSavedFace(fixture, ceiling);
                TestAssert.True(fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing &&
                    fixture.Claw.CurrentFrame.CastId == castId && fixture.Claw.CurrentTarget == fixture.Anchor,
                    "arrived Swing survives real lateral input without changing target or ending the cast");
                TestAssert.True(float.IsFinite(fixture.Player.Velocity.X) && float.IsFinite(fixture.Player.Velocity.Y) &&
                    fixture.Player.Velocity.Length() <= 900.01f, "native Swing remains finite and speed bounded");
                ThrowTerrainDiagnosticFailure();
            }
            diagnosticActive = false;
            fixture.ReleaseAll();
            await AssertSingleSpaceRelease(fixture);
            TestAssert.Equal(5, fixture.Health.CurrentHealth, "solid-face traversal preserves health");
            GD.Print($"STABLE_CLAW_TERRAIN_OK ceiling={ceiling} hit={hit} normal={normal} position={fixture.Player.Position} pausedDuringReel={ceiling}");
        }
        finally
        {
            diagnosticActive = false;
            if (diagnosticSubscribed) fixture.Claw.ClawFrameChanged -= ObserveTerrainPublication;
            if (diagnosticObserver != null && GodotObject.IsInstanceValid(diagnosticObserver))
                diagnosticObserver.SetPhysicsProcess(false);
            if (diagnosticFailure != null || diagnosticObserver?.Failure != null)
                GD.Print($"TERRAIN_SWING_DIAGNOSTIC_ERROR ceiling={ceiling} " +
                    $"error={diagnosticFailure ?? diagnosticObserver.Failure}");
            await fixture.Dispose();
        }

        void ObserveTerrainPublication(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            try
            {
                if (previousPublication.State == MechanicalClawState.Reel &&
                    frame.State == MechanicalClawState.Swing && frame.Arrived)
                    diagnosticActive = true;
                if (diagnosticActive)
                    TraceTerrain($"publication-from-{previousPublication.State}", driverSample, frame,
                        target?.GetInstanceId() ?? 0UL);
                previousPublication = frame;
            }
            catch (Exception exception) { diagnosticFailure ??= exception; }
        }
        void ThrowTerrainDiagnosticFailure()
        {
            Exception failure = diagnosticFailure ?? diagnosticObserver?.Failure;
            if (failure != null) throw new InvalidOperationException("Terrain Swing diagnostic failed", failure);
        }
        void TraceTerrain(string phase, int sample, MechanicalClawFrame? published = null,
            ulong publicationTarget = 0UL)
        {
            try
            {
                if (diagnosticRecords++ >= 256)
                    throw new InvalidOperationException("Terrain Swing diagnostic exceeded its bounded record count");
                MechanicalClawFrame frame = published ?? fixture.Claw.CurrentFrame;
                var contacts = new List<string>();
                for (int index = 0; index < fixture.Player.GetSlideCollisionCount(); index++)
                {
                    using KinematicCollision2D contact = fixture.Player.GetSlideCollision(index);
                    Node collider = contact.GetCollider() as Node;
                    contacts.Add($"id={collider?.GetInstanceId() ?? 0UL},path={collider?.GetPath().ToString() ?? "none"}," +
                        $"original_target={collider != null && MechanicalClawTargetResolver.ColliderBelongsToSurface(collider, fixture.Anchor)}," +
                        $"normal={contact.GetNormal()}");
                }
                string visibility = "not_sampled";
                if (frame.Failure != MechanicalClawFailure.None ||
                    frame.State is MechanicalClawState.Released or MechanicalClawState.Failed)
                {
                    MechanicalClawTargetSnapshot snapshot = diagnosticResolver.SnapshotLocked(fixture.Player,
                        fixture.Anchor, frame.AnchorPoint, frame.SurfaceNormal);
                    visibility = $"exists={snapshot.Exists},los={snapshot.HasLineOfSight},target={snapshot.RuntimeId}";
                }
                GD.Print($"TERRAIN_SWING_TRACE ceiling={ceiling} phase={phase} driver_sample={sample} " +
                    $"physics={Engine.GetPhysicsFrames()} input_sequence={fixture.Player.GetNode<PlayerInputReader>("Input").CurrentFrameSequence} " +
                    $"actual_position={fixture.Player.GlobalPosition} actual_velocity={fixture.Player.Velocity} " +
                    $"has_previous_post={hasPreviousPost} previous_post_tick={previousPostTick} " +
                    $"previous_post_position={previousPostPosition} previous_post_velocity={previousPostVelocity} " +
                    $"frame_position={frame.CasterWorldPosition} frame_velocity={frame.CasterVelocity} " +
                    $"state={frame.State} release={frame.ReleaseKind} failure={frame.Failure} arrived={frame.Arrived} " +
                    $"cast={frame.CastId} locked_target={frame.LockedTargetRuntimeId} publication_target={publicationTarget} " +
                    $"correction={frame.VelocityCorrection} dock={frame.DockPoint} anchor={frame.AnchorPoint} " +
                    $"normal={frame.SurfaceNormal} rope={frame.RopeLength:F6} " +
                    $"sampled_input={fixture.Player.GetNode<PlayerInputReader>("Input").Current} " +
                    $"actual_slides=[{string.Join(';', contacts)}] original_target_visibility=[{visibility}] " +
                    "prediction_recomputed=false");
            }
            catch (Exception exception) { diagnosticFailure ??= exception; }
        }
    }

    private static void AssertOutsideSavedFace(Fixture fixture, bool ceiling) => TestAssert.True(
        ceiling ? fixture.Player.Position.Y - 36f >= 189.9f : fixture.Player.Position.X + 20f <= 732.1f,
        "full original40x72 capsule remains outside the solid target face");

    private static async Task HighFallWallBrake(SceneTree tree)
    {
        var fixture = new Fixture(tree, new Vector2(400f, 0f), new Vector2(820f, 2900f),
            new Vector2(40f, 700f), null);
        try
        {
            await fixture.Start(supported: false);
            await fixture.Frames(239);
            TestAssert.True(fixture.Player.Position.Y > 2600f && Math.Abs(fixture.Player.Velocity.Y - 780f) < .1f,
                "high-fall claw begins after four real seconds at original780px/s terminal fall speed");
            await fixture.CastAt(Key.D);
            Vector2 hit = fixture.Claw.CurrentFrame.AnchorPoint;
            Vector2 normal = fixture.Claw.CurrentFrame.SurfaceNormal;
            TestAssert.True(Math.Abs(hit.X - 800f) < .1f && hit.Y >= 2550f && hit.Y <= 3250f && normal.X < -.99f,
                "falling current-D/K cast locks actual wall first-hit face rather than a floating point");
            var downward = new List<float>();
            Vector2 previousVelocity = fixture.Player.Velocity;
            for (int tick = 0; tick < 220 && !fixture.Claw.CurrentFrame.Arrived; tick++)
            {
                await fixture.Frames(1);
                MechanicalClawFrame frame = fixture.Claw.CurrentFrame;
                TestAssert.True(frame.State is not (MechanicalClawState.Failed or MechanicalClawState.Released or MechanicalClawState.Idle),
                    "visible wall remains available throughout genuine fall braking");
                if (frame.State == MechanicalClawState.Reel)
                {
                    downward.Add(fixture.Player.Velocity.Y);
                    // Existing max-pull acceleration2400 plus gravity1850 over1/60s.
                    TestAssert.True(fixture.Player.Velocity.DistanceTo(previousVelocity) <= 71.5f,
                        "fall braking changes velocity continuously within pull plus gravity budget");
                }
                previousVelocity = fixture.Player.Velocity;
                TestAssert.True(fixture.Player.Position.X + 20f <= 800.1f,
                    "fall arrest keeps the whole capsule outside the wall");
            }
            bool multiFrameBrake = false;
            for (int index = 2; index < downward.Count; index++)
                multiFrameBrake |= downward[index - 2] > downward[index - 1] &&
                    downward[index - 1] > downward[index] && downward[index] > 0f;
            TestAssert.True(multiFrameBrake && fixture.Claw.CurrentFrame.Arrived &&
                fixture.Claw.CurrentFrame.State == MechanicalClawState.Swing,
                "current-key high-fall pull brakes over multiple real frames then reaches safe Swing");
            TestAssert.True(fixture.Player.Position.DistanceTo(fixture.Claw.CurrentFrame.DockPoint) <= 16f,
                "actual falling body reaches wall dock rather than a fabricated arrived flag");
            await AssertSingleSpaceRelease(fixture);
            TestAssert.Equal(5, fixture.Health.CurrentHealth, "unlanded wall arrest cannot injure or heal the player");
            GD.Print($"STABLE_CLAW_HIGH_FALL_OK hit={hit} position={fixture.Player.Position} brakeSamples={downward.Count} landingClaim=false");
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task AssertSingleSpaceRelease(Fixture fixture)
    {
        int start = fixture.Events.Count;
        fixture.Key(Key.Space, true); await fixture.Frames(3); fixture.Key(Key.Space, false);
        MechanicalClawFrame[] released = fixture.Events.Skip(start)
            .Where(frame => frame.State is MechanicalClawState.Boost or MechanicalClawState.Released).ToArray();
        TestAssert.True(released.Length == 2 && released[0].State == MechanicalClawState.Boost &&
            released[1].State == MechanicalClawState.Released && released[0].CastId == released[1].CastId &&
            released[1].ReleaseKind == MechanicalClawReleaseKind.Boost,
            "current Space produces exactly one ordered same-cast Boost then Released transaction");
        TestAssert.True(fixture.Player.Velocity.Length() <= 900.01f, "released actual motor velocity remains capped900");
        TestAssert.Equal(0, fixture.Motor.AcceptedJumpCount, "attached release never grants ordinary or second jump");
        TestAssert.Equal(0, fixture.Motor.AcceptedDashCount, "attached release never grants unrequested dash");
    }

    private static async Task UnassistedDrop(SceneTree tree, float height, int expectedHealth)
    {
        var root = new Node2D { Name = "StableClawUnassistedFall" };
        root.AddChild(Body("HardFloor", new Vector2(400f, 1010f), new Vector2(1200f, 20f)));
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(400f, 964f - height);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var health = player.GetNode<HealthComponent>("Health");
        var motor = player.GetNode<PlayerMotor>("Motor");
        int injuries = 0;
        health.Damaged += (_, _) => injuries++;
        tree.Root.AddChild(root);
        try
        {
            for (int tick = 0; tick < 220 && !player.IsOnFloor(); tick++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(player.Velocity.Y <= 780.1f, "ordinary fall keeps the original speed cap");
            }
            TestAssert.True(player.IsOnFloor() && Math.Abs(player.Position.Y - 964f) < 1f,
                "unassisted drop reaches the actual unchanged hard floor");
            for (int tick = 0; tick < 30; tick++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.Equal(expectedHealth, health.CurrentHealth, "ordinary height-based fall damage remains unchanged");
            TestAssert.Equal(expectedHealth == 5 ? 0 : 1, injuries, "one landing never duplicates fall injury");
            TestAssert.True(motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0,
                "unassisted falling adds no input action");
            GD.Print($"STABLE_CLAW_UNASSISTED_FALL_OK height={height} health={health.CurrentHealth} injuries={injuries}");
        }
        finally { await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root); }
    }

    private static StaticBody2D Body(string name, Vector2 position, Vector2 size)
    {
        var body = new StaticBody2D { Name = name, Position = position,
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        return body;
    }

    private sealed class Fixture
    {
        internal readonly SceneTree Tree;
        internal readonly Node2D Root = new() { Name = "StableClawSyntheticWorld" };
        internal readonly CharacterBody2D Player;
        internal readonly GrappleSurface2D Anchor;
        internal readonly List<MechanicalClawFrame> Events = new();
        private readonly HashSet<Key> _held = new();
        internal MechanicalClawController Claw => Player.GetNode<MechanicalClawController>("MechanicalClaw");
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal HealthComponent Health => Player.GetNode<HealthComponent>("Health");
        internal int HeldCount => _held.Count;
        internal int InitialHealth, Receipts, Attacks;
        internal bool ShieldObserved;

        internal Fixture(SceneTree tree, float station)
        {
            Tree = tree;
            // Basin lift equivalent: full 120px support, starts36px above deck;
            // higher anchor352px right and184px up from the center station.
            Root.AddChild(Body("DeparturePlatform", new Vector2(400f, 446f), new Vector2(120f, 20f)));
            Root.AddChild(Body("RecoveryFloor", new Vector2(500f, 650f), new Vector2(1400f, 20f)));
            Anchor = new GrappleSurface2D { Name = "PointAnchor", Position = new Vector2(752f, 216f),
                AssistRadius = 24f, Material = GrappleSurfaceMaterial.DedicatedAnchor };
            Root.AddChild(Anchor);
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new Vector2(400f + station, 400f);
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Root.AddChild(Player);
        }
        internal Fixture(SceneTree tree, Vector2 spawn, Vector2 targetPosition, Vector2 targetSize, float? floorTop)
        {
            Tree = tree;
            if (floorTop.HasValue)
                Root.AddChild(Body("TerrainFloor", new Vector2(500f, floorTop.Value + 10f), new Vector2(1600f, 20f)));
            StaticBody2D target = Body("SolidGripTerrain", targetPosition, targetSize);
            Anchor = new GrappleSurface2D { Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.GripStone,
                UseCollisionHitPoint = true };
            target.AddChild(Anchor); Root.AddChild(target);
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = spawn;
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Root.AddChild(Player);
        }
        internal async Task Start(bool supported = true)
        {
            Tree.Root.AddChild(Root);
            Claw.ClawFrameChanged += (frame, _) => Events.Add(frame);
            Player.GetNode<PlayerCombat>("Combat").AttackStarted += (_, _) => Attacks++;
            await Frames(supported ? 8 : 1);
            TestAssert.True(!supported || Player.IsOnFloor() && Math.Abs(Player.Position.Y - 400f) < 1f,
                "saved actor begins on the actual small platform");
            InitialHealth = Health.CurrentHealth;
        }
        internal async Task Cast(bool holdRight = false)
        {
            Key(Godot.Key.W, true); Key(Godot.Key.D, true); await Frames(1);
            TestAssert.True(Claw.KeyboardPreview.Surface == Anchor && Claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "native W+D preview selects the real visible higher point target");
            Key(Godot.Key.K, true); await Frames(1);
            Key(Godot.Key.K, false); Key(Godot.Key.W, false);
            if (!holdRight) Key(Godot.Key.D, false);
            await Until(() => Claw.CurrentFrame.LockedTargetRuntimeId != 0, 35, "fresh K locks preview target");
            TestAssert.Equal(Anchor.GetInstanceId(), Claw.CurrentFrame.LockedTargetRuntimeId,
                "actual keyboard cast retains the exact previewed anchor");
            TestAssert.True(Player.GetNode<PlayerInputReader>("Input").Current.KeyboardActions,
                "fixture uses current keyboard mode");
        }
        internal async Task CastAt(Key direction)
        {
            Key(direction, true); await Frames(1);
            TestAssert.True(Claw.KeyboardPreview.Surface == Anchor && Claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "native directional preview selects the actual grippable solid face");
            Key(Godot.Key.K, true); await Frames(1); ReleaseAll();
            await Until(() => Claw.CurrentFrame.LockedTargetRuntimeId != 0, 35, "native K locks the shown terrain face");
            TestAssert.Equal(Anchor.GetInstanceId(), Claw.CurrentFrame.LockedTargetRuntimeId,
                "current keyboard terrain cast retains the exact shown target");
            TestAssert.True(Player.GetNode<PlayerInputReader>("Input").Current.KeyboardActions,
                "terrain approach uses current keyboard mode");
        }
        internal void Key(Key key, bool pressed)
        {
            if (_held.Contains(key) == pressed) return;
            if (pressed) _held.Add(key); else _held.Remove(key);
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
            Input.FlushBufferedEvents(); Receipts++;
            TestAssert.True(Input.IsPhysicalKeyPressed(key) == pressed, $"native physical receipt {key}={pressed}");
        }
        internal void ReleaseAll() { foreach (Key key in _held.ToArray()) Key(key, false); }
        internal async Task Frames(int count)
        {
            for (int frame = 0; frame < count; frame++)
            {
                await Tree.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
                await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
                ShieldObserved |= Player.GetNode<PlayerShieldController>("Shield").IsActive;
            }
        }
        internal async Task Until(Func<bool> condition, int cap, string reason)
        {
            for (int tick = 0; tick < cap && !condition(); tick++) await Frames(1);
            TestAssert.True(condition(), $"{reason}; position={Player.Position} state={Claw.CurrentFrame.State}");
        }
        internal void Trace(string tag, int tick) => GD.Print($"STABLE_CLAW_TRACE tag={tag} tick={tick} state={Claw.CurrentFrame.State} release={Claw.CurrentFrame.ReleaseKind} failure={Claw.CurrentFrame.Failure} arrived={Claw.CurrentFrame.Arrived} position={Player.Position} velocity={Player.Velocity} dock={Claw.CurrentFrame.DockPoint}");
        internal async Task Dispose()
        {
            ReleaseAll(); Tree.Paused = false;
            if (Root.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(Tree, Root); else Root.Free();
        }
    }
}

// Test-only synchronous observation after the saved PlayerMotor (priority 0).
internal sealed partial class StableClawPostMotorObserver : Node
{
    internal Action Observe;
    internal Exception Failure { get; private set; }

    public override void _PhysicsProcess(double delta)
    {
        if (Failure != null) return;
        try { Observe?.Invoke(); }
        catch (Exception exception) { Failure = exception; SetPhysicsProcess(false); }
    }
}
