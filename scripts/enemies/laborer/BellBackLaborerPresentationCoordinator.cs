using Godot;
using Knight.Config;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Enemies.Laborer;

public partial class BellBackLaborerPresentationCoordinator : Node
{
    [Export] public NodePath ActorPath { get; set; } = new("..");
    [Export] public NodePath VisualRootPath { get; set; } = new("../VisualRoot");
    [Export] public NodePath EffectsPath { get; set; } = new("../VisualRoot/Effects");
    [Export] public PresentationProfile Profile { get; set; } = new();

    private BellBackLaborer _actor;
    private IEntityVisual2D _visual;
    private Node2D _effects;

    public override void _Ready()
    {
        _actor = GetNode<BellBackLaborer>(ActorPath);
        _effects = GetNode<Node2D>(EffectsPath);
        Profile ??= new PresentationProfile();
        _visual = Profile.Resolve(GetNode(VisualRootPath));
        _actor.FrameChanged += OnFrame;
        _actor.PresentationCueRequested += OnCue;
    }

    public override void _ExitTree()
    {
        if (!IsInstanceValid(_actor)) return;
        _actor.FrameChanged -= OnFrame;
        _actor.PresentationCueRequested -= OnCue;
    }

    private void OnFrame(BellBackLaborerFrame frame, int facing)
    {
        EntityVisualState state = frame.State switch
        {
            BellBackLaborerState.Approach => EntityVisualState.Move,
            BellBackLaborerState.Telegraph => EntityVisualState.Attack,
            BellBackLaborerState.Attack => EntityVisualState.Attack,
            BellBackLaborerState.Exposed => EntityVisualState.Hurt,
            BellBackLaborerState.Dead => EntityVisualState.Dead,
            _ => EntityVisualState.Idle
        };
        if (_visual is IBellBackLaborerVisual typed) typed.ApplyLaborerFrame(frame, facing);
        else if (_visual is BellBackLaborerFormalVisual formal) formal.ApplyLaborerFrame(frame, facing);
        else if (_visual is BellBackLaborerPlaceholderVisual placeholder) placeholder.ApplyLaborerFrame(frame, facing);
        else _visual?.ApplyFrame(new EntityPresentationFrame(state, facing < 0 ? -1 : 1, true));
    }

    private void OnCue(PresentationCue cue)
    {
        _visual?.PlayCue(cue);
        PrologueVfxDirector.PlayFromTree(this, cue, _actor?.GlobalPosition ?? Vector2.Zero,
            new Vector2(_actor?.Facing ?? 1, 0));
        if (_effects == null || !Profile.TryInstantiateCue(cue, out Node2D effect)) return;
        _effects.AddChild(effect);
    }
}
