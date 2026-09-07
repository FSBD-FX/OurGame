using Godot;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Art;

public partial class BuildPrologueMechanismVisuals : SceneTree
{
    private const string SceneRoot = "res://scenes/presentation";
    private const string AssetPath = "res://assets/environment/nameless-belfry/mechanisms.png";

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SceneRoot));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(
            "res://assets/environment/nameless-belfry"));
        if (BuildAtlas().SavePng(ProjectSettings.GlobalizePath(AssetPath)) != Error.Ok)
        {
            GD.PushError("BuildPrologueMechanismVisuals: failed to save mechanism atlas.");
            Quit(1);
            return;
        }

        bool valid = Save("GraveLampVisual", MechanismVisualKind.GraveLamp, new Vector2(0, -5)) &&
            Save("ThreadAnchorVisual", MechanismVisualKind.ThreadAnchor, Vector2.Zero) &&
            Save("BellChainVisual", MechanismVisualKind.BellChain, new Vector2(0, -80)) &&
            Save("ShortcutDoorVisual", MechanismVisualKind.ShortcutDoor, Vector2.Zero);
        if (!valid)
        {
            GD.PushError("BuildPrologueMechanismVisuals contract validation failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildPrologueMechanismVisuals: four formal mechanism leaves packed and validated.");
        Quit(0);
    }

    private static bool Save(string name, MechanismVisualKind kind, Vector2 markerPosition)
    {
        var root = new PrologueMechanismVisual { Name = name, Kind = kind };
        root.AddChild(new Marker2D { Name = "TargetMarker", Position = markerPosition });
        string path = $"{SceneRoot}/{name}.tscn";
        Error error = SceneBuildUtil.PackAndSave(root, path);
        root.Free();
        if (error != Error.Ok) return false;
        PrologueMechanismVisual copy = GD.Load<PackedScene>(path).Instantiate<PrologueMechanismVisual>();
        bool valid = copy.Kind == kind && copy.GetNodeOrNull<Marker2D>("TargetMarker") != null;
        copy.Free();
        return valid;
    }

    private static Image BuildAtlas()
    {
        Image image = Image.CreateEmpty(512, 128, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        Color ink = new("11171a"), gold = new("d4aa58"), cyan = new("69d7cc"), metal = new("566166");
        Color[] fills = [gold, cyan, metal, gold];
        for (int index = 0; index < 4; index++)
        {
            int x = index * 128;
            image.FillRect(new Rect2I(x + 18, 18, 92, 92), ink);
            image.FillRect(new Rect2I(x + 26, 26, 76, 76), fills[index]);
            image.FillRect(new Rect2I(x + 58, 12, 12, 104), cyan);
        }
        return image;
    }
}
