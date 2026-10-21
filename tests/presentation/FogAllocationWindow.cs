namespace Knight.Tests.Presentation;

internal readonly record struct FogAllocationStamp(ulong Usec, ulong Process, int Thread, long Bytes);
internal readonly record struct FogAllocationSegments(long BetweenCallbacks, long Submission, long AfterSubmission)
{
    public long Total => BetweenCallbacks + Submission + AfterSubmission;
}

// Pure test-only partition. No engine objects, timers, IO, or collection allocations.
internal readonly record struct FogAllocationWindow(FogAllocationStamp PreviousPost,
    FogAllocationStamp Before, FogAllocationStamp After, FogAllocationStamp Post)
{
    public bool TryPartition(out FogAllocationSegments segments)
    {
        segments = default;
        if (PreviousPost.Thread <= 0 || Before.Thread != PreviousPost.Thread ||
            After.Thread != Before.Thread || Post.Thread != Before.Thread ||
            PreviousPost.Process == ulong.MaxValue || Before.Process != PreviousPost.Process + 1 ||
            After.Process != Before.Process || Post.Process != Before.Process ||
            Before.Usec < PreviousPost.Usec || After.Usec < Before.Usec || Post.Usec < After.Usec ||
            Post.Usec <= PreviousPost.Usec || PreviousPost.Bytes < 0 ||
            Before.Bytes < PreviousPost.Bytes || After.Bytes < Before.Bytes || Post.Bytes < After.Bytes)
            return false;
        segments = new(Before.Bytes - PreviousPost.Bytes, After.Bytes - Before.Bytes, Post.Bytes - After.Bytes);
        return true;
    }
}
