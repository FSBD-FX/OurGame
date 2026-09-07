using Godot;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Vfx;

namespace Knight.Mechanisms;

public partial class BellChainMechanism : Node2D, IThreadInteractionReceiver, ICheckpointResettable
{
    [Signal] public delegate void OpenedEventHandler();

    [Export] public NodePath DoorCollisionPath { get; set; } = new("Door/Collision");
    [Export] public NodePath ClosedVisualPath { get; set; } = new("VisualRoot/Placeholder/Closed");
    [Export] public NodePath OpenVisualPath { get; set; } = new("VisualRoot/Placeholder/Open");
    [Export] public NodePath FormalVisualPath { get; set; } = new("VisualRoot/Formal");
    [Export] public NodePath DoorVisualPath { get; set; } = new("VisualRoot/Door");
    [Export] public float DoorHeight { get; set; } = 180f;

    public bool IsOpen { get; private set; }

    private PrologueProgress _progress;

    public override void _Ready()
    {
        ApplyConfiguredDoorGeometry();
    }

    public void ApplyConfiguredDoorGeometry()
    {
        if (!float.IsFinite(DoorHeight) || DoorHeight <= 0f)
            throw new System.InvalidOperationException(
                $"{nameof(DoorHeight)} must be finite and positive.");
        CollisionShape2D collision = GetNodeOrNull<CollisionShape2D>(DoorCollisionPath);
        if (collision?.Shape is RectangleShape2D rectangle)
            rectangle.Size = new Vector2(rectangle.Size.X, DoorHeight);
        Node2D doorVisual = GetNodeOrNull<Node2D>(DoorVisualPath);
        if (doorVisual != null) doorVisual.Scale = new Vector2(doorVisual.Scale.X, DoorHeight / 180f);
    }

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if (IsOpen) return true;
        IsOpen = true;
        ApplyOpenState(MechanismVisualState.Active);
        PrologueVfxDirector.PlayFromTree(this, PresentationCue.ShortcutOpened,
            GlobalPosition + new Vector2(0, -80), Vector2.Up);
        EmitSignal(SignalName.Opened);
        return true;
    }

    public void ApplyProgress(PrologueProgress progress)
    {
        _progress = progress;
        IsOpen = progress?.BellChainShortcutOpen ?? false;
        ApplyOpenState(IsOpen ? MechanismVisualState.Restored : MechanismVisualState.Idle);
    }

    public void ResetForCheckpoint()
    {
        if (_progress?.BellChainShortcutOpen == true) IsOpen = true;
        ApplyOpenState(IsOpen ? MechanismVisualState.Restored : MechanismVisualState.Idle);
    }

    private void ApplyOpenState(MechanismVisualState state)
    {
        CollisionShape2D collision = GetNodeOrNull<CollisionShape2D>(DoorCollisionPath);
        if (collision != null) collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, IsOpen);
        CanvasItem closed = GetNodeOrNull<CanvasItem>(ClosedVisualPath);
        CanvasItem open = GetNodeOrNull<CanvasItem>(OpenVisualPath);
        if (closed != null) closed.Visible = !IsOpen;
        if (open != null) open.Visible = IsOpen;
        GetNodeOrNull<PrologueMechanismVisual>(FormalVisualPath)?.SetState(state);
        GetNodeOrNull<PrologueMechanismVisual>(DoorVisualPath)?.SetState(state);
    }
}
