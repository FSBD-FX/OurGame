using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunResourceRewardColdRead : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            ResourceRewardRuntimeTests.RunCold(IsolatedPrologueFixture.ValidateStorageRoot());
            Quit(0);
        }
        catch(Exception e){GD.PushError(e.ToString());Quit(1);}
    }
}
