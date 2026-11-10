using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;
public partial class RunAbyssColdRead : SceneTree
{
    public override async void _Initialize()
    {
        Exception failure=null;
        try
        {
            TestAssert.True(OS.GetCmdlineUserArgs().Length==1,"cold reader isolated root only");
            await FirstGateAbyssRuntimeTests.RunCold(this,IsolatedPrologueFixture.ValidateStorageRoot());
        }
        catch(Exception error){failure=error;}
        try{TestSceneTeardown.DrainManagedWrappersAtProcessExit("abyss-cold");}
        catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
        if(failure!=null)GD.PushError(failure.ToString());Quit(failure==null?0:1);
    }
}
