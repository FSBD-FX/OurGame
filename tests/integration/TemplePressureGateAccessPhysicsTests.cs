using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Laborer;
using Knight.Mechanisms;
using Knight.Player;

namespace Knight.Tests.Integration;

// Extracted, post-BellChain-open geometry only. No reward/flow/encounter or region proof.
public static partial class TemplePressureGateAccessPhysicsTests
{
    private static readonly Vector2 Offset = new(5120, 720);
    private const float ShelfLeft = 5865, ShelfRight = 6080;

    public static Task Access(SceneTree tree) => AccessCore(tree, false);
    public static Task JumpAccess(SceneTree tree) => AccessCore(tree, true);
    public static Task WestAccess(SceneTree tree) => AccessCore(tree, false, true);

    private static async Task AccessCore(SceneTree tree, bool jumpApproach, bool westAccess = false, bool closedChain = false)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Node2D world = null;
        Node2D teardownRoot = null;
        ClosedChainObservation closed = null;
        string accessScope = closedChain ? "mounted-closed-chain-to-post-open" : "post-open-isolated";
        CharacterBody2D player = null;
        TempleClawLosObserver los = null;
        MechanicalClawController westObservedClaw = null;
        Action<MechanicalClawFrame, GrappleSurface2D> westObserver = null;
        Exception westObserverFailure = null;
        try
        {
            CanyonPressurePlateGate gate;
            if (closedChain)
                teardownRoot = AssembleClosedChain(out world, out player, out gate, out closed);
            else
                teardownRoot = world = Assemble(new Vector2(5820, 1080), null, out player, out gate, out _);
            var observedClaw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            los = new TempleClawLosObserver { Name = "Task18PreClawLOS", Player = player, Claw = observedClaw,
                Intended = world.GetNode<GrappleSurface2D>("Collision/TempleSupplyGripBeam/GrappleSurfaceProfile"),
                ProcessPhysicsPriority = observedClaw.ProcessPhysicsPriority };
            player.AddChild(los);
            player.MoveChild(los, observedClaw.GetIndex());
            los.Subscribe();
            tree.Root.AddChild(teardownRoot);
            closed?.ApplyUpstreamFixture();
            los.ThrowIfFailed();
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, null);
            await WaitFloor(tree, player, world, 90);
            int hp = Health(player);
            if (closedChain) await ClosedChainPrelude(tree, world, player, gate, closed, hp);
            TestAssert.True(!gate.IsOpen && !gate.HasOpened && DoorRayHits(player, gate),
                "closed installed door physically blocks the same later open-door query");
            await LowerWalk(tree, world, player, gate, 6125, hp, "left-to-right");
            await LowerWalk(tree, world, player, gate, 5820, hp, "right-to-left");
            await Walk(tree, player, 5980, 90, () => LowerInvariant(world, player, gate, hp));
            int jumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
            ActionKey(player, Key.Space, true);
            bool leftFloor = false, ceilingContact = false;
            for (int i = 0; i < 90; i++)
            {
                await Frame(tree);
                if (i == 3) ActionKey(player, Key.Space, false);
                leftFloor |= !player.IsOnFloor();
                ceilingContact |= Contact(player, world.GetNode<StaticBody2D>("Collision/TempleSupplyShelf"), false);
                TestAssert.True(Health(player) == hp && !gate.HasOpened && !gate.IsOpen,
                    "ordinary below-shelf jump neither injures player nor activates upper plate");
                AssertNoEmbedding(player);
                if (leftFloor && OriginalFloor(world, player)) break;
            }
            ActionKey(player, Key.Space, false);
            TestAssert.True(leftFloor && ceilingContact && OriginalFloor(world, player) &&
                player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumps + 1,
                "ordinary Space jump contacts actual shelf underside and returns to original floor");
            GD.Print($"TASK18_TEMPLE_BELOW_SHELF_JUMP_OK {Describe(player, gate)}");

