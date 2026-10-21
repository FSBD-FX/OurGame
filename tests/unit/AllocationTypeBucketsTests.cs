using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

public static class AllocationTypeBucketsTests
{
    public static void Run()
    {
        var buckets = new AllocationTypeBuckets(2);
        buckets.Observe("System.String", 0, 100000);
        TestAssert.Equal(1, buckets.Count, "real delivered type tick must be retained");
        buckets.Observe("System.String", 0, 110000);
        TestAssert.Equal(1, buckets.Count, "same type and heap kind combine without an extra row");
        TestAssert.Equal(2L, buckets.Rows[0].Ticks, "two threshold events not two objects");
        TestAssert.Equal(210000UL, buckets.Rows[0].ThresholdBytes, "hand-derived threshold byte sum, not exact type allocation");
        buckets.Observe("System.String", 1, 120000);
        TestAssert.Equal(2, buckets.Count, "small and large heap kinds remain separate");
        buckets.Observe("System.Object[]", 0, 100000);
        TestAssert.Equal(1, buckets.Dropped, "bounded overflow explicit, existing rows preserved");
        TestAssert.Equal(3L, buckets.Ticks, "accepted tick count excludes overflow");
        buckets.Observe(null, 0, 100000);
        buckets.Observe("", 0, 100000);
        buckets.Observe("System.String", 0, 0);
        buckets.Observe("System.String", 3, 100000);
        TestAssert.Equal(4, buckets.Rejected, "missing type, zero bytes and unknown heap kind rejected");
        TestAssert.Equal(2L, buckets.Rows[0].Ticks, "invalid and overflow observations do not corrupt retained count");
        var zero = new AllocationTypeBuckets(0);
        zero.Observe("System.String", 0, 100000);
        TestAssert.Equal(1, zero.Dropped, "zero capacity is explicit bounded refusal");
        Godot.GD.Print("ALLOCATION_TYPE_BUCKET_CONTRACTS_OK assertions=10 performanceCertification=false");
    }
}
