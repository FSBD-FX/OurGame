using System;
using System.Collections.Generic;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Unit;

public static class ResidentPopulationStateTests
{
    public static void Run()
    {
        int cases = 0; var failures = new List<string>();
        Check("real_allocation_unknown_hp_not_fictitious_spawn", () =>
        {
            var state = new ResidentPopulationState(CanyonResidentCatalog.All);
            TestAssert.Equal(50, state.Count, "all stable slots retained independently of unready scenes");
            foreach (var slot in CanyonResidentCatalog.All)
            {
                var snap = state.Snapshot(slot.StableId);
                TestAssert.True(snap.Alive && snap.Health == null && snap.MaxHealth == null && !snap.Attached,
                    "unobserved slot has no invented health or native instance");
            }
        });
        Check("literal_lease_and_immutable_hp", () =>
        {
            var s = State(); var before = s.Snapshot("one");
            TestAssert.True(s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 3, 3, out var lease), "first actual instance accepted");
            TestAssert.True(lease == new ResidentLease("one", 1, 11, 21, 1) && s.Owns(lease), "literal full lease identity");
            TestAssert.True(s.ObserveHealth(lease, 2), "actual damage observation accepted");
            TestAssert.True(before.Health == null && !before.Attached && s.Snapshot("one").Health == 2,
                "old immutable snapshot not silently updated");
        });
        Check("duplicate_and_impostor_do_not_steal_slot", () =>
        {
            var s = State(); Attach(s, out var lease);
            TestAssert.True(!s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 22, 3, 3, out _), "second actor rejected");
            foreach (var bad in new[] { lease with { Epoch = 2 }, lease with { ActorInstance = 22 },
                lease with { SectorInstance = 12 }, lease with { StableId = "two" }, lease with { Generation = 2 } })
                TestAssert.True(!s.ObserveHealth(bad, 0) && !s.MarkDefeated(bad) && !s.Detach(bad), "every stale dimension rejected");
            TestAssert.True(s.Snapshot("one").Alive && s.Snapshot("one").Health == 3 && s.Owns(lease), "impostors left owner unchanged");
        });
        Check("one_native_actor_cannot_count_as_two_residents", () =>
        {
            var s = State(); Attach(s, out var lease);
            TestAssert.True(!s.TryAttach("two", PrologueSectorId.CanyonDescent, 11, 21, 3, 3, out _),
                "same native actor cannot own a second stable slot");
            TestAssert.True(s.Snapshot("two").Health == null && !s.Snapshot("two").Attached && s.Owns(lease),
                "rejection leaves both identities intact");
        });
        Check("live_unload_retains_hp_no_healing_on_remount", () =>
        {
            var s = State(); Attach(s, out var old); s.ObserveHealth(old, 2);
            TestAssert.True(s.Detach(old) && !s.Detach(old), "one detach only");
            var unloaded = s.Snapshot("one");
            TestAssert.True(unloaded.Alive && unloaded.Health == 2 && !unloaded.Attached, "unload is not defeat");
            TestAssert.True(!s.TryAttach("one", PrologueSectorId.CanyonDescent, 12, 22, 3, 3, out _), "full health replacement cannot silently heal");
            TestAssert.True(s.TryAttach("one", PrologueSectorId.CanyonDescent, 12, 22, 2, 3, out var current), "matching preserved health admitted");
            TestAssert.True(current.Generation == 2 && !s.MarkDefeated(old) && s.Owns(current), "old callback cannot defeat replacement");
        });
        Check("dead_is_monotonic_within_run", () =>
        {
            var s = State(); Attach(s, out var lease);
            TestAssert.True(s.ObserveHealth(lease, 0) && s.MarkDefeated(lease), "health-zero followed by actor defeat is idempotent");
            TestAssert.True(!s.ObserveHealth(lease, 3), "old healing cannot revive corpse");
            TestAssert.True(s.Detach(lease) && !s.Snapshot("one").Alive && s.Snapshot("one").Health == 0, "death retained after unload");
            TestAssert.True(!s.TryAttach("one", PrologueSectorId.CanyonDescent, 12, 22, 3, 3, out _), "dead slot cannot remount this run");
        });
        Check("explicit_epoch_reset_invalidates_still_mounted_callbacks", () =>
        {
            var s = State(); Attach(s, out var old); s.MarkDefeated(old); s.BeginRun();
            TestAssert.True(s.Epoch == 2 && s.Snapshot("one").Alive && s.Snapshot("one").Health == null && !s.Owns(old), "new run explicitly clears old state");
            TestAssert.True(s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 3, 3, out var current), "same mounted native instance can join new epoch after actual reset");
            TestAssert.True(!s.MarkDefeated(old) && current.Epoch == 2 && s.Snapshot("one").Alive, "old epoch cannot kill identical native IDs");
        });
        Check("invalid_admission_is_atomic", () =>
        {
            var s = State();
            TestAssert.True(!s.TryAttach("missing", PrologueSectorId.CanyonDescent, 11, 21, 3, 3, out _) &&
                !s.TryAttach("one", PrologueSectorId.CanyonBasin, 11, 21, 3, 3, out _) &&
                !s.TryAttach("one", PrologueSectorId.CanyonDescent, 0, 21, 3, 3, out _) &&
                !s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 0, 3, 3, out _) &&
                !s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 0, 3, out _) &&
                !s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 4, 3, out _) &&
                !s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 1, 0, out _), "invalid identifiers/HP rejected");
            Attach(s, out var lease);
            TestAssert.True(lease.Generation == 1 && !s.ObserveHealth(lease, -1) && !s.ObserveHealth(lease, 4) && s.Snapshot("one").Health == 3,
                "rejections consume no generation or health");
        });
        Check("allocation_constructor_validates_identity", () =>
        {
            var d = Definition("one");
            Reject(() => new ResidentPopulationState(new[] { d, d }));
            Reject(() => new ResidentPopulationState(new[] { d with { StableId = " " } }));
            Reject(() => new ResidentPopulationState(new[] { d with { Sector = (PrologueSectorId)999 } }));
            Reject(() => new ResidentPopulationState(new ResidentPopulationDefinition[] { null }));
            Reject(() => new ResidentPopulationState(null));
        });
        Check("sector_reset_preserves_neighbors_and_invalidates_same_instance_lease", () =>
        {
            var descent = Definition("one");
            var basin = Definition("neighbor") with { Sector = PrologueSectorId.CanyonBasin };
            var s = new ResidentPopulationState(new[] { descent, basin });
            Attach(s, out var old);
            TestAssert.True(s.TryAttach("neighbor", PrologueSectorId.CanyonBasin, 12, 22, 3, 3, out var other), "neighbor admitted");
            s.ObserveHealth(other, 2); s.MarkDefeated(old); s.ResetSector(PrologueSectorId.CanyonDescent);
            TestAssert.True(s.Epoch == 1 && s.Snapshot("one") is { Alive: true, Health: null, Attached: false, ResetRevision: 1 },
                "explicit sector reset clears its death/HP without changing run epoch");
            TestAssert.True(s.Owns(other) && s.Snapshot("neighbor").Health == 2 && s.Snapshot("neighbor").ResetRevision == 0,
                "neighbor HP/lease/revision unchanged");
            TestAssert.True(s.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 3, 3, out var replacement) &&
                replacement.Generation == 2 && !s.MarkDefeated(old) && s.Owns(replacement),
                "same native IDs after actual reset get a new generation, never revive old token");
        });
        GD.Print($"RESIDENT_STATE_RESULT cases={cases} failures={failures.Count} scalar_only=true formal_spawned=0");
        TestAssert.True(failures.Count == 0, string.Join(" | ", failures));

        void Check(string name, Action body) { cases++; try { body(); GD.Print("RESIDENT_STATE_OK " + name); }
            catch (Exception e) { failures.Add(name + ": " + e.Message); GD.Print("RESIDENT_STATE_FAIL " + name + ": " + e.Message); } }
    }
    private static ResidentPopulationState State() => new(new[] { Definition("one"), Definition("two") });
    private static ResidentPopulationDefinition Definition(string id) => new(id, "rock-wanderer", CanyonRouteNodeId.LowCeilingBridge,
        PrologueSectorId.CanyonDescent, ResidentFamily.Native, ResidentArchetype.Wanderer);
    private static void Attach(ResidentPopulationState state, out ResidentLease lease) => TestAssert.True(
        state.TryAttach("one", PrologueSectorId.CanyonDescent, 11, 21, 3, 3, out lease), "legitimate initial lease required");
    private static void Reject(Action action) { try { action(); } catch (ArgumentException) { return; }
        throw new InvalidOperationException("Expected argument rejection"); }
}
