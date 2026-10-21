using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunHeadlampFogTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            ReducedFogSettingsTests.Run();
            PrologueFogModelTests.Run();
            await HeadlampBodyFaceRuntimeTests.Run(this);
            await HeadlampFogRuntimeTests.Run(this);
            CanyonPressureGateModelTests.Run();
            await CanyonPressureGateRuntimeTests.Run(this);
            GD.Print("HEADLAMP_FOG_ORIGINAL_PRESSURE_GATE_REGRESSION_OK");
            GD.Print("HEADLAMP_FOG_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
