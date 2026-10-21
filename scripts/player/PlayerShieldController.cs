using System;
using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Player;

public partial class PlayerShieldController : Node
{
    [Export] public NodePath InputPath { get; set; } = new("../Input");
    [Export] public NodePath MotorPath { get; set; } = new("../Motor");
    [Export] public NodePath EnergyPath { get; set; } = new("../Energy");
    [Export] public NodePath HealthPath { get; set; } = new("../Health");
    [Export] public NodePath VisualPath { get; set; } = new("../VisualRoot/Shield");

    private readonly PlayerShieldModel _model = new(PlayerShieldSettings.Approved);
    private PlayerInputReader _input = null!;
    private PlayerMotor _motor = null!;
    private PlayerEnergyController _energy = null!;
    private HealthComponent _health = null!;
    private PlayerShieldPlaceholderVisual _visual = null!;
    private CharacterBody2D _body = null!;
    private Vector2 _direction = Vector2.Right;
    private bool _guardBroken;
    private ulong _resultTransitionId;

    public bool IsActive { get; private set; }
    public bool CanBeginGuard => !_guardBroken && _health?.CurrentHealth > 0 && _energy != null &&
        !_energy.IsOverheated && _energy.EnergyRatio > 0f;
    public Vector2 GuardDirection => _direction;
    public event Action<bool> ShieldFrameChanged;
    public event Action<PlayerShieldResult, ulong> ShieldResolved;

    public override void _Ready()
    {
        ResolveDependencies();
        _energy.EnergyFrameChanged += OnEnergyFrameChanged;
        _health.Died += OnDied;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_energy)) _energy.EnergyFrameChanged -= OnEnergyFrameChanged;
        if (IsInstanceValid(_health)) _health.Died -= OnDied;
    }

    public override void _PhysicsProcess(double delta)
    {
        ResolveDependencies();
        bool requested = _input.Current.ShieldHeld;
        if (!requested) _guardBroken = false;
        _energy.SetShieldRequested(requested && !_guardBroken && _health.CurrentHealth > 0);
        UpdateDirection();
        Refresh();
    }

    public bool TryResolve(DamageRequest request, out DamageRequest resolved)
    {
        ResolveDependencies();
        resolved = request;
        // DamageRequest.Direction remains the attacker-to-victim knockback direction produced by Hitbox.
        // Directional guarding instead needs the vector from the player back toward the attack source.
        Vector2 sourceDirection = -request.Direction;
        PlayerShieldResult result = _model.Resolve(new PlayerShieldInput(
            IsActive, _direction, sourceDirection, request.GuardClass, CanAffordHeavyGuard()));
        if (result.Guarded || result.BrokeGuard)
            ShieldResolved?.Invoke(result, ++_resultTransitionId);
        if (result.BrokeGuard)
        {
            CloseGuard(true);
            return false;
        }
        if (!result.Guarded) return false;
        if (result.SpendHeavyEnergy && !_energy.TrySpendHeavyGuard(PlayerShieldSettings.Approved.HeavyEnergyCost))
        {
            CloseGuard(true);
            return false;
        }

        resolved = request with
        {
            Amount = result.DamageAmount,
            Knockback = request.Knockback * result.KnockbackScale
        };
        Refresh();
        return true;
    }

    public void ResetFull()
    {
        _guardBroken = false;
        CloseGuard(false);
    }

    private bool CanAffordHeavyGuard()
    {
        float currentEnergy = _energy.EnergyRatio * _energy.Tuning.ToSettings().MaxEnergy;
        return currentEnergy + .0001f >= PlayerShieldSettings.Approved.HeavyEnergyCost;
    }

    private void OnEnergyFrameChanged(PlayerEnergyFrame frame)
    {
        Refresh();
    }

    private void OnDied()
    {
        CloseGuard(true);
    }

    private void CloseGuard(bool brokeGuard)
    {
        if (brokeGuard) _guardBroken = true;
        _energy?.SetShieldRequested(false);
        ApplyActive(false);
    }

    private void Refresh()
    {
        bool requested = _input != null && _input.Current.ShieldHeld && !_guardBroken;
        bool active = requested && _health != null && _health.CurrentHealth > 0 && _energy != null &&
            _energy.IsShieldSustained && !_energy.IsOverheated && _energy.EnergyRatio > 0f;
        ApplyActive(active);
    }

    private void ApplyActive(bool active)
    {
        if (_motor != null) _motor.ShieldMovementScale =
            active ? PlayerShieldSettings.Approved.MovementScale : 1f;
        _visual?.Present(active, _direction);
        if (IsActive == active) return;
        IsActive = active;
        ShieldFrameChanged?.Invoke(IsActive);
    }

    private void UpdateDirection()
    {
        if (_input.Current.KeyboardActions)
        {
            _direction = _input.KeyboardAimDirection;
            return;
        }
        Vector2 aim = _input.Current.MouseWorld - _body.GlobalPosition;
        if (aim.LengthSquared() <= float.Epsilon || !float.IsFinite(aim.X) || !float.IsFinite(aim.Y)) return;
        _direction = aim.Normalized();
    }

    private void ResolveDependencies()
    {
        _body ??= GetParent<CharacterBody2D>();
        _input ??= GetNode<PlayerInputReader>(InputPath);
        _motor ??= GetNode<PlayerMotor>(MotorPath);
        _energy ??= GetNode<PlayerEnergyController>(EnergyPath);
        _health ??= GetNode<HealthComponent>(HealthPath);
        _visual ??= GetNode<PlayerShieldPlaceholderVisual>(VisualPath);
    }
}
