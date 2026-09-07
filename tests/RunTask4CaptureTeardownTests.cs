using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Knight.Tests;

public partial class RunTask4CaptureTeardownTests : SceneTree
{
    private const int RefCountedStressWrapperCount = 16384;
    private static readonly List<RefCounted> RefCountedStressWrappers = [];

    public override void _Initialize()
    {
        if (string.Equals(System.Environment.GetEnvironmentVariable("KNIGHT_TASK4_REFCOUNTED_STRESS"), "1",
                StringComparison.Ordinal))
        {
            _ = RunRefCountedStress();
            return;
        }

        try
        {
            TestSceneTeardown.AssertManagedWrapperDrainMarkerOrdering();
            GD.Print("TASK4_TEARDOWN_ORDER_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private async Task RunRefCountedStress()
    {
        try
        {
            RefCountedStressWrappers.Clear();
            for (int index = 0; index < RefCountedStressWrapperCount; index++)
            {
                RefCounted wrapper = new();
                if (!GodotObject.IsInstanceValid(wrapper))
                    throw new InvalidOperationException($"RefCounted stress wrapper {index} is not native-valid");
                RefCountedStressWrappers.Add(wrapper);
            }
            GD.Print($"TASK4_REFCOUNTED_STRESS_READY wrappers={RefCountedStressWrappers.Count}");
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);

            bool drain = string.Equals(
                System.Environment.GetEnvironmentVariable("KNIGHT_TASK4_REFCOUNTED_STRESS_DRAIN"), "1",
                StringComparison.Ordinal);
            if (drain)
            {
                RefCountedStressWrappers.Clear();
                TestSceneTeardown.DrainManagedWrappersBeforeQuit("refcounted-stress");
            }
            else
            {
                GD.Print($"TASK4_REFCOUNTED_STRESS_NO_DRAIN_QUIT wrappers={RefCountedStressWrappers.Count}");
            }
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
