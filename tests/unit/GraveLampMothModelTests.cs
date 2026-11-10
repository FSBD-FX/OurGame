using System;
using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Moth;

namespace Knight.Tests.Unit;

public static class GraveLampMothModelTests
{
    private static readonly EnemyArchetypeSettings Settings = new(
        DetectionRange: 480f,
        MoveSpeed: 140f,
        DiveSpeed: 420f,
        MaxHealth: 2,
        PreferredHoverHeight: 140f,
        HorizontalAlignmentTolerance: 36f,
        VerticalAlignmentTolerance: 28f,
        TelegraphSeconds: 0.70f,
        AttackSeconds: 0.35f,
        RecoverySeconds: 0.70f,
        ThreadStunSeconds: 0.75f,
        Gravity: 980f,
        ContactDamage: 1);

    public static void Run()
    {
        TuningRejectsEveryInvalidBoundary();
        FirstLevelMothTuningRejectsContactDamageOtherThanOne();
        ApprovedMothTimingAndDamageTableIsExact();
        NoPlayerKeepsHover();
        DetectionEntersAlignAndMovesOnBothAxes();
        AlignmentUsesAttackPointErrorInsteadOfBodyOverlap();
        AlignmentMustCompleteBeforeTelegraph();
        TelegraphBoundaryIsInclusiveAndDiveUsesLockedDirection();
        TimelineLocksWorldTargetAndPreservesOneAttackIdentity();
        DiveEndsOnDurationOrWorldContact();
        RecoverReturnsToHoverOnlyAtItsBoundary();
        ThreadInterruptsEveryLivingStateAndStunExpiresToHover();
        DeathOverridesEveryStateAndNeverAttacks();
    }

    private static void FirstLevelMothTuningRejectsContactDamageOtherThanOne()
    {
        var tuning = new EnemyArchetypeTuning { ContactDamage = 2 };
        TestAssert.Equal(1, tuning.Validate().Count,
            "first-level Moth tuning rejects positive contact damage above one");
    }

    private static void TuningRejectsEveryInvalidBoundary()
    {
        var tuning = new EnemyArchetypeTuning
        {
            DetectionRange = 0f,
            MoveSpeed = float.NaN,
            DiveSpeed = -1f,
            MaxHealth = 0,
            PreferredHoverHeight = 0f,
            HorizontalAlignmentTolerance = -1f,
            VerticalAlignmentTolerance = float.PositiveInfinity,
            TelegraphSeconds = -0.01f,
            AttackSeconds = 0f,
            RecoverySeconds = -0.01f,
            ThreadStunSeconds = 0f,
            Gravity = -1f,
            ContactDamage = 0
        };

        TestAssert.Equal(13, tuning.Validate().Count,
            "every invalid moth tuning boundary produces one actionable validation error");
        TestAssert.Equal(0, new EnemyArchetypeTuning().Validate().Count,
            "the authored moth tuning defaults are valid");
        bool threw = false;
        try { _ = new GraveLampMothModel(tuning.ToSettings()); }
        catch (ArgumentException) { threw = true; }
        TestAssert.True(threw, "invalid tuning cannot enter the pure moth model");
    }

    private static void ApprovedMothTimingAndDamageTableIsExact()
    {
        var tuning = new EnemyArchetypeTuning();
        TestAssert.Near(.70f, tuning.TelegraphSeconds, .000001f,
            "approved Grave-Lamp Moth dive telegraph is exactly 0.70 seconds");
        TestAssert.Near(.35f, tuning.AttackSeconds, .000001f,
            "approved Grave-Lamp Moth dive active window is exactly 0.35 seconds");
        TestAssert.Near(.70f, tuning.RecoverySeconds, .000001f,
            "approved Grave-Lamp Moth recovery is exactly 0.70 seconds");
        TestAssert.Equal(1, tuning.ContactDamage,
            "first-level Grave-Lamp Moth contact damage is exactly one segment");
    }