            await Walk(tree, player, 5825, 90, () => LowerInvariant(world, player, gate, hp));
            await Frame(tree, 8);
            TestAssert.True(OriginalFloor(world, player), "claw approach is supported on unchanged lower floor");
            if (closedChain) GD.Print($"TASK18_TEMPLE_CLAW_APPROACH scope={accessScope} initialChainClosed=true genuineChainInput=true rewardPresent=true rewardBound=false {Describe(player, gate)}");
            else GD.Print($"TASK18_TEMPLE_CLAW_APPROACH postBellChainOpen=true initialSetupOnly=true {Describe(player, gate)}");
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            var beam = world.GetNode<StaticBody2D>("Collision/TempleSupplyGripBeam");
            var intended = beam.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            Vector2 aim = westAccess ? new Vector2(5828, 840) : new Vector2(5884, 840);
            ActionKey(player, Key.E, true);
            await Frame(tree);
            ActionKey(player, Key.E, false);
            TestAssert.True(player.GetNode<PlayerActionModeController>("ActionModes").CanBeginGrapple,
                "actual E selects grapple mode");
            int approachJumps = 0, approachDashes = 0;
            MechanicalClawReleaseKind approachRelease = MechanicalClawReleaseKind.None;
            ulong westCastId = 0;
            int westReleasePublications = 0;
            MechanicalClawFrame? westRelease = null;
            bool westBoostObserved = false;
            if (westAccess)
            {
                approachJumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
                approachDashes = player.GetNode<PlayerMotor>("Motor").AcceptedDashCount;
                westObservedClaw = claw;
                westObserver = (frame, target) =>
                {
                    if (westObserverFailure != null) return;
                    try
                    {
                        westBoostObserved |= frame.BoostConsumed || frame.ReleaseKind == MechanicalClawReleaseKind.Boost;
                        AssertWestOrdinary(player, hp, approachJumps, approachDashes, westBoostObserved);
                        if (frame.CastId != 0)
                        {
                            if (westCastId == 0) westCastId = frame.CastId;
                            TestAssert.True(frame.CastId == westCastId, "west access publishes only the single ordinary cast identity");
                        }
                        if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
                            TestAssert.True(target == intended && frame.LockedTargetRuntimeId == intended.GetInstanceId() &&
                                frame.AnchorPoint.X >= 5805 && frame.AnchorPoint.X <= 5855 &&
                                MathF.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f,
                                "every west attached publication identifies intended beam underside and literal anchor bounds");
                        if (frame.State != MechanicalClawState.Released) return;
                        westReleasePublications++;
                        westRelease = frame;
                        GD.Print($"TASK18_TEMPLE_WEST_RELEASE_PUBLICATION scope={accessScope} tick={Engine.GetPhysicsFrames()} publicationBoundary=preMotor target={target?.GetPath()} {Describe(player, gate)} {WestClaw(frame)}");
                    }
                    catch (Exception exception) { westObserverFailure = exception; }
                };
                claw.ClawFrameChanged += westObserver;
                if (closedChain) GD.Print($"TASK18_TEMPLE_WEST_APPROACH scope={accessScope} initialChainClosed=true genuineChainInput=true jumps={approachJumps} dashes={approachDashes} aim={aim} {Describe(player, gate)}");
                else GD.Print($"TASK18_TEMPLE_WEST_APPROACH scope=post-open-isolated initialSetupOnly=true jumps={approachJumps} dashes={approachDashes} aim={aim} {Describe(player, gate)}");
            }
            void WestInvariant()
            {
                closed?.AssertOpened(player, hp);
                if (!westAccess) return;
                if (westObserverFailure != null)
                    throw new InvalidOperationException("West access frame publication invariant failed", westObserverFailure);
                AssertWestOrdinary(player, hp, approachJumps, approachDashes, westBoostObserved);
            }
            if (jumpApproach)
            {
                approachJumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount;
                approachDashes = player.GetNode<PlayerMotor>("Motor").AcceptedDashCount;
                ActionKey(player, Key.Space, true);
                for (int i = 0; i < 8; i++)
                {
                    await Frame(tree);
                    los.ThrowIfFailed();
                    TestAssert.True(Health(player) == hp, "ordinary jump approach preserves health");
                    AssertNoEmbedding(player);
                    if (player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == approachJumps + 1 && !player.IsOnFloor()) break;
                }
                TestAssert.True(player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == approachJumps + 1 &&
                    player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == approachDashes && !player.IsOnFloor(),
                    "one ordinary Space jump is accepted and leaves actual floor before same-aim cast");
                GD.Print($"TASK18_TEMPLE_JUMP_CLAW_APPROACH_STARTED jumpApproach=true jumps={player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount} dashes={player.GetNode<PlayerMotor>("Motor").AcceptedDashCount} aim={aim} {Describe(player, gate)}");
            }
            ulong pointerSequence = player.GetNode<PlayerInputReader>("Input").MouseMotionSequence;
            Mouse(player, true, aim);
            await Frame(tree);
            WestInvariant();
            TestAssert.True(player.GetNode<PlayerInputReader>("Input").MouseMotionSequence > pointerSequence &&
                player.GetNode<PlayerInputReader>("Input").CurrentFrame.MouseWorld.DistanceTo(aim) < 1,
                "real viewport mouse events reach production input adapter and intended world aim");
            Mouse(player, false, aim);
            bool attached = false, arrived = false;
            MechanicalClawState last = (MechanicalClawState)(-1);
            for (int i = 0; i < 300; i++)
            {
                await Frame(tree);
                los.ThrowIfFailed();
                var frame = claw.CurrentFrame;
                WestInvariant();
                if (westAccess && i % 30 == 0)
                    GD.Print($"TASK18_TEMPLE_WEST_PULL_SAMPLE frame={i} scope={accessScope} {Describe(player, gate)} {WestClaw(frame)}");
                if (jumpApproach)
                    TestAssert.True(player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == approachJumps + 1 &&
                        player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == approachDashes && !frame.BoostConsumed &&
                        player.GetNode<PlayerInputReader>("Input").CurrentFrame.JumpHeld,
                        "jump-assisted pull holds same Space, no additional jump/dash/Boost");
                if (frame.State != last)
                {
                    GD.Print($"TASK18_TEMPLE_CLAW_TRANSITION frame={i} {Describe(player, gate)} {Claw(player)}");
                    if (westAccess) GD.Print($"TASK18_TEMPLE_WEST_PULL frame={i} scope={accessScope} {Describe(player, gate)} {WestClaw(frame)}");
                    last = frame.State;
                }
                TestAssert.True(Health(player) == hp, "actual claw approach preserves health");
                AssertNoEmbedding(player);
                if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
                {
                    attached = true;
                    TestAssert.True(claw.CurrentTarget == intended && frame.LockedTargetRuntimeId == intended.GetInstanceId() &&
                        frame.AnchorPoint.X >= (westAccess ? 5805 : 5858) && frame.AnchorPoint.X <= (westAccess ? 5855 : 5930) &&
                        MathF.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f,
                        "real cast locks intended beam underside and actual collision anchor");
                }
                if (frame.State is MechanicalClawState.Failed or MechanicalClawState.Released or
                    MechanicalClawState.Retract or MechanicalClawState.Idle)
                    throw new InvalidOperationException($"Single intended claw cast terminated before arrival: cast={frame.CastId} lastAttachedTarget={los.LastAttachedTargetPath} {Describe(player, gate)} {Claw(player)}");
                if (frame.Arrived) { arrived = true; break; }
            }
            TestAssert.True(attached && arrived, $"single intended claw pull reaches actual underside dock: {Claw(player)}");
            if (westAccess)
            {
                TestAssert.True(westCastId != 0 && claw.CurrentFrame.CastId == westCastId,
                    "west genuine arrival belongs to the one acquired ordinary cast");
                GD.Print($"TASK18_TEMPLE_WEST_ARRIVAL scope={accessScope} {Describe(player, gate)} {WestClaw(claw.CurrentFrame)}");
                // One coarse body-sized spatial rule, not a tuned frame or pixel window:
                // move one capsule radius east of the anchor while retaining 20px shelf clearance.
                ActionKey(player, Key.D, true);
                bool releaseReady = false;
                for (int i = 0; i < 90; i++)
                {
                    await Frame(tree);
                    los.ThrowIfFailed();
                    WestInvariant();
                    var frame = claw.CurrentFrame;
                    TestAssert.True(frame.CastId == westCastId && claw.CurrentTarget == intended &&
                        frame.LockedTargetRuntimeId == intended.GetInstanceId() &&
                        frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                        $"west D swing retains the acquired attached beam, no early release: {WestClaw(frame)}");
                    Rect2 bounds = Bounds(player);
                    float radius = bounds.Size.X / 2;
                    releaseReady = player.GlobalPosition.X >= frame.AnchorPoint.X + radius &&
                        bounds.End.Y <= 940 && player.Velocity.X > 0;
                    GD.Print($"TASK18_TEMPLE_WEST_SWING frame={i} scope={accessScope} releaseReady={releaseReady} rule=east-one-radius-foot-at-most-940-rightward {Describe(player, gate)} {WestClaw(frame)}");
                    if (releaseReady) break;
                }
                TestAssert.True(releaseReady, $"west D swing reaches coarse ordinary Cancel pose within 90 frames: {Describe(player, gate)} {WestClaw(claw.CurrentFrame)}");
            }
            // The underside dock is above the ledge: ordinary LMB cancel, then gravity and horizontal input.
            // No Space boost, second cast, hidden relocation or ordinary jump is used to rescue this landing.
            if (jumpApproach) ActionKey(player, Key.Space, false);
            Mouse(player, true, aim);
            await Frame(tree);
            WestInvariant();
            if (westAccess)
            {
                TestAssert.True(westReleasePublications == 1 && westRelease.HasValue &&
                    westRelease.Value.CastId == westCastId && westRelease.Value.LockedTargetRuntimeId == intended.GetInstanceId() &&
                    westRelease.Value.State == MechanicalClawState.Released &&
                    westRelease.Value.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                    westRelease.Value.Failure == MechanicalClawFailure.None && !westRelease.Value.BoostConsumed,
                    "west ordinary LMB publishes exactly one observed same-cast Cancel, never assumed from later state");
                approachRelease = westRelease.Value.ReleaseKind;
                GD.Print($"TASK18_TEMPLE_WEST_CANCEL_OK scope={accessScope} continuedD=true {Describe(player, gate)} {WestClaw(westRelease.Value)}");
            }
            if (jumpApproach)
            {
                approachRelease = claw.CurrentFrame.ReleaseKind;
                TestAssert.True(claw.CurrentFrame.State == MechanicalClawState.Released &&
                    approachRelease == MechanicalClawReleaseKind.Cancel && !claw.CurrentFrame.BoostConsumed,
                    "arrived jump-assisted cast uses ordinary Cancel, never Boost release");
            }
            Mouse(player, false, aim);
            await Walk(tree, player, 5896, 90, () => { TestAssert.True(Health(player) == hp, "landing HP stable"); AssertNoEmbedding(player); WestInvariant(); });
            var shelf = world.GetNode<StaticBody2D>("Collision/TempleSupplyShelf");
            for (int i = 0; i < 90 && !Contact(player, shelf, true); i++)
            {
                await Frame(tree);
                TestAssert.True(Health(player) == hp, "fall from actual dock preserves health");
                AssertNoEmbedding(player);
                WestInvariant();
            }
            for (int i = 0; i < 12; i++)
            {
                await Frame(tree);
                Rect2 bounds = Bounds(player);
                TestAssert.True(Contact(player, shelf, true) && bounds.Position.X >= 5865 && bounds.End.X <= 5930 &&
                    MathF.Abs(bounds.End.Y - 960) < 1 && Health(player) == hp &&
                    gate.PlateWorldRect.HasPoint(new Vector2(player.GlobalPosition.X, bounds.End.Y - .1f)),
                    "full capsule stably lands on actual external 65px shelf");
                AssertNoEmbedding(player);
                WestInvariant();
            }
            TestAssert.True(gate.HasOpened && gate.IsOpen && gate.Frame.Traversable && gate.DoorCollision.Disabled && !DoorRayHits(player, gate),
                "grounded plate commits open door and physical terrain query agrees");
            GD.Print($"TASK18_TEMPLE_EXTERNAL_LANDING_OK {Describe(player, gate)} {Claw(player)}");
            await Walk(tree, player, 6010, 90, () =>
            {
                TestAssert.True(Contact(player, shelf, true) && gate.IsOpen && gate.Frame.Traversable &&
                    gate.DoorCollision.Disabled && !DoorRayHits(player, gate) && Health(player) == hp,
                    "ordinary chamber entry remains supported with committed physically open door");
                AssertNoEmbedding(player);
                WestInvariant();
            });
            if (closedChain) GD.Print($"TASK18_TEMPLE_CHAMBER_ENTRY {Describe(player, gate)} rewardPresent=true rewardBound=false rewardPersistenceClaim=false");
            else GD.Print($"TASK18_TEMPLE_CHAMBER_ENTRY {Describe(player, gate)} rewardOmitted=true");
            await Walk(tree, player, 5896, 90, () => { TestAssert.True(Contact(player, shelf, true) && Health(player) == hp, "ordinary chamber return uses shelf"); AssertNoEmbedding(player); WestInvariant(); });
            await Walk(tree, player, 5825, 90, () => { TestAssert.True(Health(player) == hp, "ordinary drop preserves HP"); AssertNoEmbedding(player); WestInvariant(); });
            await WaitFloor(tree, player, world, 90);
            WestInvariant();
            TestAssert.True(Health(player) == hp, "return reaches unchanged lower support without damage");
            los.Unsubscribe();
            los.ThrowIfFailed();
            if (jumpApproach)
            {
                TestAssert.True(player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == approachJumps + 1 &&
                    player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == approachDashes &&
                    approachRelease == MechanicalClawReleaseKind.Cancel,
                    "complete jump-assisted access preserves exactly one approach jump and no dash/Boost");
                GD.Print($"TASK18_TEMPLE_JUMP_CLAW_APPROACH_OK jumpApproach=true fixture=true fullRoute=false jumpsDelta=1 dashesDelta=0 release={approachRelease} aim={aim} {Describe(player, gate)}");
                GD.Print($"TASK18_TEMPLE_JUMP_ACCESS_RETURN_OK fixture=true fullRoute=false {Describe(player, gate)}");
            }
            else if (westAccess)
            {
                TestAssert.True(westReleasePublications == 1 && approachRelease == MechanicalClawReleaseKind.Cancel,
                    "complete west access retains one observed Cancel publication");
                if (closedChain)
                {
                    closed.AssertOpened(player, hp);
                    TestAssert.True(OriginalFloor(world, player) && gate.HasOpened && gate.DoorCollision.Disabled,
                        "mounted closed-chain access physically entered supply chamber and regained lower support");
                    GD.Print($"TASK18_TEMPLE_CLOSED_CHAIN_ACCESS_RETURN_OK mode=closed-chain scope={accessScope} fixture=true fullRoute=false initialChainClosed=true chainApplications=1 chainOpenedEvents=1 rewardPresent=true rewardBound=false jumpsDelta=0 dashesDelta=0 Boost=false release={approachRelease} aim={aim} {Describe(player, gate)}");
                }
                else GD.Print($"TASK18_TEMPLE_WEST_ACCESS_RETURN_OK mode=west-access scope=post-open-isolated fixture=true fullRoute=false jumpsDelta=0 dashesDelta=0 Boost=false release={approachRelease} aim={aim} {Describe(player, gate)}");
            }
            else GD.Print($"TASK18_TEMPLE_ACCESS_RETURN_OK fixture=true fullRoute=false {Describe(player, gate)}");
        }
        catch
        {
            if (player != null && GodotObject.IsInstanceValid(player) && player.IsInsideTree())
                GD.Print($"TASK18_TEMPLE_ACCESS_FAILURE position={player.GlobalPosition} velocity={player.Velocity} bounds={Bounds(player)} contacts={Contacts(player)} {Claw(player)}");
            throw;
        }
        finally
        {
            try
            {
                if (westObservedClaw != null && westObserver != null) westObservedClaw.ClawFrameChanged -= westObserver;
                los?.Unsubscribe();
                closed?.UnsubscribeAndThrow();
                if (westObserverFailure != null)
                    throw new InvalidOperationException("West access frame publication invariant failed before cleanup", westObserverFailure);
            }
            finally
            {
                try { ReleaseInputs(player); }
                finally { if (teardownRoot != null) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, teardownRoot); }
            }
        }
    }

    private static void AssertWestOrdinary(CharacterBody2D player, int hp, int jumps, int dashes, bool boostObserved)
    {
        TestAssert.True(player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount == jumps &&
            player.GetNode<PlayerMotor>("Motor").AcceptedDashCount == dashes && !boostObserved &&
            !player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.BoostConsumed && Health(player) == hp,
            "every west access sample/publication preserves approach jump/dash counts, no Boost and real HP");
        AssertNoEmbedding(player);
    }

    private static string WestClaw(MechanicalClawFrame frame) =>
        $"cast={frame.CastId} claw={frame.State} failure={frame.Failure} locked={frame.LockedTargetRuntimeId} anchor={frame.AnchorPoint} normal={frame.SurfaceNormal} dock={frame.DockPoint} rope={frame.RopeLength} arrived={frame.Arrived} release={frame.ReleaseKind} Boost={frame.BoostConsumed} casterPose={frame.CasterWorldPosition} casterVelocity={frame.CasterVelocity} correction={frame.VelocityCorrection}";

    public static async Task LaborerClearance(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await LaborerDirection(tree, 1);
        await LaborerDirection(tree, -1);
    }

    private static async Task LaborerDirection(SceneTree tree, int direction)
    {
        Node2D world = null;
        CharacterBody2D player = null;
        BellBackLaborer enemy = null;
        Action<AttackPresentationFrame> observer = null;
        string label = direction > 0 ? "left-to-right" : "right-to-left";
        try
        {
            world = Assemble(new Vector2(direction > 0 ? 6205 : 5730, 1080),
                new Vector2(direction > 0 ? 5800 : 6140, 1074), out player, out var gate, out enemy);
            var phases = new List<(ulong Id, AttackPhase Phase)>();
            observer = frame =>
            {
                if (frame.Timeline.Phase == AttackPhase.None) return;
                var phase = (frame.Timeline.AttackId, frame.Timeline.Phase);
                if (phases.Count > 0 && phases[^1] == phase) return;
                phases.Add(phase);
                GD.Print($"TASK18_TEMPLE_LABORER_ATTACK direction={label} id={phase.Item1} phase={phase.Item2} enemy={enemy.GlobalPosition} player={player.GlobalPosition}");
            };
            enemy.AttackFrameChanged += observer;
            tree.Root.AddChild(world);
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, null);
            await WaitFloor(tree, player, world, 90);
            int hp = Health(player), enemyHp = Health(enemy), samples = 0;
            bool approach = false, crossed = false, dodging = false;
            float previous = enemy.GlobalPosition.X;
            TestAssert.True(direction > 0 ? Bounds(enemy).End.X < ShelfLeft : Bounds(enemy).Position.X > ShelfRight,
                "real Laborer starts fully outside beneath-shelf interval");
            GD.Print($"TASK18_TEMPLE_LABORER_START direction={label} fixture=true encounterPursuit=false enemy={enemy.GlobalPosition} bounds={Bounds(enemy)} hp={enemyHp} player={player.GlobalPosition} playerHp={hp}");
            // 700 frames: full ~315px clearance at unchanged 70px/s, guard/telegraph/active/recovery plus margin.
            for (int i = 0; i < 700; i++)
            {
                await Frame(tree);
                TestAssert.True(MathF.Abs(enemy.GlobalPosition.X - previous) <= 4 && Health(enemy) == enemyHp && Health(player) == hp &&
                    enemy.CurrentState != BellBackLaborerState.Dead && !gate.HasOpened,
                    "real Laborer clearance has no teleport, death, damage or upper plate activation");
                previous = enemy.GlobalPosition.X;
                AssertNoEmbedding(enemy);
                approach |= enemy.CurrentState == BellBackLaborerState.Approach;
                Rect2 bounds = Bounds(enemy);
                if (bounds.End.X >= ShelfLeft && bounds.Position.X <= ShelfRight)
                {
                    samples++;
                    TestAssert.True(OriginalFloor(world, enemy) && MathF.Abs(bounds.End.Y - 1116) < 1,
                        "every beneath-shelf Laborer sample contacts unchanged original lower floor height");
                }
                crossed |= direction > 0 ? bounds.Position.X > ShelfRight : bounds.End.X < ShelfLeft;
                // Genuine telegraph, not invulnerability: ordinary movement away before the real Active hitbox.
                if (!dodging && phases.Any(p => p.Phase == AttackPhase.Telegraph))
                {
                    TestAssert.True(crossed, "genuine attack begins only after full beneath-shelf traversal");
                    ActionKey(player, direction > 0 ? Key.D : Key.A, true);
                    dodging = true;
                }
                if (dodging && MathF.Abs(player.GlobalPosition.X - (direction > 0 ? 6205 : 5730)) >= 120)
                    ActionKey(player, direction > 0 ? Key.D : Key.A, false);
                bool ordered = phases.Any(p => p.Phase == AttackPhase.Telegraph &&
                    phases.FindIndex(q => q.Id == p.Id && q.Phase == AttackPhase.Active) > phases.IndexOf(p) &&
                    phases.FindIndex(q => q.Id == p.Id && q.Phase == AttackPhase.Recovery) >
                    phases.FindIndex(q => q.Id == p.Id && q.Phase == AttackPhase.Active));
                if (crossed && ordered)
                {
                    TestAssert.True(approach && samples > 150 && OriginalFloor(world, enemy),
                        "real AI Approach traverses entire interval on original floor and reaches ordered same-ID attack recovery");
                    GD.Print($"TASK18_TEMPLE_LABORER_CLEARANCE_OK direction={label} fixture=true encounterPursuit=false samples={samples} enemy={enemy.GlobalPosition} bounds={Bounds(enemy)} floor={Contacts(enemy)} enemyHp={Health(enemy)} playerHp={Health(player)}");
                    return;
                }
            }
            throw new InvalidOperationException($"Laborer {label} bounded clearance/attack not reached: enemy={enemy.GlobalPosition} velocity={enemy.Velocity} bounds={Bounds(enemy)} state={enemy.CurrentState} contacts={Contacts(enemy)} player={player.GlobalPosition} enemyHp={Health(enemy)} playerHp={Health(player)} phases={string.Join(';', phases)}");
        }
        catch
        {
            if (enemy != null && GodotObject.IsInstanceValid(enemy) && enemy.IsInsideTree())
                GD.Print($"TASK18_TEMPLE_LABORER_FAILURE direction={label} enemy={enemy.GlobalPosition} velocity={enemy.Velocity} state={enemy.CurrentState} bounds={Bounds(enemy)} contacts={Contacts(enemy)} hp={Health(enemy)} player={player?.GlobalPosition} playerHp={Health(player)}");
            throw;
        }
        finally
        {
            try { if (enemy != null && observer != null) enemy.AttackFrameChanged -= observer; }
            finally
            {
                try { ReleaseInputs(player); }
                finally { if (world != null) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, world); }
            }
        }
    }

    private static Node2D Assemble(Vector2 playerPose, Vector2? enemyPose, out CharacterBody2D player,
        out CanyonPressurePlateGate gate, out BellBackLaborer enemy)
    {
        var world = new Node2D { Name = "Task18ExtractedTemple", Position = Offset };
        Node sector = null;
        player = null; gate = null; enemy = null;
        try
        {
            sector = GD.Load<PackedScene>("res://scenes/prologue/sectors/LostTemple.tscn").Instantiate();
            var collision = sector.GetNode<Node2D>("Gameplay/Collision");
            gate = sector.GetNode<CanyonPressurePlateGate>("Gameplay/Mechanisms/TemplePressureGate");
            ClearExternalOwners(collision, collision);
            ClearExternalOwners(gate, gate);
            collision.GetParent().RemoveChild(collision); world.AddChild(collision);
            gate.GetParent().RemoveChild(gate); world.AddChild(gate);
            player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position = playerPose - Offset; world.AddChild(player); player.AddToGroup("player");
            var playerCapsule = (CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            TestAssert.True(playerCapsule.Height == 72 && playerCapsule.Radius == 20,
                "complete packed Player retains real 72px capsule");
            gate.PlayerBody = player;
            if (enemyPose.HasValue)
            {
                enemy = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn").Instantiate<BellBackLaborer>();
                enemy.Position = enemyPose.Value - Offset; world.AddChild(enemy);
                var enemyCapsule = (CapsuleShape2D)enemy.GetNode<CollisionShape2D>("BodyCollider").Shape;
                TestAssert.True(enemyCapsule.Height == 84 && enemyCapsule.Radius == 28 && enemy.Tuning.MoveSpeed == 70,
                    "complete packed Laborer retains real 84px capsule and ordinary AI speed");
            }
            GD.Print($"TASK18_TEMPLE_SETUP fixture=true postBellChainOpen=true encounterPursuit=false parent={Offset} player={playerPose} enemy={enemyPose} poseWritesBeforeMountOnly=true rewardFlowOmitted=true");
            return world;
        }
        catch { world.Free(); throw; }
        finally { sector?.Free(); }
    }

    private static void ClearExternalOwners(Node node, Node retainedRoot)
    {
        Node owner = node.Owner;
        if (owner != null && owner != retainedRoot && !retainedRoot.IsAncestorOf(owner)) node.Owner = null;
        TestAssert.True(node.Owner == null || node.Owner == retainedRoot || retainedRoot.IsAncestorOf(node.Owner),
            "retained fixture owner never points into discarded sector");
        var children = node.GetChildren();
        using var childrenOwner = (Godot.Collections.Array)children;
        foreach (Node child in children) ClearExternalOwners(child, retainedRoot);
    }

    private static async Task LowerWalk(SceneTree tree, Node2D world, CharacterBody2D player,
        CanyonPressurePlateGate gate, float target, int hp, string direction)
    {
        TestAssert.True(target > player.GlobalPosition.X ? Bounds(player).End.X < ShelfLeft : Bounds(player).Position.X > ShelfRight,
            "lower Player traversal starts fully outside shelf interval");
        int samples = 0;
        GD.Print($"TASK18_TEMPLE_LOWER_START direction={direction} {Describe(player, gate)}");
        await Walk(tree, player, target, 120, () =>
        {
            LowerInvariant(world, player, gate, hp);
            Rect2 bounds = Bounds(player);
            if (bounds.End.X >= ShelfLeft && bounds.Position.X <= ShelfRight) samples++;
        });
        TestAssert.True(samples > 50 && (target > 6000 ? Bounds(player).Position.X > ShelfRight : Bounds(player).End.X < ShelfLeft),
            "real lower Player crosses complete beneath-shelf interval, not endpoints only");
        GD.Print($"TASK18_TEMPLE_LOWER_CLEARANCE_OK direction={direction} fixture=true fullRoute=false samples={samples} {Describe(player, gate)}");
    }

    private static void LowerInvariant(Node2D world, CharacterBody2D player, CanyonPressurePlateGate gate, int hp)
    {
        TestAssert.True(OriginalFloor(world, player) && MathF.Abs(Bounds(player).End.Y - 1116) < 1 &&
            Health(player) == hp && !gate.HasOpened && !gate.IsOpen,
            "every lower traversal sample uses original floor, preserves HP and leaves upper plate inactive");
        AssertNoEmbedding(player);
    }

    private static async Task Walk(SceneTree tree, CharacterBody2D player, float target, int budget, Action sample)
    {
        int direction = target > player.GlobalPosition.X ? 1 : -1;
        Key key = direction > 0 ? Key.D : Key.A;
        float previous = player.GlobalPosition.X;
        ActionKey(player, key, true);
        try
        {
            for (int i = 0; i < budget && (target - player.GlobalPosition.X) * direction > 2; i++)
            {
                await Frame(tree);
                TestAssert.True(MathF.Abs(player.GlobalPosition.X - previous) <= 9, "ordinary walking has no teleport");
                previous = player.GlobalPosition.X;
                sample();
            }
            TestAssert.True((target - player.GlobalPosition.X) * direction <= 2,
                $"bounded ordinary horizontal input reaches {target}, actual={player.GlobalPosition} contacts={Contacts(player)}");
        }
        finally { ActionKey(player, key, false); }
        await Frame(tree, 6);
        sample();
    }

    private static async Task WaitFloor(SceneTree tree, CharacterBody2D body, Node2D world, int budget)
    {
        for (int i = 0; i < budget; i++) { await Frame(tree); if (OriginalFloor(world, body)) return; }
        throw new InvalidOperationException($"No original lower support: position={body.GlobalPosition} velocity={body.Velocity} contacts={Contacts(body)}");
    }

    private static bool OriginalFloor(Node2D world, CharacterBody2D body) =>
        new[] { "CheckpointFloor", "ArenaFloor", "CourtyardStep", "CourtyardFloor" }
            .Any(name => Contact(body, world.GetNode<StaticBody2D>("Collision/" + name), true));

    private static bool Contact(CharacterBody2D body, Node support, bool floor)
    {
        if (floor && !body.IsOnFloor()) return false;
        for (int i = 0; i < body.GetSlideCollisionCount(); i++)
        {
            var hit = body.GetSlideCollision(i);
            if (hit.GetCollider() == support && (floor ? hit.GetNormal().Y < -.7f : hit.GetNormal().Y > .7f)) return true;
        }
        return false;
    }

    private static Rect2 Bounds(CharacterBody2D body)
    {
        var collider = body.GetNode<CollisionShape2D>("BodyCollider");
        var capsule = (CapsuleShape2D)collider.Shape;
        return new Rect2(collider.GlobalPosition - new Vector2(capsule.Radius, capsule.Height / 2),
            new Vector2(capsule.Radius * 2, capsule.Height));
    }

    private static void AssertNoEmbedding(CharacterBody2D body)
    {
        var collider = body.GetNode<CollisionShape2D>("BodyCollider");
        var capsule = (CapsuleShape2D)collider.Shape;
        using var inset = new CapsuleShape2D { Radius = capsule.Radius - .5f, Height = capsule.Height - 1 };
        using var query = new PhysicsShapeQueryParameters2D { Shape = inset, Transform = collider.GlobalTransform,
            CollisionMask = CombatCollisionLayers.World, CollideWithBodies = true, CollideWithAreas = false };
        var hits = body.GetWorld2D().DirectSpaceState.IntersectShape(query, 32);
        using var owner = (Godot.Collections.Array)hits;
        bool empty = hits.Count == 0;
        foreach (var hit in hits) hit.Dispose();
        TestAssert.True(empty, $"actual capsule inset is not embedded in solid terrain: {Bounds(body)} contacts={Contacts(body)}");
    }

    private static bool DoorRayHits(CharacterBody2D player, CanyonPressurePlateGate gate)
    {
        using var query = PhysicsRayQueryParameters2D.Create(new Vector2(5928, 924), new Vector2(5962, 924), CombatCollisionLayers.World);
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return false;
        TestAssert.True(hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == gate.GetNode<StaticBody2D>("Door"),
            "fixed door query has no intervening terrain receiver");
        return true;
    }

    private static int Health(Node body) => body.GetNode<HealthComponent>("Health").CurrentHealth;
    private static string Contacts(CharacterBody2D body) => string.Join(';', Enumerable.Range(0, body.GetSlideCollisionCount())
        .Select(i => { var hit = body.GetSlideCollision(i); return $"{(hit.GetCollider() as Node)?.Name}:{hit.GetNormal()}"; }));
    private static string Describe(CharacterBody2D player, CanyonPressurePlateGate gate) =>
        $"position={player.GlobalPosition} velocity={player.Velocity} bounds={Bounds(player)} floor={player.IsOnFloor()} contacts={Contacts(player)} hp={Health(player)} gateOpen={gate.IsOpen} traversable={gate.Frame.Traversable} doorDisabled={gate.DoorCollision.Disabled} opened={gate.HasOpened}";
    private static string Claw(CharacterBody2D player)
    {
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var input = player.GetNode<PlayerInputReader>("Input");
        return $"claw={claw.CurrentFrame.State} failure={claw.CurrentFrame.Failure} target={claw.CurrentTarget?.GetPath()} locked={claw.CurrentFrame.LockedTargetRuntimeId} anchor={claw.CurrentFrame.AnchorPoint} dock={claw.CurrentFrame.DockPoint} arrived={claw.CurrentFrame.Arrived} inputSequence={input.CurrentFrameSequence} pointerSequence={input.MouseMotionSequence} pointer={input.LastObservedMouseViewportPosition} jump={input.CurrentFrame.JumpPressed} primary={input.CurrentFrame.PrimaryPressed}";
    }

    private static async Task Frame(SceneTree tree, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); // observe after node physics/deferred gate commit
        }
    }

    private static void Dispatch(CharacterBody2D player, InputEvent input)
    {
        Input.ParseInputEvent(input); player.GetViewport().PushInput(input, true); Input.FlushBufferedEvents();
    }

    private static void ActionKey(CharacterBody2D player, Key key, bool pressed) =>
        Dispatch(player, new InputEventKey { PhysicalKeycode = key, Pressed = pressed });

    private static void ReleaseInputs(CharacterBody2D player)
    {
        try
        {
            if (player != null && GodotObject.IsInstanceValid(player) && player.IsInsideTree())
            {
                foreach (Key key in new[] { Key.A, Key.D, Key.Space, Key.E }) ActionKey(player, key, false);
                Dispatch(player, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false });
            }
        }
        finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
    }

    private static void Mouse(CharacterBody2D player, bool pressed, Vector2 aim)
    {
        Viewport viewport = player.GetViewport();
        Vector2 pointer = viewport.GetFinalTransform() * (player.GetGlobalTransformWithCanvas() * player.ToLocal(aim));
        TestAssert.True(player.GetViewportRect().HasPoint(pointer), $"actual mouse target is on screen: pointer={pointer}");
        Dispatch(player, new InputEventMouseMotion { Position = pointer, GlobalPosition = pointer });
        Dispatch(player, new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed,
            Position = pointer, GlobalPosition = pointer, ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0 });
    }
}

