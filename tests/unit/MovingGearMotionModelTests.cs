using System;
using System.Reflection;
using Godot;

namespace Knight.Tests.Unit;

public static class MovingGearMotionModelTests
{
    private const double PositionTolerance = .001;
    private const double VelocityTolerance = .003;
    private const double PhaseTolerance = .0000001;

    public static void Run()
    {
        // Reflection keeps the test-only revision buildable before the real model exists.
        Type modelType = typeof(MovingGearMotionModelTests).Assembly.GetType(
            "Knight.Mechanisms.MovingGearMotionModel");
        TestAssert.True(modelType != null,
            "MOVING_GEAR_MODEL_RED: real periodic moving-gear motion model is missing");
        var api = new ModelApi(modelType);

        LiteralQuarterHalfAndFullCycleHaveAnalyticVelocity(api);
        CompleteCyclesKeepTheEntireTravelInTheirSweep(api);
        CrossingEitherTurnaroundIncludesItsActualExtremum(api);
        SignedAndVerticalTravelKeepCoefficientOrderedSweeps(api);
        DiagonalTravelDoesNotNormalizeAwayItsComponents(api);
        ThirtySixtyAndOneTwentyPartitionsReachLiteralHalfAndFullCycles(api);
        InvalidDeltasDoNotAdvanceOrReuseThePreviousSweep(api);
        ResetDiscardsTheOldClockAndOldSweep(api);
        InvalidConstructionThrowsWithoutPublishingAPartialModel(api);
        ValidLengthPeriodAndSpeedBoundariesAreNotOverRejected(api);

        GD.Print("MOVING_GEAR_MODEL_TESTS_OK literal_cycles=true analytic_velocity=true full_cycle_sweep=true " +
            "turnarounds=true signed_vertical=true partition_30_60_120=true invalid_delta=true reset=true " +
            "constructor_validation=true human=false");
    }

    private static void LiteralQuarterHalfAndFullCycleHaveAnalyticVelocity(ModelApi api)
    {
        // Catches linear interpolation, the wrong period, and finite-difference velocity.
        object model = api.Create(new Vector2(120f, 0f), 1f);
        ExpectFrame(api.Step(model, 0d), Vector2.Zero, Vector2.Zero,
            Vector2.Zero, Vector2.Zero, 0d, "initial zero-speed endpoint");

        MotionSnapshot quarter = api.Step(model, .25d);
        ExpectFrame(quarter, new Vector2(60f, 0f), new Vector2(376.991118f, 0f),
            Vector2.Zero, new Vector2(60f, 0f), .25d, "literal quarter cycle");
        ExpectFrame(api.Step(model, .25d), new Vector2(120f, 0f), Vector2.Zero,
            new Vector2(60f, 0f), new Vector2(120f, 0f), .5d, "literal half cycle");
        ExpectFrame(api.Step(model, .25d), new Vector2(60f, 0f), new Vector2(-376.991118f, 0f),
            new Vector2(60f, 0f), new Vector2(120f, 0f), .75d, "literal return quarter");
        ExpectFrame(api.Step(model, .25d), Vector2.Zero, Vector2.Zero,
            Vector2.Zero, new Vector2(60f, 0f), 0d, "literal completed cycle");

        // A reused mutable result must not silently rewrite an already-returned frame.
        ExpectFrame(api.Read(quarter.Source), new Vector2(60f, 0f), new Vector2(376.991118f, 0f),
            Vector2.Zero, new Vector2(60f, 0f), .25d, "previous frame remains a value snapshot");
    }

    private static void CompleteCyclesKeepTheEntireTravelInTheirSweep(ModelApi api)
    {
        // Catches the endpoint-only sweep that would miss a body crossed mid-frame.
        object model = api.Create(new Vector2(120f, 0f), 1f);
        ExpectFrame(api.Step(model, 1d), Vector2.Zero, Vector2.Zero,
            Vector2.Zero, new Vector2(120f, 0f), 0d, "one whole cycle with equal endpoints");
        api.Step(model, .25d);
        ExpectFrame(api.Step(model, 2d), new Vector2(60f, 0f), new Vector2(376.991118f, 0f),
            Vector2.Zero, new Vector2(120f, 0f), .25d, "two cycles from an interior phase");

        // A finite enormous delta must be reduced before adding it to the bounded phase.
        ExpectFrame(api.Step(model, double.MaxValue), new Vector2(60f, 0f), new Vector2(376.991118f, 0f),
            Vector2.Zero, new Vector2(120f, 0f), .25d, "large finite cycle count preserves the interior phase");
    }