    private static void AlignmentUsesAttackPointErrorInsteadOfBodyOverlap()
    {
        var model = new GraveLampMothModel(Settings);
        model.Step(Input(true, 0f, 140f, alignmentX: 90f, alignmentY: 40f), 0d);
        GraveLampMothFrame moving = model.Step(
            Input(true, 0f, 140f, alignmentX: -50f, alignmentY: 20f), 0.016d);
        GraveLampMothFrame aligned = model.Step(
            Input(true, 0f, 140f, alignmentX: 36f, alignmentY: 28f), 0d);

        TestAssert.Near(-140f, moving.VelocityX, 0.001f,
            "align movement follows the configured overhead attack point, not the player body");
        TestAssert.Near(140f, moving.VelocityY, 0.001f,
            "align movement resolves vertical attack-point error independently");
        TestAssert.Equal(GraveLampMothState.Telegraph, aligned.State,
            "moth can telegraph while remaining the preferred height above the player");
    }

    private static void NoPlayerKeepsHover()
    {
        var model = new GraveLampMothModel(Settings);
        GraveLampMothFrame frame = model.Step(Input(false, 120f, 80f), 2d);

        TestAssert.Equal(GraveLampMothState.Hover, frame.State,
            "missing player keeps the moth hovering regardless of elapsed time");
        TestAssert.Near(0f, frame.VelocityX, 0.001f, "hover has no chase X velocity");
        TestAssert.Near(0f, frame.VelocityY, 0.001f, "hover has no chase Y velocity");
        TestAssert.True(!frame.AttackActive, "hover never activates contact attack");
    }

    private static void DetectionEntersAlignAndMovesOnBothAxes()
    {
        var model = new GraveLampMothModel(Settings);
        GraveLampMothFrame entered = model.Step(Input(true, 200f, 100f), 0d);
        GraveLampMothFrame aligning = model.Step(Input(true, 200f, 100f), 0.016d);

        TestAssert.Equal(GraveLampMothState.Align, entered.State,
            "detection enters align before any telegraph or dive");
        TestAssert.True(entered.EnteredState, "detection reports the align transition");
        TestAssert.Near(140f, aligning.VelocityX, 0.001f,
            "align uses configured positive horizontal speed toward the player");
        TestAssert.Near(140f, aligning.VelocityY, 0.001f,
            "align uses configured positive vertical speed toward the player");
        TestAssert.True(!aligning.AttackActive, "align movement cannot damage");

        GraveLampMothFrame lost = model.Step(Input(false, 200f, 100f), 0d);
        TestAssert.Equal(GraveLampMothState.Hover, lost.State,
            "losing detection during align returns to hover");
    }

    private static void AlignmentMustCompleteBeforeTelegraph()
    {
        var model = new GraveLampMothModel(Settings);
        model.Step(Input(true, 200f, 100f), 0d);
        GraveLampMothFrame horizontalMiss = model.Step(Input(true, 36.01f, 0f), 0d);
        GraveLampMothFrame verticalMiss = model.Step(Input(true, 0f, 28.01f), 0d);
        GraveLampMothFrame aligned = model.Step(Input(true, 36f, 28f), 0d);

        TestAssert.Equal(GraveLampMothState.Align, horizontalMiss.State,
            "horizontal alignment outside tolerance cannot telegraph");
        TestAssert.Equal(GraveLampMothState.Align, verticalMiss.State,
            "vertical alignment outside tolerance cannot telegraph");
        TestAssert.Equal(GraveLampMothState.Telegraph, aligned.State,
            "both inclusive alignment tolerances enter telegraph");
        TestAssert.True(!aligned.AttackActive, "telegraph is visibly safe");
    }

    private static void TelegraphBoundaryIsInclusiveAndDiveUsesLockedDirection()
    {
        var model = TelegraphModel();
        GraveLampMothFrame before = model.Step(Input(true, 300f, 400f), 0.699d);
        GraveLampMothFrame dive = model.Step(Input(true, 300f, 400f), 0.001d);
        GraveLampMothFrame locked = model.Step(Input(true, -400f, 0f), 0.016d);

        TestAssert.Equal(GraveLampMothState.Telegraph, before.State,
            "telegraph remains safe before 0.70 seconds");
        TestAssert.Equal(GraveLampMothState.Dive, dive.State,
            "telegraph enters dive exactly at 0.70 seconds");
        TestAssert.True(dive.AttackActive, "dive activates contact damage");
        TestAssert.Near(252f, dive.VelocityX, 0.01f,
            "three-four-five target locks a literal 252 X dive velocity");
        TestAssert.Near(336f, dive.VelocityY, 0.01f,
            "three-four-five target locks a literal 336 Y dive velocity");
        TestAssert.Near(252f, locked.VelocityX, 0.01f,
            "dive direction does not home after launch");
        TestAssert.Near(336f, locked.VelocityY, 0.01f,
            "dive keeps its locked vertical direction after launch");
    }

