using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Guidance;

public partial class CanyonFireflyGuidanceController : Node2D
{
    private const float RouteProgressDistance = 96f;

    [Export] public NodePath LevelPath { get; set; } = new("../..");
    [Export] public NodePath WorldPath { get; set; } = new("../Sectors");
    [Export] public NodePath FlowPath { get; set; } = new("../../GameFlow");
    [Export] public NodePath GuidancePath { get; set; } = new("../../Guidance");
    [Export] public NodePath ViewPath { get; set; } = new("View");

    public bool IsLayoutActive { get; private set; }
    public CanyonFireflyFrame CurrentFrame => _model.CurrentFrame;
    public Marker2D CurrentMarker { get; private set; }
    public bool CombatTelegraphActive { get; private set; }

    private readonly CanyonFireflyGuidanceModel _model = new();
    private PrologueLevelController _level;
    private SeamlessPrologueWorld _world;
    private PrologueGameFlowController _flow;
    private PrologueGuidanceController _guidance;
    private CanyonFireflyGuidanceView _view;
    private CanyonRouteNodeId? _lastRouteNode;
    private Vector2 _lastProgressPosition;
    private bool _hasProgressPosition;

    public override void _Ready()
    {
        AddToGroup("canyon_firefly_guidance");
        _level = GetNodeOrNull<PrologueLevelController>(LevelPath);
        _world = GetNodeOrNull<SeamlessPrologueWorld>(WorldPath);
        _flow = GetNodeOrNull<PrologueGameFlowController>(FlowPath);
        _guidance = GetNodeOrNull<PrologueGuidanceController>(GuidancePath);
        _view = GetNodeOrNull<CanyonFireflyGuidanceView>(ViewPath);
        IsLayoutActive = GodotObject.IsInstanceValid(_world) &&
            _world.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion;
        if (!IsLayoutActive) ClearTransient();
    }

    public override void _Process(double delta) => RefreshNow((float)delta);

    public override void _ExitTree() => ClearTransient();

    public void RefreshNow(float elapsedActiveSeconds = 0f)
    {
        IsLayoutActive = GodotObject.IsInstanceValid(_world) &&
            _world.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion;
        if (!IsLayoutActive || !GodotObject.IsInstanceValid(_level) ||
            !GodotObject.IsInstanceValid(_flow) ||
            !GodotObject.IsInstanceValid(_level.Player) || !_level.CurrentSector.HasValue)
        {
            ClearTransient();
            return;
        }
        if (_flow.State == PrologueGameFlowState.Paused)
        {
            bool guidanceEnabled = _guidance?.GuidanceEnabled == true;
            CanyonFireflyFrame frozen = _model.Advance(elapsedActiveSeconds,
                new CanyonFireflyInput(guidanceEnabled, true, CombatTelegraphActive,
                    false, false, false, GodotObject.IsInstanceValid(CurrentMarker)));
            if (!guidanceEnabled) ClearTransient();
            else if (!frozen.Visible) _view?.Clear();
            return;
        }
        if (_flow.State != PrologueGameFlowState.Playing)
        {
            ClearTransient();
            return;
        }

        CharacterBody2D player = _level.Player;
        if (!TryResolveRouteNode(player.GlobalPosition, out CanyonRouteNodeId routeNode))
        {
            ClearTransient();
            return;
        }

        bool progressed = _lastRouteNode.HasValue && _lastRouteNode.Value != routeNode;
        if (_hasProgressPosition && player.GlobalPosition.DistanceTo(_lastProgressPosition) >=
            RouteProgressDistance) progressed = true;
        if (!_hasProgressPosition || progressed)
        {
            _hasProgressPosition = true;
            _lastProgressPosition = player.GlobalPosition;
        }
        _lastRouteNode = routeNode;

        CurrentMarker = ResolveNearestVisibleMarker(routeNode, player.GlobalPosition);
        CombatTelegraphActive = ResolveCombatTelegraph(routeNode);
        bool importantFork = CountVisibleBranches(routeNode) >= 2;
        bool bossExit = routeNode == CanyonRouteNodeId.BossHall &&
            _level.Progress.BossDefeated &&
            CurrentMarker?.Name == new StringName(nameof(CanyonRouteNodeId.SafeExit));
        CanyonFireflyFrame frame = _model.Advance(elapsedActiveSeconds,
            new CanyonFireflyInput(_guidance?.GuidanceEnabled == true, GetTree().Paused,
                CombatTelegraphActive, importantFork, bossExit, progressed,
                GodotObject.IsInstanceValid(CurrentMarker)));
        if (frame.Visible && GodotObject.IsInstanceValid(CurrentMarker))
            _view?.Present(player.GlobalPosition, CurrentMarker.GlobalPosition,
                frame.VisibleSecondsRemaining);
        else _view?.Clear();
    }

