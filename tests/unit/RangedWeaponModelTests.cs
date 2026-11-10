using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;
using RechargeCoverage = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10RechargeSourceCoverage;

namespace Knight.Tests.Unit;

public static class RangedWeaponModelTests
{
    public static void Run()
    {
        EachRolePublishesItsWarningBeforeItsOwnInclusiveFireBoundary();
        EachRoleKeepsItsOwnActiveAndRecoveryWindows();
        BurstEmitsAtZeroPointEighteenAndPointThirtySixOnly();
        CrossingBothRemainingBurstPointsDoesNotDropOrRepeatShots();
        BurstEmissionQueryUsesTheCurrentStepSchedule();
        RechargeRequiresCompleteCurrentSafeSourceCoverage();
        NormalRoutesSelectExistingSafetyPoliciesWithoutExperimentalFlags();
        GalleryForeignGuardRoutingRequiresBothScopesAndRetainsNormalAndExplicitSelection();
        GalleryEarlyCounterRoutesOnlyNormalOwnedSkitterRecoveryOrExistingExplicitProbe();
        Grip8GhostClearanceSelectsBothKeyboardRoutesOnlyAfterGrip8();
        GalleryForeignCounterRetainsTheLiveOwnedSkitterDefenseBudget();
        GalleryHp2CollectorRetainsOnlyTheFirstCompletedEligibleRows();
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

    private static void BurstEmissionQueryUsesTheCurrentStepSchedule()
    {
        var model = ActiveModel(RangedWeaponRole.Burst);
        var second = model.Step(Input(), .20d);
        ulong id = second.AttackTimeline.AttackId;
        TestAssert.Equal(1, second.FirstEmissionIndex, "wave clock fixture is after its second scheduled shot");
        TestAssert.True(model.IsBurstEmissionClearThrough(id, 1d / 60d),
            "BURST_EMISSION_CLOCK_RED: after the second shot one nominal step cannot reach the third");
        TestAssert.True(!model.IsBurstEmissionClearThrough(id, .17d), "the next shot is inside a longer step");
        TestAssert.True(model.IsBurstEmissionClearThrough(id, 1d / 60d), "queries must not advance the model");
        foreach (double invalid in new[] { 0d, -1d, double.NaN, double.PositiveInfinity, double.MaxValue })
            TestAssert.True(!model.IsBurstEmissionClearThrough(id, invalid), "invalid horizon does not prove clearance");
        TestAssert.True(!model.IsBurstEmissionClearThrough(0, .01d) &&
            !model.IsBurstEmissionClearThrough(id + 1, .01d), "absent or stale identity does not prove clearance");
        var third = model.Step(Input(), .17d);
        TestAssert.True(third.FirstEmissionIndex == 2 && third.EmissionsDue == 1, "the query must not consume the third emission");
        TestAssert.True(model.IsBurstEmissionClearThrough(id, .05d), "spent volley stays clear across the single-step Active end");
        NoEmission(model.Step(Input(), .05d), "spent volley transitions without a new shot");
        TestAssert.True(!model.IsBurstEmissionClearThrough(id, .01d), "Recovery is not the admitted Active clock");

        var edge = ActiveModel(RangedWeaponRole.Burst);
        double interval = (double).18f;
        double before = interval - interval * .000002d, inclusive = interval - interval * .0000005d;
        TestAssert.True(edge.IsBurstEmissionClearThrough(1, before) &&
            !edge.IsBurstEmissionClearThrough(1, inclusive) && !edge.IsBurstEmissionClearThrough(1, interval),
            "one-step lookahead must preserve the real inclusive emission tolerance");
        TestAssert.Equal(1, edge.Step(Input(), inclusive).FirstEmissionIndex, "actual scheduling agrees at the inclusive edge");
        var hitch = ActiveModel(RangedWeaponRole.Burst);
        TestAssert.True(!hitch.IsBurstEmissionClearThrough(1, .50d), "Active-end hitch cannot hide scheduled tail shots");
        TestAssert.Equal(2, hitch.Step(Input(), .50d).EmissionsDue, "actual hitch delivers both tail events");
        TestAssert.True(!ActiveModel(RangedWeaponRole.Laser).IsBurstEmissionClearThrough(1, .01d) &&
            !TelegraphModel(RangedWeaponRole.Burst).IsBurstEmissionClearThrough(1, .01d),
            "foreign role and phase are unknown, not clear");
        GD.Print("BURST_EMISSION_CLOCK_OK tolerance=true next_step=true hitch=true pure=true");
    }

    // Mutation targets: treating an empty/partial scan as safe, carrying an old proof,
    // or confusing distant existing rounds with permission for a new Burst emission.
    private static void NormalRoutesSelectExistingSafetyPoliciesWithoutExperimentalFlags()
    {
        // Mutation targets: omitting the normal route, enabling outside the
        // early-positioning scope, or removing the existing explicit control.
        // Selection does not itself certify sources or authorize shield release.
        (bool Early, bool Normal, bool Explicit, bool Want)[] cases =
        [
            (false, false, false, false), (false, false, true, false),
            (false, true, false, false), (false, true, true, false),
            (true, false, false, false), (true, false, true, true),
            (true, true, false, true), (true, true, true, true)
        ];
        var failures = new System.Collections.Generic.List<string>();
        foreach (var row in cases)
        {
            bool recharge = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10PostHopRechargeRoute(
                row.Early, row.Normal, row.Explicit);
            bool rideGuard = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10LiftGhostGuardRoute(
                row.Early, row.Normal, row.Explicit);
            if (recharge != row.Want) failures.Add($"recharge/{row.Early}/{row.Normal}/{row.Explicit}");
            if (rideGuard != row.Want) failures.Add($"lift/{row.Early}/{row.Normal}/{row.Explicit}");
        }
        TestAssert.True(failures.Count == 0,
            "NORMAL_SAFETY_ROUTING_RED: default normal route must select its existing guarded policies without an experiment flag; failures=" +
            string.Join(",", failures));
        GD.Print("NORMAL_ROUTE_SAFETY_ROUTING_OK cases=8 policies=2 default_selected=true explicit_preserved=true outside_scope_rejected=true safety_authorization=false");
    }

    // Mutation targets: losing normal selection, either scope gate, or the
    // existing explicit control. Selection is not a foreignUp safety proof.
    private static void GalleryForeignGuardRoutingRequiresBothScopesAndRetainsNormalAndExplicitSelection()
    {
        (bool Keyboard, bool Gallery, bool Normal, bool Explicit, bool Want)[] cases =
        [
            (false, false, false, false, false),
            (false, false, false, true, false),
            (false, false, true, false, false),
            (false, false, true, true, false),
            (false, true, false, false, false),
            (false, true, false, true, false),
            (false, true, true, false, false),
            (false, true, true, true, false),
            (true, false, false, false, false),
            (true, false, false, true, false),
            (true, false, true, false, false),
            (true, false, true, true, false),
            (true, true, false, false, false),
            (true, true, false, true, true),
            (true, true, true, false, true),
            (true, true, true, true, true)
        ];
        var failures = new System.Collections.Generic.List<string>();
        foreach (var row in cases)
        {
            bool selected = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10GalleryForeignGuardRoute(
                row.Keyboard, row.Gallery, row.Normal, row.Explicit);
            if (selected != row.Want)
                failures.Add($"keyboard={row.Keyboard}/gallery={row.Gallery}/normal={row.Normal}/explicit={row.Explicit}/want={row.Want}/actual={selected}");
        }
        TestAssert.True(failures.Count == 0,
            "GALLERY_FOREIGN_GUARD_ROUTING_RED: default normal route must select the existing foreignUp policy only within both scopes; failures=" +
            string.Join(",", failures));
        GD.Print("GALLERY_FOREIGN_GUARD_ROUTING_OK cases=16 default_selected=true explicit_preserved=true both_scopes_required=true safety_authorization=false");
    }

    // Selection only: true still has to pass the live source/floor/window/fresh checks.
    private static void GalleryEarlyCounterRoutesOnlyNormalOwnedSkitterRecoveryOrExistingExplicitProbe()
    {
        (string Name, bool Keyboard, bool Gallery, bool Normal, bool Explicit,
            bool Owned, bool Skitter, bool Recovery, bool Want)[] cases =
        [
            ("normal owned Skitter Recovery", true,true,true,false,true,true,true,true),
            ("normal plus explicit", true,true,true,true,true,true,true,true),
            ("not keyboard", false,true,true,false,true,true,true,false),
            ("not gallery", true,false,true,false,true,true,true,false),
            ("not normal and no explicit", true,true,false,false,true,true,true,false),
            ("normal foreign Skitter", true,true,true,false,false,true,true,false),
            ("normal owned laser", true,true,true,false,true,false,true,false),
            ("normal owned other family", true,true,true,false,true,false,true,false),
            ("normal not Recovery scope", true,true,true,false,true,true,false,false),
            ("normal foreign laser", true,true,true,false,false,false,true,false),
            ("normal all domain gates false", true,true,true,false,false,false,false,false),
            ("explicit without normal", true,true,false,true,true,true,true,true),
            ("explicit laser retained", true,true,false,true,true,false,true,true),
            ("explicit nonRecovery selection retained", true,true,false,true,true,true,false,true),
            ("explicit foreign selection retained", true,true,false,true,false,true,true,true),
            ("explicit domain flags absent", true,true,false,true,false,false,false,true),
            ("normal explicit laser retained", true,true,true,true,true,false,true,true),
            ("explicit still needs keyboard", false,true,false,true,true,true,true,false),
            ("explicit still needs gallery", true,false,false,true,true,true,true,false),
            ("both routes outside keyboard", false,true,true,true,true,true,true,false),
            ("both routes outside gallery", true,false,true,true,true,true,true,false)
        ];
        var failures = new List<string>();
        foreach (var row in cases)
        {
            bool selected = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10GalleryEarlyCounterRoute(
                row.Keyboard, row.Gallery, row.Normal, row.Explicit, row.Owned, row.Skitter, row.Recovery);
            if (selected != row.Want) failures.Add($"{row.Name}/want={row.Want}/actual={selected}");
        }
        TestAssert.True(failures.Count == 0,
            "GALLERY_EARLY_COUNTER_ROUTE_RED: normal owned Skitter Recovery must reach existing live checks, " +
            "without admitting other normal scopes or losing explicit selection; failures=" + string.Join(",", failures));
        GD.Print("GALLERY_EARLY_COUNTER_ROUTE_OK cases=21 normal_owned_skitter_recovery=true " +
            "explicit_preserved=true both_scopes_required=true live_safety_not_bypassed=true safety_authorization=false");
    }

    // Selection only: the existing live fight still owns floor/HP/foreign/fresh
    // checks and requires each real J to hit, followed by natural corpse withdrawal.
    private static void Grip8GhostClearanceSelectsBothKeyboardRoutesOnlyAfterGrip8()
    {
        (string Name, bool Keyboard, bool Main, bool Exploration, int Index, bool Want)[] cases =
        [
            ("normal Main after Grip8", true, true, false, 8, true),
            ("existing Explore after Grip8", true, false, true, 8, true),
            ("no keyboard Main", false, true, false, 8, false),
            ("no keyboard Explore", false, false, true, 8, false),
            ("no known variant", true, false, false, 8, false),
            ("Main before ascent", true, true, false, 0, false),
            ("Main before Grip8", true, true, false, 7, false),
            ("Main after Grip9", true, true, false, 9, false),
            ("Main negative index", true, true, false, -1, false),
            ("Main unknown index", true, true, false, int.MaxValue, false),
            ("Explore before Grip8", true, false, true, 7, false),
            ("Explore after Grip9", true, false, true, 9, false)
        ];
        var failures = new List<string>();
        foreach (var row in cases)
        {
            bool selected = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10Grip8GhostClearanceRoute(
                row.Keyboard, row.Main, row.Exploration, row.Index);
            if (selected != row.Want) failures.Add($"{row.Name}/want={row.Want}/actual={selected}");
        }
        TestAssert.True(failures.Count == 0,
            "GRIP8_GHOST_CLEARANCE_ROUTE_RED: both keyboard routes must reach the existing live fight only after Grip8; failures=" +
            string.Join(",", failures));
        GD.Print("GRIP8_GHOST_CLEARANCE_ROUTE_OK cases=12 main_selected=true exploration_retained=true " +
            "keyboard_and_grip8_required=true live_fight_not_bypassed=true safety_authorization=false");
    }

    // Mutation targets: spending only the attack fee while an owned Skitter
    // lives, a fixed-Hz reserve, or accepting unknown/invalid required budgets.
    private static void GalleryForeignCounterRetainsTheLiveOwnedSkitterDefenseBudget()
    {
        (string Name, bool Required, float Energy, float Attack, float Drain, float Rush, int Hz, bool Want)[] cases =
        [
            ("first resident J", true, 29.3f, 12f, 24f, .20f, 60, true),
            ("second resident J spends defense", true, 17.3f, 12f, 24f, .20f, 60, false),
            ("third resident J spends defense", true, 12.3f, 12f, 24f, .20f, 60, false),
            ("below 60Hz budget", true, 22.7999f, 12f, 24f, .20f, 60, false),
            ("above 60Hz budget", true, 22.8001f, 12f, 24f, .20f, 60, true),
            ("below 30Hz budget", true, 23.9999f, 12f, 24f, .20f, 30, false),
            ("above 30Hz budget", true, 24.0001f, 12f, 24f, .20f, 30, true),
            ("zero rush below budget", true, 17.9999f, 12f, 24f, 0f, 60, false),
            ("zero rush above budget", true, 18.0001f, 12f, 24f, 0f, 60, true),
            ("zero drain exact fee", true, 12f, 12f, 0f, .20f, 60, true),
            ("cannot pay fee", true, 11.999f, 12f, 0f, .20f, 60, false),
            ("unknown energy", true, float.NaN, 12f, 24f, .20f, 60, false),
            ("infinite energy", true, float.PositiveInfinity, 12f, 24f, .20f, 60, false),
            ("negative energy", true, -1f, 12f, 24f, .20f, 60, false),
            ("unknown attack", true, 29.3f, float.NaN, 24f, .20f, 60, false),
            ("negative attack", true, 29.3f, -1f, 24f, .20f, 60, false),
            ("unknown drain", true, 29.3f, 12f, float.NaN, .20f, 60, false),
            ("infinite drain", true, 29.3f, 12f, float.PositiveInfinity, .20f, 60, false),
            ("negative drain", true, 29.3f, 12f, -1f, .20f, 60, false),
            ("unknown rush", true, 29.3f, 12f, 24f, float.NaN, 60, false),
            ("infinite rush", true, 29.3f, 12f, 24f, float.PositiveInfinity, 60, false),
            ("negative rush", true, 29.3f, 12f, 24f, -1f, 60, false),
            ("zero Hz", true, 29.3f, 12f, 24f, .20f, 0, false),
            ("negative Hz", true, 29.3f, 12f, 24f, .20f, -60, false),
            ("overflowed reserve", true, float.MaxValue, 12f, float.MaxValue, float.MaxValue, 60, false),
            ("outside scope no extra restriction", false, 0f, 12f, 24f, .20f, 60, true),
            ("outside scope unknown unused values", false, float.NaN, float.NaN, float.NaN, float.NaN, 0, true)
        ];
        var failures = new System.Collections.Generic.List<string>();
        foreach (var row in cases)
        {
            bool allowed = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10GalleryForeignCounterBudget(
                row.Required, row.Energy, row.Attack, row.Drain, row.Rush, row.Hz);
            if (allowed != row.Want) failures.Add($"{row.Name}/want={row.Want}/actual={allowed}");
        }
        TestAssert.True(failures.Count == 0,
            "GALLERY_FOREIGN_COUNTER_BUDGET_RED: foreign resident J must retain the live owned Skitter defense reserve; failures=" +
            string.Join(",", failures));
        GD.Print("GALLERY_FOREIGN_COUNTER_BUDGET_OK cases=27 live_reserve=true current_Hz=true outside_scope_unrestricted=true safety_authorization=false");
    }

    // Mutation targets: dropped completed rows, inverted scope/HP filters, or
    // overwriting the first window after its 24-row prefix is full. Collector only, not safety.
    private static void GalleryHp2CollectorRetainsOnlyTheFirstCompletedEligibleRows()
    {
        var rows = new List<string>();
        const string first = "{\"loop\":317,\"physics\":8030,\"hp\":2,\"eligible\":true}";
        bool captured = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10CaptureGalleryHp2Decision(
            rows, true, true, 2, first);
        TestAssert.True(captured && rows.Count == 1 && rows[0] == first,
            "GALLERY_HP2_COLLECTOR_RED: enabled eligible HP2 must append and retain the completed literal row");

        (string Name, bool Enabled, bool Eligible, int Health, string Row)[] rejected =
        [
            ("disabled", false, true, 2, "disabled row"),
            ("not locally eligible", true, false, 2, "ineligible row"),
            ("HP1 remains separate", true, true, 1, "HP1 row"),
            ("HP0", true, true, 0, "HP0 row"),
            ("HP3", true, true, 3, "HP3 row"),
            ("null incomplete row", true, true, 2, null),
            ("empty incomplete row", true, true, 2, "")
        ];
        foreach (var test in rejected)
        {
            bool accepted = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10CaptureGalleryHp2Decision(
                rows, test.Enabled, test.Eligible, test.Health, test.Row);
            TestAssert.True(!accepted && rows.Count == 1 && rows[0] == first,
                $"{test.Name} must neither append nor overwrite an existing observation");
        }

        string[] remaining =
        [
            "row02", "row03", "row04", "row05", "row06", "row07", "row08", "row09",
            "row10", "row11", "row12", "row13", "row14", "row15", "row16", "row17",
            "row18", "row19", "row20", "row21", "row22", "row23", "row24"
        ];
        foreach (string completed in remaining)
        {
            bool accepted = Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10CaptureGalleryHp2Decision(
                rows, true, true, 2, completed);
            TestAssert.True(accepted, "each completed eligible row through the 24th must append");
        }
        TestAssert.Equal(24, rows.Count, "the 24th completed row fills the first-window prefix");
        for (int attempt = 0; attempt < 3; attempt++)
            TestAssert.True(!Knight.Tests.Integration.PrologueCanyonGeneratedMainRouteTests.Task10CaptureGalleryHp2Decision(
                rows, true, true, 2, "later window must not replace first"), "full prefix rejects every later row");
        TestAssert.True(rows.Count == 24 && rows[0] == first, "full prefix retains its first literal observation");
        for (int index = 0; index < remaining.Length; index++)
            TestAssert.Equal(remaining[index], rows[index + 1], "all first-window rows retain insertion order and content");
        GD.Print("GALLERY_HP2_COLLECTOR_OK real_rows=true first_prefix=24 filters=true completed_only=true order=true no_overwrite=true safety_authorization=false");
    }

    private static void RechargeRequiresCompleteCurrentSafeSourceCoverage()
    {
        (string Name, AttackPhase Phase, float Allowance, bool Current, bool Burst, bool EmissionClear, bool Want)[] cases =
        [
            ("quiet Active Burst", AttackPhase.Active, 0f, true, true, true, true),
            ("next Burst emission due", AttackPhase.Active, 1f, true, true, false, false),
            ("Active non-Burst", AttackPhase.Active, 1f, true, false, true, false),
            ("stale Active actor", AttackPhase.Active, 0f, false, true, true, false),
            ("safe warning", AttackPhase.Telegraph, .2f, true, false, false, true),
            ("safe recovery", AttackPhase.Recovery, .2f, true, false, false, true),
            ("safe passive clock", AttackPhase.None, .2f, true, false, false, true),
            ("inclusive short warning", AttackPhase.Telegraph, 3f / 60f, true, false, false, false),
            ("short recovery", AttackPhase.Recovery, .01f, true, false, false, false),
            ("unknown passive clock", AttackPhase.None, 0f, true, false, false, false),
            ("NaN warning", AttackPhase.Telegraph, float.NaN, true, false, false, false),
            ("infinite recovery", AttackPhase.Recovery, float.PositiveInfinity, true, false, false, false),
            ("stale warning actor", AttackPhase.Telegraph, .2f, false, false, false, false),
            ("unrecognized phase", (AttackPhase)99, .2f, true, true, true, false)
        ];
        foreach (var row in cases)
        {
            var coverage = new RechargeCoverage(10, 20);
            coverage.Observe(1, row.Phase, row.Allowance, row.Current, row.Burst, row.EmissionClear);
            TestAssert.Equal(row.Want, coverage.Admits(1, 10, 20), "recharge source: " + row.Name);
        }
        var exact = new RechargeCoverage(10, 20);
        TestAssert.True(!exact.Admits(0, 10, 20), "empty scan cannot authorize recharge");
        exact.Observe(1, AttackPhase.Telegraph, .2f, true, false, false);
        exact.Observe(2, AttackPhase.Active, 0f, true, true, true);
        TestAssert.True(exact.Admits(2, 10, 20) && exact.Count == 2 && exact.Faults == 0,
            "two safe distinct observed sources match the original scan");
        foreach (int expected in new[] { -1, 0, 1, 3 })
            TestAssert.True(!exact.Admits(expected, 10, 20), "partial or inconsistent source count cannot authorize recharge");
        TestAssert.True(!exact.Admits(2, 11, 20) && !exact.Admits(2, 10, 21), "a new tick or input invalidates the proof");
        exact.Observe(2, AttackPhase.Telegraph, .2f, true, false, false);
        TestAssert.True(!exact.Admits(3, 10, 20), "duplicate identity cannot fill missing source coverage");
        var missing = new RechargeCoverage(10, 20);
        missing.Observe(0, AttackPhase.Telegraph, .2f, true, false, false);
        TestAssert.True(!missing.Admits(1, 10, 20), "zero identity is not an observed source");
        foreach (var stamp in new[] { (Physics: 0UL, Sequence: 20UL), (Physics: 10UL, Sequence: 0UL) })
        {
            var invalid = new RechargeCoverage(stamp.Physics, stamp.Sequence);
            invalid.Observe(1, AttackPhase.Telegraph, .2f, true, false, false);
            TestAssert.True(!invalid.Admits(1, stamp.Physics, stamp.Sequence), "missing scan stamp cannot authorize recharge");
        }
        var capacity = new RechargeCoverage(10, 20);
        for (ulong id = 1; id <= 32; id++) capacity.Observe(id, AttackPhase.Telegraph, .2f, true, false, false);
        TestAssert.True(capacity.Admits(32, 10, 20), "a complete in-capacity scan remains admissible");
        capacity.Observe(33, AttackPhase.Telegraph, .2f, true, false, false);
        TestAssert.True(!capacity.Admits(33, 10, 20), "overflow cannot claim complete source coverage");
        var unsafeThenSafe = new RechargeCoverage(10, 20);
        unsafeThenSafe.Observe(1, AttackPhase.Active, 0f, true, true, false);
        unsafeThenSafe.Observe(2, AttackPhase.Telegraph, .2f, true, false, false);
        TestAssert.True(!unsafeThenSafe.Admits(2, 10, 20), "later safe source cannot erase an unsafe emission");
        var fault = new RechargeCoverage(10, 20);
        fault.Observe(1, AttackPhase.Telegraph, .2f, true, false, false);
        fault.Fault();
        fault.Observe(2, AttackPhase.Telegraph, .2f, true, false, false);
        TestAssert.True(fault.Faults == 1 && !fault.Admits(2, 10, 20), "observer fault remains a veto after more observations");
        GD.Print("RECHARGE_SOURCE_COVERAGE_OK current=true complete=true bounded=true emission_veto=true fault_veto=true");
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
