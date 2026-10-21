using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Mechanisms;
using Knight.Abilities;
using Knight.Shared;
using Knight.Presentation;

namespace Knight.Player;

public enum MobilityEquipRejection { None, NotReady, InvalidModule, Burning, ControlsBlocked, Dead, ClawActive, PendingAction, NotStable }

public partial class PlayerMotor : Node
{
    [Export] public PlayerTuning Tuning { get; set; } = new();
    [Export] public MobilityModuleDefinition InitialMobilityModule { get; set; }

    private PlayerMotionModel _model = null!;
    private PlayerFallDamageModel _fallDamage = null!;
    private LandingImpactSampler _landingSampler;
    private KinematicCollision2D _equipSupportProbe;
    private ulong _lastCompletedSupportFrame;
    private int _stableSupportTicks;
    private float _landingSafeImpactSpeed;
    private float _landingReferenceImpactSpeed;
    private readonly PlayerMotionState _state = new();
    private CharacterBody2D _body = null!;
    private PlayerInputReader _input = null!;
    private HealthComponent _health;
    private float _abilityMovementScale = 1f;
    private float _shieldMovementScale = 1f;
    private float _grappleMovementScale = 1f;
    private Vector2 _grappleVelocityCorrection = Vector2.Zero;
    private bool _realGrappleAttached;
    private bool _automaticGrapplePull;
    private Vector2 _swingRopeAnchor;
    private float _swingRopeLength;
    private bool _suppressJumpForCurrentPhysicsFrame;
    private ThrusterPulseModel _pulse;
    private ThrusterPresentationModel _thrusterPresentation;
    private ulong _lastPresentationPhysicsFrame;
    private ulong _lastHandledThrusterSample;
    private MechanicalClawController _claw;
    private ulong _motionRevision;
    internal ulong MotionAuthorityRevision => _motionRevision;

    public bool IsDashing => _pulse?.IsBurning == true;
    // The actual Ready snapshot, not the mutable author Resource or a default.
    public PlayerMotionSettings MotionSettings => _model?.Settings ??
        throw new System.InvalidOperationException("Player motion is not initialized");
    public bool AirDashAvailable => _pulse?.AirUseAvailable ?? true;
    public bool HasGrappleReleaseMomentum => _state.PreservingGrappleReleaseMomentum;
    public int AcceptedJumpCount { get; private set; }
    public int AcceptedDashCount { get; private set; }
    public int RejectedDashCount { get; private set; }
    public ulong LandingCount { get; private set; }
    public float LastFallHeight { get; private set; }
    public int LastFallDamage { get; private set; }
    public bool LastFallDamageApplied { get; private set; }
    public LandingImpactSample LastLandingImpact { get; private set; } =
        LandingImpactSample.Unavailable(LandingImpactFallbackReason.Missing);
    public Vector2 UpdraftVelocityDelta { get; private set; }
    public Vector2 UpdraftCandidateVelocity { get; private set; }
    public ThrusterPulseSnapshot ThrusterState => _pulse?.Snapshot ?? default;
    public ThrusterPresentationFrame ThrusterPresentation => _thrusterPresentation?.Frame ?? default;
    public ulong ThrusterPresentationSequence => _thrusterPresentation?.LatestSequence ?? 0UL;
    public ulong ThrusterPresentationFirstSequence => _thrusterPresentation?.FirstAvailableSequence ?? 1UL;
    public System.Collections.Generic.IReadOnlyList<ThrusterPresentationTransition> ReadThrusterTransitionsAfter(ulong sequence) =>
        _thrusterPresentation?.ReadTransitionsAfter(sequence) ?? System.Array.Empty<ThrusterPresentationTransition>();

