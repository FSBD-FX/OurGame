using Godot;
using Knight.Tools;
using Knight.Vfx;

namespace Knight.Build.Art;

public partial class BuildPrologueVfx : SceneTree
{
    private const string ScenePath = "res://scenes/vfx/PrologueVfx.tscn";
    private const string AtlasPath = "res://assets/vfx/prologue-vfx.png";

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://scenes/vfx"));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://assets/vfx"));
        Error atlasError = BuildAtlas();
        if (atlasError != Error.Ok)
        {
            GD.PushError($"BuildPrologueVfx: failed to save atlas: {atlasError}");
            Quit(1);
            return;
        }

        var root = new PrologueVfxDirector { Name = "PrologueVfx" };
        var world = new Node2D { Name = "World" };
        foreach (string effectRoot in PrologueVfxDirector.Task10EffectRootNames)
            world.AddChild(new Node2D { Name = effectRoot });
        root.AddChild(world);
        root.AddChild(new CameraFeedbackController { Name = "CameraFeedback" });
        var screen = new CanvasLayer { Name = "Screen", Layer = 20 };
        screen.AddChild(new ColorRect
        {
            Name = "Flash",
            Position = Vector2.Zero,
            Size = new Vector2(1280, 720),
            Color = new Color(0.88f, 0.95f, 0.92f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        });
        root.AddChild(screen);
        Error save = SceneBuildUtil.PackAndSave(root, ScenePath);
        root.Free();
        if (save != Error.Ok)
        {
            GD.PushError($"BuildPrologueVfx: failed to save scene: {save}");
            Quit(1);
            return;
        }

        PrologueVfxDirector copy = GD.Load<PackedScene>(ScenePath).Instantiate<PrologueVfxDirector>();
        bool valid = copy.GetNodeOrNull<Node2D>("World") != null &&
            copy.GetNodeOrNull<CameraFeedbackController>("CameraFeedback") != null &&
            copy.GetNodeOrNull<ColorRect>("Screen/Flash") != null && FileAccess.FileExists(AtlasPath);
        foreach (string effectRoot in PrologueVfxDirector.Task10EffectRootNames)
            valid &= copy.GetNodeOrNull<Node2D>($"World/{effectRoot}") != null;
        copy.Free();
        if (!valid)
        {
            GD.PushError("BuildPrologueVfx contract validation failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildPrologueVfx: typed world bursts, thread line and bounded screen feedback packed and validated.");
        Quit(0);
    }

    private static Error BuildAtlas()
    {
        Image image = Image.CreateEmpty(256, 64, false, Image.Format.Rgba8);
        image.Fill(new Color(0, 0, 0, 0));
        Color[] colors = [new("d7b85b"), new("8de4d8"), new("f2e6c9"), new("b75a4b")];
        for (int cell = 0; cell < colors.Length; cell++)
        {
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float distance = new Vector2(x, y).DistanceTo(new Vector2(32, 32));
                if (distance is < 28f and > 20f)
                {
                    Color color = colors[cell];
                    color.A = 1f - Mathf.Abs(distance - 24f) / 4f;
                    image.SetPixel(cell * 64 + x, y, color);
                }
            }
        }
        return image.SavePng(ProjectSettings.GlobalizePath(AtlasPath));
    }
}
