using System;
using Godot;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunHeadlampQuadTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string storage = IsolatedPrologueFixture.ValidateStorageRoot();
            HeadlampQuadEquivalenceTests.Run();
            GD.Print("HEADLAMP_QUAD_EQUIVALENCE_OK");
            HeadlampQuadCostProbe.Run(storage);
            GD.Print("HEADLAMP_QUAD_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
