using Godot;
using Knight.Abilities;

namespace Knight.Presentation;

/// <summary>
/// Static material skin for an existing point target. Position, material and radius are supplied
/// by the builder; this leaf neither owns physics nor changes the authoritative grab surface.
/// All authored vertices and strokes fit inside the normalized 24-pixel hit radius.
/// </summary>
public partial class CanyonAnchorVisual : Node2D
{
    [Export] public GrappleSurfaceMaterial SurfaceMaterial { get; set; }
    [Export] public float Radius { get; set; } = 24f;

    private static readonly Color Ink = new("152226");
    private static readonly Color Brass = new("b59152");
    private static readonly Color BrassShade = new("6d6248");
    private static readonly Color BrassLight = new("e4c985");
    private static readonly Color Stone = new("537d6c");
    private static readonly Color StoneLight = new("83aa8c");
    private static readonly Color StoneShade = new("355649");
    private static readonly Color Metal = new("586e73");
    private static readonly Color MetalLight = new("a2c1bd");
    private static readonly Color MetalShade = new("354b52");
    private static readonly Color Ice = new("729daf");
    private static readonly Color IceLight = new("b2d9e4");
    private static readonly Color IceShade = new("466e88");
    private static readonly Color Seal = new("5d5062");
    private static readonly Color SealShade = new("322f3c");
    private static readonly Color SealEdge = new("a297aa");

    private static readonly Vector2[] AnchorBack =
    [
        new(-20f, -8f), new(-8f, -21f), new(8f, -21f), new(20f, -8f),
        new(20f, 8f), new(8f, 21f), new(-8f, 21f), new(-20f, 8f)
    ];
    private static readonly Vector2[] AnchorCore =
        [new(0f, -4f), new(4f, 0f), new(0f, 4f), new(-4f, 0f)];
    private static readonly Vector2[] StoneBody =
    [
        new(-21f, -5f), new(-13f, -19f), new(5f, -22f), new(19f, -12f),
        new(22f, 4f), new(11f, 20f), new(-8f, 21f), new(-20f, 9f)
    ];
    private static readonly Vector2[] StoneCrown =
        [new(-13f, -19f), new(5f, -22f), new(19f, -12f), new(4f, -7f), new(-15f, -8f)];
    private static readonly Vector2[] StoneFoot =
        [new(-20f, 9f), new(-3f, 5f), new(14f, 8f), new(11f, 20f), new(-8f, 21f)];
    private static readonly Vector2[] StoneCrackOne =
        [new(-8f, -14f), new(-2f, -4f), new(-7f, 5f), new(-2f, 15f)];
    private static readonly Vector2[] StoneCrackTwo =
        [new(13f, -6f), new(6f, 1f), new(14f, 10f)];
    private static readonly Vector2[] CrystalBody =
    [
        new(0f, -23f), new(15f, -13f), new(20f, 8f), new(6f, 21f),
        new(-12f, 19f), new(-20f, 3f), new(-12f, -15f)
    ];
    private static readonly Vector2[] CrystalLightFace =
        [new(0f, -23f), new(15f, -13f), new(3f, 3f), new(-12f, 19f), new(-7f, -7f)];
    private static readonly Vector2[] CrystalShadowFace =
        [new(15f, -13f), new(20f, 8f), new(6f, 21f), new(3f, 3f)];
    private static readonly Vector2[] SealBody =
    [
        new(-19f, -11f), new(-11f, -19f), new(13f, -19f), new(19f, -13f),
        new(19f, 11f), new(11f, 19f), new(-13f, 19f), new(-19f, 13f)
    ];

