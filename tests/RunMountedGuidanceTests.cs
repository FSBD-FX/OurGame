using System;
using System.Linq;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

// Narrow saved-Main guidance contracts; not a replacement for full prologue validation.
public partial class RunMountedGuidanceTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length == 1 && args[0].StartsWith(
                "--case-storage-root=", StringComparison.Ordinal),
                "mounted guidance accepts only one external storage argument");
            IsolatedPrologueFixture.ValidateStorageRoot();
            await ToSignal(this, SignalName.ProcessFrame);
            await PrologueGuidanceRuntimeTests.AssertMountedGuidanceFailureDisposesFixture(this);
            Task7Case[] cases = PrologueGuidanceRuntimeTests.Task7Cases(this).Where(item =>
                item.Name.Contains(".MountedGuidance", StringComparison.Ordinal) ||
                item.Name.EndsWith(".DisablingGuidanceClearsWithoutTouchingCompletionOrProgress",
                    StringComparison.Ordinal)).ToArray();
            TestAssert.Equal(4, cases.Length, "retain all four original mounted guidance contracts");
            foreach (Task7Case item in cases)
            {
                ulong[] before = Root.GetChildren().Select(node => node.GetInstanceId()).Order().ToArray();
                await Task7CaseRegistry.Execute(new[] { item });
                TestAssert.True(!Paused && before.SequenceEqual(Root.GetChildren()
                    .Select(node => node.GetInstanceId()).Order()),
                    item.Name + ": successful case also releases its scene without pausing the tree");
                GD.Print("MOUNTED_GUIDANCE_CASE_OK name=" + item.Name);
            }
            GD.Print("MOUNTED_GUIDANCE_CONTRACTS_OK cases=4 failure_cleanup=1");
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
