using Godot;

namespace Knight.Prologue;

public partial class HiddenAlcoveTrigger : Area2D
{
    [Signal] public delegate void DiscoveredEventHandler(StringName discoveryId);

    [Export] public StringName DiscoveryId { get; set; }

    public override void _Ready() => BodyEntered += OnBodyEntered;

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player") && DiscoveryId != null && !DiscoveryId.IsEmpty)
            EmitSignal(SignalName.Discovered, DiscoveryId);
    }
}
