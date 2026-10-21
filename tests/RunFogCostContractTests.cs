using System;
using Godot;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunFogCostContractTests : SceneTree
{
    public override async void _Initialize()
    {
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            FogCostSampleBufferTests.Run(); FogAllocationWindowTests.Run();
            AllocationTypeBucketsTests.Run(); Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
