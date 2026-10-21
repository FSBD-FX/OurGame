using Godot;
using Knight.Combat;
using Knight.Enemies.Sentry;

namespace Knight.Tests.Unit;

public static class BellBoltSentryModelTests
{
    public static void Run()
    {
        WarningAlwaysPrecedesOneLockedShot();
        SightLossBeforeLockCancelsWithoutFiring();
        ThreadInterruptsAnUnlaunchedShotForPointEightSeconds();
        DeathWinsOverAReadyShot();
        InvalidInputNeverLaunches();
        FireAndRecoveryDoNotStartAnotherShotEarly();
        PauseResumeAndRapidStepsDoNotDuplicateFire();
    }

    private static BellBoltSentryInput Input(Vector2 target, bool visible = true,
        bool stun = false, bool dead = false) =>
        new(Vector2.Zero, target, visible, stun, dead);

    private static void WarningAlwaysPrecedesOneLockedShot()
    {
        var model = new BellBoltSentryModel();
        BellBoltSentryFrame start = model.Step(Input(new Vector2(300f, 0f)), 0d);
        TestAssert.Equal(BellBoltSentryState.Telegraph, start.State, "visible in-range target starts warning");
        TestAssert.Equal(AttackPhase.Telegraph, start.AttackTimeline.Phase, "warning precedes shot");
        TestAssert.True(!start.FireRequested, "warning cannot fire on entry");

        BellBoltSentryFrame lockedFrame = model.Step(Input(new Vector2(300f, 80f)), .55d);
        TestAssert.True(lockedFrame.AttackTimeline.HasLockedTarget, "aim locks at the configured midpoint");
        Vector2 locked = lockedFrame.AimDirection;
        BellBoltSentryFrame shot = model.Step(Input(new Vector2(-300f, 0f)), .55d);
        TestAssert.True(shot.FireRequested && shot.AimDirection.Dot(locked) > .999f,
            "player crossing after lock cannot turn the shot");
        TestAssert.Equal(AttackPhase.Active, shot.AttackTimeline.Phase, "locked warning enters active shot");
        TestAssert.True(!model.Step(Input(new Vector2(-300f, 0f)), .01d).FireRequested,
            "shot request is a one-frame edge");
    }

    private static void SightLossBeforeLockCancelsWithoutFiring()
    {
        var model = new BellBoltSentryModel();
        model.Step(Input(new Vector2(200f, 0f)), 0d);
        BellBoltSentryFrame cancelled = model.Step(Input(new Vector2(200f, 0f), visible: false), .30d);
        TestAssert.Equal(BellBoltSentryState.Idle, cancelled.State, "pre-lock sight loss cancels warning");
        TestAssert.True(!cancelled.FireRequested && cancelled.AttackTimeline.Phase == AttackPhase.None,
            "cancelled warning cannot leave queued fire or presentation identity");
    }

    private static void ThreadInterruptsAnUnlaunchedShotForPointEightSeconds()
    {
        var model = new BellBoltSentryModel();
        model.Step(Input(new Vector2(250f, 0f)), 0d);
        BellBoltSentryFrame stunned = model.Step(Input(new Vector2(250f, 0f), stun: true), .20d);
        TestAssert.Equal(BellBoltSentryState.Stunned, stunned.State, "thread interrupts an unlaunched warning");
        TestAssert.True(stunned.AttackTimeline.Phase == AttackPhase.None && !stunned.FireRequested,
            "thread interruption clears attack state");
        TestAssert.Equal(BellBoltSentryState.Stunned,
            model.Step(Input(new Vector2(250f, 0f)), .79d).State, "visible stun lasts before 0.80 seconds");
        TestAssert.Equal(BellBoltSentryState.Idle,
            model.Step(Input(new Vector2(250f, 0f)), .01d).State, "stun ends exactly at 0.80 seconds");
    }

    private static void DeathWinsOverAReadyShot()
    {
        var model = new BellBoltSentryModel();
        model.Step(Input(new Vector2(200f, 0f)), 0d);
        model.Step(Input(new Vector2(200f, 0f)), .55d);
        BellBoltSentryFrame dead = model.Step(Input(new Vector2(200f, 0f), dead: true), .55d);
        TestAssert.Equal(BellBoltSentryState.Dead, dead.State, "death wins at the fire boundary");
        TestAssert.True(!dead.FireRequested && dead.AttackTimeline.Phase == AttackPhase.None,
            "death cannot emit a delayed shot");
    }

    private static void InvalidInputNeverLaunches()
    {
        var model = new BellBoltSentryModel();
        BellBoltSentryFrame invalidAim = model.Step(Input(new Vector2(float.NaN, 0f)), 0d);
        TestAssert.Equal(BellBoltSentryState.Idle, invalidAim.State, "non-finite aim is ignored safely");
        BellBoltSentryFrame invalidDelta = model.Step(Input(new Vector2(200f, 0f)), double.PositiveInfinity);
        TestAssert.True(!invalidDelta.FireRequested, "infinite delta cannot skip a warning and launch");
    }

    private static void FireAndRecoveryDoNotStartAnotherShotEarly()
    {
        var model = new BellBoltSentryModel();
        model.Step(Input(new Vector2(200f, 0f)), 0d);
        model.Step(Input(new Vector2(200f, 0f)), 1.10d);
        BellBoltSentryFrame recover = model.Step(Input(new Vector2(200f, 0f)), .12d);
        TestAssert.Equal(BellBoltSentryState.Recover, recover.State, "active shot lasts 0.12 seconds");
        TestAssert.True(!recover.FireRequested, "recovery cannot launch another bolt");
        TestAssert.Equal(BellBoltSentryState.Recover,
            model.Step(Input(new Vector2(200f, 0f)), .99d).State, "recovery remains closed before one second");
        TestAssert.Equal(BellBoltSentryState.Idle,
            model.Step(Input(new Vector2(200f, 0f)), .01d).State, "recovery returns idle at one second");
    }

    private static void PauseResumeAndRapidStepsDoNotDuplicateFire()
    {
        var model = new BellBoltSentryModel();
        model.Step(Input(new Vector2(200f, 0f)), 0d);
        BellBoltSentryFrame beforePause = model.Step(Input(new Vector2(200f, 0f)), .40d);
        BellBoltSentryFrame paused = model.Step(Input(new Vector2(200f, 0f)), 0d);
        TestAssert.Equal(beforePause.AttackTimeline.PhaseRemainingSeconds,
            paused.AttackTimeline.PhaseRemainingSeconds, "paused zero delta does not advance warning time");
        for (int index = 0; index < 8; index++)
            TestAssert.True(!model.Step(Input(new Vector2(200f, 0f)), 0d).FireRequested,
                "rapid paused steps cannot duplicate or skip into fire");
        BellBoltSentryFrame resumed = model.Step(Input(new Vector2(200f, 0f)), .70d);
        TestAssert.True(resumed.FireRequested, "resuming advances the remaining warning once");
        TestAssert.True(!model.Step(Input(new Vector2(200f, 0f)), 0d).FireRequested,
            "the resumed fire edge is emitted once only");
    }
}
