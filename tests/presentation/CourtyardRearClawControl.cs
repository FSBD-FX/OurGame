using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Shared;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// One test-only keyboard attempt. Step owns no clock: its caller spends exactly
// one ORIGINAL outer-loop physics frame when true is returned. No extra queries
// resolve a target; only an observed production Idle publication can admit K.
internal sealed class CourtyardRearClawControl : IDisposable
{
    private enum Stage { Dormant, Left, Brake, Aim, Flight, Right, Finish, Done, Rejected }
    private readonly CharacterBody2D _player;
    private readonly List<string> _rows = new(64);
    private readonly List<AttackTelegraphVisual> _leaves = new();
    private KeyboardRouteInputDriver _driver;
    private PlayerInputReader _input;
    private MechanicalClawController _claw;
    private PlayerCombat _combat;
    private PlayerShieldController _shield;
    private PlayerMotor _motor;
    private HealthComponent _health;
    private RustShellSquire _front;
    private RangedWeaponEnemy _rear;
    private ThreadTarget2D _target;
    private LegacyThreadInteractionAdapter _adapter;
    private Stage _stage;
    private bool _disposed, _subscribed, _ownA, _ownD, _ownK, _ownL;
    private bool _aimAck, _latched, _released, _revoked, _stunned, _idle;
    private bool _guardDemanded, _guardAck, _rightAck;
    private int _healthBefore, _jumpsBefore, _dashesBefore, _applicationsBefore, _edgesBefore;
    private int _loop, _omitted, _faults, _rawEdges, _acceptedEdges;
    private ulong _playerId, _targetId, _frontAttack, _rearAttack, _cast;
    private ulong _turnSequence, _neutralSequence, _issueSequence, _issuePhysics, _issueAuthority, _aimSequence, _rightSequence;
    private ulong _lastEdgeSequence;
    private string _fault = "none", _lastPublication = "none";
    private Preview _preview;

    private readonly record struct Preview(ulong Physics, ulong Sequence, ulong Authority,
        Transform2D Pose, Vector2 Velocity, Vector2 Aim, bool Clean, bool Neutral,
        bool LeftKey, MechanicalClawTargetResolution Selection);

    public bool Started { get; private set; }
    public bool Completed { get; private set; }

    public CourtyardRearClawControl(CharacterBody2D player) { _player = player; }

    // Validation contracts shared by the actual consumer and native controls.
    internal static bool AwaitingUnacceptedIdle(MechanicalClawFrame frame, ulong expectedCast,
        bool aimAcknowledged, int rawEdges, int acceptedEdges, bool physicalK) =>
        expectedCast > 0 && frame.State == MechanicalClawState.Idle && frame.CastId == 0 &&
        frame.NextCastId == expectedCast && frame.Failure == MechanicalClawFailure.None &&
        !aimAcknowledged && rawEdges == 0 && acceptedEdges == 0 && physicalK;
    internal static bool RetainedSupportAccepted(bool fullSupport, bool onFloor) => fullSupport && onFloor;
    internal static bool StunnedRevocationAccepted(bool historicalReceipt, bool currentRevocation) =>
        historicalReceipt && currentRevocation;

