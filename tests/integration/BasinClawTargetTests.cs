using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Saved V2 terrain + actual keyboard/controller/physics. Pre-tree staging is
// not a title-to-exit route, human input certificate or long-fight clearance.
public static class BasinClawTargetTests
{
    private const string WallPath = "Gameplay/Collision/GearPistonOne";

    public static async Task Run(SceneTree tree)
    {
        RunSavedCapsuleControls();
        await RunFirstAscentLayout(tree, taller: false);
        await RunFirstAscentLayout(tree, taller: true);
        GD.Print("BASIN_ASCENT_LAYOUT_FEASIBILITY_OK historical_control=true saved_candidate=true poses=5 native_capsule=true staged_query_only=true whole_route=false");
        await RunSavedWall(tree, ice: false);
        await RunSavedWall(tree, ice: true);
        GD.Print("BASIN_CLAW_TARGET_RUNTIME_OK layout=2 keyboard=true saved_terrain=true ice_counterexample=true");
    }

    private static CapsuleShape2D ReadSavedBodyCapsule(PackedScene scene)
    {
        TestAssert.True(GodotObject.IsInstanceValid(scene), "saved body requires a live PackedScene");
        // The saved scene/state/shape are borrowed. No actor or script is instantiated.
        SceneState state = scene.GetState();
        TestAssert.True(GodotObject.IsInstanceValid(state) && state.GetBaseSceneState() == null &&
            state.GetNodeCount() > 0, "saved body requires a direct non-inherited saved graph");
        int root = -1, body = -1;
        for (int node = 0; node < state.GetNodeCount(); node++)
        {
            using NodePath path = state.GetNodePath(node);
            string text = path.ToString();
            if (text == ".") { TestAssert.True(root < 0, "one saved root"); root = node; }
            if (text is "BodyCollider" or "./BodyCollider")
            { TestAssert.True(body < 0, "one exact direct saved BodyCollider"); body = node; }
        }
        TestAssert.True(root >= 0 && body >= 0, "saved body requires exact root and direct BodyCollider, not Hurtbox");
        using StringName rootType = state.GetNodeType(root);
        using StringName bodyType = state.GetNodeType(body);
        TestAssert.True(rootType.ToString() == "CharacterBody2D" && bodyType.ToString() == "CollisionShape2D" &&
            state.GetNodeInstance(root) == null && state.GetNodeInstance(body) == null &&
            string.IsNullOrEmpty(state.GetNodeInstancePlaceholder(root)) &&
            string.IsNullOrEmpty(state.GetNodeInstancePlaceholder(body)), "saved body cannot hide instanced/scripted transform behavior");
        CapsuleShape2D owned = null;
        try
        {
            foreach (int node in new[] { root, body })
                for (int property = 0; property < state.GetNodePropertyCount(node); property++)
                {
                    using StringName propertyName = state.GetNodePropertyName(node, property);
                    using Variant value = state.GetNodePropertyValue(node, property);
                    string name = propertyName.ToString();
                    bool identity = name switch
                    {
                        "position" => value.VariantType == Variant.Type.Vector2 && value.AsVector2() == Vector2.Zero,
                        "rotation" or "skew" => value.VariantType is Variant.Type.Float or Variant.Type.Int && value.AsDouble() == 0,
                        "scale" => value.VariantType == Variant.Type.Vector2 && value.AsVector2() == Vector2.One,
                        "transform" => value.VariantType == Variant.Type.Transform2D && value.AsTransform2D() == Transform2D.Identity,
                        "top_level" or "disabled" => value.VariantType == Variant.Type.Bool && !value.AsBool(),
                        "script" => value.VariantType == Variant.Type.Nil || value.VariantType == Variant.Type.Object && value.AsGodotObject() == null,
                        _ => true
                    };
                    TestAssert.True(identity, "saved body root/collider must be enabled, unscripted and identity: " + name);
                    if (node != body || name != "shape") continue;
                    TestAssert.True(owned == null && value.VariantType == Variant.Type.Object &&
                        value.AsGodotObject() is CapsuleShape2D, "one saved Capsule object required");
                    var source = (CapsuleShape2D)value.AsGodotObject();
                    TestAssert.True(float.IsFinite(source.Radius) && source.Radius > 0 && float.IsFinite(source.Height) &&
                        source.Height >= 2 * source.Radius, "saved capsule dimensions finite and physical");
                    owned = (CapsuleShape2D)source.Duplicate();
                }
            TestAssert.True(GodotObject.IsInstanceValid(owned), "saved body shape is not missing");
            return owned;
        }
        catch { owned?.Dispose(); throw; }
    }