    // Future inventory/UI callers request a value transaction. Completed world
    // support and pending input remain Motor-owned, never caller assertions.
    public bool TryEquipMobilityModule(MobilityModuleDefinition definition,
        out MobilityEquipRejection rejection)
    {
        rejection = MobilityEquipRejection.NotReady;
        if (_pulse == null || !IsInsideTree() || !MotionOwnerAvailable() ||
            !GodotObject.IsInstanceValid(_input) || !_input.IsInsideTree() || _input.IsQueuedForDeletion()) return false;
        rejection = MobilityEquipRejection.ControlsBlocked;
        if (GetTree().Paused || !CanProcess() || !IsPhysicsProcessing() ||
            !_input.CanProcess() || !_input.IsPhysicsProcessing())
        {
            ClearEquipSupport();
            return false;
        }
        rejection = MobilityEquipRejection.Dead;
        if (!GodotObject.IsInstanceValid(_health) || !_health.IsInsideTree() ||
            _health.IsQueuedForDeletion() || _health.CurrentHealth <= 0) return false;
        rejection = MobilityEquipRejection.Burning;
        if (_pulse.IsBurning) return false;
        rejection = MobilityEquipRejection.ClawActive;
        if (GodotObject.IsInstanceValid(_claw) &&
            (_claw.CurrentFrame.State is not (MechanicalClawState.Idle or MechanicalClawState.Failed) ||
             _claw.CurrentTarget != null || _claw.ResidencyTarget != null)) return false;
        rejection = MobilityEquipRejection.PendingAction;
        if (_input.RawFrame.JumpPressed || _input.RawFrame.DashPressed || _state.JumpBufferRemaining > 0f ||
            Input.IsActionJustPressed(InputActions.Jump) || Input.IsActionJustPressed(InputActions.Dash)) return false;
        ulong physicsFrame = Engine.GetPhysicsFrames();
        rejection = MobilityEquipRejection.NotStable;
        if (_stableSupportTicks < 2 || physicsFrame < _lastCompletedSupportFrame ||
            physicsFrame - _lastCompletedSupportFrame > 1UL || !TryObserveStableSupport()) return false;
        if (!GodotObject.IsInstanceValid(definition))
        {
            rejection = MobilityEquipRejection.InvalidModule;
            return false;
        }
        MobilityModuleSettings settings = definition.ToSettings();
        if (MobilityModuleDefinition.Validate(settings).Count != 0)
        {
            rejection = MobilityEquipRejection.InvalidModule;
            return false;
        }
        ulong nextMotionRevision = checked(_motionRevision + 1UL);
        if (!_pulse.TryReplaceIdleModule(settings))
        {
            rejection = MobilityEquipRejection.Burning;
            return false;
        }
        _motionRevision = nextMotionRevision;
        _thrusterPresentation.Synchronize(_pulse.Snapshot);
        _thrusterPresentation.Step(0f, false, ThrusterCoastExit.ConfigurationChange);
        rejection = MobilityEquipRejection.None;
        return true;
    }

    public float AbilityMovementScale
    {
        get => _abilityMovementScale;
        set => _abilityMovementScale = Mathf.Clamp(value, 0f, 1f);
    }

    public float ShieldMovementScale
    {
        get => _shieldMovementScale;
        set => _shieldMovementScale = Mathf.Clamp(value, 0f, 1f);
    }

    public float GrappleMovementScale => _grappleMovementScale;
    public float EffectiveMovementScale =>
        _abilityMovementScale * _shieldMovementScale * _grappleMovementScale;

    // Owned native query objects belong to tree residency, not one-time Ready.
    // Reparenting the same Player preserves motion but re-enters this lifecycle.
    public override void _EnterTree()
    {
        _landingSampler ??= new LandingImpactSampler();
        _equipSupportProbe ??= new KinematicCollision2D();
        ClearEquipSupport();
        // Ready runs once; residency may repeat without RequestReady.
        if (GodotObject.IsInstanceValid(_health))
        {
            _health.Died -= OnDied;
            _health.Died += OnDied;
        }
    }

