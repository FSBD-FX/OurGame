using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Presentation;

/// <summary>
/// Static, replaceable scenery only. These silhouettes never describe walkable or grappleable
/// geometry. CanvasItem retains the draw commands; no process loop, particles, or runtime redraws.
/// </summary>
public partial class CanyonBackdropVisual : Node2D
{
    [Export] public PrologueSectorId SectorId { get; set; }
    [Export] public Rect2 SectorBounds { get; set; } = new(0f, 0f, 1280f, 720f);
    [Export] public bool DistantLayer { get; set; }
    [Export] public bool CoverageLayer { get; set; }
    [Export] public bool UseCanyonDepthStaging { get; set; } = false;
    [Export] public string VisualRole { get; set; } = "sector";
    [Export] public string CompositionId { get; set; } = "";
    [Export] public Rect2 CompositionBounds { get; set; }
    [Export] public Color AccentColor { get; set; } = new("846a42");

    private static readonly Color FarRock = new("17252b");
    private static readonly Color MiddleRock = new("1b2c30");
    private static readonly Color NearRock = new("213235");
    private static readonly Color Stone = new("293c3d");
    private static readonly Color StoneShadow = new("203033");
    private static readonly Color StoneSeam = new("344646");
    private static readonly Color Copper = new("45443a");
    private static readonly Color CopperShadow = new("303931");
    private static readonly Color Recess = new("152328");

    public override void _Draw()
    {
        if (CoverageLayer)
        {
            DrawRect(SectorBounds, RoomVisualCatalog.CanyonSkirtBase);
            return;
        }
        if (VisualRole == "challenge")
        {
            DrawSetTransform(CompositionBounds.Position, 0f, Vector2.One);
            if (CompositionBounds.Size.X > 0f && CompositionBounds.Size.Y > 0f)
                DrawChallengeComposition(CompositionBounds.Size.X, CompositionBounds.Size.Y);
            return;
        }
        DrawSetTransform(SectorBounds.Position, 0f, Vector2.One);
        float width = SectorBounds.Size.X;
        float height = SectorBounds.Size.Y;
        if (width <= 0f || height <= 0f) return;

        if (DistantLayer)
        {
            DrawContinuousDistantField();
            return;
        }

        switch (SectorId)
        {
            case PrologueSectorId.CanyonEntrance:
                DrawEntrance(width, height);
                break;
            case PrologueSectorId.CanyonDescent:
                DrawDescent(width, height);
                break;
            case PrologueSectorId.CanyonBasin:
                DrawBasin(width, height);
                break;
            case PrologueSectorId.CanyonAscent:
                DrawAscent(width, height);
                break;
            case PrologueSectorId.LostTemple:
                DrawTemple(width, height);
                break;
        }
    }

