using System;
using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Enemies.Skitter;

public partial class CanyonSkitterVisual : Node2D, IEntityVisual2D
{
    private CanyonSkitterState _state;
    private int _side = 1;
    private float _gait;
    private float _charge;
    private float _flash;
    private float _speed;

    public void ApplySkitterFrame(CanyonSkitterFrame frame)
    {
        _state = frame.State;
        if (frame.Side != 0) _side = frame.Side;
        _speed = frame.MoveSpeed;
        _charge = frame.AttackTimeline.Phase == AttackPhase.Telegraph &&
            frame.AttackTimeline.PhaseDurationSeconds > 0f
            ? 1f - frame.AttackTimeline.PhaseRemainingSeconds /
                frame.AttackTimeline.PhaseDurationSeconds : 0f;
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame) => QueueRedraw();

    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _flash = .18f;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float step = double.IsFinite(delta) && delta > 0d && delta < .25d ? (float)delta : 0f;
        if (_state is CanyonSkitterState.Rush or CanyonSkitterState.Retreat)
            _gait += MathF.Abs(_speed) * step * .095f;
        if (_state == CanyonSkitterState.Falling)
            Rotation += step * 2.5f;
        else Rotation = Mathf.MoveToward(Rotation, 0f, step * 5f);
        _flash = MathF.Max(0f, _flash - step);
        if (_state is CanyonSkitterState.Telegraph or CanyonSkitterState.Rush or
            CanyonSkitterState.Retreat or CanyonSkitterState.Falling || _flash > 0f)
            QueueRedraw();
    }

    public override void _Draw()
    {
        float bodyY = _state switch
        {
            CanyonSkitterState.Telegraph => -8f + _charge * 4f,
            CanyonSkitterState.Rush => -15f,
            CanyonSkitterState.Retreat => -12f,
            CanyonSkitterState.Stunned => -4f,
            CanyonSkitterState.Dead => 2f,
            _ => -12f
        };
        float stride = _state switch
        {
            CanyonSkitterState.Rush => 9f,
            CanyonSkitterState.Retreat => 6f,
            CanyonSkitterState.Telegraph => 2f,
            _ => 1f
        };
        Color farLeg = new("756250");
        Color nearLeg = new("4b3c34");
        for (int layer = 0; layer < 2; layer++)
        {
            for (int leg = 0; leg < 3; leg++)
            {
                float hipX = (leg - 1) * 10f + (layer == 0 ? 2f : -2f);
                float wave = MathF.Sin(_gait + leg * 2.2f + layer * MathF.PI);
                float footX = hipX + (leg - 1) * 9f + wave * stride;
                float footY = _state == CanyonSkitterState.Telegraph ? -1f :
                    _state == CanyonSkitterState.Stunned ? 4f :
                    _state == CanyonSkitterState.Falling ? 12f + wave * 5f :
                    MathF.Max(-5f, -MathF.Abs(wave) * stride * .45f);
                Vector2 hip = new(hipX, bodyY + 4f);
                Vector2 knee = new((hipX + footX) * .5f + (leg - 1) * 3f,
                    bodyY + 12f + (layer == 0 ? 2f : 0f));
                Vector2 foot = new(footX, footY);
                Color ink = layer == 0 ? farLeg : nearLeg;
                DrawLine(hip, knee, ink, layer == 0 ? 2.5f : 3.5f, true);
                DrawLine(knee, foot, ink, layer == 0 ? 2f : 3f, true);
                DrawCircle(knee, 2.2f, ink);
            }
            if (layer == 0)
            {
                Color underside = _state == CanyonSkitterState.Dead
                    ? new Color("514c4a") : new Color("56443a");
                DrawPolygon([
                    new Vector2(-20f, bodyY + 1f), new Vector2(-13f, bodyY - 5f),
                    new Vector2(12f, bodyY - 5f), new Vector2(20f, bodyY + 1f),
                    new Vector2(11f, bodyY + 8f), new Vector2(-14f, bodyY + 8f)
                ], [underside]);
                Color shell = _flash > 0f ? new Color("fff1cf") :
                    _state == CanyonSkitterState.Dead ? new Color("807d78") :
                    new Color("d1c6a7");
                DrawPolygon([
                    new Vector2(-15f, bodyY - 3f), new Vector2(-9f, bodyY - 10f),
                    new Vector2(9f, bodyY - 10f), new Vector2(15f, bodyY - 3f),
                    new Vector2(5f, bodyY - 6f), new Vector2(-5f, bodyY - 6f)
                ], [shell]);
                DrawLine(new Vector2(-8f, bodyY - 8f), new Vector2(8f, bodyY - 8f),
                    new Color("f1e8cb"), 1.5f, true);
                DrawCircle(new Vector2(_side * 15f, bodyY - 2f), 3f,
                    _state == CanyonSkitterState.Dead ? new Color("5f493b") : new Color("e59b4a"));
                if (_state == CanyonSkitterState.Telegraph)
                    DrawLine(new Vector2(_side * 19f, bodyY + 2f),
                        new Vector2(_side * (25f + 5f * _charge), bodyY + 4f),
                        new Color("f5b259"), 2.5f, true);
            }
        }
    }
}
