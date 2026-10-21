using Godot;
using Knight.Player;

namespace Knight.Tests;

// Test-only observation after Input(-300), before action owners(-200/-100/0).
public partial class FormalInputSampleObserver : Node
{
    private PlayerInputReader _input;
    private bool _armed;
    public ulong BeforeSequence { get; private set; }
    public ulong FirstSequence { get; private set; }
    public PlayerInputFrame FirstFrame { get; private set; }
    public bool Captured { get; private set; }

    public void Arm(PlayerInputReader input)
    {
        _input = input;
        BeforeSequence = input.CurrentFrameSequence;
        FirstSequence = 0;
        FirstFrame = default;
        Captured = false;
        _armed = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_armed || _input.CurrentFrameSequence == BeforeSequence) return;
        FirstSequence = _input.CurrentFrameSequence;
        FirstFrame = _input.Current;
        Captured = true;
        _armed = false;
    }
}
