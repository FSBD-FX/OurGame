using Godot;
using Knight.Abilities;

namespace Knight.Presentation;

/// <summary>
/// Replaceable, static terrain skin. Every mark stays inside the authored collision rectangle.
/// The separate ReadableEdge child remains the exact support-height cue for future art swaps.
/// </summary>
public partial class CanyonPlatformVisual : Node2D
{
    [Export] public GrappleSurfaceMaterial SurfaceMaterial { get; set; }
    [Export] public Vector2 SkinSize { get; set; } = new(160f, 60f);
    [Export] public bool CatchShelf { get; set; }

    public override void _Draw()
    {
        if (SkinSize.X <= 0f || SkinSize.Y <= 0f) return;
        DrawSetTransform(-SkinSize / 2f);
        Color fill = SurfaceMaterial switch
        {
            GrappleSurfaceMaterial.TempleMetal => new("40585d"),
            GrappleSurfaceMaterial.Ice => new("416577"),
            GrappleSurfaceMaterial.Forbidden => new("423d4b"),
            _ => new("3a5153")
        };
        DrawRect(new Rect2(Vector2.Zero, SkinSize), fill);
        switch (SurfaceMaterial)
        {
            case GrappleSurfaceMaterial.TempleMetal: DrawMetal(); break;
            case GrappleSurfaceMaterial.Ice: DrawIce(); break;
            case GrappleSurfaceMaterial.Forbidden: DrawSealed(); break;
            default: DrawStone(); break;
        }

        // Recesses and weathering are internal: never paint false support into a gap.
        float shadow = Mathf.Min(12f, SkinSize.Y * .2f);
        DrawRect(new Rect2(0f, SkinSize.Y - shadow, SkinSize.X, shadow), new Color("1e3039", .55f));
        DrawRect(new Rect2(0f, 6f, Mathf.Min(4f, SkinSize.X), Mathf.Max(0f, SkinSize.Y - 6f)),
            new Color("a1b1a5", .15f));
        if (CatchShelf && SkinSize.X >= 40f && SkinSize.Y >= 20f)
        {
            // Quiet paired brackets distinguish the safe return shelf, not an extra platform.
            foreach (float x in new[] { 12f, SkinSize.X - 24f })
            {
                DrawRect(new Rect2(x, 8f, 12f, Mathf.Min(18f, SkinSize.Y - 10f)), new Color("64776a"));
                DrawLine(new Vector2(x + 3f, 10f), new Vector2(x + 9f, 10f), new Color("aec6a0"), 2f);
            }
        }
    }

    private void DrawStone()
    {
        Color seam = new("243a40");
        Color lip = new("708077", .35f);
        const float rowHeight = 62f;
        for (int row = 0; row * rowHeight < SkinSize.Y; row++)
        {
            float top = row * rowHeight;
            float bottom = Mathf.Min(top + rowHeight, SkinSize.Y);
            if (top >= 8f)
            {
                DrawLine(new Vector2(3f, top), new Vector2(SkinSize.X - 3f, top), seam, 2f);
                DrawLine(new Vector2(4f, top + 2f), new Vector2(SkinSize.X - 4f, top + 2f), lip);
            }
            for (float x = row % 2 == 0 ? 106f : 54f; x < SkinSize.X - 4f; x += 112f)
            {
                float start = Mathf.Max(8f, top + 3f);
                float end = bottom - 3f;
                if (end <= start) continue;
                DrawLine(new Vector2(x, start), new Vector2(x - 4f, (start + end) / 2f), seam, 2f);
                DrawLine(new Vector2(x - 4f, (start + end) / 2f), new Vector2(x - 2f, end), seam, 2f);
            }
        }
    }

    private void DrawMetal()
    {
        float panelHeight = 92f;
        for (float y = 8f; y < SkinSize.Y - 8f; y += panelHeight)
        {
            float height = Mathf.Min(panelHeight - 8f, SkinSize.Y - y - 6f);
            if (height < 8f) continue;
            for (float x = 6f; x < SkinSize.X - 10f; x += 132f)
            {
                float width = Mathf.Min(124f, SkinSize.X - x - 6f);
                if (width < 8f) continue;
                DrawRect(new Rect2(x, y, width, height), new Color("2c4149"));
                DrawRect(new Rect2(x + 2f, y + 2f, width - 4f, height - 4f), new Color("4b6264"));
                DrawLine(new Vector2(x + 3f, y + 2f), new Vector2(x + width - 3f, y + 2f),
                    new Color("82948a", .6f));
                if (width < 24f || height < 16f) continue;
                foreach (float boltX in new[] { x + 7f, x + width - 7f })
                {
                    DrawCircle(new Vector2(boltX, y + 7f), 2f, new Color("aa9970"));
                    if (height > 30f)
                        DrawCircle(new Vector2(boltX, y + height - 7f), 2f, new Color("72857f"));
                }
            }
        }
    }

    private void DrawIce()
    {
        for (float x = 0f; x < SkinSize.X; x += 108f)
        {
            float end = Mathf.Min(x + 108f, SkinSize.X);
            float mid = (x + end) / 2f;
            DrawColoredPolygon([new(x, 6f), new(end, 6f), new(mid, SkinSize.Y)], new Color("7194a5", .24f));
            if (SkinSize.Y > 18f)
                DrawLine(new Vector2(x + 4f, SkinSize.Y - 5f),
                    new Vector2(Mathf.Max(x + 4f, end - 8f), 12f), new Color("9fbaca", .5f), 2f);
        }
    }

    private void DrawSealed()
    {
        if (SkinSize.Y < 18f) return;
        for (float x = 10f; x < SkinSize.X - 12f; x += 80f)
        {
            float end = Mathf.Min(x + 42f, SkinSize.X - 8f);
            DrawLine(new Vector2(x, SkinSize.Y - 8f), new Vector2(end, 10f), new Color("817780", .4f), 3f);
        }
    }
}
