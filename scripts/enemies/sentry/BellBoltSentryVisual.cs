using Godot;
using Knight.Presentation;

namespace Knight.Enemies.Sentry;

public partial class BellBoltSentryVisual : Node2D, IEntityVisual2D
{
    private BellBoltSentryState _state;
    private Vector2 _aim = Vector2.Right;
    private float _charge;
    private float _recoil;

    public void ApplySentryFrame(BellBoltSentryFrame frame)
    {
        _state = frame.State;
        if (!frame.AimDirection.IsZeroApprox()) _aim = frame.AimDirection;
        _charge = frame.AttackTimeline.Phase == Knight.Combat.AttackPhase.Telegraph &&
            frame.AttackTimeline.PhaseDurationSeconds > 0f
            ? 1f - frame.AttackTimeline.PhaseRemainingSeconds / frame.AttackTimeline.PhaseDurationSeconds
            : 0f;
        _recoil = frame.State == BellBoltSentryState.Fire ? 1f : 0f;
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame) => QueueRedraw();
    public void PlayCue(PresentationCue cue) => QueueRedraw();

    public override void _Draw()
    {
        Color shell = _state == BellBoltSentryState.Stunned ? new Color("9f7cc2") : new Color("b9853f");
        if (_state == BellBoltSentryState.Dead) shell = new Color("4e4742");
        DrawPolygon([new Vector2(-27f, 11f), new Vector2(-22f, -15f), new Vector2(-12f, -27f),
            new Vector2(12f, -27f), new Vector2(22f, -15f), new Vector2(27f, 11f)], [shell]);
        DrawArc(Vector2.Zero, 27f, 0f, Mathf.Tau, 32, new Color("e7c574"), 3f, true);
        DrawLine(new Vector2(-18f, 11f), new Vector2(-24f, 28f), new Color("5b4631"), 7f, true);
        DrawLine(new Vector2(0f, 11f), new Vector2(0f, 30f), new Color("5b4631"), 7f, true);
        DrawLine(new Vector2(18f, 11f), new Vector2(24f, 28f), new Color("5b4631"), 7f, true);
        Vector2 aperture = _aim.Normalized() * (18f - 5f * _recoil);
        DrawCircle(aperture, 8f + 3f * _charge, new Color("2a2020"));
        DrawArc(aperture, 11f + 6f * _charge, 0f, Mathf.Tau, 24,
            _charge > .49f ? new Color("d8584b") : new Color("edc65a"), 3f, true);
    }
}
