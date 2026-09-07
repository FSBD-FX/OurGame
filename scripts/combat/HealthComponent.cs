using System.Collections.Generic;
using Godot;

namespace Knight.Combat;

public partial class HealthComponent : Node, IDamageReceiver
{
    [Signal]
    public delegate void DamagedEventHandler(int remaining, Vector2 impulse);

    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public int MaxHealth { get; set; } = 5;

    [Export]
    public float InvulnerabilitySeconds { get; set; } = 0.35f;

    public int CurrentHealth { get; private set; }

    private readonly Queue<DamageAttackIdentity> _processedAttackOrder = new();
    private readonly HashSet<DamageAttackIdentity> _processedAttackIds = new();
    private float _invulnerabilityRemaining;
    private bool _isDead;
    private bool _initialized;

    public override void _Ready()
    {
        InitializeHealth();
    }

    public override void _PhysicsProcess(double delta)
    {
        _invulnerabilityRemaining = Mathf.Max(0f, _invulnerabilityRemaining - (float)delta);
    }

    public bool ApplyDamage(DamageRequest request)
    {
        if (!_initialized) InitializeHealth();
        if (request.Amount <= 0 || _isDead || _invulnerabilityRemaining > 0f ||
            _processedAttackIds.Contains(request.AttackIdentity)) return false;

        RememberAttack(request.AttackIdentity);
        CurrentHealth = Mathf.Max(0, CurrentHealth - request.Amount);
        _invulnerabilityRemaining = Mathf.Max(0f, InvulnerabilitySeconds);
        Vector2 impulse = request.Direction.Normalized() * request.Knockback;
        EmitSignal(SignalName.Damaged, CurrentHealth, impulse);
        if (CurrentHealth != 0) return true;

        _isDead = true;
        EmitSignal(SignalName.Died);
        return true;
    }

    public bool ReceiveDamage(DamageRequest request) => ApplyDamage(request);

    public void ResetFull()
    {
        CurrentHealth = Mathf.Max(0, MaxHealth);
        _invulnerabilityRemaining = 0f;
        _processedAttackOrder.Clear();
        _processedAttackIds.Clear();
        _isDead = false;
        _initialized = true;
    }

    private void RememberAttack(DamageAttackIdentity attackIdentity)
    {
        _processedAttackIds.Add(attackIdentity);
        _processedAttackOrder.Enqueue(attackIdentity);
        if (_processedAttackOrder.Count <= 64) return;
        _processedAttackIds.Remove(_processedAttackOrder.Dequeue());
    }

    private void InitializeHealth()
    {
        CurrentHealth = Mathf.Max(0, MaxHealth);
        _initialized = true;
    }
}
