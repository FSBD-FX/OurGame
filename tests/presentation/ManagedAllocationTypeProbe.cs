using System;
using System.Diagnostics.Tracing;

namespace Knight.Tests.Presentation;

/// <summary>Own process only. Buffered threshold events, not exact type attribution.</summary>
internal sealed class ManagedAllocationTypeProbe : EventListener
{
    private readonly object _gate = new();
    private readonly AllocationTypeBuckets _buckets = new();
    private EventSource _runtime;
    private volatile bool _sampling;
    private int _malformed;
    private ManagedAllocationTypeEvidence _evidence;

    public void Start()
    {
        foreach (EventSource source in EventSource.GetSources())
            if (source.Name == "Microsoft-Windows-DotNETRuntime") { _runtime = source; break; }
        if (_runtime == null) throw new InvalidOperationException("Own-process runtime EventSource unavailable.");
        EnableEvents(_runtime, EventLevel.Verbose, (EventKeywords)1);
        _sampling = true;
    }

    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if (!_sampling || data.EventId != 10 || data.EventSource != _runtime) return;
        string type = null;
        uint kind = uint.MaxValue;
        ulong bytes = 0;
        if (data.PayloadNames != null && data.Payload != null)
            for (int index = 0; index < data.PayloadNames.Count; index++)
            {
                if (data.PayloadNames[index] == "TypeName") type = data.Payload[index] as string;
                if (data.PayloadNames[index] == "AllocationKind" && data.Payload[index] is uint value) kind = value;
                if (data.PayloadNames[index] == "AllocationAmount64" && data.Payload[index] is ulong amount) bytes = amount;
            }
        lock (_gate)
        {
            if (!_sampling) return;
            if (type == null || kind == uint.MaxValue || bytes == 0) { _malformed++; return; }
            _buckets.Observe(type, kind, bytes);
        }
    }

    public ManagedAllocationTypeEvidence StopAndRetain()
    {
        if (_evidence != null) return _evidence;
        _sampling = false;
        if (_runtime != null) DisableEvents(_runtime);
        lock (_gate)
            _evidence = new(_runtime != null, _buckets.Ticks, _buckets.Rejected, _buckets.Dropped,
                _malformed, _buckets.Rows.ToArray());
        Dispose();
        return _evidence;
    }
}

internal sealed record ManagedAllocationTypeEvidence(bool ProviderFound, long Ticks,
    int Rejected, int Dropped, int Malformed, AllocationTypeBucket[] Rows);
