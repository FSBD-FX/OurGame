using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Tests.Integration;

// Actual saved Level/Flow, explicitly positioned/frozen resident fixtures.
// Not certified placements, normal gameplay routes or fifty formal spawns.
public static class ResidentLevelLifecycleRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await TransitionSpawnContext(tree);
        var f = new Fixture(tree, "resident-level-run-restore");
        try
        {
            await f.Open();
            TestAssert.True(f.Level.ResidentLayout.Count == 50 &&
                f.Level.ResidentLayout[0].Definition.Placement.WorldSpawn == new Vector2(666,604) &&
                f.Level.ResidentLayout[49].Definition.Placement.WorldSpawn == new Vector2(5153,1211),
                "real saved Level consumes all fifty versioned authored sites, not allocation-only null positions");
            foreach (var site in f.Level.ResidentLayout)
                TestAssert.True(!f.Level.Residents.TrySpawn(site.Definition.StableId, out _, out _, out var unresolved, true) &&
                    unresolved.Contains("scene_not_ready") && !unresolved.Contains("placement_not_authored"),
                    "actual Level manager received this authored placement but still refuses its unfinished scene: " + site.Definition.StableId);
            GD.Print("RESIDENT_LEVEL_AUTHORED_INPUT_OK identities=50 actual_manager=true unresolved_scenes=50 formal_spawned=0");
            TestAssert.True(f.Flow.State == PrologueGameFlowState.Title && f.Level.Residents != null &&
                f.Level.Residents.Epoch == 1 && f.Level.Residents.TrackedActorCount == 0,
                "real title Level owns one initialized catalog manager, not an isolated test-only owner");
            var actor = f.Resident(PrologueSectorId.CanyonEntrance);
            var health = actor.Actor.GetNode<HealthComponent>("Health");
            int resets = 0; actor.Actor.AttackFrameChanged += _ => resets++;
            TestAssert.True(health.ApplyDamage(new(1, Vector2.Right, 0, 52001)) && health.CurrentHealth == 2, "actual pre-newrun resident damage");
            f.Flow.StartNew();
            TestAssert.True(f.Flow.State == PrologueGameFlowState.Playing && f.Level.Residents.Epoch == 2 &&
                health.CurrentHealth == 3 && resets == 1 && f.Level.Residents.Snapshot(actor.Slot.StableId) is { Health: 3, Attached: true },
                "real Flow.StartNew clears epoch and actually resets/retracks still-mounted resident once");

            TestAssert.True(health.ApplyDamage(new(1, Vector2.Right, 0, 52002)), "actual post-newrun resident damage");
            bool badSaveRejected = false;
            try { f.Level.RestoreFromSave(new PrologueSaveData { Version = 999 }); }
            catch (System.IO.InvalidDataException) { badSaveRejected = true; }
            TestAssert.True(badSaveRejected && f.Level.Residents.Epoch == 2 && health.CurrentHealth == 2 && resets == 1,
                "rejected save cannot clear roster epoch, heal or reset actors before validation");
            var seed = PrologueSaveData.NewLayoutV2();
            seed.CollectedFragmentIds.AddRange(new[] { "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift", "canyon.v2.fragment.abyss-floor",
                "canyon.v2.fragment.gear-gallery", "canyon.v2.fragment.ascent-mid", "canyon.v2.fragment.courtyard" });
            string before = JsonSerializer.Serialize(seed);
            f.Level.RestoreFromSave(seed);
            TestAssert.True(f.Level.Residents.Epoch == 3 && health.CurrentHealth == 3 && resets == 2 &&
                f.Level.Progress.CollectedFragmentIds.SetEquals(seed.CollectedFragmentIds) &&
                before == JsonSerializer.Serialize(seed) && f.Level.Progress.CollectionRuleVersion == 1 &&
                !f.Level.Progress.LegacyCollectionQuotaExempt, "actual Level restore initializes runtime roster once without changing save/fragments/quota contract");
            GD.Print("RESIDENT_LEVEL_RUN_RESTORE_OK actual_level=true actual_flow_start=true restore_api=true resets=2 epochs=3 fragments=6 seed_unchanged=true formal_spawned=0");
        }
        finally { await f.Close(); }
        foreach (string scenario in new[] { "same", "retained", "absent" }) await Recovery(tree, scenario);
    }

    // A mounted callback is before CurrentSector/player/camera commit. A
    // reentrant request also leaves a queued transition after the outer call.
    // Neither intermediate state is authority for a resident birth transaction.
    private static async Task TransitionSpawnContext(SceneTree tree)
    {
        var f = new Fixture(tree, "resident-transition-spawn-context");
        int mounted = 0, admittedDuringMount = 0;
        bool subscribed = false;
        void DuringMount(PrologueSector sector)
        {
            mounted++;
            if (f.World.TryGetResidentSpawnContext(PrologueSectorId.CanyonDescent,
                out _, out _, out _, out _)) admittedDuringMount++;
            if (mounted == 1) f.World.ResetForNewRun();
        }
        try
        {
            await f.Open();
            ulong player = f.Level.Player.GetInstanceId();
            TestAssert.True(f.World.TryGetResidentSpawnContext(PrologueSectorId.CanyonDescent,
                out _, out _, out var captured, out _) && captured.GetInstanceId() == player,
                "committed actual title world supplies the captured resident context before transition");
            f.World.SectorMounted += DuringMount; subscribed = true;
            TestAssert.True(f.World.RestoreCheckpoint("canyon.v2.temple-forecourt"),
                "real checkpoint transition, not a test-written CurrentSector or player pose");
            f.World.SectorMounted -= DuringMount; subscribed = false;
            TestAssert.True(mounted > 0 && f.World.CurrentSector == PrologueSectorId.LostTemple &&
                f.Level.Player.GlobalPosition == new Vector2(5920, 1080) &&
                f.World.TryGetMountedSector(PrologueSectorId.CanyonAscent, out _),
                "outer transition really commits its destination while the reentrant start request remains queued");
            bool queuedAdmitted = f.World.TryGetResidentSpawnContext(PrologueSectorId.CanyonAscent,
                out _, out _, out _, out _);
            GD.Print($"RESIDENT_TRANSITION_CONTEXT_OBSERVED mounted_callbacks={mounted} admitted_during_mount={admittedDuringMount} queued_admitted={queuedAdmitted} player_id={player} actor_mounts=0");
            TestAssert.True(admittedDuringMount == 0 && !queuedAdmitted,
                "resident birth context must reject pre-commit and queued stream states; during=" + admittedDuringMount + ", queued=" + queuedAdmitted);
            f.Flow.StartNew();
            await f.Frames(4);
            TestAssert.True(f.World.CurrentSector == PrologueSectorId.CanyonEntrance &&
                f.World.TryGetResidentSpawnContext(PrologueSectorId.CanyonEntrance,
                    out _, out _, out var resumed, out _) && resumed.GetInstanceId() == player &&
                f.Level.Residents.TrackedActorCount == 0,
                "actual next physics drains queued transition and restores committed authority for the same player; no canonical births invented");
            GD.Print("RESIDENT_TRANSITION_CONTEXT_OK precommit_rejected=true queued_rejected=true committed_restored=true actual_flow=true actor_mounts=0 formal_spawned=0");
        }
        finally
        {
            if (subscribed && GodotObject.IsInstanceValid(f.World)) f.World.SectorMounted -= DuringMount;
            await f.Close();
        }
    }

    private sealed class Witness(RustShellSquire actor, ResidentPopulationDefinition slot, ulong sectorInstance)
    {
        public readonly RustShellSquire Actor = actor;
        public readonly ResidentPopulationDefinition Slot = slot;
        public readonly ulong SectorInstance = sectorInstance;
        public int ResetPublications;
    }
    private static async Task Recovery(SceneTree tree, string scenario)
    {
        var f = new Fixture(tree, "resident-recovery-" + scenario);
        PrologueLevelController.RecoveryCompletedEventHandler completed = null;
        try
        {
            await f.Open(); f.Flow.StartNew(); await f.Frames(4);
            var sourceId = scenario switch { "same" => PrologueSectorId.CanyonEntrance, "retained" => PrologueSectorId.CanyonDescent, _ => PrologueSectorId.LostTemple };
            if (scenario != "same")
            {
                // Lifecycle fixture positioning only, not a claimed playable route.
                f.Level.Player.GlobalPosition = scenario == "retained" ? new(1660, 1604) : new(5500, 1100);
                f.Level.Player.GetNode<PlayerMotor>("Motor").ResetTransientState();
                await f.Frames(12);
            }
            TestAssert.True(f.World.CurrentSector == sourceId && f.World.TryGetMountedSector(sourceId, out _), "real departing sector reached by fixture setup");
            f.World.TryGetMountedSector(sourceId, out var source); ulong sourceInstance = source.GetInstanceId();
            var witnesses = new List<Witness>();
            foreach (var id in f.World.MountedSectorIds)
            {
                if (!CanyonResidentCatalog.All.Any(x => x.Sector == id && x.Archetype == ResidentArchetype.Wanderer &&
                    x.Family is ResidentFamily.Native or ResidentFamily.Scrap)) continue;
                var resident = f.Resident(id); f.World.TryGetMountedSector(id, out var sector);
                var witness = new Witness(resident.Actor, resident.Slot, sector.GetInstanceId()); witnesses.Add(witness);
                TestAssert.True(resident.Actor.GetNode<HealthComponent>("Health").ApplyDamage(new(1, Vector2.Right, 0, 52100 + (ulong)id)), "real pre-recovery resident damage");
                resident.Actor.AttackFrameChanged += _ => witness.ResetPublications++;
            }
            long epoch = f.Level.Residents.Epoch; int completions = 0; bool resetStillPending = false;
            completed = _ =>
            {
                completions++;
                resetStillPending = f.ResetCounts.Values.All(x => x.Count == 0) &&
                    CanyonResidentCatalog.All.All(x => f.Level.Residents.Snapshot(x.StableId).ResetRevision == 0) &&
                    witnesses.All(x => x.ResetPublications == 0);
            };
            f.Level.RecoveryCompleted += completed;
            var health = f.Level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.CurrentHealth == health.MaxHealth && health.ApplyDamage(new(health.CurrentHealth, Vector2.Zero, 0,
                scenario == "same" ? 52201UL : scenario == "retained" ? 52202UL : 52203UL)), "real lethal player hit requests actual deferred recovery");
            for (int i = 0; i < 90 && completions == 0; i++) await f.Frames(1);
            TestAssert.True(completions == 1 && resetStillPending, "RecoveryCompleted really precedes enemy reset; manager must not reset early");
            await f.Frames(3);
            TestAssert.True(f.World.CurrentSector == PrologueSectorId.CanyonEntrance && f.World.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out _), "real recovery restored round-start destination");
            f.World.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out var destination);
            var expected = new HashSet<ulong> { destination.GetInstanceId() };
            bool sourceRetained = f.World.TryGetMountedSector(sourceId, out var retained) && retained.GetInstanceId() == sourceInstance;
            if (sourceRetained) expected.Add(sourceInstance);
            TestAssert.True(f.ResetCounts.All(x => x.Value.Count == (expected.Contains(x.Key) ? 1 : 0)), "actual World reset target instances exactly once; all other/new/retired neighbors zero");
            TestAssert.True((scenario != "absent" && sourceRetained) || (scenario == "absent" && !sourceRetained), "valid departing identity retained or actually unloaded as intended");
            foreach (var slot in CanyonResidentCatalog.All)
            {
                bool reset = slot.Sector == PrologueSectorId.CanyonEntrance || sourceRetained && slot.Sector == sourceId;
                TestAssert.Equal(reset ? 1L : 0L, f.Level.Residents.Snapshot(slot.StableId).ResetRevision,
                    "real reset selects complete sector roster, never neighbors: " + slot.StableId);
            }
            foreach (var witness in witnesses)
            {
                bool reset = expected.Contains(witness.SectorInstance); var snapshot = f.Level.Residents.Snapshot(witness.Slot.StableId);
                TestAssert.True(witness.ResetPublications == (reset ? 1 : 0) && snapshot.Health == (reset ? 3 : 2) && snapshot.Alive,
                    "actual actor reset publication/HP agree with custody, no manager double reset: " + witness.Slot.StableId);
            }
            TestAssert.True(f.Level.Residents.Epoch == epoch && completions == 1, "ordinary death recovery is not a new run");
            GD.Print($"RESIDENT_LEVEL_RECOVERY_OK scenario={scenario} actual_lethal=true destination_resets=1 source_resets={(sourceRetained ? 1 : 0)} neighbor_resets=0 epoch_unchanged=true formal_spawned=0");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(f.Level) && completed != null) f.Level.RecoveryCompleted -= completed;
            await f.Close();
        }
    }

    internal sealed class Fixture(SceneTree tree, string name)
    {
        public PrologueLevelController Level { get; private set; }
        public PrologueGameFlowController Flow { get; private set; }
        public SeamlessPrologueWorld World { get; private set; }
        private readonly List<Shape2D> _owned = new();
        private readonly List<(Shape2D Shape, Rect2 Bounds)> _borrowed = new();
        private readonly Dictionary<ulong, ArchiveResetCount> _resetCounts = new();
        public IReadOnlyDictionary<ulong, ArchiveResetCount> ResetCounts => _resetCounts;
        public async Task Open()
        {
            var paths = IsolatedPrologueFixture.CreatePaths(name);
            Level = PrologueGameFlowTests.Instantiate(name, paths.SavePath);
            // This fixture specifically tests the unfinished final registry and
            // manual custody actors. The normal saved development selection is
            // independently exercised by ResidentDevelopmentPackageTests.
            Level.ResidentContent = ResidentContentMode.FinalOnly;
            Flow = Level.GetNode<PrologueGameFlowController>("GameFlow"); Flow.AutomationAutoStart = false;
            World = Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            tree.NodeAdded += OnAdded; World.SectorMounted += OnMounted;
            tree.Root.AddChild(Level); await Frames(4);
        }
        private void OnAdded(Node node)
        {
            if (node is PrologueSector sector && ReferenceEquals(sector.GetParent(), World)) CloneShapes(sector);
        }
        private void OnMounted(PrologueSector sector)
        {
            Freeze(sector.GetNode("Gameplay/Actors"));
            Node groups = sector.GetNode("Gameplay/Encounters");
            for (int i = 0; i < groups.GetChildCount(); i++)
                if (groups.GetChild(i) is CanyonEncounterGroup group) { group.SetPhysicsProcess(false); group.SetProcess(false); }
            var count = new ArchiveResetCount(); ulong id = sector.GetInstanceId();
            _resetCounts.Add(id, count);
            sector.AddChild(new ArchiveCheckpointResetCountProbe { Name = "ResidentResetCount", Counter = count });
        }
        public (RustShellSquire Actor, ResidentPopulationDefinition Slot) Resident(PrologueSectorId id)
        {
            TestAssert.True(World.TryGetMountedSector(id, out var sector), "fixture sector actually mounted");
            var slot = CanyonResidentCatalog.All.First(x => x.Sector == id && x.Archetype == ResidentArchetype.Wanderer &&
                x.Family is ResidentFamily.Native or ResidentFamily.Scrap);
            Vector2 home = sector.GlobalPosition + sector.Bounds.Position + new Vector2(220, 250);
            slot = slot with { Placement = new(home, new(home - new Vector2(180, 180), new(360, 360)), new[] { home - new Vector2(32, 0), home + new Vector2(32, 0) }) };
            var actor = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
            try
            {
                actor.ConfigureResidence(slot); actor.Position = home - sector.GlobalPosition; CloneShapes(actor);
                sector.GetNode("Gameplay/Actors").AddChild(actor); Freeze(actor);
                TestAssert.True(Level.Residents.TryTrack(slot.StableId, actor, out _), "explicit fixture tracks actual saved actor, no canonical scene admission claim");
                return (actor, slot);
            }
            catch { if (GodotObject.IsInstanceValid(actor) && !actor.IsInsideTree()) actor.Free(); throw; }
        }
        public async Task Frames(int count)
        { for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
        private void CloneShapes(Node node)
        {
            if (node is CollisionShape2D collider && collider.Shape != null)
            {
                _borrowed.Add((collider.Shape, collider.Shape.GetRect()));
                var shape = (Shape2D)collider.Shape.Duplicate(); _owned.Add(shape); collider.Shape = shape;
            }
            for (int i = 0; i < node.GetChildCount(); i++) CloneShapes(node.GetChild(i));
        }
        private static void Freeze(Node node)
        { node.SetPhysicsProcess(false); node.SetProcess(false); for (int i = 0; i < node.GetChildCount(); i++) Freeze(node.GetChild(i)); }
        public async Task Close()
        {
            tree.NodeAdded -= OnAdded;
            if (GodotObject.IsInstanceValid(World)) World.SectorMounted -= OnMounted;
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(Level))
            {
                if (Level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, Level);
                else Level.Free();
            }
            TestAssert.True(!GodotObject.IsInstanceValid(Level) && _borrowed.All(x => x.Shape.GetRect() == x.Bounds), "actual level freed; borrowed saved shapes unchanged");
            int count = _owned.Count; foreach (var shape in _owned) shape.Dispose();
            GD.Print($"RESIDENT_LEVEL_FIXTURE_RELEASE owned_shapes={count} borrowed_disposed=0 normal_route=false");
            _owned.Clear(); _borrowed.Clear();
        }
    }
}
