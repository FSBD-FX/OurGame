using Godot;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class PlayerCombatPoseTests
{
    public static void Run()
    {
        GroundAttackReversesWindupBeforeForwardSweepWithoutChangingReach();
        ShieldBuildsAReadableSupportedUpperBodyWithoutOwningTheLegs();
        ShieldAimUsesTheSameFacingLocalPoseOnBothSides();
        ShieldRecoveryReturnsSmoothlyToLocomotion();
        MidRecoveryShieldToAttackHandoffStartsFromTheDisplayedGuard();
        DeathImmediatelyClearsTheGuardOverlay();
    }

    private static void GroundAttackReversesWindupBeforeForwardSweepWithoutChangingReach()
    {
        PlayerFormalVisual rig = NewRig();
        try
        {
            Vector2 rootPosition = rig.Position;
            PlayerPose anticipation = Apply(rig, Action(PlayerActionState.GroundAttack,
                PlayerActionPhase.Anticipation, 1f));
            PlayerPose burst = Apply(rig, Action(PlayerActionState.GroundAttack,
                PlayerActionPhase.Burst, 1f));
            PlayerPose contact = Apply(rig, Action(PlayerActionState.GroundAttack,
                PlayerActionPhase.Contact, 1f));
            PlayerPose recovery = Apply(rig, Action(PlayerActionState.GroundAttack,
                PlayerActionPhase.Recovery, 1f));

            TestAssert.True(anticipation.NearArm.RotationRadians > .02f &&
                anticipation.Weapon.RotationRadians > .05f,
                "ground attack visibly draws its arm and unchanged-length weapon back before release");
            TestAssert.True(SilhouetteDistance(anticipation, burst) > 2f,
                "ground attack windup and forward burst have a large readable upper-body silhouette difference");
            TestAssert.True(burst.NearArm.RotationRadians < -.95f &&
                burst.Weapon.RotationRadians < -.85f,
                "ground attack burst commits both the striking arm and weapon forward");
            TestAssert.True(contact.NearArm.RotationRadians > burst.NearArm.RotationRadians &&
                contact.Weapon.RotationRadians > burst.Weapon.RotationRadians,
                "ground attack contact follows through from the burst instead of holding the same silhouette");
            TestAssert.True(SilhouetteMagnitude(recovery) < .001f,
                "ground attack recovery reaches the neutral upper-body target before gameplay returns to idle");
            TestAssert.True(anticipation.Weapon.Scale.IsEqualApprox(Vector2.One) &&
                burst.Weapon.Scale.IsEqualApprox(Vector2.One) &&
                contact.Weapon.Scale.IsEqualApprox(Vector2.One),
                "readability comes from pose timing and rotation, never weapon scaling");
            TestAssert.Equal(rootPosition, rig.Position,
                "attack poses never move the formal rig root");
            TestAssert.True(!burst.Owns(PlayerPoseSlot.NearLeg) &&
                !burst.Owns(PlayerPoseSlot.FarLeg),
                "attack poses leave both locomotion legs authoritative");
        }
        finally { rig.Free(); }
    }

    private static void ShieldBuildsAReadableSupportedUpperBodyWithoutOwningTheLegs()
    {
        PlayerFormalVisual rig = NewRig();
        try
        {
            Vector2 rootPosition = rig.Position;
            PlayerPose guard = Apply(rig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Contact, 1f, Vector2.Right));

            TestAssert.True(Mathf.Abs(guard.Body.RotationRadians) > .06f &&
                Mathf.Abs(guard.NearArm.RotationRadians) > .85f &&
                Mathf.Abs(guard.FarArm.RotationRadians) > .45f &&
                Mathf.Abs(guard.Weapon.RotationRadians) > .35f,
                "held shield braces the torso, both arms, and weapon instead of leaving a neutral player inside the fan");
            TestAssert.True(guard.Owns(PlayerPoseSlot.Body) &&
                guard.Owns(PlayerPoseSlot.NearArm) && guard.Owns(PlayerPoseSlot.FarArm) &&
                guard.Owns(PlayerPoseSlot.Weapon) && guard.Owns(PlayerPoseSlot.Mantle),
                "shield owns every visible defensive upper-body layer including its supporting mantle");
            TestAssert.True(!guard.Owns(PlayerPoseSlot.NearLeg) &&
                !guard.Owns(PlayerPoseSlot.FarLeg),
                "shield leaves both locomotion legs authoritative");
            TestAssert.True(guard.Mantle.Position.Length() > 1f ||
                Mathf.Abs(guard.Mantle.RotationRadians) > .08f,
                "held shield loads the mantle into a visible supporting counter-shape");
            TestAssert.True(guard.Weapon.Scale.IsEqualApprox(Vector2.One),
                "shield does not enlarge the weapon to fabricate coverage");
            TestAssert.Equal(rootPosition, rig.Position,
                "shield poses never move the formal rig root");
        }
        finally { rig.Free(); }
    }

    private static void ShieldAimUsesTheSameFacingLocalPoseOnBothSides()
    {
        PlayerFormalVisual rightRig = NewRig();
        PlayerFormalVisual leftRig = NewRig();
        try
        {
            PlayerPose right = Apply(rightRig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Contact, 1f, new Vector2(1f, -.5f)), facing: 1);
            PlayerPose left = Apply(leftRig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Contact, 1f, new Vector2(-1f, -.5f)), facing: -1);

            AssertLocalRotationEqual(right.Body, left.Body, "shield Body");
            AssertLocalRotationEqual(right.NearArm, left.NearArm, "shield NearArm");
            AssertLocalRotationEqual(right.FarArm, left.FarArm, "shield FarArm");
            AssertLocalRotationEqual(right.Weapon, left.Weapon, "shield Weapon");
            TestAssert.Near(1f, rightRig.Scale.X, .0001f,
                "right-facing guard keeps the formal root unmirrored");
            TestAssert.Near(-1f, leftRig.Scale.X, .0001f,
                "left-facing guard mirrors the formal root exactly once");
        }
        finally
        {
            rightRig.Free();
            leftRig.Free();
        }
    }

    private static void ShieldRecoveryReturnsSmoothlyToLocomotion()
    {
        PlayerFormalVisual rig = NewRig();
        try
        {
            Apply(rig, PlayerActionFrame.None);
            PlayerPose locomotion = rig.CurrentPose;
            Apply(rig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Contact, 1f, Vector2.Right));
            float held = PoseDistance(rig.CurrentPose, locomotion);
            Apply(rig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Recovery, .5f, Vector2.Right));
            float middle = PoseDistance(rig.CurrentPose, locomotion);
            Apply(rig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Recovery, .99f, Vector2.Right));
            float terminal = PoseDistance(rig.CurrentPose, locomotion);
            Apply(rig, PlayerActionFrame.None);
            float idle = PoseDistance(rig.CurrentPose, locomotion);

            TestAssert.True(held > middle && middle > terminal,
                "shield Recovery monotonically releases the displayed defensive pose");
            TestAssert.True(middle > held * .35f && middle < held * .65f,
                "shield Recovery midpoint visibly preserves about half of the guard instead of popping off");
            TestAssert.True(terminal < .03f && idle < .03f,
                "shield terminal Recovery and following None frame meet the locomotion pose continuously");
        }
        finally { rig.Free(); }
    }

    private static void MidRecoveryShieldToAttackHandoffStartsFromTheDisplayedGuard()
    {
        PlayerFormalVisual rig = NewRig();
        try
        {
            Apply(rig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Contact, 1f, Vector2.Right));
            Apply(rig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Recovery, .5f, Vector2.Right));
            PlayerPose guarded = rig.CurrentPose;
            Apply(rig, Action(PlayerActionState.GroundAttack,
                PlayerActionPhase.Anticipation, 0f, Vector2.Right));
            PlayerPose firstAttack = rig.CurrentPose;

            TestAssert.True(SilhouetteDistance(guarded, firstAttack) < .001f &&
                guarded.Mantle.Position.DistanceTo(firstAttack.Mantle.Position) < .001f &&
                Mathf.Abs(Mathf.AngleDifference(guarded.Mantle.RotationRadians,
                    firstAttack.Mantle.RotationRadians)) < .001f,
                "mid-Recovery shield-to-attack handoff begins at the actually displayed partial guard, not a full-guard or neutral-frame pop");
            Apply(rig, Action(PlayerActionState.GroundAttack,
                PlayerActionPhase.Anticipation, 1f, Vector2.Right));
            TestAssert.True(SilhouetteDistance(guarded, rig.CurrentPose) > .5f,
                "attack Anticipation then advances away from the captured guard pose");
            TestAssert.True(!rig.CurrentActionPose.Owns(PlayerPoseSlot.Mantle),
                "completed shield-to-attack transition returns mantle ownership to locomotion");
        }
        finally { rig.Free(); }
    }

    private static void DeathImmediatelyClearsTheGuardOverlay()
    {
        PlayerFormalVisual guardedRig = NewRig();
        PlayerFormalVisual deadRig = NewRig();
        try
        {
            Apply(guardedRig, Action(PlayerActionState.Shield,
                PlayerActionPhase.Contact, 1f, Vector2.Right));
            Apply(deadRig, PlayerActionFrame.None);
            guardedRig.ApplyPlayerFrame(Frame(PlayerActionFrame.None,
                PlayerLocomotionState.Dead));
            deadRig.ApplyPlayerFrame(Frame(PlayerActionFrame.None,
                PlayerLocomotionState.Dead));

            TestAssert.True(PoseDistance(guardedRig.CurrentPose, deadRig.CurrentPose) < .001f,
                "death immediately owns the complete rig instead of retaining a fading shield overlay");
        }
        finally
        {
            guardedRig.Free();
            deadRig.Free();
        }
    }

    private static PlayerFormalVisual NewRig()
    {
        PackedScene scene = GD.Load<PackedScene>(
            "res://scenes/presentation/PlayerFormalVisual.tscn");
        TestAssert.True(scene != null, "formal player leaf scene loads for combat pose verification");
        return scene.Instantiate<PlayerFormalVisual>();
    }

    private static PlayerPose Apply(PlayerFormalVisual rig, PlayerActionFrame action, int facing = 1)
    {
        rig.ApplyPlayerFrame(Frame(action, PlayerLocomotionState.Idle, facing));
        return rig.CurrentActionPose;
    }

    private static PlayerPresentationFrame Frame(PlayerActionFrame action,
        PlayerLocomotionState locomotion = PlayerLocomotionState.Idle, int facing = 1) => new(
        new EntityPresentationFrame(locomotion == PlayerLocomotionState.Dead
            ? EntityVisualState.Dead : EntityVisualState.Idle, facing, true),
        new PlayerLocomotionFrame(locomotion, locomotion == PlayerLocomotionState.Dead ? .65f : 0f),
        action,
        SecondaryMotionFrame.Identity);

    private static PlayerActionFrame Action(PlayerActionState state, PlayerActionPhase phase,
        float progress, Vector2 aim = default) => new(state, phase, progress,
        RecoveryCancellable: phase == PlayerActionPhase.Recovery, aim);

    private static float SilhouetteDistance(PlayerPose first, PlayerPose second) =>
        RotationDistance(first.Body, second.Body) +
        RotationDistance(first.NearArm, second.NearArm) +
        RotationDistance(first.Weapon, second.Weapon);

    private static float PoseDistance(PlayerPose first, PlayerPose second) =>
        SilhouetteDistance(first, second) +
        RotationDistance(first.FarArm, second.FarArm) +
        RotationDistance(first.Mantle, second.Mantle) +
        first.Body.Position.DistanceTo(second.Body.Position) +
        first.NearArm.Position.DistanceTo(second.NearArm.Position) +
        first.FarArm.Position.DistanceTo(second.FarArm.Position) +
        first.Weapon.Position.DistanceTo(second.Weapon.Position) +
        first.Mantle.Position.DistanceTo(second.Mantle.Position);

    private static float SilhouetteMagnitude(PlayerPose pose) =>
        Mathf.Abs(pose.Body.RotationRadians) + Mathf.Abs(pose.NearArm.RotationRadians) +
        Mathf.Abs(pose.Weapon.RotationRadians) + pose.Body.Position.Length() +
        pose.NearArm.Position.Length() + pose.Weapon.Position.Length();

    private static float RotationDistance(PoseTransform2D first, PoseTransform2D second) =>
        Mathf.Abs(Mathf.AngleDifference(first.RotationRadians, second.RotationRadians));

    private static void AssertLocalRotationEqual(PoseTransform2D right,
        PoseTransform2D left, string label) => TestAssert.Near(right.RotationRadians,
        left.RotationRadians, .0001f, $"mirrored world aims keep an equivalent facing-local {label} pose");
}
