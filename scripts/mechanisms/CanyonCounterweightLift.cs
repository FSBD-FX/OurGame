using Godot;
using Knight.Abilities;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonCounterweightLift : AnimatableBody2D, ICheckpointResettable,
    IThreadInteractionReceiver
{
    [Export] public Vector2 LowerPosition { get; set; }
    [Export] public Vector2 UpperPosition { get; set; }
    [Export] public float TravelSeconds { get; set; } = 2f;
    [Export] public float EndpointDwellSeconds { get; set; } = .5f;
    public CanyonMechanismFrame Frame { get; private set; } = new(false, false, true, 0f);
    public bool IsActivated { get; private set; }
    public float ElapsedSeconds { get; private set; }

    public override void _Ready()
    {
        SyncToPhysics = true;
        CollisionLayer = Knight.Combat.CombatCollisionLayers.World;
        CollisionMask = Knight.Combat.CombatCollisionLayers.World |
                        Knight.Combat.CombatCollisionLayers.Player;
        Position = LowerPosition;
        ApplyForbiddenProfile();
    }

    public override void _PhysicsProcess(double delta) => Advance((float)delta);

    public static CanyonMechanismFrame Evaluate(bool active, float elapsed, float travel, float dwell)
    {
        if (!active) return new(false, false, true, 0f);
        travel = Mathf.Max(.01f, travel);
        dwell = Mathf.Max(0f, dwell);
        float leg = travel + dwell;
        float cycle = leg * 2f;
        float t = Mathf.PosMod(Mathf.Max(0f, elapsed), cycle);
        if (t < travel) return new(false, true, true, t / travel);
        if (t < leg) return new(false, false, true, 1f);
        t -= leg;
        if (t < travel) return new(false, true, true, 1f - t / travel);
        return new(false, false, true, 0f);
    }

    public void Activate() => IsActivated = true;

    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        Activate();
        return true;
    }

    public void AdvanceForTest(float delta) => Advance(delta);

    public void ResetForCheckpoint()
    {
        IsActivated = false;
        ElapsedSeconds = 0f;
        Frame = Evaluate(false, 0f, TravelSeconds, EndpointDwellSeconds);
        Position = LowerPosition;
    }

    private void Advance(float delta)
    {
        if (GetTree()?.Paused == true || !IsActivated || !float.IsFinite(delta) || delta <= 0f) return;
        ElapsedSeconds += delta;
        Frame = Evaluate(true, ElapsedSeconds, TravelSeconds, EndpointDwellSeconds);
        Position = LowerPosition.Lerp(UpperPosition, Frame.Phase01);
    }

    private void ApplyForbiddenProfile()
    {
        GrappleSurface2D profile = GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
        if (profile != null) profile.Material = GrappleSurfaceMaterial.Forbidden;
    }
}
