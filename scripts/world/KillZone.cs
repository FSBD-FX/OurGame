using Godot;
using Knight.Prologue;

namespace Knight.World;

/// <summary>Observable fall boundary; it delegates recovery rather than consuming player resources.</summary>
public partial class KillZone : Area2D
{
    [Export] public NodePath RoomControllerPath { get; set; } = new NodePath("..");

    public override void _Ready()
    {
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
