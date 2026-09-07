using Godot;
using Knight.Abilities;

namespace Knight.Tests.Unit;

public static class ThreadCastModelTests
{
    private const float Frame = 1f / 60f;

    public static void Run()
    {
        ReleaseBeforeThresholdCreatesTapWindup();
        HoldingAtThresholdEntersPreview();
        PreviewReleaseLocksTheCurrentTarget();
        ValidCastSpansWindupTravelLatchApplyAndRecovery();
        TravelDurationUsesTargetOffsetFromCaster();
        CastTimingUsesInjectedSettings();
        RecoveryCancellationReturnsDirectlyToIdle();
        FailedFeedbackUsesConfiguredVisualDuration();
        CooldownPressNeverEntersPreviewOrAutoFiresWhileHeld();
        DashHurtDeathAndRoomChangeCancelPreviewWithoutCooldown();
        MissingTargetProducesNamedFailure();
        TargetLostDuringTravelNeverApplies();
    }

    private static void ReleaseBeforeThresholdCreatesTapWindup()
    {
        var model = new ThreadCastModel();
        ThreadCastFrame pressed = model.Step(Input(pressed: true, held: true), .139f, Target(10), true);
        ThreadCastFrame released = model.Step(Input(released: true), Frame, Target(10), true);

        TestAssert.Equal(ThreadCastState.Idle, pressed.State, "a press held for less than .14 seconds remains undecided");
        TestAssert.Equal(ThreadCastState.Windup, released.State, "releasing before the hold threshold starts a tap cast windup");
        TestAssert.Equal(ThreadAimMode.Tap, released.AimMode, "an early release is classified as tap aim");
        TestAssert.True(released.CastId != 0, "windup allocates a non-zero cast id");
        TestAssert.Near(4f / 60f, released.StateDurationSeconds, .0001f, "windup lasts four 60 FPS frames");
    }

    private static void HoldingAtThresholdEntersPreview()
    {
        var model = new ThreadCastModel();
        ThreadCastFrame frame = model.Step(Input(pressed: true, held: true), .14f, Target(11), true);

        TestAssert.Equal(ThreadCastState.Preview, frame.State, "holding through the exact .14 second threshold enters preview");
        TestAssert.Equal(ThreadAimMode.Hold, frame.AimMode, "preview is hold aim");
        TestAssert.Equal(0UL, frame.CastId, "preview does not allocate a cast id before release");
        TestAssert.Equal(11UL, frame.TargetId, "preview exposes its current valid target");
        TestAssert.True(frame.HasTarget, "preview reports whether it has a current target");
    }

    private static void PreviewReleaseLocksTheCurrentTarget()
    {
        var model = new ThreadCastModel();
        EnterPreview(model, Target(12));
        ThreadCastFrame frame = model.Step(Input(released: true, aim: Vector2.Up), Frame, Target(13, new Vector2(180f, 0f)), true);

        TestAssert.Equal(ThreadCastState.Windup, frame.State, "releasing preview starts windup");
        TestAssert.Equal(ThreadAimMode.Hold, frame.AimMode, "preview release remains hold aim");
        TestAssert.Equal(13UL, frame.TargetId, "release locks the target available on that frame");
        TestAssert.True(frame.HasTarget, "a valid locked target is exposed");
        TestAssert.Equal(Vector2.Up, frame.Aim, "release locks the current aim");
    }

