using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunTask8Fix2SeamClassifierRed : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            PrologueCanyonFixRound3Tests.AssertRecoveryClassifierRejectsUnboundedAndAmbiguousEdges();
            GD.Print("TASK8_FIX3_SEAM_CLASSIFIER_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
