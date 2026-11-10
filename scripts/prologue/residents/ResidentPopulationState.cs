using System;
using System.Collections.Generic;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Residents;

public readonly record struct ResidentLease(string StableId, long Epoch,
    ulong SectorInstance, ulong ActorInstance, long Generation);
public sealed record ResidentSnapshot(string StableId, PrologueSectorId Sector,
    long Epoch, bool Alive, int? Health, int? MaxHealth, bool Attached, long ResetRevision = 0);

// Run-lifetime scalar custody, not a scene/placement certificate or save schema.
public sealed class ResidentPopulationState
{
    private sealed class Entry(ResidentPopulationDefinition definition)
    {
        public readonly ResidentPopulationDefinition Definition = definition;
        public bool Alive = true;
        public int? Health, MaxHealth;
        public ResidentLease? Lease;
        public long Generation;
        public long ResetRevision;
    }
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    public long Epoch { get; private set; } = 1;
    public int Count => _entries.Count;
    public ResidentPopulationState(IEnumerable<ResidentPopulationDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        foreach (var definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.StableId) || !Enum.IsDefined(definition.Sector))
                throw new ArgumentException("Require a stable identity and defined sector.", nameof(definitions));
            _entries.Add(definition.StableId, new Entry(definition));
        }
    }
    public ResidentSnapshot Snapshot(string id)
    {
        var entry = _entries[id];
        return new(id, entry.Definition.Sector, Epoch, entry.Alive, entry.Health, entry.MaxHealth, entry.Lease.HasValue, entry.ResetRevision);
    }
    public bool TryAttach(string id, PrologueSectorId sector, ulong sectorInstance,
        ulong actorInstance, int health, int maxHealth, out ResidentLease lease)
    {
        lease = default;
        if (id == null || !_entries.TryGetValue(id, out var entry) || entry.Definition.Sector != sector ||
            sectorInstance == 0 || actorInstance == 0 || maxHealth <= 0 || health <= 0 || health > maxHealth ||
            !entry.Alive || entry.Lease.HasValue || entry.Health.HasValue &&
                (entry.Health != health || entry.MaxHealth != maxHealth)) return false;
        foreach (var other in _entries.Values)
            if (other.Lease is { } owned && owned.ActorInstance == actorInstance) return false;
        long generation = checked(entry.Generation + 1);
        lease = new(id, Epoch, sectorInstance, actorInstance, generation);
        entry.Generation = generation;
        entry.Health = health; entry.MaxHealth = maxHealth; entry.Lease = lease;
        return true;
    }
    public bool ObserveHealth(ResidentLease lease, int health)
    {
        if (!Owns(lease)) return false;
        var entry = _entries[lease.StableId];
        if (health < 0 || health > entry.MaxHealth || !entry.Alive && health != 0) return false;
        entry.Health = health;
        if (health == 0) entry.Alive = false;
        return true;
    }
    public bool MarkDefeated(ResidentLease lease) => ObserveHealth(lease, 0);
    public bool Detach(ResidentLease lease)
    {
        if (!Owns(lease)) return false;
        _entries[lease.StableId].Lease = null;
        return true;
    }
    public bool Owns(ResidentLease lease) => lease.Epoch == Epoch && lease.StableId != null &&
        _entries.TryGetValue(lease.StableId, out var entry) && entry.Lease == lease;
    public void ResetSector(PrologueSectorId sector)
    {
        if (!Enum.IsDefined(sector)) throw new ArgumentOutOfRangeException(nameof(sector));
        foreach (var entry in _entries.Values)
            if (entry.Definition.Sector == sector)
            {
                entry.ResetRevision = checked(entry.ResetRevision + 1);
                entry.Alive = true; entry.Health = entry.MaxHealth = null; entry.Lease = null;
                // Keep generation monotonic within the run, even if native IDs
                // survive the reset. Epoch alone cannot reject an old callback.
            }
    }
    public void BeginRun()
    {
        long next = checked(Epoch + 1);
        foreach (var entry in _entries.Values)
        { entry.Alive = true; entry.Health = entry.MaxHealth = null; entry.Lease = null; entry.Generation = 0; entry.ResetRevision = 0; }
        Epoch = next;
    }
}
