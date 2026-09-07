using System;
using Godot;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunCanyonMaterialSkinTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            CanyonMaterialSkinTests.Run(Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--builder"));
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