    private void DrawChallengeComposition(float width, float height)
    {
        // Large, low-frequency silhouettes establish location and depth. They stay deliberately
        // below gameplay contrast and never advertise collision or a grapple response.
        Color body = new Color("26363a", .76f);
        Color shadow = new Color("142329", .86f);
        Color metal = new Color(AccentColor, .58f);
        switch (CompositionId)
        {
            case "broken-entrance-bell":
                DrawEntranceBelfry(width, height);
                break;
            case "staggered-belfry-ribs":
                // Entrance owns the cross-seam shoulder to our left. These disjoint,
                // canted remnants leave the descending route open, without fake beam tops.
                PaintRiftPolygon([new(width * .28f, 0f),
                    new(width * .39f, 0f), new(width * .43f, height * .18f),
                    new(width * .38f, height * .31f), new(width * .40f, height * .45f),
                    new(width * .32f, height * .63f), new(width * .29f, height * .90f),
                    new(width * .24f, height), new(width * .22f, height * .83f),
                    new(width * .29f, height * .54f), new(width * .31f, height * .26f)],
                    new Color("243538", .73f),
                        [new("48544d", .83f), new("616554", .87f), new("4f5c54", .81f),
                            new("34474a", .76f), new("33454a", .71f), new("24383f", .65f),
                            new("1d3038", .48f), new("1c2d34", .34f), new("1c2d34", .40f),
                            new("25393f", .67f), new("3a4d4e", .80f)]);
                PaintRiftPolygon([new(width * .82f, 0f), new(width, 0f),
                    new(width, height * .62f), new(width * .93f, height * .73f),
                    new(width * .91f, height * .93f), new(width * .83f, height),
                    new(width * .86f, height * .68f), new(width * .79f, height * .47f),
                    new(width * .84f, height * .28f), new(width * .78f, height * .13f)],
                    new Color("1c2b31", .77f),
                        [new("34464a", .79f), new("52605a", .81f), new("34484c", .72f),
                            new("263b43", .62f), new("1b2f39", .45f), new("192d36", .32f),
                            new("203740", .52f), new("293f46", .65f), new("30474b", .74f),
                            new("2c4045", .76f)]);
                DrawChain(width * .65f, height * .07f, height * .55f, .65f);
                break;
            case "abyss-salvage-crane":
                DrawRockCut(new Rect2(width * .01f, height * .09f,
                    width * .28f, height * .82f), body, shadow);
                DrawRect(new Rect2(width * .18f, height * .15f,
                    width * .06f, height * .62f), shadow);
                DrawLine(new Vector2(width * .20f, height * .23f),
                    new Vector2(width * .75f, height * .30f), body, 24f, true);
                DrawLine(new Vector2(width * .73f, height * .30f),
                    new Vector2(width * .59f, height * .43f), body, 8f, true);
                DrawLine(new Vector2(width * .70f, height * .29f),
                    new Vector2(width * .70f, height * .61f), metal, 5f, true);
                DrawHangingBasket(new Vector2(width * .70f, height * .68f), 64f, metal);
                break;
            case "sealed-archive-vault":
                DrawServiceAlcove(new Rect2(width * .04f, height * .37f,
                    width * .91f, height * .53f), body, shadow, metal);
                DrawArchiveShelf(new Vector2(width * .15f, height * .49f),
                    new Vector2(width * .25f, height * .35f), body, metal, lean: -.09f);
                DrawArchiveShelf(new Vector2(width * .48f, height * .43f),
                    new Vector2(width * .31f, height * .40f), body, metal, lean: .07f);
                DrawRect(new Rect2(width * .35f, height * .85f, width * .10f,
                    height * .045f), new Color(metal, .67f));
                DrawRect(new Rect2(width * .80f, height * .82f, width * .09f,
                    height * .065f), new Color(body, .83f));
                DrawRect(new Rect2(width * .11f, height * .86f, width * .11f,
                    height * .07f), new Color("46504b", .75f));
                DrawLine(new Vector2(width * .11f, height * .86f),
                    new Vector2(width * .22f, height * .86f),
                    new Color(metal, .40f), 4f, true);
                DrawColoredPolygon([new(width * .79f, height * .88f),
                    new(width * .91f, height * .87f), new(width * .95f, height * .94f),
                    new(width * .77f, height * .95f)], new Color(body, .64f));
                DrawFallenArchiveCanister(new Vector2(width * .83f, height * .90f),
                    width * .10f, metal);
                break;
            case "frozen-bell-choir":
                DrawFrozenFracture(new Rect2(width * .02f, height * .12f,
                    width * .34f, height * .73f), leftWall: true);
                DrawFrozenFracture(new Rect2(width * .71f, height * .19f,
                    width * .27f, height * .68f), leftWall: false);
                DrawPolyline([new(width * .02f, height * .19f), new(width * .22f, height * .19f),
                    new(width * .25f, height * .27f), new(width * .25f, height * .45f)],
                    new Color(.40f, .57f, .63f, .53f), 14f, true);
                DrawPolyline([new(width * .74f, height * .17f),
                    new(width * .74f, height * .29f), new(width * .92f, height * .29f)],
                    new Color(.40f, .57f, .63f, .48f), 13f, true);
                DrawLine(new Vector2(width * .14f, height * .31f),
                    new Vector2(width * .31f, height * .38f),
                    new Color(.57f, .71f, .77f, .42f), 5f, true);
                break;
            case "buried-gear-crown":
                DrawWorkshopBay(new Rect2(width * .04f, height * .19f,
                    width * .31f, height * .45f), body, shadow, metal, broken: false);
                DrawWorkshopBay(new Rect2(width * .70f, height * .16f,
                    width * .26f, height * .42f), body, shadow, metal, broken: true);
                DrawBearingHousing(new Vector2(width * .30f, height * .69f), width * .14f,
                    body, metal);
                DrawBearingHousing(new Vector2(width * .62f, height * .53f), width * .19f,
                    shadow, body);
                DrawLine(new Vector2(width * .30f, height * .69f),
                    new Vector2(width * .62f, height * .53f), new Color(metal, .38f), 13f, true);
                DrawPolyline([new(width * .06f, height * .74f), new(width * .14f, height * .74f),
                    new(width * .19f, height * .60f)], new Color(body, .65f), 12f, true);
                DrawSupportBracket(new Vector2(width * .49f, height * .47f),
                    new Vector2(width * .29f, height * .83f), body);
                DrawSupportBracket(new Vector2(width * .55f, height * .47f),
                    new Vector2(width * .72f, height * .82f), shadow);
                break;
            case "steam-pump-trench":
                DrawServiceAlcove(new Rect2(width * .33f, height * .18f,
                    width * .30f, height * .59f), body, shadow, metal);
                DrawArc(new Vector2(width * .48f, height * .25f), width * .14f,
                    Mathf.Pi, Mathf.Tau, 24, body, 18f, true);
                DrawPolyline([new(width * .08f, height * .68f), new(width * .34f, height * .68f),
                    new(width * .34f, height * .40f)], metal, 15f, true);
                DrawPolyline([new(width * .62f, height * .42f), new(width * .82f, height * .42f),
                    new(width * .82f, height * .78f), new(width * .94f, height * .78f)], body, 17f, true);
                DrawLine(new Vector2(width * .07f, height * .78f),
                    new Vector2(width * .27f, height * .78f), shadow, 11f, true);
                DrawLine(new Vector2(width * .71f, height * .72f),
                    new Vector2(width * .88f, height * .72f), shadow, 11f, true);
                break;
            case "moving-counterweight-lift":
                DrawGuideRail(width * .18f, height * .06f, height * .91f, body, metal);
                DrawGuideRail(width * .77f, height * .06f, height * .91f, body, metal);
                DrawServiceAlcove(new Rect2(width * .61f, height * .22f,
                    width * .27f, height * .34f), body, shadow, metal);
                DrawRect(new Rect2(width * .64f, height * .32f,
                    width * .12f, height * .18f), new Color(metal, .52f));
                DrawLine(new Vector2(width * .70f, height * .06f),
                    new Vector2(width * .70f, height * .32f), body, 6f, true);
                DrawLine(new Vector2(width * .18f, height * .40f),
                    new Vector2(width * .61f, height * .59f), new Color(body, .64f), 9f, true);
                DrawSupportBracket(new Vector2(width * .67f, height * .86f),
                    new Vector2(width * .78f, height * .98f), shadow);
                break;
            case "pendulum-procession":
                DrawGallery(new Rect2(width * .08f, height * .13f,
                    width * .84f, height * .24f), body, shadow);
                DrawBrokenBell(new Vector2(width * .23f, height * .62f), width * .067f,
                    new Color("625744", .66f), broken: false);
                DrawNarrowBell(new Vector2(width * .68f, height * .57f), width * .078f,
                    new Color("545247", .68f));
                DrawLine(new Vector2(width * .23f, height * .22f),
                    new Vector2(width * .23f, height * .55f), body, 4f, true);
                DrawLine(new Vector2(width * .68f, height * .22f),
                    new Vector2(width * .68f, height * .48f), body, 5f, true);
                break;
            case "upper-gallery-bell-frame":
                DrawGallery(new Rect2(width * .10f, height * .12f,
                    width * .83f, height * .31f), body, shadow);
                DrawRect(new Rect2(width * .16f, height * .24f,
                    width * .08f, height * .54f), shadow);
                DrawRect(new Rect2(width * .76f, height * .24f,
                    width * .08f, height * .54f), shadow);
                DrawBrokenBell(new Vector2(width * .45f, height * .58f), width * .13f,
                    metal, broken: true);
                DrawBrokenBell(new Vector2(width * .75f, height * .65f), width * .06f,
                    body, broken: false);
                break;
            case "sealed-temple-forecourt":
                DrawTempleFacade(width, height, body, shadow, metal, isSealed: true);
                DrawTempleNiche(new Rect2(width * .05f, height * .31f,
                    width * .22f, height * .47f), body, metal);
                DrawTempleNiche(new Rect2(width * .72f, height * .28f,
                    width * .20f, height * .49f), body, metal);
                DrawTempleLintels(new Vector2(width * .51f, height * .23f), width * .82f);
                break;
            case "fallen-bell-temple-facade":
                DrawTempleFacade(width, height, body, shadow, metal, isSealed: false);
                DrawTempleLintels(new Vector2(width * .52f, height * .20f), width * .76f);
                DrawBrokenBell(new Vector2(width * .61f, height * .57f), width * .12f,
                    metal, broken: true);
                break;
        }
    }