    private static void TimelineLocksWorldTargetAndPreservesOneAttackIdentity()
    {
        var model = TelegraphModel();
        Vector2 launchTarget = new(530f, 240f);
        GraveLampMothFrame telegraph = model.Step(Input(true, 300f, 400f), .699d, launchTarget);
        GraveLampMothFrame active = model.Step(Input(true, 300f, 400f), .001d, launchTarget);
        GraveLampMothFrame locked = model.Step(Input(true, -400f, 0f), .016d, new Vector2(-900f, 20f));
        GraveLampMothFrame recovery = model.Step(Input(true, -400f, 0f), .334d, new Vector2(-900f, 20f));

        TestAssert.Equal(AttackPhase.Telegraph, telegraph.AttackTimeline.Phase,
            "Moth publishes Telegraph immediately before the inclusive boundary");
        TestAssert.Equal(EnemyAttackKind.MothDive, telegraph.AttackTimeline.Kind,
            "Moth publishes the unique dive kind");
        TestAssert.Equal(1UL, telegraph.AttackTimeline.AttackId,
            "first Moth telegraph owns the first attack identity");
        TestAssert.True(!telegraph.AttackTimeline.HasLockedTarget,
            "Moth target remains unlocked during the safe telegraph");
        TestAssert.Equal(AttackPhase.Active, active.AttackTimeline.Phase,
            "Moth enters Active exactly at 0.70 seconds");
        TestAssert.True(active.AttackTimeline.HasLockedTarget,
            "Moth locks its world target at the Telegraph-to-Active boundary");
        TestAssert.Equal(launchTarget, active.AttackTimeline.LockedWorldTarget,
            "Moth records the launch target in world space");
        TestAssert.Equal(active.AttackTimeline.AttackId, locked.AttackTimeline.AttackId,
            "Moth Active keeps the Telegraph identity while the player moves");
        TestAssert.Equal(launchTarget, locked.AttackTimeline.LockedWorldTarget,
            "Moth locked world target cannot home after launch");
        TestAssert.Equal(AttackPhase.Recovery, recovery.AttackTimeline.Phase,
            "Moth enters Recovery exactly at the active endpoint");
        TestAssert.Equal(active.AttackTimeline.AttackId, recovery.AttackTimeline.AttackId,
            "Moth Recovery preserves the same attack identity");
        TestAssert.Equal(launchTarget, recovery.AttackTimeline.LockedWorldTarget,
            "Moth Recovery preserves the locked target for read-only presentation");
    }

    private static void DiveEndsOnDurationOrWorldContact()
    {
        var durationModel = DivingModel();
        GraveLampMothFrame before = durationModel.Step(Input(true, 100f, 0f), 0.349d);
        GraveLampMothFrame timedOut = durationModel.Step(Input(true, 100f, 0f), 0.001d);
        var contactModel = DivingModel();
        GraveLampMothFrame contact = contactModel.Step(Input(true, 100f, 0f, worldContact: true), 0d);

        TestAssert.Equal(GraveLampMothState.Dive, before.State,
            "dive remains active before 0.35 seconds");
        TestAssert.True(before.AttackActive, "pre-boundary dive still attacks");
        TestAssert.Equal(GraveLampMothState.Recover, timedOut.State,
            "dive duration ends into recovery at 0.35 seconds");
        TestAssert.True(!timedOut.AttackActive, "timed-out dive closes its attack");
        TestAssert.Equal(GraveLampMothState.Recover, contact.State,
            "world contact ends dive immediately");
        TestAssert.True(!contact.AttackActive, "world contact closes contact damage immediately");
    }

