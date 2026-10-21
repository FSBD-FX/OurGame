using System;
using System.Runtime.CompilerServices;
using Godot;

namespace Knight.Presentation.Minimap;

// A best-first frontier over immutable boundary subtrees and individual cells.
// A subtree's CLOSED double AABB supplies only a conservative lower bound;
// no exact candidate can pass a subtree with a strictly smaller bound.
// Moving the actor rebuilds priorities, not attempted-in-cycle authorization.
internal sealed class MinimapCandidateScheduler
{
    internal const int MaximumNodeExpansionsPerStamp = 256;
    private const int LocalSide = 43;
    private struct Node : IComparable<Node>
    {
        internal double X, Y, MinX, MinY, MaxX, MaxY;
        internal int BoundaryId, Left, Right, Parent, SubtreeSize, Pending;
        internal ulong PendingEpoch, AttemptEpoch;
        public readonly int CompareTo(Node other)
        {
            int x = X.CompareTo(other.X); if (x != 0) return x;
            int y = Y.CompareTo(other.Y); return y != 0 ? y : BoundaryId.CompareTo(other.BoundaryId);
        }
    }
    private struct Entry
    {
        internal double Distance;
        internal int Id, Kind; // 0 cell, 1 exact boundary point, 2 subtree lower bound.
    }
    private readonly MinimapTerrainCatalog _catalog;
    private readonly Node[] _nodes;
    private readonly Entry[] _heap;
    private readonly ulong[] _cellAttempts;
    private readonly int _root;
    private int _heapCount;
    private ulong _epoch = 1;
    private Vector2 _actor;
    private bool _hasActor;
    internal int LastNodeExpansions { get; private set; }
    internal int LastQueryRebuilds { get; private set; }
    internal int CycleCandidateUpperBound { get; private set; }
    internal int DirectoryBytes => _nodes.Length * Unsafe.SizeOf<Node>() + _cellAttempts.Length * sizeof(ulong);
    internal int CandidateCacheBytes => _heap.Length * Unsafe.SizeOf<Entry>();

