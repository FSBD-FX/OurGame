using System;
using System.Linq;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Presentation;

public partial class RunQuestFragmentNaturalRoute : SceneTree
{
    public override async void _Initialize()
    {
        int exit = 1;
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 3 || args.Count(v => v == "--quest-route=natural-five-to-six") != 1 ||
                args.Count(v => v == "--keyboard-route") != 1 ||
                args.Count(v => v.StartsWith("--case-storage-root=", StringComparison.Ordinal)) != 1)
                throw new ArgumentException("natural fragment route accepts only its exact scenario, keyboard and isolated storage");
            string path = await PrologueCanyonGeneratedMainRouteTests.RunNaturalFiveToSix(this);
            GD.Print($"NATURAL_FRAGMENT_ROUTE_RESULT_WRITTEN path={path} scenario=natural-five-to-six");
            GD.Print("NATURAL_FRAGMENT_ROUTE_OK scenario=natural-five-to-six synthetic_fixture=False human_validated=False full_goal_accepted=False");
            exit = 0;
        }
        catch (Exception error)
        {
            KeyboardRouteInputDriver.Current?.ReleaseAll();
            GD.PushError(error.ToString());
        }
        finally
        {
            GD.Print($"NATURAL_FRAGMENT_ROUTE_IDENTITY_RELEASED pid={System.Environment.ProcessId}");
            Quit(exit);
        }
    }
}
