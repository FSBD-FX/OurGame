using Godot;
using Knight.Abilities;
using System;

namespace Knight.Mechanisms;

// Optional, removable paint. The controller alone owns selection/time/collision.
public readonly record struct CanyonSteamJetPaint(Vector2[] WorldCorners, bool Warning, bool Danger);
public readonly record struct CanyonSteamDiverterPresentation(bool IsBound,
    CanyonSteamDiverterSide SelectedSide, bool HandoffVapor, Vector2 ControlWorldPosition,
    CanyonSteamJetPaint Left, CanyonSteamJetPaint Right, double CosmeticSeconds);
public readonly record struct CanyonSteamDiverterSupportPaint(Vector2[] LeftStemWorld, Vector2[] RightStemWorld);
public readonly record struct CanyonSteamDiverterWarningPaint(Vector2[] OutlineWorld, Vector2[][] MarksWorld);
public readonly record struct CanyonSteamDiverterPortPaint(Vector2 CenterWorld, bool Selected, Vector2[] ShaftWorld, Vector2[] ArrowheadWorld, float PressureRadius);
public readonly record struct CanyonSteamDiverterDirectionPaint(CanyonSteamDiverterPortPaint[] Ports, Vector2[] NeedleWorld);
public readonly record struct CanyonSteamDiverterSteamForm(Vector2[] ContourWorld, float Opacity, Color[] VertexColors);
public readonly record struct CanyonSteamDiverterSteamPaint(CanyonSteamDiverterSteamForm[] Forms, Vector2[] HardOutlineWorld, Vector2[][] FilamentsWorld);

public partial class CanyonSteamDiverterVisual : Node2D
{
    [Export] public NodePath ControllerPath { get; set; } = new("..");
    [Export] public NodePath ControlPath { get; set; } = new("../../Control");
    [Export] public NodePath SupportShapePath { get; set; } = new();

    // Borrowed floor contact, cosmetic only; no collision or clock authority.
    public CanyonSteamDiverterSupportPaint SupportPaint { get; private set; } = new(Array.Empty<Vector2>(), Array.Empty<Vector2>());
    // These are the actual warning strokes consumed by DrawFootprint, not a
    // second approximation of the paint or gameplay/collision authority.
    public CanyonSteamDiverterWarningPaint LeftWarningPaint { get; private set; } = new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
    public CanyonSteamDiverterWarningPaint RightWarningPaint { get; private set; } = new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
    // Actual consumed geometry, not an alternate test painter or physics owner.
    public CanyonSteamDiverterDirectionPaint DirectionPaint { get; private set; } = new(Array.Empty<CanyonSteamDiverterPortPaint>(), Array.Empty<Vector2>());
    public CanyonSteamDiverterSteamPaint LeftSteamPaint { get; private set; } = EmptySteamPaint();
    public CanyonSteamDiverterSteamPaint RightSteamPaint { get; private set; } = EmptySteamPaint();

    // Shared by paint and the component's on-screen explanation, never authority.
    public CanyonSteamDiverterPresentation Presentation { get; private set; } = new(false,
        CanyonSteamDiverterSide.Right, false, Vector2.Zero,
        new(Array.Empty<Vector2>(), false, false), new(Array.Empty<Vector2>(), false, false), 0d);

    private static readonly Color Ink = new("11171a");
    private static readonly Color Copper = new("38271c");
    private static readonly Color Brass = new("d4aa58");
    private static readonly Color WarmWhite = new("f0d481");
    private static readonly Color Vapor = new(.73f, .89f, .91f, .72f);
    private static readonly Color Warning = new(1f, .73f, .28f, .98f);
    private static readonly Color Danger = new(1f, .53f, .17f, .95f);
    private double _cosmeticSeconds;

