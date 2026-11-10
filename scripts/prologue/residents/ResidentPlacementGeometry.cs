using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Combat;
using Knight.Prologue.Traversal;

namespace Knight.Prologue.Residents;

public enum ResidentKeepOutKind { Hazard, MovingOccupancy, Protected, TraversalLanding, FragmentApproach, OriginalEnemy, ConditionalSolid, TraversalLaunch, SupplyApproach, PlayerInteraction }
public sealed record ResidentKeepOut(string SourceId, ResidentKeepOutKind Kind, Rect2 WorldBounds);
public sealed record ResidentSupportWitness(ulong ColliderId, ulong ShapeId, Rect2 WorldBounds);
// Captured by the manager from its authorized player for this admission only.
// Scalar identities label rejections; they never exclude a physics hit.
public readonly record struct ResidentPlayerCollisionIdentity(ulong BodyId, ulong BodyShapeId,
    ulong HurtboxId, ulong HurtboxShapeId);

// A single-body geometry result is NOT a patrol/attack/phase/scene certificate.
public sealed class ResidentBodyGeometryResult
{
    public Rect2 WorldBodyBounds { get; }
    public Transform2D ColliderWorldTransform { get; }
    public IReadOnlyList<string> Issues { get; }
    public IReadOnlyList<ResidentSupportWitness> Support { get; }
    public bool BodyGeometryClear => Issues.Count == 0;
    public bool IsCompletePlacementProof => false;

    public ResidentBodyGeometryResult(Rect2 bounds, Transform2D transform,
        IEnumerable<string> issues, IEnumerable<ResidentSupportWitness> support)
    {
        WorldBodyBounds = bounds; ColliderWorldTransform = transform;
        // Native hit enumeration is not a stable diagnostic ordering contract.
        Issues = Array.AsReadOnly(issues.OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Support = Array.AsReadOnly(support.ToArray());
    }
}

// Conservative fixed-pose translation occupancy, never an AI/motion certificate.
public sealed class ResidentPathGeometryResult
{
    public Rect2 WorldCorridorBounds { get; }
    public IReadOnlyList<string> Issues { get; }
    public bool CorridorClear => Issues.Count == 0;
    public bool IsCompletePlacementProof => false;

    public ResidentPathGeometryResult(Rect2 bounds,IEnumerable<string> issues)
    {
        WorldCorridorBounds=bounds;
        Issues=Array.AsReadOnly(issues.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }
}

// Pure values from one synchronous admission, never a cached native scene,
// actor, shape or a certificate for future motion/activation.
internal sealed class ResidentPatrolGeometryResult
{
    public ResidentBodyGeometryResult Body { get; }
    public IReadOnlyList<ResidentPathGeometryResult> Corridors { get; }
    public int BodyCheckCount { get; }

    internal ResidentPatrolGeometryResult(ResidentBodyGeometryResult body,
        IEnumerable<ResidentPathGeometryResult> corridors, int bodyCheckCount)
    {
        Body = body;
        Corridors = Array.AsReadOnly(corridors.ToArray());
        BodyCheckCount = bodyCheckCount;
    }
}

public static class ResidentPlacementGeometry
{
    private const int QueryCapacity = 64;
    private const float SupportTolerance = 1.25f;
    private const float GeometryTolerance = .001f;

    internal static ResidentPatrolGeometryResult CheckPatrol(CharacterBody2D actor,
        Transform2D intended, Vector2 worldSpawn, IReadOnlyList<Vector2> patrolPoints,
        PhysicsDirectSpaceState2D space, Node2D worldRoot, Rect2 permittedBodyBounds,
        bool requireGroundSupport, IReadOnlyList<ResidentKeepOut> keepOut,
        ResidentPlayerCollisionIdentity? currentPlayer = null)
    {
        ArgumentNullException.ThrowIfNull(patrolPoints);
        // Authored patrol points and this admission context are fixed here:
        // no await, activation, mounting or script callback crosses this table.
        // A later admission (including post-Ready) always starts a NEW table.
        var bodies = new Dictionary<Transform2D, ResidentBodyGeometryResult>();
        int bodyChecks = 0;
        ResidentBodyGeometryResult BodyAt(Transform2D transform)
        {
            if (!bodies.TryGetValue(transform, out var result))
            {
                bodyChecks++;
                result = CheckBody(actor, transform, space, worldRoot,
                    permittedBodyBounds, requireGroundSupport, keepOut, currentPlayer);
                bodies.Add(transform, result);
            }
            return result;
        }
        ResidentBodyGeometryResult body = BodyAt(intended);
        var corridors = new List<ResidentPathGeometryResult>();
        Vector2 previous = worldSpawn;
        foreach (Vector2 point in patrolPoints.Append(worldSpawn))
        {
            Transform2D from = new(0, previous), to = new(0, point);
            ResidentBodyGeometryResult start = BodyAt(from), end = BodyAt(to);
            // Zero-length and return segments retain their own full corridor
            // sweep/support/electric checks. Only repeated endpoints are reused.
            corridors.Add(CheckCorridorCore(actor, from, to, space, worldRoot,
                permittedBodyBounds, requireGroundSupport, keepOut, currentPlayer, start, end));
            previous = point;
        }
        return new(body, corridors, bodyChecks);
    }