    private void DrawTempleFacade(float width, float height, Color body, Color shadow,
        Color metal, bool isSealed)
    {
        DrawRect(new Rect2(width * .12f, height * .28f, width * .12f, height * .61f), shadow);
        DrawRect(new Rect2(width * .76f, height * .28f, width * .12f, height * .61f), shadow);
        DrawBrokenArch(new Vector2(width * .50f, height * .48f), width * .30f,
            height * .40f, body, 29f);
        if (isSealed)
            for (int bar = 0; bar < 4; bar++)
            {
                float x = width * (.38f + bar * .08f);
                DrawLine(new Vector2(x, height * .42f), new Vector2(x, height * .90f),
                    metal, 12f, true);
            }
    }

    private void DrawBrokenBell(Vector2 center, float radius, Color color, bool broken)
    {
        Vector2[] profile = [center + new Vector2(-radius * .52f, -radius),
            center + new Vector2(radius * .48f, -radius),
            center + new Vector2(radius * .78f, radius * .44f),
            center + new Vector2(radius, radius * .72f),
            center + new Vector2(broken ? radius * .12f : -radius, radius * .72f),
            center + new Vector2(-radius * .78f, radius * .38f)];
        Vector2[] castShadow = new Vector2[profile.Length];
        for (int index = 0; index < profile.Length; index++)
            castShadow[index] = profile[index] + new Vector2(radius * .10f, radius * .12f);
        DrawColoredPolygon(castShadow, new Color(Recess, .82f));
        DrawColoredPolygon(profile, color);
        DrawColoredPolygon([center + new Vector2(radius * .23f, -radius * .91f),
            center + new Vector2(radius * .48f, -radius),
            center + new Vector2(radius * .78f, radius * .44f),
            center + new Vector2(radius, radius * .72f),
            center + new Vector2(radius * .55f, radius * .65f)],
            new Color(color.Darkened(.24f), color.A));
        DrawLine(center + new Vector2(-radius * .44f, -radius * .87f),
            center + new Vector2(radius * .35f, -radius * .87f),
            new Color(color.Lightened(.20f), color.A * .72f), Mathf.Max(3f, radius * .045f), true);
        DrawLine(center + new Vector2(-radius * .72f, radius * .62f),
            center + new Vector2(broken ? radius * .10f : radius * .76f, radius * .62f),
            new Color(color.Lightened(.13f), color.A), Mathf.Max(4f, radius * .06f), true);
        DrawLine(center + new Vector2(0f, radius * .52f),
            center + new Vector2(0f, radius * .98f), Recess, 7f, true);
        DrawArc(center + new Vector2(0f, radius * .79f), radius * .18f,
            0f, Mathf.Pi, 14, new Color(Recess, .72f), Mathf.Max(3f, radius * .04f), true);
    }

    private void DrawNarrowBell(Vector2 center, float radius, Color color)
    {
        DrawRect(new Rect2(center.X - radius * .23f, center.Y - radius * 1.16f,
            radius * .46f, radius * .24f), new Color(color.Lightened(.12f), color.A));
        DrawColoredPolygon([center + new Vector2(-radius * .24f, -radius),
            center + new Vector2(radius * .24f, -radius),
            center + new Vector2(radius * .42f, radius * .53f),
            center + new Vector2(radius * .66f, radius * .76f),
            center + new Vector2(-radius * .61f, radius * .76f),
            center + new Vector2(-radius * .42f, radius * .53f)], color);
        DrawLine(center + new Vector2(-radius * .52f, radius * .69f),
            center + new Vector2(radius * .54f, radius * .69f),
            new Color(color.Lightened(.20f), color.A), 5f, true);
        DrawLine(center + new Vector2(0f, radius * .69f),
            center + new Vector2(0f, radius * 1.06f), Recess, 5f, true);
    }

    private void DrawContinuousDistantField()
    {
        // Every streamed sector draws the same world-aligned field. Overlapping resident sectors
        // therefore composite identical opaque pixels instead of exposing rectangular scene edges.
        Rect2 world = RoomVisualCatalog.ContinuousCanyonCoverageBounds();
        Vector2 sectorOffset = CanyonWorldLayout.SectorBounds[SectorId].Position;
        DrawSetTransform(world.Position - sectorOffset, 0f, Vector2.One);
        DrawRect(new Rect2(Vector2.Zero, world.Size), FarRock);
        int columns = Mathf.CeilToInt(world.Size.X / 760f) + 1;
        int rows = Mathf.CeilToInt(world.Size.Y / 690f) + 1;
        for (int row = 0; row < rows; row++)
        {
            float top = row * 690f - 210f;
            Color band = (row % 3) switch
            {
                0 => MiddleRock,
                1 => new Color("192a2e"),
                _ => NearRock
            };
            var ridge = new Vector2[columns + 3];
            for (int column = 0; column <= columns; column++)
            {
                float phase = ((column * 37 + row * 19) % 7) / 6f;
                ridge[column] = new Vector2(column * 760f,
                    top + 55f + phase * 170f);
            }
            ridge[columns + 1] = new Vector2(world.Size.X, top + 610f);
            ridge[columns + 2] = new Vector2(0f, top + 610f);
            DrawColoredPolygon(ridge, band);
            for (int column = 0; column < columns; column += 2)
            {
                float x = column * 760f + 120f + (row % 2) * 180f;
                float y = top + 310f + ((column + row) % 3) * 38f;
                DrawLine(new Vector2(x, y), new Vector2(x + 430f, y - 42f),
                    new Color("344447", .24f), 3f, true);
            }
        }
    }

