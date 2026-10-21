using Godot;

namespace Knight.Tests;

public partial class SceneLoadPlayer : SceneTree
{
    public override void _Initialize()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://scenes/player/Player.tscn");
        Node player = scene.Instantiate();
        Root.AddChild(player);
        GD.Print("PLAYER_SCENE_LOAD_OK");
        Quit(0);
    }
}
