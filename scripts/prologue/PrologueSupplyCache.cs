using Godot;
using Knight.Combat;
using Knight.Settings;

namespace Knight.Prologue;

// Optional, one-use healing supply. Existing discovery persistence owns its lifetime;
// replaceable child visuals never own collision, health or save state.
public partial class PrologueSupplyCache : Area2D
{
    [Export] public string DiscoveryId { get; set; } = "salvage_cache";
    [Export] public int HealthAmount { get; set; } = 2;

    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private bool _claiming;
    private float _feedbackSeconds;
    private int _lastRestored;

    public bool IsSpent => IsInstanceValid(_level) && _level.Progress.Discoveries.Contains(DiscoveryId);

    public void BindRuntime(PrologueLevelController level)
    {
        _level = level;
        _flow = level.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        RefreshVisuals();
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        RefreshVisuals();
    }

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    public override void _Process(double delta)
    {
        _feedbackSeconds = Mathf.Max(0, _feedbackSeconds - (float)delta);
        // Progress can be restored/reset while this streamed sector instance stays mounted.
        // Only changed state/text touches the scene; no per-frame allocation of visual nodes.
        RefreshVisuals();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_claiming || !IsInstanceValid(_level) || !IsInstanceValid(_flow) ||
            _flow.State != PrologueGameFlowState.Playing || body != _level.Player ||
            !IsVisibleInTree() || IsSpent || string.IsNullOrWhiteSpace(DiscoveryId) || HealthAmount <= 0) return;
        HealthComponent health = body.GetNodeOrNull<HealthComponent>("Health");
        if (health == null || health.CurrentHealth <= 0 || health.CurrentHealth >= health.MaxHealth) return;

        var level = _level;
        var flow = _flow;
        var session = flow.Save;
        string discoveryId = DiscoveryId;
        _claiming = true;
        try
        {
            // A successful heal emits a synchronous public event. Commit consumption
            // first so listeners cannot observe healed health with an unclaimed supply.
            if (!level.Progress.Discover(discoveryId)) return;
            int restored = health.RestoreHealth(HealthAmount);
            // The callback may start a new run or unload this level. Never finish an
            // old claim into a replaced/reset session; title/pause alone still saves.
            if (restored == 0 || !IsInstanceValid(level) || level.IsQueuedForDeletion() ||
                !IsInstanceValid(flow) || !ReferenceEquals(session, flow.Save) ||
                !level.Progress.Discoveries.Contains(discoveryId)) return;
            if (flow.State == PrologueGameFlowState.Playing && _level == level && _flow == flow)
            {
                _lastRestored = restored;
                _feedbackSeconds = 1.5f;
                RefreshVisuals();
            }
            level.EmitSignal(PrologueLevelController.SignalName.ProgressPersistRequested, "discovery");
        }
        finally { _claiming = false; }
    }

    private void RefreshVisuals()
    {
        bool spent = IsSpent;
        Node2D available = GetNodeOrNull<Node2D>("AvailableVisual");
        Node2D empty = GetNodeOrNull<Node2D>("SpentVisual");
        if (available != null && available.Visible == spent) available.Visible = !spent;
        if (empty != null && empty.Visible != spent) empty.Visible = spent;
        if (!spent) _feedbackSeconds = 0;
        bool english = IsInstanceValid(_flow) && _flow.Settings.Language == GameLanguage.English;
        bool full = IsInstanceValid(_level?.Player) && OverlapsBody(_level.Player) &&
            _level.Player.GetNodeOrNull<HealthComponent>("Health") is { } health &&
            health.CurrentHealth >= health.MaxHealth;
        string text = spent
            ? (_feedbackSeconds > 0 ? (english ? $"+{_lastRestored} HEALTH" : $"生命 +{_lastRestored}")
                : (english ? "SALVAGED" : "已回收"))
            : (full ? (english ? "FULL · SUPPLY KEPT" : "满血 · 补给保留")
                : (english ? $"SUPPLY +{HealthAmount}" : $"补给 +{HealthAmount}"));
        if (GetNodeOrNull<Label>("Status") is { } label && label.Text != text) label.Text = text;
    }
}
