using System;
using Knight.Combat;
using Knight.Enemies;

namespace Knight.Tests.Unit;

public static class RustShellModelTests
{
    public static void Run()
    {
        TuningRejectsInvalidRangesDurationsAndFirstLevelDamage();
        IdleRemainsStillOutsideDetectionRange();
        ChaseMovesTowardVisiblePlayer();
        AttackAlwaysPassesThroughTelegraphFirst();
        ApprovedRustShellTimingTableIsExact();
        AttackTimelineUsesInclusiveBoundariesAndOneStableIdentity();
        RecoverPreventsImmediateSecondAttack();
        ThreadPullCausesPointTwoFiveSecondStun();
        DeadOverridesEveryOtherState();
    }

    private static void IdleRemainsStillOutsideDetectionRange()
    {
        var model = new RustShellModel();
        var hiddenModel = new RustShellModel();

        RustShellFrame frame = model.Step(361f, true, false, false, 0.016d);
        RustShellFrame hidden = hiddenModel.Step(200f, false, false, false, 0.016d);

        TestAssert.Equal(RustShellState.Idle, frame.State, "player outside detection range leaves squire idle");
        TestAssert.Near(0f, frame.MoveDirection, 0.001f, "idle squire does not move");
        TestAssert.True(!frame.AttackActive, "idle outside detection range cannot attack");
        TestAssert.Equal(RustShellState.Idle, hidden.State, "player without line of sight leaves squire idle inside detection range");
        TestAssert.Near(0f, hidden.MoveDirection, 0.001f, "hidden player does not make idle squire move");
        TestAssert.True(!hidden.AttackActive, "idle without line of sight cannot attack");
    }

    private static void ChaseMovesTowardVisiblePlayer()
    {
        var model = new RustShellModel();
        var hiddenModel = new RustShellModel();
        var escapedModel = new RustShellModel();
        var detectionBoundaryModel = new RustShellModel();
        var attackBoundaryModel = new RustShellModel();

        RustShellFrame right = model.Step(200f, true, false, false, 0.016d);
        RustShellFrame left = model.Step(-200f, true, false, false, 0.016d);
        hiddenModel.Step(200f, true, false, false, 0.016d);
        RustShellFrame lostLineOfSight = hiddenModel.Step(200f, false, false, false, 0.016d);
        escapedModel.Step(200f, true, false, false, 0.016d);
        RustShellFrame escapedDetection = escapedModel.Step(361f, true, false, false, 0.016d);
        RustShellFrame detectionBoundary = detectionBoundaryModel.Step(360f, true, false, false, 0.016d);
        RustShellFrame attackBoundary = attackBoundaryModel.Step(70f, true, false, false, 0.016d);

        TestAssert.Equal(RustShellState.Chase, right.State, "visible player in detection range starts chase");
        TestAssert.Near(1f, right.MoveDirection, 0.001f, "chase moves toward player on right");
        TestAssert.True(!right.AttackActive, "chase cannot deal attack damage");
        TestAssert.Near(-1f, left.MoveDirection, 0.001f, "chase moves toward player on left");
        TestAssert.Equal(RustShellState.Idle, lostLineOfSight.State, "chase returns to idle when line of sight is lost");
        TestAssert.Near(0f, lostLineOfSight.MoveDirection, 0.001f, "lost line of sight stops chase movement");
        TestAssert.Equal(RustShellState.Idle, escapedDetection.State, "chase returns to idle beyond detection range");
        TestAssert.Near(0f, escapedDetection.MoveDirection, 0.001f, "leaving detection range stops chase movement");
        TestAssert.Equal(RustShellState.Chase, detectionBoundary.State, "detection range includes exactly 360 pixels");
        TestAssert.Near(1f, detectionBoundary.MoveDirection, 0.001f, "detection boundary still moves toward player");
        TestAssert.Equal(RustShellState.Telegraph, attackBoundary.State, "attack range includes exactly 70 pixels");
        TestAssert.True(!attackBoundary.AttackActive, "attack boundary still telegraphs before becoming active");
    }

    private static void AttackAlwaysPassesThroughTelegraphFirst()
    {
        var model = new RustShellModel();

        RustShellFrame start = model.Step(50f, true, false, false, 0d);
        RustShellFrame attack = model.Step(50f, true, false, false, 0.65d);

        TestAssert.Equal(RustShellState.Telegraph, start.State, "attack range first enters telegraph");
        TestAssert.True(!start.AttackActive, "telegraph cannot deal damage");
        TestAssert.Equal(RustShellState.Attack, attack.State, "telegraph completes into attack");
        TestAssert.True(attack.AttackActive, "attack state enables damage");
    }

    private static void ApprovedRustShellTimingTableIsExact()
    {
        var settings = new RustShellSettings();
        TestAssert.Near(.65f, settings.TelegraphSeconds, .000001f,
            "approved Rust-Shell thrust telegraph is exactly 0.65 seconds");
        TestAssert.Near(.14f, settings.AttackSeconds, .000001f,
            "approved Rust-Shell thrust active window is exactly 0.14 seconds");
        TestAssert.Near(.65f, settings.RecoverySeconds, .000001f,
            "approved Rust-Shell thrust recovery is exactly 0.65 seconds");
        TestAssert.Equal(1, settings.ContactDamage,
            "first-level Rust-Shell contact damage is exactly one segment");
    }

