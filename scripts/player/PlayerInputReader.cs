using Godot;
using Knight.Shared;

namespace Knight.Player;

public partial class PlayerInputReader : Node
{
    public PlayerInputFrame CurrentFrame { get; private set; }

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

        CurrentFrame = new PlayerInputFrame(
            Input.GetAxis(InputActions.MoveLeft, InputActions.MoveRight),
            Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown),
            Input.IsActionJustPressed(InputActions.Jump),
            Input.IsActionPressed(InputActions.Jump),
            Input.IsActionJustPressed(InputActions.Dash),
            false,
            false,
            false,
            false,
            mouseWorld,
            Input.IsActionJustPressed(InputActions.SelectAttackMode),
            Input.IsActionJustPressed(InputActions.SelectGrappleMode),
            requestedSlot,
            Input.IsActionJustPressed(InputActions.PrimaryAction),
            Input.IsActionPressed(InputActions.PrimaryAction),
            Input.IsActionJustReleased(InputActions.PrimaryAction),
            Input.IsActionPressed(InputActions.Shield));
    }
}
