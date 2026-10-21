using System;
using System.Collections.Generic;
using Godot;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Collections;

namespace Knight.Boss;

public partial class BossArenaController : Node, ICheckpointResettable
{
    [Signal] public delegate void EncounterDefeatedEventHandler();
    [Signal] public delegate void BossReadyEventHandler();

    [Export] public PackedScene BossScene { get; set; }
    [Export] public NodePath BossRootPath { get; set; } = new("../BossRoot");
    [Export] public NodePath EntryDoorCollisionPath { get; set; } = new("../Doors/Entry/Collision");
    [Export] public NodePath ExitDoorCollisionPath { get; set; } = new("../Doors/Exit/Collision");
    [Export] public NodePath ActivationTriggerPath { get; set; } = new("../ActivationTrigger");
    [Export] public Vector2 BossSpawnPosition { get; set; } = new(430, 188);
    // Only the current canyon opts in. Original layout1/sample arenas retain
    // their own admission contract rather than silently acquiring a new quota.
    [Export] public bool RequiresFragmentAdmission { get; set; }
    [Export] public NodePath AdmissionBarrierCollisionPath { get; set; } = new("../Doors/AdmissionBarrier/Collision");

    public FallenBellkeeper CurrentBoss { get; private set; }
    public bool IsEncounterActive { get; private set; }

    private CollisionShape2D _entryDoor;
    private CollisionShape2D _exitDoor;
    private CollisionShape2D[] _admissionBarrierShapes = Array.Empty<CollisionShape2D>();
    private bool? _preFightEntryOpen;
    private bool _encounterDefeated;
    private Area2D _trigger;
    private PrologueProgress _progress;
    private PrologueHud _hud;
    private PrologueMechanismVisual _entryDoorVisual;
    private PrologueMechanismVisual _exitDoorVisual;
    private CanvasItem _entryDoorPanel;
    private CanvasItem _exitDoorPanel;

