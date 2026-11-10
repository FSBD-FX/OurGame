using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static class ThreadAbilityModelTests
{
    public static void Run()
    {
        TapUsesConfiguredAssistRangeAndCone();
        HoldUsesConfiguredRangeAndCone();
        LineOfSightIsAlwaysRequired();
        MechanismWinsOverNearerEnemy();
        PullableEnemyWinsOverMovementAnchor();
        HeavyEnemyIsSelectableAsTutorialInteractionTarget();
        PriorityTieBreaksByAimThenDistanceThenId();
        TapRejectsZeroAim();
        NeutralTapAssistRejectsOutOfPolicyTargets();
        HoldWithNeutralAimSelectsNothing();
        CooldownDoesNotStartUntilCastIsConsumed();
        ConsumingTheSameCastIsIdempotent();
        DifferentCastsRespectTheCooldownBoundary();
    }

    private static void TapUsesConfiguredAssistRangeAndCone()
    {
        var model = new ThreadAbilityModel(new ThreadAbilitySettings(
            TapAssistRange: 260f, TapConeHalfAngleDegrees: 55f));

        ThreadCandidate? accepted = model.SelectTarget(Vector2.Zero,
            new Vector2(Mathf.Cos(Mathf.DegToRad(55f)), Mathf.Sin(Mathf.DegToRad(55f))) * 260f,
            ThreadAimMode.Tap, [Candidate(1, new Vector2(260f, 0f))]);
        ThreadCandidate? tooFar = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Tap,
            [Candidate(2, new Vector2(261f, 0f))]);
        ThreadCandidate? tooWide = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Tap,
            [Candidate(3, new Vector2(100f, 200f))]);
        float outsideRadians = Mathf.DegToRad(55.1f);
        ThreadCandidate? outsideBoundary = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Tap,
            [Candidate(4, new Vector2(Mathf.Cos(outsideRadians), Mathf.Sin(outsideRadians)) * 100f)]);

        TestAssert.True(accepted.HasValue, "tap accepts the configured 260 pixel and 55 degree boundary");
        TestAssert.True(!tooFar.HasValue, "tap rejects targets outside its configured assist range");
        TestAssert.True(!tooWide.HasValue, "tap rejects targets outside its configured cone");
        TestAssert.True(!outsideBoundary.HasValue, "tap rejects aim just outside the configured 55 degree boundary");
    }

    private static void TapRejectsZeroAim()
    {
        var model = new ThreadAbilityModel();

        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Zero, ThreadAimMode.Tap,
            [Candidate(1, new Vector2(100f, 0f), ThreadTargetKind.Mechanism)]);

        TestAssert.True(!selected.HasValue, "mode-aware tap requires a non-zero aim or facing vector");
    }

    private static void HoldUsesConfiguredRangeAndCone()
    {
        var model = new ThreadAbilityModel(new ThreadAbilitySettings(
            HoldRange: 400f, HoldConeHalfAngleDegrees: 30f));

        ThreadCandidate? accepted = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(1, new Vector2(400f, 0f))]);
        ThreadCandidate? tooFar = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(2, new Vector2(401f, 0f))]);
        ThreadCandidate? tooWide = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(3, new Vector2(100f, 100f))]);

        TestAssert.True(accepted.HasValue, "hold accepts the configured 400 pixel boundary");
        TestAssert.True(!tooFar.HasValue, "hold rejects targets outside its configured range");
        TestAssert.True(!tooWide.HasValue, "hold rejects targets outside its configured cone");
    }

    private static void LineOfSightIsAlwaysRequired()
    {
        var model = new ThreadAbilityModel();

        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Tap,
            [Candidate(1, new Vector2(100f, 0f), hasLineOfSight: false)]);

        TestAssert.True(!selected.HasValue, "an occluded target cannot be selected by either aim policy");
    }

    private static void MechanismWinsOverNearerEnemy()
    {
        var model = new ThreadAbilityModel();

        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(1, new Vector2(80f, 0f), ThreadTargetKind.LightEnemy),
             Candidate(2, new Vector2(240f, 0f), ThreadTargetKind.Mechanism)]);

        TestAssert.Equal(2UL, selected!.Value.Id,
            "a valid mechanism is selected before a nearer pullable enemy");
    }

    private static void PullableEnemyWinsOverMovementAnchor()
    {
        var model = new ThreadAbilityModel();

        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(1, new Vector2(80f, 0f), ThreadTargetKind.Anchor),
             Candidate(2, new Vector2(240f, 0f), ThreadTargetKind.LightEnemy)]);

        TestAssert.Equal(2UL, selected!.Value.Id,
            "a valid pullable enemy is selected before a nearer movement anchor");
    }

    private static void HeavyEnemyIsSelectableAsTutorialInteractionTarget()
    {
        var model = new ThreadAbilityModel();

        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(1, new Vector2(80f, 0f), ThreadTargetKind.HeavyEnemy),
             Candidate(2, new Vector2(160f, 0f), ThreadTargetKind.Anchor)]);

        TestAssert.Equal(1UL, selected!.Value.Id,
            "a heavy enemy tutorial interaction remains selectable without becoming a light-enemy pull target");
    }

    private static void PriorityTieBreaksByAimThenDistanceThenId()
    {
        var model = new ThreadAbilityModel();
        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Right, ThreadAimMode.Hold,
            [Candidate(9, new Vector2(100f, 10f), ThreadTargetKind.Mechanism),
             Candidate(8, new Vector2(200f, 0f), ThreadTargetKind.Mechanism),
             Candidate(4, new Vector2(120f, 0f), ThreadTargetKind.Mechanism),
             Candidate(3, new Vector2(120f, 0f), ThreadTargetKind.Mechanism)]);

        TestAssert.Equal(3UL, selected!.Value.Id,
            "equal-priority candidates sort by aim dot, distance, then stable id");
    }

    private static void NeutralTapAssistRejectsOutOfPolicyTargets()
    {
        var model = new ThreadAbilityModel();
        ThreadCandidate? beyondAssist = model.SelectTarget(Vector2.Zero, Vector2.Zero, ThreadAimMode.Tap,
            [Candidate(1, new Vector2(261f, 0f), ThreadTargetKind.Mechanism, fallbackRange: 999f)]);
        ThreadCandidate? wrongDirection = model.SelectTarget(Vector2.Zero, Vector2.Left, ThreadAimMode.Tap,
            [Candidate(2, new Vector2(220f, 0f), ThreadTargetKind.Mechanism, fallbackRange: 260f)]);

        TestAssert.True(!beyondAssist.HasValue, "neutral tap never falls back beyond its global 260 pixel policy");
        TestAssert.True(!wrongDirection.HasValue, "non-neutral tap cannot ignore its aim cone through target-local fallback");
    }

    private static void HoldWithNeutralAimSelectsNothing()
    {
        var model = new ThreadAbilityModel();
        ThreadCandidate? selected = model.SelectTarget(Vector2.Zero, Vector2.Zero, ThreadAimMode.Hold,
            [Candidate(1, new Vector2(100f, 0f), ThreadTargetKind.Mechanism)]);

        TestAssert.True(!selected.HasValue, "hold requires a direction and never uses neutral tap assist");
    }

    private static void CooldownDoesNotStartUntilCastIsConsumed()
    {
        var model = new ThreadAbilityModel();

        TestAssert.True(model.CanConsume(10d), "preview cancellation leaves cooldown ready because no cast was consumed");
        TestAssert.True(model.Consume(7UL, 10d), "a valid travel entry can consume cooldown");
        TestAssert.True(!model.CanConsume(10.349d), "consumption starts the configured cooldown");
    }

    private static void ConsumingTheSameCastIsIdempotent()
    {
        var model = new ThreadAbilityModel();

        TestAssert.True(model.Consume(7UL, 10d), "first travel entry consumes its cast");
        TestAssert.True(model.Consume(7UL, 10.1d), "re-observing the same travel cast is idempotent");
        TestAssert.True(!model.CanConsume(10.349d), "idempotence does not shorten the original cooldown");
    }

    private static void DifferentCastsRespectTheCooldownBoundary()
    {
        var model = new ThreadAbilityModel();

        TestAssert.True(model.Consume(7UL, 10d), "first cast is consumed");
        TestAssert.True(!model.Consume(8UL, 10.349d), "a distinct cast is rejected before the cooldown boundary");
        TestAssert.True(model.Consume(8UL, 10.35d), "a distinct cast is accepted at the cooldown boundary");
    }

    private static ThreadCandidate Candidate(ulong id, Vector2 position, ThreadTargetKind kind = ThreadTargetKind.Anchor,
        bool hasLineOfSight = true, float fallbackRange = 0f) =>
        new(id, position, kind, hasLineOfSight, fallbackRange);
}
