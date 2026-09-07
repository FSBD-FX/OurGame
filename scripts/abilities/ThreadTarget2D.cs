using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Mechanisms;

namespace Knight.Abilities;

public partial class ThreadTarget2D : GrappleSurface2D
{
    public new const string TargetGroup = "thread_targets";

    [Export]
    public ThreadTargetKind Kind { get; set; }

    [Export]
    public float FallbackAimAssistRange { get; set; }

    [Export]
    public NodePath PullReceiverPath { get; set; } = new NodePath("..");

    public override void _Ready()
    {
        base._Ready();
        AddToGroup(TargetGroup);
    }

    public override bool IsAvailable => base.IsAvailable && IsInGroup(TargetGroup);

    public bool IsAvailableForThread => IsAvailable;

    public bool TryGetEligibleReceiver(Node player, out Node receiver)
    {
        receiver = null;
        if (!GodotObject.IsInstanceValid(player) || !IsLiveNodeChain(player) || !IsAvailableForThread ||
            player.GetTree() != GetTree()) return false;

        Node candidate = GetNodeOrNull(PullReceiverPath);
        if (!GodotObject.IsInstanceValid(candidate) || !IsLiveNodeChain(candidate)) return false;
        HealthComponent health = FindHealth(candidate) ?? FindHealth(this);
        if (GodotObject.IsInstanceValid(health) && health.CurrentHealth <= 0) return false;
        receiver = candidate;
        return true;
    }

    public void ApplyPull(Node2D player, Vector2 origin, ThreadAbilitySettings settings)
    {
        if (!GodotObject.IsInstanceValid(player)) return;
        Node receiver = GetNodeOrNull(PullReceiverPath);
        if (receiver is IThreadInteractionReceiver interactionReceiver &&
            interactionReceiver.TryReceiveThread(new ThreadInteraction(player, origin, GlobalPosition, settings)))
            return;
        Vector2 towardTarget = origin.DirectionTo(GlobalPosition);
        Vector2 towardPlayer = GlobalPosition.DirectionTo(player.GlobalPosition);
        if (Kind == ThreadTargetKind.LightEnemy && receiver is IThreadPullReceiver pullReceiver)
        {
            pullReceiver.ApplyThreadPull(towardPlayer * settings.EnemyPullSpeed);
            return;
        }

        PlayerMotor motor = player.GetNodeOrNull<PlayerMotor>("Motor");
        if (motor != null) motor.ExternalImpulse(towardTarget * settings.PlayerPullSpeed);
    }

    /// <summary>
    /// Applies the old one-shot interaction only when the receiver explicitly accepts it. Unlike
    /// <see cref="ApplyPull"/>, this transactional path never falls back to pulling the caster
    /// after a legacy receiver rejects the request.
    /// </summary>
    public bool TryApplyLegacyInteraction(Node2D player, Vector2 origin, ThreadAbilitySettings settings)
    {
        if (!TryGetEligibleReceiver(player, out Node receiver) || !Kind.IsLegacyInteraction()) return false;
        var interaction = new ThreadInteraction(player, origin, GlobalPosition, settings);
        if (receiver is IThreadInteractionReceiver interactionReceiver)
            return interactionReceiver.TryReceiveThread(interaction);
        if (Kind == ThreadTargetKind.LightEnemy && receiver is IThreadPullReceiver pullReceiver)
        {
            Vector2 towardPlayer = GlobalPosition.DirectionTo(player.GlobalPosition);
            pullReceiver.ApplyThreadPull(towardPlayer * settings.EnemyPullSpeed);
            return true;
        }
        return false;
    }

    private static HealthComponent FindHealth(Node node)
    {
        for (Node current = node; current != null; current = current.GetParent())
        {
            if (current is HealthComponent component) return component;
            HealthComponent child = current.GetNodeOrNull<HealthComponent>("Health");
            if (child != null) return child;
        }
        return null;
    }

}
