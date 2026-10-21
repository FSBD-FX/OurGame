using Godot;

namespace Knight.Tests.Integration;

public partial class RecordingContactReadyEffect : Node2D
{
    public bool ReadyObserved { get; private set; }
    public Vector2 ReadyPosition { get; private set; }

    public override void _Ready()
    {
        ReadyPosition = GlobalPosition;
        ReadyObserved = true;
    }
}
