using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Ui;

namespace Knight.Tests;

/// <summary>Local synchronous query evidence only; not normal-play or frame-time evidence.</summary>
public partial class RunGuidanceLocalAllocationDiagnostic : SceneTree
{
    private const string SavedScene = "res://scenes/prologue/PrologueLevel.tscn";
    private readonly MethodBatch[] _batches =
    [new("Guidance.RefreshNow"), new("WorldRoot/FireflyGuidance.RefreshNow"),
        new("Hud/Guidance/VisualRoot.ResolveProtectedRegions"), new("RoomProtectionRegionAdapter.CollectGameplay"),
        new("CanyonRouteGraph.Reachable"), new("ArchivePressureGate.IdlePhysics"),
        new("MechanicalClawTargetResolver.ResolveKeyboard")];
    private PrologueLevelController _level;
    private PrologueGameFlowController _flow;
    private CharacterBody2D _actor;
    private HealthComponent _health;
    private SeamlessPrologueWorld _world;
    private PrologueGuidanceController _guidance;
    private CanyonFireflyGuidanceController _fireflies;
    private WorldTargetMarkerVisual _marker;
    private Node2D _sectorScope;
    private Node _playerRoot;
    private PrologueFogController _fog;
    private CanyonPressurePlateGate _pressureGate;
    private PlayerInputReader _input;
    private MechanicalClawController _claw;
    private readonly MechanicalClawTargetResolver _clawResolver = new();
    private readonly MechanicalClawTargetResolution[] _keyboardResolutions = new MechanicalClawTargetResolution[72];
    private int _keyboardResolutionCount;
    private IsolatedPrologueFixturePaths _paths;
    private Vector2 _position;
    private ulong _actorId, _worldId, _supportId, _sectorId;
    private int _healthValue, _recoveries;
    private ulong _started;
    private string _mode, _storageRoot, _failure;
    private bool _complete, _written, _returned;
    private IReadOnlyList<Rect2> _lastRegions;
    private IReadOnlyList<CanyonRouteNodeId> _reachable;

