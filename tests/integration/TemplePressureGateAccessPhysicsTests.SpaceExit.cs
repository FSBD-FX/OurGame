using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Player;

namespace Knight.Tests.Integration;

public static partial class TemplePressureGateAccessPhysicsTests
{
    // Three predeclared samples, not a timing search or continuous/human-latency claim.
    public static async Task SpaceExit(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var failures = new List<Exception>();
        foreach (int delay in new[] { 0, 12, 24 })
        {
            (Exception failure, bool safelyFreed) = await SpaceExitTrial(tree, delay);
            GD.Print($"TASK18_TEMPLE_SPACE_EXIT_RESULT delay={delay} status={(failure == null ? "PASSED" : "FAILED")} safelyFreed={safelyFreed} diagnostics={failure}");
            if (failure != null) failures.Add(failure);
            else GD.Print($"TASK18_TEMPLE_SPACE_EXIT_{delay}_OK fixture=true fullRoute=false cleanupComplete=true");
            if (!safelyFreed)
                throw new AggregateException("Space-exit cleanup failed; remaining fixtures cannot safely mount", failures);
        }
        if (failures.Count != 0) throw new AggregateException("Fixed Space-exit trials failed", failures);
        GD.Print("TASK18_TEMPLE_SPACE_EXIT_ALL_OK fixture=true fullRoute=false cleanupComplete=true sampledDelays=0,12,24");
    }

