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
        DrawSetTransform(SectorBounds.Position, 0f, Vector2.One);
        float width = SectorBounds.Size.X;
        float height = SectorBounds.Size.Y;
        if (width <= 0f || height <= 0f) return;

        if (DistantLayer)
        {
            DrawDistantRidges(width, height);
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

    private void DrawDistantRidges(float width, float height)
    {
        DrawPolygon([
            new(0f, height * .37f), new(width * .10f, height * .30f),
            new(width * .18f, height * .34f), new(width * .28f, height * .14f),
            new(width * .39f, height * .23f), new(width * .47f, height * .18f),
            new(width * .59f, height * .36f), new(width * .74f, height * .21f),
            new(width * .87f, height * .33f), new(width, height * .24f),
            new(width, height), new(0f, height)
        ], [FarRock]);
        DrawPolygon([
            new(0f, height * .48f), new(width * .16f, height * .55f),
            new(width * .24f, height * .40f), new(width * .32f, height * .49f),
            new(width * .43f, height * .35f), new(width * .51f, height * .56f),
            new(width * .68f, height * .45f), new(width * .83f, height * .52f),
            new(width, height * .38f), new(width, height), new(0f, height)
        ], [MiddleRock]);
        DrawPolygon([
            new(0f, height * .68f), new(width * .12f, height * .64f),
            new(width * .25f, height * .74f), new(width * .42f, height * .60f),
            new(width * .55f, height * .69f), new(width * .71f, height * .63f),
            new(width * .89f, height * .73f), new(width, height * .65f),
            new(width, height), new(0f, height)
        ], [NearRock]);

        // Sparse strata stay within the sector and remain dimmer than all playable ledges.
        for (int index = 0; index < 5; index++)
        {
            float y = height * (.46f + index * .095f);
            DrawLine(new Vector2(width * .04f, y),
                new Vector2(width * (.19f + index * .035f), y - 17f),
                new Color(.18f, .25f, .25f, .30f), 2f, true);
        }
    }

    private void DrawEntrance(float width, float height)
    {
        DrawBrokenArch(new Vector2(width * .28f, height * .56f), 178f, height * .39f,
            StoneShadow, 28f);
        DrawBrokenArch(new Vector2(width * .76f, height * .57f), 116f, height * .38f,
            Stone, 23f);
        DrawLine(new Vector2(width * .16f, height * .20f),
            new Vector2(width * .38f, height * .16f), CopperShadow, 7f, true);
        DrawGear(new Vector2(width * .56f, height * .67f), 91f, CopperShadow);
        DrawRubble(new Vector2(width * .37f, height * .92f), 1.1f);
        DrawRubble(new Vector2(width * .82f, height * .92f), .8f);
    }

    private void DrawDescent(float width, float height)
    {
        DrawShaftPier(width * .16f, height, 56f);
        DrawShaftPier(width * .81f, height, 72f);
        DrawChain(width * .37f, 0f, height * .67f, 1f);
        DrawChain(width * .66f, height * .08f, height * .95f, .7f);
        DrawGear(new Vector2(width * .37f, height * .27f), 115f, CopperShadow);
        DrawGear(new Vector2(width * .66f, height * .64f), 78f, Copper);
        DrawLine(new Vector2(width * .23f, height * .26f),
            new Vector2(width * .49f, height * .26f), Stone, 20f, true);
        DrawLine(new Vector2(width * .52f, height * .66f),
            new Vector2(width * .78f, height * .58f), StoneShadow, 18f, true);
        DrawBrokenArch(new Vector2(width * .48f, height * .91f), 165f,
            height * .08f, StoneShadow, 31f);
    }

    private void DrawBasin(float width, float height)
    {
        DrawBrokenArch(new Vector2(width * .26f, height * .61f), 154f,
            height * .34f, StoneShadow, 36f);
        DrawBrokenArch(new Vector2(width * .73f, height * .59f), 203f,
            height * .36f, StoneShadow, 29f);
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
        DrawGear(new Vector2(width * .53f, height * .81f), 103f, CopperShadow);
        DrawRubble(new Vector2(width * .31f, height * .94f), 1.35f);
        DrawRubble(new Vector2(width * .76f, height * .94f), 1.1f);
    }

    private void DrawAscent(float width, float height)
    {
        DrawShaftPier(width * .20f, height, 48f);
        DrawShaftPier(width * .77f, height, 64f);
        for (int tier = 0; tier < 3; tier++)
        {
            float y = height * (.24f + tier * .29f);
            float left = width * (.11f + (tier % 2) * .12f);
            float right = width * (.74f + (tier % 2) * .12f);
            DrawBridge(new Vector2(left, y), new Vector2(right, y - 36f), 55f);
        }
        DrawChain(width * .88f, 0f, height * .44f, .7f);
        DrawGear(new Vector2(width * .88f, height * .36f), 68f, CopperShadow);
        DrawBrokenArch(new Vector2(width * .44f, height * .12f), 112f,
            height * .07f, StoneShadow, 28f);
    }

    private void DrawTemple(float width, float height)
    {
        Vector2 center = new(width * .77f, height * .52f);
        DrawBrokenArch(center + new Vector2(0f, 23f), 202f, height * .36f, Stone, 35f);
        DrawBrokenArch(new Vector2(width * .28f, height * .58f), 128f,
            height * .37f, StoneShadow, 29f);
        DrawChain(center.X - 69f, 0f, center.Y - 105f, .9f);
        DrawChain(center.X + 62f, 0f, center.Y - 105f, .9f);

        // A large broken bell reads as a distant landmark, without an emissive core or target ring.
        DrawPolygon([
            center + new Vector2(-47f, -126f), center + new Vector2(43f, -126f),
            center + new Vector2(68f, -89f), center + new Vector2(83f, 6f),
            center + new Vector2(118f, 61f), center + new Vector2(28f, 61f),
            center + new Vector2(12f, 23f), center + new Vector2(-14f, 45f),
            center + new Vector2(-28f, 61f), center + new Vector2(-118f, 61f),
            center + new Vector2(-84f, 6f), center + new Vector2(-68f, -89f)
        ], [CopperShadow]);
        DrawArc(center + new Vector2(0f, -91f), 44f, Mathf.Pi, Mathf.Tau, 24, Copper, 6f, true);
        DrawPolyline([center + new Vector2(-20f, -113f), center + new Vector2(-4f, -70f),
            center + new Vector2(-18f, -35f), center + new Vector2(12f, 23f)], Recess, 7f, true);
        DrawLine(center + new Vector2(-91f, 39f), center + new Vector2(-25f, 39f), Copper, 7f, true);
        DrawLine(center + new Vector2(29f, 39f), center + new Vector2(92f, 39f), Copper, 7f, true);
        DrawLine(center + new Vector2(-4f, 65f), center + new Vector2(-4f, 98f), StoneShadow, 10f, true);
        DrawRubble(new Vector2(width * .67f, height * .94f), 1.25f);
        DrawRubble(new Vector2(width * .88f, height * .94f), .95f);
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

    private void DrawShaftPier(float x, float height, float width)
    {
        DrawRect(new Rect2(x - width / 2f, 0f, width, height), StoneShadow);
        DrawLine(new Vector2(x - width * .28f, 0f),
            new Vector2(x - width * .28f, height), Stone, 5f, true);
        for (float y = 80f; y < height; y += 150f)
            DrawLine(new Vector2(x - width / 2f, y), new Vector2(x + width / 2f, y + 3f),
                Recess, 6f, true);
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
