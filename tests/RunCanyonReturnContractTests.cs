using System;
using Godot;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunCanyonReturnContractTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            CanyonWorldLayoutTests.Run();
            CanyonReturnTraversalContractTests.Run();
            PrologueCanyonStructureTests.RunTask4();
            GD.Print("CANYON_RETURN_CONTRACT_OK physicalRoute=false publishedReturn=true directedSegments=18");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
