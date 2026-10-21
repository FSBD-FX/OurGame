using System;

namespace Knight.Tests.Presentation;

internal readonly record struct MinimapFrameStamp(long Ticks, ulong Frame, ulong Physics,
    int Process, int Thread, long Bytes, int Gc0, int Gc1, int Gc2)
{
    internal FogAllocationStamp Allocation => new(unchecked((ulong)Ticks), Frame, Thread, Bytes);
}

internal readonly record struct MinimapFrameEndpoint(int Arm, int Index,
    MinimapFrameStamp Pre, MinimapFrameStamp After, MinimapFrameStamp Post,
    bool Focused, bool Valid, bool HooksValid, ulong ModelRevision,
    int HandlerCalls, long HandlerTicks, long HandlerBytes, bool HandlerAvailable,
    double TimeProcessSeconds, double TimePhysicsSeconds,
    double RenderCpuMs, double RenderGpuMs, double SetupCpuMs, int DrawCalls,
    MinimapFrameStamp HandlerFirst = default, MinimapFrameStamp HandlerLast = default,
    ulong World = 0, ulong Actor = 0, ulong Support = 0, int Health = 0,
    float X = 0, float Y = 0, ulong Generation = 0, int ModelIdentity = 0);

internal sealed class MinimapFrameDiagnosticBuffer
{
    internal const int Capacity = 721;
    internal readonly MinimapFrameEndpoint[] Endpoints = new MinimapFrameEndpoint[Capacity];
    internal int Count { get; private set; }
    internal int Invalid { get; private set; }
    internal int Overflow { get; private set; }
    internal void Add(in MinimapFrameEndpoint endpoint)
    {
        if (Count == Capacity) { Overflow++; return; }
        Endpoints[Count++] = endpoint; // Never filter, repair, replace or reorder evidence.
        if (!Admissible(endpoint)) Invalid++;
    }

    internal static bool Admissible(in MinimapFrameEndpoint e) => e.Valid && e.Focused && e.HooksValid &&
        e.Pre.Ticks > 0 && e.After.Ticks >= e.Pre.Ticks && e.Post.Ticks >= e.After.Ticks &&
        e.Pre.Frame == e.After.Frame && e.Pre.Frame == e.Post.Frame &&
        e.Pre.Process > 0 && e.Pre.Process == e.After.Process && e.Pre.Process == e.Post.Process &&
        e.Pre.Thread > 0 && e.Pre.Thread == e.After.Thread && e.Pre.Thread == e.Post.Thread &&
        e.Pre.Bytes >= 0 && e.After.Bytes >= e.Pre.Bytes && e.Post.Bytes >= e.After.Bytes &&
        HandlerContained(e) &&
        FiniteNonnegative(e.TimeProcessSeconds) && FiniteNonnegative(e.TimePhysicsSeconds) &&
        FiniteNonnegative(e.RenderCpuMs) && FiniteNonnegative(e.RenderGpuMs) && FiniteNonnegative(e.SetupCpuMs);

    private static bool FiniteNonnegative(double value) => double.IsFinite(value) && value >= 0;

    internal static bool HandlerContained(in MinimapFrameEndpoint e)
    {
        if (e.Arm != 1) return e.HandlerCalls == 0 && !e.HandlerAvailable;
        var first = e.HandlerFirst; var last = e.HandlerLast;
        return e.HandlerCalls == 1 && e.HandlerAvailable &&
            first.Ticks >= e.Pre.Ticks && last.Ticks >= first.Ticks && last.Ticks <= e.After.Ticks &&
            first.Frame == e.Pre.Frame && last.Frame == e.Pre.Frame &&
            first.Physics == e.Pre.Physics && last.Physics == e.Pre.Physics &&
            first.Process == e.Pre.Process && last.Process == e.Pre.Process &&
            first.Thread == e.Pre.Thread && last.Thread == e.Pre.Thread &&
            first.Bytes >= e.Pre.Bytes && last.Bytes >= first.Bytes && last.Bytes <= e.After.Bytes &&
            e.HandlerTicks == last.Ticks - first.Ticks && e.HandlerBytes == last.Bytes - first.Bytes;
    }

    internal bool TrySegments(int index, out MinimapFrameSegments segments)
    {
        segments = default;
        if (index <= 0 || index >= Count) return false; // First boundary is unavailable, never zero.
        var previous = Endpoints[index - 1]; var current = Endpoints[index];
        if (!Admissible(previous) || !Admissible(current) || previous.Post.Process != current.Pre.Process ||
            !new FogAllocationWindow(previous.Post.Allocation, current.Pre.Allocation,
                current.After.Allocation, current.Post.Allocation).TryPartition(out var bytes)) return false;
        segments = new(current.Pre.Ticks - previous.Post.Ticks,
            current.After.Ticks - current.Pre.Ticks, current.Post.Ticks - current.After.Ticks,
            current.Post.Ticks - previous.Post.Ticks, bytes);
        return true;
    }
}

internal readonly record struct MinimapFrameSegments(long BetweenPostsToPreTicks,
    long AllPreDrawSubscribersTicks, long AfterPreToPostTicks, long PostIntervalTicks,
    FogAllocationSegments Bytes);
