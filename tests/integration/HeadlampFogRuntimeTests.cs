using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

public static class HeadlampFogRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await ActualLanternWorldPose(tree);
        await ExactLiveWorldOcclusion(tree);
    }

    private static async Task ActualLanternWorldPose(SceneTree tree)
    {
        var fixture = new Node2D
        {
            Name = "HeadlampPoseFixture",
            Position = new Vector2(310f, 470f),
            Rotation = .23f,
            Scale = new Vector2(1.25f, .8f)
        };
        PlayerFormalVisual formal = GD.Load<PackedScene>(
            "res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
        formal.Position = new Vector2(10f, -5f);
        fixture.AddChild(formal);
        tree.Root.AddChild(fixture);
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Node2D lantern = formal.GetNode<Node2D>("Lantern");
            TestAssert.True(lantern.Position.Length() > 20f,
                "real authored lantern has a nontrivial offset from the rig origin");
            foreach (int facing in new[] { 1, -1 })
            {
                formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Move, facing, true));
                AssertSamePoint(lantern.GlobalPosition, formal.LanternWorldPosition,
                    "lamp origin follows the actual translated/scaled/mirrored Lantern node");
                Vector2 before = formal.LanternWorldPosition;
                lantern.Position += new Vector2(7f, -9f);
                TestAssert.True(before.DistanceTo(lantern.GlobalPosition) > 5f,
                    "fixture really moves the lantern independently of the rig/body origin");
                AssertSamePoint(lantern.GlobalPosition, formal.LanternWorldPosition,
                    "lamp origin reads the current node pose, not a cached/rest/body offset");
            }
        }
        finally
        {
            fixture.GetParent()?.RemoveChild(fixture);
            fixture.Free();
        }
        GD.Print("HEADLAMP_ACTUAL_LANTERN_POSE_OK");
    }

    private static void AssertSamePoint(Vector2 expected, Vector2 actual, string message) =>
        TestAssert.True(float.IsFinite(actual.X) && float.IsFinite(actual.Y) &&
            expected.DistanceTo(actual) <= .0001f, $"{message}; expected={expected} actual={actual}");

    private static async Task ExactLiveWorldOcclusion(SceneTree tree)
    {
        var fixture = new Node2D { Name = "HeadlampOcclusionFixture" };
        StaticBody2D thinFloor = Wall(new Rect2(3840f, 3584f, 160f, 16f));
        fixture.AddChild(thinFloor);
        tree.Root.AddChild(fixture);
        using var sampler = new HeadlampOcclusionSampler(fixture);
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            HeadlampOcclusionFrame first = sampler.Capture();
            TestAssert.True(first.IsComplete && first.Shapes.Count == 1 && first.PhysicsQueries == 0,
                "snapshot contains the real enabled World rectangle without physics query inflation");
            Vector2 nearOrigin = new(3900f, 3548f), nearBehind = new(4021.7674f, 3593.527f);
            TestAssert.True(nearOrigin.DistanceTo(nearBehind) < 180f && !first.CanSee(nearOrigin, nearBehind),
                "actual 16px LiftEntryFloor blocks the near-clear circle between sparse contour rays");

            CollisionShape2D thinShape = thinFloor.GetNode<CollisionShape2D>("Collision");
            thinShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
            TestAssert.True(!thinShape.Disabled && !sampler.Capture().CanSee(nearOrigin, nearBehind),
                "a queued door opening cannot reveal the next room before collision Disabled commits");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            HeadlampOcclusionFrame open = sampler.Capture();
            TestAssert.True(open.IsComplete && open.Shapes.Count == 0 && open.CanSee(nearOrigin, nearBehind),
                "actual committed Disabled flag opens visibility, not just a logical door flag");
            TestAssert.True(!first.CanSee(nearOrigin, nearBehind),
                "old frame is an immutable geometry snapshot, not a live mutable collider reference");

            StaticBody2D post = Wall(new Rect2(4748f, 1160f, 36f, 60f));
            fixture.AddChild(post);
            HeadlampOcclusionFrame gap = sampler.Capture();
            Vector2 farOrigin = new(4186f, 1130f), farBehind = new(4790f, 1190f);
            TestAssert.True(gap.CanSee(farOrigin, farOrigin + Vector2.Right * 620f) &&
                gap.CanSee(farOrigin, farOrigin + Vector2.FromAngle(Mathf.DegToRad(11.25f)) * 620f) &&
                !gap.CanSee(farOrigin, farBehind) && farOrigin.DistanceTo(farBehind) < 620f,
                "actual final-catch post entirely between 32-ray angles still blocks the finite beam");

            post.CollisionLayer = CombatCollisionLayers.Enemy;
            TestAssert.True(sampler.Capture().CanSee(farOrigin, farBehind),
                "enemy-only collision is not a fog wall");
            post.CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
            TestAssert.True(!sampler.Capture().CanSee(farOrigin, farBehind),
                "World bit still blocks when combined with ThreadTarget");
            post.ProcessMode = Node.ProcessModeEnum.Disabled;
            TestAssert.True(sampler.Capture().CanSee(farOrigin, farBehind),
                "a disabled Remove-mode body is not in the physics simulation");
            post.DisableMode = CollisionObject2D.DisableModeEnum.KeepActive;
            TestAssert.True(!sampler.Capture().CanSee(farOrigin, farBehind),
                "KeepActive-mode World bodies remain solid despite disabled processing");
            post.ProcessMode = Node.ProcessModeEnum.Inherit;
            post.DisableMode = CollisionObject2D.DisableModeEnum.Remove;
            fixture.RemoveChild(post);
            TestAssert.True(sampler.Capture().CanSee(farOrigin, farBehind),
                "detached but not yet freed retired sector geometry cannot keep a stale shadow");
            post.Free();

            StaticBody2D rotated = Wall(new Rect2(-5f, -40f, 10f, 80f));
            rotated.Position = new Vector2(30f, 10f);
            rotated.Rotation = Mathf.Pi / 4f;
            rotated.Scale = new Vector2(1.25f, .8f);
            CollisionShape2D rotatedShape = rotated.GetNode<CollisionShape2D>("Collision");
            rotatedShape.Position += new Vector2(4f, 7f);
            fixture.AddChild(rotated);
            Transform2D transform = rotatedShape.GlobalTransform;
            HeadlampOcclusionFrame transformed = sampler.Capture();
            TestAssert.True(!transformed.CanSee(transform * new Vector2(-30f, 0f), transform * new Vector2(30f, 0f)) &&
                transformed.CanSee(transform * new Vector2(-30f, 50f), transform * new Vector2(30f, 50f)),
                "full collider-local affine rectangle handles rotation, scale and local offsets without an AABB shadow");
            TestAssert.True(!transformed.CanSee(transform * Vector2.Zero, transform * new Vector2(100f, 0f)),
                "an origin inside a ceiling cannot emit light out of the solid");
            rotated.Position += new Vector2(1000f, 0f);
            TestAssert.True(sampler.Capture().CanSee(transform * new Vector2(-30f, 0f), transform * new Vector2(30f, 0f)),
                "moving lift/pendulum geometry is read again instead of caching a static world rectangle");

            var area = new Area2D { CollisionLayer = CombatCollisionLayers.World };
            area.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(400f, 400f) } });
            fixture.AddChild(area);
            TestAssert.True(sampler.Capture().CanSee(new Vector2(-100f, 0f), new Vector2(100f, 0f)),
                "an Area trigger is not a solid World PhysicsBody occluder");

            StaticBody2D partition = Wall(new Rect2(20f, -50f, 4f, 100f));
            fixture.AddChild(partition);
            HeadlampOcclusionFrame twoOrigins = sampler.Capture();
            TestAssert.True(!twoOrigins.CanSee(Vector2.Zero, new Vector2(80f, 0f)) &&
                twoOrigins.CanSee(new Vector2(0f, -80f), new Vector2(80f, 0f)),
                "actor circle and actual displaced lamp have independent visibility across a real partition");
            partition.GetNode<CollisionShape2D>("Collision").Disabled = true;

            var staged = new Node2D();
            staged.SetMeta("_seamless_staged", true);
            staged.AddChild(Wall(new Rect2(30f, -20f, 5f, 40f)));
            fixture.AddChild(staged);
            TestAssert.True(sampler.Capture().CanSee(Vector2.Zero, new Vector2(80f, 0f)),
                "staged streaming geometry does not cast shadows before its activation transaction");
            staged.RemoveMeta("_seamless_staged");
            TestAssert.True(!sampler.Capture().CanSee(Vector2.Zero, new Vector2(80f, 0f)),
                "activation refresh includes existing candidate geometry without a second NodeAdded event");
            fixture.RemoveChild(staged);
            staged.Free();

            var nativeBody = new StaticBody2D
            {
                Position = new Vector2(3000f, 3000f),
                CollisionLayer = CombatCollisionLayers.World,
                Rotation = .2f
            };
            fixture.AddChild(nativeBody);
            uint owner = nativeBody.CreateShapeOwner(nativeBody);
            var ownerTransform = new Transform2D(.4f, new Vector2(12f, -8f));
            nativeBody.ShapeOwnerSetTransform(owner, ownerTransform);
            nativeBody.ShapeOwnerAddShape(owner, new RectangleShape2D { Size = new Vector2(16f, 120f) });
            Transform2D nativeTransform = nativeBody.GlobalTransform * ownerTransform;
            Vector2 nativeStart = nativeTransform * new Vector2(-40f, 0f);
            Vector2 nativeEnd = nativeTransform * new Vector2(40f, 0f);
            TestAssert.True(nativeBody.GetChildCount() == 0 && nativeBody.ShapeOwnerGetShapeCount(owner) == 1 &&
                !sampler.Capture().CanSee(nativeStart, nativeEnd),
                "real native shape-owner geometry blocks even without a CollisionShape2D child node");
            nativeBody.ShapeOwnerSetDisabled(owner, true);
            TestAssert.True(sampler.Capture().CanSee(nativeStart, nativeEnd),
                "native owner Disabled state is authoritative for non-node shapes too");
            nativeBody.ShapeOwnerSetDisabled(owner, false);
            nativeBody.ShapeOwnerSetTransform(owner, new Transform2D(.4f, new Vector2(512f, -8f)));
            TestAssert.True(sampler.Capture().CanSee(nativeStart, nativeEnd),
                "native owner transform changes are read again without a node event");
            nativeBody.RemoveShapeOwner(owner);
            TestAssert.True(sampler.Capture().IsComplete && sampler.Capture().CanSee(nativeStart, nativeEnd),
                "removing a native owner leaves no stale geometry");
            uint replacement = nativeBody.CreateShapeOwner(nativeBody);
            nativeBody.ShapeOwnerSetTransform(replacement, ownerTransform);
            nativeBody.ShapeOwnerAddShape(replacement, new RectangleShape2D { Size = new Vector2(16f, 120f) });
            using (var bodyScope = new HeadlampOcclusionSampler(nativeBody))
            {
                TestAssert.True(!bodyScope.Capture().CanSee(nativeStart, nativeEnd),
                    "scope itself can be a World body; self is not incorrectly excluded by ancestry");
                tree.Root.RemoveChild(fixture);
                TestAssert.True(!sampler.Capture().IsComplete && !bodyScope.Capture().IsComplete,
                    "a detached scope cannot authorize lighting from its old geometry");
                tree.Root.AddChild(fixture);
                TestAssert.True(!sampler.Capture().CanSee(nativeStart, nativeEnd) &&
                    !bodyScope.Capture().CanSee(nativeStart, nativeEnd),
                    "same-tree scope re-entry rebuilds native body candidates without replacing the samplers");
            }
            nativeBody.RemoveShapeOwner(replacement);

            var internalRoot = new Node2D();
            internalRoot.AddChild(Wall(new Rect2(8000f, 8000f, 10f, 100f)), false, Node.InternalMode.Front);
            fixture.AddChild(internalRoot);
            using (var internalSampler = new HeadlampOcclusionSampler(internalRoot))
                TestAssert.True(!internalSampler.Capture().CanSee(new Vector2(7990f, 8050f), new Vector2(8020f, 8050f)),
                    "initial indexing includes already-present internal World bodies");
            fixture.RemoveChild(internalRoot);
            internalRoot.Free();

            rotatedShape.Shape = new CircleShape2D { Radius = 10f };
            HeadlampOcclusionFrame unsupported = sampler.Capture();
            TestAssert.True(!unsupported.IsComplete && !unsupported.CanSee(Vector2.Zero, Vector2.One),
                "unsupported active World shapes fail closed instead of silently disappearing");
            rotatedShape.Shape = new RectangleShape2D { Size = new Vector2(10f, 80f) };
            StaticBody2D extra = Wall(new Rect2(300f, 300f, 10f, 10f));
            fixture.AddChild(extra);
            using var limited = new HeadlampOcclusionSampler(fixture, capacity: 1);
            HeadlampOcclusionFrame overflow = limited.Capture();
            TestAssert.True(!overflow.IsComplete && !overflow.CanSee(Vector2.Zero, Vector2.One),
                "geometry capacity overflow is explicit and fail-closed, never a silently truncated shadow list");
            HeadlampOcclusionFrame complete = sampler.Capture();
            TestAssert.True(complete.IsComplete && !complete.CanSee(new Vector2(float.NaN, 0f), Vector2.One),
                "nonfinite origins cannot authorize clear fog");
            TestAssert.True(!complete.CanSee(Vector2.Zero, new Vector2(float.PositiveInfinity, 0f)),
                "nonfinite shaded points cannot authorize light");
        }
        finally
        {
            fixture.GetParent()?.RemoveChild(fixture);
            fixture.Free();
        }
        GD.Print("HEADLAMP_EXACT_LIVE_WORLD_OCCLUSION_OK physics_queries=0");
    }

    private static StaticBody2D Wall(Rect2 bounds)
    {
        var body = new StaticBody2D { Position = bounds.GetCenter(), CollisionLayer = CombatCollisionLayers.World };
        body.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = bounds.Size }
        });
        return body;
    }
}
