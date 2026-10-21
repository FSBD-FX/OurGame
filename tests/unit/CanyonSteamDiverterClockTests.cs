using Knight.Mechanisms;
using System;
using System.Collections.Generic;

namespace Knight.Tests.Unit;

public static class CanyonSteamDiverterClockTests
{
    public static void RunContracts()
    {
        // Characterization guards the actual reused jet seam before new-clock
        // failures can count as RED. Expectations are independent literals.
        ExistingJetFloatBoundaries();
        var failures = new List<string>();
        foreach (Action contract in new Action[] { FullWarningBeforeActive,
            LargeStepsPreserveEveryPhaseAndDiscardRemainder, ZeroTuningUsesExistingFloors,
            InvalidTuningFailsClosedAndValidTuningRecovers, PhaseLocalTimeDoesNotStallAtLargeMagnitude,
            InvalidDeltaPauseResetAndRetirement })
        {
            try { contract(); }
            catch (InvalidOperationException failure) when (failure.Message.StartsWith("Assertion failed:", StringComparison.Ordinal))
            { failures.Add(contract.Method.Name + ": " + failure.Message); }
        }
        if (failures.Count != 0)
            throw new InvalidOperationException("Assertion failed: paired diverter clock contracts are not satisfied\n" + string.Join("\n", failures));
    }

    // Break caught: using rounded safe+warning or Phase01 as active authority.
    private static void ExistingJetFloatBoundaries()
    {
        foreach (var sample in new (float Time, bool Warning, bool Active)[] {
            (.74999994f, false, false), (.75f, true, false), (1f, true, false),
            (1.4f, true, false), (1.4000001f, false, true),
            (1.9999999f, false, true), (2f, false, false) })
            AssertFrame(CanyonSteamJet.Evaluate(sample.Time, 1f, .4f, .6f), sample.Warning, sample.Active,
                "existing jet default literal boundary " + sample.Time);
        TestAssert.Equal(0f, CanyonSteamJet.Evaluate(1f, 1f, .4f, .6f).Phase01,
            "ordinary warning restarts progress but not the warning contract");
        foreach (var sample in new (float Time, bool Warning, bool Active)[] {
            (0f, false, false), (.0075f, true, false), (.01f, true, false),
            (.02f, false, true), (.03f, false, false) })
            AssertFrame(CanyonSteamJet.Evaluate(sample.Time, 0f, 0f, 0f), sample.Warning, sample.Active,
                "existing zero-tuning floor boundary " + sample.Time);
    }

    // Break caught: shortening the safe final quarter or ordinary .4 warning.
    private static void FullWarningBeforeActive()
    {
        var clock = DefaultClock();
        clock.Step(.749999d); AssertSample(clock, false, false, "safe before early warning");
        clock.Step(.000001d); AssertPhase(clock, CanyonSteamDiverterPhase.EarlyWarning, true, false, "early warning begins at .75");
        clock.Step(.249999d); AssertSample(clock, true, false, "early warning remains harmless for its complete quarter");
        clock.Step(.000001d); AssertPhase(clock, CanyonSteamDiverterPhase.Warning, true, false, "ordinary warning starts after complete safe interval");
        TestAssert.Equal(0f, CanyonSteamJet.Evaluate(clock.SampleSeconds, 1f, .4f, .6f).Phase01,
            "clock maps ordinary warning entry to real jet progress reset");
        clock.Step(.399999d); AssertSample(clock, true, false, "ordinary warning remains harmless before full .4 seconds");
        clock.Step(.000002d); AssertPhase(clock, CanyonSteamDiverterPhase.Active, false, true, "real jet is active only after full ordinary warning");
        clock.Step(.599999d); AssertSample(clock, false, true, "active interval is not prematurely reset");
        clock.Step(.000002d); AssertPhase(clock, CanyonSteamDiverterPhase.Safe, false, false, "explicit cycle restart publishes safe frame");
        TestAssert.Equal(0f, clock.SampleSeconds, "cycle restart resets real jet sample exactly");
    }

    // Break caught: modulo/while catch-up hides one or more visible phases.
    private static void LargeStepsPreserveEveryPhaseAndDiscardRemainder()
    {
        var clock = DefaultClock();
        foreach (var expected in new (CanyonSteamDiverterPhase Phase, bool Warning, bool Active)[] {
            (CanyonSteamDiverterPhase.EarlyWarning, true, false),
            (CanyonSteamDiverterPhase.Warning, true, false),
            (CanyonSteamDiverterPhase.Active, false, true),
            (CanyonSteamDiverterPhase.Safe, false, false) })
        {
            clock.Step(double.MaxValue);
            AssertPhase(clock, expected.Phase, expected.Warning, expected.Active, "one boundary per finite large callback");
            TestAssert.Equal(0d, clock.PhaseElapsedSeconds, "large-step overshoot is discarded at each visible phase entry");
        }
        clock.Step(.001d); AssertSample(clock, false, false, "large previous steps leave no catch-up debt");
    }

