using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;

namespace Knight.Prologue.Seamless;

public partial class SeamlessPrologueWorld : Node2D
{
    public event Action<PrologueSectorId, Vector2> CheckpointActivated;
    public event Action<PrologueSector> SectorMounted;
    internal static readonly StringName StagedSectorMeta = "_seamless_staged";
    [Export] public NodePath PlayerPath { get; set; } = new("../../PlayerRoot/Player");
    [Export] public PrologueSectorId InitialSector { get; set; } = PrologueSectorId.CanyonEntrance;
    [Export] public float CameraLimitBlendRate { get; set; } = 10f;
    [Export] public Godot.Collections.Array<PrologueSectorDefinition> Definitions { get; set; } = new();

    public PrologueSectorId CurrentSector { get; private set; } = PrologueSectorId.CanyonEntrance;
    public IReadOnlyList<PrologueSectorId> MountedSectorIds
    {
        get
        {
            var ids = new List<PrologueSectorId>(_mounted.Keys);
            ids.Sort();
            return ids;
        }
    }
    public int RetiredSectorCount => _retired.Count;
    public Vector2 InitialPlayerPosition => _initialPlayerPosition;

    private readonly Dictionary<PrologueSectorId, PrologueSectorDefinition> _definitions = new();
    private readonly Dictionary<PrologueSectorId, PrologueSector> _mounted = new();
    private readonly List<PrologueSector> _retired = new();
    private readonly Dictionary<CheckpointTrigger,
        CheckpointTrigger.CheckpointActivatedEventHandler> _checkpointHandlers = new();
    private PrologueSectorStreamModel _streamModel;
    private CharacterBody2D _player;
    private Node _playerParent;
    private Camera2D _camera;
    private Vector2 _initialPlayerPosition;
    private PrologueSectorId _checkpointSector;
    private Vector2 _checkpointPosition;
    private Rect2 _cameraBounds;
    private Rect2 _cameraTargetBounds;
    private bool _initialized;
    private bool _transitioning;
    private TransitionRequest? _queuedTransition;
    private int _retirementDrainPhase;