    public override void _Ready()
    {
        Tuning ??= new PlayerTuning();
        System.Collections.Generic.IReadOnlyList<string> errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new System.InvalidOperationException("Invalid PlayerTuning: " + string.Join("; ", errors));
        string motorLabel = IsInsideTree() ? GetPath().ToString() : $"detached {Name} ({GetType().Name})";
        string tuningSource = string.IsNullOrEmpty(Tuning.ResourcePath) ? motorLabel + ":Tuning" : Tuning.ResourcePath;
        System.Collections.Generic.IReadOnlyList<string> migrationErrors = Tuning.ValidateThrusterMigration(tuningSource);
        if (migrationErrors.Count > 0)
            throw new System.InvalidOperationException("Unmapped legacy mobility authoring: " + string.Join("; ", migrationErrors));
        PlayerMotionSettings motionSettings = Tuning.ToMotionSettings();
        _model = new PlayerMotionModel(motionSettings);
        _landingSafeImpactSpeed = Tuning.FallSafeImpactSpeed;
        _landingReferenceImpactSpeed = motionSettings.MaxFallSpeed;
        _fallDamage = new PlayerFallDamageModel(Tuning.FallSafeHeight, Tuning.FallHeightPerDamage);
        _body = GetParent<CharacterBody2D>();
        _input = GetNode<PlayerInputReader>("../Input");
        _health = GetNodeOrNull<HealthComponent>("../Health");
        _claw = GetNodeOrNull<MechanicalClawController>("../MechanicalClaw");
        _state.Velocity = _body.Velocity;
        // Legacy saved Players without an authored module retain Standard.
        // A present Resource is validated and frozen, never silently replaced.
        MobilityModuleSettings initialModule = InitialMobilityModule == null
            ? MobilityModuleSettings.Standard : InitialMobilityModule.ToSettings();
        System.Collections.Generic.IReadOnlyList<string> moduleErrors = MobilityModuleDefinition.Validate(initialModule);
        if (moduleErrors.Count > 0)
            throw new System.InvalidOperationException("Invalid InitialMobilityModule at " + motorLabel + ": " + string.Join("; ", moduleErrors));
        _pulse = new ThrusterPulseModel(initialModule, GetInstanceId());
        _thrusterPresentation = new ThrusterPresentationModel(GetInstanceId());
        if (_health != null) _health.Died += OnDied;
        ResetFallTracking();
    }

