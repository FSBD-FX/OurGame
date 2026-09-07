using System;
using Godot;
using Knight.Presentation;
using Knight.Prologue;

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

    public FallenBellkeeper CurrentBoss { get; private set; }
    public bool IsEncounterActive { get; private set; }

    private CollisionShape2D _entryDoor;
    private CollisionShape2D _exitDoor;
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
        _trigger = GetNode<Area2D>(ActivationTriggerPath);
        _entryDoorVisual = _entryDoor.GetParent().GetNodeOrNull<PrologueMechanismVisual>("Visual");
        _exitDoorVisual = _exitDoor.GetParent().GetNodeOrNull<PrologueMechanismVisual>("Visual");
        _entryDoorPanel = _entryDoor.GetParent().GetNodeOrNull<CanvasItem>("ReadableDoor");
        _exitDoorPanel = _exitDoor.GetParent().GetNodeOrNull<CanvasItem>("ReadableDoor");
        _trigger.BodyEntered += OnTriggerBodyEntered;
        SpawnFreshBoss();
        ApplyPreFightDoors();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_trigger)) _trigger.BodyEntered -= OnTriggerBodyEntered;
        DisconnectBoss();
    }

    public void BeginEncounter()
    {
        if (IsEncounterActive || CurrentBoss == null) return;
        IsEncounterActive = true;
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        SetDoorVisuals(false, false);
        CurrentBoss.ActivateEncounter();
        UpdateHud(CurrentBoss.CurrentFrame, CurrentBoss.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth);
    }

    public void BindRuntime(PrologueProgress progress, PrologueHud hud)
    {
        _progress = progress;
        _hud = hud;
        if (!IsEncounterActive) _hud?.HideBoss();
    }

    public void ResetForCheckpoint()
    {
        IsEncounterActive = false;
        DisconnectBoss();
        if (IsInstanceValid(CurrentBoss))
        {
            Node parent = CurrentBoss.GetParent();
            parent?.RemoveChild(CurrentBoss);
            CurrentBoss.QueueFree();
        }
        CurrentBoss = null;
        SpawnFreshBoss();
        ApplyPreFightDoors();
    }

    private void SpawnFreshBoss()
    {
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
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        SetDoorVisuals(true, false);
        _hud?.HideBoss();
    }

    private void OnTriggerBodyEntered(Node2D body)
    {
        if (body.IsInGroup("player")) BeginEncounter();
    }

    private void OnBossDefeated()
    {
        IsEncounterActive = false;
        _entryDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
        _exitDoor.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
        SetDoorVisuals(false, true);
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
