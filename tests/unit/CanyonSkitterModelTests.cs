using Godot;
using Knight.Combat;
using Knight.Enemies.Skitter;

namespace Knight.Tests.Unit;

public static class CanyonSkitterModelTests
{
    public static void Run()
    {
        FullWarningLocksHalfwayAndCrossingCancels();
        RushRetreatAndStationaryPunishAreBounded();
        InvalidAimStepAndSettingsCannotEnableDamage();
        ThreadAndDeathWinOverAttackEdge();
    }

    private static CanyonSkitterInput Input(float targetX = 100f, bool visible = true,
        bool compatible = true, bool supported = true, bool thread = false, bool dead = false) =>
        new(new Vector2(0f, 100f), new Vector2(targetX, 100f), visible, compatible,
            supported, thread, dead);

    private static void FullWarningLocksHalfwayAndCrossingCancels()
    {
        var model = new CanyonSkitterModel();
        CanyonSkitterFrame start = model.Step(Input(), 0d);
        TestAssert.Equal(CanyonSkitterState.Telegraph, start.State, "qualified target starts warning");
        TestAssert.Equal(AttackPhase.Telegraph, start.AttackTimeline.Phase,
            "entry cannot skip the non-damaging warning");
        TestAssert.True(!start.DamageActive, "warning never enables damage");
        CanyonSkitterFrame beforeLock = model.Step(Input(-90f), .324d);
        TestAssert.True(!beforeLock.AttackTimeline.HasLockedTarget && beforeLock.Side == -1,
            "face a crossing target before half-time lock");
        CanyonSkitterFrame locked = model.Step(Input(90f), .001d);
        TestAssert.True(locked.AttackTimeline.HasLockedTarget && locked.Side == 1,
            "side freezes at exactly half of 0.65 seconds");
        CanyonSkitterFrame cancelled = model.Step(Input(-90f), .325d);
        TestAssert.True(cancelled.State != CanyonSkitterState.Rush && !cancelled.DamageActive,
            "crossing behind the locked side cancels the rush, not a reverse hit");
        TestAssert.Equal(AttackPhase.None, cancelled.AttackTimeline.Phase,
            "cancelled attack identity does not linger");
    }

    private static void RushRetreatAndStationaryPunishAreBounded()
    {
        var model = new CanyonSkitterModel();
        model.Step(Input(), 0d);
        model.Step(Input(), .65d);
        CanyonSkitterFrame rush = model.Step(Input(), 0d);
        TestAssert.Equal(CanyonSkitterState.Rush, rush.State, "full warning enters a rush");
        TestAssert.True(rush.DamageActive && rush.AttackTimeline.AttackId != 0,
            "rush alone owns one nonzero attack identity");
        TestAssert.True(rush.PlannedRushDistance <= 64f && rush.PlannedRushDistance > 0f,
            "planned travel is capped at 64 pixels");
        ulong identity = rush.AttackTimeline.AttackId;
        TestAssert.Equal(identity, model.Step(Input(), .10d).AttackTimeline.AttackId,
            "rapid active frames retain one attack identity");
        CanyonSkitterFrame retreat = model.Step(Input(), .10d);
        TestAssert.Equal(CanyonSkitterState.Retreat, retreat.State,
            "even a miss proceeds to retreat after 0.20 seconds");
        TestAssert.True(!retreat.DamageActive && retreat.RetreatDistance <= 48f,
            "48-pixel retreat is entirely non-damaging");
        CanyonSkitterFrame recover = model.Step(Input(), .30d);
        TestAssert.Equal(CanyonSkitterState.Recover, recover.State,
            "retreat always yields a stationary punish window");
        TestAssert.True(!recover.DamageActive && recover.MoveSpeed == 0f,
            "recovery cannot chase or attack");
        TestAssert.Equal(CanyonSkitterState.Recover, model.Step(Input(), .59d).State,
            "the 0.60-second recovery is not shortened");
        TestAssert.Equal(CanyonSkitterState.Idle, model.Step(Input(), .01d).State,
            "full recovery returns to waiting instead of an immediate attack");
    }

    private static void InvalidAimStepAndSettingsCannotEnableDamage()
    {
        var model = new CanyonSkitterModel();
        TestAssert.Equal(CanyonSkitterState.Idle,
            model.Step(new CanyonSkitterInput(Vector2.Zero,
                new Vector2(float.NaN, 0f), true, true, true, false, false), 0d).State,
            "non-finite aim cannot start a threat");
        TestAssert.Equal(CanyonSkitterState.Idle, model.Step(Input(0f), 0d).State,
            "overlapping target has no valid rush side");
        model.Step(Input(), 0d);
        TestAssert.True(!model.Step(Input(), double.PositiveInfinity).DamageActive,
            "invalid time cannot skip warning");
        TestAssert.True(new CanyonSkitterSettings { RushSpeed = float.PositiveInfinity }.Validate().Count > 0,
            "non-finite movement tuning is rejected");
    }

    private static void ThreadAndDeathWinOverAttackEdge()
    {
        foreach (double elapsed in new[] { 0d, .65d, .80d, 1.10d })
        {
            var model = new CanyonSkitterModel();
            model.Step(Input(), 0d);
            model.Step(Input(), elapsed);
            CanyonSkitterFrame stun = model.Step(Input(thread: true), 0d);
            TestAssert.Equal(CanyonSkitterState.Stunned, stun.State,
                "thread cancels any attack phase immediately");
            TestAssert.True(!stun.DamageActive && stun.AttackTimeline.Phase == AttackPhase.None,
                "stunned actor has no damage or typed attack identity");
        }
        var dying = new CanyonSkitterModel();
        dying.Step(Input(), 0d);
        CanyonSkitterFrame dead = dying.Step(Input(dead: true), .65d);
        TestAssert.Equal(CanyonSkitterState.Dead, dead.State,
            "death takes priority at the exact rush edge");
        TestAssert.True(!dead.DamageActive && dead.AttackTimeline.Phase == AttackPhase.None,
            "death cannot leave a late hit or cue");
    }
}
