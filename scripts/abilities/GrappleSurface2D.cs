using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Abilities;

public partial class GrappleSurface2D : Node2D
{
    public const string TargetGroup = "grapple_surfaces";

    [Export] public new GrappleSurfaceMaterial Material { get; set; } = GrappleSurfaceMaterial.DedicatedAnchor;
    [Export] public float AssistRadius { get; set; } = MechanicalClawSettings.Default.MainAnchorAssistRadius;
    [Export] public Vector2 AnchorOffset { get; set; }
    [Export] public bool UseCollisionHitPoint { get; set; }

    public Vector2 WorldAnchor => ToGlobal(AnchorOffset);
    public virtual bool IsAvailable => IsInsideTree() && IsInGroup(TargetGroup) && IsLiveNodeChain(this);

    public override void _Ready()
    {
        AddToGroup(TargetGroup);
    }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(Material)) errors.Add($"{nameof(Material)} is invalid");
        if (!float.IsFinite(AssistRadius) || AssistRadius <= 0f ||
            AssistRadius > MechanicalClawSettings.Default.MainAnchorAssistRadius)
            errors.Add($"{nameof(AssistRadius)} must be finite and inside (0, 36]");
        if (!float.IsFinite(AnchorOffset.X) || !float.IsFinite(AnchorOffset.Y))
            errors.Add($"{nameof(AnchorOffset)} must be finite");
        return errors;
    }

    protected static bool IsLiveNodeChain(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (!GodotObject.IsInstanceValid(current) || current.IsQueuedForDeletion()) return false;
        return node.IsInsideTree();
    }
}