    private static void ValidCastSpansWindupTravelLatchApplyAndRecovery()
    {
        var model = new ThreadCastModel();
        ThreadTargetSnapshot target = Target(14, new Vector2(400f, 0f));
        ThreadCastFrame frame = model.Step(Input(pressed: true, released: true), Frame, target, true);
        TestAssert.Equal(ThreadCastState.Windup, frame.State, "tap starts in windup");

        frame = Advance(model, frame, target);
        TestAssert.Equal(ThreadCastState.Travel, frame.State, "windup advances to travel");
        TestAssert.Near(.28f, frame.StateDurationSeconds, .0001f, "maximum range travel lasts .28 seconds");

        frame = Advance(model, frame, target);
        TestAssert.Equal(ThreadCastState.Latched, frame.State, "travel advances to latched");
        TestAssert.Near(5f / 60f, frame.StateDurationSeconds, .0001f, "latch lasts five frames");

        frame = Advance(model, frame, target);
        TestAssert.Equal(ThreadCastState.Apply, frame.State, "latched advances to apply");
        TestAssert.Near(3f / 60f, frame.StateDurationSeconds, .0001f, "apply release impulse lasts three frames");

        frame = Advance(model, frame, target);
        TestAssert.Equal(ThreadCastState.Recovery, frame.State, "apply advances to recovery");
        TestAssert.Near(7f / 60f, frame.StateDurationSeconds, .0001f, "recovery lasts seven frames");

        frame = Advance(model, frame, target);
        TestAssert.Equal(ThreadCastState.Idle, frame.State, "recovery returns to idle");
    }

    private static void TravelDurationUsesTargetOffsetFromCaster()
    {
        Vector2 casterWorldPosition = new(3000f, -600f);
        Vector2 targetWorldPosition = casterWorldPosition + new Vector2(200f, 0f);
        ThreadTargetSnapshot target = TargetRelativeToCaster(18, casterWorldPosition, targetWorldPosition);
        var model = new ThreadCastModel();

        ThreadCastFrame frame = model.Step(Input(pressed: true, released: true), Frame, target, true);
        frame = Advance(model, frame, target);

        TestAssert.Equal(ThreadCastState.Travel, frame.State, "the cast reaches travel using a target offset from a non-zero world position");
        TestAssert.Near(.18f, frame.StateDurationSeconds, .0001f,
            "travel duration uses the 200-pixel offset from caster rather than the target world coordinate");
    }

    private static void CastTimingUsesInjectedSettings()
    {
        var settings = new ThreadAbilitySettings(
            TapThresholdSeconds: .2f,
            WindupSeconds: .11f,
            MinimumTravelSeconds: .12f,
            MaximumTravelSeconds: .32f,
            TravelDistanceForMaximumDuration: 500f,
            LatchSeconds: .13f,
            ApplySeconds: .14f,
            RecoverySeconds: .15f);
        var model = new ThreadCastModel(settings);

        ThreadCastFrame undecided = model.Step(Input(pressed: true, held: true), .199f, Target(19), true);
        ThreadCastFrame preview = model.Step(Input(held: true), .001f, Target(19), true);
        ThreadCastFrame windup = model.Step(Input(released: true), Frame, Target(19, new Vector2(250f, 0f)), true);
        ThreadCastFrame travel = Advance(model, windup, Target(19, new Vector2(250f, 0f)));

        TestAssert.Equal(ThreadCastState.Idle, undecided.State, "configured tap threshold delays hold preview");
        TestAssert.Equal(ThreadCastState.Preview, preview.State, "configured tap threshold enters hold preview at its boundary");
        TestAssert.Near(.11f, windup.StateDurationSeconds, .0001f, "configured windup duration is consumed by the cast model");
        TestAssert.Near(.22f, travel.StateDurationSeconds, .0001f,
            "configured travel range and durations determine travel time");

        ThreadCastFrame latched = Advance(model, travel, Target(19, new Vector2(250f, 0f)));
        ThreadCastFrame apply = Advance(model, latched, Target(19, new Vector2(250f, 0f)));
        ThreadCastFrame recovery = Advance(model, apply, Target(19, new Vector2(250f, 0f)));
        TestAssert.Near(.13f, latched.StateDurationSeconds, .0001f, "configured latch duration is consumed by the cast model");
        TestAssert.Near(.14f, apply.StateDurationSeconds, .0001f, "configured apply duration is consumed by the cast model");
        TestAssert.Near(.15f, recovery.StateDurationSeconds, .0001f, "configured recovery duration is consumed by the cast model");
    }

