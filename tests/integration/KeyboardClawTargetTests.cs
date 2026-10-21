using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;

namespace Knight.Tests.Integration;

public static class KeyboardClawTargetTests
{
    public static async Task Run(SceneTree tree)
    {
        var root = new Node2D { Name = "KeyboardClawTargets" };
        var caster = new Node2D { Name = "Caster" };
        root.AddChild(caster);
        var ice = Point("IceInFront", new Vector2(80, 0), GrappleSurfaceMaterial.Ice);
        var aligned = Point("ValidAligned", new Vector2(220, 0));
        var nearer = Point("NearerButOffAxis", new Vector2(140, 28));
        root.AddChild(ice); root.AddChild(aligned); root.AddChild(nearer);
        var otherWorld = new SubViewport { Name = "UnrelatedWorld", World2D = new World2D() };
        otherWorld.AddChild(Point("GhostTarget", new Vector2(100,0)));
        root.AddChild(otherWorld);
        tree.Root.AddChild(root);
        var resolver = new MechanicalClawTargetResolver();
        try
        {
            await Physics(tree);
            // Ice is non-colliding scenery in this fixture: material filtering precedes ranking.
            TestAssert.Equal(aligned, resolver.ResolveKeyboard(caster, Vector2.Right).Surface,
                "keyboard cone chooses same-world valid aligned target, not ice, another viewport or mouse endpoint");
            for (int repeat = 0; repeat < 8; repeat++)
                TestAssert.Equal(aligned, resolver.ResolveKeyboard(caster, Vector2.Right).Surface,
                    "same direction gives stable preview");
            aligned.QueueFree(); nearer.QueueFree(); ice.QueueFree(); await Physics(tree);
            var atLimit = Point("At620", new Vector2(620,0)); root.AddChild(atLimit); await Physics(tree);
            TestAssert.Equal(atLimit, resolver.ResolveKeyboard(caster, Vector2.Right).Surface, "620 range inclusive");
            atLimit.Position = new Vector2(621,0); await Physics(tree);
            TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Right).Surface == null, "over 620 rejected");
            atLimit.QueueFree(); await Physics(tree);
            var hidden = Point("BehindWall", new Vector2(200,0)); root.AddChild(hidden);
            var blocker = Terrain("UngrappleableWall", new Vector2(100,0), new Vector2(20,160), null);
            root.AddChild(blocker); await Physics(tree);
            TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Right).Surface == null, "solid wall blocks point");
            hidden.QueueFree(); blocker.QueueFree(); await Physics(tree);
            var terrain = Terrain("GrippableWall", new Vector2(200,0), new Vector2(30,160), GrappleSurfaceMaterial.TempleMetal);
            root.AddChild(terrain); await Physics(tree);
            var hit = resolver.ResolveKeyboard(caster, Vector2.Right);
            TestAssert.Equal(terrain.GetNode<GrappleSurface2D>("GrappleSurfaceProfile"), hit.Surface,
                "keyboard finds real terrain rather than profile anchor");
            TestAssert.Near(185f, hit.Snapshot.WorldAnchor.X, .1f, "terrain uses real collision hit");
            TestAssert.True(hit.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Left), "wall normal preserved");
            hit.Surface.Material = GrappleSurfaceMaterial.Ice;
            TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Right).Surface == null, "icy wall cannot be gripped");
            terrain.QueueFree(); await Physics(tree);
            var first = Point("A", new Vector2(200,20)); var second = Point("B", new Vector2(200,-20));
            root.AddChild(second); root.AddChild(first); await Physics(tree);
            TestAssert.Equal(first, resolver.ResolveKeyboard(caster, Vector2.Right).Surface,
                "equal angle and distance tie breaks by authored tree path, not allocation order");
            first.QueueFree(); second.QueueFree(); await Physics(tree);
            var outside = Point("OutsideCone", new Vector2(150,90)); root.AddChild(outside); await Physics(tree);
            TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Right).Surface == null, "outside 22.5 cone rejected");
            outside.QueueFree(); await Physics(tree);
            await LiveCasterExclusionsFollowSubtreeChanges(tree, root, caster, resolver);
            GD.Print("KEYBOARD_CLAW_TARGETS_OK");
        }
        finally { root.QueueFree(); await Physics(tree); }
    }

    private static async Task LiveCasterExclusionsFollowSubtreeChanges(SceneTree tree, Node2D root,
        Node2D caster, MechanicalClawTargetResolver resolver)
    {
        var branch = new Node2D { Name = "NestedCasterBranch" };
        var leaf = new Node2D { Name = "NestedCasterLeaf" };
        caster.AddChild(branch); branch.AddChild(leaf);
        var secondCaster = new Node2D { Name = "SecondCaster" };
        root.AddChild(secondCaster);
        var target = Point("ExclusionTarget", new Vector2(220, 0));
        var targetArea = new Area2D { CollisionLayer = CombatCollisionLayers.ThreadTarget };
        targetArea.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 12f } });
        target.AddChild(targetArea);
        root.AddChild(target);
        var blocker = Terrain("NewCasterCollider", new Vector2(100, 0), new Vector2(20, 160), null);
        leaf.AddChild(blocker);
        await Physics(tree);
        TestAssert.Equal(target, resolver.ResolveKeyboard(caster, Vector2.Right).Surface,
            "new nested caster collision body is excluded from both point LOS and cone queries");
        TestAssert.True(resolver.ResolveKeyboard(secondCaster, Vector2.Right).Surface == null,
            "same resolver excludes only the current caster subtree, not the previous caster's body");
        AssertOtherQueries(caster, visible: true);
        AssertOtherQueries(secondCaster, visible: false);

        // The collider is already registered in physics. Change ownership without
        // yielding: the very next query must rebuild exclusions from the live tree.
        blocker.Reparent(secondCaster, true);
        TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Right).Surface == null,
            "same-frame reparent out of caster immediately stops excluding the blocker");
        TestAssert.Equal(target, resolver.ResolveKeyboard(secondCaster, Vector2.Right).Surface,
            "same-frame reparent into another caster immediately excludes that blocker there");
        AssertOtherQueries(caster, visible: false);
        AssertOtherQueries(secondCaster, visible: true);
        blocker.Reparent(leaf, true);
        TestAssert.Equal(target, resolver.ResolveKeyboard(caster, Vector2.Right).Surface,
            "repeated same-frame reparent back to a nested caster immediately restores exclusion");
        blocker.Free();
        TestAssert.Equal(target, resolver.ResolveKeyboard(secondCaster, Vector2.Right).Surface,
            "freed previous caster collider does not survive in shared resolver exclusions");
        AssertOtherQueries(secondCaster, visible: true);

        // GetChildren() previously omitted internal children. Indexed traversal must
        // keep that rule, not silently make internal scenery transparent to the claw.
        var internalBlocker = Terrain("InternalCasterCollider", new Vector2(100, 0), new Vector2(20, 160), null);
        leaf.AddChild(internalBlocker, false, Node.InternalMode.Front);
        await Physics(tree);
        TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Right).Surface == null,
            "internal collider remains outside the public child traversal and blocks the target");
        AssertOtherQueries(caster, visible: false);
        internalBlocker.Free();
        TestAssert.Equal(target, resolver.ResolveKeyboard(caster, Vector2.Right).Surface,
            "freeing an internal collider leaves no stale exclusion or blocked target");
        AssertOtherQueries(caster, visible: true);
        TestAssert.True(resolver.ResolveKeyboard(caster, Vector2.Zero).Surface == null &&
            resolver.ResolveKeyboard(caster, new Vector2(float.NaN, 0)).Surface == null,
            "zero and nonfinite directions remain rejected after repeated valid queries");
        secondCaster.QueueFree(); branch.QueueFree(); target.QueueFree();
        await Physics(tree);

        void AssertOtherQueries(Node2D currentCaster, bool visible)
        {
            MechanicalClawTargetResolution mouse = resolver.Resolve(currentCaster, target.WorldAnchor);
            TestAssert.Equal(target, mouse.Surface, "mouse assist still selects the actual point surface");
            TestAssert.Equal(visible, mouse.Snapshot.HasLineOfSight,
                "mouse admission uses the same live exclusion subtree without changing cursor ranking");
            bool occluded = MechanicalClawTargetResolver.TryGetLineOcclusion(currentCaster, target,
                currentCaster.GlobalPosition, target.WorldAnchor, out _, out _);
            TestAssert.Equal(!visible, occluded,
                "presentation occlusion follows the live subtree and preserves its separate inside-hit policy");
        }
    }

    private static GrappleSurface2D Point(string name, Vector2 position,
        GrappleSurfaceMaterial material = GrappleSurfaceMaterial.DedicatedAnchor) =>
        new() { Name = name, Position = position, Material = material };

    private static StaticBody2D Terrain(string name, Vector2 position, Vector2 size, GrappleSurfaceMaterial? material)
    {
        var body = new StaticBody2D { Name = name, Position = position, CollisionLayer = CombatCollisionLayers.World };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
        if (material.HasValue) body.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = material.Value, UseCollisionHitPoint = true, AnchorOffset = new Vector2(999,999) });
        return body;
    }

    private static async Task Physics(SceneTree tree)
    {
        for (int i=0; i<3; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}
