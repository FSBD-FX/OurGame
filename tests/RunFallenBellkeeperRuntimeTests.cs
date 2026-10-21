using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunFallenBellkeeperRuntimeTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            for (int pass = 0; pass < 2; pass++)
                await FallenBellkeeperRuntimeTests.Run(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("boss-runtime");
            GD.Print("FALLEN_BELLKEEPER_RUNTIME_TESTS_OK passes=2");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
