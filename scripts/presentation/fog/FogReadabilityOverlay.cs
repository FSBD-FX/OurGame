using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Presentation.Fog;

public enum VisibleLaserWarning { None, Tracking, Locked, Active }

/// <summary>
/// Local attack warnings above fog, copied from live authoritative leaves.
/// The caller submits after presentation in FramePreDraw, using the current
/// native snapshot from the same world. This does not enable the formal fog.
/// </summary>
public partial class FogReadabilityOverlay : Node2D
{
    private const float MaximumSourceDistance = 620f;
    private readonly List<AttackTelegraphVisual> _copies = new();
    private readonly List<AttackPresentationFrame> _acceptedCopyFrames = new();
    private readonly HashSet<ulong> _submittedSources = new();
    public int PresentedCueCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }

    public VisibleLaserWarning ReadVisibleLaser(HeadlampOcclusionFrame frame)
    {
        if (!Live(this) || !IsProcessing() || !CanProcess() || GetTree().Paused ||
            !VisibleWithLayer(this) || frame == null || !frame.IsComplete ||
            !ReferenceEquals(frame, LastOcclusionFrame)) return VisibleLaserWarning.None;
        VisibleLaserWarning strongest = VisibleLaserWarning.None;
        for (int i = 0; i < _acceptedCopyFrames.Count; i++)
        {
            AttackTelegraphVisual copy = _copies[i];
            if (!Live(copy) || !copy.CanProcess() || !VisibleWithLayer(copy) ||
                copy.GetViewport() != GetViewport() || copy.GetWorld2D().GetRid() != GetWorld2D().GetRid() ||
                copy.CurrentFrame != _acceptedCopyFrames[i]) continue;
            AttackPresentationFrame accepted = _acceptedCopyFrames[i];
            if (accepted.Timeline.Kind != EnemyAttackKind.PrismLaserBeam ||
                accepted.Shape.Kind != AttackShapeKind.Capsule ||
                accepted.Shape.Extent.X <= 0f || accepted.Shape.Extent.Y <= accepted.Shape.Extent.X) continue;
            VisibleLaserWarning warning = accepted.Timeline.Phase switch
            {
                AttackPhase.Telegraph => accepted.Timeline.HasLockedTarget ? VisibleLaserWarning.Locked : VisibleLaserWarning.Tracking,
                AttackPhase.Active when accepted.Timeline.HasLockedTarget => VisibleLaserWarning.Active,
                _ => VisibleLaserWarning.None
            };
            if (warning > strongest) strongest = warning;
        }
        return strongest;
    }

    private static bool VisibleWithLayer(Node2D node)
    {
        if (!node.IsVisibleInTree()) return false;
        for (Node ancestor = node.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor is CanvasLayer { Visible: false }) return false;
        return true;
    }

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
            frame.Timeline.Phase is not (AttackPhase.Telegraph or AttackPhase.Active) ||
            !ShapeOverlapsViewport(frame.Shape, source.GetCanvasTransform(), GetViewport().GetVisibleRect()) ||
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
        // Capture the validated/frozen frame actually held by the displayed copy,
        // not the source's mutable list or a previous batch's permission.
        _acceptedCopyFrames.Add(copy.CurrentFrame);
        PresentedCueCount++;
        return true;
    }

    private static bool ShapeOverlapsViewport(AttackShapeFrame shape, Transform2D canvas, Rect2 viewport)
    {
        if (!HeadlampOcclusionSampler.Finite(canvas.X) || !HeadlampOcclusionSampler.Finite(canvas.Y) ||
            !HeadlampOcclusionSampler.Finite(canvas.Origin)) return false;
        Rect2 world;
        try { world = AttackShapeBounds.WorldBounds(shape); }
        catch (ArgumentOutOfRangeException) { return false; }
        // Frames are world-space, including polygon vertices. Transform ALL four
        // corners of their conservative AABB, not the source's local transform
        // or physical FinalTransform. The actual copied shape stays unchanged.
        Vector2 first = canvas * world.Position;
        Vector2 second = canvas * new Vector2(world.End.X, world.Position.Y);
        Vector2 third = canvas * world.End;
        Vector2 fourth = canvas * new Vector2(world.Position.X, world.End.Y);
        if (!HeadlampOcclusionSampler.Finite(first) || !HeadlampOcclusionSampler.Finite(second) ||
            !HeadlampOcclusionSampler.Finite(third) || !HeadlampOcclusionSampler.Finite(fourth)) return false;
        Rect2 projected = new Rect2(first, Vector2.Zero).Expand(second).Expand(third).Expand(fourth);
        return HeadlampOcclusionSampler.Finite(projected.Size) && projected.HasArea() && projected.Intersects(viewport);
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
        _acceptedCopyFrames.Clear();
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
