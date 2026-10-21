using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunBellBoltSentryTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] userArgs = OS.GetCmdlineUserArgs();
            bool coreOnly = System.Array.Exists(userArgs, value => value == "--core-only");
            bool safeAdjacent = System.Array.Exists(userArgs, value => value == "--safe-adjacent");
            BellBoltSentryModelTests.Run();
            await BellBoltSentryRuntimeTests.Run(this);
            if (!coreOnly)
            {
                AttackPresentationFrameTests.Run();
                CombatReadabilityExperienceTests.Run();
                await PrologueAudioDirectorTests.Run(this);
                await PrologueVfxRuntimeTests.Task10ReplaceableEffectRootsExist(this);
                await PrologueVfxRuntimeTests.Task10TransitionsAreEntryOnlyAndClean(this);
                EncounterDefinitionTests.Run();
                await EncounterControllerTests.Run(this);
                if (!safeAdjacent)
                {
                    PrologueCanyonStructureTests.Run(GD.Load<PackedScene>(
                        "res://scenes/prologue/PrologueLevel.tscn"));
                }
            }
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("bell-bolt-sentry");
            GD.Print("BELL_BOLT_SENTRY_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
