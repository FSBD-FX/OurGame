using Godot;

namespace Knight.Mechanisms;

// Optional paint only: the gate owns all occupancy, timing, and collision decisions.
public partial class CanyonPressureGateVisual : Node2D
{
    private static readonly Color Brass = new(.91f, .62f, .22f, 1f);
    private static readonly Color WarmWhite = new(1f, .91f, .69f, 1f);
    private static readonly Color DarkBronze = new(.22f, .16f, .11f, .94f);
    private static readonly Color Warning = new(1f, .43f, .15f, 1f);
    private CanyonPressurePlateGate _gate;

    // Paint uses the same transformed shape bounds as the committed door collision.
    public Rect2 ClosedSlabWorldRect
    {
        get
        {
            Rect2 local = _gate.DoorCollision.Shape.GetRect();
            Transform2D transform = _gate.DoorCollision.GlobalTransform;
            Vector2 a = transform * local.Position;
            Vector2 b = transform * new Vector2(local.End.X, local.Position.Y);
            Vector2 c = transform * new Vector2(local.Position.X, local.End.Y);
            Vector2 d = transform * local.End;
            Vector2 min = a.Min(b).Min(c).Min(d);
            Vector2 max = a.Max(b).Max(c).Max(d);
            return new Rect2(min, max - min);
        }
    }

    public override void _Ready() => _gate = GetParentOrNull<CanyonPressurePlateGate>();

    public CanyonPressureGatePaintFrame ClosingPaint
    {
        get
        {
            if (!GodotObject.IsInstanceValid(_gate) || !_gate.IsDangerTelegraphActive)
                return CanyonPressureGatePaintFrame.Empty;
            Rect2 slab = ClosedSlabWorldRect;
            Rect2 door = _gate.AntiCrushWorldRect;
            var strokes = new CanyonPressureGatePaintStroke[6];
            strokes[0] = new(ToLocal(new Vector2(slab.GetCenter().X, slab.Position.Y)),
                ToLocal(new Vector2(slab.GetCenter().X, slab.End.Y)), 3f, Warning);
            int remaining = Mathf.CeilToInt((1f - _gate.Frame.Phase01) * 5f);
            for (int tick = 0; tick < 5; tick++)
            {
                Vector2 from = ToLocal(new Vector2(door.Position.X + 32f,
                    door.Position.Y + 26f + tick * 25f));
                strokes[tick + 1] = new(from, from + new Vector2(13f, 0f), 4f,
                    tick < remaining ? Warning : DarkBronze);
            }
            return new CanyonPressureGatePaintFrame(GlobalTransform, strokes);
        }
    }

    public override void _Process(double delta)
    {
        if (GodotObject.IsInstanceValid(_gate)) QueueRedraw();
    }

    public override void _Draw()
    {
        if (!GodotObject.IsInstanceValid(_gate)) return;
        Rect2 plate = _gate.PlateWorldRect;
        Rect2 door = _gate.AntiCrushWorldRect;
        Vector2 plateStart = ToLocal(new Vector2(plate.Position.X, plate.End.Y - 8f));
        Vector2 plateSize = new(plate.Size.X, 8f);
        DrawRect(new Rect2(plateStart, plateSize), DarkBronze, true);
        DrawRect(new Rect2(plateStart, plateSize), Brass, false, 2f);
        DrawLine(plateStart + new Vector2(7f, 3f),
            plateStart + new Vector2(plateSize.X - 7f, 3f),
            _gate.IsOpen ? WarmWhite : Brass, 3f);

        Vector2 cableStart = ToLocal(new Vector2(plate.End.X, plate.End.Y - 4f));
        Vector2 cableBend = ToLocal(new Vector2(door.Position.X + 10f, plate.End.Y - 4f));
        Vector2 cableEnd = ToLocal(new Vector2(door.Position.X + 10f, door.Position.Y + 18f));
        Color connected = _gate.IsOpen ? WarmWhite : Brass;
        DrawLine(cableStart, cableBend, connected, 2f);
        DrawLine(cableBend, cableEnd, connected, 2f);
        DrawCircle(cableEnd, 5f, connected);

        Rect2 slab = ClosedSlabWorldRect;
        Vector2 doorTop = ToLocal(new Vector2(slab.GetCenter().X, slab.Position.Y));
        Vector2 doorBottom = ToLocal(new Vector2(slab.GetCenter().X, slab.End.Y));
        if (!_gate.IsOpen)
            DrawRect(new Rect2(ToLocal(slab.Position),
                ToLocal(slab.End) - ToLocal(slab.Position)), DarkBronze, true);
        CanyonPressureGatePaintFrame closing = ClosingPaint;
        if (closing.Strokes.Count > 0)
        {
            CanyonPressureGatePaintStroke stroke = closing.Strokes[0];
            DrawLine(stroke.From, stroke.To, stroke.Color, stroke.Width);
        }
        else DrawLine(doorTop, doorBottom, connected, _gate.IsOpen ? 3f : 5f);

        Vector2 inner = ToLocal(new Vector2(_gate.InteriorWorldRect.Position.X + 28f,
            _gate.InteriorWorldRect.Position.Y + 20f));
        DrawCircle(inner, 8f, DarkBronze);
        DrawCircle(inner, _gate.IsOpen ? 5f : 3f, connected);

        for (int index = 1; index < closing.Strokes.Count; index++)
        {
            CanyonPressureGatePaintStroke stroke = closing.Strokes[index];
            DrawLine(stroke.From, stroke.To, stroke.Color, stroke.Width);
        }
    }
}
