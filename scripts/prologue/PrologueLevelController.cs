using System;
using System.Collections.Generic;
using System.Runtime;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.World;
using Knight.Mechanisms;
using Knight.Shared;
using Knight.Ui;
using Knight.Presentation;
using Knight.Vfx;
using Knight.Audio;
using Knight.Save;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;
using Knight.Settings;
using Knight.Interactions;

namespace Knight.Prologue;

public partial class PrologueLevelController : Node2D, ILevelRecovery
{
    [Signal] public delegate void RecoveryRequestedEventHandler(string reason);
    [Signal] public delegate void RecoveryCompletedEventHandler(string reason);
    [Signal] public delegate void ProgressPersistRequestedEventHandler(string reason);

    [Export] public NodePath PlayerRootPath { get; set; } = new("PlayerRoot");
    [Export] public NodePath RoomRootPath { get; set; } = new("RoomRoot");
    [Export] public NodePath SeamlessWorldPath { get; set; } = new();
    [Export] public NodePath HudPath { get; set; } = new("Hud");
    [Export] public StringName MilestoneCompletionExitId { get; set; }
    [Export(PropertyHint.MultilineText)] public string MilestoneCompletionObjective { get; set; } = string.Empty;
    [Export] public Godot.Collections.Array<RoomDefinition> RoomDefinitions { get; set; } = new();
    [Export] public Godot.Collections.Array<PrologueRoomId> RequiredRooms { get; set; } = new();
    [Export] public bool FreezeWorldWhenGameplayInactive { get; set; }

    public CharacterBody2D Player { get; private set; }
    public PrologueRoom CurrentRoom { get; private set; }
    public PrologueRoomId CurrentRoomId { get; private set; } = PrologueRoomId.AwakeningCourtyard;
    public PrologueSectorId? CurrentSector => _seamlessWorld?.CurrentSector;
    public PrologueSector CurrentSectorRoot => _seamlessWorld != null &&
        _seamlessWorld.TryGetMountedSector(_seamlessWorld.CurrentSector, out PrologueSector sector)
            ? sector : null;
    public PrologueProgress Progress { get; } = new();

    private Node2D _roomRoot;
    private SeamlessPrologueWorld _seamlessWorld;
    private PrologueHud _hud;
    private PrologueRouteGraph _graph;
    private readonly Dictionary<PrologueRoomId, RoomDefinition> _definitions = new();
    private readonly Dictionary<PrologueRoomId, PrologueRoom> _preparedRooms = new();
    private readonly List<PrologueRoom> _retiredRooms = new();
    private readonly LegacyPrologueCheckpointState _legacyCheckpoint = new();
    private bool _busy;
    private bool _recoveryQueued;
    private bool _milestoneComplete;
    private static int _activeLowLatencyOwners;
    private static GCLatencyMode _processPreviousGcLatencyMode;
    private bool _gcLatencyModeOwned;
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private string _activePromptId = string.Empty;
    private string _activePromptFallback = string.Empty;

