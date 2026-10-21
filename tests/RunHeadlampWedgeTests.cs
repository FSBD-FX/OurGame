using System;
using Godot;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunHeadlampWedgeTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string storage = IsolatedPrologueFixture.ValidateStorageRoot();
            HeadlampWedgeEquivalenceTests.Run();
            GD.Print("HEADLAMP_WEDGE_EQUIVALENCE_OK");
            HeadlampWedgeCostProbe.Run(storage);
            GD.Print("HEADLAMP_WEDGE_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
