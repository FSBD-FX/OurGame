using Godot;

namespace Knight.Prologue.Sample;

/// <summary>
/// Sample-only, retained background drawing. Architecture describes depth, never collision or
/// traversable ledges; the sample builder owns the two background Z slots and gameplay skins.
/// </summary>
public partial class BasinEnvironmentVisual : Node2D
{
    [Export] public Rect2 SectorBounds { get; set; } = new(0f, 0f, 1600f, 720f);
    [Export] public bool DistantLayer { get; set; }

    private static readonly Color Rift = new("101c23");
    private static readonly Color FarRock = new("182a31");
    private static readonly Color MiddleRock = new("1e343a");
    private static readonly Color Rock = new("263e41");
    private static readonly Color RockFace = new("2e484a");
    private static readonly Color RockShadow = new("1b3035");
    private static readonly Color Stone = new("344747");
    private static readonly Color StoneFace = new("3d5050");
    private static readonly Color StoneShade = new("293d40");
    private static readonly Color Mortar = new("1e3035");
    private static readonly Color Bronze = new("565348");
    private static readonly Color BronzeShade = new("363f3b");
    private static readonly Color Patina = new("526765");

    public override void _Draw()
    {
        if (SectorBounds.Size.X <= 0f || SectorBounds.Size.Y <= 0f) return;
        DrawSetTransform(SectorBounds.Position, 0f,
            new Vector2(SectorBounds.Size.X / 1600f, SectorBounds.Size.Y / 720f));
        if (DistantLayer) DrawAtmosphericRift();
        else DrawBuriedSanctum();
    }

    private void DrawAtmosphericRift()
    {
        DrawRect(new Rect2(0f, 0f, 1600f, 720f), Rift);

        // Large sloping planes make the open rift read at gameplay scale, without texture noise.
        DrawColoredPolygon([
            new(0f, 0f), new(820f, 0f), new(774f, 72f), new(758f, 163f),
            new(705f, 253f), new(690f, 365f), new(620f, 446f), new(661f, 568f),
            new(570f, 720f), new(0f, 720f)
        ], FarRock);
        DrawColoredPolygon([
            new(1422f, 0f), new(1600f, 0f), new(1600f, 720f), new(1290f, 720f),
            new(1346f, 598f), new(1325f, 491f), new(1377f, 397f), new(1354f, 249f),
            new(1397f, 162f)
        ], new Color("182b32"));
        DrawColoredPolygon([
            new(0f, 184f), new(238f, 142f), new(291f, 214f), new(413f, 177f),
            new(500f, 250f), new(534f, 349f), new(617f, 398f), new(642f, 526f),
            new(770f, 652f), new(823f, 720f), new(0f, 720f)
        ], MiddleRock);
        DrawColoredPolygon([
            new(1600f, 302f), new(1545f, 333f), new(1503f, 413f), new(1510f, 498f),
            new(1447f, 596f), new(1462f, 674f), new(1422f, 720f), new(1600f, 720f)
        ], MiddleRock);

        // Distant ruined uprights stay visibly behind the main monumental arch.
        DrawColoredPolygon([
            new(724f, 524f), new(724f, 263f), new(738f, 250f), new(745f, 215f),
            new(757f, 228f), new(779f, 223f), new(781f, 524f)
        ], new Color("203239"));
        DrawColoredPolygon([
            new(812f, 501f), new(818f, 306f), new(830f, 276f), new(856f, 287f),
            new(862f, 501f)
        ], new Color("1d3037"));
        DrawLine(new Vector2(744f, 279f), new Vector2(744f, 459f),
            new Color("496062", .17f), 3f, true);

        // Shafts are broad, dim planes, not emissive targets or actor-covering foreground FX.
        DrawColoredPolygon([
            new(1053f, 0f), new(1131f, 0f), new(1359f, 481f), new(1195f, 469f)
        ], new Color("91b7ad", .025f));
        DrawColoredPolygon([
            new(1234f, 0f), new(1252f, 0f), new(1430f, 423f), new(1390f, 432f)
        ], new Color("a1b7a8", .022f));
        DrawColoredPolygon([
            new(652f, 496f), new(812f, 457f), new(1003f, 477f), new(1150f, 459f),
            new(1335f, 486f), new(1481f, 462f), new(1600f, 515f), new(1600f, 720f),
            new(640f, 720f)
        ], new Color("365456", .105f));
    }

