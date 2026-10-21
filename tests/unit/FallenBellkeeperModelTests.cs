using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Config;

namespace Knight.Tests.Unit;

public static class FallenBellkeeperModelTests
{
    public static void Run()
    {
        DefaultTuningLocksApprovedBoundaries();
        ApprovedBossTimingHealthAndDamageTableIsExact();
        DormantWaitsForArenaAndIntroCannotDamage();
        PhaseOneCyclesThreeAttacksThroughTelegraphActiveAndRecovery();
        ExactSixtyPercentTriggersOneNonDamagingTransition();
        PhaseTwoSchedulesHammerAndOnlyThrownChainAcceptsThread();
        ExposureExpiryReclosesArmorAtExactEndpoint();
        ZeroHealthDefeatsEveryLivingStateAndRemainsInert();
    }

    private static void ApprovedBossTimingHealthAndDamageTableIsExact()
    {
        var tuning = new BossTuning();
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.Sweep), .80f, .25f, .70f,
            "approved Boss Sweep timing");
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.LeapSlam), .90f, .25f, .75f,
            "approved Boss LeapSlam timing");
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.GroundWave), 1.00f, .30f, .75f,
            "approved Boss GroundWave timing");
        AssertTiming(tuning.TimingFor(FallenBellkeeperAttack.HammerThrow), .85f, .80f, .70f,
            "approved Boss HammerThrow timing uses the real 0.80-second flight window");
        TestAssert.Equal(12, tuning.MaxHealth, "approved first-level Boss health is exactly 12");
        TestAssert.Equal(1, tuning.ContactDamage,
            "first-level Boss contact damage is exactly one segment");
        TestAssert.Near(.65f, tuning.PhaseTwoArmoredSeconds, .000001f,
            "Boss preserves the existing 0.65-second initial phase-two armored dwell");
    }

    private static void AssertTiming(BossAttackTiming actual, float telegraph, float active,
        float recovery, string name)
    {
        TestAssert.Near(telegraph, actual.TelegraphSeconds, .000001f, $"{name}: Telegraph");
        TestAssert.Near(active, actual.ActiveSeconds, .000001f, $"{name}: Active");
        TestAssert.Near(recovery, actual.RecoverySeconds, .000001f, $"{name}: Recovery");
    }

    private static void DefaultTuningLocksApprovedBoundaries()
    {
        BossTuning tuning = new();
        TestAssert.Equal(0, tuning.Validate().Count, "default Boss tuning is valid");
        TestAssert.Near(.60f, tuning.TransitionHealthRatio, .000001f,
            "Boss phase transition remains exactly sixty percent");
        TestAssert.True(tuning.RetryReadyDelaySeconds <= 3f,
            "Boss retry-ready delay never exceeds three seconds");
    }

    private static void DormantWaitsForArenaAndIntroCannotDamage()
    {
        Fixture fixture = NewFixture();
        FallenBellkeeperFrame dormant = fixture.Model.Step(Input(false), 10d);
        TestAssert.Equal(FallenBellkeeperState.Dormant, dormant.State,
            "Boss remains dormant until arena activation");
        TestAssert.True(!dormant.AttackActive && dormant.Attack == FallenBellkeeperAttack.None,
            "dormant Boss is inert");

        FallenBellkeeperFrame intro = fixture.Model.Step(Input(true), 0d);
        TestAssert.Equal(FallenBellkeeperState.Intro, intro.State, "arena activation enters Intro");
        TestAssert.True(intro.EnteredState && !intro.AttackActive,
            "Intro entry is visible and non-damaging");
        FallenBellkeeperFrame introEnd = fixture.Model.Step(Input(true), fixture.Settings.IntroSeconds - .001f);
        TestAssert.Equal(FallenBellkeeperState.Intro, introEnd.State, "Intro holds before endpoint");
        FallenBellkeeperFrame phaseOne = fixture.Model.Step(Input(true), .001f);
        TestAssert.Equal(FallenBellkeeperState.PhaseOne, phaseOne.State,
            "Intro enters phase one exactly at configured endpoint");
        TestAssert.True(!phaseOne.AttackActive, "phase one begins with a telegraph, not active damage");
    }

    private static void PhaseOneCyclesThreeAttacksThroughTelegraphActiveAndRecovery()
    {
        Fixture fixture = NewFixture();
        FallenBellkeeperFrame frame = EnterPhaseOne(fixture);
        FallenBellkeeperAttack[] expected =
        {
            FallenBellkeeperAttack.Sweep,
            FallenBellkeeperAttack.LeapSlam,
            FallenBellkeeperAttack.GroundWave,
            FallenBellkeeperAttack.Sweep
        };
        EnemyAttackKind[] expectedKinds =
        {
            EnemyAttackKind.BossSweep,
            EnemyAttackKind.BossLeapSlam,
            EnemyAttackKind.BossGroundWave,
            EnemyAttackKind.BossSweep
        };
        for (int index = 0; index < expected.Length; index++)
        {
            FallenBellkeeperAttack attack = expected[index];
            BossAttackTiming timing = fixture.Settings.TimingFor(attack);
            Vector2 lockedTarget = new(640f + index * 20f, 310f);
            TestAssert.Equal(attack, frame.Attack, $"deterministic phase-one attack is {attack}");
            TestAssert.Equal(AttackPhase.Telegraph, frame.AttackTimeline.Phase,
                $"{attack} begins in Telegraph");
            TestAssert.Equal(expectedKinds[index], frame.AttackTimeline.Kind,
                $"{attack} has its unique presentation kind");
            ulong attackId = frame.AttackTimeline.AttackId;
            TestAssert.Equal((ulong)(index + 1), attackId,
                $"{attack} allocates one identity only when Telegraph begins");
            frame = fixture.Model.Step(Input(true), timing.TelegraphSeconds - .001f, lockedTarget);
            TestAssert.True(!frame.AttackActive, $"{attack} telegraph cannot damage before endpoint");
            TestAssert.Near(.001f, frame.AttackTimeline.PhaseRemainingSeconds, .00001f,
                $"{attack} Telegraph is inclusive immediately before the endpoint");
            frame = fixture.Model.Step(Input(true), .001f, lockedTarget);
            TestAssert.True(frame.AttackActive, $"{attack} becomes active at telegraph endpoint");
            TestAssert.Equal(AttackPhase.Active, frame.AttackTimeline.Phase,
                $"{attack} publishes Active at the exact boundary");
            TestAssert.Equal(attackId, frame.AttackTimeline.AttackId,
                $"{attack} Active preserves its Telegraph identity");
            TestAssert.True(frame.AttackTimeline.HasLockedTarget,
                $"{attack} locks its world target at the Telegraph-to-Active boundary");
            TestAssert.Equal(lockedTarget, frame.AttackTimeline.LockedWorldTarget,
                $"{attack} publishes the locked world target");
            frame = fixture.Model.Step(Input(true), timing.ActiveSeconds, new Vector2(-500f, -500f));
            TestAssert.True(!frame.AttackActive, $"{attack} closes exactly at active endpoint");
            TestAssert.Equal(AttackPhase.Recovery, frame.AttackTimeline.Phase,
                $"{attack} publishes Recovery at the exact boundary");
            TestAssert.Equal(attackId, frame.AttackTimeline.AttackId,
                $"{attack} Recovery preserves its Telegraph identity");
            TestAssert.Equal(lockedTarget, frame.AttackTimeline.LockedWorldTarget,
                $"{attack} locked target does not chase during Recovery");
            frame = fixture.Model.Step(Input(true), timing.RecoverySeconds);
            TestAssert.True(!frame.AttackActive, $"{attack} recovery never damages");
        }
    }

    private static void ExactSixtyPercentTriggersOneNonDamagingTransition()
    {
        Fixture fixture = NewFixture();
        EnterPhaseOne(fixture);
        FallenBellkeeperFrame above = fixture.Model.Step(Input(true, .6001f), 0d);
        TestAssert.Equal(FallenBellkeeperState.PhaseOne, above.State,
            "health above sixty percent stays in phase one");
        FallenBellkeeperFrame transition = fixture.Model.Step(Input(true, .60f), 0d);
        TestAssert.Equal(FallenBellkeeperState.Transition, transition.State,
            "health exactly at sixty percent enters transition");
        TestAssert.True(transition.EnteredState && !transition.AttackActive &&
            transition.Attack == FallenBellkeeperAttack.None,
            "transition entry cancels every damage window");
        FallenBellkeeperFrame during = fixture.Model.Step(Input(true, .40f),
            fixture.Settings.TransitionSeconds - .001f);
        TestAssert.Equal(FallenBellkeeperState.Transition, during.State,
            "transition holds for its configured duration");
        TestAssert.True(!during.AttackActive, "transition never attacks");
        FallenBellkeeperFrame phaseTwo = fixture.Model.Step(Input(true, .40f), .001f);
        TestAssert.Equal(FallenBellkeeperState.PhaseTwoArmored, phaseTwo.State,
            "transition completes once into armored phase two");
        TestAssert.True(phaseTwo.ArmorClosed, "phase two begins armored");
    }

    private static void PhaseTwoSchedulesHammerAndOnlyThrownChainAcceptsThread()
    {
        Fixture fixture = NewFixture();
        FallenBellkeeperFrame armored = EnterPhaseTwo(fixture);
        TestAssert.Equal(FallenBellkeeperState.PhaseTwoArmored, armored.State,
            "phase two enters armored scheduling state");
        FallenBellkeeperFrame ignored = fixture.Model.Step(Input(true, .40f, true), 0d);
        TestAssert.Equal(FallenBellkeeperState.PhaseTwoArmored, ignored.State,
            "thread is ignored before the hammer is thrown");

        FallenBellkeeperFrame beforeTelegraph = fixture.Model.Step(Input(true, .40f), .649f);
        TestAssert.Equal(FallenBellkeeperState.PhaseTwoArmored, beforeTelegraph.State,
            "phase-two armored dwell remains active immediately before 0.65 seconds");
        FallenBellkeeperFrame telegraph = fixture.Model.Step(Input(true, .40f), .001f);
        TestAssert.Equal(FallenBellkeeperState.HammerTelegraph, telegraph.State,
            "phase-two armored dwell schedules hammer telegraph exactly at 0.65 seconds");
        TestAssert.Equal(FallenBellkeeperAttack.HammerThrow, telegraph.Attack,
            "phase-two scheduled attack is HammerThrow");
        TestAssert.True(!telegraph.AttackActive && !telegraph.ChainTargetActive,
            "hammer telegraph cannot damage or accept thread");
        TestAssert.Equal(FallenBellkeeperState.HammerTelegraph,
            fixture.Model.Step(Input(true, .40f, true), 0d).State,
            "thread is ignored during hammer telegraph");

        FallenBellkeeperFrame thrown = fixture.Model.Step(Input(true, .40f),
            fixture.Settings.TimingFor(FallenBellkeeperAttack.HammerThrow).TelegraphSeconds);
        TestAssert.Equal(FallenBellkeeperState.HammerThrown, thrown.State,
            "hammer telegraph enters thrown window at endpoint");
        TestAssert.True(thrown.AttackActive && thrown.ChainTargetActive,
            "thrown hammer is both dangerous and the active chain target");
        TestAssert.Equal(AttackPhase.Active, thrown.AttackTimeline.Phase,
            "thrown hammer publishes the Active flight phase");
        TestAssert.Equal(telegraph.AttackTimeline.AttackId, thrown.AttackTimeline.AttackId,
            "hammer flight preserves the Telegraph attack identity");
        TestAssert.Equal(EnemyAttackKind.BossHammerThrow, thrown.AttackTimeline.Kind,
            "hammer telegraph and flight use the unique BossHammerThrow kind");
        FallenBellkeeperFrame exposed = fixture.Model.Step(Input(true, .40f, true), 0d);
        TestAssert.Equal(FallenBellkeeperState.Exposed, exposed.State,
            "successful chain thread opens exposed state");
        TestAssert.True(!exposed.AttackActive && !exposed.ArmorClosed && !exposed.ChainTargetActive,
            "exposure cancels hammer damage and opens armor");
    }

    private static void ExposureExpiryReclosesArmorAtExactEndpoint()
    {
        Fixture fixture = NewFixture();
        EnterExposed(fixture);
        FallenBellkeeperFrame before = fixture.Model.Step(Input(true, .40f),
            fixture.Settings.ExposedSeconds - .001f);
        TestAssert.Equal(FallenBellkeeperState.Exposed, before.State,
            "exposure remains open before endpoint");
        TestAssert.True(!before.ArmorClosed, "armor remains open during exposure");
        FallenBellkeeperFrame closed = fixture.Model.Step(Input(true, .40f), .001f);
        TestAssert.Equal(FallenBellkeeperState.PhaseTwoArmored, closed.State,
            "exposure endpoint returns to armored phase");
        TestAssert.True(closed.ArmorClosed, "armor recloses exactly at exposure endpoint");
    }

    private static void ZeroHealthDefeatsEveryLivingStateAndRemainsInert()
    {
        FallenBellkeeperState[] states =
        {
            FallenBellkeeperState.Dormant,
            FallenBellkeeperState.Intro,
            FallenBellkeeperState.PhaseOne,
            FallenBellkeeperState.Transition,
            FallenBellkeeperState.PhaseTwoArmored,
            FallenBellkeeperState.HammerTelegraph,
            FallenBellkeeperState.HammerThrown,
            FallenBellkeeperState.Exposed
        };
        foreach (FallenBellkeeperState state in states)
        {
            Fixture fixture = At(state);
            FallenBellkeeperFrame defeated = fixture.Model.Step(Input(true, 0f, true), 0d);
            TestAssert.Equal(FallenBellkeeperState.Defeated, defeated.State,
                $"zero health defeats Boss from {state}");
            TestAssert.True(!defeated.AttackActive && !defeated.ArmorClosed &&
                !defeated.ChainTargetActive && defeated.Attack == FallenBellkeeperAttack.None,
                $"defeated frame from {state} is fully inert");
            FallenBellkeeperFrame persistent = fixture.Model.Step(Input(true, 1f, true), 10d);
            TestAssert.Equal(FallenBellkeeperState.Defeated, persistent.State,
                $"defeated state from {state} is terminal");
            TestAssert.True(!persistent.AttackActive, $"terminal defeated frame from {state} never attacks");
        }
    }

    private static Fixture At(FallenBellkeeperState state)
    {
        Fixture fixture = NewFixture();
        if (state == FallenBellkeeperState.Dormant) return fixture;
        fixture.Model.Step(Input(true), 0d);
        if (state == FallenBellkeeperState.Intro) return fixture;
        EnterPhaseOneFromIntro(fixture);
        if (state == FallenBellkeeperState.PhaseOne) return fixture;
        fixture.Model.Step(Input(true, .60f), 0d);
        if (state == FallenBellkeeperState.Transition) return fixture;
        fixture.Model.Step(Input(true, .40f), fixture.Settings.TransitionSeconds);
        if (state == FallenBellkeeperState.PhaseTwoArmored) return fixture;
        fixture.Model.Step(Input(true, .40f), fixture.Settings.PhaseTwoArmoredSeconds);
        if (state == FallenBellkeeperState.HammerTelegraph) return fixture;
        fixture.Model.Step(Input(true, .40f),
            fixture.Settings.TimingFor(FallenBellkeeperAttack.HammerThrow).TelegraphSeconds);
        if (state == FallenBellkeeperState.HammerThrown) return fixture;
        fixture.Model.Step(Input(true, .40f, true), 0d);
        return fixture;
    }

    private static FallenBellkeeperFrame EnterPhaseOne(Fixture fixture)
    {
        fixture.Model.Step(Input(true), 0d);
        return EnterPhaseOneFromIntro(fixture);
    }

    private static FallenBellkeeperFrame EnterPhaseOneFromIntro(Fixture fixture) =>
        fixture.Model.Step(Input(true), fixture.Settings.IntroSeconds);

    private static FallenBellkeeperFrame EnterPhaseTwo(Fixture fixture)
    {
        EnterPhaseOne(fixture);
        fixture.Model.Step(Input(true, .60f), 0d);
        return fixture.Model.Step(Input(true, .40f), fixture.Settings.TransitionSeconds);
    }

    private static FallenBellkeeperFrame EnterExposed(Fixture fixture)
    {
        EnterPhaseTwo(fixture);
        fixture.Model.Step(Input(true, .40f), fixture.Settings.PhaseTwoArmoredSeconds);
        fixture.Model.Step(Input(true, .40f),
            fixture.Settings.TimingFor(FallenBellkeeperAttack.HammerThrow).TelegraphSeconds);
        return fixture.Model.Step(Input(true, .40f, true), 0d);
    }

    private static FallenBellkeeperInput Input(bool active, float health = 1f, bool thread = false) =>
        new(active, health, thread);

    private static Fixture NewFixture()
    {
        BossSettings settings = new BossTuning().ToSettings();
        return new Fixture(settings, new FallenBellkeeperModel(settings));
    }

    private sealed record Fixture(BossSettings Settings, FallenBellkeeperModel Model);
}
