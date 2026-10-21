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
    // ClawDrop uses an actual collision-local face point, not the profile origin.
    [Export] public Vector2 AssistHitPoint { get; set; }
    [Export] public Vector2 AssistHitNormal { get; set; }
    [Export] public NodePath LandingSupportPath { get; set; } = new();

    public IReadOnlyList<string> Validate(Node room)
    {
        var errors = new List<string>();
        if (SegmentId == null || SegmentId.IsEmpty)
            errors.Add($"{nameof(SegmentId)} is required");
        else
        {
            if (Name != SegmentId.ToString().ValidateNodeName())
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
            case TraversalSegmentKind.ClawDrop:
                if (RequiredAbilities != TraversalAbility.Thread)
                    errors.Add($"{nameof(RequiredAbilities)} for ClawDrop must be Thread only");
                ValidateClawDrop(room, errors);
                break;
            case TraversalSegmentKind.GroundTransfer:
                if (RequiredAbilities != TraversalAbility.None)
                    errors.Add($"{nameof(RequiredAbilities)} for GroundTransfer must be None");
                if (AssistTargetPath != null && !AssistTargetPath.IsEmpty)
                    errors.Add($"{nameof(AssistTargetPath)} must be empty for GroundTransfer");
                if (LandingZone.GetCenter().Y < LaunchZone.GetCenter().Y)
                    errors.Add("GroundTransfer cannot declare an upward landing");
                break;
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

    private void ValidateClawDrop(Node room, List<string> errors)
    {
        // A relative path may cross to an adjacent authored sector, but must
        // resolve in the assembled world. Never synthesize a local replacement.
        GrappleSurface2D target = room?.GetNodeOrNull<GrappleSurface2D>(AssistTargetPath);
        if (target == null)
        {
            errors.Add($"ClawDrop must resolve {nameof(AssistTargetPath)} to real terrain");
            return;
        }
        uint requiredLayers = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
        if (!target.UseCollisionHitPoint ||
            target.Material is not (GrappleSurfaceMaterial.GripStone or GrappleSurfaceMaterial.TempleMetal) ||
            !TrySolidRectangle(target.GetParent(), requiredLayers, out CollisionShape2D face,
                out RectangleShape2D faceShape))
        {
            errors.Add("ClawDrop requires enabled solid terrain with World and ThreadTarget collision");
            return;
        }

        const float tolerance = .01f;
        Vector2 half = faceShape.Size * .5f;
        bool verticalFace = MathF.Abs(MathF.Abs(AssistHitNormal.X) - 1f) < tolerance &&
            MathF.Abs(AssistHitNormal.Y) < tolerance &&
            MathF.Abs(AssistHitPoint.X - AssistHitNormal.X * half.X) < tolerance &&
            MathF.Abs(AssistHitPoint.Y) <= half.Y;
        bool horizontalFace = MathF.Abs(MathF.Abs(AssistHitNormal.Y) - 1f) < tolerance &&
            MathF.Abs(AssistHitNormal.X) < tolerance &&
            MathF.Abs(AssistHitPoint.Y - AssistHitNormal.Y * half.Y) < tolerance &&
            MathF.Abs(AssistHitPoint.X) <= half.X;
        if (!Finite(AssistHitPoint.X) || !Finite(AssistHitPoint.Y) ||
            !Finite(AssistHitNormal.X) || !Finite(AssistHitNormal.Y) ||
            !(verticalFace || horizontalFace))
        {
            errors.Add("ClawDrop requires a finite collision-local face point and its outward normal");
            return;
        }

        Transform2D routePose = GraphPose(this), facePose = GraphPose(face);
        if (!Invertible(routePose) || !Invertible(facePose))
        {
            errors.Add("ClawDrop requires finite invertible authored terrain transforms");
            return;
        }
        Vector2 castPoint = routePose * LaunchZone.GetCenter();
        Vector2 hitPoint = facePose * AssistHitPoint;
        Transform2D inverseFace = facePose.AffineInverse();
        Vector2 outward = new(inverseFace.X.Dot(AssistHitNormal), inverseFace.Y.Dot(AssistHitNormal));
        if (!Finite(castPoint.X) || !Finite(castPoint.Y) || !Finite(hitPoint.X) || !Finite(hitPoint.Y) ||
            castPoint.DistanceTo(hitPoint) > MechanicalClawSettings.Default.MaxRange)
            errors.Add("ClawDrop nominal cast-to-hit distance exceeds the unchanged620 px claw range");
        if ((castPoint - hitPoint).Dot(outward) <= 0f)
            errors.Add("ClawDrop cast must approach the declared outward terrain face");

        Node support = room?.GetNodeOrNull(LandingSupportPath);
        if (!TrySolidRectangle(support, CombatCollisionLayers.World, out CollisionShape2D floor,
                out RectangleShape2D floorShape))
        {
            errors.Add("ClawDrop must resolve its enabled solid landing support");
            return;
        }
        Transform2D floorPose = GraphPose(floor);
        if (!Invertible(floorPose))
        {
            errors.Add("ClawDrop landing support must have a finite invertible transform");
            return;
        }
        Transform2D intoFloor = floorPose.AffineInverse() * routePose;
        Vector2 lowerLeft = intoFloor * new Vector2(LandingZone.Position.X, LandingZone.End.Y);
        Vector2 lowerRight = intoFloor * LandingZone.End;
        Vector2 floorHalf = floorShape.Size * .5f;
        if (!Finite(lowerLeft.X) || !Finite(lowerLeft.Y) || !Finite(lowerRight.X) || !Finite(lowerRight.Y) ||
            MathF.Abs(lowerLeft.Y + floorHalf.Y) > tolerance ||
            MathF.Abs(lowerRight.Y + floorHalf.Y) > tolerance ||
            MathF.Min(lowerLeft.X, lowerRight.X) < -floorHalf.X ||
            MathF.Max(lowerLeft.X, lowerRight.X) > floorHalf.X)
            errors.Add("ClawDrop landing rectangle must end on the complete real landing support, not in air");
        if (hitPoint.Y >= (routePose * LandingZone.GetCenter()).Y)
            errors.Add("ClawDrop must finish below its claw hit point by natural gravity");
    }

    private static bool TrySolidRectangle(Node node, uint layers, out CollisionShape2D collision,
        out RectangleShape2D rectangle)
    {
        collision = null; rectangle = null;
        if (node is not StaticBody2D body || (body.CollisionLayer & layers) != layers ||
            body.GetNodeOrNull<CollisionShape2D>("Collision") is not { Disabled: false } shape ||
            shape.OneWayCollision || shape.Shape is not RectangleShape2D size ||
            !Finite(size.Size.X) || !Finite(size.Size.Y) || size.Size.X <= 0f || size.Size.Y <= 0f)
            return false;
        collision = shape; rectangle = size;
        return true;
    }

    private static Transform2D GraphPose(Node node)
    {
        // Works on both packed/builder graphs before Ready and mounted graphs;
        // no GlobalTransform calls on detached nodes or hidden world offsets.
        Transform2D pose = Transform2D.Identity;
        for (Node current = node; current != null; current = current.GetParent())
            if (current is Node2D item) pose = item.Transform * pose;
        return pose;
    }

    private static bool Invertible(Transform2D pose) =>
        Finite(pose.X.X) && Finite(pose.X.Y) && Finite(pose.Y.X) && Finite(pose.Y.Y) &&
        Finite(pose.Origin.X) && Finite(pose.Origin.Y) &&
        Finite(pose.Determinant()) && MathF.Abs(pose.Determinant()) > .000001f;

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
        if (target.UseCollisionHitPoint)
        {
            uint requiredLayers = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
            bool collisionFound = false;
            if (target.GetParent() is StaticBody2D terrain &&
                (terrain.CollisionLayer & requiredLayers) == requiredLayers)
                foreach (Node child in terrain.GetChildren())
                    if (child is CollisionShape2D { Disabled: false, Shape: RectangleShape2D shape } &&
                        Finite(shape.Size.X) && Finite(shape.Size.Y) && shape.Size.X > 0f && shape.Size.Y > 0f)
                        collisionFound = true;
            if (target.Material is not (GrappleSurfaceMaterial.GripStone or GrappleSurfaceMaterial.TempleMetal) ||
                !collisionFound)
                errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must be an enabled solid terrain profile with World and ThreadTarget collision");
        }
        else
        {
            // Layout-1 authored point anchors retain their original compatibility contract.
            if (target.Material != GrappleSurfaceMaterial.DedicatedAnchor)
                errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must resolve to a DedicatedAnchor");
            Area2D collision = target.GetNodeOrNull<Area2D>("TargetCollision");
            if (collision == null || collision.CollisionLayer != CombatCollisionLayers.ThreadTarget)
                errors.Add($"{nameof(AssistTargetPath)} '{AssistTargetPath}' must use the ThreadTarget collision layer");
        }

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
