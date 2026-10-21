using Godot;
using Knight.Mechanisms;

namespace Knight.Tests.Unit;

public static class CanyonMechanismModelTests
{
    public static void Run()
    {
        CounterweightWaitsThenStopsAtBothEndpoints();
        GearPistonHasFourBehaviorallyDistinctPhases();
        SteamJetCyclesFromWarningIntoDanger();
        PendulumTelegraphPrecedesTheDangerArc();
        ProgressUsesIndependentStableIdSets();
    }

    private static void CounterweightWaitsThenStopsAtBothEndpoints()
    {
        CanyonMechanismFrame idle = CanyonCounterweightLift.Evaluate(false, 99f, 2f, .5f);
        TestAssert.True(!idle.HazardActive && idle.Traversable && idle.Phase01 == 0f,
            "inactive counterweight remains at its lower endpoint");

        CanyonMechanismFrame upper = CanyonCounterweightLift.Evaluate(true, 2.25f, 2f, .5f);
        TestAssert.True(!upper.HazardActive && upper.Traversable && upper.Phase01 == 1f,
            "counterweight dwells at the authored upper endpoint");

        CanyonMechanismFrame lower = CanyonCounterweightLift.Evaluate(true, 4.75f, 2f, .5f);
        TestAssert.True(!lower.HazardActive && lower.Traversable && lower.Phase01 == 0f,
            "counterweight reverses and dwells at the authored lower endpoint");
    }

    private static void GearPistonHasFourBehaviorallyDistinctPhases()
    {
        CanyonMechanismFrame safe = CanyonGearPiston.Evaluate(.2f, 1f, .5f, .5f, .5f);
        CanyonMechanismFrame warning = CanyonGearPiston.Evaluate(1.2f, 1f, .5f, .5f, .5f);
        CanyonMechanismFrame active = CanyonGearPiston.Evaluate(1.7f, 1f, .5f, .5f, .5f);
        CanyonMechanismFrame recovery = CanyonGearPiston.Evaluate(2.2f, 1f, .5f, .5f, .5f);
        TestAssert.True(!safe.Telegraphing && !safe.HazardActive && safe.Traversable,
            "piston safe phase is open traversal");
        TestAssert.True(warning.Telegraphing && !warning.HazardActive && warning.Traversable,
            "piston warning precedes collision");
        TestAssert.True(!active.Telegraphing && active.HazardActive && !active.Traversable,
            "piston active extension alone blocks the route");
        TestAssert.True(!recovery.Telegraphing && !recovery.HazardActive && recovery.Traversable,
            "piston recovery reopens the route");
    }

    private static void SteamJetCyclesFromWarningIntoDanger()
    {
        CanyonMechanismFrame warning = CanyonSteamJet.Evaluate(.8f, 1f, .4f, .6f);
        CanyonMechanismFrame danger = CanyonSteamJet.Evaluate(1.5f, 1f, .4f, .6f);
        TestAssert.True(warning.Telegraphing && !warning.HazardActive,
            "steam jet warns before it becomes dangerous");
        TestAssert.True(!danger.Telegraphing && danger.HazardActive,
            "steam jet danger follows its warning boundary");
    }

    private static void PendulumTelegraphPrecedesTheDangerArc()
    {
        CanyonMechanismFrame outside = CanyonPendulumHazard.Evaluate(0f, 2f);
        CanyonMechanismFrame boundary = CanyonPendulumHazard.Evaluate(.40f, 2f);
        CanyonMechanismFrame center = CanyonPendulumHazard.Evaluate(.50f, 2f);
        TestAssert.True(!outside.Telegraphing && !outside.HazardActive,
            "pendulum turn-around is outside the danger arc");
        TestAssert.True(boundary.Telegraphing && !boundary.HazardActive,
            "pendulum announces entry into its danger arc");
        TestAssert.True(center.HazardActive && !center.Telegraphing,
            "pendulum collision danger agrees with its visible center swing");
    }

    private static void ProgressUsesIndependentStableIdSets()
    {
        var progress = new Knight.Prologue.PrologueProgress();
        TestAssert.True(progress.OpenShortcut("canyon.v2.upper-basin-shortcut"),
            "upper/basin shortcut opens once");
        TestAssert.True(progress.OpenBellChainShortcut(), "legacy delegate opens only the temple gate");
        TestAssert.True(progress.IsShortcutOpen("canyon.v2.upper-basin-shortcut") &&
                        progress.BellChainShortcutOpen && progress.OpenShortcuts.Count == 2,
            "two shortcut IDs remain independent");
        TestAssert.True(progress.ActivateMechanism("canyon.v2.test-mechanism") &&
                        !progress.ActivateMechanism("canyon.v2.test-mechanism"),
            "mechanism stable IDs latch exactly once");
    }
}
