using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    public static async Task RunGroundUpAttack(SceneTree tree)
    {
        int nativeCases = 0, observedSamples = 0;
        foreach (int facing in new[] { 1, -1 })
        {
            ReleaseInput();
            var root = new Node2D { Name = "GroundUpKeyboardFixture" };
            var floor = new StaticBody2D { Position = new(0, 50), CollisionLayer = CombatCollisionLayers.World };
            floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new(1600, 20) } });
            var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            root.AddChild(floor); root.AddChild(player); tree.Root.AddChild(root);
            var coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var near = formal.GetNode<PlayerFormalLayer>("NearArm");
            var far = formal.GetNode<PlayerFormalLayer>("FarArm");
            var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
            var combat = player.GetNode<PlayerCombat>("Combat");
            var hitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
            var health = player.GetNode<HealthComponent>("Health");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var phases = new HashSet<PlayerActionPhase>();
            Exception probeFault = null;
            int attackStarts = 0, pass = 1;
            bool peakSeen = false, zeroClockChecked = false, hurtSeen = false, upResumedAfterHurt = false;
            float maxRise = 0, maxBladeUp = -1;
            float maximumHandoffStep = 0f;
            Vector2? previousHandoffWrist = null;
            Vector2 groundedRoot = default;
            int observerFailures = coordinator.FrameObserverFailureCount;
            combat.AttackStarted += (_, _) => attackStarts++;
            void Observe(PlayerPresentationFrame frame)
            {
                if (pass == 2 && frame.Action.State == PlayerActionState.Hurt) hurtSeen = true;
                if (pass == 2 && hurtSeen && frame.Action.State == PlayerActionState.GroundAttack)
                    upResumedAfterHurt = true;
                if (pass == 2)
                {
                    Vector2 wrist = near.ToGlobal(near.ArmWristLocal);
                    if (previousHandoffWrist is Vector2 previous)
                        maximumHandoffStep = Mathf.Max(maximumHandoffStep, previous.DistanceTo(wrist));
                    previousHandoffWrist = wrist;
                }
                if (probeFault != null || frame.Action.State != PlayerActionState.GroundAttack) return;
                try
                {
                    TestAssert.True(frame.Base.Grounded && frame.Action.Aim == Vector2.Up &&
                        hitbox.Position == new Vector2(0, -42), "real W/J keeps Up aim and matching ground hitbox after W release");
                    TestAssert.Equal(facing, frame.Base.Facing, "Up-J preserves the previously established facing");
                    AssertArmChain(near); AssertArmChain(far);
                    Vector2 Source(float x, float y) => weapon.ToGlobal(weapon.WeaponDrawRect.Position +
                        (new Vector2(x, y) - weapon.WeaponSourceRect.Position) * (25f / 287f));
                    TestAssert.True(Source(835, 798).DistanceTo(near.ToGlobal(near.ArmWristLocal)) < .002f,
                        "real Up-J retains the original primary bitmap grip");
                    // Hurt legitimately releases the offhand; the existing
                    // elapsed-driven regrip must not be mistaken for a full
                    // two-handed pose on its first partially resumed frame.
                    if (!hurtSeen)
                        TestAssert.True(Source(548, 798).DistanceTo(far.ToGlobal(far.ArmWristLocal)) < .002f &&
                            weapon.WeaponSecondaryGripContact, "uninterrupted Up-J retains both original bitmap grips");
                    foreach (string slot in Layers)
                        TestAssert.Equal(Vector2.One, formal.GetNode<Node2D>(slot).Scale, "Up-J cannot stretch " + slot);
                    foreach (string name in new[] { "NearLeg", "FarLeg" })
                    {
                        var leg = formal.GetNode<PlayerFormalLayer>(name);
                        TestAssert.True(Mathf.Abs(leg.ToGlobal(leg.LegSoleLocal).Y - 40f) < .2f,
                            "raised arms retain actual floor-supported soles");
                    }
                    TestAssert.True(player.GlobalPosition.DistanceTo(groundedRoot) < .02f &&
                        player.Velocity.Length() < .01f && motor.AcceptedJumpCount == 0,
                        "Up-J animation cannot inject root motion, velocity or a jump");
                    if (pass == 1)
                    {
                        phases.Add(frame.Action.Phase);
                        if (frame.Action.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact)
                        {
                            float rise = near.GlobalPosition.Y - near.ToGlobal(near.ArmWristLocal).Y;
                            maxRise = Mathf.Max(maxRise, rise);
                            maxBladeUp = Mathf.Max(maxBladeUp,
                                (Source(1180, 812) - Source(835, 798)).Normalized().Dot(Vector2.Up));
                            peakSeen |= rise > 4f;
                            if (rise > 4f && !zeroClockChecked)
                            {
                                Transform2D nearBefore = near.GlobalTransform, weaponBefore = weapon.GlobalTransform;
                                Vector2 wristBefore = near.ArmWristLocal, farBefore = far.ArmWristLocal;
                                for (int i = 0; i < 4; i++) formal.ApplyPlayerFrame(frame);
                                TestAssert.True(near.GlobalTransform.IsEqualApprox(nearBefore) &&
                                    weapon.GlobalTransform.IsEqualApprox(weaponBefore) &&
                                    near.ArmWristLocal.IsEqualApprox(wristBefore) && far.ArmWristLocal.IsEqualApprox(farBefore),
                                    "same-clock Up-J refresh cannot advance shoulder, wrist or grip motion");
                                zeroClockChecked = true;
                            }
                        }
                        observedSamples++;
                    }
                }
                catch (Exception error) { probeFault = error; }
            }
            try
            {
                await Frames(tree, 30);
                KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, true);
                await Frames(tree, 2);
                KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, false);
                await Frames(tree, 20);
                TestAssert.True(player.IsOnFloor() && player.Velocity.Length() < .01f,
                    "Up-J fixture starts naturally grounded and stopped");
                groundedRoot = player.GlobalPosition;
                int originalHealth = health.CurrentHealth;
                coordinator.FramePublished += Observe;
                async Task UpJ()
                {
                    KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
                    await Frames(tree, 1);
                    KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
                    await Frames(tree, 2);
                    KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
                    KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
                }
                await UpJ();
                for (int i = 0; i < 90 && !peakSeen && probeFault == null; i++) await Frames(tree, 1);
                if (probeFault != null) throw probeFault;
                TestAssert.True(peakSeen, "REAL_GROUND_UP_RED: actual W/J must raise the wrist above its shoulder");
                Transform2D pausedNear = near.GlobalTransform, pausedWeapon = weapon.GlobalTransform;
                tree.Paused = true;
                for (int i = 0; i < 4; i++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                TestAssert.True(near.GlobalTransform.IsEqualApprox(pausedNear) &&
                    weapon.GlobalTransform.IsEqualApprox(pausedWeapon), "paused Up-J freezes the actual rig");
                tree.Paused = false;
                await Frames(tree, 70);
                if (probeFault != null) throw probeFault;
                TestAssert.True(phases.Count == 4 && zeroClockChecked && maxRise > 4f && maxBladeUp > .85f &&
                    attackStarts == 1 && !combat.IsAttacking && coordinator.LastPlayerFrame.Action.State == PlayerActionState.None &&
                    near.ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .01f && health.CurrentHealth == originalHealth,
                    "real Up-J traverses all phases, ascends, settles and never repeats or harms its owner");

                // Real damage interrupts the second Up-J presentation. It is not
                // a fake Hurt cue, a physical knockback test, or a live-save test.
                pass = 2;
                await UpJ();
                TestAssert.True(combat.IsAttacking, "second actual Up-J starts before interruption");
                TestAssert.True(player.GetNode<Hurtbox>("Hurtbox").Receive(
                    new DamageRequest(1, Vector2.Right, 0f, 325UL, root.GetInstanceId())),
                    "actual unshielded damage is accepted during Up-J");
                await Frames(tree, 70);
                if (probeFault != null) throw probeFault;
                TestAssert.True(hurtSeen && upResumedAfterHurt && maximumHandoffStep < 32f && attackStarts == 2 &&
                    health.CurrentHealth == originalHealth - 1 && !combat.IsAttacking &&
                    coordinator.LastPlayerFrame.Action.State == PlayerActionState.None &&
                    near.ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .01f && weapon.WeaponSecondaryGripContact,
                    "actual Hurt preserves the existing same-attack resume, continuous regrip and neutral return without an extra attack");
                TestAssert.Equal(observerFailures, coordinator.FrameObserverFailureCount,
                    "native Up-J assertions cannot be silently swallowed by the observer boundary");
                GD.Print($"REAL_GROUND_UP_MEASURE facing={facing} wrist_rise={maxRise:R} blade_up={maxBladeUp:R} phases={phases.Count} attack_starts={attackStarts}");
                nativeCases++;
            }
            finally
            {
                tree.Paused = false;
                coordinator.FramePublished -= Observe;
                KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
                KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
                KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
                KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
                ReleaseInput();
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
            }
        }
        TestAssert.Equal(2, nativeCases, "both real grounded facing cases complete");
        TestAssert.True(observedSamples > 8, "Up-J has real nonempty continuous presentation samples");
        GD.Print("GROUND_UP_NATIVE_OK saved_player=true real_keys=WJ facings=2 phases=4 original_grips=true fixed_links=true soles=true zero_clock=true paused=true hurt_handoff=true same_attack_resume=true no_extra_attack=true gameplay_unchanged=true headless_geometry_only=true human=false");
    }
}
