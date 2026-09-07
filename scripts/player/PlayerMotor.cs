using Godot;
using Knight.Config;

namespace Knight.Player;

public partial class PlayerMotor : Node
{
    [Export] public PlayerTuning Tuning { get; set; } = new();

    private PlayerMotionModel _model = null!;
    private readonly PlayerMotionState _state = new();
    private CharacterBody2D _body = null!;
    private PlayerInputReader _input = null!;
    private float _abilityMovementScale = 1f;
    private float _shieldMovementScale = 1f;
    private float _grappleMovementScale = 1f;
    private Vector2 _grappleVelocityCorrection = Vector2.Zero;
    private bool _suppressJumpForCurrentPhysicsFrame;

    public bool IsDashing => _model != null && _model.IsDashing(_state);
    public bool AirDashAvailable => _state.AirDashAvailable;
    public int AcceptedJumpCount { get; private set; }
    public int AcceptedDashCount { get; private set; }
    public int RejectedDashCount { get; private set; }

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

    public override void _Ready()
    {
        Tuning ??= new PlayerTuning();
        System.Collections.Generic.IReadOnlyList<string> errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new System.InvalidOperationException("Invalid PlayerTuning: " + string.Join("; ", errors));
        _model = new PlayerMotionModel(Tuning.ToMotionSettings());
        _body = GetParent<CharacterBody2D>();
        _input = GetNode<PlayerInputReader>("../Input");
        _state.Velocity = _body.Velocity;
    }

    public override void _PhysicsProcess(double delta)
    {
        try
        {
            PlayerInputFrame sampled = _input.CurrentFrame;
            if (_suppressJumpForCurrentPhysicsFrame)
                sampled = sampled with { JumpPressed = false, JumpHeld = false };
            PlayerInputFrame input = sampled with
            {
                MoveAxis = sampled.MoveAxis * EffectiveMovementScale
            };
            PlayerMotionResult result = _model.Step(_state, input, _body.IsOnFloor(), delta);
            if (result.StartedJump) AcceptedJumpCount++;
            if (result.StartedDash) AcceptedDashCount++;
            if (result.DashRejected) RejectedDashCount++;
            Vector2 commandedVelocity = result.Velocity + _grappleVelocityCorrection;
            _body.Velocity = commandedVelocity;
            _body.MoveAndSlide();
            Vector2 collisionAdjustment = _body.Velocity - commandedVelocity;
            // Attached corrections are absolute components of the commanded velocity, not
            // per-frame impulses. Keep the unconstrained motion state separate so the same
            // correction can be presented for many physics frames without accumulating.
            _state.Velocity = result.Velocity + collisionAdjustment;
        }
        finally
        {
            _suppressJumpForCurrentPhysicsFrame = false;
        }
    }

    public void SuppressJumpForCurrentPhysicsFrame() =>
        _suppressJumpForCurrentPhysicsFrame = true;

    public void ApplyGrappleConstraint(Vector2 velocityCorrection, float movementScale)
    {
        _grappleVelocityCorrection = velocityCorrection;
        _grappleMovementScale = Mathf.Clamp(movementScale, 0f, 1f);
    }

    public void ApplyGrappleRelease(Vector2 tangentialVelocity)
    {
        // The body's observed velocity already contains the Task 5 tangential component. Make
        // that observed velocity the new unconstrained base exactly once; applying the published
        // correction again here would double the release, while retaining a correction cancelled
        // by collision would launch the player from a hidden stored impulse.
        _state.Velocity = _body.Velocity;
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
    }

    public void ClearGrappleConstraint()
    {
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
    }

    public void ExternalImpulse(Vector2 velocity)
    {
        _state.Velocity = velocity;
        _state.DashRemaining = 0f;
        _body.Velocity = velocity;
    }

    public void ResetTransientState()
    {
        _state.Reset();
        AcceptedJumpCount = 0;
        AcceptedDashCount = 0;
        RejectedDashCount = 0;
        _grappleVelocityCorrection = Vector2.Zero;
        _grappleMovementScale = 1f;
        _suppressJumpForCurrentPhysicsFrame = false;
        _body.Velocity = Vector2.Zero;
    }
}
