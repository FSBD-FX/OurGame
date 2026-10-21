using Godot;
using Knight.Interactions;

namespace Knight.Presentation.Fog;

/// <summary>Display-only consumer of the single authoritative nearby Q candidate.</summary>
public partial class FogInteractionReadabilityOverlay : Node2D
{
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    public int PresentedCueCount { get; private set; }
    private EnvironmentInteractionController _source;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Clear();
    }

    public override void _Process(double delta)
    {
        Clear();
        if (!Live(_source)) ReleaseSource();
    }

    public void PresentInteraction(EnvironmentInteractionController source, HeadlampOcclusionFrame frame)
    {
        Clear();
        if (!Live(this) || !Live(source) || source.GetViewport() != GetViewport() ||
            source.GetWorld2D() != GetWorld2D()) { ReleaseSource(); return; }
        if (_source != source) { ReleaseSource(); _source = source; }
        source.AcquirePromptRenderer(this);
        if (GetTree().Paused || frame is not { IsComplete: true } ||
            source.GetParent() is not CharacterBody2D actor || !Live(actor)) return;
        var candidate = source.CurrentCandidate;
        if (candidate == null || !Live(candidate.InteractionNode)) return;
        Node2D target = candidate.InteractionNode;
        Vector2 position = target.GlobalPosition;
        Vector2 cue = position + new Vector2(0, -30);
        Transform2D canvas = source.GetCanvasTransform(), overlay = GetCanvasTransform();
        if (!HeadlampOcclusionSampler.Finite(position) || !HeadlampOcclusionSampler.Finite(cue) ||
            !Aligned(canvas, overlay) || target.GetCanvas() != actor.GetCanvas() ||
            target.GetViewport() != GetViewport() || target.GetWorld2D() != GetWorld2D() ||
            !GetViewport().GetVisibleRect().HasPoint(canvas * position) ||
            !GetViewport().GetVisibleRect().HasPoint(canvas * cue) ||
            !frame.CanSee(actor.GlobalPosition, position)) return;
        // Object LOS authorizes the already-selected interaction. A UI label's
        // decorative offset is not a second world target or a new selection rule.
        string text = source.CurrentPromptText;
        if (string.IsNullOrWhiteSpace(text)) return;
        Vector2 local = ToLocal(cue);
        if (!HeadlampOcclusionSampler.Finite(local)) return;
        LastOcclusionFrame = frame;
        RenderingServer.CanvasItemAddCircle(GetCanvasItem(), local, 5f, new Color(.45f,1f,.82f,.9f));
        ThemeDB.FallbackFont.DrawString(GetCanvasItem(), local + new Vector2(-100,-10), text,
            HorizontalAlignment.Center, 200f, 16, new Color(.9f,1f,.93f));
        PresentedCueCount = 1;
    }

    public void Clear()
    {
        LastOcclusionFrame = null;
        PresentedCueCount = 0;
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }

    public void ClearAndRelease()
    {
        Clear();
        ReleaseSource();
    }

    private void ReleaseSource()
    {
        if (IsInstanceValid(_source)) _source.ReleasePromptRenderer(this);
        _source = null;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationUnpaused) Clear();
    }

    public override void _ExitTree()
    {
        ClearAndRelease();
        RequestReady();
    }

    private static bool Aligned(Transform2D a, Transform2D b) =>
        HeadlampOcclusionSampler.Finite(a.X) && HeadlampOcclusionSampler.Finite(a.Y) &&
        HeadlampOcclusionSampler.Finite(a.Origin) && HeadlampOcclusionSampler.Finite(b.X) &&
        HeadlampOcclusionSampler.Finite(b.Y) && HeadlampOcclusionSampler.Finite(b.Origin) &&
        a.X.DistanceTo(b.X) < .001f && a.Y.DistanceTo(b.Y) < .001f && a.Origin.DistanceTo(b.Origin) < .01f;

    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }
}
