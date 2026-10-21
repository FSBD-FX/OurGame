using System;
using Godot;
using Knight.Presentation.Fog;
using Knight.Presentation.Minimap;
using Knight.Save;

namespace Knight.Tests.Unit;

internal static class ExplorationMapModelTests
{
    private static readonly Rect2[] Loaded = { new(-400, -400, 1600, 800) };
    private static readonly HeadlampOcclusionFrame Open = new(Array.Empty<HeadlampOccluder>(), true);
    private static MinimapTerrainCatalog Catalog() => new(new Rect2(-200, -200, 1200, 400), "model-v1");
    private static int Id(int x, int y) => (y + 200) / 8 * 150 + (x + 200) / 8;
    private static MinimapObservation Observation(ulong stamp, Vector2 actor = default, bool alive = true,
        HeadlampOcclusionFrame frame = null, Rect2[] loaded = null, ulong generation = 1) =>
        new(actor, stamp, generation, alive, frame ?? Open, loaded ?? Loaded);
    private static void Drain(ExplorationMapModel model, Vector2 actor = default,
        HeadlampOcclusionFrame frame = null, Rect2[] loaded = null, ulong first = 1, int count = 64)
    { for (ulong i = first; i < first + (ulong)count; i++) model.Advance(Observation(i, actor, frame: frame, loaded: loaded)); }

