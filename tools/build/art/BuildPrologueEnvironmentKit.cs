using Godot;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Tools;

namespace Knight.Build.Art;

public partial class BuildPrologueEnvironmentKit : SceneTree
{
    private const string ScenePath = "res://scenes/presentation/PrologueEnvironmentKit.tscn";
    private const string AtlasPath = "res://assets/environment/nameless-belfry/environment-atlas.png";

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://scenes/presentation"));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(
            "res://assets/environment/nameless-belfry"));
        if (BuildAtlas().SavePng(ProjectSettings.GlobalizePath(AtlasPath)) != Error.Ok)
        {
            GD.PushError("BuildPrologueEnvironmentKit: failed to save environment atlas.");
            Quit(1);
            return;
        }

        RoomVisualDescriptor descriptor = RoomVisualCatalog.For(PrologueRoomId.AwakeningCourtyard);
        if (string.IsNullOrWhiteSpace(descriptor.Landmark))
        {
            GD.PushError("BuildPrologueEnvironmentKit: catalog landmark validation failed.");
            Quit(1);
            return;
        }

        var root = new RoomFormalVisual { Name = "PrologueEnvironmentKit" };
        var visualRoot = new Node2D { Name = "VisualRoot" };
        foreach (string stablePath in RoomVisualCatalog.StableSectorVisualRoots)
            visualRoot.AddChild(new Node2D { Name = stablePath["VisualRoot/".Length..] });
        root.AddChild(visualRoot);
        foreach (string name in new[] { "Far", "Back", "Mid", "Platforms", "Props", "Front", "Atmosphere" })
            root.AddChild(new Node2D { Name = name });
        ForegroundOccluder occluder = new() { Name = "ForegroundOccluder" };
        occluder.AddChild(Rect("LeftFrame", new Vector2(-560, 0), new Vector2(180, 720), new Color("18252a")));
        occluder.AddChild(Rect("RightFrame", new Vector2(560, 0), new Vector2(180, 720), new Color("18252a")));
        root.GetNode<Node2D>("Front").AddChild(occluder);

        Error error = SceneBuildUtil.PackAndSave(root, ScenePath);
        root.Free();
        if (error != Error.Ok)
        {
            GD.PushError($"BuildPrologueEnvironmentKit failed: {error}");
            Quit(1);
            return;
        }
        RoomFormalVisual copy = GD.Load<PackedScene>(ScenePath).Instantiate<RoomFormalVisual>();
        bool valid = copy.GetNodeOrNull<ForegroundOccluder>("Front/ForegroundOccluder") != null &&
            copy.GetNodeOrNull("Platforms") != null;
        foreach (string stablePath in RoomVisualCatalog.StableSectorVisualRoots)
            valid &= copy.GetNodeOrNull(stablePath) != null;
        copy.Free();
        if (!valid)
        {
            GD.PushError("BuildPrologueEnvironmentKit contract validation failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildPrologueEnvironmentKit: seven-layer environment kit packed and validated.");
        Quit(0);
    }

    private static Image BuildAtlas()
    {
        Image image = Image.CreateEmpty(512, 512, false, Image.Format.Rgba8);
        image.Fill(new Color("0b1117"));
        Color[] palette =
        [
            new("263a3e"), new("34554f"), new("72593c"), new("ae813f"),
            new("57415d"), new("69cfc6"), new("d7bb6c"), new("11181c")
        ];
        for (int index = 0; index < palette.Length; index++)
        {
            int x = (index % 4) * 128;
            int y = (index / 4) * 256;
            image.FillRect(new Rect2I(x, y, 128, 256), palette[index]);
            for (int line = 0; line < 8; line++)
                image.FillRect(new Rect2I(x + line * 16, y, 3, 256), palette[(index + 1) % palette.Length]);
        }
        return image;
    }

    private static Polygon2D Rect(string name, Vector2 center, Vector2 size, Color color) => new()
    {
        Name = name, Position = center, Color = color,
        Polygon = [-size / 2, new Vector2(size.X / 2, -size.Y / 2), size / 2,
            new Vector2(-size.X / 2, size.Y / 2)]
    };
}
