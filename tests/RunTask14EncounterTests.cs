using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask14EncounterTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (System.Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--frozen-only"))
            {
                await ContinuousCanyonEncounterTests.RunTask14FrozenOnly(this);
                GD.Print("TASK14_FROZEN_REAL_CAPSULE_OK");
                Quit(0);
                return;
            }
            if (System.Array.Exists(OS.GetCmdlineUserArgs(), arg => arg == "--generated-only"))
            {
                await ContinuousCanyonEncounterTests.RunTask14GeneratedOnly(this);
                GD.Print("TASK14_GENERATED_SITES_OK");
                Quit(0);
                return;
            }
            CanyonEncounterCatalogTests.Run();
            await Task14TelegraphAdapterTests.Run(this);
            await ContinuousCanyonEncounterTests.RunTask14(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("task14-encounters");
            GD.Print("TASK14_ENCOUNTER_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
