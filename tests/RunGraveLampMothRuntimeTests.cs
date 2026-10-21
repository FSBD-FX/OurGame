using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunGraveLampMothRuntimeTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await GraveLampMothRuntimeTests.Run(this);
            GD.Print("GRAVE_LAMP_MOTH_RUNTIME_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
