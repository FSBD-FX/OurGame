using Godot;

namespace Knight.Prologue.Guidance;

public partial class CanyonFireflyGuidanceView : Node2D
{
    public Vector2 FromWorldPosition { get; private set; }
    public Vector2 TargetWorldPosition { get; private set; }
    private float _activeSeconds;
    private float _visibleAge;
    private float _remainingSeconds;

    public override void _Ready()
    {
        Visible = false;
        ZIndex = 45;
    }

    public override void _Process(double delta)
    {
        if (!Visible || !double.IsFinite(delta) || delta<=0 || GetTree().Paused) return;
        float elapsed=(float)System.Math.Min(delta,.1);
        _activeSeconds+=elapsed;
        _visibleAge+=elapsed;
        QueueRedraw();
    }

    public void Present(Vector2 fromWorldPosition, Vector2 targetWorldPosition,
        float remainingSeconds=CanyonFireflyGuidanceModel.VisibleDurationSeconds)
    {
        if (!Visible) _visibleAge=0f;
        FromWorldPosition = fromWorldPosition;
        TargetWorldPosition = targetWorldPosition;
        _remainingSeconds=float.IsFinite(remainingSeconds) ? Mathf.Max(0f,remainingSeconds) : 0f;
        Visible = true;
        QueueRedraw();
    }

    public void Clear()
    {
        Visible = false;
        FromWorldPosition = default;
        TargetWorldPosition = default;
        _visibleAge=0f;
        _remainingSeconds=0f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Visible) return;
        Vector2 start = ToLocal(FromWorldPosition);
        Vector2 end = ToLocal(TargetWorldPosition);
        Vector2 delta = end - start;
        if (delta.LengthSquared() < 1f) return;
        Vector2 bend = delta.Orthogonal().Normalized() * Mathf.Min(72f, delta.Length() * .18f);
        Vector2 control = start.Lerp(end, .5f) + bend;
        float opacity=Mathf.SmoothStep(0f,.45f,_visibleAge)*
            Mathf.SmoothStep(0f,.65f,_remainingSeconds);
        // A few soft motes briefly suggest the route. No destination ring or
        // moving text competes with attacks; active time freezes on pause.
        for (int index = 0; index < 4; index++)
        {
            float phase=Mathf.PosMod(index/4f+_activeSeconds*.14f,1f);
            float inverse = 1f - phase;
            Vector2 point = inverse * inverse * start + 2f * inverse * phase * control +
                phase * phase * end;
            float pulse=.8f+.2f*Mathf.Sin(_activeSeconds*2f+index*.9f);
            DrawCircle(point, 4f,
                new Color(.48f,.82f,.72f,.13f*pulse*opacity));
            DrawCircle(point,1.2f,new Color(.78f,.91f,.72f,.48f*opacity));
        }
        DrawCircle(end,7f,new Color(.48f,.82f,.72f,.08f*opacity));
    }
}
