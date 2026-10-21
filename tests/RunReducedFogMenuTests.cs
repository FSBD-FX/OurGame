using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunReducedFogMenuTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            TestAssert.True(DisplayServer.GetName() != "headless", "menu layout requires a normal display");
            await ToSignal(this, SignalName.ProcessFrame);
            await ReducedFogMenuRuntimeTests.Run(this);
            GD.Print("REDUCED_FOG_MENU_OK");
            Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); Quit(1); }
    }
}