    public bool Step(RangedWeaponEnemy rear, Node2D selectedFront,
        PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger danger,
        float foreignSafe, int foreignSources, bool fullSupport, int loop)
    {
        if (_disposed || Completed || _stage == Stage.Rejected) return false;
        _loop = loop;
        // In particular, the null/default-danger fixture must not clear the
        // original driver's D intent, subscribe, or emit any input edge.
        if (!Started)
        {
            if (!Live(rear) || selectedFront is not RustShellSquire front || !Live(front) ||
                KeyboardRouteInputDriver.Current == null || !BindPlayer() ||
                !InitialQuiet(fullSupport) || !Window(rear, front, danger, foreignSafe, foreignSources, false))
                return false;
            ThreadTarget2D target = rear.GetNodeOrNull<ThreadTarget2D>("ThreadTarget");
            if (!Live(target)) return false;
            LegacyThreadInteractionAdapter adapter = target.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "LegacyThreadInteractionAdapter");
            if (!Live(adapter) || !target.IsAvailable ||
                !target.TryGetEligibleReceiver(_player, out Node receiver) || receiver != rear) return false;

            _rear = rear; _front = front; _target = target; _adapter = adapter;
            _driver = KeyboardRouteInputDriver.Current;
            _playerId = _player.GetInstanceId(); _targetId = target.GetInstanceId();
            _frontAttack = front.CurrentFrame.AttackTimeline.AttackId;
            _rearAttack = rear.CurrentFrame.AttackTimeline.AttackId;
            _healthBefore = _health.CurrentHealth;
            _jumpsBefore = _motor.AcceptedJumpCount; _dashesBefore = _motor.AcceptedDashCount;
            _applicationsBefore = adapter.ApplicationCount; _edgesBefore = _driver.ClawEdges;
            Started = true; _stage = Stage.Left;
            _claw.ClawFrameChanged += OnClaw;
            _rear.FrameChanged += OnRear;
            _subscribed = true;
            // Clear BOTH the real L edge and the driver's latent shield planner
            // hold, before clearing old primary/directional intent.
            _driver.PointerButton(MouseButton.Right, false, _player, _player.GlobalPosition);
            _driver.Command(InputActions.PrimaryAction, false);
            _driver.Command(InputActions.Attack, false);
            _driver.Command(InputActions.Claw, false);
            DirectionsOff();
            _turnSequence = _input.CurrentFrameSequence;
            _driver.Command(InputActions.MoveLeft, true); _ownA = true;
            Row("prepare-left", $"front_attack={_frontAttack} rear_attack={_rearAttack} " +
                $"foreign={foreignSources}/{foreignSafe:R} lead={Lead(target.WorldAnchor):R} K_issued=false");
            return true;
        }

        bool available = BoundAvailable() && rear == _rear && KeyboardRouteInputDriver.Current == _driver;
        if (_stage is Stage.Left or Stage.Brake)
        {
            if (!available || !InitialQuiet(fullSupport, allowDirection: true) ||
                !Window(_rear, _front, danger, foreignSafe, foreignSources, true) || _faults != 0)
                return Reject("pre-K current window/support/identity/observation lost");
            if (_stage == Stage.Left)
            {
                if (_preview.Sequence <= _turnSequence || !_preview.LeftKey ||
                    !_preview.Clean || _preview.Aim.Dot(Vector2.Left) < .999f) return true;
                _driver.Command(InputActions.MoveLeft, false); _ownA = false;
                _neutralSequence = _input.CurrentFrameSequence;
                _stage = Stage.Brake;
                Row("left-sampled-neutral", $"physics={_preview.Physics} sample={_preview.Sequence} " +
                    $"actual_aim={_preview.Aim} actual_A={_preview.LeftKey} clean={_preview.Clean} " +
                    "natural_brake=true K_issued=false");
                return true;
            }
            if (!Settled() || !FreshPreview()) return true;
            // This is the last, current-world admission, not the independent
            // Left diagnostic and not an old preview from before A/neutral.
            if (!ExactSelection(_preview.Selection) || !_target.IsAvailable ||
                !_target.TryGetEligibleReceiver(_player, out Node receiver) || receiver != _rear)
                return Reject("fresh production Left preview did not select the exact eligible rear");
            _cast = _claw.CurrentFrame.NextCastId;
            if (_cast == 0 || _cast == ulong.MaxValue) return Reject("unusable next cast identity");
            _issueSequence = _input.CurrentFrameSequence;
            _issuePhysics = Engine.GetPhysicsFrames();
            _issueAuthority = _claw.AuthorityRevision;
            _stage = Stage.Aim;
            _driver.Command(InputActions.Claw, true); _ownK = true;
            Row("K-issued", $"cast={_cast} preview_physics={_preview.Physics} " +
                $"preview_sequence={_preview.Sequence} authority={_preview.Authority} " +
                $"pose={_preview.Pose.Origin} velocity={_preview.Velocity} actual_aim={_preview.Aim} " +
                $"clean={_preview.Clean} neutral={_preview.Neutral} target={_preview.Selection.Snapshot.RuntimeId} " +
                $"exists={_preview.Selection.Snapshot.Exists} LOS={_preview.Selection.Snapshot.HasLineOfSight} " +
                $"legacy={_preview.Selection.Snapshot.IsLegacyInteraction} " +
                $"foreign={foreignSources}/{foreignSafe:R} lead={Lead(_target.WorldAnchor):R}");
            return true;
        }

