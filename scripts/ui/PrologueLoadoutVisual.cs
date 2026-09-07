using Godot;
using Knight.Player;

namespace Knight.Ui;

public readonly record struct PrologueLoadoutFrame(
    int ActiveSlot,
    bool Slot1Occupied,
    bool Slot2Occupied,
    bool Slot3Occupied,
    bool Slot4Occupied,
    PlayerActionRejectionReason Rejection)
{
    public static PrologueLoadoutFrame Create(int activeSlot, bool slot1, bool slot2, bool slot3,
        bool slot4, PlayerActionRejectionReason rejection) =>
        new(Mathf.Clamp(activeSlot, 1, 4), slot1, slot2, slot3, slot4, rejection);

    public bool IsOccupied(int slot) => slot switch
    {
        1 => Slot1Occupied, 2 => Slot2Occupied, 3 => Slot3Occupied, 4 => Slot4Occupied, _ => false
    };
}

public partial class PrologueLoadoutVisual : Control
{
    public PrologueLoadoutFrame CurrentFrame { get; private set; } =
        PrologueLoadoutFrame.Create(1, true, false, false, false, PlayerActionRejectionReason.None);

    public void Present(PrologueLoadoutFrame frame)
    {
        CurrentFrame = PrologueLoadoutFrame.Create(frame.ActiveSlot, frame.Slot1Occupied,
            frame.Slot2Occupied, frame.Slot3Occupied, frame.Slot4Occupied, frame.Rejection);
        for (int slot = 1; slot <= 4; slot++)
            if (GetNodeOrNull<Control>($"Slot{slot}/SelectedMarker") is { } selected)
                selected.Visible = CurrentFrame.ActiveSlot == slot;
        if (GetNodeOrNull<Control>("RejectionMarker") is { } rejected)
            rejected.Visible = CurrentFrame.Rejection != PlayerActionRejectionReason.None;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0f,
            new Vector2(Size.X / 220f, Size.Y / 36f));
        for (int slot = 1; slot <= 4; slot++)
        {
            Rect2 rect = new((slot - 1) * 52f, 0, 46, 36);
            bool active = CurrentFrame.ActiveSlot == slot;
            bool occupied = CurrentFrame.IsOccupied(slot);
            Color color = active ? new Color("d7b85b") : new Color("4b5c62");
            DrawRect(rect, new Color("0d141b"), true);
            DrawRect(rect, color, false, active ? 3f : 1f);
            DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(7, 24), slot.ToString(),
                HorizontalAlignment.Left, 14, 14, new Color("f2e6c9"));
            if (occupied) DrawCircle(rect.GetCenter() + new Vector2(7, 0), 4f, color);
            else DrawRect(new Rect2(rect.GetCenter() + new Vector2(3, -4), new Vector2(8, 8)),
                color, false, 2f);
        }
    }
}