    private void DrawEntrance(float width, float height)
    {
        DrawEntranceNaturalApproach(width, height);
    }

    private void DrawDescent(float width, float height)
    {
        DrawColoredPolygon([new(width * .94f, height * .18f),
            new(width * 1.30f, height * .21f), new(width * 1.53f, height * .36f),
            new(width * 1.48f, height * .88f), new(width * 1.08f, height * .94f)],
            new Color("26383b", .29f));
        // The shaft architecture starts below the open upper rift, with broken shoulders.
        // Preserve the lower pier positions and joint rhythm rather than shifting the shaft.
        PaintRiftPolygon([new(width * .16f - 26f, 790f),
            new(width * .16f + 14f, 800f), new(width * .16f - 6f, 809f),
            new(width * .16f + 28f, 820f), new(width * .16f - 28f, 820f),
            new(width * .16f - 46f, 804f)], StoneShadow,
                [new("34494b"), new("43544f"), new("2c4144"),
                    StoneShadow, StoneShadow, new("24393f")]);
        PaintRiftPolygon([new(width * .81f + 23f, 552f),
            new(width * .81f + 49f, 605f), new(width * .81f + 16f, 667f),
            new(width * .81f + 36f, 720f), new(width * .81f - 36f, 720f),
            new(width * .81f - 20f, 636f)], StoneShadow,
                [new("45554f"), new("536056"), new("32484a"),
                    StoneShadow, StoneShadow, new("273c42")]);
        DrawShaftPier(width * .16f, height, 56f, top: 820f);
        DrawShaftPier(width * .81f, height, 72f, top: 720f);
        foreach (float y in new[] { 820f, 1490f, 2150f, 2770f, 3260f })
        {
            DrawRockCut(new Rect2(32f, y, 225f, 250f), Stone, Recess);
            DrawRockCut(new Rect2(width - 190f, y + 95f, 325f, 270f),
                StoneShadow, Recess);
            DrawLine(new Vector2(width - 140f, y + 130f),
                new Vector2(width + 128f, y + 182f),
                new Color(StoneSeam, .42f), 9f, true);
        }
        DrawChain(width * .35f, height * .09f, height * .41f, .75f);
        DrawChain(width * .68f, height * .40f, height * .73f, .55f);
        DrawLine(new Vector2(width * 1.24f, height * .19f),
            new Vector2(width * 1.24f, height * .54f),
            new Color(CopperShadow, .58f), 4f, true);
        DrawLine(new Vector2(width * 1.42f, height * .50f),
            new Vector2(width * 1.42f, height * .82f),
            new Color(CopperShadow, .43f), 3f, true);
        DrawCircle(new Vector2(width * 1.24f, height * .54f), 11f,
            new Color(CopperShadow, .64f));
        DrawLine(new Vector2(width * .81f, height * .46f),
            new Vector2(width * 1.10f, height * .39f), StoneShadow, 16f, true);
        DrawLine(new Vector2(width * .81f, height * .68f),
            new Vector2(width * 1.17f, height * .62f), StoneShadow, 14f, true);
        DrawBearingHousing(new Vector2(width * .82f, height * .77f), 86f,
            StoneShadow, CopperShadow);
        DrawSupportBracket(new Vector2(240f, 1662f), new Vector2(126f, 1850f),
            StoneShadow);
        DrawSupportBracket(new Vector2(1110f, 3100f), new Vector2(1037f, 3270f),
            StoneShadow);
        DrawRubble(new Vector2(width * 1.13f, height * .90f), 1.5f);
    }

    private void PaintRiftPolygon(Vector2[] profile, Color originalColor, Color[] stagedColors)
    {
        if (UseCanyonDepthStaging && SectorId == PrologueSectorId.CanyonDescent)
            DrawPolygon(profile, stagedColors);
        else
            DrawColoredPolygon(profile, originalColor);
    }

    private void DrawBasin(float width, float height)
    {
        // The camp remains quiet; the workshop rises from the wall behind the gear lane.
        DrawWorkshopBay(new Rect2(142f, 174f, 316f, 188f),
            StoneShadow, Recess, CopperShadow, broken: false);
        DrawWorkshopBay(new Rect2(width * .16f, height * .26f, 286f, 216f),
            Stone, Recess, CopperShadow, broken: true);
        DrawLine(new Vector2(width * .18f, height * .31f),
            new Vector2(width * .22f, height * .10f), StoneShadow, 14f, true);
        DrawLine(new Vector2(width * .22f, height * .10f),
            new Vector2(width * .36f, height * .10f), StoneShadow, 14f, true);
        DrawServiceAlcove(new Rect2(width * .60f, height * .24f, 360f, 242f),
            Stone, Recess, CopperShadow);
        DrawServiceAlcove(new Rect2(width * .84f, height * .38f, 274f, 180f),
            StoneShadow, Recess, StoneSeam);
        Vector2[] pipe = [new(width * .05f, height * .45f),
            new(width * .39f, height * .45f), new(width * .45f, height * .58f),
            new(width * .67f, height * .58f), new(width * .72f, height * .49f),
            new(width * .97f, height * .49f)];
        DrawPolyline(pipe, StoneShadow, 26f, true);
        DrawPolyline(pipe, CopperShadow, 13f, true);
        for (int index = 0; index < 7; index++)
        {
            float x = width * (.07f + index * .045f);
            DrawLine(new Vector2(x, height * .45f - 11f),
                new Vector2(x, height * .45f + 11f), Copper, 4f, true);
        }
        DrawBearingHousing(new Vector2(width * .53f, height * .81f), 103f,
            StoneShadow, CopperShadow);
        DrawLine(new Vector2(width * .53f, height * .81f),
            new Vector2(width * .60f, height * .68f), StoneShadow, 15f, true);
        DrawLine(new Vector2(width * .60f, height * .68f),
            new Vector2(width * .69f, height * .68f), StoneShadow, 15f, true);
        DrawSupportBracket(new Vector2(210f, 414f), new Vector2(112f, 664f),
            StoneShadow);
        DrawSupportBracket(new Vector2(652f, 414f), new Vector2(750f, 666f),
            StoneShadow);
        DrawRubble(new Vector2(width * .31f, height * .94f), 1.35f);
        DrawRubble(new Vector2(width * .76f, height * .94f), 1.1f);
    }

