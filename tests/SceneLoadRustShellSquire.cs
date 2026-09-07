using Godot;

namespace Knight.Tests;

public partial class SceneLoadRustShellSquire : SceneTree
{
    public override void _Initialize()
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn");
        Node squire = scene.Instantiate();
        Root.AddChild(squire);
        GD.Print("RUST_SHELL_SQUIRE_SCENE_LOAD_OK");
        Quit(0);
    }
}
