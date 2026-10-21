using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;

namespace Knight.Tests.Integration;

public static class RangedAttackGeometryRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var fixture = new NativeFixture(tree);
        try
        {
            fixture.Populate();
            await fixture.Sync();
            fixture.AssertNativeFixtureIsLive();
            WorldBeforePlayerStopsAtTheSafeSideOfAThinWall(fixture);
            PlayerBeforeWorldReturnsTheActualHurtbox(fixture);
            EqualWorldAndPlayerContactBelongsToWorld(fixture);
            ExistingWorldOverlapCannotAuthorizePlayerDamage(fixture);
            ExistingPlayerOverlapIsAnImmediateRealTarget(fixture);
            ThickBeamClipsItsRadiusWhenTheCentreRayMisses(fixture);
            ExplosionTargetsRespectCircleAndWorldOcclusion(fixture);
            NonHurtboxPlayerCollidersDoNotMasqueradeAsTargets(fixture);
            BorrowedExcludeAndSpaceSurviveEveryQuery(fixture);
            AllowedAreaKeepsTheCompleteCapsuleInsideItsBoundary(fixture);
            InvalidQueriesNeverAuthorizeATarget(fixture);
            GD.Print("RANGED_ATTACK_GEOMETRY_NATIVE_OK independent_world=true actual_hurtbox=true thin_wall=true");
        }
        finally { await fixture.DisposeAsync(); }
    }

    // Catches endpoint-only collision, a Player-first query, or treating World
    // areas as solid terrain. Thin wall x79..81; radius5 must stop centre <=74.
    private static void WorldBeforePlayerStopsAtTheSafeSideOfAThinWall(NativeFixture f)
    {
        RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, new Vector2(20f, 50f), new Vector2(200f, 0f), 5f);
        TestAssert.True(result.WorldHit && result.Target == null && !result.StartedInWorld,
            "RANGED_NATIVE_WORLD_FIRST_RED native thin World body wins before the actual player Hurtbox");
        NearFraction(.27f, result.SafeFraction, .005f, "thin wall safe fraction is derived from x74");
        InRange(20f + 200f * result.SafeFraction, 73f, 74.1f,
            "the safe projectile centre remains before the two-pixel wall, never through it");
        TestAssert.Equal(5, f.BehindWall.Health.CurrentHealth, "a geometry query never damages the blocked real Health");
    }

    // Catches always preferring any World contact even when the Player is earlier.
    // Player circle x72..88 + swept radius5 -> centre67, (67-20)/200=.235.
    private static void PlayerBeforeWorldReturnsTheActualHurtbox(NativeFixture f)
    {
        RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, new Vector2(20f, 120f), new Vector2(200f, 0f), 5f);
        TestAssert.True(!result.WorldHit && result.Target == f.FrontPlayer.Box && !result.StartedInWorld,
            "RANGED_NATIVE_PLAYER_FIRST_RED earlier native area returns the exact real Hurtbox, not its parent/body");
        NearFraction(.235f, result.SafeFraction, .005f, "the player's leading surface supplies the first contact");
        TestAssert.True(result.Target.ResolveDamageReceiver() == f.FrontPlayer.Health &&
            f.FrontPlayer.Health.CurrentHealth == 5, "the selected Hurtbox resolves to live, untouched real Health");
    }

    // Identical world and player rectangles share x100; radius5 reaches both
    // with centre95. Catches tie-to-player and order-dependent result selection.
    private static void EqualWorldAndPlayerContactBelongsToWorld(NativeFixture f)
    {
        RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, new Vector2(20f, 190f), new Vector2(200f, 0f), 5f);
        TestAssert.True(result.WorldHit && result.Target == null && !result.StartedInWorld,
            "RANGED_NATIVE_TIE_RED equal native World/Hurtbox contact is fail-closed World priority");
        NearFraction(.375f, result.SafeFraction, .005f, "equal leading surfaces have literal safe fraction .375");
    }

    // CastMotion alone ignores this wall because the swept circle starts in it.
    // A nearby actual Hurtbox also overlaps the starting circle; World wins.
    private static void ExistingWorldOverlapCannotAuthorizePlayerDamage(NativeFixture f)
    {
        Vector2 origin = new(80f, 260f);
        RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, origin, new Vector2(200f, 0f), 5f);
        TestAssert.True(result.WorldHit && result.StartedInWorld && result.Target == null,
            "RANGED_NATIVE_EMBEDDED_RED existing World overlap retires without authorizing overlapping player damage");
        NearFraction(0f, result.SafeFraction, 0f, "embedded projectile cannot advance");
        NearVector(origin, RangedAttackGeometry.ClipBeam(f.Space, origin, Vector2.Right, 200f, 5f), .0001f,
            "a beam starting embedded in solid World cannot escape its wall");
        TestAssert.True(!RangedAttackGeometry.HasClearWorldPath(f.Space, origin, new Vector2(110f, 260f)),
            "World LOS rejects its existing solid-origin overlap");
        TestAssert.Equal(0, RangedAttackGeometry.ExplosionTargets(f.Space, origin, 56f).Length,
            "an embedded explosion centre authorizes no real target");
        TestAssert.Equal(5, f.EmbeddedPlayer.Health.CurrentHealth, "embedded geometry leaves actual HP unchanged");
    }

    // Catches using CastMotion without the starting Player IntersectShape check.
    private static void ExistingPlayerOverlapIsAnImmediateRealTarget(NativeFixture f)
    {
        RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, new Vector2(80f, 330f), new Vector2(100f, 0f), 5f);
        TestAssert.True(!result.WorldHit && !result.StartedInWorld && result.Target == f.OverlappingPlayer.Box,
            "RANGED_NATIVE_PLAYER_OVERLAP_RED an existing real player overlap is selected without a World hit");
        NearFraction(0f, result.SafeFraction, 0f, "existing player overlap is immediate contact");
    }

    // World corner (99,404) is four pixels below the ray at y400. Radius5
    // touches it with centre x96 (3:4:5 triangle); a centre-only ray sees clear.
    private static void ThickBeamClipsItsRadiusWhenTheCentreRayMisses(NativeFixture f)
    {
        TestAssert.True(RangedAttackGeometry.HasClearWorldPath(f.Space, new Vector2(20f, 400f), new Vector2(220f, 400f)),
            "grazing fixture's centreline is genuinely clear of World");
        Vector2 end = RangedAttackGeometry.ClipBeam(f.Space, new Vector2(20f, 400f), new Vector2(3f, 0f), 200f, 5f);
        InRange(end.X, 95f, 96.2f,
            "RANGED_NATIVE_THICK_BEAM_RED native radius5 cast clips the beam at its corner contact, despite a clear centre ray");
        NearFinite(400f, end.Y, .0001f, "beam clipping does not bend the locked aim");
        Vector2 headOn = RangedAttackGeometry.ClipBeam(f.Space, new Vector2(20f, 470f), Vector2.Right, 200f, 5f);
        InRange(headOn.X, 93f, 94.1f, "a head-on beam endpoint stays a complete radius before thin wall x99");
        AttackShapeFrame capsule = RangedAttackGeometry.Beam(new Vector2(20f, 470f), headOn, 5f);
        Rect2 bounds = AttackShapeBounds.WorldBounds(capsule);
        InRange(bounds.End.X, 98f, 99.1f, "same capsule danger ends at the native clipped full-radius boundary");
    }

    // Catches rectangle-only overlap, missing LOS, authorizing fake Player
    // areas/bodies, or query code applying damage itself.
    private static void ExplosionTargetsRespectCircleAndWorldOcclusion(NativeFixture f)
    {
        Hurtbox[] targets = RangedAttackGeometry.ExplosionTargets(f.Space, new Vector2(50f, 600f), 80f);
        TestAssert.True(targets.Length == 1 && targets[0] == f.VisibleExplosionPlayer.Box,
            "RANGED_NATIVE_EXPLOSION_OCCLUSION_RED only the actual circle-overlapping unobstructed Hurtbox is authorized");
        TestAssert.True(RangedAttackGeometry.HasClearWorldPath(f.Space, new Vector2(50f, 600f), new Vector2(50f, 650f)) &&
            !RangedAttackGeometry.HasClearWorldPath(f.Space, new Vector2(50f, 600f), new Vector2(110f, 600f)),
            "literal vertical target has clear World LOS while horizontal target is behind the wall");
        TestAssert.True(f.VisibleExplosionPlayer.Health.CurrentHealth == 5 && f.BlockedExplosionPlayer.Health.CurrentHealth == 5 &&
            f.OutsideCirclePlayer.Health.CurrentHealth == 5, "target discovery leaves all actual receivers unchanged");
    }

    // Catches returning an arbitrary Player-layer Area2D or letting that fake
    // near collider hide the later genuine Hurtbox; Player bodies are ignored.
    private static void NonHurtboxPlayerCollidersDoNotMasqueradeAsTargets(NativeFixture f)
    {
        RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, new Vector2(20f, 760f), new Vector2(200f, 0f), 5f);
        TestAssert.True(!result.WorldHit && result.Target == f.AfterFakePlayer.Box,
            "RANGED_NATIVE_REAL_HURTBOX_RED non-Hurtbox areas and Player bodies cannot replace or hide a genuine target");
        NearFraction(.335f, result.SafeFraction, .005f, "genuine circle contact centre87 is selected after fake front colliders");
    }

    // Catches mutating/disposing borrowed exclusion arrays or the borrowed
    // DirectSpaceState. The same caller-owned array survives all APIs and reuse.
    private static void BorrowedExcludeAndSpaceSurviveEveryQuery(NativeFixture f)
    {
        var exclude = new Godot.Collections.Array<Rid> { f.FirstWall.GetRid(), f.ExplosionWall.GetRid() };
        RangedSweepResult sweep = RangedAttackGeometry.Sweep(f.Space, new Vector2(20f, 50f), new Vector2(200f, 0f), 5f, exclude);
        TestAssert.True(!sweep.WorldHit && sweep.Target == f.BehindWall.Box,
            "excluding the near wall exposes the exact real player behind it");
        NearVector(new Vector2(220f, 50f), RangedAttackGeometry.ClipBeam(f.Space,
            new Vector2(20f, 50f), Vector2.Right, 200f, 5f, exclude), .0001f,
            "beam clipping only tests World bodies and respects caller exclusions");
        TestAssert.True(RangedAttackGeometry.HasClearWorldPath(f.Space, new Vector2(50f, 600f), new Vector2(110f, 600f), exclude),
            "LOS respects the same borrowed World exclusion");
        Hurtbox[] targets = RangedAttackGeometry.ExplosionTargets(f.Space, new Vector2(50f, 600f), 80f, exclude);
        TestAssert.True(targets.Length == 2 && Contains(targets, f.VisibleExplosionPlayer.Box) && Contains(targets, f.BlockedExplosionPlayer.Box),
            "excluded wall changes actual authorized explosion targets, not the circular selection boundary");
        TestAssert.True(exclude.Count == 2 && exclude[0] == f.FirstWall.GetRid() && exclude[1] == f.ExplosionWall.GetRid(),
            "all queries preserve the caller-owned exclusion array and its RID order");
        f.AssertNativeFixtureIsLive();
    }

    // Catches clipped centres that still let cap radius cross the allowed area.
    private static void AllowedAreaKeepsTheCompleteCapsuleInsideItsBoundary(NativeFixture f)
    {
        Vector2 from = new(50f, 850f);
        Rect2 area = new(0f, 820f, 100f, 60f);
        Vector2 end = RangedAttackGeometry.ClipToArea(from, new Vector2(200f, 880f), area, 5f);
        NearVector(new Vector2(95f, 859f), end, .0001f,
            "RANGED_NATIVE_ALLOWED_AREA_RED exact inset boundary is reached along the original shot line");
        RangedSweepResult sweep = RangedAttackGeometry.Sweep(f.Space, from, end - from, 5f);
        TestAssert.True(!sweep.WorldHit && sweep.Target == null && !sweep.StartedInWorld,
            "legal empty-space segment is not a fabricated impact");
        NearFraction(1f, sweep.SafeFraction, 0f, "legal empty-space segment can reach the clipped endpoint");
        Rect2 bounds = AttackShapeBounds.WorldBounds(RangedAttackGeometry.Beam(from, end, 5f));
        NearVector(new Vector2(45f, 845f), bounds.Position, .0001f, "full beam starts at its origin cap boundary");
        NearVector(new Vector2(100f, 864f), bounds.End, .0001f, "full beam ends inside allowed rectangle, including radius");
    }

    // Catches non-finite/negative query inputs authorizing native damage or
    // escaping via NaN fractions. Valid borrowed space remains usable after each.
    private static void InvalidQueriesNeverAuthorizeATarget(NativeFixture f)
    {
        Vector2 origin = new(20f, 120f);
        foreach (float radius in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
        {
            RangedSweepResult result = RangedAttackGeometry.Sweep(f.Space, origin, new Vector2(200f, 0f), radius);
            TestAssert.True(result.Target == null && float.IsFinite(result.SafeFraction) && result.SafeFraction == 0f,
                "invalid swept radius authorizes neither target nor movement");
            NearVector(origin, RangedAttackGeometry.ClipBeam(f.Space, origin, Vector2.Right, 200f, radius), 0f,
                "invalid beam radius fails closed at its origin");
            TestAssert.Equal(0, RangedAttackGeometry.ExplosionTargets(f.Space, origin, radius).Length,
                "invalid explosion radius authorizes no target");
        }
        TestAssert.True(!RangedAttackGeometry.HasClearWorldPath(f.Space, origin, new Vector2(float.NaN, 120f)),
            "non-finite LOS cannot authorize a shot");
        RangedSweepResult missingSpace = RangedAttackGeometry.Sweep(null, origin, Vector2.Right, 5f);
        TestAssert.True(missingSpace.Target == null && missingSpace.SafeFraction == 0f,
            "missing native space fails closed without target or movement");
        f.AssertNativeFixtureIsLive();
    }

    private static bool Contains(Hurtbox[] targets, Hurtbox wanted)
    {
        foreach (Hurtbox target in targets) if (target == wanted) return true;
        return false;
    }

    private static void NearFraction(float expected, float actual, float tolerance, string message)
    {
        InRange(actual, 0f, 1f, message + " is a finite fraction");
        TestAssert.Near(expected, actual, tolerance, message);
    }

    private static void NearVector(Vector2 expected, Vector2 actual, float tolerance, string message)
    {
        NearFinite(expected.X, actual.X, tolerance, message + " X");
        NearFinite(expected.Y, actual.Y, tolerance, message + " Y");
    }

    private static void NearFinite(float expected, float actual, float tolerance, string message)
    {
        TestAssert.True(float.IsFinite(actual), message + " is finite");
        TestAssert.Near(expected, actual, tolerance, message);
    }

    private static void InRange(float value, float minimum, float maximum, string message) =>
        TestAssert.True(float.IsFinite(value) && value >= minimum && value <= maximum,
            $"{message}; expected=[{minimum},{maximum}], actual={value}");

    private readonly record struct PlayerTarget(Hurtbox Box, HealthComponent Health);

    // Only this test utility owns the viewport, custom World2D, nodes, and
    // authored shapes. Space is borrowed from World2D and is never disposed.
    private sealed class NativeFixture
    {
        private readonly SceneTree _tree;
        private readonly World2D _world;
        private readonly SubViewport _viewport;
        private readonly Node2D _scope;
        private readonly List<Shape2D> _ownedShapes = new();
        private bool _disposed;
        public PhysicsDirectSpaceState2D Space => _world.DirectSpaceState;
        public StaticBody2D FirstWall { get; private set; }
        public StaticBody2D ExplosionWall { get; private set; }
        public PlayerTarget BehindWall { get; private set; }
        public PlayerTarget FrontPlayer { get; private set; }
        public PlayerTarget EmbeddedPlayer { get; private set; }
        public PlayerTarget OverlappingPlayer { get; private set; }
        public PlayerTarget VisibleExplosionPlayer { get; private set; }
        public PlayerTarget BlockedExplosionPlayer { get; private set; }
        public PlayerTarget OutsideCirclePlayer { get; private set; }
        public PlayerTarget AfterFakePlayer { get; private set; }

        public NativeFixture(SceneTree tree)
        {
            _tree = tree;
            _world = new World2D();
            _viewport = new SubViewport
            {
                Name = "RangedGeometryOwnedNativeWorld", Size = new Vector2I(32, 32),
                World2D = _world, RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            _scope = new Node2D { Name = "ActualPhysicsFixtures" };
            _viewport.AddChild(_scope);
            tree.Root.AddChild(_viewport);
        }

        public void Populate()
        {
            FirstWall = Wall("ThinWorldFirst", new Vector2(80f, 50f), new Vector2(2f, 40f));
            FakeArea("WorldLayerAreaIsNotTerrain", new Vector2(50f, 50f), CombatCollisionLayers.World);
            BehindWall = Player("BehindThinWall", new Vector2(130f, 50f));
            FrontPlayer = Player("PlayerBeforeWall", new Vector2(80f, 120f));
            Wall("WorldAfterPlayer", new Vector2(130f, 120f), new Vector2(2f, 40f));
            Wall("TieWorld", new Vector2(101f, 190f), new Vector2(2f, 40f));
            Player("TieHurtbox", new Vector2(101f, 190f), new Vector2(2f, 40f));
            Wall("EmbeddedWorld", new Vector2(80f, 260f), new Vector2(2f, 40f));
            EmbeddedPlayer = Player("AlsoOverlappingPlayer", new Vector2(90f, 260f));
            OverlappingPlayer = Player("AlreadyOverlappingPlayer", new Vector2(80f, 330f));
            Wall("BeamRadiusCorner", new Vector2(100f, 408f), new Vector2(2f, 8f));
            Wall("BeamHeadOnThinWall", new Vector2(100f, 470f), new Vector2(2f, 40f));
            ExplosionWall = Wall("ExplosionOccluder", new Vector2(80f, 600f), new Vector2(2f, 60f));
            VisibleExplosionPlayer = Player("VisibleExplosionTarget", new Vector2(50f, 650f));
            BlockedExplosionPlayer = Player("OccludedExplosionTarget", new Vector2(110f, 600f));
            OutsideCirclePlayer = Player("InsideSquareButOutsideCircle", new Vector2(-25f, 675f));
            FakeArea("FakeExplosionPlayerArea", new Vector2(30f, 600f), CombatCollisionLayers.Player);
            PlayerBody("FakeExplosionPlayerBody", new Vector2(35f, 610f));
            FakeArea("FakeNearPlayerArea", new Vector2(50f, 760f), CombatCollisionLayers.Player);
            PlayerBody("FakeNearPlayerBody", new Vector2(65f, 760f));
            AfterFakePlayer = Player("GenuinePlayerAfterFakes", new Vector2(100f, 760f));
        }

        public async Task Sync()
        {
            for (int frame = 0; frame < 3; frame++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            }
        }

        public void AssertNativeFixtureIsLive()
        {
            TestAssert.True(_scope.IsInsideTree() && _scope.GetWorld2D() == _world &&
                _world != _tree.Root.FindWorld2D(), "fixture owns a live independent native World2D, not the main game world");
            using var query = PhysicsRayQueryParameters2D.Create(new Vector2(20f, 50f), new Vector2(220f, 50f), CombatCollisionLayers.World);
            query.CollideWithBodies = true;
            query.CollideWithAreas = false;
            using Godot.Collections.Dictionary hit = Space.IntersectRay(query);
            TestAssert.True(hit.Count > 0 && hit["collider"].AsGodotObject() == FirstWall,
                "native precondition: real two-pixel StaticBody wall is queryable before testing ranged behavior");
            TestAssert.True(BehindWall.Box.IsInsideTree() && BehindWall.Box.ResolveDamageReceiver() == BehindWall.Health &&
                BehindWall.Health.CurrentHealth == 5, "native precondition: actual Hurtbox has initialized real Health");
        }

        private StaticBody2D Wall(string name, Vector2 position, Vector2 size)
        {
            var body = new StaticBody2D
            {
                Name = name, Position = position, CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
            };
            body.AddChild(new CollisionShape2D { Shape = Own(new RectangleShape2D { Size = size }) });
            _scope.AddChild(body);
            return body;
        }

        private PlayerTarget Player(string name, Vector2 position, Vector2? rectangleSize = null)
        {
            var owner = new Node2D { Name = name, Position = position };
            var health = new HealthComponent { Name = "Health", MaxHealth = 5, InvulnerabilitySeconds = 0f };
            var hurtbox = new Hurtbox
            {
                Name = "Hurtbox", CollisionLayer = CombatCollisionLayers.Player, CollisionMask = 0,
                Monitoring = false, Monitorable = true, DamageReceiverPath = new NodePath("../Health")
            };
            Shape2D shape = rectangleSize.HasValue
                ? Own(new RectangleShape2D { Size = rectangleSize.Value })
                : Own(new CircleShape2D { Radius = 8f });
            hurtbox.AddChild(new CollisionShape2D { Shape = shape });
            owner.AddChild(health);
            owner.AddChild(hurtbox);
            _scope.AddChild(owner);
            return new PlayerTarget(hurtbox, health);
        }

        private void FakeArea(string name, Vector2 position, uint layer)
        {
            var area = new Area2D
            {
                Name = name, Position = position, CollisionLayer = layer, CollisionMask = 0,
                Monitoring = false, Monitorable = true
            };
            area.AddChild(new CollisionShape2D { Shape = Own(new CircleShape2D { Radius = 8f }) });
            _scope.AddChild(area);
        }

        private void PlayerBody(string name, Vector2 position)
        {
            var body = new StaticBody2D
            {
                Name = name, Position = position, CollisionLayer = CombatCollisionLayers.Player, CollisionMask = 0
            };
            body.AddChild(new CollisionShape2D { Shape = Own(new CircleShape2D { Radius = 8f }) });
            _scope.AddChild(body);
        }

        private T Own<T>(T shape) where T : Shape2D
        {
            _ownedShapes.Add(shape);
            return shape;
        }

        public async Task DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            // Queue-free all nodes first so physics references are released;
            // then dispose only the shapes and World2D this utility created.
            if (GodotObject.IsInstanceValid(_viewport)) _viewport.QueueFree();
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            foreach (Shape2D shape in _ownedShapes) shape.Dispose();
            _ownedShapes.Clear();
            _world.Dispose();
        }
    }
}
