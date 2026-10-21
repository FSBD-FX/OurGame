using Godot;
using Knight.Prologue;

namespace Knight.World;

/// <summary>Observable fall boundary; it delegates recovery rather than consuming player resources.</summary>
public partial class KillZone : Area2D
{
    // Spatial source for opt-in residents; the existing player recovery signal
    // and collision mask keep their original semantics.
    public const string ResidentVoidGroup="resident_void_boundaries";
    [Export] public NodePath RoomControllerPath { get; set; } = new NodePath("..");

    public override void _Ready()
    {
        AddToGroup(ResidentVoidGroup);
        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        Node controller = GetNodeOrNull<Node>(RoomControllerPath);
        if (controller is ILevelRecovery recovery) recovery.RequestRespawn("FALL RESET");
        else GD.PushError($"KillZone could not resolve ILevelRecovery at '{RoomControllerPath}'.");
    }
}
