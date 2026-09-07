using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Tools;

namespace Knight.Build.Art;

public partial class BuildFallenBellkeeperFormalVisual : SceneTree
{
    private const string ScenePath = "res://scenes/presentation/FallenBellkeeperFormalVisual.tscn";
    private const string AtlasPath = "res://assets/boss/fallen-bellkeeper/fallen-bellkeeper.png";
    private readonly record struct PartSpec(string Name, FallenBellkeeperPartKind Kind,
        Vector2 Pivot, int ZIndex, Rect2 Source, Vector2 AtlasOrigin, Rect2 DrawBounds);

    private static readonly PartSpec[] Parts =
    [
        new("Body", FallenBellkeeperPartKind.Body, new Vector2(0, 0), 1,
            new Rect2(0, 0, 128, 176), new Vector2(64, 88), new Rect2(-52, -73, 100, 156)),
        new("Core", FallenBellkeeperPartKind.Core, new Vector2(0, -24), 2,
            new Rect2(128, 0, 80, 80), new Vector2(168, 40), new Rect2(-35, -38, 70, 76)),
        new("ArmorShell", FallenBellkeeperPartKind.ArmorShell, new Vector2(-8, -15), 3,
            new Rect2(240, 0, 176, 144), new Vector2(328, 72), new Rect2(-78, -66, 151, 127)),
        new("Chain", FallenBellkeeperPartKind.Chain, new Vector2(30, -44), 4,
            new Rect2(128, 80, 112, 80), new Vector2(161, 112), new Rect2(-9, -9, 78, 55)),
        new("Hammer", FallenBellkeeperPartKind.Hammer, new Vector2(34, -2), 5,
            new Rect2(240, 144, 128, 112), new Vector2(264, 210), new Rect2(-17, -62, 102, 106))
    ];

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://scenes/presentation"));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://assets/boss/fallen-bellkeeper"));
        Image atlas = BuildAtlas();
        if (atlas.SavePng(ProjectSettings.GlobalizePath(AtlasPath)) != Error.Ok)
        {
            GD.PushError("BuildFallenBellkeeperFormalVisual: failed to save Boss atlas.");
            Quit(1);
            return;
        }

        var root = new FallenBellkeeperFormalVisual { Name = "FallenBellkeeperFormalVisual" };
        foreach (PartSpec spec in Parts)
        {
            FallenBellkeeperFormalPart part = Part(spec.Name, spec.Kind, spec.Pivot, spec.ZIndex);
            part.SetMeta("semantic_layer", spec.Name.ToLowerInvariant());
            part.SetMeta("atlas_source", spec.Source);
            part.SetMeta("atlas_origin", spec.AtlasOrigin);
            part.SetMeta("atlas_content", MeasureOpaqueBounds(atlas, spec.Source));
            part.SetMeta("pivot", spec.Pivot);
            part.SetMeta("draw_bounds", spec.DrawBounds);
            root.AddChild(part);
        }
        root.SetMeta("profile_bounds", new Rect2(-86, -82, 220, 165));
        AddReadabilityCues(root.GetNode<FallenBellkeeperFormalPart>("Body"));
        Error error = SceneBuildUtil.PackAndSave(root, ScenePath);
        root.Free();
        if (error != Error.Ok)
        {
            GD.PushError($"BuildFallenBellkeeperFormalVisual failed: {error}");
            Quit(1);
            return;
        }

        FallenBellkeeperFormalVisual copy = GD.Load<PackedScene>(ScenePath)
            .Instantiate<FallenBellkeeperFormalVisual>();
        bool valid = ValidateRig(copy);
        copy.Free();
        if (!valid)
        {
            GD.PushError("BuildFallenBellkeeperFormalVisual contract validation failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildFallenBellkeeperFormalVisual: five-part Boss rig packed and validated.");
        Quit(0);
    }

