using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunFireflyGuidanceTests : SceneTree
{
    public override async void _Initialize()
    {
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            CanyonFireflyTraversalTests.Run(this);
            CanyonFireflyGuidanceModelTests.Run();
            await CanyonFireflyGuidanceRuntimeTests.Run(this);
            GD.Print("FIREFLY_GUIDANCE_FOCUSED_OK fullRoute=false performanceCertification=false");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
