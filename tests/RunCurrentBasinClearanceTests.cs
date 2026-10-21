using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunCurrentBasinClearanceTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length == 1 && args[0].StartsWith(
                "--case-storage-root=", StringComparison.Ordinal),
                "current clearance accepts only one external storage argument");
            IsolatedPrologueFixture.ValidateStorageRoot();
            await CurrentBasinLaborerClearanceRuntimeTests.Run(this);
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(1);
        }
    }
}