    private void DrawAscent(float width, float height)
    {
        DrawGuideRail(width * .16f, 80f, height - 90f, StoneShadow, StoneSeam);
        DrawGuideRail(width * .82f, 70f, height - 90f, StoneShadow, StoneSeam);
        DrawGallery(new Rect2(width * .12f, height * .11f,
            width * .71f, 250f), Stone, Recess);
        DrawGallery(new Rect2(width * .24f, height * .38f,
            width * .68f, 202f), StoneShadow, Recess);
        DrawGallery(new Rect2(width * .13f, height * .68f,
            width * .63f, 198f), Stone, Recess);
        DrawServiceAlcove(new Rect2(width * .55f, height * .77f, 220f, 330f),
            StoneShadow, Recess, CopperShadow);
        DrawChain(width * .87f, height * .03f, height * .30f, .55f);
        DrawBrokenBell(new Vector2(width * .52f, height * .18f), 84f, CopperShadow,
            broken: true);
    }

    private void DrawServiceAlcove(Rect2 rect, Color frame, Color dark, Color accent)
    {
        DrawRect(rect, frame);
        DrawRect(rect.Grow(-12f), dark);
        DrawLine(rect.Position + new Vector2(16f, 14f),
            rect.Position + new Vector2(rect.Size.X - 16f, 14f),
            new Color(accent, .55f), 5f, true);
        DrawLine(rect.Position + new Vector2(23f, rect.Size.Y - 28f),
            rect.Position + new Vector2(rect.Size.X - 23f, rect.Size.Y - 28f),
            new Color(frame, .72f), 9f, true);
        foreach (float fraction in new[] { .25f, .72f })
            DrawRect(new Rect2(rect.Position.X + rect.Size.X * fraction,
                rect.Position.Y + rect.Size.Y * .49f, 12f, 30f), new Color(accent, .38f));
    }

    private void DrawWorkshopBay(Rect2 rect, Color frame, Color dark, Color accent, bool broken)
    {
        DrawColoredPolygon([rect.Position + new Vector2(0f, rect.Size.Y),
            rect.Position + new Vector2(0f, 22f),
            rect.Position + new Vector2(rect.Size.X * .34f, broken ? 32f : 0f),
            rect.Position + new Vector2(rect.Size.X * .72f, 0f),
            rect.Position + new Vector2(rect.Size.X, broken ? 38f : 14f), rect.End],
            new Color(frame, .74f));
        DrawRect(rect.Grow(-15f), dark);
        if (broken)
        {
            DrawPolyline([rect.Position + new Vector2(25f, rect.Size.Y * .29f),
                rect.Position + new Vector2(rect.Size.X * .54f, rect.Size.Y * .29f),
                rect.Position + new Vector2(rect.Size.X * .54f, rect.Size.Y * .56f)],
                new Color(frame.Lightened(.10f), .54f), 11f, true);
            DrawLine(rect.Position + new Vector2(rect.Size.X * .58f, rect.Size.Y * .57f),
                rect.Position + new Vector2(rect.Size.X * .80f, rect.Size.Y * .70f),
                new Color(accent, .48f), 7f, true);
            DrawRect(new Rect2(rect.Position.X + rect.Size.X * .15f,
                rect.Position.Y + rect.Size.Y * .64f, rect.Size.X * .19f, 20f),
                new Color("786547", .39f));
        }
        else
        {
            Vector2 hub = rect.Position + rect.Size * new Vector2(.40f, .52f);
            DrawCircle(hub, rect.Size.X * .14f, new Color(frame.Lightened(.10f), .67f));
            DrawCircle(hub, rect.Size.X * .085f, dark);
            DrawCircle(hub, rect.Size.X * .032f, new Color(accent, .66f));
            DrawPolyline([hub + new Vector2(rect.Size.X * .13f, 0f),
                rect.Position + new Vector2(rect.Size.X * .78f, rect.Size.Y * .52f),
                rect.Position + new Vector2(rect.Size.X * .78f, rect.Size.Y * .78f)],
                new Color(frame, .60f), 10f, true);
            DrawRect(new Rect2(rect.Position.X + rect.Size.X * .61f,
                rect.Position.Y + rect.Size.Y * .22f, rect.Size.X * .13f, 17f),
                new Color("796649", .44f));
        }
        DrawLine(rect.Position + new Vector2(20f, rect.Size.Y - 22f),
            rect.End - new Vector2(20f, 22f), new Color(frame, .84f), 11f, true);
    }

    private void DrawSupportBracket(Vector2 upper, Vector2 wall, Color color)
    {
        DrawColoredPolygon([upper + new Vector2(-14f, 13f),
            upper + new Vector2(16f, 13f),
            wall + new Vector2(20f, -8f),
            wall + new Vector2(-20f, 8f)], new Color(color, .51f));
        DrawLine(upper + new Vector2(0f, 18f), wall,
            new Color(color.Lightened(.12f), .45f), 8f, true);
        DrawCircle(wall, 10f, new Color(color, .70f));
    }

    private void DrawBearingHousing(Vector2 center, float radius, Color housing, Color rim)
    {
        DrawRect(new Rect2(center.X - radius * 1.28f, center.Y - radius * .68f,
            radius * 2.56f, radius * 1.44f), new Color(housing, .72f));
        DrawRect(new Rect2(center.X - radius * 1.03f, center.Y - radius * .57f,
            radius * 2.06f, radius * 1.13f), Recess);
        DrawGear(center, radius * .76f, new Color(rim, .58f));
        DrawCircle(center, radius * .29f, housing);
        DrawCircle(center, radius * .12f, new Color(rim, .78f));
        DrawLine(center + new Vector2(-radius * 1.12f, radius * .77f),
            center + new Vector2(radius * 1.12f, radius * .77f), housing, 9f, true);
    }

