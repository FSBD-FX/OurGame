using Godot;
using System.Linq;
using Knight.Combat;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Art;

public partial class BuildEnemyFormalVisuals : SceneTree
{
    private const string PresentationRoot = "res://scenes/presentation";
    private const string AssetRoot = "res://assets/enemies/formal";
    private const string ProductionAtlasPath = "res://assets/enemies/formal/enemy-atlas.png";
    private readonly record struct SlotSpec(string Name, EnemyFormalPartKind Kind,
        Vector2 Pivot, int ZIndex);
    private readonly record struct LayerSpec(string Name, Rect2 Source, Vector2 AtlasOrigin,
        Rect2 DrawBounds);

    private static readonly SlotSpec[] RustSlots =
    [
        new("Corruption", EnemyFormalPartKind.RustCorruption, new Vector2(-9, -3), 0),
        new("Body", EnemyFormalPartKind.RustBody, new Vector2(0, 0), 1),
        new("Shell", EnemyFormalPartKind.RustShell, new Vector2(-2, -2), 2),
        new("Weapon", EnemyFormalPartKind.RustWeapon, new Vector2(20, -5), 3)
    ];
    private static readonly SlotSpec[] MothSlots =
    [
        new("Corruption", EnemyFormalPartKind.MothCorruption, new Vector2(-20, -3), 0),
        new("Wings/FarWing", EnemyFormalPartKind.MothFarWing, Vector2.Zero, 1),
        new("Body", EnemyFormalPartKind.MothBody, new Vector2(0, 0), 2),
        new("Wings/NearWing", EnemyFormalPartKind.MothNearWing, Vector2.Zero, 3),
        new("Lamp", EnemyFormalPartKind.MothLamp, new Vector2(0, -5), 4)
    ];
    private static readonly SlotSpec[] LaborerSlots =
    [
        new("Corruption", EnemyFormalPartKind.LaborerCorruption, new Vector2(-12, -5), 0),
        new("Bell", EnemyFormalPartKind.LaborerBell, new Vector2(-12, -12), 1),
        new("Body", EnemyFormalPartKind.LaborerBody, new Vector2(0, 0), 2),
        new("Feet", EnemyFormalPartKind.LaborerFeet, new Vector2(0, 18), 3),
        new("Weapon", EnemyFormalPartKind.LaborerWeapon, new Vector2(18, -2), 4)
    ];

