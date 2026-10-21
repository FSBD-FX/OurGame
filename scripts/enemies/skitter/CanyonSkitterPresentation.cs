using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Enemies.Skitter;

public partial class CanyonSkitterPresentation : Node
{
    private CanyonSkitter _actor;
    private CanyonSkitterVisual _visual;

    public override void _Ready()
    {
        _actor = GetParentOrNull<CanyonSkitter>();
        _visual = _actor?.GetNodeOrNull<CanyonSkitterVisual>("VisualRoot/SkitterVisual");
        if (_actor == null) return;
        _actor.FrameChanged += OnFrame;
        _actor.AttackFrameChanged += OnAttackFrame;
        _actor.PresentationCueRequested += OnCue;
        OnFrame(_actor.CurrentFrame);
    }

    public override void _ExitTree()
    {
        ReleaseTypedState();
        if (!IsInstanceValid(_actor)) return;
        _actor.FrameChanged -= OnFrame;
        _actor.AttackFrameChanged -= OnAttackFrame;
        _actor.PresentationCueRequested -= OnCue;
    }

    private void OnFrame(CanyonSkitterFrame frame)
    {
        if (!IsInstanceValid(_visual)) { _visual = null; return; }
        _visual.ApplySkitterFrame(frame);
    }

    private void OnAttackFrame(AttackPresentationFrame frame)
    {
        if (!IsInstanceValid(_actor)) return;
        if (frame.Timeline.Phase == AttackPhase.None)
        {
            ReleaseTypedState();
            return;
        }
        if (GetTree()?.GetFirstNodeInGroup("prologue_vfx") is PrologueVfxDirector vfx)
            vfx.PresentAttackFrame(_actor, frame);
        if (GetTree()?.GetFirstNodeInGroup(PrologueAudioDirector.AudioGroup) is PrologueAudioDirector audio)
            audio.PlayAttackFrame(_actor, frame, frame.Shape.Origin);
    }

    private void OnCue(PresentationCue cue)
    {
        if (IsInstanceValid(_visual)) _visual.PlayCue(cue); else _visual = null;
        PrologueVfxDirector.PlayFromTree(this, cue, _actor?.GlobalPosition ?? Vector2.Zero,
            new Vector2(_actor?.CurrentFrame.Side ?? 1, 0f));
    }

    private void ReleaseTypedState()
    {
        if (!IsInstanceValid(_actor) || !IsInsideTree()) return;
        if (GetTree()?.GetFirstNodeInGroup("prologue_vfx") is PrologueVfxDirector vfx)
            vfx.ReleaseAttackSource(_actor);
        if (GetTree()?.GetFirstNodeInGroup(PrologueAudioDirector.AudioGroup) is PrologueAudioDirector audio)
            audio.ReleaseAttackSource(_actor);
    }
}