    private static bool ValidateRig(FallenBellkeeperFormalVisual root)
    {
        int previousZ = int.MinValue;
        foreach (PartSpec spec in Parts)
        {
            FallenBellkeeperFormalPart part = root.GetNodeOrNull<FallenBellkeeperFormalPart>(spec.Name);
            if (part == null || part.Kind != spec.Kind || part.Position != spec.Pivot ||
                part.ZIndex != spec.ZIndex || part.ZIndex <= previousZ ||
                part.Scale.X <= 0f || part.Scale.Y <= 0f ||
                !Mathf.IsEqualApprox(part.Scale.X, part.Scale.Y) ||
                !float.IsFinite(part.Position.X) || !float.IsFinite(part.Position.Y) ||
                part.GetMeta("semantic_layer").AsString() != spec.Name.ToLowerInvariant() ||
                part.GetMeta("atlas_source").AsRect2() != spec.Source ||
                !part.GetMeta("atlas_origin").AsVector2().IsEqualApprox(spec.AtlasOrigin) ||
                !part.GetMeta("pivot").AsVector2().IsEqualApprox(spec.Pivot) ||
                part.GetMeta("draw_bounds").AsRect2() != spec.DrawBounds) return false;
            previousZ = part.ZIndex;
        }
        if (root.GetMeta("profile_bounds").AsRect2() != new Rect2(-86, -82, 220, 165) ||
            root.GetNodeOrNull<Polygon2D>("Body/FacingIndicator") == null ||
            root.GetNodeOrNull<Line2D>("Body/FootContact") == null) return false;
        Node2D hammer = root.GetNode<Node2D>("Hammer");
        Vector2 restPosition = hammer.Position;
        float restRotation = hammer.Rotation;
        root.PresentAttack(new AttackPresentationFrame(
            new AttackTimelineFrame(1, EnemyAttackKind.BossHammerThrow, AttackPhase.Active,
                1f, .5f, Vector2.Zero, false), default));
        return root.CurrentAttackFrame.Timeline.Kind == EnemyAttackKind.BossHammerThrow &&
            (hammer.Position.DistanceSquaredTo(restPosition) > .0001f ||
             Mathf.Abs(Mathf.AngleDifference(hammer.Rotation, restRotation)) > .0001f);
    }

    private static FallenBellkeeperFormalPart Part(string name, FallenBellkeeperPartKind kind,
        Vector2 position, int zIndex) => new()
    {
        Name = name, Kind = kind, Position = position, ZIndex = zIndex
    };

    private static void AddReadabilityCues(Node2D body)
    {
        body.AddChild(new Polygon2D
        {
            Name = "FacingIndicator",
            Position = new Vector2(20, -58),
            Polygon = [new(0, 0), new(12, 4), new(0, 8)],
            Color = new Color("e1dbc7")
        });
        body.AddChild(new Line2D
        {
            Name = "FootContact",
            Position = new Vector2(0, 75),
            Points = [new Vector2(-39, 0), new Vector2(34, 0)],
            Width = 5f,
            DefaultColor = new Color("c5bda6", .82f)
        });
    }

