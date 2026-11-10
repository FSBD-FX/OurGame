using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Residents;

// Scheduling only: the manager still owns every native admission predicate.
public sealed partial class CanyonResidentCoordinator : Node
{
    private PrologueLevelController _level;
    private SeamlessPrologueWorld _world;
    private CanyonResidentManager _manager;
    private ResidentPopulationDefinition[] _slots;
    private readonly Dictionary<PrologueSectorId, ulong> _mountedAt = new();
    private long _version, _activeEpoch;
    private bool _preparing, _ready, _active, _deferred;
    private int _resetDepth;
    private string _lastPendingAdmission = "";
    public string Failure { get; private set; } = "";
    public bool IsPrepared => _ready;

    public void Initialize(PrologueLevelController level, SeamlessPrologueWorld world,
        CanyonResidentManager manager, IEnumerable<ResidentPopulationDefinition> slots)
    {
        if (_level != null) throw new InvalidOperationException("Only one coordinator initialization.");
        _level = level; _world = world; _manager = manager; _slots = slots.ToArray();
        world.SectorMounted += OnMounted;
        world.ResidentSectorResetting += OnResetting;
        world.ResidentSectorResetFinished += OnResetFinished;
        foreach (var id in world.MountedSectorIds) _mountedAt[id] = Engine.GetPhysicsFrames();
    }

    public void Cancel()
    { _version++; _preparing = _ready = _active = false; }

    public async Task<bool> Prepare()
    {
        Cancel(); long version = _version, epoch = _manager.Epoch;
        Failure = ""; _lastPendingAdmission = ""; _preparing = true;
        if (!Live() || !_level.ResidentPreparationPending || _world.IsVisibleInTree())
            return Fail("Require genuine hidden session preparation.");
        SceneTree tree = GetTree();
        await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        for (int frame = 0; frame < 180; frame++)
        {
            await ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            if (!Live() || version != _version || epoch != _manager.Epoch) return false;
            if (tree.Paused) continue;
            if (!Reconcile(true, out bool pending)) return false;
            if (!pending) { _preparing = false; _ready = true; return true; }
        }
        return Fail("Resident preparation did not settle within 180 native physics boundaries. " +
            "last_pending=" + _lastPendingAdmission);
    }

