using Godot;
using Knight.Tests.Unit;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunPrototypeTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            foreach (string argument in OS.GetCmdlineArgs())
            {
                if (argument != "--mechanical-claw-only") continue;
                MechanicalClawModelTests.Run();
                GD.Print("MECHANICAL_CLAW_TESTS_OK");
                Quit(0);
                return;
            }

            ProjectFoundationTests.Run();
            TuningContractTests.Run();
            PlayerActionSelectionModelTests.Run();
            PlayerEnergyModelTests.Run();
            PlayerShieldModelTests.Run();
            PlayerMotionModelTests.Run();
            HealthComponentTests.Run();
            PlayerCombatModelTests.Run();
            PlayerCombatSceneTests.Run();
            PlayerArtManifestTests.Run();
            ThreadAbilityModelTests.Run();
            ThreadCastModelTests.Run();
            MechanicalClawModelTests.Run();
            GrappleSurfaceTests.Run();
            ThreadTarget2DTests.Run();
            RustShellModelTests.Run();
            PrototypeCheckpointTests.Run();
            PrototypeRoomStructureTests.Run();
            await PrototypeRoomRuntimeTests.Run(this);
            GD.Print("PROTOTYPE_TESTS_OK");
            Quit(0);
        }
        catch (System.Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