    // Break caught: zero is rejected or becomes zero-length damaging phases.
    private static void ZeroTuningUsesExistingFloors()
    {
        var clock = new CanyonSteamDiverterClock();
        TestAssert.True(clock.TryConfigure(0f, 0f, 0f), "zero tuning accepts the existing .01 floors");
        clock.Step(.007499d); AssertZeroSample(clock, false, false, "zero tuning still has a safe interval");
        clock.Step(.000001d); AssertZeroSample(clock, true, false, "floored early warning starts harmless");
        clock.Step(.0025d); AssertZeroSample(clock, true, false, "floored ordinary warning remains harmless");
        clock.Step(.009999d); AssertZeroSample(clock, true, false, "floored ordinary warning cannot be skipped");
        clock.Step(.000002d); AssertZeroSample(clock, false, true, "floored warning completes before activation");
        clock.Step(.01d); AssertZeroSample(clock, false, false, "floored active interval explicitly resets");
    }

    // Break caught: nonfinite/negative/overflow/collapsed tuning can arm jets.
    private static void InvalidTuningFailsClosedAndValidTuningRecovers()
    {
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            foreach (var tuning in new (float Safe, float Warning, float Active)[] {
                (invalid, .4f, .6f), (1f, invalid, .6f), (1f, .4f, invalid) })
                Reject(tuning.Safe, tuning.Warning, tuning.Active);
        Reject(float.MaxValue, float.MaxValue, float.MaxValue);
        Reject(100000000f, .01f, .01f);
        Reject(16777216f, .01f, .01f);
        var clock = DefaultClock(); clock.Step(10d); clock.Step(10d); clock.Step(10d);
        TestAssert.True(!clock.TryConfigure(1f, float.NaN, .6f) && !clock.IsConfigured,
            "invalid reconfiguration retires an already active sample until valid configuration");
        TestAssert.Equal(0f, clock.SampleSeconds, "invalid reconfiguration clears old active sample");
        TestAssert.True(clock.TryConfigure(1f, .4f, .6f), "valid tuning can recover without inherited active phase");
        AssertSample(clock, false, false, "valid recovery starts safe");
    }

    private static void Reject(float safe, float warning, float active)
    {
        var clock = new CanyonSteamDiverterClock();
        TestAssert.True(!clock.TryConfigure(safe, warning, active) && !clock.IsConfigured,
            "invalid or unrepresentable phase tuning fails closed");
        clock.Step(double.MaxValue);
        TestAssert.Equal(0f, clock.SampleSeconds, "rejected tuning cannot publish an armed sample");
    }

    // Break caught: float elapsed += small delta stalls at large safe duration;
    // no arbitrary global tuning cap may replace representability validation.
    private static void PhaseLocalTimeDoesNotStallAtLargeMagnitude()
    {
        var clock = new CanyonSteamDiverterClock();
        TestAssert.True(clock.TryConfigure(16777216f, 16f, 16f), "large but representable phases remain supported");
        clock.Step(.001d); clock.Step(.001d);
        TestAssert.True(clock.PhaseElapsedSeconds > .0019d && clock.PhaseElapsedSeconds < .0021d,
            "phase-local double time retains sub-ULP elapsed progress");
        clock.Step(double.MaxValue); clock.Step(double.MaxValue); clock.Step(double.MaxValue);
        AssertFrame(CanyonSteamJet.Evaluate(clock.SampleSeconds, 16777216f, 16f, 16f), false, true,
            "large accepted tuning has a representable real active sample");
    }

    // Break caught: bad delta poisons progress, pause consumes time, reset or
    // queued work re-arms a retired clock.
    private static void InvalidDeltaPauseResetAndRetirement()
    {
        var clock = DefaultClock(); clock.Step(.30d);
        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1d, 0d }) clock.Step(invalid);
        clock.Step(10d, paused: true);
        TestAssert.Equal(.30d, clock.PhaseElapsedSeconds, "pause and invalid delta preserve valid elapsed time");
        clock.Step(.45d); AssertSample(clock, true, false, "valid resume reaches early warning without poison");
        clock.Reset(); AssertSample(clock, false, false, "reset cancels prior warning");
        TestAssert.Equal(0d, clock.PhaseElapsedSeconds, "reset clears phase-local countdown");
        clock.Retire(); clock.Step(10d); clock.Reset();
        TestAssert.True(!clock.IsConfigured && !clock.TryConfigure(1f, .4f, .6f), "retired clock cannot be reconfigured or rearmed");
        TestAssert.Equal(0f, clock.SampleSeconds, "retired sample remains harmless");
    }

    private static CanyonSteamDiverterClock DefaultClock()
    {
        var clock = new CanyonSteamDiverterClock();
        TestAssert.True(clock.TryConfigure(1f, .4f, .6f) && clock.IsConfigured,
            "default representable clock tuning is accepted");
        AssertSample(clock, false, false, "configured clock starts safe");
        return clock;
    }
    private static void AssertPhase(CanyonSteamDiverterClock clock, CanyonSteamDiverterPhase phase, bool warning, bool active, string reason)
    { TestAssert.Equal(phase, clock.Phase, reason); AssertSample(clock, warning, active, reason); }
    private static void AssertSample(CanyonSteamDiverterClock clock, bool warning, bool active, string reason) =>
        AssertFrame(CanyonSteamJet.Evaluate(clock.SampleSeconds, 1f, .4f, .6f), warning, active, reason);
    private static void AssertZeroSample(CanyonSteamDiverterClock clock, bool warning, bool active, string reason) =>
        AssertFrame(CanyonSteamJet.Evaluate(clock.SampleSeconds, 0f, 0f, 0f), warning, active, reason);
    private static void AssertFrame(CanyonMechanismFrame frame, bool warning, bool active, string reason) =>
        TestAssert.True(frame.Telegraphing == warning && frame.HazardActive == active && frame.Traversable == !active, reason);
}
