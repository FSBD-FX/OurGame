using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Vfx;

namespace Knight.Presentation;

public static class RoomProtectionRegionAdapter
{
    public readonly record struct Candidate(Node Node, Node2D CoordinateRoot);

    private static readonly Vector2 PlayerSize = new(80f, 124f);
    private static readonly Vector2 EnemySize = new(104f, 136f);
    private static readonly Vector2 TargetSize = new(64f, 64f);
    private static readonly StringName PlayerGroup = new("player");

    public static IReadOnlyList<Rect2> Collect(RoomFormalVisual art)
    {
        var regions = new List<Rect2>();
        CollectInto(art, regions);
        return regions;
    }

    public static IReadOnlyList<Rect2> CollectGameplay(Node2D sector, Node playerRoot)
    {
        var regions = new List<Rect2>();
        if (sector?.IsInsideTree() == true) CollectNode(sector, sector, regions);
        if (playerRoot?.IsInsideTree() == true)
            CollectNode(playerRoot, playerRoot as Node2D, regions);
        return regions;
    }

    public static void CollectInto(RoomFormalVisual art, List<Rect2> regions)
    {
        if (regions == null) throw new System.ArgumentNullException(nameof(regions));
        regions.Clear();
        if (art == null || !art.IsInsideTree()) return;
        if (TryResolveLevelRoots(art, out Node currentRoom, out Node playerRoot))
        {
            CollectNode(currentRoom, currentRoom as Node2D, regions);
            CollectNode(playerRoot, playerRoot as Node2D, regions);
        }
        else
        {
            Node scope = art.GetParent() ?? art;
            CollectNode(scope, scope as Node2D, regions);
        }
    }

    public static void CollectCandidatesInto(RoomFormalVisual art, List<Candidate> candidates)
    {
        if (candidates == null) throw new System.ArgumentNullException(nameof(candidates));
        candidates.Clear();
        if (art == null || !art.IsInsideTree()) return;
        if (TryResolveLevelRoots(art, out Node currentRoom, out Node playerRoot))
        {
            CollectCandidateNodes(currentRoom, currentRoom as Node2D, candidates);
            CollectCandidateNodes(playerRoot, playerRoot as Node2D, candidates);
        }
        else
        {
            Node scope = art.GetParent() ?? art;
            CollectCandidateNodes(scope, scope as Node2D, candidates);
        }
    }

    public static bool IsCandidateNode(Node node) =>
        node is CanvasItem &&
        (node is CollisionObject2D || node is ThreadTarget2D ||
         node is ThreadLineVisual || node is ThreadShuttleVisual || node is AttackTelegraphVisual ||
         node is GrappleSurface2D || node is FallenBellkeeperFormalPart ||
         node.IsInGroup(PlayerGroup));

    public static void CollectFromCandidatesInto(IReadOnlyList<Candidate> candidates,
        List<Rect2> regions)
    {
        if (candidates == null) throw new System.ArgumentNullException(nameof(candidates));
        if (regions == null) throw new System.ArgumentNullException(nameof(regions));
        regions.Clear();
        for (int index = 0; index < candidates.Count; index++)
        {
            Candidate candidate = candidates[index];
            if (!GodotObject.IsInstanceValid(candidate.Node) || !candidate.Node.IsInsideTree()) continue;
            CollectCandidate(candidate.Node, candidate.CoordinateRoot, regions);
        }
    }

    private static bool TryResolveLevelRoots(RoomFormalVisual art, out Node currentRoom,
        out Node playerRoot)
    {
        Node roomBranch = art;
        for (Node parent = art.GetParent(); parent != null; parent = parent.GetParent())
        {
            if (parent.Name == "RoomRoot")
            {
                Node levelRoot = parent.GetParent();
                Node siblingPlayerRoot = levelRoot?.GetNodeOrNull("PlayerRoot");
                if (siblingPlayerRoot != null)
                {
                    currentRoom = roomBranch;
                    playerRoot = siblingPlayerRoot;
                    return true;
                }
            }
            roomBranch = parent;
        }
        currentRoom = null;
        playerRoot = null;
        return false;
    }

    private static void CollectNode(Node node, Node2D coordinateRoot, List<Rect2> regions)
    {
        CollectCandidate(node, coordinateRoot, regions);

        int childCount = node.GetChildCount();
        for (int index = 0; index < childCount; index++)
            CollectNode(node.GetChild(index), coordinateRoot, regions);
    }

