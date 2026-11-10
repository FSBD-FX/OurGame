using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Presentation;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class CanyonMaterialSkinTests
{
    private static readonly PrologueSectorId[] Sectors =
    [
        PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent,
        PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple
    ];

    // Independent authored world literals, not expectations obtained from the builder/layout catalog.
    private static readonly (string Name, int Role, Vector2 Center, Vector2 Size, Rect2 WorldRect)[] EntranceRocks =
    [
        ("EntranceOuterWall", 3, new(24, 360), new(48, 720), new(0, 0, 48, 720)),
        ("StartFloor", 1, new(160, 678), new(320, 84), new(0, 636, 320, 84)),
        ("BridgeFarBank", 1, new(840, 678), new(880, 84), new(400, 636, 880, 84)),
        ("EntranceSkyOverhang", 2, new(324, 40), new(552, 80), new(48, 0, 552, 80)),
        ("RiftEntryCeiling", 2, new(940, 200), new(680, 400), new(600, 0, 680, 400))
    ];

    public static void RunEntranceRockRoles()
    {
        int consumers = 0;
        // Builder first: missing explicit authoring must fail on the real wall as Legacy0,
        // before any new API probe or the still-old saved leaf can hide the useful RED.
        foreach (bool fromBuilder in new[] { true, false })
        {
            PrologueSector sector = fromBuilder ? PrologueCanyonBuildParts.Build(PrologueSectorId.CanyonEntrance) :
                GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(PrologueSectorId.CanyonEntrance))
                    .Instantiate<PrologueSector>();
            try
            {
                foreach (var expected in EntranceRocks)
                {
                    string label = $"{(fromBuilder ? "builder" : "packed")}/{expected.Name}";
                    var skin = sector.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/" + expected.Name);
                    TestAssert.Equal(expected.Role, ReadRockRole(skin),
                        $"ENTRANCE_ROCK_ROLE {label} must explicitly author the correct rock use");
                    AssertRockRoleApi(skin);
                    AssertEntranceRockGeometry(sector, skin, expected.Name, expected.Center, expected.Size,
                        expected.WorldRect, label);
                    AssertRockPaintPlan(ReadRockPaintPlan(skin, label), expected.Size, label);
                    consumers++;
                }
                var bridge = sector.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/LowBridgeCeiling");
                TestAssert.Equal(0, ReadRockRole(bridge), "thin LowBridgeCeiling keeps actual Legacy0 authoring");
                TestAssert.Equal(0, PaintCommands(ReadRockPaintPlan(bridge, "LowBridgeCeiling")).Length,
                    "thin LowBridgeCeiling retains its old drawing branch, not new role paint");
                TestAssert.Equal(5, sector.GetNode("VisualRoot/GameplayReadable").GetChildren()
                    .OfType<CanyonPlatformVisual>().Count(skin => ReadRockRole(skin) != 0),
                    "only the five literal entrance consumers opt into rock roles");
            }
            finally { sector.Free(); }
        }
        AssertRockRoleIsolation();
        Run(true);
        Run(false);
        TestAssert.Equal(10, consumers, "both builder and packed leaf supply all five actual rock consumers");
        GD.Print("ENTRANCE_ROCK_ROLES_OK consumers=10 roles=3 builder_and_saved=true bounded_strokes=true legacy_retained=true pixels=false human=false");
    }

    private static int ReadRockRole(CanyonPlatformVisual skin)
    {
        PropertyInfo role = skin.GetType().GetProperty("RockSkinRole");
        // The old, unmodified skin really uses its old drawing path; do not fabricate a successful plan.
        return role == null ? 0 : Convert.ToInt32(role.GetValue(skin));
    }

    private static void AssertRockRoleApi(CanyonPlatformVisual skin)
    {
        PropertyInfo role = skin.GetType().GetProperty("RockSkinRole");
        TestAssert.True(role != null && role.PropertyType.IsEnum &&
                        role.PropertyType.Name == "CanyonRockSkinRole" &&
                        role.GetCustomAttribute<ExportAttribute>() != null,
            "explicit rock role is an exported CanyonRockSkinRole enum, surviving scene packing");
        TestAssert.True(Enum.GetNames(role.PropertyType).SequenceEqual(new[] { "Legacy", "Ground", "Roof", "Wall" }) &&
                        Enum.GetValues(role.PropertyType).Cast<object>().Select(Convert.ToInt32)
                            .SequenceEqual(new[] { 0, 1, 2, 3 }),
            "rock role serialized values retain Legacy0/Ground1/Roof2/Wall3");
    }

    private static void AssertEntranceRockGeometry(PrologueSector sector, CanyonPlatformVisual skin,
        string name, Vector2 center, Vector2 size, Rect2 worldRect, string label)
    {
        var body = sector.GetNode<StaticBody2D>("Gameplay/Collision/" + name);
        var collision = body.GetNode<CollisionShape2D>("Collision");
        var shape = collision.Shape as RectangleShape2D;
        TestAssert.True(shape != null && !collision.Disabled && !collision.OneWayCollision,
            label + " retains an enabled solid rectangular collider, not an invented opening");
        TestAssert.Equal(size, shape.Size, label + " collider size is the literal authored size");
        TestAssert.Equal(center, body.Position, label + " collider center is the literal authored position");
        TestAssert.Equal(center, skin.Position, label + " skin center is the literal authored position");
        TestAssert.Equal(size, skin.SkinSize, label + " skin rectangle is not stretched or expanded");
        TestAssert.Equal(Vector2.Zero, collision.Position, label + " collider keeps its centered local shape");
        TestAssert.Equal(33u, body.CollisionLayer, label + " retains terrain/grapple collision ownership");
        TestAssert.Equal(0u, body.CollisionMask, label + " retains the original collision mask");
        TestAssert.Equal(1f, collision.OneWayCollisionMargin, label + " retains the original collision margin");
        var profile = body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        TestAssert.Equal(GrappleSurfaceMaterial.GripStone, profile.Material, label + " remains real GripStone");
        TestAssert.True(profile.UseCollisionHitPoint, label + " still grabs the real collision hit point");
        TestAssert.True(!body.HasMeta("catch_shelf") && !skin.CatchShelf,
            label + " does not gain a catch-shelf gameplay/presentation identity");
        TestAssert.True(skin.UseCanyonDepthStaging && skin.SectorId == PrologueSectorId.CanyonEntrance,
            label + " retains entrance depth staging");
        TestAssert.Equal(GrappleSurfaceMaterial.GripStone, skin.SurfaceMaterial,
            label + " paint does not change the material authority");
        TestAssert.Equal("grip-stone-masonry", skin.GetMeta("material_family").AsString(),
            label + " retains its established material family");
        foreach (Node2D node in new Node2D[] { body, collision, skin })
        {
            Transform2D world = WorldTransform(node);
            TestAssert.Equal(Vector2.Right, world.X, label + " world X basis stays unit and unrotated");
            TestAssert.Equal(Vector2.Down, world.Y, label + " world Y basis stays unit and unrotated");
            TestAssert.Equal(center, world.Origin, label + " full hierarchy preserves the literal world center");
            TestAssert.Equal(worldRect, new Rect2(world.Origin - size / 2f, size),
                label + " full hierarchy preserves the literal world rectangle");
        }
        var edge = skin.GetNode<Polygon2D>("ReadableEdge");
        TestAssert.Equal(Vector2.One, edge.Scale, label + " ReadableEdge remains unscaled");
        TestAssert.Equal(0f, edge.Rotation, label + " ReadableEdge remains unrotated");
        Vector2[] edgeWorld = edge.Polygon.Select(point => WorldTransform(edge) * point).ToArray();
        TestAssert.Equal(4, edgeWorld.Length, label + " ReadableEdge remains the four-corner support strip");
        Vector2 minimum = new(edgeWorld.Min(point => point.X), edgeWorld.Min(point => point.Y));
        Vector2 maximum = new(edgeWorld.Max(point => point.X), edgeWorld.Max(point => point.Y));
        TestAssert.Equal(new Rect2(worldRect.Position, new Vector2(size.X, 6f)),
            new Rect2(minimum, maximum - minimum), label + " six-pixel strip keeps the exact real support top and ends");
        TestAssert.Equal(new Color("9dbb95"), edge.Color, label + " keeps the established readable support color");
        AssertVisualOnly(skin);
    }

    private static Transform2D WorldTransform(Node2D node)
    {
        Transform2D transform = node.Transform;
        for (Node parent = node.GetParent(); parent != null; parent = parent.GetParent())
            if (parent is Node2D spatial) transform = spatial.Transform * transform;
        return transform;
    }

    private static object ReadRockPaintPlan(CanyonPlatformVisual skin, string label)
    {
        MethodInfo build = skin.GetType().GetMethod("BuildEntranceRockPaintPlan", Type.EmptyTypes);
        TestAssert.True(build != null && build.ReturnType.IsGenericType &&
                        build.ReturnType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) &&
                        build.ReturnType.GetGenericArguments()[0].Name == "CanyonRockPaintCommand",
            label + " exposes the actual read-only entrance command plan");
        object plan = build.Invoke(skin, null);
        TestAssert.True(plan is IEnumerable, label + " returns real commands, not null or a synthetic fallback");
        return plan;
    }

    private static object[] PaintCommands(object plan) => ((IEnumerable)plan).Cast<object>().ToArray();

    private static (IReadOnlyList<Vector2> Points, Color Tint, float StrokeWidth) PaintCommand(object command)
    {
        Type type = command.GetType();
        PropertyInfo points = type.GetProperty("Points"), tint = type.GetProperty("Tint"), width = type.GetProperty("StrokeWidth");
        TestAssert.True(points?.PropertyType == typeof(IReadOnlyList<Vector2>) &&
                        tint?.PropertyType == typeof(Color) && width?.PropertyType == typeof(float),
            "paint command exposes Points:IReadOnlyList<Vector2>, Tint:Color, StrokeWidth:float");
        return ((IReadOnlyList<Vector2>)points.GetValue(command), (Color)tint.GetValue(command),
            (float)width.GetValue(command));
    }

    private static void AssertRockPaintPlan(object plan, Vector2 size, string label)
    {
        object[] commands = PaintCommands(plan);
        TestAssert.True(commands.Length > 0, label + " submits actual role paint");
        AssertReadOnlySequence(plan, commands[0], label + " command plan");
        bool solidBase = false;
        foreach (object command in commands)
        {
            var paint = PaintCommand(command);
            TestAssert.True(paint.Points != null && float.IsFinite(paint.StrokeWidth) && paint.StrokeWidth >= 0f,
                label + " uses finite nonnegative stroke widths and a real point collection");
            TestAssert.True(float.IsFinite(paint.Tint.R) && float.IsFinite(paint.Tint.G) &&
                            float.IsFinite(paint.Tint.B) && float.IsFinite(paint.Tint.A), label + " tint is finite");
            TestAssert.True(paint.Points.Count >= (paint.StrokeWidth == 0f ? 3 : 2),
                label + " polygons have at least three vertices and strokes at least two");
            AssertReadOnlySequence(paint.Points, paint.Points[0], label + " command points");
            float radius = paint.StrokeWidth / 2f;
            foreach (Vector2 point in paint.Points)
                TestAssert.True(float.IsFinite(point.X) && float.IsFinite(point.Y) &&
                                point.X - radius >= 0f && point.X + radius <= size.X &&
                                point.Y - radius >= 0f && point.Y + radius <= size.Y,
                    $"{label} every vertex/stroke envelope including caps stays inside [0,{size.X}]×[0,{size.Y}]");
            if (paint.StrokeWidth == 0f && paint.Tint.A == 1f && paint.Points.Count == 4 &&
                paint.Points.Contains(Vector2.Zero) && paint.Points.Contains(new Vector2(size.X, 0f)) &&
                paint.Points.Contains(size) && paint.Points.Contains(new Vector2(0f, size.Y)))
            {
                float twiceArea = 0f;
                for (int index = 0; index < paint.Points.Count; index++)
                    twiceArea += paint.Points[index].Cross(paint.Points[(index + 1) % paint.Points.Count]);
                solidBase |= Mathf.Abs(twiceArea) == 2f * size.X * size.Y;
            }
        }
        TestAssert.True(solidBase, label + " opaque full-rectangle base preserves continuous solid rock, not fake holes");
    }

    private static void AssertReadOnlySequence(object sequence, object first, string label)
    {
        // Try overwriting only a newly returned in-memory plan/point wrapper, never scenes or player files.
        if (sequence is IList list)
        {
            bool refused = false;
            try { list[0] = first; }
            catch (NotSupportedException) { refused = true; }
            TestAssert.True(refused, label + " refuses caller overwrite");
        }
        Type genericList = sequence.GetType().GetInterfaces().FirstOrDefault(type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>));
        if (genericList != null)
        {
            bool refused = false;
            try { genericList.GetProperty("Item").SetValue(sequence, first, new object[] { 0 }); }
            catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { refused = true; }
            TestAssert.True(refused, label + " also refuses overwrite through generic IList");
        }
    }

    private static void AssertRockRoleIsolation()
    {
        var geometry = new Dictionary<int, string>();
        var colors = new Dictionary<int, string>();
        foreach (int role in new[] { 1, 2, 3 })
        {
            var skin = new CanyonPlatformVisual
            {
                Name = "NotAnAuthoredRockName", SkinSize = new Vector2(320f, 84f),
                SectorId = PrologueSectorId.CanyonEntrance, SurfaceMaterial = GrappleSurfaceMaterial.GripStone,
                UseCanyonDepthStaging = true
            };
            try
            {
                PropertyInfo roleProperty = skin.GetType().GetProperty("RockSkinRole");
                roleProperty.SetValue(skin, Enum.ToObject(roleProperty.PropertyType, role));
                object plan = ReadRockPaintPlan(skin, "same-size role " + role);
                AssertRockPaintPlan(plan, skin.SkinSize, "same-size role " + role);
                var paints = PaintCommands(plan).Select(PaintCommand).ToArray();
                string Point(Vector2 point) => point.X.ToString("R", CultureInfo.InvariantCulture) + "," +
                    point.Y.ToString("R", CultureInfo.InvariantCulture);
                geometry.Add(role, string.Join("|", paints.Select(paint =>
                    paint.StrokeWidth.ToString("R", CultureInfo.InvariantCulture) + ":" +
                    string.Join(";", paint.Points.Select(Point).OrderBy(value => value, StringComparer.Ordinal)))
                    .OrderBy(value => value, StringComparer.Ordinal)));
                colors.Add(role, string.Join("|", paints.Select(paint => paint.Tint.ToHtml(true))
                    .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)));

                void Empty(string reason) => TestAssert.Equal(0, PaintCommands(ReadRockPaintPlan(skin, reason)).Length,
                    reason + " retains the old branch rather than submitting new entrance paint");
                foreach (PrologueSectorId sector in Sectors.Where(id => id != PrologueSectorId.CanyonEntrance))
                {
                    skin.SectorId = sector;
                    Empty("role " + role + " outside entrance: " + sector);
                }
                skin.SectorId = PrologueSectorId.CanyonEntrance;
                skin.UseCanyonDepthStaging = false;
                Empty("role " + role + " staging off");
                skin.UseCanyonDepthStaging = true;
                foreach (GrappleSurfaceMaterial material in Enum.GetValues<GrappleSurfaceMaterial>()
                             .Where(material => material != GrappleSurfaceMaterial.GripStone))
                {
                    skin.SurfaceMaterial = material;
                    Empty("role " + role + " non-GripStone " + material);
                }
                skin.SurfaceMaterial = GrappleSurfaceMaterial.GripStone;
                foreach (Vector2 invalid in new[] { new Vector2(0, 84), new Vector2(320, 0), new Vector2(-1, 84),
                             new Vector2(320, -1), new Vector2(float.NaN, 84), new Vector2(320, float.NaN),
                             new Vector2(float.PositiveInfinity, 84), new Vector2(320, float.PositiveInfinity) })
                {
                    skin.SkinSize = invalid;
                    Empty("role " + role + " invalid rectangle " + invalid);
                }
                skin.SkinSize = new Vector2(320, 84);
                roleProperty.SetValue(skin, Enum.ToObject(roleProperty.PropertyType, 0));
                Empty("explicit Legacy0 entrance stone");
            }
            finally { skin.Free(); }
        }
        TestAssert.Equal(3, geometry.Values.Distinct(StringComparer.Ordinal).Count(),
            "Ground/Roof/Wall change real geometry at identical dimensions, not just role labels or command order");
        TestAssert.Equal(3, colors.Values.Distinct(StringComparer.Ordinal).Count(),
            "Ground/Roof/Wall have distinct actual tint sets, not just role labels");
        var oldSample = new CanyonPlatformVisual();
        try
        {
            TestAssert.Equal(0, ReadRockRole(oldSample), "old samples default to Legacy0 without inference from name/size");
            TestAssert.Equal(0, PaintCommands(ReadRockPaintPlan(oldSample, "untouched old sample")).Length,
                "default sample does not masquerade as new role paint");
        }
        finally { oldSample.Free(); }
    }

    public static void Run(bool fromBuilder)
    {
        int platforms = 0;
        int targets = 0;
        var materialFamilies = new System.Collections.Generic.Dictionary<GrappleSurfaceMaterial, string>();
        var regionalEdges = new System.Collections.Generic.Dictionary<PrologueSectorId, Color>();
        foreach (PrologueSectorId id in Sectors)
        {
            PrologueSector sector = fromBuilder ? PrologueCanyonBuildParts.Build(id) :
                GD.Load<PackedScene>(PrologueCanyonBuildParts.ScenePath(id)).Instantiate<PrologueSector>();
            try
            {
                Node readable = sector.GetNode("VisualRoot/GameplayReadable");
                foreach (StaticBody2D body in sector.GetNode("Gameplay/Collision").GetChildren()
                             .OfType<StaticBody2D>())
                {
                    GrappleSurface2D profile = body.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
                    var shape = (RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape;
                    Node2D skin = readable.GetNode<Node2D>(body.Name.ToString());
                    AssertMaterial(skin, profile.Material, materialFamilies);
                    TestAssert.Equal(body.Position, skin.Position, $"{id}/{body.Name} skin follows collider center");
                    TestAssert.Equal((int)id, skin.Get("SectorId").AsInt32(),
                        $"{id}/{body.Name} skin carries its regional staging identity");
                    TestAssert.Equal(shape.Size, skin.Get("SkinSize").AsVector2(),
                        $"{id}/{body.Name} skin uses the exact collision rectangle, not an invented ledge");
                    TestAssert.Equal(body.HasMeta("catch_shelf"), skin.Get("CatchShelf").AsBool(),
                        $"{id}/{body.Name} safe-return shelf keeps its explicit presentation identity");
                    var edge = skin.GetNode<Polygon2D>("ReadableEdge");
                    TestAssert.Equal(Vector2.One, edge.Scale, $"{id}/{body.Name} readable edge is not stretched");
                    TestAssert.True(Mathf.IsZeroApprox(edge.Rotation), $"{id}/{body.Name} readable edge is not rotated");
                    float top = edge.Position.Y + edge.Polygon.Min(p => p.Y);
                    TestAssert.True(Mathf.IsEqualApprox(-shape.Size.Y / 2f, top),
                        $"{id}/{body.Name} readable top edge matches physical support height");
                    float left = edge.Position.X + edge.Polygon.Min(p => p.X);
                    float right = edge.Position.X + edge.Polygon.Max(p => p.X);
                    float bottom = edge.Position.Y + edge.Polygon.Max(p => p.Y);
                    TestAssert.True(Mathf.IsEqualApprox(left, -shape.Size.X / 2f) &&
                                    Mathf.IsEqualApprox(right, shape.Size.X / 2f) && bottom <= shape.Size.Y / 2f,
                        $"{id}/{body.Name} readable edge cannot advertise support beyond either physical end");
                    if (!regionalEdges.ContainsKey(id)) regionalEdges.Add(id, edge.Color);
                    platforms++;
                }
                foreach (GrappleSurface2D surface in sector.GetNode("Gameplay/GrappleSurfaces")
                             .GetChildren().OfType<GrappleSurface2D>())
                {
                    Node2D skin = readable.GetNode<Node2D>(surface.Name.ToString());
                    AssertMaterial(skin, surface.Material, materialFamilies);
                    var shape = (CircleShape2D)surface.GetNode<CollisionShape2D>("TargetCollision/Collision").Shape;
                    TestAssert.Equal(surface.Position, skin.Position, $"{id}/{surface.Name} target center stays aligned");
                    TestAssert.True(Mathf.IsEqualApprox(shape.Radius, skin.Get("Radius").AsSingle()),
                        $"{id}/{surface.Name} target appearance is sized from its real hit area");
                    targets++;
                }
            }
            finally { sector.Free(); }
        }
        TestAssert.True(platforms > 0 && targets > 0, "material replacement checks exercise real terrain and targets");
        TestAssert.True(materialFamilies.Count >= 4,
            "grip stone, temple metal, ice and forbidden/dedicated surfaces keep distinct readable families");
        TestAssert.Equal(materialFamilies.Count, materialFamilies.Values.Distinct(StringComparer.Ordinal).Count(),
            "surface families cannot collapse valid, rejected and mechanism materials into one visual label");
        TestAssert.Equal(Sectors.Length, regionalEdges.Count,
            "each streamed region contributes a real terrain-edge witness");
        TestAssert.Equal(Sectors.Length, regionalEdges.Values.Distinct().Count(),
            "regional terrain edges vary while material families remain authoritative");
        GD.Print($"CANYON_MATERIAL_SKINS_OK source={(fromBuilder ? "builder" : "packed")} platforms={platforms} targets={targets}");
    }

    private static void AssertMaterial(Node2D skin, GrappleSurfaceMaterial expected,
        System.Collections.Generic.IDictionary<GrappleSurfaceMaterial, string> materialFamilies)
    {
        TestAssert.True(skin.GetPropertyList().Any(p => p["name"].AsString() == "SurfaceMaterial"),
            $"{skin.Name} replaceable skin exposes its gameplay surface material for material-specific drawing");
        TestAssert.Equal((int)expected, skin.Get("SurfaceMaterial").AsInt32(),
            $"{skin.Name} presentation cannot advertise a different grab material");
        TestAssert.Equal(Vector2.One, skin.Scale, $"{skin.Name} replacement must not stretch its geometry");
        TestAssert.True(Mathf.IsZeroApprox(skin.Rotation), $"{skin.Name} replacement must not rotate its geometry");
        string family = skin.GetMeta("material_family").AsString();
        TestAssert.True(!string.IsNullOrWhiteSpace(family),
            $"{skin.Name} exposes a stable readable material family for replacement art");
        if (materialFamilies.TryGetValue(expected, out string prior))
            TestAssert.Equal(prior, family,
                $"{skin.Name} uses the same readable family as other {expected} surfaces");
        else
            materialFamilies.Add(expected, family);
        AssertVisualOnly(skin);
    }

    private static void AssertVisualOnly(Node node)
    {
        TestAssert.True(node is not CollisionObject2D && node is not CollisionShape2D &&
                        node is not GrappleSurface2D, $"{node.Name} replaceable skin owns no gameplay physics");
        foreach (Node child in node.GetChildren()) AssertVisualOnly(child);
    }
}