    public override void _Process(double delta)
    {
        if (!IsInsideTree() || GetTree().Paused) return;
        if (double.IsFinite(delta) && delta > 0d)
        {
            _cosmeticSeconds += delta;
            if (!double.IsFinite(_cosmeticSeconds)) _cosmeticSeconds = 0d;
        }
        CanyonSteamDiverter controller = GetNodeOrNull<CanyonSteamDiverter>(ControllerPath);
        ThreadTarget2D control = GetNodeOrNull<ThreadTarget2D>(ControlPath);
        if (!Live(controller) || !controller.IsNodeReady() || !Live(control))
        { ClearPaint(); return; }
        CanyonSteamJet left = controller.GetNodeOrNull<CanyonSteamJet>(controller.LeftJetPath);
        CanyonSteamJet right = controller.GetNodeOrNull<CanyonSteamJet>(controller.RightJetPath);
        if (!TryJetPaint(left, out CanyonSteamJetPaint leftPaint) ||
            !TryJetPaint(right, out CanyonSteamJetPaint rightPaint) || !Finite(control.WorldAnchor))
        { ClearPaint(); return; }
        Presentation = new(true, controller.SelectedSide, controller.IsHandingOff,
            control.WorldAnchor, leftPaint, rightPaint, _cosmeticSeconds);
        SupportPaint = ReadSupportPaint(leftPaint, rightPaint);
        LeftWarningPaint = WarningPaint(leftPaint);
        RightWarningPaint = WarningPaint(rightPaint);
        DirectionPaint = ReadDirectionPaint(control.WorldAnchor, controller.SelectedSide);
        LeftSteamPaint = ReadSteamPaint(leftPaint, _cosmeticSeconds);
        RightSteamPaint = ReadSteamPaint(rightPaint, _cosmeticSeconds);
        QueueRedraw();
    }

    public override void _ExitTree() => ClearPaint();

    private void ClearPaint()
    {
        Presentation = new(false, CanyonSteamDiverterSide.Right, false, Vector2.Zero,
            new(Array.Empty<Vector2>(), false, false), new(Array.Empty<Vector2>(), false, false), _cosmeticSeconds);
        LeftWarningPaint = new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
        RightWarningPaint = new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
        SupportPaint = new(Array.Empty<Vector2>(), Array.Empty<Vector2>());
        DirectionPaint = new(Array.Empty<CanyonSteamDiverterPortPaint>(), Array.Empty<Vector2>());
        LeftSteamPaint = EmptySteamPaint(); RightSteamPaint = EmptySteamPaint();
        QueueRedraw();
    }

    private CanyonSteamDiverterSupportPaint ReadSupportPaint(CanyonSteamJetPaint left, CanyonSteamJetPaint right)
    {
        var empty = new CanyonSteamDiverterSupportPaint(Array.Empty<Vector2>(), Array.Empty<Vector2>());
        if (SupportShapePath == null || SupportShapePath.IsEmpty ||
            GetNodeOrNull(SupportShapePath) is not CollisionShape2D support || !Live(support) ||
            !GodotObject.IsInstanceValid(support.Shape) || support.Shape is not RectangleShape2D rectangle) return empty;
        // Borrow the actual rectangle and its full world transform. A vertical
        // riser intersects that rectangle's top edge, not an exported floor Y.
        Rect2 rect = rectangle.GetRect();
        if (!Finite(rect.Position) || !Finite(rect.Size) || rect.Size.X <= 0f || rect.Size.Y <= 0f) return empty;
        Vector2 topLeft = support.GlobalTransform * rect.Position;
        Vector2 topRight = support.GlobalTransform * new Vector2(rect.End.X, rect.Position.Y);
        if (!Finite(topLeft) || !Finite(topRight) || topLeft.X == topRight.X) return empty;
        return new(SupportStem(left.WorldCorners, topLeft, topRight), SupportStem(right.WorldCorners, topLeft, topRight));
    }

    private static Vector2[] SupportStem(Vector2[] corners, Vector2 topLeft, Vector2 topRight)
    {
        Vector2 mouth = (corners[2] + corners[3]) * .5f;
        float fraction = (mouth.X - topLeft.X) / (topRight.X - topLeft.X);
        if (!float.IsFinite(fraction) || fraction < 0f || fraction > 1f) return Array.Empty<Vector2>();
        Vector2 contact = new(mouth.X, topLeft.Lerp(topRight, fraction).Y);
        if (!Finite(contact) || contact.Y <= mouth.Y) return Array.Empty<Vector2>();
        return new[] { mouth, contact };
    }

