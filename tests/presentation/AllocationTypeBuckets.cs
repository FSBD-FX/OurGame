using System;

namespace Knight.Tests.Presentation;

internal readonly record struct AllocationTypeBucket(string Type, uint Kind, long Ticks, ulong ThresholdBytes);

// Type is the object crossing the sampling threshold, NOT a census of bytes of this type.
internal sealed class AllocationTypeBuckets
{
    private readonly AllocationTypeBucket[] _rows;
    public int Count { get; private set; }
    public int Rejected { get; private set; }
    public int Dropped { get; private set; }
    public long Ticks { get; private set; }
    public ReadOnlySpan<AllocationTypeBucket> Rows => _rows.AsSpan(0, Count);
    public AllocationTypeBuckets(int capacity = 64) => _rows = new AllocationTypeBucket[capacity];

    public void Observe(string type, uint kind, ulong thresholdBytes)
    {
        if (string.IsNullOrEmpty(type) || kind > 2 || thresholdBytes == 0) { Rejected++; return; }
        for (int index = 0; index < Count; index++)
        {
            AllocationTypeBucket row = _rows[index];
            if (row.Kind != kind || !string.Equals(row.Type, type, StringComparison.Ordinal)) continue;
            _rows[index] = row with { Ticks = row.Ticks + 1, ThresholdBytes = row.ThresholdBytes + thresholdBytes };
            Ticks++;
            return;
        }
        if (Count == _rows.Length) { Dropped++; return; }
        _rows[Count++] = new(type, kind, 1, thresholdBytes);
        Ticks++;
    }
}
