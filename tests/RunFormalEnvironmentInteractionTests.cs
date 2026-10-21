using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunFormalEnvironmentInteractionTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await FormalEnvironmentInteractionTests.Run(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("formal-environment-interaction");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