    public void Activate()
    {
        if (!_ready || !Live()) throw new InvalidOperationException("Prepare this live session before activation.");
        _activeEpoch = _manager.Epoch; _active = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_active || _preparing || _deferred || !Live() || GetTree().Paused || _level.RecoveryInProgress ||
            _manager.Epoch != _activeEpoch ||
            _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State != PrologueGameFlowState.Playing) return;
        long version = _version; _deferred = true;
        Callable.From(() =>
        {
            _deferred = false;
            if (version != _version || !_active || !Live() || GetTree().Paused || _level.RecoveryInProgress ||
                _manager.Epoch != _activeEpoch ||
                _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State != PrologueGameFlowState.Playing) return;
            Reconcile(false, out _);
        }).CallDeferred();
    }

    private bool Reconcile(bool hidden, out bool pending)
    {
        pending = false;
        long version = _version, epoch = _manager.Epoch;
        // Native mounting and activation publish synchronous callbacks. Title,
        // cancellation or a replacement session revokes this exact generation
        // inside the manager transaction, not just at the next awaited frame.
        bool Authorized() => Live() && version == _version && epoch == _manager.Epoch &&
            !GetTree().Paused && (hidden || !_level.RecoveryInProgress) &&
            (hidden ? _preparing && _level.ResidentPreparationPending && !_world.IsVisibleInTree()
                : _active && !_preparing && _activeEpoch == epoch &&
                  _level.GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.State == PrologueGameFlowState.Playing);
        if (!Authorized()) return false;
        if (_resetDepth != 0) { pending = true; return true; }
        int births = 0;
        foreach (var slot in _slots)
        {
            if (!_world.TryGetMountedSector(slot.Sector, out _)) continue;
            var state = _manager.Snapshot(slot.StableId);
            if (!state.Alive || state.Attached) continue;
            if (!_mountedAt.TryGetValue(slot.Sector, out ulong mounted) || Engine.GetPhysicsFrames() < mounted + 2 ||
                !_world.TryGetResidentSpawnContext(slot.Sector, out _, out _, out var player, out _))
            { pending = true; continue; }
            if (!hidden && !FarFromPlayerAndViewport(slot, player)) { pending = true; continue; }
            bool admitted = _manager.TrySpawnAuthorized(slot.StableId, out _, out _, out var issues, true, Authorized);
            if (!Authorized()) return false;
            if (admitted)
            {
                if (++births >= 2) { pending = true; return true; }
                continue;
            }
            // A frozen player cannot leave its own admission keep-out during
            // hidden preparation. Keep this live slot uncommitted, but let the
            // session start; the active offscreen pass will retry the complete
            // manager transaction after the player has actually moved away.
            // Mixed geometry/context failures must never be hidden this way.
            if (hidden && issues.Count > 0 && issues.Any(x => x is
                "reserved.Protected:current_player" or "start.reserved.Protected:current_player" or
                "end.reserved.Protected:current_player" or "corridor_reserved.Protected:current_player") &&
                issues.All(ResidentPlacementGeometry.IsCurrentPlayerIssue)) continue;
            if (hidden && issues.Any(x => x.Contains("current_player", StringComparison.Ordinal)))
                return Fail(slot.StableId + ": " + string.Join("|", issues));
            if (issues.Any(x => x.Contains("current_player", StringComparison.Ordinal) ||
                x is "visible_late_birth" or "invalid_spawn_context" or "spawn_context_changed" or
                    "mount_context_changed" or "activation_context_changed"))
            { _lastPendingAdmission = slot.StableId + ":" + string.Join("|", issues); pending = true; continue; }
            return Fail(slot.StableId + ": " + string.Join("|", issues));
        }
        return true;
    }

    private bool FarFromPlayerAndViewport(ResidentPopulationDefinition slot, CharacterBody2D player)
    {
        // Conservative prefilter only; the exact manager checks still run.
        if (!GodotObject.IsInstanceValid(player) || slot.Placement == null) return false;
        Rect2 home = new(slot.Placement.WorldSpawn - new Vector2(96, 96), new(192, 192));
        if (home.Grow(128).HasPoint(player.GlobalPosition) ||
            home.Grow(128).HasPoint(player.GlobalPosition + player.Velocity * .25f)) return false;
        if (!_world.IsVisibleInTree()) return false;
        var viewport = _world.GetViewport();
        return !viewport.GetVisibleRect().Intersects(viewport.CanvasTransform * home);
    }

    private bool Live() => GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion() &&
        GodotObject.IsInstanceValid(_level) && _level.IsInsideTree() && !_level.IsQueuedForDeletion() &&
        GodotObject.IsInstanceValid(_world) && _world.IsInsideTree() && !_world.IsQueuedForDeletion();
    private bool Fail(string reason)
    { Failure = reason; _preparing = _ready = _active = false; return false; }
    private void OnMounted(PrologueSector sector) => _mountedAt[sector.SectorId] = Engine.GetPhysicsFrames();
    private void OnResetting(PrologueSector sector) { _resetDepth++; _ready = false; }
    private void OnResetFinished(PrologueSector sector, bool success)
    {
        _resetDepth = Math.Max(0, _resetDepth - 1); OnMounted(sector);
        if (!success) Fail("Resident sector reset failed: " + sector.SectorId);
    }
    public override void _ExitTree()
    {
        Cancel();
        if (!GodotObject.IsInstanceValid(_world)) return;
        _world.SectorMounted -= OnMounted;
        _world.ResidentSectorResetting -= OnResetting;
        _world.ResidentSectorResetFinished -= OnResetFinished;
    }
}
