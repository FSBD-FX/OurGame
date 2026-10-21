using System;
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
        new MechanicalClawTargetSnapshot(
            RuntimeId: 0UL,
            Exists: false,
            Material: GrappleSurfaceMaterial.GripStone,
            WorldAnchor: Vector2.Zero,
            HasLineOfSight: false,
            IsLegacyInteraction: false,
            SurfaceNormal: Vector2.Zero),
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
                RuntimeId: selected.GetInstanceId(),
                Exists: selected.IsAvailable,
                Material: selected.Material,
                WorldAnchor: selected.WorldAnchor,
                HasLineOfSight: visible,
                IsLegacyInteraction: legacy,
                SurfaceNormal: selected.WorldAnchor.DirectionTo(caster.GlobalPosition)),
            selectedDistance);
    }

    public MechanicalClawTargetResolution ResolveKeyboard(Node2D caster, Vector2 direction)
    {
        if (!GodotObject.IsInstanceValid(caster) || !caster.IsInsideTree() ||
            !Finite(direction) || direction.IsZeroApprox()) return MechanicalClawTargetResolution.None;
        direction = direction.Normalized();
        const float halfAngle = Mathf.Pi / 8f;
        MechanicalClawTargetResolution best = MechanicalClawTargetResolution.None;
        float bestAngle = float.PositiveInfinity, bestDistance = float.PositiveInfinity;
        string bestId = null;
        // Resolution is synchronous: the live caster exclusions need one fresh
        // traversal per invocation, not another traversal for every candidate LOS.
        Godot.Collections.Array<Rid> excluded = CollisionRidsInSubtree(caster);
        using var excludedOwner = (Godot.Collections.Array)excluded;
        using var visibilityQuery = PhysicsRayQueryParameters2D.Create(caster.GlobalPosition, caster.GlobalPosition);
        visibilityQuery.CollideWithAreas = true; visibilityQuery.CollideWithBodies = true;
        visibilityQuery.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
            CombatCollisionLayers.ThreadTarget;
        visibilityQuery.Exclude = excluded;
        void Consider(GrappleSurface2D surface, Vector2 anchor, Vector2 normal)
        {
            if (!GodotObject.IsInstanceValid(surface) || !surface.IsAvailable || surface.Validate().Count > 0 ||
                surface.GetWorld2D() != caster.GetWorld2D() ||
                surface.Material is GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden ||
                !Finite(anchor) || !Finite(normal)) return;
            if (surface is ThreadTarget2D thread && !thread.TryGetEligibleReceiver(caster, out _)) return;
            Vector2 offset = anchor - caster.GlobalPosition;
            float distance = offset.Length();
            if (distance <= .0001f || distance > _settings.MaxRange + .0001f) return;
            float angle = Mathf.Abs(direction.AngleTo(offset));
            if (angle > halfAngle + .0001f || !HasLineOfSight(caster, surface, anchor, visibilityQuery)) return;
            // Authored unique node paths survive a packed-scene reload; allocation IDs do not.
            string id = surface.GetPath().ToString();
            bool better = angle < bestAngle - .0001f || Mathf.IsEqualApprox(angle, bestAngle) &&
                (distance < bestDistance - .0001f || Mathf.IsEqualApprox(distance, bestDistance) &&
                    string.CompareOrdinal(id, bestId) < 0);
            if (!better) return;
            bestAngle = angle; bestDistance = distance; bestId = id;
            best = new MechanicalClawTargetResolution(surface,
                new MechanicalClawTargetSnapshot(surface.GetInstanceId(), true, surface.Material,
                    anchor, true, surface.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                        "LegacyThreadInteractionAdapter") != null, normal), 0f);
        }
        Godot.Collections.Array<Node> candidates = caster.GetTree().GetNodesInGroup(GrappleSurface2D.TargetGroup);
        using var candidatesOwner = (Godot.Collections.Array)candidates;
        foreach (Node node in candidates)
            if (node is GrappleSurface2D surface && !surface.UseCollisionHitPoint)
                Consider(surface, surface.WorldAnchor, surface.WorldAnchor.DirectionTo(caster.GlobalPosition));

        // Finite 33-ray cone, including the axis and both boundaries. A solid first hit
        // blocks that ray even if its material is ice or has no grapple profile.
        using var query = PhysicsRayQueryParameters2D.Create(caster.GlobalPosition, caster.GlobalPosition);
        query.CollideWithAreas = true; query.CollideWithBodies = true;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget;
        query.Exclude = excluded;
        for (int ray = -16; ray <= 16; ray++)
        {
            Vector2 end = caster.GlobalPosition + direction.Rotated(halfAngle * ray / 16f) * _settings.MaxRange;
            query.To = end;
            using var hit = caster.GetWorld2D().DirectSpaceState.IntersectRay(query);
            if (!hit.ContainsKey("collider") || !hit.ContainsKey("position") || !hit.ContainsKey("normal") ||
                hit["collider"].AsGodotObject() is not Node collider) continue;
            GrappleSurface2D surface = FindSurface(collider);
            if (surface?.UseCollisionHitPoint == true)
                Consider(surface, hit["position"].AsVector2(), hit["normal"].AsVector2());
        }
        return best;
    }

    private static bool HasLineOfSight(Node2D caster, GrappleSurface2D surface,
        Vector2 anchor, PhysicsRayQueryParameters2D query)
    {
        if (caster.GlobalPosition.IsEqualApprox(anchor)) return true;
        query.From = caster.GlobalPosition;
        query.To = anchor;
        using Godot.Collections.Dictionary result = caster.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0) return true;
        Node collider = result["collider"].AsGodotObject() as Node;
        return collider != null && ColliderBelongsToSurface(collider, surface);
    }

    public MechanicalClawTargetSnapshot SnapshotLocked(Node2D caster, GrappleSurface2D surface,
        Vector2 lockedAnchor, Vector2 lockedNormal)
    {
        bool validSurface = GodotObject.IsInstanceValid(surface);
        Vector2 anchor = validSurface && surface.UseCollisionHitPoint
            ? lockedAnchor
            : validSurface ? surface.WorldAnchor : lockedAnchor;
        if (!GodotObject.IsInstanceValid(caster) || !GodotObject.IsInstanceValid(surface) ||
            !surface.IsAvailable || caster.GetTree() != surface.GetTree() || !Finite(anchor))
            return new MechanicalClawTargetSnapshot(
                RuntimeId: surface != null && GodotObject.IsInstanceValid(surface)
                    ? surface.GetInstanceId()
                    : 0UL,
                Exists: false,
                Material: surface != null && GodotObject.IsInstanceValid(surface)
                    ? surface.Material
                    : GrappleSurfaceMaterial.GripStone,
                WorldAnchor: Finite(anchor) ? anchor : Vector2.Zero,
                HasLineOfSight: false,
                IsLegacyInteraction: false,
                SurfaceNormal: Finite(lockedNormal) ? lockedNormal : Vector2.Zero);

        return new MechanicalClawTargetSnapshot(
            RuntimeId: surface.GetInstanceId(),
            Exists: true,
            Material: surface.Material,
            WorldAnchor: anchor,
            HasLineOfSight: HasLineOfSight(caster, surface, anchor),
            IsLegacyInteraction: surface.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "LegacyThreadInteractionAdapter") != null,
            SurfaceNormal: Finite(lockedNormal) ? lockedNormal : Vector2.Zero);
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
            !result.ContainsKey("position") || !result.ContainsKey("normal"))
            return MechanicalClawTargetResolution.None;

        Vector2 hitPoint = result["position"].AsVector2();
        Vector2 normal = result["normal"].AsVector2();
        if (!Finite(hitPoint) || !Finite(normal) || normal.IsZeroApprox())
            return MechanicalClawTargetResolution.None;
        normal = normal.Normalized();
        GrappleSurface2D surface = FindSurface(collider);
        if (!GodotObject.IsInstanceValid(surface) || !surface.IsAvailable ||
            surface.Validate().Count > 0 || !surface.UseCollisionHitPoint)
        {
            return new MechanicalClawTargetResolution(null,
                new MechanicalClawTargetSnapshot(
                    RuntimeId: collider.GetInstanceId(),
                    Exists: true,
                    Material: GrappleSurfaceMaterial.GripStone,
                    WorldAnchor: hitPoint,
                    HasLineOfSight: false,
                    IsLegacyInteraction: false,
                    SurfaceNormal: normal),
                cursorWorld.DistanceTo(hitPoint));
        }

        return new MechanicalClawTargetResolution(surface,
            new MechanicalClawTargetSnapshot(
                RuntimeId: surface.GetInstanceId(),
                Exists: true,
                Material: surface.Material,
                WorldAnchor: hitPoint,
                HasLineOfSight: true,
                IsLegacyInteraction: false,
                SurfaceNormal: normal),
            cursorWorld.DistanceTo(hitPoint));
    }

    public static bool HasLineOfSight(Node2D caster, GrappleSurface2D surface, Vector2 anchor)
    {
        Vector2 origin = caster.GlobalPosition;
        if (origin.IsEqualApprox(anchor)) return true;
        Godot.Collections.Dictionary result = TraceLineOfSight(caster, origin, anchor);
        if (result.Count == 0) return true;
        Node collider = result["collider"].AsGodotObject() as Node;
        return collider != null && ColliderBelongsToSurface(collider, surface);
    }

    // Presentation-only query. A failed/indeterminate trace must not authorize drawing a
    // full rope. Cast admission keeps its existing HitFromInside=false query below.
    public static bool TryGetLineOcclusion(Node2D caster, GrappleSurface2D surface,
        Vector2 origin, Vector2 anchor, out Vector2 point, out Vector2 normal)
    {
        point = origin;
        normal = Vector2.Zero;
        if (!GodotObject.IsInstanceValid(caster) || !caster.IsInsideTree() ||
            !GodotObject.IsInstanceValid(surface) || !surface.IsInsideTree() ||
            caster.GetTree() != surface.GetTree() || !Finite(origin) || !Finite(anchor) ||
            origin.IsEqualApprox(anchor)) return false;
        Godot.Collections.Dictionary hit = TraceLineOfSight(caster, origin, anchor, hitFromInside: true);
        if (hit.Count == 0 || !hit.ContainsKey("collider") || !hit.ContainsKey("position") ||
            !hit.ContainsKey("normal") || hit["collider"].AsGodotObject() is not Node collider ||
            ColliderBelongsToSurface(collider, surface)) return false;
        point = hit["position"].AsVector2();
        normal = hit["normal"].AsVector2();
        return Finite(point) && Finite(normal);
    }

    private static Godot.Collections.Dictionary TraceLineOfSight(Node2D caster, Vector2 origin,
        Vector2 anchor, bool hitFromInside = false)
    {
        using PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(origin, anchor);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.HitFromInside = hitFromInside;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
            CombatCollisionLayers.ThreadTarget;
        query.Exclude = CollisionRidsInSubtree(caster);
        return caster.GetWorld2D().DirectSpaceState.IntersectRay(query);
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

    public static bool ColliderBelongsToSurface(Node collider, GrappleSurface2D surface)
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
        // Read the live subtree for every query. Indexed access preserves the same
        // non-internal child preorder without allocating one native array per node.
        for (int index = 0; index < node.GetChildCount(); index++)
            AddCollisionRids(node.GetChild(index), rids);
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