    public static ResidentPathGeometryResult CheckCorridor(CharacterBody2D actor,
        Transform2D from,Transform2D to,PhysicsDirectSpaceState2D space,Node2D worldRoot,
        Rect2 permittedBodyBounds,bool requireGroundSupport,IReadOnlyList<ResidentKeepOut> keepOut,
        ResidentPlayerCollisionIdentity? currentPlayer = null)
    {
        ResidentBodyGeometryResult start=CheckBody(actor,from,space,worldRoot,permittedBodyBounds,requireGroundSupport,keepOut,currentPlayer);
        ResidentBodyGeometryResult end=CheckBody(actor,to,space,worldRoot,permittedBodyBounds,requireGroundSupport,keepOut,currentPlayer);
        return CheckCorridorCore(actor,from,to,space,worldRoot,permittedBodyBounds,
            requireGroundSupport,keepOut,currentPlayer,start,end);
    }

    private static ResidentPathGeometryResult CheckCorridorCore(CharacterBody2D actor,
        Transform2D from,Transform2D to,PhysicsDirectSpaceState2D space,Node2D worldRoot,
        Rect2 permittedBodyBounds,bool requireGroundSupport,IReadOnlyList<ResidentKeepOut> keepOut,
        ResidentPlayerCollisionIdentity? currentPlayer,ResidentBodyGeometryResult start,ResidentBodyGeometryResult end)
    {
        var issues=new List<string>();
        issues.AddRange(start.Issues.Select(x=>"start."+x));
        issues.AddRange(end.Issues.Select(x=>"end."+x));
        // A protected player can defer a birth, but is not proof that the
        // intervening corridor is otherwise clear. Finish this same query
        // path for player-only endpoints, retaining their original issues.
        if(issues.Count>0 && !(currentPlayer.HasValue && issues.All(IsCurrentPlayerIssue)))
            return new(default,issues);
        // Endpoint union contains the whole translation only when the entire
        // collider basis is fixed. Rotation/scale/skew sweeps are not certified.
        if(from.X!=to.X||from.Y!=to.Y) return new(default,new[]{"unsupported_path_basis"});
        if(requireGroundSupport&&from.Origin.Y!=to.Origin.Y)
            return new(default,new[]{"unsupported_ground_path"});
        Rect2 corridor=start.WorldBodyBounds.Merge(end.WorldBodyBounds);
        foreach(ResidentKeepOut region in keepOut)
            if(region.WorldBounds.Intersects(corridor)) issues.Add($"corridor_reserved.{region.Kind}:{region.SourceId}");

        var self=new Godot.Collections.Array<Rid>();
        using var selfOwner=(Godot.Collections.Array)self;
        foreach(CollisionObject2D body in Walk(actor).OfType<CollisionObject2D>()) self.Add(body.GetRid());
        List<ResidentSupportWitness> support=requireGroundSupport
            ? FindSupports(space,worldRoot,corridor,self,issues,"corridor_support_query_truncated","corridor_incomplete_support")
            : new();
        // This AABB deliberately over-approximates diagonals and curved shapes.
        // A rejection can be conservative; a clear result still is not motion.
        using var volume=new RectangleShape2D {Size=corridor.Size};
        Transform2D pose=new(0,corridor.GetCenter());
        foreach(NativeHit hit in Query(space,volume,pose,
            CombatCollisionLayers.World|CombatCollisionLayers.Enemy|CombatCollisionLayers.Player|
            CombatCollisionLayers.EnemyAttack,true,self,issues,"corridor_query_truncated"))
        {
            bool legalFloorContact=!hit.Area&&support.Any(x=>x.ColliderId==hit.ColliderId&&x.ShapeId==hit.ShapeId&&
                corridor.End.Y<=x.WorldBounds.Position.Y+GeometryTolerance);
            if(legalFloorContact) continue;
            string reason=PlayerHitReason(hit,currentPlayer,"corridor_") ?? (hit.Area
                ? (hit.Layer&CombatCollisionLayers.EnemyAttack)!=0?"corridor_native_hazard":"corridor_native_area"
                : (hit.Layer&CombatCollisionLayers.World)!=0?"corridor_native_world":"corridor_native_actor");
            issues.Add(reason);
        }
        CheckElectric(worldRoot,volume,pose,issues,"corridor_native_hazard","corridor_unsupported_hazard_transform");
        return new(corridor,issues);
    }

