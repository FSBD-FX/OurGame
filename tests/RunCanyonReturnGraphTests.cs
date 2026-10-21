using System;
using Godot;
using Knight.Tests.Unit;

namespace Knight.Tests;

// Pure route-graph validation only; no save fixture, GD.Load or physical-route claim.
public partial class RunCanyonReturnGraphTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            CanyonWorldLayoutTests.Run();
            GD.Print("CANYON_RETURN_GRAPH_OK physicalRoute=false publishedReturn=true");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
