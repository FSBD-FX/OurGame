using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunMinimapGridColdRead : SceneTree
{
    public override void _Initialize()
    {
        Exception failure=null;
        try
        {
            TestAssert.True(OS.GetCmdlineUserArgs().Length==1,"grid cold reader accepts isolated root only");
            MinimapVersionedGridTests.RunCold(IsolatedPrologueFixture.ValidateStorageRoot());
        }
        catch(Exception error){failure=error;}
        try{TestSceneTeardown.DrainManagedWrappersAtProcessExit("minimap-grid-cold");}
        catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
        if(failure!=null)GD.PushError(failure.ToString());Quit(failure==null?0:1);
    }
}
