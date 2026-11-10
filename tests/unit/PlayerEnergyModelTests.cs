using Knight.Config;
using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerEnergyModelTests
{
    public static void Run()
    {
        AttackCostsTwelveAndStartsRegenerationDelay();
        ShieldDrainsAndHeatsAtApprovedRates();
        ShieldClosesInTheStepThatOverheats();
        RegenerationWaitsForItsFullDelay();
        RegenerationUsesElapsedTimeAfterDelayExpires();
        CoolingUsesElapsedTimeAfterDelayExpires();
        OverheatUnlocksOnlyBelowThirtyFiveHeat();
        FullHeatAlwaysCorrectsToOverheated();
        EmptyEnergyClosesShieldWithoutOverheating();
        ResetFullRestoresTheApprovedStartingState();
        ResetFullClearsTheShieldRequestLatch();
    }

    private static void AttackCostsTwelveAndStartsRegenerationDelay()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        PlayerEnergyState state = PlayerEnergyState.Full(settings);

        TestAssert.True(model.TrySpendAttack(state, out state), "full energy accepts attack");
        TestAssert.Near(88f, state.Energy, .001f, "attack costs twelve");
        TestAssert.Near(.55f, state.RegenDelayRemaining, .001f, "attack starts the regeneration delay");
    }

    private static void ShieldDrainsAndHeatsAtApprovedRates()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        model.TrySpendAttack(PlayerEnergyState.Full(settings), out PlayerEnergyState spent);
        PlayerEnergyFrame held = model.Step(spent, new PlayerEnergyInput(true, false), 1f);

        TestAssert.True(held.ShieldSustained, "shield is sustained while energy and heat permit it");
        TestAssert.Near(64f, held.State.Energy, .001f, "shield drains 24 per second");
        TestAssert.Near(42f, held.State.Heat, .001f, "shield heats 42 per second");
    }

    private static void ShieldClosesInTheStepThatOverheats()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        PlayerEnergyFrame overheated = model.Step(PlayerEnergyState.Full(settings),
            new PlayerEnergyInput(true, false), 100f / 42f);

        TestAssert.True(overheated.State.Overheated && !overheated.ShieldSustained,
            "heat 100 forces shield closed in the same step");
        TestAssert.Near(100f, overheated.State.Heat, .001f, "heat clamps at one hundred");
    }

    private static void RegenerationWaitsForItsFullDelay()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        var spent = new PlayerEnergyState(50f, 0f, settings.RegenDelaySeconds, 0f, false);

        PlayerEnergyFrame waiting = model.Step(spent, new PlayerEnergyInput(false, false), .55f);
        PlayerEnergyFrame regenerated = model.Step(waiting.State, new PlayerEnergyInput(false, false), 1f);

        TestAssert.Near(50f, waiting.State.Energy, .001f, "no regeneration occurs before the 0.55 second delay expires");
        TestAssert.Near(78f, regenerated.State.Energy, .001f, "regeneration restores 28 points after one valid second");
    }

    private static void OverheatUnlocksOnlyBelowThirtyFiveHeat()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        var lockedAtThreshold = new PlayerEnergyState(100f, 35f, 0f, 0f, true);
        var aboveThreshold = new PlayerEnergyState(100f, 36f, 0f, 0f, true);

        PlayerEnergyFrame threshold = model.Step(lockedAtThreshold, new PlayerEnergyInput(false, false), 0f);
        PlayerEnergyFrame unlocked = model.Step(aboveThreshold, new PlayerEnergyInput(false, false), 2f / 35f);

        TestAssert.True(threshold.State.Overheated, "overheat remains locked at exactly 35 heat");
        TestAssert.True(!unlocked.State.Overheated, "overheat unlocks only after cooling below 35 heat");
    }

    private static void RegenerationUsesElapsedTimeAfterDelayExpires()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        var delayed = new PlayerEnergyState(50f, 0f, settings.RegenDelaySeconds, 0f, false);

        PlayerEnergyFrame unsplit = model.Step(delayed, new PlayerEnergyInput(false, false), .75f);
        PlayerEnergyFrame splitDelay = model.Step(delayed, new PlayerEnergyInput(false, false), .55f);
        PlayerEnergyFrame split = model.Step(splitDelay.State, new PlayerEnergyInput(false, false), .20f);

        TestAssert.Near(55.6f, unsplit.State.Energy, .001f,
            "unpartitioned regeneration uses time after the delay expires");
        TestAssert.Near(unsplit.State.Energy, split.State.Energy, .001f,
            "energy regeneration is invariant to elapsed-time partitioning");
    }

    private static void CoolingUsesElapsedTimeAfterDelayExpires()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        var delayed = new PlayerEnergyState(100f, 70f, 0f, settings.HeatCoolDelaySeconds, false);

        PlayerEnergyFrame unsplit = model.Step(delayed, new PlayerEnergyInput(false, false), .75f);
        PlayerEnergyFrame splitDelay = model.Step(delayed, new PlayerEnergyInput(false, false), .35f);
        PlayerEnergyFrame split = model.Step(splitDelay.State, new PlayerEnergyInput(false, false), .40f);

        TestAssert.Near(56f, unsplit.State.Heat, .001f,
            "unpartitioned cooling uses time after the delay expires");
        TestAssert.Near(unsplit.State.Heat, split.State.Heat, .001f,
            "heat cooling is invariant to elapsed-time partitioning");
    }

    private static void FullHeatAlwaysCorrectsToOverheated()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        var externallySupplied = new PlayerEnergyState(100f, 100f, 0f, 0f, false);

        PlayerEnergyFrame frame = model.Step(externallySupplied, new PlayerEnergyInput(false, false), 0f);

        TestAssert.True(frame.State.Overheated,
            "full heat corrects to overheated without a shield request or elapsed time");
        TestAssert.True(!frame.ShieldSustained, "full heat never sustains shield");
    }

    private static void EmptyEnergyClosesShieldWithoutOverheating()
    {
        PlayerEnergySettings settings = PlayerEnergySettings.Approved;
        var model = new PlayerEnergyModel(settings);
        var lowEnergy = new PlayerEnergyState(1f, 0f, 0f, 0f, false);

        PlayerEnergyFrame empty = model.Step(lowEnergy, new PlayerEnergyInput(true, false), 1f);

        TestAssert.Near(0f, empty.State.Energy, .001f, "energy cannot become negative");
        TestAssert.True(!empty.ShieldSustained, "zero energy closes shield in the same step");
        TestAssert.True(!empty.State.Overheated, "zero energy alone does not overheat the shield");
    }

    private static void ResetFullRestoresTheApprovedStartingState()
    {
        var controller = new PlayerEnergyController { Tuning = new PlayerEnergyTuning() };
        controller.ResetFull();

        TestAssert.Near(1f, controller.EnergyRatio, .001f, "reset restores full energy");
        TestAssert.Near(0f, controller.HeatRatio, .001f, "reset clears heat");
        TestAssert.True(!controller.IsOverheated, "reset clears overheat");
        controller.Free();
    }

    private static void ResetFullClearsTheShieldRequestLatch()
    {
        var controller = new PlayerEnergyController { Tuning = new PlayerEnergyTuning() };
        controller.SetShieldRequested(true);
        controller._PhysicsProcess(.25d);
        TestAssert.True(controller.EnergyRatio < .999f && controller.IsShieldSustained,
            "controller fixture establishes a real held shield request before reset");

        controller.ResetFull();
        controller._PhysicsProcess(.25d);

        TestAssert.Near(1f, controller.EnergyRatio, .001f,
            "recovery reset clears the shield-request latch before a later physics step");
        TestAssert.True(!controller.IsShieldSustained,
            "stale pre-recovery shield request cannot resume energy drain");
        controller.Free();
    }
}