    private static void RunSavedCapsuleControls()
    {
        foreach (string kind in new[] { "disabled", "position", "rotation", "scale", "skew", "root-position",
            "missing", "null-shape", "rectangle", "nested", "wrong-root" })
        {
            Node2D graph = kind == "wrong-root" ? new Node2D { Name = "Player" } : new CharacterBody2D { Name = "Player" };
            using Shape2D shape = kind == "rectangle" ? new RectangleShape2D { Size = new(40, 72) }
                : new CapsuleShape2D { Radius = 20, Height = 72 };
            using var packed = new PackedScene();
            try
            {
                if (kind == "root-position") graph.Position = new Vector2(1, 0);
                if (kind != "missing")
                {
                    var body = new CollisionShape2D { Name = "BodyCollider", Shape = kind == "null-shape" ? null : shape };
                    if (kind == "disabled") body.Disabled = true;
                    if (kind == "position") body.Position = new Vector2(1, 0);
                    if (kind == "rotation") body.Rotation = .1f;
                    if (kind == "scale") body.Scale = new Vector2(2, 1);
                    if (kind == "skew") body.Skew = .1f;
                    Node parent = graph;
                    if (kind == "nested")
                    {
                        var nested = new Node2D { Name = "Nested" }; graph.AddChild(nested); nested.Owner = graph; parent = nested;
                    }
                    parent.AddChild(body); body.Owner = graph;
                }
                TestAssert.Equal(Error.Ok, packed.Pack(graph), "native saved-body control packs " + kind);
            }
            finally { graph.Free(); }
            bool rejected = false;
            try { using var unexpected = ReadSavedBodyCapsule(packed); }
            catch (InvalidOperationException) { rejected = true; }
            TestAssert.True(rejected, "SAVED_BODY_READER_RED: saved body must reject " + kind + " rather than assume the capsule is usable");
        }
        var source = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        using var actual = ReadSavedBodyCapsule(source);
        TestAssert.True(actual.Radius == 20 && actual.Height == 72, "actual saved BodyCollider is20/72");
        actual.Radius = 21;
        using var unchanged = ReadSavedBodyCapsule(source);
        TestAssert.True(unchanged.Radius == 20 && unchanged.Height == 72 && unchanged != actual,
            "changing an owned duplicate never mutates the borrowed saved capsule");
        GD.Print("SAVED_BODY_CAPSULE_READ_OK saved_player=true native_controls=11 owned_duplicate=true whole_player_construction=false inherited_fixture=false");
    }

