using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

public static class FogCostSampleBufferTests
{
    public static void Run()
    {
        var valid = new FogCostSample(100, 200, 300, 5, 5, 9, 10, true,
            true, true, true, true, 0, 0, 0, 1, 120, 600, 5, 20, 0);
        TestAssert.True(FogCostSampleBuffer.Admissible(valid), "real ordered fresh single submission admitted");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { AfterProcess = 6 }),
            "submission from another process frame cannot become a zero cost sample");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { CountAfter = 11 }), "two submissions rejected");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { CountAfter = 9 }), "missing submission rejected");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { FreshSharedSnapshot = false }), "stale snapshot rejected");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { ConsumersValid = false }), "hidden high level consumer invalidates render only arm");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { GameplayValid = false }), "dead unsupported or retired actor invalidates context");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { AfterUsec = 99 }), "regressing bracket rejected");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { PostUsec = 199 }), "post before bracket rejected");
        TestAssert.True(!FogCostSampleBuffer.Admissible(valid with { RenderCpuMs = double.NaN }), "nonfinite metric cannot be serialized as a valid numeric sample");
        var detached = valid with { PresentationExpected = false, CountAfter = 9, FreshSharedSnapshot = false };
        TestAssert.True(FogCostSampleBuffer.Admissible(detached), "removed controller does not require a fabricated snapshot");
        TestAssert.True(!FogCostSampleBuffer.Admissible(detached with { CountAfter = 10 }), "removed controller must not submit");

        var buffer = new FogCostSampleBuffer();
        ulong tick = 1000;
        void Observe(bool validContext = true)
        {
            buffer.Observe(valid with { BeforeUsec = tick, AfterUsec = tick + 200,
                PostUsec = tick + 1000, GameplayValid = validContext });
            tick += 10000;
        }
        Observe(); // Priming callback is not a zero interval.
        Observe(false);
        TestAssert.Equal(0, buffer.WarmupCount, "invalid interval cannot advance warmup");
        for (int index = 0; index < 120; index++) Observe();
        TestAssert.Equal(0, buffer.ValidCount, "exact 120 warmup intervals excluded");
        for (int index = 0; index < 600; index++) Observe();
        TestAssert.True(buffer.Complete, "600 valid bounded samples complete arm");
        TestAssert.Equal(10d, buffer.Intervals[0], "hand supplied interval in milliseconds");
        TestAssert.Equal(.2d, buffer.Brackets[599], "hand supplied bracket in milliseconds");
        Observe();
        TestAssert.True(!buffer.Complete && buffer.OverflowCount == 1 && buffer.ValidCount == 600,
            "extra samples are reported not silently overwrite bounded samples");
        var full = new FogCostSampleBuffer();
        for (int index = 0; index < 8193; index++) full.Observe(valid with { GameplayValid = false });
        TestAssert.Equal(8192, full.RawCount, "raw invalid evidence is bounded too");
        TestAssert.Equal(1, full.OverflowCount, "raw overflow remains explicit");
        GDMarker();
    }

    private static void GDMarker() => Godot.GD.Print("FOG_COST_SAMPLE_CONTRACTS_OK assertions=20 route=false performanceCertification=false");
}
