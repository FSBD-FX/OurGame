using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Enemies.Ranged;

public readonly record struct RangedSweepResult(bool WorldHit, Hurtbox Target, float SafeFraction, bool StartedInWorld);

public static class RangedAttackGeometry
{
    private const int MaximumNativeHits = 256;
    private const float ContactTieTolerance = .00001f;
    private static readonly RangedSweepResult ClosedSweep = new(true, null, 0f, true);

    public static Vector2[] ScatterDirections(Vector2 aim, int pelletCount, float halfAngleDegrees)
    {
        if (pelletCount <= 0 || !float.IsFinite(halfAngleDegrees) || halfAngleDegrees < 0f ||
            !TryDirection(aim, out Vector2 direction)) return [];
        var result = new Vector2[pelletCount];
        for (int i = 0; i < result.Length; i++)
        {
            double degrees = pelletCount == 1 ? 0d : -halfAngleDegrees + 2d * halfAngleDegrees * i / (pelletCount - 1d);
            double angle = degrees * (Math.PI / 180d);
            double cosine = Math.Cos(angle), sine = Math.Sin(angle);
            result[i] = new Vector2((float)(direction.X * cosine - direction.Y * sine),
                (float)(direction.X * sine + direction.Y * cosine));
        }
        return result;
    }

    public static AttackShapeFrame Beam(Vector2 origin, Vector2 end, float halfWidth)
    {
        if (!Finite(origin) || !Finite(end) || !Positive(halfWidth)) return EmptyShape();
        double x = (double)end.X - origin.X, y = (double)end.Y - origin.Y;
        double length = Math.Sqrt(x * x + y * y);
        double height = length * .5d + halfWidth;
        if (!Representable(height)) return EmptyShape();
        Vector2 center = new((float)(((double)origin.X + end.X) * .5d), (float)(((double)origin.Y + end.Y) * .5d));
        float rotation = (float)(Math.Atan2(y, x) - Math.PI * .5d);
        return new AttackShapeFrame(AttackShapeKind.Capsule, center,
            new Vector2(halfWidth, (float)height), rotation, Array.Empty<Vector2>());
    }

    public static RangedSweepResult Sweep(PhysicsDirectSpaceState2D space, Vector2 from, Vector2 motion,
        float radius, Godot.Collections.Array<Rid> exclude = null)
    {
        if (!Live(space) || !Finite(from) || !Finite(motion) || !Finite(from + motion) || !Positive(radius))
            return ClosedSweep;
        using var circle = new CircleShape2D { Radius = radius };
        using var world = ShapeQuery(circle, from, CombatCollisionLayers.World, bodies: true, exclude);
        if (AnyOverlap(space, world)) return ClosedSweep;
        world.Motion = motion;
        if (!CastFraction(space, world, out float worldFraction, out bool worldHit)) return ClosedSweep;

        using var player = ShapeQuery(circle, from, CombatCollisionLayers.Player, bodies: false, exclude);
        Hurtbox overlapping = FirstHurtbox(space, player, out bool complete);
        if (!complete) return ClosedSweep;
        if (overlapping != null)
            return worldHit && worldFraction <= ContactTieTolerance
                ? new RangedSweepResult(true, null, worldFraction, false)
                : new RangedSweepResult(false, overlapping, 0f, false);
        if (motion == Vector2.Zero)
            return new RangedSweepResult(worldHit, null, worldHit ? worldFraction : 1f, false);

        // Native CastMotion supplies fractions, not collider identity. Discover
        // the swept capsule's candidate RIDs first, then cast the same Circle
        // against each genuine Hurtbox with the other candidate RIDs excluded.
        // This never infers a target from an unsafe/end-position sample and
        // cannot let an unrelated Player-layer area hide a real Hurtbox.
        AttackShapeFrame sweepShape = Beam(from, from + motion, radius);
        double fullHeight = 2d * sweepShape.Extent.Y;
        if (sweepShape.Kind != AttackShapeKind.Capsule || !Representable(fullHeight)) return ClosedSweep;
        using var capsule = new CapsuleShape2D { Radius = radius, Height = (float)fullHeight };
        using var candidatesQuery = ShapeQuery(capsule, sweepShape.Origin,
            CombatCollisionLayers.Player, bodies: false, exclude);
        candidatesQuery.Transform = new Transform2D(sweepShape.RotationRadians, sweepShape.Origin);
        using Godot.Collections.Array candidateHits = (Godot.Collections.Array)space.IntersectShape(candidatesQuery, MaximumNativeHits);
        if (candidateHits.Count >= MaximumNativeHits) return ClosedSweep;
        var candidates = new List<Hurtbox>();
        var candidateIds = new HashSet<ulong>();
        var excludedIds = new HashSet<Rid>();
        var ownedExclude = new Godot.Collections.Array<Rid>();
        using Godot.Collections.Array excludeOwner = (Godot.Collections.Array)ownedExclude;
        if (exclude != null)
            foreach (Rid rid in exclude)
                if (excludedIds.Add(rid)) ownedExclude.Add(rid);
        for (int i = 0; i < candidateHits.Count; i++)
        {
            using Godot.Collections.Dictionary hit = candidateHits[i].AsGodotDictionary();
            Rid rid = hit["rid"].AsRid();
            if (excludedIds.Add(rid)) ownedExclude.Add(rid);
            if (hit["collider"].AsGodotObject() is Hurtbox target && LiveTarget(target) && candidateIds.Add(target.GetInstanceId()))
                candidates.Add(target);
        }

        float playerFraction = 1f;
        Hurtbox first = null;
        player.Motion = motion;
        foreach (Hurtbox target in candidates)
        {
            Rid rid = target.GetRid();
            ownedExclude.Remove(rid);
            player.Exclude = ownedExclude;
            bool reliable = CastFraction(space, player, out float fraction, out bool hit);
            ownedExclude.Add(rid);
            if (!reliable) return ClosedSweep;
            if (!hit) continue;
            if (first == null || fraction < playerFraction ||
                (fraction == playerFraction && target.GetInstanceId() < first.GetInstanceId()))
            {
                first = target;
                playerFraction = fraction;
            }
        }
        if (worldHit && (first == null || worldFraction <= playerFraction + ContactTieTolerance))
            return new RangedSweepResult(true, null, worldFraction, false);
        return new RangedSweepResult(false, first, first == null ? 1f : playerFraction, false);
    }

