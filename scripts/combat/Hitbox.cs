using System.Collections.Generic;
using Godot;

namespace Knight.Combat;

public partial class Hitbox : Area2D
{
    [Signal]
    public delegate void ConnectedEventHandler(Hurtbox hurtbox);

    [Export]
    public int Damage { get; set; } = 1;

    [Export]
    public float Knockback { get; set; } = 260f;

    [Export]
    public DamageGuardClass GuardClass { get; set; } = DamageGuardClass.Normal;

    private ulong _attackId;
    private readonly HashSet<ulong> _hitInstances = new();

    public override void _Ready()
    {
        Monitoring = false;
        AreaEntered += OnAreaEntered;
    }

    public void Begin(ulong attackId)
    {
        _attackId = attackId;
        _hitInstances.Clear();
        Monitoring = true;
    }

    public void End() => Monitoring = false;

    private void OnAreaEntered(Area2D area)
    {
        if (area is Hurtbox hurtbox) TryConnect(hurtbox);
    }

    public bool IsOwnHurtbox(Hurtbox hurtbox)
    {
        Node sourceRoot = Owner ?? GetParent()?.GetParent();
        return sourceRoot != null && (hurtbox.Owner == sourceRoot || hurtbox.GetParent() == sourceRoot);
    }

    public bool TryConnect(Hurtbox hurtbox)
    {
        if (IsOwnHurtbox(hurtbox) || !_hitInstances.Add(hurtbox.GetInstanceId())) return false;
        Vector2 direction = GlobalPosition.DirectionTo(hurtbox.GlobalPosition);
        if (hurtbox.Receive(new DamageRequest(Damage, direction, Knockback, _attackId, GetInstanceId(), GuardClass)))
        {
            EmitSignal(SignalName.Connected, hurtbox);
            return true;
        }
        return false;
    }
}