    private void DrawGuideRail(float x, float top, float bottom, Color frame, Color glint)
    {
        DrawRect(new Rect2(x - 22f, top, 44f, bottom - top), frame);
        DrawLine(new Vector2(x - 9f, top), new Vector2(x - 9f, bottom),
            new Color(glint, .46f), 4f, true);
        DrawLine(new Vector2(x + 11f, top), new Vector2(x + 11f, bottom),
            Recess, 5f, true);
        for (float y = top + 58f; y < bottom - 30f; y += 180f)
            DrawRect(new Rect2(x - 30f, y, 60f, 10f), new Color(frame.Lightened(.13f), .70f));
    }

    private void DrawGallery(Rect2 rect, Color frame, Color recess)
    {
        DrawRect(rect, new Color(frame, .65f));
        DrawRect(rect.Grow(-18f), recess);
        for (int bay = 0; bay < 4; bay++)
        {
            float x = rect.Position.X + rect.Size.X * (bay + .16f) / 4f;
            DrawRect(new Rect2(x, rect.Position.Y + 19f, 13f, rect.Size.Y - 30f),
                new Color(frame, .84f));
        }
        DrawLine(rect.Position + new Vector2(18f, rect.Size.Y - 21f),
            rect.End - new Vector2(18f, 21f), new Color(frame.Lightened(.12f), .43f),
            7f, true);
    }

    private void DrawTemple(float width, float height)
    {
        DrawTempleNiche(new Rect2(width * .15f, height * .13f, 252f, 360f),
            Stone, CopperShadow);
        DrawTempleNiche(new Rect2(width * .33f, height * .10f, 340f, 428f),
            StoneShadow, CopperShadow);
        DrawTempleLintels(new Vector2(width * .28f, height * .25f), 510f);
        DrawTempleNiche(new Rect2(width * .55f, height * .14f, 400f, 380f),
            Stone, CopperShadow);
        DrawTempleLintels(new Vector2(width * .66f, height * .22f), 620f);
        DrawSupportBracket(new Vector2(740f, 430f), new Vector2(680f, 682f),
            StoneShadow);
        DrawSupportBracket(new Vector2(1020f, 430f), new Vector2(1100f, 680f),
            StoneShadow);
        Vector2 center = new(width * .77f, height * .52f);
        DrawBrokenArch(center + new Vector2(0f, 23f), 202f, height * .36f, Stone, 35f);
        DrawBrokenArch(new Vector2(width * .28f, height * .58f), 128f,
            height * .37f, StoneShadow, 29f);
        DrawChain(center.X - 69f, 0f, center.Y - 105f, .9f);
        DrawChain(center.X + 62f, 0f, center.Y - 105f, .9f);

        // A large broken bell reads as a distant landmark, without an emissive core or target ring.
        DrawBrokenBell(center + new Vector2(0f, -35f), 118f, Copper, broken: true);
        DrawArc(center + new Vector2(0f, -105f), 44f, Mathf.Pi, Mathf.Tau, 24, Copper, 6f, true);
        DrawPolyline([center + new Vector2(-20f, -113f), center + new Vector2(-4f, -70f),
            center + new Vector2(-18f, -35f), center + new Vector2(12f, 23f)], Recess, 7f, true);
        DrawLine(center + new Vector2(-91f, 39f), center + new Vector2(-25f, 39f), Copper, 7f, true);
        DrawLine(center + new Vector2(29f, 39f), center + new Vector2(92f, 39f), Copper, 7f, true);
        DrawLine(center + new Vector2(-4f, 65f), center + new Vector2(-4f, 98f), StoneShadow, 10f, true);
        DrawRubble(new Vector2(width * .67f, height * .94f), 1.25f);
        DrawRubble(new Vector2(width * .88f, height * .94f), .95f);
    }

    private void DrawTransportPortal(Rect2 rect, Color frame, Color recess)
    {
        DrawColoredPolygon([rect.Position + new Vector2(0f, rect.Size.Y),
            rect.Position + new Vector2(0f, 42f),
            rect.Position + new Vector2(52f, 0f),
            rect.Position + new Vector2(rect.Size.X - 20f, 7f),
            rect.Position + new Vector2(rect.Size.X, 40f), rect.End], frame);
        DrawRect(rect.Grow(-22f), recess);
        DrawRect(new Rect2(rect.Position.X + 49f, rect.Position.Y + 61f,
            rect.Size.X - 94f, rect.Size.Y - 84f), new Color("1b2d31", .62f));
        DrawLine(rect.Position + new Vector2(-14f, 40f),
            rect.Position + new Vector2(rect.Size.X + 6f, 54f),
            new Color(frame.Lightened(.15f), .59f), 10f, true);
        DrawLine(rect.Position + new Vector2(rect.Size.X * .66f, 50f),
            rect.Position + new Vector2(rect.Size.X * .57f, 114f), Recess, 6f, true);
    }

    private void DrawHangingBasket(Vector2 center, float size, Color color)
    {
        DrawColoredPolygon([center + new Vector2(-size, -size * .25f),
            center + new Vector2(size * .72f, -size * .23f),
            center + new Vector2(size * .58f, size * .45f),
            center + new Vector2(-size * .73f, size * .58f)],
            new Color(color, .58f));
        DrawLine(center + new Vector2(-size * .65f, -size * .19f),
            center + new Vector2(-size * .34f, -size * .83f), color, 4f, true);
        DrawLine(center + new Vector2(size * .49f, -size * .19f),
            center + new Vector2(size * .27f, -size * .83f), color, 4f, true);
        DrawLine(center + new Vector2(-size * .70f, size * .50f),
            center + new Vector2(size * .52f, size * .39f), Recess, 5f, true);
    }

    private void DrawTornFlag(Vector2 top, float length, Color color)
    {
        DrawLine(top, top + new Vector2(0f, length), color, 3f, true);
        DrawColoredPolygon([top + new Vector2(0f, 5f), top + new Vector2(47f, 14f),
            top + new Vector2(30f, 30f), top + new Vector2(40f, 48f),
            top + new Vector2(8f, length)], new Color(color, .58f));
    }

