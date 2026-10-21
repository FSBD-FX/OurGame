using System;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static partial class TemplePressureGateAccessPhysicsTests
{
    // Actual mounted controller/Label, not rendered visual or normal-route acceptance.
    public static async Task TempleGuidanceRuntime(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.True(!tree.Paused, "guidance fixture requires an initially unpaused tree");
        PrologueLevelController level = null;
        CharacterBody2D player = null;
        MechanicalClawController claw = null;
        Action<MechanicalClawFrame, GrappleSurface2D> observer = null;
        Exception observerFailure = null, failure = null;
        int recoveryRequests = 0, recoveryCompletions = 0;
        bool inputDrive = false, recoverySubscribed = false;
        void OnRequested(string _) => recoveryRequests++;
        void OnCompleted(string _) => recoveryCompletions++;
        void AddFailure(Exception exception) => failure = failure == null
            ? exception : new AggregateException(failure, exception);
        try
        {
            level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = PrologueSectorId.LostTemple;
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            var paths = IsolatedPrologueFixture.Configure(level, "temple-attached-guidance");
            var save = PrologueSaveData.NewLayoutV2();
            save.CheckpointId = RegionalCheckpointId;
            save.Objective = nameof(PrologueObjective.PullTempleMechanism);
            save.OpenShortcuts = [RegionalChainId];
            save.ActivatedMechanisms = ["canyon.v2.courtyard-crank", "canyon.v2.encounter.split-level",
                "canyon.v2.courtyard-seal-open"];
            // This older attachment/release test expects an eligible Boss-entry baseline.
            // Acquisition is deliberately preplaced; blocked admission is tested separately.
            save.CollectedFragmentIds = ["canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
                "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.gear-gallery",
                "canyon.v2.fragment.ascent-mid", "canyon.v2.fragment.courtyard"];
            save.BossDefeated = false; save.Completed = false;
            save = save.Normalize();
            TestAssert.True(save.CheckpointWorldPosition == new Vector2(5920, 1080) &&
                save.CheckpointSector == nameof(PrologueSectorId.LostTemple) && save.HasActivatedCheckpoint,
                "pre-mount save resolves the stable actual Temple checkpoint");
            PrologueSaveStore.SaveAtomic(paths.SavePath, save);
            level.RecoveryRequested += OnRequested; level.RecoveryCompleted += OnCompleted;
            recoverySubscribed = true;
            tree.Root.AddChild(level);
            await RegionalWait(tree, () => flow.State == PrologueGameFlowState.Title && flow.HasSave &&
                flow.LoadResult?.Status == PrologueSaveLoadStatus.Current, 30, "actual Title has current case-local Continue save");
            TestAssert.True(flow.Save.Objective == nameof(PrologueObjective.PullTempleMechanism) &&
                !flow.Save.BossDefeated && !flow.Save.Completed && !flow.BasinQualitySampleMode,
                "actual Title loads the uncompleted mechanism objective without sample mode");
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue").EmitSignal(Button.SignalName.Pressed);
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            await RegionalWait(tree, () => flow.State == PrologueGameFlowState.Playing &&
                completion.CurrentObjective == PrologueObjective.DefeatBellkeeper, 30,
                "actual Continue and completion advance the restored open mechanism to Boss entry");
            player = level.Player;
            ulong playerId = player.GetInstanceId(), castId = 0;
            var scope = level.CurrentSectorRoot.GetNode<Node2D>("Gameplay");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var boss = scope.GetNode<BossArenaController>("BossArenaController");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            var label = visual.GetNode<Label>("Text");
            claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var intended = scope.GetNode<GrappleSurface2D>("Collision/TempleSupplyGripBeam/GrappleSurfaceProfile");
            int jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
            void Invariant()
            {
                if (observerFailure != null) throw new InvalidOperationException("Temple guidance publication observer failed", observerFailure);
                TestAssert.True(level.Player == player && player.GetInstanceId() == playerId && Health(player) == 5 &&
                    motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                    recoveryRequests == 0 && recoveryCompletions == 0 && !tree.Paused &&
                    flow.State == PrologueGameFlowState.Playing && !boss.IsEncounterActive &&
                    completion.CurrentObjective == PrologueObjective.DefeatBellkeeper &&
                    level.CurrentSector == PrologueSectorId.LostTemple && world.CheckpointId == RegionalCheckpointId &&
                    claw.CurrentFrame.Failure == MechanicalClawFailure.None &&
                    (castId == 0 || claw.CurrentFrame.CastId == 0 || claw.CurrentFrame.CastId == castId),
                    "same real Player retains HP5 checkpoint Boss-entry objective and ordinary action counters without recovery or extra cast");
                AssertNoEmbedding(player);
            }
            void AssertLabel(string stage)
            {
                TestAssert.True(guidance.CurrentInstruction.Visible && visual.RenderedStepText == guidance.CurrentInstruction.StepText &&
                    visual.RenderedTargetCount == 1 && visual.HasVisiblePrompt && label.IsVisibleInTree() &&
                    label.Text.Contains(guidance.CurrentInstruction.StepText, StringComparison.Ordinal) &&
                    !label.ClipText && label.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming &&
                    visual.RenderedTextContained,
                    $"{stage} actual mounted Label shows contained complete live instruction without clipping");
            }
            await WaitFloor(tree, player, scope, 90); await Frame(tree, 2); Invariant();
            TestAssert.True(player.GlobalPosition.DistanceTo(new Vector2(5920, 1080)) <= 12 &&
                guidance.CurrentInstruction.Targets.Count == 1 &&
                guidance.CurrentInstruction.Targets[0] == PrologueGuidanceTargetKey.BossActivation,
                "real Continue settles at stable checkpoint with ordinary inactive Boss activation guidance");
            string baseline = guidance.CurrentInstruction.StepText;
            AssertLabel("baseline");
            GD.Print($"TASK18_TEMPLE_GUIDANCE_BASELINE fixture=true upstreamSavePreplaced=true skippedAcquisition=true normalRoute=false fullRoute=false visibleAcceptance=false actorsFrozen=false playerId={playerId} position={player.GlobalPosition} objective={completion.CurrentObjective} target=BossActivation prompt={visual.PromptRect} textBounds={visual.RenderedTextBounds} text={label.Text}");
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, null); inputDrive = true;
            await Walk(tree, player, 5825, 90, Invariant);

            int sequence = 0, boostSequence = -1, boosts = 0, releases = 0;
            ulong boostTick = 0, releaseTick = 0;
            MechanicalClawFrame? boost = null;
            bool spaceRequested = false;
            observer = (frame, target) =>
            {
                if (observerFailure != null) return;
                try
                {
                    sequence++;
                    if (frame.CastId != 0)
                    {
                        if (castId == 0) castId = frame.CastId;
                        TestAssert.True(frame.CastId == castId, "actual guidance test publishes only one nonzero cast identity");
                    }
                    TestAssert.True(frame.Failure == MechanicalClawFailure.None && frame.State != MechanicalClawState.Failed,
                        "actual guidance cast never publishes a failure");
                    if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing)
                        TestAssert.True(target == intended && claw.CurrentTarget == intended &&
                            frame.LockedTargetRuntimeId == intended.GetInstanceId() && intended.UseCollisionHitPoint &&
                            intended.Material == GrappleSurfaceMaterial.TempleMetal && intended is not ThreadTarget2D &&
                            intended.GetNodeOrNull<LegacyThreadInteractionAdapter>("LegacyThreadInteractionAdapter") == null &&
                            MathF.Abs(frame.AnchorPoint.Y - 860) < 1 && frame.SurfaceNormal.Y > .7f,
                            "real click attaches the intended nonlegacy collision-hit beam underside and matching lock");
                    if (frame.State == MechanicalClawState.Boost)
                    {
                        boosts++; boost = frame; boostTick = Engine.GetPhysicsFrames(); boostSequence = sequence;
                        TestAssert.True(spaceRequested && boosts == 1 && releases == 0 && frame.BoostConsumed &&
                            frame.ReleaseKind == MechanicalClawReleaseKind.Boost, "one requested real Space publishes consumed Boost");
                    }
                    if (frame.State == MechanicalClawState.Released)
                    {
                        releases++; releaseTick = Engine.GetPhysicsFrames();
                        TestAssert.True(spaceRequested && boosts == 1 && releases == 1 && boostSequence + 1 == sequence &&
                            boostTick == releaseTick && boost.Value.CastId == frame.CastId && target == null &&
                            claw.CurrentTarget == null && frame.BoostConsumed && frame.ReleaseKind == MechanicalClawReleaseKind.Boost,
                            "real Space publishes Boost then target-null Released immediately in the same cast and physics tick");
                    }
                }
                catch (Exception exception) { observerFailure = exception; }
            };
            claw.ClawFrameChanged += observer;
            // Use the approved independent keyboard claw, not the retired E/LMB path.
            ulong castTick = Engine.GetPhysicsFrames();
            ActionKey(player, Key.W, true);
            try
            {
                ActionKey(player, Key.K, true);
                await Frame(tree);
            }
            finally
            {
                ActionKey(player, Key.K, false);
                ActionKey(player, Key.W, false);
            }
            Invariant();
            for (int i = 0; i < 299 && claw.CurrentFrame.State is not
                (MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing); i++)
            { await Frame(tree); Invariant(); }
            TestAssert.True(claw.CurrentFrame.State is (MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing) &&
                claw.CurrentTarget == intended && castId != 0 && Engine.GetPhysicsFrames() <= castTick + 300,
                "real keyboard K beam attachment is reached within300 actual frames");
            await Frame(tree, 2); Invariant();
            TestAssert.True(guidance.CurrentInstruction.Targets.Count == 1 &&
                guidance.CurrentInstruction.Targets[0] == PrologueGuidanceTargetKey.ClawAttachment &&
                guidance.CurrentInstruction.StepText.Contains("A/D", StringComparison.Ordinal) &&
                guidance.CurrentInstruction.StepText.Contains("SPACE", StringComparison.OrdinalIgnoreCase),
                "normal controller processing teaches actual A/D and Space with only live ClawAttachment");
            AssertLabel("attachment");
            GD.Print($"TASK18_TEMPLE_GUIDANCE_ATTACHED cast={castId} tick={Engine.GetPhysicsFrames()} state={claw.CurrentFrame.State} anchor={claw.CurrentFrame.AnchorPoint} normal={claw.CurrentFrame.SurfaceNormal} lockedId={claw.CurrentFrame.LockedTargetRuntimeId} target={intended.GetPath()} prompt={visual.PromptRect} textBounds={visual.RenderedTextBounds} contained={visual.RenderedTextContained} text={label.Text}");

            spaceRequested = true;
            ActionKey(player, Key.Space, true); await Frame(tree); ActionKey(player, Key.Space, false); Invariant();
            TestAssert.True(boosts == 1 && releases == 1 && claw.CurrentTarget == null,
                "one real held Space frame finishes the single Boost release transaction");
            bool restored = false;
            for (int i = 0; i < 3; i++)
            {
                await Frame(tree); Invariant();
                restored = guidance.CurrentInstruction.Targets.Count == 1 &&
                    guidance.CurrentInstruction.Targets[0] == PrologueGuidanceTargetKey.BossActivation &&
                    guidance.CurrentInstruction.StepText == baseline && visual.RenderedStepText == baseline &&
                    label.Text.Contains(baseline, StringComparison.Ordinal);
                if (restored) break;
            }
            TestAssert.True(restored, "within3 natural process frames Space release restores complete ordinary Boss activation guidance");
            AssertLabel("released"); Invariant();
            GD.Print($"TASK18_TEMPLE_GUIDANCE_RELEASE cast={castId} boosts={boosts} releases={releases} boostTick={boostTick} releaseTick={releaseTick} targetNull={claw.CurrentTarget == null} restoredTarget=BossActivation objective={completion.CurrentObjective} hp={Health(player)} recoveryRequests={recoveryRequests} recoveryCompletions={recoveryCompletions} jumpsDelta={motor.AcceptedJumpCount - jumps} dashesDelta={motor.AcceptedDashCount - dashes} prompt={visual.PromptRect} text={label.Text}");
        }
        catch (Exception exception) { AddFailure(exception); }
        finally
        {
            if (observerFailure != null) AddFailure(new InvalidOperationException("Temple guidance late observer failure", observerFailure));
            try { if (GodotObject.IsInstanceValid(claw) && observer != null) claw.ClawFrameChanged -= observer; }
            catch (Exception exception) { AddFailure(exception); }
            try
            {
                if (GodotObject.IsInstanceValid(level) && recoverySubscribed)
                { level.RecoveryRequested -= OnRequested; level.RecoveryCompleted -= OnCompleted; }
            }
            catch (Exception exception) { AddFailure(exception); }
            try { if (inputDrive) PrologueCanyonTraversalTests.EndTask10InputDrive(); ReleaseInputs(player); }
            catch (Exception exception) { AddFailure(exception); }
            try
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    else level.Free();
                }
                TestAssert.True(!GodotObject.IsInstanceValid(level), "actual guidance level is freed after awaited teardown");
            }
            catch (Exception exception) { AddFailure(exception); }
        }
        if (failure != null) throw failure;
        GD.Print("TASK18_TEMPLE_GUIDANCE_RUNTIME_OK");
    }
}
