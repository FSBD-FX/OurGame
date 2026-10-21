using System;
using Godot;

namespace Knight.Tests;

// Diagnostic scalars only: no resource registry, retained wrapper, forced GC,
// cache override or frame wait. A quiet observation is not an engine fix.
internal static class QuestFragmentLoadObservation
{
    internal static void Record(string phase) => GD.Print(
        $"FRAGMENT_LOAD_BOUNDARY phase={phase} thread={System.Environment.CurrentManagedThreadId} " +
        $"gc0={GC.CollectionCount(0)} gc1={GC.CollectionCount(1)} gc2={GC.CollectionCount(2)} " +
        $"ticks_usec={Time.GetTicksUsec()}");

    internal static PackedScene AfterLoad(PackedScene scene, bool observe)
    {
        if (observe)
        {
            Record("first-load-return");
            Record("first-instantiate-enter");
        }
        return scene;
    }
}