    public void ClearTransient()
    {
        _model.Reset();
        CurrentMarker = null;
        CombatTelegraphActive = false;
        _lastRouteNode = null;
        _hasProgressPosition = false;
        _view?.Clear();
    }

    public static bool IsMarkerEligible(SeamlessPrologueWorld world,
        PrologueProgress progress, CanyonRouteNodeId from, CanyonRouteNodeId target,
        Rect2 visibleWorldBounds, out Marker2D marker)
    {
        marker = null;
        if (!GodotObject.IsInstanceValid(world) || world.LayoutVersion !=
                CanyonWorldLayout.CurrentLayoutVersion || progress == null || from == target)
            return false;
        IReadOnlyList<CanyonRouteNodeId> reachable = CanyonRouteGraph.Reachable(from,
            progress.OpenShortcuts, progress.ActivatedMechanisms, progress.BossDefeated);
        if (!reachable.Contains(target)) return false;
        return IsReachableMarkerEligible(world, from, target, visibleWorldBounds, out marker);
    }

    // The caller must establish reachability for this synchronous query. Keep
    // mount, visibility, live support and self-target checks shared with the
    // public independently validated path; never cache nodes across refreshes.
    private static bool IsReachableMarkerEligible(SeamlessPrologueWorld world,
        CanyonRouteNodeId from, CanyonRouteNodeId target, Rect2 visibleWorldBounds,
        out Marker2D marker)
    {
        marker = null;
        if (!GodotObject.IsInstanceValid(world) || world.LayoutVersion !=
                CanyonWorldLayout.CurrentLayoutVersion || from == target) return false;
        CanyonRouteNodeDefinition definition = CanyonWorldLayout.Nodes.Single(node => node.Id == target);
        if (!world.TryGetMountedSector(definition.SectorId, out PrologueSector sector)) return false;
        marker = sector.GetNodeOrNull<Marker2D>($"Gameplay/MicroScenes/{target}");
        if (!GodotObject.IsInstanceValid(marker) || !marker.IsInsideTree() ||
            !marker.IsVisibleInTree() || !visibleWorldBounds.HasPoint(marker.GlobalPosition))
        {
            marker = null;
            return false;
        }
        if (!definition.RestArea && (!TryResolveSupport(marker, target,
                out GrappleSurface2D support) ||
            support.Material is GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden))
        {
            marker = null;
            return false;
        }
        return true;
    }

    public static bool TryResolveSupport(Marker2D marker, CanyonRouteNodeId routeNode,
        out GrappleSurface2D support)
    {
        support = null;
        if (!GodotObject.IsInstanceValid(marker) || !marker.IsInsideTree()) return false;
        PrologueSector sector = FindAncestor<PrologueSector>(marker);
        CanyonRouteNodeDefinition definition = CanyonWorldLayout.Nodes.Single(node => node.Id == routeNode);
        if (!GodotObject.IsInstanceValid(sector) || sector.SectorId != definition.SectorId) return false;
        float bestDistance = float.PositiveInfinity;
        FindSupportInChildren(sector, marker, definition.WorldBounds, ref bestDistance, ref support);
        return GodotObject.IsInstanceValid(support);
    }

    private static void FindSupportInChildren(Node root, Marker2D marker, Rect2 worldBounds,
        ref float bestDistance, ref GrappleSurface2D support)
    {
        if (!GodotObject.IsInstanceValid(root)) return;
        int childCount = root.GetChildCount();
        for (int index = 0; index < childCount; index++)
        {
            Node child = root.GetChild(index);
            if (child is GrappleSurface2D candidate && candidate.IsInsideTree() &&
                candidate.IsVisibleInTree() && !candidate.IsQueuedForDeletion())
            {
                Vector2 worldPoint = candidate.GlobalPosition;
                if (worldBounds.HasPoint(worldPoint))
                {
                    float distance = marker.GlobalPosition.DistanceSquaredTo(worldPoint);
                    if (!(distance >= bestDistance))
                    {
                        bestDistance = distance;
                        support = candidate;
                    }
                }
            }
            FindSupportInChildren(child, marker, worldBounds, ref bestDistance, ref support);
        }
    }

    private Marker2D ResolveNearestVisibleMarker(CanyonRouteNodeId from, Vector2 playerPosition)
    {
        Marker2D best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (CanyonRouteNodeId target in CanyonRouteGraph.Reachable(from,
                     _level.Progress.OpenShortcuts, _level.Progress.ActivatedMechanisms,
                     _level.Progress.BossDefeated))
        {
            if (!IsReachableMarkerEligible(_world, from, target,
                    CanyonWorldLayout.DesignBounds, out Marker2D marker)) continue;
            Vector2 screen = marker.GetGlobalTransformWithCanvas().Origin;
            if (!_level.GetViewportRect().HasPoint(screen)) continue;
            float distance = playerPosition.DistanceSquaredTo(marker.GlobalPosition);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = marker;
        }
        return best;
    }

