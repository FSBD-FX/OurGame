using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Presentation;

public partial class AttackTelegraphVisual : Node2D
{
    private static readonly Color Gold = new("d7a84a");
    private static readonly Color Danger = new("c84e45");
    private AttackPresentationFrame _frame;

    // Defaults preserve existing encounters; samples can reduce occlusion without
    // changing the authoritative attack polygon or phase lifetime.
    [Export(PropertyHint.Range, "0,1")] public float ImpactFillOpacity { get; set; } = .34f;
    [Export] public Color ImpactTint { get; set; } = Colors.White;
    [Export(PropertyHint.Range, "1,8")] public float ImpactOutlineWidth { get; set; } = 4f;

    public bool GoldVisible { get; private set; }
    public bool DangerVisible { get; private set; }
    public bool ImpactVisible { get; private set; }
    public float TelegraphProgress { get; private set; }
    public AttackPresentationFrame CurrentFrame => _frame;

    public void Present(AttackPresentationFrame frame) => SetFrame(frame, true);

    // The fog's protected copy is submitted in FramePreDraw, after CanvasItem's
    // deferred redraw flush. Submit the same geometry immediately in that path.
    // Ordinary world telegraphs keep the existing queued _Draw behavior.
    public void PresentBeforeDraw(AttackPresentationFrame frame)
    {
        SetFrame(frame, false);
        RenderingServer.CanvasItemClear(GetCanvasItem());
        DrawFrame(true);
    }

    private void SetFrame(AttackPresentationFrame frame, bool queueRedraw)
    {
        if (frame.Timeline.Phase == AttackPhase.None)
        {
            _frame = default;
            GoldVisible = false;
            DangerVisible = false;
            ImpactVisible = false;
            TelegraphProgress = 0f;
            if (queueRedraw) QueueRedraw();
            return;
        }

        _frame = frame.Validate();
        GoldVisible = frame.Timeline.Phase == AttackPhase.Telegraph;
        DangerVisible = GoldVisible && frame.Timeline.HasLockedTarget;
        ImpactVisible = frame.Timeline.Phase == AttackPhase.Active;
        TelegraphProgress = frame.Timeline.PhaseDurationSeconds <= 0f
            ? 1f
            : Mathf.Clamp(1f - frame.Timeline.PhaseRemainingSeconds / frame.Timeline.PhaseDurationSeconds, 0f, 1f);
        if (queueRedraw) QueueRedraw();
    }

    public override void _Draw() => DrawFrame(false);

    private void DrawFrame(bool immediate)
    {
        if (!GoldVisible && !DangerVisible && !ImpactVisible) return;
        Vector2[] outline = LocalOutline(_frame.Shape);
        if (outline.Length < 3) return;
        if (DangerVisible)
        {
            Color fill = Danger;
            fill.A = .28f;
            Polygon(outline, fill, immediate);
            Polyline(Closed(outline), Danger, 3f, immediate);
        }
        if (GoldVisible)
        {
            Color contour = Gold;
            contour.A = .38f;
            Polyline(Closed(outline), contour, 2f, immediate);
            Vector2[] progress = ProgressPath(outline, TelegraphProgress);
            if (progress.Length > 1) Polyline(progress, Gold, 4f, immediate);
        }
        if (ImpactVisible)
        {
            Color fill = ImpactTint;
            fill.A = Mathf.Clamp(ImpactFillOpacity, 0f, 1f);
            Polygon(outline, fill, immediate);
            Polyline(Closed(outline), ImpactTint,
                Mathf.Clamp(ImpactOutlineWidth, 1f, 8f), immediate);
        }
    }

    private void Polygon(Vector2[] points, Color color, bool immediate)
    {
        if (immediate) RenderingServer.CanvasItemAddPolygon(GetCanvasItem(), points, [color]);
        else DrawPolygon(points, [color]);
    }

    private void Polyline(Vector2[] points, Color color, float width, bool immediate)
    {
        if (immediate) RenderingServer.CanvasItemAddPolyline(GetCanvasItem(), points, new Color[] { color }, width, true);
        else DrawPolyline(points, color, width, true);
    }

    private Vector2[] LocalOutline(AttackShapeFrame shape)
    {
        if (shape.Kind == AttackShapeKind.ConvexPolygon)
        {
            var polygon = new Vector2[shape.Points.Count];
            for (int i = 0; i < polygon.Length; i++) polygon[i] = ToLocal(shape.Points[i]);
            return polygon;
        }

        Vector2[] localShape = shape.Kind is AttackShapeKind.Rectangle or AttackShapeKind.GroundBand
            ? [new(-shape.Extent.X, -shape.Extent.Y), new(shape.Extent.X, -shape.Extent.Y),
                new(shape.Extent.X, shape.Extent.Y), new(-shape.Extent.X, shape.Extent.Y)]
            : RoundedOutline(shape);
        Transform2D world = new(shape.RotationRadians, shape.Origin);
        for (int i = 0; i < localShape.Length; i++) localShape[i] = ToLocal(world * localShape[i]);
        return localShape;
    }

    private static Vector2[] RoundedOutline(AttackShapeFrame shape)
    {
        const int segments = 48;
        var points = new Vector2[segments];
        if (shape.Kind == AttackShapeKind.Circle)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.Tau * i / segments;
                points[i] = new Vector2(Mathf.Cos(angle) * shape.Extent.X,
                    Mathf.Sin(angle) * shape.Extent.Y);
            }
            return points;
        }

        float radius = shape.Extent.X;
        float straight = Mathf.Max(0f, shape.Extent.Y - radius);
        for (int i = 0; i < segments / 2; i++)
        {
            float angle = Mathf.Pi + Mathf.Pi * i / (segments / 2 - 1f);
            points[i] = new Vector2(Mathf.Cos(angle) * radius,
                -straight + Mathf.Sin(angle) * radius);
        }
        for (int i = 0; i < segments / 2; i++)
        {
            float angle = Mathf.Pi * i / (segments / 2 - 1f);
            points[segments / 2 + i] = new Vector2(Mathf.Cos(angle) * radius,
                straight + Mathf.Sin(angle) * radius);
        }
        return points;
    }

    private static Vector2[] Closed(IReadOnlyList<Vector2> points)
    {
        var closed = new Vector2[points.Count + 1];
        for (int i = 0; i < points.Count; i++) closed[i] = points[i];
        closed[^1] = points[0];
        return closed;
    }

    private static Vector2[] ProgressPath(IReadOnlyList<Vector2> points, float progress)
    {
        int visible = Mathf.Clamp(Mathf.CeilToInt(points.Count * progress), 1, points.Count);
        var path = new Vector2[visible + 1];
        for (int i = 0; i < visible; i++) path[i] = points[i];
        path[^1] = points[visible % points.Count];
        return path;
    }
}