    public static ResidentBodyGeometryResult CheckBody(CharacterBody2D actor,
        Transform2D actorWorldTransform, PhysicsDirectSpaceState2D space, Node2D worldRoot,
        Rect2 permittedBodyBounds, bool requireGroundSupport, IReadOnlyList<ResidentKeepOut> keepOut,
        ResidentPlayerCollisionIdentity? currentPlayer = null)
    {
        var issues = new List<string>();
        var support = new List<ResidentSupportWitness>();
        Rect2 bounds = default;
        Transform2D pose = Transform2D.Identity;
        if (!GodotObject.IsInstanceValid(actor)) return Failure("missing_actor");
        if (!ValidPose(actorWorldTransform)) return Failure("invalid_transform");
        if (!GodotObject.IsInstanceValid(space) || !GodotObject.IsInstanceValid(worldRoot) ||
            !worldRoot.IsInsideTree() || worldRoot.GetWorld2D().DirectSpaceState != space)
            return Failure("invalid_native_world");
        if (!FinitePositive(permittedBodyBounds)) return Failure("invalid_bounds");
        if (keepOut == null) return Failure("missing_keep_out_inventory");

        Node[] nodes = Walk(actor).ToArray();
        CollisionShape2D[] physical = nodes.OfType<CollisionShape2D>()
            .Where(x => NearestBody(x) == actor).ToArray();
        if (physical.Length != 1 || nodes.OfType<CollisionPolygon2D>().Any(x => NearestBody(x) == actor))
            return Failure("multiple_body_shapes");
        CollisionShape2D collider = physical[0];
        if (collider.Name != "BodyCollider") return Failure("missing_body_collider");
        if (collider.Disabled) return Failure("disabled_body");
        if (collider.OneWayCollision || collider.Shape is not (CircleShape2D or CapsuleShape2D or RectangleShape2D))
            return Failure("unsupported_shape");
        if (!TryColliderPose(actor, collider, actorWorldTransform, out pose)) return Failure("unsupported_transform");
        Rect2 local = collider.Shape.GetRect();
        if (!FinitePositive(local)) return Failure("invalid_shape");
        bounds = pose * local;
        if (!FinitePositive(bounds)) return Failure("invalid_shape");
        if (!permittedBodyBounds.Encloses(bounds)) issues.Add("body_outside_bounds");
        foreach (ResidentKeepOut region in keepOut)
        {
            if (region == null || string.IsNullOrWhiteSpace(region.SourceId) ||
                !Enum.IsDefined(region.Kind) || !FinitePositive(region.WorldBounds))
                issues.Add("invalid_keep_out");
            else if (region.WorldBounds.Intersects(bounds)) issues.Add($"reserved.{region.Kind}:{region.SourceId}");
        }

        // Only self-owned physics objects are excluded, never whole floor RIDs.
        var self = new Godot.Collections.Array<Rid>();
        using var selfOwner = (Godot.Collections.Array)self;
        foreach (CollisionObject2D body in nodes.OfType<CollisionObject2D>()) self.Add(body.GetRid());
        if (requireGroundSupport)
            support=FindSupports(space,worldRoot,bounds,self,issues,"support_query_truncated","incomplete_support");

        List<NativeHit> overlaps = Query(space, collider.Shape, pose,
            CombatCollisionLayers.World | CombatCollisionLayers.Enemy | CombatCollisionLayers.Player |
            CombatCollisionLayers.EnemyAttack, true, self, issues, "query_truncated");
        foreach (NativeHit hit in overlaps)
        {
            // Exact tangency to the actual supporting SHAPE is legal. A second
            // wall shape on the same body is not accidentally excluded.
            bool legalFloorContact = !hit.Area && support.Any(x => x.ColliderId == hit.ColliderId &&
                x.ShapeId == hit.ShapeId && bounds.End.Y <= x.WorldBounds.Position.Y + GeometryTolerance);
            if (legalFloorContact) continue;
            string reason = PlayerHitReason(hit,currentPlayer,"") ?? (hit.Area
                ? (hit.Layer & CombatCollisionLayers.EnemyAttack) != 0 ? "native_hazard" : "native_area"
                : (hit.Layer & CombatCollisionLayers.World) != 0 ? "native_world" : "native_actor");
            issues.Add(reason);
        }
        // The saved electric lane deliberately has collision_layer=0 and
        // observes Player bodies via its MASK. No IntersectShape query mask
        // can discover a layer-zero Area. Test its real enabled shape directly,
        // without changing gameplay layers or pretending pickups are hazards.
        CheckElectric(worldRoot,collider.Shape,pose,issues,"native_hazard","unsupported_hazard_transform");
        return new ResidentBodyGeometryResult(bounds, pose, issues.Distinct(StringComparer.Ordinal), support);

        ResidentBodyGeometryResult Failure(string reason) =>
            new(bounds, pose, new[] { reason }, support);
    }