    private static void RecoverReturnsToHoverOnlyAtItsBoundary()
    {
        var model = DivingModel();
        model.Step(Input(true, 100f, 0f, worldContact: true), 0d);
        GraveLampMothFrame before = model.Step(Input(true, 100f, 0f), 0.699d);
        GraveLampMothFrame hover = model.Step(Input(true, 100f, 0f), 0.001d);

        TestAssert.Equal(GraveLampMothState.Recover, before.State,
            "recovery persists before 0.70 seconds");
        TestAssert.Equal(GraveLampMothState.Hover, hover.State,
            "recovery returns to hover exactly at 0.70 seconds");
        TestAssert.True(!hover.AttackActive, "returned hover is non-damaging");
    }

    private static void ThreadInterruptsEveryLivingStateAndStunExpiresToHover()
    {
        GraveLampMothModel[] models =
        [
            new(Settings),
            AlignModel(),
            TelegraphModel(),
            DivingModel(),
            RecoverModel(),
            ThreadStunnedModel()
        ];

        foreach (GraveLampMothModel model in models)
        {
            GraveLampMothFrame interrupted = model.Step(Input(true, 100f, 40f, thread: true), 0d);
            TestAssert.Equal(GraveLampMothState.ThreadStunned, interrupted.State,
                "thread interrupts every living moth state");
            TestAssert.True(!interrupted.AttackActive, "thread interruption always closes contact damage");
        }

        var expiry = ThreadStunnedModel();
        GraveLampMothFrame before = expiry.Step(Input(true, 100f, 40f), 0.749d);
        GraveLampMothFrame hover = expiry.Step(Input(true, 100f, 40f), 0.001d);
        TestAssert.Equal(GraveLampMothState.ThreadStunned, before.State,
            "thread stun remains before 0.75 seconds");
        TestAssert.Equal(GraveLampMothState.Hover, hover.State,
            "thread stun returns to hover exactly at 0.75 seconds");
    }

    private static void DeathOverridesEveryStateAndNeverAttacks()
    {
        GraveLampMothModel[] models =
        [
            new(Settings), AlignModel(), TelegraphModel(), DivingModel(), RecoverModel(), ThreadStunnedModel()
        ];
        foreach (GraveLampMothModel model in models)
        {
            GraveLampMothFrame dead = model.Step(Input(true, 100f, 0f, thread: true, dead: true), 0d);
            GraveLampMothFrame remainsDead = model.Step(Input(true, 100f, 0f), 10d);
            TestAssert.Equal(GraveLampMothState.Dead, dead.State,
                "death immediately overrides every moth state and simultaneous thread");
            TestAssert.True(!dead.AttackActive, "dead moth cannot retain an attack window");
            TestAssert.Near(0f, dead.VelocityX, 0.001f, "dead moth has no model X velocity");
            TestAssert.Near(0f, dead.VelocityY, 0.001f, "dead moth has no model Y velocity");
            TestAssert.Equal(GraveLampMothState.Dead, remainsDead.State, "dead is absorbing");
        }
    }

    private static GraveLampMothInput Input(bool hasPlayer, float dx, float dy,
        bool worldContact = false, bool thread = false, bool dead = false,
        float? alignmentX = null, float? alignmentY = null) =>
        new(hasPlayer, dx, dy, alignmentX ?? dx, alignmentY ?? dy, worldContact, thread, dead);

    private static GraveLampMothModel AlignModel()
    {
        var model = new GraveLampMothModel(Settings);
        model.Step(Input(true, 100f, 100f), 0d);
        return model;
    }

    private static GraveLampMothModel TelegraphModel()
    {
        GraveLampMothModel model = AlignModel();
        model.Step(Input(true, 36f, 28f), 0d);
        return model;
    }

    private static GraveLampMothModel DivingModel()
    {
        GraveLampMothModel model = TelegraphModel();
        model.Step(Input(true, 100f, 0f), 0.70d);
        return model;
    }

    private static GraveLampMothModel RecoverModel()
    {
        GraveLampMothModel model = DivingModel();
        model.Step(Input(true, 100f, 0f, worldContact: true), 0d);
        return model;
    }

    private static GraveLampMothModel ThreadStunnedModel()
    {
        var model = new GraveLampMothModel(Settings);
        model.Step(Input(true, 100f, 0f, thread: true), 0d);
        return model;
    }
}