    public override void _Ready()
    {
        _entryDoor = GetNode<CollisionShape2D>(EntryDoorCollisionPath);
        _exitDoor = GetNode<CollisionShape2D>(ExitDoorCollisionPath);
        if (RequiresFragmentAdmission)
        {
            // Only this authored admission body belongs to the quota boundary;
            // never include the independent Entry/Exit combat doors.
            var shapes = new List<CollisionShape2D>();
            var admissionBody = GetNode<CollisionShape2D>(AdmissionBarrierCollisionPath).GetParent<StaticBody2D>();
            foreach (Node child in admissionBody.GetChildren())
                if (child is CollisionShape2D shape) shapes.Add(shape);
            _admissionBarrierShapes = shapes.ToArray();
        }
        _trigger = GetNode<Area2D>(ActivationTriggerPath);
        _entryDoorVisual = _entryDoor.GetParent().GetNodeOrNull<PrologueMechanismVisual>("Visual");
        _exitDoorVisual = _exitDoor.GetParent().GetNodeOrNull<PrologueMechanismVisual>("Visual");
        _entryDoorPanel = _entryDoor.GetParent().GetNodeOrNull<CanvasItem>("ReadableDoor");
        _exitDoorPanel = _exitDoor.GetParent().GetNodeOrNull<CanvasItem>("ReadableDoor");
        _trigger.BodyEntered += OnTriggerBodyEntered;
        if (_progress?.BossDefeated == true) RestoreDefeatedEncounter();
        else
        {
            SpawnFreshBoss();
            ApplyPreFightDoors();
        }
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_trigger)) _trigger.BodyEntered -= OnTriggerBodyEntered;
        DisconnectBoss();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Collections and durable mechanism state can change while this sector
        // remains mounted. One shared decision drives the physical and trigger gates.
        if (_progress?.BossDefeated == true)
        {
            if (!_encounterDefeated) RestoreDefeatedEncounter();
            return;
        }
        if (RequiresFragmentAdmission && !IsEncounterActive && !_encounterDefeated)
            ApplyPreFightDoors();
    }

    public void BeginEncounter()
    {
        if (IsEncounterActive || _encounterDefeated || !IsInstanceValid(CurrentBoss) || _progress?.BossDefeated == true) return;
        if (CurrentBoss.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth <= 0) return;
        if (RequiresFragmentAdmission && !BossAdmissionModel.Evaluate(_progress).Allowed)
        {
            ApplyPreFightDoors();
            return;
        }
        _preFightEntryOpen = null;
        IsEncounterActive = true;
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        SetAdmissionBarrierDisabled(true);
        SetDoorVisuals(false, false);
        CurrentBoss.ActivateEncounter();
        UpdateHud(CurrentBoss.CurrentFrame, CurrentBoss.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth);
    }

    public void BindRuntime(PrologueProgress progress, PrologueHud hud)
    {
        _progress = progress;
        _hud = hud;
        if (_progress?.BossDefeated == true) RestoreDefeatedEncounter();
        else if (_entryDoor != null && !IsEncounterActive && !_encounterDefeated) ApplyPreFightDoors();
        if (!IsEncounterActive) _hud?.HideBoss();
    }

    public void ResetForCheckpoint()
    {
        if (_progress?.BossDefeated == true) { RestoreDefeatedEncounter(); return; }
        IsEncounterActive = false;
        DisconnectBoss();
        if (IsInstanceValid(CurrentBoss))
        {
            Node parent = CurrentBoss.GetParent();
            parent?.RemoveChild(CurrentBoss);
            CurrentBoss.QueueFree();
        }
        CurrentBoss = null;
        _preFightEntryOpen = null;
        SpawnFreshBoss();
        ApplyPreFightDoors();
    }

    public void RestoreDefeatedEncounter()
    {
        // Continuous NodeAdded binding may precede this node's _Ready. BindRuntime
        // retains progress; _Ready applies it once the authored doors are available.
        if (_entryDoor == null || _exitDoor == null) return;
        // Restore persisted state without replaying a death, reward or progress event.
        IsEncounterActive = false;
        _encounterDefeated = true;
        DisconnectBoss();
        if (IsInstanceValid(CurrentBoss))
        {
            CurrentBoss.Visible = false;
            CurrentBoss.ProcessMode = ProcessModeEnum.Disabled;
            CurrentBoss.QueueFree();
        }
        CurrentBoss = null;
        _preFightEntryOpen = null;
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        SetAdmissionBarrierDisabled(true);
        SetDoorVisuals(true, true);
        _hud?.HideBoss();
    }

    private void SpawnFreshBoss()
    {
        _encounterDefeated = false;
        if (BossScene == null) throw new InvalidOperationException("BossArenaController requires BossScene.");
        Node2D bossRoot = GetNode<Node2D>(BossRootPath);
        CurrentBoss = BossScene.Instantiate<FallenBellkeeper>();
        CurrentBoss.Name = "FallenBellkeeper";
        bossRoot.AddChild(CurrentBoss);
        CurrentBoss.Position = BossSpawnPosition;
        CurrentBoss.Defeated += OnBossDefeated;
        CurrentBoss.FrameChanged += OnBossFrameChanged;
        EmitSignal(SignalName.BossReady);
    }

    private void DisconnectBoss()
    {
        if (!IsInstanceValid(CurrentBoss)) return;
        CurrentBoss.Defeated -= OnBossDefeated;
        CurrentBoss.FrameChanged -= OnBossFrameChanged;
    }

    private void ApplyPreFightDoors()
    {
        if (_entryDoor == null || _exitDoor == null || _encounterDefeated) return; // BindRuntime may precede _Ready.
        bool open = !RequiresFragmentAdmission || BossAdmissionModel.Evaluate(_progress).Allowed;
        if (_preFightEntryOpen == open) return;
        _preFightEntryOpen = open;
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, open);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        SetAdmissionBarrierDisabled(open);
        SetDoorVisuals(open, false);
        _hud?.HideBoss();
    }

    private void OnTriggerBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player")) BeginEncounter();
    }

    private void SetAdmissionBarrierDisabled(bool disabled)
    {
        foreach (CollisionShape2D shape in _admissionBarrierShapes)
            shape.SetDeferred(CollisionShape2D.PropertyName.Disabled, disabled);
    }

    private void OnBossDefeated()
    {
        // A dormant opt-in Boss can still receive external/environmental Health
        // damage. Only a genuinely admitted active encounter may record victory.
        if (RequiresFragmentAdmission && !IsEncounterActive) return;
        IsEncounterActive = false;
        _encounterDefeated = true;
        _preFightEntryOpen = null;
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        SetAdmissionBarrierDisabled(true);
        SetDoorVisuals(true, true);
        _hud?.HideBoss();
        EmitSignal(SignalName.EncounterDefeated);
    }

    private void OnBossFrameChanged(FallenBellkeeperFrame frame, int facing)
    {
        if (!IsEncounterActive || CurrentBoss == null) return;
        Knight.Combat.HealthComponent health = CurrentBoss.GetNode<Knight.Combat.HealthComponent>("Health");
        UpdateHud(frame, health.CurrentHealth);
    }

    private void UpdateHud(FallenBellkeeperFrame frame, int currentHealth)
    {
        int phase = frame.State is FallenBellkeeperState.Transition or FallenBellkeeperState.PhaseTwoArmored or
            FallenBellkeeperState.HammerTelegraph or FallenBellkeeperState.HammerThrown or FallenBellkeeperState.Exposed
            ? 2 : 1;
        _hud?.ShowBoss("THE FALLEN BELLKEEPER", currentHealth,
            CurrentBoss.GetNode<Knight.Combat.HealthComponent>("Health").MaxHealth,
            phase, frame.State == FallenBellkeeperState.Exposed);
    }

    private void SetDoorVisuals(bool entryOpen, bool exitOpen)
    {
        _entryDoorVisual?.SetState(entryOpen ? MechanismVisualState.Active : MechanismVisualState.Disabled);
        _exitDoorVisual?.SetState(exitOpen ? MechanismVisualState.Active : MechanismVisualState.Disabled);
        if (_entryDoorPanel != null) _entryDoorPanel.Visible = !entryOpen;
        if (_exitDoorPanel != null) _exitDoorPanel.Visible = !exitOpen;
    }
}
