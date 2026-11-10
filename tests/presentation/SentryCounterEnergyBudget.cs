using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Sentry;
using Knight.Player;
using Knight.Shared;

namespace Knight.Tests.Presentation;

// Test-route policy only; never changes the player's energy or enemy tuning.
internal static class SentryCounterEnergyBudget
{
    internal static bool CanSpendAttack(float energy, PlayerEnergySettings settings,
        BellBoltSentrySettings sentry, int physicsHz)
    {
        if (physicsHz <= 0 || !float.IsFinite(energy)) return false;
        // Keep the complete existing guard condition: .40s of Telegraph, then
        // Fire OR any live bolt. Flight/Fire overlap; a missed bolt can travel
        // all the way to MaximumTravel, not merely to the player's current x.
        double leadTicks = Math.Ceiling(.40d * physicsHz);
        double threatTicks = Math.Ceiling(Math.Max(sentry.FireSeconds,
            (double)sentry.MaximumTravel / sentry.BoltSpeed) * physicsHz);
        double reserve = (leadTicks + threatTicks + 3d) * settings.ShieldDrainPerSecond / physicsHz;
        // Zero energy closes the real shield in that same sample. No regen
        // credit is assumed: J resets its delay and held shield cannot regen.
        return energy > settings.AttackCost + reserve;
    }
}

// Test-only synchronous policy. The consumer owns exactly one ordinary physics
// wait per call; no state, energy, health, transform or enemy clock is written.
internal readonly record struct SteamSentryAmbientDanger(bool Observed, bool Defend,
    Vector2 GuardSource, float SafeCounterSeconds)
{
    internal ulong ForeignMinimumActor { get; init; }
    internal string ForeignMinimumState { get; init; }
    internal AttackPhase ForeignMinimumPhase { get; init; }
    internal float ForeignMinimumRemaining { get; init; }
    internal string ForeignMinimumReason { get; init; }
    internal int ForeignCounterObservations { get; init; }
}

internal sealed class SteamSentryRecoveryDriver : IDisposable
{
    private readonly CharacterBody2D _player;
    private readonly BellBoltSentry _sentry;
    private readonly KeyboardRouteInputDriver _driver;
    private readonly PlayerInputReader _input;
    private readonly PlayerMotor _motor;
    private readonly PlayerCombat _combat;
    private readonly PlayerEnergyController _energy;
    private readonly PlayerShieldController _shield;
    private readonly PlayerActionModeController _modes;
    private readonly MechanicalClawController _claw;
    private readonly ThreadTarget2D _target;
    private readonly LegacyThreadInteractionAdapter _adapter;
    private readonly HealthComponent _health, _enemyHealth;
    private readonly ulong _playerId, _targetId;
    private readonly int _initialApplications;
    private readonly float _hz, _fullAttack;
    private readonly float _preferredSide;
    private bool _restoringStance;
    private bool _armed, _disposed, _pendingK, _pendingJ, _counterStarted, _enemyDied;
    private bool _sampledK;
    private ulong _issuedSequence, _issuedPhysics, _expectedCast, _latchedCast, _stunnedCast;
    private ulong _lastApplicationPhysics, _stunPhysics, _lastJPhysics, _castDeadline;
    private ulong _deathPhysics, _lastCounterSample, _lastCounterSequence;
    private int _expectedApplications, _acceptedJ, _completedJ, _margin = -1;
    private BellBoltSentryState _previousState;
    private string _badReceipt;
    private string _aimReceipt;
    private bool _energyObserved;
    private PlayerEnergyFrame _observedEnergy;
    private ulong _energyPhysics;
    private int _energyEvents;
    private BellBoltProjectile _observedShot;
    private ulong _observedShotAttack, _observedShotId, _shotRetiredPhysics;
    private bool _observedShotRetired;
    private int _shotReceiptFaults;