    private static bool TryJetPaint(CanyonSteamJet jet, out CanyonSteamJetPaint paint)
    {
        paint = new(Array.Empty<Vector2>(), false, false);
        if (!Live(jet) || !jet.IsNodeReady() || !Live(jet.Collision) ||
            !GodotObject.IsInstanceValid(jet.Collision.Shape)) return false;
        // Borrow the actual shape; presentation never owns/disposes/changes it.
        Rect2 rect = jet.Collision.Shape.GetRect();
        if (!Finite(rect.Position) || !Finite(rect.Size) || rect.Size.X <= 0f || rect.Size.Y <= 0f) return false;
        Transform2D transform = jet.Collision.GlobalTransform;
        Vector2[] corners = { transform * rect.Position, transform * new Vector2(rect.End.X, rect.Position.Y),
            transform * rect.End, transform * new Vector2(rect.Position.X, rect.End.Y) };
        foreach (Vector2 point in corners) if (!Finite(point)) return false;
        bool warning = !jet.DisabledForRun && jet.Frame.Telegraphing && jet.Collision.Disabled && !jet.Monitoring;
        bool danger = !jet.DisabledForRun && jet.Frame.HazardActive && !jet.Collision.Disabled && jet.Monitoring;
        paint = new(corners, warning, danger);
        return true;
    }

    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) &&
        !node.IsQueuedForDeletion() && node.IsInsideTree();
    private static bool Finite(Vector2 point) => float.IsFinite(point.X) && float.IsFinite(point.Y);

    public override void _Draw()
    {
        CanyonSteamDiverterPresentation frame = Presentation;
        if (!frame.IsBound) return;
        DrawFootprint(frame.Left, LeftWarningPaint, LeftSteamPaint);
        DrawFootprint(frame.Right, RightWarningPaint, RightSteamPaint);
        DrawSupport(SupportPaint.LeftStemWorld);
        DrawSupport(SupportPaint.RightStemWorld);
        DrawPipe(frame.ControlWorldPosition, frame.Left.WorldCorners, frame.SelectedSide == CanyonSteamDiverterSide.Left);
        DrawPipe(frame.ControlWorldPosition, frame.Right.WorldCorners, frame.SelectedSide == CanyonSteamDiverterSide.Right);
        Vector2 control = ToLocal(frame.ControlWorldPosition);
        DrawCircle(control, 24f, Ink); DrawCircle(control, 19f, Copper);
        DrawArc(control, 19f, 0f, Mathf.Tau, 28, Brass, 3f, true);
        foreach (CanyonSteamDiverterPortPaint port in DirectionPaint.Ports) DrawPort(port);
        DrawLine(control + new Vector2(0, -11), control + new Vector2(0, 11), Brass, 3f);
        if (DirectionPaint.NeedleWorld.Length == 2)
            DrawLine(ToLocal(DirectionPaint.NeedleWorld[0]), ToLocal(DirectionPaint.NeedleWorld[1]), WarmWhite, 5f);
        DrawCircle(control, 4f, WarmWhite);
        if (frame.HandoffVapor) DrawHandoffVapor(frame.ControlWorldPosition, frame.CosmeticSeconds);
    }

    private void DrawSupport(Vector2[] stem)
    {
        if (stem.Length != 2) return;
        DrawLine(ToLocal(stem[0]), ToLocal(stem[1]), Ink, 9f, true);
        DrawLine(ToLocal(stem[0]), ToLocal(stem[1]), Copper, 4f, true);
        // A small socket, not a broad landing edge or expanded hazard body.
        DrawCircle(ToLocal(stem[1]), 6f, Ink);
        DrawArc(ToLocal(stem[1]), 4f, 0f, Mathf.Tau, 12, Brass, 2f, true);
    }

    private void DrawPipe(Vector2 control, Vector2[] corners, bool selected)
    {
        Vector2 nozzle = (corners[2] + corners[3]) * .5f;
        float direction = nozzle.X < control.X ? -1f : 1f;
        Vector2 branch = control + new Vector2(direction * 53f, 0f);
        Vector2[] pipe = { ToLocal(control), ToLocal(branch), ToLocal(new Vector2(branch.X, nozzle.Y + 18f)),
            ToLocal(nozzle + new Vector2(0, 18f)), ToLocal(nozzle) };
        DrawPolyline(pipe, Ink, 11f, true);
        DrawPolyline(pipe, selected ? Brass : Copper, selected ? 5f : 4f, true);
        DrawCircle(ToLocal(nozzle), 9f, Ink);
        DrawArc(ToLocal(nozzle), 6f, 0f, Mathf.Tau, 16, selected ? WarmWhite : Brass, 3f, true);
    }

    private CanyonSteamDiverterDirectionPaint ReadDirectionPaint(Vector2 control, CanyonSteamDiverterSide side)
    {
        int selected = side == CanyonSteamDiverterSide.Left ? -1 : 1;
        var ports = new CanyonSteamDiverterPortPaint[2];
        for (int i = 0; i < 2; i++)
        {
            int direction = i == 0 ? -1 : 1;
            Vector2 coupling = control + new Vector2(direction * 53f, 0f);
            ports[i] = new(coupling, selected == direction, Array.Empty<Vector2>(), Array.Empty<Vector2>(), 8f);
        }
        // The old needle offset was visual-local, unlike the world-space pipe
        // fittings. Preserve that coordinate system through the world snapshot.
        Vector2 localControl = ToLocal(control);
        return new(ports, new[] { ToGlobal(localControl), ToGlobal(localControl + new Vector2(selected * 14, -7)) });
    }
    private void DrawPort(CanyonSteamDiverterPortPaint port)
    {
        Color accent = port.Selected ? WarmWhite : Brass.Darkened(.48f);
        Vector2 center = ToLocal(port.CenterWorld);
        // Small pipe coupling/pressure window: no directional arrow outside the wheel.
        DrawCircle(center, port.PressureRadius, Ink);
        DrawCircle(center, port.PressureRadius - 2f, Copper);
        DrawArc(center, port.PressureRadius - 2f, 0f, Mathf.Tau, 20, accent, 2f, true);
        DrawCircle(center, port.PressureRadius - 5f, port.Selected ? WarmWhite : Copper.Lightened(.15f));
    }

    // Interrupted edge wisps disclose the coming full range. They are not
    // persistent flow features, continuous rails, a closed frame or rungs.
    private CanyonSteamDiverterWarningPaint WarningPaint(CanyonSteamJetPaint jet)
    {
        if (!jet.Warning) return new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
        Vector2[] world = jet.WorldCorners;
        Vector2 topLeft = ToLocal(world[0]), topRight = ToLocal(world[1]);
        Vector2 bottomLeft = ToLocal(world[3]), bottomRight = ToLocal(world[2]);
        Vector2 sideEdge = topLeft - bottomLeft, top = topRight - topLeft;
        if (sideEdge.LengthSquared() <= 0f || top.LengthSquared() <= 0f)
            return new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
        // DrawFootprint uses a four-local-unit dark under-stroke. Move the
        // consumed centerline inward by its radius plus .5 AA and .1 float
        // reserve along BOTH normals; rotated/scaled visual transforms are
        // already represented in these local spans, unlike world-width math.
        float widthSpan = Mathf.Abs((bottomRight - bottomLeft).Cross(sideEdge)) / sideEdge.Length();
        float heightSpan = Mathf.Abs((bottomLeft - topLeft).Cross(top)) / top.Length();
        if (!float.IsFinite(widthSpan) || !float.IsFinite(heightSpan) ||
            widthSpan <= 5.2f || heightSpan <= 5.2f)
            return new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
        float edgeInset = Mathf.Max(.025f, 2.6f / widthSpan);
        float heightInset = Mathf.Max(.02f, 2.6f / heightSpan);
        if (edgeInset >= .46f || heightInset >= .46f)
            return new(Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
        float innerInset = Mathf.Max(.065f, edgeInset + .04f);
        var ticks = new Vector2[8][];
        for (int side = 0; side < 2; side++) for (int tick = 0; tick < 4; tick++)
        {
            float start = .02f + tick * .25f + side * .015f;
            float low = Mathf.Clamp(start, heightInset, 1f - heightInset);
            float high = Mathf.Clamp(start + .13f + (tick % 2) * .04f, heightInset, 1f - heightInset);
            float outside = side == 0 ? edgeInset : 1f - edgeInset;
            float inside = side == 0 ? innerInset : 1f - innerInset;
            ticks[side * 4 + tick] = new[] { SteamPoint(world, outside, low), SteamPoint(world, inside, high) };
        }
        return new(Array.Empty<Vector2>(), ticks);
    }

    private static CanyonSteamDiverterSteamPaint EmptySteamPaint() =>
        new(Array.Empty<CanyonSteamDiverterSteamForm>(), Array.Empty<Vector2>(), Array.Empty<Vector2[]>());
    private static CanyonSteamDiverterSteamPaint ReadSteamPaint(CanyonSteamJetPaint jet, double cosmeticSeconds)
    {
        if (!jet.Warning && !jet.Danger) return EmptySteamPaint();
        Vector2[] world = jet.WorldCorners;
        // Overlapping, bowed vapor ribbons start/end at different heights.
        // Pale outer vapor carries the extent; warmth is confined to the mouth,
        // rather than three ochre layers running uninterrupted to the top.
        float phase = (float)(cosmeticSeconds % 60d);
        Color pale = new(.91f, .94f, .87f);
        Color warmPale = new(1f, .91f, .73f);
        var forms = new[] {
            SteamLobe(world, phase, .00f, jet.Danger ? .33f : .37f,
                jet.Danger ? .29f : .21f, .65f, .13f, 0f, jet.Danger ? .30f : .20f, pale),
            SteamLobe(world, phase, .07f, jet.Danger ? .66f : .63f,
                jet.Danger ? .27f : .20f, .77f, .14f, 1.4f, jet.Danger ? .26f : .18f, warmPale),
            SteamLobe(world, phase, .24f, .48f,
                jet.Danger ? .27f : .19f, .89f, .09f, 2.7f, jet.Danger ? .23f : .15f, pale),
            SteamLobe(world, phase, .00f, .51f,
                jet.Danger ? .14f : .09f, .23f, .11f, .8f, jet.Danger ? .28f : .16f,
                new Color(1f, .74f, .36f)) };
        return new(forms, Array.Empty<Vector2>(), SteamFlow(world, phase, jet.Danger));
    }

    private static Vector2 SteamPoint(Vector2[] world, float across, float height) =>
        world[3].Lerp(world[2], across).Lerp(world[0].Lerp(world[1], across), height);

    private static CanyonSteamDiverterSteamForm SteamLobe(Vector2[] world, float phase,
        float bottom, float targetCenter, float width, float shoulder, float crownRise,
        float offset, float opacity, Color color)
    {
        const int sections = 10, crownPoints = 8;
        var contour = new Vector2[(sections + 1) * 2 + crownPoints];
        var heights = new float[contour.Length];
        float shoulderCenter = .5f, shoulderSpread = 0f;
        // Bowed flanks expand then disperse, instead of holding a constant
        // width for nearly the whole height. Every point still maps through
        // all four real borrowed corners; no world-Y or AABB approximation.
        for (int step = 0; step <= sections; step++)
        {
            float progress = step / (float)sections;
            float height = Mathf.Lerp(bottom, shoulder, progress);
            float flow = height * 9f - phase * 2.4f + offset;
            float center = .5f + (targetCenter - .5f) * (1f - Mathf.Exp(-progress * 5f)) +
                .018f * Mathf.Sin(flow) * Mathf.Sin(progress * Mathf.Pi * .5f);
            float spread = width * (.14f + .86f * Mathf.Sin(progress * Mathf.Pi * .80f)) *
                (.95f + .04f * Mathf.Sin(flow + .7f));
            if (step == sections) { shoulderCenter = center; shoulderSpread = spread; }
            contour[step] = SteamPoint(world, center - spread, height);
            contour[contour.Length - 1 - step] = SteamPoint(world, center + spread, height);
            heights[step] = heights[contour.Length - 1 - step] = height;
        }
        for (int point = 1; point <= crownPoints; point++)
        {
            float across = point / (float)(crownPoints + 1);
            float height = shoulder + crownRise * Mathf.Sin(across * Mathf.Pi) +
                .008f * Mathf.Sin(across * Mathf.Tau * 2f + phase * 1.7f + offset) * Mathf.Sin(across * Mathf.Pi);
            contour[sections + point] = SteamPoint(world,
                shoulderCenter + (across * 2f - 1f) * shoulderSpread, height);
            heights[sections + point] = height;
        }
        var colors = new Color[contour.Length];
        for (int vertex = 0; vertex < contour.Length; vertex++)
        {
            float height = Mathf.Clamp((heights[vertex] - bottom) / (shoulder + crownRise - bottom), 0f, 1f);
            float fade = Mathf.Clamp((height - .25f) / .75f, 0f, 1f);
            fade = fade * fade * (3f - 2f * fade);
            // Retain a faint upper body, not a shorter apparent safe zone.
            // Raised ribbons also emerge softly from the overlapping lower
            // body, avoiding horizontal cut lines or detached stacked disks.
            float emergence = Mathf.Clamp(height / .18f, 0f, 1f);
            emergence = emergence * emergence * (3f - 2f * emergence);
            float baseAlpha = bottom > 0f ? .06f + .94f * emergence : .65f + .35f * emergence;
            colors[vertex] = new Color(color.R, color.G, color.B, opacity * baseAlpha * (1f - .86f * fade));
        }
        return new(contour, opacity, colors);
    }

    private static Vector2[][] SteamFlow(Vector2[] world, float phase, bool danger)
    {
        var paths = new Vector2[3][];
        for (int stream = 0; stream < paths.Length; stream++)
        {
            // Persistent short curls travel upward, then respawn at the mouth;
            // the whole path stays inside at both ends of that cosmetic wrap.
            float height = .04f + Mathf.PosMod(phase * .36f + stream * .26f, .78f);
            float center = .5f + (stream - 1) * (danger ? .20f : .07f);
            paths[stream] = new Vector2[9];
            for (int point = 0; point < paths[stream].Length; point++)
            {
                float along = point / 8f;
                float across = center + (danger ? .075f : .045f) * Mathf.Sin(along * Mathf.Tau * .85f + stream * 1.7f);
                paths[stream][point] = SteamPoint(world, across, height + along * .13f);
            }
        }
        return paths;
    }
    private void DrawFootprint(CanyonSteamJetPaint jet, CanyonSteamDiverterWarningPaint warningPaint, CanyonSteamDiverterSteamPaint steamPaint)
    {
        if (!jet.Warning && !jet.Danger) return;
        Color accent = jet.Danger ? Danger : Warning;
        for (int index = 0; index < steamPaint.Forms.Length; index++)
        {
            CanyonSteamDiverterSteamForm form = steamPaint.Forms[index];
            DrawPolygon(Array.ConvertAll(form.ContourWorld, ToLocal), form.VertexColors);
        }
        foreach (Vector2[] filament in steamPaint.FilamentsWorld)
            if (filament.Length >= 2)
            {
                Vector2[] local = Array.ConvertAll(filament, ToLocal);
                DrawPolyline(local, new Color(Ink.R, Ink.G, Ink.B, .12f), jet.Danger ? 5f : 3f, true);
                DrawPolyline(local, new Color(WarmWhite.R, WarmWhite.G, WarmWhite.B, jet.Danger ? .68f : .50f), jet.Danger ? 2f : 1.3f, true);
            }
        if (jet.Warning)
        {
            if (warningPaint.OutlineWorld.Length >= 2)
                DrawPolyline(Array.ConvertAll(warningPaint.OutlineWorld, ToLocal), accent, 2f, true);
            foreach (Vector2[] mark in warningPaint.MarksWorld)
            {
                DrawLine(ToLocal(mark[0]), ToLocal(mark[1]), new Color(Ink.R, Ink.G, Ink.B, .24f), 4f, true);
                DrawLine(ToLocal(mark[0]), ToLocal(mark[1]), new Color(Warning.R, Warning.G, Warning.B, .48f), 1.5f, true);
            }
            return;
        }
        if (steamPaint.HardOutlineWorld.Length >= 2)
            DrawPolyline(Array.ConvertAll(steamPaint.HardOutlineWorld, ToLocal), accent, 4f, true);
    }

    private void DrawHandoffVapor(Vector2 control, double cosmeticSeconds)
    {
        float phase = (float)(cosmeticSeconds % 60d);
        // Low pale puffs over the valve only: no full hazard footprint, collider
        // or new target. Cosmetic phase never completes the handoff.
        for (int puff = 0; puff < 3; puff++)
        {
            float sway = Mathf.Sin(phase * 5f + puff * 2f) * 3f;
            Vector2 center = control + new Vector2((puff - 1) * 11f + sway, -25f - puff * 5f);
            DrawCircle(ToLocal(center), 8f + puff * 2f, Vapor);
        }
        for (int dot = -1; dot <= 1; dot++) DrawCircle(ToLocal(control + new Vector2(dot * 8f, 10f)), 2.5f, Vapor);
    }
}
