using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Laborer;

namespace Knight.Tests.Unit;

public static class BellBackLaborerModelTests
{
    public static void Run()
    {
        DetectionApproachAndGuardUseLiteralRanges();
        FirstLevelLaborerTuningRejectsContactDamageOtherThanOne();
        ApprovedLaborerTimingAndDamageTableIsExact();
        AttackTimelineUsesInclusiveBoundariesAndOneStableIdentity();
        OneAttackIdentityRemovesAtMostOneHealthSegment();
        GuardCyclesThroughTelegraphAttackAndRecover();
        ThreadOpensConfiguredExposedWindowWithoutMovement();
        ExposedExpiryReturnsToGuard();
        DeathOverridesEveryLivingState();
    }

    private static void FirstLevelLaborerTuningRejectsContactDamageOtherThanOne()
    {
        var tuning = new BellBackLaborerTuning { ContactDamage = 2 };
        TestAssert.Equal(1, tuning.Validate().Count,
            "first-level Laborer tuning rejects positive contact damage above one");
    }

    private static void ApprovedLaborerTimingAndDamageTableIsExact()
    {
        var tuning = new BellBackLaborerTuning();
        TestAssert.Near(.75f, tuning.TelegraphSeconds, .000001f,
            "approved Bell-Back Laborer sweep telegraph is exactly 0.75 seconds");
        TestAssert.Near(.20f, tuning.AttackSeconds, .000001f,
            "approved Bell-Back Laborer sweep active window is exactly 0.20 seconds");
        TestAssert.Near(.70f, tuning.RecoverySeconds, .000001f,
            "approved Bell-Back Laborer recovery is exactly 0.70 seconds");
        TestAssert.Equal(1, tuning.ContactDamage,
            "first-level Bell-Back Laborer contact damage is exactly one segment");
        TestAssert.Equal(0, tuning.Validate().Count,
            "authored Bell-Back Laborer tuning defaults are valid");
    }

    private static void AttackTimelineUsesInclusiveBoundariesAndOneStableIdentity()
    {
        BellBackLaborerModel model = Guarding();
        BellBackLaborerFrame telegraph = model.Step(-70f, true, false, false, .40);
        BellBackLaborerFrame beforeActive = model.Step(70f, true, false, false, .749);
        BellBackLaborerFrame active = model.Step(70f, true, false, false, .001);
        BellBackLaborerFrame beforeRecovery = model.Step(-70f, true, false, false, .199);
        BellBackLaborerFrame recovery = model.Step(-70f, true, false, false, .001);
        BellBackLaborerFrame beforeNext = model.Step(70f, true, false, false, .699);
        BellBackLaborerFrame guard = model.Step(70f, true, false, false, .001);
        BellBackLaborerFrame next = model.Step(70f, true, false, false, .40);

        TestAssert.Equal(AttackPhase.Telegraph, telegraph.AttackTimeline.Phase,
            "Laborer guard dwell enters a readable Telegraph");
        TestAssert.Equal(EnemyAttackKind.LaborerSweep, telegraph.AttackTimeline.Kind,
            "Laborer publishes the unique sweep kind");
        TestAssert.Equal(1UL, telegraph.AttackTimeline.AttackId,
            "first Laborer telegraph allocates one attack identity");
        TestAssert.Near(.001f, beforeActive.AttackTimeline.PhaseRemainingSeconds, .00001f,
            "Laborer Telegraph holds immediately before its inclusive endpoint");
        TestAssert.Equal(AttackPhase.Active, active.AttackTimeline.Phase,
            "Laborer sweep activates exactly at 0.75 seconds");
        TestAssert.Equal(telegraph.AttackTimeline.AttackId, active.AttackTimeline.AttackId,
            "Laborer Active preserves its Telegraph identity");
        TestAssert.True(active.AttackTimeline.HasLockedTarget,
            "Laborer locks sweep facing at the Telegraph-to-Active boundary");
        TestAssert.True(active.AttackTimeline.LockedWorldTarget.X > 0f,
            "Laborer locked target preserves the launch-facing side");
        TestAssert.Equal(active.AttackTimeline.LockedWorldTarget, beforeRecovery.AttackTimeline.LockedWorldTarget,
            "Laborer sweep facing cannot flip while Active");
        TestAssert.Equal(AttackPhase.Recovery, recovery.AttackTimeline.Phase,
            "Laborer sweep enters Recovery exactly at 0.20 seconds");
        TestAssert.Equal(active.AttackTimeline.AttackId, recovery.AttackTimeline.AttackId,
            "Laborer Recovery preserves the same attack identity");
        TestAssert.Equal(AttackPhase.Recovery, beforeNext.AttackTimeline.Phase,
            "Laborer Recovery holds immediately before its endpoint");
        TestAssert.Equal(BellBackLaborerState.Guard, guard.State,
            "Laborer Recovery ends exactly at 0.70 seconds");
        TestAssert.Equal(AttackPhase.Telegraph, next.AttackTimeline.Phase,
            "Laborer next cycle begins with a new Telegraph");
        TestAssert.Equal(2UL, next.AttackTimeline.AttackId,
            "only the next Laborer Telegraph allocates a new identity");
    }