    private static async Task<(Exception Failure, bool SafelyFreed)> SpaceExitTrial(SceneTree tree, int delay)
    {
        Node2D world = null;
        CharacterBody2D player = null;
        MechanicalClawController claw = null;
        Action<MechanicalClawFrame, GrappleSurface2D> observer = null;
        Exception observerFailure = null, failure = null;
        bool safelyFreed = false;
        try
        {
            world = Assemble(new Vector2(5825, 1080), null, out player, out var gate, out _);
            ulong playerId = player.GetInstanceId(), castId = 0;
            claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var input = player.GetNode<PlayerInputReader>("Input");
            var intended = world.GetNode<GrappleSurface2D>("Collision/TempleSupplyGripBeam/GrappleSurfaceProfile");
            var shelf = world.GetNode<StaticBody2D>("Collision/TempleSupplyShelf");
            tree.Root.AddChild(world);
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, null);
            int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
            void Invariant()
            {
                if (observerFailure != null) throw new InvalidOperationException("Space-exit publication observer failed", observerFailure);
                TestAssert.True(player.GetInstanceId() == playerId && Health(player) == 5 &&
                    motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes,
                    "same real Player keeps HP5 and original ordinary jump/dash counters");
                TestAssert.True(claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                    (castId == 0 || claw.CurrentFrame.CastId == 0 || claw.CurrentFrame.CastId == castId),
                    "Space-exit has no failed or second cast");
                AssertNoEmbedding(player);
            }
            for (int i = 0; i < 90; i++)
            {
                await Frame(tree); Invariant();
                if (OriginalFloor(world, player)) break;
            }
            TestAssert.True(OriginalFloor(world, player) && !gate.IsOpen && !gate.HasOpened && DoorRayHits(player, gate),
                "unchanged lower support and initially closed actual door query are established within90");
            GD.Print($"TASK18_TEMPLE_SPACE_EXIT_START delay={delay} playerId={playerId} body={Bounds(player).Size} scope=post-open-isolated initialSetupOnly=true {Describe(player, gate)}");

            MechanicalClawFrame? firstArrival = null, boost = null, released = null;
            ulong arrivalTick = 0, boostTick = 0, releaseTick = 0, requestTick = 0;
            Vector2 savedNormal = Vector2.Zero, boostBodyVelocity = Vector2.Zero;
            int publicationSequence = 0, boostSequence = -1, boostCount = 0, releaseCount = 0;
            bool spaceRequested = false;
            observer = (frame, target) =>
            {
                if (observerFailure != null) return;
                try
                {
                    publicationSequence++;
                    if (frame.CastId != 0)
                    {
                        if (castId == 0) castId = frame.CastId;
                        TestAssert.True(frame.CastId == castId, "all nonzero publications retain one CastId");
                    }
                    TestAssert.True(frame.Failure == MechanicalClawFailure.None && frame.State != MechanicalClawState.Failed,
                        "actual cast never publishes a failure");
                    TestAssert.True(player.GetInstanceId() == playerId && Health(player) == 5 &&
                        motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes,
                        "preMotor publication keeps Player identity HP and ordinary action counters");
                    if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
                    {
                        TestAssert.True(target == intended && frame.LockedTargetRuntimeId == intended.GetInstanceId() &&
                            frame.AnchorPoint.X >= 5805 && frame.AnchorPoint.X <= 5855 &&
                            MathF.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f,
                            "attached actual cast locks the literal intended beam underside");
                        if (frame.Arrived && firstArrival == null)
                        {
                            firstArrival = frame; arrivalTick = Engine.GetPhysicsFrames(); savedNormal = frame.SurfaceNormal.Normalized();
                            GD.Print($"TASK18_TEMPLE_SPACE_EXIT_ARRIVAL delay={delay} tick={arrivalTick} boundary=preMotorEvent body={Bounds(player)} velocity={player.Velocity} {WestClaw(frame)}");
                        }
                    }
                    if (frame.State == MechanicalClawState.Boost)
                    {
                        boostCount++; boost = frame; boostTick = Engine.GetPhysicsFrames(); boostSequence = publicationSequence;
                        boostBodyVelocity = player.Velocity;
                        TestAssert.True(spaceRequested && firstArrival != null && boostCount == 1 && releaseCount == 0 &&
                            frame.BoostConsumed && frame.ReleaseKind == MechanicalClawReleaseKind.Boost,
                            "only the requested single Space can publish one consumed Boost");
                    }
                    if (frame.State == MechanicalClawState.Released)
                    {
                        releaseCount++; released = frame; releaseTick = Engine.GetPhysicsFrames();
                        TestAssert.True(spaceRequested && boostCount == 1 && releaseCount == 1 && boostSequence + 1 == publicationSequence &&
                            boostTick == releaseTick && releaseTick == requestTick + 1 && boost.Value.CastId == frame.CastId &&
                            frame.BoostConsumed && frame.ReleaseKind == MechanicalClawReleaseKind.Boost &&
                            boostBodyVelocity.DistanceTo(player.Velocity) < .001f,
                            "one physics-tick transaction publishes Boost immediately followed by Released before motor application");
                    }
                    if (frame.State is MechanicalClawState.Boost or MechanicalClawState.Released)
                        GD.Print($"TASK18_TEMPLE_SPACE_EXIT_PUBLICATION delay={delay} tick={Engine.GetPhysicsFrames()} boundary=preMotorEvent sequence={publicationSequence} bodyVelocity={player.Velocity} body={Bounds(player)} {WestClaw(frame)}");
                }
                catch (Exception exception) { observerFailure = exception; }
            };
            claw.ClawFrameChanged += observer; // Before real E/LMB; no production adapter replacement.
            ActionKey(player, Key.E, true); await Frame(tree); ActionKey(player, Key.E, false); Invariant();
            TestAssert.True(player.GetNode<PlayerActionModeController>("ActionModes").CanBeginGrapple, "real E selects grapple mode");
            Vector2 aim = new(5828, 840);
            ulong pointerSequence = input.MouseMotionSequence;
            ulong castRequestTick = Engine.GetPhysicsFrames();
            Mouse(player, true, aim); await Frame(tree); Mouse(player, false, aim); Invariant();
            TestAssert.True(input.MouseMotionSequence > pointerSequence && input.CurrentFrame.MouseWorld.DistanceTo(aim) < 1,
                "real one-frame LMB and mouse adapter deliver exact fixed world aim");
            for (int i = 0; firstArrival == null && i < 299; i++)
            {
                TestAssert.True(claw.CurrentFrame.State is not (MechanicalClawState.Released or MechanicalClawState.Failed or
                    MechanicalClawState.Retract or MechanicalClawState.Idle),
                    "no early terminal state before first true arrival");
                await Frame(tree); Invariant();
            }
            Invariant();
            TestAssert.True(firstArrival != null && castId != 0 && claw.CurrentFrame.Arrived &&
                claw.CurrentFrame.State == MechanicalClawState.Swing && arrivalTick == Engine.GetPhysicsFrames(),
                "bounded first true arrival is observed postMotor without an extra timing frame");
            TestAssert.True(arrivalTick <= castRequestTick + 300, "actual arrival physics tick is within300 of real cast request");
            void PreReleaseSample(int samples)
            {
                Invariant();
                TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Swing && boostCount == 0 && releaseCount == 0,
                    "fixed D delay stays attached until single requested Space");
                GD.Print($"TASK18_TEMPLE_SPACE_EXIT_PRE_RELEASE delay={delay} samples={samples} tick={Engine.GetPhysicsFrames()} boundary=postMotorObservation position={player.GlobalPosition} velocity={player.Velocity} feet={Bounds(player).End.Y} anchor={claw.CurrentFrame.AnchorPoint} correction={claw.CurrentFrame.VelocityCorrection} contacts={Contacts(player)}");
            }
            PreReleaseSample(0);
            ulong dRequestTick = Engine.GetPhysicsFrames();
            ActionKey(player, Key.D, true);
            GD.Print($"TASK18_TEMPLE_SPACE_EXIT_D_REQUEST delay={delay} tick={dRequestTick} arrivalTick={arrivalTick} boundary=postMotorBeforeNextPhysics");
            for (int samples = 1; samples <= delay; samples++) { await Frame(tree); PreReleaseSample(samples); }
            requestTick = Engine.GetPhysicsFrames();
            TestAssert.True(dRequestTick == arrivalTick && requestTick - dRequestTick == (ulong)delay,
                "Space dispatch minus D dispatch actual physics ticks equals exactly predeclared0/12/24");
            spaceRequested = true;
            GD.Print($"TASK18_TEMPLE_SPACE_EXIT_REQUEST delay={delay} samples={delay} tick={requestTick} dTick={dRequestTick} actualDelayTicks={requestTick - dRequestTick} boundary=postMotorBeforeNextPhysics sameInputBatch={(delay == 0)} {Describe(player, gate)}");
            ActionKey(player, Key.Space, true); // delay0: D and Space before the next awaited frame.
            Vector2 releaseStart = player.GlobalPosition;
            for (int held = 1; held <= 3; held++)
            {
                await Frame(tree); Invariant();
                TestAssert.True(boostCount == 1 && releaseCount == 1 && released != null &&
                    float.IsFinite(player.Velocity.X) && float.IsFinite(player.Velocity.Y) &&
                    player.Velocity.Length() <= 900.01f && player.Velocity.Dot(savedNormal) >= -.01f,
                    "postMotor Space release is finite capped900 and removes inward target-face velocity");
                GD.Print($"TASK18_TEMPLE_SPACE_EXIT_POST_RELEASE delay={delay} heldFrames={held} tick={Engine.GetPhysicsFrames()} releaseTick={releaseTick} boundary=postMotorObservation displacement={player.GlobalPosition - releaseStart} rightwardVelocity={player.Velocity.X} {Describe(player, gate)}");
            }
            ActionKey(player, Key.Space, false);
            await Walk(tree, player, 5896, 90, Invariant); // D stays held until landing approach, then ordinary helper releases it.
            for (int i = 0; i < 90 && !Contact(player, shelf, true); i++)
            {
                await Frame(tree); Invariant();
                TestAssert.True(!OriginalFloor(world, player), $"fixed delay{delay} missed shelf and returned lower floor: {Describe(player, gate)}");
            }
            for (int i = 0; i < 12; i++)
            {
                await Frame(tree); Invariant();
                Rect2 bounds = Bounds(player);
                TestAssert.True(Contact(player, shelf, true) && bounds.Position.X >= 5865 && bounds.End.X <= 5930 &&
                    MathF.Abs(bounds.End.Y - 960) <= 1 &&
                    gate.PlateWorldRect.HasPoint(new Vector2(player.GlobalPosition.X, bounds.End.Y - .1f)) &&
                    gate.HasOpened && gate.IsOpen && gate.Frame.Traversable && gate.DoorCollision.Disabled && !DoorRayHits(player, gate),
                    $"delay{delay} full72x40 body has12 consecutive grounded external shelf samples with real plate and committed clear door: {Describe(player, gate)}");
                GD.Print($"TASK18_TEMPLE_SPACE_EXIT_LANDING delay={delay} stableSample={i + 1} tick={Engine.GetPhysicsFrames()} boundary=postMotorObservation {Describe(player, gate)}");
            }
            await Walk(tree, player, 6010, 90, () =>
            {
                Invariant();
                TestAssert.True(Contact(player, shelf, true) && gate.IsOpen && gate.Frame.Traversable &&
                    gate.DoorCollision.Disabled && !DoorRayHits(player, gate), "ordinary chamber entry stays supported with physical open door");
            });
            await Walk(tree, player, 5896, 90, () => { Invariant(); TestAssert.True(Contact(player, shelf, true), "ordinary return stays on shelf"); });
            await Walk(tree, player, 5825, 90, Invariant);
            for (int i = 0; i < 90 && !OriginalFloor(world, player); i++) { await Frame(tree); Invariant(); }
            Invariant();
            TestAssert.True(OriginalFloor(world, player) && boostCount == 1 && releaseCount == 1,
                "samePlayer ordinary chamber walk and drop regain lower floor with one Boost release and no extra action");
            GD.Print($"TASK18_TEMPLE_SPACE_EXIT_RETURN delay={delay} fixture=true fullRoute=false rewardPersistenceClaim=false closingDoorClaim=false {Describe(player, gate)}");
        }
        catch (Exception exception)
        {
            failure = exception;
            if (GodotObject.IsInstanceValid(player) && player.IsInsideTree())
                GD.Print($"TASK18_TEMPLE_SPACE_EXIT_FAILURE delay={delay} boundary=postMotorObservation position={player.GlobalPosition} velocity={player.Velocity} bounds={Bounds(player)} contacts={Contacts(player)} {Claw(player)}");
        }
        finally
        {
            // Do not let cleanup overwrite the diagnostic; do not mount another trial unless the old root is gone.
            try
            {
                if (observerFailure != null) throw new InvalidOperationException("Space-exit late observer failure before unsubscribe", observerFailure);
            }
            catch (Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }
            try { if (claw != null && observer != null) claw.ClawFrameChanged -= observer; }
            catch (Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }
            try { ReleaseInputs(player); }
            catch (Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }
            try
            {
                if (GodotObject.IsInstanceValid(world))
                {
                    if (world.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, world);
                    else world.Free();
                }
                safelyFreed = !GodotObject.IsInstanceValid(world);
                TestAssert.True(safelyFreed, "Space-exit scene is actually freed after awaited teardown");
            }
            catch (Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }
        }
        return (failure, safelyFreed);
    }
}
