using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunEnvironmentTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            PrologueEnvironmentStructureTests.Run();
            await PrologueEnvironmentRuntimeTests.Run(this);
            GD.Print("ENVIRONMENT_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
