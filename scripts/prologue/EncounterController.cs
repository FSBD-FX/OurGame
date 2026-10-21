using System;
using System.Collections.Generic;
using Godot;

namespace Knight.Prologue;

public partial class EncounterController : Node, ICheckpointResettable
{
    [Signal] public delegate void CompletedEventHandler();

    [Export] public EncounterDefinition Definition { get; set; }
    [Export] public NodePath ActorRootPath { get; set; } = new("../Actors");
    [Export] public bool AutoActivate { get; set; }

    public bool IsActive { get; private set; }
    public bool IsCompleted { get; private set; }

    private readonly List<Node> _ownedActors = new();
    private readonly HashSet<ulong> _defeatedActors = new();

    public override void _Ready()
    {
        if (AutoActivate) Activate();
    }

    public void Activate()
    {
        if (IsActive) return;
        ValidateRuntimeContract();
        IsActive = true;
        IsCompleted = false;
        SpawnAll();
    }

    public void ResetForCheckpoint()
    {
        if (!IsActive) return;
        RemoveOwnedActors();
        IsCompleted = false;
        _defeatedActors.Clear();
        SpawnAll();
    }

    public override void _ExitTree() => RemoveOwnedActors();

    private void SpawnAll()
    {
        Node actorRoot = GetNode(ActorRootPath);
        foreach (EncounterSpawn spawn in Definition.Spawns)
        {
            Marker2D marker = GetNodeOrNull<Marker2D>(spawn.MarkerPath) ??
                throw new InvalidOperationException(
                    $"Encounter '{Definition.EncounterId}' marker '{spawn.MarkerPath}' is missing or not Marker2D.");
            Node instance = spawn.Scene.Instantiate();
            if (instance is not Node2D actorNode || instance is not IEncounterActor actor)
            {
                instance.Free();
                throw new InvalidOperationException(
                    $"Encounter '{Definition.EncounterId}' spawn '{spawn.SpawnId}' must implement IEncounterActor on a Node2D root.");
            }
            actorNode.Name = spawn.SpawnId;
            actorRoot.AddChild(actorNode);
            actorNode.GlobalPosition = marker.GlobalPosition;
            actor.Defeated += () => OnActorDefeated(actorNode);
            _ownedActors.Add(actorNode);
        }
    }

    private void OnActorDefeated(Node actor)
    {
        if (!IsActive || IsCompleted || !_ownedActors.Contains(actor)) return;
        _defeatedActors.Add(actor.GetInstanceId());
        if (_defeatedActors.Count != _ownedActors.Count) return;
        IsCompleted = true;
        EmitSignal(SignalName.Completed);
    }

    private void RemoveOwnedActors()
    {
        foreach (Node actor in _ownedActors)
        {
            if (!IsInstanceValid(actor)) continue;
            Node parent = actor.GetParent();
            parent?.RemoveChild(actor);
            actor.QueueFree();
        }
        _ownedActors.Clear();
        _defeatedActors.Clear();
    }

    private void ValidateRuntimeContract()
    {
        if (Definition == null) throw new InvalidOperationException("Encounter Definition is required.");
        IReadOnlyList<string> errors = Definition.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException($"Invalid encounter '{Definition.EncounterId}': {string.Join("; ", errors)}");
        if (ActorRootPath == null || ActorRootPath.IsEmpty || GetNodeOrNull(ActorRootPath) == null)
            throw new InvalidOperationException("Encounter ActorRootPath must resolve to a node.");
    }
}
