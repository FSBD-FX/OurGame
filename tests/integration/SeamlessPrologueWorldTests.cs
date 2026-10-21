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
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class SeamlessPrologueWorldTests
{
    private const float SectorWidth = 128f;
    private const float SectorTop = -200f;
    private const float SectorHeight = 400f;

    public static async Task RunThrusterResetObserverOnly(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        ExpectedWarningLogger.AssertMatcherGuards();
        using var warnings = new ExpectedWarningLogger();
        await ThrowingObserverReentryAndSameTickRemountStayTransactional(tree);
        warnings.AssertExact(ExpectedWarningLogger.ThrowingClawObserverExpectations());
        GD.Print("SEAMLESS_RESET_OBSERVER_SCOPE_OK original_fixture_geometry=true authority_revocation=true exact_warning_source_count=true human=false full_route=false");
    }

    public static async Task Run(SceneTree tree)
    {
        await AutomaticBoundaryCrossingPreservesThePersistentPlayerAndBlendsCamera(tree);
        await AscentHeadroomBlendsAtSeamsAndRestoresAuthoredCameraPosition(tree);
        GD.Print("ASCENT_CAMERA_LIFECYCLE_OK blend=True checkpoint=True new_run=True exit_tree=True offset_unchanged=True");
        await FreshAimRestoresAscentHeadroomWhileReleaseMomentumRemains(tree);
        GD.Print("ASCENT_CAMERA_FRESH_AIM_OK release_flag=True smooth_headroom_restore=True");
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

    public static async Task RunCheckpointCompatibility(SceneTree tree)
    {
        await RunFixRound3(tree);
        await CheckpointRestoreAndNewRunResetUseContinuousWorldCoordinates(tree);
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
            TestAssert.Equal(Mathf.RoundToInt(SectorWidth), fixture.Camera.LimitRight,
                "reverse adjacent crossing immediately contracts the exposed camera edge");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent],
                "reverse crossing restores the ordered entrance required set");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task AscentHeadroomBlendsAtSeamsAndRestoresAuthoredCameraPosition(
        SceneTree tree)
    {
        Vector2 authoredPosition = new(17f, -23f);
        Fixture fixture = await CreateFixture(tree, "AscentCameraLifecycle",
            withEntranceTarget: false, authoredCameraPosition: authoredPosition);
        try
        {
            Vector2 offset = new(9f, -7f);
            fixture.Camera.Offset = offset;
            Vector2 zoom = fixture.Camera.Zoom;
            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 2f + 32f, 0f);
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "headroom fixture relocates into the basin before an adjacent crossing");
            TestAssert.Equal(authoredPosition, fixture.Camera.Position,
                "a preloaded ascent does not move the camera while the basin is current");
            TestAssert.True(fixture.World.TryActivateCheckpoint(
                PrologueSectorId.CanyonBasin, fixture.Player.GlobalPosition),
                "headroom fixture activates a real basin checkpoint");

            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonAscent, 30,
                "real physics crosses from basin into ascent");
            fixture.Driver.DriveAxis = 0f;
            TestAssert.True(fixture.Camera.Position.Y < -23f && fixture.Camera.Position.Y > -143f,
                "ascent entry begins an upward camera-local blend without snapping");
            float previousY = fixture.Camera.Position.Y;
            int changedFrames = 0;
            for (int frame = 0; frame < 90; frame++)
            {
                await PhysicsFrames(tree, 1);
                float currentY = fixture.Camera.Position.Y;
                if (currentY < previousY) changedFrames++;
                TestAssert.True(currentY <= previousY && currentY >= -143.01f,
                    "ascent camera headroom converges monotonically without overshoot");
                TestAssert.Near(17f, fixture.Camera.Position.X, .0001f,
                    "headroom preserves the authored camera horizontal position");
                TestAssert.Equal(offset, fixture.Camera.Offset,
                    "headroom leaves the nonzero static camera offset untouched");
                TestAssert.Equal(zoom, fixture.Camera.Zoom,
                    "headroom does not change camera zoom");
                previousY = currentY;
            }
            TestAssert.True(changedFrames > 1,
                "ascent entry blends across multiple scheduled physics frames");
            TestAssert.Near(-143f, fixture.Camera.Position.Y, .02f,
                "ascent settles 120 pixels above the authored camera rest position");
            AssertCameraSnappedTo(fixture.Camera, SectorWidth * 3f,
                "settled ascent retains the authored sector limits");

            fixture.Driver.DriveAxis = -1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonBasin, 30,
                "real reverse movement leaves ascent through the ordinary seam");
            fixture.Driver.DriveAxis = 0f;
            TestAssert.True(fixture.Camera.Position.Y > previousY && fixture.Camera.Position.Y < -23f,
                "ordinary ascent exit starts a smooth return instead of snapping to rest");
            previousY = fixture.Camera.Position.Y;
            for (int frame = 0; frame < 90; frame++)
            {
                await PhysicsFrames(tree, 1);
                TestAssert.True(fixture.Camera.Position.Y >= previousY && fixture.Camera.Position.Y <= -22.99f,
                    "ordinary ascent exit converges monotonically to authored rest");
                previousY = fixture.Camera.Position.Y;
            }
            TestAssert.Near(-23f, fixture.Camera.Position.Y, .02f,
                "normal exit removes ascent headroom");

            fixture.Player.GlobalPosition = new Vector2(32f, 0f);
            await PhysicsFrames(tree, 1);
            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 3f + 32f, 0f);
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(new Vector2(17f, -143f), fixture.Camera.Position,
                "a direct multihop into ascent snaps headroom with the camera limits");
            TestAssert.True(fixture.World.RestoreCheckpoint(), "basin checkpoint restore succeeds");
            TestAssert.Equal(authoredPosition, fixture.Camera.Position,
                "checkpoint recovery immediately restores the exact authored camera position");

            fixture.Driver.DriveAxis = 1f;
            await WaitForSector(tree, fixture, PrologueSectorId.CanyonAscent, 30,
                "player reenters ascent before new-run reset");
            fixture.Driver.DriveAxis = 0f;
            fixture.Level.StartNewGame();
            TestAssert.Equal(authoredPosition, fixture.Camera.Position,
                "new-run reset immediately removes even a partially blended ascent bias");
            TestAssert.Equal(offset, fixture.Camera.Offset, "camera lifecycle preserves static offset");
            TestAssert.Equal(zoom, fixture.Camera.Zoom, "camera lifecycle preserves zoom");

            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 3f + 32f, 0f);
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.CanyonAscent, fixture.World.CurrentSector,
                "world teardown begins while ascent is the committed sector");
            TestAssert.Equal(new Vector2(17f, -143f), fixture.Camera.Position,
                "world teardown starts with a real nonzero ascent camera bias");
            fixture.WorldRoot.RemoveChild(fixture.World);
            try
            {
                TestAssert.True(!fixture.World.IsInsideTree() && fixture.Player.IsInsideTree() &&
                    fixture.Camera.IsInsideTree(),
                    "the world exits the tree independently of the surviving player and camera");
                TestAssert.Equal(authoredPosition, fixture.Camera.Position,
                    "world exit immediately restores the exact authored camera rest position");
                TestAssert.Equal(offset, fixture.Camera.Offset,
                    "world exit preserves the nonzero static camera offset");
                TestAssert.Equal(zoom, fixture.Camera.Zoom, "world exit preserves camera zoom");
            }
            finally
            {
                // The detached world is no longer owned by DisposeFixture's level subtree.
                fixture.World.Free();
            }
        }
        finally { await DisposeFixture(tree, fixture); }
    }

    public static async Task RunTask3(SceneTree tree) =>
        await LayoutTwoUsesContinuousLocalRouteFocusAndStagedAbyssVisibility(tree);

    private static async Task LayoutTwoUsesContinuousLocalRouteFocusAndStagedAbyssVisibility(
        SceneTree tree)
    {
        Vector2I previousSize = tree.Root.Size;
        Vector2I previousContentSize = tree.Root.ContentScaleSize;
        tree.Root.Size = new Vector2I(1280, 720);
        tree.Root.ContentScaleSize = new Vector2I(1280, 720);
        Fixture fixture = await CreateFixture(tree, "Task3ContinuousRouteCamera",
            withEntranceTarget: false, layoutVersion: 2);
        try
        {
            Rect2 design = CanyonWorldLayout.DesignBounds;
            TestAssert.Equal(Mathf.RoundToInt(design.Position.X), fixture.Camera.LimitLeft,
                "layout two uses the one global design left limit");
            TestAssert.Equal(Mathf.RoundToInt(design.Position.Y), fixture.Camera.LimitTop,
                "layout two uses the one global design top limit");
            TestAssert.Equal(Mathf.RoundToInt(design.End.X), fixture.Camera.LimitRight,
                "layout two uses the one global design right limit");
            TestAssert.Equal(Mathf.RoundToInt(design.End.Y), fixture.Camera.LimitBottom,
                "layout two uses the one global design bottom limit");
            TestAssert.True(ReferenceEquals(fixture.Player.GetParent(), fixture.WorldRoot.GetParent()
                    .GetNode("PlayerRoot")) && ReferenceEquals(fixture.Camera.GetParent(), fixture.Player),
                "the same persistent PlayerRoot/Player graph owns the live Camera2D");

            (Vector2 Destination, Vector2 ExpectedLocal)[] forward =
            [
                (new Vector2(1290f, 200f), new Vector2(0f, 40f)),
                (new Vector2(2000f, 3610f), new Vector2(100f, -40f)),
                (new Vector2(4500f, 3500f), new Vector2(80f, -140f)),
                (new Vector2(5130f, 1000f), new Vector2(140f, -20f))
            ];
            (Vector2 Destination, Vector2 ExpectedLocal)[] reverse =
            [
                (new Vector2(5000f, 1000f), new Vector2(80f, -140f)),
                (new Vector2(4500f, 3700f), new Vector2(100f, -40f)),
                (new Vector2(2000f, 3500f), new Vector2(0f, 80f)),
                (new Vector2(1270f, 200f), new Vector2(80f, -20f))
            ];
            await AssertOrdinarySeamsBlendWithoutSnap(tree, fixture, forward, "forward");
            await AssertOrdinarySeamsBlendWithoutSnap(tree, fixture, reverse, "reverse");

            // Seam motion uses the fixture driver. Camera projection owns the requested vertical
            // velocity directly, so stop the driver before it can replace Y with zero.
            fixture.Driver.SetPhysicsProcess(false);
            await AssertActualAbyssVelocityProjection(tree, fixture, 0f, 80f, "stationary");
            await AssertActualAbyssVelocityProjection(tree, fixture, 390f, 170f, "half-fall");
            await AssertActualAbyssVelocityProjection(tree, fixture, 780f, 260f, "max-fall");
            await AssertEveryDescentFrameKeepsAuthoredBrakeWallVisible(tree, fixture);
            GD.Print("TASK3_ABYSS_CAMERA_VALIDITY_OK speeds=0,390,780 " +
                "scheduled_descent_frames=118 independent_brake_bounds=True blend_lag=True");

            fixture.Camera.Offset = new Vector2(9f, -7f);
            await AssertAbyssStage(tree, fixture, new Vector2(2240f, 400f), 780f,
                CanyonWorldLayout.FirstAbyssBrakeFace,
                CanyonWorldLayout.AbyssSideExits[0], "pre-commit Y=400");
            Rect2 preCommitViewport = CameraViewport(fixture.Camera);
            TestAssert.True(preCommitViewport.Encloses(CanyonWorldLayout.FirstAbyssBrakeFace),
                "the complete first reachable brake face fits before no-damage commitment");
            TestAssert.True(!Intersects(preCommitViewport, CanyonWorldLayout.AbyssSafeBottom),
                "the safe bottom is staged later instead of remaining visible through the whole shaft");
            await AssertAbyssStage(tree, fixture, new Vector2(2240f, 1920f), 0f,
                CanyonWorldLayout.AbyssBrakeWallSegments[1],
                CanyonWorldLayout.AbyssSideExits[1], "Y=2240 exit with 320px lead");
            await AssertAbyssStage(tree, fixture, new Vector2(2240f, 2780f), 0f,
                CanyonWorldLayout.AbyssBrakeWallSegments[2],
                CanyonWorldLayout.AbyssSideExits[2], "Y=3100 exit with 320px lead");
            await AssertAbyssStage(tree, fixture, new Vector2(2240f, 3240f), 0f,
                CanyonWorldLayout.AbyssBrakeWallSegments[3],
                CanyonWorldLayout.AbyssSafeBottom, "safe bottom before final landing decision");
            TestAssert.Equal(new Vector2(9f, -7f), fixture.Camera.Offset,
                "route focus owns Position and leaves the presentation-shake Offset channel untouched");
            GD.Print("TASK3_CONTINUOUS_CAMERA_OK global_limits=True seam_directions=8 " +
                "local_position=True abyss_windows=4 offset_unchanged=True");
        }
        finally
        {
            await DisposeFixture(tree, fixture);
            tree.Root.Size = previousSize;
            tree.Root.ContentScaleSize = previousContentSize;
        }
    }

    private static async Task AssertOrdinarySeamsBlendWithoutSnap(SceneTree tree, Fixture fixture,
        IReadOnlyList<(Vector2 Destination, Vector2 ExpectedLocal)> seams, string direction)
    {
        Node originalPlayerRoot = fixture.Player.GetParent();
        foreach ((Vector2 destination, Vector2 expectedLocal) in seams)
        {
            Vector2 before = fixture.Camera.Position;
            fixture.Player.GlobalPosition = destination;
            await PhysicsFrames(tree, 1);
            Vector2 first = fixture.Camera.Position;
            TestAssert.True(first.DistanceTo(before) > .001f && first.DistanceTo(before) < 45f,
                $"{direction} ordinary seam begins a bounded blend rather than snapping");
            TestAssert.True(Mathf.Abs(first.X) < 400f && Mathf.Abs(first.Y) < 400f,
                $"{direction} seam writes a player-local Camera2D.Position");
            TestAssert.True(ReferenceEquals(originalPlayerRoot, fixture.Player.GetParent()),
                $"{direction} seam preserves the same PlayerRoot instance");
            float previousDistance = first.DistanceTo(expectedLocal);
            int distinctFrames = 0;
            for (int frame = 0; frame < 24; frame++)
            {
                Vector2 previous = fixture.Camera.Position;
                fixture.World._PhysicsProcess(1d / 60d);
                Vector2 current = fixture.Camera.Position;
                float distance = current.DistanceTo(expectedLocal);
                if (current.DistanceTo(previous) > .001f) distinctFrames++;
                TestAssert.True(current.DistanceTo(previous) < 45f,
                    $"{direction} seam focus derivative stays bounded at frame {frame}");
                TestAssert.True(distance <= previousDistance + .001f,
                    $"{direction} seam converges monotonically at frame {frame}");
                previousDistance = distance;
            }
            TestAssert.True(distinctFrames > 2,
                $"{direction} seam convergence spans multiple camera frames");
            for (int frame = 0; frame < 120; frame++)
                fixture.World._PhysicsProcess(1d / 60d);
            TestAssert.True(fixture.Camera.Position.DistanceTo(expectedLocal) < .25f,
                $"{direction} seam eventually converges near the hand-authored local target");
        }
    }

    private static async Task AssertActualAbyssVelocityProjection(SceneTree tree, Fixture fixture,
        float fallSpeed, float expectedLocalY, string context)
    {
        fixture.Player.GlobalPosition = new Vector2(2240f, 400f);
        fixture.Player.Velocity = new Vector2(0f, fallSpeed);
        await PhysicsFrames(tree, 150);

        TestAssert.Near(expectedLocalY, fixture.Camera.Position.Y, .25f,
            $"{context} actual Camera2D projection reflects the requested vertical velocity");
        Rect2 viewport = CameraViewport(fixture.Camera);
        TestAssert.True(viewport.Encloses(new Rect2(2490f, 520f, 36f, 160f)) &&
                viewport.Encloses(new Rect2(2350f, 700f, 200f, 40f)),
            $"{context} actual 1280x720 Camera2D viewport contains the pre-commit brake face and exit");
    }

    private static async Task AssertEveryDescentFrameKeepsAuthoredBrakeWallVisible(
        SceneTree tree, Fixture fixture)
    {
        Rect2[] independentlyAuthoredBrakeWalls =
        [
            new(2490f, 400f, 36f, 800f),
            new(2490f, 1200f, 36f, 800f),
            new(2490f, 2000f, 36f, 800f),
            new(2490f, 2800f, 36f, 800f)
        ];
        fixture.Player.GlobalPosition = new Vector2(2240f, 400f);
        fixture.Player.Velocity = Vector2.Zero;
        await PhysicsFrames(tree, 150);

        bool observedBlendLag = false;
        int sampledFrames = 0;
        for (float y = 424f; y <= 3240f; y += 24f)
        {
            fixture.Player.GlobalPosition = new Vector2(2240f, y);
            fixture.Player.Velocity = new Vector2(0f, 780f);
            await PhysicsFrames(tree, 1);
            sampledFrames++;

            Rect2 viewport = CameraViewport(fixture.Camera);
            bool brakeVisible = false;
            foreach (Rect2 wall in independentlyAuthoredBrakeWalls)
                brakeVisible |= Intersects(viewport, wall);
            TestAssert.True(brakeVisible,
                $"geometric descent fixture frame {sampledFrames} at Y={y} keeps an independently authored brake segment visible");
            observedBlendLag |= fixture.Camera.Position.Y < 259f;
        }
        TestAssert.True(observedBlendLag,
            "per-frame shaft coverage includes the camera's real velocity-look-ahead blend lag");
        TestAssert.True(sampledFrames > 100,
            "small per-frame fixture increments cover the shaft instead of sampling four snapshots");
    }

    private static async Task AssertAbyssStage(SceneTree tree, Fixture fixture,
        Vector2 playerPosition, float fallSpeed, Rect2 brakeFace, Rect2 decisionSurface,
        string context)
    {
        fixture.Player.GlobalPosition = playerPosition;
        fixture.Player.Velocity = new Vector2(0f, fallSpeed);
        await PhysicsFrames(tree, 150);
        Rect2 viewport = CameraViewport(fixture.Camera);
        TestAssert.True(Intersects(viewport, brakeFace),
            $"{context} keeps a reachable authored brake-wall segment visible");
        TestAssert.True(viewport.Encloses(decisionSurface),
            $"{context} exposes the authored safe decision surface without off-screen guidance");
    }

    private static Rect2 CameraViewport(Camera2D camera)
    {
        Vector2 size = camera.GetViewportRect().Size / camera.Zoom.Abs();
        return new Rect2(camera.GetScreenCenterPosition() - size * .5f, size);
    }

    private static bool Intersects(Rect2 left, Rect2 right) =>
        left.Position.X <= right.End.X && left.End.X >= right.Position.X &&
        left.Position.Y <= right.End.Y && left.End.Y >= right.Position.Y;

    public static async Task RunTask2(SceneTree tree)
    {
        await ExplicitPhysicalAdjacencyIsAuthoritativeInBothDirections(tree);
        await SubscribeThenInitialReadReacquiresMissedResidencyAndExitUnbinds(tree);
        await ActiveAdjacentTerrainTargetPinsAcrossTravelReelSwingAndBoostRelease(tree);
        await ExternalResidentTargetRemovalClearsTheClawSynchronously(tree);
    }

    private static async Task SubscribeThenInitialReadReacquiresMissedResidencyAndExitUnbinds(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task2ResidencyReentry", withEntranceTarget: true);
        bool worldDetached = false;
        try
        {
            InvokeClawResidencyBinding(fixture.World, bind: false);
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            TestAssert.Equal(0, PinnedSectorCount(fixture.World),
                "the real residency event is missed while the world consumer is unbound");

            InvokeClawResidencyBinding(fixture.World, bind: true);
            TestAssert.Equal(1, PinnedSectorCount(fixture.World),
                "the production subscribe-then-read handshake reacquires the missed resident target");

            InvokeResidencyHandler(fixture.World, target, true);
            InvokeResidencyHandler(fixture.World, target, true);
            TestAssert.Equal(1, PinnedSectorCount(fixture.World),
                "duplicate true notifications keep one logical owner pin");

            var stale = new GrappleSurface2D { Name = "StaleResidencyTarget" };
            fixture.World.GetNode("CanyonDescent/Gameplay").AddChild(stale);
            InvokeResidencyHandler(fixture.World, stale, false);
            TestAssert.Equal(1, PinnedSectorCount(fixture.World),
                "false for a stale target cannot clear the current resident target pin");

            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 3f + 32f, 0f);
            InvokeTransition(fixture.World, PrologueSectorId.CanyonAscent);
            TestAssert.True(Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonEntrance),
                "the actual initial ResidencyTarget read pins the exact owner after the missed event");

            int beforeExit = ResidencySubscriberCount(fixture.Claw);
            fixture.WorldRoot.RemoveChild(fixture.World);
            worldDetached = true;
            int afterExit = ResidencySubscriberCount(fixture.Claw);
            TestAssert.Equal(beforeExit - 1, afterExit,
                "world exit removes exactly its residency subscriber");
            fixture.World.Free();
        }
        finally
        {
            if (!worldDetached && GodotObject.IsInstanceValid(fixture.World))
                fixture.World.QueueFree();
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task ExplicitPhysicalAdjacencyIsAuthoritativeInBothDirections(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task2ExplicitAdjacency", withEntranceTarget: false);
        try
        {
            fixture.World.Definitions[0].Previous = PrologueSectorId.LostTemple;
            fixture.World.Definitions[0].Next = null;
            fixture.World.Definitions[1].Previous = null;
            fixture.World.Definitions[1].Next = PrologueSectorId.LostTemple;
            fixture.World.Definitions[2].Previous = null;
            fixture.World.Definitions[2].Next = null;
            fixture.World.Definitions[3].Previous = null;
            fixture.World.Definitions[3].Next = null;
            fixture.World.Definitions[4].Previous = null;
            fixture.World.Definitions[4].Next = null;

            (PrologueSectorId Left, PrologueSectorId Right)[] seams =
            [
                (PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent),
                (PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin),
                (PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent),
                (PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple)
            ];
            foreach ((PrologueSectorId left, PrologueSectorId right) in seams)
            {
                TestAssert.True(InvokeAreAdjacent(fixture.World, left, right),
                    $"the explicit {left}-to-{right} physical seam ignores divergent legacy fields");
                TestAssert.True(InvokeAreAdjacent(fixture.World, right, left),
                    $"the explicit {right}-to-{left} reverse seam ignores divergent legacy fields");
            }
            TestAssert.True(!InvokeAreAdjacent(fixture.World, PrologueSectorId.CanyonEntrance,
                    PrologueSectorId.LostTemple),
                "legacy Previous cannot invent a physical streaming seam");
            TestAssert.True(!InvokeAreAdjacent(fixture.World, PrologueSectorId.CanyonDescent,
                    PrologueSectorId.LostTemple),
                "legacy Next cannot invent a physical streaming seam");
        }
        finally { await DisposeFixture(tree, fixture); }
    }

    private static async Task ActiveAdjacentTerrainTargetPinsAcrossTravelReelSwingAndBoostRelease(
        SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task2TargetResidency", withEntranceTarget: false,
            targetSector: PrologueSectorId.CanyonDescent);
        var observedStates = new List<MechanicalClawState>();
        var transaction = new List<string>();
        int targetLost = 0;
        int released = 0;
        bool latchedObservedMounted = false;
        bool boostObservedMounted = false;
        bool falseSawOldReference = false;
        bool reentrantRestoreSucceeded = false;
        bool laterFalseSawProtectedOwner = false;
        bool releasedSawProtectedOwner = false;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonDescent/Gameplay/ResidentAnchor");
            PrologueSector owner = fixture.World.GetNode<PrologueSector>("CanyonDescent");

            void Frames(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                observedStates.Add(frame.State);
                if (frame.State == MechanicalClawState.Latched)
                    latchedObservedMounted = ReferenceEquals(target, observed) && target.IsInsideTree() &&
                        ReferenceEquals(owner.GetParent(), fixture.World) &&
                        Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonDescent);
                if (frame.State == MechanicalClawState.Boost)
                {
                    transaction.Add("Boost");
                    boostObservedMounted = ReferenceEquals(observed, target) && target.IsInsideTree() &&
                        ReferenceEquals(owner.GetParent(), fixture.World) &&
                        Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonDescent);
                }
                if (frame.State == MechanicalClawState.Released)
                {
                    transaction.Add("Released");
                    released++;
                    releasedSawProtectedOwner = target.IsInsideTree() &&
                        ReferenceEquals(owner.GetParent(), fixture.World) &&
                        Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonDescent);
                }
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost) targetLost++;
            }
            void ReenterOnFalse(GrappleSurface2D observed, bool resident)
            {
                if (resident) return;
                transaction.Add("false");
                falseSawOldReference = ReferenceEquals(observed, target) &&
                    ReferenceEquals(fixture.Claw.ResidencyTarget, target) && target.IsInsideTree() &&
                    ReferenceEquals(owner.GetParent(), fixture.World);
                reentrantRestoreSucceeded = fixture.World.RestoreCheckpoint(
                    PrologueSectorId.LostTemple, new Vector2(SectorWidth * 4f + 32f, 0f));
            }
            void ObserveAfterReentry(GrappleSurface2D observed, bool resident)
            {
                if (resident) return;
                laterFalseSawProtectedOwner = ReferenceEquals(observed, target) &&
                    ReferenceEquals(fixture.Claw.ResidencyTarget, target) && target.IsInsideTree() &&
                    ReferenceEquals(owner.GetParent(), fixture.World) &&
                    fixture.World.CurrentSector == PrologueSectorId.LostTemple &&
                    Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonDescent) &&
                    Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonAscent) &&
                    Contains(fixture.World.MountedSectorIds, PrologueSectorId.LostTemple);
            }
            fixture.Claw.ClawFrameChanged += Frames;
            fixture.Claw.TargetResidencyChanged += ReenterOnFalse;
            fixture.Claw.TargetResidencyChanged += ObserveAfterReentry;

            SetInputFrame(fixture.Input, Frame(primaryPressed: true, primaryHeld: true,
                mouseWorld: target.WorldAnchor));
            for (int frame = 0; frame < 30 && fixture.Claw.ResidencyTarget == null; frame++)
            {
                await PhysicsFrames(tree, 1);
                SetInputFrame(fixture.Input, Frame(primaryHeld: true, mouseWorld: target.WorldAnchor));
            }
            TestAssert.Equal(target, fixture.Claw.ResidencyTarget,
                "the real non-legacy terrain target becomes resident on first Travel");
            TestAssert.True(observedStates.Contains(MechanicalClawState.Travel),
                "the fixture observes the real Travel publication before crossing the seam");

            Vector2 ascentPosition = new(SectorWidth * 3f + 32f, 0f);
            fixture.Player.GlobalPosition = ascentPosition;
            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            InvokeTransition(fixture.World, PrologueSectorId.CanyonAscent);
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                    PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
                "Travel keeps the outgoing target owner plus current and every landing neighbor mounted");
            TestAssert.Equal(target, fixture.Claw.ResidencyTarget,
                "crossing away from the adjacent target owner cannot force-loss the active Travel target");

            for (int frame = 0; frame < 60 &&
                !observedStates.Contains(MechanicalClawState.Reel); frame++)
            {
                SetInputFrame(fixture.Input, Frame(primaryHeld: true, mouseWorld: target.WorldAnchor));
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(latchedObservedMounted,
                "the synchronous Latched publication sees the exact resident target inside its mounted owner");
            TestAssert.True(observedStates.Contains(MechanicalClawState.Reel),
                "the active real target remains resident through Reel");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                    PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
                "Reel preserves target, current, and landing-neighbor coverage");

            fixture.Player.GlobalPosition = fixture.Claw.CurrentFrame.DockPoint;
            fixture.Claw._PhysicsProcess(.016d);
            TestAssert.True(observedStates.Contains(MechanicalClawState.Swing),
                "the real terrain claw reaches Swing without dropping residency");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonBasin,
                    PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
                "Swing preserves target, current, and landing-neighbor coverage");

            fixture.Player.GlobalPosition = ascentPosition;
            SetInputFrame(fixture.Input, Frame() with { JumpPressed = true });
            fixture.Claw._PhysicsProcess(.016d);

            TestAssert.Equal("Boost,false,Released", string.Join(",", transaction),
                "Space release publishes Boost, then matching false, then Released exactly once");
            TestAssert.True(boostObservedMounted,
                "Boost publishes while the exact active target remains inside its mounted owner");
            TestAssert.True(falseSawOldReference,
                "the first later false subscriber sees the old claw reference valid inside its owner");
            TestAssert.True(reentrantRestoreSucceeded,
                "a later false subscriber can synchronously request the LostTemple checkpoint");
            TestAssert.True(laterFalseSawProtectedOwner,
                "an observer after the reentrant transition still sees the exact target inside its mounted owner");
            TestAssert.True(releasedSawProtectedOwner,
                "Released publishes before the reentrant transition can retire the outgoing target owner");
            TestAssert.Equal(1, released,
                "the reentrant normal release publishes Released exactly once");
            TestAssert.True(fixture.Claw.ResidencyTarget == null &&
                    fixture.Claw.CurrentFrame.State == MechanicalClawState.Released &&
                    fixture.World.CurrentSector == PrologueSectorId.LostTemple,
                "the release transaction ends at the requested landing with no resident target");
            TestAssert.True(Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonDescent),
                "the outgoing owner remains mounted until a later queued streaming refresh");
            SetInputFrame(fixture.Input, Frame());
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(0, targetLost,
                "normal release and the final queued owner unload emit no TargetLost reset");
            TestAssert.True(!Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonDescent),
                "the unpinned outgoing owner retires on the queued streaming refresh after callbacks unwind");
            TestAssert.True(fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null,
                "the physics refresh keeps source target cleanup complete while unmounting the old owner");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonAscent, PrologueSectorId.LostTemple],
                "final LostTemple coverage keeps only the current sector and its explicit landing neighbor");

            const int retirementProcessFrameBudget = 6;
            for (int frame = 0; frame < retirementProcessFrameBudget &&
                (GodotObject.IsInstanceValid(target) || GodotObject.IsInstanceValid(owner) ||
                    fixture.World.RetiredSectorCount != 0); frame++)
                await ProcessFrames(tree, 1);

            TestAssert.True(!GodotObject.IsInstanceValid(target) &&
                    !GodotObject.IsInstanceValid(owner) && fixture.World.RetiredSectorCount == 0,
                $"bounded idle retirement frees the old target graph and drains its queue within " +
                $"{retirementProcessFrameBudget} process frames");
            TestAssert.Equal(1, released,
                "Released remains exactly once through the final owner-free window");
            TestAssert.Equal(0, targetLost,
                "normal release emits no TargetLost through the final owner-free window");
            fixture.Claw.ClawFrameChanged -= Frames;
            fixture.Claw.TargetResidencyChanged -= ReenterOnFalse;
            fixture.Claw.TargetResidencyChanged -= ObserveAfterReentry;
        }
        finally
        {
            await DisposeFixture(tree, fixture);
        }
    }

    private static async Task ExternalResidentTargetRemovalClearsTheClawSynchronously(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "Task2ExternalTargetLoss", withEntranceTarget: true);
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            TestAssert.Equal(target, fixture.Claw.ResidencyTarget,
                "external-loss fixture starts with a real resident terrain target");

            target.GetParent().RemoveChild(target);

            TestAssert.True(fixture.Claw.CurrentTarget == null && fixture.Claw.ResidencyTarget == null &&
                    fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle,
                "external target removal synchronously forces TargetLost cleanup before owner retirement");
            TestAssert.Equal(0, fixture.Line.Points.Length,
                "external target removal synchronously clears the motor-owned rope presentation");
            target.Free();
        }
        finally { await DisposeFixture(tree, fixture); }
    }

    public static async Task FreshAimRestoresAscentHeadroomWhileReleaseMomentumRemains(SceneTree tree)
    {
        Fixture fixture = await CreateFixture(tree, "AscentCameraFreshAim",
            withEntranceTarget: false, authoredCameraPosition: new Vector2(17f, -23f));
        fixture.Claw.ClawFrameChanged += HoldFreshAim;
        try
        {
            fixture.Player.GlobalPosition = new Vector2(SectorWidth * 3f + 32f, 0f);
            await PhysicsFrames(tree, 1);
            TestAssert.Equal(PrologueSectorId.CanyonAscent, fixture.World.CurrentSector,
                "fresh-aim camera fixture is committed to ascent");
            TestAssert.Equal(new Vector2(17f, -143f), fixture.Camera.Position,
                "fresh-aim camera fixture starts with the full ascent headroom");

            // This consumer fixture holds an airborne release lifetime through the motor's
            // public API. It does not claim to reproduce the physical release route: movement
            // is paused so only the world's genuinely scheduled camera response is measured.
            fixture.Driver.SetPhysicsProcess(false);
            fixture.Claw.SetPhysicsProcess(false);
            fixture.Modes.SetPhysicsProcess(false);
            fixture.Motor.ApplyGrappleRelease(Vector2.Zero, Vector2.Zero,
                MechanicalClawSettings.Default.ReleaseSpeedLimit, preserveLaunchMomentum: true);
            TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Motor.HasGrappleReleaseMomentum &&
                fixture.Player.Velocity.Y == 0f && fixture.Claw.CurrentFrame.State == MechanicalClawState.Idle,
                "public release API arranges an airborne zero-rise Idle release lifetime");
            await PhysicsFrames(tree, 30);
            TestAssert.True(fixture.Camera.Position.Y > -80f && fixture.Camera.Position.Y <= -23f,
                "the released Idle camera has genuinely eased back toward authored rest before aiming");

            // Let the scheduled reader consume action edges in their proper physics tick;
            // sampling immediately after ActionPress in this signal continuation can miss
            // IsActionJustPressed. No input property, claw state, or camera is injected.
            fixture.Input.SetPhysicsProcess(true);
            fixture.Modes.SetPhysicsProcess(true);
            Input.ActionPress(InputActions.SelectGrappleMode);
            await PhysicsFrames(tree, 2);
            Input.ActionRelease(InputActions.SelectGrappleMode);
            TestAssert.True(fixture.Modes.CanBeginGrapple,
                $"real mode input permits a fresh grapple; mode={fixture.Modes.CurrentMode} " +
                $"mode_pressed={fixture.Input.CurrentFrame.GrappleModePressed} " +
                $"shield_held={fixture.Input.CurrentFrame.ShieldHeld}");
            fixture.Claw.SetPhysicsProcess(true);
            Input.ActionPress(InputActions.PrimaryAction);
            await PhysicsFrames(tree, 2);
            TestAssert.Equal(MechanicalClawState.Aim, fixture.Claw.CurrentFrame.State,
                "real primary input advances the claw model from Idle into fresh Aim");

            float previousY = fixture.Camera.Position.Y;
            for (int frame = 0; frame < 3; frame++)
            {
                await PhysicsFrames(tree, 1);
                TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Motor.HasGrappleReleaseMomentum &&
                    fixture.Claw.CurrentFrame.State == MechanicalClawState.Aim,
                    "camera samples a real Aim phase while the airborne release flag still remains");
                TestAssert.True(fixture.Camera.Position.Y < previousY && fixture.Camera.Position.Y > -143f,
                    "fresh airborne Aim smoothly restores ascent headroom instead of continuing release recentering");
                previousY = fixture.Camera.Position.Y;
            }
        }
        finally
        {
            fixture.Claw.ClawFrameChanged -= HoldFreshAim;
            Input.ActionRelease(InputActions.PrimaryAction);
            Input.ActionRelease(InputActions.SelectGrappleMode);
            fixture.Input.ClearSampledFrame();
            await DisposeFixture(tree, fixture);
        }

        void HoldFreshAim(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.State == MechanicalClawState.Aim)
                fixture.Claw.SetPhysicsProcess(false);
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
                "successful commit leaves only graph-required and pinned non-staged children");
            AssertMounted(fixture.World,
                [PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonDescent,
                    PrologueSectorId.CanyonBasin, PrologueSectorId.CanyonAscent],
                "successful recovery keeps Basin, every explicit neighbor, and the active target owner");

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
        ulong authorityBeforeReset = 0, authorityAfterReset = 0;
        ulong playerBeforeReset = 0, playerAfterReset = 0, clawBeforeReset = 0, clawAfterReset = 0;
        bool resetReturned = false;
        int idleObserver = 0, synchronousLater = -1, synchronousIdle = -1;
        GrappleSurface2D target = null;
        bool externalTargetRemoved = false;
        Action<MechanicalClawFrame, GrappleSurface2D> throwingSubscription = null,
            reenterSubscription = null, laterSubscription = null;
        try
        {
            await SelectGrappleMode(tree, fixture);
            target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);

            fixture.World.Definitions[2].Scene = PackObservedSector(PrologueSectorId.CanyonBasin);
            fixture.World.Definitions[3].Scene = PackObservedSector(PrologueSectorId.CanyonAscent);
            SeamlessObservedSector.ReadyObserved = (world, sector) =>
            {
                int direct = DirectSectorChildren(world);
                peakChildren = Math.Max(peakChildren, direct);
                dictionaryMatchedTree &= direct == world.MountedSectorIds.Count;
                if (externalTargetRemoved || sector.SectorId != PrologueSectorId.CanyonBasin) return;
                externalTargetRemoved = true;
                target.GetParent().RemoveChild(target);
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
                authorityBeforeReset = fixture.Claw.AuthorityRevision;
                playerBeforeReset = fixture.Player.GetInstanceId();
                clawBeforeReset = fixture.Claw.GetInstanceId();
                fixture.Level.StartNewGame();
                authorityAfterReset = fixture.Claw.AuthorityRevision;
                playerAfterReset = fixture.Player.GetInstanceId();
                clawAfterReset = fixture.Claw.GetInstanceId();
                resetReturned = true;
            }
            void Later(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State == MechanicalClawState.Failed &&
                    frame.Failure == MechanicalClawFailure.TargetLost) laterObserver++;
                if (frame.State == MechanicalClawState.Idle) idleObserver++;
            }
            throwingSubscription = Throwing;
            reenterSubscription = Reenter;
            laterSubscription = Later;
            fixture.Claw.ClawFrameChanged += throwingSubscription;
            fixture.Claw.ClawFrameChanged += reenterSubscription;
            fixture.Claw.ClawFrameChanged += laterSubscription;

            InvokeTransition(fixture.World, PrologueSectorId.CanyonBasin);
            synchronousLater = laterObserver;
            synchronousIdle = idleObserver;
            TestAssert.Equal(PrologueSectorId.CanyonBasin, fixture.World.CurrentSector,
                "the active transition commits before queued reentrant work drains");
            await PhysicsFrames(tree, 1);
            fixture.Claw.ClawFrameChanged -= Throwing;
            fixture.Claw.ClawFrameChanged -= Reenter;
            fixture.Claw.ClawFrameChanged -= Later;
            SeamlessObservedSector.ReadyObserved = null;

            GD.Print($"SEAMLESS_RESET_OBSERVER_FACTS target_lost={targetLost} later_failed={laterObserver} " +
                $"sync_later_failed={synchronousLater} sync_idle={synchronousIdle} after_tick_idle={idleObserver} " +
                $"reset_returned={resetReturned} authority={authorityBeforeReset}->{authorityAfterReset} " +
                $"player={playerBeforeReset}/{playerAfterReset} claw={clawBeforeReset}/{clawAfterReset} " +
                $"current_state={fixture.Claw.CurrentFrame.State} current_sector={fixture.World.CurrentSector}");

            TestAssert.Equal(1, targetLost,
                "external target removal during staging publishes exactly one Failed/TargetLost notification");
            TestAssert.True(resetReturned && authorityAfterReset > authorityBeforeReset,
                "the observer reset synchronously revokes the old publication authority");
            TestAssert.True(playerBeforeReset != 0 && playerBeforeReset == playerAfterReset &&
                clawBeforeReset != 0 && clawBeforeReset == clawAfterReset,
                "the reentrant reset retains the same live Player and Claw identities");
            TestAssert.Equal(0, laterObserver,
                "an observer after a reset cannot receive the superseded Failed publication");
            TestAssert.Equal(0, synchronousLater,
                "old authority is revoked before the transition call returns");
            TestAssert.Equal(1, synchronousIdle,
                "one new-authority Idle is delivered synchronously after the reentrant reset");
            TestAssert.True(idleObserver >= synchronousIdle,
                "the following ordinary physics Idle does not erase the synchronous receipt");
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
            if (GodotObject.IsInstanceValid(fixture.Claw))
            {
                if (throwingSubscription != null) fixture.Claw.ClawFrameChanged -= throwingSubscription;
                if (reenterSubscription != null) fixture.Claw.ClawFrameChanged -= reenterSubscription;
                if (laterSubscription != null) fixture.Claw.ClawFrameChanged -= laterSubscription;
            }
            // Only this fixture deliberately detached this node. The Level can
            // no longer retire it; failure and success must release it alike.
            if (externalTargetRemoved && GodotObject.IsInstanceValid(target) &&
                !target.IsInsideTree() && target.GetParent() == null)
                target.Free();
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
        bool normalFalseSawMountedTarget = false;
        try
        {
            await SelectGrappleMode(tree, fixture);
            GrappleSurface2D target = fixture.World.GetNode<GrappleSurface2D>(
                "CanyonEntrance/Gameplay/EntranceAnchor");
            await LatchRealClaw(tree, fixture, target);
            TestAssert.True(ReferenceEquals(target, fixture.Claw.ResidencyTarget) &&
                    fixture.Claw.CurrentFrame.State is MechanicalClawState.Travel or
                        MechanicalClawState.Latched or MechanicalClawState.Reel or
                        MechanicalClawState.Swing,
                "the stress fixture begins with a real resident physics-resolved claw target");

            void Record(MechanicalClawFrame frame, GrappleSurface2D observed)
            {
                if (frame.State != MechanicalClawState.Failed ||
                    frame.Failure != MechanicalClawFailure.TargetLost) return;
                targetLostCount++;
            }
            void Residency(GrappleSurface2D observed, bool resident)
            {
                if (resident) return;
                normalFalseSawMountedTarget = ReferenceEquals(target, observed) &&
                    ReferenceEquals(target, fixture.Claw.ResidencyTarget) && target.IsInsideTree() &&
                    Contains(fixture.World.MountedSectorIds, PrologueSectorId.CanyonEntrance);
            }
            fixture.Claw.ClawFrameChanged += Record;
            fixture.Claw.TargetResidencyChanged += Residency;
            fixture.Claw.ResetTransientState();
            fixture.Claw.TargetResidencyChanged -= Residency;
            TestAssert.True(normalFalseSawMountedTarget,
                "normal release clears residency while the old target and owner remain mounted");
            TestAssert.Equal(MechanicalClawState.Idle, fixture.Claw.CurrentFrame.State,
                "normal release cleanup completes before the streaming stress begins");

            int roundTrips = 0;
            int templeArrivals = 0;
            int maxMounted = fixture.World.MountedSectorIds.Count;
            int maxRetired = fixture.World.RetiredSectorCount;
            bool headedRight = true;
            fixture.Driver.DriveAxis = 1f;
            SetInputFrame(fixture.Input, Frame());
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
            TestAssert.Equal(0, targetLostCount,
                "normal desired-set shrink never force-notifies a released resident target");
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
        bool withEntranceTarget, Vector2? authoredCameraPosition = null,
        PrologueSectorId? targetSector = null, int layoutVersion = 1)
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
        if (authoredCameraPosition.HasValue)
            player.GetNode<Camera2D>("Camera2D").Position = authoredCameraPosition.Value;
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
            LayoutVersion = layoutVersion,
            ProcessPhysicsPriority = 100
        };
        foreach (PrologueSectorDefinition definition in Definitions(withEntranceTarget, targetSector,
                     layoutVersion))
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

    private static List<PrologueSectorDefinition> Definitions(bool withEntranceTarget,
        PrologueSectorId? targetSector = null, int layoutVersion = 1)
    {
        PackedScene[] scenes = new PackedScene[5];
        PrologueSectorId[] ids = Enum.GetValues<PrologueSectorId>();
        for (int index = 0; index < ids.Length; index++)
            scenes[index] = PackSector(ids[index],
                targetSector == ids[index] || withEntranceTarget && index == 0,
                layoutVersion == 2
                    ? new Rect2(Vector2.Zero, CanyonWorldLayout.SectorBounds[ids[index]].Size)
                    : null);

        if (layoutVersion == 2)
        {
            return
            [
                DefinitionV2(ids[0], null, ids[1], scenes[0]),
                DefinitionV2(ids[1], ids[0], ids[2], scenes[1]),
                DefinitionV2(ids[2], ids[1], ids[3], scenes[2]),
                DefinitionV2(ids[3], ids[2], ids[4], scenes[3]),
                DefinitionV2(ids[4], ids[3], null, scenes[4])
            ];
        }
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

    private static PrologueSectorDefinition DefinitionV2(PrologueSectorId id,
        PrologueSectorId? previous, PrologueSectorId? next, PackedScene scene)
    {
        Rect2 world = CanyonWorldLayout.SectorBounds[id];
        return new PrologueSectorDefinition(id, scene, world.Position,
            new Rect2(Vector2.Zero, world.Size), previous, next);
    }

    private static PackedScene PackSector(PrologueSectorId id, bool withEntranceTarget,
        Rect2? localBounds = null)
    {
        var sector = new PrologueSector
        {
            Name = id.ToString(),
            SectorId = id,
            Bounds = localBounds ?? new Rect2(0f, SectorTop, SectorWidth, SectorHeight)
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
                Name = id == PrologueSectorId.CanyonEntrance ? "EntranceAnchor" : "ResidentAnchor",
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
            (!ReferenceEquals(fixture.Claw.ResidencyTarget, target) ||
                fixture.Claw.CurrentFrame.State is not (MechanicalClawState.Latched or
                    MechanicalClawState.Reel or MechanicalClawState.Swing)); frame++)
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

    private static bool InvokeAreAdjacent(SeamlessPrologueWorld world, PrologueSectorId left,
        PrologueSectorId right) =>
        (bool)typeof(SeamlessPrologueWorld).GetMethod("AreAdjacent",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(world, [left, right])!;

    private static void InvokeResidencyHandler(SeamlessPrologueWorld world, GrappleSurface2D target,
        bool resident) => typeof(SeamlessPrologueWorld).GetMethod("OnTargetResidencyChanged",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(world, [target, resident]);

    private static void InvokeClawResidencyBinding(SeamlessPrologueWorld world, bool bind)
    {
        string methodName = bind ? "BindClawResidency" : "UnbindClawResidency";
        MethodInfo method = typeof(SeamlessPrologueWorld).GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException($"Missing production residency method {methodName}.");
        method.Invoke(world, null);
    }

    private static int PinnedSectorCount(SeamlessPrologueWorld world) =>
        ((IReadOnlyCollection<PrologueSectorId>)typeof(SeamlessPrologueWorld)
            .GetField("_pinnedSectors", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(world)!).Count;

    private static int ResidencySubscriberCount(MechanicalClawController claw)
    {
        var subscribers = (Delegate)typeof(MechanicalClawController)
            .GetField("TargetResidencyChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(claw);
        return subscribers?.GetInvocationList().Length ?? 0;
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