    private static void OneAttackIdentityRemovesAtMostOneHealthSegment()
    {
        var tuning = new BellBackLaborerTuning();
        var health = new HealthComponent { MaxHealth = 5, InvulnerabilitySeconds = 0f };
        var request = new DamageRequest(tuning.ContactDamage, Vector2.Right, 0f, 4001UL);

        bool first = health.ApplyDamage(request);
        bool duplicate = health.ApplyDamage(request);

        TestAssert.True(first && !duplicate,
            "one attack identity is accepted only once by one hurtbox receiver");
        TestAssert.Equal(4, health.CurrentHealth,
            "one first-level attack identity removes exactly one health segment");
        health.Free();
    }

    private static void DetectionApproachAndGuardUseLiteralRanges()
    {
        var model = new BellBackLaborerModel();
        BellBackLaborerFrame idle = model.Step(421f, true, false, false, 1f / 60f);
        TestAssert.Equal(BellBackLaborerState.Idle, idle.State, "421 px remains outside detection");
        BellBackLaborerFrame approach = model.Step(420f, true, false, false, 1f / 60f);
        TestAssert.Equal(BellBackLaborerState.Approach, approach.State, "420 px enters approach inclusively");
        TestAssert.Equal(1f, approach.MoveDirection, "positive player offset approaches right");
        BellBackLaborerFrame guard = model.Step(90f, true, false, false, 1f / 60f);
        TestAssert.Equal(BellBackLaborerState.Guard, guard.State, "90 px enters guard inclusively");
        TestAssert.True(guard.Guarded, "guard state routes front damage through armor");
    }

    private static void GuardCyclesThroughTelegraphAttackAndRecover()
    {
        BellBackLaborerModel model = Guarding();
        BellBackLaborerFrame telegraph = model.Step(70f, true, false, false, .40);
        TestAssert.Equal(BellBackLaborerState.Telegraph, telegraph.State, "guard dwell ends in telegraph");
        TestAssert.True(!telegraph.AttackActive, "telegraph cannot deal damage");
        BellBackLaborerFrame attack = model.Step(70f, true, false, false, .75);
        TestAssert.Equal(BellBackLaborerState.Attack, attack.State, "telegraph duration enters attack");
        TestAssert.True(attack.AttackActive, "attack state enables active frames");
        BellBackLaborerFrame recover = model.Step(70f, true, false, false, .20);
        TestAssert.Equal(BellBackLaborerState.Recover, recover.State, "attack duration closes into recovery");
        TestAssert.True(!recover.AttackActive, "recovery closes damage window");
        BellBackLaborerFrame guard = model.Step(70f, true, false, false, .70);
        TestAssert.Equal(BellBackLaborerState.Guard, guard.State, "recovery returns to guard");
    }

    private static void ThreadOpensConfiguredExposedWindowWithoutMovement()
    {
        BellBackLaborerModel model = Guarding();
        BellBackLaborerFrame exposed = model.Step(70f, true, true, false, 1f / 60f);
        TestAssert.Equal(BellBackLaborerState.Exposed, exposed.State, "thread opens exposed weak point");
        TestAssert.True(!exposed.Guarded, "exposed hits bypass armor accumulator");
        TestAssert.Equal(0f, exposed.MoveDirection, "heavy enemy receives no pull movement while exposed");
        TestAssert.True(!exposed.AttackActive, "thread interruption closes any attack window");
    }

    private static void ExposedExpiryReturnsToGuard()
    {
        BellBackLaborerModel model = Guarding();
        model.Step(70f, true, true, false, 0f);
        TestAssert.Equal(BellBackLaborerState.Exposed,
            model.Step(70f, true, false, false, 1.19).State, "exposure remains open before configured endpoint");
        TestAssert.Equal(BellBackLaborerState.Guard,
            model.Step(70f, true, false, false, .01).State, "exposure closes exactly at 1.20 seconds");
    }

    private static void DeathOverridesEveryLivingState()
    {
        var states = new[]
        {
            BellBackLaborerState.Idle, BellBackLaborerState.Approach, BellBackLaborerState.Guard,
            BellBackLaborerState.Telegraph, BellBackLaborerState.Attack,
            BellBackLaborerState.Exposed, BellBackLaborerState.Recover
        };
        foreach (BellBackLaborerState state in states)
        {
            BellBackLaborerModel model = At(state);
            BellBackLaborerFrame dead = model.Step(0f, true, false, true, 0f);
            TestAssert.Equal(BellBackLaborerState.Dead, dead.State, $"death overrides {state}");
            TestAssert.True(!dead.AttackActive && !dead.Guarded, $"dead frame from {state} is inert");
        }
    }

    private static BellBackLaborerModel Guarding()
    {
        var model = new BellBackLaborerModel();
        model.Step(200f, true, false, false, 0f);
        model.Step(70f, true, false, false, 0f);
        return model;
    }

    private static BellBackLaborerModel At(BellBackLaborerState state)
    {
        var model = new BellBackLaborerModel();
        if (state == BellBackLaborerState.Idle) return model;
        model.Step(200f, true, false, false, 0f);
        if (state == BellBackLaborerState.Approach) return model;
        model.Step(70f, true, false, false, 0f);
        if (state == BellBackLaborerState.Guard) return model;
        model.Step(70f, true, false, false, .40);
        if (state == BellBackLaborerState.Telegraph) return model;
        model.Step(70f, true, false, false, .75);
        if (state == BellBackLaborerState.Attack) return model;
        if (state == BellBackLaborerState.Exposed)
        {
            model.Step(70f, true, true, false, 0f);
            return model;
        }
        model.Step(70f, true, false, false, .20);
        return model;
    }
}