    // A layout experiment, not a simulated jump or a shipping geometry edit.
    // These five literal inputs are from v177 actual Explore samples 12..16.
    private static async Task RunFirstAscentLayout(SceneTree tree, bool taller)
    {
        var root = new Node2D { Name = taller ? "BasinSavedTallGrip" : "BasinHistoricalGripControl" };
        CapsuleShape2D capsule = null;
        RectangleShape2D controlShape = null;
        try
        {
        var basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
            .Instantiate<PrologueSector>();
        root.AddChild(basin);
        basin.Position = new Vector2(1280, 3600);
        var post = basin.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutStep3956Grip");
        var collision = post.GetNode<CollisionShape2D>("Collision");
        var surface = post.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        TestAssert.True(collision.Shape is RectangleShape2D original &&
            original.Size.IsEqualApprox(new Vector2(36, 152)) &&
            (basin.Position + post.Position).IsEqualApprox(new Vector2(4232, 3800)),
            "current saved candidate is the actual newly authored first-ascent post");
        if (!taller)
        {
            // A fresh resource and instance transform, before entering physics.
            // Never mutate a shared Shape or write the packed scene/catalog.
            controlShape = new RectangleShape2D { Size = new Vector2(36, 60) };
            collision.Shape = controlShape;
            post.Position = new Vector2(2952, 246);
        }
        var caster = new Node2D { Name = "LoggedPoseCaster" };
        root.AddChild(caster);
        capsule = ReadSavedBodyCapsule(GD.Load<PackedScene>("res://scenes/player/Player.tscn"));
        tree.Root.AddChild(root);
        var resolver = new MechanicalClawTargetResolver();
        var direction = new Vector2(1, -1).Normalized();
            // Registration only; there is no player, AI combat or input driver.
            for (int tick = 0; tick < 2; tick++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(capsule.Radius == 20 && capsule.Height == 72,
                "clearance uses the actual saved player body, not a point or smaller proxy");
            float[] ys = { 3995.8418f, 3992.175f, 3989.0222f, 3986.3833f, 3984.2583f };
            foreach (float y in ys)
            {
                caster.Position = new Vector2(3961.95f, y);
                var trace = new MechanicalClawKeyboardTrace(128, surface.GetInstanceId());
                MechanicalClawTargetResolution result = resolver.ResolveKeyboard(caster, direction, trace);
                TestAssert.True(trace.ConeQueries == 33 && trace.Dropped == 0 && trace.Faults == 0,
                    "experiment observes the original complete resolver call without extra cone queries");
                using var ray = PhysicsRayQueryParameters2D.Create(caster.GlobalPosition,
                    caster.GlobalPosition + direction * 620);
                ray.CollisionMask = CombatCollisionLayers.World;
                ray.CollideWithAreas = false;
                using var first = caster.GetWorld2D().DirectSpaceState.IntersectRay(ray);
                TestAssert.True(first.ContainsKey("collider") && first.ContainsKey("position"),
                    "logged NE axis has an independent real first collision");
                Node body = first["collider"].AsGodotObject() as Node;
                Vector2 hit = first["position"].AsVector2();
                if (!taller)
                {
                    TestAssert.True(result.Surface != surface && body != post,
                        "historical-geometry control preserves the original NE first-post miss");
                }
                else
                {
                    TestAssert.True(result.Surface == surface && result.Snapshot.Exists &&
                        result.Snapshot.HasLineOfSight && body == post &&
                        hit.DistanceTo(result.Snapshot.WorldAnchor) < .5f &&
                        result.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Left),
                        "current saved taller post wins the unchanged NE angle-first query on its left face");
                    Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(result.Snapshot.WorldAnchor,
                        result.Snapshot.SurfaceNormal, capsule, 48, 4);
                    TestAssert.True(dock.X == 4166 && dock.Y > 3668 + capsule.Height * .5f &&
                        dock.Y < 3956 - capsule.Height * .5f,
                        "candidate dock has full headroom and is above the unchanged landing shelf");
                    // Native shape queries along the proposed vertical descent;
                    // not CastMotion, MoveAndSlide, inertia or a landing proof.
                    for (float dropY = dock.Y; dropY < 3920; dropY += 4)
                        AssertCapsuleSpace(caster, capsule, new Vector2(dock.X, dropY));
                    AssertCapsuleSpace(caster, capsule, new Vector2(dock.X, 3919.5f));
                    AssertCapsuleSpace(caster, capsule, new Vector2(4232, 3916));
                }
                GD.Print($"BASIN_ASCENT_LAYOUT_SAMPLE taller={taller} caster={caster.GlobalPosition} " +
                    $"first={body?.GetPath()} selected={result.Surface?.GetPath()} anchor={result.Snapshot.WorldAnchor} " +
                    "staged_query_only=true physical_jump=false cast=false landing=false");
            }
            GD.Print($"BASIN_ASCENT_LAYOUT_CASE_OK taller={taller} samples=5 " +
                $"top_gap={(taller ? 56 : 148)} underside_gap=80 pixels=false saved_scene_written=false");
        }
        finally
        {
            if (root.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
            else root.Free();
            capsule?.Dispose(); controlShape?.Dispose();
        }
    }

    private static void AssertCapsuleSpace(Node2D caster, CapsuleShape2D capsule, Vector2 center)
    {
        using var query = new PhysicsShapeQueryParameters2D
        {
            Shape = capsule,
            Transform = new Transform2D(0, center),
            CollisionMask = CombatCollisionLayers.World,
            CollideWithAreas = false,
            CollideWithBodies = true,
            Margin = 0
        };
        var hits = caster.GetWorld2D().DirectSpaceState.IntersectShape(query, 32);
        using var hitsOwner = (Godot.Collections.Array)hits;
        TestAssert.Equal(0, hits.Count, "full saved capsule stays clear of actual candidate world collision");
    }

    private static async Task RunSavedWall(SceneTree tree, bool ice)
    {
        var root = new Node2D { Name = ice ? "BasinIceKeyboardFixture" : "BasinMetalKeyboardFixture" };
        var basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
            .Instantiate<PrologueSector>();
        basin.Position = new Vector2(1280, 3600);
        var wall = basin.GetNode<StaticBody2D>(WallPath);
        var profile = wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        var wallShape = wall.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(profile.Material == GrappleSurfaceMaterial.TempleMetal && profile.UseCollisionHitPoint,
            "saved GearPistonOne is collision-hit TempleMetal, not a fabricated point anchor");
        // Instance property only; no shared Resource, collider, scene or visual mutation.
        if (ice) profile.Material = GrappleSurfaceMaterial.Ice;
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = new Vector2(2180, 3960);
        root.AddChild(basin);
        root.AddChild(player);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var health = player.GetNode<HealthComponent>("Health");
        var encounter = basin.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/GearPistonsEncounter");
        ulong playerId = player.GetInstanceId();
        var world = player.GetWorld2D();
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor() && health.CurrentHealth == 5,
                "saved player starts on actual GearEntry with default health");
            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            Input.ParseInputEvent(new InputEventMouseMotion { Position = new Vector2(-5000, -5000) });
            Input.FlushBufferedEvents();
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(input.Current.KeyboardActions && input.KeyboardAimDirection.IsEqualApprox(Vector2.Right) &&
                Mathf.Abs(input.Current.MoveAxis) < .001f && player.GlobalPosition.X < 2200 &&
                !encounter.IsActive && encounter.LiveActorCount == 0,
                "released D retains right keyboard aim before actual encounter activation");
            TestAssert.True(wallShape.Shape is RectangleShape2D rectangle &&
                rectangle.Size.IsEqualApprox(new Vector2(212, 80)) &&
                wall.GlobalPosition.IsEqualApprox(new Vector2(2546, 3956)),
                "both cases retain the exact saved V2 collider and transform");
            NativeHit axis = Ray(player, player.GlobalPosition + Vector2.Right * 620);
            TestAssert.True(axis.Collider == wall && Mathf.Abs(axis.Point.X - 2440) < .5f &&
                axis.Normal.IsEqualApprox(Vector2.Left),
                "native axis first hits saved wall left face even with Ice material");

            MechanicalClawTargetResolution preview = claw.KeyboardPreview;
            if (!ice)
            {
                TestAssert.Equal(profile, preview.Surface, "keyboard preview selects saved wall despite remote pointer");
                AssertLegalPreview(player, preview);
                TestAssert.True(Mathf.Abs(preview.Snapshot.WorldAnchor.X - 2440) < .5f &&
                    preview.Snapshot.WorldAnchor.Y is >= 3916 and <= 3996 &&
                    preview.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Left) &&
                    !preview.Snapshot.WorldAnchor.IsEqualApprox(profile.WorldAnchor),
                    "preview is native wall-face contact, not profile center");
            }
            else AssertIceRejection(player, preview, profile, axis.Point.X);
            GD.Print($"BASIN_KEYBOARD_PREVIEW ice={ice} player={player.GlobalPosition} " +
                $"surface={preview.Surface?.GetPath()} anchor={preview.Snapshot.WorldAnchor} " +
                $"axis_hit={axis.Point} material={preview.Snapshot.Material}");

