using Godot;
using Knight.Config;
using Knight.Enemies;
using Knight.Vfx;

namespace Knight.Presentation;

public partial class RustShellPresentationCoordinator : Node
{
    [Export] public NodePath ActorPath { get; set; } = new("..");
    [Export] public NodePath VisualRootPath { get; set; } = new("../VisualRoot");
    [Export] public NodePath EffectsPath { get; set; } = new("../VisualRoot/Effects");
    [Export] public PresentationProfile Profile { get; set; } = new();

    private RustShellSquire _actor;
    private Node _effects;
    private IEntityVisual2D _visual;

    public override void _Ready()
    {
        _actor = GetNodeOrNull<RustShellSquire>(ActorPath);
        _effects = GetNodeOrNull(EffectsPath);
        Profile ??= new PresentationProfile();
        _visual = Profile.Resolve(GetNode(VisualRootPath));
        if (_actor != null)
        {
            _actor.FrameChanged += OnFrameChanged;
            _actor.PresentationCueRequested += OnCueRequested;
            ApplyVisualFrame(_actor.CurrentFrame, _actor.Facing);
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_actor))
        {
            _actor.FrameChanged -= OnFrameChanged;
            _actor.PresentationCueRequested -= OnCueRequested;
        }
    }

    public static EntityPresentationFrame ResolveFrame(RustShellFrame frame, int facing)
    {
        EntityVisualState state = frame.State switch
        {
            RustShellState.Chase => EntityVisualState.Move,
            RustShellState.Telegraph => EntityVisualState.Attack,
            RustShellState.Attack => EntityVisualState.Attack,
            RustShellState.Stunned => EntityVisualState.Hurt,
            RustShellState.Dead => EntityVisualState.Dead,
            _ => EntityVisualState.Idle
        };
        return new EntityPresentationFrame(state, facing < 0 ? -1 : 1, true);
    }

    private void OnFrameChanged(RustShellFrame frame, int facing) => ApplyVisualFrame(frame, facing);

    private void ApplyVisualFrame(RustShellFrame frame, int facing)
    {
        if (_visual is RustShellFormalVisual formal) formal.ApplyRustShellFrame(frame, facing);
        else _visual?.ApplyFrame(ResolveFrame(frame, facing));
    }

    private void OnCueRequested(PresentationCue cue)
    {
        _visual?.PlayCue(cue);
        PrologueVfxDirector.PlayFromTree(this, cue, _actor?.GlobalPosition ?? Vector2.Zero,
            new Vector2(_actor?.Facing ?? 1, 0));
        if (_effects != null && Profile.TryInstantiateCue(cue, out Node2D effect))
        {
            _effects.AddChild(effect);
            if (effect is GpuParticles2D particles) particles.Finished += effect.QueueFree;
        }
    }
}
