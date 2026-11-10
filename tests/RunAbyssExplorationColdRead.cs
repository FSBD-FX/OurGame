using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;
public partial class RunAbyssExplorationColdRead : SceneTree
{
    public override async void _Initialize()
    {
        Exception failure=null;
        try
        {
            TestAssert.True(OS.GetCmdlineUserArgs().Length==1,"all30 cold reader isolated root only");
            await FirstGateAbyssExplorationTests.RunCold(this,IsolatedPrologueFixture.ValidateStorageRoot());
        }
        catch(Exception error){failure=error;}
        try{TestSceneTeardown.DrainManagedWrappersAtProcessExit("abyss-exploration-cold");}
        catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
        if(failure!=null)GD.PushError(failure.ToString());Quit(failure==null?0:1);
    }
}
