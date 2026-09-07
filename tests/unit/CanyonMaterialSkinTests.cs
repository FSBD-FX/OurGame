using System;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Build.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class CanyonMaterialSkinTests
{
    private static readonly PrologueSectorId[] Sectors =
    [
        PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent,
        PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple
    ];

    public static void Run(bool fromBuilder)
    {
        int platforms = 0;
        int targets = 0;
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
                    AssertMaterial(skin, profile.Material);
                    TestAssert.Equal(body.Position, skin.Position, $"{id}/{body.Name} skin follows collider center");
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
                    platforms++;
                }
                foreach (GrappleSurface2D surface in sector.GetNode("Gameplay/GrappleSurfaces")
                             .GetChildren().OfType<GrappleSurface2D>())
                {
                    Node2D skin = readable.GetNode<Node2D>(surface.Name.ToString());
                    AssertMaterial(skin, surface.Material);
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
        GD.Print($"CANYON_MATERIAL_SKINS_OK source={(fromBuilder ? "builder" : "packed")} platforms={platforms} targets={targets}");
    }

    private static void AssertMaterial(Node2D skin, GrappleSurfaceMaterial expected)
    {
        TestAssert.True(skin.GetPropertyList().Any(p => p["name"].AsString() == "SurfaceMaterial"),
            $"{skin.Name} replaceable skin exposes its gameplay surface material for material-specific drawing");
        TestAssert.Equal((int)expected, skin.Get("SurfaceMaterial").AsInt32(),
            $"{skin.Name} presentation cannot advertise a different grab material");
        TestAssert.Equal(Vector2.One, skin.Scale, $"{skin.Name} replacement must not stretch its geometry");
        TestAssert.True(Mathf.IsZeroApprox(skin.Rotation), $"{skin.Name} replacement must not rotate its geometry");
        AssertVisualOnly(skin);
    }

    private static void AssertVisualOnly(Node node)
    {
        TestAssert.True(node is not CollisionObject2D && node is not CollisionShape2D &&
                        node is not GrappleSurface2D, $"{node.Name} replaceable skin owns no gameplay physics");
        foreach (Node child in node.GetChildren()) AssertVisualOnly(child);
    }
}
