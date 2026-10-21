using System;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

internal static class MinimapFrameDiagnosticBufferTests
{
    internal static MinimapFrameEndpoint Sample(int index = 0) => new(0, index,
        new(110, 11, 11, 77, 1, 120, 0, 0, 0),
        new(140, 11, 11, 77, 1, 180, 0, 0, 0),
        new(200, 11, 11, 77, 1, 200, 0, 0, 0),
        true, true, true, 9, 0, 0, 0, false, .001, .002, 0, 0, 0, 0);

    internal static void Run()
    {
        // Break caught: filtering invalid endpoints erases the very evidence being diagnosed.
        var buffer = new MinimapFrameDiagnosticBuffer();
        var invalid = Sample() with { Valid = false, RenderCpuMs = double.NaN };
        buffer.Add(invalid);
        TestAssert.True(buffer.Count == 1 && buffer.Invalid == 1 &&
            !buffer.Endpoints[0].Valid && double.IsNaN(buffer.Endpoints[0].RenderCpuMs),
            "FrameDiagnosticRetainsInvalidEndpoints: invalid numeric endpoint must be retained and counted");
        for (int i = 1; i < 721; i++) buffer.Add(Sample(i));
        buffer.Add(Sample(722));
        TestAssert.True(buffer.Count == 721 && buffer.Overflow == 1 &&
            buffer.Endpoints[0].Equals(invalid) && buffer.Endpoints[720].Index == 720,
            "722nd endpoint explicitly overflows without overwriting first or last");
        var partition = new MinimapFrameDiagnosticBuffer();
        var previous = Sample() with { Post = new(100, 10, 10, 77, 1, 100, 0, 0, 0),
            Pre = new(70, 10, 10, 77, 1, 70, 0, 0, 0), After = new(80, 10, 10, 77, 1, 80, 0, 0, 0) };
        partition.Add(previous); partition.Add(Sample(1));
        TestAssert.True(!partition.TrySegments(0, out _), "first endpoint has no fabricated preceding boundary");
        TestAssert.True(partition.TrySegments(1, out var parts) &&
            parts.BetweenPostsToPreTicks == 10 && parts.AllPreDrawSubscribersTicks == 30 &&
            parts.AfterPreToPostTicks == 60 && parts.PostIntervalTicks == 100 &&
            parts.Bytes.BetweenCallbacks == 20 && parts.Bytes.Submission == 60 && parts.Bytes.AfterSubmission == 20,
            "hand derived same-frame ticks 10/30/60/100 and bytes 20/60/20");
        // Break caught: discontinuous identities or counters must not become zero cost.
        var current = Sample(1);
        MinimapFrameEndpoint[] bad = [
            current with { Pre = current.Pre with { Thread = 2 } },
            current with { After = current.After with { Frame = 12 } },
            current with { Pre = current.Pre with { Frame = 12 }, After = current.After with { Frame = 12 }, Post = current.Post with { Frame = 12 } },
            current with { Pre = current.Pre with { Ticks = 99 } },
            current with { After = current.After with { Ticks = 109 } },
            current with { Pre = current.Pre with { Bytes = 99 } },
            current with { Post = current.Post with { Bytes = 179 } },
            current with { Pre = current.Pre with { Process = 78 }, After = current.After with { Process = 78 }, Post = current.Post with { Process = 78 } }
        ];
        foreach (var endpoint in bad)
        {
            var discontinuity = new MinimapFrameDiagnosticBuffer();
            discontinuity.Add(previous); discontinuity.Add(endpoint);
            TestAssert.True(!discontinuity.TrySegments(1, out _), "thread/frame/clock/allocation/process discontinuity unavailable");
            TestAssert.True(discontinuity.Count == 2 && discontinuity.Endpoints[1].Equals(endpoint), "discontinuity retained exactly");
        }
        var prewarm = new MinimapFrameDiagnosticBuffer();
        for (int i = 0; i < 100; i++) prewarm.Add(current);
        var noAllocation = new MinimapFrameDiagnosticBuffer();
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 721; i++) noAllocation.Add(current);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        TestAssert.Equal(0L, allocated, "prewarmed bounded Add has zero managed allocation");
        // Break caught: a handler after the nominal after-pre hook must invalidate attribution,
        // even when its own clock, frame and thread are individually monotonic.
        var outside = Sample() with { Arm = 1, HandlerCalls = 1, HandlerAvailable = true,
            HandlerTicks = 20, HandlerBytes = 10,
            HandlerFirst = new(150,11,11,77,1,185,0,0,0),
            HandlerLast = new(170,11,11,77,1,195,0,0,0) };
        TestAssert.True(!MinimapFrameDiagnosticBuffer.Admissible(outside),
            "FrameDiagnosticRejectsHandlerOutsidePreAfter: individually valid handler after after-pre invalidates hooks");
        var contained = outside with { HandlerFirst = new(115,11,11,77,1,130,0,0,0),
            HandlerLast = new(135,11,11,77,1,150,0,0,0), HandlerBytes = 20 };
        TestAssert.True(MinimapFrameDiagnosticBuffer.Admissible(contained), "one actual same-thread/frame contained handler admitted");
        MinimapFrameEndpoint[] badHooks = [outside,
            contained with { HandlerFirst = contained.HandlerFirst with { Ticks = 109 } },
            contained with { HandlerLast = contained.HandlerLast with { Thread = 2 } },
            contained with { HandlerLast = contained.HandlerLast with { Frame = 12 } },
            contained with { HandlerLast = contained.HandlerLast with { Process = 78 } },
            contained with { HandlerLast = contained.HandlerLast with { Bytes = 181 } },
            contained with { HandlerCalls = 0, HandlerAvailable = false },
            contained with { HandlerCalls = 2 }, contained with { Arm = 0 },
            contained with { HandlerTicks = 0 }, contained with { HandlerBytes = 0 }
        ];
        foreach (var badHook in badHooks)
        {
            var retained = new MinimapFrameDiagnosticBuffer(); retained.Add(badHook);
            TestAssert.True(retained.Count == 1 && retained.Invalid == 1 && retained.Endpoints[0].Equals(badHook),
                "hook mismatch retained and counted invalid, never becomes valid zero-cost observation");
        }
        Godot.GD.Print("MINIMAP_FRAME_COLLECTOR_CONTROLS_OK");
    }
}
