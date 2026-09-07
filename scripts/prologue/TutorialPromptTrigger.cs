using Godot;

namespace Knight.Prologue;

public partial class TutorialPromptTrigger : Area2D
{
    [Signal]
    public delegate void PromptRequestedEventHandler(StringName tutorialId, string text);

    [Export] public StringName TutorialId { get; set; }
    [Export(PropertyHint.MultilineText)] public string Text { get; set; } = string.Empty;

    public override void _Ready() => BodyEntered += OnBodyEntered;
    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player")) EmitSignal(SignalName.PromptRequested, TutorialId, Text);
    }
}
