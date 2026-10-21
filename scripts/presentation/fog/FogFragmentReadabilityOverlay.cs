using System;
using System.Collections.Generic;
using Godot;
using Knight.Prologue.Collections;

namespace Knight.Presentation.Fog;

/// <summary>
/// A tiny replaceable cue, not a copy of the fragment art or a collection model.
/// Submit live mounted sources and the same fresh native snapshot as the fog.
/// </summary>
public partial class FogFragmentReadabilityOverlay : Node2D
{
    // Candidate tuning: a 60px transition beyond the 180px clear circle.
    [Export] public float FragmentCueRadius { get; set; } = 240f;
    public int PresentedCueCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _nativeIds = new();
    private readonly HashSet<string> _stableIds = new(StringComparer.Ordinal);
    private QuestFragment _failureSource;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 20; // Transient error text wins over sibling world/Q cues, never HUD layer5.
        Clear();
    }

    public override void _Process(double delta) => Clear();

    public void PresentFragments(IReadOnlyList<QuestFragment> sources, Vector2 actorWorldPosition,
        HeadlampOcclusionFrame occlusion)
    {
        Clear();
        if (!Live(this) || GetTree().Paused || sources == null || occlusion is not { IsComplete: true } ||
            !HeadlampOcclusionSampler.Finite(actorWorldPosition) ||
            !float.IsFinite(FragmentCueRadius) || FragmentCueRadius <= 0f) return;
        LastOcclusionFrame = occlusion;
        QuestFragment nearestFailure = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (QuestFragment fragment in sources)
        {
            if (!Live(fragment) || !fragment.IsVisibleInTree() || fragment.IsCollected ||
                !QuestFragmentCatalog.IsKnown(fragment.StableId) ||
                fragment.GetViewport() != GetViewport() || fragment.GetWorld2D().GetRid() != GetWorld2D().GetRid() ||
                fragment.VisualPath == null || fragment.VisualPath.IsEmpty ||
                fragment.GetNodeOrNull<QuestFragmentVisual>(fragment.VisualPath) is not { } visual ||
                !Live(visual) || !visual.IsVisibleInTree() || visual.GetCanvas() != fragment.GetCanvas()) continue;

            Vector2 source = fragment.GlobalPosition;
            Vector2 cue = visual.GlobalPosition + new Vector2(0f, -22f);
            Transform2D canvas = fragment.GetCanvasTransform();
            Vector2 sourceCanvas = canvas * source, cueCanvas = canvas * cue;
            if (!HeadlampOcclusionSampler.Finite(source) || !HeadlampOcclusionSampler.Finite(cue) ||
                !HeadlampOcclusionSampler.Finite(sourceCanvas) || !HeadlampOcclusionSampler.Finite(cueCanvas) ||
                !AlignedCanvas(canvas, GetCanvasTransform()) ||
                actorWorldPosition.DistanceTo(source) > FragmentCueRadius ||
                !GetViewport().GetVisibleRect().HasPoint(sourceCanvas) ||
                !GetViewport().GetVisibleRect().HasPoint(cueCanvas) ||
                !occlusion.CanSee(actorWorldPosition, source) || !occlusion.CanSee(actorWorldPosition, cue)) continue;

            // Reject duplicate native references and duplicate permanent IDs only
            // after eligibility, so a hidden retired node cannot hide a live one.
            if (_nativeIds.Contains(fragment.GetInstanceId()) || _stableIds.Contains(fragment.StableId)) continue;
            Color core = visual.CoreColor, shell = visual.ShellColor;
            if (!Finite(core) || !Finite(shell)) continue;
            Vector2 local = ToLocal(cue);
            if (!HeadlampOcclusionSampler.Finite(local)) continue;
            _nativeIds.Add(fragment.GetInstanceId());
            _stableIds.Add(fragment.StableId);
            core.A = 1f; shell.A = 1f;
            // FramePreDraw is after deferred QueueRedraw flushing. Immediate
            // commands prevent a canceled/unloaded cue surviving another raster.
            RenderingServer.CanvasItemAddPolyline(GetCanvasItem(),
                [local + new Vector2(0, -8), local + new Vector2(6, 0),
                 local + new Vector2(0, 8), local + new Vector2(-6, 0), local + new Vector2(0, -8)],
                [shell], 1.5f, true);
            RenderingServer.CanvasItemAddPolygon(GetCanvasItem(),
                [local + new Vector2(0, -3), local + new Vector2(3, 0),
                 local + new Vector2(0, 3), local + new Vector2(-3, 0)], [core]);
            PresentedCueCount++;
            if (fragment.LastResult != FragmentCollectionResult.SaveFailed) continue;
            float distance = actorWorldPosition.DistanceSquaredTo(source);
            if (distance < nearestDistance || (distance == nearestDistance && nearestFailure != null &&
                string.CompareOrdinal(fragment.StableId, nearestFailure.StableId) < 0))
            { nearestFailure = fragment; nearestDistance = distance; }
        }
        PresentSaveFailure(nearestFailure);
    }

    private void PresentSaveFailure(QuestFragment source)
    {
        if (!IsVisibleInTree() || !Live(source) || source.StatusPath == null || source.StatusPath.IsEmpty ||
            source.GetNodeOrNull<Label>(source.StatusPath) is not { } status || !Live(status) ||
            !status.IsVisibleInTree() || string.IsNullOrWhiteSpace(status.Text)) return;
        // Borrow the original label's font for this draw only; do not retain a
        // native Resource or copy a save-error model into presentation.
        Font font = status.GetThemeFont("font");
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float width = Mathf.Min(480f, viewport.X - 32f);
        const int size = 16;
        const TextServer.LineBreakFlag breaks = TextServer.LineBreakFlag.Mandatory |
            TextServer.LineBreakFlag.WordBound | TextServer.LineBreakFlag.Adaptive;
        if (font == null || width < 64f) return;
        Vector2 textSize = font.GetMultilineStringSize(status.Text, HorizontalAlignment.Left,
            width - 24f, size, brkFlags: breaks);
        float height = textSize.Y + 16f;
        if (!HeadlampOcclusionSampler.Finite(textSize) || height > viewport.Y - 24f ||
            !source.TryAcquireStatusRenderer(this)) return;
        _failureSource = source;
        float bottomInset = 120f * Mathf.Max(1f, viewport.Y / 720f);
        var panel = new Rect2((viewport.X - width) * .5f,
            Mathf.Clamp(viewport.Y - bottomInset - height, 12f, viewport.Y - height - 12f), width, height);
        // Pixel-space text stays contained and unstretched under camera zoom.
        RenderingServer.CanvasItemAddSetTransform(GetCanvasItem(), GetGlobalTransformWithCanvas().AffineInverse());
        RenderingServer.CanvasItemAddRect(GetCanvasItem(), panel, new Color(.10f, .08f, .07f));
        font.DrawMultilineString(GetCanvasItem(), panel.Position + new Vector2(12f, 8f + font.GetAscent(size)),
            status.Text, HorizontalAlignment.Left, width - 24f, size,
            modulate: Colors.White, brkFlags: breaks);
        RenderingServer.CanvasItemAddSetTransform(GetCanvasItem(), Transform2D.Identity);
    }

    public void Clear()
    {
        if (IsInstanceValid(_failureSource)) _failureSource.ReleaseStatusRenderer(this);
        _failureSource = null;
        PresentedCueCount = 0;
        LastOcclusionFrame = null;
        _nativeIds.Clear();
        _stableIds.Clear();
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }

    public override void _ExitTree()
    {
        Clear();
        RequestReady();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationUnpaused) Clear();
    }

    private static bool AlignedCanvas(Transform2D source, Transform2D target) =>
        HeadlampOcclusionSampler.Finite(source.X) && HeadlampOcclusionSampler.Finite(source.Y) &&
        HeadlampOcclusionSampler.Finite(source.Origin) && HeadlampOcclusionSampler.Finite(target.X) &&
        HeadlampOcclusionSampler.Finite(target.Y) && HeadlampOcclusionSampler.Finite(target.Origin) &&
        source.X.DistanceTo(target.X) < .001f && source.Y.DistanceTo(target.Y) < .001f &&
        source.Origin.DistanceTo(target.Origin) < .01f;

    private static bool Finite(Color value) => float.IsFinite(value.R) && float.IsFinite(value.G) &&
        float.IsFinite(value.B) && float.IsFinite(value.A);

    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }
}
