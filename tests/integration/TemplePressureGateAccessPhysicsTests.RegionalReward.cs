using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Ui;

namespace Knight.Tests.Integration;

// Real bound regional lifecycle only. Pre-mount regional positioning, two upstream
// prerequisites, public synthetic injury and one occupied checkpoint reset are disclosed.
// No normal-entry, combat usefulness, cross-process, unspent reload or full-route claim.
public static partial class TemplePressureGateAccessPhysicsTests
{
    private const string RegionalRewardId = "canyon.v2.reward.temple-supply";
    private const string RegionalCheckpointId = "canyon.v2.temple-forecourt";
    private const string RegionalChainId = "canyon.v2.temple-bell-gate";

    public static async Task RegionalReward(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(!tree.Paused, "regional fixture requires an initially unpaused tree");
        PrologueLevelController level = null;
        RegionalObservation observed = null;
        bool inputDrive = false;
        IsolatedPrologueFixturePaths retained = null;
        ulong producerLevel = 0, producerPlayer = 0, producerWorld = 0, producerSector = 0;
        try
        {
            level = NewRegionalLevel(out var flow, out retained);
            tree.Root.AddChild(level);
            await RegionalWait(tree, () => flow.State == PrologueGameFlowState.Title &&
                File.Exists(retained.SettingsPath), 30, "deferred empty Title and isolated settings write");
            TestAssert.True(!flow.HasSave && !File.Exists(retained.SavePath), "producer starts with no retained save");
            flow.StartNew();
            await Frame(tree, 8);
            observed = new RegionalObservation(level, retained, consumer: false);
            await WaitFloor(tree, observed.Player, observed.Scope, 90);
            observed.AssertLive(5);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                !observed.Gate.IsOpen && !observed.Gate.HasOpened && !observed.Gate.DoorCollision.Disabled &&
                DoorRayHits(observed.Player, observed.Gate) && !observed.Chain.IsOpen &&
                !observed.ChainCollision.Disabled && ChainDoorRayHits(observed.Player, observed.ChainDoor) &&
                !observed.Reward.IsSpent && !level.Progress.BellChainShortcutOpen,
                "actual new run starts full, reward bound/unspent, both doors physically closed");
            RegionalAppearance(observed.Reward, false);
            var capsule = (CapsuleShape2D)observed.Player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            TestAssert.True(capsule.Height == 72 && capsule.Radius == 20 && observed.Health.MaxHealth == 5,
                "packed persistent Player retains literal full 72x40 capsule and HP5");
            producerLevel = level.GetInstanceId(); producerPlayer = observed.Player.GetInstanceId();
            producerWorld = observed.World.GetInstanceId(); producerSector = level.CurrentSectorRoot.GetInstanceId();
            level.Progress.ActivateMechanism("canyon.v2.courtyard-crank");
            level.Progress.ActivateMechanism("canyon.v2.encounter.split-level");
            var seal = observed.Scope.GetNode<CanyonSealGate>("Mechanisms/CourtyardSeal");
            seal.ApplyProgress(level.Progress);
            TestAssert.True(seal.IsOpen && level.Progress.ActivatedMechanisms.SetEquals(new[] {
                "canyon.v2.courtyard-crank", "canyon.v2.encounter.split-level", "canyon.v2.courtyard-seal-open" }),
                "only two synthetic upstream IDs plus normally derived seal are prepared");
            GD.Print("TASK18_TEMPLE_REGIONAL_SETUP upstreamFixture=true skippedAcquisition=true normalEntry=false syntheticDamage=true completeLevel=true actorsFrozen=false crossProcess=false sameProcessFreshInstance=true");
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, observed.Player, null); inputDrive = true;
            await RegionalOpenChain(tree, observed);
            GD.Print($"TASK18_TEMPLE_REGIONAL_CHAIN_OK cast={observed.ChainCast} latches=1 applications=1 openedEvents=1 automaticDisk=true {Describe(observed.Player, observed.Gate)}");

            await RegionalCheckpointWalk(tree, observed, 5);
            GD.Print($"TASK18_TEMPLE_REGIONAL_CHECKPOINT_OK triggerEvents={observed.CheckpointEvents} worldEvents={observed.WorldCheckpointEvents} id={RegionalCheckpointId} position=(5920,1080) actualTraversal=true automaticDisk=true hp=5");
            await RegionalClimb(tree, observed, 5, "healthy");
            await RegionalChamberWalk(tree, observed, 6010, () => observed.AssertLive(5));
            TestAssert.True(observed.Reward.OverlapsBody(observed.Player) && observed.HealedEvents == 0 &&
                !level.Progress.Discoveries.Contains(RegionalRewardId) && !observed.Reward.IsSpent,
                "genuine full-HP reward overlap neither heals nor consumes discovery");
            RegionalAppearance(observed.Reward, false); RegionalDisk(retained, spent: false, checkpoint: true);
            await RegionalReturnLower(tree, observed, 5);
            await RegionalWaitClosed(tree, observed, 5);
            RegionalAppearance(observed.Reward, false);
            TestAssert.True(!observed.Reward.IsSpent && observed.HealedEvents == 0,
                "healthy access returns and closes with supply still available");
            RegionalDisk(retained, spent: false, checkpoint: true);
            GD.Print("TASK18_TEMPLE_REGIONAL_FULL_HP_OK healthyAccess=true healing=false hp=5 discovery=false healedEvents=0 gateClosedAfterExit=true");