    public override void _Draw()
    {
        if (!float.IsFinite(Radius) || Radius <= 0f) return;
        // Scale draw commands, not the node or hit area. Static point arrays are shared between
        // instances; CanvasItem retains the commands without an animation/redraw process loop.
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One * (Radius / 24f));
        switch (SurfaceMaterial)
        {
            case GrappleSurfaceMaterial.DedicatedAnchor:
                DrawDedicatedAnchor();
                break;
            case GrappleSurfaceMaterial.GripStone:
                DrawGripStone();
                break;
            case GrappleSurfaceMaterial.TempleMetal:
                DrawTempleMetal();
                break;
            case GrappleSurfaceMaterial.Ice:
                DrawIce();
                break;
            default:
                DrawForbidden();
                break;
        }
    }

    private void DrawDedicatedAnchor()
    {
        DrawColoredPolygon(AnchorBack, Ink);
        DrawArc(Vector2.Zero, 18f, .16f, Mathf.Pi - .16f, 24, BrassShade, 4f, true);
        DrawArc(Vector2.Zero, 18f, Mathf.Pi + .16f, Mathf.Tau - .16f, 24, Brass, 4f, true);
        DrawArc(Vector2.Zero, 13f, -.9f, .9f, 16, BrassLight, 2f, true);
        DrawLine(new Vector2(-22f, 0f), new Vector2(-17f, 0f), Brass, 5f, true);
        DrawLine(new Vector2(17f, 0f), new Vector2(22f, 0f), Brass, 5f, true);
        DrawLine(new Vector2(0f, -22f), new Vector2(0f, -17f), Brass, 5f, true);
        DrawLine(new Vector2(0f, 17f), new Vector2(0f, 22f), BrassShade, 5f, true);
        DrawLine(new Vector2(-8f, 0f), new Vector2(8f, 0f), BrassShade, 2f, true);
        DrawLine(new Vector2(0f, -8f), new Vector2(0f, 8f), BrassShade, 2f, true);
        DrawColoredPolygon(AnchorCore, BrassLight);
    }

    private void DrawGripStone()
    {
        DrawColoredPolygon(StoneBody, Stone);
        DrawColoredPolygon(StoneCrown, StoneLight);
        DrawColoredPolygon(StoneFoot, StoneShade);
        DrawPolyline(StoneCrackOne, Ink, 3f, true);
        DrawPolyline(StoneCrackTwo, Ink, 3f, true);
        DrawLine(new Vector2(-19f, -3f), new Vector2(-15f, 6f), StoneLight, 2f, true);
    }

    private void DrawTempleMetal()
    {
        DrawRect(new Rect2(-16.5f, -16.5f, 33f, 33f), Ink);
        DrawRect(new Rect2(-14.5f, -14.5f, 29f, 29f), Metal);
        DrawRect(new Rect2(-8f, -8f, 16f, 16f), MetalShade);
        DrawLine(new Vector2(-13f, -13f), new Vector2(13f, -13f), MetalLight, 2f, true);
        DrawLine(new Vector2(-13f, -13f), new Vector2(-13f, 13f), MetalLight, 2f, true);
        DrawLine(new Vector2(13f, -11f), new Vector2(13f, 13f), MetalShade, 2f, true);
        DrawCircle(new Vector2(-10.5f, -10.5f), 2f, MetalLight);
        DrawCircle(new Vector2(10.5f, -10.5f), 2f, MetalLight);
        DrawCircle(new Vector2(-10.5f, 10.5f), 2f, MetalLight);
        DrawCircle(new Vector2(10.5f, 10.5f), 2f, MetalLight);
        DrawLine(new Vector2(-4f, -4f), new Vector2(4f, 4f), Metal, 2f, true);
    }

    private void DrawIce()
    {
        DrawColoredPolygon(CrystalBody, Ice);
        DrawColoredPolygon(CrystalLightFace, IceLight);
        DrawColoredPolygon(CrystalShadowFace, IceShade);
        DrawLine(new Vector2(-14f, 5f), new Vector2(3f, -12f), IceShade, 2f, true);
        DrawLine(new Vector2(-4f, 13f), new Vector2(13f, -4f), IceLight, 2f, true);
        DrawLine(new Vector2(-14f, -10f), new Vector2(-17f, 1f), IceLight, 2f, true);
    }

    private void DrawForbidden()
    {
        DrawColoredPolygon(SealBody, Seal);
        DrawRect(new Rect2(-13f, -13f, 26f, 26f), SealShade);
        DrawLine(new Vector2(-10f, -8f), new Vector2(9f, -8f), Seal, 2f, true);
        DrawLine(new Vector2(-9f, 8f), new Vector2(10f, 8f), Seal, 2f, true);
        DrawLine(new Vector2(-13f, 13f), new Vector2(13f, -13f), Ink, 7f, true);
        DrawLine(new Vector2(-13f, 13f), new Vector2(13f, -13f), SealEdge, 3f, true);
    }
}
