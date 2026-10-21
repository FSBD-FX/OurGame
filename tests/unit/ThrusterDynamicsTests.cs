using System;
using System.Reflection;
using Godot;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class ThrusterDynamicsTests
{
    private static Type _inputType;
    private static MethodInfo _step;

    public static void Run()
    {
        Type core = typeof(MobilityModuleSettings).Assembly.GetType("Knight.Player.ThrusterDynamics");
        TestAssert.True(core != null, "Thruster dynamics is unavailable");
        _inputType = core.Assembly.GetType("Knight.Player.ThrusterDynamicsInput");
        TestAssert.True(_inputType != null, "Thruster dynamics input is unavailable");
        _step = core.GetMethod("Step", new[] { _inputType, typeof(MobilityModuleSettings),
            typeof(PlayerMotionSettings), typeof(double) });
        TestAssert.True(_step != null, "Thruster dynamics Step boundary is unavailable");
        FinitePulseUsesOnlyRemainingTime();
        ProjectionLimitDoesNotReplaceIncomingVelocity();
        DiagonalDirectionIsNormalized();
        ZeroDeltaDoesNotConsumeBurn();
        GravityRunsThroughPulseTail();
        ProjectionUsesVelocityAfterGravity();
        TerminalCapDoesNotInventThrust();
        PassiveDragIsGroundOrAirSpecific();
        HorizontalBurnAppliesDragBeforeThrustWithoutRunDrive();
        ReverseBrakingUsesOnlyItsTimeToStop();
        SameDirectionOverspeedDoesNotSnapToRunSpeed();
        OrdinaryDriveDoesNotDoubleApplyPassiveDrag();
        BurningReverseControlsDistinguishNozzleAndVelocity();
        InvalidInputsAreRejected();
        LargeFiniteDirectionNormalizesSafely();
        FixedTimeStepsConvergeAgainstIndependentPhysics();
        BrakingMatchesUnsaturatedTimeSamples();
        GD.Print("THRUSTER_DYNAMICS_CASES_OK finite_pulse=true gravity=true exclusive_controls=true reverse_arbitration=true invalid_values_rejected=true convergence_30_60_120=true brake_unsaturated_30_60_120=true independent_values=true player_wired=false");
    }

    private static MobilityModuleSettings NoPassiveDrag =>
        MobilityModuleSettings.Standard with { GroundFriction = 0f, AirDragRate = 0f };

    private static void FinitePulseUsesOnlyRemainingTime()
    {
        object result = Step(new Vector2(100f, 0f), 0f, false, Vector2.Right,
            .005f, false, .1, NoPassiveDrag);
        Near(128f, Velocity(result).X, .0001f, "only 0.005s of thrust is integrated");
        Near(0f, Get<float>(result, "BurnRemaining"), .000001f, "partial final pulse ends");
        Near(.005f, Get<float>(result, "BurnUsedSeconds"), .000001f, "effective burn duration is reported");
        Near(28f, Get<Vector2>(result, "ThrustVelocityDelta").X, .0001f, "thrust contribution is not total velocity");

        object full = Step(Vector2.Zero, 0f, false, Vector2.Right,
            .18f, false, .18, NoPassiveDrag);
        Near(850f, Velocity(full).X, .001f, "finite pulse reaches the powered projection limit, not fixed dash speed");
    }

    private static void ProjectionLimitDoesNotReplaceIncomingVelocity()
    {
        object result = Step(new Vector2(1000f, -900f), 0f, false, Vector2.Right,
            .18f, false, .1, NoPassiveDrag);
        Near(1000f, Velocity(result).X, .001f, "incoming parallel overspeed is not truncated");
        Near(-900f, Velocity(result).Y, .001f, "unrelated upward component is not normalized away");
        Near(0f, Get<Vector2>(result, "ThrustVelocityDelta").Length(), .001f, "limit stops new force rather than replacing momentum");
    }

    private static void DiagonalDirectionIsNormalized()
    {
        object result = Step(Vector2.Zero, 0f, false, new Vector2(1f, -1f),
            .18f, false, .1, NoPassiveDrag);
        Near(395.9798f, Velocity(result).X, .001f, "diagonal thrust has normalized horizontal component");
        Near(-395.9798f, Velocity(result).Y, .001f, "diagonal thrust has normalized vertical component");
        Near(560f, Get<Vector2>(result, "ThrustVelocityDelta").Length(), .001f, "diagonal does not get sqrt-two extra acceleration");
    }

    private static void ZeroDeltaDoesNotConsumeBurn()
    {
        object result = Step(new Vector2(321f, -123f), 0f, false, Vector2.Right,
            .05f, false, 0.0, NoPassiveDrag);
        Near(321f, Velocity(result).X, .0001f, "zero delta keeps horizontal momentum");
        Near(-123f, Velocity(result).Y, .0001f, "zero delta keeps vertical momentum");
        Near(.05f, Get<float>(result, "BurnRemaining"), .000001f, "zero delta keeps burn budget");
        Near(0f, Get<float>(result, "BurnUsedSeconds"), .000001f, "zero delta applies no thrust time");
    }

    private static void GravityRunsThroughPulseTail()
    {
        object result = Step(new Vector2(100f, 0f), 0f, false, Vector2.Right,
            .005f, true, .1, NoPassiveDrag);
        Near(128f, Velocity(result).X, .0001f, "partial pulse still adds only 28 horizontal speed");
        Near(185f, Velocity(result).Y, .001f, "gravity applies for the whole tick, not just the burning fraction");
        Near(185f, Get<Vector2>(result, "GravityVelocityDelta").Y, .001f, "gravity has an independent reported contribution");
        object floor = Step(Vector2.Zero, 0f, true, Vector2.Right,
            .1f, true, .1, NoPassiveDrag);
        Near(0f, Velocity(floor).Y, .001f, "standing floor does not integrate free gravity");
    }

    private static void ProjectionUsesVelocityAfterGravity()
    {
        object result = Step(new Vector2(600f, -600f), 0f, false, new Vector2(1f, -1f),
            .18f, true, .1, NoPassiveDrag);
        // Before thrust: (600,-415), parallel component 1015/sqrt(2).
        // Remaining projection contributes 850/sqrt(2)-1015/2 per axis.
        Near(693.5408f, Velocity(result).X, .002f, "powered limit is sampled after gravity");
        Near(-508.5408f, Velocity(result).Y, .002f, "diagonal gravity ordering preserves the literal result");
    }

    private static void TerminalCapDoesNotInventThrust()
    {
        object down = Step(new Vector2(0f, 740f), 0f, false, Vector2.Down,
            .18f, true, .1, NoPassiveDrag);
        Near(780f, Velocity(down).Y, .001f, "downward jet cannot bypass ordinary terminal fall speed");
        Near(40f, Get<Vector2>(down, "GravityVelocityDelta").Y, .001f, "terminal-clipped unpowered gravity is reported separately");
        Near(0f, Get<Vector2>(down, "ThrustVelocityDelta").Y, .001f, "terminal clamp must not count as useful downward thrust");
        object up = Step(new Vector2(0f, 740f), 0f, false, Vector2.Up,
            .18f, true, .1, NoPassiveDrag);
        Near(365f, Velocity(up).Y, .001f, "upward thrust acts on pre-cap gravity speed before terminal clamp");
        Near(40f, Get<Vector2>(up, "GravityVelocityDelta").Y, .001f, "baseline cap attribution is consistent");
        Near(-415f, Get<Vector2>(up, "ThrustVelocityDelta").Y, .001f, "reported thrust is the actual post-cap difference from unpowered motion");
    }

    private static void PassiveDragIsGroundOrAirSpecific()
    {
        object air = Step(new Vector2(600f, 0f), 0f, false, Vector2.Right,
            0f, false, .1, MobilityModuleSettings.Standard);
        Near(511.2863f, Velocity(air).X, .002f, "air neutral momentum follows exponential drag");
        TestAssert.True(!Get<bool>(air, "Braking"), "passive drag is not active braking");
        object ground = Step(new Vector2(600f, 0f), 0f, true, Vector2.Right,
            0f, false, .1, MobilityModuleSettings.Standard);
        Near(420f, Velocity(ground).X, .001f, "ground neutral momentum follows finite friction");
    }

    private static void HorizontalBurnAppliesDragBeforeThrustWithoutRunDrive()
    {
        object result = Step(new Vector2(600f, 0f), 1f, false, Vector2.Right,
            .18f, false, .1, MobilityModuleSettings.Standard with { PoweredProjectionLimit = 2000f });
        Near(1071.2863f, Velocity(result).X, .002f, "horizontal burn has one passive drag and one thrust, no ordinary run drive");
        Near(560f, Get<Vector2>(result, "ThrustVelocityDelta").X, .001f, "drag is not included in reported thrust");
    }

    private static void ReverseBrakingUsesOnlyItsTimeToStop()
    {
        object air = Step(new Vector2(600f, 0f), -1f, false, Vector2.Right,
            0f, false, .1, MobilityModuleSettings.Standard);
        Near(300f, Velocity(air).X, .001f, "opposite air input uses active brake without extra passive drag");
        TestAssert.True(Get<bool>(air, "Braking"), "actual active brake is observable");
        object ground = Step(new Vector2(600f, 0f), -1f, true, Vector2.Right,
            0f, false, .1, MobilityModuleSettings.Standard);
        Near(200f, Velocity(ground).X, .001f, "ground reverse brake is distinct from friction");
        object crossing = Step(new Vector2(600f, 0f), -1f, false, Vector2.Right,
            0f, false, .25, MobilityModuleSettings.Standard);
        Near(-75f, Velocity(crossing).X, .001f, "after 0.2s braking only 0.05s ordinary reverse acceleration remains");
    }

    private static void SameDirectionOverspeedDoesNotSnapToRunSpeed()
    {
        object result = Step(new Vector2(600f, 0f), 1f, false, Vector2.Right,
            0f, false, .1, MobilityModuleSettings.Standard);
        Near(511.2863f, Velocity(result).X, .002f, "same-direction overspeed decays passively, not towards 260 by run acceleration");
        object settled = Step(new Vector2(600f, 0f), 1f, false, Vector2.Right,
            0f, false, 1.0, MobilityModuleSettings.Standard);
        Near(260f, Velocity(settled).X, .001f, "ordinary drive takes over after passive decay actually reaches run speed");
    }

    private static void OrdinaryDriveDoesNotDoubleApplyPassiveDrag()
    {
        object air = Step(new Vector2(200f, 0f), 1f, false, Vector2.Right,
            0f, false, .01, MobilityModuleSettings.Standard);
        Near(215f, Velocity(air).X, .001f, "below run speed ordinary air drive is a single mutually-exclusive branch");
        object ground = Step(Vector2.Zero, 1f, true, Vector2.Right,
            0f, false, .1, MobilityModuleSettings.Standard);
        Near(220f, Velocity(ground).X, .001f, "ordinary grounded acceleration stays 2200");
    }

    private static void BurningReverseControlsDistinguishNozzleAndVelocity()
    {
        object cancelled = Step(new Vector2(600f, 0f), -1f, false, Vector2.Right,
            .18f, false, .1, NoPassiveDrag);
        Near(300f, Velocity(cancelled).X, .001f, "later reverse input cancels old right nozzle before braking");
        TestAssert.True(Get<bool>(cancelled, "CancelledForReverse"), "opposing locked nozzle reports cancellation");
        Near(0f, Get<float>(cancelled, "BurnRemaining"), .000001f, "cancelled pulse cannot resume");
        Near(0f, Get<float>(cancelled, "BurnUsedSeconds"), .000001f, "cancel frame adds no old thrust");
        object counter = Step(new Vector2(600f, 0f), -1f, false, Vector2.Left,
            .18f, false, .1, NoPassiveDrag);
        Near(40f, Velocity(counter).X, .001f, "initial counter-thrust is not cancelled or double-braked");
        TestAssert.True(!Get<bool>(counter, "CancelledForReverse"), "input matching nozzle keeps counter-thrust");
        object vertical = Step(new Vector2(600f, 0f), -1f, false, Vector2.Up,
            .18f, false, .1, NoPassiveDrag);
        Near(300f, Velocity(vertical).X, .001f, "vertical nozzle allows horizontal reverse braking");
        Near(-560f, Velocity(vertical).Y, .001f, "horizontal braking does not cancel vertical thrust");
        TestAssert.True(!Get<bool>(vertical, "CancelledForReverse"), "pure vertical nozzle remains active");
    }

    private static void InvalidInputsAreRejected()
    {
        foreach (double delta in new[] { -.01, double.NaN, double.PositiveInfinity,
                     double.NegativeInfinity, double.MaxValue })
            Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Right, .18f, false,
                delta, NoPassiveDrag), "delta");
        foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Reject(() => Step(new Vector2(value, 0f), 0f, false, Vector2.Right, .18f,
                false, .1, NoPassiveDrag), "Velocity");
            Reject(() => Step(new Vector2(0f, value), 0f, false, Vector2.Right, .18f,
                false, .1, NoPassiveDrag), "Velocity");
            Reject(() => Step(Vector2.Zero, value, false, Vector2.Right, .18f,
                false, .1, NoPassiveDrag), "MoveAxis");
            Reject(() => Step(Vector2.Zero, 0f, false, new Vector2(value, 0f), .18f,
                false, .1, NoPassiveDrag), "BurnDirection");
            Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Right, value,
                false, .1, NoPassiveDrag), "BurnRemaining");
        }
        foreach (float axis in new[] { -1.01f, 1.01f })
            Reject(() => Step(Vector2.Zero, axis, false, Vector2.Right, .18f, false,
                .1, NoPassiveDrag), "MoveAxis");
        foreach (float remaining in new[] { -.01f, .181f })
            Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Right, remaining, false,
                .1, NoPassiveDrag), "BurnRemaining");
        Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Zero, .18f, false,
            .1, NoPassiveDrag), "BurnDirection");
        Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Right, .18f, false,
            0.0, NoPassiveDrag with { ThrustAcceleration = float.NaN }), "ThrustAcceleration");
        Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Right, .18f, false,
            .1, NoPassiveDrag with { ModuleId = " " }), "ModuleId");
        foreach (PlayerMotionSettings motion in new[] {
            new PlayerMotionSettings(MaxRunSpeed: 0f),
            new PlayerMotionSettings(GroundAcceleration: float.NaN),
            new PlayerMotionSettings(AirAcceleration: -1f),
            new PlayerMotionSettings(Gravity: float.PositiveInfinity),
            new PlayerMotionSettings(MaxFallSpeed: 0f) })
            Reject(() => Step(Vector2.Zero, 0f, false, Vector2.Right, .18f, false,
                .1, NoPassiveDrag, motion), "motion");
        object valid = Activator.CreateInstance(_inputType,
            new object[] { Vector2.Zero, 0f, false, Vector2.Right, .18f, false });
        Reject(() => _step.Invoke(null, new object[] { valid, NoPassiveDrag, null, .1 }), "motion");
        object inactive = Step(new Vector2(100f, 0f), 0f, false, Vector2.Zero, 0f,
            true, .1, NoPassiveDrag, new PlayerMotionSettings(Gravity: 0f));
        Near(100f, Velocity(inactive).X, .001f, "inactive zero direction and zero gravity are legitimate");
    }

    private static void LargeFiniteDirectionNormalizesSafely()
    {
        object result = Step(Vector2.Zero, 0f, false, new Vector2(float.MaxValue, 0f),
            .18f, false, .1, NoPassiveDrag);
        Near(560f, Velocity(result).X, .001f, "finite direction magnitude must not overflow normalization into a zero jet");
    }

    private static void FixedTimeStepsConvergeAgainstIndependentPhysics()
    {
        float previousError = float.PositiveInfinity;
        foreach (int hz in new[] { 30, 60, 120 })
        {
            Vector2 velocity = Vector2.Zero;
            float remaining = .18f, burn = 0f;
            MobilityModuleSettings unlimited = NoPassiveDrag with { PoweredProjectionLimit = 2000f };
            for (int frame = 0; frame < hz; frame++)
            {
                object result = Step(velocity, 0f, false, Vector2.Right, remaining,
                    false, 1.0 / hz, unlimited);
                velocity = Velocity(result);
                remaining = Get<float>(result, "BurnRemaining");
                burn += Get<float>(result, "BurnUsedSeconds");
            }
            Near(1008f, velocity.X, .003f, hz + "Hz integrates exactly a*0.18, not a whole extra tick");
            Near(.18f, burn, .000001f, hz + "Hz consumes the same finite pulse");
            Near(0f, remaining, .000001f, hz + "Hz pulse is exhausted");

            velocity = new Vector2(600f, 0f);
            for (int frame = 0; frame < hz; frame++)
                velocity = Velocity(Step(velocity, 0f, false, Vector2.Zero, 0f,
                    false, 1.0 / hz, MobilityModuleSettings.Standard));
            Near(121.1379f, velocity.X, .003f, hz + "Hz neutral drag matches 600*exp(-1.6)");

            velocity = new Vector2(600f, 0f);
            for (int frame = 0; frame < hz / 2; frame++)
                velocity = Velocity(Step(velocity, -1f, false, Vector2.Zero, 0f,
                    false, 1.0 / hz, MobilityModuleSettings.Standard));
            Near(-260f, velocity.X, .003f, hz + "Hz finite brake then ordinary reverse reaches the unchanged run limit");

            // Independent continuous ODE solution during the 0.18s burn:
            // dv/dt = 5600 - 1.6*v, then neutral dv/dt = -1.6*v until 1s.
            // Discrete contract is drag then thrust; its positive error must converge.
            velocity = Vector2.Zero;
            remaining = .18f;
            for (int frame = 0; frame < hz; frame++)
            {
                object result = Step(velocity, 0f, false, Vector2.Right, remaining,
                    false, 1.0 / hz, MobilityModuleSettings.Standard with { PoweredProjectionLimit = 2000f });
                velocity = Velocity(result);
                remaining = Get<float>(result, "BurnRemaining");
            }
            float analytic = (float)((5600.0 / 1.6) * (1.0 - Math.Exp(-1.6 * .18)) * Math.Exp(-1.6 * .82));
            float error = Math.Abs(velocity.X - analytic);
            TestAssert.True(float.IsFinite(error) && error < previousError && error < 10f,
                hz + "Hz drag/thrust error must shrink against the independent continuous reference");
            GD.Print(FormattableString.Invariant($"THRUSTER_CONVERGENCE hz={hz} final_x={velocity.X:F6} analytic_x={analytic:F6} absolute_error={error:F6} human=false"));
            previousError = error;
        }
    }

    private static void BrakingMatchesUnsaturatedTimeSamples()
    {
        foreach (int hz in new[] { 30, 60, 120 })
        foreach (bool ground in new[] { false, true })
        foreach (double time in new[] { .1, .25 })
        {
            Vector2 velocity = new(600f, 0f);
            double tick = 1.0 / hz;
            int wholeSteps = (int)Math.Floor(time * hz);
            for (int step = 0; step < wholeSteps; step++)
                velocity = Velocity(Step(velocity, -1f, ground, Vector2.Zero, 0f,
                    false, tick, MobilityModuleSettings.Standard));
            double tail = time - wholeSteps * tick;
            if (tail > 1e-12)
                velocity = Velocity(Step(velocity, -1f, ground, Vector2.Zero, 0f,
                    false, tail, MobilityModuleSettings.Standard));
            // Independent piecewise solution: finite brake to zero, then ordinary
            // acceleration for only the remaining time (no passive drag branch).
            double brake = ground ? 4000.0 : 3000.0;
            double acceleration = ground ? 2200.0 : 1500.0;
            double stoppingTime = 600.0 / brake;
            float expected = (float)(time <= stoppingTime ? 600.0 - brake * time
                : -Math.Min(260.0, acceleration * (time - stoppingTime)));
            Near(expected, velocity.X, .003f, hz + "Hz unsaturated braking matches the independent time split");
            GD.Print(FormattableString.Invariant($"THRUSTER_BRAKE_TIME_SAMPLE hz={hz} ground={ground} time={time:F3} actual_x={velocity.X:F6} expected_x={expected:F6} fractional_tail={tail:F9} human=false"));
        }
    }

    private static void Reject(Action action, string name)
    {
        Exception rejected = null;
        try { action(); }
        catch (TargetInvocationException exception) { rejected = exception.InnerException; }
        catch (ArgumentException exception) { rejected = exception; }
        TestAssert.True(rejected is ArgumentException && rejected.Message.Contains(name, StringComparison.Ordinal),
            "invalid " + name + " must produce a named ArgumentException rather than silently repair or consume input");
    }

    private static object Step(Vector2 velocity, float moveAxis, bool onFloor, Vector2 direction,
        float remaining, bool gravity, double delta, MobilityModuleSettings module,
        PlayerMotionSettings motion = null)
    {
        object input = Activator.CreateInstance(_inputType,
            new object[] { velocity, moveAxis, onFloor, direction, remaining, gravity });
        return _step.Invoke(null, new object[] { input, module, motion ?? new PlayerMotionSettings(), delta });
    }

    private static T Get<T>(object result, string field) =>
        (T)(result.GetType().GetProperty(field)?.GetValue(result)
        ?? throw new InvalidOperationException("Missing dynamics result " + field));
    private static Vector2 Velocity(object result) => Get<Vector2>(result, "Velocity");
    private static void Near(float expected, float actual, float tolerance, string message)
    {
        TestAssert.True(float.IsFinite(actual), message + " must be finite");
        TestAssert.Near(expected, actual, tolerance, message);
    }
}