    private void DrawRockCut(Rect2 rect, Color body, Color crevice)
    {
        DrawColoredPolygon([rect.Position + new Vector2(0f, rect.Size.Y * .15f),
            rect.Position + new Vector2(rect.Size.X * .40f, 0f),
            rect.Position + new Vector2(rect.Size.X, rect.Size.Y * .11f),
            rect.Position + new Vector2(rect.Size.X * .83f, rect.Size.Y * .63f),
            rect.Position + new Vector2(rect.Size.X * .61f, rect.Size.Y),
            rect.Position + new Vector2(0f, rect.Size.Y * .90f)],
            new Color(body, .60f));
        DrawPolyline([rect.Position + new Vector2(rect.Size.X * .42f, 0f),
            rect.Position + new Vector2(rect.Size.X * .36f, rect.Size.Y * .45f),
            rect.Position + new Vector2(rect.Size.X * .59f, rect.Size.Y * .62f)],
            new Color(crevice, .83f), 6f, true);
    }

    private void DrawArchiveShelf(Vector2 origin, Vector2 size, Color frame,
        Color plate, float lean)
    {
        DrawColoredPolygon([origin + new Vector2(0f, size.Y),
            origin + new Vector2(lean * size.Y, 0f),
            origin + new Vector2(size.X + lean * size.Y, 0f),
            origin + size], new Color(frame, .84f));
        DrawColoredPolygon([origin + new Vector2(17f, size.Y - 13f),
            origin + new Vector2(lean * size.Y + 16f, 15f),
            origin + new Vector2(size.X + lean * size.Y - 18f, 15f),
            origin + new Vector2(size.X - 17f, size.Y - 13f)], Recess);
        for (int row = 1; row <= 3; row++)
        {
            float y = origin.Y + size.Y * row / 4f;
            float offset = lean * (size.Y - size.Y * row / 4f);
            DrawLine(new Vector2(origin.X + 15f + offset, y),
                new Vector2(origin.X + size.X - 15f + offset, y + 3f),
                new Color(frame.Lightened(.12f), .77f), 6f, true);
            float start = origin.X + 25f + offset;
            DrawRect(new Rect2(start, y - 27f, 21f, 22f),
                new Color("5a594b", .75f));
            DrawRect(new Rect2(start + 25f, y - 35f, 28f, 30f),
                new Color("625f50", .64f));
            DrawRect(new Rect2(start + 58f, y - 21f, 18f, 16f),
                new Color("4b5b57", .76f));
            DrawLine(new Vector2(start + 4f, y - 19f),
                new Vector2(start + 17f, y - 19f), new Color(plate, .51f), 2f, true);
            DrawLine(new Vector2(start + 30f, y - 26f),
                new Vector2(start + 48f, y - 26f), new Color(plate, .43f), 2f, true);
        }
        DrawRect(new Rect2(origin.X + size.X * .33f, origin.Y + 10f,
            size.X * .40f, 16f), new Color(plate, .48f));
        DrawLine(new Vector2(origin.X + size.X * .39f, origin.Y + 18f),
            new Vector2(origin.X + size.X * .56f, origin.Y + 18f),
            Recess, 3f, true);
    }

    private void DrawFallenArchiveCanister(Vector2 center, float length, Color metal)
    {
        DrawColoredPolygon([center + new Vector2(-length * .55f, -10f),
            center + new Vector2(length * .43f, -25f),
            center + new Vector2(length * .60f, 6f),
            center + new Vector2(-length * .38f, 22f)],
            new Color("4b534b", .76f));
        DrawArc(center + new Vector2(-length * .45f, 6f), 16f,
            0f, Mathf.Tau, 18, new Color(metal, .72f), 4f, true);
        DrawLine(center + new Vector2(-length * .25f, -12f),
            center + new Vector2(length * .34f, -20f), new Color(metal, .50f), 3f, true);
    }

    private void DrawFrozenFracture(Rect2 rect, bool leftWall)
    {
        float side = leftWall ? 1f : -1f;
        float edge = leftWall ? rect.Position.X : rect.End.X;
        DrawColoredPolygon([new(edge, rect.Position.Y),
            new(edge + side * rect.Size.X * .77f, rect.Position.Y + rect.Size.Y * .19f),
            new(edge + side * rect.Size.X * .43f, rect.Position.Y + rect.Size.Y * .46f),
            new(edge + side * rect.Size.X * .91f, rect.Position.Y + rect.Size.Y * .74f),
            new(edge + side * rect.Size.X * .47f, rect.End.Y),
            new(edge, rect.End.Y)], new Color(.29f, .45f, .54f, .31f));
        DrawPolyline([new(edge + side * 10f, rect.Position.Y + rect.Size.Y * .17f),
            new(edge + side * rect.Size.X * .66f, rect.Position.Y + rect.Size.Y * .28f),
            new(edge + side * rect.Size.X * .31f, rect.Position.Y + rect.Size.Y * .48f),
            new(edge + side * rect.Size.X * .72f, rect.Position.Y + rect.Size.Y * .70f)],
            new Color(.52f, .70f, .76f, .38f), 5f, true);
    }

    private void DrawTempleNiche(Rect2 rect, Color frame, Color metal)
    {
        bool broken = rect.Size.X > 300f;
        Vector2 p = rect.Position;
        Vector2 s = rect.Size;
        DrawColoredPolygon([p + new Vector2(0f, s.Y), p + new Vector2(0f, 60f),
            p + new Vector2(s.X * .16f, 22f),
            p + new Vector2(s.X * .48f, broken ? 10f : 0f),
            p + new Vector2(s.X * .72f, broken ? 39f : 9f),
            p + new Vector2(s.X, 45f), p + s], new Color(frame, .78f));
        DrawColoredPolygon([p + new Vector2(32f, s.Y - 24f),
            p + new Vector2(32f, s.Y * .38f),
            p + new Vector2(s.X * .25f, s.Y * .22f),
            p + new Vector2(s.X * .53f, s.Y * .18f),
            p + new Vector2(s.X - 32f, s.Y * .40f),
            p + new Vector2(s.X - 32f, s.Y - 24f)],
            new Color(broken ? "29232e" : "241f2b", .84f));
        DrawArc(new Vector2(rect.GetCenter().X, p.Y + s.Y * .48f),
            s.X * .31f, Mathf.Pi, Mathf.Tau, 26,
            new Color(frame.Lightened(.10f), .56f), 17f, true);
        DrawRect(new Rect2(p.X + 18f, p.Y + s.Y * .48f, 24f, s.Y * .38f),
            new Color(frame.Lightened(.08f), .62f));
        DrawRect(new Rect2(rect.End.X - 42f, p.Y + s.Y * .48f,
            24f, s.Y * .38f), new Color(frame.Lightened(.08f), .62f));
        if (broken)
        {
            DrawRect(new Rect2(p.X + 18f, p.Y + s.Y * .64f, 24f, 25f), Recess);
            DrawPolyline([p + new Vector2(s.X * .65f, 36f),
                p + new Vector2(s.X * .60f, 78f),
                p + new Vector2(s.X * .71f, 116f)], Recess, 5f, true);
        }
        DrawLine(p + new Vector2(38f, s.Y - 28f),
            rect.End - new Vector2(38f, 28f), new Color(metal, .48f), 6f, true);
    }

