using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Enemies.Ranged;

public partial class RangedWeaponPresentation : Node
{
    private RangedWeaponEnemy _actor;
    private Node2D _visualNode;
    private IRangedWeaponVisual2D _visual;
    private PrologueAudioDirector _audio;
    private ulong _attackId;
    private AttackPhase _phase;

    public override void _Ready()
    {
        _actor = GetParentOrNull<RangedWeaponEnemy>();
        _visualNode = _actor?.GetNodeOrNull<Node2D>("VisualRoot/WeaponVisual");
        _visual = _visualNode as IRangedWeaponVisual2D;
        if (_actor == null) return;
        _actor.FrameChanged += OnFrame;
        _actor.PresentationResetRequested += OnReset;
        _actor.AttackFrameChanged += OnAttackFrame;
        _actor.PresentationCueRequested += OnCue;
        _actor.ShotEmitted += OnShot;
        ResolveAudio();
        OnReset();
    }

    public override void _ExitTree()
    {
        ReleaseTypedState();
        if (!IsInstanceValid(_actor)) return;
        _actor.FrameChanged -= OnFrame;
        _actor.PresentationResetRequested -= OnReset;
        _actor.AttackFrameChanged -= OnAttackFrame;
        _actor.PresentationCueRequested -= OnCue;
        _actor.ShotEmitted -= OnShot;
    }

    private void OnFrame(RangedWeaponFrame frame)
    {
        if (!VisualAvailable) return;
        if (frame.State is RangedWeaponState.Telegraph or RangedWeaponState.Active or RangedWeaponState.Recovery &&
            (!IsInstanceValid(_actor) || !_actor.HasPresentationAuthority)) return;
        _visual.PresentWeaponFrame(_actor.Role, frame);
    }

    private bool VisualAvailable => _visual != null && IsInstanceValid(_visualNode) &&
        _visualNode.IsInsideTree() && !_visualNode.IsQueuedForDeletion() && _visualNode.CanProcess();
    private void OnReset()
    {
        ReleaseTypedState();
        if (_visual != null && IsInstanceValid(_visualNode) && IsInstanceValid(_actor))
            _visual.ResetWeaponPresentation(_actor.Role);
        OnFrame(_actor.CurrentFrame);
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
        if (VisualAvailable) _visual.PlayCue(_phase switch
        {
            AttackPhase.Telegraph => PresentationCue.AttackTelegraph,
            AttackPhase.Active => PresentationCue.AttackStarted,
            _ => PresentationCue.RecoveryStarted
        });
    }

    private void OnCue(PresentationCue cue)
    {
        if (VisualAvailable) _visual.PlayCue(cue);
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
        if (_visual != null && IsInstanceValid(_visualNode)) _visual.ClearAttackPresentation();
        if (IsInstanceValid(_audio) && IsInstanceValid(_actor)) _audio.ReleaseAttackSource(_actor);
        if (!IsInstanceValid(_actor) || !IsInsideTree()) return;
        if (GetTree()?.GetFirstNodeInGroup("prologue_vfx") is PrologueVfxDirector vfx)
            vfx.ReleaseAttackSource(_actor);
    }
}