    private static void CheckElectric(Node2D worldRoot,Shape2D shape,Transform2D pose,List<string> issues,
        string collisionReason,string invalidTransformReason)
    {
        foreach (CanyonShieldLaneHazard lane in Walk(worldRoot).OfType<CanyonShieldLaneHazard>()
            .Where(x => x.Monitoring && x.Damage > 0))
        foreach (CollisionShape2D hazard in Walk(lane).OfType<CollisionShape2D>()
            .Where(x => NearestBody(x) == lane && !x.Disabled && x.Shape != null))
        {
            if (!ValidPose(hazard.GlobalTransform)) issues.Add(invalidTransformReason);
            else if (shape.Collide(pose, hazard.Shape, hazard.GlobalTransform)) issues.Add(collisionReason);
        }
    }

    private static List<ResidentSupportWitness> FindSupports(PhysicsDirectSpaceState2D space,Node2D worldRoot,
        Rect2 bounds,Godot.Collections.Array<Rid> self,List<string> issues,string truncatedReason,string incompleteReason)
    {
        var support=new List<ResidentSupportWitness>();
        using var footStrip=new RectangleShape2D {Size=new Vector2(bounds.Size.X,SupportTolerance*2)};
        List<NativeHit> nativeFeet=Query(space,footStrip,new Transform2D(0,new Vector2(bounds.GetCenter().X,bounds.End.Y)),
            CombatCollisionLayers.World,false,self,issues,truncatedReason);
        var seen=nativeFeet.Select(x=>(x.ColliderId,x.ShapeId)).ToHashSet();
        foreach(StaticBody2D floor in Walk(worldRoot).OfType<StaticBody2D>())
        {
            if((floor.CollisionLayer&CombatCollisionLayers.World)==0) continue;
            foreach(CollisionShape2D shape in Walk(floor).OfType<CollisionShape2D>())
            {
                if(NearestBody(shape)!=floor||shape.Disabled||shape.OneWayCollision||shape.Shape is not RectangleShape2D||
                    !seen.Contains((floor.GetInstanceId(),shape.GetInstanceId()))) continue;
                Transform2D transform=shape.GlobalTransform;
                // No invented horizontal support from rotated, sloped or
                // mirrored floors; these remain native collision obstacles.
                if(!ValidPose(transform)||MathF.Abs(transform.X.Y)>GeometryTolerance||MathF.Abs(transform.Y.X)>GeometryTolerance||
                    transform.X.X<=0||transform.Y.Y<=0) continue;
                Rect2 floorBounds=transform*shape.Shape.GetRect();
                if(!FinitePositive(floorBounds)||MathF.Abs(floorBounds.Position.Y-bounds.End.Y)>SupportTolerance) continue;
                support.Add(new(floor.GetInstanceId(),shape.GetInstanceId(),floorBounds));
            }
        }
        float cursor=bounds.Position.X;
        foreach(ResidentSupportWitness witness in support.OrderBy(x=>x.WorldBounds.Position.X))
        {
            if(witness.WorldBounds.End.X<cursor-GeometryTolerance) continue;
            if(witness.WorldBounds.Position.X>cursor+GeometryTolerance) break;
            cursor=MathF.Max(cursor,witness.WorldBounds.End.X);
        }
        if(cursor<bounds.End.X-GeometryTolerance) issues.Add(incompleteReason);
        return support;
    }