    private void DrawBuriedSanctum()
    {
        DrawLeftStrata();
        DrawRightStrata();

        // The piers frame the camp and combat; no masonry is drawn in the quiet combat pocket
        // x=1200..1450, y=510..610. The landing shelf and floor have separate accurate skins.
        DrawPier(843f, 387f, 57f, 327f, false);
        DrawPier(1465f, 385f, 65f, 329f, true);
        DrawMonumentalArch();
        DrawHangingSupports();
        DrawBuriedFragments();
    }

    private void DrawLeftStrata()
    {
        DrawColoredPolygon([
            new(0f, 0f), new(426f, 0f), new(472f, 61f), new(455f, 139f),
            new(506f, 187f), new(517f, 259f), new(582f, 288f), new(600f, 376f),
            new(661f, 417f), new(664f, 493f), new(735f, 529f), new(783f, 625f),
            new(830f, 673f), new(811f, 720f), new(0f, 720f)
        ], RockShadow);
        DrawColoredPolygon([
            new(0f, 0f), new(337f, 0f), new(385f, 84f), new(371f, 132f),
            new(431f, 172f), new(414f, 250f), new(477f, 285f), new(469f, 358f),
            new(550f, 393f), new(537f, 484f), new(623f, 511f), new(655f, 610f),
            new(733f, 652f), new(765f, 720f), new(0f, 720f)
        ], Rock);
        DrawColoredPolygon([
            new(350f, 157f), new(430f, 174f), new(414f, 250f), new(477f, 285f),
            new(462f, 324f), new(383f, 272f), new(396f, 221f)
        ], RockFace);
        DrawColoredPolygon([
            new(407f, 365f), new(470f, 351f), new(550f, 393f), new(537f, 449f),
            new(478f, 416f), new(438f, 422f)
        ], new Color("30494b"));
        DrawColoredPolygon([
            new(552f, 530f), new(623f, 511f), new(655f, 610f), new(722f, 650f),
            new(690f, 678f), new(614f, 635f)
        ], new Color("304749"));

        DrawPolyline([new(226f, 200f), new(323f, 216f), new(387f, 267f), new(440f, 278f)],
            new Color("152a30"), 5f, true);
        DrawPolyline([new(247f, 358f), new(337f, 345f), new(409f, 368f)], Mortar, 5f, true);
        DrawPolyline([new(397f, 486f), new(469f, 477f), new(529f, 504f), new(571f, 502f)],
            Mortar, 4f, true);
        DrawPolyline([new(490f, 613f), new(564f, 628f), new(603f, 661f)], Mortar, 5f, true);

        // Tilted broken stone ribs are embedded in the cliff, not horizontal pseudo-platforms.
        DrawColoredPolygon([
            new(568f, 393f), new(594f, 380f), new(722f, 514f), new(703f, 538f)
        ], StoneShade);
        DrawLine(new Vector2(587f, 399f), new Vector2(704f, 521f),
            new Color("4c5b57", .42f), 3f, true);
        DrawLine(new Vector2(624f, 434f), new Vector2(642f, 420f), Mortar, 4f, true);
        DrawLine(new Vector2(676f, 490f), new Vector2(693f, 476f), Mortar, 4f, true);
    }

    private void DrawRightStrata()
    {
        DrawColoredPolygon([
            new(1600f, 0f), new(1536f, 0f), new(1549f, 99f), new(1514f, 166f),
            new(1530f, 232f), new(1540f, 307f), new(1571f, 370f), new(1555f, 444f),
            new(1540f, 529f), new(1552f, 603f), new(1527f, 663f), new(1508f, 720f),
            new(1600f, 720f)
        ], RockShadow);
        DrawColoredPolygon([
            new(1600f, 106f), new(1568f, 133f), new(1577f, 220f), new(1558f, 254f),
            new(1585f, 348f), new(1579f, 450f), new(1563f, 511f), new(1582f, 610f),
            new(1546f, 697f), new(1556f, 720f), new(1600f, 720f)
        ], Rock);
        DrawLine(new Vector2(1551f, 171f), new Vector2(1577f, 220f),
            new Color("3b5352", .47f), 3f, true);
        DrawPolyline([new(1569f, 550f), new(1583f, 578f), new(1600f, 584f)], Mortar, 4f, true);
    }

