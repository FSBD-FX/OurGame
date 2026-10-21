using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Mechanisms;

namespace Knight.Tests.Integration;

// Saved Player, actual InputMap and automatic scene-tree physics. No OS input.
public static class ThrusterPlayerRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await GroundCombinedConsumesAirUse(tree);
        await RejectedShiftPreservesClaw(tree);
        foreach (MechanicalClawState phase in new[] { MechanicalClawState.Aim, MechanicalClawState.Travel,
            MechanicalClawState.Reel, MechanicalClawState.Swing })
            await SpentAirUsePreservesClawPhase(tree, phase);
        foreach (MechanicalClawState phase in new[] { MechanicalClawState.Aim, MechanicalClawState.Travel,
            MechanicalClawState.Reel, MechanicalClawState.Swing })
            await AcceptedShiftDetachesWithoutRelease(tree, phase);
        await CallbackResetInvalidatesOldPublication(tree);
        await CallbackReplacementWinsTransaction(tree, false);
        await CallbackReplacementWinsTransaction(tree, true);
        await CallbackReplacementWinsTransaction(tree, true, residency: true);
        await QueuedDeletionStopsTransaction(tree, false);
        await QueuedDeletionStopsTransaction(tree, true);
        await HighIncomingVelocityIsInherited(tree);
        foreach (string pausePoint in new[] { "boost", "unpin", "released", "reset" })
            await CallbackPauseDuringBoost(tree, pausePoint);
        foreach (string replacementPoint in new[] { "boost", "unpin", "released", "paused" })
            await PendingBoostRejectsReplacedMotion(tree, replacementPoint);
        foreach (MechanicalClawState phase in new[] { MechanicalClawState.Reel, MechanicalClawState.Swing })
            await ExternalReplacementDoesNotGloballyCancelClaw(tree, phase);
        await ForceCoastAndBrake(tree);
        await PauseFreezesAndSuppressesOldShift(tree);
        await CollisionWritesBack(tree, false);
        await CollisionWritesBack(tree, true);
        await WindStillAffectsBurn(tree);
        await DeathInvalidatesActivePulse(tree);
        GD.Print("THRUSTER_PLAYER_OK saved_player=true automatic_physics=true combined_budget=true rejected_claw_preserved=true dedicated_handoff=true authority_reset=true replacement_wins=true force_coast_brake=true pause=true collision=true wind=true human=false whole_route=false");
        GD.Print("THRUSTER_BOUNDARIES_OK automatic_cases=33 accepted_claw_phases=4 rejected_claw_phases=4 queued_owner_and_ancestor=true pause_boost_stages=3 reset_revokes_pending=true residency_reentrant_reset=true high_incoming=true pending_replacement_points=4 ordinary_attached_replacement_preserved=true human=false");
    }

    private static async Task SpentAirUsePreservesClawPhase(SceneTree tree, MechanicalClawState phase)
    {
        using var fixture = new FreeFixture(tree, true);
        fixture.Root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        await fixture.Start();
        KeyEvent(Key.W, true); KeyEvent(Key.Space, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 1);
        KeyEvent(Key.Space, false); KeyEvent(Key.Shift, false);
        await Frames(tree, fixture.Input, 13);
        TestAssert.True(!fixture.Motor.IsDashing && !fixture.Motor.AirDashAvailable && !fixture.Player.IsOnFloor(),
            "spent-air phase test has ended burn without a real landing");
        var claw = fixture.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        int releases = 0;
        claw.ClawFrameChanged += (frame, _) => { if (frame.State == MechanicalClawState.Released) releases++; };
        KeyEvent(Key.K, true); await Frames(tree, fixture.Input, 1); KeyEvent(Key.K, false);
        for (int tick = 0; tick < 150 && claw.CurrentFrame.State != phase; tick++)
            await Frames(tree, fixture.Input, 1);
        TestAssert.Equal(phase, claw.CurrentFrame.State, "spent-air rejection reaches requested actual claw phase");
        KeyEvent(Key.Shift, true); await Frames(tree, fixture.Input, 1);
        TestAssert.Equal(1, fixture.Motor.AcceptedDashCount, "spent air use does not begin another pulse at " + phase);
        TestAssert.True(fixture.Motor.RejectedDashCount > 0 && !fixture.Motor.AirDashAvailable,
            "spent air rejection is recorded without refund at " + phase);
        TestAssert.Equal(0, releases, "spent-air rejected Shift never releases the actual " + phase);
        TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or
            MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
            "original claw keeps advancing after spent-air rejection at " + phase);
        Release();
    }

    private static async Task DeathInvalidatesActivePulse(SceneTree tree)
    {
        using var fixture = new FreeFixture(tree, false);
        await fixture.Start();
        KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 2);
        TestAssert.True(fixture.Motor.IsDashing, "death negative begins with a real burning pulse");
        HealthComponent health = fixture.Player.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero, 0f, 81UL,
            fixture.Player.GetInstanceId())), "death occurs through real Health, without HP rewrite");
        TestAssert.True(!fixture.Motor.IsDashing, "death synchronously invalidates active pulse");
        TestAssert.Equal(Vector2.Zero, fixture.Player.Velocity, "death clears owned motion without an extra tick");
        await Frames(tree, fixture.Input, 3);
        TestAssert.Equal(Vector2.Zero, fixture.Player.Velocity, "dead body cannot restart stale motion or gravity");
        TestAssert.Equal(0, fixture.Motor.AcceptedDashCount, "dead held Shift cannot refire after lifecycle reset");
        Release();
    }

    private static async Task ForceCoastAndBrake(SceneTree tree)
    {
        using var fixture = new FreeFixture(tree, true);
        await fixture.Start();
        KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 1);
        FreeSample start = fixture.Starts[0];
        TestAssert.Near(93.333336f, start.Velocity.X, .004f,
            "ground first tick adds finite 5600/60 force, never overwrites speed to legacy 620");
        TestAssert.True(start.Burning && fixture.Motor.AirDashAvailable,
            "ground Shift without original Space burns and preserves air qualification");
        Release(); await Frames(tree, fixture.Input, 14);
        TestAssert.True(!fixture.Motor.IsDashing && fixture.Player.Velocity.X > 300f,
            "finite burn ends but leaves real coast velocity");
        float beforeBrake = fixture.Player.Velocity.X;
        KeyEvent(Key.A, true); await Frames(tree, fixture.Input, 3);
        TestAssert.Near(beforeBrake - 200f, fixture.Player.Velocity.X, .004f,
            "three real ground reverse ticks brake by finite 4000*3/60, not instant reversal");
        Release();
    }

    private static async Task PauseFreezesAndSuppressesOldShift(SceneTree tree)
    {
        using var fixture = new FreeFixture(tree, false);
        await fixture.Start();
        KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 2);
        Vector2 position = fixture.Player.Position, velocity = fixture.Player.Velocity;
        tree.Paused = true;
        KeyEvent(Key.Shift, false); KeyEvent(Key.Shift, true);
        for (int frame = 0; frame < 4; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(position, fixture.Player.Position, "pause freezes native position");
        TestAssert.Equal(velocity, fixture.Player.Velocity, "pause freezes physical velocity");
        TestAssert.True(fixture.Motor.IsDashing, "pause does not expire the finite pulse on wall clock");
        tree.Paused = false;
        await Frames(tree, fixture.Input, 2);
        TestAssert.Equal(1, fixture.Motor.AcceptedDashCount, "paused Shift edge never replays on resume");
        TestAssert.True(fixture.Motor.IsDashing && !fixture.Motor.AirDashAvailable,
            "resume continues remaining burn without refund");
        Release();
    }

    private static async Task CollisionWritesBack(SceneTree tree, bool ceiling)
    {
        using var fixture = new FreeFixture(tree, true);
        StaticBody2D blocker = fixture.AddSolid(ceiling ? new Vector2(0, -80) : new Vector2(120, -100),
            ceiling ? new Vector2(800, 10) : new Vector2(10, 400));
        await fixture.Start();
        KeyEvent(ceiling ? Key.W : Key.D, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 25);
        TestAssert.True(!fixture.Motor.IsDashing, "blocked pulse still expires without accumulating time");
        if (ceiling)
        {
            TestAssert.True(fixture.Player.Position.Y >= -39.1f, "actual saved capsule cannot cross low ceiling");
            TestAssert.True(fixture.Player.Velocity.Y >= 0f, "blocked upward component is not retained after burn");
        }
        else
        {
            TestAssert.Near(0f, fixture.Player.Velocity.X, .001f, "thin wall removes blocked horizontal component");
            TestAssert.True(fixture.Player.Position.X <= 95.1f, "actual saved capsule cannot cross thin wall");
            blocker.Free();
            await Frames(tree, fixture.Input, 1);
            TestAssert.Near(36.666668f, fixture.Player.Velocity.X, .004f,
                "after wall removal ordinary drive restarts from zero, not hidden jet velocity");
        }
        Release();
    }

    private static async Task WindStillAffectsBurn(SceneTree tree)
    {
        using var fixture = new FreeFixture(tree, false);
        fixture.Root.AddChild(new CanyonUpdraft { Bounds = new Rect2(-1000, -1500, 3000, 3000), DescentSpeed = 100f });
        await fixture.Start();
        fixture.Motor.ExternalImpulse(new Vector2(0f, 600f));
        KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 1);
        TestAssert.True(fixture.Motor.IsDashing, "wind comparison observes an actual active short jet");
        TestAssert.Near(630.8333f, fixture.Motor.UpdraftCandidateVelocity.Y, .004f,
            "horizontal jet keeps one tick of ordinary gravity before wind");
        TestAssert.Near(-50f, fixture.Motor.UpdraftVelocityDelta.Y, .004f,
            "free active burn receives exactly one full-tick 3000/60 wind brake");
        TestAssert.Near(580.8333f, fixture.Player.Velocity.Y, .004f,
            "native final velocity includes gravity plus wind, without dash exemption");
        Release();
    }

    // Scope negative: replacing motion without a pending Space release must
    // retain the existing cast/pin and its normal next-step constraint ownership.
    private static async Task ExternalReplacementDoesNotGloballyCancelClaw(SceneTree tree, MechanicalClawState phase)
    {
        using var fixture = new FreeFixture(tree, true);
        var anchor = new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 };
        fixture.Root.AddChild(anchor); await fixture.Start();
        var input = fixture.Input;
        var claw = fixture.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        KeyEvent(Key.W, true); await Frames(tree, input, 1);
        KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
        for (int tick = 0; tick < 150 && claw.CurrentFrame.State != phase; tick++) await Frames(tree, input, 1);
        TestAssert.Equal(phase, claw.CurrentFrame.State, "range negative reaches actual " + phase);
        ulong castId = claw.CurrentFrame.CastId, authority = claw.AuthorityRevision;
        fixture.Motor.ExternalImpulse(new Vector2(0f, 120f));
        await Frames(tree, input, 1);
        TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing,
            "ExternalImpulse without pending release does not globally cancel the existing cast");
        TestAssert.True(claw.CurrentFrame.CastId == castId && claw.AuthorityRevision == authority &&
            claw.CurrentTarget == anchor && claw.ResidencyTarget == anchor,
            "normal attached replacement retains actual cast authority and residency");
        TestAssert.True(fixture.Motor.AirDashAvailable && fixture.Motor.AcceptedDashCount == 0,
            "range negative does not spend or refund air use");
        Release();
    }

    // Break caught: a release decided under old motion authority applies its
    // boost to a newer ExternalImpulse. Sample before Motor adds lawful gravity.
    private static async Task PendingBoostRejectsReplacedMotion(SceneTree tree, string replacementPoint)
    {
        using var fixture = new FreeFixture(tree, true);
        fixture.Root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        var observer = new ThrusterPostMotorObserver
        {
            ProcessPhysicsPriority = -75, ProcessMode = Node.ProcessModeEnum.Always
        };
        fixture.Root.AddChild(observer);
        await fixture.Start();
        var input = fixture.Input;
        var player = fixture.Player;
        var motor = fixture.Motor;
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        bool replaced = false, captured = false;
        int boosts = 0, boostReleases = 0, unpins = 0;
        Vector2 beforeMotor = default;
        void Replace()
        {
            if (replaced) return;
            replaced = true;
            motor.ExternalImpulse(new Vector2(0f, 120f));
        }
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (frame.State == MechanicalClawState.Boost)
            {
                boosts++;
                if (replacementPoint == "boost") Replace();
                if (replacementPoint == "paused") tree.Paused = true;
            }
            if (frame.State == MechanicalClawState.Released && frame.ReleaseKind == MechanicalClawReleaseKind.Boost)
            {
                boostReleases++;
                if (replacementPoint == "released") Replace();
            }
        };
        claw.TargetResidencyChanged += (_, resident) =>
        {
            if (!resident) { unpins++; if (replacementPoint == "unpin") Replace(); }
        };
        observer.Observe = () =>
        {
            if (!replaced || captured) return;
            captured = true; beforeMotor = player.Velocity;
        };
        try
        {
            KeyEvent(Key.W, true); await Frames(tree, input, 1);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            for (int tick = 0; tick < 150 && claw.CurrentFrame.State != MechanicalClawState.Swing; tick++)
                await Frames(tree, input, 1);
            TestAssert.Equal(MechanicalClawState.Swing, claw.CurrentFrame.State, "motion replacement reaches real Swing");
            ulong serial = claw.CurrentFrame.NextCastId;
            KeyEvent(Key.Space, true);
            await Frames(tree, input, 1);
            if (replacementPoint == "paused")
            {
                TestAssert.True(tree.Paused && !replaced, "pending is genuinely paused before outside replacement");
                Replace();
                KeyEvent(Key.Space, false); KeyEvent(Key.W, false); tree.Paused = false;
                await Frames(tree, input, 1);
            }
            TestAssert.True(replaced && captured, "replacement and automatic pre-Motor observation both occurred");
            TestAssert.Equal(new Vector2(0f, 120f), beforeMotor,
                "old release boost must not overwrite or add to replacement at " + replacementPoint);
            TestAssert.Equal(MechanicalClawState.Idle, claw.CurrentFrame.State,
                "superseded release retires its old cast instead of reattaching next tick");
            TestAssert.True(claw.CurrentTarget == null && claw.ResidencyTarget == null &&
                motor.GrappleMovementScale == 1f, "superseded release leaves no stale target or velocity constraint");
            TestAssert.Equal(serial, claw.CurrentFrame.NextCastId, "superseded release keeps the monotonic next cast serial");
            TestAssert.Equal(1, boosts, "superseded release cannot publish another Boost");
            TestAssert.Equal(replacementPoint == "released" ? 1 : 0, boostReleases,
                "old boost release never advances beyond an already delivered publication");
            TestAssert.Equal(1, unpins, "superseded release relinquishes its original residency once");
            KeyEvent(Key.Space, false); KeyEvent(Key.W, false);
            await Frames(tree, input, 2);
            TestAssert.True(player.Velocity.X == 0f && motor.AcceptedJumpCount == 0 &&
                motor.AcceptedDashCount == 0 && motor.AirDashAvailable,
                "later ordinary physics never restores stale tangential boost, Space or budget effects");
        }
        finally { tree.Paused = false; Release(); }
    }

    // Break caught: pausing inside a real boost callback must suspend the rest
    // of that release transaction, rather than changing velocity while paused.
    private static async Task CallbackPauseDuringBoost(SceneTree tree, string pausePoint)
    {
        using var fixture = new FreeFixture(tree, true);
        fixture.Root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        await fixture.Start();
        var input = fixture.Input;
        var player = fixture.Player;
        var motor = fixture.Motor;
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        bool paused = false;
        int boosts = 0, releases = 0, unpins = 0;
        Vector2 pausedPosition = default, pausedVelocity = default;
        void PauseHere()
        {
            if (paused) return;
            paused = true; pausedPosition = player.Position; pausedVelocity = player.Velocity;
            tree.Paused = true;
        }
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (frame.State == MechanicalClawState.Boost)
            {
                boosts++; if (pausePoint is "boost" or "reset") PauseHere();
            }
            if (frame.State == MechanicalClawState.Released && frame.ReleaseKind == MechanicalClawReleaseKind.Boost)
            {
                releases++; if (pausePoint == "released") PauseHere();
            }
        };
        claw.TargetResidencyChanged += (_, resident) =>
        {
            if (!resident) { unpins++; if (pausePoint == "unpin") PauseHere(); }
        };
        try
        {
            KeyEvent(Key.W, true); await Frames(tree, input, 1);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            for (int tick = 0; tick < 150 && claw.CurrentFrame.State != MechanicalClawState.Swing; tick++)
                await Frames(tree, input, 1);
            TestAssert.Equal(MechanicalClawState.Swing, claw.CurrentFrame.State, "pause callback reaches actual Swing");
            KeyEvent(Key.Space, true);
            await Frames(tree, input, 1); // Input sampled before the synchronous callback paused the tree.
            TestAssert.True(paused && tree.Paused, "actual Space boost synchronously pauses at " + pausePoint);
            TestAssert.Equal(pausedVelocity, player.Velocity, "boost callback pause freezes the remaining velocity transaction");
            TestAssert.Equal(pausedPosition, player.Position, "boost callback pause prevents movement in the old tick");
            TestAssert.Equal(pausePoint == "released" ? 1 : 0, releases,
                "release publication waits until its actual stage is permitted");
            for (int frame = 0; frame < 4; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.Equal(pausedVelocity, player.Velocity, "wall-clock frames cannot apply a paused boost");
            if (pausePoint == "reset")
            {
                claw.ResetTransientState();
                motor.ResetTransientState();
                KeyEvent(Key.Space, false); KeyEvent(Key.W, false); tree.Paused = false;
                await Frames(tree, input, 2);
                TestAssert.Equal(1, boosts, "reset cannot resurrect or repeat the old Boost publication");
                TestAssert.Equal(0, releases, "reset revokes the pending old release before resume");
                TestAssert.Equal(1, unpins, "reset releases the original residency once");
                TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Idle &&
                    player.Velocity.X == 0f && player.Velocity.Y >= 0f,
                    "resume after reset has no old tangential or upward boost");
                return;
            }
            KeyEvent(Key.Space, false); KeyEvent(Key.W, false); tree.Paused = false;
            await Frames(tree, input, 2);
            TestAssert.Equal(1, boosts, "resume never republishes Boost");
            TestAssert.Equal(1, releases, "resume publishes one completed boost release");
            TestAssert.Equal(1, unpins, "resume unpins the original target exactly once");
            TestAssert.Near(208.5749f, player.Velocity.X, .004f,
                "resume applies one 220 tangential boost followed by two real air-drag ticks, never twice");
            TestAssert.True(motor.AcceptedJumpCount == 0,
                "resume does not replay ordinary Space");
            TestAssert.True(motor.AirDashAvailable, "boost resume does not spend or refund the untouched air use");
        }
        finally { tree.Paused = false; Release(); }
    }

    // Break caught: queued deletion is not immediate ExitTree. A still-valid
    // wrapper must not commit or move after its Player/ancestor is revoked.
    private static async Task QueuedDeletionStopsTransaction(SceneTree tree, bool ancestor)
    {
        using var fixture = new FreeFixture(tree, true);
        fixture.Root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        var observer = new ThrusterPostMotorObserver { ProcessPhysicsPriority = 101 };
        tree.Root.AddChild(observer); // outside the potentially deleted subtree
        await fixture.Start();
        var input = fixture.Input;
        var player = fixture.Player;
        var motor = fixture.Motor;
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        bool armed = false, queued = false, captured = false;
        Vector2 atCallback = default, afterMotor = default;
        int accepted = -1;
        bool burning = true, airAvailable = false;
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (!armed || queued || frame.State != MechanicalClawState.Idle) return;
            queued = true; atCallback = player.Position;
            if (ancestor) fixture.Root.QueueFree(); else player.QueueFree();
        };
        observer.Observe = () =>
        {
            if (!queued || captured) return;
            captured = true; afterMotor = player.Position;
            accepted = motor.AcceptedDashCount; burning = motor.IsDashing; airAvailable = motor.AirDashAvailable;
        };
        try
        {
            KeyEvent(Key.W, true); await Frames(tree, input, 1);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            for (int tick = 0; tick < 50 && claw.CurrentFrame.State != MechanicalClawState.Reel; tick++)
                await Frames(tree, input, 1);
            TestAssert.Equal(MechanicalClawState.Reel, claw.CurrentFrame.State, "queued deletion fixture reaches real Reel");
            armed = true;
            KeyEvent(Key.W, false); KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
            await Frames(tree, input, 1);
            TestAssert.True(queued && captured, "queued subtree is observed in the same automatic physics tick");
            TestAssert.Equal(0, accepted, "queued Player/ancestor cannot commit the old short jet");
            TestAssert.Equal(atCallback, afterMotor, "queued owner cannot execute the old MoveAndSlide");
            TestAssert.True(!burning && airAvailable, "queued deletion neither burns nor spends the air use");
        }
        finally { Release(); observer.Free(); }
    }

    // Break caught: after cleanup callback replaces/resets motion, the original
    // Motor tick integrates stale work over the new owner in the same step.
    private static async Task CallbackReplacementWinsTransaction(SceneTree tree, bool reset, bool residency = false)
    {
        using var floorShape = new RectangleShape2D { Size = new Vector2(800, 20) };
        var root = new Node2D { Name = reset ? "ThrusterResetWins" : "ThrusterReplacementWins" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14); root.AddChild(player);
        var observer = new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100 };
        root.AddChild(observer); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        bool armed = false, replaced = false, captured = false;
        Vector2 observedVelocity = default;
        int accepted = -1;
        void Replace()
        {
            if (!armed || replaced) return;
            replaced = true;
            if (reset) motor.ResetTransientState();
            else motor.ExternalImpulse(new Vector2(-500f, -400f));
            if (residency) claw.ResetTransientState();
        }
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (!residency && frame.State == MechanicalClawState.Idle) Replace();
        };
        claw.TargetResidencyChanged += (_, resident) =>
        {
            if (residency && !resident) Replace();
        };
        observer.Observe = () =>
        {
            if (!replaced || captured) return;
            captured = true; observedVelocity = player.Velocity; accepted = motor.AcceptedDashCount;
        };
        try
        {
            Release(); await Frames(tree, input, 8);
            KeyEvent(Key.W, true); await Frames(tree, input, 1);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            for (int tick = 0; tick < 50 && claw.CurrentFrame.State != MechanicalClawState.Reel; tick++)
                await Frames(tree, input, 1);
            TestAssert.Equal(MechanicalClawState.Reel, claw.CurrentFrame.State, "replacement fixture genuinely reaches Reel");
            armed = true;
            KeyEvent(Key.W, false); KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
            await Frames(tree, input, 1);
            TestAssert.True(replaced && captured, "cleanup callback and automatic post-Motor observer both run");
            TestAssert.Equal(0, accepted, "superseded acceptance must never begin or count a pulse");
            TestAssert.Equal(reset ? Vector2.Zero : new Vector2(-500f, -400f), observedVelocity,
                "callback-owned replacement survives the OLD Motor step exactly, without stale integration");
            TestAssert.True(!motor.IsDashing && motor.AirDashAvailable, "aborted cleanup does not spend or create air use");
            KeyEvent(Key.Shift, false); await Frames(tree, input, 1);
            TestAssert.Equal(0, motor.AcceptedDashCount, "aborted Shift is not queued for next tick");
        }
        finally { Release(); root.Free(); }
    }

    // Break caught: a reset queued by an early observer still delivers the old
    // Reel to later observers, which can resurrect old movement/presentation.
    private static async Task CallbackResetInvalidatesOldPublication(SceneTree tree)
    {
        using var floorShape = new RectangleShape2D { Size = new Vector2(800, 20) };
        var root = new Node2D { Name = "ThrusterClawAuthority" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14); root.AddChild(player); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        bool resetObserved = false;
        int staleReelDeliveries = 0;
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (frame.State != MechanicalClawState.Reel || resetObserved) return;
            resetObserved = true;
            claw.ResetTransientState();
        };
        claw.ClawFrameChanged += (frame, _) => { if (resetObserved && frame.State == MechanicalClawState.Reel) staleReelDeliveries++; };
        try
        {
            Release(); await Frames(tree, input, 8);
            KeyEvent(Key.W, true); await Frames(tree, input, 1);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            for (int tick = 0; tick < 50 && !resetObserved; tick++) await Frames(tree, input, 1);
            TestAssert.True(resetObserved, "authority test reaches real Reel publication");
            TestAssert.Equal(0, staleReelDeliveries, "reset revokes queued OLD authority before later frame observers");
            TestAssert.Equal(MechanicalClawState.Idle, claw.CurrentFrame.State, "reset finishes at current Idle");
            TestAssert.True(claw.CurrentTarget == null && claw.ResidencyTarget == null && motor.GrappleMovementScale == 1f,
                "stale callback continuation cannot restore target or correction");
        }
        finally { Release(); root.Free(); }
    }

    // Break caught: accepted Shift leaves the Reel correction active, or uses
    // normal release (boost/projection/cap) instead of a dedicated ownership exit.
    private static async Task AcceptedShiftDetachesWithoutRelease(SceneTree tree, MechanicalClawState phase)
    {
        using var floorShape = new RectangleShape2D { Size = new Vector2(800, 20) };
        var root = new Node2D { Name = "ThrusterAcceptedClaw" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        var anchor = new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 };
        root.AddChild(anchor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14); root.AddChild(player); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        int releases = 0, unpins = 0;
        bool targetReadableDuringUnpin = false;
        bool armed = false, floorAtHandoff = false;
        Vector2 inheritedAtIdle = default;
        ulong serialAtIdle = 0UL;
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (frame.State is MechanicalClawState.Released or MechanicalClawState.Boost) releases++;
            if (armed && frame.State == MechanicalClawState.Idle)
            {
                inheritedAtIdle = player.Velocity; floorAtHandoff = player.IsOnFloor();
                serialAtIdle = frame.NextCastId;
            }
        };
        claw.TargetResidencyChanged += (target, resident) =>
        {
            if (resident) return;
            unpins++;
            targetReadableDuringUnpin = target == anchor && claw.ResidencyTarget == anchor && claw.CurrentTarget == anchor;
        };
        try
        {
            Release(); await Frames(tree, input, 8);
            KeyEvent(Key.W, true); await Frames(tree, input, 1);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            for (int tick = 0; tick < 150 && claw.CurrentFrame.State != phase; tick++)
                await Frames(tree, input, 1);
            TestAssert.Equal(phase, claw.CurrentFrame.State, "accepted handoff begins at genuine " + phase);
            TestAssert.True(motor.AirDashAvailable, "unspent real claw leaves air use available");
            armed = true;
            KeyEvent(Key.W, false); KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
            await Frames(tree, input, 1);
            TestAssert.Equal(1, motor.AcceptedDashCount, "accepted real attached Shift starts once");
            TestAssert.Equal(MechanicalClawState.Idle, claw.CurrentFrame.State, "accepted Shift synchronously exits claw ownership");
            TestAssert.True(motor.IsDashing && motor.AirDashAvailable == floorAtHandoff,
                "handoff actually burns and consumes air only when the actual boundary was airborne");
            TestAssert.Equal(0, releases, "dedicated handoff never publishes fake release or boost");
            TestAssert.Equal(phase == MechanicalClawState.Aim ? 0 : 1, unpins,
                "dedicated handoff releases exactly its actual residency, no phantom unpin in Aim");
            TestAssert.True((unpins == 0 || targetReadableDuringUnpin) &&
                claw.CurrentTarget == null && claw.ResidencyTarget == null,
                "unpin preserves old target during notification, then clears both references");
            TestAssert.Equal(serialAtIdle, claw.CurrentFrame.NextCastId, "handoff does not rewind cast serial");
            TestAssert.True(player.Velocity.X > 0f, "free jet produces visible horizontal velocity after handoff");
            TestAssert.Near(inheritedAtIdle.Y + (floorAtHandoff ? 0f : 1850f / 60f), player.Velocity.Y, .004f,
                "handoff inherits actual upward body velocity once, no normal-release projection or JumpCut");
        }
        finally { Release(); root.Free(); }
    }

    // Existing-contract proof: a 1200px/s external incoming velocity exceeds
    // the jet's 850 powered projection ceiling but is not instantaneously capped.
    private static async Task HighIncomingVelocityIsInherited(SceneTree tree)
    {
        using var fixture = new FreeFixture(tree, false);
        await fixture.Start();
        fixture.Motor.ExternalImpulse(new Vector2(1200f, -100f));
        KeyEvent(Key.D, true); KeyEvent(Key.Shift, true);
        await Frames(tree, fixture.Input, 1);
        TestAssert.Near(1168.4229f, fixture.Player.Velocity.X, .004f,
            "incoming overspeed receives one 1.6/s air-drag tick, not the 850 or legacy 620 cap");
        TestAssert.Near(-69.166664f, fixture.Player.Velocity.Y, .004f,
            "orthogonal incoming velocity is inherited once and receives actual gravity");
        TestAssert.True(fixture.Motor.IsDashing && !fixture.Motor.AirDashAvailable,
            "overspeed still begins exactly one finite airborne pulse");
        Release();
    }

    // Break caught: Claw cancels on a sampled Shift before Motor rejects it.
    private static async Task RejectedShiftPreservesClaw(SceneTree tree)
    {
        using var floorShape = new RectangleShape2D { Size = new Vector2(800, 20) };
        var root = new Node2D { Name = "ThrusterRejectedClaw" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); root.AddChild(floor);
        root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14); root.AddChild(player); tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        int releases = 0;
        claw.ClawFrameChanged += (frame, _) => { if (frame.State == MechanicalClawState.Released) releases++; };
        try
        {
            Release(); await Frames(tree, input, 8);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.W, true); KeyEvent(Key.Shift, true); KeyEvent(Key.Space, true);
            await Frames(tree, input, 1);
            KeyEvent(Key.Shift, false); KeyEvent(Key.Space, false);
            await Frames(tree, input, 2);
            KeyEvent(Key.K, true); await Frames(tree, input, 1); KeyEvent(Key.K, false);
            TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup,
                "reject test genuinely begins an active native claw cast during burn");
            ulong nextCast = claw.CurrentFrame.NextCastId;
            KeyEvent(Key.Shift, true); await Frames(tree, input, 2);
            TestAssert.Equal(1, motor.AcceptedDashCount, "active pulse rejects second Shift");
            TestAssert.True(motor.RejectedDashCount > 0, "rejected active-pulse Shift is recorded");
            TestAssert.Equal(0, releases, "REJECTED Shift must not publish an ordinary claw release");
            TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or
                MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                "rejected Shift leaves the original claw process alive");
            TestAssert.True(claw.CurrentFrame.NextCastId >= nextCast, "reject cannot rewind cast identity");
        }
        finally { Release(); root.Free(); }
    }

    private static async Task GroundCombinedConsumesAirUse(SceneTree tree)
    {
        using var floorShape = new RectangleShape2D { Size = new Vector2(800, 20) };
        var root = new Node2D { Name = "ThrusterCombinedBudget" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = floorShape });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14);
        root.AddChild(player);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        int castFrames = 0;
        claw.ClawFrameChanged += (frame, _) => { if (frame.State == MechanicalClawState.Aim) castFrames++; };
        try
        {
            Release();
            await Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "combined Shift fixture begins on legal real support");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.W, true); KeyEvent(Key.Shift, true); KeyEvent(Key.Space, true); KeyEvent(Key.K, true);
            await Frames(tree, input, 1);
            TestAssert.Equal(1, motor.AcceptedDashCount, "simultaneous native Shift starts once");
            TestAssert.Equal(0, motor.AcceptedJumpCount, "winning Shift never starts ordinary Space jump");
            TestAssert.Equal(0, castFrames, "winning Shift consumes competing K cast");
            TestAssert.True(!motor.AirDashAvailable, "ground Shift plus ORIGINAL Space consumes the one air use");
            await Frames(tree, input, 15);
            TestAssert.Equal(1, motor.AcceptedDashCount, "held Shift does not refire after finite pulse");
            TestAssert.Equal(0, motor.AcceptedJumpCount, "consumed Space never buffers a delayed jump");
            Release();
            await Frames(tree, input, 2);
            TestAssert.True(!player.IsOnFloor(), "second request is genuinely airborne");
            KeyEvent(Key.Shift, true);
            await Frames(tree, input, 2);
            TestAssert.Equal(1, motor.AcceptedDashCount, "fresh air Shift is rejected after combined ground consumption");
            TestAssert.True(motor.RejectedDashCount > 0, "air rejection is observed by actual Motor");
            Release();
            await Frames(tree, input, 4);
        }
        finally
        {
            Release();
            root.Free();
        }
    }

    internal static async Task Frames(SceneTree tree, PlayerInputReader input, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            ulong before = input.CurrentFrameSequence;
            for (int wait = 0; wait < 8 && before == input.CurrentFrameSequence; wait++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            TestAssert.True(input.CurrentFrameSequence > before, "automatic input physics advances");
        }
    }

    internal static void KeyEvent(Key key, bool pressed)
    {
        using var native = new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed, Echo = false };
        Input.ParseInputEvent(native);
        Input.FlushBufferedEvents();
    }

    internal static void Release()
    {
        foreach (Key key in new[] { Key.W, Key.A, Key.S, Key.D, Key.Shift, Key.Space, Key.K, Key.J, Key.L, Key.Q })
            KeyEvent(key, false);
    }

    private readonly record struct FreeSample(Vector2 Velocity, bool Burning);

    private sealed class FreeFixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly bool _supported;
        private readonly List<Shape2D> _ownedShapes = new();
        internal Node2D Root { get; } = new() { Name = "ThrusterFreePhysics" };
        internal CharacterBody2D Player { get; }
        internal PlayerInputReader Input => Player.GetNode<PlayerInputReader>("Input");
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal List<FreeSample> Starts { get; } = new();

        internal FreeFixture(SceneTree tree, bool supported)
        {
            _tree = tree; _supported = supported;
            if (supported) AddSolid(new Vector2(0, 60), new Vector2(4000, 20));
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new Vector2(0, supported ? 14 : -500);
            Root.AddChild(Player);
            Root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
            {
                if (Input.RawFrame.DashPressed) Starts.Add(new(Player.Velocity, Motor.IsDashing));
            } });
        }

        internal StaticBody2D AddSolid(Vector2 position, Vector2 size)
        {
            var shape = new RectangleShape2D { Size = size }; _ownedShapes.Add(shape);
            var body = new StaticBody2D { Position = position, CollisionLayer = CombatCollisionLayers.World };
            body.AddChild(new CollisionShape2D { Shape = shape }); Root.AddChild(body); return body;
        }

        internal async Task Start()
        {
            Release(); _tree.Root.AddChild(Root);
            await Frames(_tree, Input, _supported ? 8 : 1);
            TestAssert.True(Player.IsOnFloor() == _supported, "free dynamics fixture has intended real support state");
        }

        public void Dispose()
        {
            Release(); _tree.Paused = false;
            if (GodotObject.IsInstanceValid(Root)) Root.Free();
            foreach (Shape2D shape in _ownedShapes) shape.Dispose();
        }
    }
}

internal sealed partial class ThrusterPostMotorObserver : Node
{
    internal Action Observe;
    public override void _PhysicsProcess(double delta) => Observe?.Invoke();
}