    private static void CooldownPressNeverEntersPreviewOrAutoFiresWhileHeld()
    {
        var settings = new ThreadAbilitySettings(CooldownSeconds: 1f);
        var ability = new ThreadAbilityModel(settings);
        var model = new ThreadCastModel(settings);
        ThreadTargetSnapshot target = Target(20);

        ThreadCastFrame first = model.Step(Input(pressed: true, released: true), Frame, target, true);
        first = Advance(model, first, target);
        TestAssert.Equal(ThreadCastState.Travel, first.State,
            "cooldown setup consumes only after a real cast reaches Travel");
        TestAssert.True(ability.Consume(first.CastId, 0d),
            "cooldown setup consumes the first real CastId exactly once");
        while (first.State != ThreadCastState.Idle)
            first = model.Step(Input(), Frame, target, ability.CanConsume(.1d));

        ThreadCastFrame feedback = model.Step(Input(pressed: true, held: true), Frame, target,
            ability.CanConsume(.1d));
        TestAssert.Equal(ThreadCastState.Failed, feedback.State,
            "a fresh cooldown press produces feedback without entering Hold Preview");
        TestAssert.Equal(ThreadFailureReason.Cooldown, feedback.Failure,
            "cooldown feedback remains named on the press edge");
        TestAssert.True(!feedback.HasTarget && feedback.TargetId == 0,
            "cooldown feedback never locks or publishes the available target");

        ThreadCastFrame held = feedback;
        for (int index = 0; index < 30; index++)
            held = model.Step(Input(held: true), Frame, target, cooldownReady: index >= 12);
        TestAssert.Equal(ThreadCastState.Idle, held.State,
            "holding through cooldown expiry returns to Idle without automatically casting");
        TestAssert.Equal(0UL, held.CastId,
            "cooldown expiry while still held cannot allocate a cast without a new press");
    }

    private static void FailedFeedbackUsesConfiguredVisualDuration()
    {
        const float visualSeconds = .12f;
        var model = new ThreadCastModel(new ThreadAbilitySettings(VisualSeconds: visualSeconds));
        ThreadTargetSnapshot missing = new(0, Vector2.Zero, false, ThreadFailureReason.None);

        ThreadCastFrame failed = model.Step(Input(pressed: true, released: true), Frame, missing, true);
        TestAssert.Equal(ThreadCastState.Failed, failed.State, "missing target enters Failed");
        TestAssert.Near(visualSeconds, failed.StateDurationSeconds, .0001f,
            "Failed duration comes from the centralized visual setting");
        TestAssert.Near(visualSeconds, failed.StateRemainingSeconds, .0001f,
            "Failed begins with the full observable visual window");

        for (int index = 0; index < 3; index++)
        {
            failed = model.Step(Input(), Frame, missing, true);
            TestAssert.Equal(ThreadCastState.Failed, failed.State,
                "Failed remains observable for multiple 60 FPS physics frames");
        }

        for (int index = 0; index < 12 && failed.State != ThreadCastState.Idle; index++)
            failed = model.Step(Input(), Frame, missing, true);
        TestAssert.Equal(ThreadCastState.Idle, failed.State,
            "Failed returns controllably to Idle after its configured visual window");
    }

    private static void RecoveryCancellationReturnsDirectlyToIdle()
    {
        var model = new ThreadCastModel();
        ThreadTargetSnapshot target = Target(21, new Vector2(120f, 0f));
        ThreadCastFrame frame = model.Step(Input(pressed: true, released: true), Frame, target, true);
        while (frame.State != ThreadCastState.Recovery)
            frame = model.Step(Input(), Frame, target, true);
        ulong appliedCastId = frame.CastId;

        ThreadCastFrame cancelled = model.Step(Input(cancelled: true), Frame, target, false);
        TestAssert.Equal(ThreadCastState.Idle, cancelled.State,
            "cancelling Recovery drops only the completed action presentation");
        TestAssert.Equal(ThreadFailureReason.None, cancelled.Failure,
            "Recovery movement cancellation does not publish a failure");
        TestAssert.Equal(0UL, cancelled.CastId,
            "cancelled Recovery clears the completed cast frame");
        TestAssert.True(appliedCastId != 0,
            "recovery cancellation setup represents an already applied real CastId");
    }