        // Post-edge failures are strict. Never waive an already-issued cast or
        // cancel an applied cast merely because the old global scalar is zero.
        TestAssert.True(available, "courtyard rear K retains live original player/actors/driver");
        TestAssert.True(RetainedSupportAccepted(fullSupport, _player.IsOnFloor()),
            "courtyard rear K retains actual complete Step support after the edge and at completion");
        TestAssert.True(_faults == 0, $"courtyard rear K receipt fault: {_fault}");
        TestAssert.True(_health.CurrentHealth == _healthBefore && _health.CurrentHealth > 0 &&
            _player.GetInstanceId() == _playerId && _motor.AcceptedJumpCount == _jumpsBefore &&
            _motor.AcceptedDashCount == _dashesBefore && !_combat.IsAttacking && !_motor.IsDashing &&
            NoOtherActions(_input.Current) && NoOtherActions(_input.RawFrame) &&
            !Input.IsPhysicalKeyPressed(Key.J) && !Input.IsPhysicalKeyPressed(Key.Space) &&
            !Input.IsPhysicalKeyPressed(Key.Shift), "courtyard rear K preserves HP and forbids new J/jump/dash");
        TestAssert.True(_driver.ClawEdges == _edgesBefore + 1 && _rawEdges <= 1 && _acceptedEdges <= 1 &&
            _adapter.ApplicationCount <= _applicationsBefore + 1 &&
            _claw.CurrentFrame.Failure == MechanicalClawFailure.None,
            "courtyard rear K is one physical edge and at most one real adapter application");

        if (_stage == Stage.Aim)
        {
            if (!_aimAck)
            {
                TestAssert.True(Engine.GetPhysicsFrames() >= _issuePhysics &&
                    Engine.GetPhysicsFrames() - _issuePhysics <= 8 &&
                    _claw.AuthorityRevision == _issueAuthority &&
                    Window(_rear, _front, danger, foreignSafe, foreignSources, true),
                    "pending K keeps current authority/window and earns its actual Aim ACK within eight original ticks");
                return true; // Hold the SAME K, never resend a new edge.
            }
            _driver.Command(InputActions.Claw, false); _ownK = false;
            _stage = Stage.Flight;
            Row("K-ACK-release", $"issue={_issueSequence} ack={_aimSequence} raw_edges={_rawEdges}");
        }
        if (_latched && _released && !_revoked) ObserveRevocation();
        if (_stage == Stage.Flight)
        {
            if (!_latched || !_released || !_revoked || !_stunned) return true;
            TestAssert.True(_rear.CurrentFrame.State == RangedWeaponState.Stunned &&
                _rear.CurrentFrame.AttackTimeline.Phase == AttackPhase.None && ObserveRevocation(),
                "return direction is authorized by the currently revoked Stunned rear, not a sticky old receipt");
            _rightSequence = _input.CurrentFrameSequence;
            _driver.Command(InputActions.MoveRight, true); _ownD = true;
            _stage = Stage.Right;
            Row("return-right", $"actual_stunned=true revoked=true claw={_claw.CurrentFrame.State} " +
                "wait_for_full_retract=false");
        }

