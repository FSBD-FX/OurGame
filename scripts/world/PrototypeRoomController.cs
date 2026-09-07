using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies;
using Knight.Player;
using Knight.Prologue;

namespace Knight.World;

/// <summary>Owns fast local recovery and the one-room completion state without reloading a scene.</summary>
public partial class PrototypeRoomController : Node2D, ILevelRecovery
{
    [Export] public NodePath PlayerPath { get; set; } = new NodePath("Player");
    [Export] public NodePath CheckpointTriggerPath { get; set; } = new NodePath("Checkpoint");
    [Export] public NodePath ExitMarkerPath { get; set; } = new NodePath("ExitMarker");
    [Export] public NodePath HudPath { get; set; } = new NodePath("Hud");

    private CharacterBody2D _player = null!;
    private PrototypeCheckpoint _checkpoint = null!;
    private PrototypeHud _hud = null!;
    private bool _respawning;
    private bool _complete;

    public override void _Ready()
    {
        _player = GetNode<CharacterBody2D>(PlayerPath);
        _checkpoint = new PrototypeCheckpoint(_player.GlobalPosition);
        _hud = GetNodeOrNull<PrototypeHud>(HudPath);
        GetNode<Area2D>(CheckpointTriggerPath).BodyEntered += OnCheckpointEntered;
        GetNode<Area2D>(ExitMarkerPath).BodyEntered += OnExitEntered;
        HealthComponent health = _player.GetNodeOrNull<HealthComponent>("Health");
        if (health != null) health.Died += OnPlayerDied;
        _hud?.SetStatus("REACH THE BELL TOWER  •  E + LMB: GRAPPLE  •  Q + LMB: ATTACK");
    }

    public override void _ExitTree()
    {
        if (!IsInstanceValid(_player)) return;
        Area2D checkpoint = GetNodeOrNull<Area2D>(CheckpointTriggerPath);
        Area2D exit = GetNodeOrNull<Area2D>(ExitMarkerPath);
        if (checkpoint != null) checkpoint.BodyEntered -= OnCheckpointEntered;
        if (exit != null) exit.BodyEntered -= OnExitEntered;
        HealthComponent health = _player.GetNodeOrNull<HealthComponent>("Health");
        if (health != null) health.Died -= OnPlayerDied;
    }

    public void RespawnPlayer(string reason = "DEATH RESET")
    {
        if (_respawning || _complete) return;
        _respawning = true;
        _hud?.SetStatus(reason + "  •  RETURNING TO CHECKPOINT");
        RespawnAfterPhysicsFrame();
    }

    public void RequestRespawn(string reason) => RespawnPlayer(reason);

    private async void RespawnAfterPhysicsFrame()
    {
        SetPlayerGameplayProcessing(false);
        _checkpoint.ApplyRespawn(_player);
        HealthComponent health = _player.GetNodeOrNull<HealthComponent>("Health");
        PlayerMotor motor = _player.GetNodeOrNull<PlayerMotor>("Motor");
        PlayerCombat combat = _player.GetNodeOrNull<PlayerCombat>("Combat");
        MechanicalClawController claw = _player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        if (health != null) health.ResetFull();
        if (claw != null) claw.ResetTransientState();
        if (motor != null) motor.ResetTransientState();
        if (combat != null) combat.ResetTransientState();
        foreach (Node node in GetTree().GetNodesInGroup("resettable_enemies"))
            if (node is RustShellSquire enemy) enemy.ResetForCheckpoint();

        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        SetPlayerGameplayProcessing(true);
        _respawning = false;
        _hud?.SetStatus("CHECKPOINT RESTORED  •  PRESS ON");
    }

    private void OnCheckpointEntered(Node2D body)
    {
        if (!body.IsInGroup("player")) return;
        _checkpoint.Activate(body.GlobalPosition);
        _hud?.SetStatus("CHECKPOINT LIT  •  GRAPPLE TO THE ANCHOR");
    }

    private void OnExitEntered(Node2D body)
    {
        if (!body.IsInGroup("player") || _complete) return;
        _complete = true;
        _hud?.SetStatus("PROTOTYPE COMPLETE");
    }

    private void OnPlayerDied()
    {
        RespawnPlayer();
    }

    private void SetPlayerGameplayProcessing(bool enabled)
    {
        _player.SetPhysicsProcess(enabled);
        _player.GetNodeOrNull<PlayerInputReader>("Input")?.SetPhysicsProcess(enabled);
        _player.GetNodeOrNull<PlayerActionModeController>("ActionModes")?.SetPhysicsProcess(enabled);
        _player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw")?.SetPhysicsProcess(enabled);
        _player.GetNodeOrNull<PlayerMotor>("Motor")?.SetPhysicsProcess(enabled);
        _player.GetNodeOrNull<PlayerCombat>("Combat")?.SetPhysicsProcess(enabled);
    }
}
