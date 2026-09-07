using Godot;

namespace Knight.Combat;

public partial class Hurtbox : Area2D
{
    [Export] public NodePath DamageReceiverPath { get; set; } = new("../Health");

    private IDamageReceiver _receiver;

    public override void _Ready()
    {
        ResolveReceiver();
    }

    public bool Receive(DamageRequest request)
    {
        if (_receiver == null) ResolveReceiver();
        return _receiver.ReceiveDamage(request);
    }

    private void ResolveReceiver()
    {
        _receiver = GetNode(DamageReceiverPath) as IDamageReceiver ??
            throw new System.InvalidOperationException(
                $"Hurtbox DamageReceiverPath '{DamageReceiverPath}' must resolve to {nameof(IDamageReceiver)}.");
    }

    public void SetDamageReceiverPath(NodePath path)
    {
        DamageReceiverPath = path;
        _receiver = null;
    }
}