    private void DrawPier(float x, float top, float width, float height, bool right)
    {
        DrawColoredPolygon([
            new(x - 9f, top + 11f), new(x + width - 7f, top),
            new(x + width + 12f, top + height), new(x - 15f, top + height)
        ], StoneShade);
        DrawColoredPolygon([
            new(x, top), new(x + width - 10f, top + 5f),
            new(x + width - 3f, top + height), new(x - 5f, top + height)
        ], Stone);
        DrawColoredPolygon([
            new(x + 7f, top + 11f), new(x + 18f, top + 13f),
            new(x + 16f, top + height), new(x + 3f, top + height)
        ], new Color("4a5b57", .31f));

        for (int index = 1; index < 6; index++)
        {
            float y = top + index * 54f;
            DrawLine(new Vector2(x - 1f, y), new Vector2(x + width - 8f, y + 2f), Mortar, 4f, true);
            if (index % 2 == 0)
                DrawLine(new Vector2(x + width * .55f, y + 4f),
                    new Vector2(x + width * .55f - 4f, y + 49f), Mortar, 2f, true);
        }

        float bandY = top + (right ? 82f : 115f);
        DrawColoredPolygon([
            new(x - 4f, bandY), new(x + width - 4f, bandY + 2f),
            new(x + width - 3f, bandY + 16f), new(x - 5f, bandY + 13f)
        ], BronzeShade);
        DrawLine(new Vector2(x + 1f, bandY + 3f), new Vector2(x + width - 8f, bandY + 5f),
            Bronze, 2f, true);
        DrawCircle(new Vector2(x + 9f, bandY + 8f), 2f, Bronze);
        DrawCircle(new Vector2(x + width - 16f, bandY + 10f), 2f, Bronze);
    }

    private void DrawMonumentalArch()
    {
        Vector2 center = new(1183f, 397f);
        const float outer = 340f;
        const float inner = 284f;

        // A missing crown and uneven stone courses give the landmark an excavated silhouette.
        for (int index = 0; index < 8; index++)
        {
            float start = Mathf.Pi + index * .153f;
            DrawArchStone(center, outer, inner, start + .006f, start + .145f, index);
        }
        for (int index = 0; index < 9; index++)
        {
            float start = 4.803f + index * .163f;
            DrawArchStone(center, outer, inner, start + .006f,
                Mathf.Min(start + .155f, Mathf.Tau), index + 3);
        }

        DrawColoredPolygon([
            new(1033f, 95f), new(1053f, 89f), new(1067f, 113f),
            new(1060f, 132f), new(1046f, 122f), new(1043f, 108f)
        ], StoneFace);
        DrawColoredPolygon([
            new(1213f, 59f), new(1237f, 62f), new(1242f, 79f),
            new(1227f, 93f), new(1224f, 77f)
        ], StoneShade);

        DrawArchClamp(center, outer, inner, 3.66f);
        DrawArchClamp(center, outer, inner, 5.66f);
    }

    private void DrawArchStone(Vector2 center, float outer, float inner,
        float start, float end, int course)
    {
        float middle = (start + end) * .5f;
        float outerRadius = outer + (course % 3 == 0 ? 3f : 0f);
        DrawColoredPolygon([
            Radial(center, outerRadius, start), Radial(center, outerRadius, middle),
            Radial(center, outerRadius, end), Radial(center, inner, end),
            Radial(center, inner, middle), Radial(center, inner, start)
        ], course % 3 == 0 ? StoneFace : Stone);
        DrawColoredPolygon([
            Radial(center, outerRadius, start), Radial(center, outerRadius, middle),
            Radial(center, outerRadius, end), Radial(center, outerRadius - 10f, end),
            Radial(center, outerRadius - 10f, middle), Radial(center, outerRadius - 10f, start)
        ], StoneShade);
        DrawLine(Radial(center, inner + 4f, start + .012f),
            Radial(center, inner + 4f, end - .012f), new Color("66756a", .25f), 2f, true);
        if (course % 4 == 1)
            DrawPolyline([
                Radial(center, outer - 11f, middle - .021f),
                Radial(center, outer - 27f, middle + .008f),
                Radial(center, outer - 34f, middle - .011f)
            ], Mortar, 2f, true);
    }

