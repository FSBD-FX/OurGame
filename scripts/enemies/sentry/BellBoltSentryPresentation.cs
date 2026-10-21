using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Enemies.Sentry;

public partial class BellBoltSentryPresentation : Node
{
    private BellBoltSentry _actor;
    private BellBoltSentryVisual _visual;

    public override void _Ready()
    {
        _actor = GetParentOrNull<BellBoltSentry>();
        _visual = _actor?.GetNodeOrNull<BellBoltSentryVisual>("VisualRoot/SentryVisual");
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

    private void OnFrame(BellBoltSentryFrame frame)
    {
        if (!IsInstanceValid(_visual)) { _visual = null; return; }
        _visual.ApplySentryFrame(frame);
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
            _actor?.CurrentFrame.AimDirection ?? Vector2.Right);
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
