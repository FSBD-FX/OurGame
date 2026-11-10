using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    private sealed record AirUpPhysicsTrace(List<Vector2> Positions, List<Vector2> Velocities);

    // Catches Up-J sharing the airborne horizontal carrier pose rather than
    // articulating an upward cut. Native keys and controlled rig are separate.
    public static async Task RunAirUpAttack(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (int facing in new[] { 1, -1 })
        {
            AirUpPhysicsTrace horizontal = null, upward = null;
            try { horizontal = await ObserveNativeAirUp(tree, facing, false); }
            catch (Exception error) { failures.Add($"native horizontal facing={facing}: {error.Message}"); }
            try { upward = await ObserveNativeAirUp(tree, facing, true); }
            catch (Exception error) { failures.Add($"native Up facing={facing}: {error.Message}"); }
            if (horizontal != null && upward != null)
            {
                try
                {
                    TestAssert.True(horizontal.Positions.Count >= 18 && upward.Positions.Count >= 18,
                        "both actual directions retain an original full attack-length physics trace");
                    for (int i = 0; i < 18; i++)
                    {
                        TestAssert.True(horizontal.Positions[i].DistanceTo(upward.Positions[i]) < .02f &&
                            horizontal.Velocities[i].DistanceTo(upward.Velocities[i]) < .02f,
                            "Up arm animation cannot alter the matched actual jump trajectory at physical sample " + i);
                    }
                }
                catch (Exception error) { failures.Add("physics pair: " + error.Message); }
            }
        }
        ReleaseInput();
        var root = new Node2D { Name = "AirUpControlledRig" };
        var actor = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        actor.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(actor); tree.Root.AddChild(root);
        try
        {
            await Frames(tree, 2);
            AssertControlledAirUp(actor.GetNode<PlayerFormalVisual>("VisualRoot/Formal"));
        }
        catch (Exception error) { failures.Add("controlled rig: " + error.Message); }
        finally { await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root); }
        TestAssert.True(failures.Count == 0, "AIR_UP_ATTACK: " + string.Join(" | ", failures));
        GD.Print("AIR_UP_ATTACK_OK saved_player=true real_space_WJ=true facings=2 matched_horizontal=true phases=4 actual_up_hitbox=true natural_landing=true physics_pair=true typed_rates=30,60,120 directional_blade=true articulated_wrist=true original_grips=true fixed_links=true legs_preserved=true same_clock=true no_stretch=true gameplay_unchanged=true headless_geometry_only=true human=false");
    }

    private static Vector2 AirUpSourcePoint(PlayerFormalLayer weapon, Vector2 source) =>
        weapon.ToGlobal(weapon.WeaponDrawRect.Position + (source - weapon.WeaponSourceRect.Position) * (25f / 287f));

    private static void AssertAirUpGrips(PlayerFormalVisual formal)
    {
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        AssertArmChain(near); AssertArmChain(far);
        float nearGap = AirUpSourcePoint(weapon, new(835, 798)).DistanceTo(near.ToGlobal(near.ArmWristLocal));
        float farGap = AirUpSourcePoint(weapon, new(548, 798)).DistanceTo(far.ToGlobal(far.ArmWristLocal));
        TestAssert.True(nearGap < .002f && farGap < .002f && weapon.WeaponSecondaryGripContact,
            $"actual airborne original bitmap grips remain held: near={nearGap:R} far={farGap:R} requested_far={far.ToLocal(AirUpSourcePoint(weapon, new(548, 798))).Length():R}");
        foreach (string slot in Layers)
            TestAssert.Equal(Vector2.One, formal.GetNode<Node2D>(slot).Scale, "air Up cannot stretch " + slot);
        TestAssert.True(!formal.CurrentActionPose.Owns(PlayerPoseSlot.NearLeg) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.FarLeg), "air attack leaves locomotion leg slots alone");
    }

    private static async Task<AirUpPhysicsTrace> ObserveNativeAirUp(SceneTree tree, int facing, bool upward)
    {
        ReleaseInput();
        var root = new Node2D { Name = "AirUpKeyboardFixture" };
        var floor = new StaticBody2D { Position = new(400, 446), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new(1600, 20) } });
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new(400, 400);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        var observer = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
        root.AddChild(floor); root.AddChild(player); root.AddChild(observer); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var combat = player.GetNode<PlayerCombat>("Combat");
        var hitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
        var health = player.GetNode<HealthComponent>("Health");
        var coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        var phases = new HashSet<PlayerActionPhase>();
        var positions = new List<Vector2>(); var velocities = new List<Vector2>();
        Vector2 groundedRoot = default;
        int starts = 0, direction = -1, hits = 0, samples = 0, observerFailures = coordinator.FrameObserverFailureCount;
        int openings = 0, open = -1, close = -1, finish = -1;
        ulong originalId = 0;
        bool monitoring = false;
        float maxRise = float.NegativeInfinity, maxBladeUp = -1;
        Exception probeFault = null;
        void Started(ulong id, int aim) { starts++; originalId = id; direction = aim; }
        void Connected(Hurtbox _) => hits++;
        void Published(PlayerPresentationFrame frame)
        {
            if (frame.Action.State != PlayerActionState.AirAttack || probeFault != null) return;
            try
            {
                TestAssert.True(!frame.Base.Grounded && frame.Action.Aim ==
                    (upward ? Vector2.Up : new Vector2(facing, 0)) && frame.Base.Facing == facing,
                    "actual airborne attack locks intended direction/facing even after W is released");
                TestAssert.True(hitbox.Position == (upward ? new Vector2(0, -42) : new Vector2(facing * 42, 0)),
                    "actual visual direction matches production hitbox offset");
                AssertAirUpGrips(formal);
                phases.Add(frame.Action.Phase); samples++;
                if (frame.Action.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact)
                {
                    maxRise = Mathf.Max(maxRise, near.GlobalPosition.Y - near.ToGlobal(near.ArmWristLocal).Y);
                    maxBladeUp = Mathf.Max(maxBladeUp,
                        (AirUpSourcePoint(weapon, new(1180, 812)) - AirUpSourcePoint(weapon, new(835, 798))).Normalized().Dot(Vector2.Up));
                }
            }
            catch (Exception error) { probeFault = error; }
        }
        try
        {
            await Frames(tree, 30);
            KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, false);
            await Frames(tree, 20);
            TestAssert.True(player.IsOnFloor() && player.Velocity.Length() < .01f, "air Up starts on actual floor at rest");
            groundedRoot = player.GlobalPosition;
            int originalHealth = health.CurrentHealth;
            combat.AttackStarted += Started; combat.AttackConnected += Connected; coordinator.FramePublished += Published;
            observer.Sample = _ =>
            {
                if (starts == 0 || positions.Count >= 48) return;
                int ordinal = positions.Count;
                positions.Add(player.GlobalPosition - groundedRoot); velocities.Add(player.Velocity);
                if (hitbox.Monitoring && !monitoring) { openings++; if (open < 0) open = ordinal; }
                if (!hitbox.Monitoring && monitoring && close < 0) close = ordinal;
                if (!combat.IsAttacking && finish < 0) finish = ordinal;
                monitoring = hitbox.Monitoring;
            };
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            TestAssert.True(!player.IsOnFloor() && motor.AcceptedJumpCount == 1 && player.Velocity.Y < 0,
                "normal Space really leaves floor, no pose/root injection or assisted jump");
            if (upward) KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false); KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            await Frames(tree, 36);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
            await Frames(tree, 60);
            if (probeFault != null) throw probeFault;
            GD.Print($"AIR_UP_NATIVE_MEASURE facing={facing} upward={upward} phases={phases.Count} samples={samples} wrist_rise={maxRise:R} blade_up={maxBladeUp:R} starts={starts} jump={motor.AcceptedJumpCount} open={open} close={close} finish={finish}");
            TestAssert.True(starts == 1 && originalId != 0 && direction ==
                (int)(upward ? AttackDirection.Up : AttackDirection.Horizontal) && hits == 0 && phases.Count == 4 && samples > 8 &&
                openings == 1 && open == 3 && close == 9 && finish == 17,
                "actual Space/J or Space/WJ has one original full four-stage air attack and unchanged hitbox window");
            TestAssert.True(motor.AcceptedJumpCount == 1 && health.CurrentHealth == originalHealth && player.IsOnFloor() &&
                Mathf.Abs(player.GlobalPosition.X - groundedRoot.X) < .02f && !combat.IsAttacking &&
                coordinator.LastPlayerFrame.Action.State == PlayerActionState.None &&
                near.ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .01f && weapon.WeaponSecondaryGripContact,
                "actual jump naturally lands and settles with no extra jump, damage, horizontal root motion or missing final grip");
            foreach (string legName in new[] { "NearLeg", "FarLeg" })
            {
                var leg = formal.GetNode<PlayerFormalLayer>(legName);
                TestAssert.True(Mathf.Abs(leg.ToGlobal(leg.LegSoleLocal).Y - 436) < .2f,
                    "natural air Up landing preserves actual supported soles");
            }
            TestAssert.Equal(observerFailures, coordinator.FrameObserverFailureCount, "native air Up assertions cannot be swallowed");
            if (upward)
                TestAssert.True(maxRise > 4f && maxBladeUp > .90f,
                    $"REAL_AIR_UP_DIRECTION_RED: actual WJ must visibly lift wrist and blade upwards, rise={maxRise:R} blade={maxBladeUp:R}");
            return new(positions, velocities);
        }
        finally
        {
            observer.Sample = null; combat.AttackStarted -= Started; combat.AttackConnected -= Connected;
            coordinator.FramePublished -= Published;
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false); KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false); KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false); ReleaseInput();
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
        }
    }

    private static void AssertControlledAirUp(PlayerFormalVisual formal)
    {
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        var legs = new[] { formal.GetNode<PlayerFormalLayer>("NearLeg"), formal.GetNode<PlayerFormalLayer>("FarLeg") };
        var tuning = formal.GetNode<PlayerMotor>("../../Motor").Tuning;
        Vector2 root = formal.GlobalPosition;
        int cases = 0;
        foreach (int facing in new[] { 1, -1 })
        foreach (int hz in new[] { 30, 60, 120 })
        {
            var baselineBlade = new Vector2[hz + 1]; var baselineWrist = new Vector2[hz + 1];
            var baselineLegs = new Transform2D[hz + 1, 2];
            foreach (var direction in new[] { AttackDirection.Horizontal, AttackDirection.Up })
            {
                formal.PlayCue(PresentationCue.Died);
                for (int i = 0; i < 20; i++)
                    formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, 0, PlayerActionFrame.None, facing: facing));
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Fall, 0, PlayerActionFrame.None, facing: facing)
                    with { PresentationTimeSeconds = 0 });
                var timeline = new PlayerPresentationTimeline(tuning);
                var air = new PlayerPresentationInput(Vector2.Zero, false, false, false, false, facing);
                formal.ApplyPlayerFrame(timeline.Step(air, 0));
                var phases = new HashSet<PlayerActionPhase>();
                float rise = 0, turn = 0, minimum = 21, maximum = 21, largestStep = 0;
                Vector2 previous = near.ArmWristLocal;
                bool refreshed = false;
                int activeTicks = (int)MathF.Ceiling((tuning.AttackStartupSeconds + tuning.AttackActiveSeconds + tuning.AttackRecoverySeconds) * hz);
                for (int tick = 1; tick <= hz; tick++)
                {
                    var frame = timeline.Step(tick <= activeTicks ? air with
                        { AttackId = 3281, IsAttacking = true, AttackDirection = direction } : air, 1f / hz);
                    formal.ApplyPlayerFrame(frame); AssertAirUpGrips(formal);
                    TestAssert.Equal(root, formal.GlobalPosition, "controlled air arm trajectory cannot move root");
                    TestAssert.True(frame.Action.State is PlayerActionState.AirAttack or PlayerActionState.None,
                        "controlled air comparison uses actual airborne Timeline producer");
                    if (frame.Action.State == PlayerActionState.AirAttack)
                    {
                        phases.Add(frame.Action.Phase);
                        minimum = Mathf.Min(minimum, near.ArmWristLocal.Length());
                        maximum = Mathf.Max(maximum, near.ArmWristLocal.Length());
                    }
                    largestStep = Mathf.Max(largestStep, previous.DistanceTo(near.ArmWristLocal)); previous = near.ArmWristLocal;
                    Vector2 blade = formal.ToLocal(weapon.ToGlobal(Vector2.Right)) - formal.ToLocal(weapon.GlobalPosition);
                    Vector2 wrist = formal.ToLocal(near.ToGlobal(near.ArmWristLocal));
                    if (direction == AttackDirection.Horizontal) { baselineBlade[tick] = blade; baselineWrist[tick] = wrist; }
                    else if (frame.Action.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact)
                    {
                        rise = Mathf.Max(rise, baselineWrist[tick].Y - wrist.Y);
                        turn = Mathf.Max(turn, -baselineBlade[tick].AngleTo(blade));
                        if (!refreshed)
                        {
                            Vector2 nw = near.ArmWristLocal, ne = near.ArmElbowLocal, fw = far.ArmWristLocal, fe = far.ArmElbowLocal;
                            Transform2D nt = near.GlobalTransform, ft = far.GlobalTransform, wt = weapon.GlobalTransform;
                            for (int refresh = 0; refresh < 4; refresh++) formal.ApplyPlayerFrame(frame);
                            TestAssert.True(near.ArmWristLocal.IsEqualApprox(nw) && near.ArmElbowLocal.IsEqualApprox(ne) &&
                                far.ArmWristLocal.IsEqualApprox(fw) && far.ArmElbowLocal.IsEqualApprox(fe) &&
                                near.GlobalTransform.IsEqualApprox(nt) && far.GlobalTransform.IsEqualApprox(ft) && weapon.GlobalTransform.IsEqualApprox(wt),
                                "same-clock air Up refresh cannot advance internal wrist/elbow/grips or carrier poses");
                            refreshed = true;
                        }
                    }
                    for (int i = 0; i < 2; i++)
                        if (direction == AttackDirection.Horizontal) baselineLegs[tick, i] = legs[i].Transform;
                        else TestAssert.True(baselineLegs[tick, i].IsEqualApprox(legs[i].Transform),
                            "Up arm overlay preserves identical actual locomotion legs");
                }
                TestAssert.True(phases.Count == 4 && largestStep < 8 && near.ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .001f,
                    "air direction uses all original phases, bounded wrist motion and neutral return");
                if (direction == AttackDirection.Up)
                {
                    GD.Print($"AIR_UP_DIRECTION_MEASURE facing={facing} hz={hz} wrist_rise={rise:R} blade_turn={turn:R} min_reach={minimum:R} max_reach={maximum:R} max_step={largestStep:R}");
                    TestAssert.True(rise > 6 && turn > .35f,
                        $"AIR_UP_DIRECTION_RED: actual saved rig must distinguish upward cut from same-clock horizontal blade, rise={rise:R} turn={turn:R}");
                    TestAssert.True(minimum < 20.75f && maximum > 21.5f && refreshed,
                        "dedicated air Up wrist actually contracts/extends, preserving fixed links and zero-clock behavior");
                    cases++;
                }
            }
        }
        TestAssert.Equal(6, cases, "both facings at all three rates execute directional rig comparison");
        GD.Print("AIR_UP_CONTROLLED_RIG_OK cases=6 rates=30,60,120 same_air_base=true directional_blade=true articulated_wrist=true original_grips=true fixed_links=true legs_preserved=true same_clock=true pixels=false human=false");
    }
}
