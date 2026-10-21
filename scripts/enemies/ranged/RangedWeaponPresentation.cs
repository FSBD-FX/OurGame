using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Enemies.Ranged;

public partial class RangedWeaponPresentation : Node
{
    private RangedWeaponEnemy _actor;
    private RangedWeaponVisual _visual;
    private PrologueAudioDirector _audio;
    private ulong _attackId;
    private AttackPhase _phase;

    public override void _Ready()
    {
        _actor = GetParentOrNull<RangedWeaponEnemy>();
        _visual = _actor?.GetNodeOrNull<RangedWeaponVisual>("VisualRoot/WeaponVisual");
        if (_actor == null) return;
        _actor.FrameChanged += OnFrame;
        _actor.AttackFrameChanged += OnAttackFrame;
        _actor.PresentationCueRequested += OnCue;
        _actor.ShotEmitted += OnShot;
        ResolveAudio();
        OnFrame(_actor.CurrentFrame);
    }

    public override void _ExitTree()
    {
        ReleaseTypedState();
        if (!IsInstanceValid(_actor)) return;
        _actor.FrameChanged -= OnFrame;
        _actor.AttackFrameChanged -= OnAttackFrame;
        _actor.PresentationCueRequested -= OnCue;
        _actor.ShotEmitted -= OnShot;
    }

    private void OnFrame(RangedWeaponFrame frame)
    {
        if (IsInstanceValid(_visual)) _visual.ApplyWeaponFrame(frame);
    }

    private void OnAttackFrame(AttackPresentationFrame frame)
    {
        if (!IsInstanceValid(_actor)) return;
        if (frame.Timeline.Phase == AttackPhase.None) { ReleaseTypedState(); return; }
        // Geometry refreshes each frame; phase cues happen once. Actual rounds
        // are exposed by actor.ShotEmitted, not by one Burst Active audio cue.
        if (GetTree()?.GetFirstNodeInGroup("prologue_vfx") is PrologueVfxDirector vfx)
            vfx.PresentAttackFrame(_actor, frame);
        ResolveAudio()?.PlayAttackFrame(_actor, frame, frame.Shape.Origin);
        if (_attackId == frame.Timeline.AttackId && _phase == frame.Timeline.Phase) return;
        _attackId = frame.Timeline.AttackId;
        _phase = frame.Timeline.Phase;
        if (IsInstanceValid(_visual)) _visual.PlayCue(_phase switch
        {
            AttackPhase.Telegraph => PresentationCue.AttackTelegraph,
            AttackPhase.Active => PresentationCue.AttackStarted,
            _ => PresentationCue.RecoveryStarted
        });
    }

    private void OnCue(PresentationCue cue)
    {
        if (IsInstanceValid(_visual)) _visual.PlayCue(cue);
        if (IsInstanceValid(_actor))
        {
            PrologueVfxDirector.PlayFromTree(this, cue, _actor.GlobalPosition, _actor.CurrentFrame.AimDirection);
            PrologueAudioDirector.PlayPresentationCueFromTree(this, cue, _actor.GlobalPosition);
        }
    }

    private void OnShot(ulong ordinal, Vector2 origin) =>
        ResolveAudio()?.TryPlayRangedShot(_actor, ordinal, origin);

    private PrologueAudioDirector ResolveAudio()
    {
        if (IsInstanceValid(_audio) && _audio.IsInsideTree()) return _audio;
        _audio = IsInsideTree()
            ? GetTree()?.GetFirstNodeInGroup(PrologueAudioDirector.AudioGroup) as PrologueAudioDirector
            : null;
        return _audio;
    }

    private void ReleaseTypedState()
    {
        _attackId = 0;
        _phase = AttackPhase.None;
        if (IsInstanceValid(_audio) && IsInstanceValid(_actor)) _audio.ReleaseAttackSource(_actor);
        if (!IsInstanceValid(_actor) || !IsInsideTree()) return;
        if (GetTree()?.GetFirstNodeInGroup("prologue_vfx") is PrologueVfxDirector vfx)
            vfx.ReleaseAttackSource(_actor);
    }
}
