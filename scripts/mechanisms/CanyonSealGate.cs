using Godot;
using Knight.Abilities;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonSealGate : StaticBody2D, ICheckpointResettable
{
    [Export] public string RequiredMechanismId { get; set; } = "canyon.v2.courtyard-crank";
    [Export] public string RequiredEncounterId { get; set; } = "canyon.v2.encounter.split-level";
    [Export] public string ActivationId { get; set; } = "canyon.v2.courtyard-seal-open";
    [Export] public NodePath ClosedVisualPath { get; set; } = new("ClosedVisual");
    [Export] public NodePath OpenVisualPath { get; set; } = new("OpenVisual");
    public bool IsOpen { get; private set; }
    public CollisionShape2D Collision => GetNode<CollisionShape2D>("Collision");
    private PrologueProgress _progress;

    public override void _Ready()
    {
        GrappleSurface2D profile = GetNodeOrNull<GrappleSurface2D>("GrappleSurfaceProfile");
        if (profile != null) profile.Material = GrappleSurfaceMaterial.Forbidden;
        ApplyOpenState();
    }

    public override void _PhysicsProcess(double delta) => RefreshState();

    public void ApplyProgress(PrologueProgress progress)
    {
        _progress = progress;
        RefreshState();
    }

    public void RefreshState()
    {
        if (_progress == null) return;
        bool completed = _progress.ActivatedMechanisms.Contains(ActivationId);
        if (!completed && _progress.ActivatedMechanisms.Contains(RequiredMechanismId) &&
            _progress.ActivatedMechanisms.Contains(RequiredEncounterId))
        {
            _progress.ActivateMechanism(ActivationId);
            completed = true;
        }
        IsOpen = completed;
        ApplyOpenState();
    }

    public void ResetForCheckpoint() => RefreshState();

    private void ApplyOpenState()
    {
        if (GetNodeOrNull<CollisionShape2D>("Collision") is { } collision) collision.Disabled = IsOpen;
        if (GetNodeOrNull<CanvasItem>(ClosedVisualPath) is { } closed) closed.Visible = !IsOpen;
        if (GetNodeOrNull<CanvasItem>(OpenVisualPath) is { } open) open.Visible = IsOpen;
    }
}
