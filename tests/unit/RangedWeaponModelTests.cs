using System;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;

namespace Knight.Tests.Unit;

public static class RangedWeaponModelTests
{
    public static void Run()
    {
        EachRolePublishesItsWarningBeforeItsOwnInclusiveFireBoundary();
        EachRoleKeepsItsOwnActiveAndRecoveryWindows();
        BurstEmitsAtZeroPointEighteenAndPointThirtySixOnly();
        CrossingBothRemainingBurstPointsDoesNotDropOrRepeatShots();
        ScatterMissileAndLaserEmitOneMuzzleEventNotOnePerPelletOrActiveTick();
        SightLossBeforeLockCancelsAndClearsItsIdentity();
        MovingTargetIsFollowedBeforeLockButNotAfterLock();
        LosingSightAfterLockDoesNotRetargetOrCancelTheCommittedShot();
        DetectionRejectsInvisibleFarCoincidentAndNonFiniteTargets();
        ZeroNegativeAndNonFiniteDeltaCannotAdvanceTheWarningOrRepeatEmission();
        LargeDeltaCannotSkipTheFirstPublishedWarningOrRepeatACompletedBurst();
        StunAtTheFireBoundaryCancelsEveryPendingEmission();
        StunDuringBurstCancelsItsUnfiredTailAndExpiresAtPointEightSeconds();
        DeathOverridesStunAndEveryLivingPhaseAndIsAbsorbing();
        CancelStunAndRecoveryNeverReuseAnAttackIdentity();
        InvalidSettingsCannotEnterTheModel();
        SixtyFiveScatterPelletsCannotEnterTheModel();
        NineBurstEventsCannotEnterTheModel();
        EditingTheResourceCannotChangeTheValidatedModelConfiguration();
    }

    // Mutation target: early fire, off-by-one phase boundaries, or using one role's timing for all roles.
    private static void EachRolePublishesItsWarningBeforeItsOwnInclusiveFireBoundary()
    {
        (RangedWeaponRole role, double beforeLock, double beforeFire, EnemyAttackKind kind)[] cases =
        [
            (RangedWeaponRole.Scatter, .549d, .549d, EnemyAttackKind.RivetScatterShot),
            (RangedWeaponRole.Burst, .549d, .549d, EnemyAttackKind.BeltBurstShot),
            (RangedWeaponRole.Missile, .699d, .599d, EnemyAttackKind.FurnaceMissileShot),
            (RangedWeaponRole.Laser, .699d, .549d, EnemyAttackKind.PrismLaserBeam)
        ];
        foreach (var test in cases)
        {
            var model = Model(test.role);
            RangedWeaponFrame start = model.Step(Input(), 0d);
            TestAssert.Equal(RangedWeaponState.Telegraph, start.State, $"{test.role} starts with a visible warning");
            TestAssert.Equal(AttackPhase.Telegraph, start.AttackTimeline.Phase, $"{test.role} warning is non-active");
            TestAssert.Equal(test.kind, start.AttackTimeline.Kind, $"{test.role} exposes its typed attack kind");
            TestAssert.Equal(1UL, start.AttackTimeline.AttackId, $"{test.role} first warning owns attack one");
            NoEmission(start, $"{test.role} warning entry");
            RangedWeaponFrame beforeLock = model.Step(Input(), test.beforeLock);
            TestAssert.True(!beforeLock.AttackTimeline.HasLockedTarget, $"{test.role} aim remains free immediately before lock");
            RangedWeaponFrame locked = model.Step(Input(), .001d);
            TestAssert.True(locked.AttackTimeline.HasLockedTarget, $"{test.role} locks at its inclusive lock boundary");
            RangedWeaponFrame beforeFire = model.Step(Input(), test.beforeFire);
            TestAssert.Equal(RangedWeaponState.Telegraph, beforeFire.State, $"{test.role} remains safe one millisecond before fire");
            NoEmission(beforeFire, $"{test.role} pre-fire warning");
            RangedWeaponFrame active = model.Step(Input(), .001d);
            TestAssert.Equal(RangedWeaponState.Active, active.State, $"{test.role} fires at its inclusive telegraph endpoint");
            TestAssert.Equal(AttackPhase.Active, active.AttackTimeline.Phase, $"{test.role} publishes active timeline on fire");
            TestAssert.Equal(1UL, active.AttackTimeline.AttackId, $"{test.role} fire preserves warning identity");
            TestAssert.Equal(0, active.FirstEmissionIndex, $"{test.role} first muzzle event is zero-based");
            TestAssert.Equal(1, active.EmissionsDue, $"{test.role} emits exactly one event at active time zero");
        }
    }

