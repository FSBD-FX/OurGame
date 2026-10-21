using System;
using Godot;
using Knight.Presentation.Fog;
using Knight.Save;

namespace Knight.Presentation.Minimap;

public readonly record struct MinimapObservation(Vector2 ActorWorld, ulong PhysicsFrame,
    ulong WorldGeneration, bool CanReveal, HeadlampOcclusionFrame Occlusion, ReadOnlyMemory<Rect2> LoadedBounds);

public sealed class ExplorationMapModel
{
    public const int MaximumLoadedBounds = 64;
    public const int MaximumCandidatesPerStamp = 64;
    private readonly MinimapTerrainCatalog _catalog;
    private readonly byte[] _cells, _boundaries;
    private readonly MinimapCandidateScheduler _scheduler;
    private bool _hasStamp;
    private ulong _lastStamp;
    public ExplorationMapModel(MinimapTerrainCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        if (catalog.CellCount <= 0 || catalog.CellCount > MinimapTerrainCatalog.MaximumBits ||
            catalog.BoundaryCount > MinimapTerrainCatalog.MaximumBits) throw new ArgumentException("Unconfigured catalog.");
        _cells = new byte[(catalog.CellCount + 7) / 8]; _boundaries = new byte[(catalog.BoundaryCount + 7) / 8];
        _scheduler = new MinimapCandidateScheduler(catalog);
    }
    public ulong RevisionCounter { get; private set; }
    public MinimapTerrainCatalog Catalog => _catalog;
    public int LastCandidateCount { get; private set; }
    public int LastChargedOccluderTests { get; private set; }
    public int LastFiniteOccluderSweepTests { get; private set; }
    public int CandidateCycleLength => _scheduler.CycleCandidateUpperBound;
    public int LastSchedulerNodeExpansions { get; private set; }
    public int LastSchedulerQueryRebuilds { get; private set; }
    public int BitsetBytes => _cells.Length + _boundaries.Length;
    public int DirectoryBytes => _scheduler.DirectoryBytes;
    public int CandidateCacheBytes => _scheduler.CandidateCacheBytes;
    public void Advance(in MinimapObservation observation)
    {
        LastCandidateCount = LastChargedOccluderTests = LastFiniteOccluderSweepTests = 0;
        LastSchedulerNodeExpansions = LastSchedulerQueryRebuilds = 0;
        if (_hasStamp && observation.PhysicsFrame <= _lastStamp) return;
        _hasStamp = true; _lastStamp = observation.PhysicsFrame;
        if (!observation.CanReveal || !MinimapTerrainCatalog.Finite(observation.ActorWorld) ||
            observation.Occlusion == null || !observation.Occlusion.IsComplete ||
            observation.Occlusion.Shapes.Count > 256 || observation.LoadedBounds.Length is < 1 or > MaximumLoadedBounds) return;
        foreach (Rect2 bounds in observation.LoadedBounds.Span) if (!MinimapTerrainCatalog.Valid(bounds)) return;
        // Validate the complete frame before any commit. A malformed late row
        // cannot authorize earlier candidates just because an early ray rejected.
        for (int i = 0; i < observation.Occlusion.Shapes.Count; i++)
        {
            HeadlampOccluder shape = observation.Occlusion.Shapes[i];
            if (!MinimapTerrainCatalog.Valid(shape.LocalBounds) || !MinimapTerrainCatalog.Valid(shape.WorldToLocal)) return;
        }
        double gridX = Math.Floor(((double)observation.ActorWorld.X - _catalog.WorldBounds.Position.X) / 8);
        double gridY = Math.Floor(((double)observation.ActorWorld.Y - _catalog.WorldBounds.Position.Y) / 8);
        if (gridX < -21 || gridY < -21 || gridX > _catalog.Columns + 20 || gridY > _catalog.Rows + 20) return;
        _scheduler.BeginStamp(observation.ActorWorld);
        for (int work = 0; work < MaximumCandidatesPerStamp; work++)
        {
            if (!_scheduler.TryNext(out int id, out bool boundaryCandidate)) break;
            LastCandidateCount++;
            byte[] bits = boundaryCandidate ? _boundaries : _cells;
            if (Get(bits, id)) continue;
            bool allowed; int actual;
            if (boundaryCandidate)
            {
                MinimapBoundary boundary = _catalog.Boundary(id); MinimapSolid solid = _catalog.Solid(boundary.ShapeId);
                Vector2 a = solid.LocalToWorld * boundary.FromLocal, b = solid.LocalToWorld * boundary.ToLocal;
                if (!Within(observation.ActorWorld, a) || !Within(observation.ActorWorld, b) ||
                    !MinimapLoadedCoverage.Boundary(observation.ActorWorld, a, b, observation.LoadedBounds.Span)) continue;
                LastChargedOccluderTests += observation.Occlusion.Shapes.Count;
                allowed = MinimapVisibility.BoundaryAllowed(observation.ActorWorld, solid, boundary, observation.Occlusion, out actual);
            }
            else
            {
                Rect2 rect = _catalog.Cell(id);
                if (!Within(observation.ActorWorld, rect.Position) || !Within(observation.ActorWorld, rect.End) ||
                    !Within(observation.ActorWorld, new Vector2(rect.End.X, rect.Position.Y)) ||
                    !Within(observation.ActorWorld, new Vector2(rect.Position.X, rect.End.Y)) ||
                    !MinimapLoadedCoverage.Cell(observation.ActorWorld, rect, observation.LoadedBounds.Span)) continue;
                LastChargedOccluderTests += observation.Occlusion.Shapes.Count;
                allowed = MinimapVisibility.CellAllowed(observation.ActorWorld, rect, observation.Occlusion, out actual);
            }
            LastFiniteOccluderSweepTests += actual;
            if (allowed) { bits[id >> 3] |= (byte)(1 << (id & 7)); RevisionCounter++; }
        }
        LastSchedulerNodeExpansions = _scheduler.LastNodeExpansions;
        LastSchedulerQueryRebuilds = _scheduler.LastQueryRebuilds;
    }
    private static bool Within(Vector2 actor, Vector2 point)
    { double x = (double)point.X - actor.X, y = (double)point.Y - actor.Y; return x * x + y * y <= 25600d; }
    private static bool Get(byte[] bits, int id) => id >= 0 && id < bits.Length * 8 && (bits[id >> 3] & (1 << (id & 7))) != 0;
    public bool IsCellExplored(int id) => (uint)id < (uint)_catalog.CellCount && Get(_cells, id);
    public bool IsBoundaryExplored(int id) => (uint)id < (uint)_catalog.BoundaryCount && Get(_boundaries, id);
    public void Clear()
    {
        Array.Clear(_cells); Array.Clear(_boundaries); RevisionCounter++;
        _scheduler.Reset(); _hasStamp = false;
        LastCandidateCount = LastChargedOccluderTests = LastFiniteOccluderSweepTests = 0;
        LastSchedulerNodeExpansions = LastSchedulerQueryRebuilds = 0;
    }
    public ExplorationMapSaveData CaptureSave() => new()
    { TerrainRevision = _catalog.Revision, FreeCellsBase64 = Convert.ToBase64String(_cells), BoundarySegmentsBase64 = Convert.ToBase64String(_boundaries) };
    public void Restore(ExplorationMapSaveData data)
    {
        Clear();
        if (data == null || data.FormatVersion != 1 || data.TerrainRevision != _catalog.Revision ||
            !Decode(data.FreeCellsBase64, _cells, _catalog.CellCount) ||
            !Decode(data.BoundarySegmentsBase64, _boundaries, _catalog.BoundaryCount))
        { Array.Clear(_cells); Array.Clear(_boundaries); }
    }
    private static bool Decode(string text, byte[] destination, int count)
    {
        // Exact canonical size bounds parsing work; Try API never throws for malformed input.
        if (text == null || text.Length != ((destination.Length + 2) / 3) * 4 ||
            !Convert.TryFromBase64String(text, destination, out int written) || written != destination.Length) return false;
        return count % 8 == 0 || (destination[^1] & ~((1 << (count % 8)) - 1)) == 0;
    }
}