    private void DrawTempleLintels(Vector2 center, float span)
    {
        DrawColoredPolygon([center + new Vector2(-span * .51f, 48f),
            center + new Vector2(-span * .47f, 9f),
            center + new Vector2(span * .09f, 5f),
            center + new Vector2(span * .12f, 26f),
            center + new Vector2(span * .46f, 16f),
            center + new Vector2(span * .49f, 54f)], StoneShadow);
        DrawColoredPolygon([center + new Vector2(-span * .44f, 9f),
            center + new Vector2(-span * .37f, -17f),
            center + new Vector2(span * .13f, -14f),
            center + new Vector2(span * .14f, 6f)],
            new Color("5a4840", .46f));
        DrawColoredPolygon([center + new Vector2(span * .20f, 4f),
            center + new Vector2(span * .26f, -13f),
            center + new Vector2(span * .43f, -7f),
            center + new Vector2(span * .45f, 14f)],
            new Color("5a4840", .46f));
        DrawLine(center + new Vector2(span * .14f, -4f),
            center + new Vector2(span * .23f, 31f), Recess, 5f, true);
    }

    private void DrawBrokenArch(Vector2 center, float radius, float legHeight, Color color, float thickness)
    {
        DrawArc(center, radius, Mathf.Pi, Mathf.Pi * 1.42f, 21, color, thickness, true);
        DrawArc(center, radius, Mathf.Pi * 1.54f, Mathf.Tau, 23, color, thickness, true);
        DrawLine(center + new Vector2(-radius, -3f), center + new Vector2(-radius, legHeight),
            color, thickness, true);
        DrawLine(center + new Vector2(radius, -3f), center + new Vector2(radius, legHeight),
            color, thickness, true);
        for (int index = 0; index < 4; index++)
        {
            float y = (index + .4f) * legHeight / 4f;
            DrawLine(center + new Vector2(-radius - thickness * .4f, y),
                center + new Vector2(-radius + thickness * .4f, y + 2f), Recess, 3f, true);
            DrawLine(center + new Vector2(radius - thickness * .4f, y + 13f),
                center + new Vector2(radius + thickness * .4f, y + 15f), Recess, 3f, true);
        }
    }

    private void DrawShaftPier(float x, float height, float width, float top = 0f)
    {
        DrawRect(new Rect2(x - width / 2f, top, width, height - top), StoneShadow);
        DrawLine(new Vector2(x - width * .28f, top),
            new Vector2(x - width * .28f, height), Stone, 5f, true);
        for (float y = 80f; y < height; y += 150f)
        {
            if (y >= top)
                DrawLine(new Vector2(x - width / 2f, y), new Vector2(x + width / 2f, y + 3f),
                    Recess, 6f, true);
        }
    }

    private void DrawChain(float x, float top, float bottom, float scale)
    {
        for (float y = top + 9f * scale; y < bottom - 9f * scale; y += 23f * scale)
        {
            DrawArc(new Vector2(x, y), 7f * scale, 0f, Mathf.Tau, 12,
                CopperShadow, 2f * scale, true);
            DrawLine(new Vector2(x, y + 5f * scale), new Vector2(x, y + 19f * scale),
                CopperShadow, 3f * scale, true);
        }
    }

    private void DrawGear(Vector2 center, float radius, Color color)
    {
        DrawArc(center, radius, .13f, Mathf.Tau - .24f, 56, color, 13f, true);
        DrawArc(center, radius * .78f, .13f, Mathf.Tau - .24f, 48, StoneShadow, 5f, true);
        DrawCircle(center, radius * .15f, color);
        for (int index = 0; index < 12; index++)
        {
            float angle = index * Mathf.Tau / 12f + .22f;
            Vector2 axis = new(Mathf.Cos(angle), Mathf.Sin(angle));
            DrawLine(center + axis * (radius - 2f), center + axis * (radius + 9f), color, 13f, true);
            if (index % 2 == 0)
                DrawLine(center + axis * radius * .19f, center + axis * radius * .89f,
                    color, 9f, true);
        }
    }

    private void DrawBridge(Vector2 start, Vector2 end, float depth)
    {
        DrawLine(start, end, Stone, 12f, true);
        DrawLine(start + new Vector2(0f, depth), end + new Vector2(0f, depth), StoneShadow, 8f, true);
        for (int index = 0; index < 7; index++)
        {
            Vector2 first = start.Lerp(end, index / 7f);
            Vector2 next = start.Lerp(end, (index + 1) / 7f);
            DrawLine(first, next + new Vector2(0f, depth), CopperShadow, 6f, true);
            DrawLine(first, first + new Vector2(0f, depth), StoneShadow, 6f, true);
        }
    }

    private void DrawRubble(Vector2 origin, float scale)
    {
        DrawPolygon([origin + new Vector2(-111f, 10f) * scale,
            origin + new Vector2(-73f, -34f) * scale, origin + new Vector2(-31f, -21f) * scale,
            origin + new Vector2(-15f, -50f) * scale, origin + new Vector2(22f, -45f) * scale,
            origin + new Vector2(52f, -13f) * scale, origin + new Vector2(96f, -23f) * scale,
            origin + new Vector2(123f, 10f) * scale], [Stone]);
        DrawLine(origin + new Vector2(-68f, -26f) * scale,
            origin + new Vector2(-27f, -14f) * scale, StoneSeam, 3f, true);
        DrawLine(origin + new Vector2(8f, -35f) * scale,
            origin + new Vector2(33f, -11f) * scale, StoneShadow, 5f, true);
    }
}