    public override void _Ready()
    {
        if (!float.IsFinite(CameraLimitBlendRate) || CameraLimitBlendRate <= 0f)
            throw new InvalidOperationException(
                $"{nameof(CameraLimitBlendRate)} must be positive and finite.");
        if (Definitions == null)
            throw new InvalidOperationException($"{nameof(Definitions)} must not be null.");

        var definitions = new List<PrologueSectorDefinition>();
        foreach (PrologueSectorDefinition definition in Definitions) definitions.Add(definition);
        _streamModel = new PrologueSectorStreamModel(definitions);
        foreach (PrologueSectorDefinition definition in definitions)
            _definitions.Add(definition.Id, definition);
        if (!_definitions.ContainsKey(InitialSector))
            throw new InvalidOperationException($"Initial sector {InitialSector} is not defined.");

        _player = GetNodeOrNull<CharacterBody2D>(PlayerPath) ??
            throw new InvalidOperationException(
                $"{nameof(SeamlessPrologueWorld)} requires a persistent CharacterBody2D at {PlayerPath}.");
        _playerParent = _player.GetParent() ??
            throw new InvalidOperationException("The persistent player must already be under PlayerRoot.");
        if (_playerParent.Name != "PlayerRoot")
            throw new InvalidOperationException(
                "The persistent player must remain a direct child of PlayerRoot.");
        if (IsAncestorOf(_player))
            throw new InvalidOperationException("The persistent player must not be parented below a sector world.");
        _camera = _player.GetNodeOrNull<Camera2D>("Camera2D");
        _initialPlayerPosition = _player.GlobalPosition;
        _checkpointSector = InitialSector;
        _checkpointPosition = _initialPlayerPosition;

        SetCurrentSector(InitialSector);
        _cameraBounds = _cameraTargetBounds;
        ApplyCameraBounds(_cameraBounds);
        _initialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_initialized) return;
        PruneRetired();
        if (DrainOneQueuedTransition())
        {
            BlendCameraBounds(Mathf.Max(0f, (float)delta));
            return;
        }
        EnsurePersistentPlayer();
        PrologueSectorId resolved = ResolveSector(_player.GlobalPosition);
        if (resolved != CurrentSector) SetCurrentSector(resolved);
        BlendCameraBounds(Mathf.Max(0f, (float)delta));
    }

    public override void _Process(double delta)
    {
        if (_retired.Count == 0) return;
        if (_retirementDrainPhase == 0)
        {
            _retirementDrainPhase = 1;
            return;
        }
        if (_retirementDrainPhase == 1)
        {
            foreach (PrologueSector sector in _retired)
                if (GodotObject.IsInstanceValid(sector)) sector.QueueFree();
            _retirementDrainPhase = 2;
            return;
        }
        foreach (PrologueSector sector in _retired)
            if (GodotObject.IsInstanceValid(sector)) sector.Free();
        _retired.Clear();
        _retirementDrainPhase = 0;
    }

    public override void _ExitTree()
    {
        _initialized = false;
        foreach (PrologueSector sector in _mounted.Values) UnbindCheckpoints(sector);
        foreach (PrologueSector sector in _retired)
            if (GodotObject.IsInstanceValid(sector)) sector.Free();
        _checkpointHandlers.Clear();
        _mounted.Clear();
        _retired.Clear();
        _definitions.Clear();
        _player = null;
        _playerParent = null;
        _camera = null;
        _transitioning = false;
        _queuedTransition = null;
    }

    public bool TryActivateCheckpoint(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        if (!_initialized || sectorId != CurrentSector || !_mounted.ContainsKey(sectorId) ||
            sectorId is not (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple) ||
            !PrologueSectorDefinition.Finite(worldPosition) ||
            !_definitions.TryGetValue(sectorId, out PrologueSectorDefinition definition) ||
            !ContainsPoint(definition.WorldBounds, worldPosition)) return false;
        _checkpointSector = sectorId;
        _checkpointPosition = worldPosition;
        CheckpointActivated?.Invoke(sectorId, worldPosition);
        return true;
    }

    public bool TryGetMountedSector(PrologueSectorId sectorId, out PrologueSector sector)
    {
        if (_mounted.TryGetValue(sectorId, out sector) && GodotObject.IsInstanceValid(sector))
            return true;
        sector = null;
        return false;
    }

    public void ResetCurrentSectorForCheckpoint()
    {
        if (!TryGetMountedSector(CurrentSector, out PrologueSector sector)) return;
        ResetDescendants(sector);
    }

    public bool RestoreCheckpoint()
    {
        if (!_initialized || !GodotObject.IsInstanceValid(_player)) return false;
        EnsurePersistentPlayer();
        RequestTransition(new TransitionRequest(_checkpointSector, _checkpointPosition, true));
        return true;
    }

    public bool RestoreCheckpoint(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        if (!_initialized || !GodotObject.IsInstanceValid(_player) ||
            !PrologueSectorDefinition.Finite(worldPosition) ||
            sectorId != InitialSector &&
            sectorId is not (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple))
            return false;
        EnsurePersistentPlayer();
        _checkpointSector = sectorId;
        _checkpointPosition = worldPosition;
        RequestTransition(new TransitionRequest(sectorId, worldPosition, true));
        return true;
    }

    public void ResetForNewRun()
    {
        if (!_initialized || !GodotObject.IsInstanceValid(_player)) return;
        EnsurePersistentPlayer();
        _checkpointSector = InitialSector;
        _checkpointPosition = _initialPlayerPosition;
        RequestTransition(new TransitionRequest(InitialSector, _initialPlayerPosition, true));
    }

    private void SetCurrentSector(PrologueSectorId next) =>
        RequestTransition(new TransitionRequest(next, null, !AreAdjacent(CurrentSector, next)));

    private void RequestTransition(TransitionRequest requested)
    {
        if (!_definitions.ContainsKey(requested.Sector))
            throw new ArgumentOutOfRangeException(nameof(requested), requested.Sector,
                "Transition target must be a defined sector.");
        if (_transitioning)
        {
            _queuedTransition = requested;
            return;
        }

        _transitioning = true;
        try
        {
            ExecuteTransition(requested);
        }
        finally
        {
            _transitioning = false;
        }
    }

    private bool DrainOneQueuedTransition()
    {
        if (_transitioning || !_queuedTransition.HasValue) return false;
        TransitionRequest request = _queuedTransition.Value;
        _queuedTransition = null;
        RequestTransition(request);
        return true;
    }

    private void ExecuteTransition(TransitionRequest request)
    {
        IReadOnlyList<PrologueSectorId> required = _streamModel.RequiredSet(request.Sector);
        List<StagedSector> prepared = PrepareStaged(required);
        var keep = new HashSet<PrologueSectorId>(required);
        var detached = new List<MountedSector>();
        var obsolete = new List<PrologueSectorId>();
        foreach (PrologueSectorId id in _mounted.Keys)
            if (!keep.Contains(id)) obsolete.Add(id);
        obsolete.Sort();

        foreach (StagedSector item in prepared)
            if (_mounted.ContainsKey(item.Id))
            {
                CleanupStaged(prepared);
                throw new InvalidOperationException($"Sector {item.Id} is already mounted.");
            }

        ActivateAndValidateStaged(prepared);

        MechanicalClawController claw = _player?
            .GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        foreach (PrologueSectorId id in obsolete)
        {
            PrologueSector sector = _mounted[id];
            if (GodotObject.IsInstanceValid(claw)) claw.NotifyTargetWillUnload(sector);
        }

        foreach (PrologueSectorId id in obsolete)
        {
            PrologueSector sector = _mounted[id];
            UnbindCheckpoints(sector);
            _mounted.Remove(id);
            if (ReferenceEquals(sector.GetParent(), this)) RemoveChild(sector);
            detached.Add(new MountedSector(id, sector));
        }

        foreach (StagedSector item in prepared)
        {
            item.Sector.RemoveMeta(StagedSectorMeta);
            _mounted.Add(item.Id, item.Sector);
            SectorMounted?.Invoke(item.Sector);
        }

        CurrentSector = request.Sector;
        _cameraTargetBounds = _definitions[request.Sector].WorldBounds;
        if (request.Relocation.HasValue)
            _player.GlobalPosition = request.Relocation.Value;
        if (request.SnapCamera)
            SnapCameraToTarget();

        foreach (MountedSector item in detached) RetireDetached(item.Sector);
    }

    private List<StagedSector> PrepareStaged(IReadOnlyList<PrologueSectorId> required)
    {
        var prepared = new List<StagedSector>();
        try
        {
            foreach (PrologueSectorId id in required)
            {
                if (_mounted.ContainsKey(id)) continue;
                PrologueSectorDefinition definition = _definitions[id];
                Node instance = definition.Scene.Instantiate();
                if (instance is not PrologueSector sector)
                {
                    instance.Free();
                    throw new InvalidOperationException(
                        $"Sector scene {id} must have a {nameof(PrologueSector)} root.");
                }
                List<OwnedNodeState> ownedNodes = CaptureOwnedNodes(sector);
                IReadOnlyList<string> errors = sector.ValidateContract(definition);
                if (errors.Count > 0)
                {
                    sector.Free();
                    throw new InvalidOperationException(
                        $"Invalid mounted sector {id}: {string.Join("; ", errors)}");
                }
                sector.Name = id.ToString();
                sector.Position = definition.WorldOffset;
                ProcessModeEnum processMode = sector.ProcessMode;
                bool visible = sector.Visible;
                List<CollisionState> collisions = DisableCollisions(sector);
                sector.ProcessMode = ProcessModeEnum.Disabled;
                sector.Visible = false;
                sector.SetMeta(StagedSectorMeta, true);
                var staged = new StagedSector(id, sector, processMode, visible, collisions,
                    ownedNodes, new List<CheckpointBinding>());
                prepared.Add(staged);
                AddChild(sector);
                if (!GodotObject.IsInstanceValid(sector) || sector.IsQueuedForDeletion() ||
                    !ReferenceEquals(sector.GetParent(), this) ||
                    !sector.HasMeta(StagedSectorMeta))
                    throw new InvalidOperationException(
                        $"Sector {id} invalidated itself while completing staged _Ready.");
                ValidateOwnedNodePaths(staged, "post-Ready");
                IReadOnlyList<string> postReadyErrors = sector.ValidateContract(definition);
                if (postReadyErrors.Count > 0 ||
                    sector.Position.DistanceTo(definition.WorldOffset) > .01f ||
                    sector.Name != new StringName(id.ToString()))
                    throw new InvalidOperationException(
                        $"Invalid post-Ready sector {id}: {string.Join("; ", postReadyErrors)}; " +
                        $"position={sector.Position} expected={definition.WorldOffset}");
            }
            return prepared;
        }
        catch
        {
            CleanupStaged(prepared);
            throw;
        }
    }

    private static void ResetDescendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is ICheckpointResettable resettable) resettable.ResetForCheckpoint();
            ResetDescendants(child);
        }
    }

    private void ActivateAndValidateStaged(IReadOnlyList<StagedSector> prepared)
    {
        try
        {
            foreach (StagedSector item in prepared)
            {
                item.Sector.ProcessMode = item.ProcessMode;
                item.Sector.Visible = item.Visible;
                RestoreCollisionStates(item.Collisions);
            }
            foreach (StagedSector item in prepared) ValidateActivatedStaged(item);
            foreach (StagedSector item in prepared) BindPreparedCheckpoints(item);
            foreach (StagedSector item in prepared) ValidatePreparedBindings(item);
        }
        catch
        {
            CleanupStaged(prepared);
            throw;
        }
    }

    private void ValidateActivatedStaged(StagedSector item)
    {
        PrologueSector sector = item.Sector;
        PrologueSectorDefinition definition = _definitions[item.Id];
        if (!GodotObject.IsInstanceValid(sector) || sector.IsQueuedForDeletion())
            throw new InvalidOperationException(
                $"Activated sector {item.Id} was queued or deleted during finalization.");
        if (!ReferenceEquals(sector.GetParent(), this))
            throw new InvalidOperationException(
                $"Activated sector {item.Id} left its validated final parent.");
        string expectedName = item.Id.ToString();
        if (sector.Name != new StringName(expectedName) ||
            !ReferenceEquals(GetNodeOrNull<PrologueSector>(expectedName), sector))
            throw new InvalidOperationException(
                $"Activated sector {item.Id} lost its exact final name/path {expectedName}.");
        if (!sector.HasMeta(StagedSectorMeta) || sector.ProcessMode != item.ProcessMode ||
            sector.Visible != item.Visible)
            throw new InvalidOperationException(
                $"Activated sector {item.Id} retained invalid staged state.");
        IReadOnlyList<string> contractErrors = sector.ValidateContract(definition);
        if (contractErrors.Count > 0 ||
            sector.Position.DistanceTo(definition.WorldOffset) > .01f)
            throw new InvalidOperationException(
                $"Invalid activated sector {item.Id}: {string.Join("; ", contractErrors)}; " +
                $"position={sector.Position} expected={definition.WorldOffset}");
        ValidateOwnedNodePaths(item, "activated");
        foreach (CollisionState state in item.Collisions)
            if (!GodotObject.IsInstanceValid(state.Shape) || state.Shape.IsQueuedForDeletion() ||
                state.Shape.Disabled != state.Disabled)
                throw new InvalidOperationException(
                    $"Activated sector {item.Id} has invalid collision state.");
    }

    private static List<OwnedNodeState> CaptureOwnedNodes(PrologueSector sector)
    {
        var owned = new List<OwnedNodeState>();
        foreach (Node node in Descendants(sector))
            owned.Add(new OwnedNodeState(node, sector.GetPathTo(node)));
        return owned;
    }

    private static void ValidateOwnedNodePaths(StagedSector item, string phase)
    {
        foreach (OwnedNodeState owned in item.OwnedNodes)
            if (!GodotObject.IsInstanceValid(owned.Node) || owned.Node.IsQueuedForDeletion() ||
                !ReferenceEquals(item.Sector.GetNodeOrNull(owned.Path), owned.Node))
                throw new InvalidOperationException(
                    $"Sector {item.Id} lost originally-owned child {owned.Path} during {phase} validation.");
    }

    private void RetireDetached(PrologueSector sector)
    {
        _retired.Add(sector);
        _retirementDrainPhase = 0;
        TrimRetired();
    }

    private void TrimRetired()
    {
        PruneRetired();
        int limit = Math.Max(1, _definitions.Count);
        while (_retired.Count > limit)
        {
            PrologueSector oldest = _retired[0];
            _retired.RemoveAt(0);
            if (GodotObject.IsInstanceValid(oldest)) oldest.Free();
        }
    }

    private void PruneRetired()
    {
        for (int index = _retired.Count - 1; index >= 0; index--)
            if (!GodotObject.IsInstanceValid(_retired[index])) _retired.RemoveAt(index);
    }

    private void BindPreparedCheckpoints(StagedSector item)
    {
        foreach (OwnedNodeState owned in item.OwnedNodes)
        {
            if (owned.Node is not CheckpointTrigger checkpoint) continue;
            if (!GodotObject.IsInstanceValid(checkpoint) || checkpoint.IsQueuedForDeletion())
                throw new InvalidOperationException(
                    $"Sector {item.Id} contains an invalid staged checkpoint {owned.Path}.");
            if (_checkpointHandlers.ContainsKey(checkpoint))
                throw new InvalidOperationException(
                    $"Sector {item.Id} checkpoint {owned.Path} is already bound.");
            CheckpointTrigger.CheckpointActivatedEventHandler handler = _ =>
                TryActivateCheckpoint(item.Id, checkpoint.GlobalPosition);
            checkpoint.CheckpointActivated += handler;
            _checkpointHandlers.Add(checkpoint, handler);
            item.Bindings.Add(new CheckpointBinding(checkpoint, handler, owned.Path));
        }
    }

    private void ValidatePreparedBindings(StagedSector item)
    {
        int expected = 0;
        foreach (OwnedNodeState owned in item.OwnedNodes)
            if (owned.Node is CheckpointTrigger) expected++;
        if (item.Bindings.Count != expected)
            throw new InvalidOperationException(
                $"Sector {item.Id} did not record every prepared checkpoint binding.");
        foreach (CheckpointBinding binding in item.Bindings)
            if (!GodotObject.IsInstanceValid(binding.Checkpoint) ||
                binding.Checkpoint.IsQueuedForDeletion() ||
                !ReferenceEquals(item.Sector.GetNodeOrNull(binding.Path), binding.Checkpoint) ||
                !_checkpointHandlers.TryGetValue(binding.Checkpoint, out
                    CheckpointTrigger.CheckpointActivatedEventHandler registered) ||
                registered != binding.Handler)
                throw new InvalidOperationException(
                    $"Sector {item.Id} lost prepared checkpoint binding {binding.Path}.");
    }

    private void UnbindCheckpoints(PrologueSector sector)
    {
        foreach (Node node in Descendants(sector))
        {
            if (node is not CheckpointTrigger checkpoint ||
                !_checkpointHandlers.Remove(checkpoint, out
                    CheckpointTrigger.CheckpointActivatedEventHandler handler)) continue;
            checkpoint.CheckpointActivated -= handler;
        }
    }

    private PrologueSectorId ResolveSector(Vector2 worldPosition)
    {
        if (ContainsPoint(_definitions[CurrentSector].WorldBounds, worldPosition))
            return CurrentSector;
        foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
            if (ContainsPoint(_definitions[id].WorldBounds, worldPosition)) return id;
        return CurrentSector;
    }

    private void EnsurePersistentPlayer()
    {
        if (!GodotObject.IsInstanceValid(_player) || !GodotObject.IsInstanceValid(_playerParent) ||
            !ReferenceEquals(_player.GetParent(), _playerParent))
            throw new InvalidOperationException(
                "The seamless world requires the same player instance to remain under PlayerRoot.");
    }

    private void BlendCameraBounds(float delta)
    {
        if (_camera == null || !GodotObject.IsInstanceValid(_camera)) return;
        float weight = 1f - Mathf.Exp(-CameraLimitBlendRate * delta);
        _cameraBounds = new Rect2(
            _cameraBounds.Position.Lerp(_cameraTargetBounds.Position, weight),
            _cameraBounds.Size.Lerp(_cameraTargetBounds.Size, weight));
        if (_cameraBounds.Position.DistanceTo(_cameraTargetBounds.Position) < .01f &&
            _cameraBounds.Size.DistanceTo(_cameraTargetBounds.Size) < .01f)
            _cameraBounds = _cameraTargetBounds;
        ApplyCameraBounds(_cameraBounds);
    }

    private void ApplyCameraBounds(Rect2 bounds)
    {
        if (_camera == null || !GodotObject.IsInstanceValid(_camera)) return;
        _camera.LimitLeft = Mathf.RoundToInt(bounds.Position.X);
        _camera.LimitTop = Mathf.RoundToInt(bounds.Position.Y);
        _camera.LimitRight = Mathf.RoundToInt(bounds.End.X);
        _camera.LimitBottom = Mathf.RoundToInt(bounds.End.Y);
    }

    private void SnapCameraToTarget()
    {
        _cameraBounds = _cameraTargetBounds;
        ApplyCameraBounds(_cameraBounds);
    }

    private static bool ContainsPoint(Rect2 bounds, Vector2 point, float epsilon = .01f) =>
        point.X >= bounds.Position.X - epsilon && point.Y >= bounds.Position.Y - epsilon &&
        point.X <= bounds.End.X + epsilon && point.Y <= bounds.End.Y + epsilon;

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }

    private bool AreAdjacent(PrologueSectorId left, PrologueSectorId right)
    {
        if (left == right) return true;
        PrologueSectorDefinition definition = _definitions[left];
        return definition.Previous == right || definition.Next == right;
    }

    private static List<CollisionState> DisableCollisions(Node root)
    {
        var states = new List<CollisionState>();
        foreach (Node node in Descendants(root))
        {
            if (node is not CollisionShape2D collision) continue;
            states.Add(new CollisionState(collision, collision.Disabled));
            collision.Disabled = true;
        }
        return states;
    }

    private static void RestoreCollisionStates(IReadOnlyList<CollisionState> states)
    {
        foreach (CollisionState state in states)
            if (GodotObject.IsInstanceValid(state.Shape)) state.Shape.Disabled = state.Disabled;
    }

    private void CleanupStaged(IReadOnlyList<StagedSector> staged)
    {
        foreach (StagedSector item in staged)
        {
            DisconnectPreparedBindings(item);
            if (GodotObject.IsInstanceValid(item.Sector))
            {
                if (item.Sector.GetParent() != null)
                    item.Sector.GetParent().RemoveChild(item.Sector);
                item.Sector.Free();
            }
            foreach (OwnedNodeState owned in item.OwnedNodes)
                if (GodotObject.IsInstanceValid(owned.Node)) owned.Node.Free();
        }
        PruneInvalidCheckpointHandlers();
    }

    private void DisconnectPreparedBindings(StagedSector item)
    {
        foreach (CheckpointBinding binding in item.Bindings)
        {
            if (_checkpointHandlers.TryGetValue(binding.Checkpoint, out
                    CheckpointTrigger.CheckpointActivatedEventHandler registered) &&
                registered == binding.Handler)
                _checkpointHandlers.Remove(binding.Checkpoint);
            if (GodotObject.IsInstanceValid(binding.Checkpoint))
                binding.Checkpoint.CheckpointActivated -= binding.Handler;
        }
        item.Bindings.Clear();
    }

    private void PruneInvalidCheckpointHandlers()
    {
        var invalid = new List<CheckpointTrigger>();
        foreach (CheckpointTrigger checkpoint in _checkpointHandlers.Keys)
            if (!GodotObject.IsInstanceValid(checkpoint)) invalid.Add(checkpoint);
        foreach (CheckpointTrigger checkpoint in invalid) _checkpointHandlers.Remove(checkpoint);
    }

    private readonly record struct MountedSector(PrologueSectorId Id, PrologueSector Sector);
    private readonly record struct CollisionState(CollisionShape2D Shape, bool Disabled);
    private readonly record struct OwnedNodeState(Node Node, NodePath Path);
    private readonly record struct CheckpointBinding(CheckpointTrigger Checkpoint,
        CheckpointTrigger.CheckpointActivatedEventHandler Handler, NodePath Path);
    private sealed record StagedSector(PrologueSectorId Id, PrologueSector Sector,
        ProcessModeEnum ProcessMode, bool Visible, List<CollisionState> Collisions,
        List<OwnedNodeState> OwnedNodes, List<CheckpointBinding> Bindings);
    private readonly record struct TransitionRequest(PrologueSectorId Sector,
        Vector2? Relocation, bool SnapCamera);
}
