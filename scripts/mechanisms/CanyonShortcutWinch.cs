using Godot;
using Knight.Abilities;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonShortcutWinch : Node2D, IThreadInteractionReceiver, ICheckpointResettable
{
    [Signal] public delegate void OpenedEventHandler(StringName shortcutId);
    [Export] public StringName ShortcutId { get; set; } = "canyon.v2.upper-basin-shortcut";
    [Export] public NodePath BarrierCollisionPath { get; set; } = new("Barrier/Collision");
    [Export] public NodePath ClosedVisualPath { get; set; } = new("ClosedVisual");
    [Export] public NodePath OpenVisualPath { get; set; } = new("OpenVisual");
    [Export] public Rect2 CorridorBounds { get; set; } = new(-60, -160, 120, 320);
    public bool IsOpen { get; private set; }
    public CollisionShape2D BarrierCollision => GetNode<CollisionShape2D>(BarrierCollisionPath);
    private PrologueProgress _progress;

    public override void _Ready()
    {
        GrappleSurface2D profile = GetNodeOrNull<GrappleSurface2D>("Barrier/GrappleSurfaceProfile");
        if (profile != null) profile.Material = GrappleSurfaceMaterial.Forbidden;
        ApplyOpenState();
    }

    public void ApplyProgress(PrologueProgress progress)
    {
        _progress = progress;
        IsOpen = progress?.IsShortcutOpen(ShortcutId.ToString()) == true;
        ApplyOpenState();
    }

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if (IsOpen) return true;
        IsOpen = true;
        ApplyOpenState();
        EmitSignal(SignalName.Opened, ShortcutId);
        return true;
    }

    public void ResetForCheckpoint()
    {
        IsOpen = _progress?.IsShortcutOpen(ShortcutId.ToString()) == true || IsOpen;
        ApplyOpenState();
    }

    private void ApplyOpenState()
    {
        if (GetNodeOrNull<CollisionShape2D>(BarrierCollisionPath) is { } collision) collision.Disabled = IsOpen;
        if (GetNodeOrNull<CanvasItem>(ClosedVisualPath) is { } closed) closed.Visible = !IsOpen;
        if (GetNodeOrNull<CanvasItem>(OpenVisualPath) is { } open) open.Visible = IsOpen;
    }
}
