using System;
using System.Reflection;
using Godot;

namespace Knight.Tests.Unit;

public static class ArcGearMotionModelTests
{
    public static void Run()
    {
        var assembly = typeof(ArcGearMotionModelTests).Assembly;
        Type motion = assembly.GetType("Knight.Mechanisms.ArcGearMotionModel");
        Type geometry = assembly.GetType("Knight.Mechanisms.CircularArcSweep");
        TestAssert.True(motion != null && geometry != null,
            "ARC_GEAR_MODEL_RED: continuous visited-angle motion and exact curved narrowphase are required");
        object Model(Vector2 pivot, float angle, float period) => Activator.CreateInstance(motion, pivot, angle, period);
        object Step(object model, double delta) => motion.GetMethod("Step").Invoke(model, new object[] { delta });
        T Field<T>(object frame, string name) => (T)frame.GetType().GetProperty(name).GetValue(frame);
        double Distance(Vector2 a, Vector2 b, double lo = -Math.PI / 3, double hi = Math.PI / 3) =>
            (double)geometry.GetMethod("SegmentDistanceSquared").Invoke(null,
                new object[] { Vector2.Zero, new Vector2(100, 0), lo, hi, a, b });
        bool Rectangle(Vector2 center, Vector2 size) => (bool)geometry.GetMethod("IntersectsRectangle").Invoke(null,
            new object[] { Vector2.Zero, new Vector2(100, 0), -Math.PI / 3, Math.PI / 3, 2d,
                new Transform2D(0, center), size });
        void Near(double expected, double actual, double tolerance, string why) =>
            TestAssert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance, why + $" expected={expected:R} actual={actual:R}");

        Near(0, Distance(new(100, 0), new(100, 0)), 1e-12, "arc apex point is visited");
        Near(2500, Distance(new(50, 0), new(50, 0)), 1e-9, "chord interior is not visited");
        Near(0, Distance(new(100, -4), new(100, 4)), 1e-12, "capsule core through apex intersects");
        Near(49.8402551840622, Math.Sqrt(Distance(new(50, -4), new(50, 4))), 1e-9, "finite core on chord misses arc");
        Near(3, Math.Sqrt(Distance(new(103, -20), new(103, 20))), 1e-10, "interior normal candidate is necessary");
        Near(0, Distance(new(90, -50), new(90, 50)), 1e-20, "supporting-line roots are necessary, not endpoints or normals only");
        Near(5, Math.Sqrt(Distance(new(105, 0), new(105, 0))), 1e-10, "zero core uses endpoint distance");
        Near(10000, Distance(Vector2.Zero, Vector2.Zero), 1e-8, "pivot-degenerate target is not on the arc");
        TestAssert.True(Rectangle(new(100, 0), new(2, 4)) && !Rectangle(new(50, 0), new(2, 4)) &&
            Rectangle(new(55, 0), new(110, 200)), "arc rectangle apex/chord/wholly-contained controls are independent");
        Near(0, Distance(new(-100, 0), new(-100, 0), Math.PI * 5 / 6, Math.PI * 7 / 6), 1e-20,
            "angle lifting crosses atan2 branch without losing the left apex");

        object model = Model(new(160, 0), Mathf.Pi, 4f);
        object quarter = Step(model, 1d);
        TestAssert.True(Field<Vector2>(quarter, "Offset").DistanceTo(new(160, -160)) < .002f,
            "quarter-period follows upper arc, not endpoint chord");
        Near(394.784176, Field<Vector2>(quarter, "Velocity").X, .001, "analytic tangential quarter velocity");
        Near(0, Field<double>(quarter, "MinimumFraction"), 1e-12, "quarter sweep begins at fraction0");
        Near(.5, Field<double>(quarter, "MaximumFraction"), 1e-12, "quarter sweep reaches fraction.5");
        object crossing = Step(model, 2d); // t1→3 visits the far reversal at t2.
        Near(1, Field<double>(crossing, "MaximumFraction"), 1e-12, "reversal remains in the visited interval");
        object huge = Step(model, double.MaxValue);
        Near(0, Field<double>(huge, "MinimumFraction"), 0, "huge complete cycles retain the whole path");
        Near(1, Field<double>(huge, "MaximumFraction"), 0, "huge complete cycles retain the far endpoint");
        Vector2 unchanged = Field<Vector2>(huge, "Offset");
        foreach (double invalid in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            TestAssert.True(Field<Vector2>(Step(model, invalid), "Offset") == unchanged, "invalid/nonpositive delta cannot advance arc");
        motion.GetMethod("Reset").Invoke(model, null);
        object reset = motion.GetProperty("Current").GetValue(model);
        TestAssert.True(Field<Vector2>(reset, "Offset") == Vector2.Zero && Field<Vector2>(reset, "Velocity") == Vector2.Zero,
            "reset is exactly authored origin and rest");
        object reverse = Step(Model(new(160, 0), -Mathf.Pi, 4f), 1d);
        TestAssert.True(Field<Vector2>(reverse, "Offset").DistanceTo(new(160, 160)) < .002f, "signed direction selects the other arc");
        foreach (int hz in new[] { 30, 60, 120 })
        {
            object sample = Model(new(160, 0), Mathf.Pi, 4f), frame = null;
            for (int i = 0; i < 4 * hz; i++) frame = Step(sample, 1d / hz);
            TestAssert.True(Field<Vector2>(frame, "Offset").Length() < .002f, "full period returns at " + hz);
        }
        int rejected = 0;
        foreach (var bad in new[] {
            (new Vector2(0,0),1f,4f), (new Vector2(float.NaN,0),1f,4f), (new Vector2(513,0),1f,4f),
            (new Vector2(160,0),0f,4f), (new Vector2(160,0),4f,4f), (new Vector2(160,0),float.NaN,4f),
            (new Vector2(160,0),1f,.5f), (new Vector2(160,0),1f,21f), (new Vector2(160,0),1f,float.PositiveInfinity),
            (new Vector2(512,0),Mathf.Pi,1f) })
        {
            try { Model(bad.Item1, bad.Item2, bad.Item3); }
            catch (TargetInvocationException error) when (error.InnerException is ArgumentOutOfRangeException) { rejected++; }
        }
        TestAssert.Equal(10, rejected, "all invalid snapshots fail before partial setup");
        GD.Print("ARC_GEAR_MODEL_TESTS_OK literal_geometry=true visited_reversal=true full_period=true signed=true rates=3 invalid=10 native_world=false");
    }
}
