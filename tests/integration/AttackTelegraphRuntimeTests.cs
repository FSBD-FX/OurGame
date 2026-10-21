using System;
using System.Collections.Generic;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class AttackTelegraphRuntimeTests
{
    public static void Run()
    {
        FactoryCopiesRealWorldCollisionBounds();
        TelegraphLeafSeparatesGoldLockedRedAndWhiteImpact();
        RustRuntimePublishesRealShapeBeforeDamageAndClearsThroughRecovery();
        MothRuntimeLocksSweptDiveAndPublishesPostMoveActiveCircle();
        LaborerRuntimeUsesAnAuthoritativeLockedFanOnBothFacings();
        BossLockedSweepDoesNotFlipWhenPlayerCrossesSides();
        BossNewLeapTelegraphDropsThePreviousAttackTarget();
        BossUsesDistinctRealShapesAndPosesForEveryAttack();
        BossHammerPathStaysLockedThroughActiveAndRecovery();
    }

    private static void FactoryCopiesRealWorldCollisionBounds()
    {
        var source = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(84f, 26f) } };
        Transform2D transform = new(0.31f, new Vector2(140f, -28f));
        AttackShapeFrame frame = AttackShapeFactory.FromCollision(source, transform, AttackShapeKind.Rectangle);
        AssertBoundsNear(CollisionBounds(source.Shape, transform), PresentationBounds(frame),
            "attack factory preserves the real rectangle world bounds");
        source.Free();
    }

    private static void TelegraphLeafSeparatesGoldLockedRedAndWhiteImpact()
    {
        var visual = new AttackTelegraphVisual();
        var shape = new AttackShapeFrame(AttackShapeKind.Circle, new Vector2(60f, 20f),
            new Vector2(22f, 22f), 0f, Array.Empty<Vector2>());
        visual.Present(new AttackPresentationFrame(
            new AttackTimelineFrame(8, EnemyAttackKind.BossLeapSlam, AttackPhase.Telegraph,
                1f, .8f, Vector2.Zero, false), shape));
        TestAssert.True(visual.GoldVisible && !visual.DangerVisible && !visual.ImpactVisible,
            "unlocked Telegraph is a gold preparation contour only");
        visual.Present(new AttackPresentationFrame(
            new AttackTimelineFrame(8, EnemyAttackKind.BossLeapSlam, AttackPhase.Telegraph,
                1f, .25f, new Vector2(60f, 20f), true), shape));
        TestAssert.True(visual.GoldVisible && visual.DangerVisible && !visual.ImpactVisible,
            "locked Telegraph adds the red true danger shape");
        visual.Present(new AttackPresentationFrame(
            new AttackTimelineFrame(8, EnemyAttackKind.BossLeapSlam, AttackPhase.Active,
                .25f, .25f, new Vector2(60f, 20f), true), shape));
        TestAssert.True(!visual.GoldVisible && !visual.DangerVisible && visual.ImpactVisible,
            "Active clears red and shows a white impact confirmation");
        visual.Present(default);
        TestAssert.True(!visual.GoldVisible && !visual.DangerVisible && !visual.ImpactVisible,
            "None clears every transient telegraph layer");
        visual.Free();
    }

    private static void RustRuntimePublishesRealShapeBeforeDamageAndClearsThroughRecovery()
    {
        RuntimeFixture<RustShellSquire> fixture = CreateFixture<RustShellSquire>(
            "res://scenes/enemies/RustShellSquire.tscn", new Vector2(45f, 0f));
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor._PhysicsProcess(.01);
        AssertWarning(fixture, published, "Rust-Shell");
        TestAssert.Equal("rust_crouch", fixture.Actor.GetNode<RustShellFormalVisual>("VisualRoot/Formal").CurrentPoseMarker,
            "Rust-Shell warning uses a crouched silhouette marker");
        fixture.Actor._PhysicsProcess(.70);
        AssertActive(fixture, published, "Rust-Shell");
        TestAssert.Equal("rust_thrust", fixture.Actor.GetNode<RustShellFormalVisual>("VisualRoot/Formal").CurrentPoseMarker,
            "Rust-Shell Active uses a thrust silhouette marker");
        fixture.Actor._PhysicsProcess(.20);
        AssertRecovery(fixture, published, "Rust-Shell");
        fixture.Free();
    }

    private static void MothRuntimeLocksSweptDiveAndPublishesPostMoveActiveCircle()
    {
        RuntimeFixture<GraveLampMoth> fixture = CreateFixture<GraveLampMoth>(
            "res://scenes/enemies/GraveLampMoth.tscn", new Vector2(0f, 140f));
        TestAssert.Equal(CombatCollisionLayers.World, fixture.Actor.CollisionMask,
            "Moth presentation fixture preserves the packed scene production World collision mask");
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(.45);
        TestAssert.True(published.Timeline.Phase == AttackPhase.Telegraph && published.Timeline.HasLockedTarget,
            "Moth publishes a locked red dive warning before Active");
        Vector2 locked = published.Timeline.LockedWorldTarget;
        Vector2 lockedStart = fixture.Collider.GlobalPosition;
        float realRadius = ((CircleShape2D)fixture.Collider.Shape).Radius;
        TestAssert.Equal(AttackShapeKind.Capsule, published.Shape.Kind,
            "locked Moth Telegraph publishes the swept circular collider as a capsule path");
        AssertBoundsNear(SweptCircleBounds(lockedStart, locked, realRadius), PresentationBounds(published.Shape),
            "locked Moth warning bounds come from locked start, locked target, and the real collider radius");
        Rect2 lockedPath = PresentationBounds(published.Shape);
        TestAssert.Equal("moth_folded_wings",
            fixture.Actor.GetNode<GraveLampMothFormalVisual>("VisualRoot/Formal").CurrentPoseMarker,
            "Moth warning uses folded wings");
        fixture.Player.Position += new Vector2(230f, 0f);
        fixture.Actor._PhysicsProcess(.10);
        TestAssert.Equal(AttackPhase.Telegraph, published.Timeline.Phase,
            "player crossing keeps Moth in its locked Telegraph window");
        AssertBoundsNear(lockedPath, PresentationBounds(published.Shape),
            "locked Moth trajectory and landing do not chase a crossing player");
        Rect2 beforeActiveMovement = CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform);
        fixture.Actor._PhysicsProcess(.15);
        AssertActive(fixture, published, "Grave-Lamp Moth");
        TestAssert.True(published.Timeline.LockedWorldTarget.DistanceTo(locked) < .01f,
            "locked Moth dive does not chase a player who moves after lock");
        TestAssert.Equal(AttackShapeKind.Circle, published.Shape.Kind,
            "Moth Active publishes the real current circular damage collider");
        Rect2 afterActiveMovement = CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform);
        TestAssert.True(afterActiveMovement.Position.DistanceTo(beforeActiveMovement.Position) > .1f,
            "Moth Active fixture proves movement occurred during the physics step");
        AssertBoundsNear(afterActiveMovement, PresentationBounds(published.Shape),
            "Moth Active presentation is published after movement and equals that step's real collider bounds");
        TestAssert.Equal("moth_dive",
            fixture.Actor.GetNode<GraveLampMothFormalVisual>("VisualRoot/Formal").CurrentPoseMarker,
            "Moth Active uses a dive silhouette marker");
        fixture.Actor._PhysicsProcess(.36);
        AssertRecovery(fixture, published, "Grave-Lamp Moth");
        TestAssert.True(!fixture.Telegraph.GoldVisible && !fixture.Telegraph.DangerVisible &&
            !fixture.Telegraph.ImpactVisible, "Moth Recovery clears every warning and impact layer");
        fixture.Free();
    }

    private static void LaborerRuntimeUsesAnAuthoritativeLockedFanOnBothFacings()
    {
        Rect2 right = ExerciseLaborerSweep(70f, 1);
        Rect2 left = ExerciseLaborerSweep(-70f, -1);
        TestAssert.True(Mathf.Abs(right.Position.X + left.End.X) < .08f &&
            Mathf.Abs(right.End.X + left.Position.X) < .08f &&
            right.Size.DistanceTo(left.Size) < .08f,
            $"Laborer fan mirrors across both facings; right={right}, left={left}");
    }

    private static Rect2 ExerciseLaborerSweep(float playerX, int expectedFacing)
    {
        RuntimeFixture<BellBackLaborer> fixture = CreateFixture<BellBackLaborer>(
            "res://scenes/enemies/BellBackLaborer.tscn", new Vector2(playerX, 0f));
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(.45);
        AssertWarning(fixture, published, "Bell-Back Laborer");
        TestAssert.Equal(AttackShapeKind.ConvexPolygon, published.Shape.Kind,
            "Laborer Telegraph uses its real convex fan collider");
        TestAssert.Equal(expectedFacing, fixture.Actor.Facing,
            "Laborer locks the attack-facing when Telegraph begins");
        AssertConvexPolygonAuthority(fixture, published,
            "Laborer Telegraph presentation points equal the real collision polygon points");
        ulong attackId = published.Timeline.AttackId;
        Rect2 lockedFan = CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform);
        TestAssert.Equal("laborer_counter_twist",
            fixture.Actor.GetNode<BellBackLaborerFormalVisual>("VisualRoot/Formal").CurrentPoseMarker,
            "Laborer warning uses a counter-twist silhouette marker");

        fixture.Player.Position = new Vector2(-playerX, 0f);
        fixture.Actor._PhysicsProcess(.45);
        TestAssert.Equal(AttackPhase.Telegraph, published.Timeline.Phase,
            "Laborer remains in Telegraph while the player crosses sides");
        TestAssert.Equal(expectedFacing, fixture.Actor.Facing,
            "player crossover cannot flip the locked Laborer fan");
        AssertBoundsNear(lockedFan, CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            "Laborer real fan collider stays locked during crossover");
        AssertConvexPolygonAuthority(fixture, published,
            "Laborer locked Telegraph keeps collision and presentation polygon points equal");

        fixture.Actor._PhysicsProcess(.30);
        AssertActive(fixture, published, "Bell-Back Laborer");
        TestAssert.Equal(attackId, published.Timeline.AttackId,
            "Laborer Active preserves the Telegraph attack identity");
        TestAssert.Equal(expectedFacing, fixture.Actor.Facing,
            "Laborer Active preserves its locked facing");
        AssertConvexPolygonAuthority(fixture, published,
            "Laborer Active white fan equals the real damage polygon");
        TestAssert.Equal("laborer_sweep",
            fixture.Actor.GetNode<BellBackLaborerFormalVisual>("VisualRoot/Formal").CurrentPoseMarker,
            "Laborer Active uses a sweep silhouette marker");
        fixture.Actor._PhysicsProcess(.25);
        AssertRecovery(fixture, published, "Bell-Back Laborer");
        TestAssert.Equal(attackId, published.Timeline.AttackId,
            "Laborer Recovery preserves the attack identity");
        TestAssert.Equal(expectedFacing, fixture.Actor.Facing,
            "Laborer Recovery preserves its locked facing");
        AssertConvexPolygonAuthority(fixture, published,
            "Laborer Recovery presentation geometry still derives from the real fan collider");
        TestAssert.True(!fixture.Telegraph.GoldVisible && !fixture.Telegraph.DangerVisible &&
            !fixture.Telegraph.ImpactVisible, "Laborer Recovery clears all telegraph layers");

        fixture.Actor.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(99, Vector2.Right, 0f, 79001UL));
        TestAssert.Equal(AttackPhase.None, published.Timeline.Phase,
            "Laborer death clears its published attack frame");
        TestAssert.True(!fixture.Hitbox.Monitoring && !fixture.Telegraph.GoldVisible &&
            !fixture.Telegraph.DangerVisible && !fixture.Telegraph.ImpactVisible,
            "Laborer death clears damage and every telegraph layer");
        fixture.Free();
        return lockedFan;
    }

    private static void BossUsesDistinctRealShapesAndPosesForEveryAttack()
    {
        RuntimeFixture<FallenBellkeeper> fixture = CreateFixture<FallenBellkeeper>(
            "res://scenes/boss/FallenBellkeeper.tscn", new Vector2(-120f, 20f));
        fixture.Actor.Gravity = 0f;
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor.ActivateEncounter();
        fixture.Actor._PhysicsProcess(1.05);
        var expected = new Dictionary<EnemyAttackKind, (AttackShapeKind Shape, string Telegraph, string Active)>
        {
            [EnemyAttackKind.BossSweep] = (AttackShapeKind.ConvexPolygon, "boss_sweep_windup", "boss_sweep_release"),
            [EnemyAttackKind.BossLeapSlam] = (AttackShapeKind.Circle, "boss_leap_crouch", "boss_leap_impact"),
            [EnemyAttackKind.BossGroundWave] = (AttackShapeKind.GroundBand, "boss_groundwave_raise", "boss_groundwave_release")
        };
        var seen = new HashSet<EnemyAttackKind>();
        for (int i = 0; i < 20 && seen.Count < expected.Count; i++)
        {
            fixture.Actor._PhysicsProcess(.45);
            if (!expected.TryGetValue(published.Timeline.Kind, out var contract)) continue;
            if (published.Timeline.Phase == AttackPhase.Telegraph)
            {
                seen.Add(published.Timeline.Kind);
                TestAssert.Equal(contract.Shape, published.Shape.Kind,
                    $"{published.Timeline.Kind} uses its real attack-specific collision shape");
                AssertBoundsNear(CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
                    PresentationBounds(published.Shape), $"{published.Timeline.Kind} world warning bounds equal hitbox");
                TestAssert.Equal(contract.Telegraph,
                    fixture.Actor.GetNode<FallenBellkeeperFormalVisual>("VisualRoot/Formal").CurrentAttackPose,
                    $"{published.Timeline.Kind} has a unique preparation pose");
            }
            else if (published.Timeline.Phase == AttackPhase.Active)
            {
                TestAssert.Equal(contract.Active,
                    fixture.Actor.GetNode<FallenBellkeeperFormalVisual>("VisualRoot/Formal").CurrentAttackPose,
                    $"{published.Timeline.Kind} has a unique release pose");
                TestAssert.True(fixture.Hitbox.Monitoring && !fixture.Telegraph.DangerVisible,
                    $"{published.Timeline.Kind} Active enables damage and clears red");
            }
        }
        TestAssert.Equal(expected.Count, seen.Count, "Boss cycles through Sweep, LeapSlam and GroundWave true shapes");
        fixture.Free();
    }

    private static void BossLockedSweepDoesNotFlipWhenPlayerCrossesSides()
    {
        RuntimeFixture<FallenBellkeeper> fixture = CreateFixture<FallenBellkeeper>(
            "res://scenes/boss/FallenBellkeeper.tscn", new Vector2(-150f, 20f));
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor.ActivateEncounter();
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(1.05);
        TestAssert.Equal(EnemyAttackKind.BossSweep, published.Timeline.Kind,
            "cross-side fixture starts in Sweep Telegraph");
        fixture.Actor._PhysicsProcess(.45);
        TestAssert.True(published.Timeline.HasLockedTarget,
            "Sweep locks its target and attack-facing before the cross-side check");
        Rect2 lockedCollision = CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform);
        Rect2 lockedPresentation = PresentationBounds(published.Shape);
        fixture.Player.Position = new Vector2(180f, 20f);
        fixture.Actor._PhysicsProcess(.10);
        TestAssert.Equal(AttackPhase.Telegraph, published.Timeline.Phase,
            "crossing sides keeps Sweep in its locked Telegraph window");
        AssertBoundsNear(lockedCollision,
            CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            "locked Sweep collision does not flip when the player crosses sides");
        AssertBoundsNear(lockedPresentation, PresentationBounds(published.Shape),
            "locked Sweep presentation does not flip when the player crosses sides");
        fixture.Free();
    }

    private static void BossNewLeapTelegraphDropsThePreviousAttackTarget()
    {
        RuntimeFixture<FallenBellkeeper> fixture = CreateFixture<FallenBellkeeper>(
            "res://scenes/boss/FallenBellkeeper.tscn", new Vector2(-150f, 20f));
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor.ActivateEncounter();
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(1.05);
        fixture.Actor._PhysicsProcess(.45);
        Vector2 previousLockedTarget = published.Timeline.LockedWorldTarget;
        fixture.Player.Position = new Vector2(190f, 26f);
        fixture.Actor._PhysicsProcess(.40);
        fixture.Actor._PhysicsProcess(.30);
        fixture.Actor._PhysicsProcess(.75);
        TestAssert.Equal(EnemyAttackKind.BossLeapSlam, published.Timeline.Kind,
            "Boss begins LeapSlam after Sweep Recovery");
        TestAssert.Equal(AttackPhase.Telegraph, published.Timeline.Phase,
            "first LeapSlam frame is Telegraph");
        TestAssert.True(published.Shape.Origin.DistanceTo(fixture.Player.GlobalPosition) < .01f,
            $"first LeapSlam Telegraph uses the new live target, not previous lock {previousLockedTarget}");
        AssertBoundsNear(CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            PresentationBounds(published.Shape), "first LeapSlam warning equals its newly positioned real circle");
        fixture.Free();
    }

    private static void BossHammerPathStaysLockedThroughActiveAndRecovery()
    {
        RuntimeFixture<FallenBellkeeper> fixture = CreateFixture<FallenBellkeeper>(
            "res://scenes/boss/FallenBellkeeper.tscn", new Vector2(-170f, 20f));
        AttackPresentationFrame published = default;
        fixture.Actor.AttackFrameChanged += frame => published = frame;
        fixture.Actor.ActivateEncounter();
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(1.05);
        fixture.Actor.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(8, Vector2.Right, 0f, 73001));
        fixture.Actor._PhysicsProcess(.01);
        fixture.Actor._PhysicsProcess(1.25);
        fixture.Actor._PhysicsProcess(.70);
        TestAssert.Equal(EnemyAttackKind.BossHammerThrow, published.Timeline.Kind,
            "Boss publishes HammerThrow Telegraph after phase-two dwell");
        fixture.Actor._PhysicsProcess(.45);
        TestAssert.True(published.Timeline.HasLockedTarget,
            "HammerThrow locks its target and facing during Telegraph");
        TestAssert.Equal(AttackShapeKind.Capsule, published.Shape.Kind,
            "HammerThrow collision is the real projectile/chain capsule path");
        TestAssert.Equal("boss_hammer_windup",
            fixture.Actor.GetNode<FallenBellkeeperFormalVisual>("VisualRoot/Formal").CurrentAttackPose,
            "HammerThrow Telegraph has a unique windup pose");
        AssertBoundsNear(CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            PresentationBounds(published.Shape), "HammerThrow Telegraph bounds equal the real capsule path");
        Rect2 lockedPath = PresentationBounds(published.Shape);
        Vector2 lockedTarget = published.Timeline.LockedWorldTarget;

        fixture.Player.Position = new Vector2(230f, 20f);
        fixture.Actor._PhysicsProcess(.20);
        TestAssert.True(published.Timeline.LockedWorldTarget.DistanceTo(lockedTarget) < .01f,
            "HammerThrow locked target does not chase the player across sides");
        AssertBoundsNear(lockedPath, PresentationBounds(published.Shape),
            "HammerThrow locked presentation path remains stable before Active");
        AssertBoundsNear(lockedPath, CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            "HammerThrow locked collision path remains stable before Active");

        fixture.Actor._PhysicsProcess(.25);
        TestAssert.Equal(AttackPhase.Active, published.Timeline.Phase, "HammerThrow reaches Active");
        TestAssert.True(fixture.Hitbox.Monitoring && !fixture.Telegraph.DangerVisible && fixture.Telegraph.ImpactVisible,
            "HammerThrow Active enables damage, clears red and confirms impact");
        TestAssert.Equal("boss_hammer_release",
            fixture.Actor.GetNode<FallenBellkeeperFormalVisual>("VisualRoot/Formal").CurrentAttackPose,
            "HammerThrow Active exposes its release pose marker");
        AssertBoundsNear(lockedPath, PresentationBounds(published.Shape),
            "HammerThrow Active keeps the locked presentation path");
        AssertBoundsNear(lockedPath, CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            "HammerThrow Active collision still equals its presentation path");

        fixture.Actor._PhysicsProcess(.85);
        TestAssert.Equal(AttackPhase.Recovery, published.Timeline.Phase, "HammerThrow reaches Recovery");
        TestAssert.True(!fixture.Hitbox.Monitoring, "HammerThrow Recovery disables damage");
        AssertBoundsNear(lockedPath, PresentationBounds(published.Shape),
            "HammerThrow Recovery keeps the locked path for readable follow-through");
        AssertBoundsNear(lockedPath, CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            "HammerThrow Recovery collision geometry remains the presentation authority");
        fixture.Free();
    }

    private static void AssertWarning<T>(RuntimeFixture<T> fixture, AttackPresentationFrame frame, string label)
        where T : Node2D
    {
        TestAssert.Equal(AttackPhase.Telegraph, frame.Timeline.Phase, $"{label} publishes Telegraph before damage");
        TestAssert.True(!fixture.Hitbox.Monitoring && (fixture.Telegraph.GoldVisible || fixture.Telegraph.DangerVisible),
            $"{label} warning is visible while its real hitbox is not monitoring");
        AssertBoundsNear(CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            PresentationBounds(frame.Shape), $"{label} warning world bounds equal its real hitbox");
    }

    private static void AssertActive<T>(RuntimeFixture<T> fixture, AttackPresentationFrame frame, string label)
        where T : Node2D
    {
        TestAssert.Equal(AttackPhase.Active, frame.Timeline.Phase, $"{label} reaches Active");
        TestAssert.True(fixture.Hitbox.Monitoring && !fixture.Telegraph.DangerVisible && fixture.Telegraph.ImpactVisible,
            $"{label} Active enables the hitbox, clears red and confirms impact in white");
    }

    private static void AssertRecovery<T>(RuntimeFixture<T> fixture, AttackPresentationFrame frame, string label)
        where T : Node2D
    {
        TestAssert.Equal(AttackPhase.Recovery, frame.Timeline.Phase, $"{label} reaches Recovery");
        TestAssert.True(!fixture.Hitbox.Monitoring, $"{label} Recovery disables damage");
    }

    private static void AssertConvexPolygonAuthority(RuntimeFixture<BellBackLaborer> fixture,
        AttackPresentationFrame frame, string message)
    {
        TestAssert.True(fixture.Collider.Shape is ConvexPolygonShape2D,
            "Laborer authoritative damage collider is a ConvexPolygonShape2D");
        var polygon = (ConvexPolygonShape2D)fixture.Collider.Shape;
        TestAssert.Equal(polygon.Points.Length, frame.Shape.Points.Count,
            $"{message}: point count");
        for (int i = 0; i < polygon.Points.Length; i++)
        {
            Vector2 expected = fixture.Collider.GlobalTransform * polygon.Points[i];
            TestAssert.True(expected.DistanceTo(frame.Shape.Points[i]) < .01f,
                $"{message}: point {i} expected={expected}, actual={frame.Shape.Points[i]}");
        }
        AssertBoundsNear(CollisionBounds(fixture.Collider.Shape, fixture.Collider.GlobalTransform),
            PresentationBounds(frame.Shape), $"{message}: world bounds");
    }

    private static RuntimeFixture<T> CreateFixture<T>(string scenePath, Vector2 playerOffset) where T : Node2D
    {
        SceneTree tree = (SceneTree)Engine.GetMainLoop();
        var root = new Node2D { Name = $"{typeof(T).Name}AttackTelegraphFixture" };
        var player = new Node2D { Name = "AttackTarget", Position = playerOffset };
        player.AddToGroup("player");
        T actor = GD.Load<PackedScene>(scenePath).Instantiate<T>();
        actor.Name = typeof(T).Name;
        if (actor is RustShellSquire rust) rust.Gravity = 0f;
        if (actor is BellBackLaborer laborer) laborer.Tuning.Gravity = 0f;
        if (actor is FallenBellkeeper boss) boss.Gravity = 0f;
        AttackTelegraphVisual telegraph = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        root.AddChild(player);
        root.AddChild(actor);
        tree.Root.AddChild(root);
        Hitbox hitbox = actor.GetNode<Hitbox>("AttackHitbox");
        CollisionShape2D collider = hitbox.GetNode<CollisionShape2D>("AttackCollider");
        return new RuntimeFixture<T>(root, player, actor, telegraph, hitbox, collider);
    }

    private static Rect2 CollisionBounds(Shape2D shape, Transform2D transform)
    {
        List<Vector2> points = ShapePoints(shape);
        return Bounds(points.ConvertAll(point => transform * point));
    }

    private static Rect2 SweptCircleBounds(Vector2 start, Vector2 target, float radius)
    {
        Vector2 min = new(Mathf.Min(start.X, target.X) - radius, Mathf.Min(start.Y, target.Y) - radius);
        Vector2 max = new(Mathf.Max(start.X, target.X) + radius, Mathf.Max(start.Y, target.Y) + radius);
        return new Rect2(min, max - min);
    }

    private static Rect2 PresentationBounds(AttackShapeFrame frame)
    {
        List<Vector2> points;
        if (frame.Kind == AttackShapeKind.ConvexPolygon)
            points = new List<Vector2>(frame.Points);
        else
        {
            points = ShapePoints(frame.Kind, frame.Extent);
            Transform2D transform = new(frame.RotationRadians, frame.Origin);
            points = points.ConvertAll(point => transform * point);
        }
        return Bounds(points);
    }

    private static List<Vector2> ShapePoints(Shape2D shape) => shape switch
    {
        RectangleShape2D rectangle => ShapePoints(AttackShapeKind.Rectangle, rectangle.Size * .5f),
        CircleShape2D circle => ShapePoints(AttackShapeKind.Circle, Vector2.One * circle.Radius),
        CapsuleShape2D capsule => ShapePoints(AttackShapeKind.Capsule, new Vector2(capsule.Radius, capsule.Height * .5f)),
        ConvexPolygonShape2D polygon => new List<Vector2>(polygon.Points),
        _ => throw new InvalidOperationException($"Unsupported fixture shape {shape.GetType().Name}")
    };

    private static List<Vector2> ShapePoints(AttackShapeKind kind, Vector2 extent)
    {
        if (kind is AttackShapeKind.Rectangle or AttackShapeKind.GroundBand)
            return [new(-extent.X, -extent.Y), new(extent.X, -extent.Y),
                new(extent.X, extent.Y), new(-extent.X, extent.Y)];
        var points = new List<Vector2>();
        if (kind == AttackShapeKind.Circle)
        {
            for (int i = 0; i < 64; i++)
            {
                float angle = Mathf.Tau * i / 64f;
                points.Add(new Vector2(Mathf.Cos(angle) * extent.X, Mathf.Sin(angle) * extent.Y));
            }
            return points;
        }
        float halfStraight = Mathf.Max(0f, extent.Y - extent.X);
        for (int i = 0; i < 32; i++)
        {
            float angle = Mathf.Pi + Mathf.Pi * i / 31f;
            points.Add(new Vector2(Mathf.Cos(angle) * extent.X, -halfStraight + Mathf.Sin(angle) * extent.X));
        }
        for (int i = 0; i < 32; i++)
        {
            float angle = Mathf.Pi * i / 31f;
            points.Add(new Vector2(Mathf.Cos(angle) * extent.X, halfStraight + Mathf.Sin(angle) * extent.X));
        }
        return points;
    }

    private static Rect2 Bounds(IReadOnlyList<Vector2> points)
    {
        Vector2 min = points[0];
        Vector2 max = points[0];
        for (int i = 1; i < points.Count; i++)
        {
            min = new Vector2(Mathf.Min(min.X, points[i].X), Mathf.Min(min.Y, points[i].Y));
            max = new Vector2(Mathf.Max(max.X, points[i].X), Mathf.Max(max.Y, points[i].Y));
        }
        return new Rect2(min, max - min);
    }

    private static void AssertBoundsNear(Rect2 expected, Rect2 actual, string message)
    {
        TestAssert.True(expected.Position.DistanceTo(actual.Position) < .08f &&
            expected.Size.DistanceTo(actual.Size) < .08f, $"{message}; expected={expected}, actual={actual}");
    }

    private sealed record RuntimeFixture<T>(Node2D Root, Node2D Player, T Actor,
        AttackTelegraphVisual Telegraph, Hitbox Hitbox, CollisionShape2D Collider) where T : Node2D
    {
        public void Free() => Root.Free();
    }
}
