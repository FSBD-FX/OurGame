using System;
using Godot;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class PlayerPoseMixerTests
{
    public static void Run()
    {
        ActionLayerOverridesOnlyOwnedUpperBodySlots();
        LocomotionContinuesUnderThreadPreview();
        PoseInterpolationIsContinuousAtPhaseBoundaries();
        RecoveryCancellationReturnsToCurrentLocomotion();
        ActionFramesClampAndRejectInvalidValues();
        EveryLocomotionStateRoundTripsThroughPresentationFrame();
        LocomotionFramesClampAndRejectInvalidValues();
        ThreeArgumentPresentationCompatibilityMapsCoarseLocomotion();
        MixerOwnershipTracksOnlyEffectiveLayers();
        ShortestAngleInterpolationUsesMidpointAndBoundedEndpoints();
        ExtremeFinitePoseInterpolationStaysFinite();
    }

    private static void ActionLayerOverridesOnlyOwnedUpperBodySlots()
    {
        PlayerPose locomotion = PlayerPose.Identity
            .With(PlayerPoseSlot.Body, Transform(10f, 1.0f))
            .With(PlayerPoseSlot.NearArm, Transform(20f, 0.2f))
            .With(PlayerPoseSlot.FarArm, Transform(30f, 0.3f))
            .With(PlayerPoseSlot.NearLeg, Transform(40f, 0.4f))
            .With(PlayerPoseSlot.FarLeg, Transform(50f, 0.5f))
            .With(PlayerPoseSlot.Weapon, Transform(60f, 0.6f));
        PlayerPose action = PlayerPose.Identity
            .With(PlayerPoseSlot.Body, Transform(110f, -1.0f))
            .WithOwned(PlayerPoseSlot.NearArm, Transform(120f, -0.2f))
            .WithOwned(PlayerPoseSlot.FarArm, Transform(130f, -0.3f))
            .With(PlayerPoseSlot.NearLeg, Transform(140f, -0.4f))
            .With(PlayerPoseSlot.FarLeg, Transform(150f, -0.5f))
            .WithOwned(PlayerPoseSlot.Weapon, Transform(160f, -0.6f));

        PlayerPose mixed = PlayerPoseMixer.Mix(locomotion, action, 1f);

        TestAssert.Equal(locomotion.Get(PlayerPoseSlot.Body), mixed.Get(PlayerPoseSlot.Body),
            "an unowned action Body transform cannot overwrite locomotion");
        TestAssert.Equal(action.Get(PlayerPoseSlot.NearArm), mixed.Get(PlayerPoseSlot.NearArm),
            "the action layer owns and overrides NearArm");
        TestAssert.Equal(action.Get(PlayerPoseSlot.FarArm), mixed.Get(PlayerPoseSlot.FarArm),
            "the action layer owns and overrides FarArm");
        TestAssert.Equal(locomotion.Get(PlayerPoseSlot.NearLeg), mixed.Get(PlayerPoseSlot.NearLeg),
            "an action cannot overwrite the locomotion-owned NearLeg");
        TestAssert.Equal(locomotion.Get(PlayerPoseSlot.FarLeg), mixed.Get(PlayerPoseSlot.FarLeg),
            "an action cannot overwrite the locomotion-owned FarLeg");
        TestAssert.Equal(action.Get(PlayerPoseSlot.Weapon), mixed.Get(PlayerPoseSlot.Weapon),
            "the action layer owns and overrides Weapon");
        TestAssert.Equal(Transform(110f, -1.0f), action.Get(PlayerPoseSlot.Body),
            "mixing does not mutate the action input pose");
        TestAssert.Equal(Transform(10f, 1.0f), locomotion.Get(PlayerPoseSlot.Body),
            "mixing does not mutate the locomotion input pose");
    }

    private static void LocomotionContinuesUnderThreadPreview()
    {
        PlayerPose running = PlayerPose.Identity
            .With(PlayerPoseSlot.Body, Transform(8f, 0.08f))
            .With(PlayerPoseSlot.NearLeg, Transform(18f, 0.35f))
            .With(PlayerPoseSlot.FarLeg, Transform(-18f, -0.35f))
            .With(PlayerPoseSlot.NearArm, Transform(4f, 0.10f));
        PlayerPose preview = PlayerPose.Identity
            .WithOwned(PlayerPoseSlot.NearArm, Transform(28f, 0.75f))
            .WithOwned(PlayerPoseSlot.ShoulderCore, Transform(12f, 0.40f));

        PlayerPose mixed = PlayerPoseMixer.Mix(running, preview, 0.65f);

        TestAssert.Equal(running.Get(PlayerPoseSlot.Body), mixed.Get(PlayerPoseSlot.Body),
            "thread preview leaves the running torso motion visible");
        TestAssert.Equal(running.Get(PlayerPoseSlot.NearLeg), mixed.Get(PlayerPoseSlot.NearLeg),
            "thread preview leaves the near-leg run pose visible");
        TestAssert.Equal(running.Get(PlayerPoseSlot.FarLeg), mixed.Get(PlayerPoseSlot.FarLeg),
            "thread preview leaves the far-leg run pose visible");
        TestAssert.True(mixed.Get(PlayerPoseSlot.NearArm).RotationRadians > 0.10f,
            "thread preview blends its owned aiming arm over locomotion");
    }

    private static void PoseInterpolationIsContinuousAtPhaseBoundaries()
    {
        PoseTransform2D start = Transform(0f, Mathf.DegToRad(179f)) with
        {
            Scale = new Vector2(0.8f, 1.2f),
            Modulate = new Color(0.2f, 0.4f, 0.6f, 0.8f)
        };
        PoseTransform2D end = Transform(100f, Mathf.DegToRad(-179f)) with
        {
            Scale = new Vector2(1.2f, 0.8f),
            Modulate = new Color(0.8f, 0.6f, 0.4f, 0.2f)
        };

        PoseTransform2D before = PoseTransform2D.Interpolate(start, end, 0.49f);
        PoseTransform2D boundary = PoseTransform2D.Interpolate(start, end, 0.50f);
        PoseTransform2D after = PoseTransform2D.Interpolate(start, end, 0.51f);

        TestAssert.Near(49f, before.Position.X, 0.0001f,
            "position interpolation approaches the phase boundary continuously");
        TestAssert.True(AngleDistance(before.RotationRadians, boundary.RotationRadians) < 0.001f,
            "shortest-angle rotation has no pop immediately before a phase boundary");
        TestAssert.True(AngleDistance(boundary.RotationRadians, after.RotationRadians) < 0.001f,
            "shortest-angle rotation has no pop immediately after a phase boundary");
        TestAssert.Near(1f, boundary.Scale.X, 0.0001f,
            "scale interpolates at the phase boundary");
        TestAssert.Near(1f, boundary.Scale.Y, 0.0001f,
            "both scale axes interpolate at the phase boundary");
        TestAssert.Near(0.5f, boundary.Modulate.R, 0.0001f,
            "red modulation interpolates at the phase boundary");
        TestAssert.Near(0.5f, boundary.Modulate.A, 0.0001f,
            "alpha modulation interpolates at the phase boundary");
        TestAssert.True(AngleDistance(start.RotationRadians, end.RotationRadians) < Mathf.DegToRad(2.01f),
            "the fixture crosses the wrap boundary by only two degrees");
    }

    private static void RecoveryCancellationReturnsToCurrentLocomotion()
    {
        PlayerPose currentLocomotion = PlayerPose.Identity
            .With(PlayerPoseSlot.Body, Transform(7f, 0.11f))
            .With(PlayerPoseSlot.NearArm, Transform(17f, 0.22f))
            .With(PlayerPoseSlot.NearLeg, Transform(27f, 0.33f));
        PlayerPose recovery = PlayerPose.Identity
            .WithOwned(PlayerPoseSlot.Body, Transform(70f, -0.11f))
            .WithOwned(PlayerPoseSlot.NearArm, Transform(170f, -0.22f));
        var actionFrame = new PlayerActionFrame(
            PlayerActionState.GroundAttack,
            PlayerActionPhase.Recovery,
            0.72f,
            RecoveryCancellable: true);

        PlayerPose cancelled = PlayerPoseMixer.Mix(currentLocomotion, recovery,
            actionFrame.RecoveryCancellable ? 0f : 1f);

        TestAssert.Equal(currentLocomotion, cancelled,
            "cancelling Recovery removes the action layer and returns to the current locomotion pose");
    }

    private static void ActionFramesClampAndRejectInvalidValues()
    {
        var below = new PlayerActionFrame(
            State: PlayerActionState.GroundAttack,
            Phase: PlayerActionPhase.Anticipation,
            NormalizedTime: -2f,
            RecoveryCancellable: false);
        var above = new PlayerActionFrame(PlayerActionState.ThreadLatch,
            PlayerActionPhase.Contact, 4f, false);

        TestAssert.Near(0f, below.NormalizedTime, 0f,
            "action normalized time clamps below zero");
        TestAssert.Near(1f, above.NormalizedTime, 0f,
            "action normalized time clamps above one");
        AssertThrows<ArgumentOutOfRangeException>(() => new PlayerActionFrame(
            PlayerActionState.GroundAttack, PlayerActionPhase.Anticipation, float.NaN, false),
            "non-finite action time is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => new PlayerActionFrame(
            PlayerActionState.GroundAttack, PlayerActionPhase.Burst, 0.5f, true),
            "only Recovery may be declared cancellable");
    }

    private static void EveryLocomotionStateRoundTripsThroughPresentationFrame()
    {
        PlayerLocomotionState[] states =
        {
            PlayerLocomotionState.Idle,
            PlayerLocomotionState.StartRun,
            PlayerLocomotionState.Run,
            PlayerLocomotionState.Brake,
            PlayerLocomotionState.JumpStart,
            PlayerLocomotionState.Rise,
            PlayerLocomotionState.Apex,
            PlayerLocomotionState.Fall,
            PlayerLocomotionState.SoftLand,
            PlayerLocomotionState.HardLand,
            PlayerLocomotionState.Dash,
            PlayerLocomotionState.Dead,
            PlayerLocomotionState.Recovery
        };
        var baseFrame = new EntityPresentationFrame(EntityVisualState.Move, -1, true);

        for (int index = 0; index < states.Length; index++)
        {
            float progress = (index + 1f) / states.Length;
            var locomotion = new PlayerLocomotionFrame(states[index], progress);
            var presentation = new PlayerPresentationFrame(
                baseFrame,
                locomotion,
                PlayerActionFrame.None,
                SecondaryMotionFrame.Identity);

            TestAssert.Equal(states[index], presentation.Locomotion.State,
                $"presentation round-trips authoritative locomotion state {states[index]}");
            TestAssert.Near(progress, presentation.Locomotion.NormalizedTime, 0.000001f,
                $"presentation round-trips locomotion progress for {states[index]}");
        }
    }

    private static void LocomotionFramesClampAndRejectInvalidValues()
    {
        TestAssert.Near(0f,
            new PlayerLocomotionFrame(PlayerLocomotionState.StartRun, -4f).NormalizedTime,
            0f, "locomotion normalized time clamps below zero");
        TestAssert.Near(1f,
            new PlayerLocomotionFrame(PlayerLocomotionState.HardLand, 4f).NormalizedTime,
            0f, "locomotion normalized time clamps above one");
        AssertThrows<ArgumentOutOfRangeException>(() => new PlayerLocomotionFrame(
            (PlayerLocomotionState)999, 0.5f),
            "undefined locomotion state is rejected");
        AssertThrows<ArgumentOutOfRangeException>(() => new PlayerLocomotionFrame(
            PlayerLocomotionState.Run, float.NegativeInfinity),
            "non-finite locomotion progress is rejected");
    }

    private static void ThreeArgumentPresentationCompatibilityMapsCoarseLocomotion()
    {
        var frame = new PlayerPresentationFrame(
            new EntityPresentationFrame(EntityVisualState.Move, 1, true),
            PlayerActionFrame.None,
            SecondaryMotionFrame.Identity);

        TestAssert.Equal(PlayerLocomotionState.Run, frame.Locomotion.State,
            "the narrow three-argument adapter maps legacy Move to Run");
        TestAssert.Near(0f, frame.Locomotion.NormalizedTime, 0f,
            "the legacy adapter cannot invent detailed locomotion progress");
    }

    private static void MixerOwnershipTracksOnlyEffectiveLayers()
    {
        PlayerPose locomotion = PlayerPose.Identity
            .WithOwnership(PlayerPoseOwnership.Body | PlayerPoseOwnership.NearLeg |
                PlayerPoseOwnership.FarLeg);
        PlayerPose action = PlayerPose.Identity
            .WithOwned(PlayerPoseSlot.NearArm, Transform(10f, 0.2f))
            .WithOwned(PlayerPoseSlot.Weapon, Transform(20f, 0.4f));

        PlayerPose zeroWeight = PlayerPoseMixer.Mix(locomotion, action, 0f);
        PlayerPose positiveWeight = PlayerPoseMixer.Mix(locomotion, action, 0.01f);

        TestAssert.Equal(locomotion.Ownership, zeroWeight.Ownership,
            "zero action weight preserves the exact locomotion ownership mask");
        TestAssert.Equal(locomotion.Ownership | action.Ownership, positiveWeight.Ownership,
            "positive action weight unions locomotion and action ownership masks");
    }

    private static void ShortestAngleInterpolationUsesMidpointAndBoundedEndpoints()
    {
        PoseTransform2D start = PoseTransform2D.Identity with
        {
            RotationRadians = Mathf.DegToRad(179f)
        };
        PoseTransform2D end = PoseTransform2D.Identity with
        {
            RotationRadians = Mathf.DegToRad(-179f)
        };

        PoseTransform2D midpoint = PoseTransform2D.Interpolate(start, end, 0.5f);
        PoseTransform2D epsilonStart = PoseTransform2D.Interpolate(start, end, 0.001f);
        PoseTransform2D epsilonEnd = PoseTransform2D.Interpolate(start, end, 0.999f);

        TestAssert.True(AngleDistance(midpoint.RotationRadians, Mathf.Pi) < 0.00001f,
            "179 to -179 degrees crosses through the wrapped 180-degree midpoint");
        TestAssert.True(AngleDistance(start.RotationRadians, epsilonStart.RotationRadians) > 0f &&
            AngleDistance(start.RotationRadians, epsilonStart.RotationRadians) < Mathf.DegToRad(0.01f),
            "epsilon weight advances a bounded amount from the start rotation");
        TestAssert.True(AngleDistance(end.RotationRadians, epsilonEnd.RotationRadians) > 0f &&
            AngleDistance(end.RotationRadians, epsilonEnd.RotationRadians) < Mathf.DegToRad(0.01f),
            "one-minus-epsilon weight remains a bounded amount before the end rotation");
        TestAssert.True(AngleDistance(epsilonStart.RotationRadians, end.RotationRadians) >
            Mathf.DegToRad(1.9f),
            "positive interpolation weight cannot hard-cut directly to the destination rotation");
    }

    private static void ExtremeFinitePoseInterpolationStaysFinite()
    {
        PoseTransform2D positive = new(
            new Vector2(float.MaxValue, float.MaxValue),
            float.MaxValue,
            new Vector2(float.MaxValue, float.MaxValue),
            new Color(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue));
        PoseTransform2D negative = new(
            new Vector2(-float.MaxValue, -float.MaxValue),
            -float.MaxValue,
            new Vector2(-float.MaxValue, -float.MaxValue),
            new Color(-float.MaxValue, -float.MaxValue, -float.MaxValue, -float.MaxValue));

        PoseTransform2D midpoint = PoseTransform2D.Interpolate(positive, negative, 0.5f);

        TestAssert.True(IsFinite(midpoint.Position.X) && IsFinite(midpoint.Position.Y),
            "extreme finite position endpoints interpolate to finite output");
        TestAssert.True(IsFinite(midpoint.RotationRadians),
            "extreme finite rotation endpoints interpolate to finite output");
        TestAssert.True(IsFinite(midpoint.Scale.X) && IsFinite(midpoint.Scale.Y),
            "extreme finite scale endpoints interpolate to finite output");
        TestAssert.True(IsFinite(midpoint.Modulate.R) && IsFinite(midpoint.Modulate.G) &&
            IsFinite(midpoint.Modulate.B) && IsFinite(midpoint.Modulate.A),
            "extreme finite modulation endpoints interpolate to finite output");
    }

    private static PoseTransform2D Transform(float x, float rotation) =>
        new(new Vector2(x, x * 0.25f), rotation, new Vector2(1f + x * 0.001f, 1f),
            new Color(1f, 1f, 1f, 1f));

    private static float AngleDistance(float first, float second) =>
        MathF.Abs(MathF.Atan2(MathF.Sin(second - first), MathF.Cos(second - first)));

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static void AssertThrows<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Assertion failed: {message}; expected {typeof(T).Name}");
    }
}
