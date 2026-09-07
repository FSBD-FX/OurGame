using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;

namespace Knight.Prologue.Traversal;

public partial class TraversalSegment2D : Node2D
{
    private const float MechanicalClawRange = 520f;
    private const float MinimumZoneWidth = 48f;
    private const TraversalAbility LegalAbilities =
        TraversalAbility.Jump | TraversalAbility.Thread | TraversalAbility.Dash;

    [Export] public StringName SegmentId { get; set; }
    [Export] public TraversalSegmentKind Kind { get; set; }
    [Export] public TraversalAbility RequiredAbilities { get; set; }
    [Export] public Rect2 LaunchZone { get; set; }
    [Export] public Rect2 LandingZone { get; set; }
    [Export] public NodePath AssistTargetPath { get; set; } = new();

    public IReadOnlyList<string> Validate(Node room)
    {
        var errors = new List<string>();
        if (SegmentId == null || SegmentId.IsEmpty)
            errors.Add($"{nameof(SegmentId)} is required");
        else
        {
            if (Name != SegmentId)
                errors.Add($"{nameof(SegmentId)} '{SegmentId}' must match node name '{Name}'");
            if (HasDuplicateId()) errors.Add($"duplicate {nameof(SegmentId)} '{SegmentId}'");
        }

        ValidateZone(LaunchZone, nameof(LaunchZone), errors);
        ValidateZone(LandingZone, nameof(LandingZone), errors);

        if (!Enum.IsDefined(Kind)) errors.Add($"{nameof(Kind)} is invalid");
        if ((RequiredAbilities & ~LegalAbilities) != 0)
            errors.Add($"{nameof(RequiredAbilities)} contains an illegal ability mask");
        if (Kind != TraversalSegmentKind.OptionalAdvanced &&
            (RequiredAbilities & TraversalAbility.Dash) != 0)
            errors.Add("Dash is not allowed on a mandatory traversal segment");

        switch (Kind)
        {
            case TraversalSegmentKind.SingleJump:
                if (RequiredAbilities != TraversalAbility.Jump)
                    errors.Add($"{nameof(RequiredAbilities)} for SingleJump must be Jump");
                if (AssistTargetPath != null && !AssistTargetPath.IsEmpty)
                    errors.Add($"{nameof(AssistTargetPath)} must be empty for SingleJump");
                break;
            case TraversalSegmentKind.Assisted:
                if (RequiredAbilities != (TraversalAbility.Jump | TraversalAbility.Thread))
                    errors.Add($"{nameof(RequiredAbilities)} for Assisted must be Jump | Thread");
                ValidateAssistTarget(room, errors);
                break;
            case TraversalSegmentKind.OptionalAdvanced:
                if ((RequiredAbilities & TraversalAbility.Dash) == 0)
                    errors.Add($"{nameof(RequiredAbilities)} for OptionalAdvanced must include Dash");
                break;
        }

        return errors;
    }

    private bool HasDuplicateId()
    {
        Node parent = GetParent();
        if (parent == null || SegmentId == null || SegmentId.IsEmpty) return false;
        foreach (Node sibling in parent.GetChildren())
            if (!ReferenceEquals(sibling, this) && sibling is TraversalSegment2D segment &&
                segment.SegmentId == SegmentId) return true;
        return false;
    }

    private void ValidateAssistTarget(Node room, List<string> errors)
    {
        if (AssistTargetPath == null || AssistTargetPath.IsEmpty)
        {
            errors.Add($"{nameof(AssistTargetPath)} is required for Assisted");
            return;
        }
        if (room == null)
        {
            errors.Add("room is required to resolve AssistTargetPath");
            return;
        }

        GrappleSurface2D target = room.GetNodeOrNull<GrappleSurface2D>(AssistTargetPath);
        if (target == null)
        {
            errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must resolve to GrappleSurface2D");
            return;
        }
        if (target.Material != GrappleSurfaceMaterial.DedicatedAnchor)
            errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must resolve to a DedicatedAnchor");
        Area2D collision = target.GetNodeOrNull<Area2D>("TargetCollision");
        if (collision == null || collision.CollisionLayer != CombatCollisionLayers.ThreadTarget)
            errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must use the ThreadTarget collision layer");

        Vector2 anchor = RoomLocalPosition(room, target);
        Vector2 launch = LaunchZone.GetCenter();
        Vector2 landing = LandingZone.GetCenter();
        if (!float.IsFinite(anchor.X) || !float.IsFinite(anchor.Y))
        {
            errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must resolve to a finite room-local position");
            return;
        }
        if (launch.DistanceTo(anchor) > MechanicalClawRange)
            errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' exceeds the 520 px claw range");
        if ((anchor.X - launch.X) * (landing.X - launch.X) <= 0f)
            errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' is outside the landing-facing half-plane");
    }

    private static Vector2 RoomLocalPosition(Node room, Node2D target)
    {
        Transform2D transform = Transform2D.Identity;
        for (Node current = target; current != null && !ReferenceEquals(current, room); current = current.GetParent())
            if (current is Node2D node2D) transform = node2D.Transform * transform;
        return transform.Origin;
    }

    private static void ValidateZone(Rect2 zone, string name, List<string> errors)
    {
        if (!Finite(zone.Position.X) || !Finite(zone.Position.Y) || !Finite(zone.Size.X) ||
            !Finite(zone.Size.Y) || zone.Size.X < MinimumZoneWidth || zone.Size.Y <= 0f)
            errors.Add($"{name} must have finite positive dimensions and be at least 48 px wide");
    }

    private static bool Finite(float value) => float.IsFinite(value);
}
