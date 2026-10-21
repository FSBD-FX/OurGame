using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    // Catches the original first-death skip-ahead bug and repeated-death drift.
    // Walk/jump from authored Title spawn into the original Rust's live attacks;
    // never inject damage, change HP, place actors, seed a save or add terrain.
    internal static async Task RunBeforeCheckpointDeath(SceneTree tree)
    {
        Task10ApplicationPersistenceGuard persistence = Task10ApplicationPersistenceGuard.CreateFromArguments();
        using PackedScene packed = GD.Load<PackedScene>(MainPath);
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try { await RunBeforeCheckpointDeathInScene(tree, persistence, level); }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(level))
                {
                    if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                    else level.Free();
                }
            }
            finally { persistence.AssertProtectedDefaultsUnchanged(); }
        }
    }

    private static async Task RunBeforeCheckpointDeathInScene(SceneTree tree,
        Task10ApplicationPersistenceGuard persistence, PrologueLevelController level)
    {
        PrologueGameFlowController flow = ConfigureTask10Persistence(level, persistence);
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        HealthComponent health = player.GetNode<HealthComponent>("Health");
        ulong playerId = player.GetInstanceId();
        using var keyboard = KeyboardRouteInputDriver.Begin(player);
        var events = new List<EarlyDeathEvent>();
        var observationFailures = new List<string>();
        int cycle = 0, deaths = 0, requests = 0, completions = 0;
        bool completedProof = false;
        string failure = null;
        RouteMonitor monitor = null;
        void Record(string kind, string reason = "", ulong sourceId = 0, ulong attackId = 0, string path = "") =>
            events.Add(new EarlyDeathEvent(kind, cycle, Engine.GetPhysicsFrames(), health.CurrentHealth,
                player.GlobalPosition.X, player.GlobalPosition.Y, reason, sourceId, attackId, path));
        void Damaged(int remaining, Vector2 impulse)
        {
            try
            {
                var field = typeof(HealthComponent).GetField("_processedAttackOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field?.GetValue(health) is not Queue<DamageAttackIdentity> identities || identities.Count == 0)
                    throw new InvalidOperationException("accepted early damage identity unavailable");
                DamageAttackIdentity hit = identities.Last();
                Node source = GodotObject.InstanceFromId(hit.SourceRuntimeId) as Node;
                bool live = GodotObject.IsInstanceValid(source) && source.IsInsideTree() && level.IsAncestorOf(source);
                Record("damaged", $"remaining={remaining};impulse={impulse}", hit.SourceRuntimeId, hit.LocalAttackId,
                    live ? source.GetPath().ToString() : "not-live");
            }
            catch (Exception error) { observationFailures.Add(error.ToString()); }
        }
        void Died() { deaths++; Record("died"); keyboard.ReleaseAll(); }
        void Requested(string reason) { requests++; Record("requested", reason); }
        void Recovered(string reason) { completions++; Record("completed", reason); }
        health.Damaged += Damaged;
        health.Died += Died;
        level.RecoveryRequested += Requested;
        level.RecoveryCompleted += Recovered;
        try
        {
            TestAssert.Equal(new Vector2(160, 600), player.Position, "early-death proof retains the authored spawn");
            tree.Root.AddChild(level);
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            monitor = new RouteMonitor(level, player, player.GetNode<Camera2D>("Camera2D"), world, playerId,
                allowRouteBacktracking: true);
            void Observe()
            {
                monitor.Observe();
                PrologueCheckpointSnapshot checkpoint = level.Progress.RestoreCheckpoint();
                TestAssert.True(!checkpoint.HasActivatedCheckpoint && checkpoint.CheckpointId == "canyon.v2.start" &&
                    checkpoint.SectorId == PrologueSectorId.CanyonEntrance && checkpoint.WorldPosition == new Vector2(160, 600),
                    "before-first-checkpoint route never activates or advances the initial checkpoint");
            }
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(flow.State == PrologueGameFlowState.Title && !flow.HasSave &&
                !File.Exists(flow.SavePath) && level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start").HasFocus(),
                "early death begins at real Title with no prior or seeded slot");
            SendColdMenuKey(level, Key.Enter, true);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            SendColdMenuKey(level, Key.Enter, false);
            TestAssert.Equal(PrologueGameFlowState.Playing, flow.State, "actual Start input begins the early-death route");
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, player, Observe);
            try
            {
                for (cycle = 1; cycle <= 2; cycle++)
                {
                    await WaitForFloor(tree, monitor, 180);
                    Observe();
                    TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out var entrance),
                        "original entrance is mounted before each enemy approach");
                    StaticBody2D floor = entrance.GetNode<StaticBody2D>("Gameplay/Collision/StartFloor");
                    StaticBody2D farBank = entrance.GetNode<StaticBody2D>("Gameplay/Collision/BridgeFarBank");
                    TestAssert.True(health.MaxHealth == 5 && health.CurrentHealth == 5 && deaths == cycle - 1,
                        "each approach starts with the authored five HP and only the preceding completed deaths");
                    await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 300f, 80);
                    await PrologueCanyonTraversalTests.Task4CrossEntranceGap(tree, player, farBank);
                    await PrologueCanyonTraversalTests.Task4WalkTo(tree, player, 520f, 80);
                    TestAssert.True(HasFloorContact(player, farBank),
                        "first-enemy approach stays on the actual horizontal bank, not a deleted stair");
                    keyboard.ReleaseAll();
                    RustShellSquire enemy = entrance.GetNode<Node>("Gameplay/Actors").GetChildren().OfType<RustShellSquire>().Single();
                    Hitbox hitbox = enemy.GetNode<Hitbox>("AttackHitbox");
                    ulong hitboxId = hitbox.GetInstanceId();
                    TestAssert.True(enemy.Name.ToString().Contains("low-ceiling-bridge", StringComparison.Ordinal) &&
                        enemy.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                        "lethal source is the original living first encounter, not a spawned test enemy");
                    string[] fragments = flow.Save.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                    TestAssert.True(fragments.SequenceEqual(new[] { "canyon.v2.fragment.entrance" }),
                        "walking through the entrance physically collects exactly one nonempty stable fragment ID");
                    Record("approach", $"enemy={enemy.GetPath()};hitbox={hitboxId};fragments={fragments.Length}");
                    for (int frame = 0; frame < 1200 && deaths < cycle; frame++)
                    {
                        // The Rust can telegraph without changing its current facing.
                        // Move toward the real forward hitbox on the authored horizontal bank.
                        float target = Mathf.Clamp(hitbox.GlobalPosition.X, 520f, 600f);
                        keyboard.Command(InputActions.MoveRight, player.GlobalPosition.X < target - 3f);
                        keyboard.Command(InputActions.MoveLeft, player.GlobalPosition.X > target + 3f);
                        await PhysicsFrames(tree, monitor, 1);
                        ObserveCheckpointOnly();
                        if (frame % 120 == 0 && deaths < cycle)
                            GD.Print($"EARLY_DEATH_OBSERVE cycle={cycle} frame={frame} hp={health.CurrentHealth} player={player.GlobalPosition} " +
                                $"enemy={enemy.GlobalPosition} facing={enemy.Facing} state={enemy.CurrentState} hitbox={hitbox.GlobalPosition}");
                    }
                    keyboard.ReleaseAll();
                    TestAssert.True(deaths == cycle && requests == cycle,
                        "original first-enemy damage produces Health.Died, not a fall-reset substitute");
                    for (int frame = 0; frame < 120 && (completions < cycle || !player.IsOnFloor() ||
                         flow.State != PrologueGameFlowState.Playing || !player.IsPhysicsProcessing()); frame++)
                        await PhysicsFrames(tree, monitor, 1);
                    await PhysicsFrames(tree, monitor, 12);
                    Observe();
                    var current = events.Where(row => row.Cycle == cycle).ToArray();
                    var damage = current.Where(row => row.Kind == "damaged").ToArray();
                    TestAssert.True(observationFailures.Count == 0 && damage.Length == 5 &&
                        damage.Select(row => row.Health).SequenceEqual(new[] { 4, 3, 2, 1, 0 }) &&
                        damage.All(row => row.SourceId == hitboxId && row.AttackId > 0 && row.SourcePath != "not-live") &&
                        damage.Select(row => row.AttackId).Distinct().Count() == damage.Length,
                        "five real distinct accepted attacks from this exact first-enemy hitbox reduce full health to zero");
                    int diedIndex = Array.FindIndex(current, row => row.Kind == "died");
                    int requestIndex = Array.FindIndex(current, row => row.Kind == "requested");
                    int completeIndex = Array.FindIndex(current, row => row.Kind == "completed");
                    TestAssert.True(diedIndex > 0 && requestIndex > diedIndex && completeIndex > requestIndex &&
                        current[diedIndex].Health == 0 && current[requestIndex].Health == 0 &&
                        current[requestIndex].Reason == "DEATH RESET" && current[completeIndex].Reason == "DEATH RESET" &&
                        current[completeIndex].X == 160f && current[completeIndex].Y == 600f,
                        "real HP0/Died precedes exactly the legal start restoration, not a future checkpoint");
                    monitor.AssertTask9Recoveries("early death cycle " + cycle, cycle);
                    TestAssert.True(deaths == cycle && requests == cycle && completions == cycle &&
                        player.GetInstanceId() == playerId && level.Player == player &&
                        level.GetNode("PlayerRoot").GetChildCount() == 1 && health.MaxHealth == 5 && health.CurrentHealth == 5 &&
                        flow.State == PrologueGameFlowState.Playing && !tree.Paused && player.IsPhysicsProcessing() &&
                        world.CurrentSector == PrologueSectorId.CanyonEntrance && HasFloorContact(player, floor) &&
                        player.GlobalPosition.DistanceTo(new Vector2(160, 600)) < 2f,
                        "same actor fully recovers on the original StartFloor before any checkpoint was activated");
                    monitor.AssertCameraCoverage();
                    monitor.AssertFullCapsuleVisible();
                    using JsonDocument disk = JsonDocument.Parse(File.ReadAllText(flow.SavePath));
                    JsonElement saved = disk.RootElement;
                    TestAssert.True(!flow.Save.HasActivatedCheckpoint && flow.Save.CheckpointId == "canyon.v2.start" &&
                        saved.GetProperty("version").GetInt32() == 4 && saved.GetProperty("layoutVersion").GetInt32() == 2 &&
                        saved.GetProperty("checkpointSector").GetString() == "CanyonEntrance" &&
                        saved.GetProperty("collectionRuleVersion").GetInt32() == 1 &&
                        !saved.GetProperty("legacyCollectionQuotaExempt").GetBoolean() &&
                        !saved.GetProperty("bossDefeated").GetBoolean() && !saved.GetProperty("completed").GetBoolean() &&
                        !saved.GetProperty("hasActivatedCheckpoint").GetBoolean() &&
                        saved.GetProperty("checkpointId").GetString() == "canyon.v2.start" &&
                        saved.GetProperty("checkpointWorldX").GetSingle() == 160f &&
                        saved.GetProperty("checkpointWorldY").GetSingle() == 600f &&
                        fragments.SequenceEqual(flow.Save.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)) &&
                        fragments.SequenceEqual(level.Progress.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)) &&
                        fragments.SequenceEqual(saved.GetProperty("collectedFragmentIds").EnumerateArray()
                            .Select(item => item.GetString()).OrderBy(id => id, StringComparer.Ordinal)),
                        "early death does not advance the disk checkpoint or lose collected IDs in memory/disk");
                    Record("stable", $"fragments={fragments.Length}");
                    float restoredX = player.GlobalPosition.X;
                    keyboard.Command(InputActions.MoveRight, true);
                    await PhysicsFrames(tree, monitor, 10);
                    keyboard.ReleaseAll();
                    await PhysicsFrames(tree, monitor, 12);
                    TestAssert.True(player.GlobalPosition.X > restoredX + 20f && HasFloorContact(player, floor) &&
                        health.CurrentHealth == health.MaxHealth && deaths == cycle && requests == cycle && completions == cycle,
                        "fresh D works after each early death without another recovery");
                    Record("playable-again");
                    GD.Print($"EARLY_DEATH_CYCLE_OK cycle={cycle} hits={damage.Length} restored=(160,600) " +
                        $"fragments={fragments.Length} floor=true player={playerId} moved={player.GlobalPosition}");
                }
                completedProof = true;
                GD.Print("EARLY_DEATH_OK cycles=2 checkpoint_activated=false input=engine_keyboard OS_origin=false full_route=false");
            }
            finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }

            void ObserveCheckpointOnly()
            {
                TestAssert.True(!level.Progress.RestoreCheckpoint().HasActivatedCheckpoint &&
                    level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.start",
                    "waiting for the original enemy never activates a checkpoint");
            }
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            try
            {
                File.WriteAllText(persistence.ResolveWritablePath("early-death-result.json"), JsonSerializer.Serialize(new
                {
                    completedProof, failure, observationFailures, deaths, requests, completions, playerId,
                    inputBasis = "engine key events, not OS/human", fullRoute = false, events
                }, new JsonSerializerOptions { WriteIndented = true }));
            }
            finally
            {
                keyboard.ReleaseAll();
                monitor?.Dispose();
                health.Damaged -= Damaged;
                health.Died -= Died;
                level.RecoveryRequested -= Requested;
                level.RecoveryCompleted -= Recovered;
            }
        }
    }

    private sealed record EarlyDeathEvent(string Kind, int Cycle, ulong Physics, int Health,
        float X, float Y, string Reason, ulong SourceId, ulong AttackId, string SourcePath);
}