    internal SteamSentryRecoveryDriver(CharacterBody2D player, BellBoltSentry sentry,
        KeyboardRouteInputDriver driver)
    {
        _player = player; _sentry = sentry; _driver = driver;
        _preferredSide = MathF.Sign(sentry.GlobalPosition.X - player.GlobalPosition.X);
        _playerId = player.GetInstanceId(); SentryId = sentry.GetInstanceId();
        _input = player.GetNode<PlayerInputReader>("Input");
        _motor = player.GetNode<PlayerMotor>("Motor");
        _combat = player.GetNode<PlayerCombat>("Combat");
        _energy = player.GetNode<PlayerEnergyController>("Energy");
        _shield = player.GetNode<PlayerShieldController>("Shield");
        _modes = player.GetNode<PlayerActionModeController>("ActionModes");
        _claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        _health = player.GetNode<HealthComponent>("Health");
        _enemyHealth = sentry.GetNode<HealthComponent>("Health");
        _target = sentry.GetNode<ThreadTarget2D>("ThreadTarget");
        _targetId = _target.GetInstanceId();
        _adapter = _target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
        _initialApplications = _adapter.ApplicationCount;
        _hz = Engine.PhysicsTicksPerSecond;
        _fullAttack = _combat.Tuning.AttackStartupSeconds + _combat.Tuning.AttackActiveSeconds +
            _combat.Tuning.AttackRecoverySeconds;
        _previousState = sentry.CurrentFrame.State;
        _claw.ClawFrameChanged += OnClaw;
        _sentry.FrameChanged += OnSentry;
        _combat.AttackStarted += OnAttack;
        _enemyHealth.Died += OnEnemyDied;
        _energy.EnergyFrameChanged += OnEnergy;
    }
    internal ulong SentryId { get; }
    internal string Stage { get; private set; } = "WaitingForRealRetirement";
    internal int CastCommands { get; private set; }
    internal int AttackCommands { get; private set; }
    internal int Applications { get; private set; }
    internal bool Completed { get; private set; }
    internal bool HasRetiredOwner => _enemyDied;
    internal ulong DecisionPhysics { get; private set; }
    internal string TimingObservation { get; private set; } = "not_evaluated";
    internal string TimingSignature { get; private set; } = "not_evaluated";
    internal bool HasNativeGoneShotFor(ulong owner, ulong attack, ulong now) =>
        !_disposed && owner == SentryId && _observedShotAttack != 0 && _observedShotAttack == attack &&
        _observedShotId != 0 && _observedShotRetired && now > _shotRetiredPhysics &&
        _observedShot != null && !GodotObject.IsInstanceValid(_observedShot);
    internal string TransitionSignature(SteamSentryAmbientDanger ambient) =>
        $"{Stage}/{TimingSignature}/{_previousState}/{ambient.Defend}/{ambient.ForeignMinimumActor}/" +
        $"{ambient.ForeignMinimumPhase}/{_energyObserved}/{_observedEnergy.ShieldSustained}/" +
        $"{_observedEnergy.State.RegenDelayRemaining > 0f}";

    private void OnEnergy(PlayerEnergyFrame frame)
    {
        // A passive value receipt, not a request, model step, input or reset.
        _energyObserved = true; _observedEnergy = frame;
        _energyPhysics = Engine.GetPhysicsFrames(); _energyEvents++;
    }

    // Passive values only. The caller catches diagnostic faults separately;
    // no scan, shape query, input, wait or clock advance occurs here.
    internal string DescribeDecision(SteamSentryAmbientDanger ambient)
    {
        string owner = Live(_sentry)
            ? $"own_state={_sentry.CurrentFrame.State} own_phase={_sentry.CurrentFrame.AttackTimeline.Phase} " +
              $"own_remaining={_sentry.CurrentFrame.AttackTimeline.PhaseRemainingSeconds:R} sentry={_sentry.GlobalPosition} " +
              $"own_hp={_enemyHealth.CurrentHealth}"
            : "own_native_retired=true";
        return $"decision_physics={DecisionPhysics} stage={Stage} {owner} " +
            $"player={_player.GlobalPosition} velocity={_player.Velocity} sequence={_input.CurrentFrameSequence} " +
            $"axis={_input.Current.MoveAxis:R} aim={_input.KeyboardAimDirection} " +
            $"raw_WASDKJL={Input.IsPhysicalKeyPressed(Key.W)}/{Input.IsPhysicalKeyPressed(Key.A)}/" +
            $"{Input.IsPhysicalKeyPressed(Key.S)}/{Input.IsPhysicalKeyPressed(Key.D)}/" +
            $"{Input.IsPhysicalKeyPressed(Key.K)}/{Input.IsPhysicalKeyPressed(Key.J)}/{Input.IsPhysicalKeyPressed(Key.L)} " +
            $"sample_KJL={_input.Current.ClawPressed}/{_input.Current.AttackPressed}/{_input.Current.ShieldHeld} " +
            $"shield={_shield.IsActive}/{_shield.GuardDirection} claw={_claw.CurrentFrame.State} " +
            $"hp={_health.CurrentHealth} energy_points={_energy.EnergyRatio * _energy.Tuning.MaxEnergy:R} " +
            $"energy_event_observed={_energyObserved} energy_event_physics={_energyPhysics} energy_events={_energyEvents} " +
            $"energy_event_value={_observedEnergy.State.Energy:R} regen_delay={_observedEnergy.State.RegenDelayRemaining:R} " +
            $"energy_event_shield_sustained={_observedEnergy.ShieldSustained} " +
            $"shot_attack={_observedShotAttack} shot_id={_observedShotId} shot_retired={_observedShotRetired} " +
            $"shot_retired_physics={_shotRetiredPhysics} shot_receipt_faults={_shotReceiptFaults} " +
            $"heat={_energy.HeatRatio:R} overheat={_energy.IsOverheated} " +
            $"K={CastCommands}/{Applications} J={AttackCommands}/{_acceptedJ}/{_completedJ} " +
            $"pending_KJ={_pendingK}/{_pendingJ} counter_started={_counterStarted} restoring={_restoringStance} " +
            $"foreign_defend={ambient.Defend} foreign_safe={ambient.SafeCounterSeconds:R} " +
            $"foreign_min={ambient.ForeignMinimumActor}/{ambient.ForeignMinimumState}/{ambient.ForeignMinimumPhase}/" +
            $"{ambient.ForeignMinimumRemaining:R}/{ambient.ForeignMinimumReason} " +
            $"counter_observations={ambient.ForeignCounterObservations} timing=({TimingObservation})";
    }