    // Mutation target: opening another shot before active/recovery has really elapsed.
    private static void EachRoleKeepsItsOwnActiveAndRecoveryWindows()
    {
        (RangedWeaponRole role, double beforeActiveEnd, double beforeRecoveryEnd)[] cases =
        [
            (RangedWeaponRole.Scatter, .119d, 1.599d),
            (RangedWeaponRole.Burst, .419d, 1.199d),
            (RangedWeaponRole.Missile, .119d, 1.599d),
            (RangedWeaponRole.Laser, .279d, 1.399d)
        ];
        foreach (var test in cases)
        {
            RangedWeaponModel model = ActiveModel(test.role);
            RangedWeaponFrame before = model.Step(Input(), test.beforeActiveEnd);
            TestAssert.Equal(RangedWeaponState.Active, before.State, $"{test.role} active remains open before its endpoint");
            RangedWeaponFrame recovery = model.Step(Input(), .001d);
            TestAssert.Equal(RangedWeaponState.Recovery, recovery.State, $"{test.role} closes active exactly on time");
            TestAssert.Equal(AttackPhase.Recovery, recovery.AttackTimeline.Phase, $"{test.role} publishes recovery instead of a damage window");
            TestAssert.Equal(1UL, recovery.AttackTimeline.AttackId, $"{test.role} recovery belongs to the existing attack");
            NoEmission(recovery, $"{test.role} recovery entry");
            TestAssert.Equal(RangedWeaponState.Recovery, model.Step(Input(), test.beforeRecoveryEnd).State,
                $"{test.role} cannot rearm before its full recovery");
            RangedWeaponFrame idle = model.Step(Input(), .001d);
            TestAssert.Equal(RangedWeaponState.Idle, idle.State, $"{test.role} ends recovery exactly on time");
            TestAssert.Equal(AttackPhase.None, idle.AttackTimeline.Phase, $"{test.role} completed recovery clears the attack");
            NoEmission(idle, $"{test.role} recovered idle");
        }
    }

    // Mutation target: repeated t0 events, early burst events, or an off-by-one emission index.
    private static void BurstEmitsAtZeroPointEighteenAndPointThirtySixOnly()
    {
        RangedWeaponModel model = ActiveModel(RangedWeaponRole.Burst);
        NoEmission(model.Step(Input(), 0d), "paused burst after first event");
        NoEmission(model.Step(Input(), .179d), "burst before second event");
        RangedWeaponFrame second = model.Step(Input(), .001d);
        TestAssert.Equal(1, second.FirstEmissionIndex, "burst at 0.18 emits event one");
        TestAssert.Equal(1, second.EmissionsDue, "burst at 0.18 emits once");
        NoEmission(model.Step(Input(), 0d), "burst second event cannot replay");
        NoEmission(model.Step(Input(), .179d), "burst before third event");
        RangedWeaponFrame third = model.Step(Input(), .001d);
        TestAssert.Equal(2, third.FirstEmissionIndex, "burst at 0.36 emits event two");
        TestAssert.Equal(1, third.EmissionsDue, "burst at 0.36 emits once");
        NoEmission(model.Step(Input(), .059d), "burst after its last event");
        NoEmission(model.Step(Input(), .001d), "burst active end cannot emit a fourth event");
    }

    // Mutation target: equality-only scheduling that loses events between simulation ticks.
    private static void CrossingBothRemainingBurstPointsDoesNotDropOrRepeatShots()
    {
        RangedWeaponModel model = ActiveModel(RangedWeaponRole.Burst);
        RangedWeaponFrame caughtUp = model.Step(Input(), .37d);
        TestAssert.Equal(RangedWeaponState.Active, caughtUp.State, "0.37 is still inside the 0.42 burst window");
        TestAssert.Equal(1, caughtUp.FirstEmissionIndex, "catch-up starts with the first unseen burst event");
        TestAssert.Equal(2, caughtUp.EmissionsDue, "one crossing step catches both 0.18 and 0.36 events");
        TestAssert.Equal(1UL, caughtUp.AttackTimeline.AttackId, "catch-up events retain their volley identity");
        NoEmission(model.Step(Input(), 0d), "a paused catch-up cannot repeat either event");
        NoEmission(model.Step(Input(), .05d), "the burst endpoint cannot repeat caught-up events");
    }

