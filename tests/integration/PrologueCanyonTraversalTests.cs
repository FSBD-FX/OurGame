using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Player;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Shared;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Ui;
using Knight.World;

namespace Knight.Tests.Integration;

public static class PrologueCanyonTraversalTests
{
    private const float PlayerCollisionRadius = 20f;
    private const float PlayerCollisionHalfHeight = 36f;
    private const float GripStoneCatchSafetyMargin = 12f;
    private const float GripStoneCatchHeadroom = 48f;
    private const int StableLandingRequirement = 12;
    private const float LandingMarginRatio = .15f;
    private const float LandingApproachTolerance = 8f;
    private const int SingleJumpBudget = 105;
    private const int AssistedBudget = 240;
    private const string ViewportInputTransport = "viewport+inputmap os_warp=False";
    private static readonly SectorCase[] Sectors =
    [
        new("CanyonEntrance", "res://scenes/prologue/sectors/CanyonEntrance.tscn"),
        new("CanyonDescent", "res://scenes/prologue/sectors/CanyonDescent.tscn"),
        new("CanyonBasin", "res://scenes/prologue/sectors/CanyonBasin.tscn"),
        new("CanyonAscent", "res://scenes/prologue/sectors/CanyonAscent.tscn"),
        new("LostTemple", "res://scenes/prologue/sectors/LostTemple.tscn")
    ];

    private static readonly HashSet<string> CompletedLessonActions = new(StringComparer.Ordinal);
    private static SceneTree ActiveTree;
    private static Node2D ActiveInputCanvas;

    public static async Task Run(SceneTree tree)
    {
        ActiveTree = tree;
        CompletedLessonActions.Clear();
        ReleaseAll();
        TestAssert.True(InputMap.HasAction(InputActions.SelectAttackMode) &&
            InputMap.HasAction(InputActions.SelectGrappleMode) &&
            InputMap.HasAction(InputActions.PrimaryAction),
            "canyon physical probes drive the real Q/E and mouse-left InputMap actions");
        await AssertShieldLaneBehavior(tree);
        await AssertBellChainGateBehavior(tree);
        await AssertGripStoneLatchAndLanding(tree);
        await AssertGripStoneRejectsPhysicalMarginalLanding(tree);
        await AssertDenseTerrainWallLatch(tree);
        await AssertCeilingLatchBoostAndReel(tree);
        await AssertFallingSelfRescue(tree);
        await AssertAscentAnchorSightlineMatrix(tree);
        await AssertEveryCatchLayerReturns(tree);
        int mandatory = 0;
        foreach (SectorCase sectorCase in Sectors)
        {
            PrologueSector manifest = GD.Load<PackedScene>(sectorCase.Path)
                .Instantiate<PrologueSector>();
            TraversalSegment2D[] segments = manifest.GetNode("Gameplay/TraversalContracts")
                .GetChildren().OfType<TraversalSegment2D>().ToArray();
            foreach (TraversalSegment2D segment in segments)
            {
                mandatory++;
                if (segment.Kind == TraversalSegmentKind.Assisted)
                    await AssertOrdinaryJumpCannotReplaceClaw(tree, sectorCase, segment.SegmentId.ToString());
                foreach (float fraction in new[] { .2f, .5f, .8f })
                    await RunMandatorySample(tree, sectorCase, segment.SegmentId.ToString(), fraction);
                GD.Print($"TASK8_ROUTE_SEGMENT_OK sector={sectorCase.Name} segment={segment.SegmentId} " +
                    $"samples=20,50,80 dash=0 abilities={segment.RequiredAbilities}");
            }
            manifest.Free();
        }
        TestAssert.Equal(11, mandatory, "physical probe executes all eleven mandatory segments");
        TestAssert.Equal("attack|ice|shield", string.Join("|", CompletedLessonActions.Order()),
            "representative route samples perform the real attack, Ice rejection and shield inputs");
        await RunOptionalAndBossOnly(tree);
        ReleaseAll();
        GD.Print("TASK3_CANYON_TRAVERSAL_OK segments=11 samples_each=3 stable_frames=12 " +
            "margin=15% wall=True ceiling_boost=True reel=True self_rescue=True " +
            "catch_returns=4 second_air_jump=0 dash=0 optional_pass_skip=3 boss_trigger=physical");
    }

    public static async Task RunOptionalAndBossOnly(SceneTree tree)
    {
        ActiveTree = tree;
        await AssertOptionalPassAndSkip(tree, Sectors[2], "SupplyCave");
        await AssertOptionalPassAndSkip(tree, Sectors[1], "ArchiveAlcove");
        await AssertOptionalPassAndSkip(tree, Sectors[2], "SalvageCache");
        await AssertOptionalPassAndSkip(tree, Sectors[3], "HighRuin");
        await AssertTempleArenaLocksOnlyAfterTrigger(tree);
    }

    public static async Task RunCatchOneProbe(SceneTree tree)
    {
        ActiveTree = tree;
        ReleaseAll();
        GD.Print($"TASK3_CATCH_ONE_PHASE phase={CatchDirectionPhase()}");
        await AssertAscentAnchorSightlineMatrix(tree);
        await AssertCatchLayerReturns(tree,
            new CatchReturnCase(Sectors[3], "ascent_grapple_one", "CatchOne",
                "Gameplay/Collision/CatchReturnEdgeOne/GrappleSurfaceProfile",
                new Vector2(822, 1070), true));
        await AssertCatchLayerReturns(tree,
            new CatchReturnCase(Sectors[3], "ascent_grapple_two", "CatchTwo",
                "Gameplay/Collision/CatchReturnWallTwo/GrappleSurfaceProfile",
                new Vector2(1040, 790), true));
        await AssertCatchLayerReturns(tree,
            new CatchReturnCase(Sectors[3], "ascent_grapple_three", "CatchThree",
                "Gameplay/Collision/CatchReturnCeilingThree/GrappleSurfaceProfile",
                new Vector2(890, 618), true));
        ReleaseAll();
        GD.Print("TASK3_ASCENT_CATCH_PROBE_OK");
    }