    private static readonly LayerSpec[] RustLayers =
    [
        new("Body", new Rect2(0, 0, 64, 64), new Vector2(32, 32), new Rect2(-30, -29, 58, 55)),
        new("Weapon", new Rect2(64, 0, 50, 20), new Vector2(69, 10), new Rect2(-3, -5, 44, 10)),
        new("Corruption", new Rect2(114, 0, 28, 28), new Vector2(128, 14), new Rect2(-10, -10, 20, 20)),
        new("Accessory", new Rect2(142, 0, 62, 48), new Vector2(174, 27), new Rect2(-24, -24, 49, 37)),
        new("Effects", new Rect2(204, 0, 52, 64), new Vector2(226, 30), new Rect2(-20, -15, 48, 42))
    ];
    private static readonly LayerSpec[] MothLayers =
    [
        new("Body", new Rect2(0, 0, 40, 72), new Vector2(20, 34), new Rect2(-13, -24, 26, 55)),
        new("Weapon", new Rect2(40, 0, 36, 32), new Vector2(56, 16), new Rect2(-8, -7, 16, 16)),
        new("Corruption", new Rect2(76, 0, 30, 30), new Vector2(91, 15), new Rect2(-11, -11, 22, 22)),
        new("Effects", new Rect2(0, 72, 64, 56), new Vector2(24, 89), new Rect2(-19, -15, 47, 49))
    ];
    private static readonly LayerSpec[] MothAccessoryLayers =
    [
        new("FarWing", new Rect2(106, 0, 56, 52), new Vector2(159, 28), new Rect2(-50, -23, 44, 38)),
        new("NearWing", new Rect2(162, 0, 56, 52), new Vector2(165, 28), new Rect2(7, -23, 44, 38))
    ];
    private static readonly LayerSpec[] LaborerLayers =
    [
        new("Body", new Rect2(0, 0, 64, 112), new Vector2(32, 56), new Rect2(-27, -53, 54, 81)),
        new("Weapon", new Rect2(64, 0, 92, 36), new Vector2(73, 18), new Rect2(-6, -14, 76, 28)),
        new("Corruption", new Rect2(156, 0, 32, 32), new Vector2(172, 16), new Rect2(-12, -12, 24, 24)),
        new("Accessory", new Rect2(156, 32, 92, 68), new Vector2(204, 78), new Rect2(-42, -46, 77, 60)),
        new("Effects", new Rect2(64, 40, 76, 112), new Vector2(92, 96), new Rect2(-22, -48, 50, 92))
    ];

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(PresentationRoot));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(AssetRoot));

        Image rust = DrawRustShellAtlas();
        Image moth = DrawMothAtlas();
        Image laborer = DrawLaborerAtlas();
        if (Save(rust, "rust-shell.png") != Error.Ok || Save(moth, "grave-lamp-moth.png") != Error.Ok ||
            Save(laborer, "bell-back-laborer.png") != Error.Ok || SaveCombined(rust, moth, laborer) != Error.Ok)
        {
            GD.PushError("BuildEnemyFormalVisuals: failed to save authored enemy atlases.");
            Quit(1);
            return;
        }

        bool valid = SaveScene(new RustShellFormalVisual { Name = "RustShellFormalVisual" }, RustSlots,
                $"{PresentationRoot}/RustShellFormalVisual.tscn") &&
            SaveMothScene() &&
            SaveScene(new BellBackLaborerFormalVisual { Name = "BellBackLaborerFormalVisual" }, LaborerSlots,
                $"{PresentationRoot}/BellBackLaborerFormalVisual.tscn");
        if (!valid)
        {
            GD.PushError("BuildEnemyFormalVisuals contract validation failed.");
            Quit(1);
            return;
        }

        GD.Print("BuildEnemyFormalVisuals: three distinct formal enemy silhouettes packed and validated.");
        Quit(0);
    }

    private static bool SaveScene<T>(T root, SlotSpec[] slots, string path) where T : Node2D, IEntityVisual2D
    {
        foreach (SlotSpec slot in slots)
        {
            string parentPath = slot.Name.GetBaseDir();
            Node parent = parentPath.Length == 0 || parentPath == "." ? root : root.GetNode(parentPath);
            parent.AddChild(new EnemyFormalVisualPart
            {
                Name = slot.Name.GetFile(),
                Kind = slot.Kind,
                Position = slot.Pivot,
                ZIndex = slot.ZIndex
            });
        }
        AddReadableLayerContract(root);
        Error error = SceneBuildUtil.PackAndSave(root, path);
        root.Free();
        if (error != Error.Ok) return false;
        Node copy = GD.Load<PackedScene>(path).Instantiate();
        bool valid = copy is T && copy is IEntityVisual2D;
        int previousZ = int.MinValue;
        foreach (SlotSpec slot in slots)
        {
            EnemyFormalVisualPart node = copy.GetNodeOrNull<EnemyFormalVisualPart>(slot.Name);
            bool slotValid = node != null && node.Kind == slot.Kind && node.Position == slot.Pivot &&
                node.ZIndex == slot.ZIndex && node.ZIndex > previousZ &&
                node.Scale.X > 0f && node.Scale.Y > 0f &&
                Mathf.IsEqualApprox(node.Scale.X, node.Scale.Y) &&
                float.IsFinite(node.Position.X) && float.IsFinite(node.Position.Y);
            if (!slotValid)
                GD.PushError($"{path}/{slot.Name} functional slot validation failed");
            valid &= slotValid;
            previousZ = slot.ZIndex;
        }
        bool actionSupport = ValidateActionSupport(copy);
        if (!actionSupport) GD.PushError($"{path} real-slot action support validation failed");
        valid &= actionSupport;
        copy.Free();
        return valid;
    }

    private static void AddReadableLayerContract(Node2D root)
    {
        LayerSpec[] specs;
        Rect2 profile;
        Vector2 facingPosition;
        Vector2 footPosition;
        int atlasXOffset;
        string[] accessoryMotionSources;
        switch (root)
        {
            case RustShellFormalVisual:
                specs = RustLayers;
                profile = new Rect2(-42, -42, 104, 68);
                facingPosition = new Vector2(20, -15);
                footPosition = new Vector2(0, 23);
                atlasXOffset = 0;
                accessoryMotionSources = ["Shell"];
                break;
            case GraveLampMothFormalVisual:
                specs = MothLayers;
                profile = new Rect2(-58, -40, 116, 78);
                facingPosition = new Vector2(14, -13);
                footPosition = new Vector2(0, 32);
                atlasXOffset = 256;
                accessoryMotionSources = ["Wings/FarWing", "Wings/NearWing"];
                break;
            default:
                specs = LaborerLayers;
                profile = new Rect2(-55, -72, 150, 116);
                facingPosition = new Vector2(20, -48);
                footPosition = new Vector2(0, 42);
                atlasXOffset = 512;
                accessoryMotionSources = ["Bell"];
                break;
        }

        Image atlas = Image.LoadFromFile(ProjectSettings.GlobalizePath(ProductionAtlasPath));
        root.SetMeta("profile_bounds", profile);
        foreach (LayerSpec spec in specs)
        {
            Node2D layer = root.GetNodeOrNull<Node2D>(spec.Name);
            if (layer == null)
            {
                layer = new Node2D { Name = spec.Name };
                root.AddChild(layer);
            }
            PublishLayerMetadata(layer, spec, atlas, atlasXOffset,
                spec.Name.ToLowerInvariant());
        }

        Node2D accessory = root.GetNodeOrNull<Node2D>("Accessory");
        if (accessory == null)
        {
            accessory = new Node2D { Name = "Accessory" };
            root.AddChild(accessory);
        }
        accessory.ZIndex = -1;
        accessory.SetMeta("semantic_layer", "accessory");
        accessory.SetMeta("atlas_path", ProductionAtlasPath);
        accessory.SetMeta("pivot", accessory.Position);
        accessory.SetMeta("motion_sources", accessoryMotionSources);

        if (root is GraveLampMothFormalVisual)
        {
            for (int index = 0; index < MothAccessoryLayers.Length; index++)
            {
                LayerSpec spec = MothAccessoryLayers[index];
                var sublayer = new Node2D { Name = spec.Name };
                accessory.AddChild(sublayer);
                PublishLayerMetadata(sublayer, spec, atlas, atlasXOffset,
                    $"accessory_{spec.Name.ToLowerInvariant()}");
                sublayer.SetMeta("motion_source", accessoryMotionSources[index]);
            }
            Node2D weapon = root.GetNode<Node2D>("Weapon");
            weapon.ZIndex = -1;
            weapon.SetMeta("motion_sources", new string[] { "Lamp" });
        }

        Node2D effects = root.GetNode<Node2D>("Effects");
        effects.ZIndex = 8;
        var facing = new Polygon2D
        {
            Name = "FacingIndicator",
            Position = facingPosition,
            Polygon = [new(0, 0), new(8, 3), new(0, 6)],
            Color = new Color("e1dbc7")
        };
        facing.SetMeta("draw_bounds", new Rect2(0, 0, 8, 6));
        effects.AddChild(facing);
        var feet = new Line2D
        {
            Name = "FootContact",
            Position = footPosition,
            Points = [new Vector2(-18, 0), new Vector2(17, 0)],
            Width = 3f,
            DefaultColor = new Color("c5bda6", .82f)
        };
        feet.SetMeta("draw_bounds", new Rect2(-20, -2, 39, 4));
        effects.AddChild(feet);
    }

    private static void PublishLayerMetadata(Node2D layer, LayerSpec spec, Image atlas,
        int atlasXOffset, string semanticLayer)
    {
        Vector2 offset = new(atlasXOffset, 0f);
        Rect2 source = new(spec.Source.Position + offset, spec.Source.Size);
        layer.SetMeta("semantic_layer", semanticLayer);
        layer.SetMeta("atlas_path", ProductionAtlasPath);
        layer.SetMeta("atlas_source", source);
        layer.SetMeta("atlas_origin", spec.AtlasOrigin + offset);
        layer.SetMeta("atlas_content", MeasureOpaqueBounds(atlas, source));
        layer.SetMeta("pivot", layer.Position);
        layer.SetMeta("draw_bounds", spec.DrawBounds);
    }

    private static bool SaveMothScene()
    {
        var root = new GraveLampMothFormalVisual { Name = "GraveLampMothFormalVisual" };
        root.AddChild(new Node2D { Name = "Wings", Position = new Vector2(0, 1) });
        return SaveScene(root, MothSlots, $"{PresentationRoot}/GraveLampMothFormalVisual.tscn");
    }

    private static bool ValidateActionSupport(Node visual)
    {
        const float duration = 1f;
        AttackPresentationFrame frame;
        Node2D primary;
        Transform2D before;
        switch (visual)
        {
            case RustShellFormalVisual rust:
                frame = Attack(EnemyAttackKind.RustShellThrust, duration);
                primary = rust.GetNode<Node2D>("Weapon");
                before = primary.Transform;
                rust.PresentAttack(frame);
                break;
            case GraveLampMothFormalVisual moth:
                frame = Attack(EnemyAttackKind.MothDive, duration);
                primary = moth.GetNode<Node2D>("Wings/FarWing");
                before = primary.Transform;
                moth.PresentAttack(frame);
                Node2D nearWing = moth.GetNode<Node2D>("Wings/NearWing");
                return (primary.Position.DistanceSquaredTo(before.Origin) > .0001f ||
                        Mathf.Abs(Mathf.AngleDifference(primary.Rotation, before.Rotation)) > .0001f) &&
                    primary.Rotation * nearWing.Rotation < 0f;
            case BellBackLaborerFormalVisual laborer:
                frame = Attack(EnemyAttackKind.LaborerSweep, duration);
                primary = laborer.GetNode<Node2D>("Weapon");
                before = primary.Transform;
                laborer.PresentAttack(frame);
                break;
            default:
                return false;
        }
        return primary.Position.DistanceSquaredTo(before.Origin) > .0001f ||
            Mathf.Abs(Mathf.AngleDifference(primary.Rotation, before.Rotation)) > .0001f;
    }

    private static AttackPresentationFrame Attack(EnemyAttackKind kind, float duration) => new(
        new AttackTimelineFrame(1, kind, AttackPhase.Active, duration, duration * .5f,
            Vector2.Zero, false), default);

    private static Error Save(Image image, string fileName) =>
        image.SavePng(ProjectSettings.GlobalizePath($"{AssetRoot}/{fileName}"));

    private static Error SaveCombined(Image rust, Image moth, Image laborer)
    {
        Image combined = Image.CreateEmpty(768, 160, false, Image.Format.Rgba8);
        combined.Fill(Colors.Transparent);
        combined.BlitRect(rust, new Rect2I(0, 0, 256, 160), new Vector2I(0, 0));
        combined.BlitRect(moth, new Rect2I(0, 0, 256, 160), new Vector2I(256, 0));
        combined.BlitRect(laborer, new Rect2I(0, 0, 256, 160), new Vector2I(512, 0));
        return Save(combined, "enemy-atlas.png");
    }

    private static Image BaseImage()
    {
        Image image = Image.CreateEmpty(256, 160, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        return image;
    }

    private static Image DrawRustShellAtlas()
    {
        Image image = BaseImage();
        Color ink = new("111416");
        Vector2I body = Origin(RustLayers, "Body");
        FillPolygon(image, Shift(body, [new(-30, 2), new(-25, -17), new(-13, -29), new(12, -27),
            new(27, -11), new(22, 15), new(-22, 15)]), ink);
        FillPolygon(image, Shift(body, [new(-23, 1), new(-18, -14), new(-10, -22), new(10, -21),
            new(20, -9), new(16, 9), new(-17, 9)]), new Color("74402c"));
        Stroke(image, body + new Vector2I(-15, 12), body + new Vector2I(-17, 22), 3, ink);
        Stroke(image, body + new Vector2I(12, 12), body + new Vector2I(14, 22), 3, ink);

        Vector2I weapon = Origin(RustLayers, "Weapon");
        Stroke(image, weapon, weapon + new Vector2I(38, -2), 3, ink);
        Stroke(image, weapon, weapon + new Vector2I(38, -2), 1, new Color("d8d0b9"));

        Disc(image, Origin(RustLayers, "Corruption"), 10, new Color("315b55", .75f));

        Vector2I shell = Origin(RustLayers, "Accessory");
        FillPolygon(image, Shift(shell, [new(-24, 2), new(-20, -13), new(-9, -24), new(12, -22),
            new(25, -8), new(20, 12), new(-17, 12)]), ink);
        FillPolygon(image, Shift(shell, [new(-18, 0), new(-15, -10), new(-6, -18), new(9, -17),
            new(18, -6), new(14, 7), new(-13, 7)]), new Color("74402c"));
        Stroke(image, shell + new Vector2I(-17, -11), shell + new Vector2I(18, -10), 2,
            new Color("ad6e42"));
        Disc(image, shell + new Vector2I(18, -11), 4, new Color("d7a84a"));

        DrawEffectsAtlas(image, Origin(RustLayers, "Effects"), new Vector2I(20, -15),
            new Vector2I(0, 23), 18);
        return image;
    }

    private static Image DrawMothAtlas()
    {
        Image image = BaseImage();
        Color ink = new("111416");
        Vector2I body = Origin(MothLayers, "Body");
        FillPolygon(image, Shift(body, [new(0, -24), new(13, -5), new(8, 20), new(0, 27),
            new(-8, 20), new(-13, -5)]), ink);
        FillPolygon(image, Shift(body, [new(0, -20), new(9, -4), new(5, 16), new(0, 22),
            new(-5, 16), new(-9, -4)]), new Color("c39a4c"));
        Stroke(image, body + new Vector2I(0, 20), body + new Vector2I(-7, 31), 1, new Color("74a6a0"));
        Stroke(image, body + new Vector2I(0, 20), body + new Vector2I(7, 31), 1, new Color("74a6a0"));

        Vector2I weapon = Origin(MothLayers, "Weapon");
        Disc(image, weapon + new Vector2I(0, 1), 7, ink);
        Disc(image, weapon + new Vector2I(0, 1), 5, new Color("c8a755"));
        Disc(image, weapon + new Vector2I(0, 1), 3, new Color("75d9cf"));

        Disc(image, Origin(MothLayers, "Corruption"), 11, new Color("315b55", .7f));

        DrawMothWingAtlas(image, Origin(MothAccessoryLayers, "FarWing"), -1);
        DrawMothWingAtlas(image, Origin(MothAccessoryLayers, "NearWing"), 1);
        DrawEffectsAtlas(image, Origin(MothLayers, "Effects"), new Vector2I(14, -13),
            new Vector2I(0, 32), 18);
        return image;
    }

    private static Image DrawLaborerAtlas()
    {
        Image image = BaseImage();
        Color ink = new("111416");
        Vector2I body = Origin(LaborerLayers, "Body");
        FillPolygon(image, Shift(body, [new(-24, -38), new(19, -37), new(27, 28), new(-27, 28)]), ink);
        FillPolygon(image, Shift(body, [new(-20, -34), new(15, -33), new(21, 24), new(-22, 24)]),
            new Color("574438"));
        Disc(image, body + new Vector2I(10, -42), 11, ink);
        Disc(image, body + new Vector2I(10, -42), 8, new Color("d8d0b9"));

        Vector2I weapon = Origin(LaborerLayers, "Weapon");
        Stroke(image, weapon, weapon + new Vector2I(56, 0), 5, ink);
        Stroke(image, weapon + new Vector2I(7, 0), weapon + new Vector2I(55, 0), 2,
            new Color("dfbf70"));
        Disc(image, weapon + new Vector2I(55, 0), 12, ink);
        Disc(image, weapon + new Vector2I(55, 0), 8, new Color("b88a42"));

        Disc(image, Origin(LaborerLayers, "Corruption"), 12, new Color("315b55", .72f));

        Vector2I bell = Origin(LaborerLayers, "Accessory");
        FillPolygon(image, Shift(bell, [new(-35, -38), new(21, -41), new(34, 12), new(-42, 12)]), ink);
        FillPolygon(image, Shift(bell, [new(-29, -33), new(18, -36), new(28, 7), new(-35, 7)]),
            new Color("b88a42"));
        Stroke(image, bell + new Vector2I(-35, 8), bell + new Vector2I(29, 8), 2, ink);

        DrawEffectsAtlas(image, Origin(LaborerLayers, "Effects"), new Vector2I(20, -48),
            new Vector2I(0, 42), 18);
        return image;
    }

    private static Vector2I Origin(LayerSpec[] specs, string name)
    {
        LayerSpec spec = specs.First(item => item.Name == name);
        return new Vector2I(Mathf.RoundToInt(spec.AtlasOrigin.X), Mathf.RoundToInt(spec.AtlasOrigin.Y));
    }

    private static Vector2I[] Shift(Vector2I origin, Vector2I[] points) =>
        points.Select(point => point + origin).ToArray();

    private static void DrawMothWingAtlas(Image image, Vector2I origin, int side)
    {
        Color ink = new("111416");
        Vector2I hinge = origin + new Vector2I(side * 7, -5);
        Vector2I[] outer = side < 0
            ? [hinge, hinge + new Vector2I(-34, -18), hinge + new Vector2I(-43, 2),
                hinge + new Vector2I(-22, 19)]
            : [hinge, hinge + new Vector2I(34, -18), hinge + new Vector2I(43, 2),
                hinge + new Vector2I(22, 19)];
        Vector2I[] inner = side < 0
            ? [hinge + new Vector2I(-3, 0), hinge + new Vector2I(-31, -13),
                hinge + new Vector2I(-36, 1), hinge + new Vector2I(-20, 14)]
            : [hinge + new Vector2I(3, 0), hinge + new Vector2I(31, -13),
                hinge + new Vector2I(36, 1), hinge + new Vector2I(20, 14)];
        FillPolygon(image, outer, ink);
        FillPolygon(image, inner, new Color("294b4c"));
        for (int index = 0; index < inner.Length; index++)
            Stroke(image, inner[index], inner[(index + 1) % inner.Length], 1, new Color("74a6a0"));
    }

    private static void DrawEffectsAtlas(Image image, Vector2I origin, Vector2I facing,
        Vector2I foot, int halfFootWidth)
    {
        FillPolygon(image, Shift(origin + facing, [new(0, 0), new(8, 3), new(0, 6)]),
            new Color("e1dbc7"));
        Stroke(image, origin + foot + new Vector2I(-halfFootWidth, 0),
            origin + foot + new Vector2I(halfFootWidth - 1, 0), 1, new Color("c5bda6", .82f));
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
        int minX = points.Min(point => point.X);
        int maxX = points.Max(point => point.X);
        int minY = points.Min(point => point.Y);
        int maxY = points.Max(point => point.Y);
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
            if (x * x + y * y <= radiusSquared) image.SetPixelv(center + new Vector2I(x, y), color);
    }
}
