using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunFormalInputBoundaryTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await FormalInputBoundaryTests.Run(this);
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
