using System;
using Godot;
using Knight.Tests.Presentation;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunHeadlampPatchTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string storage = IsolatedPrologueFixture.ValidateStorageRoot();
            HeadlampPatchEquivalenceTests.Run();
            GD.Print("HEADLAMP_PATCH_EQUIVALENCE_OK");
            HeadlampPatchCostProbe.Run(storage);
            GD.Print("HEADLAMP_PATCH_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
