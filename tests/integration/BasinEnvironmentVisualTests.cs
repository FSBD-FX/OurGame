using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Presentation;
using Knight.Prologue.Sample;

namespace Knight.Tests.Integration;

public static class BasinEnvironmentVisualTests
{
    public static void Run()
    {
        Node2D sample = GD.Load<PackedScene>(BasinQualitySampleEntry.SectorScenePath).Instantiate<Node2D>();
        Node2D original = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<Node2D>();
        try
        {
            var distant = sample.GetNodeOrNull<BasinEnvironmentVisual>("VisualRoot/Background/AtmosphericRift");
            var structure = sample.GetNodeOrNull<BasinEnvironmentVisual>("VisualRoot/Midground/BuriedSanctum");
            TestAssert.True(distant != null && structure != null,
                "packed sample must retain independent distant-rift and architectural backdrop replacement slots");
            TestAssert.True(distant.DistantLayer && !structure.DistantLayer,
                "packed environment slots retain their distinct draw roles");
            TestAssert.Equal(new Rect2(0f, 0f, 1600f, 720f), distant.SectorBounds, "distant bounds retained");
            TestAssert.Equal(distant.SectorBounds, structure.SectorBounds, "architecture bounds retained");
            TestAssert.True(distant.Visible && structure.Visible &&
                distant.GetParent<Node2D>().ZIndex < structure.GetParent<Node2D>().ZIndex &&
                structure.GetParent<Node2D>().ZIndex < -2,
                "both sample scenery layers draw behind actual terrain and actors, never over combat");
            TestAssert.True(original.GetNodeOrNull("VisualRoot/Background/AtmosphericRift") == null &&
                original.GetNodeOrNull("VisualRoot/Midground/BuriedSanctum") == null,
                "sample art does not replace the ordinary full-level environment");

            Node2D collision = sample.GetNode<Node2D>("Gameplay/Collision");
            Node2D originalCollision = original.GetNode<Node2D>("Gameplay/Collision");
            TestAssert.Equal(originalCollision.GetChildCount(), collision.GetChildCount(),
                "art refinement never adds or removes solid surfaces");
            foreach (StaticBody2D body in collision.GetChildren().OfType<StaticBody2D>())
            {
                var oldBody = originalCollision.GetNode<StaticBody2D>(body.Name.ToString());
                TestAssert.Equal(oldBody.Transform, body.Transform, "solid transform retained: " + body.Name);
                TestAssert.Equal(oldBody.CollisionLayer, body.CollisionLayer, "solid layer retained: " + body.Name);
                TestAssert.Equal(oldBody.CollisionMask, body.CollisionMask, "solid mask retained: " + body.Name);
                var shape = body.GetChildren().OfType<CollisionShape2D>().First();
                var oldShape = oldBody.GetChildren().OfType<CollisionShape2D>().First();
                TestAssert.Equal(oldShape.Transform, shape.Transform, "collider placement retained: " + body.Name);
                TestAssert.Equal(oldShape.OneWayCollision, shape.OneWayCollision, "one-way retained: " + body.Name);
                TestAssert.Equal(oldShape.OneWayCollisionMargin, shape.OneWayCollisionMargin, "one-way margin retained");
                Vector2 size = ((RectangleShape2D)shape.Shape).Size;
                TestAssert.Equal(((RectangleShape2D)oldShape.Shape).Size, size, "solid extent retained: " + body.Name);
                var skin = sample.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/" + body.Name);
                var oldSkin = original.GetNode<CanyonPlatformVisual>("VisualRoot/GameplayReadable/" + body.Name);
                TestAssert.Equal(oldSkin.GetIndex(), skin.GetIndex(), "original sibling draw order retained: " + body.Name);
                if (body.Name == "EntryCatch" || body.Name == "SalvageFloor" || body.Name == "BasinFloor")
                {
                    TestAssert.True(skin is BasinPlatformVisual, "sample masonry renderer survives packing");
                    TestAssert.Equal(body.Name == "BasinFloor" ? 800f : 0f,
                        ((BasinPlatformVisual)skin).DetailStartX, "sample floor detail remains right-half only");
                }
                TestAssert.Equal(size, skin.SkinSize, "skin remains exactly collision-sized: " + body.Name);
                TestAssert.Equal(body.Position, skin.Position, "skin remains collision-aligned: " + body.Name);
                TestAssert.Equal(oldSkin.SurfaceMaterial, skin.SurfaceMaterial, "art does not rename material semantics");
                TestAssert.Equal(Vector2.One, skin.Scale, "skin does not stretch its authored bounds");
                TestAssert.Equal(0f, skin.Rotation, "skin does not rotate away from collision");
                TestAssert.Equal(oldSkin.CatchShelf, skin.CatchShelf, "catch-shelf identity retained");
                TestAssert.True(oldSkin.GetNode<Polygon2D>("ReadableEdge").Polygon.SequenceEqual(
                    skin.GetNode<Polygon2D>("ReadableEdge").Polygon), "support-edge polygon retained exactly");
                TestAssert.Equal(oldSkin.GetNode<Node2D>("ReadableEdge").Transform,
                    skin.GetNode<Node2D>("ReadableEdge").Transform,
                    "replacement retains the exact support-height cue: " + body.Name);
                var surface = body.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
                var oldSurface = oldBody.GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
                TestAssert.Equal(oldSurface == null, surface == null, "grapple target membership retained");
                if (surface != null)
                {
                    TestAssert.Equal(oldSurface.Material, surface.Material, "grapple material retained");
                    TestAssert.Equal(oldSurface.AssistRadius, surface.AssistRadius, "grapple assist retained");
                    TestAssert.Equal(oldSurface.AnchorOffset, surface.AnchorOffset, "grapple offset retained");
                    TestAssert.Equal(oldSurface.UseCollisionHitPoint, surface.UseCollisionHitPoint, "hit-point targeting retained");
                }
            }
            TestAssert.True(!Descendants(sample.GetNode("VisualRoot")).Any(node => node is CollisionObject2D),
                "decorative architecture, suspension and skins cannot become false collision or grapple targets");
        }
        finally { sample.Free(); original.Free(); }
    }

    private static System.Collections.Generic.IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }
}