    // Mutation target: counting scatter pellets or every laser tick as new muzzle emissions.
    private static void ScatterMissileAndLaserEmitOneMuzzleEventNotOnePerPelletOrActiveTick()
    {
        foreach (RangedWeaponRole role in new[] { RangedWeaponRole.Scatter, RangedWeaponRole.Missile, RangedWeaponRole.Laser })
        {
            var model = Model(role);
            model.Step(Input(), 0d);
            RangedWeaponFrame fired = model.Step(Input(), 2d);
            TestAssert.Equal(1, fired.EmissionsDue, $"{role} has a single muzzle event for the whole activation");
            TestAssert.Equal(0, fired.FirstEmissionIndex, $"{role} activation starts at event zero");
            for (int index = 0; index < 5; index++) NoEmission(model.Step(Input(), .01d), $"{role} active tick {index}");
        }
    }

    // Mutation target: stale queued fire or retained presentation after losing pre-lock visibility.
    private static void SightLossBeforeLockCancelsAndClearsItsIdentity()
    {
        var model = Model(RangedWeaponRole.Burst);
        model.Step(Input(), 0d);
        model.Step(Input(), .30d);
        RangedWeaponFrame cancelled = model.Step(Input(visible: false), 100d);
        TestAssert.Equal(RangedWeaponState.Idle, cancelled.State, "pre-lock sight loss cancels even on a long frame");
        TestAssert.Equal(AttackPhase.None, cancelled.AttackTimeline.Phase, "cancel removes the warning timeline");
        TestAssert.Equal(0UL, cancelled.AttackTimeline.AttackId, "cancel clears the in-flight attack identity");
        TestAssert.Equal(Vector2.Zero, cancelled.AimDirection, "cancel removes stale aim");
        NoEmission(cancelled, "cancelled warning");
    }

    // Mutation target: locking on entry, following after lock, or computing range from world origin.
    private static void MovingTargetIsFollowedBeforeLockButNotAfterLock()
    {
        var model = Model(RangedWeaponRole.Scatter);
        model.Step(Input(), 0d);
        RangedWeaponFrame following = model.Step(Input(new Vector2(190f, 260f), muzzle: new Vector2(10f, 20f)), .54d);
        TestAssert.Near(.6f, following.AimDirection.X, .00001f, "pre-lock direction follows the 180/240 offset X");
        TestAssert.Near(.8f, following.AimDirection.Y, .00001f, "pre-lock direction follows the 180/240 offset Y");
        RangedWeaponFrame locked = model.Step(Input(new Vector2(190f, 260f), muzzle: new Vector2(10f, 20f)), .01d);
        TestAssert.Near(250f, locked.AttackTimeline.LockedWorldTarget.X, .0001f,
            "locked range endpoint X is 400 units from the real muzzle, not the world origin");
        TestAssert.Near(340f, locked.AttackTimeline.LockedWorldTarget.Y, .0001f,
            "locked range endpoint Y is 400 units from the real muzzle, not the world origin");
        RangedWeaponFrame fired = model.Step(Input(new Vector2(-200f, 20f), muzzle: new Vector2(10f, 20f)), .55d);
        TestAssert.Near(.6f, fired.AimDirection.X, .00001f, "after-lock target crossing cannot turn X aim");
        TestAssert.Near(.8f, fired.AimDirection.Y, .00001f, "after-lock target crossing cannot turn Y aim");
        TestAssert.Near(250f, fired.AttackTimeline.LockedWorldTarget.X, .0001f, "fire keeps the committed world endpoint X");
        TestAssert.Near(340f, fired.AttackTimeline.LockedWorldTarget.Y, .0001f, "fire keeps the committed world endpoint Y");
    }

