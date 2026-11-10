using System;
using System.Reflection;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class FreeMotionForecastTests
{
    public static void Run()
    {
        Type type = typeof(FreeMotionForecastTests).Assembly.GetType("Knight.Tests.Presentation.FreeMotionForecast");
        TestAssert.True(type != null, "shared actual-module discrete horizontal forecast is unavailable");
        var motion = new PlayerMotionSettings();
        MobilityModuleSettings module = MobilityModuleSettings.Standard;
        object Forecast(string method, float speed, bool floor, int ticks, MobilityModuleSettings settings)
        {
            MethodInfo entry = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            TestAssert.True(entry != null, "shared forecast entry is available: " + method);
            return entry.Invoke(null, new object[] { speed, floor, motion, settings, 1d / 60d, ticks, .001f });
        }
        float Float(object value, string name) => (float)value.GetType().GetProperty(name).GetValue(value);
        int Int(object value, string name) => (int)value.GetType().GetProperty(name).GetValue(value);
        bool Bool(object value, string name) => (bool)value.GetType().GetProperty(name).GetValue(value);
        object air = Forecast("Neutral", 600f, false, 15, module);
        TestAssert.Near(402.19203f, Float(air, "Velocity"), .004f, "independent air exponential end speed");
        TestAssert.Near(121.98890f, Float(air, "NetDistance"), .004f, "independent finite geometric post-step displacement");
        TestAssert.True(Int(air, "Steps") == 15 && !Bool(air, "SpeedThresholdReached"),
            "finite airborne neutral budget does not pretend exact rest");
        float oldSpeed = 600f, oldDistance = 0f;
        for (int tick = 0; tick < 15; tick++)
        {
            oldSpeed = Math.Max(0f, oldSpeed - 2800f / 60f);
            oldDistance += oldSpeed / 60f;
        }
        TestAssert.True(Float(air, "NetDistance") is > 115f and < 130f && oldDistance < 115f,
            "literal landing band rejects old 2800 prediction, not just a self-comparison");
        object floor = Forecast("Neutral", 600f, true, 20, module);
        TestAssert.Near(0f, Float(floor, "Velocity"), .004f, "ground module friction reaches rest");
        TestAssert.Near(95f, Float(floor, "NetDistance"), .004f, "ground discrete displacement is not continuous 100");
        TestAssert.True(Bool(floor, "SpeedThresholdReached") && Int(floor, "Steps") == 20,
            "ground neutral reports real threshold within original tick budget");
        object reverse = Forecast("Reverse", 620f, false, 13, module);
        TestAssert.Near(-15f, Float(reverse, "Velocity"), .004f, "reverse crossing consumes remainder with ordinary acceleration");
        TestAssert.Near(59f, Float(reverse, "ForwardDistance"), .004f, "native whole-tick peak forward displacement");
        TestAssert.Near(58.75f, Float(reverse, "NetDistance"), .004f, "cross-zero final native displacement differs from peak");
        TestAssert.True(Bool(reverse, "BrakeReachedZero") && Int(reverse, "Steps") == 13,
            "reverse completion uses first zero crossing, not exact final zero speed");
        object left = Forecast("Reverse", -620f, false, 13, module);
        TestAssert.Near(-58.75f, Float(left, "NetDistance"), .004f, "left reverse is signed net displacement");
        TestAssert.Near(59f, Float(left, "ForwardDistance"), .004f, "left reverse peak remains positive in original travel direction");
        object noDrag = Forecast("Neutral", 600f, false, 15, module with { AirDragRate = 0f });
        TestAssert.Near(600f, Float(noDrag, "Velocity"), .004f, "actual nondefault zero-drag module is not replaced by Standard");
        TestAssert.Near(150f, Float(noDrag, "NetDistance"), .004f, "zero drag uses finite constant velocity displacement");
        TestAssert.True(!Bool(noDrag, "SpeedThresholdReached"), "zero drag never reports passive rest");
        object noTicks = Forecast("Neutral", 600f, false, 0, module);
        TestAssert.True(Int(noTicks, "Steps") == 0 && Float(noTicks, "Velocity") == 600f &&
            Float(noTicks, "NetDistance") == 0f && !Bool(noTicks, "SpeedThresholdReached"), "empty budget never manufactures motion or rest");
        MethodInfo heldEntry = type.GetMethod("HeldThenNeutral", BindingFlags.Public | BindingFlags.Static);
        TestAssert.True(heldEntry != null,
            "Gear support forecast includes both directional samples through K consumption, not one step then neutral");
        object Held(float speed, float axis, int heldSamples, int budget) => heldEntry.Invoke(null,
            new object[] { speed, axis, true, motion, module, 1d / 60d, heldSamples, budget, .001f });
        object heldRight = Held(36.666667f, 1f, 2, 20);
        TestAssert.Near(5.555556f, Float(heldRight, "NetDistance"), .004f,
            "two real right samples and discrete neutral stopping need 5.5556px, not old 2.1667px");
        TestAssert.True(Bool(heldRight, "SpeedThresholdReached") && Int(heldRight, "Steps") == 6 &&
            Float(heldRight, "NetDistance") > 3f,
            "three-pixel edge clearance rejects the two-sample handoff that old one-step forecast admitted");
        object heldLeft = Held(-36.666667f, -1f, 2, 20);
        TestAssert.Near(-5.555556f, Float(heldLeft, "NetDistance"), .004f,
            "left handoff uses signed displacement and identical retained-input count");
        object exactHeldBudget = Held(36.666667f, 1f, 2, 2);
        TestAssert.True(Int(exactHeldBudget, "Steps") == 2 && !Bool(exactHeldBudget, "SpeedThresholdReached"),
            "held samples consume the existing total budget rather than adding a neutral window");
        object zeroHeld = Held(600f, 0f, 0, 20);
        TestAssert.Near(95f, Float(zeroHeld, "NetDistance"), .004f,
            "zero retained samples preserve original neutral forecast");
        object initiallyStill = Held(0f, 1f, 2, 20);
        TestAssert.Near(2.777778f, Float(initiallyStill, "NetDistance"), .004f,
            "initial rest does not skip pending directional samples");
        bool rejectedOverBudget = false;
        try { Held(36.666667f, 1f, 3, 2); }
        catch (TargetInvocationException error) when (error.InnerException is ArgumentOutOfRangeException)
        { rejectedOverBudget = true; }
        TestAssert.True(rejectedOverBudget, "retained samples cannot exceed the caller's original total budget");
        object heldReversal = Held(200f, -1f, 5, 20);
        TestAssert.Near(.555556f, Float(heldReversal, "NetDistance"), .004f,
            "reversing handoff's final position is not its initial-direction peak");
        TestAssert.Near(3.333333f, Float(heldReversal, "ForwardDistance"), .004f,
            "one-pixel edge clearance must reject 3.3333px transient excursion despite .5556px net stopping distance");
        Godot.GD.Print("THRUSTER_FREE_FORECAST_OK actual_snapshots=true independent_geometric_sum=true ground_discrete_sum=true reverse_crossing=true peak_and_net_distinct=true old_2800_negative=true zero_drag=true no_motion_owner=true human=false");
    }
}
