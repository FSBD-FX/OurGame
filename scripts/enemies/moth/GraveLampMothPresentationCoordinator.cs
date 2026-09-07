using Godot;
using Knight.Config;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Enemies.Moth;

public partial class GraveLampMothPresentationCoordinator : Node
{
    [Export] public NodePath ActorPath { get; set; } = new("..");
    [Export] public NodePath VisualRootPath { get; set; } = new("../VisualRoot");
    [Export] public NodePath EffectsPath { get; set; } = new("../VisualRoot/Effects");
    [Export] public PresentationProfile Profile { get; set; } = new();

    private GraveLampMoth _actor;
    private IEntityVisual2D _visual;
    private Node _effects;

    public override void _Ready()
    {
        _actor = GetNodeOrNull<GraveLampMoth>(ActorPath);
        _effects = GetNodeOrNull(EffectsPath);
        Profile ??= new PresentationProfile();
        _visual = Profile.Resolve(GetNode(VisualRootPath));
        if (_actor == null) return;
        _actor.FrameChanged += OnFrameChanged;
        _actor.PresentationCueRequested += OnCue;
        OnFrameChanged(_actor.CurrentFrame, _actor.Facing, false);
    }

    public override void _ExitTree()
    {
        if (!IsInstanceValid(_actor)) return;
        _actor.FrameChanged -= OnFrameChanged;
        _actor.PresentationCueRequested -= OnCue;
    }

    public static EntityPresentationFrame ResolveFrame(GraveLampMothFrame frame, int facing, bool grounded)
    {
        EntityVisualState state = frame.State switch
        {
            GraveLampMothState.Align => EntityVisualState.Move,
            GraveLampMothState.Telegraph or GraveLampMothState.Dive => EntityVisualState.Attack,
            GraveLampMothState.ThreadStunned => EntityVisualState.Hurt,
            GraveLampMothState.Dead => EntityVisualState.Dead,
            _ => EntityVisualState.Idle
        };
        return new EntityPresentationFrame(state, facing < 0 ? -1 : 1, grounded);
    }

    private void OnFrameChanged(GraveLampMothFrame frame, int facing, bool grounded)
    {
        if (_visual is GraveLampMothFormalVisual formal)
            formal.ApplyMothFrame(frame, facing, grounded);
        else if (_visual is GraveLampMothPlaceholderVisual placeholder)
            placeholder.ApplyMothFrame(frame, facing, grounded);
        else _visual?.ApplyFrame(ResolveFrame(frame, facing, grounded));
    }

    private void OnCue(PresentationCue cue)
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
