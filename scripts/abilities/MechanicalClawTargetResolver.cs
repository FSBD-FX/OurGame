using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Abilities;

public readonly record struct MechanicalClawTargetResolution(
    GrappleSurface2D Surface,
    MechanicalClawTargetSnapshot Snapshot,
    float CursorDistance)
{
    public static MechanicalClawTargetResolution None => new(
        null,
        new MechanicalClawTargetSnapshot(0UL, false, GrappleSurfaceMaterial.GripStone,
            Vector2.Zero, false, false),
        float.PositiveInfinity);
}

public sealed class MechanicalClawTargetResolver
{
    private readonly MechanicalClawSettings _settings;

    public MechanicalClawTargetResolver(MechanicalClawSettings? settings = null)
    {
        _settings = settings ?? MechanicalClawSettings.Default;
        _settings.Validate();
    }

    public MechanicalClawTargetResolution Resolve(Node2D caster, Vector2 cursorWorld)
    {
        if (!GodotObject.IsInstanceValid(caster) || !caster.IsInsideTree() ||
            !Finite(cursorWorld)) return MechanicalClawTargetResolution.None;

        var shape = new CircleShape2D { Radius = _settings.MainAnchorAssistRadius };
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = new Transform2D(0f, cursorWorld),
            CollisionMask = CombatCollisionLayers.ThreadTarget,
            CollideWithAreas = true,
            CollideWithBodies = true,
            Exclude = CollisionRidsInSubtree(caster)
        };
        Godot.Collections.Array<Godot.Collections.Dictionary> hits =
            caster.GetWorld2D().DirectSpaceState.IntersectShape(query, 64);
        GrappleSurface2D selected = null;
        float selectedDistance = float.PositiveInfinity;
        ulong selectedId = ulong.MaxValue;
        var seen = new HashSet<ulong>();

        foreach (Godot.Collections.Dictionary hit in hits)
        {
            if (!hit.ContainsKey("collider") || hit["collider"].AsGodotObject() is not Node collider)
                continue;
            GrappleSurface2D surface = FindSurface(collider);
            if (!GodotObject.IsInstanceValid(surface) || !surface.IsAvailable || surface.Validate().Count > 0)
                continue;
            if (surface.UseCollisionHitPoint) continue;
            ulong id = surface.GetInstanceId();
            if (!seen.Add(id)) continue;
            float distance = cursorWorld.DistanceTo(surface.WorldAnchor);
            float assist = Mathf.Min(surface.AssistRadius, _settings.MainAnchorAssistRadius);
            if (distance > assist + .0001f) continue;
            if (distance < selectedDistance - .0001f ||
                Mathf.IsEqualApprox(distance, selectedDistance) && id < selectedId)
            {
                selected = surface;
                selectedDistance = distance;
                selectedId = id;
            }
        }

