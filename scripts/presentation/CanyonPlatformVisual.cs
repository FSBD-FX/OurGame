using Godot;
using Knight.Abilities;
using Knight.Prologue.Seamless;

namespace Knight.Presentation;

/// <summary>
/// Replaceable, static terrain skin. Every mark stays inside the authored collision rectangle.
/// The separate ReadableEdge child remains the exact support-height cue for future art swaps.
/// </summary>
public partial class CanyonPlatformVisual : Node2D
{
    [Export] public GrappleSurfaceMaterial SurfaceMaterial { get; set; }
    [Export] public PrologueSectorId SectorId { get; set; }
    [Export] public Vector2 SkinSize { get; set; } = new(160f, 60f);
    [Export] public bool CatchShelf { get; set; }
    [Export] public bool UseCanyonDepthStaging { get; set; } = false;

    public string MaterialFamily => RoomVisualCatalog.CanyonMaterialFamily(SurfaceMaterial);

    public override void _Draw()
    {
        if (SkinSize.X <= 0f || SkinSize.Y <= 0f) return;
        DrawSetTransform(-SkinSize / 2f);
        bool stagedStone = UseCanyonDepthStaging && SurfaceMaterial == GrappleSurfaceMaterial.GripStone &&
            (SectorId is PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent);
        bool rockMass = stagedStone && SkinSize.Y >= 72f;
        if (rockMass)
        {
            DrawCanyonRockMass();
        }
        else if (stagedStone)
        {
            DrawDepthStagedStone();
        }
        else
        {
            Color fill = SurfaceMaterial switch
            {
                GrappleSurfaceMaterial.TempleMetal => new("394d50"),
                GrappleSurfaceMaterial.Ice => new("315a6d"),
                GrappleSurfaceMaterial.Forbidden => new("443943"),
                _ => SectorId switch
                {
                    PrologueSectorId.CanyonEntrance => new Color("344b49"),
                    PrologueSectorId.CanyonDescent => new Color("30454a"),
                    PrologueSectorId.CanyonBasin => new Color("414a40"),
                    PrologueSectorId.CanyonAscent => new Color("354650"),
                    PrologueSectorId.LostTemple => new Color("454943"),
                    _ => new Color("344b49")
                }
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
            DrawRect(new Rect2(0f, SkinSize.Y - shadow, SkinSize.X, shadow), new Color("182a31", .62f));
            DrawRect(new Rect2(0f, 6f, Mathf.Min(4f, SkinSize.X), Mathf.Max(0f, SkinSize.Y - 6f)),
                new Color("a1b1a5", .15f));
            DrawEndCaps();
            DrawRegionalSupportDetail();
        }
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

    // Graybox terrain finish, not a raster asset: all facets remain INSIDE the
    // actual rectangular collision; sparse strata distinguish soil/rock from metal.
    private void DrawCanyonRockMass()
    {
        float w = SkinSize.X, h = SkinSize.Y;
        DrawPolygon([new(0,0),new(w,0),new(w,h),new(0,h)],
            [new("4a4940"),new("393f3b"),new("242f30"),new("303a35")]);
        int layers = Mathf.Clamp((int)(h / 90f), 1, 6);
        for (int i = 0; i < layers; i++)
        {
            float a = h * i / layers;
            float b = h * (i + 1) / layers;
            float lean = Mathf.Min(12f, (b - a) * .2f);
            DrawColoredPolygon([new(0,a),new(w*.24f,a+lean),new(w*.61f,a),
                new(w,a+lean*.5f),new(w,b),new(w*.58f,b-lean),
                new(w*.19f,b),new(0,b-lean*.5f)],
                new Color(i % 2 == 0 ? "5d5946" : "1d3033", .23f));
            if (i > 0) DrawPolyline([new(0,a),new(w*.24f,a+lean),
                new(w*.61f,a),new(w,a+lean*.5f)],new Color("1a292d",.72f),2f,true);
        }
        // A compressed earth layer, not a bright false platform outside the body.
        float soil = Mathf.Min(14f,h*.15f);
        DrawColoredPolygon([new(0,0),new(w,0),new(w,soil*.7f),
            new(w*.72f,soil),new(w*.33f,soil*.6f),new(0,soil)],new Color("70654a",.7f));
        DrawLine(new(0,h-2f),new(w,h-2f),new Color("17262c",.85f),4f);
    }

    private void DrawDepthStagedStone()
    {
        float width = SkinSize.X;
        float height = SkinSize.Y;
        bool inRift = SectorId == PrologueSectorId.CanyonDescent;
        Color faceLeft = new(inRift ? "3b5054" : "45564e");
        Color faceRight = new(inRift ? "495b59" : "56604f");
        // Keep the face planar: light changes across its width, not in a rounded vertical ramp.
        DrawPolygon([new(0f, 0f), new(width, 0f), new(width, height), new(0f, height)],
            [faceLeft, faceRight, faceRight, faceLeft]);
        if (width < 24f || height < 16f) return;

        float top = 8f;
        float foot = Mathf.Min(5f, height * .12f);
        float bottom = height - foot;
        float end = Mathf.Min(10f, width * .10f);
        float wear = Mathf.Abs(Mathf.Sin(Position.X * .017f + Position.Y * .011f));
        // The unchanged six-pixel child remains the support cue; these are contained planes.
        DrawPolygon([new(0f, 0f), new(width, 0f), new(width, top), new(0f, top)],
            [new("5d6b60"), new("737b64"), new("737b64"), new("5d6b60")]);
        DrawColoredPolygon([new(0f, bottom), new(width * .31f, bottom - 1f),
                new(width * .68f, bottom + 1f), new(width, bottom - 2f),
                new(width, height), new(0f, height)], new Color("293e43"));
        DrawColoredPolygon([new(0f, top), new(end, top + 1f),
                new(end * .72f, bottom - 1f), new(0f, height)], new Color("30464a"));
        DrawColoredPolygon([new(width - end, top + 2f), new(width, top),
                new(width, height), new(width - end * .66f, bottom - 1f)],
            new Color(inRift ? "596962" : "69715d"));

        if (width >= 72f && height >= 24f)
        {
            // One broad abrasion, below the support edge; no tiny noise or repeated inset box.
            float left = end + (width - 2f * end) * (.12f + wear * .12f);
            float span = (width - 2f * end) * .36f;
            DrawColoredPolygon([new(left, top + 3f), new(left + span * .72f, top + 2f),
                    new(left + span, top + 5f), new(left + span * .81f, top + (bottom - top) * .48f),
                    new(left + span * .18f, top + (bottom - top) * .57f)],
                new Color("819077", .16f));
        }

        if (width >= 92f && height >= 20f && width >= height)
        {
            // Short steps get a legible split too. Longer shelves use unequal, broad stones,
            // not a brick grid; all joints stop below the authoritative support-height edge.
            int joints = Mathf.Clamp((int)(width / 180f), 1, 6);
            float cell = (width - 2f * end) / (joints + 1f);
            for (int index = 1; index <= joints; index++)
            {
                float stagger = ((index % 2 == 0 ? .12f : -.10f) + (wear - .5f) * .20f) * cell;
                float x = end + cell * index + stagger;
                float y = top + (bottom - top) * (.40f + (index % 2) * .18f);
                float lean = Mathf.Min(4f, cell * .07f);
                DrawColoredPolygon([new(x + 2f, top + 1f), new(x + cell * .64f, top + 2f),
                        new(x + cell * .58f, bottom - 1f), new(x + lean + 2f, bottom - 2f),
                        new(x - lean + 2f, y)], new Color("203a43", index % 2 == 0 ? .15f : .23f));
                DrawPolyline([new(x, top + 1f), new(x - lean, y), new(x + lean, bottom - 2f)],
                    new Color("20363e", .90f), 1.5f, true);
                DrawLine(new Vector2(x + 2f, top + 2f), new Vector2(x - lean + 2f, y),
                    new Color("8b9379", .29f), 1f, true);
            }
        }
        else if (height >= 72f && width >= 24f && height > width)
        {
            // Tall/narrow masonry gets sparse broken courses rather than a stretched bar.
            int joints = Mathf.Clamp((int)(height / 100f), 1, 7);
            float cell = (bottom - top) / (joints + 1f);
            for (int index = 1; index <= joints; index++)
            {
                float y = top + cell * (index + (index % 2 == 0 ? .12f : -.10f));
                float middle = width * (.43f + wear * .13f);
                DrawColoredPolygon([new(end, y + 2f), new(middle, y - 1f),
                        new(width - end, y + 3f), new(width - end, y + cell * .57f),
                        new(end, y + cell * .68f)], new Color("203a43", .21f));
                DrawPolyline([new(end, y), new(middle, y - 2f), new(width - end, y + 1f)],
                    new Color("20363e", .88f), 1.5f, true);
            }
        }

        if (CatchShelf && width >= 40f && height >= 20f)
        {
            // Bearing/contact shade sits underneath the unchanged brackets drawn by _Draw.
            float bracketHeight = Mathf.Min(18f, height - 10f);
            foreach (float x in new[] { 12f, width - 24f })
                DrawColoredPolygon([new(x - 2f, 8f), new(x + 12f, 8f),
                    new(x + 14f, 11f), new(x + 14f, 8f + bracketHeight),
                    new(x + 2f, 10f + bracketHeight), new(x - 2f, 8f + bracketHeight)],
                    new Color("1c333a", .66f));
        }
    }

    private void DrawEndCaps()
    {
        float cap = Mathf.Min(9f, SkinSize.X * .08f);
        if (cap < 2f || SkinSize.Y < 10f) return;
        Color dark = new("17272d", .62f);
        DrawRect(new Rect2(0f, 6f, cap, Mathf.Max(0f, SkinSize.Y - 12f)), dark);
        DrawRect(new Rect2(SkinSize.X - cap, 6f, cap, Mathf.Max(0f, SkinSize.Y - 12f)), dark);
        DrawLine(new Vector2(cap, 8f), new Vector2(cap, SkinSize.Y - 8f),
            new Color("75857d", .30f), 2f);
        DrawLine(new Vector2(SkinSize.X - cap, 8f),
            new Vector2(SkinSize.X - cap, SkinSize.Y - 8f), new Color("75857d", .30f), 2f);
    }

    private void DrawRegionalSupportDetail()
    {
        if (SkinSize.X < 72f || SkinSize.Y < 24f ||
            SurfaceMaterial != GrappleSurfaceMaterial.GripStone) return;
        Color detail = SectorId switch
        {
            PrologueSectorId.CanyonBasin => new Color("8d7950", .42f),
            PrologueSectorId.CanyonAscent => new Color("708793", .38f),
            PrologueSectorId.LostTemple => new Color("8a755c", .38f),
            PrologueSectorId.CanyonDescent => new Color("657d80", .34f),
            _ => new Color("667a6d", .32f)
        };
        float y = Mathf.Min(SkinSize.Y - 8f, 22f);
        if (SectorId is PrologueSectorId.CanyonAscent or PrologueSectorId.CanyonDescent)
        {
            DrawLine(new Vector2(18f, y), new Vector2(42f, 8f), detail, 3f, true);
            DrawLine(new Vector2(SkinSize.X - 42f, 8f),
                new Vector2(SkinSize.X - 18f, y), detail, 3f, true);
        }
        else
        {
            DrawRect(new Rect2(18f, 10f, 28f, Mathf.Min(8f, SkinSize.Y - 14f)), detail);
            DrawRect(new Rect2(SkinSize.X - 46f, 10f, 28f,
                Mathf.Min(8f, SkinSize.Y - 14f)), detail);
        }
    }

    private void DrawStone()
    {
        Color seam = new("243a40");
        Color lip = new("708077", .35f);
        float rowHeight = SectorId switch
        {
            PrologueSectorId.CanyonEntrance => 54f,
            PrologueSectorId.CanyonDescent => 76f,
            PrologueSectorId.CanyonBasin => 88f,
            PrologueSectorId.CanyonAscent => 66f,
            _ => 58f
        };
        for (int row = 0; row * rowHeight < SkinSize.Y; row++)
        {
            float top = row * rowHeight;
            float bottom = Mathf.Min(top + rowHeight, SkinSize.Y);
            if (top >= 8f)
            {
                DrawLine(new Vector2(3f, top), new Vector2(SkinSize.X - 3f, top), seam, 2f);
                DrawLine(new Vector2(4f, top + 2f), new Vector2(SkinSize.X - 4f, top + 2f), lip);
            }
            float step = SectorId == PrologueSectorId.CanyonBasin ? 154f : 118f;
            for (float x = row % 2 == 0 ? step * .82f : step * .40f;
                 x < SkinSize.X - 7f; x += step)
            {
                float start = Mathf.Max(8f, top + 3f);
                float end = bottom - 3f;
                if (end <= start) continue;
                float fracture = SectorId == PrologueSectorId.CanyonDescent ? 9f : 4f;
                DrawLine(new Vector2(x, start), new Vector2(x - fracture, (start + end) / 2f), seam, 2f);
                DrawLine(new Vector2(x - fracture, (start + end) / 2f),
                    new Vector2(x - 2f, end), seam, 2f);
            }
        }
        if (SkinSize.X >= 150f && SkinSize.Y >= 32f)
        {
            Color inset = SectorId switch
            {
                PrologueSectorId.CanyonBasin => new Color("857150", .25f),
                PrologueSectorId.CanyonDescent => new Color("65818a", .22f),
                PrologueSectorId.CanyonAscent => new Color("6b8290", .25f),
                PrologueSectorId.LostTemple => new Color("8a6d63", .25f),
                _ => new Color("73806e", .25f)
            };
            DrawRect(new Rect2(SkinSize.X * .28f, 12f,
                Mathf.Min(44f, SkinSize.X * .18f), Mathf.Min(13f, SkinSize.Y - 20f)), inset);
            DrawLine(new Vector2(SkinSize.X * .61f, SkinSize.Y - 10f),
                new Vector2(SkinSize.X * .72f, SkinSize.Y - 10f), inset, 3f);
        }
    }

    private void DrawMetal()
    {
        float panelHeight = SectorId == PrologueSectorId.LostTemple ? 74f : 92f;
        for (float y = 8f; y < SkinSize.Y - 8f; y += panelHeight)
        {
            float height = Mathf.Min(panelHeight - 8f, SkinSize.Y - y - 6f);
            if (height < 8f) continue;
            float step = SectorId == PrologueSectorId.CanyonAscent ? 154f : 132f;
            for (float x = 6f; x < SkinSize.X - 10f; x += step)
            {
                float width = Mathf.Min(step - 8f, SkinSize.X - x - 6f);
                if (width < 8f) continue;
                DrawRect(new Rect2(x, y, width, height), new Color("2c4149"));
                Color face = SectorId == PrologueSectorId.LostTemple
                    ? new Color("535554") : new Color("4b6264");
                DrawRect(new Rect2(x + 2f, y + 2f, width - 4f, height - 4f), face);
                DrawLine(new Vector2(x + 3f, y + 2f), new Vector2(x + width - 3f, y + 2f),
                    new Color("82948a", .6f));
                if (width < 24f || height < 16f) continue;
                if (SectorId == PrologueSectorId.CanyonAscent)
                    DrawLine(new Vector2(x + 14f, y + height - 7f),
                        new Vector2(x + Mathf.Min(width - 14f, 64f), y + 8f),
                        new Color("273d43", .45f), 4f, true);
                if (SectorId == PrologueSectorId.LostTemple)
                    DrawRect(new Rect2(x + width * .35f, y + 8f,
                        Mathf.Min(width * .30f, 28f), Mathf.Min(11f, height - 12f)),
                        new Color("80664f", .35f));
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
        for (float x = 6f; x < SkinSize.X - 8f; x += 146f)
        {
            float end = Mathf.Min(x + 140f, SkinSize.X - 6f);
            float mid = (x + end) / 2f;
            DrawColoredPolygon([new(x, 8f), new(mid, 10f),
                new(end, Mathf.Min(SkinSize.Y - 6f, 24f)),
                new(mid + 10f, SkinSize.Y - 6f), new(x + 5f, SkinSize.Y - 7f)],
                new Color("7194a5", .22f));
            if (SkinSize.Y > 25f && end - x > 24f)
                DrawPolyline([new(x + 8f, 13f), new(mid - 5f, SkinSize.Y * .48f),
                    new(end - 10f, SkinSize.Y - 7f)], new Color("9fbaca", .52f), 2f);
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
