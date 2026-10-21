using Godot;
using Knight.Abilities;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonPendulumHazard : AnimatableBody2D, ICanyonTelegraphSource, ICheckpointResettable
{
    [Export] public float PeriodSeconds { get; set; } = 2f;
    [Export] public float MaxAngleDegrees { get; set; } = 55f;
    [Export] public NodePath TelegraphPath { get; set; } = new("Telegraph");
    public CanyonMechanismFrame Frame { get; private set; } = new(false, false, true, 0f);
    public bool IsDangerTelegraphActive => Frame.Telegraphing;
    public float ElapsedSeconds { get; private set; }
    public float VisibleAngleRadians { get; private set; }

    public override void _Ready()
    {
        SyncToPhysics = true;
        CollisionLayer = Knight.Combat.CombatCollisionLayers.World;
        CollisionMask = Knight.Combat.CombatCollisionLayers.None;
        GrappleSurface2D profile = GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
        if (profile != null) profile.Material = GrappleSurfaceMaterial.Forbidden;
        ApplyState();
    }

    public override void _PhysicsProcess(double delta) => Advance((float)delta);

    public static CanyonMechanismFrame Evaluate(float elapsed, float period)
    {
        period = Mathf.Max(.01f, period);
        float phase = Mathf.PosMod(elapsed, period) / period;
        float speed = Mathf.Abs(Mathf.Sin(phase * Mathf.Tau));
        bool danger = speed >= .98f;
        bool warning = !danger && speed >= .90f;
        return new(warning, danger, !danger, phase);
    }

    public void AdvanceForTest(float delta) => Advance(delta);

    public void ResetForCheckpoint()
    {
        ElapsedSeconds = 0f;
        ApplyState();
    }

    private void Advance(float delta)
    {
        if (GetTree()?.Paused == true || !float.IsFinite(delta) || delta <= 0f) return;
        ElapsedSeconds += delta;
        ApplyState();
    }

    private void ApplyState()
    {
        Frame = Evaluate(ElapsedSeconds, PeriodSeconds);
        float phase = Mathf.PosMod(ElapsedSeconds, Mathf.Max(.01f, PeriodSeconds)) / Mathf.Max(.01f, PeriodSeconds);
        VisibleAngleRadians = Mathf.DegToRad(MaxAngleDegrees) * Mathf.Cos(phase * Mathf.Tau);
        Rotation = VisibleAngleRadians;
        CanvasItem telegraph = GetNodeOrNull<CanvasItem>(TelegraphPath);
        if (telegraph != null) telegraph.Visible = Frame.Telegraphing;
    }
}
