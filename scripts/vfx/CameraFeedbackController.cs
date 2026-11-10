using Godot;

namespace Knight.Vfx;

public partial class CameraFeedbackController : Node
{
    [Export] public float MaxIntensity { get; set; } = 12f;
    [Export] public float MaxDurationSeconds { get; set; } = .35f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float IntensityScale { get; set; } = 1f;

    public CameraFeedback LastApplied { get; private set; }

    private Camera2D _camera;
    private Vector2 _restOffset;
    private float _remaining;
    private float _elapsed;

    public void BindCamera(Camera2D camera)
    {
        _camera = camera;
        _restOffset = camera?.Offset ?? Vector2.Zero;
    }

    public CameraFeedback Request(CameraFeedback request)
    {
        float intensity = Mathf.Clamp(request.Intensity, 0f, Mathf.Max(0f, MaxIntensity)) *
            Mathf.Clamp(IntensityScale, 0f, 1f);
        float duration = Mathf.Clamp(request.DurationSeconds, 0f, Mathf.Max(0f, MaxDurationSeconds));
        LastApplied = new CameraFeedback(intensity, duration);
        _remaining = duration;
        _elapsed = 0f;
        return LastApplied;
    }

    public override void _Process(double delta)
    {
        if (_camera == null || !IsInstanceValid(_camera)) return;
        if (_remaining <= 0f)
        {
            _camera.Offset = _restOffset;
            return;
        }

        float step = (float)delta;
        _elapsed += step;
        _remaining = Mathf.Max(0f, _remaining - step);
        float fade = LastApplied.DurationSeconds <= 0f ? 0f : _remaining / LastApplied.DurationSeconds;
        float x = Mathf.Sin(_elapsed * 91f) * LastApplied.Intensity * fade;
        float y = Mathf.Cos(_elapsed * 73f) * LastApplied.Intensity * .65f * fade;
        _camera.Offset = _restOffset + new Vector2(x, y);
    }

    public void ResetFeedback()
    {
        _remaining = 0f;
        _elapsed = 0f;
        LastApplied = default;
        if (_camera != null && IsInstanceValid(_camera)) _camera.Offset = _restOffset;
    }
}
