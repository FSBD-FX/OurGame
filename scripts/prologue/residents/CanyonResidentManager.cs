using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Combat;
using Knight.Enemies.Residents;
using Knight.Prologue.Seamless;
using Knight.Build.Prologue;

namespace Knight.Prologue.Residents;

// Run custody and transactional geometric admission, not placement certification.
// Unready canonical population is never automatically mounted.
public sealed partial class CanyonResidentManager : IDisposable
{
    private sealed record SectorBinding(PrologueSector Sector, Action Exit);
    private sealed record ActorBinding(CharacterBody2D Actor, HealthComponent Health,
        IEncounterActor Encounter, ResidentLease Lease, HealthComponent.DamagedEventHandler Damaged,
        HealthComponent.HealedEventHandler Healed, Action Defeated, Action Exit);
    private sealed record ResetBatch(long Epoch, ActorBinding[] Actors, Dictionary<string, long> Revisions);
    private readonly SeamlessPrologueWorld _world;
    private const string PreparationMeta = "_resident_health_preparation";
    private const string OwnerMeta = "_resident_manager_owner";
    private readonly string _ownerId = Guid.NewGuid().ToString("N");
    private readonly ResidentPopulationState _state;
    private readonly HashSet<string> _ids;
    private readonly IReadOnlyDictionary<string, ResidentPopulationDefinition> _definitions;
    private readonly IReadOnlyDictionary<string, ResidentSubtypeDefinition> _subtypes;
    private readonly ResidentOriginalSceneSources _residentSources;
    private readonly Dictionary<PrologueSectorId, SectorBinding> _sectors = new();
    private readonly Dictionary<string, ActorBinding> _actors = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, ResetBatch> _resetting = new();
    private bool _disposed;
    public int MountedSectorCount => _sectors.Count;
    public int TrackedActorCount => _actors.Count;
    public long Epoch => _state.Epoch;
    public CanyonResidentManager(SeamlessPrologueWorld world, IEnumerable<ResidentPopulationDefinition> definitions,
        IEnumerable<ResidentSubtypeDefinition> subtypes = null)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var slots = definitions.ToArray(); _state = new(slots);
        _ids = slots.Select(x => x.StableId).ToHashSet(StringComparer.Ordinal);
        _definitions = slots.ToDictionary(x => x.StableId, StringComparer.Ordinal);
        _subtypes = (subtypes ?? CanyonResidentCatalog.Subtypes).ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (!GodotObject.IsInstanceValid(world) || !world.IsInsideTree() || !world.IsNodeReady() || world.IsQueuedForDeletion())
            throw new ArgumentException("Require the live initialized streaming world.", nameof(world));
        if (world.HasMeta(OwnerMeta)) throw new InvalidOperationException("The streaming world already has a resident custody owner.");
        // Keep each actually used saved leaf for this manager's lifetime. Its
        // authored managed resources must outlive streamed actor instances.
        _residentSources = new(_subtypes.Values.Select(x => x.ScenePath).Where(x => !string.IsNullOrWhiteSpace(x)),
            path => ResourceLoader.Load<PackedScene>(path));
#if REFCOUNTED_FATAL_IDENTITY
        _residentSources.TraceOwner(_ownerId, "resident-leaf");
        _originalSources.TraceOwner(_ownerId, "original-probe");
#endif
        _world = world;
        _world.SetMeta(OwnerMeta, _ownerId);
        // Subscribe first, then enumerate: Level.Ready runs after initial mounts.
        _world.SectorMounted += OnMounted;
        _world.ResidentSectorResetting += OnSectorResetting;
        _world.ResidentSectorResetFinished += OnSectorResetFinished;
        _world.TreeExiting += Dispose;
        foreach (var id in _world.MountedSectorIds)
            if (_world.TryGetMountedSector(id, out var sector)) OnMounted(sector);
    }
    public ResidentSnapshot Snapshot(string id) => _state.Snapshot(id);
    public bool TryPrepareRestoredHealth(string stableId, CharacterBody2D freshActor)
    {
        if (_disposed || stableId == null || !_ids.Contains(stableId) || !GodotObject.IsInstanceValid(freshActor) ||
            freshActor.IsInsideTree() || freshActor.IsNodeReady() || freshActor.IsQueuedForDeletion() ||
            freshActor is not IResidentEnemyAgent || freshActor is not IEncounterActor) return false;
        var snapshot = _state.Snapshot(stableId);
        var health = freshActor.GetNodeOrNull<HealthComponent>("Health");
        if (freshActor.HasMeta(PreparationMeta) || snapshot is not { Alive: true, Attached: false, Health: > 0, MaxHealth: > 0 } ||
            !GodotObject.IsInstanceValid(health) || !health.TryPrepareResidentMountHealth(snapshot.Health.Value, snapshot.MaxHealth.Value)) return false;
        // Scalar provenance lives on the prepared actor, not in a manager-owned
        // cache of unmounted native nodes. Stale/foreign preparation cannot join
        // a later run, even when that run has not observed an HP value yet.
        freshActor.SetMeta(PreparationMeta, PreparationIdentity(stableId));
        return true;
    }
    public bool TryTrack(string stableId, CharacterBody2D actor, out ResidentLease lease)
        => TryTrackCore(stableId, actor, out lease, null);
    private bool TryTrackCore(string stableId, CharacterBody2D actor, out ResidentLease lease, SpawnClaim spawnClaim)
    {
        lease = default;
        if (_disposed || stableId == null || !_ids.Contains(stableId) || !Live(actor) ||
            actor is not IResidentEnemyAgent || actor is not IEncounterActor encounter) return false;
        if (_spawning.TryGetValue(stableId, out var pending) && !ReferenceEquals(pending, spawnClaim)) return false;
        foreach (var claim in _spawning)
            if (ReferenceEquals(claim.Value.Actor, actor) && (claim.Key != stableId || !ReferenceEquals(claim.Value, spawnClaim))) return false;
        if (spawnClaim != null && (!ReferenceEquals(pending, spawnClaim) || !ReferenceEquals(spawnClaim.Actor, actor))) return false;
        if (actor.HasMeta(PreparationMeta))
        {
            using Variant prepared = actor.GetMeta(PreparationMeta);
            if (prepared.VariantType != Variant.Type.String || prepared.AsString() != PreparationIdentity(stableId)) return false;
        }
        var slot = _state.Snapshot(stableId);
        if (!_sectors.TryGetValue(slot.Sector, out var mounted) || !Registered(mounted.Sector) ||
            !ReferenceEquals(actor.GetParent(), mounted.Sector.GetNodeOrNull("Gameplay/Actors"))) return false;
        if (_actors.TryGetValue(stableId, out var existing))
        {
            if (!ReferenceEquals(existing.Actor, actor) || !Authorized(existing)) return false;
            lease = existing.Lease; return true;
        }
        var health = actor.GetNodeOrNull<HealthComponent>("Health");
        if (!Live(health) || !_state.TryAttach(stableId, slot.Sector, mounted.Sector.GetInstanceId(),
            actor.GetInstanceId(), health.CurrentHealth, health.MaxHealth, out lease)) return false;
        ResidentLease admitted = lease;
        HealthComponent.DamagedEventHandler damaged = (remaining, _) => Observe(admitted, remaining);
        HealthComponent.HealedEventHandler healed = (_, remaining) => Observe(admitted, remaining);
        Action defeated = () =>
        {
            if (_actors.TryGetValue(admitted.StableId, out var current) && current.Lease == admitted &&
                Authorized(current) && current.Health.CurrentHealth == 0) _state.MarkDefeated(admitted);
        };
        Action exit = () => Release(admitted, captureHealth: true);
        var binding = new ActorBinding(actor, health, encounter, admitted, damaged, healed, defeated, exit);
        _actors.Add(stableId, binding);
        health.Damaged += damaged; health.Healed += healed;
        encounter.Defeated += defeated; actor.TreeExiting += exit;
        if (actor.HasMeta(PreparationMeta)) actor.RemoveMeta(PreparationMeta);
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Lease("lease-commit", _ownerId, admitted);
#endif
        return true;
    }
    private string PreparationIdentity(string stableId) => $"{_ownerId}/{_state.Epoch}/{_state.Snapshot(stableId).ResetRevision}/{stableId}";
    // Must be called at the actual new-run boundary. This does NOT reset actors
    // or restore HP: caller must complete the real reset before re-admission.
    public void BeginRun()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CanyonResidentManager));
        foreach (var binding in _actors.Values.ToArray()) Release(binding.Lease, captureHealth: false);
        _resetting.Clear();
        _state.BeginRun();
    }
    public void BeginRunAndResetMountedResidents()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(CanyonResidentManager));
        var actors = _actors.Values.Where(Authorized).ToArray();
        BeginRun(); long epoch = _state.Epoch;
        foreach (var binding in actors)
        {
            if (_disposed || _state.Epoch != epoch) return;
            var slot = _state.Snapshot(binding.Lease.StableId);
            if (!Live(binding.Actor) || !_sectors.TryGetValue(slot.Sector, out var sector) ||
                sector.Sector.GetInstanceId() != binding.Lease.SectorInstance || !Registered(sector.Sector) ||
                !ReferenceEquals(binding.Actor.GetParent(), sector.Sector.GetNodeOrNull("Gameplay/Actors"))) continue;
            binding.Encounter.ResetForCheckpoint();
            if (_disposed || _state.Epoch != epoch) return;
            if (Live(binding.Actor) && Registered(sector.Sector) && !TryTrack(slot.StableId, binding.Actor, out _))
                throw new InvalidOperationException("Reset resident failed fresh run admission: " + slot.StableId);
        }
    }
    private void Observe(ResidentLease lease, int health)
    {
        if (_actors.TryGetValue(lease.StableId, out var binding) && binding.Lease == lease && Authorized(binding))
            _state.ObserveHealth(lease, health);
    }
    private void OnSectorResetting(PrologueSector sector)
    {
        if (_disposed || !Registered(sector)) return;
        ulong instance = sector.GetInstanceId();
        var actors = _actors.Values.Where(x => x.Lease.SectorInstance == instance && Authorized(x)).ToArray();
        foreach (var actor in actors) Release(actor.Lease, captureHealth: false);
        _state.ResetSector(sector.SectorId);
        _resetting[instance] = new(_state.Epoch, actors, actors.ToDictionary(x => x.Lease.StableId,
            x => _state.Snapshot(x.Lease.StableId).ResetRevision, StringComparer.Ordinal));
    }
    private void OnSectorResetFinished(PrologueSector sector, bool succeeded)
    {
        ulong instance = sector.GetInstanceId();
        if (!_resetting.Remove(instance, out var batch) || !succeeded || _disposed || batch.Epoch != _state.Epoch || !Registered(sector)) return;
        foreach (var actor in batch.Actors)
        {
            if (_disposed || batch.Epoch != _state.Epoch) return;
            if (_state.Snapshot(actor.Lease.StableId).ResetRevision != batch.Revisions[actor.Lease.StableId] ||
                !Live(actor.Actor) || !Registered(sector) || !ReferenceEquals(actor.Actor.GetParent(), sector.GetNodeOrNull("Gameplay/Actors"))) continue;
            // World already called the actual actor reset. Never call it again.
            if (!TryTrack(actor.Lease.StableId, actor.Actor, out _))
                throw new InvalidOperationException("Reset resident failed checkpoint admission: " + actor.Lease.StableId);
        }
    }
    private bool Authorized(ActorBinding binding) => !_disposed && _state.Owns(binding.Lease) &&
        Live(binding.Actor) && Live(binding.Health) &&
        _sectors.TryGetValue(_state.Snapshot(binding.Lease.StableId).Sector, out var sector) &&
        Registered(sector.Sector) && sector.Sector.GetInstanceId() == binding.Lease.SectorInstance &&
        binding.Actor.GetInstanceId() == binding.Lease.ActorInstance &&
        ReferenceEquals(binding.Actor.GetParent(), sector.Sector.GetNodeOrNull("Gameplay/Actors"));
    private bool Registered(PrologueSector sector) => Live(sector) &&
        !sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta) &&
        GodotObject.IsInstanceValid(_world) && _world.IsInsideTree() &&
        ReferenceEquals(sector.GetParent(), _world) && _world.TryGetMountedSector(sector.SectorId, out var actual) &&
        ReferenceEquals(actual, sector);
    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() &&
        node.IsNodeReady() && !node.IsQueuedForDeletion();
    private void OnMounted(PrologueSector sector)
    {
        if (_disposed || !Registered(sector)) return;
        if (_sectors.TryGetValue(sector.SectorId, out var existing))
        {
            if (ReferenceEquals(existing.Sector, sector)) return;
            RemoveSector(existing.Sector);
        }
        Action exit = () => RemoveSector(sector);
        _sectors.Add(sector.SectorId, new(sector, exit)); sector.TreeExiting += exit;
    }
    private void RemoveSector(PrologueSector sector)
    {
        if (!_sectors.TryGetValue(sector.SectorId, out var current) || !ReferenceEquals(current.Sector, sector)) return;
        ulong sectorId = sector.GetInstanceId();
        foreach (var binding in _actors.Values.Where(x => x.Lease.SectorInstance == sectorId).ToArray())
            Release(binding.Lease, captureHealth: true);
        _sectors.Remove(sector.SectorId);
        if (GodotObject.IsInstanceValid(sector)) sector.TreeExiting -= current.Exit;
    }
    private void Release(ResidentLease lease, bool captureHealth)
    {
        if (!_actors.TryGetValue(lease.StableId, out var binding) || binding.Lease != lease) return;
        // TreeExiting revokes authority even though registry removal may precede
        // this callback. Scalar HP capture is not authorization to attack/reset.
        if (captureHealth && GodotObject.IsInstanceValid(binding.Health))
            _state.ObserveHealth(lease, binding.Health.CurrentHealth);
        _actors.Remove(lease.StableId); _state.Detach(lease);
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Lease("lease-release", _ownerId, lease);
#endif
        if (GodotObject.IsInstanceValid(binding.Health))
        { binding.Health.Damaged -= binding.Damaged; binding.Health.Healed -= binding.Healed; }
        if (GodotObject.IsInstanceValid(binding.Actor))
        { binding.Encounter.Defeated -= binding.Defeated; binding.Actor.TreeExiting -= binding.Exit; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Source(0, "manager-dispose-before-cache-clear", 0, "", _ownerId, "manager");
#endif
        _residentSources.Dispose(); // Drop references only, never Dispose shared saved resources.
        _originalSources.Dispose(); // Release references, never native borrowed resources.
#if REFCOUNTED_FATAL_IDENTITY
        Knight.Tests.Diagnostics.ResidentProfileLifetimeTrace.Source(0, "manager-dispose-after-cache-clear", 0, "", _ownerId, "manager");
#endif
        _resetting.Clear();
        foreach (var binding in _actors.Values.ToArray()) Release(binding.Lease, captureHealth: true);
        foreach (var sector in _sectors.Values.ToArray()) RemoveSector(sector.Sector);
        if (GodotObject.IsInstanceValid(_world))
        {
            _world.SectorMounted -= OnMounted; _world.TreeExiting -= Dispose;
            _world.ResidentSectorResetting -= OnSectorResetting;
            _world.ResidentSectorResetFinished -= OnSectorResetFinished;
            if (_world.HasMeta(OwnerMeta))
            {
                using Variant owner = _world.GetMeta(OwnerMeta);
                if (owner.VariantType == Variant.Type.String && owner.AsString() == _ownerId) _world.RemoveMeta(OwnerMeta);
            }
        }
    }
}
