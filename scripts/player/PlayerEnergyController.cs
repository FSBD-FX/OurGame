using System;
using Godot;
using Knight.Config;

namespace Knight.Player;

public partial class PlayerEnergyController : Node
{
    [Export] public PlayerEnergyTuning Tuning { get; set; } = new();

    private PlayerEnergyModel _model = null!;
    private PlayerEnergyState _state;
    private PlayerEnergyFrame _frame;
    private bool _shieldRequested;

    public event Action<PlayerEnergyFrame> EnergyFrameChanged;

    public float EnergyRatio => Settings.MaxEnergy <= 0f ? 0f : _state.Energy / Settings.MaxEnergy;
    public float HeatRatio => _state.Heat / 100f;
    public bool IsOverheated => _state.Overheated;
    public bool IsShieldSustained => _frame.ShieldSustained;

    public override void _Ready()
    {
        EnsureInitialized();
    }

    public override void _PhysicsProcess(double delta)
    {
        EnsureInitialized();
        ApplyFrame(_model.Step(_state, new PlayerEnergyInput(_shieldRequested, false), (float)delta));
    }

    public bool TrySpendAttack()
    {
        EnsureInitialized();
        if (!_model.TrySpendAttack(_state, out PlayerEnergyState spent)) return false;
        ApplyState(spent);
        return true;
    }

    public bool TrySpendHeavyGuard(float amount = 24f)
    {
        EnsureInitialized();
        if (!_model.TrySpendHeavyGuard(_state, amount, out PlayerEnergyState spent)) return false;
        ApplyState(spent, preserveShieldSustain: true);
        return true;
    }

    public void SetShieldRequested(bool requested)
    {
        _shieldRequested = requested;
    }

    public void ResetFull()
    {
        EnsureInitialized();
        _shieldRequested = false;
        ApplyState(PlayerEnergyState.Full(Settings));
    }

    private PlayerEnergySettings Settings => Tuning.ToSettings();

    private void EnsureInitialized()
    {
        if (_model != null) return;
        Tuning ??= new PlayerEnergyTuning();
        var errors = Tuning.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException("Invalid PlayerEnergyTuning: " + string.Join("; ", errors));
        _model = new PlayerEnergyModel(Settings);
        _state = PlayerEnergyState.Full(Settings);
        _frame = new PlayerEnergyFrame(_state, false);
    }

    private void ApplyFrame(PlayerEnergyFrame next)
    {
        PlayerEnergyState previous = _state;
        _state = next.State;
        _frame = next;
        if (_state != previous) EnergyFrameChanged?.Invoke(_frame);
    }

    private void ApplyState(PlayerEnergyState next, bool preserveShieldSustain = false)
    {
        PlayerEnergyState previous = _state;
        _state = next;
        bool shieldSustained = preserveShieldSustain && _frame.ShieldSustained && _shieldRequested &&
            _state.Energy > 0f && !_state.Overheated;
        _frame = new PlayerEnergyFrame(_state, shieldSustained);
        if (_state != previous) EnergyFrameChanged?.Invoke(_frame);
    }
}
