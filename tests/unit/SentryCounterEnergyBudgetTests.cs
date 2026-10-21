using Godot;
using Knight.Enemies.Sentry;
using Knight.Player;
using Knight.Tests.Presentation;

namespace Knight.Tests.Unit;

internal static class SentryCounterEnergyBudgetTests
{
    internal static void Run()
    {
        using var sentry = new BellBoltSentrySettings();
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);

        // Break: admitting J on affordability alone spends the next guard's energy.
        // The real failed route had 19.9 energy at its second counter request.
        var observed = new PlayerEnergyState(19.9f, 0f, 0f, 0f, false);
        TestAssert.True(model.TrySpendAttack(observed, out PlayerEnergyState spent),
            "observed Sentry counter really can pay the unchanged attack cost");
        for (int sample = 0; sample < 40; sample++)
            spent = model.Step(spent, new PlayerEnergyInput(false, false), 1f / 60f).State;
        float requestedEnergy = spent.Energy;
        PlayerEnergyFrame guarded = default;
        for (int sample = 0; sample < 28; sample++)
        {
            guarded = model.Step(spent, new PlayerEnergyInput(true, false), 1f / 60f);
            spent = guarded.State;
        }
        TestAssert.Near(0f, spent.Energy, .001f, "real model reproduces second guard energy exhaustion");
        TestAssert.True(!guarded.ShieldSustained && !spent.Overheated,
            "observed guard fails for empty energy, not heat");
        GD.Print($"SENTRY_COUNTER_ENERGY_REPRO_OK attack_energy=19.9 guard_energy={requestedEnergy:0.000} " +
            $"held_samples=28 final_energy={spent.Energy:0.000} overheated={spent.Overheated}");
        TestAssert.True(!SentryCounterEnergyBudget.CanSpendAttack(19.9f, settings, sentry, 60),
            "Sentry counter must reserve energy for its next required shield window");

        // 24 lead ticks + ceil(640/440*60)=88 flight ticks + 3 input/retire ticks.
        // 115 guards cost46; with J's12, energy58 reaches zero and is not sustainable.
        TestAssert.True(!SentryCounterEnergyBudget.CanSpendAttack(58f, settings, sentry, 60),
            "exactly exhausted shield-window budget must refuse a counter");
        TestAssert.True(!SentryCounterEnergyBudget.CanSpendAttack(57.6f, settings, sentry, 60),
            "one guard sample below the reserve must refuse a counter");
        TestAssert.True(SentryCounterEnergyBudget.CanSpendAttack(58.4f, settings, sentry, 60),
            "a payable counter with one live shield sample remaining is admitted");
        var sufficient = new PlayerEnergyState(58.4f, 0f, .55f, 0f, false);
        TestAssert.True(model.TrySpendAttack(sufficient, out sufficient), "admitted counter pays actual J");
        for (int sample = 0; sample < 115; sample++)
        {
            PlayerEnergyFrame next = model.Step(sufficient, new PlayerEnergyInput(true, false), 1f / 60f);
            TestAssert.True(next.ShieldSustained, "admitted budget sustains every real shield sample");
            sufficient = next.State;
        }
        TestAssert.Near(.4f, sufficient.Energy, .001f,
            "budget does not rely on regeneration while shielding or grant free energy");

        // A short bolt still requires the complete unchanged Fire guard window.
        sentry.MaximumTravel = 44f;
        TestAssert.True(!SentryCounterEnergyBudget.CanSpendAttack(26f, settings, sentry, 60) &&
            SentryCounterEnergyBudget.CanSpendAttack(26.4f, settings, sentry, 60),
            "reserve includes full Fire even if projectile lifetime is shorter");
        TestAssert.True(!SentryCounterEnergyBudget.CanSpendAttack(float.NaN, settings, sentry, 60) &&
            !SentryCounterEnergyBudget.CanSpendAttack(100f, settings, sentry, 0),
            "invalid energy or sampling cadence cannot admit a counter");
        GD.Print("SENTRY_COUNTER_ENERGY_BUDGET_OK observed_failure=True native_model=True boundary=True fire_window=True");
    }
}
