using Godot;
using Knight.Abilities;

namespace Knight.Ui;

public partial class PrologueThreadSpoolVisual : Control
{
    public float ReadyRatio { get; private set; } = 1f;
    public ThreadCastState DisplayState { get; private set; } = ThreadCastState.Idle;
    public ThreadFailureReason DisplayFailure { get; private set; } = ThreadFailureReason.None;
    public string StateGlyph => DisplayState switch
    {
        ThreadCastState.Preview => "◎",
        ThreadCastState.Windup => "◇",
        ThreadCastState.Travel => "➜",
        ThreadCastState.Latched => "◆",
        ThreadCastState.Apply => "↔",
        ThreadCastState.Recovery => "◌",
        ThreadCastState.Failed => "×",
        _ => ReadyRatio >= .999f ? "●" : "◔"
    };

    public void SetRatio(float ratio)
    {
        ReadyRatio = Mathf.Clamp(ratio, 0f, 1f);
        QueueRedraw();
    }

    public void SetState(ThreadCastState state, ThreadFailureReason failure)
    {
        DisplayState = state;
        DisplayFailure = failure;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float scale = Mathf.Max(.5f, Size.Y / 72f);
        Vector2 center = new(36f * scale, Size.Y * .5f);
        DrawCircle(center, 31f * scale, new Color("101820"));
        DrawArc(center, 31f * scale, -.5f * Mathf.Pi, (-.5f + 2f * ReadyRatio) * Mathf.Pi,
            40, new Color("d4a83f"), 5f * scale, true);
        DrawCircle(center, 17f * scale, new Color("24353a"));
        DrawStateShape(center, scale);
        for (int index = 0; index < 4; index++)
        {
            float angle = index * Mathf.Pi * .5f;
            Vector2 direction = Vector2.Right.Rotated(angle);
            DrawLine(center + direction * 18f * scale, center + direction * 27f * scale,
                new Color("7d6731"), 2f * scale);
        }
    }

    private void DrawStateShape(Vector2 center, float scale)
    {
        Color active = DisplayState == ThreadCastState.Failed
            ? new Color("f08a72")
            : ReadyRatio >= .999f ? new Color("9edbd2") : new Color("708d8b");
        switch (DisplayState)
        {
            case ThreadCastState.Preview:
                DrawArc(center, 10f * scale, 0f, Mathf.Tau, 24, active, 2f * scale);
                DrawLine(center + Vector2.Left * 13f * scale,
                    center + Vector2.Right * 13f * scale, active, 2f * scale);
                DrawLine(center + Vector2.Up * 13f * scale,
                    center + Vector2.Down * 13f * scale, active, 2f * scale);
                break;
            case ThreadCastState.Windup:
            case ThreadCastState.Latched:
                Vector2[] diamond =
                [center + Vector2.Up * 10f * scale, center + Vector2.Right * 10f * scale,
                 center + Vector2.Down * 10f * scale, center + Vector2.Left * 10f * scale];
                if (DisplayState == ThreadCastState.Latched)
                    DrawColoredPolygon(diamond, active);
                else
                    DrawPolyline([diamond[0], diamond[1], diamond[2], diamond[3], diamond[0]],
                        active, 2f * scale);
                break;
            case ThreadCastState.Travel:
                DrawLine(center + Vector2.Left * 11f * scale,
                    center + Vector2.Right * 7f * scale, active, 3f * scale);
                DrawColoredPolygon([center + Vector2.Right * 12f * scale,
                    center + new Vector2(4f, -7f) * scale,
                    center + new Vector2(4f, 7f) * scale], active);
                break;
            case ThreadCastState.Apply:
                DrawLine(center + Vector2.Left * 10f * scale,
                    center + Vector2.Right * 10f * scale, active, 3f * scale);
                DrawCircle(center, 4f * scale, active);
                break;
            case ThreadCastState.Recovery:
                DrawArc(center, 10f * scale, -.25f * Mathf.Pi, 1.5f * Mathf.Pi, 20,
                    active, 3f * scale);
                break;
            case ThreadCastState.Failed:
                DrawLine(center + new Vector2(-8, -8) * scale,
                    center + new Vector2(8, 8) * scale, active, 3f * scale);
                DrawLine(center + new Vector2(8, -8) * scale,
                    center + new Vector2(-8, 8) * scale, active, 3f * scale);
                break;
            default:
                DrawCircle(center, 9f * scale, active);
                break;
        }
    }
}
