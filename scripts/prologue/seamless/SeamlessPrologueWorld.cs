using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Player;

namespace Knight.Prologue.Seamless;

public partial class SeamlessPrologueWorld : Node2D
{
    private const float AscentHeadroomPixels = 120f;
    private const float ReleaseHeadroomFullRiseSpeed = 240f;
    private const float OutsideRecoveryMargin = 96f;
    private const float CameraCoverageEpsilon = .01f;
    public event Action<PrologueSectorId, Vector2> CheckpointActivated;
    public string CheckpointId { get; private set; } = "canyon.v2.start";
    public event Action<PrologueSector> SectorMounted;
    internal static readonly StringName StagedSectorMeta = "_seamless_staged";
    [Export] public NodePath PlayerPath { get; set; } = new("../../PlayerRoot/Player");
    [Export] public PrologueSectorId InitialSector { get; set; } = PrologueSectorId.CanyonEntrance;
    [Export] public int LayoutVersion { get; set; } = 1;
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
    private readonly HashSet<PrologueSectorId> _pinnedSectors = [];
    private readonly List<PrologueSector> _retired = new();
    private readonly Dictionary<CheckpointTrigger,
        Func<StringName, bool>> _checkpointHandlers = new();
    private PrologueSectorStreamModel _streamModel;
    private CharacterBody2D _player;
    private Node _playerParent;
    private Camera2D _camera;
    private PlayerMotor _playerMotor;
    private MechanicalClawController _claw;
    private GrappleSurface2D _residentTarget;
    private PrologueSectorId? _residentSector;
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private Vector2 _cameraRestPosition;
    private Vector2 _initialPlayerPosition;
    private PrologueSectorId _checkpointSector;
    private Vector2 _checkpointPosition;
    private Rect2 _cameraBounds;
    private Rect2 _cameraTargetBounds;
    private CanyonCameraFocusModel _cameraFocusModel;
    private CanyonCameraFrame _cameraFrame;
    private bool _initialized;
    private bool _transitioning;
    private TransitionRequest? _queuedTransition;
    private int _retirementDrainPhase;
    private bool _residencyRefreshPending;