    // Mutation target: cancelling or replacing an already committed direction when LOS changes.
    private static void LosingSightAfterLockDoesNotRetargetOrCancelTheCommittedShot()
    {
        var model = Model(RangedWeaponRole.Burst);
        model.Step(Input(), 0d);
        model.Step(Input(), .55d);
        RangedWeaponFrame fired = model.Step(Input(new Vector2(-300f, 0f), visible: false), .55d);
        TestAssert.Equal(RangedWeaponState.Active, fired.State, "post-lock LOS loss preserves the committed attack");
        TestAssert.Equal(Vector2.Right, fired.AimDirection, "post-lock invisible target cannot retarget the gun");
        TestAssert.Equal(1, fired.EmissionsDue, "committed burst still starts once");
        RangedWeaponFrame tail = model.Step(Input(new Vector2(float.NaN, 0f), visible: false), .36d);
        TestAssert.Equal(2, tail.EmissionsDue, "committed burst tail is independent of a later invalid target");
        TestAssert.Equal(Vector2.Right, tail.AimDirection, "invalid later target cannot poison locked aim");
    }

    // Mutation target: firing at invalid geometry, invisible targets, or out-of-range players.
    private static void DetectionRejectsInvisibleFarCoincidentAndNonFiniteTargets()
    {
        RangedWeaponInput[] rejected =
        [
            Input(visible: false), Input(new Vector2(360.01f, 0f)), Input(Vector2.Zero),
            Input(new Vector2(float.NaN, 0f)), Input(new Vector2(float.PositiveInfinity, 0f)),
            Input(muzzle: new Vector2(float.NaN, 0f))
        ];
        foreach (RangedWeaponInput input in rejected)
        {
            RangedWeaponFrame frame = Model(RangedWeaponRole.Scatter).Step(input, 100d);
            TestAssert.Equal(RangedWeaponState.Idle, frame.State, "invalid or unavailable target cannot begin warning");
            NoEmission(frame, "rejected target");
        }
        TestAssert.Equal(RangedWeaponState.Telegraph,
            Model(RangedWeaponRole.Scatter).Step(Input(new Vector2(360f, 0f)), 0d).State,
            "a target exactly at detection range remains eligible");
    }

    // Mutation target: treating pause/invalid elapsed values as progress or repeating the active edge.
    private static void ZeroNegativeAndNonFiniteDeltaCannotAdvanceTheWarningOrRepeatEmission()
    {
        var model = Model(RangedWeaponRole.Burst);
        model.Step(Input(), 0d);
        model.Step(Input(), .40d);
        foreach (double delta in new[] { 0d, -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MaxValue })
        {
            RangedWeaponFrame frame = model.Step(Input(), delta);
            TestAssert.Equal(RangedWeaponState.Telegraph, frame.State, "invalid elapsed time cannot advance warning");
            TestAssert.True(float.IsFinite(frame.AttackTimeline.PhaseRemainingSeconds), "invalid elapsed time cannot produce a non-finite remainder");
            TestAssert.Near(.70f, frame.AttackTimeline.PhaseRemainingSeconds, .00001f, "invalid elapsed time preserves warning remainder");
            NoEmission(frame, "warning with invalid elapsed time");
        }
        RangedWeaponFrame fired = model.Step(Input(), .70d);
        TestAssert.Equal(1, fired.EmissionsDue, "real elapsed time resumes exactly one first event");
        foreach (double delta in new[] { 0d, -1d, double.NaN, double.PositiveInfinity, double.MaxValue })
            NoEmission(model.Step(Input(), delta), "invalid active elapsed time");
    }

    // Mutation target: a catch-up loop that erases warning/identity or opens endless attack cycles on one hitch.
    // Readability contract: one Step advances at most the phase that was current on entry.
    // Phase-entry steps discard surplus delta; this is deliberately not cross-phase real-time equivalence.
    private static void LargeDeltaCannotSkipTheFirstPublishedWarningOrRepeatACompletedBurst()
    {
        var model = Model(RangedWeaponRole.Burst);
        RangedWeaponFrame warning = model.Step(Input(), 1000d);
        TestAssert.Equal(RangedWeaponState.Telegraph, warning.State, "large entry delta still publishes the first warning");
        NoEmission(warning, "large-delta warning entry");
        RangedWeaponFrame active = model.Step(Input(), 1000d);
        TestAssert.Equal(RangedWeaponState.Active, active.State, "a long warning frame enters active rather than hiding the attack");
        TestAssert.Equal(0, active.FirstEmissionIndex, "large-delta active entry begins at event zero");
        TestAssert.Equal(1, active.EmissionsDue, "large-delta active entry emits only the entry event");
        RangedWeaponFrame recovery = model.Step(Input(), 1000d);
        TestAssert.Equal(RangedWeaponState.Recovery, recovery.State, "large active delta ends the bounded burst");
        TestAssert.Equal(1, recovery.FirstEmissionIndex, "large active delta starts at the first unseen event");
        TestAssert.Equal(2, recovery.EmissionsDue, "large active delta catches up both remaining events once");
        TestAssert.Equal(1UL, recovery.AttackTimeline.AttackId, "caught-up tail still exposes its originating attack identity");
        RangedWeaponFrame idle = model.Step(Input(), 1000d);
        TestAssert.Equal(RangedWeaponState.Idle, idle.State, "one recovery step cannot start another attack");
        NoEmission(idle, "large-delta completed cycle");
    }

