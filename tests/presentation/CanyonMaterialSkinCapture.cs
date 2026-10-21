using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Knight.Abilities;
using Knight.Presentation;

namespace Knight.Tests.Presentation;

/// <summary>Isolated material legend for art review, not a gameplay or playtest acceptance scene.</summary>
public partial class CanyonMaterialSkinCapture : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string output = Array.Find(OS.GetCmdlineUserArgs(), a => a.StartsWith("--output="))?
                .Substring("--output=".Length) ?? throw new ArgumentException("--output=absolute.png is required");
            Root.Size = new Vector2I(1280, 720);
            Root.ContentScaleSize = new Vector2I(1280, 720);
            Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            var canvas = new Node2D();
            Root.AddChild(canvas);
            canvas.AddChild(new ColorRect { Size = new Vector2(1280, 720), Color = new Color("17252b") });
            AddText(canvas, "MATERIAL SKINS / targets normalized to 24px radius", new Vector2(44, 32), 26);
            GrappleSurfaceMaterial[] materials =
            [GrappleSurfaceMaterial.GripStone, GrappleSurfaceMaterial.TempleMetal,
                GrappleSurfaceMaterial.DedicatedAnchor, GrappleSurfaceMaterial.Ice, GrappleSurfaceMaterial.Forbidden];
            string[] labels = ["GRIP STONE", "TEMPLE METAL", "ANCHOR", "ICE / NO GRIP", "SEALED / NO GRIP"];
            for (int i = 0; i < materials.Length; i++)
            {
                float x = 138f + i * 250f;
                AddText(canvas, labels[i], new Vector2(x - 106f, 120f), 17);
                canvas.AddChild(new CanyonAnchorVisual
                { Position = new Vector2(x, 212f), SurfaceMaterial = materials[i], Radius = 24f });
                if (materials[i] == GrappleSurfaceMaterial.DedicatedAnchor) continue;
                var skin = new CanyonPlatformVisual
                {
                    Position = new Vector2(x, 410f), SkinSize = new Vector2(212f, 148f),
                    SurfaceMaterial = materials[i]
                };
                skin.AddChild(new Polygon2D
                {
                    Polygon = [new(-106f, -74f), new(106f, -74f), new(106f, -68f), new(-106f, -68f)],
                    Color = materials[i] == GrappleSurfaceMaterial.Ice ? new Color("a8d8e8") : new Color("a9c79d")
                });
                canvas.AddChild(skin);
            }
            AddText(canvas, "Shape + material. No collision, glow, particle loop, or generated textures in the visual layer.",
                new Vector2(44f, 620f), 18);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image image = Root.GetTexture().GetImage();
            AssertMaterialPixels(image);
            Error result = image.SavePng(output);
            if (result != Error.Ok) throw new InvalidOperationException($"image save failed: {result}");
            GD.Print($"CANYON_MATERIAL_CAPTURE_OK output={output} width={image.GetWidth()} height={image.GetHeight()}");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }

    private static void AddText(Node parent, string text, Vector2 position, int size)
    {
        var label = new Label { Text = text, Position = position };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", new Color("aebdb4"));
        parent.AddChild(label);
    }

    private static void AssertMaterialPixels(Image image)
    {
        Color background = image.GetPixel(20, 300);
        var silhouettes = new HashSet<string>();
        for (int i = 0; i < 5; i++)
        {
            int centerX = 138 + i * 250;
            int occupied = 0;
            var silhouette = new StringBuilder();
            for (int y = -29; y <= 29; y++)
            for (int x = -29; x <= 29; x++)
            {
                bool ink = image.GetPixel(centerX + x, 212 + y) != background;
                silhouette.Append(ink ? '1' : '0');
                if (!ink) continue;
                occupied++;
                TestAssert.True(x * x + y * y <= 26 * 26,
                    $"material {i} point drawing respects its 24px radius with 2px raster tolerance");
            }
            TestAssert.True(occupied > 100, $"material {i} target is actually rendered");
            TestAssert.True(silhouettes.Add(silhouette.ToString()),
                $"material {i} has a distinct silhouette, not only a different hue");
            if (i == 2) continue;
            var interiorColors = new HashSet<Color>();
            for (int y = 328; y < 492; y++)
            for (int x = centerX - 114; x < centerX + 114; x++)
            {
                Color pixel = image.GetPixel(x, y);
                bool inside = x >= centerX - 106 && x < centerX + 106 && y >= 336 && y < 484;
                if (inside) interiorColors.Add(pixel);
                else TestAssert.Equal(background, pixel, $"material {i} never paints false terrain outside its bounds");
            }
            TestAssert.True(interiorColors.Count >= 4, $"material {i} platform renders layered material detail");
        }
        GD.Print("CANYON_MATERIAL_PIXELS_OK distinct_shapes=5 bounded_platforms=4 renderer=live_viewport");
    }
}
