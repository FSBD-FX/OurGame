using Godot;
using Knight.Shared;
using Knight.Abilities;
using Knight.Combat;

namespace Knight.Player;

public partial class PlayerInputReader : Node
{
    private bool _keyboardControls = true;
    private bool _suppressOneShotSample;
    private int _lastFacing = 1;

    public Vector2 KeyboardAimDirection => PlayerKeyboardAimModel.ResolveDirection(CurrentFrame.Aim, _lastFacing);
    public PlayerInputFrame CurrentFrame { get; private set; }

    // Same sample/sequence as CurrentFrame, after lifecycle suppression but
    // before action arbitration. Facts only; consumers must not execute it.
    public PlayerInputFrame RawFrame { get; private set; }

    public ulong CurrentFrameSequence { get; private set; }

    /// <summary>
    /// Latest viewport-local pointer coordinate observed through Godot's normal mouse-motion
    /// event route. Real desktop mouse motion and replayed <see cref="InputEventMouseMotion"/>
    /// both reach this adapter; no caller writes a player, grapple, or aim model directly.
    /// </summary>
    public Vector2 LastObservedMouseViewportPosition { get; private set; }

    public bool HasObservedMouseMotion { get; private set; }

    public ulong MouseMotionSequence { get; private set; }

    public PlayerInputFrame Current => CurrentFrame;

    public void ClearSampledFrame()
    {
        CurrentFrame = default;
        RawFrame = default;
    }

    public void ResetForGameplayBoundary()
    {
        ClearSampledFrame();
        _suppressOneShotSample = true;
    }

    public void ConsumePrimaryPressedForCurrentPhysicsFrame()
    {
        CurrentFrame = CurrentFrame with { PrimaryPressed = false,
            AttackPressed = CurrentFrame.KeyboardActions && CurrentFrame.AttackPressed };
    }

    public void ConsumeOneShotEdgesForCurrentPhysicsFrame()
    {
        CurrentFrame = WithoutOneShotEdges(CurrentFrame);
        RawFrame = WithoutOneShotEdges(RawFrame);
    }

    private static PlayerInputFrame WithoutOneShotEdges(PlayerInputFrame frame) => frame with
        {
            JumpPressed = false,
            DashPressed = false,
            AttackPressed = false,
            ThreadPressed = false,
            ThreadReleased = false,
            AttackModePressed = false,
            GrappleModePressed = false,
            RequestedSlot = 0,
            PrimaryPressed = false,
            PrimaryReleased = false,
            ClawPressed = false,
            InteractPressed = false
        };

    public override void _Notification(int what)
    {
        if (what == NotificationPaused || what == NotificationUnpaused)
        {
            ConsumeOneShotEdgesForCurrentPhysicsFrame();
            _suppressOneShotSample = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion motion) return;
        LastObservedMouseViewportPosition = motion.Position;
        HasObservedMouseMotion = true;
        MouseMotionSequence++;
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentFrameSequence++;
        // Prefer the normal input event that Godot delivered to this adapter. The fallback
        // preserves ordinary behaviour before a first mouse-motion event arrives (for example
        // immediately after a level loads), while real users and automated playback share the
        // same event-driven route after that point.
        Vector2 mouseViewport = HasObservedMouseMotion
            ? LastObservedMouseViewportPosition
            : GetViewport().GetMousePosition();
        Vector2 mouseWorld = GetViewport().GetCanvasTransform().AffineInverse() * mouseViewport;
        int requestedSlot = Input.IsActionJustPressed(InputActions.Slot1) ? 1 :
            Input.IsActionJustPressed(InputActions.Slot2) ? 2 :
            Input.IsActionJustPressed(InputActions.Slot3) ? 3 :
            Input.IsActionJustPressed(InputActions.Slot4) ? 4 : 0;

        bool attack = Input.IsActionJustPressed(InputActions.Attack);
        bool claw = Input.IsActionJustPressed(InputActions.Claw);
        bool interact = Input.IsActionJustPressed(InputActions.Interact);
        bool keyboardShield = Input.IsActionPressed(InputActions.KeyboardShield);
        bool legacyAttackMode = Input.IsActionJustPressed(InputActions.SelectAttackMode);
        bool legacyGrappleMode = Input.IsActionJustPressed(InputActions.SelectGrappleMode);
        bool legacyPrimary = Input.IsActionJustPressed(InputActions.PrimaryAction);
        bool legacyShield = Input.IsActionPressed(InputActions.Shield);
        if (legacyAttackMode || legacyGrappleMode || legacyPrimary || legacyShield) _keyboardControls = false;
        if (attack || claw || interact || keyboardShield) _keyboardControls = true;

        CurrentFrame = new PlayerInputFrame(
            Input.GetAxis(InputActions.MoveLeft, InputActions.MoveRight),
            Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown),
            Input.IsActionJustPressed(InputActions.Jump),
            Input.IsActionPressed(InputActions.Jump),
            Input.IsActionJustPressed(InputActions.Dash),
            attack,
            false,
            false,
            false,
            mouseWorld,
            legacyAttackMode,
            legacyGrappleMode,
            requestedSlot,
            legacyPrimary,
            Input.IsActionPressed(InputActions.PrimaryAction),
            Input.IsActionJustReleased(InputActions.PrimaryAction),
            keyboardShield || legacyShield,
            ClawPressed: claw,
            InteractPressed: interact,
            KeyboardActions: _keyboardControls,
            KeyboardShieldHeld: keyboardShield,
            LegacyShieldHeld: legacyShield);
        RawFrame = CurrentFrame;
        if (!Mathf.IsZeroApprox(CurrentFrame.MoveAxis)) _lastFacing = CurrentFrame.MoveAxis < 0f ? -1 : 1;
        if (_suppressOneShotSample)
        {
            ConsumeOneShotEdgesForCurrentPhysicsFrame();
            _suppressOneShotSample = false;
        }
        if (!_keyboardControls) return;
        var currentClaw = GetParent().GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        var health = GetParent().GetNodeOrNull<HealthComponent>("Health");
        var shield = GetParent().GetNodeOrNull<PlayerShieldController>("Shield");
        bool attached = currentClaw?.CurrentFrame.State is MechanicalClawState.Latched or
            MechanicalClawState.Reel or MechanicalClawState.Swing or MechanicalClawState.Boost;
        AcceptedPlayerActions accepted = PlayerActionCommandModel.Resolve(CurrentFrame, attached,
            GetTree().Paused || health?.CurrentHealth <= 0,
            shield?.IsActive == true || shield?.CanBeginGuard == true);
        CurrentFrame = CurrentFrame with
        {
            JumpPressed = accepted.HasFlag(AcceptedPlayerActions.Jump),
            DashPressed = accepted.HasFlag(AcceptedPlayerActions.Dash),
            AttackPressed = accepted.HasFlag(AcceptedPlayerActions.Attack),
            ClawPressed = accepted.HasFlag(AcceptedPlayerActions.Claw),
            InteractPressed = accepted.HasFlag(AcceptedPlayerActions.Interact),
            // Legacy presses never compete with independent keyboard commands.
            PrimaryPressed = false, AttackModePressed = false, GrappleModePressed = false
        };
    }
}