    private readonly record struct NativeHit(ulong ColliderId, ulong ShapeId, uint Layer, bool Area);

    private static string PlayerHitReason(NativeHit hit, ResidentPlayerCollisionIdentity? identity, string prefix)
    {
        if (identity is not { } player || hit.Layer != CombatCollisionLayers.Player || hit.ShapeId == 0) return null;
        if (!hit.Area && hit.ColliderId == player.BodyId && hit.ShapeId == player.BodyShapeId)
            return prefix + "native_current_player_body";
        if (hit.Area && hit.ColliderId == player.HurtboxId && hit.ShapeId == player.HurtboxShapeId)
            return prefix + "native_current_player_hurtbox";
        return null;
    }

    internal static bool IsCurrentPlayerIssue(string issue) => issue is
        "reserved.Protected:current_player" or "start.reserved.Protected:current_player" or
        "end.reserved.Protected:current_player" or "corridor_reserved.Protected:current_player" or
        "native_current_player_body" or "native_current_player_hurtbox" or
        "start.native_current_player_body" or "start.native_current_player_hurtbox" or
        "end.native_current_player_body" or "end.native_current_player_hurtbox" or
        "corridor_native_current_player_body" or "corridor_native_current_player_hurtbox";

    private static List<NativeHit> Query(PhysicsDirectSpaceState2D space, Shape2D shape,
        Transform2D transform, uint mask, bool areas, Godot.Collections.Array<Rid> excluded,
        List<string> issues, string capacityReason)
    {
        using var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape, Transform = transform, CollisionMask = mask, Exclude = excluded,
            CollideWithBodies = true, CollideWithAreas = areas, Margin = 0
        };
        using Godot.Collections.Array hits = (Godot.Collections.Array)space.IntersectShape(query, QueryCapacity);
        if (hits.Count >= QueryCapacity) issues.Add(capacityReason);
        var result = new List<NativeHit>();
        for (int i = 0; i < hits.Count; i++)
        {
            using Godot.Collections.Dictionary hit = hits[i].AsGodotDictionary();
            if (hit["collider"].AsGodotObject() is not CollisionObject2D body)
            {
                issues.Add("invalid_native_hit"); continue;
            }
            // IntersectShape ignores Area.Monitoring. Ordinary attack Hitboxes
            // keep their native shape/layer during telegraph and End; only the
            // current automatic damage phase is a native placement obstacle.
            // Unknown Areas/subclasses/mixed layers remain conservative. This
            // does not alter explicit Hitbox.TryConnect or future hazard bounds.
            if (body is Hitbox attack && attack.GetType() == typeof(Hitbox) &&
                attack.CollisionLayer == CombatCollisionLayers.EnemyAttack && !attack.Monitoring) continue;
            int shapeIndex = hit["shape"].AsInt32();
            GodotObject owner = body.ShapeOwnerGetOwner(body.ShapeFindOwner(shapeIndex));
            result.Add(new NativeHit(body.GetInstanceId(), owner?.GetInstanceId() ?? 0, body.CollisionLayer, body is Area2D));
        }
        return result;
    }

    private static CollisionObject2D NearestBody(Node shape)
    {
        for (Node node = shape.GetParent(); node != null; node = node.GetParent())
            if (node is CollisionObject2D body) return body;
        return null;
    }

    private static bool TryColliderPose(CharacterBody2D actor, CollisionShape2D collider,
        Transform2D actorWorld, out Transform2D pose)
    {
        pose = Transform2D.Identity;
        if (actor.TopLevel) return false;
        for (Node node = collider; node != actor; node = node.GetParent())
        {
            if (node is not Node2D item || item.TopLevel || !ValidPose(item.Transform)) return false;
            pose = item.Transform * pose;
        }
        pose = actorWorld * pose;
        return ValidPose(pose);
    }

    private static bool ValidPose(Transform2D pose) => pose.IsFinite() &&
        float.IsFinite(pose.Determinant()) && MathF.Abs(pose.Determinant()) > .000001f;
    private static bool FinitePositive(Rect2 rect) => rect.Position.IsFinite() && rect.Size.IsFinite() &&
        rect.Size.X > 0 && rect.Size.Y > 0;

    private static IEnumerable<Node> Walk(Node root)
    {
        yield return root;
        using Godot.Collections.Array children = (Godot.Collections.Array)root.GetChildren();
        for (int i = 0; i < children.Count; i++)
            foreach (Node node in Walk(children[i].AsGodotObject() as Node)) yield return node;
    }
}
