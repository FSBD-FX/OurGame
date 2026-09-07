using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunTask4AudioVfxTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            PrologueAudioManifestTests.Run();
            await PrologueAudioDirectorTests.Run(this);
            await PrologueVfxRuntimeTests.Run(this);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("task4-audio-vfx");
            GD.Print("TASK4_AUDIO_VFX_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
