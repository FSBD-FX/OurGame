using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunFallDamageTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            PlayerFallDamageModelTests.Run();
            await PlayerFallDamageRuntimeTests.Run(this);
            GD.Print("FALL_DAMAGE_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
