using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunTask8GeneratedMainProof : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            Root.Size = new Vector2I(1280, 720);
            await ToSignal(this, SignalName.ProcessFrame);
            bool capture = DisplayServer.GetName() != "headless";
            bool fullRoute = await PrologueCanyonGeneratedMainRouteTests.Run(this, capture);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("task8-generated-main-proof");
            GD.Print(fullRoute
                ? $"TASK8_GENERATED_MAIN_PROOF_OK resolution={Root.Size.X}x{Root.Size.Y} " +
                  $"fixed60=True display={DisplayServer.GetName()} capture={capture}"
                : $"TASK8_GENERATED_MAIN_HEADLESS_OK resolution={Root.Size.X}x{Root.Size.Y} " +
                  $"fixed60=True display={DisplayServer.GetName()} pointer_route=leaf_physics");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