    internal static void Run(string firstRegression = "")
    {
        if (firstRegression == "DenseBoundaryCannotStarveNearCell") DenseBoundaryCannotStarveNearCell();
        if (firstRegression == "SameAnchorMovementReordersNearCandidates") SameAnchorMovementReordersNearCandidates();
        OnlyActualLivingPositionReveals(); WholePatchWithin160(); KnownSegmentNotWholeWall();
        TeleportDoesNotPaintLine(); UnloadedSweepRejected(); StaleQueuedPositionRechecked();
        RepeatPhysicsStampDoesNoWork(); CapacityNeverWraps(); StationaryDoorOpenRetriesRejectedCells();
        InvalidAuthorizationFailsClosed(); MemoryRoundtripClearAndMalformed(); ChecksAtMost16384();
        WarmedIdleAllocatesZero(); Warmed64CandidatesAtMost4096Bytes();
        DenseBoundaryCannotStarveNearCell(); SameAnchorMovementReordersNearCandidates();
        UnresolvedSubtreesRespectPreparationBudget();
        GD.Print("MINIMAP_MODEL_OK cases=17 save_io=0");
    }
    private static MinimapTerrainCatalog DenseCatalog(int count)
    {
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(-8, -8, 1, 8));
        var boundaries = new MinimapBoundary[count];
        Array.Fill(boundaries, new MinimapBoundary(0, new(-7, -8), new(-7, 0), Vector2.Right));
        return new MinimapTerrainCatalog(new Rect2(-200, -200, 400, 400), "dense-priority", new[] { solid }, boundaries);
    }
    private static HeadlampOcclusionFrame DenseFrame() => new(new[] {
        new HeadlampOccluder(Transform2D.Identity, new Rect2(-8, -8, 1, 8)) }, true);
    private static void DenseBoundaryCannotStarveNearCell()
    {
        using var catalog = DenseCatalog(1048576);
        long preparationBefore = GC.GetAllocatedBytesForCurrentThread();
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        var model = new ExplorationMapModel(catalog);
        double preparationMs = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        long preparationBytes = GC.GetAllocatedBytesForCurrentThread() - preparationBefore;
        var frame = DenseFrame(); model.Advance(Observation(1, frame: frame));
        // Hand-derived center distances squared: clear cell (4,4)=32,
        // every boundary midpoint (-7,-4)=65. A bucket-center tie cannot override this.
        TestAssert.True(model.IsCellExplored(25 * 50 + 25),
            "DenseBoundaryCannotStarveNearCell: distance-squared 32 cell precedes all 1048576 distance-squared 65 boundaries on first stamp");
        GD.Print($"MINIMAP_DENSE_PRIORITY boundaries=1048576 near_cell_delay_stamps=1 candidates={model.LastCandidateCount} expansions={model.LastSchedulerNodeExpansions} directory_bytes={model.DirectoryBytes} candidate_cache_bytes={model.CandidateCacheBytes} preparation_bytes={preparationBytes} preparation_ms={preparationMs:F3}");
        for (ulong i = 2; i <= 128; i++) model.Advance(Observation(i, frame: frame));
        int maximumExpansions = 0, fullBatches = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (ulong i = 129; i <= 640; i++)
        {
            model.Advance(Observation(i, frame: frame));
            if (model.LastCandidateCount == 64) fullBatches++;
            maximumExpansions = Math.Max(maximumExpansions, model.LastSchedulerNodeExpansions);
            TestAssert.True(model.LastSchedulerNodeExpansions <= 256 && model.LastSchedulerQueryRebuilds <= 2 &&
                model.LastCandidateCount <= 64 && model.LastChargedOccluderTests <= 16384,
                "dense actual Advance stays within independent preparation/candidate/sweep budgets");
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        TestAssert.True(fullBatches == 512 && bytes == 0, "dense warmed individual-priority scheduler allocates zero for 512 actual full batches");
        GD.Print($"MINIMAP_DENSE_ALLOC samples=512 candidates={fullBatches * 64} bytes={bytes} max_expansions={maximumExpansions}");
    }
    private static void SameAnchorMovementReordersNearCandidates()
    {
        using var catalog = DenseCatalog(256); var model = new ExplorationMapModel(catalog); var frame = DenseFrame();
        model.Advance(Observation(1, frame: frame));
        int target = 26 * 50 + 25; // Cell (0,8), center (4,12).
        TestAssert.True(!model.IsCellExplored(target), "fixture: original squared distance160 follows dense midpoint squared distance65");
        model.Advance(Observation(2, new Vector2(3.9f, 3.9f), frame: frame));
        // Same 8-unit anchor. Target distance squared65.62; boundary181.22.
        TestAssert.True(model.IsCellExplored(target),
            "SameAnchorMovementReordersNearCandidates: current distance-squared 65.62 cell precedes pending distance-squared 181.22 boundary records within same anchor");
        GD.Print($"MINIMAP_SAME_ANCHOR_PRIORITY target_delay_after_move_stamps=1 query_rebuilds={model.LastSchedulerQueryRebuilds} expansions={model.LastSchedulerNodeExpansions}");
    }
    private static void UnresolvedSubtreesRespectPreparationBudget()
    {
        // Every paired midpoint is ~100 units away, but each pair's box
        // straddles y=0 with |x|<=1. Its lower bound (<32) must be resolved
        // before committing the near cell at center (4,4), squared distance32.
        var solids = new MinimapSolid[4096]; var boundaries = new MinimapBoundary[4096];
        for (int i = 0; i < 4096; i++)
        {
            float x = -1 + (i / 2) / 1024f, y = i % 2 == 0 ? -100 : 100;
            solids[i] = new MinimapSolid(Transform2D.Identity, new Rect2(x - .25f, y - .25f, .25f, .5f));
            boundaries[i] = new MinimapBoundary(i, new(x, y - .25f), new(x, y + .25f), Vector2.Right);
        }
        using var catalog = new MinimapTerrainCatalog(new Rect2(-200, -200, 400, 400), "unresolved-boxes", solids, boundaries);
        var model = new ExplorationMapModel(catalog); model.Advance(Observation(1));
        TestAssert.True(model.LastSchedulerNodeExpansions == 256 && model.LastCandidateCount == 0,
            "unresolved strictly nearer lower bounds stop at preparation cap rather than scan all4096 nodes or commit out of order");
        int delay = 1;
        while (!model.IsCellExplored(25 * 50 + 25) && delay < 32)
        {
            model.Advance(Observation((ulong)++delay));
            TestAssert.True(model.LastSchedulerNodeExpansions <= 256 && model.LastCandidateCount <= 64,
                "pending conservative frontier resumes with fresh bounded preparation budget");
        }
        TestAssert.True(model.IsCellExplored(25 * 50 + 25), "bounded preparation eventually resolves conservative boxes and serves near cell");
        GD.Print($"MINIMAP_PREPARATION_CAP boundaries=4096 first_stamp_expansions=256 first_stamp_candidates=0 near_cell_delay_stamps={delay}");
    }
    private static void OnlyActualLivingPositionReveals()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        model.Advance(Observation(1));
        TestAssert.True(model.IsCellExplored(Id(8, 0)),
            "OnlyActualLivingPositionReveals: legal open nearby whole cell reveals after Advance");
        model.Clear(); model.Advance(Observation(2, alive: false));
        TestAssert.True(!model.IsCellExplored(Id(8, 0)), "dead/non-playing actor cannot reveal");
        Drain(model, new Vector2(800, 0), first: 3);
        TestAssert.True(model.IsCellExplored(Id(808, 0)) && !model.IsCellExplored(Id(8, 0)),
            "only actual living actor position, never a camera/facing proxy");
    }
    private static void WholePatchWithin160()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog); Drain(model);
        TestAssert.True(model.IsCellExplored(Id(144, 0)), "whole patch inside 160 is legal");
        TestAssert.True(!model.IsCellExplored(Id(152, 0)), "WholePatchWithin160: far corner outside radius rejects patch");
        TestAssert.True(!model.IsCellExplored(Id(160, 0)), "center/edge alone cannot authorize patch");
    }
    private static void KnownSegmentNotWholeWall()
    {
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(16, -8, 8, 24));
        using var catalog = new MinimapTerrainCatalog(new Rect2(-200, -200, 400, 400), "wall",
            new[] { solid }, new[] { new MinimapBoundary(0, new(16, -8), new(16, 0), Vector2.Left),
                new MinimapBoundary(0, new(16, 0), new(16, 8), Vector2.Left) });
        var frame = new HeadlampOcclusionFrame(new[] { new HeadlampOccluder(Transform2D.Identity, solid.LocalBounds),
            new HeadlampOccluder(Transform2D.Identity, new Rect2(8, 2, 2, 4)) }, true);
        var model = new ExplorationMapModel(catalog); Drain(model, frame: frame);
        TestAssert.True(model.IsBoundaryExplored(0) && !model.IsBoundaryExplored(1),
            "KnownSegmentNotWholeWall: one authorized segment does not mark its neighboring wall segment");
        TestAssert.True(!model.IsCellExplored(25 * 50 + 27), "boundary memory never unblocks occupied cell");
        ExplorationMapSaveData save = model.CaptureSave(); model.Clear(); model.Restore(save);
        TestAssert.True(model.IsBoundaryExplored(0) && !model.IsBoundaryExplored(1), "boundary bitset independently roundtrips in pure memory");
        model.Clear();
        Drain(model, frame: frame, loaded: new[] { new Rect2(-8, -16, 12, 32), new Rect2(12, -16, 20, 32) });
        TestAssert.True(!model.IsBoundaryExplored(0), "whole actor-to-boundary wedge rejects unloaded gap despite loaded endpoints");
        Drain(model, frame: frame, first: 65, loaded: new[] { new Rect2(-8, -16, 16, 32), new Rect2(8, -16, 24, 32) });
        TestAssert.True(model.IsBoundaryExplored(0), "adjacent loaded union authorizes entire boundary wedge");
    }
    private static void TeleportDoesNotPaintLine()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        Drain(model); Drain(model, new Vector2(800, 0), first: 65);
        TestAssert.True(model.IsCellExplored(Id(8, 0)) && model.IsCellExplored(Id(808, 0)), "teleport preserves both local memories");
        TestAssert.True(!model.IsCellExplored(Id(400, 0)), "TeleportDoesNotPaintLine: no interpolated corridor");
    }
    private static void UnloadedSweepRejected()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        Rect2[] gap = { new(-8, -16, 16, 32), new(24, -16, 24, 32) };
        Drain(model, loaded: gap);
        TestAssert.True(!model.IsCellExplored(Id(24, 0)), "UnloadedSweepRejected: loaded endpoints cannot cover an unloaded interior gap");
        Rect2[] adjacent = { new(-8, -16, 24, 32), new(16, -16, 32, 32) };
        Drain(model, loaded: adjacent, first: 65);
        TestAssert.True(model.IsCellExplored(Id(24, 0)), "adjacent union covers sweep even though neither rectangle alone does");
        model.Clear(); Rect2[] notch = { new(-8, -16, 24, 32), new(16, -16, 32, 18), new(16, 6, 32, 10) };
        Drain(model, loaded: notch, first: 129);
        TestAssert.True(!model.IsCellExplored(Id(24, 0)), "loaded corners/actor do not prove whole patch interior coverage");
    }
    private static void StaleQueuedPositionRechecked()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog); model.Advance(Observation(1));
        Drain(model, new Vector2(800, 0), first: 2);
        TestAssert.True(!model.IsCellExplored(Id(144, 0)), "StaleQueuedPositionRechecked: old near queue cannot commit from new actor");
        TestAssert.True(model.IsCellExplored(Id(808, 0)), "new actual actor is eventually served");
        model.Clear(); model.Advance(Observation(66, new Vector2(800, 0)));
        Drain(model, new Vector2(800, 0), first: 67, loaded: Array.Empty<Rect2>());
        TestAssert.True(!model.IsCellExplored(Id(944, 0)), "retired coverage cannot authorize queued cells");
    }
    private static void RepeatPhysicsStampDoesNoWork()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog); model.Advance(Observation(10));
        ulong revision = model.RevisionCounter;
        model.Advance(Observation(10, new Vector2(800, 0))); model.Advance(Observation(9, new Vector2(800, 0)));
        TestAssert.True(model.RevisionCounter == revision && model.LastCandidateCount == 0,
            "RepeatPhysicsStampDoesNoWork: duplicate/older physics stamp never obtains a second budget");
        TestAssert.True(!model.IsCellExplored(Id(808, 0)), "duplicate stamp cannot reveal at alternate actor");
    }
    private static void CapacityNeverWraps()
    {
        using var catalog = new MinimapTerrainCatalog(new Rect2(0, 0, 8192, 8192), "max");
        var model = new ExplorationMapModel(catalog); Drain(model, new Vector2(8180, 8180), loaded: new[] { new Rect2(0, 0, 8192, 8192) });
        TestAssert.True(model.IsCellExplored(1048575) && !model.IsCellExplored(0), "CapacityNeverWraps: maximum cell id is independent of zero");
        TestAssert.True(model.BitsetBytes == 131072, "maximum cell bitset uses exactly 128KiB, empty boundary set uses zero");
        bool rejected = false; try { using var invalid = new MinimapTerrainCatalog(new Rect2(0, 0, 8200, 8192), "too-big"); }
        catch (ArgumentException) { rejected = true; } TestAssert.True(rejected, "over-capacity grid must fail closed");
        rejected = false; try { catalog.Cell(-1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        TestAssert.True(rejected && !model.IsCellExplored(-1) && !model.IsCellExplored(1048576), "invalid IDs never wrap");
        var solid = new MinimapSolid(Transform2D.Identity, new Rect2(16, 0, 8, 8));
        var boundaries = new MinimapBoundary[1048576];
        Array.Fill(boundaries, new MinimapBoundary(0, new(16, 0), new(16, 8), Vector2.Left));
        using var max = new MinimapTerrainCatalog(new Rect2(0, 0, 8192, 8192), "both-max", new[] { solid }, boundaries);
        var maxModel = new ExplorationMapModel(max);
        TestAssert.True(maxModel.BitsetBytes == 262144 && !maxModel.IsBoundaryExplored(1048576), "both bitsets independently cap at 128KiB");
        rejected = false; try { using var invalid = new MinimapTerrainCatalog(new Rect2(0, 0, 8, 8), "too-many", default, new MinimapBoundary[1048577]); }
        catch (ArgumentException) { rejected = true; } TestAssert.True(rejected, "over-capacity boundary catalog rejects, never truncates");
    }
    private static void StationaryDoorOpenRetriesRejectedCells()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        var closed = new HeadlampOcclusionFrame(new[] { new HeadlampOccluder(Transform2D.Identity, new Rect2(12, -20, 2, 40)) }, true);
        Drain(model, frame: closed); TestAssert.True(!model.IsCellExplored(Id(24, 0)), "closed door blocks cell");
        int delay = 0;
        for (ulong stamp = 65; stamp < 129 && !model.IsCellExplored(Id(24, 0)); stamp++)
        { model.Advance(Observation(stamp)); delay++; }
        TestAssert.True(model.IsCellExplored(Id(24, 0)) && delay <= 30, "StationaryDoorOpenRetriesRejectedCells: stationary same-generation actor retries current open frame");
        GD.Print($"MINIMAP_RETRY_SAMPLE delay_stamps={delay} candidates_per_stamp=64 cycle_candidates={model.CandidateCycleLength}");
        model.Advance(Observation(129, generation: 2));
        TestAssert.True(model.IsCellExplored(Id(24, 0)), "world generation change preserves explored memory");
    }
    private static void InvalidAuthorizationFailsClosed()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        model.Advance(Observation(1, frame: new HeadlampOcclusionFrame(Array.Empty<HeadlampOccluder>(), false)));
        model.Advance(Observation(2, new Vector2(float.NaN, 0)));
        model.Advance(Observation(3, loaded: new[] { new Rect2(0, 0, float.NaN, 8) }));
        model.Advance(Observation(4, frame: new HeadlampOcclusionFrame(new HeadlampOccluder[257], true)));
        model.Advance(Observation(5, frame: new HeadlampOcclusionFrame(new[] { new HeadlampOccluder(new Transform2D(Vector2.Zero, Vector2.Down, Vector2.Zero), new Rect2(0, 0, 8, 8)) }, true)));
        TestAssert.True(!model.IsCellExplored(Id(8, 0)), "unknown/incomplete/invalid coverage and occlusion cannot reveal");
    }
    private static void MemoryRoundtripClearAndMalformed()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog); Drain(model);
        ExplorationMapSaveData save = model.CaptureSave(); ulong revision = model.RevisionCounter;
        model.Clear(); TestAssert.True(!model.IsCellExplored(Id(8, 0)) && model.RevisionCounter > revision, "Clear resets memory and revision");
        model.Restore(save); TestAssert.True(model.IsCellExplored(Id(8, 0)), "explicit pure-memory roundtrip");
        save.TerrainRevision = "other"; model.Restore(save); TestAssert.True(!model.IsCellExplored(Id(8, 0)), "revision mismatch clears optional memory");
        save.TerrainRevision = catalog.Revision; save.FreeCellsBase64 = "!!"; model.Restore(save);
        TestAssert.True(!model.IsCellExplored(Id(8, 0)), "malformed base64 fails closed without exception");
        using var tiny = new MinimapTerrainCatalog(new Rect2(0, 0, 8, 8), "tiny"); var small = new ExplorationMapModel(tiny);
        small.Restore(new ExplorationMapSaveData { TerrainRevision = "tiny", FreeCellsBase64 = "/w==" });
        TestAssert.True(!small.IsCellExplored(0), "nonzero unused padding cannot smuggle authorization");
        small.Restore(new ExplorationMapSaveData { TerrainRevision = "tiny", FreeCellsBase64 = "AQAA" });
        TestAssert.True(!small.IsCellExplored(0), "wrong byte count rejected");
        small.Restore(new ExplorationMapSaveData { FormatVersion = 2, TerrainRevision = "tiny", FreeCellsBase64 = "AQ==" });
        TestAssert.True(!small.IsCellExplored(0), "unsupported format rejected");
    }
    private static void ChecksAtMost16384()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        var shapes = new HeadlampOccluder[256]; Array.Fill(shapes, new HeadlampOccluder(Transform2D.Identity, new Rect2(2000, 2000, 8, 8)));
        var frame = new HeadlampOcclusionFrame(shapes, true); int actual = 0;
        for (ulong i = 1; i <= 64; i++)
        { model.Advance(Observation(i, frame: frame)); TestAssert.True(model.LastCandidateCount <= 64 && model.LastSchedulerNodeExpansions <= 256 && model.LastSchedulerQueryRebuilds <= 2 && model.LastChargedOccluderTests <= 16384 && model.LastFiniteOccluderSweepTests <= model.LastChargedOccluderTests, "ChecksAtMost16384: actual finite sweep tests and charged upper bound remain capped"); actual = Math.Max(actual, model.LastFiniteOccluderSweepTests); }
        TestAssert.True(actual == 16384, "instrumentation observes a real full 64x256 batch, not just arithmetic");
        GD.Print($"MINIMAP_WORK_SAMPLE samples=64 max_actual_finite_sweep_tests={actual} directory_bytes={model.DirectoryBytes} candidate_cache_bytes={model.CandidateCacheBytes}");
    }
    private static void WarmedIdleAllocatesZero()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        for (ulong i = 1; i <= 128; i++) model.Advance(Observation(i));
        long before = GC.GetAllocatedBytesForCurrentThread(); for (ulong i = 129; i <= 640; i++) model.Advance(Observation(i));
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        TestAssert.True(bytes == 0, "WarmedIdleAllocatesZero: actual model idle allocation is zero"); GD.Print($"MINIMAP_ALLOC_IDLE samples=512 bytes={bytes}");
    }
    private static void Warmed64CandidatesAtMost4096Bytes()
    {
        using var catalog = Catalog(); var model = new ExplorationMapModel(catalog);
        var frame = new HeadlampOcclusionFrame(new[] { new HeadlampOccluder(Transform2D.Identity, new Rect2(-1, -1, 2, 2)) }, true);
        for (ulong i = 1; i <= 128; i++) model.Advance(Observation(i, frame: frame));
        long before = GC.GetAllocatedBytesForCurrentThread(); int samples = 0;
        for (ulong i = 129; i <= 640; i++) { model.Advance(Observation(i, frame: frame)); if (model.LastCandidateCount == 64) samples++; }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        TestAssert.True(samples == 512 && bytes <= 4096, "Warmed64CandidatesAtMost4096Bytes: actual 512 full batches stay within allocation bound");
        GD.Print($"MINIMAP_ALLOC_ACTIVE samples={samples} candidates=32768 bytes={bytes}");
    }
}