    public static Vector2 ClipBeam(PhysicsDirectSpaceState2D space, Vector2 origin, Vector2 direction,
        float centerDistance, float radius, Godot.Collections.Array<Rid> exclude = null)
    {
        if (!Live(space) || !Finite(origin) || !TryDirection(direction, out Vector2 aim) ||
            !float.IsFinite(centerDistance) || centerDistance < 0f || !Positive(radius)) return origin;
        Vector2 motion = aim * centerDistance;
        if (!Finite(motion) || !Finite(origin + motion)) return origin;
        using var circle = new CircleShape2D { Radius = radius };
        using var query = ShapeQuery(circle, origin, CombatCollisionLayers.World, bodies: true, exclude);
        if (AnyOverlap(space, query)) return origin;
        query.Motion = motion;
        return CastFraction(space, query, out float fraction, out _) ? origin + motion * fraction : origin;
    }

    public static Hurtbox[] ExplosionTargets(PhysicsDirectSpaceState2D space, Vector2 origin,
        float radius, Godot.Collections.Array<Rid> exclude = null)
    {
        if (!Live(space) || !Finite(origin) || !Positive(radius) || !HasClearWorldPath(space, origin, origin, exclude))
            return [];
        using var circle = new CircleShape2D { Radius = radius };
        using var query = ShapeQuery(circle, origin, CombatCollisionLayers.Player, bodies: false, exclude);
        using Godot.Collections.Array hits = (Godot.Collections.Array)space.IntersectShape(query, MaximumNativeHits);
        if (hits.Count >= MaximumNativeHits) return [];
        var targets = new List<Hurtbox>();
        var seen = new HashSet<ulong>();
        for (int i = 0; i < hits.Count; i++)
        {
            using Godot.Collections.Dictionary hit = hits[i].AsGodotDictionary();
            if (hit["collider"].AsGodotObject() is not Hurtbox target || !LiveTarget(target) ||
                !seen.Add(target.GetInstanceId()) || !HasClearWorldPath(space, origin, target.GlobalPosition, exclude)) continue;
            targets.Add(target);
        }
        targets.Sort((a, b) => a.GetInstanceId().CompareTo(b.GetInstanceId()));
        return targets.ToArray();
    }

    public static bool HasClearWorldPath(PhysicsDirectSpaceState2D space, Vector2 from, Vector2 to,
        Godot.Collections.Array<Rid> exclude = null)
    {
        if (!Live(space) || !Finite(from) || !Finite(to)) return false;
        using var point = new PhysicsPointQueryParameters2D
        {
            Position = from, CollisionMask = CombatCollisionLayers.World,
            CollideWithBodies = true, CollideWithAreas = false
        };
        if (exclude != null) point.Exclude = exclude;
        using (Godot.Collections.Array overlap = (Godot.Collections.Array)space.IntersectPoint(point, 1))
            if (overlap.Count > 0) return false;
        if (from == to) return true;
        using var ray = PhysicsRayQueryParameters2D.Create(from, to, CombatCollisionLayers.World, exclude);
        ray.CollideWithBodies = true;
        ray.CollideWithAreas = false;
        ray.HitFromInside = true;
        using Godot.Collections.Dictionary hit = space.IntersectRay(ray);
        return hit.Count == 0;
    }

