using Godot;
using System.Collections.Generic;
using Knight.Combat;
using Knight.Player;
using Knight.Localization;
using Knight.Settings;

namespace Knight.Interactions;

public partial class EnvironmentInteractionController : Node2D
{
    public const string InteractableGroup = "environment_interactable";

    [Export] public float InteractionRadius { get; set; } = EnvironmentInteractionModel.DefaultRadius;

    private CharacterBody2D _actor;
    private PlayerInputReader _input;
    private HealthComponent _health;
    private IEnvironmentInteractable _candidate;
    private Node _candidateOwner;
    private ulong _lastConsumedSequence;
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private Node2D _promptRenderer;
    private readonly List<IEnvironmentInteractable> _objects = new();
    private readonly List<Node> _owners = new();
    private readonly List<EnvironmentInteractionCandidate> _snapshots = new();
    private readonly Godot.Collections.Array<Rid> _excluded = new();

    // Revalidate only the selected object; never perform an alternate UI search.
    // Visibility, opt-in, position or occlusion can change after physics selection.
    public IEnvironmentInteractable CurrentCandidate
    {
        get
        {
            if (!ControlsAvailable() || !IsLive(_candidateOwner) ||
                !_candidateOwner.IsInGroup(InteractableGroup) || _candidate is not { } candidate ||
                !IsInActorWorld(candidate.InteractionNode) || !candidate.InteractionNode.IsVisibleInTree() ||
                string.IsNullOrWhiteSpace(candidate.StableId) || !float.IsFinite(InteractionRadius) ||
                InteractionRadius < 0f) return null;
            float distanceSquared = _actor.GlobalPosition.DistanceSquaredTo(candidate.InteractionNode.GlobalPosition);
            return float.IsFinite(distanceSquared) && distanceSquared <= InteractionRadius * InteractionRadius &&
                candidate.CanInteract(_actor) && HasLineOfSight(candidate) ? candidate : null;
        }
    }

    public string CurrentPromptText => CurrentCandidate is { } candidate
        ? PrologueStrings.Get(candidate.InteractionTextKey, _language) : string.Empty;

    public void SetLanguage(GameLanguage language)
    {
        _language = System.Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        QueueRedraw();
    }

    public override void _Ready()
    {
        _actor = GetParent<CharacterBody2D>();
        _input = GetNode<PlayerInputReader>("../Input");
        _health = GetNode<HealthComponent>("../Health");
        _health.Died += ClearCandidate;
    }

