using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Enemies.Moth;
using Knight.Enemies.Laborer;
using Knight.Enemies;

namespace Knight.Tests.Integration;

public static class KeyboardClawTargetTests
{
    private static void VerifyGearCounterWindowContract()
    {
        (GraveLampMothState State, AttackPhase Phase, float Remaining, float Safe, float Lead, bool Expected)[] cases = {
            (GraveLampMothState.Telegraph,AttackPhase.Telegraph,.5f,.7f,.3f,true),
            (GraveLampMothState.Align,AttackPhase.None,0f,.5f,.3f,true),
            (GraveLampMothState.Hover,AttackPhase.None,0f,float.PositiveInfinity,.3f,true),
            (GraveLampMothState.Align,AttackPhase.Telegraph,.5f,.7f,.3f,false),
            (GraveLampMothState.Telegraph,AttackPhase.None,0f,.7f,.3f,false),
            (GraveLampMothState.Dive,AttackPhase.Active,.5f,.7f,.3f,false),
            (GraveLampMothState.Recover,AttackPhase.Recovery,.5f,.7f,.3f,false),
            (GraveLampMothState.ThreadStunned,AttackPhase.None,0f,.7f,.3f,false),
            (GraveLampMothState.Dead,AttackPhase.None,0f,.7f,.3f,false),
            (GraveLampMothState.Telegraph,AttackPhase.Telegraph,.3f,.7f,.3f,false),
            (GraveLampMothState.Align,AttackPhase.None,0f,.3f,.3f,false),
            (GraveLampMothState.Align,AttackPhase.None,0f,float.NaN,.3f,false),
            (GraveLampMothState.Align,AttackPhase.None,0f,.7f,float.NaN,false),
            (GraveLampMothState.Align,AttackPhase.None,0f,float.PositiveInfinity,float.PositiveInfinity,false),
            (GraveLampMothState.Align,AttackPhase.None,0f,.7f,-.3f,false),
            (GraveLampMothState.Align,AttackPhase.None,-1f,.7f,.3f,false),
            (GraveLampMothState.Align,AttackPhase.None,.2f,.7f,.3f,false)
        };
        foreach (var sample in cases)
            TestAssert.Equal(sample.Expected,PrologueCanyonGeneratedMainRouteTests.Task10GearMothCastWindow(
                sample.State,sample.Phase,sample.Remaining,sample.Safe,sample.Lead),
                $"GEAR_COUNTER_WINDOW_RED: safe preemptive control and unsafe-state rejection {sample}");
        GD.Print("GEAR_COUNTER_WINDOW_CONTRACT_OK checks=17 controlled_states=Hover,Align,Telegraph rejects_stunned=true");
    }