    private static void TuningRejectsInvalidRangesDurationsAndFirstLevelDamage()
    {
        var valid = new RustShellSettings();
        TestAssert.Equal(0, valid.Validate().Count,
            "default Rust-Shell settings satisfy the centralized validation contract");
        var cases = new (string Name, RustShellSettings Settings)[]
        {
            (nameof(RustShellSettings.DetectionRange), valid with { DetectionRange = -1f }),
            (nameof(RustShellSettings.DetectionRange), valid with { DetectionRange = float.NaN }),
            (nameof(RustShellSettings.AttackRange), valid with { AttackRange = -1f }),
            (nameof(RustShellSettings.AttackRange), valid with { AttackRange = float.PositiveInfinity }),
            (nameof(RustShellSettings.MoveSpeed), valid with { MoveSpeed = 0f }),
            (nameof(RustShellSettings.MoveSpeed), valid with { MoveSpeed = float.NaN }),
            (nameof(RustShellSettings.TelegraphSeconds), valid with { TelegraphSeconds = -1f }),
            (nameof(RustShellSettings.TelegraphSeconds), valid with { TelegraphSeconds = float.NaN }),
            (nameof(RustShellSettings.AttackSeconds), valid with { AttackSeconds = -1f }),
            (nameof(RustShellSettings.AttackSeconds), valid with { AttackSeconds = float.PositiveInfinity }),
            (nameof(RustShellSettings.RecoverySeconds), valid with { RecoverySeconds = -1f }),
            (nameof(RustShellSettings.RecoverySeconds), valid with { RecoverySeconds = float.NaN }),
            (nameof(RustShellSettings.ThreadStunSeconds), valid with { ThreadStunSeconds = -1f }),
            (nameof(RustShellSettings.ThreadStunSeconds), valid with { ThreadStunSeconds = float.NegativeInfinity }),
            (nameof(RustShellSettings.ContactDamage), valid with { ContactDamage = 2 })
        };

        foreach ((string name, RustShellSettings settings) in cases)
        {
            var errors = settings.Validate();
            TestAssert.Equal(1, errors.Count,
                $"invalid Rust-Shell {name} produces exactly one focused validation error");
            TestAssert.True(errors[0].Contains(name, StringComparison.Ordinal),
                $"invalid Rust-Shell {name} validation error names the rejected setting");
        }

        bool threw = false;
        try { _ = new RustShellModel(valid with { TelegraphSeconds = float.NaN }); }
        catch (ArgumentException) { threw = true; }
        TestAssert.True(threw,
            "Rust-Shell model constructor rejects settings that fail centralized validation");
    }

    private static void AttackTimelineUsesInclusiveBoundariesAndOneStableIdentity()
    {
        var model = new RustShellModel();
        RustShellFrame start = model.Step(50f, true, false, false, 0d);
        RustShellFrame beforeActive = model.Step(50f, true, false, false, .649d);
        RustShellFrame active = model.Step(50f, true, false, false, .001d);
        RustShellFrame beforeRecovery = model.Step(50f, true, false, false, .139d);
        RustShellFrame recovery = model.Step(50f, true, false, false, .001d);
        RustShellFrame beforeNext = model.Step(50f, true, false, false, .649d);
        RustShellFrame next = model.Step(50f, true, false, false, .001d);

        TestAssert.Equal(AttackPhase.Telegraph, start.AttackTimeline.Phase,
            "Rust-Shell telegraph publishes the safe preparation phase");
        TestAssert.Equal(EnemyAttackKind.RustShellThrust, start.AttackTimeline.Kind,
            "Rust-Shell publishes the unique thrust kind");
        TestAssert.Equal(1UL, start.AttackTimeline.AttackId,
            "first Rust-Shell telegraph allocates the first attack identity");
        TestAssert.Near(.65f, start.AttackTimeline.PhaseDurationSeconds, .000001f,
            "Rust-Shell telegraph publishes its exact duration");
        TestAssert.Near(.001f, beforeActive.AttackTimeline.PhaseRemainingSeconds, .00001f,
            "Rust-Shell telegraph remains inclusive immediately before its endpoint");
        TestAssert.Equal(AttackPhase.Active, active.AttackTimeline.Phase,
            "Rust-Shell becomes active exactly at 0.65 seconds");
        TestAssert.Equal(start.AttackTimeline.AttackId, active.AttackTimeline.AttackId,
            "Rust-Shell active preserves the telegraph attack identity");
        TestAssert.Near(.001f, beforeRecovery.AttackTimeline.PhaseRemainingSeconds, .00001f,
            "Rust-Shell active remains inclusive immediately before its endpoint");
        TestAssert.Equal(AttackPhase.Recovery, recovery.AttackTimeline.Phase,
            "Rust-Shell enters recovery exactly at 0.14 seconds");
        TestAssert.Equal(start.AttackTimeline.AttackId, recovery.AttackTimeline.AttackId,
            "Rust-Shell recovery preserves the telegraph attack identity");
        TestAssert.Equal(AttackPhase.Recovery, beforeNext.AttackTimeline.Phase,
            "Rust-Shell recovery holds immediately before its endpoint");
        TestAssert.Equal(AttackPhase.Telegraph, next.AttackTimeline.Phase,
            "Rust-Shell starts the next telegraph exactly at 0.65 seconds");
        TestAssert.Equal(2UL, next.AttackTimeline.AttackId,
            "only a new Rust-Shell telegraph allocates the next identity");
    }

