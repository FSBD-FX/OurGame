using Knight.Mechanisms;

namespace Knight.Tests.Unit;

public static class CanyonPressureGateModelTests
{
    public static void Run()
    {
        PlateOpenThenGraceAndWarningHaveExactBoundaries();
        ReentryCancelsWarningWithoutAClosedStep();
        LargeStepCannotEraseTheOnlyWarning();
        InvalidTimeAndConfigurationCannotAdvanceClosing();
        ResetClearsTheLocalOpeningObservation();
    }

    // Catches treating a plate press as a momentary trigger with no timed release.
    private static void PlateOpenThenGraceAndWarningHaveExactBoundaries()
    {
        var model = new CanyonPressureGateModel();
        model.Step(true, false, 0d);
        TestAssert.True(model.WantsOpen, "a supported plate press requests the open state");
        model.MarkOpened();
        TestAssert.Equal(CanyonPressureGatePhase.Open, model.Phase,
            "committed opening establishes an open phase");
        TestAssert.True(model.HasOpened, "one committed opening latches local observation");
        model.Step(true, false, 9d);
        TestAssert.Equal(CanyonPressureGatePhase.Open, model.Phase,
            "remaining on the plate cannot consume grace or warning");
        model.Step(false, false, 0d);
        TestAssert.Equal(CanyonPressureGatePhase.Grace, model.Phase,
            "leaving starts grace, not an immediate warning");
        model.Step(false, false, 1.399d);
        TestAssert.Equal(CanyonPressureGatePhase.Grace, model.Phase,
            "grace remains active before 1.40 seconds");
        model.Step(false, false, .001d);
        TestAssert.Equal(CanyonPressureGatePhase.ClosingWarning, model.Phase,
            "exact grace boundary begins the harmless warning");
        TestAssert.True(model.WantsOpen, "warning keeps the door traversable");
        model.Step(false, false, .549d);
        TestAssert.True(model.WantsOpen, "warning remains open before 0.55 seconds");
        model.Step(false, false, .001d);
        TestAssert.True(!model.WantsOpen, "only full warning requests closure");
    }

    // Catches a close/reopen impulse when either hold area becomes occupied during warning.
    private static void ReentryCancelsWarningWithoutAClosedStep()
    {
        foreach (bool plate in new[] { false, true })
        {
            var model = new CanyonPressureGateModel();
            model.Step(true, false, 0d);
            model.MarkOpened();
            model.Step(false, false, 1.40d);
            model.Step(plate, !plate, .54d);
            TestAssert.Equal(CanyonPressureGatePhase.Open, model.Phase,
                "either outer plate or interior occupant cancels closing");
            TestAssert.True(model.WantsOpen, "reentry never requests a closed collision frame");
        }
    }

    // Catches a frame spike jumping from grace directly to a blocking door.
    private static void LargeStepCannotEraseTheOnlyWarning()
    {
        var model = new CanyonPressureGateModel();
        model.Step(true, false, 0d);
        model.MarkOpened();
        model.Step(false, false, 10d);
        TestAssert.Equal(CanyonPressureGatePhase.ClosingWarning, model.Phase,
            "a large step still presents the visible warning phase");
        TestAssert.True(model.WantsOpen, "large-step warning remains harmless and traversable");
    }

    // Catches NaN, infinity, negative time, or broken tunings turning into a close request.
    private static void InvalidTimeAndConfigurationCannotAdvanceClosing()
    {
        var model = new CanyonPressureGateModel();
        model.Step(true, false, 0d);
        model.MarkOpened();
        model.Step(false, false, double.NaN);
        model.Step(false, false, double.PositiveInfinity);
        model.Step(false, false, -1d);
        TestAssert.Equal(CanyonPressureGatePhase.Grace, model.Phase,
            "invalid delta cannot advance a closing timer");
        model.OpenGraceSeconds = double.NaN;
        model.Step(false, false, 8d);
        TestAssert.True(model.WantsOpen, "nonfinite grace cannot request closure");
    }

    // Catches leaked recorder observation or a pending countdown after checkpoint reset.
    private static void ResetClearsTheLocalOpeningObservation()
    {
        var model = new CanyonPressureGateModel();
        model.Step(true, false, 0d);
        model.MarkOpened();
        model.Step(false, false, 1.40d);
        model.Reset();
        TestAssert.Equal(CanyonPressureGatePhase.Closed, model.Phase,
            "checkpoint reset clears prior phase");
        TestAssert.True(!model.HasOpened, "checkpoint reset clears local recorder latch");
        TestAssert.True(!model.WantsOpen, "reset requests the default closed state");
    }
}
