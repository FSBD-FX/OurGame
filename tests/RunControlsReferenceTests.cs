using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunControlsReferenceTests : SceneTree
{
    public override async void _Initialize()
    {
        int exitCode = 1;
        try
        {
            await PrologueUiRuntimeTests.Run(this);
            PrologueGuidanceModelTests.Run();
            await PrologueGuidanceRuntimeTests.Run(this);
            await DescentClawGuidanceRuntimeTests.Run(this);
            GD.Print("CONTROLS_REFERENCE_TESTS_OK");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
        }
        finally
        {
            Paused = false;
            Quit(exitCode);
        }
    }
}
