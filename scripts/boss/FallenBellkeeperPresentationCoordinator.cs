using Godot;
using Knight.Config;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Boss;

public partial class FallenBellkeeperPresentationCoordinator : Node
{
    [Export] public NodePath BossPath { get; set; } = new("..");
    [Export] public NodePath VisualRootPath { get; set; } = new("../VisualRoot");
    [Export] public NodePath EffectsPath { get; set; } = new("../VisualRoot/Effects");
    [Export] public PresentationProfile Profile { get; set; } = new();

    private FallenBellkeeper _boss;
    private IEntityVisual2D _visual;
    private Node2D _effects;

    public override void _Ready()
    {
        _boss = GetNode<FallenBellkeeper>(BossPath);
        _effects = GetNode<Node2D>(EffectsPath);
        Profile ??= new PresentationProfile();
        _visual = Profile.Resolve(GetNode(VisualRootPath));
        _boss.FrameChanged += OnFrame;
        _boss.PresentationCueRequested += OnCue;
    }

    public override void _ExitTree()
    {
        if (!IsInstanceValid(_boss)) return;
        _boss.FrameChanged -= OnFrame;
        _boss.PresentationCueRequested -= OnCue;
    }

    private void OnFrame(FallenBellkeeperFrame frame, int facing)
    {
        if (_visual is FallenBellkeeperFormalVisual formal)
            formal.ApplyBossFrame(frame, facing);
        else if (_visual is FallenBellkeeperPlaceholderVisual placeholder)
            placeholder.ApplyBossFrame(frame, facing);
        else
            _visual?.ApplyFrame(new EntityPresentationFrame(
                frame.State == FallenBellkeeperState.Defeated ? EntityVisualState.Dead :
                frame.Attack != FallenBellkeeperAttack.None ? EntityVisualState.Attack : EntityVisualState.Idle,
                facing, true));
    }

    private void OnCue(PresentationCue cue)
    {
        _visual?.PlayCue(cue);
        PresentationCue worldCue = cue switch
        {
            PresentationCue.PhaseChanged => PresentationCue.BossTransition,
            PresentationCue.WeakpointOpened => PresentationCue.BossExposed,
            PresentationCue.Died => PresentationCue.BossDefeated,
            _ => cue
        };
        PrologueVfxDirector.PlayFromTree(this, worldCue, _boss?.GlobalPosition ?? Vector2.Zero,
            new Vector2(_boss?.Facing ?? -1, 0));
        if (_effects == null || !Profile.TryInstantiateCue(cue, out Node2D effect)) return;
        _effects.AddChild(effect);
    }
}
