using Godot;
using Knight.Abilities;
using Knight.Presentation;

namespace Knight.Prologue.Sample;

/// <summary>Sample masonry face; the separate support edge and physics stay authoritative.</summary>
public partial class BasinPlatformVisual : CanyonPlatformVisual
{
    [Export] public float DetailStartX { get; set; }

    public override void _Draw()
    {
        base._Draw();
        if (SurfaceMaterial != GrappleSurfaceMaterial.GripStone || SkinSize.Y < 20f) return;
        float start = Mathf.Clamp(DetailStartX, 0f, SkinSize.X);
        if (SkinSize.X - start < 32f) return;
        DrawSetTransform(-SkinSize / 2f);
        DrawRect(new Rect2(start, 5f, SkinSize.X - start, SkinSize.Y - 5f), new Color("304346"));
        int index = 0;
        for (float x = start; x < SkinSize.X - 4f; x += 86f, index++)
        {
            float end = Mathf.Min(x + 84f, SkinSize.X - 1f);
            if (end - x < 6f) continue;
            float bottom = SkinSize.Y - 3f;
            DrawColoredPolygon([new(x + 1f, 6f), new(end, 6f), new(end - 3f, bottom),
                new(x + 4f, bottom)], new Color(index % 3 == 1 ? "445957" : "3d5252"));
            DrawLine(new Vector2(x + 3f, 7f), new Vector2(end - 3f, 7f), new Color("879482", .48f));
            DrawLine(new Vector2(end - 3f, 10f), new Vector2(end - 5f, bottom), new Color("192e36"), 2f);
        }
        // Sparse old bronze bindings tie the slabs to the suspended architecture.
        foreach (float x in new[] { start + 12f, SkinSize.X - 22f })
        {
            DrawRect(new Rect2(x, 6f, 10f, SkinSize.Y - 8f), new Color("70684e"));
            DrawLine(new Vector2(x + 1f, 7f), new Vector2(x + 1f, SkinSize.Y - 3f), new Color("a49a70"), 1f);
            DrawCircle(new Vector2(x + 5f, 11f), 1.5f, new Color("b4aa80"));
        }
        DrawRect(new Rect2(start, SkinSize.Y - 3f, SkinSize.X - start, 3f), new Color("162933"));
    }
}
