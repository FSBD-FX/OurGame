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
    private bool _armed, _disposed, _pendingK, _pendingJ, _counterStarted, _enemyDied;
    private bool _sampledK;
    private ulong _issuedSequence, _issuedPhysics, _expectedCast, _latchedCast, _stunnedCast;
    private ulong _lastApplicationPhysics, _stunPhysics, _lastJPhysics, _castDeadline;
    private ulong _deathPhysics, _lastCounterSample, _lastCounterSequence;
    private int _expectedApplications, _acceptedJ, _completedJ, _margin = -1;
    private BellBoltSentryState _previousState;
    private string _badReceipt;
    private string _aimReceipt;

    internal SteamSentryRecoveryDriver(CharacterBody2D player, BellBoltSentry sentry,
        KeyboardRouteInputDriver driver)
    {
        _player = player; _sentry = sentry; _driver = driver;
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
    }
    internal ulong SentryId { get; }
    internal string Stage { get; private set; } = "WaitingForRealRetirement";
    internal int CastCommands { get; private set; }
    internal int AttackCommands { get; private set; }
    internal int Applications { get; private set; }
    internal bool Completed { get; private set; }

    // Production encounter removal may precede the final J recovery. A native
    // death receipt, not invalidity/cached HP, transfers ownership to this
    // player-only finite tail. Never access the retired Sentry or its children.
    internal bool TryFinishRetiredCounter()
    {
        if (!_enemyDied) return false;
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
        if (frame.State == BellBoltSentryState.Stunned && _previousState != BellBoltSentryState.Stunned)
        {
            if (_badReceipt == null && _armed && (!_pendingK || _latchedCast != _expectedCast))
                _badReceipt = "new Stunned without the pending exact K receipt";
            _stunPhysics = Engine.GetPhysicsFrames();
            _stunnedCast = _latchedCast;
        }
        _previousState = frame.State;
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
    private bool FullSupport()
    {
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
                MathF.Abs(feet - top) < 1f) return true;
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

    internal bool TryDrive(bool observedRealBoltRetirement, int remainingFrames)
    {
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
        bool guard = bolt || _sentry.CurrentFrame.State == BellBoltSentryState.Fire ||
            timeline.Phase == AttackPhase.Telegraph && timeline.PhaseRemainingSeconds <= .40f;
        Neutral();
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
        Vector2 delta = _sentry.GlobalPosition - _player.GlobalPosition;
        float gap = MathF.Abs(delta.X), side = MathF.Sign(delta.X);
        if (!delta.IsFinite() || gap < 8f || MathF.Abs(delta.Y) > 46f) Fail("unsupported horizontal combat geometry");
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
        float safe = SafeSeconds(), single = _fullAttack + 8f / _hz;
        bool warningPermitsJ = timeline.Phase != AttackPhase.Telegraph || timeline.PhaseRemainingSeconds > .70f;
        float energy = _energy.EnergyRatio * _energy.Tuning.MaxEnergy;
        bool reserve = SentryCounterEnergyBudget.CanSpendAttack(energy, _energy.Tuning.ToSettings(), _sentry.Settings, (int)_hz);
        float doubleWindow = 26f / _hz + _fullAttack + 8f / _hz;
        bool prepareTwo = SentryCounterEnergyBudget.CanSpendAttack(energy - _energy.Tuning.AttackCost,
            _energy.Tuning.ToSettings(), _sentry.Settings, (int)_hz) && safe >= doubleWindow;
        if (Applications > 0 && reserve && warningPermitsJ && safe >= single &&
            (_counterStarted || prepareTwo) && _modes.CanBeginAttack &&
            (AttackCommands == 0 || now - _lastJPhysics >= 26UL))
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
        if (GodotObject.IsInstanceValid(_enemyHealth)) _enemyHealth.Died -= OnEnemyDied;
        if (Live(_player))
        {
            _driver.Command(InputActions.Claw, false); _driver.Command(InputActions.Attack, false);
            _driver.Command(InputActions.MoveLeft, false); _driver.Command(InputActions.MoveRight, false);
            _driver.Command(InputActions.KeyboardShield, false);
        }
    }
}