    public override void _ExitTree()
    {
        _motionRevision = checked(_motionRevision + 1UL);
        if (GodotObject.IsInstanceValid(_health)) _health.Died -= OnDied;
        if (_pulse != null) _thrusterPresentation?.End(_pulse.Interrupt(ThrusterEndReason.OwnerRetired));
        _thrusterPresentation?.Step(0f, false, ThrusterCoastExit.Reset);
        LandingImpactSampler retiredSampler = _landingSampler;
        _landingSampler = null;
        KinematicCollision2D retiredSupportProbe = _equipSupportProbe;
        _equipSupportProbe = null;
        ClearEquipSupport();
        retiredSampler?.Dispose();
        retiredSupportProbe?.Dispose();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationUnpaused) ClearEquipSupport();
    }

    private void OnDied() => ResetTransientState(ThrusterEndReason.Death);

    public override void _PhysicsProcess(double delta)
    {
        if (!MotionOwnerAvailable() || GetTree().Paused || _health?.CurrentHealth <= 0) return;
        ulong motionRevision = _motionRevision;
        try
        {
            PlayerInputFrame sampled = _input.CurrentFrame;
            if (_suppressJumpForCurrentPhysicsFrame)
                sampled = sampled with { JumpPressed = false, JumpHeld = false };
            PlayerInputFrame input = sampled with
            {
                MoveAxis = sampled.MoveAxis * EffectiveMovementScale
            };
            bool wasOnFloor = _body.IsOnFloor();
            ulong landingBefore = LandingCount;
            float beforeWorldY = _body.GlobalPosition.Y;
            _fallDamage.SampleHeight(beforeWorldY);
            bool hadActiveDash = IsDashing;
            bool startedDash = false;
            if (sampled.DashPressed && _input.CurrentFrameSequence > _lastHandledThrusterSample)
            {
                _lastHandledThrusterSample = _input.CurrentFrameSequence;
                var decision = _pulse.Preview(new(_input.CurrentFrameSequence, sampled.DashPressed,
                    _input.RawFrame.JumpPressed, wasOnFloor, _health == null || _health.CurrentHealth > 0,
                    GetTree().Paused, _input.RawFrame.ShieldHeld, _input.KeyboardAimDirection,
                    _pulse.Lifecycle, _pulse.ConfigurationEpoch));
                Vector2 inheritedVelocity = _body.Velocity;
                bool inheritedGrappleRise = _state.GrappleRiseProtected ||
                    _realGrappleAttached && inheritedVelocity.Y < 0f;
                ulong clawAuthority = _claw?.AuthorityRevision ?? 0UL;
                bool detached = decision.CanStart && (_claw == null || _claw.TryDetachForThruster(out clawAuthority));
                // A callback has transferred/reset motion authority. Never use
                // this tick's frozen velocity or integrate over its replacement.
                if (_motionRevision != motionRevision || !MotionOwnerAvailable() ||
                    GetTree().Paused || _health?.CurrentHealth <= 0) return;
                bool stillCurrent = _input.CurrentFrameSequence == _lastHandledThrusterSample &&
                    _input.CurrentFrame == sampled && !GetTree().Paused &&
                    (_claw == null || _claw.AuthorityRevision == clawAuthority) &&
                    (_health == null || _health.CurrentHealth > 0);
                ThrusterPulseCommitResult commit = detached && stillCurrent
                    ? _pulse.TryCommit(decision) : new(false, decision.CanStart
                        ? ThrusterRejectReason.StaleDecision : decision.Reason, default);
                if (commit.Accepted)
                {
                    _state.Velocity = inheritedVelocity;
                    _state.GrappleRiseProtected = inheritedGrappleRise;
                    _state.PreservingGrappleReleaseMomentum = false;
                    startedDash = true;
                    AcceptedDashCount++;
                    _thrusterPresentation.Start(commit, _pulse.BurnDirection);
                }
                else RejectedDashCount++;
            }
            ThrusterIdentity burnIdentity = _pulse.ActiveIdentity;
            PlayerMotionResult result = _model.StepFree(_state, input, wasOnFloor, delta,
                _pulse.Module, _pulse.BurnDirection, _pulse.BurnRemaining, _automaticGrapplePull,
                _suppressJumpForCurrentPhysicsFrame || sampled.DashPressed);
            _thrusterPresentation.RecordThrust(burnIdentity, result.ThrustVelocityDelta);
            _thrusterPresentation.End(_pulse.ApplyDynamics(burnIdentity, result.BurnRemaining, result.CancelledForReverse));
            if (result.ThrustVelocityDelta.Y < 0f && result.Velocity.Y < 0f)
                _state.ThrusterRiseProtected = true;
            bool dashDriven = hadActiveDash || startedDash;
            if (result.StartedJump) AcceptedJumpCount++;
            // During automatic approach the claw owns acceleration in both axes.
            // Still advance jump/dash lifecycle, but do not add a second gravity/run
            // update after its bounded servo and collision prediction. Swing and
            // release immediately return velocity ownership to ordinary motion.
            Vector2 freeVelocity = result.Velocity;
            if (_realGrappleAttached && !_automaticGrapplePull && !dashDriven && _swingRopeLength > 0f)
                freeVelocity += PlayerMotionModel.TautRopeGravitySupport(result.GravityVelocityDelta,
                    _body.GlobalPosition - _swingRopeAnchor, _swingRopeLength);
            Vector2 appliedGrappleCorrection = freeVelocity + _grappleVelocityCorrection - result.Velocity;
            Vector2 beforeWind = freeVelocity;
            UpdraftCandidateVelocity = beforeWind;
            if (!wasOnFloor && !result.StartedJump && !_realGrappleAttached && !HasGrappleReleaseMomentum)
                freeVelocity = CanyonUpdraft.ApplyAtPlayer(_body, freeVelocity, delta);
            UpdraftVelocityDelta = freeVelocity - beforeWind;
            Vector2 commandedVelocity = freeVelocity + _grappleVelocityCorrection;
            LandingImpactPreparation impactBefore = _landingSampler.Prepare(_body,
                commandedVelocity, wasOnFloor, delta);
            _body.Velocity = commandedVelocity;
            _body.MoveAndSlide();
            float afterWorldY = _body.GlobalPosition.Y;
            _fallDamage.SampleHeight(afterWorldY);
            Vector2 collisionAdjustment = _body.Velocity - commandedVelocity;
            // Attached corrections are absolute components of the commanded velocity, not
            // per-frame impulses. Keep the unconstrained motion state separate so the same
            // correction can be presented for many physics frames without accumulating.
            _state.Velocity = freeVelocity + collisionAdjustment;
            if (_body.IsOnCeiling() || _body.IsOnFloor() || _body.Velocity.Y >= 0f)
            {
                _state.GrappleRiseProtected = false;
                _state.ThrusterRiseProtected = false;
            }
            if (_body.IsOnFloor()) _state.PreservingGrappleReleaseMomentum = false;
            if (_body.IsOnFloor() && TryGetFloorContact(out bool softLanding))
            {
                if (!wasOnFloor)
                {
                    LandingFloorSupport support = _landingSampler.InspectFloorSupport(_body);
                    ConsumeFallLanding(afterWorldY, softLanding && support.AllSoft,
                        _landingSampler.Complete(_body, impactBefore, support));
                }
                else _fallDamage.ResetOrigin(afterWorldY);
            }
            else
            {
                // A genuine claw ascent starts a new descent; an upward dash does not.
                if (_realGrappleAttached && !dashDriven && appliedGrappleCorrection.Y < 0f &&
                    commandedVelocity.Y < 0f && afterWorldY < beforeWorldY)
                    _fallDamage.ResetOrigin(afterWorldY);
                else
                    _fallDamage.ObserveGrappleBraking(afterWorldY, result.Velocity.Y,
                        commandedVelocity.Y, appliedGrappleCorrection.Y, _realGrappleAttached,
                        dashDriven, delta);
            }
            if (_motionRevision == motionRevision && MotionOwnerAvailable() && !GetTree().Paused &&
                _health?.CurrentHealth > 0)
            {
                RecordCompletedSupportStep();
                // Synchronous value-only observation, after all reentrant damage
                // callbacks. Version gate above and frame gate here reject stale
                // or repeated completion; no deferred old Step can reach the model.
                ulong frame = Engine.GetPhysicsFrames();
                if (frame > _lastPresentationPhysicsFrame)
                {
                    ThrusterCoastExit exit = result.StartedJump ? ThrusterCoastExit.Jump : ThrusterCoastExit.None;
                    if (LandingCount != landingBefore) exit |= ThrusterCoastExit.Landing;
                    if (result.BrakeReachedZero) exit |= ThrusterCoastExit.BrakedToZero;
                    if (AdvancedAxisBlocked(commandedVelocity)) exit |= ThrusterCoastExit.CollisionBlocked;
                    _thrusterPresentation.Step((float)delta, result.Braking, exit);
                    _lastPresentationPhysicsFrame = frame;
                }
            }
        }
        finally
        {
            _suppressJumpForCurrentPhysicsFrame = false;
        }
    }

    public void SuppressJumpForCurrentPhysicsFrame() =>
        _suppressJumpForCurrentPhysicsFrame = true;

    public void ApplyGrappleConstraint(Vector2 velocityCorrection, float movementScale,
        bool realAttached = false, bool automaticPull = false,
        Vector2 swingAnchor = default, float swingRopeLength = 0f)
    {
        // Paired with the Reel-to-Swing correction reset, and every real attachment
        // exit: continue with actual motion, not a free base the rope was cancelling.
        if ((_automaticGrapplePull && !(realAttached && automaticPull)) ||
            (_realGrappleAttached && !realAttached))
            _state.Velocity = _body.Velocity;
        _grappleVelocityCorrection = velocityCorrection;
        _grappleMovementScale = Mathf.Clamp(movementScale, 0f, 1f);
        _realGrappleAttached = realAttached;
        if (realAttached)
        {
            if (_pulse != null) _thrusterPresentation?.End(_pulse.Interrupt(ThrusterEndReason.ClawTakeover));
            _state.GrappleRiseProtected = false;
            _state.ThrusterRiseProtected = false;
        }
        _automaticGrapplePull = realAttached && automaticPull;
        _swingRopeAnchor = swingAnchor;
        _swingRopeLength = realAttached && !automaticPull ? swingRopeLength : 0f;
        if (realAttached) _thrusterPresentation?.Step(0f, false, ThrusterCoastExit.ClawAttachment);
    }

    public void ApplyGrappleRelease(Vector2 outwardNormal, Vector2 boostDelta, float speedLimit,
        bool preserveLaunchMomentum)
    {
        Vector2 normal = outwardNormal.Normalized();
        Vector2 released = _body.Velocity + boostDelta;
        float inward = released.Dot(normal);
        if (inward < 0f) released -= normal * inward;
        if (released.Length() > speedLimit) released = released.Normalized() * speedLimit;
        _state.Velocity = released;
        _body.Velocity = released;
        _state.PreservingGrappleReleaseMomentum = preserveLaunchMomentum;
        _state.GrappleRiseProtected = preserveLaunchMomentum && released.Y < 0f;
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
        _realGrappleAttached = false;
        _automaticGrapplePull = false;
        _swingRopeLength = 0f;
    }

    public void ClearGrappleConstraint()
    {
        // Target loss/unload and callback-driven cleanup are also ownership exits.
        // Explicit respawn ResetTransientState still resets all motion to zero.
        if (_realGrappleAttached) _state.Velocity = _body.Velocity;
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
        _realGrappleAttached = false;
        _automaticGrapplePull = false;
        _swingRopeLength = 0f;
    }

    internal void DetachGrappleWithoutRelease()
    {
        // Motor freezes actual incoming velocity once, before any callbacks.
        // Dedicated cleanup must not perform a second ownership inheritance.
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
        _realGrappleAttached = false;
        _automaticGrapplePull = false;
        _swingRopeLength = 0f;
    }

    public void ExternalImpulse(Vector2 velocity)
    {
        _motionRevision = checked(_motionRevision + 1UL);
        ClearEquipSupport();
        _state.PreservingGrappleReleaseMomentum = false;
        _state.GrappleRiseProtected = false;
        _state.ThrusterRiseProtected = false;
        if (_pulse != null) _thrusterPresentation?.End(_pulse.Interrupt(ThrusterEndReason.ExternalReplacement));
        _state.Velocity = velocity;
        _state.DashRemaining = 0f;
        _body.Velocity = velocity;
        _thrusterPresentation?.Step(0f, false, ThrusterCoastExit.ExternalReplacement);
    }

    public void ResetTransientState() => ResetTransientState(ThrusterEndReason.Reset);

    private void ResetTransientState(ThrusterEndReason reason)
    {
        _motionRevision = checked(_motionRevision + 1UL);
        ClearEquipSupport();
        _state.Reset();
        if (_pulse != null)
        {
            // Capture the old identity's actual end before synchronizing context.
            _thrusterPresentation?.End(_pulse.ResetLifecycle(reason));
            _thrusterPresentation?.Synchronize(_pulse.Snapshot);
        }
        AcceptedJumpCount = 0;
        AcceptedDashCount = 0;
        RejectedDashCount = 0;
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
        _realGrappleAttached = false;
        _automaticGrapplePull = false;
        _swingRopeLength = 0f;
        _suppressJumpForCurrentPhysicsFrame = false;
        UpdraftVelocityDelta = Vector2.Zero;
        UpdraftCandidateVelocity = Vector2.Zero;
        _body.Velocity = Vector2.Zero;
        ResetFallTracking();
        _thrusterPresentation?.Step(0f, false,
            reason == ThrusterEndReason.Death ? ThrusterCoastExit.Death : ThrusterCoastExit.Reset);
    }

    public void ResetFallTracking() => _fallDamage?.ResetOrigin(_body.GlobalPosition.Y);

    private void ClearEquipSupport()
    {
        _lastCompletedSupportFrame = 0UL;
        _stableSupportTicks = 0;
    }

    private void RecordCompletedSupportStep()
    {
        ulong frame = Engine.GetPhysicsFrames();
        if (frame == _lastCompletedSupportFrame) return;
        bool stable = TryObserveStableSupport();
        _stableSupportTicks = stable
            ? (_lastCompletedSupportFrame < frame && frame - _lastCompletedSupportFrame == 1UL
                ? System.Math.Min(2, _stableSupportTicks + 1) : 1) : 0;
        _lastCompletedSupportFrame = frame;
    }

    private bool TryObserveStableSupport()
    {
        if (!_body.IsOnFloor() || _equipSupportProbe == null || !Finite(_body.UpDirection) ||
            _body.UpDirection.LengthSquared() <= 0f || !float.IsFinite(_body.FloorMaxAngle) ||
            !float.IsFinite(_body.SafeMargin) || _body.SafeMargin < 0f ||
            !Finite(_body.GlobalTransform.Origin) || !Finite(_body.GlobalTransform.X) ||
            !Finite(_body.GlobalTransform.Y)) return false;
        Vector2 realVelocity = _body.GetRealVelocity();
        if (!Finite(realVelocity) || !_body.TestMove(_body.GlobalTransform,
            -_body.UpDirection.Normalized() * .001f, _equipSupportProbe, _body.SafeMargin, true) ||
            !StableFloorContact(_equipSupportProbe, realVelocity)) return false;
        // The queried support handles zero-motion/snap frames with no slide entry;
        // actual simultaneous floor contacts must agree on tangential stability.
        for (int index = 0; index < _body.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D contact = _body.GetSlideCollision(index); // Borrowed.
            Vector2 normal = contact.GetNormal();
            if (Finite(normal) && normal.Dot(_body.UpDirection.Normalized()) >= Mathf.Cos(_body.FloorMaxAngle) &&
                !StableFloorContact(contact, realVelocity)) return false;
        }
        return true;
    }

    private bool StableFloorContact(KinematicCollision2D contact, Vector2 realVelocity)
    {
        // QueueFree revokes future support before its native body is removed.
        // Never install against a floor or streamed ancestor already retired.
        if (contact.GetCollider() is not Node collider || !collider.IsInsideTree()) return false;
        for (Node current = collider; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        Vector2 normal = contact.GetNormal(), point = contact.GetPosition();
        if (!Finite(normal) || !Finite(point) || System.Math.Abs(normal.LengthSquared() - 1f) >= .001f ||
            normal.Dot(_body.UpDirection.Normalized()) < Mathf.Cos(_body.FloorMaxAngle)) return false;
        PhysicsDirectBodyState2D support = PhysicsServer2D.BodyGetDirectState(contact.GetColliderRid());
        if (support == null || !Finite(support.Transform.Origin)) return false;
        // Despite the API name, this offset is expressed in world coordinates.
        Vector2 pointVelocity = support.GetVelocityAtLocalPosition(point - support.Transform.Origin);
        return Finite(pointVelocity) &&
            System.Math.Abs((realVelocity - pointVelocity).Dot(normal.Orthogonal())) < 1f;
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    private bool AdvancedAxisBlocked(Vector2 commanded)
    {
        ThrusterPresentationFrame observation = ThrusterPresentation;
        if (observation.Identity == default || _body.GetSlideCollisionCount() == 0 ||
            _body.GetPlatformVelocity() != Vector2.Zero) return false;
        Vector2 direction = observation.Direction;
        // One static axis surface can produce multiple native slide records.
        // Require the same collider and collinear opposing normals throughout.
        // Slopes, diagonals, carry and mixed surfaces remain Unknown; no
        // accumulating source-velocity ledger or dot-product guess is introduced.
        bool horizontal = direction.Y == 0f && Mathf.Abs(direction.X) == 1f;
        bool vertical = direction.X == 0f && Mathf.Abs(direction.Y) == 1f;
        if (!horizontal && !vertical) return false;
        var surface = _body.GetSlideCollision(0).GetColliderRid();
        bool sweptTowardSurface = false;
        for (int index = 0; index < _body.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D contact = _body.GetSlideCollision(index); // Borrowed.
            Vector2 normal = contact.GetNormal(), travel = contact.GetTravel();
            if (contact.GetCollider() is not StaticBody2D || contact.GetColliderRid() != surface ||
                contact.GetColliderVelocity() != Vector2.Zero || !Finite(normal) || !Finite(travel) ||
                Mathf.Abs(normal.LengthSquared() - 1f) > .001f || normal.Dot(direction) > -.999f ||
                !float.IsFinite(contact.GetDepth()) || contact.GetDepth() < 0f) return false;
            sweptTowardSurface |= travel.Dot(direction) > .001f;
        }
        // A real inward sweep distinguishes this from pure recovery. Penetration
        // depth includes native margins and is not a reliable sweep classifier.
        if (!sweptTowardSurface) return false;
        float before = horizontal ? commanded.X : commanded.Y;
        float after = horizontal ? _body.Velocity.X : _body.Velocity.Y;
        float sign = horizontal ? direction.X : direction.Y;
        return before * sign > .001f && Mathf.Abs(after) <= .001f;
    }

    private bool MotionOwnerAvailable()
    {
        if (IsQueuedForDeletion() || !GodotObject.IsInstanceValid(_body) || !_body.IsInsideTree()) return false;
        // QueueFree revokes the whole subtree before ExitTree runs. Checking only
        // the Player misses a scene/sector ancestor queued by a cleanup callback.
        for (Node current = _body; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }

    private void ConsumeFallLanding(float worldY, bool softLanding, LandingImpactSample impact)
    {
        PlayerFallLanding landing = _fallDamage.ConsumeLanding(worldY, softLanding,
            _health?.MaxHealth ?? int.MaxValue, impact, _landingSafeImpactSpeed, _landingReferenceImpactSpeed);
        LandingCount++;
        _pulse.ObserveLanding(_pulse.Lifecycle, LandingCount);
        LastFallHeight = landing.Height;
        LastFallDamage = landing.Damage;
        LastLandingImpact = impact;
        LastFallDamageApplied = landing.Damage > 0 && _health != null &&
            _health.ReceiveDamage(new DamageRequest(landing.Damage, Vector2.Zero, 0f,
                LandingCount, GetInstanceId()));
    }

    private bool TryGetFloorContact(out bool softLanding)
    {
        bool found = false;
        softLanding = true;
        Vector2 up = _body.UpDirection.Normalized();
        float minimumFloorDot = Mathf.Cos(_body.FloorMaxAngle);
        for (int index = 0; index < _body.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = _body.GetSlideCollision(index);
            Vector2 normal = collision.GetNormal().Normalized();
            if (normal.Dot(Vector2.Up) <= 0f || normal.Dot(up) < minimumFloorDot) continue;
            found = true;
            GodotObject collider = collision.GetCollider();
            bool explicitSoftLanding = collider != null && GodotObject.IsInstanceValid(collider) &&
                collider.HasMeta("soft_landing") &&
                collider.GetMeta("soft_landing").VariantType == Variant.Type.Bool &&
                collider.GetMeta("soft_landing").AsBool();
            // An incidental soft-pad contact cannot exempt ordinary simultaneous support.
            softLanding &= explicitSoftLanding;
        }
        return found;
    }
}