            RegionalDamage(observed, 3, 180301, 2);
            await RegionalCheckpointWalk(tree, observed, 2);
            TestAssert.True(!observed.Reward.IsSpent && observed.HealedEvents == 0,
                "injured checkpoint crossing cannot heal or consume supply");
            await RegionalClimb(tree, observed, 2, "injured");
            string availableStatus = observed.Reward.GetNode<Label>("Status").Text;
            await RegionalChamberWalk(tree, observed, 6010, () => {
                observed.AssertLive(Health(observed.Player));
                TestAssert.True(Health(observed.Player) is 2 or 4, "claim crossing only permits literal 2-to-4 transition");
            });
            observed.ThrowIfFailed();
            TestAssert.True(observed.Reward.OverlapsBody(observed.Player) && observed.HealedEvents == 1 &&
                observed.ClaimPublicationOrdered && Health(observed.Player) == 4 && observed.Reward.IsSpent,
                "one actual BodyEntered claim heals exactly2 once with consumption published first");
            RegionalAppearance(observed.Reward, true);
            TestAssert.True(observed.Reward.GetNode<Label>("Status").Text != availableStatus &&
                level.GetNode<PrologueFormalHud>("Hud/FormalHud").CurrentModel.HealthSegments.Count(s => s == HealthShardState.Filled) == 4,
                "spent status changes and actual formal HUD displays four filled health segments");
            RegionalDisk(retained, spent: true, checkpoint: true);
            GD.Print("TASK18_TEMPLE_REGIONAL_HEAL_OK syntheticDamage=true combatUsefulness=false before=2 restored=2 remaining=4 healedEvents=1 consumedBeforeHealed=true hudFilled=4 automaticDisk=true");

            await RegionalChamberWalk(tree, observed, 5896, () => observed.AssertLive(4));
            TestAssert.True(!observed.Reward.OverlapsBody(observed.Player), "first claim is physically exited before repeat damage");
            await Frame(tree, 24); observed.AssertLive(4);
            RegionalDamage(observed, 1, 180302, 3);
            await RegionalChamberWalk(tree, observed, 6010, () => observed.AssertLive(3));
            TestAssert.True(observed.Reward.OverlapsBody(observed.Player) && observed.HealedEvents == 1 && observed.Reward.IsSpent,
                "real second injured entry cannot repeat the finite reward");
            RegionalDisk(retained, spent: true, checkpoint: true);
            GD.Print("TASK18_TEMPLE_REGIONAL_REPEAT_OK hp=3 healedEvents=1 spent=true realExitReentry=true automaticDisk=true");

            Vector2 occupiedPose = observed.Player.GlobalPosition;
            observed.World.ResetCurrentSectorForCheckpoint(); // Disclosed occupied reset fixture, not death equivalence.
            TestAssert.True(!observed.Gate.HasOpened, "public occupied reset immediately clears local opened latch");
            for (int i = 0; i < 150; i++)
            {
                await Frame(tree); observed.AssertLive(3); observed.AssertChainDurable();
                TestAssert.True(Contact(observed.Player, observed.Shelf, true) && observed.Reward.OverlapsBody(observed.Player) &&
                    observed.Player.GlobalPosition.DistanceTo(occupiedPose) < 1 && observed.Gate.IsOpen &&
                    observed.Gate.Frame.Traversable && observed.Gate.DoorCollision.Disabled && !DoorRayHits(observed.Player, observed.Gate) &&
                    observed.Reward.IsSpent && observed.HealedEvents == 1,
                    "150 actual occupied frames retain supported pose, committed traversable door, spent supply and HP3");
            }
            await RegionalReturnLower(tree, observed, 3);
            await RegionalWaitClosed(tree, observed, 3);
            RegionalDisk(retained, spent: true, checkpoint: true);
            GD.Print("TASK18_TEMPLE_REGIONAL_RESET_OK resetFixture=true occupiedFrames=150 noCrush=true noDisplacement=true hp=3 spent=true gateClosedAfterPhysicalExit=true bellChainDurable=true");

