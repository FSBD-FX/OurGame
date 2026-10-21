using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static partial class PlayerFormalVisualTests
{
    private static readonly string[] Layers =
    [
        "Body", "NearArm", "FarArm", "NearLeg", "FarLeg", "Weapon", "Mantle",
        "ShoulderCore", "Lantern", "Tether"
    ];

    public static async Task Run(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
    }

    internal static IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Case(nameof(SetupAndBasicContract), () => SetupAndBasicContract(tree)),
        FormalCase(tree, nameof(TypedActionPhaseBoundaryBlendsWithoutHardCut),
            TypedActionPhaseBoundaryBlendsWithoutHardCut),
        FormalCase(tree, nameof(AirAndDownAttacksExposeFourDistinctRigStages),
            AirAndDownAttacksExposeFourDistinctRigStages),
        FormalCase(tree, nameof(ThreadTerminalPoseReturnsContinuouslyToIdle),
            ThreadTerminalPoseReturnsContinuouslyToIdle),
        FormalCase(tree, nameof(ThreadAimUsesFacingLocalMirroredRig),
            ThreadAimUsesFacingLocalMirroredRig),
        FormalCase(tree, nameof(ActionOverRunKeepsLegsAndSecondarySlotsContinuous),
            ActionOverRunKeepsLegsAndSecondarySlotsContinuous),
        FormalCase(tree, nameof(LoopWrapKeepsEveryLocomotionSlotContinuous),
            LoopWrapKeepsEveryLocomotionSlotContinuous),
        FormalCase(tree, nameof(TaskOneSecondaryOutputsReachTheirOwnedFormalSlots),
            TaskOneSecondaryOutputsReachTheirOwnedFormalSlots),
        FormalCase(tree, nameof(CompatibilityAdaptersRemainVisible),
            CompatibilityAdaptersRemainVisible),
        FormalCase(tree, nameof(SixtyFpsFramesKeepEverySlotBounded),
            SixtyFpsFramesKeepEverySlotBounded),
        Case(nameof(FacingLocalSecondaryMotionMirrorsWorldExactly),
            () => FacingLocalSecondaryMotionMirrorsWorldExactly(tree)),
        Case(nameof(RealAttackSignalsKeepTypedAnticipationAuthoritative),
            () => RealAttackSignalsKeepTypedAnticipationAuthoritative(tree)),
        Case(nameof(RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder),
            () => RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder(tree)),
        Case(nameof(ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers),
            () => ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers(tree)),
        Case(nameof(RealLethalDamageClearsAttackAcrossRecoveryLifecycle),
            () => RealLethalDamageClearsAttackAcrossRecoveryLifecycle(tree)),
        Case(nameof(RealCheckpointRecoveryClearsAttackPresentationLock),
            () => RealCheckpointRecoveryClearsAttackPresentationLock(tree)),
        Case(nameof(RealInputAttackUpdatesFormalFacingToMatchHitbox),
            () => RealInputAttackUpdatesFormalFacingToMatchHitbox(tree)),
        Case(nameof(RealMechanicalClawAimUpdatesFormalFacingToMatchCast),
            () => RealMechanicalClawAimUpdatesFormalFacingToMatchCast(tree)),
        Case(nameof(RealCompletedAttackIdDoesNotRelatchPresentation),
            () => RealCompletedAttackIdDoesNotRelatchPresentation(tree)),
        Case(nameof(RealCoordinatorKeepsFormalThreadCastThroughEveryPhase),
            () => RealCoordinatorKeepsFormalThreadCastThroughEveryPhase(tree)),
        Case(nameof(RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode),
            () => RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode(tree)),
        Case(nameof(RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase),
            () => RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase(tree)),
        Case(nameof(RealCoordinatorSustainsFormalAndPlaceholderFailedPoses),
            () => RealCoordinatorSustainsFormalAndPlaceholderFailedPoses(tree))
    ];

    private static Task7Case Case(string name, Func<Task> execute) =>
        Task7CaseRegistry.Case(nameof(PlayerFormalVisualTests), name, execute);

    private static Task7Case FormalCase(SceneTree tree, string name,
        Action<PlayerFormalVisual> execute) => Case(name, () => WithFormalFixture(tree, execute));

    private static async Task SetupAndBasicContract(SceneTree tree)
    {
        PackedScene formalScene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
        TestAssert.True(formalScene != null, "formal player scene loads");
        PlayerFormalVisual formal = formalScene.Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        foreach (string layer in Layers)
            TestAssert.True(formal.GetNodeOrNull<Node2D>(layer) != null, $"formal player has {layer} layer");
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Move, -1, true));
        TestAssert.Equal(PlayerAnimationClip.Move, formal.CurrentClip, "formal player enters move clip");
        TestAssert.Equal(new Vector2(-1, 1), formal.Scale, "facing mirrors uniformly on X only");
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Fall, 1, false));
        TestAssert.Equal(PlayerAnimationClip.Fall, formal.CurrentClip, "formal player has distinct fall pose");
        formal.PlayCue(PresentationCue.Hurt);
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Fall, 1, false));
        TestAssert.Equal(PlayerAnimationClip.Hurt, formal.CurrentClip,
            "ordinary locomotion frames do not clear unrelated Hurt cue overrides");
        formal.ClearCueOverride();
        TestAssert.Equal(PlayerAnimationClip.Fall, formal.CurrentClip,
            "explicit cue clear restores the stored locomotion base clip");
        formal.PlayCue(PresentationCue.ThreadFired);
        TestAssert.Equal(PlayerAnimationClip.ThreadCast, formal.CurrentClip, "thread cue enters cast clip");

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        Node player = playerScene.Instantiate();
        TestAssert.True(player.GetNodeOrNull<PlayerFormalVisual>("VisualRoot/Formal") != null,
            "generated Player mounts the formal visual");
        TestAssert.True(player.FindChildren("*", "PlayerPlaceholderVisual", true, false).Count == 0,
            "generated Player contains no placeholder visual");
        player.Free();
        formal.GetParent().RemoveChild(formal);
        formal.Free();
    }

    private static async Task WithFormalFixture(SceneTree tree, Action<PlayerFormalVisual> execute)
    {
        PlayerFormalVisual formal = GD.Load<PackedScene>(
            "res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        try { execute(formal); }
        finally
        {
            formal.GetParent()?.RemoveChild(formal);
            formal.Free();
        }
    }

    public static async Task RunTask7RecoveryContracts(SceneTree tree)
    {
        await RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder(tree);
        await ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers(tree);
        await RealLethalDamageClearsAttackAcrossRecoveryLifecycle(tree);
        await RealCheckpointRecoveryClearsAttackPresentationLock(tree);
        GD.Print("TASK7_LEGACY_SUITE_CASES_EXECUTED " +
                 "suite=PlayerFormalVisualTests count=4");
    }

    private static void TypedActionPhaseBoundaryBlendsWithoutHardCut(PlayerFormalVisual formal)
    {
        PlayerPresentationFrame anticipation = Frame(
            PlayerLocomotionState.Run,
            .42f,
            new PlayerActionFrame(PlayerActionState.GroundAttack,
                PlayerActionPhase.Anticipation, .98f, false));
        formal.ApplyPlayerFrame(anticipation);
        PoseTransform2D before = formal.CurrentPose.NearArm;

        PlayerPresentationFrame burst = Frame(
            PlayerLocomotionState.Run,
            .46f,
            new PlayerActionFrame(PlayerActionState.GroundAttack,
                PlayerActionPhase.Burst, .04f, false));
        formal.ApplyPlayerFrame(burst);
        PoseTransform2D after = formal.CurrentPose.NearArm;
        float positionDelta = before.Position.DistanceTo(after.Position);
        float rotationDelta = Mathf.Abs(Mathf.AngleDifference(
            before.RotationRadians, after.RotationRadians));

        TestAssert.True(positionDelta > .0001f && positionDelta < 2.5f,
            "named per-slot phase-boundary midpoint advances without a destination-position hard cut");
        TestAssert.True(rotationDelta > .0001f && rotationDelta < .10f,
            "named per-slot phase-boundary delta advances without a destination-rotation hard cut");
        TestAssert.Equal(PlayerActionPhase.Burst, formal.CurrentPlayerFrame.Action.Phase,
            "formal visual retains the authoritative typed Burst phase");
    }

    private static void AirAndDownAttacksExposeFourDistinctRigStages(PlayerFormalVisual formal)
    {
        foreach (PlayerActionState state in new[]
                 {
                     PlayerActionState.AirAttack,
                     PlayerActionState.DownAttack
                 })
        {
            var stages = new List<PlayerPose>();
            foreach (PlayerActionPhase phase in new[]
                     {
                         PlayerActionPhase.Anticipation,
                         PlayerActionPhase.Burst,
                         PlayerActionPhase.Contact,
                         PlayerActionPhase.Recovery
                     })
            {
                formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
                    new PlayerActionFrame(state, phase, 1f,
                        phase == PlayerActionPhase.Recovery)));
                stages.Add(formal.CurrentActionPose);
            }

            for (int first = 0; first < stages.Count; first++)
            for (int second = first + 1; second < stages.Count; second++)
            {
                float silhouetteDelta =
                    Mathf.Abs(Mathf.AngleDifference(stages[first].Body.RotationRadians,
                        stages[second].Body.RotationRadians)) +
                    Mathf.Abs(Mathf.AngleDifference(stages[first].NearArm.RotationRadians,
                        stages[second].NearArm.RotationRadians)) +
                    Mathf.Abs(Mathf.AngleDifference(stages[first].Weapon.RotationRadians,
                        stages[second].Weapon.RotationRadians));
                TestAssert.True(silhouetteDelta > .08f,
                    $"{state} stage {first}->{second} has a distinct Body/Arm/Weapon silhouette");
            }
        }
    }

    private static void ThreadTerminalPoseReturnsContinuouslyToIdle(PlayerFormalVisual formal)
    {
        foreach (PlayerActionState state in new[]
                 {
                     PlayerActionState.ThreadApply,
                     PlayerActionState.ThreadFailed
                 })
        {
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                PlayerActionFrame.None));
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                new PlayerActionFrame(state, PlayerActionPhase.Recovery, .5f, false,
                    new Vector2(1f, -.25f))));
            TestAssert.True(Mathf.Abs(formal.CurrentActionPose.NearArm.RotationRadians) > .04f,
                $"{state} retains a visible middle Recovery window");

            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                new PlayerActionFrame(state, PlayerActionPhase.Recovery, .98f, false,
                    new Vector2(1f, -.25f))));
            PoseTransform2D terminalArm = formal.CurrentPose.NearArm;
            PoseTransform2D terminalWeapon = formal.CurrentPose.Weapon;
            formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Idle, .4f,
                PlayerActionFrame.None));

            float armDelta = Mathf.Abs(Mathf.AngleDifference(
                terminalArm.RotationRadians, formal.CurrentPose.NearArm.RotationRadians));
            float weaponDelta = Mathf.Abs(Mathf.AngleDifference(
                terminalWeapon.RotationRadians, formal.CurrentPose.Weapon.RotationRadians));
            TestAssert.True(armDelta < .04f,
                $"{state} .98 Recovery to Idle keeps actual NearArm slot continuous (delta={armDelta})");
            TestAssert.True(weaponDelta < .04f,
                $"{state} .98 Recovery to Idle keeps actual Weapon slot continuous (delta={weaponDelta})");
        }
    }

    private static void ThreadAimUsesFacingLocalMirroredRig(PlayerFormalVisual formal)
    {
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Right), facing: 1));
        PlayerPose right = formal.CurrentActionPose;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Left), facing: -1));
        PlayerPose left = formal.CurrentActionPose;
        AssertMirroredAimUsesEquivalentLocalRig(right, left, "horizontal Right/Left");
        TestAssert.Near(-1f, formal.Scale.X, .0001f,
            "left world aim mirrors the formal root exactly once");

        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Up)));
        PlayerPose up = formal.CurrentActionPose;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: Vector2.Down)));
        PlayerPose down = formal.CurrentActionPose;

        TestAssert.True(Mathf.Abs(Mathf.AngleDifference(up.Body.RotationRadians,
                down.Body.RotationRadians)) > .10f,
            "typed ThreadCast Aim Up/Down produces distinct actual Body rotation");
        TestAssert.True(Mathf.Abs(Mathf.AngleDifference(up.NearArm.RotationRadians,
                down.NearArm.RotationRadians)) > .40f,
            "typed ThreadCast Aim Up/Down produces distinct actual Arm rotation");
        TestAssert.True(Mathf.Abs(Mathf.AngleDifference(up.Weapon.RotationRadians,
                down.Weapon.RotationRadians)) > .40f,
            "typed ThreadCast Aim Up/Down produces distinct actual Weapon rotation");

        Vector2 upRight = new Vector2(1f, -1f).Normalized();
        Vector2 upLeft = new Vector2(-1f, -1f).Normalized();
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: upRight), facing: 1));
        PlayerPose diagonalRight = formal.CurrentActionPose;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Apex, .5f,
            new PlayerActionFrame(PlayerActionState.ThreadThrow,
                PlayerActionPhase.Burst, 1f, false, Aim: upLeft), facing: -1));
        PlayerPose diagonalLeft = formal.CurrentActionPose;
        AssertMirroredAimUsesEquivalentLocalRig(diagonalRight, diagonalLeft,
            "diagonal UpRight/UpLeft");
    }

    private static void AssertMirroredAimUsesEquivalentLocalRig(
        PlayerPose right,
        PlayerPose left,
        string direction)
    {
        TestAssert.Near(right.Body.RotationRadians, left.Body.RotationRadians, .0001f,
            $"{direction} world aims produce equivalent facing-local Body rotation");
        TestAssert.Near(right.NearArm.RotationRadians, left.NearArm.RotationRadians, .0001f,
            $"{direction} world aims produce equivalent facing-local Arm rotation");
        TestAssert.Near(right.Weapon.RotationRadians, left.Weapon.RotationRadians, .0001f,
            $"{direction} world aims produce equivalent facing-local Weapon rotation");
    }

    private static void ActionOverRunKeepsLegsAndSecondarySlotsContinuous(PlayerFormalVisual formal)
    {
        var action = new PlayerActionFrame(PlayerActionState.GroundAttack,
            PlayerActionPhase.Contact, .55f, false);
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run, .20f, action));
        PoseTransform2D firstNearLeg = formal.CurrentPose.NearLeg;
        PoseTransform2D firstFarLeg = formal.CurrentPose.FarLeg;
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Run, .32f, action));
        PoseTransform2D secondNearLeg = formal.CurrentPose.NearLeg;
        PoseTransform2D secondFarLeg = formal.CurrentPose.FarLeg;

        TestAssert.True(Mathf.Abs(firstNearLeg.RotationRadians - secondNearLeg.RotationRadians) > .001f &&
            Mathf.Abs(firstFarLeg.RotationRadians - secondFarLeg.RotationRadians) > .001f,
            "named locomotion-under-action assertion keeps both run legs advancing under an upper-body attack");
        TestAssert.True(!formal.CurrentActionPose.Owns(PlayerPoseSlot.NearLeg) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.FarLeg),
            "upper-body action ownership excludes both locomotion legs");
        TestAssert.True(!formal.CurrentActionPose.Owns(PlayerPoseSlot.Mantle) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.Lantern) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.ShoulderCore) &&
            !formal.CurrentActionPose.Owns(PlayerPoseSlot.Tether),
            "upper-body action ownership excludes all Task 1 secondary slots");
    }

    private static void LoopWrapKeepsEveryLocomotionSlotContinuous(PlayerFormalVisual formal)
    {
        AssertLoopWrapKeepsEverySlotContinuous(formal, PlayerLocomotionState.Run, "Run");
        AssertLoopWrapKeepsEverySlotContinuous(formal, PlayerLocomotionState.Idle, "Idle");
    }

    private static void AssertLoopWrapKeepsEverySlotContinuous(
        PlayerFormalVisual formal,
        PlayerLocomotionState loop,
        string label)
    {
        formal.ApplyPlayerFrame(Frame(PlayerLocomotionState.Dead, .72f,
            PlayerActionFrame.None));
        formal.ApplyPlayerFrame(Frame(loop, .20f,
            PlayerActionFrame.None));
        formal.ApplyPlayerFrame(Frame(loop, .98f,
            PlayerActionFrame.None));
        PoseTransform2D[] beforeWrap = SlotTransforms(formal.CurrentPose);

        formal.ApplyPlayerFrame(Frame(loop, .02f,
            PlayerActionFrame.None));
        PoseTransform2D[] afterWrap = SlotTransforms(formal.CurrentPose);

        for (int slot = 0; slot < Layers.Length; slot++)
        {
            TestAssert.True(beforeWrap[slot].Position.DistanceTo(afterWrap[slot].Position) < 2f,
                $"{label} loop 0.98->0.02 keeps slot {Layers[slot]} position continuous instead of returning to entry pose");
            TestAssert.True(Mathf.Abs(Mathf.AngleDifference(beforeWrap[slot].RotationRadians,
                    afterWrap[slot].RotationRadians)) < .15f,
                $"{label} loop 0.98->0.02 keeps slot {Layers[slot]} rotation continuous instead of returning to entry pose");
        }
    }

    private static void TaskOneSecondaryOutputsReachTheirOwnedFormalSlots(PlayerFormalVisual formal)
    {
        var secondary = new SecondaryMotionFrame(
            Pose(3f, -2f, .11f),
            Pose(-2f, 4f, -.09f),
            Pose(1f, -1f, .17f),
            Pose(-3f, 2f, -.13f));
        PlayerPresentationFrame frame = Frame(PlayerLocomotionState.Idle, .25f,
            PlayerActionFrame.None, secondary);
        formal.ApplyPlayerFrame(frame);

        TestAssert.Near(3f, formal.CurrentPose.Mantle.Position.X, .0001f,
            "mantle continuously consumes the Task 1 mantle output");
        TestAssert.Near(4f, formal.CurrentPose.Lantern.Position.Y, .0001f,
            "lantern continuously consumes the Task 1 lantern output");
        TestAssert.Near(.17f, formal.CurrentPose.ShoulderCore.RotationRadians, .0001f,
            "shoulder spool continuously consumes the Task 1 spool output");
        TestAssert.Near(-.13f, formal.CurrentPose.Tether.RotationRadians, .0001f,
            "tether continuously consumes the Task 1 tether output");
    }

    private static void CompatibilityAdaptersRemainVisible(PlayerFormalVisual formal)
    {
        formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Rise, -1, false));
        TestAssert.Equal(PlayerAnimationClip.Rise, formal.CurrentClip,
            "legacy ApplyFrame remains a valid visible compatibility adapter");
        formal.PlayCue(PresentationCue.Hurt);
        TestAssert.Equal(PlayerAnimationClip.Hurt, formal.CurrentClip,
            "legacy PlayCue remains a valid visible compatibility adapter");
        formal.ClearCueOverride();
        TestAssert.Equal(PlayerAnimationClip.Rise, formal.CurrentClip,
            "legacy cue clear restores the compatibility locomotion base");
    }

    private static void SixtyFpsFramesKeepEverySlotBounded(PlayerFormalVisual formal)
    {
        var timeline = new PlayerPresentationTimeline(new PlayerTuning());
        var input = new PlayerPresentationInput(
            new Vector2(210f, 0f),
            Grounded: true,
            Dashing: false,
            Dead: false,
            CheckpointRecovering: false,
            Facing: 1,
            AttackId: 611,
            IsAttacking: true,
            AttackDirection: AttackDirection.Horizontal);
        formal.ApplyPlayerFrame(timeline.Step(input, 1f / 60f));
        PoseTransform2D[] previous = SlotTransforms(formal.CurrentPose);
        for (int sample = 1; sample <= 30; sample++)
        {
            formal.ApplyPlayerFrame(timeline.Step(input, 1f / 60f));
            PoseTransform2D[] current = SlotTransforms(formal.CurrentPose);
            for (int slot = 0; slot < current.Length; slot++)
            {
                TestAssert.True(previous[slot].Position.DistanceTo(current[slot].Position) < 6f,
                    $"60 FPS sample {sample} keeps slot {Layers[slot]} displacement bounded");
                TestAssert.True(Mathf.Abs(Mathf.AngleDifference(previous[slot].RotationRadians,
                        current[slot].RotationRadians)) < .5f,
                    $"60 FPS sample {sample} keeps slot {Layers[slot]} rotation bounded");
            }
            previous = current;
        }
    }

    private static PoseTransform2D[] SlotTransforms(PlayerPose pose) =>
    [
        pose.Body, pose.NearArm, pose.FarArm, pose.NearLeg, pose.FarLeg,
        pose.Weapon, pose.Mantle, pose.ShoulderCore, pose.Lantern, pose.Tether
    ];

    private static async Task FacingLocalSecondaryMotionMirrorsWorldExactly(SceneTree tree)
    {
        var rightTimeline = new PlayerPresentationTimeline(new PlayerTuning());
        var leftTimeline = new PlayerPresentationTimeline(new PlayerTuning());
        rightTimeline.Step(new PlayerPresentationInput(
            Vector2.Zero, true, false, false, false, -1), 1f / 60f);
        leftTimeline.Step(new PlayerPresentationInput(
            Vector2.Zero, true, false, false, false, 1), 1f / 60f);

        PlayerPresentationFrame rightFrame = rightTimeline.Step(new PlayerPresentationInput(
            new Vector2(240f, -20f), true, false, false, false, 1,
            HurtTriggered: true, HurtImpulse: new Vector2(100f, 20f)), 1f / 60f);
        PlayerPresentationFrame leftFrame = leftTimeline.Step(new PlayerPresentationInput(
            new Vector2(-240f, -20f), true, false, false, false, -1,
            HurtTriggered: true, HurtImpulse: new Vector2(-100f, 20f)), 1f / 60f);

        AssertTransformNear(rightFrame.Secondary.Mantle, leftFrame.Secondary.Mantle,
            "mirrored velocity/turn/impact keeps Mantle facing-local");
        AssertTransformNear(rightFrame.Secondary.Lantern, leftFrame.Secondary.Lantern,
            "mirrored velocity/turn/impact keeps Lantern facing-local");
        AssertTransformNear(rightFrame.Secondary.ShoulderSpool, leftFrame.Secondary.ShoulderSpool,
            "mirrored velocity/turn/impact keeps shoulder spool facing-local");
        AssertTransformNear(rightFrame.Secondary.Tether, leftFrame.Secondary.Tether,
            "mirrored velocity/turn/impact keeps tether/charm facing-local");

        PackedScene scene = GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn");
        PlayerFormalVisual right = scene.Instantiate<PlayerFormalVisual>();
        PlayerFormalVisual left = scene.Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(right);
        tree.Root.AddChild(left);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        right.ApplyPlayerFrame(rightFrame);
        left.ApplyPlayerFrame(leftFrame);

        AssertTransformNear(right.CurrentPose.Body, left.CurrentPose.Body,
            "mirrored world motion keeps Body local pose equivalent");
        AssertTransformNear(right.CurrentPose.Mantle, left.CurrentPose.Mantle,
            "mirrored world motion keeps Mantle local pose equivalent");
        AssertTransformNear(right.CurrentPose.Lantern, left.CurrentPose.Lantern,
            "mirrored world motion keeps Lantern local pose equivalent");
        AssertTransformNear(right.CurrentPose.ShoulderCore, left.CurrentPose.ShoulderCore,
            "mirrored world motion keeps spool local pose equivalent");
        AssertTransformNear(right.CurrentPose.Tether, left.CurrentPose.Tether,
            "mirrored world motion keeps charm/tether local pose equivalent");
        TestAssert.Equal(new Vector2(1f, 1f), right.Scale,
            "right-facing formal root keeps positive world X scale");
        TestAssert.Equal(new Vector2(-1f, 1f), left.Scale,
            "left-facing formal root provides the sole world X mirror");

        right.QueueFree();
        left.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static void AssertTransformNear(PoseTransform2D expected, PoseTransform2D actual,
        string message)
    {
        TestAssert.Near(expected.Position.X, actual.Position.X, .0001f, $"{message} position X");
        TestAssert.Near(expected.Position.Y, actual.Position.Y, .0001f, $"{message} position Y");
        TestAssert.Near(expected.RotationRadians, actual.RotationRadians, .0001f,
            $"{message} rotation");
    }

    private static async Task RealAttackSignalsKeepTypedAnticipationAuthoritative(SceneTree tree)
    {
        await AssertRealAttackSignalKeepsTypedAnticipationAuthoritative(
            tree, AttackDirection.Horizontal, "AttackStarted");
        await AssertRealAttackSignalKeepsTypedAnticipationAuthoritative(
            tree, AttackDirection.Down, "Downstrike");
    }

    private static async Task RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealCheckpointDeathPublishesDeadRecoveryLocomotionInOrder));
        level.Name = "PlayerRecoveryPresentationFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        bool recoveryRequested = false;
        bool recoveryCompleted = false;
        var publishedFrames = new List<PlayerPresentationFrame>();
        level.RecoveryRequested += _ => recoveryRequested = true;
        level.RecoveryCompleted += _ => recoveryCompleted = true;
        coordinator.FramePublished += publishedFrames.Add;

        health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0f, 99101UL));

        TestAssert.True(recoveryRequested,
            "real lethal damage enters the Prologue recovery lifecycle synchronously");
        await WaitForPublishedRecoverySequence(tree, publishedFrames);

        TestAssert.True(recoveryCompleted,
            "real generated Player death completes the Prologue checkpoint lifecycle");
        List<PlayerLocomotionState> sequence = DistinctLocomotionStates(publishedFrames);
        TestAssert.True(sequence.Count >= 3,
            "real typed-frame event publishes at least Dead, Recovery, and ordinary states");
        TestAssert.Equal(PlayerLocomotionState.Dead, sequence[0],
            "real typed-frame event publishes Dead first");
        TestAssert.Equal(PlayerLocomotionState.Recovery, sequence[1],
            "real typed-frame event publishes Recovery immediately after Dead");
        TestAssert.True(sequence[2] is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "real typed-frame event publishes ordinary locomotion immediately after Recovery");
        TestAssert.True(formal.CurrentPlayerFrame.Locomotion.State is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "real formal rig preserves Dead -> Recovery -> ordinary locomotion order");

        coordinator.FramePublished -= publishedFrames.Add;
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task WaitForPublishedRecoverySequence(
        SceneTree tree,
        List<PlayerPresentationFrame> publishedFrames)
    {
        // The frame budget is fail-closed timeout only. Ordering observations come
        // exclusively from the coordinator's natural typed-frame publication event.
        for (int timeoutFrame = 0; timeoutFrame < 120; timeoutFrame++)
        {
            if (DistinctLocomotionStates(publishedFrames).Count >= 3) return;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        throw new InvalidOperationException(
            "Assertion failed: timed out waiting for natural Dead -> Recovery -> ordinary typed-frame publications");
    }

    private static async Task ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers(SceneTree tree)
    {
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(ThrowingFrameObserverCannotFreezeRealRecoveryOrLaterObservers));
        level.Name = "ThrowingPlayerFrameObserverFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var downstreamFrames = new List<PlayerPresentationFrame>();
        void ThrowingObserver(PlayerPresentationFrame _) =>
            throw new InvalidOperationException("deliberate diagnostic observer failure");
        coordinator.FramePublished += ThrowingObserver;
        coordinator.FramePublished += downstreamFrames.Add;

        Exception leaked = null;
        try
        {
            health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Right, 0f, 99103UL));
        }
        catch (Exception exception)
        {
            leaked = exception;
        }
        TestAssert.True(leaked == null,
            "throwing frame observer is isolated from real lethal recovery dispatch");
        await WaitForPublishedRecoverySequence(tree, downstreamFrames);
        List<PlayerLocomotionState> sequence = DistinctLocomotionStates(downstreamFrames);
        TestAssert.Equal(PlayerLocomotionState.Dead, sequence[0],
            "later observer still receives Dead after an earlier observer throws");
        TestAssert.Equal(PlayerLocomotionState.Recovery, sequence[1],
            "throwing observer cannot freeze AdvanceRecoveryPresentation");
        TestAssert.True(sequence[2] is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "later observer receives ordinary locomotion after isolated observer failures");
        TestAssert.True(coordinator.FrameObserverFailureCount > 0,
            "coordinator exposes a read-only diagnostic observer failure summary");
        coordinator.FramePublished -= ThrowingObserver;
        coordinator.FramePublished -= downstreamFrames.Add;
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static List<PlayerLocomotionState> DistinctLocomotionStates(
        List<PlayerPresentationFrame> publishedFrames)
    {
        var result = new List<PlayerLocomotionState>();
        foreach (PlayerPresentationFrame frame in publishedFrames)
        {
            PlayerLocomotionState state = frame.Locomotion.State;
            if (result.Count == 0 || result[^1] != state) result.Add(state);
        }
        return result;
    }

    private static async Task RealLethalDamageClearsAttackAcrossRecoveryLifecycle(SceneTree tree)
    {
        ReleaseInput();
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealLethalDamageClearsAttackAcrossRecoveryLifecycle));
        level.Name = "PlayerAttackDeathPresentationFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");

        var attackStarted = tree.ToSignal(combat, PlayerCombat.SignalName.AttackStarted);
        await PressAttackPrimary(tree);
        await attackStarted;
        Input.ActionRelease(InputActions.PrimaryAction);
        TestAssert.True(combat.IsAttacking,
            "real Input.ActionPress starts PlayerCombat before lethal damage");

        var publishedFrames = new List<PlayerPresentationFrame>();
        coordinator.FramePublished += publishedFrames.Add;
        health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Left, 0f, 99102UL));
        await WaitForPublishedRecoverySequence(tree, publishedFrames);

        List<PlayerLocomotionState> sequence = DistinctLocomotionStates(publishedFrames);
        TestAssert.Equal(PlayerLocomotionState.Dead, sequence[0],
            "attack death lifecycle publishes Dead first");
        TestAssert.Equal(PlayerLocomotionState.Recovery, sequence[1],
            "attack death lifecycle publishes Recovery second");
        TestAssert.True(sequence[2] is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery,
            "attack death lifecycle publishes ordinary locomotion third");

        int ordinaryIndex = publishedFrames.FindIndex(frame =>
            frame.Locomotion.State is not PlayerLocomotionState.Dead and
                not PlayerLocomotionState.Recovery);
        TestAssert.True(ordinaryIndex >= 0,
            "attack death lifecycle publishes an ordinary frame after recovery");
        for (int index = 0; index <= ordinaryIndex; index++)
        {
            PlayerPresentationFrame frame = publishedFrames[index];
            TestAssert.Equal(PlayerActionState.None, frame.Action.State,
                $"natural {frame.Locomotion.State} publication clears the pre-death attack action");
        }

        coordinator.FramePublished -= publishedFrames.Add;
        ReleaseInput();
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealCheckpointRecoveryClearsAttackPresentationLock(SceneTree tree)
    {
        ReleaseInput();
        PrologueLevelController level = GD.Load<PackedScene>(
                "res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealCheckpointRecoveryClearsAttackPresentationLock));
        level.Name = "PlayerAttackCheckpointRecoveryFixture";
        level.GetNode<Camera2D>("PlayerRoot/Player/Camera2D").Enabled = false;
        tree.Root.AddChild(level);
        await Frames(tree, 3);

        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");

        var attackStarted = tree.ToSignal(combat, PlayerCombat.SignalName.AttackStarted);
        await PressAttackPrimary(tree);
        await attackStarted;
        Input.ActionRelease(InputActions.PrimaryAction);

        var publishedFrames = new List<PlayerPresentationFrame>();
        coordinator.FramePublished += publishedFrames.Add;
        level.RequestRespawn("presentation-checkpoint-recovery");
        await WaitForCondition(tree, () =>
        {
            bool sawRecovery = false;
            bool sawOrdinaryAfterRecovery = false;
            foreach (PlayerPresentationFrame frame in publishedFrames)
            {
                if (frame.Locomotion.State == PlayerLocomotionState.Recovery) sawRecovery = true;
                else if (sawRecovery) sawOrdinaryAfterRecovery = true;
            }
            return sawOrdinaryAfterRecovery;
        }, "non-death checkpoint Recovery followed by ordinary typed publication");

        int recoveryIndex = publishedFrames.FindIndex(frame =>
            frame.Locomotion.State == PlayerLocomotionState.Recovery);
        int ordinaryIndex = publishedFrames.FindIndex(recoveryIndex + 1, frame =>
            frame.Locomotion.State != PlayerLocomotionState.Recovery);
        TestAssert.True(recoveryIndex >= 0 && ordinaryIndex > recoveryIndex,
            "real checkpoint recovery publishes Recovery before ordinary locomotion");
        for (int index = recoveryIndex; index <= ordinaryIndex; index++)
            TestAssert.Equal(PlayerActionState.None, publishedFrames[index].Action.State,
                "non-death checkpoint recovery clears the presentation-only attack lock");

        coordinator.FramePublished -= publishedFrames.Add;
        ReleaseInput();
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealInputAttackUpdatesFormalFacingToMatchHitbox(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "RealInputAttackFacingFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        Hitbox hitbox = combat.GetNode<Hitbox>("AttackHitbox");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");

        Input.ActionPress(InputActions.MoveLeft);
        await PressAttackPrimary(tree);
        await Frames(tree, 1);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        TestAssert.True(combat.IsAttacking,
            "real Input.ActionPress reaches PlayerInputReader and starts PlayerCombat");
        TestAssert.True(hitbox.Position.X < 0f,
            "real PlayerCombat horizontal hitbox follows the backward left aim");
        TestAssert.Equal(-1, coordinator.LastPlayerFrame.Base.Facing,
            "typed presentation root follows the same left aim as the real hitbox while stationary");
        TestAssert.Equal(Vector2.Left, coordinator.LastPlayerFrame.Action.Aim,
            "typed horizontal attack aim matches the real left-facing hitbox");
        TestAssert.Near(-1f, formal.Scale.X, .0001f,
            "formal root mirrors left for the real backward attack");

        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealMechanicalClawAimUpdatesFormalFacingToMatchCast(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "MechanicalClawFacingFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        GrappleSurface2D surface = GrappleSurface("FacingSurface", new Vector2(-180f, -120f));
        root.AddChild(surface);
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;
        ClawHarness claw = SuspendMechanicalClaw(player);
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));

        Vector2 castAim = claw.Controller.CurrentFrame.CasterWorldPosition.DirectionTo(
            claw.Controller.CurrentFrame.AnchorPoint);
        TestAssert.True(castAim.X < 0f && castAim.Y < 0f,
            "real mechanical-claw target produces an authoritative up-left aim");
        TestAssert.Equal(-1, coordinator.LastPlayerFrame.Base.Facing,
            "typed presentation root follows the real mechanical-claw horizontal intent");
        TestAssert.Near(castAim.X,
            coordinator.LastPlayerFrame.Action.Aim.X, .0001f,
            "typed claw action consumes the normalized real cast Aim.X");
        TestAssert.Near(castAim.Y,
            coordinator.LastPlayerFrame.Action.Aim.Y, .0001f,
            "typed claw action consumes the normalized real cast Aim.Y");
        TestAssert.Near(-1f, formal.Scale.X, .0001f,
            "formal root mirrors left for the real diagonal claw cast");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealCompletedAttackIdDoesNotRelatchPresentation(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "CompletedAttackPresentationFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var afterNaturalCompletion = new List<PlayerPresentationFrame>();
        bool observedAttackPose = false;
        bool observedFirstNone = false;
        void OnFrame(PlayerPresentationFrame frame)
        {
            if (frame.Action.State is PlayerActionState.GroundAttack or PlayerActionState.AirAttack or
                PlayerActionState.DownAttack)
            {
                observedAttackPose = true;
                return;
            }
            if (!observedAttackPose || combat.IsAttacking) return;
            if (!observedFirstNone)
            {
                observedFirstNone = frame.Action.State == PlayerActionState.None;
                return;
            }
            afterNaturalCompletion.Add(frame);
        }
        coordinator.FramePublished += OnFrame;

        var attackStarted = tree.ToSignal(combat, PlayerCombat.SignalName.AttackStarted);
        await PressAttackPrimary(tree);
        await attackStarted;
        Input.ActionRelease(InputActions.PrimaryAction);
        await WaitForCondition(tree, () => observedFirstNone && afterNaturalCompletion.Count >= 5,
            "natural attack completion plus five subsequent typed publications");

        foreach (PlayerPresentationFrame frame in afterNaturalCompletion)
            TestAssert.Equal(PlayerActionState.None, frame.Action.State,
                "completed external attack ID cannot re-latch the presentation timeline");

        coordinator.FramePublished -= OnFrame;
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task WaitForCondition(
        SceneTree tree,
        Func<bool> condition,
        string description)
    {
        for (int timeoutFrame = 0; timeoutFrame < 240; timeoutFrame++)
        {
            if (condition()) return;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        throw new InvalidOperationException($"Assertion failed: timed out waiting for {description}");
    }

    private static async Task AssertRealAttackSignalKeepsTypedAnticipationAuthoritative(
        SceneTree tree,
        AttackDirection direction,
        string cueName)
    {
        var root = new Node2D { Name = $"{cueName}TypedAuthorityFixture" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        root.AddChild(player);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        PlayerPresentationCoordinator coordinator =
            player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        PoseTransform2D before = formal.CurrentActionPose.NearArm;

        combat.EmitSignal(PlayerCombat.SignalName.AttackStarted, 900UL + (ulong)direction,
            (int)direction);
        coordinator._Process(0d);

        TestAssert.Equal(PlayerActionPhase.Anticipation, coordinator.LastPlayerFrame.Action.Phase,
            $"real generated Player {cueName} enters typed Anticipation first");
        TestAssert.Near(before.Position.X, formal.CurrentActionPose.NearArm.Position.X, .0001f,
            $"real generated Player {cueName} legacy cue cannot inject Burst position before typed Anticipation");
        TestAssert.Near(before.Position.Y, formal.CurrentActionPose.NearArm.Position.Y, .0001f,
            $"real generated Player {cueName} legacy cue cannot inject Burst height before typed Anticipation");
        TestAssert.Near(before.RotationRadians,
            formal.CurrentActionPose.NearArm.RotationRadians, .0001f,
            $"real generated Player {cueName} legacy cue cannot inject Burst rotation before typed Anticipation");

        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static PlayerPresentationFrame Frame(
        PlayerLocomotionState locomotion,
        float progress,
        PlayerActionFrame action,
        SecondaryMotionFrame? secondary = null,
        int facing = 1) => new(
        new EntityPresentationFrame(locomotion switch
        {
            PlayerLocomotionState.Dead => EntityVisualState.Dead,
            PlayerLocomotionState.Dash => EntityVisualState.Dash,
            PlayerLocomotionState.Rise or PlayerLocomotionState.JumpStart => EntityVisualState.Rise,
            PlayerLocomotionState.Fall or PlayerLocomotionState.Apex => EntityVisualState.Fall,
            PlayerLocomotionState.Run or PlayerLocomotionState.StartRun or PlayerLocomotionState.Brake =>
                EntityVisualState.Move,
            PlayerLocomotionState.Recovery => EntityVisualState.Hurt,
            _ => EntityVisualState.Idle
        }, facing, locomotion is not PlayerLocomotionState.Rise and not PlayerLocomotionState.JumpStart and
            not PlayerLocomotionState.Fall and not PlayerLocomotionState.Apex),
        new PlayerLocomotionFrame(locomotion, progress),
        action,
        secondary ?? SecondaryMotionFrame.Identity);

    private static PoseTransform2D Pose(float x, float y, float rotation) => new(
        new Vector2(x, y), rotation, Vector2.One, Colors.White);

    private static async Task RealCoordinatorKeepsFormalThreadCastThroughEveryPhase(SceneTree tree)
    {
        ReleaseInput();
        var root = new Node2D { Name = "MechanicalClawPresentationCoordinatorFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        CharacterBody2D player = playerScene.Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        GrappleSurface2D surface = GrappleSurface("PresentationSurface", new Vector2(180f, 0f));
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(surface);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        PlayerFormalVisual liveFormal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        PlayerAnimationClip locomotionClipBeforeCast = liveFormal.CurrentClip;
        var observedStates = new HashSet<MechanicalClawState>();
        claw.Controller.ClawFrameChanged += (frame, target) => observedStates.Add(frame.State);

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(PlayerAnimationClip.ThreadPreview, liveFormal.CurrentClip,
            "real coordinator presents the typed ThreadPreview pose for mechanical-claw Aim");

        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Latched);
        await StepClaw(tree, claw, ClawInput(primaryHeld: true,
            mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(MechanicalClawState.Swing, claw.Controller.CurrentFrame.State,
            "latched mechanical claw advances into a persistent swing state");
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(MechanicalClawState.Released, claw.Controller.CurrentFrame.State,
            "primary release publishes the typed claw release phase");
        TestAssert.Equal(PlayerAnimationClip.ThreadApply, liveFormal.CurrentClip,
            "released claw retains the typed apply/recovery pose while retracting");

        foreach (MechanicalClawState state in new[]
                 {
                     MechanicalClawState.Aim,
                     MechanicalClawState.Windup,
                     MechanicalClawState.Travel,
                     MechanicalClawState.Latched,
                     MechanicalClawState.Swing,
                     MechanicalClawState.Released
                 })
        {
            TestAssert.True(observedStates.Contains(state),
                $"real coordinator observes the authoritative {state} state in order");
        }
        await SettleClawToIdle(tree, claw, surface.WorldAnchor);
        TestAssert.Equal(locomotionClipBeforeCast, liveFormal.CurrentClip,
            "natural retract exit clears the formal claw override on the authoritative Idle edge");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealCoordinatorClearsFormalClawPoseOnReleaseAndAttackMode(SceneTree tree)
    {
        await AssertFormalClawPoseClearsOnDetach(tree, attackMode: false);
        await AssertFormalClawPoseClearsOnDetach(tree, attackMode: true);
    }

    private static async Task AssertFormalClawPoseClearsOnDetach(SceneTree tree, bool attackMode)
    {
        ReleaseInput();
        string source = attackMode ? "Q attack-mode retract" : "primary release";
        var root = new Node2D
        {
            Name = attackMode ? "FormalAttackModeRetractFixture" : "FormalPrimaryReleaseFixture"
        };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        GrappleSurface2D surface = GrappleSurface("DetachSurface", new Vector2(180f, 0f));
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(surface);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        ThreadLineVisual line = player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        ThreadShuttleVisual shuttle = player.GetNode<ThreadShuttleVisual>("VisualRoot/MechanicalClaw");
        PlayerAnimationClip locomotionClipBeforeCast = formal.CurrentClip;

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Latched);
        await StepClaw(tree, claw, ClawInput(primaryHeld: true,
            mouseWorld: surface.WorldAnchor));
        TestAssert.Equal(PlayerAnimationClip.ThreadApply, formal.CurrentClip,
            $"real {source} setup presents the typed ThreadApply swing pose before detach");

        PlayerInputFrame detach = attackMode
            ? ClawInput(attackModePressed: true, primaryHeld: true,
                mouseWorld: surface.WorldAnchor)
            : ClawInput(primaryReleased: true, mouseWorld: surface.WorldAnchor);
        await StepClaw(tree, claw, detach);
        TestAssert.Equal(MechanicalClawState.Released, claw.Controller.CurrentFrame.State,
            $"real {source} publishes the authoritative Released edge");
        await SettleClawToIdle(tree, claw, surface.WorldAnchor);

        TestAssert.Equal(locomotionClipBeforeCast, formal.CurrentClip,
            $"real {source} clears formal claw pose to the stored locomotion base");
        TestAssert.True(line.ActiveStrandCount == 0 && shuttle.IsClawClear,
            $"real {source} clears line and shuttle on the authoritative Idle edge");
        TestAssert.Equal(MechanicalClawState.Idle, claw.Controller.CurrentFrame.State,
            $"real {source} leaves no active claw or gameplay cooldown state");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task RealCoordinatorKeepsPlaceholderThreadPoseThroughEveryPhase(SceneTree tree)
    {
        ReleaseInput();
        bool priorDiagnosticFallbacks = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        try
        {
        var root = new Node2D { Name = "MechanicalClawPlaceholderCoordinatorFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        GrappleSurface2D surface = GrappleSurface("PlaceholderSurface", new Vector2(180f, 0f));
        var placeholderHost = new Node2D { Name = "PlaceholderHost" };
        var placeholder = new PlayerPlaceholderVisual { Name = "Placeholder" };
        placeholderHost.AddChild(placeholder);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(surface);
        root.AddChild(placeholderHost);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        player.GetNode<PlayerPresentationCoordinator>("Presentation").BindVisualRoot(placeholderHost);
        var geometryAtPhase = new Dictionary<MechanicalClawState, PlaceholderGeometry>();
        claw.Controller.ClawFrameChanged += (frame, target) =>
        {
            if (frame.State != MechanicalClawState.Idle)
                geometryAtPhase[frame.State] = ReadPlaceholderGeometry(placeholder);
        };

        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: surface.WorldAnchor));
        AssertThreadPlaceholderGeometry(ReadPlaceholderGeometry(placeholder),
            "real-coordinator mechanical-claw Aim");
        await AdvanceClawTo(tree, claw, surface, MechanicalClawState.Latched);
        await StepClaw(tree, claw, ClawInput(primaryHeld: true,
            mouseWorld: surface.WorldAnchor));
        await StepClaw(tree, claw, ClawInput(primaryReleased: true,
            mouseWorld: surface.WorldAnchor));

        foreach (MechanicalClawState state in new[]
                 {
                     MechanicalClawState.Aim,
                     MechanicalClawState.Windup,
                     MechanicalClawState.Travel,
                     MechanicalClawState.Latched,
                     MechanicalClawState.Swing,
                     MechanicalClawState.Released
                 })
        {
            TestAssert.True(geometryAtPhase.TryGetValue(state, out PlaceholderGeometry geometry),
                $"real placeholder coordinator publishes the {state} phase pose");
            AssertThreadPlaceholderGeometry(geometry, $"real placeholder {state}");
        }

        await SettleClawToIdle(tree, claw, surface.WorldAnchor);
        PlaceholderGeometry afterCast = ReadPlaceholderGeometry(placeholder);
        TestAssert.True(!afterCast.ThreadCastPoseActive,
            "placeholder leaves the ThreadCast pose as soon as the coordinator returns to Idle");
        TestAssert.True(afterCast.BodySize != new Vector2(46f, 48f),
            "placeholder restores its movement-driven body silhouette after casting");
        TestAssert.Equal(new Vector2(46f, 7f), afterCast.WeaponEnd,
            "placeholder restores its default weapon geometry after casting");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
        }
        finally
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = priorDiagnosticFallbacks;
        }
    }

    private static async Task RealCoordinatorSustainsFormalAndPlaceholderFailedPoses(SceneTree tree)
    {
        ReleaseInput();
        bool priorDiagnosticFallbacks = PrologueProductionMode.AllowDiagnosticFallbacks;
        PrologueProductionMode.AllowDiagnosticFallbacks = true;
        try
        {
        var root = new Node2D { Name = "MechanicalClawFailedPresentationFixture" };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 50f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });

        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.GetNode<PlayerMotor>("Motor").Tuning = new PlayerTuning { Gravity = .01f };
        var placeholderHost = new Node2D { Name = "FailedPlaceholderHost" };
        var placeholder = new PlayerPlaceholderVisual { Name = "Placeholder" };
        placeholderHost.AddChild(placeholder);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(placeholderHost);
        tree.Root.AddChild(root);
        await Frames(tree, 2);

        ClawHarness claw = SuspendMechanicalClaw(player);
        PlayerPresentationCoordinator coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
        PlayerFormalVisual formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        PlayerAnimationClip locomotionClipBeforeFailure = formal.CurrentClip;

        await FailClawWithoutTarget(tree, claw);
        TestAssert.Equal(MechanicalClawState.Failed, claw.Controller.CurrentFrame.State,
            "formal failure fixture publishes the typed no-target failure frame");
        TestAssert.Equal(PlayerAnimationClip.ThreadFailed, formal.CurrentClip,
            "formal visual presents the distinct ThreadFailed pose instead of locomotion");
        await StepClaw(tree, claw, ClawInput());
        TestAssert.Equal(MechanicalClawState.Idle, claw.Controller.CurrentFrame.State,
            "no-target failure clears directly without allocating a cooldown cast");
        TestAssert.Equal(locomotionClipBeforeFailure, formal.CurrentClip,
            "natural Failed exit clears the formal failure override on the authoritative Idle edge");

        coordinator.BindVisualRoot(placeholderHost);
        await FailClawWithoutTarget(tree, claw);
        TestAssert.True(placeholder.ThreadFailedPoseActive,
            "placeholder presents a distinct ThreadFailed pose for the typed failure frame");
        TestAssert.True(!placeholder.ThreadCastPoseActive,
            "placeholder failure pose is not false-greened by the normal ThreadCast pose");
        TestAssert.Equal(new Vector2(48f, 44f), placeholder.CurrentBodySize,
            "placeholder failure uses its recoiled procedural body silhouette");
        TestAssert.Equal(new Vector2(9f, -3f), placeholder.CurrentWeaponStart,
            "placeholder failure pulls the procedural needle back toward the spool");
        TestAssert.Equal(new Vector2(43f, 6f), placeholder.CurrentWeaponEnd,
            "placeholder failure keeps a short visibly broken needle silhouette");
        await StepClaw(tree, claw, ClawInput());
        TestAssert.True(!placeholder.ThreadFailedPoseActive && !placeholder.ThreadGlowActive,
            "placeholder clears the Failed pose and glow when the model returns to Idle");

        ResumeMechanicalClaw(claw);
        ReleaseInput();
        root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
        }
        finally
        {
            PrologueProductionMode.AllowDiagnosticFallbacks = priorDiagnosticFallbacks;
        }
    }

    private static PlaceholderGeometry ReadPlaceholderGeometry(PlayerPlaceholderVisual placeholder)
        => new(placeholder.ThreadCastPoseActive, placeholder.CurrentBodySize,
            placeholder.CurrentWeaponStart, placeholder.CurrentWeaponEnd);

    private static void AssertThreadPlaceholderGeometry(PlaceholderGeometry geometry, string phase)
    {
        TestAssert.True(geometry.ThreadCastPoseActive,
            $"{phase} owns a real ThreadCast pose state rather than only a glow timer");
        TestAssert.Equal(new Vector2(46f, 48f), geometry.BodySize,
            $"{phase} uses the compact braced ThreadCast body silhouette");
        TestAssert.Equal(new Vector2(13f, -7f), geometry.WeaponStart,
            $"{phase} raises the procedural thread weapon from the shoulder");
        TestAssert.Equal(new Vector2(72f, -21f), geometry.WeaponEnd,
            $"{phase} extends the procedural needle beyond the default weapon geometry");
    }

    private readonly record struct PlaceholderGeometry(
        bool ThreadCastPoseActive,
        Vector2 BodySize,
        Vector2 WeaponStart,
        Vector2 WeaponEnd);

    private static ClawHarness SuspendMechanicalClaw(CharacterBody2D player)
    {
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController controller =
            player.GetNode<MechanicalClawController>("MechanicalClaw");
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        controller.SetPhysicsProcess(false);
        controller.ResetTransientState();
        return new ClawHarness(input, modes, controller);
    }

    private static void ResumeMechanicalClaw(ClawHarness claw)
    {
        SetInputFrame(claw.Input, ClawInput());
        claw.Controller.ResetTransientState();
        claw.Input.SetPhysicsProcess(true);
        claw.Modes.SetPhysicsProcess(true);
        claw.Controller.SetPhysicsProcess(true);
    }

    private static async Task StepClaw(SceneTree tree, ClawHarness claw, PlayerInputFrame frame)
    {
        SetInputFrame(claw.Input, frame);
        claw.Modes._PhysicsProcess(0d);
        claw.Controller._PhysicsProcess(1d / 60d);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task AdvanceClawTo(SceneTree tree, ClawHarness claw,
        GrappleSurface2D surface, MechanicalClawState expected)
    {
        for (int index = 0; index < 180; index++)
        {
            if (claw.Controller.CurrentFrame.State == expected) return;
            if (claw.Controller.CurrentFrame.State == MechanicalClawState.Failed)
                throw new InvalidOperationException(
                    $"Mechanical claw failed before {expected}: {claw.Controller.CurrentFrame.Failure}");
            await StepClaw(tree, claw, ClawInput(primaryHeld: true,
                mouseWorld: surface.WorldAnchor));
        }
        throw new InvalidOperationException(
            $"Timed out waiting for mechanical-claw state {expected}; " +
            $"current={claw.Controller.CurrentFrame.State}");
    }

    private static async Task SettleClawToIdle(SceneTree tree, ClawHarness claw,
        Vector2 mouseWorld)
    {
        for (int index = 0; index < 180; index++)
        {
            if (claw.Controller.CurrentFrame.State == MechanicalClawState.Idle) return;
            await StepClaw(tree, claw, ClawInput(mouseWorld: mouseWorld));
        }
        throw new InvalidOperationException(
            $"Timed out retracting mechanical claw; current={claw.Controller.CurrentFrame.State}");
    }

    private static async Task FailClawWithoutTarget(SceneTree tree, ClawHarness claw)
    {
        claw.Controller.ResetTransientState();
        await StepClaw(tree, claw, ClawInput(grappleModePressed: true,
            primaryPressed: true, primaryHeld: true, mouseWorld: new Vector2(420f, -420f)));
        await StepClaw(tree, claw, ClawInput(primaryHeld: true,
            mouseWorld: new Vector2(420f, -420f)));
        TestAssert.Equal(MechanicalClawFailure.NoTarget, claw.Controller.CurrentFrame.Failure,
            "mechanical-claw failure fixture reports the typed no-target reason");
    }

    private static PlayerInputFrame ClawInput(bool grappleModePressed = false,
        bool attackModePressed = false, bool primaryPressed = false, bool primaryHeld = false,
        bool primaryReleased = false, Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, false, false, false, false, false,
            MouseWorld: mouseWorld,
            AttackModePressed: attackModePressed,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld,
            PrimaryReleased: primaryReleased);

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame) =>
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);

    private static GrappleSurface2D GrappleSurface(string name, Vector2 position)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CircleShape2D { Radius = 10f }
        });
        surface.AddChild(area);
        return surface;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task PressAttackPrimary(SceneTree tree)
    {
        Input.ActionPress(InputActions.SelectAttackMode);
        await Frames(tree, 1);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionPress(InputActions.PrimaryAction);
    }

    private static void ReleaseInput()
    {
        Input.ActionRelease(InputActions.MoveLeft);
        Input.ActionRelease(InputActions.MoveRight);
        Input.ActionRelease(InputActions.MoveUp);
        Input.ActionRelease(InputActions.MoveDown);
        Input.ActionRelease(InputActions.Jump);
        Input.ActionRelease(InputActions.Dash);
        Input.ActionRelease(InputActions.SelectAttackMode);
        Input.ActionRelease(InputActions.SelectGrappleMode);
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.Shield);
    }

    private sealed record ClawHarness(PlayerInputReader Input,
        PlayerActionModeController Modes, MechanicalClawController Controller);
}
