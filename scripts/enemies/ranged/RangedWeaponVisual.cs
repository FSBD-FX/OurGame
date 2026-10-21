using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Enemies.Ranged;

public partial class RangedWeaponVisual : Node2D, IEntityVisual2D
{
    [Export] public RangedWeaponRole Role { get; set; }
    private RangedWeaponState _state;
    private Vector2 _aim = Vector2.Right;
    private float _charge;
    private float _heat;
    private float _recoil;
    private float _hurt;

    public void ApplyWeaponFrame(RangedWeaponFrame frame)
    {
        _state = frame.State;
        if (!frame.AimDirection.IsZeroApprox()) _aim = frame.AimDirection.Normalized();
        float duration = frame.AttackTimeline.PhaseDurationSeconds;
        float progress = duration > 0f ? 1f - frame.AttackTimeline.PhaseRemainingSeconds / duration : 0f;
        _charge = frame.State == RangedWeaponState.Telegraph ? Mathf.Clamp(progress, 0f, 1f) : 0f;
        _heat = frame.State == RangedWeaponState.Active ? 1f :
            frame.State == RangedWeaponState.Recovery ? 1f - Mathf.Clamp(progress, 0f, 1f) : 0f;
        if (frame.EmissionsDue > 0) _recoil = 1f;
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame) => QueueRedraw();
    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _hurt = .16f;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float seconds = double.IsFinite(delta) && delta > 0d && delta < 1d ? (float)delta : 0f;
        _recoil = Mathf.Max(0f, _recoil - seconds * 6f);
        _hurt = Mathf.Max(0f, _hurt - seconds);
        if (_recoil > 0f || _hurt > 0f) QueueRedraw();
    }

    public override void _Draw()
    {
        // Functional silhouettes only: these are not final faction-family art.
        Color steel = new("677278");
        Color dark = new("29363e");
        Color trim = Role switch
        {
            RangedWeaponRole.Scatter => new Color("be8b49"),
            RangedWeaponRole.Burst => new Color("819d70"),
            RangedWeaponRole.Missile => new Color("c26b46"),
            _ => new Color("86c9d8")
        };
        if (_state == RangedWeaponState.Stunned) trim = new Color("a784c4");
        if (_state == RangedWeaponState.Dead) { steel = new Color("454c50"); trim = new Color("5b5350"); }
        if (_hurt > 0f) steel = new Color("d1a68b");
        float collapse = _state == RangedWeaponState.Dead ? 13f : 0f;
        DrawLine(new Vector2(-13f, 8f + collapse), new Vector2(-22f, 29f), dark, 9f, true);
        DrawLine(new Vector2(12f, 8f + collapse), new Vector2(22f, 29f), dark, 9f, true);
        DrawLine(new Vector2(-28f, 30f), new Vector2(-12f, 30f), trim, 5f, true);
        DrawLine(new Vector2(12f, 30f), new Vector2(29f, 30f), trim, 5f, true);
        DrawPolygon([new(-24f, 11f + collapse), new(-22f, -16f + collapse),
            new(-11f, -27f + collapse), new(12f, -27f + collapse),
            new(24f, -14f + collapse), new(23f, 11f + collapse)], [steel]);
        DrawRect(new Rect2(-12f, -25f + collapse, 25f, 17f), dark);
        DrawLine(new Vector2(-8f, -17f + collapse), new Vector2(8f, -17f + collapse), trim, 4f, true);
        DrawLine(new Vector2(-22f, 5f + collapse), new Vector2(22f, 5f + collapse), trim, 3f, true);
        DrawCircle(new Vector2(-20f, -4f + collapse), 7f, dark);
        DrawSetTransform(new Vector2(-_aim.X * 5f * _recoil, -3f + collapse), _aim.Angle());
        Color hot = trim.Lerp(new Color("e58859"), _heat * .7f);
        switch (Role)
        {
            case RangedWeaponRole.Scatter:
                DrawRect(new Rect2(-13f, -9f, 32f, 22f), dark);
                DrawRect(new Rect2(7f, -12f, 24f, 8f), trim);
                DrawRect(new Rect2(7f, 5f, 24f, 8f), trim);
                DrawLine(new Vector2(31f, -12f), new Vector2(31f, 13f), hot, 4f, true);
                DrawCircle(new Vector2(-7f, 2f), 8f, hot);
                break;
            case RangedWeaponRole.Burst:
                DrawRect(new Rect2(-14f, -8f, 35f, 17f), dark);
                DrawRect(new Rect2(13f, -4f, 30f, 8f), trim);
                DrawRect(new Rect2(36f, -7f, 9f, 14f), hot);
                for (int i = 0; i < 6; i++)
                    DrawCircle(new Vector2(-9f + i * 4f, 14f + i % 2 * 4f), 3f, trim);
                DrawRect(new Rect2(-7f, -16f, 12f, 8f), hot);
                break;
            case RangedWeaponRole.Missile:
                DrawRect(new Rect2(-21f, -15f, 48f, 28f), dark);
                DrawRect(new Rect2(-19f, -12f, 39f, 22f), trim);
                DrawCircle(new Vector2(25f, -1f), 14f, hot);
                DrawCircle(new Vector2(25f, -1f), 9f, dark);
                for (int i = 0; i < 4; i++)
                    DrawLine(new Vector2(-14f + i * 8f, -10f), new Vector2(-14f + i * 8f, 8f), dark, 3f, true);
                DrawLine(new Vector2(-22f, -18f), new Vector2(-12f, -25f), hot, 5f, true);
                break;
            case RangedWeaponRole.Laser:
                DrawPolygon([new(-17f, -11f), new(6f, -16f), new(22f, 0f), new(6f, 16f), new(-17f, 11f)], [dark]);
                DrawPolyline([new(-8f, -8f), new(7f, -12f), new(18f, 0f), new(7f, 12f), new(-8f, 8f)], hot, 3f, true);
                DrawLine(new Vector2(14f, -13f), new Vector2(35f, -8f), trim, 5f, true);
                DrawLine(new Vector2(14f, 13f), new Vector2(35f, 8f), trim, 5f, true);
                DrawCircle(new Vector2(12f, 0f), 5f + _charge * 4f, hot);
                break;
        }
        if (_charge > 0f)
            DrawArc(new Vector2(8f, 0f), 21f, -Mathf.Pi * .5f, -Mathf.Pi * .5f + Mathf.Tau * _charge,
                30, _charge > .5f ? new Color("d35c4b") : new Color("e6bd62"), 2f, true);
        if (_state == RangedWeaponState.Stunned)
            DrawPolyline([new(-13f, -35f), new(-6f, -41f), new(2f, -35f), new(10f, -41f)], trim, 3f, true);
        DrawSetTransform(Vector2.Zero);
    }
}

// A purely cosmetic, owner-managed afterimage. It is never an attack leaf and
// never delivers damage. Inherited process mode freezes it during game pause.
public partial class RangedWeaponImpactRing : Node2D
{
    public float Radius { get; set; } = 56f;
    private float _remaining = .22f;
    public override void _Ready() => QueueRedraw();
    public override void _PhysicsProcess(double delta)
    {
        if (double.IsFinite(delta) && delta > 0d && delta < 1d) _remaining -= (float)delta;
        if (_remaining <= 0f) { QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float ratio = Mathf.Clamp(_remaining / .22f, 0f, 1f);
        Color rim = new("e39253") { A = ratio * .8f };
        DrawArc(Vector2.Zero, Radius * (1f + (1f - ratio) * .12f), 0f, Mathf.Tau, 48, rim, 3f, true);
        rim.A *= .35f;
        DrawCircle(Vector2.Zero, Radius * .72f, rim);
    }
}
