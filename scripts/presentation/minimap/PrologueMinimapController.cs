using Godot;
using Knight.Save;
using System;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Presentation.Fog;
using Knight.Player;
using Knight.Ui;

namespace Knight.Presentation.Minimap;

public partial class PrologueMinimapController : Node
{
    private MinimapTerrainCatalog _catalog;
    [Export] public MinimapTerrainCatalog Catalog
    {
        get => _catalog;
        set
        {
            if (Model != null) throw new InvalidOperationException("Configure minimap catalog before Ready.");
            _catalog = value;
        }
    }
    public ExplorationMapModel Model { get; private set; }
    public ulong WorldGeneration { get; private set; }
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private PrologueFogController _fog;
    private CharacterBody2D _player;
    private readonly Rect2[] _loaded = new Rect2[ExplorationMapModel.MaximumLoadedBounds];
    private readonly ulong[] _mounted = new ulong[ExplorationMapModel.MaximumLoadedBounds];
    private readonly Rect2[] _pendingLoaded = new Rect2[ExplorationMapModel.MaximumLoadedBounds];
    private readonly ulong[] _pendingMounted = new ulong[ExplorationMapModel.MaximumLoadedBounds];
    private int _loadedCount;
    private ulong _worldId, _lastStamp;
    private bool _hasStamp;
    private PrologueMinimapView _view;
    private ProloguePauseMenu _pause;

    public override void _Ready()
    {
        Model = new ExplorationMapModel(Catalog);
        _level = GetParent() as PrologueLevelController;
        _flow = _level?.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        _fog = _level?.GetNodeOrNull<PrologueFogController>("FogController");
        _player = _level?.GetNodeOrNull<CharacterBody2D>("PlayerRoot/Player");
        _view = _level?.GetNodeOrNull<PrologueMinimapView>("Hud/FormalHud/Minimap");
        _pause = _level?.GetNodeOrNull<ProloguePauseMenu>("PauseMenu");
        ProcessMode = ProcessModeEnum.Always;
        if (Live(_pause)) _pause.Opened += HidePresentation;
        if (Live(_fog)) _fog.ObservationPresented += OnObservation;
    }

    public override void _Process(double delta)
    {
        if (!Live(_view)) return;
        if (!Live(_level) || GetParent() != _level ||
            _level.GetNodeOrNull<PrologueMinimapView>("Hud/FormalHud/Minimap") != _view ||
            _view.GetParent() is not PrologueFormalHud || !Live(_flow) || _flow.GetParent() != _level ||
            _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow") != _flow || !Live(_player) ||
            _level.Player != _player || _player.GetParent() != _level.GetNodeOrNull<Node2D>("PlayerRoot") ||
            GetTree().Paused || _flow.State != PrologueGameFlowState.Playing ||
            _player.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 })
        { HidePresentation(); return; }
        Vector2 facing = _player.GetNodeOrNull<PlayerInputReader>("Input")?.KeyboardAimDirection ?? Vector2.Right;
        var guide = _level.GetNodeOrNull<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        if (Live(guide) && guide.IsVisibleInTree() && guide.HasVisiblePrompt)
            _view.GetParent<PrologueFormalHud>().AvoidActiveGuidanceForMinimap(
                new Rect2(guide.GetGlobalTransform() * guide.PromptRect.Position, guide.PromptRect.Size));
        _view.Present(Model, _player.GlobalPosition, facing, delta);
    }

    private void HidePresentation() { if (Live(_view)) _view.ClearPresentation(); }

    private void OnObservation(FogOcclusionObservation observation)
    {
        if (!Live(this) || !Live(_level) || GetParent() != _level || !Live(_flow) ||
            _flow.GetParent() != _level || _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow") != _flow ||
            !Live(_fog) || _fog.GetParent() != _level || _level.GetNodeOrNull<PrologueFogController>("FogController") != _fog ||
            !Live(_player) || _level.Player != _player ||
            _player.GetParent() != _level.GetNodeOrNull<Node2D>("PlayerRoot") || GetTree().Paused ||
            _flow.State != PrologueGameFlowState.Playing ||
            _player.GetNodeOrNull<HealthComponent>("Health") is not { CurrentHealth: > 0 }) return;
        Node2D world = _level.GetNodeOrNull<Node2D>("WorldRoot");
        if (!Live(world) || observation.WorldInstanceId != world.GetInstanceId() ||
            observation.PhysicsFrame != Engine.GetPhysicsFrames() ||
            (_hasStamp && observation.PhysicsFrame <= _lastStamp) ||
            observation.ActorWorld != _player.GlobalPosition ||
            !ReferenceEquals(observation.Frame, _fog.LastOcclusionFrame)) return;
        _hasStamp = true;
        _lastStamp = observation.PhysicsFrame;
        if (!FillLoadedBounds(world)) return;
        Model.Advance(new MinimapObservation(observation.ActorWorld, observation.PhysicsFrame,
            WorldGeneration, true, observation.Frame, _loaded.AsMemory(0, _loadedCount)));
    }

    private bool FillLoadedBounds(Node2D world)
    {
        SeamlessPrologueWorld sectors = world.GetNodeOrNull<SeamlessPrologueWorld>("Sectors");
        if (!Live(sectors) || sectors.GetParent() != world || world.GetParent() != _level ||
            world.GlobalTransform != Transform2D.Identity || sectors.GlobalTransform != Transform2D.Identity) return false;
        bool changed = _worldId != world.GetInstanceId();
        int count = 0;
        for (int i = 0; i < sectors.Definitions.Count; i++)
        {
            PrologueSectorDefinition definition = sectors.Definitions[i];
            if (definition == null) return false;
            if (!sectors.TryGetMountedSector(definition.Id, out PrologueSector sector)) continue;
            if (!Live(sector) || sector.GetParent() != sectors || count == _loaded.Length ||
                sector.Transform != new Transform2D(0, definition.WorldOffset) ||
                sector.GlobalTransform != new Transform2D(0, definition.WorldOffset)) return false;
            ulong id = sector.GetInstanceId();
            for (int j = 0; j < count; j++) if (_pendingMounted[j] == id) return false;
            Rect2 bounds = new(definition.Bounds.Position + definition.WorldOffset, definition.Bounds.Size);
            if (!MinimapTerrainCatalog.Valid(bounds)) return false;
            changed |= count >= _loadedCount || _mounted[count] != id || _loaded[count] != bounds;
            _pendingMounted[count] = id;
            _pendingLoaded[count++] = bounds;
        }
        changed |= count != _loadedCount;
        Array.Copy(_pendingMounted, _mounted, count);
        Array.Copy(_pendingLoaded, _loaded, count);
        _loadedCount = count;
        _worldId = world.GetInstanceId();
        if (changed) WorldGeneration++;
        return count > 0;
    }

    public void StartNewExploration() => Model.Clear();
    public void RestoreExploration(ExplorationMapSaveData data) => Model.Restore(data);
    public ExplorationMapSaveData CaptureExploration() => Model.CaptureSave();
    private static bool Live(Node node) => IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    public override void _ExitTree()
    {
        if (IsInstanceValid(_fog)) _fog.ObservationPresented -= OnObservation;
        if (IsInstanceValid(_pause)) _pause.Opened -= HidePresentation;
        HidePresentation(); _view = null; _pause = null;
        _level = null; _flow = null; _fog = null; _player = null;
    }
}