    private void DrawArchClamp(Vector2 center, float outer, float inner, float angle)
    {
        DrawColoredPolygon([
            Radial(center, outer + 5f, angle - .023f), Radial(center, outer + 5f, angle + .023f),
            Radial(center, inner - 3f, angle + .025f), Radial(center, inner - 3f, angle - .025f)
        ], BronzeShade);
        DrawLine(Radial(center, outer + 1f, angle - .012f),
            Radial(center, inner + 1f, angle - .014f), Bronze, 3f, true);
        DrawCircle(Radial(center, outer - 6f, angle), 2.5f, Patina);
        DrawCircle(Radial(center, inner + 9f, angle), 2.5f, Bronze);
    }

    private void DrawHangingSupports()
    {
        // Attach to the actual EntryCatch span (930..1270, top=164), not a drawn false shelf.
        DrawChain(970f, 14f, 162f);
        DrawChain(1230f, 15f, 162f);
        foreach (float x in new[] { 970f, 1230f })
        {
            DrawColoredPolygon([
                new(x - 19f, 0f), new(x + 18f, 0f), new(x + 12f, 21f),
                new(x + 5f, 29f), new(x - 8f, 24f), new(x - 16f, 13f)
            ], StoneShade);
            DrawRect(new Rect2(x - 5f, 8f, 10f, 10f), BronzeShade);
            DrawCircle(new Vector2(x, 13f), 2.5f, Bronze);
            DrawArc(new Vector2(x, 161f), 7f, Mathf.Pi, Mathf.Tau, 12, Bronze, 3f, true);
        }
    }

    private void DrawChain(float x, float top, float bottom)
    {
        for (float y = top + 9f; y < bottom - 8f; y += 19f)
        {
            DrawPolyline([
                new(x - 3f, y - 7f), new(x + 3f, y - 7f), new(x + 5f, y - 3f),
                new(x + 5f, y + 5f), new(x + 2f, y + 8f), new(x - 3f, y + 8f),
                new(x - 5f, y + 4f), new(x - 5f, y - 3f), new(x - 3f, y - 7f)
            ], BronzeShade, 3f, true);
            DrawLine(new Vector2(x - 3f, y - 4f), new Vector2(x - 3f, y + 4f),
                Bronze, 1f, true);
            DrawLine(new Vector2(x, y + 5f), new Vector2(x, y + 13f), Bronze, 2f, true);
        }
    }

    private void DrawBuriedFragments()
    {
        // Low broken foundations stay below the walkable floor and out of the combat silhouette.
        DrawColoredPolygon([
            new(765f, 720f), new(789f, 670f), new(820f, 662f), new(845f, 641f),
            new(893f, 651f), new(926f, 691f), new(978f, 705f), new(997f, 720f)
        ], StoneShade);
        DrawColoredPolygon([
            new(800f, 676f), new(819f, 663f), new(844f, 643f), new(874f, 651f),
            new(849f, 680f), new(825f, 689f)
        ], Stone);
        DrawLine(new Vector2(849f, 680f), new Vector2(866f, 710f), Mortar, 4f, true);
        DrawColoredPolygon([
            new(1400f, 720f), new(1429f, 690f), new(1471f, 680f), new(1500f, 647f),
            new(1547f, 663f), new(1572f, 697f), new(1600f, 706f), new(1600f, 720f)
        ], StoneShade);
        DrawColoredPolygon([
            new(1473f, 680f), new(1501f, 650f), new(1538f, 663f), new(1523f, 690f)
        ], Stone);
    }

    private static Vector2 Radial(Vector2 center, float radius, float angle) =>
        center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
}
