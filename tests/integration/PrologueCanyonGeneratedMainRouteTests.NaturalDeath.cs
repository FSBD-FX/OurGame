using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Shared;
using Knight.Ui;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Catches a real hazard failing to kill, duplicate/missing death recovery,
    // recovery to the wrong checkpoint, actor replacement, or lost collected IDs.
    // No seed save, direct damage, health/position writes or authored scene changes.
    // This is an engine-key route, not proof of OS/human keyboard origin.
    internal static async Task RunNaturalHazardRecovery(SceneTree tree)
    {
        Task10ApplicationPersistenceGuard persistence = Task10ApplicationPersistenceGuard.CreateFromArguments();
        using PackedScene packed = GD.Load<PackedScene>(MainPath);
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        PrologueGameFlowController flow = ConfigureTask10Persistence(level, persistence);
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        ulong playerId = player.GetInstanceId();
        TestAssert.Equal(new Vector2(160, 600), player.Position, "natural death uses the original unmodified spawn");
        using var keyboard = KeyboardRouteInputDriver.Begin(player);
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        var events = new List<NaturalDeathEvent>();
        int deaths = 0, requests = 0, completions = 0;
        bool armed = false, completedProof = false;
        CanyonShieldLaneHazard lane = null;
        string failure = null;
        var observationFailures = new List<string>();
        RouteMonitor monitor = null;
        async Task CaptureStage(string name)
        {
            if (DisplayServer.GetName() == "headless") return;
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image frame = tree.Root.GetTexture().GetImage();
            TestAssert.True(frame.GetWidth() == 1280 && frame.GetHeight() == 720,
                "natural recovery screenshots preserve the actual 1280x720 viewport");
            TestAssert.Equal(Error.Ok, frame.SavePng(persistence.ResolveWritablePath(name + ".png")),
                "natural recovery screenshot is preserved in the unique case directory");
        }
        void Record(string kind, string reason = "") => events.Add(new NaturalDeathEvent(
            kind, Engine.GetPhysicsFrames(), health.CurrentHealth, player.GlobalPosition.X,
            player.GlobalPosition.Y, reason, GodotObject.IsInstanceValid(lane) &&
                lane.IsInsideTree() && lane.GetOverlappingBodies().Contains(player)));
        void Damaged(int remaining, Vector2 impulse)
        {
            if (!armed) return;
            try
            {
                // Read only the identity the real health receiver just accepted.
                // This distinguishes lane pulses from nearby live enemy attacks.
                var field = typeof(HealthComponent).GetField("_processedAttackOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field?.GetValue(health) is not Queue<DamageAttackIdentity> identities || identities.Count == 0)
                    throw new InvalidOperationException("accepted damage identity unavailable");
                DamageAttackIdentity identity = identities.Last();
                Node source = GodotObject.InstanceFromId(identity.SourceRuntimeId) as Node;
                bool liveSource = GodotObject.IsInstanceValid(source) && source.IsInsideTree() && level.IsAncestorOf(source);
                Record("damaged", $"remaining={remaining};impulse={impulse}");
                events[^1] = events[^1] with { SourceId = identity.SourceRuntimeId,
                    AttackId = identity.LocalAttackId, SourcePath = liveSource ? source.GetPath().ToString() : "not-live" };
            }
            catch (Exception error) { observationFailures.Add(error.ToString()); }
        }
        void Died()
        {
            deaths++;
            Record("died");
            keyboard.ReleaseAll();
        }
        void Requested(string reason) { requests++; Record("requested", reason); }
        void Recovered(string reason) { completions++; Record("completed", reason); }
        // Before mount, so the Died receipt precedes the level's Died subscriber.
        health.Damaged += Damaged;
        health.Died += Died;
        level.RecoveryRequested += Requested;
        level.RecoveryCompleted += Recovered;
        try
        {
            tree.Root.AddChild(level);
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var evidence = new Task10RouteEvidence(level, world, player);
            monitor = new RouteMonitor(level, player, player.GetNode<Camera2D>("Camera2D"),
                world, playerId, allowRouteBacktracking: true, additionalObservation: evidence.Observe);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.State == PrologueGameFlowState.Title &&
                level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").HasFocus(),
                "natural death begins from actual focused Title Start");
            SendUiAccept(level, true);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            SendUiAccept(level, false);
            for (int frame = 0; frame < 12 && flow.State != PrologueGameFlowState.Playing; frame++)
                await PhysicsFrames(tree, monitor, 1);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "Enter begins actual new gameplay");
            await DriveTask10NormalRoute(tree, level, world, player, monitor, evidence,
                monitor.Observe, Task10RouteVariant.Main, false,
                persistence.ResolveWritablePath("unused-temple-snapshot.json"),
                atNaturalDeathCheckpoint: async () =>
                {
                    TestAssert.True(deaths == 0 && requests == 0 && completions == 0,
                        "Title-to-camp reaches the first checkpoint without an earlier recovery");
                    TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var basin),
                        "natural death basin is physically streamed in");
                    lane = basin.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
                    ulong laneId = lane.GetInstanceId();
                    TestAssert.True(lane.Damage == 1 && lane.Knockback == 90f &&
                        Mathf.IsEqualApprox(lane.PulseIntervalSeconds, .28f), "hazard retains its authored damage and timing");
                    string[] fragments = flow.Save.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                    TestAssert.True(fragments.Length > 0 && fragments.Distinct(StringComparer.Ordinal).Count() == fragments.Length,
                        "this recovery proof has actual nonempty, duplicate-free collected fragments");
                    await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 1860f, 100);
                    await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 1940f, 60);
                    await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 2090f, 60);
                    keyboard.ReleaseAll();
                    int startHealth = health.CurrentHealth;
                    TestAssert.True(startHealth > 0 && deaths == 0 && requests == 0 && completions == 0,
                        "unarmed approach remains alive before original pulse lane");
                    int startPulses = lane.PulseCount;
                    armed = true;
                    Record("armed", $"hazard={lane.GetPath()}#{laneId};pulses={startPulses}");
                    await CaptureStage("natural-death-01-approach");
                    // Rendering-authorized guidance has no presentation obligation
                    // in the separate headless physics-only route.
                    if (DisplayServer.GetName() != "headless")
                    {
                        PrologueGuidanceController guidance = level.GetNode<PrologueGuidanceController>("Guidance");
                        WorldTargetMarkerVisual card = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
                        TestAssert.True(guidance.CurrentInstruction.ContextTitle == "持续电磁场" &&
                            guidance.CurrentInstruction.StepText.Contains("右") &&
                            guidance.CurrentInstruction.StepText.Contains("举盾") &&
                            guidance.CurrentInstruction.Targets.Count == 0 && card.HasVisiblePrompt &&
                            card.RenderedStepText == guidance.CurrentInstruction.StepText && card.RenderedTextContained,
                            $"visible live lane teaches right-facing shield instead of the pending claw lesson; " +
                            $"objective={level.GetNode<PrologueCompletionController>("Completion").CurrentObjective}; " +
                            $"instruction={guidance.CurrentInstruction}");
                    }
                    bool capturedDamage = false;
                    for (int frame = 0; frame < 600 && deaths == 0; frame++)
                    {
                        // Input-only horizontal correction counters the real leftward impulse.
                        keyboard.Command(InputActions.MoveRight, player.GlobalPosition.X < 2260f);
                        keyboard.Command(InputActions.MoveLeft, player.GlobalPosition.X > 2280f);
                        await PhysicsFrames(tree, monitor, 1);
                        TestAssert.True(player.GlobalPosition.IsFinite() && health.CurrentHealth >= 0 &&
                            (deaths > 0 || player.GlobalPosition.X is > 2040f and < 2330f),
                            $"unshielded approach stays within the original supported lane; player={player.GlobalPosition}");
                        if (!capturedDamage && health.CurrentHealth < startHealth && deaths == 0)
                        {
                            capturedDamage = true;
                            await CaptureStage("natural-death-02-damaged");
                        }
                    }
                    keyboard.ReleaseAll();
                    TestAssert.Equal(1, deaths, "original pulse lane naturally produces exactly one Health.Died");
                    for (int frame = 0; frame < 120 && (completions == 0 ||
                         !player.IsOnFloor() || !player.IsPhysicsProcessing() ||
                         flow.State != PrologueGameFlowState.Playing); frame++)
                        await PhysicsFrames(tree, monitor, 1);
                    await PhysicsFrames(tree, monitor, 12);
                    TestAssert.True(requests == 1 && completions == 1 && deaths == 1,
                        "one real death pairs exactly one request/completion, no duplicate recovery");
                    monitor.AssertTask9Recoveries("natural pulse-lane death", 1);
                    var damage = events.Where(row => row.Kind == "damaged").ToArray();
                    TestAssert.True(observationFailures.Count == 0 && damage.Length == startHealth && damage.All(row => row.InLane) &&
                        damage.Select(row => row.Health).SequenceEqual(Enumerable.Range(0, startHealth).Reverse()),
                        "actual one-point damage reduces original health consecutively to zero with no observer gap");
                    TestAssert.True(damage.All(row => row.SourceId != 0 && row.AttackId != 0 && row.SourcePath != "not-live") &&
                        damage.Select(row => (row.SourceId, row.AttackId)).Distinct().Count() == damage.Length &&
                        damage[^1].SourceId == laneId && damage.Count(row => row.SourceId == laneId) >= 2,
                        "fatal hit and repeated pulses come from the exact authored lane; all other hits retain distinct live source identities");
                    int diedIndex = events.FindIndex(row => row.Kind == "died");
                    int requestIndex = events.FindIndex(row => row.Kind == "requested");
                    int completedIndex = events.FindIndex(row => row.Kind == "completed");
                    TestAssert.True(diedIndex > 0 && requestIndex > diedIndex && completedIndex > requestIndex &&
                        events[diedIndex].Health == 0 && events[requestIndex].Health == 0 &&
                        events[requestIndex].Reason == "DEATH RESET" && events[completedIndex].Reason == "DEATH RESET" &&
                        events[completedIndex].X == 1680f && events[completedIndex].Y == 3960f,
                        "HP0/Died precedes DEATH RESET and exact legal checkpoint completion");
                    TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin, out var recoveredBasin),
                        "recovered basin remains mounted");
                    StaticBody2D floor = recoveredBasin.GetNode<StaticBody2D>("Gameplay/Collision/BasinCampFloor");
                    TestAssert.True(player.GetInstanceId() == playerId && level.Player == player &&
                        level.GetNode("PlayerRoot").GetChildCount() == 1 && health.CurrentHealth == health.MaxHealth &&
                        flow.State == PrologueGameFlowState.Playing && !tree.Paused && player.IsPhysicsProcessing() &&
                        HasFloorContact(player, floor) && player.GlobalPosition.DistanceTo(new Vector2(1680, 3960)) < 2f &&
                        world.CurrentSector == PrologueSectorId.CanyonBasin &&
                        level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.basin-camp",
                        "same full-health actor resumes Playing on the legal basin floor, not next/start node");
                    TestAssert.True(fragments.SequenceEqual(flow.Save.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)),
                        "natural recovery retains the pre-death collection IDs");
                    using JsonDocument saved = JsonDocument.Parse(File.ReadAllText(flow.SavePath));
                    TestAssert.True(saved.RootElement.GetProperty("checkpointId").GetString() == "canyon.v2.basin-camp" &&
                        saved.RootElement.GetProperty("hasActivatedCheckpoint").GetBoolean() &&
                        fragments.SequenceEqual(saved.RootElement.GetProperty("collectedFragmentIds").EnumerateArray()
                            .Select(item => item.GetString()).OrderBy(id => id, StringComparer.Ordinal)),
                        "actual isolated disk save retains the physically activated checkpoint and exact nonempty fragment IDs");
                    Record("stable", $"startHealth={startHealth};damageCount={damage.Length};fragments={fragments.Length}");
                    await CaptureStage("natural-death-03-recovered");
                    float restoredX = player.GlobalPosition.X;
                    keyboard.Command(InputActions.MoveRight, true);
                    await PhysicsFrames(tree, monitor, 10);
                    keyboard.ReleaseAll();
                    await PhysicsFrames(tree, monitor, 12);
                    TestAssert.True(player.GlobalPosition.X > restoredX + 20f && HasFloorContact(player, floor) &&
                        health.CurrentHealth == health.MaxHealth && deaths == 1 && requests == 1 && completions == 1,
                        "new D input actually moves the recovered actor on the safe camp floor without another reset");
                    Record("playable-again");
                    completedProof = true;
                    GD.Print($"NATURAL_DEATH_RECOVERY_OK title=true checkpoint_actual=true hazard={laneId} " +
                        $"hp_start={startHealth} damage_events={damage.Length} lane_hits={damage.Count(row => row.SourceId == laneId)} died={deaths} request={requests} completed={completions} " +
                        $"player={playerId} position={player.GlobalPosition} floor=true engine_keyboard=true OS_origin=false full_route=false");
                });
            TestAssert.True(completedProof, "death branch must execute rather than silently fall through to the full route");
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            try
            {
                string resultPath = persistence.ResolveWritablePath("natural-death-result.json");
                File.WriteAllText(resultPath, JsonSerializer.Serialize(new
                {
                    completedProof, failure, observationFailures, deaths, requests, completions, playerId,
                    inputBasis = "engine key events, not OS/human", fullRoute = false, events
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            finally
            {
                try
                {
                    keyboard.ReleaseAll();
                    monitor?.Dispose();
                    health.Damaged -= Damaged;
                    health.Died -= Died;
                    level.RecoveryRequested -= Requested;
                    level.RecoveryCompleted -= Recovered;
                }
                finally
                {
                    try
                    {
                        if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
                            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    }
                    finally { persistence.AssertProtectedDefaultsUnchanged(); }
                }
            }
        }
    }

    private sealed record NaturalDeathEvent(string Kind, ulong Physics, int Health,
        float X, float Y, string Reason, bool InLane, ulong SourceId = 0,
        ulong AttackId = 0, string SourcePath = "");
}
