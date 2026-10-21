using Godot;
using Knight.Combat;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonSteamJet : Area2D, ICanyonTelegraphSource, ICheckpointResettable
{
    [Export] public float SafeSeconds { get; set; } = 1f;
    [Export] public float TelegraphSeconds { get; set; } = .4f;
    [Export] public float ActiveSeconds { get; set; } = .6f;
    [Export] public NodePath TelegraphPath { get; set; } = new("Telegraph");
    [Export] public NodePath ActiveVisualPath { get; set; } = new("ActiveVisual");
    public CanyonMechanismFrame Frame { get; private set; } = new(false, false, true, 0f);
    public bool IsDangerTelegraphActive => Frame.Telegraphing;
    public bool DisabledForRun { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public CollisionShape2D Collision => GetNode<CollisionShape2D>("Collision");
    private ulong _attackId;

    public override void _Ready()
    {
        CollisionLayer = CombatCollisionLayers.EnemyAttack;
        CollisionMask = CombatCollisionLayers.Player;
        AreaEntered += OnAreaEntered;
        ApplyFrame(Evaluate(0f, SafeSeconds, TelegraphSeconds, ActiveSeconds));
    }

    public override void _PhysicsProcess(double delta) => AdvanceRuntime((float)delta);

    public static CanyonMechanismFrame Evaluate(float elapsed, float safe, float telegraph,
        float active)
    {
        safe = Mathf.Max(.01f, safe); telegraph = Mathf.Max(.01f, telegraph);
        active = Mathf.Max(.01f, active);
        float cycle = safe + telegraph + active;
        float t = Mathf.PosMod(Mathf.Max(0f, elapsed), cycle);
        if (t < safe)
        {
            float warningStart = safe * .75f;
            return t >= warningStart
                ? new(true, false, true, (t - warningStart) / (safe - warningStart))
                : new(false, false, true, 0f);
        }
        t -= safe;
        if (t < telegraph) return new(true, false, true, t / telegraph);
        return new(false, true, false, 1f);
    }

    public void AdvanceForTest(float delta) => AdvanceRuntime(delta);

    public void DisableForRun()
    {
        DisabledForRun = true;
        ApplyFrame(new(false, false, true, 0f));
    }

    public void EnableForNewRun()
    {
        DisabledForRun = false;
        ElapsedSeconds = 0f;
        ApplyFrame(Evaluate(0f, SafeSeconds, TelegraphSeconds, ActiveSeconds));
    }

    public void ResetForCheckpoint()
    {
        ElapsedSeconds = 0f;
        if (!DisabledForRun) ApplyFrame(Evaluate(0f, SafeSeconds, TelegraphSeconds, ActiveSeconds));
    }

    public void AdvanceRuntime(float delta)
    {
        if (GetTree()?.Paused == true || DisabledForRun || !float.IsFinite(delta) || delta <= 0f) return;
        ElapsedSeconds += delta;
        ApplyFrame(Evaluate(ElapsedSeconds, SafeSeconds, TelegraphSeconds, ActiveSeconds));
    }

    private void ApplyFrame(CanyonMechanismFrame frame)
    {
        Frame = frame;
        Collision.Disabled = !frame.HazardActive;
        Monitoring = frame.HazardActive;
        CanvasItem telegraph = GetNodeOrNull<CanvasItem>(TelegraphPath);
        if (telegraph != null) telegraph.Visible = frame.Telegraphing;
        CanvasItem activeVisual = GetNodeOrNull<CanvasItem>(ActiveVisualPath);
        if (activeVisual != null) activeVisual.Visible = frame.HazardActive;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (!Frame.HazardActive || area is not Hurtbox hurtbox) return;
        _attackId++;
        hurtbox.Receive(new DamageRequest(1, Vector2.Up, 180f, _attackId, GetInstanceId()));
    }
}
