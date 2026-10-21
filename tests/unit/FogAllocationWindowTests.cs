using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

public static class FogAllocationWindowTests
{
    public static void Run()
    {
        var previous = new FogAllocationStamp(100, 4, 1, 1000);
        var before = new FogAllocationStamp(200, 5, 1, 1300);
        var after = new FogAllocationStamp(210, 5, 1, 1500);
        var post = new FogAllocationStamp(240, 5, 1, 1600);
        var window = new FogAllocationWindow(previous, before, after, post);
        TestAssert.True(window.TryPartition(out var segments), "adjacent same-thread monotonic boundaries must partition");
        TestAssert.Equal(300L, segments.BetweenCallbacks, "hand-derived between-callback bytes");
        TestAssert.Equal(200L, segments.Submission, "hand-derived submission bytes");
        TestAssert.Equal(100L, segments.AfterSubmission, "hand-derived after-submission bytes");
        TestAssert.Equal(600L, segments.Total, "partition sum reconciles to post minus previous post");
        TestAssert.True(!(window with { PreviousPost = default }).TryPartition(out _), "first callback cannot invent a previous allocation stamp");
        TestAssert.True(!(window with { Before = before with { Thread = 2 } }).TryPartition(out _), "thread-local counters from different threads cannot subtract");
        TestAssert.True(!(window with { After = after with { Process = 6 } }).TryPartition(out _), "same-frame submission required");
        TestAssert.True(!(window with { Post = post with { Process = 6 } }).TryPartition(out _), "same-frame post boundary required");
        TestAssert.True(!(window with { PreviousPost = previous with { Process = 3 } }).TryPartition(out _), "missed callback cannot masquerade as one-frame allocation");
        TestAssert.True(!(window with { PreviousPost = previous with { Process = ulong.MaxValue } }).TryPartition(out _), "frame overflow cannot wrap to adjacency");
        TestAssert.True(!(window with { Before = before with { Usec = 99 } }).TryPartition(out _), "regressing pre-boundary clock rejected");
        TestAssert.True(!(window with { After = after with { Usec = 199 } }).TryPartition(out _), "regressing submission clock rejected");
        TestAssert.True(!(window with { Post = post with { Usec = 209 } }).TryPartition(out _), "regressing post-boundary clock rejected");
        TestAssert.True(!(window with { Before = before with { Bytes = 999 } }).TryPartition(out _), "regressing inter-callback allocation rejected");
        TestAssert.True(!(window with { After = after with { Bytes = 1299 } }).TryPartition(out _), "regressing submission allocation rejected");
        TestAssert.True(!(window with { Post = post with { Bytes = 1499 } }).TryPartition(out _), "regressing post allocation rejected");
        TestAssert.True(!(window with { PreviousPost = previous with { Bytes = -1 } }).TryPartition(out _), "negative counter is not a valid origin");
        var empty = window with { Before = before with { Bytes = 1000 }, After = after with { Bytes = 1000 }, Post = post with { Bytes = 1000 } };
        TestAssert.True(empty.TryPartition(out var zero) && zero.Total == 0, "measured zero is valid with complete boundaries, unlike missing data");
        TestAssert.True(!(window with { Post = post with { Usec = previous.Usec } }).TryPartition(out _), "nonincreasing whole interval rejected");
        Godot.GD.Print("FOG_ALLOCATION_WINDOW_CONTRACTS_OK assertions=20 performanceCertification=false");
    }
}