        bool demand = PrologueCanyonGeneratedMainRouteTests.Task10MixedDefendWindow(_shield, danger);
        _guardDemanded |= demand;
        if (!_rightAck && _input.CurrentFrameSequence > _rightSequence &&
            _input.Current.MoveAxis > 0 && Input.IsPhysicalKeyPressed(Key.D) &&
            _input.KeyboardAimDirection.Dot(Vector2.Right) > .999f)
        {
            if (!_rightAck) Row("right-sampled", "actual_D_turn=true");
            _rightAck = true;
            _driver.Command(InputActions.MoveRight, false); _ownD = false;
            _stage = Stage.Finish;
        }
        if (demand && _input.Current.KeyboardShieldHeld && _input.Current.ShieldHeld &&
            Input.IsPhysicalKeyPressed(Key.L) && _shield.IsActive &&
            _input.KeyboardAimDirection.Dot(Vector2.Right) > .999f &&
            _shield.GuardDirection.Dot(Vector2.Right) > .999f)
        {
            if (!_guardAck) Row("right-guard-ACK", $"source={danger.SourceActorId} phase={danger.Phase}");
            _guardAck = true;
        }
        // stopForGuard makes this an actual sampled turn followed by neutral
        // braking, not a renewed forward stride while the front enemy attacks.
        _driver.PointerButton(MouseButton.Right, demand, _player,
            _player.GlobalPosition + Vector2.Right, stopForGuard: true);
        _ownL = demand;
        if (!_rightAck || !_idle || !Settled() || Input.IsPhysicalKeyPressed(Key.A) ||
            Input.IsPhysicalKeyPressed(Key.D) || _input.Current.MoveAxis != 0 ||
            _guardDemanded && !_guardAck || demand &&
            (!_input.Current.KeyboardShieldHeld || !_shield.IsActive)) return true;

