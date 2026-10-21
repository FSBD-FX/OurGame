using Godot;

namespace Knight.Prologue;

public partial class RoomExit : Area2D
{
    [Signal]
    public delegate void ExitRequestedEventHandler(StringName exitId);

    [Export] public StringName ExitId { get; set; }

    public override void _Ready() => BodyEntered += OnBodyEntered;

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player")) EmitSignal(SignalName.ExitRequested, ExitId);
    }
}
