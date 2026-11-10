using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    // Catches a hidden attack clock freezing during Hurt while the real
    // Combat/Hitbox window continues, then replaying an expired strike.
    public static async Task RunHurtAttackClock(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (int facing in new[] { 1, -1 })
        foreach (bool hurt in new[] { false, true })
        {
            try { await ObserveHurtAttackClock(tree, facing, hurt); }
            catch (Exception error) { failures.Add($"native facing={facing} hurt={hurt}: {error.Message}"); }
        }
        try { AssertHurtAttackTimelineClocks(); }
        catch (Exception error) { failures.Add("timeline: " + error.Message); }
        TestAssert.True(failures.Count == 0, "HURT_ATTACK_CLOCK: " + string.Join(" | ", failures));
        GD.Print("HURT_ATTACK_CLOCK_OK saved_player=true real_keys=WJ facings=2 matched_controls=2 post_combat_window=true clock_admitted=true grace_ticks=2 no_stale_strike=true single_attack=true real_hurt=true final_settled=true pure_rates=30,60,120 gameplay_unchanged=true headless_only=true human=false");
    }

    private readonly record struct HurtClockSample(ulong Tick, double Time, PlayerActionState State,
        PlayerActionPhase Phase, Vector2 Aim);

    private static async Task ObserveHurtAttackClock(SceneTree tree, int facing, bool hurt)
    {
        ReleaseInput();
        var root = new Node2D { Name = "HurtAttackClockFixture" };
        var floor = new StaticBody2D { Position = new(0, 50), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new(1600, 20) } });
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        var observer = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
        root.AddChild(floor); root.AddChild(player); root.AddChild(observer); tree.Root.AddChild(root);
        var coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var combat = player.GetNode<PlayerCombat>("Combat");
        var hitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
        var health = player.GetNode<HealthComponent>("Health");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        var samples = new List<HurtClockSample>();
        int starts = 0, connected = 0, damaged = 0, postSteps = 0, windowOpenings = 0;
        int observerFailures = coordinator.FrameObserverFailureCount;
        ulong startTick = 0, openTick = 0, closedTick = 0, finishedTick = 0, originalId = 0;
        int originalDirection = -1;
        double startTime = 0, physicsDelta = 0;
        bool previousMonitoring = false, injected = false, accepted = false;
        Vector2 damageImpulse = Vector2.Inf;
        Exception sampleFault = null;
        void Started(ulong id, int direction)
        {
            starts++;
            if (starts != 1) return;
            originalId = id; originalDirection = direction;
            startTick = Engine.GetPhysicsFrames();
            startTime = coordinator.LastPlayerFrame.PresentationTimeSeconds ?? double.NaN;
        }
        void Connected(Hurtbox _) => connected++;
        void Damaged(int _, Vector2 impulse) { damaged++; damageImpulse = impulse; }
        void Published(PlayerPresentationFrame frame)
        {
            if (starts == 0) return;
            // Collect first; never let PublishFrame swallow an assertion and
            // turn an invalid observation into a successful receipt.
            samples.Add(new(Engine.GetPhysicsFrames(), frame.PresentationTimeSeconds ?? double.NaN,
                frame.Action.State, frame.Action.Phase, frame.Action.Aim));
        }
        try
        {
            await Frames(tree, 30);
            KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, true);
            await Frames(tree, 2);
            KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, false);
            await Frames(tree, 20);
            TestAssert.True(player.IsOnFloor() && player.Velocity.Length() < .01f,
                "Hurt clock fixture starts naturally grounded and stopped");
            Vector2 groundedRoot = player.GlobalPosition;
            int originalHealth = health.CurrentHealth;
            ulong playerId = player.GetInstanceId();
            combat.AttackStarted += Started; combat.AttackConnected += Connected;
            health.Damaged += Damaged; coordinator.FramePublished += Published;
            observer.Sample = delta =>
            {
                if (starts == 0 || postSteps >= 60 || sampleFault != null) return;
                try
                {
                    physicsDelta = delta;
                    ulong tick = Engine.GetPhysicsFrames();
                    if (hitbox.Monitoring && !previousMonitoring)
                    {
                        windowOpenings++;
                        if (openTick == 0) openTick = tick;
                    }
                    if (!hitbox.Monitoring && previousMonitoring && closedTick == 0) closedTick = tick;
                    previousMonitoring = hitbox.Monitoring;
                    if (!combat.IsAttacking && finishedTick == 0) finishedTick = tick;
                    postSteps++;
                    if (hurt && !injected && tick >= startTick + 2)
                    {
                        injected = true;
                        accepted = player.GetNode<Hurtbox>("Hurtbox").Receive(
                            new DamageRequest(1, Vector2.Right, 0f, 326UL, root.GetInstanceId()));
                    }
                }
                catch (Exception error) { sampleFault = error; }
            };
            KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await Frames(tree, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            await Frames(tree, 2);
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            for (int wait = 0; wait < 90 && postSteps < 60 && sampleFault == null; wait++) await Frames(tree, 1);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (sampleFault != null) throw sampleFault;

            // Clock admission is separate from the functional RED. A recent
            // frame alone cannot hide a cumulative Process/Physics time skew.
            double maxSkew = 0, lastTime = startTime;
            ulong positiveTick = startTick, maxGap = 0;
            bool monotonic = double.IsFinite(startTime);
            var phases = new HashSet<PlayerActionPhase>();
            bool sawHurt = false, resumed = false;
            int stale = 0; ulong firstStale = 0;
            foreach (var sample in samples)
            {
                monotonic &= double.IsFinite(sample.Time) && sample.Time >= lastTime;
                if (sample.Time > lastTime)
                {
                    maxGap = Math.Max(maxGap, sample.Tick - positiveTick);
                    positiveTick = sample.Tick;
                }
                double physicalAge = (sample.Tick - startTick + 1) * physicsDelta;
                maxSkew = Math.Max(maxSkew, Math.Abs(sample.Time - startTime - physicalAge));
                lastTime = sample.Time;
                sawHurt |= sample.State == PlayerActionState.Hurt;
                if (sample.State == PlayerActionState.GroundAttack)
                {
                    phases.Add(sample.Phase);
                    resumed |= sawHurt && sample.Aim == Vector2.Up;
                    if (closedTick != 0 && sample.Tick > closedTick + 2 &&
                        sample.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact)
                    { stale++; if (firstStale == 0) firstStale = sample.Tick; }
                }
            }
            GD.Print($"HURT_ATTACK_CLOCK_MEASURE facing={facing} hurt={hurt} samples={samples.Count} steps={postSteps} id={originalId} starts={starts} open={openTick - startTick} closed={closedTick - startTick} finished={finishedTick - startTick} openings={windowOpenings} skew={maxSkew:R} gap={maxGap} monotonic={monotonic} saw_hurt={sawHurt} resumed={resumed} stale={stale} first_stale={(firstStale == 0 ? -1L : (long)(firstStale - startTick))}");
            TestAssert.True(postSteps == 60 && samples.Count > 8 && monotonic && maxGap <= 2 &&
                Math.Abs(physicsDelta - 1d / 60) < 1e-6 && maxSkew <= 2d / 60 + 1e-6,
                "CLOCK_ADMISSION_INCONCLUSIVE: natural Process clock must track native 60Hz within two ticks");
            TestAssert.True(starts == 1 && originalId != 0 && originalDirection == (int)AttackDirection.Up &&
                windowOpenings == 1 && openTick >= startTick + 3 && openTick <= startTick + 4 &&
                closedTick >= startTick + 9 && closedTick <= startTick + 10 && finishedTick > closedTick && connected == 0,
                "one real original Up attack retains its actual startup/active/recovery window with no outgoing hitpause");
            TestAssert.True(hurt ? injected && accepted && damaged == 1 && damageImpulse == Vector2.Zero &&
                health.CurrentHealth == originalHealth - 1 && sawHurt && resumed :
                !injected && damaged == 0 && health.CurrentHealth == originalHealth && !sawHurt && phases.Count == 4,
                "matched real-Hurt and uninterrupted controls actually exercise their declared paths");
            TestAssert.True(player.GetInstanceId() == playerId && player.IsOnFloor() &&
                player.GlobalPosition.DistanceTo(groundedRoot) < .02f && motor.AcceptedJumpCount == 0 &&
                !combat.IsAttacking && !hitbox.Monitoring &&
                coordinator.LastPlayerFrame.Action.State == PlayerActionState.None &&
                formal.GetNode<PlayerFormalLayer>("NearArm").ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .01f &&
                formal.GetNode<PlayerFormalLayer>("Weapon").WeaponSecondaryGripContact,
                "original live actor naturally settles without root motion, extra attack or missing final grip");
            AssertArmChain(formal.GetNode<PlayerFormalLayer>("NearArm"));
            AssertArmChain(formal.GetNode<PlayerFormalLayer>("FarArm"));
            TestAssert.Equal(observerFailures, coordinator.FrameObserverFailureCount, "clock observer cannot fail silently");
            TestAssert.Equal(0, stale, "REAL_HURT_CLOCK_RED: closed hitbox plus two ticks cannot replay an expired Burst/Contact");
        }
        finally
        {
            observer.Sample = null;
            combat.AttackStarted -= Started; combat.AttackConnected -= Connected;
            health.Damaged -= Damaged; coordinator.FramePublished -= Published;
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false); KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.A, false); KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            ReleaseInput();
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
        }
    }

    private static void AssertHurtAttackTimelineClocks()
    {
        int cases = 0;
        foreach (int hz in new[] { 30, 60, 120 })
        foreach (var variant in new[] { (true, AttackDirection.Up), (false, AttackDirection.Horizontal), (false, AttackDirection.Down) })
        foreach (bool connected in new[] { false, true })
        {
            using var tuning = new PlayerTuning();
            var control = new PlayerPresentationTimeline(tuning);
            var masked = new PlayerPresentationTimeline(tuning);
            var input = new PlayerPresentationInput(Vector2.Zero, variant.Item1, false, false, false, 1,
                AttackId: 3261, IsAttacking: true, AttackDirection: variant.Item2);
            float dt = 1f / hz;
            for (int i = 0; i < hz / 30; i++) { control.Step(input, dt); masked.Step(input, dt); }
            control.Step(input with { AttackConnected = connected }, 0);
            PlayerActionFrame hurtEntry = masked.Step(input with { AttackConnected = connected, HurtTriggered = true }, 0).Action;
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                TestAssert.Equal(hurtEntry, masked.Step(input, invalid).Action, "invalid delta cannot advance either hidden clock or Hurt");
            PlayerPresentationFrame expected = default, actual = default;
            for (int i = 0; i < hz / 5; i++)
            { expected = control.Step(input, dt); actual = masked.Step(input, dt); }
            TestAssert.True(actual.Action.State == expected.Action.State && actual.Action.Aim == expected.Action.Aim &&
                actual.Action.Phase == PlayerActionPhase.Recovery && expected.Action.Phase == PlayerActionPhase.Recovery,
                $"hidden clock returns same-ID Recovery, not expired strike hz={hz} connected={connected} actual={actual.Action.Phase}");
            // Independently calculated ages: .233333 without hitpause; old
            // whole-sample pause loses .033333 at 30/60Hz, .025 at 120Hz.
            float expectedAge = connected ? hz == 120 ? 5f / 24f : .2f : 7f / 30f;
            float recovery = (expectedAge - .16f) / .14f;
            TestAssert.Near(recovery, expected.Action.NormalizedTime, .0001f, "uninterrupted clock retains original outgoing pause");
            TestAssert.Near(recovery, actual.Action.NormalizedTime, .0001f, "Hurt advances exactly once including its ending step");
            PlayerActionFrame before = actual.Action;
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                TestAssert.Equal(before, masked.Step(input, invalid).Action, "nonpositive/invalid delta cannot advance attack clock");
            TestAssert.Equal(PlayerActionState.None, masked.Step(input with { MovementCancelRequested = true }, 0).Action.State,
                "resumed Recovery remains movement cancellable");
            cases++;
        }
        using (var tiny = new PlayerTuning { AttackStartupSeconds = .01f, AttackActiveSeconds = .01f, AttackRecoverySeconds = .01f })
        {
            var timeline = new PlayerPresentationTimeline(tiny);
            var input = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, 1, AttackId: 3262, IsAttacking: true);
            timeline.Step(input, .01f);
            timeline.Step(input with { HurtTriggered = true }, 0);
            PlayerPresentationFrame expired = default;
            for (int i = 0; i < 12; i++) expired = timeline.Step(input with { IsAttacking = i == 0 }, 1f / 60);
            TestAssert.Equal(PlayerActionState.None, expired.Action.State,
                "short real attack is already expired at the end of Hurt, not after an extra cleanup refresh");
            for (int i = 0; i < 6; i++) expired = timeline.Step(input with { IsAttacking = false }, 1f / 60);
            TestAssert.Equal(PlayerActionState.None, expired.Action.State,
                "short real attack expires while hidden without re-latching the old ID");
            TestAssert.Equal(PlayerActionPhase.Anticipation, timeline.Step(input with { AttackId = 3263 }, 0).Action.Phase,
                "fresh ID can still begin after hidden attack expiry");
            cases++;
        }
        using (var longer = new PlayerTuning { AttackStartupSeconds = .08f, AttackActiveSeconds = .18f, AttackRecoverySeconds = .20f })
        {
            var timeline = new PlayerPresentationTimeline(longer);
            var input = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, 1, AttackId: 3264, IsAttacking: true);
            timeline.Step(input, 1f / 30);
            timeline.Step(input with { HurtTriggered = true }, 0);
            PlayerPresentationFrame actual = default;
            for (int i = 0; i < 12; i++) actual = timeline.Step(input, 1f / 60);
            TestAssert.True(actual.Action.State == PlayerActionState.GroundAttack && actual.Action.Phase == PlayerActionPhase.Contact,
                "long actual window may legitimately resume Contact after Hurt; do not cancel every resumed attack");
            TestAssert.Near((7f / 30 - .179f) / .081f, actual.Action.NormalizedTime, .0001f,
                "custom active duration uses elapsed hidden age rather than a hardcoded default");
            cases++;
        }
        GD.Print($"HURT_ATTACK_TIMELINE_OK cases={cases} rates=30,60,120 hitpause_quantization_preserved=true same_id=true tiny_expiry=true fresh_id=true invalid_delta=true custom_window=true recovery_cancel=true");
    }
}
