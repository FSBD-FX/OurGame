using System.Collections.Generic;
using Godot;

namespace Knight.Presentation;

public enum PlayerFormalLayerKind
{
    Body,
    NearArm,
    FarArm,
    NearLeg,
    FarLeg,
    Weapon,
    Mantle,
    ShoulderCore,
    Lantern,
    Tether
}

public partial class PlayerFormalLayer : Node2D
{
    private static readonly Color Ink = new("11161a");
    private static readonly Color DeepCloth = new("182b2b");
    private static readonly Color ClothLight = new("31534b");
    private static readonly Color ClothBody = new("254139");
    private static readonly Color ClothFold = new("345247");
    private static readonly Color ClothShade = new("1b302d");
    private static readonly Color Bone = new("e5ddc7");
    private static readonly Color BoneShadow = new("978f7d");
    private static readonly Color Brass = new("b68a42");
    private static readonly Color BrassLight = new("e4c276");
    private static readonly Color ThreadGlow = new("75e0d6");

    [Export] public PlayerFormalLayerKind Kind { get; set; }

    private PoseTransform2D _mantleSecondary = PoseTransform2D.Identity;

    public void SetMantleSecondaryMotion(PoseTransform2D motion)
    {
        _mantleSecondary = motion;
        QueueRedraw();
    }

    /// <summary>Shared real Draw consumer for silhouette, folds, outline and collar trim.</summary>
    public Vector2 MapMantlePoint(Vector2 restPoint) => PlayerMantleShape.Map(restPoint, _mantleSecondary);

    private Vector2[] MapMantlePoints(Vector2[] restPoints) => MapMantleEdges(restPoints, closed: true);

    private Vector2[] MapMantleEdges(Vector2[] restPoints, bool closed)
    {
        // A fixed vertex alone cannot pin the part of a straight edge above the
        // collar. Split every crossing at the exact rest-band boundary before
        // mapping. Fill includes the closing edge; the authored outline is open.
        var mapped = new List<Vector2>(restPoints.Length + 2);
        for (int index = 0; index < restPoints.Length; index++)
        {
            Vector2 start = restPoints[index];
            mapped.Add(MapMantlePoint(start));
            if (!closed && index == restPoints.Length - 1) continue;
            Vector2 end = restPoints[(index + 1) % restPoints.Length];
            if (!(start.Y < -3f && end.Y > -3f || start.Y > -3f && end.Y < -3f)) continue;
            double fraction = (-3d - start.Y) / ((double)end.Y - start.Y);
            float x = (float)(start.X + ((double)end.X - start.X) * fraction);
            mapped.Add(MapMantlePoint(new Vector2(x, -3f)));
        }
        return mapped.ToArray();
    }

    public override void _Ready() => QueueRedraw();

    public override void _Draw()
    {
        switch (Kind)
        {
            case PlayerFormalLayerKind.Body: DrawBody(); break;
            case PlayerFormalLayerKind.NearArm: DrawLimb(9f, 24f, true); break;
            case PlayerFormalLayerKind.FarArm: DrawLimb(8f, 22f, false); break;
            case PlayerFormalLayerKind.NearLeg: DrawLimb(11f, 25f, true); break;
            case PlayerFormalLayerKind.FarLeg: DrawLimb(10f, 23f, false); break;
            case PlayerFormalLayerKind.Weapon: DrawWeapon(); break;
            case PlayerFormalLayerKind.Mantle: DrawMantle(); break;
            case PlayerFormalLayerKind.ShoulderCore: DrawCore(); break;
            case PlayerFormalLayerKind.Lantern: DrawLantern(); break;
            case PlayerFormalLayerKind.Tether: DrawTether(); break;
        }
    }

    private void DrawBody()
    {
        DrawPolygon([new(-15f, 4f), new(-11f, 24f), new(11f, 24f), new(15f, 4f), new(9f, -5f), new(-9f, -5f)],
            [DeepCloth]);
        // The original undergarment contour frames a broad ceramic chest, not new armor.
        DrawPolygon([new(-11f, 5f), new(-8f, 19f), new(8f, 19f), new(11f, 5f),
                new(7f, -2f), new(-7f, -2f)], [Bone]);
        DrawPolygon([new(4f, -1f), new(10f, 5f), new(7f, 18f), new(1f, 18f)],
            [Bone.Darkened(.18f)]);
        DrawPolygon([new(-7f, -2f), new(7f, -2f), new(9f, 2f), new(-9f, 2f)],
            [BoneShadow]);
        DrawPolygon([new(-8f, 19f), new(8f, 19f), new(7f, 21f), new(-7f, 21f)],
            [Ink]);
        DrawCircle(new Vector2(0f, -18f), 15f, Ink);
        DrawCircle(new Vector2(0f, -19f), 13f, Bone);
        DrawArc(new Vector2(0f, -19f), 13f, 0f, Mathf.Tau, 32, BoneShadow, 1.5f);
        DrawLine(new Vector2(-8f, -23f), new Vector2(8f, -23f), BoneShadow, 1.2f);
        DrawLine(new Vector2(4f, -19f), new Vector2(10f, -19f), ThreadGlow, 2.4f);
    }

