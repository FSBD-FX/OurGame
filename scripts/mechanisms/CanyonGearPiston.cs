using Godot;
using Knight.Abilities;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonGearPiston : AnimatableBody2D, ICanyonTelegraphSource, ICheckpointResettable
{
    [Export] public float SafeSeconds { get; set; } = 1f;
    [Export] public float TelegraphSeconds { get; set; } = .5f;
    [Export] public float ActiveSeconds { get; set; } = .5f;
    [Export] public float RecoverySeconds { get; set; } = .5f;
    [Export] public Vector2 RetractedPosition { get; set; }
    [Export] public Vector2 ExtendedPosition { get; set; } = new(0f, -96f);
    [Export] public Rect2 RouteBounds { get; set; } = new(-128f, -128f, 256f, 256f);
    [Export] public NodePath TelegraphPath { get; set; } = new("Telegraph");
    public CanyonMechanismFrame Frame { get; private set; } = new(false, false, true, 0f);
    public bool IsDangerTelegraphActive => Frame.Telegraphing;
    public float ElapsedSeconds { get; private set; }
    public CollisionShape2D Collision => GetNode<CollisionShape2D>("Collision");
    public override void _Ready()
    {
        SyncToPhysics = true;
        CollisionLayer = Knight.Combat.CombatCollisionLayers.World;
        CollisionMask = Knight.Combat.CombatCollisionLayers.None;
        Position = RetractedPosition;
        GrappleSurface2D profile = GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
        if (profile != null) profile.Material = GrappleSurfaceMaterial.Forbidden;
        ApplyFrame(Evaluate(0f, SafeSeconds, TelegraphSeconds, ActiveSeconds, RecoverySeconds));
    }

    public override void _PhysicsProcess(double delta) => Advance((float)delta);

    public static CanyonMechanismFrame Evaluate(float elapsed, float safe, float telegraph,
        float active, float recovery)
    {
        safe = Mathf.Max(.01f, safe); telegraph = Mathf.Max(.01f, telegraph);
        active = Mathf.Max(.01f, active); recovery = Mathf.Max(.01f, recovery);
        float cycle = safe + telegraph + active + recovery;
        float t = Mathf.PosMod(Mathf.Max(0f, elapsed), cycle);
        if (t < safe) return new(false, false, true, 0f);
        t -= safe;
        if (t < telegraph) return new(true, false, true, t / telegraph);
        t -= telegraph;
        if (t < active) return new(false, true, false, 1f);
        t -= active;
        return new(false, false, true, 1f - t / recovery);
    }

    public void AdvanceForTest(float delta) => Advance(delta);

    public void ResetForCheckpoint()
    {
        ElapsedSeconds = 0f;
        ApplyFrame(Evaluate(0f, SafeSeconds, TelegraphSeconds, ActiveSeconds, RecoverySeconds));
    }

    private void Advance(float delta)
    {
        if (GetTree()?.Paused == true || !float.IsFinite(delta) || delta <= 0f) return;
        ElapsedSeconds += delta;
        ApplyFrame(Evaluate(ElapsedSeconds, SafeSeconds, TelegraphSeconds, ActiveSeconds, RecoverySeconds));
    }

    private void ApplyFrame(CanyonMechanismFrame frame)
    {
        Frame = frame;
        Position = RetractedPosition.Lerp(ExtendedPosition, frame.Phase01);
        if (!RouteBounds.HasPoint(Position)) Position = RetractedPosition;
        Collision.Disabled = !frame.HazardActive;
        CanvasItem telegraph = GetNodeOrNull<CanvasItem>(TelegraphPath);
        if (telegraph != null) telegraph.Visible = frame.Telegraphing;
    }
}