            Vector2 beforeCast = player.GlobalPosition;
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            for (int tick = 0; tick < 30 && claw.CurrentFrame.LockedTargetRuntimeId == 0; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            if (preview.Surface != null)
            {
                TestAssert.Equal(preview.Snapshot.RuntimeId, claw.CurrentFrame.LockedTargetRuntimeId,
                    "real K locks the exact shown keyboard target");
                TestAssert.True(claw.CurrentFrame.AnchorPoint.DistanceTo(preview.Snapshot.WorldAnchor) < .5f,
                    "K retains shown native contact through windup");
            }
            else TestAssert.Equal(0UL, claw.CurrentFrame.LockedTargetRuntimeId,
                "empty Ice preview does not invent a K target");

            if (ice)
            {
                TestAssert.True(claw.CurrentTarget != profile &&
                    claw.CurrentFrame.LockedTargetRuntimeId != profile.GetInstanceId(),
                    "real K cannot lock the Ice wall");
                if (claw.CurrentFrame.LockedTargetRuntimeId != 0)
                    TestAssert.True(claw.CurrentFrame.AnchorPoint.X <= axis.Point.X + .5f,
                        "Ice cannot be bypassed to lock rear terrain");
                GD.Print($"BASIN_ICE_REJECTED collider_retained=true locked={claw.CurrentFrame.LockedTargetRuntimeId} " +
                    $"alternative={claw.CurrentTarget?.GetPath()}");
            }
            else
            {
                bool attached = false, commandedPull = false;
                float nearestDock = float.PositiveInfinity;
                for (int tick = 0; tick < 60; tick++)
                {
                    await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                    MechanicalClawFrame frame = claw.CurrentFrame;
                    attached |= frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;
                    commandedPull |= frame.VelocityCorrection.X > 0 && frame.LockedTargetRuntimeId == profile.GetInstanceId();
                    if (frame.LockedTargetRuntimeId == profile.GetInstanceId())
                        nearestDock = Mathf.Min(nearestDock, player.GlobalPosition.DistanceTo(frame.DockPoint));
                    if (attached && commandedPull && player.GlobalPosition.X - beforeCast.X >= 8) break;
                    if (frame.State is MechanicalClawState.Released or MechanicalClawState.Retract or MechanicalClawState.Idle) break;
                }
                GD.Print($"BASIN_KEYBOARD_PULL observed_dx={player.GlobalPosition.X - beforeCast.X} attached={attached} " +
                    $"commanded={commandedPull} nearest_dock={nearestDock} state={claw.CurrentFrame.State} " +
                    $"failure={claw.CurrentFrame.Failure} encounter_active={encounter.IsActive} actors={encounter.LiveActorCount}");
                TestAssert.True(attached && commandedPull && player.GlobalPosition.X - beforeCast.X >= 8 &&
                    nearestDock < beforeCast.DistanceTo(preview.Snapshot.WorldAnchor + Vector2.Left * 48) - 8 &&
                    Mathf.Abs(input.Current.MoveAxis) < .001f &&
                    !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.W),
                    "actual saved-wall auto-pull advances toward dock with no held movement key");
                // Stop after a bounded witnessed pull. Live enemies may subsequently
                // obstruct LOS; arrival through an enemy is not this contract.
                GD.Print("BASIN_KEYBOARD_PULL_OK arrival_not_certified=true");
            }
            TestAssert.True(player.GetInstanceId() == playerId && player.GetWorld2D() == world &&
                player.IsInsideTree() && health.CurrentHealth == 5,
                "same living player/world persists without reset through bounded trial");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
        }
    }

    private static void AssertIceRejection(CharacterBody2D player, MechanicalClawTargetResolution preview,
        GrappleSurface2D ice, float blockedX)
    {
        TestAssert.True(preview.Surface != ice && preview.Snapshot.RuntimeId != ice.GetInstanceId(),
            "keyboard rejects actual Ice profile, not its collider");
        if (preview.Surface == null) return;
        AssertLegalPreview(player, preview);
        TestAssert.True(preview.Snapshot.WorldAnchor.X <= blockedX + .5f,
            "legal Ice alternative stays in front of first solid wall, never behind it");
    }

    private static void AssertLegalPreview(CharacterBody2D player, MechanicalClawTargetResolution preview)
    {
        TestAssert.True(preview.Surface != null && preview.Surface.IsAvailable &&
            preview.Surface.GetWorld2D() == player.GetWorld2D() && preview.Snapshot.Exists &&
            preview.Snapshot.HasLineOfSight &&
            preview.Snapshot.Material is not (GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden) &&
            preview.Surface.UseCollisionHitPoint && !preview.Snapshot.IsLegacyInteraction,
            "this terrain preview is live, legal and collision-derived in player's world");
        Vector2 direction = player.GlobalPosition.DirectionTo(preview.Snapshot.WorldAnchor);
        NativeHit hit = Ray(player, preview.Snapshot.WorldAnchor + direction);
        TestAssert.True(hit.Collider == preview.Surface.GetParent() &&
            hit.Point.DistanceTo(preview.Snapshot.WorldAnchor) < .5f &&
            hit.Normal.IsEqualApprox(preview.Snapshot.SurfaceNormal),
            "independent native first-hit ray agrees with preview contact and normal");
    }

    private static NativeHit Ray(CharacterBody2D player, Vector2 end)
    {
        using var query = PhysicsRayQueryParameters2D.Create(player.GlobalPosition, end);
        query.CollisionMask = CombatCollisionLayers.World;
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        TestAssert.True(hit.ContainsKey("collider") && hit.ContainsKey("position") && hit.ContainsKey("normal"),
            "saved terrain native ray has a real first collision");
        return new NativeHit(hit["collider"].AsGodotObject() as Node,
            hit["position"].AsVector2(), hit["normal"].AsVector2());
    }

    private readonly record struct NativeHit(Node Collider, Vector2 Point, Vector2 Normal);
}