    // Mutation target: processing a due event before a same-frame interrupt.
    private static void StunAtTheFireBoundaryCancelsEveryPendingEmission()
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            var model = Model(role);
            model.Step(Input(), 0d);
            RangedWeaponFrame stunned = model.Step(Input(stun: true), 100d);
            TestAssert.Equal(RangedWeaponState.Stunned, stunned.State, $"{role} stun wins over a fire-ready warning");
            TestAssert.Equal(AttackPhase.None, stunned.AttackTimeline.Phase, $"{role} stun removes attack presentation");
            TestAssert.Equal(0UL, stunned.AttackTimeline.AttackId, $"{role} stun clears attack identity");
            NoEmission(stunned, $"{role} interrupted warning");
        }
    }

    // Mutation target: retained burst tail after stun or a shortened/restarted stun duration.
    private static void StunDuringBurstCancelsItsUnfiredTailAndExpiresAtPointEightSeconds()
    {
        RangedWeaponModel model = ActiveModel(RangedWeaponRole.Burst);
        RangedWeaponFrame stunned = model.Step(Input(stun: true), .36d);
        TestAssert.Equal(RangedWeaponState.Stunned, stunned.State, "stun interrupts before the two remaining burst points");
        NoEmission(stunned, "stunned burst tail");
        RangedWeaponFrame beforeEnd = model.Step(Input(), .799d);
        TestAssert.Equal(RangedWeaponState.Stunned, beforeEnd.State, "stun is still active at 0.799 seconds");
        NoEmission(beforeEnd, "stun before expiry");
        RangedWeaponFrame idle = model.Step(Input(), .001d);
        TestAssert.Equal(RangedWeaponState.Idle, idle.State, "stun expires exactly at 0.8 seconds");
        NoEmission(idle, "stun expiry cannot replay the discarded burst");
        RangedWeaponFrame next = model.Step(Input(), 0d);
        TestAssert.Equal(RangedWeaponState.Telegraph, next.State, "recovering from stun requires a new warning");
        TestAssert.Equal(2UL, next.AttackTimeline.AttackId, "a new post-stun warning receives a new identity");
    }

    // Mutation target: death losing to stun, retaining fire, or later returning a dead actor to combat.
    private static void DeathOverridesStunAndEveryLivingPhaseAndIsAbsorbing()
    {
        RangedWeaponModel[] models =
        [
            Model(RangedWeaponRole.Burst), TelegraphModel(RangedWeaponRole.Burst),
            ActiveModel(RangedWeaponRole.Burst), RecoveryModel(RangedWeaponRole.Burst),
            StunnedModel(RangedWeaponRole.Burst)
        ];
        foreach (RangedWeaponModel model in models)
        {
            RangedWeaponFrame dead = model.Step(Input(stun: true, dead: true), 100d);
            TestAssert.Equal(RangedWeaponState.Dead, dead.State, "death overrides simultaneous stun in every phase");
            TestAssert.Equal(AttackPhase.None, dead.AttackTimeline.Phase, "dead actors expose no attack window");
            TestAssert.Equal(Vector2.Zero, dead.AimDirection, "death clears stale aim");
            NoEmission(dead, "death interruption");
            RangedWeaponFrame stillDead = model.Step(Input(), 100d);
            TestAssert.Equal(RangedWeaponState.Dead, stillDead.State, "death is absorbing after valid target returns");
            NoEmission(stillDead, "absorbing death");
        }
    }

    // Mutation target: clearing the sequence counter alongside the current attack, reusing old damage identities.
    private static void CancelStunAndRecoveryNeverReuseAnAttackIdentity()
    {
        var model = Model(RangedWeaponRole.Scatter);
        TestAssert.Equal(1UL, model.Step(Input(), 0d).AttackTimeline.AttackId, "first warning is attack one");
        model.Step(Input(visible: false), 0d);
        TestAssert.Equal(2UL, model.Step(Input(), 0d).AttackTimeline.AttackId, "cancelled attack identity is never recycled");
        model.Step(Input(stun: true), 0d);
        model.Step(Input(), .80d);
        TestAssert.Equal(3UL, model.Step(Input(), 0d).AttackTimeline.AttackId, "stunned attack identity is never recycled");
        model.Step(Input(), 1.10d);
        model.Step(Input(), .12d);
        model.Step(Input(), 1.60d);
        RangedWeaponFrame fourth = model.Step(Input(), 0d);
        TestAssert.Equal(4UL, fourth.AttackTimeline.AttackId, "completed attack identity is never recycled");
        NoEmission(fourth, "new identity begins with a warning");
    }

    // Mutation target: invalid resources silently reaching timing/math/physics consumers.
    private static void InvalidSettingsCannotEnterTheModel()
    {
        RangedWeaponSettings[] invalid =
        [
            new() { Role = (RangedWeaponRole)99 }, new() { DetectionRange = 0f },
            new() { MaximumTravel = float.NaN }, new() { TelegraphSeconds = -1f },
            new() { DirectionLockSeconds = 1.10f }, new() { ActiveSeconds = 0f },
            new() { RecoverySeconds = float.PositiveInfinity }, new() { ThreadStunSeconds = 0f },
            new() { ProjectileSpeed = -1f }, new() { ProjectileRadius = 0f },
            new() { ScatterPelletCount = 4 }, new() { ScatterHalfAngleDegrees = 0f },
            new() { BurstEmissionCount = 0 }, new() { BurstIntervalSeconds = 0f },
            new() { ExplosionRadius = 0f }, new() { MissileGuidanceSeconds = -1f },
            new() { MissileTurnRateDegrees = 0f }, new() { MissileMaximumTurnDegrees = 181f },
            new() { MissileMaximumAgeSeconds = 0f }, new() { LaserHalfWidth = 0f },
            new() { MaxHealth = 0 }, new() { Damage = 0 }, new() { Gravity = float.NaN },
            new() { Role = RangedWeaponRole.Burst, ActiveSeconds = .35f }
        ];
        foreach (RangedWeaponSettings settings in invalid)
        {
            TestAssert.True(settings.Validate().Count > 0, "invalid ranged resource gives an actionable validation error");
            bool rejected = false;
            try { _ = new RangedWeaponModel(settings); }
            catch (ArgumentException) { rejected = true; }
            TestAssert.True(rejected, "invalid ranged resource is rejected before model simulation");
        }
        bool nullRejected = false;
        try { _ = new RangedWeaponModel(null); }
        catch (ArgumentNullException) { nullRejected = true; }
        TestAssert.True(nullRejected, "a missing ranged settings resource is rejected explicitly");
    }

    // Mutation target: an unbounded exported pellet count reaching the runtime's per-pellet work.
    private static void SixtyFiveScatterPelletsCannotEnterTheModel()
    {
        var settings = new RangedWeaponSettings { ScatterPelletCount = 65 };
        TestAssert.True(settings.Validate().Count > 0, "65 scatter pellets must be rejected before runtime work");
        bool rejected = false;
        try { _ = new RangedWeaponModel(settings); }
        catch (ArgumentException) { rejected = true; }
        TestAssert.True(rejected, "65 scatter pellets cannot enter a ranged model");
    }

    // Mutation target: an unbounded exported burst count reaching the runtime's emission-consumption loop.
    private static void NineBurstEventsCannotEnterTheModel()
    {
        RangedWeaponSettings settings = RangedWeaponSettings.ForRole(RangedWeaponRole.Burst);
        settings.BurstEmissionCount = 9;
        settings.BurstIntervalSeconds = .01f;
        TestAssert.True(settings.Validate().Count > 0, "nine burst events must be rejected even when all fit in the active window");
        bool rejected = false;
        try { _ = new RangedWeaponModel(settings); }
        catch (ArgumentException) { rejected = true; }
        TestAssert.True(rejected, "nine burst events cannot enter a ranged model");
    }

    // Mutation target: retaining the mutable Godot Resource and reading illegal edited values during a cycle.
    private static void EditingTheResourceCannotChangeTheValidatedModelConfiguration()
    {
        RangedWeaponSettings settings = RangedWeaponSettings.ForRole(RangedWeaponRole.Burst);
        var model = new RangedWeaponModel(settings);
        settings.Role = RangedWeaponRole.Scatter;
        settings.DetectionRange = 1f;
        settings.MaximumTravel = 1f;
        settings.TelegraphSeconds = float.NaN;
        settings.DirectionLockSeconds = 0f;
        settings.ActiveSeconds = 0f;
        settings.RecoverySeconds = 0f;
        settings.ThreadStunSeconds = 0f;
        settings.BurstEmissionCount = 0;
        settings.BurstIntervalSeconds = float.NaN;

        RangedWeaponFrame warning = model.Step(Input(), 0d);
        TestAssert.Equal(RangedWeaponState.Telegraph, warning.State, "post-construction range edits cannot erase an eligible target");
        TestAssert.Equal(EnemyAttackKind.BeltBurstShot, warning.AttackTimeline.Kind, "post-construction role edits cannot change attack identity");
        TestAssert.Near(1.10f, warning.AttackTimeline.PhaseRemainingSeconds, .00001f, "model warning uses its validated timing snapshot");
        RangedWeaponFrame locked = model.Step(Input(), .55d);
        TestAssert.Equal(new Vector2(560f, 0f), locked.AttackTimeline.LockedWorldTarget, "range endpoint uses the validated configuration snapshot");
        RangedWeaponFrame active = model.Step(Input(), .55d);
        TestAssert.Equal(RangedWeaponState.Active, active.State, "post-construction timing edits cannot corrupt active entry");
        TestAssert.Equal(1, active.EmissionsDue, "the original burst still has its first event");
        RangedWeaponFrame tail = model.Step(Input(), .37d);
        TestAssert.Equal(2, tail.EmissionsDue, "the original burst interval and count remain frozen");
        RangedWeaponFrame recovery = model.Step(Input(), .05d);
        TestAssert.Equal(RangedWeaponState.Recovery, recovery.State, "the original active window still closes at 0.42 seconds");
        TestAssert.Near(1.20f, recovery.AttackTimeline.PhaseRemainingSeconds, .00001f, "the original recovery duration remains frozen");
        TestAssert.Equal(RangedWeaponState.Idle, model.Step(Input(), 1.20d).State, "the original recovery still completes normally");
        model.Step(Input(stun: true), 0d);
        TestAssert.Equal(RangedWeaponState.Stunned, model.Step(Input(), .799d).State, "the original stun duration survives resource edits");
        TestAssert.Equal(RangedWeaponState.Idle, model.Step(Input(), .001d).State, "the original stun still expires at 0.8 seconds");
    }

    private static RangedWeaponInput Input(Vector2? target = null, bool visible = true,
        bool stun = false, bool dead = false, Vector2? muzzle = null) =>
        new(muzzle ?? Vector2.Zero, target ?? new Vector2(200f, 0f), visible, stun, dead);

    private static RangedWeaponModel Model(RangedWeaponRole role) => new(RangedWeaponSettings.ForRole(role));

    private static RangedWeaponModel TelegraphModel(RangedWeaponRole role)
    {
        RangedWeaponModel model = Model(role);
        model.Step(Input(), 0d);
        return model;
    }

    private static RangedWeaponModel ActiveModel(RangedWeaponRole role)
    {
        RangedWeaponModel model = TelegraphModel(role);
        model.Step(Input(), 2d);
        return model;
    }

    private static RangedWeaponModel RecoveryModel(RangedWeaponRole role)
    {
        RangedWeaponModel model = ActiveModel(role);
        model.Step(Input(), .42d);
        return model;
    }

    private static RangedWeaponModel StunnedModel(RangedWeaponRole role)
    {
        RangedWeaponModel model = Model(role);
        model.Step(Input(stun: true), 0d);
        return model;
    }

    private static void NoEmission(RangedWeaponFrame frame, string context)
    {
        TestAssert.Equal(0, frame.EmissionsDue, $"{context} has no muzzle event");
        TestAssert.Equal(-1, frame.FirstEmissionIndex, $"{context} has no misleading emission index");
    }
}