    public override void _Ready()
    {
        AcquireGcLatencyMode();
        Node playerRoot = GetNode(PlayerRootPath);
        bool seamlessConfigured = SeamlessWorldPath != null && !SeamlessWorldPath.IsEmpty;
        _seamlessWorld = seamlessConfigured
            ? GetNodeOrNull<SeamlessPrologueWorld>(SeamlessWorldPath) ??
                throw new InvalidOperationException(
                    $"Configured seamless world is missing at {SeamlessWorldPath}.")
            : null;
        _roomRoot = _seamlessWorld == null
            ? GetNode<Node2D>(RoomRootPath)
            : GetNodeOrNull<Node2D>(RoomRootPath);
        _hud = GetNodeOrNull<PrologueHud>(HudPath);
        Player = playerRoot.GetNodeOrNull<CharacterBody2D>("Player") ??
            throw new InvalidOperationException("PrologueLevelController requires PlayerRoot/Player.");
        Player.AddToGroup("player");

        if (_seamlessWorld == null)
        {
            var definitions = new List<RoomDefinition>();
            foreach (RoomDefinition definition in RoomDefinitions)
            {
                definitions.Add(definition);
                if (definition != null) _definitions[definition.RoomId] = definition;
            }
            _graph = new PrologueRouteGraph(definitions);
            var required = new HashSet<PrologueRoomId>();
            foreach (PrologueRoomId room in RequiredRooms) required.Add(room);
            IReadOnlyList<string> requiredErrors = _graph.ValidateRequiredRooms(required);
            if (requiredErrors.Count > 0)
                throw new InvalidOperationException("Invalid required prologue rooms: " +
                    string.Join("; ", requiredErrors));
            PrepareRoomInstances();
        }
        else
        {
            Progress.SetStartPosition(_seamlessWorld.InitialPlayerPosition);
            _seamlessWorld.CheckpointActivated += OnSeamlessCheckpointActivated;
            _seamlessWorld.SectorMounted += BindContinuousSector;
            GetTree().NodeAdded += OnContinuousNodeAdded;
            foreach (PrologueSectorId sectorId in _seamlessWorld.MountedSectorIds)
                if (_seamlessWorld.TryGetMountedSector(sectorId, out PrologueSector sector))
                    BindContinuousSector(sector);
        }

        HealthComponent health = Player.GetNodeOrNull<HealthComponent>("Health");
        if (health != null)
        {
            health.Died += OnPlayerDied;
            health.Damaged += OnPlayerDamaged;
            health.Healed += OnPlayerHealed;
            _hud?.SetHealth(health.CurrentHealth, health.MaxHealth);
        }
        if (_seamlessWorld == null)
        {
            LegacyPrologueCheckpointSnapshot initial = _legacyCheckpoint.Restore();
            MountRoom(initial.Room, initial.Entrance);
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_seamlessWorld))
        {
            _seamlessWorld.CheckpointActivated -= OnSeamlessCheckpointActivated;
            _seamlessWorld.SectorMounted -= BindContinuousSector;
            if (GetTree() != null) GetTree().NodeAdded -= OnContinuousNodeAdded;
        }
        FreePreparedRooms();
        FreeRetiredRooms();
        RestoreGcLatencyMode();
        if (!IsInstanceValid(Player)) return;
        HealthComponent health = Player.GetNodeOrNull<HealthComponent>("Health");
        if (health != null)
        {
            health.Died -= OnPlayerDied;
            health.Damaged -= OnPlayerDamaged;
            health.Healed -= OnPlayerHealed;
        }
    }

    public override void _Process(double delta)
    {
    }

    public async void TransitionTo(PrologueRoomLink link)
    {
        if (_seamlessWorld != null || _busy || _recoveryQueued || link.SourceRoom != CurrentRoomId) return;
        _busy = true;
        SetPlayerGameplayProcessing(false);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        PrologueRoomId destinationRoom = link.DestinationRoom;
        StringName destinationEntrance = link.EntranceId;
        PrologueRoom next = InstantiateValidatedRoom(ref destinationRoom, ref destinationEntrance);
        // Scene instantiation/validation and tree mounting both invoke substantial authored
        // setup. Keep them on adjacent physics frames so a room boundary cannot combine both
        // costs into one user-visible presentation spike.
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        MountPreparedRoom(next, destinationRoom, destinationEntrance);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        SetPlayerGameplayProcessing(true);
        _busy = false;
    }

    public void RequestRespawn(string reason)
    {
        if (_busy || _recoveryQueued) return;
        // Reject terminal sessions before HUD, pose and telemetry consumers see a request.
        if (GetNodeOrNull<PrologueGameFlowController>("GameFlow")?.AllowsRecoveryRequest() == false)
            return;
        _recoveryQueued = true;
        string resolvedReason = reason ?? "CHECKPOINT RESTORED";
        _hud?.ShowRecovery(resolvedReason);
        PrologueVfxDirector.PlayFromTree(this, PresentationCue.RecoveryStarted,
            Player?.GlobalPosition ?? Vector2.Zero, Vector2.Up);
        EmitSignal(SignalName.RecoveryRequested, resolvedReason);
        if (!_recoveryQueued) return;
        SetPlayerGameplayProcessing(false);
        // Lethal hits arrive inside AreaEntered while the physics server is flushing
        // contacts. Mounting checkpoint collisions must wait until that callback ends.
        Callable.From(() => BeginRecovery(resolvedReason)).CallDeferred();
    }

    public void SetLanguage(GameLanguage language)
    {
        _language = Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        Player?.GetNodeOrNull<EnvironmentInteractionController>("EnvironmentInteraction")?.SetLanguage(_language);
        if (!string.IsNullOrEmpty(_activePromptId)) PresentActivePrompt();
    }

    public void CancelQueuedRecovery() => _recoveryQueued = false;

    public void RecordShortcutOpened()
    {
        RecordShortcutOpened("canyon.v2.temple-bell-gate");
    }

    public void RecordShortcutOpened(StringName shortcutId)
    {
        if (shortcutId == null || shortcutId.IsEmpty || !Progress.OpenShortcut(shortcutId.ToString())) return;
        EmitSignal(SignalName.ProgressPersistRequested, "shortcut");
    }

    public void RecordBossDefeated()
    {
        if (!Progress.MarkBossDefeated()) return;
        EmitSignal(SignalName.ProgressPersistRequested, "boss-defeated");
    }

    public void StartNewGame()
    {
        FreeRetiredRooms();
        Progress.Reset();
        _legacyCheckpoint.Reset();
        _milestoneComplete = false;
        ResetPlayerTransientStateForRelocation();
        if (_seamlessWorld != null)
        {
            _seamlessWorld.ResetForNewRun();
            Progress.SetStartPosition(_seamlessWorld.InitialPlayerPosition);
            if (_seamlessWorld.InitialSector is
                (PrologueSectorId.CanyonBasin or PrologueSectorId.LostTemple))
                Progress.ActivateCheckpoint(
                    _seamlessWorld.InitialSector, _seamlessWorld.InitialPlayerPosition);
        }
        else MountRoom(PrologueRoomId.AwakeningCourtyard, "start");
        Player.GetNodeOrNull<HealthComponent>("Health")?.ResetFull();
        SetGameplayActive(true);
    }

    public void RestoreFromSave(PrologueSaveData data)
    {
        FreeRetiredRooms();
        Progress.Restore(data);
        if (_seamlessWorld == null) _legacyCheckpoint.Reset();
        ResetPlayerTransientStateForRelocation();
        if (_seamlessWorld != null)
        {
            PrologueCheckpointSnapshot checkpoint = Progress.RestoreCheckpoint();
            if (_seamlessWorld.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
                _seamlessWorld.RestoreCheckpoint(checkpoint.CheckpointId);
            else _seamlessWorld.RestoreCheckpoint(checkpoint.SectorId, checkpoint.WorldPosition);
            foreach (PrologueSectorId id in _seamlessWorld.MountedSectorIds)
                if (_seamlessWorld.TryGetMountedSector(id, out PrologueSector mounted))
                    ApplyDurableControlState(mounted);
        }
        else
        {
            LegacyPrologueCheckpointSnapshot snapshot = _legacyCheckpoint.Restore();
            MountRoom(snapshot.Room, snapshot.Entrance);
        }
        Player.GetNodeOrNull<HealthComponent>("Health")?.ResetFull();
        SetGameplayActive(true);
    }

    internal LegacyPrologueCheckpointSnapshot LegacyCheckpointForFixture =>
        _legacyCheckpoint.Restore();

    internal void RestoreLegacyFixtureCheckpoint(PrologueRoomId room, StringName entrance)
    {
        if (_seamlessWorld != null)
            throw new InvalidOperationException("Legacy fixture checkpoints cannot control the seamless canyon.");
        FreeRetiredRooms();
        if (!_legacyCheckpoint.Activate(room, entrance)) return;
        ResetPlayerTransientStateForRelocation();
        LegacyPrologueCheckpointSnapshot snapshot = _legacyCheckpoint.Restore();
        MountRoom(snapshot.Room, snapshot.Entrance);
        Player.GetNodeOrNull<HealthComponent>("Health")?.ResetFull();
        SetGameplayActive(true);
    }

    private void ResetPlayerTransientStateForRelocation()
    {
        if (!GodotObject.IsInstanceValid(Player)) return;
        Player.GetNodeOrNull<PlayerInputReader>("Input")?.ResetForGameplayBoundary();
        Player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        Player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw")?.ResetTransientState();
        Player.GetNodeOrNull<PlayerShieldController>("Shield")?.ResetFull();
        Player.GetNodeOrNull<PlayerEnergyController>("Energy")?.ResetFull();
        Player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        Player.GetNodeOrNull<PlayerPresentationCoordinator>("Presentation")?.ResetRecoveryPresentation();
        Player.Velocity = Vector2.Zero;
    }

    public void SetGameplayActive(bool enabled)
    {
        SetPlayerGameplayProcessing(enabled);
        Node2D playerRoot = GetNodeOrNull<Node2D>(PlayerRootPath);
        playerRoot?.SetVisible(enabled);
        _roomRoot?.SetVisible(enabled);
        if (_seamlessWorld?.GetParent() is CanvasItem worldRoot)
        {
            worldRoot.Visible = enabled;
            if (FreezeWorldWhenGameplayInactive)
                // A collection/healing observer may change session inside BodyEntered.
                // Defer inherited collision disabling until physics queries finish.
                worldRoot.SetDeferred(Node.PropertyName.ProcessMode, (int)(enabled
                    ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled));
        }
        if (FreezeWorldWhenGameplayInactive && playerRoot != null)
            playerRoot.SetDeferred(Node.PropertyName.ProcessMode, (int)(enabled
                ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled));
        if (_hud != null) _hud.Visible = enabled;
    }

    public bool IsInsideAuthoredSectorBounds(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        SeamlessPrologueWorld world = _seamlessWorld ??
            GetNodeOrNull<SeamlessPrologueWorld>(SeamlessWorldPath);
        if (world == null || !float.IsFinite(worldPosition.X) || !float.IsFinite(worldPosition.Y))
            return false;
        foreach (PrologueSectorDefinition definition in world.Definitions)
        {
            if (definition != null && definition.Id == sectorId)
                return definition.WorldBounds.HasPoint(worldPosition);
        }
        return false;
    }

    private void BeginRecovery(string reason)
    {
        if (!_recoveryQueued || !IsInsideTree() || !IsInstanceValid(Player)) return;
        _recoveryQueued = false;
        if (_busy) return;
        _busy = true;
        RecoverInCurrentPhysicsFrame(reason);
    }

    private void RecoverInCurrentPhysicsFrame(string reason)
    {
        SetPlayerGameplayProcessing(false);
        Player.GetNodeOrNull<PlayerInputReader>("Input")?.ResetForGameplayBoundary();
        Player.GetNodeOrNull<HealthComponent>("Health")?.ResetFull();
        Player.GetNodeOrNull<PlayerCombat>("Combat")?.ResetTransientState();
        Player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw")?.ResetTransientState();
        Player.GetNodeOrNull<PlayerShieldController>("Shield")?.ResetFull();
        Player.GetNodeOrNull<PlayerEnergyController>("Energy")?.ResetFull();
        Player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetTransientState();
        Player.Velocity = Vector2.Zero;
        if (_seamlessWorld != null)
        {
            SeamlessPrologueWorld world = _seamlessWorld;
            PrologueSectorId departingId = world.CurrentSector;
            ulong departingInstanceId = world.TryGetMountedSector(departingId,
                out PrologueSector departing) ? departing.GetInstanceId() : 0UL;
            Callable.From(() =>
            {
                if (IsInstanceValid(world) && world.IsInsideTree())
                    world.ResetRecoverySectorsForCheckpoint(departingId, departingInstanceId);
            }).CallDeferred();
            PrologueCheckpointSnapshot snapshot = Progress.RestoreCheckpoint();
            if (_seamlessWorld.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion)
                _seamlessWorld.RestoreCheckpoint(snapshot.CheckpointId);
            else _seamlessWorld.RestoreCheckpoint(snapshot.SectorId, snapshot.WorldPosition);
        }
        else
        {
            LegacyPrologueCheckpointSnapshot snapshot = _legacyCheckpoint.Restore();
            if (CurrentRoom == null || CurrentRoomId != snapshot.Room)
                MountRoom(snapshot.Room, snapshot.Entrance);
            else PlacePlayer(snapshot.Entrance);
        }

        HealthComponent restoredHealth = Player.GetNodeOrNull<HealthComponent>("Health");
        if (restoredHealth != null) _hud?.SetHealth(restoredHealth.CurrentHealth, restoredHealth.MaxHealth);
        CurrentRoom?.ResetRoomState();
        _hud?.SetObjective(reason ?? "CHECKPOINT RESTORED");

        _hud?.HideRecovery();
        PrologueVfxDirector.PlayFromTree(this, PresentationCue.RecoveryCompleted,
            Player?.GlobalPosition ?? Vector2.Zero, Vector2.Up);
        EmitSignal(SignalName.RecoveryCompleted, reason);
        ResumeGameplayAtNextPhysicsBoundary();
    }

    private async void ResumeGameplayAtNextPhysicsBoundary()
    {
        // Completion observers may leave the level before this method starts, or
        // while it is waiting for the next frame. Never ask a detached owner for a tree.
        if (!IsInstanceValid(this) || !IsInsideTree() || !IsInstanceValid(Player)) return;
        // A recovery may be requested from within a physics callback. Crossing the process
        // boundary first keeps every gameplay owner disabled through the rest of that frame;
        // the following physics signal is then the first clean boundary at which to resume.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (!IsInstanceValid(this) || !IsInsideTree() || !IsInstanceValid(Player)) return;
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        if (!IsInstanceValid(this) || !IsInsideTree() || !IsInstanceValid(Player)) return;
        SetPlayerGameplayProcessing(true);
        _busy = false;
    }

    private void OnSeamlessCheckpointActivated(PrologueSectorId sectorId, Vector2 worldPosition)
    {
        if (_seamlessWorld.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion &&
            GetNodeOrNull<PrologueGameFlowController>("GameFlow") != null)
        {
            // Flow has durably committed progress before the world emits success.
            _hud?.SetObjective("CHECKPOINT LIT");
            return;
        }
        bool activated = _seamlessWorld.LayoutVersion == CanyonWorldLayout.CurrentLayoutVersion
            ? Progress.ActivateCheckpoint(_seamlessWorld.CheckpointId)
            : Progress.ActivateCheckpoint(sectorId, worldPosition);
        if (!activated) return;
        _hud?.SetObjective("CHECKPOINT LIT");
        EmitSignal(SignalName.ProgressPersistRequested, "checkpoint");
    }

    private void BindContinuousSector(PrologueSector sector)
    {
        if (!GodotObject.IsInstanceValid(sector) || sector.HasMeta("task9_runtime_bound")) return;
        sector.SetMeta("task9_runtime_bound", true);
        foreach (Node child in Descendants(sector)) BindContinuousNode(child);
        // NodeAdded binds during staging; the world's validated activation then
        // restores authored collision bits. Reapply durable controls only after
        // that transaction commits, without replaying defeats or resetting actors.
        ApplyDurableControlState(sector);
    }

    private void ApplyDurableControlState(PrologueSector sector)
    {
        foreach (Node child in Descendants(sector))
        {
            if (child is BellChainMechanism bell) bell.ApplyProgress(Progress);
            if (child is CanyonShortcutWinch winch) winch.ApplyProgress(Progress);
            if (child is CanyonSteamValve valve) valve.ApplyProgress(Progress);
            if (child is CanyonSealGate seal) seal.ApplyProgress(Progress);
        }
    }

    private void OnContinuousNodeAdded(Node node)
    {
        if (!GodotObject.IsInstanceValid(_seamlessWorld) || node == null) return;
        for (Node current = node; current != null; current = current.GetParent())
            if (current is PrologueSector)
            {
                BindContinuousNode(node);
                return;
            }
    }

    private void BindContinuousNode(Node child)
    {
        if (child == null || child.HasMeta("task10_continuous_runtime_bound")) return;
        child.SetMeta("task10_continuous_runtime_bound", true);
        if (child is TutorialPromptTrigger prompt) prompt.PromptRequested += OnPromptRequested;
        if (child is HiddenAlcoveTrigger hidden) hidden.Discovered += OnHiddenAlcoveDiscovered;
        if (child is PrologueSupplyCache supply) supply.BindRuntime(this);
        if (child is QuestFragment fragment) fragment.BindRuntime(this);
        if (child is CanyonPressurePlateGate pressureGate) pressureGate.PlayerBody = Player;
        if (child is BellChainMechanism mechanism)
        {
            mechanism.ApplyProgress(Progress);
            mechanism.Opened += RecordShortcutOpened;
        }
        if (child is CanyonShortcutWinch winch)
        {
            winch.ApplyProgress(Progress);
            winch.Opened += RecordShortcutOpened;
        }
        if (child is CanyonSteamValve valve) valve.ApplyProgress(Progress);
        if (child is CanyonSealGate seal) seal.ApplyProgress(Progress);
        if (child is CanyonEncounterGroup encounter) encounter.ApplyProgress(Progress);
        if (child is Knight.Boss.BossArenaController bossArena)
        {
            bossArena.BindRuntime(Progress, _hud);
            bossArena.EncounterDefeated += RecordBossDefeated;
        }
        if (child is KillZone killZone) killZone.RoomControllerPath = GetPath();
    }

    private void MountRoom(PrologueRoomId roomId, StringName entranceId)
    {
        PrologueRoom next = InstantiateValidatedRoom(ref roomId, ref entranceId);
        MountPreparedRoom(next, roomId, entranceId);
    }

    private PrologueRoom InstantiateValidatedRoom(ref PrologueRoomId roomId,
        ref StringName entranceId)
    {
        if (!_definitions.TryGetValue(roomId, out RoomDefinition definition))
        {
            GD.PushError($"Missing room definition for {roomId}; falling back to AwakeningCourtyard/start.");
            roomId = PrologueRoomId.AwakeningCourtyard;
            entranceId = "start";
            if (!_definitions.TryGetValue(roomId, out definition))
                throw new InvalidOperationException("AwakeningCourtyard fallback definition is missing.");
        }

        if (!_preparedRooms.Remove(roomId, out PrologueRoom next))
            next = definition.Scene.Instantiate<PrologueRoom>();
        IReadOnlyList<string> errors = next.ValidateContract(entranceId);
        if (errors.Count > 0)
        {
            next.Free();
            throw new InvalidOperationException($"Invalid room {roomId}: {string.Join("; ", errors)}");
        }
        return next;
    }

    private void PrepareRoomInstances()
    {
        try
        {
            foreach ((PrologueRoomId roomId, RoomDefinition definition) in _definitions)
            {
                if (definition?.Scene == null || _preparedRooms.ContainsKey(roomId)) continue;
                PrologueRoom room = definition.Scene.Instantiate<PrologueRoom>();
                IReadOnlyList<string> errors = room.ValidateContract();
                if (errors.Count > 0)
                {
                    room.Free();
                    throw new InvalidOperationException(
                        $"Invalid prepared room {roomId}: {string.Join("; ", errors)}");
                }
                _preparedRooms.Add(roomId, room);
            }
        }
        catch
        {
            FreePreparedRooms();
            throw;
        }
    }

    private void FreePreparedRooms()
    {
        foreach (PrologueRoom room in _preparedRooms.Values)
            if (IsInstanceValid(room)) room.Free();
        _preparedRooms.Clear();
    }

    private void FreeRetiredRooms()
    {
        foreach (PrologueRoom room in _retiredRooms)
            if (IsInstanceValid(room)) room.Free();
        _retiredRooms.Clear();
    }

    private void RestoreGcLatencyMode()
    {
        if (!_gcLatencyModeOwned) return;
        _gcLatencyModeOwned = false;
        _activeLowLatencyOwners = Math.Max(0, _activeLowLatencyOwners - 1);
        if (_activeLowLatencyOwners == 0)
            GCSettings.LatencyMode = _processPreviousGcLatencyMode;
    }

    private void AcquireGcLatencyMode()
    {
        if (_gcLatencyModeOwned) return;
        if (_activeLowLatencyOwners == 0)
        {
            _processPreviousGcLatencyMode = GCSettings.LatencyMode;
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }
        _activeLowLatencyOwners++;
        _gcLatencyModeOwned = true;
    }

    private void TrimRetiredRooms()
    {
        // One detached graph per authored room is a fixed upper bound. This avoids releasing
        // whole room trees on the normal six-room route while preventing revisit loops from
        // growing native and managed ownership without limit.
        int limit = Math.Max(1, _definitions.Count);
        while (_retiredRooms.Count > limit)
        {
            PrologueRoom oldest = _retiredRooms[0];
            _retiredRooms.RemoveAt(0);
            if (IsInstanceValid(oldest)) oldest.Free();
        }
    }

    private void MountPreparedRoom(PrologueRoom next, PrologueRoomId roomId,
        StringName entranceId)
    {
        PrologueRoom previous = CurrentRoom;
        CurrentRoom = next;
        CurrentRoomId = roomId;
        _roomRoot.AddChild(next);
        next.ResolvePresentation();
        BindRoom(next);
        PlacePlayer(entranceId);
        ApplyCameraBounds(next.CameraBounds);
        PrologueAudioDirector.ConfigureRoomFromTree(this, roomId);
        if (previous != null)
        {
            MechanicalClawController claw = Player?
                .GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
            if (claw != null && !claw.NotifyTargetWillUnload(previous))
                claw.ResetTransientState();
            previous.GetParent()?.RemoveChild(previous);
            _retiredRooms.Add(previous);
            TrimRetiredRooms();
        }
    }

    private void BindRoom(PrologueRoom room)
    {
        Node exits = room.GetNodeOrNull("Exits");
        if (exits != null)
            foreach (Node child in exits.GetChildren())
                if (child is RoomExit exit) exit.ExitRequested += OnExitRequested;
        foreach (Node child in Descendants(room))
        {
            if (child is CheckpointTrigger checkpoint) checkpoint.CheckpointActivated += OnCheckpointActivated;
            if (child is TutorialPromptTrigger prompt) prompt.PromptRequested += OnPromptRequested;
            if (child is HiddenAlcoveTrigger hidden) hidden.Discovered += OnHiddenAlcoveDiscovered;
            if (child is BellChainMechanism mechanism)
            {
                mechanism.ApplyProgress(Progress);
                mechanism.Opened += RecordShortcutOpened;
            }
            if (child is CanyonShortcutWinch winch)
            {
                winch.ApplyProgress(Progress);
                winch.Opened += RecordShortcutOpened;
            }
            if (child is CanyonSteamValve valve) valve.ApplyProgress(Progress);
            if (child is CanyonSealGate seal) seal.ApplyProgress(Progress);
            if (child is CanyonEncounterGroup encounter) encounter.ApplyProgress(Progress);
            if (child is Knight.Boss.BossArenaController bossArena)
            {
                bossArena.BindRuntime(Progress, _hud);
                bossArena.EncounterDefeated += RecordBossDefeated;
            }
            if (child is KillZone killZone) killZone.RoomControllerPath = GetPath();
        }
    }

    private void PlacePlayer(StringName entranceId)
    {
        Marker2D entrance = CurrentRoom?.GetEntrance(entranceId) ??
            throw new InvalidOperationException($"Room {CurrentRoomId} is missing entrance '{entranceId}'.");
        Player.GlobalPosition = entrance.GlobalPosition;
        Player.Velocity = Vector2.Zero;
        Player.GetNodeOrNull<PlayerMotor>("Motor")?.ResetFallTracking();
    }

    private void OnExitRequested(StringName exitId)
    {
        if (!_graph.TryResolve(CurrentRoomId, exitId, out PrologueRoomLink link))
        {
            GD.PushError($"No route from {CurrentRoomId} through exit '{exitId}'.");
            return;
        }

        ClearPrompt();
        if (MilestoneCompletionExitId != null && !MilestoneCompletionExitId.IsEmpty &&
            exitId == MilestoneCompletionExitId)
        {
            _milestoneComplete = true;
            _hud?.SetObjective(MilestoneCompletionObjective);
        }
        TransitionTo(link);
    }

    private void OnCheckpointActivated(StringName entranceId)
    {
        if (_legacyCheckpoint.Activate(CurrentRoomId, entranceId))
        {
            _hud?.SetObjective(_milestoneComplete ? MilestoneCompletionObjective : "CHECKPOINT LIT");
            EmitSignal(SignalName.ProgressPersistRequested, "checkpoint");
        }
    }

    private void OnPromptRequested(StringName tutorialId, string text)
    {
        if (!Progress.CompleteTutorial(tutorialId)) return;
        _activePromptId = tutorialId.ToString();
        _activePromptFallback = text ?? string.Empty;
        PresentActivePrompt();
    }

    private void PresentActivePrompt()
    {
        bool english = _language == GameLanguage.English;
        (string glyph, string localized) = _activePromptId switch
        {
            "move" => ("[A/D]", english ? "MOVE" : "移动"),
            "jump" => ($"[{InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse,
                InputActions.Jump.ToString()).Text}]", english ? "JUMP" : "跳跃"),
            "attack" => ($"[{InputGlyphResolver.Resolve(InputDeviceKind.KeyboardMouse,
                InputActions.Attack.ToString()).Text}]", english ? "STRIKE" : "攻击"),
            "thread_anchor" => ("[K]",
                english ? "WASD AIM · GRAB THE DIAMOND TARGET" : "WASD 瞄准 · 抓住菱形标记目标"),
            "thread_enemy" => ("[K]",
                english ? "WASD AIM · PULL THE LIGHT ENEMY" : "WASD 瞄准 · 牵引轻型敌人"),
            "hidden_alcove" => ("[✓]", english ? "HIDDEN ALCOVE FOUND" : "隐藏壁龛已发现"),
            _ => (string.Empty, _activePromptFallback)
        };
        _hud?.ShowPrompt(glyph, localized);
    }

    private void ClearPrompt()
    {
        _activePromptId = string.Empty;
        _activePromptFallback = string.Empty;
        _hud?.ShowPrompt(string.Empty);
    }

    private void OnHiddenAlcoveDiscovered(StringName discoveryId)
    {
        if (discoveryId == "upper_hidden_alcove" && Progress.VisitHiddenAlcove())
        {
            _activePromptId = "hidden_alcove";
            _activePromptFallback = string.Empty;
            PresentActivePrompt();
            EmitSignal(SignalName.ProgressPersistRequested, "discovery");
        }
    }

    private void OnPlayerDied() => RequestRespawn("DEATH RESET");

    private void OnPlayerDamaged(int remaining, Vector2 impulse)
    {
        HealthComponent health = Player?.GetNodeOrNull<HealthComponent>("Health");
        if (health != null) _hud?.SetHealth(remaining, health.MaxHealth);
    }

    private void OnPlayerHealed(int restored, int remaining)
    {
        HealthComponent health = Player?.GetNodeOrNull<HealthComponent>("Health");
        if (health != null) _hud?.SetHealth(remaining, health.MaxHealth);
    }

    private void SetPlayerGameplayProcessing(bool enabled)
    {
        if (!IsInstanceValid(Player)) return;
        Player.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<PlayerInputReader>("Input")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<EnvironmentInteractionController>("EnvironmentInteraction")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<PlayerMotor>("Motor")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<PlayerCombat>("Combat")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<PlayerActionModeController>("ActionModes")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<PlayerShieldController>("Shield")?.SetPhysicsProcess(enabled);
        Player.GetNodeOrNull<PlayerEnergyController>("Energy")?.SetPhysicsProcess(enabled);
    }

    private void ApplyCameraBounds(Rect2 bounds)
    {
        Camera2D camera = Player.GetNodeOrNull<Camera2D>("Camera2D");
        if (camera == null) return;
        camera.LimitLeft = Mathf.RoundToInt(bounds.Position.X);
        camera.LimitTop = Mathf.RoundToInt(bounds.Position.Y);
        camera.LimitRight = Mathf.RoundToInt(bounds.End.X);
        camera.LimitBottom = Mathf.RoundToInt(bounds.End.Y);
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child)) yield return descendant;
        }
    }
}
