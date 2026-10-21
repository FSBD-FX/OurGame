using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunGrappleReleaseTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--boost-spool-only"))
            {
                PlayerPresentationTests.MechanicalBoostKeepsSpoolTension();
                GD.Print("BOOST_SPOOL_TESTS_OK");
                Quit(0);
                return;
            }
            await MechanicalClawRuntimeTests.ScheduledBoostKeepsTheRenderedAttachedAction(this);
            PlayerPresentationTests.MechanicalBoostKeepsSpoolTension();
            if (Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--boost-presentation-only"))
            {
                GD.Print("BOOST_PRESENTATION_TESTS_OK");
                Quit(0);
                return;
            }
            await MechanicalClawRuntimeTests.AttachedOcclusionReleasesBeforePullingThroughTheBlocker(this);
            await MechanicalClawRuntimeTests.AttachedVisibilityBoundariesUseOnlyTheSafeConnectionCue(this);
            await MechanicalClawRuntimeTests.CancelledTravelPreservesOrdinaryShortHop(this);
            await MechanicalClawRuntimeTests.RealClawTakeoverEndsBurnWithoutRefund(this);
            PlayerMotionModelTests.Run();
            await MechanicalClawRuntimeTests.AutomaticBoostReleasePreservesUpwardInertia(this);
            GD.Print("GRAPPLE_RELEASE_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