        if (selected == null) return ResolveTerrainRay(caster, cursorWorld);
        bool visible = HasLineOfSight(caster, selected, selected.WorldAnchor);
        bool legacy = selected.GetNodeOrNull<LegacyThreadInteractionAdapter>(
            "LegacyThreadInteractionAdapter") != null;
        return new MechanicalClawTargetResolution(selected,
            new MechanicalClawTargetSnapshot(
                selected.GetInstanceId(),
                selected.IsAvailable,
                selected.Material,
                selected.WorldAnchor,
                visible,
                legacy),
            selectedDistance);
    }

    public MechanicalClawTargetSnapshot SnapshotLocked(Node2D caster, GrappleSurface2D surface,
        Vector2 lockedAnchor)
    {
        bool validSurface = GodotObject.IsInstanceValid(surface);
        Vector2 anchor = validSurface && surface.UseCollisionHitPoint
            ? lockedAnchor
            : validSurface ? surface.WorldAnchor : lockedAnchor;
        if (!GodotObject.IsInstanceValid(caster) || !GodotObject.IsInstanceValid(surface) ||
            !surface.IsAvailable || caster.GetTree() != surface.GetTree() || !Finite(anchor))
            return new MechanicalClawTargetSnapshot(
                surface != null && GodotObject.IsInstanceValid(surface) ? surface.GetInstanceId() : 0UL,
                false,
                surface != null && GodotObject.IsInstanceValid(surface)
                    ? surface.Material
                    : GrappleSurfaceMaterial.GripStone,
                Finite(anchor) ? anchor : Vector2.Zero,
                false,
                false);

        return new MechanicalClawTargetSnapshot(
            surface.GetInstanceId(),
            true,
            surface.Material,
            anchor,
            HasLineOfSight(caster, surface, anchor),
            surface.GetNodeOrNull<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter") != null);
    }

    private MechanicalClawTargetResolution ResolveTerrainRay(Node2D caster, Vector2 cursorWorld)
    {
        Vector2 origin = caster.GlobalPosition;
        if (origin.IsEqualApprox(cursorWorld)) return MechanicalClawTargetResolution.None;
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(origin, cursorWorld);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
        query.Exclude = CollisionRidsInSubtree(caster);
        Godot.Collections.Dictionary result = caster.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0 || !result.ContainsKey("collider") ||
            result["collider"].AsGodotObject() is not Node collider ||
            !result.ContainsKey("position"))
            return MechanicalClawTargetResolution.None;

        Vector2 hitPoint = result["position"].AsVector2();
        if (!Finite(hitPoint)) return MechanicalClawTargetResolution.None;
        GrappleSurface2D surface = FindSurface(collider);
        if (!GodotObject.IsInstanceValid(surface) || !surface.IsAvailable ||
            surface.Validate().Count > 0 || !surface.UseCollisionHitPoint)
        {
            return new MechanicalClawTargetResolution(null,
                new MechanicalClawTargetSnapshot(collider.GetInstanceId(), true,
                    GrappleSurfaceMaterial.GripStone, hitPoint, false, false),
                cursorWorld.DistanceTo(hitPoint));
        }

        return new MechanicalClawTargetResolution(surface,
            new MechanicalClawTargetSnapshot(surface.GetInstanceId(), true, surface.Material,
                hitPoint, true, false),
            cursorWorld.DistanceTo(hitPoint));
    }

    private static bool HasLineOfSight(Node2D caster, GrappleSurface2D surface, Vector2 anchor)
    {
        Vector2 origin = caster.GlobalPosition;
        if (origin.IsEqualApprox(anchor)) return true;
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(origin, anchor);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
            CombatCollisionLayers.ThreadTarget;
        query.Exclude = CollisionRidsInSubtree(caster);
        Godot.Collections.Dictionary result = caster.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0) return true;
        Node collider = result["collider"].AsGodotObject() as Node;
        return collider != null && BelongsToSurface(collider, surface);
    }

    private static GrappleSurface2D FindSurface(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
        {
            if (current is GrappleSurface2D point) return point;
            GrappleSurface2D profile = current.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
            if (GodotObject.IsInstanceValid(profile)) return profile;
        }
        return null;
    }

    private static bool BelongsToSurface(Node collider, GrappleSurface2D surface)
    {
        if (IsSelfOrDescendant(collider, surface) || IsSelfOrDescendant(surface, collider)) return true;
        if (surface is not ThreadTarget2D threadTarget) return false;
        Node receiver = threadTarget.GetNodeOrNull(threadTarget.PullReceiverPath);
        return receiver != null &&
            (IsSelfOrDescendant(collider, receiver) || IsSelfOrDescendant(receiver, collider));
    }

    private static bool IsSelfOrDescendant(Node node, Node possibleAncestor)
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (ReferenceEquals(current, possibleAncestor)) return true;
        return false;
    }

    private static Godot.Collections.Array<Rid> CollisionRidsInSubtree(Node root)
    {
        var rids = new Godot.Collections.Array<Rid>();
        AddCollisionRids(root, rids);
        return rids;
    }

    private static void AddCollisionRids(Node node, Godot.Collections.Array<Rid> rids)
    {
        if (node is CollisionObject2D collisionObject) rids.Add(collisionObject.GetRid());
        foreach (Node child in node.GetChildren()) AddCollisionRids(child, rids);
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
