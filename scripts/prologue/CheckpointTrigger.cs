using System;
using Godot;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Prologue;

public partial class CheckpointTrigger : Area2D
{
    [Signal]
    public delegate void CheckpointActivatedEventHandler(StringName entranceId);

    // Layout2 authors a stable checkpoint identity here. Frozen sample/legacy
    // scenes retain their separately serialized entrance names.
    [Export] public StringName EntranceId { get; set; } = "canyon.v2.start";
    [Export] public NodePath VisualPath { get; set; } = new("Visual");

    private PrologueMechanismVisual _visual;
    // A streamed world owns this synchronous request. Public success signals
    // remain notifications; unbound legacy rooms keep their original contract.
    internal Func<StringName, bool> ActivationRequest { get; set; }

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
        if (ActivationRequest != null && !ActivationRequest(EntranceId)) return;
        _visual?.SetState(MechanismVisualState.Active);
        PrologueVfxDirector.PlayFromTree(this, PresentationCue.CheckpointActivated,
            GlobalPosition, Vector2.Up);
        EmitSignal(SignalName.CheckpointActivated, EntranceId);
    }
}
