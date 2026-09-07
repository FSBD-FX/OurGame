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

    public bool GoldVisible { get; private set; }
    public bool DangerVisible { get; private set; }
    public bool ImpactVisible { get; private set; }
    public float TelegraphProgress { get; private set; }
    public AttackPresentationFrame CurrentFrame => _frame;

    public void Present(AttackPresentationFrame frame)
    {
        if (frame.Timeline.Phase == AttackPhase.None)
        {
            _frame = default;
            GoldVisible = false;
            DangerVisible = false;
            ImpactVisible = false;
            TelegraphProgress = 0f;
            QueueRedraw();
            return;
        }

        _frame = frame.Validate();
        GoldVisible = frame.Timeline.Phase == AttackPhase.Telegraph;
        DangerVisible = GoldVisible && frame.Timeline.HasLockedTarget;
        ImpactVisible = frame.Timeline.Phase == AttackPhase.Active;
        TelegraphProgress = frame.Timeline.PhaseDurationSeconds <= 0f
            ? 1f
            : Mathf.Clamp(1f - frame.Timeline.PhaseRemainingSeconds / frame.Timeline.PhaseDurationSeconds, 0f, 1f);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!GoldVisible && !DangerVisible && !ImpactVisible) return;
        Vector2[] outline = LocalOutline(_frame.Shape);
        if (outline.Length < 3) return;
        if (DangerVisible)
        {
            Color fill = Danger;
            fill.A = .28f;
            DrawPolygon(outline, [fill]);
            DrawPolyline(Closed(outline), Danger, 3f, true);
        }
        if (GoldVisible)
        {
            Color contour = Gold;
            contour.A = .38f;
            DrawPolyline(Closed(outline), contour, 2f, true);
            Vector2[] progress = ProgressPath(outline, TelegraphProgress);
            if (progress.Length > 1) DrawPolyline(progress, Gold, 4f, true);
        }
        if (ImpactVisible)
        {
            Color white = Colors.White;
            white.A = .34f;
            DrawPolygon(outline, [white]);
            DrawPolyline(Closed(outline), Colors.White, 4f, true);
        }
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
