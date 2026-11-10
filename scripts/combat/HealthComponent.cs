using System.Collections.Generic;
using Godot;

namespace Knight.Combat;

public partial class HealthComponent : Node, IDamageReceiver
{
    [Signal]
    public delegate void DamagedEventHandler(int remaining, Vector2 impulse);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void HealedEventHandler(int restored, int remaining);

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
    private (int Current, int Maximum)? _residentMountHealth;

    public override void _Ready()
    {
        var restored = _residentMountHealth;
        InitializeHealth();
        if (restored is { } snapshot)
        {
            if (snapshot.Maximum != MaxHealth)
                throw new System.InvalidOperationException("Resident health maximum changed between preparation and native Ready.");
            CurrentHealth = snapshot.Current;
        }
    }

    // Internal mount transaction only: never writes a live actor's HP, emits
    // no fake combat/heal/death signals, and cannot resurrect a used component.
    internal bool TryPrepareResidentMountHealth(int current, int maximum)
    {
        if (_initialized || _residentMountHealth.HasValue || IsInsideTree() || IsNodeReady() || IsQueuedForDeletion() ||
            maximum != MaxHealth || maximum <= 0 || current <= 0 || current > maximum) return false;
        _residentMountHealth = (current, maximum);
        return true;
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

    public int RestoreHealth(int amount)
    {
        if (amount <= 0) return 0;
        if (!_initialized) InitializeHealth();
        if (_isDead || CurrentHealth <= 0 || CurrentHealth >= MaxHealth) return 0;

        int restored = Mathf.Min(amount, MaxHealth - CurrentHealth);
        CurrentHealth += restored;
        EmitSignal(SignalName.Healed, restored, CurrentHealth);
        return restored;
    }

    public void ResetFull()
    {
        _residentMountHealth = null;
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
        _residentMountHealth = null;
        CurrentHealth = Mathf.Max(0, MaxHealth);
        _initialized = true;
    }
}
