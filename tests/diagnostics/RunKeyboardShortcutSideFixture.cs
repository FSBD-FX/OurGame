using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests.Diagnostics;

public partial class RunKeyboardShortcutSideFixture : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            string selected = Array.Find(OS.GetCmdlineUserArgs(), value => value.StartsWith("--selected-step=", StringComparison.Ordinal));
            int selectedStep = selected == null ? 0 : int.Parse(selected["--selected-step=".Length..]);
            await PrologueCanyonTraversalTests.RunKeyboardShortcutSideFixture(this, selectedStep);
            Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); Quit(1); }
    }
}