    private static void CrossingEitherTurnaroundIncludesItsActualExtremum(ModelApi api)
    {
        // Equal segment endpoints still visit B, or A, between those endpoints.
        object upperTurn = api.Create(new Vector2(120f, 0f), 1f);
        api.Step(upperTurn, .375d);
        ExpectFrame(api.Step(upperTurn, .25d), new Vector2(102.426407f, 0f),
            new Vector2(-266.572976f, 0f), new Vector2(102.426407f, 0f),
            new Vector2(120f, 0f), .625d, "cross B turnaround");

        object lowerTurn = api.Create(new Vector2(120f, 0f), 1f);
        api.Step(lowerTurn, .875d);
        ExpectFrame(api.Step(lowerTurn, .25d), new Vector2(17.573593f, 0f),
            new Vector2(266.572976f, 0f), Vector2.Zero,
            new Vector2(17.573593f, 0f), .125d, "cross A turnaround and wrap phase");
    }

    private static void SignedAndVerticalTravelKeepCoefficientOrderedSweeps(ModelApi api)
    {
        // Catches absolute-value direction loss and coordinate-sorted sweep endpoints.
        object left = api.Create(new Vector2(-120f, 0f), 1f);
        ExpectFrame(api.Step(left, .25d), new Vector2(-60f, 0f), new Vector2(-376.991118f, 0f),
            Vector2.Zero, new Vector2(-60f, 0f), .25d, "negative horizontal quarter");
        ExpectFrame(api.Step(left, .75d), Vector2.Zero, Vector2.Zero,
            Vector2.Zero, new Vector2(-120f, 0f), 0d, "negative horizontal complete excursion");

        object vertical = api.Create(new Vector2(0f, -80f), 2f);
        ExpectFrame(api.Step(vertical, .5d), new Vector2(0f, -40f), new Vector2(0f, -125.663706f),
            Vector2.Zero, new Vector2(0f, -40f), .25d, "vertical quarter uses authored period");
        ExpectFrame(api.Step(vertical, 1d), new Vector2(0f, -40f), new Vector2(0f, 125.663706f),
            new Vector2(0f, -40f), new Vector2(0f, -80f), .75d, "vertical turnaround keeps signed endpoints");
    }

    private static void DiagonalTravelDoesNotNormalizeAwayItsComponents(ModelApi api)
    {
        object model = api.Create(new Vector2(72f, 96f), 2f);
        ExpectFrame(api.Step(model, .5d), new Vector2(36f, 48f),
            new Vector2(113.097336f, 150.796447f), Vector2.Zero,
            new Vector2(36f, 48f), .25d, "diagonal quarter keeps both authored components");
    }

    private static void ThirtySixtyAndOneTwentyPartitionsReachLiteralHalfAndFullCycles(ModelApi api)
    {
        // Catches per-tick interpolation/integration drift; expectations are not another model run.
        foreach (int ticksPerSecond in new[] { 30, 60, 120 })
        {
            object model = api.Create(new Vector2(120f, 0f), 1f);
            MotionSnapshot half = AdvanceHalfCycle(api, model, ticksPerSecond, "outbound");
            ExpectVector(new Vector2(120f, 0f), half.Offset, PositionTolerance,
                ticksPerSecond + " Hz literal half-cycle position");
            ExpectVector(Vector2.Zero, half.Velocity, VelocityTolerance,
                ticksPerSecond + " Hz half-cycle derivative is zero");
            ExpectPhase(.5d, half.Phase01, ticksPerSecond + " Hz half-cycle phase");

            MotionSnapshot full = AdvanceHalfCycle(api, model, ticksPerSecond, "return");
            ExpectVector(Vector2.Zero, full.Offset, PositionTolerance,
                ticksPerSecond + " Hz literal one-second position");
            ExpectVector(Vector2.Zero, full.Velocity, VelocityTolerance,
                ticksPerSecond + " Hz completed-cycle derivative is zero");
            ExpectPhase(0d, full.Phase01, ticksPerSecond + " Hz completed-cycle phase");
        }
    }

