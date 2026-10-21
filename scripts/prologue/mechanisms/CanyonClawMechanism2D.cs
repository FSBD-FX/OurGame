using System;
using Godot;
using Knight.Mechanisms;

namespace Knight.Prologue.Mechanisms;

public partial class CanyonClawMechanism2D : Node2D, IThreadInteractionReceiver
{
    [Signal] public delegate void ActivatedEventHandler();

    [Export] public CanyonClawMechanismKind Kind { get; set; }
    [Export] public NodePath LinkedNodePath { get; set; } = new("LinkedNode");

    public bool IsActivated { get; private set; }

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if (IsActivated || !IsValid(interaction) || !Enum.IsDefined(Kind)) return false;
        Node2D linked = GetNodeOrNull<Node2D>(LinkedNodePath);
        if (!GodotObject.IsInstanceValid(linked)) return false;

        linked.Position += Displacement(Kind);
        IsActivated = true;
        EmitSignal(SignalName.Activated);
        return true;
    }

    private bool IsValid(ThreadInteraction interaction) =>
        GodotObject.IsInstanceValid(interaction.Player) && interaction.Player.IsInsideTree() &&
        IsInsideTree() && interaction.Player.GetTree() == GetTree() &&
        interaction.Settings != null && Finite(interaction.Origin) &&
        Finite(interaction.TargetPosition);

    private static Vector2 Displacement(CanyonClawMechanismKind kind) => kind switch
    {
        CanyonClawMechanismKind.Counterweight => new Vector2(0f, 120f),
        CanyonClawMechanismKind.CargoPlatform => new Vector2(0f, -120f),
        CanyonClawMechanismKind.HiddenPanel => new Vector2(144f, 0f),
        CanyonClawMechanismKind.BellBody => new Vector2(0f, 96f),
        _ => Vector2.Zero
    };

    private static bool Finite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
