using System;
using System.IO;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;
public partial class RunForecourtColdRead : SceneTree
{
    public override async void _Initialize()
    {
        Exception failure=null;
        try
        {
            TestAssert.True(OS.GetCmdlineUserArgs().Length==1,"cold reader isolated root only");
            string storage=IsolatedPrologueFixture.ValidateStorageRoot();
            if(File.Exists(Path.Combine(storage,"forecourt-mid-route-cold-manifest.json")))
                await FirstGateForecourtRouteTests.RunMidCold(this,storage);
            else await FirstGateForecourtTerrainTests.RunCold(this,storage);
        }
        catch(Exception error){failure=error;}
        try{TestSceneTeardown.DrainManagedWrappersAtProcessExit("forecourt-cold");}
        catch(Exception error){failure=failure==null?error:new AggregateException(failure,error);}
        if(failure!=null)GD.PushError(failure.ToString());Quit(failure==null?0:1);
    }
}
