using System;
using System.Collections.Generic;
using Godot;
using Knight.Config;
using Knight.Presentation;

namespace Knight.Prologue;

public partial class PrologueRoom : Node2D
{
    [Export] public PrologueRoomId RoomId { get; set; }
    [Export] public Rect2 CameraBounds { get; set; } = new(0, 0, 1280, 720);
    [Export] public RoomPresentationProfile PresentationProfile { get; set; } = new();

    public IReadOnlyList<string> ValidateContract(StringName requestedEntrance = default)
    {
        var errors = new List<string>();
        Node entrances = GetNodeOrNull("Entrances");
        Node exits = GetNodeOrNull("Exits");
        if (entrances == null) errors.Add("Entrances node is required");
        if (exits == null) errors.Add("Exits node is required");
        if (GetNodeOrNull("KillZone") is not Area2D) errors.Add("KillZone Area2D is required");
        if (GetNodeOrNull("Presentation/VisualRoot/Placeholder") is not Node2D)
            errors.Add("Presentation/VisualRoot/Placeholder is required");
        if (!FinitePositive(CameraBounds.Size.X) || !FinitePositive(CameraBounds.Size.Y))
            errors.Add("CameraBounds must have positive finite width and height");
        if (!Empty(requestedEntrance) && entrances?.GetNodeOrNull<Marker2D>(new NodePath(requestedEntrance.ToString())) == null)
            errors.Add($"requested entrance '{requestedEntrance}' is missing");

        if (exits != null)
        {
            var ids = new HashSet<StringName>();
            foreach (Node child in exits.GetChildren())
            {
                if (child is not RoomExit roomExit) continue;
                if (Empty(roomExit.ExitId)) errors.Add($"RoomExit '{roomExit.Name}' has an empty ExitId");
                else if (!ids.Add(roomExit.ExitId)) errors.Add($"duplicate RoomExit ID '{roomExit.ExitId}'");
            }
        }
        return errors;
    }

    public Marker2D GetEntrance(StringName entranceId) => GetNodeOrNull<Marker2D>($"Entrances/{entranceId}");

    public void ResetRoomState() => ResetDescendants(this);

    public Node2D ResolvePresentation()
    {
        if (GetNodeOrNull<RoomFormalVisual>("Art") is { } formal)
        {
            Node2D placeholder = GetNodeOrNull<Node2D>("Presentation/VisualRoot/Placeholder");
            if (placeholder != null) placeholder.Visible = false;
            return formal;
        }
        PresentationProfile ??= new RoomPresentationProfile();
        return PresentationProfile.Resolve(GetNode<Node2D>("Presentation/VisualRoot"));
    }

    private static void ResetDescendants(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is ICheckpointResettable resettable) resettable.ResetForCheckpoint();
            ResetDescendants(child);
        }
    }

    private static bool FinitePositive(float value) => float.IsFinite(value) && value > 0f;
    private static bool Empty(StringName value) => value == null || value.IsEmpty;
}
