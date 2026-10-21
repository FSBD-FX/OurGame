using System;
using Godot;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

/// <summary>Cold pure contracts only; never starts a route or certifies realtime performance.</summary>
public partial class RunRealtimeProbeContractTests : SceneTree
{
    public override async void _Initialize()
    {
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            RealtimeRouteArgumentContractTests.Run();
            RealtimeFrameSamplerTests.Run();
            RealtimeLongFrameTraceTests.Run();
            Task11PointerAdmissionTraceTests.Run();
            Task24ShieldEndTimingTests.Run();
            Task24ShieldDeferredOutputTests.Run();
            GD.Print($"REALTIME_PROBE_CONTRACTS_OK argument_cases={RealtimeRouteArgumentContractTests.CaseCount} " +
                "route=false performanceCertification=false");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
