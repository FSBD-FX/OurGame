using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class SeamlessPrologueWorldTests
{
    private const float SectorWidth = 128f;
    private const float SectorTop = -200f;
    private const float SectorHeight = 400f;

    public static async Task Run(SceneTree tree)
    {
        await AutomaticBoundaryCrossingPreservesThePersistentPlayerAndBlendsCamera(tree);
        await CheckpointRestoreAndNewRunResetUseContinuousWorldCoordinates(tree);
        await LatchedTargetUnloadsBeforeRetirementAndTenRoundTripsStayBounded(tree);
        await AuthorizedFarRelocationsSnapCameraAfterPlayerPlacement(tree);
        await ThrowingObserverReentryAndSameTickRemountStayTransactional(tree);
        await MountFailureRollsBackAndLaterCrossingRecovers(tree);
        await SeamlessNewRunClearsPlayerTransientState(tree);
    }

    public static async Task RunFixRound2(SceneTree tree)
    {
        await DirectMultihopPositionChangesSnapWhileAdjacentCrossingsBlend(tree);
        await ReentrantTransitionsCoalesceAndFailedActiveWorkRetainsPending(tree);
        await StagingCompletesReadyAndBindingBeforeTargetLostCommit(tree);
        await MechanicalClawPublicationIsNonReentrantAndResetIsIdempotent(tree);
        await LatchedTargetUnloadsBeforeRetirementAndTenRoundTripsStayBounded(tree);
        await SeamlessNewRunClearsPlayerTransientState(tree);
    }

    public static async Task RunFixRound3(SceneTree tree)
    {
        await StagedSectorLifecycleRunsExactlyOnceAndPreservesEncounterActors(tree);
        await StagingFailureHasZeroLiveWorldSideEffects(tree);
    }

    public static async Task RunFixRound4(SceneTree tree)
    {
        await FinalNameBeforeReadyEliminatesRenamedMutationAndSynchronousTicks(tree);
        await VisibilityActivationFailureRollsBackBeforeTargetLost(tree);
    }

    public static async Task RunFixRound5(SceneTree tree)
    {
        await EscapedCheckpointActivationRollsBackByOwnedIdentityAndCanRetry(tree);
    }

    public static async Task RunFixRound1(SceneTree tree)
    {
        await AuthorizedFarRelocationsSnapCameraAfterPlayerPlacement(tree);
        await ThrowingObserverReentryAndSameTickRemountStayTransactional(tree);
        await MountFailureRollsBackAndLaterCrossingRecovers(tree);
        await SeamlessNewRunClearsPlayerTransientState(tree);
    }

    private static async Task AutomaticBoundaryCrossingPreservesThePersistentPlayerAndBlendsCamera(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7PersistentPlayer", withEntranceTarget: false);
        try
        {
            await PrepareNonDefaultPlayerState(tree, fixture);
            Node playerParent = fixture.Player.GetParent();
            ulong playerId = fixture.Player.GetInstanceId();
            float energy = fixture.Energy.EnergyRatio;
            float heat = fixture.Energy.HeatRatio;
            PlayerActionMode mode = fixture.Modes.CurrentMode;
            int slot = fixture.Modes.ActiveSlot;
            int initialCameraRight = fixture.Camera.LimitRight;
            Vector2 previousPosition = fixture.Player.GlobalPosition;

            fixture.Driver.DriveAxis = 1f;
            for (int frame = 0; frame < 90 &&
                fixture.World.CurrentSector == PrologueSectorId.CanyonEntrance; frame++)
            {
                await PhysicsFrames(tree, 1);
                Vector2 current = fixture.Player.GlobalPosition;
                TestAssert.True(current.X > previousPosition.X,
                    "a real CharacterBody2D advances through the seam under automatic physics");
                TestAssert.True(current.DistanceTo(previousPosition) < 40f,
                    "ordinary seam crossing contains no position rewrite or room teleport");
                previousPosition = current;
            }
            fixture.Driver.DriveAxis = 0f;

            TestAssert.Equal(PrologueSectorId.CanyonDescent, fixture.World.CurrentSector,
                "crossing the authored boundary updates the current sector without an exit trigger");
            TestAssert.True(fixture.Level.CurrentSector == PrologueSectorId.CanyonDescent,
                "configured level current-location queries delegate to SeamlessPrologueWorld");
            TestAssert.Equal(playerId, fixture.Level.Player.GetInstanceId(),
                "the exact persistent player instance survives the seam");
            TestAssert.True(ReferenceEquals(playerParent, fixture.Player.GetParent()) &&
                playerParent.Name == "PlayerRoot",
                "the player remains under PlayerRoot and is never reparented below a sector");
            TestAssert.Near(fixture.Driver.Speed, fixture.Player.Velocity.X, .01f,
                "sector changes preserve the real player velocity");
            TestAssert.Near(energy, fixture.Energy.EnergyRatio, .0001f,
                "sector changes preserve non-default energy");
            TestAssert.Near(heat, fixture.Energy.HeatRatio, .0001f,
                "sector changes preserve non-default heat");
            TestAssert.Equal(mode, fixture.Modes.CurrentMode,
                "sector changes preserve the selected action mode");
            TestAssert.Equal(slot, fixture.Modes.ActiveSlot,
                "sector changes preserve the selected loadout slot");
            TestAssert.True(fixture.Camera.LimitRight > initialCameraRight &&
                fixture.Camera.LimitRight < SectorWidth * 2f,
                "camera limits begin a smooth interpolation instead of snapping at the seam");
            int distinctCameraFrames = 0;
            int previousRight = fixture.Camera.LimitRight;
            for (int frame = 0; frame < 60 && fixture.Camera.LimitRight != SectorWidth * 2f; frame++)
            {
                await PhysicsFrames(tree, 1);
                if (fixture.Camera.LimitRight != previousRight) distinctCameraFrames++;
                TestAssert.True(fixture.Camera.LimitRight >= previousRight,
                    "adjacent camera convergence moves monotonically toward the next sector");
                previousRight = fixture.Camera.LimitRight;
            }
            TestAssert.True(distinctCameraFrames > 1 && fixture.Camera.LimitRight == SectorWidth * 2f,
                "ordinary adjacent crossing converges over multiple frames before exact limits");
            TestAssert.Equal(PrologueSectorId.CanyonDescent,
                InvokeResolve(fixture.World, new Vector2(SectorWidth, 0f)),
                "the exact shared seam deterministically stays in the current sector");
            TestAssert.Equal(PrologueSectorId.CanyonDescent,
                InvokeResolve(fixture.World, new Vector2(-10000f, 10000f)),
                "an out-of-world point cannot invent a sector transition");

            fixture.Driver.DriveAxis = -1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonEntrance, 90,
                "real reverse movement crosses the same seam without a teleport");
            fixture.Driver.DriveAxis = 0f;
            TestAssert.True(fixture.Camera.LimitRight < SectorWidth * 2f &&
                fixture.Camera.LimitRight > SectorWidth,
                "reverse adjacent crossing also begins with smooth camera interpolation");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent],
                "reverse crossing restores the ordered entrance required set");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task CheckpointRestoreAndNewRunResetUseContinuousWorldCoordinates(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7CheckpointLifecycle", withEntranceTarget: false);
        try
        {
            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonBasin, 120,
                "real movement reaches the basin checkpoint fixture");
            fixture.Driver.DriveAxis = 0f;
            await PhysicsFrames(tree, 1);
            Vector2 checkpoint = fixture.Player.GlobalPosition;

            TestAssert.True(fixture.World.TryActivateCheckpoint(
                    PrologueSectorId.CanyonBasin, checkpoint),
                "the current sector accepts a finite checkpoint position inside its world bounds");
            TestAssert.True(!fixture.World.TryActivateCheckpoint(
                    PrologueSectorId.LostTemple, checkpoint),
                "after activation, a future sector cannot overwrite the checkpoint snapshot");
            TestAssert.True(!fixture.World.TryActivateCheckpoint(
                    PrologueSectorId.CanyonAscent,
                    new Vector2(SectorWidth * 3f + 32f, 0f)),
                "after activation, a mounted adjacent sector cannot overwrite the snapshot");
            TestAssert.True(!fixture.World.TryActivateCheckpoint(
                    PrologueSectorId.CanyonBasin,
                    new Vector2(SectorWidth * 4f + 32f, 0f)),
                "after activation, an out-of-bounds coordinate cannot overwrite the snapshot");
            TestAssert.True(!fixture.World.TryActivateCheckpoint(
                    PrologueSectorId.CanyonBasin, new Vector2(float.NaN, 0f)),
                "after activation, NaN cannot overwrite the checkpoint snapshot");

            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonAscent, 120,
                "real movement leaves the activated basin checkpoint");
            fixture.Driver.DriveAxis = 0f;
            Node parent = fixture.Player.GetParent();

            TestAssert.True(fixture.World.RestoreCheckpoint(),
                "restore succeeds after seamless-world initialization");
            TestAssert.True(fixture.Player.GlobalPosition.DistanceTo(checkpoint) < .01f,
                "restore returns to the exact activated continuous-world coordinate");
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "restore prepares the checkpoint sector rather than the furthest loaded sector");
            TestAssert.True(ReferenceEquals(parent, fixture.Player.GetParent()),
                "checkpoint restore does not reparent the persistent player");

            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonAscent, 120,
                "the player can leave the restored checkpoint again");
            fixture.Driver.DriveAxis = 0f;
            fixture.Level.StartNewGame();
            TestAssert.True(fixture.Player.GlobalPosition.DistanceTo(new Vector2(32f, 0f)) < .01f,
                "level new-run reset delegates to the seamless initial world coordinate");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "new-run reset clears the activated sector and returns to entrance");
            TestAssert.True(fixture.World.RestoreCheckpoint() &&
                fixture.Player.GlobalPosition.DistanceTo(new Vector2(32f, 0f)) < .01f,
                "after reset, restore uses the initial spawn instead of the old checkpoint");

            fixture.Level.SetGameplayActive(false);
            TestAssert.True(!fixture.WorldRoot.Visible,
                "level visibility delegation hides the continuous world root");
            fixture.Level.SetGameplayActive(true);
            TestAssert.True(fixture.WorldRoot.Visible,
                "level visibility delegation restores the continuous world root");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task AuthorizedFarRelocationsSnapCameraAfterPlayerPlacement(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7FarRelocationCamera", withEntranceTarget: false);
        try
        {
            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.LostTemple, 180,
                "real physics reaches the far temple before the authorized restore");
            fixture.Driver.DriveAxis = 0f;
            await PhysicsFrames(tree, 45);

            TestAssert.True(fixture.World.RestoreCheckpoint(),
                "the initial checkpoint remains available after a real multihop traversal");
            TestAssert.True(fixture.Player.GlobalPosition.DistanceTo(new Vector2(32f, 0f)) < .01f,
                "authorized restore relocates the player before camera assertions");
            AssertCameraSnappedTo(fixture.Camera, 0f, "far restore snaps to entrance limits");
            AssertViewportAndPlayerCovered(fixture, "far restore frame 0");
            for (int frame = 1; frame <= 3; frame++)
            {
                await PhysicsFrames(tree, 1);
                AssertViewportAndPlayerCovered(fixture, $"far restore frame {frame}");
            }

            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.LostTemple, 180,
                "real physics reaches the far temple before the new-run cut");
            fixture.Driver.DriveAxis = 0f;
            await PhysicsFrames(tree, 30);
            fixture.Level.StartNewGame();
            AssertCameraSnappedTo(fixture.Camera, 0f, "far new-run snaps to entrance limits");
            AssertViewportAndPlayerCovered(fixture, "far new-run frame 0");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task DirectMultihopPositionChangesSnapWhileAdjacentCrossingsBlend(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7DirectPositionCamera", withEntranceTarget: false);
        try
        {
            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 2f + 32f, 0f);
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "a direct Position write resolves the basin on the next safe physics tick");
            AssertCameraSnappedTo(fixture.Camera, SectorWidth * 2f,
                "direct Entrance-to-Basin relocation snaps instead of panning through unloaded space");
            AssertViewportAndPlayerCovered(fixture, "direct basin relocation frame 0");
            await PhysicsFrames(tree, 1);
            AssertViewportAndPlayerCovered(fixture, "direct basin relocation frame 1");

            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 4f + 32f, 0f);
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.LostTemple, fixture.World.CurrentSector,
                "a second direct Position write resolves LostTemple");
            AssertCameraSnappedTo(fixture.Camera, SectorWidth * 4f,
                "direct Basin-to-LostTemple relocation snaps immediately");
            AssertViewportAndPlayerCovered(fixture, "direct temple relocation frame 0");
        }
        finally { await DisposeFixture(tree, fixture); }
    }

    private static async Task ReentrantTransitionsCoalesceAndFailedActiveWorkRetainsPending(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7TransitionQueue", withEntranceTarget: false);
        try
        {
            fixture.World.Definitions[2].Scene = PackObservedSector(PrologueSectorId.CanyonBasin);
            fixture.World.Definitions[0].Scene = PackObservedSector(PrologueSectorId.CanyonEntrance);
            int readyRequests = 0;
            SeamlessObservedSector.ReadyObserved = (world, sector) =>
            {
                if (!ReferenceEquals(sector.GetParent(), world) ||
                    !sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta) || readyRequests >= 4) return;
                readyRequests++;
                InvokeTransition(world, sector.SectorId == PrologueSectorId.CanyonEntrance
                    ? PrologueSectorId.CanyonBasin
                    : PrologueSectorId.CanyonEntrance);
            };

            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "a Ready reentry is queued rather than synchronously drained inside the active transition");
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "the coalesced last-valid request executes on a later safe tick");
            await PhysicsFrames(tree, 2);
            TestAssert.Equal(4, readyRequests,
                "alternating Ready A-to-B requests advance one per tick without synchronous recursion");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "the finite A-to-B Ready cycle ends deterministically without a pending transition");

            SeamlessObservedSector.ReadyObserved = (world, sector) =>
            {
                InvokeTransition(world, PrologueSectorId.LostTemple);
                sector.QueueFree();
            };
            fixture.World.Definitions[2].Scene = PackObservedSector(PrologueSectorId.CanyonBasin);
            bool failed = false;
            try { InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin); }
            catch (InvalidOperationException) { failed = true; }
            TestAssert.True(failed, "post-Ready self-deletion fails the active staged transition");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "failed active work leaves the old world committed");
            SeamlessObservedSector.ReadyObserved = null;
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.LostTemple, fixture.World.CurrentSector,
                "failure retains the last-valid pending request for a later safe tick");
        }
        finally
        {
            SeamlessObservedSector.ReadyObserved = null;
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task StagingCompletesReadyAndBindingBeforeTargetLostCommit(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7StagedCommit", withEntranceTarget: true);
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            Node entrance = fixture.World.GetNode("CanyonEntrance");
            bool readySawOldWorld = false;
            bool readyWasDisabled = false;
            SeamlessObservedSector.ReadyObserved = (world, sector) =>
            {
                if (!ReferenceEquals(sector.GetParent(), world) ||
                    !sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return;
                readySawOldWorld = ReferenceEquals(entrance, world.GetNode("CanyonEntrance")) &&
                    target.IsInsideTree() && world.CurrentSector == PrologueSectorId.CanyonEntrance;
                readyWasDisabled = !sector.IsProcessing() && !sector.IsPhysicsProcessing() &&
                    !sector.IsVisibleInTree();
            };
            fixture.World.Definitions[2].Scene = PackObservedSector(PrologueSectorId.CanyonBasin);
            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            TestAssert.True(readySawOldWorld && readyWasDisabled,
                "incoming _Ready completes under hidden disabled staging while the old world stays live");
            int committedHandlers = CheckpointHandlerCount(fixture.World);

            fixture.World.Definitions[4].Scene = PackReadyTransformedSector(PrologueSectorId.LostTemple);
            bool transformFailed = false;
            try { InvokeTransition(fixture.World, PrologueSectorId.LostTemple); }
            catch (InvalidOperationException) { transformFailed = true; }
            TestAssert.True(transformFailed,
                "a root that mutates its transform during _Ready fails post-Ready validation");
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "post-Ready validation failure cannot alter the committed world");

            fixture.World.Definitions[4].Scene = PackPartialBindFailureSector(PrologueSectorId.LostTemple);
            bool bindFailed = false;
            try { InvokeTransition(fixture.World, PrologueSectorId.LostTemple); }
            catch (InvalidOperationException) { bindFailed = true; }
            TestAssert.True(bindFailed, "partial checkpoint bind failure aborts the stage");
            TestAssert.Equal(committedHandlers, CheckpointHandlerCount(fixture.World),
                "partial bind rollback leaves exactly the currently mounted handlers without leaks");
            AssertStableSectorPaths(fixture.World);
        }
        finally
        {
            SeamlessObservedSector.ReadyObserved = null;
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task StagedSectorLifecycleRunsExactlyOnceAndPreservesEncounterActors(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7SingleReadyLifecycle", withEntranceTarget: false);
        try
        {
            SeamlessFinalParentSensitiveSector.Reset();
            fixture.World.Definitions[2].Scene = PackFinalParentSensitiveEncounterSector(
                PrologueSectorId.CanyonBasin);

            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);

            TestAssert.Equal(1, SeamlessFinalParentSensitiveSector.ReadyCount,
                "a staged sector subtree completes _Ready exactly once for its lifetime");
            TestAssert.Equal(0, SeamlessFinalParentSensitiveSector.DirectWorldParentReadyCount,
                "final reparent does not request a second parent-sensitive lifecycle");
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "single-lifecycle commit retains the requested current sector");
            PrologueSector basin = fixture.World.GetNodeOrNull<PrologueSector>("CanyonBasin");
            TestAssert.True(basin != null && basin.Name == new StringName("CanyonBasin") &&
                !basin.IsQueuedForDeletion(),
                "parent-sensitive staged sector keeps its stable final map/path without mutation");
            Node actors = basin.GetNode("Actors");
            TestAssert.Equal(1, actors.GetChildCount(),
                "an auto-activated EncounterController keeps its one staged actor after reparent");
            TestAssert.True(actors.GetChild(0) is SeamlessEncounterActor,
                "the surviving encounter child is the real typed encounter actor fixture");
        }
        finally
        {
            SeamlessFinalParentSensitiveSector.Reset();
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task StagingFailureHasZeroLiveWorldSideEffects(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7StagedFailureIsolation", withEntranceTarget: true);
        int targetLost = 0;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            IReadOnlyList<PrologueSectorId> mountedBefore = fixture.World.MountedSectorIds;
            Node entranceBefore = fixture.World.GetNode("CanyonEntrance");
            int handlersBefore = CheckpointHandlerCount(fixture.World);
            MechanicalClawFrame clawBefore = fixture.Claw.CurrentFrame;
            fixture.Claw.ClawFrameChanged += CountTargetLost;
            fixture.World.Definitions[2].Scene = PackReadyTransformedSector(PrologueSectorId.CanyonBasin);

            bool failed = false;
            try { InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin); }
            catch (InvalidOperationException) { failed = true; }

            fixture.Claw.ClawFrameChanged -= CountTargetLost;
            TestAssert.True(failed, "post-Ready staged validation failure is observable");
            TestAssert.Equal(0, targetLost,
                "a staged failure emits zero irreversible TargetLost notifications");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "a staged failure leaves current sector unchanged");
            AssertMounted(fixture.World, mountedBefore,
                "a staged failure leaves the old mounted map unchanged");
            TestAssert.True(ReferenceEquals(entranceBefore, fixture.World.GetNode("CanyonEntrance")),
                "a staged failure preserves the exact old sector instance and stable path");
            TestAssert.Equal(handlersBefore, CheckpointHandlerCount(fixture.World),
                "a staged failure leaves old checkpoint handlers unchanged");
            TestAssert.Equal(clawBefore.State, fixture.Claw.CurrentFrame.State,
                "a staged failure leaves the live claw latch state unchanged");
            TestAssert.Equal(clawBefore.CastId, fixture.Claw.CurrentFrame.CastId,
                "a staged failure leaves the live claw cast identity unchanged");

            void CountTargetLost(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost) targetLost++;
            }
        }
        finally { await DisposeFixture(tree, fixture); }
    }

    private static async Task FinalNameBeforeReadyEliminatesRenamedMutationAndSynchronousTicks(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7FinalNameActivation", withEntranceTarget: false);
        try
        {
            SeamlessRenameQueueFreeSector.Reset();
            fixture.World.Definitions[2].Scene = PackRenameQueueFreeSector(
                PrologueSectorId.CanyonBasin);

            InvokeTransition(fixture.World, PrologueSectorId.CanyonDescent);

            PrologueSector basin = fixture.World.GetNodeOrNull<PrologueSector>("CanyonBasin");
            TestAssert.Equal(0, SeamlessRenameQueueFreeSector.RenamedCount,
                "the exact final sector name is assigned before Ready can register a Renamed callback");
            TestAssert.True(basin != null && !basin.IsQueuedForDeletion() &&
                basin.Name == new StringName("CanyonBasin"),
                "eliminating the post-Ready rename leaves a stable final sector instance and path");
            TestAssert.Equal(0, SeamlessRenameQueueFreeSector.ProcessTicks,
                "synchronous prepare and commit expose no process tick");
            TestAssert.Equal(0, SeamlessRenameQueueFreeSector.PhysicsTicks,
                "synchronous prepare and commit expose no physics tick");
            TestAssert.Equal(0, SeamlessRenameQueueFreeSector.DrawTicks,
                "synchronous prepare and commit expose no render callback");
            TestAssert.Equal(3, fixture.World.MountedSectorIds.Count,
                "activation preserves the production mounted-set maximum");
            AssertStableSectorPaths(fixture.World);
        }
        finally
        {
            SeamlessRenameQueueFreeSector.Reset();
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task VisibilityActivationFailureRollsBackBeforeTargetLost(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7VisibilityActivation", withEntranceTarget: true);
        int targetLost = 0;
        bool activationSawExactOldWorld = false;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            InvokeTransition(fixture.World, PrologueSectorId.CanyonDescent);
            IReadOnlyList<PrologueSectorId> mountedBefore = fixture.World.MountedSectorIds;
            Node entranceBefore = fixture.World.GetNode("CanyonEntrance");
            int handlersBefore = CheckpointHandlerCount(fixture.World);
            MechanicalClawFrame clawBefore = fixture.Claw.CurrentFrame;
            PackedScene validAscent = fixture.World.Definitions[3].Scene;
            SeamlessVisibilityTransformSector.Reset();
            SeamlessVisibilityTransformSector.ActivationObserved = sector =>
            {
                activationSawExactOldWorld =
                    fixture.World.CurrentSector == PrologueSectorId.CanyonDescent &&
                    ReferenceEquals(entranceBefore,
                        fixture.World.GetNodeOrNull("CanyonEntrance")) &&
                    fixture.World.MountedSectorIds.Count == mountedBefore.Count &&
                    !Contains(fixture.World.MountedSectorIds, sector.SectorId) &&
                    ReferenceEquals(sector.GetParent(), fixture.World) &&
                    sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta) &&
                    target.IsInsideTree() &&
                    fixture.Claw.CurrentFrame.State == clawBefore.State &&
                    fixture.Claw.CurrentFrame.CastId == clawBefore.CastId;
            };
            fixture.Claw.ClawFrameChanged += CountTargetLost;
            fixture.World.Definitions[3].Scene = PackVisibilityTransformSector(
                PrologueSectorId.CanyonAscent);

            bool failed = false;
            try { InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin); }
            catch (InvalidOperationException) { failed = true; }

            fixture.Claw.ClawFrameChanged -= CountTargetLost;
            TestAssert.True(failed,
                "a transform mutation from the activation visibility callback fails revalidation");
            TestAssert.Equal(1, SeamlessVisibilityTransformSector.VisibilityActivationCount,
                "the real VisibilityChanged activation callback executes exactly once");
            TestAssert.True(activationSawExactOldWorld,
                "activation callbacks run while the exact old map, instance, target, and claw remain live");
            TestAssert.Equal(0, targetLost,
                "activation revalidation failure occurs before any irreversible TargetLost frame");
            TestAssert.Equal(PrologueSectorId.CanyonDescent, fixture.World.CurrentSector,
                "activation failure preserves the old current sector");
            AssertMounted(fixture.World, mountedBefore,
                "activation failure preserves the exact old mounted map");
            TestAssert.True(ReferenceEquals(entranceBefore, fixture.World.GetNode("CanyonEntrance")),
                "activation failure preserves the exact old sector instance and path");
            TestAssert.Equal(handlersBefore, CheckpointHandlerCount(fixture.World),
                "activation failure preserves old checkpoint bindings without leaks");
            TestAssert.Equal(clawBefore.State, fixture.Claw.CurrentFrame.State,
                "activation failure preserves the live claw latch state");
            TestAssert.Equal(clawBefore.CastId, fixture.Claw.CurrentFrame.CastId,
                "activation failure preserves the live claw cast identity");
            TestAssert.Equal(0, SeamlessVisibilityTransformSector.ProcessTicks,
                "failed synchronous activation exposes no process tick");
            TestAssert.Equal(0, SeamlessVisibilityTransformSector.PhysicsTicks,
                "failed synchronous activation exposes no physics tick");
            TestAssert.Equal(0, SeamlessVisibilityTransformSector.DrawTicks,
                "failed synchronous activation exposes no render callback");

            SeamlessVisibilityTransformSector.Reset();
            fixture.World.Definitions[3].Scene = validAscent;
            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            TestAssert.Equal(0, StagedSectorChildren(fixture.World),
                "successful commit removes the staged marker synchronously after obsolete detach");
            TestAssert.Equal(fixture.World.MountedSectorIds.Count,
                DirectSectorChildren(fixture.World),
                "successful commit leaves at most three non-staged direct children before any frame");
            TestAssert.True(fixture.World.MountedSectorIds.Count <= 3,
                "successful recovery preserves the mounted-set maximum");

            void CountTargetLost(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost) targetLost++;
            }
        }
        finally
        {
            SeamlessVisibilityTransformSector.Reset();
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task EscapedCheckpointActivationRollsBackByOwnedIdentityAndCanRetry(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7EscapedCheckpointActivation",
            withEntranceTarget: true);
        int targetLost = 0;
        bool activationHadPreparedHandler = false;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            IReadOnlyList<PrologueSectorId> mountedBefore = fixture.World.MountedSectorIds;
            var instancesBefore = new Dictionary<PrologueSectorId, PrologueSector>();
            foreach (PrologueSectorId id in mountedBefore)
                instancesBefore.Add(id, fixture.World.GetNode<PrologueSector>(id.ToString()));
            int handlersBefore = CheckpointHandlerCount(fixture.World);
            MechanicalClawFrame clawBefore = fixture.Claw.CurrentFrame;
            GrappleSurface2D clawTargetBefore = fixture.Claw.CurrentTarget;
            PackedScene cleanBasin = fixture.World.Definitions[2].Scene;
            SeamlessEscapingCheckpointSector.Reset();
            SeamlessEscapingCheckpointSector.ActivationObserved = checkpoint =>
                activationHadPreparedHandler = CheckpointHandlerContains(fixture.World, checkpoint);
            fixture.World.Definitions[2].Scene = PackEscapingCheckpointSector(
                PrologueSectorId.CanyonBasin);
            fixture.Claw.ClawFrameChanged += CountTargetLost;

            bool failed = false;
            try { InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin); }
            catch (InvalidOperationException) { failed = true; }

            fixture.Claw.ClawFrameChanged -= CountTargetLost;
            CheckpointTrigger escaped = SeamlessEscapingCheckpointSector.EscapedCheckpoint;
            bool escapedValid = GodotObject.IsInstanceValid(escaped);
            string escapedParent = escapedValid ? escaped.GetParent()?.Name.ToString() ?? "" : "<invalid>";
            GD.Print($"TASK7_FIX5_ESCAPE_OBSERVED handlers_before={handlersBefore} " +
                $"handlers_after_failure={CheckpointHandlerCount(fixture.World)} " +
                $"activation_had_handler={activationHadPreparedHandler} " +
                $"escaped_valid={escapedValid} escaped_parent={escapedParent}");
            TestAssert.True(failed,
                "activation fails when a required checkpoint escapes the staged root");
            TestAssert.Equal(1, SeamlessEscapingCheckpointSector.ActivationCount,
                "the real activation callback reparents the checkpoint exactly once");
            TestAssert.True(!activationHadPreparedHandler,
                "the staged checkpoint has no live handler during activation callbacks");
            TestAssert.Equal(0, targetLost,
                "escaped-checkpoint failure publishes zero irreversible TargetLost frames");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "escaped-checkpoint failure preserves the exact old current sector");
            AssertMounted(fixture.World, mountedBefore,
                "escaped-checkpoint failure preserves the old mounted map");
            foreach ((PrologueSectorId id, PrologueSector instance) in instancesBefore)
                TestAssert.True(ReferenceEquals(instance,
                        fixture.World.GetNodeOrNull<PrologueSector>(id.ToString())),
                    $"escaped-checkpoint failure preserves exact old instance/path {id}");
            TestAssert.Equal(handlersBefore, CheckpointHandlerCount(fixture.World),
                "escaped-checkpoint rollback leaves the exact pre-transition handler count");
            TestAssert.Equal(clawBefore.State, fixture.Claw.CurrentFrame.State,
                "escaped-checkpoint rollback preserves the live claw state");
            TestAssert.Equal(clawBefore.CastId, fixture.Claw.CurrentFrame.CastId,
                "escaped-checkpoint rollback preserves the live claw cast identity");
            TestAssert.True(ReferenceEquals(clawTargetBefore, fixture.Claw.CurrentTarget),
                "escaped-checkpoint rollback preserves the exact live claw target");
            TestAssert.True(escaped != null && !GodotObject.IsInstanceValid(escaped),
                "the escaped originally-owned checkpoint is disposed without an orphan");
            TestAssert.True(fixture.World.GetNodeOrNull<CheckpointTrigger>("Checkpoint") == null,
                "rollback leaves no escaped checkpoint under the world root");
            TestAssert.True(instancesBefore[PrologueSectorId.CanyonEntrance]
                    .GetNodeOrNull<CheckpointTrigger>("Checkpoint") == null,
                "rollback leaves no escaped checkpoint attached to the old sector");
            TestAssert.True(!CheckpointHandlerContains(fixture.World, escaped),
                "the disposed escaped checkpoint retains no stale activation handler");

            fixture.World.Definitions[2].Scene = cleanBasin;
            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            CheckpointTrigger basinCheckpoint = fixture.World.GetNode<CheckpointTrigger>(
                "CanyonBasin/Gameplay/Checkpoint");
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "a clean retry to the same requested sector succeeds");
            TestAssert.True(CheckpointHandlerContains(fixture.World, basinCheckpoint),
                "the clean retry binds the one valid basin checkpoint");
            TestAssert.Equal(1, CheckpointHandlerCountForSector(fixture.World,
                    fixture.World.GetNode<PrologueSector>("CanyonBasin")),
                "the clean retry binds exactly one valid handler for the requested sector");
            TestAssert.Equal(fixture.World.MountedSectorIds.Count,
                CheckpointHandlerCount(fixture.World),
                "the clean retry has exactly one valid handler per mounted fixture sector");
            TestAssert.True(!GodotObject.IsInstanceValid(escaped) &&
                    !CheckpointHandlerContains(fixture.World, escaped),
                "the stale escaped checkpoint cannot activate after the clean retry");
            AssertStableSectorPaths(fixture.World);

            GD.Print($"TASK7_FIX5_ESCAPED_CHECKPOINT_OK handlers_before={handlersBefore} " +
                $"handlers_after={CheckpointHandlerCount(fixture.World)} target_lost_failure={targetLost}");

            void CountTargetLost(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost) targetLost++;
            }
        }
        finally
        {
            CheckpointTrigger escaped = SeamlessEscapingCheckpointSector.EscapedCheckpoint;
            if (GodotObject.IsInstanceValid(escaped)) escaped.Free();
            SeamlessEscapingCheckpointSector.Reset();
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task MechanicalClawPublicationIsNonReentrantAndResetIsIdempotent(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7ClawPublishQueue", withEntranceTarget: true);
        var later = new List<MechanicalClawState>();
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            void NestedReset(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed) fixture.Claw.ResetTransientState();
            }
            void Later(MechanicalClawFrame frame, GrappleSurface2D observed) => later.Add(frame.State);
            fixture.Claw.ClawFrameChanged += NestedReset;
            fixture.Claw.ClawFrameChanged += Later;
            fixture.Claw.NotifyTargetWillUnload(target);
            fixture.Claw.ResetTransientState();
            fixture.Claw.ResetTransientState();
            fixture.Claw.ClawFrameChanged -= NestedReset;
            fixture.Claw.ClawFrameChanged -= Later;

            TestAssert.Equal(2, later.Count,
                "nested and repeated resets publish exactly one terminal Failed-to-Idle sequence");
            TestAssert.Equal(MechanicalClawState.Failed, later[0],
                "every observer finishes the Failed frame before Idle begins");
            TestAssert.Equal(MechanicalClawState.Idle, later[1],
                "Idle follows Failed exactly once after the full observer round");
        }
        finally { await DisposeFixture(tree, fixture); }
    }

    private static async Task ThrowingObserverReentryAndSameTickRemountStayTransactional(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7ReentrantRetirement", withEntranceTarget: true);
        int targetLost = 0;
        int laterObserver = 0;
        int peakChildren = 0;
        bool dictionaryMatchedTree = true;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);

            fixture.World.Definitions[2].Scene = PackObservedSector(PrologueSectorId.CanyonBasin);
            fixture.World.Definitions[3].Scene = PackObservedSector(PrologueSectorId.CanyonAscent);
            SeamlessObservedSector.ReadyObserved = (world, sector) =>
            {
                int direct = DirectSectorChildren(world);
                peakChildren = Math.Max(peakChildren, direct);
                dictionaryMatchedTree &= direct == world.MountedSectorIds.Count;
            };

            void Throwing(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost)
                    throw new InvalidOperationException("intentional Task7 observer fault");
            }
            void Reenter(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State != MechanicalClawState.Failed ||
                    frame.Failure != MechanicalClawFailure.TargetLost) return;
                targetLost++;
                fixture.Level.StartNewGame();
            }
            void Later(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost) laterObserver++;
            }
            fixture.Claw.ClawFrameChanged += Throwing;
            fixture.Claw.ClawFrameChanged += Reenter;
            fixture.Claw.ClawFrameChanged += Later;

            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "the active transition commits before queued reentrant work drains");
            await PhysicsFrames(tree, 1);
            fixture.Claw.ClawFrameChanged -= Throwing;
            fixture.Claw.ClawFrameChanged -= Reenter;
            fixture.Claw.ClawFrameChanged -= Later;
            SeamlessObservedSector.ReadyObserved = null;

            TestAssert.Equal(1, targetLost,
                "reentrant new-run observes exactly one Failed/TargetLost notification");
            TestAssert.Equal(1, laterObserver,
                "a throwing observer cannot block later claw observers or Idle cleanup");
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "internal Failed-to-Idle cleanup completes despite observer failure");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "the queued reentrant new-run wins on the next safe physics tick");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent],
                "reentrant A-to-B-to-A ends on the exact entrance required set");
            TestAssert.True(peakChildren <= 3 && dictionaryMatchedTree,
                $"callbacks observe a truthful mounted budget; peak={peakChildren} " +
                $"dictionaryMatched={dictionaryMatchedTree}");
            AssertStableSectorPaths(fixture.World);
        }
        finally
        {
            SeamlessObservedSector.ReadyObserved = null;
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task MountFailureRollsBackAndLaterCrossingRecovers(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7MountRollback", withEntranceTarget: false);
        try
        {
            IReadOnlyList<PrologueSectorId> before = fixture.World.MountedSectorIds;
            Node entrance = fixture.World.GetNode("CanyonEntrance");
            PackedScene valid = fixture.World.Definitions[2].Scene;
            int handlersBefore = CheckpointHandlerCount(fixture.World);
            fixture.World.Definitions[2].Scene = PackTransformedSector(PrologueSectorId.CanyonBasin);
            bool malformedFailedClosed = false;
            try
            {
                InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            }
            catch (InvalidOperationException)
            {
                malformedFailedClosed = true;
            }
            TestAssert.True(malformedFailedClosed,
                "a malformed rotated root fails before any live mount is detached");
            AssertMounted(fixture.World, before,
                "malformed-root failure leaves the prior mounted set unchanged");
            TestAssert.Equal(handlersBefore, CheckpointHandlerCount(fixture.World),
                "malformed-root failure leaves checkpoint handlers unchanged");

            fixture.World.Definitions[2].Scene = PackSelfRetiringSector(PrologueSectorId.CanyonBasin);

            bool failedClosed = false;
            try
            {
                InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            }
            catch (InvalidOperationException)
            {
                failedClosed = true;
            }
            TestAssert.True(failedClosed,
                "a sector that invalidates itself during ready fails the mount transaction");
            TestAssert.Equal(PrologueSectorId.CanyonEntrance, fixture.World.CurrentSector,
                "mount failure leaves current sector unchanged");
            AssertMounted(fixture.World, before,
                "mount failure restores the prior mounted dictionary exactly");
            TestAssert.True(ReferenceEquals(entrance, fixture.World.GetNode("CanyonEntrance")),
                "mount failure preserves the prior stable tree instance and path");
            TestAssert.Equal(handlersBefore, CheckpointHandlerCount(fixture.World),
                "ready failure rolls checkpoint handlers back to the exact prior set");
            AssertStableSectorPaths(fixture.World);

            fixture.World.Definitions[2].Scene = valid;
            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "a normal crossing succeeds after the failed mount rolls back");
            AssertStableSectorPaths(fixture.World);
            await ProcessFrames(tree, 4);
            TestAssert.Equal(0, fixture.World.RetiredSectorCount,
                "rollback and later crossing drain retired nodes without stale handlers");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task SeamlessNewRunClearsPlayerTransientState(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7ResetTransient", withEntranceTarget: true);
        try
        {
            fixture.Driver.SetPhysicsProcess(false);
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            fixture.Player.GlobalPosition -= new Vector2(48f, 0f);
            SetInputFrame(fixture.Input, Frame(primaryHeld: true, mouseWorld: target.WorldAnchor));
            await PhysicsFrames(tree, 1);
            StartCombatWhileClawFrameRemainsLive(fixture);
            GD.Print($"TASK7_RESET_SETUP state={fixture.Claw.CurrentFrame.State} " +
                $"correction={fixture.Claw.CurrentFrame.VelocityCorrection} combat={fixture.Combat.IsAttacking}");
            TestAssert.True((fixture.Claw.CurrentFrame.State is MechanicalClawState.Latched or
                MechanicalClawState.Reel or MechanicalClawState.Swing) &&
                !fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() && fixture.Combat.IsAttacking,
                "new-run setup has a live latch, active combat and nonzero grapple correction");
            fixture.Motor.ApplyGrappleConstraint(new Vector2(240f, 0f), .25f);
            fixture.Motor.ExternalImpulse(new Vector2(480f, 0f));
            fixture.Player.Velocity = new Vector2(480f, 0f);
            fixture.Level.StartNewGame();

            TestAssert.True(fixture.Player.Velocity.IsZeroApprox(),
                "seamless StartNewGame clears player velocity immediately");
            TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
                "seamless StartNewGame clears the motor grapple constraint");
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "seamless StartNewGame clears claw transient state");
            TestAssert.True(!fixture.Combat.IsAttacking,
                "seamless StartNewGame clears active combat state");
            Vector2 spawn = fixture.Player.GlobalPosition;
            fixture.Motor.SetPhysicsProcess(true);
            await PhysicsFrames(tree, 1);
            TestAssert.Near(spawn.X, fixture.Player.GlobalPosition.X, .01f,
                "the first physics tick after the spawn has no retained horizontal drift");
            fixture.Motor.SetPhysicsProcess(false);

            await DisposeFixture(tree, fixture);
            fixture = await CreateFixture(tree, "Task7RestoreTransient", withEntranceTarget: true);
            fixture.Driver.SetPhysicsProcess(false);
            await SelectGrappleMode(tree, fixture);
            target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            fixture.Player.GlobalPosition -= new Vector2(48f, 0f);
            SetInputFrame(fixture.Input, Frame(primaryHeld: true, mouseWorld: target.WorldAnchor));
            await PhysicsFrames(tree, 1);
            StartCombatWhileClawFrameRemainsLive(fixture);
            GD.Print($"TASK7_RESTORE_SETUP state={fixture.Claw.CurrentFrame.State} " +
                $"correction={fixture.Claw.CurrentFrame.VelocityCorrection} combat={fixture.Combat.IsAttacking}");
            TestAssert.True((fixture.Claw.CurrentFrame.State is MechanicalClawState.Latched or
                MechanicalClawState.Reel or MechanicalClawState.Swing) &&
                !fixture.Claw.CurrentFrame.VelocityCorrection.IsZeroApprox() && fixture.Combat.IsAttacking,
                "restore setup has a live latch, active combat and nonzero grapple correction");
            fixture.Motor.ApplyGrappleConstraint(new Vector2(180f, 0f), .4f);
            fixture.Motor.ExternalImpulse(new Vector2(360f, 0f));
            fixture.Player.Velocity = new Vector2(360f, 0f);
            fixture.Level.RestoreFromSave(new PrologueSaveData());
            TestAssert.True(fixture.Player.Velocity.IsZeroApprox(),
                "seamless RestoreFromSave clears player velocity immediately");
            TestAssert.Near(1f, fixture.Motor.GrappleMovementScale, .0001f,
                "seamless RestoreFromSave clears the motor grapple constraint");
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "seamless RestoreFromSave clears claw transient state");
            TestAssert.True(!fixture.Combat.IsAttacking,
                "seamless RestoreFromSave clears active combat state");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task LatchedTargetUnloadsBeforeRetirementAndTenRoundTripsStayBounded(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task7LifecycleStress", withEntranceTarget: true);
        int targetLostCount = 0;
        bool targetLostBeforeUnmount = false;
        bool mountedBudgetTruthfulAtRetire = false;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            TestAssert.Equal(MechanicalClawState.Latched, fixture.Claw.CurrentFrame.State,
                "the stress fixture begins with a real physics-resolved claw latch");

            void Record(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State != MechanicalClawState.Failed ||
                    frame.Failure != MechanicalClawFailure.TargetLost) return;
                targetLostCount++;
                targetLostBeforeUnmount = target.IsInsideTree() &&
                    Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonEntrance);
                mountedBudgetTruthfulAtRetire = DirectSectorChildren(fixture.World) ==
                    fixture.World.MountedSectorIds.Count &&
                    fixture.World.MountedSectorIds.Count <= 3 &&
                    StagedSectorChildren(fixture.World) <= 1;
            }
            fixture.Claw.ClawFrameChanged += Record;

            int roundTrips = 0;
            int templeArrivals = 0;
            int maxMounted = fixture.World.MountedSectorIds.Count;
            int maxRetired = fixture.World.RetiredSectorCount;
            bool headedRight = true;
            fixture.Driver.DriveAxis = 1f;
            SetInputFrame(fixture.Input, Frame(primaryHeld: true,
                mouseWorld: target.WorldAnchor));
            for (int frame = 0; frame < 2400 && roundTrips < 10; frame++)
            {
                await PhysicsFrames(tree, 1);
                maxMounted = Math.Max(maxMounted, fixture.World.MountedSectorIds.Count);
                maxRetired = Math.Max(maxRetired, fixture.World.RetiredSectorCount);
                TestAssert.True(fixture.World.MountedSectorIds.Count <= 3,
                    "stream lifecycle never mounts more than previous/current/next");
                TestAssert.Equal(fixture.World.MountedSectorIds.Count,
                    DirectSectorChildren(fixture.World),
                    "after synchronous commit the non-staged direct tree matches the mounted map");
                TestAssert.Equal(0, StagedSectorChildren(fixture.World),
                    "after synchronous commit no staged direct child reaches a frame");
                TestAssert.True(fixture.World.RetiredSectorCount <= 5,
                    "retirement remains bounded by the literal sector graph");

                if (headedRight && fixture.World.CurrentSector == PrologueSectorId.LostTemple &&
                    fixture.Player.GlobalPosition.X >= SectorWidth * 5f - 40f)
                {
                    templeArrivals++;
                    headedRight = false;
                    fixture.Driver.DriveAxis = -1f;
                }
                else if (!headedRight &&
                    fixture.World.CurrentSector == PrologueSectorId.CanyonEntrance &&
                    fixture.Player.GlobalPosition.X <= 40f)
                {
                    roundTrips++;
                    headedRight = true;
                    fixture.Driver.DriveAxis = 1f;
                }
            }
            fixture.Driver.DriveAxis = 0f;
            fixture.Claw.ClawFrameChanged -= Record;
            await ProcessFrames(tree, 2);

            TestAssert.Equal(10, roundTrips,
                "a real automatically moved player completes ten entrance-to-temple round trips");
            TestAssert.Equal(10, templeArrivals,
                "each stress round trip physically reaches LostTemple before returning");
            TestAssert.Equal(1, targetLostCount,
                "retiring the latched entrance publishes Failed/TargetLost exactly once");
            TestAssert.True(targetLostBeforeUnmount,
                "TargetLost is observable while the target sector is still mounted and alive");
            TestAssert.True(mountedBudgetTruthfulAtRetire,
                "retirement callbacks observe max-three mounted/non-staged children and at most one staged direct child");
            TestAssert.True(fixture.Claw.CurrentTarget == null &&
                fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle,
                "sector retirement leaves no stale claw target or live constraint");
            TestAssert.Equal(0, fixture.Line.Points.Length,
                "sector retirement clears the remaining world rope presentation");
            TestAssert.True(maxMounted <= 3 && maxRetired <= 5 &&
                fixture.World.RetiredSectorCount == 0,
                "ten return trips keep mounted and retired native graphs bounded and drained");
            TestAssert.True(maxRetired > 0,
                "detached sectors remain observable as off-tree queued retirement for at least one frame");
            GD.Print($"TASK7_SEAMLESS_STRESS_OK round_trips={roundTrips} " +
                $"temple_arrivals={templeArrivals} max_mounted={maxMounted} " +
                $"max_retired={maxRetired} target_lost={targetLostCount}");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task<Fixture> CreateFixture(SceneTree tree, string name,
        bool withEntranceTarget)
    {
        var level = new PrologueLevelController
        {
            Name = name,
            PlayerRootPath = new NodePath("PlayerRoot"),
            RoomRootPath = new NodePath("MissingLegacyRoomRoot"),
            HudPath = new NodePath("MissingHud"),
            SeamlessWorldPath = new NodePath("WorldRoot/Sectors")
        };
        var playerRoot = new Node2D { Name = "PlayerRoot" };
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.GlobalPosition = new Vector2(32f, 0f);
        player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        var driver = new SeamlessFixturePlayerDriver
        {
            Name = "SeamlessFixtureDriver",
            ProcessPhysicsPriority = 50
        };
        player.AddChild(driver);
        playerRoot.AddChild(player);
        level.AddChild(playerRoot);

        var worldRoot = new Node2D { Name = "WorldRoot" };
        var world = new SeamlessPrologueWorld
        {
            Name = "Sectors",
            PlayerPath = new NodePath("../../PlayerRoot/Player"),
            InitialSector = PrologueSectorId.CanyonEntrance,
            ProcessPhysicsPriority = 100
        };
        foreach (PrologueSectorDefinition definition in Definitions(withEntranceTarget))
            world.Definitions.Add(definition);
        worldRoot.AddChild(world);
        level.AddChild(worldRoot);
        tree.Root.AddChild(level);
        await PhysicsFrames(tree, 2);

        // Godot enables overridden callbacks when the packed player enters the tree, so the
        // fixture disables unrelated automatic owners after _Ready. Action mode, energy,
        // claw, the movement driver and seamless world remain genuinely scheduled.
        player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        player.GlobalPosition = new Vector2(32f, 0f);
        player.Velocity = Vector2.Zero;

        return new Fixture(
            level,
            worldRoot,
            world,
            player,
            driver,
            player.GetNode<Camera2D>("Camera2D"),
            player.GetNode<PlayerInputReader>("Input"),
            player.GetNode<PlayerActionModeController>("ActionModes"),
            player.GetNode<PlayerEnergyController>("Energy"),
            player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<PlayerCombat>("Combat"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"),
            player.GetNode<ThreadLineVisual>("VisualRoot/MechanicalClaw/Line"));
    }

    private static List<PrologueSectorDefinition> Definitions(bool withEntranceTarget)
    {
        PackedScene[] scenes = new PackedScene[5];
        PrologueSectorId[] ids = Enum.GetValues<PrologueSectorId>();
        for (int index = 0; index < ids.Length; index++)
            scenes[index] = PackSector(ids[index], withEntranceTarget && index == 0);

        return
        [
            Definition(ids[0], 0, null, ids[1], scenes[0]),
            Definition(ids[1], 1, ids[0], ids[2], scenes[1]),
            Definition(ids[2], 2, ids[1], ids[3], scenes[2]),
            Definition(ids[3], 3, ids[2], ids[4], scenes[3]),
            Definition(ids[4], 4, ids[3], null, scenes[4])
        ];
    }

    private static PrologueSectorDefinition Definition(PrologueSectorId id, int index,
        PrologueSectorId? previous, PrologueSectorId? next, PackedScene scene) => new(
        id,
        scene,
        Vector2.Right * SectorWidth * index,
        new Rect2(0f, SectorTop, SectorWidth, SectorHeight),
        previous,
        next);

    private static PackedScene PackSector(PrologueSectorId id, bool withEntranceTarget)
    {
        var sector = new PrologueSector
        {
            Name = id.ToString(),
            SectorId = id,
            Bounds = new Rect2(0f, SectorTop, SectorWidth, SectorHeight)
        };
        var gameplay = new Node2D { Name = "Gameplay" };
        sector.AddChild(gameplay);
        gameplay.AddChild(new CheckpointTrigger
        {
            Name = "Checkpoint",
            Position = new Vector2(24f, 0f),
            EntranceId = "fixture"
        });
        if (withEntranceTarget)
        {
            var target = new GrappleSurface2D
            {
                Name = "EntranceAnchor",
                Position = new Vector2(64f, 0f),
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
            target.AddChild(area);
            gameplay.AddChild(target);
        }
        Own(sector, sector);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(sector), $"{id} runtime sector fixture packs");
        sector.Free();
        return packed;
    }

    private static PackedScene PackObservedSector(PrologueSectorId id) => PackCustomSector(
        new SeamlessObservedSector(), id);

    private static PackedScene PackFinalParentSensitiveEncounterSector(PrologueSectorId id)
    {
        var sector = new SeamlessFinalParentSensitiveSector
        {
            Name = id.ToString(),
            SectorId = id,
            Bounds = new Rect2(0f, SectorTop, SectorWidth, SectorHeight)
        };
        var markers = new Node2D { Name = "Markers" };
        markers.AddChild(new Marker2D { Name = "Actor", Position = new Vector2(32f, 0f) });
        var actors = new Node2D { Name = "Actors" };
        var actorRoot = new SeamlessEncounterActor { Name = "Actor" };
        var actorScene = new PackedScene();
        TestAssert.Equal(Error.Ok, actorScene.Pack(actorRoot), "minimal encounter actor packs");
        actorRoot.Free();
        var controller = new EncounterController
        {
            Name = "Encounter",
            AutoActivate = true,
            ActorRootPath = new NodePath("../Actors"),
            Definition = new EncounterDefinition
            {
                EncounterId = "single_ready_actor",
                Spawns = new Godot.Collections.Array<EncounterSpawn>
                {
                    new()
                    {
                        SpawnId = "actor",
                        Scene = actorScene,
                        MarkerPath = new NodePath("../Markers/Actor")
                    }
                }
            }
        };
        sector.AddChild(markers);
        sector.AddChild(actors);
        sector.AddChild(controller);
        Own(sector, sector);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(sector), $"{id} encounter lifecycle fixture packs");
        sector.Free();
        return packed;
    }

    private static PackedScene PackReadyTransformedSector(PrologueSectorId id) => PackCustomSector(
        new SeamlessReadyTransformedSector(), id);

    private static PackedScene PackRenameQueueFreeSector(PrologueSectorId id) => PackCustomSector(
        new SeamlessRenameQueueFreeSector(), id);

    private static PackedScene PackVisibilityTransformSector(PrologueSectorId id) => PackCustomSector(
        new SeamlessVisibilityTransformSector(), id);

    private static PackedScene PackEscapingCheckpointSector(PrologueSectorId id)
    {
        var sector = new SeamlessEscapingCheckpointSector
        {
            Name = id.ToString(),
            SectorId = id,
            Bounds = new Rect2(0f, SectorTop, SectorWidth, SectorHeight),
            CheckpointPath = new NodePath("Gameplay/Checkpoint")
        };
        var gameplay = new Node2D { Name = "Gameplay" };
        gameplay.AddChild(new CheckpointTrigger
        {
            Name = "Checkpoint",
            Position = new Vector2(24f, 0f),
            EntranceId = "escaped_fixture"
        });
        sector.AddChild(gameplay);
        Own(sector, sector);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(sector), $"{id} escaping checkpoint fixture packs");
        sector.Free();
        return packed;
    }

    private static PackedScene PackPartialBindFailureSector(PrologueSectorId id)
    {
        var sector = new SeamlessPartialBindFailureSector
        {
            Name = id.ToString(),
            SectorId = id,
            Bounds = new Rect2(0f, SectorTop, SectorWidth, SectorHeight)
        };
        sector.AddChild(new CheckpointTrigger { Name = "First" });
        sector.AddChild(new CheckpointTrigger { Name = "Second" });
        sector.RetiringCheckpointPath = new NodePath("Second");
        Own(sector, sector);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(sector), $"{id} partial-bind fixture packs");
        sector.Free();
        return packed;
    }

    private static PackedScene PackSelfRetiringSector(PrologueSectorId id) => PackCustomSector(
        new SeamlessSelfRetiringSector(), id);

    private static PackedScene PackTransformedSector(PrologueSectorId id) => PackCustomSector(
        new PrologueSector { Rotation = .2f }, id);

    private static PackedScene PackCustomSector(PrologueSector sector, PrologueSectorId id)
    {
        sector.Name = id.ToString();
        sector.SectorId = id;
        sector.Bounds = new Rect2(0f, SectorTop, SectorWidth, SectorHeight);
        var packed = new PackedScene();
        TestAssert.Equal(Error.Ok, packed.Pack(sector), $"{id} custom runtime sector fixture packs");
        sector.Free();
        return packed;
    }

    private static async Task PrepareNonDefaultPlayerState(SceneTree tree, Fixture fixture)
    {
        TestAssert.True(fixture.Energy.TrySpendHeavyGuard(37f),
            "fixture spends real energy before crossing");
        fixture.Energy.SetShieldRequested(true);
        await PhysicsFrames(tree, 8);
        fixture.Energy.SetShieldRequested(false);
        await PhysicsFrames(tree, 1);
        fixture.Energy.SetPhysicsProcess(false);

        SetInputFrame(fixture.Input, Frame(grappleModePressed: true, requestedSlot: 4));
        await PhysicsFrames(tree, 1);
        SetInputFrame(fixture.Input, Frame());
        await PhysicsFrames(tree, 1);
        TestAssert.True(fixture.Energy.EnergyRatio < 1f && fixture.Energy.HeatRatio > 0f,
            $"fixture carries non-default energy and heat into the seam; " +
            $"energy={fixture.Energy.EnergyRatio:0.0000} heat={fixture.Energy.HeatRatio:0.0000}");
        TestAssert.Equal(PlayerActionMode.Grapple, fixture.Modes.CurrentMode,
            "fixture carries non-default Grapple mode into the seam");
        TestAssert.Equal(4, fixture.Modes.ActiveSlot,
            "fixture carries non-default slot four into the seam");
    }

    private static async Task SelectGrappleMode(SceneTree tree, Fixture fixture)
    {
        SetInputFrame(fixture.Input, Frame(grappleModePressed: true));
        fixture.Modes._PhysicsProcess(0d);
        await PhysicsFrames(tree, 1);
        SetInputFrame(fixture.Input, Frame());
        fixture.Modes._PhysicsProcess(0d);
        await PhysicsFrames(tree, 1);
        TestAssert.Equal(PlayerActionMode.Grapple, fixture.Modes.CurrentMode,
            "real mode controller selects Grapple before the latch");
    }

    private static void StartCombatWhileClawFrameRemainsLive(Fixture fixture)
    {
        SetInputFrame(fixture.Input, Frame() with { AttackModePressed = true, RequestedSlot = 1 });
        fixture.Modes._PhysicsProcess(0d);
        SetInputFrame(fixture.Input, Frame() with { PrimaryPressed = true, PrimaryHeld = true });
        fixture.Combat._PhysicsProcess(.01d);
    }

    private static async Task LatchRealClaw(SceneTree tree, Fixture fixture,
        GrappleSurface2D target)
    {
        SetInputFrame(fixture.Input, Frame(primaryPressed: true, primaryHeld: true,
            mouseWorld: target.WorldAnchor));
        await PhysicsFrames(tree, 1);
        for (int frame = 0; frame < 120 &&
            fixture.Claw.CurrentFrame.State != MechanicalClawState.Latched; frame++)
        {
            SetInputFrame(fixture.Input, Frame(primaryHeld: true,
                mouseWorld: target.WorldAnchor));
            await PhysicsFrames(tree, 1);
        }
    }

    private static async Task WaitForSector(SceneTree tree, Fixture fixture,
        PrologueSectorId expected, int frameBudget, string message)
    {
        for (int frame = 0; frame < frameBudget && fixture.World.CurrentSector != expected; frame++)
            await PhysicsFrames(tree, 1);
        TestAssert.Equal(expected, fixture.World.CurrentSector, message);
    }

    private static PlayerInputFrame Frame(bool grappleModePressed = false, int requestedSlot = 0,
        bool primaryPressed = false, bool primaryHeld = false, Vector2 mouseWorld = default) => new(
        0f,
        Vector2.Zero,
        false,
        false,
        false,
        false,
        false,
        MouseWorld: mouseWorld,
        GrappleModePressed: grappleModePressed,
        RequestedSlot: requestedSlot,
        PrimaryPressed: primaryPressed,
        PrimaryHeld: primaryHeld);

    private static void SetInputFrame(PlayerInputReader input, PlayerInputFrame frame) =>
        typeof(PlayerInputReader).GetProperty(nameof(PlayerInputReader.CurrentFrame),
            BindingFlags.Instance | BindingFlags.Public)!.SetValue(input, frame);

    private static void AssertMounted(SeamlessPrologueWorld world,
        IReadOnlyList<PrologueSectorId> expected, string message)
    {
        TestAssert.Equal(expected.Count, world.MountedSectorIds.Count, message + " count");
        for (int index = 0; index < expected.Count; index++)
            TestAssert.Equal(expected[index], world.MountedSectorIds[index], message + $" item {index}");
    }

    private static void InvokeTransition(SeamlessPrologueWorld world, PrologueSectorId next)
    {
        try
        {
            typeof(SeamlessPrologueWorld).GetMethod("SetCurrentSector",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(world, [next]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    private static PrologueSectorId InvokeResolve(SeamlessPrologueWorld world, Vector2 point) =>
        (PrologueSectorId)typeof(SeamlessPrologueWorld).GetMethod("ResolveSector",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(world, [point])!;

    private static int CheckpointHandlerCount(SeamlessPrologueWorld world) =>
        ((System.Collections.IDictionary)typeof(SeamlessPrologueWorld)
            .GetField("_checkpointHandlers", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(world)!).Count;

    private static bool CheckpointHandlerContains(SeamlessPrologueWorld world,
        CheckpointTrigger checkpoint) => checkpoint != null &&
        ((System.Collections.IDictionary)typeof(SeamlessPrologueWorld)
            .GetField("_checkpointHandlers", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(world)!).Contains(checkpoint);

    private static int CheckpointHandlerCountForSector(SeamlessPrologueWorld world,
        PrologueSector sector)
    {
        int count = 0;
        foreach (Node node in sector.FindChildren("*", "", true, false))
            if (node is CheckpointTrigger checkpoint &&
                CheckpointHandlerContains(world, checkpoint)) count++;
        return count;
    }

    private static int DirectSectorChildren(SeamlessPrologueWorld world)
    {
        int count = 0;
        foreach (Node child in world.GetChildren())
            if (child is PrologueSector sector &&
                !sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) count++;
        return count;
    }

    private static int StagedSectorChildren(SeamlessPrologueWorld world)
    {
        int count = 0;
        foreach (Node child in world.GetChildren())
            if (child is PrologueSector sector &&
                sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) count++;
        return count;
    }

    private static void AssertStableSectorPaths(SeamlessPrologueWorld world)
    {
        TestAssert.Equal(world.MountedSectorIds.Count, DirectSectorChildren(world),
            "mounted dictionary matches direct sector children");
        foreach (PrologueSectorId id in world.MountedSectorIds)
        {
            Node node = world.GetNodeOrNull(id.ToString());
            TestAssert.True(node is PrologueSector && node.Name == id.ToString(),
                $"mounted sector has exact stable path WorldRoot/Sectors/{id}");
        }
    }

    private static void AssertCameraSnappedTo(Camera2D camera, float left, string message)
    {
        TestAssert.Equal(Mathf.RoundToInt(left), camera.LimitLeft, message + " left");
        TestAssert.Equal(Mathf.RoundToInt(left + SectorWidth), camera.LimitRight,
            message + " right");
        TestAssert.Equal(Mathf.RoundToInt(SectorTop), camera.LimitTop, message + " top");
        TestAssert.Equal(Mathf.RoundToInt(SectorTop + SectorHeight), camera.LimitBottom,
            message + " bottom");
    }

    private static void AssertViewportAndPlayerCovered(Fixture fixture, string context)
    {
        Rect2 cameraLimits = new(fixture.Camera.LimitLeft, fixture.Camera.LimitTop,
            fixture.Camera.LimitRight - fixture.Camera.LimitLeft,
            fixture.Camera.LimitBottom - fixture.Camera.LimitTop);
        TestAssert.True(PointCoveredByMountedWorld(fixture, fixture.Player.GlobalPosition),
            $"{context}: relocated player remains inside a mounted world bound");
        Vector2[] samples =
        [
            cameraLimits.Position,
            new Vector2(cameraLimits.End.X, cameraLimits.Position.Y),
            cameraLimits.GetCenter(),
            new Vector2(cameraLimits.Position.X, cameraLimits.End.Y),
            cameraLimits.End
        ];
        foreach (Vector2 sample in samples)
            TestAssert.True(PointCoveredByMountedWorld(fixture, sample),
                $"{context}: camera-limit sample {sample} is covered by the mounted world");
    }

    private static bool PointCoveredByMountedWorld(Fixture fixture, Vector2 point)
    {
        foreach (PrologueSectorId id in fixture.World.MountedSectorIds)
        {
            PrologueSectorDefinition definition = fixture.World.Definitions[(int)id];
            Rect2 bounds = definition.WorldBounds;
            if (point.X >= bounds.Position.X - .01f && point.X <= bounds.End.X + .01f &&
                point.Y >= bounds.Position.Y - .01f && point.Y <= bounds.End.Y + .01f) return true;
        }
        return false;
    }

    private static bool Contains(IReadOnlyList<PrologueSectorId> values, PrologueSectorId expected)
    {
        foreach (PrologueSectorId value in values)
            if (value == expected) return true;
        return false;
    }

    private static void Own(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            Own(child, owner);
        }
    }

    private static async Task DisposeFixture(SceneTree tree, Fixture fixture)
    {
        if (GodotObject.IsInstanceValid(fixture.Level)) fixture.Level.QueueFree();
        await ProcessFrames(tree, 2);
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static async Task ProcessFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private sealed record Fixture(
        PrologueLevelController Level,
        Node2D WorldRoot,
        SeamlessPrologueWorld World,
        CharacterBody2D Player,
        SeamlessFixturePlayerDriver Driver,
        Camera2D Camera,
        PlayerInputReader Input,
        PlayerActionModeController Modes,
        PlayerEnergyController Energy,
        PlayerMotor Motor,
        PlayerCombat Combat,
        MechanicalClawController Claw,
        ThreadLineVisual Line);
}

internal partial class SeamlessObservedSector : PrologueSector
{
    public static Action<SeamlessPrologueWorld, PrologueSector> ReadyObserved { get; set; }

    public override void _Ready()
    {
        Node parent = GetParent();
        SeamlessPrologueWorld world = parent as SeamlessPrologueWorld ??
            parent?.GetParent() as SeamlessPrologueWorld;
        if (world != null) ReadyObserved?.Invoke(world, this);
    }
}

internal partial class SeamlessReadyTransformedSector : PrologueSector
{
    public override void _Ready() => Position += new Vector2(7f, 0f);
}

internal partial class SeamlessPartialBindFailureSector : PrologueSector
{
    [Export] public NodePath RetiringCheckpointPath { get; set; }

    public override void _Ready() => GetNode<CheckpointTrigger>(RetiringCheckpointPath).QueueFree();
}

internal partial class SeamlessSelfRetiringSector : PrologueSector
{
    public override void _Ready() => QueueFree();
}

internal partial class SeamlessFinalParentSensitiveSector : PrologueSector
{
    public static int ReadyCount { get; private set; }
    public static int DirectWorldParentReadyCount { get; private set; }

    public static void Reset()
    {
        ReadyCount = 0;
        DirectWorldParentReadyCount = 0;
    }

    public override void _Ready()
    {
        ReadyCount++;
        if (ReadyCount < 2 || GetParent() is not SeamlessPrologueWorld) return;
        DirectWorldParentReadyCount++;
        Name = "MutatedBySecondReady";
        QueueFree();
    }
}

internal partial class SeamlessRenameQueueFreeSector : PrologueSector
{
    public static int RenamedCount { get; private set; }
    public static int ProcessTicks { get; private set; }
    public static int PhysicsTicks { get; private set; }
    public static int DrawTicks { get; private set; }

    public static void Reset()
    {
        RenamedCount = 0;
        ProcessTicks = 0;
        PhysicsTicks = 0;
        DrawTicks = 0;
    }

    public override void _Ready()
    {
        Renamed += () =>
        {
            RenamedCount++;
            QueueFree();
        };
        QueueRedraw();
    }

    public override void _Process(double delta) => ProcessTicks++;
    public override void _PhysicsProcess(double delta) => PhysicsTicks++;
    public override void _Draw() => DrawTicks++;
}

internal partial class SeamlessVisibilityTransformSector : PrologueSector
{
    public static Action<SeamlessVisibilityTransformSector> ActivationObserved { get; set; }
    public static int VisibilityActivationCount { get; private set; }
    public static int ProcessTicks { get; private set; }
    public static int PhysicsTicks { get; private set; }
    public static int DrawTicks { get; private set; }

    public static void Reset()
    {
        ActivationObserved = null;
        VisibilityActivationCount = 0;
        ProcessTicks = 0;
        PhysicsTicks = 0;
        DrawTicks = 0;
    }

    public override void _Ready()
    {
        VisibilityChanged += () =>
        {
            if (!Visible || !IsVisibleInTree()) return;
            VisibilityActivationCount++;
            ActivationObserved?.Invoke(this);
            Position += new Vector2(7f, 0f);
        };
        QueueRedraw();
    }

    public override void _Process(double delta) => ProcessTicks++;
    public override void _PhysicsProcess(double delta) => PhysicsTicks++;
    public override void _Draw() => DrawTicks++;
}

internal partial class SeamlessEscapingCheckpointSector : PrologueSector
{
    public static CheckpointTrigger EscapedCheckpoint { get; private set; }
    public static int ActivationCount { get; private set; }
    public static Action<CheckpointTrigger> ActivationObserved { get; set; }
    [Export] public NodePath CheckpointPath { get; set; }

    public static void Reset()
    {
        EscapedCheckpoint = null;
        ActivationCount = 0;
        ActivationObserved = null;
    }

    public override void _Ready()
    {
        CheckpointTrigger checkpoint = GetNode<CheckpointTrigger>(CheckpointPath);
        VisibilityChanged += () =>
        {
            if (!Visible || !IsVisibleInTree() || checkpoint.GetParent() == GetParent()) return;
            ActivationCount++;
            ActivationObserved?.Invoke(checkpoint);
            EscapedCheckpoint = checkpoint;
            checkpoint.RequestReady();
            checkpoint.Reparent(GetParent().GetNode<PrologueSector>("CanyonEntrance"));
        };
    }
}

internal partial class SeamlessEncounterActor : Node2D, IEncounterActor
{
#pragma warning disable CS0067
    public event Action Defeated;
#pragma warning restore CS0067

    public void ResetForCheckpoint() { }
}

internal partial class SeamlessFixturePlayerDriver : Node
{
    public float DriveAxis { get; set; }
    public float Speed { get; set; } = 1600f;

    public override void _PhysicsProcess(double delta)
    {
        var player = GetParent<CharacterBody2D>();
        player.Velocity = new Vector2(DriveAxis * Speed, 0f);
        player.MoveAndSlide();
    }
}
