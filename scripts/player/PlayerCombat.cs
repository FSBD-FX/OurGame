using Godot;
using Knight.Combat;
using Knight.Config;

namespace Knight.Player;

public partial class PlayerCombat : Node2D
{
    [Signal]
    public delegate void AttackStartedEventHandler(ulong attackId, int direction);

    [Signal]
    public delegate void AttackConnectedEventHandler(Hurtbox hurtbox);

    [Export] public PlayerTuning Tuning { get; set; } = new();

    private PlayerCombatSettings _settings = null!;
    private PlayerCombatModel _model = null!;
    private CharacterBody2D _body = null!;
    private PlayerInputReader _input = null!;
    private PlayerActionModeController _actionModes = null!;
    private PlayerEnergyController _energy = null!;
    private PlayerMotor _motor = null!;
    private Hitbox _hitbox = null!;
    private AttackDirection _direction;
    private int _facing = 1;
    private bool _hitboxActive;

    public bool IsAttacking => _model?.IsAttacking ?? false;
    public int LastAttackFacing { get; private set; } = 1;

    public override void _Ready()
    {
        Tuning ??= new PlayerTuning();
        System.Collections.Generic.IReadOnlyList<string> errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new System.InvalidOperationException("Invalid PlayerTuning: " + string.Join("; ", errors));
        _settings = Tuning.ToCombatSettings();
        _model = new PlayerCombatModel(_settings);
        _body = GetParent<CharacterBody2D>();
        _input = GetParent().GetNode<PlayerInputReader>("Input");
        _actionModes = GetParent().GetNode<PlayerActionModeController>("ActionModes");
        _energy = GetParent().GetNode<PlayerEnergyController>("Energy");
        _motor = GetParent().GetNode<PlayerMotor>("Motor");
        _hitbox = GetNode<Hitbox>("AttackHitbox");
        _hitbox.Damage = _settings.Damage;
        _hitbox.Knockback = _settings.Knockback;
        _hitbox.Connected += OnHitboxConnected;
    }

    public override void _PhysicsProcess(double delta)
    {
        PlayerInputFrame input = _input.Current;
        if (!Mathf.IsZeroApprox(input.MoveAxis)) _facing = input.MoveAxis < 0f ? -1 : 1;

        // Independent J uses the same combat/energy model; legacy primary is retained only
        // while historical route drivers are being migrated to the keyboard contract.
        if (input.PrimaryPressed || input.AttackPressed)
        {
            if (!_actionModes.CanBeginAttack)
            {
                _actionModes.RejectAttackRequest();
            }
            else if (_energy.TrySpendAttack())
            {
            AttackDirection chosen = PlayerCombatModel.ResolveDirection(input.Aim, !_body.IsOnFloor(), _facing,
                out int attackFacing, _settings.AimDeadZone);
            ulong? attackId = _model.TryStart(chosen);
            if (attackId.HasValue)
            {
                _actionModes.ConfirmAttackStarted();
                _direction = chosen;
                _facing = attackFacing;
                LastAttackFacing = attackFacing;
                _hitbox.Position = HitboxOffset(chosen, _facing);
                EmitSignal(SignalName.AttackStarted, attackId.Value, (int)chosen);
            }
            }
            else
            {
                _actionModes.RejectAttackRequest();
            }
        }

        AttackFrame frame = _model.Tick(delta);
        if (frame.IsActive && !_hitboxActive)
        {
            _hitbox.Begin(frame.AttackId);
            _hitboxActive = true;
        }
        else if (!frame.IsActive && _hitboxActive)
        {
            _hitbox.End();
            _hitboxActive = false;
        }
    }

    private void OnHitboxConnected(Hurtbox hurtbox)
    {
        if (_direction == AttackDirection.Down)
        {
            _motor.ExternalImpulse(new Vector2(_body.Velocity.X, -_settings.DownBounceSpeed));
            _motor.ResetFallTracking();
        }
        EmitSignal(SignalName.AttackConnected, hurtbox);
    }

    public void ResetTransientState()
    {
        _model.Reset();
        _hitbox?.End();
        _hitboxActive = false;
    }

    private static Vector2 HitboxOffset(AttackDirection direction, int facing) => direction switch
    {
        AttackDirection.Up => new Vector2(0, -42),
        AttackDirection.Down => new Vector2(0, 42),
        _ => new Vector2(facing * 42, 0)
    };
}