    private static void RecoverPreventsImmediateSecondAttack()
    {
        var model = new RustShellModel();
        model.Step(50f, true, false, false, 0d);
        model.Step(50f, true, false, false, 0.65d);
        model.Step(50f, true, false, false, 0.14d);

        RustShellFrame stillRecovering = model.Step(50f, true, false, false, 0.649d);
        RustShellFrame nextTelegraph = model.Step(50f, true, false, false, 0.001d);

        TestAssert.Equal(RustShellState.Recover, stillRecovering.State, "recovery blocks an immediate repeat attack");
        TestAssert.Equal(RustShellState.Telegraph, nextTelegraph.State, "squire may telegraph only after recovery expires");
    }

    private static void ThreadPullCausesPointTwoFiveSecondStun()
    {
        var model = new RustShellModel();
        var hiddenModel = new RustShellModel();

        RustShellFrame start = model.Step(200f, true, true, false, 0d);
        RustShellFrame before = model.Step(200f, true, false, false, 0.249d);
        RustShellFrame after = model.Step(200f, true, false, false, 0.001d);
        RustShellFrame hiddenStart = hiddenModel.Step(200f, false, true, false, 0d);
        RustShellFrame hiddenAfter = hiddenModel.Step(200f, false, false, false, 0.25d);

        TestAssert.Equal(RustShellState.Stunned, start.State, "thread pull requests stun");
        TestAssert.True(!start.AttackActive, "stunned squire cannot attack");
        TestAssert.Equal(RustShellState.Stunned, before.State, "stun persists before 0.25 seconds");
        TestAssert.Equal(RustShellState.Chase, after.State, "stun returns to chase when player remains visible");
        TestAssert.Equal(RustShellState.Stunned, hiddenStart.State, "thread pull stuns even without player visibility");
        TestAssert.True(!hiddenStart.AttackActive, "hidden stunned squire cannot attack");
        TestAssert.Equal(RustShellState.Idle, hiddenAfter.State, "stun returns to idle when player is not visible");
        TestAssert.Near(0f, hiddenAfter.MoveDirection, 0.001f, "stun expiry without visibility remains still");
    }

    private static void DeadOverridesEveryOtherState()
    {
        var model = new RustShellModel();
        var telegraphModel = new RustShellModel();
        var attackModel = new RustShellModel();
        var recoverModel = new RustShellModel();

        RustShellFrame dead = model.Step(50f, true, true, true, 0d);
        RustShellFrame remainsDead = model.Step(200f, true, false, false, 1d);
        telegraphModel.Step(50f, true, false, false, 0d);
        RustShellFrame telegraphDeath = telegraphModel.Step(50f, true, false, true, 0d);
        attackModel.Step(50f, true, false, false, 0d);
        attackModel.Step(50f, true, false, false, 0.65d);
        RustShellFrame attackDeath = attackModel.Step(50f, true, false, true, 0d);
        recoverModel.Step(50f, true, false, false, 0d);
        recoverModel.Step(50f, true, false, false, 0.65d);
        recoverModel.Step(50f, true, false, false, 0.14d);
        RustShellFrame recoverDeath = recoverModel.Step(50f, true, false, true, 0d);

        TestAssert.Equal(RustShellState.Dead, dead.State, "dead overrides a simultaneous stun and attack opportunity");
        TestAssert.Near(0f, dead.MoveDirection, 0.001f, "dead squire does not move");
        TestAssert.True(!dead.AttackActive, "dead squire cannot attack");
        TestAssert.Equal(RustShellState.Dead, remainsDead.State, "dead is an absorbing state");
        TestAssert.Equal(RustShellState.Dead, telegraphDeath.State, "death immediately overrides telegraph");
        TestAssert.True(!telegraphDeath.AttackActive, "death during telegraph cannot activate attack");
        TestAssert.Equal(RustShellState.Dead, attackDeath.State, "death immediately overrides an active attack");
        TestAssert.True(!attackDeath.AttackActive, "death closes an active attack window immediately");
        TestAssert.Equal(RustShellState.Dead, recoverDeath.State, "death immediately overrides recovery");
        TestAssert.True(!recoverDeath.AttackActive, "death during recovery cannot attack");
    }
}
