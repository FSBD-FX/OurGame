using System;
using System.Linq;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Presentation;

public partial class RunQuestFragmentReturnProbe : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            const string storage = "--case-storage-root=";
            string[] stages = { "temple-reverse", "basin-abyss", "temple-basin-abyss", "temple-six" };
            string stage = stages.SingleOrDefault(candidate => args.Contains("--quest-return-probe=" + candidate));
            if (args.Length != 3 || stage == null || args.Count(value => value == "--quest-return-probe=" + stage) != 1 ||
                args.Count(value => value == "--keyboard-route") != 1 ||
                args.Count(value => value.StartsWith(storage, StringComparison.Ordinal)) != 1)
                throw new ArgumentException("fragment return accepts only one known stage, keyboard-route and one isolated absolute storage root");
            await PrologueCanyonGeneratedMainRouteTests.RunFragmentReturnProbe(this, stage);
            GD.Print($"QUEST_FRAGMENT_RETURN_PROBE_OK stage={stage} fixture=true fullRoute=false");
            GD.Print($"QUEST_FRAGMENT_RETURN_IDENTITY_RELEASED pid={System.Environment.ProcessId}");
            Quit(0);
        }
        catch (Exception error)
        {
            KeyboardRouteInputDriver.Current?.ReleaseAll();
            GD.PushError(error.ToString());
            Quit(1);
        }
    }
}
