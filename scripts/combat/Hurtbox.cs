using Godot;

namespace Knight.Combat;

public partial class Hurtbox : Area2D
{
    [Export] public NodePath DamageReceiverPath { get; set; } = new("../Health");

    public override void _Ready()
    {
        ResolveDamageReceiver();
    }

    public bool Receive(DamageRequest request)
    {
        return ResolveDamageReceiver().ReceiveDamage(request);
    }

    // Exported paths and same-path receiver nodes can change during gameplay.
    // Consumers that aggregate damage capture this one authoritative receiver
    // for both identity and delivery; no stale _Ready cache survives replacement.
    public IDamageReceiver ResolveDamageReceiver()
    {
        return GetNodeOrNull<Node>(DamageReceiverPath) as IDamageReceiver ??
            throw new System.InvalidOperationException(
                $"Hurtbox DamageReceiverPath '{DamageReceiverPath}' must resolve to {nameof(IDamageReceiver)}.");
    }

    public void SetDamageReceiverPath(NodePath path)
    {
        DamageReceiverPath = path;
    }
}
