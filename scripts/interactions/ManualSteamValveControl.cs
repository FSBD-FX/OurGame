using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;

namespace Knight.Interactions;

// Opt-in child of one authored valve target. Existing remote-only mechanisms
// never acquire this interface merely because they accept a thread interaction.
public partial class ManualSteamValveControl : Node2D, IEnvironmentInteractable
{
    public Node2D InteractionNode => GetParentOrNull<ThreadTarget2D>();
    public string StableId => Valve()?.ActivationId ?? string.Empty;
    public string InteractionTextKey => "interaction.steam_valve";

    public override void _Ready() => AddToGroup(EnvironmentInteractionController.InteractableGroup);
    public override void _ExitTree() => RequestReady();

    public bool CanInteract(CharacterBody2D actor)
    {
        if (!Live(this) || !Live(actor) || InteractionNode is not ThreadTarget2D target ||
            !Live(target) || !target.IsVisibleInTree() || !target.IsAvailable ||
            target.Kind != ThreadTargetKind.Mechanism || target.Validate().Count != 0 ||
            target.Material is GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden ||
            target.GetWorld2D() != actor.GetWorld2D() || !actor.IsOnFloor() ||
            Mathf.Abs(actor.Velocity.Y) > .1f || Mathf.Abs(actor.Velocity.X) > .1f ||
            actor.GlobalPosition.DistanceSquaredTo(target.GlobalPosition) >
                EnvironmentInteractionModel.DefaultRadius * EnvironmentInteractionModel.DefaultRadius ||
            actor.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth <= 0 ||
            !target.TryGetEligibleReceiver(actor, out Node receiver) ||
            receiver is not CanyonSteamValve valve || valve.IsActivated || !valve.IsVisibleInTree() ||
            string.IsNullOrWhiteSpace(valve.ActivationId) || valve.GetWorld2D() != actor.GetWorld2D() ||
            valve.GetNodeOrNull<CanyonSteamJet>(valve.PairedJetPath) is not { } jet ||
            !Live(jet) || jet.GetWorld2D() != actor.GetWorld2D()) return false;
        // Keep the original target's world/enemy/other-target occlusion rules;
        // only remote aiming, range and claw energy are replaced by nearby Q.
        return MechanicalClawTargetResolver.HasLineOfSight(actor, target, target.WorldAnchor);
    }

    public bool TryInteract(CharacterBody2D actor) => CanInteract(actor) && Valve()?.TryActivate() == true;

    private CanyonSteamValve Valve() => InteractionNode is ThreadTarget2D target && Live(target)
        ? target.GetNodeOrNull<CanyonSteamValve>(target.PullReceiverPath) : null;

    private static bool Live(Node node)
    {
        if (!GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current = node; current != null; current = current.GetParent())
            if (current.IsQueuedForDeletion()) return false;
        return true;
    }
}