    public override void _Ready()
    {
        if (!float.IsFinite(CameraLimitBlendRate) || CameraLimitBlendRate <= 0f)
            throw new InvalidOperationException(
                $"{nameof(CameraLimitBlendRate)} must be positive and finite.");
        if (LayoutVersion is not (1 or CanyonWorldLayout.CurrentLayoutVersion))
            throw new InvalidOperationException(
                $"{nameof(LayoutVersion)} must be 1 or {CanyonWorldLayout.CurrentLayoutVersion}.");
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
        _playerMotor = _player.GetNodeOrNull<PlayerMotor>("Motor");
        _claw = _player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        BindClawResidency();
        _level = _playerParent.GetParent() as PrologueLevelController;
        _flow = _level?.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
        _cameraRestPosition = _camera?.Position ?? Vector2.Zero;
        if (LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
            _cameraFocusModel = new CanyonCameraFocusModel(CanyonWorldLayout.CameraFocusRegions,
                new PlayerMotionSettings().MaxFallSpeed);
        _initialPlayerPosition = _player.GlobalPosition;
        _checkpointSector = InitialSector;
        _checkpointPosition = _initialPlayerPosition;

        SetCurrentSector(InitialSector);
        if (GodotObject.IsInstanceValid(_claw) &&
            ReferenceEquals(_claw.ResidencyTarget, _residentTarget))
            OnTargetResidencyChanged(_residentTarget, true);
        SnapCameraToTarget();
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
        if (DrainResidencyRefresh())
        {
            BlendCameraBounds(Mathf.Max(0f, (float)delta));
            return;
        }
        EnsurePersistentPlayer();
        if (RecoverOutsideWorld()) return;
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
        UnbindClawResidency();
        UnwatchResidentTarget();
        foreach (PrologueSector sector in _mounted.Values) UnbindCheckpoints(sector);
        foreach (PrologueSector sector in _retired)
            if (GodotObject.IsInstanceValid(sector)) sector.Free();
        _checkpointHandlers.Clear();
        _mounted.Clear();
        _pinnedSectors.Clear();
        _retired.Clear();
        _definitions.Clear();
        _player = null;
        _playerMotor = null;
        _claw = null;
        _level = null;
        _flow = null;
        _playerParent = null;
        if (GodotObject.IsInstanceValid(_camera)) _camera.Position = _cameraRestPosition;
        _camera = null;
        _cameraFocusModel = null;
        _transitioning = false;
        _queuedTransition = null;
        _residentTarget = null;
        _residentSector = null;
        _residencyRefreshPending = false;
    }

    public bool TryActivateCheckpoint(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        if (LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
        {
            foreach (string id in CanyonWorldLayout.StableCheckpointIds)
                if (CanyonWorldLayout.TryResolveCheckpoint(id, out var sector, out var position) &&
                    sector == sectorId && position == worldPosition) return TryActivateCheckpoint(id);
            return false;
        }
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

    public bool TryActivateCheckpoint(string stableId)
    {
        if (LayoutVersion != CanyonWorldLayout.CurrentLayoutVersion ||
            !CanyonWorldLayout.TryResolveCheckpoint(stableId, out var sector, out var position) ||
            !_initialized || sector != CurrentSector || !_mounted.ContainsKey(sector) ||
            sector == PrologueSectorId.CanyonEntrance ||
            _checkpointSector == PrologueSectorId.LostTemple && sector == PrologueSectorId.CanyonBasin) return false;
        // Formal layout2 saves must commit before any world state or success
        // notification changes. A detached/queued former owner is not a fallback.
        if (_flow != null && (!GodotObject.IsInstanceValid(_flow) ||
            !_flow.TryCommitCheckpoint(stableId))) return false;
        CheckpointId = stableId;
        _checkpointSector = sector;
        _checkpointPosition = position;
        CheckpointActivated?.Invoke(sector, position);
        return true;
    }

    public bool RestoreCheckpoint(string stableId)
    {
        if (LayoutVersion != CanyonWorldLayout.CurrentLayoutVersion ||
            !CanyonWorldLayout.TryResolveCheckpoint(stableId, out var sector, out var position)) return false;
        string previous = CheckpointId;
        PrologueSectorId previousSector = _checkpointSector;
        Vector2 previousPosition = _checkpointPosition;
        CheckpointId = stableId;
        try
        {
            if (RestoreCheckpoint(sector, position)) return true;
            CheckpointId = previous;
            return false;
        }
        catch
        {
            CheckpointId = previous;
            _checkpointSector = previousSector;
            _checkpointPosition = previousPosition;
            throw;
        }
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

    internal void ResetRecoverySectorsForCheckpoint(PrologueSectorId departingId,
        ulong departingInstanceId)
    {
        // Keep the existing deferred current/destination reset. A recovery may have
        // changed CurrentSector since the source identity was captured.
        PrologueSector current = TryGetMountedSector(CurrentSector, out PrologueSector mounted)
            ? mounted : null;
        ResetCurrentSectorForCheckpoint();
        if (departingInstanceId == 0 ||
            current != null && current.GetInstanceId() == departingInstanceId) return;
        // Never remount or reset a replacement with the same sector ID.
        if (TryGetMountedSector(departingId, out PrologueSector departing) &&
            departing.GetInstanceId() == departingInstanceId)
            ResetDescendants(departing);
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
        CheckpointId = "canyon.v2.start";
        _checkpointPosition = _initialPlayerPosition;
        RequestTransition(new TransitionRequest(InitialSector, _initialPlayerPosition, true));
    }

    private void SetCurrentSector(PrologueSectorId next) =>
        RequestTransition(new TransitionRequest(next, null,
            LayoutVersion == 1 && !AreAdjacent(CurrentSector, next)));

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
        IReadOnlyList<PrologueSectorId> required =
            _streamModel.RequiredSet(request.Sector, _pinnedSectors);
        List<StagedSector> prepared = PrepareStaged(required);
        var detached = new List<MountedSector>();

        foreach (StagedSector item in prepared)
            if (_mounted.ContainsKey(item.Id))
            {
                CleanupStaged(prepared);
                throw new InvalidOperationException($"Sector {item.Id} is already mounted.");
            }

        ActivateAndValidateStaged(prepared);

        foreach (StagedSector item in prepared)
        {
            item.Sector.RemoveMeta(StagedSectorMeta);
            _mounted.Add(item.Id, item.Sector);
            SectorMounted?.Invoke(item.Sector);
        }

        CurrentSector = request.Sector;
        _cameraTargetBounds = LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion
            ? CanyonWorldLayout.DesignBounds
            : _definitions[request.Sector].WorldBounds;
        if (request.Relocation.HasValue)
        {
            _player.GlobalPosition = request.Relocation.Value;
            _player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetFallTracking();
        }
        if (request.SnapCamera)
            SnapCameraToTarget();

        IReadOnlyList<PrologueSectorId> committedRequired =
            _streamModel.RequiredSet(CurrentSector, _pinnedSectors);
        var keep = new HashSet<PrologueSectorId>(committedRequired);
        var obsolete = new List<PrologueSectorId>();
        foreach (PrologueSectorId id in _mounted.Keys)
            if (!keep.Contains(id)) obsolete.Add(id);
        obsolete.Sort();

        foreach (PrologueSectorId id in obsolete)
        {
            PrologueSector sector = _mounted[id];
            if (ResidentTargetBelongsTo(sector))
            {
                if (GodotObject.IsInstanceValid(_claw)) _claw.NotifyTargetWillUnload(sector);
                continue;
            }
            UnbindCheckpoints(sector);
            _mounted.Remove(id);
            if (ReferenceEquals(sector.GetParent(), this)) RemoveChild(sector);
            detached.Add(new MountedSector(id, sector));
        }

        foreach (MountedSector item in detached) RetireDetached(item.Sector);
    }

    private void BindClawResidency()
    {
        if (!GodotObject.IsInstanceValid(_claw)) return;
        _claw.TargetResidencyChanged += OnTargetResidencyChanged;
        OnTargetResidencyChanged(_claw.ResidencyTarget, true);
    }

    private void UnbindClawResidency()
    {
        if (GodotObject.IsInstanceValid(_claw))
            _claw.TargetResidencyChanged -= OnTargetResidencyChanged;
    }

    private void OnTargetResidencyChanged(GrappleSurface2D target, bool resident)
    {
        if (resident)
        {
            if (target == null || !GodotObject.IsInstanceValid(target)) return;
            _residencyRefreshPending = false;
            if (!ReferenceEquals(target, _residentTarget))
            {
                UnwatchResidentTarget();
                if (_residentSector.HasValue) _pinnedSectors.Remove(_residentSector.Value);
                _residentTarget = target;
                _residentSector = null;
                target.TreeExiting += OnResidentTargetTreeExiting;
            }
            if (PinMountedOwner(target)) return;
            if (_initialized && GodotObject.IsInstanceValid(_claw))
                _claw.NotifyTargetWillUnload(target);
            return;
        }

        if (!ReferenceEquals(target, _residentTarget)) return;
        EnsureCurrentCoverageBeforeUnpin();
        // The source intentionally keeps ResidencyTarget visible throughout every false
        // subscriber. Keep the world-side reference and owner pin for the same interval so a
        // later subscriber can synchronously transition without detaching the target's sector.
        _residencyRefreshPending = true;
    }

    private bool PinMountedOwner(GrappleSurface2D target)
    {
        if (!TryFindMountedOwner(target, out PrologueSectorId ownerId)) return false;
        if (_residentSector.HasValue && _residentSector.Value != ownerId)
            _pinnedSectors.Remove(_residentSector.Value);
        _residentSector = ownerId;
        _pinnedSectors.Add(ownerId);
        return true;
    }

    private bool TryFindMountedOwner(Node target, out PrologueSectorId ownerId)
    {
        if (GodotObject.IsInstanceValid(target))
        {
            for (Node current = target; current != null; current = current.GetParent())
            {
                if (current is not PrologueSector candidate) continue;
                foreach ((PrologueSectorId id, PrologueSector mounted) in _mounted)
                {
                    if (!ReferenceEquals(candidate, mounted)) continue;
                    ownerId = id;
                    return true;
                }
                break;
            }
        }
        ownerId = default;
        return false;
    }

    private void EnsureCurrentCoverageBeforeUnpin()
    {
        if (!_initialized || _transitioning || _streamModel == null) return;
        RequestTransition(new TransitionRequest(CurrentSector, null, false));
    }

    private bool DrainResidencyRefresh()
    {
        if (!_residencyRefreshPending || _transitioning || _streamModel == null) return false;
        if (GodotObject.IsInstanceValid(_claw) && _claw.ResidencyTarget != null) return false;
        UnwatchResidentTarget();
        if (_residentSector.HasValue) _pinnedSectors.Remove(_residentSector.Value);
        _residentTarget = null;
        _residentSector = null;
        _residencyRefreshPending = false;
        RequestTransition(new TransitionRequest(CurrentSector, null, false));
        return true;
    }

    private void OnResidentTargetTreeExiting()
    {
        GrappleSurface2D target = _residentTarget;
        if (!_initialized || !GodotObject.IsInstanceValid(target) ||
            !GodotObject.IsInstanceValid(_claw)) return;
        _claw.NotifyTargetWillUnload(target);
    }

    private void UnwatchResidentTarget()
    {
        if (GodotObject.IsInstanceValid(_residentTarget))
            _residentTarget.TreeExiting -= OnResidentTargetTreeExiting;
    }

    private bool ResidentTargetBelongsTo(PrologueSector sector)
    {
        if (_residentTarget == null || !GodotObject.IsInstanceValid(_residentTarget)) return false;
        for (Node current = _residentTarget; current != null; current = current.GetParent())
            if (ReferenceEquals(current, sector)) return true;
        return false;
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
            Func<StringName, bool> handler = id => LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion
                ? TryActivateCheckpoint(id.ToString())
                : TryActivateCheckpoint(item.Id, checkpoint.GlobalPosition);
            checkpoint.ActivationRequest = handler;
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
                    Func<StringName, bool> registered) ||
                registered != binding.Handler || binding.Checkpoint.ActivationRequest != binding.Handler)
                throw new InvalidOperationException(
                    $"Sector {item.Id} lost prepared checkpoint binding {binding.Path}.");
    }

    private void UnbindCheckpoints(PrologueSector sector)
    {
        foreach (Node node in Descendants(sector))
        {
            if (node is not CheckpointTrigger checkpoint ||
                !_checkpointHandlers.Remove(checkpoint, out
                    Func<StringName, bool> handler)) continue;
            if (checkpoint.ActivationRequest == handler) checkpoint.ActivationRequest = null;
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

    private bool RecoverOutsideWorld()
    {
        if (!GodotObject.IsInstanceValid(_level) || !GodotObject.IsInstanceValid(_flow) ||
            _flow.State != PrologueGameFlowState.Playing || GetTree().Paused ||
            !_player.IsVisibleInTree() || !_player.IsPhysicsProcessing()) return false;
        Vector2 position = _player.GlobalPosition;
        // Use the union of sector footprints, not their enclosing rectangle (which
        // would turn gaps outside the authored world into unbounded falling space).
        // No top limit: legitimate jump/claw arcs may rise above an authored sector.
        foreach (PrologueSectorDefinition definition in _definitions.Values)
        {
            Rect2 bounds = definition.WorldBounds;
            if (position.X >= bounds.Position.X - OutsideRecoveryMargin &&
                position.X <= bounds.End.X + OutsideRecoveryMargin &&
                position.Y <= bounds.End.Y + OutsideRecoveryMargin) return false;
        }
        _level.RequestRespawn("OUT OF WORLD FALL RESET");
        return true;
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
        if (LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
        {
            BlendRouteCamera(delta);
            return;
        }
        float weight = 1f - Mathf.Exp(-CameraLimitBlendRate * delta);
        Vector2 followPosition = _camera.Position.Lerp(CameraFollowPosition(), weight);
        _cameraTargetBounds = ResolveVisibleCameraBounds(followPosition - _camera.Position);
        Rect2 blended = new(
            _cameraBounds.Position.Lerp(_cameraTargetBounds.Position, weight),
            _cameraBounds.Size.Lerp(_cameraTargetBounds.Size, weight));
        float left = _cameraTargetBounds.Position.X > _cameraBounds.Position.X
            ? _cameraTargetBounds.Position.X
            : blended.Position.X;
        float right = _cameraTargetBounds.End.X < _cameraBounds.End.X
            ? _cameraTargetBounds.End.X
            : blended.End.X;
        _cameraBounds = new Rect2(
            new Vector2(left, blended.Position.Y),
            new Vector2(right - left, blended.Size.Y));
        if (_cameraBounds.Position.DistanceTo(_cameraTargetBounds.Position) < .01f &&
            _cameraBounds.Size.DistanceTo(_cameraTargetBounds.Size) < .01f)
            _cameraBounds = _cameraTargetBounds;
        ApplyCameraBounds(_cameraBounds);
        // Bias the follow position, not Offset: camera feedback owns Offset for shake.
        // Only committed sector state is used, so failed staging cannot move the view.
        _camera.Position = followPosition;
    }

    private Rect2 ResolveVisibleCameraBounds(Vector2 prospectiveCameraDelta)
    {
        Rect2 current = _definitions[CurrentSector].WorldBounds;
        Vector2 viewport = _camera.GetViewportRect().Size;
        Vector2 zoom = _camera.Zoom.Abs();
        if (viewport.X <= 0f || viewport.Y <= 0f || zoom.X <= 0f || zoom.Y <= 0f)
            return current;

        Vector2 center = _camera.GetScreenCenterPosition();
        Vector2 prospectiveCenter = center + prospectiveCameraDelta;
        float halfVisibleHeight = viewport.Y * .5f / zoom.Y;
        float visibleTop = Mathf.Min(center.Y, prospectiveCenter.Y) - halfVisibleHeight;
        float visibleBottom = Mathf.Max(center.Y, prospectiveCenter.Y) + halfVisibleHeight;
        float left = current.Position.X;
        float right = current.End.X;
        foreach ((PrologueSectorId id, PrologueSector sector) in _mounted)
        {
            if (id == CurrentSector || !AreAdjacent(CurrentSector, id) ||
                !GodotObject.IsInstanceValid(sector)) continue;
            Rect2 adjacent = _definitions[id].WorldBounds;
            bool touchesCurrentHorizontally =
                adjacent.Position.X <= current.End.X + CameraCoverageEpsilon &&
                adjacent.End.X >= current.Position.X - CameraCoverageEpsilon;
            bool coversVisibleBand =
                adjacent.Position.Y <= visibleTop + CameraCoverageEpsilon &&
                adjacent.End.Y >= visibleBottom - CameraCoverageEpsilon;
            if (!touchesCurrentHorizontally || !coversVisibleBand) continue;
            left = Mathf.Min(left, adjacent.Position.X);
            right = Mathf.Max(right, adjacent.End.X);
        }
        return new Rect2(new Vector2(left, current.Position.Y),
            new Vector2(right - left, current.Size.Y));
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
        if (LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
        {
            _cameraBounds = CanyonWorldLayout.DesignBounds;
            _cameraTargetBounds = _cameraBounds;
            ApplyCameraBounds(_cameraBounds);
            if (GodotObject.IsInstanceValid(_camera) && _cameraFocusModel != null)
            {
                _cameraFrame = ResolveRouteCameraTarget();
                _camera.Position = _cameraFrame.LocalPosition;
            }
            return;
        }
        _cameraBounds = _cameraTargetBounds;
        ApplyCameraBounds(_cameraBounds);
        if (GodotObject.IsInstanceValid(_camera)) _camera.Position = CameraFollowPosition();
    }

    private Vector2 CameraFollowPosition()
    {
        if (CurrentSector != PrologueSectorId.CanyonAscent) return _cameraRestPosition;
        float headroom = AscentHeadroomPixels;
        // Released/Retract can still retain CurrentTarget. Follow the actual cast phase,
        // and reuse the motor's flight lifetime instead of inventing a camera-only timer.
        if (GodotObject.IsInstanceValid(_playerMotor) && GodotObject.IsInstanceValid(_claw) &&
            !_player.IsOnFloor() && _playerMotor.HasGrappleReleaseMomentum &&
            _claw.CurrentFrame.State is MechanicalClawState.Released or MechanicalClawState.Retract or
                MechanicalClawState.Idle or MechanicalClawState.Failed)
        {
            // Ease back toward the normal center before the apex so the return floor is
            // readable as descent begins. Standing and a fresh aim/cast keep upper headroom.
            headroom *= Mathf.Clamp(-_player.Velocity.Y / ReleaseHeadroomFullRiseSpeed, 0f, 1f);
        }
        return _cameraRestPosition + Vector2.Up * headroom;
    }

    private void BlendRouteCamera(float delta)
    {
        ApplyCameraBounds(CanyonWorldLayout.DesignBounds);
        CanyonCameraFrame target = ResolveRouteCameraTarget();
        _cameraFrame = _cameraFocusModel.Blend(_cameraFrame, target, delta);
        // Camera2D is a direct Player child. The model receives Player.ToLocal so a
        // world focus point is never assigned directly and parent translation is counted once.
        // Camera feedback independently owns Offset for presentation shake.
        _camera.Position = _cameraFrame.LocalPosition;
    }

    private CanyonCameraFrame ResolveRouteCameraTarget() =>
        _cameraFocusModel.ResolveTarget(_player.GlobalPosition, _player.Velocity,
            _cameraRestPosition, _player.ToLocal);

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
        foreach (PrologueSectorId neighbor in definition.Neighbors)
            if (neighbor == right) return true;
        return false;
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
                    Func<StringName, bool> registered) &&
                registered == binding.Handler)
                _checkpointHandlers.Remove(binding.Checkpoint);
            if (GodotObject.IsInstanceValid(binding.Checkpoint) && binding.Checkpoint.ActivationRequest == binding.Handler)
                binding.Checkpoint.ActivationRequest = null;
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
        Func<StringName, bool> Handler, NodePath Path);
    private sealed record StagedSector(PrologueSectorId Id, PrologueSector Sector,
        ProcessModeEnum ProcessMode, bool Visible, List<CollisionState> Collisions,
        List<OwnedNodeState> OwnedNodes, List<CheckpointBinding> Bindings);
    private readonly record struct TransitionRequest(PrologueSectorId Sector,
        Vector2? Relocation, bool SnapCamera);
}
