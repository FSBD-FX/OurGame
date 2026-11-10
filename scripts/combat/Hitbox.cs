using System;
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

    // Optional actor-specific contact permission. Rejected contacts must not spend
    // this attack's per-target identity; unconfigured hitboxes keep their old rules.
    public Func<Hurtbox, bool> TargetFilter { get; set; }

    // Optional source-owned lifetime veto, independent of caller-composed target
    // rules. Check again after callbacks and before spending target identity.
    // Only configured sources opt in; all other hitboxes retain their old rules.
    internal Func<bool> SourcePermission { get; set; }

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
        if (IsOwnHurtbox(hurtbox) || SourcePermission?.Invoke() == false ||
            TargetFilter?.Invoke(hurtbox) == false || SourcePermission?.Invoke() == false ||
            !_hitInstances.Add(hurtbox.GetInstanceId())) return false;
        Vector2 direction = GlobalPosition.DirectionTo(hurtbox.GlobalPosition);
        if (hurtbox.Receive(new DamageRequest(Damage, direction, Knockback, _attackId, GetInstanceId(), GuardClass)))
        {
            EmitSignal(SignalName.Connected, hurtbox);
            return true;
        }
        return false;
    }
}