    internal MinimapCandidateScheduler(MinimapTerrainCatalog catalog)
    {
        _catalog = catalog;
        _cellAttempts = new ulong[catalog.CellCount];
        _nodes = new Node[catalog.BoundaryCount];
        _heap = new Entry[catalog.BoundaryCount + LocalSide * LocalSide];
        for (int i = 0; i < _nodes.Length; i++)
        {
            MinimapBoundary b = catalog.Boundary(i); MinimapSolid solid = catalog.Solid(b.ShapeId);
            Vector2 a = solid.LocalToWorld * b.FromLocal, z = solid.LocalToWorld * b.ToLocal;
            // Midpoint of the actual world endpoints, with no rounded float AABB.
            _nodes[i] = new Node { X = ((double)a.X + z.X) * .5, Y = ((double)a.Y + z.Y) * .5, BoundaryId = i };
        }
        Array.Sort(_nodes);
        _root = Build(0, _nodes.Length - 1, -1);
    }
    private int Build(int first, int last, int parent)
    {
        if (first > last) return -1;
        int mid = first + (last - first) / 2;
        ref Node node = ref _nodes[mid];
        node.Left = Build(first, mid - 1, mid); node.Right = Build(mid + 1, last, mid);
        node.Parent = parent; node.SubtreeSize = last - first + 1;
        node.MinX = node.MaxX = node.X; node.MinY = node.MaxY = node.Y;
        Include(ref node, node.Left); Include(ref node, node.Right);
        return mid;
    }
    private void Include(ref Node node, int child)
    {
        if (child < 0) return;
        ref Node other = ref _nodes[child];
        node.MinX = Math.Min(node.MinX, other.MinX); node.MaxX = Math.Max(node.MaxX, other.MaxX);
        node.MinY = Math.Min(node.MinY, other.MinY); node.MaxY = Math.Max(node.MaxY, other.MaxY);
    }
    internal void BeginStamp(Vector2 actor)
    {
        LastNodeExpansions = LastQueryRebuilds = 0;
        if (!_hasActor || actor != _actor) { _actor = actor; _hasActor = true; RebuildQuery(); }
    }
    internal void Reset()
    {
        // Explicit Clear/Restore lifecycle only; do not recycle an epoch/ID.
        if (_epoch < ulong.MaxValue) _epoch++;
        _hasActor = false; _heapCount = 0;
        LastNodeExpansions = LastQueryRebuilds = 0;
    }
    internal bool TryNext(out int id, out bool boundary)
    {
        id = -1; boundary = false;
        while (true)
        {
            if (_heapCount == 0)
            {
                if (LastQueryRebuilds >= 2 || _epoch == ulong.MaxValue) return false;
                _epoch++; RebuildQuery();
                if (_heapCount == 0) return false;
            }
            Entry next = _heap[0];
            if (next.Kind == 2)
            {
                if (LastNodeExpansions == MaximumNodeExpansionsPerStamp) return false;
                Pop(); LastNodeExpansions++;
                ref Node node = ref _nodes[next.Id];
                if (Pending(next.Id) == 0 || next.Distance > 25600d) continue;
                if (node.AttemptEpoch != _epoch)
                    Push(new Entry { Id = next.Id, Kind = 1, Distance = Distance(node.X, node.Y) });
                PushSubtree(node.Left); PushSubtree(node.Right);
                continue;
            }
            Pop();
            if (next.Kind == 0)
            {
                _cellAttempts[next.Id] = _epoch; id = next.Id; return true;
            }
            ref Node candidate = ref _nodes[next.Id];
            candidate.AttemptEpoch = _epoch;
            for (int at = next.Id; at >= 0; at = _nodes[at].Parent)
            { Pending(at); _nodes[at].Pending--; }
            id = candidate.BoundaryId; boundary = true; return true;
        }
    }
    private int Pending(int at)
    {
        ref Node node = ref _nodes[at];
        if (node.PendingEpoch != _epoch) { node.PendingEpoch = _epoch; node.Pending = node.SubtreeSize; }
        return node.Pending;
    }
    private void RebuildQuery()
    {
        LastQueryRebuilds++; _heapCount = 0;
        int x = (int)Math.Floor(((double)_actor.X - _catalog.WorldBounds.Position.X) / 8);
        int y = (int)Math.Floor(((double)_actor.Y - _catalog.WorldBounds.Position.Y) / 8);
        int localCount = 0;
        for (int row = Math.Max(0, y - 21); row <= Math.Min(_catalog.Rows - 1, y + 21); row++)
        for (int column = Math.Max(0, x - 21); column <= Math.Min(_catalog.Columns - 1, x + 21); column++)
        {
            int id = row * _catalog.Columns + column; localCount++;
            if (_cellAttempts[id] == _epoch) continue;
            Vector2 center = _catalog.Cell(id).GetCenter();
            if (_heapCount == _heap.Length) throw new InvalidOperationException("Candidate frontier invariant violated.");
            _heap[_heapCount++] = new Entry { Id = id, Kind = 0, Distance = Distance(center.X, center.Y) };
        }
        // Same exact entries/comparator; Floyd construction avoids O(n log n)
        // insertion work on every sub-cell actor movement.
        for (int at = _heapCount / 2 - 1; at >= 0; at--) SiftDown(at, _heap[at]);
        CycleCandidateUpperBound = localCount + _nodes.Length;
        PushSubtree(_root);
    }
    private double Distance(double x, double y)
    { double dx = x - _actor.X, dy = y - _actor.Y; return dx * dx + dy * dy; }
    private void PushSubtree(int at)
    {
        if (at < 0 || Pending(at) == 0) return;
        ref Node n = ref _nodes[at];
        double x = Math.Clamp((double)_actor.X, n.MinX, n.MaxX), y = Math.Clamp((double)_actor.Y, n.MinY, n.MaxY);
        double distance = Distance(x, y);
        if (distance <= 25600d) Push(new Entry { Id = at, Kind = 2, Distance = distance });
    }
    private static bool Before(Entry a, Entry b)
    {
        if (a.Distance != b.Distance) return a.Distance < b.Distance;
        if (a.Kind != b.Kind) return a.Kind < b.Kind; // Exact candidates may win equal lower-bound ties.
        return a.Id < b.Id;
    }
    private void Push(Entry entry)
    {
        // Every frontier entry represents at least one distinct remaining
        // candidate. Capacity is B+1849, never a resizing/truncating queue.
        if (_heapCount == _heap.Length) throw new InvalidOperationException("Candidate frontier invariant violated.");
        int at = _heapCount++;
        while (at > 0)
        {
            int parent = (at - 1) / 2; if (!Before(entry, _heap[parent])) break;
            _heap[at] = _heap[parent]; at = parent;
        }
        _heap[at] = entry;
    }
    private void Pop()
    {
        Entry tail = _heap[--_heapCount]; if (_heapCount == 0) return;
        SiftDown(0, tail);
    }
    private void SiftDown(int at, Entry tail)
    {
        while (at * 2 + 1 < _heapCount)
        {
            int child = at * 2 + 1;
            if (child + 1 < _heapCount && Before(_heap[child + 1], _heap[child])) child++;
            if (!Before(_heap[child], tail)) break;
            _heap[at] = _heap[child]; at = child;
        }
        _heap[at] = tail;
    }
}