    private static async Task RunMandatorySample(SceneTree tree, SectorCase sectorCase,
        string segmentId, float fraction)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = $"Task8_{sectorCase.Name}_{segmentId}_{fraction:0.0}" };
        PrologueSector sector = GD.Load<PackedScene>(sectorCase.Path).Instantiate<PrologueSector>();
        TraversalSegment2D segment = sector.GetNode<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{segmentId}");
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(
            Mathf.Lerp(segment.LaunchZone.Position.X, segment.LaunchZone.End.X, fraction),
            segment.LaunchZone.GetCenter().Y);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        RouteTrace trace = null;
        try
        {
            await WaitForFloor(tree, player);
            if (segmentId == "temple_checkpoint")
                await OpenBellGateForSegment(tree, player, sector);
            trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"),
                player.GetNode<MechanicalClawController>("MechanicalClaw"));
            if (segment.Kind == TraversalSegmentKind.Assisted)
            {
                GrappleSurface2D assist = sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
                MechanicalClawTargetResolution preflight =
                    new MechanicalClawTargetResolver().Resolve(player, assist.WorldAnchor);
                string blocker = preflight.Snapshot.HasLineOfSight
                    ? "clear"
                    : FirstRayHit(player, assist.WorldAnchor);
                TestAssert.True(preflight.Surface == assist && preflight.Snapshot.HasLineOfSight,
                    $"{segment.SegmentId} launch sample has a clear real ray to its exact dedicated " +
                    $"anchor; player={player.GlobalPosition} anchor={assist.WorldAnchor} " +
                    $"resolved={preflight.Surface?.Name ?? "None"} " +
                    $"visible={preflight.Snapshot.HasLineOfSight} blocker={blocker}");
                trace.ExpectedTarget = assist.GetInstanceId();
            }
            trace.Claw.ClawFrameChanged += trace.OnClawFrameChanged;
            bool teachingSample = Mathf.IsEqualApprox(fraction, .5f);
            if (teachingSample && segmentId == "entrance_move_attack")
                await AssertAttackLesson(tree, player);
            if (teachingSample && segmentId == "descent_ice_rejection")
                await AssertIceRejectionLesson(tree, player, sector, trace);
            bool shieldLesson = teachingSample && segmentId == "basin_shield_lane";
            if (shieldLesson)
                await BeginShieldLesson(tree, player);
            if (segment.Kind == TraversalSegmentKind.Assisted)
                await DriveAssisted(tree, player, sector, segment, trace);
            else
                await DriveSingleJump(tree, player, segment, trace, probeSecondJump: true);
            if (shieldLesson)
            {
                TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
                    "basin shield-lane representative crosses while real right-mouse guard is active");
                CompletedLessonActions.Add("shield");
                SendMouse(MouseButton.Right, false, player.GlobalPosition + Vector2.Right * 220f);
                await PhysicsFrames(tree, 2);
            }
            AssertLanding(player, segment, trace);
            GD.Print($"TASK8_ROUTE_SAMPLE_OK sector={sectorCase.Name} segment={segmentId} " +
                $"sample={fraction:0.0} frames={trace.Frames} casts={trace.CastCount} " +
                $"latches={trace.LatchCount} dash={trace.Motor.AcceptedDashCount} " +
                $"second_air_jump={trace.SecondAirJumpAccepted} position={player.Position}");
        }
        finally
        {
            ReleaseAll();
            if (trace != null && GodotObject.IsInstanceValid(trace.Claw))
                trace.Claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertAttackLesson(SceneTree tree, CharacterBody2D player)
    {
        PlayerActionModeController modes = player.GetNode<PlayerActionModeController>("ActionModes");
        PlayerCombat combat = player.GetNode<PlayerCombat>("Combat");
        try
        {
            MoveMouse(player.GlobalPosition + new Vector2(220, 0));
            SendAction(InputActions.SelectAttackMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectAttackMode, false);
            SendMouse(MouseButton.Left, true, player.GlobalPosition + new Vector2(220, 0));
            await PhysicsFrames(tree, 2);
            TestAssert.True(modes.CurrentMode == PlayerActionMode.Attack && combat.IsAttacking,
                "entrance representative drives real Q plus mouse-left into the melee model; " +
                $"mode={modes.CurrentMode} slot={modes.ActiveSlot} " +
                $"can_attack={modes.CanBeginAttack} attacking={combat.IsAttacking}");
            CompletedLessonActions.Add("attack");
        }
        finally
        {
            SendAction(InputActions.SelectAttackMode, false);
            SendMouse(MouseButton.Left, false, player.GlobalPosition + new Vector2(220, 0));
            combat.ResetTransientState();
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertIceRejectionLesson(SceneTree tree, CharacterBody2D player,
        PrologueSector sector, RouteTrace trace)
    {
        GrappleSurface2D ice = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/VisibleIceRejection");
        try
        {
            MoveMouse(ice.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, ice.WorldAnchor);
            for (int frame = 0; frame < 30 && trace.ObservedFailure != MechanicalClawFailure.IceSlip;
                 frame++)
            {
                MoveMouse(ice.WorldAnchor);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            TestAssert.Equal(MechanicalClawFailure.IceSlip, trace.ObservedFailure,
                "descent representative drives real E plus mouse-left into visible Ice rejection");
            TestAssert.Equal(0, trace.LatchCount, "Ice rejection cannot create a surface latch");
            CompletedLessonActions.Add("ice");
        }
        finally
        {
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, false, ice.WorldAnchor);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task BeginShieldLesson(SceneTree tree, CharacterBody2D player)
    {
        Vector2 aim = player.GlobalPosition + Vector2.Right * 220f;
        MoveMouse(aim);
        SendMouse(MouseButton.Right, true, aim);
        await PhysicsFrames(tree, 3);
        TestAssert.True(player.GetNode<PlayerShieldController>("Shield").IsActive,
            "basin representative turns on the real right-mouse shield before traversal");
    }

    private static async Task DriveSingleJump(SceneTree tree, CharacterBody2D player,
        TraversalSegment2D segment, RouteTrace trace, bool probeSecondJump)
    {
        float direction = Mathf.Sign(segment.LandingZone.GetCenter().X -
            segment.LaunchZone.GetCenter().X);
        StringName horizontal = direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        SendAction(horizontal, true);
        SendAction(InputActions.Jump, true);
        for (int frame = 0; frame < SingleJumpBudget; frame++)
        {
            trace.Frames = frame;
            if (frame == 8) SendAction(InputActions.Jump, false);
            if (probeSecondJump && frame == 11)
            {
                trace.SecondJumpVelocityBefore = player.Velocity.Y;
                trace.SecondJumpAcceptedCountBefore = trace.Motor.AcceptedJumpCount;
                trace.SecondJumpAttempted = true;
                SendAction(InputActions.Jump, true);
            }
            if (probeSecondJump && frame == 12)
            {
                trace.SecondAirJumpAccepted = trace.Motor.AcceptedJumpCount >
                    trace.SecondJumpAcceptedCountBefore;
                trace.SecondJumpMeasured = true;
                SendAction(InputActions.Jump, false);
            }
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            trace.Observe(player, segment);
            if (direction > 0 && player.Position.X >= segment.LandingZone.GetCenter().X ||
                direction < 0 && player.Position.X <= segment.LandingZone.GetCenter().X)
                SendAction(horizontal, false);
            if (trace.StableLandingFrames >= StableLandingRequirement &&
                HasLandingMargin(segment.LandingZone, player.Position)) break;
        }
        SendAction(InputActions.Jump, false);
        SendAction(horizontal, false);
    }

    private static async Task DriveAssisted(SceneTree tree, CharacterBody2D player,
        PrologueSector sector, TraversalSegment2D segment, RouteTrace trace,
        GrappleSurface2D targetOverride = null, bool boostImmediately = false,
        Vector2? aimOverride = null, bool useCurrentPositionForDirection = false)
    {
        GrappleSurface2D target = targetOverride ??
            sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
        Vector2 aimWorld = aimOverride ?? target.WorldAnchor;
        float direction = Mathf.Sign(segment.LandingZone.GetCenter().X -
            (useCurrentPositionForDirection ? player.Position.X : segment.LaunchZone.GetCenter().X));
        StringName horizontal = direction < 0 ? InputActions.MoveLeft : InputActions.MoveRight;
        StringName terrainBoostHorizontal = horizontal;
        bool terrainBoostDirectionSwitched = false;
        bool terrainBoostDirectionRestored = false;
        if (!boostImmediately)
        {
            SendAction(horizontal, true);
            SendAction(InputActions.MoveUp, true);
            SendAction(InputActions.Jump, true);
        }
        MoveMouse(aimWorld);
        SendAction(InputActions.SelectGrappleMode, true);
        await PhysicsFrames(tree, 1);
        SendAction(InputActions.SelectGrappleMode, false);
        GD.Print($"TASK3_ASSIST_CAST_DIRECT_RAY segment={segment.SegmentId} " +
            $"from={player.GlobalPosition} target={aimWorld} " +
            $"first_hit={FirstRayHit(player, aimWorld)}");
        SendMouse(MouseButton.Left, true, aimWorld);
        try
        {
            bool obstructionPrinted = false;
            int terrainReelFrames = 0;
            bool terrainReelComplete = false;
            bool terrainPayOutComplete = false;
            bool terrainBoostPhaseComplete = false;
            bool terrainTraversalStarted = false;
            bool terrainAttachmentLossPrinted = false;
            Vector2 terrainApex = player.Position;
            Vector2 terrainApexVelocity = player.Velocity;
            float terrainApexPredictedX = float.NaN;
            float terrainApexRope = 0f;
            Vector2 terrainLowest = player.Position;
            Vector2 terrainLowestVelocity = player.Velocity;
            float terrainLowestRope = 0f;
            int terrainLowestFrame = -1;
            float closestPredictedDistance = float.PositiveInfinity;
            float closestPredictedX = float.NaN;
            Vector2 closestPredictedPosition = player.Position;
            Vector2 closestPredictedVelocity = player.Velocity;
            float closestPredictedRope = 0f;
            int closestPredictedFrame = -1;
            int frameBudget = boostImmediately ? 420 : AssistedBudget;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                trace.Frames = frame;
                MoveMouse(aimWorld);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                trace.Observe(player, segment);
                if (boostImmediately && terrainTraversalStarted &&
                    !terrainAttachmentLossPrinted &&
                    !IsAttached(trace.Claw.CurrentFrame.State) &&
                    !trace.ReleasedFromLeftMouseRelease)
                {
                    terrainAttachmentLossPrinted = true;
                    GD.Print($"TASK3_CATCH_RETURN_UNPLANNED_DETACH segment={segment.SegmentId} " +
                        $"frame={frame} position={player.Position} velocity={player.Velocity} " +
                        $"state={trace.Claw.CurrentFrame.State} " +
                        $"failure={trace.Claw.CurrentFrame.Failure} " +
                        $"left_held={Input.IsMouseButtonPressed(MouseButton.Left)} " +
                        $"slide_hit={FirstSlideHit(player)}");
                }
                if (boostImmediately && player.Position.Y > terrainLowest.Y)
                {
                    terrainLowest = player.Position;
                    terrainLowestVelocity = player.Velocity;
                    terrainLowestRope = trace.Claw.CurrentFrame.RopeLength;
                    terrainLowestFrame = frame;
                }
                if (!obstructionPrinted && trace.ObservedFailure == MechanicalClawFailure.Obstructed)
                {
                    obstructionPrinted = true;
                    GD.Print($"TASK3_ASSIST_OBSTRUCTION_DIRECT_RAY segment={segment.SegmentId} " +
                        $"frame={frame} cast={trace.Claw.CurrentFrame.CastId} " +
                        $"state={trace.Claw.CurrentFrame.State} from={player.GlobalPosition} " +
                        $"locked_anchor={trace.Claw.CurrentFrame.AnchorPoint} " +
                        $"first_hit={FirstRayHit(player, trace.Claw.CurrentFrame.AnchorPoint)}");
                }
                if (boostImmediately && !terrainTraversalStarted &&
                    IsAttached(trace.Claw.CurrentFrame.State))
                {
                    terrainTraversalStarted = true;
                    SendAction(horizontal, true);
                }
                if (frame == 8) SendAction(InputActions.Jump, false);
                if (trace.SecondJumpAttempted && !trace.SecondJumpMeasured)
                {
                    trace.SecondAirJumpAccepted = trace.Motor.AcceptedJumpCount >
                        trace.SecondJumpAcceptedCountBefore;
                    trace.SecondJumpMeasured = true;
                    SendAction(InputActions.Jump, false);
                    await PhysicsFrames(tree, 1);
                    trace.CompleteAttachedSecondSpaceCausality();
                    if (boostImmediately)
                        GD.Print($"TASK3_CATCH_RETURN_BOOST_FRAME segment={segment.SegmentId} " +
                            $"position={player.Position} velocity={player.Velocity} " +
                            $"input={terrainBoostHorizontal} state={trace.Claw.CurrentFrame.State} " +
                            $"correction={trace.Claw.CurrentFrame.VelocityCorrection} " +
                            $"rope={trace.Claw.CurrentFrame.RopeLength:0.00} " +
                            $"slide_hit={FirstSlideHit(player)}");
                    if (!boostImmediately)
                    {
                        SendMouse(MouseButton.Left, false, aimWorld);
                        await PhysicsFrames(tree, 1);
                        trace.CompleteExplicitLeftReleaseCausality();
                    }
                }
                if (boostImmediately && trace.SecondJumpMeasured && !terrainBoostPhaseComplete &&
                    ++terrainReelFrames >= 12)
                {
                    terrainBoostPhaseComplete = true;
                    if (terrainBoostDirectionSwitched && !terrainBoostDirectionRestored)
                    {
                        SendAction(terrainBoostHorizontal, false);
                        SendAction(horizontal, true);
                    }
                    GD.Print($"TASK3_CATCH_RETURN_SWING_START segment={segment.SegmentId} " +
                        $"position={player.Position} rope={trace.Claw.CurrentFrame.RopeLength:0.00}");
                }
                if (boostImmediately && trace.SecondJumpMeasured &&
                    terrainBoostDirectionSwitched && !terrainBoostDirectionRestored &&
                    terrainReelFrames >= CatchDirectionPhase())
                {
                    SendAction(terrainBoostHorizontal, false);
                    SendAction(horizontal, true);
                    terrainBoostDirectionRestored = true;
                    GD.Print($"TASK3_CATCH_RETURN_DIRECTION_RESTORE segment={segment.SegmentId} " +
                        $"frame={frame} position={player.Position} velocity={player.Velocity} " +
                        $"rope={trace.Claw.CurrentFrame.RopeLength:0.00}");
                }
                // The collision body settles a few pixels above the authored player-center
                // line while its rope is taut.  Start the real S pay-out in this explicit
                // approach window, then still require the full 12-frame 15%-inset landing.
                if (boostImmediately && terrainBoostPhaseComplete && !terrainReelComplete &&
                    player.Position.Y <= segment.LandingZone.GetCenter().Y + LandingApproachTolerance)
                {
                    terrainReelComplete = true;
                    SendAction(InputActions.MoveUp, false);
                    SendAction(InputActions.MoveDown, true);
                    GD.Print($"TASK3_CATCH_RETURN_REEL_COMPLETE segment={segment.SegmentId} " +
                        $"frame={frame} position={player.Position} " +
                        $"rope={trace.Claw.CurrentFrame.RopeLength:0.00} " +
                        $"slide_hit={FirstSlideHit(player)} " +
                        $"required={RequiredRopeForLanding(trace.Claw.CurrentFrame.AnchorPoint, segment.LandingZone):0.00}");
                }
                if (boostImmediately && terrainReelComplete && !terrainPayOutComplete &&
                    trace.Claw.CurrentFrame.RopeLength >=
                        RequiredRopeForLanding(trace.Claw.CurrentFrame.AnchorPoint,
                            segment.LandingZone) + 24f)
                {
                    terrainPayOutComplete = true;
                    SendAction(InputActions.MoveDown, false);
                    GD.Print($"TASK3_CATCH_RETURN_PAY_OUT_COMPLETE segment={segment.SegmentId} " +
                        $"frame={frame} position={player.Position} velocity={player.Velocity} " +
                        $"rope={trace.Claw.CurrentFrame.RopeLength:0.00} " +
                        $"slide_hit={FirstSlideHit(player)}");
                }
                if (boostImmediately && terrainPayOutComplete && frame % 20 == 0)
                {
                    PlayerInputFrame sampled = player.GetNode<PlayerInputReader>("Input").CurrentFrame;
                    GD.Print($"TASK3_CATCH_RETURN_POST_PAY_OUT segment={segment.SegmentId} " +
                        $"frame={frame} position={player.Position} velocity={player.Velocity} " +
                        $"correction={trace.Claw.CurrentFrame.VelocityCorrection} " +
                        $"rope={trace.Claw.CurrentFrame.RopeLength:0.00} axis={sampled.MoveAxis:0.00} " +
                        $"floor={player.IsOnFloor()} slide_hit={FirstSlideHit(player)}");
                }
                bool landingReleaseWindow = false;
                float predictedLandingX = PredictLandingX(player, trace.Motor,
                    segment.LandingZone.GetCenter().Y, direction);
                float safeLandingStart = segment.LandingZone.Position.X +
                    segment.LandingZone.Size.X * LandingMarginRatio;
                float safeLandingEnd = segment.LandingZone.End.X -
                    segment.LandingZone.Size.X * LandingMarginRatio;
                if (boostImmediately && trace.SecondJumpMeasured &&
                    !float.IsNaN(predictedLandingX))
                {
                    float distance = predictedLandingX < safeLandingStart ?
                        safeLandingStart - predictedLandingX :
                        predictedLandingX > safeLandingEnd ?
                            predictedLandingX - safeLandingEnd : 0f;
                    if (distance < closestPredictedDistance)
                    {
                        closestPredictedDistance = distance;
                        closestPredictedX = predictedLandingX;
                        closestPredictedPosition = player.Position;
                        closestPredictedVelocity = player.Velocity;
                        closestPredictedRope = trace.Claw.CurrentFrame.RopeLength;
                        closestPredictedFrame = frame;
                    }
                }
                // Forecast normally opens the release window before contact.  A taut ceiling
                // return can instead reach a real in-zone floor contact first; that is an
                // equally valid, observable release window and is still gated by the final
                // twelve-frame inset landing assertion.
                bool physicalLandingWindow = player.IsOnFloor() &&
                    HasLandingMargin(segment.LandingZone, player.Position);
                landingReleaseWindow = physicalLandingWindow ||
                    predictedLandingX >= segment.LandingZone.Position.X &&
                    predictedLandingX <= segment.LandingZone.End.X;
                if (boostImmediately && player.Position.Y < terrainApex.Y)
                {
                    terrainApex = player.Position;
                    terrainApexVelocity = player.Velocity;
                    terrainApexPredictedX = PredictLandingX(player, trace.Motor,
                        segment.LandingZone.GetCenter().Y, direction);
                    terrainApexRope = trace.Claw.CurrentFrame.RopeLength;
                }
                if (!trace.SecondJumpAttempted && trace.TryBeginAttachedSecondSpace(player, target) &&
                    (boostImmediately || landingReleaseWindow))
                {
                    TestAssert.True(!player.IsOnFloor(),
                        $"{segment.SegmentId} is airborne immediately before its real second Space edge");
                    trace.SecondJumpAttempted = true;
                    trace.SecondJumpVelocityBefore = player.Velocity.Y;
                    trace.SecondJumpAcceptedCountBefore = trace.Motor.AcceptedJumpCount;
                    if (boostImmediately)
                    {
                        Vector2 toAnchor = player.GlobalPosition.DirectionTo(
                            trace.Claw.CurrentFrame.AnchorPoint);
                        Vector2 tangent = new(-toAnchor.Y, toAnchor.X);
                        terrainBoostHorizontal = tangent.Y < 0f
                            ? InputActions.MoveRight
                            : InputActions.MoveLeft;
                        terrainBoostDirectionSwitched = terrainBoostHorizontal != horizontal;
                        if (terrainBoostDirectionSwitched)
                        {
                            SendAction(horizontal, false);
                            SendAction(terrainBoostHorizontal, true);
                        }
                    }
                    SendAction(InputActions.Jump, true);
                    trace.RecordSecondSpaceInputDispatched();
                    SendAction(InputActions.MoveUp, boostImmediately);
                }
                if (boostImmediately && trace.SecondJumpMeasured &&
                    terrainPayOutComplete && !trace.ReleasedFromLeftMouseRelease &&
                    IsAttached(trace.Claw.CurrentFrame.State) && landingReleaseWindow)
                {
                    SendAction(InputActions.MoveUp, false);
                    SendMouse(MouseButton.Left, false, aimWorld);
                    await PhysicsFrames(tree, 1);
                    trace.CompleteExplicitLeftReleaseCausality();
                    GD.Print($"TASK3_CATCH_RETURN_RELEASE segment={segment.SegmentId} " +
                        $"position={player.Position} velocity={player.Velocity}");
                }
                if (direction > 0 && player.Position.X >= segment.LandingZone.GetCenter().X ||
                    direction < 0 && player.Position.X <= segment.LandingZone.GetCenter().X)
                    SendAction(horizontal, false);
                if (trace.StableLandingFrames >= StableLandingRequirement &&
                    HasLandingMargin(segment.LandingZone, player.Position)) break;
            }
            if (boostImmediately)
            {
                GD.Print($"TASK3_CATCH_RETURN_APEX segment={segment.SegmentId} " +
                    $"position={terrainApex} velocity={terrainApexVelocity} " +
                    $"predicted_x={terrainApexPredictedX:0.00} rope={terrainApexRope:0.00}");
                GD.Print($"TASK3_CATCH_RETURN_CLOSEST_WINDOW segment={segment.SegmentId} " +
                    $"frame={closestPredictedFrame} position={closestPredictedPosition} " +
                    $"velocity={closestPredictedVelocity} predicted_x={closestPredictedX:0.00} " +
                    $"safe_distance={closestPredictedDistance:0.00} " +
                    $"rope={closestPredictedRope:0.00}");
                GD.Print($"TASK3_CATCH_RETURN_LOWEST segment={segment.SegmentId} " +
                    $"frame={terrainLowestFrame} position={terrainLowest} " +
                    $"velocity={terrainLowestVelocity} rope={terrainLowestRope:0.00}");
            }
        }
        finally
        {
            SendAction(InputActions.Jump, false);
            SendMouse(MouseButton.Left, false, aimWorld);
            SendAction(InputActions.SelectGrappleMode, false);
            SendAction(InputActions.MoveUp, false);
            SendAction(InputActions.MoveDown, false);
            SendAction(horizontal, false);
            if (boostImmediately) SendAction(terrainBoostHorizontal, false);
        }
    }

    private static void AssertLanding(CharacterBody2D player, TraversalSegment2D segment,
        RouteTrace trace)
    {
        TestAssert.True(trace.LeftFloor, $"{segment.SegmentId} leaves its real launch support");
        TestAssert.True(player.IsOnFloor() &&
            trace.StableLandingFrames >= StableLandingRequirement &&
            HasLandingMargin(segment.LandingZone, player.Position),
            $"{segment.SegmentId} lands for twelve stable frames with at least 15% horizontal " +
            "and vertical margin inside the authored safe zone; " +
            $"position={player.Position} frames={trace.Frames} casts={trace.CastCount} " +
            $"latches={trace.LatchCount} attached={trace.HasAttached} " +
            $"expected_target={trace.ExpectedTarget} actual_target={trace.LatchedTarget} " +
            $"latched_anchor={trace.LatchedAnchor} second_attempted={trace.SecondJumpAttempted} " +
            $"boost={trace.BoostFromSecondSpaceObserved} " +
            $"claw_state={trace.Claw.CurrentFrame.State} " +
            $"claw_failure={trace.Claw.CurrentFrame.Failure} observed_failure={trace.ObservedFailure} " +
            $"mode={player.GetNode<PlayerActionModeController>("ActionModes").CurrentMode}");
        TestAssert.Equal(0, trace.Motor.AcceptedDashCount,
            $"{segment.SegmentId} accepts no Dash on the mandatory route");
        TestAssert.True(!trace.SecondAirJumpAccepted,
            $"{segment.SegmentId} rejects a second Space press while airborne");
        TestAssert.True(trace.SecondJumpAttempted && trace.SecondJumpMeasured,
            $"{segment.SegmentId} performs and measures a real InputMap second-air-jump attempt");
        if (segment.Kind == TraversalSegmentKind.Assisted)
        {
            TestAssert.True(trace.AttachedSecondSpaceCausalityObserved,
                $"{segment.SegmentId} proves the current expected cast is attached at the " +
                "second-Space edge, boosts without releasing while left remains held, and " +
                "does not create an ordinary motor jump");
            TestAssert.True(trace.ReleasedFromLeftMouseRelease,
                $"{segment.SegmentId} releases only after the explicit LMB release event");
            GD.Print($"TASK8_ATTACHED_SECOND_SPACE_OK segment={segment.SegmentId} " +
                $"airborne_edge={trace.AttachedSecondSpaceAirborneAtEdge} " +
                $"boost={trace.BoostFromSecondSpaceObserved} " +
                $"same_cast_attached={trace.AttachedAfterSecondSpaceWhileLeftHeld} " +
                $"released_while_left={trace.ReleasedFromSecondSpaceWhileLeftHeld} " +
                $"lmb_release={trace.ReleasedFromLeftMouseRelease}");
            TestAssert.Equal(1, trace.CastCount,
                $"{segment.SegmentId} starts one real mechanical-claw cast");
            TestAssert.Equal(1, trace.LatchCount,
                $"{segment.SegmentId} reaches one real surface latch");
            TestAssert.Equal(trace.ExpectedTarget, trace.LatchedTarget,
                $"{segment.SegmentId} latches its exact authored assist surface");
        }
        else if (segment.SegmentId.ToString() == "descent_ice_rejection")
        {
            if (trace.ObservedFailure == MechanicalClawFailure.IceSlip)
            {
                TestAssert.Equal(0, trace.CastCount,
                    "descent Ice rejection fails before allocating a mechanical-claw cast id");
                TestAssert.Equal(0, trace.LatchCount,
                    "descent Ice lesson rejects rather than latching");
            }
            else
            {
                TestAssert.Equal(0, trace.CastCount,
                    "non-representative Ice route samples remain pure jump probes");
            }
        }
        else
        {
            TestAssert.Equal(0, trace.CastCount,
                $"{segment.SegmentId} jump route does not hide a claw cast");
        }
    }

    // Break caught: rendering a grippable wall while leaving its collision untyped makes the
    // real E + LMB terrain ray miss or lock the decorative center instead of the hit face.
    private static async Task AssertDenseTerrainWallLatch(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task3DenseTerrainWallLatch" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[1].Path).Instantiate<PrologueSector>();
        StaticBody2D wall = sector.GetNode<StaticBody2D>("Gameplay/Collision/CraneShaftWall");
        GrappleSurface2D profile = wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(170f, 574f);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
            { ExpectedTarget = profile.GetInstanceId() };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        Vector2 cursor = wall.Position + new Vector2(30f, 30f);
        RectangleShape2D wallShape = wall.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("wall must be rectangular");
        float expectedFaceX = wall.Position.X - wallShape.Size.X / 2f;
        try
        {
            await WaitForFloor(tree, player);
            MoveMouse(cursor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, cursor);
            for (int frame = 0; frame < 120 && !trace.HasAttached; frame++)
            {
                MoveMouse(cursor);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == profile.GetInstanceId(),
                "real E + LMB latches the typed CraneShaft wall collision profile");
            TestAssert.True(claw.CurrentFrame.AnchorPoint.DistanceTo(profile.WorldAnchor) > 8f,
                "wall latch preserves the physical hit point instead of snapping to profile center");
            TestAssert.Near(expectedFaceX, claw.CurrentFrame.AnchorPoint.X, 2f,
                "wall latch stays on the first physical collision face rather than the cursor");
            TestAssert.True(claw.CurrentFrame.AnchorPoint.DistanceTo(cursor) > 8f,
                $"wall collision ends the ray before the cursor; cursor={cursor} " +
                $"actual={claw.CurrentFrame.AnchorPoint}");
            float ropeBefore = claw.CurrentFrame.RopeLength;
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.MoveUp, true);
            for (int frame = 0; frame < 24; frame++)
                await PhysicsFrames(tree, 1);
            SendAction(InputActions.MoveRight, false);
            SendAction(InputActions.MoveUp, false);
            TestAssert.True(trace.HasAttached && claw.CurrentFrame.State == MechanicalClawState.Reel &&
                claw.CurrentFrame.RopeLength < ropeBefore,
                "wall latch responds to real D/W input with attached reel behavior");
        }
        finally
        {
            SendMouse(MouseButton.Left, false, cursor);
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    // Break caught: a ceiling profile that cannot retain the cast through W/S reel and attached
    // Space would silently restore the obsolete Space-detaches behavior.
    private static async Task AssertCeilingLatchBoostAndReel(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task3CeilingBoostReel" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[1].Path).Instantiate<PrologueSector>();
        StaticBody2D ceiling = sector.GetNode<StaticBody2D>("Gameplay/Collision/FrostVentCeiling");
        GrappleSurface2D profile = ceiling.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(720f, 704f);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        var states = new List<MechanicalClawFrame>();
        claw.ClawFrameChanged += Observe;
        Vector2 hit = ceiling.Position + new Vector2(-40f, -40f);
        try
        {
            await WaitForFloor(tree, player);
            MoveMouse(hit);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, hit);
            for (int frame = 0; frame < 120 && !IsAttached(claw.CurrentFrame.State); frame++)
            {
                MoveMouse(hit);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(IsAttached(claw.CurrentFrame.State) && claw.CurrentTarget == profile,
                $"real ceiling underside ray latches its typed collision profile; " +
                $"state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} " +
                $"anchor={claw.CurrentFrame.AnchorPoint} player={player.GlobalPosition} " +
                $"cursor={hit} target={claw.CurrentTarget?.GetParent()?.Name ?? "None"}");
            ulong cast = claw.CurrentFrame.CastId;
            float ropeBefore = claw.CurrentFrame.RopeLength;
            SendAction(InputActions.MoveUp, true);
            await PhysicsFrames(tree, 8);
            SendAction(InputActions.MoveUp, false);
            float reeledIn = claw.CurrentFrame.RopeLength;
            SendAction(InputActions.MoveDown, true);
            await PhysicsFrames(tree, 8);
            SendAction(InputActions.MoveDown, false);
            float reeledOut = claw.CurrentFrame.RopeLength;
            TestAssert.True(reeledIn < ropeBefore && reeledOut > reeledIn &&
                states.Any(frame => frame.State == MechanicalClawState.Reel),
                $"real W/S input reels the ceiling rope in and out; " +
                $"before={ropeBefore:0.00} in={reeledIn:0.00} out={reeledOut:0.00}");

            SendAction(InputActions.MoveLeft, true);
            await PhysicsFrames(tree, 3);
            SendAction(InputActions.MoveLeft, false);
            SendAction(InputActions.MoveRight, true);
            int jumpsBefore = motor.AcceptedJumpCount;
            float velocityBefore = player.Velocity.Y;
            SendAction(InputActions.Jump, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.Jump, false);
            await PhysicsFrames(tree, 2);
            SendAction(InputActions.MoveRight, false);
            TestAssert.True(states.Any(frame => frame.State == MechanicalClawState.Boost &&
                    frame.CastId == cast) && IsAttached(claw.CurrentFrame.State) &&
                    claw.CurrentFrame.CastId == cast,
                "attached Space publishes Boost and retains the exact ceiling cast while LMB is held");
            TestAssert.Equal(jumpsBefore, motor.AcceptedJumpCount,
                "attached ceiling Space does not consume an ordinary motor jump");
            TestAssert.True(player.Velocity.Y < velocityBefore,
                $"attached ceiling Space adds upward velocity; " +
                $"before={velocityBefore:0.00} after={player.Velocity.Y:0.00}");
            TestAssert.Equal(0, motor.AcceptedDashCount,
                "ceiling boost/reel probe uses zero Dash");
        }
        finally
        {
            SendMouse(MouseButton.Left, false, hit);
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= Observe;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }

        void Observe(MechanicalClawFrame frame, GrappleSurface2D _) => states.Add(frame);
    }

    // Break caught: a fall-recovery surface that is visually present but unreachable through the
    // real claw input cannot save the player onto its nearby authored catch layer.
    private static async Task AssertFallingSelfRescue(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task3FallingSelfRescue" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[1].Path).Instantiate<PrologueSector>();
        StaticBody2D wall = sector.GetNode<StaticBody2D>("Gameplay/Collision/SelfRescueWall");
        StaticBody2D catchLayer = sector.GetNode<StaticBody2D>("Gameplay/Collision/SelfRescueCatch");
        GrappleSurface2D profile = wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(1000f, 400f);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        ActiveInputCanvas = player;
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerInputReader input = player.GetNode<PlayerInputReader>("Input");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
            { ExpectedTarget = profile.GetInstanceId() };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        Vector2 hit = wall.Position;
        try
        {
            await PhysicsFrames(tree, 2);
            TestAssert.True(!player.IsOnFloor(),
                "self-rescue begins from a genuinely falling body without a position reset after mount");
            MechanicalClawTargetResolution preflight =
                new MechanicalClawTargetResolver().Resolve(player, hit);
            TestAssert.True(preflight.Surface == profile,
                $"self-rescue cursor resolves the intended wall before casting; " +
                $"surface={preflight.Surface?.GetParent()?.Name ?? "None"} " +
                $"exists={preflight.Snapshot.Exists} anchor={preflight.Snapshot.WorldAnchor} " +
                $"position={player.Position} cursor={hit}");
            MoveMouse(hit);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            TestAssert.True(input.CurrentFrame.MouseWorld.DistanceTo(hit) <= 1f,
                $"self-rescue real input samples the intended wall cursor; " +
                $"sampled={input.CurrentFrame.MouseWorld} intended={hit}");
            GD.Print($"TASK3_SELF_RESCUE_INPUT_VIEWPORT_OK sampled={input.CurrentFrame.MouseWorld} " +
                $"intended={hit} events={ViewportInputTransport}");
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, hit);
            for (int frame = 0; frame < 90 && !trace.HasAttached; frame++)
            {
                MoveMouse(hit);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == trace.ExpectedTarget &&
                claw.CurrentTarget == profile && !player.IsOnFloor(),
                $"falling player reaches a real typed-terrain latch before touching a catch layer; " +
                $"state={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} " +
                $"observed_failure={trace.ObservedFailure} casts={trace.CastCount} " +
                $"latches={trace.LatchCount} position={player.Position} " +
                $"target={claw.CurrentTarget?.GetParent()?.Name ?? "None"}");
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.MoveUp, true);
            await PhysicsFrames(tree, 18);
            SendAction(InputActions.MoveRight, false);
            SendAction(InputActions.MoveUp, false);
            SendMouse(MouseButton.Left, false, hit);
            int stable = 0;
            for (int frame = 0; frame < 240 && stable < StableLandingRequirement; frame++)
            {
                await PhysicsFrames(tree, 1);
                stable = player.IsOnFloor() && IsStandingOn(player, catchLayer) ? stable + 1 : 0;
            }
            TestAssert.True(stable >= StableLandingRequirement,
                $"falling self-rescue returns to the nearby named catch layer for twelve frames; " +
                $"position={player.Position}");
            TestAssert.Equal(0, trace.Motor.AcceptedDashCount,
                "falling self-rescue uses zero Dash");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertEveryCatchLayerReturns(SceneTree tree)
    {
        CatchReturnCase[] cases =
        [
            new(Sectors[1], "descent_grip_lesson", "GripStoneCatchZone",
                "Gameplay/GrappleSurfaces/DedicatedLessonAnchor", new Vector2(470, 330), false),
            new(Sectors[3], "ascent_grapple_one", "CatchOne",
                "Gameplay/Collision/CatchReturnEdgeOne/GrappleSurfaceProfile",
                new Vector2(822, 1070), true),
            new(Sectors[3], "ascent_grapple_two", "CatchTwo",
                "Gameplay/Collision/CatchReturnWallTwo/GrappleSurfaceProfile",
                new Vector2(1040, 790), true),
            new(Sectors[3], "ascent_grapple_three", "CatchThree",
                "Gameplay/Collision/CatchReturnCeilingThree/GrappleSurfaceProfile",
                new Vector2(890, 618), true)
        ];
        foreach (CatchReturnCase item in cases)
            await AssertCatchLayerReturns(tree, item);
    }

    private static async Task AssertAscentAnchorSightlineMatrix(SceneTree tree)
    {
        string[] segmentIds =
        [
            "ascent_grapple_one",
            "ascent_grapple_two",
            "ascent_grapple_three"
        ];
        var failures = new List<string>();
        foreach (string segmentId in segmentIds)
        {
            foreach (float fraction in new[] { .2f, .5f, .8f })
            {
                ReleaseAll();
                var fixture = new Node2D { Name = $"Task3AscentSightline_{segmentId}_{fraction:0.0}" };
                PrologueSector sector = GD.Load<PackedScene>(Sectors[3].Path)
                    .Instantiate<PrologueSector>();
                TraversalSegment2D segment = sector.GetNode<TraversalSegment2D>(
                    $"Gameplay/TraversalContracts/{segmentId}");
                GrappleSurface2D target = sector.GetNode<GrappleSurface2D>(segment.AssistTargetPath);
                CharacterBody2D player = NewPlayer();
                player.Position = new Vector2(
                    segment.LaunchZone.Position.X + segment.LaunchZone.Size.X * fraction,
                    segment.LaunchZone.GetCenter().Y);
                fixture.AddChild(sector);
                fixture.AddChild(player);
                tree.Root.AddChild(fixture);
                try
                {
                    await WaitForFloor(tree, player);
                    var resolver = new MechanicalClawTargetResolver();
                    MechanicalClawTargetResolution resolution = resolver.Resolve(
                        player, target.WorldAnchor);
                    bool insideLaunch = segment.LaunchZone.HasPoint(player.Position);
                    bool exact = resolution.Surface == target;
                    bool visible = resolution.Snapshot.HasLineOfSight;
                    string firstHit = FirstRayHit(player, target.WorldAnchor);
                    GD.Print($"TASK3_ASCENT_SIGHTLINE_CELL segment={segmentId} " +
                        $"sample={fraction:0.0} player={player.Position} target={target.WorldAnchor} " +
                        $"inside_launch={insideLaunch} exact={exact} visible={visible} " +
                        $"first_hit={firstHit}");
                    if (!insideLaunch || !exact || !visible)
                        failures.Add($"{segmentId}/{fraction:0.0}: player={player.Position} " +
                            $"inside={insideLaunch} exact={exact} visible={visible} hit={firstHit}");
                }
                finally
                {
                    ReleaseAll();
                    fixture.QueueFree();
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    await PhysicsFrames(tree, 2);
                }
            }
        }
        TestAssert.Equal(0, failures.Count,
            $"all three ascent anchors remain clear from all three real launch samples: " +
            string.Join(" | ", failures));
        GD.Print("TASK3_ASCENT_SIGHTLINE_MATRIX_OK segments=3 samples_each=3 exact_targets=True");
    }

    // Break caught: a catch shelf that catches the player but offers no real-input path back to the
    // matching grapple landing converts a recoverable miss into a soft lock.
    private static async Task AssertCatchLayerReturns(SceneTree tree, CatchReturnCase expected)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = $"Task3CatchReturn{expected.CatchName}" };
        PrologueSector sector = GD.Load<PackedScene>(expected.Sector.Path).Instantiate<PrologueSector>();
        TraversalSegment2D segment = sector.GetNode<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{expected.SegmentId}");
        StaticBody2D catchLayer = sector.GetNode<StaticBody2D>(
            $"Gameplay/Collision/{expected.CatchName}");
        RectangleShape2D shape = catchLayer.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("catch layer must be rectangular");
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(catchLayer.Position.X,
            catchLayer.Position.Y - shape.Size.Y / 2f - PlayerCollisionHalfHeight);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D retryTarget = sector.GetNode<GrappleSurface2D>(expected.TargetPath);
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = retryTarget.GetInstanceId()
        };
        KillZone[] killZones = sector.GetNode("Gameplay/FallRecovery").GetChildren()
            .OfType<KillZone>().ToArray();
        int killZoneEntries = 0;
        foreach (KillZone killZone in killZones) killZone.BodyEntered += OnKillZoneEntered;
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            TestAssert.True(IsStandingOn(player, catchLayer),
                $"{expected.CatchName} retry begins on its exact physical catch layer");
            TestAssert.True(retryTarget.UseCollisionHitPoint == expected.TerrainReturn,
                $"{expected.CatchName} retry target uses the authored point/terrain profile kind");
            Vector2 retryAim = sector.ToGlobal(expected.AimLocal);
            var resolver = new MechanicalClawTargetResolver();
            MechanicalClawTargetResolution preflight = resolver.Resolve(player, retryAim);
            TestAssert.True(preflight.Surface == retryTarget && preflight.Snapshot.HasLineOfSight,
                $"{expected.CatchName} retry aim resolves its exact typed return surface; " +
                $"aim={retryAim} first_hit={FirstRayHit(player, retryAim)}");
            if (expected.CatchName == "CatchThree")
                AssertCatchThreeRayNotch(sector, player, segment, retryAim, retryTarget, preflight);
            if (expected.TerrainReturn)
            {
                float hitDistance = player.GlobalPosition.DistanceTo(preflight.Snapshot.WorldAnchor);
                TestAssert.True(hitDistance >= 360f && hitDistance <= 520f,
                    $"{expected.CatchName} typed return face stays inside [360,520]; " +
                    $"actual={hitDistance:0.00} hit={preflight.Snapshot.WorldAnchor}");
            }
            GD.Print($"TASK3_CATCH_RETURN_DIRECT_RAY catch={expected.CatchName} " +
                $"segment={expected.SegmentId} from={player.GlobalPosition} " +
                $"aim={retryAim} hit={preflight.Snapshot.WorldAnchor} " +
                $"first_hit={FirstRayHit(player, retryAim)}");
            await DriveAssisted(tree, player, sector, segment, trace,
                retryTarget, boostImmediately: expected.TerrainReturn, aimOverride: retryAim,
                useCurrentPositionForDirection: expected.TerrainReturn);
            AssertLanding(player, segment, trace);
            if (expected.TerrainReturn)
                TestAssert.True(trace.LatchedAnchor.DistanceTo(retryTarget.WorldAnchor) > 8f,
                    $"{expected.CatchName} retry latches the real collision face rather than " +
                    $"the return body's decorative center; center={retryTarget.WorldAnchor} " +
                    $"hit={trace.LatchedAnchor}");
            TestAssert.Equal(0, trace.Motor.AcceptedDashCount,
                $"{expected.CatchName} return uses no Dash");
            TestAssert.Equal(0, killZoneEntries,
                $"{expected.CatchName} return enters zero KillZone recovery volumes");
            GD.Print($"TASK3_CATCH_RETURN_OK catch={expected.CatchName} " +
                $"segment={expected.SegmentId} position={player.Position} stable=12 margin=15% " +
                "killzone_entries=0");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            foreach (KillZone killZone in killZones)
                if (GodotObject.IsInstanceValid(killZone)) killZone.BodyEntered -= OnKillZoneEntered;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }

        void OnKillZoneEntered(Node2D entered)
        {
            if (entered == player) killZoneEntries++;
        }
    }

    private static void AssertCatchThreeRayNotch(PrologueSector sector, CharacterBody2D player,
        TraversalSegment2D segment, Vector2 retryAim, GrappleSurface2D retryTarget,
        MechanicalClawTargetResolution preflight)
    {
        const float MinimumClearance = 12f;
        StaticBody2D ceiling = retryTarget.GetParent() as StaticBody2D ??
            throw new InvalidOperationException("CatchThree return must be typed terrain");
        RectangleShape2D ceilingShape = ceiling.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("CatchThree ceiling must be rectangular");
        StaticBody2D landing = sector.GetNode<StaticBody2D>("Gameplay/Collision/LandingThree");
        RectangleShape2D landingShape = landing.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("LandingThree must be rectangular");
        GrappleSurface2D anchorTwo = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/AscentAnchorTwo");
        CircleShape2D anchorTwoShape = anchorTwo.GetNode<Area2D>("TargetCollision")
            .GetNode<CollisionShape2D>("Collision").Shape as CircleShape2D ??
            throw new InvalidOperationException("AnchorTwo must retain its point-target circle");
        GrappleSurface2D anchorThree = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/AscentAnchorThree");
        Area2D anchorThreeCollision = anchorThree.GetNode<Area2D>("TargetCollision");

        Vector2 ceilingHit = preflight.Snapshot.WorldAnchor;
        float hitDistance = player.GlobalPosition.DistanceTo(ceilingHit);
        TestAssert.Near(882f, ceilingHit.X, 3f,
            "CatchThree retry hits the authored ceiling underside near x=882 rather than an anchor");
        TestAssert.Near(630f, ceilingHit.Y, 2f,
            "CatchThree retry hits the authored ceiling underside at y=630");
        TestAssert.True(hitDistance >= 360f && hitDistance <= 520f,
            $"CatchThree ceiling hit stays inside [360,520]; actual={hitDistance:0.00}");

        float anchorTwoClearance = DistanceToSegment(anchorTwo.GlobalPosition,
            player.GlobalPosition, retryAim) - anchorTwoShape.Radius;
        float landingTop = landing.GlobalPosition.Y - landingShape.Size.Y / 2f;
        float landingRayX = RayXAtY(player.GlobalPosition, retryAim, landingTop);
        float landingStart = landing.GlobalPosition.X - landingShape.Size.X / 2f;
        Vector2 anchorThreeLaunch = new(
            segment.LaunchZone.Position.X + segment.LaunchZone.Size.X * .8f,
            segment.LaunchZone.GetCenter().Y);
        RayHit? anchorThreeHit = FirstRayHitDetail(player, anchorThreeLaunch,
            anchorThree.GlobalPosition);
        float ceilingLeft = ceiling.GlobalPosition.X - ceilingShape.Size.X / 2f;
        float ceilingUnderside = ceiling.GlobalPosition.Y + ceilingShape.Size.Y / 2f;
        float verticalHeadroom = landingTop - ceilingUnderside - PlayerCollisionHalfHeight * 2f;

        TestAssert.True(anchorTwoClearance >= MinimumClearance,
            $"CatchThree return line clears AnchorTwo's target circle by at least {MinimumClearance:0}px; " +
            $"actual={anchorTwoClearance:0.00}");
        TestAssert.True(landingStart - landingRayX >= MinimumClearance,
            $"CatchThree return line crosses LandingThree's height through a {MinimumClearance:0}px-plus " +
            $"notch; ray_x={landingRayX:0.00} landing_start={landingStart:0.00}");
        TestAssert.True(verticalHeadroom >= MinimumClearance,
            $"CatchThree ceiling and landing retain at least {MinimumClearance:0}px of physical " +
            $"headroom for the player; actual={verticalHeadroom:0.00}");
        TestAssert.True(anchorThreeHit.HasValue && anchorThreeHit.Value.Collider == anchorThreeCollision,
            "all three main-anchor sightline samples retain the physical AnchorThree target before the ceiling");
        TestAssert.True(ceilingLeft - anchorThreeHit.Value.Position.X >= 32f,
            $"AnchorThree's rightmost representative ray stays structurally clear of the ceiling; " +
            $"hit_x={anchorThreeHit.Value.Position.X:0.00} ceiling_left={ceilingLeft:0.00}");
        GD.Print($"TASK3_CATCH_THREE_RAY_NOTCH_OK ceiling_hit={ceilingHit} " +
            $"distance={hitDistance:0.00} anchor_two_clearance={anchorTwoClearance:0.00} " +
            $"landing_notch={landingStart - landingRayX:0.00} " +
            $"anchor_three_gap={ceilingLeft - anchorThreeHit.Value.Position.X:0.00} " +
            $"vertical_headroom={verticalHeadroom:0.00}");
    }

    private static bool HasLandingMargin(Rect2 zone, Vector2 point)
    {
        Vector2 inset = zone.Size * LandingMarginRatio;
        return new Rect2(zone.Position + inset, zone.Size - inset * 2f).HasPoint(point);
    }

    private static float RequiredRopeForLanding(Vector2 anchor, Rect2 landingZone)
    {
        float safeStart = landingZone.Position.X + landingZone.Size.X * LandingMarginRatio;
        float safeEnd = landingZone.End.X - landingZone.Size.X * LandingMarginRatio;
        float nearestSafeX = Mathf.Clamp(anchor.X, safeStart, safeEnd);
        return anchor.DistanceTo(new Vector2(nearestSafeX, landingZone.GetCenter().Y));
    }

    private static int CatchDirectionPhase()
    {
        const string prefix = "--task3-catch-phase=";
        string argument = OS.GetCmdlineUserArgs().SingleOrDefault(item =>
            item.StartsWith(prefix, StringComparison.Ordinal));
        if (argument == null) return 6;
        int phase = int.Parse(argument.Substring(prefix.Length));
        if (phase is not (0 or 4 or 6 or 8 or 12))
            throw new InvalidOperationException($"unsupported CatchOne phase {phase}");
        return phase;
    }

    private static bool IsAttached(MechanicalClawState state) => state is
        MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing or
        MechanicalClawState.Boost;

    private static string FirstRayHit(CharacterBody2D player, Vector2 target)
    {
        RayHit? hit = FirstRayHitDetail(player, player.GlobalPosition, target);
        return hit.HasValue ? $"{hit.Value.Collider.GetPath()}@{hit.Value.Position}" : "None";
    }

    private static RayHit? FirstRayHitDetail(CharacterBody2D player, Vector2 from, Vector2 target)
    {
        PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(
            from, target);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;
        query.CollisionMask = CombatCollisionLayers.World | CombatCollisionLayers.Enemy |
            CombatCollisionLayers.ThreadTarget;
        var excluded = new Godot.Collections.Array<Rid>();
        CollectCollisionRids(player, excluded);
        query.Exclude = excluded;
        Godot.Collections.Dictionary hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return null;
        Node collider = hit.ContainsKey("collider") ? hit["collider"].AsGodotObject() as Node : null;
        Vector2 position = hit.ContainsKey("position") ? hit["position"].AsVector2() : Vector2.Zero;
        return collider == null ? null : new RayHit(collider, position);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 span = end - start;
        float lengthSquared = span.LengthSquared();
        if (lengthSquared <= 0f) return point.DistanceTo(start);
        float fraction = Mathf.Clamp((point - start).Dot(span) / lengthSquared, 0f, 1f);
        return point.DistanceTo(start + span * fraction);
    }

    private static float RayXAtY(Vector2 start, Vector2 end, float y)
    {
        float vertical = end.Y - start.Y;
        if (Mathf.IsZeroApprox(vertical))
            throw new InvalidOperationException("ray-notch diagnostic requires a non-horizontal ray");
        return start.X + (y - start.Y) * (end.X - start.X) / vertical;
    }

    private static string FirstSlideHit(CharacterBody2D player)
    {
        if (player.GetSlideCollisionCount() == 0) return "None";
        KinematicCollision2D collision = player.GetSlideCollision(0);
        Node collider = collision.GetCollider() as Node;
        return $"{collider?.GetPath().ToString() ?? "Unknown"}@{collision.GetPosition()}";
    }

    private static void CollectCollisionRids(Node node, Godot.Collections.Array<Rid> rids)
    {
        if (node is CollisionObject2D collisionObject) rids.Add(collisionObject.GetRid());
        foreach (Node child in node.GetChildren()) CollectCollisionRids(child, rids);
    }

    private static async Task AssertOrdinaryJumpCannotReplaceClaw(SceneTree tree,
        SectorCase sectorCase, string segmentId)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = $"Task8JumpFallback_{segmentId}" };
        PrologueSector sector = GD.Load<PackedScene>(sectorCase.Path).Instantiate<PrologueSector>();
        TraversalSegment2D segment = sector.GetNode<TraversalSegment2D>(
            $"Gameplay/TraversalContracts/{segmentId}");
        CharacterBody2D player = NewPlayer();
        player.Position = segment.LaunchZone.GetCenter();
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"),
            player.GetNode<MechanicalClawController>("MechanicalClaw"));
        try
        {
            await WaitForFloor(tree, player);
            await DriveSingleJump(tree, player, segment, trace, probeSecondJump: true);
            await PhysicsFrames(tree, 24);
            TestAssert.True(!trace.SecondAirJumpAccepted,
                $"{segmentId} rejects a second Space press during its ordinary-jump probe");
            TestAssert.True(!segment.LandingZone.HasPoint(player.Position),
                $"{segmentId} ordinary jump is intentionally insufficient; " +
                $"position={player.Position} landing={segment.LandingZone}");
            TestAssert.True(player.IsOnFloor() &&
                sector.GetNode("Gameplay/Collision").GetChildren().Any(node =>
                    node.HasMeta("catch_shelf") && IsStandingOn(player, (StaticBody2D)node)),
                $"{segmentId} failed ordinary jump settles on a safe catch shelf; " +
                $"position={player.Position} on_floor={player.IsOnFloor()} frames={trace.Frames}");
        }
        finally
        {
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertShieldLaneBehavior(SceneTree tree)
    {
        await AssertShieldLaneUnguarded(tree);
        await AssertShieldLaneGuarded(tree);
        GD.Print("TASK8_SHIELD_LANE_OK unguarded=damaged guarded=crossed energy=drained " +
            "heat=increased recovery=observed");
    }

    private static async Task AssertShieldLaneUnguarded(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8ShieldLaneUnguarded" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[2].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(300, 574);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        try
        {
            await WaitForFloor(tree, player);
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            CanyonShieldLaneHazard lane = sector.GetNode<CanyonShieldLaneHazard>(
                "Gameplay/ShieldLane");
            int before = health.CurrentHealth;
            SendAction(InputActions.MoveRight, true);
            for (int frame = 0; frame < 120 && health.CurrentHealth == before; frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            SendAction(InputActions.MoveRight, false);
            TestAssert.True(lane.PulseCount > 0 && health.CurrentHealth < before,
                "unguarded real lane entry takes authoritative damage; " +
                $"pulses={lane.PulseCount} health={before}->{health.CurrentHealth} x={player.Position.X:0.0}");
            TestAssert.True(player.Position.X < 710f,
                "unguarded hazard interrupts progress before the lane exit");
        }
        finally
        {
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertShieldLaneGuarded(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8ShieldLaneGuarded" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[2].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(300, 574);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        try
        {
            await WaitForFloor(tree, player);
            HealthComponent health = player.GetNode<HealthComponent>("Health");
            PlayerEnergyController energy = player.GetNode<PlayerEnergyController>("Energy");
            PlayerShieldController shield = player.GetNode<PlayerShieldController>("Shield");
            CanyonShieldLaneHazard lane = sector.GetNode<CanyonShieldLaneHazard>(
                "Gameplay/ShieldLane");
            int healthBefore = health.CurrentHealth;
            float energyBefore = energy.EnergyRatio;
            float heatBefore = energy.HeatRatio;
            Vector2 aim = player.GlobalPosition + Vector2.Right * 240f;
            MoveMouse(aim);
            SendMouse(MouseButton.Right, true, aim);
            await PhysicsFrames(tree, 3);
            TestAssert.True(shield.IsActive, "right mouse activates the directional shield before entry");
            SendAction(InputActions.MoveRight, true);
            for (int frame = 0; frame < 150 && player.Position.X < 730f; frame++)
            {
                MoveMouse(player.GlobalPosition + Vector2.Right * 240f);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            SendAction(InputActions.MoveRight, false);
            float spentEnergy = energy.EnergyRatio;
            float raisedHeat = energy.HeatRatio;
            TestAssert.True(player.Position.X >= 700f && lane.PulseCount > 0,
                "guarded player actually enters and crosses the complete hazard lane; " +
                $"x={player.Position.X:0.0} pulses={lane.PulseCount}");
            TestAssert.Equal(healthBefore, health.CurrentHealth,
                "front-facing directional shield blocks production hazard damage");
            TestAssert.True(spentEnergy < energyBefore && raisedHeat > heatBefore,
                "guarded lane traversal drains energy and adds heat; " +
                $"energy={energyBefore:0.000}->{spentEnergy:0.000} " +
                $"heat={heatBefore:0.000}->{raisedHeat:0.000}");
            SendMouse(MouseButton.Right, false, player.GlobalPosition + Vector2.Right * 240f);
            await PhysicsFrames(tree, 100);
            TestAssert.True(!shield.IsActive && energy.EnergyRatio > spentEnergy &&
                energy.HeatRatio < raisedHeat,
                "releasing the shield after lane exit exposes cooldown, regeneration and heat recovery; " +
                $"energy={spentEnergy:0.000}->{energy.EnergyRatio:0.000} " +
                $"heat={raisedHeat:0.000}->{energy.HeatRatio:0.000}");
        }
        finally
        {
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task AssertBellChainGateBehavior(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8BellChainGate" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[4].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(520, 574);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
            "Gameplay/Mechanisms/BellChain");
        GrappleSurface2D target = chain.GetNode<GrappleSurface2D>("ThreadTarget");
        var trace = new RouteTrace(motor, claw) { ExpectedTarget = target.GetInstanceId() };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        LegacyThreadInteractionAdapter adapter = target.GetNode<LegacyThreadInteractionAdapter>(
            "LegacyThreadInteractionAdapter");
        try
        {
            await WaitForFloor(tree, player);
            CollisionShape2D door = chain.GetNode<CollisionShape2D>(chain.DoorCollisionPath);
            TestAssert.True(!door.Disabled && !chain.IsOpen,
                "floor-reaching BellChain shortcut starts physically closed");
            SendAction(InputActions.MoveRight, true);
            await PhysicsFrames(tree, 90);
            SendAction(InputActions.MoveRight, false);
            TestAssert.True(player.Position.X <= 710f,
                "mandatory temple floor route is blocked before the mechanism interaction; " +
                $"x={player.Position.X:0.0}");
            float blockedX = player.Position.X;

            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            for (int frame = 0; frame < 120 && !chain.IsOpen; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            await PhysicsFrames(tree, 3);
            TestAssert.True(chain.IsOpen && door.Disabled && adapter.ApplicationCount == 1 &&
                trace.CastCount == 1 && trace.LatchCount == 1 &&
                trace.LatchedTarget == target.GetInstanceId(),
                "real E-mode mechanical-claw cast/latch invokes the legacy adapter and opens the gate; " +
                $"state={claw.CurrentFrame.State} casts={trace.CastCount} latches={trace.LatchCount} " +
                $"applications={adapter.ApplicationCount}");
            await DriveHorizontalTo(tree, player, 850f, 180);
            TestAssert.True(player.Position.X >= 820f,
                "opened BellChain path can be crossed into the checkpoint side");
            float crossedX = player.Position.X;
            await DriveHorizontalTo(tree, player, 640f, 180);
            TestAssert.True(player.Position.X <= 670f,
                "opened shortcut remains usable for a quick return to the mechanism side");
            GD.Print($"TASK8_BELL_GATE_OK door_x={door.GlobalPosition.X:0.0} " +
                $"door_top={door.GlobalPosition.Y - chain.DoorHeight / 2f:0.0} " +
                $"door_bottom={door.GlobalPosition.Y + chain.DoorHeight / 2f:0.0} " +
                $"blocked_x={blockedX:0.0} casts={trace.CastCount} latches={trace.LatchCount} " +
                $"applications={adapter.ApplicationCount} crossed_x={crossedX:0.0} " +
                $"return_x={player.Position.X:0.0}");
        }
        finally
        {
            ReleaseAll();
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static async Task OpenBellGateForSegment(SceneTree tree, CharacterBody2D player,
        PrologueSector sector)
    {
        BellChainMechanism chain = sector.GetNode<BellChainMechanism>(
            "Gameplay/Mechanisms/BellChain");
        GrappleSurface2D target = chain.GetNode<GrappleSurface2D>("ThreadTarget");
        MoveMouse(target.WorldAnchor);
        SendAction(InputActions.SelectGrappleMode, true);
        await PhysicsFrames(tree, 1);
        SendAction(InputActions.SelectGrappleMode, false);
        SendMouse(MouseButton.Left, true, target.WorldAnchor);
        for (int frame = 0; frame < 120 && !chain.IsOpen; frame++)
        {
            MoveMouse(target.WorldAnchor);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
        SendMouse(MouseButton.Left, false, target.WorldAnchor);
        await PhysicsFrames(tree, 4);
        TestAssert.True(chain.IsOpen && chain.GetNode<CollisionShape2D>(
                chain.DoorCollisionPath).Disabled,
            "temple checkpoint sample satisfies the mandatory real BellChain precondition");
    }

    private static async Task AssertGripStoneLatchAndLanding(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8GripStoneProbe" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[1].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(170, 514);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/GripStoneLesson");
        StaticBody2D launchSupport = sector.GetNode<StaticBody2D>(
            "Gameplay/Collision/EntryShelf");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = target.GetInstanceId()
        };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            TestAssert.True(IsStandingOn(player, launchSupport),
                "GripStone probe begins on its explicit authored launch support");
            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            for (int frame = 0; frame < 120 && !trace.HasAttached; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == trace.ExpectedTarget,
                "real GripStone E+left cast travels and latches its exact target");
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.MoveUp, true);
            bool leftLaunchSupport = false;
            for (int frame = 0; frame < 90; frame++)
            {
                await PhysicsFrames(tree, 1);
                leftLaunchSupport |= !IsStandingOn(player, launchSupport);
            }
            TestAssert.True(leftLaunchSupport,
                "real GripStone latch pulls the player away from the original launch support");
            SendAction(InputActions.MoveRight, false);
            SendAction(InputActions.MoveUp, false);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            int stable = 0;
            for (int frame = 0; frame < 240 && stable < 3; frame++)
            {
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                stable = player.IsOnFloor() ? stable + 1 : 0;
            }
            TestAssert.True(stable >= 3,
                $"GripStone release preserves a real safe landing; position={player.Position}");
            StaticBody2D catchZone = sector.GetNodeOrNull<StaticBody2D>(
                "Gameplay/Collision/GripStoneCatchZone");
            TestAssert.True(catchZone != null && IsSafeGripStoneCatchLanding(player, catchZone),
                "GripStone cast leaves its launch shelf and settles in the named authored " +
                "GripStoneCatchZone interior with collision-radius safety and headroom");
            GD.Print($"TASK8_GRIPSTONE_OK casts={trace.CastCount} latches={trace.LatchCount} " +
                $"landing={player.Position} inset={PlayerCollisionRadius + GripStoneCatchSafetyMargin} " +
                $"headroom={GripStoneCatchHeadroom}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    // Adversarial floor-contact probe: the old cumulative causality helper accepted an attached
    // cast without checking whether the player was grounded at that immediate Space edge.
    internal static async Task AssertAttachedSecondSpaceRejectsFloorContact(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8AttachedFloorAdversary" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[1].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(170, 514);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/GripStoneLesson");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = target.GetInstanceId()
        };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            bool observedAttachedFloor = false;
            for (int frame = 0; frame < 120 && !observedAttachedFloor; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await PhysicsFrames(tree, 1);
                if (!trace.HasAttached || !player.IsOnFloor()) continue;
                bool floorEdgeAccepted = trace.TryBeginAttachedSecondSpace(player, target);
                GD.Print($"TASK8_FIX3_AIRBORNE_EDGE_PROBE state={claw.CurrentFrame.State} " +
                    $"cast={claw.CurrentFrame.CastId} left={Input.IsMouseButtonPressed(MouseButton.Left)} " +
                    $"on_floor={player.IsOnFloor()} accepted={floorEdgeAccepted}");
                TestAssert.True(!floorEdgeAccepted,
                    "an attached cumulative cast on floor must be rejected before the Space edge");
                observedAttachedFloor = true;
            }
            TestAssert.True(observedAttachedFloor,
                "adversarial real E+left fixture reaches an attached floor-contact state");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    // Intentional historical RED: the real physical edge landing is accepted by the old
    // standing-only predicate. This runner is never added to production suites.
    internal static async Task AssertGripStoneLegacyMarginPhysicalRed(SceneTree tree)
    {
        GripStoneMarginFlight flight = await RunGripStoneMarginFlight(tree);
        GD.Print($"TASK8_FIX4_GRIPSTONE_MARGIN_RED_PHYSICAL casts={flight.CastCount} " +
            $"latches={flight.LatchCount} departed={flight.DepartedLaunchSupport} " +
            $"landing={flight.Landing} catch_left={flight.CatchLeft:0.0} " +
            $"legacy_accepted={flight.LegacyAccepted} safe_accepted={flight.SafeAccepted}");
        TestAssert.True(!flight.LegacyAccepted,
            "legacy standing-only GripStone acceptance must reject the physical marginal landing");
    }

    // Break caught: an actual E+LMB GripStone flight can settle on a collision-overlap edge that
    // the old standing-only predicate accepted although the authored radius, safety, and headroom
    // contract rejects it. The catch shelf shift is a fixture arrangement before any input; there
    // is no post-launch position reset or teleport.
    internal static async Task AssertGripStoneRejectsPhysicalMarginalLanding(SceneTree tree)
    {
        GripStoneMarginFlight flight = await RunGripStoneMarginFlight(tree);
        GD.Print($"TASK8_FIX4_GRIPSTONE_MARGIN_GREEN_PHYSICAL casts={flight.CastCount} " +
            $"latches={flight.LatchCount} departed={flight.DepartedLaunchSupport} " +
            $"landing={flight.Landing} catch_left={flight.CatchLeft:0.0} " +
            $"legacy_accepted={flight.LegacyAccepted} safe_accepted={flight.SafeAccepted}");
        TestAssert.True(flight.CastCount == 1 && flight.LatchCount == 1 &&
            flight.DepartedLaunchSupport && flight.PhysicalLanding,
            "marginal GripStone evidence uses one real cast/latch, leaves launch support, and settles physically");
        TestAssert.True(flight.LegacyAccepted && !flight.SafeAccepted,
            "the physical edge settle reproduces old acceptance while the radius-plus-margin safe oracle rejects it");
    }

    private static async Task<GripStoneMarginFlight> RunGripStoneMarginFlight(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8GripStonePhysicalMarginAdversary" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[1].Path).Instantiate<PrologueSector>();
        StaticBody2D catchZone = sector.GetNode<StaticBody2D>(
            "Gameplay/Collision/GripStoneCatchZone");
        RectangleShape2D shape = catchZone.GetNode<CollisionShape2D>("Collision").Shape as
            RectangleShape2D ?? throw new InvalidOperationException("catch shelf must be rectangular");
        // The ordinary real flight lands at x≈304. Shift only this test shelf so that x≈304 is a
        // seven-pixel legacy overlap at its left edge (old standing acceptance) rather than its
        // safe authored interior. This fixture geometry is fixed before the tree is mounted.
        catchZone.Position += new Vector2(51f, 0f);
        CharacterBody2D player = NewPlayer();
        player.Position = new Vector2(170, 514);
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        MechanicalClawController claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        GrappleSurface2D target = sector.GetNode<GrappleSurface2D>(
            "Gameplay/GrappleSurfaces/GripStoneLesson");
        StaticBody2D launchSupport = sector.GetNode<StaticBody2D>(
            "Gameplay/Collision/EntryShelf");
        var trace = new RouteTrace(player.GetNode<PlayerMotor>("Motor"), claw)
        {
            ExpectedTarget = target.GetInstanceId()
        };
        claw.ClawFrameChanged += trace.OnClawFrameChanged;
        try
        {
            await WaitForFloor(tree, player);
            TestAssert.True(IsStandingOn(player, launchSupport),
                "physical margin probe begins on the authored launch support");
            MoveMouse(target.WorldAnchor);
            SendAction(InputActions.SelectGrappleMode, true);
            await PhysicsFrames(tree, 1);
            SendAction(InputActions.SelectGrappleMode, false);
            SendMouse(MouseButton.Left, true, target.WorldAnchor);
            for (int frame = 0; frame < 120 && !trace.HasAttached; frame++)
            {
                MoveMouse(target.WorldAnchor);
                await PhysicsFrames(tree, 1);
            }
            TestAssert.True(trace.HasAttached && trace.LatchedTarget == trace.ExpectedTarget,
                "physical margin probe real E+left cast latches the GripStone target");
            SendAction(InputActions.MoveRight, true);
            SendAction(InputActions.MoveUp, true);
            bool departed = false;
            for (int frame = 0; frame < 90; frame++)
            {
                await PhysicsFrames(tree, 1);
                departed |= !IsStandingOn(player, launchSupport);
            }
            SendAction(InputActions.MoveRight, false);
            SendAction(InputActions.MoveUp, false);
            SendMouse(MouseButton.Left, false, target.WorldAnchor);
            int stable = 0;
            for (int frame = 0; frame < 240 && stable < 3; frame++)
            {
                await PhysicsFrames(tree, 1);
                stable = player.IsOnFloor() ? stable + 1 : 0;
            }
            float catchLeft = catchZone.Position.X - shape.Size.X / 2f;
            bool legacyAccepted = IsStandingOn(player, catchZone);
            bool safeAccepted = IsSafeGripStoneCatchLanding(player, catchZone);
            return new GripStoneMarginFlight(trace.CastCount, trace.LatchCount, departed,
                stable >= 3, player.Position, catchLeft, legacyAccepted, safeAccepted);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(claw)) claw.ClawFrameChanged -= trace.OnClawFrameChanged;
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static bool IsStandingOn(CharacterBody2D player, StaticBody2D body)
    {
        if (body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is not RectangleShape2D shape)
            return false;
        float top = body.Position.Y - shape.Size.Y / 2f;
        return player.Position.X >= body.Position.X - shape.Size.X / 2f - 8f &&
            player.Position.X <= body.Position.X + shape.Size.X / 2f + 8f &&
            MathF.Abs(player.Position.Y - (top - 36f)) <= 10f;
    }

    private static bool IsSafeGripStoneCatchLanding(CharacterBody2D player, StaticBody2D catchZone)
    {
        if (!IsStandingOn(player, catchZone) ||
            catchZone.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is not RectangleShape2D shape)
            return false;
        Rect2 footprint = new(catchZone.Position - shape.Size / 2f, shape.Size);
        float inset = PlayerCollisionRadius + GripStoneCatchSafetyMargin;
        bool inside = player.Position.X >= footprint.Position.X + inset &&
            player.Position.X <= footprint.End.X - inset;
        Rect2 headroom = new(player.Position.X - PlayerCollisionRadius,
            player.Position.Y - PlayerCollisionHalfHeight - GripStoneCatchHeadroom,
            PlayerCollisionRadius * 2f, GripStoneCatchHeadroom);
        bool blockedAbove = catchZone.GetParent()?.GetChildren().OfType<StaticBody2D>()
            .Where(body => body != catchZone)
            .Select(body => body.GetNodeOrNull<CollisionShape2D>("Collision")?.Shape is RectangleShape2D other
                ? new Rect2(body.Position - other.Size / 2f, other.Size)
                : new Rect2())
            .Any(bounds => bounds.Size != Vector2.Zero && bounds.Intersects(headroom)) == true;
        return inside && !blockedAbove;
    }

    private static async Task AssertOptionalPassAndSkip(SceneTree tree, SectorCase sectorCase,
        string optionalName)
    {
        foreach (string route in new[] { "Pass", "Skip" })
        foreach (bool reverse in new[] { false, true })
        {
            ReleaseAll();
            var fixture = new Node2D { Name = $"Task8Optional_{optionalName}_{route}_{reverse}" };
            PrologueSector sector = GD.Load<PackedScene>(sectorCase.Path).Instantiate<PrologueSector>();
            Node path = sector.GetNode($"Gameplay/OptionalPaths/{optionalName}");
            Marker2D start = path.GetNode<Marker2D>(reverse ? $"{route}End" : $"{route}Start");
            Marker2D end = path.GetNode<Marker2D>(reverse ? $"{route}Start" : $"{route}End");
            CharacterBody2D player = NewPlayer();
            player.Position = start.Position;
            fixture.AddChild(sector);
            fixture.AddChild(player);
            tree.Root.AddChild(fixture);
            try
            {
                await WaitForFloor(tree, player);
                await DriveToMarker(tree, player, end.Position, 220);
                TestAssert.True(player.IsOnFloor() && player.Position.DistanceTo(end.Position) <= 52f,
                    $"{optionalName} {route.ToLowerInvariant()} route is physically traversable " +
                    $"{(reverse ? "back to mainline" : "forward")} without a one-way drop; " +
                    $"position={player.Position} target={end.Position} on_floor={player.IsOnFloor()}");
            }
            finally
            {
                ReleaseAll();
                fixture.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await PhysicsFrames(tree, 2);
            }
        }
        GD.Print($"TASK8_OPTIONAL_ROUTE_OK sector={sectorCase.Name} path={optionalName} " +
            "pass=True skip=True returnable=True");
    }

    private static async Task DriveToMarker(SceneTree tree, CharacterBody2D player,
        Vector2 target, int budget)
    {
        for (int frame = 0; frame < budget &&
            (!player.IsOnFloor() || player.Position.DistanceTo(target) > 42f); frame++)
        {
            float dx = target.X - player.Position.X;
            Input.ActionPress(dx < 0 ? InputActions.MoveLeft : InputActions.MoveRight);
            if (player.IsOnFloor() && target.Y < player.Position.Y - 18f)
                Input.ActionPress(InputActions.Jump);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            if (!player.IsOnFloor() || player.Velocity.Y < -80f) Input.ActionRelease(InputActions.Jump);
        }
        ReleaseAll();
        await PhysicsFrames(tree, 4);
    }

    private static async Task DriveHorizontalTo(SceneTree tree, CharacterBody2D player,
        float targetX, int budget)
    {
        StringName action = targetX < player.Position.X ? InputActions.MoveLeft : InputActions.MoveRight;
        SendAction(action, true);
        for (int frame = 0; frame < budget && MathF.Abs(player.Position.X - targetX) > 24f; frame++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        SendAction(action, false);
        await PhysicsFrames(tree, 3);
    }

    private static async Task AssertTempleArenaLocksOnlyAfterTrigger(SceneTree tree)
    {
        ReleaseAll();
        var fixture = new Node2D { Name = "Task8TempleBossTrigger" };
        PrologueSector sector = GD.Load<PackedScene>(Sectors[4].Path).Instantiate<PrologueSector>();
        CharacterBody2D player = NewPlayer();
        Marker2D start = sector.GetNode<Marker2D>("Gameplay/BossApproachStart");
        player.Position = start.Position;
        fixture.AddChild(sector);
        fixture.AddChild(player);
        tree.Root.AddChild(fixture);
        try
        {
            await PhysicsFrames(tree, 3);
            BossArenaController arena = sector.GetNode<BossArenaController>(
                "Gameplay/BossArenaController");
            CollisionShape2D entry = sector.GetNode<CollisionShape2D>(
                "Gameplay/Doors/Entry/Collision");
            TestAssert.True(!arena.IsEncounterActive && entry.Disabled,
                "temple boss entry remains open before the player crosses the activation trigger");
            await DriveToMarker(tree, player,
                sector.GetNode<Area2D>("Gameplay/ActivationTrigger").Position + new Vector2(36, 0), 180);
            await PhysicsFrames(tree, 3);
            TestAssert.True(arena.IsEncounterActive && !entry.Disabled,
                "temple boss arena locks only after a real player body crosses the trigger; " +
                $"position={player.Position} active={arena.IsEncounterActive} " +
                $"entry_disabled={entry.Disabled}");
            GD.Print($"TASK8_BOSS_TRIGGER_OK position={player.Position} " +
                $"active={arena.IsEncounterActive} entry_disabled={entry.Disabled}");
        }
        finally
        {
            ReleaseAll();
            fixture.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await PhysicsFrames(tree, 2);
        }
    }

    private static CharacterBody2D NewPlayer()
    {
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        player.Name = "Task8TraversalPlayer";
        player.AddToGroup("player");
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        return player;
    }

    private static async Task WaitForFloor(SceneTree tree, CharacterBody2D player)
    {
        ActiveInputCanvas = player;
        int stable = 0;
        for (int frame = 0; frame < 60 && stable < 3; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            stable = player.IsOnFloor() ? stable + 1 : 0;
        }
        TestAssert.True(stable >= 3, $"physical launch settles on real collision at {player.Position}");
    }

    private static float PredictLandingX(CharacterBody2D player, PlayerMotor motor,
        float landingY, float direction)
    {
        Vector2 position = player.Position;
        Vector2 velocity = player.Velocity;
        bool reachedLandingHeight = position.Y <= landingY;
        const float seconds = 1f / 60f;
        for (int frame = 0; frame < AssistedBudget; frame++)
        {
            velocity.X = Mathf.MoveToward(velocity.X, direction * motor.Tuning.MaxRunSpeed,
                motor.Tuning.AirAcceleration * seconds);
            velocity.Y = Mathf.Min(velocity.Y + motor.Tuning.Gravity * seconds,
                motor.Tuning.MaxFallSpeed);
            position += velocity * seconds;
            reachedLandingHeight |= position.Y <= landingY;
            if (reachedLandingHeight && velocity.Y >= 0 && position.Y >= landingY)
                return position.X;
        }
        return float.NaN;
    }

    private static async Task PhysicsFrames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static void SendAction(StringName action, bool pressed)
    {
        DispatchInputEvent(new InputEventAction
        {
            Action = action,
            Pressed = pressed,
            Strength = pressed ? 1f : 0f
        });
    }

    private static void SendMouse(MouseButton button, bool pressed, Vector2 position)
    {
        Vector2 viewportPosition = ToViewport(position);
        MoveMouse(position);
        DispatchInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Position = viewportPosition,
            GlobalPosition = viewportPosition,
            ButtonMask = pressed ? button == MouseButton.Left
                ? MouseButtonMask.Left
                : MouseButtonMask.Right
                : (MouseButtonMask)0
        });
    }

    private static void MoveMouse(Vector2 position)
    {
        Vector2 viewportPosition = ToViewport(position);
        Viewport viewport = ResolveInputViewport();
        DispatchMouseMotion(viewport, viewportPosition);
    }

    public static async Task AssertViewportMouseEventPropagation(SceneTree tree,
        PlayerInputReader inputReader, PrologueAimReticle reticle)
    {
        // This display-only proof uses the same MouseMotion event route as a real mouse:
        // Input.ParseInputEvent publishes InputMap state and the concrete Viewport.PushInput
        // delivers the same event to the production PlayerInputReader. It intentionally does
        // not demand an OS cursor warp, because that is host-window-manager dependent. Neither
        // this helper nor the adapter writes player, grapple, model, or aim state directly.
        ActiveTree = tree;
        TestAssert.True(inputReader != null && inputReader.IsInsideTree(),
            "Task6 input proof requires the production PlayerInputReader in the live scene");
        TestAssert.True(reticle != null && reticle.IsInsideTree(),
            "Task6 input proof requires the visible live aim reticle");
        Viewport viewport = inputReader.GetViewport();
        TestAssert.True(viewport != null && viewport.IsInsideTree(),
            "Task6 input proof has a live target viewport for mouse-event dispatch");

        DispatchMouseMotion(viewport, Vector2.Zero);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        TestAssert.True(inputReader.HasObservedMouseMotion &&
            inputReader.LastObservedMouseViewportPosition.IsEqualApprox(Vector2.Zero),
            "Task6 production input adapter must observe the event baseline (0, 0)");
        ulong baselineSequence = inputReader.MouseMotionSequence;

        Vector2 intended = new(1120f, 620f);
        DispatchMouseMotion(viewport, intended);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Vector2 sampled = inputReader.LastObservedMouseViewportPosition;
        TestAssert.True(inputReader.MouseMotionSequence > baselineSequence && sampled.IsEqualApprox(intended),
            $"Task6 production input adapter must observe (1120, 620), sampled={sampled} " +
            $"sequence={inputReader.MouseMotionSequence}");
        TestAssert.True(reticle.CurrentFrame.Visible && reticle.Visible &&
            reticle.CurrentFrame.ScreenPosition.IsEqualApprox(intended),
            $"Task6 visible reticle must bind the adapter target; reticle={reticle.CurrentFrame.ScreenPosition} " +
            $"sampled={sampled}");

        GD.Print($"TASK6_INPUT_VIEWPORT_BASELINE_OK sampled=(0, 0) events={ViewportInputTransport}");
        GD.Print("TASK6_INPUT_VIEWPORT_SAMPLE_OK sampled=(1120, 620) intended=(1120, 620) " +
            $"events={ViewportInputTransport}");
        GD.Print("TASK6_INPUT_RETICLE_BINDING_OK sampled=(1120, 620) reticle=(1120, 620) " +
            $"adapter=True reticle_visible=True events={ViewportInputTransport}");
    }

    private static void DispatchMouseMotion(Viewport viewport, Vector2 position)
    {
        // Generic headless teardown may release a held mouse action before a fixture mounts a
        // viewport. Input.ParseInputEvent still clears that InputMap action; display evidence
        // calls AssertViewportMouseEventPropagation above and retains its separate live
        // viewport/adapter/reticle hard assertions before it certifies any visible aim.
        DispatchInputEvent(new InputEventMouseMotion
        {
            Position = position,
            GlobalPosition = position
        }, viewport);
    }

    private static void DispatchInputEvent(InputEvent inputEvent, Viewport targetViewport = null)
    {
        // Input.ParseInputEvent keeps action state available to PlayerInputReader; PushInput
        // routes the exact same event to the concrete display viewport. Keeping both in one
        // helper prevents a synthetic test frame from bypassing the real input path.
        Input.ParseInputEvent(inputEvent);
        Viewport viewport = targetViewport ?? ResolveInputViewport();
        if (viewport != null && viewport.IsInsideTree())
            viewport.PushInput(inputEvent, true);
        Input.FlushBufferedEvents();
    }

    private static Viewport ResolveInputViewport()
    {
        if (GodotObject.IsInstanceValid(ActiveInputCanvas) && ActiveInputCanvas.IsInsideTree())
            return ActiveInputCanvas.GetViewport();
        return ActiveTree?.Root?.GetViewport();
    }

    private static Vector2 ToViewport(Vector2 worldPosition)
    {
        if (GodotObject.IsInstanceValid(ActiveInputCanvas) && ActiveInputCanvas.IsInsideTree())
            return ActiveInputCanvas.GetViewport().GetFinalTransform() *
                (ActiveInputCanvas.GetGlobalTransformWithCanvas() *
                    ActiveInputCanvas.ToLocal(worldPosition));
        return worldPosition;
    }

    private static void ReleaseAll()
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
        SendMouse(MouseButton.Left, false, Vector2.Zero);
        SendMouse(MouseButton.Right, false, Vector2.Zero);
    }

    private sealed record SectorCase(string Name, string Path);

    private sealed record GripStoneMarginFlight(int CastCount, int LatchCount,
        bool DepartedLaunchSupport, bool PhysicalLanding, Vector2 Landing, float CatchLeft,
        bool LegacyAccepted, bool SafeAccepted);

    private sealed record CatchReturnCase(SectorCase Sector, string SegmentId, string CatchName,
        NodePath TargetPath, Vector2 AimLocal, bool TerrainReturn);

    private readonly record struct RayHit(Node Collider, Vector2 Position);

    private sealed class RouteTrace
    {
        private readonly HashSet<ulong> _castIds = new();
        private readonly HashSet<ulong> _latchIds = new();

        public RouteTrace(PlayerMotor motor, MechanicalClawController claw)
        {
            Motor = motor;
            Claw = claw;
        }

        public PlayerMotor Motor { get; }
        public MechanicalClawController Claw { get; }
        public int Frames { get; set; }
        public bool LeftFloor { get; private set; }
        public int StableLandingFrames { get; private set; }
        public int CastCount => _castIds.Count;
        public int LatchCount => _latchIds.Count;
        public bool HasAttached { get; private set; }
        public ulong ExpectedTarget { get; set; }
        public ulong LatchedTarget { get; private set; }
        public Vector2 LatchedAnchor { get; private set; }
        public float SecondJumpVelocityBefore { get; set; }
        public int SecondJumpAcceptedCountBefore { get; set; }
        public bool SecondAirJumpAccepted { get; set; }
        public bool SecondJumpAttempted { get; set; }
        public bool SecondJumpMeasured { get; set; }
        public bool AttachedSecondSpaceCausalityObserved { get; set; }
        public bool AttachedSecondSpaceAirborneAtEdge { get; private set; }
        public MechanicalClawFailure ObservedFailure { get; private set; }
        private ulong AttachedSecondSpaceCast { get; set; }
        private bool AttachedSecondSpaceCurrentState { get; set; }
        public bool BoostFromSecondSpaceObserved { get; private set; }
        public bool AttachedAfterSecondSpaceWhileLeftHeld { get; private set; }
        public bool ReleasedFromSecondSpaceWhileLeftHeld { get; private set; }
        public bool ReleasedFromLeftMouseRelease { get; private set; }
        private bool SecondSpaceInputDispatched { get; set; }

        public void OnClawFrameChanged(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (frame.Failure != MechanicalClawFailure.None) ObservedFailure = frame.Failure;
            if (frame.State == MechanicalClawState.Windup && frame.CastId != 0)
                _castIds.Add(frame.CastId);
            bool attached = frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                MechanicalClawState.Swing or MechanicalClawState.Boost;
            if (attached && frame.CastId != 0)
            {
                if (_latchIds.Add(frame.CastId)) LatchedTarget = target?.GetInstanceId() ?? 0;
                LatchedAnchor = frame.AnchorPoint;
            }
            HasAttached |= attached;
            if (SecondSpaceInputDispatched && frame.CastId == AttachedSecondSpaceCast)
            {
                bool leftHeld = Input.IsMouseButtonPressed(MouseButton.Left);
                bool stillAttached = frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or
                    MechanicalClawState.Swing or MechanicalClawState.Boost;
                AttachedAfterSecondSpaceWhileLeftHeld |= stillAttached && leftHeld;
                BoostFromSecondSpaceObserved |= frame.State == MechanicalClawState.Boost;
                if (frame.State is MechanicalClawState.Released or MechanicalClawState.Retract)
                {
                    ReleasedFromSecondSpaceWhileLeftHeld |= leftHeld;
                    ReleasedFromLeftMouseRelease |= !leftHeld;
                }
            }
        }

        public bool TryBeginAttachedSecondSpace(CharacterBody2D player, GrappleSurface2D expectedTarget)
        {
            MechanicalClawFrame frame = Claw.CurrentFrame;
            AttachedSecondSpaceCurrentState = frame.State is MechanicalClawState.Latched or
                MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;
            bool expectedCast = frame.CastId != 0 && frame.LockedTargetRuntimeId == ExpectedTarget &&
                Claw.CurrentTarget == expectedTarget;
            bool leftHeld = Input.IsMouseButtonPressed(MouseButton.Left);
            AttachedSecondSpaceAirborneAtEdge = !player.IsOnFloor();
            if (!AttachedSecondSpaceCurrentState || !expectedCast || !leftHeld ||
                !AttachedSecondSpaceAirborneAtEdge) return false;
            AttachedSecondSpaceCast = frame.CastId;
            return true;
        }

        public void RecordSecondSpaceInputDispatched() => SecondSpaceInputDispatched = true;

        public void CompleteAttachedSecondSpaceCausality()
        {
            AttachedSecondSpaceCausalityObserved = AttachedSecondSpaceCurrentState &&
                AttachedSecondSpaceAirborneAtEdge && AttachedSecondSpaceCast != 0 &&
                SecondSpaceInputDispatched && BoostFromSecondSpaceObserved &&
                AttachedAfterSecondSpaceWhileLeftHeld && !ReleasedFromSecondSpaceWhileLeftHeld &&
                !SecondAirJumpAccepted;
        }

        public void CompleteExplicitLeftReleaseCausality()
        {
            ReleasedFromLeftMouseRelease &= !Input.IsMouseButtonPressed(MouseButton.Left);
        }

        public void Observe(CharacterBody2D player, TraversalSegment2D segment)
        {
            LeftFloor |= !player.IsOnFloor();
            StableLandingFrames = LeftFloor && player.IsOnFloor() &&
                HasLandingMargin(segment.LandingZone, player.Position)
                ? StableLandingFrames + 1
                : 0;
        }
    }
}
