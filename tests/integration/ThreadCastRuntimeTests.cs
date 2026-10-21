using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;
using Knight.Ui;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static partial class ThreadCastRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await Task7CaseRegistry.Execute(Task7Cases(tree));
    }

    internal static IReadOnlyList<Task7Case> Task7Cases(SceneTree tree) =>
    [
        Case(nameof(PreviewTravelLatchApplyAndRecoveryUseRealPhysicsFrames),
            () => PreviewTravelLatchApplyAndRecoveryUseRealPhysicsFrames(tree)),
        Case(nameof(PhysicalReleaseAtTapThresholdDoesNotAccumulateAGapSample),
            () => PhysicalReleaseAtTapThresholdDoesNotAccumulateAGapSample(tree)),
        Case(nameof(CooldownHoldNeverEntersPreviewOrAutoFires),
            () => CooldownHoldNeverEntersPreviewOrAutoFires(tree)),
        Case(nameof(NeutralTapUsesLiveFacingButNeutralHoldSelectsNothing),
            () => NeutralTapUsesLiveFacingButNeutralHoldSelectsNothing(tree)),
        Case(nameof(RealWorldObstructionUsesItsRayHitAndNamedFailuresRemainVisible),
            () => RealWorldObstructionUsesItsRayHitAndNamedFailuresRemainVisible(tree)),
        Case(nameof(TargetFreedDuringTravelFailsWithoutPullOrStaleReference),
            () => TargetFreedDuringTravelFailsWithoutPullOrStaleReference(tree)),
        Case(nameof(DeadLockedTargetStillInSceneFailsBeforeApply),
            () => DeadLockedTargetStillInSceneFailsBeforeApply(tree)),
        Case(nameof(LockedTargetRevalidationCoversGroupQueueRangeAndInsertedLos),
            () => LockedTargetRevalidationCoversGroupQueueRangeAndInsertedLos(tree)),
        Task7CaseRegistry.Case(nameof(ThreadCastRuntimeTests),
            nameof(RealLevelRoomTransitionClearsTheLockedRoomTarget),
            PrologueCanyonLegacyReplacementTests.SectorRetirementHasTypedClawTargets),
        Task7CaseRegistry.Case(nameof(ThreadCastRuntimeTests),
            nameof(ActiveClawRoomTransitionsClearImmediatelyWithoutRepeatingGameplay),
            PrologueCanyonLegacyReplacementTests.PersistentClawAndSectorTargetsShareTheRuntimeContract),
        Case(nameof(EveryRuntimeCancellationSourceClearsTransientPresentation),
            () => EveryRuntimeCancellationSourceClearsTransientPresentation(tree)),
        Case(nameof(NonPreviewCancellationSourcesRemainBoundedAndExactlyOnce),
            () => NonPreviewCancellationSourcesRemainBoundedAndExactlyOnce(tree)),
        Case(nameof(RecoveryMovementAndJumpCancelOnlyPresentation),
            () => RecoveryMovementAndJumpCancelOnlyPresentation(tree))
    ];

    private static Task7Case Case(string name, Func<Task> execute) =>
        Task7CaseRegistry.Case(nameof(ThreadCastRuntimeTests), name, execute);

    private static async Task CooldownHoldNeverEntersPreviewOrAutoFires(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "CooldownHeldInputFixture",
            new Vector2(140f, 0f), cooldownSeconds: 1f);
        int hits = 0;
        int failures = 0;
        bool sawPreview = false;
        fixture.Ability.ThreadHit += () => hits++;
        fixture.Ability.ThreadFailed += () => failures++;
        fixture.Ability.CastFrameChanged += (frame, target) =>
        {
            if (frame.State == ThreadCastState.Preview) sawPreview = true;
        };

        await TapRight(tree);
        await WaitForState(tree, fixture.Ability, ThreadCastState.Apply);
        await WaitForState(tree, fixture.Ability, ThreadCastState.Idle);
        TestAssert.Equal(1, fixture.Receiver.ApplyCount,
            "cooldown-hold setup completes one real cast before testing the next press");
        TestAssert.Equal(1, hits, "cooldown-hold setup emits one real compatibility Hit");
        float ratioBeforeFeedback = fixture.Ability.ReadyRatio;
        TestAssert.True(ratioBeforeFeedback < .999f,
            "the second real press begins while the consumed cooldown is unavailable");

        Input.ActionPress(InputActions.MoveRight);
        PressGrapplePrimary();
        await WaitForState(tree, fixture.Ability, ThreadCastState.Failed);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(ThreadFailureReason.Cooldown, fixture.Ability.CurrentCastFrame.Failure,
            "a held fresh press during cooldown emits the named Cooldown feedback immediately");
        TestAssert.Equal(string.Empty, fixture.HudState.Text,
            "Task10 keeps legacy Cooldown feedback out of player-facing HUD text");
        TestAssert.True(!fixture.Hud.GetNode<Control>("ThreadSpool").Visible &&
                        fixture.Hud.GetNodeOrNull<ProgressBar>("ThreadSpool/Cooldown") == null,
            "Task10 keeps the legacy thread spool hidden and removes its cooldown ring");
        TestAssert.True(fixture.Shuttle.FailureAtCasterVisible,
            "Cooldown feedback uses the small player-local spool and needle cue");
        TestAssert.Equal(0, fixture.Line.ActiveStrandCount,
            "Cooldown feedback never draws a fake target line");
        TestAssert.Near(1f, fixture.Motor.AbilityMovementScale, .001f,
            "Cooldown feedback never applies Preview's 65 percent movement scale");
        TestAssert.True(!fixture.Ability.HasLockedTarget && !fixture.Shuttle.CandidateDiamondVisible,
            "Cooldown feedback never locks or marks the available target");

        for (int index = 0; index < 70; index++)
        {
            await Frames(tree, 1);
            TestAssert.True(fixture.Ability.CurrentCastFrame.State != ThreadCastState.Preview,
                "holding the current primary action through cooldown expiry never enters Preview");
            TestAssert.Equal(0, fixture.Line.ActiveStrandCount,
                "holding the current primary action through cooldown expiry never creates Preview strands");
            TestAssert.Near(1f, fixture.Motor.AbilityMovementScale, .001f,
                "holding the current primary action through cooldown expiry keeps full movement authority");
        }

        TestAssert.Equal(ThreadCastState.Idle, fixture.Ability.CurrentCastFrame.State,
            "cooldown feedback returns to Idle while the same physical hold remains down");
        TestAssert.True(!sawPreview, "no cooldown-held runtime frame publishes Preview");
        TestAssert.Equal(1, fixture.Receiver.ApplyCount,
            "cooldown expiry under the same hold cannot auto-fire or pull again");
        TestAssert.Equal(1, hits, "cooldown expiry under the same hold emits no second Hit");
        TestAssert.Equal(1, failures, "one rejected press produces exactly one failure notification");
        ReleaseGrapplePrimary();
        Input.ActionRelease(InputActions.MoveRight);
        await Frames(tree, 2);
        TestAssert.Equal(1, fixture.Receiver.ApplyCount,
            "releasing the rejected hold is swallowed rather than creating a Tap cast");
        await Dispose(tree, fixture);
    }

    private static async Task PhysicalReleaseAtTapThresholdDoesNotAccumulateAGapSample(SceneTree tree)
    {
        const float threshold = .14f;
        RuntimeFixture shortTap = await CreateFixture(tree, "ThreadThresholdShortFixture",
            new Vector2(180f, 0f), tapThresholdSeconds: threshold);
        Input.ActionPress(InputActions.MoveRight);
        await Frames(tree, 2);
        PressGrapplePrimary();
        await WaitForPhysicalThreadPress(tree, shortTap.Input);
        await Frames(tree, 7); // final real-held sample remains one physics tick below Preview
        // Preserve the real reader/ability scheduling while reproducing the one-sample edge-delivery
        // gap observed at runtime: Held has dropped, but the duplicate Released edge arrives next sample.
        shortTap.Input.SetPhysicsProcess(false);
        SetInputFrame(shortTap.Input, shortTap.Input.CurrentFrame with
        {
            ThreadPressed = false,
            ThreadHeld = false,
            ThreadReleased = false
        });
        await Frames(tree, 1);
        ReleaseGrapplePrimary();
        shortTap.Input.SetPhysicsProcess(true);
        ThreadCastFrame shortResult = await WaitForPublishedState(tree, shortTap.Ability, ThreadCastState.Windup);
        TestAssert.Equal(ThreadAimMode.Tap, shortResult.AimMode,
            "physical release one physics sample below .14 s remains a Tap despite release-edge delivery");
        await Dispose(tree, shortTap);

        RuntimeFixture hold = await CreateFixture(tree, "ThreadThresholdHoldFixture",
            new Vector2(180f, 0f), tapThresholdSeconds: threshold);
        Input.ActionPress(InputActions.MoveRight);
        await Frames(tree, 2);
        PressGrapplePrimary();
        await WaitForPhysicalThreadPress(tree, hold.Input);
        await Frames(tree, 9); // ability observes nine held samples = ~0.150 s at 60 Hz
        await WaitForState(tree, hold.Ability, ThreadCastState.Preview);
        TestAssert.Equal(ThreadAimMode.Hold, hold.Ability.CurrentCastFrame.AimMode,
            "physical hold one physics sample above .14 s enters Hold Preview");
        ReleaseGrapplePrimary();
        await Dispose(tree, hold);
    }

    private static async Task PreviewTravelLatchApplyAndRecoveryUseRealPhysicsFrames(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "ThreadPhaseFixture", new Vector2(180f, 0f));
        var entered = new List<ThreadCastState>();
        int fired = 0;
        int hit = 0;
        fixture.Ability.ThreadFired += () => fired++;
        fixture.Ability.ThreadHit += () => hit++;
        fixture.Ability.CastFrameChanged += (frame, target) =>
        {
            if (entered.Count == 0 || entered[^1] != frame.State) entered.Add(frame.State);
        };

        Input.ActionPress(InputActions.MoveRight);
        PressGrapplePrimary();
        await WaitForState(tree, fixture.Ability, ThreadCastState.Preview);

        TestAssert.Equal(0, fixture.Receiver.ApplyCount, "Preview never applies gameplay pull");
        TestAssert.Near(.65f, fixture.Motor.AbilityMovementScale, .001f,
            "Preview scales only horizontal ability movement to 65 percent");
        TestAssert.Equal(ThreadLineState.Preview, fixture.Line.CurrentState,
            "Preview renders the low-brightness three-strand guide");
        TestAssert.Equal(3, fixture.Line.ActiveStrandCount,
            "Preview is represented by three real strand paths");
        TestAssert.True(fixture.Shuttle.CandidateDiamondVisible,
            "Preview marks the live candidate with the gold diamond");

        Input.ActionPress(InputActions.Jump);
        await Frames(tree, 2);
        TestAssert.True(fixture.Input.CurrentFrame.JumpPressed,
            "Preview leaves the real jump edge on the motor input path");
        TestAssert.True(fixture.Ability.CurrentCastFrame.State == ThreadCastState.Preview,
            "jump input does not collapse Preview");
        Input.ActionRelease(InputActions.Jump);

        ReleaseGrapplePrimary();
        await WaitForState(tree, fixture.Ability, ThreadCastState.Windup);
        Input.ActionRelease(InputActions.MoveRight);
        fixture.Motor.SetPhysicsProcess(false);
        fixture.Player.Velocity = Vector2.Zero;

        int travelFrames = 0;
        float priorTravelX = float.NegativeInfinity;
        float maximumTravelAdvance = 0f;
        bool sawLatchBeforeApply = false;
        bool sawRecovery = false;
        for (int frameIndex = 0; frameIndex < 120; frameIndex++)
        {
            await Frames(tree, 1);
            ThreadCastState state = fixture.Ability.CurrentCastFrame.State;
            if (state == ThreadCastState.Travel)
            {
                travelFrames++;
                TestAssert.True(fixture.Shuttle.ShuttleWorldPosition.X + .001f >= priorTravelX,
                    "the real shuttle advances monotonically through Travel");
                priorTravelX = fixture.Shuttle.ShuttleWorldPosition.X;
                maximumTravelAdvance = Mathf.Max(maximumTravelAdvance,
                    fixture.Shuttle.ShuttleWorldPosition.DistanceTo(fixture.Player.GlobalPosition));
            }
            if (state == ThreadCastState.Latched)
            {
                sawLatchBeforeApply = true;
                TestAssert.Equal(0, fixture.Receiver.ApplyCount,
                    "the diamond latch is observable before gameplay pull");
                TestAssert.True(fixture.Shuttle.LatchVisible,
                    "Latched expands the procedural diamond lock");
                AssertThreeDistinctStrands(fixture.Line, ThreadLineState.Latched, "Latched");
            }
            if (state == ThreadCastState.Apply)
            {
                TestAssert.True(sawLatchBeforeApply, "Apply cannot precede Latched");
                TestAssert.Equal(1, fixture.Receiver.ApplyCount,
                    "Apply calls the real pull receiver exactly once on its state edge");
            }
            if (state == ThreadCastState.Recovery)
            {
                sawRecovery = true;
                TestAssert.Equal(1, fixture.Receiver.ApplyCount,
                    "Recovery cannot duplicate the applied cast");
                AssertThreeDistinctStrands(fixture.Line, ThreadLineState.Recovery, "Recovery");
            }
            if (sawRecovery && state == ThreadCastState.Idle) break;
        }

        TestAssert.True(travelFrames >= 2, "Travel remains observable across multiple real physics frames");
        TestAssert.True(maximumTravelAdvance > 1f,
            "the real runtime shuttle leaves the caster and advances toward the locked target during Travel");
        AssertOrdered(entered, ThreadCastState.Preview, ThreadCastState.Windup, ThreadCastState.Travel,
            ThreadCastState.Latched, ThreadCastState.Apply, ThreadCastState.Recovery, ThreadCastState.Idle);
        TestAssert.Equal(1, fired, "the compatibility fired notification emits once per real cast");
        TestAssert.Equal(1, hit, "the compatibility hit notification emits once per applied cast");
        TestAssert.Equal(1, fixture.Receiver.ApplyCount, "the completed CastId is applied exactly once");
        TestAssert.Equal(0, fixture.Line.ActiveStrandCount, "Idle clears every strand after Recovery");
        TestAssert.True(fixture.Shuttle.IsClear, "Idle clears the shuttle and lock presentation");
        TestAssert.Near(1f, fixture.Motor.AbilityMovementScale, .001f,
            "leaving Preview restores full horizontal movement");
        await Dispose(tree, fixture);
    }

    private static async Task NeutralTapUsesLiveFacingButNeutralHoldSelectsNothing(SceneTree tree)
    {
        RuntimeFixture tap = await CreateFixture(tree, "NeutralTapFixture", new Vector2(-150f, 0f),
            tapThresholdSeconds: 1f);
        Input.ActionPress(InputActions.MoveLeft);
        await Frames(tree, 2);
        PressGrapplePrimary();
        await Frames(tree, 2);
        TestAssert.True(tap.Input.CurrentFrame.ThreadHeld,
            "neutral Tap fixture delivers the held edge to the real input reader");
        Input.ActionRelease(InputActions.MoveLeft);
        await Frames(tree, 2);
        TestAssert.True(tap.Input.CurrentFrame.ThreadHeld && tap.Input.CurrentFrame.Aim.IsZeroApprox(),
            "neutral Tap fixture reaches a zero physical aim while retaining the held thread input");
        ReleaseGrapplePrimary();
        ThreadCastFrame tapWindup = await WaitForPublishedState(tree, tap.Ability, ThreadCastState.Windup);
        TestAssert.Equal(ThreadAimMode.Tap, tapWindup.AimMode,
            "a short neutral release is classified as Tap");
        TestAssert.Equal(tap.Target.GetInstanceId(), tapWindup.TargetId,
            "neutral Tap uses the live left-facing direction instead of a fixed right fallback");
        await WaitForState(tree, tap.Ability, ThreadCastState.Apply);
        TestAssert.Equal(1, tap.Receiver.ApplyCount, "the left-facing neutral Tap reaches its real target");
        await Dispose(tree, tap);

        RuntimeFixture hold = await CreateFixture(tree, "NeutralHoldFixture", new Vector2(150f, 0f));
        PressGrapplePrimary();
        await WaitForState(tree, hold.Ability, ThreadCastState.Preview);
        TestAssert.True(!hold.Ability.CurrentCastFrame.HasTarget,
            "Hold with zero physical aim never inherits the Tap facing fallback");
        TestAssert.True(!hold.Shuttle.CandidateDiamondVisible,
            "neutral Hold does not draw a false candidate lock");
        ReleaseGrapplePrimary();
        await WaitForState(tree, hold.Ability, ThreadCastState.Failed);
        TestAssert.Equal(ThreadFailureReason.NoTarget, hold.Ability.CurrentCastFrame.Failure,
            "neutral Hold release keeps the named no-target failure");
        TestAssert.Equal(0, hold.Receiver.ApplyCount, "neutral Hold never applies pull");
        await AssertFailureWindow(tree, hold, ThreadFailureReason.NoTarget, "NoTarget");
        TestAssert.True(hold.Shuttle.FailureAtCasterVisible,
            "NoTarget uses a small player-local spool and needle failure cue");
        TestAssert.Equal(0, hold.Line.ActiveStrandCount,
            "NoTarget never draws a fake target line");
        await WaitForState(tree, hold.Ability, ThreadCastState.Idle);
        AssertPresentationClear(hold, "NoTarget visual expiry");
        await Dispose(tree, hold);
    }

    private static async Task RealWorldObstructionUsesItsRayHitAndNamedFailuresRemainVisible(SceneTree tree)
    {
        RuntimeFixture blocked = await CreateFixture(tree, "BlockedThreadFixture", new Vector2(210f, 0f));
        var wall = new StaticBody2D
        {
            Name = "ThreadWorldWall",
            Position = new Vector2(105f, 0f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        wall.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(20f, 100f) }
        });
        blocked.Root.AddChild(wall);
        await Frames(tree, 2);

        await TapRight(tree);
        await WaitForState(tree, blocked.Ability, ThreadCastState.Failed);
        TestAssert.Equal(ThreadFailureReason.Obstructed, blocked.Ability.CurrentCastFrame.Failure,
            "a real world ray obstruction keeps the named obstruction failure");
        TestAssert.Equal(ThreadLineState.Failed, blocked.Line.CurrentState,
            "obstruction draws a short failed strand to the collision point");
        TestAssert.True(Mathf.Abs(blocked.Line.LastWorldEnd.X - 95f) < 3f,
            "failed thread terminates at the real near face of the wall");
        TestAssert.True(blocked.Line.LastWorldEnd.DistanceTo(blocked.Target.GlobalPosition) > 80f,
            "failed obstruction never draws a fake line to the target");
        TestAssert.True(blocked.Shuttle.ShuttleWorldPosition.DistanceTo(blocked.Line.LastWorldEnd) < .1f,
            "the failed shuttle spark uses the same real ray hit point");
        TestAssert.Equal(0, blocked.Receiver.ApplyCount, "obstruction cannot apply pull");
        await AssertFailureWindow(tree, blocked, ThreadFailureReason.Obstructed, "Obstructed");
        TestAssert.Equal(1, blocked.Line.ActiveStrandCount,
            "obstruction retains its real endpoint strand throughout the feedback window");
        TestAssert.True(!blocked.Shuttle.IsClear,
            "obstruction retains its real endpoint X throughout the feedback window");
        await WaitForState(tree, blocked.Ability, ThreadCastState.Idle);
        TestAssert.Equal(0, blocked.Line.ActiveStrandCount, "failed obstruction clears after VisualSeconds");
        TestAssert.True(blocked.Shuttle.IsClear, "failed obstruction clears the shuttle after VisualSeconds");
        await Dispose(tree, blocked);

        RuntimeFixture far = await CreateFixture(tree, "OutOfRangeThreadFixture", new Vector2(320f, 0f));
        await TapRight(tree);
        await WaitForState(tree, far.Ability, ThreadCastState.Failed);
        TestAssert.Equal(ThreadFailureReason.OutOfRange, far.Ability.CurrentCastFrame.Failure,
            "Tap beyond 260 pixels keeps the named out-of-range failure");
        TestAssert.Equal(0, far.Receiver.ApplyCount, "out-of-range release never consumes gameplay pull");
        await Dispose(tree, far);

        RuntimeFixture cooldown = await CreateFixture(tree, "CooldownThreadFixture", new Vector2(140f, 0f), 1f);
        await TapRight(tree);
        await WaitForState(tree, cooldown.Ability, ThreadCastState.Idle, requirePriorNonIdle: true);
        await TapRight(tree);
        await WaitForState(tree, cooldown.Ability, ThreadCastState.Failed);
        TestAssert.Equal(ThreadFailureReason.Cooldown, cooldown.Ability.CurrentCastFrame.Failure,
            "a second CastId inside the cooldown keeps the named cooldown failure");
        TestAssert.Equal(1, cooldown.Receiver.ApplyCount,
            "cooldown feedback cannot duplicate the first applied cast");
        await AssertFailureWindow(tree, cooldown, ThreadFailureReason.Cooldown, "Cooldown");
        TestAssert.True(cooldown.Shuttle.FailureAtCasterVisible,
            "Cooldown Tap feedback uses its short player-local spool and needle cue");
        TestAssert.Equal(0, cooldown.Line.ActiveStrandCount,
            "Cooldown Tap feedback draws no fake hit line");
        await WaitForState(tree, cooldown.Ability, ThreadCastState.Idle);
        AssertPresentationClear(cooldown, "Cooldown visual expiry");
        await Dispose(tree, cooldown);
    }

    private static async Task TargetFreedDuringTravelFailsWithoutPullOrStaleReference(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "TargetLossFixture", new Vector2(180f, 0f));
        ThreadTarget2D eventTarget = null;
        fixture.Ability.CastFrameChanged += (frame, target) => eventTarget = target;
        await TapRight(tree);
        await WaitForState(tree, fixture.Ability, ThreadCastState.Travel);
        TestAssert.True(fixture.Ability.HasLockedTarget, "Travel retains a live weak target before loss");

        fixture.Target.Free();
        await Frames(tree, 1);

        TestAssert.Equal(ThreadCastState.Failed, fixture.Ability.CurrentCastFrame.State,
            "freeing the real target during Travel fails the cast");
        TestAssert.Equal(ThreadFailureReason.TargetLost, fixture.Ability.CurrentCastFrame.Failure,
            "Travel loss keeps the named target-lost failure");
        TestAssert.Equal(0, fixture.Receiver.ApplyCount, "target loss before Apply produces zero pull");
        TestAssert.True(!fixture.Ability.HasLockedTarget, "target loss clears the weak runtime reference");
        TestAssert.True(eventTarget == null, "target-loss frame publishes no stale target reference");
        await AssertFailureWindow(tree, fixture, ThreadFailureReason.TargetLost, "TargetLost");
        TestAssert.Equal(1, fixture.Line.ActiveStrandCount,
            "target-loss feedback retains its last real endpoint strand during VisualSeconds");
        TestAssert.True(!fixture.Shuttle.IsClear,
            "target-loss feedback retains its endpoint X during VisualSeconds");
        await WaitForState(tree, fixture.Ability, ThreadCastState.Idle);
        TestAssert.Equal(0, fixture.Line.ActiveStrandCount, "target-loss expiry clears all strands");
        TestAssert.True(fixture.Shuttle.IsClear, "target-loss expiry clears the shuttle");
        await Dispose(tree, fixture);
    }

    private static async Task DeadLockedTargetStillInSceneFailsBeforeApply(SceneTree tree)
    {
        RuntimeFixture fixture = await CreateFixture(tree, "DeadLockedThreadTargetFixture", new Vector2(180f, 0f));
        int hits = 0;
        fixture.Ability.ThreadHit += () => hits++;
        await TapRight(tree);
        await WaitForState(tree, fixture.Ability, ThreadCastState.Travel);

        fixture.TargetHealth.ApplyDamage(new DamageRequest(
            fixture.TargetHealth.MaxHealth, Vector2.Left, 0f, 7101UL));
        await WaitForState(tree, fixture.Ability, ThreadCastState.Failed);

        TestAssert.Equal(ThreadFailureReason.TargetLost, fixture.Ability.CurrentCastFrame.Failure,
            "a dead pull receiver that remains in the scene becomes named TargetLost");
        TestAssert.Equal(0, fixture.Receiver.ApplyCount,
            "dead locked targets receive zero pull before Apply");
        TestAssert.Equal(0, hits, "dead locked targets publish zero compatibility hit notifications");
        TestAssert.True(!fixture.Ability.HasLockedTarget,
            "dead locked target validation clears the weak reference");
        await Dispose(tree, fixture);
    }

    private static async Task LockedTargetRevalidationCoversGroupQueueRangeAndInsertedLos(SceneTree tree)
    {
        RuntimeFixture removed = await CreateFixture(tree, "RemovedLockedThreadTargetFixture", new Vector2(180f, 0f));
        await TapRight(tree);
        await WaitForState(tree, removed.Ability, ThreadCastState.Windup);
        removed.Target.RemoveFromGroup(ThreadTarget2D.TargetGroup);
        await WaitForState(tree, removed.Ability, ThreadCastState.Failed);
        AssertLiveTargetFailure(removed, ThreadFailureReason.TargetLost, "removed target during Windup");
        TestAssert.Near(1f, removed.Ability.ReadyRatio, .001f,
            "target loss before Travel does not consume cooldown");
        await Dispose(tree, removed);

        RuntimeFixture queued = await CreateFixture(tree, "QueuedLockedThreadTargetFixture", new Vector2(180f, 0f));
        await TapRight(tree);
        await WaitForState(tree, queued.Ability, ThreadCastState.Travel);
        queued.Target.QueueFree();
        await WaitForState(tree, queued.Ability, ThreadCastState.Failed);
        AssertLiveTargetFailure(queued, ThreadFailureReason.TargetLost, "QueueFree target during Travel");
        TestAssert.True(queued.Ability.ReadyRatio < .999f,
            "target loss after entering Travel preserves the consumed cooldown");
        await Dispose(tree, queued);

        RuntimeFixture moved = await CreateFixture(tree, "MovedOutOfRangeLockedThreadTargetFixture", new Vector2(180f, 0f));
        await TapRight(tree);
        await WaitForState(tree, moved.Ability, ThreadCastState.Travel);
        moved.Motor.SetPhysicsProcess(false);
        moved.Player.Velocity = Vector2.Zero;
        float tapBoundaryX = moved.Player.GlobalPosition.X + 260f;
        moved.Receiver.Position = new Vector2(420f, 0f);
        await WaitForState(tree, moved.Ability, ThreadCastState.Failed);
        AssertLiveTargetFailure(moved, ThreadFailureReason.OutOfRange, "target moved beyond Tap range during Travel");
        TestAssert.True(Mathf.Abs(moved.Line.LastWorldEnd.X - tapBoundaryX) < 3f,
            "out-of-range failure presentation terminates at the real Tap range boundary");
        await Dispose(tree, moved);

        RuntimeFixture obstructed = await CreateFixture(tree, "InsertedLosLockedThreadTargetFixture", new Vector2(180f, 0f));
        await TapRight(tree);
        await WaitForState(tree, obstructed.Ability, ThreadCastState.Latched);
        StaticBody2D wall = CreateWorldWall("InsertedThreadWall", new Vector2(90f, 0f));
        obstructed.Root.AddChild(wall);
        await WaitForState(tree, obstructed.Ability, ThreadCastState.Failed);
        AssertLiveTargetFailure(obstructed, ThreadFailureReason.Obstructed, "wall inserted during Latched");
        TestAssert.True(Mathf.Abs(obstructed.Line.LastWorldEnd.X - 80f) < 3f,
            "live LOS failure uses the inserted wall's real near-face hit point");
        await Dispose(tree, obstructed);
    }

    private static async Task RealLevelRoomTransitionClearsTheLockedRoomTarget(SceneTree tree)
    {
        ReleaseInput();
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            nameof(RealLevelRoomTransitionClearsTheLockedRoomTarget));
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        level.TransitionTo(new PrologueRoomLink(
            PrologueRoomId.AwakeningCourtyard,
            "to_corridor",
            PrologueRoomId.BrokenToothCorridor,
            "from_courtyard"));
        await WaitForRoom(tree, level, PrologueRoomId.BrokenToothCorridor);

        CharacterBody2D player = level.Player;
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D roomTarget =
            level.CurrentRoom.GetNode<GrappleSurface2D>("Geometry/ThreadAnchor");
        player.GlobalPosition = roomTarget.GlobalPosition + new Vector2(-180f, 0f);
        player.Velocity = Vector2.Zero;
        SuspendGeneratedClaw(input, modes, claw);
        await StepGeneratedClaw(tree, input, modes, claw,
            ClawInput(grappleModePressed: true, primaryPressed: true, primaryHeld: true,
                mouseWorld: roomTarget.WorldAnchor));
        await AdvanceGeneratedClawTo(tree, input, modes, claw, roomTarget,
            MechanicalClawState.Travel);
        TestAssert.Equal(roomTarget, claw.CurrentTarget,
            "real room surface is weak-locked during Travel");
        level.TransitionTo(new PrologueRoomLink(
            PrologueRoomId.BrokenToothCorridor,
            "to_upper",
            PrologueRoomId.UpperPendulum,
            "from_corridor"));
        await WaitForRoom(tree, level, PrologueRoomId.UpperPendulum);
        TestAssert.Equal(MechanicalClawState.Idle, claw.CurrentFrame.State,
            "real level room transition clears the claw before mounting the next room");
        TestAssert.True(claw.CurrentTarget == null,
            "room transition clears the old-room surface reference");
        TestAssert.True(player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line")
                .ActiveStrandCount == 0 &&
            player.GetNode<ThreadShuttleVisual>("VisualRoot/MechanicalClaw").IsClawClear,
            "real room transition leaves no old-room line or shuttle residue");
        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task ActiveClawRoomTransitionsClearImmediatelyWithoutRepeatingGameplay(SceneTree tree)
    {
        await ActiveClawRoomTransitionClearsImmediately(tree, MechanicalClawState.Latched);
        await ActiveClawRoomTransitionClearsImmediately(tree, MechanicalClawState.Released);
    }

    private static async Task ActiveClawRoomTransitionClearsImmediately(SceneTree tree,
        MechanicalClawState transitionState)
    {
        ReleaseInput();
        PrologueLevelController level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level,
            $"{nameof(ActiveClawRoomTransitionClearsImmediately)}-{transitionState}");
        tree.Root.AddChild(level);
        await Frames(tree, 3);
        level.TransitionTo(new PrologueRoomLink(
            PrologueRoomId.AwakeningCourtyard,
            "to_corridor",
            PrologueRoomId.BrokenToothCorridor,
            "from_courtyard"));
        await WaitForRoom(tree, level, PrologueRoomId.BrokenToothCorridor);

        CharacterBody2D player = level.Player;
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        ThreadLineVisual line =
            player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line");
        ThreadShuttleVisual shuttle =
            player.GetNode<ThreadShuttleVisual>("VisualRoot/MechanicalClaw");
        GrappleSurface2D roomTarget =
            level.CurrentRoom.GetNode<GrappleSurface2D>("Geometry/ThreadAnchor");
        player.GlobalPosition = roomTarget.GlobalPosition + new Vector2(-180f, 0f);
        player.Velocity = Vector2.Zero;
        SuspendGeneratedClaw(input, modes, claw);
        await StepGeneratedClaw(tree, input, modes, claw,
            ClawInput(grappleModePressed: true, primaryPressed: true, primaryHeld: true,
                mouseWorld: roomTarget.WorldAnchor));
        await AdvanceGeneratedClawTo(tree, input, modes, claw, roomTarget,
            MechanicalClawState.Latched);
        if (transitionState == MechanicalClawState.Released)
            await StepGeneratedClaw(tree, input, modes, claw,
                ClawInput(primaryReleased: true, mouseWorld: roomTarget.WorldAnchor));
        TestAssert.Equal(transitionState, claw.CurrentFrame.State,
            $"real authored claw reaches {transitionState} before room exit");
        TestAssert.True(!shuttle.IsClawClear,
            $"{transitionState} starts its room transition with a live claw shuttle presentation");
        if (transitionState == MechanicalClawState.Latched)
            TestAssert.True(line.ActiveStrandCount > 0,
                "Latched starts its room transition with live claw strands");

        var transitionFrames = new List<MechanicalClawFrame>();
        claw.ClawFrameChanged += RecordTransitionFrame;
        level.TransitionTo(new PrologueRoomLink(
            PrologueRoomId.BrokenToothCorridor,
            "to_upper",
            PrologueRoomId.UpperPendulum,
            "from_corridor"));
        await WaitForDisabledRoomExitCleanup(tree, claw, line, shuttle, transitionState);
        claw.ClawFrameChanged -= RecordTransitionFrame;

        int targetLostCount = transitionFrames.FindAll(frame =>
            frame.State == MechanicalClawState.Failed &&
            frame.Failure == MechanicalClawFailure.TargetLost).Count;
        int failedIndex = transitionFrames.FindIndex(frame =>
            frame.State == MechanicalClawState.Failed &&
            frame.Failure == MechanicalClawFailure.TargetLost);
        int idleIndex = transitionFrames.FindIndex(frame => frame.State == MechanicalClawState.Idle);
        TestAssert.Equal(1, targetLostCount,
            $"{transitionState} room retirement publishes Failed/TargetLost exactly once");
        TestAssert.True(failedIndex >= 0 && idleIndex > failedIndex,
            $"{transitionState} room retirement exposes TargetLost before terminal Idle cleanup");

        await WaitForRoom(tree, level, PrologueRoomId.UpperPendulum);
        await Frames(tree, 2);
        TestAssert.Equal(MechanicalClawState.Idle, claw.CurrentFrame.State,
            $"the mounted room receives no {transitionState} claw residue");
        TestAssert.True(claw.CurrentTarget == null,
            $"the mounted room receives no {transitionState} target reference");
        TestAssert.Equal(0, line.ActiveStrandCount,
            $"the mounted room receives no {transitionState} strands");
        TestAssert.True(shuttle.IsClawClear,
            $"the mounted room receives no {transitionState} shuttle state");

        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);

        void RecordTransitionFrame(MechanicalClawFrame frame, GrappleSurface2D target) =>
            transitionFrames.Add(frame);
    }

    private static async Task WaitForDisabledRoomExitCleanup(SceneTree tree,
        MechanicalClawController claw, ThreadLineVisual line, ThreadShuttleVisual shuttle,
        MechanicalClawState transitionState)
    {
        for (int index = 0; index < 12; index++)
        {
            if (claw.CurrentFrame.State == MechanicalClawState.Idle &&
                line.ActiveStrandCount == 0 && shuttle.IsClawClear)
            {
                TestAssert.True(!claw.IsPhysicsProcessing(),
                    $"{transitionState} is cleared while gameplay physics is still disabled");
                TestAssert.True(claw.CurrentTarget == null,
                    $"{transitionState} disabled cleanup clears the current target");
                TestAssert.Near(1f, claw.GetParent<CharacterBody2D>()
                        .GetNode<PlayerMotor>("Motor").GrappleMovementScale, .001f,
                    $"{transitionState} disabled cleanup restores movement scale");
                return;
            }
            if (claw.IsPhysicsProcessing())
                throw new InvalidOperationException(
                    $"{transitionState} room transition re-enabled gameplay before clearing claw presentation");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
        throw new InvalidOperationException(
            $"Timed out waiting for disabled {transitionState} room-exit claw cleanup; " +
            $"state={claw.CurrentFrame.State}, strands={line.ActiveStrandCount}, " +
            $"shuttleClear={shuttle.IsClawClear}");
    }

    private static async Task EveryRuntimeCancellationSourceClearsTransientPresentation(SceneTree tree)
    {
        RuntimeFixture dash = await CreateFixture(tree, "DashCancelFixture", new Vector2(150f, 0f));
        await EnterPreview(tree, dash);
        Input.ActionPress(InputActions.Dash);
        await Frames(tree, 2);
        AssertCancelledAndClear(dash, "dash");
        await Dispose(tree, dash);

        RuntimeFixture hurt = await CreateFixture(tree, "HurtCancelFixture", new Vector2(150f, 0f));
        await EnterPreview(tree, hurt);
        hurt.Health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 7001UL));
        AssertCancelledAndClear(hurt, "Health.Damaged");
        await Dispose(tree, hurt);

        RuntimeFixture death = await CreateFixture(tree, "DeathCancelFixture", new Vector2(150f, 0f));
        await EnterPreview(tree, death);
        death.Health.ApplyDamage(new DamageRequest(death.Health.MaxHealth, Vector2.Left, 0f, 7002UL));
        AssertCancelledAndClear(death, "Health.Died");
        await Dispose(tree, death);

        RuntimeFixture paused = await CreateFixture(tree, "PauseCancelFixture", new Vector2(150f, 0f));
        await EnterPreview(tree, paused);
        tree.Paused = true;
        try
        {
            await Frames(tree, 2);
            AssertCancelledAndClear(paused, "pause");
        }
        finally
        {
            tree.Paused = false;
        }
        await Dispose(tree, paused);

        RuntimeFixture roomExit = await CreateFixture(tree, "RoomExitCancelFixture", new Vector2(150f, 0f));
        await EnterPreview(tree, roomExit);
        roomExit.Ability.SetPhysicsProcess(false);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(ThreadCastState.Idle, roomExit.Ability.CurrentCastFrame.State,
            "room-exit processing disable resets cast state");
        AssertPresentationClear(roomExit, "room exit");
        roomExit.Ability.SetPhysicsProcess(true);
        await Dispose(tree, roomExit);

        RuntimeFixture reset = await CreateFixture(tree, "RoomResetFixture", new Vector2(150f, 0f));
        await EnterPreview(tree, reset);
        reset.Ability.ResetTransientState();
        TestAssert.Equal(ThreadCastState.Idle, reset.Ability.CurrentCastFrame.State,
            "room reset rebuilds the pure cast model at Idle");
        AssertPresentationClear(reset, "room reset");
        await Dispose(tree, reset);
    }

    private static async Task NonPreviewCancellationSourcesRemainBoundedAndExactlyOnce(SceneTree tree)
    {
        RuntimeFixture dash = await CreateFixture(tree, "TravelDashCancelFixture", new Vector2(180f, 0f));
        int dashHits = 0;
        dash.Ability.ThreadHit += () => dashHits++;
        await TapRight(tree);
        await WaitForState(tree, dash.Ability, ThreadCastState.Travel);
        Input.ActionPress(InputActions.Dash);
        await WaitForState(tree, dash.Ability, ThreadCastState.Failed);
        AssertPostTravelCancelled(dash, dashHits, "dash during Travel");
        await Dispose(tree, dash);

        RuntimeFixture hurt = await CreateFixture(tree, "LatchedHurtCancelFixture", new Vector2(180f, 0f));
        int hurtHits = 0;
        hurt.Ability.ThreadHit += () => hurtHits++;
        await TapRight(tree);
        await WaitForState(tree, hurt.Ability, ThreadCastState.Latched);
        hurt.Health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 7201UL));
        AssertPostTravelCancelled(hurt, hurtHits, "Health.Damaged during Latched");
        await Dispose(tree, hurt);

        RuntimeFixture paused = await CreateFixture(tree, "WindupPauseCancelFixture", new Vector2(180f, 0f));
        await TapRight(tree);
        await WaitForState(tree, paused.Ability, ThreadCastState.Windup);
        tree.Paused = true;
        try
        {
            await WaitForState(tree, paused.Ability, ThreadCastState.Failed);
            TestAssert.Equal(ThreadFailureReason.Cancelled, paused.Ability.CurrentCastFrame.Failure,
                "pause during Windup preserves the named cancellation reason");
            TestAssert.Equal(0, paused.Receiver.ApplyCount, "pause during Windup applies zero pull");
            TestAssert.Near(1f, paused.Ability.ReadyRatio, .001f,
                "pause before Travel consumes no cooldown");
            AssertPresentationClear(paused, "pause during Windup");
        }
        finally
        {
            tree.Paused = false;
        }
        await Dispose(tree, paused);
    }

    private static async Task RecoveryMovementAndJumpCancelOnlyPresentation(SceneTree tree)
    {
        await CancelRecoveryWithRealInput(tree, jump: false);
        await CancelRecoveryWithRealInput(tree, jump: true);
    }

    private static async Task CancelRecoveryWithRealInput(SceneTree tree, bool jump)
    {
        string source = jump ? "JumpPressed" : "movement intent";
        RuntimeFixture fixture = await CreateFixture(tree,
            jump ? "RecoveryJumpCancelFixture" : "RecoveryMoveCancelFixture", new Vector2(180f, 0f));
        int hits = 0;
        int failures = 0;
        fixture.Ability.ThreadHit += () => hits++;
        fixture.Ability.ThreadFailed += () => failures++;

        await TapRight(tree);
        await WaitForState(tree, fixture.Ability, ThreadCastState.Recovery);
        TestAssert.Equal(1, fixture.Receiver.ApplyCount,
            $"{source} setup reaches Recovery after exactly one Apply");
        TestAssert.Equal(1, hits, $"{source} setup reaches Recovery after exactly one Hit");
        float ratioBeforeCancel = fixture.Ability.ReadyRatio;
        Vector2 positionBeforeCancel = fixture.Player.GlobalPosition;

        if (jump) Input.ActionPress(InputActions.Jump);
        else Input.ActionPress(InputActions.MoveRight);
        // The PhysicsFrame signal resumes before sibling _PhysicsProcess callbacks. Movement is
        // level-triggered and already present; the just-pressed jump edge is observed on the next
        // completed reader sample, hence the second bounded signal here.
        await Frames(tree, jump ? 2 : 1);

        TestAssert.Equal(ThreadCastState.Idle, fixture.Ability.CurrentCastFrame.State,
            $"real {source} cancels Recovery presentation on its first physics sample");
        TestAssert.Equal(0, failures,
            $"real {source} cancellation does not emit ThreadFailed");
        TestAssert.Equal(1, fixture.Receiver.ApplyCount,
            $"real {source} cancellation cannot duplicate or undo Apply");
        TestAssert.Equal(1, hits,
            $"real {source} cancellation cannot duplicate the compatibility Hit");
        TestAssert.True(fixture.Ability.ReadyRatio >= ratioBeforeCancel && fixture.Ability.ReadyRatio < .999f,
            $"real {source} cancellation preserves rather than refunds the consumed cooldown");
        AssertPresentationClear(fixture, $"real {source} Recovery cancellation");

        if (jump)
        {
            TestAssert.True(fixture.Input.CurrentFrame.JumpPressed,
                "Recovery cancellation leaves the actual JumpPressed edge on PlayerMotor's path");
            TestAssert.True(fixture.Player.Velocity.Y < 0f,
                "Recovery cancellation does not consume the real jump impulse");
            Input.ActionRelease(InputActions.Jump);
        }
        else
        {
            TestAssert.True(fixture.Input.CurrentFrame.MoveAxis > .9f,
                "Recovery cancellation leaves the actual movement axis on PlayerMotor's path");
            await Frames(tree, 1);
            TestAssert.True(fixture.Player.GlobalPosition.X > positionBeforeCancel.X,
                "Recovery cancellation does not block real horizontal movement");
            Input.ActionRelease(InputActions.MoveRight);
        }

        await Dispose(tree, fixture);
    }

    private static async Task AssertFailureWindow(SceneTree tree, RuntimeFixture fixture,
        ThreadFailureReason expected, string source)
    {
        ThreadCastFrame entered = fixture.Ability.CurrentCastFrame;
        TestAssert.Equal(ThreadCastState.Failed, entered.State,
            $"{source} starts an observable Failed window");
        TestAssert.Equal(expected, entered.Failure,
            $"{source} preserves its named failure for the visual window");
        TestAssert.Near(fixture.Ability.Tuning.VisualSeconds, entered.StateDurationSeconds, .0001f,
            $"{source} uses centralized ThreadAbilitySettings.VisualSeconds");
        TestAssert.True(entered.StateRemainingSeconds > .08f,
            $"{source} begins with enough remaining time for multiple 60 FPS frames");

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(string.Empty, fixture.HudState.Text,
            $"{source} remains absent from Task10 player-facing legacy HUD text");
        await Frames(tree, 3);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(ThreadCastState.Failed, fixture.Ability.CurrentCastFrame.State,
            $"{source} remains observable after three real 60 FPS physics frames");
        TestAssert.Equal(string.Empty, fixture.HudState.Text,
            $"{source} cannot restore stale player-facing legacy HUD text");
        TestAssert.True(fixture.Ability.CurrentCastFrame.StateRemainingSeconds > 0f &&
            fixture.Ability.CurrentCastFrame.StateRemainingSeconds < entered.StateRemainingSeconds,
            $"{source} failure countdown advances toward controlled cleanup");
    }

    private static async Task<RuntimeFixture> CreateFixture(SceneTree tree, string name, Vector2 targetOffset,
        float cooldownSeconds = .35f, float tapThresholdSeconds = .1f)
    {
        ReleaseInput();
        await Frames(tree, 1);
        var root = new Node2D { Name = name };
        var floor = new StaticBody2D
        {
            Name = "Floor",
            Position = new Vector2(0f, 30f),
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        floor.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(800f, 20f) }
        });
        var player = new CharacterBody2D
        {
            Name = "Player",
            CollisionLayer = CombatCollisionLayers.Player,
            CollisionMask = CombatCollisionLayers.World
        };
        player.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new CapsuleShape2D { Radius = 10f, Height = 40f }
        });
        var input = new LegacyThreadFixtureInputReader { Name = "Input" };
        var motor = new PlayerMotor
        {
            Name = "Motor",
            Tuning = new PlayerTuning { Gravity = .01f }
        };
        var health = new HealthComponent { Name = "Health" };
        var line = new ThreadLineVisual { Name = "ThreadLine" };
        var shuttle = new ThreadShuttleVisual { Name = "ThreadShuttle" };
        var ability = new ThreadAbility
        {
            Name = "ThreadAbility",
            ShuttlePath = new NodePath("../ThreadShuttle"),
            Tuning = RuntimeTuning(cooldownSeconds, tapThresholdSeconds)
        };
        player.AddChild(input);
        player.AddChild(motor);
        player.AddChild(health);
        player.AddChild(line);
        player.AddChild(shuttle);
        player.AddChild(ability);

        var formalHud = new PrologueFormalHud
        {
            Name = "FormalHud",
            ThreadAbilityPath = new NodePath("../Player/ThreadAbility")
        };
        var threadSpool = new Control { Name = "ThreadSpool", Visible = false };
        var hudState = new Label { Name = "State" };
        threadSpool.AddChild(hudState);
        formalHud.AddChild(threadSpool);

        var receiver = new PullReceiverProbe { Name = "Receiver", Position = targetOffset };
        var targetHealth = new HealthComponent
        {
            Name = "Health",
            MaxHealth = 5,
            InvulnerabilitySeconds = 0f
        };
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Kind = ThreadTargetKind.LightEnemy
        };
        receiver.AddChild(targetHealth);
        receiver.AddChild(target);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(receiver);
        root.AddChild(formalHud);
        tree.Root.AddChild(root);
        await Frames(tree, 2);
        return new RuntimeFixture(root, player, input, motor, health, line, shuttle, ability, receiver,
            targetHealth, target, formalHud, hudState);
    }

    private static StaticBody2D CreateWorldWall(string name, Vector2 position)
    {
        var wall = new StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        wall.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(20f, 100f) }
        });
        return wall;
    }

    private static async Task EnterPreview(SceneTree tree, RuntimeFixture fixture)
    {
        Input.ActionPress(InputActions.MoveRight);
        PressGrapplePrimary();
        await WaitForState(tree, fixture.Ability, ThreadCastState.Preview);
        TestAssert.Equal(0, fixture.Receiver.ApplyCount, "cancellation setup reaches Preview without pulling");
    }

    private static async Task TapRight(SceneTree tree)
    {
        Input.ActionPress(InputActions.MoveRight);
        PressGrapplePrimary();
        await Frames(tree, 2);
        ReleaseGrapplePrimary();
        await Frames(tree, 1);
        Input.ActionRelease(InputActions.MoveRight);
    }

    private static async Task WaitForState(SceneTree tree, ThreadAbility ability, ThreadCastState expected,
        bool requirePriorNonIdle = false)
    {
        bool sawNonIdle = !requirePriorNonIdle && ability.CurrentCastFrame.State != ThreadCastState.Idle;
        for (int index = 0; index < 180; index++)
        {
            ThreadCastState state = ability.CurrentCastFrame.State;
            if (state != ThreadCastState.Idle) sawNonIdle = true;
            if (state == expected && sawNonIdle) return;
            await Frames(tree, 1);
        }
        throw new InvalidOperationException($"Timed out waiting for thread state {expected}; current={ability.CurrentCastFrame.State}");
    }

    private static async Task WaitForPhysicalThreadPress(SceneTree tree, PlayerInputReader input)
    {
        for (int index = 0; index < 30; index++)
        {
            await Frames(tree, 1);
            if (input.CurrentFrame.ThreadPressed) return;
        }
        throw new InvalidOperationException("Timed out waiting for the real PlayerInputReader thread press edge");
    }

    private static async Task WaitForRoom(SceneTree tree, PrologueLevelController level, PrologueRoomId room)
    {
        for (int index = 0; index < 120; index++)
        {
            if (level.CurrentRoomId == room && level.Player
                    .GetNode<MechanicalClawController>("MechanicalClaw").IsPhysicsProcessing())
                return;
            await Frames(tree, 1);
        }
        throw new InvalidOperationException($"Timed out waiting for real prologue room {room}");
    }

    private static async Task WaitFor(SceneTree tree, Func<bool> predicate, string description)
    {
        for (int index = 0; index < 120; index++)
        {
            if (predicate()) return;
            await Frames(tree, 1);
        }
        throw new InvalidOperationException($"Timed out waiting for {description}");
    }

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame)
    {
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame))!.SetValue(input, frame);
    }

    private static void SuspendGeneratedClaw(PlayerInputReader input,
        PlayerActionModeController modes, MechanicalClawController claw)
    {
        input.SetPhysicsProcess(false);
        modes.SetPhysicsProcess(false);
        claw.SetPhysicsProcess(false);
        claw.ResetTransientState();
    }

    private static async Task StepGeneratedClaw(SceneTree tree, PlayerInputReader input,
        PlayerActionModeController modes, MechanicalClawController claw, PlayerInputFrame frame)
    {
        SetInputFrame(input, frame);
        modes._PhysicsProcess(0d);
        claw._PhysicsProcess(1d / 60d);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task AdvanceGeneratedClawTo(SceneTree tree, PlayerInputReader input,
        PlayerActionModeController modes, MechanicalClawController claw, GrappleSurface2D target,
        MechanicalClawState expected)
    {
        for (int index = 0; index < 180; index++)
        {
            if (claw.CurrentFrame.State == expected) return;
            if (claw.CurrentFrame.State == MechanicalClawState.Failed)
                throw new InvalidOperationException(
                    $"Mechanical claw failed before {expected}: {claw.CurrentFrame.Failure}");
            await StepGeneratedClaw(tree, input, modes, claw,
                ClawInput(primaryHeld: true, mouseWorld: target.WorldAnchor));
        }
        throw new InvalidOperationException(
            $"Timed out waiting for generated mechanical-claw state {expected}; " +
            $"current={claw.CurrentFrame.State}");
    }

    private static PlayerInputFrame ClawInput(bool grappleModePressed = false,
        bool primaryPressed = false, bool primaryHeld = false, bool primaryReleased = false,
        Vector2 mouseWorld = default) =>
        new(0f, Vector2.Zero, false, false, false, false, false,
            MouseWorld: mouseWorld,
            GrappleModePressed: grappleModePressed,
            PrimaryPressed: primaryPressed,
            PrimaryHeld: primaryHeld,
            PrimaryReleased: primaryReleased);

    private static async Task<ThreadCastFrame> WaitForPublishedState(SceneTree tree, ThreadAbility ability,
        ThreadCastState expected)
    {
        ThreadCastFrame found = default;
        bool observed = false;
        void OnFrame(ThreadCastFrame frame, ThreadTarget2D target)
        {
            if (frame.State != expected) return;
            found = frame;
            observed = true;
        }

        ability.CastFrameChanged += OnFrame;
        try
        {
            if (ability.CurrentCastFrame.State == expected) return ability.CurrentCastFrame;
            for (int index = 0; index < 180 && !observed; index++) await Frames(tree, 1);
            if (observed) return found;
            throw new InvalidOperationException(
                $"Timed out waiting for published thread state {expected}; current={ability.CurrentCastFrame.State}");
        }
        finally
        {
            ability.CastFrameChanged -= OnFrame;
        }
    }

    private static void AssertCancelledAndClear(RuntimeFixture fixture, string source)
    {
        TestAssert.Equal(ThreadCastState.Failed, fixture.Ability.CurrentCastFrame.State,
            $"{source} maps to the real cancelled cast frame");
        TestAssert.Equal(ThreadFailureReason.Cancelled, fixture.Ability.CurrentCastFrame.Failure,
            $"{source} preserves the named cancellation reason");
        TestAssert.Near(1f, fixture.Ability.ReadyRatio, .001f,
            $"{source} Preview cancellation consumes no cooldown");
        AssertPresentationClear(fixture, source);
    }

    private static void AssertLiveTargetFailure(RuntimeFixture fixture, ThreadFailureReason expected, string source)
    {
        TestAssert.Equal(ThreadCastState.Failed, fixture.Ability.CurrentCastFrame.State,
            $"{source} enters the bounded failure frame");
        TestAssert.Equal(expected, fixture.Ability.CurrentCastFrame.Failure,
            $"{source} preserves its named live-validation failure");
        TestAssert.Equal(0, fixture.Receiver.ApplyCount, $"{source} applies zero pull");
        TestAssert.True(!fixture.Ability.HasLockedTarget, $"{source} clears the locked weak reference");
    }

    private static void AssertPostTravelCancelled(RuntimeFixture fixture, int hits, string source)
    {
        TestAssert.Equal(ThreadCastState.Failed, fixture.Ability.CurrentCastFrame.State,
            $"{source} enters a bounded Failed frame");
        TestAssert.Equal(ThreadFailureReason.Cancelled, fixture.Ability.CurrentCastFrame.Failure,
            $"{source} preserves the named cancellation reason");
        TestAssert.Equal(0, fixture.Receiver.ApplyCount, $"{source} applies zero pull");
        TestAssert.Equal(0, hits, $"{source} emits no compatibility hit");
        TestAssert.True(fixture.Ability.ReadyRatio < .999f,
            $"{source} preserves cooldown already consumed at Travel");
        AssertPresentationClear(fixture, source);
    }

    private static void AssertPresentationClear(RuntimeFixture fixture, string source)
    {
        TestAssert.Equal(0, fixture.Line.ActiveStrandCount, $"{source} clears all thread strands");
        TestAssert.True(fixture.Shuttle.IsClear, $"{source} clears shuttle, latch, and candidate visuals");
        TestAssert.Near(1f, fixture.Motor.AbilityMovementScale, .001f,
            $"{source} restores full horizontal movement");
    }

    private static void AssertThreeDistinctStrands(ThreadLineVisual line, ThreadLineState expected, string phase)
    {
        TestAssert.Equal(expected, line.CurrentState, $"{phase} keeps its named line stage");
        TestAssert.Equal(3, line.ActiveStrandCount, $"{phase} keeps three connected strands visible");
        Vector2[] first = line.GetStrandPoints(0);
        Vector2[] second = line.GetStrandPoints(1);
        Vector2[] third = line.GetStrandPoints(2);
        TestAssert.True(first.Length >= 4 && second.Length >= 4 && third.Length >= 4,
            $"{phase} gives every strand a real multi-point path");
        TestAssert.True(first[1].DistanceTo(second[1]) > .1f && second[1].DistanceTo(third[1]) > .1f,
            $"{phase} uses distinct paths instead of one thick duplicated line");
    }

    private static void AssertOrdered(IReadOnlyList<ThreadCastState> states, params ThreadCastState[] expected)
    {
        int prior = -1;
        foreach (ThreadCastState state in expected)
        {
            int found = -1;
            for (int index = prior + 1; index < states.Count; index++)
            {
                if (states[index] != state) continue;
                found = index;
                break;
            }
            TestAssert.True(found > prior, $"runtime cast publishes ordered phase {state}");
            prior = found;
        }
    }

    private static ThreadTuning RuntimeTuning(float cooldownSeconds = .35f, float tapThresholdSeconds = .1f) => new()
    {
        TapThresholdSeconds = tapThresholdSeconds,
        CooldownSeconds = cooldownSeconds,
        WindupSeconds = .04f,
        MinimumTravelSeconds = .08f,
        MaximumTravelSeconds = .12f,
        TravelDistanceForMaximumDuration = 400f,
        LatchSeconds = .05f,
        ApplySeconds = .05f,
        RecoverySeconds = .08f
    };

    private static async Task Dispose(SceneTree tree, RuntimeFixture fixture)
    {
        ReleaseInput();
        fixture.Root.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await Frames(tree, 1);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
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

    private static void PressGrapplePrimary()
    {
        Input.ActionPress(InputActions.SelectGrappleMode);
        Input.ActionPress(InputActions.PrimaryAction);
    }

    private static void ReleaseGrapplePrimary()
    {
        Input.ActionRelease(InputActions.PrimaryAction);
        Input.ActionRelease(InputActions.SelectGrappleMode);
    }

    private sealed record RuntimeFixture(
        Node2D Root,
        CharacterBody2D Player,
        PlayerInputReader Input,
        PlayerMotor Motor,
        HealthComponent Health,
        ThreadLineVisual Line,
        ThreadShuttleVisual Shuttle,
        ThreadAbility Ability,
        PullReceiverProbe Receiver,
        HealthComponent TargetHealth,
        ThreadTarget2D Target,
        PrologueFormalHud Hud,
        Label HudState);

    private sealed partial class LegacyThreadFixtureInputReader : PlayerInputReader
    {
        private bool _grappleSelected;

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            PlayerInputFrame frame = CurrentFrame;
            if (frame.AttackModePressed) _grappleSelected = false;
            if (frame.GrappleModePressed) _grappleSelected = true;
            SetInputFrame(this, frame with
            {
                ThreadPressed = _grappleSelected && frame.PrimaryPressed,
                ThreadHeld = _grappleSelected && frame.PrimaryHeld,
                ThreadReleased = _grappleSelected && frame.PrimaryReleased
            });
        }
    }

    private sealed partial class PullReceiverProbe : Node2D, IThreadPullReceiver
    {
        public int ApplyCount { get; private set; }
        public Vector2 LastVelocity { get; private set; }

        public void ApplyThreadPull(Vector2 velocity)
        {
            ApplyCount++;
            LastVelocity = velocity;
        }
    }
}