    public static async Task Run(SceneTree tree)
    {
        VerifyGearCounterWindowContract();
        var warning = new AttackTimelineFrame(2, EnemyAttackKind.RustShellThrust,
            AttackPhase.Telegraph, .65f, .5333333f, Vector2.Zero, true);
        var controlCases = new[]
        {
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning, Safe:.4166666f, Lead:.16f, Want:true),
            (Enabled:false, State:RustShellState.Telegraph, Timeline:warning, Safe:.4166666f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Stunned, Timeline:warning, Safe:.4166666f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning with { AttackId=0 }, Safe:.4166666f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning with { Phase=AttackPhase.Active }, Safe:.4166666f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning with { Kind=EnemyAttackKind.LaborerSweep }, Safe:.4166666f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning with { PhaseRemainingSeconds=.16f }, Safe:.4166666f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning, Safe:.16f, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning, Safe:float.NaN, Lead:.16f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning, Safe:.4166666f, Lead:float.NaN, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning, Safe:.4166666f, Lead:0f, Want:false),
            (Enabled:true, State:RustShellState.Telegraph, Timeline:warning, Safe:float.PositiveInfinity, Lead:.16f, Want:true)
        };
        foreach (var sample in controlCases)
            TestAssert.Equal(sample.Want, PrologueCanyonGeneratedMainRouteTests.Task10LowerBellControlWindow(
                sample.Enabled, sample.State, sample.Timeline, sample.Safe, sample.Lead),
                "LOWER_BELL_CONTROL_WINDOW_RED: preempt only a live warned Rust with all-source lead: " + sample);
        GD.Print("LOWER_BELL_CONTROL_WINDOW_OK checks=12 original_timing=true no_production_change=true");
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
            await NearerEnemyWinsBehindTerrain(tree, root, caster, resolver);
            GD.Print("KEYBOARD_SAME_QUERY_TRACE_OK full_resolution_parity=true saturated_0_1=true query_counters_are_callsite=true whole_route=false");
            GD.Print("KEYBOARD_CLAW_TARGETS_OK");
        }
        finally { root.QueueFree(); await Physics(tree); }
    }

    private static async Task NearerEnemyWinsBehindTerrain(SceneTree tree, Node2D root,
        Node2D caster, MechanicalClawTargetResolver resolver)
    {
        var moth = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn").Instantiate<GraveLampMoth>();
        moth.Name = "NearInteractableEnemy";
        moth.Position = new Vector2(40, -200);
        var slab = Terrain("FarAxisTerrain", new Vector2(0, -300), new Vector2(200, 20), GrappleSurfaceMaterial.TempleMetal);
        root.AddChild(moth);
        moth.SetPhysicsProcess(false);
        root.AddChild(slab);
        try
        {
            await Physics(tree);
            var target = moth.GetNode<ThreadTarget2D>("ThreadTarget");
            var adapter = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            Vector2 velocity = moth.Velocity;
            int applications = adapter.ApplicationCount;
            var selection = resolver.ResolveKeyboard(caster, Vector2.Up);
            GD.Print($"KEYBOARD_NEAR_ENEMY_MEASURE selected={selection.Surface?.GetPath()} anchor={selection.Snapshot.WorldAnchor} enemy={target.WorldAnchor} available={target.IsAvailable} distance={caster.GlobalPosition.DistanceTo(target.WorldAnchor)} applications={adapter.ApplicationCount} human=false");
            TestAssert.True(target.IsAvailable && adapter.ApplicationCount == applications && moth.Velocity.IsEqualApprox(velocity),
                "native selection reads real eligible enemy without applying pull or adapter");
            TestAssert.Equal(target, selection.Surface,
                "KEYBOARD_NEAR_ENEMY_RED nearer visible interactable enemy beats farther zero-angle terrain");
            VerifySameQueryTrace(caster,Vector2.Up,resolver,selection);
            await NearEnemyBoundaries(tree, root, caster, resolver, moth, slab);
            GD.Print("KEYBOARD_NEAR_ENEMY_PRIORITY_OK actual_saved_enemy=true native_world=true no_interaction_probe=true human=false");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(moth)) moth.QueueFree();
            if (GodotObject.IsInstanceValid(slab)) slab.QueueFree();
            await Physics(tree);
        }
    }

    private static async Task NearEnemyBoundaries(SceneTree tree, Node2D root, Node2D caster,
        MechanicalClawTargetResolver resolver, GraveLampMoth moth, StaticBody2D slab)
    {
        var target = moth.GetNode<ThreadTarget2D>("ThreadTarget");
        var adapter = target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
        var terrain = slab.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        int checks = 0;
        void Expect(GrappleSurface2D expected, string contract)
        {
            var ordinary = resolver.ResolveKeyboard(caster, Vector2.Up);
            TestAssert.Equal(expected, ordinary.Surface, contract);
            VerifySameQueryTrace(caster,Vector2.Up,resolver,ordinary);
            TestAssert.Equal(0, adapter.ApplicationCount, "selection never probes application: " + contract);
            checks++;
        }
        target.Material = GrappleSurfaceMaterial.Ice; Expect(terrain, "ice enemy cannot gain promotion");
        target.Material = GrappleSurfaceMaterial.Forbidden; Expect(terrain, "forbidden enemy cannot gain promotion");
        target.Material = GrappleSurfaceMaterial.DedicatedAnchor;
        moth.Position = new Vector2(150, -200); await Physics(tree); Expect(terrain, "near but outside22.5 cone remains excluded");
        moth.Position = new Vector2(150, -700); await Physics(tree); Expect(terrain, "beyond620 remains excluded");
        moth.Position = new Vector2(40, -200); await Physics(tree);
        target.RemoveFromGroup(ThreadTarget2D.TargetGroup); Expect(terrain, "hidden thread-group target cannot steal selection");
        target.AddToGroup(ThreadTarget2D.TargetGroup);
        target.RemoveFromGroup(GrappleSurface2D.TargetGroup); Expect(terrain, "unregistered grapple surface cannot be promoted");
        target.AddToGroup(GrappleSurface2D.TargetGroup);
        NodePath receiverPath = target.PullReceiverPath;
        target.PullReceiverPath = new NodePath("../MissingReceiver"); Expect(terrain, "missing receiver cannot be promoted");
        target.PullReceiverPath = new NodePath("."); Expect(terrain, "unsupported receiver cannot be promoted");
        target.PullReceiverPath = receiverPath;
        target.RemoveChild(adapter); Expect(terrain, "enemy without dispatch adapter cannot gain priority");
        target.AddChild(adapter);
        float speed = adapter.EnemyPullSpeed;
        adapter.EnemyPullSpeed = 0f; Expect(terrain, "zero-speed adapter cannot dispatch promotion");
        adapter.EnemyPullSpeed = float.NaN; Expect(terrain, "nonfinite adapter cannot dispatch promotion");
        adapter.EnemyPullSpeed = speed;
        NodePath adapterPath = adapter.TargetPath;
        adapter.TargetPath = new NodePath("../.."); Expect(terrain, "adapter pointing to wrong node cannot gain priority");
        adapter.TargetPath = adapterPath;

        var blocker = Terrain("EnemyOnlyOccluder", new Vector2(20, -100), new Vector2(10, 20), null);
        root.AddChild(blocker);
        try { await Physics(tree); Expect(terrain, "real wall hides enemy but not the axis terrain"); }
        finally { blocker.QueueFree(); await Physics(tree); }
        var mechanism = new ThreadTarget2D { Name = "DeclaredMechanismWithoutAdapter", Position = new Vector2(0, -280),
            Kind = ThreadTargetKind.Mechanism };
        root.AddChild(mechanism);
        try { await Physics(tree); Expect(mechanism, "explicit Mechanism remains protected even without adapter"); }
        finally { mechanism.QueueFree(); await Physics(tree); }
        var pointAnchor = Point("FarMovementAnchor", new Vector2(0, -280));
        root.AddChild(pointAnchor);
        try { await Physics(tree); Expect(target, "ordinary movement point anchor follows disclosed nearer-enemy rule"); }
        finally { pointAnchor.QueueFree(); await Physics(tree); }

        // Narrow axis terrain leaves the oblique farther-enemy LOS genuinely clear;
        // its rejection must be distance, not a wall hiding it.
        ((RectangleShape2D)slab.GetChild<CollisionShape2D>(0).Shape).Size = new Vector2(10, 20);
        moth.Position = new Vector2(100, -400); await Physics(tree);
        TestAssert.True(resolver.SnapshotLocked(caster, target, target.WorldAnchor, Vector2.Down).HasLineOfSight,
            "farther-enemy distance control has actual clear LOS");
        Expect(terrain, "visible enemy behind nearer terrain must not be promoted");
        moth.Position = new Vector2(40, -Mathf.Sqrt(290f * 290f - 40f * 40f)); await Physics(tree);
        TestAssert.Near(290f, caster.GlobalPosition.DistanceTo(target.WorldAnchor), .001f, "equal-distance fixture hand-derived290");
        Expect(terrain, "equal-distance enemy does not gain strictly-nearer priority");
        moth.Position = new Vector2(40, -285); await Physics(tree); Expect(target, "strictly closer visible enemy does gain priority");
        moth.Position = new Vector2(40, -200); await Physics(tree);
        var second = GD.Load<PackedScene>("res://scenes/enemies/GraveLampMoth.tscn").Instantiate<GraveLampMoth>();
        second.Name = "AA_SymmetricEnemy"; second.Position = new Vector2(20, -400);
        root.AddChild(second); second.SetPhysicsProcess(false);
        try
        {
            await Physics(tree);
            var other = second.GetNode<ThreadTarget2D>("ThreadTarget");
            TestAssert.True(resolver.SnapshotLocked(caster, other, other.WorldAnchor, Vector2.Down).HasLineOfSight,
                "best-angle but farther enemy in full candidate-pool control is visible");
            Expect(target, "farther best-angle enemy must not erase another closer eligible enemy");
            second.Position = new Vector2(-40, -200); await Physics(tree);
            for (int repeat = 0; repeat < 8; repeat++) Expect(other, "equal enemy angle/distance uses stable path despite later allocation");
            TestAssert.Equal(0, other.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter").ApplicationCount,
                "second enemy was not used as an interaction probe");
        }
        finally { second.QueueFree(); await Physics(tree); }
        moth.Position = new Vector2(2000, 2000); await Physics(tree);
        var heavy = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn").Instantiate<BellBackLaborer>();
        heavy.Name = "ActualHeavyEnemy"; heavy.Position = new Vector2(40, -200);
        root.AddChild(heavy); heavy.SetPhysicsProcess(false);
        try
        {
            await Physics(tree);
            var heavyTarget = heavy.GetNode<ThreadTarget2D>("ThreadTarget");
            Expect(heavyTarget, "actual saved HeavyEnemy remains supported");
            TestAssert.Equal(0, heavyTarget.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter").ApplicationCount,
                "heavy enemy was not used as interaction probe");
        }
        finally { heavy.QueueFree(); await Physics(tree); }
        moth.Position = new Vector2(40, -200); await Physics(tree);
        moth.GetNode<HealthComponent>("Health").ApplyDamage(new DamageRequest(99, Vector2.Zero, 0, 99142));
        Expect(terrain, "dead real enemy cannot gain promotion");
        GD.Print($"KEYBOARD_NEAR_ENEMY_BOUNDARIES_OK checks={checks} visible_distance=true full_pool=true original_limits=true human=false");
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

    private static void VerifySameQueryTrace(Node2D caster,Vector2 direction,
        MechanicalClawTargetResolver resolver,MechanicalClawTargetResolution ordinary)
    {
        TestAssert.Equal(ordinary,resolver.ResolveKeyboard(caster,direction,null),
            "KEYBOARD_TRACE explicit null preserves every original resolution field");
        var full = new MechanicalClawKeyboardTrace(128);
        TestAssert.Equal(ordinary,resolver.ResolveKeyboard(caster,direction,full),
            "KEYBOARD_TRACE full buffer preserves every original resolution field");
        TestAssert.True(full.ConeQueries == 33 && full.Count > 0 && full.Dropped == 0 && full.Faults == 0,
            "KEYBOARD_TRACE_RED same-call bounded receipts include all original33cone queries");
        bool resultSeen = false;
        for (int row=0; row<full.Count; row++)
            resultSeen |= full.Read(row).Stage == KeyboardClawTraceStage.Result &&
                full.Read(row).Surface == ordinary.Snapshot.RuntimeId;
        TestAssert.True(resultSeen,"KEYBOARD_TRACE actual returned identity is recorded after final promotion");
        foreach (int capacity in new[] { 0, 1 })
        {
            var saturated = new MechanicalClawKeyboardTrace(capacity);
            TestAssert.Equal(ordinary,resolver.ResolveKeyboard(caster,direction,saturated),
                "KEYBOARD_TRACE saturation never changes actual native selection");
            TestAssert.True(saturated.ConeQueries == 33 && saturated.LosQueries == full.LosQueries &&
                saturated.Dropped > 0 && saturated.Faults == 0,
                "KEYBOARD_TRACE query counters remain independent of saturated record capacity");
            TestAssert.Equal(ordinary,resolver.ResolveKeyboard(caster,direction,saturated),
                "KEYBOARD_TRACE already-full buffer remains passive");
            TestAssert.True(saturated.ConeQueries == 66 && saturated.LosQueries == 2*full.LosQueries,
                "KEYBOARD_TRACE already-full invocation still completes original query calls");
        }
        GD.Print($"KEYBOARD_SAME_QUERY_TRACE_SAMPLE rays={full.ConeQueries} los={full.LosQueries} " +
            "full_resolution_parity=true saturated_0_1=true query_counters_are_callsite=true whole_route=false");
    }

    private static async Task Physics(SceneTree tree)
    {
        for (int i=0; i<3; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }
}
