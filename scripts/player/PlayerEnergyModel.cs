using System;

namespace Knight.Player;

public sealed class PlayerEnergyModel
{
    private const float MaximumHeat = 100f;
    private readonly PlayerEnergySettings _settings;

    public PlayerEnergyModel(PlayerEnergySettings settings)
    {
        _settings = settings;
    }

    public bool TrySpendAttack(PlayerEnergyState state, out PlayerEnergyState spent) =>
        TrySpend(state, _settings.AttackCost, out spent);

    public bool TrySpendHeavyGuard(PlayerEnergyState state, float amount, out PlayerEnergyState spent) =>
        TrySpend(state, amount, out spent);

    public PlayerEnergyFrame Step(PlayerEnergyState state, PlayerEnergyInput input, float delta)
    {
        float elapsed = float.IsFinite(delta) ? MathF.Max(0f, delta) : 0f;
        float energy = ClampEnergy(state.Energy);
        float heat = ClampHeat(state.Heat);
        float regenDelay = ClampNonNegative(state.RegenDelayRemaining);
        float coolDelay = ClampNonNegative(state.HeatCoolDelayRemaining);
        bool overheated = state.Overheated || heat >= MaximumHeat;
        bool shieldSustained = input.ShieldRequested && !input.ForceShieldClosed && !overheated && energy > 0f;

        if (shieldSustained)
        {
            energy = ClampEnergy(energy - _settings.ShieldDrainPerSecond * elapsed);
            heat = ClampHeat(heat + _settings.ShieldHeatPerSecond * elapsed);
            regenDelay = _settings.RegenDelaySeconds;
            coolDelay = _settings.HeatCoolDelaySeconds;

            if (heat >= MaximumHeat)
            {
                overheated = true;
                shieldSustained = false;
            }
            else if (energy <= 0f)
            {
                shieldSustained = false;
            }
        }
        else
        {
            float regenElapsed = ConsumeDelay(ref regenDelay, elapsed);
            energy = ClampEnergy(energy + _settings.RegenPerSecond * regenElapsed);

            float coolElapsed = ConsumeDelay(ref coolDelay, elapsed);
            heat = ClampHeat(heat - _settings.HeatCoolPerSecond * coolElapsed);

            if (overheated && heat < _settings.OverheatUnlockHeat)
                overheated = false;
        }

        var next = new PlayerEnergyState(energy, heat, regenDelay, coolDelay, overheated);
        return new PlayerEnergyFrame(next, shieldSustained);
    }

    private bool TrySpend(PlayerEnergyState state, float amount, out PlayerEnergyState spent)
    {
        float energy = ClampEnergy(state.Energy);
        if (!float.IsFinite(amount) || amount < 0f || energy < amount)
        {
            spent = state;
            return false;
        }

        spent = new PlayerEnergyState(
            ClampEnergy(energy - amount),
            ClampHeat(state.Heat),
            _settings.RegenDelaySeconds,
            ClampNonNegative(state.HeatCoolDelayRemaining),
            state.Overheated);
        return true;
    }

    private float ClampEnergy(float value) => Clamp(value, 0f, _settings.MaxEnergy);
    private static float ConsumeDelay(ref float delay, float elapsed)
    {
        float consumed = MathF.Min(delay, elapsed);
        delay -= consumed;
        return elapsed - consumed;
    }

    private static float ClampHeat(float value) => Clamp(value, 0f, MaximumHeat);
    private static float ClampNonNegative(float value) => float.IsFinite(value) ? MathF.Max(0f, value) : 0f;
    private static float Clamp(float value, float minimum, float maximum) =>
        !float.IsFinite(value) ? minimum : Math.Clamp(value, minimum, maximum);
}