    private static MotionSnapshot AdvanceHalfCycle(ModelApi api, object model, int ticksPerSecond, string leg)
    {
        float minimumX = float.PositiveInfinity;
        float maximumX = float.NegativeInfinity;
        MotionSnapshot frame = default;
        for (int tick = 0; tick < ticksPerSecond / 2; tick++)
        {
            frame = api.Step(model, 1d / ticksPerSecond);
            minimumX = Math.Min(minimumX, frame.SweepFromOffset.X);
            maximumX = Math.Max(maximumX, frame.SweepToOffset.X);
        }
        ExpectNear(0d, minimumX, PositionTolerance, ticksPerSecond + " Hz " + leg + " sweep reaches A");
        ExpectNear(120d, maximumX, PositionTolerance, ticksPerSecond + " Hz " + leg + " sweep reaches B");
        return frame;
    }

    private static void InvalidDeltasDoNotAdvanceOrReuseThePreviousSweep(ModelApi api)
    {
        // Catches treating invalid time as motion, clock poisoning, and stale swept damage.
        object model = api.Create(new Vector2(120f, 0f), 1f);
        api.Step(model, .25d);
        foreach (double delta in new[] { 0d, -0d, -.125d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            MotionSnapshot frame = api.Step(model, delta);
            ExpectVector(new Vector2(60f, 0f), frame.Offset, PositionTolerance,
                "invalid delta leaves the current position unchanged: " + delta);
            ExpectVector(new Vector2(60f, 0f), frame.SweepFromOffset, PositionTolerance,
                "invalid delta starts a degenerate current-position sweep: " + delta);
            ExpectVector(new Vector2(60f, 0f), frame.SweepToOffset, PositionTolerance,
                "invalid delta cannot replay the previous 0..60 sweep: " + delta);
            ExpectPhase(.25d, frame.Phase01, "invalid delta leaves phase unchanged: " + delta);
        }
        ExpectFrame(api.Step(model, .25d), new Vector2(120f, 0f), Vector2.Zero,
            new Vector2(60f, 0f), new Vector2(120f, 0f), .5d, "valid time resumes after invalid inputs");
    }

    private static void ResetDiscardsTheOldClockAndOldSweep(ModelApi api)
    {
        // Catches reset-as-teleport sweeps and a position reset that retains the old phase.
        object model = api.Create(new Vector2(120f, 0f), 1f);
        api.Step(model, .75d);
        api.Reset(model);
        ExpectFrame(api.Step(model, 0d), Vector2.Zero, Vector2.Zero,
            Vector2.Zero, Vector2.Zero, 0d, "reset has no old travel segment");
        ExpectFrame(api.Step(model, .25d), new Vector2(60f, 0f), new Vector2(376.991118f, 0f),
            Vector2.Zero, new Vector2(60f, 0f), .25d, "reset restarts the outbound quarter");
        api.Reset(model);
        api.Reset(model);
        ExpectFrame(api.Step(model, 0d), Vector2.Zero, Vector2.Zero,
            Vector2.Zero, Vector2.Zero, 0d, "repeated reset remains at the initial endpoint");
    }

    private static void InvalidConstructionThrowsWithoutPublishingAPartialModel(ModelApi api)
    {
        // Catches sanitizing a bad authoring value instead of rejecting the whole configuration.
        object live = api.Create(new Vector2(120f, 0f), 1f);
        api.Step(live, .25d);
        var cases = new (Vector2 Travel, float Period, string Label)[]
        {
            (Vector2.Zero, 2f, "zero travel"),
            (new Vector2(float.NaN, 10f), 2f, "NaN travel x"),
            (new Vector2(10f, float.NaN), 2f, "NaN travel y"),
            (new Vector2(float.PositiveInfinity, 0f), 2f, "positive infinite travel x"),
            (new Vector2(float.NegativeInfinity, 0f), 2f, "negative infinite travel x"),
            (new Vector2(0f, float.PositiveInfinity), 2f, "positive infinite travel y"),
            (new Vector2(0f, float.NegativeInfinity), 2f, "negative infinite travel y"),
            (new Vector2(float.MaxValue, float.MaxValue), 20f, "overflow-sized finite travel"),
            (new Vector2(512.01f, 0f), 20f, "travel longer than 512 pixels"),
            (new Vector2(400f, 400f), 20f, "diagonal length exceeds 512 despite smaller components"),
            (new Vector2(120f, 0f), 0f, "zero period"),
            (new Vector2(120f, 0f), -1f, "negative period"),
            (new Vector2(120f, 0f), .999f, "period below one second"),
            (new Vector2(120f, 0f), 20.001f, "period above twenty seconds"),
            (new Vector2(120f, 0f), float.NaN, "NaN period"),
            (new Vector2(120f, 0f), float.PositiveInfinity, "positive infinite period"),
            (new Vector2(120f, 0f), float.NegativeInfinity, "negative infinite period"),
            (new Vector2(200f, 0f), 1f, "628.318531 pixel-per-second peak exceeds 600"),
            (new Vector2(140f, 140f), 1f, "diagonal peak exceeds 600 despite valid component peaks")
        };
        foreach ((Vector2 travel, float period, string label) in cases)
        {
            object published = null;
            Exception failure = null;
            try { published = api.Create(travel, period); }
            catch (TargetInvocationException error) { failure = error.InnerException; }
            TestAssert.True(failure is ArgumentException && published == null,
                "MOVING_GEAR_MODEL_RED: invalid constructor must throw ArgumentException, not publish/clamp: " + label);
        }
        ExpectFrame(api.Step(live, .25d), new Vector2(120f, 0f), Vector2.Zero,
            new Vector2(60f, 0f), new Vector2(120f, 0f), .5d,
            "rejected construction cannot partly reconfigure another live model");
    }

    private static void ValidLengthPeriodAndSpeedBoundariesAreNotOverRejected(ModelApi api)
    {
        object maximumLengthAndPeriod = api.Create(new Vector2(512f, 0f), 20f);
        ExpectFrame(api.Step(maximumLengthAndPeriod, 5d), new Vector2(256f, 0f),
            new Vector2(80.424772f, 0f), Vector2.Zero, new Vector2(256f, 0f), .25d,
            "inclusive 512-pixel and 20-second bounds");

        object nearPeakLimit = api.Create(new Vector2(190f, 0f), 1f);
        ExpectFrame(api.Step(nearPeakLimit, .25d), new Vector2(95f, 0f),
            new Vector2(596.902604f, 0f), Vector2.Zero, new Vector2(95f, 0f), .25d,
            "valid speed below 600 at inclusive one-second minimum");
    }

    private static void ExpectFrame(MotionSnapshot frame, Vector2 offset, Vector2 velocity,
        Vector2 sweepFrom, Vector2 sweepTo, double phase, string label)
    {
        ExpectVector(offset, frame.Offset, PositionTolerance, label + " offset");
        ExpectVector(velocity, frame.Velocity, VelocityTolerance, label + " velocity");
        ExpectVector(sweepFrom, frame.SweepFromOffset, PositionTolerance, label + " sweep from");
        ExpectVector(sweepTo, frame.SweepToOffset, PositionTolerance, label + " sweep to");
        ExpectPhase(phase, frame.Phase01, label + " phase");
    }

    private static void ExpectVector(Vector2 expected, Vector2 actual, double tolerance, string label)
    {
        ExpectNear(expected.X, actual.X, tolerance, label + " x");
        ExpectNear(expected.Y, actual.Y, tolerance, label + " y");
    }

    private static void ExpectNear(double expected, double actual, double tolerance, string label)
    {
        // TestAssert.Near alone would admit NaN because its comparison is false for NaN.
        TestAssert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance,
            $"MOVING_GEAR_MODEL_RED: {label}; expected={expected}, actual={actual}, tolerance={tolerance}");
    }

