using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Prologue.Residents;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Real V2 streaming and saved actor events. Explicit fixture placement/frozen AI,
// NOT formal population spawning, remount HP restoration or normal-route proof.
public static class ResidentLifecycleRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var f = new ResidentVoidStreamRuntimeTests.Fixture(tree);
        CanyonResidentManager manager = null;
        RustShellSquire stalePrepared = null;
        try
        {
            f.Initialize(); var world = f.AddWorld(); await f.Frames(3);
            var one = Definition("live"); var two = Definition("dead"); var unused = Definition("unused");
            manager = new(world, new[] { one, two, unused });
            TestAssert.Equal(3, manager.MountedSectorCount, "late bind enumerates actual registered neighbors, not only current sector");
            CanyonResidentManager duplicateOwner = null; bool duplicateRejected = false;
            try { duplicateOwner = new(world, new[] { one, two, unused }); }
            catch (InvalidOperationException) { duplicateRejected = true; }
            finally { duplicateOwner?.Dispose(); }
            TestAssert.True(duplicateRejected && manager.MountedSectorCount == 3, "one logical world has exactly one resident custody owner");
            CheckHealthPreparationGuards();
            var original = f.Sector(world, PrologueSectorId.CanyonDescent);
            var living = Mount(f, original, one); var dying = Mount(f, original, two);
            TestAssert.True(manager.TryTrack("live", living, out var first), "first real actor accepted");
            TestAssert.True(!manager.TryTrack("dead", living, out _), "one native body cannot count as two resident identities");
            TestAssert.True(manager.TryTrack("dead", dying, out var deadLease),
                "real saved configured actors admitted under exact registered Gameplay/Actors");
            TestAssert.True(manager.TryTrack("live", living, out var same) && same == first && manager.TrackedActorCount == 2,
                "repeat binding is idempotent, no duplicate handlers/leases");
            var duplicate = Mount(f, original, one);
            TestAssert.True(!manager.TryTrack("live", duplicate, out _) && manager.TrackedActorCount == 2,
                "same-slot second actor cannot take custody");
            var health = living.GetNode<HealthComponent>("Health");
            TestAssert.Equal(3, health.CurrentHealth, "literal saved Rust initial health");
            TestAssert.True(health.ApplyDamage(new(1, Vector2.Right, 0, 51001)), "real nonlethal Health signal");
            TestAssert.True(manager.Snapshot("live").Health == 2 && manager.Snapshot("live").Alive,
                "native damage observed without mirror injection");
            var fatalHealth = dying.GetNode<HealthComponent>("Health"); int defeats = 0;
            dying.Defeated += () => defeats++;
            TestAssert.True(fatalHealth.ApplyDamage(new(3, Vector2.Right, 0, 51002)) && defeats == 1,
                "actual saved actor publishes one real defeat");
            TestAssert.True(!manager.Snapshot("dead").Alive && manager.Snapshot("dead").Health == 0,
                "zero HP/Defeated latches death in run custody");
            GD.Print("RESIDENT_LIFECYCLE_NATIVE damage_hp=2 defeated=1 repeat_bind=true duplicate_rejected=true");

            await f.Go(world, PrologueSectorId.LostTemple, new(5920, 1080));
            TestAssert.True(!GodotObject.IsInstanceValid(living) && !GodotObject.IsInstanceValid(dying) && manager.TrackedActorCount == 0,
                "actual retirement frees actors and detaches every binding");
            TestAssert.True(manager.Snapshot("live") is { Alive: true, Health: 2, Attached: false } &&
                manager.Snapshot("dead") is { Alive: false, Health: 0, Attached: false }, "unload preserves live HP and dead state, not respawn");
            TestAssert.Equal(world.MountedSectorIds.Count, manager.MountedSectorCount, "unmount tracks registry rather than stale current-sector callback");
            await f.Go(world, PrologueSectorId.CanyonBasin, new(1680, 3960));
            var replacementSector = f.Sector(world, PrologueSectorId.CanyonDescent);
            TestAssert.True(replacementSector.GetInstanceId() != first.SectorInstance && manager.MountedSectorCount == 3,
                "real replacement registered; mount callback bound before CurrentSector changes");
            var replacement = Mount(f, replacementSector, one);
            TestAssert.True(!manager.TryTrack("live", replacement, out _) && manager.Snapshot("live").Health == 2,
                "fresh full-HP replacement without captured-health preparation is rejected; no silent healing");
            replacement.QueueFree(); await f.Frames(2);
            stalePrepared = Create(one);
            TestAssert.True(manager.TryPrepareRestoredHealth("live", stalePrepared), "prepare a real prior-epoch actor, not an invented callback token");
            replacement = Create(one);
            var restoredHealth = replacement.GetNode<HealthComponent>("Health");
            int restoredDamageEvents = 0, restoredHealEvents = 0, restoredDeathEvents = 0;
            restoredHealth.Damaged += (_, _) => restoredDamageEvents++;
            restoredHealth.Healed += (_, _) => restoredHealEvents++;
            restoredHealth.Died += () => restoredDeathEvents++;
            try
            {
                TestAssert.True(manager.TryPrepareRestoredHealth("live", replacement), "captured positive HP can prepare a fresh off-tree actor");
                TestAssert.True(!manager.TryPrepareRestoredHealth("live", replacement), "preparation is once-only, not arbitrary live HP writes");
                replacement.Position = one.Placement.WorldSpawn - replacementSector.GlobalPosition;
                f.MountActor(replacementSector, replacement);
            }
            catch { if (GodotObject.IsInstanceValid(replacement) && !replacement.IsInsideTree()) replacement.Free(); throw; }
            TestAssert.True(restoredHealth.CurrentHealth == 2 && restoredDamageEvents == 0 && restoredHealEvents == 0 && restoredDeathEvents == 0 &&
                manager.TryTrack("live", replacement, out var remounted) && remounted.ActorInstance != first.ActorInstance && remounted.Generation == 2,
                "actual native Ready consumes captured HP exactly once with no forged combat/health events");
            TestAssert.True(!manager.TryPrepareRestoredHealth("live", replacement), "mounted actor cannot receive preparation");
            GD.Print("RESIDENT_LIFECYCLE_HP_REMOUNT actual_ready=true hp=2 damage_events=0 heal_events=0 death_events=0");
            var deadReplacement = Mount(f, replacementSector, two);
            TestAssert.True(!manager.TryTrack("dead", deadReplacement, out _), "dead resident cannot rejoin on sector reload");

            manager.BeginRun();
            stalePrepared.Position = one.Placement.WorldSpawn - replacementSector.GlobalPosition;
            f.MountActor(replacementSector, stalePrepared);
            TestAssert.True(!manager.TryTrack("live", stalePrepared, out _) && manager.Snapshot("live").Health == null,
                "prepared old-epoch actor cannot become first native owner in a new run");
            stalePrepared.QueueFree(); await f.Frames(2);
            GD.Print("RESIDENT_LIFECYCLE_STALE_PREPARATION rejected=true native_ready=true new_slot_unoccupied=true");
            replacement.ResetForCheckpoint(); deadReplacement.ResetForCheckpoint();
            TestAssert.True(manager.Epoch == 2 && manager.TryTrack("live", replacement, out var second) &&
                manager.TryTrack("dead", deadReplacement, out var secondDead) && second.Epoch == 2 && secondDead.Epoch == 2,
                "explicit new run on still-mounted sector after actual actor reset, not a missing mount event");
            var other = f.AddWorld(); await f.Frames(3);
            var foreign = Mount(f, f.Sector(other, PrologueSectorId.CanyonDescent), unused);
            TestAssert.True(!manager.TryTrack("unused", foreign, out _), "unoccupied slot rejects same physical space/sector ID in foreign logical world");
            var unregistered = f.AddUnregisteredDescent(world, new(41280, 0));
            var impostor = Mount(f, unregistered, unused);
            TestAssert.True(!manager.TryTrack("unused", impostor, out _), "unoccupied slot rejects same parent/id but unregistered sector instance");
            unregistered.QueueFree(); await f.Frames(2);
            TestAssert.True(manager.TrackedActorCount == 2 && manager.Snapshot("live").Alive,
                "foreign/unregistered exits cannot detach or defeat real replacement");
            var legalUnused = Mount(f, replacementSector, unused);
            replacementSector.SetMeta(SeamlessPrologueWorld.StagedSectorMeta, true);
            try { TestAssert.True(!manager.TryTrack("unused", legalUnused, out _), "staged registered sector cannot authorize a fresh unoccupied slot"); }
            finally { replacementSector.RemoveMeta(SeamlessPrologueWorld.StagedSectorMeta); }
            TestAssert.True(manager.TryTrack("unused", legalUnused, out var unusedLease) && manager.TrackedActorCount == 3,
                "same unoccupied slot accepts matching own-world actor after foreign negative controls");
            legalUnused.QueueFree();
            TestAssert.True(!manager.TryTrack("unused", legalUnused, out _), "queued actor loses admission before actual native deletion");
            await f.Frames(2);
            TestAssert.True(manager.TrackedActorCount == 2 && manager.Snapshot("unused") is { Alive: true, Health: 3, Attached: false },
                "native actor-only exit revokes lease without sector unload or death");
            var beforeDispose = manager.Snapshot("live"); manager.Dispose(); manager.Dispose();
            TestAssert.True(manager.TrackedActorCount == 0 && manager.MountedSectorCount == 0 &&
                !manager.TryTrack("live", replacement, out _), "idempotent disposal revokes bindings and future admission");
            TestAssert.True(replacement.GetNode<HealthComponent>("Health").ApplyDamage(new(1, Vector2.Right, 0, 51003)),
                "real actor remains alive and emits after tracking owner disposal");
            TestAssert.True(manager.Snapshot("live").Health == beforeDispose.Health && manager.Snapshot("live").Alive,
                "disposed manager no longer consumes health callbacks");
            f.AssertHealthy(); f.AssertBorrowedUnchanged();
            GD.Print("RESIDENT_LIFECYCLE_RESULT real_registry=true saved_actor=true live_unload_hp=2 dead_unload_hp=0 new_epoch=2 foreign_rejected=true restoration_implemented=true formal_spawned=0");
        }
        finally
        {
            manager?.Dispose();
            if (GodotObject.IsInstanceValid(stalePrepared) && !stalePrepared.IsInsideTree()) stalePrepared.Free();
            await f.DisposeAsync();
        }
    }
    private static RustShellSquire Mount(ResidentVoidStreamRuntimeTests.Fixture fixture, PrologueSector sector,
        ResidentPopulationDefinition slot)
    {
        var actor = Create(slot);
        try
        {
            actor.Position = slot.Placement.WorldSpawn - sector.GlobalPosition;
            fixture.MountActor(sector, actor); return actor;
        }
        catch { if (GodotObject.IsInstanceValid(actor) && !actor.IsInsideTree()) actor.Free(); throw; }
    }
    private static RustShellSquire Create(ResidentPopulationDefinition slot)
    {
        var actor = GD.Load<PackedScene>("res://scenes/enemies/RustShellSquire.tscn").Instantiate<RustShellSquire>();
        try { actor.ConfigureResidence(slot); actor.Position = slot.Placement.WorldSpawn; return actor; }
        catch { actor.Free(); throw; }
    }
    private static ResidentPopulationDefinition Definition(string id) => new(id, "rock-wanderer", CanyonRouteNodeId.LowCeilingBridge,
        PrologueSectorId.CanyonDescent, ResidentFamily.Native, ResidentArchetype.Wanderer,
        new(new(1500, 2960), new(1300, 2700, 500, 500), new[] { new Vector2(1450, 2960), new Vector2(1550, 2960) }));
    private static void CheckHealthPreparationGuards()
    {
        var fresh = new HealthComponent { MaxHealth = 3 }; var used = new HealthComponent { MaxHealth = 3 };
        try
        {
            int events = 0;
            fresh.Damaged += (_, _) => events++; fresh.Healed += (_, _) => events++; fresh.Died += () => events++;
            TestAssert.True(!fresh.TryPrepareResidentMountHealth(0, 3) && !fresh.TryPrepareResidentMountHealth(-1, 3) &&
                !fresh.TryPrepareResidentMountHealth(4, 3) && !fresh.TryPrepareResidentMountHealth(2, 4) &&
                fresh.TryPrepareResidentMountHealth(2, 3) && !fresh.TryPrepareResidentMountHealth(1, 3) && events == 0,
                "invalid HP/max leave fresh one-time preparation usable, with no synthetic events");
            used.ResetFull();
            TestAssert.True(!used.TryPrepareResidentMountHealth(2, 3) && used.CurrentHealth == 3,
                "used/reset component cannot be quietly rewritten even off-tree");
            GD.Print("RESIDENT_HEALTH_PREPARATION_GUARDS invalid=4 repeated_rejected=true used_rejected=true quiet=true native_ready_tested_separately=true");
        }
        finally { fresh.Free(); used.Free(); }
    }
}