    private void DrawLimb(float width, float length, bool near)
    {
        // Both sides are the same ceramic; overlap and local recesses carry depth.
        Color color = Bone;
        DrawStyleBox(new Rect2(-width / 2f, -4f, width, length), color, Ink);
        float inset = 1.4f;
        float sideWidth = width * .20f;
        DrawRect(new Rect2(width / 2f - inset - sideWidth, -4f + inset,
            sideWidth, length - inset * 2f), Bone.Darkened(near ? .15f : .20f));
        DrawRect(new Rect2(-width / 2f + inset, -4f + inset,
            width - inset * 2f, 2.5f), BoneShadow);
        DrawCircle(new Vector2(0f, length - 3f), width * 0.52f, Ink);
        DrawCircle(new Vector2(0f, length - 3f), width * 0.38f, color);
    }

    private void DrawWeapon()
    {
        DrawLine(new Vector2(-2f, 0f), new Vector2(45f, -6f), Ink, 5f);
        DrawLine(new Vector2(-2f, 0f), new Vector2(45f, -6f), Bone, 2.2f);
        DrawCircle(new Vector2(2f, 0f), 5f, Brass);
        DrawCircle(new Vector2(2f, 0f), 2f, ThreadGlow);
    }

    private void DrawMantle()
    {
        Vector2[] restPoints = [new(-17f, -9f), new(13f, -8f), new(20f, 11f), new(13f, 29f),
            new(4f, 24f), new(-4f, 31f), new(-11f, 24f), new(-20f, 28f), new(-17f, 7f)];
        Vector2[] points = MapMantlePoints(restPoints);
        DrawPolygon(points, [ClothBody]);
        // Broad shoulder-to-hem planes sit inside one continuous cloth silhouette.
        DrawPolygon(MapMantlePoints([new(-14f, -6f), new(-7f, -6f), new(-4f, 22f),
                new(-10f, 20f), new(-15f, 23f), new(-13f, 7f)]), [ClothFold]);
        DrawPolygon(MapMantlePoints([new(-6f, -6f), new(5f, -5f), new(8f, 20f),
                new(4f, 20f), new(-2f, 26f), new(-4f, 18f)]), [ClothLight]);
        DrawPolygon(MapMantlePoints([new(6f, -5f), new(11f, -5f), new(16f, 10f),
                new(12f, 24f), new(8f, 21f)]), [ClothShade]);
        DrawPolyline(MapMantleEdges(restPoints, closed: false), ClothLight, 1.2f, true);
        DrawLine(MapMantlePoint(new Vector2(-12f, -3f)),
            MapMantlePoint(new Vector2(11f, -3f)), Brass, 1.4f);
    }

    private void DrawCore()
    {
        DrawCircle(Vector2.Zero, 8f, Ink);
        DrawCircle(Vector2.Zero, 6.5f, Brass);
        DrawCircle(Vector2.Zero, 3.5f, ThreadGlow);
        DrawArc(Vector2.Zero, 6.5f, 0f, Mathf.Tau, 24, BrassLight, 1.2f);
    }

    private void DrawLantern()
    {
        DrawCircle(Vector2.Zero, 9f, new Color(0.1f, 0.3f, 0.29f, 0.32f));
        DrawCircle(Vector2.Zero, 6f, Ink);
        DrawCircle(Vector2.Zero, 4.5f, ThreadGlow);
        DrawArc(Vector2.Zero, 8f, -2.5f, 0.6f, 18, BrassLight, 2f);
        DrawLine(new Vector2(-5f, 7f), new Vector2(5f, 7f), Brass, 2f);
    }

    private void DrawTether()
    {
        DrawPolyline([new(-2f, -1f), new(-7f, 8f), new(-3f, 16f), new(6f, 21f)], Ink, 3.2f);
        DrawPolyline([new(-2f, -1f), new(-7f, 8f), new(-3f, 16f), new(6f, 21f)], ThreadGlow.Darkened(0.2f), 1.2f);
    }

    private void DrawStyleBox(Rect2 rect, Color fill, Color outline)
    {
        DrawRect(rect, fill);
        DrawRect(rect, outline, false, 1.2f);
    }
}