    public override async void _Initialize()
    {
        _started = Time.GetTicksUsec();
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            TestAssert.True(args.Length == 2 && args.Count(a => a.StartsWith("--case-storage-root=", StringComparison.Ordinal)) == 1 &&
                args.Count(a => a is "--local-mode=baseline" or "--local-mode=optimized" or "--local-mode=reachability-regression" or "--local-mode=gate-allocation-regression" or "--local-mode=claw-allocation-regression") == 1,
                "local diagnostic accepts exactly storage root and one supported local mode");
            _storageRoot = IsolatedPrologueFixture.ValidateStorageRoot(); // Admission before IO or mounting.
            _mode = args.Single(a => a.StartsWith("--local-mode=", StringComparison.Ordinal))[13..];
            await ToSignal(this, SignalName.ProcessFrame);
            CheckDeadline();
            _level = GD.Load<PackedScene>(SavedScene).Instantiate<PrologueLevelController>();
            _paths = IsolatedPrologueFixture.Configure(_level, "guidance-local-" + _mode);
            _flow = _level.GetNode<PrologueGameFlowController>("GameFlow");
            _flow.AutomationAutoStart = _flow.HeadlessAutoStart = false;
            var ready = ToSignal(_level, Node.SignalName.Ready);
            Root.AddChild(_level);
            await ready;
            _level.RecoveryRequested += _ => _recoveries++;
            TestAssert.True(_level.IsNodeReady(), "actual saved level Ready observed");
            // Flow._Ready schedules InitializeAfterLevelReady with CallDeferred.
            // Match existing saved fixtures, then prove the real loader initialized.
            for (int index = 0; index < 5; index++)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                CheckDeadline();
            }
            TestAssert.True(_flow.LoadResult != null, "original deferred full-world initialization completed");
            TestAssert.Equal(PrologueGameFlowState.Title, _flow.State, "actual saved level starts at Title");
            _flow.StartNew();
            for (int index = 0; index < 60; index++)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                CheckDeadline();
            }
            _actor = _level.Player;
            _input = _actor.GetNode<PlayerInputReader>("Input");
            _claw = _actor.GetNode<MechanicalClawController>("MechanicalClaw");
            _health = _actor.GetNode<HealthComponent>("Health");
            _world = _level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            _fog = _level.GetNode<PrologueFogController>("FogController");
            _guidance = _level.GetNode<PrologueGuidanceController>("Guidance");
            _fireflies = _level.GetNode<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance");
            _marker = _level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            _sectorScope = _level.CurrentSectorRoot.GetParent() as Node2D ?? _level.CurrentSectorRoot;
            _playerRoot = _level.GetNode(_level.PlayerRootPath);
            _position = _actor.GlobalPosition;
            _actorId = _actor.GetInstanceId(); _worldId = _world.GetInstanceId();
            _sectorId = _level.CurrentSectorRoot.GetInstanceId();
            _supportId = SupportId(); _healthValue = _health.CurrentHealth;
            _pressureGate = _world.FindChild("ArchivePressureGate", true, false) as CanyonPressurePlateGate;
            RequireIdleGate();
            Measure(_batches[0], _guidance.RefreshNow);
            Measure(_batches[1], () => _fireflies.RefreshNow());
            Measure(_batches[2], () => _lastRegions = _marker.ResolveProtectedRegions());
            Measure(_batches[3], () => _lastRegions = RoomProtectionRegionAdapter.CollectGameplay(_sectorScope, _playerRoot));
            TestAssert.True(_position.DistanceTo(new Vector2(160f, 600f)) < .1f &&
                _world.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion &&
                _level.Progress.OpenShortcuts.Count == 0 && _level.Progress.ActivatedMechanisms.Count == 0 &&
                !_level.Progress.BossDefeated && _fireflies.CurrentMarker == null,
                "reachability allocation fixture retains literal natural start and closed gates");
            Measure(_batches[4], () => _reachable = CanyonRouteGraph.Reachable(CanyonRouteNodeId.LowCeilingBridge,
                _level.Progress.OpenShortcuts, _level.Progress.ActivatedMechanisms, _level.Progress.BossDefeated));
            TestAssert.True(_reachable.Count == 12 && _reachable[0] == CanyonRouteNodeId.LowCeilingBridge &&
                _reachable[1] == CanyonRouteNodeId.StaggeredRift &&
                !_reachable.Contains(CanyonRouteNodeId.BossHall) && !_reachable.Contains(CanyonRouteNodeId.SafeExit),
                "actual natural-start graph retains twelve ordered nodes and both closed gates");
            // Invoke the real mounted closed gate, with no elapsed time, movement,
            // occupancy mutation or scene/physics toggles. The closed model is stable.
            Measure(_batches[5], () => _pressureGate._PhysicsProcess(0d));
            RequireIdleGate();
            TestAssert.True(_input.CurrentFrame.KeyboardActions && _input.KeyboardAimDirection == Vector2.Right &&
                _claw.CurrentFrame.State == MechanicalClawState.Idle,
                "actual natural keyboard preview remains idle and right-facing without injected input");
            Measure(_batches[6], () => _keyboardResolutions[_keyboardResolutionCount++] =
                _clawResolver.ResolveKeyboard(_actor, _input.KeyboardAimDirection));
            TestAssert.Equal(72, _keyboardResolutionCount, "retain all8 warm and64 measured keyboard results");
            foreach (MechanicalClawTargetResolution resolution in _keyboardResolutions)
                TestAssert.Equal(_keyboardResolutions[0], resolution,
                    "every unmodified local keyboard query returns the same actual target and snapshot");
            TestAssert.Equal(_claw.KeyboardPreview.Surface, _keyboardResolutions[0].Surface,
                "local query agrees with actual live controller keyboard preview surface");
            if (_mode == "claw-allocation-regression")
            {
                long bytes = _batches[6].Raw.Sum(call => call.After.Bytes - call.Before.Bytes);
                TestAssert.True(bytes <= 524_288L,
                    $"KeyboardClawExclusionsDoNotAllocateChildrenArrays: <=524288 bytes for64 warmed full-scene natural keyboard queries; actual={bytes}");
            }
            if (_mode == "gate-allocation-regression")
            {
                long bytes = _batches[5].Raw.Sum(call => call.After.Bytes - call.Before.Bytes);
                TestAssert.True(bytes <= 262_144L,
                    $"IdlePressureGateTraversalDoesNotAllocateChildrenArrays: <=262144 bytes for64 warmed full-scene closed-gate callbacks; actual={bytes}");
            }
            // Break caught: rebuilding the same reachable graph for every candidate
            // needlessly allocates on each real full-scene firefly refresh. Keep all
            // 64 raw calls and the unchanged workload; never force GC or filter.
            if (_mode == "reachability-regression")
            {
                long bytes = _batches[1].Raw.Sum(call => call.After.Bytes - call.Before.Bytes);
                TestAssert.True(bytes <= 1_572_864L,
                    $"FireflyNearestDoesNotRebuildReachabilityPerCandidate: <=1572864 bytes for64 warmed full-scene refreshes; actual={bytes}");
            }
            _complete = true;
        }
        catch (Exception exception) { _failure = exception.ToString(); }
        finally
        {
            try
            {
                if (GodotObject.IsInstanceValid(_level) && _level.IsInsideTree())
                    await TestSceneTeardown.FreeSceneAfterAudioDrain(this, _level);
                else if (GodotObject.IsInstanceValid(_level)) _level.Free();
                CheckDeadline();
            }
            catch (Exception exception) { _failure = (_failure ?? "") + "\nTeardown: " + exception; }
            _returned = true;
            WriteEvidence("body_returned");
        }
        if (_failure != null) GD.PushError(_failure);
        bool passed = _complete && _failure == null && _written;
        GD.Print(passed ? "GUIDANCE_LOCAL_ALLOCATION_OK" : "GUIDANCE_LOCAL_ALLOCATION_FAILED");
        Quit(passed ? 0 : 2);
    }

    public override bool _Process(double delta)
    {
        if (!_returned && Time.GetTicksUsec() - _started > 45_000_000UL)
        {
            _failure ??= "internal_body_limit_45s";
            WriteEvidence("internal_timeout_partial");
            Quit(2);
        }
        return false;
    }

    public override void _Finalize()
    {
        if (_written) return;
        _failure ??= "engine_exit_before_body_return";
        WriteEvidence("engine_finalize_partial");
    }

    private void Measure(MethodBatch batch, Action operation)
    {
        CheckDeadline();
        batch.BeforeWarmup = Context();
        for (int index = 0; index < 8; index++) operation();
        batch.Before = Context();
        batch.BatchStart = Stamp();
        for (int index = 0; index < batch.Raw.Length; index++)
        {
            AllocationStamp before = Stamp();
            try { operation(); }
            finally
            {
                AllocationStamp after = Stamp();
                batch.Raw[index] = new CallRecord(index, before, after);
                batch.Retained++;
            }
        }
        batch.BatchEnd = Stamp();
        batch.After = Context();
        TestAssert.True(batch.BatchStart.Thread == batch.BatchEnd.Thread && batch.BatchStart.Process == batch.BatchEnd.Process,
            "64 synchronous local calls remain on one managed thread and process frame");
        foreach (CallRecord call in batch.Raw)
            TestAssert.True(call.Before.Thread == batch.BatchStart.Thread && call.After.Thread == batch.BatchStart.Thread &&
                call.Before.Process == batch.BatchStart.Process && call.After.Process == batch.BatchStart.Process &&
                call.After.Bytes >= call.Before.Bytes && call.After.Usec >= call.Before.Usec,
                "every local raw call retains monotonic same-thread/frame endpoints");
        CheckDeadline();
    }

    private object Context()
    {
        RequireIdleGate();
        TestAssert.True(GodotObject.IsInstanceValid(_actor) && _actor.IsInsideTree() &&
            GodotObject.IsInstanceValid(_world) && _world.IsInsideTree() &&
            _actor.GetInstanceId() == _actorId && _world.GetInstanceId() == _worldId &&
            _level.CurrentSectorRoot.GetInstanceId() == _sectorId &&
            _health.CurrentHealth == _healthValue && _healthValue > 0 && _recoveries == 0 &&
            _actor.IsOnFloor() && _supportId != 0 && SupportId() == _supportId &&
            _actor.GlobalPosition.DistanceTo(_position) < .1f &&
            _flow.State == PrologueGameFlowState.Playing && !Paused && Engine.TimeScale == 1d,
            "local query context retains same living actor/world/support, position, health and active flow");
        CanyonFireflyFrame frame = _fireflies.CurrentFrame;
        return new
        {
            actor = _actorId, world = _worldId, sector = _sectorId, support = SupportId(),
            positionX = _actor.GlobalPosition.X, positionY = _actor.GlobalPosition.Y,
            health = _health.CurrentHealth, recoveries = _recoveries, flow = _flow.State.ToString(), Paused,
            timeScale = Engine.TimeScale, process = Engine.GetProcessFrames(),
            instructionVisible = _guidance.CurrentInstruction.Visible,
            _marker.HasVisiblePrompt, targets = _marker.RenderedTargetCount,
            fireflyMarker = GodotObject.IsInstanceValid(_fireflies.CurrentMarker) ? _fireflies.CurrentMarker.GetInstanceId() : 0UL,
            fireflyVisible = frame.Visible, frame.VisibleSecondsRemaining, frame.CooldownSecondsRemaining,
            protectedRegionCount = _marker.ResolveProtectedRegions().Count,
            lastMeasuredRegionCount = _lastRegions?.Count ?? -1,
            fog = _fog.GetInstanceId(), fogInsideTree = _fog.IsInsideTree(),
            scope = _sectorScope.GetInstanceId(), playerRoot = _playerRoot.GetInstanceId(),
            mountedSectors = _world.MountedSectorIds.Select(id => id.ToString()).ToArray(),
            pressureGate = _pressureGate.GetInstanceId(), gateOpen = _pressureGate.IsOpen,
            gateShapeDisabled = _pressureGate.DoorCollision.Disabled, gateHasOpened = _pressureGate.HasOpened,
            gatePhysicsProcessing = _pressureGate.IsPhysicsProcessing(),
            keyboard = _input.CurrentFrame.KeyboardActions,
            aimX = _input.KeyboardAimDirection.X, aimY = _input.KeyboardAimDirection.Y,
            clawState = _claw.CurrentFrame.State.ToString(),
            previewTarget = _claw.KeyboardPreview.Snapshot.RuntimeId,
            queryTarget = _keyboardResolutionCount > 0 ? _keyboardResolutions[0].Snapshot.RuntimeId : 0UL,
            retainedKeyboardResults = _keyboardResolutionCount
        };
    }

    private void RequireIdleGate() => TestAssert.True(
        GodotObject.IsInstanceValid(_pressureGate) && _pressureGate.IsInsideTree() &&
        _pressureGate.IsPhysicsProcessing() && _pressureGate.PlayerBody == _actor &&
        _pressureGate.MechanismId == "canyon.v2.pressure-gate.archive-reward" &&
        !_pressureGate.IsOpen && !_pressureGate.DoorCollision.Disabled && !_pressureGate.HasOpened &&
        !_pressureGate.Frame.Traversable && !_pressureGate.IsDangerTelegraphActive,
        "natural-start live archive gate stays closed, bound, processing and unactivated throughout local queries");

    private void CheckDeadline()
    {
        if (Time.GetTicksUsec() - _started > 45_000_000UL) throw new TimeoutException("internal body limit45s");
    }

    private ulong SupportId() => _actor.GetSlideCollisionCount() > 0 ? _actor.GetLastSlideCollision().GetColliderId() : 0;

    private static AllocationStamp Stamp() => new(Time.GetTicksUsec(), System.Environment.CurrentManagedThreadId,
        Engine.GetProcessFrames(), GC.GetAllocatedBytesForCurrentThread());

    private void WriteEvidence(string outcome)
    {
        if (_written || _storageRoot == null) return;
        try
        {
            string path = Path.Combine(_paths?.CaseRoot ?? _storageRoot, "guidance-local-allocation.json");
            using var stream = new FileStream(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);
            JsonSerializer.Serialize(stream, new
            {
                outcome, mode = _mode, savedScene = SavedScene, complete = _complete, failure = _failure,
                processId = System.Environment.ProcessId, args = OS.GetCmdlineArgs(), userArgs = OS.GetCmdlineUserArgs(),
                elapsedMs = (Time.GetTicksUsec() - _started) / 1000d,
                basis = "headless_fixed60_local_synchronous_query_allocations_not_renderer_frame_time_fps_or_normal_play",
                nestedCosts = "Guidance refresh can invoke marker region queries; firefly refresh invokes support queries; keyboard resolver includes LOS and exclusion queries; costs overlap and must not be added",
                measurement = "8 warmed then64 per method in stated order; per-call stamp getter overhead included; numeric arrays preallocated; no filtering, GC, toggles or per-call logging",
                methods = _batches.Select(batch => new
                {
                    batch.Name, warmups = 8, plannedCalls = 64, batch.Retained, batch.BatchStart, batch.BatchEnd,
                    batch.BeforeWarmup, batch.Before, batch.After,
                    totalBytes = batch.Raw.Take(batch.Retained).Sum(call => call.After.Bytes - call.Before.Bytes),
                    elapsedMs = batch.Raw.Take(batch.Retained).Sum(call => (call.After.Usec - call.Before.Usec) / 1000d),
                    raw = batch.Raw.Take(batch.Retained).ToArray()
                }).ToArray()
            });
            _written = true;
            GD.Print("GUIDANCE_LOCAL_ALLOCATION_ARTIFACT path=" + path);
        }
        catch (Exception exception) { _failure = (_failure ?? "") + "\nEvidence: " + exception; GD.PushError(_failure); }
    }

    private readonly record struct AllocationStamp(ulong Usec, int Thread, ulong Process, long Bytes);
    private readonly record struct CallRecord(int Index, AllocationStamp Before, AllocationStamp After);
    private sealed class MethodBatch(string name)
    {
        public readonly string Name = name;
        public readonly CallRecord[] Raw = new CallRecord[64];
        public int Retained;
        public AllocationStamp BatchStart, BatchEnd;
        public object BeforeWarmup, Before, After;
    }
}