// Passive synchronous boundary: same-priority preceding sibling observes PRE-Claw,
// previous-tick POST-Motor pose; the existing event observes publication before this tick's Motor.
internal partial class TempleClawLosObserver : Node
{
    internal CharacterBody2D Player;
    internal MechanicalClawController Claw;
    internal GrappleSurface2D Intended;
    internal string LastAttachedTargetPath { get; private set; } = "none";
    private Exception _failure;
    private bool _subscribed, _blockedLogged, _releaseLogged;
    private Witness _before;
    private readonly MechanicalClawTargetResolver _resolver = new();

    private sealed record Witness(ulong Tick, Transform2D Pose, Vector2 Velocity, MechanicalClawFrame Frame,
        MechanicalClawTargetSnapshot Snapshot, Transform2D TargetPose, Vector2 TargetShapeSize,
        bool NoCancelOrBoostInputs, bool RayClear, string FirstPath, string FirstRid, Vector2 HitPoint, Vector2 HitNormal);

    internal void Subscribe() { Claw.ClawFrameChanged += Published; _subscribed = true; }
    internal void Unsubscribe() { if (_subscribed) { Claw.ClawFrameChanged -= Published; _subscribed = false; } }
    internal void ThrowIfFailed()
    {
        if (_failure != null) throw new InvalidOperationException("Task18 passive pre-Claw LOS diagnostic failed", _failure);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_failure != null) return;
        try
        {
            TestAssert.True(GetParent() == Player && Claw.GetParent() == Player && GetIndex() + 1 == Claw.GetIndex() &&
                ProcessThreadGroup == ProcessThreadGroupEnum.Inherit && Player.ProcessThreadGroup == ProcessThreadGroupEnum.Inherit &&
                ProcessPhysicsPriority == Claw.ProcessPhysicsPriority && ProcessPhysicsPriority == -100 &&
                Player.GetNode<Node>("Input").ProcessPhysicsPriority == -300 &&
                Player.GetNode<Node>("ActionModes").ProcessPhysicsPriority == -200 &&
                Player.GetNode<Node>("Motor").ProcessPhysicsPriority == 0,
                "LOS observer is adjacent pre-Claw sibling without changing original physics ordering");
            for (Node node = this; node != null; node = node.GetParent())
                TestAssert.True(node.ProcessThreadGroup == ProcessThreadGroupEnum.Inherit ||
                    node.ProcessThreadGroup == ProcessThreadGroupEnum.MainThread, "LOS observer inherits main-thread processing");
            TestAssert.True(Claw.ProcessThreadGroup == ProcessThreadGroupEnum.Inherit &&
                Player.GetNode<Node>("Motor").ProcessThreadGroup == ProcessThreadGroupEnum.Inherit &&
                Player.GetNode<Node>("Input").ProcessThreadGroup == ProcessThreadGroupEnum.Inherit &&
                Player.GetNode<Node>("ActionModes").ProcessThreadGroup == ProcessThreadGroupEnum.Inherit,
                "production siblings share inherited main-thread group");
            _before = null;
            MechanicalClawFrame frame = Claw.CurrentFrame;
            GrappleSurface2D target = Claw.CurrentTarget;
            if (frame.State is not (MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing) ||
                !GodotObject.IsInstanceValid(target) || target != Intended) return;
            LastAttachedTargetPath = target.GetPath().ToString();
            var snapshot = _resolver.SnapshotLocked(Player, target, frame.AnchorPoint, frame.SurfaceNormal);
            var excluded = new Godot.Collections.Array<Rid>();
            using var excludedOwner = (Godot.Collections.Array)excluded;
            AddCasterRids(Player, excluded);
            using var query = PhysicsRayQueryParameters2D.Create(Player.GlobalPosition, frame.AnchorPoint,
                CombatCollisionLayers.World | CombatCollisionLayers.Enemy | CombatCollisionLayers.ThreadTarget);
            query.CollideWithBodies = true; query.CollideWithAreas = true; query.HitFromInside = false; query.Exclude = excluded;
            using var hit = Player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            Node collider = hit.Count == 0 ? null : hit["collider"].AsGodotObject() as Node;
            bool rayClear = hit.Count == 0 || collider != null && MechanicalClawTargetResolver.ColliderBelongsToSurface(collider, target);
            var input = Player.GetNode<PlayerInputReader>("Input").CurrentFrame;
            _before = new Witness(Engine.GetPhysicsFrames(), Player.GlobalTransform, Player.Velocity, frame, snapshot,
                target.GlobalTransform, BeamSize(), !input.PrimaryPressed && !input.ShieldHeld && !input.AttackModePressed &&
                !input.DashPressed && !input.JumpPressed, rayClear, collider?.GetPath().ToString() ?? "none",
                hit.Count == 0 ? "none" : hit["rid"].AsRid().ToString(),
                hit.Count == 0 ? Vector2.Zero : hit["position"].AsVector2(), hit.Count == 0 ? Vector2.Zero : hit["normal"].AsVector2());
            if (!_blockedLogged && (!snapshot.HasLineOfSight || !rayClear))
            {
                _blockedLogged = true;
                GD.Print($"TASK18_TEMPLE_PRE_CLAW_LOS_BLOCKED actualAttachedTarget={LastAttachedTargetPath} actualTargetRuntimeId={target.GetInstanceId()} {DescribeWitness(_before)}");
            }
        }
        catch (Exception exception) { _failure = exception; }
    }

    private void Published(MechanicalClawFrame frame, GrappleSurface2D target)
    {
        if (_failure != null || _releaseLogged || frame.State != MechanicalClawState.Released ||
            frame.Failure != MechanicalClawFailure.Obstructed) return;
        try
        {
            Witness before = _before;
            bool sameTickCast = before != null && before.Tick == Engine.GetPhysicsFrames() && before.Frame.CastId == frame.CastId;
            bool unchanged = sameTickCast && before.Pose == Player.GlobalTransform && before.Velocity == Player.Velocity &&
                before.TargetPose == Intended.GlobalTransform && before.TargetShapeSize == BeamSize();
            bool identity = sameTickCast && before.Snapshot.RuntimeId == before.Frame.LockedTargetRuntimeId &&
                before.Snapshot.RuntimeId == Intended.GetInstanceId() && frame.LockedTargetRuntimeId == before.Snapshot.RuntimeId &&
                frame.AnchorPoint == before.Frame.AnchorPoint && frame.SurfaceNormal == before.Frame.SurfaceNormal;
            bool losFirstOrInference = unchanged && identity && before.Snapshot.Exists && !before.Snapshot.IsLegacyInteraction &&
                before.NoCancelOrBoostInputs && !before.Snapshot.HasLineOfSight && !before.RayClear;
            _releaseLogged = true;
            GD.Print($"TASK18_TEMPLE_SAME_TICK_OBSTRUCTED tick={Engine.GetPhysicsFrames()} cast={frame.CastId} state={frame.State} failure={frame.Failure} sameTickCast={sameTickCast} unchangedPoseVelocityGeometry={unchanged} matchingLockedIdentity={identity} publicationTarget={target?.GetPath()} lastAttachedTarget={LastAttachedTargetPath} firstOrLosInferred={losFirstOrInference} privateRetractFlagObserved=false otherBranches={(losFirstOrInference ? "shortCircuited-by-LOS-source-inference" : "unresolved-contact-stall-sweep")} {(before == null ? "preWitness=none" : DescribeWitness(before))} publicationPose={Player.GlobalPosition} publicationVelocity={Player.Velocity}");
        }
        catch (Exception exception) { _failure = exception; }
    }

    private Vector2 BeamSize() => ((RectangleShape2D)Intended.GetParent().GetNode<CollisionShape2D>("Collision").Shape).Size;
    private static string DescribeWitness(Witness witness) =>
        $"boundary=preClaw-previousTickPostMotor tick={witness.Tick} cast={witness.Frame.CastId} locked={witness.Frame.LockedTargetRuntimeId} pose={witness.Pose} velocity={witness.Velocity} anchor={witness.Frame.AnchorPoint} normal={witness.Frame.SurfaceNormal} snapshotExists={witness.Snapshot.Exists} snapshotRuntimeId={witness.Snapshot.RuntimeId} legacy={witness.Snapshot.IsLegacyInteraction} snapshotLOS={witness.Snapshot.HasLineOfSight} exactRayClear={witness.RayClear} noCancelBoostInputs={witness.NoCancelOrBoostInputs} firstCollider={witness.FirstPath} firstRID={witness.FirstRid} hitPoint={witness.HitPoint} hitNormal={witness.HitNormal} mask=World|Enemy|ThreadTarget bodies=true areas=true hitFromInside=false";
    private static void AddCasterRids(Node node, Godot.Collections.Array<Rid> rids)
    {
        if (node is CollisionObject2D collision) rids.Add(collision.GetRid());
        var children = node.GetChildren();
        using var childrenOwner = (Godot.Collections.Array)children;
        foreach (Node child in children) AddCasterRids(child, rids);
    }
}
