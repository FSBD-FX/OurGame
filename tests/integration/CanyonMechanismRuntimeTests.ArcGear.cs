using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Knight.Mechanisms;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class CanyonMechanismRuntimeTests
{
    public static async Task RunArcGear(SceneTree tree)
    {
        Type type = typeof(CanyonMechanismRuntimeTests).Assembly.GetType("Knight.Mechanisms.CanyonArcGear");
        TestAssert.True(type != null && typeof(CanyonFixedGear).IsAssignableFrom(type),
            "ARC_GEAR_RUNTIME_RED: a distinct native arc circle must preserve the fixed gear damage contract");
        var observed = new List<(int Hp, ulong Sequence)>();
        for (int variant = 0; variant < 2; variant++)
        {
            var fixture = new SpringboardFixture(tree);
            try
            {
                fixture.Board.Position = new(700, 500);
                await fixture.Mount();
                var body = fixture.Player.GetNode<CollisionShape2D>("BodyCollider");
                using (Variant nativeBodyShape = PhysicsServer2D.ShapeGetData(body.Shape.GetRid()))
                    GD.Print($"ARC_BODY_SHAPE_NATIVE_DATA variant={variant} type={nativeBodyShape.VariantType} data={nativeBodyShape}");
                TestAssert.True(body.Shape is CapsuleShape2D capsule && capsule.Radius == 20 && capsule.Height == 72 &&
                    body.Transform.IsEqualApprox(Transform2D.Identity) && fixture.Player.IsOnFloor() &&
                    fixture.Player.GlobalPosition.DistanceTo(new(180, 464)) < .2f && fixture.Health.CurrentHealth == 5,
                    "ARC_GEAR_FIXTURE: original saved player body20/72 and floor500 are real, healthy and stable");
                Vector2 start = variant == 0 ? new(20, 624) : new(20, 464);
                var gear = ReloadArcGear(fixture, type, start);
                // Native endpoints, not merely broad rectangles, must miss the actual saved capsule.
                using var endpoint = new CircleShape2D { Radius = 24 };
                TestAssert.True(!endpoint.Collide(new Transform2D(0, start), body.Shape, body.GlobalTransform) &&
                    !endpoint.Collide(new Transform2D(0, start + new Vector2(320, 0)), body.Shape, body.GlobalTransform),
                    "ARC_GEAR_FIXTURE: both literal arc endpoints miss the original native body");
                gear._PhysicsProcess(4d); // Full driven period: same endpoints, nonempty visited upper arc.
                observed.Add((fixture.Health.CurrentHealth, gear.ContactSequence));
                GD.Print($"ARC_GEAR_PAIR_OBS variant={variant} hp={fixture.Health.CurrentHealth} sequence={gear.ContactSequence} " +
                    $"node={gear.GlobalPosition} native={PhysicsServer2D.AreaGetTransform(gear.GetRid()).Origin} " +
                    "saved_reloaded=true saved_player=true endpoints_miss=true");
            }
            finally { await fixture.DisposeAsync(); }
        }
        TestAssert.True(observed[0].Hp == 4 && observed[0].Sequence == 1,
            $"ARC_GEAR_APEX_RED: a full visited arc must damage its apex once, hp={observed[0].Hp} sequence={observed[0].Sequence}");
        TestAssert.True(observed[1].Hp == 5 && observed[1].Sequence == 0,
            $"ARC_GEAR_CHORD_RED: chord/AABB interior outside the visited arc must stay safe, hp={observed[1].Hp} sequence={observed[1].Sequence}");
        GD.Print("ARC_GEAR_RUNTIME_OK cases=2 saved_reload=true saved_player=true full_cycle_apex=true chord_safe=true native_body=true normal_route=false human=false");
        await ArcGearLifetime(tree, type);
        await ArcGearNativeOwners(tree, type);
        await ArcGearCallbacks(tree, type);
        await ArcGearInvalidTargets(tree, type);
        await ArcGearCurrentPose(tree, type);
        await ArcGearKeyboard(tree, type);
        await ArcGearFog(tree);
    }

    private static async Task ArcGearLifetime(SceneTree tree, Type type)
    {
        var f = new SpringboardFixture(tree);
        try
        {
            f.Board.Position = new(700, 500); f.Health.InvulnerabilitySeconds = 0;
            f.Driver = null;
            await f.Mount();
            f.Root.SetMeta("_seamless_staged", true);
            var gear = (CanyonArcGear)ReloadArcGear(f, type, new(20, 624), true);
            var collider = gear.GetNode<CollisionShape2D>("Collision");
            var borrowed = collider.Shape;
            void Reject(string mode)
            {
                var before = gear.CurrentMotion; int hp = f.Health.CurrentHealth;
                gear._PhysicsProcess(4);
                TestAssert.True(gear.CurrentMotion == before && f.Health.CurrentHealth == hp && gear.ContactSequence == 0,
                    "ARC_GEAR_REVOKED_RED: " + mode + " cannot advance/damage");
            }
            Reject("disabled-before-Ready streaming stage");
            TestAssert.True(gear.HasValidMotionGeometry && !gear.IsDangerTelegraphActive,
                "actual staged Ready preserves future motion geometry but never authorizes damage");
            f.Root.RemoveMeta("_seamless_staged"); collider.Disabled = false;
            tree.Paused = true; Reject("pause"); tree.Paused = false;
            gear.Visible = false; Reject("hidden"); gear.Visible = true;
            gear.Monitoring = false; Reject("monitoring"); gear.Monitoring = true;
            collider.Disabled = true; Reject("disabled native circle"); collider.Disabled = false;
            gear.SetPhysicsProcess(false); Reject("physics off"); gear.SetPhysicsProcess(true);
            f.Root.SetMeta("_seamless_staged", true); Reject("staged");
            var reservation = ResidentEnvironmentInventory.Capture(f.Root).Regions;
            int arcReservations = 0;
            foreach (var region in reservation)
                if (region.SourceId.StartsWith("arc-gear:", StringComparison.Ordinal))
                {
                    arcReservations++;
                    TestAssert.True(region.Kind == ResidentKeepOutKind.Hazard &&
                        region.WorldBounds.Position.DistanceTo(new(-4, 440)) < .002f &&
                        region.WorldBounds.Size.DistanceTo(new(368, 208)) < .002f,
                        "ARC_GEAR_RESERVATION_RED: staged arc reserves its whole future upper half, not a static circle/chord");
                }
            TestAssert.Equal(1, arcReservations, "arc is reserved once before the base fixed-gear branch");
            f.Root.RemoveMeta("_seamless_staged");
            f.Root.Position = new(20, 0); Reject("moved source parent"); f.Root.Position = Vector2.Zero;
            gear.Scale = new(2, 1); Reject("non-unit source"); gear.Scale = Vector2.One;
            gear.PivotOffset = new(9999, 0); gear.SwingRadians = 9; gear.PeriodSeconds = .01f; gear.Radius = 99;
            gear._PhysicsProcess(4);
            TestAssert.True(f.Health.CurrentHealth == 4 && gear.ContactSequence == 1 && gear.DangerRadius == 24,
                "arc authoring edits after Ready cannot partially replace validated snapshots");
            gear.PivotOffset = new(160, 0); gear.SwingRadians = Mathf.Pi; gear.PeriodSeconds = 4; gear.Radius = 24;
            gear.ResetForCheckpoint();
            TestAssert.True(gear.CurrentMotion.Offset == Vector2.Zero && gear.Position == new Vector2(20, 624),
                "arc checkpoint reset returns exactly to authored origin without an attack");
            f.Root.RemoveChild(gear);
            TestAssert.True(GodotObject.IsInstanceValid(borrowed), "arc exit does not dispose borrowed collider");
            f.Root.AddChild(gear);
            TestAssert.True(gear.IsDangerTelegraphActive && gear.CurrentMotion.Offset == Vector2.Zero,
                "same-instance arc reentry gets fresh query/motion without stale visited interval");
            gear._PhysicsProcess(4);
            TestAssert.True(f.Health.CurrentHealth == 3 && gear.ContactSequence == 2, "arc reentry retains monotonic identity");
            gear.ResetForCheckpoint();
            await Frames(tree, 60);
            TestAssert.True(gear.CurrentMotion.Offset.DistanceTo(new(160, -160)) < 12 &&
                PhysicsServer2D.AreaGetTransform(gear.GetRid()).IsEqualApprox(gear.GlobalTransform),
                "ordinary engine physics advances the arc toward the apex with native pose synchronization");
            var queued = gear.CurrentMotion; f.Root.QueueFree(); gear._PhysicsProcess(4);
            TestAssert.Equal(queued, gear.CurrentMotion, "queued ancestor revokes arc before deferred free");
            GD.Print("ARC_GEAR_LIFETIME_OK saved_reload=true snapshots=true future_reservation=true pause_hidden_staged=true reset_reentry=true identity=true automatic_physics=true borrowed_retained=true human=false");
        }
        finally { tree.Paused = false; await f.DisposeAsync(); }
    }

    private static async Task ArcGearNativeOwners(SceneTree tree, Type type)
    {
        var f = new SpringboardFixture(tree);
        try
        {
            f.Board.Position = new(700, 500); f.Health.InvulnerabilitySeconds = 0;
            uint empty = f.Player.CreateShapeOwner(f.Player);
            f.Player.ShapeOwnerAddShape(empty, f.Own(new CircleShape2D { Radius = 6 }));
            uint actual = f.Player.CreateShapeOwner(f.Player);
            f.Player.ShapeOwnerAddShape(actual, f.Own(new RectangleShape2D { Size = new(8, 16) }));
            f.Player.ShapeOwnerSetTransform(actual, new Transform2D(.2f, new Vector2(0, -160)));
            uint circle = f.Player.CreateShapeOwner(f.Player);
            f.Player.ShapeOwnerAddShape(circle, f.Own(new CircleShape2D { Radius = 6 }));
            f.Player.ShapeOwnerSetTransform(circle, new Transform2D(0, new Vector2(0, -160)));
            await f.Mount();
            TestAssert.True(f.Player.ShapeOwnerGetShapeIndex(empty, 0) < f.Player.ShapeOwnerGetShapeIndex(actual, 0) &&
                f.Player.ShapeOwnerGetOwner(actual) == f.Player && f.Health.CurrentHealth == 5 && f.Player.IsOnFloor(),
                "manual native owners lack CollisionShape children; empty owner precedes a real hit and preserves original support");
            var gear = (CanyonArcGear)ReloadArcGear(f, type, new(20, 464));
            int damaged = 0, contacted = 0;
            f.Health.Damaged += (_, _) => damaged++;
            gear.Contacted += (_, _) => contacted++;
            gear._PhysicsProcess(4); gear._PhysicsProcess(.01);
            TestAssert.True(f.Health.CurrentHealth == 4 && gear.ContactSequence == 1 && damaged == 1 && contacted == 1,
                "ARC_GEAR_SHAPES_RED: native owner rectangle/circle above safe core must hit once without iframe/dedup masking");
            GD.Print("ARC_GEAR_NATIVE_OWNERS_OK saved_player=true manual_owners=true empty_before_hit=true capsule_circle_rectangle=true damage_events=1 contact_events=1 invulnerability=0 human=false");
        }
        finally { await f.DisposeAsync(); }
    }

    private static async Task ArcGearCallbacks(SceneTree tree, Type type)
    {
        string[] modes = { "live", "reset", "queue", "remove", "hide", "disable", "stop", "pause", "monitoring", "pause-contact", "move-other", "target-layer" };
        foreach (string mode in modes)
        {
            var f = new SpringboardFixture(tree); CanyonArcGear gear = null;
            try
            {
                f.Board.Position = new(700, 500); f.Health.InvulnerabilitySeconds = 0;
                var second = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
                second.Name = "SecondSavedPlayer"; second.Position = new(200, 464);
                second.GetNode<Camera2D>("Camera2D").Enabled = false; second.AddToGroup("player");
                var otherHealth = second.GetNode<HealthComponent>("Health"); otherHealth.InvulnerabilitySeconds = 0;
                if (mode is "pause" or "pause-contact")
                { f.Player.ProcessMode = Node.ProcessModeEnum.Always; second.ProcessMode = Node.ProcessModeEnum.Always; }
                f.Root.AddChild(second); await f.Mount();
                using (var apex = new CircleShape2D { Radius = 24 })
                    foreach (var player in new[] { f.Player, second })
                    {
                        var body = player.GetNode<CollisionShape2D>("BodyCollider");
                        TestAssert.True(player.IsOnFloor() && apex.Collide(new Transform2D(0, new Vector2(180, 464)), body.Shape, body.GlobalTransform),
                            "both actual saved players independently collide with a literal native apex circle");
                    }
                gear = (CanyonArcGear)ReloadArcGear(f, type, new(20, 624));
                if (mode is "pause" or "pause-contact") gear.ProcessMode = Node.ProcessModeEnum.Always;
                int damaged = 0, contacted = 0;
                void OnDamage(CharacterBody2D source)
                {
                    damaged++;
                    var before = gear.CurrentMotion; gear._PhysicsProcess(.01);
                    TestAssert.Equal(before, gear.CurrentMotion, "damage callback cannot advance arc reentrantly");
                    if (damaged != 1) return;
                    switch (mode)
                    {
                        case "reset": gear.ResetForCheckpoint(); break;
                        case "queue": gear.QueueFree(); break;
                        case "remove": f.Root.RemoveChild(gear); break;
                        case "hide": gear.Visible = false; break;
                        case "disable": gear.GetNode<CollisionShape2D>("Collision").Disabled = true; break;
                        case "stop": gear.SetPhysicsProcess(false); break;
                        case "pause": tree.Paused = true; break;
                        case "monitoring": gear.Monitoring = false; break;
                        case "move-other":
                            var other = source == f.Player ? second : f.Player;
                            other.Position = new(2000, 464); other.ForceUpdateTransform();
                            using (Variant actual = PhysicsServer2D.BodyGetState(other.GetRid(), PhysicsServer2D.BodyState.Transform))
                                GD.Print($"ARC_GEAR_CALLBACK_TARGET_MOVE mode={mode} player={other.GetInstanceId()} native={actual.AsTransform2D()} node={other.GlobalTransform}");
                            break;
                        case "target-layer":
                            (source == f.Player ? second : f.Player).CollisionLayer = CombatCollisionLayers.None;
                            break;
                    }
                    if (mode == "pause") TestAssert.True(gear.CanProcess() && f.Player.CanProcess() && second.CanProcess(),
                        "Always nodes make explicit pause gate necessary");
                }
                void First(int _, Vector2 __) => OnDamage(f.Player);
                void Second(int _, Vector2 __) => OnDamage(second);
                f.Health.Damaged += First; otherHealth.Damaged += Second;
                gear.Contacted += (_, _) => { contacted++; if (mode == "pause-contact") tree.Paused = true; };
                try
                {
                    GD.Print($"ARC_GEAR_CALLBACK_START mode={mode}");
                    gear._PhysicsProcess(4);
                    bool live = mode == "live";
                    int expectedContacts = live ? 2 : mode is "pause-contact" or "move-other" or "target-layer" ? 1 : 0;
                    TestAssert.True(f.Health.CurrentHealth + otherHealth.CurrentHealth == (live ? 8 : 9) &&
                        damaged == (live ? 2 : 1) && gear.ContactSequence == (live ? 2UL : 1UL) && contacted == expectedContacts,
                        $"ARC_GEAR_CALLBACK_RED: {mode} hp={f.Health.CurrentHealth}/{otherHealth.CurrentHealth} damage={damaged} contact={contacted} sequence={gear.ContactSequence}");
                }
                finally { f.Health.Damaged -= First; otherHealth.Damaged -= Second; }
            }
            finally
            {
                tree.Paused = false;
                if (GodotObject.IsInstanceValid(gear) && !gear.IsInsideTree() && !gear.IsQueuedForDeletion()) gear.Free();
                await f.DisposeAsync();
            }
        }
        GD.Print("ARC_GEAR_CALLBACK_OK cases=12 real_two_players=true order_independent=true reentrant_time_rejected=true reset_revokes=true pause_always=true target_revalidated=true target_layer_revoked=true human=false");
    }

    private static async Task ArcGearFog(SceneTree tree)
    {
        var world = new Node2D { Name = "ArcGearFog" };
        var sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonEntrance.tscn").Instantiate<PrologueSector>();
        var gear = new CanyonArcGear { Name = "ArcGear", Position = new(180, 500), Radius = 24, Damage = 1,
            PivotOffset = new(40, -40), SwingRadians = -Mathf.Pi / 2, PeriodSeconds = 2 };
        using var shape = new CircleShape2D { Radius = 24 };
        using var wallShape = new RectangleShape2D { Size = new(300, 8) };
        gear.AddChild(new CollisionShape2D { Name = "Collision", Shape = shape });
        sector.GetNode("Gameplay/Mechanisms").AddChild(gear);
        var overlay = new FogRecyclingReadabilityOverlay();
        var layer = new CanvasLayer { Layer = 2, FollowViewportEnabled = true };
        HeadlampOcclusionSampler sampler = null;
        try
        {
            world.AddChild(sector); layer.AddChild(overlay); world.AddChild(layer); tree.Root.AddChild(world);
            await Frames(tree, 2); sampler = new HeadlampOcclusionSampler(world);
            void Present(Vector2? actor = null) => overlay.PresentSources(new CollisionObject2D[] { gear, gear },
                actor ?? new Vector2(200, 580), sampler.Capture());
            Present(); TestAssert.Equal(1, overlay.PresentedGearCount, "canonical arc produces one current-circle cue");
            Vector2 before = gear.GlobalPosition; await Frames(tree, 8); Present();
            TestAssert.True(gear.GlobalPosition.DistanceTo(before) > 1 && overlay.PresentedSegmentCount > 0,
                "arc fog reads actual moving native circle rather than origin or future arc");
            gear.Visible = false; Present(); TestAssert.Equal(0, overlay.PresentedGearCount, "hidden arc clears cue"); gear.Visible = true;
            var wall = new StaticBody2D { Position = new(220, 548), CollisionLayer = CombatCollisionLayers.World };
            wall.AddChild(new CollisionShape2D { Shape = wallShape }); world.AddChild(wall);
            await Frames(tree, 2); Present(); TestAssert.Equal(0, overlay.PresentedGearCount, "native wall occludes current circle");
            wall.QueueFree(); await Frames(tree, 2); Present(); TestAssert.Equal(1, overlay.PresentedGearCount, "removed wall restores cue");
            Present(new Vector2(2000, 580)); TestAssert.Equal(0, overlay.PresentedSegmentCount, "arc cannot reveal distant room");
            sector.SetMeta("_seamless_staged", true); Present(); TestAssert.Equal(0, overlay.PresentedSegmentCount, "staged arc clears cue");
            sector.RemoveMeta("_seamless_staged");
            tree.Paused = true; Present(); TestAssert.Equal(0, overlay.PresentedSegmentCount, "paused arc clears cue"); tree.Paused = false;
            GD.Print("ARC_GEAR_FOG_OK canonical=true native_current_circle=true deduplicated=true real_wall=true range=true pause_staged_hidden=true pixels=false saved_map=false human=false");
        }
        finally { tree.Paused = false; sampler?.Dispose(); world.QueueFree(); await Frames(tree, 2); }
    }

    private static async Task ArcGearInvalidTargets(SceneTree tree, Type type)
    {
        foreach (string mode in new[] { "unsupported-shape", "nonuniform" })
        {
            var f = new SpringboardFixture(tree);
            try
            {
                f.Board.Position = new(700, 500); await f.Mount();
                if (mode == "unsupported-shape")
                {
                    uint owner = f.Player.CreateShapeOwner(f.Player);
                    f.Player.ShapeOwnerAddShape(owner, f.Own(new SegmentShape2D { A = new(-2, 0), B = new(2, 0) }));
                }
                else
                {
                    uint owner = f.Player.CreateShapeOwner(f.Player);
                    f.Player.ShapeOwnerAddShape(owner, f.Own(new RectangleShape2D { Size = new(8, 16) }));
                    f.Player.ShapeOwnerSetTransform(owner,
                        new Transform2D(new Vector2(2, 0), Vector2.Down, new Vector2(0, -160)));
                }
                var gear = (CanyonArcGear)ReloadArcGear(f, type, new(20, 624));
                bool rejected = false;
                try { gear._PhysicsProcess(4); }
                catch (InvalidOperationException) { rejected = true; }
                TestAssert.True(rejected && f.Health.CurrentHealth == 5 && gear.ContactSequence == 0 && !gear.IsDangerTelegraphActive,
                    "ARC_GEAR_INVALID_TARGET_RED: " + mode + " must withdraw before any partial batch damage");
            }
            finally { await f.DisposeAsync(); }
        }
        GD.Print("ARC_GEAR_INVALID_TARGETS_OK cases=2 unsupported=true nonuniform=true no_partial_damage=true authorization_withdrawn=true human=false");
    }

    private static async Task ArcGearKeyboard(SceneTree tree, Type type)
    {
        foreach (int direction in new[] { -1, 1 })
        {
            var f = new SpringboardFixture(tree);
            try
            {
                f.Board.Position = new(700, 500); await f.Mount();
                var gear = (CanyonArcGear)ReloadArcGear(f, type, new(180, 464));
                Vector2 start = f.Player.GlobalPosition;
                var input = f.Player.GetNode<PlayerInputReader>("Input");
                ulong before = input.CurrentFrameSequence;
                f.Driver = KeyboardRouteInputDriver.Begin(f.Player);
                f.Driver.Command(direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight, true);
                await Frames(tree, 30);
                TestAssert.True(input.CurrentFrameSequence > before && input.CurrentFrame.MoveAxis == direction &&
                    (f.Player.GlobalPosition.X - start.X) * direction > 2 &&
                    f.Health.CurrentHealth is > 0 and < 5 && gear.ContactSequence > 0 && gear.IsDangerTelegraphActive,
                    $"ARC_GEAR_KEYBOARD_RED: normal moving saved Player must remain a valid live native target, dir={direction} " +
                    $"move={f.Player.GlobalPosition-start} hp={f.Health.CurrentHealth} sequence={gear.ContactSequence} active={gear.IsDangerTelegraphActive}");
            }
            finally { await f.DisposeAsync(); }
        }
        GD.Print("ARC_GEAR_KEYBOARD_OK directions=2 real_input=true natural_physics=true saved_player=true moved=true actual_damage=true actor_alive=true authorization_live=true controlled_start=true human=false");
    }

    private static async Task ArcGearCurrentPose(SceneTree tree, Type type)
    {
        var f = new SpringboardFixture(tree);
        try
        {
            f.Board.Position = new(780, 500); f.Player.Position = new(700, 464); await f.Mount();
            TestAssert.True(f.Player.IsOnFloor() && f.Health.CurrentHealth == 5,
                "current-pose fixture starts healthy and grounded, not on the unrelated springboard");
            var gear = (CanyonArcGear)ReloadArcGear(f, type, new(20, 624));
            f.Player.Position = new(180, 464); f.Player.ForceUpdateTransform();
            using (Variant actual = PhysicsServer2D.BodyGetState(f.Player.GetRid(), PhysicsServer2D.BodyState.Transform))
                TestAssert.True(actual.AsTransform2D().Origin.X > 600 && f.Player.GlobalPosition == new Vector2(180, 464),
                    "current-pose control genuinely has old native body state outside arc bounds and committed scene pose inside");
            using var rect = new RectangleShape2D { Size = new(368, 208) };
            using var query = new PhysicsShapeQueryParameters2D { Shape = rect, Transform = new Transform2D(0, new Vector2(180, 544)),
                CollisionMask = CombatCollisionLayers.Player, CollideWithBodies = true, CollideWithAreas = false, Margin = 0 };
            using (Godot.Collections.Array hits = (Godot.Collections.Array)f.Player.GetWorld2D().DirectSpaceState.IntersectShape(query, 16))
                TestAssert.Equal(0, hits.Count, "literal native broadphase demonstrably misses the moved-in current Player this tick");
            gear._PhysicsProcess(4);
            TestAssert.True(f.Health.CurrentHealth == 4 && gear.ContactSequence == 1,
                $"ARC_GEAR_CURRENT_POSE_RED: old native broadphase must not omit current same-world Player, hp={f.Health.CurrentHealth} sequence={gear.ContactSequence}");
            GD.Print("ARC_GEAR_CURRENT_POSE_OK native_broadphase_empty=true current_player_inside=true native_shape_identity=true actual_damage=true controlled_relocation=true relative_ccd=false human=false");
        }
        finally { await f.DisposeAsync(); }
    }

    private static CanyonFixedGear ReloadArcGear(SpringboardFixture fixture, Type type, Vector2 start, bool disabled = false)
    {
        var authored = (CanyonFixedGear)Activator.CreateInstance(type);
        using var packed = new PackedScene();
        string path = Path.Combine(IsolatedPrologueFixture.CreatePaths("arc-gear").CaseRoot, "arc.tscn");
        try
        {
            authored.Name = "ArcGear"; authored.Position = start; authored.Radius = 24; authored.Damage = 1;
            SetMovingProperty(authored, "PivotOffset", new Vector2(160, 0));
            SetMovingProperty(authored, "SwingRadians", Mathf.Pi);
            SetMovingProperty(authored, "PeriodSeconds", 4f);
            var collider = new CollisionShape2D { Name = "Collision", Shape = fixture.Own(new CircleShape2D { Radius = 24 }), Disabled = disabled };
            authored.AddChild(collider); collider.Owner = authored;
            TestAssert.Equal(Error.Ok, packed.Pack(authored), "arc fixture packs its original owned collider");
            TestAssert.Equal(Error.Ok, ResourceSaver.Save(packed, path), "arc fixture actually saves outside real user storage");
        }
        finally { authored.Free(); }
        var reloaded = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore);
        TestAssert.True(reloaded != null, "arc fixture reload must exist");
        var gear = reloaded.Instantiate<CanyonFixedGear>();
        TestAssert.True(gear.GetType() == type && gear.Position == start && gear.Radius == 24 && gear.Damage == 1,
            "arc fixture instantiates the reloaded script, not the authoring node or a static substitute");
        fixture.Root.AddChild(gear);
        return gear;
    }
}