    private static Image BuildAtlas()
    {
        Image image = Image.CreateEmpty(512, 256, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        Color ink = new("101416");

        Vector2I body = Origin("Body");
        FillPolygon(image, Shift(body, [new(-43, -55), new(32, -62), new(48, 42), new(22, 63),
            new(-37, 59), new(-52, 23)]), ink);
        FillPolygon(image, Shift(body, [new(-36, -49), new(26, -55), new(39, 38), new(17, 54),
            new(-31, 51), new(-44, 20)]), new Color("40353a"));
        Stroke(image, body + new Vector2I(-27, 54), body + new Vector2I(-31, 74), 8, ink);
        Stroke(image, body + new Vector2I(19, 54), body + new Vector2I(25, 74), 8, ink);
        Disc(image, body + new Vector2I(17, -58), 15, ink);
        Disc(image, body + new Vector2I(17, -58), 11, new Color("cfc7b4"));

        Vector2I core = Origin("Core");
        Disc(image, core, 22, new Color("315b55", .35f));
        Disc(image, core, 15, ink);
        Disc(image, core, 11, new Color("75e3d4"));
        Stroke(image, core + new Vector2I(-22, 0), core + new Vector2I(-35, -14), 1,
            new Color("75e3d4"));
        Stroke(image, core + new Vector2I(22, 0), core + new Vector2I(35, 14), 1,
            new Color("75e3d4"));

        Vector2I armor = Origin("ArmorShell");
        FillPolygon(image, Shift(armor, [new(-68, -62), new(54, -66), new(73, 43), new(50, 57),
            new(2, 46), new(-42, 61), new(-78, 34)]), ink);
        FillPolygon(image, Shift(armor, [new(-60, -54), new(47, -58), new(64, 37), new(46, 48),
            new(2, 38), new(-39, 52), new(-69, 29)]), new Color("9f7437"));
        Stroke(image, armor + new Vector2I(-51, -20), armor + new Vector2I(-17, -5), 2,
            new Color("315b55"));
        Stroke(image, armor + new Vector2I(-17, -5), armor + new Vector2I(5, -32), 2,
            new Color("315b55"));
        Stroke(image, armor + new Vector2I(5, -32), armor + new Vector2I(25, 8), 2,
            new Color("315b55"));

        Vector2I chain = Origin("Chain");
        for (int index = 0; index < 5; index++)
        {
            Vector2I center = chain + new Vector2I(index * 15, index * 7);
            Disc(image, center, 9, ink);
            Disc(image, center, 6, Colors.Transparent);
            Stroke(image, center + new Vector2I(-5, 0), center + new Vector2I(5, 0), 1,
                new Color("d9b968"));
        }

        Vector2I hammer = Origin("Hammer");
        Stroke(image, hammer + new Vector2I(-8, 35), hammer + new Vector2I(50, -28), 8, ink);
        Stroke(image, hammer + new Vector2I(-6, 32), hammer + new Vector2I(48, -26), 4,
            new Color("d9b968"));
        FillPolygon(image, Shift(hammer, [new(21, -62), new(76, -56), new(85, -18), new(35, -8)]), ink);
        FillPolygon(image, Shift(hammer, [new(27, -56), new(70, -51), new(78, -23), new(39, -15)]),
            new Color("9f7437"));
        return image;
    }

    private static Vector2I Origin(string name)
    {
        PartSpec spec = System.Array.Find(Parts, item => item.Name == name);
        return new Vector2I(Mathf.RoundToInt(spec.AtlasOrigin.X), Mathf.RoundToInt(spec.AtlasOrigin.Y));
    }

    private static Vector2I[] Shift(Vector2I origin, Vector2I[] points)
    {
        var shifted = new Vector2I[points.Length];
        for (int index = 0; index < points.Length; index++) shifted[index] = points[index] + origin;
        return shifted;
    }

    private static Rect2 MeasureOpaqueBounds(Image image, Rect2 source)
    {
        int left = Mathf.RoundToInt(source.Position.X);
        int top = Mathf.RoundToInt(source.Position.Y);
        int right = Mathf.RoundToInt(source.End.X);
        int bottom = Mathf.RoundToInt(source.End.Y);
        int minX = right;
        int minY = bottom;
        int maxX = left - 1;
        int maxY = top - 1;
        for (int y = top; y < bottom; y++)
        for (int x = left; x < right; x++)
        {
            if (image.GetPixel(x, y).A <= .05f) continue;
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }
        return maxX < minX ? new Rect2() : new Rect2(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static void FillPolygon(Image image, Vector2I[] points, Color color)
    {
        int minX = points[0].X;
        int maxX = minX;
        int minY = points[0].Y;
        int maxY = minY;
        foreach (Vector2I point in points)
        {
            minX = Mathf.Min(minX, point.X);
            maxX = Mathf.Max(maxX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxY = Mathf.Max(maxY, point.Y);
        }
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
            if (Contains(points, new Vector2(x + .5f, y + .5f))) image.SetPixel(x, y, color);
    }

    private static bool Contains(Vector2I[] polygon, Vector2 point)
    {
        bool inside = false;
        for (int first = 0, second = polygon.Length - 1; first < polygon.Length; second = first++)
        {
            Vector2I a = polygon[first];
            Vector2I b = polygon[second];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (float)(b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    private static void Stroke(Image image, Vector2I from, Vector2I to, int radius, Color color)
    {
        int steps = Mathf.Max(Mathf.Abs(to.X - from.X), Mathf.Abs(to.Y - from.Y));
        for (int step = 0; step <= steps; step++)
        {
            float t = steps == 0 ? 0f : (float)step / steps;
            Disc(image, new Vector2I(Mathf.RoundToInt(Mathf.Lerp(from.X, to.X, t)),
                Mathf.RoundToInt(Mathf.Lerp(from.Y, to.Y, t))), radius, color);
        }
    }

    private static void Disc(Image image, Vector2I center, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
        {
            Vector2I pixel = center + new Vector2I(x, y);
            if (pixel.X >= 0 && pixel.Y >= 0 && pixel.X < image.GetWidth() && pixel.Y < image.GetHeight() &&
                x * x + y * y <= radiusSquared) image.SetPixelv(pixel, color);
        }
    }
}
