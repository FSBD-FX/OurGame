using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Config;
using Knight.Mechanisms;
using Knight.Prologue.Traversal;
using Knight.World;

namespace Knight.Tests.Integration;

public static class LandingImpactRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await Drop(tree, false);
        await Drop(tree, true);
        GD.Print("THRUSTER_IMPACT_PLAYER_OK automatic_keyboard=true saved_player=true terminal_retains_height=true real_jet_slow_landing=true height_origin_preserved=true standing_once=true human=false whole_route=false");
    }

    public static async Task RunNegatives(SceneTree tree)
    {
        await Drop(tree, true, jetGap: 20f, expectSafe: false);
        await Drop(tree, true, jetGap: 400f, expectSafe: false);
        await Drop(tree, false, supportMode: "soft");
        await Drop(tree, false, supportMode: "all-soft");
        await Drop(tree, true, supportMode: "mixed");
        await Drop(tree, false, supportMode: "non-bool");
        await Drop(tree, false, invulnerable: true);
        await Drop(tree, false, mutateAuthoredTuning: true);
        GD.Print("THRUSTER_IMPACT_NEGATIVE_OK late_jet_not_immunity=true fast_fall_after_jet=true all_soft=true mixed_hard_fallback=true non_bool_hard=true iframe_consumed_once=true tuning_snapshot=true human=false");
    }

    public static async Task RunMovingSupports(SceneTree tree)
    {
        await Drop(tree, false, floorSpeed: 120f);
        await Drop(tree, false, floorSpeed: -120f);
        await Drop(tree, true, floorSpeed: 120f);
        await Drop(tree, false, wallBeforeFloor: true);
        await InitialOverlapFallsBack(tree);
        await SustainedCarryDoesNotLandAgain(tree);
        await PureSnapDoesNotLandAgain(tree);
        await VerticalCarryDoesNotBecomeFallHeight(tree);
        GD.Print("THRUSTER_IMPACT_SUPPORT_OK moving_floor_up_down=true relative_normal_speed=true slow_moving_floor=true wall_floor_fallback=true initial_overlap_fallback=true sustained_carry_no_landing=true pure_snap_no_landing=true vertical_carry_origin=true automatic_physics=true human=false");
    }

    public static async Task RunReentry(SceneTree tree)
    {
        using var floorShape = new RectangleShape2D { Size = new Vector2(1200f, 20f) };
        var root = new Node2D { Name = "SamePlayerAirborneReentry" };
        var floor = new StaticBody2D
        {
            Position = new Vector2(0f, 60f), CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        var beforeParent = new Node2D { Name = "BeforeParent" };
        var afterParent = new Node2D { Name = "AfterParent" };
        root.AddChild(beforeParent); root.AddChild(afterParent);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 14f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        beforeParent.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        var claw = player.GetNode<Knight.Abilities.MechanicalClawController>("MechanicalClaw");
        ulong playerId = player.GetInstanceId(), motorId = motor.GetInstanceId(), inputId = input.GetInstanceId();
        ulong healthId = health.GetInstanceId(), clawId = claw.GetInstanceId();
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "same-player reentry starts on real legal support");
            TestAssert.Equal(5, health.CurrentHealth, "reentry starts with ordinary saved health");
            ulong landings = motor.LandingCount;
            ThrusterPlayerRuntimeTests.KeyEvent(Key.Space, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 2);
            TestAssert.True(!player.IsOnFloor() && player.Velocity.Y < 0f && motor.AcceptedJumpCount == 1,
                "real Space starts one ordinary jump and exercises the sampler before reentry");
            TestAssert.Equal(Knight.Abilities.MechanicalClawState.Idle, claw.CurrentFrame.State,
                "no attachment can own this ordinary jump");
            Vector2 beforePosition = player.GlobalPosition, beforeVelocity = player.Velocity;
            ulong beforeSequence = input.CurrentFrameSequence;
            player.Reparent(afterParent, true);
            TestAssert.True(player.IsInsideTree() && player.CanProcess(), "same player is live after reparent");
            TestAssert.Equal(playerId, player.GetInstanceId(), "reparent preserves actual player identity");
            TestAssert.Equal(motorId, motor.GetInstanceId(), "reparent does not substitute a new Motor");
            TestAssert.Equal(inputId, input.GetInstanceId(), "reparent retains the same input adapter");
            TestAssert.Equal(healthId, health.GetInstanceId(), "reparent retains the same health component");
            TestAssert.Equal(clawId, claw.GetInstanceId(), "reparent retains the same claw component");
            TestAssert.Equal(beforePosition, player.GlobalPosition, "reparent itself does not teleport the live player");
            TestAssert.Equal(beforeVelocity, player.Velocity, "reparent itself does not replace velocity");
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
            GD.Print($"IMPACT_REENTRY_DIAGNOSTIC before={beforePosition} after={player.GlobalPosition} " +
                $"velocity={player.Velocity} sampleBefore={beforeSequence} sampleAfter={input.CurrentFrameSequence} " +
                $"rawHeld={input.RawFrame.JumpHeld} held={input.CurrentFrame.JumpHeld} hp={health.CurrentHealth} " +
                $"claw={claw.CurrentFrame.State} acceptedJumps={motor.AcceptedJumpCount}");
            TestAssert.True(input.CurrentFrameSequence > beforeSequence && input.RawFrame.JumpHeld &&
                input.CurrentFrame.JumpHeld, "real held Space and sampling survive the same-player reentry");
            TestAssert.True(!input.RawFrame.DashPressed && !input.RawFrame.ClawPressed && !input.RawFrame.ShieldHeld,
                "no competing action explains reentry movement");
            TestAssert.True(player.GlobalPosition.Y < beforePosition.Y && player.Velocity.Y < 0f,
                "same airborne player continues ordinary physics after reentry");
            ThrusterPlayerRuntimeTests.Release();
            for (int tick = 0; tick < 150 && !player.IsOnFloor(); tick++)
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(player.IsOnFloor(), "same player completes the real jump and lands after reentry");
            TestAssert.Equal(landings + 1UL, motor.LandingCount, "reentry landing settles once");
            TestAssert.Equal(1, motor.AcceptedJumpCount, "reentry does not replay the held jump");
            TestAssert.Equal(5, health.CurrentHealth, "ordinary safe jump preserves health after reentry");
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.Equal(landings + 1UL, motor.LandingCount, "standing after reentry cannot settle again");
            GD.Print("THRUSTER_IMPACT_REENTRY_OK same_saved_player=true automatic_keyboard=true airborne_reparent=true jump_lands_once=true no_request_ready=true human=false");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    public static async Task RunIndependentDangers(SceneTree tree)
    {
        await SlowLandingThenRealDanger(tree, false);
        await SlowLandingThenRealDanger(tree, true);
        GD.Print("THRUSTER_IMPACT_DANGERS_OK actual_slow_landing=true real_hazard_overlap=true native_killzone_request=true damage_independent=true recovery_observer_only=true production_respawn_proved=false human=false");
    }

    private static async Task SlowLandingThenRealDanger(SceneTree tree, bool killZone)
    {
        const float floorTop = 2000f;
        using var floorShape = new RectangleShape2D { Size = new Vector2(1200f, 40f) };
        using var dangerShape = new RectangleShape2D { Size = new Vector2(80f, 80f) };
        // This existing test observer records a real recovery request, but cannot
        // teleport, respawn, heal or turn a dangerous route into a successful one.
        var root = new RangedFairnessRecoveryProbe { Name = killZone ? "SlowLandingThenKillZone" : "SlowLandingThenHazard" };
        var floor = new StaticBody2D
        {
            Position = new Vector2(0f, floorTop + 20f), CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        Area2D danger = killZone ? new KillZone { RoomControllerPath = new NodePath("..") }
            : new CanyonShieldLaneHazard();
        danger.Position = new Vector2(400f, floorTop - 36f);
        danger.CollisionLayer = CombatCollisionLayers.None;
        danger.CollisionMask = CombatCollisionLayers.Player;
        danger.Monitoring = true;
        danger.AddChild(new CollisionShape2D { Shape = dangerShape }); root.AddChild(danger);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, floorTop - 36f - 800f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.AddToGroup("player"); root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        int contacts = 0, injuries = 0;
        danger.BodyEntered += body => { if (body == player) contacts++; };
        health.Damaged += (_, _) => injuries++;
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            bool jetRequested = false;
            for (int tick = 0; tick < 240 && !player.IsOnFloor(); tick++)
            {
                float gap = floorTop - player.GlobalPosition.Y - 36f;
                if (!jetRequested && gap <= 80f)
                {
                    TestAssert.True(player.Velocity.Y >= 770f, "danger contrast begins with a genuine terminal fall");
                    jetRequested = true;
                    ThrusterPlayerRuntimeTests.KeyEvent(Key.W, true);
                    ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
                }
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
            }
            TestAssert.True(player.IsOnFloor() && motor.LandingCount == 1UL && motor.AcceptedDashCount == 1,
                "danger contrast first completes one real jet landing");
            TestAssert.True(motor.LastLandingImpact.IsReliable && motor.LastLandingImpact.Speed <= 300f &&
                motor.LastFallHeight > 750f && motor.LastFallDamage == 0 && health.CurrentHealth == 5,
                "independent danger begins only after reliable low-impact mitigation actually occurs");
            TestAssert.True(contacts == 0 && injuries == 0 && root.RecoveryRequests == 0,
                "danger is authored outside the entire vertical landing path");
            ThrusterPlayerRuntimeTests.Release();
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 30);
            TestAssert.True(!motor.IsDashing && player.IsOnFloor(), "walking contrast has no remaining jet burn");
            ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
            for (int tick = 0; tick < 200 && injuries == 0 && root.RecoveryRequests == 0; tick++)
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(contacts > 0 && danger.GetOverlappingBodies().Contains(player),
                "real keyboard walking produces an actual native danger overlap");
            TestAssert.True(player.IsOnFloor() && Math.Abs(player.Velocity.Y) < 1f,
                "independent danger remains harmful even at zero vertical impact speed");
            TestAssert.Equal(1UL, motor.LandingCount, "danger does not invent a new fall landing");
            TestAssert.Equal(0, motor.LastFallDamage, "old mitigated landing remains separate from danger settlement");
            if (killZone)
            {
                TestAssert.Equal(1, root.RecoveryRequests, "actual KillZone still delegates one recovery request");
                TestAssert.Equal(0, injuries, "KillZone recovery does not masquerade as height damage");
                TestAssert.Equal(5, health.CurrentHealth, "the request observer has not performed a hidden respawn/heal");
            }
            else
            {
                var lane = (CanyonShieldLaneHazard)danger;
                TestAssert.True(lane.Monitoring && lane.IsPhysicsProcessing() && lane.Damage == 1 &&
                    lane.PulseIntervalSeconds == .28f, "real lane uses its unchanged running damage settings");
                TestAssert.Equal(1, lane.PulseCount, "native hazard consumer actually pulses once");
                TestAssert.Equal(1, injuries, "actual hazard injury is not mitigated by the safe landing");
                TestAssert.Equal(4, health.CurrentHealth, "actual hazard damages health independently");
                TestAssert.Equal(0, root.RecoveryRequests, "the hazard contrast does not hide a recovery");
            }
            GD.Print($"IMPACT_INDEPENDENT_DANGER_OK kind={(killZone ? "KillZone" : "ShieldLane")} " +
                $"contacts={contacts} injuries={injuries} recovery={root.RecoveryRequests} hp={health.CurrentHealth} " +
                $"oldImpact={motor.LastLandingImpact.Speed} oldHeight={motor.LastFallHeight} " +
                $"position={player.GlobalPosition} velocity={player.Velocity} physical_D=true recovery_observer_only=true");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    private static async Task Drop(SceneTree tree, bool slowWithJet, float jetGap = 80f,
        bool expectSafe = true, string supportMode = "hard", bool invulnerable = false,
        bool mutateAuthoredTuning = false, float floorSpeed = 0f, bool wallBeforeFloor = false)
    {
        const float floorTop = 2000f;
        using var floorShape = new RectangleShape2D { Size = new Vector2(1200f, 40f) };
        using var overlapProbe = new KinematicCollision2D();
        using var recoveryProbe = new KinematicCollision2D();
        using var motionProbe = new KinematicCollision2D();
        var root = new Node2D { Name = slowWithJet ? "ActualSlowJetLanding" : "ActualTerminalLanding" };
        using var wallShape = new RectangleShape2D { Size = new Vector2(20f, 2200f) };
        PhysicsBody2D floor = floorSpeed == 0f
            ? new StaticBody2D { Position = new Vector2(0f, floorTop + 20f), CollisionLayer = CombatCollisionLayers.World }
            : new CanyonCounterweightLift
            {
                Name = "ActualMovingFloor", LowerPosition = new Vector2(0f, floorTop + 20f),
                Position = new Vector2(0f, floorTop + 20f),
                UpperPosition = new Vector2(0f, floorTop + 20f + floorSpeed * 100f),
                TravelSeconds = 100f, EndpointDwellSeconds = 0f, ProcessPhysicsPriority = -400
            };
        floor.AddChild(new CollisionShape2D { Shape = floorShape });
        if (supportMode is "soft" or "mixed" or "all-soft") floor.SetMeta("soft_landing", true);
        if (supportMode == "non-bool") floor.SetMeta("soft_landing", 1);
        root.AddChild(floor);
        if (wallBeforeFloor)
        {
            var wall = new StaticBody2D
            {
                Name = "GenuineSideWall", Position = new Vector2(40f, 1000f),
                CollisionLayer = CombatCollisionLayers.World
            };
            wall.AddChild(new CollisionShape2D { Shape = wallShape });
            root.AddChild(wall);
        }
        if (supportMode is "mixed" or "all-soft")
        {
            var secondSupport = new StaticBody2D
            {
                Position = floor.Position, CollisionLayer = CombatCollisionLayers.World
            };
            secondSupport.AddChild(new CollisionShape2D { Shape = floorShape });
            if (supportMode == "all-soft") secondSupport.SetMeta("soft_landing", true);
            root.AddChild(secondSupport);
        }
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, floorTop - 36f - 800f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        using var privateTuning = (PlayerTuning)motor.Tuning.Duplicate();
        motor.Tuning = privateTuning;
        var health = player.GetNode<HealthComponent>("Health");
        if (!slowWithJet && floorSpeed == 0f && !wallBeforeFloor) root.AddChild(new ThrusterPostMotorObserver
        {
            ProcessPhysicsPriority = -1,
            Observe = () =>
            {
                float gap = floorTop - player.GlobalPosition.Y - 36f;
                if (player.IsOnFloor() || gap > 16f) return;
                Vector2 motion = new(0f, 780f / 60f);
                bool overlap = player.TestMove(player.GlobalTransform, Vector2.Zero,
                    overlapProbe, player.SafeMargin, true);
                bool recovering = player.TestMove(player.GlobalTransform, motion,
                    recoveryProbe, player.SafeMargin, true);
                bool moving = player.TestMove(player.GlobalTransform, motion,
                    motionProbe, player.SafeMargin, false);
                GD.Print($"IMPACT_NATIVE_PROBE gap={gap} zero_recovery={overlap} true={recovering} false={moving} true_depth={recoveryProbe.GetDepth()} false_depth={motionProbe.GetDepth()} true_travel={recoveryProbe.GetTravel()} false_travel={motionProbe.GetTravel()} true_remainder={recoveryProbe.GetRemainder()} false_remainder={motionProbe.GetRemainder()}");
            }
        });
        int damageEvents = 0;
        health.Damaged += (_, _) => damageEvents++;
        ThrusterPlayerRuntimeTests.Release();
        tree.Root.AddChild(root);
        if (floor is CanyonCounterweightLift movingFloor) movingFloor.Activate();
        if (wallBeforeFloor) ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
        try
        {
            bool requestedJet = false;
            bool requestedInjury = false;
            bool sawWall = false;
            int frames = 0;
            while (!player.IsOnFloor() && frames++ < 240)
            {
                float remaining = floor.GlobalPosition.Y - 20f - (player.GlobalPosition.Y + 36f);
                sawWall |= player.IsOnWall();
                if (mutateAuthoredTuning && frames == 2)
                {
                    privateTuning.FallSafeImpactSpeed = 1000f;
                    privateTuning.MaxFallSpeed = 2000f;
                }
                if (invulnerable && !requestedInjury && remaining <= 80f)
                {
                    requestedInjury = true;
                    TestAssert.True(health.ReceiveDamage(new DamageRequest(1, Vector2.Zero, 0f,
                        811UL, floor.GetInstanceId())), "native landing starts inside a genuine combat i-frame");
                }
                if (slowWithJet && !requestedJet && remaining <= jetGap)
                {
                    TestAssert.True(player.Velocity.Y >= 770f,
                        "braking starts after a real terminal-speed fall, not a prepared slow fixture");
                    requestedJet = true;
                    ThrusterPlayerRuntimeTests.KeyEvent(Key.W, true);
                    ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
                    GD.Print($"IMPACT_JET_REQUEST gap={remaining} before={player.Velocity} human=false");
                }
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
            }
            TestAssert.True(player.IsOnFloor(), "saved Player reaches the original hard floor");
            TestAssert.Equal(1UL, motor.LandingCount, "actual landing is consumed once");
            TestAssert.True(motor.LastFallHeight > (floorSpeed == 0f ? 750f : 600f) &&
                motor.LastFallHeight < (floorSpeed == 0f ? 805f : 1200f),
                "jet key does not reset the original 800px fall height");
            TestAssert.Equal(slowWithJet ? 1 : 0, motor.AcceptedDashCount,
                "low-speed contrast uses one real accepted keyboard jet");
            float incoming = motor.UpdraftCandidateVelocity.Y;
            float relative = Math.Max(0f, incoming - floorSpeed);
            if (slowWithJet && expectSafe)
                TestAssert.True(relative >= 0f && relative <= 300f,
                    "real thrust slows the actual pre-contact motion below safety speed");
            else if (slowWithJet) TestAssert.True(incoming > 700f,
                "late or early jet key still produces an actual fast impact");
            else TestAssert.Near(780f, incoming, .01f, "terminal contrast actually strikes at 780");
            GD.Print($"IMPACT_LANDING_OBSERVED slow={slowWithJet} incoming_y={incoming} height={motor.LastFallHeight} damage={motor.LastFallDamage} hp={health.CurrentHealth} human=false");
            GD.Print($"IMPACT_MEASURED reason={motor.LastLandingImpact.Reason} speed={motor.LastLandingImpact.Speed} human=false");
            GD.Print($"IMPACT_NATIVE_CONTACT depth={player.GetSlideCollision(0).GetDepth()} travel={player.GetSlideCollision(0).GetTravel()} remainder={player.GetSlideCollision(0).GetRemainder()} count={player.GetSlideCollisionCount()}");
            bool multiple = supportMode is "mixed" or "all-soft" || wallBeforeFloor;
            TestAssert.Equal(!multiple, motor.LastLandingImpact.IsReliable,
                "only a unique genuine static floor contact has a reliable measured impact");
            if (!multiple) TestAssert.Near(relative, motor.LastLandingImpact.Speed, .02f,
                "impact sample uses relative pre-contact speed, not zero Y or doubled platform velocity");
            else TestAssert.Equal(LandingImpactFallbackReason.MultipleContacts,
                motor.LastLandingImpact.Reason, "second real support is not hidden by a single slide count");
            if (wallBeforeFloor) TestAssert.True(sawWall, "wall-floor fallback includes genuine earlier wall contact");
            if (floorSpeed != 0f) TestAssert.True(Math.Abs(floor.GlobalPosition.Y - floorTop - 20f) > 100f,
                "moving floor is a real translated carriage, not StaticBody constant velocity");
            bool soft = supportMode is "soft" or "all-soft";
            int expectedHealth = soft || slowWithJet && expectSafe && !multiple ? 5 : 4;
            TestAssert.Equal(expectedHealth, health.CurrentHealth,
                "reliable slow landing mitigates height damage while terminal impact retains it");
            TestAssert.Equal(expectedHealth == 5 ? 0 : 1, damageEvents, "landing emits only its effective injury");
            if (invulnerable)
            {
                TestAssert.Equal(1, motor.LastFallDamage, "i-frame rejection retains the real landing request");
                TestAssert.True(!motor.LastFallDamageApplied, "Health really rejects landing during its old i-frame");
            }
            ThrusterPlayerRuntimeTests.Release();
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 30);
            TestAssert.Equal(1UL, motor.LandingCount, "standing cannot consume or repeat the old landing");
            TestAssert.Equal(expectedHealth, health.CurrentHealth, "standing cannot apply deferred damage");
            TestAssert.True(health.ReceiveDamage(new DamageRequest(1, Vector2.Zero, 0f,
                812UL, floor.GetInstanceId())), "safe landing grants no persistent combat immunity");
            TestAssert.Equal(expectedHealth - 1, health.CurrentHealth, "combat remains independent of impact mitigation");
        }
        finally
        {
            ThrusterPlayerRuntimeTests.Release();
            if (GodotObject.IsInstanceValid(root)) root.Free();
        }
    }

    private static async Task InitialOverlapFallsBack(SceneTree tree)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(1200f, 40f) };
        using var query = new KinematicCollision2D();
        var root = new Node2D { Name = "InitialOverlapFallback" };
        var floor = new StaticBody2D { Position = new Vector2(0f, 2020f), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = shape });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 1980f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        ThrusterPlayerRuntimeTests.Release();
        tree.Root.AddChild(root);
        try
        {
            TestAssert.True(player.TestMove(player.GlobalTransform, Vector2.Zero, query, player.SafeMargin, true),
                "embedded fixture is a real initial native overlap");
            await ThrusterPlayerRuntimeTests.Frames(tree, player.GetNode<PlayerInputReader>("Input"), 4);
            var motor = player.GetNode<PlayerMotor>("Motor");
            TestAssert.True(player.IsOnFloor(), "native recovery resolves initial overlap onto support");
            TestAssert.Equal(1UL, motor.LandingCount, "recovery landing consumes exactly once");
            TestAssert.Equal(LandingImpactFallbackReason.InitialOverlap, motor.LastLandingImpact.Reason,
                "initial recovery is not legitimized by the positive contact-depth comparison");
            TestAssert.True(!motor.LastLandingImpact.IsReliable, "embedded fallback never becomes a safe zero speed");
            TestAssert.Equal(5, player.GetNode<HealthComponent>("Health").CurrentHealth,
                "initial birth recovery does not invent a descent height");
            GD.Print($"IMPACT_INITIAL_OVERLAP_OK reason={motor.LastLandingImpact.Reason} height={motor.LastFallHeight} damage={motor.LastFallDamage}");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    private static async Task SustainedCarryDoesNotLandAgain(SceneTree tree)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(1200f, 40f) };
        var root = new Node2D { Name = "SustainedCarryNoLanding" };
        var lift = new CanyonCounterweightLift
        {
            Position = new Vector2(0f, 70f), LowerPosition = new Vector2(0f, 70f),
            UpperPosition = new Vector2(240f, 70f), TravelSeconds = 2f,
            EndpointDwellSeconds = 0f, ProcessPhysicsPriority = -400
        };
        lift.AddChild(new CollisionShape2D { Shape = shape });
        root.AddChild(lift);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 14f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        int injuries = 0;
        health.Damaged += (_, _) => injuries++;
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "passenger starts on a real stationary carriage");
            ulong landings = motor.LandingCount;
            LandingImpactSample oldImpact = motor.LastLandingImpact;
            Vector2 start = player.GlobalPosition;
            float relativeX = player.GlobalPosition.X - lift.GlobalPosition.X;
            bool carriedVelocity = false;
            lift.Activate();
            for (int tick = 0; tick < 30; tick++)
            {
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
                TestAssert.True(player.IsOnFloor(), "real carriage motion keeps support every tick");
                carriedVelocity |= player.GetPlatformVelocity().X > 100f;
            }
            TestAssert.True(lift.GlobalPosition.X > 50f && player.GlobalPosition.X - start.X > 50f && carriedVelocity,
                "native platform and passenger both translate with nonzero platform velocity");
            TestAssert.Near(relativeX, player.GlobalPosition.X - lift.GlobalPosition.X, .1f,
                "passenger is actually carried, not independently driven");
            TestAssert.Equal(landings, motor.LandingCount, "continuous carry cannot consume another landing");
            TestAssert.Equal(oldImpact, motor.LastLandingImpact, "carry does not overwrite historical impact with zero");
            TestAssert.Equal(0, injuries, "carry is not fall injury");
            TestAssert.Equal(5, health.CurrentHealth, "carried passenger retains health");
            GD.Print($"IMPACT_CARRY_OK player_delta={player.GlobalPosition - start} platform={lift.GlobalPosition} landings={landings} native_platform_velocity={player.GetPlatformVelocity()}");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    private static async Task PureSnapDoesNotLandAgain(SceneTree tree)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(400f, 40f) };
        using var motionProbe = new KinematicCollision2D();
        using var recoveryProbe = new KinematicCollision2D();
        var root = new Node2D { Name = "PureSnapNoLanding" };
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        float step = player.FloorSnapLength / 2f;
        TestAssert.True(step > player.SafeMargin * 2f, "authored snap length permits an independently distinguishable step");
        foreach (Vector2 position in new[] { new Vector2(-200f, 70f), new Vector2(200f, 70f + step) })
        {
            var floor = new StaticBody2D { Position = position, CollisionLayer = CombatCollisionLayers.World };
            floor.AddChild(new CollisionShape2D { Shape = shape }); root.AddChild(floor);
        }
        player.Position = new Vector2(-160f, 14f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        bool observing = false, priorFloor = false, motionBlocked = false, snapSeen = false;
        Vector2 priorPosition = default, recoveryTravel = default, platformVelocity = default;
        root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = -1, Observe = () =>
        {
            if (!observing) return;
            priorFloor = player.IsOnFloor(); priorPosition = player.GlobalPosition;
            platformVelocity = player.GetPlatformVelocity();
            motionBlocked = player.TestMove(player.GlobalTransform,
                new Vector2(player.Velocity.X / 60f, 0f), motionProbe, player.SafeMargin, false);
            bool recovery = player.TestMove(player.GlobalTransform, Vector2.Zero,
                recoveryProbe, player.SafeMargin, true);
            recoveryTravel = recovery ? recoveryProbe.GetTravel() : Vector2.Zero;
        } });
        root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
        {
            if (!observing || !priorFloor || !player.IsOnFloor() || motionBlocked ||
                platformVelocity != Vector2.Zero || motor.UpdraftCandidateVelocity.Y != 0f ||
                recoveryTravel.Y > .001f || player.GlobalPosition.Y - priorPosition.Y <= .01f) return;
            snapSeen = true;
            GD.Print($"IMPACT_PURE_SNAP_TICK from={priorPosition} to={player.GlobalPosition} commanded_y={motor.UpdraftCandidateVelocity.Y} recovery_travel={recoveryTravel} ordinary_motion_blocked={motionBlocked}");
        } });
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "snap route starts grounded on the original left floor");
            ulong landings = motor.LandingCount;
            LandingImpactSample oldImpact = motor.LastLandingImpact;
            float startY = player.GlobalPosition.Y;
            observing = true; ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
            for (int tick = 0; tick < 150 && player.GlobalPosition.X < 60f; tick++)
            {
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
                TestAssert.True(player.IsOnFloor(), "automatic snap never passes through an airborne tick");
            }
            observing = false; ThrusterPlayerRuntimeTests.Release();
            TestAssert.True(player.GlobalPosition.X >= 60f && snapSeen,
                "route crosses the step and captures a real downward snap not explained by ordinary motion/recovery");
            TestAssert.Near(step, player.GlobalPosition.Y - startY, .1f, "player actually descends the authored half-snap step");
            TestAssert.Equal(landings, motor.LandingCount, "pure snap cannot consume another landing");
            TestAssert.Equal(oldImpact, motor.LastLandingImpact, "pure snap does not invent a new impact result");
            TestAssert.Equal(5, health.CurrentHealth, "automatic snap keeps ordinary standing health");
            GD.Print($"IMPACT_PURE_SNAP_OK step={step} landings={landings} physics_keyboard=true");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    private static async Task VerticalCarryDoesNotBecomeFallHeight(SceneTree tree)
    {
        using var liftShape = new RectangleShape2D { Size = new Vector2(120f, 40f) };
        using var floorShape = new RectangleShape2D { Size = new Vector2(2000f, 40f) };
        var root = new Node2D { Name = "VerticalCarryFallOrigin" };
        var lift = new CanyonCounterweightLift
        {
            Position = new Vector2(0f, 70f), LowerPosition = new Vector2(0f, 70f),
            UpperPosition = new Vector2(0f, 1070f), TravelSeconds = 4f,
            EndpointDwellSeconds = 20f, ProcessPhysicsPriority = -400
        };
        lift.AddChild(new CollisionShape2D { Shape = liftShape }); root.AddChild(lift);
        var floor = new StaticBody2D { Position = new Vector2(0f, 2020f), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 14f); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "vertical carry begins from real support");
            ulong landings = motor.LandingCount;
            lift.Activate();
            for (int tick = 0; tick < 230 && lift.GlobalPosition.Y < 820f; tick++)
            {
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
                TestAssert.True(player.IsOnFloor(), "long vertical carry has no air gaps");
                TestAssert.Equal(landings, motor.LandingCount, "supported descent is not repeated landing");
            }
            TestAssert.True(lift.GlobalPosition.Y >= 820f && player.GlobalPosition.Y > 760f,
                "passenger actually descends more than 700 supported pixels");
            float lastSupportedY = player.GlobalPosition.Y, freeApex = float.PositiveInfinity;
            bool departed = false;
            ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
            for (int tick = 0; tick < 260; tick++)
            {
                await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
                if (!player.IsOnFloor())
                {
                    if (!departed) { departed = true; freeApex = lastSupportedY; }
                    freeApex = Math.Min(freeApex, player.GlobalPosition.Y);
                }
                else if (departed) break;
                else lastSupportedY = player.GlobalPosition.Y;
            }
            TestAssert.True(departed && player.IsOnFloor(), "keyboard walking genuinely leaves the carriage and lands below");
            float observedFreeHeight = player.GlobalPosition.Y - freeApex;
            GD.Print($"IMPACT_VERTICAL_CARRY_LANDING supported_departure_y={freeApex} landing_y={player.GlobalPosition.Y} observed_free_height={observedFreeHeight} tracked_height={motor.LastFallHeight} damage={motor.LastFallDamage}");
            TestAssert.True(observedFreeHeight > 1000f && observedFreeHeight < 1400f,
                "the independent actual free descent is in the two-damage band");
            TestAssert.Near(observedFreeHeight, motor.LastFallHeight, 15f,
                "supported platform travel is excluded within one completed terminal-speed physics sample");
            TestAssert.Equal(2, motor.LastFallDamage, "only real free height determines the terminal injury");
            TestAssert.Equal(3, player.GetNode<HealthComponent>("Health").CurrentHealth,
                "a carried descent cannot become extra fall injury");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }
}