    private static void ExpectPhase(double expected, double actual, string label)
    {
        // Repeated double steps may finish a cycle just below one rather than exactly zero.
        double distance = Math.Abs(expected - actual);
        TestAssert.True(double.IsFinite(actual) && actual >= 0d && actual <= 1d &&
            (distance <= PhaseTolerance || Math.Abs(1d - distance) <= PhaseTolerance),
            $"MOVING_GEAR_MODEL_RED: {label}; expected cyclic phase={expected}, actual={actual}");
    }

    private readonly struct MotionSnapshot
    {
        public readonly object Source;
        public readonly Vector2 Offset;
        public readonly Vector2 Velocity;
        public readonly Vector2 SweepFromOffset;
        public readonly Vector2 SweepToOffset;
        public readonly double Phase01;

        public MotionSnapshot(object source, Vector2 offset, Vector2 velocity, Vector2 sweepFrom,
            Vector2 sweepTo, double phase)
        {
            Source = source;
            Offset = offset;
            Velocity = velocity;
            SweepFromOffset = sweepFrom;
            SweepToOffset = sweepTo;
            Phase01 = phase;
        }
    }

    private sealed class ModelApi
    {
        private readonly ConstructorInfo _constructor;
        private readonly MethodInfo _step;
        private readonly MethodInfo _reset;
        private readonly PropertyInfo _offset;
        private readonly PropertyInfo _velocity;
        private readonly PropertyInfo _sweepFrom;
        private readonly PropertyInfo _sweepTo;
        private readonly PropertyInfo _phase;

