using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Presentation.Fog;

/// <summary>
/// Local attack warnings above fog, copied from live authoritative leaves.
/// The caller submits after presentation in FramePreDraw, using the current
/// native snapshot from the same world. This does not enable the formal fog.
/// </summary>
public partial class FogReadabilityOverlay : Node2D
{
    private const float MaximumSourceDistance = 620f;
    private readonly List<AttackTelegraphVisual> _copies = new();
    private readonly HashSet<ulong> _submittedSources = new();
    public int PresentedCueCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        CopyAt(0);
        Clear();
    }

    public override void _Process(double delta)
    {
        // Fail closed when a caller stops submitting (pause, streaming, etc.).
        // FramePreDraw can then submit the current frame, never a cached attack.
        Clear();
    }

    public void PresentAttack(AttackTelegraphVisual source, Vector2 actorWorldPosition,
        HeadlampOcclusionFrame occlusion)
    {
        Clear();
        if (CanPresent(actorWorldPosition, occlusion))
        {
            LastOcclusionFrame = occlusion;
            PresentEligibleSource(source, actorWorldPosition, occlusion, 0);
        }
    }

    // One atomic current-frame submission: a canceled/hidden source cannot
    // erase another live threat, and omitted sources never keep stale pixels.
    public void PresentAttacks(IReadOnlyList<AttackTelegraphVisual> sources,
        Vector2 actorWorldPosition, HeadlampOcclusionFrame occlusion)
    {
        Clear();
        if (sources == null || !CanPresent(actorWorldPosition, occlusion)) return;
        LastOcclusionFrame = occlusion;
        int used = 0;
        foreach (AttackTelegraphVisual source in sources)
        {
            if (!Live(source) || !_submittedSources.Add(source.GetInstanceId())) continue;
            if (PresentEligibleSource(source, actorWorldPosition, occlusion, used)) used++;
        }
    }

    private bool CanPresent(Vector2 actorWorldPosition, HeadlampOcclusionFrame occlusion) =>
        Live(this) && !GetTree().Paused && HeadlampOcclusionSampler.Finite(actorWorldPosition) &&
        occlusion != null && occlusion.IsComplete;

    private bool PresentEligibleSource(AttackTelegraphVisual source, Vector2 actorWorldPosition,
        HeadlampOcclusionFrame occlusion, int index)
    {
        if (!Live(source) ||
            !source.IsVisibleInTree() || source.GetViewport() != GetViewport() ||
            source.GetWorld2D().GetRid() != GetWorld2D().GetRid()) return false;

        Vector2 origin = source.GlobalPosition;
        // VisibleRect and the source canvas transform are both logical pixels;
        // FinalTransform would convert only the point into physical pixels.
        Vector2 canvasOrigin = source.GetGlobalTransformWithCanvas().Origin;
        AttackPresentationFrame frame = source.CurrentFrame;
        if (!HeadlampOcclusionSampler.Finite(origin) ||
            !HeadlampOcclusionSampler.Finite(canvasOrigin) ||
            actorWorldPosition.DistanceTo(origin) > MaximumSourceDistance ||
            !GetViewport().GetVisibleRect().HasPoint(canvasOrigin) ||
            frame.Timeline.Phase is not (AttackPhase.Telegraph or AttackPhase.Active) ||
            !occlusion.CanSee(actorWorldPosition, origin))
            return false;

        // No second attack model, area reveal, or mutation of the original.
        // AttackTelegraphVisual converts the frame's actual world geometry into
        // this following CanvasLayer's local coordinates, including camera zoom.
        AttackTelegraphVisual copy = CopyAt(index);
        copy.ImpactFillOpacity = source.ImpactFillOpacity;
        copy.ImpactTint = source.ImpactTint;
        copy.ImpactOutlineWidth = source.ImpactOutlineWidth;
        copy.PresentBeforeDraw(frame);
        PresentedCueCount++;
        return true;
    }

    private AttackTelegraphVisual CopyAt(int index)
    {
        if (index == _copies.Count) _copies.Add(null);
        if (!IsInstanceValid(_copies[index]))
        {
            _copies[index] = new AttackTelegraphVisual { Name = $"ProtectedAttack{index + 1}" };
            AddChild(_copies[index]);
        }
        return _copies[index];
    }

    public void Clear()
    {
        _submittedSources.Clear();
        PresentedCueCount = 0;
        LastOcclusionFrame = null;
        foreach (AttackTelegraphVisual copy in _copies)
            if (IsInstanceValid(copy)) copy.PresentBeforeDraw(default);
    }

    public override void _ExitTree()
    {
        Clear();
        RequestReady();
    }

    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        // QueueFree marks the enemy/sector now, not each descendant leaf.
        // Revoke authority at that boundary instead of after the next flush.
        for (Node ancestor = node; ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor.IsQueuedForDeletion()) return false;
        return true;
    }
}