    private int CountVisibleBranches(CanyonRouteNodeId from)
    {
        var destinations = new HashSet<CanyonRouteNodeId>();
        foreach (CanyonRouteEdge edge in CanyonWorldLayout.Edges)
        {
            CanyonRouteNodeId? candidate = edge.From == from ? edge.To :
                edge.Bidirectional && edge.To == from ? edge.From : null;
            if (candidate.HasValue && IsMarkerEligible(_world, _level.Progress, from,
                    candidate.Value, CanyonWorldLayout.DesignBounds, out Marker2D marker) &&
                _level.GetViewportRect().HasPoint(marker.GetGlobalTransformWithCanvas().Origin))
                destinations.Add(candidate.Value);
        }
        return destinations.Count;
    }

    private bool ResolveCombatTelegraph(CanyonRouteNodeId routeNode)
    {
        PrologueSector sector = _level.CurrentSectorRoot;
        if (!GodotObject.IsInstanceValid(sector)) return false;
        foreach (Node node in Descendants(sector))
            if (node is ICanyonTelegraphSource mechanism && node.IsInsideTree() &&
                IsMechanismLocallyRelevant(node, routeNode) &&
                mechanism.IsDangerTelegraphActive) return true;

        Node actors = sector.GetNodeOrNull("Gameplay/Actors");
        foreach (Node node in Descendants(sector.GetNodeOrNull("Gameplay/Encounters")))
        {
            if (node is not CanyonEncounterGroup encounter || !encounter.IsActive ||
                encounter.Definition == null) continue;
            foreach (Node actor in Descendants(actors))
                if (actor is Node2D body && encounter.Definition.ArenaBounds.HasPoint(body.GlobalPosition) &&
                    CanyonCombatTelegraphAdapter.IsTelegraphActive(body)) return true;
        }
        BossArenaController arena = sector.GetNodeOrNull<BossArenaController>(
            "Gameplay/BossArenaController");
        return arena?.IsEncounterActive == true &&
            CanyonCombatTelegraphAdapter.IsTelegraphActive(arena.CurrentBoss);
    }

    private bool IsMechanismLocallyRelevant(Node mechanism, CanyonRouteNodeId routeNode)
    {
        CanyonRouteNodeDefinition route = CanyonWorldLayout.Nodes.Single(node => node.Id == routeNode);
        Rect2 viewport = _level.GetViewportRect();
        foreach (Node node in Descendants(mechanism))
        {
            if (node is not CollisionShape2D collision ||
                collision.Shape is not RectangleShape2D rectangle) continue;
            Rect2 worldBounds = TransformedRectangleBounds(collision.GlobalTransform,
                rectangle.Size * .5f);
            if (!route.WorldBounds.Intersects(worldBounds)) continue;
            Rect2 screenBounds = TransformedRectangleBounds(
                collision.GetGlobalTransformWithCanvas(), rectangle.Size * .5f);
            if (viewport.Intersects(screenBounds)) return true;
        }
        return false;
    }

    private static Rect2 TransformedRectangleBounds(Transform2D transform, Vector2 halfSize)
    {
        Vector2 topLeft = transform * new Vector2(-halfSize.X, -halfSize.Y);
        Vector2 topRight = transform * new Vector2(halfSize.X, -halfSize.Y);
        Vector2 bottomLeft = transform * new Vector2(-halfSize.X, halfSize.Y);
        Vector2 bottomRight = transform * new Vector2(halfSize.X, halfSize.Y);
        float left = Mathf.Min(Mathf.Min(topLeft.X, topRight.X),
            Mathf.Min(bottomLeft.X, bottomRight.X));
        float top = Mathf.Min(Mathf.Min(topLeft.Y, topRight.Y),
            Mathf.Min(bottomLeft.Y, bottomRight.Y));
        float right = Mathf.Max(Mathf.Max(topLeft.X, topRight.X),
            Mathf.Max(bottomLeft.X, bottomRight.X));
        float bottom = Mathf.Max(Mathf.Max(topLeft.Y, topRight.Y),
            Mathf.Max(bottomLeft.Y, bottomRight.Y));
        return new Rect2(left, top, right - left, bottom - top);
    }

    private static bool TryResolveRouteNode(Vector2 worldPosition, out CanyonRouteNodeId id)
    {
        return CanyonWorldLayout.TryResolveRouteNode(worldPosition, out id);
    }

    private static T FindAncestor<T>(Node node) where T : Node
    {
        for (Node current = node; current != null; current = current.GetParent())
            if (current is T match) return match;
        return null;
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        if (!GodotObject.IsInstanceValid(root)) yield break;
        // These synchronous readers do not mutate the tree. Walk the same
        // non-internal, depth-first child order without creating a Godot Array
        // and its managed enumerator for every node (including every leaf).
        int childCount = root.GetChildCount();
        for (int index = 0; index < childCount; index++)
        {
            Node child = root.GetChild(index);
            yield return child;
            foreach (Node nested in Descendants(child)) yield return nested;
        }
    }
}