    public override void _PhysicsProcess(double delta)
    {
        _objects.Clear();
        _owners.Clear();
        _snapshots.Clear();
        _candidate = null;
        _candidateOwner = null;
        if (!ControlsAvailable())
        {
            QueueRedraw();
            return;
        }
        foreach (Node owner in GetTree().GetNodesInGroup(InteractableGroup))
        {
            if (owner is not IEnvironmentInteractable item || !IsLive(owner) ||
                !IsInActorWorld(item.InteractionNode))
                continue;
            float distanceSquared = _actor.GlobalPosition.DistanceSquaredTo(item.InteractionNode.GlobalPosition);
            bool available = item.InteractionNode.IsVisibleInTree() && item.CanInteract(_actor);
            // Avoid a ray for distant or already ineligible objects. Model still owns
            // final admission and stable ordering, using the same snapshot for preview.
            bool visible = available && distanceSquared <= InteractionRadius * InteractionRadius && HasLineOfSight(item);
            _objects.Add(item);
            _owners.Add(owner);
            _snapshots.Add(new EnvironmentInteractionCandidate(item.StableId, distanceSquared, visible, available));
        }
        int selected = EnvironmentInteractionModel.Resolve(_snapshots, true, false, InteractionRadius);
        if (selected >= 0)
        {
            _candidate = _objects[selected];
            _candidateOwner = _owners[selected];
        }
        IEnvironmentInteractable candidate = CurrentCandidate;
        if (_input.Current.InteractPressed && _lastConsumedSequence != _input.CurrentFrameSequence)
        {
            _lastConsumedSequence = _input.CurrentFrameSequence;
            // No alternate selection on failure: execute only the candidate just previewed.
            // No equipment/energy access and no forged remote ThreadInteraction event.
            if (candidate != null) candidate.TryInteract(_actor);
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (IsLive(_promptRenderer)) return;
        IEnvironmentInteractable candidate = CurrentCandidate;
        if (candidate == null) return;
        Vector2 position = ToLocal(candidate.InteractionNode.GlobalPosition) + new Vector2(0f, -30f);
        DrawCircle(position, 5f, new Color(.45f, 1f, .82f, .9f));
        DrawString(ThemeDB.FallbackFont, position + new Vector2(-100f, -10f), CurrentPromptText,
            HorizontalAlignment.Center, 200f, 16, new Color(.9f, 1f, .93f));
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationUnpaused) ClearCandidate();
    }

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_health)) _health.Died -= ClearCandidate;
        ClearCandidate();
        _promptRenderer = null;
        RequestReady();
    }

    // Presentation ownership only; this never changes selection or execution.
    // Clear existing commands immediately because FramePreDraw is after the
    // deferred redraw flush. A stale owner cannot release a newer renderer.
    internal void AcquirePromptRenderer(Node2D renderer)
    {
        if (!IsLive(renderer) || renderer.GetViewport() != GetViewport() ||
            renderer.GetWorld2D() != GetWorld2D()) return;
        _promptRenderer = renderer;
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }

    internal void ReleasePromptRenderer(Node2D renderer)
    {
        if (!ReferenceEquals(_promptRenderer, renderer)) return;
        _promptRenderer = null;
        RenderingServer.CanvasItemClear(GetCanvasItem());
        QueueRedraw();
    }

    private bool ControlsAvailable() => IsLive(this) && IsPhysicsProcessing() &&
        IsLive(_actor) && IsLive(_input) && _input.IsPhysicsProcessing() &&
        IsLive(_health) && _health.CurrentHealth > 0 && !GetTree().Paused &&
        _actor.IsOnFloor() && Mathf.Abs(_actor.Velocity.Y) <= .1f &&
        Mathf.Abs(_actor.Velocity.X) <= .1f && Mathf.Abs(_input.Current.MoveAxis) <= .001f &&
        !_input.Current.JumpPressed;

    private void ClearCandidate()
    {
        _candidate = null;
        _candidateOwner = null;
        QueueRedraw();
    }

    private bool HasLineOfSight(IEnvironmentInteractable item)
    {
        Vector2 origin = _actor.GlobalPosition;
        Vector2 destination = item.InteractionNode.GlobalPosition;
        if (!Finite(destination)) return false;
        if (origin.IsEqualApprox(destination)) return true;
        _excluded.Clear();
        AddCollisionRids(_actor, _excluded);
        using var query = PhysicsRayQueryParameters2D.Create(origin, destination);
        query.CollisionMask = CombatCollisionLayers.World;
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.HitFromInside = true;
        query.Exclude = _excluded;
        var hit = _actor.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return true;
        if (!hit.ContainsKey("collider") || hit["collider"].AsGodotObject() is not Node collider) return false;
        Node2D anchor = item.InteractionNode;
        return ReferenceEquals(collider, anchor) || anchor.IsAncestorOf(collider) || collider.IsAncestorOf(anchor);
    }

    private static bool IsLive(Node node)
    {
        if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }

    private bool IsInActorWorld(Node2D node) => IsLive(node) &&
        node.GetTree() == GetTree() && node.GetWorld2D() == _actor.GetWorld2D();

    private static void AddCollisionRids(Node node, Godot.Collections.Array<Rid> rids)
    {
        if (node is CollisionObject2D collision) rids.Add(collision.GetRid());
        foreach (Node child in node.GetChildren()) AddCollisionRids(child, rids);
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