    public static Vector2 ClipToArea(Vector2 from, Vector2 end, Rect2 area, float inset)
    {
        if (!Finite(from) || !Finite(end) || !Finite(area.Position) || !Finite(area.Size) ||
            area.Size.X <= 0f || area.Size.Y <= 0f || !float.IsFinite(inset) || inset < 0f) return from;
        double minX = (double)area.Position.X + inset, minY = (double)area.Position.Y + inset;
        double maxX = (double)area.Position.X + area.Size.X - inset;
        double maxY = (double)area.Position.Y + area.Size.Y - inset;
        if (!Representable(minX) || !Representable(minY) || !Representable(maxX) || !Representable(maxY) ||
            minX > maxX || minY > maxY || from.X < minX || from.X > maxX || from.Y < minY || from.Y > maxY)
            return from;
        double x = (double)end.X - from.X, y = (double)end.Y - from.Y;
        double fraction = 1d;
        if (x > 0d) fraction = Math.Min(fraction, (maxX - from.X) / x);
        else if (x < 0d) fraction = Math.Min(fraction, (minX - from.X) / x);
        if (y > 0d) fraction = Math.Min(fraction, (maxY - from.Y) / y);
        else if (y < 0d) fraction = Math.Min(fraction, (minY - from.Y) / y);
        return new Vector2((float)(from.X + x * fraction), (float)(from.Y + y * fraction));
    }

    private static PhysicsShapeQueryParameters2D ShapeQuery(Shape2D shape, Vector2 origin,
        uint layer, bool bodies, Godot.Collections.Array<Rid> exclude)
    {
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape, Transform = new Transform2D(0f, origin), CollisionMask = layer,
            CollideWithBodies = bodies, CollideWithAreas = !bodies, Margin = 0f
        };
        if (exclude != null) query.Exclude = exclude;
        return query;
    }

    private static bool AnyOverlap(PhysicsDirectSpaceState2D space, PhysicsShapeQueryParameters2D query)
    {
        using Godot.Collections.Array hits = (Godot.Collections.Array)space.IntersectShape(query, 1);
        return hits.Count > 0;
    }

    private static Hurtbox FirstHurtbox(PhysicsDirectSpaceState2D space,
        PhysicsShapeQueryParameters2D query, out bool complete)
    {
        using Godot.Collections.Array hits = (Godot.Collections.Array)space.IntersectShape(query, MaximumNativeHits);
        complete = hits.Count < MaximumNativeHits;
        Hurtbox first = null;
        for (int i = 0; i < hits.Count; i++)
        {
            using Godot.Collections.Dictionary hit = hits[i].AsGodotDictionary();
            if (hit["collider"].AsGodotObject() is Hurtbox target && LiveTarget(target) &&
                (first == null || target.GetInstanceId() < first.GetInstanceId())) first = target;
        }
        return first;
    }

    private static bool CastFraction(PhysicsDirectSpaceState2D space, PhysicsShapeQueryParameters2D query,
        out float fraction, out bool hit)
    {
        float[] result = space.CastMotion(query);
        fraction = 0f;
        hit = false;
        if (result == null || result.Length < 2 || !float.IsFinite(result[0]) || !float.IsFinite(result[1]) ||
            result[0] < 0f || result[0] > 1f || result[1] < result[0] || result[1] > 1f) return false;
        fraction = result[0];
        hit = fraction < 1f;
        return true;
    }

    private static bool TryDirection(Vector2 value, out Vector2 result)
    {
        result = Vector2.Zero;
        if (!Finite(value)) return false;
        double length = Math.Sqrt((double)value.X * value.X + (double)value.Y * value.Y);
        if (length <= 0d) return false;
        result = new Vector2((float)(value.X / length), (float)(value.Y / length));
        return true;
    }

    private static AttackShapeFrame EmptyShape() =>
        new(AttackShapeKind.None, Vector2.Zero, Vector2.Zero, 0f, Array.Empty<Vector2>());

    private static bool Live(PhysicsDirectSpaceState2D space) => space != null && GodotObject.IsInstanceValid(space);
    private static bool LiveTarget(Hurtbox target) => GodotObject.IsInstanceValid(target) &&
        target.IsInsideTree() && !target.IsQueuedForDeletion();
    private static bool Positive(float value) => float.IsFinite(value) && value > 0f;
    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private static bool Representable(double value) => double.IsFinite(value) && Math.Abs(value) <= float.MaxValue;
}