        public ModelApi(Type modelType)
        {
            _constructor = modelType.GetConstructor(new[] { typeof(Vector2), typeof(float) });
            _step = modelType.GetMethod("Step", BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(double) }, null);
            _reset = modelType.GetMethod("Reset", BindingFlags.Public | BindingFlags.Instance,
                null, Type.EmptyTypes, null);
            TestAssert.True(_constructor != null && _step != null && _reset != null && _step.ReturnType != typeof(void),
                "MOVING_GEAR_MODEL_RED: public constructor(Vector2,float), Step(double):frame, Reset() contract");

            Type frameType = _step.ReturnType;
            _offset = VectorProperty(frameType, "Offset");
            _velocity = VectorProperty(frameType, "Velocity");
            _sweepFrom = VectorProperty(frameType, "SweepFromOffset");
            _sweepTo = VectorProperty(frameType, "SweepToOffset");
            _phase = frameType.GetProperty("Phase01", BindingFlags.Public | BindingFlags.Instance);
            TestAssert.True(_phase?.GetMethod?.IsPublic == true &&
                (_phase.PropertyType == typeof(float) || _phase.PropertyType == typeof(double)),
                "MOVING_GEAR_MODEL_RED: frame exposes a public numeric Phase01 property");
        }

        public object Create(Vector2 travel, float period) => _constructor.Invoke(new object[] { travel, period });

        public MotionSnapshot Step(object model, double delta) => Read(_step.Invoke(model, new object[] { delta }));

        public void Reset(object model) => _reset.Invoke(model, null);

        public MotionSnapshot Read(object frame)
        {
            TestAssert.True(frame != null, "MOVING_GEAR_MODEL_RED: Step returns an actual motion frame");
            Vector2 offset = (Vector2)_offset.GetValue(frame);
            Vector2 velocity = (Vector2)_velocity.GetValue(frame);
            Vector2 from = (Vector2)_sweepFrom.GetValue(frame);
            Vector2 to = (Vector2)_sweepTo.GetValue(frame);
            double phase = Convert.ToDouble(_phase.GetValue(frame));
            TestAssert.True(Finite(offset) && Finite(velocity) && Finite(from) && Finite(to) && double.IsFinite(phase),
                "MOVING_GEAR_MODEL_RED: every returned frame component remains finite");
            return new MotionSnapshot(frame, offset, velocity, from, to, phase);
        }

        private static PropertyInfo VectorProperty(Type frameType, string name)
        {
            PropertyInfo property = frameType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            TestAssert.True(property?.GetMethod?.IsPublic == true && property.PropertyType == typeof(Vector2),
                "MOVING_GEAR_MODEL_RED: frame exposes a public Vector2 " + name + " property");
            return property;
        }

        private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    }
}