        TestAssert.True(_aimAck && _aimSequence > _issueSequence && _rawEdges == 1 && _acceptedEdges == 1 &&
            _latched && _released && _revoked && _stunned &&
            _adapter.ApplicationCount == _applicationsBefore + 1 &&
            _claw.CurrentFrame.State == MechanicalClawState.Idle &&
            _claw.CurrentFrame.NextCastId == _cast + 1 && !Input.IsPhysicalKeyPressed(Key.K),
            "courtyard rear K completes with exact cast/application/Latched/Released/Idle and right return receipts");
        Completed = true; _stage = Stage.Done;
        Row("completed", $"guard_demanded={_guardDemanded} guard_ACK={_guardAck} " +
            "full_goal_complete=false no_extra_wait=true");
        // Hand back in THIS original iteration. Dispose must not clear keys
        // subsequently owned by the resumed route policy.
        return false;
    }

    private bool BindPlayer()
    {
        if (!Live(_player)) return false;
        _input = _player.GetNodeOrNull<PlayerInputReader>("Input");
        _claw = _player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        _combat = _player.GetNodeOrNull<PlayerCombat>("Combat");
        _shield = _player.GetNodeOrNull<PlayerShieldController>("Shield");
        _motor = _player.GetNodeOrNull<PlayerMotor>("Motor");
        _health = _player.GetNodeOrNull<HealthComponent>("Health");
        return PlayerAvailable();
    }

    private bool PlayerAvailable() => Live(_player) && Live(_input) && Live(_claw) && Live(_combat) &&
        Live(_shield) && Live(_motor) && Live(_health) && !_player.GetTree().Paused &&
        _input.CanProcess() && _claw.CanProcess() && _input.IsPhysicsProcessing() && _claw.IsPhysicsProcessing();

    private bool BoundAvailable() => PlayerAvailable() && Live(_rear) && Live(_front) && Live(_target) &&
        Live(_adapter) && _target.GetInstanceId() == _targetId &&
        _rear.GetNodeOrNull<ThreadTarget2D>("ThreadTarget") == _target &&
        _target.GetNodeOrNull<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter") == _adapter &&
        Living(_rear) && Living(_front);

    internal static bool Live(Node value)
    {
        if (!GodotObject.IsInstanceValid(value) || !value.IsInsideTree()) return false;
        for (Node current = value; current != null; current = current.GetParent())
            if (!GodotObject.IsInstanceValid(current) || current.IsQueuedForDeletion()) return false;
        return true;
    }

    private static bool Living(Node actor)
    {
        if (!Live(actor)) return false;
        HealthComponent health = actor.GetNodeOrNull<HealthComponent>("Health");
        return Live(health) && health.CurrentHealth > 0;
    }

    private bool InitialQuiet(bool fullSupport, bool allowDirection = false) => PlayerAvailable() &&
        fullSupport && _player.IsOnFloor() && (allowDirection || Settled()) &&
        _health.CurrentHealth > 0 && !_combat.IsAttacking && !_motor.IsDashing &&
        _claw.CurrentFrame.State == MechanicalClawState.Idle &&
        _claw.CurrentFrame.Failure == MechanicalClawFailure.None && !_shield.IsActive &&
        Clean(_input.Current) && Clean(_input.RawFrame) &&
        !Input.IsPhysicalKeyPressed(Key.K) && !Input.IsPhysicalKeyPressed(Key.L) &&
        !Input.IsPhysicalKeyPressed(Key.J) && !Input.IsPhysicalKeyPressed(Key.Space) &&
        !Input.IsPhysicalKeyPressed(Key.Shift) && !Input.IsPhysicalKeyPressed(Key.W) &&
        !Input.IsPhysicalKeyPressed(Key.S);

    private bool Window(RangedWeaponEnemy rear, RustShellSquire front,
        PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger danger,
        float foreignSafe, int foreignSources, bool sameAttacks)
    {
        if (!Live(rear) || !Live(front) || !Living(rear) || !Living(front) ||
            rear.SceneFilePath != "res://scenes/enemies/residents/v001/precision-laser.tscn" ||
            rear.Role != RangedWeaponRole.Laser || !rear.HasPresentationAuthority ||
            rear.CurrentFrame.State != RangedWeaponState.Telegraph || front.CurrentState != RustShellState.Telegraph ||
            rear.GlobalPosition.X >= _player.GlobalPosition.X || front.GlobalPosition.X <= _player.GlobalPosition.X ||
            danger.CandidateObserverFaults != 0 || danger.MissingGuardSourceCount != 0 ||
            !danger.Threat || danger.SourceActorId == 0 || danger.AttackId == 0 ||
            danger.Phase != AttackPhase.Telegraph || danger.NearContactUnknownActive ||
            danger.NearContactOverlappingActive || foreignSources <= 0 || !float.IsFinite(foreignSafe) ||
            !float.IsFinite(danger.SafeCounterSeconds)) return false;
        ThreadTarget2D target = rear.GetNodeOrNull<ThreadTarget2D>("ThreadTarget");
        if (!Live(target) || !target.IsAvailable) return false;
        AttackTimelineFrame back = rear.CurrentFrame.AttackTimeline;
        AttackTimelineFrame ahead = front.CurrentFrame.AttackTimeline;
        float lead = Lead(target.WorldAnchor);
        return float.IsFinite(lead) && lead > 0 && back.Kind == EnemyAttackKind.PrismLaserBeam &&
            ahead.Kind == EnemyAttackKind.RustShellThrust && back.AttackId != 0 && ahead.AttackId != 0 &&
            (!sameAttacks || back.AttackId == _rearAttack && ahead.AttackId == _frontAttack) &&
            back.Phase == AttackPhase.Telegraph && ahead.Phase == AttackPhase.Telegraph &&
            float.IsFinite(back.PhaseRemainingSeconds) && float.IsFinite(ahead.PhaseRemainingSeconds) &&
            back.PhaseRemainingSeconds > lead && ahead.PhaseRemainingSeconds > lead &&
            foreignSafe > lead && danger.SafeCounterSeconds > lead;
    }

    private float Lead(Vector2 anchor) => _player.GlobalPosition.DistanceTo(anchor) /
        MechanicalClawSettings.Default.ShuttleSpeed + 8f / Engine.PhysicsTicksPerSecond;

    private bool Settled() => _player.Velocity.IsFinite() && MathF.Abs(_player.Velocity.X) <= .5f &&
        MathF.Abs(_player.Velocity.Y) <= .5f;

    private static bool NoOtherActions(PlayerInputFrame frame) => frame.KeyboardActions &&
        !frame.AttackPressed && !frame.JumpPressed && !frame.JumpHeld && !frame.DashPressed &&
        !frame.PrimaryPressed && !frame.PrimaryHeld && !frame.AttackModePressed && !frame.GrappleModePressed &&
        !frame.ThreadPressed && !frame.ThreadHeld && !frame.InteractPressed && !frame.LegacyShieldHeld;

    private static bool Clean(PlayerInputFrame frame) => NoOtherActions(frame) && !frame.ShieldHeld &&
        !frame.KeyboardShieldHeld && !frame.ClawPressed;

    private bool ExactSelection(MechanicalClawTargetResolution selection) => Live(selection.Surface) &&
        selection.Surface == _target && selection.Snapshot.RuntimeId == _targetId &&
        selection.Snapshot.Exists && selection.Snapshot.HasLineOfSight && selection.Snapshot.IsLegacyInteraction &&
        selection.Snapshot.WorldAnchor.IsFinite();

    private bool FreshPreview()
    {
        ulong physics = Engine.GetPhysicsFrames();
        return _preview.Physics > 0 && _preview.Physics <= physics && physics - _preview.Physics <= 1 &&
            _preview.Sequence == _input.CurrentFrameSequence && _preview.Sequence > _neutralSequence &&
            _preview.Authority == _claw.AuthorityRevision && _preview.Authority > 0 &&
            _preview.Pose.Origin.IsFinite() && _preview.Pose.X.IsFinite() && _preview.Pose.Y.IsFinite() &&
            _preview.Pose.IsEqualApprox(_player.GlobalTransform) && _preview.Velocity.IsEqualApprox(_player.Velocity) &&
            _preview.Clean && _preview.Neutral && !_preview.LeftKey &&
            _preview.Aim.Dot(Vector2.Left) > .999f && _input.KeyboardAimDirection.Dot(Vector2.Left) > .999f &&
            !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
            ExactSelection(_claw.KeyboardPreview) && _claw.KeyboardPreview == _preview.Selection;
    }

    private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D actualTarget)
    {
        if (!Started || Completed || _stage == Stage.Rejected || _disposed) return;
        try
        {
            if (!BoundAvailable()) { Fault("claw publication lost live ownership"); return; }
            if (frame.State == MechanicalClawState.Idle && _stage is Stage.Left or Stage.Brake)
            {
                _preview = new Preview(Engine.GetPhysicsFrames(), _input.CurrentFrameSequence, _claw.AuthorityRevision,
                    _player.GlobalTransform, _player.Velocity, _input.KeyboardAimDirection,
                    Clean(_input.Current) && Clean(_input.RawFrame) && !_shield.IsActive,
                    _input.Current.MoveAxis == 0 && !Input.IsPhysicalKeyPressed(Key.A) &&
                    !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.W) && !Input.IsPhysicalKeyPressed(Key.S),
                    Input.IsPhysicalKeyPressed(Key.A), _claw.KeyboardPreview);
                return;
            }
            if (_stage is Stage.Left or Stage.Brake) { Fault("non-Idle during preparation"); return; }
            if (_input.CurrentFrameSequence != _lastEdgeSequence)
            {
                _lastEdgeSequence = _input.CurrentFrameSequence;
                if (_input.RawFrame.ClawPressed) _rawEdges++;
                if (_input.Current.ClawPressed) _acceptedEdges++;
            }
            if (frame.Failure != MechanicalClawFailure.None) Fault($"claw failure {frame.Failure}");
            if (_stage == Stage.Aim && actualTarget == null &&
                _claw.AuthorityRevision == _issueAuthority &&
                AwaitingUnacceptedIdle(frame, _cast, _aimAck, _rawEdges, _acceptedEdges,
                    Input.IsPhysicalKeyPressed(Key.K)))
            {
                Row("pending-idle-not-ACK", $"cast={_cast} next={frame.NextCastId} " +
                    "raw_K=0 accepted_K=0 original_K_held=true completion_receipt=false");
                return;
            }
            if (frame.State == MechanicalClawState.Aim)
            {
                bool accepted = _input.CurrentFrameSequence > _issueSequence &&
                    _input.RawFrame.ClawPressed && _input.Current.ClawPressed && Input.IsPhysicalKeyPressed(Key.K) &&
                    NoOtherActions(_input.Current) && NoOtherActions(_input.RawFrame) &&
                    !_input.Current.ShieldHeld && !_shield.IsActive &&
                    _input.KeyboardAimDirection.Dot(Vector2.Left) > .999f &&
                    frame.CastId == 0 && frame.NextCastId == _cast && actualTarget == _target &&
                    ExactSelection(_claw.KeyboardPreview);
                if (!accepted) Fault("Aim was not the later actual one-edge Left K ACK");
                else { _aimAck = true; _aimSequence = _input.CurrentFrameSequence; }
            }
            else if (frame.State != MechanicalClawState.Idle)
            {
                if (frame.CastId != _cast || frame.NextCastId != _cast + 1)
                    Fault("cast/next-cast identity diverged");
                if (frame.State is MechanicalClawState.Travel or MechanicalClawState.Latched or MechanicalClawState.Released &&
                    frame.LockedTargetRuntimeId != _targetId) Fault("locked target identity diverged");
            }
            if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Released)
            {
                bool receipt = _aimAck && _adapter.ApplicationCount == _applicationsBefore + 1 &&
                    _input.CurrentFrameSequence > _issueSequence && NoOtherActions(_input.Current) &&
                    NoOtherActions(_input.RawFrame) && !_input.Current.ShieldHeld && !_shield.IsActive &&
                    _rawEdges == 1 && _acceptedEdges == 1;
                if (frame.State == MechanicalClawState.Latched)
                {
                    if (!receipt || actualTarget != _target) Fault("Latched target/application/input receipt invalid");
                    else _latched = true;
                    if (!ObserveRevocation()) // ApplyThreadPull must revoke synchronously.
                        Fault("Latched did not synchronously revoke authorized rear leaves");
                }
                else if (!receipt || !_latched || actualTarget != null) Fault("Released(null) receipt invalid");
                else _released = true;
            }
            if (frame.State == MechanicalClawState.Idle)
            {
                if (!_released || frame.NextCastId != _cast + 1 || actualTarget != null)
                    Fault("eventual Idle lacks same-cast release receipt");
                else _idle = true;
            }
            string publication = frame.State.ToString();
            if (publication != _lastPublication)
            {
                _lastPublication = publication;
                Row("claw-publication", $"state={frame.State} cast={frame.CastId} next={frame.NextCastId} " +
                    $"lock={frame.LockedTargetRuntimeId} target={(Live(actualTarget) ? actualTarget.GetInstanceId() : 0)} " +
                    $"application_delta={_adapter.ApplicationCount - _applicationsBefore} " +
                    $"raw_edges={_rawEdges} accepted_edges={_acceptedEdges}");
            }
        }
        catch (Exception error) { Fault($"claw observer {error.GetType().Name}"); }
    }

    private void OnRear(RangedWeaponFrame frame)
    {
        if (!_latched || Completed || _disposed || _stage == Stage.Rejected) return;
        try
        {
            if (!BoundAvailable()) { Fault("rear publication lost live ownership"); return; }
            if (frame.State != RangedWeaponState.Stunned) return;
            bool currentlyRevoked = ObserveRevocation();
            if (frame.AttackTimeline.Phase != AttackPhase.None ||
                !StunnedRevocationAccepted(_revoked, currentlyRevoked))
                Fault("Stunned still has attack phase/authority/authorized leaves");
            else if (!_stunned) { _stunned = true; Row("rear-Stunned-None", "authority=false authorized_leaves=0"); }
        }
        catch (Exception error) { Fault($"rear observer {error.GetType().Name}"); }
    }

    private bool ObserveRevocation()
    {
        _leaves.Clear();
        try
        {
            if (!Live(_rear)) { Fault("rear unavailable at revocation"); return false; }
            _rear.AppendAuthorizedAttackLeaves(_leaves);
            bool revoked = !_rear.HasPresentationAuthority && _leaves.Count == 0;
            if (revoked && !_revoked) Row("rear-authority-revoked", "authorized_leaves=0 private_beam_identity_claim=false");
            _revoked |= revoked;
            return revoked; // A historical receipt never substitutes for this query.
        }
        finally { _leaves.Clear(); }
    }

    private void DirectionsOff()
    {
        _driver.Command(InputActions.MoveLeft, false); _driver.Command(InputActions.MoveRight, false);
        _driver.Command(InputActions.MoveUp, false); _driver.Command(InputActions.MoveDown, false);
    }

    private bool Reject(string reason)
    {
        Row("preparation-rejected", $"K_issued=false reason={reason}");
        _stage = Stage.Rejected;
        ClearOwnedKeys();
        return false;
    }

    private void Fault(string message)
    {
        _faults++;
        if (_fault == "none") _fault = message;
        Row("fault", $"detail={message}");
    }

    private void Row(string kind, string detail)
    {
        try
        {
            if (_rows.Count >= 64) { _omitted++; return; }
            _rows.Add($"COURTYARD_REAR_K kind={kind} stage={_stage} physics={Engine.GetPhysicsFrames()} " +
                $"sequence={(Live(_input) ? _input.CurrentFrameSequence : 0)} loop={_loop}/900 {detail}");
        }
        catch (Exception) { _faults++; if (_fault == "none") _fault = "scalar row recording failed"; }
    }

    public void WriteSummary()
    {
        foreach (string row in _rows) GD.Print(row);
        GD.Print($"COURTYARD_REAR_K_END started={Started} completed={Completed} stage={_stage} " +
            $"rows={_rows.Count}/64 omitted={_omitted} faults={_faults} fault={_fault} " +
            $"raw_K={_rawEdges} accepted_K={_acceptedEdges} Aim={_aimAck} " +
            $"Latched={_latched} Released={_released} Stunned={_stunned} revoked={_revoked} " +
            $"right={_rightAck} guard_demanded={_guardDemanded} guard_ACK={_guardAck} Idle={_idle} " +
            "extra_waits=0 extra_resolver_queries=0 outer_loop_only=true human_acceptance=false full_goal_complete=false");
    }

    private void ClearOwnedKeys()
    {
        if (_driver == null || KeyboardRouteInputDriver.Current != _driver || !Live(_player)) return;
        if (_ownL) _driver.PointerButton(MouseButton.Right, false, _player, _player.GlobalPosition);
        if (_ownA) _driver.Command(InputActions.MoveLeft, false);
        if (_ownD) _driver.Command(InputActions.MoveRight, false);
        if (_ownK) _driver.Command(InputActions.Claw, false);
        _ownA = _ownD = _ownK = _ownL = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_subscribed)
            {
                if (GodotObject.IsInstanceValid(_claw)) _claw.ClawFrameChanged -= OnClaw;
                if (GodotObject.IsInstanceValid(_rear)) _rear.FrameChanged -= OnRear;
            }
        }
        finally
        {
            _subscribed = false; _leaves.Clear();
            if (Started && !Completed && _stage != Stage.Rejected) ClearOwnedKeys();
        }
    }
}
