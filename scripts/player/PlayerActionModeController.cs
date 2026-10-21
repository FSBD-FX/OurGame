using System;
using Godot;
using Knight.Abilities;

namespace Knight.Player;

public enum PlayerActionRejectionReason
{
    None,
    EmptySlot,
    InsufficientEnergy,
    ShieldActive
}

public partial class PlayerActionModeController : Node
{
    [Export] public NodePath InputPath { get; set; } = new("../Input");
    [Export] public NodePath CombatPath { get; set; } = new("../Combat");
    [Export] public NodePath EnergyPath { get; set; } = new("../Energy");
    [Export] public NodePath ShieldPath { get; set; } = new("../Shield");
    [Export] public NodePath MechanicalClawPath { get; set; } = new("../MechanicalClaw");

    private readonly PlayerActionSelectionModel _selection = new();
    private PlayerActionSelectionState _state = new();
    private PlayerInputReader _input = null!;
    private PlayerCombat _combat = null!;
    private PlayerEnergyController _energy = null!;
    private PlayerShieldController _shield = null!;
    private MechanicalClawController _mechanicalClaw;

    public PlayerActionMode CurrentMode => _state.Mode;
    public int ActiveSlot => _state.ActiveSlot;
    public PlayerActionRejectionReason LastRejection { get; private set; }
    // Shared read-only affordability for teaching; use the same rule as accepted attacks.
    public bool CanAffordAttack => HasAttackEnergy();
    public bool CanBeginAttack => (_input?.Current.KeyboardActions == true || CurrentMode == PlayerActionMode.Attack) && ActiveSlot == 1 &&
        !(_combat?.IsAttacking ?? true) && !ShieldBlocksAttack && HasAttackEnergy();
    public bool CanBeginGrapple => (_input?.Current.KeyboardActions == true || CurrentMode == PlayerActionMode.Grapple) &&
        !(_input?.Current.ShieldHeld ?? true);

    // Input is sampled before Combat, but Shield refreshes later in the tick.
    // A released hold must not lose a fresh attack edge to yesterday's active state.
    // Still use the actual guard state so an overheated/closed shield permits attacks.
    private bool ShieldBlocksAttack => _shield == null || _input == null ||
        _shield.IsActive && _input.Current.ShieldHeld;

    public event Action<PlayerActionMode> ModeChanged;
    public event Action<int> LoadoutChanged;
    public event Action AttackModeRequested;
    public event Action<PlayerActionRejectionReason> PrimaryRejected;
    public event Action<PlayerActionRejectionReason> RejectionChanged;

    public override void _Ready()
    {
        ResolveDependencies();
    }

    public override void _PhysicsProcess(double delta)
    {
        ResolveDependencies();
        PlayerInputFrame input = _input.Current;
        if (input.KeyboardActions)
        {
            // Mode is presentation metadata, never a prerequisite for independent J/K.
            ApplySelection(_selection.Step(_state, input.AttackPressed, input.ClawPressed, input.RequestedSlot));
            if (input.AttackPressed && HasGrappleCompatibleActivity()) AttackModeRequested?.Invoke();
            return;
        }
        PlayerActionSelectionState next = _selection.Step(_state, input.AttackModePressed,
            input.GrappleModePressed, input.RequestedSlot);
        ApplySelection(next);
        if (input.AttackModePressed && input.PrimaryPressed)
            _input.ConsumePrimaryPressedForCurrentPhysicsFrame();
        if (input.AttackModePressed && HasGrappleCompatibleActivity()) AttackModeRequested?.Invoke();
    }

    public void RejectAttackRequest()
    {
        if (!_input.Current.KeyboardActions && CurrentMode != PlayerActionMode.Attack || _combat?.IsAttacking == true ||
            ActiveSlot == 1 && CanBeginAttack) return;
        PlayerActionRejectionReason reason = ActiveSlot != 1
            ? PlayerActionRejectionReason.EmptySlot
            : ShieldBlocksAttack
                ? PlayerActionRejectionReason.ShieldActive
                : PlayerActionRejectionReason.InsufficientEnergy;
        bool changed = LastRejection != reason;
        LastRejection = reason;
        PrimaryRejected?.Invoke(reason);
        if (changed) RejectionChanged?.Invoke(reason);
    }

    public void ConfirmAttackStarted()
    {
        if (LastRejection == PlayerActionRejectionReason.None) return;
        LastRejection = PlayerActionRejectionReason.None;
        RejectionChanged?.Invoke(PlayerActionRejectionReason.None);
    }

    private bool HasAttackEnergy() => _energy != null &&
        _energy.EnergyRatio * PlayerEnergySettings.Approved.MaxEnergy + .0001f >=
        PlayerEnergySettings.Approved.AttackCost;

    private bool HasGrappleCompatibleActivity() => _mechanicalClaw != null &&
        _mechanicalClaw.CurrentFrame.State != MechanicalClawState.Idle;

    private void ApplySelection(PlayerActionSelectionState next)
    {
        PlayerActionSelectionState previous = _state;
        _state = next;
        if (next.Mode != previous.Mode) ModeChanged?.Invoke(next.Mode);
        if (next.ActiveSlot != previous.ActiveSlot) LoadoutChanged?.Invoke(next.ActiveSlot);
    }

    private void ResolveDependencies()
    {
        _input ??= GetNode<PlayerInputReader>(InputPath);
        _combat ??= GetNode<PlayerCombat>(CombatPath);
        _energy ??= GetNode<PlayerEnergyController>(EnergyPath);
        _shield ??= GetNode<PlayerShieldController>(ShieldPath);
        _mechanicalClaw ??= GetNodeOrNull<MechanicalClawController>(MechanicalClawPath);
    }
}
