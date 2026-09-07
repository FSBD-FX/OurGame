using Godot;
using Knight.Combat;
using Knight.Player;

namespace Knight.Prologue.Traversal;

public partial class CanyonShieldLaneHazard : Area2D
{
    [Export] public float PulseIntervalSeconds { get; set; } = .28f;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float Knockback { get; set; } = 90f;

    private float _pulseRemaining;
    private ulong _nextAttackId = 1;

    public int PulseCount { get; private set; }

    public override void _Ready()
    {
        Monitoring = true;
        _pulseRemaining = 0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        _pulseRemaining = Mathf.Max(0f, _pulseRemaining - (float)delta);
        if (_pulseRemaining > 0f) return;

        foreach (Node2D body in GetOverlappingBodies())
        {
            if (!body.IsInGroup("player")) continue;
            PlayerGuardedDamageReceiver receiver = body.GetNodeOrNull<PlayerGuardedDamageReceiver>(
                "GuardedDamage");
            if (receiver == null) continue;

            receiver.ReceiveDamage(new DamageRequest(
                Damage,
                Vector2.Left,
                Knockback,
                _nextAttackId++,
                GetInstanceId(),
                DamageGuardClass.Normal));
            PulseCount++;
            _pulseRemaining = PulseIntervalSeconds;
            break;
        }
    }
}
