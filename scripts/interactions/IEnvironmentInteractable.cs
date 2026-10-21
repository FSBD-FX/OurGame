using Godot;

namespace Knight.Interactions;

/// <summary>Opt-in nearby world interaction; not inventory use or a substitute for a claw mechanism.</summary>
public interface IEnvironmentInteractable
{
    string StableId { get; }
    string InteractionTextKey => "interaction.nearby";
    Node2D InteractionNode { get; }
    bool CanInteract(CharacterBody2D actor);
    bool TryInteract(CharacterBody2D actor);
}
