using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    private readonly record struct GroundSeamObservation(ulong Tick, ulong Owner,
        Vector2 Root, PlayerGroundGaitFrame Gait, Vector2 Near, Vector2 Far);

    // Catches reprojecting an already planted world foot into a new owner's bounds.
    // This is an isolated saved-geometry sample, NOT a natural whole-level route.
    public static async Task RunGroundSeamNative(SceneTree tree)
    {
        await RunSavedGroundSeamCase(tree, 1);
        await RunSavedGroundSeamCase(tree, -1);
        foreach (string revoke in new[] { "body-detach", "shape-detach", "disabled", "moved", "shrunk" })
            await RunSavedGroundSeamCase(tree, 1, revoke);
        await RunSavedGroundSeamCase(tree, 1, basin: true);
        GroundSeamLiteralModelControls();
    }

    private static async Task RunSavedGroundSeamCase(SceneTree tree, int direction, string revoke = null, bool basin = false)
    {
        ReleaseInput();
        var root = new Node2D { Name = "SavedGroundSeamFixture" };
        Node detached = null;
        KeyboardRouteInputDriver driver = null;
        PlayerPresentationCoordinator coordinator = null;
        Action<PlayerPresentationFrame> onFrame = null;
        try
        {
            var left = TakeSavedSeamBody(basin ? "CanyonBasin" : "CanyonEntrance",
                basin ? "BasinCampFloor" : "BridgeFarBank", basin ? new(1280, 3600) : Vector2.Zero);
            root.AddChild(left);
            // Saved PrologueLevel.tscn manifest offsets: Descent=(1280,0), Basin=(1280,3600).
            var right = TakeSavedSeamBody(basin ? "CanyonBasin" : "CanyonDescent",
                basin ? "GearEntry" : "RiftUpperFloor", basin ? new(1280, 3600) : new(1280, 0));
            root.AddChild(right);
            var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
                .Instantiate<CharacterBody2D>();
            // One pre-tree spawn; Basin sample walks D from (2000,3960) to x2220.
            player.Position = basin ? new(2000, 3960) : new(direction > 0 ? 1200 : 1360, 600);
            root.AddChild(player);
            tree.Root.AddChild(root);
            Rect2 leftRect = SavedSeamWorldRect(left), rightRect = SavedSeamWorldRect(right);
            Rect2 expectedLeft = basin ? new(1280, 3996, 800, 40) : new(400, 636, 880, 84);
            Rect2 expectedRight = basin ? new(2080, 3996, 300, 40) : new(1280, 636, 620, 24);
            string caseName = basin ? "same-sector-basin" : revoke ?? "continuous";
            TestAssert.True(leftRect.IsEqualApprox(expectedLeft) && rightRect.IsEqualApprox(expectedRight) &&
                Mathf.Abs(leftRect.End.X - rightRect.Position.X) < .001f &&
                Mathf.Abs(leftRect.Position.Y - rightRect.Position.Y) < .001f,
                $"GROUND_SEAM_FIXTURE: saved literal same-Y zero-gap rectangles left={leftRect} right={rightRect}");
            ulong leftId = left.GetNode<CollisionShape2D>("Collision").GetInstanceId();
            ulong rightId = right.GetNode<CollisionShape2D>("Collision").GetInstanceId();
            TestAssert.True(leftId != rightId, "GROUND_SEAM_FIXTURE: two distinct saved native shape owners");
            await Frames(tree, 24);
            var motor = player.GetNode<PlayerMotor>("Motor");
            var health = player.GetNode<HealthComponent>("Health");
            int initialHealth = health.CurrentHealth;
            coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var near = formal.GetNode<PlayerFormalLayer>("NearLeg");
            var far = formal.GetNode<PlayerFormalLayer>("FarLeg");
            TestAssert.True(player.IsOnFloor() && initialHealth > 0,
                "GROUND_SEAM_FIXTURE: saved Player settles alive on original floor");
            driver = KeyboardRouteInputDriver.Begin(player);
            Vector2 start = player.GlobalPosition;
            var owners = new HashSet<ulong>();
            GroundSeamObservation? previous = null;
            int walked = 0, handoffs = 0, plantedComparisons = 0, matched = 0;
            bool revokeWindow = false, revoked = false, revokeChecked = false;
            ulong revokeAfterTick = 0;
            Vector2 revokedSole = Vector2.Zero;
            float worstJump = 0;
            Exception sampleFault = null;
            onFrame = frame =>
            {
                if (sampleFault != null) return;
                try
                {
                    TestAssert.True(formal.CurrentPlayerFrame == frame,
                        "GROUND_SEAM_FIXTURE: synchronous publication is the exact formal Draw frame");
                    ulong tick = Engine.GetPhysicsFrames();
                    if (previous.HasValue && previous.Value.Tick == tick) return;
                    driver.ObserveFrame();
                    TestAssert.True(health.CurrentHealth == initialHealth && motor.AcceptedJumpCount == 0 &&
                        motor.AcceptedDashCount == 0 && frame.Action.State == PlayerActionState.None &&
                        frame.Locomotion.State != PlayerLocomotionState.Recovery,
                        "GROUND_SEAM_FIXTURE: ordinary walking has no HP, jump, thruster, action or recovery intervention");
                    ulong owner = ActualSeamFloorShape(player);
                    var gait = frame.Gait;
                    var now = new GroundSeamObservation(tick, owner, player.GlobalPosition, gait,
                        near.ToGlobal(near.LegSoleLocal), far.ToGlobal(far.LegSoleLocal));
                    bool moving = player.IsOnFloor() && player.Velocity.X * direction > 80 && gait.Observed && gait.Weight >= .999f;
                    if (revoked && !revokeChecked && tick > revokeAfterTick)
                    {
                        TestAssert.True(tick == revokeAfterTick + 1 && moving && owner == rightId,
                            $"GROUND_SEAM_FIXTURE: {revoke} next physics must remain grounded on actual new shape tick={tick} old={revokeAfterTick}");
                        // Check BOTH actual feet: a revoked old plane cannot still own any stance.
                        if (gait.NearStance) AssertSeamStanceOnRight(now.Near, rightRect, revoke);
                        if (gait.FarStance) AssertSeamStanceOnRight(now.Far, rightRect, revoke);
                        TestAssert.True(gait.NearStance || gait.FarStance,
                            "GROUND_SEAM_REVOKE_RED: safe fallback retains a real current-floor stance");
                        revokeChecked = true;
                        GD.Print($"GROUND_SEAM_REVOKE_OK kind={revoke} tick={tick} current_owner={owner} " +
                            $"old_stance={revokedSole} near={now.Near} far={now.Far} native_geometry=true");
                    }
                    if (moving)
                    {
                        TestAssert.True(owner == leftId || owner == rightId,
                            $"GROUND_SEAM_FIXTURE: actual upward slide shape must be saved terrain owner={owner}");
                        owners.Add(owner); walked++;
                        foreach (var leg in new[] { near, far })
                        {
                            TestAssert.Near(18, leg.LegHipLocal.DistanceTo(leg.LegKneeLocal), .002f,
                                "GROUND_SEAM_DRAW: fixed upper leg");
                            TestAssert.Near(18, leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal), .002f,
                                "GROUND_SEAM_DRAW: fixed lower leg");
                        }
                        if (gait.NearStance) TestAssert.True(now.Near.DistanceTo(gait.NearSoleWorld) < .05f,
                            "GROUND_SEAM_DRAW: actual near sole matches published stance target");
                        if (gait.FarStance) TestAssert.True(now.Far.DistanceTo(gait.FarSoleWorld) < .05f,
                            "GROUND_SEAM_DRAW: actual far sole matches published stance target");
                        matched++;
                        if (previous is { } old && old.Owner != 0 && old.Owner != owner)
                        {
                            TestAssert.True(tick == old.Tick + 1 && old.Gait.Observed && old.Gait.Weight >= .999f &&
                                (now.Root.X - old.Root.X) * direction > 0 && Mathf.Abs(now.Root.Y - old.Root.Y) < .05f,
                                "GROUND_SEAM_FIXTURE: handoff must be adjacent grounded normal-motion ticks");
                            handoffs++;
                            float nearJump = old.Near.DistanceTo(now.Near), farJump = old.Far.DistanceTo(now.Far);
                            if (old.Gait.NearStance && gait.NearStance)
                            { plantedComparisons++; worstJump = Mathf.Max(worstJump, nearJump); }
                            if (old.Gait.FarStance && gait.FarStance)
                            { plantedComparisons++; worstJump = Mathf.Max(worstJump, farJump); }
                            GD.Print($"GROUND_SEAM_HANDOFF direction={direction} case={caseName} tick={old.Tick}->{tick} owner={old.Owner}->{owner} " +
                                $"root={old.Root}->{now.Root} phase={old.Gait.Phase}->{gait.Phase} " +
                                $"near={old.Near}->{now.Near} near_stance={old.Gait.NearStance}->{gait.NearStance} near_jump={nearJump} " +
                                $"far={old.Far}->{now.Far} far_stance={old.Gait.FarStance}->{gait.FarStance} far_jump={farJump}");
                        }
                        if (revoke != null && !revokeWindow && owner == rightId && now.Root.X > 1280 && handoffs > 0)
                        {
                            // Original native RED/GREEN contact soles were y635.92 for saved top636:
                            // allow the .08px native contact margin plus numerical headroom, not foot drift.
                            bool nearOld = gait.NearStance && now.Near.X < 1279.85f && Mathf.Abs(now.Near.Y - 636) < .12f;
                            bool farOld = gait.FarStance && now.Far.X < 1279.85f && Mathf.Abs(now.Far.Y - 636) < .12f;
                            if (nearOld || farOld)
                            {
                                revokedSole = nearOld ? now.Near : now.Far;
                                revokeWindow = true;
                            }
                        }
                    }
                    previous = now;
                }
                catch (Exception error) { sampleFault = error; }
            };
            coordinator.FramePublished += onFrame;
            driver.Command(direction > 0 ? InputActions.MoveRight : InputActions.MoveLeft, true);
            for (int i = 0; i < 100 && (player.GlobalPosition.X - start.X) * direction < 220 && sampleFault == null; i++)
            {
                await Frames(tree, 1);
                if (revokeWindow && !revoked && sampleFault == null)
                {
                    // Native node edits occur outside publication, before the next Motor step.
                    TestAssert.True(previous.HasValue && previous.Value.Owner == rightId &&
                        previous.Value.Root.X > 1280 && revokedSole.X < 1279.85f,
                        "GROUND_SEAM_FIXTURE: nonempty live carry window before native withdrawal");
                    revokeAfterTick = previous.Value.Tick;
                    var oldShape = left.GetNode<CollisionShape2D>("Collision");
                    switch (revoke)
                    {
                        case "body-detach": root.RemoveChild(left); detached = left; break;
                        case "shape-detach": left.RemoveChild(oldShape); detached = oldShape; break;
                        case "disabled": oldShape.Disabled = true; break;
                        case "moved": left.Position += new Vector2(-100, 0); break;
                        case "shrunk":
                            var original = (RectangleShape2D)oldShape.Shape;
                            var copy = (RectangleShape2D)original.Duplicate();
                            copy.Size = new(4, 84);
                            oldShape.Shape = copy;
                            TestAssert.True(original.Size == new Vector2(880, 84) && copy != original,
                                "GROUND_SEAM_FIXTURE: shrink never mutates borrowed saved Shape resource");
                            break;
                        default: throw new InvalidOperationException("unknown native seam withdrawal");
                    }
                    bool noOldGeometry = !oldShape.IsInsideTree() || oldShape.Disabled;
                    if (!noOldGeometry)
                    {
                        Rect2 actual = SavedSeamWorldRect(left);
                        noOldGeometry = revokedSole.X < actual.Position.X || revokedSole.X > actual.End.X ||
                            Mathf.Abs(revokedSole.Y - actual.Position.Y) > .05f;
                    }
                    TestAssert.True(noOldGeometry,
                        $"GROUND_SEAM_FIXTURE: {revoke} really removes native support at old actual stance {revokedSole}");
                    revoked = true;
                }
            }
            if (sampleFault != null) throw new InvalidOperationException("GROUND_SEAM_SAMPLE_FAULT", sampleFault);
            TestAssert.True(owners.Contains(leftId) && owners.Contains(rightId) && handoffs > 0 &&
                plantedComparisons > 0 && walked >= 20 && matched == walked &&
                (player.GlobalPosition.X - start.X) * direction > 180 && driver.KeyDownCount == 1 &&
                driver.AttackEdges == 0 && driver.ClawEdges == 0 && driver.ShieldEdges == 0,
                $"GROUND_SEAM_FIXTURE: nonvacuous saved sample owners={owners.Count} handoffs={handoffs} " +
                $"planted={plantedComparisons} walk={walked} matched={matched} travel={player.GlobalPosition.X-start.X}");
            if (revoke != null) TestAssert.True(revokeWindow && revoked && revokeChecked,
                $"GROUND_SEAM_FIXTURE: native withdrawal must observe an old stance on new-owner floor kind={revoke} " +
                $"window={revokeWindow} revoked={revoked} next_physics={revokeChecked}");
            // Deliberately after all fixture guards: phase reset alone is not a failure oracle.
            TestAssert.True(worstJump <= .15f,
                $"GROUND_SEAM_CONTINUITY_RED: an already-stance actual Draw sole must remain planted across static " +
                $"same-Y zero-gap owner handoff; worst_world_jump={worstJump} epsilon=.15 comparisons={plantedComparisons}");
            GD.Print($"GROUND_SEAM_NATIVE_OK direction={direction} case={caseName} saved_geometry_sample=true whole_level_route=false " +
                (basin ? "same_sector=true geometry=BasinCampFloor-GearEntry complete_sector=false " : "") +
                $"walk={walked} handoffs={handoffs} planted={plantedComparisons} worst_world_jump={worstJump} " +
                "engine_keyboard=true position_injections_after_spawn=0 pixels=false human=false");
        }
        finally
        {
            if (coordinator != null && onFrame != null) coordinator.FramePublished -= onFrame;
            try { driver?.Dispose(); }
            finally
            {
                // Detached native nodes remain fixture-owned; never leak a parentless body/shape.
                if (detached != null && GodotObject.IsInstanceValid(detached) && detached.GetParent() == null)
                    root.AddChild(detached);
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
            }
        }
    }

    private static void AssertSeamStanceOnRight(Vector2 sole, Rect2 right, string revoke) =>
        TestAssert.True(sole.X >= right.Position.X - .05f && sole.X <= right.End.X + .05f &&
            // Same measured .08px native contact-plane offset as the carry-window guard.
            Mathf.Abs(sole.Y - right.Position.Y) < .12f,
            $"GROUND_SEAM_REVOKE_RED: {revoke} cannot keep an actual stance on revoked old geometry sole={sole} current={right}");

    private static void GroundSeamLiteralModelControls()
    {
        // Typed values exercise only the pure model; these are NOT claimed as native source tests.
        foreach (string control in new[] { "continuous", "gap4", "step4", "changed-proof", "missing-proof", "zero-time", "old-slope" })
        {
            var model = new PlayerGroundGait();
            var old = new PlayerGroundSample(true, new(82, 0), 101, Transform2D.Identity, new(82, 36), Vector2.Up)
                { HasBounds = true, SupportBounds = new(0, 36, 100, 10), Facing = 1 };
            model.Step(old, true, 1f / 60);
            var before = model.Step(old with { RootWorld = new(94, 0),
                GroundNormal = control == "old-slope" ? new Vector2(.1f, -1).Normalized() : Vector2.Up }, true, 1f / 60);
            TestAssert.True(before.NearStance && before.NearSoleWorld == new Vector2(90, 36),
                "GROUND_SEAM_MODEL_FIXTURE: literal old stance x90 before crossing x100");
            float floorX = control == "gap4" ? 104 : 100;
            float floorY = control == "step4" ? 40 : 36;
            var proof = new PlayerGroundSupportObservation(true, 101, Transform2D.Identity, new(0, 36, 100, 10));
            if (control == "changed-proof") proof = proof with { SupportTransform = new Transform2D(0, new Vector2(-1, 0)) };
            if (control == "missing-proof") proof = default;
            var next = old with { RootWorld = new(106, 0), SupportId = 102,
                SupportBounds = new(floorX, floorY, 100, 10), GroundPoint = new(106, floorY), PreviousSupport = proof };
            if (control == "zero-time")
                TestAssert.Equal(before, model.Step(next, true, 0), "zero-time seam cannot advance feet or consume handoff");
            var after = model.Step(next, true, 1f / 60);
            if (control is "continuous" or "zero-time")
                TestAssert.True(after.NearStance && after.NearSoleWorld.DistanceTo(new(90, 36)) < .001f,
                    "GROUND_SEAM_MODEL_RED: valid literal coplanar proof preserves world stance");
            else
            {
                if (after.NearStance) TestAssert.True(after.NearSoleWorld.X >= floorX && Mathf.Abs(after.NearSoleWorld.Y - floorY) < .001f,
                    "GROUND_SEAM_MODEL_REJECT_RED: incompatible seam cannot retain old near stance: " + control);
                if (after.FarStance) TestAssert.True(after.FarSoleWorld.X >= floorX && Mathf.Abs(after.FarSoleWorld.Y - floorY) < .001f,
                    "GROUND_SEAM_MODEL_REJECT_RED: incompatible seam cannot retain old far stance: " + control);
                TestAssert.True(after.NearStance || after.FarStance, "model rejection establishes a current-floor stance");
            }
        }
        GD.Print("GROUND_SEAM_MODEL_CONTROLS_OK cases=7 gap=4 step=4 changed_proof=true missing_proof=true zero_time=true old_slope=true native=false");
    }

    private static StaticBody2D TakeSavedSeamBody(string sectorName, string bodyName, Vector2 offset)
    {
        var sector = GD.Load<PackedScene>($"res://scenes/prologue/sectors/{sectorName}.tscn").Instantiate<Node2D>();
        try
        {
            var body = sector.GetNode<StaticBody2D>($"Gameplay/Collision/{bodyName}");
            Transform2D saved = body.Transform;
            for (Node parent = body.GetParent(); parent is Node2D transform; parent = parent.GetParent())
                saved = transform.Transform * saved;
            body.GetParent().RemoveChild(body);
            // Keep the saved native collision and rectangle resource, not gameplay scripts that require a full sector.
            foreach (Node child in body.GetChildren())
                if (child is not CollisionShape2D) { body.RemoveChild(child); child.Free(); }
            body.Transform = new Transform2D(saved.X, saved.Y, saved.Origin + offset);
            return body;
        }
        finally { sector.Free(); }
    }

    private static Rect2 SavedSeamWorldRect(StaticBody2D body)
    {
        var shape = body.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(shape.Shape is RectangleShape2D && !shape.Disabled,
            "GROUND_SEAM_FIXTURE: saved enabled rectangle collision");
        var rectangle = (RectangleShape2D)shape.Shape;
        Vector2 a = shape.ToGlobal(-rectangle.Size * .5f), b = shape.ToGlobal(rectangle.Size * .5f);
        return new Rect2(a, b - a);
    }

    private static ulong ActualSeamFloorShape(CharacterBody2D player)
    {
        for (int i = 0; i < player.GetSlideCollisionCount(); i++)
        {
            var contact = player.GetSlideCollision(i);
            if (contact.GetNormal().Dot(Vector2.Up) >= .7f &&
                contact.GetColliderShape() is CollisionShape2D shape && shape.Shape is RectangleShape2D)
                return shape.GetInstanceId();
        }
        return 0;
    }
}
