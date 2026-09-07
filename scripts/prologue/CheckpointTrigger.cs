using Godot;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Prologue;

public partial class CheckpointTrigger : Area2D
{
    [Signal]
    public delegate void CheckpointActivatedEventHandler(StringName entranceId);

    [Export] public StringName EntranceId { get; set; } = "start";
    [Export] public NodePath VisualPath { get; set; } = new("Visual");

    private PrologueMechanismVisual _visual;

    public override void _Ready()
    {
        _visual = GetNodeOrNull<PrologueMechanismVisual>(VisualPath);
        _visual?.SetState(MechanismVisualState.Idle);
        BodyEntered += OnBodyEntered;
    }
    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _visual?.SetState(MechanismVisualState.Active);
        PrologueVfxDirector.PlayFromTree(this, PresentationCue.CheckpointActivated,
            GlobalPosition, Vector2.Up);
        EmitSignal(SignalName.CheckpointActivated, EntranceId);
    }
}
