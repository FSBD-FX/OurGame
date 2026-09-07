using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBossDoorVisibilityTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await BossDoorVisibilityTests.Run(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("boss-door-visibility");
            GD.Print("BOSS_DOOR_VISIBILITY_TESTS_OK initial=True active=True defeated=True retry=True");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
