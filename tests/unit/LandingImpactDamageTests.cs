using System;
using System.Reflection;
using Godot;
using Knight.Player;
using Knight.Config;

namespace Knight.Tests.Unit;

public static class LandingImpactDamageTests
{
    private static Func<float, float, bool, float, float, float, float, int, int> _damage;

    public static void Run()
    {
        MethodInfo formula = typeof(PlayerFallDamageModel).GetMethod("CalculateImpactDamage",
            BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(float), typeof(float), typeof(bool), typeof(float), typeof(float),
                typeof(float), typeof(float), typeof(int) }, null);
        TestAssert.True(formula != null, "Landing impact damage formula is unavailable");
        _damage = formula.CreateDelegate<Func<float, float, bool, float, float, float, float, int, int>>();
        LiteralDamageValues();
        UnreliableMeasurementsRetainHeightDamage();
        WideHeightIsScaledBeforeFinalCap();
        ConfigurationIsExplicitAndValidated();
        RelativeNormalMotionRejectsInvalidMeasurements();
        ConsumptionIsHeightCompatibleAndOnce();
        PlayerFallDamageModelTests.Run();
        GD.Print("THRUSTER_IMPACT_FORMULA_OK wide_height_before_cap=true invalid_measurement_fallback=true independent_values=true player_wired=false");
    }

    private static int Damage(float height, float impactSpeed, bool reliable = true,
        int maximumDamage = 5) => _damage(height, impactSpeed, reliable, 600f, 400f,
            300f, 780f, maximumDamage);

    private static void LiteralDamageValues()
    {
        TestAssert.Equal(1, Damage(800f, 780f), "terminal impact retains one height damage");
        TestAssert.Equal(0, Damage(800f, 300f), "safe relative normal speed prevents height damage");
        TestAssert.Equal(0, Damage(800f, 0f), "reliable zero relative impact is safe");
        TestAssert.Equal(1, Damage(800f, 301f), "positive fractional damage rounds up at settlement");
        TestAssert.Equal(1, Damage(800f, 540f), "one height damage times one quarter rounds up");
        TestAssert.Equal(1, Damage(1400f, 540f), "two height damage times one quarter rounds up");
        TestAssert.Equal(0, Damage(600f, 1000f), "impact cannot invent damage below height threshold");
        TestAssert.Equal(0, Damage(100f, 1000f), "short fall remains safe at high speed");
        TestAssert.Equal(2, Damage(1400f, 1000f), "speed above reference does not amplify height damage");
        TestAssert.Equal(0, Damage(800f, 780f, maximumDamage: 0), "zero damage capacity remains zero");
        TestAssert.Equal(0, Damage(800f, 780f, maximumDamage: -1), "negative damage capacity remains zero");
    }

    private static void UnreliableMeasurementsRetainHeightDamage()
    {
        TestAssert.Equal(1, Damage(800f, 780f, false), "unreliable terminal sample uses old height");
        TestAssert.Equal(2, Damage(1400f, 0f, false), "missing measurement is not zero-speed safety");
        foreach (float speed in new[] { float.NaN, float.PositiveInfinity,
                     float.NegativeInfinity, -1f })
            TestAssert.Equal(2, Damage(1400f, speed), "invalid speed falls back to height: " + speed);
        TestAssert.Equal(5, Damage(4600f, float.NaN), "invalid sample preserves old final height cap");
        TestAssert.Equal(0, Damage(float.NaN, 780f), "unknown height keeps old zero damage semantics");
        TestAssert.Equal(0, Damage(float.NegativeInfinity, 780f), "negative infinite height cannot cause damage");
    }

    private static void WideHeightIsScaledBeforeFinalCap()
    {
        TestAssert.Equal(3, Damage(4600f, 540f), "ten height damage times quarter is three, not capped-base two");
        TestAssert.Equal(5, Damage(4600f, 780f), "only final requested damage is HP-capped");
        TestAssert.Equal(0, Damage(4600f, 300f), "wide base times safe-speed factor is zero");
        TestAssert.Equal(5, Damage(float.MaxValue, 540f), "wide finite height cannot overflow integer conversion");
        TestAssert.Equal(int.MaxValue, Damage(float.MaxValue, 540f, maximumDamage: int.MaxValue),
            "maximum integer capacity is applied after wide multiplication");
        TestAssert.Equal(0, Damage(float.PositiveInfinity, 300f), "zero factor is resolved before infinity times zero");
        TestAssert.Equal(5, Damage(float.PositiveInfinity, 540f), "positive factor with infinite base final-caps safely");
        TestAssert.Equal(5, Damage(float.PositiveInfinity, 0f, false), "unreliable infinite fall retains old capped height damage");
    }

    private static void ConfigurationIsExplicitAndValidated()
    {
        TestAssert.Equal(2, _damage(1400f, 540f, true, 600f, 400f, 300f, 600f, 5),
            "actual configured reference speed replaces default 780");
        TestAssert.Equal(1, _damage(800f, 450f, true, 200f, 300f, 100f, 800f, 5),
            "height and safe speed settings are both consumed explicitly");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity,
                     float.NegativeInfinity, -1f })
        {
            Reject(() => _damage(800f, 780f, true, invalid, 400f, 300f, 780f, 5), "safeHeight");
            Reject(() => _damage(800f, 780f, true, 600f, invalid, 300f, 780f, 5), "heightPerDamage");
            Reject(() => _damage(800f, 780f, true, 600f, 400f, invalid, 780f, 5), "safeImpactSpeed");
            Reject(() => _damage(800f, 780f, true, 600f, 400f, 300f, invalid, 5), "referenceImpactSpeed");
        }
        Reject(() => _damage(800f, 780f, true, 600f, 0f, 300f, 780f, 5), "heightPerDamage");
        Reject(() => _damage(800f, 780f, true, 600f, 400f, 300f, 300f, 5), "referenceImpactSpeed");
        Reject(() => _damage(800f, 780f, true, 600f, 400f, 300f, 299f, 5), "referenceImpactSpeed");
        TestAssert.Equal(1, _damage(800f, 100f, true, 0f, 800f, 0f, 100f, 5),
            "zero safe height and zero safe impact are legal settings");
    }

    private static void Reject(Action action, string parameter)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException exception)
        {
            TestAssert.Equal(parameter, exception.ParamName, "invalid configuration names its boundary");
            return;
        }
        throw new InvalidOperationException("Expected invalid landing configuration rejection: " + parameter);
    }

    public static void RunConfiguration()
    {
        PropertyInfo safety = typeof(PlayerTuning).GetProperty("FallSafeImpactSpeed");
        TestAssert.True(safety != null && safety.PropertyType == typeof(float),
            "Character landing safety speed configuration is unavailable");
        TestAssert.True(safety.GetCustomAttribute<ExportAttribute>() != null,
            "character landing safety speed is an authored resource field");
        using var tuning = new PlayerTuning();
        TestAssert.Near(300f, (float)safety.GetValue(tuning), 0f,
            "default safety impact speed is 300");
        TestAssert.Near(780f, tuning.MaxFallSpeed, 0f, "terminal speed remains unchanged");
        TestAssert.Near(1850f, tuning.Gravity, 0f, "character gravity remains unchanged");
        TestAssert.Near(590f, tuning.JumpSpeed, 0f, "normal jump remains unchanged");
        TestAssert.Equal(0, tuning.Validate().Count, "default character landing settings are valid");
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity,
                     float.NegativeInfinity })
        {
            safety.SetValue(tuning, invalid);
            TestAssert.True(HasError(tuning, "FallSafeImpactSpeed"), "invalid safety setting is diagnosed");
        }
        safety.SetValue(tuning, 300f);
        foreach (float invalid in new[] { 0f, 299f, 300f, float.NaN,
                     float.PositiveInfinity, float.NegativeInfinity })
        {
            tuning.MaxFallSpeed = invalid;
            TestAssert.True(HasError(tuning, nameof(PlayerTuning.MaxFallSpeed)),
                "reference must be finite and strictly above safety");
        }
        tuning.MaxFallSpeed = 600f;
        TestAssert.Equal(0, tuning.Validate().Count, "nondefault valid reference is allowed");
        safety.SetValue(tuning, 0f);
        TestAssert.Equal(0, tuning.Validate().Count, "zero safe speed is allowed");
        GD.Print("THRUSTER_IMPACT_CONFIG_OK character_owned=true authored=true default_300=true reference_actual_max_fall=true invalid_values_rejected=true gravity_jump_unchanged=true owned_resource_disposed=true player_wired=false");
    }

    private static bool HasError(PlayerTuning tuning, string field)
    {
        foreach (string error in tuning.Validate())
            if (error.Contains(field, StringComparison.Ordinal)) return true;
        return false;
    }

    private static void ConsumptionIsHeightCompatibleAndOnce()
    {
        TestAssert.True(!default(LandingImpactSample).IsReliable,
            "default measurement cannot accidentally grant safety");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -1f })
        {
            var model = new PlayerFallDamageModel();
            model.ResetOrigin(0f);
            var invalidSample = new LandingImpactSample(invalid, LandingImpactFallbackReason.Reliable);
            TestAssert.True(!invalidSample.IsReliable, "reliability cannot legitimize an invalid speed");
            PlayerFallLanding landing = model.ConsumeLanding(1400f, false, 5, invalidSample, 300f, 780f);
            TestAssert.Equal(2, landing.Damage, "invalid measurement consumes old height damage");
            TestAssert.Near(1400f, landing.Height, 0f, "fallback preserves original height diagnostics");
            TestAssert.Equal(0, model.ConsumeLanding(1400f, false, 5, invalidSample, 300f, 780f).Damage,
                "fallback settlement also consumes exactly once before Health");
        }
        var slow = new PlayerFallDamageModel();
        slow.ResetOrigin(0f);
        TestAssert.Equal(0, slow.ConsumeLanding(1400f, false, 5,
            new(300f, LandingImpactFallbackReason.Reliable), 300f, 780f).Damage,
            "reliable slow landing consumes without injury");
        slow.SampleHeight(2200f);
        TestAssert.Equal(1, slow.ConsumeLanding(2200f, false, 5,
            new(780f, LandingImpactFallbackReason.Reliable), 300f, 780f).Damage,
            "previous safe landing cannot immunize the next fast fall");
        var soft = new PlayerFallDamageModel();
        soft.ResetOrigin(0f);
        TestAssert.Equal(0, soft.ConsumeLanding(3200f, true, 5,
            LandingImpactSample.Unavailable(LandingImpactFallbackReason.MultipleContacts), 300f, 780f).Damage,
            "explicit all-soft exemption is independent of impact reliability");
    }

    private static void RelativeNormalMotionRejectsInvalidMeasurements()
    {
        MethodInfo method = typeof(LandingImpactSample).GetMethod("MeasureRelativeNormal",
            BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(Vector2), typeof(Vector2), typeof(Vector2) }, null);
        TestAssert.True(method != null, "Relative normal impact measurement boundary is unavailable");
        var measure = method.CreateDelegate<Func<Vector2, Vector2, Vector2, LandingImpactSample>>();
        foreach ((Vector2 body, Vector2 floor, Vector2 normal, float expected) in new[]
        {
            (new Vector2(0f, 780f), Vector2.Zero, Vector2.Up, 780f),
            (new Vector2(0f, 780f), new Vector2(0f, 120f), Vector2.Up, 660f),
            (new Vector2(0f, 780f), new Vector2(0f, -120f), Vector2.Up, 900f),
            (new Vector2(0f, 780f), new Vector2(0f, 780f), Vector2.Up, 0f),
            (new Vector2(0f, -20f), Vector2.Zero, Vector2.Up, 0f),
            (new Vector2(400f, 50f), new Vector2(-200f, 50f), Vector2.Up, 0f),
            (new Vector2(100f, 500f), Vector2.Zero, new Vector2(.6f, -.8f), 340f)
        })
        {
            LandingImpactSample sample = measure(body, floor, normal);
            TestAssert.True(sample.IsReliable, "finite validated relative normal motion is measurable");
            TestAssert.Near(expected, sample.Speed, .001f, "relative normal projection matches independent values");
        }
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            TestAssert.Equal(LandingImpactFallbackReason.InvalidMotion,
                measure(new Vector2(invalid, 780f), Vector2.Zero, Vector2.Up).Reason,
                "nonfinite body velocity is not safety");
            TestAssert.Equal(LandingImpactFallbackReason.InvalidMotion,
                measure(Vector2.Zero, new Vector2(0f, invalid), Vector2.Up).Reason,
                "nonfinite contact-point velocity is not safety");
        }
        foreach (Vector2 invalid in new[] { Vector2.Zero, Vector2.Down, Vector2.Right,
                     new Vector2(0f, -2f), new Vector2(float.NaN, -1f),
                     new Vector2(0f, float.PositiveInfinity) })
        {
            LandingImpactSample sample = measure(new Vector2(0f, 780f), Vector2.Zero, invalid);
            TestAssert.Equal(LandingImpactFallbackReason.InvalidContact, sample.Reason,
                "invalid or non-floor unit normal is rejected rather than guessed");
            TestAssert.True(!sample.IsReliable && float.IsNaN(sample.Speed),
                "invalid normal cannot be represented as safe zero impact");
        }
    }
}
