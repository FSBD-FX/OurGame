using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunCanyonSkitterTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] userArgs = OS.GetCmdlineUserArgs();
            bool coreOnly = System.Array.Exists(userArgs, value => value == "--core-only");
            bool safeAdjacent = System.Array.Exists(userArgs, value => value == "--safe-adjacent");
            if (!coreOnly && !safeAdjacent)
                throw new System.ArgumentException(
                    "Pass --core-only or --safe-adjacent; full legacy aggregate is storage-unsafe.");
            CanyonSkitterModelTests.Run();
            await CanyonSkitterRuntimeTests.Run(this);
            if (safeAdjacent)
            {
                BellBoltSentryModelTests.Run();
                await BellBoltSentryRuntimeTests.Run(this);
                AttackPresentationFrameTests.Run();
                CombatReadabilityExperienceTests.Run();
                await PrologueAudioDirectorTests.Run(this);
                await PrologueVfxRuntimeTests.Task10ReplaceableEffectRootsExist(this);
                await PrologueVfxRuntimeTests.Task10TransitionsAreEntryOnlyAndClean(this);
                EncounterDefinitionTests.Run();
                await EncounterControllerTests.Run(this);
            }
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("canyon-skitter");
            GD.Print("CANYON_SKITTER_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
