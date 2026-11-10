using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    // Catches an already accepted J attack freezing behind a later real K
    // terrain cast, then replaying its expired strike when K is cancelled.
    public static async Task RunClawAttackClock(SceneTree tree)
    {
        var failures = new List<string>();
        foreach (int facing in new[] { 1, -1 })
        foreach (bool cast in new[] { false, true })
        {
            try { await ObserveClawAttackClock(tree, facing, cast); }
            catch (Exception error) { failures.Add($"native facing={facing} cast={cast}: {error.Message}"); }
        }
        try { AssertClawAttackTimelineClocks(); }
        catch (Exception error) { failures.Add("timeline: " + error.Message); }
        TestAssert.True(failures.Count == 0, "CLAW_ATTACK_CLOCK: " + string.Join(" | ", failures));
        GD.Print("CLAW_ATTACK_CLOCK_OK saved_player=true real_keys=WJK facings=2 matched_controls=2 terrain_surface=true later_tick_cast=true actual_cancel_idle=true post_combat_window=true clock_admitted=true grace_ticks=2 no_stale_strike=true single_attack=true final_settled=true pure_rates=30,60,120 claw_states=10 gameplay_unchanged=true headless_only=true human=false");
    }

    private readonly record struct ClawClockSample(ulong Tick, double Time, PlayerActionState State,
        PlayerActionPhase Phase, MechanicalClawState ClawState);
    private readonly record struct ClawClockEvent(ulong Tick, MechanicalClawFrame Frame, ulong Target);

    private static async Task ObserveClawAttackClock(SceneTree tree, int facing, bool cast)
    {
        ReleaseInput();
        var root = new Node2D { Name = "ClawAttackClockFixture" };
        StaticBody2D Solid(Vector2 position, Vector2 size)
        {
            var solid = new StaticBody2D { Position = position, CollisionLayer = CombatCollisionLayers.World,
                CollisionMask = CombatCollisionLayers.None };
            solid.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
            return solid;
        }
        root.AddChild(Solid(new(500, 446), new(1600, 20)));
        var ceiling = Solid(new(400, 180), new(260, 20));
        var anchor = new GrappleSurface2D { Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.GripStone,
            UseCollisionHitPoint = true };
        ceiling.AddChild(anchor); root.AddChild(ceiling);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new(400, 400);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        var observer = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
        root.AddChild(player); root.AddChild(observer); tree.Root.AddChild(root);
        var coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var combat = player.GetNode<PlayerCombat>("Combat");
        var hitbox = player.GetNode<Hitbox>("Combat/AttackHitbox");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var health = player.GetNode<HealthComponent>("Health");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var input = player.GetNode<PlayerInputReader>("Input");
        var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        var samples = new List<ClawClockSample>();
        var events = new List<ClawClockEvent>();
        int starts = 0, connected = 0, damaged = 0, postSteps = 0, openings = 0, overlaps = 0;
        int observerFailures = coordinator.FrameObserverFailureCount;
        ulong startTick = 0, openTick = 0, closedTick = 0, finishedTick = 0, attackId = 0, cancelTick = 0;
        int attackDirection = -1;
        double startTime = 0, physicsDelta = 0;
        bool previousMonitoring = false;
        void Started(ulong id, int direction)
        {
            starts++;
            if (starts != 1) return;
            attackId = id; attackDirection = direction;
            startTick = Engine.GetPhysicsFrames();
            startTime = coordinator.LastPlayerFrame.PresentationTimeSeconds ?? double.NaN;
        }
        void Connected(Hurtbox _) => connected++;
        void Damaged(int _, Vector2 __) => damaged++;
        void Published(PlayerPresentationFrame frame)
        {
            if (starts == 0) return;
            // Idle publication happens inside the coordinator's earlier Claw
            // subscription. Read CurrentFrame here, not a later test flag.
            samples.Add(new(Engine.GetPhysicsFrames(), frame.PresentationTimeSeconds ?? double.NaN,
                frame.Action.State, frame.Action.Phase, claw.CurrentFrame.State));
        }
        void ClawChanged(MechanicalClawFrame frame, GrappleSurface2D target) =>
            events.Add(new(Engine.GetPhysicsFrames(), frame, target?.GetInstanceId() ?? 0));
        try
        {
            await Frames(tree, 30);
            KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, true);
            await Frames(tree, 2);
            KeyboardActionRuntimeTests.KeyEvent(facing == 1 ? Key.D : Key.A, false);
            await Frames(tree, 20);
            TestAssert.True(player.IsOnFloor() && player.Velocity.Length() < .01f,
                "Claw clock fixture starts naturally grounded and stopped");
            int originalHealth = health.CurrentHealth;
            ulong playerId = player.GetInstanceId(), anchorId = anchor.GetInstanceId();
            combat.AttackStarted += Started; combat.AttackConnected += Connected;
            health.Damaged += Damaged; coordinator.FramePublished += Published; claw.ClawFrameChanged += ClawChanged;
            observer.Sample = delta =>
            {
                if (starts == 0 || postSteps >= 120) return;
                physicsDelta = delta;
                ulong tick = Engine.GetPhysicsFrames();
                if (hitbox.Monitoring && !previousMonitoring)
                { openings++; if (openTick == 0) openTick = tick; }
                if (!hitbox.Monitoring && previousMonitoring && closedTick == 0) closedTick = tick;
                previousMonitoring = hitbox.Monitoring;
                if (!combat.IsAttacking && finishedTick == 0) finishedTick = tick;
                if (combat.IsAttacking && claw.CurrentFrame.State != MechanicalClawState.Idle) overlaps++;
                postSteps++;
            };
            KeyboardActionRuntimeTests.KeyEvent(Key.W, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            TestAssert.True(claw.KeyboardPreview.Surface == anchor && claw.KeyboardPreview.Snapshot.Exists &&
                claw.KeyboardPreview.Snapshot.HasLineOfSight && !claw.KeyboardPreview.Snapshot.IsLegacyInteraction,
                "real W preview selects the actual grippable solid ceiling, not a legacy or invented target");
            KeyboardActionRuntimeTests.KeyEvent(Key.J, true);
            for (int wait = 0; wait < 12 && starts == 0; wait++) await Frames(tree, 1);
            TestAssert.True(starts == 1 && combat.IsAttacking, "J is really accepted before the later K edge");
            KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            if (cast) KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false);
            if (cast)
            {
                for (int wait = 0; wait < 80 && !(postSteps >= 32 && finishedTick != 0 &&
                    claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing); wait++)
                    await Frames(tree, 1);
                TestAssert.True(finishedTick != 0 && claw.CurrentFrame.LockedTargetRuntimeId == anchorId &&
                    claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                    "real attached claw outlives the original completed attack before cancellation");
                cancelTick = Engine.GetPhysicsFrames();
                KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
                await KeyboardActionRuntimeTests.Frames(tree, input, 2);
                KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            }
            for (int wait = 0; wait < 160 && postSteps < 120; wait++) await Frames(tree, 1);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            double maxSkew = 0, lastTime = startTime;
            ulong positiveTick = startTick, maxGap = 0, idleTick = 0;
            bool monotonic = double.IsFinite(startTime);
            int stale = 0, postIdleSamples = 0;
            var phases = new HashSet<PlayerActionPhase>();
            foreach (var sample in samples)
            {
                monotonic &= double.IsFinite(sample.Time) && sample.Time >= lastTime;
                if (sample.Time > lastTime)
                { maxGap = Math.Max(maxGap, sample.Tick - positiveTick); positiveTick = sample.Tick; }
                maxSkew = Math.Max(maxSkew, Math.Abs(sample.Time - startTime - (sample.Tick - startTick + 1) * physicsDelta));
                lastTime = sample.Time;
                if (cast && cancelTick != 0 && sample.Tick >= cancelTick && sample.ClawState == MechanicalClawState.Idle)
                { if (idleTick == 0) idleTick = sample.Tick; postIdleSamples++; }
                if (sample.State is PlayerActionState.GroundAttack or PlayerActionState.AirAttack or PlayerActionState.DownAttack)
                {
                    phases.Add(sample.Phase);
                    if (closedTick != 0 && sample.Tick > closedTick + 2 &&
                        sample.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact) stale++;
                }
            }
            var liveEvents = events.Where(item => item.Frame.State != MechanicalClawState.Idle).ToArray();
            var attached = events.Where(item => item.Frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing).ToArray();
            var releases = events.Where(item => item.Frame.State == MechanicalClawState.Released).ToArray();
            int castIds = events.Where(item => item.Frame.CastId != 0).Select(item => item.Frame.CastId).Distinct().Count();
            GD.Print($"CLAW_ATTACK_CLOCK_MEASURE facing={facing} cast={cast} samples={samples.Count} steps={postSteps} attack={attackId} starts={starts} open={openTick - startTick} closed={closedTick - startTick} finished={finishedTick - startTick} openings={openings} overlap={overlaps} cast_ids={castIds} events={events.Count} phases={phases.Count} attached_events={attached.Length} releases={releases.Length} cancel={(cancelTick == 0 ? -1L : (long)(cancelTick - startTick))} idle={(idleTick == 0 ? -1L : (long)(idleTick - startTick))} post_idle_samples={postIdleSamples} skew={maxSkew:R} gap={maxGap} monotonic={monotonic} stale={stale}");
            TestAssert.True(postSteps == 120 && samples.Count > 20 && monotonic && maxGap <= 2 &&
                Math.Abs(physicsDelta - 1d / 60) < 1e-6 && maxSkew <= 2d / 60 + 1e-6,
                "CLOCK_ADMISSION_INCONCLUSIVE: natural Process/Physics clocks must stay within two ticks");
            TestAssert.True(starts == 1 && attackId != 0 && attackDirection == (int)AttackDirection.Up &&
                openings == 1 && openTick >= startTick + 3 && openTick <= startTick + 4 &&
                closedTick >= startTick + 9 && closedTick <= startTick + 10 && finishedTick > closedTick && connected == 0,
                "same original attack keeps actual window and no outgoing hitpause during later K");
            TestAssert.True(cast ? liveEvents.Length > 0 && liveEvents[0].Tick > startTick && castIds == 1 && overlaps > 0 &&
                attached.Length > 0 && attached.All(item => item.Target == anchorId && item.Frame.LockedTargetRuntimeId == anchorId) &&
                releases.Length == 1 && releases[0].Frame.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                releases[0].Tick >= finishedTick && idleTick >= cancelTick && idleTick <= releases[0].Tick + 43 &&
                Engine.GetPhysicsFrames() >= idleTick + 30 && postIdleSamples > 30 &&
                !events.Any(item => item.Frame.State == MechanicalClawState.Failed) :
                events.All(item => item.Frame.State == MechanicalClawState.Idle) && castIds == 0 &&
                releases.Length == 0 && overlaps == 0 && phases.Count == 4,
                "matched real terrain attachment/cancel/Idle path or unmasked four-phase control is actually exercised");
            TestAssert.True(player.GetInstanceId() == playerId && health.CurrentHealth == originalHealth && damaged == 0 &&
                motor.AcceptedJumpCount == 0 && !combat.IsAttacking && !hitbox.Monitoring &&
                claw.CurrentFrame.State == MechanicalClawState.Idle && coordinator.LastPlayerFrame.Action.State == PlayerActionState.None &&
                formal.GetNode<PlayerFormalLayer>("NearArm").ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .01f &&
                formal.GetNode<PlayerFormalLayer>("Weapon").WeaponSecondaryGripContact,
                "original actor returns to neutral grip without new attack, fake jump or damage");
            AssertArmChain(formal.GetNode<PlayerFormalLayer>("NearArm")); AssertArmChain(formal.GetNode<PlayerFormalLayer>("FarArm"));
            TestAssert.Equal(observerFailures, coordinator.FrameObserverFailureCount, "Claw clock observations cannot fail silently");
            TestAssert.Equal(0, stale, "REAL_CLAW_CLOCK_RED: releasing a real claw cannot replay expired Burst/Contact");
        }
        finally
        {
            observer.Sample = null;
            combat.AttackStarted -= Started; combat.AttackConnected -= Connected; health.Damaged -= Damaged;
            coordinator.FramePublished -= Published; claw.ClawFrameChanged -= ClawChanged;
            KeyboardActionRuntimeTests.KeyEvent(Key.W, false); KeyboardActionRuntimeTests.KeyEvent(Key.J, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false); KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false); ReleaseInput();
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
        }
    }

    private static void AssertClawAttackTimelineClocks()
    {
        int cases = 0;
        MechanicalClawState[] states = { MechanicalClawState.Aim, MechanicalClawState.Windup, MechanicalClawState.Travel,
            MechanicalClawState.Latched, MechanicalClawState.Reel, MechanicalClawState.Swing, MechanicalClawState.Boost,
            MechanicalClawState.Released, MechanicalClawState.Retract, MechanicalClawState.Failed };
        foreach (int hz in new[] { 30, 60, 120 })
        foreach (var state in states)
        foreach (bool connected in new[] { false, true })
        {
            using var tuning = new PlayerTuning();
            var control = new PlayerPresentationTimeline(tuning);
            var masked = new PlayerPresentationTimeline(tuning);
            var input = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, 1,
                AttackId: 3271, IsAttacking: true, AttackDirection: AttackDirection.Up);
            float dt = 1f / hz;
            for (int i = 0; i < hz / 30; i++) { control.Step(input, dt); masked.Step(input, dt); }
            var cover = MechanicalClawFrame.Idle() with { State = state, CastId = 1, EffectiveVisibility = true,
                AnchorPoint = new(0, -200), LockedTargetRuntimeId = 2 };
            control.Step(input with { AttackConnected = connected }, 0);
            PlayerActionFrame entry = masked.Step(input with { Claw = cover, AttackConnected = connected }, 0).Action;
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                TestAssert.Equal(entry, masked.Step(input with { Claw = cover }, invalid).Action,
                    "zero or invalid time cannot advance hidden clock or covering claw pose");
            PlayerPresentationFrame expected = default;
            for (int i = 0; i < hz / 5; i++)
            { expected = control.Step(input, dt); masked.Step(input with { Claw = cover }, dt); }
            PlayerActionFrame actual = masked.Step(input, 0).Action;
            float age = connected ? hz == 120 ? 5f / 24f : .2f : 7f / 30f;
            float progress = (age - .16f) / .14f;
            TestAssert.True(actual.State == expected.Action.State && actual.Aim == Vector2.Up &&
                actual.Phase == PlayerActionPhase.Recovery && expected.Action.Phase == PlayerActionPhase.Recovery,
                $"Claw cover must resume same-ID Recovery, not stale strike hz={hz} state={state} connected={connected} actual={actual.Phase}");
            TestAssert.Near(progress, expected.Action.NormalizedTime, .0001f, "uncovered independent age keeps original pause");
            TestAssert.Near(progress, actual.NormalizedTime, .0001f, "all Claw covers advance hidden age exactly once");
            TestAssert.Equal(actual, masked.Step(input, 0).Action, "Idle zero-time refresh cannot advance or restart resumed attack");
            TestAssert.Equal(PlayerActionState.None, masked.Step(input with { MovementCancelRequested = true }, 0).Action.State,
                "uncovered Recovery remains cancellable");
            TestAssert.Equal(PlayerActionState.None, masked.Step(input, dt).Action.State, "cancelled same ID cannot reappear");
            TestAssert.Equal(PlayerActionPhase.Anticipation, masked.Step(input with { AttackId = 3272 }, 0).Action.Phase,
                "fresh attack after cancelled cover starts at its own zero age");
            cases++;
        }
        foreach (int hz in new[] { 30, 60, 120 })
        {
            using var tiny = new PlayerTuning { AttackStartupSeconds = .01f, AttackActiveSeconds = .02f, AttackRecoverySeconds = .03f };
            var timeline = new PlayerPresentationTimeline(tiny);
            var input = new PlayerPresentationInput(Vector2.Zero, true, false, false, false, 1, AttackId: 3273, IsAttacking: true);
            timeline.Step(input, 0);
            var cover = MechanicalClawFrame.Idle() with { State = MechanicalClawState.Swing, CastId = 2, AnchorPoint = Vector2.Up };
            for (int i = 0; i < hz / 5; i++)
                timeline.Step(input with { Claw = cover, IsAttacking = (i + 1f) / hz < .06f }, 1f / hz);
            TestAssert.Equal(PlayerActionState.None, timeline.Step(input with { IsAttacking = false }, 0).Action.State,
                "short attack is already expired when long claw cover ends");
            TestAssert.Equal(PlayerActionState.None, timeline.Step(input with { IsAttacking = false }, 1f / hz).Action.State,
                "expired original ID cannot re-latch");
            PlayerActionFrame fresh = timeline.Step(input with { AttackId = 3274, Grounded = false }, 0).Action;
            TestAssert.True(fresh.State == PlayerActionState.AirAttack && fresh.Phase == PlayerActionPhase.Anticipation &&
                fresh.NormalizedTime == 0, "fresh airborne ID cannot inherit hidden age");
            cases++;
        }
        TestAssert.Equal(63, cases, "all ten claw states/rates/pause cases plus three expiry cases execute");
        GD.Print($"CLAW_ATTACK_TIMELINE_OK cases={cases} claw_states=10 rates=30,60,120 hitpause_quantization_preserved=true invalid_delta=true expiry=true fresh_id=true cancel=true");
    }
}