    private static void CollectCandidateNodes(Node node, Node2D coordinateRoot,
        List<Candidate> candidates)
    {
        if (IsCandidateNode(node))
            candidates.Add(new Candidate(node, coordinateRoot));
        int childCount = node.GetChildCount();
        for (int index = 0; index < childCount; index++)
            CollectCandidateNodes(node.GetChild(index), coordinateRoot, candidates);
    }

    private static void CollectCandidate(Node node, Node2D coordinateRoot, List<Rect2> regions)
    {
        if (node is not CanvasItem canvas || !canvas.IsVisibleInTree()) return;
        if (node.IsInGroup(PlayerGroup)) AddCentered(canvas, PlayerSize, regions);

        if (node is CollisionObject2D collision)
        {
            uint layer = collision.CollisionLayer;
            if (collision is StaticBody2D && (layer & CombatCollisionLayers.World) != 0 &&
                collision.GetMeta("catch_shelf", false).AsBool())
                AddLandingTopBands(collision, regions);
            if ((layer & CombatCollisionLayers.Enemy) != 0)
                AddCollisionOrCentered(collision,
                    node is FallenBellkeeper ? new Vector2(126f, 154f) : EnemySize, regions);
            if ((layer & CombatCollisionLayers.ThreadTarget) != 0 &&
                collision.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile")?.UseCollisionHitPoint != true)
                AddCollisionOrCentered(collision, TargetSize, regions);
            bool isAttackLayer =
                (layer & (CombatCollisionLayers.EnemyAttack | CombatCollisionLayers.PlayerAttack)) != 0;
            if (isAttackLayer && (collision is not Area2D attackArea || attackArea.Monitoring))
                AddCollisionOrCentered(collision, new Vector2(128f, 72f), regions);
        }

        if (node is ThreadTarget2D && node.IsInGroup("thread_targets"))
            AddCentered(canvas, TargetSize, regions);
        if (node is GrappleSurface2D surface && surface.IsAvailable &&
            surface.Material == GrappleSurfaceMaterial.DedicatedAnchor)
        {
            Vector2 anchor = surface.GetCanvasTransform() * surface.WorldAnchor;
            regions.Add(new Rect2(anchor - TargetSize * .5f, TargetSize));
        }
        // The raised hammer extends far outside the Boss collision body. Protect the
        // actual drawn part in its current animated transform, including its outline.
        if (node is FallenBellkeeperFormalPart { Kind: FallenBellkeeperPartKind.Hammer })
        {
            Transform2D transform = canvas.GetGlobalTransformWithCanvas();
            transform.Origin = transform * new Vector2(34, -12);
            regions.Add(TransformedBounds(transform, new Vector2(54, 54)));
        }
        // DrawArmor's outer Ink polygon spans (-78, -66) through (73, 61).
        // Its exposed pose lifts outside the body; use the visible part's live canvas transform.
        if (node is FallenBellkeeperFormalPart { Kind: FallenBellkeeperPartKind.ArmorShell })
        {
            Transform2D transform = canvas.GetGlobalTransformWithCanvas();
            transform.Origin = transform * new Vector2(-2.5f, -2.5f);
            regions.Add(TransformedBounds(transform, new Vector2(75.5f, 63.5f)));
        }
        if (node is ThreadLineVisual line && line.CurrentState != ThreadLineState.Idle)
            AddThreadLine(line, regions);
        if (node is ThreadShuttleVisual shuttle)
        {
            Vector2 point = shuttle.GetCanvasTransform() * shuttle.ShuttleWorldPosition;
            regions.Add(new Rect2(point - Vector2.One * 24, Vector2.One * 48));
        }
        if (node is AttackTelegraphVisual telegraph &&
            telegraph.CurrentFrame.Timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active)
            AddAttackShape(telegraph, coordinateRoot, regions);
    }

    private static void AddCentered(CanvasItem item, Vector2 size, List<Rect2> regions)
    {
        Vector2 center = item.GetGlobalTransformWithCanvas().Origin;
        regions.Add(new Rect2(center - size * .5f, size));
    }

    private static void AddLandingTopBands(CollisionObject2D collision, List<Rect2> regions)
    {
        // CanyonContentBuilder explicitly marks safe catch shelves. Protect their
        // landing lip, not entire terrain bodies (which would exclude walls/roofs
        // from nearly every possible card placement). Shapes are authored direct children.
        foreach (Node child in collision.GetChildren())
        {
            if (child is not CollisionShape2D { Disabled: false, Shape: RectangleShape2D rectangle } shape)
                continue;
            Transform2D transform = shape.GetGlobalTransformWithCanvas();
            transform.Origin = transform * new Vector2(0, -rectangle.Size.Y * .5f);
            regions.Add(TransformedBounds(transform, new Vector2(rectangle.Size.X * .5f, 6f)));
        }
    }

    private static void AddCollisionOrCentered(CollisionObject2D collision, Vector2 fallbackSize,
        List<Rect2> regions)
    {
        bool added = false;
        bool hasAuthoredShape = false;
        CollectCollisionShapes(collision, regions, ref hasAuthoredShape, ref added);
        if (!added && !hasAuthoredShape) AddCentered(collision, fallbackSize, regions);
    }

    private static void CollectCollisionShapes(Node node, List<Rect2> regions,
        ref bool hasAuthoredShape, ref bool added)
    {
        int childCount = node.GetChildCount();
        for (int index = 0; index < childCount; index++)
        {
            Node child = node.GetChild(index);
            if (child is CollisionShape2D shape && shape.Shape != null)
            {
                hasAuthoredShape = true;
                if (!shape.Disabled)
                {
                    Vector2 halfExtents = shape.Shape switch
                    {
                        RectangleShape2D rectangle => rectangle.Size * .5f,
                        CircleShape2D circle => Vector2.One * circle.Radius,
                        CapsuleShape2D capsule => new Vector2(capsule.Radius, capsule.Height * .5f),
                        _ => Vector2.Zero
                    };
                    if (halfExtents != Vector2.Zero)
                    {
                        regions.Add(TransformedBounds(shape.GetGlobalTransformWithCanvas(), halfExtents));
                        added = true;
                    }
                }
            }
            CollectCollisionShapes(child, regions, ref hasAuthoredShape, ref added);
        }
    }

    private static void AddThreadLine(ThreadLineVisual line, List<Rect2> regions)
    {
        Vector2[] points = line.GetStrandPoints(0);
        if (points.Length < 2) return;
        Transform2D transform = line.GetGlobalTransformWithCanvas();
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vector2 local in points)
        {
            Vector2 point = transform * local;
            minX = Mathf.Min(minX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxX = Mathf.Max(maxX, point.X);
            maxY = Mathf.Max(maxY, point.Y);
        }
        const float padding = 18f;
        regions.Add(new Rect2(minX - padding, minY - padding,
            maxX - minX + padding * 2f, maxY - minY + padding * 2f));
    }

    private static void AddAttackShape(AttackTelegraphVisual telegraph, Node2D coordinateRoot,
        List<Rect2> regions)
    {
        AttackShapeFrame shape = telegraph.CurrentFrame.Shape;
        if (coordinateRoot == null || shape.Kind == AttackShapeKind.None) return;
        // Frames already describe world geometry. Preserve rotation and actual
        // polygon vertices; a horizontal capsule must not protect a tall box.
        Rect2 bounds = AttackShapeBounds.WorldBounds(shape).Grow(16f);
        Transform2D canvas = coordinateRoot.GetCanvasTransform();
        canvas.Origin = canvas * bounds.GetCenter();
        regions.Add(TransformedBounds(canvas, bounds.Size * .5f));
    }

    private static Rect2 TransformedBounds(Transform2D transform, Vector2 halfExtents)
    {
        Vector2 topLeft = transform * new Vector2(-halfExtents.X, -halfExtents.Y);
        Vector2 topRight = transform * new Vector2(halfExtents.X, -halfExtents.Y);
        Vector2 bottomRight = transform * new Vector2(halfExtents.X, halfExtents.Y);
        Vector2 bottomLeft = transform * new Vector2(-halfExtents.X, halfExtents.Y);
        float minX = Mathf.Min(Mathf.Min(topLeft.X, topRight.X),
            Mathf.Min(bottomRight.X, bottomLeft.X));
        float minY = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y),
            Mathf.Min(bottomRight.Y, bottomLeft.Y));
        float maxX = Mathf.Max(Mathf.Max(topLeft.X, topRight.X),
            Mathf.Max(bottomRight.X, bottomLeft.X));
        float maxY = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y),
            Mathf.Max(bottomRight.Y, bottomLeft.Y));
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
}