            await Frame(tree, 24); observed.AssertLive(3);
            observed.DeathExpected = true;
            TestAssert.True(observed.Health.ApplyDamage(new DamageRequest(99, Vector2.Zero, 0, 180303, producerLevel)),
                "public unique lethal synthetic damage is accepted");
            await RegionalWait(tree, () => {
                observed.ThrowIfFailed();
                return observed.RecoveryCompletedEvents == 1 && observed.Flow.State == PrologueGameFlowState.Playing &&
                    Health(observed.Player) == 5 && OriginalFloor(observed.Scope, observed.Player);
            }, 120, "actual Died/Recovery/Playing with real lower support");
            await Frame(tree, 8); observed.AssertLive(5); observed.AssertChainDurable(); observed.AssertCheckpoint();
            TestAssert.True(observed.DiedEvents == 1 && observed.RecoveryRequestedEvents == 1 &&
                observed.RecoveryCompletedEvents == 1 && observed.SawRecovering &&
                observed.Player.GetInstanceId() == producerPlayer && observed.Player.GlobalPosition.DistanceTo(new Vector2(5920, 1080)) <= 12 &&
                !observed.Gate.HasOpened && !observed.Gate.IsOpen && !observed.Gate.DoorCollision.Disabled && DoorRayHits(observed.Player, observed.Gate) &&
                observed.Reward.IsSpent && observed.HealedEvents == 1,
                "one real death restores samePlayer HP5 at stable Temple checkpoint, resets local closed gate, never reclaims reward");
            RegionalDisk(retained, spent: true, checkpoint: true);
            GD.Print("TASK18_TEMPLE_REGIONAL_DEATH_OK syntheticDamage=true died=1 requested=1 completed=1 recoveringObserved=true samePlayer=true checkpoint=(5920,1080) hp=5 gateLocalClosed=true spent=true syntheticHealEvents=0");
            observed.DisposeAndThrow(); observed = null;
            PrologueCanyonTraversalTests.EndTask10InputDrive(); inputDrive = false;
            ReleaseInputs(level.Player);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level); level = null;

            level = NewRegionalLevel(out flow, out _, retained);
            tree.Root.AddChild(level);
            await RegionalWait(tree, () => flow.State == PrologueGameFlowState.Title && flow.HasSave &&
                flow.LoadResult?.Status == PrologueSaveLoadStatus.Current, 30, "fresh actual Title has valid producer disk Continue");
            observed = new RegionalObservation(level, retained, consumer: true);
            TestAssert.True(level.GetInstanceId() != producerLevel && observed.Player.GetInstanceId() != producerPlayer &&
                observed.World.GetInstanceId() != producerWorld && level.CurrentSectorRoot.GetInstanceId() != producerSector &&
                flow.Save.Discoveries.Contains(RegionalRewardId), "fresh level/player/world/sector load spent discovery from retained valid disk");
            flow.Continue(); await Frame(tree, 8);
            await WaitFloor(tree, observed.Player, observed.Scope, 90);
            observed.AssertLive(5); observed.AssertCheckpoint(); observed.AssertChainDurable();
            TestAssert.True(observed.Player.GlobalPosition.DistanceTo(new Vector2(5920, 1080)) <= 12 &&
                !observed.Gate.HasOpened && !observed.Gate.IsOpen && !observed.Gate.DoorCollision.Disabled && DoorRayHits(observed.Player, observed.Gate) &&
                observed.Reward.IsSpent && observed.HealedEvents == 0 && observed.ChainOpenedEvents == 0 &&
                observed.ChainLatches == 0 && observed.Adapter.ApplicationCount == 0,
                "fresh Continue restores real checkpoint and spent reward, local gate closed, chain restored without interaction");
            RegionalAppearance(observed.Reward, true);
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, observed.Player, null); inputDrive = true;
            RegionalDamage(observed, 1, 180304, 4);
            await Walk(tree, observed.Player, 5825, 90, () => observed.AssertLower(4));
            await RegionalClimb(tree, observed, 4, "resumed");
            await RegionalChamberWalk(tree, observed, 6010, () => observed.AssertLive(4));
            TestAssert.True(observed.Reward.OverlapsBody(observed.Player) && observed.HealedEvents == 0 &&
                observed.Reward.IsSpent && Health(observed.Player) == 4 && observed.Adapter.ApplicationCount == 0,
                "fresh-instance real ordinary climb/reentry cannot award disk-consumed reward again");
            RegionalDisk(retained, spent: true, checkpoint: true);
            observed.DisposeAndThrow(); observed = null;
            PrologueCanyonTraversalTests.EndTask10InputDrive(); inputDrive = false; ReleaseInputs(level.Player);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level); level = null;
            GD.Print("TASK18_TEMPLE_REGIONAL_CONTINUE_OK sameProcessFreshInstance=true crossProcess=false newIdentities=true diskLoaded=true realClimbReentry=true hp=4 healedEvents=0 spent=true bellChainApplications=0 gateInitiallyClosed=true");
        }
        finally
        {
            try { observed?.DisposeAndThrow(); }
            finally
            {
                try
                {
                    if (inputDrive) PrologueCanyonTraversalTests.EndTask10InputDrive();
                    if (GodotObject.IsInstanceValid(level?.Player)) ReleaseInputs(level.Player);
                }
                finally
                {
                    // No pauses are introduced; restore only a pause belonging to this mounted level.
                    if (GodotObject.IsInstanceValid(level))
                    {
                        if (tree.Paused && level.IsInsideTree() && level.GetNode<ProloguePauseMenu>("PauseMenu").IsOpen)
                            level.GetNode<ProloguePauseMenu>("PauseMenu").Execute(ProloguePauseCommand.Resume);
                        if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                        else level.Free();
                    }
                }
            }
        }
        GD.Print("TASK18_TEMPLE_REGIONAL_REWARD_OK fixture=true fullRoute=false sameProcessFreshInstance=true crossProcess=false teardownAwaited=true artifactsRetained=true");
    }

    private static PrologueLevelController NewRegionalLevel(out PrologueGameFlowController flow,
        out IsolatedPrologueFixturePaths paths, IsolatedPrologueFixturePaths retained = null)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            level.GetNode<CharacterBody2D>("PlayerRoot/Player").Position = new Vector2(5770, 1080);
            level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = PrologueSectorId.LostTemple;
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            paths = IsolatedPrologueFixture.Configure(level, "temple-regional-reward");
            if (retained != null)
            {
                // Producer-owned record only, not a CLI-supplied save file or nonempty-storage launch exemption.
                string storage = Path.GetDirectoryName(Path.GetDirectoryName(paths.CaseRoot)!)!;
                RegionalRetainedPaths(retained, storage);
                flow.SavePath = retained.SavePath; flow.SettingsPath = retained.SettingsPath;
                level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory = retained.PlaytestsDirectory;
            }
            var bound = retained ?? paths;
            TestAssert.True(flow.SavePath == bound.SavePath && flow.SettingsPath == bound.SettingsPath &&
                level.GetNode<ProloguePlaytestRecorder>("PlaytestRecorder").OutputDirectory == bound.PlaytestsDirectory,
                "all persistence owners bind external case paths off-tree before Ready");
            GD.Print($"TASK18_TEMPLE_REGIONAL_PATHS consumer={retained != null} save={bound.SavePath} settings={bound.SettingsPath} playtests={bound.PlaytestsDirectory} allocatedCase={paths.CaseRoot} poseWritesBeforeMountOnly=true recorderManualOverride=false");
            return level;
        }
        catch { level.Free(); throw; }
    }

    private static void RegionalRetainedPaths(IsolatedPrologueFixturePaths paths, string storage)
    {
        string expectedRoot = Path.Combine(storage, "full-regression") + Path.DirectorySeparatorChar;
        TestAssert.True(Path.GetFullPath(paths.CaseRoot).StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileName(paths.CaseRoot).StartsWith("temple-regional-reward-", StringComparison.Ordinal) &&
            paths.SavePath == Path.Combine(paths.CaseRoot, "save.json") && paths.SettingsPath == Path.Combine(paths.CaseRoot, "settings.json") &&
            paths.PlaytestsDirectory == Path.Combine(paths.CaseRoot, "playtests") && File.Exists(paths.SavePath) && File.Exists(paths.SettingsPath),
            "Continue paths are exactly retained producer files inside the already validated case storage");
        foreach (string path in new[] { paths.CaseRoot, paths.SavePath, paths.SettingsPath, paths.PlaytestsDirectory })
        {
            if (File.Exists(path)) TestAssert.True((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "retained file is not a reparse");
            for (DirectoryInfo current = new(File.Exists(path) ? Path.GetDirectoryName(path)! : path); current != null; current = current.Parent)
                if (current.Exists) TestAssert.True((current.Attributes & FileAttributes.ReparsePoint) == 0, "retained path has no reparse ancestors");
        }
        RegionalDisk(paths, true, true);
    }

    private static async Task RegionalOpenChain(SceneTree tree, RegionalObservation o)
    {
        o.TrackingChain = true;
        TestAssert.True(o.Target.WorldAnchor == new Vector2(5680, 890), "bound chain uses literal actual WorldAnchor");
        await SelectGrappleAndCast(tree, o.Player, o.Target.WorldAnchor);
        await RegionalWait(tree, () => { o.AssertLive(5); return o.Chain.IsOpen && o.ChainCollision.Disabled; }, 120, "genuine chain/deferred collision opening");
        o.AssertChainDurable();
        TestAssert.True(o.ChainLatches == 1 && o.ChainOpenedEvents == 1 && o.Adapter.ApplicationCount == 1 &&
            o.ChainCasts.Count == 1 && o.ChainCast != 0, "one independent real latch/application/open/cast is required");
        await RegionalWait(tree, () => { o.AssertLive(5); return o.Claw.CurrentFrame.State == MechanicalClawState.Idle && OriginalFloor(o.Scope, o.Player); },
            120, "normal legacy Cancel/retract to Idle");
        TestAssert.True(o.ChainCancelEvents == 1, "one ordinary legacy Cancel is observed, never Boost");
        o.TrackingChain = false; RegionalDisk(o.Paths, spent: false, checkpoint: false);
    }

    private static async Task RegionalCheckpointWalk(SceneTree tree, RegionalObservation o, int hp)
    {
        int trigger = o.CheckpointEvents, world = o.WorldCheckpointEvents;
        await Walk(tree, o.Player, 6000, 90, () => o.AssertLower(hp));
        await Walk(tree, o.Player, 5825, 90, () => o.AssertLower(hp));
        TestAssert.True(o.CheckpointEvents > trigger && o.WorldCheckpointEvents > world,
            "real A/D traversal emits both authored CheckpointTrigger and SeamlessWorld activation");
        o.AssertCheckpoint(); o.AssertLive(hp); RegionalDisk(o.Paths, o.Reward.IsSpent, true);
    }

    private static async Task RegionalClimb(SceneTree tree, RegionalObservation o, int hp, string visit)
    {
        var player = o.Player; var claw = o.Claw;
        var intended = o.Scope.GetNode<GrappleSurface2D>("Collision/TempleSupplyGripBeam/GrappleSurfaceProfile");
        int jumps = player.GetNode<PlayerMotor>("Motor").AcceptedJumpCount, dashes = player.GetNode<PlayerMotor>("Motor").AcceptedDashCount;
        ulong cast = 0; int releases = 0, aims = 0; bool attached = false, arrived = false, boost = false;
        MechanicalClawFrame? release = null; Exception failure = null;
        void Invariant()
        {
            if (failure != null) throw new InvalidOperationException($"Regional climb observer failed visit={visit}", failure);
            o.AssertLive(hp); o.AssertChainDurable(); AssertWestOrdinary(player, hp, jumps, dashes, boost);
        }
        void OnFrame(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (failure != null) return;
            try
            {
                boost |= frame.BoostConsumed || frame.ReleaseKind == MechanicalClawReleaseKind.Boost;
                AssertWestOrdinary(player, hp, jumps, dashes, boost);
                if (frame.State == MechanicalClawState.Aim) aims++;
                if (frame.CastId != 0)
                {
                    if (cast == 0) cast = frame.CastId;
                    TestAssert.True(frame.CastId == cast, "each regional visit has one ordinary beam cast identity");
                }
                if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
                {
                    attached = true;
                    TestAssert.True(target == intended && frame.LockedTargetRuntimeId == intended.GetInstanceId() &&
                        frame.AnchorPoint.X >= 5805 && frame.AnchorPoint.X <= 5855 && MathF.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f,
                        "every real attached publication locks literal west beam underside bounds/normal");
                }
                if (frame.State == MechanicalClawState.Released) { releases++; release = frame; }
            }
            catch (Exception exception) { failure = exception; }
        }
        TestAssert.True(OriginalFloor(o.Scope, player) && claw.CurrentFrame.State == MechanicalClawState.Idle,
            "each visit starts naturally supported below with claw Idle");
        claw.ClawFrameChanged += OnFrame;
        Vector2 aim = new(5828, 840);
        try
        {
            await SelectGrappleAndCast(tree, player, aim); Invariant();
            for (int i = 0; i < 300; i++)
            {
                await Frame(tree); Invariant(); var frame = claw.CurrentFrame;
                if (i % 30 == 0) GD.Print($"TASK18_TEMPLE_REGIONAL_CLIMB_SAMPLE visit={visit} stage=pull frame={i} {Describe(player, o.Gate)} {WestClaw(frame)}");
                if (frame.State is MechanicalClawState.Failed or MechanicalClawState.Released or MechanicalClawState.Retract or MechanicalClawState.Idle)
                    throw new InvalidOperationException($"Regional single beam cast terminated before Arrived visit={visit} {WestClaw(frame)}");
                if (frame.Arrived) { arrived = true; break; }
            }
            TestAssert.True(aims == 1 && cast != 0 && cast != o.ChainCast && o.ClimbCasts.Add(cast) && attached && arrived && claw.CurrentFrame.CastId == cast,
                "one actual fixed beam cast reaches genuine Arrived within300 frames");
            ActionKey(player, Key.D, true);
            bool ready = false;
            for (int i = 0; i < 90; i++)
            {
                await Frame(tree); Invariant(); var frame = claw.CurrentFrame;
                TestAssert.True(frame.CastId == cast && claw.CurrentTarget == intended && frame.LockedTargetRuntimeId == intended.GetInstanceId() &&
                    frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                    "ordinary D retains the attached actual beam until coarse release pose");
                Rect2 bounds = Bounds(player);
                ready = player.GlobalPosition.X >= frame.AnchorPoint.X + bounds.Size.X / 2 && bounds.End.Y <= 940 && player.Velocity.X > 0;
                if (i % 15 == 0 || ready) GD.Print($"TASK18_TEMPLE_REGIONAL_CLIMB_SAMPLE visit={visit} stage=swing frame={i} ready={ready} rule=east-one-radius-foot-at-most-940-rightward {Describe(player, o.Gate)} {WestClaw(frame)}");
                if (ready) break;
            }
            TestAssert.True(ready, "coarse unchanged body-sized D pose must arrive within90 frames");
            Mouse(player, true, aim); await Frame(tree); Invariant(); Mouse(player, false, aim);
            TestAssert.True(releases == 1 && release.HasValue && release.Value.CastId == cast &&
                release.Value.LockedTargetRuntimeId == intended.GetInstanceId() && release.Value.ReleaseKind == MechanicalClawReleaseKind.Cancel &&
                release.Value.Failure == MechanicalClawFailure.None && !release.Value.BoostConsumed,
                "exactly one observed same-cast ordinary LMB Cancel with no Boost is required");
            await Walk(tree, player, 5896, 90, Invariant); // Continued D, never Space/jump/dash/reel/second cast.
            await RegionalWait(tree, () => { Invariant(); return Contact(player, o.Shelf, true); }, 90, "actual upper shelf landing");
            for (int i = 0; i < 12; i++)
            {
                await Frame(tree); Invariant(); Rect2 bounds = Bounds(player);
                TestAssert.True(Contact(player, o.Shelf, true) && bounds.Position.X >= 5865 && bounds.End.X <= 5930 && MathF.Abs(bounds.End.Y - 960) < 1 &&
                    o.Gate.PlateWorldRect.HasPoint(new Vector2(player.GlobalPosition.X, bounds.End.Y - .1f)),
                    "full capsule has12 stable grounded frames on literal65px external shelf/top960");
            }
            TestAssert.True(releases == 1 && o.Gate.HasOpened && o.Gate.IsOpen && o.Gate.Frame.Traversable &&
                o.Gate.DoorCollision.Disabled && !DoorRayHits(player, o.Gate), "stable landing commits real pressure opening/query clearance");
            GD.Print($"TASK18_TEMPLE_REGIONAL_CLIMB_COMPLETE visit={visit} cast={cast} aim={aim} arrived=true releases=1 release=Cancel jumpsDelta=0 dashesDelta=0 Boost=false stableFrames=12 {Describe(player, o.Gate)}");
        }
        finally
        {
            claw.ClawFrameChanged -= OnFrame;
            ActionKey(player, Key.D, false); Mouse(player, false, aim);
            if (failure != null) throw new InvalidOperationException($"Regional climb observer failed before cleanup visit={visit}", failure);
        }
    }

    private static async Task RegionalChamberWalk(SceneTree tree, RegionalObservation o, float target, Action healthSample) =>
        await Walk(tree, o.Player, target, 90, () => {
            healthSample(); o.AssertChainDurable();
            TestAssert.True(Contact(o.Player, o.Shelf, true) && o.Gate.IsOpen && o.Gate.Frame.Traversable &&
                o.Gate.DoorCollision.Disabled && !DoorRayHits(o.Player, o.Gate), "real chamber A/D crossing retains actual shelf/open physical door");
            AssertNoEmbedding(o.Player);
        });

    private static async Task RegionalReturnLower(SceneTree tree, RegionalObservation o, int hp)
    {
        await RegionalChamberWalk(tree, o, 5896, () => o.AssertLive(hp));
        TestAssert.True(!o.Reward.OverlapsBody(o.Player), "actual Area exit precedes west drop");
        await Walk(tree, o.Player, 5825, 90, () => { o.AssertLive(hp); o.AssertChainDurable(); });
        await WaitFloor(tree, o.Player, o.Scope, 90); o.AssertLower(hp);
    }

    private static async Task RegionalWaitClosed(SceneTree tree, RegionalObservation o, int hp) =>
        await RegionalWait(tree, () => {
            o.AssertLower(hp); o.AssertChainDurable();
            return !o.Gate.IsOpen && !o.Gate.DoorCollision.Disabled && DoorRayHits(o.Player, o.Gate) &&
                o.Claw.CurrentFrame.State == MechanicalClawState.Idle;
        }, 180, "normal unoccupied pressure closure and claw Idle");

    private static async Task RegionalWait(SceneTree tree, Func<bool> condition, int budget, string label)
    {
        for (int i = 0; i < budget; i++) { await Frame(tree); if (condition()) return; }
        throw new TimeoutException($"Regional bounded condition missing: {label}, budget={budget}");
    }

    private static void RegionalDamage(RegionalObservation o, int amount, ulong attack, int expected)
    {
        o.ThrowIfFailed();
        TestAssert.True(o.Health.ApplyDamage(new DamageRequest(amount, Vector2.Zero, 0, attack, o.Level.GetInstanceId())) &&
            Health(o.Player) == expected, $"disclosed public synthetic zero-knockback damage{amount} arranges literal HP{expected}");
        o.AssertLive(expected);
    }

    private static void RegionalAppearance(PrologueSupplyCache reward, bool spent)
    {
        TestAssert.True(reward.IsSpent == spent && reward.GetNode<Node2D>("AvailableVisual").IsVisibleInTree() == !spent &&
            reward.GetNode<Node2D>("SpentVisual").IsVisibleInTree() == spent &&
            reward.GetNode<Label>("Status").IsVisibleInTree() && !string.IsNullOrWhiteSpace(reward.GetNode<Label>("Status").Text),
            "actual bound supply artwork and readable status match finite consumption state");
    }

    private static void RegionalDisk(IsolatedPrologueFixturePaths paths, bool spent, bool checkpoint)
    {
        TestAssert.True(File.Exists(paths.SavePath), "automatic bound progress persistence produces an actual case-owned save");
        var save = PrologueSaveStore.LoadOrNew(paths.SavePath, 2);
        TestAssert.True(!save.WasRecovered && save.Version == 3 && save.LayoutVersion == 2 &&
            save.OpenShortcuts.Contains(RegionalChainId) && save.Discoveries.Contains(RegionalRewardId) == spent && !save.BossDefeated && !save.Completed,
            "valid automatically written disk snapshot independently retains shortcut and exact reward state");
        using JsonDocument wire = JsonDocument.Parse(File.ReadAllText(paths.SavePath));
        if (checkpoint)
            TestAssert.True(save.HasActivatedCheckpoint && save.CheckpointId == RegionalCheckpointId && save.CheckpointSector == "LostTemple" &&
                save.CheckpointWorldPosition == new Vector2(5920, 1080) && wire.RootElement.GetProperty("checkpointId").GetString() == RegionalCheckpointId &&
                wire.RootElement.GetProperty("checkpointWorldX").GetSingle() == 5920 && wire.RootElement.GetProperty("checkpointWorldY").GetSingle() == 1080,
                "raw automatic disk, not just normalization, names literal real stable Temple checkpoint/coordinates");
    }

    private sealed class RegionalObservation
    {
        internal readonly PrologueLevelController Level;
        internal readonly PrologueGameFlowController Flow;
        internal readonly SeamlessPrologueWorld World;
        internal readonly Node2D Scope;
        internal readonly CharacterBody2D Player;
        internal readonly HealthComponent Health;
        internal readonly MechanicalClawController Claw;
        internal readonly CanyonPressurePlateGate Gate;
        internal readonly PrologueSupplyCache Reward;
        internal readonly StaticBody2D Shelf, ChainDoor;
        internal readonly BellChainMechanism Chain;
        internal readonly CollisionShape2D ChainCollision;
        internal readonly ThreadTarget2D Target;
        internal readonly LegacyThreadInteractionAdapter Adapter;
        internal readonly IsolatedPrologueFixturePaths Paths;
        private readonly CanyonEncounterGroup _encounter;
        private readonly BossArenaController _boss;
        private readonly CheckpointTrigger _checkpoint;
        private readonly ulong _playerId, _levelId, _worldId, _sectorId;
        private readonly bool _consumer;
        private bool _subscribed;
        private Exception _failure;
        internal bool TrackingChain, DeathExpected, SawRecovering, ClaimPublicationOrdered;
        internal int ChainLatches, ChainOpenedEvents, ChainCancelEvents, HealedEvents, CheckpointEvents, WorldCheckpointEvents;
        internal int DiedEvents, RecoveryRequestedEvents, RecoveryCompletedEvents;
        internal ulong ChainCast;
        internal readonly HashSet<ulong> ChainCasts = new();
        internal readonly HashSet<ulong> ClimbCasts = new();

        internal RegionalObservation(PrologueLevelController level, IsolatedPrologueFixturePaths paths, bool consumer)
        {
            Level = level; Paths = paths; _consumer = consumer;
            Flow = level.GetNode<PrologueGameFlowController>("GameFlow"); World = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            Scope = level.CurrentSectorRoot.GetNode<Node2D>("Gameplay"); Player = level.Player;
            Health = Player.GetNode<HealthComponent>("Health"); Claw = Player.GetNode<MechanicalClawController>("MechanicalClaw");
            Gate = Scope.GetNode<CanyonPressurePlateGate>("Mechanisms/TemplePressureGate"); Reward = Scope.GetNode<PrologueSupplyCache>("Rewards/TempleSupply");
            Shelf = Scope.GetNode<StaticBody2D>("Collision/TempleSupplyShelf"); Chain = Scope.GetNode<BellChainMechanism>("Mechanisms/BellChain");
            ChainDoor = Chain.GetNode<StaticBody2D>("Door"); ChainCollision = Chain.GetNode<CollisionShape2D>(Chain.DoorCollisionPath);
            Target = Chain.GetNode<ThreadTarget2D>("ThreadTarget"); Adapter = Target.GetNode<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter");
            _checkpoint = Scope.GetNode<CheckpointTrigger>("TempleCheckpoint");
            _encounter = Scope.GetNode<CanyonEncounterGroup>("Encounters/SealedCourtyardEncounter"); _boss = Scope.GetNode<BossArenaController>("BossArenaController");
            _playerId = Player.GetInstanceId(); _levelId = Level.GetInstanceId(); _worldId = World.GetInstanceId(); _sectorId = level.CurrentSectorRoot.GetInstanceId();
            TestAssert.True(Reward.DiscoveryId == RegionalRewardId && Reward.HealthAmount == 2 && Gate.PlayerBody == Player &&
                _checkpoint.EntranceId.ToString() == RegionalCheckpointId && _checkpoint.GlobalPosition == new Vector2(5920, 1080),
                "real packed regional reward/Player gate binding and authored stable checkpoint are present");
            Chain.Opened += OnOpened; Claw.ClawFrameChanged += OnClaw; Health.Healed += OnHealed; Health.Died += OnDied;
            _checkpoint.CheckpointActivated += OnCheckpoint; World.CheckpointActivated += OnWorldCheckpoint;
            Level.RecoveryRequested += OnRequested; Level.RecoveryCompleted += OnCompleted; _subscribed = true;
        }

        private void Observe(Action assertion) { if (_failure != null) return; try { assertion(); } catch (Exception e) { _failure = e; } }
        private void OnOpened(StringName id) => Observe(() => {
            ChainOpenedEvents++;
            TestAssert.True(!_consumer && TrackingChain && id.ToString() == RegionalChainId && ChainOpenedEvents == 1,
                "one requested genuine producer BellChain Opened event only");
        });
        private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target) => Observe(() => {
            TestAssert.True(!frame.BoostConsumed && frame.ReleaseKind != MechanicalClawReleaseKind.Boost, "regional route never publishes Boost");
            if (frame.State == MechanicalClawState.Latched && target == Target)
            {
                ChainLatches++;
                TestAssert.True(!_consumer && TrackingChain && ChainLatches == 1 && frame.LockedTargetRuntimeId == Target.GetInstanceId(),
                    "actual chain latch is observed exactly once only during the producer interaction, never on Continue");
            }
            if (!TrackingChain) return;
            if (frame.CastId != 0) { ChainCasts.Add(frame.CastId); if (ChainCast == 0) ChainCast = frame.CastId; }
            if (frame.State == MechanicalClawState.Latched)
            {
                TestAssert.True(target == Target && frame.LockedTargetRuntimeId == Target.GetInstanceId(), "requested chain cast never latches a substituted target");
            }
            if (frame.State == MechanicalClawState.Released)
            {
                ChainCancelEvents++;
                TestAssert.True(frame.ReleaseKind == MechanicalClawReleaseKind.Cancel && frame.Failure == MechanicalClawFailure.None,
                    "actual legacy target release is ordinary Cancel with no failure");
            }
        });
        private void OnHealed(int restored, int remaining) => Observe(() => {
            HealedEvents++;
            ClaimPublicationOrdered = Reward.IsSpent && Level.Progress.Discoveries.Contains(RegionalRewardId);
            TestAssert.True(!_consumer && HealedEvents == 1 && restored == 2 && remaining == 4 && Health.CurrentHealth == 4 &&
                ClaimPublicationOrdered && Flow.State == PrologueGameFlowState.Playing,
                "synchronous claim event sees already-spent discovery, literal +2/remaining4 exactly once");
        });
        private void OnCheckpoint(StringName id) => Observe(() => {
            CheckpointEvents++; TestAssert.True(id.ToString() == RegionalCheckpointId, "real trigger publishes stable Temple identity");
        });
        private void OnWorldCheckpoint(PrologueSectorId sector, Vector2 position) => Observe(() => {
            WorldCheckpointEvents++;
            TestAssert.True(sector == PrologueSectorId.LostTemple && position == new Vector2(5920, 1080) && World.CheckpointId == RegionalCheckpointId,
                "real world activation publishes stable Temple coordinates/identity");
        });
        private void OnDied() => Observe(() => { DiedEvents++; TestAssert.True(DeathExpected && DiedEvents == 1, "only disclosed lethal fixture dies once"); });
        private void OnRequested(string reason) => Observe(() => {
            RecoveryRequestedEvents++; SawRecovering = Flow.State == PrologueGameFlowState.Recovering;
            TestAssert.True(DeathExpected && RecoveryRequestedEvents == 1 && SawRecovering && reason == "DEATH RESET", "actual Flow enters Recovering for one normal death request");
        });
        private void OnCompleted(string reason) => Observe(() => {
            RecoveryCompletedEvents++;
            TestAssert.True(DeathExpected && RecoveryCompletedEvents == 1 && Flow.State == PrologueGameFlowState.Playing && reason == "DEATH RESET",
                "one actual normal recovery completion returns Flow to Playing");
        });
        internal void ThrowIfFailed() { if (_failure != null) throw new InvalidOperationException("Regional asynchronous observation failed", _failure); }
        internal void AssertLive(int hp)
        {
            ThrowIfFailed();
            TestAssert.True(Level.GetInstanceId() == _levelId && Level.Player == Player && Player.GetInstanceId() == _playerId &&
                World.GetInstanceId() == _worldId && Level.CurrentSectorRoot.GetInstanceId() == _sectorId && Level.CurrentSector == PrologueSectorId.LostTemple &&
                Flow.State == PrologueGameFlowState.Playing && !Player.GetTree().Paused && Health.CurrentHealth == hp && !_encounter.IsActive && !_encounter.IsComplete &&
                _encounter.LiveActorCount == 0 && !_encounter.IsArrivalWarningActive && !_boss.IsEncounterActive && GodotObject.IsInstanceValid(_boss.CurrentBoss) &&
                !Level.Progress.BossDefeated && Reward.IsInsideTree(),
                "same bound actual regional identities/health/Playing retain normally inactive live encounter/Boss and complete reward");
            TestAssert.True(RecoveryRequestedEvents == (DeathExpected ? 1 : 0) && RecoveryCompletedEvents == (DeathExpected ? 1 : 0),
                "no unintended regional recovery occurs");
            AssertNoEmbedding(Player);
        }
        internal void AssertLower(int hp)
        {
            AssertLive(hp);
            TestAssert.True(OriginalFloor(Scope, Player) && MathF.Abs(Bounds(Player).End.Y - 1116) < 1,
                "real lower traversal stays on unchanged authored floor at1116");
        }
        internal void AssertChainDurable()
        {
            ThrowIfFailed();
            TestAssert.True(Chain.IsOpen && ChainCollision.Disabled && !ChainDoorRayHits(Player, ChainDoor) && Level.Progress.OpenShortcuts.Contains(RegionalChainId) &&
                Adapter.ApplicationCount == (_consumer ? 0 : 1) && ChainOpenedEvents == (_consumer ? 0 : 1) && ChainLatches == (_consumer ? 0 : 1),
                "normal bound chain shortcut remains durable without new latch/application/reopen");
        }
        internal void AssertCheckpoint()
        {
            var checkpoint = Level.Progress.RestoreCheckpoint();
            TestAssert.True(World.CheckpointId == RegionalCheckpointId && checkpoint.CheckpointId == RegionalCheckpointId &&
                checkpoint.HasActivatedCheckpoint && checkpoint.SectorId == PrologueSectorId.LostTemple && checkpoint.WorldPosition == new Vector2(5920, 1080),
                "world and real bound level progress independently carry stable literal Temple checkpoint");
        }
        internal void DisposeAndThrow()
        {
            if (_subscribed)
            {
                Chain.Opened -= OnOpened; Claw.ClawFrameChanged -= OnClaw; Health.Healed -= OnHealed; Health.Died -= OnDied;
                _checkpoint.CheckpointActivated -= OnCheckpoint; World.CheckpointActivated -= OnWorldCheckpoint;
                Level.RecoveryRequested -= OnRequested; Level.RecoveryCompleted -= OnCompleted; _subscribed = false;
            }
            ThrowIfFailed();
        }
    }
}
