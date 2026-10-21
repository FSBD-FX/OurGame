using Knight.Mechanisms;
using System;
using System.Collections.Generic;

namespace Knight.Tests.Unit;

public static class CanyonSteamDiverterModelTests
{
    public static void RunFirstHandoff()
    {
        AcceptedSwitchKeepsBothSidesHarmlessForTheCompleteHandoff();
    }

    public static void RunRemainingContracts()
    {
        var failures = new List<string>();
        foreach (Action contract in new Action[] { AcceptedSwitchKeepsBothSidesHarmlessForTheCompleteHandoff,
            BusySwitchCannotToggleOrRestartTheHandoff, BothDirectionsRemainExclusive,
            InvalidDeltaCannotAdvanceOrPoisonTheHandoff, PauseFreezesAndResumeCompletesRemainingTime,
            ResetRestoresRightAndCancelsOldTransition, RetirementRejectsInputAndCannotRearm })
        {
            try { contract(); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            {
                failures.Add(contract.Method.Name + ": " + failure.Message);
            }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: paired diverter model contracts are not satisfied\n" + string.Join("\n", failures));
    }

    // Break caught: a second accepted cast toggles back or refreshes the timer.
    private static void BusySwitchCannotToggleOrRestartTheHandoff()
    {
        var model = new CanyonSteamDiverterModel();
        TestAssert.True(model.TrySwitch(), "first switch is accepted before busy rejection test");
        model.Step(.30d);
        TestAssert.True(!model.TrySwitch(), "busy paired diverter rejects repeated switches without restarting handoff");
        TestAssert.Equal(CanyonSteamDiverterSide.Left, model.SelectedSide,
            "rejected busy cast cannot change the desired side");
        model.Step(.30d);
        TestAssert.True(!model.IsHandingOff && model.LeftCanCycle && !model.RightCanCycle,
            "rejected busy cast cannot extend the original handoff deadline");
    }

    // Break caught: reversal selects left again or leaves the old side eligible.
    private static void BothDirectionsRemainExclusive()
    {
        var model = new CanyonSteamDiverterModel();
        model.TrySwitch(); model.Step(.60d);
        TestAssert.True(model.LeftCanCycle && !model.RightCanCycle, "forward selection allows only left cycling");
        TestAssert.True(model.TrySwitch(), "a fresh cast can reverse after completed handoff");
        TestAssert.Equal(CanyonSteamDiverterSide.Right, model.SelectedSide, "reverse selects right");
        TestAssert.True(!model.LeftCanCycle && !model.RightCanCycle, "reverse transition disarms both sides");
        model.Step(.60d);
        TestAssert.True(!model.LeftCanCycle && model.RightCanCycle, "completed reverse allows only right cycling");
    }

    // Break caught: infinity ends transition, negative time rewinds it, or NaN
    // poisons elapsed time so later valid physics steps never complete.
    private static void InvalidDeltaCannotAdvanceOrPoisonTheHandoff()
    {
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -.20d })
        {
            var model = new CanyonSteamDiverterModel();
            model.TrySwitch(); model.Step(.30d); model.Step(invalid);
            TestAssert.True(model.IsHandingOff && !model.LeftCanCycle && !model.RightCanCycle,
                "invalid delta cannot end the harmless handoff");
            model.Step(.30d);
            TestAssert.True(!model.IsHandingOff && model.LeftCanCycle,
                "invalid delta cannot poison or rewind later valid handoff completion");
        }
    }

    // Break caught: pause consumes transition time or resume resets its deadline.
    private static void PauseFreezesAndResumeCompletesRemainingTime()
    {
        var model = new CanyonSteamDiverterModel();
        model.TrySwitch(); model.Step(.30d);
        model.Step(10d, paused: true);
        TestAssert.True(model.IsHandingOff && !model.LeftCanCycle && !model.RightCanCycle,
            "pause cannot consume the remaining harmless handoff");
        model.Step(.299d);
        TestAssert.True(model.IsHandingOff, "resume preserves the unfinished 0.60 second deadline");
        model.Step(.001d);
        TestAssert.True(!model.IsHandingOff && model.LeftCanCycle, "resume finishes only the remaining transition time");
    }

    // Break caught: checkpoint recovery preserves a local selection/countdown.
    private static void ResetRestoresRightAndCancelsOldTransition()
    {
        var model = new CanyonSteamDiverterModel();
        model.TrySwitch(); model.Step(.30d); model.Reset();
        TestAssert.Equal(CanyonSteamDiverterSide.Right, model.SelectedSide,
            "checkpoint model reset chooses documented default right");
        TestAssert.True(!model.IsHandingOff && !model.LeftCanCycle && model.RightCanCycle,
            "reset cancels handoff and only right may cycle");
        model.Step(10d);
        TestAssert.True(!model.LeftCanCycle && model.RightCanCycle, "old transition cannot later arm reset left side");
    }

    // Break caught: retiring a controller leaves eligible clocks/queued input.
    private static void RetirementRejectsInputAndCannotRearm()
    {
        var model = new CanyonSteamDiverterModel();
        model.TrySwitch(); model.Retire();
        TestAssert.True(model.IsRetired, "retirement marks the paired model unavailable");
        TestAssert.True(!model.TrySwitch(), "retired pair rejects new input");
        model.Step(10d); model.Reset();
        TestAssert.True(!model.LeftCanCycle && !model.RightCanCycle,
            "retired instance cannot rearm through queued time or reset");
    }

    // Break caught: a successful switch leaves either jet cycling, selects the
    // old side, or starts the new side before the independent literal 0.60 s.
    private static void AcceptedSwitchKeepsBothSidesHarmlessForTheCompleteHandoff()
    {
        var model = new CanyonSteamDiverterModel();
        TestAssert.True(model.RightCanCycle && !model.LeftCanCycle,
            "fresh diverter allows only the documented default right side to cycle");
        TestAssert.True(model.TrySwitch(),
            "idle paired diverter accepts a switch into the harmless handoff");
        TestAssert.Equal(CanyonSteamDiverterSide.Left, model.SelectedSide,
            "accepted switch chooses the formerly harmless left side");
        TestAssert.True(model.IsHandingOff && !model.LeftCanCycle && !model.RightCanCycle,
            "accepted switch immediately publishes neither side may cycle");
        model.Step(.599d);
        TestAssert.True(model.IsHandingOff && !model.LeftCanCycle && !model.RightCanCycle,
            "both sides stay logically disarmed before the full 0.60 second handoff");
        model.Step(.001d);
        TestAssert.True(!model.IsHandingOff && model.LeftCanCycle && !model.RightCanCycle,
            "only the selected left side may cycle after the complete handoff");
    }
}
