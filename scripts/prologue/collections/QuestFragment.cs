using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Localization;

namespace Knight.Prologue.Collections;

public partial class QuestFragment : Area2D
{
    [Signal] public delegate void CollectedEventHandler(string stableId);
    [Export] public string StableId { get; set; } = string.Empty;
    [Export] public NodePath VisualPath { get; set; } = new("AvailableVisual");
    [Export] public NodePath StatusPath { get; set; } = new("Status");
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private bool _claiming;
    private bool _bodySubscribed;
    private bool _flowSubscribed;
    private Node2D _statusRenderer;
    private bool _statusWasVisible;
    public bool IsCollected => IsInstanceValid(_level) && _level.Progress.CollectedFragmentIds.Contains(StableId);
    public int FeedbackCount { get; private set; }
    public FragmentCollectionResult LastResult { get; private set; } = FragmentCollectionResult.Unavailable;
    public void BindRuntime(PrologueLevelController level)
    {
        DisconnectFlow();
        _level = level;
        _flow = IsInstanceValid(level) ? level.GetNodeOrNull<PrologueGameFlowController>("GameFlow") : null;
        ConnectFlow();
        RefreshVisuals();
    }

    public override void _EnterTree()
    {
        BodyEntered += OnBodyEntered;
        _bodySubscribed = true;
        ConnectFlow();
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2;
        RefreshVisuals();
    }

    public override void _ExitTree()
    {
        ReleaseStatusRenderer(_statusRenderer);
        if (_bodySubscribed) BodyEntered -= OnBodyEntered;
        _bodySubscribed = false;
        DisconnectFlow();
    }

    private void ConnectFlow()
    {
        if (_flowSubscribed || !IsInsideTree() || !IsInstanceValid(_flow)) return;
        _flow.SessionStarted += OnSessionStarted;
        _flowSubscribed = true;
    }

    private void DisconnectFlow()
    {
        if (_flowSubscribed && IsInstanceValid(_flow)) _flow.SessionStarted -= OnSessionStarted;
        _flowSubscribed = false;
    }

    public override void _Process(double delta) => RefreshVisuals();

    private void OnSessionStarted()
    {
        LastResult = FragmentCollectionResult.Unavailable;
        RefreshVisuals();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (_claiming || !IsInsideTree() || IsQueuedForDeletion() || !IsInstanceValid(_level) ||
            !_level.IsInsideTree() || _level.IsQueuedForDeletion() || !IsInstanceValid(_flow) ||
            body != _level.Player || !IsVisibleInTree() || GetTree().Paused ||
            _flow.State != PrologueGameFlowState.Playing || IsCollected ||
            body.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 }) return;
        _claiming = true;
        try
        {
            LastResult = _flow.TryCollectFragment(StableId);
            RefreshVisuals();
            if (LastResult != FragmentCollectionResult.Collected) return;
            FeedbackCount++;
            _level.GetNodeOrNull<PrologueAudioDirector>("Audio")?.Play(PrologueAudioCue.UiConfirm, GlobalPosition);
            // Last observable operation: listeners can unload or start a new run.
            EmitSignal(SignalName.Collected, StableId);
        }
        finally { _claiming = false; }
    }

    private void RefreshVisuals()
    {
        bool collected = IsCollected;
        Node2D visual = GetNodeOrNull<Node2D>(VisualPath);
        if (visual != null && visual.Visible == collected) visual.Visible = !collected;
        if (GetNodeOrNull<Label>(StatusPath) is not { } status || !IsInstanceValid(_flow)) return;
        if (_statusRenderer != null && (!IsInstanceValid(_statusRenderer) ||
            !_statusRenderer.IsInsideTree() || _statusRenderer.IsQueuedForDeletion()))
            ReleaseStatusRenderer(_statusRenderer);
        string text = string.Empty;
        if (!collected && PrologueStrings.KeyCount(_flow.Settings.Language) > 0)
            text = PrologueStrings.Get(LastResult == FragmentCollectionResult.SaveFailed
                ? "pickup.status.save_failed" : "pickup.status.available", _flow.Settings.Language);
        if (status.Text != text) status.Text = text;
    }

    // Display ownership only. The original result, text, transaction and native
    // BodyEntered retry remain authoritative. Never suppress a world label until
    // its replacement is actually drawable, and restore its authored visibility.
    internal bool TryAcquireStatusRenderer(Node2D renderer)
    {
        if (LastResult != FragmentCollectionResult.SaveFailed || IsCollected ||
            !IsInstanceValid(renderer) || !renderer.IsInsideTree() || renderer.IsQueuedForDeletion() || !renderer.IsVisibleInTree() ||
            renderer.GetViewport() != GetViewport() || renderer.GetWorld2D() != GetWorld2D() ||
            GetNodeOrNull<Label>(StatusPath) is not { } status ||
            (_statusRenderer != null && !ReferenceEquals(_statusRenderer, renderer))) return false;
        if (_statusRenderer == null) _statusWasVisible = status.Visible;
        _statusRenderer = renderer;
        status.Visible = false;
        return true;
    }

    internal void ReleaseStatusRenderer(Node2D renderer)
    {
        if (_statusRenderer == null || !ReferenceEquals(_statusRenderer, renderer)) return;
        _statusRenderer = null;
        if (GetNodeOrNull<Label>(StatusPath) is { } status) status.Visible = _statusWasVisible;
    }
}