    // Production encounter removal may precede the final J recovery. A native
    // death receipt, not invalidity/cached HP, transfers ownership to this
    // player-only finite tail. Never access the retired Sentry or its children.
    internal bool TryFinishRetiredCounter(SteamSentryAmbientDanger ambient = default)
    {
        if (!_enemyDied) return false;
        DecisionPhysics = Engine.GetPhysicsFrames();
        TimingObservation = TimingSignature = "retired_tail_no_J_admission";
        if (_disposed || !_armed || _pendingK || AttackCommands != 2 || _acceptedJ != 2)
            Fail("retirement lacks settled K and two accepted real counters");
        if (!Live(_player) || !Live(_input) || !Live(_combat) || !Live(_health) ||
            _player.GetInstanceId() != _playerId || _health.CurrentHealth <= 0 || !FullSupport())
            Fail("retired counter lost its actual player/support");
        if (_badReceipt != null) Fail(_badReceipt);
        ulong now = Engine.GetPhysicsFrames();
        if (now - _deathPhysics > (ulong)Math.Ceiling(_fullAttack * _hz) + 8UL)
            Fail("retired counter exceeded its non-resetting saved-clock tail");
        Neutral();
        RequestAmbientGuard(ambient);
        if (AwaitCounterTail(now)) return true;
        Completed = _completedJ == 2 && !_combat.IsAttacking;
        if (!Completed) Fail("retired counter lacks complete attacks and six fresh samples");
        Stage = "CompletedRetiredCounter";
        return true;
    }

    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) &&
        node.IsInsideTree() && !node.IsQueuedForDeletion();
    private void Fail(string reason) => throw new InvalidOperationException(
        $"Steam Sentry recovery {reason}; stage={Stage} physics={Engine.GetPhysicsFrames()} " +
        $"K={CastCommands}/{Applications} J={AttackCommands}/{_acceptedJ}/{_completedJ}");

    // Callbacks only record actual native receipts. Never issue commands or throw
    // inside the synchronous Latched -> Released / health / presentation delivery.
    private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (!_pendingK) return;
        if (frame.State == MechanicalClawState.Aim)
            _aimReceipt = $"seq={_input.CurrentFrameSequence}/{_issuedSequence} " +
                $"K={_input.Current.ClawPressed}/{Input.IsPhysicalKeyPressed(Key.K)} physics={Engine.GetPhysicsFrames()}";
        if (frame.State == MechanicalClawState.Aim && _input.CurrentFrameSequence > _issuedSequence &&
            _input.Current.ClawPressed && Input.IsPhysicalKeyPressed(Key.K)) _sampledK = true;
        if (frame.State != MechanicalClawState.Latched) return;
        if (!_sampledK || frame.CastId != _expectedCast || target != _target ||
            frame.LockedTargetRuntimeId != _targetId || _adapter.ApplicationCount != _expectedApplications)
        { _badReceipt = $"wrong K/target/application receipt sampled={_sampledK} " +
            $"cast={frame.CastId}/{_expectedCast} target={target?.GetInstanceId()}/{_targetId} " +
            $"locked={frame.LockedTargetRuntimeId} apps={_adapter.ApplicationCount}/{_expectedApplications} Aim=({_aimReceipt})"; return; }
        _latchedCast = frame.CastId;
        Applications++;
        _lastApplicationPhysics = Engine.GetPhysicsFrames();
    }
    private void OnSentry(BellBoltSentryFrame frame)
    {
        if (frame.FireRequested)
        {
            // Actor emits FrameChanged AFTER its one FireRequested launch.
            // Observe that exact direct child, not any old retirement boolean
            // or un-attributed ShieldResolved event. Missing birth stays unsafe.
            if (_observedShot != null) _observedShot.Retired -= OnObservedShotRetired;
            _observedShot = null; _observedShotAttack = _observedShotId = _shotRetiredPhysics = 0;
            _observedShotRetired = false;
            try
            {
                ulong attack = frame.AttackTimeline.AttackId;
                var shot = _sentry.GetNodeOrNull<BellBoltProjectile>($"BellBolt{attack}");
                if (attack != 0 && Live(shot) && !shot.IsRetired)
                {
                    _observedShot = shot; _observedShotAttack = attack; _observedShotId = shot.GetInstanceId();
                    shot.Retired += OnObservedShotRetired;
                }
            }
            catch { _shotReceiptFaults++; } // Never throw or issue input in callbacks.
        }
        if (frame.State == BellBoltSentryState.Stunned && _previousState != BellBoltSentryState.Stunned)
        {
            if (_badReceipt == null && _armed && (!_pendingK || _latchedCast != _expectedCast))
                _badReceipt = "new Stunned without the pending exact K receipt";
            _stunPhysics = Engine.GetPhysicsFrames();
            _stunnedCast = _latchedCast;
        }
        _previousState = frame.State;
    }
    private void OnObservedShotRetired(BellBoltProjectile shot)
    {
        if (_disposed || !ReferenceEquals(shot, _observedShot)) return;
        _observedShotRetired = true; _shotRetiredPhysics = Engine.GetPhysicsFrames();
    }
    private void OnAttack(ulong id, int direction)
    {
        if (!_armed) return;
        if (!_pendingJ || !Input.IsPhysicalKeyPressed(Key.J) || direction != (int)AttackDirection.Horizontal)
        { _badReceipt = "unexpected accepted J"; return; }
        _acceptedJ++;
    }
    private void OnEnemyDied()
    {
        if (!_armed) return; // A legitimate baseline kill is not our counter sequence.
        _enemyDied = true;
        _deathPhysics = Engine.GetPhysicsFrames();
    }

    private bool LiveBolt(out Vector2 source)
    {
        source = _sentry.GetNode<Node2D>("Muzzle").GlobalPosition;
        Godot.Collections.Array<Node> children = _sentry.GetChildren();
        using Godot.Collections.Array backing = (Godot.Collections.Array)children;
        foreach (Node child in children)
            if (child is BellBoltProjectile bolt && Live(bolt) && !bolt.IsRetired)
            { source = bolt.GlobalPosition; return true; }
        return false;
    }
    private bool FullSupport() => FullSupport(out _, out _);
    private bool FullSupport(out float floorLeft, out float floorRight)
    {
        floorLeft = floorRight = 0f;
        CollisionShape2D body = _player.GetNode<CollisionShape2D>("BodyCollider");
        if (!_player.IsOnFloor() || body.Disabled || body.Shape is not CapsuleShape2D capsule) return false;
        for (int index = 0; index < _player.GetSlideCollisionCount(); index++)
        {
            using KinematicCollision2D contact = _player.GetSlideCollision(index);
            if (contact.GetNormal().Y >= -.7f || contact.GetCollider() is not StaticBody2D floor || !Live(floor) ||
                contact.GetColliderShape() is not CollisionShape2D shape || !Live(shape) || shape.Disabled ||
                shape.Shape is not RectangleShape2D rectangle) continue;
            Transform2D b = body.GlobalTransform, s = shape.GlobalTransform;
            if (!b.Origin.IsFinite() || !s.Origin.IsFinite() || MathF.Abs(b.X.Y) > .001f ||
                MathF.Abs(b.Y.X) > .001f || MathF.Abs(s.X.Y) > .001f || MathF.Abs(s.Y.X) > .001f) continue;
            float radius = capsule.Radius * MathF.Abs(b.X.X);
            float width = rectangle.Size.X * MathF.Abs(s.X.X) * .5f;
            float feet = b.Origin.Y + capsule.Height * MathF.Abs(b.Y.Y) * .5f;
            float top = s.Origin.Y - rectangle.Size.Y * MathF.Abs(s.Y.Y) * .5f;
            if (b.Origin.X - radius >= s.Origin.X - width && b.Origin.X + radius <= s.Origin.X + width &&
                MathF.Abs(feet - top) < 1f)
            { floorLeft = s.Origin.X - width; floorRight = s.Origin.X + width; return true; }
        }
        return false;
    }
    private void Neutral()
    {
        _driver.PointerButton(MouseButton.Right, false, _player, _player.GlobalPosition);
        _driver.Command(InputActions.MoveLeft, false); _driver.Command(InputActions.MoveRight, false);
        _driver.Command(InputActions.MoveUp, false); _driver.Command(InputActions.MoveDown, false);
        _driver.Command(InputActions.Jump, false); _driver.Command(InputActions.Dash, false);
    }
    private bool RequestAmbientGuard(SteamSentryAmbientDanger ambient)
    {
        if (!ambient.Observed || !ambient.Defend || _combat.IsAttacking || !_shield.CanBeginGuard) return false;
        Stage = "GuardActualForeignDanger";
        _driver.PointerButton(MouseButton.Right, true, _player, ambient.GuardSource);
        return true;
    }
    private float SafeSeconds()
    {
        AttackTimelineFrame timeline = _sentry.CurrentFrame.AttackTimeline;
        if (_sentry.CurrentFrame.State == BellBoltSentryState.Stunned && _stunnedCast != 0)
            return MathF.Max(0f, _sentry.Settings.ThreadStunSeconds -
                (Engine.GetPhysicsFrames() - _stunPhysics + 1UL) / _hz) +
                _sentry.Settings.TelegraphSeconds - .40f;
        return timeline.Phase switch
        {
            AttackPhase.Telegraph => MathF.Max(0f, timeline.PhaseRemainingSeconds - .40f),
            AttackPhase.Recovery => timeline.PhaseRemainingSeconds + _sentry.Settings.TelegraphSeconds - .40f,
            _ => 0f // None/Idle is not a cached or infinite Stunned window.
        };
    }

    private bool AwaitCounterTail(ulong now)
    {
        if (_pendingJ)
        {
            Stage = "AwaitActualJ";
            // As with K, do not release raw input between the native Input and
            // Combat consumers. Keep the minimum hold and require acceptance.
            if (now - _issuedPhysics >= 2UL && _acceptedJ == AttackCommands)
            {
                _driver.Command(InputActions.Attack, false);
                _pendingJ = false;
            }
            if (now - _issuedPhysics > 3UL && _acceptedJ != AttackCommands)
                Fail("fresh J was not accepted");
            return true;
        }
        if (_completedJ >= AttackCommands) return false;
        Stage = "FullJAndSixSafetySamples";
        if (_combat.IsAttacking) return true;
        // Repeated calls in one engine/input sample cannot manufacture margin.
        if (now <= _lastCounterSample || _input.CurrentFrameSequence <= _lastCounterSequence) return true;
        _lastCounterSample = now;
        _lastCounterSequence = _input.CurrentFrameSequence;
        if (_margin < 0) { _margin = 6; return true; }
        if (--_margin > 0) return true;
        _completedJ++; _margin = -1;
        return false;
    }

    internal bool TryDrive(bool observedRealBoltRetirement, int remainingFrames,
        SteamSentryAmbientDanger ambient = default)
    {
        DecisionPhysics = Engine.GetPhysicsFrames();
        TimingObservation = TimingSignature = "not_evaluated";
        if (_disposed) Fail("used after Dispose");
        if (!Live(_player) || !Live(_sentry) || !Live(_input) || !Live(_claw) ||
            _player.GetInstanceId() != _playerId || _sentry.GetInstanceId() != SentryId ||
            _player.SceneFilePath != "res://scenes/player/Player.tscn" ||
            _sentry.SceneFilePath != "res://scenes/enemies/BellBoltSentry.tscn" || _health.CurrentHealth <= 0)
            Fail("owner identity/health invalid");
        if (!_armed)
        {
            // Take over only the exact untouched-HP2 recovery case. A baseline
            // J may still be in startup while HP is2, so let it finish first.
            if (!observedRealBoltRetirement || _enemyHealth.CurrentHealth != 2 ||
                _combat.IsAttacking || _input.Current.AttackPressed || Input.IsPhysicalKeyPressed(Key.J))
                return false;
            _armed = true;
        }
        if (_badReceipt != null) Fail(_badReceipt);
        if (!FullSupport()) Fail("lost full native floor support");
        ulong now = Engine.GetPhysicsFrames();
        bool bolt = LiveBolt(out Vector2 source);
        AttackTimelineFrame timeline = _sentry.CurrentFrame.AttackTimeline;
        bool emptyRetiredFireTail = _sentry.CurrentFrame.State == BellBoltSentryState.Fire &&
            HasNativeGoneShotFor(SentryId, timeline.AttackId, now);
        bool guard = bolt || _sentry.CurrentFrame.State == BellBoltSentryState.Fire && !emptyRetiredFireTail ||
            timeline.Phase == AttackPhase.Telegraph && timeline.PhaseRemainingSeconds <= .40f;
        Neutral();
        // The caller's one all-actor scan has already ranked this foreign
        // source above our own warning/bolt. Do not erase it with Neutral.
        if (RequestAmbientGuard(ambient)) return true;
        if (guard)
        {
            if (_combat.IsAttacking) Fail("authorized danger overlaps the complete J window");
            Stage = "GuardActualDanger";
            _driver.PointerButton(MouseButton.Right, true, _player, source);
            return true;
        }
        if (_pendingK)
        {
            Stage = "AwaitExactKAndNewStunned";
            // An awaited observer may run between Input and Claw in this same
            // physics tick. Sequence advance alone must not release the raw key
            // before the consuming Aim callback has observed it still held.
            if (_sampledK) _driver.Command(InputActions.Claw, false);
            if (now > _castDeadline || _claw.CurrentFrame.Failure != MechanicalClawFailure.None)
                Fail("K handshake/retraction did not finish within its saved-clock bound");
            if (_sampledK && _latchedCast == _expectedCast && _stunnedCast == _expectedCast &&
                _adapter.ApplicationCount == _expectedApplications && _claw.CurrentFrame.State == MechanicalClawState.Idle &&
                !Input.IsPhysicalKeyPressed(Key.K) && !_input.Current.ClawPressed)
                _pendingK = false;
            return true;
        }
        if (AwaitCounterTail(now)) return true;
        if (_enemyHealth.CurrentHealth <= 0)
        {
            Completed = AttackCommands == 2 && _completedJ == 2;
            if (!Completed) Fail("enemy death lacks two complete real J receipts");
            Stage = "Completed";
            return true;
        }
        if (emptyRetiredFireTail)
        {
            // Retirement alone is insufficient (Monitoring is deferred). This
            // exact born shot is now natively gone, the same Fire has no future
            // spawn pulse, and all FOREIGN defense already took priority above.
            // Only release the empty guard; do not attack or K earlier in Fire.
            Stage = "WaitRetiredOwnFireTail";
            return true;
        }
        Vector2 delta = _sentry.GlobalPosition - _player.GlobalPosition;
        float gap = MathF.Abs(delta.X), side = MathF.Sign(delta.X);
        // A directional keyboard guard can move across the former stance.
        // Only a quiet, supported original-clock iteration may recover it.
        if (delta.IsFinite() && MathF.Abs(delta.Y) <= 46f && _preferredSide != 0f &&
            (_restoringStance || delta.X * _preferredSide < 55f))
        {
            if (!FullSupport(out float left, out float right)) Fail("stance recovery lacks full native floor");
            CollisionShape2D body = _player.GetNode<CollisionShape2D>("BodyCollider");
            float radius = ((CapsuleShape2D)body.Shape).Radius * MathF.Abs(body.GlobalTransform.X.X);
            float stanceX = _sentry.GlobalPosition.X - _preferredSide * 60f;
            if (stanceX - radius < left + 2f || stanceX + radius > right - 2f)
                Fail("stance recovery target does not fit the actual support");
            HorizontalForecastResult stop = FreeMotionForecast.Neutral(_player.Velocity.X, true,
                _motor.MotionSettings, _motor.ThrusterState.Module, 1d / _hz, remainingFrames, 2f);
            if (!stop.SpeedThresholdReached) Fail("stance braking exceeds the original remaining clock");
            float error = stanceX - (_player.GlobalPosition.X + stop.NetDistance);
            int direction = MathF.Abs(error) > 1.5f ? MathF.Sign(error) : 0;
            bool settled = delta.X * _preferredSide is >= 55f and <= 64f &&
                MathF.Abs(_player.Velocity.X) <= 2f && MathF.Abs(_input.Current.MoveAxis) <= .001f && direction == 0;
            _restoringStance = !settled;
            if (!settled)
            {
                Stage = "RestoreActualCounterStance";
                _driver.Command(InputActions.MoveLeft, direction < 0);
                _driver.Command(InputActions.MoveRight, direction > 0);
                return true;
            }
        }
        if (!delta.IsFinite() || gap < 8f || MathF.Abs(delta.Y) > 46f)
            Fail($"unsupported horizontal combat geometry; player={_player.GlobalPosition} sentry={_sentry.GlobalPosition} " +
                $"delta={delta} gap={gap:R} nonfinite={!delta.IsFinite()} too_near={gap < 8f} vertical={MathF.Abs(delta.Y) > 46f} " +
                $"velocity={_player.Velocity} input_sequence={_input.CurrentFrameSequence} axis={_input.Current.MoveAxis:R} " +
                $"WASD={Input.IsPhysicalKeyPressed(Key.W)}/{Input.IsPhysicalKeyPressed(Key.A)}/" +
                $"{Input.IsPhysicalKeyPressed(Key.S)}/{Input.IsPhysicalKeyPressed(Key.D)} " +
                $"L={Input.IsPhysicalKeyPressed(Key.L)}/{_input.Current.ShieldHeld}/{_shield.IsActive} " +
                $"own_guard={guard} foreign_guard={ambient.Defend} last_stage_only=true");
        float speed = MathF.Abs(_player.Velocity.X);
        if (gap > 64f)
        {
            Stage = "ApproachAndBrake";
            if (_motor.ThrusterState.BurnRemaining > 0f)
                Fail("ground-neutral forecast cannot own an active jet");
            HorizontalForecastResult stop = FreeMotionForecast.Neutral(_player.Velocity.X, true,
                _motor.MotionSettings, _motor.ThrusterState.Module, 1d / _hz, remainingFrames, 2f);
            if (!stop.SpeedThresholdReached)
                Fail("ground neutral cannot settle within the caller's unchanged remaining clock");
            float braking = stop.ForwardDistance + 2f * speed / _hz;
            bool move = gap - 60f > braking;
            _driver.Command(side < 0f ? InputActions.MoveLeft : InputActions.MoveRight, move);
            return true;
        }
        if (speed > 2f || MathF.Abs(_input.Current.MoveAxis) > .001f || _input.Current.ShieldHeld || _shield.IsActive ||
            _combat.IsAttacking || _claw.CurrentFrame.State != MechanicalClawState.Idle) { Stage = "SampleNeutralStance"; return true; }
        float ambientSafe = !ambient.Observed ? float.PositiveInfinity :
            float.IsNaN(ambient.SafeCounterSeconds) || ambient.SafeCounterSeconds < 0f
                ? 0f : ambient.SafeCounterSeconds;
        float ownSafe = SafeSeconds();
        float safe = MathF.Min(ownSafe, ambientSafe), single = _fullAttack + 8f / _hz;
        bool warningPermitsJ = timeline.Phase != AttackPhase.Telegraph || timeline.PhaseRemainingSeconds > .70f;
        float energy = _energy.EnergyRatio * _energy.Tuning.MaxEnergy;
        bool reserve = SentryCounterEnergyBudget.CanSpendAttack(energy, _energy.Tuning.ToSettings(), _sentry.Settings, (int)_hz);
        float doubleWindow = 26f / _hz + _fullAttack + 8f / _hz;
        bool prepareTwo = SentryCounterEnergyBudget.CanSpendAttack(energy - _energy.Tuning.AttackCost,
            _energy.Tuning.ToSettings(), _sentry.Settings, (int)_hz) && ownSafe >= doubleWindow;
        bool beforeMode = Applications > 0 && reserve && warningPermitsJ && safe >= single &&
            (_counterStarted || prepareTwo);
        // Preserve the original short-circuit reads. A not-reached property
        // is not reported as a failed J gate by a shadow recomputation.
        bool? canBegin = beforeMode ? _modes.CanBeginAttack : null;
        bool? cadence = canBegin == true ? AttackCommands == 0 || now - _lastJPhysics >= 26UL : null;
        TimingSignature = $"{Applications > 0}/{reserve}/{warningPermitsJ}/{safe >= single}/" +
            $"{_counterStarted || prepareTwo}/{canBegin?.ToString() ?? "not_queried"}/{cadence?.ToString() ?? "not_queried"}";
        TimingObservation = $"evaluated=true apps={Applications > 0} reserve={reserve} warning={warningPermitsJ} " +
            $"own_safe={ownSafe:R} foreign_safe={ambientSafe:R} single={single:R} double={doubleWindow:R} " +
            $"prepare_two={prepareTwo} counter_started={_counterStarted} before_mode={beforeMode} " +
            $"can_begin={canBegin?.ToString() ?? "not_queried"} cadence={cadence?.ToString() ?? "not_queried"} " +
            $"optimistic_first_J_energy={MathF.Min(_energy.Tuning.MaxEnergy, energy + _energy.Tuning.RegenPerSecond * MathF.Max(0f, ownSafe - doubleWindow)):R}";
        if (beforeMode && canBegin == true && cadence == true)
        {
            if (AttackCommands >= 2) Fail("more than two counter commands");
            Stage = "IssueFreshJ"; _counterStarted = true; _pendingJ = true;
            _issuedPhysics = _lastJPhysics = now; _issuedSequence = _input.CurrentFrameSequence;
            AttackCommands++;
            _driver.Command(InputActions.Attack, true);
            return true;
        }
        if (_counterStarted) { Stage = "WaitCurrentCounterWindow"; return true; }
        if (_sentry.CurrentFrame.State == BellBoltSentryState.Stunned) { Stage = "NaturalRegeneration"; return true; }
        if (Applications > 0 && now - _lastApplicationPhysics < (ulong)_hz) { Stage = "ApplicationSpacing"; return true; }
        if (CastCommands >= 4) Fail("finite four-K ceiling could not prepare both counters");
        float flight = _player.GlobalPosition.DistanceTo(_target.WorldAnchor) / MechanicalClawSettings.Default.ShuttleSpeed;
        float fullCast = flight + _player.GlobalPosition.DistanceTo(_target.WorldAnchor) /
            MechanicalClawSettings.Default.RetractSpeed + 10f / _hz;
        if (ambientSafe < fullCast) { Stage = "WaitAllSourceKWindow"; return true; }
        if (timeline.Phase == AttackPhase.Telegraph && timeline.PhaseRemainingSeconds <= .40f + flight + 3f / _hz)
        { Stage = "WaitSafeKWindow"; return true; }
        MechanicalClawTargetResolution selection = new MechanicalClawTargetResolver().ResolveKeyboard(_player, _input.KeyboardAimDirection);
        if (!_modes.CanBeginGrapple || selection.Surface != _target || selection.Snapshot.RuntimeId != _targetId ||
            !selection.Snapshot.HasLineOfSight || !_target.TryGetEligibleReceiver(_player, out Node receiver) || receiver != _sentry)
        { Stage = "WaitExactKeyboardTarget"; return true; }
        Stage = "IssueFreshK"; _pendingK = true; _sampledK = false;
        _latchedCast = _stunnedCast = 0; _expectedCast = _claw.CurrentFrame.NextCastId;
        _expectedApplications = _initialApplications + CastCommands + 1;
        _issuedSequence = _input.CurrentFrameSequence; _issuedPhysics = now;
        float distance = _player.GlobalPosition.DistanceTo(_target.WorldAnchor);
        _castDeadline = now + (ulong)Math.Ceiling((flight + distance / MechanicalClawSettings.Default.RetractSpeed) * _hz) + 10UL;
        CastCommands++;
        _driver.Command(InputActions.Claw, true);
        return true;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (Live(_claw)) _claw.ClawFrameChanged -= OnClaw;
        if (Live(_sentry)) _sentry.FrameChanged -= OnSentry;
        if (Live(_combat)) _combat.AttackStarted -= OnAttack;
        if (Live(_energy)) _energy.EnergyFrameChanged -= OnEnergy;
        if (_observedShot != null) _observedShot.Retired -= OnObservedShotRetired;
        _observedShot = null;
        if (GodotObject.IsInstanceValid(_enemyHealth)) _enemyHealth.Died -= OnEnemyDied;
        if (Live(_player))
        {
            _driver.Command(InputActions.Claw, false); _driver.Command(InputActions.Attack, false);
            _driver.Command(InputActions.MoveLeft, false); _driver.Command(InputActions.MoveRight, false);
            _driver.Command(InputActions.KeyboardShield, false);
        }
    }
}