    private static void DashHurtDeathAndRoomChangeCancelPreviewWithoutCooldown()
    {
        foreach (string reason in new[] { "dash", "hurt", "death", "room change" })
        {
            var model = new ThreadCastModel();
            EnterPreview(model, Target(15));
            ThreadCastFrame cancelled = model.Step(Input(cancelled: true), Frame, Target(15), true);

            TestAssert.Equal(ThreadCastState.Failed, cancelled.State, $"{reason} cancellation is observable as failure");
            TestAssert.Equal(ThreadFailureReason.Cancelled, cancelled.Failure, $"{reason} has a named cancellation reason");
            while (cancelled.State != ThreadCastState.Idle)
                cancelled = model.Step(Input(), Frame, Target(15), true);
            ThreadCastFrame retry = model.Step(Input(pressed: true, released: true), Frame, Target(16), true);
            TestAssert.Equal(ThreadCastState.Windup, retry.State, $"{reason} cancellation does not consume cooldown");
        }
    }

    private static void MissingTargetProducesNamedFailure()
    {
        var model = new ThreadCastModel();
        ThreadCastFrame frame = model.Step(Input(pressed: true, released: true), Frame,
            new ThreadTargetSnapshot(0, Vector2.Zero, false, ThreadFailureReason.None), true);

        TestAssert.Equal(ThreadCastState.Failed, frame.State, "casting without a target fails");
        TestAssert.Equal(ThreadFailureReason.NoTarget, frame.Failure, "a missing target uses the named no-target failure");
    }

    private static void TargetLostDuringTravelNeverApplies()
    {
        var model = new ThreadCastModel();
        ThreadTargetSnapshot target = Target(17, new Vector2(1f, 0f));
        ThreadCastFrame frame = model.Step(Input(pressed: true, released: true), Frame, target, true);
        frame = Advance(model, frame, target);
        TestAssert.Equal(ThreadCastState.Travel, frame.State, "the test enters travel before losing target");

        frame = model.Step(Input(), Frame, new ThreadTargetSnapshot(17, Vector2.Zero, false, ThreadFailureReason.TargetLost), true);
        TestAssert.Equal(ThreadCastState.Failed, frame.State, "losing a travel target fails the cast");
        TestAssert.Equal(ThreadFailureReason.TargetLost, frame.Failure, "travel loss has the named target-lost failure");

        for (int index = 0; index < 30; index++)
        {
            frame = model.Step(Input(), Frame, new ThreadTargetSnapshot(17, Vector2.Zero, false, ThreadFailureReason.TargetLost), true);
            TestAssert.True(frame.State != ThreadCastState.Apply, "a lost target can never reach apply");
        }
    }

    private static void EnterPreview(ThreadCastModel model, ThreadTargetSnapshot target)
    {
        ThreadCastFrame frame = model.Step(Input(pressed: true, held: true), .14f, target, true);
        TestAssert.Equal(ThreadCastState.Preview, frame.State, "test setup reaches preview");
    }

    private static ThreadCastFrame Advance(ThreadCastModel model, ThreadCastFrame frame, ThreadTargetSnapshot target)
    {
        int frames = Mathf.CeilToInt(frame.StateRemainingSeconds / Frame);
        for (int index = 0; index < frames; index++)
            frame = model.Step(Input(), Frame, target, true);
        return frame;
    }

    private static ThreadCastInput Input(bool pressed = false, bool held = false, bool released = false,
        Vector2? aim = null, bool cancelled = false) =>
        new(pressed, held, released, aim ?? Vector2.Right, cancelled);

    private static ThreadTargetSnapshot Target(ulong id, Vector2? position = null) =>
        new(id, position ?? new Vector2(120f, 0f), true, ThreadFailureReason.None);

    private static ThreadTargetSnapshot TargetRelativeToCaster(ulong id, Vector2 casterWorldPosition,
        Vector2 targetWorldPosition) =>
        new(id, targetWorldPosition - casterWorldPosition, true, ThreadFailureReason.None);
}
