using Godot;

namespace Knight.World;

/// <summary>Presentation-only readable route status for the gray-box prototype.</summary>
public partial class PrototypeHud : CanvasLayer
{
    [Export] public NodePath StatusPath { get; set; } = new NodePath("Status");

    public void SetStatus(string message)
    {
        Label label = GetNodeOrNull<Label>(StatusPath);
        if (label != null) label.Text = message;
    }
}
