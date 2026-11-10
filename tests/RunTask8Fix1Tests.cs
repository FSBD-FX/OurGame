using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask8Fix1Tests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await Task7CaseRegistryTests.Run(this);
            PrologueCanyonFixRound1Tests.RunStructural();
            PrologueCanyonFixRound3Tests.AssertRecoveryClassifierRejectsUnboundedAndAmbiguousEdges();
            await PrologueCanyonFixRound2Tests.Run(this);
            await PrologueCanyonTraversalTests.Run(this);
            GD.Print("TASK8_FIX1_FOCUSED_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
